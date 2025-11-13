# Kong API Gateway Configuration

## Overview

Kong API Gateway is configured in DB-less mode using declarative configuration. It handles:
- JWT authentication validation
- Request routing to microservices
- Rate limiting per service
- User claim extraction and forwarding as headers
- CORS handling
- Request tracing and logging

## Architecture

```
Client Request
    ↓
Kong Gateway :8000
    ↓ (validates JWT)
    ↓ (extracts claims)
    ↓ (forwards as headers: X-User-Id, X-User-Roles, X-User-Email)
    ↓
Backend Service (account/content/execution/contest)
    ↓ (trusts headers from gateway)
    ↓ (validates HMAC signature)
    ↓
Response
```

## Services Configuration

### Account Service (Port 3001)
- Routes: `/api/auth`, `/api/users`, `/api/admin`
- Rate Limit: 100/min, 1000/hour
- JWT required for protected endpoints

### Content Service (Port 3002)
- Routes: `/api/problems`, `/api/testcases`, `/api/editorials`, `/api/discussions`, `/api/problemlists`
- Rate Limit: 200/min, 2000/hour
- CORS enabled for frontend
- JWT required

### Execution Service (Port 3003)
- Routes: `/api/submissions`, `/api/judge`
- Rate Limit: 30/min, 500/hour (stricter due to expensive operations)
- JWT required

### Contest Service (Port 3004)
- Routes: `/api/contests`, `/api/leaderboard`
- Rate Limit: 100/min, 1000/hour
- JWT required

## JWT Configuration

Kong validates JWT tokens with:
- Algorithm: HS256
- Secret: `your-secret-key-min-32-characters-long-for-security`
- Required claims: `exp` (expiration)
- Issuer: `codehakam`

**Important:** Change the JWT secret in production!

## Headers Forwarded to Services

Kong extracts JWT claims and forwards them as HTTP headers:

- `X-User-Id`: User ID from JWT `sub` claim
- `X-User-Roles`: Comma-separated roles from JWT `roles` claim
- `X-User-Email`: User email from JWT `email` claim
- `X-Gateway-Signature`: HMAC signature for request validation
- `X-Request-Id`: Correlation ID for tracing

## Usage

### Starting Kong

```bash
# Start all infrastructure including Kong
docker compose -f dev-infra.compose.yml up -d

# Check Kong status
curl http://localhost:8001/status

# View Kong configuration
curl http://localhost:8001
```

### Testing Through Gateway

```bash
# 1. Login to get JWT token
curl -X POST http://localhost:8000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "password123"
  }'

# Response: {"token": "eyJhbGc..."}

# 2. Use token to access protected endpoints
curl http://localhost:8000/api/problems \
  -H "Authorization: Bearer eyJhbGc..."

# Kong will:
# - Validate JWT
# - Extract user claims
# - Forward to content-service:3002 with headers
# - Return response
```

### Direct Service Access (Local Development)

You can bypass Kong for local development:

```bash
# Access service directly
curl http://localhost:3002/api/problems \
  -H "X-User-Id: 1" \
  -H "X-User-Roles: admin"

# Service accepts direct headers in development mode
```

## Rate Limiting

Rate limits are configured per service:

| Service | Per Minute | Per Hour |
|---------|-----------|----------|
| Account | 100 | 1000 |
| Content | 200 | 2000 |
| Execution | 30 | 500 |
| Contest | 100 | 1000 |

When limit is exceeded:
```json
HTTP/1.1 429 Too Many Requests
{
  "message": "API rate limit exceeded"
}
```

## CORS Configuration

CORS is enabled for Content Service:
- Allowed Origins: `http://localhost:3000`, `http://localhost:5173`
- Allowed Methods: GET, POST, PUT, PATCH, DELETE, OPTIONS
- Credentials: Enabled
- Max Age: 3600 seconds

## Admin API

Kong Admin API is available at `http://localhost:8001`

Useful endpoints:
```bash
# List all services
curl http://localhost:8001/services

# List all routes
curl http://localhost:8001/routes

# List all plugins
curl http://localhost:8001/plugins

# View service health
curl http://localhost:8001/status

# Get metrics (Prometheus format)
curl http://localhost:8001/metrics
```

## Monitoring

Kong exposes Prometheus metrics at `http://localhost:8001/metrics`

Metrics include:
- Request count per service
- Request latency (P50, P95, P99)
- Error rates
- Bandwidth usage
- Plugin execution time

## Troubleshooting

### Kong not starting

```bash
# Check logs
docker compose -f dev-infra.compose.yml logs kong

# Validate configuration
docker run --rm -v $(pwd)/config/kong:/kong kong:3.9-alpine kong config parse /kong/kong.yml
```

### JWT validation failing

```bash
# Check if JWT secret matches between account-service and Kong
# Account-service: appsettings.json -> JwtSettings:SecretKey
# Kong: config/kong/kong.yml -> consumers.jwt_secrets.secret

# Test JWT decoding
echo "eyJhbGc..." | cut -d'.' -f2 | base64 -d | jq .
```

### Service not reachable through Kong

```bash
# Check if service is running
curl http://localhost:3002/health

# Check Kong routing
curl http://localhost:8001/services/content-service

# Test without JWT (should get 401)
curl -v http://localhost:8000/api/problems

# Test with JWT
curl -v http://localhost:8000/api/problems \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Rate limit testing

```bash
# Generate rapid requests to test rate limiting
for i in {1..150}; do
  curl http://localhost:8000/api/problems \
    -H "Authorization: Bearer $TOKEN" \
    -w "\nStatus: %{http_code}\n"
  sleep 0.1
done

# Should see 429 after 200 requests per minute
```

## Production Considerations

Before deploying to production:

1. **Change JWT Secret**: Update in both `kong.yml` and account-service config
2. **Enable HTTPS**: Configure SSL certificates in Kong
3. **Database Mode**: Consider PostgreSQL backend for multi-instance Kong
4. **Rate Limiting**: Adjust based on actual traffic patterns
5. **CORS Origins**: Restrict to production domain only
6. **Logging**: Configure external log aggregation (e.g., Elasticsearch)
7. **Monitoring**: Integrate with Prometheus/Grafana
8. **HMAC Secret**: Configure gateway signature secret for header validation

## Configuration Changes

To modify Kong configuration:

1. Edit `config/kong/kong.yml`
2. Restart Kong: `docker compose -f dev-infra.compose.yml restart kong`
3. Verify: `curl http://localhost:8001/status`

Kong reloads declarative configuration on restart automatically.

## References

- [Kong Documentation](https://docs.konghq.com/)
- [Kong DB-less Mode](https://docs.konghq.com/gateway/latest/production/deployment-topologies/db-less-and-declarative-config/)
- [Kong JWT Plugin](https://docs.konghq.com/hub/kong-inc/jwt/)
- [Kong Rate Limiting](https://docs.konghq.com/hub/kong-inc/rate-limiting/)
