using System.Data;
using Microsoft.Data.SqlClient;

namespace QuickBalance.API.DB
{
    public class CurrConvDB
    {
        private readonly DBConn db;

        public CurrConvDB(DBConn db)
        {
            this.db = db;
        }

        private static string SqlEscape(string value)
        {
            return (value ?? "").Replace("'", "''");
        }

        // ---------------------------------------------------------
        // INSERT
        // ---------------------------------------------------------
        public async Task<int> Insert(
            int baseCurrency,
            int alternateCurrency,
            decimal standardRate,
            decimal sellRate,
            decimal buyRate,
            DateTime conversionDate,
            int userId)
        {
            string dateValue = conversionDate.ToString("yyyy-MM-dd");

            string query = $@"
                INSERT INTO CURRCONV
                (
                    BASECURR,
                    ALTCURR,
                    STDRATE,
                    SELLRATE,
                    BUYRATE,
                    TDATE,
                    LOGCD,
                    UTIME
                )
                OUTPUT INSERTED.CODE
                VALUES
                (
                    {baseCurrency},
                    {alternateCurrency},
                    {standardRate.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                    {sellRate.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                    {buyRate.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                    '{dateValue}',
                    {userId},
                    GETDATE()
                )";

            return await db.InsertScalar(query);
        }

        // ---------------------------------------------------------
        // SEARCH BY DATE
        // ---------------------------------------------------------
        public async Task<DataTable> SearchByDate(DateTime conversionDate)
        {
            string dateValue = conversionDate.ToString("yyyy-MM-dd");

            string query = $@"
                SELECT *
                FROM CURRCONV
                WHERE TDATE = '{dateValue}'
                ORDER BY CODE";

            return await db.GetDatatable(query);
        }

        // ---------------------------------------------------------
        // SEARCH BY CODE
        // ---------------------------------------------------------
        public async Task<DataTable> SearchByCode(int code)
        {
            string query = $@"
                SELECT *
                FROM CURRCONV
                WHERE CODE = {code}";

            return await db.GetDatatable(query);
        }

        // ---------------------------------------------------------
        // CHECK WHETHER ANOTHER CONVERSION EXISTS FOR DATE
        // ---------------------------------------------------------
        public async Task<DataTable> CheckExists(
            DateTime conversionDate,
            int excludeCode = 0)
        {
            string dateValue = conversionDate.ToString("yyyy-MM-dd");

            string query;

            if (excludeCode > 0)
            {
                query = $@"
                    SELECT *
                    FROM CURRCONV
                    WHERE TDATE = '{dateValue}'
                    AND CODE <> {excludeCode}";
            }
            else
            {
                query = $@"
                    SELECT *
                    FROM CURRCONV
                    WHERE TDATE = '{dateValue}'";
            }

            return await db.GetDatatable(query);
        }

        // ---------------------------------------------------------
        // DELETE BY DATE
        // ---------------------------------------------------------
        public async Task<int> DeleteByDate(DateTime conversionDate)
        {
            string dateValue = conversionDate.ToString("yyyy-MM-dd");

            string query = $@"
                DELETE FROM CURRCONV
                WHERE TDATE = '{dateValue}'";

            return await db.Execute(query);
        }

        // ---------------------------------------------------------
        // LIST
        // ---------------------------------------------------------
        public async Task<DataTable> List()
        {
            string query = @"
                SELECT
                    CURRCONV.CODE AS Code,
                    CURRCONV.TDATE AS [Date],
                    CURRENCYMAS.SYMBOL AS Symbol,
                    CURRCONV.STDRATE AS [Std Rate],
                    CURRCONV.SELLRATE AS [Sell Rate],
                    CURRCONV.BUYRATE AS [Buy Rate]
                FROM CURRCONV
                INNER JOIN CURRENCYMAS
                    ON CURRCONV.ALTCURR = CURRENCYMAS.CODE
                ORDER BY CURRCONV.TDATE DESC, CURRCONV.CODE DESC";

            return await db.GetDatatable(query);
        }

        // ---------------------------------------------------------
        // GET LATEST CONVERSION FOR ALTERNATE CURRENCY
        // ---------------------------------------------------------
        public async Task<DataTable> Search(
            int alternateCurrency,
            DateTime conversionDate)
        {
            string dateValue = conversionDate.ToString("yyyy-MM-dd");

            string query = $@"
                SELECT *
                FROM CURRCONV
                WHERE ALTCURR = {alternateCurrency}
                AND TDATE <= '{dateValue}'
                ORDER BY TDATE DESC, CODE DESC";

            return await db.GetDatatable(query);
        }
    }
}