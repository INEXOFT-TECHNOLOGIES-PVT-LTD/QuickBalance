# Retest 26-Nov — Results 4

Test-only run against http://localhost:5161/api/v1. No code changes. Every record created starts with "TRN". Both requests used countryCode 1, stateCode 4, active true.

| # | test | expected | actual | PASS/FAIL |
|---|------|----------|--------|-----------|
| A | POST /suppliers "TRN Supplier 09" with ledgerGroupId 7; GET supplier and account | supplier ledgerGroupId 7; account groupId 7 | 200 "Supplier and account created successfully." Supplier 51: ledgerGroupId **7**, accountCode S23000-51, accountId 116. Account 116 (GET /accounts?search=TRN): groupId **7**. | **PASS** |
| B | POST /suppliers "TRN Supplier 10" with ledgerGroupId 99999 | 400, nothing created | **400** `{"status":false,"message":"Selected supplier ledger group does not exist or is inactive."}`. No "TRN Supplier 10" in /suppliers?search=TRN or in /accounts?search=TRN. | **PASS** |

## Notes

- The fix is now active in the running API (results-3 had the old behaviour: value ignored, 27 used).

## Clean-up

| record | action | result |
|--------|--------|--------|
| Supplier 51 "TRN Supplier 09" | DELETE | 200, "Supplier deleted successfully." |

After clean-up, `GET /suppliers?search=TRN` returns 0 records. Deleting the supplier does not delete its account (account 116 remains), as in earlier runs.

### TRN accounts still present (GET /accounts?search=TRN). Not deleted.

| accountId | accountName | accountCode | groupId | isActive | origin |
|-----------|-------------|-------------|---------|----------|--------|
| 105 | TRN Customer 01 | C11000-38 | 26 | true | earlier run |
| 106 | TRN Customer 1b | C11000-39 | 26 | true | earlier run |
| 107 | TRN Customer 1c | C11000-40 | 26 | false | earlier run |
| 108 | TRN Customer 02 | C11000-41 | 26 | true | earlier run |
| 109 | TRN Supplier 01 | S20000-44 | 27 | true | results-2 |
| 110 | TRN Supplier 02 | S20000-45 | 27 | true | results-2 |
| 111 | TRN Supplier 03 | S20000-46 | 27 | false | results-2 |
| 112 | TRN Supplier 05 | S20000-47 | 27 | true | results-2 |
| 113 | TRN Supplier 06 | S20000-48 | 27 | true | results-3 |
| 114 | TRN Supplier 07 | S20000-49 | 27 | true | results-3 |
| 115 | TRN Supplier 08 | S20000-50 | 27 | true | results-3 |
| 116 | TRN Supplier 09 | S23000-51 | 7 | true | this run |
