# Sales Web API map (Sales Order, Sales Invoice, Sales Return)

Status: **RUN 1 done (Parts 1-3). Part 4 = RUN 2, Parts 5-6 = RUN 3: NOT STARTED.**
All paths are relative to `D:\PROJECT\Salman\Latest\QuickBalance\QuickBalance.API` unless stated. Read-only analysis.

## Code actually read

| File | Lines read | Notes |
|---|---|---|
| CLAUDE.md | headings grep (all); full text via session context | copied to docs/analysis/claude-md/QuickBalance.API__CLAUDE.md |
| Program.cs | 40-95 in full; AddScoped/AddSingleton lines by grep (45-169) | auth/CORS/rate-limit config NOT READ here (only via CLAUDE.md:100-105) |
| Configuration/DatabaseConnection.cs | 1-66 (live code; 69-144 is commented-out old copy) | |
| DB/DBConn.cs | 9-48, 351-406, 751-855; method list by grep | other bodies NOT READ |
| DB/CommonFunction.cs | 43-172 | `CheckDatabaseConnection`, lines 173+ NOT READ |
| Services/CurrentUser.cs | 1-77 | |
| CLS/ApiResponse.cs | 1-80 | lines 81-143 (ApiResponseWithError/WithSuccess/PaginationInfo) NOT READ |
| Interfaces/IBrandInterface.cs | 1-22 | |
| Controllers/Master/BrandController.cs | 1-50 | |
| Services/BrandService.cs | grep only (catch/log/Failure lines) | body NOT READ |
| Repository/Master/BrandDB.cs | grep only | body NOT READ |
| Controllers/Transactions/Sales/SalesOrderController.cs | 1-335, 478-492, 536-584; routes by grep | commented-out old code 335-435 skipped |
| Controllers/Transactions/Sales/SalesInvoiceController.cs | 29-118; routes + DB-call lines by grep | list (120-204), get (205-307), cancel (591-653), pending (654-721) bodies NOT READ |
| Controllers/Transactions/Sales/SalesReturnController.cs | 60-95; routes + DB-call lines by grep | rest NOT READ |
| DB/SalesOrderDB.cs | 1-143 (all) | |
| DB/SalesInvoiceDB.cs | 1-205 (all) | |
| DB/SalesReturnDB.cs | 20-187 | lines 1-19 (usings/ctor) NOT READ |
| DB/CordMDB.cs | 20-85; SQL/method lines by grep | `List` (87-145), `SearchByOrdNOAsync` body (191+) NOT READ |
| DB/CordTDB.cs, CordDDB.cs, SalMDB.cs, SalTDB.cs, SalDDB.cs, SntStkDB.cs | method + SQL lines by grep only | bodies NOT READ |
| DTOs/SalesOrderRequest.cs, SalesInvoiceRequest.cs, SalesReturnRequest.cs | all | |
| DTOs/PaginationRequest.cs | class declarations by grep only (lines 3, 51) | properties NOT READ |
| docs/SALES-SQL/*Manage*.sql | grep for BEGIN TRAN / TRY / @xxxNO checks only | full SP bodies are RUN 2 |

---

# PART 1 - Project conventions

## 1.1 CLAUDE.md files

Found (whole tree under `D:\PROJECT\Salman\Latest\QuickBalance`):
1. `QuickBalance.API/CLAUDE.md` (in scope) - copied verbatim to `docs/analysis/claude-md/QuickBalance.API__CLAUDE.md`.
2. `qucikbalance17-11-2026/QuickBalance.API/CLAUDE.md` - backup snapshot, **out of scope, not read, not copied** (per your instruction). No conflict analysis is possible.

Rules from CLAUDE.md (no conflicts, single file):

| Topic | Rule | Cite |
|---|---|---|
| Architecture | Two styles coexist. New master CRUD: Controller -> `I*Interface` (in `Interfaces/`) -> `Services/*Service` -> `Repository/**` (Dapper) -> `DBConn`. `Controllers/Transactions/*` and `Controllers/Reports/*` are "legacy port style" (per-action context building, `DB/*DB.cs`, string SQL). Use the style of the area you edit; new master CRUD uses the new style. | CLAUDE.md:37-70 |
| DI | Every new service/repository must be registered by hand in Program.cs; nothing auto-registered. | CLAUDE.md:48 |
| Naming (DTOs) | `<Entity>Request` / `<Entity>Response`; per-entity subfolder with matching namespace for new modules; don't add to `DTOs/Respons-DTO/`. | CLAUDE.md:81-85 |
| Routes | `api/v1/<kebab-plural>`; sales transactions use RPC style: `POST manage`, `GET list`, `GET get/{id:int}`, `POST cancel/{id:int}`. | CLAUDE.md:86-93 |
| Responses | `ApiResponse<T>` envelopes in `CLS/ApiResponse.cs`; controllers return `result.Status ? Ok(result) : BadRequest(result)`; JSON camelCase; validation/401/403 rewritten globally into `ApiResponseWithError`. | CLAUDE.md:46-50 |
| Error handling | Services throw `ArgumentException` for user-facing validation, catch all, return `ApiResponse<T>.Failure`. Legacy controllers return `StatusCode(500, new { status=false, message = ex.Message })`, which leaks exception text. | CLAUDE.md:42-44, 98 |
| Logging | `ILogger<T>` in services only; `_logger.LogError(ex, "Error <verb>ing <Entity> {Id}.", id)`; structured placeholders; thin controllers don't log. | CLAUDE.md:94-98 |
| Swagger tags | `[Tags("NN. Name")]`; 01-25 said to be in use, next free 26 (but see 2.4: tags 25/26/27 are already used by the sales controllers). | CLAUDE.md:49 |

Discrepancy between CLAUDE.md and code: CLAUDE.md:49 says the next free tag is 26, but `SalesOrderController` = "25. Sales Orders", `SalesInvoiceController` = "26. Sales Invoices", `SalesReturnController` = "27. Sales Returns" (Controllers/Transactions/Sales/SalesOrderController.cs:17, SalesInvoiceController.cs:18, SalesReturnController.cs:13). Next free is therefore 28 (assuming no other tag >27; NOT checked).

## 1.2 One existing layered feature: Brand

| Layer | Type | File |
|---|---|---|
| Controller | `BrandsController` (`[ApiController]`, `[Tags("13.Item Brand")]`, `[Route("api/v1/brands")]`, `[Authorize]`) | Controllers/Master/BrandController.cs:13-17 |
| Interface | `IBrandInterface` (Create/GetById/GetAll/Update/Delete/GetActive, all `Task<ApiResponse<...>>`) | Interfaces/IBrandInterface.cs:7-21 |
| Service | `BrandService` | Services/BrandService.cs |
| Repository | `BrandDB` (sealed; ctor `BrandDB(DBConn db)`) | Repository/Master/BrandDB.cs:11,15 |

Controller ctor injects `IBrandInterface` + `ICurrentUser` (BrandController.cs:22-28) and returns `result.Status ? Ok(result) : BadRequest(result)` (BrandController.cs:44).

DI registration (Program.cs:135-136):
```csharp
builder.Services.AddScoped<BrandDB>();
builder.Services.AddScoped<IBrandInterface, BrandService>();
```
Service behaviour (grep of Services/BrandService.cs): validation throws `ArgumentException` (lines 27, 30); `catch (ArgumentException ex)` returns `Failure(ex.Message)` (78-80); `catch (Exception ex)` logs `_logger.LogError(ex, "Error creating Brand.")` (82-84) and returns a generic failure. Repository uses Dapper helpers `_db.QueryFirstOrDefaultAsync` / `QueryListAsync` and `OFFSET @Offset ROWS` (BrandDB.cs:44-109, 91).

## 1.3 Database access

- Connection string source: `DatabaseConnection.GetConnectionString(Variable variables, bool isMasterDB=false)` reads the `DatabaseSettings` config section (ServerName, DatabaseName, UserName, Password), uses `MasterDB` as catalog when `isMasterDB` (Configuration/DatabaseConnection.cs:16-25), builds with `Encrypt=true; TrustServerCertificate=true; MultipleActiveResultSets=true` (39-48), writes server/db/user/password into `variables` (50-53) and parses `CompCode` from the `COMP<n>` prefix of the database name, default 1 (55-62). Registered as singleton (Program.cs:124).
- A second builder exists: `IDbConnectionFactory`/`DbConnectionFactory`, singleton (Program.cs:45-47); used by the DI `DBConn` factory lambda (Program.cs:49-66). Its source file was NOT READ.
- `DBConn : IDisposable` (DB/DBConn.cs:9) has two constructors, `DBConn(string connectionString)` (22) and `DBConn(IDbConnectionFactory)` (29). **`DBConn` is registered twice**: factory lambda at Program.cs:49 and plain `AddScoped<DBConn>()` at Program.cs:91; with standard .NET DI the later registration is the one resolved. Which one the app ends up using was not verified at runtime.
- Methods (DBConn.cs, by grep): legacy `GetDatatable` (532/555/585), `Execute`, `ExecuteScalar`, `GetnewId`, `ReturnIntValue/DecValue/StringValue`, `GetDataSet` (783), `GetDataTableFromProcedure(name)` (755, swallows exceptions and returns empty table, 772-775), `ExecuteStoredProcedure(name, List<SqlParameter>)` (807-832, clones each parameter via `CloneSqlParameter` at 658, reads with `ExecuteReaderAsync` + `DataTable.Load`; **no try/catch, so SQL errors propagate**), Dapper helpers `QueryListAsync / QueryFirstOrDefaultAsync / QuerySingleOrDefaultAsync / ExecuteAsync / QueryMultipleAsync` (858-908), `*Master*` variants targeting MasterDB (59-109).
- TVP support: sales DB classes pass `SqlDbType.Structured` with `TypeName = "dbo.ASK_UDT_*"` and a `DataTable`; `ExecuteStoredProcedure` forwards them via `CloneSqlParameter` (DBConn.cs:820). The body of `CloneSqlParameter` (658+) was NOT READ, so correct cloning of `Structured` parameters is assumed from the fact the endpoints are in use, not verified.
- Optional transactions: `BeginTransaction` (351), `CommitTransaction` (367), `RollbackTransaction` (389). `Dispose()` calls `RollbackTransaction()` then disposes command/adapter/connection (837-855).
- Disposal: `DBConn` is disposed by the DI container only when DI-created. The sales controllers create it with `new DBConn(connectionString)` and never dispose it (e.g. SalesOrderController.cs:54, no `using`).
- EF Core: **no DbContext found**. Grep for `DbContext|UseSqlServer|EntityFrameworkCore` returned only `DB/PlDB.cs:1: using Microsoft.EntityFrameworkCore.Metadata.Internal` (an unused using). Dapper is used (`using Dapper` in DBConn.cs:2, CordMDB.cs:1, CordTDB.cs:1, CordDDB.cs:1, AccMasDB.cs:1, CustomerDB.cs:3; grep output was truncated at 8 hits).

## 1.4 Auth and user context

- All three sales controllers have class-level `[Authorize]` (SalesOrderController.cs:19, SalesInvoiceController.cs:21; SalesReturnController.cs:16).
- `ICurrentUser`/`CurrentUser` (scoped, Program.cs:94; Services/CurrentUser.cs:5-77): `UserId` from `ClaimTypes.NameIdentifier`, throws `UnauthorizedAccessException` if missing or <=0 (13-14, 34-45); `TenantId` = claim `tenant_id` (19-20); `CompanyId`/`BranchId` = optional claims `company_id`/`branch_id` (22-26).
- **Inconsistent resolution of the user id in the sales controllers:** Order manage uses `_currentUser.UserId` (SalesOrderController.cs:40); Order list/get and Invoice manage read `User.FindFirst(ClaimTypes.NameIdentifier)` directly (SalesOrderController.cs:124-127, 215-228; SalesInvoiceController.cs:39-42).
- Company: one deployment = one company DB; `CompCode` is derived from `DatabaseName` (DatabaseConnection.cs:55-62), not from a JWT claim. JWT `tenant_id` check is in Program.cs `OnTokenValidated` per CLAUDE.md:34 (NOT READ in code).
- `CommonFunction.SetVariableValues(Variable)` (DB/CommonFunction.cs:43+) loads:
  - From **MasterDB**: `COMPMAS WHERE ID = CompCode` -> `CompShortName`; `COUNTRY` -> `VatLbl` (45-69); master `CONFIGTB` -> `BranchYN`, `ApprovalSystem` (153-167).
  - From the company DB: `FYDETAILS WHERE PREVFIN = false` -> `fyStartDate`, `fyEndDate`, `Finyr`, `DiscCD`, `CashCD`, `VATCode` (71-84); `CONFIGTB` -> `batch`, `SlNoTrack`, `MultiCurr`, `VRSYS`, `MULTIVRSYS`, `STKTRNSFR*`, `STKMVACC*`, `DBSalSummary`, `BrPrList`, `TagAccMstr`, `additionalCost`, `InventoryApproval`, `DecimalPoint`/`DecString`, `QtyDecimalPoint`/`QtyDecString` (86-135); per-user `VRSYS_DEFBRANCH` and `VRSYS_ALLOWEDBRANCHES` via `SearchByMNNameAndUser` (139-149); `StkIssRec` (169).
  - Rest of the method (172+) NOT READ.
- Note: it builds a `MasterDB` connection from `Variables.ServerName/UserName/Password` (45-57); these are only populated if `GetConnectionString` ran first, which the controllers do.

## 1.5 Response format

- New style: `ApiResponse<T> { Status, Message, Data (omitted when default), Pagination (omitted when null) }` with static `Success`, `SuccessWithPagination`, `Failure` (CLS/ApiResponse.cs:9-55); `ApiResponseWithoutPagination<T>` (61+). `ApiResponseWithError` / `ApiResponseWithSuccess` / `PaginationInfo` classes: NOT READ (lines 81-143).
- Sales controllers do **not** use these envelopes; they return anonymous objects `{ status, message, data }` / `{ status=false, message, errors = [] }` (e.g. SalesOrderController.cs:89-93). Pagination object is `PaginationResponse { Page, PageSize, TotalRecords, TotalPages }` (SalesOrderController.cs:165-171); its class file NOT READ.
- Status codes seen in the sales controllers: 200 Ok, 201 Created (only when the SP message is exactly `"Saved"`), 400, 401, 404 (Order get only, confirmed), 500.

---

# PART 2 - Existing sales endpoints

Common to all (legacy port style): each action does `LngTB.InitLng()`, builds a `Variable`, gets the connection string from `DatabaseConnection`, `new DBConn(connectionString)`, `new CommonFunction(DBConn)`, `CheckDatabaseConnection()` (500 on failure), `SetVariableValues`, then calls the DB class (e.g. SalesOrderController.cs:36-64). Controllers inject `DatabaseConnection` and `ICurrentUser` only (SalesOrderController.cs:22-29); no service/repository layer, no logger.

## 2.1 Sales Order - `api/v1/sales-orders` (`[Tags("25. Sales Orders")]`)

| Verb + route | Action (line) | Input | Calls | Tables / SP | Response |
|---|---|---|---|---|---|
| POST `manage` | `ManageSalesOrder` (31) | body `SalesOrderRequest` | `SalesOrderDB.ManageSalesOrder` (SalesOrderDB.cs:94) | SP `ASK_SP_ManageSalesOrderDetails` (SalesOrderDB.cs:115); tables inside SP = Run 2 | 400 if body null / no items / `CustomerCode==0` (70-77); reads `MESSAGE`,`STATUS` from first row (84-85); STATUS=1 -> 201 if message=="Saved" else 200; else 400; empty result -> 500 |
| GET `list` | `ListSalesOrder` (106) | `PaginationRequests` + `fromDate`,`toDate` | `CordMDB.ListAsync("O")` (156; CordMDB.cs:20-85) | inline SQL: CORDM INNER JOIN ACCMAS, `STYPE='O'`, `LIST_ALLOW_VR_STMNT` filter, search on VSDOCNO/ACCNAME, date range, `ORDER BY ORDNO DESC OFFSET/FETCH`, `COUNT(*) OVER()` as TotalRecords (CordMDB.cs:66-84) | 200 `{status,message,data[],pagination}`; 500 returns `ex.Message` (185, 190) |
| GET `get/{id:int}` | `GetSelectedSalesOrder` (196) | route id | `CordMDB.SearchByOrdNOAsync` (279), `CordTDB.SearchByOrdNoAsync` (297), `CordDDB.SearchAddInfoAsync` (307) | CORDM + ACCMAS + SMMAST (+ TBUSER) (CordMDB.cs:191-201 partial); CORDT + PRMAST + PRUNITMAS (CordTDB.cs:125-129); CORDD (CordDDB.cs:29) | 400 id<=0; 401; 404 not found (283); 200 `SelectedSalesOrderResponse{Header,Items,AdditionalInformations}` (309-321); 500 generic message (327-332) |
| POST `cancel/{id:int}` | `CancelSalesOrder` (438) | route id | `SalesOrderDB.CancelSalesOrder` (480; SalesOrderDB.cs:118) | SP `ASK_SP_CancelSalesOrderDetails` | STATUS=1 -> 200, else 400 (486-489) |
| GET `pending-list` | (501) | `PaginationRequest` (has CustomerId, OrderId, SortBy, SortDirection) | `SalesOrderDB.PendingSalesOrderInInvoices` (542; SalesOrderDB.cs:128) | SP `ASK_SP_PendingSalesOrderInInvoices` | 200; if `OrderId==0` returns list + pagination, else item list without pagination (563-581) |

## 2.2 Sales Invoice - `api/v1/sales-invoices` (`[Tags("26. Sales Invoices")]`)

| Verb + route | Action (line) | Input | Calls | Tables / SP | Response |
|---|---|---|---|---|---|
| POST `manage` | `ManageSalesInvoice` (29-30) | body `SalesInvoiceRequest` + **header `Idempotency-Key`** (required, <=100 chars, 70-79) | `SalesInvoiceDB.ManageSalesInvoice(request, userId, idempotencyKey, requestHash)` (94; SalesInvoiceDB.cs:142) | SP `ASK_SP_ManageSalesInvoiceDetails` (SalesInvoiceDB.cs:182) | same validation + STATUS/MESSAGE handling as Order (84-107). `requestHash` = SHA-256 hex of `JsonSerializer.Serialize(request)` computed before normalisation (55-59) |
| GET `list` | `ListSalesInvoice` (120-121) | `PaginationRequest` (singular type) | `SalMDB.List(clsSalM)` (164) | SALM INNER JOIN ACCMAS (SalMDB.cs:80); body NOT READ | NOT READ |
| GET `get/{id:int}` | `GetSelectedSalesInvoice` (205-206) | route id | `SalMDB.GetInvoiceAsync` (273; SalMDB.cs:107) | SALM, ACCMAS, SMMAST, TBUSER, SALT, PRMAST, PRUNITMAS, SNTSTK, SALD (SalMDB.cs:113-221, grep) | body NOT READ |
| POST `cancel/{id:int}` | `CancelSelectedSalesInvoice` (591-592) | route id | `SalesInvoiceDB.CancelSalesInvoice(id, userId, commonFunction)` (633) | SP `ASK_SP_CancelSalesInvoiceDetails` (SalesInvoiceDB.cs:192); note the `commonFunction` parameter is unused (185-193) | NOT READ |
| GET `pending` | `ListPendingSalesInvoicesInReturn` (654-655) | query `salesReturnId`, `SalesInvoiceNumber` | `PendingSalesInvoicesInReturn` (693; SalesInvoiceDB.cs:195) then `salMDB.SearchByInvno` (701) | SP `ASK_SP_PendingSalesInvoicesInReturn` + SALM/ACCMAS/SMMAST | NOT READ |

## 2.3 Sales Return - `api/v1/sales-returns` (`[Tags("27. Sales Returns")]`)

| Verb + route | Action (line) | Input | Calls | Tables / SP | Response |
|---|---|---|---|---|---|
| POST `manage` | `ManageSalesReturn` (24-25) | body `SalesReturnRequest` | `SalesReturnDB.ManageSalesReturn` (72; SalesReturnDB.cs:142) | SP `ASK_SP_ManageSalesReturnDetails` (SalesReturnDB.cs:174) | same validation/response as Order (62-85) |
| GET `list` | `ListSalesReturn` (98-99) | `PaginationRequest` | `SalMDB.List(clsSalM)` (144) | SALM (same list method as invoice; how it separates invoices from returns is NOT READ) | NOT READ |
| GET `get/{id:int}` | `GetSelectedSalesReturn` (182-183) | route id | `SalMDB.SearchByInvno` (242), `SalTDB.SearchByInvno` (245), `SntStkDB.GetByVoucherIn` (250), `SalDDB.SearchAddInfo` (255), `SalMDB.SearchByOrdNo` (269) | SALM, ACCMAS, SMMAST, TBUSER, SALT, PRMAST, PRUNITMAS, SNTSTK, SALD (inline concatenated SQL: SalMDB.cs:96, SalTDB.cs:20, SntStkDB.cs:47, SalDDB.cs:20) | 404 "Sales return not found." (259); rest NOT READ |
| POST `cancel/{id:int}` | `CancelSelectedSalesReturn` (292-293) | route id | `SalesReturnDB.CancelSalesReturn` (334; SalesReturnDB.cs:177) | SP `ASK_SP_CancelSalesReturnDetails` | NOT READ |
| (pending) | none | | | | the "pending invoices for a return" lookup lives in the **invoice** controller (`GET sales-invoices/pending`) |

## 2.4 Inconsistencies between the three controllers

1. **Idempotency:** only Invoice manage requires `Idempotency-Key` and passes `@IDEMPOTENCYKEY`/`@REQUESTHASH` to the SP (SalesInvoiceDB.cs:167-171); Order and Return have neither (SalesOrderDB.cs:100-113, SalesReturnDB.cs:151-172).
2. **Pending routes:** Order uses `pending-list` (SalesOrderController.cs:501), Invoice uses `pending` (SalesInvoiceController.cs:654); Return has none.
3. **Pagination request type:** Order list uses `PaginationRequests` (plural; SalesOrderController.cs:107), Invoice and Return list use `PaginationRequest` (SalesInvoiceController.cs:121, SalesReturnController.cs:99). Page-size limits for each: NOT READ.
4. **User id source:** `_currentUser.UserId` vs direct claim lookup (see 1.4).
5. **Error disclosure:** manage/list/cancel return `ex.Message` on 500 (SalesOrderController.cs:97, 185), but Order get returns a generic message (327-332).
6. **Order of checks in Invoice manage:** the Idempotency-Key header is validated only after the DB connection check and `SetVariableValues` (SalesInvoiceController.cs:51-79).
7. **Read side mixes two styles:** Order get uses new `*Async` methods returning DTOs (CordMDB/CordTDB/CordDDB, with commented-out older `DataTable` versions); Invoice get uses `SalMDB.GetInvoiceAsync`; Return get still uses legacy `DataTable` methods with string-concatenated SQL (SalMDB.cs:96, SalTDB.cs:20, SntStkDB.cs:47).
8. **Dead code:** large commented-out blocks in SalesOrderController (335-435), SalesInvoiceController (308-590) and CordMDB/CordTDB.
9. **Swagger tag numbers 25/26/27** are used, contradicting CLAUDE.md:49 (see 1.1).
10. **No attributes for model validation:** none of the request DTOs has `[Required]`/`[Range]`; validation is hand-written in the controller (3 checks) and the SP.

---

# PART 3 - Save/Update flow

Common facts:
- **Create vs update** is decided by the document number in the request: `SordNo` / `SinvNo` / `SretNo` equal to 0 means create, non-zero means update. Evidence in the SPs: Order `IF (@SORDNO = 0)` (SO/3-ASK_SP_ManageSalesOrderDetails.sql:118), `IF (@SORDNO <> 0)` (90); Invoice `IF(@SINVNO = 0)` (SI/4-...sql:299), `IF(@SINVNO <> 0)` (256); Return `IF(@SRETNO = 0)` (SR/4-...sql:266), `IF(@SRETNO <> 0)` (237). The C# code only forwards the number. The log note is `'Create '` vs `'Update'` (SO:222, SI:428, SR:395).
- **C# transaction:** none. A grep for `BeginTransaction|TransactionScope|SqlTransaction|CommitTransaction` over the sales controllers and DB classes returned no matches. Transaction handling is inside the SPs: all three Manage SPs have `BEGIN TRY` / `BEGIN TRAN` / `COMMIT TRAN` / `BEGIN CATCH` / `ROLLBACK TRAN` (Order: SO/3-...sql:20-21, 239-243; Invoice: SI/4-...sql:27-28, 603-607; Return: SR/4-...sql:26-27, 455-459). What the CATCH block returns is Run 2.
- **Normalisation applied to every item row in all three DB classes:** `AlCode` forced to `"0"`, `AlName` forced to `""` (any request value is ignored); `GdwCode` 0 -> 1; `ConvFact` 0 -> 1; `NetAmount = Taxable + TaxAmount` (client value ignored). Order: SalesOrderDB.cs:48-52; Invoice: SalesInvoiceDB.cs:67-74; Return: SalesReturnDB.cs:67-74. Invoice and Return additionally: `Mrp` 0 -> `Rate`; `MfgDate`/`ExpDate` null -> header `Date`.
- Null lists are replaced with empty lists; empty/null strings become `string.Empty`.
- Header-level normalisation (Invoice and Return only): `PayMode` null or "CASH" -> `"Cash"`, "CREDIT" -> `"Credit"`, anything else -> `"Bank"`; `DueDate` null -> `Date` (SalesInvoiceDB.cs:144-145; SalesReturnDB.cs:144-145). Invoice only: `Pos` "Y" -> "Y" else "N"; `PcName` forced to `"API"` (SalesInvoiceDB.cs:146-147).
- All SPs are called via `DBConn.ExecuteStoredProcedure` and return a DataTable; the controllers read `MESSAGE` and `STATUS` from row 0.
- DTO nullability: no `[Required]` attributes anywhere. Value types are non-nullable (default 0). Nullable: `PayMode`, `Pos`, `PcName` (string?), `DueDate`, `MfgDate`, `ExpDate` (DateTime?). `SalesOrderRequest`, `SalesInvoiceRequest`, `SalesReturnRequest` property lists below are complete.

## 3.1 Sales Order

**Request DTO** (DTOs/SalesOrderRequest.cs)
- `SalesOrderRequest` (3-22): `int SordNo`, `DateTime Date`, `int CustomerCode`, `int SalesmanCode`, `decimal DiscPerc`, `decimal DiscAmt`, `decimal RoundOff`, `List<SalesOrderItemRequest> Items`, `List<SalesOrderAdditionalRequest> Additional`.
- `SalesOrderItemRequest` (24-63): `int SlNo, PrCode`; `decimal Qty, Rate, LDiscPerc, LDiscPrice, DiscPerc, DiscPrice`; `int UnitCode, GdwCode`; `string AlCode, AlName` (default ""); `int ConvType`; `decimal ConvFact, Taxable`; `int TaxCode`; `decimal TaxPerc, TaxAmount, NetAmount`.
- `SalesOrderAdditionalRequest` (65-71): `int AccId`, `decimal Amount`.

**TVP build** (`SalesOrderDB.CreateItemTable`, SalesOrderDB.cs:21-92)
- Item table, 19 columns in this order (24-42): SLNO int, PRCODE int, QTY dec, RATE dec, LDISCPERC dec, LDISCPRICE dec, DISCPERC dec, DISCPRICE dec, UNITCODE int, GDWCODE int, ALCODE string, ALNAME string, CONVTYPE int, CONVFACT dec, TAXABLE dec, TAXCODE int, TAXPERC dec, TAXAMOUNT dec, NETAMOUNT dec. Rows are added in the same order (54-74).
- Additional table, 2 columns (80-81): ACCID int, AMOUNT dec.
- Defaults/normalisation: see "Common facts" (48-52).

**SP call** (SalesOrderDB.cs:94-116): `ASK_SP_ManageSalesOrderDetails` with `@SORDNO int`, `@DATE datetime`, `@CUSCODE int`, `@SMCODE int`, `@DISCPERC dec(18,6)`, `@DISCAMT dec(18,6)`, `@ROUNDOFF dec(18,6)`, `@USERID int` (from `Variables.UserId`), `@ITEMDTLS` structured `dbo.ASK_UDT_SalesOrderItem`, `@ADDIDTLS` structured `dbo.ASK_UDT_SalesOrderAdditionalInfo`. The SP's declared signature has not been compared (Run 2).

**Create vs update:** `SordNo == 0` create, else update (see above). **Transaction:** only inside the SP.
**Controller checks:** body not null, at least one item, `CustomerCode != 0` (SalesOrderController.cs:70-77). No check on `Date`, quantities or `SordNo` existence in C#.

## 3.2 Sales Invoice

**Request DTO** (DTOs/SalesInvoiceRequest.cs)
- `SalesInvoiceRequest` (3-34): `int SinvNo`, `DateTime Date`, `int CustomerCode`, `string? PayMode`, `int CashLedgerCode`, `int BankLedgerCode`, `DateTime? DueDate`, `int SalesmanCode`, `string? Pos`, `string? PcName`, `decimal DiscPerc, DiscAmt, RoundOff`, `List<SalesInvoiceItemRequest> Items`, `List<SalesInvoiceAdditionalRequest> Additional`.
- `SalesInvoiceItemRequest` (36-91): `int SlNo, PrCode`; `decimal Qty, Rate, PrRate, Mrp, LDiscPerc, LDiscPrice, DiscPerc, DiscPrice`; `string Batch`; `DateTime? MfgDate, ExpDate`; `int UnitCode, GdwCode, SordNo, OrdRowId`; `string AlCode, AlName`; `int ConvType`; `decimal ConvFact, Taxable`; `int TaxCode`; `decimal TaxPerc, TaxAmount, NetAmount`; `List<SalesInvoiceSerialRequest> SerialNumbers`.
- `SalesInvoiceSerialRequest` (93-104): `string InvRType`, `int InVrNo, InOrdNo, InSlNo`, `string ItemSlNo`.
- `SalesInvoiceAdditionalRequest` (106-111): `int AccId`, `decimal Amount`.

**TVP build** (`SalesInvoiceDB.CreateItemTable`, SalesInvoiceDB.cs:22-140)
- Item table, 26 columns (25-50): SLNO, PRCODE, QTY, RATE, PRRATE, MRP, LDISCPERC, LDISCPRICE, DISCPERC, DISCPRICE, BATCH (string), MFGDATE (DateTime), EXPDATE (DateTime), UNITCODE, GDWCODE, SORDNO, ORDROWID, ALCODE, ALNAME, CONVTYPE, CONVFACT, TAXABLE, TAXCODE, TAXPERC, TAXAMOUNT, NETAMOUNT. Row order matches (76-103). Null `MfgDate`/`ExpDate` are already replaced by `request.Date` (72-73) so the `DBNull.Value` branches (88-89) are never taken.
- Serial table, 9 columns (53-61): INVRTYPE string, INVRNO int, INORDNO int, INSLNO int, PRCODE int, OUTSLNO int, UNITCODE int, GDWCODE int, ITEMSLNO string. Each row copies `PrCode`, `UnitCode`, `GdwCode` from the parent item and sets `OUTSLNO = item.SlNo` (111-121).
- Additional table, 2 columns: ACCID, AMOUNT (128-129).

**SP call** (SalesInvoiceDB.cs:142-183): `ASK_SP_ManageSalesInvoiceDetails` with `@SINVNO int`, `@DATE datetime`, `@CUSCODE int`, `@PAYMODE nvarchar(100)`, `@CLCODE int`, `@BLCODE int`, `@DUEDATE datetime`, `@SMCODE int`, `@POS nvarchar(10)`, `@PCNAME nvarchar(max)`, `@DISCPERC`/`@DISCAMT`/`@ROUNDOFF` dec(18,6), `@USERID int`, `@IDEMPOTENCYKEY nvarchar(100)`, `@REQUESTHASH char(64)`, `@ITEMDTLS` = `dbo.ASK_UDT_SalesInvoiceItem`, `@SLNODTLS` = `dbo.ASK_UDT_SalesInvoiceSerialNumber`, `@ADDIDTLS` = `dbo.ASK_UDT_SalesInvoiceAdditionalInfo`.

**Create vs update:** `SinvNo == 0` create, else update. **Idempotency:** required header, hashed request (see 2.2). **Transaction:** inside SP only. **Controller checks:** same three as Order plus the header check (SalesInvoiceController.cs:70-91).

## 3.3 Sales Return

**Request DTO** (DTOs/SalesReturnRequest.cs)
- `SalesReturnRequest` (3-32): `int SretNo`, **`int SinvNo`** (original invoice, 0 = return without invoice, see SP SR:76), `DateTime Date`, `int CustomerCode`, `string? PayMode`, `int CashLedgerCode`, `int BankLedgerCode`, `DateTime? DueDate`, `int SalesmanCode`, `decimal DiscPerc, DiscAmt, RoundOff`, `Items`, `Additional`. (No `Pos`/`PcName`.)
- `SalesReturnItemRequest` (34-89), `SalesReturnSerialRequest` (91-102), `SalesReturnAdditionalRequest` (104-109): identical field lists to the invoice versions.

**TVP build** (`SalesReturnDB.CreateItemTable`, SalesReturnDB.cs:22-140): identical column lists and logic to Invoice (26 item columns, 9 serial columns, 2 additional columns), with the return UDT types named in the SP call.

**SP call** (SalesReturnDB.cs:142-175): `ASK_SP_ManageSalesReturnDetails` with `@SRETNO`, `@SINVNO`, `@DATE`, `@CUSCODE`, `@PAYMODE nvarchar(100)`, `@CLCODE`, `@BLCODE`, `@DUEDATE`, `@SMCODE`, `@DISCPERC/@DISCAMT/@ROUNDOFF dec(18,6)`, `@USERID`, `@ITEMDTLS` = `dbo.ASK_UDT_SalesReturnItem`, `@SLNODTLS` = `dbo.ASK_UDT_SalesReturnSerialNumber`, `@ADDIDTLS` = `dbo.ASK_UDT_SalesReturnAdditionalInfo`. No idempotency parameters.

**Create vs update:** `SretNo == 0` create, else update. **Transaction:** inside SP only. **Controller checks:** same three as Order (SalesReturnController.cs:62-69); no check that `SinvNo` is supplied (the SP decides: SR:76 mentions a `@SRWITHOUTINV` setting).

## 3.4 Observations for new code (from Parts 1-3 only)

- Because `AlCode`/`AlName`/`NetAmount` are overwritten server-side, a new layered implementation should either keep that behaviour deliberately or drop those request fields.
- `ExecuteStoredProcedure` does not catch exceptions; every sales controller wraps the call in try/catch and returns `ex.Message`.
- SP-level `ROLLBACK` behaviour and returned `STATUS`/`MESSAGE` contract need Run 2 to confirm.

**INCOMPLETE (known gaps in Run 1):** list/get/cancel/pending bodies of Invoice and Return controllers, `SalMDB.List`, `CordMDB.SearchByOrdNOAsync` SQL, `PaginationRequest` property limits, `CloneSqlParameter` body, `CommonFunction` after line 172, auth/CORS configuration in Program.cs. These were marked NOT READ above rather than guessed.
