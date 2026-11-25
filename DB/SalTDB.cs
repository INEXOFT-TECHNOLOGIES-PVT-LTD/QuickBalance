using QuickBalance.API.CLS;
using System.Data;

namespace QuickBalance.API.DB
{
    public class SalTDB
    {
        private readonly DBConn db;
        public Variable Variables = new Variable();
        public Function Functions = new Function();
        string qry = "";

        public SalTDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public async Task<DataTable> SearchByInvno(ClsSalT clsSalT)
        {
            qry = "SELECT PRMAST.PRNAME,PRMAST.ALIAS,PRMAST.SECNAME AS SECNAME,PRMAST.BMEQN,PRUNITMAS.PRUNITNAME,PRUNITMAS.SECNAME AS UNITSECNAME,PRMAST.UNIT,PRMAST.ALTUNIT,PRMAST.SALESPRICETYPE,  SALT.* FROM((PRMAST INNER JOIN SALT ON PRMAST.PRCODE = SALT.PRCODE) LEFT JOIN PRUNITMAS ON SALT.UNITCODE = PRUNITMAS.PRUNITID) WHERE INVNO='" + clsSalT.InvNo + "'";
            return await db.GetDatatable(qry);
        }
    }
}
