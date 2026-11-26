# Sales Order review input

## Controllers/Transactions/Sales/SalesOrderController.cs

```csharp
﻿using Microsoft.AspNetCore.Authorization;
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
```

## DB/SalesOrderDB.cs

```csharp
﻿using Azure.Core;
using Microsoft.Data.SqlClient;
using QuickBalance.API.DTOs;
using System.Data;

namespace QuickBalance.API.DB
{
    public class SalesOrderDB
    {
        private readonly DBConn db;

        DataTable itemDetails = new();

        DataTable additionalDetails = new();

        public SalesOrderDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public void CreateItemTable(SalesOrderRequest request)
        {
            itemDetails = new();
            itemDetails.Columns.Add("SLNO", typeof(int));
            itemDetails.Columns.Add("PRCODE", typeof(int));
            itemDetails.Columns.Add("QTY", typeof(decimal));
            itemDetails.Columns.Add("RATE", typeof(decimal));
            itemDetails.Columns.Add("LDISCPERC", typeof(decimal));
            itemDetails.Columns.Add("LDISCPRICE", typeof(decimal));
            itemDetails.Columns.Add("DISCPERC", typeof(decimal));
            itemDetails.Columns.Add("DISCPRICE", typeof(decimal));
            itemDetails.Columns.Add("UNITCODE", typeof(int));
            itemDetails.Columns.Add("GDWCODE", typeof(int));
            itemDetails.Columns.Add("ALCODE", typeof(string));
            itemDetails.Columns.Add("ALNAME", typeof(string));
            itemDetails.Columns.Add("CONVTYPE", typeof(int));
            itemDetails.Columns.Add("CONVFACT", typeof(decimal));
            itemDetails.Columns.Add("TAXABLE", typeof(decimal));
            itemDetails.Columns.Add("TAXCODE", typeof(int));
            itemDetails.Columns.Add("TAXPERC", typeof(decimal));
            itemDetails.Columns.Add("TAXAMOUNT", typeof(decimal));
            itemDetails.Columns.Add("NETAMOUNT", typeof(decimal));

            List<SalesOrderItemRequest> items = request.Items ?? new List<SalesOrderItemRequest>();

            foreach (var item in items)
            {
                item.AlCode = "0";
                item.AlName = "";
                item.GdwCode = item.GdwCode == 0 ? 1 : item.GdwCode;
                item.ConvFact = item.ConvFact == 0 ? 1 : item.ConvFact;
                item.NetAmount = item.Taxable + item.TaxAmount;

                itemDetails.Rows.Add(
                    item.SlNo,
                    item.PrCode,
                    item.Qty,
                    item.Rate,
                    item.LDiscPerc,
                    item.LDiscPrice,
                    item.DiscPerc,
                    item.DiscPrice,
                    item.UnitCode,
                    item.GdwCode,
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
            }

            //--

            additionalDetails = new();
            additionalDetails.Columns.Add("ACCID", typeof(int));
            additionalDetails.Columns.Add("AMOUNT", typeof(decimal));

            List<SalesOrderAdditionalRequest> additional = request.Additional ?? new List<SalesOrderAdditionalRequest>();

            foreach (var item in additional)
            {
                additionalDetails.Rows.Add(
                    item.AccId,
                    item.Amount
                );
            }
        }

        public async Task<DataTable> ManageSalesOrder(SalesOrderRequest request, int UserId)
        {
            CreateItemTable(request);

            List<SqlParameter> parameters = new();

            parameters.Add(new SqlParameter("@SORDNO", SqlDbType.Int) { Value = request.SordNo });
            parameters.Add(new SqlParameter("@DATE", SqlDbType.DateTime) { Value = request.Date });
            parameters.Add(new SqlParameter("@CUSCODE", SqlDbType.Int) { Value = request.CustomerCode });
            parameters.Add(new SqlParameter("@SMCODE", SqlDbType.Int) { Value = request.SalesmanCode });
            parameters.Add(new SqlParameter("@DISCPERC", SqlDbType.Decimal) { Precision = 18, Scale = 6, Value = request.DiscPerc });
            parameters.Add(new SqlParameter("@DISCAMT", SqlDbType.Decimal) { Precision = 18, Scale = 6, Value = request.DiscAmt });
            parameters.Add(new SqlParameter("@ROUNDOFF", SqlDbType.Decimal) { Precision = 18, Scale = 6, Value = request.RoundOff });
            parameters.Add(new SqlParameter("@USERID", SqlDbType.Int) { Value = UserId });

            SqlParameter itemParameter = new SqlParameter("@ITEMDTLS", SqlDbType.Structured) { TypeName = "dbo.ASK_UDT_SalesOrderItem", Value = itemDetails };
            parameters.Add(itemParameter);

            SqlParameter additionalParameter = new SqlParameter("@ADDIDTLS", SqlDbType.Structured) { TypeName = "dbo.ASK_UDT_SalesOrderAdditionalInfo", Value = additionalDetails };
            parameters.Add(additionalParameter);

            return await db.ExecuteStoredProcedure("ASK_SP_ManageSalesOrderDetails", parameters);
        }

        public async Task<DataTable> CancelSalesOrder(int SordNo, int UserId)
        {
            List<SqlParameter> parameters = new();

            parameters.Add(new SqlParameter("@SORDNO", SqlDbType.Int) { Value = SordNo });
            parameters.Add(new SqlParameter("@USERID", SqlDbType.Int) { Value = UserId });

            return await db.ExecuteStoredProcedure("ASK_SP_CancelSalesOrderDetails", parameters);
        }

        public async Task<DataTable> PendingSalesOrderInInvoices(PaginationRequest request)
        {
            List<SqlParameter> parameters = new();

            parameters.Add(new SqlParameter("@CUSTOMERID", SqlDbType.Int) { Value = request.CustomerId });
            parameters.Add(new SqlParameter("@ORDERID", SqlDbType.Int) { Value = request.OrderId });
            parameters.Add(new SqlParameter("@PAGE", SqlDbType.Int) { Value = request.Page });
            parameters.Add(new SqlParameter("@PAGESIZE", SqlDbType.Int) { Value = request.PageSize });
            parameters.Add(new SqlParameter("@SEARCH", SqlDbType.NVarChar, -1) { Value = request.Search ?? "" });
            parameters.Add(new SqlParameter("@SORTBY", SqlDbType.NVarChar, 50) { Value = request.SortBy == null ? "" : request.SortBy == "ordNo" || request.SortBy == "orderNo" ? "no" : "date" });
            parameters.Add(new SqlParameter("@SORTDIRECTION", SqlDbType.NVarChar, 50) { Value = request.SortDirection == null ? "" : request.SortDirection.ToUpper() == "ASC" ? "ASC" : "DESC" });

            return await db.ExecuteStoredProcedure("ASK_SP_PendingSalesOrderInInvoices", parameters);
        }
    }
}
```

## DB/CordMDB.cs

```csharp
﻿using Dapper;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs.Respons_DTO;
using System.Data;

namespace QuickBalance.API.DB
{
    public class CordMDB
    {
        private readonly DBConn db;
        public Variable Variables = new Variable();
        public Function Functions = new Function();
        string qry = "";

        public CordMDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public async Task<DataTable> ListAsync(string STYPE)
        {
            //int page = Variables.pgRequest.Page;
            //int pageSize = Variables.pgRequest.PageSize;

            //string? search = Variables.pgRequest.Search;
            //DateTime? fromDate = Variables.pgRequest.FromDate;
            //DateTime? toDate = Variables.pgRequest.ToDate;


            int page = Variables.Page;
            int pageSize = Variables.PageSize;

            string? search = Variables.Search;
            DateTime? fromDate = Variables.FromDate;
            DateTime? toDate = Variables.ToDate;

            int offset = (page - 1) * pageSize;

            string searchCondition = "";

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Replace("'", "''");

                searchCondition = $@"
            AND (
                CORDM.{Functions.VSDOCNO()} LIKE '%{search}%'
                OR ACCMAS.ACCNAME LIKE '%{search}%'
            )";
            }

            string dateCondition = "";

            if (fromDate.HasValue)
            {
                dateCondition += $@"
            AND CORDM.ORDDATE >= '{fromDate.Value.ToString(Variables.format)}'";
            }

            if (toDate.HasValue)
            {
                dateCondition += $@"
            AND CORDM.ORDDATE < '{toDate.Value.AddDays(1).ToString(Variables.format)}'";
            }

            qry = $@"
        SELECT
            CORDM.ORDNO AS Id,
            CORDM.{Functions.VSDOCNO()} AS [Order No],
            FORMAT(CORDM.ORDDATE, '{Variables.format}') AS [Order Date],
            ACCMAS.ACCNAME AS Customer,
            FORMAT(CORDM.AMOUNT, '{Variables.DecString}') AS Amount,
            COUNT(*) OVER() AS TotalRecords
        FROM CORDM
        INNER JOIN ACCMAS ON CORDM.CUSCODE = ACCMAS.ID
        WHERE CORDM.STYPE = '{STYPE}'
            {Functions.LIST_ALLOW_VR_STMNT(true, "CORDM")}
            {searchCondition}
            {dateCondition}
        ORDER BY CORDM.ORDNO DESC
        OFFSET {offset} ROWS
        FETCH NEXT {pageSize} ROWS ONLY";

            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> List(string STYPE)
        {
            int page = Variables.pgRequest.Page;
            int pageSize = Variables.pgRequest.PageSize;

            string? search = Variables.pgRequest.Search;
            DateTime? fromDate = Variables.pgRequest.FromDate;
            DateTime? toDate = Variables.pgRequest.ToDate;

            string orderBy = "CORDM.ORDNO DESC";
            switch (Variables.pgRequest.SortBy?.ToLower())
            {
                case "ordno":
                case "orderno":
                    orderBy = "CORDM.ORDNO";
                    break;

                case "date":
                case "orderdate":
                    orderBy = "CORDM.ORDDATE";
                    break;

                case "customer":
                    orderBy = "ACCMAS.ACCNAME";
                    break;

                case "amount":
                    orderBy = "CORDM.AMOUNT";
                    break;
            }
            string sortDirection = Variables.pgRequest.SortDirection == "ASC" ? "ASC" : "DESC";
            orderBy += $" {sortDirection}";

            int offset = (page - 1) * pageSize;

            string searchCondition = "";

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Replace("'", "''");
                searchCondition = $@" AND (CORDM.{Functions.VSDOCNO()} LIKE '%{search}%' OR ACCMAS.ACCNAME LIKE '%{search}%')";
            }

            string dateCondition = "";
            if (fromDate.HasValue)
                dateCondition += $" AND CORDM.ORDDATE >= '{fromDate.Value.ToString(Variables.format)}'";
            if (toDate.HasValue)
                dateCondition += $" AND CORDM.ORDDATE < '{toDate.Value.AddDays(1).ToString(Variables.format)}'";

            qry = $@"SELECT CORDM.ORDNO AS Id, CORDM.{Functions.VSDOCNO()} AS [Order No], FORMAT(CORDM.ORDDATE, '{Variables.format}') AS [Order Date], ACCMAS.ACCNAME AS Customer, FORMAT(CORDM.AMOUNT, '{Variables.DecString}') AS Amount, COUNT(*) OVER() AS TotalRecords
FROM CORDM INNER JOIN ACCMAS ON CORDM.CUSCODE = ACCMAS.ID
WHERE CORDM.STYPE = '{STYPE}'{Functions.LIST_ALLOW_VR_STMNT(true, "CORDM")}{searchCondition}{dateCondition}
ORDER BY {orderBy}
OFFSET {offset} ROWS
FETCH NEXT {pageSize} ROWS ONLY";

            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> SearchByOrdNO(ClsCordM clsCordM)
        {
            qry = "SELECT SMMAST.SMNAME, ACCMAS.ACCNAME, CORDM.*, CORDM.INVNO AS SALNO, DTUSER.USERNAME FROM (((ACCMAS INNER JOIN CORDM ON ACCMAS.ID = CORDM.CUSCODE) LEFT JOIN SMMAST ON CORDM.SMCODE = SMMAST.SMCODE) LEFT JOIN (SELECT USERCODE, FULLNAME AS USERNAME FROM TBUSER) AS DTUSER ON CORDM.LOGCD = DTUSER.USERCODE) WHERE CORDM.ORDNO='" + clsCordM.OrdNo + "' AND CORDM.STYPE='" + clsCordM.SType + "'";
            return await db.GetDatatable(qry);
        }

        //public async Task<SalesOrderHeaderDto?> SearchByOrdNOAsync(ClsCordM request)
        //{
        //    const string sql = @"
        //SELECT
        //    SMMAST.SMNAME,
        //    ACCMAS.ACCNAME,
        //    CORDM.*,
        //    CORDM.INVNO AS SALNO,
        //    DTUSER.USERNAME
        //FROM ACCMAS
        //INNER JOIN CORDM
        //    ON ACCMAS.ID = CORDM.CUSCODE
        //LEFT JOIN SMMAST
        //    ON CORDM.SMCODE = SMMAST.SMCODE
        //LEFT JOIN
        //(
        //    SELECT
        //        USERCODE,
        //        FULLNAME AS USERNAME
        //    FROM TBUSER
        //) AS DTUSER
        //    ON CORDM.LOGCD = DTUSER.USERCODE
        //WHERE CORDM.ORDNO = @OrdNo
        //  AND CORDM.STYPE = @SType;";

        //    //using var connection = db.GetConnection();

        //    //if (connection.State != ConnectionState.Open)
        //    //    await connection.OpenAsync();

        //    return await db.QueryFirstOrDefaultAsync<SalesOrderHeaderDto>(
        //        sql,
        //        new
        //        {
        //            request.OrdNo,
        //            request.SType
        //        });
        //}

        public async Task<SalesOrderHeaderDto?> SearchByOrdNOAsync(ClsCordM request)
        {
            const string sql = @"
            SELECT
                CORDM.*,
                CORDM.INVNO AS SALNO,
                ACCMAS.ACCNAME AS ACCNAME,
                SMMAST.SMNAME AS SMNAME,
                DTUSER.USERNAME AS USERNAME
            FROM CORDM
            INNER JOIN ACCMAS
                ON ACCMAS.ID = CORDM.CUSCODE
            LEFT JOIN SMMAST
                ON SMMAST.SMCODE = CORDM.SMCODE
            LEFT JOIN
            (
                SELECT
                    USERCODE,
                    FULLNAME AS USERNAME
                FROM TBUSER
            ) AS DTUSER
                ON DTUSER.USERCODE = CORDM.LOGCD
            WHERE CORDM.ORDNO = @OrdNo
              AND CORDM.STYPE = @SType;";

            return await db.QueryFirstOrDefaultAsync<SalesOrderHeaderDto>(
                sql,
                new
                {
                    request.OrdNo,
                    request.SType
                });
        }


    }
}

```

## DB/CordTDB.cs

```csharp
﻿using Dapper;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs.Respons_DTO;
using System.Data;

namespace QuickBalance.API.DB
{
    public class CordTDB
    {
        private readonly DBConn db;
        public Variable Variables = new Variable();
        public Function Functions = new Function();
        string qry = "";

        public CordTDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public async Task<DataTable> SearchByOrdNo(ClsCordT clsCordT)
        {
            qry = "SELECT PRMAST.PRNAME AS PRNAME,PRMAST.ALIAS,PRMAST.SECNAME AS SECNAME,PRMAST.UNIT,PRMAST.ALTUNIT,PRMAST.SALESPRICETYPE,PRMAST.BMEQN,PRUNITMAS.PRUNITNAME,PRUNITMAS.SECNAME AS UNITSECNAME, CORDT.* FROM ((PRMAST INNER JOIN (SELECT *, ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS SL FROM CORDT) AS CORDT ON PRMAST.PRCODE = CORDT.PRCODE)LEFT JOIN PRUNITMAS ON CORDT.UNITCODE = PRUNITMAS.PRUNITID) WHERE ORDNO='" + clsCordT.OrdNo + "' AND STYPE='" + clsCordT.SType + "' ORDER BY SL";
            return await db.GetDatatable(qry);
        }
        //public async Task<List<SalesOrderItemDto>> SearchByOrdNoAsync(ClsCordT request)
        //{
        //    const string sql = @"
        //    SELECT
        //        PRMAST.PRNAME AS PRNAME,
        //        PRMAST.ALIAS,
        //        PRMAST.SECNAME AS SECNAME,
        //        PRMAST.UNIT,
        //        PRMAST.ALTUNIT,
        //        PRMAST.SALESPRICETYPE,
        //        PRMAST.BMEQN,
        //        PRUNITMAS.PRUNITNAME,
        //        PRUNITMAS.SECNAME AS UNITSECNAME,
        //        CORDT.*
        //    FROM PRMAST
        //    INNER JOIN
        //    (
        //        SELECT
        //            *,
        //            ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS SL
        //        FROM CORDT
        //    ) AS CORDT
        //        ON PRMAST.PRCODE = CORDT.PRCODE
        //    LEFT JOIN PRUNITMAS
        //        ON CORDT.UNITCODE = PRUNITMAS.PRUNITID
        //    WHERE CORDT.ORDNO = @OrdNo
        //      AND CORDT.STYPE = @SType
        //    ORDER BY CORDT.SL;";

        //    //using var connection = db.GetConnection();

        //    //if (connection.State != ConnectionState.Open)
        //    //    await connection.OpenAsync();

        //    var result = await db.QueryListAsync<SalesOrderItemDto>(
        //        sql,
        //        new
        //        {
        //            request.OrdNo,
        //            request.SType
        //        });

        //    return result.AsList();
        //}
        //      public async Task<List<SalesOrderItemDto>> SearchByOrdNoAsync(
        //ClsCordT request)
        //      {
        //          const string sql = @"
        //      SELECT
        //          PRMAST.PRNAME AS PRNAME,
        //          PRMAST.ALIAS AS ALIAS,
        //          PRMAST.SECNAME AS SECNAME,
        //          PRMAST.UNIT AS UNIT,
        //          PRMAST.ALTUNIT AS ALTUNIT,
        //          PRMAST.SALESPRICETYPE AS SALESPRICETYPE,
        //          PRMAST.BMEQN AS BMEQN,
        //          PRUNITMAS.PRUNITNAME AS PRUNITNAME,
        //          PRUNITMAS.SECNAME AS UNITSECNAME,
        //          CORDT.*
        //      FROM CORDT
        //      LEFT JOIN PRMAST
        //          ON PRMAST.PRCODE = CORDT.PRCODE
        //      LEFT JOIN PRUNITMAS
        //          ON CORDT.UNITCODE = PRUNITMAS.PRUNITID
        //      WHERE CORDT.ORDNO = @OrdNo
        //        AND CORDT.STYPE = @SType
        //      ORDER BY CORDT.ORDNO;";

        //          var result = await db.QueryListAsync<SalesOrderItemDto>(
        //              sql,
        //              new
        //              {
        //                  request.OrdNo,
        //                  request.SType
        //              });

        //          return result.AsList();
        //      }


        public async Task<List<SalesOrderItemDto>> SearchByOrdNoAsync(
      ClsCordT request)
        {
            const string sql = @"
          SELECT
              CORDT.*,
              PRMAST.PRNAME AS PRNAME,
              PRMAST.ALIAS AS ALIAS,
              PRMAST.SECNAME AS SECNAME,
              PRMAST.UNIT AS UNIT,
              PRMAST.ALTUNIT AS ALTUNIT,
              PRMAST.SALESPRICETYPE AS SALESPRICETYPE,
              PRMAST.BMEQN AS BMEQN,
              PRUNITMAS.PRUNITNAME AS PRUNITNAME,
              PRUNITMAS.SECNAME AS UNITSECNAME
          FROM
          (
              SELECT
                  C.*,
                  ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS SL
              FROM CORDT AS C
          ) AS CORDT
          INNER JOIN PRMAST
              ON PRMAST.PRCODE = CORDT.PRCODE
          LEFT JOIN PRUNITMAS
              ON PRUNITMAS.PRUNITID = CORDT.UNITCODE
          WHERE CORDT.ORDNO = @OrdNo
            AND CORDT.STYPE = @SType
          ORDER BY CORDT.SL;";

            var result = await db.QueryListAsync<SalesOrderItemDto>(
                sql,
                new
                {
                    request.OrdNo,
                    request.SType
                });

            return result.AsList();
        }

        public async Task<DataTable> SearchByPrCode2(ClsCordT clsCordT)
        {
            qry = "SELECT * FROM dbo.CORDT WHERE PRCODE = '" + clsCordT.PrCode + "' AND CANCELFLG = 'False'";
            return await db.GetDatatable(qry);
        }
    }
}

```

## DB/CordDDB.cs

```csharp
﻿using Dapper;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs.Respons_DTO;
using System.Data;

namespace QuickBalance.API.DB
{
    public class CordDDB
    {
        private readonly DBConn db;
        public Variable Variables = new Variable();
        public Function Functions = new Function();
        string qry = "";

        public CordDDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public async Task<DataTable> SearchAddInfo(ClsCordD clsCordD)
        {
            qry = "SELECT CODE,TAX,TAXAMT FROM CORDD WHERE VRNO='" + clsCordD.VrNo + "' AND VRTYPE='" + clsCordD.VrType + "' AND DFLAG='" + clsCordD.DFlag + "' AND CODE<>'0'";
            return await db.GetDatatable(qry);
        }
        public async Task<List<AddInfoDto>> SearchAddInfoAsync(ClsCordD request)
        {
            const string sql = @"
            SELECT CODE, TAX, TAXAMT
            FROM CORDD
            WHERE VRNO = @VrNo
              AND VRTYPE = @VrType
              AND DFLAG = @DFlag
              AND CODE <> '0';";



            var result = await db.QueryListAsync<AddInfoDto>(
                sql,
                new
                {
                    request.VrNo,
                    request.VrType,
                    request.DFlag
                });

            return result.AsList();
        }
    }
}

```

## DTOs/SalesOrderRequest.cs

```csharp
﻿namespace QuickBalance.API.DTOs
{
    public class SalesOrderRequest
    {
        public int SordNo { get; set; }

        public DateTime Date { get; set; }

        public int CustomerCode { get; set; }

        public int SalesmanCode { get; set; }

        public decimal DiscPerc { get; set; }  //additional discount percentage

        public decimal DiscAmt { get; set; } //additional discount amount

        public decimal RoundOff { get; set; } //round off amount

        public List<SalesOrderItemRequest> Items { get; set; } = new();

        public List<SalesOrderAdditionalRequest> Additional { get; set; } = new();
    }

    public class SalesOrderItemRequest
    {
        public int SlNo { get; set; }

        public int PrCode { get; set; }

        public decimal Qty { get; set; }

        public decimal Rate { get; set; }

        public decimal LDiscPerc { get; set; }

        public decimal LDiscPrice { get; set; }

        public decimal DiscPerc { get; set; }

        public decimal DiscPrice { get; set; }

        public int UnitCode { get; set; }

        public int GdwCode { get; set; }

        public string AlCode { get; set; } = string.Empty;

        public string AlName { get; set; } = string.Empty;

        public int ConvType { get; set; }

        public decimal ConvFact { get; set; }

        public decimal Taxable { get; set; }

        public int TaxCode { get; set; }

        public decimal TaxPerc { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal NetAmount { get; set; }
    }

    public class SalesOrderAdditionalRequest
    {
        public int AccId { get; set; }

        public decimal Amount { get; set; }
    
    }
}
```

## DTOs/Respons-DTO/SelectedSalesOrderResponse.cs

```csharp
﻿
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace QuickBalance.API.DTOs.Respons_DTO
{
    public class SelectedSalesOrderResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public SelectedSalesOrderData Data { get; set; } = new();
    }

    public class SelectedSalesOrderData
    {
        public SalesOrderHeaderDto Header { get; set; } = new();
        public List<SalesOrderItemDto> Items { get; set; } = new();
        public List<AddInfoDto> AdditionalInformations { get; set; } = new();
    }

    // Reuses the properties representing CORDM columns.
        public class SalesOrderHeaderDto: ClsCordM
        {
            //public int ID { get; set; }
            //public string? STYPE { get; set; }
            //public int? ORDNO { get; set; }
            public DateTime? ORDDATE { get; set; }
            public int? INVNO { get; set; }
            public DateTime? INVDATE { get; set; }
            public string? BTYPE { get; set; }
            public int? CUSCODE { get; set; }
            public decimal? DISCP { get; set; }
            public decimal? DISCVAL { get; set; }
            public decimal? NTVALUE { get; set; }
            public decimal? TVALUE { get; set; }
            public decimal? TAX { get; set; }
            public decimal? COINADJ { get; set; }
            public decimal? AMOUNT { get; set; }
            public decimal? FREIGHT { get; set; }
            public int? SMCODE { get; set; }
            public bool? RETURNFLG { get; set; }
            public bool? BILLFLG { get; set; }
            public int? LOGCD { get; set; }
            public int? LOGBILL { get; set; }
            public bool? CLOSED { get; set; }
            public string? REMARKS { get; set; }
            public bool? INCLUSIVE { get; set; }
            public DateTime? DELYDATE { get; set; }
            public decimal? INVAMOUNT { get; set; }
            public decimal? ADVANCE { get; set; }
            public DateTime? CREATEDDT { get; set; }
            public decimal? CUSDUITY { get; set; }
            public decimal? CURCONRATE { get; set; }
            public int? CURCODE { get; set; }
            public string? SHIPADDRESS { get; set; }
            public int? GDNO { get; set; }
            public string? PARTYORDNO { get; set; }
            public bool? CANCELFLG { get; set; }
            public int? INFORMAT { get; set; }
            public int? PRCATCODE { get; set; }
            public string? INVNOTEXT { get; set; }
            public int? VSVRTYPE { get; set; }
            public string? VSDOCNO { get; set; }
            public int? VSORDCD { get; set; }
            public int? PRLISTCODE { get; set; }
            public int? SYC_VRNO { get; set; }
            public int? SYC_ORDNO { get; set; }
            public string? CLOSEREMARKS { get; set; }
            public string? TRANSFERRED { get; set; }

            public string? QTNF1 { get; set; }
            public string? QTNF2 { get; set; }
            public string? QTNF3 { get; set; }
            public string? QTNF4 { get; set; }
            public string? QTNF5 { get; set; }
            public string? QTNF6 { get; set; }
            public string? QTNF7 { get; set; }
            public string? QTNF8 { get; set; }
            public string? QTNF9 { get; set; }
            public string? QTNF10 { get; set; }
            public string? QTNF11 { get; set; }
            public string? QTNF12 { get; set; }
            public string? QTNF13 { get; set; }
            public string? QTNF14 { get; set; }
            public string? QTNF15 { get; set; }
            public string? QTNF16 { get; set; }
            public string? QTNF17 { get; set; }
            public string? QTNF18 { get; set; }
            public string? QTNF19 { get; set; }
            public string? QTNF20 { get; set; }

            public string? SOF1 { get; set; }
            public string? SOF2 { get; set; }
            public string? SOF3 { get; set; }
            public string? SOF4 { get; set; }
            public string? SOF5 { get; set; }
            public string? SOF6 { get; set; }
            public string? SOF7 { get; set; }
            public string? SOF8 { get; set; }
            public string? SOF9 { get; set; }
            public string? SOF10 { get; set; }
            public string? SOF11 { get; set; }
            public string? SOF12 { get; set; }
            public string? SOF13 { get; set; }
            public string? SOF14 { get; set; }
            public string? SOF15 { get; set; }
            public string? SOF16 { get; set; }
            public string? SOF17 { get; set; }
            public string? SOF18 { get; set; }
            public string? SOF19 { get; set; }
            public string? SOF20 { get; set; }

           public int? ULOGCD { get; set; }
            public DateTime? UDATE { get; set; }
            public int? APRVLABY { get; set; }
            public DateTime? APRVLADT { get; set; }
            public int? APRVLBBY { get; set; }
            public DateTime? APRVLBDT { get; set; }
            public string? APRVLST { get; set; }
            public int? APRVLRBY { get; set; }
            public DateTime? APRVLRDT { get; set; }
            public int? APRVLJBY { get; set; }
            public DateTime? APRVLJDT { get; set; }

            // Joined display fields
            public string? SMNAME { get; set; }
            public string? ACCNAME { get; set; }
            public string? SALNO { get; set; }
            public string? USERNAME { get; set; }
    }

    // Reuses the properties representing CORDT columns.
    public class SalesOrderItemDto : ClsCordT
    {
        // CORDT fields
        public int INVNO { get; set; }
        public DateTime? INVDATE { get; set; }
        public string? BTYPE { get; set; }
        public int CUSCODE { get; set; }

        public decimal? QTY { get; set; }
        public decimal? RATE { get; set; }
        public decimal? PRATE { get; set; }
        public decimal? DISC { get; set; }

        public int? TAXCODE { get; set; }
        public decimal? TAX { get; set; }

        public bool? BILLFLG { get; set; }
        public bool? RETURNFLG { get; set; }

        public decimal? MRP { get; set; }
        public decimal? ORGRATE { get; set; }

        public int? SMCODE { get; set; }
        public int? LOGCD { get; set; }

        public string? PRNAME { get; set; }
        public string? ALCODE { get; set; }
        public string? ALNAME { get; set; }

        public int? UNITCODE { get; set; }
        public int? GDWCODE { get; set; }

        public string? PRDESC { get; set; }
        public DateTime? CREATEDDT { get; set; }

        public decimal? CURCONRATE { get; set; }
        public int? CURCODE { get; set; }
        public decimal? ORGQTY { get; set; }

        public bool? CANCELFLG { get; set; }
        public bool? PRFLG { get; set; }

        public int? CONVTYPE { get; set; }
        public decimal? CONVFACT { get; set; }
        public decimal? CONVQTY { get; set; }
        public decimal? CONVRATE { get; set; }

        public string? LDISCTYPE { get; set; }
        public decimal? LDISCPERC { get; set; }
        public decimal? LDISCAMT { get; set; }
        public decimal? DISCP { get; set; }

        public int? QTNCODE { get; set; }

        public decimal? BML { get; set; }
        public decimal? BMQ { get; set; }
        public decimal? BMW { get; set; }

        public int? VSVRTYPE { get; set; }

        public string? PRV { get; set; }
        public bool? CLOSES { get; set; }

        public int? ORDROWID { get; set; }
        public int? QTNROWID { get; set; }

        public string? PRVY { get; set; }

        // PRMAST product details
        public string? ALIAS { get; set; }
        public string? SECNAME { get; set; }
        public string? UNIT { get; set; }
        public string? ALTUNIT { get; set; }
        public string? SALESPRICETYPE { get; set; }
        public string? BMEQN { get; set; }

        // PRUNITMAS unit details
        public string? PRUNITNAME { get; set; }
        public string? UNITSECNAME { get; set; }

        // Internal ordering field; excluded from the API response.
        [JsonIgnore]
        public long SL { get; set; }
}

    public class AddInfoDto
    {
        public string? CODE { get; set; }
        public decimal? TAX { get; set; }
        public decimal? TAXAMT { get; set; }
    }
}

```

## DTOs/PaginationRequest.cs

```csharp
﻿namespace QuickBalance.API.DTOs
{
        public class PaginationRequest
        {
            public string? Type { get; set; } = string.Empty;

            public int CustomerId { get; set; }

            public int OrderId { get; set; }

            public int Page { get; set; } = 1;

            public int PageSize { get; set; } = 20;

            public string? Search { get; set; }

            public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public string? SortBy { get; set; }

        public string? SortDirection { get; set; } = "DESC";

        public void Normalize()
        {
            if (Page < 1)
                Page = 1;

            if (PageSize < 1)
                PageSize = 20;

            if (PageSize > 100)
                PageSize = 100;

            //if (FromDate.HasValue && ToDate.HasValue && FromDate > ToDate)
            //    (FromDate, ToDate) = (ToDate, FromDate);

            if (string.IsNullOrWhiteSpace(SortDirection))
                SortDirection = "DESC";

            SortDirection = SortDirection.ToUpper();

            if (SortDirection != "ASC" && SortDirection != "DESC")
                SortDirection = "DESC";
        }
        public int Offset => (Page - 1) * PageSize;

    }

    public class PaginationRequests
    {

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public string? Search { get; set; }

        //public string? SortBy { get; set; }

        //public string? SortDirection { get; set; } = "DESC";

        public void Normalizes()
        {
            if (Page < 1)
                Page = 1;

            if (PageSize < 1)
                PageSize = 20;

            if (PageSize > 100)
                PageSize = 100;

            //if (FromDate.HasValue && ToDate.HasValue && FromDate > ToDate)
            //    (FromDate, ToDate) = (ToDate, FromDate);

            //SortDirection = SortDirection.ToUpper();

            //if (SortDirection != "ASC" && SortDirection != "DESC")
            //    SortDirection = "DESC";
        }
        //public int Offset => (Page - 1) * PageSize;

    }
}

```

## Other types used by SalesOrderController

### CLS/ClsCordM.cs

```csharp
﻿namespace QuickBalance.API.CLS
{
    public class ClsCordM
    {
        public int OrdNo { get; set; }

        public string? SType { get; set; }
    }
}

```

### CLS/ClsCordT.cs

```csharp
﻿namespace QuickBalance.API.CLS
{
    public class ClsCordT
    {
        public int OrdNo { get; set; }
        public int PrCode { get; set; }

        public string? SType { get; set; }
    }
}

```

### CLS/ClsCordD.cs

```csharp
﻿namespace QuickBalance.API.CLS
{
    public class ClsCordD
    {
        public bool DFlag { get; set; }

        public int VrNo { get; set; }

        public string? VrType { get; set; }
    }
}

```

### DTOs/PaginationResponse.cs

```csharp
﻿namespace QuickBalance.API.DTOs
{
    public class PaginationResponse
    {
        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalRecords { get; set; }

        public int TotalPages { get; set; }
    }

    public class PaginationResponse<T>
    {
        public List<T> Data { get; set; } = new();

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalRecords { get; set; }

        public int TotalPages { get; set; }
    }
}

```

### DB/LngTB.cs

```csharp
﻿namespace QuickBalance.API.DB
{
    public class LngTB
    {
        public static string Vr___Type = "";
        public static string Doc_Type = "";
        public static string Branch = "";
        public static string From_Branch = "";
        public static string To_Branch = "";
        public static string Def__ = "";
        public static string All_Allowed = "";
        public static string Sel___Allowed = "";
        public static string All = "";
        public static string Selected = "";
        public static string Branch_Name = "";
        public static string Type_Name = "";
        public static string Branch_List = "";
        public static string Default_Branch = "";
        public static string Allowed_Branches = "";

        public static string Total = "";
        public static string Sub_Total = "";
        public static string Grand_Total = "";
        public static string Grand__Total = "";
        public static string Opening_Balance = "";
        public static string Closing_Balance = "";
        public static string Trial_Difference = "";
        public static string Trial_Balance_Total = "";

        public static string Cash_Total = "";
        public static string Credit_Total = "";
        public static string Day_Total = "";
        public static string Customer_Total = "";
        public static string Bank_Total = "";
        public static string User_Total = "";
        public static string Supplier_Total = "";
        public static string Group_Total = "";

        public static string Sales_Total = "";
        public static string Sales_Return_Total = "";
        public static string Sales_Sub_Total = "";
        public static string Sales_Return_Sub_Total = "";
        public static string Sub_Total_Sales_Return = "";
        public static string Grand_Total_Sales_Return = "";

        public static string Purchase_Total = "";
        public static string Purchase_Return_Total = "";
        public static string Purchase_Sub_Total = "";
        public static string Purchase_Return_Sub_Total = "";
        public static string Sub_Total_Purchase_Return = "";
        public static string Grand_Total_Purchase_Return = "";

        public static string Net_Profit = "";
        public static string Net_Loss = "";
        public static string Gross_Profit = "";
        public static string Gross_Loss = "";
        public static string Gross_Profit_Bd = "";
        public static string Gross_Loss_Bd = "";

        public static string Adjustment = "";
        public static string Production = "";
        public static string Transfer = "";
        public static string Stock_Transfer = "";
        public static string Stock_Transfer_Receipt = "";
        public static string Stock_Transfer_Issue = "";
        public static string Goods_Receipt_Note = "";
        public static string Purchase = "";
        public static string Pur__Return = "";
        public static string PurchaseReturn = "";
        public static string Purchase_Return = "";

        public static string Goods_Delivery_Note = "";
        public static string Sales = "";
        public static string Sales__Ret = "";
        public static string SalesReturn = "";
        public static string Sales_Return = "";

        public static string Payment = "";
        public static string Receipt = "";
        public static string DrNote = "";
        public static string CrNote = "";
        public static string Journal = "";
        public static string Gen__Payment = "";
        public static string Gen__Receipt = "";

        public static string Opening_Cash = "";
        public static string Opening_Bank = "";
        public static string Closing_Cash = "";
        public static string Closing_Bank = "";

        public static string Opening_Stock = "";
        public static string Closing_Stock = "";

        public static string Delivery_Note_To_Prepare = "";
        public static string Sales_Bills_To_Prepare = "";
        public static string Purchase_Bills_To_Receive = "";
        public static string Receipt_Note_To_Receive = "";

        public static string Description = "";
        public static string Income = "";
        public static string Expenditure = "";

        public static string Phone = "";
        public static string Email = "";

        public static string Customer_Name = "";
        public static string Product_Name = "";
        public static string Category_Name = "";
        public static string Brand_Name = "";
        public static string Salesman_Name = "";

        public static string From = "";
        public static string To = "";
        public static string As_on = "";

        public static string Pending = "";
        public static string Due = "";
        public static string All_Including_Cleared = "";

        public static string Opening = "";
        public static string Cus__Opening = "";
        public static string Sup__Opening = "";

        public static string Customer = "";
        public static string Salesman = "";
        public static string Bill = "";
        public static string Product = "";
        public static string Category = "";
        public static string Brand = "";

        public static string Total_Value = "";
        public static string Grand_Total_Value = "";
        public static string Item_Total = "";

        public static string Transfer_To = "";
        public static string Transfer_From = "";

        public static string ITEM_PRODUCED = "";
        public static string RAW_MATERIAL = "";
        public static string BY_PRODUCTS = "";

        public static string Active = "";
        public static string Inactive = "";
        public static string Activation = "";
        public static string Close = "";

        public static string Cost_of_sales = "";

        public static string Original_Data = "";
        public static string Modified_Data = "";
        public static string Create_Date = "";
        public static string Modified_Date = "";

        public static string Delivery_Note_Type = "";
        public static string Receipt_Voucher_Type = "";
        public static string Receipt_Note_Type = "";
        public static string Payment_Voucher_Type = "";

        public static string Sales_Account = "";
        public static string Purchase_Account = "";
        public static string Sales_Ret___Account = "";
        public static string Purchase_Ret___Account = "";

        public static string Cash_Ledger = "";
        public static string Bank_Ledger = "";

        public static string Price_List = "";
        public static string Item_Name = "";
        public static string Alias = "";
        public static string Stock = "";
        public static string Pur___Price = "";
        public static string Sal___Price = "";
        public static string Price = "";
        public static string Pur___Rate = "";

        public static string Master_Transfer = "";
        public static string Master_Restore = "";
        public static string Master_Request_Restore = "";
        public static string Master_Request = "";

        public static string Printer_Configuration = "";
        public static string Bank = "";
        public static string Receipt_Amount = "";
        public static string Payment_Amount = "";
        public static string Avg_Cost_Price = "";

        public static string Total_Debit = "";
        public static string Total_Credit = "";
        public static string Total_Amount = "";

        public static string Qtn_No__ = "";
        public static string Physical_Stock = "";

        public static string Jan = "";
        public static string Feb = "";
        public static string Mar = "";
        public static string Apr = "";
        public static string May = "";
        public static string Jun = "";

        public static string Jul = "";
        public static string Aug = "";
        public static string Sep = "";
        public static string Oct = "";
        public static string Nov = "";
        public static string Dec = "";

        public static string Refresh_Details = "";
        public static string Invoice_8_Bill_No__ = "";
        public static string Invoice_Value = "";

        public static string Ref___No__ = "";
        public static string Cumulative_Total = "";
        public static string Supp___Ref__ = "";

        public static string Party_Name = "";
        public static string Salesman_Total = "";

        public static string Product_Group = "";

        public static string Sorting = "";
        public static string Searching = "";
        public static string Search_Type = "";
        public static string Search_By = "";
        public static string Exact = "";

        public static string Remove_Group_Total = "";
        public static string Closing_Amount = "";

        public static string DN_Qty = "";
        public static string RN_Qty = "";

        public static string TB_IFRS = "";
        public static string TA_IFRS = "";
        public static string IS_IFRS = "";
        public static string BS_IFRS = "";

        public static string All_Sales_Invoice = "";
        public static string Current_Customer_Sales_Invoice = "";
        public static string Sales_Invoice = "";
        public static string Purchase_Invoice = "";
        public static string Current_Supplier = "";
        public static string All_Purchase_Invoice = "";
        public static string Current_Supplier_Purchase_Invoice = "";
        public static string Current_Customer = "";
        public static string All_Sales = "";
        public static string Current_Customer_Sales = "";
        public static string All_Purchase = "";
        public static string Current_Supplier_Purchase = "";

        public static string Particulars = "";
        public static string Type = "";

        public static string Delivery_Note_Closing = "";
        public static string Supplier_Name = "";
        public static string Created = "";
        public static string Export = "";

        public static string Refresh = "";
        public static string Doc___View = "";
        public static string Doc___Attachment = "";
        public static string Import_File = "";

        public static string Customer_Debit_Note = "";
        public static string Total_Taxable_Amount = "";

        public static string Taxable = "";
        public static string Tax = "";
        public static string Amount = "";

        public static string Qty = "";
        public static string Total_Qty = "";

        public static string Order_and_Invoice = "";
        public static string Invoice = "";

        public static string Approval_Modules = "";
        public static string Include_Unapproved = "";

        public static string Goods_Receipt_Note_Return = "";
        public static string Goods_Delivery_Note_Return = "";
        public static void InitLng()
        {
            Vr___Type = "Vr. Type";
            Doc_Type = "Doc Type";
            Branch = "Branch";
            From_Branch = "From Branch";
            To_Branch = "To Branch";
            Def__ = "Def.";
            All_Allowed = "All Allowed";
            Sel___Allowed = "Sel. Allowed";
            All = "All";
            Selected = "Selected";
            Branch_Name = "Branch Name";
            Type_Name = "Type Name";
            Branch_List = "Branch List";
            Default_Branch = "Default Branch";
            Allowed_Branches = "Allowed Branches";

            Total = "Total";
            Sub_Total = "Sub Total";
            Grand_Total = "Grand Total";
            Grand__Total = "Grand.Total";
            Opening_Balance = "Opening Balance";
            Closing_Balance = "Closing Balance";
            Trial_Difference = "Trial Difference";
            Trial_Balance_Total = "Trial Balance Total";

            Cash_Total = "Cash Total";
            Credit_Total = "Credit Total";
            Day_Total = "Day Total";
            Customer_Total = "Customer Total";
            Bank_Total = "Bank Total";
            User_Total = "User Total";
            Supplier_Total = "Supplier Total";
            Group_Total = "Group Total";

            Sales_Total = "Sales Total";
            Sales_Return_Total = "Sales Return Total";
            Sales_Sub_Total = "Sales Sub Total";
            Sales_Return_Sub_Total = "Sales Return Sub Total";
            Sub_Total_Sales_Return = "Sub Total (Sales - Return)";
            Grand_Total_Sales_Return = "Grand Total (Sales - Return)";

            Purchase_Total = "Purchase Total";
            Purchase_Return_Total = "Purchase Return Total";
            Purchase_Sub_Total = "Purchase Sub Total";
            Purchase_Return_Sub_Total = "Purchase Return Sub Total";
            Sub_Total_Purchase_Return = "Sub Total (Purchase - Return)";
            Grand_Total_Purchase_Return = "Grand Total (Purchase - Return)";

            Net_Profit = "Net Profit";
            Net_Loss = "Net Loss";
            Gross_Profit = "Gross Profit";
            Gross_Loss = "Gross Loss";
            Gross_Profit_Bd = "Gross Profit B/d";
            Gross_Loss_Bd = "Gross Loss B/d";

            Adjustment = "Adjustment";
            Production = "Production";
            Transfer = "Transfer";
            Stock_Transfer = "Stock Transfer";
            Stock_Transfer_Receipt = "Stock Transfer Receipt";
            Stock_Transfer_Issue = "Stock Transfer Issue";
            Goods_Receipt_Note = "Goods Receipt Note";
            Purchase = "Purchase";
            Pur__Return = "Pur.Return";
            PurchaseReturn = "PurchaseReturn";
            Purchase_Return = "Purchase Return";

            Goods_Delivery_Note = "Goods Delivery Note";
            Sales = "Sales";
            Sales__Ret = "Sales.Ret";
            SalesReturn = "SalesReturn";
            Sales_Return = "Sales Return";

            Payment = "Payment";
            Receipt = "Receipt";
            DrNote = "DrNote";
            CrNote = "CrNote";
            Journal = "Journal";
            Gen__Payment = "Gen.Payment";
            Gen__Receipt = "Gen.Receipt";

            Opening_Cash = "Opening Cash";
            Opening_Bank = "Opening Bank";
            Closing_Cash = "Closing Cash";
            Closing_Bank = "Closing Bank";

            Opening_Stock = "Opening Stock";
            Closing_Stock = "Closing Stock";

            Delivery_Note_To_Prepare = "Delivery Note To Prepare";
            Sales_Bills_To_Prepare = "Sales Bills To Prepare";
            Purchase_Bills_To_Receive = "Purchase Bills To Receive";
            Receipt_Note_To_Receive = "Receipt Note To Receive";

            Description = "Description";
            Income = "Income";
            Expenditure = "Expenditure";

            Phone = "Phone";
            Email = "Email";

            Customer_Name = "Customer Name";
            Product_Name = "Product Name";
            Category_Name = "Category Name";
            Brand_Name = "Brand Name";
            Salesman_Name = "Salesman Name";

            From = "From";
            To = "To";
            As_on = "As on";

            Pending = "Pending";
            Due = "Due";
            All_Including_Cleared = "All Including Cleared";

            Opening = "Opening";
            Cus__Opening = "Cus.Opening";
            Sup__Opening = "Sup.Opening";

            Customer = "Customer";
            Salesman = "Salesman";
            Bill = "Bill";
            Product = "Product";
            Category = "Category";
            Brand = "Brand";

            Total_Value = "Total Value";
            Grand_Total_Value = "Grand Total Value";
            Item_Total = "Item Total";

            Transfer_To = "Transfer To";
            Transfer_From = "Transfer From";

            ITEM_PRODUCED = "ITEM PRODUCED";
            RAW_MATERIAL = "RAW MATERIAL";
            BY_PRODUCTS = "BY PRODUCTS";

            Active = "Active";
            Inactive = "Inactive";
            Activation = "Activation";
            Close = "Close";

            Cost_of_sales = "Cost of sales";

            Original_Data = "Original Data";
            Modified_Data = "Modified Data";
            Create_Date = "Create Date";
            Modified_Date = "Modified Date";

            Delivery_Note_Type = "Delivery Note Type";
            Receipt_Voucher_Type = "Receipt Voucher Type";
            Receipt_Note_Type = "Receipt Note Type";
            Payment_Voucher_Type = "Payment Voucher Type";

            Sales_Account = "Sales Account";
            Purchase_Account = "Purchase Account";
            Sales_Ret___Account = "Sales Ret. Account";
            Purchase_Ret___Account = "Purchase Ret. Account";

            Cash_Ledger = "Cash Ledger";
            Bank_Ledger = "Bank Ledger";

            Price_List = "Price List";
            Item_Name = "Item Name";
            Alias = "Alias";
            Stock = "Stock";
            Pur___Price = "Pur. Price";
            Sal___Price = "Sal. Price";
            Price = "Price";
            Pur___Rate = "Pur. Rate";

            Master_Transfer = "Master Transfer";
            Master_Restore = "Master Restore";
            Master_Request_Restore = "Master Request Restore";
            Master_Request = "Master Request";

            Printer_Configuration = "Printer Configuration";
            Bank = "Bank";
            Receipt_Amount = "Receipt Amount";
            Payment_Amount = "Payment Amount";
            Avg_Cost_Price = "Avg Cost Price";

            Total_Debit = "Total Debit";
            Total_Credit = "Total Credit";
            Total_Amount = "Total Amount";

            Qtn_No__ = "Qtn No.";
            Physical_Stock = "Physical Stock";

            Jan = "Jan";
            Feb = "Feb";
            Mar = "Mar";
            Apr = "Apr";
            May = "May";
            Jun = "Jun";

            Jul = "Jul";
            Aug = "Aug";
            Sep = "Sep";
            Oct = "Oct";
            Nov = "Nov";
            Dec = "Dec";

            Refresh_Details = "Refresh Details";
            Invoice_8_Bill_No__ = "Invoice & Bill No.";
            Invoice_Value = "Invoice Value";

            Ref___No__ = "Ref. No.";
            Cumulative_Total = "Cumulative Total";
            Supp___Ref__ = "Supp. Ref.";

            Party_Name = "Party Name";
            Salesman_Total = "Salesman Total";

            Product_Group = "Product Group";

            Sorting = "Sorting";
            Searching = "Searching";
            Search_Type = "Search Type :";
            Search_By = "Search By :";
            Exact = "Exact";

            Remove_Group_Total = "Remove Group Total";
            Closing_Amount = "Closing Amount";

            DN_Qty = "DN Qty";
            RN_Qty = "RN Qty";

            TB_IFRS = "Trial Balance – IFRS";
            TA_IFRS = "Trading A/c – IFRS";
            IS_IFRS = "Income Statement – IFRS";
            BS_IFRS = "Balance Sheet – IFRS";

            All_Sales_Invoice = "All Sales Invoice";
            Current_Customer_Sales_Invoice = "Current Customer Sales Invoice";
            Sales_Invoice = "Sales Invoice";
            Purchase_Invoice = "Purchase Invoice";
            Current_Supplier = "Current Supplier";
            All_Purchase_Invoice = "All Purchase Invoice";
            Current_Supplier_Purchase_Invoice = "Current Supplier Purchase Invoice";
            Current_Customer = "Current Customer";
            All_Sales = "All Sales";
            Current_Customer_Sales = "Current Customer Sales";
            All_Purchase = "All Purchase";
            Current_Supplier_Purchase = "Current Supplier Purchase";

            Particulars = "Particulars";
            Type = "Type";

            Delivery_Note_Closing = "Delivery Note Closing";
            Supplier_Name = "Supplier Name";
            Created = "Created";
            Export = "Export";

            Refresh = "Refresh";
            Doc___View = "Doc. View";
            Doc___Attachment = "Doc. Attachment";
            Import_File = "Import File";

            Customer_Debit_Note = "Customer Debit Note";
            Total_Taxable_Amount = "Total Taxable Amount";

            Taxable = "Taxable";
            Tax = "Tax";
            Amount = "Amount";

            Qty = "Qty";
            Total_Qty = "Total Qty";

            Order_and_Invoice = "Order && Invoice";
            Invoice = "Invoice";

            Approval_Modules = "Approval Modules";
            Include_Unapproved = "Include Unapproved";

            Goods_Receipt_Note_Return = "Goods Receipt Note Return";
            Goods_Delivery_Note_Return = "Goods Delivery Note Return";
        }
        public static string GetLngByNotes(string OrgText, string Notes)
        {            
            return OrgText;
        }
    }
}

```

### Services/CurrentUser.cs

```csharp
﻿using System.Security.Claims;

namespace QuickBalance.API.Services
{
    public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
    {
        private ClaimsPrincipal? Principal =>
        accessor.HttpContext?.User;

        public bool IsAuthenticated =>
            Principal?.Identity?.IsAuthenticated == true;

        public int UserId =>
            GetRequiredInt(ClaimTypes.NameIdentifier);

        public string? UserName =>
            GetClaim(ClaimTypes.Name) ?? GetClaim("name");

        public string? TenantId =>
            GetClaim("tenant_id");

        public int? CompanyId =>
            GetOptionalInt("company_id");

        public int? BranchId =>
            GetOptionalInt("branch_id");

        public string? Role =>
            GetClaim(ClaimTypes.Role) ?? GetClaim("role");

        private string? GetClaim(string claimType) =>
            Principal?.FindFirst(claimType)?.Value;

        private int GetRequiredInt(string claimType)
        {
            if (!IsAuthenticated ||
                !int.TryParse(GetClaim(claimType), out int value) ||
                value <= 0)
            {
                throw new UnauthorizedAccessException(
                    $"Required claim '{claimType}' is missing or invalid.");
            }

            return value;
        }

        private int? GetOptionalInt(string claimType)
        {
            var value = GetClaim(claimType);

            if (string.IsNullOrWhiteSpace(value))
                return null;

            return int.TryParse(value, out int result)
                ? result
                : throw new UnauthorizedAccessException(
                    $"Claim '{claimType}' is invalid.");
        }
    }

    public interface ICurrentUser
    {
        bool IsAuthenticated { get; }

        int UserId { get; }

        string? UserName { get; }

        string? TenantId { get; }

        int? CompanyId { get; }

        int? BranchId { get; }

        string? Role { get; }
    }
}
```

## Search results

```text
./claude-code-prompt-SO1-collect-files.md:27:- every line in the whole project (file path + line number + line text) that contains "ASK_SP_ManageSalesOrderDetails",
./claude-code-prompt-SO1-collect-files.md:28:  "ASK_SP_CancelSalesOrderDetails" or "ASK_SP_PendingSalesOrderInInvoices".
./DB/SalesOrderDB.cs:115:            return await db.ExecuteStoredProcedure("ASK_SP_ManageSalesOrderDetails", parameters);
./DB/SalesOrderDB.cs:125:            return await db.ExecuteStoredProcedure("ASK_SP_CancelSalesOrderDetails", parameters);
./DB/SalesOrderDB.cs:140:            return await db.ExecuteStoredProcedure("ASK_SP_PendingSalesOrderInInvoices", parameters);
./docs/analysis/sales-webapi-map.md:115:| POST `manage` | `ManageSalesOrder` (31) | body `SalesOrderRequest` | `SalesOrderDB.ManageSalesOrder` (SalesOrderDB.cs:94) | SP `ASK_SP_ManageSalesOrderDetails` (SalesOrderDB.cs:115); tables inside SP = Run 2 | 400 if body null / no items / `CustomerCode==0` (70-77); reads `MESSAGE`,`STATUS` from first row (84-85); STATUS=1 -> 201 if message=="Saved" else 200; else 400; empty result -> 500 |
./docs/analysis/sales-webapi-map.md:118:| POST `cancel/{id:int}` | `CancelSalesOrder` (438) | route id | `SalesOrderDB.CancelSalesOrder` (480; SalesOrderDB.cs:118) | SP `ASK_SP_CancelSalesOrderDetails` | STATUS=1 -> 200, else 400 (486-489) |
./docs/analysis/sales-webapi-map.md:119:| GET `pending-list` | (501) | `PaginationRequest` (has CustomerId, OrderId, SortBy, SortDirection) | `SalesOrderDB.PendingSalesOrderInInvoices` (542; SalesOrderDB.cs:128) | SP `ASK_SP_PendingSalesOrderInInvoices` | 200; if `OrderId==0` returns list + pagination, else item list without pagination (563-581) |
./docs/analysis/sales-webapi-map.md:159:- **Create vs update** is decided by the document number in the request: `SordNo` / `SinvNo` / `SretNo` equal to 0 means create, non-zero means update. Evidence in the SPs: Order `IF (@SORDNO = 0)` (SO/3-ASK_SP_ManageSalesOrderDetails.sql:118), `IF (@SORDNO <> 0)` (90); Invoice `IF(@SINVNO = 0)` (SI/4-...sql:299), `IF(@SINVNO <> 0)` (256); Return `IF(@SRETNO = 0)` (SR/4-...sql:266), `IF(@SRETNO <> 0)` (237). The C# code only forwards the number. The log note is `'Create '` vs `'Update'` (SO:222, SI:428, SR:395).
./docs/analysis/sales-webapi-map.md:179:**SP call** (SalesOrderDB.cs:94-116): `ASK_SP_ManageSalesOrderDetails` with `@SORDNO int`, `@DATE datetime`, `@CUSCODE int`, `@SMCODE int`, `@DISCPERC dec(18,6)`, `@DISCAMT dec(18,6)`, `@ROUNDOFF dec(18,6)`, `@USERID int` (from `Variables.UserId`), `@ITEMDTLS` structured `dbo.ASK_UDT_SalesOrderItem`, `@ADDIDTLS` structured `dbo.ASK_UDT_SalesOrderAdditionalInfo`. The SP's declared signature has not been compared (Run 2).
./docs/analysis/sql/ASK_SP_CancelSalesOrderDetails.sql:6:CREATE PROCEDURE [dbo].[ASK_SP_CancelSalesOrderDetails]
./docs/migration/Sales/00-scope.md:106:| **DB script** | None. No procedure or view selects from them | `ASK_SP_CancelSalesInvoiceDetails` (5148, 5157, 5165), `ASK_SP_CancelSalesOrderDetails` (5220) and `ASK_SP_CancelSalesReturnDetails` (5295, 5305) **delete** from them. Only the unused `ManageSales_Salt` (8280-8334) inserts |
./docs/migration/Sales/00-scope.md:147:**Decision:** `ASK_SP_ManageSalesInvoiceDetails` and `ASK_SP_ManageSalesOrderDetails` are the API implementation, but they must be **proven correct, not assumed**. Phase 2/3 verify them **line by line** against the legacy save chains (`01-file-map.md` §2.7 and §5.4): every table write, column value, order of writes, numbering, stock rows (including `STKDTPHY`, D4), receipts/advance, bill-by-bill and approval. Each difference goes into the gap table as a defect in the SP or as an accepted deviation. The cancel SPs (`ASK_SP_Cancel*`) get the same treatment against §2.8, §4.6 and §5.5. The new `ASK_SP_ManageSalesReturnDetails` (W2) is designed from §4.5.
./docs/migration/Sales/01-file-map-OLD.md:138:| `ASK_SP_ManageSalesOrderDetails` | 6117–6368 | No | **Yes** – `DB/SalesOrderDB.cs:115` |
./docs/migration/Sales/01-file-map-OLD.md:139:| `ASK_SP_CancelSalesOrderDetails` | 5196–5247 | No | **Yes** – `DB/SalesOrderDB.cs:125` |
./docs/migration/Sales/01-file-map-OLD.md:140:| `ASK_SP_PendingSalesOrderInInvoices` | 6447–6537 | No | **Yes** – `DB/SalesOrderDB.cs:140` |
./docs/migration/Sales/01-file-map.md:410:| `ASK_SP_PendingSalesOrderInInvoices` | 6447–6590 | No | Yes: `DB/SalesOrderDB.cs:140` | |
./docs/migration/Sales/01-file-map.md:412:| `ASK_SP_ManageSalesOrderDetails` / `ASK_SP_CancelSalesOrderDetails` | 6117 / 5196 | No | Yes: `DB/SalesOrderDB.cs` | |
./docs/migration/Sales/api-review.md:26:| `ASK_SP_ManageSalesOrderDetails` | Yes | No | `POST sales-orders/manage` |
./docs/migration/Sales/api-review.md:27:| `ASK_SP_CancelSalesOrderDetails` | Yes | No | `POST sales-orders/cancel/{id}` |
./docs/migration/Sales/api-review.md:28:| `ASK_SP_PendingSalesOrderInInvoices` | Yes | No | `GET sales-orders/pending-list` |
./docs/migration/Sales/api-review.md:46:| POST | `manage` | `ASK_SP_ManageSalesOrderDetails` | 201 when SP message is `"Saved"`, else 200; 400 when SP `STATUS != 1` |
./docs/migration/Sales/api-review.md:49:| POST | `cancel/{id:int}` | `ASK_SP_CancelSalesOrderDetails` | |
./docs/migration/Sales/api-review.md:50:| GET | `pending-list` | `ASK_SP_PendingSalesOrderInInvoices` | `pagination` only when `orderId == 0` |
./docs/SALES-SQL/SO/4-ASK_SP_CancelSalesOrderDetails.sql:6:CREATE PROCEDURE [dbo].[ASK_SP_CancelSalesOrderDetails]
./docs/SalesOrder_Controller_Report.md:77:   - Runs stored procedure **`ASK_SP_ManageSalesOrderDetails`** with `@SORDNO, @DATE, @CUSCODE, @SMCODE, @DISCPERC, @DISCAMT, @ROUNDOFF` (decimals 18,6), `@USERID`, `@ITEMDTLS` (TVP `dbo.ASK_UDT_SalesOrderItem`) and `@ADDIDTLS` (TVP `dbo.ASK_UDT_SalesOrderAdditionalInfo`), through `DBConn.ExecuteStoredProcedure`.
./docs/SalesOrder_Controller_Report.md:85:**Tables touched:** *needs confirmation.* Everything happens inside `ASK_SP_ManageSalesOrderDetails`, most likely `CORDM`, `CORDT` and `CORDD`.
./docs/SalesOrder_Controller_Report.md:126:3. `SalesOrderDB.CancelSalesOrder(id, UserId)` runs SP **`ASK_SP_CancelSalesOrderDetails`** with `@SORDNO`, `@USERID`.
./docs/SalesOrder_Controller_Report.md:137:3. `SalesOrderDB.PendingSalesOrderInInvoices(request)` runs SP **`ASK_SP_PendingSalesOrderInInvoices`** with:
./docs/SalesOrder_Controller_Report.md:165:| `DB/SalesOrderDB.cs` | `ManageSalesOrder` | SP `ASK_SP_ManageSalesOrderDetails` + 2 TVPs |
./docs/SalesOrder_Controller_Report.md:166:| `DB/SalesOrderDB.cs` | `CancelSalesOrder` | SP `ASK_SP_CancelSalesOrderDetails` |
./docs/SalesOrder_Controller_Report.md:167:| `DB/SalesOrderDB.cs` | `PendingSalesOrderInInvoices` | SP `ASK_SP_PendingSalesOrderInInvoices` |
./docs/SalesOrder_Controller_Report.md:209:- The bodies of `ASK_SP_ManageSalesOrderDetails`, `ASK_SP_CancelSalesOrderDetails` and `ASK_SP_PendingSalesOrderInInvoices`: tables written, transaction handling, numbering, and every possible `STATUS` / `MESSAGE`.
./SalesControllers_Report.md:29:| POST | `manage` | `ManageSalesOrder` | Body `SalesOrderRequest` | SP `ASK_SP_ManageSalesOrderDetails` (TVPs `ASK_UDT_SalesOrderItem`, `ASK_UDT_SalesOrderAdditionalInfo`) | 201 if SP message = `"Saved"`, else 200; 400 if SP `STATUS != 1` |
./SalesControllers_Report.md:32:| POST | `cancel/{id:int}` | `CancelSalesOrder` | Route `id` | SP `ASK_SP_CancelSalesOrderDetails` | 200 / 400 per SP status |
./SalesControllers_Report.md:33:| GET | `pending-list` | `ListPendingSalesOrderInInvoices` | Query `PaginationRequest` (`customerId`, `orderId`, `page`, `pageSize`, `search`, `sortBy`, `sortDirection`) | SP `ASK_SP_PendingSalesOrderInInvoices` | 200; includes `pagination` only when `orderId == 0` (header list), otherwise item list without pagination |
```
