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

    [Route("api/v1/reports/cash-book")]
    [Authorize]
    public class CashBookController : ControllerBase
    {
        private readonly DatabaseConnection _databaseConnection;
        public CashBookController(DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }

        [HttpPost]
        public async Task<IActionResult> GetCashBook(CashBookRequest request, [FromQuery] PaginationRequest pagination)
        {
            try
            {
                LngTB.InitLng();
                Variable Variables = new Variable();
                Variables.VoucherTypeCodes = request.VoucherTypeCodes;

                //--
                    // ====================================================
                    // PAGINATION
                    // ====================================================

                    pagination ??= new PaginationRequest();
                    pagination.Normalize();


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

                AccMasDB accMasDB = new AccMasDB(DBConn);
                accMasDB.Variables = Variables; 
                accMasDB.Functions.Variables = Variables;

                DayBookDB dayBookDB = new DayBookDB(DBConn);
                dayBookDB.Variables = Variables;
                dayBookDB.Functions.Variables = Variables;

                //-- Get Report

                DataTable dt = new DataTable();
                dt.Columns.Add("VRDATE");
                dt.Columns.Add("INVTEXT");
                dt.Columns.Add("RECID");
                dt.Columns.Add("RECPARTICULARS");
                dt.Columns.Add("RECTYPE");
                dt.Columns.Add("RECCASH");
                dt.Columns.Add("PAYCASH");
                dt.Columns.Add("TYPE");
                dt.Columns.Add("REMARKS");

                int cashLedgerId = request.SelectedCashLedger == 0 ? Variables.CashCD : request.SelectedCashLedger;

                decimal OpenBal = -1 * await accMasDB.Opening(cashLedgerId);
                if (request.FromDate > Variables.fyStartDate)
                    OpenBal += await dayBookDB.SelectedCashOpening(request.FromDate.AddDays(-1).ToString(Variables.format), cashLedgerId, true, true, request.IncludeUnapproved);

                await dayBookDB.CreateTable(request.FromDate.ToString(Variables.format), request.ToDate.ToString(Variables.format), "DB", VatSummary: request.Summary, Unapproved: request.IncludeUnapproved);

                DataTable dtR = await dayBookDB.SelectedCashBookDates(cashLedgerId);                

                bool LineWiseDate = request.LineWiseDate;
                if (dtR.Rows.Count > 0)
                {
                    decimal DayOpen = 0;
                    int index = 0;
                    foreach (DataRow dr in dtR.Rows)
                    {
                        if (!LineWiseDate)
                            dt.Rows.Add(null, null, null, Convert.ToDateTime(dr["VRDATE"]).ToString(Variables.format), null, null, null, 'B');

                        DayOpen = await dayBookDB.SelectedCashOpenBalByDate(Convert.ToDateTime(dr["VRDATE"]), cashLedgerId);
                        OpenBal += DayOpen;

                        dt.Rows.Add(Convert.ToDateTime(dr["VRDATE"]).ToString(Variables.format), null, null, LngTB.Opening_Balance, null, OpenBal.ToString(Variables.DecString), null, 'B');
                        dt.Merge(await dayBookDB.SelectedCashTransactionsOnDate(Convert.ToDateTime(dr["VRDATE"]), cashLedgerId, request.Summary), true, MissingSchemaAction.Ignore);

                        decimal DayClosing = OpenBal;
                        DataTable dtTot = await dayBookDB.SelectedCashDayTotal(Convert.ToDateTime(dr["VRDATE"]).Date, cashLedgerId);
                        if (dtTot.Rows.Count > 0)
                        {
                            DataRow drTot = dtTot.Rows[0];
                            if (!LineWiseDate)
                            {
                                dt.Rows.Add(null, null, null, null, null, null, null, null);
                                if (OpenBal > 0)
                                {
                                    if (drTot["RECCASH"] != DBNull.Value && drTot["PAYCASH"] != DBNull.Value)
                                        dt.Rows.Add(Convert.ToDateTime(dr["VRDATE"]).ToString(Variables.format), null, null, LngTB.Total, null, (OpenBal + Convert.ToDecimal(drTot["RECCASH"]) + Convert.ToDecimal(drTot["RECAMT"])).ToString(Variables.DecString), (Convert.ToDecimal(drTot["PAYCASH"]) + Convert.ToDecimal(drTot["PAYAMT"])).ToString(Variables.DecString), 'B');
                                    else
                                        dt.Rows.Add(Convert.ToDateTime(dr["VRDATE"]).ToString(Variables.format), null, null, LngTB.Total, null, OpenBal.ToString(Variables.DecString), "0.00", 'B');
                                }
                                else
                                {
                                    if (drTot["RECCASH"] != DBNull.Value && drTot["PAYCASH"] != DBNull.Value)
                                        dt.Rows.Add(Convert.ToDateTime(dr["VRDATE"]).ToString(Variables.format), null, null, LngTB.Total, null, (Convert.ToDecimal(drTot["RECCASH"]) + Convert.ToDecimal(drTot["RECAMT"])).ToString(Variables.DecString), ((-1 * OpenBal) + Convert.ToDecimal(drTot["PAYCASH"]) + Convert.ToDecimal(drTot["PAYAMT"])).ToString(Variables.DecString), 'B');
                                    else
                                        dt.Rows.Add(Convert.ToDateTime(dr["VRDATE"]).ToString(Variables.format), null, null, LngTB.Total, null, "0.00", (-1 * OpenBal).ToString(Variables.DecString), 'B');
                                }
                            }

                            decimal TotRec = 0, TotPay = 0;
                            if (drTot["RECCASH"] != DBNull.Value)
                                TotRec = Convert.ToDecimal(drTot["RECCASH"]) + Convert.ToDecimal(drTot["RECAMT"]);
                            if (drTot["PAYCASH"] != DBNull.Value)
                                TotPay = Convert.ToDecimal(drTot["PAYCASH"]) + Convert.ToDecimal(drTot["PAYAMT"]);
                            DayClosing = DayClosing + TotRec - TotPay;
                        }

                        OpenBal -= DayOpen;

                        if (LineWiseDate)
                        {
                            if (index == dtR.Rows.Count - 1)
                            {
                                dt.Rows.Add(null, null, null, null, null, null, null);
                                dt.Rows.Add(null, null, null, LngTB.Closing_Balance, null, null, (DayClosing).ToString(Variables.DecString), 'B');
                            }
                        }
                        else
                        {
                            dt.Rows.Add(null, null, null, null, null, null, null);
                            dt.Rows.Add(Convert.ToDateTime(dr["VRDATE"]).ToString(Variables.format), null, null, LngTB.Closing_Balance, null, null, (DayClosing).ToString(Variables.DecString), 'B');
                            dt.Rows.Add();
                        }

                        index++;
                    }
                }
                else
                {
                    if (LineWiseDate)
                        dt.Rows.Add(null, null, null, LngTB.Closing_Balance, null, OpenBal.ToString(Variables.DecString), null, 'B');
                    else
                        dt.Rows.Add(request.FromDate.ToString(Variables.format), null, null, LngTB.Closing_Balance, null, OpenBal.ToString(Variables.DecString), null, 'B');
                }
                await dayBookDB.DropTable();

                //--    
                int totalCount = dt.Rows.Count;

                var tableData = dt.AsEnumerable()
                    .Skip(pagination.Offset)
                    .Take(pagination.PageSize)
                    .Select(row => new
                    {
                        vrdate = row.IsNull("VRDATE") ? null : Convert.ToString(row["VRDATE"]),
                        invtext = row.IsNull("INVTEXT") ? null : Convert.ToString(row["INVTEXT"]),
                        recid = row.IsNull("RECID") ? null : Convert.ToString(row["RECID"]),
                        recparticulars = row.IsNull("RECPARTICULARS") ? null : Convert.ToString(row["RECPARTICULARS"]),
                        rectype = row.IsNull("RECTYPE") ? null : Convert.ToString(row["RECTYPE"]),
                        reccash = row.IsNull("RECCASH") ? null : Convert.ToString(row["RECCASH"]),
                        paycash = row.IsNull("PAYCASH") ? null : Convert.ToString(row["PAYCASH"]),
                        type = row.IsNull("TYPE") ? null : Convert.ToString(row["TYPE"]),
                        remarks = row.IsNull("REMARKS") ? null : Convert.ToString(row["REMARKS"])
                    })
                    .ToList();

                int totalPages = (int)Math.Ceiling(
                    totalCount / (double)pagination.PageSize
                );

                return Ok(new
                {
                    status = true,
                    message = "Cash book retrieved successfully.",
                    data = tableData,
                    pagination = new
                    {
                        page = pagination.Page,
                        pageSize = pagination.PageSize,
                        totalRecords = totalCount,
                        //totalCount,
                        totalPages,
                        //hasPreviousPage = pagination.Page > 1,
                        //hasNextPage = pagination.Page < totalPages
                    }
                });

                //var tableData = dt.AsEnumerable().Select(row => dt.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).ToList();
                // convert datatable rows to lowercase json properties
                //var tableData = dt.AsEnumerable()
                //    .Select(row => new
                //    {
                //        vrdate = row.IsNull("VRDATE") ? null : Convert.ToString(row["VRDATE"]),
                //        invtext = row.IsNull("INVTEXT") ? null : Convert.ToString(row["INVTEXT"]),
                //        recid = row.IsNull("RECID") ? null : Convert.ToString(row["RECID"]),
                //        recparticulars = row.IsNull("RECPARTICULARS") ? null : Convert.ToString(row["RECPARTICULARS"]),
                //        rectype = row.IsNull("RECTYPE") ? null : Convert.ToString(row["RECTYPE"]),
                //        reccash = row.IsNull("RECCASH") ? null : Convert.ToString(row["RECCASH"]),
                //        paycash = row.IsNull("PAYCASH") ? null : Convert.ToString(row["PAYCASH"]),
                //        type = row.IsNull("TYPE") ? null : Convert.ToString(row["TYPE"]),
                //        remarks = row.IsNull("REMARKS") ? null : Convert.ToString(row["REMARKS"])
                //    })
                //    .ToList();

                //return Ok(new
                //{
                //    status = true,
                //    message = "cash book retrieved successfully.",
                //    data = tableData
                //});
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