using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using QuickBalance.API.Configuration;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Respons_DTO;
using QuickBalance.API.Services;
using System.Data;
using System.Reflection.PortableExecutable;
using System.Security.Claims;

namespace QuickBalance.API.Controllers.Transactions.Sales
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = false)]   // set to true to hide this controller from Swagger
    [Tags("25. Sales Orders")]
    [Route("api/v1/sales-orders")]
    [Authorize]
    public class SalesOrderController : ControllerBase
    {
        private readonly DatabaseConnection _databaseConnection;
     
        private readonly ICurrentUser _currentUser;
        public SalesOrderController(DatabaseConnection database,ICurrentUser currentUser)
        {
            _currentUser = currentUser;
            _databaseConnection = database;
        }

        [HttpPost("manage")]
        public async Task<IActionResult> ManageSalesOrder([FromBody] SalesOrderRequest request)
        {
            try
            {
                LngTB.InitLng();
                Variable Variables = new Variable();

                //--
                Variables.UserId = _currentUser.UserId;

                //var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                //if (userIdClaim == null)
                //    return Unauthorized(new { status = false, message = "User ID not found in token.", errors = new List<object>() });
                //Variables.UserId = int.Parse(userIdClaim.Value);

                //--

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

                //-- Manage Sales Order Details

                try
                {
                    if (request == null)
                        return BadRequest(new { status = false, message = "Order data is required.", errors = new List<object>() });

                    if (request.Items == null || request.Items.Count == 0)
                        return BadRequest(new { status = false, message = "At least one order item is required.", errors = new List<object>() });

                    if (request.CustomerCode == 0)
                        return BadRequest(new { status = false, message = "Party details are required.", errors = new List<object>() });

                    SalesOrderDB salesOrderDB = new SalesOrderDB(DBConn);
                    DataTable result = await salesOrderDB.ManageSalesOrder(request, Variables.UserId);
                    if (result.Rows.Count == 0)
                        return StatusCode(500, new { status = false, message = "No response received from sales order procedure.", errors = new List<object>() });

                    string message = Convert.ToString(result.Rows[0]["MESSAGE"]) ?? "";
                    int status = Convert.ToInt32(result.Rows[0]["STATUS"]);
                    if (status == 1)
                    {
                        if (message == "Saved")
                            return Created("", new { status = true, message = message, data = new { message = message } });
                        return Ok(new { status = true, message = message, data = new { message = message } });
                    }

                    return BadRequest(new { status = false, message = message, errors = new List<object>() });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { status = false, message = ex.Message, errors = new List<object>() });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message, errors = new List<object>() });
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListSalesOrder([FromQuery] PaginationRequests   request, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                LngTB.InitLng();
                Variable Variables = new Variable();
                //request.Normalize();
                //Variables.pgRequest = request;
                Variables.Page = request.Page;
                Variables.PageSize = request.PageSize;
                Variables.Search = request.Search;
                Variables.FromDate = fromDate;
                Variables.ToDate = toDate;


                //--

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { status = false, message = "User ID not found in token.", errors = new List<object>() });
                Variables.UserId = int.Parse(userIdClaim.Value);

                ////--

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

                CordMDB cordMDB = new CordMDB(DBConn);
                cordMDB.Variables = Variables;
                cordMDB.Functions.Variables = Variables;

                //-- List Sales Order Details

                try
                {
                    DataTable dt = await cordMDB.ListAsync("O");

                    int totalRecords = 0;
                    if (dt.Rows.Count > 0)
                        totalRecords = Convert.ToInt32(dt.Rows[0]["TotalRecords"]);
                    if (dt.Columns.Contains("TotalRecords"))
                        dt.Columns.Remove("TotalRecords");

                    int totalPages = totalRecords > 0 ? (int)Math.Ceiling(totalRecords / (double)request.PageSize) : 0;
                    PaginationResponse pagination = new PaginationResponse
                    {
                        Page = request.Page,
                        PageSize = request.PageSize,
                        TotalRecords = totalRecords,
                        TotalPages = totalPages
                    };

                    var tableData = dt.AsEnumerable().Select(row => dt.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).ToList();

                    return Ok(new
                    {
                        status = true,
                        message = tableData.Count > 0 ? "Sales order details loaded successfully." : "No sales order details found.",
                        data = tableData,
                        pagination = pagination
                    });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { status = false, message = ex.Message, errors = new List<object>() });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message, errors = new List<object>() });
            }
        }



        [HttpGet("get/{id:int}")]
        public async Task<IActionResult> GetSelectedSalesOrder(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Order number is required.",
                        errors = new List<object>()
                    });
                }

                LngTB.InitLng();

                var variables = new Variable();

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null ||
                    !int.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new
                    {
                        status = false,
                        message = "A valid user ID was not found in the token.",
                        errors = new List<object>()
                    });
                }

                variables.UserId = userId;

                //var databaseConnection = new DatabaseConnection();
                //var connectionString =
                //    databaseConnection.GetConnectionString(variables);
                string connectionString = _databaseConnection.GetConnectionString(variables);

                //DBConn db = new DBConn(connectionString);

                var dbConn = new DBConn(connectionString);
                var commonFunction = new CommonFunction(dbConn);

                if (!await commonFunction.CheckDatabaseConnection())
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Database connection failed.",
                        errors = new List<object>()
                    });
                }

                await commonFunction.SetVariableValues(variables);

                commonFunction.Variables = variables;
                commonFunction.Functions.Variables = variables;

                var cordMDB = new CordMDB(dbConn)
                {
                    Variables = variables
                };
                cordMDB.Functions.Variables = variables;

                var cordTDB = new CordTDB(dbConn)
                {
                    Variables = variables
                };
                cordTDB.Functions.Variables = variables;

                var cordDDB = new CordDDB(dbConn)
                {
                    Variables = variables
                };
                cordDDB.Functions.Variables = variables;

                var headerRequest = new ClsCordM
                {
                    OrdNo = id,
                    SType = "O"
                };

                var header = await cordMDB.SearchByOrdNOAsync(headerRequest);

                if (header == null)
                {
                    return NotFound(new
                    {
                        status = false,
                        message = "Sales order not found.",
                        errors = new List<object>()
                    });
                }

                var itemRequest = new ClsCordT
                {
                    OrdNo = id,
                    SType = "O"
                };

                var items = await cordTDB.SearchByOrdNoAsync(itemRequest);

                var additionalInfoRequest = new ClsCordD
                {
                    VrNo = id,
                    VrType = "O",
                    DFlag = false
                };

                var additionalInfo =
                    await cordDDB.SearchAddInfoAsync(additionalInfoRequest);

                var response = new SelectedSalesOrderResponse
                {
                    Status = true,
                    Message = "Sales order retrieved successfully.",
                    Data = new SelectedSalesOrderData
                    {
                        Header = header,
                        Items = items,
                        AdditionalInformations = additionalInfo
                    }
                };

                return Ok(response);
            }
            catch (Exception)
            {
                // Log the exception using your configured ILogger.
                // Do not expose internal database errors to API clients.
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred while retrieving the sales order.",
                    errors = new List<object>()
                });
            }
        }
        //[HttpGet("get/{id:int}")]

        //public async Task<IActionResult> GetSelectedSalesOrder(int id)
        //{
        //    try
        //    {
        //        if (id <= 0)
        //            return BadRequest(new { status = false, message = "Order number is required.", errors = new List<object>() });

        //        LngTB.InitLng();
        //        Variable Variables = new Variable();

        //        //--

        //        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        //        if (userIdClaim == null)
        //            return Unauthorized(new { status = false, message = "User ID not found in token.", errors = new List<object>() });
        //        Variables.UserId = int.Parse(userIdClaim.Value);

        //        //--

        //        DatabaseConnection databaseConnection = new DatabaseConnection();
        //        string connectionString = databaseConnection.GetConnectionString(Variables);
        //        DBConn DBConn = new DBConn(connectionString);
        //        CommonFunction commonFunction = new CommonFunction(DBConn);

        //        if (!await commonFunction.CheckDatabaseConnection())
        //            return StatusCode(500, new { status = false, message = "Database connection failed.", errors = new List<object>() });

        //        //--

        //        await commonFunction.SetVariableValues(Variables);
        //        commonFunction.Variables = Variables;
        //        commonFunction.Functions.Variables = Variables;

        //        CordMDB cordMDB = new CordMDB(DBConn);
        //        cordMDB.Variables = Variables;
        //        cordMDB.Functions.Variables = Variables;
        //        ClsCordM clsCordM = new ClsCordM();

        //        CordTDB cordTDB = new CordTDB(DBConn);
        //        cordTDB.Variables = Variables;
        //        cordTDB.Functions.Variables = Variables;
        //        ClsCordT clsCordT = new ClsCordT();

        //        CordDDB cordDDB = new CordDDB(DBConn);
        //        cordDDB.Variables = Variables;
        //        cordDDB.Functions.Variables = Variables;
        //        ClsCordD clsCordD = new ClsCordD();

        //        //-- Get Selected Sales Order Details

        //        clsCordM.SType = "O";
        //        clsCordM.OrdNo = id;
        //        DataTable dtHdr = await cordMDB.SearchByOrdNO(clsCordM);

        //        clsCordT.SType = "O";
        //        clsCordT.OrdNo = id;
        //        DataTable dtItem = await cordTDB.SearchByOrdNo(clsCordT);

        //        if (dtItem.Columns.Contains("SL"))
        //            dtItem.Columns.Remove("SL");

        //        clsCordD.DFlag = false;
        //        clsCordD.VrNo = id;
        //        clsCordD.VrType = "O";
        //        //var additionalInfo = await cordDDB.SearchAddInfo(clsCordD);

        //        //var response = new SelectedSalesOrderResponse
        //        //{
        //        //    Status = true,
        //        //    Message = "Sales order retrieved successfully.",
        //        //    Data = new SelectedSalesOrderData
        //        //    {
        //        //        Header = dtHdr,
        //        //        Items = dtItem,
        //        //        AdditionalInformations = additionalInfo
        //        //    }
        //        //};
        //        DataTable dtAdd = await cordDDB.SearchAddInfo(clsCordD);

        //        if (dtHdr == null || dtHdr.Rows.Count == 0)
        //        {
        //            return NotFound(new { status = false, message = "Sales order not found.", errors = new List<object>() });
        //        }
        //        else
        //        {
        //            var invoiceDetails = new
        //            {
        //                Header = dtHdr.AsEnumerable().Select(row => dtHdr.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).FirstOrDefault(),
        //                Items = dtItem.AsEnumerable().Select(row => dtItem.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).ToList(),
        //                AdditionalInformations = dtAdd.AsEnumerable().Select(row => dtAdd.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).ToList()
        //            };

        //            return Ok(new { status = true, message = "Sales order retrieved successfully.", data = invoiceDetails });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { status = false, message = ex.Message, errors = new List<object>() });
        //    }
        //}

        [HttpPost("cancel/{id:int}")]
            public async Task<IActionResult> CancelSalesOrder(int id)
            {
                try
                {
                    if (id <= 0)
                        return BadRequest(new { status = false, message = "Order number is required.", errors = new List<object>() });

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

                    //-- Cancel Sales Order Details

                    try
                    {
                        SalesOrderDB salesOrderDB = new SalesOrderDB(DBConn);
                        DataTable result = await salesOrderDB.CancelSalesOrder(id, Variables.UserId);
                        if (result.Rows.Count == 0)
                            return StatusCode(500, new { status = false, message = "No response received from sales order procedure.", errors = new List<object>() });

                        string message = Convert.ToString(result.Rows[0]["MESSAGE"]) ?? "";
                        int status = Convert.ToInt32(result.Rows[0]["STATUS"]);
                        if (status == 1)
                            return Ok(new { status = true, message = message, data = new { message = message } });

                        return BadRequest(new { status = false, message = message, errors = new List<object>() });
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(500, new { status = false, message = ex.Message, errors = new List<object>() });
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { status = false, message = ex.Message, errors = new List<object>() });
                }
            }
        [HttpGet("pending-list")]
        public async Task<IActionResult> ListPendingSalesOrderInInvoices([FromQuery] PaginationRequest request)
        {
            try
            {
                LngTB.InitLng();
                Variable Variables = new Variable();
                request.Normalize();
                Variables.pgRequest = request;

                //--

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { status = false, message = "User ID not found in token.", errors = new List<object>() });
                Variables.UserId = int.Parse(userIdClaim.Value);

                //--

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

                //-- List Sales Order Details

                try
                {
                    SalesOrderDB salesOrderDB = new SalesOrderDB(DBConn);
                    DataTable dt = await salesOrderDB.PendingSalesOrderInInvoices(request);

                    int totalRecords = 0;
                    if (dt.Columns.Contains("TotalRecords"))
                    {
                        if (dt.Rows.Count > 0)
                            totalRecords = Convert.ToInt32(dt.Rows[0]["TotalRecords"]);
                        dt.Columns.Remove("TotalRecords");
                    }

                    int totalPages = totalRecords > 0 ? (int)Math.Ceiling(totalRecords / (double)request.PageSize) : 0;
                    PaginationResponse pagination = new PaginationResponse
                    {
                        Page = request.Page,
                        PageSize = request.PageSize,
                        TotalRecords = totalRecords,
                        TotalPages = totalPages
                    };

                    var tableData = dt.AsEnumerable().Select(row => dt.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).ToList();

                    if (request.OrderId == 0)
                    {
                        return Ok(new
                        {
                            status = true,
                            message = tableData.Count > 0 ? "Pending sales order details loaded successfully." : "No pending sales order details found.",
                            data = tableData,
                            pagination = pagination
                        });
                    }
                    else
                    {
                        return Ok(new
                        {
                            status = true,
                            message = tableData.Count > 0 ? "Pending sales order item details loaded successfully." : "No pending sales order item details found.",
                            data = tableData,
                        });
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { status = false, message = ex.Message, errors = new List<object>() });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message, errors = new List<object>() });
            }
        }
    }
}