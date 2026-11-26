/* =====================================================================
   test-lookup-queries.sql  -  READ-ONLY lookup queries for POST testing
   =====================================================================
   Purpose : lists the valid ids a POST body must reference (group,
             category, brand, unit, account, ...). Copy the id into the
             body in docs/test-post-bodies.md (the Qnn numbers match).

   Rules   : SELECT statements only. Nothing here changes data.
             Run Q01-Q22 in the COMPANY database (COMP<code>_<yyyy>_<yyyy>).
             Run Q23-Q24 in MasterDB (countries and regions live there).

   Source  : table and column names are from
             DB\ProjectDB\QuickBalance.sql and the repository SQL.
             The filters mirror what the API itself accepts, so an id
             listed here will pass the API's "does it exist" check.

   Delete flags (differ per table, all respected below):
       DELFLG   : BRANDMAS, CATGMAS, PRGRPMAS, PRUNITMAS, TAXMAS, GDWGRP,
                  GDWMAS, SMGRP, SMMAST, CURRENCYMAS, PRICECATMAS,
                  ACCMAS, SUPMAS, PRMAST
       GDELFLG  : GRPLDG   (active flag is GACTIVE)
       ISDELETED: PARTYGROUPMAS, BOMMAS
       none     : COUNTRY, REGION (ACTIVE = 0 is the "deleted" state)
   ===================================================================== */


/* =====================================================================
   A. ITEM MASTER LOOKUPS   (company database)
   ===================================================================== */

-- Q01  Units
-- Products.unit, Products.altUnit, Products.aliases[].unitId,
-- Products.aliases[].invUnitCode, UnitConversions.mainUnit,
-- UnitConversions.subUnit, Boms.unit, Boms.items[].unitId
SELECT PRUNITID AS id, PRUNITNAME AS name
FROM dbo.PRUNITMAS
WHERE ACTIVE = 1 AND (DELFLG = 0 OR DELFLG IS NULL)
ORDER BY PRUNITNAME;

-- Q02  Brands
-- Products.brandId   (BRANDMAS has no ACTIVE column, only DELFLG)
SELECT ID AS id, BRANDNAME AS name
FROM dbo.BRANDMAS
WHERE (DELFLG = 0 OR DELFLG IS NULL)
ORDER BY BRANDNAME;

-- Q03  Item categories
-- Products.categoryId
SELECT CATGCODE AS id, CATGNAME AS name
FROM dbo.CATGMAS
WHERE ACTIVE = 1 AND (DELFLG = 0 OR DELFLG IS NULL)
ORDER BY CATGNAME;

-- Q04  Item groups (all)
-- Products.groupId
SELECT PRGRPID AS id, GRPNAME AS name, GRPCODE AS code,
       PRIMGRPFLG AS isPrimaryGroup
FROM dbo.PRGRPMAS
WHERE ACTIVE = 1 AND (DELFLG = 0 OR DELFLG IS NULL)
ORDER BY GRPNAME;

-- Q05  Item groups (primary groups only)
-- ItemGroup.primaryGroupId  (needed when isPrimaryGroup = false)
SELECT PRGRPID AS id, GRPNAME AS name, GRPCODE AS code
FROM dbo.PRGRPMAS
WHERE ACTIVE = 1 AND (DELFLG = 0 OR DELFLG IS NULL)
  AND PRIMGRPFLG = 1
ORDER BY GRPNAME;

-- Q06  Taxes
-- Products.taxCatId
-- (the Item Master lookup hides tax types E = Exempted and UR = Unregistered)
SELECT CODE AS id, TAXNAME AS name, TAXPERC AS percentage, TTYPE AS taxType
FROM dbo.TAXMAS
WHERE ACTIVE = 1 AND (DELFLG = 0 OR DELFLG IS NULL)
  AND ISNULL(TTYPE, 'T') NOT IN ('E', 'UR')
ORDER BY TAXNAME;

-- Q07  Price categories
-- Products.priceCatId   (one row per level in the table, so DISTINCT)
SELECT DISTINCT CODE AS id, CATNAME AS name
FROM dbo.PRICECATMAS
WHERE (DELFLG = 0 OR DELFLG IS NULL)
ORDER BY CATNAME;

-- Q08  Suppliers
-- Products.supplierId
SELECT ID AS id, SUPNAME AS name
FROM dbo.SUPMAS
WHERE ACTIVE = 1 AND (DELFLG = 0 OR DELFLG IS NULL)
ORDER BY SUPNAME;

-- Q09  Locations (godowns)
-- Products.gdwnId  (request-only field, not returned by GET)
SELECT GDWID AS id, GDWNAME AS name
FROM dbo.GDWMAS
WHERE ACTIVE = 1 AND (DELFLG = 0 OR DELFLG IS NULL)
ORDER BY GDWNAME;

-- Q10  Products
-- Boms.prCode (finished product), Boms.items[].productId
SELECT PRCODE AS id, PRNAME AS name, ALIAS AS alias
FROM dbo.PRMAST
WHERE ACTIVE = 1 AND (DELFLG = 0 OR DELFLG IS NULL)
ORDER BY PRNAME;


/* =====================================================================
   B. ACCOUNTING LOOKUPS   (company database)
   ===================================================================== */

-- Q11  Ledger groups (all)
-- LedgerGroups.mainGroupId (needed when mainGroup = false),
-- Accounts.groupId
SELECT GROUPID AS id, GROUPNAME AS name, GROUPCODE AS code,
       MAINGROUP AS isMainGroup, MGROUP AS parentGroupId,
       GROUPNATURE AS finalAccount, CRDR AS crDr
FROM dbo.GRPLDG
WHERE GACTIVE = 1 AND (GDELFLG = 0 OR GDELFLG IS NULL)
ORDER BY GROUPCODE;

-- Q12  Ledger groups allowed for customers
-- Customers.ledgerGroupId  (same query as GET /customers/lookups/ledger-groups)
SELECT GROUPID AS id, GROUPNAME AS name
FROM dbo.GRPLDG
WHERE GACTIVE = 1 AND (GDELFLG = 0 OR GDELFLG IS NULL)
  AND (GROUPID = 26 OR MGROUP = 26)
ORDER BY GROUPNAME;

-- Q13  Ledger groups allowed for suppliers
-- Suppliers.ledgerGroupId  (same query as GET /suppliers/lookups/ledger-groups).
-- Note: on create the API always uses ledger group 27 and ignores this value.
SELECT GROUPID AS id, GROUPNAME AS name
FROM dbo.GRPLDG
WHERE GACTIVE = 1 AND (GDELFLG = 0 OR GDELFLG IS NULL)
  AND (GROUPID = 27 OR MGROUP = 33)
ORDER BY GROUPNAME;

-- Q14  Accounts (ledgers)
-- Taxes.purchaseAccountId, purchaseReturnAccountId, salesAccountId,
--       salesReturnAccountId, vatAccountId, taxAccountId
-- ItemGroup.stockAccountId, purchaseAccountId, salesAccountId
-- Locations.stockAccountId
-- Salesman.accountId (when connectWithAccount = true)
-- Products.saleAccId, Products.purAcctId
-- Boms.mcGen, Boms.mcCons, Boms.additionalInfo[].accountId
SELECT A.ID AS id, A.ACCNAME AS name, A.ACCODE AS accountCode,
       G.GROUPNAME AS ledgerGroup
FROM dbo.ACCMAS A
LEFT JOIN dbo.GRPLDG G ON G.GROUPID = A.MGROUP
WHERE A.ACTIVE = 1 AND (A.DELFLG = 0 OR A.DELFLG IS NULL)
ORDER BY A.ACCNAME;


/* =====================================================================
   C. PEOPLE AND GROUP LOOKUPS   (company database)
   ===================================================================== */

-- Q15  Salesman groups
-- Salesman.salesmanGroupId
-- Also what the Customers.salesManId check reads (see note in
-- docs/test-post-bodies.md, Customers).
SELECT SMGRPCODE AS id, SMGRPNAME AS name
FROM dbo.SMGRP
WHERE ACTIVE = 1 AND (DELFLG = 0 OR DELFLG IS NULL)
ORDER BY SMGRPNAME;

-- Q16  Salesmen
-- Customers.salesManId  (same query as GET /customers/lookups/salesman,
-- plus the delete flag)
SELECT SMCODE AS id, SMNAME AS name, SMGRPCODE AS salesmanGroupId
FROM dbo.SMMAST
WHERE ACTIVE = 1 AND (DELFLG = 0 OR DELFLG IS NULL)
ORDER BY SMNAME;

-- Q17  Party groups for customers
-- Customers.partyGrpId  (0 = none; GROUPTYPE 'C' = Customer)
SELECT ID AS id, GROUPNAME AS name, GROUPSNAME AS shortName
FROM dbo.PARTYGROUPMAS
WHERE (ISDELETED = 0 OR ISDELETED IS NULL)
  AND UPPER(LTRIM(RTRIM(ISNULL(GROUPTYPE, '')))) = 'C'
ORDER BY GROUPNAME;

-- Q18  Party groups for suppliers
-- Suppliers.partyGrpId  (0 = none; GROUPTYPE 'S' = Supplier)
SELECT ID AS id, GROUPNAME AS name, GROUPSNAME AS shortName
FROM dbo.PARTYGROUPMAS
WHERE (ISDELETED = 0 OR ISDELETED IS NULL)
  AND UPPER(LTRIM(RTRIM(ISNULL(GROUPTYPE, '')))) = 'S'
ORDER BY GROUPNAME;

-- Q19  Location groups
-- Locations.groupId
SELECT GRPID AS id, GRPNAME AS name
FROM dbo.GDWGRP
WHERE ACTIVE = 1 AND (DELFLG = 0 OR DELFLG IS NULL)
ORDER BY GRPNAME;


/* =====================================================================
   D. CURRENCY LOOKUPS   (company database)
   ===================================================================== */

-- Q20  Currencies (all)
-- Accounts.currencyCode (when currencyFlag = true),
-- Customers.curCode, Suppliers.curCode (0 = none),
-- Products.currencyId
SELECT CODE AS id, SYMBOL AS symbol, CURRSTRING AS name, BASEFLG AS isBase
FROM dbo.CURRENCYMAS
WHERE (DELFLG = 0 OR DELFLG IS NULL)
ORDER BY SYMBOL;

-- Q21  Base currency
-- CurrencyConversions: this one must NOT be used in details[].currencyId
SELECT CODE AS id, SYMBOL AS symbol, CURRSTRING AS name
FROM dbo.CURRENCYMAS
WHERE BASEFLG = 1 AND (DELFLG = 0 OR DELFLG IS NULL);

-- Q22  Alternate (non-base) currencies
-- CurrencyConversions.details[].currencyId
SELECT CODE AS id, SYMBOL AS symbol, CURRSTRING AS name
FROM dbo.CURRENCYMAS
WHERE (BASEFLG = 0 OR BASEFLG IS NULL) AND (DELFLG = 0 OR DELFLG IS NULL)
ORDER BY SYMBOL;


/* =====================================================================
   E. COUNTRY AND REGION LOOKUPS   >>> RUN IN MasterDB <<<
      Switch the database selector in SSMS to MasterDB first.
      These tables are shared by every company. Do not create test
      countries or regions on a shared server without asking.
   ===================================================================== */

-- Q23  Countries
-- Regions.countryId, Customers.countryCode, Suppliers.countryCode
SELECT COUNTRYID AS id, COUNTRYNAME AS name
FROM dbo.COUNTRY
WHERE ISNULL(ACTIVE, 1) = 1
ORDER BY COUNTRYNAME;

-- Q24  Regions (with their country)
-- Customers.stateCode, Suppliers.stateCode
-- The region must belong to the country sent in countryCode.
SELECT R.REGIONID AS id, R.REGIONNAME AS name,
       R.COUNTRYID AS countryId, C.COUNTRYNAME AS countryName
FROM dbo.REGION R
LEFT JOIN dbo.COUNTRY C ON C.COUNTRYID = R.COUNTRYID
WHERE ISNULL(R.ACTIVE, 1) = 1
ORDER BY C.COUNTRYNAME, R.REGIONNAME;


/* =====================================================================
   F. FIELD -> QUERY INDEX
   ---------------------------------------------------------------------
   Controller        Field                                   Query
   ----------------  --------------------------------------  ---------
   Brands            (none)                                  -
   Units             (none)                                  -
   ItemCategories    (none)                                  -
   PartyGroups       (none)                                  -
   PriceCategories   (none)                                  -
   SalesmanGroups    (none)                                  -
   LocationGroups    (none)                                  -
   Currencies        (none)                                  -
   Countries         (none)                                  -
   LedgerGroups      mainGroupId                             Q11
   UnitConversions   mainUnit, subUnit                       Q01
   Regions           countryId                               Q23 (MasterDB)
   CurrencyConv.     details[].currencyId                    Q22 (Q21 = excluded)
   Accounts          groupId                                 Q11
                     currencyCode                            Q20
   Taxes             purchaseAccountId, purchaseReturn...,   Q14
                     salesAccountId, salesReturn...,
                     vatAccountId, taxAccountId
   ItemGroup         primaryGroupId                          Q05
                     stockAccountId, purchaseAccountId,      Q14
                     salesAccountId
   Locations         groupId                                 Q19
                     stockAccountId                          Q14
   Salesman          salesmanGroupId                         Q15
                     accountId                               Q14
   Suppliers         ledgerGroupId                           Q13
                     countryCode / stateCode                 Q23 / Q24 (MasterDB)
                     partyGrpId                              Q18
                     curCode                                 Q20
   Customers         ledgerGroupId                           Q12
                     countryCode / stateCode                 Q23 / Q24 (MasterDB)
                     partyGrpId                              Q17
                     salesManId                              Q16 (see Q15 note)
                     curCode                                 Q20
   Products          groupId                                 Q04
                     categoryId                              Q03
                     brandId                                 Q02
                     unit, altUnit                           Q01
                     taxCatId                                Q06
                     priceCatId                              Q07
                     saleAccId, purAcctId                    Q14
                     supplierId                              Q08
                     gdwnId                                  Q09
                     currencyId                              Q20
                     aliases[].unitId, invUnitCode           Q01
   Boms              prCode, items[].productId               Q10
                     unit, items[].unitId                    Q01
                     mcGen, mcCons, additionalInfo[]...      Q14
   ===================================================================== */
