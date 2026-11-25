using System.Data;

namespace QuickBalance.API.DB
{
    public class PrUnitMasDBs
    {
        private readonly DBConn db;

        public PrUnitMasDBs(DBConn dbConn)
        {
            db = dbConn;
        }

        // Existing method
        public async Task<DataTable> ProductUnits()
        {
            string qry =
                "SELECT PRUNITID AS Id, PRUNITNAME AS UOM " +
                "FROM PRUNITMAS " +
                "ORDER BY PRUNITNAME";

            return await db.GetDatatable(qry);
        }

        // Unit Master - Insert
        public async Task<int> Insert(
            string unitName,
            bool isActive,
            string secondName,
            string formalName)
        {
            unitName = unitName.Replace("'", "''");
            secondName = secondName.Replace("'", "''");
            formalName = formalName.Replace("'", "''");

            string qry = $@"
                INSERT INTO PRUNITMAS
                (
                    PRUNITNAME,
                    ACTIVE,
                    SECNAME,
                    FORMALNAME
                )
                OUTPUT INSERTED.PRUNITID
                VALUES
                (
                    '{unitName}',
                    {(isActive ? 1 : 0)},
                    '{secondName}',
                    '{formalName}'
                );
            ";

            return await db.InsertScalar(qry);
        }

        // Unit Master - Get All
        public async Task<DataTable> GetAll()
        {
            string qry = @"
                SELECT
                    PRUNITID,
                    PRUNITNAME,
                    ACTIVE,
                    SECNAME,
                    FORMALNAME
                FROM PRUNITMAS
                ORDER BY PRUNITNAME;
            ";

            return await db.GetDatatable(qry);
        }

        // Unit Master - Get By ID
        public async Task<DataTable> GetById(int unitId)
        {
            string qry = $@"
                SELECT
                    PRUNITID,
                    PRUNITNAME,
                    ACTIVE,
                    SECNAME,
                    FORMALNAME
                FROM PRUNITMAS
                WHERE PRUNITID = {unitId};
            ";

            return await db.GetDatatable(qry);
        }

        // Unit Master - Update
        public async Task<int> Update(
            int unitId,
            string unitName,
            bool isActive,
            string secondName,
            string formalName)
        {
            unitName = unitName.Replace("'", "''");
            secondName = secondName.Replace("'", "''");
            formalName = formalName.Replace("'", "''");

            string qry = $@"
                UPDATE PRUNITMAS
                SET
                    PRUNITNAME = '{unitName}',
                    ACTIVE = {(isActive ? 1 : 0)},
                    SECNAME = '{secondName}',
                    FORMALNAME = '{formalName}'
                WHERE PRUNITID = {unitId};

                SELECT @@ROWCOUNT;
            ";

            object? result = await db.ExecuteScalar(qry);

            if (result == null || result == DBNull.Value)
                return 0;

            return Convert.ToInt32(result);
        }

        // Unit Master - Name Validation
        public async Task<bool> NameExists(
            string unitName,
            int excludeUnitId = 0)
        {
            unitName = unitName.Replace("'", "''");

            string qry = $@"
                SELECT COUNT(*)
                FROM PRUNITMAS
                WHERE PRUNITNAME = '{unitName}'
                  AND PRUNITID <> {excludeUnitId};
            ";

            object? result = await db.ExecuteScalar(qry);

            if (result == null || result == DBNull.Value)
                return false;

            return Convert.ToInt32(result) > 0;
        }

        // Active Units
        public async Task<DataTable> GetActiveUnits()
        {
            string qry = @"
                SELECT
                    PRUNITID,
                    PRUNITNAME
                FROM PRUNITMAS
                WHERE ACTIVE = 1
                ORDER BY PRUNITNAME;
            ";

            return await db.GetDatatable(qry);
        }

        // Search Units
        public async Task<DataTable> Search(string text)
        {
            text = text.Replace("'", "''");

            string qry = $@"
                SELECT
                    PRUNITID,
                    PRUNITNAME
                FROM PRUNITMAS
                WHERE PRUNITNAME LIKE '%{text}%'
                   OR CAST(PRUNITID AS VARCHAR(20)) LIKE '%{text}%'
                   OR SECNAME LIKE '%{text}%'
                ORDER BY PRUNITNAME;
            ";

            return await db.GetDatatable(qry);
        }
    }
}