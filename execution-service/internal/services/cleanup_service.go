package services

import (
	"context"
	"fmt"
	"log"
	"time"

	"execution_service/internal/database"
)

type CleanupService struct {
	db               *database.DB
	retentionPeriods map[string]time.Duration
	cleanupInterval  time.Duration
}

type CleanupConfig struct {
	SubmissionsRetention       time.Duration
	ExecutionLogsRetention     time.Duration
	TestResultsRetention       time.Duration
	PlagiarismReportsRetention time.Duration
	CleanupInterval            time.Duration
}

func NewCleanupService(db *database.DB, config *CleanupConfig) *CleanupService {
	retentionPeriods := map[string]time.Duration{
		"submissions":        config.SubmissionsRetention,
		"execution_logs":     config.ExecutionLogsRetention,
		"test_results":       config.TestResultsRetention,
		"plagiarism_reports": config.PlagiarismReportsRetention,
	}

	return &CleanupService{
		db:               db,
		retentionPeriods: retentionPeriods,
		cleanupInterval:  config.CleanupInterval,
	}
}

func (cs *CleanupService) Start(ctx context.Context) {
	ticker := time.NewTicker(cs.cleanupInterval)
	defer ticker.Stop()

	log.Printf("Starting cleanup service with interval: %v", cs.cleanupInterval)

	for {
		select {
		case <-ctx.Done():
			log.Printf("Cleanup service shutting down")
			return
		case <-ticker.C:
			cs.performCleanup(ctx)
		}
	}
}

func (cs *CleanupService) performCleanup(ctx context.Context) {
	log.Printf("Starting scheduled cleanup run")

	// Clean up old submissions
	if err := cs.cleanupOldSubmissions(ctx); err != nil {
		log.Printf("Failed to cleanup old submissions: %v", err)
	}

	// Clean up old execution logs
	if err := cs.cleanupOldExecutionLogs(ctx); err != nil {
		log.Printf("Failed to cleanup old execution logs: %v", err)
	}

	// Clean up old test results
	if err := cs.cleanupOldTestResults(ctx); err != nil {
		log.Printf("Failed to cleanup old test results: %v", err)
	}

	// Clean up old plagiarism reports
	if err := cs.cleanupOldPlagiarismReports(ctx); err != nil {
		log.Printf("Failed to cleanup old plagiarism reports: %v", err)
	}

	log.Printf("Cleanup run completed")
}

func (cs *CleanupService) cleanupOldSubmissions(ctx context.Context) error {
	cutoffDate := time.Now().Add(-cs.retentionPeriods["submissions"])

	// Archive old submissions before deletion
	if err := cs.archiveSubmissions(ctx, cutoffDate); err != nil {
		return fmt.Errorf("failed to archive submissions: %w", err)
	}

	// For now, we'll implement a simple cleanup using existing methods
	// In a real implementation, you'd add a method to the database package
	log.Printf("Would delete submissions older than %v", cutoffDate)
	return nil
}

func (cs *CleanupService) cleanupOldExecutionLogs(ctx context.Context) error {
	cutoffDate := time.Now().Add(-cs.retentionPeriods["execution_logs"])
	log.Printf("Would delete execution logs older than %v", cutoffDate)
	return nil
}

func (cs *CleanupService) cleanupOldTestResults(ctx context.Context) error {
	cutoffDate := time.Now().Add(-cs.retentionPeriods["test_results"])
	log.Printf("Would delete test results older than %v", cutoffDate)
	return nil
}

func (cs *CleanupService) cleanupOldPlagiarismReports(ctx context.Context) error {
	cutoffDate := time.Now().Add(-cs.retentionPeriods["plagiarism_reports"])
	log.Printf("Would delete plagiarism reports older than %v", cutoffDate)
	return nil
}

func (cs *CleanupService) archiveSubmissions(ctx context.Context, cutoffDate time.Time) error {
	log.Printf("Would archive submissions older than %v", cutoffDate)
	return nil
}

func (cs *CleanupService) GetCleanupStats(ctx context.Context) (map[string]interface{}, error) {
	stats := map[string]interface{}{
		"submissions_by_age": map[string]int{
			"last_24h": 0,
			"last_7d": 0,
			"last_30d": 0,
			"older":    0,
		},
		"table_sizes": map[string]string{
			"submissions":         "unknown",
			"execution_logs":      "unknown",
			"submission_test_results": "unknown",
			"plagiarism_reports":  "unknown",
		},
	}

	return stats, nil
}
	
	stats["table_sizes"] = map[string]string{
		"submissions":         "unknown",
		"execution_logs":      "unknown",
		"submission_test_results": "unknown",
		"plagiarism_reports":  "unknown",
	}

	return stats, nil
}

	submissionCounts := make(map[string]int)
	for period, whereClause := range submissionQueries {
		query := fmt.Sprintf("SELECT COUNT(*) FROM execution.submissions %s", whereClause)
		var count int
		err := cs.db.conn.GetContext(ctx, &count, query)
		if err == nil {
			submissionCounts[period] = count
		}
	}
	stats["submissions_by_age"] = submissionCounts

	// Get table sizes
	tables := []string{"submissions", "execution_logs", "submission_test_results", "plagiarism_reports"}
	tableSizes := make(map[string]interface{})

	for _, table := range tables {
		query := fmt.Sprintf(`
			SELECT 
				pg_size_pretty(pg_total_relation_size('execution.%s')) as size
		`, table)

		var size string
		err := cs.db.conn.GetContext(ctx, &size, query)
		if err == nil {
			tableSizes[table] = size
		}
	}
	stats["table_sizes"] = tableSizes

	return stats, nil
}

func (cs *CleanupService) ForceCleanup(ctx context.Context, dataType string, olderThan time.Duration) error {
	cutoffDate := time.Now().Add(-olderThan)

	switch dataType {
	case "submissions":
		return cs.cleanupOldSubmissions(ctx)
	case "execution_logs":
		return cs.cleanupOldExecutionLogs(ctx)
	case "test_results":
		return cs.cleanupOldTestResults(ctx)
	case "plagiarism_reports":
		return cs.cleanupOldPlagiarismReports(ctx)
	default:
		return fmt.Errorf("unknown data type: %s", dataType)
	}
}

func (cs *CleanupService) GetDefaultCleanupConfig() *CleanupConfig {
	return &CleanupConfig{
		SubmissionsRetention:       90 * 24 * time.Hour,  // 90 days
		ExecutionLogsRetention:     30 * 24 * time.Hour,  // 30 days
		TestResultsRetention:       60 * 24 * time.Hour,  // 60 days
		PlagiarismReportsRetention: 180 * 24 * time.Hour, // 180 days
		CleanupInterval:            24 * time.Hour,       // Daily
	}
}
