using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using QuickBalance.API.Configuration;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using System.Data;
using System.Security.Claims;

namespace QuickBalance.API.Controllers.Reports.Accounts
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]

    [Route("api/v1/reports/ledger-opening")]
    [Authorize]
    public class LedgerOpeningController : ControllerBase
    {
        private readonly DatabaseConnection _databaseConnection;
        public LedgerOpeningController(DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }
        [HttpPost]
        public async Task<IActionResult> GetLedgerOpening(LedgerOpeningRequest request)
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
                //string connectionString = databaseConnection.GetConnectionString(Variables);
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

                //-- Get Report

                DataTable dtRep = new DataTable();
                dtRep.Columns.Add("ID");
                dtRep.Columns.Add("ACCNAME");
                dtRep.Columns.Add("GROUPID");
                dtRep.Columns.Add("GROUPNAME");
                dtRep.Columns.Add("OPENBAL");
                dtRep.Columns.Add("BALANCE");
                dtRep.Columns.Add("TOTBAL");

                await dayBookDB.CreateTable(Variables.fyEndDate.ToString(Variables.format), request.ReportDate.AddDays(-1).ToString(Variables.format), "DB", Unapproved: request.IncludeUnapproved);

                if (request.FilterCostCentre)
                {
                    if (request.CostCentreCode <= 0)
                    {
                        return StatusCode(400, new { status = false, message = "Select Valid Cost Centre", errors = new List<object>() });
                    }
                }

                DataTable dt = await dayBookDB.OpenBalReport(request.AccountWise, (request.AccountCode == 0 && request.GroupCode == 0), request.ShowParentGroup, request.FilterCostCentre, (request.AccountWise ? request.AccountCode : request.GroupCode), request.CostCentreCode, request.ReportDate, request.IncludeUnapproved);

                foreach (DataRow dr in dt.Rows)
                {
                    decimal bal = 0;
                    if (!string.IsNullOrEmpty(dr["BALANCE"].ToString()))
                        bal += Convert.ToDecimal(dr["BALANCE"]);
                    if (!string.IsNullOrEmpty(dr["OPENBAL"].ToString()))
                        bal += Convert.ToDecimal(dr["OPENBAL"]);

                    bal = Math.Round(bal, 2);
                    if (bal >= 0)
                        dr["TOTBAL"] = bal.ToString("#,0.00") + " Cr";
                    else
                        dr["TOTBAL"] = (-1 * bal).ToString("#,0.00") + " Dr";

                    if (request.ShowZeroBalance)
                    {
                        dtRep.Rows.Add(dr["ID"], dr["ACCNAME"], dr["GROUPID"], dr["GROUPNAME"], dr["OPENBAL"], dr["BALANCE"], dr["TOTBAL"]);
                    }
                    else
                    {
                        if (bal != 0)
                        {
                            dtRep.Rows.Add(dr["ID"], dr["ACCNAME"], dr["GROUPID"], dr["GROUPNAME"], dr["OPENBAL"], dr["BALANCE"], dr["TOTBAL"]);
                        }
                    }
                }

                await dayBookDB.DropTable();

                //--

                var tableData = dtRep.AsEnumerable().Select(row => dtRep.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).ToList();
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