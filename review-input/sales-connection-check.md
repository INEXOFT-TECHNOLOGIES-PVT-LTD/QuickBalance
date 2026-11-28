# Sales controllers: DBConn / connection check

Read-only review. No files were changed, built or run. No connection strings, passwords or tokens are included.

Scope: SalesOrderController, SalesBillingController, SalesReturnsController, and the code behind them.
Not checked (as requested): SalesInvoiceController.cs, SalesReturnController.cs.

## Summary table

| Controller | Endpoint | File | DBConn method / connection used | AFFECTED or SAFE |
|---|---|---|---|---|
| SalesOrder | POST api/v1/sales-orders/manage | Repository/Transactions/Sales/SalesOrderRepository.cs:34 -> DB/SalesOrderDB.cs:114 | `DBConn.ExecuteStoredProcedure` (ASK_SP_ManageSalesOrderDetails) | AFFECTED |
| SalesOrder | POST api/v1/sales-orders/cancel/{id} | SalesOrderRepository.cs:46 -> DB/SalesOrderDB.cs:124 | `DBConn.ExecuteStoredProcedure` (ASK_SP_CancelSalesOrderDetails) | AFFECTED |
| SalesOrder | GET api/v1/sales-orders/pending-list | SalesOrderRepository.cs:115 -> DB/SalesOrderDB.cs:145 | `DBConn.ExecuteStoredProcedure` (ASK_SP_PendingSalesOrderInInvoices) | AFFECTED |
| SalesOrder | GET api/v1/sales-orders/list | SalesOrderRepository.cs:69 -> DB/CordMDB.cs:85, 144, 150 | `DBConn.GetDatatable` | SAFE |
| SalesOrder | GET api/v1/sales-orders/get/{id} | SalesOrderRepository.cs:90-95 -> CordMDB.cs:150, CordTDB.cs:23, 146, CordDDB.cs:23 | `DBConn.GetDatatable` | SAFE |
| SalesBilling | POST api/v1/sales-billing/manage | Repository/Transactions/Sales/SalesBillingRepository.cs:499-510 | own `SqlConnection` from `IDbConnectionFactory`, `OpenAsync` at :501, then `ExecuteReaderAsync` | SAFE |
| SalesBilling | POST api/v1/sales-billing/cancel/{id} | SalesBillingRepository.cs:499-510 (same procedure-call helper) | own `SqlConnection`, opened at :501 | SAFE |
| SalesBilling | GET api/v1/sales-billing/get/{id} | SalesBillingRepository.cs:172-177 (also :301-306, :463-468) | own `SqlConnection`, `OpenAsync`, then Dapper `QueryMultipleAsync` | SAFE |
| SalesBilling | GET api/v1/sales-billing/list | SalesBillingRepository.cs:301-306 | own `SqlConnection`, `OpenAsync`, Dapper `QueryMultipleAsync` | SAFE |
| SalesBilling | GET api/v1/sales-billing/pending | SalesBillingRepository.cs:463-468 | own `SqlConnection`, `OpenAsync`, Dapper `QueryMultipleAsync` | SAFE |
| SalesReturns | POST api/v2/sales-returns/manage | Repository/Transactions/Sales/SalesReturnRepository.cs:421-430 | own `SqlConnection`, `OpenAsync` at :422, then `ExecuteReaderAsync` | SAFE |
| SalesReturns | POST api/v2/sales-returns/cancel/{id} | SalesReturnRepository.cs:421-430 (same helper) | own `SqlConnection`, opened at :422 | SAFE |
| SalesReturns | GET api/v2/sales-returns/get/{id} | SalesReturnRepository.cs:158-161 | own `SqlConnection`, `OpenAsync` at :159, Dapper `QueryMultipleAsync` | SAFE |
| SalesReturns | GET api/v2/sales-returns/list | SalesReturnRepository.cs:276-279 | own `SqlConnection`, `OpenAsync` at :277, Dapper `QueryMultipleAsync` | SAFE |

Totals: 3 AFFECTED, 11 SAFE (14 endpoints).

## Per-controller findings

### 1. SalesOrderController (api/v1/sales-orders)
Chain: controller -> ISalesOrderInterface -> SalesOrderService (no DB access) -> SalesOrderRepository -> SalesOrderDB / CordMDB / CordTDB / CordDDB / CommonFunction.

1. Uses DBConn: yes. The repository creates `new DBConn(connectionString)` in `OpenAsync` (:134), and the DB classes call:
   - `ExecuteStoredProcedure`: SalesOrderDB.cs:114, 124, 145.
   - `GetDatatable`: CordMDB.cs:85, 144, 150; CordTDB.cs:23, 146; CordDDB.cs:23.
2. AFFECTED: the three `ExecuteStoredProcedure` calls. SAFE: all `GetDatatable` calls.
3. Direct `GetConnection()` calls: none in these files. (CordMDB.cs:178 and CordTDB.cs:54 are in commented-out code.)
4. Direct `SqlConnection`: `CommonFunction.CheckDatabaseConnection` (CommonFunction.cs:22-39) opens its own connection with `OpenAsync`. SAFE.
5. `CommonFunction.SetVariableValues` (:43) only uses `GetDatatable` on `DBConn` (lines 59, 67, 72, 87, 154, 179). SAFE.

### 2. SalesBillingController (api/v1/sales-billing)
Chain: controller -> ISalesBillingInterface -> SalesBillingService (no DB access) -> SalesBillingRepository.

1. Uses DBConn: no. It uses `IDbConnectionFactory` only.
2. No DBConn calls, so nothing is AFFECTED.
3. `GetConnection()` is not called.
4. `SqlConnection` created at :172, :301, :348, :463, :498. Each is followed by `OpenAsync` (:175, :304, :351, :466, :501) before any Dapper call or `ExecuteReaderAsync` (:510). SAFE.

### 3. SalesReturnsController (api/v2/sales-returns)
Chain: controller -> ISalesReturnInterface -> SalesReturnService (no DB access) -> SalesReturnRepository.

1. Uses DBConn: no. It uses `IDbConnectionFactory` only.
2. No DBConn calls, so nothing is AFFECTED.
3. `GetConnection()` is not called.
4. `SqlConnection` created at :158, :276, :421. Each is followed by `OpenAsync` (:159, :277, :422) before Dapper or `ExecuteReaderAsync` (:430). SAFE. The comment at :417 says the repository opens its own connection because `DBConn.ExecuteStoredProcedure` expects an already-open connection.

## AFFECTED calls whose errors are hidden

None of the AFFECTED calls (`ExecuteStoredProcedure` at SalesOrderDB.cs:114, 124, 145) return -5, -1, null or empty on failure. `ExecuteStoredProcedure` throws `InvalidOperationException` when the connection is not open.

The error still does not reach the client as a raw exception. `SalesOrderService` catches it and the controller returns a 500 body (`status:false`, generic message). So the failure is visible as an error response, but it is not a silent bad value.

The hidden-error methods on your list (`Execute`, `ExecuteScalar`, `ReturnIntValue`, `GetnewId`, and so on) are not called by any of these three controllers.

## Important note: current state of DBConn.cs

`DB/DBConn.cs` has uncommitted changes (shown as modified in git status). In the current working tree:
- `CreateCommand` (:268) now uses the shared `con` field. It throws "Connection is not open" if `con` is not open. It no longer uses `GetConnection()`.
- `ExecuteStoredProcedure` (:813-817) calls `await OpenConnection()` first, which opens `con`.

If that working-tree version is what runs, the three Sales Order calls above would work. I classified them AFFECTED because your brief lists `ExecuteStoredProcedure` as affected and the committed version used `GetConnection()`. Re-check them after you confirm which DBConn version is deployed.
