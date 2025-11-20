package httpclient

import (
	"context"
	"encoding/json"
	"fmt"
	"net/http"
	"time"

	"execution_service/internal/services"
)

type ContentServiceClient struct {
	baseURL    string
	httpClient *http.Client
}

type TestCaseResponse struct {
	ID          int64  `json:"id"`
	InputURL    string `json:"input_url"`
	OutputURL   string `json:"output_url"`
	IsSample    bool   `json:"is_sample"`
	TimeLimit   int    `json:"time_limit"`
	MemoryLimit int    `json:"memory_limit"`
}

type ProblemResponse struct {
	ID          int64              `json:"id"`
	Title       string             `json:"title"`
	TimeLimit   int                `json:"time_limit_ms"`
	MemoryLimit int                `json:"memory_limit_kb"`
	TestCases   []TestCaseResponse `json:"test_cases"`
}

func NewContentServiceClient(baseURL string) *ContentServiceClient {
	return &ContentServiceClient{
		baseURL: baseURL,
		httpClient: &http.Client{
			Timeout: 30 * time.Second,
		},
	}
}

func (c *ContentServiceClient) GetProblem(ctx context.Context, problemID int64) (*ProblemResponse, error) {
	url := fmt.Sprintf("%s/api/problems/%d", c.baseURL, problemID)

	req, err := http.NewRequestWithContext(ctx, "GET", url, nil)
	if err != nil {
		return nil, fmt.Errorf("failed to create request: %w", err)
	}

	req.Header.Set("Content-Type", "application/json")

	resp, err := c.httpClient.Do(req)
	if err != nil {
		return nil, fmt.Errorf("failed to make request: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return nil, fmt.Errorf("content service returned status %d", resp.StatusCode)
	}

	var problem ProblemResponse
	if err := json.NewDecoder(resp.Body).Decode(&problem); err != nil {
		return nil, fmt.Errorf("failed to decode response: %w", err)
	}

	return &problem, nil
}

func (c *ContentServiceClient) GetTestCases(ctx context.Context, problemID int64) ([]TestCaseResponse, error) {
	problem, err := c.GetProblem(ctx, problemID)
	if err != nil {
		return nil, fmt.Errorf("failed to get problem: %w", err)
	}

	return problem.TestCases, nil
}

func (c *ContentServiceClient) HealthCheck(ctx context.Context) error {
	url := fmt.Sprintf("%s/health", c.baseURL)

	req, err := http.NewRequestWithContext(ctx, "GET", url, nil)
	if err != nil {
		return fmt.Errorf("failed to create request: %w", err)
	}

	resp, err := c.httpClient.Do(req)
	if err != nil {
		return fmt.Errorf("failed to make request: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return fmt.Errorf("content service health check failed with status %d", resp.StatusCode)
	}

	return nil
}
