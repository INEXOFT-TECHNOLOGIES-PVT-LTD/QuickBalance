using QuickBalance.API.CLS;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.PartyGroup;
using QuickBalance.API.DTOs.Product;
using QuickBalance.API.DTOs.Respons_DTO;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace QuickBalance.API.Repository.Master
{
    public sealed class PartyGroupRepository
    {
        private readonly DBConn db;

        public PartyGroupRepository(DBConn db)
        {
            this.db = db;
        }

        // =========================================================
        // CHECK DUPLICATE GROUP NAME
        // =========================================================
        public async Task<bool> ExistsAsync(
            string groupName,
            string groupType,
            int? excludeId = null)
        {
            const string sql = @"
                SELECT TOP 1 1
                FROM PARTYGROUPMAS
                WHERE GROUPTYPE = @GroupType
                  AND GROUPNAME = @GroupName
                  AND ISNULL(ISDELETED, 0) = 0
                  AND (@ExcludeId IS NULL OR ID <> @ExcludeId);";

            var result = await db.QueryFirstOrDefaultAsync<int?>(
                sql,
                new
                {
                    GroupType = groupType,
                    GroupName = groupName,
                    ExcludeId = excludeId
                });

            return result.HasValue;
        }

        // =========================================================
        // CREATE PARTY GROUP
        // =========================================================
        public async Task<int> CreateAsync(
            string groupName,
            string? groupShortName,
            string groupType)
        {
            const string sql = @"
                INSERT INTO PARTYGROUPMAS
                (
                    GROUPNAME,
                    GROUPSNAME,
                    GROUPTYPE
                )
                VALUES
                (
                    @GroupName,
                    @GroupShortName,
                    @GroupType
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var result = await db.QueryFirstOrDefaultAsync<int>(
                sql,
                new
                {
                    GroupName = groupName,
                    GroupShortName = groupShortName,
                    GroupType = groupType
                });

            return result;
        }

        // =========================================================
        // UPDATE PARTY GROUP
        // =========================================================
        public async Task<bool> UpdateAsync(
            int id,
            string groupName,
            string? groupShortName,
            string groupType)
        {
            const string sql = @"
                UPDATE PARTYGROUPMAS
                SET
                    GROUPNAME = @GroupName,
                    GROUPSNAME = @GroupShortName
                WHERE ID = @Id
                  AND GROUPTYPE = @GroupType;";

            int rowsAffected = await db.ExecuteAsync(
                sql,
                new
                {
                    Id = id,
                    GroupName = groupName,
                    GroupShortName = groupShortName,
                    GroupType = groupType
                });

            return rowsAffected > 0;
        }

        // =========================================================
        // DELETE PARTY GROUP
        // =========================================================
        public async Task<bool> DeleteAsync(
            int id,
            int userId)
        {
            const string sql = @"
                UPDATE PARTYGROUPMAS
                SET ISDELETED = 1,
                    LOGCD = @UserId,
                    UTIME = GETDATE()
                WHERE ID = @Id
                  AND ISNULL(ISDELETED, 0) = 0;";

            int rowsAffected = await db.ExecuteAsync(
                sql,
                new
                {
                    Id = id,
                    UserId = userId
                });

            return rowsAffected > 0;
        }

        // =========================================================
        // GET PARTY GROUP BY ID
        // =========================================================
        public async Task<PartyGroupResponse?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT
                    ID AS Id,
                    ISNULL(GROUPNAME, '') AS GroupName,
                    GROUPSNAME AS GroupShortName,
                    CASE GROUPTYPE WHEN 'C' THEN 'Customer' ELSE 'Supplier' END AS GroupType
                FROM PARTYGROUPMAS
                WHERE ID = @Id
                  AND ISNULL(ISDELETED, 0) = 0;";

            return await db.QueryFirstOrDefaultAsync<PartyGroupResponse>(
                sql,
                new { Id = id });
        }

        // =========================================================
        // GET PARTY GROUPS
        // =========================================================
        public async Task<List<LookupItem>> GetAllAsync(
            string groupType)
        {
            const string sql = @"
                SELECT
                    ID AS Id,
                    ISNULL(GROUPNAME, '') AS Name
                FROM PARTYGROUPMAS
                WHERE GROUPTYPE = @GroupType
                  AND ISNULL(ISDELETED, 0) = 0
                ORDER BY GROUPNAME;";

            return await db.QueryListAsync<LookupItem>(
                sql,
                new
                {
                    GroupType = groupType
                });
        }


        public async Task<PagedResponse<PartyGroupListItem>> GetAllAsync(string groupType,int page,int pageSize,string? search)
        {
            if (string.IsNullOrWhiteSpace(groupType))
                throw new ArgumentException("Group type is required.");

            if (page < 1)
                throw new ArgumentException("Page must be greater than 0.");

            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100.");

            const string countSql = @"
            SELECT COUNT(*)
            FROM PARTYGROUPMAS
            WHERE GROUPTYPE = @GroupType
              AND ISNULL(ISDELETED, 0) = 0
              AND (
                    @Search IS NULL
                    OR GROUPNAME LIKE '%' + @Search + '%'
                  );";

                const string sql = @"
            SELECT
                ID AS Id,
                ISNULL(GROUPNAME, '') AS Name,
                ISNULL(GROUPNAME, '') AS GroupName,
                GROUPSNAME AS GroupShortName,
                CASE GROUPTYPE WHEN 'C' THEN 'Customer' ELSE 'Supplier' END AS GroupType
            FROM PARTYGROUPMAS
            WHERE GROUPTYPE = @GroupType
              AND ISNULL(ISDELETED, 0) = 0
              AND (
                    @Search IS NULL
                    OR GROUPNAME LIKE '%' + @Search + '%'
                  )
            ORDER BY GROUPNAME, ID
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;";

            var parameters = new
            {
                GroupType = groupType.Trim(),
                Search = string.IsNullOrWhiteSpace(search)
                    ? null
                    : search.Trim(),
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            };

            int totalRecords = await db.QuerySingleOrDefaultAsync<int>(
                countSql,
                parameters);

            var data = await db.QueryListAsync<PartyGroupListItem>(
                sql,
                parameters);

            return new PagedResponse<PartyGroupListItem>
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


        //var data = await db.QueryListAsync<LookupItem>(
        //sql,
        //parameters);

        //return (data, totalRecords);
    }
    }
