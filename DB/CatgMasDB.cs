using System.Data;
using QuickBalance.API.CLS;

namespace QuickBalance.API.DB
{
    public class CatgMasDB
    {
        private readonly DBConn db;

        public Variable Variables { get; set; } = new Variable();

        public CommonFunction Functions { get; set; }

        public CatgMasDB(DBConn db)
        {
            this.db = db;
            Functions = new CommonFunction(db);
        }

        // ============================================================
        // INSERT
        // ============================================================
        public async Task<int> Insert(
            string categoryName,
            string alias,
            bool isActive,
            int logCode,
            string secondName)
        {
            string qry =
                "INSERT INTO CATGMAS " +
                "(CATGNAME, ALIAS, ACTIVE, LOGCD, UTIME, SECNAME) " +
                "OUTPUT INSERTED.CATGCODE " +
                "VALUES (" +
                "'" + Escape(categoryName) + "', " +
                "'" + Escape(alias) + "', " +
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
            int categoryCode,
            string categoryName,
            string alias,
            bool isActive,
            string secondName)
        {
            string qry =
                "UPDATE CATGMAS SET " +
                "CATGNAME = '" + Escape(categoryName) + "', " +
                "ALIAS = '" + Escape(alias) + "', " +
                "ACTIVE = " + (isActive ? "1" : "0") + ", " +
                "SECNAME = '" + Escape(secondName) + "' " +
                "WHERE CATGCODE = " + categoryCode + "; " +
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
                "CATGCODE, " +
                "CATGNAME, " +
                "ALIAS, " +
                "ACTIVE, " +
                "SECNAME, " +
                "LOGCD, " +
                "UTIME " +
                "FROM CATGMAS " +
                "ORDER BY CATGNAME";

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // GET BY ID
        // ============================================================
        public async Task<DataTable> GetById(int categoryCode)
        {
            string qry =
                "SELECT " +
                "CATGCODE, " +
                "CATGNAME, " +
                "ALIAS, " +
                "ACTIVE, " +
                "SECNAME, " +
                "LOGCD, " +
                "UTIME " +
                "FROM CATGMAS " +
                "WHERE CATGCODE = " + categoryCode;

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // CHECK NAME - CREATE
        // ============================================================
        public async Task<bool> NameExists(string categoryName)
        {
            string qry =
                "SELECT COUNT(*) " +
                "FROM CATGMAS " +
                "WHERE CATGNAME = '" + Escape(categoryName) + "'";

            int count = await db.ReturnIntValue(qry);

            return count > 0;
        }

        // ============================================================
        // CHECK NAME - UPDATE
        // ============================================================
        public async Task<bool> NameExists(
            string categoryName,
            int categoryCode)
        {
            string qry =
                "SELECT COUNT(*) " +
                "FROM CATGMAS " +
                "WHERE CATGNAME = '" + Escape(categoryName) + "' " +
                "AND CATGCODE <> " + categoryCode;

            int count = await db.ReturnIntValue(qry);

            return count > 0;
        }

        // ============================================================
        // ACTIVE CATEGORIES
        // ============================================================
        public async Task<DataTable> GetActive()
        {
            string qry =
                "SELECT " +
                "CATGCODE, " +
                "CATGNAME " +
                "FROM CATGMAS " +
                "WHERE ACTIVE = 1 " +
                "ORDER BY CATGNAME";

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // SEARCH - EXISTING APPLICATION
        // ============================================================
        public async Task<DataTable> Search()
        {
            string qry =
                "SELECT CATGCODE AS Id, CATGNAME AS Category " +
                "FROM CATGMAS " +
                "ORDER BY CATGNAME";

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // SEARCH - API WITH TEXT
        // ============================================================
        public async Task<DataTable> Search(string text)
        {
            string searchText = Escape(text);

            string qry =
                "SELECT " +
                "CATGCODE AS Id, " +
                "CATGNAME AS Category " +
                "FROM CATGMAS " +
                "WHERE CATGNAME LIKE '%" + searchText + "%' " +
                "ORDER BY CATGNAME";

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // DROPDOWN
        // ============================================================
        public async Task<DataTable> GetDropdown()
        {
            string qry =
                "SELECT " +
                "CATGCODE AS Id, " +
                "CATGNAME AS Category " +
                "FROM CATGMAS " +
                "WHERE ACTIVE = 1 " +
                "ORDER BY CATGNAME";

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // EXPORT
        // ============================================================
        public async Task<DataTable> ExportToJson()
        {
            string qry =
                "SELECT " +
                "CATGNAME, " +
                "SECNAME, " +
                "ALIAS " +
                "FROM CATGMAS " +
                "WHERE ACTIVE = 1 " +
                "ORDER BY CATGNAME";

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // ESCAPE SQL STRING
        // ============================================================
        private string Escape(string value)
        {
            return (value ?? "").Replace("'", "''");
        }
    }
}