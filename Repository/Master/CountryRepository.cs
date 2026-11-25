using Dapper;
using QuickBalance.API.CLS;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Respons_DTO;

namespace QuickBalance.API.Repository.Master
{
    public class CountryRepository
    {
        private readonly DBConn _db;

        public CountryRepository(DBConn db)
        {
            _db = db;
        }

        public async Task<List<CountryDtos>> GetCountries()
        {
            const string sql = @"
            SELECT
                COUNTRYID AS Id,
                COUNTRYNAME AS CountryName,
                VATNOLIMIT AS VatNoLimit,
                VATCHARALLOW AS VatCharAllow
            FROM dbo.COUNTRY
            WHERE ISNULL(ACTIVE, 1) = 1
            ORDER BY COUNTRYNAME;";

            return await _db.QueryMasterListAsync<CountryDtos>(sql);
        }




        public async Task<PagedResponse<CountryDtos>> GetCountrieFull(int page,int pageSize,string? search)
        {

            search = string.IsNullOrWhiteSpace(search)
                ? null
                : search.Trim();

            int? searchId = null;

            if (int.TryParse(search, out int parsedId))
                searchId = parsedId;

            const string countSql = """
            SELECT COUNT(1)
            FROM dbo.COUNTRY
            WHERE ACTIVE = 1
              AND
              (
                  @Search IS NULL
                  OR COUNTRYNAME LIKE '%' + @Search + '%'
                  OR (@SearchId IS NOT NULL AND COUNTRYID = @SearchId)
              );
            """;

            int totalRecords = await _db.QueryMasterFirstOrDefaultAsync<int>(
                countSql,
                new
                {
                    Search = search,
                    SearchId = searchId
                });

            const string sql = """
            SELECT
                COUNTRYID AS Id,
                COUNTRYNAME AS CountryName,
                ACTIVE AS Active,
                CURSYMBOL AS CurSymbol,
                FARABIC AS Farabic,
                FORMALNAME AS FormalName,
                SARABIC AS Sarabic,
                CURNAME AS CurName,
                TARABIC AS Tarabic,
                VAT AS Vat,
                VATPER AS VatPer,
                LNG AS Lng,
                VATLABEL AS VatLabel,
                VATNOLIMIT AS VatNoLimit,
                ISO AS Iso,
                ISO3 AS Iso3,
                NUMCODE AS NumCode,
                PHCODE AS PhCode,
                COUNTRYSECNAME AS CountrySecName,
                VATCHARALLOW AS VatCharAllow
            FROM dbo.COUNTRY
            WHERE ACTIVE = 1
              AND
              (
                  @Search IS NULL
                  OR COUNTRYNAME LIKE '%' + @Search + '%'
                  OR (@SearchId IS NOT NULL AND COUNTRYID = @SearchId)
              )
            ORDER BY COUNTRYNAME, COUNTRYID
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;
            """;

            int offset = (page - 1) * pageSize;

            var data = (await _db.QueryMasterListAsync<CountryDtos>(
                sql,
                new
                {
                    Search = search,
                    SearchId = searchId,
                    Offset = offset,
                    PageSize = pageSize
                })).ToList();

            return new PagedResponse<CountryDtos>
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

        public async Task<CountryDtos?> GetCountryById(int id)
        {
            const string sql = """
                SELECT
                    COUNTRYID AS Id,
                    COUNTRYNAME AS CountryName,
                    ACTIVE AS Active,
                    CURSYMBOL AS CurSymbol,
                    FARABIC AS Farabic,
                    FORMALNAME AS FormalName,
                    SARABIC AS Sarabic,
                    CURNAME AS CurName,
                    TARABIC AS Tarabic,
                    VAT AS Vat,
                    VATPER AS VatPer,
                    LNG AS Lng,
                    VATLABEL AS VatLabel,
                    VATNOLIMIT AS VatNoLimit,
                    ISO AS Iso,
                    ISO3 AS Iso3,
                    NUMCODE AS NumCode,
                    PHCODE AS PhCode,
                    COUNTRYSECNAME AS CountrySecName,
                    VATCHARALLOW AS VatCharAllow
                FROM dbo.COUNTRY
                WHERE COUNTRYID = @Id
                  AND ACTIVE = 1;
                """;


            return await _db.QueryMasterFirstOrDefaultAsync<CountryDtos>(
                sql,
                new { Id = id });
        }

        public async Task<bool> CountryExists(string countryName, int? excludeId = null)
        {
            const string sql = """
                SELECT COUNT(1)
                FROM dbo.COUNTRY
                WHERE UPPER(LTRIM(RTRIM(COUNTRYNAME))) =
                      UPPER(LTRIM(RTRIM(@CountryName)))
                  AND ACTIVE = 1
                  AND (@ExcludeId IS NULL OR COUNTRYID <> @ExcludeId);
                """;


            int count = await _db.QueryMasterFirstOrDefaultAsync<int>(
                sql,
                new { CountryName = countryName, ExcludeId = excludeId });

            return count > 0;
        }

        public async Task<int> InsertCountry(CountryRequest request)
        {
            const string sql = """
                INSERT INTO dbo.COUNTRY
                (
                    COUNTRYNAME, ACTIVE, CURSYMBOL, FARABIC,
                    FORMALNAME, SARABIC, CURNAME, TARABIC,
                    VAT, VATPER, LNG, VATLABEL, VATNOLIMIT,
                    ISO, ISO3, NUMCODE, PHCODE,
                    COUNTRYSECNAME, VATCHARALLOW
                )
                OUTPUT INSERTED.COUNTRYID
                VALUES
                (
                    @CountryName, 1, @CurSymbol, @Farabic,
                    @FormalName, @Sarabic, @CurName, @Tarabic,
                    @Vat, @VatPer, @Lng, @VatLabel, @VatNoLimit,
                    @Iso, @Iso3, @NumCode, @PhCode,
                    @CountrySecName, @VatCharAllow
                );
                """;


            return await _db.QueryMasterScalarAsync<int>(
                sql,
                request);
        }

        public async Task<int> UpdateCountry(int id, CountryRequest request)
        {
            const string sql = """
                UPDATE dbo.COUNTRY
                SET
                    COUNTRYNAME = @CountryName,
                    CURSYMBOL = @CurSymbol,
                    FARABIC = @Farabic,
                    FORMALNAME = @FormalName,
                    SARABIC = @Sarabic,
                    CURNAME = @CurName,
                    TARABIC = @Tarabic,
                    VAT = @Vat,
                    VATPER = @VatPer,
                    LNG = @Lng,
                    VATLABEL = @VatLabel,
                    VATNOLIMIT = @VatNoLimit,
                    ISO = @Iso,
                    ISO3 = @Iso3,
                    NUMCODE = @NumCode,
                    PHCODE = @PhCode,
                    COUNTRYSECNAME = @CountrySecName,
                    VATCHARALLOW = @VatCharAllow
                WHERE COUNTRYID = @Id
                  AND ACTIVE = 1;
                """;


            return await _db.ExecuteMasterAsync(
                sql,
                new
                {
                    Id = id,
                    request.CountryName,
                    request.CurSymbol,
                    request.Farabic,
                    request.FormalName,
                    request.Sarabic,
                    request.CurName,
                    request.Tarabic,
                    request.Vat,
                    request.VatPer,
                    request.Lng,
                    request.VatLabel,
                    request.VatNoLimit,
                    request.Iso,
                    request.Iso3,
                    request.NumCode,
                    request.PhCode,
                    request.CountrySecName,
                    request.VatCharAllow
                });
        }

        public async Task<int> DeleteCountry(int id)
        {
            const string sql = """
                UPDATE dbo.COUNTRY
                SET ACTIVE = 0
                WHERE COUNTRYID = @Id
                  AND ACTIVE = 1;
                """;


            return await _db.ExecuteMasterAsync(
                sql,
                new { Id = id });
        }
    }
}
