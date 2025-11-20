package main

import (
	"context"
	"fmt"
	"log"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"execution_service/internal/api"
	"execution_service/internal/cache"
	"execution_service/internal/config"
	"execution_service/internal/database"
	"execution_service/internal/httpclient"
	"execution_service/internal/middleware"
	"execution_service/internal/plagiarism"
	"execution_service/internal/queue"
	"execution_service/internal/sandbox"
	"execution_service/internal/services"
	"execution_service/internal/storage"
	"execution_service/internal/worker"

	"github.com/gin-gonic/gin"
)

func main() {
	cfg, err := config.Load()
	if err != nil {
		log.Fatalf("Failed to load config: %v", err)
	}

	db, err := database.NewDB(
		cfg.Database.URL,
		cfg.Database.MaxOpenConns,
		cfg.Database.MaxIdleConns,
		cfg.Database.ConnMaxLifetime,
	)
	if err != nil {
		log.Fatalf("Failed to connect to database: %v", err)
	}
	defer db.Close()

	minioClient, err := storage.NewMinIOClient(&cfg.MinIO)
	if err != nil {
		log.Fatalf("Failed to create MinIO client: %v", err)
	}

	rabbitmqClient, err := queue.NewRabbitMQClient(&cfg.RabbitMQ)
	if err != nil {
		log.Fatalf("Failed to create RabbitMQ client: %v", err)
	}
	defer rabbitmqClient.Close()

	valkeyClient, err := cache.NewValkeyClient(&cfg.Valkey)
	if err != nil {
		log.Fatalf("Failed to create Valkey client: %v", err)
	}
	defer valkeyClient.Close()

	isolateSandbox := sandbox.NewIsolateSandbox(&cfg.Isolate)

	// Initialize resource validation service
	contentClient := httpclient.NewContentServiceClient("http://localhost:3002")
	resourceValidator := services.NewResourceValidationService(&cfg.Judge, contentClient)

	judgePool := worker.NewJudgePool(
		cfg.Judge.WorkerCount,
		db,
		rabbitmqClient,
		minioClient,
		isolateSandbox,
		resourceValidator,
	)

	// Initialize plagiarism detector
	plagiarismDetector := plagiarism.NewPlagiarismDetector(db, minioClient, &cfg.Plagiarism)

	// Set plagiarism enqueuer for judge pool
	judgePool.SetPlagiarismEnqueuer(plagiarismDetector.EnqueueSubmission)

	handler := api.NewHandler(db, rabbitmqClient, judgePool)

	// Initialize security middleware
	securityMiddleware := middleware.NewSecurityMiddleware(cfg.JWT.Secret)

	gin.SetMode(gin.ReleaseMode)
	router := gin.New()
	router.Use(gin.Logger())
	router.Use(gin.Recovery())

	// Apply security middleware
	router.Use(securityMiddleware.SecurityHeaders())
	router.Use(securityMiddleware.JWTRateLimit(60))             // 60 requests per minute
	router.Use(securityMiddleware.ValidateRequestSize(1 << 20)) // 1MB max request size
	router.Use(securityMiddleware.ValidateContentType("application/json", "text/plain"))

	handler.RegisterRoutes(router)

	server := &http.Server{
		Addr:         ":" + cfg.Server.Port,
		Handler:      router,
		ReadTimeout:  cfg.Server.ReadTimeout,
		WriteTimeout: cfg.Server.WriteTimeout,
	}

	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()

	errChan := make(chan error, 1)

	go func() {
		log.Printf("Starting execution service on port %s", cfg.Server.Port)
		if err := server.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			errChan <- fmt.Errorf("failed to start server: %w", err)
		}
	}()

	go func() {
		log.Printf("Starting judge worker pool with %d workers", cfg.Judge.WorkerCount)
		if err := judgePool.Start(ctx); err != nil {
			errChan <- fmt.Errorf("failed to start judge pool: %w", err)
		}
	}()

	// Start plagiarism detector
	go func() {
		log.Printf("Starting plagiarism detection")
		if err := plagiarismDetector.Start(ctx); err != nil {
			errChan <- fmt.Errorf("failed to start plagiarism detector: %w", err)
		}
	}()

	rabbitmqClient.StartHeartbeat()

	quit := make(chan os.Signal, 1)
	signal.Notify(quit, syscall.SIGINT, syscall.SIGTERM)

	select {
	case err := <-errChan:
		log.Printf("Service error: %v", err)
		cancel()
	case <-quit:
		log.Println("Shutting down execution service...")
		cancel()
	}

	shutdownCtx, shutdownCancel := context.WithTimeout(context.Background(), 30*time.Second)
	defer shutdownCancel()

	if err := server.Shutdown(shutdownCtx); err != nil {
		log.Printf("Server forced to shutdown: %v", err)
	}

	judgePool.Stop()
	plagiarismDetector.Stop()

	log.Println("Execution service stopped")
}
