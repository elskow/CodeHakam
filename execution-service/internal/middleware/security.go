package middleware

import (
	"fmt"
	"net/http"
	"strings"
	"time"

	"execution_service/internal/sandbox"
	"github.com/gin-gonic/gin"
	"github.com/golang-jwt/jwt/v5"
)

type SecurityMiddleware struct {
	securityValidator *sandbox.SecurityValidator
	jwtSecret         []byte
}

type userRequests struct {
	requests    []time.Time // Sliding window of request timestamps
	maxRequests int
	windowSize  time.Duration
}

func NewSecurityMiddleware(jwtSecret string) *SecurityMiddleware {
	config := sandbox.NewSecurityValidator(&sandbox.SecurityConfig{}).GetDefaultSecurityConfig()
	validator := sandbox.NewSecurityValidator(config)

	return &SecurityMiddleware{
		securityValidator: validator,
		jwtSecret:         []byte(jwtSecret),
	}
}

func (sm *SecurityMiddleware) SecurityHeaders() gin.HandlerFunc {
	return func(c *gin.Context) {
		c.Header("X-Content-Type-Options", "nosniff")
		c.Header("X-Frame-Options", "DENY")
		c.Header("X-XSS-Protection", "1; mode=block")
		c.Header("Strict-Transport-Security", "max-age=31536000; includeSubDomains")
		c.Header("Content-Security-Policy", "default-src 'self'")
		c.Header("Referrer-Policy", "strict-origin-when-cross-origin")
		c.Next()
	}
}

func (sm *SecurityMiddleware) JWTRateLimit(requestsPerMinute int) gin.HandlerFunc {
	users := make(map[string]*userRequests)

	return func(c *gin.Context) {
		userID := sm.extractUserIDFromJWT(c)
		if userID == "" {
			sm.handleUnauthenticatedRateLimit(c, requestsPerMinute/10)
			return
		}

		now := time.Now()

		user, exists := users[userID]
		if !exists {
			users[userID] = &userRequests{
				requests:    []time.Time{now},
				maxRequests: requestsPerMinute,
				windowSize:  time.Minute,
			}
			c.Next()
			return
		}

		cutoff := now.Add(-user.windowSize)
		validRequests := make([]time.Time, 0)
		for _, reqTime := range user.requests {
			if reqTime.After(cutoff) {
				validRequests = append(validRequests, reqTime)
			}
		}
		user.requests = validRequests

		if len(user.requests) >= user.maxRequests {
			oldestRequest := user.requests[0]
			resetTime := oldestRequest.Add(user.windowSize)

			c.JSON(http.StatusTooManyRequests, gin.H{
				"error":      "Rate limit exceeded",
				"reset_time": resetTime.Unix(),
				"limit":      user.maxRequests,
				"window":     user.windowSize.String(),
			})
			c.Abort()
			return
		}

		user.requests = append(user.requests, now)

		if len(users) > 10000 {
			sm.cleanupOldUserEntries(users)
		}

		c.Next()
	}
}

func (sm *SecurityMiddleware) extractUserIDFromJWT(c *gin.Context) string {
	authHeader := c.GetHeader("Authorization")
	if authHeader == "" {
		return ""
	}

	parts := strings.Split(authHeader, " ")
	if len(parts) != 2 || parts[0] != "Bearer" {
		return ""
	}

	tokenString := parts[1]

	token, err := jwt.Parse(tokenString, func(token *jwt.Token) (interface{}, error) {
		if _, ok := token.Method.(*jwt.SigningMethodHMAC); !ok {
			return nil, fmt.Errorf("unexpected signing method: %v", token.Header["alg"])
		}
		return sm.jwtSecret, nil
	})

	if err != nil {
		return ""
	}

	if claims, ok := token.Claims.(jwt.MapClaims); ok && token.Valid {
		if userID, ok := claims["user_id"].(string); ok {
			return userID
		}
		if userID, ok := claims["user_id"].(float64); ok {
			return fmt.Sprintf("%.0f", userID)
		}
	}

	return ""
}

func (sm *SecurityMiddleware) handleUnauthenticatedRateLimit(c *gin.Context, requestsPerMinute int) {
	c.JSON(http.StatusTooManyRequests, gin.H{
		"error": "Authentication required for higher rate limits",
		"limit": requestsPerMinute,
	})
	c.Abort()
}

func (sm *SecurityMiddleware) cleanupOldUserEntries(users map[string]*userRequests) {
	now := time.Now()
	cutoff := now.Add(-5 * time.Minute)

	for userID, user := range users {
		if len(user.requests) == 0 {
			delete(users, userID)
			continue
		}

		lastRequest := user.requests[len(user.requests)-1]
		if lastRequest.Before(cutoff) {
			delete(users, userID)
		}
	}
}

func (sm *SecurityMiddleware) ValidateRequestSize(maxSize int64) gin.HandlerFunc {
	return func(c *gin.Context) {
		if c.Request.ContentLength > maxSize {
			c.JSON(http.StatusRequestEntityTooLarge, gin.H{"error": "Request too large"})
			c.Abort()
			return
		}

		c.Request.Body = http.MaxBytesReader(c.Writer, c.Request.Body, maxSize)
		c.Next()
	}
}

func (sm *SecurityMiddleware) ValidateContentType(allowedTypes ...string) gin.HandlerFunc {
	return func(c *gin.Context) {
		if c.Request.Method == "POST" || c.Request.Method == "PUT" || c.Request.Method == "PATCH" {
			contentType := c.GetHeader("Content-Type")
			if contentType == "" {
				c.JSON(http.StatusBadRequest, gin.H{"error": "Content-Type header required"})
				c.Abort()
				return
			}

			allowed := false
			for _, allowedType := range allowedTypes {
				if strings.HasPrefix(contentType, allowedType) {
					allowed = true
					break
				}
			}

			if !allowed {
				c.JSON(http.StatusUnsupportedMediaType, gin.H{
					"error": fmt.Sprintf("Content-Type %s not allowed", contentType),
				})
				c.Abort()
				return
			}
		}

		c.Next()
	}
}

func (sm *SecurityMiddleware) getClientIP(r *http.Request) string {
	xff := r.Header.Get("X-Forwarded-For")
	if xff != "" {
		ips := strings.Split(xff, ",")
		return strings.TrimSpace(ips[0])
	}

	xri := r.Header.Get("X-Real-IP")
	if xri != "" {
		return xri
	}

	return strings.Split(r.RemoteAddr, ":")[0]
}

func (sm *SecurityMiddleware) RequireAuth() gin.HandlerFunc {
	return func(c *gin.Context) {
		authHeader := c.GetHeader("Authorization")
		if authHeader == "" {
			c.JSON(http.StatusUnauthorized, gin.H{"error": "Authorization header required"})
			c.Abort()
			return
		}

		parts := strings.Split(authHeader, " ")
		if len(parts) != 2 || parts[0] != "Bearer" {
			c.JSON(http.StatusUnauthorized, gin.H{"error": "Bearer token required"})
			c.Abort()
			return
		}

		tokenString := parts[1]
		token, err := jwt.Parse(tokenString, func(token *jwt.Token) (interface{}, error) {
			if _, ok := token.Method.(*jwt.SigningMethodHMAC); !ok {
				return nil, fmt.Errorf("unexpected signing method: %v", token.Header["alg"])
			}
			return sm.jwtSecret, nil
		})

		if err != nil {
			c.JSON(http.StatusUnauthorized, gin.H{"error": "Invalid token: " + err.Error()})
			c.Abort()
			return
		}

		if claims, ok := token.Claims.(jwt.MapClaims); ok && token.Valid {
			// Check token expiration
			if exp, ok := claims["exp"].(float64); ok {
				if time.Now().Unix() > int64(exp) {
					c.JSON(http.StatusUnauthorized, gin.H{"error": "Token expired"})
					c.Abort()
					return
				}
			}

			// Set user context
			if userID, ok := claims["user_id"]; ok {
				c.Set("user_id", userID)
			}
			if username, ok := claims["username"]; ok {
				c.Set("username", username)
			}
			if role, ok := claims["role"]; ok {
				c.Set("role", role)
			}
			c.Next()
		} else {
			c.JSON(http.StatusUnauthorized, gin.H{"error": "Invalid token claims"})
			c.Abort()
		}
	}
}

func (sm *SecurityMiddleware) RequireAdmin() gin.HandlerFunc {
	return func(c *gin.Context) {
		role, exists := c.Get("role")
		if !exists {
			c.JSON(http.StatusUnauthorized, gin.H{"error": "User role not found"})
			c.Abort()
			return
		}

		if role != "admin" && role != "super_admin" {
			c.JSON(http.StatusForbidden, gin.H{"error": "Admin access required"})
			c.Abort()
			return
		}

		c.Next()
	}
}

func (sm *SecurityMiddleware) LogSecurityViolation(c *gin.Context, violation string) {
	fmt.Printf("SECURITY VIOLATION: %s from %s: %s\n",
		time.Now().Format(time.RFC3339), sm.getClientIP(c.Request), violation)
}
