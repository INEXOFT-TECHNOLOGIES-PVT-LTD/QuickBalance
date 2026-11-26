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
