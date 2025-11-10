# CodeHakam

A competitive programming platform built with microservices architecture, designed for hosting programming contests, practice problems, and educational content.

## Overview

CodeHakam is an open-source competitive programming platform inspired by Codeforces, TLX TOKI, and DOMjudge. The platform provides a secure, scalable environment for competitive programming with real-time scoreboards, multiple contest formats, and comprehensive judging capabilities.

### Key Features

- **Secure Code Execution**: Battle-tested Isolate sandbox for safe code execution
- **Multiple Contest Formats**: ICPC (ACM), IOI, and practice modes
- **Real-time Scoreboard**: Live updates via SignalR WebSocket
- **Multi-language Support**: C, C++, Python, Java, Go, Rust
- **Priority Judge Queue**: Contest submissions prioritized over practice (RabbitMQ + JSON)
- **Async Architecture**: Queue-based submission processing with RabbitMQ
- **Discussion Forums**: Problem discussions and community interaction
- **Rating System**: Elo-based rating calculation
- **Virtual Contests**: Time-shifted contest participation
- **Team Contests**: Support for team-based competitions
- **Plagiarism Detection**: Automated code similarity analysis
- **Comprehensive Admin Tools**: Contest management, user moderation, analytics

## Architecture

CodeHakam implements a microservices architecture with 5 core services:

1. **Account Service** (.NET 9) - Authentication, profiles, ratings, RBAC
2. **Content Service** (.NET 9) - Problems, test cases, editorials, forums
3. **Execution Service** (Go 1.21+) - Judge workers consuming from RabbitMQ queue, Isolate sandbox integration
4. **Contest Service** (.NET 9 + SignalR) - Contests, real-time scoreboards, clarifications
5. **Platform Service** (Bun 1.0+) - Files, notifications, admin tools

### Technology Stack

- **Languages**: .NET 9 C# (3 services), Go 1.21+ (1 service), Bun 1.0+ (1 service)
- **Database**: PostgreSQL 16 + PgBouncer (connection pooling)
- **Cache & Session**: Valkey 7.2+ (Redis-compatible, Linux Foundation fork)
- **Message Queue**: RabbitMQ 3.12+ (priority queues, dead-letter exchange, JSON messages)
- **Object Storage**: Cloudflare R2 + CDN (production, zero egress fees), MinIO (local dev)
- **Migrations**: Goose (SQL + Go code migrations)
- **API Gateway**: Kong (Apache 2.0, DB-less mode)
- **Sandbox**: Isolate (Linux namespaces for code execution)
- **Monitoring**: Prometheus, Grafana, Jaeger, Loki + Promtail
- **Resilience**: Circuit Breakers (gobreaker, Polly)
- **Email**: AWS SES (transactional emails)
- **Backups**: pgBackRest (continuous archiving)
- **Containerization**: Docker, Docker Compose, Kubernetes

