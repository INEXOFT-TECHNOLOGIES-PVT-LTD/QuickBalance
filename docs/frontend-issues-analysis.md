# Frontend issues: analysis (26 Nov 2026)

Source: `docs/frontend-issues-26nov.md`. Context: `docs/rule3-plan.md` and `docs/rule4-plan.md` (Batches A–H, made 25 Nov).

Method: code and `QuickBalance.sql` only. No code changed, nothing run against the database. Paths are relative to the project root; `QuickBalance.sql` is `D:\PROJECT\Salman\Latest\QuickBalance\DB\ProjectDB\QuickBalance.sql`.

Order: regressions, bugs, validation, features, clarifications, then a suggested fix order.

Evidence limits:
- For 9.1 and 8.3/9.2 the real runtime exception is not visible in code. 9.1 is a code-level conclusion that matches the symptom; 8.3/9.2 is unresolved until the server log line is read.
- `QuickBalance.sql` is the schema script. The deployed database may differ.

---

## A. Regressions (caused by yesterday's batches)

### 9.1 `POST /customers` returns 500 "Error updating customer."

**FIXED (26 Nov).** `CustomerDB.cs` `GetDefaultCustomerLedgerGroup`: `useDefault = (ledgerGroupId ?? 0) <= 0` and the parameter is never null (`?? 26`). `CustomerController` create catch now logs with `_logger.LogError` and returns "Error creating customer."; the update catch logs with `_logger.LogError` instead of `Console.WriteLine`. C# only.

| field | value |
|---|---|
| type | bug (regression) |
| root cause | `GetDefaultCustomerLedgerGroup(int? ledgerGroupId)`, `DB/CustomerDB.cs:946-984`. With `ledgerGroupId` null, `bool useDefault = ledgerGroupId <= 0` is `false` (a null comparison), so the parameter becomes `useDefault ? 26 : ledgerGroupId`, which is null. `SqlParameter.Value = null` means "not supplied", so SQL Server fails with *the parameterized query expects the parameter '@GroupId', which was not supplied*. The controller's catch-all turns it into a 500. |
| caused by yesterday's batches? | **Yes, Batch F.** `ledgerGroupId` on the customer request became `int?` (`DTOs/CustomerRequest.cs:35-36`). Before, it was `int` (omitted = 0, so default group 26 was used). The controller got `?? 0` only for the effective group (rule4-plan.md:252), not for the call at `Controllers/Master/CustomerController.cs:213-215`. |
| proposed fix | In `GetDefaultCustomerLedgerGroup`: `bool useDefault = (ledgerGroupId ?? 0) <= 0;` and pass `ledgerGroupId ?? 26`. Also change the create catch message to "Error creating customer." and log through `ILogger` instead of `Console.WriteLine`. |
| needs SQL query change? | No (C# only) |
| breaking for frontend? | No (restores the old omitted-field behaviour) |
| effort | S |

**Note (26 Nov, salesman lookup).** `ValidateCustomerLookupsAsync` checked `salesManId` against `SMGRP` (salesman group). It now checks `SMMAST.SMCODE` (salesman): create requires `ISNULL(ACTIVE,1)=1 AND ISNULL(DELFLG,0)=0`; update (new `isUpdate` parameter, passed from the update action) requires only `ISNULL(DELFLG,0)=0`, so a customer with a later-deactivated salesman can still be saved. The check still runs only when `salesManId > 0`, and the error message is unchanged. `GetCustomerSalesmen` (`CustomerDB.cs` ~3168) reads from `SALESMANMAS`, which does not exist in `QuickBalance.sql`; it was left untouched and looks dead or broken (`GetSalesmen`, on `SMMAST`, is the working lookup).

The catch block that produces the message (`Controllers/Master/CustomerController.cs:260-271`, in `CreateCustomer`; the text was copied from `UpdateCustomer`):

```csharp
catch (Exception ex)
{
    //_logger.LogError(ex, "Error updating customer with ID {CustomerId}", id);
    Console.WriteLine($"Customer update failed: {ex}");

    return StatusCode(
        StatusCodes.Status500InternalServerError,
        ApiResponseWithError.Failure("Error updating customer."));
}
```

The real exception is only written to the console (:266). To confirm the cause, look for "Customer update failed:" in the server console output.

Other Batch F changes checked on the create path (none produces the 500):

| Batch F change | result |
|---|---|
| `stateCode` `int?` | A null is rejected earlier with 400 "Please select a valid region." (`CustomerDB.cs:1125`). `AddCustomerParameters` (:90) only runs after that. |
| `salesManId` `int?` | Written as `DBNull` (`CustomerDB.cs:113`); the lookup runs only when `> 0` (:1153). |
| `ledgerGroupId` `int?` | **The cause** (above). |
| `pendingBills[].invoiceDate/dueDate` `DateTime?` | Null-checked in `InsertPendingBillAsync` (`CustomerDB.cs:1555-1559`), throws `ArgumentException`, which returns 400, not 500. |
| `crDr` normalisation | Setter upper-cases (`DTOs/CustomerRequest.cs:139-144`); `DrCr` is trimmed and upper-cased before every check (`CustomerDB.cs:1422,1567`). |
| `isActive` `JsonPropertyName` | Binds to `Active` correctly (`CustomerRequest.cs:32-33`). |

Side finding (not a regression, to confirm): `ValidateCustomerLookupsAsync` checks `salesManId` against `SMGRP.SMGRPCODE`, which is the salesman-**group** table (`DB/CustomerDB.cs:1153-1160`), not `SMMAST`. Check against the legacy customer form before changing.

---

## B. Bugs

### 2.1 `POST /ledger-groups` returns "Group Code already in use." with no `groupCode` in the body — **FIXED (2026-11-26)**

| field | value |
|---|---|
| type | bug |
| root cause | Create **ignores** `request.GroupCode` and generates a code (`Controllers/Master/LedgerGroupController.cs:167-241`). For a sub-group the code is `highest active child + 1`, or, when the parent has no active children, `parent.GroupCode + 1` (:233-241). The duplicate check `GetAllByGroupCode` (`DB/LedgerGroupDB.cs:436-448`, `GDELFLG = 0 AND GACTIVE = 1 AND GROUPCODE = @GroupCode`) looks at **all** active groups. `parent + 1` is usually already the code of the parent's next sibling, so the check fires. The child lookup (`GetSubGroupsOrderByGrpCd`, :536-552) only sees children of this parent, so generation and check disagree. Repro: `mainGroup: false`, `mainGroupId: 59`, where group 59 has no active children. |
| caused by yesterday's batches? | **No.** Batch C made `groupCode` `int?` (`DTOs/LedgerGroupRequest.cs:14`) but the create action never reads it. The `request.GroupCode` reads are in the update paths (`LedgerGroupController.cs:436,497`). |
| proposed fix | Generate a free code: loop `+1` until `GetAllByGroupCode` returns 0, or take the max code in the parent's range. Return "Group Code already in use." only when the client **supplies** a code. The numbering rule for a sub-group under a non-main parent needs the owner's agreement. |
| needs SQL query change? | No (a C# loop is enough; an optional max-code query is a small addition) |
| note (2026-11-26, finalAccount rule) | Sub-group create (`LedgerGroupController.Create`), final rule: if the parent has a valid `finalAccount` (P/T/B/M) it is always used and the request value is ignored (as before); otherwise a `finalAccount` sent in the request is used (uppercased, must be P/T/B/M, else "Invalid Final Account selection"); otherwise 400 "Final Account is required for this group (B, P, T or M)." Main-group create and the update action are unchanged. CR/DR is still inherited from the parent. |
| breaking for frontend? | No |
| effort | M |

### 8.1 `DELETE /suppliers/{id}` for an inactive supplier returns "Supplier not found." — **FIXED (2026-11-26)**

| field | value |
|---|---|
| type | bug |
| root cause | `GetSupplierByIdExist` filters `S.ACTIVE = 1` (`DB/SupplierDB.cs:693-699`). Its only caller is the delete action (`Controllers/Master/SupplierController.cs:796-803`), which returns "Supplier not found." when it gets nothing. The delete UPDATE itself has no ACTIVE filter (`SupplierDB.cs:3825-3834`). |
| caused by yesterday's batches? | No |
| proposed fix | Remove `S.ACTIVE = 1` from that query (keep the `DELFLG` check), or drop the pre-check, because `DeleteSupplierWithValidations` already returns NotFound. |
| needs SQL query change? | **Yes** (one WHERE clause) |
| breaking for frontend? | No |
| effort | S |

### 5.1 Salesman group: deleted name cannot be reused — **FIXED (2026-11-26)**

| field | value |
|---|---|
| type | bug |
| root cause | `GroupNameExistsAsync`, create and update variants, is `WHERE SMGRPNAME = @GroupName` with no delete filter (`Repository/Master/SalesManGroupRepository.cs:92-137`). Delete is soft (`SET DELFLG = 1`, :281). `SMGRP` has no UNIQUE index (`QuickBalance.sql:4444-4457`); `DELFLG` is `NOT NULL` with default 0 (:5086). |
| caused by yesterday's batches? | No |
| proposed fix | Add `AND ISNULL(DELFLG, 0) = 0` to both queries. |
| needs SQL query change? | **Yes** |
| breaking for frontend? | No |
| effort | S |

### 6.1 Salesman: deleted name cannot be reused — **FIXED (2026-11-26)**

| field | value |
|---|---|
| type | bug |
| root cause | `NameExistsAsync`, create and update variants, has no delete filter (`Repository/Master/SalesManRepository.cs:656-704`, `FROM SMMAST WHERE SMNAME = @SmName`). Delete is soft (`DELFLG = 1`, :714). No UNIQUE index on `SMMAST` (`QuickBalance.sql:4461+`). |
| caused by yesterday's batches? | No |
| proposed fix | Add `AND ISNULL(DELFLG, 0) = 0` to both queries. |
| needs SQL query change? | **Yes** |
| breaking for frontend? | No |
| effort | S |

### 4.1 Location group: deleted name cannot be reused (not reproducible from code) — **FIXED (2026-11-26)** (duplicate check now uses `DELFLG`)

| field | value |
|---|---|
| type | bug (unconfirmed) |
| root cause | The duplicate check filters `ACTIVE = 1` (`Repository/Master/GdwGrpRepository.cs:300-345`), and delete sets `ACTIVE = 0, DELFLG = 1` (:271-294), so by the code a deleted name **is** reusable. No UNIQUE index on `GDWGRP` (`QuickBalance.sql:2246-2258`); `DELFLG` has default 0 (:5079). The check has the opposite flaw: an inactive but not-deleted group does not block a duplicate. Possible explanations: the tested record was deactivated rather than deleted, the deployed DB differs from the script, or an older build was tested. The frontend's exact request and response are needed. |
| caused by yesterday's batches? | No |
| proposed fix | Make the check consistent: `ISNULL(DELFLG, 0) = 0` instead of `ACTIVE = 1`, in both variants. |
| needs SQL query change? | **Yes** |
| breaking for frontend? | No |
| effort | S |

### 8.3 and 9.2 `POST /party-groups` returns "Unable to create party group."

| field | value |
|---|---|
| type | bug (root cause not visible in code) |
| root cause | The message comes from the catch-all in `Services/PartyGroupService.cs:70-76`. It swallows the exception and only logs it: `_logger.LogError(ex, "Error creating party group.")`. The insert is `Repository/Master/PartyGroupRepository.cs:51-82` (`GROUPNAME, GROUPSNAME, GROUPTYPE`). **`groupShortName` is not required**: it is `string?` (`DTOs/PartyGroup/PartyGroupRequest.cs:11-12`), the column `GROUPSNAME` is nullable, and a null is passed. `QuickBalance.sql:2838-2846` defines `ID IDENTITY, GROUPNAME, GROUPSNAME, GROUPTYPE, ISDELETED, LOGCD, UTIME`, all nullable, so the insert is valid against the script. Most likely cause: the deployed table differs from the script (rule4-plan.md:240 lists the short-name column as "not verified"), or a permissions or other SQL error. |
| caused by yesterday's batches? | Probably no. Batch C added `GET /party-groups/{id}` and list fields; the create path is unchanged (unconfirmed). |
| proposed fix | Step 1: read the server log line "Error creating party group." for the real SQL error (no database access needed from us). Step 2: fix the column name or insert to match. Also return the real reason as the frontend asked (rethrow, as `Services/LocationGroupService.cs:67-68` does, or return a safe message). `groupName` is `[StringLength(20)]` but the column is `nvarchar(250)`; decide the intended limit. |
| needs SQL query change? | Maybe (depends on the log) |
| breaking for frontend? | No |
| effort | S–M |

---

## C. Validation

### 6.2a Salesman: `email` is required on save

**FIXED (26 Nov).** `SalesmanRequest.Email` is now `string?`. The service already trims with `?.` and checks the format only when the value is not blank.

| field | value |
|---|---|
| type | validation |
| root cause | `SalesmanRequest.Email` is a non-nullable `string` with default `""` (`DTOs/SalesMan/SalesmanRequest.cs:21-22`). The project has `<Nullable>enable</Nullable>` and `Program.cs` does not set `SuppressImplicitRequiredAttributeForNonNullableReferenceTypes`, so MVC adds an implicit `[Required]`. A null or empty email then fails model validation ("The Email field is required."). The service itself only checks the format when the value is non-blank (`Services/SalesmanService.cs:424-434`). |
| caused by yesterday's batches? | Not caused, but **Batch B's "optional strings are `string?`" pass missed `Email`** (rule4-plan.md:208). |
| proposed fix | `public string? Email { get; set; }` (the service already uses `request.Email?.Trim() ?? ""`). Confirm by sending a body without `email`. |
| needs SQL query change? | No |
| breaking for frontend? | No |
| effort | S |

### 6.2b Salesman: `accountId` required when `connectWithAccount` is false

**FIXED (26 Nov).** `SalesmanService` create: the `GetAccountById` lookup now runs only inside `if (connectWithAccount)`.

| field | value |
|---|---|
| type | validation (logic bug) |
| root cause | `Services/SalesmanService.cs:70-75` calls `_account.GetAccountById(request.AccountId)` **unconditionally**. With `accountId = 0` it returns 0, so the response is "Account not found." The check at lines 63-67 correctly requires an account only when connecting. The update path is fine (`:257-258`). |
| caused by yesterday's batches? | No |
| proposed fix | Move the account lookup (:70-75) inside `if (connectWithAccount) { ... }`. |
| needs SQL query change? | No |
| breaking for frontend? | No |
| effort | S |

### 8.2 Supplier: `country` is required on save — **FIXED (2026-11-26)** (kept required; `[Range(1, int.MaxValue)]` added to `countryCode` and `stateCode` on the supplier and customer request DTOs, no logic change)

| field | value |
|---|---|
| type | validation / clarification |
| root cause | `DB/SupplierDB.cs:1076-1092` returns "Please select a valid country." when `CountryCode <= 0`, then looks the country up in MasterDB `COUNTRY`. A region is also required (`StateCode <= 0`, :1095-1097). The DTO field is a plain `int` with no attribute (`DTOs/SupplierRequest.cs:42`), so the schema shows it as optional. Customers apply the same rule (`CustomerDB.cs:1106`). |
| needed by later logic? | Not technically. Country and region are stored (`SUPMAS.COUNTRYCODE`, `STATECODE`) for the address/VAT fields. Stored procedures in `QuickBalance.sql` compare `COUNTRYCODE <> 0 AND COUNTRYCODE <> (SELECT TOP 1 COUNTRY FROM COMPMAS)` (for example :5425), so 0 is explicitly tolerated. It is a business-rule choice, not a technical need. |
| caused by yesterday's batches? | No |
| proposed fix | Owner decision: (a) keep it required and mark it in the DTO (`[Range(1, int.MaxValue)]`) so Swagger matches, or (b) make country and region optional by skipping those checks when 0. Apply the same decision to Customers. Check the legacy supplier form before choosing. |
| needs SQL query change? | No |
| breaking for frontend? | (a) No, (b) No |
| effort | S |

---

## D. New features

### 1.2 `GET /item-group/primary-groups`: confirm endpoint, add paging and search — **FIXED (2026-11-26)**

| field | value |
|---|---|
| type | new feature (plus a clarification) |
| confirmation | **Yes, this is the correct endpoint** for the Primary Group dropdown. |
| current state | `Controllers/Master/ItemGroupController.cs:121-133` takes no parameters and returns the whole active list, `ApiResponseWithoutPagination`. Query (`Repository/Master/ItemGroupDBs.cs:329-342`): `SELECT … FROM dbo.PRGRPMAS WHERE ISNULL(DELFLG,0)=0 AND ACTIVE=1 AND PRIMGRPFLG=1 ORDER BY GRPNAME`. |
| caused by yesterday's batches? | No |
| proposed fix | Accept `[FromQuery] PaginationRequests` (`page`, `pageSize`, `search`; `DTOs/PaginationRequest.cs:51-58`). Add `AND (@Search IS NULL OR GRPNAME LIKE @Search)`, `OFFSET/FETCH`, and a separate `COUNT(*)`; return `ApiResponse<…>` with `pagination` (same pattern as the list query in the same file). Compatibility: `PaginationRequests` defaults to `pageSize = 20`, which would silently truncate existing callers; keep the unpaged behaviour when `page` is not sent, or document the new default. |
| sample request | `GET /api/v1/item-group/primary-groups?page=1&pageSize=20&search=food` |
| needs SQL query change? | **Yes** (search, paging, count) |
| breaking for frontend? | Additive if unpaged stays the default; otherwise the response gains `pagination` and truncates at 20 |
| effort | M |

---

## E. Clarifications (answer only)

### 1.1 Item group status filter

**Already works.** `GET /api/v1/item-group?active=true` returns active items, `?active=false` inactive ones, and omitting it returns both. Optional `&page=1&pageSize=20&search=abc`. Example:

`GET /api/v1/item-group?page=1&pageSize=20&active=false`

Controller: `Controllers/Master/ItemGroupController.cs:55-63` (`bool? active`). SQL: `Repository/Master/ItemGroupDBs.cs:148` (`@Active IS NULL OR ISNULL(ACTIVE,0) = @Active`). Rule 3 (rule3-plan.md:16) reached the same conclusion. Type: clarification. The frontend only needs to send the query parameter. Effort: S (reply).

### 3.2 Accounts status filter

**Already works.** `GET /api/v1/accounts?active=true`, `?active=false`, omitted = both (`Controllers/Master/AccountLedgerController.cs:48-50`, `[FromQuery] bool? active`; the list query uses `@Active IS NULL OR …`, rule3-plan.md:9). The response field is `isActive` since Batch G. Example:

`GET /api/v1/accounts?page=1&pageSize=20&active=true`

Type: clarification. Effort: S (reply).

### 3.1 `taxReturnFlag`

Answer: **manual, not system-generated.**

- Request: `bool TaxReturnFlag = false` (`DTOs/AccountLedgerRequest.cs:91`).
- Create and update copy it to `ClsAccMasT.TaxRetFlag` (`Controllers/Master/AccountLedgerController.cs:385` create, `:700` update).
- It is saved to `ACCMAS.TAXRETFLAG`, `bit NOT NULL` (`QuickBalance.sql:541`; insert/update in `DB/AccMasDB.cs:1251-1355`).
- It is read back in `DB/AccMasDB.cs:437,490,625` and returned as `taxReturnFlag` (Batch G).
- **Nothing else in the API uses it**: no calculation, no filter, no report.
- Accounts created automatically for customers and suppliers hard-code it to `0`/`false` (`DB/CustomerDB.cs:1197-1229`, `DB/SupplierDB.cs:4555`).

Recommendation: the UI can leave it out (the default `false` is saved), or show a checkbox on the account form if the business uses tax-return reporting. Whether the legacy desktop app uses it in reports was not checked (stay-in-project rule). Type: clarification. Effort: S (reply).

### 7.1 BOM `isActive` — **FIXED (2026-11-26)** (`BomRequest.IsActive` is now `bool?`; create writes `?? true`, update writes `ACTIVE = ISNULL(@IsActive, ACTIVE)`)

| operation | behaviour |
|---|---|
| create | Default **true** (`DTOs/BomRequest.cs:51`); written as `ACTIVE = @IsActive` (`Repository/Master/BomRepository.cs:81-107`). |
| update | Also writes `ACTIVE = @IsActive` (`BomRepository.cs:136-156`). Because the request default is `true`, an update that omits `isActive` **re-activates an inactive BOM**. |
| list / get-by-id | Return `isActive` (`BomRepository.cs:184,324`); the list supports `?active=`. |
| delete | `ISDELETED = 1, ACTIVE = 0` (`BomRepository.cs:527`). |

Recommendation: the UI shows an Active toggle on the edit form (create defaults to on). Optionally make the update request field `bool?` so an omitted value keeps the current status. Type: clarification (plus an optional tweak). Effort: S.

---

## Suggested fix order (max 3 issues per batch)

| batch | issues | note |
|---|---|---|
| 1 | 9.1 | The only regression. Fix the null ledger group and the catch message. |
| 2 | 8.1, 5.1, 6.1 | One-line SQL filters. |
| 3 | 6.2a, 6.2b, 4.1 | Salesman validation, and the location-group check once the frontend supplies a repro. |
| 4 | 8.3, 9.2 | Party groups: read the log first (one fix covers both issues). |
| 5 | 2.1 | Ledger-group code generation; needs the numbering rule agreed. |
| 6 | 1.2, 8.2, BOM update `isActive` | Paging/search feature, country/region decision, optional tweak. |
| no code | 1.1, 3.1, 3.2, 7.1 | Reply to the frontend with the answers and example requests above. |

## Open items

- Real exceptions for 9.1 (console line "Customer update failed:") and 8.3/9.2 (log line "Error creating party group.") were not available to read.
- 4.1 cannot be reproduced from code.
- The legacy WinForms behaviour for `taxReturnFlag` and the supplier country rule was not checked.
- The `salesManId` lookup against `SMGRP` in `CustomerDB.cs:1153-1160` needs confirmation.

## Update 26 Nov (retest follow-up)

### Problem 2 – Ledger group free-code check (FIXED, build 0 errors)

- `LedgerGroupDB.GetAllNonDeletedByGroupCode(int)` is new: `WHERE GDELFLG = 0 AND GROUPCODE = @GroupCode` (no `GACTIVE` filter).
- `LedgerGroupController.Create` (sub-group free-code loop) now uses it, so a code held by an inactive group (e.g. group 46) is skipped.
- `GetAllByGroupCode` is unchanged (still used by the top-level create duplicate check).
- Re-test 2.1 Test 1: the new group's `groupCode` must differ from group 46's code.

### Problem 1 – Supplier create rejects ledger groups (ANALYSIS ONLY, awaiting approval)

Cause (`Controllers/Master/SupplierController.cs:235-258`, `DB/SupplierDB.cs`):
- Create ignores `request.LedgerGroupId`. It always calls `GetDefaultSupplierLedgerGroup()` (`SupplierDB.cs:4342`: `GROUPID = 27 AND GROUPCODE = '20000'`). That returns a row, because the error is not "Default supplier ledger group was not found".
- It then calls `ValidateSupplierLookupsAsync` (`SupplierDB.cs:1019-1024`): `GROUPID = @id AND ISNULL(GDELFLG,0) = 0 AND ISNULL(GACTIVE,1) = 1`. For 27 this returns 0 rows, so group 27 has `GACTIVE = 0` or `GDELFLG <> 0`. This was not checked against the DB, because no DB access was used.
- That is why the retry with `ledgerGroupId: 27` fails identically: the value is never read.
- Lookup (`SupplierDB.cs:4200-4204`): `GACTIVE = 1 AND (GROUPID = 27 OR MGROUP = 33)`. 27 is missing, which confirms `GACTIVE <> 1` (NULL fails `= 1`). It lists 7, 2, 6, 34, so those are the groups under MGROUP 33.
- The two rules differ: validation treats NULL `GACTIVE` as active, the lookup does not. Neither one is the same as the group the create actually uses.
- Customers: `CustomerDB.GetDefaultCustomerLedgerGroup(int? id)` accepts an optional id, defaults to 26 and requires `GACTIVE = 1`. Suppliers have the default (27 / '20000') hard-coded, but the optional id was dropped. A commented-out call at `SupplierController.cs:1507` (`GetDefaultSupplierLedgerGroup(request.LedgerGroupId)`) shows the earlier version took the id.

Proposed fix (not applied):
1. One shared predicate for lookup and validation: `ISNULL(GDELFLG,0) = 0 AND ISNULL(GACTIVE,1) = 1 AND (GROUPID = 27 OR MGROUP = 33)`.
2. `GetDefaultSupplierLedgerGroup(int? id)` like customers: use `id` when it is > 0 (checked with the shared rule), otherwise default to 27.
3. The controller passes `request.LedgerGroupId`, and validation uses the same rule.
4. If group 27 really has `GACTIVE = 0` in the DB, the rule cannot accept it without either dropping the `GACTIVE` check for 27 or fixing that row (a data change, which is out of scope). Need a decision.

### Problem 1 – Supplier ledger group (FIXED 26 Nov, build 0 errors, not run against the DB)

Applied exactly as proposed, with no exemption for group 27 and no data change:
1. `SupplierDB.SupplierLedgerGroupRule` is one shared predicate: `ISNULL(GDELFLG,0) = 0 AND ISNULL(GACTIVE,1) = 1 AND (GROUPID = 27 OR MGROUP = 33)`. It is used by `GetLedgerGroups` (the lookup), `ValidateSupplierLookupsAsync` and `GetDefaultSupplierLedgerGroup`.
2. `GetDefaultSupplierLedgerGroup(int? ledgerGroupId)` uses the given id when it is > 0. Otherwise it defaults to group 27 with code `20000`, like customers' 26.
3. `SupplierController.Create` passes `request.LedgerGroupId`. A bad id returns "Selected supplier ledger group does not exist or is inactive."

Expected after the fix:
- If group 27 is active, create without `ledgerGroupId` works and 27 appears in the lookup.
- If group 27 is still inactive or deleted in the DB, create without `ledgerGroupId` still returns "Default supplier ledger group was not found." The code does not exempt 27 from the active check, so the row needs fixing in the DB (a data change, out of scope).
- Passing an id from `GET /suppliers/lookups/ledger-groups` (7, 2, 6, 34) should work.
Retest 8.1: create, then delete, a supplier, once with no id and once with a lookup id.

### Problem 1 follow-up – retest 3 and 5 failed (26 Nov, build 0 errors, not run against the DB)

Finding: the Problem 1 fix (items 1-3 above) is already in the code. The controller passes `request.LedgerGroupId` (`SupplierController.cs:235`). `GetDefaultSupplierLedgerGroup(int?)` uses the shared rule (`SupplierDB.cs:4343-4382`). `InsertSupplier` passes the resolved `ledgerGroupId` to the `ACCMAS` insert as `MGROUP` (`SupplierDB.cs:1689`), so the supplier and its account use the same group. The DTO property `LedgerGroupId` binds to JSON `ledgerGroupId`.

Likely cause of the retest result: the API process that was tested was started before the fix. Both 7 and invalid 99999 gave 27, which is the old hard-coded default. A running instance (PID 17700) still locks `bin\Debug\net10.0\QuickBalance.API.exe`, so a plain `dotnet build` fails with MSB3027. Stop and restart the API before retesting.

One real defect found and fixed: `DTOs/SupplierRequest.cs` had `[Range(1, int.MaxValue)]` on `LedgerGroupId`, so `0` returned a model-validation 400 instead of defaulting to 27. It is now `[Range(0, int.MaxValue)]`.

Behaviour now:
- `ledgerGroupId` omitted or 0 -> 27.
- Valid id (passes the shared rule) -> used for the supplier and its linked account.
- Invalid id (e.g. 99999) -> 400 "Selected supplier ledger group does not exist or is inactive."
- Negative id -> 400 from model validation.
Note: whether 7 is valid depends on it being under MGROUP 33 and active in the DB. The lookup endpoint lists the valid ids.
