using Azure.Core;
using Microsoft.Data.SqlClient;
using QuickBalance.API.DTOs;
using System.Data;
using System.Text;

namespace QuickBalance.API.DB
{
    public class CommonFunction
    {
        private readonly DBConn db;
        public Variable Variables = new Variable();
        public Function Functions = new Function();
        public List<FinancialYearResponse> financialYears = new List<FinancialYearResponse>();
        string qry = "";

        public CommonFunction(DBConn dbConn)
        {
            db = dbConn;
        }

        public async Task<bool> CheckDatabaseConnection()
        {
            var connectionString = db.ConnectionString;

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return false;
            }

            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task SetVariableValues(Variable Variables)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = Variables.ServerName,
                InitialCatalog = "MasterDB",
                UserID = Variables.UserName,
                Password = Variables.Password,
                Encrypt = true,
                TrustServerCertificate = true,
                MultipleActiveResultSets = true
            };

            string connectionString = builder.ConnectionString;
            DBConn dbMSql = new DBConn(connectionString);
            qry = "SELECT * FROM COMPMAS WHERE ID = " + Variables.CompCode;
            DataTable dtComp = await dbMSql.GetDatatable(qry);
            if (dtComp.Rows.Count > 0)
            {
                DataRow drComp = dtComp.Rows[0];

                Variables.CompShortName = drComp["SHORTNAME"].ToString();

                qry = "SELECT * FROM COUNTRY WHERE COUNTRYID = " + Convert.ToInt32(drComp["COUNTRY"]);
                DataTable dtCountry = await dbMSql.GetDatatable(qry);
                Variables.VatLbl = dtCountry.Rows.Count > 0 && dtCountry.Rows[0]["VATLABEL"].ToString() != "" ? dtCountry.Rows[0]["VATLABEL"].ToString() : "VAT";
            }

            qry = "SELECT * FROM FYDETAILS WHERE PREVFIN = '" + false + "'";
            DataTable dtFy = await db.GetDatatable(qry);
            if (dtFy.Rows.Count > 0)
            {
                DataRow drFy = dtFy.Rows[0];

                Variables.fyStartDate = Convert.ToDateTime(drFy["FYDATEFR"]).Date;
                Variables.fyEndDate = Convert.ToDateTime(drFy["FYDATETO"]).Date;
                Variables.Finyr = drFy["FINYR"].ToString();

                Variables.DiscCD = Convert.ToInt32(drFy["DISCCD"]);
                Variables.CashCD = Convert.ToInt32(drFy["CASHCD"]);
                Variables.VATCode = Convert.ToInt32(drFy["VATCD"]);
            }

            qry = "SELECT * FROM CONFIGTB";
            DataTable dtCompanyConfig = await db.GetDatatable(qry);
            if (dtCompanyConfig.Rows.Count > 0)
            {
                DataRow drCompanyConfig = dtCompanyConfig.Rows[0];

                Variables.batch = drCompanyConfig["BATCHWISE"] != DBNull.Value && Convert.ToBoolean(drCompanyConfig["BATCHWISE"]);
                Variables.SlNoTrack = drCompanyConfig["SLNOTRACK"] != DBNull.Value && Convert.ToBoolean(drCompanyConfig["SLNOTRACK"]);
                Variables.MultiCurr = Convert.ToBoolean(drCompanyConfig["MULTICURR"]);
                Variables.VRSYS = drCompanyConfig["VRSYS"] != DBNull.Value && Convert.ToBoolean(drCompanyConfig["VRSYS"]) == true;
                Variables.MULTIVRSYS = drCompanyConfig["MULTIVRSYS"] != DBNull.Value && Convert.ToBoolean(drCompanyConfig["MULTIVRSYS"]) == true;
                Variables.STKTRNSFRYN = drCompanyConfig["STKTRNSFRYN"] != DBNull.Value && Convert.ToBoolean(drCompanyConfig["STKTRNSFRYN"]);
                Variables.STKTRNSFRACC = Convert.ToInt32(drCompanyConfig["STKTRNSFRACC"].ToString() == "" ? "0" : drCompanyConfig["STKTRNSFRACC"].ToString());
                Variables.STKTRNSFRRECACC = Convert.ToInt32(drCompanyConfig["STKTRNSFRRECACC"].ToString() == "" ? "0" : drCompanyConfig["STKTRNSFRRECACC"].ToString());
                Variables.STKMVACC = drCompanyConfig["STKMVACC"] != DBNull.Value && Convert.ToBoolean(drCompanyConfig["STKMVACC"]) == true;
                Variables.STKMVACCD = drCompanyConfig["STKMVACCD"].ToString() == "" ? 0 : Convert.ToInt32(drCompanyConfig["STKMVACCD"]);
                Variables.DBSalSummary = drCompanyConfig["DBSALSUM"] != DBNull.Value && Convert.ToBoolean(drCompanyConfig["DBSALSUM"]);
                Variables.BrPrList = drCompanyConfig["BRPRLIST"] != DBNull.Value && Convert.ToBoolean(drCompanyConfig["BRPRLIST"]);
                Variables.TagAccMstr = drCompanyConfig["TAGACCMSTR"] != DBNull.Value && Convert.ToBoolean(drCompanyConfig["TAGACCMSTR"]);
                Variables.additionalCost = drCompanyConfig["ADDITIONALCOST"] != DBNull.Value && Convert.ToBoolean(drCompanyConfig["ADDITIONALCOST"]);
                Variables.InventoryApproval = drCompanyConfig["INVAPRVL"] != DBNull.Value && Convert.ToBoolean(drCompanyConfig["INVAPRVL"]);

                if (drCompanyConfig["DECPOINTS"] != DBNull.Value)
                    Variables.DecimalPoint = Convert.ToInt32(drCompanyConfig["DECPOINTS"]);
                else
                    Variables.DecimalPoint = 2;

                string zeroes = "";
                for (int i = 0; i < Variables.DecimalPoint; i++)
                    zeroes += "0";
                Variables.DecString = "#,0." + zeroes;

                if (zeroes.Length == 0)
                    Variables.DecString = "#,0";

                try
                {
                    if (drCompanyConfig["QTYDECPOINTS"].ToString() != "" && drCompanyConfig["QTYDECPOINTS"].ToString() != "0")
                        Variables.QtyDecimalPoint = Convert.ToInt32(drCompanyConfig["QTYDECPOINTS"]);
                    else
                        Variables.QtyDecimalPoint = 2;

                    string _zeroes = "";
                    for (int i = 0; i < Variables.QtyDecimalPoint; i++)
                    {
                        _zeroes += "0";
                    }
                    Variables.QtyDecString = "#,0." + _zeroes;
                }
                catch { }

                //---------------------------------------------

                DataTable dtCheckBr = await SearchByMNNameAndUser(Variables.UserId, "VRSYS_DefBranch");
                if (dtCheckBr.Rows.Count > 0)
                    Variables.VRSYS_DEFBRANCH = Convert.ToInt32(dtCheckBr.Rows[0]["MNCODE"].ToString());
                else
                    Variables.VRSYS_DEFBRANCH = 0;

                dtCheckBr = await SearchByMNNameAndUser(Variables.UserId, "VRSYS_AllowBranch");
                if (dtCheckBr.Rows.Count > 0)
                    Variables.VRSYS_ALLOWEDBRANCHES = dtCheckBr.Rows[0]["MNCODE"].ToString();
                else
                    Variables.VRSYS_ALLOWEDBRANCHES = string.Empty;

                //----------------------------------------------

                qry = "SELECT * FROM CONFIGTB";
                DataTable dtMasterConfig = await dbMSql.GetDatatable(qry);
                if (dtMasterConfig.Rows.Count > 0)
                {
                    DataRow drMasterConfig = dtMasterConfig.Rows[0];
                    if (Convert.ToBoolean(drMasterConfig["BRANCHAYN"].ToString() == "" ? "False" : drMasterConfig["BRANCHAYN"].ToString()) && Convert.ToBoolean(drCompanyConfig["BRANCHYN"].ToString() == "" ? "False" : drCompanyConfig["BRANCHYN"].ToString()))
                        Variables.BranchYN = true;
                    else
                        Variables.BranchYN = false;

                    if (Convert.ToBoolean(drMasterConfig["APRVSYSAYN"].ToString() == "" ? "False" : drMasterConfig["APRVSYSAYN"].ToString()) && Convert.ToBoolean(drCompanyConfig["APRVSYSYN"].ToString() == "" ? "False" : drCompanyConfig["APRVSYSYN"].ToString()))
                        Variables.ApprovalSystem = true;
                    else
                        Variables.ApprovalSystem = false;
                }

                Variables.StkIssRec = Variables.VRSYS && Variables.BranchYN && !Variables.MULTIVRSYS;

                if(!Variables.BranchYN)
                    Variables.BrPrList = false;
            }
        }

        public async Task<DataTable> SearchByMNNameAndUser(int UserCode, string MnName)
        {
            qry = "SELECT * FROM USER_TEMP WHERE USERCODE='" + UserCode + "' AND BTNNAME='" + MnName + "'";
            return await db.GetDatatable(qry);
        }

        public async Task<List<FinancialYearResponse>> GetCompanyFY(Variable Variables)
        {
            try
            {
                // ---------------------------------------------
                // 1. Connection to SQL Server
                // ---------------------------------------------
                var serverBuilder = new SqlConnectionStringBuilder
                {
                    DataSource = Variables.ServerName,
                    UserID = Variables.UserName,
                    Password = Variables.Password,
                    Encrypt = true,
                    TrustServerCertificate = true,
                    MultipleActiveResultSets = true
                };
                string serverConnectionString = serverBuilder.ConnectionString;

                // ---------------------------------------------
                // 2. Build company database prefix
                // ---------------------------------------------
                string companyPrefix = Variables.CompCode.ToString().Length == 2 ? "COMP" + Variables.CompCode : Variables.CompCode.ToString().Length < 2 ? "COMP0" + Variables.CompCode : "COMP" + Variables.CompCode;

                // ---------------------------------------------
                // 3. Get all company databases
                // ---------------------------------------------
                List<string> databaseNames = new List<string>();
                using (SqlConnection con = new SqlConnection(serverConnectionString))
                {
                    await con.OpenAsync();

                    string databaseQuery = @"SELECT name FROM sys.databases WHERE name LIKE @DatabasePattern ORDER BY name";
                    using SqlCommand cmd = new SqlCommand(databaseQuery, con);
                    cmd.Parameters.Add("@DatabasePattern", SqlDbType.NVarChar).Value = "%" + companyPrefix + "%";
                    using SqlDataReader reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                        databaseNames.Add(reader["name"].ToString() ?? "");
                }

                // ---------------------------------------------
                // 4. Read COMPMAS from each database
                // ---------------------------------------------
                foreach (string databaseName in databaseNames)
                {
                    if (string.IsNullOrWhiteSpace(databaseName))
                        continue;

                    var dbBuilder = new SqlConnectionStringBuilder
                    {
                        DataSource = Variables.ServerName,
                        InitialCatalog = databaseName,
                        UserID = Variables.UserName,
                        Password = Variables.Password,
                        Encrypt = true,
                        TrustServerCertificate = true,
                        MultipleActiveResultSets = true
                    };

                    using SqlConnection dbConnection = new SqlConnection(dbBuilder.ConnectionString);
                    await dbConnection.OpenAsync();
                    string query = @" SELECT FYFROM, FYTO, FINYR FROM COMPMAS";
                    using SqlCommand cmd = new SqlCommand(query, dbConnection);
                    using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                    // ---------------------------------------------
                    // Original desktop code effectively used
                    // the last COMPMAS row.
                    // ---------------------------------------------

                    DateTime? fromDate = null;
                    DateTime? toDate = null;
                    string financialYear = "";

                    while (await reader.ReadAsync())
                    {
                        if (reader["FYFROM"] != DBNull.Value)
                            fromDate = Convert.ToDateTime(reader["FYFROM"]);

                        if (reader["FYTO"] != DBNull.Value)
                            toDate = Convert.ToDateTime(reader["FYTO"]);

                        financialYear = reader["FINYR"]?.ToString() ?? "";
                    }

                    if (fromDate.HasValue && toDate.HasValue)
                    {
                        financialYears.Add(new FinancialYearResponse
                        {
                            DatabaseName = databaseName,
                            FromDate = fromDate.Value.Date,
                            ToDate = toDate.Value.Date,
                            FinancialYear = financialYear
                        });
                    }
                }

                // ---------------------------------------------
                // 5. Sort by financial year start
                // ---------------------------------------------
                financialYears = financialYears.OrderBy(x => x.FromDate).ToList();
                return financialYears;
            }
            catch
            {
                return new List<FinancialYearResponse>();
            }
        }

        public async Task GetPrevYearConn()
        {
            Variables.NextYearConn = Variables.PrevYearConn = "";
            try
            {
                // ---------------------------------------------
                // 1. Connection to SQL Server
                // ---------------------------------------------
                var serverBuilder = new SqlConnectionStringBuilder
                {
                    DataSource = Variables.ServerName,
                    UserID = Variables.UserName,
                    Password = Variables.Password,
                    Encrypt = true,
                    TrustServerCertificate = true,
                    MultipleActiveResultSets = true
                };
                string serverConnectionString = serverBuilder.ConnectionString;

                // ---------------------------------------------
                // 2. Build company database prefix
                // ---------------------------------------------
                string companyPrefix = Variables.CompCode.ToString().Length == 2 ? "COMP" + Variables.CompCode : Variables.CompCode.ToString().Length < 2 ? "COMP0" + Variables.CompCode : "COMP" + Variables.CompCode;

                // ---------------------------------------------
                // 3. Get all company databases
                // ---------------------------------------------
                List<string> databaseNames = new List<string>();
                using (SqlConnection con = new SqlConnection(serverConnectionString))
                {
                    await con.OpenAsync();

                    string databaseQuery = @"SELECT name FROM sys.databases WHERE name LIKE @DatabasePattern ORDER BY name";
                    using SqlCommand cmd = new SqlCommand(databaseQuery, con);
                    cmd.Parameters.Add("@DatabasePattern", SqlDbType.NVarChar).Value = "%" + companyPrefix + "%";
                    using SqlDataReader reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                        databaseNames.Add(reader["name"].ToString() ?? "");
                }

                string NextYrDB = "";
                foreach (string comp in databaseNames)
                {
                    if (Variables.DatabaseName != comp)
                    {
                        string year = comp.Substring(comp.IndexOf('_') + 1);
                        string startYr = year.Substring(0, year.IndexOf('_'));
                        string endYr = year.Substring(year.IndexOf('_') + 1);

                        if (Convert.ToInt64(startYr) > Convert.ToInt64(Variables.fyStartDate.Year) && Convert.ToInt64(Variables.fyStartDate.Year) + 1 == Convert.ToInt64(startYr))
                        {
                            NextYrDB = comp;
                            break;
                        }
                    }
                }

                string PrevYrDB = "";
                foreach (string comp in databaseNames)
                {
                    if (Variables.DatabaseName != comp)
                    {
                        string year = comp.Substring(comp.IndexOf('_') + 1);
                        string startYr = year.Substring(0, year.IndexOf('_'));
                        string endYr = year.Substring(year.IndexOf('_') + 1);

                        if (Convert.ToInt64(startYr) < Convert.ToInt64(Variables.fyStartDate.Year) && Convert.ToInt64(Variables.fyStartDate.Year) - 1 == Convert.ToInt64(startYr))
                        {
                            PrevYrDB = comp;
                            break;
                        }
                    }
                }

                if (NextYrDB != "")
                {
                    var serverBuilder1 = new SqlConnectionStringBuilder
                    {
                        DataSource = Variables.ServerName,
                        InitialCatalog = NextYrDB,
                        UserID = Variables.UserName,
                        Password = Variables.Password,
                        Encrypt = true,
                        TrustServerCertificate = true,
                        MultipleActiveResultSets = true
                    };
                    string serverConnectionString1 = serverBuilder1.ConnectionString;

                    Variables.NextYearConn = serverConnectionString1;
                }

                if (PrevYrDB != "")
                {
                    var serverBuilder1 = new SqlConnectionStringBuilder
                    {
                        DataSource = Variables.ServerName,
                        InitialCatalog = PrevYrDB,
                        UserID = Variables.UserName,
                        Password = Variables.Password,
                        Encrypt = true,
                        TrustServerCertificate = true,
                        MultipleActiveResultSets = true
                    };
                    string serverConnectionString1 = serverBuilder1.ConnectionString;

                    Variables.PrevYearConn = serverConnectionString1;
                }

                BuildPrevAndNextByFinancialYear((Variables.fyStartDate.Year.ToString() + "_" + Variables.fyEndDate.Year.ToString()), databaseNames);
            }
            catch
            {
                Variables.PrevYearConns.Clear();
                Variables.NextYearConns.Clear();
                Variables.PrevYRDB = "";
                Variables.NextYRDB = "";
                Variables.NextYearConn = Variables.PrevYearConn = "";
            }

            if (Variables.PrevYearConn != "")
            {
                try
                {
                    using (SqlConnection con = new SqlConnection(Variables.PrevYearConn))
                    {
                        await con.OpenAsync();

                        string databaseQuery = @"SELECT * FROM COMPMAS";
                        using SqlCommand cmd = new SqlCommand(databaseQuery, con);
                        using SqlDataReader reader = await cmd.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            Variables.PrevFINYR = reader["FINYR"].ToString();
                            Variables.PrevStartDate = Convert.ToDateTime(reader["FYFROM"]);
                            Variables.PrevEndDate = Convert.ToDateTime(reader["FYTO"]);
                        }
                    }
                }
                catch { }
            }
        }

        public void BuildPrevAndNextByFinancialYear(string currentFinancialYear, IEnumerable<string> allDbs)
        {
            Variables.PrevYearConns.Clear();
            Variables.NextYearConns.Clear();
            Variables.PrevYRDB = "";
            Variables.NextYRDB = "";

            if (string.IsNullOrWhiteSpace(currentFinancialYear) || allDbs == null)
                return;

            // Parse + order DBs by financial year
            var ordered = allDbs
                .Select(db =>
                {
                    if (string.IsNullOrWhiteSpace(db))
                        return null;

                    string name = db;
                    string[] p = name.Split('_');
                    if (p.Length < 3)
                        return null;

                    int fromYear, toYear;
                    if (!int.TryParse(p[1], out fromYear)) return null;
                    if (!int.TryParse(p[2], out toYear)) return null;

                    string conn = $"Data Source={Variables.ServerName}; Initial Catalog={name};User ID={Variables.UserName};Password={Variables.Password}";

                    return new
                    {
                        Conn = conn,
                        FullName = name,
                        FromYear = fromYear,
                        ToYear = toYear,
                        YearPart = p[1] + "_" + p[2]
                    };
                })
                .Where(x => x != null)
                .OrderBy(x => x.FromYear)
                .ThenBy(x => x.ToYear)
                .ToList();

            // All previous years
            foreach (var prev in ordered.Where(x => string.Compare(x.YearPart, currentFinancialYear) < 0))
                Variables.PrevYearConns[prev.YearPart] = prev.Conn;

            // All next years
            foreach (var next in ordered.Where(x => string.Compare(x.YearPart, currentFinancialYear) > 0))
                Variables.NextYearConns[next.YearPart] = next.Conn;

            if (Variables.PrevYearConns.Count > 0)
                Variables.PrevYRDB = Variables.PrevYearConns.Last().Key;

            if (Variables.NextYearConns.Count > 0)
                Variables.NextYRDB = Variables.NextYearConns.First().Key;

            // Last 2 year connection is taken
            if (Variables.PrevYearConns.Count > 2)
            {
                var lastTwoItems = Variables.PrevYearConns.Skip(Variables.PrevYearConns.Count - 2).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                Variables.PrevYearConns = lastTwoItems;
            }
        }

        public static async Task<DBConn?> GetUserDatabaseConnection(string authConnectionString, int userId, string databaseName)
        {
            string serverName = "";
            string sqlUserName = "";
            string encryptedPassword = "";

            // =========================================
            // 1. Connect to JwtAuth database
            // =========================================
            using (var connection = new SqlConnection(authConnectionString))
            {
                await connection.OpenAsync();

                const string sql = """
                SELECT
                    SqlServerName,
                    SqlUserName,
                    SqlPasswordEncrypted
                FROM Users
                WHERE Id = @Id
                """;

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", userId);
                using var reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    return null;
                }

                serverName = reader["SqlServerName"]?.ToString() ?? "";
                sqlUserName = reader["SqlUserName"]?.ToString() ?? "";
                encryptedPassword = reader["SqlPasswordEncrypted"]?.ToString() ?? "";
            }

            // =========================================
            // 2. Validate
            // =========================================
            if (string.IsNullOrWhiteSpace(serverName) || string.IsNullOrWhiteSpace(databaseName) || string.IsNullOrWhiteSpace(sqlUserName) || string.IsNullOrWhiteSpace(encryptedPassword))
            {
                return null;
            }

            // =========================================
            // 3. Decrypt password
            // TEMPORARY Base64
            // =========================================
            string sqlPassword = Encoding.UTF8.GetString(Convert.FromBase64String(encryptedPassword));

            // =========================================
            // 4. Build customer DB connection string
            // =========================================
            var builder =
                new SqlConnectionStringBuilder
                {
                    DataSource = serverName,
                    InitialCatalog = databaseName,
                    UserID = sqlUserName,
                    Password = sqlPassword,

                    Encrypt = true,
                    TrustServerCertificate = true,
                    MultipleActiveResultSets = true
                };

            string connectionString = builder.ConnectionString;

            // =========================================
            // 5. Create DBConn
            // =========================================
            return new DBConn(connectionString);
        }
    }
}