# Sales module – scope and decisions

Legacy source: `D:\PROJECT\IFlare\Inventory\Inventory` (built by `Inventory.sln`)
DB script: `D:\PROJECT\Salman\Latest\QuickBalance\DB\ProjectDB\QuickBalance.sql`
Last updated: 2026-11-23. Line numbers refer to the files above. Evidence for each item is in `01-file-map.md`.

## Phase status

| Phase | Output | Status |
|---|---|---|
| 1. Discovery | `01-file-map.md` | **COMPLETE.** Reviewed and approved 2026-11-23 (decisions D7–D10). Amended the same day: approval writes corrected (§2.7 note, §8). See C13 |
| 2. Legacy analysis | `02-legacy-analysis.md` | **NEXT.** Start here. Include the items in §6 and the open items in D7 |
| 3. Gap analysis | `03-gap-analysis.md` | Not started |
| 4. Implementation plan | `04-implementation-plan.md` | Not started |

---

## 1. Forms in scope

| Menu (Home → Transactions) | File | Class | Approval module name | `SALM.STYPE` / tables | Opened from |
|---|---|---|---|---|---|
| **Sales Order** | `View\SalesOrder.cs` (+ `SalesOrder.designer.cs`) | `Inventory.View.SalesOrder : Form` | `"Sales Order"` | `CORDM.STYPE = 'O'` → `CORDM`/`CORDT`/`CORDD` | `Home.TrSalOrderAdd_Click` (Home.cs:3341). Title set at runtime from `Variables.SalesOrder` = menu text (Home.cs:1329) |
| **Billing** (Sales Invoice) | `View\Sales.cs` (+ `Sales.Designer.cs`) | `Inventory.View.Sales : Form` | `"Sales Invoice"` | `SALM.STYPE = 'O'`, `RETURNFLG = 0` → `SALM`/`SALT`/`SALD` | `Home.TrBillingAdd_Click` (Home.cs:3149) |
| **Sales Return** | `View\SalesReturn.cs` (+ `SalesReturn.designer.cs`) | `Inventory.View.SalesReturn : Form` | `"Sales Return"` | `SALM.STYPE = 'R'`, `RETURNFLG = 1` → `SALM`/`SALT`/`SALD` | `Home.TrSalesReturnAdd_Click` (Home.cs:3475) |

Menu captions come from `View\Home.resx` (`TrSalOrder.Text` = "Sales Order", `TrBilling.Text` = "Billing", `TrSalesReturn.Text` = "Sales Return"). The forms are also opened for drill-down from `Functions.cs:4306-4345` (duplicate mode), `ApprovalModuleDB`, `BankReconciliation` and `CustomerReceipt`.

`SalesOrder` is the same class as **Quotation** (`QtnFlg = true`, menu `TrQuotation` = "Quotation"). Only the Sales Order paths are in scope.

## 2. General scope

| Topic | Decision |
|---|---|
| Out-of-scope forms | `GoodsDeliveryNote.cs`, `POS.cs`, Quotation, e-invoice. **Shared code that the three forms call is in scope** |
| Reports | Excluded. Separate module later |
| Database branches | SQL Server only. Ignore every MS Access (`dbAcc`) branch |
| Printing | Out of scope. The API returns JSON and the front end prints. `01-file-map.md` §9 lists the fields the layouts use, and the API must be able to return them |

---

## 3. Decisions (2026-11-23)

### D1. Auto delivery note from Billing (`chk_trck`)

**Decision:** analyze it fully in Phase 2 and cross-check it against the API (`ASK_SP_ManageSalesInvoiceDetails` / `ASK_SP_CancelSalesInvoiceDetails`). **Whether to implement it is decided later.**

How the legacy app records it:
- When `chk_trck` is ticked, saving a bill creates a delivery note in the same save: `Sales.cs` `SaveGdnM`/`SaveGdnT`/`SaveGdnD` 19215-20083, called from `btn_save_Click` 5338-5750.
- The DN is written as `GDNM.STYPE = 'DN'` with **`GDNM.TRCKFLG = 1`** (`Sales.cs:19305`). The Goods Delivery Note form itself sets `TRCKFLG = 0` (`GoodsDeliveryNote.cs:11116`).
- The bill stores the DN's number in **`SALM.GDNNO`** (= `GDNM.ORDNO`, `Sales.cs:3280-3300`). Untracked bills get `GDNNO = 0`.
- When a bill is reopened, `GDNNO > 0` is what ticks `chk_trck` (`Sales.cs:6678-6687`).
- Bills that *pull* lines from an existing DN don't use `GDNNO`. They link per line through `SALT.NTROWID` / `STKDT` instead, so they aren't counted as auto-DN.

**Query: count invoices that used it** (run once per company/financial-year database, because each `COMP<code>_<yyyy>_<yyyy>` DB holds one year):

```sql
-- Sales invoices whose delivery note was auto-created by Billing (chk_trck)
SELECT
    COUNT(*)                                                    AS TotalInvoices,
    SUM(CASE WHEN ISNULL(m.GDNNO, 0) > 0 THEN 1 ELSE 0 END)     AS InvoicesWithGdnNo,
    SUM(CASE WHEN ad.ORDNO IS NOT NULL THEN 1 ELSE 0 END)       AS AutoDnInvoices,
    SUM(CASE WHEN ad.ORDNO IS NOT NULL
              AND ISNULL(m.CANCELFLG, 0) = 0 THEN 1 ELSE 0 END) AS AutoDnInvoicesNotCancelled,
    SUM(CASE WHEN ISNULL(m.GDNNO, 0) > 0
              AND ad.ORDNO IS NULL THEN 1 ELSE 0 END)           AS GdnNoButNoAutoDn   -- data anomalies to review
FROM SALM m
OUTER APPLY (
    SELECT TOP (1) g.ORDNO
    FROM GDNM g
    WHERE g.STYPE = 'DN'
      AND g.ORDNO = m.GDNNO
      AND ISNULL(g.TRCKFLG, 0) = 1
) ad
WHERE m.STYPE = 'O'
  AND ISNULL(m.RETURNFLG, 0) = 0;
```

`OUTER APPLY … TOP (1)` keeps the count per invoice correct even if `GDNM.ORDNO` repeats, for example across voucher-system branches. A non-zero `GdnNoButNoAutoDn` means a bill points at a DN that is missing, isn't `DN`, or wasn't auto-created. Check those rows before deciding on D1.

### D2. Quotations

**Decision:** skip quotation logic (`Sales.SearchQuotation` 24362-24835, `SalesOrder.SearchQtn` / `ListQtn`, quotation balance checks in `ValidateTaggedVoucher`). **Document the link fields** so the API keeps them and quotation support can be added later.

| Table | Column | Type (DDL line) | Meaning / how set |
|---|---|---|---|
| `SALT` | `QTNCODE` | int (4361) | Source quotation number (`CORDM.ORDNO`, `STYPE='Q'`) for the line. `Sales.cs:3833` from grid `col_qtnCode` |
| `SALT` | `QTNROWID` | int (4362) | Source quotation line row id. `Sales.cs:3834` |
| `SALT` | `PRVQ` / `PRVQY` | nvarchar(50) (4363, 4366) | `'Y'` + financial year when the quotation is from a **previous year's** DB. `Sales.cs:3835-3836` |
| `SALM` | `QTNNOS` / `QTNVSNOS` | nvarchar(max) (4248-4249) | Comma list of quotation numbers / voucher-system numbers on the bill. **`Sales.cs` never builds these for a new bill.** It only reloads and re-saves them on edit (6759-6760), so the line-level fields are the real link. Check the API's usage in Phase 3 |
| `CORDT` | `QTNCODE` / `QTNROWID` | int (1453, 1461) | Same link for a Sales Order created from a quotation. `SalesOrder.cs:3871-3882` |
| `GDNT`, `STKDT` | `QTNCODE`, `QTNROWID`, `PRVQ`, `PRVQY` | (2150-2153, 4564-4567) | Carried on DN lines and stock-ledger rows. Used to calculate quotation balance quantity |
| `CORDM` | `QTNF1`…`QTNF20` | nvarchar(250) | Quotation additional-info labels. Not a link, listed to avoid confusion |

### D3. Receipts at billing (cash / bank / bank2)

**Decision: in scope.** `Sales.cs` `SaveReceipt` 4071-4165 and `SaveAdjustment` 4168-4213. For each of Cash (`C`), Bank (`B`) and Bank2 (`B2`) with an amount, it posts a receipt voucher `YVR` (`VRTYPE='RV'`) as a debit/credit pair plus a bill-by-bill settlement (`BILLBYBILL`). When an amount is changed to 0 on edit, it cancels the old voucher. The voucher numbers are stored on the bill in `SALM.YVRNO`, `BANKYVRNO`, `BANKYVRNOT`, with ledgers in `CASHLCD`, `BANKLCD`, `BANKLCDT`. Sequence: `01-file-map.md` §2.7 (`SaveReceipt` row). The equivalents for the other two forms are also in scope: the **Sales Return cash refund** (`SalesReturn.SavePayment`, `YVR` PV, §4.5 steps 13–14) and the **Sales Order advance** (`SalesOrder.SaveAdvance`, `YVR` RV, §5.4 step 10).

### D4. `STKDTPHY` / `SNTSTKPHY` (physical stock copies)

**Decision rule:** keep them if anything reads them.
**Finding: they are read, so keep them.**

| Where | Reads | Writes |
|---|---|---|
| **Legacy app** | **Yes, 37 read sites:**<br>• `StkDtDB.phy_GetUTime` 8941-8990, called by every form's `SaveStkDt` for row timing<br>• `StkDtDB.phy_GetForLM` 9075-9086, called by `Sales.Fill` for partially used (LM) lines<br>• **`StkDtDB.phy_Save` 9088-9100 copies `STKDTPHY` rows back into `STKDT`.** It's called 15 times from `ApprovalModuleDB` (for example 5481, 5489, 5894), so approval restores stock rows from the PHY table<br>• `SntStkDB.phy_GetByVoucherIn` 683-686 (`SalesReturn.Fill`) and `phy_GetByVoucherOut` 688-691 (`Sales.Fill`) read serial numbers<br>• `VoucherSystemDB.AddPrevDetailsUnderNewBranch` 763-1163<br>• Branch↔HO data sync (`View\Tools\DataSyncing.cs`, `View\HO_ModuleAccess.cs`) lists both tables for syncing | 50 write sites (`phy_insert`, `phy_DeleteByVr`, `phy_UnmarkByVr`, `Phy_DeleteByVoucher`) alongside every `STKDT`/`SNTSTK` write |
| **DB script** | None. No procedure or view selects from them | `ASK_SP_CancelSalesInvoiceDetails` (5148, 5157, 5165), `ASK_SP_CancelSalesOrderDetails` (5220) and `ASK_SP_CancelSalesReturnDetails` (5295, 5305) **delete** from them. Only the unused `ManageSales_Salt` (8280-8334) inserts |
| **Web API (C#)** | None | None directly. Only through the cancel SPs above |

**Gap for Phase 3:** `ASK_SP_ManageSalesInvoiceDetails` / `…SalesOrderDetails` do **not** insert into `STKDTPHY` / `SNTSTKPHY`, but the legacy app reads them during approval (`phy_Save`) and when it opens a voucher. A bill or order created through the API and then approved or opened in the desktop app may be missing stock or serial rows. Phase 3 confirms this and Phase 4 plans the writes.

### D5. Copy/paste voucher and attached documents

| Feature | Decision | Notes |
|---|---|---|
| `MakeDup` / `btn_vr_paste_Click` (copy/paste voucher) | **Out of scope** | `Sales.cs` 26028-26524, `SalesReturn.cs` 15150-15521, `SalesOrder.cs` 11337-11612 |
| `SaveDoc` (attached documents) | **Out of scope. Tables only** | **Writes no tables.** `Functions.SaveDoc` (Functions.cs:3436-3484) copies uploaded files to `{DocPath}\UPLOADS\{CompShortName}\{VrType}\{VrNo}\` (VrType `SI` for bills; `VrNo` = `VSVRTYPE_VSORDCD` when the voucher system is on, else `ORDNO`). It only *reads* config: `CONFIGTB.DCMGMTYN` (company DB, DDL line 1234) together with `DCMGMTAYN` and `DOCPATH` from the configuration row loaded by `ConfigDB.GetData` (Home.cs:6141, 6215-6217). `DCMGMTAYN` and `DOCPATH` aren't in this DB script, so they likely belong to the MasterDB `CONFIGTB` (to confirm if this feature is migrated). `CONFIGTB.DOCAPIKEY` (1237) is related |

### D6. Sales Order short-close

**Decision: in scope.** `SalesOrder.cs` `btn_Close_Click` 9806-9982 closes an order so its remaining quantity is no longer pending. `SalesOrder.btn_cancel_Click` also refuses to cancel a closed order ("The voucher is already closed", 6899). Phase 2 documents the close rules and the columns it writes. Phase 3 checks whether the API has an equivalent.

### D7. Phase 1 map completeness

**Decision: approved.** The map in `01-file-map.md` is the basis for Phase 2. Phase 2 must also check these gaps, which were known at sign-off:

| # | Gap | Where |
|---|---|---|
| a | §1 lists menu entry points for Billing only. Add the Sales Order and Sales Return entries (`Home.cs:3341`, `:3475`) and their modify/list handlers, plus the `HomeUI.cs` launchers | `View/Home.cs`, `View/HomeUI.cs` |
| b | `SalesOrder.cmb_party_Leave` (6528–6603) wasn't read | `SalesOrder.cs` |
| c | The return's bill-adjustment dialog flow (`FormType "RET"`, `GenType "SRB"`) wasn't traced into `View/BillAdjustment.cs` | `SalesReturn.cs:4136–4163` |
| d | `SalesReturn.SaveSalM` voucher numbering also reserves a `YVR` number (`YvrDB.MaxVrNo` 2234) for the refund. Check how that number is released when no refund is posted | `SalesReturn.cs:2132–2303` |
| e | How a Sales Order is consumed by a Goods Delivery Note (the DN form is out of scope, but its `STKDT` rows drive the SO edit lock and pending quantity) | `StkDtDB.PendingOrders_NEW`, `BalOrderQtyByRowID` |
| f | What `ManageEinvoiceXML_API` / `_DIRECT` return when e-invoicing is off (affects §6 item C3) | `SalesReturn.cs:13188`, `:13435`; same in `Sales.cs` |

### D8. Print fields: raw data only

**Decision:** the API returns **raw data only**:
- all stored values for the voucher (header, lines, SALD/CORDD add-info, `SIF/SOF 1..20`, tax lines)
- values that need a DB lookup: company and branch info (`dtBranch` fields), customer address, secondary-language names (company, customer, product), previous balance, linked document numbers (SO/DN/QTN/original invoice), and the e-invoice QR value when it is stored (`EINV`)

Formatting, amounts in words and layout belong to the front end. `01-file-map.md` §9 is the reference list: every field there is either returned raw, derivable by the front end from raw fields, or explicitly dropped.

**Phase 4 must include a check that each `get` endpoint (invoice, return, order) returns these raw fields.**

### D9. SP baseline for Phase 3

**Decision:** `ASK_SP_ManageSalesInvoiceDetails` and `ASK_SP_ManageSalesOrderDetails` are the API implementation, but they must be **proven correct, not assumed**. Phase 2/3 verify them **line by line** against the legacy save chains (`01-file-map.md` §2.7 and §5.4): every table write, column value, order of writes, numbering, stock rows (including `STKDTPHY`, D4), receipts/advance, bill-by-bill and approval. Each difference goes into the gap table as a defect in the SP or as an accepted deviation. The cancel SPs (`ASK_SP_Cancel*`) get the same treatment against §2.8, §4.6 and §5.5. The new `ASK_SP_ManageSalesReturnDetails` (W2) is designed from §4.5.

### D10. Previous-year returns

**Decision: analyze fully in Phase 2.** Cover:
- how the legacy app finds an invoice from a previous financial year (`txt_bill_no_KeyDown` 8370–8871: `isPRV`, connection switch, `SalMDB.SearchByOrdNoWithVRSYS`)
- how returned quantity is computed across years (`SalTDB.RetQty` with `PRVR <> 'Y'` / `= 'Y'`)
- which columns mark the link (`SALM.RTNINVNO`, `SALT.RTNINVNO`, `SALT.PRVR`)
- how the transfer flag is written into the **previous-year DB** (`SalesReturn.cs:4178–4183`, `Functions.SetCuurCon(Variables.PrevYearConn)`, `SalMDB.FY_Update_RetTransfer_Status` `SalMDB.cs:3765`), and how `Variables.PrevYearConn` is resolved
- serial numbers from previous-year invoices (`SntStkDB.GetByVoucherOut_SI_in_SR_Prv`)

**Whether the API supports it is decided in the implementation plan (Phase 4).** One deployment serves one company DB (see `CLAUDE.md`), so supporting it means a cross-database connection. The plan must address that.

---

## 4. Current API status (2026-11-23)

Details and evidence: `api-review.md`. Confirmed against the live DBs `COMP91_2026_2026` and `MasterDB`.

| Form | Status |
|---|---|
| **Sales Order** | **Working.** To be verified against the legacy save chain in Phase 3 |
| **Sales Invoice** | **Save, list and cancel broken.** They connect to `MasterDB`, which has no `ASK_` objects (`api-review.md` A1). The list hides the failure and returns an empty result because `SalMDB.List` swallows errors (A4) |
| **Sales Return** | **Save missing.** `ASK_SP_ManageSalesReturnDetails` and `ASK_UDT_SalesReturnItem` / `…SerialNumber` / `…AdditionalInfo` were never deployed (A2). Cancel SP exists |

## 5. Required work items

These are required in the plan. No code changes until the plan is approved.

| # | Item | Depends on |
|---|---|---|
| **W1** | Fix `isMasterDB: true` in the three invoice actions (`SalesInvoiceController.cs:45` manage, `:139` list, `:613` cancel) so they use the company DB | — |
| **W2** | Design and create `ASK_SP_ManageSalesReturnDetails` and its 3 UDTs (`ASK_UDT_SalesReturnItem`, `ASK_UDT_SalesReturnSerialNumber`, `ASK_UDT_SalesReturnAdditionalInfo`), based on the legacy `SalesReturn.cs` save chain and following the `ASK_SP_ManageSalesInvoiceDetails` pattern. UDT columns must match `SalesReturnDB.CreateItemTable` | Phase 2 analysis of the `SalesReturn.cs` save chain |

---

## 6. Phase 2: items to confirm

These are carried over from `01-file-map.md` §10. Phase 2 confirms or rejects each one with file:line evidence. Confirmed defects move to §7.

| # | Finding (`01-file-map.md` §10) | Status at Phase 1 sign-off |
|---|---|---|
| C1 | No transaction boundary in any of the three save chains (§10.1) | **Confirmed** → L1 |
| C2 | Update is delete-and-reinsert for lines (SALT/SALD, CORDT/CORDD, GDNT/GDND, BATCHDET) (§10.2) | Confirmed as behavior. Phase 2 checks the side effects (row ids such as `ORDROWID`/`NTROWID` that other vouchers reference) |
| C3 | Sales Return save is gated on the e-invoice result (`SalesReturn.cs:4016`). Does it block saving when e-invoicing is off? Check the same gate in `Sales.cs` (around 5308) | Open (see D7 f) |
| C4 | Cancel is soft (qty/rate zeroed, originals kept in `ORGQTY`/`ORGRATE`) (§10.3) | Behavior to document, not a defect |
| C5 | Voucher numbering: `GetnewId` (`SalMDB.cs:46`, `CordMDB.cs:73`) and the `FYDETAILS` counter (`FyDetailsDB.ValidateVoucherNo/UpdateVoucherNo`) both appear. Which one issues the number, and is it concurrency-safe? (§10.4) | Open |
| C6 | Sales Return reports success after a failed stock save (`SalesReturn.cs:4082–4229`) (§10.8) | **Confirmed** → L2 |
| C7 | Sales Order advance: no compensation when the second leg fails (`SalesOrder.cs:4408–4410`) (§10.10) | **Confirmed** → L3 |
| C8 | Sales Order edit deletes `BILLBYBILL` Receipt rows (4386–4388) that save never writes. Who writes them? (§10.11) | Open |
| C9 | Sales Order reopen doesn't reset `CORDT.CLOSES` / `STKDT.CLOSES` (§10.12) | Open. Check every caller of `CordTDB`/`StkDtDB` close methods with `Close=false` |
| C10 | Sales Order edit lock once an invoice/DN consumed it (`SalesOrder.cs:4270–4280`) (§10.13) | Business rule, not a defect. The API must enforce it |
| C11 | API SPs don't write `STKDTPHY`/`SNTSTKPHY` (D4) | Open. Verified during the line-by-line SP check (D9) |
| C12 | Missing `ASK_SP_ManageSalesReturnDetails` + UDTs (§10.7) | Confirmed against the live DB (`api-review.md` A2). Tracked as W2 |
| C13 | Approval writes. `APRVLMODULETB` is config only (read). `UpdateDetails`/`SaveDetails` write `APRVLST`/approver columns on SALM **and** on the generated auto-DN (`GDNM`) and receipt/refund vouchers (`YVR`): `'H'` while pending, `''` when fully approved (`01-file-map.md` §2.7 note) | Mapped. Phase 2 documents the state machine (`P`/`H`/`A`/`''`). D9 checks that the API SPs set the same values. Also confirm the SO advance `YVR` has no equivalent |

## 7. Legacy defects not to copy

These are confirmed legacy bugs. The API must not reproduce them. Phase 4 states how each one is avoided.

| # | Defect | Evidence | What the API does instead |
|---|---|---|---|
| L1 | **No transaction.** Each save runs 10–20 auto-committed statements (header, lines, stock, batch, serials, YVR, BILLBYBILL, approval, log). A failure midway leaves a partial voucher | `Sales.cs` `btn_save_Click` 5166–6152; `SalesReturn.cs` 4018–4199; `SalesOrder.cs` 4367–4424 | One DB transaction per save/cancel (inside the SP) |
| L2 | **Sales Return reports success after a failed stock save.** The `SaveStkDt == false` branch closes at 4165. Approval, log, counters and the success message still run | `SalesReturn.cs:4082–4229` | Any failed write fails the whole save |
| L3 | **Sales Order advance can be one-sided.** If the Dr cash leg saves and the Cr party leg fails, nothing is undone and no error is shown | `SalesOrder.cs:4408–4410` (the `else` at 4457 belongs to the first leg) | Both legs in the same transaction |
| L4 | **A failed update leaves a header with no lines.** The failure path deletes SALM only in Save mode, but deletes SALT/SALD (CORDT/CORDD) in both modes. A failed update therefore keeps the updated header and loses all its lines | `SalesReturn.cs:4231–4258`; `SalesOrder.cs:4457–4470`, `4566–4595` | Rollback restores the previous version |
| L5 | **SQL built by string concatenation** in every legacy DB method | e.g. `CordMDB.cs:711`, `StkDtDB.cs:30`, `SalTDB.cs:32` | Parameterized SQL / TVPs only |
