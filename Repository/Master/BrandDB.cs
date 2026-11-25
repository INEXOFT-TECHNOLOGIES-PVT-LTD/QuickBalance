using global::QuickBalance.API.CLS;
using global::QuickBalance.API.DB;
using global::QuickBalance.API.DTOs;
using global::QuickBalance.API.DTOs.Brand;
using global::QuickBalance.API.DTOs.Respons_DTO;

namespace QuickBalance.API.Repository.Master
{
  

    public sealed class BrandDB
    {
        private readonly DBConn _db;

        public BrandDB(DBConn db)
        {
            _db = db;
        }

        private const string SelectColumns = @"
        ID AS Id,
        BRANDNAME AS BrandName,
        SECNAME AS SecondName,
        ALIAS AS Alias
    ";

        public async Task<int> InsertAsync(BrandRequest request)
        {
            const string query = @"
            INSERT INTO dbo.BRANDMAS
            (
                BRANDNAME,
                SECNAME,
                ALIAS
            )
            OUTPUT INSERTED.ID
            VALUES
            (
                @BrandName,
                @SecondName,
                @Alias
            );";

            var result = await _db.QueryFirstOrDefaultAsync<int>(
                query,
                new
                {
                    request.BrandName,
                    request.SecondName,
                    request.Alias
                });

            return result;
        }

        public async Task<BrandResponse?> GetByIdAsync(int id)
        {
            string query = $@"
            SELECT {SelectColumns}
            FROM dbo.BRANDMAS
            WHERE ID = @Id
              AND ISNULL(DELFLG, 0) = 0;";

            return await _db.QueryFirstOrDefaultAsync<BrandResponse>(
                query,
                new { Id = id });
        }

        public async Task<PagedResponse<BrandResponse>> GetAllAsync(
            int page, int pageSize, string? search)
        {
            string where = @"
            WHERE ISNULL(DELFLG, 0) = 0
              AND (
                @Search IS NULL
                OR BRANDNAME LIKE @Search
                OR SECNAME LIKE @Search
                OR ALIAS LIKE @Search
              )";

            string countQuery = $@"
            SELECT COUNT(*)
            FROM dbo.BRANDMAS
            {where};";

            string dataQuery = $@"
            SELECT {SelectColumns}
            FROM dbo.BRANDMAS
            {where}
            ORDER BY BRANDNAME, ID
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;";

            string? searchValue = string.IsNullOrWhiteSpace(search)
                ? null
                : $"%{search.Trim()}%";

            var parameters = new
            {
                Search = searchValue,
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            };

            int totalRecords =
                await _db.QueryFirstOrDefaultAsync<int>(
                    countQuery, parameters);

            var data = await _db.QueryListAsync<BrandResponse>(
                dataQuery, parameters);

            int totalPages = totalRecords == 0
                ? 0
                : (int)Math.Ceiling(totalRecords / (double)pageSize);

            return new PagedResponse<BrandResponse>
            {
                Data = data, //.ToList(),
                Pagination = new PaginationInfo
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    TotalPages = totalPages
                }
            };
        }

        public async Task<bool> NameExistsAsync(
            string brandName, int? excludeId = null)
        {
            const string query = @"
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM dbo.BRANDMAS
                WHERE BRANDNAME = @BrandName
                  AND ISNULL(DELFLG, 0) = 0
                  AND (@ExcludeId IS NULL OR ID <> @ExcludeId)
            )
            THEN 1 ELSE 0 END;";

            return await _db.QueryFirstOrDefaultAsync<bool>(
                query,
                new { BrandName = brandName, ExcludeId = excludeId });
        }

        public async Task<bool> SecondNameExistsAsync(
            string secondName, int? excludeId = null)
        {
            const string query = @"
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM dbo.BRANDMAS
                WHERE SECNAME = @SecondName
                  AND ISNULL(DELFLG, 0) = 0
                  AND (@ExcludeId IS NULL OR ID <> @ExcludeId)
            )
            THEN 1 ELSE 0 END;";

            return await _db.QueryFirstOrDefaultAsync<bool>(
                query,
                new { SecondName = secondName, ExcludeId = excludeId });
        }

        public async Task<bool> AliasExistsAsync(
            string alias, int? excludeId = null)
        {
            const string query = @"
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM dbo.BRANDMAS
                WHERE ALIAS = @Alias
                  AND ISNULL(DELFLG, 0) = 0
                  AND (@ExcludeId IS NULL OR ID <> @ExcludeId)
            )
            THEN 1 ELSE 0 END;";

            return await _db.QueryFirstOrDefaultAsync<bool>(
                query,
                new { Alias = alias, ExcludeId = excludeId });
        }

        public async Task<int> UpdateAsync(
            int id, BrandRequest request)
        {
            const string query = @"
            UPDATE dbo.BRANDMAS
            SET
                BRANDNAME = @BrandName,
                SECNAME = @SecondName,
                ALIAS = @Alias
            WHERE ID = @Id;";

            return await _db.ExecuteAsync(
                query,
                new
                {
                    Id = id,
                    request.BrandName,
                    request.SecondName,
                    request.Alias
                });
        }

        public async Task<int> DeleteAsync(int id, int userId)
        {
            const string query = @"
            UPDATE dbo.BRANDMAS
            SET DELFLG = 1,
                LOGCD = @UserId,
                UTIME = GETDATE()
            WHERE ID = @Id
              AND ISNULL(DELFLG, 0) = 0;";

            return await _db.ExecuteAsync(query, new { Id = id, UserId = userId });
        }

        public async Task<List<BrandResponse>> GetActiveAsync()
        {
            string query = $@"
            SELECT
                ID AS Id,
                BRANDNAME AS BrandName
            FROM dbo.BRANDMAS
            WHERE ISNULL(DELFLG, 0) = 0
            ORDER BY BRANDNAME;";

            var result = await _db.QueryListAsync<BrandResponse>(query);

            return result;
        }
    }
}
