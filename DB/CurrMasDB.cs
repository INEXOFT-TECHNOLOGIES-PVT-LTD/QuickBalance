using QuickBalance.API.CLS;
using System.Data;

namespace QuickBalance.API.DB
{
    public class CurrMasDB
    {
        private readonly DBConn db;
        public Variable Variables = new Variable();
        public Function Functions = new Function();
        string qry = "";

        public CurrMasDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public async Task<DataTable> SearchByCode(ClsCurrMas clsCurrMas)
        {
            qry = "SELECT * FROM CURRENCYMAS WHERE CODE='" + clsCurrMas.Code + "'";
            return await db.GetDatatable(qry);
        }

        //public async Task<DataTable> CurrencyByBaseFlag(
        //   ClsCurrMas clsCurrMas)
        //{
        //    string baseFlag = clsCurrMas.BaseFlg ? "1" : "0";

        //    qry =
        //        "SELECT * FROM CURRENCYMAS " +
        //        "WHERE BASEFLG=" + baseFlag +
        //        " ORDER BY CURRSTRING";

        //    return await db.GetDatatable(qry);
        //}

        // =========================================================
        // INSERT
        // =========================================================
        public async Task<int> Insert(ClsCurrMas clsCurrMas)
        {
            string symbol =
                (clsCurrMas.Symbol ?? "").Replace("'", "''");

            string currString =
                (clsCurrMas.CurrString ?? "").Replace("'", "''");

            string subString =
                (clsCurrMas.SubString ?? "").Replace("'", "''");

            string secSymbol =
                (clsCurrMas.SecSymbol ?? "").Replace("'", "''");

            string secCurrString =
                (clsCurrMas.SecCurrString ?? "").Replace("'", "''");

            string secSubString =
                (clsCurrMas.SecSubString ?? "").Replace("'", "''");

            qry =
                "INSERT INTO CURRENCYMAS " +
                "(SYMBOL,CURRSTRING,[SUBSTRING],CONVMODE,BASEFLG," +
                "SECSYMBOL,SECCURSTRNG,SECSUBSTRNG) " +
                "OUTPUT INSERTED.CODE " +
                "VALUES(" +
                "N'" + symbol + "'," +
                "N'" + currString + "'," +
                "N'" + subString + "'," +
                clsCurrMas.ConvMode + "," +
                "0," +
                "N'" + secSymbol + "'," +
                "N'" + secCurrString + "'," +
                "N'" + secSubString + "')";

            return await db.InsertScalar(qry);
        }
        // =========================================================
        // UPDATE
        // =========================================================
        public async Task<int> Update(ClsCurrMas clsCurrMas)
        {
            string symbol =
                (clsCurrMas.Symbol ?? "").Replace("'", "''");

            string currString =
                (clsCurrMas.CurrString ?? "").Replace("'", "''");

            string subString =
                (clsCurrMas.SubString ?? "").Replace("'", "''");

            string secSymbol =
                (clsCurrMas.SecSymbol ?? "").Replace("'", "''");

            string secCurrString =
                (clsCurrMas.SecCurrString ?? "").Replace("'", "''");

            string secSubString =
                (clsCurrMas.SecSubString ?? "").Replace("'", "''");

            qry =
                "UPDATE CURRENCYMAS SET " +
                "SYMBOL=N'" + symbol + "'," +
                "CURRSTRING=N'" + currString + "'," +
                "[SUBSTRING]=N'" + subString + "'," +
                "CONVMODE=" + clsCurrMas.ConvMode + "," +
                "SECSYMBOL=N'" + secSymbol + "'," +
                "SECCURSTRNG=N'" + secCurrString + "'," +
                "SECSUBSTRNG=N'" + secSubString + "' " +
                "WHERE CODE=" + clsCurrMas.Code;

            return await db.Execute(qry);
        }

        // =========================================================
        // GET CURRENCIES BY BASE FLAG
        // =========================================================
        public async Task<DataTable> CurrencyByBaseFlag(
            ClsCurrMas clsCurrMas)
        {
            string baseFlag = clsCurrMas.BaseFlg ? "1" : "0";

            qry =
                "SELECT * FROM CURRENCYMAS " +
                "WHERE BASEFLG=" + baseFlag +
                " ORDER BY CURRSTRING";

            return await db.GetDatatable(qry);
        }

        // =========================================================
        // ALL CURRENCIES
        // =========================================================
        public async Task<DataTable> AllCurrencies(
    string? search = null)
        {
            string searchCondition = "";

            if (!string.IsNullOrWhiteSpace(search))
            {
                string value = search.Replace("'", "''");

                searchCondition =
                    " WHERE SYMBOL LIKE N'%" + value + "%' " +
                    " OR CURRSTRING LIKE N'%" + value + "%' " +
                    " OR SECSYMBOL LIKE N'%" + value + "%' ";
            }

            qry =
                "SELECT * " +
                "FROM CURRENCYMAS " +
                searchCondition +
                " ORDER BY CURRSTRING";

            return await db.GetDatatable(qry);
        }

        // =========================================================
        // CURRENCIES FOR REPORT
        // =========================================================
        public async Task<DataTable> CurrenciesRpt(int currentCurrencyCode)
        {
            qry =
                "SELECT * FROM CURRENCYMAS " +
                "WHERE CODE<>" + currentCurrencyCode +
                " ORDER BY CURRSTRING";

            return await db.GetDatatable(qry);
        }

        // =========================================================
        // LIST
        // =========================================================
        public async Task<DataTable> List()
        {
            qry =
                "SELECT " +
                "CODE AS Id," +
                "CURRSTRING AS [Currency String]," +
                "SYMBOL AS [Currency]," +
                "[SUBSTRING] AS [Sub String] " +
                "FROM CURRENCYMAS " +
                "ORDER BY CURRSTRING";

            return await db.GetDatatable(qry);
        }

        // =========================================================
        // GET MAX CODE
        // =========================================================
        public async Task<int> GetMaxCode()
        {
            return await db.GetnewId("CODE", "CURRENCYMAS");
        }

        // =========================================================
        // DELETE BY CODE
        // =========================================================
        public async Task<int> Delete(int code)
        {
            qry =
                "DELETE FROM CURRENCYMAS " +
                "WHERE CODE=" + code;

            return await db.Execute(qry);
        }

        // =========================================================
        // VALIDATE SYMBOL FOR UPDATE
        // =========================================================
        public async Task<bool> ValidateSymbolForUpdate(
            ClsCurrMas clsCurrMas)
        {
            qry =
                "SELECT * FROM CURRENCYMAS " +
                "WHERE SYMBOL='" +
                clsCurrMas.Symbol.Replace("'", "''") +
                "' AND CODE<>'" +
                clsCurrMas.Code +
                "'";

            DataTable dt = await db.GetDatatable(qry);

            return dt.Rows.Count == 0;
        }


        // =========================================================
        // SEARCH BY SYMBOL
        // =========================================================
        public async Task<DataTable> SearchBySymbol(ClsCurrMas clsCurrMas)
        {
            qry = "SELECT * FROM CURRENCYMAS WHERE SYMBOL='" +
                  clsCurrMas.Symbol.Replace("'", "''") + "'";

            return await db.GetDatatable(qry);
        }
    }
}
