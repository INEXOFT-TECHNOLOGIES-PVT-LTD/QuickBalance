using System;
using System.Data;
using System.Threading.Tasks;

namespace QuickBalance.API.DB
{
    public class ItemGroupDB
    {
        private readonly DBConn DBConn;

        public ItemGroupDB(DBConn dbConn)
        {
            DBConn = dbConn;
        }

        // ============================================================
        // INSERT ITEM GROUP
        // ============================================================

        public async Task<int> Insert(
            string groupName,
            string secondName,
            int groupCode,
            bool isPrimaryGroup,
            int primaryGroupId,
            bool tagWithAccount,
            int stockAccount,
            int purchaseAccount,
            int salesAccount,
            bool isActive,
            int userId)
        {
            // When Tag With Account = NO,
            // old desktop saves all account IDs as 0
            if (!tagWithAccount)
            {
                stockAccount = 0;
                purchaseAccount = 0;
                salesAccount = 0;
            }

            // When Primary Group = YES,
            // old desktop saves PrimaryGroupId as 0
            if (isPrimaryGroup)
            {
                primaryGroupId = 0;
            }

            // Escape single quotes
            groupName = groupName.Replace("'", "''");
            secondName = secondName.Replace("'", "''");

            string qry = $@"
                INSERT INTO PRGRPMAS
                (
                    GRPNAME,
                    GRPCODE,
                    PRIMGRPFLG,
                    PRIMGRPID,
                    TAGACCFLG,
                    PURACCID,
                    SALESACCID,
                    STOCKACCID,
                    LOGCD,
                    UTIME,
                    ACTIVE,
                    SECNAME
                )
                VALUES
                (
                    '{groupName}',
                    {groupCode},
                    {(isPrimaryGroup ? 1 : 0)},
                    {primaryGroupId},
                    {(tagWithAccount ? 1 : 0)},
                    {purchaseAccount},
                    {salesAccount},
                    {stockAccount},
                    {userId},
                    GETDATE(),
                    {(isActive ? 1 : 0)},
                    '{secondName}'
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
            ";

            return await DBConn.InsertScalar(qry);
        }

        // ============================================================
        // GET ALL ITEM GROUPS
        // ============================================================

        //public async Task<DataTable> GetAll()
        //{
        //    string qry = @"
        //        SELECT
        //            PRGRPID,
        //            GRPNAME,
        //            SECNAME,
        //            GRPCODE,
        //            PRIMGRPFLG,
        //            PRIMGRPID,
        //            TAGACCFLG,
        //            PURACCID,
        //            SALESACCID,
        //            STOCKACCID,
        //            ACTIVE,
        //            LOGCD,
        //            UTIME
        //        FROM PRGRPMAS
        //        ORDER BY GRPNAME;
        //    ";

        //    return await DBConn.GetDatatable(qry);
        //}

        public async Task<(DataTable Data, int TotalRecords)> GetAll(int page,    int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            long offset = ((long)page - 1) * pageSize;

            const string countSql = @"
            SELECT COUNT(*)
            FROM PRGRPMAS;";

            string qry = $@"
            SELECT
                PRGRPID,
                GRPNAME,
                SECNAME,
                GRPCODE,
                PRIMGRPFLG,
                PRIMGRPID,
                TAGACCFLG,
                PURACCID,
                SALESACCID,
                STOCKACCID,
                ACTIVE,
                LOGCD,
                UTIME
            FROM PRGRPMAS
            ORDER BY GRPNAME, PRGRPID
            OFFSET {offset} ROWS
            FETCH NEXT {pageSize} ROWS ONLY;";

            DataTable countTable = await DBConn.GetDatatable(countSql);

            int totalRecords = countTable.Rows.Count > 0
                ? Convert.ToInt32(countTable.Rows[0][0])
                : 0;

            DataTable data = await DBConn.GetDatatable(qry);

            return (data, totalRecords);
        }

        // ============================================================
        // GET ITEM GROUP BY ID
        // ============================================================

        public async Task<DataTable> GetById(int groupId)
        {
            string qry = $@"
                SELECT
                    PRGRPID,
                    GRPNAME,
                    SECNAME,
                    GRPCODE,
                    PRIMGRPFLG,
                    PRIMGRPID,
                    TAGACCFLG,
                    PURACCID,
                    SALESACCID,
                    STOCKACCID,
                    ACTIVE,
                    LOGCD,
                    UTIME
                FROM PRGRPMAS
                WHERE PRGRPID = {groupId};
            ";

            return await DBConn.GetDatatable(qry);
        }

        // ============================================================
        // UPDATE ITEM GROUP
        // ============================================================

        public async Task<int> Update(
            int groupId,
            string groupName,
            string secondName,
            int groupCode,
            bool isPrimaryGroup,
            int primaryGroupId,
            bool tagWithAccount,
            int stockAccount,
            int purchaseAccount,
            int salesAccount,
            bool isActive,
            int userId)
        {
            // --------------------------------------------------------
            // OLD DESKTOP LOGIC
            // --------------------------------------------------------

            // If Tag With Account = NO,
            // save all account IDs as 0
            if (!tagWithAccount)
            {
                stockAccount = 0;
                purchaseAccount = 0;
                salesAccount = 0;
            }

            // If Primary Group = YES,
            // save PrimaryGroupId as 0
            if (isPrimaryGroup)
            {
                primaryGroupId = 0;
            }

            // --------------------------------------------------------
            // ESCAPE SINGLE QUOTES
            // --------------------------------------------------------

            groupName = groupName.Replace("'", "''");
            secondName = secondName.Replace("'", "''");

            // --------------------------------------------------------
            // UPDATE QUERY
            // --------------------------------------------------------

            string qry = $@"
                UPDATE PRGRPMAS
                SET
                    GRPNAME = '{groupName}',
                    GRPCODE = {groupCode},
                    PRIMGRPFLG = {(isPrimaryGroup ? 1 : 0)},
                    PRIMGRPID = {primaryGroupId},
                    TAGACCFLG = {(tagWithAccount ? 1 : 0)},
                    PURACCID = {purchaseAccount},
                    SALESACCID = {salesAccount},
                    STOCKACCID = {stockAccount},
                    ACTIVE = {(isActive ? 1 : 0)},
                    SECNAME = '{secondName}',
                    LOGCD = {userId},
                    UTIME = GETDATE()
                WHERE PRGRPID = {groupId};

                SELECT @@ROWCOUNT;
            ";

            // --------------------------------------------------------
            // EXECUTE UPDATE AND GET AFFECTED ROW COUNT
            // --------------------------------------------------------

            object? result =
                await DBConn.ExecuteScalar(qry);

            if (result == null || result == DBNull.Value)
            {
                return 0;
            }

            return Convert.ToInt32(result);
        }

        // ============================================================
        // DELETE ITEM GROUP
        // ============================================================

        public async Task<int> Delete(int groupId)
        {
            string qry = $@"
        DELETE FROM PRGRPMAS
        WHERE PRGRPID = {groupId};

        SELECT @@ROWCOUNT;
    ";

            object? result =
                await DBConn.ExecuteScalar(qry);

            if (result == null || result == DBNull.Value)
            {
                return 0;
            }

            return Convert.ToInt32(result);
        }

        // ============================================================
        // CHECK GROUP NAME EXISTS
        // ============================================================

        public async Task<bool> GroupNameExists(
            string groupName,
            int excludeGroupId = 0)
        {
            groupName = groupName.Replace("'", "''");

            string qry = $@"
        SELECT COUNT(*)
        FROM PRGRPMAS
        WHERE UPPER(LTRIM(RTRIM(GRPNAME))) =
              UPPER(LTRIM(RTRIM('{groupName}')))
          AND PRGRPID <> {excludeGroupId};
    ";

            object? result =
                await DBConn.ExecuteScalar(qry);

            if (result == null || result == DBNull.Value)
            {
                return false;
            }

            return Convert.ToInt32(result) > 0;
        }


        // ============================================================
        // CHECK GROUP CODE EXISTS
        // ============================================================

        public async Task<bool> GroupCodeExists(
            int groupCode,
            int excludeGroupId = 0)
        {
            string qry = $@"
        SELECT COUNT(*)
        FROM PRGRPMAS
        WHERE GRPCODE = {groupCode}
          AND PRGRPID <> {excludeGroupId};
    ";

            object? result =
                await DBConn.ExecuteScalar(qry);

            if (result == null || result == DBNull.Value)
            {
                return false;
            }

            return Convert.ToInt32(result) > 0;
        }

        public async Task<DataTable> GetPrimaryGroups()
        {
            string qry = @"
        SELECT PRGRPID, GRPNAME FROM PRGRPMAS WHERE ACTIVE = 1  ORDER BY GRPNAME; ";

            return await DBConn.GetDatatable(qry);
        }
    }
}