IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260522053541_InitialMigration'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(50) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260522053541_InitialMigration'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] uniqueidentifier NOT NULL,
        [Username] nvarchar(50) NOT NULL,
        [Password] nvarchar(255) NOT NULL,
        [Email] nvarchar(100) NOT NULL,
        [RoleId] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Users_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260522053541_InitialMigration'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [AuditLogId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Action] nvarchar(50) NOT NULL,
        [EntityName] nvarchar(100) NOT NULL,
        [EntityId] nvarchar(255) NOT NULL,
        [Timestamp] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([AuditLogId]),
        CONSTRAINT [FK_AuditLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260522053541_InitialMigration'
)
BEGIN
    CREATE TABLE [Goals] (
        [GoalId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Title] nvarchar(255) NOT NULL,
        [TargetAmount] decimal(18,2) NOT NULL,
        [CurrentAmount] decimal(18,2) NOT NULL,
        [Deadline] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Goals] PRIMARY KEY ([GoalId]),
        CONSTRAINT [FK_Goals_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260522053541_InitialMigration'
)
BEGIN
    CREATE TABLE [Investments] (
        [InvestmentId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [InvestmentType] nvarchar(50) NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Investments] PRIMARY KEY ([InvestmentId]),
        CONSTRAINT [FK_Investments_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260522053541_InitialMigration'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [RefreshTokenId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Token] nvarchar(500) NOT NULL,
        [ExpiryDate] datetime2 NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([RefreshTokenId]),
        CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260522053541_InitialMigration'
)
BEGIN
    CREATE TABLE [Transactions] (
        [TransactionId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Category] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [TransactionDate] datetime2 NOT NULL,
        [TransactionType] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Transactions] PRIMARY KEY ([TransactionId]),
        CONSTRAINT [FK_Transactions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260522053541_InitialMigration'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'IsActive', N'Name') AND [object_id] = OBJECT_ID(N'[Roles]'))
        SET IDENTITY_INSERT [Roles] ON;
    EXEC(N'INSERT INTO [Roles] ([Id], [IsActive], [Name])
    VALUES (1, CAST(1 AS bit), N''User''),
    (2, CAST(1 AS bit), N''Admin''),
    (3, CAST(1 AS bit), N''Manager''),
    (4, CAST(1 AS bit), N''Auditor'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'IsActive', N'Name') AND [object_id] = OBJECT_ID(N'[Roles]'))
        SET IDENTITY_INSERT [Roles] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260522053541_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260522053541_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Goals_UserId] ON [Goals] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260522053541_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Investments_UserId] ON [Investments] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260522053541_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260522053541_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Roles_Name] ON [Roles] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260522053541_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Transactions_UserId] ON [Transactions] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260522053541_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260522053541_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Users_RoleId] ON [Users] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260522053541_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260522053541_InitialMigration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260522053541_InitialMigration', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626111453_AddNewsArticleTable'
)
BEGIN
    CREATE TABLE [NewsArticles] (
        [Id] int NOT NULL IDENTITY,
        [JsonData] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_NewsArticles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626111453_AddNewsArticleTable'
)
BEGIN
    CREATE INDEX [IX_NewsArticles_CreatedAt] ON [NewsArticles] ([CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626111453_AddNewsArticleTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260626111453_AddNewsArticleTable', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629093827_AddTodayNewsArticleTable'
)
BEGIN
    CREATE TABLE [TodayNewsArticles] (
        [Id] int NOT NULL IDENTITY,
        [JsonData] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_TodayNewsArticles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629093827_AddTodayNewsArticleTable'
)
BEGIN
    CREATE INDEX [IX_TodayNewsArticles_CreatedAt] ON [TodayNewsArticles] ([CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629093827_AddTodayNewsArticleTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260629093827_AddTodayNewsArticleTable', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630073244_AddSeparateNewsTables'
)
BEGIN
    DROP TABLE [NewsArticles];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630073244_AddSeparateNewsTables'
)
BEGIN
    CREATE TABLE [FinanceNewsArticles] (
        [Id] int NOT NULL IDENTITY,
        [JsonData] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_FinanceNewsArticles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630073244_AddSeparateNewsTables'
)
BEGIN
    CREATE INDEX [IX_FinanceNewsArticles_CreatedAt] ON [FinanceNewsArticles] ([CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630073244_AddSeparateNewsTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260630073244_AddSeparateNewsTables', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702064859_AddArticleCountColumn'
)
BEGIN
    ALTER TABLE [TodayNewsArticles] ADD [ArticleCount] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702064859_AddArticleCountColumn'
)
BEGIN
    ALTER TABLE [FinanceNewsArticles] ADD [ArticleCount] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702064859_AddArticleCountColumn'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260702064859_AddArticleCountColumn', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713104528_AddGoogleAuth'
)
BEGIN
    ALTER TABLE [Users] ADD [GoogleId] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713104528_AddGoogleAuth'
)
BEGIN
    ALTER TABLE [Users] ADD [ProfilePicture] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713104528_AddGoogleAuth'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Users_GoogleId] ON [Users] ([GoogleId]) WHERE [GoogleId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713104528_AddGoogleAuth'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260713104528_AddGoogleAuth', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715071917_AddTotpFields'
)
BEGIN
    ALTER TABLE [Users] ADD [IsTotpConfigured] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715071917_AddTotpFields'
)
BEGIN
    ALTER TABLE [Users] ADD [TotpSecret] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715071917_AddTotpFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260715071917_AddTotpFields', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715104417_AddRecoveryCodes'
)
BEGIN
    CREATE TABLE [RecoveryCodes] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [CodeHash] nvarchar(64) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UsedAt] datetime2 NULL,
        CONSTRAINT [PK_RecoveryCodes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RecoveryCodes_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715104417_AddRecoveryCodes'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RecoveryCodes_UserId_CodeHash] ON [RecoveryCodes] ([UserId], [CodeHash]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715104417_AddRecoveryCodes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260715104417_AddRecoveryCodes', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715111202_AddEmailLoginCodes'
)
BEGIN
    CREATE TABLE [EmailLoginCodes] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [CodeHash] nvarchar(64) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [UsedAt] datetime2 NULL,
        CONSTRAINT [PK_EmailLoginCodes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmailLoginCodes_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715111202_AddEmailLoginCodes'
)
BEGIN
    CREATE INDEX [IX_EmailLoginCodes_UserId_CodeHash] ON [EmailLoginCodes] ([UserId], [CodeHash]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715111202_AddEmailLoginCodes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260715111202_AddEmailLoginCodes', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE TABLE [Features] (
        [Id] uniqueidentifier NOT NULL,
        [FeatureKey] nvarchar(100) NOT NULL,
        [DisplayName] nvarchar(200) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Category] nvarchar(100) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [SortOrder] int NOT NULL DEFAULT 0,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Features] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE TABLE [Plans] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Slug] nvarchar(100) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [MonthlyPrice] decimal(18,2) NOT NULL,
        [AnnualPrice] decimal(18,2) NULL,
        [Currency] nvarchar(10) NOT NULL DEFAULT N'INR',
        [SortOrder] int NOT NULL DEFAULT 0,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [IsDefault] bit NOT NULL DEFAULT CAST(0 AS bit),
        [TrialDays] int NOT NULL DEFAULT 0,
        [MaxUsers] int NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Plans] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE TABLE [FeatureAudits] (
        [Id] uniqueidentifier NOT NULL,
        [FeatureId] uniqueidentifier NOT NULL,
        [Action] nvarchar(50) NOT NULL,
        [OldValues] nvarchar(max) NULL,
        [NewValues] nvarchar(max) NULL,
        [PerformedBy] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_FeatureAudits] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FeatureAudits_Features_FeatureId] FOREIGN KEY ([FeatureId]) REFERENCES [Features] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE TABLE [PlanAudits] (
        [Id] uniqueidentifier NOT NULL,
        [PlanId] uniqueidentifier NOT NULL,
        [Action] nvarchar(50) NOT NULL,
        [OldValues] nvarchar(max) NULL,
        [NewValues] nvarchar(max) NULL,
        [PerformedBy] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_PlanAudits] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PlanAudits_Plans_PlanId] FOREIGN KEY ([PlanId]) REFERENCES [Plans] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE TABLE [PlanFeatures] (
        [Id] uniqueidentifier NOT NULL,
        [PlanId] uniqueidentifier NOT NULL,
        [FeatureId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_PlanFeatures] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PlanFeatures_Features_FeatureId] FOREIGN KEY ([FeatureId]) REFERENCES [Features] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PlanFeatures_Plans_PlanId] FOREIGN KEY ([PlanId]) REFERENCES [Plans] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE TABLE [PlanPriceHistories] (
        [Id] uniqueidentifier NOT NULL,
        [PlanId] uniqueidentifier NOT NULL,
        [MonthlyPrice] decimal(18,2) NOT NULL,
        [AnnualPrice] decimal(18,2) NULL,
        [EffectiveFrom] datetime2 NOT NULL,
        [EffectiveTo] datetime2 NULL,
        [ChangedBy] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_PlanPriceHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PlanPriceHistories_Plans_PlanId] FOREIGN KEY ([PlanId]) REFERENCES [Plans] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE TABLE [UserSubscriptions] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [PlanId] uniqueidentifier NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [BillingCycle] nvarchar(20) NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NOT NULL,
        [TrialEndDate] datetime2 NULL,
        [NextRenewalDate] datetime2 NULL,
        [CancelledAt] datetime2 NULL,
        [CancelReason] nvarchar(500) NULL,
        [AutoRenew] bit NOT NULL DEFAULT CAST(1 AS bit),
        [ScheduledPlanId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_UserSubscriptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserSubscriptions_Plans_PlanId] FOREIGN KEY ([PlanId]) REFERENCES [Plans] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserSubscriptions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE TABLE [Payments] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [SubscriptionId] uniqueidentifier NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Currency] nvarchar(10) NOT NULL DEFAULT N'INR',
        [Status] nvarchar(20) NOT NULL,
        [PaymentMethod] nvarchar(50) NULL,
        [TransactionRef] nvarchar(200) NULL,
        [GatewayResponse] nvarchar(max) NULL,
        [PaidAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Payments_UserSubscriptions_SubscriptionId] FOREIGN KEY ([SubscriptionId]) REFERENCES [UserSubscriptions] ([Id]),
        CONSTRAINT [FK_Payments_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE TABLE [SubscriptionHistories] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [SubscriptionId] uniqueidentifier NOT NULL,
        [Action] nvarchar(50) NOT NULL,
        [FromPlanId] uniqueidentifier NULL,
        [ToPlanId] uniqueidentifier NULL,
        [Notes] nvarchar(500) NULL,
        [PerformedBy] nvarchar(50) NOT NULL DEFAULT N'System',
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_SubscriptionHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SubscriptionHistories_UserSubscriptions_SubscriptionId] FOREIGN KEY ([SubscriptionId]) REFERENCES [UserSubscriptions] ([Id]),
        CONSTRAINT [FK_SubscriptionHistories_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE TABLE [Invoices] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [PaymentId] uniqueidentifier NULL,
        [InvoiceNumber] nvarchar(50) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Tax] decimal(18,2) NOT NULL DEFAULT 0.0,
        [TotalAmount] decimal(18,2) NOT NULL,
        [Currency] nvarchar(10) NOT NULL DEFAULT N'INR',
        [Status] nvarchar(20) NOT NULL,
        [IssuedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [DueDate] datetime2 NOT NULL,
        [PaidAt] datetime2 NULL,
        CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Invoices_Payments_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [Payments] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Invoices_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAt', N'Description', N'DisplayName', N'FeatureKey', N'IsActive', N'SortOrder', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Features]'))
        SET IDENTITY_INSERT [Features] ON;
    EXEC(N'INSERT INTO [Features] ([Id], [Category], [CreatedAt], [Description], [DisplayName], [FeatureKey], [IsActive], [SortOrder], [UpdatedAt])
    VALUES (''b0000000-0000-0000-0000-000000000001'', N''Core'', ''2026-01-01T00:00:00.0000000Z'', N''Main financial dashboard with overview and widgets.'', N''Dashboard'', N''dashboard'', CAST(1 AS bit), 1, ''2026-01-01T00:00:00.0000000Z''),
    (''b0000000-0000-0000-0000-000000000002'', N''Core'', ''2026-01-01T00:00:00.0000000Z'', N''Track income and expense transactions.'', N''Transactions'', N''transactions'', CAST(1 AS bit), 2, ''2026-01-01T00:00:00.0000000Z''),
    (''b0000000-0000-0000-0000-000000000003'', N''Core'', ''2026-01-01T00:00:00.0000000Z'', N''Access curated financial news articles.'', N''Financial News'', N''news'', CAST(1 AS bit), 3, ''2026-01-01T00:00:00.0000000Z''),
    (''b0000000-0000-0000-0000-000000000004'', N''Core'', ''2026-01-01T00:00:00.0000000Z'', N''Manage user profile and account settings.'', N''Profile Management'', N''profile'', CAST(1 AS bit), 4, ''2026-01-01T00:00:00.0000000Z''),
    (''b0000000-0000-0000-0000-000000000005'', N''Core'', ''2026-01-01T00:00:00.0000000Z'', N''Configure 2FA, recovery codes, and login security.'', N''Security Settings'', N''security_settings'', CAST(1 AS bit), 5, ''2026-01-01T00:00:00.0000000Z''),
    (''b0000000-0000-0000-0000-000000000006'', N''Core'', ''2026-01-01T00:00:00.0000000Z'', N''Guided setup and onboarding experience.'', N''Onboarding'', N''onboarding'', CAST(1 AS bit), 6, ''2026-01-01T00:00:00.0000000Z''),
    (''b0000000-0000-0000-0000-000000000007'', N''Analytics'', ''2026-01-01T00:00:00.0000000Z'', N''Basic financial analytics and charts.'', N''Analytics'', N''analytics'', CAST(1 AS bit), 7, ''2026-01-01T00:00:00.0000000Z''),
    (''b0000000-0000-0000-0000-000000000008'', N''Investments'', ''2026-01-01T00:00:00.0000000Z'', N''Monitor and manage investment portfolio.'', N''Investment Tracking'', N''investment_tracking'', CAST(1 AS bit), 8, ''2026-01-01T00:00:00.0000000Z''),
    (''b0000000-0000-0000-0000-000000000009'', N''Finance'', ''2026-01-01T00:00:00.0000000Z'', N''Manage payment cards and linked accounts.'', N''Cards Management'', N''cards'', CAST(1 AS bit), 9, ''2026-01-01T00:00:00.0000000Z''),
    (''b0000000-0000-0000-0000-000000000010'', N''Reports'', ''2026-01-01T00:00:00.0000000Z'', N''Generate financial reports and summaries.'', N''Reports'', N''reports'', CAST(1 AS bit), 10, ''2026-01-01T00:00:00.0000000Z''),
    (''b0000000-0000-0000-0000-000000000011'', N''Reports'', ''2026-01-01T00:00:00.0000000Z'', N''Export reports and data as PDF documents.'', N''Export PDF'', N''export_pdf'', CAST(1 AS bit), 11, ''2026-01-01T00:00:00.0000000Z''),
    (''b0000000-0000-0000-0000-000000000012'', N''Reports'', ''2026-01-01T00:00:00.0000000Z'', N''Export data as CSV files for spreadsheet use.'', N''Export CSV'', N''export_csv'', CAST(1 AS bit), 12, ''2026-01-01T00:00:00.0000000Z''),
    (''b0000000-0000-0000-0000-000000000013'', N''Analytics'', ''2026-01-01T00:00:00.0000000Z'', N''Advanced analytics with trend analysis and predictions.'', N''Premium Analytics'', N''premium_analytics'', CAST(1 AS bit), 13, ''2026-01-01T00:00:00.0000000Z''),
    (''b0000000-0000-0000-0000-000000000014'', N''AI'', ''2026-01-01T00:00:00.0000000Z'', N''AI-powered financial insights and recommendations.'', N''AI Suggestions'', N''ai_suggestions'', CAST(1 AS bit), 14, ''2026-01-01T00:00:00.0000000Z''),
    (''b0000000-0000-0000-0000-000000000015'', N''Admin'', ''2026-01-01T00:00:00.0000000Z'', N''Admin: manage users, roles, and permissions.'', N''User Management'', N''user_management'', CAST(1 AS bit), 15, ''2026-01-01T00:00:00.0000000Z'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAt', N'Description', N'DisplayName', N'FeatureKey', N'IsActive', N'SortOrder', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Features]'))
        SET IDENTITY_INSERT [Features] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AnnualPrice', N'CreatedAt', N'Currency', N'Description', N'IsActive', N'IsDefault', N'MaxUsers', N'MonthlyPrice', N'Name', N'Slug', N'SortOrder', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Plans]'))
        SET IDENTITY_INSERT [Plans] ON;
    EXEC(N'INSERT INTO [Plans] ([Id], [AnnualPrice], [CreatedAt], [Currency], [Description], [IsActive], [IsDefault], [MaxUsers], [MonthlyPrice], [Name], [Slug], [SortOrder], [UpdatedAt])
    VALUES (''a0000000-0000-0000-0000-000000000001'', 0.0, ''2026-01-01T00:00:00.0000000Z'', N''INR'', N''Get started with essential financial tools at no cost.'', CAST(1 AS bit), CAST(1 AS bit), NULL, 0.0, N''Free'', N''free'', 1, ''2026-01-01T00:00:00.0000000Z'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AnnualPrice', N'CreatedAt', N'Currency', N'Description', N'IsActive', N'IsDefault', N'MaxUsers', N'MonthlyPrice', N'Name', N'Slug', N'SortOrder', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Plans]'))
        SET IDENTITY_INSERT [Plans] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AnnualPrice', N'CreatedAt', N'Currency', N'Description', N'IsActive', N'MaxUsers', N'MonthlyPrice', N'Name', N'Slug', N'SortOrder', N'TrialDays', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Plans]'))
        SET IDENTITY_INSERT [Plans] ON;
    EXEC(N'INSERT INTO [Plans] ([Id], [AnnualPrice], [CreatedAt], [Currency], [Description], [IsActive], [MaxUsers], [MonthlyPrice], [Name], [Slug], [SortOrder], [TrialDays], [UpdatedAt])
    VALUES (''a0000000-0000-0000-0000-000000000002'', 4999.0, ''2026-01-01T00:00:00.0000000Z'', N''INR'', N''Essential features for personal finance management.'', CAST(1 AS bit), NULL, 499.0, N''Basic'', N''basic'', 2, 7, ''2026-01-01T00:00:00.0000000Z''),
    (''a0000000-0000-0000-0000-000000000003'', 9999.0, ''2026-01-01T00:00:00.0000000Z'', N''INR'', N''Advanced analytics and reporting for serious investors.'', CAST(1 AS bit), NULL, 999.0, N''Advanced'', N''advanced'', 3, 14, ''2026-01-01T00:00:00.0000000Z''),
    (''a0000000-0000-0000-0000-000000000004'', 14999.0, ''2026-01-01T00:00:00.0000000Z'', N''INR'', N''Full access to all features including AI-powered insights and premium support.'', CAST(1 AS bit), NULL, 1499.0, N''Pro'', N''pro'', 4, 14, ''2026-01-01T00:00:00.0000000Z'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AnnualPrice', N'CreatedAt', N'Currency', N'Description', N'IsActive', N'MaxUsers', N'MonthlyPrice', N'Name', N'Slug', N'SortOrder', N'TrialDays', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Plans]'))
        SET IDENTITY_INSERT [Plans] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'FeatureId', N'PlanId') AND [object_id] = OBJECT_ID(N'[PlanFeatures]'))
        SET IDENTITY_INSERT [PlanFeatures] ON;
    EXEC(N'INSERT INTO [PlanFeatures] ([Id], [CreatedAt], [FeatureId], [PlanId])
    VALUES (''c0000000-0000-0000-0001-000000000001'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000001'', ''a0000000-0000-0000-0000-000000000001''),
    (''c0000000-0000-0000-0001-000000000002'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000002'', ''a0000000-0000-0000-0000-000000000001''),
    (''c0000000-0000-0000-0001-000000000003'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000003'', ''a0000000-0000-0000-0000-000000000001''),
    (''c0000000-0000-0000-0001-000000000004'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000004'', ''a0000000-0000-0000-0000-000000000001''),
    (''c0000000-0000-0000-0001-000000000005'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000005'', ''a0000000-0000-0000-0000-000000000001''),
    (''c0000000-0000-0000-0001-000000000006'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000006'', ''a0000000-0000-0000-0000-000000000001''),
    (''c0000000-0000-0000-0002-000000000001'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000001'', ''a0000000-0000-0000-0000-000000000002''),
    (''c0000000-0000-0000-0002-000000000002'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000002'', ''a0000000-0000-0000-0000-000000000002''),
    (''c0000000-0000-0000-0002-000000000003'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000003'', ''a0000000-0000-0000-0000-000000000002''),
    (''c0000000-0000-0000-0002-000000000004'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000004'', ''a0000000-0000-0000-0000-000000000002''),
    (''c0000000-0000-0000-0002-000000000005'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000005'', ''a0000000-0000-0000-0000-000000000002''),
    (''c0000000-0000-0000-0002-000000000006'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000006'', ''a0000000-0000-0000-0000-000000000002''),
    (''c0000000-0000-0000-0002-000000000007'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000007'', ''a0000000-0000-0000-0000-000000000002''),
    (''c0000000-0000-0000-0002-000000000008'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000008'', ''a0000000-0000-0000-0000-000000000002''),
    (''c0000000-0000-0000-0002-000000000009'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000009'', ''a0000000-0000-0000-0000-000000000002''),
    (''c0000000-0000-0000-0003-000000000001'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000001'', ''a0000000-0000-0000-0000-000000000003''),
    (''c0000000-0000-0000-0003-000000000002'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000002'', ''a0000000-0000-0000-0000-000000000003''),
    (''c0000000-0000-0000-0003-000000000003'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000003'', ''a0000000-0000-0000-0000-000000000003''),
    (''c0000000-0000-0000-0003-000000000004'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000004'', ''a0000000-0000-0000-0000-000000000003''),
    (''c0000000-0000-0000-0003-000000000005'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000005'', ''a0000000-0000-0000-0000-000000000003''),
    (''c0000000-0000-0000-0003-000000000006'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000006'', ''a0000000-0000-0000-0000-000000000003''),
    (''c0000000-0000-0000-0003-000000000007'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000007'', ''a0000000-0000-0000-0000-000000000003''),
    (''c0000000-0000-0000-0003-000000000008'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000008'', ''a0000000-0000-0000-0000-000000000003''),
    (''c0000000-0000-0000-0003-000000000009'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000009'', ''a0000000-0000-0000-0000-000000000003''),
    (''c0000000-0000-0000-0003-000000000010'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000010'', ''a0000000-0000-0000-0000-000000000003''),
    (''c0000000-0000-0000-0003-000000000011'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000011'', ''a0000000-0000-0000-0000-000000000003''),
    (''c0000000-0000-0000-0003-000000000012'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000012'', ''a0000000-0000-0000-0000-000000000003''),
    (''c0000000-0000-0000-0003-000000000013'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000013'', ''a0000000-0000-0000-0000-000000000003''),
    (''c0000000-0000-0000-0004-000000000001'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000001'', ''a0000000-0000-0000-0000-000000000004''),
    (''c0000000-0000-0000-0004-000000000002'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000002'', ''a0000000-0000-0000-0000-000000000004''),
    (''c0000000-0000-0000-0004-000000000003'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000003'', ''a0000000-0000-0000-0000-000000000004''),
    (''c0000000-0000-0000-0004-000000000004'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000004'', ''a0000000-0000-0000-0000-000000000004''),
    (''c0000000-0000-0000-0004-000000000005'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000005'', ''a0000000-0000-0000-0000-000000000004''),
    (''c0000000-0000-0000-0004-000000000006'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000006'', ''a0000000-0000-0000-0000-000000000004''),
    (''c0000000-0000-0000-0004-000000000007'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000007'', ''a0000000-0000-0000-0000-000000000004''),
    (''c0000000-0000-0000-0004-000000000008'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000008'', ''a0000000-0000-0000-0000-000000000004''),
    (''c0000000-0000-0000-0004-000000000009'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000009'', ''a0000000-0000-0000-0000-000000000004''),
    (''c0000000-0000-0000-0004-000000000010'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000010'', ''a0000000-0000-0000-0000-000000000004''),
    (''c0000000-0000-0000-0004-000000000011'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000011'', ''a0000000-0000-0000-0000-000000000004''),
    (''c0000000-0000-0000-0004-000000000012'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000012'', ''a0000000-0000-0000-0000-000000000004''),
    (''c0000000-0000-0000-0004-000000000013'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000013'', ''a0000000-0000-0000-0000-000000000004''),
    (''c0000000-0000-0000-0004-000000000014'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000014'', ''a0000000-0000-0000-0000-000000000004'');
    INSERT INTO [PlanFeatures] ([Id], [CreatedAt], [FeatureId], [PlanId])
    VALUES (''c0000000-0000-0000-0004-000000000015'', ''2026-01-01T00:00:00.0000000Z'', ''b0000000-0000-0000-0000-000000000015'', ''a0000000-0000-0000-0000-000000000004'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'FeatureId', N'PlanId') AND [object_id] = OBJECT_ID(N'[PlanFeatures]'))
        SET IDENTITY_INSERT [PlanFeatures] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AnnualPrice', N'ChangedBy', N'CreatedAt', N'EffectiveFrom', N'EffectiveTo', N'MonthlyPrice', N'PlanId') AND [object_id] = OBJECT_ID(N'[PlanPriceHistories]'))
        SET IDENTITY_INSERT [PlanPriceHistories] ON;
    EXEC(N'INSERT INTO [PlanPriceHistories] ([Id], [AnnualPrice], [ChangedBy], [CreatedAt], [EffectiveFrom], [EffectiveTo], [MonthlyPrice], [PlanId])
    VALUES (''d0000000-0000-0000-0000-000000000001'', 0.0, ''00000000-0000-0000-0000-000000000000'', ''2026-01-01T00:00:00.0000000Z'', ''2026-01-01T00:00:00.0000000Z'', NULL, 0.0, ''a0000000-0000-0000-0000-000000000001''),
    (''d0000000-0000-0000-0000-000000000002'', 4999.0, ''00000000-0000-0000-0000-000000000000'', ''2026-01-01T00:00:00.0000000Z'', ''2026-01-01T00:00:00.0000000Z'', NULL, 499.0, ''a0000000-0000-0000-0000-000000000002''),
    (''d0000000-0000-0000-0000-000000000003'', 9999.0, ''00000000-0000-0000-0000-000000000000'', ''2026-01-01T00:00:00.0000000Z'', ''2026-01-01T00:00:00.0000000Z'', NULL, 999.0, ''a0000000-0000-0000-0000-000000000003''),
    (''d0000000-0000-0000-0000-000000000004'', 14999.0, ''00000000-0000-0000-0000-000000000000'', ''2026-01-01T00:00:00.0000000Z'', ''2026-01-01T00:00:00.0000000Z'', NULL, 1499.0, ''a0000000-0000-0000-0000-000000000004'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AnnualPrice', N'ChangedBy', N'CreatedAt', N'EffectiveFrom', N'EffectiveTo', N'MonthlyPrice', N'PlanId') AND [object_id] = OBJECT_ID(N'[PlanPriceHistories]'))
        SET IDENTITY_INSERT [PlanPriceHistories] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_FeatureAudits_FeatureId] ON [FeatureAudits] ([FeatureId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_Features_Category] ON [Features] ([Category]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Features_FeatureKey] ON [Features] ([FeatureKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_Features_IsActive] ON [Features] ([IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Invoices_InvoiceNumber] ON [Invoices] ([InvoiceNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_Invoices_PaymentId] ON [Invoices] ([PaymentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_Invoices_UserId] ON [Invoices] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_Payments_CreatedAt] ON [Payments] ([CreatedAt] DESC);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_Payments_Status] ON [Payments] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_Payments_SubscriptionId] ON [Payments] ([SubscriptionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_Payments_UserId] ON [Payments] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_PlanAudits_PlanId] ON [PlanAudits] ([PlanId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_PlanFeatures_FeatureId] ON [PlanFeatures] ([FeatureId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_PlanFeatures_PlanId] ON [PlanFeatures] ([PlanId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PlanFeatures_PlanId_FeatureId] ON [PlanFeatures] ([PlanId], [FeatureId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_PlanPriceHistories_PlanId] ON [PlanPriceHistories] ([PlanId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_Plans_IsActive] ON [Plans] ([IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Plans_Name] ON [Plans] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Plans_Slug] ON [Plans] ([Slug]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_Plans_SortOrder] ON [Plans] ([SortOrder]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_SubscriptionHistories_CreatedAt] ON [SubscriptionHistories] ([CreatedAt] DESC);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_SubscriptionHistories_SubscriptionId] ON [SubscriptionHistories] ([SubscriptionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_SubscriptionHistories_UserId] ON [SubscriptionHistories] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_UserSubscriptions_EndDate] ON [UserSubscriptions] ([EndDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_UserSubscriptions_PlanId] ON [UserSubscriptions] ([PlanId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_UserSubscriptions_Status] ON [UserSubscriptions] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_UserSubscriptions_UserId] ON [UserSubscriptions] ([UserId]) WHERE [Status] IN (''Active'', ''Trial'')');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
                    INSERT INTO [UserSubscriptions] ([Id], [UserId], [PlanId], [Status], [BillingCycle], [StartDate], [EndDate], [AutoRenew], [CreatedAt], [UpdatedAt])
                    SELECT 
                        NEWID(),
                        u.[Id],
                        'A0000000-0000-0000-0000-000000000001',
                        'Active',
                        'Lifetime',
                        SYSUTCDATETIME(),
                        '2099-12-31T23:59:59',
                        0,
                        SYSUTCDATETIME(),
                        SYSUTCDATETIME()
                    FROM [Users] u
                    WHERE NOT EXISTS (
                        SELECT 1 FROM [UserSubscriptions] us WHERE us.[UserId] = u.[Id]
                    )
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722071052_AddSubscriptionSystem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260722071052_AddSubscriptionSystem', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825063950_AddBannerTable'
)
BEGIN
    CREATE TABLE [Banners] (
        [Id] uniqueidentifier NOT NULL,
        [CompressedImage] varbinary(max) NOT NULL,
        [ContentType] nvarchar(50) NOT NULL DEFAULT N'image/jpeg',
        [OriginalUrl] nvarchar(2048) NOT NULL,
        [SourcePageUrl] nvarchar(2048) NULL,
        [Title] nvarchar(500) NULL,
        [Description] nvarchar(2000) NULL,
        [OriginalSizeBytes] bigint NOT NULL,
        [CompressedSizeBytes] bigint NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Banners] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825063950_AddBannerTable'
)
BEGIN
    CREATE INDEX [IX_Banners_OriginalUrl] ON [Banners] ([OriginalUrl]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825063950_AddBannerTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260825063950_AddBannerTable', N'8.0.0');
END;
GO

COMMIT;
GO

