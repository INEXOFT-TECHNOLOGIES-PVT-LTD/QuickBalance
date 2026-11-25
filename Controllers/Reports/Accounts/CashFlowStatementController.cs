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

    [Route("api/v1/reports/cash-flow-statement")]
    [Authorize]
    public class CashFlowStatementController : ControllerBase
    {

        private readonly DatabaseConnection _databaseConnection;
        public CashFlowStatementController(DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }

        [HttpPost]
        public async Task<IActionResult> GetCashFlowStatement(CashFlowStatementRequest request)
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
                //string connectionString = databaseConnection.GetConnectionString(Variables);

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

                GrpLdgDB GrpLdgDB = new GrpLdgDB(DBConn);
                GrpLdgDB.Variables = Variables;
                GrpLdgDB.Functions.Variables = Variables;

                //-- Get Report

                DataTable dtlsWithAcc = new DataTable();
                decimal OpenBal = 0;

                DataTable dtDT = new DataTable();
                dtDT.Columns.Add("GRP_ID1");
                dtDT.Columns.Add("GRP_NAME1");
                dtDT.Columns.Add("GRP_AMNT1");
                dtDT.Columns.Add("GRP_ID2");
                dtDT.Columns.Add("GRP_NAME2");
                dtDT.Columns.Add("GRP_AMNT2");

                DataTable dtMN = new DataTable();
                dtMN.Columns.Add("MN_ID");
                dtMN.Columns.Add("MN_NAME");
                dtMN.Columns.Add("MN_OPEN");
                dtMN.Columns.Add("MN_AMT1");
                dtMN.Columns.Add("MN_AMT2");
                dtMN.Columns.Add("MN_CL");

                DataTable dtMonth = new DataTable();
                dtMonth.Columns.Add("ID", typeof(int));
                dtMonth.Columns.Add("FROMDATE", typeof(DateTime));
                dtMonth.Columns.Add("TODATE", typeof(DateTime));
                dtMonth.Columns.Add("MONTH", typeof(string));                

                DateTime StartDate = new DateTime(Variables.fyStartDate.Year, Variables.fyStartDate.Month, 1);
                DateTime EndDate = new DateTime(Variables.fyEndDate.Year, Variables.fyEndDate.Month, 1);
                int ID = 1;
                int ActualFromMonth = request.FromMonth;
                int ActualToMonth = request.ToMonth;
                if (!request.DateWise)
                {
                    try
                    {
                        while (StartDate <= EndDate)
                        {
                            if (StartDate.Month == request.FromMonth)
                                ActualFromMonth = ID;

                            if (StartDate.Month == request.ToMonth)
                                ActualToMonth = ID;

                            if (StartDate.Month == 12)
                            {
                                dtMonth.Rows.Add(ID, StartDate, new DateTime(StartDate.Year + 1, 1, 1).AddDays(-1), StartDate.ToString("MMMM yyyy"));
                                StartDate = new DateTime(StartDate.Year + 1, 1, 1);
                            }
                            else
                            {
                                dtMonth.Rows.Add(ID, StartDate, new DateTime(StartDate.Year, StartDate.Month + 1, 1).AddDays(-1), StartDate.ToString("MMMM yyyy"));
                                StartDate = new DateTime(StartDate.Year, StartDate.Month + 1, 1);
                            }
                            ID++;
                        }
                    }
                    catch { }
                }                

                clsAccMas.LedgerGroupId = 4;
                DataTable dtCashLedgers = await accMasDB.AccountsByGroupAll(clsAccMas, Unapproved: request.IncludeUnapproved);
                foreach (DataRow drCashLedgers in dtCashLedgers.Rows)
                    OpenBal += await accMasDB.Opening(Convert.ToInt32(drCashLedgers["ID"]));
                OpenBal = -1 * OpenBal;

                DateTime cStartDate = request.FromDate;
                DateTime cEndDate = request.ToDate;
                if (!request.DateWise)
                {
                    cStartDate = Convert.ToDateTime(dtMonth.Select("ID ='" + ActualFromMonth + "'")[0]["FROMDATE"]);
                    cEndDate = Convert.ToDateTime(dtMonth.Select("ID ='" + ActualToMonth + "'")[0]["TODATE"]);
                }

                if (cStartDate > Variables.fyStartDate)
                {
                    int index = 0;
                    int LastIndex = dtCashLedgers.Rows.Count - 1;
                    foreach (DataRow drCashLedgers in dtCashLedgers.Rows)
                    {
                        OpenBal += await dayBookDB.SelectedCashOpening(cStartDate.AddDays(-1).ToString(Variables.format), Convert.ToInt32(drCashLedgers["ID"]), true, true, request.IncludeUnapproved);
                        index++;
                    }
                }

                string OpeningAmount = Math.Abs(OpenBal).ToString(Variables.DecString) + " " + (OpenBal > 0 ? "Dr" : OpenBal < 0 ? "Cr" : "");

                await dayBookDB.CreateTable(cStartDate.ToString(Variables.format), cEndDate.ToString(Variables.format), "DB", false, StkIssRecAdj: true, Unapproved: request.IncludeUnapproved);
                await dayBookDB.CreateTable_ManageJournalEntry();

                if (request.DateWise)
                {
                    DataTable dtR = await dayBookDB.GetCashFlowDetails_DateWise();
                    dtlsWithAcc = await dayBookDB.GetCashFlowDetailsWithAccount();

                    if (dtR.Rows.Count > 0)
                    {
                        if (!dtR.Columns.Contains("slno"))
                            dtR.Columns.Add("slno", typeof(int));
                        for (int i = 0; i < dtR.Rows.Count; i++)
                            dtR.Rows[i]["slno"] = i;

                        DataRow[] receipts = dtR.Select("RECEIPT <> 0");
                        if (receipts.Length > 0)
                            receipts = receipts.OrderBy(r => (int)r["slno"]).ToArray();

                        DataRow[] payments = dtR.Select("PAYMENT <> 0");
                        if (receipts.Length > 0)
                            payments = payments.OrderBy(r => (int)r["slno"]).ToArray();

                        int maxRows = Math.Max(receipts.Length, payments.Length);
                        decimal ClosingAmount = OpenBal;
                        for (int i = 0; i < maxRows; i++)
                        {
                            var row = dtDT.NewRow();

                            if (i < receipts.Length)
                            {
                                row["GRP_ID1"] = receipts[i]["GROUPID"].ToString();
                                row["GRP_NAME1"] = receipts[i]["GROUPNAME"];
                                row["GRP_AMNT1"] = Convert.ToDecimal(receipts[i]["RECEIPT"]).ToString(Variables.DecString);

                                ClosingAmount += Convert.ToDecimal(receipts[i]["RECEIPT"]);
                            }

                            if (i < payments.Length)
                            {
                                row["GRP_ID2"] = payments[i]["GROUPID"].ToString();
                                row["GRP_NAME2"] = payments[i]["GROUPNAME"];
                                row["GRP_AMNT2"] = Convert.ToDecimal(payments[i]["PAYMENT"]).ToString(Variables.DecString);

                                ClosingAmount -= Convert.ToDecimal(payments[i]["PAYMENT"]);
                            }
                            dtDT.Rows.Add(row);
                        }

                        dtDT.Rows.Add();
                        dtDT.Rows.Add("", "", "", "", LngTB.Closing_Amount, Math.Abs(ClosingAmount).ToString(Variables.DecString) + " " + (ClosingAmount > 0 ? "Dr" : ClosingAmount < 0 ? "Cr" : ""));
                    }
                }
                else
                {
                    ID = ActualFromMonth;
                    decimal monthOpenBal = OpenBal;
                    decimal monthClosingBal = OpenBal;
                    while (ID <= ActualToMonth)
                    {
                        string monthName = Convert.ToDateTime(dtMonth.Select("ID ='" + ID + "'")[0]["FROMDATE"]).ToString("MMMM");
                        string fromDate = Convert.ToDateTime(dtMonth.Select("ID ='" + ID + "'")[0]["FROMDATE"]).ToString(Variables.format);
                        string toDate = Convert.ToDateTime(dtMonth.Select("ID ='" + ID + "'")[0]["TODATE"]).ToString(Variables.format);
                        decimal rec = 0, pay = 0;
                        DataTable dtls = await dayBookDB.GetCashFlowDetails_MonthWise(fromDate, toDate);
                        if (dtls.Rows.Count > 0)
                        {
                            rec = dtls.Rows[0]["RECEIPT"].ToString() != "" ? Convert.ToDecimal(dtls.Rows[0]["RECEIPT"]) : 0;
                            pay = dtls.Rows[0]["PAYMENT"].ToString() != "" ? Convert.ToDecimal(dtls.Rows[0]["PAYMENT"]) : 0;
                        }
                        monthClosingBal += (rec - pay);
                        dtMN.Rows.Add(ID.ToString(), monthName, Math.Abs(monthOpenBal).ToString(Variables.DecString) + " " + (monthOpenBal > 0 ? "Dr" : monthOpenBal < 0 ? "Cr" : ""), rec.ToString(Variables.DecString), pay.ToString(Variables.DecString), Math.Abs(monthClosingBal).ToString(Variables.DecString) + " " + (monthClosingBal > 0 ? "Dr" : monthClosingBal < 0 ? "Cr" : ""));
                        monthOpenBal = monthClosingBal;
                        ID++;
                    }
                }

                //--

                var tableData = (request.DateWise ? dtDT : dtMN).AsEnumerable().Select(row => (request.DateWise ? dtDT : dtMN).Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).ToList();
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
    }
}