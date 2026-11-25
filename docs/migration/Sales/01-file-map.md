# Sales module – Phase 1 file map

Legacy root: `D:\PROJECT\IFlare\Inventory\Inventory` (paths below are relative to it).
DB script: `D:\PROJECT\Salman\Latest\QuickBalance\DB\ProjectDB\QuickBalance.sql` (shown as `QuickBalance.sql`).

> **Line ranges.** Start lines come from grep. End lines are "up to the next member", so they're approximate. Physical line counts: `Sales.cs` 26,552, `SalesReturn.cs` 15,548, `SalesOrder.cs` 11,615 (rechecked 2026-11-23).

## 0. Scope used for this map

Scope decisions are in `00-scope.md`. This map follows them.

| In scope (mapped in depth) | Out of scope (listed, not mapped) |
|---|---|
| **Sales Invoice** `View/Sales.cs` (§2), **Sales Return** `View/SalesReturn.cs` (§4), **Sales Order** `View/SalesOrder.cs` (§5), plus the shared code they call (§3) | POS (`View/POS Module/POS.cs`), Goods Delivery Note form, Quotation paths of `SalesOrder.cs` (D2), copy/paste voucher (D5), Crystal/RDLC layouts under `Print/CRR*` (only their data contract is kept, §9), sales reports under `View/Report/*Sales*`, `bin/` and `obj/` copies |

Sales Invoice optionally creates a **Goods Delivery Note** (`GDNM/GDNT/GDND`) in the same save when "Track" (`chk_trck`) is checked. It can also pull lines from pending Sales Orders, Delivery Notes and Quotations. Those paths are included because they're part of the invoice save.

---

## 1. Entry points

| File | Method | Line | Purpose |
|---|---|---|---|
| `View/Home.cs` | `TrBillingAdd_Click` | 3144–3154 | Menu → new Sales Invoice |
| `View/Home.cs` | `TrBillingModify_Click` | 3156–3202 | Menu → list (`View.Data`, `SalMDB.List`, `STYPE='O'`) → open with `DisplayType="M"`, `InvNo=<id>` |
| `View/HomeUI.cs` | (new UI launcher) | 2114, 5040–5092 | Same as above for the newer home screen |
| `View/Data.cs` | generic list/search dialog | 1–1162 | Paged list with search fields `SALM.<VSDOCNO>`, date, customer, TVALUE/TAX/AMOUNT |
| Many reports (`DayBook`, `Ledger`, `BillsReceivable`, `SalesReport`, …) | drill-down | e.g. `View/Report/SalesReport.cs:474` | Open an invoice in view mode |

---

## 2. Sales Invoice form – `View/Sales.cs`

### 2.1 Load, lookups, form state

| Method / handler | Lines | Purpose | DB calls |
|---|---|---|---|
| ctor + field declarations | 25–1012 | UI sizing; instantiates ~45 `Model.*DB` objects (793–909) | – |
| `GetExemptedTaxCD` | 1013–1017 | Exempt tax code | `TaxMasDB.GetIDExemptedTax` |
| `FillItem` | 1018–1052 | Product picker source | `PrMastDB.ProductNameAndCode / ProductsBarCodeAndAlias / AliasAndPrCode` |
| `FillCustomer` / `FillSalesman` / `FillCostCenter` | 1053–1088 | Combos | `AccMasDB.AccountsForSales`, `SmMastDB.ActiveSalesman`, `CostCMasDB.CostCenters` |
| `Combo` | 1089–1150 | Format, pay mode, cash/bank accounts, price list, price category | `InvFormatDB.ActiveFormats`, `PayModeDB.ActiveModes`, `AccMasDB.AccountsByGroup`, `PriceListMDB.List`, `PriceCatMasDB.Categories` |
| `Sales_Load` | 6387–6665 | Loads all lookups in one `DataSet`, approval config, next voucher no., opens in modify mode when `InvNo>0` | `db.GetDataSet` (+ `*.GetQuery` builders), `FyDetailDB.InvOptions`, `ApprovalModuleDB.GetAprvl`, `SalMDB.MAXORDNO/SearchByInvno`, `VoucherSystemDB.CheckVrUnderBranch/GetBranchCodeByTypeCode` |
| `Sales_Shown` | 17928–17991 | Approval button state | `ApprovalModuleDB.ManageAprvl` |
| `Clear` | 6153–6386 | Reset form, next `ORDNO` | `SalMDB.MAXORDNO`, `VoucherSystemDB.Manage` |
| `New` / `ManageNextOrPrevVr` / `btn_vr_left_Click` | 25950–26026 | Navigate prev/next voucher | `ApprovalModuleDB.ManageAprvl` |
| `MakeDup` / `btn_vr_paste_Click` | 26027–26525 | Copy/paste (duplicate) an invoice | `SalMDB/SalTDB.SearchByInvno`, `SalDDB.SearchAddInfo`, `PrMastDB.SalAcc` |
| `ProcessCmdKey` | 25717–25949 | Keyboard shortcuts (F-keys, Ctrl+S, …) | – |
| `ManageMasterAccess`, `MasterCreate/Edit`, `Add*` | 24837–24849, 25149–25716 | Inline "add customer/salesman/item" | lookups only |

### 2.2 Modify / view (loading an existing invoice)

| Method | Lines | Purpose | DB calls |
|---|---|---|---|
| `Fill` | 6666–7568 | Load header, lines and add-info for modify; balance-qty for linked SO/DN/QTN lines; serial numbers; e-invoice data | `SalMDB.SearchByInvno`, `SalTDB.SearchByInvno`, `SalDDB.SearchAddInfo/SlCodeForAccount`, `StkDtDB.GetForLM/BalOrderQtyByRowID/BalNtQtyByRowID`, `CordTDB.BalQtnQtyByRowID`, `SntStkDB.GetByVoucherOut`, `EInvDB.Get`, `SalMDB.GetAvailablePoints`, `ApprovalModuleDB.GetApprovalStockUpdateStatus` |
| `Fill2` | 7569–7686 | Linked GDN, batch details, stock rows | `GdnMDB.VrNoByOrdNo`, `BatchDetDB.SearchByVrNo`, `StkDtDB.SearchByVr` |
| `Fill3`, `SetDefaultBill`, `Taxes` | 7687–7797 | Defaults, tax list | `TaxMasDB.ActiveTaxes` |

### 2.3 Data entry: customer, lines, pricing

| Method / handler | Lines | Purpose | DB calls |
|---|---|---|---|
| `cmb_customer_SelectedValueChanged` | 8935–9214 | Address, price category, currency, pending GDNs, product template | `CusMasDB.GetAddress/GetCustomerByAccode`, `GdnMDB.SearchByCustomer`, `PRTMDB.getAll` |
| `cmb_customer_Leave` | 18595–18686 | Currency and credit info | `AccMasDB.SearchByID`, `CurrMasDB.SearchByCode` |
| `GetPartyBalance` | 9215–9234 | Customer ledger balance | `AccMasDB.LedgerBalance(Specific)` |
| `dgv_purchase_CellEndEdit` (**the main sales grid handler despite the name**) | 9727–11011 | Product, unit, qty, rate, discount, tax per cell; godown stock; batch picker; price category slabs | `PrMastDB.SearchByCode/DetailedByCode/UnitsForProduct/CheckBatched/CheckSlNoTracking`, `PriceCatLevDB.SearchByPrCodeAndRange(Catg)`, `TaxMasDB.SearchByID`, `GdwMasDB.ActiveGodownsWithStock` |
| `dgv_sales_CellValueChanged`, `CellBeginEdit`, `TextKeyUp`, `cmb_text_changed`, `dgv_sales_KeyPress` | 17764–17835, 18133–18196, 8009–8123, 18202–18234, 18334–18357 | Product search-as-you-type | `PrMastDB.ProductsBarCodeAndAlias*`, `SearchBarCodeAndAlias` |
| `SearchProduct` / `SearchMultiAlias` / `SetMultiALiasWithQty` | 20100–20578 | Resolve product by name, barcode or alias | `PrMastDB.SearchByNameOrBarcode/DetailedByCode/SalAcc` |
| `PriceCalculation` | 8746–8763 | Apply discount % to price | – |
| `cmb_pr_cat_SelectedValueChanged` | 18712–18797 | Re-price lines by price category | `PriceCatLevDB.*` |
| `cmb_ProductTemplate_SelectedValueChanged` | 19715–19910 | Insert lines from a product template | `PRTMDB.SearchByCode`, `PRTTDB.SearchByOrdNo` |
| `SetPriceFlag`, `CheckBatchPrice`, `CheckBatchDuplicated`, `CheckBatch`, `CalcPricefromBatch` | 21650–21817, 8764–8780, 9683–9706 | Batch-price rules | `PrMastDB.CheckBatched/DetailedByCode`, `PrstDB.ChechBatchByCode` |
| `PRateCalculation` | 20579–20890 | **Cost (purchase) rate** per item for profit/COGS (`SALT.PRRATE`) | `PurmDB.GetTrTypeByOrdno`, `StkDtDB.ReceiptsByPrcode/TransactionsByPrcode`, `OpnStkDB.SummaryByItem` |
| `ManageExpemtedTax`, `chk_vat_CheckedChanged` | 8327–8425, 11699–11743 | Exempt-tax and VAT on/off | `PrMastDB.DetailedByCode` |
| `SetCurrStock`, `ViewBranchWiseStockDetails` | 24179–24209, 22054–22069 | Stock display | – |

### 2.4 Calculations

| Method | Lines | Purpose |
|---|---|---|
| `Calc` | 11200–11232 | Unit-converted qty |
| `RefreshPrdDtls` | 11233–11251 | Refresh product variables for a row |
| `Calculation(ri)` | 11252–11679 | **Line maths**: qty × rate, line discount (type/%/amt), tax, line total |
| `TaxSummary` | 11012–11199 | Tax-wise summary grid (`dgv_vat_details`) |
| `SubTotal` | 18911–18974 | Sums the line grid |
| `AddInfoCalculation` | 18985–19034 | Additional charges/deductions grid (`dgv_add_info`) → SALD |
| `NetCalculation` | 18816–18891 | **Header totals**: discount %, freight, coin adjustment, net amount |
| `TotalAllignment`, `Balance` | 19035–19208, 21115–21133 | Display, balance due |
| `txt_pay_amt_TextChanged`, `txt_cash_paid_TextChanged` | 18892–18910 | Cash-paid and change |
| `Cal_Pr_Rate` | 5101–5127 | Convert cost rate to alt currency |
| `RefreshDetails` | 5075–5100 | Recalculate before save |
| `Consolidate` + `LM` | 4860–5074, 23662–23807 | Line-merge (`Variables.LINEMERGESI`): merge duplicate product lines before save |
| `calculatePoints`, `SetRedeem`, `btn_Redeem_Click`, `AddRedeemAccount` | 21858–22001 | Loyalty points earn/redeem |

### 2.5 Linking to Order / Delivery Note / Quotation (pending documents)

| Method | Lines | Purpose | DB calls |
|---|---|---|---|
| `PendingList` | 9235–9432 | Pending SO and DN for the customer (temp-table based) | `StkDtDB.CreateSTKTempTB/DropSTKTempTB/PendingOrders_NEW/PendingNotes_NEW/Create*_PRV_MULTI/Get*_PRV_MULTI` |
| `dgv_order_CellValueChanged` | 17109–17260 | Tick/untick a pending doc | `StkDtDB.CreateSTKTempTB/DropSTKTempTB` |
| `SearchOrder` | 15928–16744 | Pull lines from SO / DN | `CordMDB/CordTDB.SearchByOrdNo`, `GdnMDB/GdnTDB.SearchByOrdNo*`, `StkDtDB.BalOrderQty*/BalNtQty*` |
| `SearchDN_PRV` | 16781–16954 | Pull lines from previous-year DN | `GdnTDB.SearchByOrdNO_PRV`, `StkDtDB.BalNotesQty_PRV` |
| `SearchQuotation` / `btn_PendingVoucher_Click` | 24361–24836, 24210–24360 | Pull lines from a quotation | `CordMDB/CordTDB.*`, `StkDtDB.*Quotation_PRV_MULTI` |
| `txt_SO_KeyDown`, `ItemDetailsOrdBySO` | 22070–22166, 24080–24153 | Enter SO number directly | `CordMDB.SoByVrText`, `CordTDB.SearchByOrdNo` |
| `GetDeliveryNoteSerialNumberDetails` | 16745–16780 | Serial numbers from DN | `SntStkDB.GetDeliveryNoteDetails` |
| `GetAdditionalDetails` | 23148–23379 | Carry over add-info charges from SO/DN | `CordMDB/GdnMDB.SearchByOrdNOS_AdditionalInfo`, `CordDDB/GdnDDB.SearchAddInfo_ByOrdNOS` |
| `ManagePendigDetails`, `LMInvText`, `ManageLM`, `ManageQtnLM`, `Clear_LM`, `CheckOrdQty` | 21430–21565, 23808–24079, 24887–25020 | Line-merge against linked docs; over-qty checks | `VoucherSystemDB.GetVoucherByUserBranch/GetVSDocNoByOrdNo` |

### 2.6 Validation

| Method | Lines | Purpose | DB calls |
|---|---|---|---|
| `Functions.HB_ValidateSaving_Transactions` | `Functions.cs` 2160–2219 | Permission/lock check before any save | – |
| `Functions.FYValidation` | `Functions.cs` 2831– | Date inside financial year | – |
| `Validation` | 1466–2203 | **Main validation**: duplicate invoice text, date ≥ last date, customer, rows, stock by godown, batch stock, unit conversion, serial numbers, MFG/EXP, credit, … | `SalMDB.CheckInvNoTextExists/MaxDate`, `PrMastDB.CheckStockByGodown/CheckBatched/CheckSlNoTracking/Get_MFG_EXP/UnitConDetailsByCode`, `PrstDB.GetAvailableStock`, `BatchDetDB.GetStockByVoucher`, `SntStkDB.CheckUsedByProduct`, `CurrConvDB.ToBaseActual` |
| `ValidateTaggedVoucher` | 2204–2857 | Linked SO, DN and QTN qty not exceeded (added 18-02-2026) | `StkDtDB.Bal*`, `CordTDB.BalQtnQty*`, `StkDtDB.*_PRV_MULTI` |
| `BlockOutSideProduct` | 23617–23636 | Block products from another branch | – |
| `ValidateEinvoice` | 22214–22319 | E-invoice mandatory fields | `CompMasDB.CompanyInfo`, `BranchMasterDB.SearchByID`, `CusMasDB.SearchByAccID` |

### 2.7 Save / update (`btn_save_Click` orchestration)

| Method | Lines | Purpose | DB calls / tables written |
|---|---|---|---|
| **`btn_save_Click`** | 5166–6152 | Edit → Update mode switch (5180–5264); validation, approval, confirm (5267–5306); e-invoice gate (5308); then the write chain below. Branch with GDN (`chk_trck`) is 5338–5650; without GDN is ~5651–6020 | see chain |
| `SaveSalM` | 3178–3550 | Header insert/update; voucher numbering; points | `SalMDB.insert/update/MAXORDNO`, `FyDetailsDB.ValidateVoucherNo`, `VoucherSystemDB.GetSubDocNo/Manage`, `YvrDB.MaxVrNo`, `GdnMDB.MAXOrdNo` → **SALM** |
| (update only) delete lines | 5316–5323 | Delete then re-insert | `SalTDB.DeleteByVrNo` → SALT, `SalDDB.DeletByVrNo` → SALD |
| `SaveEinv` | 23060–23103 | E-invoice record | `EInvDB.insert` → **EINV** |
| `SaveDoc` | 25127–25148 | Attachments | `Functions.SaveDoc` (`Functions.cs` 3436) |
| `SaveSalT` | 3551–3859 | One row per grid line | `SalTDB.insert` → **SALT** |
| `SaveSalD` | 3860–4069 | Account/tax lines (sales account per item, tax, add-info charges) | `SalDDB.insert` → **SALD** |
| `SaveStkDt(OrdNo, GdnNo, "O")` | 2858–3177 | Stock ledger out-entries | `StkDtDB.DeleteByVr/DeleteByTrnNo/insert/GetUTime`, `phy_*` → **STKDT**, **STKDTPHY** |
| `SaveGdnM` / `SaveGdnT` / `SaveGdnD` (when `chk_trck`) | 19214–19347, 19348–19697, 19938–20084 | Auto Delivery Note | `GdnMDB.insert/update`, `GdnTDB.insert`, `GdnDDB.insert` → **GDNM/GDNT/GDND** |
| (update + GDN) cleanup | 5343–5360 | Delete GDN lines, batch details, revert stock | `GdnTDB.DeleteByVrNo`, `GdnDDB.DeletByVrNo`, `BatchDetDB.DeleteByVrNo`, `ClearStock` |
| `SaveStkDt(OrdNo, GdnOrdNo, "DN")` | 2858–3177 | Stock rows for the DN | **STKDT/STKDTPHY** |
| `SaveBatchDet` | 4258–4453 | If `Variables.batch` | `BatchDetDB.DeleteByVrNo/insert` → **BATCHDET** |
| `UpdateSntStk` | 4454–4525 | If `Variables.SlNoTrack` | `SntStkDB.*` → **SNTSTK/SNTSTKPHY** |
| `configDB.UpdateLasVrDate` | 5380–5384 | Last voucher date (new only) | **CONFIGTB** |
| `SaveReceipt` ×2 + `SaveAdjustment` (cash, bank, bank2) | 4070–4166, 4167–4213; called 5386–5560 | Part-payment receipt voucher (Dr cash/bank, Cr customer) and bill-by-bill adjustment | `YvrDB.Delete/insert/MaxVrNo`, `BillDB.DeletByTypeCodeAndStype/insert`, `FyDetailsDB.UpdateVoucherNo` → **YVR**, **BILLBYBILL** (and FYDETAILS counter) |
| `View.BillAdjustment` dialog | 5570–5594 | Manual bill-by-bill adjustment for credit sales | **BILLBYBILL / PENDBIL** (`View/BillAdjustment.cs`, 922 lines) |
| Approval save | 5597–5600 | `ApprovalModuleDB.UpdateDetails` (when `!ApprovalValueStatus && ApprovalStatus == "S"`) / `SaveDetails` (when `Approval` is not `""` or `"S"`) | **SALM**, **GDNM**, **YVR** approval columns. See the note below this table. `APRVLMODULETB` is **only read** (approval config) |
| `UpdateTransferFlg_PRV` | 4526–4626 | Mark previous-year SO/DN as transferred | `CordMDB/GdnMDB.FY_Update_Transfer_Status` → CORDM, GDNM |
| `LogNote("create"/"edit")` | 21147–21237 | Audit log | `LogNoteDB.Insert` → **LOGNOTE** |
| `CrrPrinting` | 13319–15913 | Print after save | read-only |
| `FyDetailsDB.UpdateVoucherNo`, `UpdateVoucherSystemOrdNo` | 21412–21429 | Voucher counters | **FYDETAILS**, **VRSYSTEM** |
| `UpdatePrst` / `UpdateStock` / `ClearStock` | 4214–4257, 4724–4859 | Rack and product stock adjust, revert on edit | `PrstDB.updateByRack/UpdateSalesPrice`, `PrMastDB.UpdateStock` → **PRST**, **PRMAST** |
| **`SaveSalt_Sql`** | 5128–5165 | Calls SP `ManageSalesBilling_Salt`. **Dead code**: only referenced from commented-out lines 5335 and 5371 | – |

> **Approval writes (added 2026-11-23 after the Phase 1 sign-off).** Neither method writes `APRVLMODULETB`. That table holds approval configuration and is read by `GetAprvl` (`ApprovalModuleDB.cs:49`). The writes go to the voucher and the vouchers generated with it:
>
> | Method | Writes for `"Sales Invoice"` | Writes for `"Sales Return"` |
> |---|---|---|
> | `UpdateDetails` (`ApprovalModuleDB.cs:6012`, only if `ApprovalLevel > 0`) | `SALM.APRVLST='P'` (6043). Linked vouchers set to **`'H'`**: `GDNM` (`STYPE='DN'`, `ORDNO=SALM.GDNNO`), and `YVR` RV for `SALM.YVRNO`, `BANKYVRNO`, `BANKYVRNOT` (6069–6108) | `SALM.APRVLST='P'`. `YVR` PV for `SALM.YVRNO` set to `'H'` (6109–6125) |
> | `SaveDetails` (`ApprovalModuleDB.cs:299`) | `SALM.APRVLABY/APRVLADT` and/or `APRVLBBY/APRVLBDT` for the approving level (306, 349). When fully approved (`status='A'`, 304): `APRVLST='A'`, and `APRVLST=''` on the same linked `GDNM` and the three `YVR` rows (382–421) | Same `SALM` columns. When fully approved, `YVR` PV for `YVRNO` set to `APRVLST=''` (422–438) |
>
> For Sales Order, the same methods write `CORDM` (`STYPE='O'`) approval columns (345, 6039). There's no generated-voucher block, so the advance `YVR` keeps its approval status. Phase 2 confirms this. **D9 impact:** the API save SPs must set these `APRVLST` values on SALM and on the auto-DN and receipt vouchers they create. A linked voucher stuck at `'H'` or `'P'` behaves differently in the desktop app.

### 2.8 Cancel

| Method | Lines | Purpose | DB calls |
|---|---|---|---|
| `btn_cancel_Click` | 17588–17735 | Approval re-check, confirm, then soft-cancel: zero the qty/rate and set `CANCELFLG` | `SalMDB.CancelBill/SearchByTypeAndRtnInv/UpdateBillRefFlgAndcashPaidByOrdNoAndType`, `SalTDB.Cancel`, `SalDDB.Cancel`, `GdnMDB/GdnTDB/GdnDDB.Cancel`, `StkDtDB.DeleteByVr/phy_DeleteByVr`, `BatchDetDB.DeleteByVrNo`, `SntStkDB.ClearAdjustmentAllByVoucher/Phy_DeleteByVoucher`, `YvrDB.Cancel`, `BillDB.GetByRefNoAndVrType/DeleteByRefNoAndVrType`, `LogNote("cancel")` |

### 2.9 E-invoice (ZATCA-style), print, misc

| Method | Lines | Purpose |
|---|---|---|
| `btn_Einvoice_Click`, `AutoGenerateEinvoice` | 22199–22213, 24159–24178 | Manual and auto e-invoice |
| `ManageEinvoiceXML_API` / `ManageEinvoiceXML_DIRECT` | 22320–22582, 22583–22964 | Build XML and submit (API or direct hash chain); `EInvDB.Get_PrevInvHash`, `VetExceptionResonAll` |
| `GenerateEinvoiceQr`, `GenerateEmbededXmlinPdf` | 22965–23059 | QR code and PDF/A-3 embed |
| `Printing` / `RdlcPrint` / `CrrPrinting` / `btn_print_Click` | 11766–12559, 12584–13318, 13319–15913, 15914–15927 | RDLC and Crystal printing (format chosen by `cmb_format` / `INVFORMAT`) |
| `btn_SendMail_Click` | 22014–22053 | Email invoice |
| `InvNoText` / `VrInvNoText` | 20891–21113 | Invoice number text from `VRNOCONF` series |
| `ManageVoucherSystem`, `cmb_VSDocType_*`, `Set_VRSYS_TagAcc` | 21341–21411, 21566–21628 | Branch voucher system (`VRSYS`) |
| `btn_DcUpload_Click` / `btn_DcView_Click` | 25036–25126 | Attachments |
| `btn_payment_Click`, `ListPayType`, `ShowPaymentType` | 24850–24886, 26526–end | Split cash/bank payment dialog |

---

## 3. Entity and DB classes (legacy)

| File | Class | Members used by Sales | Lines | Purpose |
|---|---|---|---|---|
| `Controller/clsSalM.cs` | `clsSalM` | all | 1–941 | SALM entity (≈95 columns, incl. `SIF1..SIF20`) |
| `Controller/clsSalT.cs` | `clsSalT` | all | 1–901 | SALT entity |
| `Controller/clsSalD.cs` | `clsSalD` | all | 1–293 | SALD entity |
| `Model/SalMDB.cs` | `SalMDB` | `insert` 18, `MAXORDERNO` 42, `DeleteBynvNo` 53, `update` 66, `UpdateGdnNo` 79, `SearchByInvno` 108, `MAXORDNO` 122, `List` 161, `CancelBill` 3152, `SearchByTypeAndRtnInv` 3165, `MaxDate` 3206, `CheckInvNoTextExists` 3485, `GetAvailablePoints` 3552, `UnMarkByGdnNo` 3665, `UpdateBillRefFlgAndcashPaidByOrdNoAndType` 3688 | 1–4010 | Inline SQL, string-concatenated; SQL and MS Access branches |
| `Model/SalTDB.cs` | `SalTDB` | `insert` 15, `Cancel` 28, `SearchByInvno` 55, `DeleteByVrNo` 69 | 1–1110 | |
| `Model/SalDDB.cs` | `SalDDB` | `insert` 17, `DeletByVrNo` 31, `SlCodeForAccount` 544, `SearchAddInfo` 558, `Cancel` 572 | 1–720 | |
| `Model/StkDtDB.cs` | `StkDtDB` | `insert` 12, `DeleteByVr` 44, `GetUTime` 95, `DeleteByTrnNo` 159, `PendingOrders_NEW` 548, `PendingNotes_NEW` 658, `BalOrderQtyByRowID` 977, `BalNtQtyByRowID` 1030, `UnmarkByVr` 4888, `UnmarkCurrentVoucher` 4915, `Create/GetPending*_PRV_MULTI` 6320–6476, `phy_insert` 8927, `phy_DeleteByVr` 8991, `phy_UnmarkByVr` 9042 | 1–9122 | Stock ledger (STKDT, STKDTPHY), pending-doc logic |
| `Model/GdnMDB.cs`, `GdnTDB.cs`, `GdnDDB.cs` | GDN | `insert` 18/17/17, `Cancel` 31/30/31, `update` 106, `DeleteBynvNo` 92, `DeleteByVrNo` 83, `DeletByVrNo` 44, `UnMarkBySalNo` 222, `GetVsVrTypeByVrNo` 317, `FY_Update_Transfer_Status` 323 | – | Auto delivery note |
| `Model/YvrDB.cs` | `YvrDB` | `insert` 12, `Cancel` 36, `Delete` 75, `MaxVrNo` 198 | – | Receipt vouchers (YVR) |
| `Model/BillByBillDB.cs` | `BillByBillDB` | `insert` 16, `DeletByTypeCodeAndStype` 417, `DeleteByRefNoAndVrType` 443, `SearchByRefAndVrType` 536, `GetByRefNoAndVrType` 1533 | – | BILLBYBILL / PENDBIL |
| `Model/BatchDetDB.cs` | `BatchDetDB` | `insert` 16, `DeleteByVrNo` 30, `GetStockByVoucher` 111 | – | BATCHDET |
| `Model/SntStkDB.cs` | `SntStkDB` | `CheckUsedByProduct` 327, `ClearAdjustmentAllByVoucher` 340, `UpdateUsedBySerialNumber` 353, `Phy_DeleteByVoucher` 661, `phy_insert` 666 | – | Serial numbers |
| `Model/PrMastDB.cs` | `PrMastDB` | `UpdateStock` 949, `UnitConDetailsByCode` 983, `CheckStockByGodown` 18546, `SalAcc` 18780 (+ many lookups) | 1–20395 | Product master |
| `Model/PrstDB.cs` | `PrstDB` | `GetAvailableStock` 2998, `updateByRack` 3071, `UpdateSalesPrice` 3111 | – | PRST (rack/batch stock) |
| `Model/FyDetailsDB.cs` | `FyDetailsDB` | `InvOptions` 182, `ValidateVoucherNo` 754, `UpdateVoucherNo` 825 | – | Voucher counters, invoice options |
| `Model/VoucherSystemDB.cs` | `VoucherSystemDB` | `Manage` 114, `GetSubDocNo` 371, `UpdateOrdNo` 492, `UpdateOrdNoChekByTable` 497 | – | VRSYSTEM branch numbering |
| `Model/CurrConvDB.cs` | `CurrConvDB` | `ToBase` 114, `ToAlt` 139, `ToBaseActual` 163 | – | Currency conversion |
| `Model/ApprovalModuleDB.cs` | `ApprovalModuleDB` | `GetAprvl` 42, `GetInventoryApprovalStatus` 59, `SaveConfirm` 151, `SaveDetails` 299, `UpdateDetails` 6012 | – | Approval workflow |
| `Model/EInvDB.cs` | `EInvDB` | `insert` 13, `Get` 26 | 1–55 | EINV |
| `Model/LogNoteDB.cs` | `LogNoteDB` | `Insert` 16 | – | LOGNOTE |
| `Model/ConfigDB.cs` | `ConfigDB` | `UpdateLasVrDate` 419 | – | CONFIGTB |
| `Model/AccMasDB.cs` | `AccMasDB` | `AccountsForSales` 400, `LedgerBalance` 897/919 | – | Customer lookup/balance |
| `Model/VrNoConfDB.cs` | `VrNoConfDB` | `SearchByVrTypeAndSeries` 53 | – | Invoice-number series |
| `SP.cs` | `SP` | `ManageSales_Salt` 13 (POS), `ManageSalesBilling_Salt` 71 (dead in Sales.cs), `ManageSalesReturn_Salt` 183, `ManageSalesOrder_Cordt` 236 | 1–484 | SP wrappers |
| `Functions.cs` | `Functions` | `GetTableStatement` 888, `VSDOCNO` 923, `LIST_ALLOW_VR_STMNT` 973, `GetGodownByBranch` 1404, `HB_ValidateSaving_Transactions` 2160, `HB_Validate_MasterOpening` 2220, `FYValidation` 2831, `SaveDoc` 3436 | 1–5108 | Shared helpers |
| `Variables.cs` | `Variables` | `LINEMERGESI`, `batch`, `SlNoTrack`, `TrackEdit`, `VRSYS`, `EinvoiceMode`, `ForcePay`, `PointsSysYN`, `CashCD`, `mult_alias`, `CostC`, … | 1–626 | Global settings (ported to the API as `DB/Variable.cs`) |

### Supporting sub-forms

| File | Lines | Purpose |
|---|---|---|
| `View/BatchForSales.cs` | 1–1015 | Batch picker (opened at `Sales.cs:10214`) |
| `View/BillAdjustment.cs` | 1–922 | Bill-by-bill adjustment after a credit sale (`Sales.cs:5570`) |
| `View/EInvoiceDetails.cs` | 1–178 | E-invoice details dialog (`Sales.cs:22205`) |
| `View/Data.cs` | 1–1162 | Generic voucher list and search |

---

## 4. Sales Return form – `View/SalesReturn.cs`

`SALM.STYPE='R'`, `RETURNFLG=1`. Approval module `"Sales Return"`, voucher type `SR`, cash refund posted as `YVR.VRTYPE='PV'`. The structure mirrors the invoice. Differences are called out below. Every write is inline SQL: `SaveSalt_Sql` 3893 (SP `ManageSalesReturn_Salt`) is **dead**, referenced only from the commented line 4081.

### 4.1 Load, lookups, modify

| Method / handler | Lines | Purpose | DB calls |
|---|---|---|---|
| `SalesReturn_Load` | 4531–4732 | Approval config, branch check, taxes, invoice options, next number, modify mode | `ApprovalModuleDB.GetAprvl`, `SalMDB.SearchByInvno/MAXORDNO`, `VoucherSystemDB.CheckVrUnderBranch/GetBranchCodeByTypeCode`, `TaxMasDB.ActiveTaxes`, `FyDetailDB.InvOptions`, `PrMastDB.Get_MFG_EXP` |
| `FillItem` / `FillCustomer` / `FillSalesman` / `FillCostCenter` / `Combo` | 1177–1311 | Lookups | `PrMastDB.ProductsBarCodeAndAlias/AliasAndPrCode/UnitsForProduct`, `PrAliasDB.AliasAndAlCode`, `AccMasDB.AccountsForSales/AccountsByGroup`, `SmMastDB.ActiveSalesman`, `CostCMasDB.CostCenters`, `InvFormatDB.ActiveFormats`, `PayModeDB.ActiveModes` |
| `Fill` | 773–1176 | Load an existing return. Reads `SALM.RTNINVNO` into `BillNo` (824) and the previous-year flag `SALT.PRVR` into `isPRV` (825, 1133) | `SalMDB/SalTDB.SearchByInvno`, `VoucherSystemDB.GetVSDocNoByOrdNo/GetOrdNoByInvNo`, `ApprovalModuleDB.GetApprovalStockUpdateStatus`, `SalDDB.SlCodeForAccount/SearchAddInfo`, `EInvDB.Get`, `SntStkDB.GetByVoucherIn/GetByVoucherInUsed/phy_GetByVoucherIn`, `PrMastDB.SalRetAcc`, `CurrMasDB.SearchByCode` |
| `Fill2` / `Fill3` / `SetDefaultBill` / `Taxes` | 4733–4932 | Defaults, tax list | – |
| `Clear` | 4301–4469 | Reset the form | – |
| `SalesReturn_Shown` | 11003–11044 | Approval button state | `ApprovalModuleDB.ManageAprvl` |

### 4.2 Link to the original invoice

| Method / handler | Lines | Purpose | DB calls |
|---|---|---|---|
| `txt_bill_no_KeyDown` | 8370–8871 | Enter the original invoice number, load its lines and compute the quantity already returned. Handles voucher-system numbers and previous-year invoices (`isPRV = true`, 8411) | `VoucherSystemDB.GetOrdNoByInvNo`, `SalMDB.SearchByOrdNoWithVRSYS/SearchByOrdNo`, `SalTDB.SearchByOrdNo`, `AccMasDB.SearchByID`, `CurrMasDB.SearchByCode`, `SalDDB.SlCodeForAccount/SearchAddInfo`, **`SalTDB.RetQty`** (8547 other-year rows `<> 'Y'`, 8552 `= 'Y'`; `SalTDB.cs:110`), `PrMastDB.SalRetAcc` (`PrMastDB.cs:18803`, sales-return account per item) |
| `txt_bill_no_Leave` / `txt_bill_no_TextChanged` | 11661–11708 / 11449–11453 | Same lookup on leave | `VoucherSystemDB.GetOrdNoByInvNo`, `SalMDB.SearchByOrdNoWithVRSYS/SearchByOrdNo` |
| `cmb_customer_SelectedValueChanged` | 5536–5675 | Changing the customer clears the invoice link (`BillNo = 0`, 5542). Balance, address, product list | `AccMasDB.LedgerBalance(Specific)`, `CusMasDB.GetAddress`, `PrMastDB.ProductsBarCodeAndAlias(ByGrpCode)` |
| `cmb_party_Leave` | 11383–11448 | Currency | `AccMasDB.SearchByID`, `CurrMasDB.SearchByCode` |
| Setting `Variables.SrWithoutInv` | used at 1940 | Allows a return with no original invoice | – |

### 4.3 Data entry and calculations

| Method | Lines | Purpose | DB calls |
|---|---|---|---|
| `dgv_sales_return_CellEndEdit` | 5750–7130 | Main line handler. Serial numbers come from the original SI or its DN | `PrMastDB.SearchByCode/DetailedByCode/UnitsForProduct/CheckSlNoTracking/CheckBatched`, `PrUnitMasDB.SearchById/SearchByName`, `PrAliasDB.*`, `SntStkDB.GetByVoucherOut_SI_in_SR(_Prv)/GetByVoucherOut_DN_by_SI_in_SR` (`SntStkDB.cs:106-116`), `GdwMasDB.ActiveGodownsWithStock`, `PriceCatLevDB.SearchByPrCodeAndRange` |
| `SearchProduct` / `SearchMultiAlias` / `SetMultiALiasWithQty` | 7131–7448 | Resolve product | `PrMastDB.*`, `PrAliasDB.*` |
| `PRateCalculation` | 7449–7754 | Cost rate (`SALT.PRRATE`) | `PrMastDB.SearchByCode`, `StkDtDB.TransactionsByPrcode/ReceiptsByPrcode`, `OpnStkDB.SummaryByItem`, `PurmDB.GetTrTypeByOrdno` |
| `RefreshPrdDtls` / `Calculation(ri)` | 7907–7924 / 7925–8369 | Line maths | – |
| `TaxSummary` | 7755–7906 | Tax summary grid | – |
| `PriceCalculation` / `NetCalculation` / `SubTotal` / `AddInfoCalculation` / `TotalAllignment` / `Balance` | 11454 / 11473 / 11562 / 11709 / 11760 / 12292 | Header totals | – |
| `Consolidate` | 3722–3892 | Line merge when `Variables.LINEMERGESR` | – |
| `ManageExpemtedTax`, `chk_vat_CheckedChanged` | 5012–5112, 8901–8943 | Exempt tax, VAT on/off | `PrMastDB.DetailedByCode` |
| `CalcPricefromBatch`, `SetPriceFlag`, `CheckBatchPrice`, `CheckBatch` | 3569, 12725, 12830, 5368 | Batch price rules | – |

### 4.4 Validation

| Method | Lines | Purpose | DB calls |
|---|---|---|---|
| `Functions.HB_ValidateSaving_Transactions` / `FYValidation` | called 3932 / 3936 | Permission/lock, FY date | – |
| `Validation` | 1491–2030 | Duplicate number text, batch MFG/EXP, serial numbers against the original SI/DN, invoice amount, earlier returns against the same invoice, currency, and batch stock on update (`ClearStock("ValidateUpdate")`, 1831) | `PrMastDB.Get_MFG_EXP/CheckBatched/SearchByCode/CheckSlNoTracking`, `SalMDB.CheckInvNoTextExists` (1512), `SntStkDB.GetUsedByVoucher/ValidateByCode/GetByVoucherOut_*_SR*`, `Functions.InvoiceAmountValidation` (1909), `SalMDB.SearchByOrdNo` (1956), `SalMDB.SearchByTypeAndRtnInv` (1979, 1983), `CurrConvDB.ToBaseActual` |
| `ValidateBatch` / `ValidateBatchGotoNegetive` | 2031–2131 / 3595–3721 | Batch stock | `PrstDB.GetBatchForValidation`, `BatchDetDB.GetBatchForValidation` |
| `ValidateEinvoice` | 13064–13187 | E-invoice mandatory fields | – |

### 4.5 Save / update (`btn_save_Click` 3930–4300)

Steps in execution order:

| # | Step | Lines | Method (lines) | Tables written / DB calls |
|---|---|---|---|---|
| 1 | Permission and FY checks | 3932–3936 | – | – |
| 2 | Edit → Update switch, `LogNote("old")` | 3943–3973 | – | – |
| 3 | `Validation`, approval refresh, save confirm | 3985–4005 | – | `ApprovalModuleDB.RefreshAprvl/SaveConfirm/GetInventoryApprovalStatus` |
| 4 | `Consolidate` (if `LINEMERGESR`), `SubTotal`, `TaxSummary`, `AddInfoCalculation`, `NetCalculation` | 4007–4014 | – | – |
| 5 | **Gate:** continues only if approval is pending, or the e-invoice XML succeeds (`EinvoiceMode` `API`/`Direct`) | 4016 | `ManageEinvoiceXML_API` 13188, `_DIRECT` 13435 | – |
| 6 | Header | 4018 | `SaveSalM` 2132–2303 (`RTNINVNO = BillNo` 2207) | **SALM**: `SalMDB.MAXORDNO/insert/update`, `FyDetailsDB.ValidateVoucherNo`, `YvrDB.MaxVrNo`, `VoucherSystemDB.GetSubDocNo/Manage` |
| 7a | *Update only:* delete lines, revert stock, release bill references | 4021–4069 | `ClearStock("Cancel")` 3416–3568 | `SalTDB.DeleteByVrNo`, `SalDDB.DeletByVrNo`, `BatchDetDB.DeleteByVrNo` (`SR`), `PrMastDB.UpdateStock`, `PrstDB.deleteByRack`; `BillDB.GetRefNo` then `SalMDB.UpdateBillRefFlgByOrdNoAndType` (VRTYPE Sales) / `PendBilDB.UpdateBillRefFlgByTypeAndInvNo` (Cus.Opening); `BillDB.DeletByTypeCodeAndStype` (Payment/SR); `SaveEinv` |
| 7b | *New only:* e-invoice, attachments | 4070–4075 | `SaveEinv` 13973, `SaveDoc` 14460 | **EINV** |
| 8 | Lines | 4077 | `SaveSalT` 2304–2589 (`RTNINVNO` 2323) | **SALT** |
| 9 | Account/tax lines | 4079 | `SaveSalD` 2590–2792 (inserts at 2668, 2718, 2766) | **SALD** |
| 10 | Stock ledger (in-entries) | 4082 | `SaveStkDt` 2793–2947 | **STKDT/STKDTPHY**: `StkDtDB.GetUTime/DeleteByVr/insert` + `phy_*`, `PrstDB.updateByRack` |
| 11 | Batch (if `Variables.batch`) | 4084–4089 | `SavePrSt` 2969–3152 (only if `ApprovalInventoryStatus`), `SaveBatchDet` 3153–3317 | **PRST** (`PrstDB.insert/DeleteByVrNo/updateByRack`), **BATCHDET** |
| 12 | Serials (if `Variables.SlNoTrack`) | 4091–4092 | `SaveSntStk` 3318–3403 | **SNTSTK/SNTSTKPHY** |
| 13 | Cash refund (credit mode `cmb_payment = 2`, cash > 0): delete the old PV, post 2 legs, adjust | 4095–4114 | `SavePayment` 12159–12231 ×2, `SaveAdjustment` 12232–12277 | **YVR** (PV), **BILLBYBILL** |
| 13b | Refund removed on edit: cancel the old PV | 4115–4132 | – | `YvrDB.Cancel`, `BillDB.DeletByTypeCodeAndStype`, `VoucherSystemDB.UpdateOrdNoChekByTable` |
| 14 | Credit: manual bill adjustment dialog (`FormType RET`, `GenType SRB`) | 4136–4163 | `View/BillAdjustment.cs` | **BILLBYBILL** |
| 15 | Approval records | 4167–4170 | `ApprovalModuleDB.UpdateDetails/SaveDetails` | **SALM** approval columns, **YVR** PV `APRVLST` (see the note in §2.7) |
| 16 | Last voucher date (new only) | 4172–4176 | – | **CONFIGTB** |
| 17 | Previous-year invoice: mark it as transferred in the **previous-year DB** | 4178–4183 | `SalMDB.FY_Update_RetTransfer_Status` (`SalMDB.cs:3765`) | SALM (prev-year DB) |
| 18 | Voucher counters, VRSYS `SR`/`PR` | 4185–4194 | `UpdateVoucherSystemOrdNo` 12629 | **FYDETAILS**, **VRSYSTEM** |
| 19 | Audit | 4196–4199 | `LogNote` 12337–12426 | **LOGNOTE** |
| – | Failure compensation: delete SALM (new only), SALT and SALD. Counters are reset in every path and in `catch` | 4231–4299 | – | – |

### 4.6 Cancel (`btn_cancel_Click` 10860–10975)

Calls `HB_ValidateSaving_Transactions`, the approval refresh, `ClearStock("ValidateCancel")` (10885), and the used-serial check `SntStkDB.GetByVoucherInUsed` (10889). It then runs `SalMDB.CancelBill`, `ClearStock("Cancel")` (10901), `SalTDB.Cancel`, `SalDDB.Cancel`, `StkDtDB.DeleteByVr/phy_DeleteByVr`, and `BillDB.GetRefNo`. Bill references are released through `SalMDB.UpdateBillRefFlgByOrdNoAndType` / `PendBilDB.UpdateBillRefFlgByTypeAndInvNo`. The rest of the chain is `BillDB.DeletByTypeCodeAndStype` ×2, `YvrDB.Cancel` (refund PV), `BatchDetDB.DeleteByVrNo`, `SntStkDB.DelteByVoucherIn/Phy_DeleteByVoucher` and `LogNote("cancel")`.

### 4.7 Other

| Method | Lines | Purpose |
|---|---|---|
| `InvNoText` / `VrInvNoText` | 11958 / 12058 | Number text from `VRNOCONF` |
| `ManageManualVoucherSystem` / `ManageVoucherSystem` / `UpdateVoucherSystemOrdNo` / `Set_VRSYS_TagAcc` | 12497 / 12557 / 12629 / 12643 | Branch voucher system |
| E-invoice | 13049–14016 | `btn_Einvoice_Click`, `ValidateEinvoice`, `ManageEinvoiceXML_API/_DIRECT`, `GetOrgInvDtls` 13836 (original-invoice reference for the credit note), QR, PDF/A-3, `SaveEinv` → EINV |
| `Print` / `CrrPrint` | 8961 / 9192–10228 | Printing (out of scope, data contract in §9) |
| `ListPayType` / `ShowPaymentType` | 15523 / 15541 | Payment split dialog |
| `MakeDup` / `btn_vr_paste_Click` | 15149 / 15455 | Copy/paste (out of scope, D5) |

---

## 5. Sales Order form – `View/SalesOrder.cs`

`CORDM/CORDT/CORDD` with `STYPE='O'` (the same class handles Quotation `STYPE='Q'` when `QtnFlg = true`, which is out of scope per D2). Approval module `"Sales Order"`. Advance posted as `YVR.VRTYPE='RV'` with `REFNO = 'SO:' + ORDNO`. Every write is inline SQL: `SaveCort_Sql` 4138 (SP `ManageSalesOrder_Cordt`, call at 4156) is **dead**, referenced only from the commented lines 4396 and 4401.

### 5.1 Load, lookups, modify

| Method / handler | Lines | Purpose | DB calls |
|---|---|---|---|
| `SalesOrder_Load` | 690–905 | Approval config, branch, invoice options, next number, number series | `ApprovalModuleDB.GetAprvl`, `VoucherSystemDB.CheckVrUnderBranch/GetBranchCodeByTypeCode`, `FyDetailsDB.InvOptions`, `CordMDB.MAXCODE` (`CordMDB.cs:69`), `VrNoConfigDB.SearchByVrTypeAndSeries` |
| `FillItem` / `FillCustomer` / `FillSalesman` / `Combo` | 668, 1732–1819 | Lookups | `PrMastDB.*`, `AccMasDB.AccountsForSales`, `SmMastDB.ActiveSalesman`, `InvFormatDB.ActiveFormats`, `PriceListMDB.List`, `PriceCatMasDB.Categories`, `PrMastDB.UnitsForProduct` |
| `Fill` | 906–1206 | Load the order. Reads `CORDM.CLOSED`/`CLOSEREMARKS` (961–987) and per-line `CORDT.CLOSES` (1116) | `CordMDB.SearchByOrdNO` (`CordMDB.cs:123`), `CordTDB.SearchByOrdNo`, `ApprovalModuleDB.GetApprovalStockUpdateStatus`, `CordDDB.SearchAddInfo`, `CurrMasDB.SearchByCode` |
| `Fill2` | 1207–1313 | Edit lock and button state | `StkDtDB.SearchByVr` (1230), quotation links `CordMDB.GetSOFromQTN` / `GdnMDB.GetDNFromQTN` / `SalMDB.GetSIFromQTN` |
| `Fill3` / `Taxes` | 1314–1365 | Defaults, taxes | `TaxMasDB.ActiveTaxes` |
| `Clear(isClose)` | 4635–4777 | Reset | – |
| `SalesOrder_Shown` | 7261–7309 | Approval button state | – |

### 5.2 Data entry and calculations

| Method | Lines | Purpose | DB calls |
|---|---|---|---|
| `dgv_sales_CellEndEdit` | 1820–2488 | Main line handler | `PrMastDB.SearchByCode/DetailedByCode/DetailedByAlias/UnitsForProduct`, `PrUnitMasDB.*`, `PrAliasDB.*`, `GdwMasDB.ActiveGodownsWithStock`, `PriceCatLevDB.SearchByPrCodeAndRange(Catg)` |
| `cmb_customer_SelectedValueChanged` | 5190–5322 | Balance, address, product list | `AccMasDB.LedgerBalance(Specific)`, `CusMasDB.GetAddress`, `PrMastDB.ProductsBarCodeAndAlias(ByGrpCode)` |
| `cmb_party_Leave` | 6528–6603 | Party leave handler (not yet read) | – |
| `cmb_pr_cat_SelectedValueChanged` | 7798–7882 | Re-price by price category | `PriceCatLevDB.*` |
| `SearchProduct` / `SearchMultiAlias` / `SetMultiALiasWithQty` | 8061–8432 | Resolve product | `PrMastDB.*` |
| `RefreshPrdDtls` / `Calculation(ri)` | 1366 / 1384–1731 | Line maths | – |
| `TaxSummary` / `AddInfoCalculation` / `Total` / `SubTotal` / `NetCalculation` / `TotalAllignment` | 2489 / 2611 / 2652 / 2680 / 5113 / 7933 | Totals | – |
| `Consolidate` | 3987–4137 | Line merge when `Variables.LINEMERGESO` | – |
| `CheckOrdQty` | 10328–10420 | Quantity check per row | `PrMastDB.SearchByCode` |
| `SetPriceFlag`, `ManageExpemtedTax` | 9459, 4829 | Batch price, exempt tax | – |
| `GetAdditionalDetails` | 9634–9777 | Carry add-info from source docs | `CordMDB.SearchByOrdNOS_AdditionalInfo`, `CordDDB.SearchAddInfo_ByOrdNOS` |
| Line close guards | 7310–7317, 7418–7427, 10994 | "This item is closed and cannot be modified." | – |
| *Quotation pull (out of scope, D2)* | `SearchQtn` 8696, `ListQtn` 9073, `dgv_order_CellValueChanged` 9158, `btn_PendingVoucher_Click` 11238 | – | – |

### 5.3 Validation

| Method | Lines | Purpose | DB calls |
|---|---|---|---|
| `Validation` | 2961–3193 | Duplicate number text, invoice amount, duplicate party order number | `CordMDB.CheckInvNoTextExists` (2982), `Functions.InvoiceAmountValidation` (3088), `CordMDB.SearchByPartyOrdNoAndType` (3122, `CordMDB.cs:891`) |
| `ValidateTaggedVoucher` | 3194–3417 | Quotation balance checks (**D2: skip logic, keep fields**) | `StkDtDB.Create/GetPendingQuotation_PRV_MULTI`, `CordTDB.BalQtnQty(ByRowID)`, `PrMastDB.UnitConDetailsByCode` |

### 5.4 Save / update (`btn_save_Click` 4252–4634)

| # | Step | Lines | Method (lines) | Tables written / DB calls |
|---|---|---|---|---|
| 1 | Permission and FY checks | 4254–4258 | – | – |
| 2 | Edit → Update. **Edit is blocked when `STKDT` has issue rows (`TYPE='I'`) against the order**, i.e. an invoice or DN was already generated (4270–4280). Then `LogNote("old")` | 4264–4328 | – | `StkDtDB.SearchByVr` |
| 3 | `Validation`, approval refresh, save confirm | 4337–4356 | – | `ApprovalModuleDB.*` |
| 4 | `Consolidate` (if `LINEMERGESO`), totals | 4358–4365 | – | – |
| 5 | Header | 4367 | `SaveCordM` 3597–3718 (new: `ClosedFlg=false` 3633, `CLOSEREMARKS=''` 3661) | **CORDM**: `CordMDB.Insert/Update`, `FyDetailsDB.ValidateVoucherNo`, `VoucherSystemDB.Manage` |
| 6a | *Update only:* delete lines and add-info, delete the old advance and its bill refs | 4370–4390 | – | `CordTDB.DeleteByOrdNo`, `CordDDB.DeleteByVrNo`, `YvrDB.SelectByRefNo/DeleteByRefNo` (`'SO:'+OrdNo`), `FyDetailsDB.UpdateVoucherNo("RV")`, `BillDB.DeletByTypeCodeAndStype` (Receipt) |
| 6b | *New only:* attachments | 4391–4395 | `SaveDoc` 10527 | – |
| 7 | Lines (`CLOSES` per line, 3880) | 4397 | `SaveCordT` 3719–3901 | **CORDT** |
| 8 | Add-info | 4399 | `SaveCordD` 3902–3986 (inserts at 3927, 3964) | **CORDD** |
| 9 | Order stock rows (pending-quantity source, `CLOSES` 3571) | 4402 | `SaveStkDt` 3458–3596 | **STKDT/STKDTPHY** |
| 10 | Advance (if `txt_advance > 0`): Dr cash, then Cr party | 4404–4411 | `SaveAdvance` 3418–3457 ×2 | **YVR** (RV): `YvrDB.MaxVrNo/insert` |
| 11 | Approval records, `LogNote`, VRSYS counter, FY counter, previous-year quotation transfer flag | 4412–4424 (and the same block repeated at 4474–4486 and 4521–4533) | `UpdateVoucherSystemOrdNo` 9447, `SetVoucherNo` 8535, `UpdateTransferFlg_PRV` 4169–4212 | **CORDM** approval columns (§2.7 note), **LOGNOTE**, **VRSYSTEM**, **FYDETAILS**, CORDM (prev-year, `FY_Update_Transfer_Status`) |
| – | Failure compensation: delete CORDM (new only), CORDT and CORDD, `SetVoucherNo(false)` | 4457–4470, 4566–4610 | – | – |

### 5.5 Cancel, short-close, reopen (D6)

| Method | Lines | Purpose | DB calls / columns written |
|---|---|---|---|
| `btn_cancel_Click` | 6821–6935 | Blocked when linked vouchers exist (`StkDtDB.SearchByVr` 6846) or the order is closed ("The voucher is already closed." 6899). Asks for remarks (6910) | `CordMDB.SearchByOrdNO`, `CordMDB/CordTDB/CordDDB.Cancel` (6911–6915), `StkDtDB.DeleteByVr/phy_DeleteByVr` (6920–6921), `YvrDB.CancelByRefNo` (6925, advance) |
| `btn_Close_Click`, whole voucher | 9805–9880 | Close with remarks | `CordMDB.Close` (9876) → `CORDM.CLOSED=1, CLOSEREMARKS` (`CordMDB.cs:707`) |
| `btn_Close_Click`, selected lines | 9898–9970 | Choose lines to close: `CORDT` is **deleted and re-inserted** with `CLOSES` per line (9929–9937). Then the whole order is closed | `CordTDB.DeleteByOrdNo` + `SaveCordT` (9935–9937), `CordTDB.Close` (9951) → `CORDT.CLOSES` (`CordTDB.cs:42`), `StkDtDB.Close/phy_Close` (9956–9957) → `STKDT/STKDTPHY.CLOSES` (`StkDtDB.cs:25`, `:9101`), `CordMDB.Close` (9965) |
| `btn_reOpen_Click` | 9983–10034 | Reopen | `CordMDB.Reopen` (10001) → `CORDM.CLOSED=0, CLOSEREMARKS=''` (`CordMDB.cs:720`). **No reset of `CORDT.CLOSES` or `STKDT.CLOSES` was found in this range** (Phase 2 to confirm) |

### 5.6 Other

| Method | Lines | Purpose |
|---|---|---|
| `InvNoText` / `SetVoucherNo` | 8433 / 8535 | Number text from `VRNOCONF`. FY counter |
| `LogNote` | 8559–8657 | **LOGNOTE** |
| `ManageManualVoucherSystem` / `ManageVoucherSystem` / `UpdateVoucherSystemOrdNo` | 9355 / 9378 / 9447 | Branch voucher system |
| `btn_AdditionalInfo_Click` | 10056 | Additional info (`SOF1..20`) |
| `Print` / `CrrPrint` | 5345 / 5519–6517 | Printing (out of scope, data contract in §9) |
| `MakeDup` / `btn_vr_paste_Click` | 11336 / 11548 | Out of scope (D5) |

---

## 6. Stored procedures and types (`QuickBalance.sql`)

**None of the three legacy forms calls a stored procedure on a live path.** The two SP wrappers they reference are dead code, shown in the table below.

| Object | Script line | Used by legacy? | Used by Web API? | Notes |
|---|---|---|---|---|
| `ManageSalesBilling_Salt` | 8358 | Only via dead `SaveSalt_Sql` | No | TVP `UDT_SALBILL` (351) |
| `ManageSales_Salt` | 8192 | POS (`POS.cs:2302`) | No | Out of scope (POS) |
| `ManageSalesReturn_Salt` | 8595 | Only via dead `SalesReturn.SaveSalt_Sql` (sole call site commented at 4081) | No | TVP `UDT_SALRET` (395) |
| `ManageSalesOrder_Cordt` | 8497 | Only via dead `SalesOrder.SaveCort_Sql` 4138 (call sites commented at 4396, 4401) | No | **Dead** (verified 2026-11-23) |
| `Manage_SaslesInvoiceDetails` | 6959 | No | No | Unused; looks like an earlier API attempt |
| `Manage_SaslesOrderDetails` | 7422 | No | No | Unused |
| **`ASK_SP_ManageSalesInvoiceDetails`** | 5334–6116 | No | **Yes**: `DB/SalesInvoiceDB.cs:182` | API-era port of the invoice save. Phase 2/3 will diff it against the legacy chain above |
| **`ASK_SP_CancelSalesInvoiceDetails`** | 5107–5195 | No | **Yes**: `DB/SalesInvoiceDB.cs:192` | |
| `ASK_SP_PendingSalesOrderInInvoices` | 6447–6590 | No | Yes: `DB/SalesOrderDB.cs:140` | |
| `ASK_SP_PendingSalesInvoicesInReturn` | 6369–6446 | No | Yes: `DB/SalesInvoiceDB.cs:202` | |
| `ASK_SP_ManageSalesOrderDetails` / `ASK_SP_CancelSalesOrderDetails` | 6117 / 5196 | No | Yes: `DB/SalesOrderDB.cs` | |
| `ASK_SP_CancelSalesReturnDetails` | 5248 | No | Yes: `DB/SalesReturnDB.cs:184` | `ASK_SP_ManageSalesReturnDetails` is referenced by `DB/SalesReturnDB.cs:174` but **not found in `QuickBalance.sql`** |
| `Get_PendingSalesOrderDetails` / `Get_PendingSalesOrderQuanity` | 6591 / 6611 | No | To check | |
| Types `ASK_UDT_SalesInvoiceItem` / `…SerialNumber` / `…AdditionalInfo` | 44 / 72 / 40 | – | Yes | `ASK_UDT_SalesReturn*` used by the API are also **not in the script** |

## 7. Inline SQL (legacy invoice path)

The legacy invoice path uses **no stored procedure**. Every write is a string-concatenated statement sent through `clsDBFunction` (`Variables.db`), with an MS Access fallback (`Variables.dbAcc`). Representative statements:

| Location | Statement |
|---|---|
| `SalMDB.cs:22` | `INSERT INTO SALM (…95 cols…) OUTPUT INSERTED.INVNO VALUES(…)` → `INVNO` identity is the internal ID; `ORDNO` is the per-type running number |
| `SalMDB.cs:70` | `UPDATE SALM SET … WHERE INVNO=…` |
| `SalMDB.cs:46` | `MAXORDERNO`: `GetnewId("ORDNO","SALM WHERE STYPE=…")` (MAX+1, not concurrency-safe) |
| `SalTDB.cs:19` / `:73` | `INSERT INTO SALT (…67 cols…)` / `DELETE FROM SALT WHERE INVNO=…` |
| `SalTDB.cs:32` | Cancel: `UPDATE SALT SET ORGRATE=RATE, ORGQTY=QTY, QTY=0, RATE=0, CANCELFLG=1, CONVTYPE=-1 … WHERE INVNO=…` |
| `SalDDB.cs:21` / `:35` | `INSERT INTO SALD (…24 cols…)` / `DELETE FROM SALD WHERE VRNO=…` (no VRTYPE filter) |
| `Home.cs:3164–3178` | List query: `SALM INNER JOIN ACCMAS ON SALM.CUSTCODE=ACCMAS.ID`, branch filter `LIST_ALLOW_VR_STMNT` |
| `CordMDB.cs:711` / `:724` | Close / reopen: `UPDATE CORDM SET CLOSED=…, CLOSEREMARKS=… WHERE ORDNO=… AND STYPE=…` |
| `CordTDB.cs:46` | `UPDATE CORDT SET CLOSES='TRUE' WHERE ORDNO=… AND STYPE=…` |
| `StkDtDB.cs:30` / `:9106` | `UPDATE STKDT` / `STKDTPHY SET CLOSES=… WHERE ORDNO=… AND VRTYPE=… AND TYPE=…` |

## 8. Tables touched (all three forms)

Columns: **SI** = Sales Invoice, **SR** = Sales Return, **SO** = Sales Order. W = written, R = read.

| Table | SI | SR | SO |
|---|---|---|---|
| SALM / SALT / SALD | W | W (+ prev-year SALM transfer flag) | R (quotation links only) |
| CORDM / CORDT / CORDD | R (+ transfer flag) | – | W (incl. `CLOSED`, `CLOSEREMARKS`, `CLOSES`) |
| GDNM / GDNT / GDND | W (auto-DN) | – | R |
| STKDT / STKDTPHY | W | W | W (+ `CLOSES`) |
| PRST / PRMAST | W | W | – |
| BATCHDET | W | W | – |
| SNTSTK / SNTSTKPHY | W | W | – |
| YVR | W (RV) | W (PV) | W (RV advance, `REFNO 'SO:'+ORDNO`) |
| BILLBYBILL | W | W | W (delete only) |
| PENDBIL | W | W (`BILLREFFLG`) | – |
| EINV | W | W | – |
| FYDETAILS / VRSYSTEM / CONFIGTB / LOGNOTE | W | W | W (no CONFIGTB) |
| APRVLMODULETB | R | R | R |
| Approval columns (`APRVLST`, `APRVLABY/ADT`, `APRVLBBY/BDT`) | W on SALM, GDNM (auto-DN), YVR RV | W on SALM, YVR PV | W on CORDM |

Per-table detail for the invoice:

| Table | Script line | R/W | Role |
|---|---|---|---|
| **SALM** | 4182 | R/W | Invoice header (`STYPE` O=invoice, R=return; `INVNO` id, `ORDNO` number, `INVNOTEXT`/`VSDOCNO` display number) |
| **SALT** | 4300 | R/W | Invoice lines (`INVNO` FK, `PRCODE`, qty/rate/disc/tax, links `SORDNO`/`GDNNO`/`ORDROWID`/`NTROWID`/`QTNROWID`, `PRV*` previous-year flags) |
| **SALD** | 4114 | R/W | Account and tax distribution / additional info (`VRNO`=INVNO, `ETYPE`, `CODE`=account, `AMOUNT`, `TAXAMT`) |
| **STKDT** / **STKDTPHY** | 4531 / 4583 | R/W | Stock ledger (book and physical) |
| **GDNM / GDNT / GDND** | 2005 / 2092 / 1972 | R/W | Auto delivery note |
| GDNTPRV, SALTPRV, SALE | 2168, 4378, 4149 | ? | Not referenced directly by the Sales.cs save path. Verify in Phase 2 |
| **YVR** | 4969 | R/W | Receipt vouchers for cash/bank paid at billing |
| **BILLBYBILL** / PENDBIL | 674 / 2878 | R/W | Bill-wise receivable adjustment |
| **BATCHDET** | 636 | R/W | Batch-wise stock out |
| **SNTSTK / SNTSTKPHY** | 4485 / 4508 | R/W | Serial-number tracking |
| **PRST** / **PRMAST** | 3515 / 3364 | R/W | Rack/batch stock, product stock, sales price |
| **EINV** (+ EINVOICE) | 1764 / 1792 | R/W | E-invoice data |
| **FYDETAILS** | 1845 | R/W | Voucher-number counters, invoice options |
| **VRSYSTEM**, **VRNOCONF** | 4934 / 4912 | R/W, R | Branch voucher numbering, series config |
| **CONFIGTB** | 1074 | W | Last voucher date |
| **APRVLMODULETB** | 605 | R | Approval configuration (`APRVL` level, `MINVAL`). Not written by the save path |
| **LOGNOTE** | 2638 | W | Audit log |
| CORDM / CORDT / CORDD | 1307 / 1413 / 1278 | R (+W transfer flag) | Sales order / quotation source docs |
| ACCMAS, CUSMAS, TAXMAS, CURRCONV, INVFORMAT, INVOPTIONS, PAYMODE, SMMAST, COSTCMAS, PRICE* , PRUNITMAS, PRTM/PRTT, GDWMAS, BRANCHMASTER, COMPMAS | various | R | Lookups / masters |

---

## 9. Print data contract (fields the API must be able to return)

Printing is out of scope (`00-scope.md` §2), but the front end must be able to build the same documents. Every RDLC and Crystal layout binds to typed datasets in `Print/BillDataset.xsd` and `Print/TaxInvoiceDataSet.xsd`. The forms fill them with `.Merge(...)` (for example `Sales.cs` 11874–12508 RDLC and 13319–15913 Crystal). Column lists below are extracted from the XSDs.

| Dataset table | Used by | Columns |
|---|---|---|
| `dtSalesBill` (69) | SI, SR, SO | Header `M_VRDATE, M_INVDATE, M_BILLNO, M_HEAD, M_COMPNAME, M_COMPSECNAME, M_COMPADDRESS, M_VATNO, M_PHONE, M_LOGO, M_ACCNO, M_IBAN, M_BANK, M_CUSTNAME, M_CUSTSECNAME, M_CUSPRINTNAME, M_CUSTADDRESS, M_SALESMAN, SALESMAN`. Totals `M_TOTQTY, M_TOTAMT, M_TOTTAX, M_DTOTAL, M_FREIGHT, M_DISC, M_CUSDUITY, M_ROUND, M_NETAMT, M_VAT, M_BALANCE, M_AMTINWORDS, LOP, ADJ, QRCODE`. Lines `D_SLNO, D_ITEM, D_ITEMDESC, D_ITEMALIAS, PRALIAS, PRALIAS_QTY_UNIT, D_PRSECNAME, D_PRDPRINTNAME, D_UPCCODE, D_QTY, D_MAINQTY, D_ALTQTY, D_UNIT, D_RATE, D_ACTUALPRICE, D_ACTUALAMOUNT, D_DISC, D_DISCPERC, D_ADDINFODISC, D_TOTDISC, D_AMT, D_TAX, D_TAXPERC, D_TOTAL, D_PRNOTES, D_BML, D_BMQ, D_BMW, D_BATCH, D_ITEMSLNO, D_GODOWN`. Tax summary `T_SLNO, T_PERC, T_AMT, T_TAXAMT` |
| `dtSalesBill1` (2) | SI, SR, SO | `ADDINFO, ADDAMT` (additional charges, SALD/CORDD) |
| `dtSalesBill2` (8) | SI | `DETAIL, DETAMT, DETAIL2…4, DETAMT2…4` |
| `dtTotal` (4) | SI | `TOTQTY, TOTAMT, TOTTAX, TOTVALUE` |
| `dtSalTax` (57) | SI | Bilingual tax invoice: each `M_*`/`D_*` field has a `…2` (secondary language) pair, plus `M_EXVAT, M_VATINWRDS, M_TOTINWRDS, DX_RATE, DX_AMT, D_TPRC, T_QTY, M_GTOT` |
| `dtTaxInvoice` (73) | SI, SR, SO | `M_CPONO, M_DNOTE, M_GRNNO, M_VNDRNO, M_PAYTERMS, M_PURDATE`, VAT/net totals, `T_*INWORDS`, `C_ACCNAME, C_BANK, C_IBANNO`, `D_PRDMFGDATE, D_PRDEXPDATE, D_MRP` (+ `…2`/`…1` pairs) |
| `dtTaxInvoiceArabic` (24) | SI, SR, SO | Secondary-language header and lines, `M_NARRATION, M_SHIPADDRESS, QRCODE, M_COMPSECADDRESS` |
| `dtBranch` (24) | SI, SR, SO | `BRNAME, BRALIASNAME, BRSECNAME, ADRS, PH, MAIL, ACCNAME, ACCNO, BANK, IBAN, SWCD`, national address `STREETONE, STREETTWO, BULDINGNO, PLOTID, CITYNAME(SEC), POSTALCODE, DISTRICT(SEC)`, `GRPVATNO, EINVIDTYPE, EINVID, VATTYPE` (`BranchMasterDB.SearchByID4Print`) |
| `dtAddInfo_SI` (60) | SI, SR | `SIF1..20` (label), `SIV1..20` (value), `SIA1..20` (flag) from `SALM.SIF*` |
| `dtAddInfo_SO` / `dtAddInfo_QTN` (60 each) | SO / Quotation | `SOF/SOV/SOA 1..20`, `QTNF/QTNV/QTNA 1..20` |

Most of these are formatted or derived values (words, QR, secondary names). Phase 3 decides which ones the `get` endpoints return as raw data and which the front end derives.

---

## 10. Early observations (to confirm in Phase 2)

1. **No transaction boundary.** `btn_save_Click` (5166–6152) runs about 15 independent auto-committed statements in sequence: SALM, then delete+insert SALT/SALD, STKDT, GDN*, BATCHDET, SNTSTK, YVR, BILLBYBILL, LOGNOTE. A failure midway leaves a partial invoice. The API's `ASK_SP_ManageSalesInvoiceDetails` may already fix this.
2. **Update is delete-and-reinsert** for SALT and SALD (5316–5323), and for GDNT, GDND and BATCHDET when a GDN exists.
3. **Cancel is a soft cancel**: qty and rate are zeroed, with the originals kept in `ORGQTY`/`ORGRATE` (`SalTDB.cs:32`).
4. `ORDNO` is generated by `MAX+1` (`SalMDB.cs:46`), which isn't concurrency-safe.
5. **SQL injection exposure** in every legacy DB method. This doesn't matter for the port, except that it shows the API must not copy the SQL verbatim.
6. The main grid handler is named `dgv_purchase_CellEndEdit` (9727). It's a copy-paste name, but it's the real sales line logic.
7. Two repository scripts are missing objects the API references: `ASK_SP_ManageSalesReturnDetails`, `ASK_UDT_SalesReturnItem/SerialNumber/AdditionalInfo` (confirmed against the live DB, `api-review.md` A2).
8. **Sales Return reports success after a failed stock save.** In `SalesReturn.btn_save_Click`, the block that handles `SaveStkDt == false` closes at 4165. Approval, `LogNote`, counters and the success message (4167–4229) then run anyway, and SALM, SALT and SALD stay in place.
9. **Sales Return save is gated on e-invoice** (4016). With no approval pending, the voucher saves only when `EinvoiceMode` is `API` or `Direct` and the XML step returns `true`. Phase 2 must check what these return when e-invoicing is off.
10. **Sales Order advance has no compensation.** If the first `SaveAdvance` leg (Dr cash) succeeds and the second (Cr party) fails (4408–4410), nothing is rolled back and no message is shown. The order is saved with a one-sided YVR.
11. **Sales Order edit deletes the advance's `BILLBYBILL` rows** (4386–4388), but save never inserts them. Something else must write them, such as bill adjustment on the receipt. Phase 2 checks this.
12. **Sales Order reopen** resets only `CORDM.CLOSED`, not `CORDT.CLOSES` or `STKDT.CLOSES` (§5.5). Closed lines may stay closed after a reopen.
13. **Sales Order edit lock:** an order can't be edited once any invoice or DN has consumed it (`STKDT TYPE='I'`, 4270–4280). The API must enforce the same rule.

---

## Review questions before Phase 2

1. **Map completeness.** Sales Return (§4) and Sales Order (§5) are now mapped at the same depth as the invoice. Is anything missing that you expected, such as another entry point, form or setting?
2. **Print data contract (§9).** Should Phase 2/3 treat every column in §9 as an API requirement? Or only the raw data, with the front end handling formatting, amount-in-words, QR, and secondary-language names?
3. **Baseline for Phase 3** (carried over and still open). Should I treat `ASK_SP_ManageSalesInvoiceDetails` / `…SalesOrderDetails` as the API implementation to diff against the legacy chains? Or should I verify those SPs line by line as untrusted ports? The second takes longer but would catch errors like the `STKDTPHY` gap (D4).
4. **Sales Return line-level linkage.** A return links to its source invoice at header level (`SALM.RTNINVNO`, `SALT.RTNINVNO`) and checks returned quantity through `SalTDB.RetQty`. Previous-year invoices (`PRVR = 'Y'`, transfer flag written to the previous-year DB) are part of this. Should previous-year returns be in scope for the API? `00-scope.md` doesn't decide this.
