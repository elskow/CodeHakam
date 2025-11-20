package api

import (
	"fmt"
	"net/http"
	"strconv"

	"execution_service/internal/database"
	"execution_service/internal/middleware"
	"execution_service/internal/models"
	"execution_service/internal/queue"
	"execution_service/internal/validation"
	"execution_service/internal/worker"

	"github.com/gin-gonic/gin"
)

type Handler struct {
	db       *database.DB
	queue    *queue.RabbitMQClient
	pool     *worker.JudgePool
	security *middleware.SecurityMiddleware
}

func NewHandler(db *database.DB, q *queue.RabbitMQClient, p *worker.JudgePool) *Handler {
	securityMiddleware := middleware.NewSecurityMiddleware("default-secret-change-in-production")
	return &Handler{
		db:       db,
		queue:    q,
		pool:     p,
		security: securityMiddleware,
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

	isolateSandbox := h.pool.GetSandbox()
	if isolateSandbox == nil {
		c.JSON(http.StatusServiceUnavailable, gin.H{"error": "Sandbox not available"})
		return
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
