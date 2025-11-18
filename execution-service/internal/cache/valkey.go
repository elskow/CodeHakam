package cache

import (
	"context"
	"encoding/json"
	"fmt"
	"time"

	"execution_service/internal/config"
	"execution_service/internal/models"

	"github.com/go-redis/redis/v8"
)

type ValkeyClient struct {
	client *redis.Client
}

func NewValkeyClient(cfg *config.ValkeyConfig) (*ValkeyClient, error) {
	rdb := redis.NewClient(&redis.Options{
		Addr:     cfg.URL,
		Password: cfg.Password,
		DB:       0,
	})

	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	_, err := rdb.Ping(ctx).Result()
	if err != nil {
		return nil, fmt.Errorf("failed to connect to Valkey: %w", err)
	}

	return &ValkeyClient{
		client: rdb,
	}, nil
}

func (v *ValkeyClient) Close() error {
	return v.client.Close()
}

func (v *ValkeyClient) CacheSubmissionResult(ctx context.Context, submissionID int64, result *models.JudgeResult) error {
	key := fmt.Sprintf("submission:result:%d", submissionID)

	data, err := json.Marshal(result)
	if err != nil {
		return fmt.Errorf("failed to marshal result: %w", err)
	}

	return v.client.Set(ctx, key, data, time.Hour).Err()
}

func (v *ValkeyClient) GetCachedSubmissionResult(ctx context.Context, submissionID int64) (*models.JudgeResult, error) {
	key := fmt.Sprintf("submission:result:%d", submissionID)

	data, err := v.client.Get(ctx, key).Result()
	if err != nil {
		if err == redis.Nil {
			return nil, fmt.Errorf("not found")
		}
		return nil, fmt.Errorf("failed to get cached result: %w", err)
	}

	var result models.JudgeResult
	err = json.Unmarshal([]byte(data), &result)
	if err != nil {
		return nil, fmt.Errorf("failed to unmarshal result: %w", err)
	}

	return &result, nil
}

func (v *ValkeyClient) CacheTestCases(ctx context.Context, problemID int64, testCases []models.TestCase) error {
	key := fmt.Sprintf("problem:test_cases:%d", problemID)

	data, err := json.Marshal(testCases)
	if err != nil {
		return fmt.Errorf("failed to marshal test cases: %w", err)
	}

	return v.client.Set(ctx, key, data, 30*time.Minute).Err()
}

func (v *ValkeyClient) GetCachedTestCases(ctx context.Context, problemID int64) ([]models.TestCase, error) {
	key := fmt.Sprintf("problem:test_cases:%d", problemID)

	data, err := v.client.Get(ctx, key).Result()
	if err != nil {
		if err == redis.Nil {
			return nil, fmt.Errorf("not found")
		}
		return nil, fmt.Errorf("failed to get cached test cases: %w", err)
	}

	var testCases []models.TestCase
	err = json.Unmarshal([]byte(data), &testCases)
	if err != nil {
		return nil, fmt.Errorf("failed to unmarshal test cases: %w", err)
	}

	return testCases, nil
}

func (v *ValkeyClient) CacheLanguage(ctx context.Context, code string, language *models.SupportedLanguage) error {
	key := fmt.Sprintf("language:config:%s", code)

	data, err := json.Marshal(language)
	if err != nil {
		return fmt.Errorf("failed to marshal language: %w", err)
	}

	return v.client.Set(ctx, key, data, 24*time.Hour).Err()
}

func (v *ValkeyClient) GetCachedLanguage(ctx context.Context, code string) (*models.SupportedLanguage, error) {
	key := fmt.Sprintf("language:config:%s", code)

	data, err := v.client.Get(ctx, key).Result()
	if err != nil {
		if err == redis.Nil {
			return nil, fmt.Errorf("not found")
		}
		return nil, fmt.Errorf("failed to get cached language: %w", err)
	}

	var language models.SupportedLanguage
	err = json.Unmarshal([]byte(data), &language)
	if err != nil {
		return nil, fmt.Errorf("failed to unmarshal language: %w", err)
	}

	return &language, nil
}

func (v *ValkeyClient) SetQueueSize(ctx context.Context, size int) error {
	return v.client.Set(ctx, "judge:queue:size", size, 10*time.Second).Err()
}

func (v *ValkeyClient) GetQueueSize(ctx context.Context) (int, error) {
	size, err := v.client.Get(ctx, "judge:queue:size").Int()
	if err != nil {
		if err == redis.Nil {
			return 0, nil
		}
		return 0, fmt.Errorf("failed to get queue size: %w", err)
	}
	return size, nil
}

func (v *ValkeyClient) InvalidateSubmission(ctx context.Context, submissionID int64) error {
	key := fmt.Sprintf("submission:result:%d", submissionID)
	return v.client.Del(ctx, key).Err()
}

func (v *ValkeyClient) InvalidateProblem(ctx context.Context, problemID int64) error {
	key := fmt.Sprintf("problem:test_cases:%d", problemID)
	return v.client.Del(ctx, key).Err()
}

func (v *ValkeyClient) InvalidateLanguage(ctx context.Context, code string) error {
	key := fmt.Sprintf("language:config:%s", code)
	return v.client.Del(ctx, key).Err()
}

func (v *ValkeyClient) IsHealthy() bool {
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	_, err := v.client.Ping(ctx).Result()
	return err == nil
}

func (v *ValkeyClient) GetStats(ctx context.Context) (map[string]string, error) {
	info, err := v.client.Info(ctx).Result()
	if err != nil {
		return nil, fmt.Errorf("failed to get info: %w", err)
	}

	stats := make(map[string]string)
	stats["info"] = info

	return stats, nil
}
