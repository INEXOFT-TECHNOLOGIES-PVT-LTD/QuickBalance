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

    [Route("api/v1/reports/trial-balance")]
    [Authorize]
    public class TrialBalanceController : ControllerBase
    {
        private readonly DatabaseConnection _databaseConnection;
        public TrialBalanceController(DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }
        [HttpPost]
        public async Task<IActionResult> GetTrialBalance(TrialBalanceRequest request)
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

                CostCMasDB costCMasDB = new CostCMasDB(DBConn);
                costCMasDB.Variables = Variables;
                costCMasDB.Functions.Variables = Variables;

                OpnDirDB opnDirDB = new OpnDirDB(DBConn);
                opnDirDB.Variables = Variables;
                opnDirDB.Functions.Variables = Variables;
                ClsOpnDir clsOpnDir = new ClsOpnDir();

                GrpLdgDB grpLdgDB = new GrpLdgDB(DBConn);

                ClsTrialBal clsTrialBal = new ClsTrialBal();
                Function Functions = new Function();
                Functions.Variables = Variables;

                //-- Get Report

                bool OpeningSummary = false;
                ManageOpeningSummary(request, OpeningSummary, true);

                DataTable dtls = new DataTable();
                DataTable dtCC_OpenBal = new DataTable();

                DataTable dtP = new DataTable();
                dtP.Columns.Add("AccId");
                dtP.Columns.Add("AccName");
                dtP.Columns.Add("OpenBal");
                dtP.Columns.Add("OpCredit");
                dtP.Columns.Add("OpDebit");
                dtP.Columns.Add("Debit");
                dtP.Columns.Add("Credit");
                dtP.Columns.Add("Balance");
                dtP.Columns.Add("GroupId");
                dtP.Columns.Add("OrderNo");
                dtP.Columns.Add("DETFLG");
                dtP.Columns.Add("OPNFLG");
                dtP.Columns.Add("Type");
                dtP.Columns.Add("CCredit");
                dtP.Columns.Add("CDebit");

                clsTrialBal.FromDate = request.FromDate;
                clsTrialBal.ToDate = request.ToDate;
                clsTrialBal.Alphabetic = request.Alphabetical;
                clsTrialBal.Detailed = request.Detailed;
                clsTrialBal.WithoutBal = request.WithoutOpenBalance;
                if (request.From != null && request.From == "CT")
                {
                    await dayBookDB.CreateTable_CS();
                    DataTable dt = new DataTable();

                    if (request.FromDate.Date > Variables.fyStartDate)
                        await dayBookDB.CreateTable(Variables.fyStartDate.ToString(Variables.format), request.FromDate.Date.AddDays(-1).ToString(Variables.format), "OP", CostCenter: true, Unapproved: request.IncludeUnapproved);
                    else
                        await dayBookDB.CreateTable(request.FromDate.Date.AddDays(-1).ToString(Variables.format), request.FromDate.Date.AddDays(-1).ToString(Variables.format), "OP", CostCenter: true, Unapproved: request.IncludeUnapproved);

                    await dayBookDB.CreateTable(request.FromDate.Date.ToString(Variables.format), request.ToDate.Date.ToString(Variables.format), "DB", CostCenter: true, Unapproved: request.IncludeUnapproved);

                    int Variables_CashCD = Variables.CashCD;
                    Variables.CashCD = 0;
                    try
                    {
                        if (!request.Detailed)
                        {
                            dt = await dayBookDB.CostCentreTrialBalance(!request.WithoutOpenBalance);
                        }
                        else
                        {
                            if (request.Alphabetical)
                                dt = await dayBookDB.CostCentreTrialBalanceDtls(!request.WithoutOpenBalance);
                            else
                                dt = await dayBookDB.CostCentreTrialBalanceDtlsGrp(!request.WithoutOpenBalance);
                        }
                        dtCC_OpenBal = await costCMasDB.GetOpening();
                    }
                    catch { }
                    Variables.CashCD = Variables_CashCD;

                    if (request.Detailed == true)
                    {
                        DataRow dr1 = dt.Rows[dt.Rows.Count - 1];
                        dt.Rows.Add(0, LngTB.Trial_Difference, null, null, null, dr1["Balance"], "B");
                    }

                    if (dt != null)
                    {

                        foreach (DataRow drA in dt.Rows)
                        {
                            decimal cb = 0;
                            decimal op = 0;

                            if (drA["Balance"] != DBNull.Value && !string.IsNullOrEmpty(drA["Balance"].ToString()))
                                cb = Convert.ToDecimal(drA["Balance"]);

                            if (drA["OpenBal"] != DBNull.Value && !string.IsNullOrEmpty(drA["OpenBal"].ToString()))
                                op = Convert.ToDecimal(drA["OpenBal"]);

                            if (cb > 0)
                            {
                                if (op > 0)
                                    dtP.Rows.Add(drA["AccId"], drA["AccName"], op.ToString(Variables.DecString) + " Cr", "", "", drA["Debit"], drA["Credit"], cb.ToString(Variables.DecString) + " Cr", null, null, null, null, drA["Type"]);
                                else if (op < 0)
                                    dtP.Rows.Add(drA["AccId"], drA["AccName"], (-1 * op).ToString(Variables.DecString) + " Dr", "", "", drA["Debit"], drA["Credit"], cb.ToString(Variables.DecString) + " Cr", null, null, null, null, drA["Type"]);
                                else
                                    dtP.Rows.Add(drA["AccId"], drA["AccName"], null, "", "", drA["Debit"], drA["Credit"], cb.ToString(Variables.DecString) + " Cr", null, null, null, null, drA["Type"]);
                            }
                            else if (cb < 0)
                            {
                                if (op < 0)
                                    dtP.Rows.Add(drA["AccId"], drA["AccName"], (-1 * op).ToString(Variables.DecString) + " Dr", "", "", drA["Debit"], drA["Credit"], (-1 * cb).ToString(Variables.DecString) + " Dr", null, null, null, null, drA["Type"]);
                                else if (op > 0)
                                    dtP.Rows.Add(drA["AccId"], drA["AccName"], op.ToString(Variables.DecString) + " Cr", "", "", drA["Debit"], drA["Credit"], (-1 * cb).ToString(Variables.DecString) + " Dr", null, null, null, null, drA["Type"]);
                                else
                                    dtP.Rows.Add(drA["AccId"], drA["AccName"], null, "", "", drA["Debit"], drA["Credit"], (-1 * cb).ToString(Variables.DecString) + " Dr", null, null, null, null, drA["Type"]);
                            }

                            else
                            {
                                if (op > 0)
                                    dtP.Rows.Add(drA["AccId"], drA["AccName"], op.ToString(Variables.DecString) + " Cr", "", "", drA["Debit"], drA["Credit"], null, null, null, null, null, drA["Type"]);
                                else if (op < 0)
                                    dtP.Rows.Add(drA["AccId"], drA["AccName"], (-1 * op).ToString(Variables.DecString) + " Dr", "", "", drA["Debit"], drA["Credit"], null, null, null, null, null, drA["Type"]);
                                else
                                    dtP.Rows.Add(drA["AccId"], drA["AccName"], null, "", "", drA["Debit"], drA["Credit"], null, null, null, null, null, drA["Type"]);
                            }
                        }

                        await dayBookDB.DropTable();
                        await dayBookDB.DropTable_CS();
                    }
                }
                else
                {
                    string selGrp = request.Scheduled && !request.AllGroup && request.GroupCode > 0 ? request.GroupCode.ToString() : "";
                    DataTable dt = new DataTable();
                    if (request.FromDate.Date > Variables.fyStartDate.Date)
                        await dayBookDB.CreateTable(Variables.fyStartDate.ToString(Variables.format), request.FromDate.Date.AddDays(-1).ToString(Variables.format), "OP", Unapproved: request.IncludeUnapproved);
                    else
                        await dayBookDB.CreateTable(Variables.fyStartDate.AddDays(-1).ToString(Variables.format), request.FromDate.Date.AddDays(-1).ToString(Variables.format), "OP", Unapproved: request.IncludeUnapproved);

                    await dayBookDB.CreateTable(request.FromDate.Date.ToString(Variables.format), request.ToDate.Date.ToString(Variables.format), "DB", Unapproved: request.IncludeUnapproved);

                    if (request.Scheduled == false)
                    {
                        if (request.WithoutOpenBalance == true)
                            dt = await dayBookDB.TrialBalance(false, request.Detailed, request.IncludeUnapproved);
                        else
                            dt = await dayBookDB.TrialBalance(true, request.Detailed, request.IncludeUnapproved);
                    }
                    else if (request.Scheduled == true)
                    {
                        if (request.WithoutOpenBalance == true)
                        {
                            DataTable dtR = await dayBookDB.TrialBalanceGrouped(false, request.Detailed, Unapproved: request.IncludeUnapproved);
                            if (dtR != null && dtR.Rows.Count > 0)
                                dt = dtR;
                            else if (dt.Columns.Count > 0)
                                dt.Rows.Clear();
                        }
                        else
                        {
                            dt = await dayBookDB.TrialBalanceGrouped(true, request.Detailed, Unapproved: request.IncludeUnapproved);
                        }

                        try
                        {
                            // Arrange details by ledger group by group 16_Feb_2024
                            DataTable dtOriginal = dt.Clone();
                            DataTable dtGrp = await grpLdgDB.AccGroupSeries(request.IncludeUnapproved);
                            string idCol = "", nameCol = "";

                            // < Filter selected Group Dtls / 12_May_2025
                            if (selGrp != "")
                            {
                                for (int i = 1; i <= dtGrp.Columns.Count / 2; i++)
                                {
                                    DataRow[] drGroup = dtGrp.Select("GRPID" + i + " = '" + selGrp + "'");
                                    if (drGroup.Length > 0)
                                    {
                                        dtGrp = drGroup.CopyToDataTable();
                                        if (i != 1)
                                        {
                                            for (int j = i - 1; j > 0; j--)
                                            {
                                                dtGrp.Columns.Remove("GRPID" + j);
                                                dtGrp.Columns.Remove("GRPNAME" + j);
                                            }

                                            int l = 1;
                                            for (int k = 1; k < dtGrp.Columns.Count; k++)
                                            {

                                                if (k % 2 != 0)
                                                {
                                                    dtGrp.Columns[k].ColumnName = "GRPID" + l;

                                                }
                                                else
                                                {
                                                    dtGrp.Columns[k].ColumnName = "GRPNAME" + l;
                                                    l++;
                                                }
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                            // </ Filter selected Group Dtls >

                            List<string> ls = new List<string>();
                            foreach (DataRow item in dtGrp.Rows)
                            {
                                for (int i = 1; i <= dtGrp.Columns.Count / 2; i++)
                                {
                                    idCol = "GRPID" + i;
                                    nameCol = "GRPNAME" + i;
                                    int GrpID = Convert.ToInt32(item[idCol].ToString() == "" ? "0" : item[idCol].ToString());
                                    string GrpName = item[nameCol].ToString();
                                    if (GrpID > 0 && !ls.Contains(GrpID.ToString()))
                                    {
                                        ls.Add(GrpID.ToString());
                                        DataRow[] drSubGrp = dtGrp.Select(idCol + "='" + GrpID + "'");
                                        if (drSubGrp.Length > 0)
                                        {
                                            DataTable dtSubGrp = drSubGrp.CopyToDataTable();
                                            string GrpIds = "";
                                            foreach (DataRow item2 in dtSubGrp.Rows)
                                            {
                                                for (int j = i; j <= dtGrp.Columns.Count / 2; j++)
                                                {
                                                    GrpIds += (GrpIds == "" || item2["GRPID" + j].ToString() == "" ? "" : ",") + item2["GRPID" + j].ToString();
                                                }
                                            }
                                            if (GrpIds != "")
                                            {
                                                string Extra = "";
                                                string Extra2 = "";
                                                for (int k = 2; k <= i; k++)
                                                {
                                                    Extra += (Extra == "" ? " " : "") + ".";
                                                }
                                                if (Extra != "")
                                                {
                                                    Extra2 = Extra;
                                                    Extra += " ";
                                                }

                                                DataRow[] drAccRow = dt.Select("GroupId='" + GrpID + "'");
                                                DataTable dtls_1 = await dayBookDB.TrialBalanceSummary(GrpID, Extra + GrpName, GrpIds);
                                                if (drAccRow.Length > 0 || dtls_1.Rows.Count > 0)
                                                {
                                                    if (drAccRow.Length > 0)
                                                    {
                                                        DataTable dtAccRow = drAccRow.CopyToDataTable();

                                                        if (dtls_1.Rows[0]["OpenBal"].ToString() == dtAccRow.Rows[0]["OpenBal"].ToString() &&
                                                            dtls_1.Rows[0]["OpCredit"].ToString() == dtAccRow.Rows[0]["OpCredit"].ToString() &&
                                                            dtls_1.Rows[0]["OpDebit"].ToString() == dtAccRow.Rows[0]["OpDebit"].ToString() &&
                                                            dtls_1.Rows[0]["Debit"].ToString() == dtAccRow.Rows[0]["Debit"].ToString() &&
                                                            dtls_1.Rows[0]["Credit"].ToString() == dtAccRow.Rows[0]["Credit"].ToString() &&
                                                            dtls_1.Rows[0]["Balance"].ToString() == dtAccRow.Rows[0]["Balance"].ToString())
                                                        {
                                                            dtAccRow.Rows[0]["AccName"] = Extra + GrpName;
                                                            dtOriginal.Merge(dtAccRow, true, MissingSchemaAction.Ignore);
                                                        }
                                                        else
                                                        {
                                                            dtOriginal.Merge(dtls_1, true, MissingSchemaAction.Ignore);
                                                            dtAccRow.Rows[0]["AccName"] = Extra2 + (Extra2 == "" ? " . " : ". ") + GrpName;
                                                            dtOriginal.Merge(dtAccRow, true, MissingSchemaAction.Ignore);
                                                        }

                                                    }
                                                    else
                                                    {
                                                        if (Convert.ToDecimal(dtls_1.Rows[0]["OpenBal"].ToString() == "" ? "0" : dtls_1.Rows[0]["OpenBal"].ToString()) != 0 ||
                                                            Convert.ToDecimal(dtls_1.Rows[0]["OpCredit"].ToString() == "" ? "0" : dtls_1.Rows[0]["OpCredit"].ToString()) != 0 ||
                                                            Convert.ToDecimal(dtls_1.Rows[0]["OpDebit"].ToString() == "" ? "0" : dtls_1.Rows[0]["OpDebit"].ToString()) != 0 ||
                                                            Convert.ToDecimal(dtls_1.Rows[0]["Debit"].ToString() == "" ? "0" : dtls_1.Rows[0]["Debit"].ToString()) != 0 ||
                                                            Convert.ToDecimal(dtls_1.Rows[0]["Credit"].ToString() == "" ? "0" : dtls_1.Rows[0]["Credit"].ToString()) != 0 ||
                                                            Convert.ToDecimal(dtls_1.Rows[0]["Balance"].ToString() == "" ? "0" : dtls_1.Rows[0]["Balance"].ToString()) != 0)
                                                        {
                                                            dtOriginal.Merge(dtls_1, true, MissingSchemaAction.Ignore);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (dtOriginal.Rows.Count > 0 || selGrp != "")
                            {
                                DataRow drTotal = dt.Rows[dt.Rows.Count - 1];
                                dtOriginal.ImportRow(drTotal);
                                dt = dtOriginal;
                            }
                        }
                        catch { }
                    }

                    DataTable dtCash = await dayBookDB.CashTotal();
                    if (dtCash.Rows.Count > 0 && dt.Rows.Count > 0)
                    {
                        DataRow drC = dtCash.Rows[0];
                        DataRow dr = dt.Rows[dt.Rows.Count - 1];
                        if (request.Alphabetical == true && dr["AccName"].ToString() == LngTB.Total)
                        {
                            if (request.Detailed == true)
                            {
                                Decimal OpC = 0;
                                if (request.WithoutOpenBalance == false)
                                {
                                    if (drC["OpenBal"] != DBNull.Value)
                                        OpC = Convert.ToDecimal(drC["OpenBal"]);

                                    if (OpC > 0)
                                    {
                                        if (dr["OpCredit"] != DBNull.Value)
                                            dr["OpCredit"] = (Convert.ToDecimal(dr["OpCredit"]) + OpC).ToString(Variables.DecString);
                                        else
                                            dr["OpCredit"] = OpC.ToString(Variables.DecString);
                                    }
                                    else if (OpC < 0)
                                    {
                                        if (dr["OpDebit"] != DBNull.Value)
                                            dr["OpDebit"] = (Convert.ToDecimal(dr["OpDebit"]) + (-1 * OpC)).ToString(Variables.DecString);
                                        else
                                            dr["OpDebit"] = (-1 * OpC).ToString(Variables.DecString);
                                    }

                                    if (dr["OpenBal"] != DBNull.Value)
                                        dr["OpenBal"] = (Convert.ToDecimal(dr["OpenBal"]) + OpC).ToString(Variables.DecString);
                                    else
                                        dr["OpenBal"] = OpC.ToString(Variables.DecString);
                                }

                                if (dr["Debit"] != DBNull.Value)
                                {
                                    if (drC["Debit"] != DBNull.Value)
                                        dr["Debit"] = (Convert.ToDecimal(dr["Debit"]) + Convert.ToDecimal(drC["Debit"])).ToString(Variables.DecString);
                                }
                                else
                                {
                                    if (drC["Debit"] != DBNull.Value)
                                        dr["Debit"] = Convert.ToDecimal(drC["Debit"]).ToString(Variables.DecString);
                                }

                                if (dr["Credit"] != DBNull.Value)
                                {
                                    if (drC["Credit"] != DBNull.Value)
                                        dr["Credit"] = (Convert.ToDecimal(dr["Credit"]) + Convert.ToDecimal(drC["Credit"])).ToString(Variables.DecString);
                                }
                                else
                                {
                                    if (drC["Credit"] != DBNull.Value)
                                        dr["Credit"] = Convert.ToDecimal(drC["Credit"]).ToString(Variables.DecString);
                                }
                            }
                            else
                            {
                                if (drC["Balance"] != DBNull.Value && Convert.ToDecimal(drC["Balance"]) != 0)
                                {
                                    decimal CashBal = Convert.ToDecimal(drC["Balance"]);
                                    if (request.WithoutOpenBalance == true)
                                    {
                                        if (drC["OpenBal"] != DBNull.Value)
                                            CashBal = CashBal - Convert.ToDecimal(drC["OpenBal"]);

                                    }

                                    if (CashBal > 0)
                                    {
                                        if (dr["Credit"] != DBNull.Value)
                                            dr["Credit"] = (Convert.ToDecimal(dr["Credit"]) + CashBal).ToString(Variables.DecString);
                                        else
                                            dr["Credit"] = CashBal.ToString(Variables.DecString);
                                    }
                                    else
                                    {
                                        if (dr["Debit"] != DBNull.Value)
                                            dr["Debit"] = (Convert.ToDecimal(dr["Debit"]) + (-1 * CashBal)).ToString(Variables.DecString);
                                        else
                                            dr["Debit"] = (-1 * CashBal).ToString(Variables.DecString);
                                    }
                                }
                            }

                            if (dr["Balance"] != DBNull.Value)
                            {
                                if (request.WithoutOpenBalance == true)
                                    dr["Balance"] = (Convert.ToDecimal(dr["Balance"]) + Convert.ToDecimal(drC["Balance"]) - Convert.ToDecimal(drC["OpenBal"])).ToString(Variables.DecString);
                                else
                                    dr["Balance"] = (Convert.ToDecimal(dr["Balance"]) + Convert.ToDecimal(drC["Balance"])).ToString(Variables.DecString);
                            }

                            dr["Type"] = "B";
                        }
                    }

                    #region GetPendingDetails
                    string[] PendingNotes = { LngTB.GetLngByNotes("STOCK ISSUED BUT BILL NOT ISSUED", "Trial_Balance"), LngTB.GetLngByNotes("BILL ISSUED BUT STOCK NOT ISSUED", "Trial_Balance"), LngTB.GetLngByNotes("BILL RECEIVED BUT MATERIAL NOT RECEIVED", "Trial_Balance"), LngTB.GetLngByNotes("MATERIAL RECEIVED BILL NOT RECEIVED", "Trial_Balance") };
                    if (selGrp == "")
                    {
                        try
                        {
                            decimal OpBal = 0, OpCr = 0, OpDr = 0, Dr = 0, Cr = 0, Bal = 0, CCr = 0, CDr = 0;
                            if (dt.Rows.Count > 0)
                            {
                                DataRow lasr_row = dt.Rows[dt.Rows.Count - 1];

                                OpBal = Convert.ToDecimal(lasr_row[2].ToString() == "" ? "0" : lasr_row[2].ToString());
                                OpCr = Convert.ToDecimal(lasr_row[3].ToString() == "" ? "0" : lasr_row[3].ToString());
                                OpDr = Convert.ToDecimal(lasr_row[4].ToString() == "" ? "0" : lasr_row[4].ToString());
                                Dr = Convert.ToDecimal(lasr_row[5].ToString() == "" ? "0" : lasr_row[5].ToString());
                                Cr = Convert.ToDecimal(lasr_row[6].ToString() == "" ? "0" : lasr_row[6].ToString());
                                Bal = Convert.ToDecimal(lasr_row[7].ToString() == "" ? "0" : lasr_row[7].ToString());
                                if (dt.Columns.Contains("CCredit"))
                                {
                                    CCr = Convert.ToDecimal(lasr_row[11].ToString() == "" ? "0" : lasr_row[11].ToString());
                                    CDr = Convert.ToDecimal(lasr_row[12].ToString() == "" ? "0" : lasr_row[12].ToString());
                                }
                            }

                            clsOpnDir.TYPENAME = "SIDN";
                            decimal SalDif = await opnDirDB.GetOpeningByTypeName(clsOpnDir);

                            clsOpnDir.TYPENAME = "PIRN";
                            decimal PurDif = await opnDirDB.GetOpeningByTypeName(clsOpnDir);

                            if (SalDif != 0 || PurDif != 0)
                            {
                                if (SalDif != 0)
                                {
                                    if (!dt.Columns.Contains("CCredit"))
                                    {
                                        dt.Rows.Add(0, (SalDif < 0 ? PendingNotes[0] : PendingNotes[1]), 0.ToString(Variables.DecString), null, null, (SalDif < 0 ? Math.Abs(SalDif) : 0).ToString(Variables.DecString), (SalDif > 0 ? Math.Abs(SalDif) : 0).ToString(Variables.DecString), SalDif, 0, 0, null);
                                    }
                                    else
                                    {
                                        dt.Rows.Add(0, (SalDif < 0 ? PendingNotes[0] : PendingNotes[1]), 0.ToString(Variables.DecString), null, null, (SalDif < 0 ? Math.Abs(SalDif) : 0).ToString(Variables.DecString), (SalDif > 0 ? Math.Abs(SalDif) : 0).ToString(Variables.DecString), SalDif, 0, 0, null, (SalDif > 0 ? SalDif : 0).ToString(Variables.DecString), (SalDif < 0 ? Math.Abs(SalDif) : 0).ToString(Variables.DecString));
                                    }

                                    Bal += SalDif;
                                    Cr += SalDif > 0 ? Math.Abs(SalDif) : 0;
                                    Dr += SalDif < 0 ? Math.Abs(SalDif) : 0;
                                    if (dt.Columns.Contains("CCredit"))
                                    {
                                        CCr += SalDif > 0 ? Math.Abs(SalDif) : 0;
                                        CDr += SalDif < 0 ? Math.Abs(SalDif) : 0;
                                    }
                                }

                                if (PurDif != 0)
                                {
                                    if (!dt.Columns.Contains("CCredit"))
                                    {
                                        dt.Rows.Add(0, (PurDif < 0 ? PendingNotes[2] : PendingNotes[3]), 0.ToString(Variables.DecString), null, null, (PurDif < 0 ? Math.Abs(PurDif) : 0).ToString(Variables.DecString), (PurDif > 0 ? Math.Abs(PurDif) : 0).ToString(Variables.DecString), PurDif, 0, 0, null);
                                    }
                                    else
                                    {
                                        dt.Rows.Add(0, (PurDif < 0 ? PendingNotes[2] : PendingNotes[3]), 0.ToString(Variables.DecString), null, null, (PurDif < 0 ? Math.Abs(PurDif) : 0).ToString(Variables.DecString), (PurDif > 0 ? Math.Abs(PurDif) : 0).ToString(Variables.DecString), PurDif, 0, 0, null, (PurDif > 0 ? PurDif : 0).ToString(Variables.DecString), (PurDif < 0 ? Math.Abs(PurDif) : 0).ToString(Variables.DecString));
                                    }

                                    Bal += PurDif;
                                    Cr += PurDif > 0 ? Math.Abs(PurDif) : 0;
                                    Dr += PurDif < 0 ? Math.Abs(PurDif) : 0;
                                    if (dt.Columns.Contains("CCredit"))
                                    {
                                        CCr += SalDif > 0 ? Math.Abs(SalDif) : 0;
                                        CDr += SalDif < 0 ? Math.Abs(SalDif) : 0;
                                    }
                                }

                                if (!request.Detailed)
                                {
                                    Bal = Cr - Dr;
                                }

                                if (!dt.Columns.Contains("CCredit"))
                                {
                                    dt.Rows.Add(0, LngTB.Trial_Balance_Total, OpBal.ToString(Variables.DecString), (request.WithoutOpenBalance ? null : OpCr.ToString(Variables.DecString)), (request.WithoutOpenBalance ? null : OpDr.ToString(Variables.DecString)), Dr.ToString(Variables.DecString), Cr.ToString(Variables.DecString), Bal.ToString(Variables.DecString), 0, 0, "B");
                                }
                                else
                                {
                                    dt.Rows.Add(0, LngTB.Trial_Balance_Total, OpBal.ToString(Variables.DecString), (request.WithoutOpenBalance ? null : OpCr.ToString(Variables.DecString)), (request.WithoutOpenBalance ? null : OpDr.ToString(Variables.DecString)), Dr.ToString(Variables.DecString), Cr.ToString(Variables.DecString), Bal.ToString(Variables.DecString), 0, 0, "B", CCr.ToString(Variables.DecString), CDr.ToString(Variables.DecString));
                                }
                            }
                        }
                        catch (Exception ex) { }
                    }
                    #endregion


                    if (dt.Rows.Count > 0)
                    {
                        DataRow dr1 = dt.Rows[dt.Rows.Count - 1];
                        if (dr1["Balance"] != DBNull.Value && !string.IsNullOrEmpty(dr1["Balance"].ToString()))
                        {
                            if (request.Detailed)
                            {
                                dt.Rows.Add(0, LngTB.Trial_Difference, null, null, null, null, null, dr1["Balance"].ToString(), null, null, "B");

                            }
                            else
                            {
                                if (Convert.ToDecimal(dr1["Balance"]) >= 0)
                                    dt.Rows.Add(0, LngTB.Trial_Difference, null, null, null, null, dr1["Balance"], dr1["Balance"], null, null, "B");
                                else
                                    dt.Rows.Add(0, LngTB.Trial_Difference, null, null, null, (-1 * Convert.ToDecimal(dr1["Balance"])).ToString(Variables.DecString), null, dr1["Balance"], null, null, "B");
                            }
                        }
                        else
                        {
                            decimal cr = 0;
                            if (dr1["Credit"] != DBNull.Value && !string.IsNullOrEmpty(dr1["Credit"].ToString()))
                                cr = Convert.ToDecimal(dr1["Credit"]);

                            decimal dr = 0;
                            if (dr1["Debit"] != DBNull.Value && !string.IsNullOrEmpty(dr1["Debit"].ToString()))
                                dr = Convert.ToDecimal(dr1["Debit"]);

                            if (request.Detailed == true)
                            {
                                if ((cr - dr) > 0)
                                    dt.Rows.Add(0, LngTB.Trial_Difference, null, null, null, null, null, cr - dr, null, null, "B");
                                else if ((cr - dr) < 0)
                                    dt.Rows.Add(0, LngTB.Trial_Difference, null, null, null, null, null, -1 * (cr - dr), null, null, "B");
                            }
                            else
                            {
                                if ((cr - dr) > 0)
                                    dt.Rows.Add(0, LngTB.Trial_Difference, null, null, null, null, cr - dr, cr - dr, null, null, "B");
                                else if ((cr - dr) < 0)
                                    dt.Rows.Add(0, LngTB.Trial_Difference, null, null, null, -1 * (cr - dr), null, -1 * (cr - dr), null, null, "B");
                            }
                        }
                    }

                    if (dt != null)
                    {
                        dtls = dt.Copy();
                        decimal CCr = 0, CDr = 0;
                        foreach (DataRow drA in dt.Rows)
                        {
                            decimal cb = 0;
                            decimal op = 0;
                            if (drA["Balance"] != DBNull.Value && !string.IsNullOrEmpty(drA["Balance"].ToString()))
                                cb = Convert.ToDecimal(drA["Balance"]);

                            if (drA["OpenBal"] != DBNull.Value && !string.IsNullOrEmpty(drA["OpenBal"].ToString()))
                                op = Convert.ToDecimal(drA["OpenBal"]);

                            if (cb > 0)
                            {
                                if (drA["AccName"].ToString() != LngTB.Trial_Difference && drA["AccName"].ToString() != LngTB.Total && ((drA["AccId"] != DBNull.Value && Convert.ToInt32(drA["AccId"]) > 0) || PendingNotes.Contains(drA["AccName"].ToString())))
                                    CCr += cb;

                                if (op > 0)
                                    dtP.Rows.Add(drA["AccId"], drA["AccName"], op.ToString(Variables.DecString) + " Cr", drA["OpCredit"], drA["OpDebit"], drA["Debit"], drA["Credit"], cb.ToString(Variables.DecString) + " Cr", null, null, null, null, drA["Type"], cb.ToString(Variables.DecString), 0.ToString(Variables.DecString));
                                else if (op < 0)
                                    dtP.Rows.Add(drA["AccId"], drA["AccName"], (-1 * op).ToString(Variables.DecString) + " Dr", drA["OpCredit"], drA["OpDebit"], drA["Debit"], drA["Credit"], cb.ToString(Variables.DecString) + " Cr", null, null, null, null, drA["Type"], cb.ToString(Variables.DecString), 0.ToString(Variables.DecString));
                                else
                                    dtP.Rows.Add(drA["AccId"], drA["AccName"], null, drA["OpCredit"], drA["OpDebit"], drA["Debit"], drA["Credit"], cb.ToString(Variables.DecString) + " Cr", null, null, null, null, drA["Type"], cb.ToString(Variables.DecString), 0.ToString(Variables.DecString));
                            }
                            else if (cb < 0)
                            {
                                if (drA["AccName"].ToString() != LngTB.Trial_Difference && drA["AccName"].ToString() != LngTB.Total && ((drA["AccId"] != DBNull.Value && Convert.ToInt32(drA["AccId"]) > 0) || PendingNotes.Contains(drA["AccName"].ToString())))
                                    CDr += (-1 * cb);

                                if (op < 0)
                                    dtP.Rows.Add(drA["AccId"], drA["AccName"], (-1 * op).ToString(Variables.DecString) + " Dr", drA["OpCredit"], drA["OpDebit"], drA["Debit"], drA["Credit"], (-1 * cb).ToString(Variables.DecString) + " Dr", null, null, null, null, drA["Type"], 0.ToString(Variables.DecString), (-1 * cb).ToString(Variables.DecString));
                                else if (op > 0)
                                    dtP.Rows.Add(drA["AccId"], drA["AccName"], op.ToString(Variables.DecString) + " Cr", drA["OpCredit"], drA["OpDebit"], drA["Debit"], drA["Credit"], (-1 * cb).ToString(Variables.DecString) + " Dr", null, null, null, null, drA["Type"], 0.ToString(Variables.DecString), (-1 * cb).ToString(Variables.DecString));
                                else
                                    dtP.Rows.Add(drA["AccId"], drA["AccName"], null, drA["OpCredit"], drA["OpDebit"], drA["Debit"], drA["Credit"], (-1 * cb).ToString(Variables.DecString) + " Dr", null, null, null, null, drA["Type"], 0.ToString(Variables.DecString), (-1 * cb).ToString(Variables.DecString));
                            }
                            else
                            {
                                if (op > 0)
                                    dtP.Rows.Add(drA["AccId"], drA["AccName"], op.ToString(Variables.DecString) + " Cr", drA["OpCredit"], drA["OpDebit"], drA["Debit"], drA["Credit"], null, null, null, null, null, drA["Type"], 0.ToString(Variables.DecString), 0.ToString(Variables.DecString));
                                else if (op < 0)
                                    dtP.Rows.Add(drA["AccId"], drA["AccName"], (-1 * op).ToString(Variables.DecString) + " Dr", drA["OpCredit"], drA["OpDebit"], drA["Debit"], drA["Credit"], null, null, null, null, null, drA["Type"], 0.ToString(Variables.DecString), 0.ToString(Variables.DecString));
                                else if (drA["AccName"] != DBNull.Value && (drA["AccName"].ToString() == LngTB.Grand_Total || drA["AccName"].ToString() == LngTB.Total || drA["AccName"].ToString() == LngTB.Trial_Difference || request.Detailed == false))
                                    dtP.Rows.Add(drA["AccId"], drA["AccName"], null, drA["OpCredit"], drA["OpDebit"], drA["Debit"], drA["Credit"], null, null, null, null, null, drA["Type"], 0.ToString(Variables.DecString), 0.ToString(Variables.DecString));
                                else
                                {
                                    if (request.Detailed && (Convert.ToDecimal(drA["Debit"].ToString() == "" ? "0" : drA["Debit"].ToString()) != 0 || Convert.ToDecimal(drA["Credit"].ToString() == "" ? "0" : drA["Credit"].ToString()) != 0))
                                        dtP.Rows.Add(drA["AccId"], drA["AccName"], null, drA["OpCredit"], drA["OpDebit"], drA["Debit"], drA["Credit"], null, null, null, null, null, drA["Type"], 0.ToString(Variables.DecString), 0.ToString(Variables.DecString));
                                }
                            }

                            if (drA["AccName"] != DBNull.Value && dtP.Rows.Count > 0)
                            {
                                int ri = dtP.Rows.Count - 1;
                                decimal Bal = (dtP.Rows[ri]["Credit"].ToString() == "" ? 0 : Convert.ToDecimal(dtP.Rows[ri]["Credit"])) - (dtP.Rows[ri]["Debit"].ToString() == "" ? 0 : Convert.ToDecimal(dtP.Rows[ri]["Debit"]));
                                if (drA["AccName"].ToString() == LngTB.Trial_Difference)
                                {
                                    if (CCr - CDr > 0)
                                    {
                                        dtP.Rows[ri]["CCredit"] = (CCr - CDr).ToString(Variables.DecString);
                                        dtP.Rows[ri]["CDebit"] = 0.ToString(Variables.DecString);
                                    }
                                    else
                                    {
                                        dtP.Rows[ri]["CCredit"] = 0.ToString(Variables.DecString);
                                        dtP.Rows[ri]["CDebit"] = (-1 * (CCr - CDr)).ToString(Variables.DecString);
                                    }

                                    if (request.Detailed == false)
                                        dtP.Rows[ri]["Balance"] = Bal == 0 ? "" : (Math.Abs(Bal).ToString(Variables.DecString) + (Bal > 0 ? " Cr" : " Dr"));
                                }
                                else if (drA["AccName"].ToString() == LngTB.Total || drA["AccName"].ToString() == LngTB.Grand__Total)
                                {
                                    dtP.Rows[ri]["CCredit"] = CCr.ToString(Variables.DecString);
                                    dtP.Rows[ri]["CDebit"] = CDr.ToString(Variables.DecString);

                                    if (request.Detailed == false)
                                        dtP.Rows[ri]["Balance"] = Bal == 0 ? "" : (Math.Abs(Bal).ToString(Variables.DecString) + (Bal > 0 ? " Cr" : " Dr"));
                                }
                                else if (request.Detailed == false)
                                {
                                    dtP.Rows[ri]["Balance"] = Math.Abs(Bal).ToString(Variables.DecString) + (Bal > 0 ? " Cr" : " Dr");
                                }
                            }
                        }

                        if (selGrp != "" && dtP.Rows.Count > 0)
                        {
                            if (dtP.Rows[dtP.Rows.Count - 1]["AccName"].ToString() == LngTB.Trial_Difference)
                                dtP.Rows.RemoveAt(dtP.Rows.Count - 1);

                            if (dtP.Rows.Count > 0 && dtP.Rows[dtP.Rows.Count - 1]["AccName"].ToString() == LngTB.Grand__Total)
                                dtP.Rows.RemoveAt(dtP.Rows.Count - 1);
                        }

                        await dayBookDB.DropTable();
                    }

                    if (request.Detailed == true)
                    {
                        if (request.WithoutOpenBalance == true)
                            dtP.Rows.Add(null, null, null, null, null, null, null, null, 0, 3, true, false);
                        else
                            dtP.Rows.Add(null, null, null, null, null, null, null, null, 0, 3, true, true);
                    }
                    else
                    {
                        dtP.Rows.Add(null, null, null, null, null, null, null, null, 0, 3, false, false);
                    }

                    DataRow dr4 = dtP.Rows[dtP.Rows.Count - 1];
                    dtP.Rows.Remove(dr4);
                    await dayBookDB.DropTable();
                }


                ManageOpeningSummary(request, OpeningSummary, false);
                
                //--

                var tableData = dtP.AsEnumerable().Select(row => dtP.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).ToList();
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

        bool ManageOpeningSummary(TrialBalanceRequest request, bool _OpeningSummary, bool Get)
        {
            bool OpeningSummary = _OpeningSummary;
            if (Get)
            {
                OpeningSummary = request.OpeningBalanceDetail && !request.Detailed;

                if (OpeningSummary)
                {
                    request.Detailed = true;
                    request.OpeningBalanceDetail = false;
                }
            }
            else
            {
                if (OpeningSummary)
                {
                    request.Detailed = false;
                    request.OpeningBalanceDetail = true;
                }
            }
            return OpeningSummary;
        }
    }
}