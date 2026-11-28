# Retest checklist – 26 Nov fixes

Run these in Swagger (or Postman) **after Rebuild + restart** of the API.
Replace `<...>` with real ids from your own database (use the GET shown next to it).
Tick each line when the result matches **Expected**.

Base URL: `http://localhost:5161/api/v1`

---

## 9.1 Customer create (was 500)

**Find ids first:**
- Country: `GET /countries` → pick an `id` (example: 1)
- Region: `GET /regions?countryId=<countryId>` → pick an `id` (example: 4)
- Salesman: `GET /customers/lookups/salesman` → pick an `id`

### Test 1 – no ledgerGroupId, no salesman
`POST /customers`
```json
{
  "cusName": "TRN Customer 01",
  "alias": "TRN-CUS-01",
  "address": "Test address",
  "phone": "9000000001",
  "countryCode": <countryId>,
  "stateCode": <regionId>,
  "isActive": true
}
```
**Expected:** `status: true` ("Customer and account created successfully.").
Then `GET /customers/<newId>` → `ledgerGroupId: 26`.

### Test 1b – `isActive` left out
**On create:** same body without `isActive` → saved as **active** (default `true`). Safe.

**On update (check this):**
1. Create a customer with `"isActive": false`.
2. `GET /customers/<newId>` → copy `data`, remove the `isActive` field.
3. `PUT /customers/<newId>` with that body.
4. `GET /customers/<newId>`

**Expected today:** `isActive: true`, because a missing `isActive` on update is treated as `true`, so the customer is **re-activated**.
Unlike BOMs (7.1), customers do not keep the current status yet. Until that changes, the frontend must **always send `isActive`** on edit.

### Test 2 – valid salesman
Same body, new name and alias, plus `"salesManId": <salesmanId>`.
**Expected:** `status: true`.

### Test 3 – wrong salesman
Same body, new name and alias, plus `"salesManId": 99999`.
**Expected:** 400 "Selected salesman does not exist or is inactive."

### Test 4 – no country (8.2)
Same body without `countryCode`.
**Expected:** 400 "Please select a valid country."

---

## 6.2 Salesman (email / accountId)

**Find id first:** `GET /salesman-groups` → pick an `id`.

### Test 1 – no email, no account
`POST /salesman`
```json
{
  "salesmanName": "TRN Salesman 01",
  "salesmanGroupId": <groupId>,
  "connectWithAccount": false
}
```
**Expected:** `status: true` ("Salesman created successfully.").

### Test 2 – wrong email format
Same body, new name, plus `"email": "abc"`.
**Expected:** 400 "Incorrect email format."

### Test 3 – connect with account but no account
Same body, new name, with `"connectWithAccount": true` and no `accountId`.
**Expected:** 400 (account required).

---

## 4.1 / 5.1 / 6.1 Deleted name can be reused

Do the same 3 steps for each endpoint.

### Salesman groups
1. `POST /salesman-groups`
```json
{ "groupName": "TRN-MM", "isActive": true }
```
2. `DELETE /salesman-groups/<newId>`
3. `POST /salesman-groups` with the **same** body again.

**Expected:** step 3 → `status: true`.

### Salesman
1. `POST /salesman`
```json
{ "salesmanName": "TRN-MM", "salesmanGroupId": <groupId>, "connectWithAccount": false }
```
2. `DELETE /salesman/<newId>`
3. `POST /salesman` with the **same** body again.

**Expected:** step 3 → `status: true`.

### Location groups
1. `POST /location-groups`
```json
{ "groupName": "TRN-MM", "isActive": true }
```
2. `DELETE /location-groups/<newId>`
3. `POST /location-groups` with the **same** body again.

**Expected:** step 3 → `status: true`.

**Extra (inactive still blocks):**
1. `POST /location-groups` with `{ "groupName": "TRN-NN", "isActive": false }`
2. `POST /location-groups` with `{ "groupName": "TRN-NN", "isActive": true }`

**Expected:** step 2 → rejected as duplicate.

---

## 8.1 Delete inactive supplier

**Find ids first:** `GET /countries`, `GET /regions?countryId=<countryId>`.

1. `POST /suppliers`
```json
{
  "supName": "TRN Supplier 01",
  "alias": "TRN-SUP-01",
  "countryCode": <countryId>,
  "stateCode": <regionId>,
  "isActive": false
}
```
2. `DELETE /suppliers/<newId>`

**Expected:** step 2 → `status: true` (not "Supplier not found.").

**8.2 check:** step 1 body without `countryCode` → 400 "Please select a valid country."

---

## 2.1 Ledger group create

**Find ids first:** `GET /ledger-groups?pageSize=100`
- `<parentB>` = a group whose `finalAccount` is `"B"`
- `30` = "Asset" (top-level, empty finalAccount)

### Test 1 – the frontend's payload (parent with a final account)
`POST /ledger-groups`
```json
{
  "groupName": "TRN LG 01",
  "subLedgerFlag": true,
  "mainGroup": false,
  "mainGroupId": <parentB>,
  "isActive": true
}
```
**Expected:** `status: true`. GET it → `finalAccount: "B"`, `groupCode` next to the parent's code.

### Test 2 – client sends a different value
Same body, new name, plus `"finalAccount": "P"`.
**Expected:** `status: true`, and GET shows `finalAccount: "B"` (parent wins).

### Test 3 – top-level parent, no finalAccount
Same body, new name, `"mainGroupId": 30`, no `finalAccount`.
**Expected:** 400 "Final Account is required for this group (B, P, T or M)."

### Test 4 – top-level parent with finalAccount
Same as Test 3 plus `"finalAccount": "B"`.
**Expected:** `status: true`.

---

## 1.2 Primary groups (paging and search)

| Request | Expected |
|---|---|
| `GET /item-group/primary-groups` | Full list, **no** `pagination` |
| `GET /item-group/primary-groups?page=1&pageSize=5` | Max 5 rows **and** `pagination` |
| `GET /item-group/primary-groups?search=a` | Only names containing "a", no `pagination` |
| `GET /item-group/primary-groups?page=1&pageSize=5&search=a` | Filtered + paged, `totalRecords` = matches only |

---

## 1.1 / 3.2 Status filters (no change, just confirm)

| Request | Expected |
|---|---|
| `GET /item-group?active=true` | Only `isActive: true` rows |
| `GET /item-group?active=false` | Only `isActive: false` rows |
| `GET /accounts?active=true` | Only `isActive: true` rows |
| `GET /accounts?active=false` | Only `isActive: false` rows |

---

## 7.1 BOM update keeps status

1. `GET /boms/<bomId>` → copy `data`.
2. `PUT /boms/<bomId>` with that data and `"isActive": false` → `status: true`.
3. `PUT /boms/<bomId>` with the same data **without** the `isActive` field.
4. `GET /boms/<bomId>`

**Expected:** step 4 → `isActive: false` (stays inactive).

5. `PUT /boms/<bomId>` with `"isActive": true` → GET shows `isActive: true`.

---

## Clean-up

The test records start with **TRN** so they are easy to find.
Delete them afterwards with the DELETE endpoints (customers, suppliers, salesman,
salesman groups, location groups, ledger groups).

---

## Still open (do not test yet)

- 8.3 / 9.2 Party group create – waiting for the exception from the API log.
