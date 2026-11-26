# Rule 4 plan
**Batch H: BREAKING, not yet released to frontend**

**Batch G: BREAKING, not yet released to frontend**

**Batch F: BREAKING, not yet released to frontend**

**Batch E: BREAKING, not yet released to frontend**

**Batch D: BREAKING, not yet released to frontend**

Rule 4: a field has the same name AND type in the request body and in the response (get-by-id), so an edit form can load a record and save it back without renaming.

Scope: every master endpoint with POST/PUT. Excluded: `active`/`isActive` (see `docs/rule3-plan.md`), legacy and hidden controllers (categories, currenciesssss, makedblank), sales transactions and reports.

Facts come from reading the DTO and entity classes, not only `docs/api-audit.md`. No code was changed and nothing was run against the database. Line numbers refer to the code as it stands after rule 3 Batch 1 (Units, Boms, Regions), so Bom and Region lines may move by a few.

Legend: **req** = request DTO, **res** = get-by-id response. **JP** = `[JsonPropertyName]` on the wire only; the C# property and SQL alias stay as they are. **Breaking** = the frontend must change.

---

## Part A: name or type differences

### Accounts (`/accounts`)

req `AccountLedgerRequest` and `AccountLedgerUpdateRequest` (nested class, adds `rowVersion`). res `AccountLedgerDtos`: get-by-id uses `AccMasDB.SearchByIDs`, which returns `AccountLedgerDtos`. The longer `AccountLedgerDto` is used only by list and report queries, not get-by-id.
Mechanism: JP on the response properties. Dapper maps by C# name, so the SQL aliases stay.

| field (req → res) | type (req → res) | proposed | breaking | files |
|---|---|---|---|---|
| accountName → accName | string → string? | `accountName` | yes (res) | `DTOs/AccountLedgerRequest.cs:63`; `DTOs/Respons-DTO/AccountLedgerDto.cs:9` |
| secondName → secName | string? → string? | `secondName` | yes (res) | `AccountLedgerRequest.cs:66`; `AccountLedgerDto.cs:16` |
| groupId → ledgerGroupId | int → int? | `groupId` (request name; response `ledgerGroupId` is renamed, D3) | yes (res) | `AccountLedgerRequest.cs:79`; `AccountLedgerDto.cs:18` |
| openingBalance → openBal | decimal? → decimal? | `openingBalance` | yes (res) | `AccountLedgerRequest.cs:105`; `AccountLedgerDto.cs:26` |
| openingBalanceType → none | string (CR/DR) → missing | add `openingBalanceType` to the response. Check where CR/DR is stored before coding | yes (additive) | `AccountLedgerRequest.cs:110`; `DB/AccMasDB.cs:604-640` |
| vatPercentage → vatPerc | decimal? → decimal? | `vatPercentage` | yes (res) | `AccountLedgerRequest.cs:89`; `AccountLedgerDto.cs:31` |
| taxReturnFlag → taxRetFlag | bool → bool? | `taxReturnFlag`, bool | yes (res) | `AccountLedgerRequest.cs:91`; `AccountLedgerDto.cs:32` |
| bankAccountName → bankAccName | string? → string? | `bankAccountName` | yes (res) | `AccountLedgerRequest.cs:93`; `AccountLedgerDto.cs:34` |
| bankAccountNumber → bankAccNo | string? → string? | `bankAccountNumber` | yes (res) | `AccountLedgerRequest.cs:95`; `AccountLedgerDto.cs:35` |
| ibanNumber → ibanNo | string? → string? | `ibanNumber` | yes (res) | `AccountLedgerRequest.cs:99`; `AccountLedgerDto.cs:36` |
| currencyFlag → curFlg | bool → bool? | `currencyFlag`, bool | yes (res) | `AccountLedgerRequest.cs:112`; `AccountLedgerDto.cs:40` |
| currencyCode → curCode | int? → int? | `currencyCode` | yes (res) | `AccountLedgerRequest.cs:114`; `AccountLedgerDto.cs:42` |
| currencyConversionRate → curConvRate | decimal? → decimal? | `currencyConversionRate` | yes (res) | `AccountLedgerRequest.cs:117`; `AccountLedgerDto.cs:41` |
| none → acCode and accountCode | missing → string? (two keys; `accountCode` is a computed alias) | drop `acCode` from the response; keep `accountCode` read-only (D4) | yes if `acCode` is consumed | `AccountLedgerDto.cs:10,13` |
| rowVersion | byte[]? (update) → byte[]? | same | no | `AccountLedgerRequest.cs:148`; `AccountLedgerDto.cs:47` |

Unchanged: alias, address, phone, printName, depreciation, branch, swiftCode. Nullability-only: vatFlag, taxReturnFlag, currencyFlag (Part B).

### Products (`/products`)

req `ProductRequests` and `ProductUpdateRequests` (used by `PUT /{id}` and the `with-image` multipart forms; `ProductRequest` at `ProductRequest.cs:6` is not used by the controller). res `ProductResponse`.
Mechanism: JP on the response (Dapper DTO). On the request, replace the underscore JP names with plain names.

| field (req → res) | type (req → res) | proposed | breaking | files |
|---|---|---|---|---|
| desc → description | string? → string? | `description` (response name is clearer) | yes (req) | `DTOs/ProductRequest.cs:260`; `DTOs/Product/ProductResponse.cs:10` |
| catgId → categoryId | int? → int? | `categoryId` | yes (req) | `ProductRequest.cs:275`; `ProductResponse.cs:17` |
| brandId | int? → **string?** | `brandId`, int? in both | yes (res type) | `ProductRequest.cs:278`; `ProductResponse.cs:18` |
| notes → prNotes | string? → string? | `notes` | yes (res) | `ProductRequest.cs:376`; `ProductResponse.cs:72` |
| saleAccId → saleAcId | int? → int? | `saleAccId` (request name kept; only the response is renamed, D5) | yes (res) | `ProductRequest.cs:325`; `ProductResponse.cs:41` |
| purAcctId → purAcId | int? → int? | `purAcctId` (request name kept; only the response is renamed, D5) | yes (res) | `ProductRequest.cs:328`; `ProductResponse.cs:42` |
| `_BMPL`, `_BMPQ`, `_BMPW` → bmpl, bmpq, bmpw | decimal? → decimal? | `bmpl`, `bmpq`, `bmpw` (drop underscores; JP on request) | yes (req) | `ProductRequest.cs:378-388`; `ProductResponse.cs:74-76` |
| `_SLNOTRACK` → slNoTrack | bool? → bool? | `slNoTrack` | yes (req) | `ProductRequest.cs:396`; `ProductResponse.cs:81` |
| `_BMEQN` → bmEqn | string? → string? | `bmEqn` | yes (req) | `ProductRequest.cs:399`; `ProductResponse.cs:82` |
| opnStkQty → opnStock | decimal? → decimal? | `opnStkQty` | yes (res) | `ProductRequest.cs:425`; `ProductResponse.cs:25` |
| opnStkValue → opnStkval | decimal? → decimal? | `opnStkValue` | yes (res) | `ProductRequest.cs:428`; `ProductResponse.cs:26` |
| aprvlst → none | string? → missing (commented out in res) | add to the response if the column exists, else document as request-only (D6) | no if added | `ProductRequest.cs:410`; `ProductResponse.cs:85` |
| gdwnId, brCd → none | int? → missing | add to the response if the columns exist, else document as request-only (D6) | no if added | `ProductRequest.cs:419-422` |
| none → image | missing → string? | response-only; uploaded via the image endpoints (documented exception, D7) | no | `ProductResponse.cs:96` |
| none → logCd | missing → string? | response-only, internal; hide | no | `ProductResponse.cs:59` |
| none → priceCategories[] | missing → list | response-only (D8) | no | `ProductResponse.cs:103` |
| priceCategories[].catName, discStruct, levFrom, levTo, discPerc, effValue, appPrice | n/a → string?/decimal? | align with the price-category master levels: `categoryName`, `discountStructure`, `levelFrom`, `levelTo`, `discountPercent`, `effectiveValue`, `appliedPrice` | yes (res) | `ProductResponse.cs:132-144`; `DTOs/PriceCategoryRequest.cs:6-49` |
| aliases[].productId | missing → int | response-only extra; ignored on save | no | `ProductResponse.cs:119` |
| aliases[].qty, unitId, salesPrice, purPrice, alType, invUnitCode | nullable → non-null | non-null in both (Part B) | no | `ProductRequest.cs:441-469`; `ProductResponse.cs:116-129` |
| rowVersion | byte[]? (update only) → byte[] | same | no | `ProductRequest.cs:438`; `ProductResponse.cs:99` |

`aliases[].active` is rule 3 and is skipped.
Unchanged: prName, alias, printName, upcCode, skuCode, barCode, groupId, unit, altUnit, unitConv, convType, purPriceType, basePurPrice, altPurPrice, salesPriceType, baseSalePrice, baseMinPrice, altSalePrice, altMinPrice, costMeth, taxCatId, minQty, reOrdQty, maxQty, servFlg, supplierId, batchFlg, manDateFlg, expDateFlg, expDays, priceCatFlg, priceCatId, stock, secName, opnStkRate, curConvRate, currencyId, delFlg, posFlg, baseMrp, altMrp, aliases[].id/alCode/alName.

### Boms (`/boms`)

req `BomRequest` (with `BomItemRequest` and `BomAdditionalInfoRequest`). res `BomDetailsDto`: `BomMasterDto` under `bilofm_details`, plus `BomItemDto` and `BomAdditionalInfoDto`.

| field (req → res) | type (req → res) | proposed | breaking | files |
|---|---|---|---|---|
| flat header → `bilofm_details{...}` | flat → nested object | flat header in the response, with `items` and `additionalInfo` at top level | yes (res) | `DTOs/BomRequest.cs:203-205`; `Services/BomService.cs:332` |
| mcGenFlg | **"Y"/"N" string → bool** | bool in both; remove the regex; replace `IsYes()` | yes (req) | `BomRequest.cs:37,65,140`; `Repository/Master/BomRepository.cs:101,150`; `BomService.cs:575` |
| mcConsFlg | **"Y"/"N" string → bool** | bool in both | yes (req) | `BomRequest.cs:45,73,142`; `BomRepository.cs:103,152`; `BomService.cs:578` |
| items[].productId → prCode | int → int | `productId` | yes (res) | `DTOs/BomItemRequest.cs:21`; `BomRequest.cs:178` (`BomItemDto.PrCode`) |
| items[].quantity → qty | decimal → decimal | `quantity` | yes (res) | `BomItemRequest.cs:27` |
| items[].unitId → unit | int → int | `unitId` | yes (res) | `BomItemRequest.cs:32` |
| items[].type | string? → string? | same | no | `BomItemRequest.cs:37` |
| items[] extra response fields | n/a | `id, price, purPriceType, altPurPrice, mainUnit, altUnit, prName, baseSalePrice, altSalePrice`: response-only, ignored on save | no | `BomRequest.cs:178-194` |
| additionalInfo[].accountId, amount | int/decimal → int?/decimal? | same names; nullability (Part B) | no | `BomAdditionalInfoRequest.cs:14,18`; `BomRequest.cs:196-201` |
| none → logCd, uTime, productName, unitName | missing → returned | response-only, read-only | no | `BomRequest.cs:131-150` |

Header fields `bomName, alias, prCode, qty, unit, expences, mcGen, mcCons, secondName` already match. The item names differ from the header names (`productId` vs `prCode`), but rule 4 compares request with response, so the header is left alone.

### Customers (`/customers`)

req `CustomerRequest` and `CustomerUpdateRequest` (adds `rowVersion`). res `clsCustomer` (legacy class).
Mechanism: JP on the property. Do NOT rename the C# properties: they are used as SQL parameters (`DB/CustomerDB.cs:51,113`).

| field (req → res) | type (req → res) | proposed | breaking | files |
|---|---|---|---|---|
| salesMan → salesManId | int? → int | `salesManId`, int? (response name is clearer) | yes (req) | `DTOs/CustomerRequest.cs:79`; `CLS/clsCustomer.cs:43`; `DB/CustomerDB.cs:51,113,234` |
| crDr | default "CR" → default "DR"; casing differs (`Cr`, `CR`, `DR`) | one value set: uppercase `CR`/`DR` in request and response, normalised in code on read and write, no database UPDATE (D12) | no for the request (regex is case-insensitive); yes for the response if the UI compares `"Cr"` | `CustomerRequest.cs:137`; `clsCustomer.cs:76`; `CLS/clsSupplier.cs:218` |
| pendingBills[] | `CustomerPendingBillRequest` → `CustomerPendingBillDto` | same names; response adds `currencyCode, conversionRate, branchId`; response dates nullable (Part B) | no | `CustomerRequest.cs:296-327`; `clsCustomer.cs:92-104` |
| response-only: accountId, accountCode, disc, priceLevel, prTmplateId, pntSysId, itemGroupId, grpItem, prListCd, prCategoryId, cityNameSec, districtSec, aprvLst, logId, stateArabicName | n/a | read-only. The binder ignores extra fields on save, so the round trip works (D9) | no | `clsCustomer.cs:13-71` |
| rowVersion | byte[]? → byte[]? | same | no | `CustomerRequest.cs:159`; `clsCustomer.cs:73` |

Unchanged: alias, cusName, address, phone, email, printName, countryCode, bnkAccName, bnkAccNo, branch, ibanNo, swiftCode, vatFlag, billFlag, crPerFlag, crLimitFlag, crLimit, vatRegNo, shipAddress, secName, curFlg, crPeriod, gccFlg, streetOne, streetTwo, buildingNo, plotId, cityName, postalCode, district, grpVatNo, einvIdType, einvId, vatType, partyGrpId, openBal, curCode, curConvRate.

### Suppliers (`/suppliers`)

req `SupplierRequest` and `SupplierEditRequest`. res `clsSupplierResponse` (`CLS/clsSupplier.cs:149`). `SupplierRequestUpdatedDto` (`DTOs/SupplierRequest.cs:165`) is not used by the controller, which takes `SupplierEditRequest` (`Controllers/Master/SupplierController.cs:341`); treat it as dead code.
Mechanism: JP, no C# renames.

| field (req → res) | type (req → res) | proposed | breaking | files |
|---|---|---|---|---|
| crDr | string, default `"Cr"` in both; Customers default `"DR"` | uppercase `CR`/`DR`, same rule as Customers: normalised in code, no database UPDATE (D12) | no (req); yes (res) if compared | `DTOs/SupplierRequest.cs:136`; `clsSupplier.cs:218` |
| pendingBills[] | `PendingBillRequest` → `SupplierPendingBillResult` | same names; response adds `accountId, currencyCode, conversionRate, branchId` | no | `DTOs/Respons-DTO/PendingBillRequest.cs:19`; `clsSupplier.cs:126-139` |
| response-only: accountId, disc, priceLevel, itemGroupId, grpItem, cityNameSec, districtSec, aprvLst, logId | n/a | read-only (D9) | no | `clsSupplier.cs:159-207` |
| rowVersion | byte[]? → byte[]? | same | no | `SupplierRequest.cs:160`; `clsSupplier.cs:209` |

Everything else matches by name and type: supName, alias, address, phone, email, ledgerGroupId (`int?` both), stateCode, printName, countryCode, bank fields, VAT fields, address fields, openBal, curCode, curConvRate, swiftCode. Caveat: I read `SupplierRequest` only up to line 200, which covers the live request class (lines 11-161).

### CurrencyConversion (`/currency-conversions`)

req `CurrencyConversionRequest {conversionDate, details[]}`. res: `GetConversionByIdAsync` returns `List<CurrencyConversionResponse>` (flat rows), `Services/CurrencyConversionService.cs:84`.

| field (req → res) | type | proposed | breaking | files |
|---|---|---|---|---|
| details[] → flat list of rows | nested → flat | response: `{conversionDate, details:[{id, currencyId, symbol, standardRate, sellRate, buyRate}]}` | yes (res) | `DTOs/CurrencyConversionRequest.cs:27-34`; `Controllers/Master/CurrencyConversionController.cs:78-128` |
| details[].currencyId → alternateCurrencyId | int → int? | `currencyId`, int | yes (res) | `CurrencyConversionRequest.cs:12`, `:43` |
| details[].standardRate, sellRate, buyRate | decimal → decimal? | same names; non-null decimal in both | no | `CurrencyConversionRequest.cs:16-24`, `:49-53` |
| none → baseCurrencyId | missing → int? | response-only | no | `CurrencyConversionRequest.cs:41` |
| conversionDate | DateTime? → DateTime? | same | no | `:29`, `:47` |

List rows (`GET /currency-conversions`) also use `alternateCurrencyId`. Rename them the same way so list and get-by-id agree (D10).

### PriceCategory (`/price-categories`)

req `PriceCategoryRequest {categoryName, levels[]}`. res `PriceCategoryResponse {id, categoryName, levels[]}`.

| field | type | proposed | breaking | files |
|---|---|---|---|---|
| levels[].id | missing → int | accept an optional `id` on request levels (update by id), or ignore it | no | `DTOs/PriceCategoryRequest.cs:6-49`; `DTOs/PriceCategoryResponse.cs:26` |
| levels[].categoryName | missing → string | drop from the response (it repeats the parent `categoryName`) | yes (res), if consumed | `PriceCategoryResponse.cs:28` |
| levels[].discountStructure, levelFrom, levelTo, discountPercent, effectiveValue, appliedPrice | same | same | no | `PriceCategoryRequest.cs:13-48`; `PriceCategoryResponse.cs:30-40` |

### PartyGroups (`/party-groups`)

req `PartyGroupRequest {groupName, groupShortName, groupType}` (`DTOs/PartyGroup/PartyGroupRequest.cs:5-19`). The controller has `GET` (list), `POST`, `PUT /{id}` and `DELETE /{id}` (`Controllers/Master/PartyGroupController.cs:29,45,60,74`). **There is no `GET {id}`.** The list returns `LookupItem {id, name}` (`Repository/Master/PartyGroupRepository.cs:146-194`).

| item | proposal | breaking | files |
|---|---|---|---|
| missing `GET /party-groups/{id}` | add it, returning `{id, groupName, groupShortName, groupType}` (new `PartyGroupResponse`) | no (additive) | `PartyGroupController.cs`; `PartyGroupRepository.cs`; new `DTOs/PartyGroup/PartyGroupResponse.cs`; `Interfaces/IPartyGroupInterface.cs` |
| list row `name` vs request `groupName` | add `groupName, groupShortName, groupType` to list rows; keep `name` for dropdown consumers | no (additive) | `PartyGroupRepository.cs:163-194`; `DTOs/LookupItem.cs` |
| groupType | string in both | same | no | `PartyGroupRequest.cs:18` |

The table has no ACTIVE column (rule 3 N/A, see `docs/rule3-plan.md`). Not yet checked: the short-name column in `QuickBalance.sql` near line 2838. Confirm it before coding.

### Regions (`/regions`)

req `RegionRequest {countryId, regionName, regionSecName}`. res `RegionResponse {countryId, regionId, regionName, regionSecName}`.

| field | type | proposed | breaking | files |
|---|---|---|---|---|
| regionId (res) | int? | `id`, int. JP on the response; the request has no id because it is in the route | yes (res) | `DTOs/RegionResponse.cs:6`; `Repository/Master/RegionRepository.cs:23,72,119`; `Controllers/Master/RegionController.cs:41` |
| countryId | int → int? | int in both (Part B) | no | `RegionRequest.cs:19`; `RegionResponse.cs:5` |
| regionName, regionSecName | string? in both | same | no | |

The dropdown query (`GetRegionsAsyncRecords`) returns the same `RegionResponse`, so the `regionId` rename also affects dropdown consumers (D11).

---

## Part B: nullability-only differences (non-breaking)

Make both sides the same. The wire format changes only in `null` versus a default value.

| master | field | req → res | files |
|---|---|---|---|
| Taxes | purchaseAccountId, purchaseReturnAccountId, salesAccountId, salesReturnAccountId, vatAccountId, taxAccountId | `int?` (with `[Required]`) → `int` | `DTOs/Tax/TaxMasterRequest.cs:24-49`; `TaxMasterResponse.cs:16-28` |
| Taxes | secondName, taxType | `string?` → `string` | `TaxMasterRequest.cs:52,60`; `TaxMasterResponse.cs:30,32` |
| Currency | baseFlg | `bool?` → `bool` | `DTOs/Currencies/CurrencyRequest.cs:38`; `CurrencyResponse.cs:16` |
| Salesman | employeeCode | `int?` → `int` | `DTOs/SalesMan/SalesmanRequest.cs:7`; `SalesmanResponse.cs:10` |
| Salesman | secondName, printName, address, phone | `string?` → `string` | `SalesmanRequest.cs:13-19`; `SalesmanResponse.cs:14-20` |
| SalesmanGroup | secondName, printName | `string?` → `string` | `SalesmanGroupRequest.cs:10-12`; `SalesmanGroupResponse.cs:9-11` |
| SalesmanGroup | logCode, updateTime (get-by-id only) | none → returned | `SalesmanGroupResponse.cs:15-17` (internal fields; hide them) |
| LedgerGroup | groupCode, sequenceNo | `int?` → `int` | `DTOs/LedgerGroupRequest.cs:14,32`; `DTOs/Respons-DTO/LedgerGroupDto.cs:21,26` |
| LedgerGroup | secondName, finalAccount, crDr | `string?` → `string` | `LedgerGroupRequest.cs:11,21,36`; `LedgerGroupDto.cs:20,22,27` |
| Accounts | vatFlag, taxReturnFlag, currencyFlag | `bool` → `bool?` | `AccountLedgerRequest.cs:85,91,112`; `AccountLedgerDto.cs:30,32,40` |
| Customers | ledgerGroupId | `int?` → `int` | `CustomerRequest.cs:35`; `clsCustomer.cs:16` |
| Customers | stateCode | `int` → `int?` | `CustomerRequest.cs:37`; `clsCustomer.cs:22` |
| Customers | pendingBills[].invoiceDate, dueDate | `DateTime` → `DateTime?` | `CustomerRequest.cs:304,320`; `clsCustomer.cs:95,99` |
| Suppliers and Customers | logId (response-only) | `int` on Suppliers, `string?` on Customers | `clsSupplier.cs:159`; `clsCustomer.cs:13` |
| Products | aliases[].qty, unitId, salesPrice, purPrice, alType, invUnitCode | `decimal?`/`int?` → `decimal`/`int` | `ProductRequest.cs:451-466`; `ProductResponse.cs:122-127` |
| CurrencyConversion | standardRate, sellRate, buyRate | `decimal` → `decimal?` | `CurrencyConversionRequest.cs:16-24,49-53` |
| Regions | countryId | `int` → `int?` | `RegionRequest.cs:19`; `RegionResponse.cs:5` |
| Boms | additionalInfo[].accountId, amount | `int`/`decimal` → `int?`/`decimal?` | `BomAdditionalInfoRequest.cs:14,18`; `BomRequest.cs:198-199` |

---

## Suggested order of batches (max 2 masters per batch, non-breaking first)

1. **Batch A (non-breaking): FIXED.** Taxes + Currency (nullability).
2. **Batch B (non-breaking): FIXED.** Salesman + SalesmanGroup. `employeeCode` is `int?` in both (the request may omit it: create auto-generates and update keeps the existing value, so `int` would break that; this differs from the `int` in Part B). Optional strings are `string?` in both. `logCode`/`updateTime` removed from `GET /salesman-groups/{id}` (class and SQL).
3. **Batch C (non-breaking): FIXED.** LedgerGroup + PartyGroups. LedgerGroup: `groupCode`, `sequenceNo` are `int?`, and `secondName`, `finalAccount`, `crDr` are `string?`, in both request and response (all are optional in the request: null or 0 means generate or keep, so `int`/`string` would break existing callers; same reasoning as Batch B `employeeCode`). PartyGroups: added `GET /party-groups/{id}` (`PartyGroupResponse`) and `groupName`, `groupShortName`, `groupType` on list rows (`PartyGroupListItem`); `name` kept. `groupType` is returned as `Customer`/`Supplier` like the request (stored as `C`/`S`).
4. **Batch D (small breaking): FIXED.** Dropped `levels[].categoryName` (response); request levels accept optional `id` (ignored, save replaces all levels); `RegionResponse.regionId` → `id` in list, GET {id} and dropdown; route param `{regionId}` → `{id}`; `countryId` non-nullable in the response. Original scope: PriceCategory + Regions (`levels[].categoryName`; `regionId` → `id` accepted, D11, released with the frontend; no dual names).
5. **Batch E (breaking): FIXED.** `GET /boms/{id}` is flat (`bilofm_details` removed; `items`, `additionalInfo` at top level); `mcGenFlg`/`mcConsFlg` are `bool` in request and response (the DB columns are already `bit`, so no Y/N conversion was needed; `IsYes()` removed); `items[]` response uses `productId`, `quantity`, `unitId`; `additionalInfo[].accountId`/`amount` are `int?`/`decimal?` in both. `GET /currency-conversions/{id}` returns `{conversionDate, details[{id, currencyId, symbol, standardRate, sellRate, buyRate}]}`; `alternateCurrencyId` → `currencyId` in get-by-id and list rows; rates are non-null `decimal` (SQL `ISNULL(..., 0)` on read); `baseCurrencyId` stays on list rows only. Original scope: Boms (flat response, bool flags, item names) + CurrencyConversion (nested response, `currencyId`; list rows also rename `alternateCurrencyId` → `currencyId`, D10).
6. **Batch F (breaking, JP renames): FIXED.** Suppliers + Customers (`salesManId`; `crDr` uppercase `CR`/`DR`, normalised in code on read and write with no database UPDATE, D12; response-only fields stay read-only, D9; nullability). They share the pending-bill code and the `crDr` rule.
7. **Batch G (breaking, large): FIXED, see "Batch G applied".** Accounts alone. Request name `groupId`, so the response `ledgerGroupId` → `groupId` (D3). Drop `acCode`, keep `accountCode` read-only (D4). Other renames as in the Accounts table.
8. **Batch H (breaking, largest payload): FIXED (with Countries response `isActive`); `brandId` type, `gdwnId`/`brCd` response and the SQL-dependent items are open, see "Batch H applied".** Products alone. Underscore keys (`_BMPL`, `_BMPQ`, `_BMPW`, `_SLNOTRACK`, `_BMEQN`) become plain names; `brandId` type; account ids keep the request names `saleAccId` and `purAcctId` and only the response changes (D5); `description` and `notes` per D1; `aprvlst`, `gdwnId`, `brCd` per D6; `image` stays out of the round trip (D7); `priceCategories[]` is read-only (D8); aliases nullability.

All breaking batches (D–H) switch the wire names at once and are released together with the frontend. No dual-name converters (D2).

Rule 3 Batch 2 (Accounts, Suppliers, Customers: `active` → `isActive`) and Batch 3 (Products) touch the same classes. Ship the matching rule 4 batch with them (F with Suppliers/Customers, G with Accounts, H with Products) so the frontend changes only once.

---

## Decisions (agreed)

- **D1** Yes: prefer request names. Exceptions: Products `description`, Customers `salesManId`.
- **D2** Switch wire names at once, released together with the frontend. No dual-name converters.
- **D3** Accounts uses `groupId` (matches `docs/api-common-issues.md`).
- **D4** Drop `acCode` from the response; keep `accountCode` read-only.
- **D5** Products keep the request names `saleAccId` and `purAcctId`; change only the response (`saleAcId` → `saleAccId`, `purAcId` → `purAcctId`).
- **D6** `aprvlst`, `gdwnId`, `brCd`: add to the response if the columns exist, else document as request-only.
- **D7** `image` stays out of the round trip (separate image endpoints).
- **D8** `priceCategories[]` is read-only in the product response.
- **D9** Customer and Supplier response-only fields stay read-only.
- **D10** Rename `alternateCurrencyId` to `currencyId` in list rows too.
- **D11** `regionId` → `id` accepted, released with the frontend.
- **D12** `crDr` is uppercase `CR`/`DR` everywhere, normalised in code on read and write. No database UPDATE.

## Not verified

- Whether the `APRVLST`, `GDWNID` and `BRCD` columns exist and can be returned by the product get-by-id query (D6 depends on it).
- The `PARTYGROUPMAS` short-name column, and where the account opening balance type (CR/DR) is stored.
- `docs/api-common-issues.md`: a grep for "R4" and "rule 4" found no rule text there. The rule definition used is from the task and `docs/api-audit.md:8`.

## Batch F applied (Customers, Suppliers)

Batch F: BREAKING, not yet released to frontend. Wire names changed with `[JsonPropertyName]` only; C# property names and SQL parameters are unchanged. No stored procedure or database data was touched.

| change | where |
|---|---|
| `active` → `isActive` (rule 3 Batch 2) in `clsCustomer`, `clsSupplier`, `clsSupplierResponse`, `CustomerRequest` (+ `CustomerUpdateRequest`), `SupplierRequest` (+ `SupplierEditRequest`). Request default `true` is inherited by the update DTOs. List query param stays `?active=` | `CLS/clsCustomer.cs`, `CLS/clsSupplier.cs`, `DTOs/CustomerRequest.cs`, `DTOs/SupplierRequest.cs` |
| `salesMan` → `salesManId` in `CustomerRequest`; `clsCustomer.SalesManId` is now `int?` | same |
| `crDr`: property setter upper-cases (and trims) on bind and on read, so old `Cr`/`Dr` come back `CR`/`DR`. No database UPDATE | all four `CrDr` properties; `SupplierController` update normalises to `CR`/`DR` (was `Cr`/`Dr`); `SupplierDB` fallbacks now `CR`/`DR` |
| Part B (Customers): `ledgerGroupId` `int?` in request and response; `stateCode` `int?` in request and response; `pendingBills[].invoiceDate` / `dueDate` `DateTime?` in request (response already nullable). Null/default dates are still rejected by `CustomerDB` | `CustomerDB.cs` (null-safe `StateCode`, `InvoiceDate`, `DueDate`), `CustomerController.cs` (`?? 0` on the effective ledger group) |

`crDr` defaults before → after: Customers request `"CR"` → `"DR"`, Customers response `"DR"` → `"DR"`; Suppliers request `"Cr"` → `"CR"`, Suppliers response `"Cr"` → `"CR"`. Left alone: `SupplierRequestUpdatedDto` (dead code), pending-bill `drCr` (stored `Dr`/`Cr` in the pending-bill table, not part of the `crDr` rule), and the response-only fields (D9).

## Batch H applied (Products, Countries)

Batch H: BREAKING, not yet released to frontend. Only C# DTO attributes and computed properties changed. No SQL text, stored procedure, save logic or database data was touched. C# property names are unchanged, so Dapper mapping is unchanged.

| change | where |
|---|---|
| Response: `prNotes`→`notes`, `saleAcId`→`saleAccId`, `purAcId`→`purAcctId`, `opnStock`→`opnStkQty`, `opnStkval`→`opnStkValue` (D5 keeps the request names) | `DTOs/Product/ProductResponse.cs` |
| Response `priceCategories[]` (read-only, D8): `catName`→`categoryName`, `discStruct`→`discountStructure`, `levFrom`→`levelFrom`, `levTo`→`levelTo`, `discPerc`→`discountPercent`, `effValue`→`effectiveValue`, `appPrice`→`appliedPrice` | same |
| Response `active`→`isActive` (read-only, `Active ?? true`) for products, `aliases[]` (rule 3) and countries | `ProductResponse.cs`, `DTOs/CountryDto.cs` (`CountryDtos`) |
| D6: `aprvlst` added to the product response (the SQL already selected `APRVLST`, only the property was commented out) | `ProductResponse.cs` |
| Request: `desc`→`description`, `catgId`→`categoryId`, `_BMPL/_BMPQ/_BMPW`→`bmpl/bmpq/bmpw`, `_SLNOTRACK`→`slNoTrack`, `_BMEQN`→`bmEqn`, `active`→`isActive` (default `true`, also on `aliases[]`) | `DTOs/ProductRequest.cs` (`ProductRequests`, `ProductUpdateRequests`, `PrAliasRequests`) |
| Safety net for one release: the old request names are still accepted through write-only alias properties (`DescOld`, `CatgIdOld`, `BmplOld`, `BmpqOld`, `BmpwOld`, `SlNoTrackOld`, `BmEqnOld`, `ActiveOld`), marked `// TODO remove after frontend release`. The new name wins when both are sent. Works on `PUT /products/{id}`, `POST /products/with-image` and `PUT /products/{id}/with-image`, which all use System.Text.Json | same |

Behaviour note: a product or alias request without `isActive` now saves `ACTIVE = 1` (it saved NULL before), because the DTO default is `true`.

### Needs approval (not done, needs SQL or save-logic changes, or a data decision)
1. **Products `brandId`:** response `string?`, request `int?`. `PRMAST.BRAND` is `nvarchar(255)`, and values such as "QuickStyle" would be lost on a round trip. Not changed.
2. **Countries `?active=` filter:** needs the hard-coded `WHERE ACTIVE = 1` in `CountryRepository.cs` changed.
3. **Countries request `isActive`:** needs `ACTIVE` to be written on insert and update.
4. **Products with NULL `ACTIVE`:** the C# `isActive` shows NULL as `true`, but `?active=true`, `GetByIdDetailAsync` (`ProductDB.cs:573`) and the alias query (`:602`) still skip NULL rows. That needs `ISNULL(...)` in SQL.
5. **`gdwnId`, `brCd` in the product response:** `PRMAST` has no such columns (`BRCD` belongs to the stock table), so they stay request-only (D6).

## Batch G applied (Accounts)

Batch G: BREAKING, not yet released to frontend. Only C# DTO attributes and computed properties changed, in `DTOs/Respons-DTO/AccountLedgerDto.cs` (class `AccountLedgerDtos` only) and `DTOs/AccountLedgerRequest.cs`. No SQL text, stored procedure, save logic or database data was touched. `AccountLedgerDto` (singular), reports, `list-party` and `{id}/opening` are unchanged. `ACCMAS` has no CR/DR column (`QuickBalance.sql:526-567`), so the type is derived from the sign of `OPENBAL`.

| change | where |
|---|---|
| Response: `accName`→`accountName`, `secName`→`secondName`, `ledgerGroupId`→`groupId` (D3), `vatPerc`→`vatPercentage`, `taxRetFlag`→`taxReturnFlag`, `bankAccName`→`bankAccountName`, `bankAccNo`→`bankAccountNumber`, `ibanNo`→`ibanNumber`, `curConvRate`→`currencyConversionRate`, `curCode`→`currencyCode` (JSON names only; C# names unchanged) | `AccountLedgerDtos`; applies to list, get-by-id and `GET /accounts/group/{groupId}` |
| `curFlg`→`currencyFlag` as read-only `CurrencyFlag => CurFlg ?? false` (`CurFlg` is `[JsonIgnore]`), so it is never null | same |
| `active`→`isActive` as read-only `IsActive => Active ?? true` (`Active` is `[JsonIgnore]`) | same |
| `acCode` hidden (D4), `accountCode` kept read-only | same |
| `openBal` hidden; read-only `openingBalance` (absolute value) and `openingBalanceType` (`"DR"` if `OpenBal < 0`, else `"CR"`) | same |
| Request: `Active` gets `[JsonPropertyName("isActive")]` (default `true`); the `openingBalanceType` regex is case-insensitive (`^(?i)(CR|DR)$`) | `AccountLedgerRequest` |

Breaking: renamed keys; `openBal` is gone (a debit account returns `500` + `"DR"` where it returned `-500`); `acCode` is gone; `active` is `isActive`.

Also in this change: `GET /customers/{id}` now returns `ApiResponse<clsCustomer>` with message "Customer retrieved successfully." (data unchanged). This is not a rule 4 item.
