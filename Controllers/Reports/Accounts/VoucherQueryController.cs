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

    [Route("api/v1/reports/voucher-query")]
    [Authorize]
    public class VoucherQueryController : ControllerBase
    {
        private readonly DatabaseConnection _databaseConnection;
        public VoucherQueryController(DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }
        [HttpPost]
        public async Task<IActionResult> GetVoucherQuery(VoucherQueryRequest request)
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
                DBConn db = new DBConn(connectionString);
                DBConn DBConn = new DBConn(connectionString);
                CommonFunction commonFunction = new CommonFunction(DBConn);

                if (!await commonFunction.CheckDatabaseConnection())
                    return StatusCode(500, new { status = false, message = "Database connection failed.", errors = new List<object>() });

                //--

                await commonFunction.SetVariableValues(Variables);
                commonFunction.Variables = Variables;
                commonFunction.Functions.Variables = Variables;

                YvrDB yvrDB = new YvrDB(DBConn);
                yvrDB.Variables = Variables;
                yvrDB.Functions.Variables = Variables;

                //-- Get Report

                ClsDate clsDate = new ClsDate();
                clsDate.DateFrom = request.FromDate.Date.ToString(Variables.format);
                clsDate.ToDate = request.ToDate.Date.ToString(Variables.format);
                DataTable dt = await yvrDB.GetVoucherQuery(request.AccountCode, clsDate, request.Remarks.Trim().Replace("'", "''"), request.VoucherNo.Trim().Replace("'", "''"), request.Amount.ToString(), request.VoucherType.ToUpper() == "ALL" ? "" : request.VoucherType, request.Exact ? "E" : "", request.IncludeUnapproved);

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