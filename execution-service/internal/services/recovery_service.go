package services

import (
	"context"
	"fmt"
	"log"
	"sync"
	"time"

	"execution_service/internal/database"
	"execution_service/internal/models"
	"execution_service/internal/sandbox"
)

type RecoveryService struct {
	db               *database.DB
	sandbox          *sandbox.IsolateSandbox
	recoveryInterval time.Duration
	maxRetries       int
	recoveryTimeout  time.Duration
	isRunning        bool
	stopChan         chan struct{}
	wg               sync.WaitGroup
}

type RecoveryTask struct {
	Type      string
	TargetID  int64
	Retries   int
	LastRetry time.Time
	Data      map[string]interface{}
}

type RecoveryResult struct {
	Success bool
	Error   error
	Message string
}

func NewRecoveryService(db *database.DB, sandbox *sandbox.IsolateSandbox) *RecoveryService {
	return &RecoveryService{
		db:               db,
		sandbox:          sandbox,
		recoveryInterval: 30 * time.Second,
		maxRetries:       3,
		recoveryTimeout:  60 * time.Second,
		stopChan:         make(chan struct{}),
	}
}

func (rs *RecoveryService) Start(ctx context.Context) error {
	if rs.isRunning {
		return fmt.Errorf("recovery service is already running")
	}

	rs.isRunning = true
	log.Println("Starting automatic recovery service")

	rs.wg.Add(1)
	go rs.recoveryLoop(ctx)

	return nil
}

func (rs *RecoveryService) Stop() {
	if !rs.isRunning {
		return
	}

	log.Println("Stopping automatic recovery service")
	close(rs.stopChan)
	rs.wg.Wait()
	rs.isRunning = false
}

func (rs *RecoveryService) recoveryLoop(ctx context.Context) {
	defer rs.wg.Done()

	ticker := time.NewTicker(rs.recoveryInterval)
	defer ticker.Stop()

	for {
		select {
		case <-ctx.Done():
			return
		case <-rs.stopChan:
			return
		case <-ticker.C:
			rs.performRecoveryTasks(ctx)
		}
	}
}

func (rs *RecoveryService) performRecoveryTasks(ctx context.Context) {
	// Recover failed workers
	rs.recoverFailedWorkers(ctx)

	// Clean up orphaned Isolate boxes
	rs.cleanupOrphanedBoxes(ctx)

	// Recover stuck submissions
	rs.recoverStuckSubmissions(ctx)
}

func (rs *RecoveryService) recoverFailedWorkers(ctx context.Context) {
	// Get workers that have been unhealthy for too long
	workers, err := rs.db.GetUnhealthyWorkers(ctx, 5*time.Minute)
	if err != nil {
		log.Printf("Failed to get unhealthy workers: %v", err)
		return
	}

	for _, worker := range workers {
		log.Printf("Attempting to recover worker %s (ID: %d)", worker.WorkerName, worker.ID)

		result := rs.recoverWorker(ctx, worker)
		if result.Success {
			log.Printf("Successfully recovered worker %s", worker.WorkerName)
		} else {
			log.Printf("Failed to recover worker %s: %v", worker.WorkerName, result.Error)
		}
	}
}

func (rs *RecoveryService) recoverWorker(ctx context.Context, worker models.JudgeWorker) *RecoveryResult {
	// Mark worker as recovering
	err := rs.db.UpdateWorkerStatus(ctx, int(worker.ID), "recovering", nil)
	if err != nil {
		return &RecoveryResult{
			Success: false,
			Error:   fmt.Errorf("failed to update worker status: %w", err),
		}
	}

	// Reset worker state in database
	err = rs.db.ResetWorkerState(ctx, int(worker.ID))
	if err != nil {
		return &RecoveryResult{
			Success: false,
			Error:   fmt.Errorf("failed to reset worker state: %w", err),
		}
	}

	// Update worker back to idle
	err = rs.db.UpdateWorkerStatus(ctx, int(worker.ID), "idle", nil)
	if err != nil {
		return &RecoveryResult{
			Success: false,
			Error:   fmt.Errorf("failed to update worker status to idle: %w", err),
		}
	}

	return &RecoveryResult{
		Success: true,
		Message: fmt.Sprintf("Worker %s recovered successfully", worker.WorkerName),
	}
}

func (rs *RecoveryService) cleanupOrphanedBoxes(ctx context.Context) {
	// Get all active boxes from database
	activeBoxes, err := rs.db.GetActiveBoxes(ctx)
	if err != nil {
		log.Printf("Failed to get active boxes: %v", err)
		return
	}

	for _, boxID := range activeBoxes {
		// Check if box is actually in use by checking corresponding worker
		isInUse, err := rs.db.IsBoxInUse(ctx, boxID)
		if err != nil {
			log.Printf("Failed to check if box %d is in use: %v", boxID, err)
			continue
		}

		if !isInUse {
			log.Printf("Cleaning up orphaned box %d", boxID)
			rs.sandbox.CleanupBox(boxID)
			rs.db.ReleaseBox(ctx, boxID)
		}
	}
}

func (rs *RecoveryService) recoverStuckSubmissions(ctx context.Context) {
	// Get submissions that have been in judging state for too long
	stuckSubmissions, err := rs.db.GetStuckSubmissions(ctx, 10*time.Minute)
	if err != nil {
		log.Printf("Failed to get stuck submissions: %v", err)
		return
	}

	for _, submission := range stuckSubmissions {
		log.Printf("Recovering stuck submission %d", submission.ID)

		result := rs.recoverSubmission(ctx, submission)
		if result.Success {
			log.Printf("Successfully recovered submission %d", submission.ID)
		} else {
			log.Printf("Failed to recover submission %d: %v", submission.ID, result.Error)
		}
	}
}

func (rs *RecoveryService) recoverSubmission(ctx context.Context, submission models.Submission) *RecoveryResult {
	// Reset submission to pending state
	err := rs.db.ResetSubmissionState(ctx, submission.ID)
	if err != nil {
		return &RecoveryResult{
			Success: false,
			Error:   fmt.Errorf("failed to reset submission state: %w", err),
		}
	}

	// Clear any associated execution logs
	err = rs.db.ClearExecutionLogs(ctx, submission.ID)
	if err != nil {
		log.Printf("Warning: failed to clear execution logs for submission %d: %v", submission.ID, err)
	}

	return &RecoveryResult{
		Success: true,
		Message: fmt.Sprintf("Submission %d recovered and reset to pending", submission.ID),
	}
}

// Health check for Isolate sandbox
func (rs *RecoveryService) CheckSandboxHealth(ctx context.Context) *RecoveryResult {
	// Try to create and cleanup a test box
	boxID, err := rs.sandbox.CreateBox()
	if err != nil {
		return &RecoveryResult{
			Success: false,
			Error:   fmt.Errorf("sandbox health check failed: %w", err),
		}
	}

	rs.sandbox.CleanupBox(boxID)

	return &RecoveryResult{
		Success: true,
		Message: "Sandbox is healthy",
	}
}

// Manual recovery trigger for specific worker
func (rs *RecoveryService) RecoverWorker(ctx context.Context, workerID int) *RecoveryResult {
	worker, err := rs.db.GetWorker(ctx, workerID)
	if err != nil {
		return &RecoveryResult{
			Success: false,
			Error:   fmt.Errorf("worker not found: %w", err),
		}
	}

	return rs.recoverWorker(ctx, *worker)
}

// Manual recovery trigger for specific submission
func (rs *RecoveryService) RecoverSubmission(ctx context.Context, submissionID int64) *RecoveryResult {
	submission, err := rs.db.GetSubmission(ctx, submissionID)
	if err != nil {
		return &RecoveryResult{
			Success: false,
			Error:   fmt.Errorf("submission not found: %w", err),
		}
	}

	return rs.recoverSubmission(ctx, *submission)
}

// Get recovery statistics
func (rs *RecoveryService) GetRecoveryStats(ctx context.Context) (map[string]interface{}, error) {
	stats := make(map[string]interface{})

	// Get worker health stats
	healthyWorkers, err := rs.db.GetWorkerStats(ctx)
	if err != nil {
		return nil, fmt.Errorf("failed to get worker stats: %w", err)
	}
	stats["workers"] = healthyWorkers

	// Get submission stats
	submissionStats, err := rs.db.GetSubmissionStats(ctx)
	if err != nil {
		return nil, fmt.Errorf("failed to get submission stats: %w", err)
	}
	stats["submissions"] = submissionStats

	// Get sandbox health
	sandboxHealth := rs.CheckSandboxHealth(ctx)
	stats["sandbox_health"] = sandboxHealth

	stats["recovery_service_running"] = rs.isRunning

	return stats, nil
}
