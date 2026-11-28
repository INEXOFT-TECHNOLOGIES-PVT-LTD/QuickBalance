
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs.Respons_DTO;
using System.Data;

namespace QuickBalance.API.DB
{
    public class LedgerGroupDB
    {
        private readonly DBConn db;

        public LedgerGroupDB(DBConn dbConn)
        {
            db = dbConn;
        }

        // ============================================================
        // INSERT
        // ============================================================
        public async Task<int> Insert(ClsGrpLdg group, int userId)
        {
            const string query = @"
                INSERT INTO GRPLDG
                (
                    GROUPNAME,
                    GROUPCODE,
                    GROUPNATURE,
                    SUBLEDGFLAG,
                    MGROUP,
                    MAINGROUP,
                    LOGCD,
                    UTIME,
                    FORDER,
                    SECNAME,
                    SEQNO,
                    CRDR,
                    GACTIVE,
                    APRVLST,
                    APRVLRBY,
                    APRVLRDT,
                    GDELFLG
                )
                OUTPUT INSERTED.GROUPID
                VALUES
                (
                    @GroupName,
                    @GroupCode,
                    @GroupNature,
                    @SubLedgerFlag,
                    @MainGroupId,
                    @MainGroup,
                    @UserId,
                    GETDATE(),
                    @GroupNature,
                    @SecondName,
                    @SequenceNo,
                    @CrDr,
                    @IsActive,
                    N'',
                    @UserId,
                    GETDATE(),
                    0
                );";

            return await db.QuerySingleOrDefaultAsync<int>(
                query,
                new
                {
                    GroupName = group.GroupName,
                    GroupCode = group.GroupCode,
                    GroupNature = group.GroupNature,
                    SubLedgerFlag = group.SubLedgeFlag,
                    MainGroupId = group.MGroup,
                    MainGroup = group.Maingroup,
                    UserId = userId,
                    SecondName = group.SecName ?? "",
                    SequenceNo = group.SeqNo,
                    CrDr = group.CRDR,
                    IsActive = group.Gactive
                });
        }

        // ============================================================
        // UPDATE
        // ============================================================
        public async Task<int> Update(ClsGrpLdg group, int userId)
        {
            const string query = @"
                UPDATE GRPLDG
                SET
                    GROUPNAME = @GroupName,
                    GROUPCODE = @GroupCode,
                    GROUPNATURE = @GroupNature,
                    SUBLEDGFLAG = @SubLedgerFlag,
                    MGROUP = @MainGroupId,
                    MAINGROUP = @MainGroup,
                    LOGCD = @UserId,
                    UTIME = GETDATE(),
                    FORDER = @GroupNature,
                    SECNAME = @SecondName,
                    SEQNO = @SequenceNo,
                    CRDR = @CrDr,
                    GACTIVE = @IsActive
                WHERE GROUPID = @GroupId;";

            return await db.ExecuteAsync(
                query,
                new
                {
                    GroupId = group.GroupId,
                    GroupName = group.GroupName,
                    GroupCode = group.GroupCode,
                    GroupNature = group.GroupNature,
                    SubLedgerFlag = group.SubLedgeFlag,
                    MainGroupId = group.MGroup,
                    MainGroup = group.Maingroup,
                    UserId = userId,
                    SecondName = group.SecName ?? "",
                    SequenceNo = group.SeqNo,
                    CrDr = group.CRDR,
                    IsActive = group.Gactive
                });
        }

        // ============================================================
        // GET BY ID
        // ============================================================
        public async Task<DataTable> SearchByID(int groupId)
        {
            string query = $@"
                SELECT *
                FROM GRPLDG
                WHERE GACTIVE=1  AND GROUPID = {groupId}";

            return await db.GetDatatable(query);
        }


        public async Task<bool> DeleteAsync(int id, int userId)
        {
            const string sql = @"
                UPDATE dbo.GRPLDG
                SET
                    GDELFLG = 1,
                    GACTIVE = 0,
                    LOGCD = @UserId,
                    UTIME = GETDATE()
                WHERE GROUPID = @Id
                  AND ISNULL(GDELFLG, 0) = 0;";

            var rowsAffected = await db.ExecuteAsync(
                sql,
                new
                {
                    Id = id,
                    UserId = userId
                });
            return rowsAffected > 0;

        }

        // ============================================================
        // DELETE PRE-CHECKS
        // ============================================================
        public async Task<bool> ExistsForDeleteAsync(int id)
        {
            const string sql = @"
                SELECT COUNT(*)
                FROM dbo.GRPLDG
                WHERE GROUPID = @Id
                  AND ISNULL(GDELFLG, 0) = 0;";

            return await db.QuerySingleOrDefaultAsync<int>(
                sql, new { Id = id }) > 0;
        }

        public async Task<bool> HasSubGroupsAsync(int id)
        {
            const string sql = @"
                SELECT COUNT(*)
                FROM dbo.GRPLDG
                WHERE MGROUP = @Id
                  AND GROUPID <> @Id
                  AND ISNULL(GDELFLG, 0) = 0;";

            return await db.QuerySingleOrDefaultAsync<int>(
                sql, new { Id = id }) > 0;
        }

        public async Task<bool> HasAccountsAsync(int id)
        {
            const string sql = @"
                SELECT COUNT(*)
                FROM dbo.ACCMAS
                WHERE MGROUP = @Id
                  AND ISNULL(DELFLG, 0) = 0;";

            return await db.QuerySingleOrDefaultAsync<int>(
                sql, new { Id = id }) > 0;
        }
        public async Task<LedgerGroupDto?> SearchByIDs(int groupId)
        {
            const string query = @"
                SELECT
                    GROUPID AS Id,
                    GROUPNAME AS GroupName,
                    SECNAME AS SecondName,
                    GROUPCODE AS GroupCode,
                    GROUPNATURE AS FinalAccount ,
                    SUBLEDGFLAG AS SubLedgerFlag,
                    MAINGROUP AS MainGroup,
                    MGROUP AS MainGroupId,
                    SEQNO AS SequenceNo,
                    CRDR AS CRDR,
                    GACTIVE AS IsActive ,
                    GDELFLG AS GDELFLG
                FROM GRPLDG
                WHERE ISNULL(GDELFLG, 0) = 0 AND GROUPID = @GroupId;";
            //Gactive
            //GroupNature
            return await db.QueryFirstOrDefaultAsync<LedgerGroupDto>(
                query,
                new { GroupId = groupId });
        }

        // ============================================================
        // GET BY ID - DTO
        // ============================================================
        //public async Task<LedgerGroupDto?> SearchByIDs(int groupId)
        //{
        //    const string query = @"
        //        SELECT
        //            GROUPID AS GroupId,
        //            GROUPNAME AS GroupName,
        //            SECNAME AS SecName,
        //            GROUPCODE AS GroupCode,
        //            GROUPNATURE AS GroupNature,
        //            SUBLEDGFLAG AS SubLedgerFlag,
        //            MAINGROUP AS MainGroup,
        //            MGROUP AS MainGroupId,
        //            SEQNO AS SeqNo,
        //            CRDR AS CRDR,
        //            GACTIVE AS Gactive,
        //            GDELFLG AS GDELFLG
        //        FROM GRPLDG
        //        WHERE GROUPID = @GroupId;";

        //    return await db.QueryFirstOrDefaultAsync<LedgerGroupDto>(
        //        query,
        //        new { GroupId = groupId });
        //}

        // ============================================================
        // GET ALL - PAGINATED
        // ============================================================
        public async Task<PaginatedResult<LedgerGroupDto>> GetAll(
            string? search = null,
            int page = 1,
            int pageSize = 50,
            bool? active = null)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 500);

            int offset = checked((page - 1) * pageSize);

            search = string.IsNullOrWhiteSpace(search)
                ? null
                : search.Trim();

            const string whereClause = @"
                WHERE ISNULL(GDELFLG, 0) = 0
                  AND (@Active IS NULL OR ISNULL(GACTIVE, 0) = @Active)
                  AND (
                        @Search IS NULL
                     OR GROUPNAME LIKE N'%' + @Search + N'%'
                     OR SECNAME LIKE N'%' + @Search + N'%'
                     OR CAST(GROUPCODE AS NVARCHAR(50))
                        LIKE N'%' + @Search + N'%'
                  )";

            string countQuery = $@"
                SELECT COUNT_BIG(*)
                FROM GRPLDG
                {whereClause};";

            string dataQuery = $@"
                SELECT
                    GROUPID AS Id,
                    GROUPCODE AS GroupCode,
                    GROUPNAME AS GroupName,
                    SECNAME AS SecondName,
                    GROUPNATURE AS FinalAccount,
                    SUBLEDGFLAG AS SubLedgerFlag,
                    MAINGROUP AS MainGroup,
                    MGROUP AS MainGroupId,
                    SEQNO AS SequenceNo,
                    CRDR AS CRDR,
                    GACTIVE AS IsActive,
                    GDELFLG AS GDELFLG
                FROM GRPLDG
                {whereClause}
                ORDER BY GROUPNAME, GROUPCODE
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY;";

            long totalRecords =
                await db.QuerySingleOrDefaultAsync<long>(
                    countQuery,
                    new { Search = search, Active = active });

            var data = await db.QueryListAsync<LedgerGroupDto>(
                dataQuery,
                new
                {
                    Search = search,
                    Active = active,
                    Offset = offset,
                    PageSize = pageSize
                });

            return new PaginatedResult<LedgerGroupDto>
            {
                Data = data,
                TotalRecords = totalRecords
            };
        }

        // ============================================================
        // ACTIVE GROUP LOOKUP
        // ============================================================
        public async Task<List<LedgerGroupDto>> NameAndId()
        {
            const string query = @"
                SELECT
                    GROUPID AS Id,
                    GROUPNAME AS GroupName
                FROM GRPLDG
                WHERE GACTIVE = 1
                  AND GDELFLG = 0
                ORDER BY GROUPNAME;";

            return await db.QueryListAsync<LedgerGroupDto>(query);
        }

        // ============================================================
        // ACTIVE GROUP SEARCH
        // ============================================================
        public async Task<List<LedgerGroupDto>> Search()
        {
            const string query = @"
                SELECT
                    GROUPID AS Id,
                    GROUPNAME AS GroupName
                FROM GRPLDG
                WHERE GACTIVE = 1
                  AND GDELFLG = 0
                ORDER BY GROUPNAME;";

            return await db.QueryListAsync<LedgerGroupDto>(query);
        }

        // ============================================================
        // MAX GROUP ID
        // ============================================================
        public async Task<int> MaxId()
        {
            return await db.GetnewId("GROUPID", "GRPLDG");
        }

        // ============================================================
        // MAX SEQUENCE NUMBER
        // ============================================================
        public async Task<int> MaxSeqNo()
        {
            const string query = @"
                SELECT ISNULL(MAX(TRY_CAST(SEQNO AS INT)), 0) + 1
                FROM GRPLDG;";
            return await db.QuerySingleOrDefaultAsync<int>(query);
        }

        // ============================================================
        // MAX GROUP CODE
        // ============================================================
        public async Task<int> MaxGroupCode()
        {
            const string query = @"
                SELECT ISNULL(MAX(GROUPCODE), 0) + 1
                FROM GRPLDG;";

            return await db.QuerySingleOrDefaultAsync<int>(query);
        }

        // ============================================================
        // CHECK NAME - CREATE
        // ============================================================
        public async Task<List<LedgerGroupDto>> GetAllByName(string groupName)
        {
            const string query = @"
                SELECT GROUPID AS Id,
                       GROUPNAME AS GroupName
                FROM GRPLDG
                WHERE   GACTIVE = 1  AND GDELFLG = 0 and GROUPNAME = @GroupName;";

            return await db.QueryListAsync<LedgerGroupDto>(
                query,
                new { GroupName = groupName.Trim() });
        }

        // ============================================================
        // CHECK NAME - UPDATE
        // ============================================================
        public async Task<List<LedgerGroupDto>> CheckNameExists(
            string groupName,
            int groupId)
        {
            const string query = @"
                SELECT GROUPID AS Id,
                       GROUPNAME AS GroupName
                FROM GRPLDG
                WHERE GROUPNAME = @GroupName AND GACTIVE = 1  AND GDELFLG = 0
                  AND GROUPID <> @GroupId;";

            return await db.QueryListAsync<LedgerGroupDto>(
                query,
                new
                {
                    GroupName = groupName.Trim(),
                    GroupId = groupId
                });
        }

        // ============================================================
        // CHECK GROUP CODE - CREATE
        // ============================================================
        public async Task<List<LedgerGroupDto>> GetAllByGroupCode(
            int groupCode)
        {
            const string query = @"
                SELECT GROUPID AS Id,
                       GROUPCODE AS GroupCode
                FROM GRPLDG
                WHERE   GDELFLG = 0 and GACTIVE = 1 and GROUPCODE = @GroupCode;";

            return await db.QueryListAsync<LedgerGroupDto>(
                query,
                new { GroupCode = groupCode });
        }

        // ============================================================
        // CHECK GROUP CODE - FREE-CODE LOOP (create)
        // All non-deleted groups, active or inactive.
        // ============================================================
        public async Task<List<LedgerGroupDto>> GetAllNonDeletedByGroupCode(
            int groupCode)
        {
            const string query = @"
                SELECT GROUPID AS Id,
                       GROUPCODE AS GroupCode
                FROM GRPLDG
                WHERE GDELFLG = 0 AND GROUPCODE = @GroupCode;";

            return await db.QueryListAsync<LedgerGroupDto>(
                query,
                new { GroupCode = groupCode });
        }

        // ============================================================
        // CHECK GROUP CODE - UPDATE
        // ============================================================
        public async Task<List<LedgerGroupDto>> CheckGroupExists(
            int groupCode,
            int groupId)
        {
            const string query = @"
                SELECT GROUPID AS Id,
                       GROUPCODE AS GroupCode
                FROM GRPLDG
                WHERE   GDELFLG = 0 AND GACTIVE = 1 AND GROUPCODE = @GroupCode
                  AND GROUPID <> @GroupId;";

            return await db.QueryListAsync<LedgerGroupDto>(
                query,
                new
                {
                    GroupCode = groupCode,
                    GroupId = groupId
                });
        }

        // ============================================================
        // CHECK SEQUENCE - CREATE
        // ============================================================
        public async Task<List<LedgerGroupDto>> GetAllBySeqNo(int seqNo)
        {
            const string query = @"
                SELECT GROUPID AS Id,
                       SEQNO AS SequenceNo
                FROM GRPLDG
                WHERE SEQNO = @SeqNo;";

            return await db.QueryListAsync<LedgerGroupDto>(
                query,
                new { SeqNo = seqNo });
        }

        // ============================================================
        // CHECK SEQUENCE - UPDATE
        // ============================================================
        public async Task<List<LedgerGroupDto>> CheckSeqNoExists(
            int seqNo,
            int groupId)
        {
            const string query = @"
                SELECT GROUPID AS Id,
                       SEQNO AS SequenceNo
                FROM GRPLDG
                WHERE   GDELFLG = 0 AND GACTIVE = 1 and SEQNO = @SeqNo
                  AND GROUPID <> @GroupId;";

            return await db.QueryListAsync<LedgerGroupDto>(
                query,
                new
                {
                    SeqNo = seqNo,
                    GroupId = groupId
                });
        }

        // ============================================================
        // GET SUB GROUPS
        // ============================================================
        public async Task<List<LedgerGroupDto>> GetSubGroups(
            int mainGroupId)
        {
            const string query = @"
                SELECT
                    GROUPID AS Id,
                    GROUPNAME AS GroupName,
                    GROUPCODE AS GroupCode,
                    MGROUP AS MainGroupId
                FROM GRPLDG
                WHERE   GDELFLG = 0 AND GACTIVE = 1 AND MGROUP = @MainGroupId
                ORDER BY GROUPNAME;";

            return await db.QueryListAsync<LedgerGroupDto>(
                query,
                new { MainGroupId = mainGroupId });
        }

        // ============================================================
        // GET SUB GROUPS ORDERED BY GROUP CODE
        // ============================================================
        public async Task<List<LedgerGroupDto>> GetSubGroupsOrderByGrpCd(
            int mainGroupId)
        {
            const string query = @"
                SELECT
                    GROUPID AS Id,
                    GROUPNAME AS GroupName,
                    GROUPCODE AS GroupCode,
                    MGROUP AS MainGroupId
                FROM GRPLDG
                WHERE   GDELFLG = 0 AND GACTIVE = 1 AND MGROUP = @MainGroupId
                ORDER BY GROUPCODE DESC;";

            return await db.QueryListAsync<LedgerGroupDto>(
                query,
                new { MainGroupId = mainGroupId });
        }

        // ============================================================
        // GET MAX CHILD GROUP CODE
        // ============================================================
        public async Task<int> GetMaxCode(int mainGroupId)
        {
            const string query = @"
                SELECT ISNULL(MAX(GROUPCODE), 0)
                FROM GRPLDG
                WHERE MGROUP = @MainGroupId;";

            return await db.QuerySingleOrDefaultAsync<int>(
                query,
                new { MainGroupId = mainGroupId });
        }

        // ============================================================
        // GET GROUPS FOR LIST
        // ============================================================
        public async Task<List<LedgerGroupDto>> GetForList(
            string? search = null)
        {
            search = string.IsNullOrWhiteSpace(search)
                ? null
                : search.Trim();

            const string query = @"
                SELECT
                    GROUPID AS Id,
                    GROUPNAME AS GroupName,
                    SECNAME AS SecName,
                    GROUPCODE AS GroupCode,
                    GROUPNATURE AS GroupNature
                FROM GRPLDG
                WHERE GDELFLG = 0
                  AND
                  (
                      @Search IS NULL
                      OR GROUPNAME LIKE N'%' + @Search + N'%'
                      OR SECNAME LIKE N'%' + @Search + N'%'
                      OR CAST(GROUPCODE AS NVARCHAR(50))
                         LIKE N'%' + @Search + N'%'
                  )
                ORDER BY GROUPNAME;";

            return await db.QueryListAsync<LedgerGroupDto>(
                query,
                new { Search = search });
        }

        // ============================================================
        // CUSTOMER / SUPPLIER LEDGER GROUPS
        // C = Customer, S = Supplier
        // ============================================================
        public async Task<List<LedgerGroupDto>> CustOrSupLedgerGroup(string type)
        {
            int groupId;

            if (string.Equals(type, "C", StringComparison.OrdinalIgnoreCase))
            {
                groupId = 26;
            }
            else if (string.Equals(
                type, "S", StringComparison.OrdinalIgnoreCase))
            {
                groupId = 27;
            }
            else
            {
                return new List<LedgerGroupDto>();
            }

            const string query = @"
                SELECT
                    GROUPID AS Id,
                    GROUPNAME AS GroupName,
                    GROUPCODE AS GroupCode,
                    GROUPNATURE AS GroupNature,
                    MGROUP AS MainGroupId
                FROM GRPLDG
                WHERE GACTIVE = 1
                  AND GDELFLG = 0
                  AND (GROUPID = @GroupId OR MGROUP = @GroupId)
                ORDER BY GROUPNAME;";

            return await db.QueryListAsync<LedgerGroupDto>(
                query,
                new { GroupId = groupId });
        }


        // ============================================================
        // FINAL ACCOUNT LOOKUP
        // ============================================================
        public List<FinalAccountDto> GetFinalAccountsLookup()
        {
            return new List<FinalAccountDto>
            {
                new() { Code = "P", FinalAccount = "Profit & Loss A/c" },
                new() { Code = "T", FinalAccount = "Trading A/c" },
                new() { Code = "B", FinalAccount = "Balance Sheet" },
                new() { Code = "M", FinalAccount = "Manufacturing A/c" }
            };
        }
    }
}




//using Microsoft.Data.SqlClient;
//using QuickBalance.API.CLS;
//using QuickBalance.API.DTOs.Respons_DTO;
//using System.Data;

//namespace QuickBalance.API.DB
//{
//    public class LedgerGroupDB
//    {
//        private readonly DBConn db;

//        public LedgerGroupDB(DBConn dbConn)
//        {
//            db = dbConn;
//        }

//        // ============================================================
//        // INSERT
//        // ============================================================
//        public async Task<int> Insert(
//            ClsGrpLdg group,
//            int userId)
//        {
//            string groupName = Escape(group.GroupName);
//            string secondName = Escape(group.SecName);
//            string groupNature = Escape(group.GroupNature);
//            string crDr = Escape(group.CRDR);

//            string query = $@"
//                INSERT INTO GRPLDG
//                (
//                    GROUPNAME,
//                    GROUPCODE,
//                    GROUPNATURE,
//                    SUBLEDGFLAG,
//                    MGROUP,
//                    MAINGROUP,
//                    LOGCD,
//                    UTIME,
//                    FORDER,
//                    SECNAME,
//                    SEQNO,
//                    CRDR,
//                    GACTIVE,
//                    APRVLST,
//                    APRVLRBY,
//                    APRVLRDT,
//                    GDELFLG
//                )
//                OUTPUT INSERTED.GROUPID
//                VALUES
//                (
//                    N'{groupName}',
//                    {group.GroupCode},
//                    N'{groupNature}',
//                    {(group.SubLedgeFlag ? 1 : 0)},
//                    {group.MGroup},
//                    {(group.Maingroup ? 1 : 0)},
//                    {userId},
//                    GETDATE(),
//                    N'{groupNature}',
//                    N'{secondName}',
//                    {group.SeqNo},
//                    N'{crDr}',
//                    {(group.Gactive ? 1 : 0)},
//                    N'',
//                    {userId},
//                    GETDATE(),
//                    0
//                )";

//            return await db.InsertScalar(query);
//        }

//        // ============================================================
//        // UPDATE
//        // ============================================================
//        public async Task<int> Update(
//            ClsGrpLdg group,
//            int userId)
//        {
//            string groupName = Escape(group.GroupName);
//            string secondName = Escape(group.SecName);
//            string groupNature = Escape(group.GroupNature);
//            string crDr = Escape(group.CRDR);

//            string query = $@"
//                UPDATE GRPLDG SET
//                    GROUPNAME = N'{groupName}',
//                    GROUPCODE = {group.GroupCode},
//                    GROUPNATURE = N'{groupNature}',
//                    SUBLEDGFLAG = {(group.SubLedgeFlag ? 1 : 0)},
//                    MGROUP = {group.MGroup},
//                    MAINGROUP = {(group.Maingroup ? 1 : 0)},
//                    LOGCD = {userId},
//                    UTIME = GETDATE(),
//                    FORDER = N'{groupNature}',
//                    SECNAME = N'{secondName}',
//                    SEQNO = {group.SeqNo},
//                    CRDR = N'{crDr}',
//                    GACTIVE = {(group.Gactive ? 1 : 0)}
//                WHERE GROUPID = {group.GroupId}";

//            return await db.Execute(query);
//        }

//        // ============================================================
//        // GET BY ID
//        // ============================================================
//        public async Task<DataTable> SearchByID(int groupId)
//        {
//            string query = $@"
//                SELECT *
//                FROM GRPLDG
//                WHERE GROUPID = {groupId}";

//            return await db.GetDatatable(query);
//        }


//        public async Task<LedgerGroupDto?> SearchByIDs(int groupId)
//        {
//            const string query = @"
//            SELECT
//                GROUPID     AS GroupId,
//                GROUPNAME   AS GroupName,
//                SECNAME     AS SecondName,
//                GROUPCODE   AS GroupCode,
//                GROUPNATURE AS FinalAccount,
//                SUBLEDGFLAG AS SubLedgerFlag,
//                MAINGROUP   AS MainGroup,
//                MGROUP      AS MainGroupId,
//                SEQNO       AS SequenceNo,
//                CRDR        AS CrDr,
//                GACTIVE     AS IsActive,
//                APRVLST     AS ApprovalStatus,
//                GDELFLG     AS GDELFLG
//            FROM GRPLDG
//            WHERE GROUPID = @GroupId;";

//            return await db.QueryFirstOrDefaultAsync<LedgerGroupDto>(
//                query,
//                new { GroupId = groupId }
//            );
//        }

//        // ============================================================
//        // GET ALL
//        // Search applies to:
//        // GROUPNAME
//        // SECNAME
//        // GROUPCODE
//        // ============================================================
//        //public async Task<DataTable> GetAll(string? search = null)
//        //{
//        //    string searchCondition = "";

//        //    if (!string.IsNullOrWhiteSpace(search))
//        //    {
//        //        string value = Escape(search);

//        //        searchCondition = $@"
//        //            WHERE
//        //                GROUPNAME LIKE N'%{value}%'
//        //                OR SECNAME LIKE N'%{value}%'
//        //                OR CAST(GROUPCODE AS NVARCHAR(50))
//        //                    LIKE N'%{value}%'";
//        //    }

//        //    string query = $@"
//        //        SELECT *
//        //        FROM GRPLDG
//        //        {searchCondition}
//        //        ORDER BY GROUPNAME";

//        //    return await db.GetDatatable(query);
//        //}

//        public async Task<PaginatedResult<LedgerGroupDto>> GetAll(string? search = null,int page = 1,int pageSize = 50)
//        {
//            page = Math.Max(1, page);
//            pageSize = Math.Clamp(pageSize, 1, 500);

//            int offset = checked((page - 1) * pageSize);

//            search = string.IsNullOrWhiteSpace(search)
//                ? null
//                : search.Trim();

//            const string whereClause = @"
//            WHERE
//                (@Search IS NULL
//                 OR GROUPNAME LIKE N'%' + @Search + N'%'
//                 OR SECNAME LIKE N'%' + @Search + N'%'
//                 OR CAST(GROUPCODE AS NVARCHAR(50))
//                    LIKE N'%' + @Search + N'%')";

//            string countQuery = $@"
//            SELECT COUNT_BIG(*)
//            FROM GRPLDG
//            {whereClause};";

//            string dataQuery = $@"
//            SELECT
//                GROUPCODE AS GroupCode,
//                GROUPNAME AS GroupName,
//                SECNAME AS SecName
//            FROM GRPLDG
//            {whereClause}
//            ORDER BY GROUPNAME, GROUPCODE
//            OFFSET @Offset ROWS
//            FETCH NEXT @PageSize ROWS ONLY;";

//            var parameters = new { Search = search };

//            long totalRecords = await db.QuerySingleOrDefaultAsync<long>(
//                countQuery,
//                parameters);

//            var data = await db.QueryListAsync<LedgerGroupDto>(
//                dataQuery,
//                new
//                {
//                    Search = search,
//                    Offset = offset,
//                    PageSize = pageSize
//                });

//            return new PaginatedResult<LedgerGroupDto>
//            {
//                Data = data,
//                TotalRecords = totalRecords
//            };
//        }

//        // ============================================================
//        // ACTIVE GROUP LOOKUP
//        // ============================================================
//        public async Task<DataTable> NameAndId()
//        {
//            string query = @"
//                SELECT
//                    GROUPID,
//                    GROUPNAME
//                FROM GRPLDG
//                WHERE GACTIVE = 1
//                  AND GDELFLG = 0
//                ORDER BY GROUPNAME";

//            return await db.GetDatatable(query);
//        }

//        // ============================================================
//        // SIMPLE ACTIVE SEARCH
//        // ============================================================
//        public async Task<DataTable> Search()
//        {
//            string query = @"
//                SELECT
//                    GROUPID,
//                    GROUPNAME
//                FROM GRPLDG
//                WHERE GACTIVE = 1
//                  AND GDELFLG = 0
//                ORDER BY GROUPNAME";

//            return await db.GetDatatable(query);
//        }

//        // ============================================================
//        // MAX GROUP ID
//        // ============================================================
//        public async Task<int> MaxId()
//        {
//            return await db.GetnewId(
//                "GROUPID",
//                "GRPLDG");
//        }

//        // ============================================================
//        // MAX SEQUENCE NUMBER
//        // ============================================================
//        public async Task<int> MaxSeqNo()
//        {
//            return await db.GetnewId(
//                "SEQNO",
//                "GRPLDG");
//        }

//        // ============================================================
//        // MAX GROUP CODE
//        // ============================================================
//        public async Task<int> MaxGroupCode()
//        {
//            string query = @"
//                SELECT ISNULL(MAX(GROUPCODE), 0) + 1
//                FROM GRPLDG";

//            return await db.ReturnIntValue(query);
//        }

//        // ============================================================
//        // CHECK NAME - CREATE
//        // ============================================================
//        public async Task<DataTable> GetAllByName(
//            string groupName)
//        {
//            string value = Escape(groupName);

//            string query = $@"
//                SELECT *
//                FROM GRPLDG
//                WHERE GROUPNAME = N'{value}'";

//            return await db.GetDatatable(query);
//        }

//        // ============================================================
//        // CHECK NAME - UPDATE
//        // ============================================================
//        public async Task<DataTable> CheckNameExists(
//            string groupName,
//            int groupId)
//        {
//            string value = Escape(groupName);

//            string query = $@"
//                SELECT *
//                FROM GRPLDG
//                WHERE GROUPNAME = N'{value}'
//                  AND GROUPID <> {groupId}";

//            return await db.GetDatatable(query);
//        }

//        // ============================================================
//        // CHECK GROUP CODE - CREATE
//        // ============================================================
//        public async Task<DataTable> GetAllByGroupCode(
//            int groupCode)
//        {
//            string query = $@"
//                SELECT *
//                FROM GRPLDG
//                WHERE GROUPCODE = {groupCode}";

//            return await db.GetDatatable(query);
//        }

//        // ============================================================
//        // CHECK GROUP CODE - UPDATE
//        // ============================================================
//        public async Task<DataTable> CheckGroupExists(
//            int groupCode,
//            int groupId)
//        {
//            string query = $@"
//                SELECT *
//                FROM GRPLDG
//                WHERE GROUPCODE = {groupCode}
//                  AND GROUPID <> {groupId}";

//            return await db.GetDatatable(query);
//        }

//        // ============================================================
//        // CHECK SEQUENCE - CREATE
//        // ============================================================
//        public async Task<DataTable> GetAllBySeqNo(
//            int seqNo)
//        {
//            string query = $@"
//                SELECT *
//                FROM GRPLDG
//                WHERE SEQNO = {seqNo}";

//            return await db.GetDatatable(query);
//        }

//        // ============================================================
//        // CHECK SEQUENCE - UPDATE
//        // ============================================================
//        public async Task<DataTable> CheckSeqNoExists(
//            int seqNo,
//            int groupId)
//        {
//            string query = $@"
//                SELECT *
//                FROM GRPLDG
//                WHERE SEQNO = {seqNo}
//                  AND GROUPID <> {groupId}";

//            return await db.GetDatatable(query);
//        }

//        // ============================================================
//        // GET SUB GROUPS
//        // ============================================================
//        public async Task<DataTable> GetSubGroups(
//            int mainGroupId)
//        {
//            string query = $@"
//                SELECT *
//                FROM GRPLDG
//                WHERE MGROUP = {mainGroupId}
//                ORDER BY GROUPNAME";

//            return await db.GetDatatable(query);
//        }

//        // ============================================================
//        // GET SUB GROUPS ORDERED BY GROUP CODE
//        // ============================================================
//        public async Task<DataTable> GetSubGroupsOrderByGrpCd(
//            int mainGroupId)
//        {
//            string query = $@"
//                SELECT *
//                FROM GRPLDG
//                WHERE MGROUP = {mainGroupId}
//                ORDER BY GROUPCODE DESC";

//            return await db.GetDatatable(query);
//        }

//        // ============================================================
//        // GET MAX CHILD GROUP CODE
//        // ============================================================
//        public async Task<int> GetMaxCode(
//            int mainGroupId)
//        {
//            string query = $@"
//                SELECT ISNULL(MAX(GROUPCODE), 0)
//                FROM GRPLDG
//                WHERE MGROUP = {mainGroupId}";

//            return await db.ReturnIntValue(query);
//        }

//        // ============================================================
//        // GET GROUPS FOR LIST
//        // ============================================================
//        public async Task<DataTable> GetForList(
//            string? search = null)
//        {
//            string searchCondition = "";

//            if (!string.IsNullOrWhiteSpace(search))
//            {
//                string value = Escape(search);

//                searchCondition = $@"
//                    AND
//                    (
//                        GROUPNAME LIKE N'%{value}%'
//                        OR SECNAME LIKE N'%{value}%'
//                        OR CAST(GROUPCODE AS NVARCHAR(50))
//                            LIKE N'%{value}%'
//                    )";
//            }

//            string query = $@"
//                SELECT
//                    GROUPID AS Id,
//                    GROUPNAME AS [Group Name],
//                    GROUPCODE AS Code,
//                    GROUPNATURE AS [Group Nature]
//                FROM GRPLDG
//                WHERE GDELFLG = 0
//                {searchCondition}
//                ORDER BY GROUPNAME";

//            return await db.GetDatatable(query);
//        }

//        // ============================================================
//        // CUSTOMER / SUPPLIER LEDGER GROUPS
//        // Legacy:
//        // C = Customer -> group 26
//        // S = Supplier -> group 27
//        // ============================================================
//        public async Task<DataTable> CustOrSupLedgerGroup(
//            string type)
//        {
//            int groupId = 0;

//            if (string.Equals(type, "C",
//                    StringComparison.OrdinalIgnoreCase))
//            {
//                groupId = 26;
//            }
//            else if (string.Equals(type, "S",
//                    StringComparison.OrdinalIgnoreCase))
//            {
//                groupId = 27;
//            }

//            if (groupId == 0)
//                return new DataTable();

//            string query = $@"
//                SELECT
//                    GROUPID,
//                    GROUPNAME,
//                    GROUPCODE,
//                    GROUPNATURE
//                FROM GRPLDG
//                WHERE GACTIVE = 1
//                  AND GDELFLG = 0
//                  AND
//                  (
//                      GROUPID = {groupId}
//                      OR MGROUP = {groupId}
//                  )
//                ORDER BY GROUPNAME";

//            return await db.GetDatatable(query);
//        }

//        // ============================================================
//        // ESCAPE SQL STRING
//        // ============================================================
//        private static string Escape(string? value)
//        {
//            return (value ?? "")
//                .Replace("'", "''");
//        }
//        // ============================================================
//        // FINAL ACCOUNTS LOOKUP
//        // Legacy source: AccountGroupMaster_Load()
//        // ============================================================
//        public async Task<DataTable> GetFinalAccountsLookup()
//        {
//            DataTable dt = new DataTable();

//            dt.Columns.Add("CODE");
//            dt.Columns.Add("FINALACC");

//            dt.Rows.Add("P", "Profit & Loss A/c");
//            dt.Rows.Add("T", "Trading A/c");
//            dt.Rows.Add("B", "Balance Sheet");
//            dt.Rows.Add("M", "Manufacturing A/c");

//            return await Task.FromResult(dt);
//        }
//    }
//}