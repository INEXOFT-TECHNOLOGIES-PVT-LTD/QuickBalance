using QuickBalance.API.CLS;
using System.Data;

namespace QuickBalance.API.DB
{
    public class YvrDB
    {
        private readonly DBConn db;
        public Variable Variables = new Variable();
        public Function Functions = new Function();
        string qry = "";

        public YvrDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public async Task<DataTable> GetVoucherQuery(int AccCode, ClsDate clsDate, string Remarks, string VrNo, string Amount, string VrType, string Exact, bool Unapproved)
        {
            string d = "'";

            string amtQry = Amount == "" ? "" : " AND(YVR.RECEIPT=" + Amount + " OR YVR.PAYMENT=" + Amount + ") ";
            string VrType2 = VrType == "PV" ? "GP" : VrType == "RV" ? "GR" : "";

            qry = "select VRNO,VRDATE,YVR." + Functions.VSDOCNO() + " as INVNOTEXT,VRTYPE,ACCNAME,PAYMENT as Debit,RECEIPT as Credit,DESCRIPTION as Remarks from YVR INNER JOIN ACCMAS ON YVR.CODE=ACCMAS.ID" +
                " WHERE (YVR.CODE=" + AccCode + " OR " + AccCode + "=0)" +
                " AND YVR.VRDATE BETWEEN " + d + clsDate.DateFrom + d + " AND " + d + clsDate.ToDate + d +
                " AND ((iif(YVR.DESCRIPTION is null,'',YVR.DESCRIPTION) LIKE '%" + Remarks + "%' OR '" + Remarks + "'='' ) AND (iif(YVR.DESCRIPTION is null,'',YVR.DESCRIPTION)='" + Remarks + "' OR '" + Exact + @"'=''))" +
                " AND ((YVR." + Functions.VSDOCNO() + " LIKE '%" + VrNo + "%' OR '" + VrNo + "'='' ) AND (YVR." + Functions.VSDOCNO() + "='" + VrNo + "' OR '" + Exact + @"'=''))" +
                " AND ('" + VrType + "'='' OR YVR.VRTYPE='" + VrType + "' OR YVR.VRTYPE='" + VrType2 + "')" + amtQry + Functions.APRVLR(true, Unapproved, "YVR") + Functions.BRSTMNT("YVR", true) + "ORDER BY YVR.VRDATE,YVR." + Functions.VSDOCNO();

            return await db.GetDatatable(qry);
        }
    }
}
