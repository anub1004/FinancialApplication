# Financial Application - Backend Architecture & Technical Documentation

## 📋 Table of Contents

1. [Project Overview](#project-overview)
2. [Solution Architecture](#solution-architecture)
3. [Technology Stack](#technology-stack)
4. [Folder Structure](#folder-structure)
5. [Module Descriptions](#module-descriptions)
6. [Database Schema](#database-schema)
7. [API Flow & Request/Response Cycle](#api-flow--requestresponse-cycle)
8. [Authentication & Authorization](#authentication--authorization)
9. [Configuration & Dependency Injection](#configuration--dependency-injection)
10. [Data Flow Diagrams](#data-flow-diagrams)
11. [Key Services & their Responsibilities](#key-services--their-responsibilities)

---

## Project Overview

The **Financial Application** is a comprehensive .NET 8-based backend system designed to manage financial data, user authentication, investment tracking, and news aggregation. It follows a **Clean Architecture** pattern with clear separation of concerns across multiple projects.

### Key Features:
- **User Authentication & Authorization** with JWT tokens
- **Role-Based Access Control** (Admin, Manager, Auditor, User)
- **Financial News Aggregation** from multiple sources
- **Investment & Goal Tracking**
- **Audit Logging** for compliance
- **News Data Update Service** (background job)
- **Refresh Token Management**

---

## Solution Architecture

```
Financial Application Solution (Clean Architecture)
│
├── Presentation Layer
│   └── FinancialApplication.Api (ASP.NET Core 8 REST API)
│
├── Application Layer
│   └── FinancialApplication.Application (Business Logic & Interfaces)
│
├── Domain Layer
│   └── FinancialApplication.Domain (Entities & Core Business Rules)
│
├── Infrastructure Layer
│   ├── FinancialApplication.Infrastructure (Data Access, Services, Security)
│   └── NewsDataUpdateService (Background Worker Service)
│
└── Testing Layer
	└── FinancialApplication.Tests (Unit/Integration Tests)
```

### Architecture Principles:
- **Dependency Inversion**: High-level modules don't depend on low-level modules; both depend on abstractions
- **Single Responsibility**: Each class has one reason to change
- **Loose Coupling**: Components interact through interfaces
- **SOLID Principles**: Followed throughout the codebase

---

## Technology Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **Framework** | .NET 8 | Runtime & Framework |
| **Web Framework** | ASP.NET Core | REST API hosting |
| **ORM** | Entity Framework Core 8 | Database access & migrations |
| **Database** | SQL Server | Primary data store |
| **Authentication** | JWT (JSON Web Tokens) | Stateless authentication |
| **Security** | BCrypt | Password hashing |
| **Serialization** | System.Text.Json | JSON processing |
| **HTML Parsing** | HtmlAgilityPack | Web scraping for images |
| **Logging** | Microsoft.Extensions.Logging | Application logging |
| **HTTP Client** | HttpClientFactory | HTTP requests management |
| **CORS** | Built-in ASP.NET Core | Cross-origin requests |
| **Swagger/OpenAPI** | Swashbuckle | API documentation |

---

## Folder Structure

```
FinancialApplication/
│
├── FinancialApplication.Api/                      # Presentation Layer
│   ├── Controllers/
│   │   ├── Auth/
│   │   │   └── AuthController.cs                  # Login, Register, Token Refresh
│   │   ├── Admin/
│   │   │   └── AdminController.cs                 # Admin operations (role management)
│   │   ├── TodayNewsController.cs                 # Today's news articles retrieval
│   │   ├── FinanceNewsController.cs               # Finance news articles retrieval
│   │   ├── BlogController.cs                      # Blog-related operations
│   │   └── WeatherForecast.cs                     # Sample controller
│   ├── Properties/
│   │   └── launchSettings.json                    # Development settings
│   ├── appsettings.json                           # Configuration (production)
│   ├── appsettings.Development.json               # Configuration (development)
│   ├── Program.cs                                 # Startup & DI configuration
│   └── FinancialApplication.Api.csproj
│
├── FinancialApplication.Application/              # Application Layer (Business Logic)
│   ├── Interfaces/
│   │   ├── IAuthService.cs                        # Auth service contract
│   │   ├── IAdminService.cs                       # Admin operations contract
│   │   ├── IBannerFetchService.cs                 # Banner fetching contract
│   │   ├── INewsProcessingService.cs              # News processing contract
│   │   └── IJwtTokenGenerator.cs                  # JWT generation contract
│   ├── DTOs/                                      # Data Transfer Objects
│   │   ├── AuthenticationResult.cs
│   │   ├── AuthDto.cs
│   │   ├── LoginUserDto.cs
│   │   ├── RegisterUserDto.cs
│   │   └── ...other DTOs
│   └── FinancialApplication.Application.csproj
│
├── FinancialApplication.Domain/                   # Domain Layer (Entities)
│   ├── Domain/
│   │   ├── Entity/                                # Core Entities
│   │   │   ├── User.cs                            # User entity
│   │   │   ├── Role.cs                            # Role entity (Admin, User, etc.)
│   │   │   ├── Transaction.cs                     # Financial transaction
│   │   │   ├── Investment.cs                      # Investment record
│   │   │   ├── Goal.cs                            # Financial goal
│   │   │   ├── RefreshToken.cs                    # Token storage
│   │   │   ├── AuditLog.cs                        # Audit trail
│   │   │   ├── TodayNewsArticle.cs                # Today's news article
│   │   │   ├── FinanceNewsArticle.cs              # Finance news article
│   │   │   └── Transaction.cs.cs                  # ⚠️ Duplicate (naming issue)
│   │   └── Enums/
│   │       ├── GoalStatusEnum.cs                  # Goal status values
│   │       └── TransactionTypeEnum.cs             # Transaction type values
│   └── FinancialApplication.Domain.csproj
│
├── FinancialApplication.Infrastructure/           # Infrastructure Layer
│   ├── Data/
│   │   ├── AppDbContext.cs                        # EF Core DbContext (all tables)
│   │   └── Migrations/
│   │       ├── 20250522053541_InitialMigration
│   │       ├── 20250626111453_AddNewsArticleTable
│   │       ├── 20250629093827_AddTodayNewsArticleTable
│   │       ├── 20250630073244_AddSeparateNewsTables
│   │       ├── 20250702064859_AddArticleCountColumn
│   │       └── AppDbContextModelSnapshot.cs
│   ├── Security/
│   │   ├── AuthorizationService.cs                # Authorization logic
│   │   ├── JwtTokenGenerator.cs                   # JWT token creation
│   │   ├── PasswordHasher.cs                      # Password hashing (BCrypt)
│   │   └── RefreshTokenGenerator.cs               # Refresh token generation
│   ├── Services/
│   │   ├── AuthService.cs                         # Authentication & user management
│   │   ├── AdminService.cs                        # Admin operations
│   │   ├── AuditService.cs                        # Audit logging
│   │   ├── BannerFetchService.cs                  # Banner/image fetching
│   │   └── NewsProcessingService.cs               # News scraping & processing
│   └── FinancialApplication.Infrastructure.csproj
│
├── NewsDataUpdateService/                         # Background Worker Service
│   ├── Services/                                  # Service implementations
│   ├── Program.cs                                 # Console app entry point
│   ├── appsettings.json                           # Service configuration
│   └── NewsDataUpdateService.csproj
│
├── FinancialApplication.Tests/                    # Testing Layer
│   ├── UnitTest1.cs                               # Sample unit test
│   └── FinancialApplication.Tests.csproj
│
├── Documentation/                                 # 📁 NEW: Documentation folder
│   └── BACKEND_ARCHITECTURE.md                    # This file
│
└── .github/                                       # GitHub configuration
	└── workflows/                                 # CI/CD pipelines
```

---

## Module Descriptions

### 1. **FinancialApplication.Api** (Presentation Layer)

**Purpose**: Exposes REST endpoints for client applications

**Responsibilities**:
- Handle HTTP requests/responses
- Route requests to appropriate controllers
- Validate input data
- Set HTTP status codes
- Manage cookies (auth tokens)
- Implement CORS policy

**Key Controllers**:

#### **AuthController** (`/api/auth`)
```
POST /api/auth/register          → Create new user account
POST /api/auth/login             → Authenticate user, return JWT
POST /api/auth/refresh-token     → Refresh expired access token
POST /api/auth/logout            → Invalidate user session
GET  /api/auth/check-auth        → Verify current authentication
POST /api/auth/validate-token    → Validate token validity
```

#### **AdminController** (`/api/admin`)
```
POST   /api/admin/assign-role    → Assign role to user
POST   /api/admin/revoke-role    → Remove role from user
DELETE /api/admin/deactivate-user → Deactivate user account
GET    /api/admin/users          → List all users
```

#### **FinanceNewsController** (`/api/FinanceNews`)
```
GET /api/FinanceNews?page=1&pageSize=10&search=stocks
	→ Fetch paginated finance news with optional search
```

#### **TodayNewsController** (`/api/TodayNews`)
```
GET /api/TodayNews?page=1&pageSize=10&search=technology
	→ Fetch paginated today's news with optional search
```

#### **BlogController** (`/api/Blog`)
```
GET /api/Blog                    → Fetch blog articles
```

---

### 2. **FinancialApplication.Application** (Application/Business Logic Layer)

**Purpose**: Contains business logic and contracts

**Responsibilities**:
- Define service interfaces
- Create Data Transfer Objects (DTOs)
- Implement business rules
- Handle use case orchestration

**Key Interfaces** (Contracts):

| Interface | Purpose |
|-----------|---------|
| `IAuthService` | User authentication, registration, token management |
| `IAdminService` | User & role management |
| `IJwtTokenGenerator` | JWT token creation |
| `INewsProcessingService` | News fetching, scraping, processing |
| `IBannerFetchService` | Extract images from news URLs |
| `IAuthorizationService` | Check authorization (roles/permissions) |
| `IAuditService` | Log user actions |
| `IPasswordHasher` | Hash & verify passwords |

**Key DTOs** (Data Contracts):
- `RegisterUserDto`: User registration data
- `LoginUserDto`: User login credentials
- `AuthenticationResult`: Login/register response
- `AuthDto`: Token validation result

---

### 3. **FinancialApplication.Domain** (Domain Layer - Core Business Entities)

**Purpose**: Represents core business concepts as entities

**Responsibilities**:
- Define entity structure
- Express business rules
- No external dependencies (database, HTTP, etc.)

**Key Entities**:

| Entity | Purpose | Key Properties |
|--------|---------|-----------------|
| **User** | Represents application user | Id, Username, Email, Password, RoleId, IsActive, CreatedAt, UpdatedAt |
| **Role** | User role/permission level | Id, Name (Admin/User/Manager/Auditor), IsActive |
| **Transaction** | Financial transaction record | Id, UserId, Amount, Type, Date, Description |
| **Investment** | Investment tracking | Id, UserId, AmountInvested, ReturnValue, Status |
| **Goal** | Financial goal tracking | Id, UserId, TargetAmount, CurrentAmount, Status |
| **RefreshToken** | Token storage | RefreshTokenId, UserId, Token, ExpiresAt |
| **AuditLog** | User action logging | AuditLogId, UserId, Action, Timestamp |
| **FinanceNewsArticle** | Finance news storage | Id, JsonData (articles array), ArticleCount, CreatedAt |
| **TodayNewsArticle** | General news storage | Id, JsonData (articles array), ArticleCount, CreatedAt |

**Enums**:
- `TransactionTypeEnum`: Income, Expense, Transfer, Investment
- `GoalStatusEnum`: NotStarted, InProgress, Completed, Failed

---

### 4. **FinancialApplication.Infrastructure** (Infrastructure Layer)

**Purpose**: Handles all external integrations and data access

#### **4.1 Data Access (AppDbContext)**

```csharp
DbSets:
- Users
- Transactions
- Investments
- Goals
- Roles
- RefreshTokens
- AuditLogs
- FinanceNewsArticles
- TodayNewsArticles
```

**Key Features**:
- Fluent EF Core configuration
- Role seeding (Admin, User, Manager, Auditor)
- Foreign key relationships
- Index optimization
- Migration support

#### **4.2 Security Services**

**AuthService** (`AuthService.cs`)
```
Functions:
├── RegisterAsync()           → Create new user, hash password
├── LoginAsync()              → Verify credentials, return tokens
├── AuthenticateAsync()       → Generate authentication tokens
├── RefreshAccessTokenAsync() → Issue new access token
├── ValidateAccessToken()     → Verify token signature & expiry
├── ValidateRefreshTokenAsync() → Validate refresh token
├── _Logout()                 → Invalidate tokens
├── CheckAuth()               → Verify current session
└── ValidateToken()           → Parse & validate JWT claims
```

**JwtTokenGenerator** (`JwtTokenGenerator.cs`)
```
Generates JWT tokens with:
- Subject (UserId)
- Claims (Username, Email, Role)
- Expiration (from config)
- Signing credentials (secret key)
```

**PasswordHasher** (`PasswordHasher.cs`)
```
- HashPassword()    → BCrypt hashing
- VerifyPassword()  → Compare hashed password
```

**RefreshTokenGenerator** (`RefreshTokenGenerator.cs`)
```
- Generate()        → Create secure refresh token
- Validate()        → Verify token existence & expiry
```

**AuthorizationService** (`AuthorizationService.cs`)
```
- CheckUserRole()   → Verify user role permissions
- HasPermission()   → Check if user has permission
```

#### **4.3 Business Services**

**AdminService** (`AdminService.cs`)
```
└── UserManagement:
	├── AssignRoleAsync()     → Change user role
	├── RevokeRoleAsync()     → Revoke user role
	├── DeactivateUserAsync() → Disable user account
	└── ActivateUserAsync()   → Re-enable user account
```

**AuditService** (`AuditService.cs`)
```
└── AuditLogging:
	├── LogAsync()           → Record user action
	├── GetAuditLogsAsync()  → Retrieve audit trail
	└── GetUserAuditAsync()  → Get user's action history
```

**BannerFetchService** (`BannerFetchService.cs`)
```
└── ImageExtraction:
	├── FetchBannerAsync()   → Extract image from URL
	├── GetImageUrl()        → Parse image URL from HTML
	└── ValidateImageUrl()   → Check image URL validity
```

**NewsProcessingService** (`NewsProcessingService.cs`)
```
└── NewsManagement:
	├── FetchNewsAsync()     → Call external news API
	├── ProcessNewsAsync()   → Parse, filter, enrich articles
	├── ScrapeImageAsync()   → Extract images for articles
	├── SaveNewsAsync()      → Store in database
	├── CleanupOldNews()     → Remove expired articles
	└── GetArticlesAsync()   → Retrieve news from DB
```

---

### 5. **NewsDataUpdateService** (Background Worker Service)

**Purpose**: Scheduled background job to fetch and update news data

**Technology**: .NET Console Application with Dependency Injection

**Entry Point**: `Program.cs`

**Workflow**:
```
1. Read configuration (API URLs, API keys)
2. Initialize DI container with DbContext & services
3. Fetch Finance News from API #1
   └── Parse response → Scrape images → Enrich articles → Save to DB
4. Fetch Today's News from API #2
   └── Parse response → Scrape images → Enrich articles → Save to DB
5. Cleanup old articles (older than retention days)
6. Log results & exit
```

**Configuration (appsettings.json)**:
```json
{
  "NewsService": {
	"ApiUrl": "https://newsapi.example.com/v2/everything?...",
	"ApiKey": "your-api-key",
	"RetentionDays": 7,
	"MaxConcurrentScrapes": 10
  },
  "NewsConfigApi2": {
	"ApiUrl": "https://newsapi2.example.com/v2/top-headlines?...",
	"ApiKey": "another-api-key"
  }
}
```

**Features**:
- Thread-safe image URL caching
- Concurrent HTTP requests management
- Error handling & retry logic
- Configurable timeout & headers
- News data retention policy
- Comprehensive logging

---

## Database Schema

### Entity Relationships

```
┌─────────────────────────────────────────────────┐
│                    User                          │
│  ┌───────────────────────────────────────────┐  │
│  │ Id (GUID) [PK]                            │  │
│  │ Username (unique, max 50)                 │  │
│  │ Password (hashed, max 255)                │  │
│  │ Email (unique, max 100)                   │  │
│  │ RoleId (FK → Roles)                       │  │
│  │ IsActive (bool)                           │  │
│  │ CreatedAt (DateTime)                      │  │
│  │ UpdatedAt (DateTime)                      │  │
│  └───────────────────────────────────────────┘  │
│         ↓         ↓         ↓                    │
│    [FK]       [FK]      [FK]                    │
└─────────────────────────────────────────────────┘
  ↓              ↓              ↓
┌─────────────┐ ┌──────────────┐ ┌──────────────┐
│  Transactions│ │ Investments  │ │    Goals     │
├─────────────┤ ├──────────────┤ ├──────────────┤
│ Id          │ │ Id           │ │ Id           │
│ UserId(FK)  │ │ UserId(FK)   │ │ UserId(FK)   │
│ Amount      │ │ Amount       │ │ TargetAmount │
│ Type        │ │ Returns      │ │ CurrentAmt   │
│ Date        │ │ Status       │ │ Status       │
└─────────────┘ └──────────────┘ └──────────────┘


┌──────────────────────────────────────────────┐
│                 Role                          │
│ ┌────────────────────────────────────────┐  │
│ │ Id (int) [PK]                          │  │
│ │ Name (unique: Admin/User/Manager/etc.) │  │
│ │ IsActive (bool)                        │  │
│ │ 1 → Many with User                     │  │
│ └────────────────────────────────────────┘  │
└──────────────────────────────────────────────┘


┌──────────────────────────────┐
│     RefreshToken             │
├──────────────────────────────┤
│ RefreshTokenId (GUID) [PK]   │
│ UserId (FK → User)           │
│ Token (string)               │
│ ExpiresAt (DateTime)         │
│ CreatedAt (DateTime)         │
└──────────────────────────────┘


┌──────────────────────────────┐
│        AuditLog              │
├──────────────────────────────┤
│ AuditLogId (GUID) [PK]       │
│ UserId (FK → User)           │
│ Action (string)              │
│ Timestamp (DateTime)         │
│ Details (string, optional)   │
└──────────────────────────────┘


┌────────────────────────────────────┐
│    FinanceNewsArticle              │
├────────────────────────────────────┤
│ Id (GUID) [PK]                     │
│ JsonData (nvarchar(max)) - array   │
│ ArticleCount (int)                 │
│ CreatedAt (DateTime UTC)           │
└────────────────────────────────────┘


┌────────────────────────────────────┐
│    TodayNewsArticle                │
├────────────────────────────────────┤
│ Id (GUID) [PK]                     │
│ JsonData (nvarchar(max)) - array   │
│ ArticleCount (int)                 │
│ CreatedAt (DateTime UTC)           │
└────────────────────────────────────┘
```

### Database Indexes

| Table | Column | Type | Purpose |
|-------|--------|------|---------|
| Users | Email | Unique | Prevent duplicate emails |
| Users | Username | Unique | Prevent duplicate usernames |
| Roles | Name | Unique | Ensure unique role names |

### Cascading Deletes

- User → Transactions (CASCADE)
- User → RefreshTokens (CASCADE)
- User → AuditLogs (CASCADE)
- User → Role (RESTRICT - prevent orphaning)

---

## API Flow & Request/Response Cycle

### Authentication Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                        LOGIN REQUEST                             │
│                                                                   │
│  { email: "user@example.com", password: "secret123" }            │
│                          ↓                                        │
│   ┌────────────────────────────────────────────────┐            │
│   │        AuthController.Login()                   │            │
│   │  - Validates input                             │            │
│   │  - Calls IAuthService.LoginAsync()             │            │
│   └────────────────────────────────────────────────┘            │
│                          ↓                                        │
│   ┌────────────────────────────────────────────────┐            │
│   │  AuthService.LoginAsync()                      │            │
│   │  - Query users by email                        │            │
│   │  - Verify password (PasswordHasher)            │            │
│   │  - Check IsActive status                       │            │
│   │  - Load user's Role                            │            │
│   │  - Call AuthenticateAsync()                    │            │
│   └────────────────────────────────────────────────┘            │
│                          ↓                                        │
│   ┌────────────────────────────────────────────────┐            │
│   │  AuthService.AuthenticateAsync()               │            │
│   │  - Generate Access JWT (IJwtTokenGenerator)    │            │
│   │  - Generate Refresh Token                      │            │
│   │  - Save refresh token to DB                    │            │
│   │  - Return AuthenticationResult                 │            │
│   └────────────────────────────────────────────────┘            │
│                          ↓                                        │
│   ┌────────────────────────────────────────────────┐            │
│   │  AuthController.Login() - Sets Cookies         │            │
│   │  - authToken (HttpOnly, Secure, 1 hour)       │            │
│   │  - refreshToken (HttpOnly, Secure, 7 days)    │            │
│   │  - Returns 200 OK                              │            │
│   └────────────────────────────────────────────────┘            │
│                          ↓                                        │
│                    RESPONSE: 200 OK                               │
│                    {                                              │
│                      "isAuthenticated": true,                    │
│                      "token": "eyJhbGciOiJIUzI1NiIs...",         │
│                      "refreshtoken": "secure-refresh-token",     │
│                      "expiresIn": 3600,                          │
│                      "role": "User",                             │
│                      "message": "Login successful"               │
│                    }                                              │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

### Token Refresh Flow

```
┌──────────────────────────────────────────────────────┐
│          REFRESH TOKEN REQUEST                        │
│                                                       │
│  Header: Authorization: Bearer <expired-access-jwt>  │
│  Cookie: refreshToken=<valid-refresh-token>         │
│                          ↓                            │
│  ┌─────────────────────────────────────────┐        │
│  │  AuthController.RefreshToken()          │        │
│  │  - Extracts token from cookie           │        │
│  │  - Calls AuthService.RefreshAccessToken│        │
│  └─────────────────────────────────────────┘        │
│                          ↓                            │
│  ┌─────────────────────────────────────────┐        │
│  │  AuthService.RefreshAccessTokenAsync()  │        │
│  │  - Parse refresh token                  │        │
│  │  - Query refresh token from DB          │        │
│  │  - Verify not expired                   │        │
│  │  - Load user & role                     │        │
│  │  - Generate new access JWT              │        │
│  │  - Update refresh token expiry          │        │
│  └─────────────────────────────────────────┘        │
│                          ↓                            │
│           RESPONSE: 200 OK                           │
│           { "token": "new-jwt-token" }              │
│                                                       │
└──────────────────────────────────────────────────────┘
```

### Protected Resource Request Flow

```
┌────────────────────────────────────────────────────────────┐
│          ACCESS PROTECTED ENDPOINT                          │
│                                                             │
│  GET /api/FinanceNews                                      │
│  Header: Authorization: Bearer <access-jwt>               │
│                          ↓                                  │
│  ┌──────────────────────────────────────────┐             │
│  │  ASP.NET Core Authentication Middleware  │             │
│  │  - Extract JWT from Authorization header│             │
│  │  - Validate signature                    │             │
│  │  - Validate claims (iss, aud, exp)      │             │
│  │  - Create ClaimsPrincipal                │             │
│  │  - Attach to HttpContext.User            │             │
│  └──────────────────────────────────────────┘             │
│                          ↓ (If valid)                      │
│  ┌──────────────────────────────────────────┐             │
│  │  FinanceNewsController.GetNews()         │             │
│  │  [Authorize] attribute verified           │             │
│  │  - Query FinanceNewsArticles from DB     │             │
│  │  - Parse JSON data                       │             │
│  │  - Apply filters (search)                │             │
│  │  - Sort articles (images first)          │             │
│  │  - Paginate results                      │             │
│  └──────────────────────────────────────────┘             │
│                          ↓                                  │
│           RESPONSE: 200 OK                                 │
│           {                                                │
│             "articleCount": 150,                          │
│             "totalItems": 150,                            │
│             "totalPages": 3,                              │
│             "pageNumber": 1,                              │
│             "pageSize": 50,                               │
│             "items": [ { article objects } ]             │
│           }                                                │
│                                                             │
│           OR 401 Unauthorized (if JWT invalid)             │
│                                                             │
└────────────────────────────────────────────────────────────┘
```

### News Retrieval Flow

```
┌──────────────────────────────────────────────────────────────┐
│          NEWS FETCH & PROCESSING PIPELINE                     │
│                                                                │
│  NewsDataUpdateService.Main()                                 │
│           ↓                                                    │
│  ┌────────────────────────────────────────┐                  │
│  │  Read Configuration                     │                  │
│  │  - NewsService:ApiUrl                   │                  │
│  │  - NewsService:ApiKey                   │                  │
│  │  - NewsConfigApi2:ApiUrl                │                  │
│  │  - MaxConcurrentScrapes: 10             │                  │
│  └────────────────────────────────────────┘                  │
│           ↓                                                    │
│  ┌────────────────────────────────────────┐                  │
│  │  INewsProcessingService.FetchNewsAsync │                  │
│  │           ↓                             │                  │
│  │  1. HttpClient.GetAsync(apiUrl)        │                  │
│  │     - Read external news API response   │                  │
│  │     - Parse JSON to articles array      │                  │
│  │           ↓                             │                  │
│  │  2. ProcessNewsAsync()                  │                  │
│  │     - For each article (parallel):      │                  │
│  │       a) Check cache for image URL      │                  │
│  │       b) If not cached, scrape URL      │                  │
│  │       c) Extract image (HtmlAgilityPack)│                  │
│  │       d) Cache image URL                │                  │
│  │       e) Enrich article JSON with image │                  │
│  │           ↓                             │                  │
│  │  3. SaveNewsAsync()                     │                  │
│  │     - Serialize enriched articles       │                  │
│  │     - Save as single JSON record        │                  │
│  │     - Store in FinanceNewsArticles DB   │                  │
│  │           ↓                             │                  │
│  │  4. CleanupOldNews()                    │                  │
│  │     - Delete articles older than 7 days│                  │
│  │           ↓                             │                  │
│  │  RESULT: DB contains latest articles    │                  │
│  └────────────────────────────────────────┘                  │
│           ↓                                                    │
│  Client calls GET /api/FinanceNews                            │
│           ↓                                                    │
│  ┌────────────────────────────────────────┐                  │
│  │  FinanceNewsController.GetNews()       │                  │
│  │  - Query latest record from DB         │                  │
│  │  - Parse JSON array                    │                  │
│  │  - Apply filters (search parameter)    │                  │
│  │  - Sort (images first, then by date)   │                  │
│  │  - Paginate (page, pageSize params)    │                  │
│  │  - Return paginated results            │                  │
│  └────────────────────────────────────────┘                  │
│                                                                │
└──────────────────────────────────────────────────────────────┘
```

---

## Authentication & Authorization

### JWT Token Structure

```
Header.Payload.Signature
│       │        │
│       │        └─ HMAC-SHA256(header.payload, secret)
│       │
│       └─ Claims (encoded): 
│          {
│            "sub": "user-id-guid",          // Subject (user ID)
│            "username": "john_doe",
│            "email": "john@example.com",
│            "role": "Admin",
│            "iss": "FinancialApp",          // Issuer
│            "aud": "FinancialAppClient",    // Audience
│            "exp": 1234567890,              // Expiry timestamp
│            "iat": 1234567000               // Issued at
│          }
│
└─ Header: { "alg": "HS256", "typ": "JWT" }

Token Lifetime: 1 hour (default)
Refresh Token Lifetime: 7 days (default)
```

### JWT Configuration (appsettings.json)

```json
{
  "Jwt": {
	"Key": "your-super-secret-key-minimum-32-characters",
	"Issuer": "FinancialApp",
	"Audience": "FinancialAppClient",
	"ExpireMinutes": 60,
	"RefreshTokenExpireDays": 7
  }
}
```

### Role-Based Access Control (RBAC)

| Role | Permissions | Endpoints Accessible |
|------|-------------|----------------------|
| **Admin** | Full system access | All endpoints + /api/admin/* |
| **Manager** | Manage financial data | /api/FinanceNews, /api/Transactions, /api/Investments |
| **Auditor** | View-only access | /api/AuditLogs, /api/Reports |
| **User** | Personal data | /api/TodayNews, /api/Goals (own only) |

### Authorization Middleware

```csharp
// Program.cs
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuerSigningKey = true,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)),
			ValidateIssuer = true,
			ValidIssuer = "FinancialApp",
			ValidateAudience = true,
			ValidAudience = "FinancialAppClient",
			ValidateLifetime = true,
			ClockSkew = TimeSpan.Zero
		};
	})
```

### Protected Endpoints

```csharp
[Authorize]
[HttpGet("api/FinanceNews")]
public async Task<IActionResult> GetNews() { ... }

[Authorize(Roles = "Admin")]
[HttpPost("api/admin/assign-role")]
public async Task<IActionResult> AssignRole() { ... }
```

---

## Configuration & Dependency Injection

### Program.cs Configuration

```
1. CORS Setup
   └─ Allow localhost:5173 (React frontend)

2. Database Context
   └─ AppDbContext with SQL Server

3. JWT Services
   ├─ IJwtTokenGenerator → JwtTokenGenerator
   ├─ RefreshTokenGenerator
   └─ IPasswordHasher → PasswordHasher

4. Authentication Services
   ├─ IAuthService → AuthService
   ├─ IAuthorizationService → AuthorizationService
   └─ IAuditService → AuditService

5. Business Services
   ├─ IAdminService → AdminService
   ├─ INewsProcessingService → NewsProcessingService
   ├─ IBannerFetchService → BannerFetchService

6. HTTP Clients
   ├─ BannerFetcher (15s timeout)
   └─ NewsScraper (15s timeout, User-Agent header)

7. Authentication Scheme
   └─ JWT Bearer Tokens
```

### Dependency Injection Container

```csharp
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<RefreshTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<INewsProcessingService, NewsProcessingService>();
builder.Services.AddScoped<IBannerFetchService, BannerFetchService>();
```

### CORS Policy

```csharp
options.AddPolicy("ReactPolicy", policy =>
{
	policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
		  .AllowAnyHeader()
		  .AllowAnyMethod()
		  .AllowCredentials();
});
```

---

## Data Flow Diagrams

### Simplified Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                       Client (React)                             │
│                    (localhost:5173)                              │
└──────────────────────────┬──────────────────────────────────────┘
						   │ HTTP/CORS
						   ↓
┌─────────────────────────────────────────────────────────────────┐
│                   FinancialApplication.Api                       │
│  (ASP.NET Core 8, REST Endpoints, Port: 5000)                   │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  Controllers                                             │   │
│  │  ├─ AuthController      /api/auth/*                     │   │
│  │  ├─ AdminController     /api/admin/*                    │   │
│  │  ├─ FinanceNewsController /api/FinanceNews             │   │
│  │  ├─ TodayNewsController   /api/TodayNews               │   │
│  │  └─ BlogController        /api/Blog                    │   │
│  └─────────────────────────────────────────────────────────┘   │
│                           ↓                                      │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  Authentication Middleware (JWT)                        │   │
│  │  - Validate token                                       │   │
│  │  - Extract claims                                       │   │
│  │  - Attach to HttpContext.User                           │   │
│  └─────────────────────────────────────────────────────────┘   │
│                           ↓                                      │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  FinancialApplication.Application                       │   │
│  │  (Interfaces & DTOs)                                    │   │
│  └─────────────────────────────────────────────────────────┘   │
│                           ↓                                      │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  FinancialApplication.Infrastructure                    │   │
│  │  ├─ Services                                            │   │
│  │  │  ├─ AuthService         (JWT, tokens)               │   │
│  │  │  ├─ AdminService        (user mgmt)                 │   │
│  │  │  ├─ NewsProcessingService (news fetch/process)     │   │
│  │  │  ├─ BannerFetchService  (image extraction)         │   │
│  │  │  └─ AuditService        (logging)                   │   │
│  │  │                                                     │   │
│  │  ├─ Security                                           │   │
│  │  │  ├─ JwtTokenGenerator                               │   │
│  │  │  ├─ PasswordHasher (BCrypt)                         │   │
│  │  │  ├─ AuthorizationService                            │   │
│  │  │  └─ RefreshTokenGenerator                           │   │
│  │  └─ Data                                               │   │
│  │     └─ AppDbContext (Entity Framework Core)            │   │
│  └─────────────────────────────────────────────────────────┘   │
│                           ↓                                      │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  FinancialApplication.Domain                           │   │
│  │  (Entities: User, Role, Transaction, etc.)             │   │
│  └─────────────────────────────────────────────────────────┘   │
└──────────────────────┬───────────────────────────────────────────┘
					   │
				  DB Context
					   ↓
┌─────────────────────────────────────────────────────────────────┐
│                   SQL Server (Database)                          │
│                                                                  │
│  Tables:                                                         │
│  ├─ Users (id, username, email, password, roleId, ...)         │
│  ├─ Roles (id, name, isActive)                                  │
│  ├─ Transactions (id, userId, amount, type, date, ...)        │
│  ├─ Investments (id, userId, amount, status, ...)             │
│  ├─ Goals (id, userId, targetAmount, currentAmount, ...)      │
│  ├─ RefreshTokens (id, userId, token, expiresAt, ...)         │
│  ├─ AuditLogs (id, userId, action, timestamp, ...)            │
│  ├─ FinanceNewsArticles (id, jsonData, articleCount, ...)      │
│  └─ TodayNewsArticles (id, jsonData, articleCount, ...)        │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
		 ↑
		 │ (NewsDataUpdateService)
		 │ (Scheduled background job)
		 │
┌─────────────────────────────────────────────────────────────────┐
│              External News APIs                                  │
│                                                                  │
│  ├─ https://newsapi.com/v2/everything?...  (Finance News)      │
│  └─ https://newsapi.com/v2/top-headlines?... (Today's News)    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Data Processing Pipeline

```
External API → Parse JSON → Thread-Safe Cache → Image Scraping
									  ↓
							  HtmlAgilityPack
									  ↓
						   Extract Image URL
									  ↓
							Enrich Article JSON
									  ↓
							  Serialize to JSON
									  ↓
					Save Single Record to Database
									  ↓
					(FinanceNewsArticles Table)
									  ↓
				  Client: GET /api/FinanceNews
									  ↓
						 Parse JSON Array
									  ↓
					Apply Search Filter
									  ↓
					Sort (images first)
									  ↓
					Paginate Results
									  ↓
					Return to Client
```

---

## Key Services & Their Responsibilities

### Service Summary Matrix

| Service | Layer | Responsibility | Key Methods |
|---------|-------|-----------------|------------|
| **AuthService** | Infrastructure | User authentication, registration, token management | LoginAsync, RegisterAsync, RefreshAccessTokenAsync, ValidateAccessToken |
| **AdminService** | Infrastructure | User & role management | AssignRoleAsync, RevokeRoleAsync, DeactivateUserAsync |
| **AuditService** | Infrastructure | Audit logging for compliance | LogAsync, GetAuditLogsAsync, GetUserAuditAsync |
| **NewsProcessingService** | Infrastructure | News fetching, parsing, enrichment | FetchNewsAsync, ProcessNewsAsync, ScrapeImageAsync, SaveNewsAsync |
| **BannerFetchService** | Infrastructure | Image extraction from URLs | FetchBannerAsync, GetImageUrl, ValidateImageUrl |
| **JwtTokenGenerator** | Infrastructure/Security | JWT token creation | GenerateToken, ValidateToken |
| **PasswordHasher** | Infrastructure/Security | Password hashing & verification | HashPassword, VerifyPassword |
| **AuthorizationService** | Infrastructure/Security | Role-based authorization | CheckUserRole, HasPermission |

### Service Interaction Diagram

```
AuthController
	 ↓
IAuthService (AuthService)
	 ├→ IPasswordHasher
	 ├→ IJwtTokenGenerator
	 ├→ RefreshTokenGenerator
	 ├→ AppDbContext (Users, Roles, RefreshTokens)
	 └→ IAuditService (audit login)

AdminController
	 ↓
IAdminService (AdminService)
	 ├→ AppDbContext (Users, Roles)
	 └→ IAuditService (audit admin actions)

FinanceNewsController
	 ↓
AppDbContext (FinanceNewsArticles)
	 └─ (Data filled by NewsDataUpdateService)

NewsDataUpdateService
	 ↓
INewsProcessingService (NewsProcessingService)
	 ├→ HttpClientFactory (external news API)
	 ├→ IBannerFetchService (image extraction)
	 ├→ HtmlAgilityPack (HTML parsing)
	 ├→ AppDbContext (save articles)
	 └→ ILogger (error tracking)
```

---

## Entity Relationships & Flows

### User Registration Flow

```
Client POST /api/auth/register
	↓
AuthController.Register()
	↓
AuthService.RegisterAsync()
	├─ Check email uniqueness
	├─ Check username uniqueness
	├─ Get default "User" role
	├─ Hash password (PasswordHasher.BCrypt)
	├─ Create User entity
	├─ Save to DB (DbContext.Users.Add)
	├─ Call AuthenticateAsync()
	│   ├─ Generate JWT token (IJwtTokenGenerator)
	│   ├─ Generate refresh token (RefreshTokenGenerator)
	│   ├─ Save refresh token to DB
	│   └─ Return AuthenticationResult
	├─ Audit log (IAuditService)
	└─ Return AuthenticationResult

Client receives token and refresh token
```

### Request Authorization Flow

```
Client sends request with Authorization header
	↓
ASP.NET Core Middleware intercepts
	↓
JwtBearerHandler validates JWT
	├─ Extract token from Authorization: Bearer <token>
	├─ Verify signature with secret key
	├─ Check issuer matches config
	├─ Check audience matches config
	├─ Verify token not expired
	└─ Extract claims
		├─ Subject (UserId)
		├─ Username
		├─ Email
		└─ Role
	↓
ClaimsPrincipal created and attached to HttpContext.User
	↓
Controller action with [Authorize] attribute checks claims
	├─ If valid → Execute controller action
	└─ If invalid → Return 401 Unauthorized
```

### News Update Flow

```
Scheduled Job (NewsDataUpdateService.Main)
	↓
For each news source (Finance + Today):
	├─ Build API URL with query params
	├─ Call INewsProcessingService.FetchNewsAsync()
	│   ├─ HttpClient GET request
	│   ├─ Parse JSON response
	│   └─ Return articles array
	│
	├─ Call INewsProcessingService.ProcessNewsAsync()
	│   ├─ For each article (concurrent, max 10):
	│   │   ├─ Check _imageUrlCache for article URL
	│   │   ├─ If not cached:
	│   │   │   └─ Call IBannerFetchService.FetchBannerAsync(url)
	│   │   │       ├─ HttpClient GET of article URL
	│   │   │       ├─ Parse HTML (HtmlAgilityPack)
	│   │   │       ├─ Extract image URL from og:image, img src, etc.
	│   │   │       └─ Return image URL
	│   │   ├─ Add image URL to _imageUrlCache
	│   │   └─ Enrich article JSON with image URL
	│   └─ Return enriched articles
	│
	├─ Call INewsProcessingService.SaveNewsAsync()
	│   ├─ Serialize articles to JSON string
	│   ├─ Create/Update FinanceNewsArticle record
	│   │   ├─ Set JsonData = serialized articles
	│   │   ├─ Set ArticleCount = number of articles
	│   │   └─ Set CreatedAt = DateTime.UtcNow
	│   ├─ Save to database
	│   └─ Log success
	│
	└─ Call INewsProcessingService.CleanupOldNews()
		├─ Query articles older than RetentionDays (7)
		├─ Delete old records
		└─ Free database space
```

---

## Configuration Summary

### Key Configuration Settings

```json
{
  "ConnectionStrings": {
	"DefaultConnection": "Server=localhost;Database=FinancialAppDb;Integrated Security=true;"
  },

  "Jwt": {
	"Key": "YourSuperSecretKeyAtLeast32Characters!",
	"Issuer": "FinancialApp",
	"Audience": "FinancialAppClient",
	"ExpireMinutes": 60,
	"RefreshTokenExpireDays": 7
  },

  "NewsService": {
	"ApiUrl": "https://newsapi.org/v2/everything?q=stock&sortBy=publishedAt&language=en",
	"ApiKey": "your-newsapi-key",
	"RetentionDays": 7,
	"MaxConcurrentScrapes": 10
  },

  "NewsConfigApi2": {
	"ApiUrl": "https://newsapi.org/v2/top-headlines?country=us",
	"ApiKey": "your-newsapi-key2"
  },

  "Logging": {
	"LogLevel": {
	  "Default": "Information"
	}
  }
}
```

---

## Quick Reference Guide

### Common API Endpoints

```
# Authentication
POST   /api/auth/register           - Register new user
POST   /api/auth/login              - Login user
POST   /api/auth/refresh-token      - Refresh JWT
POST   /api/auth/logout             - Logout user
GET    /api/auth/check-auth         - Check authentication status
POST   /api/auth/validate-token     - Validate JWT token

# Admin
POST   /api/admin/assign-role       - Assign role to user
POST   /api/admin/revoke-role       - Revoke user role
DELETE /api/admin/deactivate-user   - Deactivate user

# News
GET    /api/FinanceNews             - Get finance news
GET    /api/TodayNews               - Get today's news
GET    /api/Blog                    - Get blog articles

# Query Parameters
?page=1                    - Page number (1-based)
?pageSize=10              - Items per page
?search=keyword           - Search within articles
```

### HTTP Status Codes

| Code | Meaning |
|------|---------|
| 200 | Success |
| 201 | Created |
| 400 | Bad Request (validation error) |
| 401 | Unauthorized (invalid/missing token) |
| 403 | Forbidden (insufficient permissions) |
| 404 | Not Found |
| 409 | Conflict (duplicate email/username) |
| 500 | Server Error |

### Environment Setup

```powershell
# Clone repository
git clone https://github.com/anub1004/FinancialApplication.git
cd FinancialApplication

# Install dependencies
dotnet restore

# Apply migrations
dotnet ef database update -p FinancialApplication.Infrastructure

# Run API
cd FinancialApplication.Api
dotnet run

# Run background service
cd NewsDataUpdateService
dotnet run

# Run tests
cd FinancialApplication.Tests
dotnet test
```

---

## Known Issues & Technical Debt

| Issue | Location | Severity | Note |
|-------|----------|----------|------|
| Duplicate file | `Domain/Entity/Transaction.cs.cs` | Low | File naming issue - has extra `.cs` |
| Image cache not distributed | `NewsProcessingService.cs` | Medium | Uses in-memory cache; consider Redis for production |
| No request rate limiting | `Program.cs` | Medium | Should implement rate limiting middleware |
| No API request caching | Controllers | Low | Consider response caching for news endpoints |
| Limited error handling | Various | Medium | Some services lack comprehensive error handling |

---

## Future Enhancements

```
1. Frontend Integration
   ├─ React client application
   ├─ WebSocket real-time updates
   └─ Progress notifications

2. Performance Optimization
   ├─ Image CDN integration
   ├─ Redis caching (distributed)
   ├─ Database query optimization
   └─ Async image processing queue

3. Feature Expansion
   ├─ Portfolio analysis
   ├─ Price alerting
   ├─ Social sharing
   ├─ Mobile app (React Native)
   └─ Advanced analytics

4. Security Hardening
   ├─ Two-factor authentication
   ├─ API rate limiting
   ├─ Request signing
   └─ Enhanced audit logging

5. DevOps & Deployment
   ├─ Docker containerization
   ├─ Kubernetes orchestration
   ├─ CI/CD pipeline
   ├─ Azure App Service deployment
   ├─ Automated testing
   └─ Performance monitoring
```

---

## Support & Maintenance

- **Repository**: https://github.com/anub1004/FinancialApplication
- **Branch**: dev
- **Framework**: .NET 8 LTS
- **Database**: SQL Server 2019+
- **Documentation Updated**: [Current Date]
- **Last Code Review**: [To be updated]

---

**End of Documentation**

---

## Document Information

- **Created**: 2024
- **Last Updated**: 2024
- **Version**: 1.0
- **Status**: Complete
- **Author**: Architecture & Technical Documentation
- **Target Audience**: Developers, DevOps Engineers, Technical Architects

