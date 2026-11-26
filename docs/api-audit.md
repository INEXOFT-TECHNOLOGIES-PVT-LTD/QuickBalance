# API Consistency Audit

Audit of every controller under `Controllers/` against the 4 rules in `docs/api-common-issues.md`.
Read-only: no code was changed. Sections are appended one controller at a time.

> **Scope.** Current phase: MASTERS ONLY. Sales transactions, account reports, purchase reports, Batch, SerialNumber and Companies are out of scope for now. Do not change them.

Rules: **R1** list envelope (`status`, `message`, `data[]`, `pagination`) · **R2** flat camelCase list rows ·
**R3** masters: `isActive` field, `?active=true|false` filter, default true on create, omitted filter returns both ·
**R4** request and response use the same field names and types.

## Envelope / pagination classes that already exist

| Class | File | Used by |
|---|---|---|
| `ApiResponse<T>` (`Status`, `Message`, `Data`, optional `Pagination`; `Success`, `SuccessWithPagination`, `Failure`) | `CLS/ApiResponse.cs:9` | New-layered master services (see per-controller notes) |
| `ApiResponseWithoutPagination<T>` | `CLS/ApiResponse.cs:61` | Swagger `ProducesResponseType` on get-by-id in new-style controllers (runtime type is `ApiResponse<T>`) |
| `ApiResponseWithError` | `CLS/ApiResponse.cs:97` | Swagger error docs, global 400/401/403 rewrite |
| `ApiResponseWithSuccess` | `CLS/ApiResponse.cs:114` | Swagger docs for create/update/delete |
| `PaginationInfo` (`Page`, `PageSize`, `TotalRecords`, `TotalPages`) | `CLS/ApiResponse.cs:133` | Pagination block inside `ApiResponse<T>` |
| `PagedResponse<T>` | `DTOs/Respons-DTO/PagedResponse.cs:5` | Repository return type; service maps it to `PaginationInfo` |
| `PaginationResponse`, `PaginationResponse<T>` | `DTOs/PaginationResponse.cs:3,14` | Older code (usage listed per controller) |
| `PaginationRequest`, `PaginationRequests` | `DTOs/PaginationRequest.cs:3,51` | Older request-side paging |
| `ApiResponseWithouPagination<T>` (typo duplicate, `internal`) | `Controllers/Master/SalesmanController.cs:232` | SalesmanController only |
| `ApiResponseOperationFilter` | `Configuration/ApiResponseOperationFilter.cs:8` | Swagger docs only |

### Which controllers use which envelope at runtime

| Envelope | Controllers |
|---|---|
| `ApiResponse<T>` + `SuccessWithPagination` (the standard) | Brand, ItemGroup, Units, Taxes, UnitConversions, PartyGroup, Countries, Region, Currency, CurrencyConversion, PriceCategory, Salesman, SalesmanGroup, ItemCategories, Location, LocationGroup, Bom, Product, Supplier (built directly in the controller) |
| `ApiResponseWithoutPagination<T>` | Users (login), Common (lookups), AccountLedger `GET {id}/opening` |
| Old `PaginationResponse<T>` (`data,page,pageSize,totalRecords,totalPages`, no `status`/`message`) | AccountLedger `GET /accounts` |
| Hand-made `PartyListResponse` (`DTOs/Respons-DTO/PartyQueryResult.cs:36`) with old `PaginationResponse` | Accounts `list-party` |
| Anonymous object with the right fields (`status,message,data,pagination`) | LedgerGroup, CurrencyMaster, DayBook, CashBook, SalesOrder, SalesInvoice, SalesReturn (old non-generic `PaginationResponse` for the pagination block in the sales ones) |
| Anonymous object, **missing `message` and/or `pagination`** | Customer (no `message`), Ledger report (no `message`, `pageNumber`), CashAndBankBook, CashFlowStatement, DailyBalance, DailyClosing, LedgerOpening, LedgerOpeningBalance, MonthWiseProfitAndLoss, ProfitAndLoss, ReceiptAndPayment, TrialBalance, VoucherQuery, Category (paging values at top level), Companies, Batch (`pageNumber`), SerialNumber (`pageNumber`) |
| Anonymous object with string `status` (`"success"`/`"error"`) | all nine purchase reports (SupplierWiseProduct uses bool) |
| No envelope | Version |

Where the doc's examples disagree with the source: `docs/api-common-issues.md` says `GET /products` has no `status`/`message` and `GET /customers` has no `message`. Source: **Products does return both** (`ProductService.cs:418`), Customers has no `message` (`CustomerController.cs:59-73`). Verify the running build of Products.

---

## BrandsController (`api/v1/brands`) — `Controllers/Master/BrandController.cs`

Uses `ApiResponse<T>` via `BrandService` → `BrandDB` (`Repository/Master/BrandDB.cs`). Table `BRANDMAS` has no active column (only `DELFLG` soft delete), so R3 does not apply unless the product wants brands deactivatable.

| endpoint | rule broken | current shape | expected shape | fix location | Final status |
|---|---|---|---|---|---|
| `GET /api/v1/brands` | FIXED (Swagger) | Runtime: `{status,message,data:[{id,brandName,secondName,alias}],pagination}`. Swagger `ProducesResponseType` says `data` is a single `BrandResponse` | Swagger shows `data` as array | `BrandController.cs:32` → `ApiResponse<IReadOnlyList<BrandResponse>>` | PASS |
| `GET /api/v1/brands/{id}` | OK | `{status,message,data:{id,brandName,secondName,alias}}` | same | — | PASS |
| `POST /api/v1/brands` | OK (R4: request `brandName,secondName,alias` = response) | body `BrandRequest` | same | — | PASS |
| `PUT /api/v1/brands/{id}` | OK | same as POST | same | — | PASS |
| `DELETE /api/v1/brands/{id}` | OK | `{status,message}` | same | — | PASS |
| `GET /api/v1/brands` R3 | R3 (not applicable) | no `isActive` field, no `?active=` filter; `BRANDMAS.DELFLG` only | decide: add `ACTIVE` column + filter, or exclude brands from R3 | `BrandDB.cs:21,78`, `QuickBalance.sql:869` | WON'T FIX (BRANDMAS has no ACTIVE column; excluded from rule 3 by decision) |

---

## ItemGroupController (`api/v1/item-group`) — `Controllers/Master/ItemGroupController.cs`

Uses `ApiResponse<T>` via `ItemGroupService` → `Repository/Master/ItemGroupDBs.cs` (aliases `ID AS Id`, `ACTIVE AS IsActive`). Route is singular (convention says plural).

| endpoint | rule broken | current shape | expected shape | fix location | Final status |
|---|---|---|---|---|---|
| `GET /api/v1/item-group` | FIXED (Swagger) | Runtime envelope is correct. Swagger declares `data` as one `ItemGroupResponse` | `data` documented as array | `ItemGroupController.cs:51` | PASS |
| `GET /api/v1/item-group` | R2 — **FIXED** (`ItemGroupListItemDto`) | Rows flat, camelCase. Extra internal fields `logCode`, `updateTime` | drop internal fields from list rows | `DTOs/Respons-DTO/ProductCategoryResponse.cs:17-18`, `ItemGroupDBs.cs:27-28` | PASS |
| `GET /api/v1/item-group` | R3 | `isActive` bool and `?active=` filter present; omitted returns both | OK | — | PASS |
| `GET /api/v1/item-group/{id}` | OK | `{status,message,data:{id,groupName,groupCode,isPrimaryGroup:bool,...,isActive:bool,secName}}` | — | — | PASS |
| `POST /api/v1/item-group` | R4 | Request: `groupId`, `secondName`, `stockAccount`, `purchaseAccount`, `salesAccount`, `isPrimaryGroup:"Y"/"N"`, `tagWithAccount:"Y"/"N"`, `isActive:"Y"/"N"` (strings) | Same names and types as response: `secName` (or rename both), `stockAccountId`, `purchaseAccountId`, `salesAccountId`, booleans, `id` instead of `groupId` | `DTOs/ItemGroupMasterRequest.cs:5-25` vs `ProductCategoryResponse.cs:5-20` | PASS (live `ItemGroupRequest` is typed/bool; the Y/N class is used only by the backup controller) |
| `PUT /api/v1/item-group/{id}` | R4 | same request DTO as POST | same fix as POST | `ItemGroupMasterRequest.cs` | PASS (same as POST) |
| `DELETE /api/v1/item-group/{id}` | OK | `{status,message}` | — | — | PASS |
| `GET /api/v1/item-group/primary-groups` | R1/R2 — **FIXED** (`ApiResponseWithoutPagination<IReadOnlyList<ItemGroupResponse>>` + `ProducesResponseType`; rows unchanged, so `{id,name}` not done) | Lookup; no `ProducesResponseType`; not paged (`GetPrimaryGroupsAsync`) | document as array envelope; rows `{id,name}` | `ItemGroupController.cs:121` | PASS |

## UnitsController (`api/v1/units`) — `Controllers/Master/UnitsController.cs`

`ApiResponse<T>` via service/repo. DTOs in `DTOs/UnitMasterRequest.cs`.

| endpoint | rule broken | current shape | expected shape | fix location | Final status |
|---|---|---|---|---|---|
| `GET /api/v1/units` | FIXED (Swagger) | Runtime correct; Swagger `data` is single `UnitMasterResponse` | array | `UnitsController.cs:36` | PASS |
| `GET /api/v1/units` | R3 — **FIXED** (Batch 1: `UnitMasterResponse.IsActive` defaults `true`) | `isActive` + `?active=` present; omitted returns both | OK | `DTOs/UnitMasterRequest.cs:32` | PASS |
| `GET /api/v1/units/{id}` | OK | `{id,unitName,secondName,formalName,isActive}` | — | — | PASS |
| `POST /api/v1/units` | OK | request `unitName,secondName,formalName,isActive=true` matches response (R3 default true) | — | — | PASS |
| `PUT /api/v1/units/{id}` | OK | same | — | — | PASS |
| `DELETE /api/v1/units/{id}` | OK | — | — | — | PASS |
| `UnitMasterResponse.IsActive` default | R3 (minor) | response class defaults `isActive=false`, request defaults `true` | default `true` | `DTOs/UnitMasterRequest.cs:32` | PASS |

## TaxesController (`api/v1/taxes`) — `Controllers/Master/TaxesController.cs`

`ApiResponse<T>` via service/repo. DTOs in `DTOs/Tax/` (the flat `DTOs/TaxMasterRequest.cs` is excluded from the build).

| endpoint | rule broken | current shape | expected shape | fix location | Final status |
|---|---|---|---|---|---|
| `GET /api/v1/taxes` | FIXED (Swagger) | Swagger `data` is single object | array | `TaxesController.cs:54` | PASS |
| `GET /api/v1/taxes` | R3 | `isActive` + `?active=` present | OK | — | PASS |
| `GET /api/v1/taxes/{id}` | OK | — | — | — | PASS |
| `POST /api/v1/taxes` | R4 (type) — **FIXED** (Batch A) | Request account ids are `int?`, `secondName` and `taxType` are `string?`. Response returns non-null `int`, `string` | same nullability both ways | `DTOs/Tax/TaxMasterRequest.cs:24-60` vs `TaxMasterResponse.cs:16-32` | PASS |
| `PUT /api/v1/taxes/{id}` | R4 (type) — **FIXED** (Batch A) | same as POST | same | same | PASS |
| `DELETE /api/v1/taxes/{id}` | OK | — | — | — | PASS |

## UnitConversionsController (`api/v1/unit-conversions`) — `Controllers/Master/UnitConversionsController.cs`

`ApiResponse<T>` via `UnitConversionRepository` (`CODE AS Id`). Table has no active column, so R3 does not apply.

| endpoint | rule broken | current shape | expected shape | fix location | Final status |
|---|---|---|---|---|---|
| `GET /api/v1/unit-conversions` | FIXED (Swagger) | runtime correct; Swagger `data` single `UnitConversionDto` | array | `UnitConversionsController.cs:30` | PASS |
| `GET /api/v1/unit-conversions` | R2 | flat camelCase rows `{id,mainUnit,subUnit,convFact,mainUnitName,subUnitName}` | OK | — | PASS |
| `GET /api/v1/unit-conversions/{id}` | FIXED (Swagger) | no `ProducesResponseType` | add | `UnitConversionsController.cs:41` | PASS |
| `POST /api/v1/unit-conversions` | OK | `UnitConversionRequestDto {mainUnit,subUnit,convFact}` = response names. Types `int?`/`decimal?` match | — | — | PASS |
| `PUT /api/v1/unit-conversions/{id}` | OK | same | — | — | PASS |
| `DELETE /api/v1/unit-conversions/{id}` | OK | — | — | — | PASS |
| (dead code) | note | `DTOs/UnitConversionRequest.cs` uses `ConversionFactor` (not used by the controller) | delete or ignore | `DTOs/UnitConversionRequest.cs` | WON'T FIX (dead code, not routed) |

## PartyGroupController (`api/v1/party-groups`) — `Controllers/Master/PartyGroupController.cs`

`ApiResponse<T>`; list rows are `LookupItem {id,name}` from `PartyGroupRepository.cs:147-148`.

| endpoint | rule broken | current shape | expected shape | fix location | Final status |
|---|---|---|---|---|---|
| `GET /api/v1/party-groups?groupType=` | FIXED (Swagger) | Swagger says `ApiResponseWithoutPagination<LookupItem>` but runtime returns pagination | array + pagination in Swagger | `PartyGroupController.cs:30` | PASS |
| `GET /api/v1/party-groups` | FIXED (Batch C) | rows `{id,name}` only; request has `groupName`, `groupShortName`, `groupType` | rows should expose `groupName`, `groupShortName`, `groupType` (same names as request) | `PartyGroupRepository.cs:186-187`, `DTOs/PartyGroup/PartyGroupRequest.cs:9-18`, `DTOs/LookupItem.cs` | PASS |
| `GET /api/v1/party-groups` | R3 | no `isActive` field, no `?active=` filter | add if table supports it | `PartyGroupRepository.cs:163` | WON'T FIX (PARTYGROUPMAS has no ACTIVE column) |
| (missing) | FIXED (Batch C) | no `GET /{id}`, so an edit form cannot load a record | add get-by-id | `PartyGroupController.cs` | PASS |
| `POST /api/v1/party-groups` | R4 | request `groupName,groupShortName,groupType` ≠ list row `name` | same names | `PartyGroupRequest.cs` | PASS (Batch C: list rows and GET {id} expose the request names) |
| `PUT /api/v1/party-groups/{id}` | R4 | same | same | same | PASS (same as POST) |
| `DELETE /api/v1/party-groups/{id}` | OK | — | — | — | PASS |

---

## CountriesController (`api/v1/countries`) — `Controllers/Master/CountriesController.cs`

`ApiResponse<T>` via `CountryService` → `Repository/Master/CountryRepository.cs`. Table `COUNTRY` is in MasterDB.

| endpoint | rule broken | current shape | expected shape | fix location | Final status |
|---|---|---|---|---|---|
| `GET /api/v1/countries` | FIXED (Swagger) | Swagger `data` is single `CountryDtos` | array | `CountriesController.cs:30` | PASS |
| `GET /api/v1/countries` | R3 — **response `isActive` FIXED (Batch H)**; `?active=` still open (needs SQL change, awaiting approval) | Field is `active` (`bool?`), not `isActive`. No `?active=` parameter. The query is hard-coded `WHERE ACTIVE = 1`, so inactive rows can never be listed | `isActive`; `?active=true\|false`; omitted returns both | `DTOs/CountryDto.cs:19`, `CountriesController.cs:34`, `CountryRepository.cs:91` (also 155, 171) | OPEN (`?active=` filter and request `isActive` need SQL/save changes; list still `WHERE ACTIVE = 1`) |
| `GET /api/v1/countries/{id}` | R3 — **response `isActive` FIXED (Batch H)** | `active` again; only active rows are returned by id | `isActive` | `CountryDto.cs:19`, `CountryRepository.cs:155` | OPEN (get-by-id still `AND ACTIVE = 1`, so an inactive country cannot be fetched) |
| `POST /api/v1/countries` | R3/R4 | `CountryRequest` has no `active`/`isActive`; response has `active`. Request has `vatCharAllow` but `CountryDtos.VatCharAllow` is commented out, so SQL `VATCHARALLOW AS VatCharAllow` is dropped from the response | add `isActive` (default true) to request; add `vatCharAllow` to response | `CountryDto.cs:36` (response), `CountryDto.cs:126` (request) | OPEN (request has no `isActive`; `vatCharAllow` still not in the response) |
| `PUT /api/v1/countries/{id}` | R3/R4 | same as POST | same | same | OPEN (same as POST) |
| `DELETE /api/v1/countries/{id}` | OK | — | — | — | PASS |

## RegionController (`api/v1/regions`) — `Controllers/Master/RegionController.cs`

`ApiResponse<T>` via `RegionService` → `Repository/Master/RegionRepository.cs`. Table `REGION` is in MasterDB.

| endpoint | rule broken | current shape | expected shape | fix location | Final status |
|---|---|---|---|---|---|
| `GET /api/v1/regions?countryId=` | FIXED (Swagger) | Swagger `ApiResponse<List<RegionResponse>>`, which wraps an array in a generic list. Runtime pagination present | OK shape; align Swagger | `RegionController.cs:25` | PASS |
| `GET /api/v1/regions` | R2 | rows use `regionId` as the key (`id` elsewhere) | `id` | `DTOs/RegionResponse.cs:6`, `RegionRepository.cs:23` | PASS (Batch D: key is `id`) |
| `GET /api/v1/regions` | R3 — **FIXED** (Batch 1: `isActive` in response, `?active=true|false`, omitted returns both) | SQL selects `ACTIVE AS IsActive` but `RegionResponse` has no `IsActive`, so the field never reaches the client. No `?active=` filter; hard-coded `WHERE ACTIVE = 1` | add `isActive`; add `?active=`; omitted returns both | `RegionResponse.cs`, `RegionRepository.cs:77` | PASS |
| `GET /api/v1/regions/{id}` | R2/R3/R4 — R3 part **FIXED** (Batch 1). R4 **FIXED, Batch D** (key `regionId` → `id` in list, get-by-id and dropdown; route param `{id}`; `countryId` non-nullable). BREAKING, not yet released to frontend | `regionId` route name and key; no `isActive`; only active rows | `{id}` | `RegionController.cs:41`, `RegionRepository.cs:123` | PASS |
| `POST /api/v1/regions` | R3 — **FIXED** (Batch 1: default `true`) | `RegionRequest.IsActive` is a plain `bool`, so an omitted value is `false` (all other masters default true) | default `true` | `DTOs/RegionRequest.cs:28` | PASS |
| `POST /api/v1/regions` | R4 | request `countryId,regionName,regionSecName,isActive`; response lacks `isActive`, uses `regionId` | same names both ways | `RegionRequest.cs:19-28` vs `RegionResponse.cs` | PASS (Batch 1 + D) |
| `PUT /api/v1/regions/{regionId}` | R3/R4 | same as POST | same | same | PASS (Batch 1 + D) |
| `DELETE /api/v1/regions/{regionId}` | OK | — | — | — | PASS |

## CommonController (`api/v1`, lookups) — `Controllers/Master/CommonController.cs`

Static lookups. Uses `ApiResponseWithoutPagination<T>` directly (not paged by design). Not masters, so R3 does not apply.

| endpoint | rule broken | current shape | expected shape | fix location | Final status |
|---|---|---|---|---|---|
| `GET /api/v1/vat-types` | **FIXED** (envelope: `ApiResponseWithoutPagination<IReadOnlyList<DropdownOption>>`, data unchanged; numeric id not done) | `{status,message,data:[{id:"Standard VAT",name:"Standard VAT"}]}`, no `pagination` key; `id` is a string | decide whether non-paged lookups must send `pagination`; `id` should be numeric/code | `CommonController.cs:21-34` | PASS |
| `GET /api/v1/einvoice-id-types` | R2 — **FIXED** (`data:[{id,name}]`) | `data:["CRN","MOM",...]` is an array of bare strings, not objects | `[{id:"CRN",name:"CRN"}]` | `CommonController.cs:45-53` | PASS |

---

## CurrencyController (`api/v1/currencies`) — `Controllers/Master/CurrencyController.cs`

`ApiResponse<T>` with `SuccessWithPagination` (`Services/CurrencyService.cs`). Currency table has no active flag, so R3 does not apply. Extra list filter `?excludeBase=`.

| endpoint | rule broken | current shape | expected shape | fix location | Final status |
|---|---|---|---|---|---|
| `GET /api/v1/currencies` | OK (R1, R2) | `{status,message,data:[{id,symbol,currString,subString,convMode,baseFlg,secSymbol,secCurrString,secSubString}],pagination}` | — | — | PASS |
| `GET /api/v1/currencies/{id}` | OK | same row shape | — | — | PASS |
| `POST /api/v1/currencies` | R4 (type) — **FIXED** (Batch A) | `baseFlg` is `bool?` in the request and `bool` in the response | same type | `DTOs/Currencies/CurrencyRequest.cs:38` vs `CurrencyResponse.cs:16` | PASS |
| `PUT /api/v1/currencies/{id}` | R4 (type) — **FIXED** (Batch A) | same | same | same | PASS |
| `DELETE /api/v1/currencies/{id}` | OK | — | — | — | PASS |

## CurrencyMasterController (`api/v1/currenciesssss`, hidden from Swagger) — `Controllers/Master/CurrencyMasterController.cs`

Legacy duplicate of `CurrencyController`. `IgnoreApi=true`, but the route is live. Builds anonymous objects from a `DataTable` (`currMasDB.AllCurrencies`). Not registered in `Program.cs` as a service, so it uses the legacy-port context. The route name looks like a typo and the controller should be removed or left out of the frontend contract.

| endpoint | rule broken | current shape | expected shape | fix location | Final status |
|---|---|---|---|---|---|
| `GET /api/v1/currenciesssss` | R1 | `{status,message,data,pagination}` is built by hand (anonymous). Paging is done in memory after loading the whole DataTable | same envelope from `ApiResponse<T>`; paging in SQL | `CurrencyMasterController.cs:36-88` | WON'T FIX (hidden legacy duplicate of /currencies; retire it) |
| `GET /api/v1/currenciesssss` | R2/R4 | keys `code,currencyString,conversionMode,baseFlag,secondSymbol,secondCurrencyString,secondSubString` differ from `CurrencyController` (`id,currString,convMode,baseFlg,secSymbol,...`) | single naming across both | `CurrencyMasterController.cs:48-72` | WON'T FIX (hidden legacy duplicate of /currencies; retire it) |
| `GET .../{id}` | R1/R4 | `NotFound(new {status,message})` (HTTP 404, other masters return 400); row built by `ConvertRow` | consistent | `CurrencyMasterController.cs:106-117` | WON'T FIX (hidden legacy duplicate of /currencies; retire it) |
| `POST`, `PUT /{id}` | R4 | `CurrencyMasterRequest` has `currencyString,conversionMode,secondSymbol` and no `baseFlg`; response has `baseFlag` | same names both ways | `DTOs/CurrencyMasterRequest.cs:5-13` | WON'T FIX (hidden legacy duplicate of /currencies; retire it) |
| `DELETE /{id}` | R1 | anonymous `{status,message}` | `ApiResponseWithSuccess` | `CurrencyMasterController.cs:381` | WON'T FIX (hidden legacy duplicate of /currencies; retire it) |
| `GET .../base`, `GET .../alternate`, `GET .../list` | R1/R2 | non-paged lists built from DataTable with anonymous objects | document or remove | `CurrencyMasterController.cs:320,342,364` | WON'T FIX (hidden legacy duplicate of /currencies; retire it) |

## CurrencyConversionController (`api/v1/currency-conversions`) — `Controllers/Master/CurrencyConversionController.cs`

`ApiResponse<T>` via `CurrencyConversionService`. One conversion row per date and currency. R3 does not apply (no active flag).

| endpoint | rule broken | current shape | expected shape | fix location | Final status |
|---|---|---|---|---|---|
| `GET /api/v1/currency-conversions` | FIXED (Swagger) | Swagger `ApiResponse<List<...>>` | matches runtime array + pagination | `CurrencyConversionController.cs:36` | PASS |
| `GET /api/v1/currency-conversions` | R2; key rename **FIXED, Batch E** (`alternateCurrencyId` → `currencyId`, D10; BREAKING, not yet released to frontend) | flat rows `{id,baseCurrencyId,alternateCurrencyId,symbol,conversionDate,standardRate,sellRate,buyRate}` | OK | `DTOs/CurrencyConversionRequest.cs:37-53` | PASS |
| `GET /api/v1/currency-conversions/{id}` | R4 | Controller comment says it "returns all details belonging to the selected date"; Swagger declares a single `CurrencyConversionResponse` | a response with `conversionDate` and `details[]` to mirror the request | `CurrencyConversionController.cs:76-79` | PASS (Batch E) |
| `POST /api/v1/currency-conversions` | R4 — **FIXED, Batch E** (`GET {id}` now `{conversionDate, details[...]}` with `currencyId`; rates non-null `decimal`). BREAKING, not yet released to frontend | Request `{conversionDate, details:[{currencyId, standardRate, sellRate, buyRate}]}`. Response uses `alternateCurrencyId` (not `currencyId`) and non-nested rows; `decimal` in request vs `decimal?` in response | same field names (`currencyId` or `alternateCurrencyId`) and nesting | `CurrencyConversionRequest.cs:12-33` vs `:41-53` | PASS |
| `PUT /api/v1/currency-conversions/{id}` | R4 | same | same | same | PASS (Batch E) |
| `DELETE /api/v1/currency-conversions/{id}` | OK | — | — | — | PASS |

## PriceCategoryController (`api/v1/price-categories`) — `Controllers/Master/PriceCategoryController.cs`

`ApiResponse<T>` with `SuccessWithPagination`. No active flag (R3 n/a).

| endpoint | rule broken | current shape | expected shape | fix location | Final status |
|---|---|---|---|---|---|
| `GET /api/v1/price-categories` | R2 — **FIXED** (new flat `PriceCategoryListItemDto`; `GET {id}` unchanged) | each list row carries `levels:[...]` (repository loads levels per category in a loop) | list rows flat `{id,categoryName,levelCount}`; `levels` only in get-by-id | `Repository/Master/PriceCategoryRepository.cs:146`, `DTOs/PriceCategoryResponse.cs:10` | PASS |
| `GET /api/v1/price-categories/{id}` | OK | `{id,categoryName,levels:[...]}` | — | — | PASS |
| `POST /api/v1/price-categories` | R4 — **FIXED, Batch D** (response `levels[].categoryName` removed; request levels accept optional `id`, ignored). BREAKING, not yet released to frontend | request `levels[]` items have no `id`/`categoryName`; response levels have `id`,`categoryName` (extra) | same names; drop the duplicated `categoryName` per level | `DTOs/PriceCategoryRequest.cs:6-64` vs `PriceCategoryResponse.cs:14-30` | PASS |
| `PUT /api/v1/price-categories/{id}` | R4 | same | same | same | PASS (Batch D) |
| `DELETE /api/v1/price-categories/{id}` | OK | — | — | — | PASS |
| (extra DTOs) | note | `PriceCategoryLevelRequest` (`catName`, `discStruct`, `levFrom` …) is a second, differently named level DTO | remove or align | `DTOs/PriceCategoryLevelRequest.cs` | WON'T FIX (note only; not used by the live controller) |

## SalesmanController (`api/v1/salesman`) — `Controllers/Master/SalesmanController.cs`

`ApiResponse<T>` with `SuccessWithPagination`. Route is singular. Private duplicate class `ApiResponseWithouPagination<T>` at line 232.

| endpoint | rule broken | current shape | expected shape | fix location | Final status |
|---|---|---|---|---|---|
| `GET /api/v1/salesman` | OK (R1, R2, R3) | flat camelCase rows incl. `isActive`; `?active=` supported | — | — | PASS |
| `GET /api/v1/salesman/{id}` | OK | same row | — | — | PASS |
| `POST /api/v1/salesman` | R4 (type) — **FIXED** (Batch B: `employeeCode` `int?` both; optional strings `string?` both) | request `employeeCode` is `int?`, response `int`; `email` required in request but `string` | same nullability | `DTOs/SalesMan/SalesmanRequest.cs:7` vs `SalesmanResponse.cs:10` | PASS |
| `PUT /api/v1/salesman/{id}` | R4 (type) | same | same | same | PASS (Batch B) |
| `DELETE /api/v1/salesman/{id}` | OK | — | — | — | PASS |
| (code) | note | `internal class ApiResponseWithouPagination<T>` duplicates `ApiResponseWithoutPagination<T>` | delete the duplicate | `SalesmanController.cs:232` | WON'T FIX (code cleanup, no wire effect) |

## SalesmanGroupController (`api/v1/salesman-groups`) — `Controllers/Master/SalesmanGroupController.cs`

| endpoint | rule broken | current shape | expected shape | fix location | Final status |
|---|---|---|---|---|---|
| `GET /api/v1/salesman-groups` | R2 — **FIXED** (`SalesmanGroupListItemDto`) | rows include internal `logCode`, `updateTime` | remove from list rows | `DTOs/SalesMan/SalesmanGroupResponse.cs:15-17` | PASS |
| `GET /api/v1/salesman-groups` | R3 | `isActive` + `?active=` present | OK | — | PASS |
| `GET /api/v1/salesman-groups/{id}` | OK | — | — | — | PASS |
| `POST /api/v1/salesman-groups` | R4 (type) — **FIXED** (Batch B: `string?` both; `logCode`/`updateTime` removed from get-by-id) | request `secondName`, `printName` are `string?`; response non-null `string` | same | `SalesmanGroupRequest.cs:10-12` vs `SalesmanGroupResponse.cs:9-11` | PASS |
| `PUT /api/v1/salesman-groups/{id}` | R4 (type) | same | same | same | PASS (Batch B) |
| `DELETE /api/v1/salesman-groups/{id}` | OK | — | — | — | PASS |

---

## ItemCategoriesController (`api/v1/item-categories`) — `Controllers/Master/ItemCategoriesController.cs`

`ApiResponse<T>` with `SuccessWithPagination` (`ItemCategoryService`). Cleanest master after Brand.

| endpoint | rule broken | current shape | expected shape | fix location | Final status |
|---|---|---|---|---|---|
| `GET /api/v1/item-categories` | OK (R1-R3) | flat rows `{id,categoryName,alias,secondName,isActive}`; `?active=` supported | — | — | PASS |
| `GET /api/v1/item-categories/{id}` | OK | — | — | — | PASS |
| `POST /api/v1/item-categories` | OK | request `categoryName,alias,secondName,isActive=true` = response | — | — | PASS |
| `PUT /api/v1/item-categories/{id}` | OK | same | — | — | PASS |
| `DELETE /api/v1/item-categories/{id}` | OK | — | — | — | PASS |

## CategoryController (`api/v1/categories`, hidden from Swagger) — `Controllers/Master/CategoryController.cs`

Legacy duplicate of Item Categories (table `CATGMAS`). `IgnoreApi=true`, but the route is live. It uses `DB/CatgMasDB.cs` with a `DataTable` and returns anonymous objects. It sorts, searches and pages in memory.

| endpoint | rule broken | current shape | expected shape | fix location | Final status |
|---|---|---|---|---|---|
| `GET /api/v1/categories` | R1 | `{status,message,data,page,pageSize,totalRecords,totalPages}`: paging values are top-level, no `pagination` object. Page is 0-based (`Skip(Page*PageSize)`) | `pagination:{page,pageSize,totalRecords,totalPages}`, 1-based | `CategoryController.cs:313,328-336` | WON'T FIX (hidden legacy /categories; use /item-categories) |
| `GET /api/v1/categories` | R2 | rows are dictionaries keyed by raw SQL names: `CATGCODE, CATGNAME, ALIAS, ACTIVE, SECNAME, LOGCD, UTIME` | camelCase `id,categoryName,alias,isActive,secondName` | `DB/CatgMasDB.cs:79-86` | WON'T FIX (hidden legacy /categories; use /item-categories) |
| `GET /api/v1/categories` | R3 | field `ACTIVE`; no `?active=` filter; returns all rows | `isActive` + `?active=` | `CatgMasDB.cs:83` | WON'T FIX (hidden legacy /categories; use /item-categories) |
| `GET /api/v1/categories/{id}`, `/active`, `/search`, `/dropdown`, `/export` | R1/R2 | anonymous `{status,message,data}`, raw DataTable keys; unauthorized returns `status:"error"` (string) | `ApiResponse<T>`; camelCase | `CategoryController.cs:176-180, 353-415, 553-766` | WON'T FIX (hidden legacy /categories; use /item-categories) |
| `POST /api/v1/categories`, `PUT /{id}` | R4 | request `categoryName,secondName,alias,isActive` vs response `CATGNAME,SECNAME,ALIAS,ACTIVE` | same names | `DTOs/CategoryMasterRequest.cs:5-8` | WON'T FIX (hidden legacy /categories; use /item-categories) |

Recommendation: retire this controller in favour of `api/v1/item-categories`.

## LocationController (`api/v1/locations`) — `Controllers/Master/LocationController.cs`

`ApiResponse<T>` with `SuccessWithPagination` (`LocationService`). Lines 150-947 are commented-out legacy code.

| endpoint | rule broken | current shape | expected shape | fix location | Final status |
|---|---|---|---|---|---|
| `GET /api/v1/locations` | R2 — **FIXED** (`LocationListItemDto`) | rows include internal `logCd` | drop `logCd` from list rows | `DTOs/LocationMasterRequest.cs:47` | PASS |
| `GET /api/v1/locations` | R3 | `isActive` + `?active=` present | OK | — | PASS |
| `GET /api/v1/locations/{id}` | OK | — | — | — | PASS |
| `POST /api/v1/locations` | OK | request `locationName,groupId,isActive=true,stockAccountId,stockPLFlag,secondName` = response (`secondName` is `string?` both sides) | — | — | PASS |
| `PUT /api/v1/locations/{id}` | OK | same | — | — | PASS |
| `DELETE /api/v1/locations/{id}` | OK | — | — | — | PASS |

## LocationGroupController (`api/v1/location-groups`) — `Controllers/Master/LocationGroupController.cs`

| endpoint | rule broken | current shape | expected shape | fix location | Final status |
|---|---|---|---|---|---|
| `GET /api/v1/location-groups` | R2 — **FIXED** (`LocationGroupListItemDto`) | rows include internal `logCd` | drop | `DTOs/LocationGroupMasterRequest.cs:41` | PASS |
| `GET /api/v1/location-groups` | R3 | `isActive` + `?active=` present | OK | — | PASS |
| `GET /api/v1/location-groups/{id}` | OK | — | — | — | PASS |
| `POST /api/v1/location-groups` | OK | `groupName,isActive=true,secondName` = response | — | — | PASS |
| `PUT /api/v1/location-groups/{id}` | OK | same | — | — | PASS |
| `DELETE /api/v1/location-groups/{id}` | OK | — | — | — | PASS |

## BomController (`api/v1/boms`) — `Controllers/Master/BomController.cs`

`ApiResponse<T>` with `SuccessWithPagination` (`BomService.cs:299`) and `BomRepository.cs:267-405`.

| endpoint | rule broken | current shape | expected shape | fix location | Final status |
|---|---|---|---|---|---|
| `GET /api/v1/boms` | R2 — **FIXED** (new flat `BomListItemDto`; `GET /boms/{id}` unchanged) | each row is `BomListDetailsDto`: `{bilofm_details:{...}, items:[...], additionalInfo:[...]}`. Nested object, snake_case key, detail arrays in the list | flat row `{id,bomName,alias,productName,qty,unitName,isActive}` | `DTOs/BomRequest.cs:148-154` (`bilofm_details`), `BomRepository.cs:383` | PASS |
| `GET /api/v1/boms` | R3 — **FIXED** (`isActive` added to the row) | `?active=` accepted (`BM.ACTIVE`), but `BomMasterDto` has no status property, so SQL `BM.ACTIVE AS Active` (`BomRepository.cs:328`) is dropped | add `isActive` to the row | `DTOs/BomRequest.cs:129-146`, `BomRepository.cs:328` | PASS |
| `GET /api/v1/boms` | FIXED (Swagger) | Swagger: `ApiResponse<BomListDetailsDto>` (single object) | array | `BomController.cs:54` | PASS |
| `GET /api/v1/boms/{id}` | R2/R4 — **FIXED, Batch E** (flat response; `items[]` uses `productId`/`quantity`/`unitId`; `mcGenFlg`/`mcConsFlg` bool; `additionalInfo` nullable in both). BREAKING, not yet released to frontend. R3 part FIXED (Batch 1: `isActive` added) | `BomDetailsDto` also nests under `bilofm_details`; unit as `unit`, `prCode`, `qty` | `{id,bomName,...,items:[{productId,quantity,unitId,...}],additionalInfo:[...]}` | `DTOs/BomRequest.cs:188-192` | PASS |
| `POST /api/v1/boms` | R4 — **FIXED, Batch E** (`mcGenFlg`/`mcConsFlg` now bool in the request; Y/N regex and `IsYes()` removed). BREAKING, not yet released to frontend. R3 part **FIXED** (Batch 1: request `isActive` default true, written to `ACTIVE`) | request items use `productId, quantity, unitId`; response items use `prCode, qty, unit`. `mcGenFlg` / `mcConsFlg` are `"Y"/"N"` strings in the request and bool in the response. Response adds `logCd`, `uTime`, `productName`, `unitName`; request has no `isActive` | same names and types both ways; booleans; `isActive` default true | `DTOs/BomItemRequest.cs:21-37` vs `BomRequest.cs:163-179`; `BomRequest.cs:37,45` vs `:138,140` | PASS |
| `PUT /api/v1/boms/{id}` | R4 | same as POST | same | same | PASS (Batch E) |
| `DELETE /api/v1/boms/{id}` | R3 | soft delete sets `ACTIVE = 0` and `ISDELETED = 1` (`BomRepository.cs`, delete statement), so a deleted BOM is distinguishable from a deactivated one (reads filter `ISDELETED`) | none | `BomRepository.cs` | PASS (the stale note is wrong: delete sets `ACTIVE = 0` and `ISDELETED = 1`) |
| (dead code) | note | `BomMasterRequest`, `BomRequests` duplicates | remove | `DTOs/BomMasterRequest.cs`, `BomRequest.cs:95` | WON'T FIX (dead code) |

---

## ProductController (`api/v1/products`) — `Controllers/Master/ProductController.cs`

`ApiResponse<T>` with `SuccessWithPagination` (`ProductService.cs:418`, message "Products retrieved successfully."). **Discrepancy with `docs/api-common-issues.md` Example B:** the doc says `GET /products` has no `status`/`message`. In this source tree it does. Either the frontend is on an older build, or the Swagger-declared type misled them (Swagger shows `PagedResponse<ProductResponse>`, which has no status/message, at `ProductController.cs:104`). Verify against the running build before treating R1 as fixed.

JSON create (`POST /`) is commented out (`ProductController.cs:72`). Create exists only as `POST with-image` (multipart, product JSON inside form field `product`).

| endpoint | rule broken | current shape | expected shape | fix location | Final status |
|---|---|---|---|---|---|
| `GET /api/v1/products` | FIXED (Swagger) | Runtime envelope OK. Swagger type is `PagedResponse<ProductResponse>` | `ApiResponse<IReadOnlyList<...>>` | `ProductController.cs:103-105` | PASS |
| `GET /api/v1/products` | R2 — **WON'T FIX: frontend needs full data in list** | Each row is the full `ProductResponse` incl. nested `aliases[]`, `priceCategories[]`, `rowVersion` (byte[]), `image`, `delFlg`, `logCd` | flat summary row; nested arrays and `rowVersion` only in get-by-id | `DTOs/Product/ProductResponse.cs:96-103`, `ProductDB.cs:276-282` | WON'T FIX (frontend needs full data in the list) |
| `GET /api/v1/products` | R3 — **FIXED (Batch H)**: response `isActive` (`Active ?? true`), request `isActive` default `true` | Field is `active` (`bool?`), not `isActive`. `?active=` exists. Request `active` is `bool?` with no default, so an omitted value is `null` on create | `isActive`; default true | `ProductResponse.cs:56`, `ProductRequest.cs:356` | OPEN (rows with NULL `ACTIVE` show `isActive:true` but are skipped by the get-by-id and alias `ACTIVE = 1` filters) |
| `GET /api/v1/products/{id}` | R3/R4 — **FIXED (Batch H)**, see the PUT row | see POST row; `active` name | `isActive` | same | OPEN (get-by-id skips NULL-`ACTIVE` products, `ProductDB.cs:573`) |
| `PUT /api/v1/products/{id}` | R4 — **FIXED (Batch H) except `brandId` type** (needs approval). Names now match: request `description`, `categoryId`, `bmpl/bmpq/bmpw`, `slNoTrack`, `bmEqn`; response `notes`, `saleAccId`, `purAcctId`, `opnStkQty`, `opnStkValue`, price-category `categoryName/discountStructure/levelFrom/levelTo/discountPercent/effectiveValue/appliedPrice`; `aprvlst` added to the response. Old request names are still accepted for one release (`// TODO remove after frontend release`). `gdwnId`/`brCd` stay request-only (no `PRMAST` column) | Request (`ProductUpdateRequests`) vs response renames: `desc`→`description`, `catgId`→`categoryId`, `notes`→`prNotes`, `saleAccId`→`saleAcId`, `purAcctId`→`purAcId`, `_BMPL/_BMPQ/_BMPW`→`bmpl/bmpq/bmpw`, `_SLNOTRACK`→`slNoTrack`, `_BMEQN`→`bmEqn`, `opnStkQty/opnStkValue`→`opnStock/opnStkval`, `gdwnId`/`brCd` request only. Types: `brandId` int (request) vs string (response); `expDays` fine; `rowVersion` required in update but a byte[] in response | one name and one type per field; drop underscore-prefixed JSON names | `DTOs/ProductRequest.cs:260-428` (esp. 275,278,325,328,376-401) vs `DTOs/Product/ProductResponse.cs:10,17,18,41,42,72,74-82` | OPEN (`brandId` is `int?` in the request and `string?` in the response) |
| `PUT /api/v1/products/{id}` | R4 | Request has no `priceCategories`; response has `priceCategories[]` (`catName, discStruct, levFrom, ...`). Alias items: request `PrAliasRequests`, response `ProductAliasResponse` adds `productId`; `qty`, `unitId`, `salesPrice`, `purPrice` are nullable in request, non-null in response | symmetrical | `ProductRequest.cs:432,441-468` vs `ProductResponse.cs:116-144` | OPEN (alias `unitId`: the list alias query selects `UNITCODE` without `AS UnitId`, so it likely returns 0; get-by-id aliases it) |
| `POST /api/v1/products/with-image` | R4 — **FIXED (Batch H)**; the `product` JSON uses System.Text.Json, so new and old names both work | multipart; `product` is a JSON string. Uses `ProductRequests` (same renames as above) | JSON `POST /` with the same DTO as the response; image sent separately | `ProductController.cs:295-318`, `ProductRequest.cs:484-490` | PASS (`brandId` still open, see the PUT row) |
| `PUT /api/v1/products/{id}/with-image` | R4 — **FIXED (Batch H)** | same as above | same | `ProductController.cs:322` | PASS (`brandId` still open, see the PUT row) |
| `DELETE /api/v1/products/{id}` | OK | `{status,message}` | — | — | PASS |
| `POST /api/v1/products/image`, `PUT /api/v1/products/image` | R4 | multipart `ProductImageRequest {productId, imageFile}` | `/products/{id}/image` for consistency with GET/DELETE | `ProductController.cs:218,249` | OPEN (routes still `/products/image`, not `/products/{id}/image`; not changed) |
| `GET /api/v1/products/{id}/image` | R1 | returns `ProductImageResponse {prCode, image}` (`prCode` where everything else says `id`) | `{id,image}` | `ProductResponse.cs:106-109` | OPEN (`prCode` still used instead of `id`; not changed) |
| `DELETE /api/v1/products/{id}/image` | OK | — | — | — | PASS |

## ProductControllers (`api/v1/makedblank`) — `Controllers/Master/ProductControllerss.cs`

8,699-line backup copy. `IgnoreApi=true`, route `api/v1/makedblank` (a placeholder route that is still registered). Not traced endpoint by endpoint.

| endpoint | rule broken | current shape | expected shape | fix location | Final status |
|---|---|---|---|---|---|
| all `api/v1/makedblank/**` | n/a (backup) | live route with legacy DataTable code | remove from the build or exclude from compile (`.csproj`) | `Controllers/Master/ProductControllerss.cs:23-29` | WON'T FIX (backup copy; exclude from the build) |

---

## CustomerController (`api/v1/customers`) — `Controllers/Master/CustomerController.cs`

Hybrid: Dapper SQL in `DB/CustomerDB.cs:158+` (aliases already camelCase-able), but the controller builds anonymous `Ok(new {...})` objects instead of `ApiResponse<T>`. Does not use any class from `CLS/ApiResponse.cs` at runtime (only in Swagger attributes). Lines 626-1330 are mostly commented out.

| endpoint | rule broken | current shape | expected shape | fix location | Final status |
|---|---|---|---|---|---|
| `GET /api/v1/customers` | FIXED | `{status,data,pagination}`: **no `message`**. Error path `{status,message,error:ex.Message}` leaks the exception | `ApiResponse<T>.SuccessWithPagination("Customers retrieved successfully.", ...)` | `CustomerController.cs:59-73` (also 77-82) | PASS |
| `GET /api/v1/customers` | R2 | Rows are `clsCustomer` (≈70 fields) incl. `rowVersion` (byte[]), `logId`, and a `pendingBills[]` list (empty in list) | flat summary row (`id,cusName,alias,phone,email,isActive,...`); `pendingBills` and `rowVersion` only in get-by-id | `CLS/clsCustomer.cs:73-80`, `CustomerDB.cs:200-260` | OPEN (list rows are still the full `clsCustomer` with `rowVersion` and `pendingBills`) |
| `GET /api/v1/customers` | R3 | **FIXED (rule 3 Batch 2)** — Field is `active`, not `isActive`. `?active=` present and works (`@Active IS NULL OR ...`) | `isActive` | `CLS/clsCustomer.cs:12`, `CustomerDB.cs:208` (`C.ACTIVE AS Active`) | PASS (Batch 2: `isActive`) |
| `GET /api/v1/customers/{id}` | R1 (not a list) — **FIXED**: now `ApiResponse<clsCustomer>` with message "Customer retrieved successfully." (data unchanged) | `{status,data}`, no `message`; not-found returns `{status:false,message}` | `ApiResponse<T>` | `CustomerController.cs:111-115` | PASS |
| `POST /api/v1/customers` | R3/R4 | **FIXED (rule 4 Batch F; wire names, nullability, crDr)** — `CustomerRequest.active` defaults `true` (OK). Request vs `clsCustomer`: `salesMan` (int?) → `salesManId` (int); `ledgerGroupId` int? vs int; `stateCode` int vs int?; `crDr` default `"CR"` vs `"DR"`; `partyGrpId` / `prCategoryId` / `prListCd` partly missing from request; response adds `accountId, accountCode, disc, priceLevel, prTmplateId, pntSysId, itemGroupId, grpItem, logId` that the request cannot set | one DTO per entity, same names and types | `DTOs/CustomerRequest.cs:35-148` vs `CLS/clsCustomer.cs:6-78` | PASS (Batch F) |
| `PUT /api/v1/customers/{id}` | R4 | **FIXED (rule 4 Batch F)** — `CustomerUpdateRequest : CustomerRequest` (+ `rowVersion`); same differences | same | `CustomerRequest.cs:156-159` | PASS (Batch F) |
| `DELETE /api/v1/customers/{id}` | R1 (minor) | `{status,message}` anonymous | `ApiResponseWithSuccess` | `CustomerController.cs:605` | PASS (non-list, `{status,message}`) |
| `GET /api/v1/customers/lookups/ledger-groups` | R1/R2 — **FIXED** (`ApiResponseWithoutPagination<IReadOnlyList<LookupItem>>`, error path logged, no `ex.Message`) | Swagger `ApiResponseWithoutPagination<LookupItem>` (single object) but runtime returns an anonymous list `{id,name}`; no `pagination` | document array; decide on non-paged lookup envelope | `CustomerController.cs:680, 692` | PASS |
| `GET /api/v1/customers/lookups/salesman` | R1/R2 — **FIXED** (same as ledger-groups lookup) | same | same | `CustomerController.cs:946, 958` | PASS |

---

## SupplierController (`api/v1/suppliers`) — `Controllers/Master/SupplierController.cs`

Same family as Customer, but the **list already uses the standard envelope**: `ApiResponse<List<clsSupplierResponse>>.SuccessWithPagination("Suppliers retrieved successfully.", ...)` (`SupplierController.cs:92-96`). Lines 1228-2152 are commented-out legacy code. `clsSupplier` (`CLS/clsSupplier.cs`) is the response model; `SupplierRequest` is the input.

| endpoint | rule broken | current shape | expected shape | fix location | Final status |
|---|---|---|---|---|---|
| `GET /api/v1/suppliers` | FIXED (Swagger) | Runtime OK. Swagger declares `ApiResponse<List<clsSupplier>>` (a different class from the runtime `clsSupplierResponse`) | align Swagger type | `SupplierController.cs:49` | PASS |
| `GET /api/v1/suppliers` | R2 | Rows are `clsSupplierResponse` (≈70 fields) incl. `rowVersion` and nested `pendingBills[]` | flat summary rows; nested data only in get-by-id | `CLS/clsSupplier.cs:150-223` | OPEN (list rows are still the full `clsSupplierResponse` with `rowVersion` and `pendingBills`) |
| `GET /api/v1/suppliers` | R3 | **FIXED (rule 3 Batch 2)** — `active` (not `isActive`); `?active=` works | `isActive` | `CLS/clsSupplier.cs:13,157` | PASS (Batch 2: `isActive`) |
| `GET /api/v1/suppliers/{id}` | OK/R3 | **FIXED (rule 3 Batch 2)** — `active` name; otherwise enveloped | `isActive` | same | PASS |
| `POST /api/v1/suppliers` | R3/R4 | **FIXED (rule 3 Batch 2 + rule 4 Batch F; crDr uppercase)** — `active` defaults true (OK). `crDr` default is `"Cr"` in the response class; Customer uses `"DR"`/`"CR"`, so the same field has three casings across the API. `salesManId` is `int?` here and `int` on customers. Response carries `accountId`, `disc`, `priceLevel`, `logId`, `prTmplateId`, `pntSysId`, `itemGroupId`, `grpItem` that the request does not accept | one shared party DTO for customer and supplier; consistent `crDr` | `DTOs/SupplierRequest.cs:15-…` vs `CLS/clsSupplier.cs:7-81` | PASS (Batch F) |
| `PUT /api/v1/suppliers/{id}` | R4 | **FIXED (rule 4 Batch F)** — same as POST (+ `rowVersion`) | same | same | PASS (Batch F) |
| `DELETE /api/v1/suppliers/{id}` | OK | — | — | `SupplierController.cs:762` | PASS |
| `GET /api/v1/suppliers/lookups/ledger-groups` | R1/R2 — **FIXED** (array envelope; `ILogger` injected into `SupplierController`, error logged) | non-paged lookup, anonymous/`LookupItem` | document array; decide non-paged envelope | `SupplierController.cs:843` | PASS |

Not fully traced: the remaining `SupplierRequest` properties past line 39 were not read. The R4 list above is from `clsSupplier` against the first part of `SupplierRequest` and from the Customer comparison.

---

## AccountLedgerController (`api/v1/accounts`) — `Controllers/Master/AccountLedgerController.cs`

Hand-built responses over `DB/AccMasDB.cs`. The list uses the **older** `PaginationResponse<T>` (`DTOs/PaginationResponse.cs:14`) rather than `ApiResponse<T>`. Confirms `docs/api-common-issues.md` Example A. Shares the route prefix with `AccountsController`.

| endpoint | rule broken | current shape | expected shape | fix location | Final status |
|---|---|---|---|---|---|
| `GET /api/v1/accounts` | FIXED | `{data:[...],page,pageSize,totalRecords,totalPages}`: no `status`, no `message`, paging at top level, no `pagination` object. Errors are `{status,message=ex.Message}` | `ApiResponse<T>.SuccessWithPagination("Accounts retrieved successfully.", data, new PaginationInfo{...})` | `AccountLedgerController.cs:82-92` (type `PaginationResponse<AccountLedgerDtos>`) | PASS |
| `GET /api/v1/accounts` | FIXED | Swagger declares `ApiResponse<AccountLedgerDtos>`, which the runtime does not return | match runtime | `AccountLedgerController.cs:44` | PASS |
| `GET /api/v1/accounts` | R2 — **FIXED (rule 4 Batch G)**: response names now match the request (`accountName`, `secondName`, `groupId`, `vatPercentage`, `taxReturnFlag`, `bankAccountName`, `bankAccountNumber`, `ibanNumber`, `currencyFlag` (read-only, `CurFlg ?? false`), `currencyConversionRate`, `currencyCode`); `acCode` removed, `accountCode` kept; `openBal` replaced by read-only `openingBalance` (absolute) + `openingBalanceType` (`DR` if negative, else `CR`) | flat rows, camelCase, but shortened SQL-style names: `accName`, `acCode`, `openBal`, `vatPerc`, `taxRetFlag`, `bankAccName`, `bankAccNo`, `ibanNo`, `curFlg`, `curConvRate`, `curCode`; plus `rowVersion` (byte[]) | friendly names matching the request (see R4) | `DTOs/Respons-DTO/AccountLedgerDto.cs:6-47` | PASS |
| `GET /api/v1/accounts` | R3 — **FIXED (rule 3 Batch 2)**: response `isActive` (read-only, `Active ?? true`), request `isActive` (default true); `?active=` unchanged | field `active` (not `isActive`); `?active=` present | `isActive` | `AccountLedgerDto.cs:23`, `AccountLedgerRequest.cs:119` | PASS |
| `GET /api/v1/accounts/{id}` | R1/R4 — **R4 FIXED (Batch G)**, same names as the list row; R1 (anonymous envelope) not changed | `{status,message,data}` anonymous; `data` is `AccountLedgerDtos` or the longer detail class (lines 50-90, which uses strings for `crPeriod`, `crLimit`, `salesman`, `countryCode`, `grpItem`) | typed `ApiResponse<T>`; numeric fields numeric | `AccountLedgerController.cs:138-143`, `AccountLedgerDto.cs:50-90` | PASS (get-by-id has status, message, data) |
| `POST /api/v1/accounts` | R4 | Request → response renames: `accountName`→`accName`, `openingBalance`→`openBal`, `groupId`→`ledgerGroupId`, `vatPercentage`→`vatPerc`, `taxReturnFlag`→`taxRetFlag`, `bankAccountName/Number`→`bankAccName/No`, `ibanNumber`→`ibanNo`, `currencyFlag`→`curFlg`, `currencyCode`→`curCode`, `currencyConversionRate`→`curConvRate`, `openingBalanceType` (CR/DR) has no response field, `active` same. Types: `vatFlag` bool vs bool?; `accountCode` is not accepted but returned as `acCode` | one name per field in both directions | `DTOs/AccountLedgerRequest.cs:58-119` vs `AccountLedgerDto.cs:8-47` | PASS (Batch G) |
| `PUT /api/v1/accounts/{id}` | R4 | same as POST (+ `rowVersion`) | same | `AccountLedgerRequest.cs:148` | PASS (Batch G) |
| `GET /api/v1/accounts/{id}/opening` | OK (R1) | `ApiResponseWithoutPagination<AccountOpeningResponse>{accountId,openingBalance}`. Note: a zero opening balance returns "Account not found" (`opening==0`) | distinguish "missing account" from "balance is 0" | `AccountLedgerController.cs:785` | OPEN (minor: a zero opening balance returns "Account not found") |
| `GET /api/v1/accounts/group/{groupId}` | R1/R2 — **R2 FIXED (Batch G)**: same `AccountLedgerDtos` names; R1 not changed | anonymous `{status,message,data}`; not paged; Swagger says single `AccountLedgerDtos` | array + document; decide on paging | `AccountLedgerController.cs:826,852-857` | OPEN (anonymous envelope, not paged; names fixed in Batch G) |
| `DELETE /api/v1/accounts/{id}` | not traced | soft delete; response shape not read | — | `AccountLedgerController.cs:892` | PASS (non-list, `{status,message}`) |

## AccountsController (`api/v1/accounts/list-party`, hidden from Swagger) — `Controllers/AccountsController.cs`

Legacy-port style with `IDbConnectionFactory` and `ICurrentUser`. The only action is `GET list-party`. Same route prefix as `AccountLedgerController`.

| endpoint | rule broken | current shape | expected shape | fix location | Final status |
|---|---|---|---|---|---|
| `GET /api/v1/accounts/list-party?type=customer\|supplier` | R1 | Hand-made `PartyListResponse {status,message,data,pagination:PaginationResponse}`: correct fields, but a third envelope class (`DTOs/Respons-DTO/PartyQueryResult.cs:36`) | `ApiResponse<T>` + `PaginationInfo` | `AccountsController.cs:84-96` | OPEN (hidden endpoint, third envelope class; not changed) |
| same | R2 | `PartyAccountDto {id,accCode,accName,alias,secName,phone,vatRegNo,address,shipAddress}`: flat camelCase; `accCode`/`accName` differ from `acCode`/`accName` in `/accounts` (`acCode`) | one naming per concept | `PartyQueryResult.cs:15-34`, `AccountLedgerDto.cs:10` | OPEN (`accCode` vs `accountCode` naming; not changed) |
| same | R3 | no `isActive` field and no `?active=` filter | add if the party list should show inactive | `AccMasDB.AccountsForSales/AccountsForPurchase` | OPEN (no `isActive` or `?active=`; not changed) |
| same | R1 (errors) | errors are `{status:false,message:ex.Message,errors:[]}` (leaks exception text) | `ApiResponseWithError` | `AccountsController.cs:134` | OPEN (exception text returned to the client; not changed) |

---

## LedgerGroupController (`api/v1/ledger-groups`) — `Controllers/Master/LedgerGroupController.cs`

Controller builds the envelope by hand with an anonymous object that has the right fields (`status,message,data,pagination`); it does not use `ApiResponse<T>`. Lines 840+ are commented-out legacy code. DB layer: `LedgerGroupDB`.

| endpoint | rule broken | current shape | expected shape | fix location | Final status |
|---|---|---|---|---|---|
| `GET /api/v1/ledger-groups` | FIXED (Swagger + envelope: now `ApiResponse<IReadOnlyList<LedgerGroupDto>>.SuccessWithPagination`, JSON unchanged) | Runtime fields correct, but built as an anonymous object. Swagger declares `ApiResponse<LedgerGroupDto>` (single object) | use `ApiResponse<IReadOnlyList<...>>.SuccessWithPagination`; Swagger array | `LedgerGroupController.cs:33,62-74` | PASS |
| `GET /api/v1/ledger-groups` | R2/R3 | flat camelCase rows with `isActive`; `?active=` present | OK | `DTOs/Respons-DTO/LedgerGroupDto.cs:18-28` | PASS |
| `GET /api/v1/ledger-groups/{id}` | **FIXED** (`ApiResponse<LedgerGroupDto>`; errors `ApiResponseWithError`) | `{status,message,data}` (anonymous) | `ApiResponse<T>` for consistency | `LedgerGroupController.cs:107-112` | PASS |
| `POST /api/v1/ledger-groups` | FIXED (Batch C) | request `groupCode:int?`, `sequenceNo:int?`, `crDr:string?`, `finalAccount:string?`, `secondName:string?`; response non-null `int`/`string`. `isActive` default true (OK) | same nullability | `DTOs/LedgerGroupRequest.cs:11-38` vs `LedgerGroupDto.cs:20-27` | PASS |
| `PUT /api/v1/ledger-groups/{id}` | R4 (type) | same | same | same | PASS (Batch C) |
| `DELETE /api/v1/ledger-groups/{id}` | not traced | — | — | `LedgerGroupController.cs:613` | PASS (non-list, `{status,message}`) |
| `GET /api/v1/ledger-groups/customer-supplier`, `GET .../lookups/final-accounts` | **FIXED** (`ApiResponseWithoutPagination<IReadOnlyList<...>>`, data always array; Swagger updated) | non-paged lookups | document shape | `LedgerGroupController.cs:751,785` | PASS |

---

## UsersController (`api/v1/auth`) — `Controllers/UsersController.cs`

New-style (`IAuthService`). Not lists and not masters, so R1-R3 do not apply. Lines 130+ are commented-out legacy code.

| endpoint | rule broken | current shape | expected shape | fix location |
|---|---|---|---|---|
| `POST /api/v1/auth/login` | OK | `ApiResponseWithoutPagination<LoginResponse>`. Swagger uses the same class for the 400/401/429/500 responses, whose runtime body is the same envelope with `data` absent | — | — |
| `POST /api/v1/auth/logout` | R1 (minor) | Missing header returns an anonymous `{status,message}` (`Unauthorized(new {...})`) while other paths return `ApiResponseWithError`/service result | use `ApiResponseWithError.Failure` | `UsersController.cs:101-105` |

## CompaniesController (`api/Companies/ListCompany`, hidden from Swagger) — `Controllers/CompaniesController.cs`

Legacy route (`api/[controller]`, not `api/v1`). Reads companies from `MasterDB` for the server/user/password passed **in the query string**.

| endpoint | rule broken | current shape | expected shape | fix location |
|---|---|---|---|---|
| `GET /api/Companies/ListCompany?serverName&userName&password` | R1 | `{status,message,data:[...]}` with no `pagination`; not-found returns HTTP 404 with `{status:false}` (other endpoints return 400); a DB failure returns `status:"error"` (string) while others use a bool | `ApiResponse<T>`; `/api/v1/companies` | `CompaniesController.cs:46,59-72` |
| same | R2 | rows are raw DataTable keys `ID`, `COMPNAME` (uppercase) | `{id,companyName}` | `DB/CompMasDB.cs:20` |
| same | R3 | hard-coded `ACTIVE=true` (string concatenated into SQL); no `isActive` field, no `?active=` | `isActive` + filter if needed | `CompMasDB.cs:20` |
| same | note (security) | `password` in a GET query string is logged by proxies and browsers; SQL is built with string concatenation; exception text returned to the client | POST body or server-side config | `CompaniesController.cs:16,77` |

## VersionController (`/Version`) — `Controllers/Version.cs`

| endpoint | rule broken | current shape | expected shape | fix location |
|---|---|---|---|---|
| `GET /Version` | out of scope | `{version:"4.000"}` — not under `/api/v1`, no envelope | `api/v1/version` + `ApiResponse<T>` if the frontend uses it | `Version.cs:6,12-15` |

## BatchController (`api/v1/batch`, hidden from Swagger) — `Controllers/BatchController.cs`

Legacy-port style. Query: `productId`, `godownId`, `page`, `pageSize` (`PaginationRequest`).

| endpoint | rule broken | current shape | expected shape | fix location |
|---|---|---|---|---|
| `GET /api/v1/batch/list-pending` | R1 | `pagination` uses `pageNumber` (not `page`) and adds `hasPreviousPage`, `hasNextPage` | `pagination:{page,pageSize,totalRecords,totalPages}` | `BatchController.cs:105-113,142-150` |
| same | R1 | Paging is done in memory over the whole `PendingList` DataTable | SQL `OFFSET/FETCH` | `BatchController.cs:124-126`, `DB/PrstDB.cs:18-22` |
| same | R2 | Rows are dictionaries of raw SQL column names (long concatenated query in `PrstDB.PendingList`) | camelCase flat DTO | `DB/PrstDB.cs:20` |
| same | R1 (errors) | 404 for missing product (others 400), `ex.Message` returned, extra `errors:[]` key | `ApiResponseWithError` | `BatchController.cs:80-85,164` |

## SerialNumberController (`api/v1/serial-number`, hidden from Swagger) — `Controllers/SerialNumberController.cs`

Same pattern as Batch (`productId`, `unitId`, `godownId`, paging).

| endpoint | rule broken | current shape | expected shape | fix location |
|---|---|---|---|---|
| `GET /api/v1/serial-number/list-pending` | R1 | `pagination.pageNumber` + `hasPreviousPage`/`hasNextPage` | standard `PaginationInfo` | `SerialNumberController.cs:109-117` |
| same | R2 | rows from `SntStkDB` DataTable dictionaries (raw column names) | camelCase flat DTO | `SerialNumberController.cs:90-100` |
| same | R1 (errors) | `{status,message=ex.Message,errors:[]}` | `ApiResponseWithError` | `SerialNumberController.cs` |

---

## SalesOrderController (`api/v1/sales-orders`) — `Controllers/Transactions/Sales/SalesOrderController.cs`

Legacy-port style: builds `DBConn`/`Variable`, runs `CordMDB.ListAsync("O")` (`DB/CordMDB.cs:20-74`), returns anonymous objects. Uses the older `PaginationResponse` class (`DTOs/PaginationResponse.cs:3`) for the pagination block. RPC-style routes by design (`manage`, `list`, `get/{id}`, `cancel/{id}`).

| endpoint | rule broken | current shape | expected shape | fix location |
|---|---|---|---|---|
| `GET /api/v1/sales-orders/list` | R1 | `{status,message,data,pagination:{page,pageSize,totalRecords,totalPages}}` is **correct**. Paging is real (SQL `OFFSET`, `COUNT(*) OVER()`). Built as an anonymous object, not `ApiResponse<T>`; no `ProducesResponseType` so Swagger shows no schema | use `ApiResponse<T>`; add Swagger types | `SalesOrderController.cs:165-181` |
| `GET /api/v1/sales-orders/list` | R2 | rows are DataTable dictionaries keyed `Id`, `[Order No]`, `[Order Date]`, `Customer`, `Amount` — keys with spaces and capitals; `Amount` and `Order Date` are pre-formatted **strings** (`FORMAT(...)`) | `{id,orderNo,orderDate,customerName,amount}` with real number/date types | `DB/CordMDB.cs:68-73` |
| `GET /api/v1/sales-orders/get/{id}` | R4 | `data:{header, items, additionalInformations}` built from legacy classes (`ClsCordM`, `ClsCordT`, `ClsCordD`), while `POST manage` takes `{sordNo, date, customerCode, salesmanCode, discPerc, discAmt, roundOff, items[], additional[]}` | same field names for the same data in both directions | `SalesOrderController.cs:309-319` vs `DTOs/SalesOrderRequest.cs:5-52` |
| `POST /api/v1/sales-orders/manage` | R4 | see above; `sordNo` (request) vs `OrdNo` (response class) | align | `DTOs/SalesOrderRequest.cs:5` |
| `POST /api/v1/sales-orders/cancel/{id}` | not traced | — | — | `SalesOrderController.cs:438` |
| `GET /api/v1/sales-orders/pending-list` | R1/R2 | not read in detail; same pattern (DataTable dictionaries) | same as list | `SalesOrderController.cs:501` |
| all error paths | R1 | `{status:false,message:ex.Message,errors:[]}` leaks exception text on the list endpoint; get/{id} masks it | `ApiResponseWithError` | `SalesOrderController.cs:185,190` |

## SalesInvoiceController (`api/v1/sales-invoices`) — `Controllers/Transactions/Sales/SalesInvoiceController.cs`

Same pattern. List runs `SalMDB.List` (`DB/SalMDB.cs:21-79`). Idempotency header on `manage`.

| endpoint | rule broken | current shape | expected shape | fix location |
|---|---|---|---|---|
| `GET /api/v1/sales-invoices/list` | R1 | envelope and `pagination` fields correct (old `PaginationResponse`); anonymous object; no Swagger schema | `ApiResponse<T>` | `SalesInvoiceController.cs:173-188` |
| `GET /api/v1/sales-invoices/list` | R2 | keys `Invoice_Id`, `Inv_No`, `Date`, `Customer`, `Taxable`, `Tax`, `Amount` (underscored/capitalised, `Taxable`/`Tax` only for one `SType`); amounts and dates are formatted strings | `{id,invoiceNo,invoiceDate,customerName,taxableAmount,taxAmount,amount}` | `DB/SalMDB.cs:79` |
| `GET /api/v1/sales-invoices/get/{id}` | R4 | header/items/additional returned in legacy shape; request is `SalesInvoiceRequest` | align names | `SalesInvoiceController.cs:205`, `DTOs/SalesInvoiceRequest.cs` |
| `POST /api/v1/sales-invoices/manage` | R4 | same as above | same | `SalesInvoiceController.cs:29-30` |
| `POST /api/v1/sales-invoices/cancel/{id}` | not traced | — | — | `SalesInvoiceController.cs:591` |
| `GET /api/v1/sales-invoices/pending?salesReturnId&SalesInvoiceNumber` | R1/R2 | not paged lookup, DataTable-based; query parameter `SalesInvoiceNumber` is PascalCase while the others are camelCase | camelCase query names | `SalesInvoiceController.cs:654-655` |

## SalesReturnController (`api/v1/sales-returns`) — `Controllers/Transactions/Sales/SalesReturnController.cs`

Same pattern as invoices (`SalMDB.List`, `SalesReturnDB`).

| endpoint | rule broken | current shape | expected shape | fix location |
|---|---|---|---|---|
| `GET /api/v1/sales-returns/list` | R1/R2 | same as invoice list: correct fields, anonymous object, underscored/capitalised keys, formatted strings | same fix | `SalesReturnController.cs:153-168`, `DB/SalMDB.cs:79` |
| `GET /api/v1/sales-returns/get/{id}` | R4 | legacy header/items/additional vs `SalesReturnRequest` | align names | `SalesReturnController.cs:182-240` |
| `POST /api/v1/sales-returns/manage` | R4 | as above | same | `SalesReturnController.cs:24-25` |
| `POST /api/v1/sales-returns/cancel/{id}` | not traced | — | — | `SalesReturnController.cs:292` |

---

## Account reports (`api/v1/reports/*`, `Controllers/Reports/Accounts/`)

All 13 controllers are `IgnoreApi=true` (hidden from Swagger) and use the legacy-port style: build `DBConn`/`Variable`, run a `DB/*DB.cs` method (mostly `DayBookDB`, `AccMasDB`, `PurmDB`), turn the resulting `DataTable` into a list of dictionaries, and return an anonymous object. None uses `ApiResponse<T>`; none documents a response type. Each has a single `POST` taking a `<Report>Request` body (R4 request/response symmetry does not apply to filter requests). Errors are `StatusCode(500, new {status=false, message=ex.Message, errors=[]})` (leaks exception text).

Row keys come straight from `DataTable` column names defined in the controller (`dt.Columns.Add("AccName")`), so they are PascalCase (`AccName`, `OpenBal`, `DebitGroup`), UPPERCASE (`VRNO`, `VRDATE`) or all-lowercase concatenations (`recparticulars`). Amounts and dates are often pre-formatted strings.

| endpoint | rule broken | current shape | expected shape | fix location |
|---|---|---|---|---|
| `POST /api/v1/reports/day-book` | R1 | `{status,message,data,pagination:{page,pageSize,totalRecords,totalPages}}` — shape is correct, but built by hand; paging is in memory (`dt.Skip/Take`) after building the whole report | `ApiResponse<T>` + SQL paging (or keep in memory, but document) | `DayBookController.cs:191-262` |
| `POST /api/v1/reports/day-book` | R2 | rows `{vrdate,recid,recparticulars,rectype,reccash,recamt,payid,payparticulars,paytype,paycash,payamt,type}`: lowercase run-together names; every value converted to string (amounts, dates) | camelCase `voucherDate`, `receiptId`, `receiptParticulars` …; numbers as numbers | `DayBookController.cs:198-250` |
| `POST /api/v1/reports/cash-book` | R1/R2 | has `status,message,data,pagination` (correct). Rows `{vrdate,invtext,recid,recparticulars,rectype,reccash,paycash,type,remarks,...}`: same lowercase style; totals (`TotRec`,`TotPay`) mixed in | same fixes as day-book | `CashBookController.cs:176-210` |
| `POST /api/v1/reports/ledger` | R1 | `{status,data,pagination}`: **no `message`**; pagination key is `pageNumber` (not `page`) and adds `hasPreviousPage`/`hasNextPage`. Paging in memory | `page`, no extras; add `message` | `LedgerController.cs:619-632` |
| `POST /api/v1/reports/ledger` | R2 | raw dictionary keys `VRNO, VRDATE, VRTYPE, REFNO, BILLNO, ACCOUNT, …` (UPPERCASE) | camelCase | `LedgerController.cs:95-100,608-617` |
| `POST /api/v1/reports/cash-and-bank-book` | R1/R2 | `{status,columns,data}`: no `message`, no `pagination`; extra `columns` array (dynamic columns); raw column-name dictionaries | `ApiResponse<T>`; pagination or documented as unpaged | `CashAndBankBookController.cs:343-345` |
| `POST /api/v1/reports/cash-flow-statement` | R1/R2 | `{status,data}` only; rows are dictionaries from `dtDT` or `dtMN` depending on `request.DateWise`, so the column set changes with a flag | stable shape per endpoint | `CashFlowStatementController.cs:241-242` |
| `POST /api/v1/reports/daily-balance` | R1/R2 | `{status,data}`; raw dictionaries | same | `DailyBalanceController.cs:92-93` |
| `POST /api/v1/reports/daily-closing` | R1/R2 | `{status,data}`; raw dictionaries | same | `DailyClosingController.cs:149-150` |
| `POST /api/v1/reports/ledger-opening-balance` | R1/R2 | `{status,data}`; raw dictionaries | same | `LedgerOpeningBalanceController.cs:124-125` |
| `POST /api/v1/reports/ledger-opening` | R1/R2 | `{status,data}`; raw dictionaries | same | `LedgerOpeningController.cs:114-115` |
| `POST /api/v1/reports/month-wise-profit-and-loss` | R1/R2 | `{status,data}`; raw dictionaries from `dtRep` | same | `MonthWiseProfitAndLossController.cs:455-456` |
| `POST /api/v1/reports/profit-and-loss` | R1/R2 | `{status,data}`; columns `DebitGroup, Debit, CreditGroup, Credit, ID, Type` (PascalCase; two-sided layout) | same | `ProfitAndLossController.cs:119-124,167-168` |
| `POST /api/v1/reports/receipt-and-payment` | R1/R2 | `{status,data,fullData}`: second array `fullData` has no counterpart elsewhere; raw dictionaries | one `data` object with named sections | `ReceiptAndPaymentController.cs:311-313` |
| `POST /api/v1/reports/trial-balance` | R1/R2 | `{status,data}`; columns `AccId, AccName, OpenBal, OpCredit, OpDebit, Debit, …` | same | `TrialBalanceController.cs:88-93,727-728` |
| `POST /api/v1/reports/voucher-query` | R1/R2 | `{status,data}`; raw dictionaries | same | `VoucherQueryController.cs:73-74` |

R3 does not apply to reports.

---

## Purchase reports (`Controllers/*PurchaseReport*.cs`, `PurchaseSummary`, `PurchaseTax*`, `BillwisePurchaseSummary`, `SupplierWiseProductReport`)

All nine are `IgnoreApi=true` (hidden from Swagger) legacy ports. Eight use the **unversioned** `api/[controller]` route (PascalCase, no `v1`); `PurchaseSummary` uses `api/v1/purchase/summary`. Each has one `POST` with a `<Report>Request` body (R4 request/response symmetry n/a). Common problems for all of them:

- `status` is the **string** `"success"` / `"error"` (a boolean `true`/`false` everywhere else). `SupplierWiseProductReport` returns `status: true`. `BillwisePurchaseSummary` mixes `false`, `"error"` and `"success"` in one action.
- Rows are dictionaries built from `DataTable` column names (raw SQL / report column names).
- Error responses return `error = ex.Message` or `message = ex.Message`.

| endpoint | rule broken | current shape | expected shape | fix location |
|---|---|---|---|---|
| `POST /api/GroupwisePurchaseReport` | R1 | `{status:"success",data}`: string status, no `message`, no `pagination`; route not under `api/v1` | `ApiResponse<T>`; `/api/v1/reports/groupwise-purchase` | `GroupwisePurchaseReportController.cs:15,113-116` |
| `POST /api/GroupwisePurchaseReport` | R2 | raw dictionary keys | camelCase DTO | `GroupwisePurchaseReportController.cs:105` |
| `POST /api/LedgerGroupwisePurchaseReport` | R1/R2 | same | same | `LedgerGroupwisePurchaseReportController.cs:16,101-104,93` |
| `POST /api/PurchaseDetailReport` | R1/R2 | `{status:"success",data}`; rows built field by field into `Dictionary<string,object?>` | same | `PurchaseDetailReportController.cs:16,227-231` |
| `POST /api/ProductwisePurchaseReport` | R1/R2 | same as Groupwise | same | `ProductwisePurchaseReportController.cs:16,155-158,147` |
| `POST /api/v1/purchase/summary` | R1 | `{status:"success",message,data,pagination:{page,pageSize,totalCount,totalPages,hasPreviousPage,hasNextPage}}`: string status, `totalCount` instead of `totalRecords`, extra `hasPreviousPage`/`hasNextPage`; paging in memory | `status:true`, `totalRecords`, no extras | `PurchaseSummaryController.cs:136-149` |
| `POST /api/v1/purchase/summary` | R2 | raw dictionary keys | camelCase | `PurchaseSummaryController.cs:127` |
| `POST /api/PurchaseTaxReport` | R1/R2 | `{status:"success",data}` | same as Groupwise | `PurchaseTaxReportController.cs:16,95-98,89` |
| `POST /api/PurchaseTaxReportDetailed` | R1/R2 | `{status:"success",data}` | same | `PurchaseTaxReportDetailedController.cs:16,98-101,90` |
| `POST /api/BillwisePurchaseSummary` | R1 | `{status:"success",data}`; failures return `status:false` (token) or `"error"` (db) in the same action | one status type | `BillwisePurchaseSummaryController.cs:64,152,305-309` |
| `POST /api/BillwisePurchaseSummary` | R2 | raw dictionary keys | camelCase | `BillwisePurchaseSummaryController.cs:288` |
| `POST /api/SupplierWiseProductReport` | R1/R2 | `{status:true,data}`: bool status (unlike the others), no `message`/`pagination`; failures use `"error"` strings | one status type | `SupplierWiseProductReportController.cs:232-236,54,68,110` |

R3 does not apply to reports.

---

## Summary

All 58 controller files under `Controllers/` were audited (the 8,699-line `ProductControllerss.cs` backup as a single row; `VersionController` as out of scope).

Counts are **distinct endpoint labels in the tables above** (178 in total), computed from the table rows. An endpoint that breaks two rules is counted under each, so the rule counts do not add up to the total. A combined cell such as `POST, PUT /{id}` counts once. Treat the numbers as accurate to a few endpoints, not exact.

| Result | Endpoints |
|---|---|
| **R1** list envelope broken (missing `status`/`message`/`pagination`, wrong pagination keys, string `status`, top-level paging) | 52 |
| R1, Swagger only (runtime is correct, `ProducesResponseType` says `data` is a single object) | 13 more |
| **R2** list rows not flat camelCase (raw SQL/DataTable keys, nested detail, internal fields, formatted strings) | 55 |
| **R3** `isActive` / `?active=` / default-true problems (see below) | 27 |
| **R4** request and response names or types differ | 49 |
| Pass all rules (`OK`) | 49 |
| Not traced, or out of scope | 8 |
| Other | 1 |
| Endpoints with at least one broken rule | 120 |

### Patterns behind the numbers

1. **R1 is mostly the legacy-port controllers.** Account reports, purchase reports, Batch, SerialNumber, Companies and the sales `list` endpoints return anonymous objects. Only `/accounts` (old `PaginationResponse<T>`) and `/customers` (no `message`) break R1 among the endpoints the frontend already uses. `/products` already returns the full envelope in this source tree.
2. **R2:** every DataTable-driven endpoint exposes raw column names: UPPERCASE (`VRNO`), PascalCase (`AccName`), underscored (`Invoice_Id`), with spaces (`[Order No]`), or lowercase run-together (`recparticulars`). New-style masters are flat camelCase, but six leak internal fields (`logCd`, `logCode`, `updateTime`, `rowVersion`) and four embed nested detail in list rows (BOM, PriceCategory, Product, Customer/Supplier `pendingBills`).
3. **R3:** the `active` vs `isActive` split is exactly as in the doc: Accounts, Customers, Suppliers, Products, Countries, Categories (legacy) use `active`; the other masters use `isActive`. Other R3 findings: `/countries` and `/regions` have no `?active=` filter (both hard-code `WHERE ACTIVE = 1`), `/regions` request defaults `isActive` to `false`, `/boms` filters on `ACTIVE` but never returns it, `/regions` SQL selects `IsActive` but the DTO drops it, `/products` and `/countries` have no default-true on create, Brand/PriceCategory/UnitConversion/Currency tables have no active column at all.
4. **R4:** the biggest groups are Accounts, Products, BOMs, ItemGroup (`"Y"/"N"` strings vs bool), CurrencyConversion (`details[].currencyId` vs `alternateCurrencyId`) and the sales transactions (`header/items/additionalInformations` vs flat request). Many other masters have only nullability differences (`int?` vs `int`, `string?` vs `string`).
5. **Duplicates and dead routes that are still live:** `api/v1/categories` (replaced by item-categories), `api/v1/currenciesssss` (replaced by currencies), `api/v1/makedblank` (backup copy of Products), `api/v1/accounts` served by two controllers, `api/Companies` and nine `api/[controller]` purchase report routes outside `/api/v1`.

### Limits of this audit

- Based on reading source; nothing was run. R1 and R2 claims for the new-style masters come from the DTO classes and repository SQL aliases, not from live JSON, so a `JsonIgnore` or custom serializer setting could change them.
- Not traced in detail: DELETE bodies on AccountLedger/LedgerGroup, `cancel/{id}` and `pending*` actions on sales controllers, the lookup endpoints on LedgerGroup, and the Supplier request class past its first properties.
- The remaining `DB/*DB.cs` report classes were not read line by line. Column names for reports come from the `Columns.Add(...)` calls in the controllers or from the first aliases found.

---

## Final status (masters) — 25 November 2026

The `Final status` column was added to every master table above (Brands through LedgerGroup), from a re-check of the current code. Sections after LedgerGroup (Users, Companies, Version, Batch, SerialNumber, sales transactions, account reports, purchase reports) are out of scope for this phase and have no status. Nothing was run against the database.

**Corrections to older notes in this file:** the ItemGroup R4 `"Y"/"N"` rows apply only to the legacy `ItemGroupMasterRequest` (backup controller), the live request is typed. The BOM delete row is wrong: delete sets `ACTIVE = 0` and `ISDELETED = 1`. `GET /accounts` and `GET /customers` now return the full envelope.

### OPEN items

| # | Item | Why it is open | Where |
|---|---|---|---|
| 1 | Products `brandId` | Request `int?`, response `string?`. `PRMAST.BRAND` is `nvarchar(255)`, so non-numeric values would be lost on a round trip. Needs an owner decision. | `ProductRequest.cs`, `ProductResponse.cs`, rule4-plan "Needs approval" 1 |
| 2 | Countries `?active=` filter | List and count are still `WHERE ACTIVE = 1`. Needs SQL change. | `CountryRepository.cs:51,91`; rule3-plan, rule4-plan "Needs approval" 2 |
| 3 | Countries request `isActive` | `ACTIVE` is not written on insert or update. Needs save-logic change. Delete = deactivate was accepted, so deleted countries would reappear once inactive rows are listed. | `CountryRepository.cs:235,265-271`; rule4-plan "Needs approval" 3 |
| 4 | Countries get-by-id | Still `AND ACTIVE = 1`, so an inactive country cannot be fetched. | `CountryRepository.cs:155` |
| 5 | Countries `vatCharAllow` | In the request, still commented out in the response. | `CountryDto.cs:13,44,134` |
| 6 | Products with NULL `ACTIVE` | The C# `isActive` shows NULL as `true`, but get-by-id and the alias queries use `ACTIVE = 1` and skip those rows. The list filter treats NULL as inactive (`ISNULL(P.ACTIVE,0) = @Active`). Needs `ISNULL(...)` in SQL and a data decision. | `ProductDB.cs:130,319,405,573,602`; rule4-plan "Needs approval" 4 |
| 7 | Product alias `unitId` | The list alias query selects `UNITCODE` with no `AS UnitId` (`ProductDB.cs:398`), while get-by-id selects `UNITCODE As UnitId` (`:595`). The list likely returns `unitId: 0`. Static reading only, not run. Needs a one-line SQL alias fix. | `ProductDB.cs:391-407` |
| 8 | Product `gdwnId`, `brCd` | Request-only. `PRMAST` has no such columns (D6). Document only. | rule4-plan "Needs approval" 5 |
| 9 | Product image routes | `POST/PUT /products/image` and `GET /products/{id}/image` returns `prCode`. Not changed. | `ProductController.cs:218,249` |
| 10 | Customers and Suppliers list rows | Still the full entity (about 70 fields, `rowVersion`, `pendingBills[]`). Products has the same shape but is WON'T FIX by decision. | `clsCustomer.cs`, `clsSupplier.cs` |
| 11 | `GET /accounts/group/{groupId}` | Anonymous envelope and not paged. | `AccountLedgerController.cs:826` |
| 12 | `GET /accounts/{id}/opening` | A zero opening balance returns "Account not found". | `AccountLedgerController.cs:785` |
| 13 | `GET /accounts/list-party` | Hidden endpoint: third envelope class, `accCode` naming, no `isActive`, exception text returned. | `AccountsController.cs` |
| 14 | `vat-types` | `id` is still the string name, not a numeric code. Envelope is fixed. | `CommonController.cs:21-34` |
| 15 | Categories `?active=` | Not touched; hidden legacy controller, superseded by item-categories. | `CatgMasDB.cs` (rule3-plan) |
| 16 | Frontend release | Batches D to H and rule 3 renames are breaking and not yet released to the frontend. | rule3-plan, rule4-plan |
| 17 | Unverified | `PARTYGROUPMAS` short-name column check, and whether `APRVLST`/`GDWNID`/`BRCD` can be returned (D6). | rule4-plan "Not verified" |

### Decisions on record
- Countries: delete = deactivate is accepted (no schema change).
- PartyGroups and Brand: excluded from rule 3 (no `ACTIVE` column).
- Products list keeps full rows (frontend needs them).
