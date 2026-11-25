using QuickBalance.API.CLS;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Respons_DTO;

namespace QuickBalance.API.Repository.Master
{
    public class UnitConversionRepository
    {
        private readonly DBConn _db;

        public UnitConversionRepository(DBConn db)
        {
            _db = db;
        }

        public async Task<List<UnitConversionDto>> GetAllWithoutPagination()
        {
            const string sql = @"
                SELECT
                    CODE AS Id,
                    MAINUNIT AS MainUnit,
                    SUBUNIT AS SubUnit,
                    CONVFACT AS ConvFact,
                    LOGCD AS LogCd,
                    UTIME AS UTime
                FROM dbo.PRUNITCONV
                WHERE ISNULL(DELFLG, 0) = 0
                ORDER BY CODE;";

            return await _db.QueryListAsync<UnitConversionDto>(sql);
        }


        public async Task<PagedResponse<UnitConversionDto>> GetAll(
          int page,
          int pageSize,
          string? search = null)
        {
            search = string.IsNullOrWhiteSpace(search)
                ? null
                : search.Trim();

            int? searchCode = null;

            if (int.TryParse(search, out int parsedCode))
                searchCode = parsedCode;

            const string countSql = """
            SELECT COUNT(1)
            FROM dbo.PRUNITCONV
            WHERE ISNULL(DELFLG, 0) = 0
              AND (
                @Search IS NULL
                OR MAINUNIT LIKE '%' + @Search + '%'
                OR SUBUNIT LIKE '%' + @Search + '%'
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

            const string sql = """
                         SELECT
                        C.CODE AS Id,
                        C.MAINUNIT AS MainUnit,
                        MU.PRUNITNAME AS MainUnitName,
                        C.SUBUNIT AS SubUnit,
                        SU.PRUNITNAME AS SubUnitName,
                        C.CONVFACT AS ConvFact,
                        C.LOGCD AS LogCd,
                        C.UTIME AS UTime
                    FROM dbo.PRUNITCONV C
                    LEFT JOIN dbo.PRUNITMAS MU
                        ON MU.PRUNITID = C.MAINUNIT
                    LEFT JOIN dbo.PRUNITMAS SU
                        ON SU.PRUNITID = C.SUBUNIT
                    WHERE
                        @Search IS NULL
                        OR C.MAINUNIT LIKE '%' + @Search + '%'
                        OR C.SUBUNIT LIKE '%' + @Search + '%'
                        OR (@SearchCode IS NOT NULL AND C.CODE = @SearchCode)
                    ORDER BY C.CODE
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY;
                    """;

            int offset = (page - 1) * pageSize;

            var data = (await _db.QueryListAsync<UnitConversionDto>(
                sql,
                new
                {
                    Search = search,
                    SearchCode = searchCode,
                    Offset = offset,
                    PageSize = pageSize
                })).ToList();

            return new PagedResponse<UnitConversionDto>
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
                //Pagination = new PaginationInfo
                //{
                //    Page = page,
                //    PageSize = pageSize,
                //    TotalRecords = totalRecords,
                //    TotalPages = (int)Math.Ceiling(
                //        totalRecords / (double)pageSize)
                //}
            };
        }
        public async Task<UnitConversionDto?> GetByCode(int code)
        {
            const string sql = @"
                SELECT
                    CODE AS Id,
                    MAINUNIT AS MainUnit,
                    SUBUNIT AS SubUnit,
                    CONVFACT AS ConvFact,
                    LOGCD AS LogCd,
                    UTIME AS UTime
                FROM dbo.PRUNITCONV
                WHERE CODE = @Code
                  AND ISNULL(DELFLG, 0) = 0;";

            return await _db.QueryFirstOrDefaultAsync<UnitConversionDto>(
                sql,
                new { Code = code });
        }

        public async Task<UnitConversionDto?> Create(int userId, UnitConversionRequestDto request)
        {
            const string sql = @"
            INSERT INTO dbo.PRUNITCONV
            (
                MAINUNIT,
                SUBUNIT,
                CONVFACT,
                LOGCD,
                UTIME
            )
            OUTPUT
                INSERTED.CODE AS Code,
                INSERTED.MAINUNIT AS MainUnit,
                INSERTED.SUBUNIT AS SubUnit,
                INSERTED.CONVFACT AS ConvFact,
                INSERTED.LOGCD AS LogCd,
                INSERTED.UTIME AS UTime
            VALUES
            (
                @MainUnit,
                @SubUnit,
                @ConvFact,
                @UserId,
                GETDATE()
            );";

            return await _db.QueryFirstOrDefaultAsync<UnitConversionDto>(
                sql,
                new
                {
                    request.MainUnit,
                    request.SubUnit,
                    request.ConvFact,
                    UserId = userId
                });
        }
        public async Task<UnitConversionDto?> Update(int userId,
            int code,
            UnitConversionRequestDto request)
        {
            const string sql = @"
                UPDATE dbo.PRUNITCONV
                SET
                    MAINUNIT = @MainUnit,
                    SUBUNIT = @SubUnit,
                    CONVFACT = @ConvFact,
                    LOGCD = @userId,
                    UTIME = GETDATE()
                OUTPUT
                    INSERTED.CODE AS Id,
                    INSERTED.MAINUNIT AS MainUnit,
                    INSERTED.SUBUNIT AS SubUnit,
                    INSERTED.CONVFACT AS ConvFact,
                    INSERTED.LOGCD AS LogCd,
                    INSERTED.UTIME AS UTime
                WHERE CODE = @Code;";

            return await _db.QueryFirstOrDefaultAsync<UnitConversionDto>(
                sql,
                new
                {
                    Code = code,
                    request.MainUnit,
                    request.SubUnit,
                    request.ConvFact,
                    userId = userId
                });
        }

        public async Task<int> Delete(int code, int userId)
        {
            const string sql = @"
                UPDATE dbo.PRUNITCONV
                SET DELFLG = 1,
                    LOGCD = @UserId,
                    UTIME = GETDATE()
                WHERE CODE = @Code
                  AND ISNULL(DELFLG, 0) = 0;";

            return await _db.ExecuteAsync(
                sql,
                new { Code = code, UserId = userId });
        }
    }
}
