using QuickBalance.API.CLS;
using System.Data;

namespace QuickBalance.API.DB
{
    public class PrstDB
    {
        private readonly DBConn db;
        public Variable Variables = new Variable();
        public Function Functions = new Function();
        string qry = "";

        public PrstDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public async Task<DataTable> PendingList(ClsPrst clsPrst, bool EXP_YN)
        {
            qry = "SELECT BATCH, Format(CONVMRP,'" + Variables.DecString + "') AS SALPRICE, Format(CONVPRATE,'" + Variables.DecString + "') As PURPRICE,Format(PRST.CONVRATE,'" + Variables.DecString + "') AS MRP, Format(BALQTY,'" + Variables.QtyDecString + "') AS BALQTY, IIF(EXPCHECK = 'TRUE', Format(MFGDATE,'" + Variables.format + "'),'') AS MFGDATE, IIF(EXPCHECK = 'TRUE',Format(EXPIRY,'" + Variables.format + "'),'') AS EXPIRY FROM PRST WHERE PRCODE='" + clsPrst.PrCode + "' AND GRACK='" + clsPrst.GRack + "' AND BALQTY > '" + 0 + "'" + (Variables.BranchYN ? " AND IIF(BRCD IS NULL, 0, BRCD) = " + Variables.VRSYS_DEFBRANCH : "") + (EXP_YN ? " ORDER BY PRST.EXPIRY " : "");
            return await db.GetDatatable(qry);
        }


        public async Task<DataTable> SearchByPrCode(ClsPrst clsPrst)
        {
            qry = "SELECT * FROM dbo.PRST WHERE PRCODE='" + clsPrst.PrCode + "'";
            return await db.GetDatatable(qry);
        }
    }
}
