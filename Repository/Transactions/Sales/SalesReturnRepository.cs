using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using QuickBalance.API.Configuration;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs.Sales.SalesReturn;

namespace QuickBalance.API.Repository.Transactions.Sales
{
    /// <summary>
    /// Database access for the NEW Sales Return API.
    /// Only this class knows about SQL, stored procedures and table types.
    /// (The old DB/SalesReturnDB.cs belongs to the old controller and is not used here.)
    /// </summary>
    public class SalesReturnRepository
    {
        private const string SaveProcedure = "ASK_SP_ManageSalesReturnDetails";

        // Same constants as Sales Billing: alias is not used by the API.
        private const string AliasCode = "0";
        private const string AliasName = "";

        private readonly IDbConnectionFactory _connectionFactory;

        public SalesReturnRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        // ------------------------------------------------------------------
        // SAVE (create when ReturnId = 0, update when ReturnId > 0)
        // ------------------------------------------------------------------
        public async Task<SalesReturnSaveResult> SaveAsync(
            SalesReturnSaveRequest request,
            int userId,
            string? idempotencyKey,
            string? requestHash)
        {
            DateTime returnDate = request.ReturnDate!.Value;

            List<SqlParameter> parameters = new()
            {
                new SqlParameter("@SRETNO", SqlDbType.Int) { Value = request.ReturnId },
                new SqlParameter("@SINVNO", SqlDbType.Int) { Value = request.OriginalInvoiceId },
                new SqlParameter("@DATE", SqlDbType.DateTime) { Value = returnDate },
                new SqlParameter("@CUSCODE", SqlDbType.Int) { Value = request.CustomerCode },
                new SqlParameter("@PAYMODE", SqlDbType.NVarChar, 100) { Value = request.PayMode ?? "Cash" },
                new SqlParameter("@CLCODE", SqlDbType.Int) { Value = request.CashLedgerCode },
                new SqlParameter("@BLCODE", SqlDbType.Int) { Value = request.BankLedgerCode },
                new SqlParameter("@DUEDATE", SqlDbType.DateTime) { Value = request.DueDate ?? returnDate },
                new SqlParameter("@SMCODE", SqlDbType.Int) { Value = request.SalesmanCode },
                DecimalParameter("@DISCPERC", request.BillDiscountPercent),
                DecimalParameter("@DISCAMT", request.BillDiscountAmount),
                DecimalParameter("@ROUNDOFF", request.RoundOff),
                new SqlParameter("@USERID", SqlDbType.Int) { Value = userId },
                // The key and hash are only required for a NEW return; on update they may be empty.
                new SqlParameter("@IDEMPOTENCYKEY", SqlDbType.NVarChar, 100) { Value = (object?)idempotencyKey ?? DBNull.Value },
                new SqlParameter("@REQUESTHASH", SqlDbType.Char, 64) { Value = (object?)requestHash ?? DBNull.Value },
                TableParameter("@ITEMDTLS", "dbo.ASK_UDT_SalesReturnItem", BuildItemTable(request)),
                TableParameter("@SLNODTLS", "dbo.ASK_UDT_SalesReturnSerialNumber", BuildSerialTable(request)),
                TableParameter("@ADDIDTLS", "dbo.ASK_UDT_SalesReturnAdditionalInfo", BuildChargeTable(request))
            };

            DataTable result = await ExecuteProcedureAsync(SaveProcedure, parameters);
            return ReadSaveResult(result);
        }

        // ------------------------------------------------------------------
        // GET ONE RETURN: header, items, charges, serial numbers.
        // One database call, four result sets (in this exact order).
        // Column names copied from the working Sales Billing query; STYPE 'R' instead of 'O'.
        // ------------------------------------------------------------------
        private const string GetByIdSql = @"
            DECLARE @OrderNo INT = (SELECT ORDNO FROM SALM WHERE INVNO = @ReturnId AND STYPE = 'R');

            -- 1. Header (+ the original invoice, linked by its number: SALM.RTNINVNO = invoice ORDNO)
            SELECT
                M.INVNO AS ReturnId,
                M.ORDNO AS ReturnNo,
                M.INVNOTEXT AS ReturnNoText,
                M.INVDATE AS ReturnDate,
                ISNULL(I.INVNO, 0) AS OriginalInvoiceId,
                ISNULL(M.RTNINVNO, 0) AS OriginalInvoiceNo,
                I.INVNOTEXT AS OriginalInvoiceNoText,
                M.CUSTCODE AS CustomerCode,
                A.ACCNAME AS CustomerName,
                M.SMCODE AS SalesmanCode,
                S.SMNAME AS SalesmanName,
                CASE M.BTYPE WHEN 'C' THEN 'Cash' WHEN 'R' THEN 'Credit' ELSE 'Bank' END AS PayMode,
                CASE WHEN M.BTYPE = 'C' THEN ISNULL(M.ADJCD, 0) ELSE 0 END AS CashLedgerCode,
                ISNULL(M.BLCODE, 0) AS BankLedgerCode,
                M.DUEDATE AS DueDate,
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
            LEFT JOIN SALM I ON I.STYPE = 'O' AND ISNULL(M.RTNINVNO, 0) <> 0 AND I.ORDNO = M.RTNINVNO
            WHERE M.INVNO = @ReturnId AND M.STYPE = 'R';

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
                ISNULL(T.CONVTYPE, 0) AS ConversionType,
                ISNULL(T.CONVFACT, 1) AS ConversionFactor,
                ISNULL(T.CONVQTY, 0) AS ConversionQuantity
            FROM SALT T
            LEFT JOIN PRMAST P ON P.PRCODE = T.PRCODE
            LEFT JOIN PRUNITMAS UN ON UN.PRUNITID = T.UNITCODE
            WHERE T.INVNO = @ReturnId AND T.STYPE = 'R'
            ORDER BY T.TRNROWID;

            -- 3. Extra charges (user-added rows only)
            SELECT
                CAST(D.CODE AS INT) AS AccountCode,
                A.ACCNAME AS AccountName,
                ISNULL(D.TAXAMT, 0) AS Amount
            FROM SALD D
            LEFT JOIN ACCMAS A ON A.ID = D.CODE
            WHERE D.VRNO = @ReturnId AND D.VRTYPE = 'R' AND D.ETYPE = 'A' AND D.DFLAG = 0;

            -- 4. Serial numbers that came back IN on this return (INSLNO = line number)
            SELECT
                INSLNO AS [LineNo],
                ITEMSLNO AS SerialNo
            FROM SNTSTK
            WHERE INVRTYPE = 'SR' AND INVRNO = @ReturnId AND INORDNO = @OrderNo;";

        public async Task<SalesReturnDetailResponse?> GetByIdAsync(int returnId)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(new Variable());
            await connection.OpenAsync();

            using SqlMapper.GridReader results = await connection.QueryMultipleAsync(
                GetByIdSql,
                new { ReturnId = returnId });

            // Result sets must be read in the same order as the SELECTs above.
            SalesReturnHeaderResponse? header = await results.ReadFirstOrDefaultAsync<SalesReturnHeaderResponse>();
            if (header == null)
                return null;

            List<SalesReturnItemResponse> items = (await results.ReadAsync<SalesReturnItemResponse>()).ToList();
            var charges = (await results.ReadAsync<QuickBalance.API.DTOs.Sales.SalesBilling.SalesBillingChargeResponse>()).ToList();
            List<SalesReturnSerialRow> serials = (await results.ReadAsync<SalesReturnSerialRow>()).ToList();

            // Attach each serial number to its line (INSLNO = LineNo).
            foreach (SalesReturnItemResponse item in items)
            {
                item.SerialNumbers = serials
                    .Where(s => s.LineNo == item.LineNo)
                    .Select(s => new SalesReturnSerialResponse { SerialNo = s.SerialNo })
                    .ToList();
            }

            return new SalesReturnDetailResponse
            {
                Header = header,
                Items = items,
                Charges = charges
            };
        }

        // ------------------------------------------------------------------
        // LIST RETURNS (paged).
        // Sorting uses a WHITE LIST: ORDER BY cannot use parameters, so only
        // these fixed column texts can ever reach the SQL.
        // ------------------------------------------------------------------
        private static readonly Dictionary<string, string> ListSortColumns =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["returnNo"] = "M.ORDNO",
                ["date"] = "M.INVDATE",
                ["customer"] = "A.ACCNAME",
                ["amount"] = "M.AMOUNT"
            };

        private const string ListFrom = @"
            FROM SALM M
            LEFT JOIN ACCMAS A ON A.ID = M.CUSTCODE
            LEFT JOIN SALM I ON I.STYPE = 'O' AND ISNULL(M.RTNINVNO, 0) <> 0 AND I.ORDNO = M.RTNINVNO";

        // All filters are PARAMETERS (@...), never text glued into the SQL.
        private const string ListWhere = @"
            WHERE M.STYPE = 'R'
              AND (@CustomerCode = 0 OR M.CUSTCODE = @CustomerCode)
              AND (@FromDate IS NULL OR M.INVDATE >= @FromDate)
              AND (@ToDate IS NULL OR M.INVDATE < DATEADD(DAY, 1, @ToDate))
              AND (@Search IS NULL
                   OR M.INVNOTEXT LIKE @Search
                   OR A.ACCNAME LIKE @Search
                   OR CAST(M.ORDNO AS NVARCHAR(20)) LIKE @Search
                   OR I.INVNOTEXT LIKE @Search)";

        public async Task<(List<SalesReturnListItemResponse> Items, int TotalRecords)> GetListAsync(
            SalesReturnListRequest request)
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
            -- 1. Total number of matching returns (for the pagination block)
            SELECT COUNT(*)
            {ListFrom}
            {ListWhere};

            -- 2. One page of returns
            SELECT
                M.INVNO AS ReturnId,
                M.ORDNO AS ReturnNo,
                M.INVNOTEXT AS ReturnNoText,
                M.INVDATE AS ReturnDate,
                ISNULL(M.RTNINVNO, 0) AS OriginalInvoiceNo,
                I.INVNOTEXT AS OriginalInvoiceNoText,
                M.CUSTCODE AS CustomerCode,
                A.ACCNAME AS CustomerName,
                CASE M.BTYPE WHEN 'C' THEN 'Cash' WHEN 'R' THEN 'Credit' ELSE 'Bank' END AS PayMode,
                ISNULL(M.TVALUE, 0) AS TaxableAmount,
                ISNULL(M.TAX, 0) AS TaxAmount,
                ISNULL(M.AMOUNT, 0) AS Amount,
                CAST(ISNULL(M.CANCELFLG, 0) AS BIT) AS IsCancelled
            {ListFrom}
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

            await using SqlConnection connection = _connectionFactory.CreateConnection(new Variable());
            await connection.OpenAsync();

            using SqlMapper.GridReader results = await connection.QueryMultipleAsync(sql, parameters);

            int totalRecords = await results.ReadFirstAsync<int>();
            List<SalesReturnListItemResponse> items = (await results.ReadAsync<SalesReturnListItemResponse>()).ToList();

            return (items, totalRecords);
        }

        // ------------------------------------------------------------------
        // CANCEL (soft cancel through the existing, reviewed procedure).
        // The procedure returns MESSAGE + STATUS; ReadSaveResult reads that
        // (INVNO / ORDNO / ISREPLAY are not returned, so they stay 0 / false).
        // ------------------------------------------------------------------
        private const string CancelProcedure = "ASK_SP_CancelSalesReturnDetails";

        public async Task<SalesReturnSaveResult> CancelAsync(int returnId, int userId)
        {
            List<SqlParameter> parameters = new()
            {
                new SqlParameter("@SRETNO", SqlDbType.Int) { Value = returnId },
                new SqlParameter("@USERID", SqlDbType.Int) { Value = userId }
            };

            DataTable result = await ExecuteProcedureAsync(CancelProcedure, parameters);
            return ReadSaveResult(result);
        }

        // ------------------------------------------------------------------
        // Table-valued parameters
        // IMPORTANT: column ORDER must match the table type exactly,
        // because SQL Server matches table-type columns by POSITION, not by name.
        // ------------------------------------------------------------------

        // dbo.ASK_UDT_SalesReturnItem (24 columns)
        private static DataTable BuildItemTable(SalesReturnSaveRequest request)
        {
            DataTable table = new();
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
            table.Columns.Add("ALCODE", typeof(string));
            table.Columns.Add("ALNAME", typeof(string));
            table.Columns.Add("CONVTYPE", typeof(int));
            table.Columns.Add("CONVFACT", typeof(decimal));
            table.Columns.Add("TAXABLE", typeof(decimal));
            table.Columns.Add("TAXCODE", typeof(int));
            table.Columns.Add("TAXPERC", typeof(decimal));
            table.Columns.Add("TAXAMOUNT", typeof(decimal));
            table.Columns.Add("NETAMOUNT", typeof(decimal));

            foreach (SalesReturnSaveItemRequest item in request.Items)
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
                    (object?)item.MfgDate ?? DBNull.Value,
                    (object?)item.ExpiryDate ?? DBNull.Value,
                    item.UnitCode,
                    item.GodownCode,
                    AliasCode,
                    AliasName,
                    item.ConversionType,
                    item.ConversionFactor,
                    item.TaxableAmount,
                    item.TaxCode,
                    item.TaxPercent,
                    item.TaxAmount,
                    item.TaxableAmount + item.TaxAmount);   // NETAMOUNT, same rule as Sales Billing
            }

            return table;
        }

        // dbo.ASK_UDT_SalesReturnSerialNumber (3 columns)
        // INSLNO = the line number, so the procedure knows which line each serial belongs to.
        private static DataTable BuildSerialTable(SalesReturnSaveRequest request)
        {
            DataTable table = new();
            table.Columns.Add("PRCODE", typeof(int));
            table.Columns.Add("INSLNO", typeof(int));
            table.Columns.Add("ITEMSLNO", typeof(string));

            foreach (SalesReturnSaveItemRequest item in request.Items)
            {
                foreach (SalesReturnSaveSerialRequest serial in item.SerialNumbers)
                {
                    table.Rows.Add(item.ProductCode, item.LineNo, serial.SerialNo);
                }
            }

            return table;
        }

        // dbo.ASK_UDT_SalesReturnAdditionalInfo (2 columns)
        private static DataTable BuildChargeTable(SalesReturnSaveRequest request)
        {
            DataTable table = new();
            table.Columns.Add("ACCID", typeof(int));
            table.Columns.Add("AMOUNT", typeof(decimal));

            foreach (var charge in request.Charges)
            {
                table.Rows.Add(charge.AccountCode, charge.Amount);
            }

            return table;
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------
        private static SqlParameter DecimalParameter(string name, decimal value) =>
            new(name, SqlDbType.Decimal) { Precision = 18, Scale = 6, Value = value };

        private static SqlParameter TableParameter(string name, string typeName, DataTable value) =>
            new(name, SqlDbType.Structured) { TypeName = typeName, Value = value };

        // Opens our OWN connection (the reason: DBConn.ExecuteStoredProcedure expects an already
        // opened connection - that caused the "closed connection" error in Sales Billing).
        private async Task<DataTable> ExecuteProcedureAsync(string procedureName, List<SqlParameter> parameters)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(new Variable());
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

        // Every path of the procedure (saved, business error, replay, SQL error) returns
        // the same 5 columns: MESSAGE, STATUS, INVNO, ORDNO, ISREPLAY.
        private static SalesReturnSaveResult ReadSaveResult(DataTable table)
        {
            if (table.Rows.Count == 0)
            {
                return new SalesReturnSaveResult
                {
                    Success = false,
                    Message = "No response received from the sales return procedure."
                };
            }

            DataRow row = table.Rows[0];

            return new SalesReturnSaveResult
            {
                Success = ReadInt(row, "STATUS") == 1,
                Message = table.Columns.Contains("MESSAGE") ? Convert.ToString(row["MESSAGE"]) ?? string.Empty : string.Empty,
                ReturnId = ReadInt(row, "INVNO"),
                ReturnNo = ReadInt(row, "ORDNO"),
                IsReplay = table.Columns.Contains("ISREPLAY")
                           && row["ISREPLAY"] != DBNull.Value
                           && Convert.ToBoolean(row["ISREPLAY"])
            };
        }

        private static int ReadInt(DataRow row, string column) =>
            row.Table.Columns.Contains(column) && row[column] != DBNull.Value
                ? Convert.ToInt32(row[column])
                : 0;
    }

    /// <summary>What the save procedure answered, in C# form. Used only by the service.</summary>
    public class SalesReturnSaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsReplay { get; set; }       // true = same Idempotency-Key sent again; nothing new was saved
        public int ReturnId { get; set; }        // SALM.INVNO of the return (0 when not saved)
        public int ReturnNo { get; set; }        // SALM.ORDNO of the return (0 when not saved)
    }

    /// <summary>
    /// One serial-number row as read from SNTSTK (internal: used only to attach
    /// serial numbers to their return line).
    /// </summary>
    internal sealed class SalesReturnSerialRow
    {
        public int LineNo { get; set; }
        public string? SerialNo { get; set; }
    }
}
