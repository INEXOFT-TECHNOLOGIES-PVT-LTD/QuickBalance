# Purchase Form: Save Workflow (legacy analysis)

**Status:** Step 1 (locate) complete. Steps 2–4 NOT YET TRACED. This file will be extended once Step 1 is confirmed.
Analysis only. No code or database objects were modified.

Tags: `[VERIFIED: file:line]` means read directly. `[ASSUMPTION]` means not confirmed in code. Anything not read is marked NOT READ.

## 1. Locations

Active legacy source: `D:\PROJECT\IFlare\Inventory\Inventory\` (the `Inventory.csproj`).
The root-level `D:\PROJECT\IFlare\Inventory\View\` and `\Model\` are an older orphaned copy and are ignored.

| Item | Path | Notes |
|---|---|---|
| Form code-behind | `Inventory\View\Purchase.cs` | about 19.8k lines, `class Purchase : Form` [VERIFIED: Purchase.cs:23] |
| Form designer | `Inventory\View\Purchase.Designer.cs` | event wiring |
| Data access | `Inventory\Model\PurmDB.cs` | about 3.2k lines, instantiated at [VERIFIED: Purchase.cs:556] |
| Other helpers | PrMastDB, CommonDB, Functions, ExceptionLogging | NOT READ, to be located in Step 2 |
| DB script | `DB\ProjectDB\QuickBalance.sql` | tables `PURM` (:3700), `PURD` (:3663) |
| Stored procedures | `ManagePurchase_Purt` (:7673), `ManagePurchaseOrder_Pordt` (:7844), `ManagePurchaseReturn_Purt` (:7937) | bodies NOT READ |

Triggers on purchase tables: none matched `PUR*` names in a first grep; to be re-checked by table name in Step 2 [ASSUMPTION].

## 2. Save handlers

| Trigger | Wiring | Handler | Notes |
|---|---|---|---|
| Save button click | [VERIFIED: Designer:694] | `btn_save_Click` [VERIFIED: Purchase.cs:6157] | body NOT READ |
| Save button KeyDown | [VERIFIED: Designer:695] | `txt_batch_KeyDown` [VERIFIED: Purchase.cs:15735] | body fully commented out, does nothing; F10 Save shortcut is inside the comment |
| Ctrl+S | `ProcessCmdKey` [VERIFIED: Purchase.cs:18945-18951] | `btn_save.PerformClick()` | only if Save is Visible and Enabled and `btn_save.Text != Variables.EditText` |
| AcceptButton | none set [VERIFIED: grep of Purchase.*] | | |

## 3. Quit / Close handlers

| Trigger | Wiring | Handler | Behaviour |
|---|---|---|---|
| Quit button click | [VERIFIED: Designer:711] | `btn_quit_Click` [VERIFIED: Purchase.cs:5836-5840] | focuses the button, then `this.Close()` |
| Quit button KeyDown | [VERIFIED: Designer:712] | `txt_batch_KeyDown` | does nothing (commented out) |
| Esc | `CancelButton = btn_quit` [VERIFIED: Designer:2556] | | triggers Quit |
| FormClosing | [VERIFIED: Designer:2657] | `Purchase_FormClosing` [VERIFIED: Purchase.cs:11612-11639] | see below |
| FormClosed | [VERIFIED: Designer:2658] | `Purchase_FormClosed` [VERIFIED: Purchase.cs:13899] | closes `docObj`, `Functions.RefreshDisplayType`, restores filter dates, disposes the form if `DisplayType == "M"` [VERIFIED: 13899-13923]; rest NOT READ |

`Purchase_FormClosing` logic [VERIFIED: Purchase.cs:11614-11638]:
- New mode (`btn_save.Text != Variables.UpdateText` and `btn_cancel` not focused): shows `CommonDB.SaveConfirm(btn_quit)`. Yes closes the form, otherwise `e.Cancel = true`.
- Update mode with `Updated == false`: shows the same prompt.
- Otherwise: closes without a prompt.
- Every closing branch calls `CommonDB.TrvShow(false)`.
- The text of the `SaveConfirm` dialog is NOT READ (it lives in `CommonDB`).

Other shortcuts in `ProcessCmdKey` [VERIFIED: Purchase.cs:18943-19075]: Ctrl+P (print), Ctrl+H, Ctrl+L, Ctrl+G, Ctrl+I, and Ctrl+A / Ctrl+M (create or modify masters). None is Save or Quit.

## 4. Save operation (Step 2): NOT YET TRACED
Validation, calculations, table writes, stored procedures and triggers, new vs edit mode, transactions, and the after-save flow are pending.

Methods located but NOT READ: `Validation` :3231, `ValidateBatch` :4310, `ValidateTaggedVoucher` :3834, `ValidateEinvoice` :16296, `SavePurD` :1598, `SavePrSt` :1824, `SaveBatchDet` :2004, `SaveSntStk` :2167, `SavePurT` :2571, `SaveAdjustment` :3187, `SaveStkDt` :1275, `SavePurt_Sql` :6116, `SaveGrnT` :14602, `SaveGrnD` :14932, `SaveDoc` :18541, `SaveEinv` :17077; `PurmDB.insert` / `update` called at :3094 / :3101.

Insert vs update is chosen by `btn_save.Text == Variables.SaveText / UpdateText` (e.g. Purchase.cs:3072, :3097) [VERIFIED by grep match only; logic NOT READ].

## 5. Pending sections
- Sequence diagram (Mermaid) of the Save flow
- Table-impact matrix
- Step 4: Web API business rules, UI-only behaviour, suspected bugs
