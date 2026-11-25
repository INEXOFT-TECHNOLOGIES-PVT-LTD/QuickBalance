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

    [Route("api/v1/serial-number")]
    [Authorize]
    public class SerialNumberController : ControllerBase
    {
        private readonly DatabaseConnection _databaseConnection;
        public SerialNumberController(DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }
        [HttpGet("list-pending")]
        public async Task<IActionResult> GetSerialNumbersByProductForAdjustment([FromQuery] int productId, [FromQuery] int unitId, [FromQuery] int godownId, [FromQuery] PaginationRequest pagination)
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
                // ====================================================
                // PAGINATION
                // ====================================================
                pagination ??= new PaginationRequest();
                pagination.Normalize();


                //DatabaseConnection databaseConnection = new DatabaseConnection();
                //string connectionString = databaseConnection.GetConnectionString(Variables);

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

                PrMastDB prMastDB = new PrMastDB(DBConn);
                prMastDB.Variables = Variables;
                prMastDB.Functions.Variables = Variables;
                ClsPrMast clsPrMast = new();

                SntStkDB sntStkDB = new SntStkDB(DBConn);
                sntStkDB.Variables = Variables;
                sntStkDB.Functions.Variables = Variables;
                ClsSntStk clsSntStk = new();

                //-- Get Details

                clsSntStk.PRCODE = productId;
                clsSntStk.GDWCODE = godownId;
                clsSntStk.UNITCODE = unitId;
                DataTable dt = await sntStkDB.SearchForAdjust(clsSntStk);
                // ====================================================
                // APPLY PAGINATION
                // ====================================================
                int totalRecords = dt?.Rows.Count ?? 0;

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

                if (tableData.Count <= 0)
                {
                    return Ok(new
                    {
                        status = false,
                        message = $"No serial numbers found for the given product.",
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
                }
                else
                {
                    return Ok(new
                    {
                        status = true,
                        message = "Serial numbers loaded successfully.",
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
                }
                //if (dt.Rows.Count > 0)
                //{
                //    var tableData = dt.AsEnumerable().Select(row => dt.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).ToList();
                //    return Ok(new { status = true, message = $"Serial numbers loaded successfully.", data = tableData });
                //}
                //else
                //{
                //    return Ok(new { status = false, message = $"No serial numbers found for the given product.", data = new List<object>() });
                //}
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = ex.Message, errors = new List<object>() });
            }
        }
    }
}
