package worker

import (
	"context"
	"fmt"
	"log"
	"strings"
	"sync"
	"time"

	"execution_service/internal/checker"
	"execution_service/internal/database"
	"execution_service/internal/httpclient"
	"execution_service/internal/models"
	"execution_service/internal/queue"
	"execution_service/internal/sandbox"
	"execution_service/internal/services"
	"execution_service/internal/storage"
	"execution_service/internal/validation"

	amqp "github.com/rabbitmq/amqp091-go"
)

type JudgeWorker struct {
	id                  int
	db                  *database.DB
	queue               *queue.RabbitMQClient
	storage             *storage.MinIOClient
	sandbox             *sandbox.IsolateSandbox
	validator           *validation.CodeValidator
	customChecker       *checker.CustomChecker
	resourceValidator   *services.ResourceValidationService
	circuitBreaker      *services.CircuitBreakerService
	plagiarismEnqueuer  func(submissionID, userID, problemID int64, language, codeURL string)
	currentJob          *models.JudgeRequest
	isProcessing        bool
	workerID            int64
	lastHeartbeat       time.Time
	failureCount        int
	maxFailures         int
	healthCheckInterval time.Duration
	recoveryInterval    time.Duration
	isHealthy           bool
	mutex               sync.RWMutex
}

type JudgePool struct {
	workers             []*JudgeWorker
	db                  *database.DB
	queue               *queue.RabbitMQClient
	storage             *storage.MinIOClient
	sandbox             *sandbox.IsolateSandbox
	customChecker       *checker.CustomChecker
	workerCount         int
	minWorkers          int
	maxWorkers          int
	heartbeatInterval   time.Duration
	healthCheckInterval time.Duration
	autoScaleInterval   time.Duration
	maxWorkerFailures   int
	shutdownTimeout     time.Duration
	isRunning           bool
	autoScalingEnabled  bool
	mutex               sync.RWMutex
}

func NewJudgePool(workerCount int, db *database.DB, q *queue.RabbitMQClient, s *storage.MinIOClient, sb *sandbox.IsolateSandbox, resourceValidator *services.ResourceValidationService) *JudgePool {
	// Initialize advanced code validator
	validatorConfig := validation.NewCodeValidator(&validation.ValidationConfig{}).GetDefaultConfig()
	validator := validation.NewCodeValidator(validatorConfig)

	// Initialize custom checker
	checkerConfig := checker.NewCustomChecker(nil, nil, nil).GetDefaultConfig()
	customChecker := checker.NewCustomChecker(sb, s, checkerConfig)

	workers := make([]*JudgeWorker, workerCount)
	for i := 0; i < workerCount; i++ {
		worker := &JudgeWorker{
			id:                  i + 1,
			db:                  db,
			queue:               q,
			storage:             s,
			sandbox:             sb,
			validator:           validator,
			customChecker:       customChecker,
			resourceValidator:   resourceValidator,
			circuitBreaker:      services.NewCircuitBreakerService(),
			maxFailures:         3,
			healthCheckInterval: 30 * time.Second,
			recoveryInterval:    60 * time.Second,
			isHealthy:           true,
			lastHeartbeat:       time.Now(),
		}

		workerModel := &models.JudgeWorker{
			WorkerName: fmt.Sprintf("judge-worker-%d", i+1),
			Status:     "idle",
		}

		if err := db.CreateJudgeWorker(context.Background(), workerModel); err != nil {
			log.Printf("Failed to create worker record: %v", err)
			worker.workerID = int64(i + 1)
		} else {
			worker.workerID = int64(workerModel.ID)
		}

		workers[i] = worker
	}

	return &JudgePool{
		workers:             workers,
		db:                  db,
		queue:               q,
		storage:             s,
		sandbox:             sb,
		customChecker:       customChecker,
		workerCount:         workerCount,
		minWorkers:          2,
		maxWorkers:          20,
		heartbeatInterval:   15 * time.Second,
		healthCheckInterval: 30 * time.Second,
		autoScaleInterval:   30 * time.Second,
		maxWorkerFailures:   3,
		shutdownTimeout:     30 * time.Second,
		autoScalingEnabled:  true,
	}
}

func (jp *JudgePool) Start(ctx context.Context) error {
	jp.mutex.Lock()
	if jp.isRunning {
		jp.mutex.Unlock()
		return fmt.Errorf("judge pool is already running")
	}
	jp.isRunning = true
	jp.mutex.Unlock()

	log.Printf("Starting judge pool with %d workers", jp.workerCount)

	// Start worker health monitoring
	go jp.healthMonitor(ctx)

	// Start heartbeat reporter
	go jp.heartbeatReporter(ctx)

	// Start auto-scaling if enabled
	if jp.autoScalingEnabled {
		go jp.autoScaler(ctx)
	}

	// Start all workers
	for _, worker := range jp.workers {
		go worker.start(ctx)
	}

	return nil
}

func (jw *JudgeWorker) start(ctx context.Context) {
	log.Printf("Judge worker %d started", jw.id)

	// Start heartbeat goroutine
	heartbeatCtx, cancelHeartbeat := context.WithCancel(ctx)
	defer cancelHeartbeat()
	go jw.heartbeatLoop(heartbeatCtx)

	msgs, err := jw.queue.ConsumeSubmissions(ctx)
	if err != nil {
		log.Printf("Worker %d failed to start consuming: %v", jw.id, err)
		jw.markUnhealthy()
		return
	}

	for {
		select {
		case <-ctx.Done():
			log.Printf("Worker %d shutting down", jw.id)
			return
		case msg := <-msgs:
			jw.mutex.RLock()
			isProcessing := jw.isProcessing
			isHealthy := jw.isHealthy
			jw.mutex.RUnlock()

			if isProcessing || !isHealthy {
				log.Printf("Worker %d is busy or unhealthy, rejecting message", jw.id)
				jw.queue.RejectMessage(msg, true)
				continue
			}

			jw.processMessage(ctx, msg)
		}
	}
}

func (jw *JudgeWorker) processMessage(ctx context.Context, msg amqp.Delivery) {
	jw.mutex.Lock()
	jw.isProcessing = true
	jw.mutex.Unlock()

	defer func() {
		jw.mutex.Lock()
		jw.isProcessing = false
		jw.currentJob = nil
		jw.mutex.Unlock()

		if jw.workerID > 0 {
			jw.db.UpdateWorkerStatus(ctx, int(jw.workerID), "idle", nil)
		}

		// Update heartbeat after processing
		jw.updateHeartbeat()
	}()

	request, err := queue.ParseJudgeRequest(msg)
	if err != nil {
		log.Printf("Worker %d failed to parse message: %v", jw.id, err)
		jw.markUnhealthy()
		jw.queue.RejectMessage(msg, false)
		return
	}

	jw.currentJob = request
	if jw.workerID > 0 {
		jw.db.UpdateWorkerStatus(ctx, int(jw.workerID), "busy", &request.SubmissionID)
	}
	log.Printf("Worker %d processing submission %d", jw.id, request.SubmissionID)

	err = jw.processSubmission(ctx, request)
	if err != nil {
		log.Printf("Worker %d failed to process submission %d: %v", jw.id, request.SubmissionID, err)
		jw.logError(request.SubmissionID, fmt.Sprintf("Processing failed: %v", err))
		jw.queue.RejectMessage(msg, true)
		return
	}

	jw.queue.AcknowledgeMessage(msg)
	log.Printf("Worker %d completed submission %d", jw.id, request.SubmissionID)
}

func (jw *JudgeWorker) processSubmission(ctx context.Context, request *models.JudgeRequest) error {
	// Use circuit breaker for storage operations
	var code []byte
	_, err := jw.circuitBreaker.Execute("minio", func() (interface{}, error) {
		downloadedCode, downloadErr := jw.storage.DownloadCode(ctx, request.CodeURL)
		code = downloadedCode
		return nil, downloadErr
	})
	if err != nil {
		return fmt.Errorf("failed to download code (circuit breaker open): %w", err)
	}

	jw.logInfo(request.SubmissionID, "Starting advanced code validation")

	// Advanced code validation
	validationResult := jw.validator.ValidateCode(code, "code."+request.Language)
	if !validationResult.IsValid {
		errorMsg := "Code validation failed: "
		for _, violation := range validationResult.Violations {
			if violation.Severity == "critical" {
				errorMsg += fmt.Sprintf("[%s] %s", violation.Type, violation.Description)
				break
			}
		}

		err := jw.db.UpdateSubmissionCompilationError(ctx, request.SubmissionID, errorMsg)
		if err != nil {
			return fmt.Errorf("failed to update compilation error: %w", err)
		}
		return fmt.Errorf("code validation failed: %s", errorMsg)
	}

	// Log non-critical violations
	for _, violation := range validationResult.Violations {
		if violation.Severity != "critical" {
			jw.logInfo(request.SubmissionID, fmt.Sprintf("Security warning: [%s] %s at line %d",
				violation.Type, violation.Description, violation.Line))
		}
	}

	jw.logInfo(request.SubmissionID, "Starting compilation")

	// Use separate compilation time limit (30 seconds max)
	compileTimeLimit := time.Duration(30) * time.Second
	if time.Duration(request.TimeLimitMs)*time.Millisecond < compileTimeLimit {
		compileTimeLimit = time.Duration(request.TimeLimitMs) * time.Millisecond
	}

	compileResult, err := jw.sandbox.Compile(ctx, request.Language, code, compileTimeLimit)
	if err != nil {
		return fmt.Errorf("compilation error: %w", err)
	}

	if !compileResult.Success {
		jw.logInfo(request.SubmissionID, fmt.Sprintf("Compilation failed: %s", compileResult.Error))
		err := jw.db.UpdateSubmissionCompilationError(ctx, request.SubmissionID, compileResult.Error)
		if err != nil {
			return fmt.Errorf("failed to update compilation error: %w", err)
		}

		eventData := map[string]any{
			"submission_id": request.SubmissionID,
			"language":      request.Language,
			"error_message": compileResult.Error,
		}
		jw.queue.PublishEvent(ctx, "SubmissionCompilationFailed", eventData)
		return nil
	}

	jw.logInfo(request.SubmissionID, "Compilation successful, starting execution")

	testCases, err := jw.getTestCases(ctx, request.ProblemID)
	if err != nil {
		return fmt.Errorf("failed to get test cases: %w", err)
	}

	// Validate and normalize resource limits
	limits, validationRes := jw.resourceValidator.ValidateAndNormalizeLimits(ctx, request.ProblemID, request.TimeLimitMs, request.MemoryLimitKb)
	if !validationRes.IsValid {
		jw.logError(request.SubmissionID, fmt.Sprintf("Resource validation failed: %v", validationRes.Violations))
		// Continue with normalized limits but log the violation
	}

	results := make([]models.SubmissionTestResult, 0, len(testCases))
	finalVerdict := models.VerdictAccepted
	maxTime := 0
	maxMemory := 0
	passedCount := 0

	for i, testCase := range testCases {
		jw.logInfo(request.SubmissionID, fmt.Sprintf("Running test case %d", i+1))

		input, err := jw.storage.DownloadCode(ctx, testCase.InputURL)
		if err != nil {
			return fmt.Errorf("failed to download test input: %w", err)
		}

		expectedOutput, err := jw.storage.DownloadCode(ctx, testCase.OutputURL)
		if err != nil {
			return fmt.Errorf("failed to download test output: %w", err)
		}

		// Validate and normalize resource limits
		limits, validationResult := jw.resourceValidator.ValidateAndNormalizeLimits(ctx, request.ProblemID, request.TimeLimitMs, request.MemoryLimitKb)
		if !validationResult.IsValid {
			jw.logError(request.SubmissionID, fmt.Sprintf("Resource validation failed: %v", validationResult.Violations))
			// Continue with normalized limits but log the violation
		}

		// Use per-test-case limits if available, otherwise fall back to problem limits
		timeLimit := time.Duration(testCase.TimeLimit) * time.Millisecond
		memoryLimit := testCase.MemoryLimit

		if timeLimit <= 0 {
			timeLimit = time.Duration(limits.TimeLimitMs) * time.Millisecond
		}
		if memoryLimit <= 0 {
			memoryLimit = limits.MemoryLimitKb
		}

		execResult, err := jw.sandbox.Execute(ctx, request.Language, input, timeLimit, memoryLimit)
		if err != nil {
			return fmt.Errorf("execution error: %w", err)
		}

		if execResult.ExecutionTime > maxTime {
			maxTime = execResult.ExecutionTime
		}
		if execResult.MemoryUsed > maxMemory {
			maxMemory = execResult.MemoryUsed
		}

		testVerdict := execResult.Verdict
		if testVerdict == models.VerdictAccepted {
			// Check output using appropriate checker
			isCorrect, _ := jw.checkOutput(testCase.InputURL, string(expectedOutput), execResult.Output, testCase.CheckerURL)
			if !isCorrect {
				testVerdict = models.VerdictWrongAns
			} else {
				passedCount++
			}
		}

		if testVerdict != models.VerdictAccepted {
			finalVerdict = testVerdict
		}

		result := models.SubmissionTestResult{
			SubmissionID:    request.SubmissionID,
			TestCaseID:      testCase.ID,
			TestNumber:      i + 1,
			Verdict:         testVerdict,
			ExecutionTimeMs: &execResult.ExecutionTime,
			MemoryUsedKb:    &execResult.MemoryUsed,
		}

		// Store checker output if available
		if testVerdict == models.VerdictAccepted {
			_, checkerOutput := jw.checkOutput(testCase.InputURL, string(expectedOutput), execResult.Output, testCase.CheckerURL)
			if checkerOutput != "" {
				result.CheckerOutput = &checkerOutput
			}
		} else {
			result.CheckerOutput = &execResult.Error
		}

		results = append(results, result)

		if finalVerdict != models.VerdictAccepted && finalVerdict != models.VerdictWrongAns {
			break
		}
	}

	judgeResult := &models.JudgeResult{
		SubmissionID:    request.SubmissionID,
		Verdict:         finalVerdict,
		ExecutionTimeMs: maxTime,
		MemoryUsedKb:    maxMemory,
		TestCasesPassed: passedCount,
		TestCasesTotal:  len(testCases),
	}

	err = jw.db.UpdateSubmissionResult(ctx, request.SubmissionID, judgeResult)
	if err != nil {
		return fmt.Errorf("failed to update submission result: %w", err)
	}

	err = jw.db.CreateSubmissionTestResults(ctx, results)
	if err != nil {
		return fmt.Errorf("failed to create test results: %w", err)
	}

	jw.logInfo(request.SubmissionID, fmt.Sprintf("Judging completed: %s (%d/%d)", finalVerdict, passedCount, len(testCases)))

	// Log resource usage
	jw.resourceValidator.LogResourceUsage(request.SubmissionID, limits, maxTime, maxMemory)

	err = jw.queue.PublishEvent(ctx, "SubmissionJudged", judgeResult)
	if err != nil {
		return fmt.Errorf("failed to publish judged event: %w", err)
	}

	// Enqueue for plagiarism check if submission was accepted
	if finalVerdict == models.VerdictAccepted && jw.plagiarismEnqueuer != nil {
		jw.plagiarismEnqueuer(request.SubmissionID, request.UserID, request.ProblemID, request.Language, request.CodeURL)
	}

	return nil
}

func (jw *JudgeWorker) getTestCases(ctx context.Context, problemID int64) ([]models.TestCase, error) {
	// Use circuit breaker for content service calls
	var testCaseResponses []httpclient.TestCaseResponse
	_, err := jw.circuitBreaker.Execute("content-service", func() (interface{}, error) {
		contentClient := httpclient.NewContentServiceClient("http://localhost:3002")
		responses, getErr := contentClient.GetTestCases(ctx, problemID)
		testCaseResponses = responses
		return nil, getErr
	})

	if err != nil {
		jw.logError(problemID, fmt.Sprintf("Failed to get test cases from content service (circuit breaker open): %v", err))

		testCases := []models.TestCase{
			{
				ID:          1,
				InputURL:    fmt.Sprintf("s3://testcases/problems/%d/testcases/1/input.txt", problemID),
				OutputURL:   fmt.Sprintf("s3://testcases/problems/%d/testcases/1/output.txt", problemID),
				IsSample:    true,
				TimeLimit:   2000,
				MemoryLimit: 262144,
			},
		}
		return testCases, nil
	}

	testCases := make([]models.TestCase, len(testCaseResponses))
	for i, tc := range testCaseResponses {
		testCases[i] = models.TestCase{
			ID:          tc.ID,
			InputURL:    tc.InputURL,
			OutputURL:   tc.OutputURL,
			IsSample:    tc.IsSample,
			TimeLimit:   tc.TimeLimit,
			MemoryLimit: tc.MemoryLimit,
		}
	}

	return testCases, nil
}

func (jw *JudgeWorker) logInfo(submissionID int64, message string) {
	log.Printf("[Submission %d] %s", submissionID, message)
	ctx := context.Background()
	jw.db.CreateExecutionLog(ctx, &models.ExecutionLog{
		SubmissionID: submissionID,
		Level:        "INFO",
		Message:      message,
	})
}

func (jw *JudgeWorker) checkOutput(inputURL, expectedOutput, actualOutput, checkerURL string) (bool, string) {
	// If no custom checker, use exact string matching
	if checkerURL == "" {
		expected := strings.TrimSpace(expectedOutput)
		actual := strings.TrimSpace(actualOutput)
		return expected == actual, ""
	}

	// Use custom checker for validation
	ctx := context.Background()

	// Create a test case model for the checker
	testCase := &models.TestCase{
		CheckerURL: checkerURL,
	}

	// Validate output using custom checker
	checkerResult, err := jw.customChecker.ValidateOutput(ctx, testCase, actualOutput, expectedOutput)
	if err != nil {
		jw.logError(0, fmt.Sprintf("Custom checker execution failed: %v", err))
		// Fall back to exact matching if checker fails
		expected := strings.TrimSpace(expectedOutput)
		actual := strings.TrimSpace(actualOutput)
		return expected == actual, "Custom checker failed, used exact matching"
	}

	return checkerResult.IsCorrect, checkerResult.Message
}

func (jw *JudgeWorker) logError(submissionID int64, message string) {
	log.Printf("[Submission %d] ERROR: %s", submissionID, message)
	ctx := context.Background()
	jw.db.CreateExecutionLog(ctx, &models.ExecutionLog{
		SubmissionID: submissionID,
		Level:        "ERROR",
		Message:      message,
	})
}

func (jp *JudgePool) GetStatus() map[string]any {
	activeWorkers := 0
	for _, worker := range jp.workers {
		if worker.isProcessing {
			activeWorkers++
		}
	}

	queueSize, _ := jp.queue.GetQueueInfo()

	return map[string]any{
		"total_workers":  jp.workerCount,
		"active_workers": activeWorkers,
		"queue_size":     queueSize,
		"is_healthy":     jp.queue.IsHealthy(),
	}
}

func (jp *JudgePool) GetSandbox() *sandbox.IsolateSandbox {
	return jp.sandbox
}

func (jp *JudgePool) ScaleWorkers(newWorkerCount int) error {
	jp.mutex.Lock()
	defer jp.mutex.Unlock()

	if !jp.isRunning {
		return fmt.Errorf("judge pool is not running")
	}

	if newWorkerCount < 1 || newWorkerCount > 50 {
		return fmt.Errorf("worker count must be between 1 and 50")
	}

	currentCount := len(jp.workers)

	if newWorkerCount == currentCount {
		return nil
	}

	if newWorkerCount > currentCount {
		// Scale up - add new workers
		for i := currentCount; i < newWorkerCount; i++ {
			worker := &JudgeWorker{
				id:                  i + 1,
				db:                  jp.db,
				queue:               jp.queue,
				storage:             jp.storage,
				sandbox:             jp.sandbox,
				maxFailures:         3,
				healthCheckInterval: 30 * time.Second,
				recoveryInterval:    60 * time.Second,
				isHealthy:           true,
				lastHeartbeat:       time.Now(),
			}

			workerModel := &models.JudgeWorker{
				WorkerName: fmt.Sprintf("judge-worker-%d", i+1),
				Status:     "idle",
			}

			if err := jp.db.CreateJudgeWorker(context.Background(), workerModel); err != nil {
				log.Printf("Failed to create worker record: %v", err)
				worker.workerID = int64(i + 1)
			} else {
				worker.workerID = int64(workerModel.ID)
			}

			jp.workers = append(jp.workers, worker)

			// Start the new worker
			go worker.start(context.Background())
		}
		log.Printf("Scaled up workers from %d to %d", currentCount, newWorkerCount)
	} else {
		// Scale down - gracefully stop excess workers
		excessWorkers := jp.workers[newWorkerCount:]
		for _, worker := range excessWorkers {
			// Wait for current job to finish
			worker.mutex.RLock()
			isProcessing := worker.isProcessing
			worker.mutex.RUnlock()

			if isProcessing {
				// Mark for shutdown after current job
				worker.mutex.Lock()
				worker.isHealthy = false
				worker.mutex.Unlock()
			}
		}

		// Remove excess workers from slice
		jp.workers = jp.workers[:newWorkerCount]
		log.Printf("Scaled down workers from %d to %d", currentCount, newWorkerCount)
	}

	jp.workerCount = newWorkerCount
	return nil
}

func (jp *JudgePool) Stop() {
	jp.mutex.Lock()
	if !jp.isRunning {
		jp.mutex.Unlock()
		return
	}
	jp.isRunning = false
	jp.mutex.Unlock()

	log.Printf("Stopping judge pool gracefully")

	// Create shutdown context with timeout
	ctx, cancel := context.WithTimeout(context.Background(), jp.shutdownTimeout)
	defer cancel()

	// Wait for all workers to finish current jobs
	done := make(chan bool)
	go func() {
		for _, worker := range jp.workers {
			worker.mutex.RLock()
			isProcessing := worker.isProcessing
			worker.mutex.RUnlock()

			if isProcessing {
				log.Printf("Waiting for worker %d to finish current job", worker.id)
			}
		}

		// Check every second if all workers are done
		for {
			allDone := true
			for _, worker := range jp.workers {
				worker.mutex.RLock()
				if worker.isProcessing {
					allDone = false
					worker.mutex.RUnlock()
					break
				}
				worker.mutex.RUnlock()
			}

			if allDone {
				break
			}
			time.Sleep(1 * time.Second)
		}

		done <- true
	}()

	select {
	case <-done:
		log.Printf("All workers finished gracefully")
	case <-ctx.Done():
		log.Printf("Shutdown timeout reached, forcing stop")
	}

	log.Printf("Judge pool stopped")
}

func (jp *JudgePool) SetPlagiarismEnqueuer(enqueuer func(submissionID, userID, problemID int64, language, codeURL string)) {
	for _, worker := range jp.workers {
		worker.plagiarismEnqueuer = enqueuer
	}
}

func (jp *JudgePool) healthMonitor(ctx context.Context) {
	ticker := time.NewTicker(jp.healthCheckInterval)
	defer ticker.Stop()

	for {
		select {
		case <-ctx.Done():
			return
		case <-ticker.C:
			jp.checkWorkerHealth(ctx)
		}
	}
}

func (jp *JudgePool) heartbeatReporter(ctx context.Context) {
	ticker := time.NewTicker(jp.heartbeatInterval)
	defer ticker.Stop()

	for {
		select {
		case <-ctx.Done():
			return
		case <-ticker.C:
			jp.reportPoolHealth(ctx)
		}
	}
}

func (jp *JudgePool) checkWorkerHealth(ctx context.Context) {
	jp.mutex.RLock()
	workers := make([]*JudgeWorker, len(jp.workers))
	copy(workers, jp.workers)
	jp.mutex.RUnlock()

	for _, worker := range workers {
		worker.mutex.RLock()
		isHealthy := worker.isHealthy
		lastHeartbeat := worker.lastHeartbeat
		worker.mutex.RUnlock()

		// Check if worker heartbeat is stale
		if time.Since(lastHeartbeat) > jp.healthCheckInterval*2 {
			log.Printf("Worker %d heartbeat timeout, last seen %v ago", worker.id, time.Since(lastHeartbeat))

			worker.mutex.Lock()
			worker.failureCount++
			worker.isHealthy = false
			worker.mutex.Unlock()

			// Try to recover worker if failure count is below threshold
			if worker.failureCount < jp.maxWorkerFailures {
				log.Printf("Attempting to recover worker %d (attempt %d/%d)", worker.id, worker.failureCount, jp.maxWorkerFailures)
				go jp.recoverWorker(ctx, worker)
			} else {
				log.Printf("Worker %d exceeded max failures, marking as failed", worker.id)
				jp.handleFailedWorker(ctx, worker)
			}
		} else if !isHealthy && time.Since(lastHeartbeat) < jp.healthCheckInterval {
			// Worker recovered
			worker.mutex.Lock()
			worker.isHealthy = true
			worker.failureCount = 0
			worker.mutex.Unlock()
			log.Printf("Worker %d recovered and is healthy", worker.id)
		}
	}
}

func (jp *JudgePool) recoverWorker(ctx context.Context, worker *JudgeWorker) {
	// Wait recovery interval
	time.Sleep(worker.recoveryInterval)

	// Check if context is still valid
	select {
	case <-ctx.Done():
		return
	default:
	}

	// Reset worker state
	worker.mutex.Lock()
	worker.isHealthy = true
	worker.lastHeartbeat = time.Now()
	worker.failureCount = 0
	worker.isProcessing = false
	worker.currentJob = nil
	worker.mutex.Unlock()

	// Update worker status in database
	if worker.workerID > 0 {
		err := jp.db.UpdateWorkerStatus(ctx, int(worker.workerID), "idle", nil)
		if err != nil {
			log.Printf("Failed to update recovered worker %d status: %v", worker.id, err)
		}
	}

	log.Printf("Worker %d recovery completed", worker.id)
}

func (jp *JudgePool) handleFailedWorker(ctx context.Context, worker *JudgeWorker) {
	// Update worker status in database
	if worker.workerID > 0 {
		err := jp.db.UpdateWorkerStatus(ctx, int(worker.workerID), "failed", nil)
		if err != nil {
			log.Printf("Failed to update failed worker %d status: %v", worker.id, err)
		}
	}

	// In a production system, you might want to:
	// - Send alerts
	// - Try to restart the worker process
	// - Remove from rotation and create a new worker
	log.Printf("Worker %d handling complete - marked as failed", worker.id)
}

func (jp *JudgePool) reportPoolHealth(ctx context.Context) {
	jp.mutex.RLock()
	workers := make([]*JudgeWorker, len(jp.workers))
	copy(workers, jp.workers)
	jp.mutex.RUnlock()

	healthyWorkers := 0
	unhealthyWorkers := 0
	activeWorkers := 0

	for _, worker := range workers {
		worker.mutex.RLock()
		if worker.isHealthy {
			healthyWorkers++
		} else {
			unhealthyWorkers++
		}
		if worker.isProcessing {
			activeWorkers++
		}
		worker.mutex.RUnlock()
	}

	// Get queue info
	queueSize, _ := jp.queue.GetQueueInfo()

	// Log pool health
	log.Printf("Pool Health - Total: %d, Healthy: %d, Unhealthy: %d, Active: %d, Queue: %d",
		len(workers), healthyWorkers, unhealthyWorkers, activeWorkers, queueSize)

	// Store health metrics in database
	healthData := map[string]interface{}{
		"total_workers":     len(workers),
		"healthy_workers":   healthyWorkers,
		"unhealthy_workers": unhealthyWorkers,
		"active_workers":    activeWorkers,
		"queue_size":        queueSize,
		"timestamp":         time.Now(),
	}

	// Create health log entry
	logEntry := &models.ExecutionLog{
		Level:   "INFO",
		Message: fmt.Sprintf("Pool health report: %+v", healthData),
	}

	if err := jp.db.CreateExecutionLog(ctx, logEntry); err != nil {
		log.Printf("Failed to store health report: %v", err)
	}
}

func (jw *JudgeWorker) heartbeatLoop(ctx context.Context) {
	ticker := time.NewTicker(jw.healthCheckInterval / 3) // Send heartbeat every 10 seconds
	defer ticker.Stop()

	for {
		select {
		case <-ctx.Done():
			return
		case <-ticker.C:
			jw.updateHeartbeat()
		}
	}
}

func (jw *JudgeWorker) updateHeartbeat() {
	jw.mutex.Lock()
	jw.lastHeartbeat = time.Now()
	jw.mutex.Unlock()
}

func (jw *JudgeWorker) markUnhealthy() {
	jw.mutex.Lock()
	jw.isHealthy = false
	jw.failureCount++
	jw.mutex.Unlock()
}

func (jw *JudgeWorker) markHealthy() {
	jw.mutex.Lock()
	jw.isHealthy = true
	jw.failureCount = 0
	jw.mutex.Unlock()
}

func (jp *JudgePool) autoScaler(ctx context.Context) {
	ticker := time.NewTicker(jp.autoScaleInterval)
	defer ticker.Stop()

	log.Printf("Starting auto-scaler with %d min workers, %d max workers", jp.minWorkers, jp.maxWorkers)

	for {
		select {
		case <-ctx.Done():
			log.Printf("Auto-scaler shutting down")
			return
		case <-ticker.C:
			jp.performAutoScaling(ctx)
		}
	}
}

func (jp *JudgePool) performAutoScaling(ctx context.Context) {
	jp.mutex.RLock()
	currentWorkers := jp.workerCount
	jp.mutex.RUnlock()

	// Get current queue metrics
	queueSize, err := jp.queue.GetQueueInfo()
	if err != nil {
		log.Printf("Failed to get queue info for auto-scaling: %v", err)
		return
	}

	// Get active workers count
	activeWorkers := 0
	for _, worker := range jp.workers {
		worker.mutex.RLock()
		if worker.isProcessing {
			activeWorkers++
		}
		worker.mutex.RUnlock()
	}

	// Calculate optimal worker count
	optimalWorkers := jp.calculateOptimalWorkers(queueSize, activeWorkers, currentWorkers)

	if optimalWorkers != currentWorkers {
		log.Printf("Auto-scaling: %d -> %d workers (queue: %d, active: %d)",
			currentWorkers, optimalWorkers, queueSize, activeWorkers)

		err := jp.ScaleWorkers(optimalWorkers)
		if err != nil {
			log.Printf("Auto-scaling failed: %v", err)
		}
	}
}

func (jp *JudgePool) calculateOptimalWorkers(queueSize, activeWorkers, currentWorkers int) int {
	// Scaling factors
	scaleUpThreshold := 3     // Scale up if queue size > active workers * 3
	scaleDownThreshold := 0.5 // Scale down if queue size < active workers * 0.5
	maxScaleUp := 5           // Maximum workers to add at once
	maxScaleDown := 3         // Maximum workers to remove at once

	// Calculate desired workers based on queue load
	var desiredWorkers int

	if queueSize == 0 {
		// No queue - scale down to minimum
		desiredWorkers = jp.minWorkers
	} else if queueSize > activeWorkers*scaleUpThreshold {
		// High load - scale up aggressively
		desiredWorkers = currentWorkers + maxScaleUp
	} else if float64(queueSize) < float64(activeWorkers)*scaleDownThreshold && currentWorkers > jp.minWorkers {
		// Low load - scale down gradually
		desiredWorkers = currentWorkers - maxScaleDown
	} else {
		// Moderate load - maintain current level
		desiredWorkers = currentWorkers
	}

	// Apply bounds
	if desiredWorkers < jp.minWorkers {
		desiredWorkers = jp.minWorkers
	}
	if desiredWorkers > jp.maxWorkers {
		desiredWorkers = jp.maxWorkers
	}

	// Don't scale down if workers are busy
	if desiredWorkers < currentWorkers && activeWorkers >= desiredWorkers {
		desiredWorkers = currentWorkers
	}

	return desiredWorkers
}

func (jp *JudgePool) EnableAutoScaling() {
	jp.mutex.Lock()
	jp.autoScalingEnabled = true
	jp.mutex.Unlock()
	log.Printf("Auto-scaling enabled")
}

func (jp *JudgePool) DisableAutoScaling() {
	jp.mutex.Lock()
	jp.autoScalingEnabled = false
	jp.mutex.Unlock()
	log.Printf("Auto-scaling disabled")
}

func (jp *JudgePool) SetAutoScalingLimits(minWorkers, maxWorkers int) error {
	if minWorkers < 1 || maxWorkers < minWorkers {
		return fmt.Errorf("invalid limits: min=%d, max=%d", minWorkers, maxWorkers)
	}

	jp.mutex.Lock()
	jp.minWorkers = minWorkers
	jp.maxWorkers = maxWorkers
	jp.mutex.Unlock()

	log.Printf("Auto-scaling limits updated: min=%d, max=%d", minWorkers, maxWorkers)
	return nil
}
