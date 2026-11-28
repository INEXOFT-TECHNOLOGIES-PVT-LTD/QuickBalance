# Retest results – 26 Nov fixes

Run date: 2026-11-26. Base URL `http://localhost:5161/api/v1`. Test-only run, no code changed.
Ids used (from GETs): countryId 1, regionId 4 (Abha), salesman id 1, salesman group id 1, ledger group parentB 44 ("Test Main Group", finalAccount B), top-level parent 30 (Asset), BOM 9.

Summary: 41 rows, 37 PASS, 4 FAIL (8.1 supplier create, its retry, 8.1 supplier delete not testable, 2.1 Test 1 groupCode). See "FAILs" below.

| section | test | request | expected | actual | PASS/FAIL |
|---|---|---|---|---|---|
| 9.1 | Test 1 – no ledgerGroupId, no salesman | POST /customers (cusName "TRN Customer 01", countryCode 1, stateCode 4, isActive true) | status true | 200 `status:true` "Customer and account created successfully." | PASS |
| 9.1 | Test 1 – GET new id | GET /customers/38 | ledgerGroupId 26 | 200, `ledgerGroupId: 26` | PASS |
| 9.1 | Test 1b – create, isActive left out | POST /customers ("TRN Customer 1b", no isActive) | saved active | 200 `status:true`; GET /customers/39 → `isActive: true` | PASS |
| 9.1 | Test 1b – create inactive | POST /customers ("TRN Customer 1c", isActive false) | created inactive | 200 `status:true`; GET /customers/40 → `isActive: false` | PASS |
| 9.1 | Test 1b – PUT without isActive | PUT /customers/40 (GET data, isActive removed), then GET /customers/40 | `isActive: true` (re-activated, known behaviour) | PUT 200 "Customer and linked account updated successfully."; GET → `isActive: true` | PASS |
| 9.1 | Test 2 – valid salesman | POST /customers ("TRN Customer 02", salesManId 1) | status true | 200 `status:true`; GET /customers/41 → `salesManId: 1` | PASS |
| 9.1 | Test 3 – wrong salesman | POST /customers (salesManId 99999) | 400 "Selected salesman does not exist or is inactive." | 400 "Selected salesman does not exist or is inactive." | PASS |
| 9.1 / 8.2 | Test 4 – no country | POST /customers (no countryCode) | 400 "Please select a valid country." | 400 "Please select a valid country." | PASS |
| 6.2 | Test 1 – no email, no account | POST /salesman ("TRN Salesman 01", salesmanGroupId 1, connectWithAccount false) | status true | 200 `status:true` "Salesman created successfully." (id 5) | PASS |
| 6.2 | Test 2 – wrong email | POST /salesman (email "abc") | 400 "Incorrect email format." | 400 "Incorrect email format." | PASS |
| 6.2 | Test 3 – connect, no account | POST /salesman (connectWithAccount true, no accountId) | 400 account required | 400 "Account ID is required when connecting with an account." | PASS |
| 4.1 | Salesman groups – create "TRN-MM" | POST /salesman-groups | status true | 200 `status:true`, id 2 | PASS |
| 4.1 | Salesman groups – delete | DELETE /salesman-groups/2 | success | 200 "Salesman group deleted successfully." | PASS |
| 4.1 | Salesman groups – recreate same name | POST /salesman-groups (same body) | status true | 200 `status:true` "Salesman group created successfully." (id 3) | PASS |
| 5.1 | Salesman – create "TRN-MM" | POST /salesman | status true | 200 `status:true`, id 6 | PASS |
| 5.1 | Salesman – delete | DELETE /salesman/6 | success | 200 "Salesman deleted successfully." | PASS |
| 5.1 | Salesman – recreate same name | POST /salesman (same body) | status true | 200 `status:true` "Salesman created successfully." (id 7) | PASS |
| 6.1 | Location groups – create "TRN-MM" | POST /location-groups | status true | 200 `status:true`, id 8 | PASS |
| 6.1 | Location groups – delete | DELETE /location-groups/8 | success | 200 "Location group deleted successfully." | PASS |
| 6.1 | Location groups – recreate same name | POST /location-groups (same body) | status true | 200 `status:true` "Location group created successfully." (id 10) | PASS |
| 6.1 | Location groups extra – create inactive "TRN-NN" | POST /location-groups (isActive false) | created | 200 `status:true`, id 9 | PASS |
| 6.1 | Location groups extra – duplicate of inactive | POST /location-groups ("TRN-NN", isActive true) | rejected as duplicate | 400 "Location group name already exists." | PASS |
| 8.1 | Supplier create (inactive) | POST /suppliers ("TRN Supplier 01", countryCode 1, stateCode 4, isActive false) | status true | 400 "Selected supplier ledger group does not exist or is inactive." | FAIL |
| 8.1 | Supplier create – retry with ledgerGroupId 27 (one allowed retry) | POST /suppliers (same + `"ledgerGroupId": 27`) | status true | 400 "Selected supplier ledger group does not exist or is inactive." | FAIL |
| 8.1 | Supplier delete (inactive) | DELETE /suppliers/<newId> | status true | NOT RUN – no supplier could be created | FAIL (blocked) |
| 8.2 | Supplier – no country | POST /suppliers (no countryCode) | 400 "Please select a valid country." | 400 "Please select a valid country." | PASS |
| 2.1 | Test 1 – parent with final account | POST /ledger-groups ("TRN LG 01", mainGroupId 44); GET | status true; finalAccount "B"; groupCode next to parent's code | 200 `status:true`; GET → `finalAccount: "B"`, `groupCode: 846132865`. Parent 44 has groupCode 232442344. 846132865 is the next number after sibling 45 (846132864) but is the **same** as groupCode of group 46 ("Test Inactive Group"), so it duplicates another group's code | FAIL (groupCode) |
| 2.1 | Test 2 – client sends finalAccount P | POST /ledger-groups ("TRN LG 02", mainGroupId 44, finalAccount "P"); GET | status true; GET finalAccount "B" | 200 `status:true`; GET → `finalAccount: "B"` (groupCode 846132866) | PASS |
| 2.1 | Test 3 – top-level parent, no finalAccount | POST /ledger-groups ("TRN LG 03", mainGroupId 30) | 400 "Final Account is required for this group (B, P, T or M)." | 400 "Final Account is required for this group (B, P, T or M)." | PASS |
| 2.1 | Test 4 – top-level parent with finalAccount | POST /ledger-groups ("TRN LG 04", mainGroupId 30, finalAccount "B") | status true | 200 `status:true`; GET → `finalAccount: "B"`, groupCode 232442346 | PASS |
| 1.2 | Primary groups, no params | GET /item-group/primary-groups | full list, no pagination | 200, 7 rows, no `pagination` | PASS |
| 1.2 | Primary groups, paged | GET /item-group/primary-groups?page=1&pageSize=5 | max 5 rows and pagination | 200, 5 rows, `pagination {page 1, pageSize 5, totalRecords 7, totalPages 2}` | PASS |
| 1.2 | Primary groups, search | GET /item-group/primary-groups?search=a | only names with "a", no pagination | 200, 3 rows (Sundry Debtors - Updated, SwTest-Primary, SwTest-Primary2), no `pagination` | PASS |
| 1.2 | Primary groups, paged + search | GET /item-group/primary-groups?page=1&pageSize=5&search=a | filtered + paged, totalRecords = matches | 200, 3 rows, `pagination {totalRecords 3, totalPages 1}` | PASS |
| 1.1 | Item group active=true | GET /item-group?active=true | only isActive true | 200, 11 rows, all `isActive: true` | PASS |
| 1.1 | Item group active=false | GET /item-group?active=false | only isActive false | 200, 0 rows (no inactive item groups exist, so this is an empty-result pass, not proof of filtering) | PASS |
| 3.2 | Accounts active=true | GET /accounts?active=true | only isActive true | 200, page 1 = 20 rows all `isActive: true`, totalRecords 79 | PASS |
| 3.2 | Accounts active=false | GET /accounts?active=false | only isActive false | 200, 1 row `isActive: false`, totalRecords 1 | PASS |
| 7.1 | Step 2 – PUT BOM 9 with isActive false | PUT /boms/9 (GET data + isActive false) | status true | 200 `status:true` "bill of materials updated successfully." | PASS |
| 7.1 | Steps 3–4 – PUT without isActive, then GET | PUT /boms/9 (same data, no isActive); GET /boms/9 | `isActive: false` | PUT 200 `status:true`; GET → `isActive: false` | PASS |
| 7.1 | Step 5 – PUT with isActive true | PUT /boms/9 (isActive true); GET /boms/9 | `isActive: true` | PUT 200 `status:true`; GET → `isActive: true` (BOM 9 is back to its original active state) | PASS |

## FAILs (full response bodies)

### 8.1 Supplier create – blocked by ledger group validation

Request 1: `POST /suppliers`
```json
{"supName":"TRN Supplier 01","alias":"TRN-SUP-01","countryCode":1,"stateCode":4,"isActive":false}
```
Response (HTTP 400):
```json
{"status":false,"message":"Selected supplier ledger group does not exist or is inactive."}
```

Retry (one allowed): same body plus `"ledgerGroupId": 27` (the value existing suppliers carry in `GET /suppliers`).
Response (HTTP 400):
```json
{"status":false,"message":"Selected supplier ledger group does not exist or is inactive."}
```

Notes for whoever fixes it (not changed here):
- `GET /suppliers/lookups/ledger-groups` returns only ids 7, 2, 6, 34 (Duties and Taxes, Long Term Liability / Bank OD, Other Current Liabilities, Outstanding / Accrued Exp). Id 27 ("Accounts Payable / Sundry Creditors") is not in that list, although every existing supplier uses 27.
- I did not try ids from the lookup, because the instructions allowed only one retry. As a result the supplier delete test (8.1 step 2, the real check of this fix) was **not run**.
- Without `ledgerGroupId` the body in the checklist fails, so the checklist body is missing a required field, or the API should default it the way customers default to 26.

### 8.1 Supplier delete – not run
No supplier was created, so `DELETE /suppliers/<newId>` could not be tested.

### 2.1 Test 1 – groupCode
Request: `POST /ledger-groups`
```json
{"groupName":"TRN LG 01","subLedgerFlag":true,"mainGroup":false,"mainGroupId":44,"isActive":true}
```
Response (HTTP 200):
```json
{"status":true,"message":"Ledger group created successfully."}
```
Resulting record (from `GET /ledger-groups?pageSize=100`): `"id":48,"groupName":"TRN LG 01","groupCode":846132865,"finalAccount":"B","mainGroupId":44`.
`finalAccount` is correct. The `groupCode` (846132865) is the same as the code of the unrelated top-level group 46 ("Test Inactive Group", 846132865), and the parent's own code is 232442344. Existing child 45 has 846132864, so the new code follows that sibling's series rather than the parent's code. Check whether code uniqueness matters here. The same sequence is also used by children of parent 30: TRN LG 04 got 232442346 after group 47 (232442345).

## Other observations (not FAILs)
- Deleting a customer does not delete its linked account (see clean-up).
- `GET /accounts?active=true&pageSize=200` returned HTTP 400 (my extra param, not part of the checklist); `/item-group?pageSize=200` was silently clamped to 100. The checklist requests without `pageSize` work.

## Clean-up
Deleted (all HTTP 200): customers 38, 39, 40, 41; salesman 5, 7; salesman group 3 (id 2 deleted during the test); location groups 9, 10 (id 8 deleted during the test); ledger groups 48, 49, 50. Search for `TRN` in customers, salesman, salesman-groups, location-groups, ledger-groups and suppliers now returns 0 records.

**Not deleted (no endpoint in the checklist):** the 4 accounts that were created with the customers still exist in `GET /accounts?search=TRN`: "TRN Customer 01", "TRN Customer 1b", "TRN Customer 1c", "TRN Customer 02" (accountIds 105, 106, 107, 108; accountCodes like `C11000-38`). They need a manual cleanup, or an account DELETE call if you approve one.

No suppliers were created, so there was nothing to remove there.

Also changed and restored: BOM 9 ("Office Computer Assembly Test ") was set inactive and then back to active during test 7.1. It is active again, but its `uTime` has been updated.

Skipped as instructed: "Still open" (party groups 8.3 / 9.2).
