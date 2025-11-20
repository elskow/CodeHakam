package services

import (
	"context"
	"encoding/json"
	"fmt"
	"regexp"
	"strings"
	"unicode/utf8"
)

type InputSanitizer struct {
	config *SanitizationConfig
}

type SanitizationConfig struct {
	MaxCodeSize       int
	MaxFilenameLength int
	AllowedExtensions []string
	ForbiddenPatterns []string
	SQLInjection      []string
	XSSPatterns       []string
	CommandInjection  []string
	PathTraversal     []string
	SuspiciousImports []string
	HardcodedSecrets  []string
}

type SanitizationResult struct {
	IsValid    bool
	Violations []SanitizationViolation
	Sanitized  string
}

type SanitizationViolation struct {
	Type        string
	Line        int
	Description string
	Severity    string
	Suggestion  string
}

func NewInputSanitizer() *InputSanitizer {
	config := &SanitizationConfig{
		MaxCodeSize:       1024 * 1024, // 1MB
		MaxFilenameLength: 255,
		AllowedExtensions: []string{".cpp", ".c", ".cc", ".cxx", ".java", ".py", ".go", ".js", ".ts"},
		ForbiddenPatterns: []string{
			`eval\s*\(`,
			`exec\s*\(`,
			`system\s*\(`,
			`shell_exec\s*\(`,
			`passthru\s*\(`,
			`<script[^>]*>`,
			`javascript:`,
			`vbscript:`,
			`onload\s*=`,
			`onerror\s*=`,
		},
		SQLInjection: []string{
			`(?i)(union|select|insert|update|delete|drop|create|alter|exec|execute)\s`,
			`(?i)(or|and)\s+\d+\s*=\s*\d+`,
			`(?i)'\s*or\s*'`,
			`(?i)"\s*or\s*"`,
			`(?i)--`,
			`(?i)#`,
			`(?i)/\*.*\*/`,
		},
		XSSPatterns: []string{
			`(?i)<script[^>]*>.*?</script>`,
			`(?i)javascript:`,
			`(?i)vbscript:`,
			`(?i)onload\s*=`,
			`(?i)onerror\s*=`,
			`(?i)onclick\s*=`,
			`(?i)onmouseover\s*=`,
		},
		CommandInjection: []string{
			`(?i)(rm|del|format|fdisk|mkfs)\s`,
			`(?i)(cat|type)\s+/etc/`,
			`(?i)(wget|curl|nc|netcat)\s`,
			`(?i)(chmod|chown|chgrp)\s`,
			`(?i)(sudo|su)\s`,
			`(?i)(ps|top|kill)\s`,
		},
		PathTraversal: []string{
			`\.\./.*`,
			`\.\.\\.*`,
			`/etc/`,
			`/proc/`,
			`/sys/`,
			`C:\\Windows\\`,
			`%windir%`,
		},
		SuspiciousImports: []string{
			`(?i)import\s+os\.system`,
			`(?i)import\s+subprocess`,
			`(?i)import\s+socket`,
			`(?i)import\s+urllib`,
			`(?i)import\s+requests`,
			`(?i)import\s+ftplib`,
			`(?i)Runtime\.getRuntime\(\)`,
			`(?i)ProcessBuilder`,
			`(?i)System\.exec`,
		},
		HardcodedSecrets: []string{
			`(?i)password\s*=\s*["'][^"']+["']`,
			`(?i)secret\s*=\s*["'][^"']+["']`,
			`(?i)key\s*=\s*["'][^"']+["']`,
			`(?i)token\s*=\s*["'][^"']+["']`,
			`(?i)api_key\s*=\s*["'][^"']+["']`,
			`AKIA[0-9A-Z]{16}`,
			`[0-9a-f]{32}`,
			`[0-9a-f]{40}`,
		},
	}

	return &InputSanitizer{config: config}
}

func (is *InputSanitizer) ValidateCode(ctx context.Context, code []byte, language string) (*SanitizationResult, error) {
	result := &SanitizationResult{
		IsValid:    true,
		Violations: []SanitizationViolation{},
	}

	codeStr := string(code)

	// Validate code size
	if len(code) > is.config.MaxCodeSize {
		result.IsValid = false
		result.Violations = append(result.Violations, SanitizationViolation{
			Type:        "size_limit",
			Line:        0,
			Description: fmt.Sprintf("Code size exceeds limit of %d bytes", is.config.MaxCodeSize),
			Severity:    "error",
			Suggestion:  "Reduce code size",
		})
	}

	// Validate encoding
	if !is.isValidEncoding(code) {
		result.IsValid = false
		result.Violations = append(result.Violations, SanitizationViolation{
			Type:        "encoding",
			Line:        0,
			Description: "Code contains invalid UTF-8 characters",
			Severity:    "error",
			Suggestion:  "Use valid UTF-8 encoding",
		})
	}

	// Validate binary content
	if is.containsBinaryContent(codeStr) {
		result.IsValid = false
		result.Violations = append(result.Violations, SanitizationViolation{
			Type:        "binary_content",
			Line:        0,
			Description: "Code contains binary content",
			Severity:    "error",
			Suggestion:  "Remove binary content",
		})
	}

	// Validate patterns
	is.validatePatterns(codeStr, result)

	// Validate language specific patterns
	is.validateLanguageSpecific(codeStr, language, result)

	return result, nil
}

func (is *InputSanitizer) SanitizeCode(code string) string {
	// Remove null bytes
	code = strings.ReplaceAll(code, "\x00", "")

	// Remove excessive whitespace
	lines := strings.Split(code, "\n")
	sanitizedLines := []string{}

	for _, line := range lines {
		// Trim trailing whitespace
		line = strings.TrimRight(line, " \t")
		// Skip empty lines at the beginning
		if len(sanitizedLines) == 0 && strings.TrimSpace(line) == "" {
			continue
		}
		sanitizedLines = append(sanitizedLines, line)
	}

	return strings.Join(sanitizedLines, "\n")
}

func (is *InputSanitizer) validatePatterns(code string, result *SanitizationResult) {
	lines := strings.Split(code, "\n")

	for lineNum, line := range lines {
		// Check for forbidden patterns
		for _, pattern := range is.config.ForbiddenPatterns {
			if matched, _ := regexp.MatchString(pattern, line); matched {
				result.IsValid = false
				result.Violations = append(result.Violations, SanitizationViolation{
					Type:        "forbidden_pattern",
					Line:        lineNum + 1,
					Description: "Forbidden pattern detected",
					Severity:    "critical",
					Suggestion:  "Remove forbidden pattern",
				})
			}
		}

		// Check for SQL injection
		for _, pattern := range is.config.SQLInjection {
			if matched, _ := regexp.MatchString(pattern, line); matched {
				result.IsValid = false
				result.Violations = append(result.Violations, SanitizationViolation{
					Type:        "sql_injection",
					Line:        lineNum + 1,
					Description: "SQL injection pattern detected",
					Severity:    "critical",
					Suggestion:  "Avoid SQL injection patterns",
				})
			}
		}

		// Check for XSS
		for _, pattern := range is.config.XSSPatterns {
			if matched, _ := regexp.MatchString(pattern, line); matched {
				result.IsValid = false
				result.Violations = append(result.Violations, SanitizationViolation{
					Type:        "xss",
					Line:        lineNum + 1,
					Description: "XSS pattern detected",
					Severity:    "critical",
					Suggestion:  "Avoid XSS patterns",
				})
			}
		}

		// Check for command injection
		for _, pattern := range is.config.CommandInjection {
			if matched, _ := regexp.MatchString(pattern, line); matched {
				result.IsValid = false
				result.Violations = append(result.Violations, SanitizationViolation{
					Type:        "command_injection",
					Line:        lineNum + 1,
					Description: "Command injection pattern detected",
					Severity:    "critical",
					Suggestion:  "Avoid system calls",
				})
			}
		}

		// Check for path traversal
		for _, pattern := range is.config.PathTraversal {
			if matched, _ := regexp.MatchString(pattern, line); matched {
				result.IsValid = false
				result.Violations = append(result.Violations, SanitizationViolation{
					Type:        "path_traversal",
					Line:        lineNum + 1,
					Description: "Path traversal pattern detected",
					Severity:    "high",
					Suggestion:  "Avoid path traversal",
				})
			}
		}

		// Check for hardcoded secrets
		if is.containsHardcodedSecrets(line) {
			result.IsValid = false
			result.Violations = append(result.Violations, SanitizationViolation{
				Type:        "hardcoded_secret",
				Line:        lineNum + 1,
				Description: "Hardcoded secret detected",
				Severity:    "high",
				Suggestion:  "Remove hardcoded secrets",
			})
		}
	}
}

func (is *InputSanitizer) validateLanguageSpecific(code string, language string, result *SanitizationResult) {
	lines := strings.Split(code, "\n")

	for lineNum, line := range lines {
		// Check for suspicious imports
		for _, pattern := range is.config.SuspiciousImports {
			if matched, _ := regexp.MatchString(pattern, line); matched {
				result.IsValid = false
				result.Violations = append(result.Violations, SanitizationViolation{
					Type:        "suspicious_import",
					Line:        lineNum + 1,
					Description: "Suspicious import detected",
					Severity:    "medium",
					Suggestion:  "Review import usage",
				})
			}
		}
	}
}

func (is *InputSanitizer) isValidEncoding(code []byte) bool {
	return utf8.Valid(code)
}

func (is *InputSanitizer) containsBinaryContent(code string) bool {
	nonPrintable := 0
	for _, b := range code {
		if b < 32 || b > 126 {
			nonPrintable++
		}
	}
	return float64(nonPrintable)/float64(len(code)) > 0.01
}

func (is *InputSanitizer) containsHardcodedSecrets(line string) bool {
	for _, pattern := range is.config.HardcodedSecrets {
		if matched, _ := regexp.MatchString(pattern, line); matched {
			return true
		}
	}
	return false
}

func (is *InputSanitizer) GetValidationReport(result *SanitizationResult) (string, error) {
	report := map[string]interface{}{
		"is_valid":   result.IsValid,
		"violations": result.Violations,
	}

	jsonReport, err := json.MarshalIndent(report, "", "  ")
	if err != nil {
		return "", fmt.Errorf("failed to generate validation report: %w", err)
	}

	return string(jsonReport), nil
}
