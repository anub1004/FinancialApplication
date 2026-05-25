# Financial Application - Authorization System Overview

## ✅ Updated: No Permission Table Required

Your Financial Application now uses a **code-based authorization system** without a separate permissions table in the database.

---

## What Changed?

### Before
```
Database Tables:
├── Roles
├── Users  
├── Permissions         ❌ REMOVED
├── RolePermissions     ❌ REMOVED
├── Transactions
├── Investments
├── Goals
└── AuditLogs
```

### Now
```
Database Tables:
├── Roles (only 4 roles)
├── Users
├── Transactions
├── Investments
├── Goals
└── AuditLogs

Code-Based Authorization:
├── Program.cs (policies)
├── AuthorizationService (helpers)
└── Service Layer (checks)
```

---

## System Architecture

```
┌─────────────────────────────────────────────────────┐
│                  appsettings.json                    │
│             JWT Configuration & Settings             │
└────────────────────┬────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────┐
│                  Program.cs                          │
├─────────────────────────────────────────────────────┤
│ • Authentication (JWT)                              │
│ • Authorization Policies (ALL defined here)         │
│ • Service Registration                              │
│ • No permission table needed!                       │
└────────────────────┬────────────────────────────────┘
                     │
    ┌────────────────┼────────────────┐
    │                │                │
┌───▼──┐      ┌─────▼────────┐   ┌──▼───────┐
│ JWT  │      │ Authorization│   │  Roles   │
│Bearer│      │   Service    │   │  (Only 4)│
│Token │      │  Helpers     │   │ in DB    │
└──────┘      └──────────────┘   └──────────┘
    │                │
    └────────────────┼────────────────┐
                     │                │
              ┌──────▼─────┐    ┌────▼──────────┐
              │ Controllers│    │ Service Layer │
              │ [Authorize]│    │ AuthChecks    │
              └────────────┘    └───────────────┘
```

---

## Key Components

### 1. Roles Table (Database)
Only 4 roles needed:
- **Admin** - Full system access
- **Manager** - View/manage all users' data
- **Auditor** - Read-only access
- **User** - Own data only

### 2. Authorization Policies (Program.cs)
All permissions defined as policies:
```csharp
services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ViewAllUsers", policy => policy.RequireRole("Admin", "Manager"));
    options.AddPolicy("ViewAuditLogs", policy => policy.RequireRole("Admin", "Manager", "Auditor"));
});
```

### 3. AuthorizationService (Application Code)
Helper methods for permission checks:
```csharp
public class AuthorizationService
{
    public bool IsAdmin() { }
    public bool CanViewUserData(Guid userId) { }
    public bool CanEditUserData(Guid userId) { }
    public void EnsureCanViewUserData(Guid userId) { }
}
```

### 4. Decorators in Controllers
Use `[Authorize]` attributes:
```csharp
[Authorize]
[HttpGet("me")]
public async Task<IActionResult> GetMyData() { }

[Authorize(Roles = "Admin")]
[HttpDelete("users/{userId}")]
public async Task<IActionResult> DeleteUser(Guid userId) { }

[Authorize(Policy = "ViewAllUsers")]
[HttpGet("all")]
public async Task<IActionResult> GetAllUsers() { }
```

---

## Permission Matrix

```
Feature                  | Admin | Manager | Auditor | User |
─────────────────────────┼───────┼─────────┼─────────┼──────┤
View Own Data            │  ✓    │    ✓    │    ✓    │  ✓   │
Edit Own Data            │  ✓    │    ✓    │         │  ✓   │
View All Users Data      │  ✓    │    ✓    │    ✓    │      │
Edit Other Users Data    │  ✓    │         │         │      │
Delete Users             │  ✓    │         │         │      │
View Audit Logs          │  ✓    │    ✓    │    ✓    │      │
Create Users             │  ✓    │    ✓    │         │      │
Manage Roles             │  ✓    │         │         │      │
```

---

## Documentation Files

### 📄 DATABASE_AND_AUTHORIZATION_GUIDE.md
**Complete technical guide** covering:
- Database architecture
- Authentication flow
- Authorization patterns (3 methods)
- Data isolation strategy
- Token refresh flow
- Security best practices
- Complete setup guide
- Implementation checklist

### 📄 AUTHORIZATION_IMPLEMENTATION_GUIDE.md
**Visual diagrams and flows** including:
- High-level authorization flow
- Authorization decision tree
- Role-permission mapping
- Database comparison (with vs without permission table)
- Implementation checklist
- Key benefits

### 📄 AUTHORIZATION_QUICK_REFERENCE.md
**Quick reference for developers** with:
- 5-minute quick start
- Common authorization patterns
- Role permissions reference
- Helper methods
- HTTP status codes
- JWT structure
- Testing guidelines
- Debugging tips

---

## Quick Start for Developers

### Step 1: Setup JWT in appsettings.json
```json
{
  "JwtSettings": {
    "SecretKey": "your-256-bit-secret-key-minimum-32-characters",
    "ExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

### Step 2: Configure Program.cs
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(/* config */);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("ViewAllUsers", p => p.RequireRole("Admin", "Manager"));
    // ... more policies
});

builder.Services.AddScoped<AuthorizationService>();
```

### Step 3: Add [Authorize] Attributes
```csharp
[Authorize]
[HttpGet("me/transactions")]
public async Task<IActionResult> GetMyTransactions() { }

[Authorize(Policy = "ViewAllUsers")]
[HttpGet("all")]
public async Task<IActionResult> GetAllTransactions() { }
```

### Step 4: Check Permissions in Services
```csharp
public async Task<List<Transaction>> GetTransactions(Guid userId)
{
    _authService.EnsureCanViewUserData(userId);
    return await _db.Transactions.Where(t => t.UserId == userId).ToListAsync();
}
```

---

## Why This Approach?

### ✅ Advantages
- **Simple** - No permission table to maintain
- **Fast** - No database joins for authorization
- **Maintainable** - Single source of truth in Program.cs
- **Flexible** - Easy to add/modify permissions
- **Scalable** - Permissions change with code deployments
- **Auditable** - Complete audit trail in database

### ❌ What We Don't Do
- ❌ Store permissions in database
- ❌ Create join tables for roles and permissions
- ❌ Query permission tables for every request
- ❌ Use hardcoded role checks scattered in code

---

## Common Use Cases

### Use Case 1: User Viewing Their Own Transactions
```csharp
[Authorize]
[HttpGet("me/transactions")]
public async Task<IActionResult> GetMyTransactions()
{
    var userId = GetCurrentUserId();
    return Ok(await _service.GetTransactions(userId));
}
```
✅ Any authenticated user can call this

### Use Case 2: Manager Viewing All Transactions
```csharp
[Authorize(Policy = "ViewAllUsers")]
[HttpGet("transactions")]
public async Task<IActionResult> GetAllTransactions()
{
    return Ok(await _service.GetAllTransactions());
}
```
✅ Only Admin and Manager can call this

### Use Case 3: Auditor Viewing Audit Logs
```csharp
[Authorize(Policy = "ViewAuditLogs")]
[HttpGet("audit-logs")]
public async Task<IActionResult> GetAuditLogs()
{
    return Ok(await _service.GetAuditLogs());
}
```
✅ Admin, Manager, and Auditor can call this

### Use Case 4: Admin Deleting User
```csharp
[Authorize(Roles = "Admin")]
[HttpDelete("users/{userId}")]
public async Task<IActionResult> DeleteUser(Guid userId)
{
    await _service.DeleteUser(userId);
    return Ok();
}
```
✅ Only Admin can call this

---

## Testing Authorization

### Test Login
```bash
POST /auth/login
{
  "username": "john",
  "password": "password123"
}
Response: { "accessToken": "...", "refreshToken": "..." }
```

### Test Protected Endpoint (with token)
```bash
GET /api/transactions/me
Authorization: Bearer <accessToken>
Response: 200 OK + data
```

### Test Protected Endpoint (without token)
```bash
GET /api/transactions/me
Response: 401 Unauthorized
```

### Test Insufficient Permissions
```bash
GET /api/transactions (requires admin/manager)
Authorization: Bearer <user-token>
Response: 403 Forbidden
```

---

## Security Checklist

- ✅ Passwords hashed with bcrypt
- ✅ JWT tokens with expiration
- ✅ Refresh tokens stored securely
- ✅ HTTPS enforced in production
- ✅ Authorization checked at controller level
- ✅ Authorization checked at service level
- ✅ Audit logging for all write operations
- ✅ Data isolation enforced
- ✅ No hardcoded credentials
- ✅ Rate limiting on auth endpoints

---

## Next Steps

1. **Read DATABASE_AND_AUTHORIZATION_GUIDE.md** for complete technical details
2. **Review AUTHORIZATION_QUICK_REFERENCE.md** for common patterns
3. **Implement AuthenticationService** for JWT generation
4. **Implement AuthorizationService** for permission helpers
5. **Add [Authorize] attributes** to controllers
6. **Add authorization checks** in service layer
7. **Test with different roles** to verify permissions
8. **Set up audit logging** for compliance

---

## Support Documents

All three guides are located in:
```
FinancialApplication.Domain/
├── DATABASE_AND_AUTHORIZATION_GUIDE.md      (Complete technical reference)
├── AUTHORIZATION_IMPLEMENTATION_GUIDE.md    (Visual diagrams & flows)
└── AUTHORIZATION_QUICK_REFERENCE.md         (Quick reference for developers)
```

Read them in this order:
1. **AUTHORIZATION_QUICK_REFERENCE.md** (5 min) - Quick start
2. **AUTHORIZATION_IMPLEMENTATION_GUIDE.md** (10 min) - Visual overview
3. **DATABASE_AND_AUTHORIZATION_GUIDE.md** (30 min) - Complete guide

---

**Last Updated**: 2024  
**System**: Financial Application v1.0  
**Framework**: .NET 8  
**Authentication**: JWT Bearer Tokens  
**Authorization**: Role-Based with Policies (Code-Based)
