-- ============================================================================
-- SQL Server Database Creation Script for IT Repair Service
-- ============================================================================
-- This script creates the complete database schema including:
--   - Identity tables (AspNetUsers, AspNetRoles, AspNetRoleClaims,
--     AspNetUserClaims, AspNetUserLogins, AspNetUserTokens, AspNetUserRoles)
--   - Application tables (RepairTickets, RepairTicketStatusHistories, NewsItems)
-- ============================================================================

-- Create the database if it does not exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'ITService')
BEGIN
    CREATE DATABASE [ITService];
END
GO

USE [ITService];
GO

-- ============================================================================
-- ASP.NET IDENTITY TABLES
-- ============================================================================

-- AspNetRoles
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetRoles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AspNetRoles] (
        [Id]               NVARCHAR(450)  NOT NULL,
        [Name]             NVARCHAR(256)  NULL,
        [NormalizedName]   NVARCHAR(256)  NULL,
        [ConcurrencyStamp] NVARCHAR(MAX)  NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

-- AspNetUsers
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AspNetUsers] (
        [Id]                   NVARCHAR(450)  NOT NULL,
        [UserName]             NVARCHAR(256)  NULL,
        [NormalizedUserName]   NVARCHAR(256)  NULL,
        [Email]                NVARCHAR(256)  NULL,
        [NormalizedEmail]      NVARCHAR(256)  NULL,
        [EmailConfirmed]       BIT            NOT NULL DEFAULT 0,
        [PasswordHash]         NVARCHAR(MAX)  NULL,
        [SecurityStamp]        NVARCHAR(MAX)  NULL,
        [ConcurrencyStamp]     NVARCHAR(MAX)  NULL,
        [PhoneNumber]          NVARCHAR(MAX)  NULL,
        [PhoneNumberConfirmed] BIT            NOT NULL DEFAULT 0,
        [TwoFactorEnabled]     BIT            NOT NULL DEFAULT 0,
        [LockoutEnd]           DATETIMEOFFSET NULL,
        [LockoutEnabled]       BIT            NOT NULL DEFAULT 0,
        [AccessFailedCount]    INT            NOT NULL DEFAULT 0,

        -- ApplicationUser custom fields
        [FullName]             NVARCHAR(MAX)  NOT NULL DEFAULT N'',
        [Department]           NVARCHAR(MAX)  NOT NULL DEFAULT N'',
        [Section]              NVARCHAR(MAX)  NOT NULL DEFAULT N'',
        [Title]                NVARCHAR(MAX)  NOT NULL DEFAULT N'',
        [Company]              NVARCHAR(MAX)  NOT NULL DEFAULT N'',
        [Manager]              NVARCHAR(MAX)  NOT NULL DEFAULT N'',
        [TelephoneNumber]      NVARCHAR(MAX)  NOT NULL DEFAULT N'',
        [EmployeeID]           NVARCHAR(MAX)  NOT NULL DEFAULT N'',
        [IsActive]             BIT            NOT NULL DEFAULT 1,
        [MustChangePassword]   BIT            NOT NULL DEFAULT 0,
        [CreatedByName]        NVARCHAR(120)  NULL,
        [UpdatedByName]        NVARCHAR(120)  NULL,
        [CreatedAt]            DATETIME2(7)   NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt]            DATETIME2(7)   NULL,

        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

-- AspNetRoleClaims
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetRoleClaims]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AspNetRoleClaims] (
        [Id]         INT            IDENTITY (1, 1) NOT NULL,
        [RoleId]     NVARCHAR(450)  NOT NULL,
        [ClaimType]  NVARCHAR(MAX)  NULL,
        [ClaimValue] NVARCHAR(MAX)  NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId])
            REFERENCES [dbo].[AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END
GO

-- AspNetUserClaims
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserClaims]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AspNetUserClaims] (
        [Id]         INT            IDENTITY (1, 1) NOT NULL,
        [UserId]     NVARCHAR(450)  NOT NULL,
        [ClaimType]  NVARCHAR(MAX)  NULL,
        [ClaimValue] NVARCHAR(MAX)  NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId])
            REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END
GO

-- AspNetUserLogins
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserLogins]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AspNetUserLogins] (
        [LoginProvider]       NVARCHAR(450)  NOT NULL,
        [ProviderKey]         NVARCHAR(450)  NOT NULL,
        [ProviderDisplayName] NVARCHAR(MAX)  NULL,
        [UserId]              NVARCHAR(450)  NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY CLUSTERED ([LoginProvider] ASC, [ProviderKey] ASC),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId])
            REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END
GO

-- AspNetUserTokens
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserTokens]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AspNetUserTokens] (
        [UserId]        NVARCHAR(450)  NOT NULL,
        [LoginProvider] NVARCHAR(450)  NOT NULL,
        [Name]          NVARCHAR(450)  NOT NULL,
        [Value]         NVARCHAR(MAX)  NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY CLUSTERED ([UserId] ASC, [LoginProvider] ASC, [Name] ASC),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId])
            REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END
GO

-- AspNetUserRoles
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserRoles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AspNetUserRoles] (
        [UserId] NVARCHAR(450) NOT NULL,
        [RoleId] NVARCHAR(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY CLUSTERED ([UserId] ASC, [RoleId] ASC),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId])
            REFERENCES [dbo].[AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId])
            REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END
GO

-- ============================================================================
-- APPLICATION TABLES
-- ============================================================================

-- RepairTickets
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RepairTickets]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RepairTickets] (
        [Id]                   INT              IDENTITY (1, 1) NOT NULL,
        [RequesterName]        NVARCHAR(100)    NOT NULL,
        [Department]           NVARCHAR(100)    NOT NULL,
        [DeviceName]           NVARCHAR(150)    NOT NULL,
        [IssueDescription]     NVARCHAR(500)    NOT NULL,
        [RepairType]           NVARCHAR(MAX)    NOT NULL,
        [DriveAccessDepartment] NVARCHAR(100)   NULL,
        [Priority]             NVARCHAR(MAX)    NOT NULL,
        [Status]               NVARCHAR(MAX)    NOT NULL,
        [CreatedAt]            DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
        [CreatedByName]        NVARCHAR(120)    NULL,
        [UpdatedAt]            DATETIME2(7)     NULL,
        [UpdatedByName]        NVARCHAR(120)    NULL,
        [ApproverDepartment]   NVARCHAR(100)    NOT NULL DEFAULT N'',
        [ApproverUserId]       NVARCHAR(450)    NOT NULL DEFAULT N'',
        [ApproverName]         NVARCHAR(150)    NOT NULL DEFAULT N'',
        [SecondApproverUserId] NVARCHAR(450)    NULL,
        [SecondApproverName]   NVARCHAR(150)    NULL,
        [ThirdApproverUserId]  NVARCHAR(450)    NULL,
        [ThirdApproverName]    NVARCHAR(150)    NULL,
        [ApprovalLevel]        INT              NOT NULL DEFAULT 1,
        [AssignedItUserId]     NVARCHAR(450)    NULL,
        [AssignedItName]       NVARCHAR(150)    NULL,
        [RequesterUserId]      NVARCHAR(450)    NULL,
        [DocumentNo]           NVARCHAR(20)     NULL,

        CONSTRAINT [PK_RepairTickets] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

-- RepairTicketStatusHistories
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RepairTicketStatusHistories]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RepairTicketStatusHistories] (
        [Id]             INT              IDENTITY (1, 1) NOT NULL,
        [RepairTicketId] INT              NOT NULL,
        [FromStatus]     NVARCHAR(MAX)    NULL,
        [ToStatus]       NVARCHAR(MAX)    NOT NULL,
        [Action]         NVARCHAR(50)     NOT NULL DEFAULT N'',
        [Remark]         NVARCHAR(250)    NULL,
        [ChangedAt]      DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
        [ChangedByUserId] NVARCHAR(450)   NULL,
        [ChangedByName]  NVARCHAR(120)    NULL,

        CONSTRAINT [PK_RepairTicketStatusHistories] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_RepairTicketStatusHistories_RepairTickets_RepairTicketId] FOREIGN KEY ([RepairTicketId])
            REFERENCES [dbo].[RepairTickets] ([Id]) ON DELETE CASCADE
    );
END
GO

-- NewsItems
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[NewsItems]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[NewsItems] (
        [Id]                INT              IDENTITY (1, 1) NOT NULL,
        [Title]             NVARCHAR(160)    NOT NULL,
        [Content]           NVARCHAR(4000)   NOT NULL,
        [CreatedByName]     NVARCHAR(120)    NULL,
        [UpdatedByName]     NVARCHAR(120)    NULL,
        [AttachmentFileName] NVARCHAR(260)   NULL,
        [AttachmentUrl]     NVARCHAR(500)    NULL,
        [IsActive]          BIT              NOT NULL DEFAULT 1,
        [CreatedAt]         DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt]         DATETIME2(7)     NULL,

        CONSTRAINT [PK_NewsItems] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

-- ============================================================================
-- INDEXES
-- ============================================================================

-- Identity indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'RoleNameIndex' AND object_id = OBJECT_ID(N'[dbo].[AspNetRoles]'))
    CREATE UNIQUE NONCLUSTERED INDEX [RoleNameIndex] ON [dbo].[AspNetRoles] ([NormalizedName] ASC) WHERE [NormalizedName] IS NOT NULL;

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'EmailIndex' AND object_id = OBJECT_ID(N'[dbo].[AspNetUsers]'))
    CREATE NONCLUSTERED INDEX [EmailIndex] ON [dbo].[AspNetUsers] ([NormalizedEmail] ASC);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'UserNameIndex' AND object_id = OBJECT_ID(N'[dbo].[AspNetUsers]'))
    CREATE UNIQUE NONCLUSTERED INDEX [UserNameIndex] ON [dbo].[AspNetUsers] ([NormalizedUserName] ASC) WHERE [NormalizedUserName] IS NOT NULL;

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_AspNetRoleClaims_RoleId' AND object_id = OBJECT_ID(N'[dbo].[AspNetRoleClaims]'))
    CREATE NONCLUSTERED INDEX [IX_AspNetRoleClaims_RoleId] ON [dbo].[AspNetRoleClaims] ([RoleId] ASC);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_AspNetUserClaims_UserId' AND object_id = OBJECT_ID(N'[dbo].[AspNetUserClaims]'))
    CREATE NONCLUSTERED INDEX [IX_AspNetUserClaims_UserId] ON [dbo].[AspNetUserClaims] ([UserId] ASC);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_AspNetUserLogins_UserId' AND object_id = OBJECT_ID(N'[dbo].[AspNetUserLogins]'))
    CREATE NONCLUSTERED INDEX [IX_AspNetUserLogins_UserId] ON [dbo].[AspNetUserLogins] ([UserId] ASC);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_AspNetUserRoles_RoleId' AND object_id = OBJECT_ID(N'[dbo].[AspNetUserRoles]'))
    CREATE NONCLUSTERED INDEX [IX_AspNetUserRoles_RoleId] ON [dbo].[AspNetUserRoles] ([RoleId] ASC);

-- Status history index for efficient querying by ticket + date
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_RepairTicketStatusHistories_RepairTicketId_ChangedAt' AND object_id = OBJECT_ID(N'[dbo].[RepairTicketStatusHistories]'))
    CREATE NONCLUSTERED INDEX [IX_RepairTicketStatusHistories_RepairTicketId_ChangedAt]
        ON [dbo].[RepairTicketStatusHistories] ([RepairTicketId] ASC, [ChangedAt] ASC);

-- ============================================================================
-- SEED DATA: Default roles
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetRoles] WHERE [Name] = N'User')
    INSERT INTO [dbo].[AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
    VALUES (NEWID(), N'User', N'USER', NEWID());

IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetRoles] WHERE [Name] = N'ITSupport')
    INSERT INTO [dbo].[AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
    VALUES (NEWID(), N'ITSupport', N'ITSUPPORT', NEWID());

IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetRoles] WHERE [Name] = N'Approve')
    INSERT INTO [dbo].[AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
    VALUES (NEWID(), N'Approve', N'APPROVE', NEWID());

IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetRoles] WHERE [Name] = N'Admin')
    INSERT INTO [dbo].[AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
    VALUES (NEWID(), N'Admin', N'ADMIN', NEWID());

GO

PRINT N'Database script completed successfully.';
GO