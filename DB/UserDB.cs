using QuickBalance.API.CLS;
using System.Data;

namespace QuickBalance.API.DB
{
    public class UserDB
    {
        private readonly DBConn db;
        public Variable Variables = new Variable();
        public Function Functions = new Function();
        string qry = "";

        public UserDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public async Task<DataTable> Login(ClsUser clsUser)
        {
            qry = "SELECT * FROM TBUSER WHERE USERNAME = '" + clsUser.UserName + "' AND PSWD = N'" + clsUser.Password + "'";
            return await db.GetDatatable(qry);
        }
    }
}
