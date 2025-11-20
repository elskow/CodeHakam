package rbac

import (
	"fmt"
	"log"
	"strconv"
	"strings"

	"execution_service/internal/database"

	"github.com/casbin/casbin/v2"
	gormadapter "github.com/casbin/gorm-adapter/v3"
	_ "github.com/lib/pq"
)

type RBACService struct {
	enforcer *casbin.Enforcer
	db       *database.DB
}

type Permission struct {
	Resource string `json:"resource"`
	Action   string `json:"action"`
	Scope    string `json:"scope"`
}

type Role struct {
	Name        string       `json:"name"`
	Permissions []Permission `json:"permissions"`
	IsSystem    bool         `json:"is_system"`
	Description string       `json:"description"`
}

func NewRBACService(databaseURL string, db *database.DB) (*RBACService, error) {
	// Initialize Gorm adapter for Casbin
	adapter, err := gormadapter.NewAdapter("postgres", databaseURL, "casbin_rule")
	if err != nil {
		return nil, fmt.Errorf("failed to create Casbin adapter: %w", err)
	}

	// Create enforcer with model configuration
	enforcer, err := casbin.NewEnforcer("rbac_model.conf", adapter)
	if err != nil {
		return nil, fmt.Errorf("failed to create Casbin enforcer: %w", err)
	}

	rbac := &RBACService{
		enforcer: enforcer,
		db:       db,
	}

	// Initialize default roles and permissions
	if err := rbac.initializeDefaultRoles(); err != nil {
		log.Printf("Warning: Failed to initialize default roles: %v", err)
	}

	return rbac, nil
}

func (r *RBACService) CheckPermission(userID int64, resource, action string) (bool, error) {
	userIDStr := strconv.FormatInt(userID, 10)

	// Check direct permission
	allowed, err := r.enforcer.Enforce(userIDStr, resource, action)
	if err != nil {
		return false, fmt.Errorf("failed to check permission: %w", err)
	}

	return allowed, nil
}

func (r *RBACService) HasRole(userID int64, role string) (bool, error) {
	userIDStr := strconv.FormatInt(userID, 10)

	roles, err := r.enforcer.GetRolesForUser(userIDStr)
	if err != nil {
		return false, fmt.Errorf("failed to get user roles: %w", err)
	}

	for _, userRole := range roles {
		if userRole == role {
			return true, nil
		}
	}

	return false, nil
}

func (r *RBACService) AssignRole(userID int64, role string) error {
	userIDStr := strconv.FormatInt(userID, 10)

	_, err := r.enforcer.AddRoleForUser(userIDStr, role)
	if err != nil {
		return fmt.Errorf("failed to assign role: %w", err)
	}

	return r.enforcer.SavePolicy()
}

func (r *RBACService) RemoveRole(userID int64, role string) error {
	userIDStr := strconv.FormatInt(userID, 10)

	_, err := r.enforcer.DeleteRoleForUser(userIDStr, role)
	if err != nil {
		return fmt.Errorf("failed to remove role: %w", err)
	}

	return r.enforcer.SavePolicy()
}

func (r *RBACService) AddPermission(role, resource, action string) error {
	_, err := r.enforcer.AddPolicy(role, resource, action)
	if err != nil {
		return fmt.Errorf("failed to add permission: %w", err)
	}

	return r.enforcer.SavePolicy()
}

func (r *RBACService) RemovePermission(role, resource, action string) error {
	_, err := r.enforcer.RemovePolicy(role, resource, action)
	if err != nil {
		return fmt.Errorf("failed to remove permission: %w", err)
	}

	return r.enforcer.SavePolicy()
}

func (r *RBACService) GetUserRoles(userID int64) ([]string, error) {
	userIDStr := strconv.FormatInt(userID, 10)

	roles, err := r.enforcer.GetRolesForUser(userIDStr)
	if err != nil {
		return nil, fmt.Errorf("failed to get user roles: %w", err)
	}

	return roles, nil
}

func (r *RBACService) GetRolePermissions(role string) ([]string, error) {
	permissions, err := r.enforcer.GetPermissionsForUser(role)
	if err != nil {
		return nil, fmt.Errorf("failed to get role permissions: %w", err)
	}

	var result []string
	for _, permission := range permissions {
		if len(permission) >= 3 {
			result = append(result, fmt.Sprintf("%s:%s", permission[1], permission[2]))
		}
	}

	return result, nil
}

func (r *RBACService) GetAllRoles() []Role {
	// Get all unique roles from policies
	policies, _ := r.enforcer.GetPolicy()
	roleMap := make(map[string]Role)

	for _, policy := range policies {
		if len(policy) >= 3 {
			role := policy[0]
			resource := policy[1]
			action := policy[2]

			if existingRole, exists := roleMap[role]; exists {
				// Add permission to existing role
				existingRole.Permissions = append(existingRole.Permissions, Permission{
					Resource: resource,
					Action:   action,
				})
				roleMap[role] = existingRole
			} else {
				// Create new role
				roleMap[role] = Role{
					Name: role,
					Permissions: []Permission{
						{Resource: resource, Action: action},
					},
					IsSystem:    r.isSystemRole(role),
					Description: r.getRoleDescription(role),
				}
			}
		}
	}

	var roles []Role
	for _, role := range roleMap {
		roles = append(roles, role)
	}

	return roles
}

func (r *RBACService) initializeDefaultRoles() error {
	defaultRoles := map[string][]Permission{
		"user": {
			{Resource: "submission", Action: "create", Scope: ""},
			{Resource: "submission", Action: "read:own", Scope: "own"},
			{Resource: "problem", Action: "read", Scope: ""},
			{Resource: "language", Action: "read", Scope: ""},
		},
		"setter": {
			{Resource: "submission", Action: "read:any", Scope: "any"},
			{Resource: "problem", Action: "create", Scope: ""},
			{Resource: "problem", Action: "edit:own", Scope: "own"},
			{Resource: "problem", Action: "delete:own", Scope: "own"},
			{Resource: "testcase", Action: "create", Scope: ""},
			{Resource: "testcase", Action: "edit:own", Scope: "own"},
			{Resource: "testcase", Action: "delete:own", Scope: "own"},
			{Resource: "submission", Action: "rejudge:own", Scope: "own"},
		},
		"moderator": {
			{Resource: "submission", Action: "read:any", Scope: "any"},
			{Resource: "problem", Action: "read:any", Scope: "any"},
			{Resource: "discussion", Action: "moderate", Scope: ""},
			{Resource: "report", Action: "review", Scope: ""},
		},
		"admin": {
			{Resource: "user", Action: "manage", Scope: ""},
			{Resource: "problem", Action: "manage", Scope: ""},
			{Resource: "submission", Action: "rejudge:any", Scope: "any"},
			{Resource: "contest", Action: "manage", Scope: ""},
			{Resource: "system", Action: "configure", Scope: ""},
			{Resource: "audit", Action: "view", Scope: ""},
			{Resource: "worker", Action: "manage", Scope: ""},
			{Resource: "queue", Action: "manage", Scope: ""},
		},
		"super_admin": {
			{Resource: "*", Action: "*", Scope: ""}, // Full access
		},
	}

	for role, permissions := range defaultRoles {
		for _, permission := range permissions {
			r.enforcer.AddPolicy(role, permission.Resource, permission.Action)
		}
	}

	return r.enforcer.SavePolicy()
}

func (r *RBACService) isSystemRole(role string) bool {
	systemRoles := []string{"user", "setter", "moderator", "admin", "super_admin"}
	for _, systemRole := range systemRoles {
		if role == systemRole {
			return true
		}
	}
	return false
}

func (r *RBACService) getRoleDescription(role string) string {
	descriptions := map[string]string{
		"user":        "Regular platform user with basic submission permissions",
		"setter":      "Problem creator with content management permissions",
		"moderator":   "Content moderator with review and moderation permissions",
		"admin":       "Platform administrator with system management permissions",
		"super_admin": "Platform owner with full system access",
	}

	if desc, exists := descriptions[role]; exists {
		return desc
	}
	return "Custom role"
}

func (r *RBACService) ValidateAction(resource, action string) bool {
	// Parse action format (e.g., "read:own", "edit:any", "create")
	parts := strings.Split(action, ":")

	if len(parts) == 1 {
		// Simple action like "create", "read"
		validActions := []string{"create", "read", "update", "delete", "manage"}
		for _, validAction := range validActions {
			if parts[0] == validAction {
				return true
			}
		}
	} else if len(parts) == 2 {
		// Scoped action like "read:own", "edit:any"
		baseAction := parts[0]
		scope := parts[1]

		validBaseActions := []string{"read", "edit", "delete", "rejudge"}
		validScopes := []string{"own", "any"}

		for _, validAction := range validBaseActions {
			if baseAction == validAction {
				for _, validScope := range validScopes {
					if scope == validScope {
						return true
					}
				}
			}
		}
	}

	return false
}

func (r *RBACService) CreateCustomRole(name, description string, permissions []Permission) error {
	// Check if role already exists
	roles := r.GetAllRoles()
	for _, existingRole := range roles {
		if existingRole.Name == name {
			return fmt.Errorf("role %s already exists", name)
		}
	}

	// Add permissions for the new role
	for _, permission := range permissions {
		if !r.ValidateAction(permission.Resource, permission.Action) {
			return fmt.Errorf("invalid permission: %s:%s", permission.Resource, permission.Action)
		}

		_, err := r.enforcer.AddPolicy(name, permission.Resource, permission.Action)
		if err != nil {
			return fmt.Errorf("failed to add permission %s:%s: %w", permission.Resource, permission.Action, err)
		}
	}

	return r.enforcer.SavePolicy()
}

func (r *RBACService) DeleteRole(role string) error {
	if r.isSystemRole(role) {
		return fmt.Errorf("cannot delete system role: %s", role)
	}

	// Remove all policies for this role
	_, err := r.enforcer.RemoveFilteredPolicy(0, role)
	if err != nil {
		return fmt.Errorf("failed to delete role: %w", err)
	}

	// Remove all role assignments for this role
	_, err = r.enforcer.DeleteRole(role)
	if err != nil {
		return fmt.Errorf("failed to delete role assignments: %w", err)
	}

	return r.enforcer.SavePolicy()
}

func (r *RBACService) RefreshPolicy() error {
	return r.enforcer.LoadPolicy()
}
