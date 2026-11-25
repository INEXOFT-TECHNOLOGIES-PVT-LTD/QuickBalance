using System.Data;

namespace QuickBalance.API.DB
{
    public class BrandMasDB
    {
        private readonly DBConn db;

        public BrandMasDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public async Task<DataTable> ActiveBrandsAll()
        {
            string qry =
                "SELECT ID, BRANDNAME " +
                "FROM BRANDMAS " +
                "ORDER BY BRANDNAME";

            return await db.GetDatatable(qry);
        }
    }
}