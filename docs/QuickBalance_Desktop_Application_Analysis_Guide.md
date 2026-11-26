# QuickBalance Desktop Application Analysis Guide

## Purchase Invoice: Legacy Flow, API Mapping, Pending Findings and Agent Instructions

**Purpose:** This document is the working analysis reference for
understanding the QuickBalance legacy desktop application and converting
its purchase-invoice workflow into the QuickBalance ASP.NET Core Web
API.

An AI coding agent can use this document to: - Understand the known
purchase-invoice flow. - Identify which legacy operations and
validations have already been analysed. - Find missing methods, SQL
queries, table mappings and business rules. - Trace a complete desktop
workflow from user action to database writes. - Update this document as
new evidence is discovered. - Avoid implementing guessed behaviour or
silently omitting legacy operations.

> **Evidence rule:** Distinguish confirmed source-code behaviour from
> inferred behaviour and unresolved questions. Never mark an operation
> complete merely because an API endpoint or SQL insert exists.

------------------------------------------------------------------------

# 1. Project and architecture context

## 1.1 API conventions

  -----------------------------------------------------------------------
  Area                                Current convention
  ----------------------------------- -----------------------------------
  API                                 QuickBalance ASP.NET Core Web API

  Runtime                             .NET 10

  Database                            SQL Server

  Data access                         Dapper with
                                      `Microsoft.Data.SqlClient`

  Repository                          `QuickBalance.API.Repository.*`

  Interface                           `QuickBalance.API.Interfaces.*`

  DTOs                                `QuickBalance.API.DTOs.*`

  Service                             Business validation and
                                      orchestration

  Controller                          HTTP endpoint and
                                      authenticated-user handling

  User identity                       Authenticated claim; do not accept
                                      `userId` from request

  SQL timeout                         60 seconds

  Transactions                        One SQL connection and one SQL
                                      transaction for all writes in one
                                      operation

  New code                            Prefer typed DTOs; avoid
                                      `DataTable` unless a legacy
                                      dependency makes it unavoidable
  -----------------------------------------------------------------------

The existing Purchase Order repository is the reference for the
connection/transaction style: 1. Create and open one `SqlConnection`. 2.
Begin a transaction, currently using `IsolationLevel.Serializable`. 3.
Execute every related SQL command through that same connection and
transaction. 4. Verify inserted records. 5. Commit only after all
required work succeeds. 6. Roll back on failure and rethrow the original
exception.

Do not use independent `DBConn` helper calls inside this transaction if
those helpers open their own connections. They would not participate in
the transaction.

## 1.2 Purchase invoice API files

Known project structure:

``` text
Controllers/
  Transactions/
    Purchase/
      PurchaseInvoiceController.cs

DTOs/
  Purchase/
    PurchaseInvoiceCreateRequest.cs
    PurchaseInvoiceUpdateRequest.cs
    PurchaseInvoiceItemRequest.cs
    PurchaseInvoiceAdditionalInfoRequest.cs
    PurchaseInvoiceTaxDetailRequest.cs
    PurchaseInvoiceResponse.cs
    PurchaseInvoiceCreateResponse.cs

Interfaces/
  Purchase/
    IPurchaseInvoiceRepository.cs
    IPurchaseInvoiceService.cs

Services/
  Purchase/
    PurchaseInvoiceService.cs

Repository/
  Purchase/
    PurchaseInvoiceRepository.cs
```

## 1.3 Current interfaces

Repository:

``` csharp
public interface IPurchaseInvoiceRepository
{
    Task<int> CreatePurchaseInvoiceAsync(
        PurchaseInvoiceCreateRequest request,
        int userId);

    Task<PurchaseInvoiceResponse?> GetPurchaseInvoiceByIdAsync(
        int vrNo);

    Task<bool> UpdatePurchaseInvoiceAsync(
        int vrNo,
        PurchaseInvoiceUpdateRequest request,
        int userId);

    Task<bool> DeletePurchaseInvoiceAsync(
        int vrNo,
        int userId);
}
```

Service:

``` csharp
public interface IPurchaseInvoiceService
{
    Task<PurchaseInvoiceCreateResponse> CreatePurchaseInvoiceAsync(
        PurchaseInvoiceCreateRequest request,
        int userId);

    Task<PurchaseInvoiceResponse?> GetPurchaseInvoiceByIdAsync(int vrNo);

    Task<PurchaseInvoiceCreateResponse> UpdatePurchaseInvoiceAsync(
        int vrNo,
        PurchaseInvoiceUpdateRequest request,
        int userId);

    Task<bool> DeletePurchaseInvoiceAsync(int vrNo, int userId);
}
```

The repository returns the generated `VRNO` as `int`. The service
converts it into `PurchaseInvoiceCreateResponse`. Do not change this
contract without updating the interface, implementation, service and
callers together.

------------------------------------------------------------------------

# 2. Purchase invoice DTOs currently supplied

These are the DTO definitions supplied in the project conversation.
Treat them as the current API contract until the source files are
changed.

## 2.1 PurchaseInvoiceCreateRequest

``` csharp
public class PurchaseInvoiceCreateRequest
{
    public DateTime PurchaseDate { get; set; }
    public string BillNo { get; set; } = string.Empty;
    public DateTime BillDate { get; set; }

    public int SupplierCode { get; set; }
    public int FormatId { get; set; }
    public int PaymentId { get; set; }

    public int CurrencyCode { get; set; }
    public decimal CurrencyConversionRate { get; set; }

    public int CostCentreCode { get; set; }
    public int CashAccountCode { get; set; }
    public int BankAccountCode { get; set; }

    public bool VatEnabled { get; set; }
    public bool TrackingEnabled { get; set; }

    public decimal CashPaid { get; set; }

    public DateTime? DueDate { get; set; }
    public string? Remarks { get; set; }
    public string? ShippingAddress { get; set; }
    public string? BayanNo { get; set; }

    public List<PurchaseInvoiceItemRequest> Items { get; set; } = [];
    public List<PurchaseInvoiceAdditionalInfoRequest> AdditionalInfo { get; set; } = [];
    public List<PurchaseInvoiceTaxDetailRequest> VatDetails { get; set; } = [];
}
```

## 2.2 PurchaseInvoiceItemRequest

``` csharp
public sealed class PurchaseInvoiceItemRequest
{
    public int ProductCode { get; set; }
    public decimal Quantity { get; set; }
    public int UnitCode { get; set; }
    public decimal Price { get; set; }
    public decimal ActualPrice { get; set; }

    public int TaxCode { get; set; }
    public decimal TaxPercentage { get; set; }

    public decimal DiscountPercentage { get; set; }
    public decimal LineDiscountPercentage { get; set; }
    public decimal LineDiscountAmount { get; set; }

    public int GodownCode { get; set; }
    public int CostCentreCode { get; set; }

    public int OrderNo { get; set; }
    public int OrderRowId { get; set; }
    public int GrnNo { get; set; }
    public int NtRowId { get; set; }
    public int TrnRowId { get; set; }

    public int AliasCode { get; set; }
    public decimal AliasQuantity { get; set; }

    public int ConversionType { get; set; }
    public decimal ConversionFactor { get; set; }

    public decimal Mrp { get; set; }
    public decimal SalesRate { get; set; }

    public string? Description { get; set; }
    public string? Batch { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufacturingDate { get; set; }

    public string? PriceFlag { get; set; }
    public string? PreviousReference { get; set; }
    public string? PreviousNoteReference { get; set; }
    public string? LineMerge { get; set; }

    public decimal Bml { get; set; }
    public decimal Bmq { get; set; }
    public decimal Bmw { get; set; }

    public int SerialNumber { get; set; }
    public int OrderRowReferenceId { get; set; }
    public int NoteRowReferenceId { get; set; }
    public int TransactionRowReferenceId { get; set; }
}
```

## 2.3 Additional information and VAT detail DTOs

``` csharp
public sealed class PurchaseInvoiceAdditionalInfoRequest
{
    public int Code { get; set; }
    public decimal Percentage { get; set; }
    public decimal Amount { get; set; }
    public int GccHead { get; set; }
}

public sealed class PurchaseInvoiceTaxDetailRequest
{
    public int Code { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxPercentage { get; set; }
    public decimal TaxAmount { get; set; }
    public int CostCentreCode { get; set; }
}
```

## 2.4 Response DTOs

``` csharp
public sealed class PurchaseInvoiceCreateResponse
{
    public int VoucherNo { get; set; }
    public int OrderNo { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class PurchaseInvoiceResponse
{
    public int VoucherNo { get; set; }
    public int OrderNo { get; set; }
    public string VoucherType { get; set; } = "P";
    public DateTime PurchaseDate { get; set; }
    public string BillNo { get; set; } = string.Empty;
    public int SupplierCode { get; set; }
}
```

`PurchaseInvoiceUpdateRequest` inherits `PurchaseInvoiceCreateRequest`
and adds `VoucherNo`.

------------------------------------------------------------------------

# 3. Purchase invoice end-to-end business flow

The target workflow must be confirmed against the desktop form's actual
event handlers and database methods. The following is the currently
understood flow.

``` text
User opens Purchase Invoice screen
        |
        v
Load defaults and lookup data
        |
        v
Select supplier, date, currency, payment mode,
cost centre, format and invoice options
        |
        v
Add / edit purchase item rows
        |
        v
Calculation()
        |
        v
SubTotal()
        |
        v
TaxSummary()
        |
        v
AddInfoCalculation()
        |
        v
NetCalculation()
        |
        v
Validate invoice and required fields
        |
        v
Generate purchase order/reference number
        |
        v
Insert PURM master
        |
        v
Insert PURT item rows
        |
        v
Insert PURD financial/detail rows
        |
        v
Insert normal STKDT stock rows
        |
        v
Verify all required records
        |
        v
Commit transaction
```

Important: this diagram is a working model. Confirm the actual order of
calls from the desktop Save button handler. A method may be called more
than once as the user edits the invoice.

------------------------------------------------------------------------

# 4. Legacy calculation methods

The desktop calculation methods are the source of truth for financial
behaviour. Do not replace them with a simplified subtotal formula.

## 4.1 Calculation()

**Purpose:** Calculate values at purchase-item level.

Known areas of behaviour from the reviewed legacy snippets and
discussion: - Quantity and unit conversion. - Alternative unit and alias
conversion. - Price selection and base-currency conversion. -
Actual-price versus normal-price conversion. - Line discount percentage
and amount. - General item discount percentage. - Taxable amount and
tax. - MRP and sales-rate handling. - Additional-cost conversion where
applicable.

**Evidence status:** The method name and responsibility are known, but
its complete source body and exact expression order must be attached and
checked before declaring calculation parity.

## 4.2 SubTotal()

A legacy snippet showed aggregation of values including: - Discountable
amount. - Quantity. - Total amount. - Total tax. - Total amount
including tax. - Total MRP.

The snippet initialized aggregate values and iterated the purchase grid.
Exact expressions, rounding points and which grid columns feed each
accumulator must be confirmed from the complete method.

## 4.3 TaxSummary()

Known responsibility: - Summarize tax by tax code. - Apply invoice
format and VAT-enabled conditions. - Produce tax-account allocation
information used when saving `PURD`.

Need to confirm: - Grouping key and treatment of zero-tax rows. -
Whether tax percentage is derived from product tax master or
request/grid value. - Rounding precision and when rounding occurs. -
Whether VAT reason/export formats alter tax calculation or only the
saved reason code. - How tax-account amounts are allocated and
reconciled to line totals.

## 4.4 AddInfoCalculation()

Known legacy rules from prior analysis: - Additional-information rows
are used for invoice-level discount, freight, customs duty and related
values. - Discount percentage is applied to the legacy discountable
base. - Customs duty uses the legacy total-plus-freight base described
in the prior code review. - VAT calculation uses the legacy
gross/customs-duty base described in the prior code review. - Row
positions and configured codes matter; do not assume the array order is
arbitrary.

The exact row indexes, percentage bases, rounding and blank-row
behaviour must be verified against the original method.

## 4.5 NetCalculation()

Known responsibility: - Combine calculated item totals, tax summary and
additional-information values. - Calculate gross, net and payable
amounts. - Apply freight and coin adjustment. - Produce the values
ultimately saved to `PURM`.

The final `PURM` values must be outputs of the complete legacy
calculation sequence, not a repository-only `sum(quantity * price)`
calculation.

## 4.6 Calculation parity checklist

-   [ ] Original source of `Calculation()` captured.
-   [ ] Original source of `SubTotal()` captured.
-   [ ] Original source of `TaxSummary()` captured.
-   [ ] Original source of `AddInfoCalculation()` captured.
-   [ ] Original source of `NetCalculation()` captured.
-   [ ] Every referenced grid column mapped to a DTO property or a
    server-side lookup.
-   [ ] Every rounding function and precision setting identified.
-   [ ] Currency conversion functions identified.
-   [ ] Legacy calculation order reproduced.
-   [ ] Test cases compared between desktop and API for each calculation
    stage.
-   [ ] Final `PURM` totals match desktop results.

------------------------------------------------------------------------

# 5. Database tables and insert responsibilities

## 5.1 PURM --- purchase invoice master

The legacy `PurmDB.insert(clsPurM, DateTime InvDateTime)` inserts the
invoice header and returns the generated voucher number.

**Required API behaviour:** - Use parameterized SQL. - Use SQL Server
`OUTPUT INSERTED.VRNO`. - Use the generated `VRNO` for related detail
and stock records. - Use the authenticated `userId` for legacy log-user
fields. - Use one transaction shared by all invoice writes.

The legacy insert mapping supplied in the project discussion includes
these 84 columns:

``` text
VRTYPE, PURDATE, BILLNO, BILLDATE, SUPCODE, DISCP, DISCVAL, TAX,
NTVALUE, TVALUE, COINADJ, AMOUNT, LOGCD, REMARKS, FREIGHT, UDATE,
CREATEDDT, INFORMAT, PAYMODE, CUSDUITY, RETURNFLG, ORDNO, SLCODE,
BTYPE, VATFLG, BILLREFFLG, CURCONRATE, CURCODE, SHIPADDRESS, SLFLG,
CANCELFLG, GRNNO, DUEDATE, ADJCD, CASHPAID, YVRNO, INVNOTEXT,
VSVRTYPE, VSDOCNO, VSORDCD, MRP, TRTYPE, TRVRNO, TRBRCD, TRGDWNCD,
STATUS, DRACC, CRACC, TRANSFERRED, BAYANNO, ADDITIONALCOST, PIF1,
PIF2, PIF3, PIF4, PIF5, PIF6, ISMNBILL, MNBILLNO, ULOGCD, PIF7,
PIF8, PIF9, PIF10, PIF11, PIF12, PIF13, PIF14, PIF15, PIF16,
PIF17, PIF18, PIF19, PIF20, PHYWISE, PHYMODE, PHYCATCD,
ADDCOSTACCD, RETTRANSFER, APRVLST, APRVLRBY, APRVLRDT
```

Known mapping rules: - `VRTYPE = "P"` for purchase invoice. - `LOGCD`
and `ULOGCD` use authenticated `userId`. - `UDATE` and `CREATEDDT` use
the invoice creation timestamp. - `APRVLRBY` and `APRVLRDT` follow the
legacy approval metadata rules; approval processing itself is outside
the agreed initial scope. - Remarks, shipping address, Bayan number and
PIF text fields must be parameterized Unicode strings. - Currency
conversion rate and currency code come from the request after
validation. - Totals come from the legacy calculation sequence. -
Payment mode maps to legacy `BTYPE`: PaymentId 1 =\> `C`; 2 =\> `R`; 3
=\> `CD`; 4 =\> `B`; otherwise empty string. - Cost-centre flags and
codes depend on the legacy cost-centre configuration and
multi-cost-centre mode. - VAT flag maps from `VatEnabled`. - `ORDNO`
must be generated by the approved legacy voucher-number routine, not
assumed to be a globally unique `MAX + 1` if fiscal-year/series logic
applies. - `INVNOTEXT` comes from the legacy `InvNoText("Purchase")`
behaviour. - `GRNNO` must not trigger GRN creation in this API scope.
Its header value needs a confirmed rule. - Cash/bank fields in `PURM`
must preserve the legacy header mapping, but this scope does not create
payment vouchers. - Dynamic PIF values need an explicit, validated
mapping if configured; do not use arbitrary reflection over client
input.

**Open questions for PURM:** - Exact mapping for every one of the 84
columns. - How `VSVRTYPE`, `VSDOCNO` and `VSORDCD` are populated when
voucher-system mode is enabled. - Exact `ORDNO` generator and
fiscal-year locking behaviour. - Whether `GRNNO` is zero or supplied
under each tracking configuration. - Which payment-related header fields
are saved without creating payment records. - How PIF fields are
configured and populated. - Whether approval metadata should be
blank/default in the initial API scope.

## 5.2 PURT --- purchase invoice item details

The legacy `PurTDB.insert(clsPurT)` inserts one row per purchase item.

Columns supplied:

``` text
VRTYPE, VRNO, PURDATE, BILLNO, BILLDATE, SUPCODE, PRCODE, QTY,
UNITCODE, RATE, PRRATE, BTYPE, LOGCD, TAXCODE, DISC, TAX, BATCH,
EXPIRY, GDWCODE, PRDESC, RETURNFLG, ORDNO, STKFLG, CURCONRATE,
CURCODE, GRNFLG, SLCODE, PRFLG, PORDNO, GRNNO, ORGQTY, ORGRATE,
CANCELFLG, CONVTYPE, CONVFACT, CONVQTY, CONVRATE, LDISCTYPE,
LDISCPERC, LDISCAMT, DISCP, BML, BMQ, BMW, MRP, MFGDATE,
VSVRTYPE, VATREASONCD, RETREASON, PRV, SNTSLNO, PRVN, LM,
ADDCOST, CONVADDCOST, ORDROWID, NTROWID, TRNROWID, PRVY,
PRVNY, PRVR, PRVRY, ALCODE, ALNAME
```

Known mapping rules: - `VRTYPE = "P"`. - `VRNO` is the `PURM.VRNO`
returned by the master insert. - `LOGCD = userId`. - Common invoice
date, bill number/date, supplier and currency values come from the
invoice master/request. - `PRCODE`, `QTY`, `UNITCODE`, `TAXCODE` come
from each item. - `PRRATE` uses `ToBaseActual(Price)` when
`INVAMTEDIT_PI` is enabled; otherwise `ToBase(Price)`. - `DISC` uses
actual/base conversion as specified by the legacy code. - `TAX` depends
on invoice format and tax/additional-info rules; do not assume request
`TaxPercentage` is the final tax amount. - `GDWCODE` uses item godown if
supplied; otherwise the legacy branch-default godown. - `PRDESC` uses
item description. - `RETURNFLG = false`. - `ORDNO` comes from the
invoice master. - `STKFLG = "R"`. - `GRNFLG`, `PORDNO` and `GRNNO`
follow the legacy order/GRN linkage rules; do not create GRN records as
part of this operation. - Cost-centre code depends on the
enabled/multi-cost-centre legacy settings. - `PRFLG` derives from
`PriceFlag`. - `ORGQTY = 0`, `ORGRATE = 0`, `CANCELFLG = false` in the
described legacy path. - Alternative unit is read from the product
master. - `CONVTYPE`, `CONVFACT`, `CONVQTY` and `CONVRATE` must follow
the legacy alias and alternate-unit rules. - `LDISCTYPE`, `LDISCPERC`,
`LDISCAMT` derive from line discount inputs. - `DISCP` stores the legacy
discount percentage. - BML/BMQ/BMW, batch, MRP, expiry and manufacturing
date map from the item. - Expiry/manufacturing dates default to purchase
date where the legacy code does so. - VAT reason code depends on invoice
format/export settings or item VAT exception. - Previous-reference
flags, serial number and line-merge fields must follow the exact legacy
mapping. - Additional cost and converted additional cost use the legacy
four-decimal base conversion when positive. - Row-reference columns use
the appropriate request reference IDs and tracking-mode rules. - Alias
code/name must be sourced and validated using the legacy product/alias
lookup.

**Open questions for PURT:** - Exact `RATE` versus `PRRATE` mapping and
whether either is a tax-inclusive/exclusive value. - Exact tax amount
per row and rounding. - Product alternative-unit lookup SQL and
schema. - Exact alias quantity and conversion-factor rules, including
zero factors. - Exact source of VAT reason and return reason. - Exact
PRV/PRVN/PRVT and PRVR/PRVRY rules. - Exact line-merge behaviour and
source-row schema. - Whether batch and serial data are persisted in this
table in the current scope or handled by a separate operation. - Whether
every DTO reference field maps directly to its similarly named database
field.

## 5.3 PURD --- purchase invoice financial/detail rows

Columns supplied:

``` text
VRTYPE, VRNO, PURDATE, BILLNO, BILLDATE, SUPCODE, ETYPE, CODE,
AMOUNT, TAX, BTYPE, LOGCD, TAXAMT, RETURNFLG, ORDNO, SLCODE,
VATFLG, CURCONRATE, CURCODE, DFLAG, ORGAMT, ORGTAXAMT, CANCELFLG,
ADJCD, GCCCD, VSVRTYPE
```

The legacy save logic has three logical sections.

### A. VAT detail rows

-   Process rows with nonzero amount.
-   `ETYPE = "T"`.
-   Code uses the configured VAT code when VAT is enabled; otherwise use
    the row code.
-   Amount and tax amount use the legacy currency conversion.
-   Tax percentage/value uses legacy rounding precision.
-   `RETURNFLG = false`.
-   Cost-centre mapping depends on cost-centre and multi-cost-centre
    settings.
-   `DFLAG = true`.
-   `ADJCD` uses the cash account for cash mode, bank account for bank
    mode, otherwise zero.
-   `GCCCD = 0`.

### B. Additional-information rows

-   Process nonzero values and the special last-row condition from the
    legacy loop.
-   `ETYPE = "T"` for the specified legacy row index (index 3 in the
    reviewed logic); otherwise `"A"`.
-   `CODE` comes from the additional-information description/code
    mapping.
-   Amount base depends on the legacy row counter: gross, net or total.
-   Tax is rounded with legacy precision.
-   Tax amount uses base-currency conversion.
-   Cost centre depends on multi-cost-centre mode.
-   `DFLAG` depends on the row counter (`false` when `j > 4`, otherwise
    `true`).
-   `GCCCD` comes from the row's GCC head.
-   The special row index 3 is skipped for insertion in the described
    legacy logic.
-   Preserve the legacy counter increment behaviour exactly.

### C. Tax-account allocation rows

-   Group purchase item amounts by tax account.
-   Insert one `ETYPE = "D"` row per grouped tax account.
-   Amount is the base-currency converted sum.
-   Tax and tax amount fields are zero in the described logic.
-   Empty allocation groups are valid; do not fail the whole invoice
    merely because no allocation rows are required.

**Open questions for PURD:** - Exact DTO-to-row mapping for VAT detail
and additional-info records. - Whether
`PurchaseInvoiceTaxDetailRequest.Amount` means tax base, tax amount or
gross amount in each legacy branch. - Exact VAT code source. - Exact
`TAXAMT`, `ORGAMT` and `ORGTAXAMT` values for every row type. - Exact
tax-account lookup and grouping SQL. - Whether the current DTOs contain
enough information to distinguish all legacy additional-info row
types. - Whether VAT rows and additional-info rows can share the same
`ETYPE`/`CODE` combinations.

## 5.4 STKDT --- normal stock transactions

The legacy `StkDtDB.insert(clsStkDt)` inserts normal stock movement
rows.

Columns supplied:

``` text
VRTYPE, PRCODE, VRDATE, ORDNO, NTNO, TRNNO, QTY, TYPE, PARTYCD,
LOGCD, UTIME, RATE, UNITCODE, CONVTYPE, CONVFACT, CONVQTY, PRFLG,
CONVRATE, GDWCODE, LUTIME, VSVRTYPE, PRV, PRVN, PRVT, SNTSLNO,
LM, ADDCOST, CONVADDCOST, CLOSES, ORDROWID, NTROWID, TRNROWID,
QTNCODE, QTNROWID, PRVQ, PRVQY, PRVY, PRVNY, PRVTY, NTRNO, NTRROWID
```

Known behaviour: - Purchase invoice stock transaction uses
`VRTYPE = "TR"` in the described purchase path. - `TYPE = "R"`. -
`VRDATE` is purchase date. - `PARTYCD` is supplier. -
`LOGCD = userId`. - `UTIME` uses current time for a new stock row under
the legacy `uType` conditions; otherwise the legacy timestamp source is
used. - `LUTIME = DateTime.Now` in the legacy method. - Product,
quantity, rate and unit come from the purchase item. - Rate subtracts
line discount value and general discount before applying the appropriate
base conversion in the described legacy flow. - `PRFLG` and `CONVRATE`
follow price-flag and actual-price conversion rules. - Godown defaults
to the branch godown when no item godown is selected. -
Alternate-unit/alias conversion must match `PURT`. - Additional cost
conversion follows the legacy four-decimal conversion. -
Previous-reference, serial, line-merge and row-reference fields map
according to the legacy rules. - Only normal `STKDT` is inserted by this
API.

**Scope boundary:** `STKDTPHY` is explicitly excluded and will be
handled separately. Do not write to `STKDTPHY`, call physical-stock
insert functions, or claim physical stock is updated.

**Open questions for STKDT:** - Exact `TRNNO` and `NTNO` values for a
purchase invoice with/without tracking. - Exact `VSVRTYPE` source. -
Exact timestamp retrieval behaviour for new versus update operations. -
Product lookup SQL for alternate unit and alias. - Exact stock rate
calculation and conversion rounding. - Exact `LM` merged-row expansion
behaviour and source rows. - Which stock reference fields are mandatory
and how they map from DTOs. - Whether normal stock insertion is always
required or conditional on a legacy inventory setting.

------------------------------------------------------------------------

# 6. Purchase order repository: reference pattern

The supplied `CreatePurchaseOrderAsync` method is an architectural
reference, not a purchase-invoice business-rule template.

Useful patterns: - Parameterized SQL. - One connection. - Serializable
transaction. - `UPDLOCK, HOLDLOCK` around number generation. -
`OUTPUT INSERTED...`. - Insert verification. - Affected-row count
checks. - Rollback and rethrow.

Do not copy the purchase-order subtotal logic into purchase invoice
creation. Purchase invoice financial calculations have their own legacy
methods and additional tables.

The purchase-order example uses `MAX(ORDNO) + 1`. Confirm whether the
purchase invoice legacy generator is
`FyDetailsDB.ValidateVoucherNo("PUR", "GET", 0)` and preserve its
fiscal-year/series rules if so.

------------------------------------------------------------------------

# 7. Current implementation status

Use these statuses consistently: - **Confirmed:** directly supported by
source code or supplied SQL. - **Partially analysed:** some behaviour is
known, but the full method or dependencies are missing. - **Pending
evidence:** not enough source material to implement safely. - **Not in
scope:** explicitly excluded from this API operation. - **Implemented:**
code exists and has been reviewed/tested against legacy behaviour. -
**Verified:** desktop/API output and database effects have been compared
successfully.

  ------------------------------------------------------------------------
  Feature                  Current status          Notes
  ------------------------ ----------------------- -----------------------
  Create request DTO       Confirmed               User supplied full
                                                   class

  Item DTO                 Confirmed               User supplied full
                                                   class

  Additional-info DTO      Confirmed               User supplied full
                                                   class

  VAT detail DTO           Confirmed               User supplied full
                                                   class

  Repository interface     Confirmed               Returns `Task<int>`

  Service interface        Confirmed               Create returns response
                                                   DTO

  Service validation       Partially analysed      Existing validation
                                                   code supplied

  `Calculation()`          Partially analysed      Full method body
                                                   required for exact
                                                   parity

  `SubTotal()`             Partially analysed      Snippet/behaviour
                                                   known; full source
                                                   required

  `TaxSummary()`           Partially analysed      Grouping and rounding
                                                   details need source

  `AddInfoCalculation()`   Partially analysed      Legacy bases known at a
                                                   high level; full source
                                                   required

  `NetCalculation()`       Partially analysed      Full formula and
                                                   rounding sequence
                                                   required

  `PURM` columns           Partially analysed      Column list supplied;
                                                   exact values for all
                                                   fields need
                                                   confirmation

  `PURT` columns           Partially analysed      Column list and many
                                                   mappings supplied

  `PURD` columns           Partially analysed      Three insertion
                                                   sections identified

  `STKDT` columns          Partially analysed      Column list and major
                                                   mapping rules supplied

  Voucher number           Pending evidence        Confirm
  generation                                       fiscal-year/series
                                                   behaviour

  Product/alternate-unit   Pending evidence        Exact query and schema
  lookup SQL                                       required

  Tax-account lookup SQL   Pending evidence        Exact query and
                                                   grouping required

  Godown default lookup    Pending evidence        Legacy branch default
                                                   function/query required

  Line merge expansion     Pending evidence        Need LM helper and
                                                   source-row schema

  Full repository          Pending                 Do not treat
  implementation                                   placeholder code as
                                                   complete

  `STKDTPHY`               Not in scope            Separate operation

  GRN creation             Not in scope            Do not create GRN
                                                   records here

  Payment voucher creation Not in scope            Header mapping may
                                                   still need preserving

  Approval workflow        Not in scope            No approval workflow
                                                   processing in initial
                                                   create API

  End-to-end parity tests  Pending                 Compare desktop and API
  ------------------------------------------------------------------------

------------------------------------------------------------------------

# 8. Missing evidence checklist

When inspecting the desktop application, collect these exact source
items.

## 8.1 Save button and orchestration

-   [ ] Purchase Invoice form Save button event handler.
-   [ ] Method called immediately before save.
-   [ ] Validation methods called by Save.
-   [ ] Exact call order for calculation methods.
-   [ ] Exact order of `PURM`, `PURT`, `PURD` and `STKDT` writes.
-   [ ] Transaction begin/commit/rollback behaviour in desktop code.
-   [ ] Post-save UI updates, voucher number display and grid reset.

## 8.2 Calculations

-   [ ] `Calculation()`.
-   [ ] `SubTotal()`.
-   [ ] `TaxSummary()`.
-   [ ] `AddInfoCalculation()`.
-   [ ] `NetCalculation()`.
-   [ ] `ToBase()`.
-   [ ] `ToBaseActual()`.
-   [ ] `ToFourBase()`.
-   [ ] `CurrConvDB.ToBase()`.
-   [ ] `Variables.FourDecimalPoint` definition.
-   [ ] `INVAMTEDIT_PI` setting and meaning.
-   [ ] `VATCode` and VAT configuration.
-   [ ] Tax-account grouping logic.

## 8.3 Database methods and queries

-   [ ] `PurmDB.insert`.
-   [ ] `PurTDB.insert`.
-   [ ] `PurDDB.insert`.
-   [ ] `StkDtDB.insert`.
-   [ ] `FyDetailsDB.ValidateVoucherNo`.
-   [ ] `InvNoText("Purchase")`.
-   [ ] Product alternative-unit query.
-   [ ] Alias lookup query.
-   [ ] Tax-account lookup query.
-   [ ] `Functions.GetGodownByBranch`.
-   [ ] Cost-centre configuration query/settings.
-   [ ] Currency conversion queries.
-   [ ] `GetUTime` and stock timestamp methods.
-   [ ] `LM()` line-merge helper and its source query.

## 8.4 Validation and configuration

-   [ ] Required supplier validation.
-   [ ] Duplicate supplier bill-number validation.
-   [ ] Purchase date and bill date validation.
-   [ ] Financial-year validation.
-   [ ] Product active/allowed validation.
-   [ ] Unit and conversion validation.
-   [ ] Tax code and tax percentage validation.
-   [ ] Godown validation.
-   [ ] Cost-centre validation.
-   [ ] Duplicate voucher/reference validation.
-   [ ] Approval and edit restrictions, even if approval processing is
    out of scope.
-   [ ] Currency-rate validation.
-   [ ] Payment-mode/account validation.

------------------------------------------------------------------------

# 9. Required repository implementation sequence

The final `CreatePurchaseInvoiceAsync` should follow this sequence.

1.  Validate request and authenticated `userId`.
2.  Open one SQL connection.
3.  Begin a transaction with the agreed isolation level.
4.  Load required database configuration/lookups using that same
    connection and transaction where relevant.
5.  Generate `ORDNO` using the confirmed legacy voucher-number routine.
6.  Run the exact legacy calculation pipeline using server-side data and
    validated inputs.
7.  Insert `PURM`, using all mapped columns and `OUTPUT INSERTED.VRNO`.
8.  Verify the inserted `PURM` row.
9.  Insert each `PURT` row with correct master references and legacy
    conversion values.
10. Verify `PURT` count and, where appropriate, key totals.
11. Insert applicable `PURD` VAT detail rows.
12. Insert applicable `PURD` additional-information rows.
13. Insert grouped `PURD` tax-account allocation rows.
14. Verify `PURD` records using the expected row counts/types and
    totals.
15. Insert normal `STKDT` rows only.
16. Verify `STKDT` rows.
17. Commit.
18. Return generated `VRNO`.

On any exception: - Roll back the transaction. - Preserve the original
exception. - Do not swallow database errors. - Do not return success if
any required insert or verification failed.

Avoid `Console.WriteLine` as the only production diagnostics; use the
project's configured `ILogger` where available.

------------------------------------------------------------------------

# 10. Verification and legacy parity tests

A successful SQL insert is not sufficient to establish correctness.

For each test invoice, capture the same input in the desktop application
and API, then compare:

  Comparison                           Desktop value   API value Expected
  ---------------------------------- --------------- ----------- ------------------------------
  Generated order/reference number                               Same numbering rule
  Voucher number                                                 Valid unique `VRNO`
  Item quantity                                                  Exact
  Converted quantity                                             Exact
  Base rate                                                      Exact
  Line discount                                                  Exact
  Taxable amount                                                 Exact
  Item tax                                                       Exact
  Invoice discount                                               Exact
  Freight                                                        Exact
  Customs duty                                                   Exact
  VAT                                                            Exact
  Gross total                                                    Exact
  Net total                                                      Exact
  Coin adjustment                                                Exact
  Payable amount                                                 Exact
  PURM record                                                    Correct columns
  PURT records                                                   Same count and values
  PURD records                                                   Same types, codes and values
  STKDT records                                                  Same normal stock effects

Test at least: - One item, no discount and no tax. - Multiple items with
different tax codes. - Line discount percentage. - Line discount
amount. - Invoice-level discount. - Freight. - Customs duty. - VAT
enabled and disabled. - Each supported invoice format. - Foreign
currency and non-unit conversion rate. - Alternate unit. - Alias
quantity. - Price flag enabled and disabled. - Multiple godowns. - Cost
centre enabled and disabled. - Cash, credit, card and bank payment
modes. - Zero/empty optional additional-information rows. - Rollback
when a `PURT`, `PURD` or `STKDT` insert fails.

------------------------------------------------------------------------

# 11. Instructions for the AI coding agent

When asked to analyse or implement a pending desktop operation, follow
this protocol.

## 11.1 First identify the operation

Examples: - Create purchase invoice. - Update purchase invoice. - Delete
purchase invoice. - Purchase return. - Goods receipt. - Stock
adjustment. - Payment allocation. - Ledger posting.

Do not assume two operations share the same business rules merely
because they use similar tables.

## 11.2 Trace the complete flow

For the requested operation, inspect in this order: 1. Form/page and
user action event handler. 2. Validation methods. 3. Calculation
methods. 4. Service/controller orchestration if an API already exists.
5. Database helper methods. 6. SQL queries and table writes. 7.
Dependent lookup methods. 8. Configuration flags and global variables.
9. Error handling and transaction behaviour. 10. Post-save effects.

Follow every called method that materially changes data or calculations.

## 11.3 Maintain an evidence ledger

For every finding, record:

  Field            Required information
  ---------------- ----------------------------------------
  Operation        Name of desktop operation
  Source file      Exact file path
  Method           Exact method name
  Evidence         Relevant code/query
  Tables read      Table names
  Tables written   Table names
  Business rule    What the code does
  API mapping      DTO/service/repository destination
  Status           Confirmed/partial/pending/not in scope
  Open question    Missing detail, if any

Do not invent source file paths or database columns.

## 11.4 Identify missing work

When the user asks for pending operations: - Read this document. -
Inspect the current source files and repository implementation. -
Compare them with the evidence ledger and checklist. - Report only work
that is genuinely incomplete or unverified. - Separate missing desktop
analysis from missing API implementation and missing testing.

When the user asks for the full flow: - Present the end-to-end
sequence. - Include methods, validations, SQL reads/writes, table
relationships and transaction boundaries. - Identify conditional
branches and configuration flags. - Mark inferred steps clearly.

## 11.5 Implement safely

-   Follow the existing API architecture and DTO contracts.
-   Preserve legacy validations and calculation order.
-   Use parameterized SQL.
-   Use one transaction for related writes.
-   Use authenticated user identity, not a client-provided user ID.
-   Never replace an unknown legacy formula with an assumed formula.
-   Never invent table columns or helper method signatures.
-   Ask for only the specific missing source needed to resolve a
    blocker.
-   Keep explicitly excluded work out of scope.
-   Add verification and parity tests.

## 11.6 Update this document after analysis

After each analysis or implementation: 1. Update the operation's status.
2. Add newly confirmed methods and queries. 3. Add table read/write
mappings. 4. Record validations and conditional branches. 5. Remove
resolved open questions. 6. Add new unresolved questions. 7. Record
files changed and tests performed. 8. Never change `Pending` to
`Verified` without test evidence.

------------------------------------------------------------------------

# 12. How to ask the agent

Use prompts like these with this Markdown file attached or available in
the project.

### Find pending work

> Read the QuickBalance Desktop Application Analysis Guide. Inspect the
> current Purchase Invoice desktop and API code. List all pending
> analysis, implementation and testing tasks. Separate confirmed facts
> from assumptions. Do not modify code yet.

### Trace a complete operation

> Trace the complete Purchase Invoice save flow from the desktop Save
> button to every database write. Follow all called methods,
> calculations, validation and configuration branches. Update the
> evidence ledger and identify missing source code.

### Analyse a specific method

> Analyse `TaxSummary()` using the original source. Explain every input,
> calculation, rounding step, output and database dependency. Map the
> output to the Purchase Invoice DTOs and `PURD` insert. Update this
> guide with confirmed findings.

### Implement an operation

> Implement Purchase Invoice creation according to this guide and the
> actual legacy source. Preserve the existing repository interface and
> API conventions. Do not guess missing rules. Use a single SQL
> transaction and verify all required writes. Keep `STKDTPHY` out of
> scope.

### Review an implementation

> Compare the current PurchaseInvoiceRepository implementation against
> this guide and the desktop source. Identify missing columns, incorrect
> calculations, incorrect defaults, omitted validations, transaction
> gaps and unverified side effects. Do not mark anything verified
> without evidence.

### Continue from previous analysis

> Read this guide and the current source. Continue from the unresolved
> questions and pending checklist. Do not repeat completed analysis.
> Update this document with new findings.

------------------------------------------------------------------------

# 13. Change log

  -----------------------------------------------------------------------
  Date                                Change
  ----------------------------------- -----------------------------------
  2026-11-25                          Created initial Purchase Invoice
                                      analysis guide from supplied DTOs,
                                      interfaces, repository example,
                                      legacy method descriptions and SQL
                                      column mappings.

  Pending                             Add exact source-file paths and
                                      complete calculation method bodies.

  Pending                             Complete and verify the `PURM`,
                                      `PURT`, `PURD` and `STKDT` API
                                      implementation.

  Pending                             Perform desktop/API calculation and
                                      database parity tests.
  -----------------------------------------------------------------------

------------------------------------------------------------------------

# 14. Important scope and accuracy rules

1.  This document is a living analysis record, not proof that the API
    implementation is complete.
2.  The original desktop source code is authoritative when a summary
    conflicts with actual code.
3.  The database schema is authoritative for actual column types,
    nullability, constraints and keys.
4.  The current DTOs are authoritative for the current API request
    contract until deliberately changed.
5.  `PURM`, `PURT`, `PURD` and normal `STKDT` are the agreed initial
    purchase-invoice create scope.
6.  `STKDTPHY` is handled separately.
7.  GRN creation, payment voucher creation and approval processing are
    excluded from this initial create operation.
8.  Do not use a preliminary subtotal as the final invoice amount.
9.  Do not claim exact legacy parity until calculations and persisted
    rows have been compared against the desktop application.
10. Every new discovery should be added to this guide so future analysis
    can continue without repeating completed work.
