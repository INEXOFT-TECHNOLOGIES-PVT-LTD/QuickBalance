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
    public class SupplierWiseProductReportController : ControllerBase
    {
        private readonly DatabaseConnection _databaseConnection;
        public SupplierWiseProductReportController(DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }

        [HttpPost]
        public async Task<IActionResult> GetReport(
            [FromBody] SupplierWiseProductReportRequest request)
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

                string connectionString = _databaseConnection.GetConnectionString(Variables);
                //DBConn db = new DBConn(connectionString);
                DBConn db =
                    new DBConn(
                        connectionString
                    );

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

                PurmDB purmDB =
                    new PurmDB(db);

                purmDB.Variables =
                    Variables;

                purmDB.Functions.Variables =
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

                ClsPurM clsPurM =
                    new ClsPurM();

                if (request.Purchase)
                {
                    clsPurM.ReturnFlg =
                        false;

                    clsPurM.VrType =
                        "P";
                }
                else
                {
                    clsPurM.ReturnFlg =
                        true;

                    clsPurM.VrType =
                        "R";
                }

                // -----------------------------------------
                // 8. Supplier Wise Product Report
                // -----------------------------------------

                DataTable result =
                    await purmDB.SupplierWiseProductReport(
                        clsDate,
                        clsPurM,
                        request.ProductId,
                        request.GroupId,
                        request.CompanyName,
                        request.CategoryId,
                        request.SupplierCode,
                        request.PartyGroup,
                        request.Summary,
                        request.LW,
                        request.IncludeUnapproved
                    );

                // -----------------------------------------
                // 9. Convert DataTable to JSON
                // -----------------------------------------

                var data =
                    result.AsEnumerable()
                        .Select(row =>
                            result.Columns
                                .Cast<DataColumn>()
                                .ToDictionary(
                                    column =>
                                        column.ColumnName,
                                    column =>
                                        row[column] ==
                                        DBNull.Value
                                            ? null
                                            : row[column]
                                )
                        )
                        .ToList();

                // -----------------------------------------
                // 10. Return Result
                // -----------------------------------------

                return Ok(
                    new
                    {
                        status = true,
                        data = data
                    }
                );
            }
            catch (SqlException ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        status = false,
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
                        status = false,
                        message =
                            "Unexpected error occurred.",
                        error = ex.Message
                    }
                );
            }
        }
    }
}