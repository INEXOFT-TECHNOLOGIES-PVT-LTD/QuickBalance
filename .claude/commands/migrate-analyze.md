---
description: Analyze a legacy WinForms module and plan its Web API migration
argument-hint: <ModuleName>
---
Analyze the **$ARGUMENTS** module of the legacy WinForms app. Do NOT modify any code.
Write all output to docs/migration/$ARGUMENTS/.

## Phase 1 – Discovery (targeted, do not read files blindly)
1. Grep the legacy project and DB scripts for $ARGUMENTS-related terms (form names, class names,
   table names, SP names, menu items, e.g. "Sale", "Invoice", "frmSales", "usp_Sales").
2. Follow references outward: forms → event handlers → helper/BLL/DAL classes → SPs/SQL → tables.
3. For large files, record only the relevant methods and line ranges.
4. Write `01-file-map.md`: a table of file | class | method/event handler | line range | purpose,
   plus a list of SPs, inline SQL, and tables touched.
5. STOP and ask me to review the file map before continuing.

## Phase 2 – Legacy analysis → `02-legacy-analysis.md`
Using only the mapped locations, document:
- End-to-end workflow (screen load → data entry → save/update/delete/print), with a Mermaid sequence diagram
- Every validation rule (field, condition, message, where it lives)
- Every calculation (totals, tax, discount, rounding, stock, ledger effects), with exact formulas
- Insert/update/delete operations, transaction boundaries, and the order of DB writes
- Stored procedures and inline SQL: parameters, logic summary, tables affected
- Tables and columns used, and their relationships
- Dependencies on other modules (Stock, Accounts, Customer, etc.)
- Hidden business rules, edge cases, and suspected bugs, cited by file:line

## Phase 3 – Gap analysis → `03-gap-analysis.md`
Compare against the current Web API: existing entities, endpoints, services, repositories, and
validations. Produce a table: Feature | Legacy location | Web API status (Done / Partial / Missing) | Notes.

## Phase 4 – Implementation plan → `04-implementation-plan.md`
A step-by-step plan that follows the existing Web API conventions: entities/DTOs, repositories,
services, business rules, endpoints, transactions, validation, tests, and the order of work.
Flag open questions that need my decision.
