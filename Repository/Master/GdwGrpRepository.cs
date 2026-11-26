using QuickBalance.API.CLS;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Respons_DTO;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace QuickBalance.API.Repository.Master
{
    public class GdwGrpRepository
    {
        private readonly DBConn _db;

        public GdwGrpRepository(DBConn db)
        {
            _db = db;
        }

        // =========================================================
        // CREATE
        // =========================================================

        public async Task<int> CreateAsync(
            LocationGroupMasterRequest request,
            int userId)
        {
            const string sql = @"
                INSERT INTO dbo.GDWGRP
                (
                    GRPNAME,
                    ACTIVE,
                    LOGCD,
                    UTIME,
                    SECNAME
                )
                OUTPUT INSERTED.GRPID
                VALUES
                (
                    @GroupName,
                    @IsActive,
                    @LogCd,
                    GETDATE(),
                    @SecondName
                );";

            return await _db.QueryFirstOrDefaultAsync<int>(
                sql,
                new
                {
                    GroupName = request.GroupName.Trim(),
                    IsActive = request.IsActive,
                    LogCd = userId,
                    SecondName = request.SecondName?.Trim() ?? string.Empty
                });
        }

        // =========================================================
        // GET ALL - PAGINATION + SEARCH + SORT
        // =========================================================

        public async Task<PagedResponse<LocationGroupDto>> GetAllAsync(
            int page,
            int pageSize,
            string? search,
            bool? active = null)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            string whereSql = @"
                WHERE ISNULL(DELFLG, 0) = 0
                  AND (@Active IS NULL OR ISNULL(ACTIVE, 0) = @Active)
                  AND
                  (
                    @Search IS NULL
                    OR GRPNAME LIKE '%' + @Search + '%'
                    OR SECNAME LIKE '%' + @Search + '%'
                    OR CONVERT(VARCHAR(20), GRPID) LIKE '%' + @Search + '%'
                  )";

            var parameters = new
            {
                Search = string.IsNullOrWhiteSpace(search)
                    ? null
                    : search.Trim(),
                Active = active
            };

            // -----------------------------------------
            // TOTAL RECORDS
            // -----------------------------------------

            string countSql = $@"
                SELECT COUNT(1)
                FROM dbo.GDWGRP
                {whereSql};";

            int totalRecords =
                await _db.QueryFirstOrDefaultAsync<int>(
                    countSql,
                    parameters);

            // -----------------------------------------
            // SAFE SORTING
            // -----------------------------------------

            //string orderColumn = (sortBy ?? "").Trim().ToUpperInvariant();

            //orderColumn = orderColumn switch
            //{
            //    "GRPID" => "GRPID",
            //    "GRPNAME" => "GRPNAME",
            //    "ACTIVE" => "ACTIVE",
            //    "SECNAME" => "SECNAME",
            //    "LOGCD" => "LOGCD",
            //    "UTIME" => "UTIME",
            //    _ => "GRPID"
            //};

            //string direction =
            //    string.Equals(
            //        sortDirection,
            //        "ASC",
            //        StringComparison.OrdinalIgnoreCase)
            //        ? "ASC"
            //        : "DESC";

            // -----------------------------------------
            // PAGINATED DATA
            // -----------------------------------------

            string dataSql = $@"
                SELECT
                    GRPID AS Id,
                    GRPNAME AS GroupName,
                    ACTIVE AS IsActive,
                    SECNAME AS SecondName,
                    LOGCD AS LogCd,
                    UTIME AS UTime
                FROM dbo.GDWGRP
                {whereSql}
                ORDER BY GRPID DESC
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY;";

            var data = await _db.QueryListAsync<LocationGroupDto>(
                dataSql,
                new
                {
                    Search = parameters.Search,
                    Active = parameters.Active,
                    Offset = (page - 1) * pageSize,
                    PageSize = pageSize
                });

            int totalPages = totalRecords == 0
                ? 0
                : (int)Math.Ceiling(
                    totalRecords / (double)pageSize);

            return new PagedResponse<LocationGroupDto>
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
        // GET BY ID
        // =========================================================

        public async Task<LocationGroupDto?> GetByIdAsync(int groupId)
        {
            const string sql = @"
                SELECT
                    GRPID AS Id,
                    GRPNAME AS GroupName,
                    ACTIVE AS IsActive,
                    SECNAME AS SecondName,
                    LOGCD AS LogCd,
                    UTIME AS UTime
                FROM dbo.GDWGRP
                WHERE ISNULL(DELFLG, 0) = 0 AND GRPID = @GroupId;";

            return await _db.QueryFirstOrDefaultAsync<LocationGroupDto>(
                sql,
                new { GroupId = groupId });
        }

        // =========================================================
        // DELETE STATE (no ACTIVE and no DELFLG filter)
        // null  = no such group
        // true  = group exists and is already deleted (DELFLG = 1)
        // false = group exists and is not deleted (active or inactive)
        // =========================================================

        public async Task<bool?> GetDeleteStateAsync(int groupId)
        {
            const string sql = @"
                SELECT CAST(
                    CASE WHEN ISNULL(DELFLG, 0) = 1 THEN 1 ELSE 0 END
                    AS INT)
                FROM dbo.GDWGRP
                WHERE GRPID = @GroupId;";

            int? state = await _db.QueryFirstOrDefaultAsync<int?>(
                sql,
                new { GroupId = groupId });

            return state.HasValue ? state.Value == 1 : null;
        }

        // =========================================================
        // IN-USE CHECK
        // True when any non-deleted godown (GDWMAS) references the group.
        // =========================================================

        public async Task<bool> HasLocationsAsync(int groupId)
        {
            const string sql = @"
                SELECT CASE WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.GDWMAS
                    WHERE GRPID = @GroupId
                      AND ISNULL(DELFLG, 0) = 0
                )
                THEN 1 ELSE 0 END;";

            int result = await _db.QueryFirstOrDefaultAsync<int>(
                sql,
                new { GroupId = groupId });

            return result == 1;
        }

        // =========================================================
        // UPDATE
        // =========================================================

        public async Task<int> UpdateAsync(
            int groupId,
            LocationGroupMasterRequest request)
        {
            const string sql = @"
                UPDATE dbo.GDWGRP
                SET
                    GRPNAME = @GroupName,
                    ACTIVE = @IsActive,
                    SECNAME = @SecondName,
                    UTIME = GETDATE()
                WHERE GRPID = @GroupId;

                SELECT @@ROWCOUNT;";

            return await _db.QueryFirstOrDefaultAsync<int>(
                sql,
                new
                {
                    GroupId = groupId,
                    GroupName = request.GroupName.Trim(),
                    IsActive = request.IsActive,
                    SecondName = request.SecondName?.Trim() ?? string.Empty
                });
        }


        public async Task<bool> DeleteAsync(
           int groupId,
           int userId)
        {
            const string sql = @"
                UPDATE dbo.GDWGRP
                SET
                    DELFLG = 1,
                    ACTIVE = 0,
                    LOGCD = @UserId,
                    UTIME = GETDATE()
                WHERE GRPID = @GroupId
                  AND ISNULL(DELFLG, 0) = 0";

            var rowsAffected= await _db.ExecuteAsync(
                sql,
                new
                {
                    GroupId = groupId,
                    UserId = userId
                });
            return rowsAffected > 0;

        }

        // =========================================================
        // DUPLICATE CHECK - CREATE
        // =========================================================

        public async Task<bool> NameExistsAsync(string groupName)
        {
            const string sql = @"
                SELECT CASE WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.GDWGRP
                    WHERE ACTIVE=1 AND GRPNAME = @GroupName
                )
                THEN 1 ELSE 0 END;";

            int result = await _db.QueryFirstOrDefaultAsync<int>(
                sql,
                new { GroupName = groupName.Trim() });

            return result == 1;
        }

        // =========================================================
        // DUPLICATE CHECK - UPDATE
        // =========================================================

        public async Task<bool> NameExistsAsync(
            string groupName,
            int groupId)
        {
            const string sql = @"
                SELECT CASE WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.GDWGRP
                    WHERE ACTIVE=1 AND GRPNAME = @GroupName
                    AND GRPID <> @GroupId
                )
                THEN 1 ELSE 0 END;";

            int result = await _db.QueryFirstOrDefaultAsync<int>(
                sql,
                new
                {
                    GroupName = groupName.Trim(),
                    GroupId = groupId
                });

            return result == 1;
        }

        // =========================================================
        // GET ACTIVE GROUPS
        // =========================================================

        public async Task<IReadOnlyList<LocationGroupDropdownDto>>
            GetActiveAsync()
        {
            const string sql = @"
                SELECT
                    GRPID AS Id,
                    GRPNAME AS GroupName
                FROM dbo.GDWGRP
                WHERE ACTIVE = 1
                ORDER BY GRPNAME;";

            var result =
                await _db.QueryListAsync<LocationGroupDropdownDto>(sql);

            return result.ToList();
        }

        // =========================================================
        // SEARCH
        // =========================================================

        public async Task<IReadOnlyList<LocationGroupDropdownDto>>
            SearchAsync(string? search)
        {
            const string sql = @"
                SELECT
                    GRPID AS Id,
                    GRPNAME AS GroupName
                FROM dbo.GDWGRP
                WHERE ACTIVE=1 AND
                    @Search IS NULL
                    OR GRPNAME LIKE '%' + @Search + '%'
                ORDER BY GRPNAME;";

            var result =
                await _db.QueryListAsync<LocationGroupDropdownDto>(
                    sql,
                    new
                    {
                        Search = string.IsNullOrWhiteSpace(search)
                            ? null
                            : search.Trim()
                    });

            return result.ToList();
        }

        // =========================================================
        // DROPDOWN
        // =========================================================

        public async Task<IReadOnlyList<LocationGroupDropdownDto>>
            GetDropdownAsync()
        {
            const string sql = @"
                SELECT
                    GRPID AS Id,
                    GRPNAME AS GroupName
                FROM dbo.GDWGRP
                WHERE ACTIVE = 1
                ORDER BY GRPNAME;";

            var result =
                await _db.QueryListAsync<LocationGroupDropdownDto>(sql);

            return result.ToList();
        }
    }
}