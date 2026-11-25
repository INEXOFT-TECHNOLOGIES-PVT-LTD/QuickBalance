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

    [Route("api/v1/reports/daily-balance")]
    [Authorize]
    public class DailyBalanceController : ControllerBase
    {
        private readonly DatabaseConnection _databaseConnection;
        public DailyBalanceController(DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }
        [HttpPost]
        public async Task<IActionResult> GetDailyBalance(DailyBalanceRequest request)
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

                AccMasDB accMasDB = new AccMasDB(DBConn);
                accMasDB.Variables = Variables;
                accMasDB.Functions.Variables = Variables;
                ClsAccMas clsAccMas = new ClsAccMas();

                DayBookDB dayBookDB = new DayBookDB(DBConn);
                dayBookDB.Variables = Variables;
                dayBookDB.Functions.Variables = Variables;

                //-- Get Report

                DataTable dt = new DataTable();

                await dayBookDB.CreateTable(request.FromDate.ToString(Variables.format), request.ToDate.ToString(Variables.format), "DB", Unapproved: request.IncludeUnapproved);
                dt = await dayBookDB.DailyBalanceReport(request.FromDate.Date, request.ToDate.Date);                
                clsAccMas.Id = Variables.CashCD;

                decimal CashOpenBal = -1 * await accMasDB.Opening(Variables.CashCD);
                CashOpenBal += -1 * await dayBookDB.OpenBalance(request.FromDate.Date, request.IncludeUnapproved);
                foreach (DataRow dr in dt.Rows)
                {
                    if (dr["RECCASH"] == DBNull.Value)
                        dr["RECCASH"] = 0.ToString(Variables.DecString);
                    if (dr["PAYCASH"] == DBNull.Value)
                        dr["PAYCASH"] = 0.ToString(Variables.DecString);
                    dr["OPENBAL"] = CashOpenBal.ToString(Variables.DecString);
                    CashOpenBal += Convert.ToDecimal(dr["RECCASH"]) - Convert.ToDecimal(dr["PAYCASH"]);
                    dr["CLOSINGBAL"] = CashOpenBal.ToString(Variables.DecString);
                }

                await dayBookDB.DropTable();

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
    }
}