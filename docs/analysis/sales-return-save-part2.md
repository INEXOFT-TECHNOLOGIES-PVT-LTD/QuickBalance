# Sales Return Save, Part 2 (legacy analysis)

Analysis only. No code or database objects were modified. No agents were used.
Live file: `D:\PROJECT\IFlare\Inventory\Inventory\View\SalesReturn.cs` (written `SR.cs` below).
Model folder: `D:\PROJECT\IFlare\Inventory\Inventory\Model\` (written `Model\`).

Tags: `[VERIFIED: file:line]` = read directly. `[ASSUMPTION]` = not confirmed in code. Anything not read is marked NOT READ.
SQL is copied verbatim from the `Variables.dbType == "SQL"` branch. Each Model method also has an Access (`dbAcc`) branch with the same columns and different quoting; it is not copied.
Line numbers in the task brief matched the file exactly (SaveSalT 2304, SaveSalD 2590, SaveStkDt 2793, SavePrSt 2969, SaveBatchDet 3153, SaveSntStk 3318, ClearStock 3416, SavePayment 12159, SaveAdjustment 12232, SaveEinv 13973).

## 0. Where these methods are called (call order in the Save handler)

[VERIFIED: SR.cs:4021-4168]

1. Edit mode (`btn_save.Text == Variables.UpdateText`): reverse the old save first.
   - `SalTDB.DeleteByVrNo`, `SalDDB.DeletByVrNo`, `BatchDetDB.DeleteByVrNo(SType="SR")` (4024-4031). The `PrstDB.DeleteByVrNoAndType` call is commented out (4033-4035).
   - `ClearStock("Cancel")` (4037).
   - Un-reconcile bill refs from `BILLBYBILL` rows of Stype `SalesReturn` (4039-4063).
   - Delete `BILLBYBILL` where TYPECODE=YvrNo, STYPE=`Payment`, GENTYPE=`SR` (4064-4067).
   - `SaveEinv()` (4068).
2. New mode: `VrNo = retVal; SaveEinv(); SaveDoc();` (4070-4075). `SaveDoc` is NOT READ.
3. `SaveSalT()` then `SaveSalD()` then `SaveStkDt(clsSalM.OrdNo)` (4077-4082). Each is nested in the previous one's success `if`.
4. If `Variables.batch`: `SavePrSt()` only when `ApprovalInventoryStatus`, then `SaveBatchDet()` always (4084-4089).
5. If `Variables.SlNoTrack`: `SaveSntStk()` (4091-4092).
6. Part payment and `BillAdjustment` dialog (4094-4163), see section 7.

The return values of `SavePrSt`, `SaveBatchDet`, `SaveSntStk`, `SaveAdjustment` and `SaveEinv` are not checked by the caller. A failure there is silent.
[VERIFIED: SR.cs:4087-4092, 4104, 4068, 4073]

---

## 1. SaveSalT() — writes SALT [VERIFIED: SR.cs:2304-2589]

Writes one `SALT` row per grid row of `dgv_sales_return` via `SalTDB.insert`. Rows are skipped unless `Qty > 0` and the PRCODE cell is non-null (2328, 2503).
Success means every row was counted (`i == Rows.Count`, 2578). A row whose insert returns 0 is not counted, so the method returns false.

### Header-level values (same for every row)

| Column | Value | Source |
|---|---|---|
| STYPE | constant `"R"` | 2308 |
| ORDNO | `clsSalM.OrdNo` | 2309 |
| INVNO | `VrNo` | 2321 |
| INVDATE | `dtp_date.Value.ToString(Variables.format)` | 2310 |
| BTYPE | payment mode 1 → `"C"`, 2 → `"R"`, anything else (3, 4) → `"D"` | 2311-2320 |
| CUSTCODE | `cmb_party.SelectedValue` | 2322 |
| RTNINVNO | `BillNo` (the original sales invoice number; its assignment is NOT READ) | 2323 |
| VSVRTYPE | `clsSalM.VSVRTYPE` | 2324 |
| RETURNFLG | `clsSalT.Returnflg = true` | 2352 |
| SMCODE | `cmb_salesman.SelectedValue` | 2361 |
| CURCONRATE / CURCODE | `CurrConvRate` / `CurrCode` | 2380-2381 |
| LOGCD | `Variables.UserId` (in the Model, not the form) | `Model\SalTDB.cs:19` |
| GDNFLG | constant `false` | 2399 |
| PRV | constant `"N"` | 2499 |
| PRVR / PRVRY | `isPRV ? "Y" : "N"` / `isPRV ? Variables.PrevYRDB : ""` | 2501-2502 |
| CANCELFLG, SORDNO, GDNNO, DISCP, SPRTMPCODE, PRVN, LM, ORDROWID, NTROWID, TRNROWID, QTNCODE, QTNROWID, PRVQ, PRVQY, PRVY, PRVNY | not set in this method; whatever the `clsSalT` object holds | NOT READ (class defaults in `Controller\clsSalT.cs`) |

### Per-row values

| Column | Value | Source |
|---|---|---|
| PRCODE | grid `Dgv_col_prcode` | 2330 |
| QTY / ORGQTY | grid `Dgv_col_qty` (both the same) | 2331, 2362 |
| UNITCODE | grid `Dgv_col_unit_code` | 2332 |
| RATE | grid `Dgv_col_price`. `CurrConvDB.ToBaseActual` if `Variables.INVAMTEDIT_SI`, else `CurrConvDB.ToBase` | 2334-2337 |
| ORGRATE | same as RATE | 2360 |
| PRRATE | grid `col_prRate` via `ToBase`, or 0 if empty | 2339-2342 |
| TAXCODE | grid `Dgv_col_taxcode` | 2343 |
| DISC | grid `Dgv_col_disc` via `ToBaseActual`, or 0 | 2344-2347 |
| TAX | grid `Dgv_col_tax_perc`, or 0 (a percentage, not converted) | 2348-2351 |
| BATCH | grid `Batch`, or `""` | 2353-2358 |
| EXPIRY | grid `col_expiry` if present, else the document date (the earlier `DateTime.Now` at 2359 is overwritten) | 2359, 2485-2488 |
| MFGDATE | grid `col_mfgdate` if present, else the document date | 2489-2492 |
| ALCODE / ALNAME | grid `Dgv_col_alcode` (else `"0"`) / `Dgv_col_alname` (else `""`) | 2364-2371 |
| GDWCODE | grid `Dgv_col_godown`, else `Functions.GetGodownByBranch()` | 2372-2375 |
| PRDESC | grid `Dgv_col_prod_desc`, or `""` | 2376-2379 |
| LDISCTYPE | grid `col_Ldisc_type`, or `""` | 2383-2386 |
| LDISCPERC | grid `col_Ldisc_perc`, or 0 | 2387-2390 |
| LDISCAMT | grid `col_Ldisc_value` via `ToBase`, or 0 | 2391-2394 |
| TOTDISC | grid `col_net_disc` via `ToBaseActual`, or 0 | 2395-2398 |
| SLCODE | if `Variables.CostC`: `col_costc` when `txt_multi_cosctc` = "YES", else `cmb_costc.SelectedValue`; if not `CostC`: 0 | 2401-2409 |
| PRFLG | `true` if grid `col_price_flg` = "Y" | 2417-2420 |
| CONVTYPE | grid `col_conv_type` if present. Otherwise unchanged from the previous row (it is not reset) | 2421-2422 |
| CONVFACT | grid `col_conv_fact` if non-zero, else 1 | 2423-2426 |
| CONVQTY | see the rule below | 2427-2459 |
| CONVRATE | grid `col_conv_rate` via `ToBaseActual`, else grid `Dgv_col_price` via `ToBaseActual`. The `if (true)` makes the `ToBase` branch dead code | 2462-2475 |
| BML / BMQ / BMW | `GetDec(col_Length / col_QtyNos / col_Weight)` | 2477-2479 |
| MRP | grid `col_mrp` via `ToBase`, or 0 | 2481-2484 |
| VATREASONCD | `ExportReason` when `cmb_format` = 7, else `Functions.GetInt(col_vatExceptionReason)` | 2494-2497 |
| RETREASON | `Functions.GetStr(col_ret_reason)` | 2498 |
| SNTSLNO | `Functions.GetInt(col_slno)` | 2500 |

CONVQTY rule [VERIFIED: SR.cs:2427-2459]:
- If `col_conv_type` is set and not `"-1"`:
  - If the line unit equals the item's alternate unit (`altUnitId`, read from `PRMAST.ALTUNIT` by `PrMastDB.UnitConDetailsByCode`): type 0 → `qty × conv_fact`. Type 1 with a non-zero factor → `qty / conv_fact`. Anything else → `qty`.
  - Otherwise: `qty × Al_Qty` if `Al_Qty` is present and non-zero, else `qty`.
- If `col_conv_type` is empty or `-1`: `qty × Al_Qty` if `Al_Qty` is present and non-zero, else `qty`.

### Model SQL: `SalTDB.insert` [VERIFIED: Model\SalTDB.cs:15-20]

```sql
INSERT INTO SALT (STYPE,ORDNO,INVNO,INVDATE,BTYPE,PRCODE,CUSTCODE,QTY,RATE,PRRATE,DISC,TAXCODE,TAX,RETURNFLG,BATCH,EXPIRY,ORGRATE,SMCODE,LOGCD,ORGQTY,ALCODE,ALNAME,UNITCODE,GDWCODE,PRDESC,CURCONRATE,CURCODE,LDISCTYPE,LDISCPERC,LDISCAMT,TOTDISC,GDNFLG,SLCODE,PRFLG,SORDNO,GDNNO,CANCELFLG,RTNINVNO,CONVTYPE,CONVFACT,CONVQTY,CONVRATE,DISCP,MRP,SPRTMPCODE,BML,BMQ,BMW,MFGDATE,VSVRTYPE,VATREASONCD,RETREASON,PRV,SNTSLNO,PRVN,PRVR,LM,ORDROWID,NTROWID,TRNROWID,QTNCODE,QTNROWID,PRVQ,PRVQY,PRVY,PRVNY,PRVRY)
VALUES('<SType>','<OrdNo>','<InvNo>','<InvDate>','<BType>','<PrCode>','<CustCode>','<Qty>','<Rate>','<PrRate>','<Disc>','<TaxCode>','<Tax>','<Returnflg>','<Batch>','<Expiry>','<OrgRate>','<SmCode>','<Variables.UserId>','<OrgQty>','<AlCode>',N'<AlName>','<UnitCode>','<GdwCode>',N'<PrDesc>','<CurConRate>','<CurCode>','<LDiscType>','<LDiscPerc>','<LDiscAmt>','<TotDisc>','<GdnFlg>','<SlCode>','<PrFlg>','<SordNo>','<GdNo>','<CancelFlg>','<RtnInvNo>','<ConvType>','<ConvFact>','<ConvQty>','<ConvRate>','<DiscP>','<Mrp>','<_PRTMPCODE>','<_BML>','<_BMQ>','<_BMW>','<MfgDate>',<VSVRTYPE>,<VATREASONCD>,'<RetReason>','<PRV>','<SNTSLNO>','<PRVN>','<PRVR>','<LM>','<ORDROWID>','<NTROWID>','<TRNROWID>','<QTNCODE>','<QTNROWID>','<PRVQ>','<PRVQY>','<PRVY>','<PRVNY>','<PRVRY>')
```

The placeholders `<X>` stand for `clsSalT.X`, concatenated into the string with single quotes. The statement is a string-concatenated INSERT with no parameters. It is not verbatim at the character level for that reason; the column list is verbatim, and the value order matches it one to one. [VERIFIED: Model\SalTDB.cs:19]
Note: the SQL `SALT.RTNINVNO` value comes from the `BillNo` variable, and `SALT.SORDNO` is not set by this method.

### Side effect inside SaveSalT: PRMAST.STOCK [VERIFIED: SR.cs:2507-2569]

After each successful `SALT` insert, when `ApprovalInventoryStatus` is true (the else branch of `!ApprovalInventoryStatus`):
- Compute `stock` in main-unit quantity.
  - Alias item (`Dgv_col_alcode > 0` and `Variables.mult_alias`): `Al_Qty × qty` (default `Al_Qty` = 1) (2523-2532).
  - Otherwise: if `PrMastDB.CheckMainUnit` is true, `stock = qty`. If not, use `PRMAST.CONVTYPE` and `UNITCONV`: type 0 → `qty × UNITCONV`. Type 1 → `qty / UNITCONV` (or `qty` if `UNITCONV` = 0). Any other type or no row → 0 (2538-2564).
- Then `UpdateStock(prcode, stock)` (2533, 2566).

`UpdateStock` SQL [VERIFIED: SR.cs:3404-3414 → Model\PrMastDB.cs:949-954]:

```sql
UPDATE PRMAST SET STOCK=STOCK+'<Stock>' WHERE PRCODE='<PrCode>'
```

The sign is positive, so a sales return increases `PRMAST.STOCK`. When `ApprovalInventoryStatus` is false, `PRMAST.STOCK` is not touched.
Note: in the alias branch `UpdateStock` is called with `Al_Qty × qty` only and does not apply the unit-conversion step. [VERIFIED: SR.cs:2532-2533]

Helper SQL used above [VERIFIED: Model\PrMastDB.cs:963-969, 983-988]:

```sql
SELECT * FROM PRMAST WHERE UNIT='<Unit>' AND PRCODE='<PrCode>'
SELECT UNIT,ALTUNIT,UNITCONV,CONVTYPE FROM PRMAST WHERE PRCODE='<PrCode>'
```

---

## 2. SaveSalD() — writes SALD [VERIFIED: SR.cs:2590-2791]

All rows are inserted through `SalDDB.insert`. There are three passes into the same `clsSalD` object, so values set in one pass carry into the next unless overwritten.

### Fields set once before the passes

| Column | Value | Source |
|---|---|---|
| VRTYPE | constant `"R"` | 2594 |
| VRNO | `VrNo` | 2595 |
| INVDATE | `dtp_date.Value.ToString(Variables.format)` | 2596 |
| CUSCODE | `cmb_party.SelectedValue` | 2597 |
| RETURNFLG | `true` | 2598 |
| VATFLG | `chk_vat.Checked` | 2599 |
| CURCONRATE / CURCODE | `CurrConvRate` / `CurrCode` | 2600-2601 |
| ORDNO | `clsSalM.OrdNo` | 2602 |
| BTYPE | payment mode 1 → `"C"`, 2 → `"R"`, 3 → `"D"`, 4 → `"B"` (SALT differs: it maps 4 to `"D"`) | 2603-2618 |
| BLCODE | `cmb_bank.SelectedValue` if payment mode is 3 or 4, else 0 | 2620-2627 |
| ADJCD | mode 1: `cmb_cash.SelectedValue` if > 0, else `Variables.CashCD`. Mode 4: `cmb_bank.SelectedValue` (0 if null). Others: 0 | 2628-2643 |
| VSVRTYPE | `clsSalM.VSVRTYPE` | 2644 |
| LOGCD | `Variables.UserId` (in Model) | `Model\SalDDB.cs:21` |
| ORGAMT, ORGTAXAMT (first two passes), CANCELFLG | not set here | NOT READ (`Controller\clsSalD.cs` defaults) |

### Pass 1 — tax rows from `dgv_vat_details` (2646-2678)

For each grid row with `Amt` > 0:
- ETYPE = `"T"`, DFLAG = `true`.
- CODE = `Variables.VATCode` if `chk_vat.Checked`, else the row's `Code`.
- AMOUNT = `ToBase(Amt)`. TAX = `TaxPerc` or 0. TAXAMT = `ToBase(Vat)`.
- SLCODE = `cmb_costc.SelectedValue` if `Variables.CostC` and `txt_multi_cosctc` = "NO", else 0.
- A failed insert breaks the loop.

### Pass 2 — footer rows from `dgv_add_info` (2680-2731)

Rows are in this order: 0 freight, 1 discount, 2 customs duty, 3 VAT, 4 round-off. [VERIFIED: SR.cs:4627-4639, columns by `drFy` codes `FREIGHTCD`, `DISCCD`, `CUSDUITYCD`, `VATCODE`, `COINADJCD`]
For each row with a non-zero `Col_value`, using `j` as the row position:
- ETYPE = `"T"` when j == 3 (VAT row), else `"A"` (2687-2690).
- CODE = `Desc_Col` cell (2691).
- AMOUNT is the base for the line: j == 2 → `ToBase(txt_gross_amt)`, j == 3 → `ToBase(txt_netAmt)`, others (freight, discount, round-off) → `ToBase(txt_total)` (2692-2697).
- TAX = `Math.Round(Col_perc, Variables.FourDecimalPoint)`. TAXAMT = `ToBase(Col_value)` (2700-2702).
- SLCODE as in SaveSalT (2703-2711).
- DFLAG = `false` if j > 4, else `true` (2712-2715). With 5 rows (j ≤ 4), DFLAG is always `true` in practice. [ASSUMPTION: no extra rows are added; row adds at SR.cs:4286, 4320 are not read]
- The VAT row (index 3) is NOT inserted here (`if (dr.Index != 3)`, 2716-2724). VAT goes in through pass 1 only.

### Pass 3 — account-wise product amounts (2733-2776)

Built from the sales-return grid: for each row with a value in column 0, `col_tax_acc` and `Dgv_col_amount`, the pair (account code, line amount) is collected. The amounts are grouped by account code.
For each distinct account code one row is inserted:
- ETYPE = `"D"`, CODE = the tax account (`col_tax_acc`), AMOUNT = `ToBase(sum of line amounts)`, TAX = 0, TAXAMT = 0, ORGTAXAMT = 0.
- DFLAG, BTYPE, ADJCD and the other fields are whatever the previous passes left in `clsSalD`. [VERIFIED: no reassignment between 2776 and the loop]
- `f = 1` after the first successful insert; a failed insert breaks with `f = 0`.

### Return value (2778-2783)
- True if the loop counters match their row counts and `Variables.VatFlg == false`.
- If `Variables.VatFlg == true`, it also needs `f == 1`, so at least one `"D"` row must have been written. With no tax-account rows the method returns false, which aborts the Save chain at 4079.

The ETYPE/DFLAG summary is therefore:

| Rows | ETYPE | DFLAG | Source |
|---|---|---|---|
| Tax detail rows (per tax code) | `T` | true | 2652-2653 |
| Freight (0), discount (1), customs duty (2), round-off (4) | `A` | true | 2687-2690, 2712-2715 |
| VAT footer row (3) | `T` in code but never inserted | n/a | 2716-2724 |
| Product amount per tax account | `D` | inherited from the last row written | 2747 |

[ASSUMPTION: the "charges" for a return are the same freight/customs/round-off rows as for a sales invoice; the form has no separate charges grid in this method.]

### Model SQL: `SalDDB.insert` [VERIFIED: Model\SalDDB.cs:17-22]

```sql
INSERT INTO SALD (VRTYPE,VRNO,INVDATE,CUSCODE,ETYPE,CODE,AMOUNT,TAX,BTYPE,LOGCD,TAXAMT,RETURNFLG,SLCODE,VATFLG,CURCONRATE,CURCODE,ORDNO,DFLAG,ORGAMT,ORGTAXAMT,CANCELFLG,BLCODE,ADJCD,VSVRTYPE)
VALUES('<VrType>','<VrNo>','<InvDate>','<CusCode>','<EType>','<Code>','<Amount>','<Tax>','<BType>','<Variables.UserId>','<TaxAmt>','<ReturnFlg>','<SlCode>','<VatFlg>','<CurConRate>','<CurCode>','<OrdNo>','<DFlag>','<OrgAmt>','<OrgTaxAmt>','<CancelFlg>','<BlCode>','<AdjCd>',<VSVRTYPE>)
```

(String-concatenated, same convention as above.) Delete used in edit mode [VERIFIED: Model\SalDDB.cs:31-37]:

```sql
DELETE FROM SALD WHERE VRNO='<VrNo>'
```

Note: this delete filters on `VRNO` only, not `VRTYPE`. See section 9, item 1.

---

## 3. SaveStkDt(TrnNo) — writes STKDT or STKDTPHY [VERIFIED: SR.cs:2793-2947]

Called as `SaveStkDt(clsSalM.OrdNo)`, so `TrnNo = clsSalM.OrdNo` (SR.cs:4082).

### Values for a RETURN

| Column | Value | Source |
|---|---|---|
| **VRTYPE** | constant **`"RET"`** | 2797 |
| **TYPE** | constant **`"R"`** | 2799 |
| TRNNO | `TrnNo` (the sales-return order number) | 2800 |
| VRDATE | `dtp_date.Value.ToString(Variables.format)` | 2798 |
| PARTYCD | `cmb_party.SelectedValue` | 2801 |
| VSVRTYPE | `clsSalM.VSVRTYPE` | 2802 |
| ORDNO, NTNO | constant 0 | 2819, 2824 |
| PRCODE | grid `Dgv_col_prcode` | 2818 |
| QTY | grid `Dgv_col_qty` (or 0). Entered qty, not converted | 2820-2823 |
| RATE | grid `Dgv_col_price`; `ToBaseActual` if `INVAMTEDIT_SI`, else `ToBase`. Note the guard tests `dgr.Cells["Dgv_col_price"].ToString()` (the cell object, not `.Value`), which is never empty | 2826-2836 |
| UNITCODE | grid `Dgv_col_unit_code`, or 0 | 2838-2841 |
| CONVTYPE | grid `col_conv_type` if present, else carried from the previous row | 2850-2851 |
| CONVFACT | grid `col_conv_fact` if non-zero, else 1 | 2852-2855 |
| CONVQTY | same rule as SALT, except the else-branch first sets `qty` then overwrites with `qty × Al_Qty` when present | 2856-2889 |
| PRFLG | `true` if `col_price_flg` = "Y" | 2890-2893 |
| CONVRATE | grid `col_conv_rate` via `ToBaseActual`, else grid `Dgv_col_price` via `ToBaseActual` | 2896-2909 |
| GDWCODE | grid `Dgv_col_godown`, else `Functions.GetGodownByBranch()` | 2911-2914 |
| UTIME | existing UTIME of the old rows when editing, now when new. `uType` = `"S"` for Save, `"U"` for Update | 2804-2808; Model\StkDtDB.cs:16 |
| LOGCD, LUTIME | `Variables.UserId`, `DateTime.Now` | Model\StkDtDB.cs:16 |
| ADDCOST, CONVADDCOST, CLOSES, PRV, PRVN, PRVT, SNTSLNO, LM, row-id columns, NTRNO | not set | NOT READ (`clsStkDt` defaults) |

Which table: `StkDtDB.insert` (table `STKDT`) when `ApprovalInventoryStatus` is true; otherwise `StkDtDB.phy_insert` (table `STKDTPHY`) (2918).
Existing rows are always deleted from both tables first (2810-2811). The UTIME is read from `STKDT` when `ApprovalStockUpdate` is true, else from `STKDTPHY` (2804).
On failure of any row, both tables are cleaned again for this voucher and the method returns false (2934-2939).

### Model SQL

`StkDtDB.insert` [VERIFIED: Model\StkDtDB.cs:12-17]:

```sql
INSERT INTO STKDT (VRTYPE,PRCODE,VRDATE,ORDNO,NTNO,TRNNO,QTY,TYPE,PARTYCD,LOGCD,UTIME,RATE,UNITCODE,CONVTYPE,CONVFACT,CONVQTY,PRFLG,CONVRATE,GDWCODE,LUTIME,VSVRTYPE,PRV,PRVN,PRVT,SNTSLNO,LM,ADDCOST,CONVADDCOST,CLOSES,ORDROWID,NTROWID,TRNROWID,QTNCODE,QTNROWID,PRVQ,PRVQY,PRVY,PRVNY,PRVTY,NTRNO,NTRROWID)
VALUES('<VrType>','<PrCode>','<VrDate>','<OrdNo>','<NtNo>','<TrnNo>','<Qty>','<Type>','<PartyCd>','<Variables.UserId>','<(uType=="" || uType=="S") ? DateTime.Now : uTimes>','<Rate>','<UnitCode>','<ConvType>','<ConvFact>','<ConvQty>','<Prflg>','<ConvRate>','<GdwCode>','<DateTime.Now>',<VSVRTYPE>,'<PRV>','<PRVN>','<PRVT>','<SNTSLNO>','<LM>','<addCost>','<convAddCost>','<CLOSES>','<ORDROWID>','<NTROWID>','<TRNROWID>','<QTNCODE>','<QTNROWID>','<PRVQ>','<PRVQY>','<PRVY>','<PRVNY>','<PRVTY>','<NTRNO>','<NTRROWID>')
```

`StkDtDB.phy_insert` [VERIFIED: Model\StkDtDB.cs:8927-8933]: identical column list and values, target table `STKDTPHY`.

Delete for the RET voucher type [VERIFIED: Model\StkDtDB.cs:54-55, 9001-9002]:

```sql
DELETE FROM STKDT WHERE TRNNO='<TrnNo>' AND TYPE='<Type>' AND VRTYPE='<VrType>'
DELETE FROM STKDTPHY WHERE TRNNO='<TrnNo>' AND TYPE='<Type>' AND VRTYPE='<VrType>'
```

UTIME lookup for the RET voucher type [VERIFIED: Model\StkDtDB.cs:105-106, 8950-8951]:

```sql
SELECT UTIME FROM STKDT WHERE TRNNO='<TrnNo>' AND TYPE='<Type>' AND VRTYPE='<VrType>'
SELECT UTIME FROM STKDTPHY WHERE TRNNO='<TrnNo>' AND TYPE='<Type>' AND VRTYPE='<VrType>'
```

Differences from the purchase path: the Purchase form uses VRTYPE `"TR"` TYPE `"R"` (earlier review). A sales return uses VRTYPE `"RET"` TYPE `"R"`. `"RET"` has its own branch in `DeleteByVr`, `GetUTime` and their `phy_` versions, so those helpers handle it correctly.

---

## 4. ClearStock(Type) — reverses / validates stock [VERIFIED: SR.cs:3416-3568]

Behaviour:
- Returns true at once when `ApprovalStockUpdate` is false (3418-3419). Nothing is written in that case.
- It loops over `dtSalT` (the rows of the voucher as loaded when the form was opened for edit; its fill is NOT READ), not the current grid.
- `Type` values seen in callers: `"Cancel"` (SR.cs:4037, 10901), `"ValidateUpdate"` (SR.cs:1831), `"ValidateCancel"` (SR.cs:10885). `ValidateBatchGotoNegetive` and `CalcPricefromBatch` are NOT READ.

Per old row:
1. Quantity in the item's base unit:
   - If the row has an alias code (`ALCODE` non-empty and ≠ "0"): look up the alias (`PrAliasDB.SearchByAlCode`, falling back to `SearchByPrCodeAndName`). If found, `qty = QTY × alias.QTY` and the unit is the alias unit. If not found, `qty` stays 0 (3435-3457). Alias SQL is NOT READ.
   - Otherwise `qty = QTY` (3459).
2. `stock = -1 × qty` if `PrMastDB.CheckMainUnit` is true. Otherwise use `PRMAST.CONVTYPE`/`UNITCONV`: type 0 → `-qty × UNITCONV`. Type 1 → `-qty / UNITCONV` (or `-qty` if `UNITCONV` = 0). Any other type → stock stays 0 (3460-3482).
3. Type `"Cancel"` only: `PrMastDB.UpdateStock` with that negative value, and the row is counted only if the update returns > 0. For every other Type the row is counted without a write (3484-3492).

   ```sql
   UPDATE PRMAST SET STOCK=STOCK+'<Stock>' WHERE PRCODE='<PrCode>'
   ```

   [VERIFIED: Model\PrMastDB.cs:949-954] The negative stock undoes the positive stock added by `SaveSalT`.
4. If the old row has a batch (3494):
   - Loads item master and unit data via `PrMastDB.SearchByCode` (SQL NOT READ).
   - Fills `clsPrst`: batch, `BalQty = stock` (negative), `Mrp` = row RATE, `PRate` = row PRRATE, `Rate` = row MRP, `GRack` = row GDWCODE, dates (3507-3520).
   - Reads `MFG_YN`/`EXP_YN` from `dtPrMfgExpDetails` (3521-3527).
   - Type `"Cancel"`:
     - If `BatchDetDB.GetBatchForValidation` returns rows (other vouchers hold this batch), run `PrstDB.updateByRack`; else run `PrstDB.deleteByRack` (3529-3540).
   - Any other type: it reads `PrstDB.GetAvailableStock` and returns **false** when `BalQty - PrevQty + EnterQty < 0`, where `PrevQty = |stock|` (3544-3553).

SQL [VERIFIED: Model\BatchDetDB.cs:105-109, Model\PrstDB.cs:3071-3092, 3156-3170, 2998-3030]:

```sql
-- BatchDetDB.GetBatchForValidation
SELECT * FROM <Functions.VRSYS_ALLOWBR_BATCHDET(DefBranch: true)>BATCHDET WHERE BATCH='<Batch>' AND PRCODE=<PrCode> AND (STYPE<>'<SType>' OR VRNO<>'<VrNo>') AND GRACK=<GRack>

-- PrstDB.updateByRack (BatchRtEdit = true)
UPDATE PRST SET BALQTY=BALQTY+'<BalQty>' WHERE BATCH='<Batch>' AND PRCODE='<PrCode>' AND GRACK='<GRack>'<DateStmnt><BrStmnt>

-- PrstDB.updateByRack (BatchRtEdit = false)
UPDATE PRST SET BALQTY=BALQTY+'<BalQty>' WHERE BATCH='<Batch>' AND PRCODE='<PrCode>' AND GRACK='<GRack>' AND CONVMRP='<Convmrp>' AND CONVRATE='<ConvRate>'<DateStmnt><BrStmnt>

-- PrstDB.deleteByRack (BatchRtEdit = true)
DELETE FROM PRST WHERE BATCH='<Batch>' AND PRCODE='<PrCode>' AND GRACK='<GRack>'<DateStmnt><BrStmnt>

-- PrstDB.deleteByRack (BatchRtEdit = false)
DELETE FROM PRST WHERE BATCH='<Batch>' AND PRCODE='<PrCode>' AND GRACK='<GRack>' AND CONVMRP='<Convmrp>' AND CONVRATE='<ConvRate>'<DateStmnt><BrStmnt>

-- PrstDB.GetAvailableStock (BatchRtEdit = true)
SELECT SUM(BALQTY) AS BALQTY FROM PRST WHERE PRCODE='<PrCode>' AND BATCH='<Batch>' AND GRACK='<GRack>'<DateStmnt><BrStmnt>

-- PrstDB.GetAvailableStock (BatchRtEdit = false)
SELECT SUM(BALQTY) AS BALQTY FROM PRST WHERE PRCODE='<PrCode>' AND BATCH='<Batch>' AND GRACK='<GRack>' AND CONVMRP = '<Convmrp>' AND CONVRATE = '<ConvRate>'<DateStmnt><BrStmnt>
```

Where (identical in all of the above, [VERIFIED: Model\PrstDB.cs:3001-3002, 3073-3081]):
- `DateStmnt` = `(MFG_YN ? " AND MFGDATE='<MfgDate>'" : "") + (EXP_YN ? " AND EXPIRY='<Expiry>'" : "")`
- `BrStmnt` = `Variables.BranchYN ? " AND IIF(BRCD IS NULL,0,BRCD)=<branch>" : ""`

The branch code is `Variables.VRSYS_DEFBRANCH` in the stock lookup (GetAvailableStock) and `clsPrst.BrCd` (defaulted to that value) in update/delete.

Observed behaviour worth recording:
- Its stock sign is always negative because it is the reverse of a **sales return**, which adds stock. For a Sales *Invoice* the same code would be opposite; here it is correct. [ASSUMPTION: the sibling Sales form uses the mirror logic; not read in this task]
- A batch-bearing return that has been consumed elsewhere (`GetBatchForValidation` has rows) is only decremented, not deleted, in PRST.

---

## 5. SavePrSt() and SaveBatchDet()

### 5a. SavePrSt() — writes PRST [VERIFIED: SR.cs:2969-3151]

Runs only when `Variables.batch` and `ApprovalInventoryStatus` (SR.cs:4084-4087). Only grid rows with a non-empty `Batch` and a non-zero qty are written; other rows are counted as done.

| Column | Value | Source |
|---|---|---|
| STYPE | constant `"SR"` | 2973 |
| VRNO | `VrNo` | 2974 |
| PURDATE | `dtp_date.Text` parsed as date | 2975 |
| BILLNO | `txt_inv_no.Text` or `""` | 2976-2979 |
| BILLDATE | `dtp_date.Value.Date` | 2980 |
| SUPCODE | `cmb_party.SelectedValue` (the customer, in the supplier column) | 2981 |
| PRCODE | grid `Dgv_col_prcode` | 2990 |
| QTY | grid qty if the line unit ≠ the item's alternate unit, else 0 | 3000-3008 |
| ALTQTY | grid qty if the line unit = the alternate unit, else 0 | 3009-3018 |
| BATCH | grid `Batch` | 3019-3022 |
| EXPIRY | grid `col_expiry`, else `Variables.fyStartDate` | 3023-3026 |
| MFGDATE | grid `col_mfgdate`, else `Variables.fyStartDate` | 3082-3085 |
| PRATE | grid `col_prRate` via `ToBase`, or 0 | 3028-3031 |
| RATE | grid `col_mrp` via `ToBase`, or 0 | 3033-3036 |
| MRP | grid `Dgv_col_price` via `ToBase`, or 0 | 3038-3041 |
| DISC, TAX, TAXCODE | 0 | 3043-3045 |
| BALQTY | main-unit quantity: `Qty + AltQty × UNITCONV` (CONVTYPE 0), `Qty + AltQty / UNITCONV` (CONVTYPE 1), else `Qty + AltQty`; falls back to `Qty + AltQty` if no `PRMAST` row or `UNITCONV` ≤ 0 | 3048-3075 |
| EXPCHECK | `true` if `col_expiry` present | 3077-3080 |
| CONVPRATE | grid `col_convprate` via `ToBaseActual`, else `PRate` | 3088-3091 |
| CONVMRP | grid `col_convSalRate` via `ToBaseActual`, else `Mrp` | 3093-3096 |
| CONVRATE | grid `col_convmrp` via `ToBaseActual`, else `Rate` | 3098-3101 |
| GRACK | grid `Dgv_col_godown` | 3103 |
| BRCD | `Variables.VRSYS_DEFBRANCH` if `BranchYN` and 0 | Model\PrstDB.cs:18-19 |
| LOGCD | `Variables.UserId` | Model\PrstDB.cs:22 |

Upsert logic [VERIFIED: SR.cs:3118-3134]: try `PrstDB.updateByRack(clsPrst, MFG_YN, EXP_YN)` first (adds BALQTY to the existing batch row). If it updates nothing, run `PrstDB.insert`. If that also fails, `PrstDB.DeleteByVrNo` and stop.
`MFG_YN` / `EXP_YN` come from `dtPrMfgExpDetails` (`MANDATEFLG`, `EXPDATEFLG`), filled elsewhere (NOT READ) (3105-3115).

SQL:

```sql
-- PrstDB.insert  [Model\PrstDB.cs:16-27]
INSERT INTO PRST (STYPE,VRNO,PURDATE,BILLNO,BILLDATE,SUPCODE,PRCODE,QTY,BATCH,EXPIRY,PRATE,RATE,DISC,TAX,TAXCODE,BALQTY,EXPCHECK,LOGCD,MRP,MFGDATE,ALTQTY,GRACK,CONVRATE,BRCD,CONVPRATE,CONVMRP)
VALUES ('<SType>','<VrNo>','<PurDate.Date>','<BillNo>','<BillDate.Date>','<SupCode>','<PrCode>','<Qty>','<Batch>','<Expiry>','<PRate>','<Rate>','<Disc>','<Tax>','<TaxCode>','<BalQty>','<ExpCheck>','<Variables.UserId>','<Mrp>','<MfgDate>','<AltQty>','<GRack>','<ConvRate>',<BrCd>,'<ConvpRate>','<Convmrp>')

-- PrstDB.updateByRack: see section 4 (same two statements)

-- PrstDB.DeleteByVrNo  [Model\PrstDB.cs:33-40]
DELETE FROM PRST WHERE VRNO='<VrNo>'<(Variables.BranchYN ? " AND IIF(BRCD IS NULL,0,BRCD)=" + Variables.VRSYS_DEFBRANCH : "")>
```

Note the PRST failure cleanup deletes by `VRNO` alone (no STYPE), so it can remove other voucher types' PRST rows with the same number. See section 9.

### 5b. SaveBatchDet() — writes BATCHDET [VERIFIED: SR.cs:3153-3317]

Runs whenever `Variables.batch` is true, regardless of `ApprovalInventoryStatus` (SR.cs:4088). Same row filter as SavePrSt.

| Column | Value | Source |
|---|---|---|
| STYPE | `"SR"` | 3157 |
| VRNO | `VrNo` | 3158 |
| PURDATE | `dtp_date.Text` as date | 3159 |
| BILLNO | `txt_inv_no.Text` or `""` | 3160-3163 |
| BILLDATE | `dtp_date.Value.Date` | 3164 |
| SUPCODE | `cmb_party.SelectedValue` | 3165 |
| VSVRTYPE | `clsSalM.VSVRTYPE` | 3166 |
| PRCODE | grid `Dgv_col_prcode` | 3175 |
| QTY / ALTQTY | as SavePrSt (QTY when unit ≠ alt unit; ALTQTY when unit = alt unit) | 3185-3203 |
| BATCH | grid `Batch` | 3205 |
| EXPIRY / MFGDATE | grid values, else `Variables.fyStartDate` | 3206-3209, 3235-3238 |
| MRP | grid `Dgv_col_price` via `ToBase` | 3211-3214 |
| RATE | grid `col_mrp` via `ToBase` | 3216-3219 |
| PRATE | grid `col_prRate` via `ToBase` | 3221-3224 |
| DISC, TAX, TAXCODE | 0 | 3226-3228 |
| EXPCHECK | `true` if `col_expiry` present | 3230-3233 |
| CONVMRP | grid `col_conv_rate` via `ToBase`, else `Mrp` | 3240-3243 |
| CONVRATE | grid `col_convmrp` via `ToBase`, else `Rate` | 3245-3248 |
| CONVPRATE | grid `col_convprate` via `ToBase`, else `PRate` | 3250-3253 |
| GRACK | grid `Dgv_col_godown` | 3255 |
| **STKFLG** | constant **`"R"`** | 3256 |
| CONVQTY | same rule as SaveSalT | 3258-3291 |
| LOGCD | `Variables.UserId` | Model\BatchDetDB.cs:21 |

Inconsistency: `SavePrSt` uses `ToBaseActual` for the conv rates, `SaveBatchDet` uses `ToBase` for the same columns. [VERIFIED: SR.cs:3089-3099 vs 3241-3251]

SQL [VERIFIED: Model\BatchDetDB.cs:16-21, 30-36]:

```sql
INSERT INTO BATCHDET (STYPE,VRNO,PURDATE,BILLNO,BILLDATE,SUPCODE,PRCODE,QTY,BATCH,EXPIRY,PRATE,RATE,DISC,TAX,TAXCODE,EXPCHECK,LOGCD,MRP,MFGDATE,ALTQTY,GRACK,CONVRATE,STKFLG,CONVPRATE,CONVMRP,CONVQTY,VSVRTYPE)
VALUES ('<SType>','<VrNo>','<PurDate.Date, Variables.format>','<BillNo>','<BillDate.Date, Variables.format>','<SupCode>','<PrCode>','<Qty>','<Batch>','<Expiry, Variables.format>','<PRate>','<Rate>','<Disc>','<Tax>','<TaxCode>','<ExpCheck>','<Variables.UserId>','<Mrp>','<MfgDate, Variables.format>','<AltQty>','<GRack>','<ConvRate>','<StkFlg>','<ConvpRate>','<Convmrp>','<ConvQty>',<VSVRTYPE>)

DELETE FROM BATCHDET WHERE VRNO='<VrNo>' AND STYPE='<SType>' 
```

---

## 6. SaveSntStk() — writes SNTSTK / SNTSTKPHY [VERIFIED: SR.cs:3318-3403]

Runs only when `Variables.SlNoTrack` (SR.cs:4091).

Flow:
1. Set `INVRTYPE = OUTVRTYPE = "SR"`, `INVRNO = OUTVRNO = VrNo`, `INORDNO = OUTORDNO = clsSalM.OrdNo` (3322-3324).
2. Delete existing rows from both tables: `SntStkDB.DelteByVoucherIn` and `SntStkDB.Phy_DeleteByVoucher` (3325-3326).
3. Blank the OUT-side fields: `OUTVRTYPE=""`, `OUTVRNO=0`, `OUTORDNO=0`, `OUTSLNO=0`, `USED=false` (3328-3332).
4. For each grid row with `PRCODE > 0`:
   - `INSLNO` = `col_slno`, `PRCODE`, `GDWCODE` (grid godown else `Functions.GetGodownByBranch()`), `UNITCODE` (3339-3342).
   - `_QTY` = `Convert.ToInt32(qty)`, the whole units only (3344).
   - Look up the serials entered for this item and line in the in-memory table `dtSNT` (`PRCODE`, `INSLNO` filter). For each serial (up to `_QTY`), set `ITEMSLNO` and reset OUT fields (3345-3358). The fill of `dtSNT` is NOT READ.
   - If `dtSNT_Used` has a row for the same product, line, godown, unit and serial, copy its `OUTVRTYPE`, `OUTVRNO`, `OUTORDNO`, `OUTSLNO`, `USED` onto the row. This preserves the "already sold" state across an edit (3359-3370).
   - Insert into `SNTSTK` when `ApprovalInventoryStatus`, else into `SNTSTKPHY` (3372-3375).
5. Returns true if all rows were counted. Note `i++` occurs only inside `if (dtSNT.Rows.Count > 0)` for valid rows, so a return with serial tracking on but an empty `dtSNT` returns false (3382, 3391). The caller ignores the return value.

SQL [VERIFIED: Model\SntStkDB.cs:12-23, 210-217, 661-664, 666-676]:

```sql
INSERT INTO SNTSTK (INVRTYPE, INVRNO, INORDNO, INSLNO, OUTVRTYPE, OUTVRNO, OUTORDNO, OUTSLNO, PRCODE, GDWCODE, UNITCODE, ITEMSLNO, USED, BRCD)
VALUES ('<INVRTYPE>', <INVRNO>, <INORDNO>, <INSLNO>, '<OUTVRTYPE>', <OUTVRNO>, <OUTORDNO>, <OUTSLNO>, <PRCODE>, <GDWCODE>, <UNITCODE>, '<ITEMSLNO>', '<USED>', <BRCD>)

INSERT INTO SNTSTKPHY (INVRTYPE, INVRNO, INORDNO, INSLNO, OUTVRTYPE, OUTVRNO, OUTORDNO, OUTSLNO, PRCODE, GDWCODE, UNITCODE, ITEMSLNO, USED, BRCD)
VALUES ('<INVRTYPE>', <INVRNO>, <INORDNO>, <INSLNO>, '<OUTVRTYPE>', <OUTVRNO>, <OUTORDNO>, <OUTSLNO>, <PRCODE>, <GDWCODE>, <UNITCODE>, '<ITEMSLNO>', '<USED>', <BRCD>)

DELETE FROM SNTSTK WHERE INVRTYPE = '<INVRTYPE>' AND INORDNO = <INORDNO> AND INVRNO = <INVRNO>

DELETE FROM SNTSTKPHY WHERE (INVRTYPE = '<INVRTYPE>' AND INVRNO = <INVRNO><(OUTVRTYPE != "ST" ? " AND INORDNO = " + INORDNO : "")>) OR (OUTVRTYPE = '<OUTVRTYPE>' AND OUTVRNO = <OUTVRNO><(OUTVRTYPE != "ST" ? " AND OUTORDNO = " + OUTORDNO : "")>)
```

`BRCD` = `Variables.VRSYS_DEFBRANCH` when `BranchYN` and BRCD is 0 (Model\SntStkDB.cs:14-15).
Note: `DelteByVoucherIn` is called at 3325 while `OUTVRTYPE = "SR"`, so the SNTSTKPHY delete includes the `OR (OUTVRTYPE='SR' AND OUTVRNO=<VrNo> AND OUTORDNO=<OrdNo>)` clause.

---

## 7. SavePayment(...) and SaveAdjustment() — how cash / credit / bank returns differ

### 7a. What decides the path [VERIFIED: SR.cs:4094-4163]

The payment mode is `cmb_payment.SelectedValue`. Codes seen: 1, 2, 3, 4. The code-to-name map comes from `PayModeDB.ActiveModes()` (SR.cs:1261), which was NOT READ.
The only mapping the Save code establishes is:

| Mode | SALT.BTYPE | SALD.BTYPE | SALD.ADJCD | SALD.BLCODE | Source |
|---|---|---|---|---|---|
| 1 | `C` | `C` | `cmb_cash` selection if > 0, else `Variables.CashCD` | 0 | SR.cs:2311-2314, 2603-2606, 2628-2634, 2620-2627 |
| 2 | `R` | `R` | 0 | 0 | SR.cs:2315-2318, 2607-2610, 2642-2643 |
| 3 | `D` | `D` | 0 | `cmb_bank` | SR.cs:2319-2320, 2611-2614, 2620-2623 |
| 4 | `D` | `B` | `cmb_bank` selection (0 if null) | `cmb_bank` | SR.cs:2319-2320, 2615-2618, 2635-2641 |

[ASSUMPTION: 1 = Cash, 2 = Credit, 3 = Cheque/other bank mode, 4 = Bank, inferred from the `C`/`R`/`D`/`B` letters. Mode names are NOT READ.]

### 7b. Cash (1), bank (3, 4)

- No `YVR` rows and no `BILLBYBILL` rows are written for modes 1, 3 and 4 by the Save handler. The only accounting trail is the `SALD` rows (BTYPE, ADJCD, BLCODE) from section 2. [VERIFIED: the YVR block at SR.cs:4095 is guarded by `cmb_payment.SelectedValue == 2`]
- The part-payment `else` branch (4115-4132) still runs for these modes: if `YvrNo != 0` (an earlier save created a payment voucher), it cancels that voucher with `YvrDB.Cancel` and deletes its `BILLBYBILL` rows. Otherwise nothing happens.

### 7c. Credit (2)

Credit is the only mode that writes `YVR` and `BILLBYBILL` through these two methods.

1. Condition: mode == 2 **and** `txt_cash_paid.Text` is non-empty and > 0 (the "cash paid back" amount) (4095).
2. `clsYvr.VrType = "PV"`, `clsYvr.VrNo = YvrNo`, then `YvrDB.Delete(clsYvr)` clears any previous voucher with that number (4097-4099).
3. `SavePayment(Variables.CashCD, paid, 0, false, party)` writes the **cash leg**: `CODE = CashCD`, `RECEIPT = paid`, `PAYMENT = 0`, `ADJCD = party`, `CDTYPE = "C"` (4100).
4. If that succeeds, `SavePayment(party, 0, paid, false, Variables.CashCD)` writes the **party leg**: `CODE = party`, `RECEIPT = 0`, `PAYMENT = paid`, `ADJCD = CashCD`, `CDTYPE = "D"` (4102).
5. If both succeed, `SaveAdjustment()` writes one `BILLBYBILL` row (4104). Then `UpdateVoucherSystemOrdNo("PV")` and `FyDetailsDB.UpdateVoucherNo("PV")` run, and `IsNewNbrGnrtdRP = false` (4106-4108). Their bodies are NOT READ.
6. If the first `SavePayment` fails, `FyDetailDB.UpdateVoucherNo("PV")` runs instead (4110-4113).
7. The no-payment case for mode 2 (cash paid = 0 or empty) falls to the 7b `else` branch: an existing voucher is cancelled.
8. After that, for mode 2 only, the `BillAdjustment` dialog (`ba.ShowDialog()`) opens for the credit balance (`txt_bal_amt`), with `FormType = "RET"`, `GenType = "SRB"`, `sType = "SalesReturn"`, `type = "C"`. It first deletes `BILLBYBILL` rows with TYPECODE = OrdNo, STYPE = `SalesReturn`, GENTYPE = `SRB` (4136-4163). The dialog's own writes are NOT READ.

#### SavePayment values [VERIFIED: SR.cs:12159-12230]

| Column | Value | Source |
|---|---|---|
| VRDATE | `dtp_date.Value.ToString(Variables.format)` | 12163 |
| CODE | argument `code` | 12164 |
| RECEIPT / PAYMENT | `ToBase(receipt)` / `ToBase(payment)` | 12166-12167 |
| DESCRIPTION | `"Against SalesReturn Invoice:" + clsSalM.OrdNo` | 12169 |
| SLCODE / STYPE | `cmb_costc.SelectedValue` and `"S"` if `cmb_costc.Enabled` and > 0, else 0 and `"G"` | 12170-12179 |
| CHQNO, BANK, INSTCODE, REFNO, VATNO | `""` | 12181-12184, 12189, 12216 |
| CHQDATE | `DateTime.Now` formatted | 12183 |
| PARTY | `cmb_party.Text` | 12186 |
| CANCELLED | false | 12187 |
| DIRECT | true | 12188 |
| CUSCODE | `cmb_party.SelectedValue` | 12190 |
| ORDNO | `YvrNo` | 12192 |
| NARRATION | `""` (becomes `"Adjustment"` and CASHFLG `"F"` when `addFlg`; both callers pass false) | 12193, 12206-12214 |
| PDCTYPE | `"R"` | 12195 |
| CDTYPE | `"C"` if `receipt > 0` else `"D"` | 12197-12200 |
| CASHFLG | `"T"` | 12202 |
| CURCONRATE / CURCODE | `CurrConvRate` / `CurrCode` | 12204-12205 |
| ADJCD | argument `Adjcd` | 12215 |
| YVRTYPE | `"SR"` | 12217 |
| VATDATE | document date | 12218 |
| INVNOTEXT | `VrInvNoText("Payment")` (body NOT READ) | 12219 |
| APRVLST | `"H"` if `ApprovalLevel > 0 && ApprovalStatus == "P" && !ApprovalValueStatus`, else `""` | 12220 |
| VRTYPE / VRNO | set by the caller to `"PV"` / `YvrNo` before the call, not inside this method | SR.cs:4097-4098 |
| LOGCD | not set by this method (see the `Delete` note below); `ULOGCD` = `Variables.UserId` | Model\YvrDB.cs:21 |

On exception it shows a `MessageBox` and returns 0 (12223-12229).

SQL [VERIFIED: Model\YvrDB.cs:12-23, 36-70, 75-155]:

```sql
INSERT INTO YVR(VRNO,VRDATE,CODE,SLCODE,CHQNO,RECEIPT,PAYMENT,DESCRIPTION,VRTYPE,STYPE,PARTY,BANK,LOGCD,CANCELLED,DIRECT,UTIME,REFNO,CUSCODE,CHQDATE,ORDNO,NARRATION,PDCTYPE,INSTCODE,CDTYPE,CASHFLG,CURCONRATE,CURCODE,ORGREC,ORGPAY,CANCELFLG,ADJCD,VATNO,YVRTYPE,INVNOTEXT,VATDATE,PURCODE,VSVRTYPE,VSDOCNO,VSORDCD,PDCDT,SMCD,ADJFLG,ADJAMT,CTIME,ULOGCD<,APRVLABY,APRVLADT,APRVLBBY,APRVLBDT,APRVLST,APRVLRBY,APRVLRDT,APRVLJBY,APRVLJDT>,ADJSLNO)
OUTPUT INSERTED.ID
VALUES('<VrNo>','<VrDate>','<Code>','<SlCode>','<ChqNo>','<Receipt>','<Payment>','<Desc>','<VrType>','<SType>','<Party>','<Bank>','<LogCd>','<Cancelled>','<Direct>','<now>','<RefNo>','<CusCode>','<ChqDate>','<OrdNo>','<Narration>','<PdcType>','<InstCode>','<CdType>','<CashFlg>','<CurConRate>','<CurCode>','<OrgRec>','<OrgPay>','<CancelFlg>','<AdjCd>','<vatNo>','<YvrType>','<InvNoText>','<VatDate>',<PURCODE>,'<VSVRTYPE>','<VSDOCNO>','<VSORDCD>','<PdcDT>','<SMCD>','<ADJFLG>','<ADJAMT>','<CTIME>','<Variables.UserId>'<,approval values or NULL>,'<ADJSLNO>')

UPDATE YVR SET ORGREC=RECEIPT,ORGPAY=PAYMENT,RECEIPT='0',PAYMENT='0',CANCELFLG='True' WHERE VRNO='<VrNo>' AND VRTYPE='<VrType>'

DELETE FROM YVR WHERE VRNO='<VrNo>' AND VRTYPE='<VrType>'
```

The insert is executed through `db.InsertScalar` and returns the new `ID`. The approval columns (`APRVLABY` and so on) are `NULL` when the corresponding `clsYvr` field is empty (Model\YvrDB.cs:14-24).
Note that `SavePayment` leaves `clsYvr.LogCd`, so the inserted `LOGCD` is whatever `clsYvr` still holds (for example from the last `YvrDB.Delete`, which preloads `LogCd` and `CTIME` from the old row, Model\YvrDB.cs:112-121). `CTIME` is also not set in `SavePayment`.

#### SaveAdjustment values — `BILLBYBILL` [VERIFIED: SR.cs:12232-12276]

| Column | Value | Source |
|---|---|---|
| METHOD | `"Adjustment"` | 12236 |
| REFNO | `clsSalM.OrdNo` as text | 12240 |
| ACCID | `cmb_party.SelectedValue` | 12242 |
| CREDIT | `ToBase(txt_pay_amt)` or 0 ("to store total amount") | 12244-12247 |
| DEBIT | `ToBase(txt_cash_paid)` or 0 | 12248-12251 |
| CDTYPE | `"D"` | 12252 |
| DUEDATE / VRDATE | `dtp_date.Value.Date` | 12253, 12261 |
| CLOSED | false | 12254 |
| VRTYPE | `"SalesReturn"` | 12255 |
| ORDERNO | 0 | 12256 |
| STYPE | `"Payment"` | 12257 |
| TYPECODE | `clsYvr.OrdNo` as text (equals `YvrNo`) | 12258 |
| GENTYPE | `"SR"` | 12259 |
| VSVRTYPE | `clsYvr.VSVRTYPE` | 12262 |
| ACTUALVRTYPE, ACTUALVRNO, ADJSLNO | not set here | NOT READ (class defaults) |

Before the insert it deletes the matching rows (12260):

```sql
DELETE FROM BILLBYBILL WHERE TYPECODE = '<TypeCode>' AND STYPE = '<Stype>' AND GENTYPE='<GenType>'
```

```sql
INSERT INTO BILLBYBILL (METHOD,REFNO,ACCID,CREDIT,DUEDATE,CLOSED,DEBIT,VRTYPE,CDTYPE,STYPE,TYPECODE,ORDERNO,VRDATE,GENTYPE,VSVRTYPE,ACTUALVRTYPE,ACTUALVRNO,ADJSLNO) OUTPUT INSERTED.ID
VALUES('<Method>','<RefNo>','<AccId>','<Credit>','<DueDate>','<Closed>','<Debit>','<VrType>','<Cdtype>','<Stype>','<TypeCode>','<OrderNo>','<VrDate>','<GenType>',<VSVRTYPE>,'<ActualVrType>',<ActualVrNo>,'<ADJSLNO>')
```

[VERIFIED: Model\BillByBillDB.cs:16-21, 417-423]

Summary of the three modes:

| | Cash | Credit | Bank |
|---|---|---|---|
| SALT/SALD BTYPE | C / C | R / R | D / D (mode 3) or D / B (mode 4) |
| SALD ADJCD | cash account | 0 | bank (mode 4) or 0 (mode 3) |
| YVR rows | none | 2 rows (cash leg, party leg) if cash-paid > 0 | none |
| BILLBYBILL via SaveAdjustment | none | 1 row if both YVR rows succeed | none |
| `BillAdjustment` dialog | no | yes | no |

---

## 8. SaveEinv() — writes EINV [VERIFIED: SR.cs:13973-14016]

- Table written: `EINV` through `EInvDB.insert` (Model\EInvDB.cs:13-18).
- When it runs: only if `EinvID != ""` (13977). The `clsEinv` fields are copied from form-level e-invoice variables (`EinvAPIResp`, `EinvMode`, `CSR`, `PrivateKey`, `PublicKey`, `SecretKey`, `Invwarning`, `SignedXML`, `InvHash`, `UUID`, `PIH`, `InvQR`, `InvTypeName`), with `VRTYPE = "SR"`, `VRNO = VrNo`, `ORDNO = clsSalM.OrdNo`.
- Call sites: Save handler edit mode (SR.cs:4068), new mode (4073), and 14133 (inside the e-invoice flow; context NOT READ).
- Note: it runs **before** `SaveSalT` and the other writes (4068 and 4073 precede 4077). All exceptions are swallowed (`catch { }`, 14015).

SQL [VERIFIED: Model\EInvDB.cs:13-18]:

```sql
INSERT INTO EINV (VRTYPE, VRNO, ORDNO, VSVRTYPE, EINVID, APIRESPONSE, EINVMODE, CSR, PRIVATEKEY, PUBLICKEY, SECRETKEY, INVWARNING, SIGNEDXML, INVHASH, UUID, PIH, INVQR, INVTYPE, CRDATE)
VALUES('<VRTYPE>',<VRNO>,<ORDNO>,<VSVRTYPE>,'<EINVID>',N'<APIRESPONSE>',N'<EINVMODE>',N'<CSR>',N'<PRIVATEKEY>',N'<PUBLICKEY>',N'<SECRETKEY>',N'<INVWARNING>',N'<SIGNEDXML>',N'<INVHASH>',N'<UUID>',N'<PIH>',N'<INVQR>',N'<INVTYPE>','<DateTime.Now>')
```

Whether a prior `EINV` row is deleted in edit mode: NOT READ (the edit-mode block at 4021-4069 does not delete from `EINV`, so a second row for the same voucher is possible). [ASSUMPTION: no unique key on EINV; the table definition was not checked here.]

---

## 9. Findings worth the team's attention

1. **Delete scope is too wide in places.** `SalDDB.DeletByVrNo` deletes `WHERE VRNO=<n>` with no VRTYPE, and `PrstDB.DeleteByVrNo` deletes `WHERE VRNO=<n>` with no STYPE. Editing a sales return can therefore delete SALD or PRST rows of other voucher types that happen to share the number. [VERIFIED: Model\SalDDB.cs:35, Model\PrstDB.cs:33-40, called from SR.cs:4027 and 3131] The SALT delete uses `INVNO` alone as well (Model\SalTDB.cs:73), and SALT carries `STYPE`.
2. **SALD DFLAG on the "D" rows is inherited**, not set (section 2, pass 3).
3. **BTYPE differs between SALT and SALD for payment mode 4** (`D` vs `B`).
4. **No transaction.** All writes are auto-commit string SQL; a failure part-way leaves partial data. Compensation exists only inside `SaveStkDt` and `SavePrSt`/`SaveBatchDet` (cleanup on failure) and none for the rest. [VERIFIED by absence in SR.cs:4021-4168; the `clsDBFunction.Execute` behaviour was established in the Purchase review]
5. **Silent failures:** `SavePrSt`, `SaveBatchDet`, `SaveSntStk`, `SaveAdjustment`, `SaveEinv` results are ignored by the caller (SR.cs:4068-4104).
6. **`PRMAST.STOCK` is updated inside `SaveSalT`**, only when `ApprovalInventoryStatus` is true (section 1), whereas `ClearStock("Cancel")` reverses it only when `ApprovalStockUpdate` is true (SR.cs:3418). The two flags are different variables, initialised both true (SR.cs:766-767) and set from different approval helpers (876, 4003). If they diverge, the stock can drift.
7. **`ClearStock` loops over `dtSalT`** (the data loaded when the voucher was opened), not the grid, so it reverses the old state as intended; but the fill is NOT READ.
8. **The `StkDtDB.insert` SQL has no `ISNULL` or defaults**: `UTIME` for an update is taken from the old rows, falling back to `DateTime.Now` when none exist (SR.cs:2805).
9. **The `Dgv_col_price` guard at SR.cs:2826 tests the cell object**, not its value, so the `else { Rate = 0 }` branch is unreachable for a populated row.
10. **Part payment applies only to credit returns (mode 2)**, and the amount flows from `txt_cash_paid`; cash and bank returns never produce `YVR` rows from this form. If the Web API must support "refund in cash against a credit return", it must follow the two-row `YVR` pattern in section 7c.

## 10. NOT READ in this task

- `clsSalT`, `clsSalD`, `clsStkDt`, `clsYvr`, `clsBill` class defaults (unset columns above).
- `SaveDoc`, `SaveSalM`/`SalMDB.insert` (number generation, `clsSalM.OrdNo`, `VrNo`, `BillNo`, `retVal`), `UpdateVoucherSystemOrdNo`, `FyDetailsDB.UpdateVoucherNo`, `VrInvNoText`.
- `PrAliasDB.SearchByAlCode` / `SearchByPrCodeAndName`, `PrMastDB.SearchByCode` / `GetTaxDetails`, `CalcPricefromBatch`, `ValidateBatchGotoNegetive`.
- Loaders for `dtSalT`, `dtSNT`, `dtSNT_Used`, `dtPrMfgExpDetails`; the `BillAdjustment` dialog; `PayModeDB.ActiveModes`.
- Meaning of `Variables.batch`, `SlNoTrack`, `BatchRtEdit`, `VatFlg`, `INVAMTEDIT_SI`, `CostC`, `BranchYN` beyond how they are used above.
- The e-invoice flow around SR.cs:14133, and any `EINV` cleanup in edit mode.
- The Access (`dbAcc`) branch of every Model method.
