using QuickBalance.API.CLS;
using System.Data;

namespace QuickBalance.API.DB
{
    public class SalDDB
    {
        private readonly DBConn db;
        public Variable Variables = new Variable();
        public Function Functions = new Function();
        string qry = "";

        public SalDDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public async Task<DataTable> SearchAddInfo(ClsSalD clsSalD)
        {
            qry = "SELECT CODE,TAX,TAXAMT,SLCODE FROM SALD WHERE VRNO='" + clsSalD.VrNo + "' AND VRTYPE='" + clsSalD.VrType + "' AND DFLAG='" + clsSalD.DFlag + "' AND CODE<>'0' AND ETYPE <> 'D'";
            return await db.GetDatatable(qry);
        }
    }
}
