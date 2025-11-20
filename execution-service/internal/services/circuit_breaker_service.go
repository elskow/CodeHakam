package services

import (
	"context"
	"fmt"
	"log"
	"time"

	"github.com/sony/gobreaker"
)

type CircuitBreakerService struct {
	minioBreaker    *gobreaker.CircuitBreaker
	rabbitmqBreaker *gobreaker.CircuitBreaker
	contentBreaker  *gobreaker.CircuitBreaker
	isolateBreaker  *gobreaker.CircuitBreaker
}

type CircuitBreakerResult struct {
	Success bool
	Error   error
	State   gobreaker.State
}

func NewCircuitBreakerService() *CircuitBreakerService {
	settings := gobreaker.Settings{
		MaxRequests: 5,
		Interval:    30 * time.Second,
		Timeout:     10 * time.Second,
		ReadyToTrip: func(counts gobreaker.Counts) bool {
			return counts.ConsecutiveFailures > 3
		},
		OnStateChange: func(name string, from, to gobreaker.State) {
			log.Printf("Circuit breaker '%s' changed from %s to %s", name, from, to)
		},
		IsSuccessful: func(err error) bool {
			return err == nil
		},
	}

	return &CircuitBreakerService{
		minioBreaker:    gobreaker.NewCircuitBreaker(settings),
		rabbitmqBreaker: gobreaker.NewCircuitBreaker(settings),
		contentBreaker:  gobreaker.NewCircuitBreaker(settings),
		isolateBreaker:  gobreaker.NewCircuitBreaker(settings),
	}
}

func (cbs *CircuitBreakerService) ExecuteMinIOOperation(ctx context.Context, operation func() error) *CircuitBreakerResult {
	_, err := cbs.minioBreaker.Execute(func() (interface{}, error) {
		return nil, operation()
	})

	return &CircuitBreakerResult{
		Success: err == nil,
		Error:   err,
		State:   cbs.minioBreaker.State(),
	}
}

func (cbs *CircuitBreakerService) ExecuteRabbitMQOperation(ctx context.Context, operation func() error) *CircuitBreakerResult {
	_, err := cbs.rabbitmqBreaker.Execute(func() (interface{}, error) {
		return nil, operation()
	})

	return &CircuitBreakerResult{
		Success: err == nil,
		Error:   err,
		State:   cbs.rabbitmqBreaker.State(),
	}
}

func (cbs *CircuitBreakerService) ExecuteContentServiceOperation(ctx context.Context, operation func() error) *CircuitBreakerResult {
	_, err := cbs.contentBreaker.Execute(func() (interface{}, error) {
		return nil, operation()
	})

	return &CircuitBreakerResult{
		Success: err == nil,
		Error:   err,
		State:   cbs.contentBreaker.State(),
	}
}

func (cbs *CircuitBreakerService) ExecuteIsolateOperation(ctx context.Context, operation func() error) *CircuitBreakerResult {
	_, err := cbs.isolateBreaker.Execute(func() (interface{}, error) {
		return nil, operation()
	})

	return &CircuitBreakerResult{
		Success: err == nil,
		Error:   err,
		State:   cbs.isolateBreaker.State(),
	}
}

func (cbs *CircuitBreakerService) GetStates() map[string]gobreaker.State {
	return map[string]gobreaker.State{
		"minio":    cbs.minioBreaker.State(),
		"rabbitmq": cbs.rabbitmqBreaker.State(),
		"content":  cbs.contentBreaker.State(),
		"isolate":  cbs.isolateBreaker.State(),
	}
}

func (cbs *CircuitBreakerService) IsHealthy() bool {
	states := cbs.GetStates()
	for _, state := range states {
		if state == gobreaker.StateOpen {
			return false
		}
	}
	return true
}

func (cbs *CircuitBreakerService) Reset(name string) error {
	// gobreaker doesn't have a Reset method, so we create new breakers
	switch name {
	case "minio":
		cbs.minioBreaker = gobreaker.NewCircuitBreaker(gobreaker.Settings{
			Name:    "minio",
			Timeout: 30 * time.Second,
			ReadyToTrip: func(counts gobreaker.Counts) bool {
				return counts.ConsecutiveFailures > 5
			},
		})
	case "rabbitmq":
		cbs.rabbitmqBreaker = gobreaker.NewCircuitBreaker(gobreaker.Settings{
			Name:    "rabbitmq",
			Timeout: 30 * time.Second,
			ReadyToTrip: func(counts gobreaker.Counts) bool {
				return counts.ConsecutiveFailures > 5
			},
		})
	case "content":
		cbs.contentBreaker = gobreaker.NewCircuitBreaker(gobreaker.Settings{
			Name:    "content",
			Timeout: 30 * time.Second,
			ReadyToTrip: func(counts gobreaker.Counts) bool {
				return counts.ConsecutiveFailures > 5
			},
		})
	case "isolate":
		cbs.isolateBreaker = gobreaker.NewCircuitBreaker(gobreaker.Settings{
			Name:    "isolate",
			Timeout: 30 * time.Second,
			ReadyToTrip: func(counts gobreaker.Counts) bool {
				return counts.ConsecutiveFailures > 5
			},
		})
	default:
		return fmt.Errorf("unknown circuit breaker: %s", name)
	}
	return nil
}

// CircuitBreakerMiddleware wraps operations with circuit breaker protection
type CircuitBreakerMiddleware struct {
	service *CircuitBreakerService
}

func NewCircuitBreakerMiddleware(service *CircuitBreakerService) *CircuitBreakerMiddleware {
	return &CircuitBreakerMiddleware{
		service: service,
	}
}

func (cbm *CircuitBreakerMiddleware) WrapMinIOOperation(operation func() error) error {
	result := cbm.service.ExecuteMinIOOperation(context.Background(), operation)
	if !result.Success {
		return fmt.Errorf("minio operation failed: %w (circuit breaker state: %s)", result.Error, result.State)
	}
	return nil
}

func (cbm *CircuitBreakerMiddleware) WrapRabbitMQOperation(operation func() error) error {
	result := cbm.service.ExecuteRabbitMQOperation(context.Background(), operation)
	if !result.Success {
		return fmt.Errorf("rabbitmq operation failed: %w (circuit breaker state: %s)", result.Error, result.State)
	}
	return nil
}

func (cbm *CircuitBreakerMiddleware) WrapContentServiceOperation(operation func() error) error {
	result := cbm.service.ExecuteContentServiceOperation(context.Background(), operation)
	if !result.Success {
		return fmt.Errorf("content service operation failed: %w (circuit breaker state: %s)", result.Error, result.State)
	}
	return nil
}

func (cbm *CircuitBreakerMiddleware) WrapIsolateOperation(operation func() error) error {
	result := cbm.service.ExecuteIsolateOperation(context.Background(), operation)
	if !result.Success {
		return fmt.Errorf("isolate operation failed: %w (circuit breaker state: %s)", result.Error, result.State)
	}
	return nil
}

// Additional methods for external service integration
func (cbs *CircuitBreakerService) GetCircuitBreaker(name string) *gobreaker.CircuitBreaker {
	switch name {
	case "content-service":
		return cbs.contentBreaker
	case "minio":
		return cbs.minioBreaker
	case "rabbitmq":
		return cbs.rabbitmqBreaker
	case "isolate":
		return cbs.isolateBreaker
	default:
		// Create a new circuit breaker for unknown services
		settings := gobreaker.Settings{
			Name:        name,
			MaxRequests: 5,
			Interval:    30 * time.Second,
			Timeout:     10 * time.Second,
			ReadyToTrip: func(counts gobreaker.Counts) bool {
				return counts.ConsecutiveFailures > 3
			},
			OnStateChange: func(name string, from, to gobreaker.State) {
				log.Printf("Circuit breaker '%s' changed from %s to %s", name, from, to)
			},
			IsSuccessful: func(err error) bool {
				return err == nil
			},
		}
		return gobreaker.NewCircuitBreaker(settings)
	}
}
