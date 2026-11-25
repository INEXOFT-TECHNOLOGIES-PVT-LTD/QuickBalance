using QuickBalance.API.CLS;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs.Currencies;
using QuickBalance.API.DTOs.Respons_DTO;

namespace QuickBalance.API.Repository
{
    public class CurrencyRepository
    {
        private readonly DBConn _db;

        public CurrencyRepository(DBConn db)
        {
            _db = db;
        }




        public async Task<List<CurrencyResponse>> GetCurrenciesAsync()
        {
            const string query = """
            SELECT
                CODE AS Code,
                SYMBOL AS Symbol,
                CURRSTRING AS CurrString,
                [SUBSTRING] AS SubString,   
                CONVMODE AS ConvMode,
                BASEFLG AS BaseFlg,
                SECSYMBOL AS SecSymbol,
                SECCURSTRNG AS SecCurrString,
                SECSUBSTRNG AS SecSubString
            FROM dbo.CURRENCYMAS
            WHERE ISNULL(DELFLG, 0) = 0
            ORDER BY CODE;
            """;

            return await _db.QueryListAsync<CurrencyResponse>(query);
        }



        public async Task<ApiResponse<IReadOnlyList<CurrencyResponse>>> GetCurrenciesAsync(int page,int pageSize,string? search = null,bool excludeBase = false)
        {
            if (page < 1)
               return ApiResponse<IReadOnlyList<CurrencyResponse>>.Failure("Page number must be greater than zero.");

            if (pageSize < 1 || pageSize > 200)
                return ApiResponse<IReadOnlyList<CurrencyResponse>>.Failure("Page size must be between 1 and 200.");

            search = string.IsNullOrWhiteSpace(search)
                ? null
                : search.Trim();

            int? searchCode = null;

            if (int.TryParse(search, out int parsedCode))
                searchCode = parsedCode;

            const string countSql = """
            SELECT COUNT(1)
            FROM dbo.CURRENCYMAS
            WHERE ISNULL(DELFLG, 0) = 0
              AND (@ExcludeBase = 0 OR ISNULL(BASEFLG, 0) = 0)
              AND (
                @Search IS NULL
                OR SYMBOL LIKE '%' + @Search + '%'
                OR CURRSTRING LIKE '%' + @Search + '%'
                OR [SUBSTRING] LIKE '%' + @Search + '%'
                OR SECSYMBOL LIKE '%' + @Search + '%'
                OR SECCURSTRNG LIKE '%' + @Search + '%'
                OR SECSUBSTRNG LIKE '%' + @Search + '%'
                OR (@SearchCode IS NOT NULL AND CODE = @SearchCode)
              );
            """;

            int totalRecords = await _db.QueryFirstOrDefaultAsync<int>(
                countSql,
                new
                {
                    Search = search,
                    SearchCode = searchCode,
                    ExcludeBase = excludeBase
                });

            const string query = """
            SELECT
                CODE AS Id,
                SYMBOL AS Symbol,
                CURRSTRING AS CurrString,
                [SUBSTRING] AS SubString,
                CONVMODE AS ConvMode,
                BASEFLG AS BaseFlg,
                SECSYMBOL AS SecSymbol,
                SECCURSTRNG AS SecCurrString,
                SECSUBSTRNG AS SecSubString
            FROM dbo.CURRENCYMAS
            WHERE ISNULL(DELFLG, 0) = 0
              AND (@ExcludeBase = 0 OR ISNULL(BASEFLG, 0) = 0)
              AND (
                @Search IS NULL
                OR SYMBOL LIKE '%' + @Search + '%'
                OR CURRSTRING LIKE '%' + @Search + '%'
                OR [SUBSTRING] LIKE '%' + @Search + '%'
                OR SECSYMBOL LIKE '%' + @Search + '%'
                OR SECCURSTRNG LIKE '%' + @Search + '%'
                OR SECSUBSTRNG LIKE '%' + @Search + '%'
                OR (@SearchCode IS NOT NULL AND CODE = @SearchCode)
              )
            ORDER BY CODE
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;
            """;

            int offset = (page - 1) * pageSize;

            var data = (await _db.QueryListAsync<CurrencyResponse>(
                query,
                new
                {
                    Search = search,
                    SearchCode = searchCode,
                    ExcludeBase = excludeBase,
                    Offset = offset,
                    PageSize = pageSize
                })).ToList();

            return new ApiResponse<IReadOnlyList<CurrencyResponse>>
            {
                Data = data,
                Pagination = new PaginationInfo
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    TotalPages = (int)Math.Ceiling(
                        totalRecords / (double)pageSize)
                }
            };
        }



        public async Task<ApiResponse<IReadOnlyList<CurrencyResponse>>> GetAlternateCurrenciesAsync(
            int page,
            int pageSize,
            string? search = null)
        {
            if (page < 1)
                return ApiResponse<IReadOnlyList<CurrencyResponse>>.Failure(
                    "Page number must be greater than zero.");

            if (pageSize < 1 || pageSize > 200)
                return ApiResponse<IReadOnlyList<CurrencyResponse>>.Failure(
                    "Page size must be between 1 and 200.");

            search = string.IsNullOrWhiteSpace(search)
                ? null
                : search.Trim();

            int? searchCode = null;

            if (int.TryParse(search, out int parsedCode))
                searchCode = parsedCode;

            const string countSql = """
            SELECT COUNT(1)
            FROM dbo.CURRENCYMAS
            WHERE ISNULL(DELFLG, 0) = 0
              AND BASEFLG = 0
              AND
              (
                  @Search IS NULL
                  OR SYMBOL LIKE '%' + @Search + '%'
                  OR CURRSTRING LIKE '%' + @Search + '%'
                  OR [SUBSTRING] LIKE '%' + @Search + '%'
                  OR SECSYMBOL LIKE '%' + @Search + '%'
                  OR SECCURSTRNG LIKE '%' + @Search + '%'
                  OR SECSUBSTRNG LIKE '%' + @Search + '%'
                  OR (@SearchCode IS NOT NULL AND CODE = @SearchCode)
              );
            """;

            int totalRecords = await _db.QueryFirstOrDefaultAsync<int>(
                countSql,
                new
                {
                    Search = search,
                    SearchCode = searchCode
                });

            const string query = """
            SELECT
                CODE AS Id,
                SYMBOL AS Symbol,
                CURRSTRING AS CurrString,
                [SUBSTRING] AS SubString,
                CONVMODE AS ConvMode,
                BASEFLG AS BaseFlg,
                SECSYMBOL AS SecSymbol,
                SECCURSTRNG AS SecCurrString,
                SECSUBSTRNG AS SecSubString
            FROM dbo.CURRENCYMAS
            WHERE ISNULL(DELFLG, 0) = 0
              AND BASEFLG = 0
              AND
              (
                  @Search IS NULL
                  OR SYMBOL LIKE '%' + @Search + '%'
                  OR CURRSTRING LIKE '%' + @Search + '%'
                  OR [SUBSTRING] LIKE '%' + @Search + '%'
                  OR SECSYMBOL LIKE '%' + @Search + '%'
                  OR SECCURSTRNG LIKE '%' + @Search + '%'
                  OR SECSUBSTRNG LIKE '%' + @Search + '%'
                  OR (@SearchCode IS NOT NULL AND CODE = @SearchCode)
              )
            ORDER BY CODE
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;
            """;

            int offset = (page - 1) * pageSize;

            var data = (await _db.QueryListAsync<CurrencyResponse>(
                query,
                new
                {
                    Search = search,
                    SearchCode = searchCode,
                    Offset = offset,
                    PageSize = pageSize
                })).ToList();

            return new ApiResponse<IReadOnlyList<CurrencyResponse>>
            {
                Data = data,
                Pagination = new PaginationInfo
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    TotalPages = (int)Math.Ceiling(
                        totalRecords / (double)pageSize)
                }
            };
        }

        public async Task<CurrencyResponse?> GetCurrencyByIdAsync(int code)
        {
            const string query = """
            SELECT
                CODE AS Id,
                SYMBOL AS Symbol,
                CURRSTRING AS CurrString,
                [SUBSTRING] AS SubString,
                CONVMODE AS ConvMode,
                BASEFLG AS BaseFlg,
                SECSYMBOL AS SecSymbol,
                SECCURSTRNG AS SecCurrString,
                SECSUBSTRNG AS SecSubString
            FROM dbo.CURRENCYMAS
            WHERE CODE = @Code
              AND ISNULL(DELFLG, 0) = 0;
            """;

            return await _db.QueryFirstOrDefaultAsync<CurrencyResponse>(
                query,
                new { Code = code });
        }
        // CREATE CURRENCY
        public async Task<int> CreateCurrencyAsync(
            CurrencyRequest request)
        {
            const string query = """
            INSERT INTO dbo.CURRENCYMAS
            (
                SYMBOL,
                CURRSTRING,
                [SUBSTRING],
                CONVMODE,
                BASEFLG,
                SECSYMBOL,
                SECCURSTRNG,
                SECSUBSTRNG
            )
            OUTPUT INSERTED.CODE
            VALUES
            (
                @Symbol,
                @CurrString,
                @SubString,
                @ConvMode,
                @BaseFlg,
                @SecSymbol,
                @SecCurrString,
                @SecSubString
            );
            """;

            return await _db.QueryFirstOrDefaultAsync<int>(
                query,
                new
                {
                    request.Symbol,
                    request.CurrString,
                    request.SubString,
                    request.ConvMode,
                    request.BaseFlg,
                    request.SecSymbol,
                    request.SecCurrString,
                    request.SecSubString
                });
        }

        // UPDATE CURRENCY


        //new vlaidation duplicate checking 
        public async Task<bool> CurrencySymbolExistsAsync(string symbol, int? excludeCode = null)
        {
            const string query = """
            SELECT COUNT(1)
            FROM dbo.CURRENCYMAS
            WHERE UPPER(LTRIM(RTRIM(SYMBOL))) =
                  UPPER(LTRIM(RTRIM(@Symbol)))
              AND ISNULL(DELFLG, 0) = 0
              AND (@ExcludeCode IS NULL OR CODE <> @ExcludeCode);
            """;

            int count = await _db.QueryFirstOrDefaultAsync<int>(
                query,
                new
                {
                    Symbol = symbol,
                    ExcludeCode = excludeCode
                });

            Console.WriteLine($"Duplicate Check Symbol: {symbol}");
            Console.WriteLine($"Exclude Code: {excludeCode}");
            Console.WriteLine($"Duplicate Count: {count}");

            return count > 0;
        }
        public async Task<bool> UpdateCurrencyAsync(
            int code,
            CurrencyRequest request)
        {
            const string query = """
            UPDATE dbo.CURRENCYMAS
            SET
                SYMBOL = @Symbol,
                CURRSTRING = @CurrString,
                [SUBSTRING] = @SubString,
                CONVMODE = @ConvMode,
                SECSYMBOL = @SecSymbol,
                SECCURSTRNG = @SecCurrString,
                SECSUBSTRNG = @SecSubString,
                BASEFLG=@BaseFlg
            WHERE CODE = @Code;
            """;

            var affectedRows = await _db.ExecuteAsync(
                query,
                new
                {
                    Code = code,
                    request.Symbol,
                    request.CurrString,
                    request.SubString,
                    request.ConvMode,
                    request.SecSymbol,
                    request.SecCurrString,
                    request.SecSubString,
                    request.BaseFlg,
                });

            return affectedRows > 0;
        }


        public async Task<bool> DeleteCurrencyAsync(int code, int userId)
        {
            const string query = """
            UPDATE dbo.CURRENCYMAS
            SET DELFLG = 1,
                LOGCD = @UserId,
                UTIME = GETDATE()
            WHERE CODE = @Code
              AND ISNULL(DELFLG, 0) = 0;
            """;

            var affectedRows = await _db.ExecuteAsync(
                query,
                new { Code = code, UserId = userId });

            return affectedRows > 0;
        }
    }
}