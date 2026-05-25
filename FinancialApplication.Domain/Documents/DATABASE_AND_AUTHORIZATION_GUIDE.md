# Financial Application - Database & Authorization Guide

## 1. Database Architecture Overview

### Core Tables Structure

```
┌─────────────────────────────────────────────────────────┐
│                    ROLES (Reference)                     │
├─────────────────────────────────────────────────────────┤
│ Id (PK, int, auto-increment)                            │
│ Name (string, max 50) - Admin, Manager, Auditor, User  │
│ IsActive (bool)                                         │
└────────────────┬────────────────────────────────────────┘
                 │ 1:N
                 │
┌────────────────▼────────────────────────────────────────┐
│                    USERS (Main Entity)                   │
├─────────────────────────────────────────────────────────┤
│ Id (PK, GUID)                                           │
│ Username (string, max 50) - Unique identifier           │
│ Password (string, max 255) - Hashed/encrypted           │
│ Email (string, max 100) - Email address                 │
│ RoleId (FK, int) → References Roles.Id                  │
│ IsActive (bool)                                         │
│ CreatedAt (DateTime) - Account creation timestamp       │
│ UpdatedAt (DateTime) - Last update timestamp            │
└────────────────┬────────────────────────────────────────┘
                 │ 1:N (owns multiple records)
     ┌───────────┼───────────┬──────────────┐
     │           │           │              │
┌────▼─┐   ┌────▼─┐   ┌────▼─┐      ┌────▼──┐
│Trans-│   │Invest│   │Goals │      │Audit  │
│actions│  │ments │   │      │      │ Logs  │
└──────┘   └──────┘   └──────┘      └───────┘
```

### Related Tables

#### TRANSACTIONS
- **TransactionId** (PK, GUID)
- **UserId** (FK) → Users.Id
- **Amount** (decimal, 2 places)
- **Category** (string, max 100) - e.g., "Groceries", "Salary", "Utilities"
- **Description** (string, max 500)
- **TransactionDate** (DateTime)
- **TransactionType** (enum) - Income or Expense
- **CreatedAt** (DateTime)

#### INVESTMENTS
- **InvestmentId** (PK, GUID)
- **UserId** (FK) → Users.Id
- **Amount** (decimal, 2 places)
- **InvestmentType** (enum) - Stock, Bond, MutualFund, RealEstate, Cryptocurrency
- **StartDate** (DateTime)
- **EndDate** (DateTime)
- **Status** (string, max 50) - Active, Completed, Cancelled
- **CreatedAt** (DateTime)
- **UpdatedAt** (DateTime)

#### GOALS
- **GoalId** (PK, GUID)
- **UserId** (FK) → Users.Id
- **Title** (string, max 255)
- **TargetAmount** (decimal, 2 places)
- **CurrentAmount** (decimal, 2 places)
- **Deadline** (DateTime)
- **Status** (enum) - NotStarted, InProgress, Completed, Failed
- **CreatedAt** (DateTime)
- **UpdatedAt** (DateTime)

#### AUDIT_LOGS
- **AuditLogId** (PK, GUID)
- **UserId** (FK) → Users.Id - Who performed the action
- **Action** (string, max 50) - Create, Update, Delete
- **EntityName** (string, max 100) - Transaction, Investment, Goal, User
- **EntityId** (string, max 255) - ID of affected entity
- **Timestamp** (DateTime)

#### REFRESH_TOKENS
- **RefreshTokenId** (PK, GUID)
- **UserId** (FK) → Users.Id
- **Token** (string, max 500) - JWT refresh token
- **ExpiryDate** (DateTime)
- **CreatedDate** (DateTime)

---

## 2. Role-Based Access Control (RBAC) System - No Permission Table Required

### Default Roles

| Role | Description |
|------|-------------|
| **Admin** | System administrator - Full access to all features |
| **Manager** | Manager/Supervisor - Can view/manage all users' data |
| **Auditor** | Auditor - Read-only access to all data |
| **User** | Regular user - Access to own data only |

### Permission Management Strategy (No Database Table Needed)

Permissions are managed through **claims and policies in code**, not in the database.

#### Method 1: Role-Based Authorization (Simplest)

Define permissions based on roles in the ASP.NET Core configuration:

```csharp
// In Program.cs or Startup.cs
services.AddAuthorization(options =>
{
    // Admin can do everything
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    // Admin and Manager can view all users
    options.AddPolicy("ViewAllUsers", policy =>
        policy.RequireRole("Admin", "Manager"));

    // Only users can edit their own data
    options.AddPolicy("EditOwnData", policy =>
        policy.RequireRole("User", "Manager", "Admin"));

    // Only Admin and Manager can create users
    options.AddPolicy("CreateUsers", policy =>
        policy.RequireRole("Admin", "Manager"));

    // Only Admin, Manager, and Auditor can view audit logs
    options.AddPolicy("ViewAuditLogs", policy =>
        policy.RequireRole("Admin", "Manager", "Auditor"));

    // Only Admin can delete users
    options.AddPolicy("DeleteUsers", policy =>
        policy.RequireRole("Admin"));
});
```

#### Method 2: Claims-Based Authorization (More Flexible)

Embed permissions as claims in the JWT token when user logs in:

```csharp
// In AuthenticationService.cs - when generating JWT token
private List<Claim> GetUserClaims(User user)
{
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role.Name)
    };

    // Add permissions based on role
    switch (user.Role.Name)
    {
        case "Admin":
            claims.AddRange(new[]
            {
                new Claim("permission", "view_all_users"),
                new Claim("permission", "edit_all_users"),
                new Claim("permission", "delete_users"),
                new Claim("permission", "manage_roles"),
                new Claim("permission", "view_audit_logs"),
                new Claim("permission", "create_users")
            });
            break;

        case "Manager":
            claims.AddRange(new[]
            {
                new Claim("permission", "view_all_users"),
                new Claim("permission", "view_audit_logs"),
                new Claim("permission", "create_users")
            });
            break;

        case "Auditor":
            claims.AddRange(new[]
            {
                new Claim("permission", "view_all_users"),
                new Claim("permission", "view_audit_logs")
            });
            break;

        case "User":
            // Regular users have no special permissions beyond their role
            break;
    }

    return claims;
}
```

Then in `Program.cs`:

```csharp
services.AddAuthorization(options =>
{
    options.AddPolicy("ViewAllUsers", policy =>
        policy.RequireClaim("permission", "view_all_users"));

    options.AddPolicy("EditAllUsers", policy =>
        policy.RequireClaim("permission", "edit_all_users"));

    options.AddPolicy("DeleteUsers", policy =>
        policy.RequireClaim("permission", "delete_users"));

    options.AddPolicy("ViewAuditLogs", policy =>
        policy.RequireClaim("permission", "view_audit_logs"));

    options.AddPolicy("CreateUsers", policy =>
        policy.RequireClaim("permission", "create_users"));
});
```

#### Permission Matrix

```
Feature                 | Admin | Manager | Auditor | User |
────────────────────────┼───────┼─────────┼─────────┼──────┤
View Own Transactions   │  ✓    │    ✓    │    ✓    │  ✓   │
Edit Own Transactions   │  ✓    │    ✓    │         │  ✓   │
View All Transactions   │  ✓    │    ✓    │    ✓    │      │
Delete Own Transactions │  ✓    │    ✓    │         │  ✓   │
View All Users          │  ✓    │    ✓    │    ✓    │      │
Edit Other Users        │  ✓    │         │         │      │
Delete Users            │  ✓    │         │         │      │
View Audit Logs         │  ✓    │    ✓    │    ✓    │      │
Manage Roles            │  ✓    │         │         │      │
Create Users            │  ✓    │    ✓    │         │      │
```

---

## 3. Authentication Flow

### Login Process (JWT-based)

```
┌──────────────┐
│   Client     │
└──────┬───────┘
       │ 1. POST /auth/login
       │    { username, password }
       ▼
┌──────────────────────────────────────┐
│   Authentication Service             │
├──────────────────────────────────────┤
│ 1. Validate credentials              │
│ 2. Hash password & compare           │
│ 3. Fetch User + Role from DB         │
│ 4. Generate JWT Access Token         │
│ 5. Generate Refresh Token            │
│ 6. Store Refresh Token in DB         │
└──────┬───────────────────────────────┘
       │ 2. Return { accessToken, refreshToken }
       ▼
┌──────────────┐
│   Client     │ ◄─ Stores tokens locally
└──────┬───────┘
       │ 3. Include accessToken in Authorization header
       │    Authorization: Bearer <accessToken>
       ▼
┌──────────────────────────────────────┐
│   Protected API Endpoint             │
├──────────────────────────────────────┤
│ 1. Validate JWT signature            │
│ 2. Check token expiration            │
│ 3. Extract claims (UserId, Role)     │
│ 4. Check authorization policy        │
│ 5. Process request                   │
└──────────────────────────────────────┘
```

---

## 4. Authorization Implementation Patterns - No Permission Table

### Pattern 1: Using Policies in Controllers

```csharp
// Get own transactions (any authenticated user)
[Authorize]
[HttpGet("me/transactions")]
public async Task<IActionResult> GetMyTransactions()
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var transactions = await _db.Transactions
        .Where(t => t.UserId == Guid.Parse(userId))
        .ToListAsync();
    return Ok(transactions);
}

// View all users' transactions (Admin/Manager only)
[Authorize(Policy = "ViewAllUsers")]
[HttpGet("transactions")]
public async Task<IActionResult> GetAllTransactions()
{
    var allTransactions = await _db.Transactions
        .Include(t => t.User)
        .ToListAsync();
    return Ok(allTransactions);
}

// Delete a transaction (owner or Admin)
[Authorize]
[HttpDelete("transactions/{transactionId}")]
public async Task<IActionResult> DeleteTransaction(Guid transactionId)
{
    var transaction = await _db.Transactions.FindAsync(transactionId);
    var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var isAdmin = User.IsInRole("Admin");

    // Allow if user owns it or is Admin
    if (transaction.UserId != Guid.Parse(currentUserId) && !isAdmin)
    {
        return Forbid();
    }

    _db.Transactions.Remove(transaction);
    await _db.SaveChangesAsync();
    return Ok();
}

// Create user (Admin/Manager only)
[Authorize(Policy = "CreateUsers")]
[HttpPost("users")]
public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
{
    // Create user logic
    return Ok();
}

// View audit logs (Admin/Manager/Auditor)
[Authorize(Policy = "ViewAuditLogs")]
[HttpGet("audit-logs")]
public async Task<IActionResult> GetAuditLogs()
{
    var logs = await _db.AuditLogs.ToListAsync();
    return Ok(logs);
}

// Delete user (Admin only)
[Authorize(Policy = "AdminOnly")]
[HttpDelete("users/{userId}")]
public async Task<IActionResult> DeleteUser(Guid userId)
{
    var user = await _db.Users.FindAsync(userId);
    _db.Users.Remove(user);
    await _db.SaveChangesAsync();
    return Ok();
}
```

### Pattern 2: Custom Authorization Handler (Advanced)

For complex permission logic:

```csharp
// Create custom requirement
public class OwnsResourceRequirement : IAuthorizationRequirement { }

// Create custom handler
public class OwnsResourceHandler : AuthorizationHandler<OwnsResourceRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationDbContext _db;

    public OwnsResourceHandler(IHttpContextAccessor httpContextAccessor, ApplicationDbContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OwnsResourceRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Get the resource ID from route
        if (!httpContext.GetRouteValue("transactionId", out var transactionIdStr))
        {
            return;
        }

        var transaction = await _db.Transactions.FindAsync(Guid.Parse(transactionIdStr.ToString()));

        if (transaction?.UserId.ToString() == userId)
        {
            context.Succeed(requirement);
        }
    }
}

// Register in Program.cs
services.AddAuthorizationBuilder()
    .AddPolicy("OwnResource", policy =>
        policy.Requirements.Add(new OwnsResourceRequirement()));

services.AddScoped<IAuthorizationHandler, OwnsResourceHandler>();

// Use in controller
[Authorize(Policy = "OwnResource")]
[HttpPut("transactions/{transactionId}")]
public async Task<IActionResult> UpdateTransaction(Guid transactionId, [FromBody] UpdateTransactionRequest request)
{
    // Handler already verified user owns the transaction
    var transaction = await _db.Transactions.FindAsync(transactionId);
    // Update logic...
    return Ok();
}
```

### Pattern 3: Role Check in Service Layer

```csharp
public class TransactionService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TransactionService(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    private string GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private bool IsAdmin()
    {
        return _httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;
    }

    private bool IsManagerOrAdmin()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.IsInRole("Manager") == true || user?.IsInRole("Admin") == true;
    }

    public async Task<List<Transaction>> GetTransactions(Guid userId)
    {
        var currentUserId = GetCurrentUserId();

        // Users can only see their own, Manager/Admin can see all
        if (!IsManagerOrAdmin() && userId.ToString() != currentUserId)
        {
            throw new UnauthorizedException("You don't have permission to view this data");
        }

        return await _db.Transactions
            .Where(t => t.UserId == userId)
            .ToListAsync();
    }

    public async Task DeleteTransaction(Guid transactionId)
    {
        var transaction = await _db.Transactions.FindAsync(transactionId);
        var currentUserId = GetCurrentUserId();

        // Allow if user owns it or is Admin
        if (transaction.UserId.ToString() != currentUserId && !IsAdmin())
        {
            throw new UnauthorizedException("You don't have permission to delete this transaction");
        }

        _db.Transactions.Remove(transaction);
        await _db.SaveChangesAsync();
    }
}
```

---

## 5. Data Isolation & Authorization in Code

### User Data Isolation

```csharp
public class AuthorizationService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthorizationService(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Get current user ID from JWT claims
    /// </summary>
    public Guid GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.Parse(userIdClaim ?? throw new UnauthorizedAccessException());
    }

    /// <summary>
    /// Get current user's role
    /// </summary>
    public string GetCurrentUserRole()
    {
        return _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.Role)?.Value ?? "User";
    }

    /// <summary>
    /// Check if current user is Admin
    /// </summary>
    public bool IsAdmin()
    {
        return GetCurrentUserRole() == "Admin";
    }

    /// <summary>
    /// Check if current user is Admin or Manager
    /// </summary>
    public bool IsAdminOrManager()
    {
        var role = GetCurrentUserRole();
        return role == "Admin" || role == "Manager";
    }

    /// <summary>
    /// Check if user can view specific user's data
    /// </summary>
    public bool CanViewUserData(Guid userId)
    {
        var currentUserId = GetCurrentUserId();

        // Can view own data or all data if Admin/Manager/Auditor
        return currentUserId == userId || 
               new[] { "Admin", "Manager", "Auditor" }.Contains(GetCurrentUserRole());
    }

    /// <summary>
    /// Check if user can edit specific user's data
    /// </summary>
    public bool CanEditUserData(Guid userId)
    {
        var currentUserId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        // Can edit own data or all data if Admin/Manager
        return currentUserId == userId || role == "Admin" || role == "Manager";
    }

    /// <summary>
    /// Check if user can delete something
    /// </summary>
    public bool CanDelete()
    {
        var role = GetCurrentUserRole();
        return role == "Admin" || role == "Manager" || role == "User";
    }

    /// <summary>
    /// Ensure user has permission or throw exception
    /// </summary>
    public void EnsureCanViewUserData(Guid userId)
    {
        if (!CanViewUserData(userId))
        {
            throw new ForbiddenAccessException($"You don't have permission to view data for user {userId}");
        }
    }

    public void EnsureCanEditUserData(Guid userId)
    {
        if (!CanEditUserData(userId))
        {
            throw new ForbiddenAccessException($"You don't have permission to edit data for user {userId}");
        }
    }
}
```

### Usage in Services

```csharp
public class TransactionService
{
    private readonly ApplicationDbContext _db;
    private readonly AuthorizationService _authService;
    private readonly AuditService _auditService;

    public async Task<List<Transaction>> GetTransactions(Guid userId)
    {
        _authService.EnsureCanViewUserData(userId);

        return await _db.Transactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<Transaction> CreateTransaction(Guid userId, CreateTransactionRequest request)
    {
        _authService.EnsureCanEditUserData(userId);

        var transaction = new Transaction
        {
            UserId = userId,
            Amount = request.Amount,
            Category = request.Category,
            Description = request.Description,
            TransactionDate = request.TransactionDate,
            TransactionType = request.TransactionType,
            CreatedAt = DateTime.UtcNow
        };

        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync();

        // Log audit trail
        await _auditService.LogAction("Create", "Transaction", transaction.TransactionId.ToString());

        return transaction;
    }

    public async Task DeleteTransaction(Guid transactionId)
    {
        var transaction = await _db.Transactions.FindAsync(transactionId);
        var currentUserId = _authService.GetCurrentUserId();
        var isAdmin = _authService.IsAdmin();

        // Allow if user owns it or is Admin
        if (transaction.UserId != currentUserId && !isAdmin)
        {
            throw new ForbiddenAccessException("You don't have permission to delete this transaction");
        }

        _db.Transactions.Remove(transaction);
        await _db.SaveChangesAsync();

        // Log audit trail
        await _auditService.LogAction("Delete", "Transaction", transactionId.ToString());
    }
}
```

### Audit Trail Logging

```csharp
public class AuditService
{
    private readonly ApplicationDbContext _db;
    private readonly AuthorizationService _authService;

    public async Task LogAction(string action, string entityName, string entityId, string details = null)
    {
        var auditLog = new AuditLog
        {
            UserId = _authService.GetCurrentUserId(),
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Timestamp = DateTime.UtcNow
        };

        _db.AuditLogs.Add(auditLog);
        await _db.SaveChangesAsync();
    }
}
```

---

## 6. Token Refresh Flow

```
┌──────────────┐
│   Client     │ Has expired accessToken + valid refreshToken
└──────┬───────┘
       │ POST /auth/refresh
       │ { refreshToken }
       ▼
┌──────────────────────────────────────┐
│   Token Service                      │
├──────────────────────────────────────┤
│ 1. Find RefreshToken in DB           │
│ 2. Verify token validity             │
│ 3. Check expiration date             │
│ 4. Generate new accessToken          │
│ 5. (Optional) Generate new refreshToken
│ 6. Update/invalidate old token       │
└──────┬───────────────────────────────┘
       │ Return new accessToken
       ▼
┌──────────────┐
│   Client     │ ◄─ Uses new accessToken for subsequent requests
└──────────────┘
```

---

## 7. Security Best Practices

### Password Security
- ✅ Hash passwords using **bcrypt** or **PBKDF2** (never store plain text)
- ✅ Use **salt** to prevent rainbow table attacks
- ✅ Enforce minimum password requirements

### Token Security
- ✅ Set short expiration times for access tokens (15 min - 1 hour)
- ✅ Set longer expiration for refresh tokens (7 days - 30 days)
- ✅ Store refresh tokens securely in database
- ✅ Implement token revocation on logout

### API Security
- ✅ Always validate JWT signatures
- ✅ Check token expiration before processing
- ✅ Implement rate limiting on auth endpoints
- ✅ Log all authentication attempts (especially failures)
- ✅ Use HTTPS only for all communications

### Database Security
- ✅ Enforce foreign key constraints
- ✅ Use parameterized queries to prevent SQL injection
- ✅ Implement row-level security for multi-tenant scenarios
- ✅ Encrypt sensitive fields (SSN, bank accounts, etc.)

---

## 8. Complete Setup Guide - Program.cs Configuration

### Authentication & Authorization Setup

```csharp
// In Program.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplicationBuilder.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Configuration
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Authorization Policies (No permission table - all defined in code)
builder.Services.AddAuthorization(options =>
{
    // Admin policies
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    // Manager policies
    options.AddPolicy("ViewAllUsers", policy =>
        policy.RequireRole("Admin", "Manager"));

    options.AddPolicy("CreateUsers", policy =>
        policy.RequireRole("Admin", "Manager"));

    // Auditor policies
    options.AddPolicy("ViewAuditLogs", policy =>
        policy.RequireRole("Admin", "Manager", "Auditor"));

    // Combined policies
    options.AddPolicy("ManageTransactions", policy =>
        policy.RequireRole("Admin", "Manager"));

    options.AddPolicy("ManageInvestments", policy =>
        policy.RequireRole("Admin", "Manager"));

    options.AddPolicy("ManageGoals", policy =>
        policy.RequireRole("Admin", "Manager"));
});

// Add custom services
builder.Services.AddScoped<AuthorizationService>();
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<InvestmentService>();
builder.Services.AddScoped<GoalService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();

// Important: Order matters - Authentication before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
```

### appsettings.json Configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=FinancialAppDb;Trusted_Connection=true;"
  },
  "JwtSettings": {
    "SecretKey": "your-256-bit-secret-key-minimum-32-characters-required",
    "Issuer": "FinancialApp",
    "Audience": "FinancialAppUsers",
    "ExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

---

## 9. Quick Implementation Checklist

### Database Setup
- [ ] Create Roles table with default roles (Admin, Manager, Auditor, User)
- [ ] Create Users table with RoleId foreign key
- [ ] Create Transaction, Investment, Goal tables
- [ ] Create AuditLog table
- [ ] Create RefreshToken table
- [ ] Add indexes for performance

### Authentication Setup
- [ ] Configure JWT settings in appsettings.json
- [ ] Add authentication middleware in Program.cs
- [ ] Create AuthenticationService with JWT generation
- [ ] Create login endpoint (POST /auth/login)
- [ ] Create refresh token endpoint (POST /auth/refresh)
- [ ] Implement password hashing (bcrypt)

### Authorization Setup (No Permission Table Needed)
- [ ] Define authorization policies in Program.cs
- [ ] Create AuthorizationService class
- [ ] Add role-based checks in service layer
- [ ] Implement resource-based access checks
- [ ] Create custom authorization handlers if needed

### Business Logic & Audit
- [ ] Create TransactionService with authorization checks
- [ ] Create InvestmentService with authorization checks
- [ ] Create GoalService with authorization checks
- [ ] Create AuditService for logging
- [ ] Add audit logging to all Create/Update/Delete operations

### API Controllers
- [ ] Create AuthenticationController
- [ ] Create TransactionController with authorization
- [ ] Create InvestmentController with authorization
- [ ] Create GoalController with authorization
- [ ] Create AuditLogController (Admin/Manager/Auditor only)
- [ ] Create UserController (Admin only)

### Testing & Security
- [ ] Test login and token generation
- [ ] Test token refresh flow
- [ ] Test authorization policies with different roles
- [ ] Test data isolation (users can't access others' data)
- [ ] Enable HTTPS in production
- [ ] Implement rate limiting on auth endpoints
- [ ] Add proper error handling and logging

---

## 10. Example Authorization Scenarios

### Scenario 1: User viewing their transactions
```
User (role="User") → GET /api/transactions/me
✓ Allowed (accessing own data)
```

### Scenario 2: Admin viewing all user transactions
```
Admin (role="Admin") → GET /api/users/{userId}/transactions
✓ Allowed (admin has full access)
```

### Scenario 3: User trying to view another user's data
```
User (role="User") → GET /api/users/{otherUserId}/transactions
✗ Forbidden (unauthorized access)
→ Returns 403 Forbidden
```

### Scenario 4: Auditor viewing audit logs
```
Auditor (role="Auditor") → GET /api/audit-logs
✓ Allowed (read-only access)
```

### Scenario 5: User trying to delete another user
```
User (role="User") → DELETE /api/users/{userId}
✗ Forbidden (insufficient permissions)
→ Returns 403 Forbidden
```

---

## Summary

### Key Points - No Permission Table Needed ✅

**Permissions are managed entirely through code**, not in the database:

1. **Define roles in database** - Only Roles table with Admin, Manager, Auditor, User
2. **Define permissions in code** - Authorization policies in `Program.cs`
3. **Check permissions in code** - AuthorizationService class with helper methods
4. **Enforce in controllers** - `[Authorize]` attributes and policy checks
5. **Log all actions** - AuditService for compliance and tracking

### Why This Approach?

✅ **No permission table maintenance** - Reduces database complexity
✅ **Permissions change rarely** - Code deployment handles updates
✅ **Cleaner architecture** - Authorization logic stays in application code
✅ **Better performance** - No extra database queries for permissions
✅ **Easier auditing** - Single source of truth in code

### Architecture Summary

```
┌─────────────────────────────────────────────────────┐
│                   appsettings.json                   │
│              (JWT configuration)                     │
└────────────────┬────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────┐
│                   Program.cs                         │
│         (Authentication & Authorization)             │
│         (All policies defined here)                  │
└────────────────┬────────────────────────────────────┘
                 │
      ┌──────────┴───────────┐
      │                      │
      ▼                      ▼
┌──────────────┐      ┌──────────────────────┐
│  Controllers │      │ Service Layer        │
│ [Authorize]  │      │ (AuthorizationService)
│  attributes  │      │ (Permission checks)  │
└──────────────┘      └──────────────────────┘
      │                      │
      └──────────┬───────────┘
                 │
                 ▼
      ┌──────────────────────┐
      │  Database (Roles)    │
      │ Only 4 default roles │
      │ No permission table  │
      └──────────────────────┘
```

This system is **simple, scalable, and maintainable** - permissions change in code deployments, not database migrations.

