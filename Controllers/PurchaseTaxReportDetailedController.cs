using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using QuickBalance.API.CLS;
using QuickBalance.API.Configuration;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using System.Data;
using System.Security.Claims;

namespace QuickBalance.API.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]

[Route("api/[controller]")]
[Authorize]
public class PurchaseTaxReportDetailedController : ControllerBase
{
    private readonly DatabaseConnection _databaseConnection;

    public  PurchaseTaxReportDetailedController (DatabaseConnection databaseConnection)
    {
        _databaseConnection = databaseConnection;
    }

    [HttpPost]
    public async Task<IActionResult> GetPurchaseTaxReportDetailed(PurchaseTaxReportDetailedRequest request)
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
            //DBConn db = new DBConn(connectionString);
            DBConn DBConn = new DBConn(connectionString);
            CommonFunction commonFunction = new CommonFunction(DBConn);

            if (!await commonFunction.CheckDatabaseConnection())
                return StatusCode(500, new { status = "error", message = "Database connection failed." });

            //--


            await commonFunction.SetVariableValues(Variables);

            commonFunction.Variables = Variables;
            commonFunction.Functions.Variables = Variables;

            PurTDB purTDB = new PurTDB(DBConn);
            purTDB.Variables = Variables;
            purTDB.Functions.Variables = Variables;

            ClsPurT clsPurT = new ClsPurT();
            ClsDate clsDate = new ClsDate();

            clsDate.DateFrom = request.FromDate.ToString(Variables.format);
            clsDate.ToDate = request.ToDate.ToString(Variables.format);

            clsPurT.VrType = "P";
            clsPurT.CancelFlg = false;
            clsPurT.OrdNo = request.OrdNo;

            await purTDB.CreateForVATReport(
                clsPurT,
                clsDate,
                request.IncludeUnapproved
            );

            DataTable dt = request.OrdNo > 0
                ? await purTDB.VATReportDetailedByVrNo(request.OrdNo)
                : await purTDB.VATReportDetailed();

            await purTDB.DropTb();

            var data = dt.AsEnumerable()
                .Select(row => dt.Columns.Cast<DataColumn>()
                    .ToDictionary(
                        column => column.ColumnName,
                        column => row[column] == DBNull.Value ? null : row[column]
                    ))
                .ToList();

            return Ok(new
            {
                status = "success",
                data
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