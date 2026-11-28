# Retest 26-Nov — Results 2

Test-only run against http://localhost:5161/api/v1. Nothing was fixed. Every record created starts with "TRN".

| # | test | request | expected | actual (status + message) | PASS/FAIL |
|---|------|---------|----------|---------------------------|-----------|
| 1 | Lookup includes id 27 | GET /suppliers/lookups/ledger-groups | list contains id 27 | 200, status true. Ids returned: 27, 7, 2, 6, 34 | PASS |
| 2 | Default ledger group | POST /suppliers "TRN Supplier 01" (countryCode 1, stateCode 4, isActive true, no ledgerGroupId); then GET /suppliers/44 | status true, ledgerGroupId 27 | 200, "Supplier and account created successfully."; GET returns ledgerGroupId 27 | PASS |
| 3 | Explicit ledgerGroupId 7 | POST /suppliers "TRN Supplier 02" with ledgerGroupId 7; then GET /suppliers/45 | status true, ledgerGroupId 7 | 200, "Supplier and account created successfully."; GET returns ledgerGroupId **27**. The account (id 110) also has groupId 27. | **FAIL** |
| 4 | Fix 8.1: delete inactive supplier | POST /suppliers "TRN Supplier 03" with isActive false; DELETE /suppliers/46 | created; delete returns status true | Create 200. DELETE 200, "Supplier deleted successfully." | PASS |
| 5 | Invalid ledgerGroupId 99999 | POST /suppliers "TRN Supplier 05" with ledgerGroupId 99999 | 400 with a clear message | 200, "Supplier and account created successfully." Supplier 47 / account 112 were created, and GET returns ledgerGroupId 27 (the value was ignored, not rejected) | **FAIL** |
| 6 | Fix 2.1: ledger group code | POST /ledger-groups "TRN LG 05" (subLedgerFlag true, mainGroup false, mainGroupId 44, isActive true) | status true, unique groupCode | 200, "Ledger group created successfully." id 51, groupCode 846132866. No other group has this code. | PASS |
| 7 | Next free code | POST /ledger-groups "TRN LG 06" (same body) | status true, next free unique code | 200, "Ledger group created successfully." id 52, groupCode 846132867. No other group has this code. | PASS |

## Notes

- **Test 3 and 5 request field.** The ledger group field was sent as `ledgerGroupId` in the JSON body, as specified. It is ignored on create. Test 3 and test 5 both ended up with 27, even though 5 had the invalid id 99999. This is probably the same root cause: the supplier create path never reads the value. Check the DTO property name and the repository insert. This was not investigated, because the run was test-only.
- **Test 4.** The suppliers' required name field is `supName`. No other extra fields were needed.
- **Test 6 and 7 neighbours.** The highest existing codes before the run were 846132864 (id 45 "Test Sub Group", active) and 846132865 (id 46 "Test Inactive Group", inactive). TRN LG 05 took 846132866 and TRN LG 06 took 846132867, so each was the previous maximum + 1. Both unique across active and inactive groups.
- **Pre-existing duplicate code (not caused by this run).** Ids 17 ("Retained Earning / Profit and Loss") and 20 ("Reserves & Surplus") both have groupCode 31000.

## Full response bodies for FAILs

**Test 3** — POST /suppliers (ledgerGroupId 7), HTTP 200:
```json
{"status":true,"message":"Supplier and account created successfully."}
```
GET /suppliers/45 returned `"ledgerGroupId":27` (full body not saved).

**Test 5** — POST /suppliers (ledgerGroupId 99999), HTTP 200:
```json
{"status":true,"message":"Supplier and account created successfully."}
```
GET /suppliers/47 returned `"ledgerGroupId":27` (full body not saved).

## Clean-up

| record | action | result |
|--------|--------|--------|
| Supplier 44 "TRN Supplier 01" | DELETE | 200, "Supplier deleted successfully." |
| Supplier 45 "TRN Supplier 02" | DELETE | 200, "Supplier deleted successfully." |
| Supplier 46 "TRN Supplier 03" | DELETE (test 4) | 200, "Supplier deleted successfully." |
| Supplier 47 "TRN Supplier 05" | DELETE | 200, "Supplier deleted successfully." |
| Ledger group 51 "TRN LG 05" | DELETE | 200, "Ledger group Deleted successfully." |
| Ledger group 52 "TRN LG 06" | DELETE | 200, "Ledger group Deleted successfully." |

After clean-up, `GET /suppliers?search=TRN` and the ledger group list contain no TRN records.

### Linked accounts still present (GET /accounts?search=TRN). Not deleted.

| accountId | accountName | accountCode | groupId | isActive | origin |
|-----------|-------------|-------------|---------|----------|--------|
| 105 | TRN Customer 01 | C11000-38 | 26 | true | previous run |
| 106 | TRN Customer 1b | C11000-39 | 26 | true | previous run |
| 107 | TRN Customer 1c | C11000-40 | 26 | false | previous run |
| 108 | TRN Customer 02 | C11000-41 | 26 | true | previous run |
| 109 | TRN Supplier 01 | S20000-44 | 27 | true | this run |
| 110 | TRN Supplier 02 | S20000-45 | 27 | true | this run |
| 111 | TRN Supplier 03 | S20000-46 | 27 | false | this run |
| 112 | TRN Supplier 05 | S20000-47 | 27 | true | this run |

Deleting a supplier does not remove its linked account, so 8 TRN accounts remain: the 4 from the previous run (105-108) and 4 new ones (109-112). Account 112's row was cut off in the output I captured, but the account is there.
