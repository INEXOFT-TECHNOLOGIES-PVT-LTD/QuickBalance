# Sales module – Phase 1 file map

Legacy root: `D:\PROJECT\Salman\IFlare\Inventory` (paths below are relative to `Inventory\` unless noted)
DB script: `D:\PROJECT\Salman\Latest\QuickBalance\DB\ProjectDB\QuickBalance.sql`
Generated: 2026-11-23. Read-only analysis — no code was changed.

---

## 0. Blockers found during discovery (read first)

| # | Finding | Impact |
|---|---|---|
| B1 | **Every form in the live project is a 0-byte file.** `Inventory\View\**` has 494 `.cs` files and all 494 are empty, including `Sales.cs`, `SalesReturn.cs`, `SalesOrder.cs`, `GoodsDeliveryNote.cs`, `SearchInvoice.cs`, `SI_QuotationDetails.cs`, `SO_QuotationDetails.cs`, `BatchForSales.cs`, `POS Module\POS.cs`. The older root-level `View\` copy is empty too (225 of 232). No non-empty copy or archive exists in the legacy folder (the `Zip1.zip`/`Zip2.zip` files in `bin\` are 22-byte empty archives). | The screen layer (screen load, event handlers, form-level validation, grid/total/tax calculations, **the order of DB writes on Save**, print triggers) **cannot be analysed from source**. |
| B2 | The `Model\*DB.cs` classes are **single-table SQL wrappers** (`SalMDB.insert` → one `INSERT INTO SALM`, etc.). The orchestration that calls them in sequence lived in the deleted forms. | Phase 2 can document each table's writes, but not the save sequence. The only surviving multi-table sequences are the **approval reject/approve routines** in `ApprovalModuleDB.cs` (see §2.4), which undo a posted invoice/return and so reveal every table a save touches. |
| B3 | **No transactions anywhere** in the legacy code (no `BeginTransaction`, `Commit`, `Rollback`, or `TransactionScope` in any non-empty file). | Legacy saves and cancels are non-atomic. The Web API should not copy this. |
| B4 | **Legacy code calls no stored procedures.** All sales SQL is inline, built by string concatenation, with a SQL Server branch (`db`) and an MS Access branch (`dbAcc`) in every method. | The sales SPs in the DB script are not legacy logic. The `ASK_SP_*` ones are already used by the Web API (§3). |
| B5 | Two code trees exist. The solution `Inventory.sln` builds **`Inventory\Inventory.csproj`**. The root-level `Inventory.csproj` with `Controller\`, `Model\`, `View\` is an orphaned older copy (66 of 68 controllers and 78 of 78 models empty). | Only `Inventory\` is mapped below. |

**Decision needed before Phase 2:** see §6.

---

## 1. Scope used

"Sales" = Sales Invoice (core), Sales Return, plus the documents an invoice links to: Sales Order / Quotation (`CORD*`), Goods Delivery Note (`GDN*`), receipts (`YVR`), bill-by-bill settlement, stock, batch, serial number, e-invoice, and print. Sales **reports** (about 60 methods in `SalMDB`/`SalTDB`/`SalDDB`) are listed in §2.5 but kept out of the core analysis.

---

## 2. Legacy code map

### 2.1 Entity classes (property bags, no logic)

| File | Class | Lines | Purpose |
|---|---|---|---|
| `Controller\clsSalM.cs` | `clsSalM` | 1–1052 | Invoice header: `SALM` columns |
| `Controller\clsSalT.cs` | `clsSalT` | 1–1005 | Invoice line: `SALT` columns |
| `Controller\clsSalD.cs` | `clsSalD` | 1–340 | Invoice tax/charge/account lines: `SALD` columns |
| `Controller\clsGdnM.cs` | `clsGdnM` | 1–700 | Delivery note header (`GDNM`) |
| `Controller\clsCordM.cs` | `clsCordM` | 1–685 | Sales order / quotation header (`CORDM`) |
| `Controller\clsBillByBill.cs` | `clsBillByBill` | 1–244 | Bill-by-bill settlement row |
| `Controller\clsEinv.cs` | `clsEinv` | 1–258 | E-invoice record |

### 2.2 Sales invoice / return – write path

| File | Class | Method | Lines | Purpose |
|---|---|---|---|---|
| `Model\SalMDB.cs` | `SalMDB` | `insert(clsSalM, DateTime)` | 18–30 | `INSERT INTO SALM` (~95 columns) `OUTPUT INSERTED.INVNO`. Sets `LOGCD`, `ULOGCD`, `APRVLRBY` = current user, `PCNAME` = machine |
| | | `MAXCODE()` | 31–41 | Next `INVNO` via `GetnewId("INVNO","SALM")` (MAX+1) |
| | | `MAXORDERNO(clsSalM)` | 42–52 | Next `ORDNO` per `STYPE` (MAX+1, race-prone) |
| | | `update(clsSalM)` | 66–78 | `UPDATE SALM SET …` by `INVNO` |
| | | `DeleteBynvNo(clsSalM)` | 53–65 | `DELETE FROM SALM WHERE INVNO=` (used by edit = delete + re-insert?) |
| | | `UpdateGdnNo(clsSalM)` | 79–93 | Link invoice to a delivery note (`GDNNO`) by `ORDNO` (**no STYPE filter**) |
| | | `CancelBill(clsSalM)` | 2740–2752 | Zero all amounts, `CANCELFLG=1`, `BILLREFFLG=0`, `CASHPAID=0`, `BANKPAID=0` |
| | | `UpdateBillRefFlg` / `…ByOrdNoAndType` / `…AndcashPaidByOrdNoAndType` | 2452–2477, 3276–3288 | Mark invoice settled/unsettled |
| | | `UpdateInvNoTextAndOrderNoByInvNo` | 3047–3059 | Change displayed bill number |
| | | `CheckBynvNo`, `CheckInvNoTextExists`, `CheckVoucherNumberExists` | 3060–3085, 3034–3046 | Duplicate bill-number checks |
| | | `UpdateEinvDtls` | 3266–3270 | Store e-invoice result on `SALM` |
| | | `UnMarkByGdnNo` | 3253–3265 | Unlink delivery note |
| | | `UpdateBnkDate` | 3576–3588 | Bank clearing date |
| | | `GetAvailablePoints` | 3140–3145 | Loyalty points balance (`PNTIN*`/`PNTOUT*`) |
| | | `GetSIFromQTN` | 3595–3607 | Invoice generated from a quotation |
| `Model\SalTDB.cs` | `SalTDB` | `insert(clsSalT)` | 15–27 | `INSERT INTO SALT` (~65 columns: qty, rate, unit conversion, batch, expiry, line discount, tax code, links to SO/GDN/quotation rows) |
| | | `Cancel(clsSalT)` | 28–41 | Keep `ORGQTY`/`ORGRATE`, zero `QTY`/`RATE`, `CANCELFLG=1` |
| | | `DeleteByVrNo(clsSalT)` | 69–81 | `DELETE FROM SALT WHERE INVNO=` |
| | | `UpdatePrRate` | 42–54 | Update cost rate on the line |
| | | `RetQty(clsSalT, PRV)` | 110–123 | Quantity already returned against an invoice line |
| | | `insert_PRV` / `SearchByOrdNoAndType_PRV` | 998–1023 | `SALTPRV`: previous-version snapshot of lines (audit/edit history) |
| | | `GetCustLastPrice` | 1024–1029 | Last price sold to this customer |
| | | `Updat_PurRate_AverageCost` | 1030–1087 | Recalculate average cost on lines (added 09-10-2025) |
| `Model\SalDDB.cs` | `SalDDB` | `insert(clsSalD)` | 17–30 | `INSERT INTO SALD` (tax/charge/ledger lines: `ETYPE`, `CODE`, `AMOUNT`, `TAXAMT`, `SLCODE`, currency) |
| | | `DeletByVrNo(clsSalD)` | 31–44 | `DELETE FROM SALD WHERE VRNO=` (**no VRTYPE filter**) |
| | | `Cancel(clsSalD)` | 572–585 | Keep `ORGAMT`, zero `AMOUNT`/`TAX`/`TAXAMT`, `CANCELFLG=1` (**no VRTYPE filter**) |
| | | `SlCodeForAccount` | 544–557 | Sales ledger account for posting |
| | | `SearchAddInfo` / `SearchAddInfo_ByOrdNOS` | 558–571, 708–722 | Additional info lines |

### 2.3 Read/search methods used by the invoice screen

| File | Method | Lines | Purpose |
|---|---|---|---|
| `Model\SalMDB.cs` | `Search`, `SearchByInvno`, `SearchByOrdNo`, `SearchByOrdNoWithVRSYS`, `ModifySearch`, `List`, `ListAll`, `SearchByCusCode`, `SearchByCurrency`, `MaxDate` | 94–181, 3021–3033, 3086–3105, 2780–2801 | Load and browse invoices |
| | `PendingBill` | 2727–2739 | Unsettled credit invoices (`BTYPE='R'`) for a customer |
| | `SearchByTypeAndRtnInv`, `…_Curr`, `GetSalesRetunDtls` | 2753–2779, 3271–3275 | Invoices available for return |
| | `GetAllDNByInvoice`, `GetAllORDByInvoice`, `SearchByOrdNOS_AdditionalInfo`, `ListForCDN` | 3422–3434, 3376–3380, 3589–3594 | Linked delivery notes / orders |
| `Model\SalTDB.cs` | `SearchByInvno`, `SearchByOrdNo`, `SearchByOrdNoAndSO`, `SearchByOrdNoAndPrCode`, `SearchByPrCode`, `SearchByOrdNo_PrCode_Unit`, `SearchByOrdNoAndType` | 55–109, 489–516, 810–815, 1088–1095 | Load lines |

### 2.4 Surviving multi-table sequences (best evidence of the save flow)

| File | Class | Method / block | Lines | Purpose |
|---|---|---|---|---|
| `Model\ApprovalModuleDB.cs` | `ApprovalModuleDB` | `SaveDetails` – "Sales Invoice" | 382–421 | On approval, clears `APRVLST` on the linked `GDNM` (DN), and `YVR` receipts `YVRNO`, `BANKYVRNO`, `BANKYVRNOT` (`VRTYPE='RV'`) |
| | | `SaveDetails` – "Sales Return" | 422–438 | Clears `APRVLST` on the linked payment voucher (`YVR`, `VRTYPE='PV'`) |
| | | `ValidateInentoryApproval` – "Goods Delivery Note" / "…Return" / "Sales Order" | 3765–5026 | Stock and validation checks before approval |
| | | `UpdateDetails` – Sales Invoice / Return | 5051–5186 | Approval status write-back |
| | | **`RejectDetails` – "Sales Invoice"** | **7418–7655** | Full reversal: `CancelBill` → `SalT.Cancel` → `SalD.Cancel` → delete `STKDT` (TR/I) → cancel up to 3 `YVR` receipts and reset `BILLREFFLG`/`CASHPAID` → delete `BILLBYBILL` → delete `BATCHDET` (O) → clear `SNTSTK` serials → cancel linked GDN (`GDNM`/`GDNT`/`GDND`, `STKDT` NT/I, `BATCHDET` DN) → **add stock back to `PRMAST` and `PRST` per line with unit conversion** → insert `LOGNOTE` "Rejected" |
| | | **`RejectDetails` – "Sales Return"** | **7656–8033** | Reversal of a return: stock availability check (`PrstDB.GetAvailableStock`) and serial-in-use check before cancel, then `CancelBill`, stock back out of `PRMAST`/`PRST`, `SalT`/`SalD.Cancel`, `STKDT`, `BILLBYBILL`, `PENDBIL`, `YVR`, `BATCHDET`, `SNTSTK`, `LOGNOTE`, `PARTYDBCR` cancel |
| | | `RejectDetails` – Quotation / Sales Order / GDN / GDN Return | 7055–7417 | Same pattern for the other sales documents |
| | | `CalcPricefromBatch` (called at 7612) | — | Price conversion to the main unit for batch stock |

### 2.5 Supporting DB classes called by the flow

| File | Method | Lines | Purpose |
|---|---|---|---|
| `Model\StkDtDB.cs` | `insert`, `DeleteByVr` | 12–24, 44–94 | Stock ledger (`STKDT`). Invoice = `VRTYPE 'TR'`, `TYPE 'I'`, keyed by `ORDNO`. DN = `'NT'` |
| `Model\PrMastDB.cs` | `UpdateStock` | 949–962 | `UPDATE PRMAST SET STOCK = STOCK + @qty` (signed; a sale passes a negative) |
| | `SearchByCode`, `CheckMainUnit`, `UnitConDetailsByCode` | 190–202, … | Unit and conversion lookup |
| `Model\PrstDB.cs` | `insert`, `updateByRack`, `deleteByRack`, `GetAvailableStock` | 16–32, 3071–3110, … | Batch/rack stock (`PRST`) |
| `Model\PrAliasDB.cs` | `SearchByAlCode`, `SearchByPrCodeAndName` | 93–113, 136–154 | Barcode/alias → product and pack quantity |
| `Model\BatchDetDB.cs` | `insert`, `DeleteByVrNo` | 16–29, 30–43 | Batch movement per voucher (`STYPE 'O'` invoice, `'DN'`) |
| `Model\SntStkDB.cs` | `ClearAdjustmentAllByVoucher`, `GetByVoucherInUsed`, `DelteByVoucherIn` | 340–352, … | Serial numbers out (`OUTVRTYPE 'SI'`) |
| `Model\BillByBillDB.cs` | `insert`, `GetByRefNoAndVrType`, `DeleteByRefNoAndVrType`, `InsertIntoAdj`, `GetRefNo`, `DeletByTypeCodeAndStype` | 16–28, 1533–1542, 443–450, 794–807, … | Bill-by-bill settlement (`VRTYPE 'Sales'`, `GENTYPE 'S'`) |
| `Model\YvrDB.cs` | `insert`, `Cancel` | 12–35, 36–74 | Receipt vouchers for cash/bank paid at billing (`VRTYPE 'RV'`) — the **ledger effect** of cash sales |
| `Model\PendBilDB.cs` | `UpdateBillRefFlgByTypeAndInvNo` | — (358 lines) | Pending bills (returns) |
| `Model\PartyDbCrDB.cs` | `Cancel` | — (114 lines) | Party debit/credit notes (returns) |
| `Model\GdnMDB.cs` / `GdnTDB.cs` / `GdnDDB.cs` | `insert`, `Cancel`, `VrNoByOrdNo`, `UpdateInvNoTextAndOrderNoByInvNo` | 18–43, 146–158, 289–302 | Delivery note (364/241/404 lines) |
| `Model\CordMDB.cs` / `CordTDB.cs` / `CordDDB.cs` | — | 905/220/85 lines | Sales order / quotation (`STYPE 'O'` / `'Q'`) |
| `Model\EInvDB.cs` | `insert`, `Get`, `GetByTypeAndOrdNo`, `Get_PrevInvHash`, `VetExceptionResonAll/ByCat` | 13–58 | E-invoice (hash chain via previous invoice hash) |
| `Model\LogNoteDB.cs` | `Insert` | 16–28 | Audit note per voucher |
| `Model\InvFormatDB.cs` | `ActiveFormats`, `AllActiveFormats`, `GetQuery` | 17–90 | Invoice print formats (`INVFORMAT`), SQL stored per format |
| `Model\InvOptionsDB.cs` | `InvOptions` | 14–29 | Invoice options (`INVOPTIONS`) |
| `Print\InvoicePrint.cs` | `InvoicePrint` form: `InvoicePrint_Load`, `Print`, `PrintPage` | 20–62 | RDLC invoice print. Layouts in `Print\CRR\` (269 files) and about 50 dated `Print\CRR\Customise\*` folders of customer-specific layouts |

### 2.6 Sales reports (out of core scope; for Phase 3 inventory only)

`SalMDB`: `SalesSummary*` 182–660, `POSUserWiseReport*` 661–926, `POSSalesSummary` 927–1063, `BillwiseSalesSummary*` 1064–1483, `GroupwiseSalesSummary` 1484–1524, `ProductwiseSalesSummary*` 1525–1841, `AgewiseAnalysisReceivables*` 1842–2451, `NotMarkedInv*` / `InvAll*` / `InvSel*` 2480–2726, dashboards 2802–2912 and 3358–3375, collection reports 2913–3020, `LedgerGroupwiseSalesSummary` 3106–3139, `PointLedgerReport` 3146–3247, `EInvoiceReport` 3289–3340, `SalesDetailReport` 3381–3421, `CustWiseProductDetails` / `CustomerWiseProductReport` 3435–3575.
`SalTDB`: VAT reports 124–209, profitability reports 210–474 and 816–997, salesman/agent reports 540–809, `ExportToJSON` 517–539.
`SalDDB`: cash/non-cash sales 45–299, VAT registers and returns 300–543 and 586–707.

---

## 3. Stored procedures and types (`QuickBalance.sql`)

| Object | Lines | Called by legacy? | Called by Web API? |
|---|---|---|---|
| `ASK_SP_ManageSalesInvoiceDetails` | 5334–6116 | No | **Yes** – `DB/SalesInvoiceDB.cs:182` |
| `ASK_SP_CancelSalesInvoiceDetails` | 5107–5195 | No | **Yes** – `DB/SalesInvoiceDB.cs:192` |
| `ASK_SP_PendingSalesInvoicesInReturn` | 6369–6446 | No | **Yes** – `DB/SalesInvoiceDB.cs:202` |
| `ASK_SP_CancelSalesReturnDetails` | 5248–5333 | No | **Yes** – `DB/SalesReturnDB.cs:184` |
| `ASK_SP_ManageSalesOrderDetails` | 6117–6368 | No | **Yes** – `DB/SalesOrderDB.cs:115` |
| `ASK_SP_CancelSalesOrderDetails` | 5196–5247 | No | **Yes** – `DB/SalesOrderDB.cs:125` |
| `ASK_SP_PendingSalesOrderInInvoices` | 6447–6537 | No | **Yes** – `DB/SalesOrderDB.cs:140` |
| `Manage_SaslesInvoiceDetails` (sic) | 6959–7421 | No | No – unused |
| `ManageSales_Salt`, `ManageSalesBilling_Salt`, `ManageSalesReturn_Salt`, `ManageSalesOrder_Cordt` | 8192–8697 | No | No – unused |
| `Get_PendingSalesOrderDetails`, `Get_PendingSalesOrderQuanity` | 6591–6671 | No | No – unused |
| Types `ASK_UDT_SalesInvoiceItem` / `…AdditionalInfo` / `…SerialNumber` | 40–82 | — | `ASK_UDT_SalesInvoiceItem` used at `DB/SalesInvoiceDB.cs:173` |
| Types `ASK_UDT_SalesOrderItem` / `…AdditionalInfo` | 83–107 | — | Sales order |
| Types `UDT_SALBILL`, `UDT_SALRET` | 351–434 | — | No – unused (belong to the unused SPs) |

There's no sales-return "Manage" SP with the `ASK_` prefix. Check how `SalesReturnController` saves in Phase 3.

---

## 4. Inline SQL inventory (write statements only)

| Table | Statement | Where |
|---|---|---|
| `SALM` | INSERT, UPDATE (full), DELETE by INVNO, UPDATE `GDNNO`, cancel (zero + `CANCELFLG`), UPDATE `BILLREFFLG`/`CASHPAID`/`BANKPAID`, UPDATE `INVNOTEXT`/`ORDNO`, e-invoice fields, bank date | `SalMDB.cs` 18–93, 2452–2477, 2740–2752, 3047–3059, 3253–3288, 3576–3588 |
| `SALT` | INSERT, cancel, DELETE by INVNO, UPDATE `PRRATE`, unmark GDN, average-cost update | `SalTDB.cs` 15–81, 475–488, 1030–1087 |
| `SALTPRV` | INSERT (snapshot) | `SalTDB.cs` 998–1010 |
| `SALD` | INSERT, DELETE by VRNO, cancel | `SalDDB.cs` 17–44, 572–585 |
| `PRMAST` | `STOCK = STOCK + n` | `PrMastDB.cs` 949–962 |
| `PRST` | insert / updateByRack / deleteByRack | `PrstDB.cs` 16–32, 3071–3110 |
| `STKDT`, `BATCHDET`, `SNTSTK`, `BILLBYBILL`, `YVR`, `PENDBIL`, `PARTYDBCR`, `GDNM/T/D`, `LOGNOTE`, `EINV` | insert / delete / cancel | §2.5 |
| `GDNM`, `YVR` | `UPDATE … SET APRVLST=''` | `ApprovalModuleDB.cs` 396–434, 7440 |

All of these concatenate values into SQL strings, so every one is an SQL injection risk.

---

## 5. Tables touched by a sales invoice

**Core:** `SALM` (header, 4182–4299), `SALT` (lines, 4300–4377), `SALD` (tax/charge/ledger lines, 4114–4148), `SALTPRV` (line history, 4378–4443).
**Stock:** `PRMAST.STOCK`, `PRST` (batch/rack, 3515–3551), `STKDT` (stock ledger, 4531–4582), `BATCHDET` (636–673), `SNTSTK` (serials, 4485–4507).
**Accounts:** `YVR` (receipt vouchers, 4969–5106), `BILLBYBILL` (674–704), `PENDBIL` (2878–2902), `PARTYDBCR` (2782–2837, returns).
**Linked documents:** `GDNM`/`GDNT`/`GDND` (delivery note, 1972–2167), `CORDM`/`CORDT`/`CORDD` (order/quotation, 1278–1472).
**Other:** `EINV`/`EINVOICE` (1764–1808), `LOGNOTE` (2638–2654), `INVFORMAT` (2603–2614), `INVOPTIONS` (2615–2626).
**Unknown:** `SALE` (4149–4181) is defined in the DB but no legacy or API code references were found yet. To check in Phase 2.

---

## 6. Questions for your review before Phase 2

1. **Missing forms (B1).** Is there another copy of the WinForms source with the `View\` files intact, such as another machine, an older backup or a decompiled build? If so, give me the path and I'll redo §2 with the real event handlers. If not, Phase 2 has to rebuild the workflow, validations and calculations from three sources: (a) the reversal code in `ApprovalModuleDB`, (b) column semantics, and (c) **the logic in `ASK_SP_ManageSalesInvoiceDetails`**, which the Web API already uses. I'll label every rule with its source and confidence. Is that acceptable?
2. **Scope.** Should Phase 2 cover only **Sales Invoice + Sales Return**, or also Sales Order, Quotation, Goods Delivery Note, POS and e-invoice?
3. **Reports.** Leave the roughly 60 sales report methods (§2.6) out of this migration and treat them as a separate module?
4. **MS Access branch.** Every legacy method has a SQL Server and an Access (`dbAcc`) branch. Is it OK to ignore the Access branch completely? The Web API is SQL Server only.
5. **Print.** Is invoice printing (RDLC layouts in `Print\CRR`, including the about 50 customer-specific `Customise\*` layouts) in scope for the API? Or does the front end handle printing from a JSON response?
