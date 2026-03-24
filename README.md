# SnapSearch — File Search & Preview System
**BABSLAI · Release 1.0 · .NET 8 WPF · Dapper · SQL Express · Clean Architecture**

---

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Solution Structure](#solution-structure)
3. [Database Setup](#database-setup)
4. [Configuration](#configuration)
5. [Build & Run](#build--run)
6. [Default Credentials](#default-credentials)
7. [User Roles & Permissions](#user-roles--permissions)
8. [NAS / Network Path Setup](#nas--network-path-setup)
9. [Key NuGet Packages](#key-nuget-packages)
10. [Troubleshooting](#troubleshooting)

---

## Prerequisites

| Requirement | Version |
|---|---|
| Visual Studio | 2022 (17.x) |
| .NET SDK | 8.0 |
| SQL Server Express | 2019 or 2022 |
| Windows OS | 7 SP1 or later (WPF) |

---

## Solution Structure

```
SnapSearch/
├── src/
│   ├── SnapSearch.Domain/              # Entities, Enums
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── AccessLog.cs
│   │   │   ├── AppSetting.cs
│   │   │   └── SearchHistory.cs
│   │   └── Enums/
│   │       ├── UserRole.cs
│   │       └── ActionType.cs
│   │
│   ├── SnapSearch.Application/         # Use cases, Service interfaces + implementations
│   │   ├── Common/
│   │   │   ├── Helpers/               # PasswordHelper, NetworkHelper
│   │   │   └── Profiles/              # AutoMapper MappingProfile
│   │   ├── Contracts/Infrastructure/  # Repository + service interfaces
│   │   ├── DTOs/                      # All data transfer objects
│   │   ├── Features/                  # AuthService, UserService, SearchService,
│   │   │                              #   AccessLogService, SettingsService
│   │   └── Services/                  # Service interfaces
│   │
│   ├── SnapSearch.Infrastructure/      # Dapper repos, FileSearchService
│   │   ├── Configurations/
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── UnitOfWork.cs
│   │   │   └── Repositories/          # UserRepository, AccessLogRepository,
│   │   │                              #   AppSettingRepository, SearchHistoryRepository
│   │   └── Services/
│   │       └── FileSearchService.cs   # File system search + PDF/DOCX content parsing
│   │
│   └── SnapSearch.Presentation/       # WPF .NET 8
│       ├── Common/
│       │   ├── BaseViewModel.cs
│       │   ├── RelayCommand.cs        # RelayCommand + AsyncRelayCommand
│       │   ├── SessionContext.cs      # Singleton current-user holder
│       │   └── Converters/            # All IValueConverter implementations
│       ├── Themes/
│       │   ├── Colors.xaml            # Dark navy colour palette
│       │   ├── Styles.xaml            # Full WPF control styles
│       │   └── Converters.xaml        # Converter resource dictionary
│       ├── View/
│       │   ├── LoginWindow.xaml(.cs)
│       │   ├── MainShellWindow.xaml(.cs)
│       │   ├── SearchView.xaml(.cs)
│       │   ├── FilePreviewWindow.xaml(.cs)
│       │   ├── UserManagementView.xaml(.cs)
│       │   ├── AccessLogView.xaml(.cs)
│       │   └── SettingsView.xaml(.cs)
│       └── ViewModels/
│           ├── LoginViewModel.cs
│           ├── MainShellViewModel.cs
│           ├── SearchViewModel.cs
│           ├── FilePreviewViewModel.cs
│           ├── UserManagementViewModel.cs
│           ├── AccessLogViewModel.cs
│           └── SettingsViewModel.cs
│
└── Database/
    └── SnapSearch_Schema.sql          # All tables + 20 stored procedures
```

---

## Database Setup

### 1. Open SQL Server Management Studio (SSMS)

Connect to your SQL Express instance:
```
Server: .\SQLEXPRESS
Authentication: Windows Authentication
```

### 2. Run the Schema Script

Open and execute the full script:
```
Database/SnapSearch_Schema.sql
```

This script will:
- Create the `SnapSearchDb` database (if it doesn't exist)
- Create all 4 tables: `Users`, `AccessLogs`, `AppSettings`, `SearchHistory`
- Create all 20 stored procedures (all parameterised, no triggers, no blob types)
- Insert the default **admin** user

### 3. Verify

Run the following to confirm:
```sql
USE SnapSearchDb;
SELECT * FROM Users;
SELECT name FROM sys.procedures ORDER BY name;
```

You should see 1 user row and 20 stored procedures.

---

## Configuration

### `appsettings.json` (Development / Local)

Located at:
```
SnapSearch.Presentation/appsettings.json
```

Default connection string:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=SnapSearchDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### `appsettings.Production.json` (NAS / Server)

Update the server name to your NAS SQL Express instance:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=NAS_SERVER\\SQLEXPRESS;Database=SnapSearchDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Set the environment variable before running in production:
```cmd
set DOTNET_ENVIRONMENT=Production
```

---

## Build & Run

### Using Visual Studio 2022

1. Open `SnapSearch.sln`
2. Set **SnapSearch.Presentation** as the Startup Project
3. Select **Debug** or **Release** configuration
4. Press `F5` to build and run

### Using .NET CLI

```bash
cd SnapSearch/src/SnapSearch.Presentation
dotnet restore
dotnet build
dotnet run
```

---

## Default Credentials

| Username | Password   | Role  |
|----------|-----------|-------|
| `admin`  | `Admin@123` | Admin |

> **Important:** Change the admin password immediately after first login via the User Management screen.

The password hash stored in the database is a SHA-256 hash of the plain text password. The `PasswordHelper.Hash()` method in `SnapSearch.Application.Common.Helpers` handles all hashing.

---

## User Roles & Permissions

| Permission | Admin | ViewListOnly | ViewerOnly | ViewAndPrint | Compliance |
|---|:---:|:---:|:---:|:---:|:---:|
| Search files | ✅ | ✅ | ✅ | ✅ | ✅ |
| View file list | ✅ | ✅ | ✅ | ✅ | ✅ |
| Preview file contents | ✅ | ❌ | ✅ | ✅ | ✅ |
| Print file | ✅ | ❌ | ❌ | ✅ | ❌ |
| Export / Copy / Save | ✅ | ❌ | ❌ | ❌ | ✅ |
| Manage users | ✅ | ❌ | ❌ | ❌ | ❌ |
| View access logs | ✅ | ❌ | ❌ | ❌ | ❌ |
| Change settings | ✅ | ❌ | ❌ | ❌ | ❌ |

**Role descriptions:**
- **Admin** — Full access: manage users, view logs, change settings, all file operations
- **ViewListOnly** — Can search and see file names/paths only; cannot open or preview files
- **ViewerOnly** — Can search and preview files; cannot print or export
- **ViewAndPrint** — Can search, preview, and print files
- **Compliance** — Can search, preview, export, copy, and save files (for audit/export purposes)

---

## NAS / Network Path Setup

SnapSearch is designed to search files stored on a **NAS (Network Attached Storage)** device.

### Configure the Default Search Directory

1. Log in as **Admin**
2. Navigate to **Settings**
3. Set the **Default Search Directory** to your NAS UNC path, e.g.:
   ```
   \\NAS_SERVER\SharedDocuments
   ```
4. Click **Save Settings**

### Ensure Network Access

- The Windows account running SnapSearch must have **read access** to the NAS share
- For production deployments, consider running the app under a domain service account with appropriate share permissions
- UNC paths (`\\server\share`) and mapped drives (e.g., `Z:\`) are both supported

### File Types Supported for Content Search

| Format | Content Search | Preview |
|--------|:---:|:---:|
| `.txt`, `.log`, `.csv` | ✅ | ✅ |
| `.xml`, `.json`, `.md` | ✅ | ✅ |
| `.ini`, `.cfg`, `.bat`, `.ps1` | ✅ | ✅ |
| `.pdf` | ✅ (via PdfPig) | ✅ (text) |
| `.docx`, `.doc` | ✅ (via OpenXml) | ✅ (text) |
| `.xlsx`, `.xls`, `.pptx` | ❌ | ❌ (name search only) |

---

## Key NuGet Packages

| Package | Version | Purpose |
|---|---|---|
| `Dapper` | 2.1.35 | Micro-ORM for all SQL queries via stored procedures |
| `Microsoft.Data.SqlClient` | 5.2.1 | SQL Server connection |
| `AutoMapper` | 13.0.1 | Entity ↔ DTO mapping |
| `DocumentFormat.OpenXml` | 3.0.2 | DOCX content search |
| `PdfPig` | 0.1.9 | PDF content search & text extraction |
| `Microsoft.Extensions.DependencyInjection` | 8.0.0 | DI container |
| `Microsoft.Extensions.Configuration.Json` | 8.0.0 | appsettings.json loading |

---

## Troubleshooting

### "Cannot connect to database"
- Verify SQL Express is running: `services.msc` → SQL Server (SQLEXPRESS)
- Confirm the connection string server name matches your instance
- Ensure Windows Authentication is enabled on the SQL instance

### "Stored procedure not found"
- Re-run `SnapSearch_Schema.sql` in SSMS
- Verify you are connected to `SnapSearchDb` (not `master`)

### "Access to path is denied" during file search
- The Windows user account running SnapSearch does not have read access to the search directory
- Right-click the folder → Properties → Security → Add the user account

### "Login failed" with correct password
- The admin password hash in the seed data matches `Admin@123`
- If you changed the password directly in SQL, re-hash it using:
  ```csharp
  PasswordHelper.Hash("YourNewPassword")
  ```
  Then update the `PasswordHash` column in the `Users` table

### PDF content search returns no matches
- Confirm `PdfPig` NuGet package is installed in `SnapSearch.Infrastructure`
- Scanned PDFs (image-only) cannot be text-searched; only PDFs with embedded text are supported

### DOCX content search returns no matches
- Confirm `DocumentFormat.OpenXml` NuGet package is installed in `SnapSearch.Infrastructure`
- Password-protected DOCX files cannot be opened and will be silently skipped

---

## Access Log Fields Captured

Every search, file view, print, export, login, and logout is recorded with:

| Field | Description |
|---|---|
| Username | Who performed the action |
| Action | Login / Logout / Search / ViewFile / PrintFile / ExportFile / CopyFile / SaveFile |
| SearchKeyword | The keyword used (for Search actions) |
| FilePath | The file accessed (for file actions) |
| IP Address | Local machine IP at time of action |
| MAC Address | Network adapter MAC address |
| Timestamp | UTC datetime of the action |
| Details | Additional context (e.g., result count, export destination) |

---

*SnapSearch · BABSLAI · Release 1.0 · Built with .NET 8, WPF, Dapper, Clean Architecture*
