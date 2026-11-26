# Master POST test bodies (for trainee testing)

Valid minimal `POST` body for every master controller, using the field names after rule 4 Batches D to H and rule 3 (`isActive`). Use it with `docs/test-lookup-queries.sql`: every `<... from Qnn>` placeholder is an id that query lists.

**Before you start**
- Log in first (`POST /api/v1/auth/login`) and send the JWT. All masters need it.
- Run the controllers in the order below. A controller only depends on the ones above it.
- Use a test prefix such as `ZT-` in every name, alias and code, so you can find and clean up your rows. Names are compared without regard to case, and some also ignore spaces at the ends.
- Most creates return only a message (`"... created successfully."`), not the new id. Find the record with the matching `GET` (`?search=<your unique name>`), then open it with `GET /{id}`.
- A business error (duplicate, bad id) returns HTTP 400 with `status:false` and a message. A model error also returns 400 with a list of fields.
- Do not edit or delete rows that were there before you started.

Placeholder format: `<categoryId from Q03>` means "run query Q03, pick an `id`".

## Order

| # | Controller | Route | Depends on |
|---|---|---|---|
| 1 | Units | `/api/v1/units` | none |
| 2 | Brands | `/api/v1/brands` | none |
| 3 | Item categories | `/api/v1/item-categories` | none |
| 4 | Party groups | `/api/v1/party-groups` | none |
| 5 | Price categories | `/api/v1/price-categories` | none |
| 6 | Salesman groups | `/api/v1/salesman-groups` | none |
| 7 | Location groups | `/api/v1/location-groups` | none |
| 8 | Currencies | `/api/v1/currencies` | none |
| 9 | Countries | `/api/v1/countries` | none |
| 10 | Ledger groups | `/api/v1/ledger-groups` | itself (sub-groups only) |
| 11 | Unit conversions | `/api/v1/unit-conversions` | units |
| 12 | Regions | `/api/v1/regions` | countries |
| 13 | Currency conversions | `/api/v1/currency-conversions` | currencies |
| 14 | Accounts | `/api/v1/accounts` | ledger groups, currencies |
| 15 | Taxes | `/api/v1/taxes` | accounts |
| 16 | Item groups | `/api/v1/item-group` | accounts, item groups |
| 17 | Locations | `/api/v1/locations` | location groups, accounts |
| 18 | Salesmen | `/api/v1/salesman` | salesman groups, accounts (optional) |
| 19 | Suppliers | `/api/v1/suppliers` | ledger groups, countries, regions, party groups, currencies |
| 20 | Customers | `/api/v1/customers` | ledger groups, countries, regions, party groups, salesmen, currencies |
| 21 | Products | `/api/v1/products/with-image` | item groups, categories, brands, units, taxes, price categories, accounts, suppliers, currencies |
| 22 | BOMs | `/api/v1/boms` | products, units, accounts |

---

## 1. Units: `POST /api/v1/units`

```json
{ "unitName": "ZT-Unit1", "secondName": "ZT-U1", "formalName": "ZT Unit", "isActive": true }
```
- **Required:** `unitName` (at most 25 characters).
- **References:** none.
- **Must not duplicate:** `unitName` among active units.
- **Check:** `GET /api/v1/units?search=ZT-Unit1`, then `GET /api/v1/units/{id}`.

## 2. Brands: `POST /api/v1/brands`

```json
{ "brandName": "ZT-Brand1", "secondName": "ZT-B1-2nd", "alias": "ZTB1" }
```
- **Required:** `brandName` (2 to 50 characters). `alias` at most 25.
- **References:** none.
- **Must not duplicate:** `brandName`, `secondName` and `alias`. Each is checked on its own against non-deleted brands. Omit `secondName` and `alias` if you have no unique value (not verified how an empty string is treated).
- **Check:** `GET /api/v1/brands?search=ZT-Brand1`, then `GET /api/v1/brands/{id}`.

## 3. Item categories: `POST /api/v1/item-categories`

```json
{ "categoryName": "ZT-Category1", "alias": "ZTC1", "secondName": "ZT-Cat-2nd", "isActive": true }
```
- **Required:** `categoryName` (at most 100).
- **References:** none.
- **Must not duplicate:** `categoryName` (non-deleted). `alias` is not checked.
- **Check:** `GET /api/v1/item-categories?search=ZT-Category1`, then `GET /api/v1/item-categories/{id}`.

## 4. Party groups: `POST /api/v1/party-groups`

```json
{ "groupName": "ZT-CustGrp1", "groupShortName": "ZTCG1", "groupType": "Customer" }
```
- **Required:** `groupName` (at most 20), `groupType` (`Customer` or `Supplier`, exact case). Add a second group with `"groupType": "Supplier"` for the supplier tests.
- **References:** none.
- **Must not duplicate:** `groupName` within the same `groupType` (non-deleted).
- **Check:** `GET /api/v1/party-groups?groupType=Customer`, then `GET /api/v1/party-groups/{id}` (returns `groupName`, `groupShortName`, `groupType`).

## 5. Price categories: `POST /api/v1/price-categories`

```json
{
  "categoryName": "ZT-Price1",
  "levels": [
    { "discountStructure": "10", "levelFrom": 1, "levelTo": 100,
      "discountPercent": 10, "effectiveValue": 0, "appliedPrice": 0 }
  ]
}
```
- **Required:** `categoryName` (2 to 25 characters), at least one level. In a level: `discountStructure` (1 to 25 characters). Number limits: `levelFrom`, `levelTo`, `effectiveValue`, `appliedPrice` 0 to 100000, `discountPercent` 0 to 100.
- **References:** none. A level `id` may be sent but is ignored.
- **Must not duplicate:** `categoryName` (non-deleted).
- **Check:** `GET /api/v1/price-categories?search=ZT-Price1`, then `GET /api/v1/price-categories/{id}` (levels are only in get-by-id).

## 6. Salesman groups: `POST /api/v1/salesman-groups`

```json
{ "groupName": "ZT-SMGroup1", "secondName": "ZT-SMG1", "printName": "ZT SM Group 1", "isActive": true }
```
- **Required:** `groupName`.
- **References:** none.
- **Must not duplicate:** `groupName`. The check does **not** ignore deleted or inactive rows, so a name used by a deleted group is also refused.
- **Check:** `GET /api/v1/salesman-groups?search=ZT-SMGroup1`, then `GET /api/v1/salesman-groups/{id}`.

## 7. Location groups: `POST /api/v1/location-groups`

```json
{ "groupName": "ZT-LocGrp1", "secondName": "ZT-LG1", "isActive": true }
```
- **Required:** `groupName` (2 to 20 characters). `secondName` at most 20.
- **References:** none.
- **Must not duplicate:** `groupName` among active groups.
- **Check:** `GET /api/v1/location-groups?search=ZT-LocGrp1`, then `GET /api/v1/location-groups/{id}`.

## 8. Currencies: `POST /api/v1/currencies`

```json
{ "symbol": "ZTC", "currString": "ZT Currency", "subString": "ZT Cent", "convMode": 1,
  "baseFlg": false, "secSymbol": "ZT2", "secCurrString": "ZT Currency 2nd", "secSubString": "ZT Cent 2nd" }
```
- **Required:** none by validation (each text field is at most 50 characters). Always send `symbol`.
- **References:** none.
- **Must not duplicate:** `symbol` ("Currency symbol already exists").
- **Keep `baseFlg` false.** A base currency already exists and the conversions use it.
- **Check:** `GET /api/v1/currencies?search=ZTC`, then `GET /api/v1/currencies/{id}`.

## 9. Countries: `POST /api/v1/countries`

```json
{ "countryName": "ZT-Country1", "iso": "ZZ", "iso3": "ZZZ", "phCode": 999, "curSymbol": "ZTC", "curName": "ZT Currency" }
```
- **Required:** `countryName` (2 to 100 characters).
- **References:** none.
- **Must not duplicate:** `countryName` among active countries (case-insensitive).
- **Warning:** countries and regions live in **MasterDB** and are shared by every company. Ask before creating test rows on a shared server. The request has no `isActive` yet (known open item), and delete only deactivates.
- **Check:** `GET /api/v1/countries?search=ZT-Country1`, then `GET /api/v1/countries/{id}`.

## 10. Ledger groups: `POST /api/v1/ledger-groups`

Main group:
```json
{ "groupName": "ZT-LedgerMain1", "secondName": "ZT-LM1", "mainGroup": true,
  "finalAccount": "B", "crDr": "DR", "subLedgerFlag": false, "isActive": true }
```
Sub-group (inherits `finalAccount` and `crDr` from its parent):
```json
{ "groupName": "ZT-LedgerSub1", "mainGroup": false, "mainGroupId": "<mainGroupId from Q11>", "isActive": true }
```
- **Required:** `groupName`. Main group: `finalAccount` (`P`, `T`, `B` or `M`) and `crDr` (`CR` or `DR`). Sub-group: `mainGroupId`.
- **References:** `mainGroupId` from Q11 (must be active).
- **Must not duplicate:** `groupName` (active, not deleted). `groupCode` is generated (any value sent is ignored on create). `sequenceNo` is generated unless sent, and a sent value must not be in use.
- **Check:** `GET /api/v1/ledger-groups?search=ZT-LedgerMain1`, then `GET /api/v1/ledger-groups/{id}`.

## 11. Unit conversions: `POST /api/v1/unit-conversions`

```json
{ "mainUnit": "<unitId from Q01>", "subUnit": "<another unitId from Q01>", "convFact": 12 }
```
- **Required:** `mainUnit`, `subUnit`, `convFact` (above 0).
- **References:** `mainUnit`, `subUnit` from Q01 (both must exist).
- **Must not duplicate:** no duplicate check was found in the service. Use two different units and do not repeat a pair that is already listed (not verified whether the database refuses it).
- **Check:** `GET /api/v1/unit-conversions?search=...`, then `GET /api/v1/unit-conversions/{id}`.

## 12. Regions: `POST /api/v1/regions`

```json
{ "countryId": "<countryId from Q23>", "regionName": "ZT-Region1", "regionSecName": "ZT-R1", "isActive": true }
```
- **Required:** `countryId`, `regionName` (at most 50).
- **References:** `countryId` from Q23 (MasterDB, must be active).
- **Must not duplicate:** `regionName` within the same country.
- **Warning:** MasterDB, shared by all companies (see Countries).
- **Check:** `GET /api/v1/regions?countryId=<countryId>`, then `GET /api/v1/regions/{id}`. The key is `id` (it used to be `regionId`).

## 13. Currency conversions: `POST /api/v1/currency-conversions`

```json
{ "conversionDate": "2026-11-25",
  "details": [ { "currencyId": "<currencyId from Q22>", "standardRate": 3.75, "sellRate": 3.76, "buyRate": 3.74 } ] }
```
- **Required:** `conversionDate`, at least one detail with the three rates above 0.
- **References:** `details[].currencyId` from Q22 (non-base). Never use the base currency from Q21. A base currency must be configured.
- **Must not duplicate:** `conversionDate` (one set per date), and no repeated `currencyId` inside `details`.
- **Check:** `GET /api/v1/currency-conversions`, find your row (note its `id`), then `GET /api/v1/currency-conversions/{id}`. The response is `{ conversionDate, details[] }`.

## 14. Accounts: `POST /api/v1/accounts`

```json
{ "accountName": "ZT-Account1", "alias": "ZTA1", "secondName": "ZT Account 1",
  "groupId": "<groupId from Q11>", "openingBalance": 0, "openingBalanceType": "CR",
  "vatFlag": false, "currencyFlag": false, "isActive": true }
```
- **Required:** `accountName` (2 to 100 characters), `groupId`, `openingBalanceType` (`CR` or `DR`, any case; defaults to `CR`).
- **Conditional:** `vatFlag: true` needs `vatPercentage` above 0. `currencyFlag: true` needs `currencyCode` (Q20) and `currencyConversionRate` above 0.
- **References:** `groupId` from Q11. `currencyCode` from Q20 (only when `currencyFlag` is true).
- **Must not duplicate:** `accountName` (non-deleted). `accountCode` is generated as `<group code>-NN` and cannot be sent.
- **Response:** `data.accountId` holds the new id.
- **Check:** `GET /api/v1/accounts/{id}`. `openingBalance` is shown as an absolute number with `openingBalanceType` (`DR` or `CR`).

## 15. Taxes: `POST /api/v1/taxes`

```json
{ "taxName": "ZT-Tax5", "taxPercentage": 5, "isActive": true,
  "purchaseAccountId": "<id from Q14>", "purchaseReturnAccountId": "<id from Q14>",
  "salesAccountId": "<id from Q14>", "salesReturnAccountId": "<id from Q14>",
  "vatAccountId": "<id from Q14>", "taxAccountId": "<id from Q14>",
  "secondName": "ZT Tax 5", "taxType": "T" }
```
- **Required:** `taxName`, `taxPercentage` (0 to 100), and all six account ids (each 1 or more).
- **References:** the six account ids, all from Q14. They may be the same account for a test.
- **Must not duplicate:** `taxName` among active taxes.
- **`taxType`:** `T` (default), `E` or `UR`. Use `T` so the tax shows in the product lookup.
- **Check:** `GET /api/v1/taxes?search=ZT-Tax5`, then `GET /api/v1/taxes/{id}`.

## 16. Item groups: `POST /api/v1/item-group`

Primary group:
```json
{ "groupName": "ZT-ItemGroup1", "groupCode": 9001, "isPrimaryGroup": true,
  "tagWithAccount": false, "isActive": true, "secName": "ZT IG 1" }
```
Child group:
```json
{ "groupName": "ZT-ItemGroup2", "groupCode": 9002, "isPrimaryGroup": false,
  "primaryGroupId": "<id from Q05>", "tagWithAccount": false, "isActive": true }
```
- **Required:** `groupName` (2 to 50 characters), `groupCode` (1 or more). Child group: `primaryGroupId`. With `tagWithAccount: true`: `stockAccountId`, `purchaseAccountId` and `salesAccountId` (all above 0).
- **References:** `primaryGroupId` from Q05 (must be an active primary group). The three accounts from Q14.
- **Must not duplicate:** `groupName` and `groupCode` (both among non-deleted groups). Pick a code that is not in Q04's `code` column.
- **Check:** `GET /api/v1/item-group?search=ZT-ItemGroup1`, then `GET /api/v1/item-group/{id}`.

## 17. Locations: `POST /api/v1/locations`

```json
{ "locationName": "ZT-Loc1", "groupId": "<groupId from Q19>", "isActive": true,
  "stockAccountId": "<id from Q14>", "stockPLFlag": false, "secondName": "ZT L1" }
```
- **Required:** `locationName` (2 to 25 characters), `groupId`, `stockAccountId`. `secondName` at most 25.
- **References:** `groupId` from Q19, `stockAccountId` from Q14.
- **Must not duplicate:** `locationName`. The check does **not** ignore deleted or inactive rows.
- **Check:** `GET /api/v1/locations?search=ZT-Loc1`, then `GET /api/v1/locations/{id}`.

## 18. Salesmen: `POST /api/v1/salesman`

```json
{ "salesmanName": "ZT-Salesman1", "secondName": "ZT SM 1", "printName": "ZT Salesman 1",
  "address": "Test street", "phone": "+971500000000", "email": "zt.salesman1@example.com",
  "isActive": true, "salesmanGroupId": "<id from Q15>", "connectWithAccount": false, "accountId": 0 }
```
- **Required:** `salesmanName` (at most 100), `salesmanGroupId` (1 or more). `email` must be a valid address if given. `phone` may contain only digits and `+`.
- **References:** `salesmanGroupId` from Q15. `accountId` from Q14 only when `connectWithAccount` is true (otherwise leave it 0).
- **Must not duplicate:** `salesmanName`. The check does **not** ignore deleted or inactive rows. `employeeCode` is generated if you leave it out.
- **Check:** `GET /api/v1/salesman?search=ZT-Salesman1`, then `GET /api/v1/salesman/{id}`.

## 19. Suppliers: `POST /api/v1/suppliers`

```json
{ "supName": "ZT-Supplier1", "alias": "ZTS1", "address": "Test street", "phone": "+971500000001",
  "email": "zt.supplier1@example.com", "isActive": true,
  "countryCode": "<id from Q23>", "stateCode": "<id from Q24>",
  "partyGrpId": 0, "curCode": 0, "curConvRate": 1,
  "openBal": 0, "crDr": "CR", "pendingBills": [] }
```
- **Required:** `supName` (at most 50), `countryCode` (above 0), `stateCode` (above 0), `crDr` (`CR` or `DR`, any case; it is returned uppercase).
- **References:** `countryCode` from Q23 and `stateCode` from Q24 (the region must belong to that country; both in MasterDB). `partyGrpId` from Q18 (0 = none). `curCode` from Q20 (0 = none). `ledgerGroupId` is optional (Q13), but the API always saves the supplier under ledger group 27.
- **Must not duplicate:** `supName` among active, non-deleted suppliers.
- **Format:** `vatRegNo`, if sent, is exactly 15 digits. `phone` must look like a phone number.
- **Response:** only `"Supplier and account created successfully."` (a linked account is created too).
- **Check:** `GET /api/v1/suppliers?search=ZT-Supplier1`, then `GET /api/v1/suppliers/{id}` (look for `isActive`, `crDr: "CR"`). Also check the new row in `GET /api/v1/accounts`.

## 20. Customers: `POST /api/v1/customers`

```json
{ "cusName": "ZT-Customer1", "alias": "ZTCU1", "address": "Test street", "phone": "+971500000002",
  "email": "zt.customer1@example.com", "isActive": true,
  "ledgerGroupId": "<id from Q12>",
  "countryCode": "<id from Q23>", "stateCode": "<id from Q24>",
  "partyGrpId": 0, "curCode": 0, "curConvRate": 1,
  "openBal": 0, "crDr": "DR", "pendingBills": [] }
```
- **Required:** `cusName` (at most 100), `countryCode` (above 0), `stateCode` (above 0), `crDr`. Send `ledgerGroupId` too: it must be 1 or more, and the API checks it is active and not deleted.
- **References:** `ledgerGroupId` from Q12. `countryCode` from Q23 and `stateCode` from Q24 (region must belong to the country; MasterDB). `partyGrpId` from Q17 (0 = none). `curCode` from Q20 (0 = none).
- **`salesManId`:** leave it out for the minimal test. The lookup `GET /customers/lookups/salesman` lists salesmen (**Q16**, table `SMMAST`), but the create check reads the **salesman group** table (`SMGRP`, **Q15**) for the same value. A salesman id that is not also a valid salesman group id is likely refused with "Selected salesman does not exist or is inactive." This looks like a bug. Report what you see. Do not work around it in the data.
- **Must not duplicate:** `cusName` among active, non-deleted customers (returns HTTP 409).
- **Format:** `vatRegNo`, if sent, is exactly 15 digits.
- **Response:** only `"Customer and account created successfully."`.
- **Check:** `GET /api/v1/customers?search=ZT-Customer1`, then `GET /api/v1/customers/{id}` (message "Customer retrieved successfully.", `isActive`, `crDr` uppercase).

## 21. Products: `POST /api/v1/products/with-image`

Product create is **multipart/form-data** (the JSON-only create is disabled).
- Form field `product` (text): the JSON below.
- Form field `imageFile` (file, optional, at most 5 MB).

```json
{ "prName": "ZT-Product1", "alias": "ZTP1", "printName": "ZT Product 1", "description": "Test product",
  "groupId": "<id from Q04>", "categoryId": "<id from Q03>", "brandId": "<id from Q02>",
  "unit": "<id from Q01>", "altUnit": "<a different id from Q01>", "unitConv": 12,
  "taxCatId": "<id from Q06>", "baseSalePrice": 10, "basePurPrice": 7,
  "saleAccId": "<id from Q14>", "purAcctId": "<id from Q14>",
  "isActive": true, "batchFlg": false, "slNoTrack": false, "aliases": [] }
```
- **Required:** `prName` (at most 50, no `&` or `@`). The service also asks for `groupId`, `unit` and `taxCatId`: always send them.
- **References:** `groupId` Q04, `categoryId` Q03, `brandId` Q02, `unit` and `altUnit` Q01, `taxCatId` Q06, `priceCatId` Q07, `saleAccId` and `purAcctId` Q14, `supplierId` Q08, `gdwnId` Q09, `currencyId` Q20, `aliases[].unitId` and `invUnitCode` Q01. Leave out any you do not need.
- **Rules:** `altUnit` must differ from `unit`. `batchFlg` and `slNoTrack` cannot both be true. If `opnStkQty` is above 0, `opnStkValue` must also be above 0.
- **Must not duplicate:** `prName` (non-deleted), `alias`, `barCode` (among active products), and every `aliases[].alName` inside the same request. `alias` must not equal one of the `aliases[].alName` values either. An alias row needs `alName`.
- **New names:** use `description`, `categoryId`, `bmpl/bmpq/bmpw`, `slNoTrack`, `bmEqn`, `isActive`. The old names (`desc`, `catgId`, `_BMPL`, `_SLNOTRACK`, `active`, ...) are still accepted for one release, so test both once and confirm the new name wins when both are sent.
- **Check:** `GET /api/v1/products?search=ZT-Product1`, then `GET /api/v1/products/{id}`. Compare `notes`, `saleAccId`, `purAcctId`, `opnStkQty`, `isActive`.
- **Known open items:** `brandId` is a number in the request but a string in the response. `aliases[].unitId` may show 0 in the list while get-by-id shows the real unit. Report both as expected.

## 22. BOMs: `POST /api/v1/boms`

```json
{ "bomName": "ZT-BOM1", "alias": "ZTBOM1",
  "prCode": "<finished product id from Q10>", "qty": 1, "unit": "<id from Q01>", "expences": 0,
  "mcGenFlg": false, "mcConsFlg": false, "secondName": "ZT BOM 1", "isActive": true,
  "items": [ { "productId": "<component product id from Q10>", "quantity": 2, "unitId": "<id from Q01>", "type": "C" } ],
  "additionalInfo": [] }
```
- **Required:** `bomName` (2 to 100 characters), `prCode`, `qty` (above 0), `unit`, at least one item. Item: `productId`, `quantity` (above 0), `unitId`, `type` (`C` or `G`).
- **Conditional:** `mcGenFlg: true` needs `mcGen` (account from Q14). `mcConsFlg: true` needs `mcCons` (account from Q14). Each `additionalInfo[]` entry needs `accountId` (Q14) and `amount`. The flags are true/false booleans, not `"Y"`/`"N"`.
- **References:** `prCode` and `items[].productId` Q10, `unit` and `items[].unitId` Q01, accounts Q14.
- **Must not duplicate:** `bomName` and `alias` among non-deleted BOMs.
- **Check:** `GET /api/v1/boms?search=ZT-BOM1`, then `GET /api/v1/boms/{id}`. The response is flat. Each `items[].id` is the BOM id, not a line id, so do not treat it as a row key.

---

## Quick reference: what must be unique

| Controller | Unique fields | Ignores deleted rows? |
|---|---|---|
| Units | `unitName` | active only |
| Brands | `brandName`, `secondName`, `alias` | yes |
| Item categories | `categoryName` | yes |
| Party groups | `groupName` per `groupType` | yes |
| Price categories | `categoryName` | yes |
| Salesman groups | `groupName` | **no** |
| Location groups | `groupName` | active only |
| Currencies | `symbol` | not verified |
| Countries | `countryName` | active only |
| Ledger groups | `groupName`, `groupCode`, `sequenceNo` | yes |
| Unit conversions | (none found) | n/a |
| Regions | `regionName` within a country | active only |
| Currency conversions | `conversionDate`, `currencyId` within details | n/a |
| Accounts | `accountName`, generated `accountCode` | yes |
| Taxes | `taxName` | active only |
| Item groups | `groupName`, `groupCode` | yes |
| Locations | `locationName` | **no** |
| Salesmen | `salesmanName` | **no** |
| Suppliers | `supName` | yes (active only) |
| Customers | `cusName` | yes (active only) |
| Products | `prName`, `alias`, `barCode` (active only), alias names | yes |
| BOMs | `bomName`, `alias` | yes |

"Ignores deleted rows: no" means a deleted or inactive row with the same name still blocks the new one.
