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

    [Route("api/v1/purchase")]
    [Authorize]
    public class PurchaseSummaryController : ControllerBase
    {
        private readonly DatabaseConnection _databaseConnection;

        public PurchaseSummaryController(DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }

        [HttpPost("summary")]
        public async Task<IActionResult> GetPurchaseSummary(PurchaseSummaryRequest request, [FromQuery] PaginationRequest pagination)
        {
            try
            {   
                LngTB.InitLng();
                Variable Variables = new Variable();
                Variables.VoucherTypeCodes = request.VoucherTypeCodes;

                //--
                pagination ??= new PaginationRequest();
                pagination.Normalize();


                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { status = "error", message = "User ID not found in token." });
                Variables.UserId = int.Parse(userIdClaim.Value);

                //--

                //DatabaseConnection databaseConnection = new DatabaseConnection();
                //string connectionString = databaseConnection.GetConnectionString(Variables);
                //DBConn DBConn = new DBConn(connectionString);


                string connectionString = _databaseConnection.GetConnectionString(Variables);
                DBConn db = new DBConn(connectionString);
                CommonFunction commonFunction = new CommonFunction(db);

                if (!await commonFunction.CheckDatabaseConnection())
                    return StatusCode(500, new { status = "error", message = "Database connection failed." });

                //--

                await commonFunction.SetVariableValues(Variables);
                commonFunction.Variables = Variables;
                commonFunction.Functions.Variables = Variables;

                PurmDB purmDB = new PurmDB(db);
                purmDB.Variables = Variables;
                purmDB.Functions.Variables = Variables;
                ClsPurM clsPurM = new ClsPurM();

                ClsDate clsDate = new ClsDate();
                // -----------------------------------------
               // 5. Get Report
              //  ------------------------------------------
                string Type = "";
                int SupId = 0;
                clsDate.DateFrom = request.FromDate.ToString(Variables.format);
                clsDate.ToDate = request.ToDate.ToString(Variables.format);

                if (request.DateWise == true)
                    Type = "Datewise";
                else
                    Type = "Supplierwise";

                if (request.PaymentType == "Cash")
                    clsPurM.PayMode = 1;
                else if (request.PaymentType == "Credit")
                    clsPurM.PayMode = 2;
                else
                    clsPurM.PayMode = 0;

                if (request.VoucherType == "P")
                {
                    clsPurM.ReturnFlg = false;
                    clsPurM.VrType = "P";
                }
                else
                {
                    clsPurM.ReturnFlg = true;
                    clsPurM.VrType = "R";
                }
                SupId = request.SupplierCode;

                bool RemGrpTot = request.RemoveGroupTotal;
                int PartyGrp   = request.SupplierGroupCode;
                bool Unapproved = request.IncludeUnapproved;

                DataTable dt = await purmDB.PurchaseSummary(clsDate, clsPurM, Type, SupId, RemGrpTot, PartyGrp, Unapproved);

                // -----------------------------------------
                // 6. Convert DataTable to JSON-friendly data
                // -----------------------------------------
                //var tableData = dt.AsEnumerable().Select(row => dt.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).ToList();
                // 6. Apply pagination
                int totalCount = dt.Rows.Count;

                int totalPages = (int)Math.Ceiling(
                    totalCount / (double)pagination.PageSize
                );

                var tableData = dt.AsEnumerable()
                    .Skip(pagination.Offset)
                    .Take(pagination.PageSize)
                    .Select(row => dt.Columns
                        .Cast<DataColumn>()
                        .ToDictionary(
                            column => column.ColumnName,
                            column => row.IsNull(column)
                                ? null
                                : row[column]
                        ))
                    .ToList();

                // 7. Return paginated response
                return Ok(new
                {
                    status = "success",
                    message = "Purchase summary retrieved successfully.",
                    data = tableData,
                    pagination = new
                    {
                        page = pagination.Page,
                        pageSize = pagination.PageSize,
                        totalCount,
                        totalPages,
                        hasPreviousPage = pagination.Page > 1,
                        hasNextPage = pagination.Page < totalPages
                    }
                });
                // -----------------------------------------
                // 7. Return Result
                // -----------------------------------------
                //return Ok(new { status = "success", data = tableData });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { status = "error", message = "Database error occurred.", error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = "Unexpected error occurred.", error = ex.Message });
            }
        }
    }
}