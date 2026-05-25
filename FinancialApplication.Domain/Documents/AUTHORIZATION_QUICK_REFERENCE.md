# Authorization Quick Reference Guide

## 5-Minute Quick Start

### 1. Database Setup
Only 1 table needed - Roles:
```sql
INSERT INTO Roles (Name, IsActive) VALUES
('Admin', 1),
('Manager', 1),
('Auditor', 1),
('User', 1)
```

### 2. Program.cs Setup
```csharp
// Add JWT Authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* config */ });

// Add Authorization Policies (ALL PERMISSIONS DEFINED HERE)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("ViewAllUsers", p => p.RequireRole("Admin", "Manager"));
    options.AddPolicy("ViewAuditLogs", p => p.RequireRole("Admin", "Manager", "Auditor"));
});

// Add Services
builder.Services.AddScoped<AuthorizationService>();
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<AuditService>();
```

### 3. Controller Usage
```csharp
[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly TransactionService _service;
    
    // Public endpoint - anyone can call
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request) { }
    
    // Requires authentication only
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyTransactions() { }
    
    // Requires specific role
    [Authorize(Roles = "Admin,Manager")]
    [HttpGet("all")]
    public async Task<IActionResult> GetAllTransactions() { }
    
    // Requires specific policy
    [Authorize(Policy = "ViewAuditLogs")]
    [HttpGet("audit")]
    public async Task<IActionResult> GetAuditLogs() { }
}
```

### 4. Service Layer
```csharp
public class TransactionService
{
    private readonly AuthorizationService _auth;
    
    public async Task<List<Transaction>> GetTransactions(Guid userId)
    {
        // Check authorization in service
        _auth.EnsureCanViewUserData(userId);
        
        return await _db.Transactions
            .Where(t => t.UserId == userId)
            .ToListAsync();
    }
}
```

---

## Common Authorization Patterns

### Pattern 1: Check Current User Role
```csharp
var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
if (userRole != "Admin")
    return Forbid();
```

### Pattern 2: Check If User Owns Resource
```csharp
var transaction = await _db.Transactions.FindAsync(id);
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

if (transaction.UserId.ToString() != userId && !User.IsInRole("Admin"))
    return Forbid();
```

### Pattern 3: Get Current User ID
```csharp
var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
var guidUserId = Guid.Parse(currentUserId);
```

### Pattern 4: Check Multiple Roles
```csharp
var allowedRoles = new[] { "Admin", "Manager", "Auditor" };
var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

if (!allowedRoles.Contains(userRole))
    return Forbid();
```

---

## Role Permissions Reference

### Admin
- ✅ Full access
- ✅ Can manage users
- ✅ Can view everything
- ✅ Can delete anything
- ✅ Can view audit logs

### Manager
- ✅ Can view all users' data
- ✅ Can create users
- ✅ Can view audit logs
- ❌ Cannot delete users
- ❌ Cannot change roles

### Auditor
- ✅ Can view all data (read-only)
- ✅ Can view audit logs
- ❌ Cannot modify anything

### User (Regular)
- ✅ Can view own data
- ✅ Can create own records
- ✅ Can edit own records
- ❌ Cannot view others' data

---

## AuthorizationService Helper Methods

```csharp
// Get current user info
Guid GetCurrentUserId()
string GetCurrentUserRole()

// Role checks
bool IsAdmin()
bool IsManager()
bool IsAuditor()
bool IsAdminOrManager()

// Permission checks
bool CanViewUserData(Guid userId)
bool CanEditUserData(Guid userId)
bool CanDelete()

// Throw if unauthorized
void EnsureCanViewUserData(Guid userId)
void EnsureCanEditUserData(Guid userId)
void EnsureIsAdmin()
```

---

## Common HTTP Status Codes

```
200 OK                  → Success
201 Created             → Resource created
400 Bad Request         → Invalid input
401 Unauthorized        → No valid token
403 Forbidden           → Valid token but no permission
404 Not Found           → Resource doesn't exist
500 Internal Server Error → Server error
```

---

## JWT Token Structure

```
Header.Payload.Signature

Example Payload:
{
  "sub": "550e8400-e29b-41d4-a716-446655440000",
  "role": "Manager",
  "email": "user@example.com",
  "username": "john_doe",
  "iat": 1234567890,
  "exp": 1234571490
}
```

---

## appsettings.json Configuration

```json
{
  "JwtSettings": {
    "SecretKey": "your-256-bit-secret-key-min-32-chars",
    "ExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

---

## Testing Authorization

### Test as Admin
1. Login with admin user
2. Call protected endpoints
3. Should succeed

### Test as Manager
1. Login with manager user
2. Can view all users but not delete
3. Cannot access admin-only endpoints

### Test as Regular User
1. Login with regular user
2. Can only access own data
3. Cannot view other users' data
4. Cannot access admin/manager endpoints

### Test Unauthorized
1. Don't include JWT token
2. Should get 401 Unauthorized

### Test Forbidden
1. Include valid JWT but insufficient role
2. Should get 403 Forbidden

---

## Debugging Tips

### Problem: Getting 401 Unauthorized
- ❌ JWT token missing or invalid
- ✅ Check Authorization header format
- ✅ Verify token hasn't expired
- ✅ Check JWT secret key matches

### Problem: Getting 403 Forbidden
- ❌ User role insufficient for endpoint
- ✅ Verify user's role in database
- ✅ Check policy definition in Program.cs
- ✅ Check [Authorize] attribute

### Problem: Claims Not Found
- ❌ Claims not added to token
- ✅ Check AuthenticationService.GetUserClaims()
- ✅ Verify claims added before token generation
- ✅ Check JWT configuration

### Problem: Service Can't Get UserId
- ❌ IHttpContextAccessor not injected
- ✅ Add builder.Services.AddHttpContextAccessor()
- ✅ Inject IHttpContextAccessor in constructor
- ✅ Verify User context is available

---

## Remember

🎯 **No Permission Table Needed** - All permissions are in Program.cs  
🎯 **Single Source of Truth** - Authorization policies defined once  
🎯 **Simple & Fast** - No database joins for permission checks  
🎯 **Maintainable** - Update permissions via code deployments  

❌ Don't store permissions in database  
❌ Don't hardcode role checks everywhere  
✅ Use AuthorizationService helper methods  
✅ Use [Authorize] attributes consistently  
