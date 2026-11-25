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

    [Route("api/v1/reports/ledger")]
    [Authorize]
    public class LedgerController : ControllerBase
    {

        private readonly DatabaseConnection _databaseConnection;
        public LedgerController(DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }
        [HttpPost]
        public async Task<IActionResult> GetLedger(LedgerRequest request, [FromQuery] PaginationRequest pagination)
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

                AccMasDB accMasDB = new AccMasDB(DBConn);
                accMasDB.Variables = Variables;
                accMasDB.Functions.Variables = Variables;
                ClsAccMas clsAccMas = new ClsAccMas();

                CurrMasDB currMasDB = new CurrMasDB(DBConn);
                currMasDB.Variables = Variables;
                currMasDB.Functions.Variables = Variables;
                ClsCurrMas clsCurrMas = new ClsCurrMas();

                DbModelDB dbModelDB = new DbModelDB(DBConn);
                currMasDB.Variables = Variables;
                currMasDB.Functions.Variables = Variables;

                Dictionary<string, string> _referenceLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                Dictionary<DateTime, string> _financialYearLookup = new Dictionary<DateTime, string>();

                //-- Get Report

                await commonFunction.GetCompanyFY(Variables);

                bool GetOutOfYearData = false;
                if (commonFunction.financialYears.Count > 0)
                {
                    if (Variables.fyStartDate <= request.FromDate.Date && Variables.fyEndDate >= request.ToDate)
                        GetOutOfYearData = false;
                    else
                        GetOutOfYearData = true;
                }

                DataTable RepDt = new DataTable();
                RepDt.Columns.Add("VRNO");
                RepDt.Columns.Add("VRDATE");
                RepDt.Columns.Add("VRTYPE");
                RepDt.Columns.Add("REFNO");
                RepDt.Columns.Add("BILLNO");
                RepDt.Columns.Add("ACCOUNT");
                RepDt.Columns.Add("Debit");
                RepDt.Columns.Add("Credit");
                RepDt.Columns.Add("BALANCE");
                RepDt.Columns.Add("BALTYPE");
                RepDt.Columns.Add("NARRATION");
                RepDt.Columns.Add("TDATE");
                RepDt.Columns.Add("TYPE");
                RepDt.Columns.Add("BRNAME");
                RepDt.Columns.Add("VSORDCD");

                DataTable dtRefDtls = new DataTable();
                bool fromCashFlowStmnt = false;
                decimal OpenBal = 0;
                decimal GroupBal = 0;
                decimal GroupBal_Dr = 0;
                decimal GroupBal_Cr = 0;

                DataTable dtOPN_MultiCurr = new DataTable();
                bool MultiCurr = false;
                int MultiCurrCode = 0;
                bool MultiCurrMode = false;
                if (Variables.MultiCurr && request.MultiCurrSelected && request.MultiCurrCode > 0)
                {
                    MultiCurr = true;
                    clsCurrMas.Code = MultiCurrCode = request.MultiCurrCode;
                    DataTable dtCurr = await currMasDB.SearchByCode(clsCurrMas);
                    if (dtCurr.Rows.Count > 0 && Convert.ToInt32(dtCurr.Rows[0]["CONVMODE"]) != 1)
                        MultiCurrMode = true;

                    clsAccMas.Id = request.SingleAccountSelected == true ? Convert.ToInt32(request.SingleAccountCode) : 0;
                    clsAccMas.LedgerGroupId = request.GroupSelected == true ? Convert.ToInt32(request.GroupCode) : 0;
                    dtOPN_MultiCurr = await accMasDB.GetOPN_MultiCurr(clsAccMas, MultiCurrCode, MultiCurrMode);
                }

                if (request.WithReference)
                    dayBookDB.TB_REFDTLS = "";

                if (!GetOutOfYearData)
                {
                    if (request.FromDate.Date > Variables.fyStartDate)
                        await dayBookDB.CreateTable(Variables.fyStartDate.ToString(Variables.format), request.FromDate.AddDays(-1).ToString(Variables.format), "OP", SkipTbCr: false, IsDetail: request.DetailReport, FromLedger: true, MultiCurr: MultiCurr, MultiCurrCode: MultiCurrCode, MultiCurrMode: MultiCurrMode, Unapproved: request.IncludeUnapproved);
                    else
                        await dayBookDB.CreateTable(Variables.fyStartDate.AddDays(-1).ToString(Variables.format), request.FromDate.AddDays(-1).ToString(Variables.format), "OP", SkipTbCr: false, IsDetail: request.DetailReport, FromLedger: true, MultiCurr: MultiCurr, MultiCurrCode: MultiCurrCode, MultiCurrMode: MultiCurrMode, Unapproved: request.IncludeUnapproved);

                    await dayBookDB.CreateTable(request.FromDate.ToString(Variables.format), request.ToDate.ToString(Variables.format), "DB", SkipTbCr: false, IsDetail: request.DetailReport, FromLedger: true, MultiCurr: MultiCurr, MultiCurrCode: MultiCurrCode, MultiCurrMode: MultiCurrMode, Unapproved: request.IncludeUnapproved);
                    await dayBookDB.CancelRecPayID();

                    if (!request.DetailReport)
                        await dayBookDB.RemoveAdjCD();

                    if (request.WithReference)
                        await dayBookDB.CreateRefDtls(request.SingleAccountSelected ? Convert.ToInt32(request.SingleAccountCode) : 0, Variables.Finyr);
                }
                else
                {
                    string CurCon = connectionString;
                    string FirstConn = "", FirstDb = "", ConnString = "", dbName = "";
                    string tableName = "DB" + DateTime.Now.Hour.ToString() + "_" + DateTime.Now.Minute.ToString() + "_" + DateTime.Now.Second.ToString();
                    string OpTable = "OP" + DateTime.Now.Hour.ToString() + "_" + DateTime.Now.Minute.ToString() + "_" + DateTime.Now.Second.ToString();

                    foreach (var item in commonFunction.financialYears)
                    {
                        if (item.ToDate.Date >= request.FromDate.Date && item.FromDate.Date <= request.ToDate.Date)
                        {
                            dbName = item.DatabaseName;

                            var builderOut = new SqlConnectionStringBuilder
                            {
                                DataSource = Variables.ServerName,
                                InitialCatalog = dbName,
                                UserID = Variables.UserName,
                                Password = Variables.Password,
                                Encrypt = true,
                                TrustServerCertificate = true,
                                MultipleActiveResultSets = true
                            };
                            ConnString = builderOut.ConnectionString;

                            if (FirstConn == "")
                            {
                                FirstConn = ConnString;
                                FirstDb = dbName;
                            }

                            DBConn.SetCurrentConnection(ConnString);
                            dayBookDB.tableName = dayBookDB.OpTable = OpTable;

                            if (request.FromDate.Date > item.FromDate.Date)
                                await dayBookDB.CreateTable(item.FromDate.Date.ToString(Variables.format), request.FromDate.AddDays(-1).ToString(Variables.format), "OP", SkipTbCr: true, IsDetail: request.DetailReport, FromLedger: true, MultiCurr: MultiCurr, MultiCurrCode: MultiCurrCode, MultiCurrMode: MultiCurrMode, StkIssRecAdj: fromCashFlowStmnt, Unapproved: request.IncludeUnapproved);
                            else
                                await dayBookDB.CreateTable(item.FromDate.Date.AddDays(-1).ToString(Variables.format), item.FromDate.Date.AddDays(-1).ToString(Variables.format), "OP", SkipTbCr: true, IsDetail: request.DetailReport, FromLedger: true, MultiCurr: MultiCurr, MultiCurrCode: MultiCurrCode, MultiCurrMode: MultiCurrMode, StkIssRecAdj: fromCashFlowStmnt, Unapproved: request.IncludeUnapproved);

                            dayBookDB.tableName = tableName;

                            await dayBookDB.CreateTable(request.FromDate.ToString(Variables.format), request.ToDate.ToString(Variables.format), "DB", SkipTbCr: true, IsDetail: request.DetailReport, FromLedger: true, MultiCurr: MultiCurr, MultiCurrMode: MultiCurrMode, StkIssRecAdj: fromCashFlowStmnt, Unapproved: request.IncludeUnapproved);
                            await dayBookDB.CancelRecPayID();

                            if (!request.DetailReport)
                                await dayBookDB.RemoveAdjCD();

                            if (request.WithReference)
                                await dayBookDB.CreateRefDtls(request.SingleAccountSelected ? Convert.ToInt32(request.SingleAccountCode) : 0, item.FinancialYear);

                            if (FirstConn != ConnString)
                            {
                                await dbModelDB.insertTable(ConnString, FirstConn, dayBookDB.OpTable, "");
                                await dbModelDB.insertTable(ConnString, FirstConn, dayBookDB.tableName, "");

                                if (request.WithReference)
                                    await dbModelDB.insertTable(ConnString, FirstConn, dayBookDB.TB_REFDTLS, "");

                                DBConn.SetCurrentConnection(ConnString);
                                await dayBookDB.DropTable();
                                await dayBookDB.DropTable_TB_REFDTLS();
                            }
                        }
                    }
                    DBConn.SetCurrentConnection(FirstConn);
                }

                bool BrNameYN = commonFunction.Functions.BRTBSTMNT_GET_BRNAME() == "" ? false : true;
                if (request.WithReference)
                {
                    dtRefDtls = await dayBookDB.GetRefDtls();
                    await dayBookDB.DropTable_TB_REFDTLS();
                    BuildReferenceLookup(request, Variables, dtRefDtls, _referenceLookup, _financialYearLookup);
                }

                if (request.SingleAccountSelected)
                {
                    DataTable dtR = null;
                    clsAccMas.Id = Convert.ToInt32(request.SingleAccountCode);
                    DataTable dt = await accMasDB.SearchByID(clsAccMas, DefBranch: false);
                    if (dt.Rows.Count > 0)
                    {
                        DataRow dr = dt.Rows[0];

                        if (MultiCurr)
                        {
                            DataRow[] drOPN_MultiCurr = dtOPN_MultiCurr.Select("ACCID='" + clsAccMas.Id + "'");
                            if (drOPN_MultiCurr.Length > 0)
                                OpenBal = Convert.ToDecimal(drOPN_MultiCurr[0]["OPNVAL"].ToString() == "" ? "0" : drOPN_MultiCurr[0]["OPNVAL"].ToString());
                            else
                                OpenBal = 0;
                        }
                        else
                        {
                            OpenBal = Convert.ToDecimal(dr["OPENBAL"].ToString() == "" ? "0" : dr["OPENBAL"].ToString());
                        }

                        if (fromCashFlowStmnt)
                            await dayBookDB.CreateTable_RemoveRecordExceptCasLedgerEntry(request.SingleAccountCode);

                        if (request.SingleAccountCode == Variables.CashCD)
                        {
                            OpenBal += await dayBookDB.CashLedgerBalance(request.SingleAccountCode);
                            dtR = await dayBookDB.CashLedger(request.SingleAccountCode, IsDetail: request.DetailReport, FromLedger: true, VrSummary: request.VoucherSummary);
                        }
                        else if (Convert.ToInt32(dr["MGROUP"]) == 4)
                        {
                            OpenBal += await dayBookDB.LedgerBalance(Convert.ToInt32(request.SingleAccountCode));
                            dtR = await dayBookDB.Ledger(request.SingleAccountCode, IsDetail: request.DetailReport, VrSummary: request.VoucherSummary);
                        }
                        else
                        {
                            OpenBal += await dayBookDB.LedgerBalance(Convert.ToInt32(request.SingleAccountCode));
                            dtR = await dayBookDB.Ledger(request.SingleAccountCode, IsDetail: request.DetailReport, VrSummary: request.VoucherSummary);
                        }

                        GetRefereceDetails(request, Variables, commonFunction.financialYears, _referenceLookup, _financialYearLookup, GetOutOfYearData, clsAccMas.Id, dtR);
                    }

                    if (fromCashFlowStmnt)
                        OpenBal = 0;

                    if (OpenBal > 0)
                        RepDt.Rows.Add(null, null, null, null, null, LngTB.Opening_Balance, null, null, OpenBal.ToString(Variables.DecString), "Cr", null, request.FromDate.Date, 'B');
                    else
                        RepDt.Rows.Add(null, null, null, null, null, LngTB.Opening_Balance, null, null, (-1 * OpenBal).ToString(Variables.DecString), "Dr", null, request.FromDate.Date, 'B');

                    decimal Cr = 0, Dr = 0;
                    decimal bal = OpenBal;

                    if (OpenBal > 0)
                        Cr = OpenBal;
                    else
                        Dr = -1 * OpenBal;

                    if (dtR != null)
                    {
                        foreach (DataRow drA in dtR.Rows)
                        {
                            if (drA["Credit"] != DBNull.Value && !string.IsNullOrEmpty(drA["Credit"].ToString()))
                                Cr += Convert.ToDecimal(drA["Credit"]);
                            if (drA["Debit"] != DBNull.Value && !string.IsNullOrEmpty(drA["Debit"].ToString()))
                                Dr += Convert.ToDecimal(drA["Debit"]);

                            bal = Cr - Dr;

                            string[] split = drA["BILLNO"].ToString().Split(new[] { "#@#" }, StringSplitOptions.None);
                            string VNo = split.Length > 0 ? split[0].ToString() : "";
                            string BNo = split.Length > 1 ? split[1].ToString() : "";

                            if (bal > 0)
                                RepDt.Rows.Add(drA["VRNO"], drA["VRDATE"], drA["VRTYPE"], VNo, BNo, drA["ACCOUNT"], drA["Debit"], drA["Credit"], bal.ToString(Variables.DecString), "Cr", drA["NARRATION"], request.FromDate.Date, null, (BrNameYN ? drA["BRNAME"] : ""), (BrNameYN ? drA["VSORDCD"] : "0"));
                            else
                                RepDt.Rows.Add(drA["VRNO"], drA["VRDATE"], drA["VRTYPE"], VNo, BNo, drA["ACCOUNT"], drA["Debit"], drA["Credit"], (-1 * bal).ToString(Variables.DecString), "Dr", drA["NARRATION"], request.FromDate.Date, null, (BrNameYN ? drA["BRNAME"] : ""), (BrNameYN ? drA["VSORDCD"] : "0"));
                        }
                    }

                    if (bal > 0)
                        RepDt.Rows.Add(null, null, null, null, null, LngTB.Closing_Balance, ((Dr - (OpenBal > 0 ? 0 : -1 * OpenBal)) != 0 ? (Dr - (OpenBal > 0 ? 0 : -1 * OpenBal)).ToString(Variables.DecString) : ""), ((Cr - (OpenBal > 0 ? OpenBal : 0)) != 0 ? (Cr - (OpenBal > 0 ? OpenBal : 0)).ToString(Variables.DecString) : ""), bal.ToString(Variables.DecString), "Cr", null, request.FromDate.Date, 'B');
                    else
                        RepDt.Rows.Add(null, null, null, null, null, LngTB.Closing_Balance, ((Dr - (OpenBal > 0 ? 0 : -1 * OpenBal)) != 0 ? (Dr - (OpenBal > 0 ? 0 : -1 * OpenBal)).ToString(Variables.DecString) : ""), ((Cr - (OpenBal > 0 ? OpenBal : 0)) != 0 ? (Cr - (OpenBal > 0 ? OpenBal : 0)).ToString(Variables.DecString) : ""), (-1 * bal).ToString(Variables.DecString), "Dr", null, request.FromDate.Date, 'B');
                }
                else if (request.GroupSelected)
                {
                    if (request.GroupCode > 0)
                    {
                        clsAccMas.LedgerGroupId = request.GroupCode;
                        DataTable dt = await accMasDB.ActiveAccountsForLedger_Fast(clsAccMas.LedgerGroupId, dayBookDB.OpTable, dayBookDB.tableName, request.IncludeUnapproved);
                        DataTable Dt_Opn = await dayBookDB.LedgerBalance_Fast(clsAccMas.LedgerGroupId);
                        DataTable Dt_Trn = await dayBookDB.Ledger_Fast(clsAccMas.LedgerGroupId, IsDetail: request.DetailReport, FromLedger: true, VrSummary: request.VoucherSummary);

                        int i = 0;
                        foreach (DataRow dr in dt.Rows)
                        {
                            if (MultiCurr)
                            {
                                DataRow[] drOPN_MultiCurr = dtOPN_MultiCurr.Select("ACCID='" + dr["ID"].ToString() + "'");
                                if (drOPN_MultiCurr.Length > 0)
                                    OpenBal = Convert.ToDecimal(drOPN_MultiCurr[0]["OPNVAL"].ToString() == "" ? "0" : drOPN_MultiCurr[0]["OPNVAL"].ToString());
                                else
                                    OpenBal = 0;
                            }
                            else
                            {
                                OpenBal = Convert.ToDecimal(dr["OPENBAL"].ToString() == "" ? "0" : dr["OPENBAL"].ToString());
                            }

                            DataTable DtR;
                            if (Convert.ToInt32(dr["ID"]) == Variables.CashCD)
                            {
                                OpenBal += await dayBookDB.CashLedgerBalance(Convert.ToInt32(dr["ID"]));
                                DtR = await dayBookDB.CashLedger(Convert.ToInt32(dr["ID"]), IsDetail: request.DetailReport, FromLedger: true, VrSummary: request.VoucherSummary);
                            }
                            else
                            {
                                DataRow[] Dr_Opn = Dt_Opn.Select("ACCID ='" + Convert.ToInt32(dr["ID"]) + "'");
                                if (Dr_Opn.Length > 0 && Dr_Opn[0]["BALANCE"].ToString() != "")
                                    OpenBal += Convert.ToDecimal(Dr_Opn[0]["BALANCE"]);

                                DataRow[] Dr_Trn = Dt_Trn.Select("ACCID ='" + Convert.ToInt32(dr["ID"]) + "'");
                                if (Dr_Trn.Length > 0)
                                    DtR = Dr_Trn.CopyToDataTable();
                                else
                                    DtR = Dt_Trn.Clone();
                            }

                            if (DtR.Rows.Count > 0 || OpenBal != 0)
                            {
                                GetRefereceDetails(request, Variables, commonFunction.financialYears, _referenceLookup, _financialYearLookup, GetOutOfYearData, Convert.ToInt32(dr["ID"]), DtR);

                                RepDt.Rows.Add("0", ".", "**********", null, null, dr["ACCNAME"].ToString(), null, null, null, null, null, request.FromDate.Date, 'B');
                                if (OpenBal > 0)
                                    RepDt.Rows.Add(null, null, null, null, null, LngTB.Opening_Balance, null, null, OpenBal.ToString(Variables.DecString), "Cr", null, request.FromDate.Date, 'B');
                                else
                                    RepDt.Rows.Add(null, null, null, null, null, LngTB.Opening_Balance, null, null, (-1 * OpenBal).ToString(Variables.DecString), "Dr", null, request.FromDate.Date, 'B');

                                decimal Cr = 0, Dr = 0;
                                decimal bal = OpenBal;

                                if (OpenBal > 0)
                                    Cr = OpenBal;
                                else
                                    Dr = -1 * OpenBal;

                                foreach (DataRow drA in DtR.Rows)
                                {
                                    if (drA["Credit"] != DBNull.Value && !string.IsNullOrEmpty(drA["Credit"].ToString()))
                                        Cr += Convert.ToDecimal(drA["Credit"]);
                                    if (drA["Debit"] != DBNull.Value && !string.IsNullOrEmpty(drA["Debit"].ToString()))
                                        Dr += Convert.ToDecimal(drA["Debit"]);

                                    bal = Cr - Dr;

                                    string[] split = drA["BILLNO"].ToString().Split(new[] { "#@#" }, StringSplitOptions.None);
                                    string VNo = split.Length > 0 ? split[0].ToString() : "";
                                    string BNo = split.Length > 1 ? split[1].ToString() : "";

                                    if (bal > 0)
                                        RepDt.Rows.Add(drA["VRNO"], drA["VRDATE"], drA["VRTYPE"], VNo, BNo, drA["ACCOUNT"], drA["Debit"], drA["Credit"], bal.ToString(Variables.DecString), "Cr", drA["NARRATION"], request.FromDate.Date, null, (BrNameYN ? drA["BRNAME"] : ""), (BrNameYN ? drA["VSORDCD"] : "0"));
                                    else
                                        RepDt.Rows.Add(drA["VRNO"], drA["VRDATE"], drA["VRTYPE"], VNo, BNo, drA["ACCOUNT"], drA["Debit"], drA["Credit"], (-1 * bal).ToString(Variables.DecString), "Dr", drA["NARRATION"], request.FromDate.Date, null, (BrNameYN ? drA["BRNAME"] : ""), (BrNameYN ? drA["VSORDCD"] : "0"));
                                }

                                if (bal > 0)
                                    RepDt.Rows.Add(null, null, null, null, null, LngTB.Closing_Balance, ((Dr - (OpenBal > 0 ? 0 : -1 * OpenBal)) != 0 ? (Dr - (OpenBal > 0 ? 0 : -1 * OpenBal)).ToString(Variables.DecString) : ""), ((Cr - (OpenBal > 0 ? OpenBal : 0)) != 0 ? (Cr - (OpenBal > 0 ? OpenBal : 0)).ToString(Variables.DecString) : ""), bal.ToString(Variables.DecString), "Cr", null, request.FromDate.Date, 'B');
                                else
                                    RepDt.Rows.Add(null, null, null, null, null, LngTB.Closing_Balance, ((Dr - (OpenBal > 0 ? 0 : -1 * OpenBal)) != 0 ? (Dr - (OpenBal > 0 ? 0 : -1 * OpenBal)).ToString(Variables.DecString) : ""), ((Cr - (OpenBal > 0 ? OpenBal : 0)) != 0 ? (Cr - (OpenBal > 0 ? OpenBal : 0)).ToString(Variables.DecString) : ""), (-1 * bal).ToString(Variables.DecString), "Dr", null, request.FromDate.Date, 'B');

                                GroupBal += bal;
                                GroupBal_Dr += (Dr - (OpenBal > 0 ? 0 : -1 * OpenBal));
                                GroupBal_Cr += (Cr - (OpenBal > 0 ? OpenBal : 0));
                            }
                            i++;
                        }
                        if (GroupBal != 0)
                        {
                            if (GroupBal > 0)
                                RepDt.Rows.Add(null, null, null, null, null, LngTB.Grand_Total, (GroupBal_Dr != 0 ? GroupBal_Dr.ToString(Variables.DecString) : ""), (GroupBal_Cr != 0 ? GroupBal_Cr.ToString(Variables.DecString) : ""), GroupBal.ToString(Variables.DecString), "Cr", null, request.FromDate.Date, 'B');
                            else
                                RepDt.Rows.Add(null, null, null, null, null, LngTB.Grand_Total, (GroupBal_Dr != 0 ? GroupBal_Dr.ToString(Variables.DecString) : ""), (GroupBal_Cr != 0 ? GroupBal_Cr.ToString(Variables.DecString) : ""), (-1 * GroupBal).ToString(Variables.DecString), "Dr", null, request.FromDate.Date, 'B');
                        }
                    }
                }
                else if (request.AllAccountSelected)
                {
                    DataTable dt = await accMasDB.ActiveAccountsForLedger_Fast(0, dayBookDB.OpTable, dayBookDB.tableName, request.IncludeUnapproved);
                    DataTable Dt_Opn = await dayBookDB.LedgerBalance_Fast(0);
                    DataTable Dt_Trn = await dayBookDB.Ledger_Fast(0, IsDetail: request.DetailReport, FromLedger: true, VrSummary: request.VoucherSummary);
                    int i = 0;
                    foreach (DataRow dr in dt.Rows)
                    {
                        if (MultiCurr)
                        {
                            DataRow[] drOPN_MultiCurr = dtOPN_MultiCurr.Select("ACCID='" + dr["ID"].ToString() + "'");
                            if (drOPN_MultiCurr.Length > 0)
                                OpenBal = Convert.ToDecimal(drOPN_MultiCurr[0]["OPNVAL"].ToString() == "" ? "0" : drOPN_MultiCurr[0]["OPNVAL"].ToString());
                            else
                                OpenBal = 0;
                        }
                        else
                        {
                            OpenBal = Convert.ToDecimal(dr["OPENBAL"].ToString() == "" ? "0" : dr["OPENBAL"].ToString());
                        }

                        DataTable DtR;
                        if (Convert.ToInt32(dr["ID"]) == Variables.CashCD /*Convert.ToInt32(dr["MGROUP"]) == 4*/)
                        {
                            OpenBal += await dayBookDB.CashLedgerBalance(Convert.ToInt32(dr["ID"]));
                            DtR = await dayBookDB.CashLedger(Convert.ToInt32(dr["ID"]), IsDetail: request.DetailReport, FromLedger: true, VrSummary: request.VoucherSummary);
                        }
                        else
                        {
                            DataRow[] Dr_Opn = Dt_Opn.Select("ACCID ='" + Convert.ToInt32(dr["ID"]) + "'");
                            if (Dr_Opn.Length > 0 && Dr_Opn[0]["BALANCE"].ToString() != "")
                                OpenBal += Convert.ToDecimal(Dr_Opn[0]["BALANCE"]);

                            DataRow[] Dr_Trn = Dt_Trn.Select("ACCID ='" + Convert.ToInt32(dr["ID"]) + "'");
                            if (Dr_Trn.Length > 0)
                                DtR = Dr_Trn.CopyToDataTable();
                            else
                                DtR = Dt_Trn.Clone();
                        }


                        if (DtR.Rows.Count > 0 || OpenBal != 0)
                        {
                            GetRefereceDetails(request, Variables, commonFunction.financialYears, _referenceLookup, _financialYearLookup, GetOutOfYearData, Convert.ToInt32(dr["ID"]), DtR);

                            RepDt.Rows.Add("0", ".", "**********", null, null, dr["ACCNAME"].ToString(), null, null, null, null, null, request.FromDate.Date, 'B');
                            if (OpenBal > 0)
                                RepDt.Rows.Add(null, null, null, null, null, LngTB.Opening_Balance, null, null, OpenBal.ToString(Variables.DecString), "Cr", null, request.FromDate.Date, 'B');
                            else
                                RepDt.Rows.Add(null, null, null, null, null, LngTB.Opening_Balance, null, null, (-1 * OpenBal).ToString(Variables.DecString), "Dr", null, request.FromDate.Date, 'B');

                            decimal Cr = 0, Dr = 0;
                            decimal bal = OpenBal;

                            if (OpenBal > 0)
                                Cr = OpenBal;
                            else
                                Dr = -1 * OpenBal;

                            foreach (DataRow drA in DtR.Rows)
                            {
                                if (drA["Credit"] != DBNull.Value && !string.IsNullOrEmpty(drA["Credit"].ToString()))
                                    Cr += Convert.ToDecimal(drA["Credit"]);
                                if (drA["Debit"] != DBNull.Value && !string.IsNullOrEmpty(drA["Debit"].ToString()))
                                    Dr += Convert.ToDecimal(drA["Debit"]);

                                bal = Cr - Dr;

                                string[] split = drA["BILLNO"].ToString().Split(new[] { "#@#" }, StringSplitOptions.None);
                                string VNo = split.Length > 0 ? split[0].ToString() : "";
                                string BNo = split.Length > 1 ? split[1].ToString() : "";

                                if (bal > 0)
                                    RepDt.Rows.Add(drA["VRNO"], drA["VRDATE"], drA["VRTYPE"], VNo, BNo, drA["ACCOUNT"], drA["Debit"], drA["Credit"], bal.ToString(Variables.DecString), "Cr", drA["NARRATION"], request.FromDate.Date, null, (BrNameYN ? drA["BRNAME"] : ""), (BrNameYN ? drA["VSORDCD"] : "0"));
                                else
                                    RepDt.Rows.Add(drA["VRNO"], drA["VRDATE"], drA["VRTYPE"], VNo, BNo, drA["ACCOUNT"], drA["Debit"], drA["Credit"], (-1 * bal).ToString(Variables.DecString), "Dr", drA["NARRATION"], request.FromDate.Date, null, (BrNameYN ? drA["BRNAME"] : ""), (BrNameYN ? drA["VSORDCD"] : "0"));
                            }

                            if (bal > 0)
                                RepDt.Rows.Add(null, null, null, null, null, LngTB.Closing_Balance, ((Dr - (OpenBal > 0 ? 0 : -1 * OpenBal)) != 0 ? (Dr - (OpenBal > 0 ? 0 : -1 * OpenBal)).ToString(Variables.DecString) : ""), ((Cr - (OpenBal > 0 ? OpenBal : 0)) != 0 ? (Cr - (OpenBal > 0 ? OpenBal : 0)).ToString(Variables.DecString) : ""), bal.ToString(Variables.DecString), "Cr", null, request.FromDate.Date, 'B');
                            else
                                RepDt.Rows.Add(null, null, null, null, null, LngTB.Closing_Balance, ((Dr - (OpenBal > 0 ? 0 : -1 * OpenBal)) != 0 ? (Dr - (OpenBal > 0 ? 0 : -1 * OpenBal)).ToString(Variables.DecString) : ""), ((Cr - (OpenBal > 0 ? OpenBal : 0)) != 0 ? (Cr - (OpenBal > 0 ? OpenBal : 0)).ToString(Variables.DecString) : ""), (-1 * bal).ToString(Variables.DecString), "Dr", null, request.FromDate.Date, 'B');
                        }
                        i++;
                    }
                }
                else if (request.MultiAccountSelected)
                {
                    DataTable dtAcc = await accMasDB.ActiveAccountsForLedger(request.IncludeUnapproved, request.MultiAccountCode);
                    int i = 0;
                    foreach (DataRow drAcc in dtAcc.Rows)
                    {
                        if (MultiCurr)
                        {
                            DataRow[] drOPN_MultiCurr = dtOPN_MultiCurr.Select("ACCID='" + drAcc["ID"].ToString() + "'");
                            if (drOPN_MultiCurr.Length > 0)
                            {
                                OpenBal = Convert.ToDecimal(drOPN_MultiCurr[0]["OPNVAL"].ToString() == "" ? "0" : drOPN_MultiCurr[0]["OPNVAL"].ToString());
                            }
                            else
                            {
                                OpenBal = 0;
                            }
                        }
                        else
                        {
                            OpenBal = drAcc["OPENBAL"] == DBNull.Value ? 0 : Convert.ToDecimal(drAcc["OPENBAL"]);
                        }

                        DataTable DtR = new DataTable();
                        clsAccMas.Id = Convert.ToInt32(drAcc["ID"]);
                        DataTable dt = await accMasDB.SearchByID(clsAccMas, DefBranch: false);
                        if (dt.Rows.Count > 0)
                        {
                            DataRow dr = dt.Rows[0];
                            if (Convert.ToInt32(drAcc["ID"]) == Variables.CashCD)
                            {
                                OpenBal += await dayBookDB.CashLedgerBalance(Convert.ToInt32(drAcc["ID"]));
                                DtR = await dayBookDB.CashLedger(Convert.ToInt32(drAcc["ID"]));
                            }
                            else
                            {
                                OpenBal += await dayBookDB.LedgerBalance(Convert.ToInt32(drAcc["ID"]));
                                DtR = await dayBookDB.Ledger(Convert.ToInt32(drAcc["ID"]));
                            }

                            GetRefereceDetails(request, Variables, commonFunction.financialYears, _referenceLookup, _financialYearLookup, GetOutOfYearData, clsAccMas.Id, DtR);
                        }

                        if (DtR.Rows.Count > 0 || OpenBal != 0)
                        {

                            RepDt.Rows.Add("0", ".", "**********", null, null, drAcc["ACCNAME"].ToString(), null, null, null, null, null, request.FromDate.Date, 'B');
                            if (OpenBal > 0)
                                RepDt.Rows.Add(null, null, null, null, null, LngTB.Opening_Balance, null, null, OpenBal.ToString(Variables.DecString), "Cr", null, request.FromDate.Date, 'B');
                            else
                                RepDt.Rows.Add(null, null, null, null, null, LngTB.Opening_Balance, null, null, (-1 * OpenBal).ToString(Variables.DecString), "Dr", null, request.FromDate.Date, 'B');

                            decimal Cr = 0, Dr = 0;
                            decimal bal = OpenBal;

                            if (OpenBal > 0)
                                Cr = OpenBal;
                            else
                                Dr = -1 * OpenBal;

                            foreach (DataRow drA in DtR.Rows)
                            {
                                if (drA["Credit"] != DBNull.Value && !string.IsNullOrEmpty(drA["Credit"].ToString()))
                                    Cr += Convert.ToDecimal(drA["Credit"]);
                                if (drA["Debit"] != DBNull.Value && !string.IsNullOrEmpty(drA["Debit"].ToString()))
                                    Dr += Convert.ToDecimal(drA["Debit"]);

                                bal = Cr - Dr;

                                string[] split = drA["BILLNO"].ToString().Split(new[] { "#@#" }, StringSplitOptions.None);
                                string VNo = split.Length > 0 ? split[0].ToString() : "";
                                string BNo = split.Length > 1 ? split[1].ToString() : "";

                                if (bal > 0)
                                    RepDt.Rows.Add(drA["VRNO"], drA["VRDATE"], drA["VRTYPE"], VNo, BNo, drA["ACCOUNT"], drA["Debit"], drA["Credit"], bal.ToString(Variables.DecString), "Cr", drA["NARRATION"], request.FromDate.Date, null, (BrNameYN ? drA["BRNAME"] : ""), (BrNameYN ? drA["VSORDCD"] : "0"));
                                else
                                    RepDt.Rows.Add(drA["VRNO"], drA["VRDATE"], drA["VRTYPE"], VNo, BNo, drA["ACCOUNT"], drA["Debit"], drA["Credit"], (-1 * bal).ToString(Variables.DecString), "Dr", drA["NARRATION"], request.FromDate.Date, null, (BrNameYN ? drA["BRNAME"] : ""), (BrNameYN ? drA["VSORDCD"] : "0"));
                            }

                            if (bal > 0)
                                RepDt.Rows.Add(null, null, null, null, null, LngTB.Closing_Balance, ((Dr - (OpenBal > 0 ? 0 : -1 * OpenBal)) != 0 ? (Dr - (OpenBal > 0 ? 0 : -1 * OpenBal)).ToString(Variables.DecString) : ""), ((Cr - (OpenBal > 0 ? OpenBal : 0)) != 0 ? (Cr - (OpenBal > 0 ? OpenBal : 0)).ToString(Variables.DecString) : ""), bal.ToString(Variables.DecString), "Cr", null, request.FromDate.Date, 'B');
                            else
                                RepDt.Rows.Add(null, null, null, null, null, LngTB.Closing_Balance, ((Dr - (OpenBal > 0 ? 0 : -1 * OpenBal)) != 0 ? (Dr - (OpenBal > 0 ? 0 : -1 * OpenBal)).ToString(Variables.DecString) : ""), ((Cr - (OpenBal > 0 ? OpenBal : 0)) != 0 ? (Cr - (OpenBal > 0 ? OpenBal : 0)).ToString(Variables.DecString) : ""), (-1 * bal).ToString(Variables.DecString), "Dr", null, request.FromDate.Date, 'B');
                        }
                        i++;
                    }
                }

                await dayBookDB.DropTable();
                fromCashFlowStmnt = false;

                if (GetOutOfYearData)
                    DBConn.SetCurrentConnection(connectionString);

                //--
                int totalRecords = RepDt.Rows.Count;

                int totalPages = (int)Math.Ceiling(
                    totalRecords / (double)pagination.PageSize
                );

                var pagedRows = RepDt.AsEnumerable()
                    .Skip((pagination.Page - 1) * pagination.PageSize)
                    .Take(pagination.PageSize);

                var tableData = pagedRows
                    .Select(row => RepDt.Columns
                        .Cast<DataColumn>()
                        .ToDictionary(
                            column => column.ColumnName,
                            column => row[column] == DBNull.Value
                                ? null
                                : row[column]
                        ))
                    .ToList();

                return Ok(new
                {
                    status = true,
                    data = tableData,
                    pagination = new
                    {
                        pageNumber = pagination.Page,
                        pageSize = pagination.PageSize,
                        totalRecords,
                        totalPages,
                        hasPreviousPage = pagination.Page > 1,
                        hasNextPage = pagination.Page < totalPages
                    }
                });

                //var tableData = RepDt.AsEnumerable().Select(row => RepDt.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).ToList();
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

        private void BuildReferenceLookup(LedgerRequest request, Variable Variables, DataTable dtRefDtls, Dictionary<string, string> _referenceLookup, Dictionary<DateTime, string> _financialYearLookup)
        {
            _referenceLookup.Clear();
            _financialYearLookup.Clear();

            if (!request.WithReference || dtRefDtls == null || dtRefDtls.Rows.Count == 0)
            {
                return;
            }

            var groups = dtRefDtls.AsEnumerable().GroupBy(row =>
            {
                int accountId = Convert.ToInt32(row["ACCID"]);
                string referenceType = NormalizeReferenceType(Convert.ToString(row["STYPE"]));
                string voucherNo = Convert.ToString(row["TYPECODE"]);
                string financialYear = Convert.ToString(row["FINYEAR"]);
                return CreateReferenceKey(accountId, referenceType, voucherNo, financialYear);
            }, StringComparer.OrdinalIgnoreCase);

            foreach (var group in groups)
            {
                string result = string.Join("; ", group.Select(row =>
                {
                    decimal amount = Convert.ToDecimal(Convert.ToString(row["AMOUNT"]) == "" ? "0" : Convert.ToString(row["AMOUNT"]));
                    return "Inv : " + Convert.ToString(row["REFNO"]) + " -> Amt : " + amount.ToString(Variables.DecString);
                }));

                _referenceLookup[group.Key] = result;
            }
        }

        private void GetRefereceDetails(LedgerRequest request, Variable Variables, List<FinancialYearResponse> financialYears, Dictionary<string, string> _referenceLookup, Dictionary<DateTime, string> _financialYearLookup, bool GetOutOfYearData, int AccID, DataTable dtls)
        {
            try
            {
                if (dtls == null || dtls.Rows.Count == 0)
                    return;

                bool addReferences = request.WithReference && _referenceLookup.Count > 0;
                foreach (DataRow item in dtls.Rows)
                {
                    string narration = Convert.ToString(item["NARRATION"]);
                    item["NARRATION"] = narration.Trim();

                    if (!addReferences)
                        continue;

                    string referenceType = GetLedgerReferenceType(Convert.ToString(item["VRTYPE"]));

                    if (referenceType == "")
                        continue;

                    string financialYear = Variables.Finyr;
                    if (GetOutOfYearData && Convert.ToString(item["VRDATE"]) != "")
                    {
                        DateTime voucherDate = Convert.ToDateTime(item["VRDATE"]);
                        financialYear = GetFinancialYear(request, Variables, financialYears, _referenceLookup, _financialYearLookup, GetOutOfYearData, voucherDate);
                    }

                    string key = CreateReferenceKey(AccID, referenceType, Convert.ToString(item["VRNO"]), financialYear);
                    string referenceText;
                    if (_referenceLookup.TryGetValue(key, out referenceText))
                    {
                        if (!string.IsNullOrEmpty(referenceText))
                        {
                            item["ACCOUNT"] = Convert.ToString(item["ACCOUNT"]) + Environment.NewLine + "(" + referenceText + ")";
                        }
                    }
                }
            }
            catch{}
        }

        private string NormalizeReferenceType(string referenceType)
        {
            if (referenceType == "JP" || referenceType == "JR")
            {
                return "Journal";
            }

            return referenceType;
        }
        private string CreateReferenceKey(int accountId, string referenceType, string voucherNo, string financialYear)
        {
            return accountId.ToString() + "\u001f" + referenceType + "\u001f" + voucherNo + "\u001f" + financialYear;
        }

        private string GetLedgerReferenceType(string voucherType)
        {
            if (voucherType == LngTB.Receipt)
                return "Receipt";

            if (voucherType == LngTB.Payment)
                return "Payment";

            if (voucherType == LngTB.Journal)
                return "Journal";

            if (voucherType == LngTB.Gen__Receipt)
                return "GR";

            if (voucherType == LngTB.Gen__Payment)
                return "GP";

            return "";
        }

        private string GetFinancialYear(LedgerRequest request, Variable Variables, List<FinancialYearResponse> financialYears, Dictionary<string, string> _referenceLookup, Dictionary<DateTime, string> _financialYearLookup, bool GetOutOfYearData, DateTime voucherDate)
        {
            DateTime date = voucherDate.Date;
            string financialYear;
            if (_financialYearLookup.TryGetValue(date, out financialYear))
            {
                return financialYear;
            }
            financialYear = Variables.Finyr;
            foreach (var item in financialYears)
            {
                DateTime fromDate = item.FromDate;
                DateTime toDate = item.ToDate;
                if (date >= fromDate && date <= toDate)
                {
                    string foundYear = item.FinancialYear;
                    if (!string.IsNullOrEmpty(foundYear)) financialYear = foundYear;
                    break;
                }
            }
            _financialYearLookup[date] = financialYear;
            return financialYear;
        }
    }
}