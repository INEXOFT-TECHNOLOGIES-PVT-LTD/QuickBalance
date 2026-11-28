# Sales Order 500 - review input

Error: "BeginExecuteReader requires an open and available Connection. The connection's current state is closed."

## Repository/Transactions/Sales/SalesOrderRepository.cs

```csharp
using System.Data;
using QuickBalance.API.CLS;
using QuickBalance.API.Configuration;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Respons_DTO;

namespace QuickBalance.API.Repository.Transactions.Sales
{
    /// <summary>
    /// The only Sales Order class that talks to the database.
    /// Phase 1: it owns the connection (opened, settings loaded, always disposed) and calls the existing
    /// DB classes (SalesOrderDB, CordMDB, CordTDB, CordDDB), whose SQL depends on the company settings
    /// (date format, decimals, document number field, branch filter) loaded by CommonFunction.
    /// No validation and no messages here - those live in SalesOrderService.
    /// </summary>
    public sealed class SalesOrderRepository
    {
        private readonly DatabaseConnection _databaseConnection;

        public SalesOrderRepository(DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }

        // ------------------------------------------------------------------
        // Save (create when SordNo = 0, update when SordNo > 0)
        // ------------------------------------------------------------------
        public async Task<SalesOrderProcedureResult> SaveAsync(SalesOrderRequest request, int userId)
        {
            Variable variables = NewVariables(userId);
            using DBConn dbConn = await OpenAsync(variables);

            DataTable result = await new SalesOrderDB(dbConn).ManageSalesOrder(request, userId);
            return ReadProcedureResult(result);
        }

        // ------------------------------------------------------------------
        // Cancel (soft cancel)
        // ------------------------------------------------------------------
        public async Task<SalesOrderProcedureResult> CancelAsync(int orderNo, int userId)
        {
            Variable variables = NewVariables(userId);
            using DBConn dbConn = await OpenAsync(variables);

            DataTable result = await new SalesOrderDB(dbConn).CancelSalesOrder(orderNo, userId);
            return ReadProcedureResult(result);
        }

        // ------------------------------------------------------------------
        // List (paged). Returns the page rows (TotalRecords column removed) and the total.
        // ------------------------------------------------------------------
        public async Task<(DataTable Rows, int TotalRecords)> GetListAsync(
            int userId, int page, int pageSize, string? search, DateTime? fromDate, DateTime? toDate)
        {
            Variable variables = NewVariables(userId);
            variables.Page = page;
            variables.PageSize = pageSize;
            variables.Search = search;
            variables.FromDate = fromDate;
            variables.ToDate = toDate;

            using DBConn dbConn = await OpenAsync(variables);

            CordMDB cordMDB = new CordMDB(dbConn);
            cordMDB.Variables = variables;
            cordMDB.Functions.Variables = variables;

            DataTable rows = await cordMDB.ListAsync("O");
            return (rows, TakeTotalRecords(rows));
        }

        // ------------------------------------------------------------------
        // One order: header, lines, extra charges. null = not found.
        // ------------------------------------------------------------------
        public async Task<SelectedSalesOrderData?> GetByIdAsync(int orderNo, int userId)
        {
            Variable variables = NewVariables(userId);
            using DBConn dbConn = await OpenAsync(variables);

            var cordMDB = new CordMDB(dbConn) { Variables = variables };
            cordMDB.Functions.Variables = variables;

            var cordTDB = new CordTDB(dbConn) { Variables = variables };
            cordTDB.Functions.Variables = variables;

            var cordDDB = new CordDDB(dbConn) { Variables = variables };
            cordDDB.Functions.Variables = variables;

            var header = await cordMDB.SearchByOrdNOAsync(new ClsCordM { OrdNo = orderNo, SType = "O" });
            if (header == null)
                return null;

            var items = await cordTDB.SearchByOrdNoAsync(new ClsCordT { OrdNo = orderNo, SType = "O" });
            var additionalInfo = await cordDDB.SearchAddInfoAsync(new ClsCordD { VrNo = orderNo, VrType = "O", DFlag = false });

            return new SelectedSalesOrderData
            {
                Header = header,
                Items = items,
                AdditionalInformations = additionalInfo
            };
        }

        // ------------------------------------------------------------------
        // Pending orders for invoicing (header list or lines of one order).
        // ------------------------------------------------------------------
        public async Task<(DataTable Rows, int TotalRecords)> GetPendingAsync(PaginationRequest request, int userId)
        {
            Variable variables = NewVariables(userId);
            variables.pgRequest = request;

            using DBConn dbConn = await OpenAsync(variables);

            DataTable rows = await new SalesOrderDB(dbConn).PendingSalesOrderInInvoices(request);
            return (rows, TakeTotalRecords(rows));
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------
        private static Variable NewVariables(int userId)
        {
            LngTB.InitLng();
            Variable variables = new Variable();
            variables.UserId = userId;
            return variables;
        }

        // Same steps the old controller did, in one place. The caller disposes the DBConn ("using").
        private async Task<DBConn> OpenAsync(Variable variables)
        {
            string connectionString = _databaseConnection.GetConnectionString(variables);
            DBConn dbConn = new DBConn(connectionString);

            try
            {
                CommonFunction commonFunction = new CommonFunction(dbConn);

                if (!await commonFunction.CheckDatabaseConnection())
                    throw new DatabaseUnavailableException();

                await commonFunction.SetVariableValues(variables);
                commonFunction.Variables = variables;
                commonFunction.Functions.Variables = variables;

                return dbConn;
            }
            catch
            {
                dbConn.Dispose();   // never leak the connection if opening / settings fail
                throw;
            }
        }

        // The list queries return the total in every row (COUNT(*) OVER()); take it once and drop the column.
        private static int TakeTotalRecords(DataTable rows)
        {
            int totalRecords = 0;

            if (rows.Columns.Contains("TotalRecords"))
            {
                if (rows.Rows.Count > 0)
                    totalRecords = Convert.ToInt32(rows.Rows[0]["TotalRecords"]);
                rows.Columns.Remove("TotalRecords");
            }

            return totalRecords;
        }

        private static SalesOrderProcedureResult ReadProcedureResult(DataTable result)
        {
            if (result.Rows.Count == 0)
                return new SalesOrderProcedureResult { HasResponse = false };

            DataRow row = result.Rows[0];
            return new SalesOrderProcedureResult
            {
                HasResponse = true,
                Success = Convert.ToInt32(row["STATUS"]) == 1,
                Message = Convert.ToString(row["MESSAGE"]) ?? string.Empty
            };
        }
    }

    /// <summary>MESSAGE / STATUS returned by the save and cancel procedures.</summary>
    public sealed class SalesOrderProcedureResult
    {
        public bool HasResponse { get; init; }
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
    }

    /// <summary>Thrown when the company database cannot be reached.</summary>
    public sealed class DatabaseUnavailableException : Exception
    {
        public DatabaseUnavailableException() : base("Database connection failed.") { }
    }
}
```

## DB/SalesOrderDB.cs

```csharp
using Microsoft.Data.SqlClient;
using QuickBalance.API.DTOs;
using System.Data;

namespace QuickBalance.API.DB
{
    public class SalesOrderDB
    {
        private readonly DBConn db;

        DataTable itemDetails = new();

        DataTable additionalDetails = new();

        public SalesOrderDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public void CreateItemTable(SalesOrderRequest request)
        {
            itemDetails = new();
            itemDetails.Columns.Add("SLNO", typeof(int));
            itemDetails.Columns.Add("PRCODE", typeof(int));
            itemDetails.Columns.Add("QTY", typeof(decimal));
            itemDetails.Columns.Add("RATE", typeof(decimal));
            itemDetails.Columns.Add("LDISCPERC", typeof(decimal));
            itemDetails.Columns.Add("LDISCPRICE", typeof(decimal));
            itemDetails.Columns.Add("DISCPERC", typeof(decimal));
            itemDetails.Columns.Add("DISCPRICE", typeof(decimal));
            itemDetails.Columns.Add("UNITCODE", typeof(int));
            itemDetails.Columns.Add("GDWCODE", typeof(int));
            itemDetails.Columns.Add("ALCODE", typeof(string));
            itemDetails.Columns.Add("ALNAME", typeof(string));
            itemDetails.Columns.Add("CONVTYPE", typeof(int));
            itemDetails.Columns.Add("CONVFACT", typeof(decimal));
            itemDetails.Columns.Add("TAXABLE", typeof(decimal));
            itemDetails.Columns.Add("TAXCODE", typeof(int));
            itemDetails.Columns.Add("TAXPERC", typeof(decimal));
            itemDetails.Columns.Add("TAXAMOUNT", typeof(decimal));
            itemDetails.Columns.Add("NETAMOUNT", typeof(decimal));

            List<SalesOrderItemRequest> items = request.Items ?? new List<SalesOrderItemRequest>();

            foreach (var item in items)
            {
                item.AlCode = "0";
                item.AlName = "";
                item.GdwCode = item.GdwCode == 0 ? 1 : item.GdwCode;
                item.ConvFact = item.ConvFact == 0 ? 1 : item.ConvFact;
                item.NetAmount = item.Taxable + item.TaxAmount;

                itemDetails.Rows.Add(
                    item.SlNo,
                    item.PrCode,
                    item.Qty,
                    item.Rate,
                    item.LDiscPerc,
                    item.LDiscPrice,
                    item.DiscPerc,
                    item.DiscPrice,
                    item.UnitCode,
                    item.GdwCode,
                    item.AlCode ?? string.Empty,
                    item.AlName ?? string.Empty,
                    item.ConvType,
                    item.ConvFact,
                    item.Taxable,
                    item.TaxCode,
                    item.TaxPerc,
                    item.TaxAmount,
                    item.NetAmount
                );
            }

            //--

            additionalDetails = new();
            additionalDetails.Columns.Add("ACCID", typeof(int));
            additionalDetails.Columns.Add("AMOUNT", typeof(decimal));

            List<SalesOrderAdditionalRequest> additional = request.Additional ?? new List<SalesOrderAdditionalRequest>();

            foreach (var item in additional)
            {
                additionalDetails.Rows.Add(
                    item.AccId,
                    item.Amount
                );
            }
        }

        public async Task<DataTable> ManageSalesOrder(SalesOrderRequest request, int UserId)
        {
            CreateItemTable(request);

            List<SqlParameter> parameters = new();

            parameters.Add(new SqlParameter("@SORDNO", SqlDbType.Int) { Value = request.SordNo });
            parameters.Add(new SqlParameter("@DATE", SqlDbType.DateTime) { Value = request.Date });
            parameters.Add(new SqlParameter("@CUSCODE", SqlDbType.Int) { Value = request.CustomerCode });
            parameters.Add(new SqlParameter("@SMCODE", SqlDbType.Int) { Value = request.SalesmanCode });
            parameters.Add(new SqlParameter("@DISCPERC", SqlDbType.Decimal) { Precision = 18, Scale = 6, Value = request.DiscPerc });
            parameters.Add(new SqlParameter("@DISCAMT", SqlDbType.Decimal) { Precision = 18, Scale = 6, Value = request.DiscAmt });
            parameters.Add(new SqlParameter("@ROUNDOFF", SqlDbType.Decimal) { Precision = 18, Scale = 6, Value = request.RoundOff });
            parameters.Add(new SqlParameter("@USERID", SqlDbType.Int) { Value = UserId });

            SqlParameter itemParameter = new SqlParameter("@ITEMDTLS", SqlDbType.Structured) { TypeName = "dbo.ASK_UDT_SalesOrderItem", Value = itemDetails };
            parameters.Add(itemParameter);

            SqlParameter additionalParameter = new SqlParameter("@ADDIDTLS", SqlDbType.Structured) { TypeName = "dbo.ASK_UDT_SalesOrderAdditionalInfo", Value = additionalDetails };
            parameters.Add(additionalParameter);

            return await db.ExecuteStoredProcedure("ASK_SP_ManageSalesOrderDetails", parameters);
        }

        public async Task<DataTable> CancelSalesOrder(int SordNo, int UserId)
        {
            List<SqlParameter> parameters = new();

            parameters.Add(new SqlParameter("@SORDNO", SqlDbType.Int) { Value = SordNo });
            parameters.Add(new SqlParameter("@USERID", SqlDbType.Int) { Value = UserId });

            return await db.ExecuteStoredProcedure("ASK_SP_CancelSalesOrderDetails", parameters);
        }

        public async Task<DataTable> PendingSalesOrderInInvoices(PaginationRequest request)
        {
            List<SqlParameter> parameters = new();

            parameters.Add(new SqlParameter("@CUSTOMERID", SqlDbType.Int) { Value = request.CustomerId });
            parameters.Add(new SqlParameter("@ORDERID", SqlDbType.Int) { Value = request.OrderId });
            parameters.Add(new SqlParameter("@PAGE", SqlDbType.Int) { Value = request.Page });
            parameters.Add(new SqlParameter("@PAGESIZE", SqlDbType.Int) { Value = request.PageSize });
            parameters.Add(new SqlParameter("@SEARCH", SqlDbType.NVarChar, -1) { Value = request.Search ?? "" });
            // Always send a valid sort. Before, no sortBy sent "" -> the procedure had no ORDER BY match,
            // so OFFSET/FETCH paging returned rows in random order (duplicates / missing rows between pages).
            string sortBy = (request.SortBy ?? string.Empty).Trim().ToLowerInvariant();
            string procedureSortBy = sortBy == "ordno" || sortBy == "orderno" || sortBy == "no" ? "no" : "date";
            string procedureSortDirection = string.Equals(request.SortDirection, "ASC", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";

            parameters.Add(new SqlParameter("@SORTBY", SqlDbType.NVarChar, 50) { Value = procedureSortBy });
            parameters.Add(new SqlParameter("@SORTDIRECTION", SqlDbType.NVarChar, 50) { Value = procedureSortDirection });

            return await db.ExecuteStoredProcedure("ASK_SP_PendingSalesOrderInInvoices", parameters);
        }
    }
}
```

## DB/DBConn.cs (class DBConn, full file)

```csharp

using Dapper;
using Microsoft.Data.SqlClient;
using QuickBalance.API.Configuration;
using System.Data;

namespace QuickBalance.API.DB
{
    public class DBConn : IDisposable
    {
        private string _connectionString;
        private SqlConnection? con;
        private SqlCommand? dbCommand;
        private SqlDataAdapter? dbAdapter;
        private SqlTransaction? _transaction;

        public string ConnectionString => _connectionString;


        private readonly IDbConnectionFactory? _connectionFactory;

        public DBConn(string connectionString)
        {
            _connectionString = connectionString
                ?? throw new InvalidOperationException(
                    "Connection string is required.");
        }

        public DBConn(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory
                ?? throw new ArgumentNullException(nameof(connectionFactory));

            var variables = new Variable();

            _connectionString = _connectionFactory.GetConnectionString(
                variables,
                isMasterDB: false);
        }


        private SqlConnection CreateMasterConnection()
        {
            if (_connectionFactory == null)
            {
                throw new InvalidOperationException(
                    "Master database connection factory is not configured.");
            }

            return _connectionFactory.CreateConnection(
                new Variable(),
                isMasterDB: true);
        }


        // =========================================================
        // MASTER DATABASE - QUERY LIST
        // =========================================================
        public async Task<List<T>> QueryMasterListAsync<T>(
            string query,
            object? parameters = null)
        {
            await using var connection = CreateMasterConnection();

            var result = await connection.QueryAsync<T>(
                new CommandDefinition(
                    query,
                    parameters,
                    commandTimeout: 60));

            return result.AsList();
        }

        // =========================================================
        // MASTER DATABASE - QUERY FIRST
        // =========================================================
        public async Task<T?> QueryMasterFirstOrDefaultAsync<T>(
            string query,
            object? parameters = null)
        {
            await using var connection = CreateMasterConnection();

            return await connection.QueryFirstOrDefaultAsync<T>(
                new CommandDefinition(
                    query,
                    parameters,
                    commandTimeout: 60));
        }

        // =========================================================
        // MASTER DATABASE - EXECUTE
        // =========================================================
        public async Task<int> ExecuteMasterAsync(
            string query,
            object? parameters = null)
        {
            await using var connection = CreateMasterConnection();

            return await connection.ExecuteAsync(
                new CommandDefinition(
                    query,
                    parameters,
                    commandTimeout: 60));
        }

        // =========================================================
        // MASTER DATABASE - INSERT AND RETURN ID
        // =========================================================
        public async Task<T?> QueryMasterScalarAsync<T>(
            string query,
            object? parameters = null)
        {
            await using var connection = CreateMasterConnection();

            return await connection.QueryFirstOrDefaultAsync<T>(
                new CommandDefinition(
                    query,
                    parameters,
                    commandTimeout: 60));
        }
        //public DBConn(string connectionString)
        //{
        //    _connectionString = connectionString
        //        ?? throw new InvalidOperationException(
        //            "Connection string is required.");
        //}

        // =========================================================
        // OPEN CONNECTION
        // =========================================================
        public async Task OpenConnection()
        {
            if (con == null)
            {
                con = new SqlConnection(_connectionString);
            }

            if (con.State != ConnectionState.Open)
            {
                await con.OpenAsync();
            }
        }


        // =========================================================
        // CHANGE CURRENT CONNECTION
        // =========================================================
        //public void SetCurrentConnection(string connectionString)
        //{
        //    if (_transaction != null)
        //    {
        //        throw new InvalidOperationException(
        //            "Cannot change the connection during an active transaction.");
        //    }

        //    CloseConnection();

        //    _connectionString = connectionString
        //        ?? throw new InvalidOperationException(
        //            "Connection string is required.");

        //    if (con != null)
        //    {
        //        con.ConnectionString = _connectionString;
        //    }
        //}

        public void SetCurrentConnection(string connectionString)
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException(
                    "Cannot change the connection during an active transaction.");
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string is required.");
            }

            con?.Dispose();
            con = null;

            _connectionString = connectionString;
        }

        // =========================================================
        // ACTIVE TRANSACTION CONNECTION
        // =========================================================
        public SqlConnection TransactionConnections
        {
            get
            {
                if (con == null || con.State != ConnectionState.Open)
                {
                    throw new InvalidOperationException(
                        "Transaction connection is not open.");
                }

                return con;
            }
        }

        // =========================================================
        // ACTIVE TRANSACTION
        // =========================================================
        public SqlTransaction ActiveTransaction
        {
            get
            {
                return _transaction
                    ?? throw new InvalidOperationException(
                        "No active database transaction.");
            }
        }

        // =========================================================
        // CLOSE CONNECTION
        // =========================================================
        public void CloseConnection()
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException(
                    "Cannot close the connection during an active transaction.");
            }

            try
            {
                if (con != null &&
                    con.State != ConnectionState.Closed)
                {
                    con.Close();
                }
            }
            catch
            {
                // Preserve existing connection cleanup behavior.
            }
        }

        // =========================================================
        // GET CONNECTION
        // =========================================================
        //public SqlConnection GetConnection()
        //{
        //    if (con == null)
        //    {
        //        con = new SqlConnection(_connectionString);
        //    }

        //    return con;
        //}
        public SqlConnection GetConnection()
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException(
                    "DBConn connection string is empty.");
            }

            return new SqlConnection(_connectionString);
        }
        // =========================================================
        // CREATE COMMAND
        // Automatically attaches the active transaction.
        // =========================================================
        private SqlCommand CreateCommand(string qry)
        {
            var command = new SqlCommand(qry, GetConnection())
            {
                CommandType = CommandType.Text,
                CommandTimeout = 180
            };

            if (_transaction != null)
            {
                command.Transaction = _transaction;
            }

            return command;
        }

        // =========================================================
        // BEGIN TRANSACTION
        //// =========================================================
        //public async Task BeginTransaction()
        //{
        //    if (_transaction != null)
        //    {
        //        throw new InvalidOperationException(
        //            "A database transaction is already active.");
        //    }

        //    await OpenConnection();

        //    _transaction = GetConnection().BeginTransaction();
        //}

        //// =========================================================
        //// COMMIT TRANSACTION
        //// =========================================================
        //public void CommitTransaction()
        //{
        //    if (_transaction == null)
        //    {
        //        throw new InvalidOperationException(
        //            "No active database transaction to commit.");
        //    }

        //    var transaction = _transaction;

        //    try
        //    {
        //        transaction.Commit();
        //    }
        //    finally
        //    {
        //        _transaction = null;
        //        transaction.Dispose();
        //    }
        //}

        //// =========================================================
        //// ROLLBACK TRANSACTION
        //// =========================================================
        //public void RollbackTransaction()
        //{
        //    if (_transaction == null)
        //    {
        //        return;
        //    }

        //    var transaction = _transaction;

        //    try
        //    {
        //        transaction.Rollback();
        //    }
        //    finally
        //    {
        //        _transaction = null;
        //        transaction.Dispose();
        //    }
        //}

        // =========================================================
        // BEGIN TRANSACTION
        // =========================================================
        public async Task BeginTransaction()
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException(
                    "A database transaction is already active.");
            }

            await OpenConnection();

            _transaction = con!.BeginTransaction();
        }

        // =========================================================
        // COMMIT TRANSACTION
        // =========================================================
        public void CommitTransaction()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException(
                    "No active database transaction to commit.");
            }

            try
            {
                _transaction.Commit();
            }
            finally
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }

        // =========================================================
        // ROLLBACK TRANSACTION
        // =========================================================
        public void RollbackTransaction()
        {
            if (_transaction == null)
                return;

            try
            {
                _transaction.Rollback();
            }
            finally
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }

        // =========================================================
        // EXECUTE QUERY
        // =========================================================
        public async Task<int> Execute(string qry)
        {
            try
            {
                await OpenConnection();

                dbCommand?.Dispose();
                dbCommand = CreateCommand(qry);

                return await dbCommand.ExecuteNonQueryAsync();
            }
            catch
            {
                // Preserve existing error convention.
                return -5;
            }
        }

        // =========================================================
        // EXECUTE SCALAR
        // =========================================================
        public async Task<object?> ExecuteScalar(string qry)
        {
            try
            {
                await OpenConnection();

                dbCommand?.Dispose();
                dbCommand = CreateCommand(qry);

                return await dbCommand.ExecuteScalarAsync();
            }
            catch
            {
                return null;
            }
        }

        // =========================================================
        // GET NEW ID
        // =========================================================
        public async Task<int> GetnewId(
            string field,
            string table)
        {
            try
            {
                await OpenConnection();

                string qry = $"SELECT MAX({field}) FROM {table}";

                using var command = CreateCommand(qry);

                var result = await command.ExecuteScalarAsync();

                if (result == null || result == DBNull.Value)
                {
                    return 1;
                }

                return Convert.ToInt32(result) + 1;
            }
            catch
            {
                return 1;
            }
        }

        // =========================================================
        // GET NEW ID BY CONDITION
        // =========================================================
        public async Task<int> GetnewIdByCondition(
            string field,
            string table,
            string condition)
        {
            try
            {
                await OpenConnection();

                string qry =
                    $"SELECT MAX({field}) FROM {table} {condition}";

                using var command = CreateCommand(qry);

                var result = await command.ExecuteScalarAsync();

                if (result == null || result == DBNull.Value)
                {
                    return 1;
                }

                return Convert.ToInt32(result) + 1;
            }
            catch
            {
                return 1;
            }
        }

        // =========================================================
        // INSERT SCALAR
        // =========================================================
        public async Task<int> InsertScalar(string qry)
        {
            await OpenConnection();

            using var command = CreateCommand(qry);

            var result = await command.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
            {
                throw new InvalidOperationException(
                    "The database insert did not return a value.");
            }

            return Convert.ToInt32(result);
        }

        // =========================================================
        // GET DATATABLE
        // Uses a separate connection, as in the existing code.
        // =========================================================
        public async Task<DataTable> GetDatatable(string qry)
        {
            var dt = new DataTable();

            await using var connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync();

            using var command = new SqlCommand(qry, connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 180
            };

            using var reader = await command.ExecuteReaderAsync();

            dt.Load(reader);

            return dt;
        }

        /// Salman added below new method for handle  db injection bloack
        public async Task<DataTable> GetDatatable(string qry, params SqlParameter[] parameters)
        {
            var dt = new DataTable();

            await using var connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync();

            await using var command = new SqlCommand(qry, connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 180
            };

            if (parameters != null && parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }

            await using var reader = await command.ExecuteReaderAsync();

            dt.Load(reader);

            return dt;
        }

        // =========================================================
        // GET DATATABLE WITH PARAMETERS
        // =========================================================
        public async Task<DataTable> GetDatatable(
            string qry,
            IEnumerable<SqlParameter> parameters)
        {
            var dt = new DataTable();

            await using var connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync();

            using var command = new SqlCommand(qry, connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 180
            };

            if (parameters != null)
            {
                foreach (SqlParameter parameter in parameters)
                {
                    command.Parameters.Add(CloneSqlParameter(parameter));
                }
            }

            using var reader = await command.ExecuteReaderAsync();

            dt.Load(reader);

            return dt;
        }

        public async Task<int> ExecuteWithParams(string qry, params SqlParameter[] parameters)
        {
            await OpenConnection();
            using var command = CreateCommand(qry);
            if (parameters != null)
            {
                foreach (var p in parameters)
                    command.Parameters.Add(p);
            }
            return await command.ExecuteNonQueryAsync();
        }

        // =========================================================
        // EXECUTE SCALAR INTEGER WITH PARAMETERS
        // =========================================================
        public async Task<int> ExecuteScalarIntAsync(
            string qry,
            IEnumerable<SqlParameter> parameters)
        {
            await OpenConnection();

            using var command = CreateCommand(qry);

            if (parameters != null)
            {
                foreach (SqlParameter parameter in parameters)
                {
                    command.Parameters.Add(CloneSqlParameter(parameter));
                }
            }

            object? result = await command.ExecuteScalarAsync();

            return result == null || result == DBNull.Value
                ? 0
                : Convert.ToInt32(result);
        }

        // =========================================================
        // CLONE SQL PARAMETER
        // =========================================================
        private static SqlParameter CloneSqlParameter(
            SqlParameter parameter)
        {
            return new SqlParameter
            {
                ParameterName = parameter.ParameterName,
                SqlDbType = parameter.SqlDbType,
                Direction = parameter.Direction,
                IsNullable = parameter.IsNullable,
                Size = parameter.Size,
                Precision = parameter.Precision,
                Scale = parameter.Scale,
                Value = parameter.Value ?? DBNull.Value
            };
        }

        // =========================================================
        // RETURN INTEGER
        // =========================================================
        public async Task<int> ReturnIntValue(string qry)
        {
            try
            {
                await OpenConnection();

                using var command = CreateCommand(qry);

                var result = await command.ExecuteScalarAsync();

                if (result == null || result == DBNull.Value)
                {
                    return 0;
                }

                return Convert.ToInt32(result);
            }
            catch
            {
                return -1;
            }
        }

        // =========================================================
        // RETURN DECIMAL
        // =========================================================
        public async Task<decimal> ReturnDecValue(string qry)
        {
            try
            {
                await OpenConnection();

                using var command = CreateCommand(qry);

                var result = await command.ExecuteScalarAsync();

                if (result == null || result == DBNull.Value)
                {
                    return 0;
                }

                return Convert.ToDecimal(result);
            }
            catch
            {
                return 0;
            }
        }

        // =========================================================
        // RETURN STRING
        // =========================================================
        public async Task<string> ReturnStringValue(string qry)
        {
            try
            {
                await OpenConnection();

                using var command = CreateCommand(qry);

                var result = await command.ExecuteScalarAsync();

                if (result == null || result == DBNull.Value)
                {
                    return "";
                }

                return result.ToString() ?? "";
            }
            catch
            {
                return "Error";
            }
        }

        // =========================================================
        // GET DATA FROM STORED PROCEDURE
        // =========================================================
        public async Task<DataTable> GetDataTableFromProcedure(
            string procedureName)
        {
            DataTable dt = new DataTable();

            try
            {
                await OpenConnection();

                using var command = CreateCommand(procedureName);
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 0;

                using var adapter = new SqlDataAdapter(command);

                await Task.Run(() => adapter.Fill(dt));
            }
            catch
            {
                // Preserve existing behavior: return an empty table.
            }

            return dt;
        }

        // =========================================================
        // GET DATASET
        // =========================================================
        public async Task<DataSet> GetDataSet(string qry)
        {
            DataSet ds = new DataSet();

            try
            {
                await OpenConnection();

                using var command = CreateCommand(qry);
                using var adapter = new SqlDataAdapter(command);

                await Task.Run(() => adapter.Fill(ds));
            }
            catch
            {
                // Preserve existing behavior: return an empty dataset.
            }

            return ds;
        }

        // =========================================================
        // EXECUTE STORED PROCEDURE
        // =========================================================
        public async Task<DataTable> ExecuteStoredProcedure(
            string procedureName,
            List<SqlParameter> parameters)
        {
            await OpenConnection();

            using var command = CreateCommand(procedureName);
            command.CommandType = CommandType.StoredProcedure;

            if (parameters != null && parameters.Count > 0)
            {
                foreach (SqlParameter parameter in parameters)
                {
                    command.Parameters.Add(CloneSqlParameter(parameter));
                }
            }

            DataTable table = new DataTable();

            using SqlDataReader reader =
                await command.ExecuteReaderAsync();

            table.Load(reader);

            return table;
        }

        // =========================================================
        // DISPOSE
        // =========================================================
        public void Dispose()
        {
            try
            {
                RollbackTransaction();
            }
            catch
            {
                // Continue disposing resources.
            }

            dbCommand?.Dispose();
            dbAdapter?.Dispose();
            con?.Dispose();

            dbCommand = null;
            dbAdapter = null;
            con = null;
        }


        public async Task<List<T>> QueryListAsync<T>(string query, object? parameters = null)
        {
            await using var connection =
                new SqlConnection(_connectionString);

            var result = await connection.QueryAsync<T>(
                new CommandDefinition(
                    query,
                    parameters,
                    commandTimeout: 60));

            return result.AsList();
        }

        public async Task<T?> QueryFirstOrDefaultAsync<T>(string query, object? parameters = null)
        {
            await using var connection =
                new SqlConnection(_connectionString);

            return await connection.QueryFirstOrDefaultAsync<T>(
                new CommandDefinition(
                    query,
                    parameters,
                    commandTimeout: 60));
        }

        public async Task<T?> QuerySingleOrDefaultAsync<T>(string query, object? parameters = null)
        {
            await using var connection =
                new SqlConnection(_connectionString);

            return await connection.QuerySingleOrDefaultAsync<T>(
                new CommandDefinition(
                    query,
                    parameters,
                    commandTimeout: 60));
        }

        public async Task<int> ExecuteAsync(string query, object? parameters = null)
        {
            await using var connection = new SqlConnection(_connectionString);

            return await connection.ExecuteAsync(
                new CommandDefinition(
                    query,
                    parameters,
                    commandTimeout: 60));
        }


        public async Task<SqlMapper.GridReader> QueryMultipleAsync(
     string query,
     object? parameters = null)
        {
            var connection = new SqlConnection(_connectionString);

            try
            {
                var result = await connection.QueryMultipleAsync(
                    new CommandDefinition(
                        query,
                        parameters,
                        commandTimeout: 60));

                return result;
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }

        //    public async Task<SqlMapper.GridReader> QueryMultipleAsync(
        //string query,
        //object? parameters = null)
        //    {
        //        var connection = new SqlConnection(_connectionString);

        //        try
        //        {
        //            //await connection.OpenAsync();

        //            var result = await connection.QueryMultipleAsync(
        //                new CommandDefinition(
        //                    query,
        //                    parameters,
        //                    commandTimeout: 60));

        //            return result;
        //        }
        //        catch
        //        {
        //            await connection.DisposeAsync();
        //            throw;
        //        }
        //    }
    }
}

//using Dapper;
//using Microsoft.Data.SqlClient;
//using System.Data;

//namespace QuickBalance.API.DB
//{
//    public class DBConn
//    {
//        private string _connectionString;
//        private SqlConnection? con;
//        public string ConnectionString => _connectionString;

//        public DBConn(string connectionString)
//        {
//            _connectionString = connectionString ?? throw new InvalidOperationException("Connection string is required.");
//        }

//        // =========================================================
//        // Open Connection
//        // =========================================================

//        public async Task OpenConnection()
//        {
//            try
//            {
//                if (con == null)
//                {
//                    con = new SqlConnection(_connectionString);
//                }

//                if (con.State != ConnectionState.Open)
//                {
//                    await con.OpenAsync();
//                }
//            }
//            catch (Exception ex)
//            {
//                throw;
//            }
//        }

//        // =========================================================
//        // CHANGE CURRENT CONNECTION
//        // =========================================================
//        public void SetCurrentConnection(string connectionString)
//        {
//            CloseConnection();

//            _connectionString = connectionString ?? throw new InvalidOperationException("Connection string is required.");

//            if (con != null)
//            {
//                con.ConnectionString = _connectionString;
//            }
//        }

//        // =========================================================
//        // Close Connection
//        // =========================================================

//        public void CloseConnection()
//        {
//            try
//            {
//                if (con != null && con.State == ConnectionState.Open)
//                {
//                    con.Close();
//                }
//            }
//            catch (Exception ex)
//            {
//                // Keep same behavior as desktop application
//            }
//        }

//        // =========================================================
//        // Get Connection
//        // =========================================================

//        public SqlConnection GetConnection()
//        {
//            if (con == null)
//            {
//                con = new SqlConnection(_connectionString);
//            }

//            return con;
//        }

//        // =========================================================
//        // Execute Query
//        // =========================================================

//        public async Task<int> Execute(string qry)
//        {
//            try
//            {
//                await OpenConnection();

//                using var command = new SqlCommand(qry, GetConnection());
//                command.CommandType = CommandType.Text;
//                command.CommandTimeout = 180;
//                return await command.ExecuteNonQueryAsync();
//            }
//            catch (Exception ex)
//            {
//                return -5;
//            }
//        }

//        // =========================================================
//        // Execute Scalar
//        // =========================================================

//        public async Task<object?> ExecuteScalar(string qry)
//        {
//            try
//            {
//                await OpenConnection();

//                using var command = new SqlCommand(qry, GetConnection());

//                command.CommandType = CommandType.Text;
//                command.CommandTimeout = 180;

//                return await command.ExecuteScalarAsync();
//            }
//            catch (Exception ex)
//            {
//                return null;
//            }
//        }

//        // =========================================================
//        // Get New ID
//        // =========================================================

//        public async Task<int> GetnewId(string field, string table)
//        {
//            try
//            {
//                await OpenConnection();

//                string qry = $"SELECT MAX({field}) FROM {table}";

//                using var command = new SqlCommand(qry, GetConnection());

//                command.CommandType = CommandType.Text;
//                command.CommandTimeout = 180;

//                var result = await command.ExecuteScalarAsync();

//                if (result == null || result == DBNull.Value)
//                {
//                    return 1;
//                }

//                return Convert.ToInt32(result) + 1;
//            }
//            catch (Exception ex)
//            {
//                return 1;
//            }
//        }

//        // =========================================================
//        // Get New ID By Condition
//        // =========================================================

//        public async Task<int> GetnewIdByCondition(string field, string table, string condition)
//        {
//            try
//            {
//                await OpenConnection();

//                string qry = $"SELECT MAX({field}) FROM {table} {condition}";

//                using var command = new SqlCommand(qry, GetConnection());

//                command.CommandType = CommandType.Text;
//                command.CommandTimeout = 180;

//                var result = await command.ExecuteScalarAsync();

//                if (result == null || result == DBNull.Value)
//                {
//                    return 1;
//                }

//                return Convert.ToInt32(result) + 1;
//            }
//            catch (Exception ex)
//            {
//                return 1;
//            }
//        }

//        // =========================================================
//        // Insert Scalar
//        // =========================================================

//        public async Task<int> InsertScalar(string qry)
//        {
//            try
//            {
//                await OpenConnection();

//                using var command = new SqlCommand(qry, GetConnection());

//                command.CommandType = CommandType.Text;
//                command.CommandTimeout = 180;

//                var result = await command.ExecuteScalarAsync();

//                if (result == null || result == DBNull.Value)
//                {
//                    return 0;
//                }

//                return Convert.ToInt32(result);
//            }
//            catch (Exception ex)
//            {
//                return -1;
//            }
//        }

//        // =========================================================
//        // Get DataTable
//        // =========================================================

//        public async Task<DataTable> GetDatatable(string qry)
//        {
//            DataTable dt = new DataTable();

//            try
//            {
//                await OpenConnection();

//                using var adapter = new SqlDataAdapter(qry, GetConnection());

//                await Task.Run(() =>
//                {
//                    adapter.Fill(dt);
//                });
//            }
//            catch (Exception ex)
//            {
//                // Return empty DataTable
//            }

//            return dt;
//        }


//        // =========================================================
//        // CLONE SQL PARAMETER
//        // =========================================================
//        private static SqlParameter CloneSqlParameter(
//            SqlParameter parameter)
//        {
//            return new SqlParameter
//            {
//                ParameterName = parameter.ParameterName,
//                SqlDbType = parameter.SqlDbType,
//                Direction = parameter.Direction,
//                IsNullable = parameter.IsNullable,
//                Size = parameter.Size,
//                Precision = parameter.Precision,
//                Scale = parameter.Scale,
//                Value = parameter.Value ?? DBNull.Value
//            };
//        }

//        // =========================================================
//        // Return Integer
//        // =========================================================
//        public async Task<DataTable> GetDatatable(
//           string qry,
//           IEnumerable<SqlParameter> parameters)
//        {
//            var dt = new DataTable();

//            await using var connection =
//                new SqlConnection(_connectionString);

//            await connection.OpenAsync();

//            using var command = new SqlCommand(qry, connection)
//            {
//                CommandType = CommandType.Text,
//                CommandTimeout = 180
//            };

//            if (parameters != null)
//            {
//                foreach (SqlParameter parameter in parameters)
//                {
//                    command.Parameters.Add(CloneSqlParameter(parameter));
//                }
//            }

//            using var reader = await command.ExecuteReaderAsync();

//            dt.Load(reader);

//            return dt;
//        }

//        public async Task<int> ReturnIntValue(string qry)
//        {
//            try
//            {
//                await OpenConnection();

//                using var command = new SqlCommand(qry, GetConnection());

//                command.CommandType = CommandType.Text;
//                command.CommandTimeout = 180;

//                var result = await command.ExecuteScalarAsync();

//                if (result == null || result == DBNull.Value)
//                {
//                    return 0;
//                }

//                return Convert.ToInt32(result);
//            }
//            catch (Exception ex)
//            {
//                return -1;
//            }
//        }

//        // =========================================================
//        // Return Decimal
//        // =========================================================

//        public async Task<decimal> ReturnDecValue(string qry)
//        {
//            try
//            {
//                await OpenConnection();

//                using var command = new SqlCommand(qry, GetConnection());

//                command.CommandType = CommandType.Text;
//                command.CommandTimeout = 180;

//                var result = await command.ExecuteScalarAsync();

//                if (result == null || result == DBNull.Value)
//                {
//                    return 0;
//                }

//                return Convert.ToDecimal(result);
//            }
//            catch (Exception ex)
//            {
//                return 0;
//            }
//        }

//        // =========================================================
//        // Return String
//        // =========================================================

//        public async Task<string> ReturnStringValue(string qry)
//        {
//            try
//            {
//                await OpenConnection();

//                using var command = new SqlCommand(qry, GetConnection());

//                command.CommandType = CommandType.Text;
//                command.CommandTimeout = 180;

//                var result = await command.ExecuteScalarAsync();

//                if (result == null || result == DBNull.Value)
//                {
//                    return "";
//                }

//                return result.ToString() ?? "";
//            }
//            catch (Exception ex)
//            {
//                return "Error";
//            }
//        }

//        // =========================================================
//        // Get Data From Stored Procedure
//        // =========================================================

//        public async Task<DataTable> GetDataTableFromProcedure(string procedureName)
//        {
//            DataTable dt = new DataTable();

//            try
//            {
//                await OpenConnection();

//                using var command = new SqlCommand(procedureName, GetConnection());
//                command.CommandType = CommandType.StoredProcedure;
//                command.CommandTimeout = 0;

//                using var adapter = new SqlDataAdapter(command);

//                await Task.Run(() =>
//                {
//                    adapter.Fill(dt);
//                });
//            }
//            catch (Exception ex)
//            {
//                // Return empty DataTable
//            }

//            return dt;
//        }

//        // =========================================================
//        // Get DataSet
//        // =========================================================

//        public async Task<DataSet> GetDataSet(string qry)
//        {
//            DataSet ds = new DataSet();

//            try
//            {
//                await OpenConnection();

//                using var adapter = new SqlDataAdapter(qry, GetConnection());

//                await Task.Run(() =>
//                {
//                    adapter.Fill(ds);
//                });
//            }
//            catch (Exception ex)
//            {
//                // Return empty DataSet
//            }

//            return ds;
//        }     

//        public async Task<DataTable> ExecuteStoredProcedure(string procedureName, List<SqlParameter> parameters)
//        {
//            try
//            {
//                await OpenConnection();
//                using SqlCommand command = new SqlCommand(procedureName, con);

//                command.CommandType = CommandType.StoredProcedure;
//                if (parameters != null && parameters.Count > 0)
//                    command.Parameters.AddRange(parameters.ToArray());

//                DataTable table = new DataTable();
//                using SqlDataReader reader = await command.ExecuteReaderAsync();
//                table.Load(reader);
//                return table;
//            }
//            catch (Exception ex)
//            {
//                return new DataTable();
//            }
//        }
//        public async Task<List<T>> QueryListAsync<T>(string query, object? parameters = null)
//        {
//            await using var connection =
//                new SqlConnection(_connectionString);

//            var result = await connection.QueryAsync<T>(
//                new CommandDefinition(
//                    query,
//                    parameters,
//                    commandTimeout: 60));

//            return result.AsList();
//        }

//        public async Task<T?> QueryFirstOrDefaultAsync<T>(string query, object? parameters = null)
//        {
//            await using var connection =
//                new SqlConnection(_connectionString);

//            return await connection.QueryFirstOrDefaultAsync<T>(
//                new CommandDefinition(
//                    query,
//                    parameters,
//                    commandTimeout: 60));
//        }

//        public async Task<T?> QuerySingleOrDefaultAsync<T>(string query, object? parameters = null)
//        {
//            await using var connection =
//                new SqlConnection(_connectionString);

//            return await connection.QuerySingleOrDefaultAsync<T>(
//                new CommandDefinition(
//                    query,
//                    parameters,
//                    commandTimeout: 60));
//        }

//        public async Task<int> ExecuteAsync(string query, object? parameters = null)
//        {
//            await using var connection = new SqlConnection(_connectionString);

//            return await connection.ExecuteAsync(
//                new CommandDefinition(
//                    query,
//                    parameters,
//                    commandTimeout: 60));
//        }
//    }
//}
```

## DB/CommonFunction.cs (lines 1-180: class header, fields, CheckDatabaseConnection, SetVariableValues, SearchByMNNameAndUser)

```csharp
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
```

## Services/SalesOrderService.cs (class header + SaveAsync, lines 14-69)

```csharp
public sealed class SalesOrderService : ISalesOrderInterface
{
    private readonly SalesOrderRepository _repository;
    private readonly ILogger<SalesOrderService> _logger;

    public SalesOrderService(SalesOrderRepository repository, ILogger<SalesOrderService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    // ------------------------------------------------------------------
    // SAVE
    // ------------------------------------------------------------------
    public async Task<SalesOrderServiceResult<object>> SaveAsync(SalesOrderRequest request, int userId)
    {
        try
        {
            ValidateOrder(request);

            SalesOrderProcedureResult result = await _repository.SaveAsync(request, userId);

            if (!result.HasResponse)
                return SalesOrderServiceResult<object>.FailedResult("No response received from sales order procedure.");

            if (result.Success)
            {
                // Same as before: message is the procedure's "Saved" / "Updated"; "Saved" = 201.
                return result.Message == "Saved"
                    ? SalesOrderServiceResult<object>.CreatedResult(result.Message)
                    : SalesOrderServiceResult<object>.Ok(result.Message);
            }

            if (result.Message.StartsWith("Error"))
            {
                _logger.LogError("Sales order save failed in the procedure: {Message}", result.Message);
                return SalesOrderServiceResult<object>.FailedResult("Could not save the sales order. Please try again or contact support.");
            }

            // Business rule (not found, cancelled, invoiced, unknown product/customer, outside party, ...).
            return SalesOrderServiceResult<object>.Invalid(result.Message);
        }
        catch (ArgumentException ex)
        {
            return SalesOrderServiceResult<object>.Invalid(ex.Message);
        }
        catch (DatabaseUnavailableException ex)
        {
            return SalesOrderServiceResult<object>.FailedResult(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving sales order {SordNo}.", request?.SordNo);
            return SalesOrderServiceResult<object>.FailedResult("An error occurred while saving the sales order.");
        }
    }
```

## Callers of SetVariableValues( (file:line)

- Controllers\AccountsController.cs:63
- Controllers\BatchController.cs:57
- Controllers\BillwisePurchaseSummaryController.cs:188
- Controllers\GroupwisePurchaseReportController.cs:57
- Controllers\LedgerGroupwisePurchaseReportController.cs:59
- Controllers\ProductwisePurchaseReportController.cs:62
- Controllers\PurchaseDetailReportController.cs:124
- Controllers\PurchaseSummaryController.cs:63
- Controllers\PurchaseTaxReportController.cs:59
- Controllers\PurchaseTaxReportDetailedController.cs:59
- Controllers\SerialNumberController.cs:60
- Controllers\SupplierWiseProductReportController.cs:124
- Controllers\Master\ProductControllerss.cs:119
- Controllers\Master\ProductControllerss.cs:328
- Controllers\Master\ProductControllerss.cs:581
- Controllers\Master\ProductControllerss.cs:945
- Controllers\Master\ProductControllerss.cs:1384
- Controllers\Master\ProductControllerss.cs:2752
- Controllers\Master\ProductControllerss.cs:2915
- Controllers\Master\ProductControllerss.cs:3843
- Controllers\Master\ProductControllerss.cs:4102
- Controllers\Master\ProductControllerss.cs:4288
- Controllers\Master\ProductControllerss.cs:4455
- Controllers\Master\ProductControllerss.cs:4642
- Controllers\Master\ProductControllerss.cs:4868
- Controllers\Master\ProductControllerss.cs:5246
- Controllers\Master\ProductControllerss.cs:5465
- Controllers\Master\ProductControllerss.cs:5633
- Controllers\Master\ProductControllerss.cs:5807
- Controllers\Master\ProductControllerss.cs:5969
- Controllers\Master\ProductControllerss.cs:6177
- Controllers\Master\ProductControllerss.cs:6276
- Controllers\Master\ProductControllerss.cs:6367
- Controllers\Master\ProductControllerss.cs:6461
- Controllers\Master\ProductControllerss.cs:6555
- Controllers\Master\ProductControllerss.cs:6650
- Controllers\Master\ProductControllerss.cs:6755
- Controllers\Master\ProductControllerss.cs:6935
- Controllers\Master\ProductControllerss.cs:7073
- Controllers\Master\ProductControllerss.cs:7392
- Controllers\Master\ProductControllerss.cs:7670
- Controllers\Master\ProductControllerss.cs:7801
- Controllers\Master\ProductControllerss.cs:7901
- Controllers\Master\ProductControllerss.cs:7955
- Controllers\Master\ProductControllerss.cs:8043
- Controllers\Master\ProductControllerss.cs:8117
- Controllers\Master\ProductControllerss.cs:8278
- Controllers\Reports\Accounts\CashAndBankBookController.cs:53
- Controllers\Reports\Accounts\CashBookController.cs:61
- Controllers\Reports\Accounts\CashFlowStatementController.cs:58
- Controllers\Reports\Accounts\DailyBalanceController.cs:54
- Controllers\Reports\Accounts\DailyClosingController.cs:59
- Controllers\Reports\Accounts\DayBookController.cs:59
- Controllers\Reports\Accounts\LedgerController.cs:56
- Controllers\Reports\Accounts\LedgerOpeningBalanceController.cs:51
- Controllers\Reports\Accounts\LedgerOpeningController.cs:52
- Controllers\Reports\Accounts\MonthWiseProfitAndLossController.cs:57
- Controllers\Reports\Accounts\ProfitAndLossController.cs:56
- Controllers\Reports\Accounts\ReceiptAndPaymentController.cs:57
- Controllers\Reports\Accounts\TrialBalanceController.cs:56
- Controllers\Reports\Accounts\VoucherQueryController.cs:56
- Controllers\Transactions\Sales\SalesInvoiceController.cs:63
- Controllers\Transactions\Sales\SalesInvoiceController.cs:151
- Controllers\Transactions\Sales\SalesInvoiceController.cs:263
- Controllers\Transactions\Sales\SalesInvoiceController.cs:362
- Controllers\Transactions\Sales\SalesInvoiceController.cs:527
- Controllers\Transactions\Sales\SalesInvoiceController.cs:625
- Controllers\Transactions\Sales\SalesInvoiceController.cs:685
- Controllers\Transactions\Sales\SalesReturnController.cs:55
- Controllers\Transactions\Sales\SalesReturnController.cs:131
- Controllers\Transactions\Sales\SalesReturnController.cs:216
- Controllers\Transactions\Sales\SalesReturnController.cs:326
- DB\CommonFunction.cs:43
- Repository\Master\PrUnitMasDB.cs:45
- Repository\Transactions\Sales\SalesOrderRepository.cs:143
