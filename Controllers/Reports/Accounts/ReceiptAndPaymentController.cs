using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using QuickBalance.API.CLS;
using QuickBalance.API.Configuration;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using System.Data;
using System.Security.Claims;

namespace QuickBalance.API.Controllers.Reports.Accounts
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]

    [Route("api/v1/reports/receipt-and-payment")]
    [Authorize]
    public class ReceiptAndPaymentController : ControllerBase
    {
        private readonly DatabaseConnection _databaseConnection;
        public ReceiptAndPaymentController(DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }

        [HttpPost]
        public async Task<IActionResult> GetReceiptAndPayment(ReceiptAndPaymentRequest request)
        {
            try
            {
                LngTB.InitLng();
                Variable Variables = new Variable();
                Variables.VoucherTypeCodes = request.VoucherTypeCodes;

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
                
                AccMasDB accMasDB = new AccMasDB(DBConn);
                accMasDB.Variables = Variables;
                accMasDB.Functions.Variables = Variables;
                ClsAccMas clsAccMas = new ClsAccMas();

                DayBookDB dayBookDB = new DayBookDB(DBConn);
                dayBookDB.Variables = Variables;
                dayBookDB.Functions.Variables = Variables;

                //-- Get Report

                DataTable dtP = new DataTable();
                dtP.Columns.Add("grpcode");
                dtP.Columns.Add("AccName");
                dtP.Columns.Add("Debit");
                dtP.Columns.Add("Credit");
                DataTable dtgrp = dtP.Clone();
                DataTable dt = new DataTable();

                if (request.FromDate.Date > Variables.fyStartDate.Date)
                    await dayBookDB.CreateTable(Variables.fyStartDate.ToString(Variables.format), request.FromDate.AddDays(-1).ToString(Variables.format), "OP", Unapproved: request.IncludeUnapproved);
                else
                    await dayBookDB.CreateTable(Variables.fyStartDate.AddDays(-1).ToString(Variables.format), request.FromDate.AddDays(-1).ToString(Variables.format), "OP", Unapproved: request.IncludeUnapproved);

                await dayBookDB.CreateTable(request.FromDate.ToString(Variables.format), request.ToDate.ToString(Variables.format), "DB", Unapproved: request.IncludeUnapproved);

                #region CASH_Opening
                decimal CashOpenBalTotCr = 0; decimal CashOpenBalTotDr = 0;
                DataTable dt_CashOpdet = dtP.Clone();
                decimal CashClosingBalTotCr = 0; decimal CashClosingBalTotDr = 0;
                DataTable dt_CashCdet = dtP.Clone();
                clsAccMas.LedgerGroupId = 4;
                DataTable dt_Cash = await accMasDB.AccountsByGroupRpt(clsAccMas, request.IncludeUnapproved);
                if (dt_Cash.Rows.Count > 0)
                {
                    foreach (DataRow dtc in dt_Cash.Rows)
                    {
                        decimal CashOpenBal = -1 * await accMasDB.Opening(Convert.ToInt32(dtc["ID"]));
                        CashOpenBal += -1 * await dayBookDB.CashLedgerBalance(Convert.ToInt32(dtc["ID"]));
                        if (CashOpenBal > 0)
                        {
                            dt_CashOpdet.Rows.Add(-1, dtc["ACCNAME"], CashOpenBal.ToString(Variables.TwoDecString), 0.ToString(Variables.TwoDecString));
                            CashOpenBalTotCr += CashOpenBal;
                        }
                        else
                        {
                            dt_CashOpdet.Rows.Add(-1, dtc["ACCNAME"], 0.ToString(Variables.TwoDecString), (-1 * CashOpenBal).ToString(Variables.TwoDecString));
                            CashOpenBalTotDr += CashOpenBal;
                        }

                        decimal CashClosingBal = CashOpenBal;
                        CashClosingBal += -1 * await dayBookDB.CashLedgerClosingBalance(Convert.ToInt32(dtc["ID"]));
                        if (CashClosingBal > 0)
                        {
                            dt_CashCdet.Rows.Add(9000, dtc["ACCNAME"], CashClosingBal.ToString(Variables.TwoDecString), 0.ToString(Variables.TwoDecString));
                            CashClosingBalTotCr += CashClosingBal;
                        }
                        else
                        {
                            dt_CashCdet.Rows.Add(9000, dtc["ACCNAME"], 0.ToString(Variables.TwoDecString), (-1 * CashClosingBal).ToString(Variables.TwoDecString));
                            CashClosingBalTotDr += CashClosingBal;
                        }
                    }
                }

                dtgrp.Rows.Add(-1, LngTB.Opening_Cash, CashOpenBalTotCr.ToString(Variables.TwoDecString), (-1 * CashOpenBalTotDr).ToString(Variables.TwoDecString));

                if (request.Alphabetical)
                    dtP.Rows.Add(-1, LngTB.Opening_Cash, CashOpenBalTotCr.ToString(Variables.TwoDecString), (-1 * CashOpenBalTotDr).ToString(Variables.TwoDecString));

                if (dt_CashOpdet != null)
                    dtP.Merge(dt_CashOpdet);
                #endregion CASH_Opening

                #region BANK_Opening
                decimal BankOpenBalTotCr = 0; decimal BankOpenBalTotDr = 0;
                DataTable dt_bnkOpdet = dtP.Clone();
                decimal BankClosingBalTotCr = 0; decimal BankClosingBalTotDr = 0;
                DataTable dt_bnkCdet = dtP.Clone();
                clsAccMas.LedgerGroupId = 1;
                DataTable dt_bnk = await accMasDB.AccountsByGroupRpt(clsAccMas, request.IncludeUnapproved);
                if (dt_bnk.Rows.Count > 0)
                {
                    foreach (DataRow dtr in dt_bnk.Rows)
                    {
                        decimal bnk_OpenBal = -1 * await dayBookDB.LedgerBalance(Convert.ToInt32(dtr["ID"]));
                        bnk_OpenBal += -1 * Convert.ToDecimal(dtr["OPENBAL"]);
                        if (bnk_OpenBal > 0)
                        {
                            dt_bnkOpdet.Rows.Add(0, dtr["ACCNAME"], bnk_OpenBal.ToString(Variables.TwoDecString), 0.ToString(Variables.TwoDecString));
                            BankOpenBalTotCr += bnk_OpenBal;
                        }
                        else
                        {
                            dt_bnkOpdet.Rows.Add(0, dtr["ACCNAME"], 0.ToString(Variables.TwoDecString), (-1 * bnk_OpenBal).ToString(Variables.TwoDecString));
                            BankOpenBalTotDr += bnk_OpenBal;
                        }

                        decimal bnk_ClsBal = bnk_OpenBal;
                        bnk_ClsBal += -1 * await dayBookDB.LedgerClosingBalance(Convert.ToInt32(dtr["ID"]));
                        if (bnk_ClsBal > 0)
                        {
                            dt_bnkCdet.Rows.Add(9001, dtr["ACCNAME"], bnk_ClsBal.ToString(Variables.TwoDecString), 0.ToString(Variables.TwoDecString));
                            BankClosingBalTotCr += bnk_ClsBal;
                        }
                        else
                        {
                            dt_bnkCdet.Rows.Add(9001, dtr["ACCNAME"], 0.ToString(Variables.TwoDecString), (-1 * bnk_ClsBal).ToString(Variables.TwoDecString));
                            BankClosingBalTotDr += bnk_ClsBal;
                        }
                    }
                }

                dtgrp.Rows.Add(0, LngTB.Opening_Bank, BankOpenBalTotCr.ToString(Variables.TwoDecString), (-1 * BankOpenBalTotDr).ToString(Variables.TwoDecString));

                if (request.Alphabetical)
                    dtP.Rows.Add(0, LngTB.Opening_Bank, BankOpenBalTotCr.ToString(Variables.TwoDecString), (-1 * BankOpenBalTotDr).ToString(Variables.TwoDecString));

                if (dt_bnkOpdet != null)
                    dtP.Merge(dt_bnkOpdet);
                #endregion BANK_Opening

                #region DETAILS
                dayBookDB.tableRecPay = "";
                for (int l = 1; l <= 2; l++)
                {
                    clsAccMas.LedgerGroupId = l == 1 ? 4 : 1;
                    DataTable dtls = await accMasDB.AccountsByGroupRpt(clsAccMas, request.IncludeUnapproved);
                    foreach (DataRow dr in dtls.Rows)
                    {
                        if (Convert.ToInt32(dr["ID"]) == Variables.CashCD)
                            await dayBookDB.CashLedgerForRecPayRpt(Convert.ToInt32(dr["ID"]));
                        else
                            await dayBookDB.LedgerForRecPayRpt(Convert.ToInt32(dr["ID"]));
                    }
                }

                DataTable OtherGroup = new DataTable();
                DataTable SubDetails = new DataTable();

                SubDetails = await dayBookDB.GetRecPayRpt();

                for (int m = 0; m < SubDetails.Rows.Count; m++)
                {
                    decimal Credit = Convert.ToDecimal(SubDetails.Rows[m]["Credit"].ToString().Trim() == "" ? "0" : SubDetails.Rows[m]["Credit"].ToString());
                    decimal Debit = Convert.ToDecimal(SubDetails.Rows[m]["Debit"].ToString().Trim() == "" ? "0" : SubDetails.Rows[m]["Debit"].ToString());

                    decimal DebitAmnt = Debit;
                    decimal CreditAmnt = Credit;

                    SubDetails.Rows[m]["Debit"] = DebitAmnt.ToString(Variables.DecString);
                    SubDetails.Rows[m]["Credit"] = CreditAmnt.ToString(Variables.DecString);
                }

                if (request.Alphabetical)
                {
                    dtP.Rows.Add();
                    if (SubDetails.Rows.Count > 0)
                    {
                        dtP.Merge(SubDetails, true, MissingSchemaAction.Ignore);
                        dtP.Rows.Add();
                    }
                }
                else
                {
                    OtherGroup = await dayBookDB.GetRecPayGrp();
                    foreach (DataRow item in OtherGroup.Rows)
                    {
                        string GrpCode = item[0].ToString();
                        string GrpName = item[1].ToString();

                        decimal Credit = Convert.ToDecimal(item["Credit"].ToString().Trim() == "" ? "0" : item["Credit"].ToString());
                        decimal Debit = Convert.ToDecimal(item["Debit"].ToString().Trim() == "" ? "0" : item["Debit"].ToString());

                        decimal DebitAmnt = Debit;
                        decimal CreditAmnt = Credit;

                        DataRow[] drSub = SubDetails.Select("GrpCode='" + GrpCode + "'");
                        if (drSub.Length > 0)
                        {
                            DataTable dts = drSub.CopyToDataTable();
                            dtP.Merge(dts, true, MissingSchemaAction.Ignore);
                        }

                        dtgrp.Rows.Add(GrpCode, GrpName, DebitAmnt.ToString(Variables.TwoDecString), CreditAmnt.ToString(Variables.TwoDecString));
                    }
                }
                #endregion DETAILS

                #region CASH_Closing
                dtgrp.Rows.Add(9000, LngTB.Closing_Cash, CashClosingBalTotCr.ToString(Variables.TwoDecString), (-1 * CashClosingBalTotDr).ToString(Variables.TwoDecString));

                if (request.Alphabetical)
                    dtP.Rows.Add(9000, LngTB.Closing_Cash, CashClosingBalTotCr.ToString(Variables.TwoDecString), (-1 * CashClosingBalTotDr).ToString(Variables.TwoDecString));

                if (dt_CashCdet != null)
                    dtP.Merge(dt_CashCdet);
                #endregion CASH_Closing

                #region BANK_Closing
                dtgrp.Rows.Add(9001, LngTB.Closing_Bank, BankClosingBalTotCr.ToString(Variables.TwoDecString), (-1 * BankClosingBalTotDr).ToString(Variables.TwoDecString));

                if (request.Alphabetical)
                    dtP.Rows.Add(9001, LngTB.Closing_Bank, BankClosingBalTotCr.ToString(Variables.TwoDecString), (-1 * BankClosingBalTotDr).ToString(Variables.TwoDecString));

                if (dt_bnkCdet != null)
                    dtP.Merge(dt_bnkCdet);
                #endregion BANK_Closing

                DataTable dtMain = new DataTable();
                dtMain.Columns.Add("grpcode");
                dtMain.Columns.Add("btn");
                dtMain.Columns.Add("AccName");
                dtMain.Columns.Add("Debit");
                dtMain.Columns.Add("Credit");

                if (request.Scheduled)
                {
                    dtMain.Merge(dtgrp, true, MissingSchemaAction.Ignore);
                    foreach (DataRow dr in dtMain.Rows)
                        dr[1] = request.Detailed ? "-" : "+";

                    if (request.Detailed)
                    {
                        string GrpId = "";
                        DataTable DtSub = dtMain.Clone();
                        foreach (DataRow dr in dtMain.Rows)
                        {
                            DtSub.Rows.Add(dr[0], dr[1], dr[2], dr[3], dr[4]);
                            GrpId = dr[0].ToString();

                            DataRow[] drSub = dtP.Select("grpcode='" + GrpId + "'");
                            if (drSub.Length > 0)
                            {
                                DataTable dtl = drSub.CopyToDataTable();
                                DtSub.Merge(dtl, true, MissingSchemaAction.Ignore);
                            }
                        }
                        dtMain = DtSub;
                    }
                }
                else
                {
                    dtMain.Merge(dtP, true, MissingSchemaAction.Ignore);
                }

                await dayBookDB.DropTable();

                //--

                var tableData = dtMain.AsEnumerable().Select(row => dtMain.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).ToList();
                var tableData2 = dtP.AsEnumerable().Select(row => dtP.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).ToList();
                return Ok(new { status = true, data = tableData, fullData = tableData2, });
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