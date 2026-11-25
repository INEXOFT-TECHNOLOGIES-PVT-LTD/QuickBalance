using QuickBalance.API.CLS;
using System.Data;

namespace QuickBalance.API.DB
{
    public class PrGrpMasDB
    {
        private readonly DBConn db;
        public Variable Variables = new Variable();
        public Function Functions = new Function();
        string qry = "";

        public PrGrpMasDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public async Task<DataTable> StockAccDetails(ClsPrGrpMas clsPrGrpMas)
        {
            qry = "SELECT PRGRPMAS.PRGRPID, PRGRPMAS.GRPNAME, ACCMAS.ID AS ACCID, ACCMAS.ACCNAME FROM    (ACCMAS INNER JOIN  PRGRPMAS ON ACCMAS.ID = PRGRPMAS.STOCKACCID) WHERE PRGRPMAS.PRGRPID='" + clsPrGrpMas.PrGrpId + "' ";
            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> ActiveGroups()
        {
            string qry =
                "SELECT PRGRPID, GRPNAME " +
                "FROM PRGRPMAS " +
                "WHERE ACTIVE = 'TRUE' " +
                "ORDER BY GRPNAME";

            return await db.GetDatatable(qry);
        }
    }
}
