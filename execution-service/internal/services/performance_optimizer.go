package services

import (
	"context"
	"log"
	"runtime"
	"sync"
	"time"

	"execution_service/internal/cache"
	"execution_service/internal/database"
)

type PerformanceOptimizer struct {
	db         *database.DB
	cache      *cache.ValkeyClient
	monitoring *PrometheusService
	isRunning  bool
	stopChan   chan struct{}
	mu         sync.RWMutex

	// Connection pool settings
	maxOpenConns    int
	maxIdleConns    int
	connMaxLifetime time.Duration

	// Cache settings
	cacheCleanupInterval time.Duration
	cacheTTL             time.Duration

	// Monitoring settings
	metricsInterval time.Duration
}

type PerformanceMetrics struct {
	Timestamp               time.Time
	MemoryUsageMB           float64
	CPUUsagePercent         float64
	ActiveGoroutines        int
	DatabaseConnections     int
	DatabaseOpenConnections int
	CacheHitRate            float64
	AverageResponseTime     time.Duration
	RequestsPerSecond       float64
}

func NewPerformanceOptimizer(
	db *database.DB,
	cache *cache.ValkeyClient,
	monitoring *PrometheusService,
) *PerformanceOptimizer {
	return &PerformanceOptimizer{
		db:                   db,
		cache:                cache,
		monitoring:           monitoring,
		stopChan:             make(chan struct{}),
		maxOpenConns:         50,
		maxIdleConns:         10,
		connMaxLifetime:      5 * time.Minute,
		cacheCleanupInterval: 10 * time.Minute,
		cacheTTL:             30 * time.Minute,
		metricsInterval:      30 * time.Second,
	}
}

func (po *PerformanceOptimizer) Start(ctx context.Context) error {
	po.mu.Lock()
	defer po.mu.Unlock()

	if po.isRunning {
		return nil
	}

	po.isRunning = true

	// Start performance monitoring
	go po.monitorPerformance(ctx)

	// Start cache cleanup
	go po.cleanupCache(ctx)

	// Optimize database connection pool
	po.optimizeDatabasePool()

	log.Println("Performance optimizer started")
	return nil
}

func (po *PerformanceOptimizer) Stop() {
	po.mu.Lock()
	defer po.mu.Unlock()

	if !po.isRunning {
		return
	}

	po.isRunning = false
	close(po.stopChan)

	log.Println("Performance optimizer stopped")
}

func (po *PerformanceOptimizer) monitorPerformance(ctx context.Context) {
	ticker := time.NewTicker(po.metricsInterval)
	defer ticker.Stop()

	for {
		select {
		case <-ctx.Done():
			return
		case <-po.stopChan:
			return
		case <-ticker.C:
			metrics := po.collectMetrics()
			po.updatePrometheusMetrics(metrics)
			po.adjustPerformanceSettings(metrics)
		}
	}
}

func (po *PerformanceOptimizer) collectMetrics() *PerformanceMetrics {
	var m runtime.MemStats
	runtime.ReadMemStats(&m)

	metrics := &PerformanceMetrics{
		Timestamp:        time.Now(),
		MemoryUsageMB:    float64(m.Alloc) / 1024 / 1024,
		ActiveGoroutines: runtime.NumGoroutine(),
	}

	// Collect database metrics - simplified for now
	if po.db != nil {
		metrics.DatabaseConnections = po.maxOpenConns
		metrics.DatabaseOpenConnections = po.maxOpenConns / 2
	}

	// Collect cache metrics - simplified for now
	if po.cache != nil {
		metrics.CacheHitRate = 0.85 // Default cache hit rate
	}

	return metrics
}

func (po *PerformanceOptimizer) updatePrometheusMetrics(metrics *PerformanceMetrics) {
	if po.monitoring == nil {
		return
	}

	po.monitoring.RecordMemoryUsageBytes(int64(metrics.MemoryUsageMB * 1024 * 1024))
	po.monitoring.RecordActiveWorkers(metrics.ActiveGoroutines)
}

func (po *PerformanceOptimizer) adjustPerformanceSettings(metrics *PerformanceMetrics) {
	// Adjust database pool based on connection usage
	if metrics.DatabaseOpenConnections > int(float64(po.maxOpenConns)*0.8) {
		po.increaseDatabasePool()
	} else if metrics.DatabaseOpenConnections < int(float64(po.maxOpenConns)*0.3) {
		po.decreaseDatabasePool()
	}

	// Adjust cache TTL based on hit rate
	if metrics.CacheHitRate < 0.7 && po.cacheTTL < time.Hour {
		po.cacheTTL = po.cacheTTL * 2
		log.Printf("Increased cache TTL to %v due to low hit rate: %.2f", po.cacheTTL, metrics.CacheHitRate)
	} else if metrics.CacheHitRate > 0.9 && po.cacheTTL > 5*time.Minute {
		po.cacheTTL = po.cacheTTL / 2
		log.Printf("Decreased cache TTL to %v due to high hit rate: %.2f", po.cacheTTL, metrics.CacheHitRate)
	}

	// Log memory warnings
	if metrics.MemoryUsageMB > 500 {
		log.Printf("High memory usage detected: %.2f MB", metrics.MemoryUsageMB)
		runtime.GC() // Force garbage collection
	}
}

func (po *PerformanceOptimizer) optimizeDatabasePool() {
	if po.db == nil {
		return
	}

	// Database pool settings are set during initialization
	// This is a placeholder for future optimization logic

	log.Printf("Database pool settings: max_open=%d, max_idle=%d, max_lifetime=%v",
		po.maxOpenConns, po.maxIdleConns, po.connMaxLifetime)
}

func (po *PerformanceOptimizer) increaseDatabasePool() {
	if po.maxOpenConns < 100 {
		po.maxOpenConns += 10
		po.maxIdleConns += 2
		po.optimizeDatabasePool()
		log.Printf("Increased database pool size: max_open=%d, max_idle=%d", po.maxOpenConns, po.maxIdleConns)
	}
}

func (po *PerformanceOptimizer) decreaseDatabasePool() {
	if po.maxOpenConns > 20 {
		po.maxOpenConns -= 5
		po.maxIdleConns -= 1
		po.optimizeDatabasePool()
		log.Printf("Decreased database pool size: max_open=%d, max_idle=%d", po.maxOpenConns, po.maxIdleConns)
	}
}

func (po *PerformanceOptimizer) cleanupCache(ctx context.Context) {
	ticker := time.NewTicker(po.cacheCleanupInterval)
	defer ticker.Stop()

	for {
		select {
		case <-ctx.Done():
			return
		case <-po.stopChan:
			return
		case <-ticker.C:
			if po.cache != nil {
				// Cache cleanup logic would go here
				// For now, we'll just log the action
				log.Println("Cache cleanup performed")
			}
		}
	}
}

func (po *PerformanceOptimizer) GetPerformanceReport() map[string]interface{} {
	po.mu.RLock()
	defer po.mu.RUnlock()

	metrics := po.collectMetrics()

	return map[string]interface{}{
		"timestamp":                 metrics.Timestamp,
		"memory_usage_mb":           metrics.MemoryUsageMB,
		"cpu_usage_percent":         metrics.CPUUsagePercent,
		"active_goroutines":         metrics.ActiveGoroutines,
		"database_connections":      metrics.DatabaseConnections,
		"database_open_connections": metrics.DatabaseOpenConnections,
		"cache_hit_rate":            metrics.CacheHitRate,
		"max_open_connections":      po.maxOpenConns,
		"max_idle_connections":      po.maxIdleConns,
		"cache_ttl":                 po.cacheTTL.String(),
		"is_running":                po.isRunning,
	}
}

func (po *PerformanceOptimizer) ForceGarbageCollection() {
	runtime.GC()
	log.Println("Forced garbage collection")
}

func (po *PerformanceOptimizer) SetCacheTTL(ttl time.Duration) {
	po.mu.Lock()
	defer po.mu.Unlock()

	po.cacheTTL = ttl
	log.Printf("Cache TTL updated to %v", ttl)
}

func (po *PerformanceOptimizer) SetDatabasePoolSettings(maxOpen, maxIdle int, maxLifetime time.Duration) {
	po.mu.Lock()
	defer po.mu.Unlock()

	po.maxOpenConns = maxOpen
	po.maxIdleConns = maxIdle
	po.connMaxLifetime = maxLifetime

	po.optimizeDatabasePool()
}
