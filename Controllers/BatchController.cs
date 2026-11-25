using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    [Route("api/v1/batch")]
    [Authorize]
    public class BatchController : ControllerBase
    {
        private readonly DatabaseConnection _databaseConnection;

        public BatchController(
            DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }
        [HttpGet("list-pending")]

        public async Task<IActionResult> GetBatchesByProductForAdjustment([FromQuery] int productId, [FromQuery] int godownId, [FromQuery] PaginationRequest pagination)
        {
            try
            {
                LngTB.InitLng();
                Variable Variables = new Variable();

                //--

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { status = false, message = "User ID not found in token.", errors = new List<object>() });
                Variables.UserId = int.Parse(userIdClaim.Value);

                //--

                //DatabaseConnection databaseConnection = new DatabaseConnection();
                //string connectionString = databaseConnection.GetConnectionString(Variables);
                string connectionString =_databaseConnection.GetConnectionString(Variables);
                DBConn DBConn = new DBConn(connectionString);
                CommonFunction commonFunction = new CommonFunction(DBConn);

                if (!await commonFunction.CheckDatabaseConnection())
                    return StatusCode(500, new { status = false, message = "Database connection failed.", errors = new List<object>() });

                //--
                // ====================================================
                pagination ??= new PaginationRequest();
                pagination.Normalize();

                await commonFunction.SetVariableValues(Variables);
                commonFunction.Variables = Variables;
                commonFunction.Functions.Variables = Variables;

                PrMastDB prMastDB = new PrMastDB(DBConn);
                prMastDB.Variables = Variables;
                prMastDB.Functions.Variables = Variables;
                ClsPrMast clsPrMast = new();

                PrstDB prstDB = new PrstDB(DBConn);
                prstDB.Variables = Variables;
                prstDB.Functions.Variables = Variables;
                ClsPrst clsprst = new();

                //-- Get Details

                clsPrMast.PrCode = productId;
                bool MFG_YN = false, EXP_YN = false;
                //DataRow drPrd = (await prMastDB.SearchByCode(clsPrMast)).Rows[0];
                DataTable productTable = await prMastDB.SearchByCode(clsPrMast);

                if (productTable == null || productTable.Rows.Count == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        message = $"Product not found for productId: {productId}.",
                        errors = new List<object>()
                    });
                }
                DataRow drPrd = productTable.Rows[0];


                MFG_YN = Convert.ToBoolean(drPrd["MANDATEFLG"]);
                EXP_YN = Convert.ToBoolean(drPrd["EXPDATEFLG"]);

                clsprst.PrCode = productId;
                clsprst.GRack = godownId;
                DataTable dt = await prstDB.PendingList(clsprst, EXP_YN);
                int totalRecords = dt?.Rows.Count ?? 0;

                if (totalRecords == 0)
                {
                    return Ok(new
                    {
                        status = true,
                        message = "No batch details found for the given product.",
                        data = new List<object>(),
                        pagination = new
                        {
                            pageNumber = pagination.Page,
                            pageSize = pagination.PageSize,
                            totalRecords = 0,
                            totalPages = 0,
                            hasPreviousPage = false,
                            hasNextPage = false
                        }
                    });
                }

                // ====================================================
                // APPLY PAGINATION
                // ====================================================
                int totalPages = (int)Math.Ceiling(
                    totalRecords / (double)pagination.PageSize
                );

                var tableData = dt.AsEnumerable()
                    .Skip(pagination.Offset)
                    .Take(pagination.PageSize)
                    .Select(row => dt.Columns
                        .Cast<DataColumn>()
                        .ToDictionary(
                            column => column.ColumnName,
                            column => row[column] == DBNull.Value
                                ? null
                                : row[column]
                        ))
                    .ToList();

                return Ok(new
                {
                    status = true,
                    message = "Batch details loaded successfully.",
                    data = tableData,
                    pagination = new
                    {
                        pageNumber = pagination.Page,
                        pageSize = pagination.PageSize,
                        totalRecords,
                        totalPages,
                        hasPreviousPage = pagination.Page > 1,
                        hasNextPage = pagination.Page < totalPages
                    }
                });
                //if (dt.Rows.Count > 0)
                //{
                //    var tableData = dt.AsEnumerable().Select(row => dt.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).ToList();
                //    return Ok(new { status = true, message = $"Batch details loaded successfully.", data = tableData });
                //}
                //else
                //{
                //    return Ok(new { status = false, message = $"No batch details found for the given product.", errors = new List<object>() });
                //}
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = ex.Message, errors = new List<object>() });
            }
        }
    }
}
