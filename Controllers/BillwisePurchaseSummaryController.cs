using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using QuickBalance.API.CLS;
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
    public class BillwisePurchaseSummaryController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public BillwisePurchaseSummaryController(
            IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> GetBillwisePurchaseSummary(
            BillwisePurchaseSummaryRequest request)
        {
            try
            {
                // -----------------------------------------
                // 1. Get Database Settings from appsettings.json
                // -----------------------------------------

                string serverName =
                    _configuration["DatabaseSettings:ServerName"] ?? "";

                string databaseName =
                    _configuration["DatabaseSettings:DatabaseName"] ?? "";

                string dbUserName =
                    _configuration["DatabaseSettings:UserName"] ?? "";

                string dbPassword =
                    _configuration["DatabaseSettings:Password"] ?? "";

                // -----------------------------------------
                // 2. Get User Information from JWT Token
                // -----------------------------------------

                string? companyCodeClaim =
                    User.FindFirst("companyCode")?.Value;

                string? userCodeClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrWhiteSpace(companyCodeClaim) ||
                    string.IsNullOrWhiteSpace(userCodeClaim))
                {
                    return Unauthorized(new
                    {
                        status = false,
                        message = "Required information is missing from token."
                    });
                }

                int companyCode;

                int userCode;

                if (!int.TryParse(
                        companyCodeClaim,
                        out companyCode))
                {
                    return Unauthorized(new
                    {
                        status = false,
                        message = "Invalid company code in token."
                    });
                }

                if (!int.TryParse(
                        userCodeClaim,
                        out userCode))
                {
                    return Unauthorized(new
                    {
                        status = false,
                        message = "Invalid user code in token."
                    });
                }

                // -----------------------------------------
                // 3. Check Database Settings
                // -----------------------------------------

                if (string.IsNullOrWhiteSpace(serverName) ||
                    string.IsNullOrWhiteSpace(databaseName) ||
                    string.IsNullOrWhiteSpace(dbUserName) ||
                    string.IsNullOrWhiteSpace(dbPassword))
                {
                    return StatusCode(
                        500,
                        new
                        {
                            status = false,
                            message =
                                "Database settings are not configured."
                        });
                }

                // -----------------------------------------
                // 4. Create Connection String
                // -----------------------------------------

                var builder =
                    new SqlConnectionStringBuilder
                    {
                        DataSource = serverName,
                        InitialCatalog = databaseName,
                        UserID = dbUserName,
                        Password = dbPassword,
                        Encrypt = true,
                        TrustServerCertificate = true
                    };

                string connectionString =
                    builder.ConnectionString;

                // -----------------------------------------
                // 5. Create Database Connection
                // -----------------------------------------

                DBConn DBConn =
                    new DBConn(connectionString);

                CommonFunction commonFunction =
                    new CommonFunction(DBConn);

                // -----------------------------------------
                // 6. Check Database Connection
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
                        });
                }

                // -----------------------------------------
                // 7. Initialize Language and Variables
                // -----------------------------------------

                LngTB.InitLng();

                Variable Variables =
                    new Variable();

                Variables.ServerName =
                    serverName;

                Variables.DatabaseName =
                    databaseName;

                Variables.UserName =
                    dbUserName;

                Variables.Password =
                    dbPassword;

                Variables.CompCode =
                    companyCode;

                Variables.UserId =
                    userCode;

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
                // 8. Initialize Purchase DB
                // -----------------------------------------

                PurmDB purmDB =
                    new PurmDB(DBConn);

                purmDB.Variables =
                    Variables;

                purmDB.Functions.Variables =
                    Variables;

                ClsPurM clsPurM =
                    new ClsPurM();

                ClsDate clsDate =
                    new ClsDate();

                // -----------------------------------------
                // 9. Get Report
                // -----------------------------------------

                string Type = "";

                int SupId = 0;

                clsDate.DateFrom =
                    request.FromDate.ToString(
                        Variables.format
                    );

                clsDate.ToDate =
                    request.ToDate.ToString(
                        Variables.format
                    );

                if (request.BillWise == true)
                    Type = "Billwise";
                else
                    Type = "Supplierwise";

                if (request.PaymentType == "Cash")
                    clsPurM.PayMode = 1;
                else if (request.PaymentType == "Credit")
                    clsPurM.PayMode = 2;
                else
                    clsPurM.PayMode = 0;

                SupId =
                    request.AllSupplier
                        ? 0
                        : request.SupplierCode;

                bool RemGrpTot =
                    request.RemoveGroupTotal;

                int PartyGrp =
                    request.SupplierGroupCode;

                bool Unapproved =
                    request.IncludeUnapproved;

                int GdwnCD =
                    request.AllGodown
                        ? 0
                        : request.GodownCode;

                DataTable dt =
                    await purmDB.BillPurchaseSummaryNew(
                        clsDate,
                        clsPurM,
                        Type,
                        SupId,
                        RemGrpTot,
                        GdwnCD,
                        PartyGrp,
                        Unapproved
                    );

                // -----------------------------------------
                // 10. Convert DataTable to JSON-friendly data
                // -----------------------------------------

                var tableData =
                    dt
                        .AsEnumerable()
                        .Select(row =>
                            dt.Columns
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
                // 11. Return Result
                // -----------------------------------------

                return Ok(
                    new
                    {
                        status = "success",
                        data = tableData
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