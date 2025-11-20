package services

import (
	"context"
	"fmt"
	"log"
	"net/http"
	"os"
	"time"

	"github.com/google/uuid"
)

type StructuredLogger struct {
	serviceName string
	level       LogLevel
	output      *os.File
}

type LogLevel int

const (
	DEBUG LogLevel = iota
	INFO
	WARN
	ERROR
	FATAL
)

type LogEntry struct {
	Timestamp     time.Time              `json:"timestamp"`
	Level         string                 `json:"level"`
	Service       string                 `json:"service"`
	Message       string                 `json:"message"`
	CorrelationID string                 `json:"correlation_id,omitempty"`
	UserID        *int64                 `json:"user_id,omitempty"`
	RequestID     string                 `json:"request_id,omitempty"`
	Method        string                 `json:"method,omitempty"`
	Path          string                 `json:"path,omitempty"`
	StatusCode    int                    `json:"status_code,omitempty"`
	Duration      time.Duration          `json:"duration,omitempty"`
	Error         string                 `json:"error,omitempty"`
	Metadata      map[string]interface{} `json:"metadata,omitempty"`
}

func NewStructuredLogger(serviceName string, level LogLevel) *StructuredLogger {
	return &StructuredLogger{
		serviceName: serviceName,
		level:       level,
		output:      os.Stdout,
	}
}

func (sl *StructuredLogger) WithContext(ctx context.Context) *LogContext {
	correlationID := getCorrelationID(ctx)
	return &LogContext{
		logger:        sl,
		correlationID: correlationID,
		context:       ctx,
	}
}

func (sl *StructuredLogger) Log(level LogLevel, message string, fields map[string]interface{}) {
	if level < sl.level {
		return
	}

	entry := LogEntry{
		Timestamp: time.Now().UTC(),
		Level:     sl.levelToString(level),
		Service:   sl.serviceName,
		Message:   message,
		Metadata:  fields,
	}

	// Add correlation ID if available
	if ctx := context.Background(); ctx != nil {
		if correlationID := getCorrelationID(ctx); correlationID != "" {
			entry.CorrelationID = correlationID
		}
	}

	sl.outputLog(entry)
}

func (sl *StructuredLogger) Debug(message string, fields ...map[string]interface{}) {
	sl.Log(DEBUG, message, mergeFields(fields...))
}

func (sl *StructuredLogger) Info(message string, fields ...map[string]interface{}) {
	sl.Log(INFO, message, mergeFields(fields...))
}

func (sl *StructuredLogger) Warn(message string, fields ...map[string]interface{}) {
	sl.Log(WARN, message, mergeFields(fields...))
}

func (sl *StructuredLogger) Error(message string, fields ...map[string]interface{}) {
	sl.Log(ERROR, message, mergeFields(fields...))
}

func (sl *StructuredLogger) Fatal(message string, fields ...map[string]interface{}) {
	sl.Log(FATAL, message, mergeFields(fields...))
	os.Exit(1)
}

func (sl *StructuredLogger) outputLog(entry LogEntry) {
	logMessage := fmt.Sprintf("[%s] %s - %s",
		entry.Timestamp.Format(time.RFC3339),
		entry.Level,
		entry.Message)

	if entry.CorrelationID != "" {
		logMessage += fmt.Sprintf(" [correlation_id:%s]", entry.CorrelationID)
	}

	if entry.Error != "" {
		logMessage += fmt.Sprintf(" error:%s", entry.Error)
	}

	// Add metadata
	for key, value := range entry.Metadata {
		logMessage += fmt.Sprintf(" %s:%v", key, value)
	}

	log.Println(logMessage)
}

func (sl *StructuredLogger) levelToString(level LogLevel) string {
	switch level {
	case DEBUG:
		return "DEBUG"
	case INFO:
		return "INFO"
	case WARN:
		return "WARN"
	case ERROR:
		return "ERROR"
	case FATAL:
		return "FATAL"
	default:
		return "UNKNOWN"
	}
}

type LogContext struct {
	logger        *StructuredLogger
	correlationID string
	context       context.Context
}

func (lc *LogContext) WithField(key string, value interface{}) *LogContext {
	return lc.WithFields(map[string]interface{}{key: value})
}

func (lc *LogContext) WithFields(fields map[string]interface{}) *LogContext {
	return &LogContext{
		logger:        lc.logger,
		correlationID: lc.correlationID,
		context:       context.WithValue(lc.context, "fields", fields),
	}
}

func (lc *LogContext) WithUserID(userID int64) *LogContext {
	return lc.WithField("user_id", userID)
}

func (lc *LogContext) WithRequestID(requestID string) *LogContext {
	return lc.WithField("request_id", requestID)
}

func (lc *LogContext) WithError(err error) *LogContext {
	return lc.WithField("error", err.Error())
}

func (lc *LogContext) Debug(message string, fields ...map[string]interface{}) {
	lc.logger.Debug(message, lc.mergeContextFields(fields...))
}

func (lc *LogContext) Info(message string, fields ...map[string]interface{}) {
	lc.logger.Info(message, lc.mergeContextFields(fields...))
}

func (lc *LogContext) Warn(message string, fields ...map[string]interface{}) {
	lc.logger.Warn(message, lc.mergeContextFields(fields...))
}

func (lc *LogContext) Error(message string, fields ...map[string]interface{}) {
	lc.logger.Error(message, lc.mergeContextFields(fields...))
}

func (lc *LogContext) Fatal(message string, fields ...map[string]interface{}) {
	lc.logger.Fatal(message, lc.mergeContextFields(fields...))
}

func (lc *LogContext) mergeContextFields(fields ...map[string]interface{}) map[string]interface{} {
	merged := make(map[string]interface{})

	// Add correlation ID
	if lc.correlationID != "" {
		merged["correlation_id"] = lc.correlationID
	}

	// Add context fields
	if fields := lc.context.Value("fields"); fields != nil {
		if contextFields, ok := fields.(map[string]interface{}); ok {
			for k, v := range contextFields {
				merged[k] = v
			}
		}
	}

	// Add provided fields
	for _, fieldMap := range fields {
		for k, v := range fieldMap {
			merged[k] = v
		}
	}

	return merged
}

// Context key for correlation ID
type correlationIDKey struct{}

func WithCorrelationID(ctx context.Context, correlationID string) context.Context {
	return context.WithValue(ctx, correlationIDKey{}, correlationID)
}

func getCorrelationID(ctx context.Context) string {
	if id := ctx.Value(correlationIDKey{}); id != nil {
		if correlationID, ok := id.(string); ok {
			return correlationID
		}
	}
	return ""
}

func GenerateCorrelationID() string {
	return uuid.New().String()
}

func mergeFields(fields ...map[string]interface{}) map[string]interface{} {
	merged := make(map[string]interface{})
	for _, fieldMap := range fields {
		for k, v := range fieldMap {
			merged[k] = v
		}
	}
	return merged
}

// HTTP middleware for correlation ID
type CorrelationIDMiddleware struct {
	logger *StructuredLogger
}

func NewCorrelationIDMiddleware(logger *StructuredLogger) *CorrelationIDMiddleware {
	return &CorrelationIDMiddleware{
		logger: logger,
	}
}

func (cim *CorrelationIDMiddleware) Middleware(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		correlationID := r.Header.Get("X-Correlation-ID")
		if correlationID == "" {
			correlationID = GenerateCorrelationID()
		}

		ctx := WithCorrelationID(r.Context(), correlationID)
		r = r.WithContext(ctx)

		// Add correlation ID to response header
		w.Header().Set("X-Correlation-ID", correlationID)

		next.ServeHTTP(w, r)
	})
}
