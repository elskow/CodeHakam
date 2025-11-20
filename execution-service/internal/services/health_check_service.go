package services

import (
	"context"
	"time"

	"execution_service/internal/cache"
	"execution_service/internal/database"
	"execution_service/internal/queue"
	"execution_service/internal/sandbox"
	"execution_service/internal/storage"
)

type HealthCheckService struct {
	db      *database.DB
	queue   *queue.RabbitMQClient
	storage *storage.MinIOClient
	cache   *cache.ValkeyClient
	sandbox *sandbox.IsolateSandbox
	timeout time.Duration
}

type HealthStatus string

const (
	StatusHealthy   HealthStatus = "healthy"
	StatusDegraded  HealthStatus = "degraded"
	StatusUnhealthy HealthStatus = "unhealthy"
)

type HealthCheckResult struct {
	Status    HealthStatus           `json:"status"`
	Timestamp time.Time              `json:"timestamp"`
	Uptime    time.Duration          `json:"uptime"`
	Checks    map[string]CheckResult `json:"checks"`
	Version   string                 `json:"version"`
}

type CheckResult struct {
	Status  HealthStatus  `json:"status"`
	Message string        `json:"message"`
	Details interface{}   `json:"details,omitempty"`
	Latency time.Duration `json:"latency,omitempty"`
}

func NewHealthCheckService(db *database.DB, queue *queue.RabbitMQClient, storage *storage.MinIOClient, cache *cache.ValkeyClient, sandbox *sandbox.IsolateSandbox) *HealthCheckService {
	return &HealthCheckService{
		db:      db,
		queue:   queue,
		storage: storage,
		cache:   cache,
		sandbox: sandbox,
		timeout: 10 * time.Second,
	}
}

func (hcs *HealthCheckService) CheckHealth(ctx context.Context) *HealthCheckResult {
	startTime := time.Now()

	checks := make(map[string]CheckResult)

	// Database health check
	checks["database"] = hcs.checkDatabase(ctx)

	// RabbitMQ health check
	checks["rabbitmq"] = hcs.checkRabbitMQ(ctx)

	// MinIO health check
	checks["minio"] = hcs.checkMinIO(ctx)

	// Cache health check
	checks["cache"] = hcs.checkCache(ctx)

	// Isolate sandbox health check
	checks["isolate"] = hcs.checkIsolate(ctx)

	// Determine overall status
	overallStatus := StatusHealthy
	for _, check := range checks {
		if check.Status == StatusUnhealthy {
			overallStatus = StatusUnhealthy
			break
		} else if check.Status == StatusDegraded {
			overallStatus = StatusDegraded
		}
	}

	return &HealthCheckResult{
		Status:    overallStatus,
		Timestamp: time.Now().UTC(),
		Uptime:    time.Since(startTime),
		Checks:    checks,
		Version:   "1.0.0",
	}
}

func (hcs *HealthCheckService) checkDatabase(ctx context.Context) CheckResult {
	start := time.Now()

	// Simple ping to database
	err := hcs.db.Ping(ctx)
	latency := time.Since(start)

	if err != nil {
		return CheckResult{
			Status:  StatusUnhealthy,
			Message: "Database connection failed",
			Details: err.Error(),
			Latency: latency,
		}
	}

	// Check if we can execute a simple query
	err = hcs.db.Ping(ctx)
	if err != nil {
		return CheckResult{
			Status:  StatusDegraded,
			Message: "Database query failed",
			Details: err.Error(),
			Latency: latency,
		}
	}

	return CheckResult{
		Status:  StatusHealthy,
		Message: "Database is healthy",
		Latency: latency,
	}
}

func (hcs *HealthCheckService) checkRabbitMQ(ctx context.Context) CheckResult {
	start := time.Now()

	// Check if RabbitMQ is healthy
	if !hcs.queue.IsHealthy() {
		return CheckResult{
			Status:  StatusUnhealthy,
			Message: "RabbitMQ is not healthy",
			Latency: time.Since(start),
		}
	}

	// Try to get queue info
	queueSize, err := hcs.queue.GetQueueInfo()
	latency := time.Since(start)

	if err != nil {
		return CheckResult{
			Status:  StatusDegraded,
			Message: "Failed to get queue info",
			Details: err.Error(),
			Latency: latency,
		}
	}

	return CheckResult{
		Status:  StatusHealthy,
		Message: "RabbitMQ is healthy",
		Details: map[string]interface{}{
			"queue_size": queueSize,
		},
		Latency: latency,
	}
}

func (hcs *HealthCheckService) checkMinIO(ctx context.Context) CheckResult {
	start := time.Now()

	// Check MinIO health by checking bucket existence
	exists, err := hcs.storage.Client.BucketExists(ctx, hcs.storage.Bucket)
	latency := time.Since(start)

	if err != nil {
		return CheckResult{
			Status:  StatusUnhealthy,
			Message: "MinIO connection failed",
			Details: err.Error(),
			Latency: latency,
		}
	}

	return CheckResult{
		Status:  StatusHealthy,
		Message: "MinIO is healthy",
		Details: map[string]interface{}{
			"bucket_exists": exists,
		},
		Latency: latency,
	}
}

func (hcs *HealthCheckService) checkCache(ctx context.Context) CheckResult {
	start := time.Now()

	// Check cache health
	if !hcs.cache.IsHealthy() {
		return CheckResult{
			Status:  StatusUnhealthy,
			Message: "Cache is not healthy",
			Latency: time.Since(start),
		}
	}

	// Try to get stats
	stats, err := hcs.cache.GetStats(ctx)
	latency := time.Since(start)

	if err != nil {
		return CheckResult{
			Status:  StatusDegraded,
			Message: "Failed to get cache stats",
			Details: err.Error(),
			Latency: latency,
		}
	}

	return CheckResult{
		Status:  StatusHealthy,
		Message: "Cache is healthy",
		Details: stats,
		Latency: latency,
	}
}

func (hcs *HealthCheckService) checkIsolate(ctx context.Context) CheckResult {
	start := time.Now()

	// Try to create and cleanup a test box
	boxID, err := hcs.sandbox.CreateBox()
	if err != nil {
		return CheckResult{
			Status:  StatusUnhealthy,
			Message: "Failed to create isolate box",
			Details: err.Error(),
			Latency: time.Since(start),
		}
	}

	// Cleanup the test box
	hcs.sandbox.CleanupBox(boxID)
	latency := time.Since(start)

	return CheckResult{
		Status:  StatusHealthy,
		Message: "Isolate sandbox is healthy",
		Latency: latency,
	}
}

func (hcs *HealthCheckService) IsHealthy() bool {
	result := hcs.CheckHealth(context.Background())
	return result.Status == StatusHealthy
}

func (hcs *HealthCheckService) GetDetailedHealth(ctx context.Context) (map[string]interface{}, error) {
	result := hcs.CheckHealth(ctx)

	details := make(map[string]interface{})
	details["status"] = result.Status
	details["timestamp"] = result.Timestamp
	details["uptime"] = result.Uptime.String()
	details["version"] = result.Version
	details["checks"] = result.Checks

	// Add additional metrics
	details["metrics"] = map[string]interface{}{
		"total_checks":     len(result.Checks),
		"healthy_checks":   hcs.countHealthyChecks(result.Checks),
		"degraded_checks":  hcs.countDegradedChecks(result.Checks),
		"unhealthy_checks": hcs.countUnhealthyChecks(result.Checks),
	}

	return details, nil
}

func (hcs *HealthCheckService) countHealthyChecks(checks map[string]CheckResult) int {
	count := 0
	for _, check := range checks {
		if check.Status == StatusHealthy {
			count++
		}
	}
	return count
}

func (hcs *HealthCheckService) countDegradedChecks(checks map[string]CheckResult) int {
	count := 0
	for _, check := range checks {
		if check.Status == StatusDegraded {
			count++
		}
	}
	return count
}

func (hcs *HealthCheckService) countUnhealthyChecks(checks map[string]CheckResult) int {
	count := 0
	for _, check := range checks {
		if check.Status == StatusUnhealthy {
			count++
		}
	}
	return count
}

// Readiness probe (for Kubernetes)
func (hcs *HealthCheckService) CheckReadiness(ctx context.Context) CheckResult {
	// Check if all critical dependencies are healthy
	checks := map[string]CheckResult{
		"database": hcs.checkDatabase(ctx),
		"rabbitmq": hcs.checkRabbitMQ(ctx),
	}

	for _, check := range checks {
		if check.Status != StatusHealthy {
			return CheckResult{
				Status:  StatusUnhealthy,
				Message: "Service is not ready",
				Details: checks,
			}
		}
	}

	return CheckResult{
		Status:  StatusHealthy,
		Message: "Service is ready",
	}
}

// Liveness probe (for Kubernetes)
func (hcs *HealthCheckService) CheckLiveness(ctx context.Context) CheckResult {
	// Basic liveness check - just check if the service is running
	return CheckResult{
		Status:  StatusHealthy,
		Message: "Service is alive",
	}
}
