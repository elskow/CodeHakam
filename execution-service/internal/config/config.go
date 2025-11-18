package config

import (
	"fmt"
	"os"
	"strconv"
	"time"

	"gopkg.in/yaml.v3"
)

type Config struct {
	Server   ServerConfig   `yaml:"server"`
	Database DatabaseConfig `yaml:"database"`
	RabbitMQ RabbitMQConfig `yaml:"rabbitmq"`
	MinIO    MinIOConfig    `yaml:"minio"`
	Valkey   ValkeyConfig   `yaml:"valkey"`
	Judge    JudgeConfig    `yaml:"judge"`
	Isolate  IsolateConfig  `yaml:"isolate"`
}

type ServerConfig struct {
	Port         string        `yaml:"port"`
	ReadTimeout  time.Duration `yaml:"read_timeout"`
	WriteTimeout time.Duration `yaml:"write_timeout"`
}

type DatabaseConfig struct {
	URL             string        `yaml:"url"`
	MaxOpenConns    int           `yaml:"max_open_conns"`
	MaxIdleConns    int           `yaml:"max_idle_conns"`
	ConnMaxLifetime time.Duration `yaml:"conn_max_lifetime"`
}

type RabbitMQConfig struct {
	URL           string `yaml:"url"`
	QueueName     string `yaml:"queue_name"`
	PrefetchCount int    `yaml:"prefetch_count"`
}

type MinIOConfig struct {
	Endpoint   string `yaml:"endpoint"`
	AccessKey  string `yaml:"access_key"`
	SecretKey  string `yaml:"secret_key"`
	BucketName string `yaml:"bucket_name"`
	UseSSL     bool   `yaml:"use_ssl"`
}

type ValkeyConfig struct {
	URL      string `yaml:"url"`
	Password string `yaml:"password"`
}

type JudgeConfig struct {
	WorkerCount        int           `yaml:"worker_count"`
	WorkerTimeout      time.Duration `yaml:"worker_timeout"`
	MaxQueueSize       int           `yaml:"max_queue_size"`
	DefaultTimeLimit   time.Duration `yaml:"default_time_limit"`
	DefaultMemoryLimit int           `yaml:"default_memory_limit"`
	MaxTimeLimit       time.Duration `yaml:"max_time_limit"`
	MaxMemoryLimit     int           `yaml:"max_memory_limit"`
	MaxStackSize       int           `yaml:"max_stack_size"`
	MaxOutputSize      int           `yaml:"max_output_size"`
}

type IsolateConfig struct {
	Path     string `yaml:"path"`
	BoxRoot  string `yaml:"box_root"`
	MaxBoxes int    `yaml:"max_boxes"`
}

func Load() (*Config, error) {
	cfg := &Config{}

	if err := loadFromYAML(cfg); err != nil {
		return nil, err
	}

	if err := loadFromEnv(cfg); err != nil {
		return nil, err
	}

	return cfg, nil
}

func loadFromYAML(cfg *Config) error {
	configFile := "config.yaml"
	if _, err := os.Stat(configFile); os.IsNotExist(err) {
		return nil
	}

	data, err := os.ReadFile(configFile)
	if err != nil {
		return fmt.Errorf("failed to read config file: %w", err)
	}

	if err := yaml.Unmarshal(data, cfg); err != nil {
		return fmt.Errorf("failed to parse config file: %w", err)
	}

	return nil
}

func loadFromEnv(cfg *Config) error {
	if port := os.Getenv("SERVICE_PORT"); port != "" {
		cfg.Server.Port = port
	}
	if cfg.Server.Port == "" {
		cfg.Server.Port = "3003"
	}

	if dbURL := os.Getenv("DATABASE_URL"); dbURL != "" {
		cfg.Database.URL = dbURL
	}

	if rabbitURL := os.Getenv("RABBITMQ_URL"); rabbitURL != "" {
		cfg.RabbitMQ.URL = rabbitURL
	}

	if queueName := os.Getenv("RABBITMQ_QUEUE_NAME"); queueName != "" {
		cfg.RabbitMQ.QueueName = queueName
	}
	if cfg.RabbitMQ.QueueName == "" {
		cfg.RabbitMQ.QueueName = "judge.submissions"
	}

	if prefetchCount := os.Getenv("RABBITMQ_PREFETCH_COUNT"); prefetchCount != "" {
		if count, err := strconv.Atoi(prefetchCount); err == nil {
			cfg.RabbitMQ.PrefetchCount = count
		}
	}
	if cfg.RabbitMQ.PrefetchCount == 0 {
		cfg.RabbitMQ.PrefetchCount = 1
	}

	if endpoint := os.Getenv("MINIO_ENDPOINT"); endpoint != "" {
		cfg.MinIO.Endpoint = endpoint
	}
	if accessKey := os.Getenv("MINIO_ACCESS_KEY"); accessKey != "" {
		cfg.MinIO.AccessKey = accessKey
	}
	if secretKey := os.Getenv("MINIO_SECRET_KEY"); secretKey != "" {
		cfg.MinIO.SecretKey = secretKey
	}
	if bucketName := os.Getenv("MINIO_BUCKET_NAME"); bucketName != "" {
		cfg.MinIO.BucketName = bucketName
	}
	if cfg.MinIO.BucketName == "" {
		cfg.MinIO.BucketName = "submissions"
	}

	if useSSL := os.Getenv("MINIO_USE_SSL"); useSSL != "" {
		if ssl, err := strconv.ParseBool(useSSL); err == nil {
			cfg.MinIO.UseSSL = ssl
		}
	}

	if valkeyURL := os.Getenv("VALKEY_URL"); valkeyURL != "" {
		cfg.Valkey.URL = valkeyURL
	}

	if valkeyPassword := os.Getenv("VALKEY_PASSWORD"); valkeyPassword != "" {
		cfg.Valkey.Password = valkeyPassword
	}

	if workerCount := os.Getenv("WORKER_COUNT"); workerCount != "" {
		if count, err := strconv.Atoi(workerCount); err == nil {
			cfg.Judge.WorkerCount = count
		}
	}
	if cfg.Judge.WorkerCount == 0 {
		cfg.Judge.WorkerCount = 4
	}

	if workerTimeout := os.Getenv("WORKER_TIMEOUT_SECONDS"); workerTimeout != "" {
		if timeout, err := strconv.Atoi(workerTimeout); err == nil {
			cfg.Judge.WorkerTimeout = time.Duration(timeout) * time.Second
		}
	}
	if cfg.Judge.WorkerTimeout == 0 {
		cfg.Judge.WorkerTimeout = 60 * time.Second
	}

	if maxQueueSize := os.Getenv("MAX_QUEUE_SIZE"); maxQueueSize != "" {
		if size, err := strconv.Atoi(maxQueueSize); err == nil {
			cfg.Judge.MaxQueueSize = size
		}
	}
	if cfg.Judge.MaxQueueSize == 0 {
		cfg.Judge.MaxQueueSize = 1000
	}

	if isolatePath := os.Getenv("ISOLATE_PATH"); isolatePath != "" {
		cfg.Isolate.Path = isolatePath
	}
	if cfg.Isolate.Path == "" {
		cfg.Isolate.Path = "/usr/local/bin/isolate"
	}

	if boxRoot := os.Getenv("ISOLATE_BOX_ROOT"); boxRoot != "" {
		cfg.Isolate.BoxRoot = boxRoot
	}
	if cfg.Isolate.BoxRoot == "" {
		cfg.Isolate.BoxRoot = "/var/local/lib/isolate"
	}

	return nil
}
