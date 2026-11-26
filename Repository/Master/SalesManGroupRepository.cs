using QuickBalance.API.DB;
using QuickBalance.API.DTOs.SalesMan;

namespace QuickBalance.API.Repository.Master
{
    public class SalesManGroupRepository
    {
        private readonly DBConn _db;

        public SalesManGroupRepository(DBConn db)
        {
            _db = db;
        }

        // =========================================================
        // INSERT
        // =========================================================
        public async Task<int> InsertAsync(
            string groupName,
            string printName,
            bool isActive,
            int userId,
            string secondName)
        {
            const string query = @"
                INSERT INTO SMGRP
                (
                    SMGRPNAME,
                    PRINTNAME,
                    ACTIVE,
                    LOGCD,
                    UTIME,
                    SECNAME
                )
                OUTPUT INSERTED.SMGRPCODE
                VALUES
                (
                    @GroupName,
                    @PrintName,
                    @IsActive,
                    @UserId,
                    GETDATE(),
                    @SecondName
                )";

            return await _db.QueryFirstOrDefaultAsync<int>(
                query,
                new
                {
                    GroupName = groupName,
                    PrintName = printName,
                    IsActive = isActive,
                    UserId = userId,
                    SecondName = secondName
                });
        }

        // =========================================================
        // UPDATE
        // =========================================================
        public async Task<int> UpdateAsync(
            int groupCode,
            string groupName,
            string printName,
            bool isActive,
            string secondName)
        {
            const string query = @"
                UPDATE SMGRP
                SET
                    SMGRPNAME = @GroupName,
                    PRINTNAME = @PrintName,
                    ACTIVE = @IsActive,
                    SECNAME = @SecondName
                WHERE SMGRPCODE = @GroupCode";

            return await _db.ExecuteAsync(
                query,
                new
                {
                    GroupCode = groupCode,
                    GroupName = groupName,
                    PrintName = printName,
                    IsActive = isActive,
                    SecondName = secondName
                });
        }

        // =========================================================
        // CHECK DUPLICATE NAME - CREATE
        // =========================================================
        public async Task<bool> GroupNameExistsAsync(
            string groupName)
        {
            const string query = @"
                SELECT CAST(
                    CASE WHEN EXISTS
                    (
                        SELECT 1
                        FROM SMGRP
                        WHERE SMGRPNAME = @GroupName
                    )
                    THEN 1 ELSE 0 END
                AS BIT)";

            return await _db.QueryFirstOrDefaultAsync<bool>(
                query,
                new { GroupName = groupName });
        }

        // =========================================================
        // CHECK DUPLICATE NAME - UPDATE
        // =========================================================
        public async Task<bool> GroupNameExistsAsync(
            string groupName,
            int excludeGroupCode)
        {
            const string query = @"
                SELECT CAST(
                    CASE WHEN EXISTS
                    (
                        SELECT 1
                        FROM SMGRP
                        WHERE SMGRPNAME = @GroupName
                          AND SMGRPCODE <> @GroupCode
                    )
                    THEN 1 ELSE 0 END
                AS BIT)";

            return await _db.QueryFirstOrDefaultAsync<bool>(
                query,
                new
                {
                    GroupName = groupName,
                    GroupCode = excludeGroupCode
                });
        }

        // =========================================================
        // GET ALL - PAGINATED
        // =========================================================
        public async Task<List<SalesmanGroupListItemDto>> GetAllAsync(int page, int pageSize, string? search, bool? active = null)
        {
            const string query = @"
        SELECT
            SMGRPCODE AS Id,
            SMGRPNAME AS GroupName,
            SECNAME AS SecondName,
            PRINTNAME AS PrintName,
            ACTIVE AS IsActive
        FROM SMGRP
        WHERE
            (@Active IS NULL OR ISNULL(ACTIVE, 0) = @Active)
            AND DELFLG = 0
            AND
            (
                @Search IS NULL
                OR @Search = ''
                OR SMGRPNAME LIKE '%' + @Search + '%'
                OR SECNAME LIKE '%' + @Search + '%'
                OR PRINTNAME LIKE '%' + @Search + '%'
            )
        ORDER BY SMGRPNAME
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY";

            return await _db.QueryListAsync<SalesmanGroupListItemDto>(
                query,
                new
                {
                    Search = search,
                    Active = active,
                    Offset = (page - 1) * pageSize,
                    PageSize = pageSize
                });
        }

        // =========================================================
        // GET TOTAL COUNT
        // =========================================================
        public async Task<int> GetTotalCountAsync(string? search, bool? active = null)
        {
            const string query = @"
        SELECT COUNT(1)
        FROM SMGRP
        WHERE
            (@Active IS NULL OR ISNULL(ACTIVE, 0) = @Active)
            AND DELFLG = 0
            AND
            (
                @Search IS NULL
                OR @Search = ''
                OR SMGRPNAME LIKE '%' + @Search + '%'
                OR SECNAME LIKE '%' + @Search + '%'
                OR PRINTNAME LIKE '%' + @Search + '%'
            )";

            return await _db.QueryFirstOrDefaultAsync<int>(
                query,
                new { Search = search, Active = active });
        }

        // =========================================================
        // GET BY ID
        // =========================================================
        public async Task<SalesmanGroupResponse?> GetByIdAsync(int groupCode)
        {
            const string query = @"
        SELECT
            SMGRPCODE AS Id,
            SMGRPNAME AS GroupName,
            SECNAME AS SecondName,
            PRINTNAME AS PrintName,
            ACTIVE AS IsActive
        FROM SMGRP
        WHERE
            SMGRPCODE = @GroupCode
            AND DELFLG = 0";

            return await _db.QueryFirstOrDefaultAsync<SalesmanGroupResponse>(
                query,
                new { GroupCode = groupCode });
        }

        // =========================================================
        // LOOKUP
        // =========================================================
        public async Task<List<SalesmanGroupLookupResponse>> GetLookupAsync()
        {
            const string query = @"
                SELECT
                    SMGRPCODE AS Id,
                    SMGRPNAME AS GroupName
                FROM SMGRP
                ORDER BY SMGRPNAME";

            return await _db.QueryListAsync<SalesmanGroupLookupResponse>(
                query);
        }

        // =========================================================
        // MAX ID
        // =========================================================
        public async Task<int> MaxIdAsync()
        {
            const string query = @"
                SELECT ISNULL(MAX(SMGRPCODE), 0) + 1
                FROM SMGRP";

            return await _db.QueryFirstOrDefaultAsync<int>(query);
        }

        // =========================================================
        // CHECK GROUP ASSIGNMENT
        // =========================================================
        public async Task<bool> HasSalesmenAsync(int groupCode)
        {
            const string query = @"
        SELECT CAST(
            CASE WHEN EXISTS
            (
                SELECT 1
                FROM SMMAST
                WHERE SMGRPCODE = @GroupCode
                  AND DELFLG = 0
            )
            THEN 1 ELSE 0 END
        AS BIT)";

            return await _db.QueryFirstOrDefaultAsync<bool>(
                query,
                new { GroupCode = groupCode });
        }
        // =========================================================
        // SOFT DELETE
        // =========================================================
        public async Task<int> SoftDeleteAsync(int groupCode)
        {
            const string query = @"
        UPDATE SMGRP
        SET DELFLG = 1
        WHERE SMGRPCODE = @GroupCode
          AND DELFLG = 0";

            return await _db.ExecuteAsync(
                query,
                new { GroupCode = groupCode });
        }
    }
}
