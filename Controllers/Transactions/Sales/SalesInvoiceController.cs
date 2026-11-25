using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using QuickBalance.API.Configuration;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Respons_DTO;
using System.Data;
using System.Reflection.PortableExecutable;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace QuickBalance.API.Controllers.Transactions.Sales
{
    [ApiController]
    [Tags("26. Sales Invoices")]

    [Route("api/v1/sales-invoices")]
    [Authorize]
    public class SalesInvoiceController : ControllerBase
    {
        private readonly DatabaseConnection _databaseConnection;
        public SalesInvoiceController(DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }
        [HttpPost("manage")]
        public async Task<IActionResult> ManageSalesInvoice([FromBody] SalesInvoiceRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
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
                string connectionString = _databaseConnection.GetConnectionString(Variables);

                //DBConn db = new DBConn(connectionString);
                DBConn DBConn = new DBConn(connectionString);
                CommonFunction commonFunction = new CommonFunction(DBConn);

                if (!await commonFunction.CheckDatabaseConnection())
                    return StatusCode(500, new { status = false, message = "Database connection failed.", errors = new List<object>() });

                //--
                string requestJson = JsonSerializer.Serialize(request);

                string requestHash = Convert.ToHexString(
                    SHA256.HashData(
                        Encoding.UTF8.GetBytes(requestJson)));


                await commonFunction.SetVariableValues(Variables);
                commonFunction.Variables = Variables;
                commonFunction.Functions.Variables = Variables;

                //-- Manage Sales Invoice Details



                if (string.IsNullOrWhiteSpace(idempotencyKey) ||
                    idempotencyKey.Length > 100)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "A valid Idempotency-Key header is required.",
                        errors = new List<object>()
                    });
                }


                try
                {
                    if (request == null)
                        return BadRequest(new { status = false, message = "Invoice data is required.", errors = new List<object>() });

                    if (request.Items == null || request.Items.Count == 0)
                        return BadRequest(new { status = false, message = "At least one invoice item is required.", errors = new List<object>() });

                    if (request.CustomerCode == 0)
                        return BadRequest(new { status = false, message = "Party details are required.", errors = new List<object>() });

                    SalesInvoiceDB salesInvoiceDB = new SalesInvoiceDB(DBConn);
                    DataTable result = await salesInvoiceDB.ManageSalesInvoice(request, Variables.UserId,idempotencyKey, requestHash);
                    if (result.Rows.Count == 0)
                        return StatusCode(500, new { status = false, message = "No response received from sales invoice procedure.", errors = new List<object>() });

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
        public async Task<IActionResult> ListSalesInvoice([FromQuery] PaginationRequest request)
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

                SalMDB salMDB = new SalMDB(DBConn);
                salMDB.Variables = Variables;
                salMDB.Functions.Variables = Variables;
                ClsSalM clsSalM = new ClsSalM();

                //-- List Sales Invoice Details

                try
                {
                    clsSalM.SType = "O";
                    DataTable dt = await salMDB.List(clsSalM);

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
                        message = tableData.Count > 0 ? "Sales invoice details loaded successfully." : "No sales invoice details found.",
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
        public async Task<IActionResult> GetSelectedSalesInvoice(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invoice number is required.",
                        errors = new List<object>()
                    });
                }

                LngTB.InitLng();

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null ||
                    !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new
                    {
                        status = false,
                        message = "Valid user ID not found in token.",
                        errors = new List<object>()
                    });
                }

                // Select the logged-in user's company database.
                Variable variables = new Variable
                {
                    UserId = userId
                };

                //DatabaseConnection databaseConnection = new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(variables);
                string connectionString = _databaseConnection.GetConnectionString(variables);

                //DBConn db = new DBConn(connectionString);
                DBConn dbConn = new DBConn(connectionString);
                CommonFunction commonFunction = new CommonFunction(dbConn);

                if (!await commonFunction.CheckDatabaseConnection())
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Database connection failed.",
                        errors = new List<object>()
                    });
                }

                // Preserve existing application variable initialization.
                await commonFunction.SetVariableValues(variables);

                commonFunction.Variables = variables;
                commonFunction.Functions.Variables = variables;

                // The service uses the same company-specific connection string.
                //SalesInvoiceService invoiceService =
                //    new SalesInvoiceService(connectionString);
                SalMDB salMDB = new SalMDB(dbConn);
                salMDB.Variables = variables;
                salMDB.Functions.Variables = variables;
                var dtHdr = await salMDB.GetInvoiceAsync(id);

                //SalesInvoiceResponseDto? invoice =
                //    await invoiceService.GetInvoiceAsync(id);

                if (dtHdr == null)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Sales invoice not found.",
                        errors = new List<object>()
                    });
                }

                return Ok(new
                {
                    status = true,
                    message = "Sales invoice retrieved successfully.",
                    data = dtHdr
                });
            }
            catch (Exception ex)
            {
                // Replace with your application's ILogger logging if available.
                // Avoid returning internal SQL or exception details to API clients.
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred while retrieving the sales invoice.",
                    errors = new List<object>()
                });
            }
        }

        //[HttpGet("get/{id:int}")]
        //public async Task<IActionResult> GetSelectedSalesInvoice(int id)
        //{
        //    try
        //    {
        //        if (id <= 0)
        //        {
        //            return BadRequest(new
        //            {
        //                status = false,
        //                message = "Invoice number is required.",
        //                errors = new List<object>()
        //            });
        //        }

        //        LngTB.InitLng();

        //        Variable Variables = new Variable();

        //        // Get logged-in user ID
        //        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        //        if (userIdClaim == null ||
        //            !int.TryParse(userIdClaim.Value, out int userId))
        //        {
        //            return Unauthorized(new
        //            {
        //                status = false,
        //                message = "Valid user ID not found in token.",
        //                errors = new List<object>()
        //            });
        //        }

        //        Variables.UserId = userId;

        //        // Database connection
        //        DatabaseConnection databaseConnection = new DatabaseConnection();
        //        string connectionString = databaseConnection.GetConnectionString(Variables);

        //        DBConn DBConn = new DBConn(connectionString);
        //        CommonFunction commonFunction = new CommonFunction(DBConn);

        //        if (!await commonFunction.CheckDatabaseConnection())
        //        {
        //            return StatusCode(500, new
        //            {
        //                status = false,
        //                message = "Database connection failed.",
        //                errors = new List<object>()
        //            });
        //        }

        //        // Initialize variables
        //        await commonFunction.SetVariableValues(Variables);

        //        commonFunction.Variables = Variables;
        //        commonFunction.Functions.Variables = Variables;

        //        // Initialize database classes
        //        SalMDB salMDB = new SalMDB(DBConn);
        //        salMDB.Variables = Variables;
        //        salMDB.Functions.Variables = Variables;

        //        SalTDB salTDB = new SalTDB(DBConn);
        //        salTDB.Variables = Variables;
        //        salTDB.Functions.Variables = Variables;

        //        SntStkDB sntStkDB = new SntStkDB(DBConn);
        //        sntStkDB.Variables = Variables;
        //        sntStkDB.Functions.Variables = Variables;

        //        SalDDB salDDB = new SalDDB(DBConn);
        //        salDDB.Variables = Variables;
        //        salDDB.Functions.Variables = Variables;

        //        // Get invoice header
        //        ClsSalM clsSalM = new ClsSalM
        //        {
        //            InvNo = id
        //        };

        //        DataTable dtHdr = await salMDB.SearchByInvno(clsSalM);

        //        // Validate header before accessing Rows[0]
        //        if (dtHdr == null || dtHdr.Rows.Count == 0)
        //        {
        //            return NotFound(new
        //            {
        //                status = false,
        //                message = "Sales invoice not found.",
        //                errors = new List<object>()
        //            });
        //        }

        //        // Get invoice items
        //        ClsSalT clsSalT = new ClsSalT
        //        {
        //            InvNo = id
        //        };

        //        DataTable dtItem = await salTDB.SearchByInvno(clsSalT);

        //        // Get serial numbers
        //        ClsSntStk clsSntStk = new ClsSntStk
        //        {
        //            OUTVRTYPE = "SI",
        //            OUTVRNO = id,
        //            OUTORDNO = dtHdr.Rows[0]["ORDNO"] == DBNull.Value
        //                ? 0
        //                : Convert.ToInt32(dtHdr.Rows[0]["ORDNO"])
        //        };

        //        DataTable dtSlNo = await sntStkDB.GetByVoucherOut(clsSntStk);

        //        // Get additional information
        //        ClsSalD clsSalD = new ClsSalD
        //        {
        //            DFlag = false,
        //            VrNo = id,
        //            VrType = "O"
        //        };

        //        DataTable dtAdd = await salDDB.SearchAddInfo(clsSalD);

        //        // Prepare response
        //        var invoiceDetails = new
        //        {
        //            Header = dtHdr.AsEnumerable()
        //                .Select(row => dtHdr.Columns.Cast<DataColumn>()
        //                    .ToDictionary(
        //                        column => column.ColumnName,
        //                        column => row[column] == DBNull.Value
        //                            ? null
        //                            : row[column]))
        //                .FirstOrDefault(),

        //            Items = dtItem == null
        //                ? new List<Dictionary<string, object>>()
        //                : dtItem.AsEnumerable()
        //                    .Select(row => dtItem.Columns.Cast<DataColumn>()
        //                        .ToDictionary(
        //                            column => column.ColumnName,
        //                            column => row[column] == DBNull.Value
        //                                ? null
        //                                : row[column]))
        //                    .ToList(),

        //            SerialNumbers = dtSlNo == null
        //                ? new List<Dictionary<string, object>>()
        //                : dtSlNo.AsEnumerable()
        //                    .Select(row => dtSlNo.Columns.Cast<DataColumn>()
        //                        .ToDictionary(
        //                            column => column.ColumnName,
        //                            column => row[column] == DBNull.Value
        //                                ? null
        //                                : row[column]))
        //                    .ToList(),

        //            AdditionalInformations = dtAdd == null
        //                ? new List<Dictionary<string, object>>()
        //                : dtAdd.AsEnumerable()
        //                    .Select(row => dtAdd.Columns.Cast<DataColumn>()
        //                        .ToDictionary(
        //                            column => column.ColumnName,
        //                            column => row[column] == DBNull.Value
        //                                ? null
        //                                : row[column]))
        //                    .ToList()
        //        };

        //        return Ok(new
        //        {
        //            status = true,
        //            message = "Sales invoice retrieved successfully.",
        //            data = invoiceDetails
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            message = ex.Message,
        //            errors = new List<object>()
        //        });
        //    }
        //}

        //[HttpGet("get/{id:int}")]
        //public async Task<IActionResult> GetSelectedSalesInvoice(int id)
        //{
        //    try
        //    {
        //        if(id <= 0)
        //            return BadRequest(new { status = false, message = "Invoice number is required.", errors = new List<object>() });

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

        //        SalMDB salMDB = new SalMDB(DBConn);
        //        salMDB.Variables = Variables;
        //        salMDB.Functions.Variables = Variables;
        //        ClsSalM clsSalM = new ClsSalM();

        //        SalTDB salTDB = new SalTDB(DBConn);
        //        salTDB.Variables = Variables;
        //        salTDB.Functions.Variables = Variables;
        //        ClsSalT clsSalT = new ClsSalT();

        //        SntStkDB sntStkDB = new SntStkDB(DBConn);
        //        sntStkDB.Variables = Variables;
        //        sntStkDB.Functions.Variables = Variables;
        //        ClsSntStk clsSntStk = new ClsSntStk();

        //        SalDDB salDDB = new SalDDB(DBConn);
        //        salDDB.Variables = Variables;
        //        salDDB.Functions.Variables = Variables;
        //        ClsSalD clsSalD = new ClsSalD();

        //        //-- Get Selected Sales Invoice Details

        //        clsSalM.InvNo = id;
        //        DataTable dtHdr = await salMDB.SearchByInvno(clsSalM);

        //        clsSalT.InvNo = id;
        //        DataTable dtItem = await salTDB.SearchByInvno(clsSalT);

        //        clsSntStk.OUTVRTYPE = "SI";
        //        clsSntStk.OUTVRNO = id;
        //        clsSntStk.OUTORDNO = Convert.ToInt32(dtHdr.Rows[0]["ORDNO"]);
        //        DataTable dtSlNo = await sntStkDB.GetByVoucherOut(clsSntStk);

        //        clsSalD.DFlag = false;
        //        clsSalD.VrNo = id;
        //        clsSalD.VrType = "O";
        //        DataTable dtAdd = await salDDB.SearchAddInfo(clsSalD);

        //        if (dtHdr == null || dtHdr.Rows.Count == 0)
        //        {
        //            return NotFound(new { status = false, message = "Sales invoice not found.", errors = new List<object>() });
        //        }
        //        else
        //        {
        //            var invoiceDetails = new
        //            {
        //                Header = dtHdr.AsEnumerable().Select(row => dtHdr.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).FirstOrDefault(),
        //                Items = dtItem.AsEnumerable().Select(row => dtItem.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).ToList(),
        //                SerialNumbers = dtSlNo.AsEnumerable().Select(row => dtSlNo.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).ToList(),
        //                AdditionalInformations = dtAdd.AsEnumerable().Select(row => dtAdd.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).ToList()
        //            };

        //            return Ok(new { status = true, message = "Sales invoice retrieved successfully.", data = invoiceDetails });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { status = false, message = ex.Message, errors = new List<object>() });
        //    }
        //}

        [HttpPost("cancel/{id:int}")]
        public async Task<IActionResult> CancelSelectedSalesInvoice(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { status = false, message = "Invoice number is required.", errors = new List<object>() });

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

                //-- Cancel Sales Invoice Details

                try
                {
                    SalesInvoiceDB salesInvoiceDB = new SalesInvoiceDB(DBConn);
                    DataTable result = await salesInvoiceDB.CancelSalesInvoice(id, Variables.UserId, commonFunction);
                    if (result.Rows.Count == 0)
                        return StatusCode(500, new { status = false, message = "No response received from sales invoice procedure.", errors = new List<object>() });

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
        [HttpGet("pending")]
        public async Task<IActionResult> ListPendingSalesInvoicesInReturn([FromQuery] int salesReturnId, [FromQuery] string SalesInvoiceNumber)
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
                //DBConn DBConn = new DBConn(connectionString);
                string connectionString = _databaseConnection.GetConnectionString(Variables);

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
                    SalesInvoiceDB salesOrderDB = new SalesInvoiceDB(DBConn);
                    DataTable dtItem = await salesOrderDB.PendingSalesInvoicesInReturn(salesReturnId, SalesInvoiceNumber);

                    SalMDB salMDB = new SalMDB(DBConn);
                    salMDB.Variables = Variables;
                    salMDB.Functions.Variables = Variables;
                    ClsSalM clsSalM = new ClsSalM();

                    clsSalM.InvNo = dtItem.Rows.Count > 0 ? Convert.ToInt32(dtItem.Rows[0]["INVNO"]) : 0;
                    DataTable dtHdr = await salMDB.SearchByInvno(clsSalM);
                    return Ok(new
                    {
                        status = true,
                        message = clsSalM.InvNo > 0 ? "Pending sales invoice details loaded successfully." : "No pending sales invoice details found.",
                        Header = dtHdr.AsEnumerable().Select(row => dtHdr.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).FirstOrDefault(),
                        Items = dtItem.AsEnumerable().Select(row => dtItem.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).ToList()
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
    }
}