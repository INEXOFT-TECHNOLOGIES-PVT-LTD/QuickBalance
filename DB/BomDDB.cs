using System.Data;
using QuickBalance.API.DTOs;

namespace QuickBalance.API.DB
{
    public class BomDDB
    {
        private readonly DBConn db;

        public BomDDB(DBConn dbConn)
        {
            db = dbConn;
        }

        // ============================================================
        // INSERT BOM ADDITIONAL INFORMATION
        // ============================================================

        public async Task<int> Insert(
            int bomCode,
            BomAdditionalInfoRequest request)
        {
            string qry =
                "INSERT INTO BOMD " +
                "(BOMCODE, CODE, AMOUNT) " +
                "VALUES (" +
                bomCode + "," +
                request.AccountId + "," +
                request.Amount + "); " +
                "SELECT @@ROWCOUNT;";

            object? result = await db.ExecuteScalar(qry);

            if (result == null || result == DBNull.Value)
                return 0;

            return Convert.ToInt32(result);
        }

        // ============================================================
        // DELETE BOM ADDITIONAL INFORMATION
        // ============================================================

        public async Task<int> DeleteByBomCode(int bomCode)
        {
            string qry =
                "DELETE FROM BOMD " +
                "WHERE BOMCODE=" + bomCode + "; " +
                "SELECT @@ROWCOUNT;";

            object? result = await db.ExecuteScalar(qry);

            if (result == null || result == DBNull.Value)
                return 0;

            return Convert.ToInt32(result);
        }

        // ============================================================
        // SEARCH BOM ADDITIONAL INFORMATION BY BOM CODE
        // ============================================================

        public async Task<DataTable> SearchByBomCode(int bomCode)
        {
            string qry =
                "SELECT * " +
                "FROM BOMD " +
                "WHERE BOMCODE=" + bomCode;

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // VALIDATE ACCOUNT
        // ============================================================

        public async Task<DataTable> ValidateAccount(int accountId)
        {
            string qry =
                "SELECT * " +
                "FROM BOMD " +
                "WHERE CODE=" + accountId;

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // REQUEST / TEMP - INSERT
        // ============================================================

        public async Task<int> InsertRequest(
            int bomCode,
            BomAdditionalInfoRequest request,
            int branchId)
        {
            string qry =
                "INSERT INTO BOMD_TMP " +
                "(BOMCODE, CODE, AMOUNT, BRCD, REQID) " +
                "VALUES (" +
                bomCode + "," +
                request.AccountId + "," +
                request.Amount + "," +
                branchId + "," +
                bomCode + "); " +
                "SELECT @@ROWCOUNT;";

            object? result = await db.ExecuteScalar(qry);

            if (result == null || result == DBNull.Value)
                return 0;

            return Convert.ToInt32(result);
        }

        // ============================================================
        // REQUEST / TEMP - DELETE
        // ============================================================

        public async Task<int> DeleteByBomCodeRequest(int bomCode)
        {
            string qry =
                "DELETE FROM BOMD_TMP " +
                "WHERE BOMCODE=" + bomCode + "; " +
                "SELECT @@ROWCOUNT;";

            object? result = await db.ExecuteScalar(qry);

            if (result == null || result == DBNull.Value)
                return 0;

            return Convert.ToInt32(result);
        }

        // ============================================================
        // REQUEST / TEMP - SEARCH
        // ============================================================

        public async Task<DataTable> SearchByBomCodeRequest(int bomCode)
        {
            string qry =
                "SELECT * " +
                "FROM BOMD_TMP " +
                "WHERE BOMCODE=" + bomCode;

            return await db.GetDatatable(qry);
        }
    }
}