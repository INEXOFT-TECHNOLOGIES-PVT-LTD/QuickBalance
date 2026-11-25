using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using System.Data;

namespace QuickBalance.API.DB
{
    public class BomMasDB
    {
        private readonly DBConn db;

        public Variable Variables = new Variable();
        public Function Functions = new Function();

        string qry = "";

        public BomMasDB(DBConn dbConn)
        {
            db = dbConn;
        }

        // =========================================================
        // INSERT BOM MASTER
        // =========================================================

        public async Task<int> Insert(BomMasterRequest request, int userId)
        {
            bool mcGenFlg = string.Equals(
                request.McGenFlg?.Trim(),
                "Y",
                StringComparison.OrdinalIgnoreCase);

            bool mcConsFlg = string.Equals(
                request.McConsFlg?.Trim(),
                "Y",
                StringComparison.OrdinalIgnoreCase);

            qry =
                "INSERT INTO BOMMAS " +
                "(BOMNAME, ALIAS, PRCODE, QTY, UNIT, EXPENCES, " +
                "MCGENFLG, MCGEN, MCCONSFLG, MCCONS, LOGCD, UTIME, SECNAME) " +
                "OUTPUT INSERTED.CODE " +
                "VALUES (" +
                "N'" + (request.BomName ?? "").Replace("'", "''") + "'," +
                "N'" + (request.Alias ?? "").Replace("'", "''") + "'," +
                request.PrCode + "," +
                request.Qty + "," +
                request.Unit + "," +
                request.Expences + "," +
                (mcGenFlg ? "1" : "0") + "," +
                request.McGen + "," +
                (mcConsFlg ? "1" : "0") + "," +
                request.McCons + "," +
                userId + "," +
                "GETDATE()," +
                "N'" + (request.SecondName ?? "").Replace("'", "''") + "')";

            return await db.InsertScalar(qry);
        }

        // =========================================================
        // UPDATE BOM MASTER
        // =========================================================

        public async Task<int> Update(
     int id,
     BomMasterRequest request,
     int userId)
        {
            try
            {
                bool mcGenFlg =
                    string.Equals(
                        request.McGenFlg?.Trim(),
                        "Y",
                        StringComparison.OrdinalIgnoreCase);

                bool mcConsFlg =
                    string.Equals(
                        request.McConsFlg?.Trim(),
                        "Y",
                        StringComparison.OrdinalIgnoreCase);

                qry =
                    "UPDATE BOMMAS SET " +
                    "BOMNAME=N'" + SqlEscape(request.BomName) + "'," +
                    "ALIAS=N'" + SqlEscape(request.Alias) + "'," +
                    "PRCODE=" + request.PrCode + "," +
                    "QTY=" + request.Qty + "," +
                    "UNIT=" + request.Unit + "," +
                    "EXPENCES=" + request.Expences + "," +
                    "MCGENFLG=" + (mcGenFlg ? "1" : "0") + "," +
                    "MCGEN=" + request.McGen + "," +
                    "MCCONSFLG=" + (mcConsFlg ? "1" : "0") + "," +
                    "MCCONS=" + request.McCons + "," +
                    "LOGCD=" + userId + "," +
                    "UTIME=GETDATE()," +
                    "SECNAME=N'" + SqlEscape(request.SecondName) + "' " +
                    "WHERE CODE=" + id + "; " +
                    "SELECT @@ROWCOUNT;";

                object? result =
                    await db.ExecuteScalar(qry);

                if (result == null ||
                    result == DBNull.Value)
                {
                    return 0;
                }

                return Convert.ToInt32(result);
            }
            catch
            {
                throw;
            }
        }
        // =========================================================
        // SEARCH BOM BY CODE
        // =========================================================

        public async Task<DataTable> SearchByCode(int id)
        {
            qry =
                "SELECT * " +
                "FROM BOMMAS " +
                "WHERE CODE=" + id;

            return await db.GetDatatable(qry);
        }

        // =========================================================
        // DELETE BOM MASTER
        // =========================================================

        public async Task<int> DeleteByCode(int id)
        {
            qry =
                "DELETE FROM BOMMAS " +
                "WHERE CODE=" + id + "; " +
                "SELECT @@ROWCOUNT;";

            object? result =
                await db.ExecuteScalar(qry);

            if (result == null ||
                result == DBNull.Value)
            {
                return 0;
            }

            return Convert.ToInt32(result);
        }

        // =========================================================
        // CHECK OCCURRENCE
        // =========================================================

        public async Task<bool> CheckOccurrence(
            string field,
            string value)
        {
            string safeValue =
                SqlEscape(value);

            qry =
                "SELECT COUNT(*) " +
                "FROM BOMMAS " +
                "WHERE " +
                field +
                "=N'" +
                safeValue +
                "'";

            object? result =
                await db.ExecuteScalar(qry);

            if (result == null ||
                result == DBNull.Value)
            {
                return false;
            }

            return Convert.ToInt32(result) > 0;
        }

        // =========================================================
        // CHECK OCCURRENCE FOR UPDATE
        // =========================================================

        public async Task<bool> CheckOccurrenceForUpdate(
            string field,
            string value,
            int id)
        {
            string safeValue =
                SqlEscape(value);

            qry =
                "SELECT COUNT(*) " +
                "FROM BOMMAS " +
                "WHERE " +
                field +
                "=N'" +
                safeValue +
                "' " +
                "AND CODE<>" +
                id;

            object? result =
                await db.ExecuteScalar(qry);

            if (result == null ||
                result == DBNull.Value)
            {
                return false;
            }

            return Convert.ToInt32(result) > 0;
        }

        // =========================================================
        // LIST BOM
        // =========================================================

        public async Task<DataTable> List()
        {
            qry =
                "SELECT " +
                "CODE AS Id, " +
                "BOMNAME AS BOM, " +
                "ALIAS AS Alias " +
                "FROM BOMMAS " +
                "ORDER BY BOMNAME";

            return await db.GetDatatable(qry);
        }

        // =========================================================
        // BOM LIST
        // =========================================================

        public async Task<DataTable> BOMs()
        {
            qry =
                "SELECT " +
                "CODE AS Id, " +
                "BOMNAME AS BOM, " +
                "ALIAS AS Alias " +
                "FROM BOMMAS " +
                "ORDER BY BOMNAME";

            return await db.GetDatatable(qry);
        }

        // =========================================================
        // ALL BOMs
        // =========================================================

        public async Task<DataTable> BOMsAll()
        {
            qry =
                "SELECT * " +
                "FROM BOMMAS " +
                "ORDER BY BOMNAME";

            return await db.GetDatatable(qry);
        }

        // =========================================================
        // DETAILS BY CODE
        // =========================================================

        public async Task<DataTable> DetailsByCode(int id)
        {
            qry =
                "SELECT * " +
                "FROM BOMMAS " +
                "WHERE CODE=" + id;

            return await db.GetDatatable(qry);
        }

        // =========================================================
        // BOM DETAILS
        // =========================================================

        public async Task<DataTable> BOMDetailsByCode(int id)
        {
            qry =
                "SELECT " +
                "BOMMAS.*, " +
                "PRMAST.PRNAME, " +
                "PRUNITMAS.PRUNITNAME " +
                "FROM BOMMAS " +
                "LEFT JOIN PRMAST " +
                "ON BOMMAS.PRCODE=PRMAST.PRCODE " +
                "LEFT JOIN PRUNITMAS " +
                "ON BOMMAS.UNIT=PRUNITMAS.PRUNITID " +
                "WHERE BOMMAS.CODE=" + id;

            return await db.GetDatatable(qry);
        }

        // =========================================================
        // CHECK REQUEST OCCURRENCE
        // =========================================================

        public async Task<bool> CheckOccurrenceRequest(
            string field,
            string value,
            int branchId)
        {
            string safeValue =
                SqlEscape(value);

            qry =
                "SELECT COUNT(*) " +
                "FROM BOMMAS_TMP " +
                "WHERE " +
                field +
                "=N'" +
                safeValue +
                "' " +
                "AND BRCD=" +
                branchId;

            object? result =
                await db.ExecuteScalar(qry);

            if (result == null ||
                result == DBNull.Value)
            {
                return false;
            }

            return Convert.ToInt32(result) > 0;
        }

        // =========================================================
        // CHECK REQUEST OCCURRENCE FOR UPDATE
        // =========================================================

        public async Task<bool> CheckOccurrenceForUpdateRequest(
            string field,
            string value,
            int id,
            int branchId)
        {
            string safeValue =
                SqlEscape(value);

            qry =
                "SELECT COUNT(*) " +
                "FROM BOMMAS_TMP " +
                "WHERE " +
                field +
                "=N'" +
                safeValue +
                "' " +
                "AND CODE<>" +
                id +
                " " +
                "AND BRCD=" +
                branchId;

            object? result =
                await db.ExecuteScalar(qry);

            if (result == null ||
                result == DBNull.Value)
            {
                return false;
            }

            return Convert.ToInt32(result) > 0;
        }

        // =========================================================
        // INSERT REQUEST
        // =========================================================

        public async Task<int> InsertRequest(
            BomMasterRequest request,
            int userId,
            int branchId)
        {
            string mcGenFlg =
                NormalizeFlag(request.McGenFlg);

            string mcConsFlg =
                NormalizeFlag(request.McConsFlg);

            qry =
                "INSERT INTO BOMMAS_TMP " +
                "(BOMNAME, ALIAS, PRCODE, QTY, UNIT, EXPENCES, " +
                "MCGENFLG, MCGEN, MCCONSFLG, MCCONS, LOGCD, UTIME, " +
                "SECNAME, BRCD, REQID) " +
                "OUTPUT INSERTED.CODE " +
                "VALUES (" +
                "N'" + SqlEscape(request.BomName) + "'," +
                "N'" + SqlEscape(request.Alias) + "'," +
                request.PrCode + "," +
                request.Qty + "," +
                request.Unit + "," +
                request.Expences + "," +
                "N'" + mcGenFlg + "'," +
                request.McGen + "," +
                "N'" + mcConsFlg + "'," +
                request.McCons + "," +
                userId + "," +
                "GETDATE()," +
                "N'" + SqlEscape(request.SecondName) + "'," +
                branchId + "," +
                "0)";

            object? result =
                await db.ExecuteScalar(qry);

            if (result == null ||
                result == DBNull.Value)
            {
                return 0;
            }

            return Convert.ToInt32(result);
        }

        // =========================================================
        // UPDATE REQUEST
        // =========================================================

        public async Task<int> UpdateRequest(
            int id,
            BomMasterRequest request,
            int userId,
            int branchId)
        {
            string mcGenFlg =
                NormalizeFlag(request.McGenFlg);

            string mcConsFlg =
                NormalizeFlag(request.McConsFlg);

            qry =
                "UPDATE BOMMAS_TMP SET " +
                "BOMNAME=N'" + SqlEscape(request.BomName) + "'," +
                "ALIAS=N'" + SqlEscape(request.Alias) + "'," +
                "PRCODE=" + request.PrCode + "," +
                "QTY=" + request.Qty + "," +
                "UNIT=" + request.Unit + "," +
                "EXPENCES=" + request.Expences + "," +
                "MCGENFLG=N'" + mcGenFlg + "'," +
                "MCGEN=" + request.McGen + "," +
                "MCCONSFLG=N'" + mcConsFlg + "'," +
                "MCCONS=" + request.McCons + "," +
                "LOGCD=" + userId + "," +
                "UTIME=GETDATE()," +
                "SECNAME=N'" + SqlEscape(request.SecondName) + "' " +
                "WHERE CODE=" + id +
                " AND BRCD=" + branchId +
                "; SELECT @@ROWCOUNT;";

            object? result =
                await db.ExecuteScalar(qry);

            if (result == null ||
                result == DBNull.Value)
            {
                return 0;
            }

            return Convert.ToInt32(result);
        }

        // =========================================================
        // DELETE REQUEST
        // =========================================================

        public async Task<int> DeleteByCodeRequest(
            int id,
            int branchId)
        {
            qry =
                "DELETE FROM BOMMAS_TMP " +
                "WHERE CODE=" +
                id +
                " AND BRCD=" +
                branchId +
                "; SELECT @@ROWCOUNT;";

            object? result =
                await db.ExecuteScalar(qry);

            if (result == null ||
                result == DBNull.Value)
            {
                return 0;
            }

            return Convert.ToInt32(result);
        }

        // =========================================================
        // SEARCH REQUEST
        // =========================================================

        public async Task<DataTable> SearchByCodeRequest(
            int id,
            int branchId)
        {
            qry =
                "SELECT * " +
                "FROM BOMMAS_TMP " +
                "WHERE CODE=" +
                id +
                " AND BRCD=" +
                branchId;

            return await db.GetDatatable(qry);
        }

        // =========================================================
        // LIST REQUEST
        // =========================================================

        public async Task<DataTable> ListRequest(
            int branchId)
        {
            qry =
                "SELECT " +
                "CODE AS Id, " +
                "BOMNAME AS BOM, " +
                "ALIAS AS Alias " +
                "FROM BOMMAS_TMP " +
                "WHERE BRCD=" +
                branchId +
                " ORDER BY BOMNAME";

            return await db.GetDatatable(qry);
        }

        // =========================================================
        // NORMALIZE Y/N FLAG
        // =========================================================

        private string NormalizeFlag(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "N";
            }

            value =
                value.Trim().ToUpper();

            if (value == "Y" ||
                value == "YES" ||
                value == "TRUE" ||
                value == "1")
            {
                return "Y";
            }

            return "N";
        }

        // =========================================================
        // SQL STRING ESCAPE
        // =========================================================

        private string SqlEscape(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            return value.Replace("'", "''");
        }
    }
}