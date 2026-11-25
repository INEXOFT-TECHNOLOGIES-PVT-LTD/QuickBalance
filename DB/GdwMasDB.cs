using System.Data;
using System.Globalization;

namespace QuickBalance.API.DB
{
    public class GdwMasDB
    {
        private readonly DBConn db;

        public GdwMasDB(DBConn dbConn)
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
            string locationName,
            int groupId,
            bool isActive,
            int stockAccountId,
            bool stockPLFlag,
            int logCode,
            string secondName)
        {
            string qry =
                "INSERT INTO GDWMAS " +
                "(GDWNAME, GRPID, ACTIVE, STKACCID, STKPLFLG, LOGCD, UTIME, SECNAME) " +
                "OUTPUT INSERTED.GDWID " +
                "VALUES (" +
                "'" + Escape(locationName) + "', " +
                groupId + ", " +
                (isActive ? "1" : "0") + ", " +
                stockAccountId + ", " +
                (stockPLFlag ? "1" : "0") + ", " +
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
            int locationId,
            string locationName,
            int groupId,
            bool isActive,
            int stockAccountId,
            bool stockPLFlag,
            string secondName)
        {
            string qry =
                "UPDATE GDWMAS SET " +
                "GDWNAME = '" + Escape(locationName) + "', " +
                "GRPID = " + groupId + ", " +
                "ACTIVE = " + (isActive ? "1" : "0") + ", " +
                "STKACCID = " + stockAccountId + ", " +
                "STKPLFLG = " + (stockPLFlag ? "1" : "0") + ", " +
                "SECNAME = '" + Escape(secondName) + "' " +
                "WHERE GDWID = " + locationId + "; " +
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
                "GDWID, " +
                "GDWNAME, " +
                "GRPID, " +
                "ACTIVE, " +
                "STKACCID, " +
                "STKPLFLG, " +
                "SECNAME, " +
                "LOGCD, " +
                "UTIME " +
                "FROM GDWMAS " +
                "ORDER BY GDWNAME";

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // GET BY ID
        // ============================================================
        public async Task<DataTable> GetById(int locationId)
        {
            string qry =
                "SELECT " +
                "GDWID, " +
                "GDWNAME, " +
                "GRPID, " +
                "ACTIVE, " +
                "STKACCID, " +
                "STKPLFLG, " +
                "SECNAME, " +
                "LOGCD, " +
                "UTIME " +
                "FROM GDWMAS " +
                "WHERE GDWID = " + locationId;

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // NAME EXISTS - CREATE
        // ============================================================
        public async Task<bool> NameExists(string locationName)
        {
            string qry =
                "SELECT COUNT(*) " +
                "FROM GDWMAS " +
                "WHERE GDWNAME = '" + Escape(locationName) + "'";

            int count = await db.ReturnIntValue(qry);

            return count > 0;
        }

        // ============================================================
        // NAME EXISTS - UPDATE
        // ============================================================
        public async Task<bool> NameExists(
            string locationName,
            int locationId)
        {
            string qry =
                "SELECT COUNT(*) " +
                "FROM GDWMAS " +
                "WHERE GDWNAME = '" + Escape(locationName) + "' " +
                "AND GDWID <> " + locationId;

            int count = await db.ReturnIntValue(qry);

            return count > 0;
        }

        // ============================================================
        // ACTIVE LOCATIONS
        // ============================================================
        public async Task<DataTable> GetActive()
        {
            string qry =
                "SELECT " +
                "GDWID, " +
                "GDWNAME " +
                "FROM GDWMAS " +
                "WHERE ACTIVE = 1 " +
                "ORDER BY GDWNAME";

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // SEARCH
        // ============================================================
        public async Task<DataTable> Search(string text)
        {
            string searchText = Escape(text);

            string qry =
                "SELECT " +
                "GDWID AS Id, " +
                "GDWNAME AS Godown " +
                "FROM GDWMAS " +
                "WHERE GDWID LIKE '%" + searchText + "%' " +
                "OR GDWNAME LIKE '%" + searchText + "%' " +
                "ORDER BY GDWNAME";

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // LOCATION DROPDOWN
        // Matches old Search()
        // ============================================================
        public async Task<DataTable> GetDropdown()
        {
            string qry =
                "SELECT " +
                "GDWID AS Id, " +
                "GDWNAME AS Godown " +
                "FROM GDWMAS " +
                "ORDER BY GDWNAME";

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // VALIDATE STOCK ACCOUNT
        // Matches old ValidateAcc()
        // ============================================================
        public async Task<bool> IsStockAccountUsed(int accountId)
        {
            string qry =
                "SELECT COUNT(*) " +
                "FROM GDWMAS " +
                "WHERE STKACCID = " + accountId;

            int count = await db.ReturnIntValue(qry);

            return count > 0;
        }
    }
}