# Party group create error ("Unable to create party group.") – issues 8.3 / 9.2

## 1. Where logs go
- `Program.cs`, `appsettings.json`, `appsettings.Development.json` and the `.csproj` contain no Serilog, NLog, file logger or other logging provider.
- Only the `Logging:LogLevel` section exists (Default `Information`, `Microsoft.AspNetCore` `Warning`).
- The app uses the default ASP.NET Core providers, so logs go to the **console only** (and debug output). There is no log file.
- The catch-all is `Services/PartyGroupService.cs:72`: `_logger.LogError(ex, "Error creating party group.")`.

## 2. Call made
`POST http://localhost:5161/api/v1/party-groups` with `{ "groupName": "TRN Retail", "groupType": "Customer" }`

Result: **HTTP 200**, body `{"status":true,"message":"Party group created successfully."}`

The error did **not** reproduce, so there is no exception to capture.

## 3. Exception
None. Logs are console-only and the call succeeded, so I stopped here as instructed. The full exception can only be read from the console window where `dotnet run` is running, or from the original failing run.

## 4. Analysis (no fix applied)
The insert is at `Repository/Master/PartyGroupRepository.cs:57-70`:
`INSERT INTO PARTYGROUPMAS (GROUPNAME, GROUPSNAME, GROUPTYPE) VALUES (...)`, then `SELECT CAST(SCOPE_IDENTITY() AS INT)`.

Since the same payload now succeeds, the earlier failure was probably state-dependent. Unverified candidates:
- **Duplicate or unique-key violation** (SQL error 2601/2627) if a TRN group already existed when the earlier test ran. The service does a duplicate check first, so this is less likely.
- **`PARTYGROUPMAS` has no IDENTITY on its key column**, or the key is not auto-generated. `SCOPE_IDENTITY()` would then return NULL, and the insert fails on a NOT NULL key (error 515). The legacy app may use `GetnewId`. This would fail on every call, though, and it succeeded now, so it is unlikely.
- **GROUPNAME / GROUPSNAME length or NOT NULL mismatch** when `groupShortName` is null or the name is long (error 515 or 8152).
- A transient DB or connection problem (192.168.1.54).

To confirm, run `dotnet run` with the console visible and repeat the failing request from the frontend. Also compare the frontend's exact payload (for example a null or oversized `groupShortName`, or a different `groupType`) with the one used here. The proposed code fix depends on that exception, so I won't guess one.

Cheap improvement to propose: add a file or structured log sink, for example Serilog's file sink, so exceptions survive after the console closes.

## 5. Cleanup
A "TRN Retail" (Customer) party group **was created** by this test. The create response has no id, and finding it would need a GET endpoint, which the rules did not allow. **It has not been deleted.** To remove it, give me the id (or allow one `GET /api/v1/party-groups?search=TRN`) and I'll call `DELETE /api/v1/party-groups/{id}`.

---

# Follow-up test run (2026-11-26)

Base: `http://localhost:5161/api/v1/party-groups`. No code was changed.

| # | Request | Status | Body |
|---|---------|--------|------|
| 1 | POST `{"groupName":"TRN Supplier Grp","groupType":"Supplier"}` | 200 | `{"status":true,"message":"Party group created successfully."}` |
| 2 | POST `{"groupName":"TRN Retail 2","groupShortName":"TRN-R2","groupType":"Customer"}` | 200 | `{"status":true,"message":"Party group created successfully."}` |
| 3 | POST `{"groupName":"TRN Retail","groupType":"Customer"}` (duplicate) | 400 | `{"status":false,"message":"Group name already exists."}` |
| 4a | GET `?search=TRN&pageSize=100` | 400 | `{"status":false,"message":"The groupType field is required."}` |
| 4b | GET `?search=TRN&groupType=Customer&pageSize=100` | 200 | 2 rows: id 11 "TRN Retail" (the group left over from the first test), id 13 "TRN Retail 2" (short name TRN-R2) |
| 4c | GET `?search=TRN&groupType=Supplier&pageSize=100` | 200 | 1 row: id 12 "TRN Supplier Grp" |
| 5 | DELETE `/11`, `/12`, `/13` | 200 each | `{"status":true,"message":"Party group deleted successfully."}` |

## Findings
- Tests 1 and 2 passed (status true). A group with a short name saves correctly.
- Test 3 passed: the duplicate returns a clear message, "Group name already exists.", as HTTP 400. It is not a 500 or the generic "Unable to create party group."
- The list endpoint **requires `groupType`**, so the search in step 4 had to be run once per type (4b, 4c). This is a deviation from the literal call `GET /party-groups?search=TRN`.
- Cleanup is complete. The "TRN Retail" group (id 11) left over from the first run was deleted too. There are no TRN party groups left (3 found, 3 deleted).
- The original "Unable to create party group." error is still not reproduced. A duplicate gives a clear message, and normal creates succeed.
