-- =============================================
-- SnapSearch Database Schema + Stored Procedures
-- SQL Express / SQL Server
-- =============================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'SnapSearchDb')
    CREATE DATABASE SnapSearchDb;
GO

USE SnapSearchDb;
GO

-- =============================================
-- TABLES
-- =============================================

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
CREATE TABLE Users (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    Username     NVARCHAR(100)  NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256)  NOT NULL,
    Role         NVARCHAR(50)   NOT NULL DEFAULT 'ViewerOnly',
    IsActive     BIT            NOT NULL DEFAULT 1,
    CreatedAt    DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt    DATETIME2      NULL
);
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AccessLogs' AND xtype='U')
CREATE TABLE AccessLogs (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    UserId        INT            NULL,
    Username      NVARCHAR(100)  NOT NULL,
    Action        NVARCHAR(50)   NOT NULL,
    FilePath      NVARCHAR(1000) NULL,
    SearchKeyword NVARCHAR(500)  NULL,
    IpAddress     NVARCHAR(50)   NOT NULL DEFAULT '',
    MacAddress    NVARCHAR(50)   NOT NULL DEFAULT '',
    AccessedAt    DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    Details       NVARCHAR(2000) NULL
);
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AppSettings' AND xtype='U')
CREATE TABLE AppSettings (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    [Key]       NVARCHAR(200)  NOT NULL UNIQUE,
    [Value]     NVARCHAR(2000) NOT NULL,
    Description NVARCHAR(500)  NULL,
    UpdatedAt   DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SearchHistory' AND xtype='U')
CREATE TABLE SearchHistory (
    Id                  INT IDENTITY(1,1) PRIMARY KEY,
    UserId              INT            NOT NULL,
    Keyword             NVARCHAR(500)  NOT NULL,
    SearchDirectory     NVARCHAR(1000) NULL,
    FileExtensionFilter NVARCHAR(50)   NULL,
    ResultCount         INT            NOT NULL DEFAULT 0,
    SearchedAt          DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
GO

-- Default admin seed
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
    -- Password: Admin@123  (SHA256 hash)
    INSERT INTO Users (Username, PasswordHash, Role, IsActive, CreatedAt)
    VALUES ('admin', '6G94qKPK8LYNjnTllCqm2G3BUM08AzOK7yW30tfjrMc=', 'Admin', 1, GETUTCDATE());
GO

-- =============================================
-- STORED PROCEDURES - Users
-- =============================================

CREATE OR ALTER PROCEDURE sp_GetUserById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Username, PasswordHash, Role, IsActive, CreatedAt, UpdatedAt
    FROM Users
    WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_GetUserByUsername
    @Username NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Username, PasswordHash, Role, IsActive, CreatedAt, UpdatedAt
    FROM Users
    WHERE Username = @Username;
END
GO

CREATE OR ALTER PROCEDURE sp_GetAllUsers
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Username, PasswordHash, Role, IsActive, CreatedAt, UpdatedAt
    FROM Users
    ORDER BY Username;
END
GO

CREATE OR ALTER PROCEDURE sp_CreateUser
    @Username     NVARCHAR(100),
    @PasswordHash NVARCHAR(256),
    @Role         NVARCHAR(50),
    @IsActive     BIT,
    @CreatedAt    DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Users (Username, PasswordHash, Role, IsActive, CreatedAt)
    VALUES (@Username, @PasswordHash, @Role, @IsActive, @CreatedAt);
    SELECT SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE sp_UpdateUser
    @Id           INT,
    @Username     NVARCHAR(100),
    @PasswordHash NVARCHAR(256),
    @Role         NVARCHAR(50),
    @IsActive     BIT,
    @UpdatedAt    DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Users
    SET Username     = @Username,
        PasswordHash = @PasswordHash,
        Role         = @Role,
        IsActive     = @IsActive,
        UpdatedAt    = @UpdatedAt
    WHERE Id = @Id;
    SELECT @@ROWCOUNT;
END
GO

CREATE OR ALTER PROCEDURE sp_DeleteUser
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Users SET IsActive = 0 WHERE Id = @Id;
    SELECT @@ROWCOUNT;
END
GO

CREATE OR ALTER PROCEDURE sp_AuthenticateUser
    @Username     NVARCHAR(100),
    @PasswordHash NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Username, PasswordHash, Role, IsActive, CreatedAt, UpdatedAt
    FROM Users
    WHERE Username = @Username
      AND PasswordHash = @PasswordHash
      AND IsActive = 1;
END
GO

-- =============================================
-- STORED PROCEDURES - AccessLogs
-- =============================================

CREATE OR ALTER PROCEDURE sp_CreateAccessLog
    @UserId       INT            = NULL,
    @Username     NVARCHAR(100),
    @Action       NVARCHAR(50),
    @FilePath     NVARCHAR(1000) = NULL,
    @SearchKeyword NVARCHAR(500) = NULL,
    @IpAddress    NVARCHAR(50),
    @MacAddress   NVARCHAR(50),
    @AccessedAt   DATETIME2,
    @Details      NVARCHAR(2000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO AccessLogs (UserId, Username, Action, FilePath, SearchKeyword, IpAddress, MacAddress, AccessedAt, Details)
    VALUES (@UserId, @Username, @Action, @FilePath, @SearchKeyword, @IpAddress, @MacAddress, @AccessedAt, @Details);
    SELECT SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE sp_GetAllAccessLogs
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 5000 Id, UserId, Username, Action, FilePath, SearchKeyword, IpAddress, MacAddress, AccessedAt, Details
    FROM AccessLogs
    ORDER BY AccessedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_GetAccessLogsByUserId
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, UserId, Username, Action, FilePath, SearchKeyword, IpAddress, MacAddress, AccessedAt, Details
    FROM AccessLogs
    WHERE UserId = @UserId
    ORDER BY AccessedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_GetAccessLogsByDateRange
    @From DATETIME2,
    @To   DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, UserId, Username, Action, FilePath, SearchKeyword, IpAddress, MacAddress, AccessedAt, Details
    FROM AccessLogs
    WHERE AccessedAt BETWEEN @From AND @To
    ORDER BY AccessedAt DESC;
END
GO

-- =============================================
-- STORED PROCEDURES - AppSettings
-- =============================================

CREATE OR ALTER PROCEDURE sp_GetSettingByKey
    @Key NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, [Key], [Value], Description, UpdatedAt
    FROM AppSettings
    WHERE [Key] = @Key;
END
GO

CREATE OR ALTER PROCEDURE sp_GetAllSettings
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, [Key], [Value], Description, UpdatedAt
    FROM AppSettings
    ORDER BY [Key];
END
GO

CREATE OR ALTER PROCEDURE sp_UpsertSetting
    @Key         NVARCHAR(200),
    @Value       NVARCHAR(2000),
    @Description NVARCHAR(500) = NULL,
    @UpdatedAt   DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM AppSettings WHERE [Key] = @Key)
        UPDATE AppSettings
        SET [Value] = @Value, Description = @Description, UpdatedAt = @UpdatedAt
        WHERE [Key] = @Key;
    ELSE
        INSERT INTO AppSettings ([Key], [Value], Description, UpdatedAt)
        VALUES (@Key, @Value, @Description, @UpdatedAt);
    SELECT @@ROWCOUNT;
END
GO

CREATE OR ALTER PROCEDURE sp_DeleteSetting
    @Key NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM AppSettings WHERE [Key] = @Key;
    SELECT @@ROWCOUNT;
END
GO

-- =============================================
-- STORED PROCEDURES - SearchHistory
-- =============================================

CREATE OR ALTER PROCEDURE sp_CreateSearchHistory
    @UserId              INT,
    @Keyword             NVARCHAR(500),
    @SearchDirectory     NVARCHAR(1000) = NULL,
    @FileExtensionFilter NVARCHAR(50)   = NULL,
    @ResultCount         INT,
    @SearchedAt          DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO SearchHistory (UserId, Keyword, SearchDirectory, FileExtensionFilter, ResultCount, SearchedAt)
    VALUES (@UserId, @Keyword, @SearchDirectory, @FileExtensionFilter, @ResultCount, @SearchedAt);
    SELECT SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE sp_GetSearchHistoryByUserId
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, UserId, Keyword, SearchDirectory, FileExtensionFilter, ResultCount, SearchedAt
    FROM SearchHistory
    WHERE UserId = @UserId
    ORDER BY SearchedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_GetAllSearchHistory
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, UserId, Keyword, SearchDirectory, FileExtensionFilter, ResultCount, SearchedAt
    FROM SearchHistory
    ORDER BY SearchedAt DESC;
END
GO