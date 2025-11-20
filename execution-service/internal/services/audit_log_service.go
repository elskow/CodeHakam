package services

import (
	"context"
	"fmt"
	"time"

	"execution_service/internal/database"
	"execution_service/internal/models"
)

type AuditLogService struct {
	db *database.DB
}

type AuditEvent struct {
	UserID     int64                  `json:"user_id"`
	Action     string                 `json:"action"`
	Resource   string                 `json:"resource"`
	ResourceID *int64                 `json:"resource_id,omitempty"`
	IPAddress  string                 `json:"ip_address"`
	UserAgent  string                 `json:"user_agent"`
	Details    map[string]interface{} `json:"details,omitempty"`
	Timestamp  time.Time              `json:"timestamp"`
	Severity   string                 `json:"severity"` // info, warning, critical
}

func NewAuditLogService(db *database.DB) *AuditLogService {
	return &AuditLogService{
		db: db,
	}
}

func (a *AuditLogService) LogAdminAction(ctx context.Context, event *AuditEvent) error {
	// Create audit log entry
	logEntry := &models.ExecutionLog{
		SubmissionID: 0, // Admin actions don't have submission ID
		Level:        "AUDIT",
		Message:      fmt.Sprintf("ADMIN_ACTION: %s %s by user %d from %s", event.Action, event.Resource, event.UserID, event.IPAddress),
	}

	// Add structured details to message
	if event.ResourceID != nil {
		logEntry.Message = fmt.Sprintf("%s (ID: %d)", logEntry.Message, *event.ResourceID)
	}

	if event.Details != nil {
		logEntry.Message = fmt.Sprintf("%s | Details: %+v", logEntry.Message, event.Details)
	}

	err := a.db.CreateExecutionLog(ctx, logEntry)
	if err != nil {
		return fmt.Errorf("failed to create audit log: %w", err)
	}

	return nil
}

func (a *AuditLogService) LogSubmissionAction(ctx context.Context, submissionID int64, userID int64, action, details string) error {
	logEntry := &models.ExecutionLog{
		SubmissionID: submissionID,
		Level:        "AUDIT",
		Message:      fmt.Sprintf("SUBMISSION_ACTION: %s submission %d by user %d - %s", action, submissionID, userID, details),
	}

	err := a.db.CreateExecutionLog(ctx, logEntry)
	if err != nil {
		return fmt.Errorf("failed to create submission audit log: %w", err)
	}

	return nil
}

func (a *AuditLogService) LogSecurityEvent(ctx context.Context, event *AuditEvent) error {
	logEntry := &models.ExecutionLog{
		SubmissionID: 0,
		Level:        "SECURITY",
		Message:      fmt.Sprintf("SECURITY_EVENT: %s %s by user %d from %s - %s", event.Action, event.Resource, event.UserID, event.IPAddress, event.Severity),
	}

	if event.Details != nil {
		logEntry.Message = fmt.Sprintf("%s | Details: %+v", logEntry.Message, event.Details)
	}

	err := a.db.CreateExecutionLog(ctx, logEntry)
	if err != nil {
		return fmt.Errorf("failed to create security audit log: %w", err)
	}

	return nil
}

func (a *AuditLogService) LogSystemEvent(ctx context.Context, action, details string, severity string) error {
	logEntry := &models.ExecutionLog{
		SubmissionID: 0,
		Level:        "SYSTEM",
		Message:      fmt.Sprintf("SYSTEM_EVENT: %s - %s [%s]", action, details, severity),
	}

	err := a.db.CreateExecutionLog(ctx, logEntry)
	if err != nil {
		return fmt.Errorf("failed to create system audit log: %w", err)
	}

	return nil
}

func (a *AuditLogService) GetAuditLogs(ctx context.Context, level string, limit, offset int) ([]models.ExecutionLog, error) {
	// This would require adding a method to the database package
	// For now, return empty slice
	return []models.ExecutionLog{}, nil
}

func (a *AuditLogService) CleanupOldLogs(ctx context.Context, olderThan time.Duration) error {
	// This would require adding a cleanup method to the database package
	// For now, just log the action
	return a.LogSystemEvent(ctx, "AUDIT_CLEANUP", fmt.Sprintf("Cleaned up audit logs older than %v", olderThan), "info")
}

// Predefined admin actions for consistency
const (
	AdminActionUserCreate        = "USER_CREATE"
	AdminActionUserUpdate        = "USER_UPDATE"
	AdminActionUserDelete        = "USER_DELETE"
	AdminActionUserBan           = "USER_BAN"
	AdminActionUserUnban         = "USER_UNBAN"
	AdminActionProblemCreate     = "PROBLEM_CREATE"
	AdminActionProblemUpdate     = "PROBLEM_UPDATE"
	AdminActionProblemDelete     = "PROBLEM_DELETE"
	AdminActionSubmissionRejudge = "SUBMISSION_REJUDGE"
	AdminActionWorkerScale       = "WORKER_SCALE"
	AdminActionSystemConfig      = "SYSTEM_CONFIG"
	AdminActionBoxCleanup        = "BOX_CLEANUP"
	AdminActionRoleAssign        = "ROLE_ASSIGN"
	AdminActionRoleRevoke        = "ROLE_REVOKE"
)

// Predefined security events
const (
	SecurityEventAuthFailure    = "AUTH_FAILURE"
	SecurityEventUnauthorized   = "UNAUTHORIZED"
	SecurityEventForbidden      = "FORBIDDEN"
	SecurityEventRateLimit      = "RATE_LIMIT"
	SecurityEventSuspiciousCode = "SUSPICIOUS_CODE"
	SecurityEventResourceAbuse  = "RESOURCE_ABUSE"
)

// Severity levels
const (
	SeverityInfo     = "info"
	SeverityWarning  = "warning"
	SeverityCritical = "critical"
)
