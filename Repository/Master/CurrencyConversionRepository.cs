
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;

namespace QuickBalance.API.Repository
{
    public class CurrencyConversionRepository
    {
        private readonly DBConn _db;

        public CurrencyConversionRepository(DBConn db)
        {
            _db = db;
        }

        // ---------------------------------------------------------
        // GET PAGINATED CONVERSIONS
        // ---------------------------------------------------------
        public async Task<(List<CurrencyConversionResponse> Data, int TotalRecords)>
            GetConversionsAsync(int page, int pageSize, string? search = null)
        {
            int offset = (page - 1) * pageSize;

            const string dataQuery = """
                SELECT
                    CC.CODE AS Id,
                    CC.BASECURR AS BaseCurrencyId,
                    CC.ALTCURR AS AlternateCurrencyId,
                    CM.SYMBOL AS Symbol,
                    CC.TDATE AS ConversionDate,
                    CC.STDRATE AS StandardRate,
                    CC.SELLRATE AS SellRate,
                    CC.BUYRATE AS BuyRate,
                    CC.LOGCD AS UserId,
                    CC.UTIME AS UpdatedAt
                FROM dbo.CURRCONV CC
                LEFT JOIN dbo.CURRENCYMAS CM
                    ON CM.CODE = CC.ALTCURR
                WHERE ISNULL(CC.DELFLG, 0) = 0
                  AND
                    (
                        @Search IS NULL
                        OR CM.SYMBOL LIKE '%' + @Search + '%'
                        OR CONVERT(VARCHAR(10), CC.TDATE, 23)
                            LIKE '%' + @Search + '%'
                    )
                ORDER BY CC.TDATE DESC, CC.CODE DESC
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY;
                """;

            const string countQuery = """
                SELECT COUNT(1)
                FROM dbo.CURRCONV CC
                LEFT JOIN dbo.CURRENCYMAS CM
                    ON CM.CODE = CC.ALTCURR
                WHERE ISNULL(CC.DELFLG, 0) = 0
                  AND
                    (
                        @Search IS NULL
                        OR CM.SYMBOL LIKE '%' + @Search + '%'
                        OR CONVERT(VARCHAR(10), CC.TDATE, 23)
                            LIKE '%' + @Search + '%'
                    );
                """;

            var parameters = new
            {
                Search = string.IsNullOrWhiteSpace(search)
                    ? null
                    : search.Trim(),
                Offset = offset,
                PageSize = pageSize
            };

            var data =
                await _db.QueryListAsync<CurrencyConversionResponse>(
                    dataQuery, parameters);

            int totalRecords =
                await _db.QueryFirstOrDefaultAsync<int>(
                    countQuery, parameters);

            return (data, totalRecords);
        }

        // ---------------------------------------------------------
        // GET CONVERSION BY DETAIL CODE
        // ---------------------------------------------------------
        public async Task<CurrencyConversionResponse?>
            GetConversionByIdAsync(int code)
        {
            const string query = """
                SELECT
                    CC.CODE AS Id,
                    CC.BASECURR AS BaseCurrencyId,
                    CC.ALTCURR AS AlternateCurrencyId,
                    CM.SYMBOL AS Symbol,
                    CC.TDATE AS ConversionDate,
                    CC.STDRATE AS StandardRate,
                    CC.SELLRATE AS SellRate,
                    CC.BUYRATE AS BuyRate,
                    CC.LOGCD AS UserId,
                    CC.UTIME AS UpdatedAt
                FROM dbo.CURRCONV CC
                LEFT JOIN dbo.CURRENCYMAS CM
                    ON CM.CODE = CC.ALTCURR
                WHERE CC.CODE = @Code
                  AND ISNULL(CC.DELFLG, 0) = 0;
                """;

            return await _db.QueryFirstOrDefaultAsync
                <CurrencyConversionResponse>(
                    query, new { Code = code });
        }

        // ---------------------------------------------------------
        // GET ALL CONVERSIONS BY DATE
        // ---------------------------------------------------------
        public async Task<List<CurrencyConversionResponse>>
            GetConversionsByDateAsync(DateTime conversionDate)
        {
            const string query = """
                SELECT
                    CC.CODE AS Id,
                    CC.BASECURR AS BaseCurrencyId,
                    CC.ALTCURR AS AlternateCurrencyId,
                    CM.SYMBOL AS Symbol,
                    CC.TDATE AS ConversionDate,
                    CC.STDRATE AS StandardRate,
                    CC.SELLRATE AS SellRate,
                    CC.BUYRATE AS BuyRate,
                    CC.LOGCD AS UserId,
                    CC.UTIME AS UpdatedAt
                FROM dbo.CURRCONV CC
                LEFT JOIN dbo.CURRENCYMAS CM
                    ON CM.CODE = CC.ALTCURR
                WHERE CC.TDATE = @ConversionDate
                  AND ISNULL(CC.DELFLG, 0) = 0
                ORDER BY CC.CODE;
                """;

            return await _db.QueryListAsync
                <CurrencyConversionResponse>(
                    query,
                    new { ConversionDate = conversionDate.Date });
        }

        // ---------------------------------------------------------
        // GET CONVERSION DATE BY CODE
        // ---------------------------------------------------------
        public async Task<DateTime?>
            GetConversionDateByIdAsync(int code)
        {
            const string query = """
                SELECT TDATE
                FROM dbo.CURRCONV
                WHERE CODE = @Code
                  AND ISNULL(DELFLG, 0) = 0;
                """;

            return await _db.QueryFirstOrDefaultAsync<DateTime?>(
                query, new { Code = code });
        }

        // ---------------------------------------------------------
        // CHECK DATE EXISTS
        // ---------------------------------------------------------
        public async Task<bool>
            ConversionDateExistsAsync(DateTime conversionDate)
        {
            const string query = """
                SELECT COUNT(1)
                FROM dbo.CURRCONV
                WHERE TDATE = @ConversionDate
                  AND ISNULL(DELFLG, 0) = 0;
                """;

            int count = await _db.QueryFirstOrDefaultAsync<int>(
                query,
                new { ConversionDate = conversionDate.Date });

            return count > 0;
        }

        // ---------------------------------------------------------
        // GET BASE CURRENCY
        // ---------------------------------------------------------
        public async Task<CurrencyConversionCurrencyResponse?>
            GetBaseCurrencyAsync()
        {
            const string query = """
                SELECT TOP 1
                    CODE AS Id,
                    SYMBOL AS Symbol,
                    CAST(BASEFLG AS BIT) AS IsBaseCurrency,
                    CONVMODE AS ConversionMode
                FROM dbo.CURRENCYMAS
                WHERE BASEFLG = 1
                  AND ISNULL(DELFLG, 0) = 0
                ORDER BY CODE;
                """;

            return await _db.QueryFirstOrDefaultAsync
                <CurrencyConversionCurrencyResponse>(query);
        }

        // ---------------------------------------------------------
        // GET CURRENCY BY ID
        // ---------------------------------------------------------
        public async Task<CurrencyConversionCurrencyResponse?>
            GetCurrencyByIdAsync(int currencyId)
        {
            const string query = """
                SELECT
                    CODE AS Id,
                    SYMBOL AS Symbol,
                    CAST(BASEFLG AS BIT) AS IsBaseCurrency,
                    CONVMODE AS ConversionMode
                FROM dbo.CURRENCYMAS
                WHERE CODE = @CurrencyId
                  AND ISNULL(DELFLG, 0) = 0;
                """;

            return await _db.QueryFirstOrDefaultAsync
                <CurrencyConversionCurrencyResponse>(
                    query, new { CurrencyId = currencyId });
        }

        // ---------------------------------------------------------
        // INSERT CONVERSION
        // ---------------------------------------------------------
        public async Task<int> CreateConversionAsync(
            int baseCurrencyId,
            int alternateCurrencyId,
            decimal standardRate,
            decimal sellRate,
            decimal buyRate,
            DateTime conversionDate,
            int userId)
        {
            const string query = """
                INSERT INTO dbo.CURRCONV
                (
                    BASECURR,
                    ALTCURR,
                    STDRATE,
                    SELLRATE,
                    BUYRATE,
                    TDATE,
                    LOGCD,
                    UTIME
                )
                OUTPUT INSERTED.CODE
                VALUES
                (
                    @BaseCurrencyId,
                    @AlternateCurrencyId,
                    @StandardRate,
                    @SellRate,
                    @BuyRate,
                    @ConversionDate,
                    @UserId,
                    GETDATE()
                );
                """;

            return await _db.QueryFirstOrDefaultAsync<int>(
                query,
                new
                {
                    BaseCurrencyId = baseCurrencyId,
                    AlternateCurrencyId = alternateCurrencyId,
                    StandardRate = standardRate,
                    SellRate = sellRate,
                    BuyRate = buyRate,
                    ConversionDate = conversionDate.Date,
                    UserId = userId
                });
        }


        // ---------------------------------------------------------
        // UPDATE CURRENCY CONVERSION
        // ---------------------------------------------------------
        public async Task<bool> UpdateConversionsAsync(
            DateTime oldDate,
            int baseCurrencyId,
            CurrencyConversionRequest request,
            int userId)
        {
            await _db.BeginTransaction();

            try
            {
                // Delete existing conversion records for the old date.
                await _db.ExecuteAsync(
                    """
                    DELETE FROM dbo.CURRCONV
                    WHERE TDATE = @ConversionDate;
                    """,
                    new { ConversionDate = oldDate.Date });

                // Insert the updated conversion details.
                foreach (var detail in request.Details)
                {
                    await CreateConversionAsync(
                        baseCurrencyId,
                        detail.CurrencyId,
                        detail.StandardRate,
                        detail.SellRate,
                        detail.BuyRate,
                        request.ConversionDate!.Value,
                        userId);
                }

                // Save all changes together.
                _db.CommitTransaction();

                return true;
            }
            catch
            {
                // Undo all changes if any operation fails.
                _db.RollbackTransaction();

                throw;
            }
        }

        // ---------------------------------------------------------
        // GET ALL CURRENCIES
        // ---------------------------------------------------------
        // ---------------------------------------------------------
        // GET PAGINATED CURRENCIES
        // ---------------------------------------------------------
        public async Task<(
            List<CurrencyConversionCurrencyResponse> Data,
            int TotalRecords)>
            GetCurrenciesAsync(
                int page,
                int pageSize,
                string? search = null)
        {
            int offset = (page - 1) * pageSize;

            const string dataQuery = """
        SELECT
            CODE AS Id,
            SYMBOL AS Symbol,
            CAST(BASEFLG AS BIT) AS IsBaseCurrency,
            CONVMODE AS ConversionMode
        FROM dbo.CURRENCYMAS
        WHERE ISNULL(DELFLG, 0) = 0
          AND
            (
                @Search IS NULL
                OR SYMBOL LIKE '%' + @Search + '%'
            )
        ORDER BY CODE
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY;
        """;

            const string countQuery = """
        SELECT COUNT(1)
        FROM dbo.CURRENCYMAS
        WHERE ISNULL(DELFLG, 0) = 0
          AND
            (
                @Search IS NULL
                OR SYMBOL LIKE '%' + @Search + '%'
            );
        """;

            var parameters = new
            {
                Search = string.IsNullOrWhiteSpace(search)
                    ? null
                    : search.Trim(),

                Offset = offset,
                PageSize = pageSize
            };

            var data =
                await _db.QueryListAsync<CurrencyConversionCurrencyResponse>(
                    dataQuery,
                    parameters);

            int totalRecords =
                await _db.QueryFirstOrDefaultAsync<int>(
                    countQuery,
                    parameters);

            return (data, totalRecords);
        }

        // ---------------------------------------------------------
        // DELETE ALL CONVERSIONS BY DATE
        // ---------------------------------------------------------
        public async Task<bool>
            DeleteConversionsByDateAsync(DateTime conversionDate, int userId)
        {
            const string query = """
                UPDATE dbo.CURRCONV
                SET DELFLG = 1,
                    LOGCD = @UserId,
                    UTIME = GETDATE()
                WHERE TDATE = @ConversionDate
                  AND ISNULL(DELFLG, 0) = 0;
                """;

            int affectedRows = await _db.ExecuteAsync(
                query,
                new { ConversionDate = conversionDate.Date, UserId = userId });

            return affectedRows > 0;
        }

    }
}
