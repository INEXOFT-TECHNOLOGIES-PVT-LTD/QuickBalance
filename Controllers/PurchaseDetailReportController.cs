using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
    public class PurchaseDetailReportController : ControllerBase
    {
        private readonly DatabaseConnection _databaseConnection;
        public PurchaseDetailReportController(DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }
        [HttpPost]
        public async Task<IActionResult> PurchaseDetailReport(
            [FromBody] PurchaseDetailReportRequest request)
        {
            try
            {
                LngTB.InitLng();

                Variable Variables =
                    new Variable();

                Variables.VoucherTypeCodes =
                    request.VoucherTypeCodes;

                // -----------------------------------------
                // 1. Get User ID from JWT Token
                // -----------------------------------------

                var userIdClaim =
                    User.FindFirst(
                        ClaimTypes.NameIdentifier
                    );

                if (userIdClaim == null)
                {
                    return Unauthorized(
                        new
                        {
                            status = "error",
                            message =
                                "User ID not found in token."
                        }
                    );
                }

                if (!int.TryParse(
                        userIdClaim.Value,
                        out int userId))
                {
                    return Unauthorized(
                        new
                        {
                            status = "error",
                            message =
                                "Invalid User ID in token."
                        }
                    );
                }

                Variables.UserId =
                    userId;

                // -----------------------------------------
                // 2. Get Database Connection
                // -----------------------------------------

                //DatabaseConnection databaseConnection =
                //    new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(
                //        Variables
                //    );

                //DBConn db =
                //    new DBConn(
                //        connectionString
                //    );

                    
        string connectionString = _databaseConnection.GetConnectionString(Variables);
        DBConn db = new DBConn(connectionString);
        CommonFunction commonFunction =
                    new CommonFunction(db);

                // -----------------------------------------
                // 3. Check Database Connection
                // -----------------------------------------

                if (!await commonFunction.CheckDatabaseConnection())
                {
                    return StatusCode(
                        500,
                        new
                        {
                            status = "error",
                            message =
                                "Database connection failed."
                        }
                    );
                }

                // -----------------------------------------
                // 4. Set Variables
                // -----------------------------------------

                Variables.VoucherTypeCodes =
                    request.VoucherTypeCodes;

                await commonFunction.SetVariableValues(
                    Variables
                );

                commonFunction.Variables =
                    Variables;

                commonFunction.Functions.Variables =
                    Variables;

                // -----------------------------------------
                // 5. Initialize Purchase DB
                // -----------------------------------------

                PurmDB PurmDB =
                    new PurmDB(db);

                PurmDB.Variables =
                    Variables;

                PurmDB.Functions.Variables =
                    Variables;

                // -----------------------------------------
                // 6. Date
                // -----------------------------------------

                ClsDate clsDate =
                    new ClsDate();

                clsDate.DateFrom =
                    request.FromDate.ToString(
                        Variables.format
                    );

                clsDate.ToDate =
                    request.ToDate.ToString(
                        Variables.format
                    );

                // -----------------------------------------
                // 7. Purchase / Return
                // -----------------------------------------

                ClsPurT clsPurT =
                    new ClsPurT();

                if (request.Purchase)
                    clsPurT.VrType = "P";
                else
                    clsPurT.VrType = "R";

                clsPurT.SupCode =
                    request.SupplierCode;

                clsPurT.CancelFlg =
                    false;

                // -----------------------------------------
                // 8. Get Purchase Detail Report
                // -----------------------------------------

                DataTable dt =
                    await PurmDB.PurchaseDetailReport(
                        clsDate,
                        clsPurT,
                        request.RemoveGroupTotal,
                        request.IncludeUnapproved
                    );

                // -----------------------------------------
                // 9. Convert DataTable to JSON
                // -----------------------------------------

                var result =
                    new List<
                        Dictionary<string, object?>
                    >();

                foreach (DataRow row in dt.Rows)
                {
                    var item =
                        new Dictionary<
                            string,
                            object?
                        >();

                    foreach (DataColumn column in dt.Columns)
                    {
                        item[column.ColumnName] =
                            row[column] ==
                            DBNull.Value
                                ? null
                                : row[column];
                    }

                    result.Add(item);
                }

                // -----------------------------------------
                // 10. Return Result
                // -----------------------------------------

                return Ok(
                    new
                    {
                        status = "success",
                        data = result
                    }
                );
            }
            catch (SqlException ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        status = "error",
                        message =
                            "Database error occurred.",
                        error = ex.Message
                    }
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        status = "error",
                        message =
                            "Unexpected error occurred.",
                        error = ex.Message
                    }
                );
            }
        }
    }
}