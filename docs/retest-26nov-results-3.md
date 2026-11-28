# Retest 26-Nov — Results 3

Test-only run against http://localhost:5161/api/v1. No code changes. Every record created starts with "TRN". All three requests used countryCode 1, stateCode 4, isActive true.

| # | test | expected | actual | PASS/FAIL |
|---|------|----------|--------|-----------|
| A | POST /suppliers "TRN Supplier 06" with ledgerGroupId 7; GET supplier and account | supplier ledgerGroupId 7; account groupId 7 | 200 "Supplier and account created successfully." Supplier 48: ledgerGroupId **27**, accountCode S20000-48. Account 113 groupId **27**. | **FAIL** |
| B | POST /suppliers "TRN Supplier 07" with ledgerGroupId 99999 | 400, nothing created | **200** "Supplier and account created successfully." Supplier 49 / account 114 created, ledgerGroupId 27. | **FAIL** |
| C | POST /suppliers "TRN Supplier 08" without ledgerGroupId | ledgerGroupId 27 | 200. Supplier 50: ledgerGroupId 27, accountCode S20000-50. Account 115 groupId 27. | PASS |

## Notes

- A and B behave exactly as in results-2 (the value is ignored and 27 is used). The source now contains the fix (controller passes `request.LedgerGroupId`, shared rule, DTO `[Range(0,…)]`), so the running API is still serving the old behaviour. The fix is not active in the process under test. The process (PID 17700) started at 17:57; the last build that could update `bin\` failed on the exe lock (MSB3027), so `bin\Debug\net10.0\QuickBalance.API.dll` (17:54) most likely does not contain the fix. This was not verified further, because the run was test-only (no restart or rebuild was done).
- Retest A and B after stopping the API, running `dotnet build`, and starting it again. C passes either way because the default is 27.
- Response bodies for A and B were both `{"status":true,"message":"Supplier and account created successfully."}`.

## Clean-up

| record | action | result |
|--------|--------|--------|
| Supplier 48 "TRN Supplier 06" | DELETE | 200, "Supplier deleted successfully." |
| Supplier 49 "TRN Supplier 07" | DELETE | 200, "Supplier deleted successfully." |
| Supplier 50 "TRN Supplier 08" | DELETE | 200, "Supplier deleted successfully." |

After clean-up, `GET /suppliers?search=TRN` returns no records.

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
| 113 | TRN Supplier 06 | S20000-48 | 27 | true | this run |
| 114 | TRN Supplier 07 | S20000-49 | 27 | true | this run |
| 115 | TRN Supplier 08 | S20000-50 | 27 | true | this run |

Accounts 113 and 114 should be removed once the fix is live. 114 should never have been created.
