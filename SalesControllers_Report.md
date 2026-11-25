# Sales Transaction Controllers — Report

**Folder:** `Controllers/Transactions/Sales/`
**Date:** 2026-11-23
**Scope:** `SalesOrderController.cs`, `SalesInvoiceController.cs`, `SalesReturnController.cs` and the DB classes they call (`SalesOrderDB`, `SalesInvoiceDB`, `SalesReturnDB`, `CordMDB`/`CordTDB`/`CordDDB`, `SalMDB`/`SalTDB`/`SalDDB`, `SntStkDB`).

---

## 1. Summary

| Controller | Route prefix | Swagger | Endpoints | DB target |
|---|---|---|---|---|
| `SalesOrderController` | `api/v1/sales-orders` | Visible, tag `25. Sales Orders` | 5 | Company DB |
| `SalesInvoiceController` | `api/v1/sales-invoices` | Hidden (`IgnoreApi = true`) | 5 | **MasterDB** for manage/list/cancel; company DB for get/pending |
| `SalesReturnController` | `api/v1/sales-returns` | Hidden (`IgnoreApi = true`) | 4 | Company DB |

All three use the **legacy port style** (see `CLAUDE.md`): each action builds its own `Variable` → `DatabaseConnection` → `DBConn` → `CommonFunction.SetVariableValues(...)` context. There is no service layer, no logger, and no DI for the DB classes. All are `[Authorize]` and use the RPC-style action names (`manage`, `list`, `get/{id}`, `cancel/{id}`).

Writes go through stored procedures with table-valued parameters. Reads use inline SQL in the `*MDB/*TDB/*DDB` classes (string-concatenated).

---

## 2. Endpoint Inventory

### 2.1 SalesOrderController — `api/v1/sales-orders`

| Method | Route | Action | Input | Backend | Response |
|---|---|---|---|---|---|
| POST | `manage` | `ManageSalesOrder` | Body `SalesOrderRequest` | SP `ASK_SP_ManageSalesOrderDetails` (TVPs `ASK_UDT_SalesOrderItem`, `ASK_UDT_SalesOrderAdditionalInfo`) | 201 if SP message = `"Saved"`, else 200; 400 if SP `STATUS != 1` |
| GET | `list` | `ListSalesOrder` | Query `PaginationRequests` (`page`, `pageSize`, `search`) + `fromDate`, `toDate` | `CordMDB.ListAsync("O")` (inline SQL on `CORDM` + `ACCMAS`) | 200 with `data[]` + `pagination` |
| GET | `get/{id:int}` | `GetSelectedSalesOrder` | Route `id` (ORDNO) | `CordMDB.SearchByOrdNOAsync`, `CordTDB.SearchByOrdNoAsync`, `CordDDB.SearchAddInfoAsync` | 200 typed `SelectedSalesOrderResponse` (`Header`, `Items`, `AdditionalInformations`); 404 if not found |
| POST | `cancel/{id:int}` | `CancelSalesOrder` | Route `id` | SP `ASK_SP_CancelSalesOrderDetails` | 200 / 400 per SP status |
| GET | `pending-list` | `ListPendingSalesOrderInInvoices` | Query `PaginationRequest` (`customerId`, `orderId`, `page`, `pageSize`, `search`, `sortBy`, `sortDirection`) | SP `ASK_SP_PendingSalesOrderInInvoices` | 200; includes `pagination` only when `orderId == 0` (header list), otherwise item list without pagination |

### 2.2 SalesInvoiceController — `api/v1/sales-invoices`

| Method | Route | Action | Input | Backend | Response |
|---|---|---|---|---|---|
| POST | `manage` | `ManageSalesInvoice` | Body `SalesInvoiceRequest`; **required header `Idempotency-Key`** (≤ 100 chars) | SP `ASK_SP_ManageSalesInvoiceDetails` (TVPs item / serial number / additional info; also `@IDEMPOTENCYKEY`, `@REQUESTHASH` = SHA-256 of request JSON) | 201 / 200 / 400 per SP |
| GET | `list` | `ListSalesInvoice` | Query `PaginationRequest` | `SalMDB.List` with `SType = "O"` (inline SQL on `SALM`) | 200 with `data[]` + `pagination` |
| GET | `get/{id:int}` | `GetSelectedSalesInvoice` | Route `id` (INVNO) | `SalMDB.GetInvoiceAsync` (typed `SalesInvoiceResponseDto`) | 200; **400** if not found |
| POST | `cancel/{id:int}` | `CancelSelectedSalesInvoice` | Route `id` | SP `ASK_SP_CancelSalesInvoiceDetails` | 200 / 400 per SP |
| GET | `pending` | `ListPendingSalesInvoicesInReturn` | Query `salesReturnId`, `SalesInvoiceNumber` | SP `ASK_SP_PendingSalesInvoicesInReturn` + `SalMDB.SearchByInvno` | 200 with top-level `Header` + `Items` (no `data` wrapper) |

### 2.3 SalesReturnController — `api/v1/sales-returns`

| Method | Route | Action | Input | Backend | Response |
|---|---|---|---|---|---|
| POST | `manage` | `ManageSalesReturn` | Body `SalesReturnRequest` | SP `ASK_SP_ManageSalesReturnDetails` (TVPs `ASK_UDT_SalesReturnItem`, `…SerialNumber`, `…AdditionalInfo`) | 201 / 200 / 400 per SP |
| GET | `list` | `ListSalesReturn` | Query `PaginationRequest` | `SalMDB.List` with `SType = "R"` | 200 with `data[]` + `pagination` |
| GET | `get/{id:int}` | `GetSelectedSalesReturn` | Route `id` | `SalMDB.SearchByInvno`, `SalTDB.SearchByInvno`, `SntStkDB.GetByVoucherIn` (`INVRTYPE = "SR"`), `SalDDB.SearchAddInfo` (`VrType = "R"`), `SalMDB.SearchByOrdNo` for the linked invoice number | 200 with `Header`, `salesinvoicenumber`, `Items`, `SerialNumbers`, `AdditionalInformations` (DataTable → dictionaries) |
| POST | `cancel/{id:int}` | `CancelSelectedSalesReturn` | Route `id` | SP `ASK_SP_CancelSalesReturnDetails` | 200 / 400 per SP |

---

## 3. Common Request Flow (write endpoints)

1. `LngTB.InitLng()`; build `Variable` with `UserId` from the `NameIdentifier` claim.
2. Build connection string via `DatabaseConnection.GetConnectionString(...)`, create `DBConn` + `CommonFunction`.
3. `CheckDatabaseConnection()` → 500 if it fails.
4. `SetVariableValues(Variables)` loads company settings (FY, decimals, VRSYS, …).
5. Controller-level validation: request not null, `Items` not empty, `CustomerCode != 0`.
6. `*DB.Manage…` normalises the request and fills TVP `DataTable`s:
   - `Mrp = Rate` when 0, `GdwCode = 1` when 0, `ConvFact = 1` when 0, `AlCode = "0"`, `NetAmount = Taxable + TaxAmount` (server recalculates; client value is ignored).
   - Invoice/return: `PayMode` mapped to `Cash` / `Credit` / `Bank` (anything else → `Bank`), `DueDate` defaults to `Date`, `MfgDate`/`ExpDate` default to `Date`.
   - Invoice only: `Pos` → `Y`/`N`, `PcName = "API"`.
7. SP returns one row with `STATUS` and `MESSAGE`; the controller maps it to 201/200/400.

Create vs. update is decided by the SP based on the header id in the body (`SordNo` / `SinvNo` / `SretNo`; 0 = new).

---

## 4. Issues Found

Ordered by severity.

### High

1. **Sales invoice manage / list / cancel run against `MasterDB`.**
   `SalesInvoiceController.cs:45`, `:139`, `:613` call `GetConnectionString(Variables, isMasterDB: true)`, while `get` and `pending` (`:245`, `:674`) and all order/return actions use the company DB. The invoice SPs, `SALM` and the UDTs are defined in the company schema script (`DB/ProjectDB/QuickBalance.sql`). With `isMasterDB: true`, `CompCode` is also never set and `SetVariableValues` reads settings from the wrong database. Likely effects: SP not found / wrong data on save and cancel, and the list call returns an **empty list instead of an error** (see #3).

2. **`GetSelectedSalesReturn` reads `dtHdr.Rows[0]` before checking for "not found".**
   `SalesReturnController.cs:249` dereferences `dtHdr.Rows[0]["ORDNO"]`; the empty check is at `:257`. A non-existent id throws `IndexOutOfRangeException` → HTTP 500 with the raw exception message instead of 404. `Convert.ToInt32(dtHdr.Rows[0]["RTNINVNO"])` (`:263`) also throws if the column is `NULL`.

3. **`SalMDB.List` swallows all exceptions** (`SalMDB.cs:88-91`) and returns an empty `DataTable`. Invoice and return list endpoints then answer `200 "No … details found."` for SQL errors, hiding real failures (including issue #1).

4. **Sales return SP and UDTs are missing from the schema script.**
   `QuickBalance.sql` contains `ASK_SP_CancelSalesReturnDetails` but **not** `ASK_SP_ManageSalesReturnDetails`, `ASK_UDT_SalesReturnItem`, `ASK_UDT_SalesReturnSerialNumber` or `ASK_UDT_SalesReturnAdditionalInfo`. Either the script is out of date or `POST sales-returns/manage` will fail on a fresh database.

### Medium

5. **Sales order list has no paging normalisation.** `ListSalesOrder` takes `PaginationRequests` but never calls `Normalizes()` (`SalesOrderController.cs:113` is commented out). `pageSize=0` or `page=0` produce invalid `OFFSET/FETCH` SQL (500) and `totalRecords / 0.0` in the page-count calculation; there is no upper cap on `pageSize`.

6. **SQL built by string concatenation in read paths.** `CordMDB.ListAsync` and `SalMDB.List` embed `search` (quote-escaped only) and dates directly into SQL; `SalMDB.SearchByInvno` / `SearchByOrdNo` concatenate ids. Ints are safe, but the search pattern should be parameterised.

7. **Raw exception messages returned to clients.** Nearly every `catch` returns `ex.Message` with HTTP 500 (only the two `get/{id}` actions in Order/Invoice return a generic message). No logging anywhere, so the generic-message paths lose the error entirely.

8. **`DBConn` is never disposed.** `DBConn` implements `IDisposable`, but each action does `new DBConn(...)` without `using`. `SalesReturnController.ListSalesReturn` (`:121`) also creates a second, unused `DBConn db`.

9. **`pending` (invoice) query parameter `SalesInvoiceNumber` is non-nullable `string`** with `<Nullable>enable</Nullable>`, so `[ApiController]` treats it as required — callers get a 400 if they omit it even when only `salesReturnId` is meaningful. When nothing is pending, `SearchByInvno` still runs with `InvNo = 0`.

### Low / Consistency

10. **Inconsistent "not found" status:** invoice `get` returns **400**, order and return `get` return **404**.
11. **Inconsistent response shapes:**
    - Order `get` returns a typed DTO; invoice `get` returns a typed DTO; return `get` returns raw DataTable dictionaries (uppercase DB column names).
    - Invoice `pending` returns `Header`/`Items` at top level without a `data` wrapper.
    - Order `pending-list` omits `pagination` when `orderId > 0`.
12. **Inconsistent paging DTOs:** order list uses `PaginationRequests` + separate `fromDate`/`toDate` query params; invoice/return list use `PaginationRequest` (which already has `FromDate`, `ToDate`, `SortBy`, `SortDirection`). Order list ignores sorting.
13. **Inconsistent user-id source:** `ManageSalesOrder` uses `ICurrentUser.UserId`; every other action parses the claim manually (and some use `int.Parse`, others `int.TryParse`).
14. **Idempotency only on invoices.** Orders and returns have no protection against double-submit. In the invoice action the `Idempotency-Key` check happens *after* the DB connection and `SetVariableValues`, so a missing header still costs two DB round trips.
15. **Redundant nested `try/catch`** blocks with identical handlers in every write/list action.
16. **Dead code:** large commented-out older versions of `GetSelected…` in both Order and Invoice controllers; unused `using System.Reflection.PortableExecutable;` (Order, Invoice) and `using Azure.Core;` (`SalesOrderDB`). `CancelSalesInvoice` takes an unused `CommonFunction` parameter.
17. **Swagger visibility:** only Sales Orders is documented. Invoice and Return controllers are hidden (`IgnoreApi = true`) and have no `[Tags]`; if they are meant to be public, they need tags (next free numbers are 26+).

---

## 5. Recommendations

1. Remove `isMasterDB: true` from the three invoice actions (or confirm in writing that the invoice SPs really live in `MasterDB`).
2. Move the `dtHdr` empty check above the first `Rows[0]` access in `GetSelectedSalesReturn`, and null-guard `RTNINVNO`.
3. Stop swallowing exceptions in `SalMDB.List`; let them reach the controller.
4. Add the missing sales-return SP/UDT definitions to `QuickBalance.sql` (or regenerate the script).
5. Normalise paging in `ListSalesOrder` (call `Normalizes()` or switch to `PaginationRequest`).
6. Standardise: 404 for not found, one paging DTO, one response envelope (`ApiResponse<T>`), `ICurrentUser` for the user id.
7. Wrap `DBConn` in `using`, add `ILogger<T>`, return generic 500 messages and log the exception.
8. Longer term: move these actions into the layered style (`ISalesOrderService` → repository) with DI-registered classes, as described in `CLAUDE.md`.

---

## 6. File References

| File | Purpose |
|---|---|
| `Controllers/Transactions/Sales/SalesOrderController.cs` | Sales order endpoints |
| `Controllers/Transactions/Sales/SalesInvoiceController.cs` | Sales invoice endpoints |
| `Controllers/Transactions/Sales/SalesReturnController.cs` | Sales return endpoints |
| `DB/SalesOrderDB.cs`, `DB/SalesInvoiceDB.cs`, `DB/SalesReturnDB.cs` | SP calls + TVP construction |
| `DB/CordMDB.cs`, `CordTDB.cs`, `CordDDB.cs` | Order header / items / additional-info reads |
| `DB/SalMDB.cs`, `SalTDB.cs`, `SalDDB.cs`, `SntStkDB.cs` | Invoice/return header, items, additional info, serial numbers |
| `DTOs/SalesOrderRequest.cs`, `SalesInvoiceRequest.cs`, `SalesReturnRequest.cs` | Request bodies |
| `DTOs/PaginationRequest.cs` | `PaginationRequest` and `PaginationRequests` |
| `DTOs/Respons-DTO/SelectedSalesOrderResponse.cs` | Typed order detail response |
| `Configuration/DatabaseConnection.cs` | Connection string builder (`isMasterDB` switch) |
