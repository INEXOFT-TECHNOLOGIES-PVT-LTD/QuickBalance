using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using QuickBalance.API.Configuration;
using QuickBalance.API.DB;
using System.Data;
using System.Security.Claims;

namespace QuickBalance.API.Controllers.Reports.Accounts
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]

    [Route("api/v1/reports/daily-closing")]
    [Authorize]
    public class DailyClosingController : ControllerBase
    {

        private readonly DatabaseConnection _databaseConnection;
        public DailyClosingController(DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }
        private class BankCashGroupResult
        {
            public string CashGrp { get; set; } = string.Empty;
            public string BankGrp { get; set; } = string.Empty;
        }

        [HttpPost]
        public async Task<IActionResult> GetDailyClosing(DailyClosingRequest request)
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

                //DatabaseConnection databaseConnection = new DatabaseConnection();

                string connectionString = _databaseConnection.GetConnectionString(Variables);
                DBConn db = new DBConn(connectionString); DBConn DBConn = new DBConn(connectionString);
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

                GrpLdgDB GrpLdgDB = new GrpLdgDB(DBConn);
                GrpLdgDB.Variables = Variables;
                GrpLdgDB.Functions.Variables = Variables;

                //-- Get Report

                DataTable dt = new DataTable();
                dt.Columns.Add("ACCID");
                dt.Columns.Add("ACCNAME");
                dt.Columns.Add("OPDR");
                dt.Columns.Add("OPCR");
                dt.Columns.Add("TRDR");
                dt.Columns.Add("TRCR");
                dt.Columns.Add("CLDR");
                dt.Columns.Add("CLCR");

                BankCashGroupResult groupResult = await GetBankOrCashLedger(GrpLdgDB, request.IncludeUnapproved);

                if (request.AsOnDate.Date > Variables.fyStartDate)
                    await dayBookDB.CreateTable(Variables.fyStartDate.ToString(Variables.format), request.AsOnDate.AddDays(-1).ToString(Variables.format), "OP", Unapproved: request.IncludeUnapproved);
                else
                    await dayBookDB.CreateTable(Variables.fyStartDate.AddDays(-1).ToString(Variables.format), request.AsOnDate.AddDays(-1).ToString(Variables.format), "OP", Unapproved: request.IncludeUnapproved);

                await dayBookDB.CreateTable(request.AsOnDate.ToString(Variables.format), request.AsOnDate.ToString(Variables.format), "DB", Unapproved: request.IncludeUnapproved);

                for (int i = 1; i <= 2; i++)
                {
                    decimal OpenBal = 0;
                    DataTable dtAcc = await accMasDB.AccountsByMulGroupRpt(i == 1 ? groupResult.CashGrp : groupResult.BankGrp, request.IncludeUnapproved);
                    foreach (DataRow dr in dtAcc.Rows)
                    {
                        if (request.AccountCode == 0 || Convert.ToInt32(dr["ID"]) == request.AccountCode)
                        {
                            if (dr["OPENBAL"] != DBNull.Value)
                                OpenBal = Convert.ToDecimal(dr["OPENBAL"]);

                            DataTable DtR;
                            if (Convert.ToInt32(dr["ID"]) == Variables.CashCD)
                            {
                                OpenBal += await dayBookDB.CashLedgerBalance(Convert.ToInt32(dr["ID"]));
                                DtR = await dayBookDB.CashLedger(Convert.ToInt32(dr["ID"]));
                            }
                            else
                            {
                                OpenBal += await dayBookDB.LedgerBalance(Convert.ToInt32(dr["ID"]));
                                DtR = await dayBookDB.Ledger(Convert.ToInt32(dr["ID"]));
                            }

                            if (DtR.Rows.Count > 0 || OpenBal != 0)
                            {
                                decimal TrCr = 0, TrDr = 0;
                                decimal Cr = 0, Dr = 0;
                                decimal bal = OpenBal;
                                if (OpenBal > 0)
                                    Cr = OpenBal;
                                else
                                    Dr = -1 * OpenBal;
                                foreach (DataRow drA in DtR.Rows)
                                {
                                    if (drA["Credit"] != DBNull.Value && !string.IsNullOrEmpty(drA["Credit"].ToString()))
                                    {
                                        TrCr += Convert.ToDecimal(drA["Credit"]);
                                        Cr += Convert.ToDecimal(drA["Credit"]);
                                    }
                                    if (drA["Debit"] != DBNull.Value && !string.IsNullOrEmpty(drA["Debit"].ToString()))
                                    {
                                        TrDr += Convert.ToDecimal(drA["Debit"]);
                                        Dr += Convert.ToDecimal(drA["Debit"]);
                                    }
                                    bal = Cr - Dr;
                                }
                                dt.Rows.Add(dr["ID"].ToString(), dr["ACCNAME"].ToString(), (OpenBal < 0 ? (-1 * OpenBal).ToString(Variables.DecString) : ""), (OpenBal > 0 ? OpenBal.ToString(Variables.DecString) : ""), (TrDr == 0 ? "" : TrDr.ToString(Variables.DecString)), (TrCr == 0 ? "" : TrCr.ToString(Variables.DecString)), (bal < 0 ? (-1 * bal).ToString(Variables.DecString) : ""), (bal > 0 ? (bal).ToString(Variables.DecString) : ""));
                            }
                        }
                    }
                }

                //--
                var tableData = dt.AsEnumerable().Select(row => dt.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).ToList();
                return Ok(new { status = true, data = tableData });
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

        private async Task<BankCashGroupResult> GetBankOrCashLedger(GrpLdgDB GrpLdgDB, bool IncludeUnapproved)
        {
            var result = new BankCashGroupResult();
            string BankGrp = "", CashGrp = "";
            try
            {
                DataTable dtGrp = await GrpLdgDB.AccGroupSeries(IncludeUnapproved);
                string idCol = "", nameCol = "";
                foreach (DataRow item in dtGrp.Rows)
                {
                    for (int i = 1; i <= dtGrp.Columns.Count / 2; i++)
                    {
                        idCol = "GRPID" + i;
                        nameCol = "GRPNAME" + i;
                        int GrpID = Convert.ToInt32(item[idCol].ToString() == "" ? "0" : item[idCol].ToString());
                        string GrpName = item[nameCol].ToString();
                        if (GrpID == 1 || GrpID == 4)
                        {
                            DataRow[] drSubGrp = dtGrp.Select(idCol + "='" + GrpID + "'");
                            if (drSubGrp.Length > 0)
                            {
                                DataTable dtSubGrp = drSubGrp.CopyToDataTable();
                                string GrpIds = "";
                                foreach (DataRow item2 in dtSubGrp.Rows)
                                {
                                    for (int j = i; j <= dtGrp.Columns.Count / 2; j++)
                                    {
                                        GrpIds += (GrpIds == "" || item2["GRPID" + j].ToString() == "" ? "" : ",") + item2["GRPID" + j].ToString();
                                    }
                                }

                                if (GrpID == 1)
                                    BankGrp = GrpIds;
                                else
                                    CashGrp = GrpIds;

                                break;
                            }
                        }
                    }
                    if (BankGrp != "" && CashGrp != "")
                        break;
                }
            }
            catch { }

            result.BankGrp = BankGrp;
            result.CashGrp = CashGrp;
            return result;
        }
    }
}