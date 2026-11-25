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
    public class ProductwisePurchaseReportController : ControllerBase
    {
        private readonly DatabaseConnection _databaseConnection;

        public ProductwisePurchaseReportController(DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }
        

        [HttpPost]
        public async Task<IActionResult> GetProductwisePurchaseReport(
            ProductwisePurchaseReportRequest request)
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
                //string connectionString = databaseConnection.GetConnectionString(Variables);

                string connectionString = _databaseConnection.GetConnectionString(Variables);
                DBConn dBConn = new DBConn(connectionString);

                //DBConn DBConn = new DBConn(connectionString);
                CommonFunction commonFunction = new CommonFunction(dBConn);

                if (!await commonFunction.CheckDatabaseConnection())
                    return StatusCode(500, new { status = "error", message = "Database connection failed." });

                //--


                await commonFunction.SetVariableValues(Variables);

                commonFunction.Variables = Variables;
                commonFunction.Functions.Variables = Variables;

                PurmDB purmDB = new PurmDB(dBConn);

                purmDB.Variables = Variables;
                purmDB.Functions.Variables = Variables;

                ClsPurM clsPurM = new ClsPurM();
                ClsDate clsDate = new ClsDate();

                string Type = "";
                int PrId = 0;
                int GrpId = 0;
                string? CmpName = null;
                int CatgId = 0;
                int GdwnCD = 0;
                int SuppId = 0;
                int PartyGrp = 0;

                clsDate.DateFrom =
                    request.FromDate.ToString(Variables.format);

                clsDate.ToDate =
                    request.ToDate.ToString(Variables.format);

                if (request.Purchase)
                {
                    clsPurM.ReturnFlg = false;
                    clsPurM.VrType = "P";
                }
                else
                {
                    clsPurM.ReturnFlg = true;
                    clsPurM.VrType = "R";
                }

                if (request.Type == "Product")
                    Type = "Product";
                else
                    Type = "Group";

                if (request.AllProduct)
                    PrId = 0;
                else
                    PrId = request.ProductCode;

                if (request.AllGroup)
                    GrpId = 0;
                else
                    GrpId = request.GroupCode;

                if (request.AllBrand)
                    CmpName = null;
                else
                    CmpName = request.BrandCode;

                if (request.AllCategory)
                    CatgId = 0;
                else
                    CatgId = request.CategoryCode;

                if (request.AllGodown)
                    GdwnCD = 0;
                else
                    GdwnCD = request.GodownCode;

                if (request.AllSupplier)
                    SuppId = 0;
                else
                    SuppId = request.SupplierCode;

                if (request.AllSupplierGroup)
                    PartyGrp = 0;
                else
                    PartyGrp = request.SupplierGroupCode;

                bool Unapproved = request.IncludeUnapproved;

                DataTable dt = await purmDB.ProductwisePurcahseSummary(clsDate, clsPurM, Type, PrId, GrpId, CmpName, CatgId, GdwnCD, SuppId, PartyGrp, Unapproved);

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