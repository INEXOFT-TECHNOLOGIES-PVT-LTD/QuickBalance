# Billing input (verbatim copies)

| # | Original path (relative to repository root) | Lines | Secrets replaced | Source encoding |
|---|---|---|---|---|
| 1 | QuickBalance.API/Controllers/Transactions/Sales/SalesInvoiceController.cs | 721 | none | utf-8 |
| 2 | QuickBalance.API/DB/SalesInvoiceDB.cs | 205 | none | utf-8 |
| 3 | QuickBalance.API/DTOs/SalesInvoiceRequest.cs | 112 | none | utf-8 |
| 4 | QuickBalance.API/docs/SALES-SQL/SI/1-ASK_UDT_SalesInvoiceItem.sql | 30 | none | utf-8 |
| 4 | QuickBalance.API/docs/SALES-SQL/SI/2-ASK_UDT_SalesInvoiceSerialNumber.sql | 12 | none | utf-8 |
| 4 | QuickBalance.API/docs/SALES-SQL/SI/3-ASK_UDT_SalesInvoiceAdditionalInfo.sql | 5 | none | utf-8 |
| 4 | QuickBalance.API/docs/SALES-SQL/SI/4-ASK_SP_ManageSalesInvoiceDetails.sql | 610 | none | utf-16 |
| 5 | QuickBalance.API/Program.cs | 506 | none | utf-8 |
| 6 | QuickBalance.API/Configuration/DB/IDbConnectionFactory.cs | 16 | none | utf-8 |
| 6 | QuickBalance.API/Configuration/DbConnectionFactory.cs | 80 | none | utf-8 |

Secret scan: no password or connection-string literal found, so nothing was replaced. All files found. Content is stored as UTF-8; source files with another encoding are listed in the last column.

## QuickBalance.API/Controllers/Transactions/Sales/SalesInvoiceController.cs

````csharp
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
````

## QuickBalance.API/DB/SalesInvoiceDB.cs

````csharp
using Microsoft.Data.SqlClient;
using QuickBalance.API.DTOs;
using System.Data;

namespace QuickBalance.API.DB
{
    public class SalesInvoiceDB
    {
        private readonly DBConn db;

        DataTable itemDetails = new();

        DataTable serialDetails = new();

        DataTable additionalDetails = new();

        public SalesInvoiceDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public void CreateItemTable(SalesInvoiceRequest request)
        {
            itemDetails = new();
            itemDetails.Columns.Add("SLNO", typeof(int));
            itemDetails.Columns.Add("PRCODE", typeof(int));
            itemDetails.Columns.Add("QTY", typeof(decimal));
            itemDetails.Columns.Add("RATE", typeof(decimal));
            itemDetails.Columns.Add("PRRATE", typeof(decimal));
            itemDetails.Columns.Add("MRP", typeof(decimal));
            itemDetails.Columns.Add("LDISCPERC", typeof(decimal));
            itemDetails.Columns.Add("LDISCPRICE", typeof(decimal));
            itemDetails.Columns.Add("DISCPERC", typeof(decimal));
            itemDetails.Columns.Add("DISCPRICE", typeof(decimal));
            itemDetails.Columns.Add("BATCH", typeof(string));
            itemDetails.Columns.Add("MFGDATE", typeof(DateTime));
            itemDetails.Columns.Add("EXPDATE", typeof(DateTime));
            itemDetails.Columns.Add("UNITCODE", typeof(int));
            itemDetails.Columns.Add("GDWCODE", typeof(int));
            itemDetails.Columns.Add("SORDNO", typeof(int));
            itemDetails.Columns.Add("ORDROWID", typeof(int));
            itemDetails.Columns.Add("ALCODE", typeof(string));
            itemDetails.Columns.Add("ALNAME", typeof(string));
            itemDetails.Columns.Add("CONVTYPE", typeof(int));
            itemDetails.Columns.Add("CONVFACT", typeof(decimal));
            itemDetails.Columns.Add("TAXABLE", typeof(decimal));
            itemDetails.Columns.Add("TAXCODE", typeof(int));
            itemDetails.Columns.Add("TAXPERC", typeof(decimal));
            itemDetails.Columns.Add("TAXAMOUNT", typeof(decimal));
            itemDetails.Columns.Add("NETAMOUNT", typeof(decimal));

            serialDetails = new();
            serialDetails.Columns.Add("INVRTYPE", typeof(string));
            serialDetails.Columns.Add("INVRNO", typeof(int));
            serialDetails.Columns.Add("INORDNO", typeof(int));
            serialDetails.Columns.Add("INSLNO", typeof(int));
            serialDetails.Columns.Add("PRCODE", typeof(int));
            serialDetails.Columns.Add("OUTSLNO", typeof(int));
            serialDetails.Columns.Add("UNITCODE", typeof(int));
            serialDetails.Columns.Add("GDWCODE", typeof(int));
            serialDetails.Columns.Add("ITEMSLNO", typeof(string));

            List<SalesInvoiceItemRequest> items = request.Items ?? new List<SalesInvoiceItemRequest>();

            foreach (var item in items)
            {
                item.Mrp = item.Mrp == 0 ? item.Rate : item.Mrp;
                item.AlCode = "0";
                item.AlName = "";
                item.GdwCode = item.GdwCode == 0 ? 1 : item.GdwCode;
                item.ConvFact = item.ConvFact == 0 ? 1 : item.ConvFact;
                item.MfgDate = item.MfgDate ?? request.Date;
                item.ExpDate = item.ExpDate ?? request.Date;
                item.NetAmount = item.Taxable + item.TaxAmount;

                itemDetails.Rows.Add(
                    item.SlNo,
                    item.PrCode,
                    item.Qty,
                    item.Rate,
                    item.PrRate,
                    item.Mrp,
                    item.LDiscPerc,
                    item.LDiscPrice,
                    item.DiscPerc,
                    item.DiscPrice,
                    item.Batch ?? string.Empty,
                    item.MfgDate.HasValue ? item.MfgDate.Value : DBNull.Value,
                    item.ExpDate.HasValue ? item.ExpDate.Value : DBNull.Value,
                    item.UnitCode,
                    item.GdwCode,
                    item.SordNo,
                    item.OrdRowId,
                    item.AlCode ?? string.Empty,
                    item.AlName ?? string.Empty,
                    item.ConvType,
                    item.ConvFact,
                    item.Taxable,
                    item.TaxCode,
                    item.TaxPerc,
                    item.TaxAmount,
                    item.NetAmount
                );

                //--

                List<SalesInvoiceSerialRequest> serialNumbers = item.SerialNumbers ?? new List<SalesInvoiceSerialRequest>();

                foreach (var serial in serialNumbers)
                {
                    serialDetails.Rows.Add(
                        serial.InvRType ?? string.Empty,
                        serial.InVrNo,
                        serial.InOrdNo,
                        serial.InSlNo,
                        item.PrCode,
                        item.SlNo,
                        item.UnitCode,
                        item.GdwCode,
                        serial.ItemSlNo ?? string.Empty
                    );
                }
            }

            //--

            additionalDetails = new();
            additionalDetails.Columns.Add("ACCID", typeof(int));
            additionalDetails.Columns.Add("AMOUNT", typeof(decimal));

            List<SalesInvoiceAdditionalRequest> additional = request.Additional ?? new List<SalesInvoiceAdditionalRequest>();

            foreach (var item in additional)
            {
                additionalDetails.Rows.Add(
                    item.AccId,
                    item.Amount
                );
            }
        }

        public async Task<DataTable> ManageSalesInvoice(SalesInvoiceRequest request, int UserId, string idempotencyKey, string requestHash)
        {
            request.PayMode = request.PayMode == null || request.PayMode.ToUpper() == "CASH" ? "Cash" : request.PayMode.ToString().ToUpper() == "CREDIT" ? "Credit" : "Bank";
            request.DueDate = request.DueDate ?? request.Date;
            request.Pos = request.Pos != null && request.Pos.ToUpper() == "Y" ? "Y" : "N";
            request.PcName = "API";

            CreateItemTable(request);

            List<SqlParameter> parameters = new();

            parameters.Add(new SqlParameter("@SINVNO", SqlDbType.Int) { Value = request.SinvNo });
            parameters.Add(new SqlParameter("@DATE", SqlDbType.DateTime) { Value = request.Date });
            parameters.Add(new SqlParameter("@CUSCODE", SqlDbType.Int) { Value = request.CustomerCode });
            parameters.Add(new SqlParameter("@PAYMODE", SqlDbType.NVarChar, 100) { Value = request.PayMode });
            parameters.Add(new SqlParameter("@CLCODE", SqlDbType.Int) { Value = request.CashLedgerCode });
            parameters.Add(new SqlParameter("@BLCODE", SqlDbType.Int) { Value = request.BankLedgerCode });
            parameters.Add(new SqlParameter("@DUEDATE", SqlDbType.DateTime) { Value = request.DueDate });
            parameters.Add(new SqlParameter("@SMCODE", SqlDbType.Int) { Value = request.SalesmanCode });
            parameters.Add(new SqlParameter("@POS", SqlDbType.NVarChar, 10) { Value = request.Pos });
            parameters.Add(new SqlParameter("@PCNAME", SqlDbType.NVarChar, -1) { Value = request.PcName });
            parameters.Add(new SqlParameter("@DISCPERC", SqlDbType.Decimal) { Precision = 18, Scale = 6, Value = request.DiscPerc });
            parameters.Add(new SqlParameter("@DISCAMT", SqlDbType.Decimal) { Precision = 18, Scale = 6, Value = request.DiscAmt });
            parameters.Add(new SqlParameter("@ROUNDOFF", SqlDbType.Decimal) { Precision = 18, Scale = 6, Value = request.RoundOff });
            parameters.Add(new SqlParameter("@USERID", SqlDbType.Int) { Value = UserId });
            parameters.Add(new SqlParameter("@IDEMPOTENCYKEY", SqlDbType.NVarChar, 100)
            {
                Value = idempotencyKey
            });
            parameters.Add(new SqlParameter("@REQUESTHASH",SqlDbType.Char, 64) { Value = requestHash});

            SqlParameter itemParameter = new SqlParameter("@ITEMDTLS", SqlDbType.Structured) { TypeName = "dbo.ASK_UDT_SalesInvoiceItem", Value = itemDetails };
            parameters.Add(itemParameter);

            SqlParameter serialParameter = new SqlParameter("@SLNODTLS", SqlDbType.Structured) { TypeName = "dbo.ASK_UDT_SalesInvoiceSerialNumber", Value = serialDetails };
            parameters.Add(serialParameter);

            SqlParameter additionalParameter = new SqlParameter("@ADDIDTLS", SqlDbType.Structured) { TypeName = "dbo.ASK_UDT_SalesInvoiceAdditionalInfo", Value = additionalDetails };
            parameters.Add(additionalParameter);

            return await db.ExecuteStoredProcedure("ASK_SP_ManageSalesInvoiceDetails", parameters);
        }

        public async Task<DataTable> CancelSalesInvoice(int SinvNo, int UserId, CommonFunction commonFunction)
        {
            List<SqlParameter> parameters = new();

            parameters.Add(new SqlParameter("@SINVNO", SqlDbType.Int) { Value = SinvNo });
            parameters.Add(new SqlParameter("@USERID", SqlDbType.Int) { Value = UserId });

            return await db.ExecuteStoredProcedure("ASK_SP_CancelSalesInvoiceDetails", parameters);
        }

        public async Task<DataTable> PendingSalesInvoicesInReturn(int salesReturnId, string salesInvoiceNumber)
        {
            List<SqlParameter> parameters = new();

            parameters.Add(new SqlParameter("@SALESRETURNID", SqlDbType.Int) { Value = salesReturnId });
            parameters.Add(new SqlParameter("@SALESINVOICENUMBER", SqlDbType.NVarChar, -1) { Value = salesInvoiceNumber });

            return await db.ExecuteStoredProcedure("ASK_SP_PendingSalesInvoicesInReturn", parameters);
        }
    }
}
````

## QuickBalance.API/DTOs/SalesInvoiceRequest.cs

````csharp
namespace QuickBalance.API.DTOs
{
    public class SalesInvoiceRequest
    {
        public int SinvNo { get; set; }

        public DateTime Date { get; set; }

        public int CustomerCode { get; set; }

        public string? PayMode { get; set; }

        public int CashLedgerCode { get; set; }

        public int BankLedgerCode { get; set; }

        public DateTime? DueDate { get; set; }

        public int SalesmanCode { get; set; }

        public string? Pos { get; set; }

        public string? PcName { get; set; }

        public decimal DiscPerc { get; set; }

        public decimal DiscAmt { get; set; }

        public decimal RoundOff { get; set; }

        public List<SalesInvoiceItemRequest> Items { get; set; } = new();

        public List<SalesInvoiceAdditionalRequest> Additional { get; set; } = new();
    }

    public class SalesInvoiceItemRequest
    {
        public int SlNo { get; set; }

        public int PrCode { get; set; }

        public decimal Qty { get; set; }

        public decimal Rate { get; set; }

        public decimal PrRate { get; set; }

        public decimal Mrp { get; set; }

        public decimal LDiscPerc { get; set; }

        public decimal LDiscPrice { get; set; }

        public decimal DiscPerc { get; set; }

        public decimal DiscPrice { get; set; }

        public string Batch { get; set; } = string.Empty;

        public DateTime? MfgDate { get; set; }

        public DateTime? ExpDate { get; set; }

        public int UnitCode { get; set; }

        public int GdwCode { get; set; }

        public int SordNo { get; set; }

        public int OrdRowId { get; set; }

        public string AlCode { get; set; } = string.Empty;

        public string AlName { get; set; } = string.Empty;

        public int ConvType { get; set; }

        public decimal ConvFact { get; set; }

        public decimal Taxable { get; set; }

        public int TaxCode { get; set; }

        public decimal TaxPerc { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal NetAmount { get; set; }

        public List<SalesInvoiceSerialRequest> SerialNumbers { get; set; } = new();
    }

    public class SalesInvoiceSerialRequest
    {
        public string InvRType { get; set; } = string.Empty;

        public int InVrNo { get; set; }

        public int InOrdNo { get; set; }

        public int InSlNo { get; set; }

        public string ItemSlNo { get; set; } = string.Empty;
    }

    public class SalesInvoiceAdditionalRequest
    {
        public int AccId { get; set; }

        public decimal Amount { get; set; }
    }
}
````

## QuickBalance.API/docs/SALES-SQL/SI/1-ASK_UDT_SalesInvoiceItem.sql

````sql
CREATE TYPE [dbo].[ASK_UDT_SalesInvoiceItem] AS TABLE(
	[SLNO] [int] NULL,
	[PRCODE] [int] NULL,
	[QTY] [decimal](18, 6) NULL,
	[RATE] [decimal](18, 6) NULL,
	[PRRATE] [decimal](18, 6) NULL,
	[MRP] [decimal](18, 6) NULL,
	[LDISCPERC] [decimal](18, 6) NULL,
	[LDISCPRICE] [decimal](18, 6) NULL,
	[DISCPERC] [decimal](18, 6) NULL,
	[DISCPRICE] [decimal](18, 6) NULL,
	[BATCH] [nvarchar](max) NULL,
	[MFGDATE] [datetime] NULL,
	[EXPDATE] [datetime] NULL,
	[UNITCODE] [int] NULL,
	[GDWCODE] [int] NULL,
	[SORDNO] [int] NULL,
	[ORDROWID] [int] NULL,
	[ALCODE] [nvarchar](max) NULL,
	[ALNAME] [nvarchar](max) NULL,
	[CONVTYPE] [int] NULL,
	[CONVFACT] [decimal](18, 6) NULL,
	[TAXABLE] [decimal](18, 6) NULL,
	[TAXCODE] [int] NULL,
	[TAXPERC] [decimal](18, 6) NULL,
	[TAXAMOUNT] [decimal](18, 6) NULL,
	[NETAMOUNT] [decimal](18, 6) NULL
)


````

## QuickBalance.API/docs/SALES-SQL/SI/2-ASK_UDT_SalesInvoiceSerialNumber.sql

````sql
CREATE TYPE [dbo].[ASK_UDT_SalesInvoiceSerialNumber] AS TABLE(
	[INVRTYPE] [nvarchar](max) NULL,
	[INVRNO] [int] NULL,
	[INORDNO] [int] NULL,
	[INSLNO] [int] NULL,
	[PRCODE] [int] NULL,
	[OUTSLNO] [int] NULL,
	[UNITCODE] [int] NULL,
	[GDWCODE] [int] NULL,
	[ITEMSLNO] [nvarchar](max) NULL
)

````

## QuickBalance.API/docs/SALES-SQL/SI/3-ASK_UDT_SalesInvoiceAdditionalInfo.sql

````sql
CREATE TYPE [dbo].[ASK_UDT_SalesInvoiceAdditionalInfo] AS TABLE(
	[ACCID] [int] NULL,
	[AMOUNT] [decimal](18, 6) NULL
)

````

## QuickBalance.API/docs/SALES-SQL/SI/4-ASK_SP_ManageSalesInvoiceDetails.sql

````sql
-- ==================================================
-- Author:		Ashique C
-- Create date: 10 Sep 2026
-- Description:	Validate and Save/Update Sales Invoice Details
-- ==================================================
CREATE PROCEDURE [dbo].[ASK_SP_ManageSalesInvoiceDetails]
	@SINVNO INT,
	@DATE DATETIME,
	@CUSCODE INT,
	@PAYMODE NVARCHAR(100),
	@CLCODE INT,
	@BLCODE INT,
	@DUEDATE DATETIME,
	@SMCODE INT,
	@POS NVARCHAR(10),
	@PCNAME NVARCHAR(MAX),
	@DISCPERC DECIMAL(18, 6),
	@DISCAMT DECIMAL(18, 6),
	@ROUNDOFF DECIMAL(18, 6),
	@USERID INT,
	@ITEMDTLS AS ASK_UDT_SalesInvoiceItem READONLY,
	@SLNODTLS AS ASK_UDT_SalesInvoiceSerialNumber READONLY,
	@ADDIDTLS AS ASK_UDT_SalesInvoiceAdditionalInfo READONLY
AS
BEGIN	
	SET NOCOUNT ON;
	BEGIN TRY
	BEGIN TRAN

	IF OBJECT_ID('tempdb..#TEMP_SI') IS NOT NULL
    DROP TABLE #TEMP_SI

	--## For Update
	DECLARE @SINVORDNO INT = 0, @GDNINVNO INT = 0, @GDNORDNO INT = 0
	IF(@SINVNO <> 0)
	BEGIN
		SELECT @SINVORDNO = ORDNO, @GDNORDNO = GDNNO FROM SALM WHERE INVNO = @SINVNO
		IF(@GDNORDNO <> 0)
			SELECT @GDNINVNO = INVNO FROM GDNM WHERE STYPE = 'DN' AND ORDNO = @GDNORDNO
	END

	--## Variable Declaration
	DECLARE @ALTUNITWISE BIT, @DATEBLOCK BIT, @SUDATEBLOCK INT, @TAGACCMSTR INT, @COSTC BIT, @BATCHWISE BIT, @BATCHRTEDIT BIT, @SLNOWISE BIT, @NEGSTOCK BIT, @MULTIALIAS BIT
	SELECT  @ALTUNITWISE = ALTUNIT, @DATEBLOCK = DATEBLOCK, @SUDATEBLOCK = SUDATEBLOCK, @TAGACCMSTR = TAGACCMSTR, @COSTC = COSTC, @BATCHWISE = BATCHWISE, @BATCHRTEDIT = BATCHRTEDIT, @SLNOWISE = SLNOTRACK, @NEGSTOCK = NEGSTOCK, @MULTIALIAS = MULTIALIAS FROM CONFIGTB

	DECLARE @CASHCD INT, @DISCCD INT, @COINADJCD INT
	SELECT @CASHCD = CASHCD, @DISCCD = DISCCD, @COINADJCD = COINADJCD FROM FYDETAILS WHERE PREVFIN = 0
	SET @CLCODE = CASE WHEN @PAYMODE = 'Cash' AND @CLCODE = 0 THEN @CASHCD ELSE @CLCODE END

	DECLARE @USERTYPE NVARCHAR(MAX) = (SELECT USERTYPE FROM TBUSER WHERE USERCODE = @USERID)
	DECLARE @ALLOWMINSALESPRICE INT = (SELECT COUNT(*) FROM USER_TEMP WHERE USERCODE = @USERID AND MNCODE = 'chk_minSalesPrice')
	DECLARE @SLNOUPDATE TABLE (INVRTYPE NVARCHAR(MAX), INVRNO INT, INORDNO INT, INSLNO INT, PRCODE INT, OUTSLNO INT, GDWCODE INT, UNITCODE INT, ITEMSLNO NVARCHAR(MAX))
		
	DECLARE @BTYPE NVARCHAR(1) = CASE WHEN @PAYMODE = 'Cash' THEN 'C' WHEN @PAYMODE = 'Credit' THEN 'R' ELSE 'B'  END
	DECLARE @ADJCD INT = (CASE WHEN @PAYMODE = 'Cash' THEN @CLCODE WHEN @PAYMODE = 'Bank' THEN @BLCODE ELSE 0 END)		

	SELECT H.SLNO, H.PRCODE, P.PRNAME, GDWCODE, H.UNITCODE, P.UNIT AS MAINUNIT, P.ALTUNIT AS ALTUNIT, H.ALCODE, H.ALNAME,

	H.QTY, H.QTY AS CONVQTY,
	H.RATE, H.RATE AS CONVRATE,

	ISNULL(H.PRRATE, CAST(0 AS DECIMAL(18,6))) AS PRRATE, ISNULL(H.PRRATE, CAST(0 AS DECIMAL(18,6))) AS CONVPRRATE,
	ISNULL(H.MRP, CAST(0 AS DECIMAL(18,6))) AS MRP, ISNULL(H.MRP, CAST(0 AS DECIMAL(18,6))) AS CONVMRP,

	ISNULL(H.LDISCPERC, CAST(0 AS DECIMAL(18,6))) AS LDISCPERC, ISNULL(H.LDISCPRICE, CAST(0 AS DECIMAL(18,6))) AS LDISCPRICE,
	ISNULL(H.DISCPERC, CAST(0 AS DECIMAL(18,6))) AS DISCPERC, ISNULL(H.DISCPRICE, CAST(0 AS DECIMAL(18,6))) AS DISCPRICE,

	ISNULL(H.BATCH, '') AS BATCH, ISNULL(H.MFGDATE, @DATE) AS MFGDATE, ISNULL(H.EXPDATE, @DATE) AS EXPDATE,
	P.BASEMINPRICE, P.ALTSALEPRICE, ISNULL(H.TAXABLE,CAST(0 AS DECIMAL(18,6))) AS TAXABLE, H.TAXCODE, ISNULL(H.TAXPERC,CAST(0 AS DECIMAL(18,6))) AS TAXPERC, ISNULL(H.TAXAMOUNT,CAST(0 AS DECIMAL(18,6))) AS TAXAMOUNT, ISNULL(H.NETAMOUNT,CAST(0 AS DECIMAL(18,6))) AS NETAMOUNT,
	H.SORDNO, H.ORDROWID,CASE WHEN @ALTUNITWISE = 1 AND P.ALTUNIT > 0 AND P.SALESPRICETYPE = 1 THEN 1 ELSE 0 END AS PRFLG, (CASE WHEN H.SORDNO <> 0 THEN H.CONVTYPE ELSE P.CONVTYPE END) AS CONVTYPE, (CASE WHEN H.SORDNO <> 0 THEN H.CONVFACT ELSE P.UNITCONV END) AS CONVFACT,
	P.SERVFLG, P.BATCHFLG, P.SLNOTRACK INTO #TEMP_SI FROM @ITEMDTLS H INNER JOIN PRMAST P ON H.PRCODE = P.PRCODE
	
	DECLARE @RESULT NVARCHAR(MAX) = ''
	IF (@SINVNO <> 0 AND (ISNULL(@SINVORDNO, 0) = 0 OR ISNULL(@GDNORDNO, 0) = 0))
		SET @RESULT = 'Delivery note details are missing for this invoice. Sorry, the voucher cannot be updated.'
	ELSE IF (SELECT COUNT(*) FROM #TEMP_SI) = 0
		SET @RESULT = 'Item Details Must not be blank'
	ELSE IF EXISTS (SELECT * FROM CUSMAS WHERE ACCID = @CUSCODE AND COUNTRYCODE <> 0 AND COUNTRYCODE <> (SELECT TOP 1 COUNTRY FROM COMPMAS))	
		SET @RESULT = 'Sales invoice creation is restricted. Outside parties are not allowed.'

	--//

	IF(@RESULT = '')
	BEGIN		
		DECLARE @ROWNO INT= 1
		DECLARE @MinSLNO INT = (SELECT MIN(SLNO) FROM #TEMP_SI)
		DECLARE @MaxSLNO INT = (SELECT MAX(SLNO) FROM #TEMP_SI)
		WHILE (@MinSLNO <= @MaxSLNO)
		BEGIN

			IF (@RESULT <> '') BREAK
			
			DECLARE @PRCODE INT, @PRNAME NVARCHAR(MAX), @SERVFLG BIT, @BATCHFLG BIT, @BATCH NVARCHAR(MAX), @MFGDATE DATETIME, @EXPDATE DATETIME, @SLNOTRACK BIT, @GDWCODE INT, @UNITCODE INT, @MAINUNIT INT, @ALTUNIT INT, @SORDNO INT, @ORDROWID INT, @QTY DECIMAL(18, 6), @ALCODE NVARCHAR(MAX), @ALNAME NVARCHAR(MAX), @RATE DECIMAL(18, 6), @PRRATE DECIMAL(18, 6), @MRP DECIMAL(18, 6), @CONVTYPE INT, @CONVFACT DECIMAL(18, 6), @BASEMINPRICE DECIMAL(18, 6), @ALTSALEPRICE DECIMAL(18, 6), @TAXABLEAMT DECIMAL(18, 6)
			SELECT @PRCODE = PRCODE, @PRNAME = PRNAME, @SERVFLG = SERVFLG, @BATCHFLG = BATCHFLG, @BATCH = BATCH, @MFGDATE = MFGDATE, @EXPDATE = EXPDATE, @SLNOTRACK = SLNOTRACK, @GDWCODE = GDWCODE, @UNITCODE = UNITCODE, @MAINUNIT = MAINUNIT, @ALTUNIT = ALTUNIT, @SORDNO = SORDNO, @ORDROWID = ORDROWID, @QTY = QTY, @ALCODE = ALCODE, @ALNAME = ALNAME, @RATE = RATE, @PRRATE = PRRATE, @MRP = MRP, @CONVTYPE = CONVTYPE, @CONVFACT = CONVFACT, @BASEMINPRICE = BASEMINPRICE, @ALTSALEPRICE = ALTSALEPRICE, @TAXABLEAMT = TAXABLE FROM #TEMP_SI WHERE SLNO = @MinSLNO

			--## Get Alias Quantity
			DECLARE @ALQTY DECIMAL(18, 6) = 0
			SELECT @ALQTY = ISNULL(MAX(A.QTY),0), @ALNAME = ISNULL(MAX(A.ALNAME),@ALNAME) FROM PRMAST P INNER JOIN PRALIAS A ON P.PRCODE = A.PRCODE WHERE P.PRCODE = @PRCODE AND (CAST(A.CODE AS nvarchar(MAX)) = @ALCODE OR A.ALNAME = @ALNAME)			

			--## Convert to main unit
			DECLARE @CONVQTY DECIMAL(18, 6) = @QTY, @CONVRATE DECIMAL(18, 6) = @RATE, @CONVPRRATE DECIMAL(18, 6) = @PRRATE, @CONVMRP DECIMAL(18, 6) = @MRP
			SET @CONVQTY = (CASE WHEN @MULTIALIAS = 1 AND ISNULL(@ALCODE,'') <> '' AND @ALQTY <> 0 THEN @ALQTY ELSE CAST(1 AS DECIMAL(18, 6)) END) * (CASE WHEN @UNITCODE = @MAINUNIT THEN @CONVQTY WHEN @CONVTYPE = 0 AND @CONVFACT <> 0 THEN @CONVQTY*@CONVFACT WHEN @CONVTYPE = 1 AND @CONVFACT <> 0 THEN @CONVQTY/@CONVFACT ELSE  @CONVQTY END)
			SET @CONVRATE = (CASE WHEN @UNITCODE = @MAINUNIT THEN @CONVRATE WHEN @CONVTYPE = 0 AND @CONVFACT <> 0 THEN @CONVRATE/@CONVFACT WHEN @CONVTYPE = 1 AND @CONVFACT <> 0 THEN @CONVRATE*@CONVFACT ELSE  @CONVRATE END) / (CASE WHEN @MULTIALIAS = 1 AND ISNULL(@ALCODE,'') <> '' AND @ALQTY <> 0 THEN @ALQTY ELSE CAST(1 AS DECIMAL(18, 6)) END)
			SET @CONVPRRATE = (CASE WHEN @UNITCODE = @MAINUNIT THEN @CONVPRRATE WHEN @CONVTYPE = 0 AND @CONVFACT <> 0 THEN @CONVPRRATE/@CONVFACT WHEN @CONVTYPE = 1 AND @CONVFACT <> 0 THEN @CONVPRRATE*@CONVFACT ELSE  @CONVPRRATE END)
			SET @CONVMRP = (CASE WHEN @UNITCODE = @MAINUNIT THEN @CONVMRP WHEN @CONVTYPE = 0 AND @CONVFACT <> 0 THEN @CONVMRP/@CONVFACT WHEN @CONVTYPE = 1 AND @CONVFACT <> 0 THEN @CONVMRP*@CONVFACT ELSE  @CONVMRP END)

			--## Update #TEMP_SI			
			UPDATE #TEMP_SI SET ALNAME = @ALNAME, CONVQTY = @CONVQTY, CONVRATE = @CONVRATE, CONVPRRATE = @CONVPRRATE, CONVMRP = @CONVMRP WHERE SLNO = @MinSLNO
			
			--## Validate Tagged Voucher
			IF(@SORDNO <> 0)
			BEGIN
				DECLARE @PENDINGQTY DECIMAL(18, 6) = 0
				IF(@ORDROWID > 0)
				BEGIN
					SELECT @PENDINGQTY = ORDTLS.QTY - (ISNULL(NTDTLS.QTY, 0) + ISNULL(TRDTLS.QTY, 0) + ISNULL(TRNTDTLS.QTY, 0)) FROM 
					((((SELECT ORDNO, NTNO, TRNNO, PRCODE, ORDROWID, SUM(CONVQTY) AS QTY FROM STKDT WHERE VRTYPE = 'O' AND TYPE = 'I' AND STKDT.CLOSES = 'FALSE' AND PARTYCD = @CUSCODE AND PRCODE = @PRCODE AND ORDNO = @SORDNO AND ORDROWID = @ORDROWID GROUP BY ORDNO, NTNO, TRNNO, PRCODE, ORDROWID) AS ORDTLS
					LEFT JOIN (SELECT ORDNO, PRCODE, ORDROWID, SUM(CONVQTY) AS QTY FROM STKDT WHERE VRTYPE = 'NT' AND TYPE = 'I' AND IIF(PRV IS NULL,'N',PRV) <> 'Y' AND ORDNO <> 0 AND TRNNO = 0 AND PARTYCD = @CUSCODE GROUP BY ORDNO, PRCODE, ORDROWID) AS NTDTLS ON ORDTLS.ORDNO = NTDTLS.ORDNO AND ORDTLS.PRCODE = NTDTLS.PRCODE AND ORDTLS.ORDROWID = NTDTLS.ORDROWID)
					LEFT JOIN (SELECT ORDNO, PRCODE, ORDROWID, SUM(CONVQTY) AS QTY FROM STKDT WHERE VRTYPE = 'TR' AND TYPE = 'I' AND IIF(PRV IS NULL,'N',PRV) <> 'Y' AND ORDNO <> 0 AND NTNO = 0 AND PARTYCD = @CUSCODE GROUP BY ORDNO, PRCODE, ORDROWID) AS TRDTLS ON ORDTLS.ORDNO = TRDTLS.ORDNO AND ORDTLS.PRCODE = TRDTLS.PRCODE AND ORDTLS.ORDROWID = TRDTLS.ORDROWID)
					LEFT JOIN (SELECT SUBTRNTDTLS.ORDNO, SUBTRNTDTLS.PRCODE, SUBTRNTDTLS.ORDROWID, SUM(SUBTRNTDTLS.QTY) AS QTY FROM ( SELECT SUBTRDTLS.ORDNO, SUBTRDTLS.PRCODE, SUBTRDTLS.ORDROWID, SUBTRDTLS.QTY FROM (SELECT ORDNO, NTNO, TRNNO, PRCODE, ORDROWID, SUM(CONVQTY) AS QTY FROM STKDT WHERE VRTYPE = 'TR' AND TYPE = 'I' AND IIF(PRV IS NULL,'N',PRV) <> 'Y' AND ORDNO <> 0 AND NTNO <> 0 AND PARTYCD = @CUSCODE GROUP BY ORDNO, NTNO, TRNNO, PRCODE, ORDROWID) AS SUBTRDTLS 
					LEFT JOIN (SELECT ORDNO, NTNO, TRNNO, PRCODE, ORDROWID FROM STKDT WHERE VRTYPE = 'NT' AND TYPE = 'I' AND IIF(PRV IS NULL,'N',PRV) <> 'Y' AND ORDNO <> 0 AND TRNNO = 0 AND PARTYCD = @CUSCODE GROUP BY ORDNO, NTNO, TRNNO, PRCODE, ORDROWID) AS SUBNTDTLS ON SUBTRDTLS.ORDNO = SUBNTDTLS.ORDNO AND SUBTRDTLS.NTNO = SUBNTDTLS.NTNO AND SUBTRDTLS.PRCODE = SUBNTDTLS.PRCODE AND SUBTRDTLS.ORDROWID = SUBNTDTLS.ORDROWID WHERE SUBNTDTLS.ORDNO IS NULL ) AS SUBTRNTDTLS GROUP BY  SUBTRNTDTLS.ORDNO, SUBTRNTDTLS.PRCODE, SUBTRNTDTLS.ORDROWID ) AS TRNTDTLS ON ORDTLS.ORDNO = TRNTDTLS.ORDNO AND ORDTLS.PRCODE = TRNTDTLS.PRCODE AND ORDTLS.ORDROWID = TRNTDTLS.ORDROWID)
					WHERE ORDTLS.QTY - (ISNULL(NTDTLS.QTY, 0) + ISNULL(TRDTLS.QTY, 0) + ISNULL(TRNTDTLS.QTY, 0)) > 0 AND ORDTLS.ORDNO NOT IN (SELECT CORDM.ORDNO FROM CORDM WHERE (IIF(CORDM.TRANSFERRED IS NULL, 'N', CORDM.TRANSFERRED) = 'Y') AND CORDM.STYPE = 'O')
				END
				ELSE
				BEGIN
					SELECT @PENDINGQTY = ORDTLS.QTY - (ISNULL(NTDTLS.QTY, 0) + ISNULL(TRDTLS.QTY, 0) + ISNULL(TRNTDTLS.QTY, 0)) FROM 
					((((SELECT ORDNO, NTNO, TRNNO, PRCODE, SUM(CONVQTY) AS QTY FROM STKDT WHERE VRTYPE = 'O' AND TYPE = 'I' AND (STKDT.TYPE = 'R' OR STKDT.CLOSES = 'FALSE') AND PARTYCD = @CUSCODE AND PRCODE = @PRCODE AND ORDNO = @SORDNO GROUP BY ORDNO, NTNO, TRNNO, PRCODE) AS ORDTLS
					LEFT JOIN (SELECT ORDNO, PRCODE, SUM(CONVQTY) AS QTY FROM STKDT WHERE VRTYPE = 'NT' AND TYPE = 'I' AND IIF(PRV IS NULL,'N',PRV) <> 'Y' AND ORDNO <> 0 AND TRNNO = 0 AND PARTYCD = @CUSCODE GROUP BY ORDNO, PRCODE) AS NTDTLS ON ORDTLS.ORDNO = NTDTLS.ORDNO AND ORDTLS.PRCODE = NTDTLS.PRCODE)
					LEFT JOIN (SELECT ORDNO, PRCODE, SUM(CONVQTY) AS QTY FROM STKDT WHERE VRTYPE = 'TR' AND TYPE = 'I' AND IIF(PRV IS NULL,'N',PRV) <> 'Y' AND ORDNO <> 0 AND NTNO = 0 AND PARTYCD = @CUSCODE GROUP BY ORDNO, PRCODE) AS TRDTLS ON ORDTLS.ORDNO = TRDTLS.ORDNO AND ORDTLS.PRCODE = TRDTLS.PRCODE)
					LEFT JOIN ( SELECT SUBTRDTLS.ORDNO, SUBTRDTLS.PRCODE, SUBTRDTLS.QTY FROM (SELECT ORDNO, NTNO, TRNNO, PRCODE, SUM(CONVQTY) AS QTY FROM STKDT WHERE VRTYPE = 'TR' AND TYPE = 'I' AND IIF(PRV IS NULL,'N',PRV) <> 'Y' AND ORDNO <> 0 AND NTNO <> 0 AND PARTYCD = @CUSCODE GROUP BY ORDNO, NTNO, TRNNO, PRCODE) AS SUBTRDTLS 
					LEFT JOIN (SELECT ORDNO, NTNO, TRNNO, PRCODE FROM STKDT WHERE VRTYPE = 'NT' AND TYPE = 'I' AND IIF(PRV IS NULL,'N',PRV) <> 'Y' AND ORDNO <> 0 AND TRNNO = 0 AND PARTYCD = @CUSCODE GROUP BY ORDNO, NTNO, TRNNO, PRCODE) AS SUBNTDTLS ON SUBTRDTLS.ORDNO = SUBNTDTLS.ORDNO AND SUBTRDTLS.NTNO = SUBNTDTLS.NTNO AND SUBTRDTLS.PRCODE = SUBNTDTLS.PRCODE WHERE SUBNTDTLS.ORDNO IS NULL) AS TRNTDTLS ON ORDTLS.ORDNO = TRNTDTLS.ORDNO AND ORDTLS.PRCODE = TRNTDTLS.PRCODE)
					WHERE ORDTLS.QTY - (ISNULL(NTDTLS.QTY, 0) + ISNULL(TRDTLS.QTY, 0) + ISNULL(TRNTDTLS.QTY, 0)) > 0 AND ORDTLS.ORDNO NOT IN (SELECT CORDM.ORDNO FROM CORDM WHERE (IIF(CORDM.TRANSFERRED IS NULL, 'N', CORDM.TRANSFERRED) = 'Y') AND CORDM.STYPE = 'O')

					--## Reduced already existsed quantity.
					SET @PENDINGQTY = ISNULL(@PENDINGQTY, CAST(0 AS DECIMAL(18, 6))) - (SELECT ISNULL(SUM(CONVQTY),0) FROM #TEMP_SI WHERE SORDNO = @SORDNO AND PRCODE = @PRCODE AND SLNO < @MinSLNO)
				END

				DECLARE @PRVSOQTY DECIMAL(18, 6) = CASE WHEN @SINVNO = 0 THEN @SINVNO ELSE (SELECT ISNULL(SUM(CONVQTY), 0) FROM SALT WHERE INVNO = @SINVNO AND SORDNO = @SORDNO AND ISNULL(PRV, '') <> 'Y' AND (ORDROWID = @ORDROWID OR @ORDROWID = 0) AND PRCODE = @PRCODE) END

				IF(@CONVQTY - @PRVSOQTY > @PENDINGQTY)
					SET @RESULT = 'The entered quantity is exceeds the available pending quantity of for Sales Order Voucher '''+(SELECT ISNULL(MAX(INVNOTEXT),'') FROM CORDM WHERE STYPE = 'O' AND ORDNO = @SORDNO)+''' for the product: '''+@PRNAME+''' at Row: '+CAST(@ROWNO AS NVARCHAR)+'. Please adjust the quantity to proceed.'
			END

			--## Stock Validation
			IF(@RESULT = '' AND @SERVFLG = 0 AND @NEGSTOCK = 0)
			BEGIN
				DECLARE @STOCK NUMERIC(18,6)
				SELECT @STOCK = ISNULL(SUM(ISNULL(STK.QTY,0)),0) FROM (
				SELECT IIF(S.PRCODE IS NULL, P.OPNSTOCK, S.CONVQTY) AS QTY FROM PRMAST P LEFT JOIN OPNSTK S ON P.PRCODE = S.PRCODE WHERE P.PRCODE = @PRCODE AND IIF(S.PRCODE IS NULL, 1, S.GDWCODE) = @GDWCODE
				UNION ALL
				SELECT CONVQTY AS QTY FROM SALT WHERE PRCODE = @PRCODE AND GDWCODE = @GDWCODE AND RETURNFLG = 1 AND CANCELFLG = 0 AND SALT.INVDATE <= @DATE
				UNION ALL
				SELECT CASE WHEN STKFLG = 'R' THEN CONVQTY WHEN STKFLG = 'I' THEN -1 * CONVQTY ELSE 0 END AS QTY FROM PURT D INNER JOIN (SELECT PURM.VRNO,PURM.STATUS FROM PURM) M ON D.VRNO = M.VRNO WHERE STATUS = 'Updated' AND PRCODE = @PRCODE AND D.GDWCODE = @GDWCODE AND VRTYPE <> 'P' AND CANCELFLG = 0 AND PURDATE <= @DATE
				UNION ALL
				SELECT CONVQTY AS QTY FROM GRNT WHERE PRCODE = @PRCODE AND GDWCODE = @GDWCODE AND CANCELFLG = 0 AND PURDATE <= @DATE
				UNION ALL
				SELECT -1 * CONVQTY AS QTY FROM GDNT WHERE PRCODE = @PRCODE AND GDWCODE = @GDWCODE AND CANCELFLG = 0 AND INVDATE <= @DATE
				UNION ALL
				SELECT CASE WHEN D.TYPE='G' THEN D.CONVQTY WHEN D.TYPE='C' THEN -1 * D.CONVQTY ELSE 0 END AS QTY FROM PRDNM H INNER JOIN PRDNT D ON H.ORDNO = D.ORDNO WHERE D.PRCODE = @PRCODE AND D.GDWCODE = @GDWCODE AND H.CANCELFLG = 0 AND H.VRDATE <= @DATE ) STK			

				DECLARE @USEDQTY DECIMAL(18, 6) = (SELECT ISNULL(SUM(CONVQTY),0) FROM #TEMP_SI WHERE PRCODE = @PRCODE AND GDWCODE = @GDWCODE AND SLNO < @MinSLNO)
				DECLARE @PRVQTY DECIMAL(18, 6) = CASE WHEN @SINVNO = 0 THEN 0 ELSE (SELECT ISNULL(SUM(CONVQTY),0) FROM SALT WHERE PRCODE = @PRCODE AND GDWCODE = @GDWCODE AND INVNO = @SINVNO) END

				IF(@CONVQTY + @USEDQTY - @PRVQTY > @STOCK)
					SET @RESULT = 'Entered quanity is greater than available stock for product: '''+@PRNAME+''' at Row: '+CAST(@ROWNO AS NVARCHAR)
			END

			--## Batch Validation
			IF(@RESULT = '' AND @BATCHWISE = 1 AND @BATCHFLG = 1)
			BEGIN
				IF(@BATCH = '')
				BEGIN
					SET @RESULT = 'Missing batch details Product: '+@PRNAME+' at Row: '+CAST(@ROWNO AS NVARCHAR)
				END
				ELSE
				BEGIN					
					DECLARE @USEDQTY_B DECIMAL(18,6) = ISNULL((SELECT SUM(ISNULL(B.CONVQTY,0)) FROM PRMAST P INNER JOIN #TEMP_SI B ON P.PRCODE = B.PRCODE WHERE P.PRCODE = @PRCODE AND B.BATCH = @BATCH AND B.GDWCODE = @GDWCODE AND (@BATCHRTEDIT = 1 OR (B.CONVRATE = @CONVRATE AND B.CONVMRP = @CONVMRP)) AND (ISNULL(P.MANDATEFLG,0) = 0 OR B.MFGDATE = @MFGDATE) AND (ISNULL(P.EXPDATEFLG,0) = 0 OR B.EXPDATE = @EXPDATE) AND SLNO < @MinSLNO),0)
					DECLARE @PRVQTY_B DECIMAL(18,6) = CASE WHEN @SINVNO = 0 THEN 0 ELSE (SELECT ISNULL(SUM(ISNULL(B.CONVQTY, 0)), 0) FROM PRMAST P INNER JOIN BATCHDET B ON P.PRCODE = B.PRCODE WHERE B.BATCH = @BATCH AND B.PRCODE = @PRCODE AND B.STYPE = 'O' AND B.VRNO = @SINVNO AND B.GRACK = @GDWCODE) END
					DECLARE @STOCK_B DECIMAL(18,6) = (SELECT ISNULL(SUM(ISNULL(B.BALQTY, 0)), 0) FROM PRMAST P INNER JOIN PRST B ON P.PRCODE = B.PRCODE WHERE P.PRCODE = @PRCODE AND B.BATCH = @BATCH AND B.GRACK = @GDWCODE AND (@BATCHRTEDIT = 1 OR (B.CONVMRP = @CONVRATE AND B.CONVRATE = @CONVMRP)) AND (ISNULL(P.MANDATEFLG,0) = 0 OR B.MFGDATE = @MFGDATE) AND (ISNULL(P.EXPDATEFLG,0) = 0 OR B.EXPIRY = @EXPDATE))

					IF(@CONVQTY + @USEDQTY_B - @PRVQTY_B > @STOCK_B)
						SET @RESULT = 'Entered quanity is greater than available batch stock for product: '''+@PRNAME+''' at Row: '+CAST(@ROWNO AS NVARCHAR)
				END
			END

			--## Serial Number Tracking Validation
			IF(@RESULT = '' AND @SLNOWISE = 1 AND @SLNOTRACK = 1)
			BEGIN
				DECLARE @SLNO_COUNT INT = (SELECT COUNT(*) FROM @SLNODTLS WHERE PRCODE = @PRCODE AND OUTSLNO = @MinSLNO AND GDWCODE = @GDWCODE AND UNITCODE = @UNITCODE)
				DECLARE @DUPLICATE_SLNO NVARCHAR(100) = NULL
				SELECT TOP 1  @DUPLICATE_SLNO = ITEMSLNO FROM @SLNODTLS WHERE PRCODE = @PRCODE GROUP BY ITEMSLNO HAVING COUNT(ITEMSLNO) > 1

				IF (@SLNO_COUNT = 0)
					SET @RESULT = 'Missing serial number tracking details for product: '''+@PRNAME+''' at Row: '+CAST(@ROWNO AS NVARCHAR)
				ELSE IF (@QTY % 1 <> 0 )
					SET @RESULT = 'Decimal numbers are not allowed with serial number tracking for Product: '''+@PRNAME+''' at Row: '+CAST(@ROWNO AS NVARCHAR)
				ELSE IF (@SLNO_COUNT > @QTY)
					SET @RESULT = 'Entered quantity is greater than serial number tracking record for product: '''+@PRNAME+''' at Row: '+CAST(@ROWNO AS NVARCHAR)
				ELSE IF (@SLNO_COUNT < @QTY)
					SET @RESULT = 'Entered quantity is less than serial number tracking record for product: '''+@PRNAME+''' at Row: '+CAST(@ROWNO AS NVARCHAR)
				ELSE IF @DUPLICATE_SLNO IS NOT NULL
					SET @RESULT = 'Serial number'''+@DUPLICATE_SLNO+''' is become duplicate for product: '''+@PRNAME+''' at Row: '+CAST(@ROWNO AS NVARCHAR)
				ELSE
				BEGIN							
					DECLARE @SL TABLE (SLNO INT IDENTITY(1,1), INVRTYPE NVARCHAR(MAX), INVRNO INT, INORDNO INT, INSLNO INT, ITEMSLNO NVARCHAR(MAX))
					INSERT INTO @SL (INVRTYPE, INVRNO, INORDNO, INSLNO, ITEMSLNO) SELECT INVRTYPE, INVRNO, INORDNO, INSLNO, ITEMSLNO FROM @SLNODTLS WHERE PRCODE = @PRCODE AND OUTSLNO = @MinSLNO AND GDWCODE = @GDWCODE AND UNITCODE = @UNITCODE

					DECLARE @MinOUTSLNO INT = 1
					DECLARE @MaxOUTSLNO INT = (SELECT MAX(SLNO) FROM @SL)

					WHILE (@MinOUTSLNO <= @MaxOUTSLNO)
					BEGIN

						IF (@RESULT <> '') BREAK

						DECLARE @ITEMSLNO NVARCHAR(MAX) = (SELECT ITEMSLNO FROM @SL WHERE SLNO = @MinOUTSLNO)
						DECLARE @USED BIT = (SELECT TOP 1 S.USED FROM @SL D INNER JOIN SNTSTK S ON S.INVRTYPE = D.INVRTYPE AND S.INVRNO = D.INVRNO AND S.INORDNO = D.INORDNO AND S.INSLNO = D.INSLNO AND S.ITEMSLNO = D.ITEMSLNO WHERE D.SLNO = @MinOUTSLNO AND S.PRCODE = @PRCODE AND S.GDWCODE = @GDWCODE AND S.UNITCODE = @UNITCODE)
						DECLARE @USED_SI BIT = CASE WHEN @SINVNO = 0 THEN 0 WHEN EXISTS (SELECT S.PRCODE FROM @SL D INNER JOIN SNTSTK S ON S.INVRTYPE = D.INVRTYPE AND S.INVRNO = D.INVRNO AND S.INORDNO = D.INORDNO AND S.INSLNO = D.INSLNO AND S.ITEMSLNO = D.ITEMSLNO WHERE D.SLNO = @MinOUTSLNO AND S.PRCODE = @PRCODE AND S.GDWCODE = @GDWCODE AND S.UNITCODE = @UNITCODE AND S.OUTVRTYPE = 'SI' AND S.OUTVRNO = @SINVNO AND S.OUTORDNO = @SINVORDNO) THEN 1 ELSE 0 END
						IF(@USED IS NULL)
							SET @RESULT = 'Invalid serial number '''+@ITEMSLNO+''' for product: '''+@PRNAME+''' at Row: '+CAST(@ROWNO AS NVARCHAR)						
						ELSE IF(@USED = 1 AND @USED_SI = 0)
							SET @RESULT = 'Serial number '''+@ITEMSLNO+''' is already in use for product: '''+@PRNAME+''' at Row: '+CAST(@ROWNO AS NVARCHAR)
						ELSE
							INSERT INTO @SLNOUPDATE (INVRTYPE, INVRNO, INORDNO, INSLNO, PRCODE, OUTSLNO, GDWCODE, UNITCODE, ITEMSLNO) SELECT INVRTYPE, INVRNO, INORDNO, INSLNO, @PRCODE, @MinSLNO, @GDWCODE, @UNITCODE, ITEMSLNO FROM @SL WHERE SLNO = @MinOUTSLNO

						SET @MinOUTSLNO = @MinOUTSLNO + 1
					END
				END
			END

			--## Minimum Sales Price Validation
			IF(@RESULT = '' AND @ALLOWMINSALESPRICE > 0 AND @RATE < @BASEMINPRICE)
				SET @RESULT = 'The minimum sales price for '''+@PRNAME+''' is '+FORMAT(@BASEMINPRICE, 'N2')+' at Row: '+CAST(@ROWNO AS NVARCHAR)

			--## Voucher Date Validation
			IF(@RESULT = '' AND @SINVNO = 0 AND @DATEBLOCK = 1 AND (@USERTYPE <> 'SuperUser' OR (@USERTYPE = 'SuperUser' AND @SUDATEBLOCK = 1)))
			BEGIN
				DECLARE @MAXDATE DATETIME = (SELECT MAX(INVDATE) FROM SALM WHERE STYPE = 'O')
				IF (@MAXDATE IS NOT NULL AND @DATE < @MAXDATE)
					SET @RESULT = CASE WHEN @USERTYPE <> 'SuperUser' THEN 'Backdated Entries Not Allowed On This User. Please Contact Your Super To User.' ELSE 'Selected Date Is Less Than Last Voucher Date' END
			END

			SET @MinSLNO = CASE WHEN @RESULT <> '' OR NOT EXISTS (SELECT * FROM #TEMP_SI WHERE SLNO > @MinSLNO) THEN (@MaxSLNO + 1) ELSE (SELECT MIN(SLNO) FROM #TEMP_SI WHERE SLNO > @MinSLNO) END
			SET @ROWNO = @ROWNO + 1
		END
	END

	IF (@RESULT = '' AND @SINVNO = 0 AND ((SELECT TOP 1 BINVNO FROM FYDETAILS) = -1 OR (SELECT TOP 1 GDNVRNO FROM FYDETAILS) = -1))
		SET @RESULT = 'Something went wrong. Please try again.'

	IF (@RESULT = '')
	BEGIN

		DECLARE @INVNO INT, @ORDNO INT, @INVNO_DN INT, @ORDNO_DN INT

		IF(@SINVNO <> 0)
		BEGIN
			--## Set INVNO AND ORDNO
			SET @INVNO = @SINVNO
			SET @ORDNO = @SINVORDNO
			SET @INVNO_DN = @GDNINVNO
			SET @ORDNO_DN = @GDNORDNO
		END
		ELSE
		BEGIN
			--## Get Max ORDNO		
			UPDATE FYDETAILS SET @ORDNO = BINVNO, BINVNO = -1
			UPDATE FYDETAILS SET @ORDNO_DN = GDNVRNO, GDNVRNO = -1

			SET @ORDNO = @ORDNO + 1
			IF EXISTS(SELECT * FROM SALM WHERE STYPE = 'O' AND ORDNO = @ORDNO)
				SET @ORDNO = (SELECT MAX(ORDNO) + 1 FROM SALM WHERE STYPE = 'O')

			SET @ORDNO_DN = @ORDNO_DN + 1
			IF EXISTS(SELECT * FROM GDNM WHERE STYPE = 'DN' AND ORDNO = @ORDNO_DN)
				SET @ORDNO_DN = (SELECT MAX(ORDNO) + 1 FROM GDNM WHERE STYPE = 'DN')
		END

		--## Generate INVNOTEXT
		DECLARE @EmbDate NVARCHAR(MAX), @Emp NVARCHAR(MAX), @FYStatus INT, @FYDateFrom DATETIME, @FYDateTo DATETIME
		DECLARE @Prefit NVARCHAR(MAX), @Seperator NVARCHAR(MAX), @Suffix NVARCHAR(MAX), @fixedLength NVARCHAR(MAX), @Length INT, @StartNo int
		SELECT @FYStatus = CASE WHEN YEAR(FYDATEFR) = YEAR(FYDATETO) THEN 1 ELSE 0 END, @FYDateFrom = FYDATEFR, @FYDateTo = FYDATETO FROM FYDETAILS

		SELECT @EmbDate = EMBDATE, @Emp = CASE WHEN ISNULL(EMBDATE,'') = '' OR ISNULL(EMBFORMAT,'') = '' THEN '' ELSE (CASE WHEN @FYStatus = 1 THEN FORMAT(@FYDateFrom,'yyyy') WHEN EMBFORMAT = 'YYYY-YY' THEN FORMAT(@FYDateFrom,'yyyy')+'-'+FORMAT(@FYDateTo,'yy') ELSE FORMAT(@FYDateFrom,'yy')+'-'+FORMAT(@FYDateTo,'yy') END) END,
		@Prefit = ISNULL(PREFIX,''), @Seperator = ISNULL(SEPERATOR,''), @Suffix = ISNULL(SUFFIX,''), @fixedLength = ISNULL(FIXLENGTH,''), @Length = CAST(ISNULL([LENGTH],0) AS INT), @StartNo = CAST(ISNULL(STARTNO,0) AS INT) FROM VRNOCONF WHERE VRTYPE = 'Sales'			
		SET @ORDNO = CASE WHEN @ORDNO = 1 AND @StartNo > 0 THEN @StartNo ELSE @ORDNO END
		DECLARE @INVNOTEXT NVARCHAR(MAX) = (CASE WHEN @EmbDate = 'Prefix' AND @Emp <> '' THEN @Emp + @Seperator ELSE '' END) + @Prefit + (CASE WHEN @Prefit = '' THEN '' ELSE @Seperator END) + (CASE WHEN @fixedLength <> 'Y' OR LEN(CAST(@ORDNO AS VARCHAR)) > @Length THEN CAST(@ORDNO AS VARCHAR) ELSE RIGHT(REPLICATE('0', @Length) + CAST(@ORDNO AS VARCHAR), @Length) END) +	(CASE WHEN @Suffix = '' THEN '' ELSE @Seperator END) + @Suffix + (CASE WHEN @EmbDate = 'Suffix' AND @Emp <> '' THEN @Seperator + @Emp ELSE '' END)

		SELECT @EmbDate = EMBDATE, @Emp = CASE WHEN ISNULL(EMBDATE,'') = '' OR ISNULL(EMBFORMAT,'') = '' THEN '' ELSE (CASE WHEN @FYStatus = 1 THEN FORMAT(@FYDateFrom,'yyyy') WHEN EMBFORMAT = 'YYYY-YY' THEN FORMAT(@FYDateFrom,'yyyy')+'-'+FORMAT(@FYDateTo,'yy') ELSE FORMAT(@FYDateFrom,'yy')+'-'+FORMAT(@FYDateTo,'yy') END) END,
		@Prefit = ISNULL(PREFIX,''), @Seperator = ISNULL(SEPERATOR,''), @Suffix = ISNULL(SUFFIX,''), @fixedLength = ISNULL(FIXLENGTH,''), @Length = CAST(ISNULL([LENGTH],0) AS INT), @StartNo = CAST(ISNULL(STARTNO,0) AS INT) FROM VRNOCONF WHERE VRTYPE = 'Goods Delivery Note'			
		SET @ORDNO_DN = CASE WHEN @ORDNO_DN = 1 AND @StartNo > 0 THEN @StartNo ELSE @ORDNO_DN END
		DECLARE @INVNOTEXT_DN NVARCHAR(MAX) = (CASE WHEN @EmbDate = 'Prefix' AND @Emp <> '' THEN @Emp + @Seperator ELSE '' END) + @Prefit + (CASE WHEN @Prefit = '' THEN '' ELSE @Seperator END) + (CASE WHEN @fixedLength <> 'Y' OR LEN(CAST(@ORDNO_DN AS VARCHAR)) > @Length THEN CAST(@ORDNO_DN AS VARCHAR) ELSE RIGHT(REPLICATE('0', @Length) + CAST(@ORDNO_DN AS VARCHAR), @Length) END) +	(CASE WHEN @Suffix = '' THEN '' ELSE @Seperator END) + @Suffix + (CASE WHEN @EmbDate = 'Suffix' AND @Emp <> '' THEN @Seperator + @Emp ELSE '' END)

		--## Summary Details
		DECLARE @TAXABLE DECIMAL(18,6), @TAXAMOUNT DECIMAL(18,6), @NETAMOUNT DECIMAL(18,6), @TOTMRP DECIMAL(18,6) 
		SELECT @TAXABLE = SUM(TAXABLE), @TAXAMOUNT = SUM(TAXAMOUNT), @NETAMOUNT = SUM(NETAMOUNT), @TOTMRP = SUM(ISNULL(MRP*QTY,0)) FROM #TEMP_SI
		DECLARE @ADDAMT DECIMAL(18,6) = (SELECT ISNULL(SUM(AMOUNT), 0) FROM @ADDIDTLS)	
		
		IF(@SINVNO = 0)
		BEGIN
			--## Save SALM
			DECLARE @INVNO_TB TABLE (ID INT)		

			INSERT INTO [dbo].[SALM] (STYPE, ORDNO, INVDATE, BTYPE, CUSTCODE, DISCP, DISCVAL,
			NTVALUE, TVALUE, TAX, COINADJ, AMOUNT, FREIGHT, SMCODE, LOGCD, 
			LOGBILL, UDATE, CREATEDDT, INFORMAT, PAYMODE,
			REMARKS, CUSDUITY, RETURNFLG, SLCODE, VATFLG, BILLREFFLG, CURCONRATE, CURCODE, SHIPADDRESS, SLFLG, CANCELFLG,
			BLCODE, GDNNO, RTNINVNO, DUEDATE, ADJCD, POS,
			CASHPAID, YVRNO, PRCATCODE, INVNOTEXT, CARDNO, PCNAME, VSVRTYPE, VSDOCNO, VSORDCD, PRLISTCODE,
			PNTINID, PNTINPER, PNTINVAL, PNTOUTID, PNTOUTPER, PNTOUTVAL, MRP, TRANSFERRED,
			ISMNBILL, MNBILLNO, RETTRANSFER, PARTYORDNO, QTNNOS, QTNVSNOS, SIF1, SIF2, SIF3, SIF4, SIF5, SIF6,
			BANKPAID, BANKYVRNO, CASHLCD, BANKLCD, BANKPAIDT, BANKYVRNOT, BANKLCDT, ULOGCD, 
			SIF7, SIF8, SIF9, SIF10, SIF11, SIF12, SIF13, SIF14, SIF15, SIF16, SIF17, SIF18, SIF19, SIF20, PAYTYPE)

			OUTPUT INSERTED.INVNO INTO @INVNO_TB

			SELECT 'O' AS STYPE, @ORDNO AS ORDNO, @DATE AS INVDATE, @BTYPE AS BTYPE, @CUSCODE AS CUSTCODE, @DISCPERC AS DISCP, @DISCAMT AS DISCVAL,
			@NETAMOUNT AS NTVALUE, @TAXABLE AS TVALUE, @TAXAMOUNT AS TAX, @ROUNDOFF AS COINADJ, (@NETAMOUNT + @ROUNDOFF + @ADDAMT) AS AMOUNT, 0 AS FREIGHT, @SMCODE AS SMCODE, @USERID AS LOGCD,
			0 AS LOGBILL, GETDATE() AS UDATE, GETDATE() AS CREATEDDT, 1 AS INFORMAT, (CASE WHEN @PAYMODE = 'Cash' THEN 1 WHEN @PAYMODE = 'Credit' THEN 2 ELSE 4 END) AS PAYMODE,
			'' AS REMARKS, 0 AS CUSDUITY, 0 AS RETURNFLG, 0 AS SLCODE, 0 AS VATFLG, 0 AS BILLREFFLG, 1 AS CURCONRATE, 1 AS CURCODE, '' AS SHIPADDRESS, @COSTC AS SLFLG, 0 AS CANCELFLG,
			@BLCODE AS BLCODE, @ORDNO_DN AS GDNNO, 0 AS RTNINVNO, @DUEDATE AS DUEDATE, @ADJCD AS ADJCD, @POS AS POS,
			(CASE WHEN @PAYMODE = 'Cash' OR @PAYMODE = 'Bank' THEN (@NETAMOUNT + @ROUNDOFF + @ADDAMT) ELSE 0 END) AS CASHPAID, 0 AS YVRNO, 0 AS PRCATCODE, @INVNOTEXT AS INVNOTEXT, '' AS CARDNO, @PCNAME AS PCNAME, 0 AS VSVRTYPE, '' AS VSDOCNO, 0 AS VSORDCD, 0 AS PRLISTCODE,
			0 AS PNTINID, 0 AS PNTINPER, 0 AS PNTINVAL, 0 AS PNTOUTID, 0 AS PNTOUTPER, 0 AS PNTOUTVAL, @TOTMRP AS MRP, 'N' AS TRANSFERRED,
			0 AS ISMNBILL, '' AS MNBILLNO, 'N' AS RETTRANSFER, '' AS PARTYORDNO, '' AS QTNNOS, '' AS QTNVSNOS, '' AS SIF1, '' AS SIF2, '' AS SIF3, '' AS SIF4, '' AS SIF5, '' AS SIF6,
			0 AS BANKPAID, 0 AS BANKYVRNO, 0 AS CASHLCD, 0 AS BANKLCD, 0 AS BANKPAIDT, 0 AS BANKYVRNOT, 0 AS BANKLCDT, @USERID AS ULOGCD, 
			'' AS SIF7, '' AS SIF8, '' AS SIF9, '' AS SIF10, '' AS SIF11, '' AS SIF12, '' AS SIF13, '' AS SIF14, '' AS SIF15, '' AS SIF16, '' AS SIF17, '' AS SIF18, '' AS SIF19, '' AS SIF20, 0 AS PAYTYPE

			SELECT @INVNO = ID FROM @INVNO_TB
		END
		ELSE
		BEGIN
			--## Update SALM
			UPDATE [dbo].[SALM] SET STYPE = 'O', ORDNO = @ORDNO, INVDATE = @DATE, BTYPE = @BTYPE, CUSTCODE = @CUSCODE, DISCP = @DISCPERC, DISCVAL = @DISCAMT,
			NTVALUE = @NETAMOUNT, TVALUE = @TAXABLE, TAX = @TAXAMOUNT, COINADJ = @ROUNDOFF, AMOUNT = (@NETAMOUNT + @ROUNDOFF + @ADDAMT), FREIGHT = 0, SMCODE = @SMCODE, ULOGCD = @USERID,
			LOGBILL = 0, UDATE = GETDATE(), INFORMAT = 1, PAYMODE = (CASE WHEN @PAYMODE = 'Cash' THEN 1 WHEN @PAYMODE = 'Credit' THEN 2 ELSE 4 END),
			REMARKS = '', CUSDUITY = 0, RETURNFLG = 0, SLCODE = 0, VATFLG = 0, CURCONRATE = 1, CURCODE = 1, SHIPADDRESS = '', SLFLG = @COSTC,
			BLCODE = @BLCODE, GDNNO = @ORDNO_DN, RTNINVNO = 0, DUEDATE = @DUEDATE, ADJCD = @ADJCD, POS = @POS,
			CASHPAID = (CASE WHEN @PAYMODE = 'Cash' OR @PAYMODE = 'Bank' THEN (@NETAMOUNT + @ROUNDOFF + @ADDAMT) ELSE 0 END), YVRNO = 0, PRCATCODE = 0, INVNOTEXT = @INVNOTEXT, CARDNO = '', PCNAME = @PCNAME, PRLISTCODE = 0,
			PNTINID = 0, PNTINPER = 0, PNTINVAL = 0, PNTOUTID = 0, PNTOUTPER = 0, PNTOUTVAL = 0, MRP = @TOTMRP, TRANSFERRED = 'N',
			ISMNBILL = 0, MNBILLNO = '', PARTYORDNO = '', SIF1 = '', SIF2 = '', SIF3 = '', SIF4 = '', SIF5 = '', SIF6 = '',
			BANKPAID = 0, BANKYVRNO = 0, CASHLCD = 0, BANKLCD = 0, BANKPAIDT = 0, BANKYVRNOT = 0, BANKLCDT = 0,
			SIF7 = '', SIF8 = '', SIF9 = '', SIF10 = '', SIF11 = '', SIF12 = '', SIF13 = '', SIF14 = '', SIF15 = '', SIF16 = '', SIF17 = '', SIF18 = '', SIF19 = '', SIF20 = '', PAYTYPE = 0 WHERE INVNO = @INVNO
		END

		--## Clear Stock
		UPDATE P SET P.STOCK = P.STOCK + D.QTY FROM PRMAST P INNER JOIN (SELECT PRCODE, SUM(CONVQTY) AS QTY FROM SALT WHERE INVNO = @INVNO GROUP BY PRCODE) D on P.PRCODE = D.PRCODE

		--## Save SALT		
		DELETE FROM [dbo].[SALT] WHERE INVNO = @INVNO

		INSERT INTO [dbo].[SALT] (STYPE, ORDNO, INVNO, INVDATE, BTYPE, PRCODE, CUSTCODE,
		QTY, RATE, PRRATE, DISC, TAXCODE, TAX, RETURNFLG, BATCH, EXPIRY, ORGRATE, SMCODE, LOGCD, ORGQTY,
		ALCODE, ALNAME, UNITCODE, GDWCODE, PRDESC, CURCONRATE, CURCODE, LDISCTYPE, LDISCPERC, LDISCAMT, TOTDISC, GDNFLG, SLCODE, PRFLG,
		SORDNO, GDNNO, CANCELFLG, RTNINVNO, CONVTYPE, CONVFACT, CONVQTY, CONVRATE, DISCP, MRP, SPRTMPCODE,
		BML, BMQ, BMW, MFGDATE, VSVRTYPE, VATREASONCD, RETREASON, PRV, SNTSLNO, PRVN, PRVR, LM,
		ORDROWID, NTROWID, TRNROWID, QTNCODE, QTNROWID, PRVQ, PRVY, PRVNY, PRVQY, PRVRY)

		SELECT 'O' AS STYPE, @ORDNO AS ORDNO, @INVNO AS INVNO, @DATE AS INVDATE, @BTYPE AS BTYPE, PRCODE, @CUSCODE AS CUSTCODE,
		QTY, RATE, PRRATE, DISCPRICE AS DISC, TAXCODE, TAXPERC AS TAX, 0 AS RETURNFLG, BATCH, EXPDATE AS EXPIRY, RATE AS ORGRATE, @SMCODE AS SMCODE, @USERID AS LOGCD, QTY AS ORGQTY, 
		ALCODE, ALNAME, UNITCODE, GDWCODE, '' AS PRDESC, 1 AS CURCONRATE, 1 AS CURCODE, '' AS LDISCTYPE, LDISCPERC, LDISCPRICE AS LDISCAMT, (LDISCPRICE + DISCPRICE) AS TOTDISC, 0 AS GDNFLG, 0 AS SLCODE, PRFLG,
		SORDNO, 0 AS GDNNO, 0 AS CANCELFLG, 0 AS RTNINVNO, CONVTYPE, CONVFACT, CONVQTY, CONVRATE, DISCPERC AS DISCP, MRP, 0 AS SPRTMPCODE,
		0 AS BML, 0 AS BMQ, 0 AS BMW, MFGDATE, 0 AS VSVRTYPE, '' AS VATREASONCD, '' AS RETREASON, 'N' AS PRV, SLNO AS SNTSLNO, 'N' AS PRVN, 'N' AS PRVR, 'N' AS LM,
		ORDROWID, SLNO AS NTROWID, SLNO AS TRNROWID, 0 AS QTNCODE, 0 AS QTNROWID, 'N' AS PRVQ, '' AS PRVY, '' AS PRVNY, '' AS PRVQY, '' AS PRVRY FROM #TEMP_SI ORDER BY SLNO

		--## Save SALD				
		DELETE FROM [dbo].[SALD] WHERE VRNO = @INVNO

		--1 Summary by Tax Code
		INSERT INTO [dbo].[SALD] (VRTYPE, VRNO, INVDATE, CUSCODE, ETYPE, CODE, AMOUNT, TAX, BTYPE, LOGCD,
		TAXAMT, RETURNFLG, SLCODE, VATFLG, CURCONRATE, CURCODE, ORDNO, DFLAG, ORGAMT, ORGTAXAMT, CANCELFLG, BLCODE, ADJCD, VSVRTYPE)

		SELECT 'O' AS VRTYPE, @INVNO AS VRNO, @DATE AS INVDATE, @CUSCODE AS CUSCODE, 'T' AS ETYPE, TAXCODE AS CODE, SUM(TAXABLE) AS AMOUNT, TAXPERC AS TAX, @BTYPE AS BTYPE, @USERID AS LOGCD,
		SUM(TAXAMOUNT) AS TAXAMT, 0 AS RETURNFLG, 0 AS SLCODE, 0 AS VATFLG, 1 AS CURCONRATE, 1 AS CURCODE, @ORDNO AS ORDNO, 1 AS DFLAG, 0 AS ORGAMT, 0 AS ORGTAXAMT, 0 AS CANCELFLG, @BLCODE AS BLCODE, @ADJCD AS ADJCD, 0 AS VSVRTYPE FROM #TEMP_SI GROUP BY TAXCODE, TAXPERC

		--2 Discount
		IF(@DISCAMT <> 0)
		BEGIN
			INSERT INTO [dbo].[SALD] (VRTYPE, VRNO, INVDATE, CUSCODE, ETYPE, CODE, AMOUNT, TAX, BTYPE, LOGCD,
			TAXAMT, RETURNFLG, SLCODE, VATFLG, CURCONRATE, CURCODE, ORDNO, DFLAG, ORGAMT, ORGTAXAMT, CANCELFLG, BLCODE, ADJCD, VSVRTYPE)

			SELECT 'O' AS VRTYPE, @INVNO AS VRNO, @DATE AS INVDATE, @CUSCODE AS CUSCODE, 'A' AS ETYPE, @DISCCD AS CODE, @NETAMOUNT AS AMOUNT, @DISCPERC AS TAX, @BTYPE AS BTYPE, @USERID AS LOGCD,
			@DISCAMT AS TAXAMT, 0 AS RETURNFLG, 0 AS SLCODE, 0 AS VATFLG, 1 AS CURCONRATE, 1 AS CURCODE, @ORDNO AS ORDNO, 1 AS DFLAG, 0 AS ORGAMT, 0 AS ORGTAXAMT, 0 AS CANCELFLG, @BLCODE AS BLCODE, @ADJCD AS ADJCD, 0 AS VSVRTYPE
		END

		--3 Roundoff
		IF(@ROUNDOFF <> 0)
		BEGIN
			INSERT INTO [dbo].[SALD] (VRTYPE, VRNO, INVDATE, CUSCODE, ETYPE, CODE, AMOUNT, TAX, BTYPE, LOGCD,
			TAXAMT, RETURNFLG, SLCODE, VATFLG, CURCONRATE, CURCODE, ORDNO, DFLAG, ORGAMT, ORGTAXAMT, CANCELFLG, BLCODE, ADJCD, VSVRTYPE)

			SELECT 'O' AS VRTYPE, @INVNO AS VRNO, @DATE AS INVDATE, @CUSCODE AS CUSCODE, 'A' AS ETYPE, @COINADJCD AS CODE, @NETAMOUNT AS AMOUNT, 0 AS TAX, @BTYPE AS BTYPE, @USERID AS LOGCD,
			@ROUNDOFF AS TAXAMT, 0 AS RETURNFLG, 0 AS SLCODE, 0 AS VATFLG, 1 AS CURCONRATE, 1 AS CURCODE, @ORDNO AS ORDNO, 1 AS DFLAG, 0 AS ORGAMT, 0 AS ORGTAXAMT, 0 AS CANCELFLG, @BLCODE AS BLCODE, @ADJCD AS ADJCD, 0 AS VSVRTYPE
		END

		--4 Additional Information
		INSERT INTO [dbo].[SALD] (VRTYPE, VRNO, INVDATE, CUSCODE, ETYPE, CODE, AMOUNT, TAX, BTYPE, LOGCD,
		TAXAMT, RETURNFLG, SLCODE, VATFLG, CURCONRATE, CURCODE, ORDNO, DFLAG, ORGAMT, ORGTAXAMT, CANCELFLG, BLCODE, ADJCD, VSVRTYPE)

		SELECT 'O' AS VRTYPE, @INVNO AS VRNO, @DATE AS INVDATE, @CUSCODE AS CUSCODE, 'A' AS ETYPE, ACCID AS CODE, @NETAMOUNT AS AMOUNT, 0 AS TAX, @BTYPE AS BTYPE, @USERID AS LOGCD,
		AMOUNT AS TAXAMT, 0 AS RETURNFLG, 0 AS SLCODE, 0 AS VATFLG, 1 AS CURCONRATE, 1 AS CURCODE, @ORDNO AS ORDNO, 0 AS DFLAG, 0 AS ORGAMT, 0 AS ORGTAXAMT, 0 AS CANCELFLG, @BLCODE AS BLCODE, @ADJCD AS ADJCD, 0 AS VSVRTYPE FROM @ADDIDTLS

		--4 Summary By Sales Account
		INSERT INTO [dbo].[SALD] (VRTYPE, VRNO, INVDATE, CUSCODE, ETYPE, CODE, AMOUNT, TAX, BTYPE,LOGCD,
		TAXAMT, RETURNFLG, SLCODE, VATFLG, CURCONRATE, CURCODE, ORDNO, DFLAG, ORGAMT, ORGTAXAMT, CANCELFLG,
		BLCODE, ADJCD, VSVRTYPE)

		SELECT 'O' AS VRTYPE, @INVNO AS VRNO, @DATE AS INVDATE, @CUSCODE AS CUSCODE, 'D' AS ETYPE, CODE, SUM(TAXABLE) AS AMOUNT, 0 AS TAX, @BTYPE AS BTYPE, @USERID AS LOGCD,
		0 AS TAXAMT, 0 AS RETURNFLG, 0 AS SLCODE, 0 AS VATFLG, 1 AS CURCONRATE, 1 AS CURCODE, @ORDNO AS ORDNO, 0 AS DFLAG, 0 AS ORGAMT, 0 AS ORGTAXAMT, 0 AS CANCELFLG,
		@BLCODE AS BLCODE, @ADJCD AS ADJCD, 0 AS VSVRTYPE 
		FROM (SELECT H.TAXABLE, CASE WHEN @TAGACCMSTR = 1 AND P.SALEACCODE <> 0 THEN P.SALEACCODE WHEN G.TAGACCFLG = 1 AND G.SALESACCID <> 0 THEN G.SALESACCID WHEN T.SALCODE <> 0 THEN T.SALCODE ELSE 7 END AS CODE FROM 
		#TEMP_SI H INNER JOIN PRMAST P ON H.PRCODE = P.PRCODE LEFT JOIN PRGRPMAS G ON P.GROUPCODE = G.PRGRPID LEFT JOIN TAXMAS T ON P.TAXCODE = T.CODE) DTLS GROUP BY CODE
	
		--## Save STKDT
		DELETE FROM [dbo].[STKDT] WHERE VRTYPE = 'TR' AND TYPE = 'I' AND TRNNO = @ORDNO

		INSERT INTO [dbo].[STKDT] (VRTYPE, PRCODE, VRDATE, ORDNO, NTNO, TRNNO, QTY, TYPE, PARTYCD, LOGCD, UTIME, RATE,
		UNITCODE, CONVTYPE, CONVFACT, CONVQTY, PRFLG, CONVRATE, GDWCODE, LUTIME, VSVRTYPE,
		PRV, PRVN, PRVT, SNTSLNO, LM, ADDCOST, CONVADDCOST, CLOSES, ORDROWID, NTROWID, TRNROWID,
		QTNCODE, QTNROWID, PRVQ, PRVQY, PRVY, PRVNY, PRVTY)

		SELECT 'TR' AS VRTYPE, PRCODE, @DATE AS VRDATE, SORDNO AS ORDNO, @ORDNO_DN AS NTNO, @ORDNO AS TRNNO, QTY, 'I' AS TYPE, @CUSCODE AS PARTYCD, @USERID AS LOGCD, GETDATE() AS UTIME, RATE,
		UNITCODE, CONVTYPE, CONVFACT, CONVQTY, PRFLG, CONVRATE, GDWCODE, GETDATE() AS LUTIME, 0 AS VSVRTYPE,
		'N' AS PRV, 'N' AS PRVN, 'N' AS PRVT, SLNO AS SNTSLNO, 'N' AS LM, 0 AS ADDCOST, 0 AS CONVADDCOST, 0 AS CLOSES, ORDROWID, SLNO AS NTROWID, SLNO AS TRNROWID,
		0 AS QTNCODE, 0 AS QTNROWID, 'N' AS PRVQ, '' AS PRVQY, '' AS PRVY, '' AS PRVNY, '' AS PRVTY FROM #TEMP_SI ORDER BY SLNO

		--## Save LOGNOTE
		INSERT INTO [dbo].[LOGNOTE] (VRTYPE, VRNO, EDATE, USERCODE, NOTES, CFLAG, VSVRTYPE)
		SELECT 'S' AS VRTYPE, @INVNO AS VRNO, GETDATE() AS EDATE, @USERID AS USERCODE, (CASE WHEN @SINVNO = 0 THEN 'Create ' ELSE 'Update' END) AS NOTES, (CASE WHEN @SINVNO = 0 THEN 1 ELSE 0 END) AS CFLAG, 0 AS VSVRTYPE

		--//

		--## Save GDNM
		IF(@SINVNO = 0)
		BEGIN
			DECLARE @INVNO_DN_TB TABLE (ID INT)

			INSERT INTO [dbo].[GDNM] (STYPE, ORDNO, INVDATE, BTYPE, CUSTCODE, DISCP, DISCVAL, NTVALUE, TVALUE, TAX,
			COINADJ, AMOUNT, FREIGHT, SMCODE, LOGCD, LOGBILL, UDATE, CREATEDDT, INFORMAT,
			PAYMODE, REMARKS, CUSDUITY, RETURNFLG, SLCODE, VATFLG, BILLREFFLG,
			CURCONRATE, CURCODE, SHIPADDRESS, SALNO, SLFLG, CANCELFLG, TRCKFLG, PRCATCODE, INVNOTEXT,
			VSVRTYPE, VSDOCNO, VSORDCD, PRLISTCODE, TRANSFERRED, PARTYORDNO,
			GDNF1, GDNF2, GDNF3, GDNF4, GDNF5, GDNF6,
			CLOSED, CLEDGERDR, CLEDGERCR, CLOSEREMARKS, ULOGCD,
			GDNF7, GDNF8, GDNF9, GDNF10, GDNF11, GDNF12, GDNF13, GDNF14, GDNF15, GDNF16, GDNF17, GDNF18, GDNF19, GDNF20)

			OUTPUT INSERTED.INVNO INTO @INVNO_DN_TB

			SELECT 'DN' AS STYPE, @ORDNO_DN AS ORDNO, @DATE AS INVDATE, @BTYPE AS BTYPE, @CUSCODE AS CUSTCODE, @DISCPERC AS DISCP, @DISCAMT AS DISCVAL, @NETAMOUNT AS NTVALUE, @TAXABLE AS TVALUE, @TAXAMOUNT AS TAX,
			@ROUNDOFF AS COINADJ, (@NETAMOUNT + @ROUNDOFF + @ADDAMT) AS AMOUNT, 0 AS FREIGHT, @SMCODE AS SMCODE, @USERID AS LOGCD, 0 AS LOGBILL, GETDATE() AS UDATE, GETDATE() AS CREATEDDT, 1 AS INFORMAT,
			(CASE WHEN @PAYMODE = 'Cash' THEN 1 WHEN @PAYMODE = 'Credit' THEN 2 ELSE 4 END) AS PAYMODE, '' AS REMARKS, 0 AS CUSDUITY, 0 AS RETURNFLG, 0 AS SLCODE, 0 AS VATFLG, 0 AS BILLREFFLG,
			1 AS CURCONRATE, 1 AS CURCODE, '' AS SHIPADDRESS, 0 AS SALNO, @COSTC AS SLFLG, 0 AS CANCELFLG, 1 AS TRCKFLG, 0 AS PRCATCODE, @INVNOTEXT_DN AS INVNOTEXT,
			0 ASVSVRTYPE, '' AS VSDOCNO, 0 AS VSORDCD, 0 AS PRLISTCODE, 'N' AS TRANSFERRED, '' AS PARTYORDNO,
			'' AS GDNF1, '' AS GDNF2, '' AS GDNF3, '' AS GDNF4, '' AS GDNF5, '' AS GDNF6,
			0 AS CLOSED, 0 AS CLEDGERDR, 0 AS CLEDGERCR, '' AS CLOSEREMARKS, @USERID AS ULOGCD,
			'' AS GDNF7, '' AS GDNF8, '' AS GDNF9, '' AS GDNF10, '' AS GDNF11, '' AS GDNF12, '' AS GDNF13, '' AS GDNF14, '' AS GDNF15, '' AS GDNF16, '' AS GDNF17, '' AS GDNF18, '' AS GDNF19, '' AS GDNF20

			SELECT @INVNO_DN = ID FROM @INVNO_DN_TB
		END
		ELSE
		BEGIN
			UPDATE [dbo].[GDNM] SET STYPE = 'DN', ORDNO = @ORDNO_DN, INVDATE = @DATE, BTYPE = @BTYPE, CUSTCODE = @CUSCODE, DISCP = @DISCPERC, DISCVAL = @DISCAMT, NTVALUE = @NETAMOUNT, TVALUE = @TAXABLE, TAX = @TAXAMOUNT,
			COINADJ = @ROUNDOFF, AMOUNT = (@NETAMOUNT + @ROUNDOFF + @ADDAMT), FREIGHT = 0, SMCODE = @SMCODE, ULOGCD = @USERID, LOGBILL = 0, UDATE = GETDATE(), INFORMAT = 1,
			PAYMODE = (CASE WHEN @PAYMODE = 'Cash' THEN 1 WHEN @PAYMODE = 'Credit' THEN 2 ELSE 4 END), REMARKS = '', CUSDUITY = 0, RETURNFLG = 0, SLCODE = 0, VATFLG = 0,
			CURCONRATE = 1, CURCODE = 1, SHIPADDRESS = '' ,SLFLG = @COSTC, TRCKFLG = 1, PRCATCODE = 0, INVNOTEXT = @INVNOTEXT_DN,
			PRLISTCODE = 0, TRANSFERRED = 'N', PARTYORDNO = '', GDNF1 = '', GDNF2 = '', GDNF3 = '', GDNF4 = '', GDNF5 = '', GDNF6 = '',
			CLOSED = 0, CLEDGERDR = 0, CLEDGERCR = 0, CLOSEREMARKS = '', 
			GDNF7 = '', GDNF8 = '', GDNF9 = '', GDNF10 = '', GDNF11 = '', GDNF12 = '', GDNF13 = '', GDNF14 = '', GDNF15 = '', GDNF16 = '', GDNF17 = '', GDNF18 = '', GDNF19 = '', GDNF20 = '' WHERE INVNO = @INVNO_DN
		END

		--## Save GDNT
		DELETE FROM [dbo].[GDNT] WHERE INVNO = @INVNO_DN

		INSERT INTO [dbo].[GDNT] (STYPE, ORDNO, INVNO, INVDATE, BTYPE, PRCODE, CUSTCODE,
		QTY, RATE, PRRATE, DISC, TAXCODE, TAX, RETURNFLG, BATCH, EXPIRY, ORGRATE, SMCODE, LOGCD, ORGQTY,
		ALCODE, ALNAME, UNITCODE, GDWCODE, PRDESC, CURCONRATE, CURCODE, LDISCTYPE, LDISCPERC, LDISCAMT, TOTDISC, SLCODE, PRFLG,
		SORDNO, SALNO, CANCELFLG, CONVTYPE, CONVFACT, CONVQTY, CONVRATE, DISCP, MRP, BML, BMQ, BMW, MFGDATE, VSVRTYPE,
		PRV, SNTSLNO, PRVT, CLOSED, ORDROWID, NTROWID, TRNROWID, LM, PRVY, PRVTY, QTNCODE, QTNROWID,
		PRVQ, PRVQY)

		SELECT 'DN' AS STYPE, @ORDNO_DN AS ORDNO, @INVNO_DN AS INVNO, @DATE AS INVDATE, @BTYPE AS BTYPE, PRCODE, @CUSCODE AS CUSTCODE,
		QTY, RATE, PRRATE, DISCPRICE AS DISC, TAXCODE, TAXPERC AS TAX, 0 AS RETURNFLG, BATCH, EXPDATE AS EXPIRY, RATE AS ORGRATE, @SMCODE AS SMCODE, @USERID AS LOGCD, QTY AS ORGQTY,
		ALCODE, ALNAME, UNITCODE, GDWCODE, '' AS PRDESC, 1 AS CURCONRATE, 1 AS CURCODE, '' AS LDISCTYPE, LDISCPERC AS LDISCPERC, LDISCPRICE AS LDISCAMT, (LDISCPRICE + DISCPRICE) AS TOTDISC, 0 AS SLCODE, PRFLG,
		SORDNO, @ORDNO AS SALNO, 0 AS CANCELFLG, CONVTYPE, CONVFACT, CONVQTY, CONVRATE, @DISCPERC AS DISCP, MRP, 0 AS BML, 0 AS BMQ, 0 AS BMW, MFGDATE, 0 AS VSVRTYPE,
		'N' AS PRV, SLNO AS SNTSLNO, '' AS PRVT, 0 AS CLOSED, ORDROWID, SLNO AS NTROWID, SLNO AS TRNROWID, 'N' AS LM, '' AS PRVY, '' AS PRVTY, 0 AS QTNCODE, 0 AS QTNROWID,
		'N' AS PRVQ, '' AS PRVQY FROM #TEMP_SI ORDER BY SLNO

		--## Save GDND
		DELETE FROM [dbo].[GDND] WHERE VRNO = @INVNO_DN

		--1 Summary by Tax Code
		INSERT INTO [dbo].[GDND] (VRTYPE, VRNO, INVDATE, CUSCODE, ETYPE, CODE, AMOUNT, TAX, BTYPE,LOGCD,
		TAXAMT, RETURNFLG, SLCODE, VATFLG, CURCONRATE, CURCODE, ORDNO, DFLAG, ORGAMT, ORGTAXAMT, CANCELFLG,
		VSVRTYPE)

		SELECT 'DN' AS VRTYPE, @INVNO_DN AS VRNO, @DATE AS INVDATE, @CUSCODE AS CUSCODE, 'T' AS ETYPE, TAXCODE AS CODE, SUM(TAXABLE) AS AMOUNT, TAXPERC AS TAX, @BTYPE AS BTYPE, @USERID AS LOGCD,
		SUM(TAXAMOUNT) AS TAXAMT, 0 AS RETURNFLG, 0 AS SLCODE, 0 AS VATFLG, 1 AS CURCONRATE, 1 AS CURCODE, @ORDNO_DN AS ORDNO, 1 AS DFLAG, 0 AS ORGAMT, 0 AS ORGTAXAMT, 0 AS CANCELFLG,
		0 AS VSVRTYPE FROM #TEMP_SI GROUP BY TAXCODE, TAXPERC

		--2 Discount
		IF(@DISCAMT <> 0)
		BEGIN
			INSERT INTO [dbo].[GDND] (VRTYPE, VRNO, INVDATE, CUSCODE, ETYPE, CODE, AMOUNT, TAX, BTYPE,LOGCD,
			TAXAMT, RETURNFLG, SLCODE, VATFLG, CURCONRATE, CURCODE, ORDNO, DFLAG, ORGAMT, ORGTAXAMT, CANCELFLG, VSVRTYPE)

			SELECT 'DN' AS VRTYPE, @INVNO_DN AS VRNO, @DATE AS INVDATE, @CUSCODE AS CUSCODE, 'A' AS ETYPE, @DISCCD AS CODE, @NETAMOUNT AS AMOUNT, @DISCPERC AS TAX, @BTYPE AS BTYPE, @USERID AS LOGCD,
			@DISCAMT AS TAXAMT, 0 AS RETURNFLG, 0 AS SLCODE, 0 AS VATFLG, 1 AS CURCONRATE, 1 AS CURCODE, @ORDNO_DN AS ORDNO, 1 AS DFLAG, 0 AS ORGAMT, 0 AS ORGTAXAMT, 0 AS CANCELFLG, 0 AS VSVRTYPE
		END

		--3 Roundoff
		IF(@ROUNDOFF <> 0)
		BEGIN
			INSERT INTO [dbo].[GDND] (VRTYPE, VRNO, INVDATE, CUSCODE, ETYPE, CODE, AMOUNT, TAX, BTYPE,LOGCD,
			TAXAMT, RETURNFLG, SLCODE, VATFLG, CURCONRATE, CURCODE, ORDNO, DFLAG, ORGAMT, ORGTAXAMT, CANCELFLG, VSVRTYPE)

			SELECT 'DN' AS VRTYPE, @INVNO_DN AS VRNO, @DATE AS INVDATE, @CUSCODE AS CUSCODE, 'A' AS ETYPE, @COINADJCD AS CODE, @NETAMOUNT AS AMOUNT, 0 AS TAX, @BTYPE AS BTYPE, @USERID AS LOGCD,
			@ROUNDOFF AS TAXAMT, 0 AS RETURNFLG, 0 AS SLCODE, 0 AS VATFLG, 1 AS CURCONRATE, 1 AS CURCODE, @ORDNO_DN AS ORDNO, 1 AS DFLAG, 0 AS ORGAMT, 0 AS ORGTAXAMT, 0 AS CANCELFLG, 0 AS VSVRTYPE
		END

		--4 Additional Information
		INSERT INTO [dbo].[GDND] (VRTYPE, VRNO, INVDATE, CUSCODE, ETYPE, CODE, AMOUNT, TAX, BTYPE,LOGCD,
		TAXAMT, RETURNFLG, SLCODE, VATFLG, CURCONRATE, CURCODE, ORDNO, DFLAG, ORGAMT, ORGTAXAMT, CANCELFLG, VSVRTYPE)

		SELECT 'DN' AS VRTYPE, @INVNO_DN AS VRNO, @DATE AS INVDATE, @CUSCODE AS CUSCODE, 'A' AS ETYPE, ACCID AS CODE, @NETAMOUNT AS AMOUNT, 0 AS TAX, @BTYPE AS BTYPE, @USERID AS LOGCD,
		AMOUNT AS TAXAMT, 0 AS RETURNFLG, 0 AS SLCODE, 0 AS VATFLG, 1 AS CURCONRATE, 1 AS CURCODE, @ORDNO_DN AS ORDNO, 0 AS DFLAG, 0 AS ORGAMT, 0 AS ORGTAXAMT, 0 AS CANCELFLG, 0 AS VSVRTYPE FROM @ADDIDTLS

		-- Save STKDT
		DELETE FROM [dbo].[STKDT] WHERE VRTYPE = 'NT' AND TYPE = 'I' AND NTNO = @ORDNO_DN

		INSERT INTO [dbo].[STKDT] (VRTYPE, PRCODE, VRDATE, ORDNO, NTNO, TRNNO, QTY, TYPE, PARTYCD, LOGCD, UTIME, RATE,
		UNITCODE, CONVTYPE, CONVFACT, CONVQTY, PRFLG, CONVRATE, GDWCODE, LUTIME, VSVRTYPE,
		PRV, PRVN, PRVT, SNTSLNO, LM, ADDCOST, CONVADDCOST, CLOSES, ORDROWID, NTROWID, TRNROWID,
		QTNCODE, QTNROWID, PRVQ, PRVQY, PRVY, PRVNY, PRVTY)

		SELECT 'NT' AS VRTYPE, PRCODE, @DATE AS VRDATE, SORDNO AS ORDNO, @ORDNO_DN AS NTNO, @ORDNO AS TRNNO, QTY, 'I' AS TYPE, @CUSCODE AS PARTYCD, @USERID AS LOGCD, GETDATE() AS UTIME, RATE,
		UNITCODE, CONVTYPE, CONVFACT, CONVQTY, PRFLG, CONVRATE, GDWCODE, GETDATE() AS LUTIME, 0 AS VSVRTYPE,
		'N' AS PRV, 'N' AS PRVN, 'N' AS PRVT, SLNO AS SNTSLNO, 'N' AS LM, 0 AS ADDCOST, 0 AS CONVADDCOST, 0 AS CLOSES, ORDROWID, SLNO AS NTROWID, SLNO AS TRNROWID,
		0 AS QTNCODE, 0 AS QTNROWID, 'N' AS PRVQ, '' AS PRVQY, '' AS PRVY, '' AS PRVNY, '' AS PRVTY FROM #TEMP_SI ORDER BY SLNO

		--## Update Stock
		UPDATE P SET P.STOCK = P.STOCK - D.QTY FROM PRMAST P INNER JOIN (SELECT PRCODE, SUM(CONVQTY) AS QTY FROM #TEMP_SI GROUP BY PRCODE) D on P.PRCODE = D.PRCODE

		--## Batch Update
		IF(@BATCHWISE = 1)
		BEGIN
			--## Clear Batch Stock
			UPDATE P  SET P.BALQTY = P.BALQTY + D.CONVQTY FROM PRST P 
			INNER JOIN (SELECT PRCODE, BATCH, GRACK, SUM(CONVQTY) AS CONVQTY, CONVMRP, CONVRATE, MFGDATE, EXPIRY FROM BATCHDET WHERE VSVRTYPE = 'O' AND VRNO = @INVNO GROUP BY PRCODE, BATCH, GRACK, CONVMRP, CONVRATE, MFGDATE, EXPIRY) D ON P.PRCODE = D.PRCODE AND P.BATCH = D.BATCH AND P.GRACK = D.GRACK
			INNER JOIN PRMAST ON PRMAST.PRCODE = P.PRCODE WHERE ISNULL(D.BATCH, '') != '' AND (@BATCHRTEDIT = 1 OR (P.CONVRATE = D.CONVRATE AND P.CONVMRP = D.CONVMRP)) AND (PRMAST.MANDATEFLG = 0 OR P.MFGDATE = D.MFGDATE) AND (PRMAST.EXPDATEFLG = 0 OR P.EXPIRY = D.EXPIRY)
			
			--## Save BATCHDET
			DELETE FROM [dbo].[BATCHDET] WHERE VSVRTYPE = 'O' AND VRNO = @INVNO

			INSERT INTO [dbo].[BATCHDET] (STYPE, VRNO, PURDATE, BILLNO, BILLDATE, SUPCODE, PRCODE, QTY, BATCH, EXPIRY,
			PRATE, RATE, DISC, TAX, TAXCODE, BALQTY, EXPCHECK, LOGCD,
			MRP, MFGDATE, ALTQTY, GRACK, CONVRATE, STKFLG, CONVPRATE, CONVMRP, CONVQTY, VSVRTYPE)

			SELECT 'O' AS STYPE, @INVNO AS VRNO, @DATE AS PURDATE, @INVNOTEXT AS BILLNO, @DATE AS BILLDATE, @CUSCODE AS SUPCODE, PRCODE, QTY, BATCH, EXPDATE AS EXPIRY,
			PRRATE AS PRATE, MRP AS RATE, 0 AS DISC, 0 AS TAX, 0 AS TAXCODE, 0 AS BALQTY, 1 AS EXPCHECK, @USERID AS LOGCD,
			RATE AS MRP, MFGDATE, (CASE WHEN UNITCODE = ALTUNIT THEN QTY ELSE 0 END) AS ALTQTY, GDWCODE AS GRACK, CONVMRP AS CONVRATE, 'I' AS STKFLG, CONVPRRATE, CONVRATE AS CONVMRP, CONVQTY, 0 AS VSVRTYPE FROM #TEMP_SI where ISNULL(BATCH, '') <> ''
			
			--//
			
			DELETE FROM [dbo].[BATCHDET] WHERE VSVRTYPE = 'DN' AND VRNO = @INVNO_DN

			INSERT INTO [dbo].[BATCHDET] (STYPE, VRNO, PURDATE, BILLNO, BILLDATE, SUPCODE, PRCODE, QTY, BATCH, EXPIRY,
			PRATE, RATE, DISC, TAX, TAXCODE, BALQTY, EXPCHECK, LOGCD,
			MRP, MFGDATE, ALTQTY, GRACK, CONVRATE, STKFLG, CONVPRATE, CONVMRP, CONVQTY, VSVRTYPE)

			SELECT 'DN' AS STYPE, @INVNO_DN AS VRNO, @DATE AS PURDATE, @INVNOTEXT_DN AS BILLNO, @DATE AS BILLDATE, @CUSCODE AS SUPCODE, PRCODE, (CASE WHEN UNITCODE = MAINUNIT THEN QTY ELSE 0 END) AS QTY, BATCH, EXPDATE AS EXPIRY,
			PRRATE AS PRATE, MRP AS RATE, 0 AS DISC, 0 AS TAX, 0 AS TAXCODE, 0 AS BALQTY, 1 AS EXPCHECK, @USERID AS LOGCD,
			RATE AS MRP, MFGDATE, (CASE WHEN UNITCODE = ALTUNIT THEN QTY ELSE 0 END) AS ALTQTY, GDWCODE AS GRACK, CONVMRP AS CONVRATE, 'I' AS STKFLG, CONVPRRATE, CONVRATE AS CONVMRP, CONVQTY, 0 AS VSVRTYPE FROM #TEMP_SI where ISNULL(BATCH, '') <> ''

			--## Update Batch Stock
			UPDATE P  SET P.BALQTY = P.BALQTY - D.CONVQTY FROM PRST P 
			INNER JOIN (SELECT PRCODE, BATCH, GDWCODE, SUM(CONVQTY) AS CONVQTY, CONVMRP, CONVRATE, MFGDATE, EXPDATE from #TEMP_SI D GROUP BY PRCODE, BATCH, GDWCODE, CONVMRP, CONVRATE, MFGDATE, EXPDATE) D ON P.PRCODE = D.PRCODE AND P.BATCH = D.BATCH AND P.GRACK = D.GDWCODE
			INNER JOIN PRMAST ON PRMAST.PRCODE = P.PRCODE WHERE ISNULL(D.BATCH, '') != '' AND (@BATCHRTEDIT = 1 OR (P.CONVRATE = D.CONVMRP AND P.CONVMRP = D.CONVRATE)) AND (PRMAST.MANDATEFLG = 0 OR P.MFGDATE = D.MFGDATE) AND (PRMAST.EXPDATEFLG = 0 OR P.EXPIRY = D.EXPDATE)
		END
		
		IF(@SLNOWISE = 1)
		BEGIN
			-- Clear Serial Number Track Tagging
			UPDATE SNTSTK SET OUTVRTYPE = '', OUTVRNO = 0, OUTORDNO = 0, OUTSLNO = 0, USED = 0 WHERE OUTVRTYPE = 'SI' AND OUTVRNO = @INVNO AND OUTORDNO = @ORDNO
			-- Update Serial Number Track Tagging
			UPDATE H SET H.OUTVRTYPE = 'SI', H.OUTVRNO = @INVNO, H.OUTORDNO = @ORDNO, H.OUTSLNO = D.OUTSLNO, USED = 1 FROM SNTSTK H INNER JOIN @SLNOUPDATE D ON H.INVRTYPE = D.INVRTYPE AND H.INVRNO = D.INVRNO AND H.INORDNO = D.INORDNO AND H.INSLNO = D.INSLNO AND H.ITEMSLNO = D.ITEMSLNO
		END

		IF(@SINVNO <> 0)
		BEGIN
			--## Update FYDETAILS
			UPDATE FYDETAILS SET BINVNO = @ORDNO
			UPDATE FYDETAILS SET GDNVRNO = @ORDNO_DN
			SET @RESULT = 'Saved'
		END
		ELSE
		BEGIN
			SET @RESULT = 'Updated'
		END
		
	END

	SELECT @RESULT AS MESSAGE, (CASE WHEN @RESULT = 'Saved' OR @RESULT = 'Updated' THEN 1 ELSE 0 END) AS STATUS

	COMMIT TRAN
	END TRY
	BEGIN CATCH	
		IF(@@TRANCOUNT > 0)
			ROLLBACK TRAN;
	SELECT 'Error: ' + ERROR_MESSAGE() AS MESSAGE, 0 AS STATUS
	END CATCH
END
````

## QuickBalance.API/Program.cs

````csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using QuickBalance.API;
using QuickBalance.API.CLS;
using QuickBalance.API.Configuration;
using QuickBalance.API.DB;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Repositories;
using QuickBalance.API.Repository;
using QuickBalance.API.Repository.Master;
using QuickBalance.API.Repository.Master.QuickBalance.API.Repository.Master;
using QuickBalance.API.Services;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// DATABASE CONFIGURATION
// ============================================================
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
};
builder.Services
    .AddOptions<DatabaseSettings>()
    .Bind(builder.Configuration.GetSection(
        DatabaseSettings.SectionName))
    .Validate(
        settings =>
            !string.IsNullOrWhiteSpace(settings.ServerName) &&
            !string.IsNullOrWhiteSpace(settings.DatabaseName) &&
            !string.IsNullOrWhiteSpace(settings.UserName) &&
            !string.IsNullOrWhiteSpace(settings.Password),
        "DatabaseSettings must contain ServerName, DatabaseName, UserName and Password.")
    .ValidateOnStart();

builder.Services.AddSingleton<
    IDbConnectionFactory,
    DbConnectionFactory>();

builder.Services.AddScoped<DBConn>(serviceProvider =>
{
    var connectionFactory =
        serviceProvider.GetRequiredService<IDbConnectionFactory>();

    var variables = new Variable();

    string connectionString =
        connectionFactory.GetConnectionString(variables);

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "DbConnectionFactory returned an empty connection string.");
    }

    return new DBConn(connectionString);
});
//builder.Services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();

// ============================================================
// TOKEN REVOCATION
// ============================================================

builder.Services.AddSingleton<TokenRevocationService>(
    serviceProvider =>
    {
        var connectionFactory =
            serviceProvider.GetRequiredService<IDbConnectionFactory>();

        var variables = new Variable();

        string connectionString =
            connectionFactory.GetConnectionString(variables);

        return new TokenRevocationService(connectionString);
    });

// ============================================================
// APPLICATION SERVICES
// ============================================================

builder.Services.AddScoped<DBConn>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// ============================================================
// DATABASE CLASSES
// ============================================================
builder.Services.AddScoped<AuthRepository>();
builder.Services.AddScoped<IAuthInterface, AuthService>();
//
//builder.Services.AddScoped<GdwMasDB>();
//builder.Services.AddScoped<GdwGrpDB>();
//builder.Services.AddScoped<CatgMasDB>();
//builder.Services.AddScoped<TaxMasDB>();

// BOM
//builder.Services.AddScoped<BomMasDB>();
//builder.Services.AddScoped<BomTDB>();
//builder.Services.AddScoped<BomDDB>();

//builder.Services.AddScoped<PrMastDB>();
//builder.Services.AddScoped<SmGrpDB>();
//builder.Services.AddScoped<SmMastDB>();
//builder.Services.AddScoped<CurrConvDB>();
//builder.Services.AddScoped<CurrMasDB>();
builder.Services.AddScoped<AccMasDB>();
builder.Services.AddScoped<LedgerGroupDB>();
builder.Services.AddScoped<SupplierDB>();
builder.Services.AddScoped<CustomerDB>();
//builder.Services.AddScoped<PurchaseOrderDB>();

///
builder.Services.AddSingleton<DatabaseConnection>();

builder.Services.AddScoped<ItemGroupDBs>();

builder.Services.AddScoped<IItemGroupInterface,ItemGroupService>();
builder.Services.AddScoped<PartyGroupRepository>();
builder.Services.AddScoped<IPartyGroupInterface, PartyGroupService>();
builder.Services.AddScoped<ProductDB>();
builder.Services.AddScoped<IProductInterface, ProductService>();
builder.Services.AddScoped<ItemCategoryDB>();
builder.Services.AddScoped<IItemCategoryInterface, ItemCategoryService>();
builder.Services.AddScoped<BrandDB>();
builder.Services.AddScoped<IBrandInterface, BrandService>();
builder.Services.AddScoped<IUnitInterface, UnitService>();
builder.Services.AddScoped<PrUnitMasDB>();
builder.Services.AddScoped<ITaxInterface, TaxService>();
builder.Services.AddScoped<TaxMasDB>();
builder.Services.AddScoped<PriceCategoryRepository>();
builder.Services.AddScoped<IPriceCategoryInterface, PriceCategoryService>();
builder.Services.AddScoped<ICurrencyInterface, CurrencyService>();
builder.Services.AddScoped<CurrencyRepository>();
builder.Services.AddScoped<IRegionInterface, RegionService>();
builder.Services.AddScoped<RegionRepository>();
builder.Services.AddScoped<CountryRepository>();
builder.Services.AddScoped<ICountryService, CountryService>();


builder.Services.AddScoped<UnitConversionRepository>();

builder.Services.AddScoped<IUnitConversionService,UnitConversionService>();
builder.Services.AddScoped<BomRepository>();
builder.Services.AddScoped<IBomService, BomService>();
builder.Services.AddScoped<GdwGrpRepository>();
builder.Services.AddScoped<ILocationGroupService, LocationGroupService>();
builder.Services.AddScoped<GdwMasRepository>();
builder.Services.AddScoped<ILocationService, LocationService>();

builder.Services.AddScoped<CurrencyConversionRepository>();
builder.Services.AddScoped<ICurrencyConversionInterface, CurrencyConversionService>();

builder.Services.AddScoped<SalesManGroupRepository>();
builder.Services.AddScoped<ISalesmanGroupInterface, SalesmanGroupService>();


builder.Services.AddScoped<SalesManRepository>();
builder.Services.AddScoped<ISalesmanInterface, SalesmanService>();
// ============================================================
// CONTROLLERS AND JSON
// ============================================================

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            JsonNamingPolicy.CamelCase;

        options.JsonSerializerOptions.DictionaryKeyPolicy =
            JsonNamingPolicy.CamelCase;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors)
                .Select(x => x.ErrorMessage)
                .Distinct();

            string message = string.Join(" ", errors);

            return new BadRequestObjectResult(
                ApiResponseWithError.Failure(message));
        };
    });

builder.Services.AddEndpointsApiExplorer();

// ============================================================
// SWAGGER
// ============================================================

builder.Services.AddSwaggerGen(options =>
{
    options.OperationFilter<ApiResponseOperationFilter>();

    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API Documentation",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token."
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});

builder.Services.AddMemoryCache();

// ============================================================
// JWT CONFIGURATION
// ============================================================

var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

var configuredTenantId =
    builder.Configuration["DatabaseSettings:TenantId"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "Jwt:Key is not configured.");
}

if (string.IsNullOrWhiteSpace(jwtIssuer))
{
    throw new InvalidOperationException(
        "Jwt:Issuer is not configured.");
}

if (string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "Jwt:Audience is not configured.");
}

if (string.IsNullOrWhiteSpace(configuredTenantId))
{
    throw new InvalidOperationException(
        "DatabaseSettings:TenantId is not configured.");
}
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,

                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)),

                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

        options.Events = new JwtBearerEvents
        {
            // 1. Validate token, tenant and revocation.
            OnTokenValidated = async context =>
            {
                string? tokenTenantId =
                    context.Principal?
                        .FindFirst("tenant_id")?.Value;

                if (string.IsNullOrWhiteSpace(tokenTenantId) ||
                    !string.Equals(
                        tokenTenantId,
                        configuredTenantId,
                        StringComparison.Ordinal))
                {
                    context.Fail("Invalid tenant.");
                    return;
                }

                string authorizationHeader =
                    context.Request.Headers.Authorization.ToString();

                if (!authorizationHeader.StartsWith(
                    "Bearer ",
                    StringComparison.OrdinalIgnoreCase))
                {
                    context.Fail("Authorization token is required.");
                    return;
                }

                string token =
                    authorizationHeader.Substring(7).Trim();

                if (string.IsNullOrWhiteSpace(token))
                {
                    context.Fail("Authorization token is required.");
                    return;
                }

                var revocationService =
                    context.HttpContext.RequestServices
                        .GetRequiredService<TokenRevocationService>();

                try
                {
                    bool isRevoked =
                        await revocationService.IsTokenRevokedAsync(token);

                    if (isRevoked)
                    {
                        context.Fail("This token has been logged out.");
                    }
                }
                catch (Exception ex)
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("JwtRevocation");

                    logger.LogError(
                        ex,
                        "Failed to check JWT revocation in SQL Server.");

                    context.Fail(
                        "Unable to validate token revocation.");
                }
            },

            // 2. Global 401 Unauthorized response.
            OnChallenge = async context =>
            {
                context.HandleResponse();

                context.Response.StatusCode =
                    StatusCodes.Status401Unauthorized;

                context.Response.ContentType =
                    "application/json";

                var response = ApiResponseWithError.Failure(
                    "Unauthorized user.");

                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(response, jsonOptions));
            },

            // 3. Global 403 Forbidden response.
            OnForbidden = async context =>
            {
                context.Response.StatusCode =
                    StatusCodes.Status403Forbidden;

                context.Response.ContentType =
                    "application/json";

                var response = ApiResponseWithError.Failure(
                    "You are not authorized to access this resource.");

                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(response, jsonOptions));
            }
        };
    });
// ============================================================
// AUTHORIZATION
// ============================================================

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy =
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireAssertion(context =>
            {
                var value = context.User
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                return int.TryParse(value, out int userId) &&
                       userId > 0;
            })
            .Build();
});

// ============================================================
// CORS
// ============================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins("http://localhost:8081")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ============================================================
// RATE LIMITING
// ============================================================

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode =
            StatusCodes.Status429TooManyRequests;

        context.HttpContext.Response.ContentType =
            "application/json";

        var response = new
        {
            status = false,
            message = "Too many login attempts. Please try again later."
        };

        await context.HttpContext.Response.WriteAsync(
            JsonSerializer.Serialize(response),
            cancellationToken);
    };

    options.AddPolicy("LoginLimit", httpContext =>
    {
        string ipAddress =
            httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ipAddress,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 4,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

// ============================================================
// BUILD APPLICATION
// ============================================================
var app = builder.Build();

app.UseStaticFiles();

app.UseCors("FrontendPolicy");

// Keep Swagger accessible without authentication.
app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "API Documentation");

    options.DocumentTitle = "API Documentation";
    options.ConfigObject.PersistAuthorization = true;
    options.InjectStylesheet("/swagger/custom.css");
});

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.Run();
````

## QuickBalance.API/Configuration/DB/IDbConnectionFactory.cs

````csharp
using Microsoft.Data.SqlClient;
using QuickBalance.API.DB;

namespace QuickBalance.API.Configuration
{
    public interface IDbConnectionFactory
    {
        string GetConnectionString(
            Variable variables,
            bool isMasterDB = false);

        SqlConnection CreateConnection(
            Variable variables,
            bool isMasterDB = false);
    }
}
````

## QuickBalance.API/Configuration/DbConnectionFactory.cs

````csharp
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using QuickBalance.API.DB;

namespace QuickBalance.API.Configuration
{
    public sealed class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly DatabaseSettings _settings;

        public DbConnectionFactory(
            IOptions<DatabaseSettings> options)
        {
            _settings = options.Value;
        }

        public string GetConnectionString(
            Variable variables,
            bool isMasterDB = false)
        {
            ArgumentNullException.ThrowIfNull(variables);

            var databaseName = isMasterDB
                ? "MasterDB"
                : _settings.DatabaseName;

            var builder = new SqlConnectionStringBuilder
            {
                DataSource = _settings.ServerName,
                InitialCatalog = databaseName,
                UserID = _settings.UserName,
                Password = _settings.Password,
                Encrypt = true,
                TrustServerCertificate = true,
                MultipleActiveResultSets = true,
                PersistSecurityInfo = false
            };

            // Preserve existing Variable initialization.
            //variables.ServerName = _settings.ServerName;
            //variables.DatabaseName = _settings.DatabaseName;
            //variables.UserName = _settings.UserName;
            //variables.Password = _settings.Password;

            variables.ServerName = _settings.ServerName;
            variables.DatabaseName = databaseName;
            variables.UserName = _settings.UserName;
            variables.Password = _settings.Password;
            // Preserve the existing CompCode derivation.
            string value = variables.DatabaseName;
            int number = 1;

            if (!string.IsNullOrEmpty(value))
            {
                string part = value
                    .Split('_')[0]
                    .Replace("COMP", "");

                if (int.TryParse(part, out int result))
                {
                    number = result;
                }
            }

            variables.CompCode = number;

            return builder.ConnectionString;
        }

        public SqlConnection CreateConnection(
            Variable variables,
            bool isMasterDB = false)
        {
            string connectionString =
                GetConnectionString(variables, isMasterDB);

            return new SqlConnection(connectionString);
        }
    }
}
````
