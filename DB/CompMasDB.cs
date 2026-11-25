using QuickBalance.API.CLS;
using System.Data;

namespace QuickBalance.API.DB
{
    public class CompMasDB
    {
        private readonly DBConn dbMSql;
        public Variable Variables = new Variable();
        public Function Functions = new Function();
        string qry = "";

        public CompMasDB(DBConn dbConn)
        {
            dbMSql = dbConn;
        }

        public async Task<DataTable> List(ClsCompMas clsCompMas)
        {
            qry = "SELECT ID, COMPNAME FROM COMPMAS WHERE ACTIVE='" + clsCompMas.Active + "' ORDER BY COMPNAME";
            return await dbMSql.GetDatatable(qry);
        }
    }
}
