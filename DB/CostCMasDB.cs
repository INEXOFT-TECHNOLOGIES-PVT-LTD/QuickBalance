using System.Data;

namespace QuickBalance.API.DB
{
    public class CostCMasDB
    {
        private readonly DBConn db;
        public Variable Variables = new Variable();
        public Function Functions = new Function();
        string qry = "";

        public CostCMasDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public async Task<DataTable> GetOpening()
        {
            qry = "SELECT * FROM " + Functions.VRSYS_ALLOWBR_COSTCMAS() + "COSTCMAS WHERE OPNBAL <> 0" + Functions.VRSYS_ALLOWBR_REPORT(true, "COSTCMAS", true);
            return await db.GetDatatable(qry);
        }
    }
}
