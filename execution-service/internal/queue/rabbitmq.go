package queue

import (
	"context"
	"encoding/json"
	"fmt"
	"log"
	"time"

	"execution_service/internal/config"
	"execution_service/internal/models"

	amqp "github.com/rabbitmq/amqp091-go"
)

type RabbitMQClient struct {
	conn    *amqp.Connection
	channel *amqp.Channel
	queue   amqp.Queue
	config  *config.RabbitMQConfig
}

func NewRabbitMQClient(cfg *config.RabbitMQConfig) (*RabbitMQClient, error) {
	conn, err := amqp.Dial(cfg.URL)
	if err != nil {
		return nil, fmt.Errorf("failed to connect to RabbitMQ: %w", err)
	}

	ch, err := conn.Channel()
	if err != nil {
		return nil, fmt.Errorf("failed to open channel: %w", err)
	}

	err = ch.Qos(
		cfg.PrefetchCount,
		0,
		false,
	)
	if err != nil {
		return nil, fmt.Errorf("failed to set QoS: %w", err)
	}

	queue, err := ch.QueueDeclare(
		cfg.QueueName,
		true,
		false,
		false,
		false,
		amqp.Table{
			"x-max-priority":         10,
			"x-dead-letter-exchange": "judge.failed",
			"x-message-ttl":          300000,
		},
	)
	if err != nil {
		return nil, fmt.Errorf("failed to declare queue: %w", err)
	}

	err = ch.ExchangeDeclare(
		"codehakam.events",
		"topic",
		true,
		false,
		false,
		false,
		nil,
	)
	if err != nil {
		return nil, fmt.Errorf("failed to declare exchange: %w", err)
	}

	return &RabbitMQClient{
		conn:    conn,
		channel: ch,
		queue:   queue,
		config:  cfg,
	}, nil
}

func (r *RabbitMQClient) Close() error {
	if r.channel != nil {
		r.channel.Close()
	}
	if r.conn != nil {
		r.conn.Close()
	}
	return nil
}

func (r *RabbitMQClient) PublishSubmission(ctx context.Context, request *models.JudgeRequest) error {
	body, err := json.Marshal(request)
	if err != nil {
		return fmt.Errorf("failed to marshal judge request: %w", err)
	}

	err = r.channel.PublishWithContext(
		ctx,
		"",
		r.queue.Name,
		false,
		false,
		amqp.Publishing{
			ContentType: "application/json",
			Body:        body,
			Priority:    uint8(request.Priority),
			Timestamp:   time.Now(),
		},
	)
	if err != nil {
		return fmt.Errorf("failed to publish message: %w", err)
	}

	return nil
}

func (r *RabbitMQClient) PublishEvent(ctx context.Context, eventType string, data any) error {
	event := models.EventMessage{
		EventType: eventType,
		Data:      make(map[string]any),
		Timestamp: time.Now(),
	}

	switch v := data.(type) {
	case *models.JudgeResult:
		event.Data["submission_id"] = v.SubmissionID
		event.Data["verdict"] = v.Verdict
		event.Data["execution_time_ms"] = v.ExecutionTimeMs
		event.Data["memory_used_kb"] = v.MemoryUsedKb
		event.Data["test_cases_passed"] = v.TestCasesPassed
		event.Data["test_cases_total"] = v.TestCasesTotal
	case map[string]any:
		event.Data = v
	default:
		return fmt.Errorf("unsupported event data type")
	}

	body, err := json.Marshal(event)
	if err != nil {
		return fmt.Errorf("failed to marshal event: %w", err)
	}

	routingKey := fmt.Sprintf("submission.%s", eventType)
	if eventType == "SubmissionJudged" {
		routingKey = "submission.judged"
	}

	err = r.channel.PublishWithContext(
		ctx,
		"codehakam.events",
		routingKey,
		false,
		false,
		amqp.Publishing{
			ContentType: "application/json",
			Body:        body,
			Timestamp:   time.Now(),
		},
	)
	if err != nil {
		return fmt.Errorf("failed to publish event: %w", err)
	}

	return nil
}

func (r *RabbitMQClient) ConsumeSubmissions(ctx context.Context) (<-chan amqp.Delivery, error) {
	msgs, err := r.channel.ConsumeWithContext(
		ctx,
		r.queue.Name,
		"judge-worker",
		false,
		false,
		false,
		false,
		nil,
	)
	if err != nil {
		return nil, fmt.Errorf("failed to register consumer: %w", err)
	}

	return msgs, nil
}

func (r *RabbitMQClient) AcknowledgeMessage(msg amqp.Delivery) error {
	return msg.Ack(false)
}

func (r *RabbitMQClient) RejectMessage(msg amqp.Delivery, requeue bool) error {
	return msg.Nack(false, requeue)
}

func (r *RabbitMQClient) GetQueueInfo() (int, error) {
	queue, err := r.channel.QueueDeclarePassive(
		r.queue.Name,
		true,
		false,
		false,
		false,
		nil,
	)
	if err != nil {
		return 0, fmt.Errorf("failed to inspect queue: %w", err)
	}

	return queue.Messages, nil
}

func (r *RabbitMQClient) PurgeQueue() error {
	_, err := r.channel.QueuePurge(r.queue.Name, false)
	if err != nil {
		return fmt.Errorf("failed to purge queue: %w", err)
	}

	return nil
}

func (r *RabbitMQClient) PurgeQueueByName(queueName string) error {
	_, err := r.channel.QueuePurge(queueName, false)
	if err != nil {
		return fmt.Errorf("failed to purge queue %s: %w", queueName, err)
	}

	return nil
}

func (r *RabbitMQClient) IsHealthy() bool {
	if r.conn == nil || r.conn.IsClosed() {
		return false
	}
	if r.channel == nil || r.channel.IsClosed() {
		return false
	}

	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	err := r.channel.PublishWithContext(
		ctx,
		"",
		r.queue.Name,
		false,
		false,
		amqp.Publishing{
			ContentType: "text/plain",
			Body:        []byte("health-check"),
			Timestamp:   time.Now(),
			Expiration:  "1000",
		},
	)

	return err == nil
}

func ParseJudgeRequest(msg amqp.Delivery) (*models.JudgeRequest, error) {
	var request models.JudgeRequest
	err := json.Unmarshal(msg.Body, &request)
	if err != nil {
		return nil, fmt.Errorf("failed to unmarshal judge request: %w", err)
	}
	return &request, nil
}

func (r *RabbitMQClient) StartHeartbeat() {
	go func() {
		ticker := time.NewTicker(30 * time.Second)
		defer ticker.Stop()

		for range ticker.C {
			if r.conn == nil || r.conn.IsClosed() {
				log.Printf("RabbitMQ connection lost, attempting to reconnect...")
				if err := r.reconnect(); err != nil {
					log.Printf("Failed to reconnect to RabbitMQ: %v", err)
				}
			}
		}
	}()
}

func (r *RabbitMQClient) reconnect() error {
	conn, err := amqp.Dial(r.config.URL)
	if err != nil {
		return fmt.Errorf("failed to reconnect to RabbitMQ: %w", err)
	}

	ch, err := conn.Channel()
	if err != nil {
		conn.Close()
		return fmt.Errorf("failed to open channel on reconnect: %w", err)
	}

	err = ch.Qos(
		r.config.PrefetchCount,
		0,
		false,
	)
	if err != nil {
		ch.Close()
		conn.Close()
		return fmt.Errorf("failed to set QoS on reconnect: %w", err)
	}

	queue, err := ch.QueueDeclare(
		r.config.QueueName,
		true,
		false,
		false,
		false,
		amqp.Table{
			"x-max-priority":         10,
			"x-dead-letter-exchange": "judge.failed",
			"x-message-ttl":          300000,
		},
	)
	if err != nil {
		ch.Close()
		conn.Close()
		return fmt.Errorf("failed to declare queue on reconnect: %w", err)
	}

	if r.conn != nil {
		r.conn.Close()
	}
	if r.channel != nil {
		r.channel.Close()
	}

	r.conn = conn
	r.channel = ch
	r.queue = queue

	log.Printf("Successfully reconnected to RabbitMQ")
	return nil
}

// Additional methods for dead letter queue and retry queue management
func (r *RabbitMQClient) DeclareExchange(ctx context.Context, name, kind string, durable, autoDelete, internal, noWait bool, args amqp.Table) error {
	return r.channel.ExchangeDeclare(
		name,
		kind,
		durable,
		autoDelete,
		internal,
		noWait,
		args,
	)
}

func (r *RabbitMQClient) DeclareQueue(ctx context.Context, name string, durable, autoDelete, exclusive, noWait bool, args amqp.Table) (amqp.Queue, error) {
	return r.channel.QueueDeclare(
		name,
		durable,
		autoDelete,
		exclusive,
		noWait,
		args,
	)
}

func (r *RabbitMQClient) BindQueue(ctx context.Context, queueName, exchangeName, routingKey string) error {
	return r.channel.QueueBind(
		queueName,
		exchangeName,
		routingKey,
		false,
		nil,
	)
}

func (r *RabbitMQClient) ConsumeFromQueue(ctx context.Context, queueName, consumer string) (<-chan amqp.Delivery, error) {
	return r.channel.Consume(
		queueName,
		consumer,
		false,
		false,
		false,
		false,
		nil,
	)
}

func (r *RabbitMQClient) PublishToQueue(ctx context.Context, queueName string, body []byte) error {
	return r.channel.PublishWithContext(
		ctx,
		"",
		queueName,
		false,
		false,
		amqp.Publishing{
			ContentType: "application/json",
			Body:        body,
			Timestamp:   time.Now(),
		},
	)
}

func (r *RabbitMQClient) GetQueueSize(ctx context.Context, queueName string) (int, error) {
	queue, err := r.channel.QueueDeclarePassive(
		queueName,
		true,
		false,
		false,
		false,
		nil,
	)
	if err != nil {
		return 0, fmt.Errorf("failed to inspect queue %s: %w", queueName, err)
	}

	return queue.Messages, nil
}
