# Rule 3 plan

Target: response `isActive` (bool) in list and get-by-id · request `isActive` (bool), default true on create · list `?active=true|false`, omitted returns both.

Legend: OK = already matches. All paths are relative to the project root. Facts were checked against the code, not only `docs/api-audit.md`.

| master | response field now | request field now | ?active= filter now | default on create now | omitted filter returns now | changes needed | files (file:line) |
|---|---|---|---|---|---|---|---|
| Accounts (`/accounts`) | `active` (`bool?`) in list and get-by-id | `active` (`bool`) | yes (`@Active IS NULL OR ...`) | `true` | both | rename response and request to `isActive` | `DTOs/Respons-DTO/AccountLedgerDto.cs:23,59`; `DTOs/AccountLedgerRequest.cs:119`; `Controllers/Master/AccountLedgerController.cs:50`; `DB/AccMasDB.cs:394,429,482,617` |
| Customers | `active` (`bool`) | `active` (`bool`) | yes | `true` | both | rename to `isActive` (response and request) | `CLS/clsCustomer.cs:12`; `DTOs/CustomerRequest.cs:32`; `Controllers/Master/CustomerController.cs:49`; `DB/CustomerDB.cs:177,269` |
| Suppliers | `active` (`bool`), also nested `clsSupplierResponse.Active` | `active` (`bool`); `SupplierRequestUpdatedDto.Active` has no default | yes | `true` (create), none on the update DTO | both | rename to `isActive`; give the update DTO the same default | `CLS/clsSupplier.cs:13,157`; `DTOs/SupplierRequest.cs:31,172`; `Controllers/Master/SupplierController.cs:57`; `DB/SupplierDB.cs:461,542` |
| Products | `active` (`bool?`) in list and get-by-id | `active` as `bool` (`ProductRequest`, no default), as `bool?` (`ProductRequests`) | yes | **`false`/NULL.** `bool?` omitted becomes SQL NULL (`PRMAST.ACTIVE` is nullable); `ProductRequest.Active` omitted becomes `false`. | both. A NULL-active row never matches `?active=true`. | rename to `isActive`; default `true` and non-null in the request; backfill NULL in the SQL insert (`request.Active ?? true`) | `DTOs/Product/ProductResponse.cs:56`; `DTOs/ProductRequest.cs:67,356`; `Controllers/Master/ProductController.cs:117`; `Repository/Master/ProductDB.cs:75,130,1206,1623` |
| Countries | `active` (`bool?`), get-by-id too | no active field | **no.** Hard-coded `WHERE ACTIVE = 1`. | n/a: the column is not set on insert, the DB default applies (unverified: COUNTRY is in MasterDB, not in `QuickBalance.sql`) | **active only** | rename to `isActive`; add `?active=`; add `isActive` (default true) to the request. **Needs a decision first:** `DeleteCountry` is `SET ACTIVE = 0`, so delete and deactivate are the same state. | `DTOs/CountryDto.cs:19,126`; `Controllers/Master/CountriesController.cs`; `Repository/Master/CountryRepository.cs:51,91,155,171,235,265-271` |
| Regions (**FIXED, Batch 1**) | **none.** SQL selects `ACTIVE AS IsActive` but `RegionResponse` has no property. | `isActive` (`bool`, no default so `false`) | **no.** Hard-coded `WHERE ACTIVE=1`. | `false` | **active only** | add `isActive` to the response; add `?active=`; request default `true` | `DTOs/RegionResponse.cs`; `DTOs/RegionRequest.cs:28`; `Controllers/Master/RegionController.cs`; `Repository/Master/RegionRepository.cs:26,52,77,123,138,163,227-232` |
| Boms (**FIXED, Batch 1**) | list **OK** (`isActive`, fixed earlier). **get-by-id: none.** `BomMasterDto` has no status and the get-by-id SQL does not select `ACTIVE`. | **none** | yes | not written on create or update, so the DB default `1` applies (`QuickBalance.sql:5074`) | both | add `isActive` to get-by-id; add `isActive` (default true) to the request and write it | `DTOs/BomRequest.cs:8,95,129-147`; `Controllers/Master/BomController.cs:62`; `Repository/Master/BomRepository.cs:170-183,286,320,520-527` |
| ItemGroup | `isActive` (OK) | `ItemGroupRequest.IsActive` `bool` default `true` (OK). Legacy `ItemGroupMasterRequest.IsActive` is `"Y"/"N"` but is used only by `ProductControllerss.cs:7507` (backup controller). | yes | `true` | both | none for the live API; the audit's `"Y"/"N"` note applies only to the legacy class | `DTOs/ProductCategoryRequest.cs:37`; `DTOs/Respons-DTO/ProductCategoryResponse.cs:19,38`; `Controllers/Master/ItemGroupController.cs:57`; `Repository/Master/ItemGroupDBs.cs:148` |
| Units (**FIXED, Batch 1**) | `isActive` (OK) | `isActive` default `true`; `UnitMasterResponse.IsActive` defaults to `false` | yes | `true` | both | change the `false` default on the response class to `true` (cosmetic) | `DTOs/UnitMasterRequest.cs:17,32`; `Controllers/Master/UnitsController.cs:44`; `Repository/Master/PrUnitMasDB.cs:152` |
| Taxes | `isActive` (OK) | `isActive` default `true` | yes | `true` | both | none | `DTOs/Tax/TaxMasterRequest.cs:19`; `DTOs/Tax/TaxMasterResponse.cs:12`; `Controllers/Master/TaxesController.cs:62`; `Repository/Master/TaxMasDB.cs:120` |
| Salesman | `isActive` (OK) | `isActive` default `true` | yes | `true` | both | none | `DTOs/SalesMan/SalesmanRequest.cs:24`; `SalesmanResponse.cs:26`; `Controllers/Master/SalesmanController.cs:44`; `Repository/Master/SalesManRepository.cs:415,469` |
| SalesmanGroup | `isActive` (OK) | `isActive` default `true` | yes | `true` | both | none | `DTOs/SalesMan/SalesmanGroupRequest.cs:14`; `SalesmanGroupResponse.cs:13,31`; `Controllers/Master/SalesmanGroupController.cs:49`; `Repository/Master/SalesManGroupRepository.cs:153,187` |
| ItemCategories | `isActive` (OK) | `isActive` default `true` | yes | `true` | both | none | `DTOs/ItemCategory/ItemCategoryRequest.cs:17,27`; `Controllers/Master/ItemCategoriesController.cs:57`; `Repository/Master/ItemCategoryDB.cs:148` |
| Location | `isActive` (OK) | `isActive` default `true` | yes | `true` | both | none | `DTOs/LocationMasterRequest.cs:21,43,57`; `Controllers/Master/LocationController.cs:63`; `Repository/Master/GdwMasRepository.cs:272` |
| LocationGroup | `isActive` (OK) | `isActive` default `true` | yes | `true` | both | none | `DTOs/LocationGroupMasterRequest.cs:23,37,53`; `Controllers/Master/LocationGroupController.cs:82`; `Repository/Master/GdwGrpRepository.cs:71` |
| LedgerGroup | `isActive` (OK) | `isActive` default `true` | yes | `true` | both | none (the column is `GACTIVE`, aliased) | `DTOs/Respons-DTO/LedgerGroupDto.cs:28`; `DTOs/LedgerGroupRequest.cs:38`; `Controllers/Master/LedgerGroupController.cs:39`; `DB/LedgerGroupDB.cs:273` |
| PartyGroups | none | none | no | n/a | all non-deleted | **N/A: no active column.** `PARTYGROUPMAS` has only `ISDELETED` (`QuickBalance.sql:2838`). The audit's "add if table supports it" is answered: it does not. | `Repository/Master/PartyGroupRepository.cs:146-188`; `QuickBalance.sql:2838-2846` |
| Categories (legacy `/categories`, extra row) | raw `ACTIVE` key (DataTable) | `isActive` default `true` | no | `true` | all | superseded by ItemCategories; either retire it or add `?active=` and camelCase | `DB/CatgMasDB.cs:79-86,156,203,220`; `DTOs/CategoryMasterRequest.cs:8` |
| Brand | N/A: no active column (`BRANDMAS` has `DELFLG` only, `QuickBalance.sql:869`) | | | | | | |
| PriceCategory | N/A: no active column (`PRICECATMAS`, `QuickBalance.sql:3283`) | | | | | | |
| UnitConversion | N/A: no active column (`PRUNITCONV`, `QuickBalance.sql:3615`) | | | | | | |
| Currency / CurrencyConversion | N/A: no active column (`CURRENCYMAS`, `QuickBalance.sql:1556`) | | | | | | |

## Nested objects that use `active`

| object | where | rename? |
|---|---|---|
| product `aliases[].active` | `ProductAliasResponse.Active` (`DTOs/Product/ProductResponse.cs:128`); request `PrAliasRequests.Active` `bool?` (`DTOs/ProductRequest.cs:468`); `PrAliasRequest.Active` default `true` (`DTOs/PrAliasRequest.cs:14`); `CLS/ClsPrAlias.cs:23` | **Yes**, to `isActive`, in the same batch as Products so the product payload is consistent. Make the request non-null and default `true`. |
| supplier `clsSupplierResponse.active` | `CLS/clsSupplier.cs:157` | Yes, with Suppliers. |
| `AccountLedgerDtos.Active` (list) and `AccountLedgerDto.Active` (get-by-id) | `DTOs/Respons-DTO/AccountLedgerDto.cs:23,59` | Yes, both, with Accounts. |
| `ClsAccMasT.Active`, `ClsCompMas.Active`, `ClsPrMast.Active` | `CLS/*` | **No.** These are internal entity classes used by the legacy-port code, not returned as API JSON. Renaming them risks the SQL parameter mapping (`Active = clsAccMas.Active`). |

Recommended mechanism for the legacy `cls*`/Dapper classes: keep the C# property name and add `[JsonPropertyName("isActive")]` (response) or accept it on the request. That changes the wire name only, with no SQL alias changes.

## SQL / stored procedures that would need a change

No stored procedure is involved. Every list, get and write query above is inline SQL in a repository or DB class. I did not exhaustively check the 22 SPs in `QuickBalance.sql`, but none is referenced by these paths.

| SQL | change |
|---|---|
| `CountryRepository.cs:27,51,91` (count + list: `WHERE ACTIVE = 1`) | `AND (@Active IS NULL OR ACTIVE = @Active)`; add the `@Active` parameter. Keep the dropdown query at `:27` active-only. |
| `CountryRepository.cs:155,171,235` (get-by-id, duplicate check, update) | get-by-id: drop `AND ACTIVE = 1` so an inactive country can be fetched. Leave the duplicate check as is. |
| `CountryRepository.cs:265-271` (`DeleteCountry` = `SET ACTIVE = 0`) | **Decision needed** (see below). |
| `RegionRepository.cs:52,77` (count + list) | same `@Active` pattern |
| `RegionRepository.cs:123` (get-by-id) | drop `AND ACTIVE=1`, and add `ACTIVE AS IsActive` |
| `RegionRepository.cs:138,163,188` (name lookup, validation, dropdown) | leave as is |
| `ProductDB.cs:1158-1206, 1306-1326` (insert) | `request.Active ?? true` so ACTIVE is never NULL |
| `BomRepository.cs` get-by-id select (`:170-183`) | add `B.ACTIVE AS IsActive` |
| `BomRepository.cs` create/update | write `ACTIVE = @IsActive` (default 1) |
| `CatgMasDB.cs:156,203,220` (`/active`, search, dropdown) | leave as is (they are intentionally active-only) |
| `CompMasDB.cs:20` | out of scope (not a master list) |

## Suggested order of fixes (max 3 per batch, safest first)

1. **Batch 1: Units, Boms, Regions. FIXED.**
   - Units is a one-line default on the response class.
   - Boms: add `isActive` to get-by-id plus a request default. The column default is already `1` and the list is already fixed.
   - Regions: add the response property, `?active=`, and the request default. No existing consumer reads inactive regions.
2. **Batch 2: Accounts, Suppliers, Customers. Customers, Suppliers and Accounts FIXED (Accounts with rule 4 Batch G: response `isActive` is read-only `Active ?? true`, request `isActive` default true, list query stays `?active=`).** Wire-name rename via `[JsonPropertyName]`. These are legacy-port classes shared with SQL parameter code, so do not rename the C# properties. They also add `isActive` to the request. The frontend must switch `active` to `isActive` at the same time.
3. **Batch 3: Products (+ `aliases[].isActive`), Countries, Categories (legacy). Products FIXED and Countries response FIXED (rule 4 Batch H, C# DTOs only). Open, needs approval because it needs SQL or save-logic changes: Countries `?active=` filter and request `isActive`; Products rows with NULL `ACTIVE` are still skipped by `ACTIVE = 1` filters (`ProductDB.cs:573,602`) and by `?active=true`. Categories not touched.**
   - Products: response `isActive` is read-only `Active ?? true` (also `aliases[].isActive`); request `isActive` defaults to `true` (also aliases), and the old name `active` is still accepted for one release.
   - Countries: response `isActive` is read-only `Active ?? true` in list and get-by-id.
   - Products: biggest payload and the NULL-active insert fix.
   - Countries last because of the delete-vs-deactivate conflict.
   - Categories only if it is kept.
4. **No change needed:** ItemGroup, Taxes, Salesman, SalesmanGroup, ItemCategories, Location, LocationGroup, LedgerGroup (already match the target). **N/A:** PartyGroups, Brand, PriceCategory, UnitConversion, Currency, CurrencyConversion.

## Decisions

- **Countries:** delete = deactivate is accepted (option a), no schema change.
- **PartyGroups and Brand:** excluded from rule 3, no schema change.

## Decisions the owner needs to make

- **Countries delete:** `DeleteCountry` sets `ACTIVE = 0`. If the list starts returning inactive rows, deleted countries reappear. Options: (a) keep delete = deactivate and accept it, (b) add a `DELFLG`/`ISDELETED` column to MasterDB `COUNTRY` (a schema change).
- **PartyGroups and Brand:** add an `ACTIVE` column (schema change), or keep them excluded from rule 3. This plan assumes excluded.
- **Frontend coordination:** the rename `active` to `isActive` for Accounts, Customers, Suppliers, Products and Countries is a breaking wire change. Ship it with the frontend.
- **Stale audit note:** `docs/api-audit.md:307` says BOM delete is indistinguishable from deactivate. Actually `BomRepository.cs:522` also sets `ISDELETED = 1`, so the audit row can be corrected.
