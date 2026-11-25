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

    [Route("api/v1/reports/profit-and-loss")]
    [Authorize]
    public class ProfitAndLossController : ControllerBase
    {
        private readonly DatabaseConnection _databaseConnection;
        public ProfitAndLossController(DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }
        [HttpPost]
        public async Task<IActionResult> GetProfitLoss(ProfitAndLossRequest request)
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

                PlDB plDB = new PlDB(DBConn);
                plDB.Variables = Variables;
                plDB.Functions.Variables = Variables;

                OpnDirDB opnDirDB = new OpnDirDB(DBConn);
                opnDirDB.Variables = Variables;
                opnDirDB.Functions.Variables = Variables;

                AccMasDB accMasDB = new AccMasDB(DBConn);
                accMasDB.Variables = Variables;
                accMasDB.Functions.Variables = Variables;

                GrpLdgDB grpLdgDB = new GrpLdgDB(DBConn);
                grpLdgDB.Variables = Variables;
                grpLdgDB.Functions.Variables = Variables;

                //-- Get Report

                var report = new ProfitAndLossContext();

                report.Variables = Variables;
                report.Functions = commonFunction.Functions;

                report.dayBookDB = dayBookDB;
                report.purmDB = purmDB;
                report.taxMasDB = taxMasDB;
                report.prMastDB = prMastDB;
                report.prGrpMasDB = prGrpMasDB;
                report.stkDtDB = stkDtDB;
                report.plDB = plDB;
                report.opnDirDB = opnDirDB;
                report.accMasDB = accMasDB;
                report.grpLdgDB = grpLdgDB;

                DataTable dtP = report.dtP;
                dtP.Columns.Add("DebitGroup");
                dtP.Columns.Add("Debit");
                dtP.Columns.Add("CreditGroup");
                dtP.Columns.Add("Credit");
                dtP.Columns.Add("ID");
                dtP.Columns.Add("Type");
                dtP.Columns.Add("TypeL");
                dtP.Columns.Add("TypeR");

                DataTable dtP2 = report.dtP2;
                dtP2.Columns.Add("DebitGroup");
                dtP2.Columns.Add("Debit");
                dtP2.Columns.Add("CreditGroup");
                dtP2.Columns.Add("Credit");
                dtP2.Columns.Add("ID");

                DataTable dtDate = report.dtDate;
                report.dtDate.Columns.Add("From");
                report.dtDate.Columns.Add("To");
                report.dtDate.Columns.Add("RepDate");

                DataTable dtBrTrVr = report.dtBrTrVr;
                if (Variables.StkIssRec)
                    report.dtBrTrVr = await purmDB.GetTrTypeAll();

                if (request.ReportType == "M")
                {
                    await NewReport("M", request, report);
                }
                else if (request.ReportType == "T")
                {
                    await NewReport("T", request, report);
                }
                else if (request.ReportType == "P")
                {
                    await NewReport("T", request, report);
                    await NewReport("P", request, report);
                }
                else if (request.ReportType == "B")
                {
                    await NewReport("B", request, report);
                }

                await stkDtDB.DropTBForPL();
                await plDB.Drop();

                //--

                var tableData = report.dtP.AsEnumerable().Select(row => report.dtP.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).ToList();
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

        private async Task NewReport(string FOrder, ProfitAndLossRequest request, ProfitAndLossContext report)
        {
            DataTable dt = report.dt;
            DataTable dtP = report.dtP;

            Variable Variables = report.Variables;
            Function Functions = report.Functions;

            ClsDate clsDate = report.clsDate;
            ClsTaxMas clsTaxMas = report.clsTaxMas;
            ClsPrGrpMas clsPrGrpMas = report.clsPrGrpMas;
            ClsStkDt clsStkDt = report.clsStkDt;
            ClsOpnDir clsOpnDir = report.clsOpnDir;
            ClsAccMas clsAccMas = report.clsAccMas;

            DayBookDB dayBookDB = report.dayBookDB;
            TaxMasDBbkp taxMasDB = report.taxMasDB;
            PrMastDB prMastDB = report.prMastDB;
            PrGrpMasDB prGrpMasDB = report.prGrpMasDB;
            StkDtDB stkDtDB = report.stkDtDB;
            OpnDirDB opnDirDB = report.opnDirDB;
            AccMasDB accMasDB = report.accMasDB;
            GrpLdgDB grpLdgDB = report.grpLdgDB;

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
            clsDate.ToDate = request.ToDate.Date.ToString(Variables.format);

            if (request.FromDate.Date > Variables.fyStartDate.Date)
                await dayBookDB.CreateTable(Variables.fyStartDate.ToString(Variables.format), request.FromDate.Date.AddDays(-1).ToString(Variables.format), "OP", Unapproved: request.IncludeUnapproved);
            else
                await dayBookDB.CreateTable(Variables.fyStartDate.AddDays(-1).ToString(Variables.format), request.FromDate.Date.AddDays(-1).ToString(Variables.format), "OP", Unapproved: request.IncludeUnapproved);

            if (request.FromDate.ToString(Variables.format) == "" || request.ToDate.ToString(Variables.format) == "")
                await dayBookDB.CreateTable(request.FromDate.Date.ToString(Variables.format), request.ToDate.Date.ToString(Variables.format), "DB", Unapproved: request.IncludeUnapproved);
            else
                await dayBookDB.CreateTable(request.FromDate.ToString(Variables.format), request.ToDate.ToString(Variables.format), "DB", Unapproved: request.IncludeUnapproved);

            clsTaxMas.Code = Variables.VATCode;
            DataTable dtTax = await taxMasDB.SearchByID(clsTaxMas);
            if (dtTax.Rows.Count > 0)
            {
                DataRow dr = dtTax.Rows[0];
                await dayBookDB.InsertRow(request.FromDate.Date, Convert.ToInt32(dr["PURCODE"]));
                await dayBookDB.InsertRow(request.ToDate.Date, Convert.ToInt32(dr["SALCODE"]));
            }

            tablename = dayBookDB.tableName;
            opTableName = dayBookDB.OpTable;

            decimal OpenStock = 0;

            if (request.FromDate.Date > Variables.fyStartDate)
                OpenStock = await StockValueNew(Variables.fyStartDate, request.FromDate.Date.AddDays(-1), request, report);
            else
                OpenStock = await StockValueNew(request.FromDate.Date.AddDays(-1), request.FromDate.Date.AddDays(-1), request, report);

            clsDate.DateFrom = Variables.fyStartDate.ToString(Variables.format);
            clsDate.ToDate = request.ToDate.Date.ToString(Variables.format);

            await prMastDB.CreateForClosingStock(clsDate, false, PLReport: true, Unapproved: request.IncludeUnapproved);
            await prMastDB.insertToCSTK(clsDate, false, false, Unapproved: request.IncludeUnapproved);
            DataTable dtls = await prMastDB.ClosingStock_PL();
            decimal CStk = 0;
            if (dtls.Rows.Count > 0)
            {
                DataRow[] drls = dtls.Select("DS = 'Total'");
                if (drls.Length > 0)
                {
                    CStk = drls[0]["Tvalue"].ToString() == "" ? 0 : Convert.ToDecimal(drls[0]["Tvalue"]);
                }
            }

            DataTable dts = await prMastDB.ClosingStockValueLedgerWise();
            foreach (DataRow drls in dtls.Rows)
            {
                if (drls["DS"].ToString() != "Total")
                {
                    decimal StockValue = drls["Tvalue"].ToString() == "" ? 0 : Convert.ToDecimal(drls["Tvalue"]);

                    int flg = 0;
                    foreach (DataRow dr in dts.Rows)
                    {
                        if (Convert.ToInt32(dr["PRGRPID"]) == Convert.ToInt32(drls["GROUPCODE"]))
                        {
                            if (dr["STOCK"] != DBNull.Value && !string.IsNullOrEmpty(dr["STOCK"].ToString()))
                                dr["STOCK"] = Convert.ToDecimal(dr["STOCK"]) + Math.Round(StockValue, Variables.DecimalPoint, MidpointRounding.AwayFromZero);
                            else
                                dr["STOCK"] = Math.Round(StockValue, Variables.DecimalPoint, MidpointRounding.AwayFromZero);

                            flg = 1;
                            break;
                        }
                    }

                    if (flg == 0)
                    {
                        clsPrGrpMas.PrGrpId = Convert.ToInt32(drls["GROUPCODE"]);
                        DataTable dtGrp = await prGrpMasDB.StockAccDetails(clsPrGrpMas);
                        if (dtGrp.Rows.Count > 0)
                        {
                            DataRow drGrp = dtGrp.Rows[0];
                            dts.Rows.Add(drGrp["ACCID"], drGrp["PRGRPID"], drGrp["ACCNAME"], StockValue);
                        }
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

            decimal PrevSalDif = 0;
            if (FOrder == "T" || FOrder == "B")
            {
                if (FOrder != "B")
                {
                    if (OpenStock >= 0)
                    {
                        li++;
                        dtP.Rows.Add(LngTB.Opening_Stock, OpenStock.ToString(Variables.DecString), null, null, null, null, "LB", null);
                        Debit += OpenStock;
                    }
                    else
                    {
                        ri++;
                        dtP.Rows.Add(null, null, LngTB.Opening_Stock, (-1 * OpenStock).ToString(Variables.DecString), null, null, null, "RB");
                        Credit += OpenStock;
                    }
                }

                decimal SalDif = 0;
                if (FOrder == "T" || FOrder == "B")
                {
                    SalDif = await GetSalDiff("T", request, report);

                    if (request.FromDate.Date == Variables.fyStartDate.Date)
                    {
                        clsOpnDir.TYPENAME = "SIDN";
                        PrevSalDif = await opnDirDB.GetOpeningByTypeName(clsOpnDir);
                        SalDif += PrevSalDif;
                    }
                }

                report.SIDN_Amount = SalDif;
                CStk += (-1 * SalDif);
                if (CStk >= 0)
                {

                    if (FOrder == "B")
                    {
                        ri++;
                        dtP.Rows.Add(null, null, LngTB.Closing_Stock, CStk.ToString(Variables.DecString), null, null, null, "RB");
                    }
                    else
                    {
                        if (dtP.Rows.Count > 0 && ri == 0)
                        {
                            dtP.Rows[ri][2] = LngTB.Closing_Stock;
                            dtP.Rows[ri][3] = CStk.ToString(Variables.DecString);
                            dtP.Rows[ri][7] = "RB";
                            ri++;
                        }
                        else
                        {
                            ri++;
                            dtP.Rows.Add(null, null, LngTB.Closing_Stock, CStk.ToString(Variables.DecString), null, null, null, "RB");
                        }
                    }

                    Credit += CStk;

                    foreach (DataRow drs in dts.Rows)
                    {
                        if (drs["STOCK"] != DBNull.Value)
                        {
                            ri++;
                            dtP.Rows.Add(null, null, " " + drs["ACCNAME"].ToString(), Convert.ToDecimal(drs["STOCK"]).ToString(Variables.DecString) + " ", null);
                        }
                    }

                    if (SalDif > 0)
                    {
                        if (ri >= li)
                        {
                            dtP.Rows.Add(null, null, " " + LngTB.Delivery_Note_To_Prepare, "(-)" + SalDif.ToString(Variables.DecString) + " ", 0 /*dr["AccId"]*/);
                            ri++;
                        }
                        else
                        {
                            dtP.Rows[ri][2] = " " + LngTB.Delivery_Note_To_Prepare;
                            dtP.Rows[ri][3] = "(-)" + SalDif.ToString(Variables.DecString) + " ";
                            ri++;
                        }
                    }
                    else if (SalDif < 0)
                    {
                        if (ri >= li)
                        {
                            dtP.Rows.Add(null, null, " " + LngTB.Sales_Bills_To_Prepare, (-1 * SalDif).ToString(Variables.DecString) + " ", 0/* dr["AccId"]*/);

                        }
                        else
                        {
                            dtP.Rows[ri][2] = " " + LngTB.Sales_Bills_To_Prepare;
                            dtP.Rows[ri][3] = (-1 * SalDif).ToString(Variables.DecString) + " ";
                        }
                        ri++;
                    }
                }
                else
                {
                    li++;
                    dtP.Rows.Add(LngTB.Closing_Stock, (-1 * CStk).ToString(Variables.DecString), null, null, null, null, "LB");
                    Debit += -1 * CStk;
                    foreach (DataRow drs in dts.Rows)
                    {
                        if (Convert.ToDecimal(drs["STOCK"]) > 0)
                        {
                            li++;
                            dtP.Rows.Add("" + drs["ACCNAME"].ToString(), Convert.ToDecimal(drs["STOCK"]).ToString(Variables.DecString) + " ", null, null, null);
                        }
                        else
                        {
                            li++;
                            dtP.Rows.Add("" + drs["ACCNAME"].ToString(), (-1 * Convert.ToDecimal(drs["STOCK"])).ToString(Variables.DecString) + " ", null, null, null);

                        }
                    }

                    if (SalDif > 0)
                    {
                        if (ri >= li)
                        {
                            dtP.Rows.Add(" " + LngTB.Delivery_Note_To_Prepare, "(-)" + SalDif.ToString(Variables.DecString) + " ", null, null, 0 /*dr["AccId"]*/);

                        }
                        else
                        {
                            dtP.Rows[ri][2] = " " + LngTB.Delivery_Note_To_Prepare;
                            dtP.Rows[ri][3] = "(-)" + SalDif.ToString(Variables.DecString) + " ";
                        }
                        ri++;
                    }
                    else if (SalDif < 0)
                    {
                        if (ri >= li)
                        {
                            dtP.Rows.Add(null, null, " " + LngTB.Sales_Bills_To_Prepare, (-1 * SalDif).ToString(Variables.DecString) + " ", 0/* dr["AccId"]*/);

                        }
                        else
                        {
                            dtP.Rows[ri][2] = " " + LngTB.Sales_Bills_To_Prepare;
                            dtP.Rows[ri][3] = (-1 * SalDif).ToString(Variables.DecString) + " ";
                        }
                        ri++;
                    }
                }
            }
            /******************************TRN-NOTE DIFF ENDS*****************************/

            /****************************Previous Report*****************************/
            if (FOrder == "T")
            {
                PrevProf = 0;
            }
            else if (FOrder == "P")
            {
                PrevProf = report.GrossProfit;
            }
            else if (FOrder == "B")
            {
                PrevProf = await dayBookDB.NetProfit("M", Unapproved: request.IncludeUnapproved);
                PrevProf += await dayBookDB.NetProfit("T", Unapproved: request.IncludeUnapproved);

                decimal PrevPurDiff = await stkDtDB.GetPurDiffByFOrderAndDate("T", clsDate, Unapproved: request.IncludeUnapproved);
                PrevProf = PrevProf + PrevPurDiff;
                PrevProf += await dayBookDB.NetProfit("P", Unapproved: request.IncludeUnapproved);
                PrevProf += CStk;
                PrevProf -= OpenStock;

                if (request.FromDate.Date == Variables.fyStartDate.Date)
                {
                    clsOpnDir.TYPENAME = "PIRN";
                    PrevProf -= await opnDirDB.GetOpeningByTypeName(clsOpnDir);
                }
            }

            if (FOrder != "M")
            {
                if (FOrder == "B")
                {
                    report.PL_Amount = PrevProf;
                    if (PrevProf > 0)
                    {
                        Debit += PrevProf;
                    }
                    else if (PrevProf < 0)
                    {
                        Credit += (-1 * PrevProf);
                    }
                }
                else
                {
                    if (PrevProf > 0)
                    {
                        if (ri >= li)
                        {
                            dtP.Rows.Add(null, null, LngTB.Gross_Profit_Bd, PrevProf.ToString(Variables.DecString), null, null, null, "RB");
                        }
                        else
                        {
                            dtP.Rows[ri][2] = LngTB.Gross_Profit_Bd;
                            dtP.Rows[ri][3] = PrevProf.ToString(Variables.DecString);
                            dtP.Rows[ri][7] = "RB";
                        }

                        ri++;
                        Credit += PrevProf;
                    }
                    else if (PrevProf < 0)
                    {
                        if (li >= ri)
                        {
                            dtP.Rows.Add(LngTB.Gross_Loss_Bd, (-1 * PrevProf).ToString(Variables.DecString), null, null, null, null, "LB");
                        }
                        else
                        {
                            dtP.Rows[li][0] = LngTB.Gross_Loss_Bd;
                            dtP.Rows[li][1] = (-1 * PrevProf).ToString(Variables.DecString);
                            dtP.Rows[li][6] = "LB";
                        }

                        li++;

                        Debit += -1 * PrevProf;
                    }
                }
            }

            if (FOrder == "B")
            {
                dtP.Rows.Add();
                ri++;
            }
            /***********************Prev Report ENDS*************************/

            clsAccMas.Id = Variables.CashCD;
            DataTable dtAcc = await accMasDB.SearchByID(clsAccMas, DefBranch: false);
            int CashGrpId = 0;
            decimal Cashbal = 0;
            if (dtAcc.Rows.Count > 0)
            {
                CashGrpId = Convert.ToInt32(dtAcc.Rows[0]["MGROUP"]);
            }
            clsDate.DateFrom = request.FromDate.Date.ToString(Variables.format);
            clsDate.ToDate = request.ToDate.Date.ToString(Variables.format);
            do
            {
                dt = await dayBookDB.PL(FOrder, Unapproved: request.IncludeUnapproved);
            }
            while (dt == null || dt.Columns.Count == 0);

            if (FOrder == "B")
            {
                try
                {
                    DataTable dtLG = await grpLdgDB.GetAll();
                    DataTable dtLC = dt.Copy();
                    dtLC.Columns.Add("MG");
                    DataTable dtR = dtLC.Clone();

                    int c1 = 0;
                    foreach (DataRow item in dtLC.Rows)
                    {
                        string GroupID = item["ACCID"].ToString();
                        item["MG"] = GroupID;
                        if (c1 == 0)
                        {
                            dtR.Rows.Add(item.ItemArray);
                        }
                        else
                        {
                            DataRow[] drLG = dtLG.Select("GROUPID = '" + GroupID + "'");
                            if (drLG.Length > 0)
                            {
                                int c2 = 0;
                                foreach (DataRow item2 in dtR.Rows)
                                {
                                    if (item2["ACCID"].ToString() == drLG[0]["MGROUP"].ToString())
                                    {
                                        item["MG"] = drLG[0]["MGROUP"].ToString();
                                        break;
                                    }
                                    c2++;
                                }

                                if (dtR.Rows.Count == c2)
                                {
                                    dtR.Rows.Add(item.ItemArray);
                                }
                                else
                                {
                                    int LR_MG = c2 + 1;
                                    if (dtR.Select("MG = '" + item["MG"].ToString() + "'").Length > 1)
                                    {
                                        for (int i = dtR.Rows.Count - 1; i >= 0; i--)
                                        {
                                            if (dtR.Rows[i]["MG"].ToString() == item["MG"].ToString())
                                            {
                                                LR_MG = i + 1;
                                                break;
                                            }
                                        }
                                    }

                                    DataRow newRow = dtR.NewRow();
                                    newRow.ItemArray = item.ItemArray;
                                    dtR.Rows.InsertAt(newRow, LR_MG);
                                }
                            }
                            else
                            {
                                dtR.Rows.Add(item.ItemArray);
                            }
                        }
                        c1++;
                    }

                    dtR.Columns.Remove("MG");
                    dt = dtR.Copy();
                }
                catch { }
            }

            decimal PurDif_BS = 0;
            try
            {
                if (FOrder == "B")
                {
                    DataTable dt_tr = new DataTable();
                    do
                    {
                        dt_tr = await dayBookDB.PL("T", Unapproved: request.IncludeUnapproved);
                    }
                    while (dt_tr == null || dt_tr.Columns.Count == 0);

                    foreach (DataRow dr_tr in dt_tr.Rows)
                    {
                        PurDif_BS += await stkDtDB.GetPurDiffByGroupAndDate(Convert.ToInt32(dr_tr["AccId"]), clsDate, Unapproved: request.IncludeUnapproved);
                    }

                    if (request.FromDate.Date == Variables.fyStartDate.Date)
                    {
                        clsOpnDir.TYPENAME = "PIRN";
                        PurDif_BS -= await opnDirDB.GetOpeningByTypeName(clsOpnDir);
                    }
                }
            }
            catch { }

            int PurAccGrpCD = await accMasDB.GetGroupByPurAcc();
            bool isExistsPurAccGrpCD = false;
            try
            {
                isExistsPurAccGrpCD = dt.Select("AccId = '" + PurAccGrpCD + "'").Length > 0;
            }
            catch { }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                decimal PurDif = 0;
                PurDif = await stkDtDB.GetPurDiffByGroupAndDate(Convert.ToInt32(dr["AccId"]), clsDate, Unapproved: request.IncludeUnapproved);

                if (FOrder == "B" && PurDif_BS != 0 && Convert.ToInt32(dr["AccId"]) == 27)
                {
                    PurDif += PurDif_BS;
                }

                if (((i == 0 && !isExistsPurAccGrpCD) || (isExistsPurAccGrpCD && Convert.ToInt32(dr["AccId"]) == PurAccGrpCD)) && FOrder != "B" && (request.ReportType != "P" || (request.ReportType == "P" && FOrder == "T")) && request.FromDate.Date == Variables.fyStartDate.Date)
                {
                    clsOpnDir.TYPENAME = "PIRN";
                    PurDif -= await opnDirDB.GetOpeningByTypeName(clsOpnDir);
                }

                report.PIRN_Amount -= PurDif;
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
                    if (ri >= li)
                    {
                        dtP.Rows.Add(null, null, dr["AccName"], (Convert.ToDecimal(dr["Balance"]) + PurDif).ToString(Variables.DecString), dr["AccId"], null, null, "RB");
                    }
                    else
                    {
                        dtP.Rows[ri][2] = dr["AccName"];
                        dtP.Rows[ri][3] = (Convert.ToDecimal(dr["Balance"]) + PurDif).ToString(Variables.DecString);
                        dtP.Rows[ri][7] = "RB";
                    }
                    ri++;

                    if (PurDif != 0 && !request.DetailedReport)
                    {
                        if (ri >= li)
                        {
                            dtP.Rows.Add(null, null, "  " + dr["AccName"].ToString(), (Convert.ToDecimal(dr["Balance"])).ToString(Variables.DecString) + " ", dr["AccId"]);
                        }
                        else
                        {
                            dtP.Rows[ri][2] = dr["AccName"].ToString();
                            dtP.Rows[ri][3] = (Convert.ToDecimal(dr["Balance"])).ToString(Variables.DecString) + " ";
                        }
                        ri++;
                    }

                    if (PurDif > 0)
                    {
                        if (ri >= li)
                        {
                            dtP.Rows.Add(null, null, "  " + LngTB.Purchase_Bills_To_Receive, (PurDif).ToString(Variables.DecString) + " ", dr["AccId"]);
                        }
                        else
                        {
                            dtP.Rows[ri][2] = "  " + LngTB.Purchase_Bills_To_Receive;
                            dtP.Rows[ri][3] = (PurDif).ToString(Variables.DecString) + " ";
                        }

                        ri++;
                    }
                    else if (PurDif < 0)
                    {
                        if (ri >= li)
                        {
                            dtP.Rows.Add(null, null, "  " + LngTB.Receipt_Note_To_Receive, "(-)" + (-1 * PurDif).ToString(Variables.DecString) + " ", dr["AccId"]);
                        }
                        else
                        {
                            dtP.Rows[ri][2] = " " + LngTB.Receipt_Note_To_Receive;
                            dtP.Rows[ri][3] = "(-)" + (-1 * PurDif).ToString(Variables.DecString) + " ";
                        }

                        ri++;
                    }

                    Credit += (Convert.ToDecimal(dr["Balance"]) + PurDif);
                    decimal balance = Convert.ToDecimal(dr["Balance"]);
                }
                else if (Convert.ToDecimal(dr["Balance"]) < 0)
                {
                    if (li >= ri)
                    {
                        dtP.Rows.Add(dr["AccName"], ((-1 * Convert.ToDecimal(dr["Balance"])) + PurDif).ToString(Variables.DecString), null, null, dr["AccId"], null, "LB");
                    }
                    else
                    {

                        dtP.Rows[li][0] = dr["AccName"];
                        dtP.Rows[li][1] = ((-1 * Convert.ToDecimal(dr["Balance"])) + PurDif).ToString(Variables.DecString);
                        dtP.Rows[li][6] = "LB";
                    }

                    li++;

                    if (PurDif != 0 && !request.DetailedReport)
                    {
                        if (li >= ri)
                        {
                            dtP.Rows.Add("  " + dr["AccName"].ToString(), (-1 * Convert.ToDecimal(dr["Balance"])).ToString(Variables.DecString) + " ", null, null, dr["AccId"]);
                        }
                        else
                        {
                            dtP.Rows[li][0] = "  " + dr["AccName"].ToString();
                            dtP.Rows[li][1] = (-1 * Convert.ToDecimal(dr["Balance"])).ToString(Variables.DecString) + " ";
                        }

                        li++;
                    }

                    if (PurDif > 0)
                    {
                        if (li >= ri)
                        {
                            dtP.Rows.Add("  " + LngTB.Purchase_Bills_To_Receive, PurDif.ToString(Variables.DecString) + " ", null, null, dr["AccId"]);
                        }
                        else
                        {
                            dtP.Rows[li][0] = "  " + LngTB.Purchase_Bills_To_Receive;
                            dtP.Rows[li][1] = PurDif.ToString(Variables.DecString) + " ";
                        }

                        li++;
                    }
                    else if (PurDif < 0)
                    {
                        if (li >= ri)
                        {
                            dtP.Rows.Add("  " + LngTB.Receipt_Note_To_Receive, "(-)" + (-1 * PurDif).ToString(Variables.DecString) + " ", null, null, dr["AccId"]);
                        }
                        else
                        {
                            dtP.Rows[li][0] = "  " + LngTB.Receipt_Note_To_Receive;
                            dtP.Rows[li][1] = "(-)" + (-1 * PurDif).ToString(Variables.DecString) + " ";
                        }

                        li++;
                    }

                    Debit += (-1 * Convert.ToDecimal(dr["Balance"])) + PurDif;
                }
                else if (Convert.ToDecimal(dr["Balance"]) == 0)
                {
                    if (PurDif != 0)
                    {
                        if (li >= ri)
                        {
                            dtP.Rows.Add(dr["AccName"], ((-1 * Convert.ToDecimal(dr["Balance"])) + PurDif).ToString(Variables.DecString), null, null, dr["AccId"], null, "LB");
                        }
                        else
                        {
                            dtP.Rows[li][0] = dr["AccName"];
                            dtP.Rows[li][1] = ((-1 * Convert.ToDecimal(dr["Balance"])) + PurDif).ToString(Variables.DecString);
                            dtP.Rows[li][6] = "LB";
                        }

                        li++;

                        if (PurDif != 0 && !request.DetailedReport)
                        {
                            if (li >= ri)
                            {
                                dtP.Rows.Add("  " + dr["AccName"].ToString(), (-1 * Convert.ToDecimal(dr["Balance"])).ToString(Variables.DecString) + " ", null, null, dr["AccId"]);
                            }
                            else
                            {
                                dtP.Rows[li][0] = "  " + dr["AccName"].ToString();
                                dtP.Rows[li][1] = (-1 * Convert.ToDecimal(dr["Balance"])).ToString(Variables.DecString) + " ";
                            }

                            li++;
                        }

                        if (PurDif > 0)
                        {
                            if (li >= ri)
                            {
                                dtP.Rows.Add("  " + LngTB.Purchase_Bills_To_Receive, PurDif.ToString(Variables.DecString) + " ", null, null, dr["AccId"]);
                            }
                            else
                            {
                                dtP.Rows[li][0] = "  " + LngTB.Purchase_Bills_To_Receive;
                                dtP.Rows[li][1] = PurDif.ToString(Variables.DecString) + " ";
                            }

                            li++;
                        }
                        else if (PurDif < 0)
                        {
                            if (li >= ri)
                            {
                                dtP.Rows.Add("  " + LngTB.Receipt_Note_To_Receive, "(-)" + (-1 * PurDif).ToString(Variables.DecString) + " ", null, null, dr["AccId"]);
                            }
                            else
                            {
                                dtP.Rows[li][0] = "  " + LngTB.Receipt_Note_To_Receive;
                                dtP.Rows[li][1] = "(-)" + (-1 * PurDif).ToString(Variables.DecString) + " ";
                            }

                            li++;
                        }

                        Debit += (-1 * Convert.ToDecimal(dr["Balance"])) + PurDif;
                    }
                }

                /**********************Detailed Report*********************/
                if (request.DetailedReport)
                {

                    if (request.FromDate.Date > Variables.fyStartDate.Date)
                        await dayBookDB.CreateTable(Variables.fyStartDate.ToString(Variables.format), request.FromDate.Date.AddDays(-1).ToString(Variables.format), "OPI", Unapproved: request.IncludeUnapproved);
                    else
                        await dayBookDB.CreateTable(Variables.fyStartDate.AddDays(-1).ToString(Variables.format), request.FromDate.Date.AddDays(-1).ToString(Variables.format), "OPI", Unapproved: request.IncludeUnapproved);

                    if (FOrder == "B")
                        await dayBookDB.CreateTable(Variables.fyStartDate.ToString(Variables.format), request.ToDate.Date.ToString(Variables.format), "DBI", Unapproved: request.IncludeUnapproved);
                    else
                        await dayBookDB.CreateTable(request.FromDate.Date.ToString(Variables.format), request.ToDate.Date.ToString(Variables.format), "DBI", Unapproved: request.IncludeUnapproved);

                    clsAccMas.LedgerGroupId = Convert.ToInt32(dr["AccId"]);
                    DataTable dtA = await accMasDB.AccountsByGroupRpt(clsAccMas, request.IncludeUnapproved);
                    foreach (DataRow drA in dtA.Rows)
                    {
                        decimal BAL = 0;
                        if (FOrder == "B")
                            BAL = Convert.ToDecimal(drA["OPENBAL"]);

                        BAL += await dayBookDB.LedgerCurBalance(Convert.ToInt32(drA["ID"]));

                        if (Convert.ToInt32(drA["ID"]) == Variables.CashCD)
                        {
                            DataTable dtCash = new DataTable();
                            if (FOrder == "B")
                                dtCash = await dayBookDB.CashTotalNew();
                            else
                                dtCash = await dayBookDB.CashTotal();

                            if (dtCash.Rows.Count > 0)
                            {
                                BAL = Convert.ToDecimal(dtCash.Rows[0]["Balance"]) - Convert.ToDecimal(dtCash.Rows[0]["OpenBal"]);
                            }
                        }

                        if (BAL != 0)
                        {
                            if (Convert.ToDecimal(dr["Balance"]) > 0)
                            {
                                if (BAL > 0)
                                {
                                    if (FOrder == "B")
                                        BAL = -1 * BAL;

                                    if (ri >= li)
                                    {
                                        dtP.Rows.Add(null, null, "  " + drA["ACCNAME"].ToString(), BAL.ToString(Variables.DecString), drA["ID"]);
                                    }
                                    else
                                    {
                                        dtP.Rows[ri][2] = "  " + drA["ACCNAME"].ToString();
                                        dtP.Rows[ri][3] = BAL.ToString(Variables.DecString);
                                    }

                                    ri++;
                                }
                                else
                                {
                                    if (FOrder == "B")
                                        BAL = -1 * BAL;

                                    if (ri >= li)
                                    {
                                        dtP.Rows.Add(null, null, "  " + drA["ACCNAME"].ToString(), (BAL).ToString(Variables.DecString), drA["ID"]);
                                    }
                                    else
                                    {
                                        dtP.Rows[ri][2] = "  " + drA["ACCNAME"].ToString();
                                        dtP.Rows[ri][3] = (BAL).ToString(Variables.DecString);
                                    }

                                    ri++;
                                }
                            }
                            else if (Convert.ToDecimal(dr["Balance"]) < 0)
                            {
                                if (FOrder != "B")
                                    BAL = -1 * BAL;

                                if (BAL > 0)
                                {

                                    if (li >= ri)
                                    {
                                        dtP.Rows.Add("" + drA["ACCNAME"].ToString(), (BAL).ToString(Variables.DecString), null, null, drA["ID"]);
                                    }
                                    else
                                    {

                                        dtP.Rows[li][0] = "  " + drA["ACCNAME"].ToString();
                                        dtP.Rows[li][1] = (BAL).ToString(Variables.DecString);
                                    }

                                    li++;
                                }
                                else
                                {
                                    if (li >= ri)
                                    {
                                        dtP.Rows.Add("" + drA["ACCNAME"].ToString(), (BAL).ToString(Variables.DecString), null, null, drA["ID"]);
                                    }
                                    else
                                    {
                                        dtP.Rows[li][0] = "  " + drA["ACCNAME"].ToString();
                                        dtP.Rows[li][1] = (BAL).ToString(Variables.DecString);
                                    }

                                    li++;
                                }
                            }
                        }
                    }

                    await dayBookDB.DropTable();
                    dayBookDB.tableName = tablename;
                    dayBookDB.OpTable = opTableName;
                }
                /************************Detailed Report ENDS**************************/
            }

            await prMastDB.DropTable();
            await stkDtDB.DropTBForPL();

            Balance = Credit - Debit;
            if (request.ReportType != "B")
            {
                if (FOrder == "P")
                {
                    report.PL_Amount = Balance;
                    if (Balance >= 0)
                    {
                        dtP.Rows.Add(LngTB.Net_Profit, Balance.ToString(Variables.DecString), null, null, null, null, "LB");
                        Debit += Balance;
                    }
                    else
                    {
                        dtP.Rows.Add(null, null, LngTB.Net_Loss, -1 * Balance, null, null, null, "RB");
                        Credit += (-1 * Balance);
                    }

                    ri++;
                    li++;
                }
                else
                {
                    if (Balance >= 0)
                    {
                        dtP.Rows.Add(LngTB.Gross_Profit, Balance.ToString(Variables.DecString), null, null, null, null, "LB");
                        Debit += Balance;
                    }
                    else
                    {
                        dtP.Rows.Add(null, null, LngTB.Gross_Loss, -1 * Balance, null, null, null, "RB");
                        Credit += (-1 * Balance);
                    }

                    ri++;
                    li++;
                }
            }
            else
            {
                if (PrevProf > 0)
                {
                    dtP.Rows.Add(LngTB.Net_Profit, (PrevProf).ToString(Variables.DecString), null, null, null, null, "LB");

                    li++;
                    ri++;
                }
                else if (PrevProf < 0)
                {
                    dtP.Rows.Add(null, null, LngTB.Net_Loss, (-1 * PrevProf).ToString(Variables.DecString), null, null, null, "RB");

                    ri++;
                    li++;
                }

                if (Balance >= 0)
                {
                    dtP.Rows.Add(LngTB.Trial_Difference, Balance.ToString(Variables.DecString), null, null, null, null, "LB");
                    Debit += Balance;
                }
                else
                {
                    dtP.Rows.Add(null, null, LngTB.Trial_Difference, -1 * Balance, null, null, null, "RB");
                    Credit += (-1 * Balance);
                }

                ri++;
                li++;
            }

            ri++;
            li++;

            dtP.Rows.Add(LngTB.Total.ToUpper(), Debit.ToString(Variables.DecString), LngTB.Total.ToUpper(), Credit.ToString(Variables.DecString), null, 'B');
            report.GrossProfit = Balance;
            await dayBookDB.DropTable();
        }

        private async Task<decimal> StockValueNew(DateTime dtFrom, DateTime DtTo, ProfitAndLossRequest request, ProfitAndLossContext report)
        {
            Variable Variables = report.Variables;

            ClsDate clsDate = report.clsDate;
            ClsStkDt clsStkDt = report.clsStkDt;

            PrMastDB prMastDB = report.prMastDB;
            StkDtDB stkDtDB = report.stkDtDB;

            decimal CStk = 0;
            if (dtFrom < Variables.fyStartDate)
            {
                CStk = await prMastDB.OpeningStock_PL(request.IncludeUnapproved);
            }
            else
            {
                clsDate.DateFrom = dtFrom.ToString(Variables.format);
                clsDate.ToDate = DtTo.ToString(Variables.format);
                await stkDtDB.UpdateConvFact();
                await prMastDB.CreateForClosingStock(clsDate, false, PLReport: true, Unapproved: request.IncludeUnapproved);
                await prMastDB.insertToCSTK(clsDate, false, false, Unapproved: request.IncludeUnapproved);
                DataTable dtls = await prMastDB.ClosingStock_PL();
                if (dtls.Rows.Count > 0)
                {
                    DataRow[] drls = dtls.Select("DS = 'Total'");
                    if (drls.Length > 0)
                    {
                        CStk = drls[0]["Tvalue"].ToString() == "" ? 0 : Convert.ToDecimal(drls[0]["Tvalue"]);
                    }
                }

                await stkDtDB.CreateTBForPL(clsStkDt, clsDate, Unapproved: request.IncludeUnapproved);
                CStk += (-1 * await GetSalDiff("T", request, report));
                await stkDtDB.DropTBForPL();
                await prMastDB.DropTable();
            }
            return CStk;
        }

        private async Task<decimal> GetSalDiff(string FOrder, ProfitAndLossRequest request, ProfitAndLossContext report)
        {
            Variable Variables = report.Variables;

            ClsDate clsDate = report.clsDate;
            ClsPrMast clsPrMast = report.clsPrMast;
            ClsStkDt clsStkDt = report.clsStkDt;

            PrMastDB prMastDB = report.prMastDB;
            StkDtDB stkDtDB = report.stkDtDB;

            DataTable dtPendingSales = await prMastDB.GetPendingSales(clsDate, Unapproved: request.IncludeUnapproved);
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
                DataTable dtRec = new DataTable();
                DataTable dtStock = new DataTable();
                if (CostMeth == 1 || CostMeth == 6)
                    dtStock = await prMastDB.GetStockByPrCode(clsPrMast);
                else if (CostMeth == 4 || CostMeth == 5)
                    dtRec = await prMastDB.ReceiptNotesByPrCode(clsPrMast);

                decimal _PRRATE = 0;
                if (CostMeth == 1 || CostMeth == 6)
                {
                    report.RecFlag = true;

                    if (dtStock.Rows.Count > 0 && dtStock.Rows[0]["AVGGRNRATE"].ToString() != "")
                    {
                        SalDif += QtyDiff * Convert.ToDecimal(dtStock.Rows[0]["AVGGRNRATE"]);
                        _PRRATE = Convert.ToDecimal(dtStock.Rows[0]["AVGGRNRATE"]);
                    }
                }
                else if (CostMeth == 2)
                {
                    if (dtPendingSales.Rows.Count > 0)
                    {
                        DataRow[] drPS = dtPendingSales.Select("PRCODE='" + clsPrMast.PrCode + "'");
                        foreach (DataRow itemPS in drPS)
                        {
                            decimal Qt = -1 * (Convert.ToDecimal(itemPS["CONVQTY"].ToString() == "" ? "0" : itemPS["CONVQTY"].ToString()) > 0 ? Convert.ToDecimal(itemPS["CONVQTY"].ToString()) : Convert.ToDecimal(itemPS["QTY"].ToString() == "" ? "0" : itemPS["QTY"].ToString()));
                            decimal Rt = Convert.ToDecimal(itemPS["PRRATE"].ToString() == "" ? "0" : itemPS["PRRATE"].ToString());
                            if (Qt * Rt != 0 && QtyDiff < 0)
                            {
                                if (Math.Abs(Qt) > Math.Abs(QtyDiff))
                                {
                                    Qt = QtyDiff;
                                }

                                QtyDiff -= Qt;
                                SalDif += (Qt * Rt);
                            }
                        }
                    }

                    DataTable dtW = await prMastDB.TransactionsWithSalesByPrCode(clsPrMast, clsDate, Unapproved: request.IncludeUnapproved);
                    if (dtW.Rows.Count == 0)
                        report.RecFlag = true;

                    decimal Stock = 0, StockValue = 0, Rate = 0;
                    decimal RTot = 0, ITot = 0, Stot = 0;
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
                            if (drW["VRTYPE"] != DBNull.Value && drW["VRTYPE"].ToString() != "RET" && (drW["VRTYPE"].ToString() != "ST" || (Variables.StkIssRec && report.dtBrTrVr.Select("VRNO='" + Convert.ToInt32(drW["VRNO"].ToString() == "" ? "0" : drW["VRNO"].ToString()) + "'").Length > 0)))
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
                                Rate = Math.Round(StockValue / Stock, Variables.DecimalPoint);
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
                            }
                            ITot += IQty;
                            Stot += IQty * TRate;
                        }
                    }

                    if (RTot == 0 && ITot != 0 && Rate == 0)
                    {
                        DataTable dtpr = await prMastDB.SearchByCode(clsPrMast);
                        if (dtpr.Rows.Count > 0)
                        {
                            Rate = Convert.ToDecimal(dtpr.Rows[0]["BASEPURPRICE"].ToString() == "" ? "0" : dtpr.Rows[0]["BASEPURPRICE"].ToString());
                            if (Rate == 0)
                                Rate = Math.Round(Stot / ITot, Variables.DecimalPoint);
                        }
                    }

                    SalDif += (QtyDiff * Rate);
                    _PRRATE = Rate;

                }
                else if (CostMeth == 3)
                {
                    report.RecFlag = true;
                    SalDif = PurPrice * QtyDiff;
                }
                else if (CostMeth == 4)/*FIFO Method*/
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