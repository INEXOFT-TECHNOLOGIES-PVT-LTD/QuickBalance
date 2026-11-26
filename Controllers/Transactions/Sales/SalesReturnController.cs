using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using QuickBalance.API.Configuration;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using System.Data;
using System.Security.Claims;

namespace QuickBalance.API.Controllers.Transactions.Sales
{
    [ApiExplorerSettings(IgnoreApi = true)]   // hidden: replaced by the new layered API
    [ApiController]
    [Tags("27. Sales Returns")]

    [Route("api/v1/sales-returns")]
    [Authorize]
    public class SalesReturnController : ControllerBase
    {
        private readonly DatabaseConnection _databaseConnection;
        public SalesReturnController(DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }
        [HttpPost("manage")]
        public async Task<IActionResult> ManageSalesReturn([FromBody] SalesReturnRequest request)
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

                //-- Manage Sales Return Details

                try
                {
                    if (request == null)
                        return BadRequest(new { status = false, message = "Return data is required.", errors = new List<object>() });

                    if (request.Items == null || request.Items.Count == 0)
                        return BadRequest(new { status = false, message = "At least one return item is required.", errors = new List<object>() });

                    if (request.CustomerCode == 0)
                        return BadRequest(new { status = false, message = "Party details are required.", errors = new List<object>() });

                    SalesReturnDB salesReturnDB = new SalesReturnDB(DBConn);
                    DataTable result = await salesReturnDB.ManageSalesReturn(request, Variables.UserId);
                    if (result.Rows.Count == 0)
                        return StatusCode(500, new { status = false, message = "No response received from sales return procedure.", errors = new List<object>() });

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
        public async Task<IActionResult> ListSalesReturn([FromQuery] PaginationRequest request)
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

                DBConn db = new DBConn(connectionString);
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
                    clsSalM.SType = "R";
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
                        message = tableData.Count > 0 ? "Sales return details loaded successfully." : "No sales return details found.",
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
        public async Task<IActionResult> GetSelectedSalesReturn(int id)
        {
            try
            {
                if(id <= 0)
                    return BadRequest(new { status = false, message = "Sales return number is required.", errors = new List<object>() });

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

                SalMDB salMDB = new SalMDB(DBConn);
                salMDB.Variables = Variables;
                salMDB.Functions.Variables = Variables;
                ClsSalM clsSalM = new ClsSalM();

                SalTDB salTDB = new SalTDB(DBConn);
                salTDB.Variables = Variables;
                salTDB.Functions.Variables = Variables;
                ClsSalT clsSalT = new ClsSalT();

                SntStkDB sntStkDB = new SntStkDB(DBConn);
                sntStkDB.Variables = Variables;
                sntStkDB.Functions.Variables = Variables;
                ClsSntStk clsSntStk = new ClsSntStk();

                SalDDB salDDB = new SalDDB(DBConn);
                salDDB.Variables = Variables;
                salDDB.Functions.Variables = Variables;
                ClsSalD clsSalD = new ClsSalD();

                //-- Get Selected Sales Invoice Details

                clsSalM.InvNo = id;
                DataTable dtHdr = await salMDB.SearchByInvno(clsSalM);

                clsSalT.InvNo = id;
                DataTable dtItem = await salTDB.SearchByInvno(clsSalT);

                clsSntStk.INVRTYPE = "SR";
                clsSntStk.INVRNO = id;
                clsSntStk.INORDNO = Convert.ToInt32(dtHdr.Rows[0]["ORDNO"]);
                DataTable dtSlNo = await sntStkDB.GetByVoucherIn(clsSntStk);

                clsSalD.DFlag = false;
                clsSalD.VrNo = id;
                clsSalD.VrType = "R";
                DataTable dtAdd = await salDDB.SearchAddInfo(clsSalD);                

                if (dtHdr == null || dtHdr.Rows.Count == 0)
                {
                    return NotFound(new { status = false, message = "Sales return not found.", errors = new List<object>() });
                }
                else
                {
                    int SalesInvoiceId = Convert.ToInt32(dtHdr.Rows[0]["RTNINVNO"]);
                    string SalesInvoiceNumber = "";
                    if (SalesInvoiceId > 0)
                    {
                        clsSalM.OrdNo = SalesInvoiceId;
                        clsSalM.ReturnFlg = false;
                        DataTable dtSalesInvoice = await salMDB.SearchByOrdNo(clsSalM);
                        if (dtSalesInvoice != null && dtSalesInvoice.Rows.Count > 0)
                            SalesInvoiceNumber = Convert.ToString(dtSalesInvoice.Rows[0][commonFunction.Functions.VSDOCNO()]) ?? "";
                    }

                    var invoiceDetails = new
                    {
                        Header = dtHdr.AsEnumerable().Select(row => dtHdr.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).FirstOrDefault(),
                        salesinvoicenumber = SalesInvoiceNumber,
                        Items = dtItem.AsEnumerable().Select(row => dtItem.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).ToList(),
                        SerialNumbers = dtSlNo.AsEnumerable().Select(row => dtSlNo.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).ToList(),
                        AdditionalInformations = dtAdd.AsEnumerable().Select(row => dtAdd.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).ToList()
                    };

                    return Ok(new { status = true, message = "Sales return retrieved successfully.", data = invoiceDetails });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message, errors = new List<object>() });
            }
        }

        [HttpPost("cancel/{id:int}")]
        public async Task<IActionResult> CancelSelectedSalesReturn(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { status = false, message = "Sales return number is required.", errors = new List<object>() });

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

                //-- Cancel Sales Invoice Details

                try
                {
                    SalesReturnDB salesReturnDB = new SalesReturnDB(DBConn);
                    DataTable result = await salesReturnDB.CancelSalesReturn(id, Variables.UserId);
                    if (result.Rows.Count == 0)
                        return StatusCode(500, new { status = false, message = "No response received from sales return procedure.", errors = new List<object>() });

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
    }
}