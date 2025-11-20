package services

import (
	"context"
	"encoding/json"
	"fmt"
	"log"
	"time"

	"execution_service/internal/models"
	"execution_service/internal/queue"

	amqp "github.com/rabbitmq/amqp091-go"
)

type DeadLetterQueueService struct {
	queue          *queue.RabbitMQClient
	maxRetries     int
	retryDelay     time.Duration
	dlqName        string
	retryQueueName string
	isRunning      bool
	stopChan       chan struct{}
}

type RetryableSubmission struct {
	*models.JudgeRequest
	RetryCount    int       `json:"retry_count"`
	OriginalQueue string    `json:"original_queue"`
	LastError     string    `json:"last_error"`
	FirstFailed   time.Time `json:"first_failed"`
	LastRetry     time.Time `json:"last_retry"`
}

func NewDeadLetterQueueService(queue *queue.RabbitMQClient) *DeadLetterQueueService {
	return &DeadLetterQueueService{
		queue:          queue,
		maxRetries:     3,
		retryDelay:     5 * time.Minute,
		dlqName:        "judge.failed",
		retryQueueName: "judge.retry",
		stopChan:       make(chan struct{}),
	}
}

func (dlqs *DeadLetterQueueService) Start(ctx context.Context) error {
	if dlqs.isRunning {
		return fmt.Errorf("dead letter queue service is already running")
	}

	dlqs.isRunning = true
	log.Println("Starting dead letter queue service")

	// Setup dead letter queue and retry queue
	err := dlqs.setupQueues(ctx)
	if err != nil {
		return fmt.Errorf("failed to setup queues: %w", err)
	}

	// Start processing dead letter messages
	go dlqs.processDeadLetterMessages(ctx)

	// Start retry queue processor
	go dlqs.processRetryMessages(ctx)

	return nil
}

func (dlqs *DeadLetterQueueService) Stop() {
	if !dlqs.isRunning {
		return
	}

	log.Println("Stopping dead letter queue service")
	close(dlqs.stopChan)
	dlqs.isRunning = false
}

func (dlqs *DeadLetterQueueService) setupQueues(ctx context.Context) error {
	// Declare dead letter exchange
	err := dlqs.queue.DeclareExchange(ctx, "judge.dlq", "direct", true, false, false, false, nil)
	if err != nil {
		return fmt.Errorf("failed to declare dlq exchange: %w", err)
	}

	// Declare dead letter queue
	_, err = dlqs.queue.DeclareQueue(ctx, dlqs.dlqName, true, false, false, false, amqp.Table{
		"x-message-ttl":             7 * 24 * time.Hour.Milliseconds(), // 7 days TTL
		"x-dead-letter-exchange":    "judge.dlq",
		"x-dead-letter-routing-key": dlqs.dlqName,
	})
	if err != nil {
		return fmt.Errorf("failed to declare dlq queue: %w", err)
	}

	// Bind dead letter queue to exchange
	err = dlqs.queue.BindQueue(ctx, dlqs.dlqName, "judge.dlq", dlqs.dlqName)
	if err != nil {
		return fmt.Errorf("failed to bind dlq queue: %w", err)
	}

	// Declare retry queue
	_, err = dlqs.queue.DeclareQueue(ctx, dlqs.retryQueueName, true, false, false, false, amqp.Table{
		"x-message-ttl":             dlqs.retryDelay.Milliseconds(),
		"x-dead-letter-exchange":    "judge.dlq",
		"x-dead-letter-routing-key": dlqs.dlqName,
	})
	if err != nil {
		return fmt.Errorf("failed to declare retry queue: %w", err)
	}

	// Bind retry queue to exchange
	err = dlqs.queue.BindQueue(ctx, dlqs.retryQueueName, "judge.dlq", dlqs.retryQueueName)
	if err != nil {
		return fmt.Errorf("failed to bind retry queue: %w", err)
	}

	return nil
}

func (dlqs *DeadLetterQueueService) processDeadLetterMessages(ctx context.Context) {
	msgs, err := dlqs.queue.ConsumeFromQueue(ctx, dlqs.dlqName, "dlq-consumer")
	if err != nil {
		log.Printf("Failed to start consuming from dead letter queue: %v", err)
		return
	}

	for {
		select {
		case <-ctx.Done():
			return
		case <-dlqs.stopChan:
			return
		case msg := <-msgs:
			dlqs.handleDeadLetterMessage(ctx, msg)
		}
	}
}

func (dlqs *DeadLetterQueueService) processRetryMessages(ctx context.Context) {
	msgs, err := dlqs.queue.ConsumeFromQueue(ctx, dlqs.retryQueueName, "retry-consumer")
	if err != nil {
		log.Printf("Failed to start consuming from retry queue: %v", err)
		return
	}

	for {
		select {
		case <-ctx.Done():
			return
		case <-dlqs.stopChan:
			return
		case msg := <-msgs:
			dlqs.handleRetryMessage(ctx, msg)
		}
	}
}

func (dlqs *DeadLetterQueueService) handleDeadLetterMessage(ctx context.Context, msg amqp.Delivery) {
	var retryableSubmission RetryableSubmission
	err := json.Unmarshal(msg.Body, &retryableSubmission)
	if err != nil {
		log.Printf("Failed to unmarshal dead letter message: %v", err)
		dlqs.queue.AcknowledgeMessage(msg)
		return
	}

	log.Printf("Processing dead letter message for submission %d (retry count: %d)",
		retryableSubmission.SubmissionID, retryableSubmission.RetryCount)

	// Check if we should retry
	if retryableSubmission.RetryCount < dlqs.maxRetries {
		// Check if enough time has passed since last retry
		if time.Since(retryableSubmission.LastRetry) >= dlqs.retryDelay {
			dlqs.scheduleRetry(ctx, &retryableSubmission)
		} else {
			// Put back in retry queue with delay
			dlqs.scheduleRetry(ctx, &retryableSubmission)
		}
	} else {
		// Max retries reached, mark as permanently failed
		dlqs.markAsPermanentlyFailed(ctx, &retryableSubmission)
	}

	dlqs.queue.AcknowledgeMessage(msg)
}

func (dlqs *DeadLetterQueueService) handleRetryMessage(ctx context.Context, msg amqp.Delivery) {
	var retryableSubmission RetryableSubmission
	err := json.Unmarshal(msg.Body, &retryableSubmission)
	if err != nil {
		log.Printf("Failed to unmarshal retry message: %v", err)
		dlqs.queue.AcknowledgeMessage(msg)
		return
	}

	log.Printf("Retrying submission %d (attempt %d/%d)",
		retryableSubmission.SubmissionID, retryableSubmission.RetryCount+1, dlqs.maxRetries)

	// Increment retry count and update timestamps
	retryableSubmission.RetryCount++
	retryableSubmission.LastRetry = time.Now()

	// Publish back to main queue
	err = dlqs.queue.PublishSubmission(ctx, retryableSubmission.JudgeRequest)
	if err != nil {
		log.Printf("Failed to publish retry submission %d: %v", retryableSubmission.SubmissionID, err)
		// Put back in dead letter queue
		dlqs.sendToDeadLetterQueue(ctx, &retryableSubmission)
	}

	dlqs.queue.AcknowledgeMessage(msg)
}

func (dlqs *DeadLetterQueueService) scheduleRetry(ctx context.Context, submission *RetryableSubmission) {
	// Update retry count and timestamps
	submission.RetryCount++
	submission.LastRetry = time.Now()

	// Send to retry queue
	body, err := json.Marshal(submission)
	if err != nil {
		log.Printf("Failed to marshal retry submission: %v", err)
		return
	}

	err = dlqs.queue.PublishToQueue(ctx, dlqs.retryQueueName, body)
	if err != nil {
		log.Printf("Failed to publish to retry queue: %v", err)
		// Send to dead letter queue instead
		dlqs.sendToDeadLetterQueue(ctx, submission)
	}
}

func (dlqs *DeadLetterQueueService) sendToDeadLetterQueue(ctx context.Context, submission *RetryableSubmission) {
	body, err := json.Marshal(submission)
	if err != nil {
		log.Printf("Failed to marshal submission for dead letter queue: %v", err)
		return
	}

	err = dlqs.queue.PublishToQueue(ctx, dlqs.dlqName, body)
	if err != nil {
		log.Printf("Failed to publish to dead letter queue: %v", err)
	}
}

func (dlqs *DeadLetterQueueService) markAsPermanentlyFailed(ctx context.Context, submission *RetryableSubmission) {
	log.Printf("Marking submission %d as permanently failed after %d retries",
		submission.SubmissionID, submission.RetryCount)

	// Update submission in database with permanent failure status
	// This would typically involve calling a database method to update the submission
	// For now, we'll log it and potentially send an alert
	log.Printf("ALERT: Submission %d permanently failed after %d retries. Last error: %s",
		submission.SubmissionID, submission.RetryCount, submission.LastError)

	// TODO: Implement database update for permanent failure
	// err := dlqs.db.MarkSubmissionAsPermanentlyFailed(ctx, submission.SubmissionID, submission.LastError)
}

func (dlqs *DeadLetterQueueService) GetDLQStats(ctx context.Context) (map[string]interface{}, error) {
	stats := make(map[string]interface{})

	// Get dead letter queue size
	dlqSize, err := dlqs.queue.GetQueueSize(ctx, dlqs.dlqName)
	if err != nil {
		return nil, fmt.Errorf("failed to get dlq size: %w", err)
	}
	stats["dead_letter_queue_size"] = dlqSize

	// Get retry queue size
	retrySize, err := dlqs.queue.GetQueueSize(ctx, dlqs.retryQueueName)
	if err != nil {
		return nil, fmt.Errorf("failed to get retry queue size: %w", err)
	}
	stats["retry_queue_size"] = retrySize

	stats["max_retries"] = dlqs.maxRetries
	stats["retry_delay"] = dlqs.retryDelay.String()
	stats["service_running"] = dlqs.isRunning

	return stats, nil
}

func (dlqs *DeadLetterQueueService) PurgeDLQ(ctx context.Context) error {
	err := dlqs.queue.PurgeQueueByName(dlqs.dlqName)
	if err != nil {
		return fmt.Errorf("failed to purge dead letter queue: %w", err)
	}

	err = dlqs.queue.PurgeQueueByName(dlqs.retryQueueName)
	if err != nil {
		return fmt.Errorf("failed to purge retry queue: %w", err)
	}

	log.Println("Purged dead letter and retry queues")
	return nil
}
