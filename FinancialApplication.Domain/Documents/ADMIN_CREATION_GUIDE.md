## HOW TO CREATE YOUR FIRST ADMIN USER

### Method 1: Direct SQL Query (Fastest)

Run this SQL script in your database to create the first admin user:

```sql
-- Step 1: Ensure all roles exist
INSERT INTO Roles (Name, IsActive) VALUES ('Admin', 1)
GO
INSERT INTO Roles (Name, IsActive) VALUES ('Manager', 1)
GO
INSERT INTO Roles (Name, IsActive) VALUES ('Auditor', 1)
GO
INSERT INTO Roles (Name, IsActive) VALUES ('User', 1)
GO

-- Step 2: Get the Admin role ID (usually ID = 1)
DECLARE @AdminRoleId INT = (SELECT Id FROM Roles WHERE Name = 'Admin' AND IsActive = 1)

-- Step 3: Create admin user with hashed password
-- Password: "Admin@123" (hashed with BCrypt)
INSERT INTO Users (Id, Username, Email, Password, RoleId, IsActive, CreatedAt, UpdatedAt)
VALUES (
    NEWID(),
    'admin',
    'admin@financialapp.com',
    '$2a$11$xxxx...', -- Replace with BCrypt hashed password (see Method 2)
    @AdminRoleId,
    1,
    GETUTCDATE(),
    GETUTCDATE()
)
GO
```

### Method 2: Generate Hashed Password (Required for SQL)

Run this C# code in Visual Studio's Package Manager Console or a test file to get the BCrypt hash:

```csharp
// Install: Install-Package BCrypt.Net-Next
using BCrypt.Net;

string password = "Admin@123"; // Change this
string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
Console.WriteLine(hashedPassword);
// Output will be something like: $2a$11$L9.BlV8zYNmKl0.PbP8j7.xxxxx
```

Then use this hash in the SQL query above.

### Method 3: Use the API (After First Admin is Created)

1. **Create a regular user first** (via `/api/auth/register`):
   ```json
   {
     "username": "tempuser",
     "email": "temp@example.com",
     "password": "TempPass@123"
   }
   ```

2. **Manually update the database** to set this user's RoleId to Admin role ID

3. **Login with this admin account** to get JWT token

4. **Use `/api/auth/assign-role` endpoint** to assign roles to other users:
   ```json
   {
     "userId": "user-guid-here",
     "roleName": "Manager"
   }
   ```

---

## AVAILABLE ROLES & THEIR PERMISSIONS

| Role | Permissions |
|------|-----------|
| **Admin** | • Manage all users<br>• Assign/change roles<br>• View audit logs<br>• Manage transactions, investments, goals |
| **Manager** | • View all users<br>• Create users<br>• Edit user data<br>• Manage transactions, investments, goals |
| **Auditor** | • View audit logs only |
| **User** | • View/manage own data only |

---

## ENDPOINTS

### Register User (No Auth Required)
```
POST /api/auth/register
Content-Type: application/json

{
  "username": "john_doe",
  "email": "john@example.com",
  "password": "SecurePass@123"
}

Response: 200 OK
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "...",
  "expiresAt": "2026-01-01T10:30:00Z",
  "expiresIn": 900
}
```

### Login (No Auth Required)
```
POST /api/auth/login
Content-Type: application/json

{
  "email": "admin@financialapp.com",
  "password": "Admin@123"
}

Response: 200 OK
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "...",
  "expiresAt": "2026-01-01T10:30:00Z",
  "expiresIn": 900
}
```

### Assign Role to User (Admin Only)
```
POST /api/auth/assign-role
Authorization: Bearer {your-admin-token}
Content-Type: application/json

{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "roleName": "Manager"
}

Response: 200 OK
"User 'john_doe' assigned role 'Manager' successfully."
```

---

## QUICK START SUMMARY

1. **Create Roles** (SQL Script or EF Migrations)
2. **Create Admin User** (SQL Query with BCrypt hash)
3. **Login as Admin** (get JWT token)
4. **Assign Roles to Others** (use `/assign-role` endpoint)
