# Loto3000

A .NET 10 lottery homework project with SQL Server persistence, JWT Bearer authentication, a Bootstrap 5/vanilla JavaScript UI, transactional draws, a public winners board, and automated tests. This README covers running and testing the project locally.

## Features

- **Players** register, log in, pick seven unique numbers from 1–37 (manually or with **Quick pick**), and view their own ticket history with match/prize results.
- **Administrators** view active-session statistics and initiate a draw of eight cryptographically random numbers. The draw calculates every prize and opens the next session in a single transaction.
- **Winners Board** is public and lists session number, winner full name, matched numbers, prize, and draw date, newest first.
- **Security:** BCrypt password hashing, JWT Bearer tokens, role-based authorization, rate limiting, security headers/CSP, and sanitized error responses.
- **Tooling:** EF Core migrations, Swagger UI in Development, Serilog console/file logging, and xUnit tests.

## Contents

1. [Prerequisites and quick start](#1-prerequisites-and-quick-start)
2. [Architecture and package boundaries](#2-architecture-and-package-boundaries)
3. [SQL Server and migrations](#3-sql-server-and-migrations)
4. [Playing and drawing](#4-playing-and-drawing)
5. [API and Swagger](#5-api-and-swagger)
6. [Local configuration](#6-local-configuration)
7. [Verification](#7-verification)
8. [Troubleshooting](#8-troubleshooting)

## 1. Prerequisites and quick start

- .NET 10 SDK; `global.json` accepts the latest installed .NET 10 feature band.
- SQL Server with Windows Authentication; SQL Server 2019 or newer is recommended.
- The .NET CLI, or Visual Studio with ASP.NET/web tooling.
- Optional: SQL Server Management Studio (SSMS) to inspect the database.

Open a PowerShell terminal in the directory containing `Loto3000.sln`:

The commands below use SQL Server on `localhost`. If you use SQL Express or LocalDB, configure the connection first as described in [SQL Server and migrations](#3-sql-server-and-migrations).

```powershell
dotnet restore Loto3000.sln
dotnet tool restore
dotnet build Loto3000.sln -c Release
dotnet ef database update --project Loto3000.DataAccess --startup-project Loto3000.Web
dotnet test Loto3000.Tests -c Release --no-build
dotnet run --project Loto3000.Web --launch-profile http
```

Open **http://localhost:5100** for the SPA or **http://localhost:5100/swagger** for Swagger UI.

The checked-in Development configuration enables `Database:ApplyMigrationsOnStartup`, so the explicit `database update` step is optional with the development launch profiles. If you disable that setting, run the documented `database update` command before starting the app.

Development administrator on a fresh database: **`admin` / `Admin@123`**. Registration creates only `Player` accounts. On application startup the seeder creates the administrator if there are no users and session #1 if there are no sessions. Applying a migration alone creates the schema, not the seed data.

For local HTTPS:

```powershell
dotnet dev-certs https --trust
dotnet run --project Loto3000.Web --launch-profile https
```

Use **https://localhost:7100**. In Visual Studio, open `Loto3000.sln`, select `Loto3000.Web` as the startup project, select the `https` launch profile, and press F5.

## 2. Architecture and package boundaries

| Project               | Responsibility                                                              | Project references           |
| --------------------- | --------------------------------------------------------------------------- | ---------------------------- |
| `Loto3000.Domain`     | POCO entities, enums, business exception; zero NuGet packages               | None                         |
| `Loto3000.DataAccess` | EF Core mappings, SQL Server repositories, unit of work, migrations, seeder | Domain                       |
| `Loto3000.Services`   | DTOs, authentication, validation, prizes, draw workflow                     | DataAccess, Domain           |
| `Loto3000.Web`        | API controllers, middleware, composition, static SPA                        | Services, DataAccess, Domain |
| `Loto3000.Tests`      | xUnit business-rule, workflow, authentication, and mapping tests            | Services, Domain             |

Controllers inject service interfaces only. Business services inject repository interfaces, `IUnitOfWork`, and `ILogger<T>`. `AuthService` also receives immutable `JwtOptions` for token creation. No business service references `DbContext`.

Repositories and the unit of work share a scoped `Loto3000DbContext`. `AddDataAccessLayer` and `AddBusinessServices` keep registration modular. Password hashing is implemented only in Services; the DataAccess seeder receives a BCrypt hash and does not reference BCrypt.

### Solution layout

Exactly five projects target `net10.0`. DTOs and mappers are **folders inside Services**, not additional projects:

```text
Loto3000.sln
├── Loto3000.Domain/
│   ├── Models/             User, LotterySession, Ticket, Draw, Winner
│   ├── Enums/              UserRole, SessionStatus, PrizeType
│   └── Exceptions/         BusinessRuleException
├── Loto3000.DataAccess/
│   ├── Data/               Loto3000DbContext, DatabaseSeeder
│   ├── Interfaces/         Repository, IUnitOfWork and seeder contracts
│   ├── Implementations/    EF repositories and UnitOfWork
│   ├── Configurations/     Conversions, relationships, constraints, indexes
│   └── Migrations/         Initial schema, designer metadata and snapshot
├── Loto3000.Services/
│   ├── DTOs/
│   │   ├── Auth/           Login, registration, identity and JwtOptions
│   │   ├── Tickets/        Ticket requests and confirmations
│   │   ├── Draws/          Draw requests and results
│   │   ├── Winners/        Public board rows
│   │   └── Sessions/       Session statistics
│   ├── Mappers/            Static User, Ticket, Draw, Winner, Session mappers
│   ├── Interfaces/         Auth, Ticket, Draw and Winner service contracts
│   ├── Implementations/    Business workflows, no direct DbContext access
│   ├── Rules/              LotteryRules
│   └── Validation/         Request validation independent of MVC
├── Loto3000.Web/
│   ├── Controllers/        Thin HTTP adapters to service interfaces
│   ├── Infrastructure/     Database initialization, claims helpers, rate limits, design-time EF factory
│   ├── Middleware/         Safe problem responses and security headers
│   └── wwwroot/            index.html, app.js, styles.css, local Bootstrap
└── Loto3000.Tests/          Rule, workflow, architecture and mapper tests
```

Web references persistence for composition/startup; its controllers still use service interfaces. Tests reference Domain and Services directly and inspect persistence metadata through transitive references. DTOs avoid exposing tracked entities or private account fields; static mappers copy number lists so response mutations cannot change entities.

`Directory.Build.props` centrally enables nullable analysis, deterministic builds, `TreatWarningsAsErrors`, and `GenerateDocumentationFile`. Public APIs have XML documentation; missing documentation fails compilation rather than being suppressed. Swagger loads the generated Web and Services XML files. The initial migration and model snapshot are checked in.

### Direct NuGet references

| Project    | Package                                       | Version | Purpose                                                   |
| ---------- | --------------------------------------------- | ------- | --------------------------------------------------------- |
| DataAccess | Microsoft.EntityFrameworkCore.SqlServer       | 10.0.12 | SQL Server provider, LINQ queries and persistence         |
| DataAccess | Microsoft.EntityFrameworkCore.Tools           | 10.0.12 | Visual Studio Package Manager Console migration commands  |
| Services   | System.IdentityModel.Tokens.Jwt               | 8.23.0  | JWT creation and claim serialization                      |
| Services   | Microsoft.IdentityModel.Tokens                | 8.23.0  | Signing keys, credentials and token validation primitives |
| Services   | BCrypt.Net-Next                               | 4.2.0   | Salted adaptive password hashing and verification         |
| Web        | Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.12 | Bearer authentication middleware                          |
| Web        | Microsoft.EntityFrameworkCore.Design          | 10.0.12 | EF design-time factory and CLI migration infrastructure   |
| Web        | Serilog.AspNetCore                            | 10.0.0  | Host integration and structured request logging           |
| Web        | Serilog.Sinks.File                            | 7.0.0   | Daily rolling log files and retention                     |
| Web        | Swashbuckle.AspNetCore                        | 10.2.3  | OpenAPI generation and interactive Swagger UI             |
| Tests      | Microsoft.NET.Test.Sdk                        | 18.10.1 | Test host and discovery infrastructure                    |
| Tests      | xunit                                         | 2.9.3   | Fact/theory test framework                                |
| Tests      | xunit.runner.visualstudio                     | 4.0.0   | Test Explorer/VSTest adapter                              |
| Tests      | Moq                                           | 4.20.72 | Repository/UoW mocks and fault injection                  |
| Tests      | FluentAssertions                              | 7.2.2   | Readable assertions                                       |

EF tooling, EF Design, and the Visual Studio test runner are private assets. Runtime transitive dependencies are resolved normally; no application package is added to Domain. Bootstrap 5.3.8 CSS and its MIT license are vendored under `wwwroot/vendor/bootstrap`; the SPA needs neither npm nor a CDN.

Versions are pinned in the project files; `dotnet-tools.json` pins the local `dotnet-ef` tool. Check FluentAssertions licensing before upgrading from the pinned 7.2.2 version to 8.x.

## 3. SQL Server and migrations

The default connection string in `Loto3000.Web/appsettings.json` is:

```text
Server=localhost;Database=Loto3000Db;Trusted_Connection=True;TrustServerCertificate=True;
```

The Windows identity running the migration needs database creation/schema permissions. For SQL Express or LocalDB, set `ConnectionStrings__DefaultConnection` to the appropriate instance before running both the EF commands and the application. Configuration environment variables use a double underscore for nested keys.

For **LocalDB**, install SQL Server Express LocalDB (for example through the Visual Studio installer), then run in the same terminal:

```powershell
sqllocaldb info
sqllocaldb start MSSQLLocalDB
$env:ConnectionStrings__DefaultConnection = 'Server=(localdb)\MSSQLLocalDB;Database=Loto3000Db;Trusted_Connection=True;TrustServerCertificate=True;'
dotnet ef database update --project Loto3000.DataAccess --startup-project Loto3000.Web
dotnet run --project Loto3000.Web --launch-profile http
```

If the named LocalDB instance is absent, create it with `sqllocaldb create MSSQLLocalDB` before starting it. For SQL Express, use `Server=.\SQLEXPRESS` instead. Terminal process variables do not update an already-running Visual Studio process; configure the same connection string in the IDE launch environment or development configuration when using F5. Remove a terminal override with `Remove-Item Env:ConnectionStrings__DefaultConnection` when finished.

### Terminal workflow

An initial migration and its model snapshot are already included under `Loto3000.DataAccess/Migrations`.

```powershell
dotnet tool restore
dotnet ef migrations list --project Loto3000.DataAccess --startup-project Loto3000.Web
dotnet ef database update --project Loto3000.DataAccess --startup-project Loto3000.Web
dotnet ef migrations has-pending-model-changes --project Loto3000.DataAccess --startup-project Loto3000.Web
```

The initial migration was generated with `dotnet ef migrations add InitialCreate --project Loto3000.DataAccess --startup-project Loto3000.Web --output-dir Migrations`. Do not add a second `InitialCreate` migration to this solution.

The EF design-time factory reads the SQL connection configuration without needing JWT or bootstrap secrets and does not run the application seeder.

### Package Manager Console workflow

In Visual Studio, set `Loto3000.Web` as the startup project. Open **Tools > NuGet Package Manager > Package Manager Console** and use:

```powershell
Get-Migration -Project Loto3000.DataAccess -StartupProject Loto3000.Web
Update-Database -Project Loto3000.DataAccess -StartupProject Loto3000.Web
```

### Verify with SSMS

1. Connect to **Database Engine**, server **localhost** (or `(localdb)\MSSQLLocalDB` / `.\SQLEXPRESS`, matching configuration), using **Windows Authentication**. For a local self-signed SQL certificate, select **Trust server certificate** in connection options.
2. Refresh **Databases**, expand **Loto3000Db > Tables**.
3. Confirm `dbo.Users`, `dbo.LotterySessions`, `dbo.Tickets`, `dbo.Draws`, `dbo.Winners`, and `dbo.__EFMigrationsHistory` exist.
4. Start the application, then open **New Query**, select `Loto3000Db`, and run:

```sql
SELECT MigrationId, ProductVersion FROM dbo.__EFMigrationsHistory;
SELECT Id, Username, FirstName, LastName, Email, Role, CreatedAt FROM dbo.Users;
SELECT Id, SessionNumber, StartTime, EndTime, Status FROM dbo.LotterySessions ORDER BY SessionNumber;
SELECT Id, UserId, SessionId, Numbers, SubmittedAt FROM dbo.Tickets ORDER BY Id DESC;
SELECT Id, SessionId, DrawnNumbers, DrawnAt, InitiatedByAdminId FROM dbo.Draws ORDER BY Id DESC;
SELECT TicketId, UserId, MatchedNumbers, MatchedCount, Prize, WonAt FROM dbo.Winners ORDER BY WonAt DESC;
SELECT name, is_unique, filter_definition FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.LotterySessions');
SELECT OBJECT_NAME(parent_object_id) AS TableName, name, definition FROM sys.check_constraints;
```

`SessionStatus`: Active = 0, Completed = 1. `UserRole`: Player = 0, Admin = 1. Number lists are JSON arrays, with EF value converters and snapshot comparers. Prize enum values equal their qualifying match counts: 3 through 7.

## 4. Playing and drawing

1. Open **Login / Register**, create a player account, or sign in.
2. On **Play Loto**, select exactly seven unique numbers from 1–37, or use **Quick pick**. Confirm the ticket.
3. The confirmation includes the session and ticket identifiers and a link to **Winners Board**. **My tickets** shows the player's own history and eventual match/prize result.
4. Sign in as an administrator and open **Admin Panel**. Check session statistics and select **Initiate draw**. Confirm the irreversible action.
5. Eight unique cryptographically secure numbers, newly winning tickets, and the next active session appear immediately. The public board lists **session number, full name, matched numbers, prize and draw date**, newest draw first, without exposing email, username or password hash.

| Matches | Prize                      |
| ------- | -------------------------- |
| 7       | Car (Jackpot)              |
| 6       | Vacation                   |
| 5       | TV                         |
| 4       | $100 Gift Card             |
| 3       | $50 Gift Card              |
| 0–2     | No prize; no Winner record |

All eight drawn numbers participate equally in matching; there is no separate bonus-number rule. Multiple tickets per player and draws of empty sessions are supported.

`LotteryRules.GenerateDrawNumbers` uses a partial Fisher–Yates shuffle of 1–37 with `RandomNumberGenerator.GetInt32`, not `Random` or time-based seeds. Each selection is uniform over the remaining suffix; swapping removes it from future choices. Sorting is only for display. Evaluation computes the sorted set intersection `ticket.Numbers.Intersect(draw.DrawnNumbers)`: a seven-number ticket can match at most seven of the eight drawn values. Invalid persisted ticket numbers fail the draw rather than silently allocating an incorrect prize.

### Transaction and concurrency guarantees

Ticket creation and drawing use serializable transactions. Retrieving the active session acquires an update/range lock retained until commit. This serializes ticket acceptance against session closure. A filtered unique index permits only one active session, and a unique foreign key permits only one draw per session.

Draws persist the draw, all winners, and completed status before inserting the next active session, within the same transaction. If either save fails, all changes roll back. Client draw requests include the displayed `sessionId`; a retry or concurrent request for that completed session is rejected instead of accidentally drawing the next session. The SPA also supplies `sessionId` with tickets to reject stale submissions.

The next session number is incremented in a checked context; overflow rolls back rather than wrapping. Cancellation cleanup uses a non-canceled rollback token, clears tracked state, and preserves the original failure if rollback also fails. No automatic EF transient retry strategy is enabled: replaying only part of this multi-save workflow would be unsafe. Future retries must replay the whole transaction with explicit idempotency design.

Requests are never automatically resubmitted. If a connection fails after a write, refresh the session/history/board before retrying. The server may have committed a request whose response was lost.

## 5. API and Swagger

| Method | Route                        | Access                          | Behavior                                                                          |
| ------ | ---------------------------- | ------------------------------- | --------------------------------------------------------------------------------- |
| POST   | `/api/auth/register`         | Public; always creates Player   | Validate identity/password, store hash, return JWT and public user (201)          |
| POST   | `/api/auth/login`            | Public                          | Verify password, return signed JWT and public user (200)                          |
| GET    | `/api/auth/me`               | Authenticated                   | Resolve the authenticated user's public profile                                   |
| POST   | `/api/tickets`               | Authenticated                   | Validate seven numbers and active session; persist owned ticket (201)             |
| GET    | `/api/tickets/my-tickets`    | Authenticated; own tickets only | Return ticket history and available results                                       |
| POST   | `/api/draws/initiate`        | Admin                           | Atomically draw the requested session, allocate winners and open the next session |
| GET    | `/api/draws/current-session` | Public                          | Return active session identity, number and ticket statistics                      |
| GET    | `/api/winners`               | Public                          | Return public winners ordered by descending draw date                             |

In Swagger, call `POST /api/auth/login`, copy `token`, select **Authorize**, and paste the token **without** the `Bearer` prefix. Swagger adds the correct header. Register/login and the public endpoints can be called before authorization.

Ticket request:

```json
{ "sessionId": 1, "numbers": [1, 4, 6, 9, 12, 22, 37] }
```

Use the actual ID from `/api/draws/current-session`; the number above illustrates the initial session. For tickets, `sessionId` is optional: omitting it submits to whichever session is active when the request is processed. Include it, as the UI does, to reject stale submissions. Optional `userId` or `username` fields must match the authenticated identity and cannot transfer ownership.

Draw request:

```json
{ "sessionId": 1 }
```

Unlike tickets, draw requests require a positive `sessionId` matching the active session.

DTO validation and business-rule violations, including invalid login credentials, return 400 problem responses. Missing/invalid tokens on protected endpoints return 401; unauthorized roles return 403. Unexpected errors return a sanitized 500 response with a trace ID. Register/login share a limit of 20 requests/minute/IP, and ticket/draw writes share a limit of 30 requests/minute/authenticated user; exceeding the limit returns 429 with `Retry-After`.

## 6. Local configuration

The `http` and `https` launch profiles select the Development environment and load [Loto3000.Web/appsettings.Development.json](Loto3000.Web/appsettings.Development.json) alongside [Loto3000.Web/appsettings.json](Loto3000.Web/appsettings.json).

- The development settings include a local JWT signing key and the default administrator password. These are demo credentials for this homework.
- `ConnectionStrings:DefaultConnection` selects the SQL Server instance and database; see the connection examples above.
- `Database:ApplyMigrationsOnStartup` is enabled in Development.
- `Jwt:Issuer` is `Loto3000`, and `Jwt:Audience` is `Loto3000.Web`. Tokens expire after 60 minutes by default; `Jwt:ExpirationMinutes` accepts 1–120.
- Swagger is available automatically in Development.
- Changing `BootstrapAdmin:Password` does not reset an existing administrator's password. The seeder preserves existing accounts.

The SPA stores only the JWT in `localStorage`, restores identity through `/api/auth/me`, sends Bearer headers, clears expired credentials, and synchronizes logout across tabs. Logout clears the browser token; it does not revoke an already copied token before its expiration. Browser-readable token storage makes XSS prevention important. The SPA uses safe DOM text rendering, local assets, and a restrictive Content Security Policy; no third-party JavaScript runs on application pages.

### Logging

Serilog writes to Console and `<content-root>/Logs/loto3000-YYYYMMDD.log` with daily rolling, a 30-file cap, and a 30-day retention limit. During local runs this is `Loto3000.Web/Logs`. Request bodies, passwords, and JWTs are not logged. Use the trace ID in an API error response to find the corresponding server log entry when debugging.

## 7. Verification

### Unit tests — no SQL Server connection required

```powershell
dotnet test Loto3000.Tests -c Release --logger 'trx;LogFileName=unit-tests.trx' --results-directory artifacts/TestResults
```

Visual Studio: **Test > Test Explorer > Run All Tests** after building the solution. The suite covers ticket validation, prize rules, draw generation, winner selection, session transitions, rollback, authentication, public winner ordering, and mapping/architecture checks. Unit tests use mocks and do not require a SQL Server instance.

### Manual API verification

Use a disposable development database because ticket submissions and draws change persisted data. This repository does not include an automated SQL/browser smoke-test harness.

1. Start the app with a Development launch profile and open `/swagger`.
2. Register a player, then set Swagger's **Authorize** token to the token returned by registration.
3. Fetch `/api/draws/current-session` and submit seven unique numbers to `/api/tickets` with that session's ID.
4. Log in as the development administrator and replace the Swagger authorization token with the admin token. Logging in does not automatically update Swagger authorization.
5. Call `/api/draws/initiate` with the same session ID, then confirm that `/api/draws/current-session` returns the next session.
6. Check `/api/winners` for qualifying tickets. An empty board is valid when no ticket matches at least three numbers. Switch back to the player token to check `/api/tickets/my-tickets`.

If Node.js is installed, check the browser script syntax. The package commands below inspect dependency versions and reported vulnerabilities:

```powershell
node --check Loto3000.Web/wwwroot/app.js
dotnet list Loto3000.sln package --include-transitive
dotnet list Loto3000.sln package --vulnerable --include-transitive
```

## 8. Troubleshooting

- **Cannot connect to SQL Server:** verify the instance/service is running and the Windows account has access. If `sqlcmd` is installed, test with `sqlcmd -S localhost -E -C -Q "SELECT @@VERSION"`.
- **Pending migrations:** run the documented `database update` command against the same connection string as the application.
- **Missing signing/bootstrap configuration:** run with the `http` or `https` launch profile so the Development settings are loaded. Remove any environment overrides left over from other runs.
- **Port already in use:** stop the application previously launched on that port, or use another launch URL.
- **401 after reload:** the token expired, was modified, or the signing key changed. Sign in again.
- **400 after a draw:** refresh the current session; the submitted session ID is stale.
- **429 responses:** wait for the one-minute rate-limit window; do not automatically repeat write requests.
- **Visual Studio does not discover tests:** open `Loto3000.sln`, build, and refresh Test Explorer. The CLI test command also discovers and runs all tests.
