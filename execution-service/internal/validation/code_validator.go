package validation

import (
	"fmt"
	"regexp"
	"strings"
	"unicode/utf8"
)

type CodeValidator struct {
	config *ValidationConfig
}

type ValidationConfig struct {
	MaxCodeSize         int64
	AllowedExtensions   []string
	BlacklistedPatterns []string
	SuspiciousPatterns  []string
	MaxLineLength       int
	MaxNestingDepth     int
	AllowedCharsets     []string
}

type ValidationResult struct {
	IsValid    bool        `json:"is_valid"`
	Violations []Violation `json:"violations"`
}

type Violation struct {
	Type        string `json:"type"`
	Line        int    `json:"line"`
	Description string `json:"description"`
	Severity    string `json:"severity"`
}

func NewCodeValidator(config *ValidationConfig) *CodeValidator {
	return &CodeValidator{
		config: config,
	}
}

func (cv *CodeValidator) ValidateCode(code []byte, filename string) *ValidationResult {
	result := &ValidationResult{
		IsValid:    true,
		Violations: []Violation{},
	}

	// Basic size validation
	if int64(len(code)) > cv.config.MaxCodeSize {
		result.IsValid = false
		result.Violations = append(result.Violations, Violation{
			Type:        "code_size_exceeded",
			Line:        0,
			Description: fmt.Sprintf("Code size %d exceeds maximum %d bytes", len(code), cv.config.MaxCodeSize),
			Severity:    "critical",
		})
	}

	// File extension validation
	if !cv.isValidExtension(filename) {
		result.IsValid = false
		result.Violations = append(result.Violations, Violation{
			Type:        "invalid_extension",
			Line:        0,
			Description: fmt.Sprintf("File extension not allowed: %s", filename),
			Severity:    "high",
		})
	}

	// Binary content detection
	if cv.containsBinaryContent(code) {
		result.IsValid = false
		result.Violations = append(result.Violations, Violation{
			Type:        "binary_content",
			Line:        0,
			Description: "Binary content detected in source code",
			Severity:    "critical",
		})
	}

	// Character encoding validation
	if !cv.isValidEncoding(code) {
		result.IsValid = false
		result.Violations = append(result.Violations, Violation{
			Type:        "invalid_encoding",
			Line:        0,
			Description: "Invalid character encoding detected",
			Severity:    "high",
		})
	}

	// Advanced pattern analysis
	codeStr := string(code)
	cv.analyzePatterns(codeStr, result)

	// Line-by-line analysis
	cv.analyzeLines(codeStr, result)

	// Language-specific validation
	cv.validateLanguageSpecific(codeStr, filename, result)

	return result
}

func (cv *CodeValidator) analyzePatterns(code string, result *ValidationResult) {
	// Check for blacklisted patterns (critical security issues)
	for _, pattern := range cv.config.BlacklistedPatterns {
		if matched, _ := regexp.MatchString(pattern, code); matched {
			result.IsValid = false
			result.Violations = append(result.Violations, Violation{
				Type:        "blacklisted_pattern",
				Line:        cv.findPatternLine(code, pattern),
				Description: fmt.Sprintf("Blacklisted pattern detected: %s", pattern),
				Severity:    "critical",
			})
		}
	}

	// Check for suspicious patterns (potential issues)
	for _, pattern := range cv.config.SuspiciousPatterns {
		if matched, _ := regexp.MatchString(pattern, code); matched {
			result.Violations = append(result.Violations, Violation{
				Type:        "suspicious_pattern",
				Line:        cv.findPatternLine(code, pattern),
				Description: fmt.Sprintf("Suspicious pattern detected: %s", pattern),
				Severity:    "medium",
			})
		}
	}
}

func (cv *CodeValidator) analyzeLines(code string, result *ValidationResult) {
	lines := strings.Split(code, "\n")

	for lineNum, line := range lines {
		// Check line length
		if len(line) > cv.config.MaxLineLength {
			result.Violations = append(result.Violations, Violation{
				Type:        "line_too_long",
				Line:        lineNum + 1,
				Description: fmt.Sprintf("Line %d exceeds maximum length %d", lineNum+1, cv.config.MaxLineLength),
				Severity:    "low",
			})
		}

		// Check for obfuscation techniques
		if cv.isObfuscated(line) {
			result.Violations = append(result.Violations, Violation{
				Type:        "obfuscation_detected",
				Line:        lineNum + 1,
				Description: "Code obfuscation techniques detected",
				Severity:    "high",
			})
		}

		// Check for hardcoded secrets
		if cv.containsHardcodedSecrets(line) {
			result.Violations = append(result.Violations, Violation{
				Type:        "hardcoded_secret",
				Line:        lineNum + 1,
				Description: "Potential hardcoded secret detected",
				Severity:    "medium",
			})
		}
	}

	// Check nesting depth
	maxDepth := cv.calculateMaxNestingDepth(code)
	if maxDepth > cv.config.MaxNestingDepth {
		result.Violations = append(result.Violations, Violation{
			Type:        "excessive_nesting",
			Line:        0,
			Description: fmt.Sprintf("Maximum nesting depth %d exceeds limit %d", maxDepth, cv.config.MaxNestingDepth),
			Severity:    "medium",
		})
	}
}

func (cv *CodeValidator) validateLanguageSpecific(code string, filename string, result *ValidationResult) {
	extension := cv.getExtension(filename)

	switch extension {
	case ".cpp", ".c", ".cc", ".cxx":
		cv.validateCPlusPlus(code, result)
	case ".java":
		cv.validateJava(code, result)
	case ".py":
		cv.validatePython(code, result)
	case ".go":
		cv.validateGo(code, result)
	case ".js", ".ts":
		cv.validateJavaScript(code, result)
	}
}

func (cv *CodeValidator) validateCPlusPlus(code string, result *ValidationResult) {
	// C++ specific validations
	dangerousPatterns := []string{
		`#include\s*<\s*sys/`,
		`#include\s*<\s*asm/`,
		`__asm__`,
		`_asm`,
		`union\s*\{.*\}\s*;`, // Potential type punning
		`reinterpret_cast`,
		`const_cast`,
		`volatile\s*\*`,
	}

	for _, pattern := range dangerousPatterns {
		if matched, _ := regexp.MatchString(pattern, code); matched {
			result.Violations = append(result.Violations, Violation{
				Type:        "cpp_dangerous_construct",
				Line:        cv.findPatternLine(code, pattern),
				Description: fmt.Sprintf("Dangerous C++ construct: %s", pattern),
				Severity:    "medium",
			})
		}
	}
}

func (cv *CodeValidator) validatePython(code string, result *ValidationResult) {
	// Python specific validations
	dangerousPatterns := []string{
		`import\s+os`,
		`import\s+subprocess`,
		`import\s+sys`,
		`from\s+os\s+import`,
		`from\s+subprocess\s+import`,
		`exec\s*\(`,
		`eval\s*\(`,
		`__import__`,
		`globals\s*\(\)`,
		`locals\s*\(\)`,
		`open\s*\(["']/`, // Absolute file paths
	}

	for _, pattern := range dangerousPatterns {
		if matched, _ := regexp.MatchString(pattern, code); matched {
			result.Violations = append(result.Violations, Violation{
				Type:        "python_dangerous_import",
				Line:        cv.findPatternLine(code, pattern),
				Description: fmt.Sprintf("Dangerous Python construct: %s", pattern),
				Severity:    "medium",
			})
		}
	}
}

func (cv *CodeValidator) validateJava(code string, result *ValidationResult) {
	// Java specific validations
	dangerousPatterns := []string{
		`Runtime\.getRuntime`,
		`ProcessBuilder`,
		`System\.exit`,
		`Class\.forName`,
		`Method\.invoke`,
		`Constructor\.newInstance`,
		`Unsafe`,
		`sun\.misc\.Unsafe`,
	}

	for _, pattern := range dangerousPatterns {
		if matched, _ := regexp.MatchString(pattern, code); matched {
			result.Violations = append(result.Violations, Violation{
				Type:        "java_dangerous_construct",
				Line:        cv.findPatternLine(code, pattern),
				Description: fmt.Sprintf("Dangerous Java construct: %s", pattern),
				Severity:    "medium",
			})
		}
	}
}

func (cv *CodeValidator) validateGo(code string, result *ValidationResult) {
	// Go specific validations
	dangerousPatterns := []string{
		`os\.Exec`,
		`exec\.Command`,
		`syscall\.`,
		`unsafe\.`,
		`reflect\.`,
		`runtime\.Breakpoint`,
		`runtime\.Goexit`,
	}

	for _, pattern := range dangerousPatterns {
		if matched, _ := regexp.MatchString(pattern, code); matched {
			result.Violations = append(result.Violations, Violation{
				Type:        "go_dangerous_construct",
				Line:        cv.findPatternLine(code, pattern),
				Description: fmt.Sprintf("Dangerous Go construct: %s", pattern),
				Severity:    "medium",
			})
		}
	}
}

func (cv *CodeValidator) validateJavaScript(code string, result *ValidationResult) {
	// JavaScript specific validations
	dangerousPatterns := []string{
		`eval\s*\(`,
		`Function\s*\(`,
		`setTimeout\s*\(`,
		`setInterval\s*\(`,
		`require\s*\(`,
		`import\s+.*\s+from`,
		`process\.`,
		`global\.`,
		`Buffer\.from`,
	}

	for _, pattern := range dangerousPatterns {
		if matched, _ := regexp.MatchString(pattern, code); matched {
			result.Violations = append(result.Violations, Violation{
				Type:        "javascript_dangerous_construct",
				Line:        cv.findPatternLine(code, pattern),
				Description: fmt.Sprintf("Dangerous JavaScript construct: %s", pattern),
				Severity:    "medium",
			})
		}
	}
}

// Helper functions
func (cv *CodeValidator) isValidExtension(filename string) bool {
	for _, ext := range cv.config.AllowedExtensions {
		if strings.HasSuffix(strings.ToLower(filename), ext) {
			return true
		}
	}
	return false
}

func (cv *CodeValidator) containsBinaryContent(code []byte) bool {
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

func (cv *CodeValidator) isValidEncoding(code []byte) bool {
	return utf8.Valid(code)
}

func (cv *CodeValidator) findPatternLine(code string, pattern string) int {
	lines := strings.Split(code, "\n")
	for i, line := range lines {
		if matched, _ := regexp.MatchString(pattern, line); matched {
			return i + 1
		}
	}
	return 0
}

func (cv *CodeValidator) isObfuscated(line string) bool {
	// Check for common obfuscation patterns
	obfuscationPatterns := []string{
		`\x[0-9a-fA-F]{2}`, // Hex escapes
		`\\[0-7]{3}`,       // Octal escapes
		`\$\{[^}]*\}`,      // Variable variables
		`base64_decode`,
		`str_rot13`,
		`eval\s*\(\s*base64`,
	}

	for _, pattern := range obfuscationPatterns {
		if matched, _ := regexp.MatchString(pattern, line); matched {
			return true
		}
	}
	return false
}

func (cv *CodeValidator) containsHardcodedSecrets(line string) bool {
	secretPatterns := []string{
		`password\s*=\s*["'][^"']+["']`,
		`secret\s*=\s*["'][^"']+["']`,
		`key\s*=\s*["'][^"']+["']`,
		`token\s*=\s*["'][^"']+["']`,
		`api_key\s*=\s*["'][^"']+["']`,
		`AKIA[0-9A-Z]{16}`,         // AWS access key pattern
		`[A-Za-z0-9+/]{32,}={0,2}`, // Base64 encoded strings
	}

	for _, pattern := range secretPatterns {
		if matched, _ := regexp.MatchString(pattern, line); matched {
			return true
		}
	}
	return false
}

func (cv *CodeValidator) calculateMaxNestingDepth(code string) int {
	maxDepth := 0
	currentDepth := 0

	for _, char := range code {
		switch char {
		case '{':
			currentDepth++
			if currentDepth > maxDepth {
				maxDepth = currentDepth
			}
		case '}':
			currentDepth--
		}
	}

	return maxDepth
}

func (cv *CodeValidator) getExtension(filename string) string {
	parts := strings.Split(filename, ".")
	if len(parts) > 1 {
		return "." + strings.ToLower(parts[len(parts)-1])
	}
	return ""
}

func (cv *CodeValidator) GetDefaultConfig() *ValidationConfig {
	return &ValidationConfig{
		MaxCodeSize:       65536, // 64KB
		AllowedExtensions: []string{".cpp", ".c", ".java", ".py", ".go", ".rs", ".js", ".ts"},
		BlacklistedPatterns: []string{
			`(?i)system\s*\(`,
			`(?i)exec\s*\(`,
			`(?i)fork\s*\(`,
			`(?i)clone\s*\(`,
			`(?i)ptrace\s*\(`,
			`(?i)socket\s*\(`,
			`(?i)connect\s*\(`,
			`(?i)bind\s*\(`,
			`(?i)listen\s*\(`,
			`(?i)accept\s*\(`,
		},
		SuspiciousPatterns: []string{
			`(?i)#include\s*<\s*net/`,
			`(?i)#include\s*<\s*arpa/`,
			`(?i)import\s+socket`,
			`(?i)import\s+thread`,
			`(?i)Runtime\.getRuntime`,
			`(?i)ProcessBuilder`,
		},
		MaxLineLength:   1000,
		MaxNestingDepth: 10,
		AllowedCharsets: []string{"utf-8", "ascii"},
	}
}
