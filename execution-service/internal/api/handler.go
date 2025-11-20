package api

import (
	"fmt"
	"net/http"
	"strconv"
	"time"

	"execution_service/internal/database"
	"execution_service/internal/middleware"
	"execution_service/internal/models"
	"execution_service/internal/queue"
	"execution_service/internal/services"
	"execution_service/internal/storage"
	"execution_service/internal/validation"
	"execution_service/internal/worker"

	"github.com/gin-gonic/gin"
)

type Handler struct {
	db       *database.DB
	queue    *queue.RabbitMQClient
	pool     *worker.JudgePool
	storage  *storage.MinIOClient
	security *middleware.SecurityMiddleware
	audit    *services.AuditLogService
	metrics  *services.MetricsService
}

func NewHandler(db *database.DB, q *queue.RabbitMQClient, p *worker.JudgePool, s *storage.MinIOClient, jwtSecret string) *Handler {
	securityMiddleware := middleware.NewSecurityMiddleware(jwtSecret)
	auditService := services.NewAuditLogService(db)
	metricsService := services.NewMetricsService()
	return &Handler{
		db:       db,
		queue:    q,
		pool:     p,
		storage:  s,
		security: securityMiddleware,
		audit:    auditService,
		metrics:  metricsService,
	}
}

func (h *Handler) RequireAuth() gin.HandlerFunc {
	return h.security.RequireAuth()
}

func (h *Handler) RequireAdmin() gin.HandlerFunc {
	return h.security.RequireAdmin()
}

func (h *Handler) RegisterRoutes(r *gin.Engine) {
	api := r.Group("/api")
	{
		submissions := api.Group("/submissions")
		{
			submissions.POST("", h.CreateSubmission)
			submissions.GET("/:id", h.GetSubmission)
			submissions.GET("/user/:userId", h.GetUserSubmissions)
			submissions.GET("/problem/:problemId", h.GetProblemSubmissions)
			submissions.POST("/:id/rejudge", h.RejudgeSubmission)
		}

		judge := api.Group("/judge")
		{
			judge.GET("/status", h.GetJudgeStatus)
			judge.GET("/workers", h.GetWorkers)
			judge.POST("/workers/scale", h.ScaleWorkers)
			judge.GET("/queue", h.GetQueueStatus)
		}

		languages := api.Group("/languages")
		{
			languages.GET("/", h.GetLanguages)
			languages.GET("/:code", h.GetLanguage)
		}

		admin := api.Group("/admin")
		admin.Use(h.RequireAuth())
		admin.Use(h.RequireAdmin())
		{
			admin.POST("/clear-box/:id", h.ClearBox)
		}
	}

	r.GET("/health", h.HealthCheck)
	r.GET("/metrics", h.Metrics)
	r.GET("/circuit-breakers", h.CircuitBreakerStatus)
	r.GET("/prometheus", h.PrometheusMetrics)
	r.GET("/cleanup-stats", h.CleanupStats)
}

func (h *Handler) CreateSubmission(c *gin.Context) {
	var request struct {
		UserID        int64  `json:"user_id" binding:"required,min=1"`
		ProblemID     int64  `json:"problem_id" binding:"required,min=1"`
		ContestID     *int64 `json:"contest_id,omitempty"`
		Language      string `json:"language" binding:"required"`
		Code          string `json:"code" binding:"required"`
		TimeLimitMs   int    `json:"time_limit_ms,omitempty"`
		MemoryLimitKb int    `json:"memory_limit_kb,omitempty"`
	}

	if err := c.ShouldBindJSON(&request); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	// Validate language
	if err := validation.ValidateLanguage(request.Language); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	// Validate code
	codeBytes := []byte(request.Code)
	if err := validation.ValidateCode(codeBytes, request.Language); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	// Set default limits if not provided
	timeLimit := request.TimeLimitMs
	if timeLimit <= 0 {
		timeLimit = 2000 // Default 2 seconds
	}
	memoryLimit := request.MemoryLimitKb
	if memoryLimit <= 0 {
		memoryLimit = 262144 // Default 256MB
	}

	// Validate limits
	if timeLimit > 30000 {
		c.JSON(http.StatusBadRequest, gin.H{"error": "time limit must be <= 30000ms"})
		return
	}
	if memoryLimit > 524288 {
		c.JSON(http.StatusBadRequest, gin.H{"error": "memory limit must be <= 524288KB"})
		return
	}

	// Create submission record
	submission := &models.Submission{
		UserID:          request.UserID,
		ProblemID:       request.ProblemID,
		ContestID:       request.ContestID,
		Language:        request.Language,
		Verdict:         models.VerdictPending,
		Score:           0,
		TestCasesPassed: 0,
		IsPublic:        false,
	}

	// Upload code to storage
	codeURL, err := h.storage.UploadCode(c.Request.Context(), submission.ID, request.Language, codeBytes)
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to upload code"})
		return
	}
	submission.CodeURL = codeURL

	// Save submission to database
	err = h.db.CreateSubmission(c.Request.Context(), submission)
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to create submission"})
		return
	}

	// Determine priority based on contest
	priority := 0 // Default practice priority
	if request.ContestID != nil {
		priority = 5 // Contest priority
	}

	// Create judge request
	judgeRequest := &models.JudgeRequest{
		SubmissionID:  submission.ID,
		UserID:        request.UserID,
		ProblemID:     request.ProblemID,
		Language:      request.Language,
		CodeURL:       codeURL,
		TimeLimitMs:   timeLimit,
		MemoryLimitKb: memoryLimit,
		Priority:      priority,
	}

	// Validate judge request
	if err := validation.ValidateJudgeRequest(judgeRequest); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	// Publish to RabbitMQ
	err = h.queue.PublishSubmission(c.Request.Context(), judgeRequest)
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to queue submission"})
		return
	}

	// Log submission creation
	h.db.CreateExecutionLog(c.Request.Context(), &models.ExecutionLog{
		SubmissionID: submission.ID,
		Level:        "INFO",
		Message:      fmt.Sprintf("Submission created for user %d, problem %d, language %s", request.UserID, request.ProblemID, request.Language),
	})

	c.JSON(http.StatusCreated, gin.H{
		"submission_id": submission.ID,
		"status":        "queued",
		"message":       "Submission queued for judging",
	})
}

func (h *Handler) GetSubmission(c *gin.Context) {
	idStr := c.Param("id")
	id, err := validation.ValidateSubmissionID(idStr)
	if err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	submission, err := h.db.GetSubmission(c.Request.Context(), id)
	if err != nil {
		c.JSON(http.StatusNotFound, gin.H{"error": "Submission not found"})
		return
	}

	c.JSON(http.StatusOK, submission)
}

func (h *Handler) GetUserSubmissions(c *gin.Context) {
	userIDStr := c.Param("userId")
	userID, err := validation.ValidateUserID(userIDStr)
	if err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	limitStr := c.Query("limit")
	offsetStr := c.Query("offset")
	limit, offset, err := validation.ValidatePagination(limitStr, offsetStr)
	if err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	submissions, err := h.db.GetUserSubmissions(c.Request.Context(), userID, limit, offset)
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to get submissions"})
		return
	}

	c.JSON(http.StatusOK, gin.H{
		"submissions": submissions,
		"limit":       limit,
		"offset":      offset,
	})
}

func (h *Handler) GetProblemSubmissions(c *gin.Context) {
	problemIDStr := c.Param("problemId")
	problemID, err := validation.ValidateProblemID(problemIDStr)
	if err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	limitStr := c.Query("limit")
	offsetStr := c.Query("offset")
	limit, offset, err := validation.ValidatePagination(limitStr, offsetStr)
	if err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	submissions, err := h.db.GetProblemSubmissions(c.Request.Context(), problemID, limit, offset)
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to get submissions"})
		return
	}

	c.JSON(http.StatusOK, gin.H{
		"submissions": submissions,
		"limit":       limit,
		"offset":      offset,
	})
}

func (h *Handler) RejudgeSubmission(c *gin.Context) {
	idStr := c.Param("id")
	id, err := validation.ValidateSubmissionID(idStr)
	if err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	submission, err := h.db.GetSubmission(c.Request.Context(), id)
	if err != nil {
		c.JSON(http.StatusNotFound, gin.H{"error": "Submission not found"})
		return
	}

	// Get user info for audit logging
	userIDValue, _ := c.Get("user_id")
	var userID int64
	if v, ok := userIDValue.(float64); ok {
		userID = int64(v)
	}

	request := &models.JudgeRequest{
		SubmissionID:  id,
		UserID:        submission.UserID,
		ProblemID:     submission.ProblemID,
		Language:      submission.Language,
		CodeURL:       submission.CodeURL,
		TimeLimitMs:   2000,
		MemoryLimitKb: 262144,
		Priority:      5,
	}

	// Log admin action before execution
	auditEvent := &services.AuditEvent{
		UserID:     userID,
		Action:     services.AdminActionSubmissionRejudge,
		Resource:   "submission",
		ResourceID: &id,
		IPAddress:  c.ClientIP(),
		UserAgent:  c.GetHeader("User-Agent"),
		Details: map[string]interface{}{
			"submission_id": id,
			"problem_id":    submission.ProblemID,
			"user_id":       submission.UserID,
			"language":      submission.Language,
		},
		Timestamp: time.Now(),
		Severity:  services.SeverityInfo,
	}

	if err := h.audit.LogAdminAction(c.Request.Context(), auditEvent); err != nil {
		// Log error but don't fail the request
		fmt.Printf("Failed to log admin action: %v\n", err)
	}

	err = h.queue.PublishSubmission(c.Request.Context(), request)
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to queue rejudge"})
		return
	}

	c.JSON(http.StatusOK, gin.H{"message": "Rejudge queued"})
}

func (h *Handler) GetJudgeStatus(c *gin.Context) {
	status := h.pool.GetStatus()
	c.JSON(http.StatusOK, status)
}

func (h *Handler) GetWorkers(c *gin.Context) {
	status := h.pool.GetStatus()
	c.JSON(http.StatusOK, gin.H{
		"workers": status,
	})
}

func (h *Handler) ScaleWorkers(c *gin.Context) {
	var request struct {
		WorkerCount int `json:"worker_count" binding:"required,min=1,max=50"`
	}

	if err := c.ShouldBindJSON(&request); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	// Get user info for audit logging
	userIDValue, _ := c.Get("user_id")
	var userID int64
	if v, ok := userIDValue.(float64); ok {
		userID = int64(v)
	}

	// Get current status
	currentStatus := h.pool.GetStatus()
	currentWorkers := currentStatus["total_workers"].(int)

	if request.WorkerCount == currentWorkers {
		c.JSON(http.StatusOK, gin.H{
			"message":           "No scaling needed",
			"current_workers":   currentWorkers,
			"requested_workers": request.WorkerCount,
		})
		return
	}

	// Log admin action before execution
	auditEvent := &services.AuditEvent{
		UserID:    userID,
		Action:    services.AdminActionWorkerScale,
		Resource:  "judge_workers",
		IPAddress: c.ClientIP(),
		UserAgent: c.GetHeader("User-Agent"),
		Details: map[string]interface{}{
			"previous_count": currentWorkers,
			"new_count":      request.WorkerCount,
		},
		Timestamp: time.Now(),
		Severity:  services.SeverityInfo,
	}

	if err := h.audit.LogAdminAction(c.Request.Context(), auditEvent); err != nil {
		// Log error but don't fail the request
		fmt.Printf("Failed to log admin action: %v\n", err)
	}

	// Perform scaling operation
	err := h.pool.ScaleWorkers(request.WorkerCount)
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{
			"error":             fmt.Sprintf("Failed to scale workers: %v", err),
			"current_workers":   currentWorkers,
			"requested_workers": request.WorkerCount,
		})
		return
	}

	c.JSON(http.StatusOK, gin.H{
		"message":          "Worker scaling completed",
		"previous_workers": currentWorkers,
		"current_workers":  request.WorkerCount,
	})
}

func (h *Handler) GetQueueStatus(c *gin.Context) {
	queueSize, err := h.queue.GetQueueInfo()
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to get queue info"})
		return
	}

	c.JSON(http.StatusOK, gin.H{
		"queue_size": queueSize,
		"is_healthy": h.queue.IsHealthy(),
	})
}

func (h *Handler) GetLanguages(c *gin.Context) {
	languages, err := h.db.GetSupportedLanguages(c.Request.Context())
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to get languages"})
		return
	}

	c.JSON(http.StatusOK, gin.H{"languages": languages})
}

func (h *Handler) GetLanguage(c *gin.Context) {
	code := c.Param("code")
	if err := validation.ValidateLanguage(code); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	language, err := h.db.GetLanguage(c.Request.Context(), code)
	if err != nil {
		c.JSON(http.StatusNotFound, gin.H{"error": "Language not found"})
		return
	}

	c.JSON(http.StatusOK, language)
}

func (h *Handler) ClearBox(c *gin.Context) {
	idStr := c.Param("id")
	boxID, err := strconv.Atoi(idStr)
	if err != nil || boxID < 0 || boxID > 1000 {
		c.JSON(http.StatusBadRequest, gin.H{"error": "Invalid box ID (must be 0-1000)"})
		return
	}

	// Get user info for audit logging
	userIDValue, _ := c.Get("user_id")
	var userID int64
	if v, ok := userIDValue.(float64); ok {
		userID = int64(v)
	}

	isolateSandbox := h.pool.GetSandbox()
	if isolateSandbox == nil {
		c.JSON(http.StatusServiceUnavailable, gin.H{"error": "Sandbox not available"})
		return
	}

	// Log admin action before execution
	auditEvent := &services.AuditEvent{
		UserID:     userID,
		Action:     services.AdminActionBoxCleanup,
		Resource:   "sandbox_box",
		ResourceID: &[]int64{int64(boxID)}[0],
		IPAddress:  c.ClientIP(),
		UserAgent:  c.GetHeader("User-Agent"),
		Details: map[string]interface{}{
			"box_id": boxID,
		},
		Timestamp: time.Now(),
		Severity:  services.SeverityInfo,
	}

	if err := h.audit.LogAdminAction(c.Request.Context(), auditEvent); err != nil {
		// Log error but don't fail the request
		fmt.Printf("Failed to log admin action: %v\n", err)
	}

	isolateSandbox.CleanupBox(boxID)

	c.JSON(http.StatusOK, gin.H{
		"message": fmt.Sprintf("Box %d cleaned up successfully", boxID),
		"box_id":  boxID,
	})
}

func (h *Handler) HealthCheck(c *gin.Context) {
	health := gin.H{
		"status": "healthy",
	}

	if err := h.db.Ping(c.Request.Context()); err != nil {
		health["status"] = "unhealthy"
		health["database"] = "disconnected"
	} else {
		health["database"] = "connected"
	}

	if !h.queue.IsHealthy() {
		health["status"] = "unhealthy"
		health["rabbitmq"] = "disconnected"
	} else {
		health["rabbitmq"] = "connected"
	}

	status := h.pool.GetStatus()
	health["workers"] = status["total_workers"]
	health["active_workers"] = status["active_workers"]
	health["queue_size"] = status["queue_size"]

	if health["status"] == "healthy" {
		c.JSON(http.StatusOK, health)
	} else {
		c.JSON(http.StatusServiceUnavailable, health)
	}
}

func (h *Handler) Metrics(c *gin.Context) {
	queueSize, _ := h.queue.GetQueueInfo()
	status := h.pool.GetStatus()

	metrics := gin.H{
		"queue_size":     queueSize,
		"total_workers":  status["total_workers"],
		"active_workers": status["active_workers"],
		"is_healthy":     status["is_healthy"],
		"uptime_seconds": 0,
	}

	c.JSON(http.StatusOK, metrics)
}

func (h *Handler) CircuitBreakerStatus(c *gin.Context) {
	// This would require access to the circuit breaker service
	// For now, return a placeholder response
	c.JSON(http.StatusOK, gin.H{
		"message": "Circuit breaker status endpoint",
		"services": map[string]string{
			"content-service": "closed",
			"minio":           "closed",
			"rabbitmq":        "closed",
			"database":        "closed",
		},
	})
}

func (h *Handler) PrometheusMetrics(c *gin.Context) {
	h.metrics.Handler().ServeHTTP(c.Writer, c.Request)
}

func (h *Handler) CleanupStats(c *gin.Context) {
	config := &services.CleanupConfig{
		SubmissionsRetention:       90 * 24 * time.Hour,  // 90 days
		ExecutionLogsRetention:     30 * 24 * time.Hour,  // 30 days
		TestResultsRetention:       60 * 24 * time.Hour,  // 60 days
		PlagiarismReportsRetention: 180 * 24 * time.Hour, // 180 days
		CleanupInterval:            24 * time.Hour,       // Daily
	}
	cleanupService := services.NewCleanupService(h.db, config)
	stats, err := cleanupService.GetCleanupStats(c.Request.Context())
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to get cleanup stats"})
		return
	}

	c.JSON(http.StatusOK, stats)
}
