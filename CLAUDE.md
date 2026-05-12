# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview
GMT (Gestor Modular Tecnológico) is a web application for the TecNM Campus Acapulco liaison department. It manages student authentication and bureaucratic processes.

## Technology Stack
- **Framework**: ASP.NET Core 8.0 (MVC)
- **Database**: PostgreSQL (AWS RDS)
- **ORM**: Entity Framework Core with Npgsql provider
- **Architecture**: Traditional MVC pattern with Controllers, Models, Views

## Development Commands

### Building & Running
```bash
# Build the project
dotnet build

# Run the application
dotnet run
# Access at https://localhost:5001

# Run in development mode
dotnet watch run
```

### Database Operations
```bash
# Migrations (if needed)
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Generate DbContext from database
dotnet ef dbcontext scaffold "ConnectionString" Npgsql.EntityFrameworkCore.PostgreSQL
```

### Testing
```bash
# Run all tests
dotnet test

# Run specific test
dotnet test --filter TestName

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Code Architecture

### Core Structure
- **Controllers/**: HTTP request handlers (AccountController for auth, HomeController for main pages)
- **Models/**: Entity Framework entities mapped to PostgreSQL tables
  - `Login`: Authentication credentials with institutional email and password hash
  - `Alumno`: Student records with academic info and foreign key to Login
- **Data/**: Database context and configuration
- **Views/**: Razor templates for rendering HTML
- **Pages/**: Razor Pages for error and privacy pages

### Authentication Flow
1. User submits email/password to `AccountController.Login()` (POST)
2. System queries `Logins` table for matching institutional email
3. Password validation (currently plain text, needs hashing)
4. On success: redirect to `HomeController.Index()`
5. On failure: return to login with error message

### Database Configuration
- Connection string in `appsettings.json` under "DefaultConnection"
- PostgreSQL table names use snake_case (configured via `[Table]` attributes)
- Entity relationships defined with foreign keys and navigation properties

## Key Files
- `Program.cs`: Application startup and routing configuration
- `GMT.csproj`: Project dependencies (Npgsql EF Core)
- `ApplicationDbContext.cs`: EF Core context with DbSets for Login and Alumno
- `AccountController.cs`: Authentication logic and login flow

## Important Notes
- Password storage uses plain text currently - implement hashing before production
- Routing pattern: `{controller=Account}/{action=Login}/{id?}`
- Development environment uses HTTPS redirection
- AWS PostgreSQL connection configured in dependency injection

# GMT Project Guidelines (Windows 11)

## Environment
- OS: Windows 11
- Path Style: Always use backslashes `\` for file paths.
- Project Type: ASP.NET Core MVC (.NET 8)

## Project Structure
- Controllers: `Controllers\`
- Models: `Models\` (Verify if it is `Models\` or `ViewModels\`)
- Views: `Views\`
- Services: `Services\`
- Database: `Data\`

## Operational Rules
1. **Always** run `dir /s /b` or `tree /f` before searching for files if location is uncertain.
2. **Editing:** Use `write_to_file` for complete file rewrites to avoid patch errors on Windows CRLF line endings.
3. **Build:** Always verify changes with `dotnet build`.
4. **Git:** Always check `git status` before committing.