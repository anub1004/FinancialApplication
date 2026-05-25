# Authorization Flow - Code-Based (No Permission Table)

## High-Level Authorization Flow

```
┌─────────────────┐
│  User Login     │
│ /auth/login     │
└────────┬────────┘
         │ (username, password)
         ▼
┌──────────────────────────────────────────┐
│  Authentication Service                  │
├──────────────────────────────────────────┤
│ 1. Find User by username                 │
│ 2. Verify password (bcrypt)              │
│ 3. Fetch User + Role                     │
│ 4. Generate JWT Token with claims:       │
│    - sub (UserId)                        │
│    - role (Admin/Manager/Auditor/User)   │
│    - email                               │
│    - username                            │
│ 5. Generate Refresh Token                │
│ 6. Store Refresh Token in DB             │
└────────┬─────────────────────────────────┘
         │ Response: { accessToken, refreshToken }
         ▼
┌─────────────────┐
│  Client         │ (Stores tokens locally)
└────────┬────────┘
         │ Header: Authorization: Bearer <accessToken>
         │
         ▼
┌──────────────────────────────────────────┐
│  API Endpoint (Protected)                │
├──────────────────────────────────────────┤
│ @Authorize                               │
│ [HttpGet("transactions")]                │
└────────┬─────────────────────────────────┘
         │
         ▼
┌──────────────────────────────────────────┐
│  JWT Validation Middleware               │
├──────────────────────────────────────────┤
│ 1. Verify signature                      │
│ 2. Check expiration                      │
│ 3. Extract claims                        │
│ 4. Create ClaimsPrincipal                │
└────────┬─────────────────────────────────┘
         │ User context available in controller
         ▼
┌──────────────────────────────────────────┐
│  Authorization Check (Program.cs Policy) │
├──────────────────────────────────────────┤
│ Example: [Authorize(Policy = "ViewAll")] │
│ Check if user has required role/claim    │
│ All defined in Program.cs - NO DB TABLE  │
└────────┬─────────────────────────────────┘
         │ YES: Allow
         │ NO: Return 403 Forbidden
         ▼
┌──────────────────────────────────────────┐
│  Service Layer Authorization             │
├──────────────────────────────────────────┤
│ AuthorizationService.EnsureCanView()     │
│ AuthorizationService.EnsureCanEdit()     │
│ Additional resource-level checks         │
└────────┬─────────────────────────────────┘
         │
         ▼
┌──────────────────────────────────────────┐
│  Business Logic + Audit Logging          │
├──────────────────────────────────────────┤
│ Execute operation                        │
│ Log to AuditLog table                    │
└────────┬─────────────────────────────────┘
         │
         ▼
┌──────────────────────────────────────────┐
│  Return Response                         │
├──────────────────────────────────────────┤
│ 200 OK or appropriate status code        │
└──────────────────────────────────────────┘
```

## Authorization Decision Tree

```
User Request
     │
     ▼
Is endpoint [Authorize]?
     │
     ├─ NO ──→ Allow (public endpoint)
     │
     └─ YES
          │
          ▼
     Valid JWT?
          │
          ├─ NO ──→ 401 Unauthorized
          │
          └─ YES
               │
               ▼
          Token Expired?
               │
               ├─ YES ──→ 401 Unauthorized (refresh needed)
               │
               └─ NO
                    │
                    ▼
               Check Policy
               (if specified)
                    │
          ┌─────────┴─────────┐
          │                   │
         YES                  NO
          │                   │
          ▼                   ▼
     Continue to         403 Forbidden
     Service Check
          │
          ▼
     Service Layer:
     AuthorizationService
     checks role/ownership
          │
          ├─ Allowed ──→ Execute Business Logic
          │
          └─ Denied ──→ 403 Forbidden
```

## Role-Permission Mapping (Code-Based)

### Program.cs Configuration

```csharp
// ALL Permissions defined here - Single Source of Truth
services.AddAuthorization(options =>
{
    // Admin Permissions
    options.AddPolicy("AdminOnly", 
        policy => policy.RequireRole("Admin"));
    
    options.AddPolicy("ManageUsers", 
        policy => policy.RequireRole("Admin"));
    
    // Manager Permissions (Read all, create users)
    options.AddPolicy("ViewAllUsers", 
        policy => policy.RequireRole("Admin", "Manager"));
    
    options.AddPolicy("CreateUsers", 
        policy => policy.RequireRole("Admin", "Manager"));
    
    // Auditor Permissions (Read-only)
    options.AddPolicy("ViewAuditLogs", 
        policy => policy.RequireRole("Admin", "Manager", "Auditor"));
    
    // User Permissions (Own data only)
    options.AddPolicy("EditOwnData", 
        policy => policy.RequireRole("User", "Manager", "Admin"));
});
```

### Service Layer Authorization

```csharp
public class AuthorizationService
{
    // Role checks
    public bool IsAdmin() => GetRole() == "Admin"
    public bool IsManager() => GetRole() == "Manager"
    public bool IsAuditor() => GetRole() == "Auditor"
    
    // Permission checks
    public bool CanViewAllUsers() => IsAdmin() || IsManager() || IsAuditor()
    public bool CanEditUser(Guid userId) => IsAdmin() || IsManager() || userId == CurrentUser
    public bool CanCreateUsers() => IsAdmin() || IsManager()
    public bool CanDeleteUsers() => IsAdmin()
    public bool CanViewAuditLogs() => IsAdmin() || IsManager() || IsAuditor()
}
```

## Database - Only 4 Roles

```
ROLES Table
───────────────────────────────────────
Id  │ Name      │ IsActive
────┼───────────┼──────────
1   │ Admin     │ true
2   │ Manager   │ Auditor
3   │ Auditor   │ true
4   │ User      │ true
───────────────────────────────────────

❌ NO PERMISSIONS TABLE ❌
❌ NO ROLE_PERMISSIONS TABLE ❌

All permissions managed in Program.cs
```

## Comparison: With vs Without Permission Table

### ❌ With Permission Table (Complex)
```
ROLES → ROLE_PERMISSIONS → PERMISSIONS ← USER_ROLES

Requires:
- 3 database tables
- Multiple joins
- Database migrations for new permissions
- More queries
- Complex caching logic
```

### ✅ Without Permission Table (Simple)
```
ROLES ← USERS

Requires:
- 1 database table
- Direct role check
- Permission changes via code deployment
- Single source of truth
- Better performance
```

## Implementation Checklist

```
PHASE 1: Setup JWT Authentication
├─ [ ] Configure JwtSettings in appsettings.json
├─ [ ] Add JWT authentication middleware
├─ [ ] Create AuthenticationService
└─ [ ] Create Login endpoint

PHASE 2: Define Authorization Policies
├─ [ ] Add all policies to Program.cs
├─ [ ] Create AuthorizationService class
├─ [ ] Add role-checking methods
└─ [ ] Create resource-checking methods

PHASE 3: Implement Controllers & Services
├─ [ ] Add [Authorize] attributes
├─ [ ] Call AuthorizationService in services
├─ [ ] Add proper error handling
└─ [ ] Test each endpoint

PHASE 4: Audit & Security
├─ [ ] Create AuditService
├─ [ ] Log all write operations
├─ [ ] Implement password hashing
├─ [ ] Add HTTPS enforcement
└─ [ ] Test with different roles
```

## Key Benefits

✅ **Simple**: Only 1 database table for roles  
✅ **Fast**: No permission table joins  
✅ **Maintainable**: Single source of truth in code  
✅ **Flexible**: Easy to add/modify permissions  
✅ **Scalable**: Permissions change with code deployments  
✅ **Auditable**: Complete audit trail of who did what  
