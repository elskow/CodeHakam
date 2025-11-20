package services

import (
	"net/http"
	"time"

	"github.com/prometheus/client_golang/prometheus"
	"github.com/prometheus/client_golang/prometheus/promhttp"
)

type MetricsService struct {
	registry *prometheus.Registry

	// Judge metrics
	queueSize          *prometheus.GaugeVec
	activeWorkers      *prometheus.GaugeVec
	workerHealth       *prometheus.GaugeVec
	submissionTotal    *prometheus.CounterVec
	submissionDuration *prometheus.HistogramVec
	submissionVerdicts *prometheus.CounterVec

	// Performance metrics
	executionTime   *prometheus.HistogramVec
	memoryUsage     *prometheus.HistogramVec
	compilationTime *prometheus.HistogramVec

	// System metrics
	circuitBreakerState *prometheus.GaugeVec
	sandboxOperations   *prometheus.CounterVec
	storageOperations   *prometheus.CounterVec

	// Error metrics
	errorTotal         *prometheus.CounterVec
	securityViolations *prometheus.CounterVec
}

func NewMetricsService() *MetricsService {
	registry := prometheus.NewRegistry()

	ms := &MetricsService{
		registry: registry,

		queueSize: prometheus.NewGaugeVec(
			prometheus.GaugeOpts{
				Name: "judge_queue_size",
				Help: "Current number of submissions in judge queue",
			},
			[]string{"priority"},
		),

		activeWorkers: prometheus.NewGaugeVec(
			prometheus.GaugeOpts{
				Name: "judge_workers_active",
				Help: "Number of active judge workers",
			},
			[]string{"status"},
		),

		workerHealth: prometheus.NewGaugeVec(
			prometheus.GaugeOpts{
				Name: "judge_workers_health",
				Help: "Health status of judge workers (1=healthy, 0=unhealthy)",
			},
			[]string{"worker_id"},
		),

		submissionTotal: prometheus.NewCounterVec(
			prometheus.CounterOpts{
				Name: "judge_submissions_total",
				Help: "Total number of submissions processed",
			},
			[]string{"language", "status"},
		),

		submissionDuration: prometheus.NewHistogramVec(
			prometheus.HistogramOpts{
				Name:    "judge_submission_duration_seconds",
				Help:    "Time taken to process submissions",
				Buckets: prometheus.DefBuckets,
			},
			[]string{"language", "verdict"},
		),

		submissionVerdicts: prometheus.NewCounterVec(
			prometheus.CounterOpts{
				Name: "judge_submissions_verdicts_total",
				Help: "Number of submissions by verdict",
			},
			[]string{"verdict", "language"},
		),

		executionTime: prometheus.NewHistogramVec(
			prometheus.HistogramOpts{
				Name:    "judge_execution_time_milliseconds",
				Help:    "Execution time of test cases",
				Buckets: []float64{10, 50, 100, 250, 500, 1000, 2000, 5000, 10000, 30000},
			},
			[]string{"language"},
		),

		memoryUsage: prometheus.NewHistogramVec(
			prometheus.HistogramOpts{
				Name:    "judge_memory_usage_kb",
				Help:    "Memory usage of submissions",
				Buckets: []float64{1024, 4096, 16384, 65536, 262144, 524288, 1048576},
			},
			[]string{"language"},
		),

		compilationTime: prometheus.NewHistogramVec(
			prometheus.HistogramOpts{
				Name:    "judge_compilation_time_milliseconds",
				Help:    "Compilation time of submissions",
				Buckets: []float64{100, 250, 500, 1000, 2000, 5000, 10000, 30000},
			},
			[]string{"language"},
		),

		circuitBreakerState: prometheus.NewGaugeVec(
			prometheus.GaugeOpts{
				Name: "judge_circuit_breaker_state",
				Help: "State of circuit breakers (1=closed, 0=open)",
			},
			[]string{"service"},
		),

		sandboxOperations: prometheus.NewCounterVec(
			prometheus.CounterOpts{
				Name: "judge_sandbox_operations_total",
				Help: "Number of sandbox operations",
			},
			[]string{"operation", "result"},
		),

		storageOperations: prometheus.NewCounterVec(
			prometheus.CounterOpts{
				Name: "judge_storage_operations_total",
				Help: "Number of storage operations",
			},
			[]string{"operation", "result"},
		),

		errorTotal: prometheus.NewCounterVec(
			prometheus.CounterOpts{
				Name: "judge_errors_total",
				Help: "Number of errors in judge service",
			},
			[]string{"component", "error_type"},
		),

		securityViolations: prometheus.NewCounterVec(
			prometheus.CounterOpts{
				Name: "judge_security_violations_total",
				Help: "Number of security violations detected",
			},
			[]string{"violation_type", "severity"},
		),
	}

	// Register all metrics
	registry.MustRegister(
		ms.queueSize,
		ms.activeWorkers,
		ms.workerHealth,
		ms.submissionTotal,
		ms.submissionDuration,
		ms.submissionVerdicts,
		ms.executionTime,
		ms.memoryUsage,
		ms.compilationTime,
		ms.circuitBreakerState,
		ms.sandboxOperations,
		ms.storageOperations,
		ms.errorTotal,
		ms.securityViolations,
	)

	return ms
}

// Metrics recording methods
func (ms *MetricsService) RecordQueueSize(priority string, size float64) {
	ms.queueSize.WithLabelValues(priority).Set(size)
}

func (ms *MetricsService) RecordActiveWorkers(status string, count float64) {
	ms.activeWorkers.WithLabelValues(status).Set(count)
}

func (ms *MetricsService) RecordWorkerHealth(workerID string, healthy float64) {
	ms.workerHealth.WithLabelValues(workerID).Set(healthy)
}

func (ms *MetricsService) RecordSubmission(language, status string) {
	ms.submissionTotal.WithLabelValues(language, status).Inc()
}

func (ms *MetricsService) RecordSubmissionDuration(language, verdict string, duration time.Duration) {
	ms.submissionDuration.WithLabelValues(language, verdict).Observe(duration.Seconds())
}

func (ms *MetricsService) RecordSubmissionVerdict(verdict, language string) {
	ms.submissionVerdicts.WithLabelValues(verdict, language).Inc()
}

func (ms *MetricsService) RecordExecutionTime(language string, timeMs float64) {
	ms.executionTime.WithLabelValues(language).Observe(timeMs)
}

func (ms *MetricsService) RecordMemoryUsage(language string, memoryKb float64) {
	ms.memoryUsage.WithLabelValues(language).Observe(memoryKb)
}

func (ms *MetricsService) RecordCompilationTime(language string, timeMs float64) {
	ms.compilationTime.WithLabelValues(language).Observe(timeMs)
}

func (ms *MetricsService) RecordCircuitBreakerState(service string, state float64) {
	ms.circuitBreakerState.WithLabelValues(service).Set(state)
}

func (ms *MetricsService) RecordSandboxOperation(operation, result string) {
	ms.sandboxOperations.WithLabelValues(operation, result).Inc()
}

func (ms *MetricsService) RecordStorageOperation(operation, result string) {
	ms.storageOperations.WithLabelValues(operation, result).Inc()
}

func (ms *MetricsService) RecordError(component, errorType string) {
	ms.errorTotal.WithLabelValues(component, errorType).Inc()
}

func (ms *MetricsService) RecordSecurityViolation(violationType, severity string) {
	ms.securityViolations.WithLabelValues(violationType, severity).Inc()
}

// HTTP handler for Prometheus metrics
func (ms *MetricsService) Handler() http.Handler {
	return promhttp.Handler()
}

// Get registry for custom metrics
func (ms *MetricsService) GetRegistry() *prometheus.Registry {
	return ms.registry
}
