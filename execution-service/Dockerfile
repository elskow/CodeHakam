FROM golang:1.21-alpine AS builder

WORKDIR /app

RUN apk add --no-cache git ca-certificates

COPY go.mod go.sum ./
RUN go mod download

COPY . .

RUN CGO_ENABLED=0 GOOS=linux go build -o execution-service cmd/server/main.go
RUN CGO_ENABLED=0 GOOS=linux go build -o migrate cmd/migrate/main.go

FROM alpine:latest

RUN apk --no-cache add ca-certificates isolate bash

RUN adduser -D -s /bin/sh appuser

WORKDIR /app

COPY --from=builder /app/execution-service .
COPY --from=builder /app/migrate .
COPY --from=builder /app/config.yaml .

RUN mkdir -p /var/local/lib/isolate
RUN chown -R appuser:appuser /app /var/local/lib/isolate

USER appuser

EXPOSE 3003

CMD ["./execution-service"]