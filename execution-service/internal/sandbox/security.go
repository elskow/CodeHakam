package sandbox

import (
	"fmt"
	"os/exec"
	"regexp"
	"strings"
	"time"
)

type SecurityValidator struct {
	config *SecurityConfig
}

type SecurityConfig struct {
	MaxCodeSize          int64    // Maximum code size in bytes
	AllowedExtensions    []string // Allowed file extensions
	BlacklistedPatterns  []string // Blacklisted code patterns
	NetworkDisabled      bool     // Network access disabled
	FilesystemRestricted bool     // Filesystem access restricted
}

type SecurityViolation struct {
	Type        string `json:"type"`
	Description string `json:"description"`
	Severity    string `json:"severity"`
}

func NewSecurityValidator(config *SecurityConfig) *SecurityValidator {
	return &SecurityValidator{
		config: config,
	}
}

func (sv *SecurityValidator) ValidateCode(code []byte, filename string) []SecurityViolation {
	var violations []SecurityViolation

	// Check file size
	if int64(len(code)) > sv.config.MaxCodeSize {
		violations = append(violations, SecurityViolation{
			Type:        "code_size_exceeded",
			Description: fmt.Sprintf("Code size %d exceeds maximum allowed %d bytes", len(code), sv.config.MaxCodeSize),
			Severity:    "high",
		})
	}

	// Check file extension
	if !sv.isAllowedExtension(filename) {
		violations = append(violations, SecurityViolation{
			Type:        "invalid_extension",
			Description: fmt.Sprintf("File extension not allowed: %s", filename),
			Severity:    "medium",
		})
	}

	// Check for blacklisted patterns
	codeStr := string(code)
	for _, pattern := range sv.config.BlacklistedPatterns {
		if matched, _ := regexp.MatchString(pattern, codeStr); matched {
			violations = append(violations, SecurityViolation{
				Type:        "blacklisted_pattern",
				Description: fmt.Sprintf("Blacklisted pattern detected: %s", pattern),
				Severity:    "critical",
			})
		}
	}

	// Check for binary content
	if sv.containsBinaryContent(code) {
		violations = append(violations, SecurityViolation{
			Type:        "binary_content",
			Description: "Binary content detected in source code",
			Severity:    "critical",
		})
	}

	return violations
}

func (sv *SecurityValidator) ValidateSandboxEnvironment(boxID int) []SecurityViolation {
	var violations []SecurityViolation

	// Verify network isolation
	if sv.config.NetworkDisabled {
		if !sv.isNetworkIsolated(boxID) {
			violations = append(violations, SecurityViolation{
				Type:        "network_not_isolated",
				Description: "Network isolation not properly enforced",
				Severity:    "critical",
			})
		}
	}

	// Verify filesystem restrictions
	if sv.config.FilesystemRestricted {
		if !sv.isFilesystemRestricted(boxID) {
			violations = append(violations, SecurityViolation{
				Type:        "filesystem_not_restricted",
				Description: "Filesystem restrictions not properly enforced",
				Severity:    "critical",
			})
		}
	}

	// Check for sandbox escape attempts
	if sv.hasSandboxEscapeAttempts(boxID) {
		violations = append(violations, SecurityViolation{
			Type:        "sandbox_escape_attempt",
			Description: "Potential sandbox escape attempt detected",
			Severity:    "critical",
		})
	}

	return violations
}

func (sv *SecurityValidator) ValidateResourceUsage(cpuTime, wallTime, memoryKb int, timeLimit time.Duration, memoryLimit int) []SecurityViolation {
	var violations []SecurityViolation

	timeLimitMs := int(timeLimit.Milliseconds())

	// Check for suspicious resource usage patterns
	if wallTime > timeLimitMs*2 {
		violations = append(violations, SecurityViolation{
			Type:        "excessive_wall_time",
			Description: fmt.Sprintf("Wall time %dms significantly exceeds limit %dms", wallTime, timeLimitMs),
			Severity:    "medium",
		})
	}

	if memoryKb > memoryLimit*2 {
		violations = append(violations, SecurityViolation{
			Type:        "excessive_memory_usage",
			Description: fmt.Sprintf("Memory usage %dKB significantly exceeds limit %dKB", memoryKb, memoryLimit),
			Severity:    "medium",
		})
	}

	// Check for potential infinite loops or resource exhaustion
	if cpuTime > 0 && wallTime > cpuTime*10 {
		violations = append(violations, SecurityViolation{
			Type:        "potential_infinite_loop",
			Description: "Wall time much higher than CPU time, possible infinite loop",
			Severity:    "low",
		})
	}

	return violations
}

func (sv *SecurityValidator) isAllowedExtension(filename string) bool {
	for _, ext := range sv.config.AllowedExtensions {
		if strings.HasSuffix(strings.ToLower(filename), ext) {
			return true
		}
	}
	return false
}

func (sv *SecurityValidator) containsBinaryContent(code []byte) bool {
	// Simple heuristic: if more than 1% of bytes are non-printable ASCII, consider it binary
	nonPrintable := 0
	for _, b := range code {
		if b < 32 || b > 126 {
			if b != '\n' && b != '\r' && b != '\t' {
				nonPrintable++
			}
		}
	}

	return float64(nonPrintable)/float64(len(code)) > 0.01
}

func (sv *SecurityValidator) isNetworkIsolated(boxID int) bool {
	// Check if network namespace is properly isolated
	cmd := exec.Command("ip", "netns", "list")
	output, err := cmd.CombinedOutput()
	if err != nil {
		return false
	}

	// Look for isolate network namespace
	return strings.Contains(string(output), fmt.Sprintf("isolate_%d", boxID))
}

func (sv *SecurityValidator) isFilesystemRestricted(boxID int) bool {
	// Check if sandbox filesystem is properly restricted
	boxDir := fmt.Sprintf("/var/local/lib/isolate/%d", boxID)

	// Check for dangerous symlinks or mount points
	dangerousPaths := []string{"/proc", "/sys", "/dev", "/etc/passwd", "/etc/shadow"}

	for _, path := range dangerousPaths {
		fullPath := fmt.Sprintf("%s%s", boxDir, path)
		if _, err := exec.Command("test", "-e", fullPath).CombinedOutput(); err == nil {
			// Path exists in sandbox, check if it's properly restricted
			cmd := exec.Command("mountpoint", fullPath)
			if err := cmd.Run(); err != nil {
				// Not a mount point, potential security issue
				return false
			}
		}
	}

	return true
}

func (sv *SecurityValidator) hasSandboxEscapeAttempts(boxID int) bool {
	// Check for common sandbox escape patterns in logs
	// This would typically check execution logs or sandbox filesystem
	// For now, return false as this would require log integration
	return false
}

func (sv *SecurityValidator) GetDefaultSecurityConfig() *SecurityConfig {
	return &SecurityConfig{
		MaxCodeSize:       65536, // 64KB
		AllowedExtensions: []string{".cpp", ".c", ".java", ".py", ".go", ".rs", ".js", ".ts"},
		BlacklistedPatterns: []string{
			`(?i)system\s*\(`,         // system() calls
			`(?i)exec\s*\(`,           // exec() calls
			`(?i)fork\s*\(`,           // fork() calls
			`(?i)clone\s*\(`,          // clone() calls
			`(?i)ptrace\s*\(`,         // ptrace() calls
			`(?i)socket\s*\(`,         // socket() calls
			`(?i)connect\s*\(`,        // connect() calls
			`(?i)bind\s*\(`,           // bind() calls
			`(?i)listen\s*\(`,         // listen() calls
			`(?i)accept\s*\(`,         // accept() calls
			`(?i)open\s*\(["']*/`,     // absolute file paths
			`(?i)fopen\s*\(["']*/`,    // absolute file paths
			`(?i)#include\s*<`,        // system includes (potential escape)
			`(?i)import\s+os`,         // Python os module
			`(?i)import\s+subprocess`, // Python subprocess
			`(?i)Runtime\.getRuntime`, // Java runtime access
			`(?i)ProcessBuilder`,      // Java process builder
			`(?i)cmd\.Exec`,           // Go exec package
			`(?i)os\.Exec`,            // Go os/exec
		},
		NetworkDisabled:      true,
		FilesystemRestricted: true,
	}
}
