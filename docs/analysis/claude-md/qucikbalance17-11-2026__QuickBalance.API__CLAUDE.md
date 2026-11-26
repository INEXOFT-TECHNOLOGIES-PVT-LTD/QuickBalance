# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

QuickBalance.API is an ASP.NET Core Web API (.NET 10) for an accounting/ERP product (masters, sales transactions, purchase reports and account reports). It talks to an existing SQL Server database through Dapper and ADO.NET; EF Core is referenced but not used for data access. The database schema belongs to the legacy QuickBalance desktop app, so tables and columns use short uppercase names (`BRANDMAS`, `ACCMAS`, `PRMAST`, `GDWMAS`, …).

The project is not under git. Sibling folders such as `../DB/BAK`, `../qucikbalance17-11-2026` and the `.zip` files are manual backups or snapshots. Don't edit them.

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
- Controllers carry `[Tags("NN. Name")]`. The numeric prefix sets the Swagger group order, so pick the next free number. `ApiResponseOperationFilter` adds the standard response docs.
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

## Auth and cross-cutting concerns

- JWT bearer authentication. The fallback authorization policy requires every endpoint to be authenticated with a positive `NameIdentifier` claim, so use `[AllowAnonymous]` for public endpoints.
- Logout revokes tokens by storing them in SQL through `TokenRevocationService` (a singleton). Every request checks revocation.
- The `"LoginLimit"` rate-limit policy allows 4 requests per minute per IP.
- CORS allows only `http://localhost:8081` (`FrontendPolicy`).

## Gotchas

- `DTOs/TaxMasterRequest.cs` is excluded in the `.csproj`, so the active one is `DTOs/Tax/TaxMasterRequest.cs`.
- Files like `ProductControllerss.cs`, `PriceCategoryControllerBKP.cs`, `TaxMasDBbkp.cs` and `*.TMP` are old backup copies left in the tree. Check which class is actually registered or routed before editing one.
- Some repositories use the odd nested namespace `QuickBalance.API.Repository.Master.QuickBalance.API.Repository.Master`. Keep `using` directives consistent with the file you're editing.
- Both `Interface/` (empty) and `Interfaces/` exist. Put interfaces in `Interfaces/`.
