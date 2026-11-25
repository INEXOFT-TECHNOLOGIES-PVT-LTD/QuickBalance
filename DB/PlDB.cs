using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Data;

namespace QuickBalance.API.DB
{
    public class PlDB
    {
        private readonly DBConn db;
        public Variable Variables = new Variable();
        public Function Functions = new Function();
        string qry = "";
        string tbl = "TB" + DateTime.Now.ToLongTimeString();

        public PlDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public async Task<int> Drop()
        {
            qry = "DROP TABLE " + tbl + "";
            return await db.Execute(qry);
        }
    }
}
