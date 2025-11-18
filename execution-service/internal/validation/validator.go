package validation

import (
	"fmt"
	"regexp"
	"strconv"
	"strings"

	"execution_service/internal/models"
)

var (
	languageRegex = regexp.MustCompile(`^[a-z]+$`)
	idRegex       = regexp.MustCompile(`^\d+$`)
)

func ValidateJudgeRequest(req *models.JudgeRequest) error {
	if req.SubmissionID <= 0 {
		return fmt.Errorf("invalid submission ID")
	}

	if req.UserID <= 0 {
		return fmt.Errorf("invalid user ID")
	}

	if req.ProblemID <= 0 {
		return fmt.Errorf("invalid problem ID")
	}

	if !languageRegex.MatchString(req.Language) {
		return fmt.Errorf("invalid language")
	}

	if req.CodeURL == "" {
		return fmt.Errorf("code URL is required")
	}

	if req.TimeLimitMs <= 0 || req.TimeLimitMs > 30000 {
		return fmt.Errorf("time limit must be between 1 and 30000 ms")
	}

	if req.MemoryLimitKb <= 0 || req.MemoryLimitKb > 524288 {
		return fmt.Errorf("memory limit must be between 1 and 524288 KB")
	}

	if req.Priority < 0 || req.Priority > 10 {
		return fmt.Errorf("priority must be between 0 and 10")
	}

	return nil
}

func ValidateSubmissionID(idStr string) (int64, error) {
	if !idRegex.MatchString(idStr) {
		return 0, fmt.Errorf("invalid submission ID format")
	}

	id, err := strconv.ParseInt(idStr, 10, 64)
	if err != nil {
		return 0, fmt.Errorf("invalid submission ID")
	}

	if id <= 0 {
		return 0, fmt.Errorf("submission ID must be positive")
	}

	return id, nil
}

func ValidateUserID(idStr string) (int64, error) {
	if !idRegex.MatchString(idStr) {
		return 0, fmt.Errorf("invalid user ID format")
	}

	id, err := strconv.ParseInt(idStr, 10, 64)
	if err != nil {
		return 0, fmt.Errorf("invalid user ID")
	}

	if id <= 0 {
		return 0, fmt.Errorf("user ID must be positive")
	}

	return id, nil
}

func ValidateProblemID(idStr string) (int64, error) {
	if !idRegex.MatchString(idStr) {
		return 0, fmt.Errorf("invalid problem ID format")
	}

	id, err := strconv.ParseInt(idStr, 10, 64)
	if err != nil {
		return 0, fmt.Errorf("invalid problem ID")
	}

	if id <= 0 {
		return 0, fmt.Errorf("problem ID must be positive")
	}

	return id, nil
}

func ValidateLanguage(code string) error {
	if !languageRegex.MatchString(code) {
		return fmt.Errorf("invalid language format")
	}

	supportedLanguages := map[string]bool{
		"cpp":    true,
		"c":      true,
		"java":   true,
		"python": true,
		"go":     true,
	}

	if !supportedLanguages[code] {
		return fmt.Errorf("unsupported language: %s", code)
	}

	return nil
}

func ValidatePagination(limitStr, offsetStr string) (int, int, error) {
	limit := 20
	offset := 0

	if limitStr != "" {
		l, err := strconv.Atoi(limitStr)
		if err != nil || l <= 0 || l > 100 {
			return 0, 0, fmt.Errorf("limit must be between 1 and 100")
		}
		limit = l
	}

	if offsetStr != "" {
		o, err := strconv.Atoi(offsetStr)
		if err != nil || o < 0 {
			return 0, 0, fmt.Errorf("offset must be non-negative")
		}
		offset = o
	}

	return limit, offset, nil
}

func SanitizeString(input string) string {
	input = strings.TrimSpace(input)
	input = strings.ReplaceAll(input, "\x00", "")
	input = strings.ReplaceAll(input, "\r", "")
	input = strings.ReplaceAll(input, "\n", " ")
	input = strings.ReplaceAll(input, "\t", " ")

	for strings.Contains(input, "  ") {
		input = strings.ReplaceAll(input, "  ", " ")
	}

	return input
}

func ValidateCode(code []byte, language string) error {
	maxCodeSize := 65536

	if len(code) > maxCodeSize {
		return fmt.Errorf("code size exceeds maximum allowed size of %d bytes", maxCodeSize)
	}

	if len(code) == 0 {
		return fmt.Errorf("code cannot be empty")
	}

	codeStr := string(code)
	if strings.Contains(codeStr, "system(") || strings.Contains(codeStr, "exec(") {
		return fmt.Errorf("code contains potentially dangerous system calls")
	}

	return nil
}
