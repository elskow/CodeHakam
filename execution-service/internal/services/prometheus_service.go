package services

import (
	"context"
	"net/http"
	"sync"
	"time"

	"github.com/prometheus/client_golang/prometheus"
	"github.com/prometheus/client_golang/prometheus/promhttp"
)

type PrometheusService struct {
	registry *prometheus.Registry

	// Queue metrics
	queueSizeGauge               prometheus.Gauge
	queueProcessingTimeHistogram prometheus.Histogram
	queueFailureCounter          prometheus.Counter

	// Worker metrics
	activeWorkersGauge    prometheus.Gauge
	workerHealthGauge     prometheus.Gauge
	workerRecoveryCounter prometheus.Counter

	// Execution metrics
	submissionTotalCounter   prometheus.Counter
	submissionSuccessCounter prometheus.Counter
	submissionFailureCounter prometheus.Counter
	executionTimeHistogram   prometheus.Histogram
	memoryUsageHistogram     prometheus.Histogram

	// Error metrics
	errorCounter               prometheus.Counter
	circuitBreakerTripsCounter prometheus.Counter
	isolateFailuresCounter     prometheus.Counter

	// Resource metrics
	memoryUsageGauge prometheus.Gauge
	cpuUsageGauge    prometheus.Gauge

	// HTTP metrics
	httpRequestTotal    *prometheus.CounterVec
	httpRequestDuration prometheus.Histogram
	httpResponseSize    prometheus.Histogram

	mutex sync.RWMutex
}

func NewPrometheusService() *PrometheusService {
	registry := prometheus.NewRegistry()

	return &PrometheusService{
		registry: registry,

		// Queue metrics
		queueSizeGauge: prometheus.NewGauge(
			prometheus.GaugeOpts{
				Name: "judge_queue_size",
				Help: "Current number of submissions in queue",
			},
		),
		queueProcessingTimeHistogram: prometheus.NewHistogram(
			prometheus.HistogramOpts{
				Name:    "judge_queue_processing_time_seconds",
				Help:    "Time taken to process submissions",
				Buckets: []float64{0.1, 0.5, 1, 2, 5, 10, 30, 60, 120, 300},
			},
		),
		queueFailureCounter: prometheus.NewCounter(
			prometheus.CounterOpts{
				Name: "judge_queue_failures_total",
				Help: "Total number of queue processing failures",
			},
		),

		// Worker metrics
		activeWorkersGauge: prometheus.NewGauge(
			prometheus.GaugeOpts{
				Name: "judge_active_workers",
				Help: "Number of currently active workers",
			},
		),
		workerHealthGauge: prometheus.NewGauge(
			prometheus.GaugeOpts{
				Name: "judge_worker_health",
				Help: "Health status of workers (1=healthy, 0=unhealthy)",
			},
		),
		workerRecoveryCounter: prometheus.NewCounter(
			prometheus.CounterOpts{
				Name: "judge_worker_recoveries_total",
				Help: "Total number of worker recoveries",
			},
		),

		// Execution metrics
		submissionTotalCounter: prometheus.NewCounter(
			prometheus.CounterOpts{
				Name: "judge_submissions_total",
				Help: "Total number of submissions processed",
			},
		),
		submissionSuccessCounter: prometheus.NewCounter(
			prometheus.CounterOpts{
				Name: "judge_submissions_success_total",
				Help: "Total number of successful submissions",
			},
		),
		submissionFailureCounter: prometheus.NewCounter(
			prometheus.CounterOpts{
				Name: "judge_submissions_failure_total",
				Help: "Total number of failed submissions",
			},
		),
		executionTimeHistogram: prometheus.NewHistogram(
			prometheus.HistogramOpts{
				Name:    "judge_execution_time_seconds",
				Help:    "Time taken to execute submissions",
				Buckets: []float64{0.01, 0.05, 0.1, 0.25, 0.5, 1, 2, 5, 10, 30, 60},
			},
		),
		memoryUsageHistogram: prometheus.NewHistogram(
			prometheus.HistogramOpts{
				Name:    "judge_memory_usage_kb",
				Help:    "Memory usage by submissions",
				Buckets: []float64{1024, 2048, 4096, 8192, 16384, 32768, 65536, 131072, 262144},
			},
		),

		// Error metrics
		errorCounter: prometheus.NewCounter(
			prometheus.CounterOpts{
				Name: "judge_errors_total",
				Help: "Total number of errors",
			},
		),
		circuitBreakerTripsCounter: prometheus.NewCounter(
			prometheus.CounterOpts{
				Name: "judge_circuit_breaker_trips_total",
				Help: "Total number of circuit breaker trips",
			},
		),
		isolateFailuresCounter: prometheus.NewCounter(
			prometheus.CounterOpts{
				Name: "judge_isolate_failures_total",
				Help: "Total number of Isolate sandbox failures",
			},
		),

		// Resource metrics
		memoryUsageGauge: prometheus.NewGauge(
			prometheus.GaugeOpts{
				Name: "judge_memory_usage_bytes",
				Help: "Current memory usage in bytes",
			},
		),
		cpuUsageGauge: prometheus.NewGauge(
			prometheus.GaugeOpts{
				Name: "judge_cpu_usage_percent",
				Help: "Current CPU usage percentage",
			},
		),

		// HTTP metrics
		httpRequestTotal: prometheus.NewCounterVec(
			prometheus.CounterOpts{
				Name: "http_requests_total",
				Help: "Total number of HTTP requests",
			},
			[]string{"method", "path", "status"},
		),
		httpRequestDuration: prometheus.NewHistogram(
			prometheus.HistogramOpts{
				Name:    "http_request_duration_seconds",
				Help:    "HTTP request duration",
				Buckets: []float64{0.001, 0.005, 0.01, 0.025, 0.5, 1, 2, 5, 10, 30},
			},
		),
		httpResponseSize: prometheus.NewHistogram(
			prometheus.HistogramOpts{
				Name:    "http_response_size_bytes",
				Help:    "HTTP response size in bytes",
				Buckets: []float64{100, 1000, 10000, 100000},
			},
		),
	}
}

func (pms *PrometheusService) Start(ctx context.Context) error {
	pms.mutex.Lock()
	defer pms.mutex.Unlock()

	// Register metrics with Prometheus
	pms.registry.MustRegister(pms.queueSizeGauge)
	pms.registry.MustRegister(pms.queueProcessingTimeHistogram)
	pms.registry.MustRegister(pms.queueFailureCounter)
	pms.registry.MustRegister(pms.activeWorkersGauge)
	pms.registry.MustRegister(pms.workerHealthGauge)
	pms.registry.MustRegister(pms.workerRecoveryCounter)
	pms.registry.MustRegister(pms.submissionTotalCounter)
	pms.registry.MustRegister(pms.submissionSuccessCounter)
	pms.registry.MustRegister(pms.submissionFailureCounter)
	pms.registry.MustRegister(pms.executionTimeHistogram)
	pms.registry.MustRegister(pms.memoryUsageHistogram)
	pms.registry.MustRegister(pms.errorCounter)
	pms.registry.MustRegister(pms.circuitBreakerTripsCounter)
	pms.registry.MustRegister(pms.isolateFailuresCounter)
	pms.registry.MustRegister(pms.memoryUsageGauge)
	pms.registry.MustRegister(pms.cpuUsageGauge)
	pms.registry.MustRegister(pms.httpRequestTotal)
	pms.registry.MustRegister(pms.httpRequestDuration)
	pms.registry.MustRegister(pms.httpResponseSize)

	return nil
}

func (pms *PrometheusService) Stop() {
	pms.mutex.Lock()
	defer pms.mutex.Unlock()
}

func (pms *PrometheusService) RecordQueueSize(size int) {
	pms.queueSizeGauge.Set(float64(size))
}

func (pms *PrometheusService) RecordQueueProcessingTime(duration time.Duration) {
	pms.queueProcessingTimeHistogram.Observe(duration.Seconds())
}

func (pms *PrometheusService) RecordQueueFailure() {
	pms.queueFailureCounter.Inc()
}

func (pms *PrometheusService) RecordActiveWorkers(count int) {
	pms.activeWorkersGauge.Set(float64(count))
}

func (pms *PrometheusService) RecordWorkerHealth(healthy bool) {
	if healthy {
		pms.workerHealthGauge.Set(1)
	} else {
		pms.workerHealthGauge.Set(0)
	}
}

func (pms *PrometheusService) RecordWorkerRecovery() {
	pms.workerRecoveryCounter.Inc()
}

func (pms *PrometheusService) RecordSubmissionStart() {
	pms.submissionTotalCounter.Inc()
}

func (pms *PrometheusService) RecordSubmissionSuccess() {
	pms.submissionSuccessCounter.Inc()
}

func (pms *PrometheusService) RecordSubmissionFailure() {
	pms.submissionFailureCounter.Inc()
}

func (pms *PrometheusService) RecordExecutionTime(duration time.Duration) {
	pms.executionTimeHistogram.Observe(duration.Seconds())
}

func (pms *PrometheusService) RecordMemoryUsageKB(kb int) {
	pms.memoryUsageHistogram.Observe(float64(kb))
}

func (pms *PrometheusService) RecordError() {
	pms.errorCounter.Inc()
}

func (pms *PrometheusService) RecordCircuitBreakerTrip(service string) {
	pms.circuitBreakerTripsCounter.Inc()
}

func (pms *PrometheusService) RecordIsolateFailure() {
	pms.isolateFailuresCounter.Inc()
}

func (pms *PrometheusService) RecordMemoryUsageBytes(bytes int64) {
	pms.memoryUsageGauge.Set(float64(bytes))
}

func (pms *PrometheusService) RecordCPUUsage(percent float64) {
	pms.cpuUsageGauge.Set(percent)
}

func (pms *PrometheusService) RecordHTTPRequest(method, path string, statusCode int, duration time.Duration, responseSize int) {
	pms.httpRequestTotal.WithLabelValues(method, path, string(rune(statusCode))).Inc()
	pms.httpRequestDuration.Observe(duration.Seconds())
	pms.httpResponseSize.Observe(float64(responseSize))
}

func (pms *PrometheusService) GetHandler() http.Handler {
	return promhttp.HandlerFor(pms.registry, promhttp.HandlerOpts{})
}
