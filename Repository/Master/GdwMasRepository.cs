using QuickBalance.API.CLS;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Respons_DTO;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace QuickBalance.API.Repository.Master
{
    public class GdwMasRepository
    {
        private readonly DBConn _db;

        public GdwMasRepository(DBConn db)
        {
            _db = db;
        }

        // =========================================================
        // CHECK LOCATION NAME
        // =========================================================

        public async Task<bool> NameExistsAsync(
            string locationName,
            int? excludeLocationId = null)
        {
            const string sql = @"
                SELECT CASE
                    WHEN EXISTS
                    (
                        SELECT 1
                        FROM dbo.GDWMAS
                        WHERE  GDWNAME = @LocationName
                          AND (@ExcludeLocationId IS NULL
                               OR GDWID <> @ExcludeLocationId)
                    )
                    THEN 1 ELSE 0
                END;";

            int result = await _db.QueryFirstOrDefaultAsync<int>(
                sql,
                new
                {
                    LocationName = locationName,
                    ExcludeLocationId = excludeLocationId
                });

            return result == 1;
        }

        // =========================================================
        // CHECK LOCATION GROUP
        // =========================================================

        public async Task<bool> GroupExistsAsync(int groupId)
        {
            const string sql = @"
                SELECT CASE WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.GDWGRP
                    WHERE GRPID = @GroupId
                )
                THEN 1 ELSE 0 END;";

            int result = await _db.QueryFirstOrDefaultAsync<int>(
                sql,
                new { GroupId = groupId });

            return result == 1;
        }

        // =========================================================
        // CHECK STOCK ACCOUNT
        // =========================================================

        public async Task<bool> StockAccountExistsAsync(int accountId)
        {
            const string sql = @"
                SELECT CASE WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.ACCMAS
                    WHERE ID = @AccountId
                )
                THEN 1 ELSE 0 END;";

            int result = await _db.QueryFirstOrDefaultAsync<int>(
                sql,
                new { AccountId = accountId });

            return result == 1;
        }

        // =========================================================
        // CREATE
        // =========================================================

        public async Task<int> CreateAsync(
            LocationMasterRequest request,
            int userId)
        {
            const string sql = @"
                INSERT INTO dbo.GDWMAS
                (
                    GDWNAME,
                    GRPID,
                    ACTIVE,
                    STKACCID,
                    STKPLFLG,
                    LOGCD,
                    UTIME,
                    SECNAME
                )
                OUTPUT INSERTED.GDWID
                VALUES
                (
                    @LocationName,
                    @GroupId,
                    @IsActive,
                    @StockAccountId,
                    @StockPLFlag,
                    @UserId,
                    GETDATE(),
                    @SecondName
                );";

            return await _db.QueryFirstOrDefaultAsync<int>(
                sql,
                new
                {
                    LocationName = request.LocationName.Trim(),
                    request.GroupId,
                    request.IsActive,
                    request.StockAccountId,
                    request.StockPLFlag,
                    UserId = userId,
                    SecondName = request.SecondName?.Trim() ?? string.Empty
                });
        }

        // =========================================================
        // UPDATE
        // =========================================================

        public async Task<int> UpdateAsync(
            int id,
            LocationMasterRequest request)
        {
            const string sql = @"
                UPDATE dbo.GDWMAS
                SET
                    GDWNAME = @LocationName,
                    GRPID = @GroupId,
                    ACTIVE = @IsActive,
                    STKACCID = @StockAccountId,
                    STKPLFLG = @StockPLFlag,
                    SECNAME = @SecondName
                WHERE GDWID = @Id;";

            return await _db.ExecuteAsync(
                sql,
                new
                {
                    Id = id,
                    LocationName = request.LocationName.Trim(),
                    request.GroupId,
                    request.IsActive,
                    request.StockAccountId,
                    request.StockPLFlag,
                    SecondName = request.SecondName?.Trim() ?? string.Empty
                });
        }


        public async Task<bool> DeleteAsync(
            int id,
            int userId)
        {
            const string sql = @"
                UPDATE dbo.GDWMAS
                SET
                    DELFLG = 1,
                    ACTIVE = 0,
                    LOGCD = @UserId,
                    UTIME = GETDATE()
                WHERE GDWID = @Id
                  AND ISNULL(DELFLG, 0) = 0;";

            var rowsAffected= await _db.ExecuteAsync(
                sql,
                new
                {
                    Id = id,
                    UserId = userId
                });

            return rowsAffected > 0;


        }

        // =========================================================
        // GET BY ID
        // =========================================================

        public async Task<LocationDto?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT
                    GDWID AS Id,
                    GDWNAME AS LocationName,
                    GRPID AS GroupId,
                    ACTIVE AS IsActive,
                    STKACCID AS StockAccountId,
                    STKPLFLG AS StockPLFlag,
                    SECNAME AS SecondName,
                    LOGCD AS LogCd,
                    UTIME AS UTime
                FROM dbo.GDWMAS
                WHERE ISNULL(DELFLG, 0) = 0 AND GDWID = @Id;";

            return await _db.QueryFirstOrDefaultAsync<LocationDto>(
                sql,
                new { Id = id });
        }

        // =========================================================
        // GET ALL - PAGINATED
        // =========================================================

        public async Task<PagedResponse<LocationListItemDto>> GetAllAsync(
            int page,
            int pageSize,
            string? search,
            bool? active = null)

        {
            string? searchValue = string.IsNullOrWhiteSpace(search)
                ? null
                : search.Trim();

            // Only allow actual database columns.
            //var sortColumns = new Dictionary<string, string>(
            //    StringComparer.OrdinalIgnoreCase)
            //{
            //    ["GDWID"] = "GDWID",
            //    ["GDWNAME"] = "GDWNAME",
            //    ["GRPID"] = "GRPID",
            //    ["ACTIVE"] = "ACTIVE",
            //    ["STKACCID"] = "STKACCID",
            //    ["STKPLFLG"] = "STKPLFLG",
            //    ["SECNAME"] = "SECNAME",
            //    ["LOGCD"] = "LOGCD",
            //    ["UTIME"] = "UTIME"
            //};

            //string orderColumn =
            //    !string.IsNullOrWhiteSpace(sortBy) &&
            //    sortColumns.TryGetValue(sortBy.Trim(), out var selectedColumn)
            //        ? selectedColumn
            //        : "GDWNAME";

            //string orderDirection =
            //    string.Equals(
            //        sortDirection,
            //        "DESC",
            //        StringComparison.OrdinalIgnoreCase)
            //        ? "DESC"
            //        : "ASC";
            string whereClause = $@"
                WHERE ISNULL(DELFLG, 0) = 0
                    AND (@Active IS NULL OR ISNULL(ACTIVE, 0) = @Active)
                    AND
                    (
                        @Search IS NULL
                        OR GDWNAME LIKE '%' + @Search + '%'
                        OR SECNAME LIKE '%' + @Search + '%'
                        OR CONVERT(VARCHAR(20), GDWID) LIKE '%' + @Search + '%'
                        OR CONVERT(VARCHAR(20), GRPID) LIKE '%' + @Search + '%'
                        OR CONVERT(VARCHAR(20), STKACCID) LIKE '%' + @Search + '%'
                        OR CONVERT(VARCHAR(20), LOGCD) LIKE '%' + @Search + '%'
                    )";

            string countSql = $@"
                SELECT COUNT(1)
                FROM dbo.GDWMAS
                {whereClause};";

            int totalRecords =
                await _db.QueryFirstOrDefaultAsync<int>(
                    countSql,
                    new { Search = searchValue, Active = active });

            string dataSql = $@"
                SELECT
                    GDWID AS Id,
                    GDWNAME AS LocationName,
                    GRPID AS GroupId,
                    ACTIVE AS IsActive,
                    STKACCID AS StockAccountId,
                    STKPLFLG AS StockPLFlag,
                    SECNAME AS SecondName
                FROM dbo.GDWMAS
                {whereClause}
                ORDER BY GDWID  DESC
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY;";

            var data = await _db.QueryListAsync<LocationListItemDto>(
                dataSql,
                new
                {
                    Search = searchValue,
                    Active = active,
                    Offset = (page - 1) * pageSize,
                    PageSize = pageSize
                });

            int totalPages = (int)Math.Ceiling(
                totalRecords / (double)pageSize);

            return new PagedResponse<LocationListItemDto>
            {
                Data = data.ToList(),
                Pagination = new PaginationInfo
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    TotalPages = totalPages
                }
            };
        }

        // =========================================================
        // ACTIVE LOCATIONS
        // =========================================================

        public async Task<IReadOnlyList<LocationActiveDto>> GetActiveAsync()
        {
            const string sql = @"
                SELECT
                    GDWID AS Id,
                    GDWNAME AS LocationName
                FROM dbo.GDWMAS
              
                ORDER BY GDWNAME;";
            //WHERE ACTIVE = 1
            var result = await _db.QueryListAsync<LocationActiveDto>(
                sql,
                new { });

            return result.ToList();
        }

        // =========================================================
        // SEARCH
        // =========================================================

        public async Task<IReadOnlyList<LocationDropdownDto>> SearchAsync(
            string search)
        {
            const string sql = @"
                SELECT
                    GDWID AS Id,
                    GDWNAME AS Godown
                FROM dbo.GDWMAS
                WHERE
                    CONVERT(VARCHAR(20), GDWID) LIKE '%' + @Search + '%'
                    OR GDWNAME LIKE '%' + @Search + '%'
                ORDER BY GDWNAME;";
            //ACTIVE = 1 AND
           var result = await _db.QueryListAsync<LocationDropdownDto>(
                sql,
                new { Search = search });

            return result.ToList();
        }

        // =========================================================
        // DROPDOWN
        // =========================================================

        public async Task<IReadOnlyList<LocationDropdownDto>> GetDropdownAsync()
        {
            const string sql = @"
                SELECT
                    GDWID AS Id,
                    GDWNAME AS Godown
                FROM dbo.GDWMAS
                WHERE ISNULL(DELFLG, 0) = 0 AND ISNULL(ACTIVE, 1) = 1
                ORDER BY GDWNAME;";

            var result = await _db.QueryListAsync<LocationDropdownDto>(
                sql,
                new { });

            return result.ToList();
        }

        // =========================================================
        // CHECK WHETHER STOCK ACCOUNT IS USED
        // =========================================================

        public async Task<bool> IsStockAccountUsedAsync(int accountId)
        {
            const string sql = @"
                SELECT CASE WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.GDWMAS
                    WHERE  STKACCID = @AccountId
                )
                THEN 1 ELSE 0 END;";

            int result = await _db.QueryFirstOrDefaultAsync<int>(
                sql,
                new { AccountId = accountId });

            return result == 1;
        }
    }
}