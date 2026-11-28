# Server test – 26 Nov fixes (retest-26nov.md subset)

Base URL: `http://3.6.168.118:8080/api/v1`. Test only, no code changed. Token read from `$QB_TOKEN`, never printed or saved.
Ids below come from GETs on this server: country 5 (Afghanistan), region 99, salesman group 1, parent ledger group 41 ("Current Assets", finalAccount B).

| # | Step | Expected | Actual | Result |
|---|---|---|---|---|
| 1 | `GET /item-group/primary-groups?page=1&pageSize=5` | 5 rows + `pagination` | 5 rows, `pagination` = page 1, size 5, 50 records, 10 pages | PASS |
| 2a | `GET /suppliers/lookups/ledger-groups` contains 27 | id 27 present | 27 "Accounts Payable / Sundry Creditors" present | PASS |
| 2b | `GET /ledger-groups/26` isActive | true | true (also 27 is active) | PASS |
| 3a | `POST /customers` (no ledgerGroupId, no salesman) | `status:true` | 200 "Customer and account created successfully."; GET shows `ledgerGroupId: 26`, accountId 639 | PASS |
| 3b | `POST /suppliers` (no ledgerGroupId) | `status:true` | 200 "Supplier and account created successfully."; GET shows `ledgerGroupId: 27`, accountId 640 | PASS |
| 3c | `POST /party-groups` `{groupName:"TRN Party Cust", groupType:"Customer"}` | `status:true` | **400 "Unable to create party group."** Retry with `groupShortName` gave the same 400. | **FAIL** |
| 3d | `POST /ledger-groups` under parent 41 (finalAccount B) | `status:true`, finalAccount B | 200; GET shows `finalAccount: "B"`, groupCode 620030000, mainGroupId 41 | PASS |
| 4 | `POST /salesman` without email, connectWithAccount false | `status:true` | 200 "Salesman created successfully." (id 9); GET shows `email: ""`, `accountId: 0` | PASS |
| 5 | Salesman group "TRN-MM": create, delete, create again | 3rd call `status:true` | create (id 8) OK, delete OK, create again OK (id 9) | PASS |
| 6 | Supplier "TRN Sup Inactive" with `isActive:false`, then DELETE | DELETE `status:true` | create OK (id 374, isActive false); DELETE 200 "Supplier deleted successfully." | PASS |

## Party group failure (3c, issue 8.3 / 9.2)
- The error reproduces on this server. `POST /party-groups` returns 400 "Unable to create party group." for both payloads tried.
- `GET /party-groups?groupType=Customer&page=1&pageSize=50` also returns 400 "Unable to retrieve party groups." So the party group read path fails too, not only the insert.
- Without `groupType`, the GET returns "The groupType field is required." (normal validation).
- I have no access to the server logs, so the exception is not captured. It is likely a DB-level problem on this server (for example the `PARTYGROUPMAS` table or its key), since the same call worked on the local database (see `docs/party-group-error.md`). That is unverified.
- No party group was created, so there is nothing to clean up. The controller has no DELETE endpoint for party groups.

## Ids created
| Kind | Id | Name |
|---|---|---|
| Customer | 72 | TRN Customer 01 |
| Customer account | 639 | TRN Customer 01 |
| Supplier | 373 | TRN Supplier 01 |
| Supplier account | 640 | TRN Supplier 01 |
| Supplier | 374 | TRN Sup Inactive |
| Supplier account | 641 | TRN Sup Inactive |
| Ledger group | 105 | TRN LG 01 |
| Salesman | 9 | TRN Salesman 01 |
| Salesman group | 8 | TRN-MM (deleted in step 5) |
| Salesman group | 9 | TRN-MM (recreated) |

## Ids deleted
Each was fetched first and its name started with "TRN".
- Supplier 374 (step 6)
- Salesman group 8 (step 5)
- Customer 72, supplier 373, salesman 9, salesman group 9, ledger group 105
- Accounts 639, 640, 641. Deleting the customer and suppliers did **not** remove their linked accounts, so they were deleted separately.

After clean-up, `?search=TRN` returns 0 rows for customers, suppliers, accounts, ledger-groups, salesman and salesman-groups.

## Skipped
- BOM test (7.1): manual, because it needs a PUT on an existing record.
- Customer tests 1b, 2, 3, 4; salesman tests 2 and 3; location groups; salesman name reuse; ledger group tests 2–4; primary-groups search variants; status filters: not in the requested list.
- Party group cleanup: nothing created.
- Pre-existing records: none changed. A TRN search before the run found none.

## Notes
- Deleting a customer or supplier leaves the linked account behind. Check whether this is intended.

## Party groups retest
Base URL `http://3.6.168.118:8080/api/v1`. Only the four calls below; no DELETE or PUT, no code changed.

| # | Call | Expected | Actual status | Actual message | Rows | Result |
|---|---|---|---|---|---|---|
| 1 | `GET /party-groups?groupType=Customer` | 200 + list | 200 | Party groups retrieved successfully. | 3 | PASS |
| 2 | `GET /party-groups?groupType=Supplier` | 200 + list | 200 | Party groups retrieved successfully. | 1 | PASS |
| 3 | `POST /party-groups` `{groupName:"TRN Party", groupType:"Customer"}` | "Party group created successfully." | 200 | Party group created successfully. | – | PASS |
| 4 | Same POST again | "Group name already exists." | 400 | Group name already exists. | – | PASS |

The earlier 400 on create and list (issue 8.3 / 9.2) no longer occurs on this server.

**Left behind:** party group "TRN Party" (Customer) was created in step 3 and **not deleted**, because the rules forbade DELETE and the controller has no DELETE endpoint for party groups. Its id was not read (the create response has none). Remove it manually if needed.
