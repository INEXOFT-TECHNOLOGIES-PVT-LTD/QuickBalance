using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using QuickBalance.API.CLS;
using QuickBalance.API.Configuration;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using System.Data;
using System.Security.Claims;

namespace QuickBalance.API.Controllers
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]

    [Route("api/[controller]")]
    public class GroupwisePurchaseReportController : ControllerBase
    {

        private readonly DatabaseConnection _databaseConnection;
        public GroupwisePurchaseReportController(
            DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }
        [HttpPost]
        public async Task<IActionResult> GetGroupwisePurchaseReport(
            GroupwisePurchaseReportRequest request)
        {
            try
            {
                LngTB.InitLng();
                Variable Variables = new Variable();
                Variables.VoucherTypeCodes = request.VoucherTypeCodes;

                //--

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { status = "error", message = "User ID not found in token." });
                Variables.UserId = int.Parse(userIdClaim.Value);

                //--

                //DatabaseConnection databaseConnection = new DatabaseConnection();
                string connectionString = _databaseConnection.GetConnectionString(Variables);

                //string connectionString = databaseConnection.GetConnectionString(Variables);
                DBConn DBConn = new DBConn(connectionString);
                CommonFunction commonFunction = new CommonFunction(DBConn);

                if (!await commonFunction.CheckDatabaseConnection())
                    return StatusCode(500, new { status = "error", message = "Database connection failed." });

                //--


                await commonFunction.SetVariableValues(Variables);

                commonFunction.Variables = Variables;
                commonFunction.Functions.Variables = Variables;

                PurmDB purmDB = new PurmDB(DBConn);

                purmDB.Variables = Variables;
                purmDB.Functions.Variables = Variables;

                ClsDate clsDate = new ClsDate();
                ClsPurM clsPurT = new ClsPurM();

                clsDate.DateFrom =
                    request.FromDate.ToString(Variables.format);

                clsDate.ToDate =
                    request.ToDate.ToString(Variables.format);

                if (request.Purchase)
                {
                    clsPurT.ReturnFlg = false;
                    clsPurT.VrType = "P";
                }
                else
                {
                    clsPurT.ReturnFlg = true;
                    clsPurT.VrType = "R";
                }

                int GdwnCD = request.AllGodown
                    ? 0
                    : request.GodownCode;

                int SuppId = request.AllSupplier
                    ? 0
                    : request.SupplierCode;

                int PartyGrp = request.AllSupplierGroup
                    ? 0
                    : request.SupplierGroupCode;

                bool Unapproved = request.IncludeUnapproved;

                DataTable dt = await purmDB.GroupwisePurchaseSummary(clsDate, clsPurT, GdwnCD, SuppId, PartyGrp, Unapproved );

                var tableData = dt.AsEnumerable()
                    .Select(row => dt.Columns.Cast<DataColumn>()
                        .ToDictionary(
                            column => column.ColumnName,
                            column => row[column] == DBNull.Value
                                ? null
                                : row[column]
                        ))
                    .ToList();

                return Ok(new
                {
                    status = "success",
                    data = tableData
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = "error",
                    message = "Database error occurred.",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "error",
                    message = "Unexpected error occurred.",
                    error = ex.Message
                });
            }
        }
    }
}