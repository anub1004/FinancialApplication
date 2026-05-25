# Authorization System - Visual Reference

## Complete System Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    USER APPLICATION                              │
├─────────────────────────────────────────────────────────────────┤
│ 1. User enters credentials                                       │
│ 2. Client sends POST /auth/login                                │
└────────────┬────────────────────────────────────────────────────┘
             │ { username, password }
             ▼
┌─────────────────────────────────────────────────────────────────┐
│                   AUTH CONTROLLER                                │
├─────────────────────────────────────────────────────────────────┤
│ [HttpPost("login")]                                             │
│ public async Task<IActionResult> Login(LoginRequest request)   │
└────────────┬────────────────────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────────────────────┐
│              AUTHENTICATION SERVICE                              │
├─────────────────────────────────────────────────────────────────┤
│ 1. Find user by username in DB                                  │
│ 2. Verify password (bcrypt.Compare)                            │
│ 3. Load User + Role from database                              │
│ 4. Create JWT claims:                                           │
│    - sub: UserId                                                │
│    - role: RoleName                                             │
│    - email: Email                                               │
│    - username: Username                                         │
│ 5. Generate AccessToken (exp: 15 min)                          │
│ 6. Generate RefreshToken (exp: 7 days)                         │
│ 7. Save RefreshToken to DB                                      │
└────────────┬────────────────────────────────────────────────────┘
             │ { accessToken, refreshToken }
             ▼
┌─────────────────────────────────────────────────────────────────┐
│                   CLIENT APPLICATION                             │
├─────────────────────────────────────────────────────────────────┤
│ Store tokens in localStorage/sessionStorage                     │
│ accessToken → Use for API calls (short-lived)                  │
│ refreshToken → Keep safe (long-lived)                          │
└────────────┬────────────────────────────────────────────────────┘
             │
             ├─ NextRequest: GET /api/transactions/me
             │  Header: Authorization: Bearer <accessToken>
             │
             ▼
┌─────────────────────────────────────────────────────────────────┐
│            JWT VALIDATION MIDDLEWARE                             │
├─────────────────────────────────────────────────────────────────┤
│ 1. Extract token from Authorization header                      │
│ 2. Verify signature (using secret key)                         │
│ 3. Check token expiration                                       │
│ 4. Extract claims                                               │
│ 5. Create ClaimsPrincipal                                       │
│ 6. Attach to HttpContext.User                                   │
└────────────┬────────────────────────────────────────────────────┘
             │
             ├─ Valid? → YES
             │
             ▼
┌─────────────────────────────────────────────────────────────────┐
│          AUTHORIZATION POLICY CHECK (Program.cs)                 │
├─────────────────────────────────────────────────────────────────┤
│ Check [Authorize] attribute:                                    │
│ - Requires authentication? ✓ Already authenticated              │
│ - Specific roles required? Check user's role claim              │
│ - Policy specified? Check policy definition                     │
│ - Examples:                                                      │
│   [Authorize] → Any authenticated user                          │
│   [Authorize(Roles="Admin")] → Must be admin                   │
│   [Authorize(Policy="ViewAll")] → Policy check                 │
└────────────┬────────────────────────────────────────────────────┘
             │
             ├─ Has Permission? → YES
             │
             ▼
┌─────────────────────────────────────────────────────────────────┐
│                TRANSACTION CONTROLLER                            │
├─────────────────────────────────────────────────────────────────┤
│ [Authorize]                                                     │
│ [HttpGet("me")]                                                 │
│ public async Task<IActionResult> GetMyTransactions()           │
│ {                                                               │
│   var userId = GetCurrentUserId(); // From claims              │
│   return await _service.GetTransactions(userId);               │
│ }                                                               │
└────────────┬────────────────────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────────────────────┐
│              TRANSACTION SERVICE                                 │
├─────────────────────────────────────────────────────────────────┤
│ public async Task<List<Transaction>>                            │
│   GetTransactions(Guid userId)                                  │
│ {                                                               │
│   // Service-level authorization check                         │
│   _authService.EnsureCanViewUserData(userId);                 │
│                                                                 │
│   // Fetch from database                                       │
│   return await _db.Transactions                                │
│     .Where(t => t.UserId == userId)                            │
│     .ToListAsync();                                            │
│ }                                                               │
└────────────┬────────────────────────────────────────────────────┘
             │
             ├─ Authorized at service level? YES
             │
             ▼
┌─────────────────────────────────────────────────────────────────┐
│               DATABASE QUERY                                     │
├─────────────────────────────────────────────────────────────────┤
│ SELECT * FROM Transactions                                      │
│ WHERE UserId = @userId                                          │
│ ORDER BY CreatedAt DESC                                         │
└────────────┬────────────────────────────────────────────────────┘
             │ Results
             ▼
┌─────────────────────────────────────────────────────────────────┐
│              AUDIT SERVICE (Optional)                            │
├─────────────────────────────────────────────────────────────────┤
│ Log the action:                                                 │
│ INSERT INTO AuditLogs                                           │
│ (UserId, Action, EntityName, EntityId, Timestamp)              │
│ VALUES (currentUserId, 'Read', 'Transaction', 'all', NOW)     │
└────────────┬────────────────────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────────────────────┐
│              HTTP RESPONSE (200 OK)                              │
├─────────────────────────────────────────────────────────────────┤
│ [                                                               │
│   { TransactionId: "...", Amount: 100.00, ... },              │
│   { TransactionId: "...", Amount: 50.00, ... }                │
│ ]                                                               │
└─────────────────────────────────────────────────────────────────┘
```

---

## Authorization Decision Tree

```
Client Request
│
├─ No [Authorize] on endpoint?
│  └─ YES → Allow (public endpoint)
│
└─ Has [Authorize]?
   │
   ├─ NO JWT Token?
   │  └─ YES → 401 Unauthorized ❌
   │
   ├─ Token Signature Invalid?
   │  └─ YES → 401 Unauthorized ❌
   │
   ├─ Token Expired?
   │  └─ YES → 401 Unauthorized ❌
   │          (User should refresh)
   │
   ├─ No Specific Role/Policy Required?
   │  └─ YES → Allow ✓
   │
   ├─ Specific Role Required?
   │  ├─ User has role?
   │  │  └─ YES → Check Service Level
   │  │  └─ NO → 403 Forbidden ❌
   │  │
   │  └─ Policy Required?
   │     ├─ Policy check passes?
   │     │  └─ YES → Check Service Level
   │     │  └─ NO → 403 Forbidden ❌
   │     │
   │     └─ Service Level Check
   │        ├─ AuthorizationService allows?
   │        │  └─ YES → Execute Business Logic ✓
   │        │  └─ NO → 403 Forbidden ❌
```

---

## Database-Code Relationship

```
┌──────────────────────────────────┐
│      DATABASE (Roles Table)       │
├──────────────────────────────────┤
│ Id  │ Name      │ IsActive       │
├─────┼───────────┼────────────────┤
│ 1   │ Admin     │ true           │
│ 2   │ Manager   │ true           │
│ 3   │ Auditor   │ true           │
│ 4   │ User      │ true           │
└──────────────────────────────────┘
         │
         │ Loaded when User logs in
         │ Attached to JWT token as claim
         │
         ▼
┌──────────────────────────────────┐
│      JWT Token Payload            │
├──────────────────────────────────┤
│ {                                │
│   "sub": "userId",               │
│   "role": "Manager",             │ ← From DB
│   "email": "user@example.com",   │
│   "username": "john",            │
│   "iat": 1234567890,             │
│   "exp": 1234571490              │
│ }                                │
└──────────────────────────────────┘
         │
         │ Token sent with each request
         │ Extracted by middleware
         │
         ▼
┌──────────────────────────────────┐
│    Program.cs Policies            │
├──────────────────────────────────┤
│ "AdminOnly"                      │
│   → require role "Admin"         │
│                                  │
│ "ViewAllUsers"                   │
│   → require role "Admin" OR      │
│      "Manager"                   │
│                                  │
│ "ViewAuditLogs"                  │
│   → require role "Admin" OR      │
│      "Manager" OR "Auditor"      │
└──────────────────────────────────┘
         │
         │ Policy applied to endpoints
         │ Role claim checked from token
         │
         ▼
┌──────────────────────────────────┐
│     Controller Attributes        │
├──────────────────────────────────┤
│ [Authorize(Policy = "ViewAll")]  │
│ public GetAll() { }              │
│                                  │
│ Only allowed for Admin/Manager   │
│ Permission managed entirely      │
│ in code, not database!           │
└──────────────────────────────────┘
```

---

## Role Hierarchy & Permissions

```
┌─────────────────────────────────────────────────────────────┐
│                         ADMIN                               │
│  • Full system access                                       │
│  • Can do anything                                          │
│  • Implicit permission for all actions                      │
├─────────────────────────────────────────────────────────────┤
│  ✓ View own data      ✓ View all users                     │
│  ✓ Edit own data      ✓ Edit other users                   │
│  ✓ Delete own data    ✓ Delete users                       │
│  ✓ Create records     ✓ View audit logs                    │
│                       ✓ Manage roles                        │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                       MANAGER                               │
│  • Manage other users' data                                 │
│  • View audit logs                                          │
│  • Cannot delete users or manage roles                      │
├─────────────────────────────────────────────────────────────┤
│  ✓ View own data      ✓ View all users                     │
│  ✓ Edit own data      ✗ Edit other users (limited)        │
│  ✓ Delete own data    ✗ Delete users                       │
│  ✓ Create records     ✓ View audit logs                    │
│                       ✗ Manage roles                        │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                       AUDITOR                               │
│  • Read-only access                                         │
│  • Cannot modify anything                                   │
│  • Can view audit logs                                      │
├─────────────────────────────────────────────────────────────┤
│  ✓ View own data      ✓ View all users (read-only)        │
│  ✗ Edit own data      ✗ Edit other users                  │
│  ✗ Delete own data    ✗ Delete users                       │
│  ✗ Create records     ✓ View audit logs                    │
│                       ✗ Manage roles                        │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                       USER (Regular)                        │
│  • Access to own data only                                  │
│  • Cannot view other users                                  │
│  • Cannot access admin functions                            │
├─────────────────────────────────────────────────────────────┤
│  ✓ View own data      ✗ View all users                     │
│  ✓ Edit own data      ✗ Edit other users                  │
│  ✓ Delete own data    ✗ Delete users                       │
│  ✓ Create records     ✗ View audit logs                    │
│                       ✗ Manage roles                        │
└─────────────────────────────────────────────────────────────┘
```

---

## Error Response Codes

```
┌────────────────────────────────────────────────────────────┐
│ 200 OK                                                     │
├────────────────────────────────────────────────────────────┤
│ Request successful, resource returned                      │
│ Response body contains the data                            │
└────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────┐
│ 201 Created                                                │
├────────────────────────────────────────────────────────────┤
│ Resource created successfully                              │
│ Response includes Location header with new resource URL    │
└────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────┐
│ 400 Bad Request                                            │
├────────────────────────────────────────────────────────────┤
│ Invalid request data (validation error)                    │
│ Response body explains what's wrong                        │
└────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────┐
│ 401 Unauthorized ❌                                        │
├────────────────────────────────────────────────────────────┤
│ No valid JWT token provided                               │
│ OR Token expired or invalid signature                      │
│ Action: Login again or refresh token                       │
│                                                            │
│ Body: { "message": "Unauthorized" }                        │
└────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────┐
│ 403 Forbidden ❌                                           │
├────────────────────────────────────────────────────────────┤
│ Valid JWT but insufficient permissions                     │
│ User role/policy doesn't allow this action                │
│ Action: Contact admin for permission upgrade               │
│                                                            │
│ Body: { "message": "Forbidden" }                          │
└────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────┐
│ 404 Not Found                                              │
├────────────────────────────────────────────────────────────┤
│ Resource not found                                         │
│ Check the resource ID                                      │
└────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────┐
│ 500 Internal Server Error                                  │
├────────────────────────────────────────────────────────────┤
│ Server error occurred                                      │
│ Check server logs for details                              │
└────────────────────────────────────────────────────────────┘
```

---

## Token Lifecycle

```
Time Line:
0 min         15 min        7 days
│             │             │
├─ Access Token ────────────┤
│ Short-lived │             │
│ 15 minutes  │             │
└─────────────┴─────────────┘
                │
                ├─ Expired! User needs new token
                │
                ├─ Call POST /auth/refresh
                │ with refreshToken
                │
                ▼
├─────────────────────────────────────────────┤
│      New Access Token Generated             │
│ 15 more minutes                             │
└─────────────────────────────────────────────┘

├────────────────────────────────────────────────────────────┤
│ Refresh Token (In Database + HTTP-only Cookie)             │
│ 7 days lifetime                                             │
│ Never expires unless:                                       │
│ • User logs out (revoked in DB)                             │
│ • 7 days pass                                               │
│ • User password changes (invalidated)                       │
└────────────────────────────────────────────────────────────┘
```

---

## Sequence: Login & First Request

```
Step 1: Login
┌─────────────────────────────────────────┐
│ Client: POST /auth/login                │
│ Body: { username, password }            │
└────────────┬────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────┐
│ Server: Validate credentials            │
│ Generate JWT accessToken                │
│ Generate refreshToken & save to DB      │
└────────────┬────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────┐
│ Response:                               │
│ {                                       │
│   "accessToken": "jwt...",              │
│   "refreshToken": "jwt...",             │
│   "expiresIn": 900                      │
│ }                                       │
└─────────────────────────────────────────┘

Step 2: First API Request
┌─────────────────────────────────────────┐
│ Client stores tokens                    │
│ accessToken → localStorage              │
│ refreshToken → httpOnly cookie          │
└────────────┬────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────┐
│ Client: GET /api/transactions/me        │
│ Header: Authorization: Bearer <access>  │
└────────────┬────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────┐
│ Server Middleware:                      │
│ 1. Extract token from header            │
│ 2. Validate signature                   │
│ 3. Check expiration                     │
│ 4. Extract claims                       │
└────────────┬────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────┐
│ Controller:                             │
│ Get ClaimsPrincipal (User)              │
│ Read role, email, etc from claims       │
└────────────┬────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────┐
│ Check [Authorize] policy                │
│ Verify user has required role/claim     │
└────────────┬────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────┐
│ Response: 200 OK                        │
│ Body: [ transaction, transaction, ... ] │
└─────────────────────────────────────────┘
```

---

## No Permission Table Architecture

```
❌ WRONG (With Permission Table):
┌──────────┐    ┌─────────────┐    ┌─────────────┐    ┌────────────┐
│ Roles    ├─►  │Role_Perms   ├─►  │ Permissions ├─►  │ User_Roles │
└──────────┘    └─────────────┘    └─────────────┘    └────────────┘

Query: SELECT p.Name FROM Permissions p
       JOIN RolePermissions rp ON p.Id = rp.PermissionId
       JOIN Roles r ON rp.RoleId = r.Id
       WHERE r.Id = @roleId

Problems:
- 3+ tables
- Multiple joins
- Slow queries
- Migrations needed for new permissions
```

```
✅ CORRECT (Code-Based):
┌──────────┐              ┌─────────────────┐
│ Roles    │              │  Program.cs     │
│          │              │  (Policies)     │
│ - Admin  │              │                 │
│ - Manager├─────────────►│ - AdminOnly     │
│ - Auditor│              │ - ViewAllUsers  │
│ - User   │              │ - ViewAuditLogs │
└──────────┘              └─────────────────┘

No table joins needed
Fast authorization checks
Permissions change with code deployments
```

---

This is the foundation of your authorization system! 🎯
