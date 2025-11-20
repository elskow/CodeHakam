package services

import (
	"context"
	"fmt"
	"net/http"
	"strconv"
	"sync"
	"time"

	"execution_service/internal/cache"
)

type RateLimitService struct {
	cache  *cache.ValkeyClient
	limits map[string]*RateLimitConfig
	mutex  sync.RWMutex
}

type RateLimitConfig struct {
	RequestsPerMinute int
	BurstSize         int
	WindowDuration    time.Duration
	KeyPrefix         string
}

type RateLimitResult struct {
	Allowed   bool
	Remaining int
	ResetTime time.Time
	Headers   map[string]string
}

func NewRateLimitService(cache *cache.ValkeyClient) *RateLimitService {
	return &RateLimitService{
		cache:  cache,
		limits: make(map[string]*RateLimitConfig),
		mutex:  sync.RWMutex{},
	}
}

func (rls *RateLimitService) AddRateLimit(key string, requestsPerMinute, burstSize int) {
	rls.mutex.Lock()
	defer rls.mutex.Unlock()

	rls.limits[key] = &RateLimitConfig{
		RequestsPerMinute: requestsPerMinute,
		BurstSize:         burstSize,
		WindowDuration:    time.Minute,
		KeyPrefix:         "rate_limit:",
	}
}

func (rls *RateLimitService) CheckRateLimit(ctx context.Context, key string, identifier string) *RateLimitResult {
	rls.mutex.RLock()
	config, exists := rls.limits[key]
	rls.mutex.RUnlock()

	if !exists {
		// Default rate limit if not configured
		config = &RateLimitConfig{
			RequestsPerMinute: 60,
			BurstSize:         10,
			WindowDuration:    time.Minute,
			KeyPrefix:         "rate_limit:",
		}
	}

	cacheKey := fmt.Sprintf("%s%s:%s", config.KeyPrefix, key, identifier)

	// Get current count from cache
	currentCount, err := rls.getCurrentCount(ctx, cacheKey)
	if err != nil {
		// Log error but allow request (fail open)
		return &RateLimitResult{
			Allowed:   true,
			Remaining: config.RequestsPerMinute,
			ResetTime: time.Now().Add(config.WindowDuration),
			Headers:   rls.getRateLimitHeaders(config.RequestsPerMinute, config.RequestsPerMinute),
		}
	}

	remaining := config.RequestsPerMinute - currentCount
	resetTime := rls.getResetTime(ctx, cacheKey, config.WindowDuration)

	if currentCount >= config.RequestsPerMinute {
		return &RateLimitResult{
			Allowed:   false,
			Remaining: 0,
			ResetTime: resetTime,
			Headers:   rls.getRateLimitHeaders(0, config.RequestsPerMinute),
		}
	}

	// Increment counter
	rls.incrementCounter(ctx, cacheKey)

	return &RateLimitResult{
		Allowed:   true,
		Remaining: remaining - 1,
		ResetTime: resetTime,
		Headers:   rls.getRateLimitHeaders(remaining-1, config.RequestsPerMinute),
	}
}

func (rls *RateLimitService) getCurrentCount(ctx context.Context, key string) (int, error) {
	countStr, err := rls.cache.GetCachedString(ctx, key)
	if err != nil {
		return 0, nil
	}

	count, err := strconv.Atoi(countStr)
	if err != nil {
		return 0, nil
	}

	return count, nil
}

func (rls *RateLimitService) incrementCounter(ctx context.Context, key string) {
	currentCount, _ := rls.getCurrentCount(ctx, key)
	newCount := currentCount + 1

	// Set with expiration
	rls.cache.CacheString(ctx, key, strconv.Itoa(newCount), time.Minute)
}

func (rls *RateLimitService) getResetTime(ctx context.Context, key string, windowDuration time.Duration) time.Time {
	// For simplicity, we'll use a fixed reset time based on window duration
	// In a real implementation, you'd store the first request timestamp
	return time.Now().Add(windowDuration)
}

func (rls *RateLimitService) getRateLimitHeaders(remaining, limit int) map[string]string {
	return map[string]string{
		"X-RateLimit-Limit":     strconv.Itoa(limit),
		"X-RateLimit-Remaining": strconv.Itoa(remaining),
		"X-RateLimit-Reset":     time.Now().Add(time.Minute).Format(time.RFC3339),
	}
}

// HTTP middleware for rate limiting
type RateLimitMiddleware struct {
	service *RateLimitService
}

func NewRateLimitMiddleware(service *RateLimitService) *RateLimitMiddleware {
	return &RateLimitMiddleware{
		service: service,
	}
}

func (rlm *RateLimitMiddleware) Middleware(key string) func(http.Handler) http.Handler {
	return func(next http.Handler) http.Handler {
		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			identifier := r.RemoteAddr
			if xff := r.Header.Get("X-Forwarded-For"); xff != "" {
				identifier = xff
			}

			result := rlm.service.CheckRateLimit(r.Context(), key, identifier)

			if !result.Allowed {
				w.Header().Set("Content-Type", "application/json")
				w.WriteHeader(http.StatusTooManyRequests)
				w.Write([]byte(`{"error":"Rate limit exceeded"}`))
				return
			}

			// Add rate limit headers
			for key, value := range result.Headers {
				w.Header().Set(key, value)
			}

			next.ServeHTTP(w, r)
		})
	}
}

func (rlm *RateLimitMiddleware) MiddlewareWithCustomKey(keyFunc func(r *http.Request) string) func(http.Handler) http.Handler {
	return func(next http.Handler) http.Handler {
		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			key := keyFunc(r)
			identifier := r.RemoteAddr
			if xff := r.Header.Get("X-Forwarded-For"); xff != "" {
				identifier = xff
			}

			result := rlm.service.CheckRateLimit(r.Context(), key, identifier)

			if !result.Allowed {
				w.Header().Set("Content-Type", "application/json")
				w.WriteHeader(http.StatusTooManyRequests)
				w.Write([]byte(`{"error":"Rate limit exceeded"}`))
				return
			}

			// Add rate limit headers
			for key, value := range result.Headers {
				w.Header().Set(key, value)
			}

			next.ServeHTTP(w, r)
		})
	}
}

// IP-based rate limiting
func (rlm *RateLimitMiddleware) IPRateLimit(requestsPerMinute, burstSize int) func(http.Handler) http.Handler {
	return rlm.MiddlewareWithCustomKey(func(r *http.Request) string {
		return r.RemoteAddr
	})
}

// User-based rate limiting
func (rlm *RateLimitMiddleware) UserRateLimit(requestsPerMinute, burstSize int) func(http.Handler) http.Handler {
	return rlm.MiddlewareWithCustomKey(func(r *http.Request) string {
		// Extract user ID from JWT token or session
		if userID := r.Header.Get("X-User-ID"); userID != "" {
			return "user:" + userID
		}
		return r.RemoteAddr
	})
}

// Endpoint-based rate limiting
func (rlm *RateLimitMiddleware) EndpointRateLimit(endpoint string, requestsPerMinute, burstSize int) func(http.Handler) http.Handler {
	return rlm.Middleware(endpoint)
}
