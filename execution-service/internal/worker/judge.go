package worker

import (
	"context"
	"fmt"
	"log"
	"strings"
	"time"

	"execution_service/internal/database"
	"execution_service/internal/httpclient"
	"execution_service/internal/models"
	"execution_service/internal/queue"
	"execution_service/internal/sandbox"
	"execution_service/internal/storage"

	amqp "github.com/rabbitmq/amqp091-go"
)

type JudgeWorker struct {
	id           int
	db           *database.DB
	queue        *queue.RabbitMQClient
	storage      *storage.MinIOClient
	sandbox      *sandbox.IsolateSandbox
	currentJob   *models.JudgeRequest
	isProcessing bool
	workerID     int64
}

type JudgePool struct {
	workers     []*JudgeWorker
	db          *database.DB
	queue       *queue.RabbitMQClient
	storage     *storage.MinIOClient
	sandbox     *sandbox.IsolateSandbox
	workerCount int
}

func NewJudgePool(workerCount int, db *database.DB, q *queue.RabbitMQClient, s *storage.MinIOClient, sb *sandbox.IsolateSandbox) *JudgePool {
	workers := make([]*JudgeWorker, workerCount)
	for i := 0; i < workerCount; i++ {
		worker := &JudgeWorker{
			id:      i + 1,
			db:      db,
			queue:   q,
			storage: s,
			sandbox: sb,
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
		workers:     workers,
		db:          db,
		queue:       q,
		storage:     s,
		sandbox:     sb,
		workerCount: workerCount,
	}
}

func (jp *JudgePool) Start(ctx context.Context) error {
	log.Printf("Starting judge pool with %d workers", jp.workerCount)

	for _, worker := range jp.workers {
		go worker.start(ctx)
	}

	return nil
}

func (jw *JudgeWorker) start(ctx context.Context) {
	log.Printf("Judge worker %d started", jw.id)

	msgs, err := jw.queue.ConsumeSubmissions(ctx)
	if err != nil {
		log.Printf("Worker %d failed to start consuming: %v", jw.id, err)
		return
	}

	for {
		select {
		case <-ctx.Done():
			log.Printf("Worker %d shutting down", jw.id)
			return
		case msg := <-msgs:
			if jw.isProcessing {
				log.Printf("Worker %d is busy, rejecting message", jw.id)
				jw.queue.RejectMessage(msg, true)
				continue
			}

			jw.processMessage(ctx, msg)
		}
	}
}

func (jw *JudgeWorker) processMessage(ctx context.Context, msg amqp.Delivery) {
	jw.isProcessing = true
	defer func() {
		jw.isProcessing = false
		jw.currentJob = nil
		if jw.workerID > 0 {
			jw.db.UpdateWorkerStatus(ctx, int(jw.workerID), "idle", nil)
		}
	}()

	request, err := queue.ParseJudgeRequest(msg)
	if err != nil {
		log.Printf("Worker %d failed to parse message: %v", jw.id, err)
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
	code, err := jw.storage.DownloadCode(ctx, request.CodeURL)
	if err != nil {
		return fmt.Errorf("failed to download code: %w", err)
	}

	jw.logInfo(request.SubmissionID, "Starting compilation")

	compileResult, err := jw.sandbox.Compile(ctx, request.Language, code, time.Duration(request.TimeLimitMs)*time.Millisecond)
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

		execResult, err := jw.sandbox.Execute(ctx, request.Language, input,
			time.Duration(testCase.TimeLimit)*time.Millisecond, testCase.MemoryLimit)
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
			output := strings.TrimSpace(execResult.Output)
			expected := strings.TrimSpace(string(expectedOutput))
			if output != expected {
				testVerdict = models.VerdictWrongAns
			} else {
				passedCount++
			}
		}

		if testVerdict != models.VerdictAccepted {
			finalVerdict = testVerdict
		}

		results = append(results, models.SubmissionTestResult{
			SubmissionID:    request.SubmissionID,
			TestCaseID:      testCase.ID,
			TestNumber:      i + 1,
			Verdict:         testVerdict,
			ExecutionTimeMs: &execResult.ExecutionTime,
			MemoryUsedKb:    &execResult.MemoryUsed,
			CheckerOutput:   &execResult.Error,
		})

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

	err = jw.queue.PublishEvent(ctx, "SubmissionJudged", judgeResult)
	if err != nil {
		return fmt.Errorf("failed to publish judged event: %w", err)
	}

	return nil
}

func (jw *JudgeWorker) getTestCases(ctx context.Context, problemID int64) ([]models.TestCase, error) {
	contentClient := httpclient.NewContentServiceClient("http://localhost:3002")

	testCaseResponses, err := contentClient.GetTestCases(ctx, problemID)
	if err != nil {
		jw.logError(problemID, fmt.Sprintf("Failed to get test cases from content service: %v", err))

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

func (jp *JudgePool) Stop() {
	log.Printf("Stopping judge pool")
}
