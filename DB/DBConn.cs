
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
            if (con == null || con.State != ConnectionState.Open)
            {
                throw new InvalidOperationException(
                    "Connection is not open. Call OpenConnection() first.");
            }

            var command = new SqlCommand(qry, con)
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