# Infrastructure Security Implementation Guide

## Overview

The Infrastructure layer contains all security-related components for the Financial Application:
- **JWT Token Generation** - Creating and validating JWT tokens
- **Authentication** - User login and token management
- **Authorization** - Permission checking and access control
- **Audit Logging** - Tracking all user actions
- **Password Hashing** - Secure password storage using PBKDF2-SHA256

---

## Components

### 1. JwtTokenGenerator

**File**: `Security/JwttokenGenerator.cs`

Responsible for generating and validating JWT tokens.

#### Methods

```csharp
// Generate access token (short-lived, 15 minutes)
string GenerateAccessToken(Guid userId, string email, string username, string role)

// Generate refresh token (long-lived, 7 days)
string GenerateRefreshToken(Guid userId)

// Validate token and get user ID
Guid? ValidateTokenAndGetUserId(string token)

// Extract claims from token
ClaimsPrincipal GetPrincipalFromToken(string token)
```

#### Claims Added to Access Token

**Standard Claims:**
- `NameIdentifier`: User ID (GUID)
- `Name`: Username
- `Email`: User email
- `Role`: User role (Admin, Manager, Auditor, User)
- `Jti`: JWT ID (unique identifier)

**Permission Claims** (based on role):
- `permission: view_all_users`
- `permission: edit_all_users`
- `permission: delete_users`
- `permission: manage_roles`
- `permission: view_audit_logs`
- `permission: create_users`

#### Permission Matrix (Claims)

```
Role        | Permissions
─────────────────────────────────────
Admin       | All permissions
Manager     | view_all, view_audit, create_users
Auditor     | view_all, view_audit_logs
User        | (no special permissions)
```

#### Configuration (appsettings.json)

```json
{
  "Jwt": {
    "Key": "your-256-bit-secret-key-minimum-32-characters",
    "Issuer": "YourApp",
    "Audience": "YourAppUsers",
    "ExpireMinutes": 15,
    "RefreshTokenExpireDays": 7
  }
}
```

#### Example Usage

```csharp
var tokenGenerator = new JwtTokenGenerator(configuration);

// Generate tokens
var accessToken = tokenGenerator.GenerateAccessToken(
    userId: Guid.NewGuid(),
    email: "user@example.com",
    username: "john_doe",
    role: "Manager"
);

var refreshToken = tokenGenerator.GenerateRefreshToken(userId);

// Validate token
Guid? userId = tokenGenerator.ValidateTokenAndGetUserId(accessToken);

// Get claims
var principal = tokenGenerator.GetPrincipalFromToken(refreshToken);
```

---

### 2. AuthenticationService

**File**: `Security/AuthenticationService.cs`

Handles user authentication flow and token operations.

#### Interfaces & Classes

```csharp
public interface IAuthenticationService
{
    Task<AuthenticationResult> AuthenticateAsync(Guid userId, string email, string username, string role);
    Task<string> RefreshAccessTokenAsync(string refreshToken);
    Guid? ValidateAccessToken(string token);
    Task<bool> ValidateRefreshTokenAsync(Guid userId, string refreshToken);
}

public class AuthenticationResult
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int ExpiresIn { get; set; } // In seconds
}
```

#### Login Flow

```
1. User submits username + password
2. AuthenticationController.Login() is called
3. Verify password (using PasswordHasher.VerifyPassword)
4. Call AuthenticationService.AuthenticateAsync()
5. Generate AccessToken + RefreshToken
6. Store RefreshToken in database
7. Return tokens to client
8. Log login action to AuditLog
```

#### Usage Example

```csharp
// In AuthenticationController.Login()
var authService = new AuthenticationService(
    tokenGenerator,
    refreshTokenGenerator,
    configuration);

var result = await authService.AuthenticateAsync(
    userId: user.Id,
    email: user.Email,
    username: user.Username,
    role: user.Role.Name
);

return Ok(new
{
    accessToken = result.AccessToken,
    refreshToken = result.RefreshToken,
    expiresIn = result.ExpiresIn
});
```

---

### 3. AuthorizationService

**File**: `Security/AuthorizationService.cs`

Checks user permissions and enforces access control.

#### Interfaces & Classes

```csharp
public interface IAuthorizationService
{
    Guid GetCurrentUserId();
    string GetCurrentUserRole();
    bool IsAdmin();
    bool IsManager();
    bool IsAuditor();
    bool IsAdminOrManager();
    bool CanViewUserData(Guid userId);
    bool CanEditUserData(Guid userId);
    void EnsureCanViewUserData(Guid userId);
    void EnsureCanEditUserData(Guid userId);
    bool HasPermission(string permission);
    IEnumerable<string> GetPermissions();
}

public class AuthorizationException : Exception
{
    public AuthorizationException(string message) : base(message) { }
}
```

#### Permission Rules

**View Data:**
- Can always view own data
- Admin, Manager, Auditor can view all data
- Regular users cannot view others' data

**Edit Data:**
- Can always edit own data
- Admin, Manager can edit all data
- Regular users cannot edit others' data

#### Usage in Services

```csharp
public class TransactionService
{
    private readonly IAuthorizationService _authService;
    
    public async Task<List<Transaction>> GetTransactions(Guid userId)
    {
        // This throws AuthorizationException if not allowed
        _authService.EnsureCanViewUserData(userId);
        
        return await _db.Transactions
            .Where(t => t.UserId == userId)
            .ToListAsync();
    }
    
    public async Task DeleteTransaction(Guid transactionId)
    {
        var transaction = await _db.Transactions.FindAsync(transactionId);
        var currentUserId = _authService.GetCurrentUserId();
        var isAdmin = _authService.IsAdmin();
        
        if (transaction.UserId != currentUserId && !isAdmin)
        {
            throw new AuthorizationException("Cannot delete other users' transactions");
        }
        
        _db.Transactions.Remove(transaction);
        await _db.SaveChangesAsync();
    }
}
```

#### Usage in Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly TransactionService _service;
    private readonly IAuthorizationService _authService;
    
    // View own transactions - any authenticated user
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyTransactions()
    {
        var userId = _authService.GetCurrentUserId();
        return Ok(await _service.GetTransactions(userId));
    }
    
    // View all transactions - admin/manager only
    [Authorize(Policy = "ViewAllUsers")]
    [HttpGet("all")]
    public async Task<IActionResult> GetAllTransactions()
    {
        return Ok(await _service.GetAllTransactions());
    }
}
```

---

### 4. AuditService

**File**: `Security/AuditService.cs`

Logs all user actions for compliance and debugging.

#### Interfaces & Classes

```csharp
public interface IAuditService
{
    Task LogActionAsync(Guid userId, string action, string entityName, string entityId, string details = null);
    Task LogLoginAsync(Guid userId, string username, string ipAddress = null);
    Task LogLogoutAsync(Guid userId, string username);
    Task LogFailedLoginAsync(string username, string reason, string ipAddress = null);
    Task LogAuthorizationFailureAsync(Guid userId, string action, string resource, string reason);
}
```

#### Usage Examples

```csharp
// Log a transaction creation
await _auditService.LogActionAsync(
    userId: currentUserId,
    action: "Create",
    entityName: "Transaction",
    entityId: transaction.TransactionId.ToString()
);

// Log a login
await _auditService.LogLoginAsync(
    userId: user.Id,
    username: user.Username,
    ipAddress: Request.HttpContext.Connection.RemoteIpAddress?.ToString()
);

// Log failed login
await _auditService.LogFailedLoginAsync(
    username: "john@example.com",
    reason: "Invalid password",
    ipAddress: Request.HttpContext.Connection.RemoteIpAddress?.ToString()
);

// Log authorization failure
await _auditService.LogAuthorizationFailureAsync(
    userId: currentUserId,
    action: "Delete",
    resource: "User",
    reason: "Insufficient permissions"
);
```

---

### 5. PasswordHasher

**File**: `Security/PasswordHasher.cs`

Securely hashes and verifies passwords using PBKDF2-SHA256.

#### Interfaces & Classes

```csharp
public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public class PasswordHasher : IPasswordHasher
{
    // Uses 10,000 iterations with 128-bit salt
}
```

#### Hash Format

```
Byte 0: Version (1)
Bytes 1-4: Iterations (as 32-bit integer)
Bytes 5-20: Salt (128 bits)
Bytes 21-52: Hash (256 bits)
Total: 53 bytes → Base64 encoded
```

#### Security Parameters

- **Algorithm**: PBKDF2 with SHA256
- **Iterations**: 10,000
- **Salt**: 128 bits (16 bytes) - random
- **Hash Output**: 256 bits (32 bytes)

#### Usage

```csharp
private readonly IPasswordHasher _passwordHasher;

// Register user
public async Task RegisterUserAsync(RegisterRequest request)
{
    // Validate password
    if (request.Password.Length < 8)
        throw new ValidationException("Password must be at least 8 characters");
    
    // Hash password
    var passwordHash = _passwordHasher.HashPassword(request.Password);
    
    var user = new User
    {
        Username = request.Username,
        Email = request.Email,
        Password = passwordHash,
        RoleId = 4 // User role
    };
    
    await _db.Users.AddAsync(user);
    await _db.SaveChangesAsync();
}

// Login
public async Task<AuthenticationResult> LoginAsync(LoginRequest request)
{
    var user = await _db.Users
        .Include(u => u.Role)
        .FirstOrDefaultAsync(u => u.Username == request.Username);
    
    if (user == null)
    {
        await _auditService.LogFailedLoginAsync(request.Username, "User not found");
        throw new AuthenticationException("Invalid credentials");
    }
    
    // Verify password
    if (!_passwordHasher.VerifyPassword(request.Password, user.Password))
    {
        await _auditService.LogFailedLoginAsync(request.Username, "Invalid password");
        throw new AuthenticationException("Invalid credentials");
    }
    
    // Generate tokens
    var result = await _authService.AuthenticateAsync(
        user.Id,
        user.Email,
        user.Username,
        user.Role.Name
    );
    
    await _auditService.LogLoginAsync(user.Id, user.Username);
    
    return result;
}
```

---

## Integration: Program.cs

Add services to dependency injection:

```csharp
// Authentication
builder.Services.AddScoped<JwtTokenGenerator>();
builder.Services.AddScoped<RefreshTokenGenerator>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Authorization
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddHttpContextAccessor();

// Audit & Security
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

// Authentication Middleware
var jwtSettings = builder.Configuration.GetSection("Jwt");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Key"])),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("ViewAllUsers", p => p.RequireRole("Admin", "Manager"));
    options.AddPolicy("CreateUsers", p => p.RequireRole("Admin", "Manager"));
    options.AddPolicy("ViewAuditLogs", p => p.RequireRole("Admin", "Manager", "Auditor"));
});

app.UseAuthentication();
app.UseAuthorization();
```

---

## Security Best Practices

✅ **Password Security**
- Minimum 8 characters required
- PBKDF2-SHA256 with 10,000 iterations
- 128-bit random salt per password
- Never store plain text passwords

✅ **Token Security**
- AccessToken: 15 minutes (short-lived)
- RefreshToken: 7 days (long-lived)
- JWT signed with HMAC-SHA256
- Tokens validated on every request
- Refresh tokens stored in database

✅ **Authorization**
- Role-based access control
- Permission checks at controller level
- Permission checks at service level
- Ownership verification for user data
- Audit logging of all operations

✅ **API Security**
- [Authorize] attributes on protected endpoints
- HTTPS only (enforce in production)
- Rate limiting on authentication endpoints
- CORS configured for frontend

---

## Common Patterns

### Pattern 1: Protected Endpoint

```csharp
[Authorize]
[HttpGet("me")]
public async Task<IActionResult> GetMyData()
{
    var userId = _authService.GetCurrentUserId();
    return Ok(await _service.GetMyData(userId));
}
```

### Pattern 2: Role-Based Access

```csharp
[Authorize(Roles = "Admin")]
[HttpDelete("users/{userId}")]
public async Task<IActionResult> DeleteUser(Guid userId)
{
    await _service.DeleteUser(userId);
    return Ok();
}
```

### Pattern 3: Policy-Based Access

```csharp
[Authorize(Policy = "ViewAllUsers")]
[HttpGet("all")]
public async Task<IActionResult> GetAllUsers()
{
    return Ok(await _service.GetAllUsers());
}
```

### Pattern 4: Service-Level Authorization

```csharp
public async Task UpdateUser(Guid userId, UpdateUserRequest request)
{
    _authService.EnsureCanEditUserData(userId);
    // Proceed with update
}
```

---

## Testing Checklist

- [ ] JWT token generation with all claims
- [ ] JWT token validation and expiration
- [ ] Password hashing and verification
- [ ] Login with correct credentials
- [ ] Login fails with wrong password
- [ ] Authorization checks for different roles
- [ ] Audit logging of all actions
- [ ] Refresh token generation and validation
- [ ] Token expiration handling
- [ ] HTTPS enforcement in production

---

**Status**: ✅ Ready for use  
**Framework**: .NET 8  
**Authentication**: JWT Bearer  
**Authorization**: Role & Policy-Based  
**Password**: PBKDF2-SHA256 (10k iterations)
