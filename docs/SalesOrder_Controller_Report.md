 # SalesOrderController Report

Source: `Controllers/Transactions/Sales/SalesOrderController.cs`. This report covers only that controller and the DB methods and models it calls directly. It doesn't trace further into `CommonFunction`, `Function`, `DBConn` or the stored procedure bodies, which live in SQL Server and aren't in this repo.

---

## 1. Overview

| Item | Value |
|---|---|
| Class | `QuickBalance.API.Controllers.Transactions.Sales.SalesOrderController` |
| Base route | `api/v1/sales-orders` |
| Swagger tag | `25. Sales Orders` (visible in Swagger: `IgnoreApi = false`) |
| Auth | `[Authorize]` on the class. The global fallback policy also requires a positive `NameIdentifier` claim (see `CLAUDE.md`). |
| Style | Legacy port style. Each action builds its own `Variable`, `DBConn` and `CommonFunction`. **There is no service layer**, so the controller calls `SalesOrderDB` / `CordMDB` / `CordTDB` / `CordDDB` directly. |
| Injected | `DatabaseConnection` (connection-string builder) and `ICurrentUser` (the only action that uses it is `manage`) |

Purpose: create or update sales orders, list them, fetch one order with its items and additional info, cancel an order, and list orders that are still pending invoicing. In the database a sales order is a `CORDM` / `CORDT` / `CORDD` row with `STYPE` / `VRTYPE = 'O'`.

---

## 2. Actions

| # | Verb | Route | Request model | Response (success) | Purpose |
|---|---|---|---|---|---|
| 1 | POST | `/api/v1/sales-orders/manage` | Body: `SalesOrderRequest` | 201 or 200, anonymous `{ status, message, data: { message } }` | Create or update a sales order through a stored procedure |
| 2 | GET | `/api/v1/sales-orders/list` | Query: `PaginationRequests` (`page`, `pageSize`, `search`) + `fromDate`, `toDate` | 200, anonymous `{ status, message, data: [dict], pagination: PaginationResponse }` | Paged list of sales orders |
| 3 | GET | `/api/v1/sales-orders/get/{id:int}` | Route: `id` (order no.) | 200, `SelectedSalesOrderResponse` | One order: header, items and additional info |
| 4 | POST | `/api/v1/sales-orders/cancel/{id:int}` | Route: `id` (order no.) | 200, anonymous `{ status, message, data: { message } }` | Cancel a sales order through a stored procedure |
| 5 | GET | `/api/v1/sales-orders/pending-list` | Query: `PaginationRequest` (`customerId`, `orderId`, `page`, `pageSize`, `search`, `sortBy`, `sortDirection`, …) | 200, anonymous `{ status, message, data: [dict], pagination? }` | Orders, or one order's items, still pending in invoices |

Errors from every action are anonymous `{ status: false, message, errors: [] }` objects. They are **not** the `ApiResponse<T>` / `ApiResponseWithError` envelopes used by the newer controllers.

---

## 3. Action flows

### Common setup (all actions)

1. `LngTB.InitLng()`.
2. `new Variable()` and set `UserId`:
   - `manage`: `_currentUser.UserId`.
   - Others: read the `ClaimTypes.NameIdentifier` claim. If it's missing, return **401**. `list`, `cancel` and `pending-list` use `int.Parse`. `get` uses `int.TryParse` and returns 401 on failure.
3. `_databaseConnection.GetConnectionString(Variables)` → `new DBConn(...)` → `new CommonFunction(DBConn)`.
4. `commonFunction.CheckDatabaseConnection()`. If it returns false, the action returns **500** "Database connection failed."
5. `commonFunction.SetVariableValues(Variables)` loads company settings (date format, decimal string, VRSYS etc.). *What it loads exactly: needs confirmation (not traced).*

### 3.1 `POST manage`: create or update

**Request: `SalesOrderRequest`** (`DTOs/SalesOrderRequest.cs`)

| Field | Type | Notes |
|---|---|---|
| `sordNo` | int | Sent as `@SORDNO`. *Whether 0 means create and >0 means update: needs confirmation (decided inside the SP).* |
| `date` | DateTime | `@DATE` |
| `customerCode` | int | `@CUSCODE`. Must be ≠ 0. |
| `salesmanCode` | int | `@SMCODE` |
| `discPerc`, `discAmt`, `roundOff` | decimal | Bill-level additional discount % and amount, and round-off |
| `items` | `List<SalesOrderItemRequest>` | Must contain at least one item |
| `additional` | `List<SalesOrderAdditionalRequest>` | `accId`, `amount` |

`SalesOrderItemRequest` fields: `slNo, prCode, qty, rate, lDiscPerc, lDiscPrice, discPerc, discPrice, unitCode, gdwCode, alCode, alName, convType, convFact, taxable, taxCode, taxPerc, taxAmount, netAmount`.

**Steps**
1. Common setup.
2. Validation (in the controller; nothing in the DTO):
   - `request == null` → 400 "Order data is required."
   - `items` null or empty → 400 "At least one order item is required."
   - `customerCode == 0` → 400 "Party details are required."
3. `SalesOrderDB.ManageSalesOrder(request, UserId)`:
   - `CreateItemTable` builds two in-memory `DataTable`s and **changes each item** before adding it:
     - `AlCode` is forced to `"0"` and `AlName` to `""`, so client values are ignored.
     - `GdwCode` 0 → 1, `ConvFact` 0 → 1.
     - `NetAmount = Taxable + TaxAmount`, so the client's `netAmount` is ignored.
   - Item table columns: `SLNO, PRCODE, QTY, RATE, LDISCPERC, LDISCPRICE, DISCPERC, DISCPRICE, UNITCODE, GDWCODE, ALCODE, ALNAME, CONVTYPE, CONVFACT, TAXABLE, TAXCODE, TAXPERC, TAXAMOUNT, NETAMOUNT`.
   - Additional table columns: `ACCID, AMOUNT`.
   - Runs stored procedure **`ASK_SP_ManageSalesOrderDetails`** with `@SORDNO, @DATE, @CUSCODE, @SMCODE, @DISCPERC, @DISCAMT, @ROUNDOFF` (decimals 18,6), `@USERID`, `@ITEMDTLS` (TVP `dbo.ASK_UDT_SalesOrderItem`) and `@ADDIDTLS` (TVP `dbo.ASK_UDT_SalesOrderAdditionalInfo`), through `DBConn.ExecuteStoredProcedure`.
4. Result handling (the SP must return a row with `STATUS` and `MESSAGE`):
   - 0 rows → 500 "No response received from sales order procedure."
   - `STATUS == 1` and `MESSAGE == "Saved"` → **201** `Created("")` (empty Location).
   - `STATUS == 1` with any other message → **200**. *Presumably an update; the exact message text needs confirmation.*
   - Any other `STATUS` → **400** with the SP's `MESSAGE`.
5. An exception → 500 with `ex.Message`.

**Tables touched:** *needs confirmation.* Everything happens inside `ASK_SP_ManageSalesOrderDetails`, most likely `CORDM`, `CORDT` and `CORDD`.

### 3.2 `GET list`: paged list

**Steps**
1. Copy `page`, `pageSize` and `search` from `PaginationRequests`, and `fromDate` / `toDate` from the query, into `Variables`. **No normalization:** `Normalizes()` is never called.
2. Common setup (claim → 401 if missing).
3. `CordMDB.ListAsync("O")` builds **concatenated SQL**:
   - `FROM CORDM INNER JOIN ACCMAS ON CORDM.CUSCODE = ACCMAS.ID`
   - `WHERE CORDM.STYPE = 'O'` + `Functions.LIST_ALLOW_VR_STMNT(true, "CORDM")` (*filter content needs confirmation*)
   - Search: `CORDM.<VSDOCNO()> LIKE '%s%' OR ACCMAS.ACCNAME LIKE '%s%'`. The only escaping is doubling `'` to `''`.
   - Dates: `ORDDATE >= fromDate` and `ORDDATE < toDate + 1 day`, formatted with `Variables.format`.
   - Columns: `Id` (ORDNO), `[Order No]` (`VSDOCNO()` column, *which column needs confirmation*), `[Order Date]`, `Customer`, `Amount` (formatted string), `COUNT(*) OVER() AS TotalRecords`.
   - `ORDER BY CORDM.ORDNO DESC OFFSET … FETCH NEXT …`
4. Read `TotalRecords` from row 0, drop that column, compute `totalPages`, convert rows to dictionaries.
5. Return 200 with "Sales order details loaded successfully." or "No sales order details found." An exception → 500 with `ex.Message`.

**Tables:** `CORDM`, `ACCMAS` (read only).

### 3.3 `GET get/{id}`: single order

**Steps**
1. `id <= 0` → 400 "Order number is required."
2. Common setup (claim → 401 if missing or invalid).
3. Header: `CordMDB.SearchByOrdNOAsync({ OrdNo = id, SType = "O" })`. This is parameterized Dapper:
   `CORDM.*` + `INVNO AS SALNO`, `ACCMAS.ACCNAME`, `SMMAST.SMNAME`, `TBUSER.FULLNAME AS USERNAME`. It inner-joins `ACCMAS` and left-joins `SMMAST` and `TBUSER`. The result maps to `SalesOrderHeaderDto`.
   - `null` → **404** "Sales order not found."
4. Items: `CordTDB.SearchByOrdNoAsync({ OrdNo = id, SType = "O" })`. This is parameterized:
   `CORDT.*` + `PRMAST` (name, alias, secname, unit, altunit, salespricetype, bmeqn) + `PRUNITMAS` (unit name, secname), filtered on `ORDNO` and `STYPE`. The result maps to `List<SalesOrderItemDto>` (`SL` is `[JsonIgnore]`).
5. Additional info: `CordDDB.SearchAddInfoAsync({ VrNo = id, VrType = "O", DFlag = false })`. This is parameterized:
   `SELECT CODE, TAX, TAXAMT FROM CORDD WHERE VRNO, VRTYPE, DFLAG AND CODE <> '0'` → `List<AddInfoDto>`.
6. Return 200 `SelectedSalesOrderResponse { status, message, data: { header, items, additionalInformations } }`.
7. An exception → 500 with a generic message. The exception is **not logged**: there's a comment saying it should be, but no logger is injected.

**Tables:** `CORDM`, `CORDT`, `CORDD`, `ACCMAS`, `SMMAST`, `TBUSER`, `PRMAST`, `PRUNITMAS` (read only).

### 3.4 `POST cancel/{id}`: cancel order

**Steps**
1. `id <= 0` → 400 "Order number is required."
2. Common setup (claim → 401 if missing).
3. `SalesOrderDB.CancelSalesOrder(id, UserId)` runs SP **`ASK_SP_CancelSalesOrderDetails`** with `@SORDNO`, `@USERID`.
4. 0 rows → 500. `STATUS == 1` → 200 with `MESSAGE`. Otherwise 400 with `MESSAGE`.
5. An exception → 500 with `ex.Message`.

**Tables / behaviour:** *needs confirmation* (inside the SP). Things to confirm: whether it sets `CANCELFLG`, and whether it blocks cancelling orders that are already invoiced.

### 3.5 `GET pending-list`: orders pending in invoices

**Steps**
1. `request.Normalize()`: page < 1 → 1, pageSize < 1 → 20, pageSize > 100 → 100, and `sortDirection` is forced to ASC or DESC. `Variables.pgRequest = request`.
2. Common setup (claim → 401 if missing).
3. `SalesOrderDB.PendingSalesOrderInInvoices(request)` runs SP **`ASK_SP_PendingSalesOrderInInvoices`** with:
   - `@CUSTOMERID`, `@ORDERID`, `@PAGE`, `@PAGESIZE`, `@SEARCH` (`""` if null)
   - `@SORTBY`: null → `""`; `"ordNo"` or `"orderNo"` → `"no"`; **any other value → `"date"`**
   - `@SORTDIRECTION`: null → `""`; otherwise `ASC` or `DESC`
4. If the result has a `TotalRecords` column, read it and remove it.
5. Response:
   - `orderId == 0`: 200 with `data` and `pagination` (order-level list).
   - `orderId != 0`: 200 with `data` only and **no pagination** (item-level list for that order).
6. An exception → 500 with `ex.Message`.

**Tables:** *needs confirmation* (inside the SP).

---

## 4. Dependencies

**Infrastructure**
- `DatabaseConnection` (injected): builds the connection string from `Variable`.
- `ICurrentUser` (injected): only `manage` uses it.
- `DBConn` (created with `new` in each action): `ExecuteStoredProcedure`, `GetDatatable`, `QueryFirstOrDefaultAsync`, `QueryListAsync`.
- `CommonFunction`: `CheckDatabaseConnection`, `SetVariableValues`.
- `Function` (via `CordMDB.Functions`): `VSDOCNO()`, `LIST_ALLOW_VR_STMNT(...)`.
- `LngTB.InitLng()`.

**DB classes and methods called**

| Class | Method | Kind |
|---|---|---|
| `DB/SalesOrderDB.cs` | `ManageSalesOrder` | SP `ASK_SP_ManageSalesOrderDetails` + 2 TVPs |
| `DB/SalesOrderDB.cs` | `CancelSalesOrder` | SP `ASK_SP_CancelSalesOrderDetails` |
| `DB/SalesOrderDB.cs` | `PendingSalesOrderInInvoices` | SP `ASK_SP_PendingSalesOrderInInvoices` |
| `DB/CordMDB.cs` | `ListAsync` | Concatenated SQL |
| `DB/CordMDB.cs` | `SearchByOrdNOAsync` | Parameterized Dapper |
| `DB/CordTDB.cs` | `SearchByOrdNoAsync` | Parameterized Dapper |
| `DB/CordDDB.cs` | `SearchAddInfoAsync` | Parameterized Dapper |

**Models**
- Request: `SalesOrderRequest`, `SalesOrderItemRequest`, `SalesOrderAdditionalRequest` (`DTOs/SalesOrderRequest.cs`); `PaginationRequests`, `PaginationRequest` (`DTOs/PaginationRequest.cs`).
- Response: `SelectedSalesOrderResponse`, `SelectedSalesOrderData`, `SalesOrderHeaderDto`, `SalesOrderItemDto`, `AddInfoDto` (`DTOs/Respons-DTO/SelectedSalesOrderResponse.cs`); `PaginationResponse` (`DTOs/PaginationResponse.cs`).
- Search keys: `ClsCordM`, `ClsCordT`, `ClsCordD` (`CLS/`).
- SQL types: `dbo.ASK_UDT_SalesOrderItem`, `dbo.ASK_UDT_SalesOrderAdditionalInfo`.

---

## 5. Unclear or risky points

### Validation gaps
1. **`manage`: no item-level validation.** `prCode = 0`, `qty <= 0`, negative `rate` and `unitCode = 0` all reach the SP. *Whether the SP rejects them needs confirmation.*
2. **`manage`: `date` isn't validated.** If it's omitted it defaults to `0001-01-01`, which is outside the SQL `DateTime` range. The likely result is a 500 that exposes the raw exception message. *Needs confirmation by testing.*
3. **`manage`: `salesmanCode`** and the `additional[].accId` values aren't validated.
4. **`manage`: client values are silently overwritten:** `netAmount` (recomputed), `alCode` and `alName` (forced to `"0"` and `""`), and `gdwCode` / `convFact` (0 → 1). The frontend should know this.
5. **`list`: pagination isn't normalized.** `page = 0` gives a negative `OFFSET`, and `pageSize = 0` gives `FETCH NEXT 0`. Both are SQL errors and return 500. `pageSize` has no upper limit. `fromDate > toDate` isn't checked.
6. **`cancel`, `pending-list`:** `customerId` and `orderId` aren't validated. `PaginationRequest.Type`, `FromDate` and `ToDate` are accepted but `pending-list` never uses them.

### Security
7. **`list` builds SQL by string concatenation** (`CordMDB.ListAsync`). The only protection for search is doubling `'`. Dates are formatted into the SQL, and `STYPE` is a hard-coded `"O"`. It's lower risk than raw input but still not parameterized, unlike the other read paths.
8. **Raw `ex.Message` is returned to clients** in `manage`, `list`, `cancel` and `pending-list`, which can expose SQL and schema details. Only `get` hides it.

### Behaviour and consistency
9. **Create vs update** depends on the SP returning exactly `MESSAGE == "Saved"` (201). Any other success message returns 200. A text change in the SP would silently change the HTTP status. `Created("")` sends an empty Location header and doesn't return the new order number.
10. **`get` returns cancelled orders.** There's no `CANCELFLG` filter on the header or items. *Whether that's intended needs confirmation.*
11. **`get` item ordering:** `ROW_NUMBER() OVER (ORDER BY (SELECT NULL))` runs over the **whole `CORDT` table** before filtering by order. That makes the order non-deterministic, and it could get slow on a large table. If item order matters, sort by a real column (e.g. a row ID or SLNO). *Which column needs confirmation.*
12. **`pending-list` `sortBy`:** any unrecognised value (e.g. `"customer"`) silently becomes `"date"`.
13. **The response shape changes:** `pending-list` drops `pagination` when `orderId != 0`.
14. **HTTP code inconsistency:** `get` returns **404** for not found, while the rest of the API returns 400 with `status:false`.
15. **Envelope inconsistency:** these actions return anonymous `{status, message, errors}` objects instead of `ApiResponse<T>`, and there are no `[ProducesResponseType]` attributes, so Swagger doesn't document the responses.
16. **User ID inconsistency:** `manage` uses `ICurrentUser`, and the other actions parse the claim themselves.
17. **No logging:** no `ILogger` is injected, so 500 errors aren't logged on the server.
18. **`DBConn` lifetime:** each action creates it with `new` and never disposes it explicitly. *Whether `DBConn` closes its connection after each call needs confirmation (not traced).*
19. **Dead code:** about 100 lines of the old `GetSelectedSalesOrder` are commented out, and there's an unused `using System.Reflection.PortableExecutable;`.

### Needs confirmation (outside the code read)
- The bodies of `ASK_SP_ManageSalesOrderDetails`, `ASK_SP_CancelSalesOrderDetails` and `ASK_SP_PendingSalesOrderInInvoices`: tables written, transaction handling, numbering, and every possible `STATUS` / `MESSAGE`.
- The column definitions of `dbo.ASK_UDT_SalesOrderItem` and `dbo.ASK_UDT_SalesOrderAdditionalInfo`. The C# `DataTable` column order must match them exactly.
- What `Functions.VSDOCNO()` and `Functions.LIST_ALLOW_VR_STMNT(true, "CORDM")` return.
- What `CommonFunction.SetVariableValues` loads (`Variables.format`, `Variables.DecString`).
