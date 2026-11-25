using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Tax;
using System.Data;

namespace QuickBalance.API.DB
{
    public class TaxMasDBbkp
    {
        private readonly DBConn db;
        public Variable Variables = new Variable();
        public Function Functions = new Function();
        string qry = "";
        public TaxMasDBbkp(DBConn dbConn)
        {
            db = dbConn;
        }

        // =========================================================
        // INSERT TAX
        // =========================================================
        public async Task<int> Insert(
            TaxMasterRequest request,
            int userId)
        {
            string taxName =
                (request.TaxName ?? "").Replace("'", "''");

            string secondName =
                (request.SecondName ?? "").Replace("'", "''");

            string taxType =
                string.IsNullOrWhiteSpace(request.TaxType)
                    ? "T"
                    : request.TaxType.Replace("'", "''");

            string qry =
                "INSERT INTO TAXMAS " +
                "(TAXNAME,TAXPERC,ACTIVE,LOGCD," +
                "PURCODE,PRTNCODE,SALCODE,SRTNCODE," +
                "UTIME,VATCODE,PTAXCODE,SECNAME,TTYPE) " +
                "OUTPUT INSERTED.CODE " +
                "VALUES(" +
                "'" + taxName + "'," +
                request.TaxPercentage + "," +
                (request.IsActive ? "1" : "0") + "," +
                userId + "," +
                request.PurchaseAccountId + "," +
                request.PurchaseReturnAccountId + "," +
                request.SalesAccountId + "," +
                request.SalesReturnAccountId + "," +
                "GETDATE()," +
                request.VatAccountId + "," +
                request.TaxAccountId + "," +
                "'" + secondName + "'," +
                "'" + taxType + "')";

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
        // UPDATE TAX
        // =========================================================
        public async Task<int> Update(
            int id,
            TaxMasterRequest request,
            int userId)
        {
            string taxName =
                (request.TaxName ?? "").Replace("'", "''");

            string secondName =
                (request.SecondName ?? "").Replace("'", "''");

            string taxType =
                string.IsNullOrWhiteSpace(request.TaxType)
                    ? "T"
                    : request.TaxType.Replace("'", "''");

            string qry =
                "UPDATE TAXMAS SET " +
                "TAXNAME='" + taxName + "'," +
                "TAXPERC=" + request.TaxPercentage + "," +
                "ACTIVE=" + (request.IsActive ? "1" : "0") + "," +
                "LOGCD=" + userId + "," +
                "PURCODE=" + request.PurchaseAccountId + "," +
                "PRTNCODE=" + request.PurchaseReturnAccountId + "," +
                "SALCODE=" + request.SalesAccountId + "," +
                "SRTNCODE=" + request.SalesReturnAccountId + "," +
                "UTIME=GETDATE()," +
                "VATCODE=" + request.VatAccountId + "," +
                "PTAXCODE=" + request.TaxAccountId + "," +
                "SECNAME='" + secondName + "'," +
                "TTYPE='" + taxType + "' " +
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
        // GET ALL TAX DETAILS
        // =========================================================
        public async Task<DataTable> GetAll()
        {
            string qry = "SELECT * " + "FROM TAXMAS " + "ORDER BY TAXNAME";

            return await db.GetDatatable(qry);
        }

        // =========================================================
        // GET TAX BY ID
        // =========================================================
        public async Task<DataTable> GetById(int id)
        {
            string qry = "SELECT * " + "FROM TAXMAS " + "WHERE CODE=" + id;

            return await db.GetDatatable(qry);
        }

        // =========================================================
        // CHECK TAX NAME EXISTS
        // =========================================================
        public async Task<bool> NameExists(string taxName)
        {
            string name =
                (taxName ?? "").Replace("'", "''");

            string qry = "SELECT COUNT(*) " + "FROM TAXMAS " + "WHERE TAXNAME='" + name + "'";

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
        // CHECK TAX NAME EXISTS
        // EXCLUDING CURRENT ID
        // =========================================================
        public async Task<bool> NameExists(
            string taxName,
            int id)
        {
            string name =
                (taxName ?? "").Replace("'", "''");

            string qry = "SELECT COUNT(*) " + "FROM TAXMAS " + "WHERE TAXNAME='" + name + "' " + "AND CODE<>" + id;

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
        // TAX CATEGORY / TAX GROUP
        // =========================================================
        public async Task<DataTable> Search()
        {
            string qry = "SELECT CODE AS Id, TAXNAME AS [Tax Group] " + "FROM TAXMAS " + "ORDER BY TAXNAME";

            return await db.GetDatatable(qry);
        }

        // =========================================================
        // TAX SEARCH BY TEXT
        // =========================================================
        public async Task<DataTable> Search(string text)
        {
            string searchText =
                (text ?? "").Replace("'", "''");

            string qry =
                "SELECT CODE AS Id, TAXNAME AS [Tax Group] " + "FROM TAXMAS " + "WHERE TAXNAME LIKE '%" + searchText + "%' " + "ORDER BY TAXNAME";

            return await db.GetDatatable(qry);
        }

        // =========================================================
        // ACTIVE TAXES FOR ITEM MASTER
        // =========================================================
        public async Task<DataTable> ActiveTaxesForItemMaster()
        {
            string qry =
                "SELECT CODE AS Id, TAXNAME AS [Tax Group] " + "FROM TAXMAS " + "WHERE ACTIVE = 'TRUE' " + "AND TTYPE <> 'E' " + "AND TTYPE <> 'UR' " + "ORDER BY TAXNAME";

            return await db.GetDatatable(qry);
        }

        // =========================================================
        // ACTIVE TAXES
        // =========================================================
        public async Task<DataTable> ActiveTaxes()
        {
            string qry =
                "SELECT CODE AS Id, TAXNAME AS [Tax Group] " + "FROM TAXMAS " + "WHERE ACTIVE = 'TRUE' " + "ORDER BY TAXNAME";
            return await db.GetDatatable(qry);
        }

        // =========================================================
        // ACTIVE ALL TAXES
        // =========================================================
        public async Task<DataTable> ActiveTaxesAll()
        {
            string qry =
                "SELECT CODE AS Id, TAXNAME AS [Tax Group] " + "FROM TAXMAS " + "WHERE ACTIVE = 'TRUE' " + "ORDER BY TAXNAME";

            return await db.GetDatatable(qry);
        }

        // =========================================================
        // TAX PERCENTAGES
        // =========================================================
        public async Task<DataTable> Taxes()
        {
            string qry =
                "SELECT TAXPERC " + "FROM TAXMAS " + "ORDER BY TAXPERC";

            return await db.GetDatatable(qry);
        }

        // =========================================================
        // GET TAX DETAILS BY PERCENTAGE
        // =========================================================
        public async Task<DataTable> TaxDetailsByPerc(
            decimal taxPerc)
        {
            string qry =
                "SELECT * " + "FROM TAXMAS " + "WHERE TAXPERC = " + taxPerc + " AND ACTIVE = 'TRUE'";

            return await db.GetDatatable(qry);
        }

        // =========================================================
        // GET TAX DETAILS BY NAME AND PERCENTAGE
        // =========================================================
        public async Task<DataTable> TaxDetailsByNameAndPerc(
            string taxName,
            decimal taxPerc)
        {
            string name =
                (taxName ?? "").Replace("'", "''");

            string qry =
                "SELECT * " + "FROM TAXMAS " + "WHERE TAXPERC = " + taxPerc + " AND TAXNAME = '" + name + "'";

            return await db.GetDatatable(qry);
        }

        // =========================================================
        // ACTIVE TAXES BY TYPE
        // =========================================================
        public async Task<DataTable> ActiveTaxesByType(
            string taxType)
        {
            string type =
                (taxType ?? "").Replace("'", "''");

            string qry =
                "SELECT * " + "FROM TAXMAS " + "WHERE ACTIVE = 'TRUE' " + "AND TTYPE = '" + type + "' " + "ORDER BY TAXNAME";

            return await db.GetDatatable(qry);
        }

        // =========================================================
        // VALIDATE ACCOUNT
        // =========================================================
        public async Task<DataTable> ValidateAcc(
            int accountId)
        {
            string qry =
                "SELECT * " + "FROM TAXMAS " + "WHERE PURCODE = " + accountId + " OR PRTNCODE = " + accountId + " OR SALCODE = " + accountId + " OR SRTNCODE = " + accountId + " OR VATCODE = " + accountId + " OR PTAXCODE = " + accountId;

            return await db.GetDatatable(qry);
        }

        // =========================================================
        // GET EXEMPTED TAX ID
        // =========================================================
        public async Task<int> GetIDExemptedTax()
        {
            string qry =
                "SELECT CODE " + "FROM TAXMAS " + "WHERE TAXNAME = 'Exempted'";

            DataTable dt =
                await db.GetDatatable(qry);

            if (dt.Rows.Count > 0 &&
                dt.Rows[0]["CODE"] != DBNull.Value)
            {
                return Convert.ToInt32(
                    dt.Rows[0]["CODE"]);
            }

            return 0;
        }

        // =========================================================
        // UPDATE DEFAULT TAX
        // =========================================================
        public async Task<int> UpdateDefaultTax(
            int id,
            string taxName,
            decimal percentage)
        {
            string name =
                (taxName ?? "").Replace("'", "''");

            string qry =
                "UPDATE TAXMAS SET " + "TAXNAME='" + name + "'," + "TAXPERC=" + percentage + " " + "WHERE CODE=" + id + "; " + "SELECT @@ROWCOUNT;";

            object? result =
                await db.ExecuteScalar(qry);

            if (result == null ||
                result == DBNull.Value)
            {
                return 0;
            }

            return Convert.ToInt32(result);
        }
        public async Task<DataTable> SearchByID(ClsTaxMas clsTaxMas)
        {
          string  qry = "SELECT * FROM TAXMAS WHERE CODE='" + clsTaxMas.Code + "'";
            return await db.GetDatatable(qry);
        }
    }
}


//using QuickBalance.API.CLS;
//using System.Data;

//namespace QuickBalance.API.DB
//{
//    public class TaxMasDB
//    {
//        private readonly DBConn db;
//        public Variable Variables = new Variable();
//        public Function Functions = new Function();
//        string qry = "";

//        public TaxMasDB(DBConn dbConn)
//        {
//            db = dbConn;
//        }

//public async Task<DataTable> SearchByID(ClsTaxMas clsTaxMas)
//{
//    qry = "SELECT * FROM TAXMAS WHERE CODE='" + clsTaxMas.Code + "'";
//    return await db.GetDatatable(qry);
//}
//    }
//}
