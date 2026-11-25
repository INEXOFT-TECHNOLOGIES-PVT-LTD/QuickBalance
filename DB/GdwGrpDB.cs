using System.Data;

namespace QuickBalance.API.DB
{
    public class GdwGrpDB
    {
        private readonly DBConn db;

        public GdwGrpDB(DBConn dbConn)
        {
            db = dbConn;
        }

        private string Escape(string value)
        {
            return (value ?? "").Replace("'", "''");
        }

        // ============================================================
        // INSERT
        // ============================================================
        public async Task<int> Insert(
            string groupName,
            bool isActive,
            int logCode,
            string secondName)
        {
            string qry =
                "INSERT INTO GDWGRP " +
                "(GRPNAME, ACTIVE, LOGCD, UTIME, SECNAME) " +
                "OUTPUT INSERTED.GRPID " +
                "VALUES (" +
                "'" + Escape(groupName) + "', " +
                (isActive ? "1" : "0") + ", " +
                logCode + ", " +
                "GETDATE(), " +
                "'" + Escape(secondName) + "'" +
                ")";

            return await db.InsertScalar(qry);
        }

        // ============================================================
        // UPDATE
        // ============================================================
        public async Task<int> Update(
            int groupId,
            string groupName,
            bool isActive,
            string secondName)
        {
            string qry =
                "UPDATE GDWGRP SET " +
                "GRPNAME = '" + Escape(groupName) + "', " +
                "ACTIVE = " + (isActive ? "1" : "0") + ", " +
                "SECNAME = '" + Escape(secondName) + "' " +
                "WHERE GRPID = " + groupId + "; " +
                "SELECT @@ROWCOUNT;";

            object? result = await db.ExecuteScalar(qry);

            if (result == null)
                return 0;

            return Convert.ToInt32(result);
        }

        // ============================================================
        // GET ALL
        // ============================================================
        public async Task<DataTable> GetAll()
        {
            string qry =
                "SELECT " +
                "GRPID, " +
                "GRPNAME, " +
                "ACTIVE, " +
                "SECNAME, " +
                "LOGCD, " +
                "UTIME " +
                "FROM GDWGRP " +
                "ORDER BY GRPNAME";

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // GET BY ID
        // ============================================================
        public async Task<DataTable> GetById(int groupId)
        {
            string qry =
                "SELECT " +
                "GRPID, " +
                "GRPNAME, " +
                "ACTIVE, " +
                "SECNAME, " +
                "LOGCD, " +
                "UTIME " +
                "FROM GDWGRP " +
                "WHERE GRPID = " + groupId;

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // CHECK NAME - CREATE
        // ============================================================
        public async Task<bool> NameExists(string groupName)
        {
            string qry =
                "SELECT COUNT(*) " +
                "FROM GDWGRP " +
                "WHERE GRPNAME = '" + Escape(groupName) + "'";

            int count = await db.ReturnIntValue(qry);

            return count > 0;
        }

        // ============================================================
        // CHECK NAME - UPDATE
        // ============================================================
        public async Task<bool> NameExists(
            string groupName,
            int groupId)
        {
            string qry =
                "SELECT COUNT(*) " +
                "FROM GDWGRP " +
                "WHERE GRPNAME = '" + Escape(groupName) + "' " +
                "AND GRPID <> " + groupId;

            int count = await db.ReturnIntValue(qry);

            return count > 0;
        }

        // ============================================================
        // ACTIVE GROUPS
        // Old: Search()
        // ============================================================
        public async Task<DataTable> GetActive()
        {
            string qry =
                "SELECT " +
                "GRPID, " +
                "GRPNAME " +
                "FROM GDWGRP " +
                "WHERE ACTIVE = 1 " +
                "ORDER BY GRPNAME";

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // SEARCH / DROPDOWN
        // ============================================================
        public async Task<DataTable> Search(string text)
        {
            string searchText = Escape(text);

            string qry =
                "SELECT " +
                "GRPID AS Id, " +
                "GRPNAME AS [Group Name] " +
                "FROM GDWGRP " +
                "WHERE GRPNAME LIKE '%" + searchText + "%' " +
                "ORDER BY GRPNAME";

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // DROPDOWN
        // ============================================================
        public async Task<DataTable> GetDropdown()
        {
            string qry =
                "SELECT " +
                "GRPID AS Id, " +
                "GRPNAME AS [Group Name] " +
                "FROM GDWGRP " +
                "WHERE ACTIVE = 1 " +
                "ORDER BY GRPNAME";

            return await db.GetDatatable(qry);
        }
    }
}