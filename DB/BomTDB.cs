using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using System.Data;

namespace QuickBalance.API.DB
{
    public class BomTDB
    {
        private readonly DBConn db;

        public Variable Variables = new Variable();
        public Function Functions = new Function();

        string qry = "";

        public BomTDB(DBConn dbConn)
        {
            db = dbConn;
        }

        // ============================================================
        // INSERT BOM ITEM
        // ============================================================

        public async Task<int> Insert(
            int bomCode,
            BomItemRequest request)
        {
            string type = request.Type?.Trim().ToUpper() ?? "C";

            qry =
                "INSERT INTO BOMT " +
                "(BOMCODE, PRCODE, QTY, UNIT, TYPE) " +
                "VALUES (" +
                bomCode + "," +
                request.ProductId + "," +
                request.Quantity + "," +
                request.UnitId + "," +
                "N'" + type + "'); " +
                "SELECT @@ROWCOUNT;";

            object? result = await db.ExecuteScalar(qry);

            if (result == null || result == DBNull.Value)
                return 0;

            return Convert.ToInt32(result);
        }

        // ============================================================
        // DELETE BOM ITEMS BY BOM CODE
        // ============================================================

        public async Task<int> DeleteByBomCode(int bomCode)
        {
            qry =
                "DELETE FROM BOMT " +
                "WHERE BOMCODE=" + bomCode + "; " +
                "SELECT @@ROWCOUNT;";

            object? result = await db.ExecuteScalar(qry);

            if (result == null || result == DBNull.Value)
                return 0;

            return Convert.ToInt32(result);
        }

        // ============================================================
        // SEARCH BOM ITEMS BY BOM CODE
        // ============================================================

        public async Task<DataTable> SearchByBomCode(int bomCode)
        {
            qry =
                "SELECT " +
                "BOMT.*," +
                "PRMAST.BASEPURPRICE AS PRICE," +
                "PRMAST.PURPRICETYPE," +
                "PRMAST.ALTPURPRICE," +
                "PRMAST.UNIT AS MAINUNIT," +
                "PRMAST.ALTUNIT," +
                "PRMAST.PRNAME," +
                "PRMAST.BASESALEPRICE," +
                "PRMAST.ALTSALEPRICE " +
                "FROM BOMT " +
                "INNER JOIN PRMAST " +
                "ON BOMT.PRCODE=PRMAST.PRCODE " +
                "WHERE BOMT.BOMCODE=" + bomCode;

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // SEARCH BOM BY PRODUCT CODE
        // ============================================================

        public async Task<DataTable> SearchByPrCode(ClsBomT clsBomT)
        {
            qry =
                "SELECT * " +
                "FROM BOMT " +
                "WHERE PRCODE=" + clsBomT.PrCode;

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // INSERT BOM REQUEST / TEMP ITEM
        // ============================================================

        public async Task<int> InsertRequest(
            int bomCode,
            BomItemRequest request,
            int branchId)
        {
            string type = request.Type?.Trim().ToUpper() ?? "C";

            qry =
                "INSERT INTO BOMT_TMP " +
                "(BOMCODE, PRCODE, QTY, UNIT, TYPE, BRCD, REQID) " +
                "VALUES (" +
                bomCode + "," +
                request.ProductId + "," +
                request.Quantity + "," +
                request.UnitId + "," +
                "N'" + type + "'," +
                branchId + "," +
                bomCode + "); " +
                "SELECT @@ROWCOUNT;";

            object? result = await db.ExecuteScalar(qry);

            if (result == null || result == DBNull.Value)
                return 0;

            return Convert.ToInt32(result);
        }

        // ============================================================
        // DELETE REQUEST / TEMP ITEMS
        // ============================================================

        public async Task<int> DeleteByBomCodeRequest(int bomCode)
        {
            qry =
                "DELETE FROM BOMT_TMP " +
                "WHERE BOMCODE=" + bomCode + "; " +
                "SELECT @@ROWCOUNT;";

            object? result = await db.ExecuteScalar(qry);

            if (result == null || result == DBNull.Value)
                return 0;

            return Convert.ToInt32(result);
        }

        // ============================================================
        // SEARCH REQUEST / TEMP ITEMS
        // ============================================================

        public async Task<DataTable> SearchByBomCodeRequest(int bomCode)
        {
            qry =
                "SELECT " +
                "BOMT_TMP.*," +
                "PRMAST.BASEPURPRICE AS PRICE," +
                "PRMAST.PURPRICETYPE," +
                "PRMAST.ALTPURPRICE," +
                "PRMAST.UNIT AS MAINUNIT," +
                "PRMAST.ALTUNIT," +
                "PRMAST.PRNAME," +
                "PRMAST.BASESALEPRICE," +
                "PRMAST.ALTSALEPRICE " +
                "FROM BOMT_TMP " +
                "INNER JOIN PRMAST " +
                "ON BOMT_TMP.PRCODE=PRMAST.PRCODE " +
                "WHERE BOMT_TMP.BOMCODE=" + bomCode;

            return await db.GetDatatable(qry);
        }
    }
}