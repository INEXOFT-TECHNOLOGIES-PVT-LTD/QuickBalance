using Dapper;
using System.Data;
using Microsoft.Data.SqlClient;
using QuickBalance.API.Configuration;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs.Sales.SalesBilling;

namespace QuickBalance.API.Repository.Transactions.Sales
{
    /// <summary>
    /// The only Sales Billing class that talks to the database.
    /// It maps an already validated and prepared SalesBillingRequest to the
    /// parameters of ASK_SP_ManageSalesInvoiceDetails and reads the result.
    /// No validation and no business defaults here - those live in the service.
    /// </summary>
    public sealed class SalesBillingRepository
    {
        private const string SaveProcedureName = "ASK_SP_ManageSalesInvoiceDetails";

        // Fixed values, same as the existing API.
        private const string PcName = "API";
        private const string NoAliasCode = "0";
        private const string NoAliasName = "";

        private readonly IDbConnectionFactory _connectionFactory;

        public SalesBillingRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<SalesBillingSaveResult> SaveAsync(
            SalesBillingRequest request,
            int userId,
            string idempotencyKey,
            string requestHash)
        {
            List<SqlParameter> parameters = new List<SqlParameter>
            {
                new SqlParameter("@SINVNO", SqlDbType.Int) { Value = request.InvoiceId },
                new SqlParameter("@DATE", SqlDbType.DateTime) { Value = ToDbValue(request.InvoiceDate) },
                new SqlParameter("@CUSCODE", SqlDbType.Int) { Value = request.CustomerCode },
                new SqlParameter("@PAYMODE", SqlDbType.NVarChar, 100) { Value = ToDbValue(request.PayMode) },
                new SqlParameter("@CLCODE", SqlDbType.Int) { Value = request.CashLedgerCode },
                new SqlParameter("@BLCODE", SqlDbType.Int) { Value = request.BankLedgerCode },
                new SqlParameter("@DUEDATE", SqlDbType.DateTime) { Value = ToDbValue(request.DueDate) },
                new SqlParameter("@SMCODE", SqlDbType.Int) { Value = request.SalesmanCode },
                new SqlParameter("@POS", SqlDbType.NVarChar, 10) { Value = request.IsPos ? "Y" : "N" },
                new SqlParameter("@PCNAME", SqlDbType.NVarChar, -1) { Value = PcName },
                new SqlParameter("@DISCPERC", SqlDbType.Decimal) { Precision = 18, Scale = 6, Value = request.BillDiscountPercent },
                new SqlParameter("@DISCAMT", SqlDbType.Decimal) { Precision = 18, Scale = 6, Value = request.BillDiscountAmount },
                new SqlParameter("@ROUNDOFF", SqlDbType.Decimal) { Precision = 18, Scale = 6, Value = request.RoundOff },
                new SqlParameter("@USERID", SqlDbType.Int) { Value = userId },
                new SqlParameter("@IDEMPOTENCYKEY", SqlDbType.NVarChar, 100) { Value = ToDbValue(idempotencyKey) },
                new SqlParameter("@REQUESTHASH", SqlDbType.Char, 64) { Value = ToDbValue(requestHash) },

                new SqlParameter("@ITEMDTLS", SqlDbType.Structured)
                {
                    TypeName = "dbo.ASK_UDT_SalesInvoiceItem",
                    Value = BuildItemTable(request)
                },
                new SqlParameter("@SLNODTLS", SqlDbType.Structured)
                {
                    TypeName = "dbo.ASK_UDT_SalesInvoiceSerialNumber",
                    Value = BuildSerialTable(request)
                },
                new SqlParameter("@ADDIDTLS", SqlDbType.Structured)
                {
                    TypeName = "dbo.ASK_UDT_SalesInvoiceAdditionalInfo",
                    Value = BuildChargeTable(request)
                }
            };

            DataTable result = await ExecuteProcedureAsync(SaveProcedureName, parameters);

            return ReadResult(result);
        }

        // ------------------------------------------------------------------
        // Read one invoice: header, items, charges, serial numbers.
        // One database call, four result sets (in this exact order).
        // ------------------------------------------------------------------

        private const string GetByIdSql = @"
            DECLARE @OrderNo INT = (SELECT ORDNO FROM SALM WHERE INVNO = @InvoiceId AND STYPE = 'O');

            -- 1. Header
            SELECT
                M.INVNO AS InvoiceId,
                M.ORDNO AS InvoiceNo,
                M.INVNOTEXT AS InvoiceNoText,
                M.INVDATE AS InvoiceDate,
                M.CUSTCODE AS CustomerCode,
                A.ACCNAME AS CustomerName,
                M.SMCODE AS SalesmanCode,
                S.SMNAME AS SalesmanName,
                CASE M.BTYPE WHEN 'C' THEN 'Cash' WHEN 'R' THEN 'Credit' ELSE 'Bank' END AS PayMode,
                CASE WHEN M.BTYPE = 'C' THEN ISNULL(M.ADJCD, 0) ELSE 0 END AS CashLedgerCode,
                ISNULL(M.BLCODE, 0) AS BankLedgerCode,
                M.DUEDATE AS DueDate,
                CAST(CASE WHEN M.POS = 'Y' THEN 1 ELSE 0 END AS BIT) AS IsPos,
                ISNULL(M.DISCP, 0) AS BillDiscountPercent,
                ISNULL(M.DISCVAL, 0) AS BillDiscountAmount,
                ISNULL(M.COINADJ, 0) AS RoundOff,
                ISNULL(M.TVALUE, 0) AS TaxableAmount,
                ISNULL(M.TAX, 0) AS TaxAmount,
                ISNULL(M.NTVALUE, 0) AS NetAmount,
                ISNULL(M.AMOUNT, 0) AS Amount,
                ISNULL(M.CASHPAID, 0) AS CashPaid,
                CAST(ISNULL(M.CANCELFLG, 0) AS BIT) AS IsCancelled,
                U.FULLNAME AS CreatedBy
            FROM SALM M
            LEFT JOIN ACCMAS A ON A.ID = M.CUSTCODE
            LEFT JOIN SMMAST S ON S.SMCODE = M.SMCODE
            LEFT JOIN TBUSER U ON U.USERCODE = M.LOGCD
            WHERE M.INVNO = @InvoiceId AND M.STYPE = 'O';

            -- 2. Items
            SELECT
                T.TRNROWID AS [LineNo],
                T.PRCODE AS ProductCode,
                P.PRNAME AS ProductName,
                T.UNITCODE AS UnitCode,
                UN.PRUNITNAME AS UnitName,
                T.GDWCODE AS GodownCode,
                ISNULL(T.QTY, 0) AS Quantity,
                ISNULL(T.RATE, 0) AS Rate,
                ISNULL(T.PRRATE, 0) AS PurchaseRate,
                ISNULL(T.MRP, 0) AS Mrp,
                ISNULL(T.LDISCPERC, 0) AS ItemDiscountPercent,
                ISNULL(T.LDISCAMT, 0) AS ItemDiscountAmount,
                ISNULL(T.DISCP, 0) AS DiscountPercent,
                ISNULL(T.DISC, 0) AS DiscountAmount,
                T.TAXCODE AS TaxCode,
                ISNULL(T.TAX, 0) AS TaxPercent,
                T.BATCH AS Batch,
                T.MFGDATE AS MfgDate,
                T.EXPIRY AS ExpiryDate,
                ISNULL(T.SORDNO, 0) AS SalesOrderNo,
                ISNULL(T.ORDROWID, 0) AS SalesOrderLineId,
                ISNULL(T.CONVTYPE, 0) AS ConversionType,
                ISNULL(T.CONVFACT, 1) AS ConversionFactor,
                ISNULL(T.CONVQTY, 0) AS ConversionQuantity
            FROM SALT T
            LEFT JOIN PRMAST P ON P.PRCODE = T.PRCODE
            LEFT JOIN PRUNITMAS UN ON UN.PRUNITID = T.UNITCODE
            WHERE T.INVNO = @InvoiceId
            ORDER BY T.TRNROWID;

            -- 3. Extra charges (user-added rows only)
            SELECT
                CAST(D.CODE AS INT) AS AccountCode,
                A.ACCNAME AS AccountName,
                ISNULL(D.TAXAMT, 0) AS Amount
            FROM SALD D
            LEFT JOIN ACCMAS A ON A.ID = D.CODE
            WHERE D.VRNO = @InvoiceId AND D.VRTYPE = 'O' AND D.ETYPE = 'A' AND D.DFLAG = 0;

            -- 4. Serial numbers sold on this invoice
            SELECT
                OUTSLNO AS [LineNo],
                INVRTYPE AS SourceVoucherType,
                INVRNO AS SourceVoucherId,
                INORDNO AS SourceVoucherNo,
                INSLNO AS SourceLineNo,
                ITEMSLNO AS SerialNo
            FROM SNTSTK
            WHERE OUTVRTYPE = 'SI' AND OUTVRNO = @InvoiceId AND OUTORDNO = @OrderNo;";

        public async Task<SalesBillingDetailResponse?> GetByIdAsync(int invoiceId)
        {
            await using SqlConnection connection =
                _connectionFactory.CreateConnection(new Variable());

            await connection.OpenAsync();

            using SqlMapper.GridReader results = await connection.QueryMultipleAsync(
                GetByIdSql,
                new { InvoiceId = invoiceId });

            // Result sets must be read in the same order as the SELECTs above.
            SalesBillingHeaderResponse? header =
                await results.ReadFirstOrDefaultAsync<SalesBillingHeaderResponse>();

            if (header == null)
                return null;

            List<SalesBillingItemResponse> items =
                (await results.ReadAsync<SalesBillingItemResponse>()).ToList();

            List<SalesBillingChargeResponse> charges =
                (await results.ReadAsync<SalesBillingChargeResponse>()).ToList();

            List<SalesBillingSerialRow> serials =
                (await results.ReadAsync<SalesBillingSerialRow>()).ToList();

            // Attach each serial number to its line (OUTSLNO = LineNo).
            foreach (SalesBillingItemResponse item in items)
            {
                item.SerialNumbers = serials
                    .Where(s => s.LineNo == item.LineNo)
                    .Select(s => new SalesBillingSerialResponse
                    {
                        SourceVoucherType = s.SourceVoucherType,
                        SourceVoucherId = s.SourceVoucherId,
                        SourceVoucherNo = s.SourceVoucherNo,
                        SourceLineNo = s.SourceLineNo,
                        SerialNo = s.SerialNo
                    })
                    .ToList();
            }

            return new SalesBillingDetailResponse
            {
                Header = header,
                Items = items,
                Charges = charges
            };
        }

        // ------------------------------------------------------------------
        // List invoices (paged).
        // Sorting uses a WHITE LIST: ORDER BY cannot use parameters, so only
        // these fixed column texts can ever reach the SQL.
        // ------------------------------------------------------------------

        private static readonly Dictionary<string, string> ListSortColumns =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["invoiceNo"] = "M.ORDNO",
                ["date"] = "M.INVDATE",
                ["customer"] = "A.ACCNAME",
                ["amount"] = "M.AMOUNT"
            };

        // All filters are PARAMETERS (@...), never text glued into the SQL.
        private const string ListWhere = @"
            WHERE M.STYPE = 'O'
              AND (@CustomerCode = 0 OR M.CUSTCODE = @CustomerCode)
              AND (@FromDate IS NULL OR M.INVDATE >= @FromDate)
              AND (@ToDate IS NULL OR M.INVDATE < DATEADD(DAY, 1, @ToDate))
              AND (@Search IS NULL
                   OR M.INVNOTEXT LIKE @Search
                   OR A.ACCNAME LIKE @Search
                   OR CAST(M.ORDNO AS NVARCHAR(20)) LIKE @Search)";

        public async Task<(List<SalesBillingListItemResponse> Items, int TotalRecords)> GetListAsync(
            SalesBillingListRequest request)
        {
            string sortColumn = ListSortColumns.TryGetValue(request.SortBy ?? string.Empty, out string? column)
                ? column
                : "M.INVNO";

            string sortDirection = string.Equals(request.SortDirection, "ASC", StringComparison.OrdinalIgnoreCase)
                ? "ASC"
                : "DESC";

            // SQL Server does not allow the same column twice in ORDER BY,
            // so the INVNO tie-breaker is only added when sorting by another column.
            string orderBy = sortColumn == "M.INVNO"
                ? $"M.INVNO {sortDirection}"
                : $"{sortColumn} {sortDirection}, M.INVNO DESC";

            string sql = $@"
            -- 1. Total number of matching invoices (for the pagination block)
            SELECT COUNT(*)
            FROM SALM M
            LEFT JOIN ACCMAS A ON A.ID = M.CUSTCODE
            {ListWhere};

            -- 2. One page of invoices
            SELECT
                M.INVNO AS InvoiceId,
                M.ORDNO AS InvoiceNo,
                M.INVNOTEXT AS InvoiceNoText,
                M.INVDATE AS InvoiceDate,
                M.CUSTCODE AS CustomerCode,
                A.ACCNAME AS CustomerName,
                CASE M.BTYPE WHEN 'C' THEN 'Cash' WHEN 'R' THEN 'Credit' ELSE 'Bank' END AS PayMode,
                ISNULL(M.TVALUE, 0) AS TaxableAmount,
                ISNULL(M.TAX, 0) AS TaxAmount,
                ISNULL(M.AMOUNT, 0) AS Amount,
                CAST(ISNULL(M.CANCELFLG, 0) AS BIT) AS IsCancelled
            FROM SALM M
            LEFT JOIN ACCMAS A ON A.ID = M.CUSTCODE
            {ListWhere}
            ORDER BY {orderBy}
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;";

            var parameters = new
            {
                request.CustomerCode,
                FromDate = request.FromDate?.Date,
                ToDate = request.ToDate?.Date,
                Search = string.IsNullOrWhiteSpace(request.Search) ? null : $"%{request.Search.Trim()}%",
                Offset = (request.Page - 1) * request.PageSize,
                request.PageSize
            };

            await using SqlConnection connection =
                _connectionFactory.CreateConnection(new Variable());

            await connection.OpenAsync();

            using SqlMapper.GridReader results = await connection.QueryMultipleAsync(sql, parameters);

            int totalRecords = await results.ReadFirstAsync<int>();

            List<SalesBillingListItemResponse> items =
                (await results.ReadAsync<SalesBillingListItemResponse>()).ToList();

            return (items, totalRecords);
        }

        // ------------------------------------------------------------------
        // Cancel an invoice (soft cancel through the existing procedure).
        // ------------------------------------------------------------------

        private const string CancelProcedureName = "ASK_SP_CancelSalesInvoiceDetails";

        public async Task<SalesBillingSaveResult> CancelAsync(int invoiceId, int userId)
        {
            List<SqlParameter> parameters = new List<SqlParameter>
            {
                new SqlParameter("@SINVNO", SqlDbType.Int) { Value = invoiceId },
                new SqlParameter("@USERID", SqlDbType.Int) { Value = userId }
            };

            DataTable result = await ExecuteProcedureAsync(CancelProcedureName, parameters);

            // Same MESSAGE / STATUS result shape as the save procedure.
            return ReadResult(result);
        }

        // ------------------------------------------------------------------
        // Invoice state before an update.
        // null = invoice not found, true = cancelled, false = normal.
        // ------------------------------------------------------------------

        public async Task<bool?> IsCancelledAsync(int invoiceId)
        {
            const string sql = @"
                SELECT CAST(ISNULL(CANCELFLG, 0) AS BIT)
                FROM SALM
                WHERE INVNO = @InvoiceId AND STYPE = 'O';";

            await using SqlConnection connection =
                _connectionFactory.CreateConnection(new Variable());

            await connection.OpenAsync();

            return await connection.QueryFirstOrDefaultAsync<bool?>(
                sql,
                new { InvoiceId = invoiceId });
        }

        // ------------------------------------------------------------------
        // Pending invoice for return: which lines can still be returned.
        // Same rule as ASK_SP_ManageSalesReturnDetails: per product, sold minus what OTHER,
        // NOT-cancelled returns of this invoice already took back. When a product is on several
        // lines, the returned quantity is taken from the first lines first (by line number).
        // Replaces the logic of the old ASK_SP_PendingSalesInvoicesInReturn (cursor, INT conversion factor).
        // ------------------------------------------------------------------
        private const string PendingForReturnSql = @"
            DECLARE @InvNo INT = NULL, @OrdNo INT = NULL;

            SELECT TOP 1 @InvNo = INVNO, @OrdNo = ORDNO
            FROM SALM
            WHERE STYPE = 'O'
              AND ((@InvoiceId > 0 AND INVNO = @InvoiceId)
                OR (@InvoiceId = 0 AND INVNOTEXT = @InvoiceNoText))
            ORDER BY INVNO DESC;

            DECLARE @MultiAlias BIT = (SELECT TOP 1 MULTIALIAS FROM CONFIGTB);

            -- 1. Header (no row = invoice not found)
            SELECT
                M.INVNO AS InvoiceId,
                M.ORDNO AS InvoiceNo,
                M.INVNOTEXT AS InvoiceNoText,
                M.INVDATE AS InvoiceDate,
                M.CUSTCODE AS CustomerCode,
                A.ACCNAME AS CustomerName,
                ISNULL(M.SMCODE, 0) AS SalesmanCode,
                CASE M.BTYPE WHEN 'C' THEN 'Cash' WHEN 'R' THEN 'Credit' ELSE 'Bank' END AS PayMode,
                ISNULL(M.DISCP, 0) AS BillDiscountPercent,
                CAST(ISNULL(M.CANCELFLG, 0) AS BIT) AS IsCancelled
            FROM SALM M
            LEFT JOIN ACCMAS A ON A.ID = M.CUSTCODE
            WHERE M.INVNO = @InvNo;

            -- 2. Lines that still have quantity to return
            WITH Returned AS (
                SELECT T.PRCODE, SUM(ISNULL(T.CONVQTY, 0)) AS RETQTY
                FROM SALT T
                WHERE T.STYPE = 'R' AND T.RTNINVNO = @OrdNo AND T.INVNO <> @ReturnId
                  AND NOT EXISTS (SELECT 1 FROM SALM R WHERE R.INVNO = T.INVNO AND ISNULL(R.CANCELFLG, 0) = 1)
                GROUP BY T.PRCODE
            ),
            Lines AS (
                SELECT
                    T.TRNROWID, T.PRCODE, P.PRNAME, T.UNITCODE, UN.PRUNITNAME, T.GDWCODE,
                    ISNULL(T.QTY, 0) AS QTY, ISNULL(T.CONVQTY, 0) AS CONVQTY,
                    T.RATE, T.PRRATE, T.MRP, T.LDISCPERC, T.LDISCAMT, T.DISCP, T.DISC, T.TAXCODE, T.TAX,
                    T.BATCH, T.MFGDATE, T.EXPIRY, T.CONVTYPE, T.CONVFACT, T.ALCODE,
                    P.UNIT AS MAINUNIT,
                    ISNULL(R.RETQTY, 0) AS RETQTY,
                    ISNULL(SUM(ISNULL(T.CONVQTY, 0)) OVER (PARTITION BY T.PRCODE ORDER BY T.TRNROWID, T.SNTSLNO
                        ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING), 0) AS SOLDBEFORE,
                    AL.ALQTY
                FROM SALT T
                LEFT JOIN PRMAST P ON P.PRCODE = T.PRCODE
                LEFT JOIN PRUNITMAS UN ON UN.PRUNITID = T.UNITCODE
                LEFT JOIN Returned R ON R.PRCODE = T.PRCODE
                OUTER APPLY (SELECT ISNULL(MAX(A.QTY), 0) AS ALQTY FROM PRALIAS A
                             WHERE A.PRCODE = T.PRCODE AND (CAST(A.CODE AS NVARCHAR(MAX)) = T.ALCODE OR A.ALNAME = T.ALNAME)) AL
                WHERE T.INVNO = @InvNo AND T.STYPE = 'O'
            ),
            Allocated AS (
                SELECT L.*,
                    CASE WHEN L.RETQTY - L.SOLDBEFORE <= 0 THEN 0
                         WHEN L.RETQTY - L.SOLDBEFORE >= L.CONVQTY THEN L.CONVQTY
                         ELSE L.RETQTY - L.SOLDBEFORE END AS ALLOCQTY
                FROM Lines L
            )
            SELECT
                TRNROWID AS [LineNo],
                PRCODE AS ProductCode,
                PRNAME AS ProductName,
                UNITCODE AS UnitCode,
                PRUNITNAME AS UnitName,
                GDWCODE AS GodownCode,
                QTY AS SoldQuantity,
                -- main-unit remainder back to the line's unit (same conversion as the old procedure)
                CASE WHEN ALLOCQTY = 0 THEN QTY
                     ELSE ((CONVQTY - ALLOCQTY)
                           / CASE WHEN @MultiAlias = 1 AND ISNULL(ALCODE, '') <> '' AND ALQTY <> 0 THEN ALQTY ELSE CAST(1 AS DECIMAL(18, 6)) END)
                           * CASE WHEN UNITCODE = MAINUNIT THEN 1 WHEN CONVTYPE = 1 AND ISNULL(CONVFACT, 0) <> 0 THEN CONVFACT ELSE 1 END
                           / CASE WHEN UNITCODE <> MAINUNIT AND CONVTYPE = 0 AND ISNULL(CONVFACT, 0) <> 0 THEN CONVFACT ELSE 1 END
                END AS ReturnableQuantity,
                CONVQTY - ALLOCQTY AS ReturnableConversionQuantity,
                ISNULL(RATE, 0) AS Rate,
                ISNULL(PRRATE, 0) AS PurchaseRate,
                ISNULL(MRP, 0) AS Mrp,
                ISNULL(LDISCPERC, 0) AS ItemDiscountPercent,
                ISNULL(LDISCAMT, 0) AS ItemDiscountAmount,
                ISNULL(DISCP, 0) AS DiscountPercent,
                ISNULL(DISC, 0) AS DiscountAmount,
                TAXCODE AS TaxCode,
                ISNULL(TAX, 0) AS TaxPercent,
                BATCH AS Batch,
                MFGDATE AS MfgDate,
                EXPIRY AS ExpiryDate,
                ISNULL(CONVTYPE, 0) AS ConversionType,
                ISNULL(CONVFACT, 1) AS ConversionFactor
            FROM Allocated
            WHERE CONVQTY - ALLOCQTY > 0
            ORDER BY TRNROWID;";

        public async Task<SalesBillingPendingResponse?> GetPendingForReturnAsync(SalesBillingPendingRequest request)
        {
            await using SqlConnection connection =
                _connectionFactory.CreateConnection(new Variable());

            await connection.OpenAsync();

            using SqlMapper.GridReader results = await connection.QueryMultipleAsync(
                PendingForReturnSql,
                new
                {
                    request.InvoiceId,
                    InvoiceNoText = request.InvoiceNoText?.Trim() ?? string.Empty,
                    request.ReturnId
                });

            SalesBillingPendingHeaderResponse? header =
                await results.ReadFirstOrDefaultAsync<SalesBillingPendingHeaderResponse>();

            if (header == null)
                return null;

            List<SalesBillingPendingItemResponse> items =
                (await results.ReadAsync<SalesBillingPendingItemResponse>()).ToList();

            return new SalesBillingPendingResponse { Header = header, Items = items };
        }

        // ------------------------------------------------------------------
        // Runs a stored procedure on its OWN connection: opened here and
        // closed automatically by "await using", even if an error happens.
        // ------------------------------------------------------------------

        private async Task<DataTable> ExecuteProcedureAsync(
            string procedureName,
            List<SqlParameter> parameters)
        {
            await using SqlConnection connection =
                _connectionFactory.CreateConnection(new Variable());

            await connection.OpenAsync();

            await using SqlCommand command = new SqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddRange(parameters.ToArray());

            await using SqlDataReader reader = await command.ExecuteReaderAsync();

            DataTable table = new DataTable();
            table.Load(reader);

            return table;
        }

        // ------------------------------------------------------------------
        // Table-valued parameters.
        // IMPORTANT: SQL Server matches these columns BY POSITION, not by name.
        // The column order must be exactly the order of the UDT definition.
        // ------------------------------------------------------------------

        private static DataTable BuildItemTable(SalesBillingRequest request)
        {
            // Order = dbo.ASK_UDT_SalesInvoiceItem
            DataTable table = new DataTable();
            table.Columns.Add("SLNO", typeof(int));
            table.Columns.Add("PRCODE", typeof(int));
            table.Columns.Add("QTY", typeof(decimal));
            table.Columns.Add("RATE", typeof(decimal));
            table.Columns.Add("PRRATE", typeof(decimal));
            table.Columns.Add("MRP", typeof(decimal));
            table.Columns.Add("LDISCPERC", typeof(decimal));
            table.Columns.Add("LDISCPRICE", typeof(decimal));
            table.Columns.Add("DISCPERC", typeof(decimal));
            table.Columns.Add("DISCPRICE", typeof(decimal));
            table.Columns.Add("BATCH", typeof(string));
            table.Columns.Add("MFGDATE", typeof(DateTime));
            table.Columns.Add("EXPDATE", typeof(DateTime));
            table.Columns.Add("UNITCODE", typeof(int));
            table.Columns.Add("GDWCODE", typeof(int));
            table.Columns.Add("SORDNO", typeof(int));
            table.Columns.Add("ORDROWID", typeof(int));
            table.Columns.Add("ALCODE", typeof(string));
            table.Columns.Add("ALNAME", typeof(string));
            table.Columns.Add("CONVTYPE", typeof(int));
            table.Columns.Add("CONVFACT", typeof(decimal));
            table.Columns.Add("TAXABLE", typeof(decimal));
            table.Columns.Add("TAXCODE", typeof(int));
            table.Columns.Add("TAXPERC", typeof(decimal));
            table.Columns.Add("TAXAMOUNT", typeof(decimal));
            table.Columns.Add("NETAMOUNT", typeof(decimal));

            foreach (SalesBillingItemRequest item in request.Items)
            {
                table.Rows.Add(
                    item.LineNo,
                    item.ProductCode,
                    item.Quantity,
                    item.Rate,
                    item.PurchaseRate,
                    item.Mrp,
                    item.ItemDiscountPercent,
                    item.ItemDiscountAmount,
                    item.DiscountPercent,
                    item.DiscountAmount,
                    item.Batch ?? string.Empty,
                    ToDbValue(item.MfgDate),
                    ToDbValue(item.ExpiryDate),
                    item.UnitCode,
                    item.GodownCode,
                    item.SalesOrderNo,
                    item.SalesOrderLineId,
                    NoAliasCode,
                    NoAliasName,
                    item.ConversionType,
                    item.ConversionFactor,
                    item.TaxableAmount,
                    item.TaxCode,
                    item.TaxPercent,
                    item.TaxAmount,
                    item.TaxableAmount + item.TaxAmount); // NETAMOUNT: always calculated, never from the client
            }

            return table;
        }

        private static DataTable BuildSerialTable(SalesBillingRequest request)
        {
            // Order = dbo.ASK_UDT_SalesInvoiceSerialNumber
            DataTable table = new DataTable();
            table.Columns.Add("INVRTYPE", typeof(string));
            table.Columns.Add("INVRNO", typeof(int));
            table.Columns.Add("INORDNO", typeof(int));
            table.Columns.Add("INSLNO", typeof(int));
            table.Columns.Add("PRCODE", typeof(int));
            table.Columns.Add("OUTSLNO", typeof(int));
            table.Columns.Add("UNITCODE", typeof(int));
            table.Columns.Add("GDWCODE", typeof(int));
            table.Columns.Add("ITEMSLNO", typeof(string));

            foreach (SalesBillingItemRequest item in request.Items)
            {
                foreach (SalesBillingSerialRequest serial in item.SerialNumbers)
                {
                    // Product, unit, godown and line are copied from the parent line,
                    // so a serial can never point at a different product.
                    table.Rows.Add(
                        serial.SourceVoucherType,
                        serial.SourceVoucherId,
                        serial.SourceVoucherNo,
                        serial.SourceLineNo,
                        item.ProductCode,
                        item.LineNo,
                        item.UnitCode,
                        item.GodownCode,
                        serial.SerialNo);
                }
            }

            return table;
        }

        private static DataTable BuildChargeTable(SalesBillingRequest request)
        {
            // Order = dbo.ASK_UDT_SalesInvoiceAdditionalInfo
            DataTable table = new DataTable();
            table.Columns.Add("ACCID", typeof(int));
            table.Columns.Add("AMOUNT", typeof(decimal));

            foreach (SalesBillingChargeRequest charge in request.Charges)
            {
                table.Rows.Add(charge.AccountCode, charge.Amount);
            }

            return table;
        }

        // ------------------------------------------------------------------
        // Result
        // ------------------------------------------------------------------

        private static SalesBillingSaveResult ReadResult(DataTable result)
        {
            if (result.Rows.Count == 0)
            {
                return new SalesBillingSaveResult
                {
                    Success = false,
                    Message = "No response received from the sales invoice procedure."
                };
            }

            DataRow row = result.Rows[0];

            // A duplicate (replayed) request returns extra columns, including INVNO and ORDNO.
            bool isReplay = result.Columns.Contains("INVNO");

            return new SalesBillingSaveResult
            {
                Success = Convert.ToInt32(row["STATUS"]) == 1,
                Message = Convert.ToString(row["MESSAGE"]) ?? string.Empty,
                IsReplay = isReplay,
                InvoiceId = isReplay ? ToNullableInt(row["INVNO"]) : null,
                InvoiceNo = isReplay ? ToNullableInt(row["ORDNO"]) : null
            };
        }

        // ------------------------------------------------------------------
        // Helpers: C# null -> SQL NULL (DBNull.Value)
        // ------------------------------------------------------------------

        private static object ToDbValue(DateTime? value)
        {
            return value.HasValue ? value.Value : DBNull.Value;
        }

        private static object ToDbValue(string? value)
        {
            return value is null ? DBNull.Value : value;
        }

        private static int? ToNullableInt(object value)
        {
            return value == DBNull.Value ? null : Convert.ToInt32(value);
        }
    }

    /// <summary>
    /// What the repository hands back to the service after calling the procedure.
    /// The service (Step 4) decides what this means for the API response.
    /// </summary>
    public sealed class SalesBillingSaveResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;

        // True when the procedure recognised a repeated Idempotency-Key
        // and returned the earlier result instead of saving again.
        public bool IsReplay { get; init; }

        // Only filled on a replay (the live procedure returns them only then).
        public int? InvoiceId { get; init; }
        public int? InvoiceNo { get; init; }
    }

    /// <summary>
    /// One serial-number row as read from SNTSTK (internal: used only to attach
    /// serial numbers to their invoice line).
    /// </summary>
    internal sealed class SalesBillingSerialRow
    {
        public int LineNo { get; set; }
        public string? SourceVoucherType { get; set; }
        public int SourceVoucherId { get; set; }
        public int SourceVoucherNo { get; set; }
        public int SourceLineNo { get; set; }
        public string? SerialNo { get; set; }
    }
}
