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

    [Route("api/v1/reports/month-wise-profit-and-loss")]
    [Authorize]
    public class MonthWiseProfitAndLossController : ControllerBase
    {

        private readonly DatabaseConnection _databaseConnection;
        public MonthWiseProfitAndLossController(DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }
        [HttpPost]
        public async Task<IActionResult> GetMonthWiseProfitAndLoss(MonthWiseProfitAndLossRequest request)
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

                string connectionString = _databaseConnection.GetConnectionString(Variables);
                DBConn db = new DBConn(connectionString);
                //string connectionString = databaseConnection.GetConnectionString(Variables);
                DBConn DBConn = new DBConn(connectionString);
                CommonFunction commonFunction = new CommonFunction(DBConn);

                if (!await commonFunction.CheckDatabaseConnection())
                    return StatusCode(500, new { status = false, message = "Database connection failed.", errors = new List<object>() });

                //--

                await commonFunction.SetVariableValues(Variables);
                commonFunction.Variables = Variables;
                commonFunction.Functions.Variables = Variables;

                DayBookDB dayBookDB = new DayBookDB(DBConn);
                dayBookDB.Variables = Variables;
                dayBookDB.Functions.Variables = Variables;

                PurmDB purmDB = new PurmDB(DBConn);
                purmDB.Variables = Variables;
                purmDB.Functions.Variables = Variables;

                TaxMasDBbkp taxMasDB = new TaxMasDBbkp(DBConn);
                taxMasDB.Variables = Variables;
                taxMasDB.Functions.Variables = Variables;

                PrMastDB prMastDB = new PrMastDB(DBConn);
                prMastDB.Variables = Variables;
                prMastDB.Functions.Variables = Variables;

                PrGrpMasDB prGrpMasDB = new PrGrpMasDB(DBConn);
                prGrpMasDB.Variables = Variables;
                prGrpMasDB.Functions.Variables = Variables;

                StkDtDB stkDtDB = new StkDtDB(DBConn);
                stkDtDB.Variables = Variables;
                stkDtDB.Functions.Variables = Variables;

                AccMasDB accMasDB = new AccMasDB(DBConn);
                accMasDB.Variables = Variables;
                accMasDB.Functions.Variables = Variables;

                //-- Get Report

                var report = new MonthWiseProfitAndLossContext();

                report.Variables = Variables;
                report.Functions = commonFunction.Functions;

                report.dayBookDB = dayBookDB;
                report.purmDB = purmDB;
                report.taxMasDB = taxMasDB;
                report.prMastDB = prMastDB;
                report.prGrpMasDB = prGrpMasDB;
                report.stkDtDB = stkDtDB;
                report.accMasDB = accMasDB;

                DataTable dtP = report.dtP;
                dtP.Columns.Add("DebitGroup");
                dtP.Columns.Add("Debit");
                dtP.Columns.Add("CreditGroup");
                dtP.Columns.Add("Credit");
                dtP.Columns.Add("ID");
                dtP.Columns.Add("Type");
                dtP.Columns.Add("RepType");
                dtP.Columns.Add("MonthName");
                dtP.Columns.Add("RowNum", typeof(int));

                DataTable dtTCr = report.dtTCr;
                dtTCr.Columns.Add("ID");
                dtTCr.Columns.Add("Description");

                DataTable dtTDr = report.dtTDr;
                dtTDr.Columns.Add("ID");
                dtTDr.Columns.Add("Description");

                DataTable dtTGPL = report.dtTGPL;
                dtTGPL.Columns.Add("ID");
                dtTGPL.Columns.Add("Description");

                DataTable dtTTot = report.dtTTot;
                dtTTot.Columns.Add("Description");

                DataTable dtGPLB = report.dtGPLB;
                dtGPLB.Columns.Add("ID");
                dtGPLB.Columns.Add("Description");

                DataTable dtPNPL = report.dtPNPL;
                dtPNPL.Columns.Add("ID");
                dtPNPL.Columns.Add("Description");

                DataTable dtPCr = report.dtPCr;
                dtPCr.Columns.Add("ID");
                dtPCr.Columns.Add("Description");

                DataTable dtPDr = report.dtPDr;
                dtPDr.Columns.Add("ID");
                dtPDr.Columns.Add("Description");

                DataTable dtPTot = report.dtPTot;
                dtPTot.Columns.Add("Description");

                DateTime StartDate = new DateTime(Variables.fyStartDate.Year, request.FromMonth, 1);
                if (StartDate < Variables.fyStartDate)
                    StartDate = StartDate.AddYears(1);

                DateTime EndDate = new DateTime(Variables.fyEndDate.Year, request.ToMonth, 1).AddMonths(1).AddDays(-1);
                if (EndDate > Variables.fyEndDate)
                    EndDate = EndDate.AddYears(-1);

                if (StartDate > EndDate)
                    return BadRequest(new { status = false, message = "Invalid Period", errors = new List<object>() });

                int i = 0;
                DataTable dt = dtP.Clone();
                while (StartDate < EndDate)
                {
                    report.months[i] = StartDate.ToString("MMMM");
                    string CrCol = StartDate.ToString("MMMM") + "_Cr";
                    string DrCol = StartDate.ToString("MMMM") + "_Dr";
                    if (dtTCr.Columns.Contains(CrCol) == false)
                    {
                        dtTCr.Columns.Add(CrCol);
                        dtTCr.Columns.Add(DrCol);
                        dtTDr.Columns.Add(CrCol);
                        dtTDr.Columns.Add(DrCol);
                        dtTGPL.Columns.Add(CrCol);
                        dtTGPL.Columns.Add(DrCol);
                        dtTTot.Columns.Add(CrCol);
                        dtTTot.Columns.Add(DrCol);

                        dtGPLB.Columns.Add(CrCol);
                        dtGPLB.Columns.Add(DrCol);
                        dtPCr.Columns.Add(CrCol);
                        dtPCr.Columns.Add(DrCol);
                        dtPDr.Columns.Add(CrCol);
                        dtPDr.Columns.Add(DrCol);
                        dtPNPL.Columns.Add(CrCol);
                        dtPNPL.Columns.Add(DrCol);
                        dtPTot.Columns.Add(CrCol);
                        dtPTot.Columns.Add(DrCol);
                    }

                    await NewReport("T", StartDate, StartDate.AddMonths(1).AddDays(-1), request, report);

                    /*<Trading Acc Credit Side>*/
                    DataRow[] drTCr = dtP.Select(" (RepType IS NULL OR ( RepType<>'GP' AND RepType<>'GL' AND RepType<>'TT') ) AND Credit IS NOT NULL ", "RowNum ASC");
                    if (drTCr.Count() > 0)
                    {
                        foreach (DataRow dr in drTCr)
                        {
                            string desc = dr["CreditGroup"].ToString();
                            DataRow[] drCr = dtTCr.Select("Description='" + desc + "' AND (ID IS NULL OR ID='0' OR ID='" + dr["ID"] + "')");
                            if (drCr.Count() > 0)
                            {
                                drCr[0][CrCol] = dr["Credit"];
                                drCr[0]["ID"] = dr["ID"];
                            }
                            else
                            {
                                dtTCr.Rows.Add();
                                dtTCr.Rows[dtTCr.Rows.Count - 1]["Description"] = desc;
                                dtTCr.Rows[dtTCr.Rows.Count - 1][CrCol] = dr["Credit"];
                                dtTCr.Rows[dtTCr.Rows.Count - 1]["ID"] = dr["ID"];
                            }
                        }
                    }
                    /*<Trading Acc Credit Side>*/

                    /*<Trading Acc Debit Side>*/
                    DataRow[] drTDr = dtP.Select(" (RepType IS NULL OR ( RepType<>'GP' AND RepType<>'GL' AND RepType<>'TT') ) AND Debit IS NOT NULL ", "RowNum ASC");
                    if (drTDr.Count() > 0)
                    {

                        foreach (DataRow dr in drTDr)
                        {
                            string desc = dr["DebitGroup"].ToString();
                            DataRow[] drDr = dtTDr.Select("Description='" + desc + "' AND (ID IS NULL OR ID='0' OR ID='" + dr["ID"] + "')");
                            if (drDr.Count() > 0)
                            {
                                drDr[0][DrCol] = dr["Debit"];
                                drDr[0]["ID"] = dr["ID"];
                            }
                            else
                            {
                                dtTDr.Rows.Add();
                                dtTDr.Rows[dtTDr.Rows.Count - 1]["Description"] = desc;
                                dtTDr.Rows[dtTDr.Rows.Count - 1][DrCol] = dr["Debit"];
                                dtTDr.Rows[dtTDr.Rows.Count - 1]["ID"] = dr["ID"];
                            }
                        }
                    }
                    /*<Trading Acc Debit Side>*/

                    /*<Trading Acc Total>*/

                    //Gross Profit
                    DataRow[] drTGP = dtP.Select("RepType='GP'", "RowNum ASC");
                    if (drTGP.Count() > 0)
                    {
                        foreach (DataRow dr in drTGP)
                        {
                            string desc = dr["DebitGroup"].ToString();
                            DataRow[] drGp = dtTGPL.Select("Description='" + desc + "'");
                            if (drGp.Count() > 0)
                            {
                                drGp[0][DrCol] = dr["Debit"];

                            }
                            else
                            {
                                dtTGPL.Rows.Add();
                                dtTGPL.Rows[dtTGPL.Rows.Count - 1]["Description"] = desc;
                                dtTGPL.Rows[dtTGPL.Rows.Count - 1][DrCol] = dr["Debit"];
                            }
                        }
                    }
                    //Gross Profit

                    //Gross Loss
                    DataRow[] drTGL = dtP.Select("RepType='GL'", "RowNum ASC");
                    if (drTGL.Count() > 0)
                    {
                        foreach (DataRow dr in drTGL)
                        {
                            string desc = dr["DebitGroup"].ToString();
                            DataRow[] drGp = dtTGPL.Select("Description='" + desc + "'");
                            if (drGp.Count() > 0)
                            {
                                drGp[0][CrCol] = dr["Credit"];
                            }
                            else
                            {
                                dtTGPL.Rows.Add();
                                dtTGPL.Rows[dtTGPL.Rows.Count - 1]["Description"] = desc;
                                dtTGPL.Rows[dtTGPL.Rows.Count - 1][CrCol] = dr["Credit"];
                            }
                        }
                    }
                    //Gross Loss

                    //Total
                    DataRow[] drTTot = dtP.Select("RepType='TT'", "RowNum ASC");
                    if (drTTot.Count() > 0)
                    {
                        string desc = drTTot[0]["DebitGroup"].ToString();
                        DataRow[] drTT = dtTTot.Select("Description='" + desc + "'");
                        if (drTT.Count() > 0)
                        {
                            drTT[0][CrCol] = drTTot[0]["Credit"];
                            drTT[0][DrCol] = drTTot[0]["Debit"];
                        }
                        else
                        {
                            dtTTot.Rows.Add();
                            dtTTot.Rows[dtTTot.Rows.Count - 1]["Description"] = desc;
                            dtTTot.Rows[dtTTot.Rows.Count - 1][CrCol] = drTTot[0]["Credit"];
                            dtTTot.Rows[dtTTot.Rows.Count - 1][DrCol] = drTTot[0]["Debit"];
                        }
                    }
                    //Total

                    /*</Trading Acc Total>*/
                    dtP.Rows.Clear();

                    ///////////////////////////////////------------------P&L--------------------///////////////////////////////////

                    await NewReport("P", StartDate, StartDate.AddMonths(1).AddDays(-1), request, report);

                    /*<PL Acc Credit Side>*/
                    DataRow[] drPCr = dtP.Select(" (RepType IS NULL OR ( RepType<>'NP' AND RepType<>'NL' AND RepType<>'PT') ) AND Credit IS NOT NULL ", "RowNum ASC");
                    if (drPCr.Count() > 0)
                    {
                        foreach (DataRow dr in drPCr)
                        {
                            string desc = dr["CreditGroup"].ToString();
                            DataRow[] drCr = dtPCr.Select("Description='" + desc + "' AND (ID IS NULL OR ID='0' OR ID='" + dr["ID"] + "')");
                            if (drCr.Count() > 0)
                            {
                                drCr[0][CrCol] = dr["Credit"];
                                drCr[0]["ID"] = dr["ID"];
                            }
                            else
                            {
                                dtPCr.Rows.Add();
                                dtPCr.Rows[dtPCr.Rows.Count - 1]["Description"] = desc;
                                dtPCr.Rows[dtPCr.Rows.Count - 1][CrCol] = dr["Credit"];
                                dtPCr.Rows[dtPCr.Rows.Count - 1]["ID"] = dr["ID"];
                            }
                        }
                    }
                    /*<PL Acc Credit Side>*/

                    /*<PL Acc Debit Side>*/
                    DataRow[] drPDr = dtP.Select(" (RepType IS NULL OR ( RepType<>'NP' AND RepType<>'NL' AND RepType<>'PT') ) AND Debit IS NOT NULL ", "RowNum ASC");
                    if (drPDr.Count() > 0)
                    {
                        foreach (DataRow dr in drPDr)
                        {
                            string desc = dr["DebitGroup"].ToString();
                            DataRow[] drDr = dtPDr.Select("Description='" + desc + "' AND (ID IS NULL OR ID='0' OR ID='" + dr["ID"] + "')");
                            if (drDr.Count() > 0)
                            {
                                drDr[0][DrCol] = dr["Debit"];
                                drDr[0]["ID"] = dr["ID"];
                            }
                            else
                            {
                                dtPDr.Rows.Add();
                                dtPDr.Rows[dtPDr.Rows.Count - 1]["Description"] = desc;
                                dtPDr.Rows[dtPDr.Rows.Count - 1][DrCol] = dr["Debit"];
                                dtPDr.Rows[dtPDr.Rows.Count - 1]["ID"] = dr["ID"];
                            }
                        }
                    }
                    /*<PL Acc Debit Side>*/

                    /*<PL Acc Total>*/

                    //Net Profit
                    DataRow[] drPNP = dtP.Select("RepType='NP'", "RowNum ASC");
                    if (drPNP.Count() > 0)
                    {
                        foreach (DataRow dr in drPNP)
                        {
                            string desc = dr["DebitGroup"].ToString();
                            DataRow[] drNp = dtPNPL.Select("Description='" + desc + "'");
                            if (drNp.Count() > 0)
                            {
                                drNp[0][DrCol] = dr["Debit"];

                            }
                            else
                            {
                                dtPNPL.Rows.Add();
                                dtPNPL.Rows[dtPNPL.Rows.Count - 1]["Description"] = desc;
                                dtPNPL.Rows[dtPNPL.Rows.Count - 1][DrCol] = dr["Debit"];
                            }
                        }
                    }
                    //Net Profit

                    //Net Loss
                    DataRow[] drPNL = dtP.Select("RepType='NL'", "RowNum ASC");
                    if (drPNL.Count() > 0)
                    {
                        foreach (DataRow dr in drPNL)
                        {
                            string desc = dr["DebitGroup"].ToString();
                            DataRow[] drNp = dtPNPL.Select("Description='" + desc + "'");
                            if (drNp.Count() > 0)
                            {
                                drNp[0][CrCol] = dr["Credit"];
                            }
                            else
                            {
                                dtPNPL.Rows.Add();
                                dtPNPL.Rows[dtPNPL.Rows.Count - 1]["Description"] = desc;
                                dtPNPL.Rows[dtPNPL.Rows.Count - 1][CrCol] = dr["Credit"];
                            }
                        }
                    }
                    //Net Loss

                    //Total
                    DataRow[] drPTot = dtP.Select("RepType='PT'", "RowNum ASC");
                    if (drPTot.Count() > 0)
                    {
                        string desc = drPTot[0]["DebitGroup"].ToString();
                        DataRow[] drPT = dtPTot.Select("Description='" + desc + "'");
                        if (drPT.Count() > 0)
                        {
                            drPT[0][CrCol] = drPTot[0]["Credit"];
                            drPT[0][DrCol] = drPTot[0]["Debit"];
                        }
                        else
                        {
                            dtPTot.Rows.Add();
                            dtPTot.Rows[dtPTot.Rows.Count - 1]["Description"] = desc;
                            dtPTot.Rows[dtPTot.Rows.Count - 1][CrCol] = drPTot[0]["Credit"];
                            dtPTot.Rows[dtPTot.Rows.Count - 1][DrCol] = drPTot[0]["Debit"];
                        }
                    }
                    //Total

                    /*</PL Acc Total>*/
                    dtP.Select().ToList<DataRow>().ForEach(r => r["MonthName"] = StartDate.Month.ToString());
                    dt.Merge(dtP, true, MissingSchemaAction.Ignore);
                    dtP.Rows.Clear();
                    StartDate = StartDate.AddMonths(1);
                    i++;
                }

                DataTable dtRep = dtTTot.Clone();
                dtRep.Merge(dtTCr, true, MissingSchemaAction.Ignore);
                dtRep.Merge(dtTDr, true, MissingSchemaAction.Ignore);
                dtRep.Rows.Add();
                dtRep.Merge(dtTGPL, true, MissingSchemaAction.Ignore);
                dtRep.Merge(dtTTot, true, MissingSchemaAction.Ignore);
                dtRep.Rows.Add();
                dtRep.Merge(dtPCr, true, MissingSchemaAction.Ignore);
                dtRep.Merge(dtPDr, true, MissingSchemaAction.Ignore);
                dtRep.Rows.Add();
                dtRep.Merge(dtPNPL, true, MissingSchemaAction.Ignore);
                dtRep.Merge(dtPTot, true, MissingSchemaAction.Ignore);

                //--

                var tableData = dtRep.AsEnumerable().Select(row => dtRep.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).ToList();
                return Ok(new { status = true, data = tableData });
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

        private async Task NewReport(string FOrder, DateTime dtp_from, DateTime dtp_to, MonthWiseProfitAndLossRequest request, MonthWiseProfitAndLossContext report)
        {
            try
            {
                DataTable dt = report.dt;
                DataTable dtP = report.dtP;

                Variable Variables = report.Variables;
                Function Functions = report.Functions;

                ClsDate clsDate = report.clsDate;
                ClsTaxMas clsTaxMas = report.clsTaxMas;
                ClsPrMast clsPrMast = report.clsPrMast;
                ClsPrGrpMas clsPrGrpMas = report.clsPrGrpMas;
                ClsStkDt clsStkDt = report.clsStkDt;
                ClsAccMas clsAccMas = report.clsAccMas;

                DayBookDB dayBookDB = report.dayBookDB;
                PurmDB purmDB = report.purmDB;
                TaxMasDBbkp taxMasDB = report.taxMasDB;
                PrMastDB prMastDB = report.prMastDB;
                PrGrpMasDB prGrpMasDB = report.prGrpMasDB;
                StkDtDB stkDtDB = report.stkDtDB;
                AccMasDB accMasDB = report.accMasDB;

                int li = 0, ri = 0;
                if (dtP.Rows.Count > 0)
                    li = ri = dtP.Rows.Count - 1;

                string opTableName = ""; string tablename = "";

                if (FOrder != "P")
                {
                    dtP.Rows.Clear();
                    li = ri = 0;
                }
                else
                {
                    dtP.Rows.Add();
                    li = ri = dtP.Rows.Count - 1;
                }

                decimal Credit = 0, Debit = 0, Balance = 0;
                decimal PrevProf = 0;

                clsDate.DateFrom = Variables.fyStartDate.ToString(Variables.format);
                clsDate.ToDate = dtp_to.Date.ToString(Variables.format);

                if (dtp_from.Date > Variables.fyStartDate.Date)
                    await dayBookDB.CreateTable(Variables.fyStartDate.ToString(Variables.format), dtp_from.Date.AddDays(-1).ToString(Variables.format), "OP", Unapproved: request.IncludeUnapproved);
                else
                    await dayBookDB.CreateTable(Variables.fyStartDate.AddDays(-1).ToString(Variables.format), dtp_from.Date.AddDays(-1).ToString(Variables.format), "OP", Unapproved: request.IncludeUnapproved);

                await dayBookDB.CreateTable(dtp_from.ToString(Variables.format), dtp_to.ToString(Variables.format), "DB", Unapproved: request.IncludeUnapproved);

                clsTaxMas.Code = Variables.VATCode;
                DataTable dtTax = await taxMasDB.SearchByID(clsTaxMas);
                if (dtTax.Rows.Count > 0)
                {
                    DataRow dr = dtTax.Rows[0];
                    await dayBookDB.InsertRow(dtp_from.Date, Convert.ToInt32(dr["PURCODE"]));
                    await dayBookDB.InsertRow(dtp_from.Date, Convert.ToInt32(dr["SALCODE"]));
                }

                tablename = dayBookDB.tableName;
                opTableName = dayBookDB.OpTable;

                decimal OpenStock = 0;
                if (dtp_from.Date > Variables.fyStartDate)
                    OpenStock = await StockValue(Variables.fyStartDate, dtp_from.Date.AddDays(-1), request, report);
                else
                    OpenStock = await StockValue(dtp_from.Date.AddDays(-1), dtp_from.Date.AddDays(-1), request, report);


                if (FOrder == "T" || FOrder == "B")
                {
                    if (OpenStock >= 0)
                    {
                        li++;
                        dtP.Rows.Add(LngTB.Opening_Stock, OpenStock.ToString(Variables.DecString), null, null, null, "", "", null, dtP.Rows.Count);
                        Debit += OpenStock;
                    }
                    else
                    {
                        ri++;
                        dtP.Rows.Add(LngTB.Opening_Stock, null, LngTB.Opening_Stock, (-1 * OpenStock).ToString(Variables.DecString), null, "", "", null, dtP.Rows.Count);
                        Credit += (-1 * OpenStock);
                    }
                }
                clsDate.DateFrom = Variables.fyStartDate.ToString(Variables.format);
                clsDate.ToDate = dtp_to.Date.ToString(Variables.format);

                /************ STOCK CALCULATION ************/
                await prMastDB.CreateForClosingStock(clsDate, false, Unapproved: request.IncludeUnapproved);
                decimal CStk = 0;

                DataTable dts = await prMastDB.ClosingStockValueLedgerWise();

                await prMastDB.CreateForCStkFIFO(clsDate, Unapproved: request.IncludeUnapproved);
                DataTable dtF = await prMastDB.ClosingStockFIFO_LIFO();
                foreach (DataRow drF in dtF.Rows)
                {
                    clsPrMast.PrCode = Convert.ToInt32(drF["PRCODE"]);
                    decimal RTot = Convert.ToDecimal(drF["RQTY"]);
                    decimal ITot = Convert.ToDecimal(drF["IQTY"]);
                    decimal Stock = Convert.ToDecimal(drF["STOCK"]);

                    DataTable dtF1 = await prMastDB.ReceiptstyByPrCode(clsPrMast);
                    if (dtF1.Rows.Count > 0)
                    {
                        if (Convert.ToInt32(drF["COSTMETH"]) == 4)
                        {
                            foreach (DataRow drF1 in dtF1.Rows)
                            {
                                if (Convert.ToDecimal(drF1["RQTY"]) - ITot > 0)
                                {
                                    CStk += (Convert.ToDecimal(drF1["RQTY"]) - ITot) * Convert.ToDecimal(drF1["RATE"]);
                                    int flg = 0;
                                    foreach (DataRow dr in dts.Rows)
                                    {
                                        if (Convert.ToInt32(dr["PRGRPID"]) == Convert.ToInt32(drF1["GROUPCODE"]))
                                        {
                                            dr["STOCK"] = Convert.ToDecimal(dr["STOCK"]) + (Convert.ToDecimal(drF1["RQTY"]) - ITot) * Convert.ToDecimal(drF1["RATE"]);
                                            flg = 1;
                                            break;
                                        }
                                    }

                                    if (flg == 0)
                                    {

                                        clsPrGrpMas.PrGrpId = Convert.ToInt32(drF1["GROUPCODE"]);
                                        System.Data.DataTable dtGrp = await prGrpMasDB.StockAccDetails(clsPrGrpMas);
                                        if (dtGrp.Rows.Count > 0)
                                        {
                                            DataRow drGrp = dtGrp.Rows[0];
                                            dts.Rows.Add(drGrp["ACCID"], drGrp["PRGRPID"], drGrp["ACCNAME"], (Convert.ToDecimal(drF1["RQTY"]) - ITot) * Convert.ToDecimal(drF1["RATE"]));
                                        }
                                    }

                                    ITot = 0;
                                }
                                else
                                {
                                    ITot = ITot - Convert.ToDecimal(drF1["RQTY"]);
                                }
                            }
                        }
                        else if (Convert.ToInt32(drF["COSTMETH"]) == 5)
                        {
                            int i = dtF1.Rows.Count - 1;
                            while (i >= 0)
                            {
                                DataRow drF1 = dtF1.Rows[i];
                                if (Convert.ToDecimal(drF1["RQTY"]) - ITot > 0)
                                {
                                    CStk += (Convert.ToDecimal(drF1["RQTY"]) - ITot) * Convert.ToDecimal(drF1["RATE"]);

                                    int flg = 0;
                                    foreach (DataRow dr in dts.Rows)
                                    {
                                        if (dr["PRGRPID"] == drF1["GROUPCODE"])
                                        {
                                            dr["STOCK"] = Convert.ToDecimal(dr["STCOK"]) + (Convert.ToDecimal(drF1["RQTY"]) - ITot) * Convert.ToDecimal(drF1["RATE"]);
                                            flg = 1;
                                            break;
                                        }
                                    }

                                    if (flg == 0)
                                    {

                                        clsPrGrpMas.PrGrpId = Convert.ToInt32(drF1["GROUPCODE"]);
                                        System.Data.DataTable dtGrp = await prGrpMasDB.StockAccDetails(clsPrGrpMas);
                                        if (dtGrp.Rows.Count > 0)
                                        {
                                            DataRow drGrp = dtGrp.Rows[0];
                                            dts.Rows.Add(drGrp["ACCID"], drGrp["PRGRPID"], drGrp["ACCNAME"], (Convert.ToDecimal(drF1["RQTY"]) - ITot) * Convert.ToDecimal(drF1["RATE"]));
                                        }
                                    }
                                }
                                else
                                {
                                    ITot = ITot - Convert.ToDecimal(drF1["RQTY"]);
                                }

                                i--;
                            }
                        }
                    }
                }

                DataTable dtTrans = await prMastDB.WACTransactions();
                DataTable dtWAC = await prMastDB.ClosingStockWAC();
                foreach (DataRow drW in dtWAC.Rows)
                {
                    clsPrMast.PrCode = Convert.ToInt32(drW["PRCODE"]);
                    decimal RTot = 0;
                    decimal ITot = 0;
                    decimal Stock = 0;
                    decimal StockValue = 0;
                    decimal Rate = 0;

                    DataRow[] dtR = dtTrans.Select("PRCODE='" + Convert.ToInt32(drW["PRCODE"]) + "'", "VRDATE,UTIME ASC");
                    foreach (DataRow drW1 in dtR)
                    {
                        decimal RQty = Convert.ToDecimal(drW1["RQTY"]);
                        decimal IQty = Convert.ToDecimal(drW1["IQTY"]);
                        decimal TRate = Convert.ToDecimal(drW1["RATE"]);
                        if (RQty != 0)
                        {
                            if (RTot == 0 && ITot > 0 && drW1["VRTYPE"].ToString() != "RET" && (drW1["VRTYPE"].ToString() != "ST" || (Variables.StkIssRec && await purmDB.GetTrType(Convert.ToInt32(drW1["VRNO"].ToString() == "" ? "0" : drW1["VRNO"].ToString())) != "ST")))
                                StockValue += (Stock * TRate);

                            RTot += RQty;
                            if (drW1["VRTYPE"] != DBNull.Value && drW1["VRTYPE"].ToString() != "RET" && (drW1["VRTYPE"].ToString() != "ST" || (Variables.StkIssRec && await purmDB.GetTrType(Convert.ToInt32(drW1["VRNO"].ToString() == "" ? "0" : drW1["VRNO"].ToString())) != "ST")))
                            {
                                Rate = TRate;
                                StockValue += (RQty * TRate);
                            }
                            else
                            {
                                StockValue += (RQty * Rate);
                            }

                            Stock += RQty;

                            if (Stock != 0 && (drW1["VRTYPE"].ToString() != "ST" || (Variables.StkIssRec && await purmDB.GetTrType(Convert.ToInt32(drW1["VRNO"].ToString() == "" ? "0" : drW1["VRNO"].ToString())) != "ST")))
                                Rate = Math.Round(StockValue / Stock, Variables.DecimalPoint);
                        }
                        else if (IQty != 0)
                        {
                            if (drW1["VRTYPE"] != DBNull.Value && (drW1["VRTYPE"].ToString() == "RET" || drW1["VRTYPE"].ToString() == "OS"))
                            {
                                ITot += IQty;
                                StockValue -= (IQty * TRate);
                                Stock -= IQty;
                                if (Stock != 0)
                                    Rate = Math.Round(StockValue / Stock, Variables.DecimalPoint, MidpointRounding.AwayFromZero);
                            }
                            else
                            {
                                ITot += IQty;
                                StockValue -= (IQty * Rate);
                                Stock -= IQty;
                            }
                        }

                        if (Rate < 0)
                            Rate = -1 * Rate;
                    }

                    StockValue = Stock * Rate;
                    CStk += StockValue;

                    int flg = 0;
                    foreach (DataRow dr in dts.Rows)
                    {
                        if (Convert.ToInt32(dr["PRGRPID"]) == Convert.ToInt32(drW["GROUPCODE"]))
                        {
                            if (dr["STOCK"] != DBNull.Value && !string.IsNullOrEmpty(dr["STOCK"].ToString()))
                                dr["STOCK"] = Convert.ToDecimal(dr["STOCK"]) + Math.Round(StockValue, Variables.DecimalPoint);
                            else
                                dr["STOCK"] = Math.Round(StockValue, Variables.DecimalPoint);
                            flg = 1;
                            break;
                        }
                    }
                    if (flg == 0)
                    {

                        clsPrGrpMas.PrGrpId = Convert.ToInt32(drW["GROUPCODE"]);
                        System.Data.DataTable dtGrp = await prGrpMasDB.StockAccDetails(clsPrGrpMas);
                        if (dtGrp.Rows.Count > 0)
                        {
                            DataRow drGrp = dtGrp.Rows[0];
                            dts.Rows.Add(drGrp["ACCID"], drGrp["PRGRPID"], drGrp["ACCNAME"], StockValue);
                        }
                    }
                }

                DataRow[] drS = dts.Select();
                if (drS.Count() > 0)
                {
                    DataTable dt_1 = drS.CopyToDataTable();
                    var result = dt_1.AsEnumerable().GroupBy(x => x.Field<int>("ACCID")).Select(grp => new { ACCID = grp.Key, STOCK = grp.Sum(x => Convert.ToDouble(x["STOCK"])) }).ToArray();

                    dts.Rows.Clear();
                    foreach (var a in result)
                    {
                        string AccName = dt_1.Select("ACCID='" + a.ACCID + "'")[0]["ACCNAME"].ToString();
                        dts.Rows.Add(a.ACCID, null, AccName, a.STOCK);
                    }
                }
                /*****************************STOCK ENDS***********************************/

                /*****************TRN-NOTE DIFFERENCE CALCULATION******************/
                await stkDtDB.CreateTBForPL(clsStkDt, clsDate, Unapproved: request.IncludeUnapproved);
                if (FOrder == "T" || FOrder == "B")
                {
                    decimal SalDif = 0;
                    if (FOrder == "T")
                        SalDif = await GetSalDiff("T", request, report);

                    CStk += (-1 * SalDif);
                    if (CStk >= 0)
                    {
                        ri++;
                        dtP.Rows.Add(LngTB.Closing_Stock, null, LngTB.Closing_Stock, CStk.ToString(Variables.DecString), null, "", "", null, dtP.Rows.Count);
                        Credit += CStk;

                        foreach (DataRow drs in dts.Rows)
                        {
                            if (drs["STOCK"] != DBNull.Value)
                            {
                                ri++;
                                dtP.Rows.Add(" " + drs["ACCNAME"].ToString(), null, " " + drs["ACCNAME"].ToString(), Convert.ToDecimal(drs["STOCK"]).ToString(Variables.DecString) + " ", null, "", "", null, dtP.Rows.Count);
                            }
                        }

                        if (SalDif > 0)
                        {
                            dtP.Rows.Add(" " + LngTB.Delivery_Note_To_Prepare, null, " " + LngTB.Delivery_Note_To_Prepare, "(-)" + SalDif.ToString(Variables.DecString) + " ", 0 , "", "", null, dtP.Rows.Count);
                            ri++;
                        }
                        else if (SalDif < 0)
                        {
                            dtP.Rows.Add(" " + LngTB.Sales_Bills_To_Prepare, null, " " + LngTB.Sales_Bills_To_Prepare, (-1 * SalDif).ToString(Variables.DecString) + " ", 0, "", "", null, dtP.Rows.Count);
                            ri++;
                        }
                    }
                    else
                    {
                        li++;
                        dtP.Rows.Add(LngTB.Closing_Stock, (-1 * CStk).ToString(Variables.DecString), null, null, null, "", "", null, dtP.Rows.Count);
                        Debit += -1 * CStk;
                        foreach (DataRow drs in dts.Rows)
                        {
                            if (Convert.ToDecimal(drs["STOCK"]) > 0)
                            {
                                li++;
                                dtP.Rows.Add(" " + drs["ACCNAME"].ToString(), Convert.ToDecimal(drs["STOCK"]).ToString(Variables.DecString) + " ", null, null, null, "", "", null, dtP.Rows.Count);
                            }
                            else
                            {
                                li++;
                                dtP.Rows.Add(" " + drs["ACCNAME"].ToString(), (-1 * Convert.ToDecimal(drs["STOCK"])).ToString(Variables.DecString) + " ", null, null, null, "", "", null, dtP.Rows.Count);
                            }
                        }

                        if (SalDif > 0)
                        {
                            dtP.Rows.Add(" " + LngTB.Delivery_Note_To_Prepare, "(-)" + SalDif.ToString(Variables.DecString) + " ", null, null, 0, "", "", null, dtP.Rows.Count);
                            ri++;
                        }
                        else if (SalDif < 0)
                        {
                            dtP.Rows.Add(" " + LngTB.Sales_Bills_To_Prepare, null, " " + LngTB.Sales_Bills_To_Prepare, (-1 * SalDif).ToString(Variables.DecString) + " ", 0, "", "", null, dtP.Rows.Count);
                            ri++;
                        }
                    }
                }
                /******************************TRN-NOTE DIFF ENDS*****************************/

                /****************************Previous Report*****************************/
                if (FOrder == "T")
                    PrevProf = await dayBookDB.NetProfit("M", Unapproved: request.IncludeUnapproved);
                else if (FOrder == "P")
                    PrevProf = report.GrossProfit;

                if (PrevProf > 0)
                {
                    dtP.Rows.Add(LngTB.Gross_Profit_Bd, null, LngTB.Gross_Profit_Bd, PrevProf.ToString(Variables.DecString), null, null, "GPB", null, dtP.Rows.Count);
                    ri++;

                    Credit += PrevProf;
                }
                else if (PrevProf < 0)
                {
                    dtP.Rows.Add(LngTB.Gross_Loss_Bd, (-1 * PrevProf).ToString(Variables.DecString), null, null, null, null, "GLB", null, dtP.Rows.Count);
                    li++;

                    Debit += -1 * PrevProf;
                }

                /***********************Prev Report ENDS*************************/
                clsAccMas.Id = Variables.CashCD;
                DataTable dtAcc = await accMasDB.SearchByID(clsAccMas, DefBranch: false);

                int CashGrpId = 0;
                decimal Cashbal = 0;
                if (dtAcc.Rows.Count > 0)
                    CashGrpId = Convert.ToInt32(dtAcc.Rows[0]["MGROUP"]);

                clsDate.DateFrom = dtp_from.ToString(Variables.format);
                clsDate.ToDate = dtp_to.ToString(Variables.format);
                do
                {
                    dt = await dayBookDB.PL(FOrder, Unapproved: request.IncludeUnapproved);
                }
                while (dt == null || dt.Columns.Count == 0);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataRow dr = dt.Rows[i];

                    decimal PurDif = 0;
                    PurDif = await stkDtDB.GetPurDiffByGroupAndDate(Convert.ToInt32(dr["AccId"]), clsDate, Unapproved: request.IncludeUnapproved);
                    PurDif = (-1 * PurDif);

                    if (Convert.ToInt32(dr["AccId"]) == CashGrpId)
                    {
                        dayBookDB.tableName = tablename;
                        dayBookDB.OpTable = opTableName;
                        Cashbal += await dayBookDB.CashBalance();
                        dr["Balance"] = (Convert.ToDecimal(dr["Balance"]) + Cashbal).ToString();
                    }

                    if (Convert.ToDecimal(dr["Balance"]) > 0)
                    {
                        dtP.Rows.Add(dr["AccName"], null, dr["AccName"], (Convert.ToDecimal(dr["Balance"]) + PurDif /*- SalDif*/).ToString(Variables.DecString), dr["AccId"], "", "", null, dtP.Rows.Count);
                        ri++;

                        if (PurDif != 0)
                        {
                            dtP.Rows.Add("  " + dr["AccName"].ToString(), null, "  " + dr["AccName"].ToString(), (Convert.ToDecimal(dr["Balance"])).ToString(Variables.DecString) + " ", dr["AccId"], "", "", null, dtP.Rows.Count);
                            ri++;
                        }
                        if (PurDif > 0)
                        {
                            dtP.Rows.Add("  " + LngTB.Purchase_Bills_To_Receive, null, "  " + LngTB.Purchase_Bills_To_Receive, (PurDif).ToString(Variables.DecString) + " ", dr["AccId"], "", "", null, dtP.Rows.Count);
                            ri++;
                        }
                        else if (PurDif < 0)
                        {
                            dtP.Rows.Add("  " + LngTB.Receipt_Note_To_Receive, null, "  " + LngTB.Receipt_Note_To_Receive, "(-)" + (-1 * PurDif).ToString(Variables.DecString) + " ", dr["AccId"], "", "", null, dtP.Rows.Count);
                            ri++;
                        }

                        Credit += (Convert.ToDecimal(dr["Balance"]) + PurDif);
                        decimal balance = Convert.ToDecimal(dr["Balance"]);
                    }
                    else if (Convert.ToDecimal(dr["Balance"]) < 0)
                    {
                        dtP.Rows.Add(dr["AccName"], ((-1 * Convert.ToDecimal(dr["Balance"])) + PurDif /*- SalDif*/).ToString(Variables.DecString), null, null, dr["AccId"], "", "", null, dtP.Rows.Count);
                        li++;

                        if (PurDif != 0)
                        {
                            dtP.Rows.Add("  " + dr["AccName"].ToString(), (-1 * Convert.ToDecimal(dr["Balance"])).ToString(Variables.DecString) + " ", null, null, dr["AccId"], "", "", null, dtP.Rows.Count);
                            li++;
                        }
                        if (PurDif > 0)
                        {
                            dtP.Rows.Add("  " + LngTB.Purchase_Bills_To_Receive, PurDif.ToString(Variables.DecString) + " ", null, null, dr["AccId"], "", "", null, dtP.Rows.Count);
                            li++;
                        }
                        else if (PurDif < 0)
                        {
                            dtP.Rows.Add(" " + LngTB.Receipt_Note_To_Receive, "(-)" + (-1 * PurDif).ToString(Variables.DecString) + " ", null, null, dr["AccId"], "", "", null, dtP.Rows.Count);
                            li++;
                        }

                        Debit += (-1 * Convert.ToDecimal(dr["Balance"])) + PurDif;
                    }
                    else if (Convert.ToDecimal(dr["Balance"]) == 0)
                    {
                        if (PurDif != 0)
                        {
                            dtP.Rows.Add(dr["AccName"], ((-1 * Convert.ToDecimal(dr["Balance"])) + PurDif /*- SalDif*/).ToString(Variables.DecString), null, null, dr["AccId"], "", "", null, dtP.Rows.Count);
                            li++;

                            if ((PurDif != 0))
                            {
                                dtP.Rows.Add("  " + dr["AccName"].ToString(), (-1 * Convert.ToDecimal(dr["Balance"])).ToString(Variables.DecString) + " ", null, null, dr["AccId"], "", "", null, dtP.Rows.Count);
                                li++;


                            }
                            if (PurDif > 0)
                            {
                                dtP.Rows.Add("  " + LngTB.Purchase_Bills_To_Receive, PurDif.ToString(Variables.DecString) + " ", null, null, dr["AccId"], "", "", null, dtP.Rows.Count);
                                li++;
                            }
                            else if (PurDif < 0)
                            {
                                dtP.Rows.Add("  " + LngTB.Receipt_Note_To_Receive, "(-)" + (-1 * PurDif).ToString(Variables.DecString) + " ", null, null, dr["AccId"], "", "", null, dtP.Rows.Count);
                                li++;
                            }

                            Debit += (-1 * Convert.ToDecimal(dr["Balance"])) + PurDif /*- SalDif*/;
                        }
                    }
                }

                await prMastDB.DropTable();
                await stkDtDB.DropTBForPL();

                Balance = Credit - Debit;

                if (FOrder == "P")
                {
                    if (Balance >= 0)
                    {
                        dtP.Rows.Add(LngTB.Net_Profit, Balance.ToString(Variables.DecString), null, null, null, null, "NP", null, dtP.Rows.Count);
                        Debit += Balance;
                    }
                    else
                    {
                        dtP.Rows.Add(LngTB.Net_Loss, null, LngTB.Net_Loss, (-1 * Balance).ToString(Variables.DecString), null, null, "NL", null, dtP.Rows.Count);
                        Credit += (-1 * Balance);
                    }

                    ri++;
                    li++;
                }
                else
                {
                    if (Balance >= 0)
                    {
                        dtP.Rows.Add(LngTB.Gross_Profit, Balance.ToString(Variables.DecString), null, null, null, null, "GP", null, dtP.Rows.Count);
                        Debit += Balance;
                    }
                    else
                    {
                        dtP.Rows.Add(LngTB.Gross_Profit, null, LngTB.Gross_Profit, (-1 * Balance).ToString(Variables.DecString), null, null, "GL", null, dtP.Rows.Count);
                        Credit += (-1 * Balance);
                    }

                    ri++;
                    li++;
                }

                ri++;
                li++;

                if (FOrder == "T")
                    dtP.Rows.Add(LngTB.Total.ToUpper(), Debit.ToString(Variables.DecString), LngTB.Total.ToUpper(), Credit.ToString(Variables.DecString), null, 'B', "TT", null, dtP.Rows.Count);
                else
                    dtP.Rows.Add(LngTB.Total.ToUpper(), Debit.ToString(Variables.DecString), LngTB.Total.ToUpper(), Credit.ToString(Variables.DecString), null, 'B', "PT", null, dtP.Rows.Count);

                report.GrossProfit = Balance;
                await dayBookDB.DropTable();
            }
            catch (Exception ex) { }
        }

        private async Task<decimal> StockValue(DateTime dtFrom, DateTime DtTo, MonthWiseProfitAndLossRequest request, MonthWiseProfitAndLossContext report)
        {
            Variable Variables = report.Variables;

            ClsDate clsDate = report.clsDate;
            ClsPrMast clsPrMast = report.clsPrMast;
            ClsPrGrpMas clsPrGrpMas = report.clsPrGrpMas;
            ClsStkDt clsStkDt = report.clsStkDt;

            PurmDB purmDB = report.purmDB;
            PrMastDB prMastDB = report.prMastDB;
            PrGrpMasDB prGrpMasDB = report.prGrpMasDB;
            StkDtDB stkDtDB = report.stkDtDB;

            clsDate.DateFrom = dtFrom.ToString(Variables.format);
            clsDate.ToDate = DtTo.ToString(Variables.format);
            await prMastDB.CreateForClosingStock(clsDate, false, Unapproved: request.IncludeUnapproved);

            decimal CStk = 0;
            DataTable dts = await prMastDB.ClosingStockValueLedgerWise();
            await prMastDB.CreateForCStkFIFO(clsDate, Unapproved: request.IncludeUnapproved);
            DataTable dtF = await prMastDB.ClosingStockFIFO_LIFO();
            foreach (DataRow drF in dtF.Rows)
            {
                clsPrMast.PrCode = Convert.ToInt32(drF["PRCODE"]);
                decimal RTot = Convert.ToDecimal(drF["RQTY"]);
                decimal ITot = Convert.ToDecimal(drF["IQTY"]);
                decimal Stock = Convert.ToDecimal(drF["STOCK"]);

                DataTable dtF1 = await prMastDB.ReceiptstyByPrCode(clsPrMast);
                if (dtF1.Rows.Count > 0)
                {
                    if (Convert.ToInt32(drF["COSTMETH"]) == 4)
                    {
                        foreach (DataRow drF1 in dtF1.Rows)
                        {
                            if (Convert.ToDecimal(drF1["RQTY"]) - ITot > 0)
                            {
                                CStk += (Convert.ToDecimal(drF1["RQTY"]) - ITot) * Convert.ToDecimal(drF1["RATE"]);
                                int flg = 0;
                                foreach (DataRow dr in dts.Rows)
                                {
                                    if (Convert.ToInt32(dr["PRGRPID"]) == Convert.ToInt32(drF1["GROUPCODE"]))
                                    {
                                        dr["STOCK"] = Convert.ToDecimal(dr["STOCK"]) + (Convert.ToDecimal(drF1["RQTY"]) - ITot) * Convert.ToDecimal(drF1["RATE"]);
                                        flg = 1;
                                        break;
                                    }
                                }

                                if (flg == 0)
                                {

                                    clsPrGrpMas.PrGrpId = Convert.ToInt32(drF1["GROUPCODE"]);
                                    DataTable dtGrp = await prGrpMasDB.StockAccDetails(clsPrGrpMas);
                                    if (dtGrp.Rows.Count > 0)
                                    {
                                        DataRow drGrp = dtGrp.Rows[0];
                                        dts.Rows.Add(drGrp["ACCID"], drGrp["PRGRPID"], drGrp["ACCNAME"], (Convert.ToDecimal(drF1["RQTY"]) - ITot) * Convert.ToDecimal(drF1["RATE"]));
                                    }
                                }

                                ITot = 0;
                            }
                            else
                            {
                                ITot = ITot - Convert.ToDecimal(drF1["RQTY"]);
                            }
                        }
                    }
                    else if (Convert.ToInt32(drF["COSTMETH"]) == 5)
                    {
                        int i = dtF1.Rows.Count - 1;
                        while (i >= 0)
                        {
                            DataRow drF1 = dtF1.Rows[i];
                            if (Convert.ToDecimal(drF1["RQTY"]) - ITot > 0)
                            {
                                CStk += (Convert.ToDecimal(drF1["RQTY"]) - ITot) * Convert.ToDecimal(drF1["RATE"]);
                                int flg = 0;
                                foreach (DataRow dr in dts.Rows)
                                {
                                    if (dr["PRGRPID"] == drF1["GROUPCODE"])
                                    {
                                        dr["STOCK"] = Convert.ToDecimal(dr["STCOK"]) + (Convert.ToDecimal(drF1["RQTY"]) - ITot) * Convert.ToDecimal(drF1["RATE"]);
                                        flg = 1;
                                        break;
                                    }
                                }

                                if (flg == 0)
                                {

                                    clsPrGrpMas.PrGrpId = Convert.ToInt32(drF1["GROUPCODE"]);
                                    System.Data.DataTable dtGrp = await prGrpMasDB.StockAccDetails(clsPrGrpMas);
                                    if (dtGrp.Rows.Count > 0)
                                    {
                                        DataRow drGrp = dtGrp.Rows[0];
                                        dts.Rows.Add(drGrp["ACCID"], drGrp["PRGRPID"], drGrp["ACCNAME"], (Convert.ToDecimal(drF1["RQTY"]) - ITot) * Convert.ToDecimal(drF1["RATE"]));
                                    }
                                }
                            }
                            else
                            {
                                ITot = ITot - Convert.ToDecimal(drF1["RQTY"]);
                            }

                            i--;
                        }
                    }
                }
            }

            DataTable dtTrans = await prMastDB.WACTransactions();
            DataTable dtWAC = await prMastDB.ClosingStockWAC();
            foreach (DataRow drW in dtWAC.Rows)
            {
                clsPrMast.PrCode = Convert.ToInt32(drW["PRCODE"]);
                decimal RTot = 0;
                decimal ITot = 0;
                decimal Stock = 0;
                decimal StockValue = 0;
                decimal Rate = 0;

                DataRow[] dtR = dtTrans.Select("PRCODE='" + Convert.ToInt32(drW["PRCODE"]) + "'", "VRDATE,UTIME ASC");
                foreach (DataRow drW1 in dtR)
                {
                    decimal RQty = Convert.ToDecimal(drW1["RQTY"]);
                    decimal IQty = Convert.ToDecimal(drW1["IQTY"]);
                    decimal TRate = Convert.ToDecimal(drW1["RATE"]);
                    if (RQty != 0)
                    {
                        if (RTot == 0 && ITot > 0 && drW1["VRTYPE"].ToString() != "RET" && (drW1["VRTYPE"].ToString() != "ST" || (Variables.StkIssRec && await purmDB.GetTrType(Convert.ToInt32(drW1["VRNO"].ToString() == "" ? "0" : drW1["VRNO"].ToString())) != "ST")))
                            StockValue += (Stock * TRate);

                        RTot += RQty;
                        if (drW1["VRTYPE"] != DBNull.Value && drW1["VRTYPE"].ToString() != "RET" && (drW1["VRTYPE"].ToString() != "ST" || (Variables.StkIssRec && await purmDB.GetTrType(Convert.ToInt32(drW1["VRNO"].ToString() == "" ? "0" : drW1["VRNO"].ToString())) != "ST")))
                        {
                            Rate = TRate;
                            StockValue += (RQty * TRate);
                        }
                        else
                        {
                            StockValue += (RQty * Rate);
                        }

                        Stock += RQty;

                        if (Stock != 0 && (drW1["VRTYPE"].ToString() != "ST" || (Variables.StkIssRec && await purmDB.GetTrType(Convert.ToInt32(drW1["VRNO"].ToString() == "" ? "0" : drW1["VRNO"].ToString())) != "ST")))
                            Rate = Math.Round(StockValue / Stock, Variables.DecimalPoint);
                    }
                    else if (IQty != 0)
                    {
                        if (drW1["VRTYPE"] != DBNull.Value && (drW1["VRTYPE"].ToString() == "RET" || drW1["VRTYPE"].ToString() == "OS"))
                        {
                            ITot += IQty;
                            StockValue -= (IQty * TRate);
                            Stock -= IQty;
                            if (Stock != 0)
                                Rate = Math.Round(StockValue / Stock, Variables.DecimalPoint, MidpointRounding.AwayFromZero);
                        }
                        else
                        {
                            ITot += IQty;
                            StockValue -= (IQty * Rate);
                            Stock -= IQty;
                        }
                    }

                    if (Rate < 0)
                        Rate = -1 * Rate;
                }

                StockValue = Stock * Rate;
                CStk += StockValue;

                int flg = 0;
                foreach (DataRow dr in dts.Rows)
                {
                    if (Convert.ToInt32(dr["PRGRPID"]) == Convert.ToInt32(drW["GROUPCODE"]))
                    {
                        if (dr["STOCK"] != DBNull.Value && !string.IsNullOrEmpty(dr["STOCK"].ToString()))
                            dr["STOCK"] = Convert.ToDecimal(dr["STOCK"]) + Math.Round(StockValue, Variables.DecimalPoint);
                        else
                            dr["STOCK"] = Math.Round(StockValue, Variables.DecimalPoint);

                        flg = 1;
                        break;
                    }
                }

                if (flg == 0)
                {

                    clsPrGrpMas.PrGrpId = Convert.ToInt32(drW["GROUPCODE"]);
                    DataTable dtGrp = await prGrpMasDB.StockAccDetails(clsPrGrpMas);
                    if (dtGrp.Rows.Count > 0)
                    {
                        DataRow drGrp = dtGrp.Rows[0];
                        dts.Rows.Add(drGrp["ACCID"], drGrp["PRGRPID"], drGrp["ACCNAME"], StockValue);
                    }

                }
            }

            DataRow[] drS = dts.Select();
            if (drS.Count() > 0)
            {
                DataTable dt = drS.CopyToDataTable();
                var result = dt.AsEnumerable().GroupBy(x => x.Field<int>("ACCID")).Select(grp => new { ACCID = grp.Key, STOCK = grp.Sum(x => Convert.ToDouble(x["STOCK"])) }).ToArray();
                dts.Rows.Clear();
                foreach (var a in result)
                {
                    string AccName = dt.Select("ACCID='" + a.ACCID + "'")[0]["ACCNAME"].ToString();
                    dts.Rows.Add(a.ACCID, null, AccName, a.STOCK);
                }
            }

            await stkDtDB.CreateTBForPL(clsStkDt, clsDate, Unapproved: request.IncludeUnapproved);
            CStk += (-1 * await GetSalDiff("T", request, report));
            await stkDtDB.DropTBForPL();
            await prMastDB.DropTable();
            return CStk;
        }

        private async Task<decimal> GetSalDiff(string FOrder, MonthWiseProfitAndLossRequest request, MonthWiseProfitAndLossContext report)
        {
            Variable Variables = report.Variables;

            ClsDate clsDate = report.clsDate;
            ClsPrMast clsPrMast = report.clsPrMast;
            ClsStkDt clsStkDt = report.clsStkDt;

            PurmDB purmDB = report.purmDB;
            PrMastDB prMastDB = report.prMastDB;
            StkDtDB stkDtDB = report.stkDtDB;

            decimal SalDif = 0;
            DataTable dt = await stkDtDB.GetSalQtyDiffByFOrder(FOrder);

            if (dt.Rows.Count == 0)
                report.RecFlag = true;

            foreach (DataRow dr in dt.Rows)
            {
                report.RecFlag = false;
                decimal SalQty = Convert.ToDecimal(dr["SLQTY"]);
                decimal DNQty = Convert.ToDecimal(dr["DNQTY"]);
                decimal QtyDiff = Convert.ToDecimal(dr["DIFQTY"]);
                decimal PurPrice = Convert.ToDecimal(dr["PURPRICE"]);
                int CostMeth = Convert.ToInt32(dr["COSTMETH"]);

                clsPrMast.PrCode = Convert.ToInt32(dr["PRCODE"]);
                DataTable dtRec = await prMastDB.ReceiptNotesByPrCode(clsPrMast);
                DataTable dtStock = await prMastDB.ReceiptNotesByPrCode(clsPrMast);
                if (CostMeth == 1 || CostMeth == 6)
                {
                    report.RecFlag = true;

                    if (dtStock.Rows.Count > 0 && dtStock.Rows[0]["PRPRICE"].ToString() != "")
                        SalDif += QtyDiff * Convert.ToDecimal(dtStock.Rows[0]["PRPRICE"]);
                }
                else if (CostMeth == 2)
                {
                    DataTable dtD = new DataTable();
                    dtD.Columns.Add("SalNo");
                    dtD.Columns.Add("GDNNo");
                    dtD.Columns.Add("Rate");

                    DataTable dtW = await prMastDB.TransactionsWithSalesByPrCode(clsPrMast, clsDate, Unapproved: request.IncludeUnapproved);
                    if (dtW.Rows.Count == 0)
                        report.RecFlag = true;

                    decimal Stock = 0, StockValue = 0, Rate = 0;
                    decimal RTot = 0, ITot = 0;
                    foreach (DataRow drW in dtW.Rows)
                    {
                        decimal RQty = Convert.ToDecimal(drW["RQTY"]);
                        decimal IQty = Convert.ToDecimal(drW["IQTY"]);
                        decimal TRate = Convert.ToDecimal(drW["RATE"]);

                        if (RQty != 0)
                        {
                            report.RecFlag = true;
                            if (RTot == 0 && ITot > 0)
                                StockValue += (Stock * TRate);
                            if (drW["VRTYPE"] != DBNull.Value && drW["VRTYPE"].ToString() != "RET" && (drW["VRTYPE"].ToString() != "ST" || (Variables.StkIssRec && await purmDB.GetTrType(Convert.ToInt32(drW["VRNO"].ToString() == "" ? "0" : drW["VRNO"].ToString())) != "ST")))
                            {
                                Rate = TRate;
                                StockValue += (RQty * TRate);
                            }
                            else
                            {
                                StockValue += (RQty * Rate);
                            }

                            Stock += RQty;
                            if (Stock != 0)
                                Rate = StockValue / Stock;

                            RTot += RQty;
                        }
                        else if (IQty != 0)
                        {
                            if (drW["VRTYPE"].ToString() == "NT")
                            {
                                clsStkDt.VrType = "NT";
                                clsStkDt.NtNo = Convert.ToInt32(drW["VRNO"]);
                                clsStkDt.PrCode = Convert.ToInt32(drW["PRCODE"]);
                                clsStkDt.Type = "I";
                                DataTable dtTr = await stkDtDB.SearchByVrNo(clsStkDt);
                                if (dtTr.Rows.Count > 0)
                                {
                                    int TrnNo = Convert.ToInt32(dtTr.Rows[0]["TRNNo"]);
                                    if (TrnNo != 0)
                                    {
                                        DataRow[] drTDr = dtD.Select("SalNo='" + TrnNo + "'");
                                        if (drTDr.Count() > 0)
                                            dtD.Rows.Add(0, Convert.ToInt32(drW["VRNO"]), Rate);
                                        else
                                            dtD.Rows.Add(0, Convert.ToInt32(drW["VRNO"]), Rate);
                                    }
                                    else
                                    {
                                        dtD.Rows.Add(0, Convert.ToInt32(drW["VRNO"]), Rate);
                                    }
                                }
                                else
                                {
                                    dtD.Rows.Add(0, Convert.ToInt32(drW["VRNO"]), Rate);
                                }

                                StockValue -= (IQty * Rate);
                                Stock -= IQty;
                            }
                            else if (drW["VRTYPE"].ToString() == "RET")
                            {
                                StockValue -= (IQty * TRate);
                                Stock -= IQty;
                                if (Stock != 0)
                                    Rate = Math.Round(StockValue / Stock, Variables.DecimalPoint);
                            }
                            else if (drW["VRTYPE"].ToString() == "TR")
                            {
                                clsStkDt.VrType = "TR";
                                clsStkDt.TrnNo = Convert.ToInt32(drW["VRNO"]);
                                clsStkDt.PrCode = Convert.ToInt32(drW["PRCODE"]);
                                clsStkDt.Type = "I";
                                DataTable dtTr = await stkDtDB.SearchByVrNo(clsStkDt);
                                if (dtTr.Rows.Count > 0)
                                {
                                    int NtNo = Convert.ToInt32(dtTr.Rows[0]["NTNo"]);
                                    if (NtNo != 0)
                                    {
                                        DataRow[] drTDr = dtD.Select("GDNNo='" + NtNo + "'");
                                        if (drTDr.Count() > 0)
                                            dtD.Rows.Add(Convert.ToInt32(drW["VRNO"]), 0, Rate);
                                        else
                                            dtD.Rows.Add(Convert.ToInt32(drW["VRNO"]), 0, Rate);
                                    }
                                    else
                                    {
                                        dtD.Rows.Add(Convert.ToInt32(drW["VRNO"]), 0, Rate);
                                    }
                                }
                                else
                                {
                                    dtD.Rows.Add(Convert.ToInt32(drW["VRNO"]), 0, Rate);
                                }
                            }
                            ITot += IQty;
                        }
                    }

                    SalDif += (QtyDiff * Rate);
                }
                else if (CostMeth == 3)
                {
                    report.RecFlag = true;
                    SalDif = PurPrice * QtyDiff;
                }
                else if (CostMeth == 4)
                {
                    report.RecFlag = true;
                    foreach (DataRow drRec in dtRec.Rows)
                    {
                        if (SalQty != 0)
                        {
                            if (SalQty >= Convert.ToDecimal(drRec["RQTY"]))
                            {
                                SalDif += Convert.ToDecimal(drRec["RQTY"]) * Convert.ToDecimal(drRec["RATE"]);
                                SalQty -= Convert.ToDecimal(drRec["RQTY"]);
                            }
                            else
                            {
                                SalDif += SalQty * Convert.ToDecimal(drRec["RATE"]);
                                SalQty = 0;
                            }
                        }

                        if (DNQty != 0)
                        {
                            if (DNQty >= Convert.ToDecimal(drRec["RQTY"]))
                            {
                                SalDif -= Convert.ToDecimal(drRec["RQTY"]) * Convert.ToDecimal(drRec["RATE"]);
                                DNQty -= Convert.ToDecimal(drRec["RQTY"]);
                            }
                            else
                            {
                                SalDif -= DNQty * Convert.ToDecimal(drRec["RATE"]);
                                DNQty = 0;
                            }
                        }

                        if (SalQty == 0 && DNQty == 0)
                            break;
                    }
                }
                else if (CostMeth == 5)
                {
                    report.RecFlag = true;
                    int i = dtRec.Rows.Count - 1;
                    while (i >= 0)
                    {
                        DataRow drRec = dtRec.Rows[i];
                        if (SalQty != 0)
                        {
                            if (SalQty >= Convert.ToDecimal(drRec["RQTY"]))
                            {
                                SalDif += Convert.ToDecimal(drRec["RQTY"]) * Convert.ToDecimal(drRec["RATE"]);
                                SalQty -= Convert.ToDecimal(drRec["RQTY"]);
                            }
                            else
                            {
                                SalDif += SalQty * Convert.ToDecimal(drRec["RATE"]);
                                SalQty = 0;
                            }
                        }

                        if (DNQty != 0)
                        {
                            if (DNQty >= Convert.ToDecimal(drRec["RQTY"]))
                            {
                                SalDif -= Convert.ToDecimal(drRec["RQTY"]) * Convert.ToDecimal(drRec["RATE"]);
                                DNQty -= Convert.ToDecimal(drRec["RQTY"]);
                            }
                            else
                            {
                                SalDif -= DNQty * Convert.ToDecimal(drRec["RATE"]);
                                DNQty = 0;
                            }
                        }

                        i--;

                        if (SalQty == 0 && DNQty == 0)
                            break;
                    }
                }
            }
            return SalDif;
        }
    }
}