# Frontend issues – 26 November 2026

Source: frontend team. Scope: master endpoints under `/api/v1`.

---

## 1. Inventory – Item Group

### 1.1 Item Group List API – Missing Status Filter
- **Endpoint:** `GET /api/v1/item-group`
- The Item Group List API is missing a status filter.
- Please include a Status filter so users can filter item groups by Active/Inactive status.
- The filter must work through API query parameters and be integrated into the Item Group listing screen.

### 1.2 Clarification: Primary Group Dropdown API – Item Group Creation
- **Endpoint:** `GET /api/v1/item-group/primary-groups`
- Used to populate the Primary Group dropdown in the Item Group Creation screen.
- Please confirm whether this is the correct endpoint for fetching Primary Groups.
- If it is, please add **pagination and search** so the dropdown can load and search Primary Groups efficiently.
- Please provide the updated endpoint, supported query parameters (pagination and search), and a sample request and response.

---

## 2. Accounting – Ledger Group

### 2.1 Ledger Group Create API – Duplicate Group Code Error
- **Endpoint:** `POST /api/v1/ledger-groups`
- **Request:**
```json
{
  "groupName": "testhdkj",
  "subLedgerFlag": true,
  "mainGroup": false,
  "mainGroupId": 59,
  "isActive": true
}
```
- **Response:**
```json
{
  "status": false,
  "message": "Group Code already in use."
}
```
- Please investigate the cause and fix it so ledger groups can be created when the group code is valid.
- Ensure a proper API error message is returned when a duplicate group code is really submitted.

---

## 3. Accounting – Account Ledger

### 3.1 Account Ledger API – Tax Return Flag Clarification
- **Endpoint:** `/api/v1/accounts`
- The API has the `taxReturnFlag` field, but it is not in the current UI. Please clarify:
  1. Is `taxReturnFlag` system-generated or entered manually?
  2. If manual, where should it be handled in the UI?
  3. If system-generated, what is the logic for its value?
- How should this field be handled during account creation and saving?

### 3.2 Account Ledger List API – Missing Status Filter
- **Endpoint:** `GET /api/v1/accounts`
- The status filter is missing in the Account Ledger List API.
- Please include a Status filter so users can filter accounts by active/inactive status.

---

## 4. Godown – Location Group

### 4.1 Location Group API – Deleted Group Name Cannot Be Reused
- **Endpoint:** `/api/v1/location-groups`
- Steps:
  1. Create a location group named `MM`.
  2. Delete it.
  3. Create a new location group named `MM`.
  4. The API does not allow the creation.
- Please check whether deleted records are still included in the duplicate-name validation, and allow reuse of deleted names.

---

## 5. Payroll – Salesman Group

### 5.1 Salesman Group API – Deleted Group Name Cannot Be Reused
- **Endpoint:** `/api/v1/salesman-groups`
- Same steps as 4.1: create `MM`, delete it, create `MM` again → not allowed.
- Please check whether deleted records are still included in the duplicate-name validation, and allow reuse of deleted names.

---

## 6. Payroll – Sales Executive (Salesman)

### 6.1 Salesman API – Deleted Name Cannot Be Reused
- **Endpoint:** `/api/v1/salesman`
- Same steps as 4.1: create `MM`, delete it, create `MM` again → not allowed.
- Please check whether deleted records are still included in the duplicate-name validation, and allow reuse of deleted names.

### 6.2 Salesman API – Required Field Validation
- **Endpoint:** `/api/v1/salesman`
- The API documentation marks only these fields as required:
  - Salesman Name
  - Salesman Group ID
- But on save, the API also requires:
  1. **Email**: required on save, though not marked required in the documentation.
  2. **accountId**: required even when `connectWithAccount` is `false`.
- Expected:
  - `email` is optional.
  - `accountId` is not required when `connectWithAccount` is `false`.
- Validation rules must match the API documentation and business logic.

---

## 7. Inventory – Bill of Material

### 7.1 BOM API – Active/Inactive Status Clarification
- The BOM creation form has no Active/Inactive option, but the API has an Active/Inactive status (in the list).
- Should the status be system-generated, or should the UI provide an option to manage it?
- Please describe the expected API behaviour.

---

## 8. Accounting – Supplier

### 8.1 Supplier API – Inactive Supplier Cannot Be Deleted
- **Endpoint:** `DELETE /api/v1/suppliers/{id}`
- Deleting an inactive supplier returns **"Supplier not found."**
- Deleting an inactive supplier should work the same as deleting an active one.

### 8.2 Supplier API – Country Required
- The API documentation marks only `supplierName` as required.
- On save, the API also requires **country**.
- Please clarify why, and make validation consistent with the API schema.

### 8.3 Supplier Group Creation Issue
- **Endpoint:** `/api/v1/party-groups` (used from the Supplier form's "create group" modal)
- Creating a supplier group returns **"Unable to create."**
- Please fix the endpoint, or provide the correct endpoint for creating supplier groups.

---

## 9. Accounting – Customer

### 9.1 Customer Create API – 500 Error
- **Endpoint:** `POST /api/v1/customers`
- Returns **500 Internal Server Error** with the message **"Error updating customer."**
- Happens even with a valid request body as per the Swagger documentation.
- Please check and fix the backend issue.

### 9.2 Customer Party Group Create API
- **Endpoint:** `POST /api/v1/party-groups`
- **Request:**
```json
{
  "groupName": "Retail",
  "groupType": "Customer"
}
```
- Returns **"Unable to create party group."**
- The same issue occurs when creating a Supplier party group (see 8.3).
- Please fix both, and return the actual error message instead of a generic one.

---

## Summary

| # | Area | Issue | Type |
|---|---|---|---|
| 1.1 | Item Group | Missing status filter on list | Clarification / feature |
| 1.2 | Item Group | Primary groups: confirm endpoint, add paging and search | Feature |
| 2.1 | Ledger Group | Create fails: "Group Code already in use" | Bug |
| 3.1 | Accounts | `taxReturnFlag`: manual or system-generated? | Clarification |
| 3.2 | Accounts | Missing status filter on list | Clarification / feature |
| 4.1 | Location Groups | Deleted name cannot be reused | Bug |
| 5.1 | Salesman Groups | Deleted name cannot be reused | Bug |
| 6.1 | Salesman | Deleted name cannot be reused | Bug |
| 6.2 | Salesman | Email and accountId wrongly required | Validation |
| 7.1 | BOM | How to handle `isActive` in the UI | Clarification |
| 8.1 | Suppliers | Inactive supplier delete: "not found" | Bug |
| 8.2 | Suppliers | Country wrongly required | Validation |
| 8.3 | Party Groups | Supplier group create: "Unable to create" | Bug |
| 9.1 | Customers | Create returns 500 "Error updating customer" | Bug |
| 9.2 | Party Groups | Customer group create: "Unable to create party group" | Bug |
