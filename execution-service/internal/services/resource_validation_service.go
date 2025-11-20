package services

import (
	"context"
	"fmt"
	"log"

	"execution_service/internal/config"
	"execution_service/internal/httpclient"
)

type ResourceValidationService struct {
	config         *config.JudgeConfig
	contentClient  *httpclient.ContentServiceClient
	maxTimeLimit   int
	maxMemoryLimit int
	maxStackSize   int
	maxOutputSize  int
}

type ResourceLimits struct {
	TimeLimitMs   int
	MemoryLimitKb int
	StackSizeKb   int
	OutputSizeKb  int
}

type ValidationResult struct {
	IsValid    bool
	Violations []ResourceViolation
}

type ResourceViolation struct {
	Type        string `json:"type"`
	Description string `json:"description"`
	Severity    string `json:"severity"` // "warning", "error"
}

func NewResourceValidationService(cfg *config.JudgeConfig, contentClient *httpclient.ContentServiceClient) *ResourceValidationService {
	return &ResourceValidationService{
		config:         cfg,
		contentClient:  contentClient,
		maxTimeLimit:   int(cfg.MaxTimeLimit.Milliseconds()),
		maxMemoryLimit: cfg.MaxMemoryLimit,
		maxStackSize:   cfg.MaxStackSize,
		maxOutputSize:  cfg.MaxOutputSize,
	}
}

func (rvs *ResourceValidationService) ValidateAndNormalizeLimits(ctx context.Context, problemID int64, requestedTime, requestedMemory int) (*ResourceLimits, *ValidationResult) {
	result := &ValidationResult{
		IsValid:    true,
		Violations: []ResourceViolation{},
	}

	// Get problem-specific limits from content service
	problemLimits, err := rvs.getProblemLimits(ctx, problemID)
	if err != nil {
		log.Printf("Failed to get problem limits for %d: %v, using defaults", problemID, err)
		problemLimits = &ResourceLimits{
			TimeLimitMs:   int(rvs.config.DefaultTimeLimit.Milliseconds()),
			MemoryLimitKb: rvs.config.DefaultMemoryLimit,
			StackSizeKb:   rvs.config.MaxStackSize,
			OutputSizeKb:  rvs.config.MaxOutputSize,
		}
	}

	// Use problem-specific limits if available, otherwise use requested
	finalLimits := &ResourceLimits{
		TimeLimitMs:   requestedTime,
		MemoryLimitKb: requestedMemory,
		StackSizeKb:   problemLimits.StackSizeKb,
		OutputSizeKb:  problemLimits.OutputSizeKb,
	}

	// If problem has specific limits, use them
	if problemLimits.TimeLimitMs > 0 {
		finalLimits.TimeLimitMs = problemLimits.TimeLimitMs
	}
	if problemLimits.MemoryLimitKb > 0 {
		finalLimits.MemoryLimitKb = problemLimits.MemoryLimitKb
	}

	// Validate against maximum allowed limits
	if finalLimits.TimeLimitMs > rvs.maxTimeLimit {
		result.IsValid = false
		result.Violations = append(result.Violations, ResourceViolation{
			Type:        "time_limit_exceeded",
			Description: fmt.Sprintf("Time limit %dms exceeds maximum allowed %dms", finalLimits.TimeLimitMs, rvs.maxTimeLimit),
			Severity:    "error",
		})
		finalLimits.TimeLimitMs = rvs.maxTimeLimit
	}

	if finalLimits.MemoryLimitKb > rvs.maxMemoryLimit {
		result.IsValid = false
		result.Violations = append(result.Violations, ResourceViolation{
			Type:        "memory_limit_exceeded",
			Description: fmt.Sprintf("Memory limit %dKB exceeds maximum allowed %dKB", finalLimits.MemoryLimitKb, rvs.maxMemoryLimit),
			Severity:    "error",
		})
		finalLimits.MemoryLimitKb = rvs.maxMemoryLimit
	}

	// Validate minimum limits
	if finalLimits.TimeLimitMs < 100 { // 100ms minimum
		result.Violations = append(result.Violations, ResourceViolation{
			Type:        "time_limit_too_low",
			Description: fmt.Sprintf("Time limit %dms is too low, setting to minimum 100ms", finalLimits.TimeLimitMs),
			Severity:    "warning",
		})
		finalLimits.TimeLimitMs = 100
	}

	if finalLimits.MemoryLimitKb < 1024 { // 1MB minimum
		result.Violations = append(result.Violations, ResourceViolation{
			Type:        "memory_limit_too_low",
			Description: fmt.Sprintf("Memory limit %dKB is too low, setting to minimum 1MB", finalLimits.MemoryLimitKb),
			Severity:    "warning",
		})
		finalLimits.MemoryLimitKb = 1024
	}

	// Log resource limit violations
	for _, violation := range result.Violations {
		if violation.Severity == "error" {
			log.Printf("Resource validation error for problem %d: %s", problemID, violation.Description)
		} else {
			log.Printf("Resource validation warning for problem %d: %s", problemID, violation.Description)
		}
	}

	return finalLimits, result
}

func (rvs *ResourceValidationService) getProblemLimits(ctx context.Context, problemID int64) (*ResourceLimits, error) {
	// Try to get problem details from content service
	problem, err := rvs.contentClient.GetProblem(ctx, problemID)
	if err != nil {
		return nil, err
	}

	limits := &ResourceLimits{
		TimeLimitMs:   problem.TimeLimit,
		MemoryLimitKb: problem.MemoryLimit,
		StackSizeKb:   rvs.maxStackSize,
		OutputSizeKb:  rvs.maxOutputSize,
	}

	return limits, nil
}

func (rvs *ResourceValidationService) LogResourceUsage(submissionID int64, limits *ResourceLimits, actualTime, actualMemory int) {
	// Check for resource limit violations
	violations := []ResourceViolation{}

	if actualTime > limits.TimeLimitMs {
		violations = append(violations, ResourceViolation{
			Type:        "time_limit_violation",
			Description: fmt.Sprintf("Execution time %dms exceeded limit %dms", actualTime, limits.TimeLimitMs),
			Severity:    "error",
		})
	}

	if actualMemory > limits.MemoryLimitKb {
		violations = append(violations, ResourceViolation{
			Type:        "memory_limit_violation",
			Description: fmt.Sprintf("Memory usage %dKB exceeded limit %dKB", actualMemory, limits.MemoryLimitKb),
			Severity:    "error",
		})
	}

	// Log violations
	for _, violation := range violations {
		log.Printf("Resource violation for submission %d: %s", submissionID, violation.Description)
	}

	// Log resource usage statistics
	utilizationTime := float64(actualTime) / float64(limits.TimeLimitMs) * 100
	utilizationMemory := float64(actualMemory) / float64(limits.MemoryLimitKb) * 100

	log.Printf("Resource usage for submission %d: Time=%dms (%.1f%%), Memory=%dKB (%.1f%%)",
		submissionID, actualTime, utilizationTime, actualMemory, utilizationMemory)
}

func (rvs *ResourceValidationService) GetMaxLimits() *ResourceLimits {
	return &ResourceLimits{
		TimeLimitMs:   rvs.maxTimeLimit,
		MemoryLimitKb: rvs.maxMemoryLimit,
		StackSizeKb:   rvs.maxStackSize,
		OutputSizeKb:  rvs.maxOutputSize,
	}
}

func (rvs *ResourceValidationService) GetDefaultLimits() *ResourceLimits {
	return &ResourceLimits{
		TimeLimitMs:   int(rvs.config.DefaultTimeLimit.Milliseconds()),
		MemoryLimitKb: rvs.config.DefaultMemoryLimit,
		StackSizeKb:   rvs.maxStackSize,
		OutputSizeKb:  rvs.maxOutputSize,
	}
}
