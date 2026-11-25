# Sales module – current Web API review

Scope: `Controllers/Transactions/Sales/` (`SalesOrderController.cs`, `SalesInvoiceController.cs`, `SalesReturnController.cs`) and the DB classes they call.
Last updated: 2026-11-23. Line numbers refer to the API source as of this date. No code has been changed as part of this review.

---

## 1. Status

| Form | API status | Basis |
|---|---|---|
| **Sales Order** | **Working.** Still to be verified against the legacy `SalesOrder.cs` save chain (Phase 3) | SPs and UDTs exist in the company DB and the controller uses the company DB |
| **Sales Invoice** | **Save, list and cancel are broken.** `get` and `pending` work | Issue A1: the three actions connect to `MasterDB` |
| **Sales Return** | **Save is missing.** List, get and cancel are wired up (get has a crash bug, A3) | Issue A2: the save SP and its UDTs were never deployed |

## 2. Database verification (2026-11-23)

Checked against the live databases `COMP91_2026_2026` (company) and `MasterDB`.

| Object | `COMP91_2026_2026` | `MasterDB` | Used by |
|---|---|---|---|
| `ASK_SP_ManageSalesInvoiceDetails` | Yes | No | `POST sales-invoices/manage` |
| `ASK_SP_CancelSalesInvoiceDetails` | Yes | No | `POST sales-invoices/cancel/{id}` |
| `ASK_SP_PendingSalesInvoicesInReturn` | Yes | No | `GET sales-invoices/pending` |
| `ASK_UDT_SalesInvoiceItem` / `…SerialNumber` / `…AdditionalInfo` | Yes | No | Invoice save |
| `ASK_SP_ManageSalesOrderDetails` | Yes | No | `POST sales-orders/manage` |
| `ASK_SP_CancelSalesOrderDetails` | Yes | No | `POST sales-orders/cancel/{id}` |
| `ASK_SP_PendingSalesOrderInInvoices` | Yes | No | `GET sales-orders/pending-list` |
| `ASK_UDT_SalesOrderItem` / `…AdditionalInfo` | Yes | No | Order save |
| `ASK_SP_CancelSalesReturnDetails` | Yes | No | `POST sales-returns/cancel/{id}` |
| **`ASK_SP_ManageSalesReturnDetails`** | **No** | No | `POST sales-returns/manage` |
| **`ASK_UDT_SalesReturnItem` / `…SerialNumber` / `…AdditionalInfo`** | **No** | No | Return save |

`MasterDB` contains no `ASK_` objects at all. The DB script `QuickBalance.sql` matches the live company DB: it has no sales-return save SP or UDTs either.

---

## 3. Endpoint inventory

All three controllers use the legacy port style (`Variable` → `DatabaseConnection` → `DBConn` → `CommonFunction.SetVariableValues` per action), are `[Authorize]`, have no service layer or logger, and use RPC-style routes.

### 3.1 Sales Order — `api/v1/sales-orders` (Swagger tag `25. Sales Orders`)

| Method | Route | Backend | Notes |
|---|---|---|---|
| POST | `manage` | `ASK_SP_ManageSalesOrderDetails` | 201 when SP message is `"Saved"`, else 200; 400 when SP `STATUS != 1` |
| GET | `list` | `CordMDB.ListAsync("O")` | Query `PaginationRequests` + `fromDate`/`toDate`. No paging normalisation (A5) |
| GET | `get/{id:int}` | `CordMDB`/`CordTDB`/`CordDDB` | Typed `SelectedSalesOrderResponse`; 404 when not found |
| POST | `cancel/{id:int}` | `ASK_SP_CancelSalesOrderDetails` | |
| GET | `pending-list` | `ASK_SP_PendingSalesOrderInInvoices` | `pagination` only when `orderId == 0` |

### 3.2 Sales Invoice — `api/v1/sales-invoices` (hidden from Swagger)

| Method | Route | Backend | DB | Notes |
|---|---|---|---|---|
| POST | `manage` | `ASK_SP_ManageSalesInvoiceDetails` | **MasterDB** ✗ | Requires `Idempotency-Key` header; sends SHA-256 request hash |
| GET | `list` | `SalMDB.List` (`STYPE = 'O'`) | **MasterDB** ✗ | Returns an empty list instead of an error (A4) |
| GET | `get/{id:int}` | `SalMDB.GetInvoiceAsync` | Company | Returns **400** when not found |
| POST | `cancel/{id:int}` | `ASK_SP_CancelSalesInvoiceDetails` | **MasterDB** ✗ | |
| GET | `pending` | `ASK_SP_PendingSalesInvoicesInReturn` + `SalMDB.SearchByInvno` | Company | `Header`/`Items` at top level, no `data` wrapper |

### 3.3 Sales Return — `api/v1/sales-returns` (hidden from Swagger)

| Method | Route | Backend | Notes |
|---|---|---|---|
| POST | `manage` | `ASK_SP_ManageSalesReturnDetails` | **SP and UDTs don't exist** (A2) |
| GET | `list` | `SalMDB.List` (`STYPE = 'R'`) | |
| GET | `get/{id:int}` | `SalMDB`/`SalTDB`/`SntStkDB` (`INVRTYPE='SR'`)/`SalDDB` | Crashes on unknown id (A3); returns raw DataTable dictionaries |
| POST | `cancel/{id:int}` | `ASK_SP_CancelSalesReturnDetails` | |

---

## 4. Issues

### Confirmed (DB check 2026-11-23)

**A1. Invoice save, list and cancel connect to `MasterDB`.**
`SalesInvoiceController.cs:45` (`manage`), `:139` (`list`) and `:613` (`cancel`) call `GetConnectionString(Variables, isMasterDB: true)`. `MasterDB` has no `ASK_` objects and no `SALM`, so save and cancel fail. `CompCode` is also never set, and `SetVariableValues` reads settings from `MasterDB`. The `get` (`:245`) and `pending` (`:674`) actions correctly use the company DB.

**A2. Sales Return save was never deployed.**
`SalesReturnDB.ManageSalesReturn` calls `ASK_SP_ManageSalesReturnDetails` with TVPs `dbo.ASK_UDT_SalesReturnItem`, `dbo.ASK_UDT_SalesReturnSerialNumber` and `dbo.ASK_UDT_SalesReturnAdditionalInfo`. None of these exist in the company DB or in `QuickBalance.sql`. Only `ASK_SP_CancelSalesReturnDetails` exists.

**A4. `SalMDB.List` swallows exceptions** (`SalMDB.cs:88-91`) and returns an empty `DataTable`. Combined with A1, the invoice list answers `200 "No sales invoice details found."` instead of reporting the failure.

### Open (from code review, not yet tested)

**A3. Sales Return `get/{id}` reads `Rows[0]` before the not-found check.** `SalesReturnController.cs:249` reads `dtHdr.Rows[0]["ORDNO"]`; the empty check is at `:257`. An unknown id gives HTTP 500 with the raw exception message instead of 404. `Convert.ToInt32(dtHdr.Rows[0]["RTNINVNO"])` (`:263`) also throws on `NULL`.

**A5. Sales Order list has no paging normalisation.** `PaginationRequests.Normalizes()` is never called (`SalesOrderController.cs:113` commented out). `page=0` or `pageSize=0` produce invalid `OFFSET/FETCH` and a division by zero in the page count; `pageSize` has no upper cap.

**A6. String-concatenated SQL in read paths.** `CordMDB.ListAsync` and `SalMDB.List` embed `search` (quote-escaped only) and dates; `SalMDB.SearchByInvno` / `SearchByOrdNo` concatenate ids.

**A7. Raw `ex.Message` returned with HTTP 500** in almost every catch block. No logging anywhere.

**A8. `DBConn` is never disposed** (it implements `IDisposable`). `SalesReturnController.ListSalesReturn:121` creates a second, unused `DBConn`.

**A9. Invoice `pending` requires `SalesInvoiceNumber`.** It is a non-nullable `string` with `<Nullable>enable</Nullable>`, so `[ApiController]` returns 400 when it is omitted.

**A10. Inconsistencies between the three controllers:**
- Not found: invoice returns 400; order and return return 404.
- Response shapes: typed DTOs (order/invoice `get`), raw dictionaries with DB column names (return `get`), no `data` wrapper (invoice `pending`).
- Paging DTOs: order list uses `PaginationRequests`; invoice/return lists use `PaginationRequest` (with sorting).
- User id: `ManageSalesOrder` uses `ICurrentUser`; the other actions parse the claim (`int.Parse` or `int.TryParse`).
- Idempotency key exists only on invoice save, and is checked after two DB round trips.

**A11. Housekeeping:** redundant nested `try/catch`; large commented-out `GetSelected…` blocks; unused `using System.Reflection.PortableExecutable;` and `using Azure.Core;`; unused `CommonFunction` parameter on `SalesInvoiceDB.CancelSalesInvoice`; invoice and return controllers hidden from Swagger with no `[Tags]`.

---

## 5. Required work items

These are tracked in `00-scope.md` §5. No code is changed until the plan is approved.

| # | Item | Fixes | Notes |
|---|---|---|---|
| **W1** | Remove `isMasterDB: true` from the three invoice actions (`SalesInvoiceController.cs:45`, `:139`, `:613`) | A1 | Then retest invoice save, list and cancel against `COMP91_2026_2026` |
| **W2** | Design and create `ASK_SP_ManageSalesReturnDetails` and UDTs `ASK_UDT_SalesReturnItem`, `ASK_UDT_SalesReturnSerialNumber`, `ASK_UDT_SalesReturnAdditionalInfo` | A2 | Derive behaviour from the legacy `SalesReturn.cs` save chain (Phase 2 analysis); follow the structure of `ASK_SP_ManageSalesInvoiceDetails`. UDT columns must match the `DataTable`s built in `SalesReturnDB.CreateItemTable`. Add the objects to `QuickBalance.sql` |

The other issues (A3–A11) are not yet scheduled. A4 should be fixed alongside W1 so list failures are visible during retesting.
