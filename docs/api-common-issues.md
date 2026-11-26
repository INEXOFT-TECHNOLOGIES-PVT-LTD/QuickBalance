# **API Common Issues** 

Consistency rules for every endpoint  ·  Frontend team  ·  24 November 2026 

## **Purpose** 

While connecting the V3 API (/api/v1) to the frontend, we found four kinds of inconsistency that repeat across endpoints. Each section below states the rule every endpoint should follow, then shows the endpoints where we found the problem. 

The endpoints named here are examples only, not a complete list. Please check every endpoint against each rule, including endpoints the frontend does not use yet. 

## **Summary** 

|**#**|**Common issue**|**Rule for every endpoint**|**Examples found**|
|---|---|---|---|
|1|List response envelope|Every list returnsstatus,message,<br>data(array) andpagination|/accounts,/products,<br>/customers|
|2|List row shape|List rows are flat objects with camelCase<br>keys|/boms|
|3|Status field and filter|One field name,isActive, and anactive<br>filter on every master|/customers,/accounts,<br>/boms,/countries|
|4|Request and response<br>names|A field has the same name and type in the<br>request and the response|/accounts,/products,<br>/boms|



## **1. List response envelope** 

**Rule:** Every paged list returns the same envelope: status, message, data (always an array) and pagination. 

**Problem:** Some lists leave fields out or put the paging values somewhere else. The frontend reads the next page from pagination, so a list without it stops after the first page. 

**Example A: GET /accounts (current)** 

<mark>{ "data": [ { "id": 7, "accName": "Cash In Hand", "active": true } ], "page": 1, "pageSize": 20, "totalRecords": 45, "totalPages": 3 }</mark> 

V3 API Common Issues  ·  Page 1 

**Example B: GET /products and GET /customers (current)** 

<mark>// GET /products: no status or message { "data": [ { "id": 1, "prName": "Pepsi 330ml", "active": true } ], "pagination": { "page": 1, "pageSize": 20, "totalRecords": 45, "totalPages": 3 } } // GET /customers: no message { "status": true, "data": [ { "id": 3, "cusName": "Al Noor Trading", "active": true } ], "pagination": { "page": 1, "pageSize": 20, "totalRecords": 45, "totalPages": 3 } }</mark> 

**Expected (every list)** 

<mark>{ "status": true, "message": "Accounts retrieved successfully.", "data": [ { "id": 7, "accountName": "Cash In Hand", "isActive": true } ], "pagination": { "page": 1, "pageSize": 20, "totalRecords": 45, "totalPages": 3 } }</mark> 

**Please check:** that every list endpoint returns this envelope. Also check Swagger: 12 list endpoints (units, taxes, brands, item-group and others) describe data as a single object instead of an array. 

## **2. List row shape** 

**Rule:** Each row in a list is a flat object with camelCase keys. Nested detail (line items, extra rows) belongs in the get-by-id response only. 

**Problem:** Some list rows wrap their fields in a nested object, or use snake_case or underscoreprefixed keys. The frontend cannot read the row id or its columns. 

**Example: GET /boms (current)** 

<mark>"data": [ { "bilofm_details": { "id": 5, "bomName": "Chair", "alias": "CH1" }, "items": [ { "id": 1, "prCode": 20, "prName": "Wood", "qty": 4, "unit": 1, "type": "C" } ], "additionalInfo": [ { "id": 1, "accountId": 40, "amount": 10 } ] } ]</mark> 

V3 API Common Issues  ·  Page 2 

### **Expected** 

<mark>"data": [ { "id": 5, "bomName": "Chair", "alias": "CH1", "productName": "Wooden Chair", "qty": 1, "unitName": "Pcs" } ]</mark> 

**Please check:** that every list returns flat rows with camelCase keys. Products also use underscoreprefixed request keys (_SLNOTRACK, _BMPL, _BMPQ, _BMPW, _BMEQN). 

## **3. Status field and active filter** 

**Rule:** Every master that can be deactivated uses the field name isActive and accepts ?active=true| false on its list. Leaving out active returns both active and inactive records. 

**Problem:** Accounts, customers, suppliers and products use active. Every other master uses isActive. Some endpoints have the field but no filter, or the filter but no field. 

**Example: GET /customers and GET /units rows (current)** 

<mark>// GET /customers row { "id": 3, "cusName": "Al Noor Trading", "active": true } // GET /units row { "id": 1, "unitName": "Kilogram", "isActive": true }</mark> 

### **Expected (every master)** 

<mark>{ "id": 3, "cusName": "Al Noor Trading", "isActive": true }</mark> 

### **Also found** 

- /boms accepts ?active= but its rows have no status field. 

- /countries has a status field but no ?active= filter. 

- /regions defaults isActive to false when it is left out. Every other master defaults it to true. 

**Please check:** every master for the field name, the filter, the default value on create, and what an omitted active returns. 

## **4. Same field names in request and response** 

**Rule:** A field has the same name and the same type in the request body and in the response. 

**Problem:** Several endpoints rename fields between the request and the response. An edit form then has to rename every field before it can save a record it just loaded. 

V3 API Common Issues  ·  Page 3 

### **Example: /accounts (current)** 

<mark>// POST /accounts body // GET /accounts/7 response</mark> 

<mark>{ "accountName": "Cash In Hand", "openingBalance": 5000, "groupId": 12 }</mark> 

- <mark>{ "accName": "Cash In Hand", "openBal": 5000, "ledgerGroupId": 12 }</mark> 

### **Expected (request and response)** 

- <mark>{ "accountName": "Cash In Hand", "openingBalance": 5000, "groupId": 12 }</mark> 

### **Also found** 

- Products: desc and description, catgId and categoryId, notes and prNotes. brandId is sent as a number but returned as a string. 

- BOMs: mcGenFlg is sent as "Y" or "N" but returned as a boolean. Items use productId and quantity in the request but prCode and qty in the response. 

**Please check:** every endpoint with a POST or PUT body against its GET response. 

V3 API Common Issues  ·  Page 4 

