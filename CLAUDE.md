# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

QuickBalance.API is an ASP.NET Core Web API (.NET 10) for an accounting/ERP product (masters, sales transactions, purchase reports and account reports). It talks to an existing SQL Server database through Dapper and ADO.NET; EF Core is referenced but not used for data access. The database schema belongs to the legacy QuickBalance desktop app, so tables and columns use short uppercase names (`BRANDMAS`, `ACCMAS`, `PRMAST`, `GDWMAS`, …).

The project is not under git. Sibling folders such as `../DB/BAK`, `../qucikbalance17-11-2026` and the `.zip` files are manual backups or snapshots. Don't edit them.

## Where things are

- **This Web API:** `D:\PROJECT\Salman\Latest\QuickBalance\QuickBalance.API`
- **Legacy WinForms source:** `D:\PROJECT\IFlare\Inventory`. This is the desktop app being migrated. Read it for reference only and don't modify it. The solution `Inventory.sln` builds `Inventory\Inventory.csproj`, and the root-level `Controller/Model/View` folders belong to an older orphaned `.csproj`. Don't use the old copy at `D:\PROJECT\Salman\IFlare\Inventory`, because all of its form (`View\`) files are empty.
- **DB scripts:** `D:\PROJECT\Salman\Latest\QuickBalance\DB\ProjectDB\QuickBalance.sql`. This is a single schema script for the company database, with about 135 tables, 22 stored procedures and 21 user-defined types. Search it for table or SP names rather than reading it whole.

Both external folders are allowed through `permissions.additionalDirectories` in `.claude/settings.local.json`, so `/migrate-analyze <ModuleName>` (`.claude/commands/migrate-analyze.md`) and other legacy comparisons work without `/add-dir`.

## Commands

```powershell
dotnet build                      # build (solution: QuickBalance.API.slnx)
dotnet run --launch-profile http  # http://localhost:5161, Swagger UI at /swagger
dotnet publish -c Release -p:PublishProfile=FolderProfile   # outputs to ./publish
```

There is no test project and no linter configuration. To verify changes, build and then call endpoints through Swagger or `QuickBalance.API.http`. Swagger sends a JWT from `POST` login under the "01. Authentication" tag (`UsersController`).

## Configuration

`appsettings.json` → `DatabaseSettings` (ServerName, DatabaseName, UserName, Password, CompanyCode, TenantId, SecretKey) and `Jwt` (Key, Issuer, Audience, ExpireMinutes). Startup fails fast if any of these are missing.

- **One deployment serves one company database.** `DatabaseName` follows the format `COMP<code>_<yyyy>_<yyyy>`, and `CompCode` is parsed from the `COMP<n>` prefix (see `DbConnectionFactory` / `DatabaseConnection`). Passing `isMasterDB: true` switches the catalog to `MasterDB`.
- `TenantId` must match the `tenant_id` claim in every JWT (checked in `Program.cs` `OnTokenValidated`).
- `SecretKey` decrypts user passwords stored with the `AES256GCM$` prefix. Older passwords are plain Base64 (`AuthService.VerifyPassword`).

## Architecture

The codebase has two coexisting styles. Use the style that matches the area you're editing, and use the new style for new master CRUD.

### New layered style (most `Controllers/Master/*`)

`Controller` → `I*Interface` / `I*Service` (in `Interfaces/`) → `Services/*Service` → `Repository/**` (Dapper SQL) → `DBConn`.

- Controllers are thin. They call the service and return `result.Status ? Ok(result) : BadRequest(result)`.
- Services do validation (they throw `ArgumentException` for user-facing messages), normalization, duplicate checks and logging. They catch all exceptions and return `ApiResponse<T>.Failure(...)`, so a business error still comes back as HTTP 400 with `status:false`.
- Repositories write raw parameterized SQL with column aliases that map to DTO property names (`BRANDNAME AS BrandName`). They call `DBConn.QueryListAsync` / `QueryFirstOrDefaultAsync` / `ExecuteAsync` / `QueryMultipleAsync`. Pagination uses `OFFSET/FETCH` plus a separate `COUNT(*)` query and returns `PagedResponse<T>`. `BrandDB.cs` is a clean reference implementation.
- **Every new service or repository must be registered by hand in `Program.cs`.** Nothing is auto-registered.
- Response envelopes live in `CLS/ApiResponse.cs`: `ApiResponse<T>` (with optional `Pagination`), `ApiResponseWithoutPagination<T>`, `ApiResponseWithError` and `ApiResponseWithSuccess`. JSON is camelCase. Model-validation failures and 401/403 responses are rewritten globally into `ApiResponseWithError`.
- Master controllers, `UsersController` and `SalesOrderController` carry `[Tags("NN. Name")]`. The numeric prefix sets the Swagger group order. Numbers 01–25 are in use, so the next free one is 26. A few tags omit the space (`"13.Item Brand"`, `"12.Item Categories"`); use `"NN. Name"` for new ones. `ApiResponseOperationFilter` adds the standard response docs.
- `ICurrentUser` (`Services/CurrentUser.cs`) exposes JWT claims such as `UserId`, `TenantId`, `CompanyId` and `BranchId`.

### Legacy port style (`Controllers/Reports/*`, `Controllers/Transactions/*`, the purchase report controllers, and parts of Customer/Supplier/AccountLedger)

This style is ported from the desktop app. Each action builds its own context:

```csharp
LngTB.InitLng();
var Variables = new Variable { UserId = <NameIdentifier claim> };
var conn = _databaseConnection.GetConnectionString(Variables);
var DBConn = new DBConn(conn);
var commonFunction = new CommonFunction(DBConn);
await commonFunction.SetVariableValues(Variables);   // loads company settings: FY dates, decimals, CashCD, VRSYS, etc.
var someDB = new XxxDB(DBConn) { Variables = Variables }; someDB.Functions.Variables = Variables;
```

- `DB/Variable.cs` is the per-request settings bag (financial year, decimal format strings, feature flags, branch/VRSYS config). Many `DB/*DB.cs` classes depend on it being populated first.
- `DB/*DB.cs` classes (`DayBookDB`, `AccMasDB`, `CustomerDB`, `SupplierDB`, `PurmDB`, …) are large and often build SQL with string concatenation. Reports commonly create temp tables (`CreateTable` … `DropTable`), return `DataTable`, and the controller converts the rows to dictionaries.
- `CLS/Cls*.cs` are the entity classes paired with these DB classes.
- Some of these controllers have `[ApiExplorerSettings(IgnoreApi = true)]`, so they don't appear in Swagger.

### Data access entry points

- `DBConn` (`DB/DBConn.cs`) is registered as scoped. It wraps a `SqlConnection` with optional transaction support (`TransactionConnections`, `CommitTransaction`, `RollbackTransaction`), Dapper helpers, legacy `GetDatatable` / `ExecuteScalar` / `GetnewId` helpers and `*Master*` variants that target `MasterDB`.
- Two connection-string builders exist with the same logic: `IDbConnectionFactory`/`DbConnectionFactory`, used by DI, and `DatabaseConnection`, used by legacy controllers.

### Conventions

These describe what the code does today. Follow the newer pattern for new code.

**DTOs**
- Name them `<Entity>Request` (input) and `<Entity>Response` (output), for example `SalesmanRequest`/`SalesmanResponse` and `TaxMasterRequest`/`TaxMasterResponse`. Report inputs are `<Report>Request` (`DayBookRequest`, `TrialBalanceRequest`).
- Newer modules put both in a per-entity subfolder whose namespace matches the folder: `DTOs/SalesMan/`, `DTOs/Tax/`, `DTOs/Currencies/`, `DTOs/PartyGroup/`. Most older DTOs sit flat in `DTOs/` (namespace `QuickBalance.API.DTOs`), and some modules split them, such as `DTOs/BrandRequest.cs` with `DTOs/Brand/BrandResponse.cs`.
- `DTOs/Respons-DTO/` (namespace `QuickBalance.API.DTOs.Respons_DTO`) holds older `*Dto` and query-result classes plus the shared `PagedResponse<T>`. Don't add new entity DTOs there.

**Routes**
- New-style and most legacy controllers use `api/v1/<kebab-case-plural>`, for example `api/v1/brands`, `api/v1/currency-conversions` and `api/v1/salesman-groups`. A few are singular (`api/v1/salesman`, `api/v1/item-group`), so use the plural for new ones.
- Master CRUD routes: `GET /` (paged with `page`, `pageSize`, `search` query params), `GET /{id:int}`, `POST /`, `PUT /{id:int}`, `DELETE /{id:int}`. Sub-resources and lookups are kebab-case segments (`{id:int}/image`, `lookups/ledger-groups`, `primary-groups`).
- Account reports use `api/v1/reports/<kebab-name>` (`reports/day-book`, `reports/trial-balance`) and take a `POST` body of `<Report>Request`.
- Sales transactions use RPC-style actions instead of REST verbs: `POST manage` (create or update), `GET list`, `GET get/{id:int}`, `POST cancel/{id:int}`.
- Older purchase report controllers and `CompaniesController` still use `api/[controller]` (PascalCase, no version). Don't copy that.
- `AccountLedgerController` and `AccountsController` both use the route prefix `api/v1/accounts`. Check both before you add an action there.

**Logging**
- Logging uses the built-in `ILogger<T>` with no Serilog or NLog. Levels are set in the `Logging` section of `appsettings.json`.
- Services do the logging. They inject `ILogger<XxxService>` and call `_logger.LogError(ex, "Error <verb>ing <Entity> {Id}.", id)` in the catch-all `Exception` block, then return a generic `ApiResponse<T>.Failure(...)` message. `ArgumentException` (validation) is returned to the caller and not logged.
- Use structured placeholders (`{Id}`, `{Code}`), not string interpolation. A few services also log successful writes with `LogInformation` (`CurrencyService`, `CurrencyConversionService`, `ProductService`).
- Thin controllers don't log. Legacy controllers (reports, transactions) usually have no logger and return `StatusCode(500, new { status = false, message = ex.Message, ... })`, which sends the raw exception message to the client.

## Auth and cross-cutting concerns

- JWT bearer authentication. The fallback authorization policy requires every endpoint to be authenticated with a positive `NameIdentifier` claim, so use `[AllowAnonymous]` for public endpoints.
- Logout revokes tokens by storing them in SQL through `TokenRevocationService` (a singleton). Every request checks revocation.
- The `"LoginLimit"` rate-limit policy allows 4 requests per minute per IP.
- CORS allows only `http://localhost:8081` (`FrontendPolicy`).

## Gotchas

- `DTOs/TaxMasterRequest.cs` is excluded in the `.csproj`, so the active one is `DTOs/Tax/TaxMasterRequest.cs`.
- Files like `ProductControllerss.cs`, `TaxMasDBbkp.cs` and `*.TMP` are old backup copies left in the tree. Check which class is actually registered or routed before editing one.
- Some repositories use the odd nested namespace `QuickBalance.API.Repository.Master.QuickBalance.API.Repository.Master`. Keep `using` directives consistent with the file you're editing.
- Both `Interface/` (empty) and `Interfaces/` exist. Put interfaces in `Interfaces/`.
- Use the company DB by default. MasterDB holds only COMPMAS, CONFIGTB (system-level), COUNTRY, HBDTLS, LNGMAS, LNGTB, MULTILNGTB, REGION, SYSINFO.
