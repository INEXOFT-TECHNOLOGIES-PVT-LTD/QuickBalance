using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using QuickBalance.API.CLS;
using QuickBalance.API.DB;
using System.Data;

namespace QuickBalance.API.Controllers
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]

    [Route("api/[controller]")]
    public class CompaniesController : ControllerBase
    {
        [HttpGet("ListCompany")]
        public async Task<IActionResult> GetCompanies([FromQuery] string serverName, [FromQuery] string userName, [FromQuery] string password)
        {
            try
            {
                // -----------------------------------------
                // 1. Check Connection String
                // -----------------------------------------
                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = serverName,
                    InitialCatalog = "MasterDB",
                    UserID = userName,
                    Password = password,
                    Encrypt = true,
                    TrustServerCertificate = true,
                    MultipleActiveResultSets = true
                };
                string connectionString = builder.ConnectionString;

                // -----------------------------------------
                // 2. Create Database Connection
                // -----------------------------------------
                DBConn DBConn = new DBConn(connectionString);
                CommonFunction commonFunction = new CommonFunction(DBConn);

                // -----------------------------------------
                // 3. Check Database Connection
                // -----------------------------------------
                if (!await commonFunction.CheckDatabaseConnection())
                {
                    return StatusCode(500, new { status = "error", message = "Database connection failed." });
                }

                // -----------------------------------------
                // 4. Get Details
                // -----------------------------------------
                CompMasDB compMasDB = new CompMasDB(DBConn);
                ClsCompMas clsCompMas = new ClsCompMas();
                clsCompMas.Active = true;
                DataTable dt = await compMasDB.List(clsCompMas);
                if (dt.Rows.Count > 0)
                {
                    var tableData = dt.AsEnumerable().Select(row => dt.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column])).ToList();
                    return Ok(new
                    {
                        status = true,
                        message = "Companies loaded successfully.",
                        data = tableData
                    });
                }
                else
                {
                    return NotFound(new
                    {
                        status = false,
                        message = "No companies found."
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = ex.Message });
            }
        }
    }
}