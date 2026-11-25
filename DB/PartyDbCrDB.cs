using QuickBalance.API.CLS;
using System.Data;

namespace QuickBalance.API.DB
{
    public class PartyDbCrDB
    {
        private readonly DBConn db;

        public Variable Variables = new Variable();
        public Function Functions = new Function();

        string qry = "";

        public PartyDbCrDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public async Task<DataTable> SearchByPrCode2(ClsPartyDbCr clsPartyDbCr)
        {
            qry = "SELECT * FROM dbo.PARTYDBCR WHERE PRCODE='" + clsPartyDbCr.PRCODE + "' AND CANCELFLG = 'False'";
            return await db.GetDatatable(qry);
        }
    }
}