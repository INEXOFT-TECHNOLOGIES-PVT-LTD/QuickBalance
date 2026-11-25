# Master API changes for the frontend

Date: 25 November 2026 · Scope: master endpoints under `/api/v1` · Source: `docs/api-audit.md`, `docs/rule3-plan.md`, `docs/rule4-plan.md`, checked against the current code.

**Release note:** the changes marked **breaking** rename keys or change shapes. They are not released to the frontend yet. Ship them together with the matching frontend change. There are no dual names, except the Products request safety net described below.

Rules applied: 1) list envelope, 2) flat camelCase list rows, 3) `isActive` and `?active=`, 4) same field name and type in request and response.

---

## 1. Change at a glance

| Area | Change | Breaking |
|---|---|---|
| `active` → `isActive` | Accounts, Customers, Suppliers, Products (and `aliases[]`), Countries (response) | yes |
| Accounts | Many renamed keys, new `openingBalance` and `openingBalanceType`, `acCode` removed | yes |
| Customers, Suppliers | `salesMan` → `salesManId`, `crDr` uppercase, nullability | yes (small) |
| Products | Request and response renames, underscore keys removed | yes |
| BOMs | Flat get-by-id, `bool` flags, item field names | yes |
| Currency conversions | Get-by-id has `details[]`, `alternateCurrencyId` → `currencyId` | yes |
| Regions | `regionId` → `id`, route param `{id}`, `isActive` added | yes |
| Price categories | `levels[].categoryName` removed from the response | yes (small) |
| Lookups, envelopes | `einvoice-id-types` objects, array envelopes, `message` added | mostly no |
| Nullability only | Taxes, Currency, Salesman, SalesmanGroup, LedgerGroup | no |

---

## 2. Rule 3: `isActive`

Every master that can be deactivated now uses `isActive` (bool) in list rows, get-by-id and the request. List filter: `?active=true|false`. Leaving `active` out returns both active and inactive records. The query parameter is still named `active` (only the JSON field is renamed).

| Master | Old | New | Notes |
|---|---|---|---|
| Accounts | `active` (`bool?`) | `isActive` | Response value is read-only and never null (`null` is returned as `true`). Request `isActive` defaults to `true`. |
| Customers | `active` | `isActive` | Request default `true`. |
| Suppliers | `active` | `isActive` | Request default `true`. Applies to create and update. |
| Products | `active` (`bool?`) | `isActive` | Response never null. Request default `true`. Same for `aliases[].isActive`. |
| Countries | `active` (`bool?`) | `isActive` | Response only, list and get-by-id. Request has no `isActive` yet (see open items). |
| Regions | not returned | `isActive` | New in the response. `?active=` added. Request default is now `true` (it was `false`). |
| BOMs | list had `isActive`, get-by-id had none | `isActive` in list and get-by-id | Request accepts `isActive` (default `true`) and it is saved. |
| Units | `isActive` | `isActive` | Response class default changed to `true` (no visible effect). |

Not applicable (the table has no active column): Brands, PartyGroups, PriceCategory, UnitConversion, Currency, CurrencyConversion. Already correct: ItemGroup, Taxes, Salesman, SalesmanGroup, ItemCategories, Location, LocationGroup, LedgerGroup.

---

## 3. Per endpoint

### Accounts (`/accounts`) — rule 4 Batch G, breaking
Applies to list, `GET /accounts/{id}` and `GET /accounts/group/{groupId}`.

| Old response key | New key | Type |
|---|---|---|
| `accName` | `accountName` | string |
| `secName` | `secondName` | string |
| `ledgerGroupId` | `groupId` | int |
| `vatPerc` | `vatPercentage` | decimal |
| `taxRetFlag` | `taxReturnFlag` | bool |
| `bankAccName` | `bankAccountName` | string |
| `bankAccNo` | `bankAccountNumber` | string |
| `ibanNo` | `ibanNumber` | string |
| `curFlg` | `currencyFlag` | bool, never null (null is returned as `false`) |
| `curConvRate` | `currencyConversionRate` | decimal |
| `curCode` | `currencyCode` | int |
| `active` | `isActive` | bool, never null |
| `acCode` | removed | use `accountCode` (read-only) |
| `openBal` | `openingBalance` + `openingBalanceType` | see below |

**Opening balance.** The database stores one signed number. The response now returns the absolute value in `openingBalance` and the side in `openingBalanceType`: `"DR"` if the stored value is negative, otherwise `"CR"`. Example: a debit account that returned `openBal: -500` now returns `openingBalance: 500, openingBalanceType: "DR"`. Both fields are read-only in the response. The request already used the same names. The request `openingBalanceType` accepts `CR`/`DR` in any case.

**Envelope.** `GET /accounts` now returns `{ status, message, data[], pagination:{page,pageSize,totalRecords,totalPages} }`. Before, paging was at the top level (`page`, `pageSize`, `totalRecords`, `totalPages`) with no `status` or `message`. Read the next page from `pagination`.

Unchanged: `GET /accounts/{id}/opening`, `list-party`, `DELETE`.

### Customers (`/customers`) — Batch F + rule 3, breaking (small)
- `active` → `isActive` (response and request).
- Request `salesMan` → `salesManId` (`int?`; the response already used `salesManId`).
- `crDr` is always uppercase `CR` or `DR`, see behaviour notes.
- Types now equal in request and response: `ledgerGroupId` `int?`, `stateCode` `int?`, `pendingBills[].invoiceDate` and `dueDate` `DateTime?` (null dates are still rejected on save).
- `GET /customers` now includes `message` ("Customers retrieved successfully."). Envelope: `{status, message, data[], pagination}`.
- `GET /customers/{id}` now returns the standard `ApiResponse` envelope with `message: "Customer retrieved successfully."`. `data` is unchanged. A missing customer still returns `status: false` with a message.
- `lookups/ledger-groups` and `lookups/salesman`: `data` is always an array of `{id, name}` (no `pagination`, they are not paged).
- Response-only fields (stay read-only, ignored on save): `accountId`, `accountCode`, `disc`, `priceLevel`, `prTmplateId`, `pntSysId`, `itemGroupId`, `grpItem`, `logId`.

### Suppliers (`/suppliers`) — Batch F + rule 3, breaking (small)
- `active` → `isActive` (response and request, create and update).
- `crDr` uppercase (the old default was `Cr`).
- `lookups/ledger-groups`: array envelope.
- Response-only fields stay read-only (`accountId`, `disc`, `priceLevel`, `itemGroupId`, `grpItem`, `logId`).

### Products (`/products`) — Batch H + rule 3, breaking
**Response.**

| Old key | New key |
|---|---|
| `active` | `isActive` (never null) |
| `prNotes` | `notes` |
| `saleAcId` | `saleAccId` |
| `purAcId` | `purAcctId` |
| `opnStock` | `opnStkQty` |
| `opnStkval` | `opnStkValue` |
| `aliases[].active` | `aliases[].isActive` |
| `priceCategories[].catName` | `categoryName` |
| `priceCategories[].discStruct` | `discountStructure` |
| `priceCategories[].levFrom` / `levTo` | `levelFrom` / `levelTo` |
| `priceCategories[].discPerc` | `discountPercent` |
| `priceCategories[].effValue` | `effectiveValue` |
| `priceCategories[].appPrice` | `appliedPrice` |
| not returned | `aprvlst` (new) |

**Request** (`PUT /products/{id}`, `POST /products/with-image`, `PUT /products/{id}/with-image`).

| Old key | New key |
|---|---|
| `desc` | `description` |
| `catgId` | `categoryId` |
| `_BMPL`, `_BMPQ`, `_BMPW` | `bmpl`, `bmpq`, `bmpw` |
| `_SLNOTRACK` | `slNoTrack` |
| `_BMEQN` | `bmEqn` |
| `active` | `isActive` (default `true`; also on `aliases[]`) |

Kept as they were: request `saleAccId` and `purAcctId` (the response was renamed to match them). `priceCategories[]` is read-only. `image` is not part of the round trip (use the image endpoints). `gdwnId` and `brCd` are request-only. The list keeps full rows (nested `aliases[]`, `priceCategories[]`) on purpose.

**Safety net (one release).** The old request names above are still accepted: `desc`, `catgId`, `_BMPL`, `_BMPQ`, `_BMPW`, `_SLNOTRACK`, `_BMEQN`, `active`. If a body has both the old and the new name, the new name wins. This works on all three write endpoints. The old names will be removed in the next release, so switch to the new names now. Responses only use the new names.

### BOMs (`/boms`) — Batch E + rule 3, breaking
- `GET /boms` rows are flat: `{id, bomName, alias, productName, qty, unitName, isActive, ...}`. No `bilofm_details`, no `items[]` or `additionalInfo[]` in rows.
- `GET /boms/{id}` is flat: the header fields are at the top level, plus `items[]` and `additionalInfo[]`. `bilofm_details` is gone. It includes `isActive`.
- `mcGenFlg` and `mcConsFlg` are `bool` in the request and the response. The request no longer accepts `"Y"`/`"N"`.
- `items[]` in the response: `prCode` → `productId`, `qty` → `quantity`, `unit` → `unitId` (same as the request). `type` is unchanged. Extra response-only item fields (`price`, `purPriceType`, `altPurPrice`, `mainUnit`, `altUnit`, `prName`, `baseSalePrice`, `altSalePrice`) are ignored on save.
- `additionalInfo[].accountId` and `amount` are nullable in both directions.
- Request accepts `isActive` (default `true`).

### Currency conversions (`/currency-conversions`) — Batch E, breaking
- `GET /currency-conversions/{id}` returns `{ conversionDate, details: [ { id, currencyId, symbol, standardRate, sellRate, buyRate } ] }`. This mirrors the request `{ conversionDate, details[] }`. Before it returned a flat list of rows.
- `alternateCurrencyId` → `currencyId`, in get-by-id and in list rows.
- `standardRate`, `sellRate`, `buyRate` are non-null decimals in the request and the response (a missing database value is returned as `0`).
- `baseCurrencyId` stays on list rows only.

### Regions (`/regions`) — Batch D + rule 3, breaking
- `regionId` → `id` in the list, in `GET /regions/{id}` and in the dropdown query.
- Route parameter is `{id}` (`PUT /regions/{id}`, `DELETE /regions/{id}`).
- `isActive` is now in the response. `?active=true|false` was added to the list.
- Request `isActive` default is `true` (it was `false`).
- `countryId` is a non-null int in the response.
- `GET /regions/{id}` now returns inactive regions too.

### Price categories (`/price-categories`) — Batch D, breaking (small)
- `levels[].categoryName` was removed from the response (it repeated the parent `categoryName`).
- Request `levels[]` accepts an optional `id`. It is ignored: saving replaces all levels.
- List rows are flat: `{id, categoryName, levelCount}`. `levels[]` is only in get-by-id.

### Party groups (`/party-groups`) — Batch C, non-breaking
- New `GET /party-groups/{id}` returns `{id, groupName, groupShortName, groupType}`.
- List rows also have `groupName`, `groupShortName`, `groupType`. `name` is kept for dropdowns.
- `groupType` is `Customer` or `Supplier` in both directions.

### Lookups and small shape changes
- `GET /einvoice-id-types`: `data` is now `[{ id: "CRN", name: "CRN" }, ...]` (it was an array of strings). **Breaking** for any code that reads the strings.
- `GET /vat-types`: array envelope, rows unchanged (`id` is still the string name).
- `GET /item-group/primary-groups`, `GET /ledger-groups/customer-supplier`, `GET /ledger-groups/lookups/final-accounts`: documented and returned as an array envelope, `data` is always an array, rows unchanged.
- List rows no longer contain internal fields: item-group (`logCode`, `updateTime`), salesman-groups (`logCode`, `updateTime`), locations (`logCd`), location-groups (`logCd`). `GET /salesman-groups/{id}` also drops them.
- `GET /ledger-groups` and `GET /ledger-groups/{id}` now use the standard envelope (JSON unchanged).
- Swagger: list endpoints now document `data` as an array (units, taxes, brands, item-group, countries, currency conversions, party groups, and others). Runtime was already correct.

### Nullability only (non-breaking)
Request and response now agree on null versus a default value.
- Taxes: account ids and `secondName`, `taxType`.
- Currency: `baseFlg`.
- Salesman: `employeeCode` is `int?` in both; optional strings are `string?` in both.
- SalesmanGroup: `secondName`, `printName` are `string?` in both.
- LedgerGroup: `groupCode`, `sequenceNo` are `int?`; `secondName`, `finalAccount`, `crDr` are `string?` (null or 0 means generate or keep the existing value).
- Products: `aliases[]` numeric fields.

---

## 4. Behaviour notes

1. **Omitted `isActive` saves as active.** Create and update requests that leave out `isActive` save `ACTIVE = 1`. Products and product aliases used to save NULL (or `false` on one request class). Regions used to save `false`.
2. **`crDr` is always uppercase.** Customers and Suppliers return and store `CR` or `DR` (old values such as `Cr`, `Dr` are returned uppercase, with no database update). The request accepts any casing. The old defaults were `CR` (customer request), `DR` (customer response) and `Cr` (supplier); the default is now `DR` for customers and `CR` for suppliers. Pending-bill `drCr` is not part of this rule and keeps its stored `Dr`/`Cr` values.
3. **`PUT` without `isActive` reactivates BOMs and Regions.** `isActive` defaults to `true` and is written on every update. An edit form that does not send `isActive` will turn an inactive BOM or region back on. Always send the loaded value. (Products and aliases behave the same way, because an omitted `isActive` saves as active.)
4. **`items[].id` in a BOM is the BOM id.** In `GET /boms/{id}` each `items[].id` is the BOM code (`BOMCODE`), not a line id, so every item in the same BOM has the same `id`. Do not use it as a row key.
5. **Soft delete.** `DELETE` on a BOM sets `ACTIVE = 0` and `ISDELETED = 1`. Deleting a country sets `ACTIVE = 0` (delete and deactivate are the same state).
6. **Accounts opening balance.** Use `openingBalance` (positive) with `openingBalanceType`. Do not compute the sign yourself from the old `openBal`.
7. **Customer and supplier create/update** ignore the response-only fields in the list above, so a loaded record can be sent back unchanged.

---

## 5. Known open items

These are not changed. They need approval for a SQL or save-logic change, or a decision. Details are in `docs/api-audit.md` ("OPEN items").

| Item | What the frontend sees today |
|---|---|
| Products `brandId` | Request sends a number (`int?`), the response returns a string (`string?`, because the column is `nvarchar(255)`). Treat it as a string on read and convert to a number on write until this is decided. |
| Countries `?active=` | The list returns active countries only. `?active=` is not supported yet, and the country request has no `isActive`. `GET /countries/{id}` does not return an inactive country. `isActive` in the response is always `true` for the rows you can see. |
| Products with NULL `ACTIVE` | These show `isActive: true`, but `GET /products/{id}` and the alias lookups skip them. `?active=true` does not return them, because the list filter treats NULL as inactive. |
| Product alias `unitId` (list vs get-by-id) | The list alias query selects `UNITCODE` without an `unitId` alias, while get-by-id aliases it. List rows may return `aliases[].unitId = 0` where get-by-id returns the real unit. This comes from reading the SQL and was not run. Load the product by id to edit aliases. |
| Product `gdwnId`, `brCd` | Request-only. `PRMAST` has no such columns, so they are not returned. |
| Product image routes | Still `POST/PUT /products/image` (body has `productId`), and `GET /products/{id}/image` returns `prCode` instead of `id`. |
| Customers and Suppliers lists | Rows are still the full entity (with `rowVersion` and `pendingBills[]`). Products lists also keep full rows on purpose. |
| `GET /accounts/group/{groupId}` | Not paged, anonymous envelope (field names are fixed). |
| `GET /accounts/{id}/opening` | Returns "Account not found" when the opening balance is 0. |
| `vat-types` `id` | Still the string name, not a numeric code. |
| Hidden legacy routes | `/categories`, `/currenciesssss`, `/makedblank`, `/accounts/list-party` are not part of this contract. Use `/item-categories` and `/currencies`. |
