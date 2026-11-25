using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using QuickBalance.API.CLS;
using QuickBalance.API.Configuration;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using System.Data;
using System.Security.Claims;

namespace QuickBalance.API.Controllers.Reports.Accounts
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]

    [Route("api/v1/reports/cash-and-bank-book")]
    [Authorize]
    public class CashAndBankBookController : ControllerBase
    {
        private readonly DatabaseConnection _databaseConnection;
        public CashAndBankBookController(DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }
        [HttpPost]
        public async Task<IActionResult> GetCashAndBankBook(CashAndBankBookRequest request)
        {
            try
            {
                LngTB.InitLng();
                Variable Variables = new Variable();
                Variables.VoucherTypeCodes = request.VoucherTypeCodes;

                //--

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { status = false, message = "User ID not found in token.", errors = new List<object>() });
                Variables.UserId = int.Parse(userIdClaim.Value);

                //--

                string connectionString = _databaseConnection.GetConnectionString(Variables);
                //DBConn db = new DBConn(connectionString);
                DBConn DBConn = new DBConn(connectionString);
                CommonFunction commonFunction = new CommonFunction(DBConn);

                if (!await commonFunction.CheckDatabaseConnection())
                    return StatusCode(500, new { status = false, message = "Database connection failed.", errors = new List<object>() });

                //--

                await commonFunction.SetVariableValues(Variables);
                commonFunction.Variables = Variables;
                commonFunction.Functions.Variables = Variables;

                DayBookDB dayBookDB = new DayBookDB(DBConn);
                dayBookDB.Variables = Variables;
                dayBookDB.Functions.Variables = Variables;

                AccMasDB accMasDB = new AccMasDB(DBConn);
                accMasDB.Variables = Variables;
                accMasDB.Functions.Variables = Variables;
                ClsAccMas clsAccMas = new ClsAccMas();

                Function Functions = new Function();
                Functions.Variables = Variables;

                //-- Get Report

                if (request.FromDate.Date > Variables.fyStartDate)
                    await dayBookDB.CreateTable(Variables.fyStartDate.ToString(Variables.format), request.FromDate.AddDays(-1).ToString(Variables.format), "OP", Unapproved: request.IncludeUnapproved);
                else
                    await dayBookDB.CreateTable(Variables.fyStartDate.AddDays(-1).ToString(Variables.format), request.FromDate.AddDays(-1).ToString(Variables.format), "OP", Unapproved: request.IncludeUnapproved);

                await dayBookDB.CreateTable(request.FromDate.ToString(Variables.format), request.ToDate.ToString(Variables.format), "DB", Unapproved: request.IncludeUnapproved);

                DataTable dtLedger = await accMasDB.GetCashOrBankLedgers(request.IncludeUnapproved);
                DataTable DtR = new DataTable();
                foreach (DataRow drLedger in dtLedger.Rows)
                {
                    decimal OpenBal = Convert.ToDecimal(drLedger["OPENBAL"].ToString() == "" ? "0" : drLedger["OPENBAL"].ToString());
                    DataTable Dt;

                    if (Convert.ToInt32(drLedger["ID"]) == Variables.CashCD)
                    {
                        OpenBal += await dayBookDB.CashLedgerBalance(Convert.ToInt32(drLedger["ID"]));
                        Dt = await dayBookDB.CashLedgerGrpByAcc(Convert.ToInt32(drLedger["ID"]));
                    }
                    else
                    {
                        OpenBal += await dayBookDB.LedgerBalance(Convert.ToInt32(drLedger["ID"]));
                        Dt = await dayBookDB.LedgerGrpByAcc(Convert.ToInt32(drLedger["ID"]));
                    }

                    if (DtR.Columns.Count == 0)
                        DtR = Dt.Copy();
                    else if (Dt.Rows.Count > 0)
                        DtR.Merge(Dt, true, MissingSchemaAction.Ignore);

                    drLedger["OPENBAL"] = OpenBal.ToString(Variables.DecString);
                }

                List<string> ls = new List<string>();
                foreach (DataRow item in dtLedger.Rows)
                    if (Convert.ToDecimal(item["OPENBAL"]) != 0 || DtR.Select("ACCID = '" + item["ID"] + "'").Length > 0)
                        ls.Add(item["ID"].ToString());

                DataTable dt = new DataTable();
                dt.Columns.Add("R_Particular");
                dt.Columns.Add("R_Type");
                foreach (string item in ls)
                    dt.Columns.Add("R_" + item);
                dt.Columns.Add("R_GRPNAME");

                dt.Columns.Add("P_Particular");
                dt.Columns.Add("P_Type");
                foreach (string item in ls)
                    dt.Columns.Add("P_" + item);
                dt.Columns.Add("P_GRPNAME");

                DataTable dtOpn = dt.Clone();
                dtOpn.Rows.Add(LngTB.Opening_Balance);
                foreach (DataRow drLedger in dtLedger.Rows)
                {
                    decimal OpenBal = Function.GetDec(drLedger["OPENBAL"]);
                    if (OpenBal < 0)
                        dtOpn.Rows[0]["R_" + drLedger["ID"].ToString()] = (-1 * OpenBal).ToString(Variables.DecString);
                    else if (OpenBal > 0)
                        dtOpn.Rows[0]["P_" + drLedger["ID"].ToString()] = OpenBal.ToString(Variables.DecString);
                }

                HashSet<string> AllAccounts = new HashSet<string>();
                foreach (DataRow row in DtR.Rows)
                    AllAccounts.Add(row["ACCOUNT"].ToString() + "#@ASK@#" + row["VRTYPE"].ToString() + "#@ASK@#" + row["GRPNAME"].ToString());

                int R_index = 0;
                int P_index = 0;
                foreach (string account in AllAccounts.OrderBy(n => n))
                {
                    string[] parts = account.Split(new[] { "#@ASK@#" }, StringSplitOptions.None);
                    string accName = parts[0];
                    string vrType = parts[1];
                    string grpName = parts[2];

                    DataRow[] DR = DtR.Select($"ACCOUNT = '{accName.Replace("'", "''")}' AND VRTYPE = '{vrType.Replace("'", "''")}' AND GRPNAME = '{grpName.Replace("'", "''")}'");
                    foreach (DataRow item in DR)
                    {
                        decimal debit = Function.GetDec(item["Debit"]);
                        decimal credit = Function.GetDec(item["Credit"]);

                        if (debit != 0)
                        {
                            if (dt.Rows.Count - 1 < R_index)
                                dt.Rows.Add();

                            dt.Rows[R_index]["R_Particular"] = accName;
                            dt.Rows[R_index]["R_Type"] = vrType;
                            dt.Rows[R_index]["R_" + item["ACCID"].ToString()] = debit.ToString(Variables.DecString);
                            dt.Rows[R_index]["R_GRPNAME"] = (request.Summary ? "" : "♦ ") + grpName;
                            R_index++;
                        }

                        if (credit != 0)
                        {
                            if (dt.Rows.Count - 1 < P_index)
                                dt.Rows.Add();

                            dt.Rows[P_index]["P_Particular"] = accName;
                            dt.Rows[P_index]["P_Type"] = vrType;
                            dt.Rows[P_index]["P_" + item["ACCID"].ToString()] = credit.ToString(Variables.DecString);
                            dt.Rows[P_index]["P_GRPNAME"] = (request.Summary ? "" : "♦ ") + grpName;
                            P_index++;
                        }
                    }
                }

                // Order By Ledger Group - 31_03_2026
                List<string> r_grp = dt.AsEnumerable().Select(row => row.Field<string>("R_GRPNAME")).Where(val => !string.IsNullOrWhiteSpace(val)).Distinct().OrderBy(val => val).ToList();
                List<string> p_grp = dt.AsEnumerable().Select(row => row.Field<string>("P_GRPNAME")).Where(val => !string.IsNullOrWhiteSpace(val)).Distinct().OrderBy(val => val).ToList();

                string[] skipColumns = { "R_Particular", "R_Type", "R_GRPNAME", "P_Particular", "P_Type", "P_GRPNAME" };
                var rColumns = dt.Columns.Cast<DataColumn>().Where(c => c.ColumnName.StartsWith("R_") && !skipColumns.Contains(c.ColumnName)).ToList();
                var pColumns = dt.Columns.Cast<DataColumn>().Where(c => c.ColumnName.StartsWith("P_") && !skipColumns.Contains(c.ColumnName)).ToList();

                List<DataRow> leftRows = new List<DataRow>();
                foreach (string item in r_grp)
                {
                    DataRow[] groupData = dt.Select("R_GRPNAME = '" + item.Replace("'", "''") + "'");
                    DataRow header = dt.NewRow();
                    header["R_Particular"] = item;
                    header["R_Type"] = "-";

                    foreach (var col in rColumns)
                    {
                        decimal columnSum = 0;
                        foreach (DataRow dr in groupData)
                            columnSum += Function.GetDec(dr[col.ColumnName]);
                        header[col.ColumnName] = columnSum == 0 ? "" : columnSum.ToString(Variables.DecString);
                    }
                    leftRows.Add(header);

                    if (!request.Summary)
                        foreach (DataRow row in groupData) leftRows.Add(row);
                }


                List<DataRow> rightRows = new List<DataRow>();
                foreach (string item in p_grp)
                {
                    DataRow[] groupData = dt.Select("P_GRPNAME = '" + item.Replace("'", "''") + "'");
                    DataRow header = dt.NewRow();
                    header["P_Particular"] = item;
                    header["P_Type"] = "-";

                    foreach (var col in pColumns)
                    {
                        decimal columnSum = 0;
                        foreach (DataRow dr in groupData)
                            columnSum += Function.GetDec(dr[col.ColumnName]);
                        header[col.ColumnName] = columnSum == 0 ? "" : columnSum.ToString(Variables.DecString);
                    }
                    rightRows.Add(header);

                    if (!request.Summary)
                        foreach (DataRow row in groupData) rightRows.Add(row);
                }

                DataTable dtGrp = dt.Clone();
                int maxCount = Math.Max(leftRows.Count, rightRows.Count);
                for (int i = 0; i < maxCount; i++)
                {
                    DataRow newRow = dtGrp.NewRow();
                    foreach (DataColumn col in dtGrp.Columns)
                    {
                        string colName = col.ColumnName;
                        if (colName.StartsWith("R_", StringComparison.OrdinalIgnoreCase))
                        {
                            if (i < leftRows.Count)
                                newRow[colName] = leftRows[i][colName];
                        }

                        else if (colName.StartsWith("P_", StringComparison.OrdinalIgnoreCase))
                        {
                            if (i < rightRows.Count)
                                newRow[colName] = rightRows[i][colName];
                        }
                    }
                    dtGrp.Rows.Add(newRow);
                }
                dt = dtGrp;
                // End Order By Ledger Group

                // Opening Balance
                DataRow drOpn = dtOpn.Rows[0];
                DataRow topRow = dt.NewRow();
                topRow.ItemArray = drOpn.ItemArray;
                dt.Rows.InsertAt(topRow, 0);
                dt.Columns.Remove("R_GRPNAME");
                dt.Columns.Remove("P_GRPNAME");
                // End Opening Balance

                // Closing Balance
                if (dt.Rows.Count > 0)
                {
                    dt.Rows.Add(LngTB.Total);
                    DataRow drTot = dt.Rows[dt.Rows.Count - 1];

                    dt.Rows.Add(LngTB.Closing_Balance);
                    DataRow drCB = dt.Rows[dt.Rows.Count - 1];

                    foreach (DataRow drLedger in dtLedger.Rows)
                    {
                        decimal R_Total = 0;
                        decimal P_Total = 0;

                        string R_ColName = "R_" + drLedger["ID"].ToString();
                        string P_ColName = "P_" + drLedger["ID"].ToString();

                        bool R_ColStat = dt.Columns.Contains(R_ColName);
                        bool P_ColStat = dt.Columns.Contains(P_ColName);
                        foreach (DataRow dr in dt.Rows)
                        {
                            if (R_ColStat && (request.Summary || dr["R_type"].ToString() != "-"))
                                R_Total += Function.GetDec(dr["R_" + drLedger["ID"].ToString()]);

                            if (P_ColStat && (request.Summary || dr["P_type"].ToString() != "-"))
                                P_Total += Function.GetDec(dr["P_" + drLedger["ID"].ToString()]);
                        }

                        if (R_ColStat && R_Total > 0)
                            drTot["R_" + drLedger["ID"].ToString()] = R_Total.ToString(Variables.DecString);

                        if (P_ColStat && P_Total > 0)
                            drTot["P_" + drLedger["ID"].ToString()] = P_Total.ToString(Variables.DecString);

                        if (R_Total - P_Total > 0)
                            drCB["R_" + drLedger["ID"].ToString()] = (R_Total - P_Total).ToString(Variables.DecString);

                        else if (P_Total - R_Total > 0)
                            drCB["P_" + drLedger["ID"].ToString()] = (P_Total - R_Total).ToString(Variables.DecString);
                    }
                }
                // End Closing Balance

                await dayBookDB.DropTable();

                //--

                var columns = dt.Columns.Cast<DataColumn>().Select(column =>
                {
                    string columnName = column.ColumnName;
                    string headerText = columnName;

                    // ----------------------------------------------------
                    // Particular
                    // ----------------------------------------------------
                    if (columnName == "R_Particular" || columnName == "P_Particular")
                        headerText = LngTB.Particulars;

                    // ----------------------------------------------------
                    // Type
                    // ----------------------------------------------------
                    else if (columnName == "R_Type" || columnName == "P_Type")
                        headerText = LngTB.Type;

                    // ----------------------------------------------------
                    // Account Columns
                    // ----------------------------------------------------
                    else
                    {
                        string accId = columnName.Replace("R_", "").Replace("P_", "");
                        DataRow[] drAcc = dtLedger.Select("ID = '" + accId.Replace("'", "''") + "'");

                        if (drAcc.Length > 0)
                            headerText = drAcc[0]["ACCNAME"].ToString();
                    }

                    return new { key = columnName, headerText = headerText };
                }
                ).ToList();

                var tableData = dt.AsEnumerable().Select(row => dt.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => { if (row[column] == DBNull.Value) return null; return row[column]; })).ToList();

                return Ok(new { status = true, columns = columns, data = tableData });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message, errors = new List<object>() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message, errors = new List<object>() });
            }
        }
    }
}