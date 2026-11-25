using System.Data;

namespace QuickBalance.API.DB
{
    public class PriceCatMasDB
    {
        private readonly DBConn db;

        public PriceCatMasDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public async Task<DataTable> Categories()
        {
            string qry =
                "SELECT DISTINCT CODE, CATNAME " +
                "FROM PRICECATMAS " +
                "ORDER BY CATNAME";

            return await db.GetDatatable(qry);
        }
    }
}