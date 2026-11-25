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

    [Route("api/v1/reports/ledger-opening-balance")]
    [Authorize]
    public class LedgerOpeningBalanceController : ControllerBase
    {

        private readonly DatabaseConnection _databaseConnection;
        public LedgerOpeningBalanceController(DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }
        [HttpPost]
        public async Task<IActionResult> GetLedgerOpeningBalance(LedgerOpeningBalanceRequest request)
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

                DayBookDB dayBookDB = new DayBookDB(DBConn);
                dayBookDB.Variables = Variables;
                dayBookDB.Functions.Variables = Variables;

                AccMasDB accMasDB = new AccMasDB(DBConn);
                accMasDB.Variables = Variables;
                accMasDB.Functions.Variables = Variables;

                Function Functions = new Function();
                Functions.Variables = Variables;

                await commonFunction.GetPrevYearConn();

                //-- Get Report

                DataTable PRV_Ledger = new DataTable();
                if (Variables.PrevYearConn != "")
                {
                    DBConn.SetCurrentConnection(Variables.PrevYearConn);
                    await dayBookDB.CreateTable(Variables.PrevStartDate.ToString(Variables.format), Variables.PrevEndDate.ToString(Variables.format), "DB", Unapproved: request.IncludeUnapproved);
                    PRV_Ledger = await dayBookDB.PRV_LedgerBalance(request.IncludeUnapproved);
                    await dayBookDB.DropTable();
                    DBConn.SetCurrentConnection(connectionString);
                }

                DataTable dtRep = await accMasDB.GetLedgerOpeningBalanceReport((request.SelectedGroup && request.GroupCode > 0 ? request.GroupCode : 0), (request.SelectedAccount && request.AccountCode > 0 ? request.AccountCode : 0), request.IncludeZeroBalanceAccounts, request.IncludeUnapproved);

                if (PRV_Ledger.Rows.Count > 0)
                {
                    decimal PrvDebit_Tot = 0, PrvCredit_Tot = 0;
                    foreach (DataRow item in dtRep.Rows)
                    {
                        DataRow[] PRV = PRV_Ledger.Select("ACCNAME ='" + Function.ExactFilter(item["ACCNAME"].ToString()) + "'");
                        decimal PrvDebit = 0, PrvCredit = 0;
                        decimal bal = 0;
                        if (PRV.Length > 1)
                        {
                            if (item["ACCODE"].ToString() == PRV[0]["ACCODE"].ToString())
                                bal = Function.GetDec(PRV[0]["OPENBAL"]) + Function.GetDec(PRV[0]["BALANCE"]);
                            else
                                bal = Function.GetDec(PRV[1]["OPENBAL"]) + Function.GetDec(PRV[1]["BALANCE"]);
                        }
                        else if (PRV.Length > 0)
                        {
                            bal = Function.GetDec(PRV[0]["OPENBAL"]) + Function.GetDec(PRV[0]["BALANCE"]);
                        }

                        if (bal > 0)
                            PrvCredit = bal;
                        else
                            PrvDebit = -1 * bal;

                        if (item["ACCNAME"].ToString() == LngTB.Total)
                        {
                            item["PRVDEBIT"] = PrvDebit_Tot.ToString(Variables.DecString);
                            item["PRVCREDIT"] = PrvCredit_Tot.ToString(Variables.DecString);
                        }
                        else
                        {
                            PrvDebit_Tot += PrvDebit;
                            PrvCredit_Tot += PrvCredit;
                            item["PRVDEBIT"] = PrvDebit.ToString(Variables.DecString);
                            item["PRVCREDIT"] = PrvCredit.ToString(Variables.DecString);
                        }
                    }
                }

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
    }
}