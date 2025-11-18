.PHONY: build run test clean docker-build docker-run migrate-up migrate-down

BUILD_DIR := build
BINARY_NAME := execution-service
MIGRATE_NAME := migrate

build:
	@echo "Building execution service..."
	@mkdir -p $(BUILD_DIR)
	@CGO_ENABLED=0 GOOS=linux go build -o $(BUILD_DIR)/$(BINARY_NAME) cmd/server/main.go
	@CGO_ENABLED=0 GOOS=linux go build -o $(BUILD_DIR)/$(MIGRATE_NAME) cmd/migrate/main.go

run:
	@echo "Running execution service..."
	@go run cmd/server/main.go

test:
	@echo "Running tests..."
	@go test -v ./...

clean:
	@echo "Cleaning up..."
	@rm -rf $(BUILD_DIR)
	@go clean

docker-build:
	@echo "Building Docker image..."
	@docker build -t execution-service:latest .

docker-run:
	@echo "Running Docker container..."
	@docker run --rm -it --privileged \
		-p 3003:3003 \
		-e DATABASE_URL="postgresql://localhost:5432/codehakam" \
		-e RABBITMQ_URL="amqp://localhost:5672" \
		-e MINIO_ENDPOINT="localhost:9000" \
		-e MINIO_ACCESS_KEY="minioadmin" \
		-e MINIO_SECRET_KEY="minioadmin" \
		-e VALKEY_URL="redis://localhost:6379" \
		execution-service:latest

migrate-up:
	@echo "Running database migrations..."
	@go run cmd/migrate/main.go up

migrate-down:
	@echo "Rolling back database migration..."
	@go run cmd/migrate/main.go down

migrate-status:
	@echo "Checking migration status..."
	@go run cmd/migrate/main.go status

dev-setup: migrate-up
	@echo "Setting up development environment..."

lint:
	@echo "Running linter..."
	@golangci-lint run

fmt:
	@echo "Formatting code..."
	@go fmt ./...

mod-tidy:
	@echo "Tidying go.mod..."
	@go mod tidy

deps-update:
	@echo "Updating dependencies..."
	@go get -u ./...
	@go mod tidy