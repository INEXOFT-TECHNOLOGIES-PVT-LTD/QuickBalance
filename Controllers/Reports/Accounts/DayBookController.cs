using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using QuickBalance.API.Configuration;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using System.Data;
using System.Security.Claims;

namespace QuickBalance.API.Controllers.Reports.Accounts
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]

    [Route("api/v1/reports/day-book")]
    [Authorize]
    public class DayBookController : ControllerBase
    {

        private readonly DatabaseConnection _databaseConnection;
        public DayBookController(DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }

        [HttpPost]
        //public async Task<IActionResult> GetDayBook(DayBookRequest request)
        public async Task<IActionResult> GetDayBook([FromBody] DayBookRequest request,[FromQuery] PaginationRequest pagination)
        {
            try
            {
                LngTB.InitLng();
                Variable Variables = new Variable();
                Variables.VoucherTypeCodes = request.VoucherTypeCodes;

                //--
                pagination ??= new PaginationRequest();
                pagination.Normalize();

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { status = false, message = "User ID not found in token.", errors = new List<object>() });
                Variables.UserId = int.Parse(userIdClaim.Value);

                //--

                string connectionString = _databaseConnection.GetConnectionString(Variables);
                //DBConn db = new DBConn(connectionString);
                //DatabaseConnection databaseConnection = new DatabaseConnection();
                //string connectionString = databaseConnection.GetConnectionString(Variables);
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

                DayBookDB dayBookDB = new DayBookDB(DBConn);
                dayBookDB.Variables = Variables;
                dayBookDB.Functions.Variables = Variables;

                //-- Get Report

                DataTable dt = new DataTable();
                dt.Columns.Add("VRDATE");
                dt.Columns.Add("RECID");
                dt.Columns.Add("RECPARTICULARS");
                dt.Columns.Add("RECTYPE");
                dt.Columns.Add("RECCASH");
                dt.Columns.Add("RECAMT");
                dt.Columns.Add("PAYID");
                dt.Columns.Add("PAYPARTICULARS");
                dt.Columns.Add("PAYTYPE");
                dt.Columns.Add("PAYCASH");
                dt.Columns.Add("PAYAMT");
                dt.Columns.Add("TYPE");

                decimal TotOpenBAl = await accMasDB.Opening(Variables.CashCD);
                TotOpenBAl = -1 * TotOpenBAl;

                TotOpenBAl += await dayBookDB.Opening(request.FromDate.AddDays(-1).ToString(Variables.format), request.IncludeUnapproved);
                await dayBookDB.CreateTable(request.FromDate.ToString(Variables.format), request.ToDate.ToString(Variables.format), "DB", VatSummary: request.Summary, Unapproved: request.IncludeUnapproved);

                DataTable dtDate = await dayBookDB.Dates();
                if (dtDate.Rows.Count > 0)
                {
                    foreach (DataRow dr in dtDate.Rows)
                    {
                        decimal openBal = TotOpenBAl + await dayBookDB.Balance(Convert.ToDateTime(dr["VRDATE"]).Date);
                        openBal = Math.Round(openBal, Variables.DecimalPoint);

                        dt.Rows.Add(Convert.ToDateTime(dr["VRDATE"]).Date, null, Convert.ToDateTime(dr["VRDATE"]).ToString(Variables.format), null, null, null, null, Convert.ToDateTime(dr["VRDATE"]).ToString(Variables.format), null, null, null, 'B');
                        dt.Rows.Add(Convert.ToDateTime(dr["VRDATE"]).Date, null, LngTB.Opening_Balance, null, openBal.ToString(Variables.DecString), null, null, null, null, null, null, 'B');

                        DataTable dtCr = await dayBookDB.CreditTransactionsOnDate(Convert.ToDateTime(dr["VRDATE"]), request.Summary);
                        DataTable dtDr = await dayBookDB.DebitTransactionsOnDate(Convert.ToDateTime(dr["VRDATE"]), request.Summary);
                        int ri = dt.Rows.Count - 1;
                        if (dtCr.Rows.Count > 0)
                        {
                            dt.Merge(dtCr, true, MissingSchemaAction.Ignore);
                            ri++;
                        }
                        else
                        {
                            dt.Rows.Add();
                            ri++;
                        }
                        foreach (DataRow drDr in dtDr.Rows)
                        {
                            if (ri >= dt.Rows.Count)
                            {
                                dt.Rows.Add();
                            }
                            dt.Rows[ri]["PAYID"] = drDr["PAYID"];
                            dt.Rows[ri]["PAYPARTICULARS"] = drDr["PAYPARTICULARS"];
                            dt.Rows[ri]["PAYTYPE"] = drDr["PAYTYPE"];
                            dt.Rows[ri]["PAYCASH"] = drDr["PAYCASH"];
                            dt.Rows[ri]["PAYAMT"] = drDr["PAYAMT"];
                            dt.Rows[ri]["TYPE"] = "";
                            ri++;
                        }
                        decimal DayClosing = openBal;
                        DataTable dtTot = await dayBookDB.DayTotal(Convert.ToDateTime(dr["VRDATE"]).Date);
                        if (dtTot.Rows.Count > 0)
                        {
                            dt.Rows.Add(null, null, null, null, null, null, null, null, null, null, null, null);
                            DataRow drTot = dtTot.Rows[0];
                            if (string.IsNullOrEmpty(drTot["RECCASH"].ToString()))
                                drTot["RECCASH"] = 0;
                            if (string.IsNullOrEmpty(drTot["RECAMT"].ToString()))
                                drTot["RECAMT"] = 0;
                            if (string.IsNullOrEmpty(drTot["PAYCASH"].ToString()))
                                drTot["PAYCASH"] = 0;
                            if (string.IsNullOrEmpty(drTot["PAYAMT"].ToString()))
                                drTot["PAYAMT"] = 0;

                            if (openBal > 0)
                                dt.Rows.Add(Convert.ToDateTime(dr["VRDATE"]).Date, null, LngTB.Total, null, (openBal + Convert.ToDecimal(drTot["RECCASH"])).ToString(Variables.DecString), Convert.ToDecimal(drTot["RECAMT"]).ToString(Variables.DecString), null, LngTB.Total, null, Convert.ToDecimal(drTot["PAYCASH"]).ToString(Variables.DecString), Convert.ToDecimal(drTot["PAYAMT"]).ToString(Variables.DecString), 'B');
                            else
                                dt.Rows.Add(Convert.ToDateTime(dr["VRDATE"]).Date, null, LngTB.Total, null, Convert.ToDecimal(drTot["RECCASH"]).ToString(Variables.DecString), Convert.ToDecimal(drTot["RECAMT"]).ToString(Variables.DecString), null, LngTB.Total, null, ((-1 * openBal) + Convert.ToDecimal(drTot["PAYCASH"])).ToString(Variables.DecString), Convert.ToDecimal(drTot["PAYAMT"]).ToString(Variables.DecString), 'B');
                            DayClosing = DayClosing + Convert.ToDecimal(drTot["RECCASH"]) - Convert.ToDecimal(drTot["PAYCASH"]);
                        }

                        dt.Rows.Add(null, null, null, null, null, null, null, null, null, null, null, null);
                        dt.Rows.Add(Convert.ToDateTime(dr["VRDATE"]).Date, null, null, null, null, null, null, LngTB.Closing_Balance, null, (DayClosing).ToString(Variables.DecString), null, 'B');
                        dt.Rows.Add(null, null, null, null, null, null, null, null, null, null, null);
                    }
                }
                else
                {
                    dt.Rows.Add(request.FromDate.Date, null, LngTB.Opening_Balance, null, TotOpenBAl.ToString(Variables.DecString), null, null, null, null, null, null, 'B');
                }
                await dayBookDB.DropTable();

                //-----//

                //var tableData = dt.AsEnumerable().Select(row => dt.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).ToList();
                //var tableData = dt.AsEnumerable()
                //.Select(row => new
                //{
                //    vrdate = row.IsNull("VRDATE") ? null : Convert.ToString(row["VRDATE"]),
                //    recid = row.IsNull("RECID") ? null : Convert.ToString(row["RECID"]),
                //    recparticulars = row.IsNull("RECPARTICULARS") ? null : Convert.ToString(row["RECPARTICULARS"]),
                //    rectype = row.IsNull("RECTYPE") ? null : Convert.ToString(row["RECTYPE"]),
                //    reccash = row.IsNull("RECCASH") ? null : Convert.ToString(row["RECCASH"]),
                //    recamt = row.IsNull("RECAMT") ? null : Convert.ToString(row["RECAMT"]),
                //    payid = row.IsNull("PAYID") ? null : Convert.ToString(row["PAYID"]),
                //    payparticulars = row.IsNull("PAYPARTICULARS") ? null : Convert.ToString(row["PAYPARTICULARS"]),
                //    paytype = row.IsNull("PAYTYPE") ? null : Convert.ToString(row["PAYTYPE"]),
                //    paycash = row.IsNull("PAYCASH") ? null : Convert.ToString(row["PAYCASH"]),
                //    payamt = row.IsNull("PAYAMT") ? null : Convert.ToString(row["PAYAMT"]),
                //    type = row.IsNull("TYPE") ? null : Convert.ToString(row["TYPE"])
                //})
                //.ToList();

                //return Ok(new
                //{
                //    status = true,
                //    data = tableData
                //});
                // Total report rows before pagination
                int totalCount = dt.Rows.Count;

                int totalPages = (int)Math.Ceiling(
                    totalCount / (double)pagination.PageSize
                );

                // Apply pagination to the completed report
                var tableData = dt.AsEnumerable()
                    .Skip(pagination.Offset)
                    .Take(pagination.PageSize)
                    .Select(row => new
                    {
                        vrdate = row.IsNull("VRDATE")
                            ? null
                            : Convert.ToString(row["VRDATE"]),

                        recid = row.IsNull("RECID")
                            ? null
                            : Convert.ToString(row["RECID"]),

                        recparticulars = row.IsNull("RECPARTICULARS")
                            ? null
                            : Convert.ToString(row["RECPARTICULARS"]),

                        rectype = row.IsNull("RECTYPE")
                            ? null
                            : Convert.ToString(row["RECTYPE"]),

                        reccash = row.IsNull("RECCASH")
                            ? null
                            : Convert.ToString(row["RECCASH"]),

                        recamt = row.IsNull("RECAMT")
                            ? null
                            : Convert.ToString(row["RECAMT"]),

                        payid = row.IsNull("PAYID")
                            ? null
                            : Convert.ToString(row["PAYID"]),

                        payparticulars = row.IsNull("PAYPARTICULARS")
                            ? null
                            : Convert.ToString(row["PAYPARTICULARS"]),

                        paytype = row.IsNull("PAYTYPE")
                            ? null
                            : Convert.ToString(row["PAYTYPE"]),

                        paycash = row.IsNull("PAYCASH")
                            ? null
                            : Convert.ToString(row["PAYCASH"]),

                        payamt = row.IsNull("PAYAMT")
                            ? null
                            : Convert.ToString(row["PAYAMT"]),

                        type = row.IsNull("TYPE")
                            ? null
                            : Convert.ToString(row["TYPE"])
                    })
                    .ToList();

                return Ok(new
                {
                    status = true,
                    message = "Day book retrieved successfully.",
                    data = tableData,
                    pagination = new
                    {
                        page = pagination.Page,
                        pageSize = pagination.PageSize,
                        totalRecords=totalCount,
                        totalPages,
                        //hasPreviousPage = pagination.Page > 1,
                        //hasNextPage = pagination.Page < totalPages
                    }
                });
                //return Ok(new { status = true, data = tableData });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message, errors = new List<object>() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message, errors = new List<object>() });
            }
        }
    }
}
