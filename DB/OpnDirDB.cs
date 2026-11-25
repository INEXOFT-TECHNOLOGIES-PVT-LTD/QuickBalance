using QuickBalance.API.CLS;

namespace QuickBalance.API.DB
{
    public class OpnDirDB
    {
        private readonly DBConn db;
        public Variable Variables = new Variable();
        public Function Functions = new Function();
        string qry = "";

        public OpnDirDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public async Task<decimal> GetOpeningByTypeName(ClsOpnDir clsOpnDir)
        {
            qry = "SELECT SUM(AMOUNT) FROM OPNDIR WHERE TYPENAME = '" + clsOpnDir.TYPENAME + "'" + Functions.BYBRANCH("OPNDIR", true);
            return await db.ReturnDecValue(qry);
        }
    }
}
