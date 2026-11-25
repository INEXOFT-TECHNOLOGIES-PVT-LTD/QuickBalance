using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.Configuration;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Respons_DTO;
using QuickBalance.API.Services;
using System.Data;
using System.Security.Claims;

namespace QuickBalance.API.Controllers
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/v1/accounts")]
    [Authorize]
    public class AccountsController : ControllerBase
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ICurrentUser _currentUser;

        public AccountsController(
            IDbConnectionFactory connectionFactory,
            ICurrentUser currentUser)
        {
            _connectionFactory = connectionFactory;
            _currentUser = currentUser;
        }

        [HttpGet("list-party")]
        public async Task<IActionResult> GetAccounts([FromQuery] PaginationRequest request)
        {
            try
            {
                LngTB.InitLng();
                Variable Variables = new Variable();
                request.Normalize();
                Variables.pgRequest = request;

                //--
                Variables.UserId = _currentUser.UserId;
                //var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                //if (userIdClaim == null)
                //    return Unauthorized(new { status = false, message = "User ID not found in token.", errors = new List<object>() });
                //Variables.UserId = int.Parse(userIdClaim.Value);

                //--

                //DatabaseConnection databaseConnection = new DatabaseConnection();
                //string connectionString = databaseConnection.GetConnectionString(Variables);
                //DBConn DBConn = new DBConn(connectionString);
                //CommonFunction commonFunction = new CommonFunction(DBConn);
                string connectionString =_connectionFactory.GetConnectionString(Variables);

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

                //-- Get Details                

                string type = request.Type ?? "";
                type = type.Trim().ToLower();

                if (string.IsNullOrWhiteSpace(type))
                    return BadRequest(new { status = false, message = "List type is required.", errors = new List<object>() });

                if (type != "customer" && type != "supplier")
                    return BadRequest(new { status = false, message = "Invalid list type. Use customer or supplier.", errors = new List<object>() });
                
                PartyQueryResult result = type == "customer" ? await accMasDB.AccountsForSales() : await accMasDB.AccountsForPurchase();

                int totalPages = result.TotalRecords == 0
                    ? 0
                    : (int)Math.Ceiling(
                        result.TotalRecords / (double)request.PageSize);

                var response = new PartyListResponse
                {
                    Status = true,
                    Message = result.Data.Count > 0
                        ? $"{(type == "customer" ? "Customer" : "Supplier")} accounts loaded successfully."
                        : $"No {type} accounts found.",
                    Data = result.Data,
                    Pagination = new PaginationResponse
                    {
                        Page = request.Page,
                        PageSize = request.PageSize,
                        TotalRecords = result.TotalRecords,
                        TotalPages = totalPages
                    }
                };
                return Ok(response);

                //DataTable dt = type == "customer" ? await accMasDB.AccountsForSales() : await accMasDB.AccountsForPurchase();

                //int totalRecords = 0;
                //if (dt.Rows.Count > 0)
                //    totalRecords = Convert.ToInt32(dt.Rows[0]["TotalRecords"]);
                //if (dt.Columns.Contains("TotalRecords"))
                //    dt.Columns.Remove("TotalRecords");

                //int totalPages = totalRecords > 0 ? (int)Math.Ceiling(totalRecords / (double)request.PageSize) : 0;
                //PaginationResponse pagination = new PaginationResponse
                //{
                //    Page = request.Page,
                //    PageSize = request.PageSize,
                //    TotalRecords = totalRecords,    
                //    TotalPages = totalPages
                //};

                //var tableData = dt.AsEnumerable().Select(row => dt.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).ToList();
                //return Ok(new
                //{
                //    status = true,
                //    message = tableData.Count > 0 ? $"{(type == "customer" ? "Customer" : "Supplier")} accounts loaded successfully." : $"No {(type == "customer" ? "customer" : "supplier")} accounts found.",
                //    data = tableData,
                //    pagination = pagination
                //});
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = ex.Message, errors = new List<object>() });
            }
        }
    }
}