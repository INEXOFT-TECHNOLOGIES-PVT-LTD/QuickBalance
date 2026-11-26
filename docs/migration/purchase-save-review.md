# Purchase Save: Review of the Analysis Guide against Legacy Source

Analysis only. No code or database objects were modified. Done without agents, using Read / Grep / Glob (plus one read-only `sed`/`wc` column count).

- Guide reviewed: `docs/QuickBalance_Desktop_Application_Analysis_Guide.md` (the file is under `docs/`, not `docs/migration/` as referenced in the request).
- Previous report `docs/migration/purchase-form-save-workflow.md` covers Step 1 only (handlers); it is not re-verified here.
- Legacy source: `D:\PROJECT\IFlare\Inventory\Inventory\` (abbreviated `P.cs` = `View\Purchase.cs`, `Model\` = `Model\*.cs`).
- DB script: `D:\PROJECT\Salman\Latest\QuickBalance\DB\ProjectDB\QuickBalance.sql`.

Tags: `[VERIFIED: file:line]` means read directly. `[ASSUMPTION]` means not confirmed in code. `NOT READ` means located but not opened.
Verdicts: CORRECT / INCORRECT / PARTIAL / CANNOT VERIFY.

---

## 1. Real Save call order inside `btn_save_Click` (P.cs:6157)

The first click on an "Edit" button only switches the form to Update mode (P.cs:6171-6274); nothing is saved. The save path (the `else` branch at 6275) runs in this order:

| # | Step | Evidence |
|---|---|---|
| 1 | `Functions.HB_ValidateSaving_Transactions(btn_save,"Purchase")`; returns if blocked | [VERIFIED: P.cs:6159; Functions.cs:2160-2219] |
| 2 | `Functions.FYValidation(dtp_pur_date)`: only a warning, always returns true | [VERIFIED: P.cs:6162-6163; Functions.cs:2831-2846] |
| 3 | Remove trailing blank grid row (new mode only) | [VERIFIED: P.cs:6277-6281] |
| 4 | `CalculateAdditionalCost()`; if `Variables.LINEMERGEPI` then `Consolidate(); LM();` | [VERIFIED: P.cs:6283-6289] (bodies NOT READ) |
| 5 | **`Validation()`** (see section 4) | [VERIFIED: P.cs:6291] |
| 6 | Approval refresh and `ApprovalModuleDB.SaveConfirm` / `CommonDB.SaveConfirm(btn_save)` yes/no prompt | [VERIFIED: P.cs:6293-6311] (dialog text NOT READ) |
| 7 | **Calculations: `SubTotal(); TaxSummary(); AddInfoCalculation(true); NetCalculation();`**, which run AFTER validation | [VERIFIED: P.cs:6314-6317] |
| 8 | E-invoice XML (`ManageEinvoiceXML_API/_DIRECT`), only if `Variables.EinvoiceMode` is set | [VERIFIED: P.cs:6319] (NOT READ) |
| 9 | **`SavePurM()`**: number generation (`FyDetailsDB.ValidateVoucherNo("PUR","GET")`, `InvNoText`) then `PurmDB.insert` / `update` | [VERIFIED: P.cs:6321, 3072-3101] |
| 10 | New: `SaveEinv(); SaveDoc();`. Edit: `PurTDB.DeleteByVrNo`, `PurDDB.DeletByVrNo`, `SaveEinv()` | [VERIFIED: P.cs:6324-6338] |
| 11 | `SavePurT()` → `SavePurD()` → `SaveStkDt(OrdNo, GrnNo, "P")` → `UpdatePurRate()` (return value ignored) | [VERIFIED: P.cs:6340-6347] |
| 12 | If `chk_trck` (GRN tracking): `SaveGrnM` → (edit: delete GRNT/GRND, `DeductStock("Cancel")`, `BatchDetDB.DeleteByVrNo`) → `SaveGrnT` → `SaveGrnD` → `SaveStkDt(...,"RN")` → if `Variables.batch`: `SaveBatchDet()` then `SavePrSt()` (only if `ApprovalInventoryStatus`) → if `Variables.SlNoTrack`: `SaveSntStk()` | [VERIFIED: P.cs:6348-6398] |
| 13 | Part-payment voucher (`YvrDB.Delete`, `SavePayment` ×2, `SaveAdjustment`) or cancel of the old payment voucher | [VERIFIED: P.cs:6401-6434] |
| 14 | Credit mode (payment = 2): modal `BillAdjustment` dialog; `BillDB.DeleteByRefNoAndVrType` | [VERIFIED: P.cs:6436-6463] |
| 15 | Approval details save/update; `ConfigDB.UpdateLasVrDate` (new only) | [VERIFIED: P.cs:6465-6474] |
| 16 | Voucher counters: `FyDetailsDB.ValidateVoucherNo("PUR","SET",OrdNo)` (or `UpdateVoucherNo`), same for GRN and payment; `UpdateVoucherSystemOrdNo("PI"/"GRN"/"PV")`; `UpdateTransferFlg_PRV()`; `LogNote("create"/"edit")` | [VERIFIED: P.cs:6476-6499] |
| 17 | `CommonDB.SuccessMessage()` (yes → `CrrPrint()`); new → `Clear()`; edit → `New()` or `Updated=true; Close()` | [VERIFIED: P.cs:6501-6520] |

**Without GRN tracking** (`chk_trck` false) the `else` at P.cs:6618-6739 runs: no GRN, no `SaveBatchDet` / `SavePrSt` / `SaveSntStk`; payment and counter handling are repeated.

---

## 2. Claim-by-claim verification

| # | Guide claim (section) | Verdict | Evidence / correct behaviour |
|---|---|---|---|
| 1 | Flow: Calculation → SubTotal → TaxSummary → AddInfoCalculation → NetCalculation → **Validate** → number → PURM → PURT → PURD → STKDT (§3) | **PARTIAL** | Order of writes is right, but validation runs BEFORE `SubTotal/TaxSummary/AddInfoCalculation/NetCalculation` [VERIFIED: P.cs:6291 vs 6314-6317]. `Validation()` therefore reads totals left by grid events. Number generation happens inside `SavePurM` after calculation [VERIFIED: P.cs:3072-3077], not before. `Calculation(int ri)` (P.cs:7589) is a per-row grid event, NOT READ. |
| 2 | "One SQL connection and one SQL transaction for all writes" (§1.1, §9) | **INCORRECT for legacy** (design target for the API) | Legacy has **no transaction**: no `BeginTransaction`/`Commit`/`Rollback` in `Purchase.cs` and none in `clsDBFunction.cs` (grep returned nothing). Each `db.Execute` is its own auto-committed statement [VERIFIED: clsDBFunction.cs:98-125]. Failure handling is manual compensation (section 6). The API using one transaction is stricter than legacy; that is fine, but it is a deliberate deviation. |
| 3 | `PurmDB.insert` inserts the header and returns the generated voucher number via `OUTPUT INSERTED.VRNO` (§5.1) | **CORRECT** | [VERIFIED: PurmDB.cs:21-22] `... OUTPUT INSERTED.VRNO VALUES(...)` then `db.InsertScalar(qry)`. SQL is string-concatenated (not parameterized). |
| 4 | PURM insert has "84 columns" (§5.1) | **INCORRECT (count)** | The column list in the guide matches the code exactly (sorted diff identical), but it contains **82** columns, not 84 [VERIFIED: PurmDB.cs:21; count by `wc`]. |
| 5 | `VRTYPE = "P"` | **CORRECT** | [VERIFIED: P.cs:2884] |
| 6 | `LOGCD` and `ULOGCD` use authenticated user | **CORRECT** | Insert writes `Variables.UserId` to both [VERIFIED: PurmDB.cs:21]. **Update writes only `ULOGCD`, never `LOGCD`** [VERIFIED: PurmDB.cs:47]. |
| 7 | `UDATE` and `CREATEDDT` use the invoice creation timestamp | **PARTIAL** | Both = `InvDateTime` argument [VERIFIED: PurmDB.cs:21]. **Resolved in round 2:** `InvDateTime` is a form field initialised with `DateTime.Now` when the form object is created [VERIFIED: P.cs:16275] and only refreshed inside the e-invoice paths [P.cs:16419, 16625]. For a normal purchase `CREATEDDT`/`UDATE` is therefore the time the form was opened, not the time of the save click; the API should use the request time. `APRVLRDT = DateTime.Now`, `APRVLRBY = UserId` [VERIFIED: PurmDB.cs:21]. Update sets `UDATE = DateTime.Now` [VERIFIED: PurmDB.cs:47]. |
| 8 | PaymentId → BTYPE: 1=C, 2=R, 3=CD, 4=B, else "" | **CORRECT** | [VERIFIED: P.cs:2913-2926]. The same mapping is repeated for PURT (P.cs:2599-2612) and PURD (P.cs:1603-1616). |
| 9 | ORDNO generated by `FyDetailsDB.ValidateVoucherNo("PUR","GET",0)` | **CORRECT**, with caveats | [VERIFIED: P.cs:3074-3078] in a `while (returnOrdNo == 0)` loop. SQL-mode GET runs `UPDATE FYDETAILS SET @Col = PURVRNO, PURVRNO = case when PURVRNO >= 0 then -1 else PURVRNO end` and returns old + 1, then `ChckVrNoExists` bumps to MAX+1 if taken [VERIFIED: FyDetailsDB.cs:777-785, 1329-1420]. It is a lock-marker scheme: the counter is set to -1 so concurrent callers get 0 and spin. The counter is restored only in `ValidateVoucherNo("PUR","SET",OrdNo)` after success (P.cs:6476-6477) or `UpdateVoucherNo("PUR")` (= `MAX(ORDNO)` from PURM where VRTYPE='P', or STARTNO-1) on failure paths [VERIFIED: FyDetailsDB.cs:891-895; P.cs:6538-6542, 6838]. The column is `FYDETAILS.PURVRNO` [VERIFIED: FyDetailsDB.cs:758]. |
| 10 | `INVNOTEXT` from `InvNoText("Purchase")` | **CORRECT** | Exists [VERIFIED: P.cs:15374]. Builds `PREFIX + SEPERATOR + zero-padding (FIXLENGTH='Y', LENGTH) + number + SEPERATOR + SUFFIX` from `VRNOCONF` (VrType = "Purchase", Series = "Main"); if the number is 1 it uses `STARTNO` [VERIFIED: P.cs:15409-15448]. Embedded financial-year text: `VRNOCONF.EMBDATE` = "Prefix" or "Suffix" plus `EMBFORMAT` `YYYY-YY` / `YY-YY` / `YYYY` is added from `fyStartDate`/`fyEndDate` [VERIFIED: P.cs:15451-15492] (read in round 2). For manual voucher mode (`ProjectName=="Quick Balance" && !VRSYS && MVRSYS`) INVNOTEXT is the user-typed `txt_vr_no` [VERIFIED: P.cs:3082-3085]. |
| 11 | "`GRNNO` must not trigger GRN creation; header value needs a confirmed rule" | **PARTIAL** | Rule: `chk_trck` unchecked → `PURM.GRNNO = 0` [VERIFIED: P.cs:2993-2994]. Checked and `GrnNo==0` → `clsPurM.GrnNo = clsGrnM.OrdNo` [VERIFIED: P.cs:2975-2979], and the form then creates GRN rows (section 3). **Resolved in round 2:** `InvNoText("Goods Receipt Note")` takes the next GRN number (`ValidateVoucherNo("GRN","GET")`, sets `IsNewNbrGnrtdRD`) and assigns `clsGrnM.OrdNo = MaxOrdNo` at its end [VERIFIED: P.cs:15399-15403, 15500-15501], so `PURM.GRNNO` is correct at P.cs:2979. The GRN number is consumed inside `SavePurM`, before the PURM insert. For the API: `TrackingEnabled=false` must mean `GRNNO=0` with no GRN. |
| 12 | Cash/bank header fields preserved without creating payment vouchers | **PARTIAL** | Cash (1): `ADJCD` = cash ledger (or `Variables.CashCD`); bank (4): `ADJCD` = bank ledger; **`CASHPAID` and `YVRNO` are forced to 0** in both [VERIFIED: P.cs:2997-3020; the earlier `CashPaid = ToBase(...)` at 3004/3016 is overwritten at 3006/3018]. Other modes with `txt_cash_paid > 0`: `ADJCD=0`, `CASHPAID=ToBase(cash_paid)`, and **`YVRNO` is generated from the `PV` counter** (`ValidateVoucherNo("PV","GET")`) and a PV voucher is created later [VERIFIED: P.cs:3021-3047, 6401-6416]. If payment vouchers are out of scope the API must write `CASHPAID=0, YVRNO=0` and must not consume the PV counter. |
| 13 | PURM totals come from the calculation pipeline | **CORRECT** | `NtValue=ToBase(txt_total)`, `TValue=ToBase(txt_to_amt)`, `Tax=ToBase(txt_tot_tax_amt)`, `Amount=ToBase(txt_pay_amt)`, `Freight=ToBase(addInfo row0)`, `DiscVal=ToBase(row1)`, `CusDuity=ToBase(row2)`, `CoinAdj=ToBase(row4)`, `DiscP=Round(row1 %, FourDecimalPoint)`, `Mrp=ToBase(txt_tot_mrp)` [VERIFIED: P.cs:2901-2938]. All values are read from on-screen text boxes after the section-1 step 7 calculations. |
| 14 | PurTDB insert: one PURT row per item; column list | **CORRECT** | `PurTDB.insert` exists; `INSERT INTO PURT(...)` with 64 columns [VERIFIED: PurTDB.cs:20, column count by `wc`]. The guide's list was not diffed column-by-column. |
| 15 | `PRRATE = ToBaseActual(Price)` if `INVAMTEDIT_PI` else `ToBase(Price)` | **CORRECT** | [VERIFIED: P.cs:2594-2597]. `CONVRATE` always uses `ToBaseActual` (`if (true)`) [VERIFIED: P.cs:2708-2714]. |
| 16 | `DISC` uses actual/base conversion | **CORRECT** | `DISC = ToBaseActual(col_disc)`, else 0 [VERIFIED: P.cs:2615-2618]. `LDISCAMT = ToBase(col_Ldisc_value)` [P.cs:2732-2735]. |
| 17 | `TAX` depends on invoice format; do not assume request TaxPercentage | **PARTIAL (surprising)** | If format ≠ 4 and grid `Tax` cell non-empty: `PURT.TAX = grid Tax` (no currency conversion). Otherwise (format 4, or empty): `PURT.TAX = Convert.ToDecimal(dgv_add_info.Rows[1].Cells["Col_value"].Value)`, i.e. the **invoice-level discount amount** [VERIFIED: P.cs:2619-2622]. `TAXCODE` is the grid `col_taxCode` [P.cs:2614]. Probable legacy quirk; reproduce or decide explicitly. |
| 18 | `GDWCODE`: item godown else branch default | **CORRECT** | [VERIFIED: P.cs:2623-2626]; `Validation()` also fills an empty godown with `Functions.GetGodownByBranch()` when the user has no default/allowed godown [P.cs:3302-3313]. |
| 19 | `STKFLG="R"`, `RETURNFLG=false`, `ORGQTY=0`, `ORGRATE=0`, `CANCELFLG=false` | **CORRECT** | [VERIFIED: P.cs:2631-2633, 2661-2663] |
| 20 | Expiry/mfg dates default to purchase date | **CORRECT** | [VERIFIED: P.cs:2770-2777] |
| 21 | Additional cost via four-decimal conversion when positive | **CORRECT** | `ToFourBase` only if `addCost>0 || convAddCost>0`, else 0 [VERIFIED: P.cs:2791-2801] |
| 22 | Open question: batch/serial persisted in PURT? | **ANSWERED** | `PURT.BATCH` = grid Batch [VERIFIED: P.cs:2760-2763]; `SNTSLNO` = grid `col_slno` [P.cs:2787]. Batch master rows go to **BATCHDET / PRST** and serial numbers to **SNTSTK** via separate methods (section 3), only when `chk_trck` is on. |
| 23 | PURD section A (VAT detail rows) | **CORRECT** | [VERIFIED: P.cs:1629-1688] Rows with `Amt != 0`; `ETYPE="T"`; `CODE = Variables.VATCode` if `chk_vat` else grid `Code`; `AMOUNT=ToBase(Amt)`; `TAX=Round(TaxPerc, FourDecimalPoint)`; `TAXAMT=ToBase(Vat)`; `DFLAG=true`; `SLCODE` = cost centre only when `CostC && !multi`; `ADJCD` = cash/bank ledger by payment; `GCCCD=0`. |
| 24 | PURD section B (add-info rows) | **CORRECT**, with a stale-state note | [VERIFIED: P.cs:1692-1761]. Row included if value ≠ 0 or last row. `ETYPE="T"` for index 3 (**and index 3 is never inserted**), else "A". `AMOUNT`: j==2 → `txt_gross_amt`, j==3 → `txt_netAmt`, else `txt_total` (all `ToBase`). `DFLAG = !(j > 4)`. `GCCCD` = `Col_GCCHead`. `ADJCD` is not set in this loop, so it keeps whatever the VAT loop left (initially `CashCD` if cash else 0, P.cs:1623-1626). |
| 25 | PURD section C: "Empty allocation groups are valid; do not fail the whole invoice" | **INCORRECT** | `SavePurD` returns true only when `f==1` [P.cs:1812]. `f` is set to 1 only inside the tax-account loop, which runs only if `dtTemp` has rows [P.cs:1773-1810]. If no row has `col_tax_acc`, `f` stays 0 and **`SavePurD` returns false → "Error in Data insertion"**. Quote: `if (i == dgv_vat_details.Rows.Count && j == dgv_add_info.Rows.Count && f==1) return true; else return false;`. Rows are filtered on `col_tax_acc != null && Amount != null` [P.cs:1767-1771]. In practice every product has a tax account, so the form never hits this; the API should either require a tax account per item or knowingly relax the rule. Also `ETYPE="D"` rows: `TAX=0, TAXAMT=0, ORGTAXAMT=0, GCCCD=0`, `AMOUNT=ToBase(sum of line Amount per tax account)` [P.cs:1776-1787]. |
| 26 | STKDT: `VRTYPE="TR"`, `TYPE="R"` for purchase | **CORRECT** (for the "P" call) | `SaveStkDt(…,"P")`: `VrType="TR"`, `Type="R"`, `TrnNo=OrdNo` [VERIFIED: P.cs:1279-1291]. With `chk_trck` a **second pass `SaveStkDt(…,"RN")` writes `VRTYPE="NT"`** rows [P.cs:1284-1287, 6385]. |
| 27 | STKDT `UTIME` / `LUTIME` | **CORRECT** | `UTIME = now` when `uType` is "" or "S" (new), else the old time read by `GetUTime`/`phy_GetUTime`; `LUTIME = DateTime.Now` [VERIFIED: P.cs:1294-1298; StkDtDB.cs:16]. |
| 28 | STKDT `RATE` = price − line discount − general discount, then base conversion | **CORRECT** | [VERIFIED: P.cs:1344-1357]. Note `dgr.Cells["Price"].ToString() != ""` compares the cell's type name, so it is always true (harmless bug, P.cs:1345). |
| 29 | "Only normal `STKDT` is inserted"; `STKDTPHY` is separate | **INCORRECT** | `SaveStkDt` writes **STKDT if `ApprovalInventoryStatus`, otherwise STKDTPHY** (`phy_insert`) [VERIFIED: P.cs:1557, 1564, 1573], and **always deletes both** STKDT and STKDTPHY first (`DeleteByVr` + `phy_DeleteByVr`) [P.cs:1302-1303, 1307-1308]. **Resolved in round 2:** `ApprovalInventoryStatus = GetInventoryApprovalStatus(...)` is `!Variables.ApprovalSystem || ApprovalLevel == 0 || !Variables.InventoryApproval || ApprovalValueStatus || (ApprovalStatus != "P" && ApprovalStatus != "H") || (Level 1 && Approval=="1") || (Level 2 && Approval=="2") || Approval=="1&2"` [VERIFIED: ApprovalModuleDB.cs:59-62]; it is also initialised to `true` on form clear [P.cs:4589]. So with the approval system off (or `ApprovalLevel == 0`) the form always writes **STKDT**, never STKDTPHY. |
| 30 | SubTotal accumulates discountable amt, qty, amount, tax, total, MRP | **CORRECT** | [VERIFIED: P.cs:7397-7439] Sums grid cols `col_discountable_amt`, `Qty`, `Amount`, `TaxAmount`, `Total`, and `col_mrp*Qty` into the footer boxes. |
| 31 | AddInfoCalculation bases | **CORRECT** | [VERIFIED: P.cs:7292-7347] Row 0 = freight, 1 = discount, 2 = customs duty, 3 = VAT, 4 = round-off. `disc = Discountable × pct/100` (not rounded); `cd = (total + freight) × pct/100` (not rounded); `vat = Round((gross + cd) × pct/100, DecimalPoint)`. |
| 32 | NetCalculation: gross, net, payable | **CORRECT**, plus a flag | [VERIFIED: P.cs:7387-7389] `gross = Round(total + freight)`; `net = Round(gross + cd + vat)`; `payable = Round(net + roundOff + AddAmt)` (AddAmt = sum of rows > 4). **The `discount` variable is read but never used** (the `- discount` version is commented out at P.cs:7386); discount is already inside the line totals. **Confirmed in round 2** (section 12.1): `Calculation(ri)` computes `Amount = (NetPrice − disc_price) × qty`, where `disc_price` already applies the invoice-level discount percentage per line [P.cs:7876, 7888]. |
| 33 | TaxSummary groups by tax code, depends on format/VAT | **PARTIAL** | [VERIFIED: P.cs:7447-7570] Rebuilds `dgv_vat_details`; if `chk_vat` is off: per tax code sums `Amount` and `TaxAmount`; format 4 (unregistered) sums `Amount` only (tax stays 0); format 5 (exempt) only tax types "E" [P.cs:7499-7532]; rounded `MidpointRounding.AwayFromZero` to `DecimalPoint`. If `chk_vat` is on: a single row from gross + customs duty and the VAT amount, with `Variables.VATCode`. Format 5 uses `TTYPE=="E"`, format 4 branches on `TTYPE=="UR"` but both inner branches add `Amount`. |
| 34 | `PurmDB`, `PurTDB`, `PurDDB`, `StkDtDB`, `FyDetailsDB.ValidateVoucherNo`, `InvNoText` exist | **CORRECT** | `PurmDB.insert` PurmDB.cs:17; `PurTDB.insert` PurTDB.cs:16; `PurDDB.insert` PurDDB.cs:15; `StkDtDB.insert` StkDtDB.cs:12; `FyDetailsDB.ValidateVoucherNo` FyDetailsDB.cs:754; `InvNoText` P.cs:15374. |
| 35 | Repository sequence: PURM → PURT → PURD → STKDT, verify each | **PARTIAL** | Write order matches (P.cs:6321, 6340-6345). Legacy "verification" is only `> 0` / `i == Rows.Count`; there is no totals check. Edit mode deletes PURT/PURD first (section 5). |
| 36 | GRN creation, payment voucher creation, approval are out of scope | **PARTIAL** | Those are separate writes in the legacy flow, but several header/detail values depend on them (`GRNNO`, `YVRNO`, `CASHPAID`, `APRVLST`, `STKDT` vs `STKDTPHY`, `NTNO`). See section 3. |
| 37 | `SavePurt_Sql` / stored procedure `ManagePurchase_Purt` is part of the Save | **INCORRECT (if assumed)** | `SavePurt_Sql` (P.cs:6116) calls `SP.ManagePurchase_Purt` (SP.cs:368-431, `DataTransactionsByProcedure`), but **every call is commented out** (P.cs:3344, 6344, 6384). The live Save uses inline string SQL only; the SP is dead code in this form. The SP body in `QuickBalance.sql:7673` is NOT READ. |
| 38 | Product alternate-unit lookup, alias lookup, tax-account lookup, godown default, LM expansion (§7 "pending evidence") | **PARTIAL** | Alternate unit: `PrMastDB.UnitConDetailsByCode` → `ALTUNIT` [P.cs:2666-2671] (query NOT READ). Godown default: `Functions.GetGodownByBranch()` (body NOT READ). Tax account: grid `col_tax_acc` (source query NOT READ). Alias: grid `col_aliasCode` / `col_aliasName` [P.cs:2816-2817]. Line-merge: `dtLM` rows expand into several STKDT rows [P.cs:1475-1566]; `LM()` NOT READ. |

---

## 3. Tables written that the guide does NOT mention

All are inline SQL via `Model\*DB` classes, auto-committed, no transaction.

| Table | When | Operation | Evidence | Status |
|---|---|---|---|---|
| **STKDTPHY** | Whenever `!ApprovalInventoryStatus`, plus every STKDT delete | DELETE (always), INSERT (conditional) | P.cs:1302-1303, 1557-1573 | partly read |
| **PURT** | Every save | INSERT; edit: DELETE then INSERT | P.cs:6332-6335, 2819 | in guide |
| **GRNM / GRNT / GRND** | `chk_trck` checked | INSERT; edit: DELETE (GRNT, GRND) + UPDATE (GRNM) then re-insert | P.cs:6350-6385; `SaveGrnM` P.cs:14486, `SaveGrnT` :14602, `SaveGrnD` :14932 | headers read; bodies largely NOT READ |
| **STKDT rows with VRTYPE `NT`** | `chk_trck` | DELETE/INSERT via second `SaveStkDt` | P.cs:6385, 1284-1287 | read |
| **BATCHDET** | `Variables.batch && chk_trck` | INSERT (edit: `DeleteByVrNo` first) | P.cs:6388-6390, 6363-6365; `SaveBatchDet` P.cs:2004 | first 60 lines read; insert SQL NOT READ |
| **PRST** | `Variables.batch && chk_trck && ApprovalInventoryStatus` | INSERT (edit-delete is commented out at P.cs:6367-6369; `DeductStock("Cancel")` at :6361 may do it, NOT READ) | P.cs:6391-6392, 1824 | first 70 lines read |
| **SNTSTK (+ phy variant)** | `Variables.SlNoTrack` | DELETE (`DelteByVoucherIn`, `Phy_DeleteByVoucher`) then INSERT or `phy_insert`; qty must equal serial rows | P.cs:6397-6398, 2167-2218 | read to line 2226 |
| **YVR** (payment voucher) | `txt_cash_paid > 0` | DELETE old, INSERT 2 rows (`SavePayment` cash/supplier) with `YvrType="P"`, `VrType="PV"`; or `YvrDB.Cancel` | P.cs:3114-3176, 6401-6434 | read; `YvrDB.insert` SQL NOT READ |
| **Bill-by-bill table** (`BillByBillDB`) | part payment (`SaveAdjustment`) and credit mode (`BillAdjustment` dialog) | DELETE + INSERT (`Stype="Payment"`, `VrType="Purchase"`, `GenType="P"` / `"PB"`) | P.cs:3187-3216, 6429, 6455-6462 | table name NOT READ |
| **FYDETAILS** | every save | UPDATE `PURVRNO` / `PSRTNNO` (GRN) / `GPVRNO` (PV) | FyDetailsDB.cs:754-824, 891-895; P.cs:6476-6489 | read |
| **VRNOCONF** | number text | SELECT only | P.cs:15409-15412 | read |
| **Config table** (`ConfigDB.UpdateLasVrDate`) | new save | UPDATE last voucher date | P.cs:6470-6474 | table NOT READ |
| **Voucher-system tables** (`VoucherSystemDB.Manage`, `GetSubDocNo`, `UpdateOrdNoChekByTable`) | `Variables.VRSYS` | read/update; sets `PURM.VSVRTYPE/VSDOCNO/VSORDCD` | P.cs:2981-2987, 3087-3092, 6489-6491 | NOT READ |
| **Approval tables** (`ApprovalModuleDB.SaveDetails/UpdateDetails`) | `ApprovalLevel > 0` | INSERT/UPDATE; `PURM.APRVLST` = "" / "S" / "P" | P.cs:6465-6468, 3058 | NOT READ |
| **E-invoice / document tables** (`SaveEinv`, `SaveDoc`) | new: after PURM; edit: SaveEinv | INSERT | P.cs:6327-6336; `SaveEinv` :17077, `SaveDoc` :18541 | NOT READ |
| **PRMAST rates** (`UpdatePurRate`) | every save after STKDT | reads `PRMAST`; the update SQL is NOT READ | P.cs:6347, 5841 | partly read |
| **Audit/log** (`LogNote`) | every save | NOT READ (P.cs:15629). Errors also go to a text file via `ExceptionLogging.SendErrorToText`. | P.cs:6496-6499 | NOT READ |
| **Previous-year flag** (`UpdateTransferFlg_PRV`) | `Variables.PrevYearConn != ""` | UPDATE | P.cs:6493-6494 | NOT READ |

**Triggers:** the DB script `QuickBalance.sql` contains **no `CREATE TRIGGER`** statements (grep, case-insensitive, returned none) [VERIFIED]. Tables PURM, PURT, PURD, STKDT, STKDTPHY, PRST, BATCHDET, SNTSTK, GRNM, GRNT, GRND, FYDETAILS exist at QuickBalance.sql lines 3700, 3802, 3663, 4531, 4583, 3515, 636, 4485, 2340, 2422, 2305, 1845.

---

## 4. Validation: every check, in execution order, with exact message

### 4a. Before / around `Validation()` (P.cs:6157-6311)
1. HB module permission: `"<FormName> creation or modify is not possible because of module permission is denied."` (button disabled) [Functions.cs:2207-2211]. Only when `Variables.HB_active` and company matches.
2. Edit with tagged (`lbl_block.Visible && Variables.TrackEdit`): `"Editing Purchase will remove all marking against this Purchase.If click 'Yes' you can't retrun back.Are you sure to continue?"` (Yes/No) [P.cs:6178].
3. `FYValidation`: warning `"⚠ Warning! You are modifying or creating a voucher outside the current financial year."`, only if `NextYearConn != ""` and the date ≠ today; it never blocks [Functions.cs:2831-2846].
4. Approval gating (`ApprovalModuleDB.ManageAprvl` / `SaveConfirm`; messages NOT READ) [P.cs:6293-6311].
5. `CommonDB.SaveConfirm(btn_save)` yes/no (text NOT READ) [P.cs:6311].

### 4b. `Validation()` (P.cs:3231-3833), in order
| # | Check | Exact message | Line |
|---|---|---|---|
| 1 | Manual voucher mode (`QuickBalance`, !VRSYS, MVRSYS): voucher no blank | `"Please Enter Voucher Number."` | 3243-3246 |
| 2 | Same: duplicate `INVNOTEXT` (`PurmDB.CheckInvNoTextExists`) | `"Voucher Number Already Exists."` (shown later, step 13) | 3253-3256, 3594-3599 |
| 3 | Per row (if `Description` not null): Qty == 0 | `"Quantity is zero at Row N"` | 3285-3291 |
| 4 | Price == 0 → Yes/No confirm | `"Price is zero at Row N. Are you sure you want to continue anyway?"` (No → abort) | 3292-3297 |
| 5 | `CheckOrdQty(row)` (order qty; body NOT READ) | (inside method) | 3298 |
| 6 | Godown ≤ 0 and user has a default or allowed godown | `"Invalid Godown at Row N"`; else filled with `GetGodownByBranch()` | 3302-3313 |
| 7 | `Variables.batch && chk_trck` → `ValidateBatch(row)` (4c) | | 3316 |
| 8 | Serial tracking (`SlNoTrack && chk_trck`): see 4d (inside `try { } catch (Exception ex) { }`, so exceptions are silently swallowed) | | 3321-3479 |
| 9 | Line merging: `Qty ≠ sum(dtLM.QTY)` | `"Entered quantity and reference quantity do not match for the selected product '<name>' at Row N."` | 3509-3514 |
| 10 | `ValidateTaggedVoucher()` (4e) | | 3519 |
| 11 | Edit + batch + tracking: `DeductStock("ValidateUpdate")` fails | `"Batch Stock Will Go To Negative For Product For Product <name>."` (note the duplicated word; the code concatenates `"…For Product" + " For Product "`) | 3522-3526 |
| 12 | Supplier < 1 | `"Invalid Supplier"` | 3531-3536 |
| 13 | Format < 1 | `"Invalid Invoice Format Selected"` | 3537-3542 |
| 14 | VAT party, foreign GCC supplier, format ∉ {6,7} | `"Invalid invoice type selection, please must choose invoice type as 'GCC Purchase' or 'Exempt' for outside party (GCC Supplier)."` | 3543-3548 |
| 15 | VAT party, foreign non-GCC supplier, format ∉ {8,7} | `"Invalid invoice type selection, please must choose invoice type as 'Outside GCC' or 'Exempt' for outside party (Outside GCC Supplier)."` | 3549-3554 |
| 16 | VAT party, local supplier, format ∈ {6,8} | `"Invalid invoice type selection, please must not choose invoice type as 'GCC Purchase' or 'Outside GCC' for inside party."` | 3555-3560 |
| 17 | Payment < 1 | `"Please Choose Method of Payment"` | 3567-3572 |
| 18 | Grid has no rows | `"Item Details Must not be blank"` | 3573-3578 |
| 19 | `txt_total` blank | `"Total Amoount is Blank.Please check the inputs"` | 3579-3584 |
| 20 | `InvoiceAmountValidation(txt_pay_amt)`: negative net | `"Invoice net amount is less than zero."` | 3585; Functions.cs:2101-2110 |
| 21 | Manual voucher message from steps 1-2 | `MVR_MSG` | 3594-3599 |
| 22 | `CommonDB.ExpiryCheck(btn_save,"PURM","Transaction",date)` (demo limit; body read only to line 356, rest NOT READ) | | 3600-3604 |
| 23 | VAT-enabled party with format 4 | `"You Have Selected VAT Enabled Party.Please Chose Valid Invoice Type."` | 3611-3616 |
| 24 | Non-VAT party with an invalid format | `"You Have Selected VAT Disabled Party.Please must choose invoice type as 'Not Registered' or 'GCC Purchase' for outside party (GCC Supplier)."` / `"...or 'Outside GCC' for outside party (Outside GCC Supplier)."` / `"You Have Selected VAT Disabled Party.Please Chose Valid Invoice Type."` | 3617-3627 |
| 25 | Bank payment with no bank ledger | `"Invalid Bank Ledger Selection"` | 3628-3633 |
| 26 | Cash payment with no cash ledger | `"Invalid Cash Ledger Selection"` | 3634-3639 |
| 27 | New + `VRSYS` with no doc type | `"Missing Document Type"` | 3640-3645 |
| 28 | `CommonDB.CheckDate(purchase date)` (date block) | `"Voucher date is less than last voucher date\r\nSo can't update this voucher"` (SuperUser with `SUDateBlock`) or `"Backdated Entries Not Allowed On This User. Please Contact Your Super To User."`; compares to `BLOCKDT` (<=) or `LastVrDate` (<) | 3688-3689; CommonDB.cs:1424-1473 |
| 29 | Supplier bill no blank | `"Missing invoice number."` | 3720-3725 |
| 30 | Duplicate supplier + bill no (`PurmDB.SearchBySupplierBillNoAndType`) | New: `"Purchase Bill with same Invoice.No already is generated for the selected Supplier"`. Edit, different voucher: `"Purchase Bill with same Invoice.No is already generated for the selected Supplier"` | 3695-3719 |
| 31 | Row loop: product blank | `"Invalid Product choosen"` | 3731-3736 |
| 32 | Qty blank | `"Invalid Quantity Choosen"` | 3737-3742 |
| 33 | Qty ≤ 0 | `"Quantity must not be Zero"` | 3743-3748 |
| 34 | Unit blank or ≤ 0 | `"Invalid Unit choosen"` | 3749-3754 |
| 35 | Batch product (`PrMastDB.CheckBatched`) with empty batch when `chk_trck && batch` | row turns red; `"Invalid Batch Details"` | 3758-3771, 3800-3804 |
| 36 | Cash supplier (`CashCD`) with payment ≠ 1 | `"Must choose Payment Mode as 'Cash' For Cash Billing"` (also forces payment = 1) | 3807-3814 |

Not found in the paths read: an explicit "user right to save" check, a tax-percentage check, a bill-date check (`dtp_inv_date` is never validated), and a purchase-order stock-availability check for a plain new purchase. (Button enablement and user rights are probably decided at form load: NOT READ.)

### 4c. `ValidateBatch(row)` (P.cs:4310-4411), only when the row has a batch
- Existing batch with different rates / mfg / exp (`PrstDB.GetBatchForValidation`, excluding this voucher): `"Batch details already exists with different rates[ or Mfg/Exp date] for selected '<product>' product at Row N."` [4394]. Mfg/Exp parts depend on `PrMfgExpDetails` flags.
- Duplicate product + batch + godown in another grid row: `"Duplicate batch number detected for the product ‘<product>’ at rows N and M."` [4403].
- The whole body is wrapped in `try … catch { }`, so exceptions are swallowed and the method returns true.

### 4d. Serial-number validation (P.cs:3321-3479, all `MessageBox`, title "Warning")
- `"Serial number '<s>' is become duplicate for selected product '<p>'."` (or without product) [3356/3358]
- Edit: `"The serial number '<s>' has already been used for the product '<p>' and is missing."` [3404]
- `"Decimal numbers are not allowed with serial number tracking for selected '<p>' product at Row N."` [3426]
- `"Entered quantity is greater than serial number tracking record for selected '<p>' product at Row N."` [3431]
- `"Entered quantity is less than serial number tracking record for selected '<p>' product at Row N."` [3436]
- `"Serial number '<s>' is already in use for selected '<p>' product at Row N."` [3463]
- `"Missing serial number tracking details for selected '<p>' product at Row N."` [3471]

### 4e. `ValidateTaggedVoucher()` (P.cs:3834-4309)
For each item row with a product (and for each line-merge sub-row), the quantity is converted to base units (alias and alternate unit). It then checks it against the pending balance of the linked PO (`ORDNO` with no GRN) or the linked GRN (`NTNO`), accounting for quantities already used on earlier rows of the same form and, on edit with `ApprovalStockUpdate`, adding back the old PURT / LM quantities. Failure messages:
- `"The entered quantity of '{0}' exceeds the available pending quantity of '{1}' for Purchase Order Voucher '{2}' for the product '{3}' at Row {4}.\n\nPlease adjust the quantity to proceed."` [4119]
- `"The entered quantity of '{0}' exceeds the available pending quantity of '{1}' for Receipt Note Voucher '{2}' for the product '{3}' at Row {4}.\n\nPlease adjust the quantity to proceed."` [4290]

Other exceptions inside are shown with `MessageBox.Show(ex.Message)` [4305]. The previous-year (`PRV*`) branches use `StkDtDB.*_PRV_MULTI` (bodies NOT READ).

### 4f. After validation passes (inside `SavePurM`)
`CommonDB.ExpiryCheck` is repeated; a true result returns 0, so the save is aborted silently [P.cs:2881-2882].

---

## 5. Edit (Update) mode: delete-and-reinsert, not differential

Mode is chosen by `btn_save.Text == Variables.SaveText` vs `UpdateText` (`SavePurM`, P.cs:3072 / 3097) [VERIFIED].

| Table | Edit behaviour | Evidence |
|---|---|---|
| PURM | `UPDATE ... WHERE VRNO`. It does not update `ORDNO`, `LOGCD`, `CREATEDDT`, `VSVRTYPE/VSDOCNO/VSORDCD`, `APRVL*`; `ULOGCD` and `UDATE` are set | PurmDB.cs:47; P.cs:3099-3101 |
| PURT | `DELETE FROM PURT WHERE VRNO` then re-insert | P.cs:6332-6333; PurTDB.cs:33 |
| PURD | `DELETE FROM PURD WHERE VRNO` then re-insert | P.cs:6334-6335; PurDDB.cs:33 |
| STKDT / STKDTPHY | `DeleteByVr` (`TRNNO`, `TYPE='R'`, `VRTYPE='TR'`) and `phy_DeleteByVr`, then re-insert; the old `UTIME` is retained (`uType="U"`) | P.cs:1294-1303; StkDtDB.cs:52-53 |
| GRNT / GRND (if `chk_trck` and `G_VrNo>0`) | deleted by `G_VrNo`, re-inserted; GRNM is updated by `SaveGrnM` (rest NOT READ) | P.cs:6353-6358 |
| BATCHDET | `DeleteByVrNo(SType "P")`, then `SaveBatchDet` | P.cs:6361-6365 |
| PRST | delete is **commented out** (P.cs:6367-6369); `DeductStock("Cancel")` runs before (P.cs:6361); effect on PRST NOT READ | |
| SNTSTK | `DelteByVoucherIn` + `Phy_DeleteByVoucher`, then re-insert | P.cs:2171-2175 |
| YVR payment | `YvrDB.Delete` then re-insert when part-paid; else `YvrDB.Cancel` + bill delete when `YvrNo != 0` | P.cs:6401-6434 |
| Bill-by-bill | deleted and re-created | P.cs:3212, 6429, 6461 |
| Old tagging | with `TrackEdit` and `lbl_block`: `StkDtDB.UnmarkByVr` and `GrnTDB.UnMarkPurNo` before editing | P.cs:6184-6190 |

Ledger entries: **PURM/PURT/PURD carry the financial rows themselves** (PURD with `ETYPE` T / A / D); no separate ledger or voucher table write was found in the Save path read, other than YVR for part payment. Whether reports derive ledger entries from PURD is [ASSUMPTION].

---

## 6. Transactions and error handling

- **No real SQL transaction** (see claim 2). Every `Execute` auto-commits [VERIFIED: clsDBFunction.cs:98-125]. On a SQL error `Execute` swallows the exception and returns `-5`, storing the message in `Variables.errormsg` [clsDBFunction.cs:110-119].
- **Manual compensation, New mode only** (`if (btn_save.Text == Variables.SaveText)`):
  - `SavePurM` fails (returns ≤ 0): only counters are restored, with `"Error in Data insertion"` [P.cs:6806-6819].
  - `SavePurT` fails: delete PURM and PURT [P.cs:6786-6804].
  - `SavePurD` fails: delete PURM, PURT, PURD [P.cs:6762-6783].
  - `SaveStkDt` fails: delete PURM, PURT, PURD [P.cs:6741-6760]; `SaveStkDt` also deletes its own STKDT rows [P.cs:1587-1588].
  - GRN steps fail: delete GRNM/GRNT/GRND, `UnmarkCurrentVoucher`, `UnMarkByGrnNo`, plus PURM/PURT/PURD when `SaveGrnM` fails [P.cs:6594-6616, 6572-6592, 6547-6571, 6522-6545].
  - All failure paths also reset the counters with `FyDetailsDB.UpdateVoucherNo("PUR"/"GRN"/<PV>)`.
- **Edit mode has no compensation.** After PURT/PURD are deleted (P.cs:6333-6335), a failure in `SavePurT`, `SavePurD` or `SaveStkDt` leaves the voucher without its details and only shows `"Error in Data insertion"`.
- **Return values ignored** (no rollback if they fail): `UpdatePurRate()`, `SaveBatchDet()`, `SavePrSt()`, `SaveSntStk()` [P.cs:6347, 6390-6398]; the `SavePayment` / `SaveAdjustment` results only gate each other [P.cs:6406-6410].
- **Catch-all** (P.cs:6834-6843): logs to a text file, resets voucher counters, **shows no message to the user** (the `MessageBox` is commented out) and does not undo anything.
- Generic failure message: `"Error in Data insertion"` (title `"Error"`) [P.cs:6544 etc.].

---

## 7. Post-save behaviour (UI side) [VERIFIED: P.cs:6501-6520]
`CommonDB.SuccessMessage()` (text NOT READ); Yes → `CrrPrint()`. New → `Clear()`. Edit → `New()` (if opened from "new") or `Updated = true; this.Close()`. The next number is shown by `txt_vr_no.Text = InvNoText("Purchase")` in `Clear` / `New` [P.cs:4520, 4823]; this uses `ValidateVoucherNo("PUR","GETONLY")`, which reads without locking [P.cs:15386].

---

## 8. Missing steps / rules / validations not in the guide

1. Validation runs before calculation, and uses footer totals that the grid events produced (claim 1).
2. Voucher counter lock-marker scheme in `FYDETAILS.PURVRNO` (claim 9), including the reset on every failure path.
3. Full validation list in section 4 (the guide's §8.4 checklist is mostly unanswered): supplier, format ↔ supplier VAT / GCC / country rules, payment ↔ ledger rules, cash-supplier rule, bill-no mandatory and duplicate, voucher-date block, per-row qty / unit / godown / batch / serial rules, PO and GRN pending-quantity rules, negative batch stock on edit.
4. Conditional tables (section 3): STKDTPHY, GRNM/GRNT/GRND, BATCHDET, PRST, SNTSTK, YVR, bill-by-bill, FYDETAILS, config last-voucher-date, voucher system, approval, e-invoice, PRMAST rate update, log.
5. `chk_trck` switches on GRN creation, the second STKDT pass (`VRTYPE='NT'`), and batch/serial persistence.
6. Edit mode is delete-and-reinsert for PURT/PURD/STKDT (+STKDTPHY), with no compensation on failure.
7. `PURT.TAX` fallback to the invoice discount amount (claim 17).
8. `PURM.CASHPAID/YVRNO` handling per payment mode (claim 12).
9. `CONVRATE` always `ToBaseActual` (claim 15); `LDISCTYPE` literal strings `"Discount Based on %"` / `"Absolute Value"` / `""` [P.cs:2741-2750].
10. `PURT.VATREASONCD` = `ExportReason` for formats 4, 6, 7, 8, else the grid exception reason [P.cs:2781-2784].
11. Line-merge (`LM='Y'`) expands one grid row into several STKDT rows, and distributes additional cost per quantity [P.cs:1475-1566].
12. Currency conversion everywhere uses `currConvDB.ToBase / ToBaseActual / ToFourBase` (bodies NOT READ); the guide's checklist items 8.2 remain open.

---

## 9. Incorrect items and the correct behaviour (summary)

| Guide statement | Correct behaviour |
|---|---|
| One SQL transaction in the desktop flow (§1.1, §9) | None in legacy; auto-commit with manual compensation (section 6). |
| PURM has 84 columns | 82 columns; the list itself is right. |
| Empty tax-account allocation is valid (§5.3C) | `SavePurD` returns false if no `ETYPE='D'` row is written (P.cs:1812). |
| Only normal STKDT is written (§5.4) | STKDT or STKDTPHY by `ApprovalInventoryStatus`; both are always deleted first; an `NT` pass is added when `chk_trck`. |
| Flow starts with `Calculation()`/`SubTotal()` before validating (§3) | Validation first; `SubTotal/TaxSummary/AddInfoCalculation/NetCalculation` run after the confirm prompt; number generation after that. |
| `ManagePurchase_Purt` (implied alternative) | Dead code in this form (all callers commented out). |
| UDATE / CREATEDDT = creation timestamp (§5.1) | `InvDateTime` argument (source NOT READ); update touches only `UDATE`/`ULOGCD`. |

---

## 10. Suspected legacy bugs / oddities
1. `while (returnOrdNo == 0)` spin: if the app dies between `GET` (counter = -1) and `SET`, other clients loop forever (P.cs:3074-3076; FyDetailsDB.cs:777).
2. **Confirmed in round 2:** the duplicate-bill check on edit compares `txt_vr_no.Text` with `dr["VRNO"]` (P.cs:3711). `txt_vr_no` holds `PURM.INVNOTEXT` (or `ORDNO` when empty) after loading a voucher [VERIFIED: P.cs:4926-4928], while `SearchBySupplierBillNoAndType` returns `SELECT * FROM PURM` rows whose `VRNO` is the identity column [PurmDB.cs:2475]. `ORDNO`/`INVNOTEXT` and `VRNO` differ in general, so editing a bill can be wrongly rejected (or wrongly allowed). The API should compare identity to identity (`VRNO <> @vrNo`).
3. ~~`clsPurM.GrnNo = clsGrnM.OrdNo` before assignment~~ **Resolved in round 2:** assigned by `InvNoText("Goods Receipt Note")` (P.cs:15501); not a bug.
4. `clsPurT` is reused across rows, and `Rate`, `ConvType` are not reset when the grid cells are empty, so values can leak from the previous row (P.cs:2672-2673, 2778-2779).
5. `PURT.TAX` stores the invoice discount amount for format 4 (claim 17).
6. `SavePurM` computes `CashPaid` and then overwrites it with 0 (P.cs:3004-3006, 3016-3018).
7. `dgr.Cells["Price"].ToString() != ""` is always true (P.cs:1345).
8. `Validation()` wraps the serial-number block in `catch (Exception ex) { }`, and `ValidateBatch` in `catch { }`; both fail open.
9. Duplicate Qty messages ("Quantity is zero at Row N" vs "Quantity must not be Zero"); negative price is not checked.
10. "Batch Stock Will Go To Negative For Product For Product" duplicated wording (P.cs:3525).
11. SQL is built by string concatenation including free text (`REMARKS`, `SHIPADDRESS`, `PRDESC`), i.e. injection / quote-breaking risk; the API must parameterize.
12. Return values of several post-STKDT steps are ignored (section 6).

---

## 11. Round-1 NOT READ list (superseded; the current list is section 12.9)

- `Calculation(int ri)` (P.cs:7589) and the grid events that fill `Amount`, `TaxAmount`, `Total`, `col_discountable_amt`; `CalculateAdditionalCost` (P.cs:18165); `Consolidate` (5904); `LM` (17503).
- `currConvDB.ToBase / ToBaseActual / ToFourBase`; `Variables.FourDecimalPoint`, `DecimalPoint`, `INVAMTEDIT_PI`, `VATCode`, `CashCD`.
- Bodies of `SaveGrnM` (after line 14545), `SaveGrnT` (14602), `SaveGrnD` (14932); `GrnMDB` / `GrnTDB` / `GrnDDB`.
- `SavePrSt` (after 1893), `SaveBatchDet` (after 2063), `SaveSntStk` (after 2226) and `PrstDB` / `BatchDetDB` / `SntStkDB` insert SQL; `DeductStock` (P.cs:961, 1218).
- `YvrDB.insert/Delete/Cancel`, `BillByBillDB.insert/Delete*`, `ConfigDB.UpdateLasVrDate`, `VoucherSystemDB.*`, `ApprovalModuleDB.*` (including `GetInventoryApprovalStatus`), `LogNote` (15629), `UpdateVoucherSystemOrdNo` (15891), `UpdateTransferFlg_PRV`, `UpdatePurRate` (remainder of 5841), `SaveEinv` (17077), `SaveDoc` (18541), `ManageEinvoiceXML_*`.
- `CheckOrdQty`, `CommonDB.SaveConfirm` / `SuccessMessage` / `ExpiryCheck` (after line 356), `Functions.GetGodownByBranch`.
- `PurmDB.SearchBySupplierBillNoAndType` (PurmDB.cs:2471), `CheckInvNoTextExists` (2591), `UnMarkByGrnNo` (2679); `StkDtDB.GetUTime`, `phy_*`, `UnmarkByVr` (4888), `UnmarkCurrentVoucher` (4915).
- Where `InvDateTime` (passed to `PurmDB.insert`) is assigned; where `clsGrnM.OrdNo` is assigned.
- `ManagePurchase_Purt` body in `QuickBalance.sql:7673` (dead path, low priority).
- Remainder of `Clear()` / `New()` (P.cs:4412+) for exact post-save reset.
- Column types, nullability and constraints of PURM / PURT / PURD / STKDT in `QuickBalance.sql` (line refs in section 3); not compared with the guide's DTOs.

---

# 12. Round 2 addendum: additional source analysis

Analysis only; no code or database object was modified; no agents used. This section closes the blocking gaps from the readiness assessment. Where a round-1 statement is superseded, the original row was edited in place and says "Resolved in round 2".

## 12.1 Item-level calculation: `Calculation(int ri)` (P.cs:7589-7921)

It runs on every relevant grid edit and ends with `SubTotal(); TaxSummary();` [P.cs:7914-7915]. The Save step 7 (`SubTotal → TaxSummary → AddInfoCalculation(true) → NetCalculation`, P.cs:6314-6317) only re-aggregates; **per-line values are NOT recomputed at save: they are taken from the grid cells as left by the last `Calculation` run.** The API has to reproduce this routine per item.

Order inside `Calculation(ri)`:

| # | Step | Formula / rule | Evidence |
|---|---|---|---|
| 1 | Reload product data | `PURPRICETYPE`, `UNIT` (main), `ALTUNIT`, `UNITCONV` (ConvFact), `CONVTYPE`; the grid's `col_conv_fact` / `col_conv_type` override the master when non-empty | P.cs:7571-7588 |
| 2 | Auto price flag | if flag is empty/"N": set `col_price_flg="Y"` when `(PurPriceType==0 && unit==alt)` or `(PurPriceType==1 && unit==main)` | P.cs:7597-7603 |
| 3 | `INVAMTEDIT_PI` price reconciliation | compares `col_ActualPrice` with `Price` rounded to `DecimalPoint`; `PriceIsSame=false` when the actual price carries more decimals than `DecimalPoint` | P.cs:7609-7638 |
| 4 | Discount % | `discPer = col_disc_perc`, else the invoice discount % (`dgv_add_info` row 1 `Col_perc`) | P.cs:7644-7649 |
| 5 | Inputs | `taxPerc = Round(Tax cell, DecimalPoint)` only if `Variables.VatFlg && format != 4`; `qty = Round(Qty, QtyDecimalPoint)`; `price = Round(Price, DecimalPoint)` (or `DecimalPointActual` when `!PriceIsSame`) | P.cs:7656-7664 |
| 6 | Tax-inclusive format | format 3 and `!chk_vat`: `price = price / (1 + taxPerc/100)`. Format 4, 5 or `!VatFlg`: `taxPerc = 0` | P.cs:7666-7673 |
| 7 | Unit conversion (flag N/empty) | `PurPriceType 0` (price on main unit): if the selected unit is alt then `qty = qty*ConvFact` (convType 0) or `qty/ConvFact` (convType 1); `convPrice = price`. `PurPriceType 1` (price on alt unit): if the selected unit is main, `qty = qty/ConvFact` (type 0) or `qty*ConvFact` (type 1); `convPrice = price/ConvFact` (type 0) or `price*ConvFact` (type 1). Else (type 2): `convPrice = price/ConvFact` (type 0) or `*ConvFact` (type 1) when the unit is not main | P.cs:7676-7740 |
| 8 | Unit conversion (flag "Y") | type 0: `convPrice = price/ConvFact` (convType 0) or `*ConvFact` (convType 1); type 1: `convPrice = price` | P.cs:7741-7762 |
| 9 | Alias | `convPrice = convPrice / col_aliasQty` when `mult_alias && aliasCode>0 && aliasQty>0` | P.cs:7764-7765 |
| 10 | Converted columns | `col_convsalrate`, `col_convmrp`, `col_convprate`: value as-is for the main unit, else `×ConvFact` (convType 1) or `÷ConvFact` (convType 0) | P.cs:7768-7826 |
| 11 | Line discount | if `col_Ldisc_perc` non-empty (and the user is not editing the value column): `LDiscAmt = Round(price×LDiscPerc/100, DecimalPoint, AwayFromZero)`; else if `col_Ldisc_value`: `LDiscAmt = value`, `LDiscPerc = Round(LDiscAmt×100/price, DecimalPoint)` (0 if price 0); else both 0. `convPrice` is reduced by the equivalent amount | P.cs:7829-7864 |
| 12 | Discountable base | `NetPrice = price − LDiscAmt`; `col_discountable_amt = NetPrice × qty` (rounded to `DecimalPoint` only when `!PriceIsSame`) | P.cs:7868-7873 |
| 13 | Invoice-level discount per line | `disc_price = Round(NetPrice × discPer/100, DecimalPointActual)` → `col_disc`; `convPrice` reduced by the same ratio; `col_conv_rate = convPrice` (6 dp) | P.cs:7876-7884 |
| 14 | **Amount** | `amount = (NetPrice − disc_price) × qty`, no explicit rounding; stored in the cell as text formatted with `DecString` | P.cs:7888-7893 |
| 15 | **Tax** | `chk_vat` off: `taxAmt = amount × taxPerc/100` (unrounded, text-formatted with `DecString`). `chk_vat` on: `TaxAmount = 0` (tax is computed at invoice level in `AddInfoCalculation` / `TaxSummary`) | P.cs:7894-7908 |
| 16 | **Total** | `Total = amount + taxAmt` | P.cs:7909-7910 |

Notes for the API:
- `qty` in step 7 is the **price-unit-converted quantity**. `Amount` uses it, while `PURT.QTY` stores the entered `Qty` and `PURT.CONVQTY` is built separately in `SavePurT` (P.cs:2679-2705).
- The grid stores `Amount`, `TaxAmount` and `Total` as formatted strings, so effective rounding to `DecimalPoint` happens when the cell text is formatted and parsed back in `SubTotal` / `SavePurD`. That .NET `decimal.ToString("#,0.00")` rounds half away from zero is [ASSUMPTION] (not verified in this repo); use `MidpointRounding.AwayFromZero` as the other legacy code does explicitly.
- Invoice discount is applied per line (step 13) and also reported at invoice level: `AddInfoCalculation` row 1 = `Discountable × pct/100` is informational (`PURM.DISCVAL`), and `NetCalculation` does not subtract it again (P.cs:7304, 7387). This resolves the round-1 ambiguity.
- `CalculateAdditionalCost()` (P.cs:18165-18233), run before validation: when `addInfo_additionalCost > 0`, each line's `AddCost = (Round(Qty×Price, DecimalPoint) / TotalAmnt) × additionalCost / Qty` (uses gross Qty×Price, not net of discount), written to `col_addCost` and `col_convAddCost`; `convAddCost` is converted by `ConvFact`/`convType` for non-main units and divided by alias qty. With no additional cost, both columns are set to 0.
- `AddInfoCalculation(true)` (P.cs:6316): when the customs-duty percentage is blank, the percentage is derived from the value, `cd% = cdValue × 100 / (total + freight)` (not rounded) [P.cs:7321-7323].
- `SubTotal`, `AddInfoCalculation`, `NetCalculation` and `TaxSummary` are in claims 30-33 above.

## 12.2 Currency conversion and rounding

`CurrConvDB` [Model\CurrConvDB.cs]:

| Function | Rule | Rounding | Lines |
|---|---|---|---|
| `ToBase(x, CurrCode, rate)` | load currency by `CurrCode`; if `BASEFLG == false`: `CONVMODE == 1` → `x × rate`, else `x / rate` (only if `rate ≠ 0`); base or unknown currency → `x` unchanged | `Round(DecimalPoint, AwayFromZero)` | 114-137 |
| `ToBaseActual` | same conversion | `Round(DecimalPointActual = 6, AwayFromZero)` | 163-186 |
| `ToFourBase` | same conversion | `Round(FourDecimalPoint = 4, AwayFromZero)` | 211-234 |

Settings [VERIFIED]: `DecimalPoint` = `CONFIGTB.DECPOINTS` (default 2) with `DecString` = `"#,0." + zeros` [Home.cs:5608-5621]; `QtyDecimalPoint` = `CONFIGTB.QTYDECPOINTS` (default 2) [Variables.cs:126; Home.cs:5074-5083]; `DecimalPointActual = 6` and `FourDecimalPoint = 4` are constants [Variables.cs:151-155]; `INVAMTEDIT_PI` = `CONFIGTB.INVAMTEDITPI` [Home.cs:5122]. Base currency: `CurrMasDB.CurrencyByBaseFlag` [Home.cs:4830-4834].

Storage precision [VERIFIED: QuickBalance.sql]: PURM money columns are `decimal(18,4)`; PURT `RATE`, `PRRATE`, `DISC`, `ORGRATE`, `CONVRATE`, `ADDCOST`, `CONVADDCOST` and STKDT `RATE`, `CONVRATE`, `ADDCOST`, `CONVADDCOST` are `decimal(18,6)`; PURT `QTY`, `CONVQTY`, `CONVFACT`, `LDISCPERC`, `LDISCAMT`, `DISCP`, `MRP`, `TAX` and PURD `AMOUNT`, `TAX`, `TAXAMT` are `decimal(18,4)`. A 6-dp value written to a 4-dp column is rounded by SQL Server.

## 12.3 Configuration values the Save depends on (source in the company DB)

| Setting | Source | Evidence |
|---|---|---|
| `CashCD`, `VATCode`, `DiscCD` | `FYDETAILS` (`CASHCD`, `VATCD`, `DISCCD`) via `FyDetailsDB.CurrentFinYear()` | Home.cs:4843-4849 |
| `VatFlg`, `Country` | company master row (`VATFLG`, `COUNTRY`) | Home.cs:4796, 4804 |
| `batch` | `CONFIGTB.BATCHWISE` | Home.cs:5488-5491 |
| `SlNoTrack` | `CONFIGTB.SLNOTRACK` | Home.cs:5792 |
| `CostC`, multi cost centre | `CONFIGTB.COSTC`, `MULTCOSTC` | Home.cs:4901-4908 |
| `mult_alias` | `CONFIGTB.MULTIALIAS` (line 5622); **an earlier assignment sets it from `!BATCHWISE`** (Home.cs:4897-4900), so the two loaders conflict | Home.cs:4897-4900, 5622 |
| `INVAMTEDIT_PI`, `DecimalPoint`, `QtyDecimalPoint` | `CONFIGTB.INVAMTEDITPI`, `DECPOINTS`, `QTYDECPOINTS` | Home.cs:5122, 5609, 5074 |
| `DateBlock`, `BLOCKDT`, `LASTVRDATE`, `DEFDATE` | `CONFIGTB` | Home.cs:4924-4956 |
| `MVRSYS` | `CONFIGTB.MVRSYS`, only when `ProjectName == "Quick Balance"` | Home.cs:4958-4973 |
| `EinvoiceMode` | `CONFIGTB.EINVMODE`, default "Direct" | Configuration.cs:2011 |
| Financial year dates `fyStartDate` / `fyEndDate` | `FYDETAILS` (loader NOT located) | used at P.cs:4321, 15461 |

Loader NOT located / NOT READ: `VRSYS`, `BranchYN`, `LINEMERGEPI`, `GenNote`, `ApprovalSystem`, `InventoryApproval`, `UserType`, `UP_*` user rights. CLAUDE.md says the API's legacy-port style has `CommonFunction.SetVariableValues(Variables)` which loads "FY dates, decimals, CashCD, VRSYS"; I did not read it, so whether it can be reused is [ASSUMPTION].

## 12.4 What the grid derives per item (server-side lookups the API must replace)

From `SearchProduct` (P.cs:15263-15345) and `Calculation`:
- `UNIT`, `ALTUNIT`, `UNITCONV`, `CONVTYPE`, `PURPRICETYPE` from `PRMAST` (`PrMastDB.DetailedByCode`, query NOT READ; `UnitConDetailsByCode` = `SELECT UNIT,ALTUNIT,UNITCONV,CONVTYPE FROM PRMAST WHERE PRCODE` [PrMastDB.cs:987]).
- Default `Price` = `BASEPURPRICE` and unit = main (`PURPRICETYPE != 1`), or `ALTPURPRICE` and unit = alt (`PURPRICETYPE == 1`) [P.cs:15294-15311].
- Tax: `TAXCODE == 0` → `Tax = 0`, `col_taxCode = 0`; else `Tax = TAXPERC`, `col_taxCode = TAXCODE` from the product's tax master. For formats 6, 7, 8 with `ExemptedTaxCD > 0 && ExportIsChecked`: `Tax = 0`, `col_taxCode = ExemptedTaxCD` [P.cs:15316-15333; same logic on format change `ManageExpemtedTax`, P.cs:9862-9904].
- **Tax account `col_tax_acc`** (feeds PURD `ETYPE='D'`): `PrMastDB.PurAcc(PrCode)` or the voucher-system override `VRSYS_SLPRAcc` when `VRSYS_TagAcc` [P.cs:15280]. SQL [PrMastDB.cs:18825-18833]:
  - `Variables.TagAccMstr` on: `PRMAST.PURACCODE <> 0` → that; else `PRGRPMAS.TAGACCFLG='TRUE'` → `PRGRPMAS.PURACCID`; else `TAXMAS.PURCODE`.
  - off: `PRGRPMAS.TAGACCFLG='TRUE' AND PURACCID <> 0` → `PRGRPMAS.PURACCID`; `TAGACCFLG='FALSE'` → `TAXMAS.PURCODE`.
  - The result is `NULL` (→ 0 through `ReturnIntValue`, [ASSUMPTION]) when neither branch matches (`TAGACCFLG='TRUE'` with `PURACCID = 0`); in that case the `ETYPE='D'` rows get no valid account.
- Godown: `Functions.GetGodownByBranch()` returns the user's branch godown only when `VRSYS && BranchYN` (`BranchMasterDB.GetUserBranch()` / `SearchByID` → `GODOWN`, fallback 1); **otherwise always 1** [Functions.cs:1404-1429].
- `PRDESC` = `PRMAST.DESCRIPTION`; alias name = `PRMAST.ALIAS`; alias code/qty are cleared on product select and chosen separately [P.cs:15335-15337].
- `col_price_flg` is auto-set in `Calculation` (step 2 above).
- Exception reason: `ShowReason` returns immediately (`"Curently no need"`) [P.cs:17123], so for a new purchase `ExportReason = 0` (P.cs:4566) and `col_vatExceptionReason` stays empty unless loaded from an existing row (P.cs:5312, 5315). `PURT.VATREASONCD` is therefore 0 in practice.

Design consequence: the API should not trust client-supplied `TaxCode`, `TaxPercentage`, `ConversionType`, `ConversionFactor`, godown or tax account without validating them against `PRMAST` / `TAXMAS`; legacy takes them from the product master.

## 12.5 Additional writes and side effects (previously NOT READ)

| Item | Detail | Evidence |
|---|---|---|
| **PRMAST price update** (`UpdatePurRate`) | After every STKDT save: for each grid row, `BASEPURPRICE = Price − (col_Ldisc_value + col_disc)` when the price applies to the main unit (`PurPriceType 0` with flag N/empty, or type 1 with flag Y, or type 2 with unit = main), else `ALTPURPRICE = Price − discounts`; also calls `Branch_PriceList_Update` (body NOT READ). So a purchase **changes the item master purchase price**. Possible `NullReferenceException` for `PurPriceType 1` with an empty price flag (caught; the result is ignored) | P.cs:5841-5895; PrMastDB.cs:1105-1131 |
| **LOGNOTE** | New: `INSERT INTO LOGNOTE (VRTYPE,VRNO,EDATE,USERCODE,NOTES,CFLAG,VSVRTYPE)` with `('P', VrNo, now, UserId, 'Create ', true, VSVRTYPE)`. Edit: a text diff of supplier, bill date, purchase date, payment, cost centre, taxable, freight, discount, round-off between the "old" snapshot (taken when Edit is clicked) and the saved values (rest of the diff text NOT READ) | LogNoteDB.cs:16-28; P.cs:15629-15678 |
| **CONFIGTB.LASTVRDATE** | `UPDATE CONFIGTB SET LASTVRDATE=@date` (no `WHERE`), new voucher only; also refreshes `Variables.LastVrDate` | ConfigDB.cs:419-436; P.cs:6470-6474 |
| **YVR** (part-payment voucher) | `SavePayment` writes **two rows** with `VRTYPE="PV"`, `VRNO = YvrNo` (from the `PV` counter), `YVRTYPE="P"`, description `"Against Purchase Invoice:<OrdNo>"`, `CASHFLG="T"`: (1) `CODE = Variables.CashCD`, `RECEIPT = ToBase(cash_paid)`, `CDTYPE="C"`, `ADJCD = supplier`; (2) `CODE = supplier`, `PAYMENT = ToBase(cash_paid)`, `CDTYPE="D"`, `ADJCD = CashCD`. `APRVLST = "H"` when approval is pending and not value-approved, else "". Insert columns: VRNO, VRDATE, CODE, SLCODE, CHQNO, RECEIPT, PAYMENT, DESCRIPTION, VRTYPE, STYPE, PARTY, BANK, LOGCD, CANCELLED, DIRECT, UTIME, REFNO, CUSCODE, CHQDATE, ORDNO, NARRATION, PDCTYPE, INSTCODE, CDTYPE, CASHFLG, CURCONRATE, CURCODE, ORGREC, ORGPAY, CANCELFLG, ADJCD, VATNO, YVRTYPE, INVNOTEXT, VATDATE, PURCODE, VSVRTYPE, VSDOCNO, VSORDCD, PDCDT, SMCD, ADJFLG, ADJAMT, CTIME, ULOGCD, approval columns, ADJSLNO. `Cancel` = `UPDATE YVR SET ORGREC=RECEIPT, ORGPAY=PAYMENT, RECEIPT=0, PAYMENT=0, CANCELFLG=1 WHERE VRNO AND VRTYPE` | P.cs:3114-3176; YvrDB.cs:27, 36-74 |
| **BILLBYBILL** (supplier bill adjustment) | `SaveAdjustment` inserts `METHOD="Adjustment"`, `REFNO = OrdNo`, `ACCID = supplier`, `CREDIT = ToBase(pay_amt)`, `DEBIT = ToBase(cash_paid)`, `CDTYPE="D"`, `DUEDATE`, `CLOSED=false`, `VRTYPE="Purchase"`, `ORDERNO=0`, `STYPE="Payment"`, `TYPECODE = payment VRNO`, `GENTYPE="P"`, `VRDATE = bill date`, `VSVRTYPE`. In credit mode (payment 2) the modal `BillAdjustment` dialog (NOT READ) creates the `GENTYPE="PB"` rows after `BillDB.DeleteByRefNoAndVrType`. Insert column list at BillByBillDB.cs:20 | P.cs:3187-3216, 6436-6463; BillByBillDB.cs:16-28 |
| **PRST** (batch stock) | `SavePrSt` is an **upsert**: `PrstDB.updateByRack(...)`, and if no row is updated, `PrstDB.insert`. `BALQTY = Qty + (AltQty × ConvFact | ÷ ConvFact)`; `Disc = 0`, `Tax = 0`, `TaxCode = 0`. Insert columns: STYPE, VRNO, PURDATE, BILLNO, BILLDATE, SUPCODE, PRCODE, QTY, BATCH, EXPIRY, PRATE, RATE, DISC, TAX, TAXCODE, BALQTY, EXPCHECK, LOGCD, MRP, MFGDATE, ALTQTY, GRACK, CONVRATE, BRCD, CONVPRATE, CONVMRP. A failed insert deletes the voucher's PRST rows | P.cs:1824-1995; PrstDB.cs:16-31 |
| **BATCHDET** | Insert columns: STYPE, VRNO, PURDATE, BILLNO, BILLDATE, SUPCODE, PRCODE, QTY, BATCH, EXPIRY, PRATE, RATE, DISC, TAX, TAXCODE, EXPCHECK, LOGCD, MRP, MFGDATE, ALTQTY, GRACK, CONVRATE, STKFLG, CONVPRATE, CONVMRP, CONVQTY, VSVRTYPE | BatchDetDB.cs:16-28 |
| **Edit reversal of batch stock** | `DeductStock("Cancel")` (only when `ApprovalStockUpdate`): for each old `PURT` row it runs `PrMastDB.UpdateStock` (`PRMAST.STOCK = STOCK − CONVQTY`), and for batch rows `PrstDB.updateByRack` or `deleteByRack`. `DeductStock("ValidateUpdate")` blocks the edit when batch balance would go negative. **Nothing in the Save path increases `PRMAST.STOCK`** (the `UpdateStock` call in `SavePurT` is commented out, P.cs:2822-2860), so `PRMAST.STOCK` appears not to be maintained on purchase (derived from STKDT?) while cancel still decrements it: [ASSUMPTION], needs a decision | P.cs:961-1063; PrMastDB.cs:949-961 |
| **STKDT `UTIME`** | `GetUTime` = `SELECT UTIME FROM STKDT WHERE TRNNO=@ and TYPE='R' and VRTYPE='TR'` (for `"NT"`: by `NTNO`); the first row's time is reused on update | StkDtDB.cs:95-104; P.cs:1294-1298 |
| **Approval tables** | `SaveDetails` / `UpdateDetails` update `PURM.APRVLST` (for "Purchase Invoice") and write the approval record (`UpdateDetails` sets `PURM.APRVLST='P'` by `VRNO` [ApprovalModuleDB.cs:6031-6032]; `SaveDetails` body NOT READ) | ApprovalModuleDB.cs |
| **Confirm / success texts** | `"Do you want to Save ?"` / `"Do you want to Update ?"` (title `"Confirmation"`, only when `Variables.SaveFlg` is true; otherwise auto-Yes). `"Data Saved Successfully.Do You Wish To Print ?"` (if `PrintFlg && PrintConfirm`) or `"Data Saved Successfully"` | CommonDB.cs:1152-1161, 1183; 1197-1209 |
| **Unused `*PHY` tables** | `PURTPHY` exists in the schema and `PurTDB` / `PurmDB` have `phy_*` members, but the Purchase form never calls them in Save | QuickBalance.sql:3876; grep of P.cs |

## 12.6 Validation clarifications

- **Financial-year / period lock:** `CommonDB.ExpiryCheck` only acts when `!Variables.Active` (unlicensed/demo): `"You are Using Demo and can store transactions between <FYstart> and <FYstart+1 month> Only."` for dates beyond one month, and `"Can't Enter Transactions With Date Greater Than Financial Year Ending Date."` for dates before the FY start (the text does not match the condition) [CommonDB.cs:329-376]. For a licensed install it returns false, so **the Save path has no explicit financial-year range check**; the form relies on the date picker (`MinDate`/`MaxDate`, referenced at P.cs:4824-4828; the code that sets them was not located). The API must enforce `PURDATE` within the `FYDETAILS` start/end explicitly: decision for the team.
- **Date block:** `CommonDB.CheckDate` (CommonDB.cs:1424-1473): with `Variables.DateBlock`: if `BLOCKDT` is set, dates `<= BLOCKDT` are rejected (SuperUser only when `SUDateBlock`; other users always); if not set, dates `< Variables.LastVrDate` are rejected (`LastVrDate` from `CONFIGTB.LASTVRDATE`). Messages as in section 4b row 28. `UserType == "SuperUser"` and `SUDateBlock` loaders NOT READ.
- **Order-quantity check** `CheckOrdQty` (P.cs:17634): only for rows linked to a PO (`OrderNo > 0`, no GRN, `col_ordRowID > 0`, not previous-year) or a GRN (`col_grn_no > 0`, `col_ntRowID > 0`); remainder of the body NOT READ.
- No user-rights check on Save found; `Variables.UP_*` rights only gate master creation shortcuts (P.cs:19011-19075). Enablement of the Save button by user permission is decided at form load (NOT READ).
- `Variables.SaveFlg == false` removes the "Do you want to Save?" prompt only; it is not a permission.

## 12.7 PURM columns that `SavePurM` never sets, and schema facts

PURM columns **not assigned** in `SavePurM` (written with the `clsPurM` field default; the integer, bool and decimal fields default to 0 / false; string getters were checked only for `BillNo`, so empty-string defaults for the others are [ASSUMPTION]): `BILLREFFLG`, `TRTYPE`, `TRVRNO`, `TRBRCD`, `TRGDWNCD`, `STATUS`, `DRACC`, `CRACC`, `ISMNBILL`, `MNBILLNO`, `PHYWISE`, `PHYMODE`, `PHYCATCD`, `RETTRANSFER`, `PIF1..PIF20` (set only by the reflection loop over `piAddFields_F1..F20`, P.cs:3060-3070), `VSVRTYPE/VSDOCNO/VSORDCD` (only when `VRSYS`). `TRANSFERRED = "N"`, `BAYANNO`, `ADDITIONALCOST`, `ADDCOSTACCD`, `APRVLST` are set [P.cs:3054-3058]. `clsPurM.BillNo` doubles single quotes in its getter (clsPurM.cs:17-25).

Schema [VERIFIED: QuickBalance.sql:3700-3793]: `PURM.VRNO` is the `IDENTITY` primary key; `RETURNFLG` is the only `NOT NULL` column besides `VRNO`; the table also has `SYC_VRNO`, `SYC_ORDNO`, `SYC_YVRNO`, `BNKDATE`, `APRVLABY/ADT/BBY/BDT/JBY/JDT` that the legacy insert does not write. `PURT.CODE` is `IDENTITY` (PK) and `RETURNFLG` is `NOT NULL`. `PURD` has no primary key and no identity. `STKDT.VRTYPE` is `NOT NULL`. There are no triggers.

## 12.8 New findings and corrections (round 2)

1. `UpdateVoucherSystemOrdNo("PI")` and `("GRN")` are **no-ops**: the method acts only on `"SI"` (when `IsNewNbrGnrtd`), `"GDN"` (when `IsNewNbrGnrtdRD`) or `"PV"`, but Save passes `"PI"`, `"GRN"`, `"PV"` (P.cs:15891-15907 vs 6489-6491; looks copied from Sales). `VoucherSystemDB.Manage(this,true)` in `SavePurM` (P.cs:3089) may do the work; NOT READ.
2. Duplicate-bill check on edit compares the wrong identifiers (confirmed, section 10 item 2).
3. `PURM.CREATEDDT` is the form-open time (claim 7).
4. `UpdatePurRate` rewrites the item master's purchase price on every purchase and its result is ignored.
5. `PRMAST.STOCK` is decremented on edit-cancel but never incremented on save (section 12.5).
6. `mult_alias` is assigned by two conflicting loaders (section 12.3).
7. `CalculateAdditionalCost` allocates on gross `Qty × Price`, not net of discount (section 12.1).
8. `ExpiryCheck` messages and conditions are mismatched for the demo path, and there is no explicit FY range check for licensed installs (section 12.6).
9. `ShowReason` is disabled, so `PURT.VATREASONCD` is 0 for new purchases (section 12.4).
10. Round-1 item 3 (`GRNNO` read before assignment) is **not a bug**; withdrawn.

## 12.9 Current NOT READ list (after round 2)

Still unread; none is required for the core PURM / PURT / PURD / STKDT write path unless the feature is enabled:
- **GRN tracking** (`chk_trck`): `SaveGrnT` (P.cs:14602), `SaveGrnD` (14932), `GrnMDB.insert/update`, `GrnTDB`, `GrnDDB`.
- **Batch / serial persistence detail:** `SaveBatchDet` after line 2063; `SaveSntStk` after line 2226; `SntStkDB.insert`; `PrstDB.updateByRack`, `deleteByRack`, `GetAvailableStock`; `BatchDetDB.GetBatchForValidation`; `ValidateBatchGotoNegetive`.
- **Voucher system / branch:** `VoucherSystemDB.Manage`, `GetSubDocNo`, `UpdateOrdNo`, `UpdateOrdNoChekByTable`; `BranchMasterDB`; loaders of `VRSYS`, `BranchYN`.
- **E-invoice and documents:** `SaveEinv` (17077), `SaveDoc` (18541), `ManageEinvoiceXML_API/_DIRECT`, `ValidateEinvoice` (16296).
- **Line merge:** `Consolidate` (5904), `LM` (17503), `dtLM` structure, `ManagePendigDetails`, `CheckOrdQty` remainder, `StkDtDB.GetForLM`.
- **Payments:** the `BillAdjustment` dialog, `YvrDB.Delete` remainder, `BillDB.DeleteBy*` bodies, `VrInvNoText("Payment")` (P.cs:15505+), `CrrPrint`.
- **Approval:** `ApprovalModuleDB.RefreshAprvl`, `ManageAprvl`, the `SaveDetails` body for "Purchase Invoice", `Variables.ApprovalSystem` / `InventoryApproval` loaders.
- **Other:** `Branch_PriceList_Update`, `PrMastDB.DetailedByCode` query, `TaxMasDB.SearchByID`, `Functions.LIST_ALLOW_VR_STMNT`, `UserType` / `SUDateBlock` / `UP_*` loaders, date-picker `MinDate/MaxDate` setters, `fyStartDate/fyEndDate` loader, `CommonFunction.SetVariableValues` in the API repo.
- **Edit-note diff text** in `LogNote` after P.cs:15678; remainder of `Clear()` / `New()` (P.cs:4412+).
- `ManagePurchase_Purt` body (QuickBalance.sql:7673), dead path, low priority.
- Column types for the GRN, PRST, BATCHDET, SNTSTK, YVR, BILLBYBILL and LOGNOTE tables (only PURM, PURT, PURD and STKDT were compared).

## 12.10 Updated readiness assessment

| Area | Round 1 | Now |
|---|---|---|
| Write order, tables, number generation, validation messages | Specified | Specified |
| Item calculation (`Calculation`), rounding, currency conversion | Missing | **Specified** (12.1-12.2) |
| Configuration sources | Missing | **Specified** for the needed values (12.3); `VRSYS`/`BranchYN` loaders still open |
| Server-side item lookups (tax, tax account, godown, price, units) | Missing | **Specified** (12.4) |
| PURM unset columns, schema precision | Missing | **Specified** (12.7) |
| `InvDateTime`, `GRNNO`, STKDT vs STKDTPHY | Open | **Resolved** |
| Payment, bill-by-bill, PRMAST price update, LOGNOTE, CONFIGTB writes | Not mentioned | **Documented** (12.5); scope decisions still needed |
| GRN, batch/serial, line-merge, e-invoice, approval detail | NOT READ | Still NOT READ (12.9) |

**Remaining decisions for the team** (unchanged in nature): scope of GRN tracking, batch/serial and part-payment; whether `UpdatePurRate` (item master price) and `LOGNOTE` are in scope; how to enforce the financial-year range (legacy has none for licensed installs); how to produce `INVNOTEXT` / `PURVRNO` safely under concurrency; whether to keep legacy quirks (`PURT.TAX` discount fallback, per-line invoice discount, gross-based additional cost).
