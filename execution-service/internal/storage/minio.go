package storage

import (
	"bytes"
	"context"
	"fmt"
	"io"
	"net/url"
	"strings"
	"time"

	"execution_service/internal/config"

	"github.com/minio/minio-go/v7"
	"github.com/minio/minio-go/v7/pkg/credentials"
)

type MinIOClient struct {
	Client *minio.Client
	Bucket string
}

func NewMinIOClient(cfg *config.MinIOConfig) (*MinIOClient, error) {
	client, err := minio.New(cfg.Endpoint, &minio.Options{
		Creds:  credentials.NewStaticV4(cfg.AccessKey, cfg.SecretKey, ""),
		Secure: cfg.UseSSL,
	})
	if err != nil {
		return nil, fmt.Errorf("failed to create MinIO client: %w", err)
	}

	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	exists, err := client.BucketExists(ctx, cfg.BucketName)
	if err != nil {
		return nil, fmt.Errorf("failed to check bucket existence: %w", err)
	}

	if !exists {
		err = client.MakeBucket(ctx, cfg.BucketName, minio.MakeBucketOptions{})
		if err != nil {
			return nil, fmt.Errorf("failed to create bucket: %w", err)
		}
	}

	return &MinIOClient{
		Client: client,
		Bucket: cfg.BucketName,
	}, nil
}

func (m *MinIOClient) UploadCode(ctx context.Context, submissionID int64, language string, code []byte) (string, error) {
	filename := fmt.Sprintf("%d.%s", submissionID, getFileExtension(language))
	objectName := fmt.Sprintf("submissions/%s", filename)

	_, err := m.Client.PutObject(ctx, m.Bucket, objectName, bytes.NewReader(code), int64(len(code)), minio.PutObjectOptions{
		ContentType: "text/plain",
	})
	if err != nil {
		return "", fmt.Errorf("failed to upload code: %w", err)
	}

	return m.getObjectURL(objectName), nil
}

func (m *MinIOClient) DownloadCode(ctx context.Context, codeURL string) ([]byte, error) {
	objectName, err := m.parseURL(codeURL)
	if err != nil {
		return nil, fmt.Errorf("invalid code URL: %w", err)
	}

	obj, err := m.Client.GetObject(ctx, m.Bucket, objectName, minio.GetObjectOptions{})
	if err != nil {
		return nil, fmt.Errorf("failed to get object: %w", err)
	}
	defer obj.Close()

	code, err := io.ReadAll(obj)
	if err != nil {
		return nil, fmt.Errorf("failed to read object: %w", err)
	}

	return code, nil
}

func (m *MinIOClient) UploadTestCase(ctx context.Context, problemID int64, testNumber int, input, output []byte) (inputURL, outputURL string, err error) {
	inputName := fmt.Sprintf("problems/%d/testcases/%d/input.txt", problemID, testNumber)
	outputName := fmt.Sprintf("problems/%d/testcases/%d/output.txt", problemID, testNumber)

	_, err = m.Client.PutObject(ctx, m.Bucket, inputName, bytes.NewReader(input), int64(len(input)), minio.PutObjectOptions{
		ContentType: "text/plain",
	})
	if err != nil {
		return "", "", fmt.Errorf("failed to upload input: %w", err)
	}

	_, err = m.Client.PutObject(ctx, m.Bucket, outputName, bytes.NewReader(output), int64(len(output)), minio.PutObjectOptions{
		ContentType: "text/plain",
	})
	if err != nil {
		return "", "", fmt.Errorf("failed to upload output: %w", err)
	}

	return m.getObjectURL(inputName), m.getObjectURL(outputName), nil
}

func (m *MinIOClient) DownloadTestCase(ctx context.Context, inputURL, outputURL string) (input, output []byte, err error) {
	inputName, err := m.parseURL(inputURL)
	if err != nil {
		return nil, nil, fmt.Errorf("invalid input URL: %w", err)
	}

	outputName, err := m.parseURL(outputURL)
	if err != nil {
		return nil, nil, fmt.Errorf("invalid output URL: %w", err)
	}

	inputObj, err := m.Client.GetObject(ctx, m.Bucket, inputName, minio.GetObjectOptions{})
	if err != nil {
		return nil, nil, fmt.Errorf("failed to get input object: %w", err)
	}
	defer inputObj.Close()

	input, err = io.ReadAll(inputObj)
	if err != nil {
		return nil, nil, fmt.Errorf("failed to read input object: %w", err)
	}

	outputObj, err := m.Client.GetObject(ctx, m.Bucket, outputName, minio.GetObjectOptions{})
	if err != nil {
		return nil, nil, fmt.Errorf("failed to get output object: %w", err)
	}
	defer outputObj.Close()

	output, err = io.ReadAll(outputObj)
	if err != nil {
		return nil, nil, fmt.Errorf("failed to read output object: %w", err)
	}

	return input, output, nil
}

func (m *MinIOClient) DeleteFile(ctx context.Context, fileURL string) error {
	objectName, err := m.parseURL(fileURL)
	if err != nil {
		return fmt.Errorf("invalid file URL: %w", err)
	}

	err = m.Client.RemoveObject(ctx, m.Bucket, objectName, minio.RemoveObjectOptions{})
	if err != nil {
		return fmt.Errorf("failed to delete file: %w", err)
	}

	return nil
}

func (m *MinIOClient) GetFileURL(ctx context.Context, fileURL string) (string, error) {
	objectName, err := m.parseURL(fileURL)
	if err != nil {
		return "", fmt.Errorf("invalid file URL: %w", err)
	}

	reqParams := make(url.Values)
	presignedURL, err := m.Client.PresignedGetObject(ctx, m.Bucket, objectName, 24*time.Hour, reqParams)
	if err != nil {
		return "", fmt.Errorf("failed to generate presigned URL: %w", err)
	}

	return presignedURL.String(), nil
}

func (m *MinIOClient) parseURL(fileURL string) (string, error) {
	if !strings.HasPrefix(fileURL, "s3://") {
		return "", fmt.Errorf("invalid S3 URL format")
	}

	parsed, err := url.Parse(fileURL)
	if err != nil {
		return "", fmt.Errorf("failed to parse URL: %w", err)
	}

	if parsed.Host != m.Bucket {
		return "", fmt.Errorf("bucket mismatch: expected %s, got %s", m.Bucket, parsed.Host)
	}

	return strings.TrimPrefix(parsed.Path, "/"), nil
}

func (m *MinIOClient) getObjectURL(objectName string) string {
	return fmt.Sprintf("s3://%s/%s", m.Bucket, objectName)
}

func getFileExtension(language string) string {
	extensions := map[string]string{
		"cpp":    "cpp",
		"c":      "c",
		"java":   "java",
		"python": "py",
		"go":     "go",
	}

	if ext, exists := extensions[language]; exists {
		return ext
	}

	return "txt"
}

func (m *MinIOClient) ListTestCases(ctx context.Context, problemID int64) ([]string, error) {
	prefix := fmt.Sprintf("problems/%d/testcases/", problemID)

	objects := m.Client.ListObjects(ctx, m.Bucket, minio.ListObjectsOptions{
		Prefix: prefix,
	})

	var testCases []string
	for obj := range objects {
		if obj.Err != nil {
			return nil, fmt.Errorf("failed to list objects: %w", obj.Err)
		}
		testCases = append(testCases, m.getObjectURL(obj.Key))
	}

	return testCases, nil
}
