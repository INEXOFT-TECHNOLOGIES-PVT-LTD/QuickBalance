using System.Data;

namespace QuickBalance.API.DB
{
    public class UnitConvDB
    {
        private readonly DBConn db;

        public UnitConvDB(DBConn dbConn)
        {
            db = dbConn;
        }

        // ============================================================
        // GET ALL
        // ============================================================

        public async Task<DataTable> GetAll()
        {
            string qry =
                "SELECT " +
                "C.CODE, " +
                "C.MAINUNIT, " +
                "MU.PRUNITNAME AS MAINUNITNAME, " +
                "C.SUBUNIT, " +
                "SU.PRUNITNAME AS SUBUNITNAME, " +
                "C.CONVFACT " +
                "FROM PRUNITCONV C " +
                "LEFT JOIN PRUNITMAS MU ON MU.PRUNITID = C.MAINUNIT " +
                "LEFT JOIN PRUNITMAS SU ON SU.PRUNITID = C.SUBUNIT " +
                "ORDER BY MU.PRUNITNAME, SU.PRUNITNAME";

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // GET BY ID
        // ============================================================

        public async Task<DataTable> GetById(int code)
        {
            string qry =
                "SELECT " +
                "C.CODE, " +
                "C.MAINUNIT, " +
                "MU.PRUNITNAME AS MAINUNITNAME, " +
                "C.SUBUNIT, " +
                "SU.PRUNITNAME AS SUBUNITNAME, " +
                "C.CONVFACT " +
                "FROM PRUNITCONV C " +
                "LEFT JOIN PRUNITMAS MU ON MU.PRUNITID = C.MAINUNIT " +
                "LEFT JOIN PRUNITMAS SU ON SU.PRUNITID = C.SUBUNIT " +
                "WHERE C.CODE = " + code;

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // INSERT
        // ============================================================

        public async Task<int> Insert(
            int mainUnit,
            int subUnit,
            decimal conversionFactor)
        {
            string qry =
                "INSERT INTO PRUNITCONV " +
                "(MAINUNIT, SUBUNIT, CONVFACT) " +
                "OUTPUT INSERTED.CODE " +
                "VALUES (" +
                mainUnit + ", " +
                subUnit + ", " +
                conversionFactor.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                ")";

            return await db.InsertScalar(qry);
        }

        // ============================================================
        // UPDATE
        // ============================================================

        public async Task<int> Update(
            int code,
            int mainUnit,
            int subUnit,
            decimal conversionFactor)
        {
            string qry =
                "UPDATE PRUNITCONV SET " +
                "MAINUNIT = " + mainUnit + ", " +
                "SUBUNIT = " + subUnit + ", " +
                "CONVFACT = " +
                conversionFactor.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                " WHERE CODE = " + code + "; " +
                "SELECT @@ROWCOUNT;";

            object? result = await db.ExecuteScalar(qry);

            if (result == null)
                return 0;

            return Convert.ToInt32(result);
        }

        // ============================================================
        // CHECK MAIN + SUB COMBINATION
        // ============================================================

        public async Task<bool> CombinationExists(
            int mainUnit,
            int subUnit)
        {
            string qry =
                "SELECT COUNT(*) " +
                "FROM PRUNITCONV " +
                "WHERE MAINUNIT = " + mainUnit +
                " AND SUBUNIT = " + subUnit;

            int count = await db.ReturnIntValue(qry);

            return count > 0;
        }

        // ============================================================
        // CHECK MAIN + SUB COMBINATION DURING UPDATE
        // ============================================================

        public async Task<bool> CombinationExists(
            int mainUnit,
            int subUnit,
            int code)
        {
            string qry =
                "SELECT COUNT(*) " +
                "FROM PRUNITCONV " +
                "WHERE MAINUNIT = " + mainUnit +
                " AND SUBUNIT = " + subUnit +
                " AND CODE <> " + code;

            int count = await db.ReturnIntValue(qry);

            return count > 0;
        }

        // ============================================================
        // CHECK MAIN UNIT
        // ============================================================

        public async Task<bool> MainUnitExists(int mainUnit)
        {
            string qry =
                "SELECT COUNT(*) " +
                "FROM PRUNITCONV " +
                "WHERE MAINUNIT = " + mainUnit;

            int count = await db.ReturnIntValue(qry);

            return count > 0;
        }

        // ============================================================
        // CHECK MAIN UNIT DURING UPDATE
        // ============================================================

        public async Task<bool> MainUnitExists(
            int mainUnit,
            int code)
        {
            string qry =
                "SELECT COUNT(*) " +
                "FROM PRUNITCONV " +
                "WHERE MAINUNIT = " + mainUnit +
                " AND CODE <> " + code;

            int count = await db.ReturnIntValue(qry);

            return count > 0;
        }
    }
}