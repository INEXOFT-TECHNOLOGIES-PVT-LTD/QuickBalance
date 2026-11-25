using QuickBalance.API.CLS;
using System.Data;

namespace QuickBalance.API.DB
{
    public class SntStkDB
    {
        private readonly DBConn db;
        public Variable Variables = new Variable();
        public Function Functions = new Function();
        string qry = "";

        public SntStkDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public async Task<DataTable> SearchForAdjust(ClsSntStk clsSntStk)
        {
            qry = @"SELECT SNTSTK.INVRTYPE,SNTSTK.INVRNO,SNTSTK.INORDNO,SNTSTK.INSLNO,SNTSTK.ITEMSLNO,IIF(DTLS.VRDATE IS NULL,'',FORMAT(DTLS.VRDATE,'" + Variables.format + @"')) AS VRDATE FROM SNTSTK LEFT JOIN (

            SELECT 'PRD' AS INVRTYPE,PRDNT.ORDNO AS INORDNO,PRDNT.SNTSLNO AS INSLNO,PRDNM.VRDATE FROM PRDNM INNER JOIN PRDNT ON PRDNM.ORDNO = PRDNT.ORDNO WHERE PRDNT.PRCODE = " + clsSntStk.PRCODE + @" AND PRDNT.TYPE='G'
            UNION ALL
            SELECT 'ADJ' AS INVRTYPE,PURT.ORDNO AS INORDNO,PURT.SNTSLNO AS INSLNO,PURT.PURDATE AS VRDATE FROM PURT WHERE PURT.PRCODE = " + clsSntStk.PRCODE + @" AND PURT.VRTYPE='A'
            UNION ALL
            SELECT 'ST' AS INVRTYPE,PURT.ORDNO AS INORDNO,PURT.SNTSLNO AS INSLNO,PURT.PURDATE AS VRDATE FROM PURT WHERE PURT.PRCODE = " + clsSntStk.PRCODE + @" AND PURT.VRTYPE='T' AND PURT.STKFLG='R'
            UNION ALL
            SELECT 'PHY' AS INVRTYPE,PURT.ORDNO AS INORDNO,PURT.SNTSLNO AS INSLNO,PURT.PURDATE AS VRDATE FROM PURT WHERE PURT.PRCODE = " + clsSntStk.PRCODE + @" AND PURT.VRTYPE='PHY' AND PURT.STKFLG='R'
            UNION ALL
            SELECT 'RN' AS INVRTYPE,GRNT.ORDNO AS INORDNO,GRNT.SNTSLNO AS INSLNO,GRNT.PURDATE AS VRDATE FROM GRNT WHERE GRNT.PRCODE = " + clsSntStk.PRCODE + @" AND GRNT.VRTYPE='RN'
            UNION ALL
            SELECT 'PI' AS INVRTYPE,PURT.ORDNO AS INORDNO,PURT.SNTSLNO AS INSLNO,PURT.PURDATE AS VRDATE FROM PURT WHERE PURT.PRCODE = " + clsSntStk.PRCODE + @" AND PURT.VRTYPE='P'
            UNION ALL
            SELECT 'SR' AS INVRTYPE,SALT.ORDNO AS INORDNO,SALT.SNTSLNO AS INSLNO,SALT.INVDATE AS VRDATE FROM SALT WHERE SALT.PRCODE = " + clsSntStk.PRCODE + @" AND SALT.STYPE='R' ) DTLS ON SNTSTK.INVRTYPE = DTLS.INVRTYPE AND SNTSTK.INORDNO = DTLS.INORDNO AND SNTSTK.INSLNO = DTLS.INSLNO
            WHERE SNTSTK.PRCODE = " + clsSntStk.PRCODE + " AND SNTSTK.GDWCODE = " + clsSntStk.GDWCODE + " AND SNTSTK.UNITCODE = " + clsSntStk.UNITCODE + " AND SNTSTK.USED = 'FALSE'" + (Variables.BranchYN ? " AND IIF(SNTSTK.BRCD IS NULL,0,SNTSTK.BRCD)=" + Variables.VRSYS_DEFBRANCH : "") + " ORDER BY IIF(DTLS.INVRTYPE IS NULL,0,1),DTLS.VRDATE,SNTSTK.ITEMSLNO";
            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> GetByVoucherOut(ClsSntStk clsSntStk)
        {
            qry = "SELECT INVRTYPE,INVRNO,INORDNO,INSLNO,PRCODE,OUTSLNO,GDWCODE,UNITCODE,ITEMSLNO FROM SNTSTK WHERE OUTVRTYPE = '" + clsSntStk.OUTVRTYPE + "' AND OUTVRNO = " + clsSntStk.OUTVRNO + " AND OUTORDNO = " + clsSntStk.OUTORDNO + "";
            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> GetByVoucherIn(ClsSntStk clsSntStk)
        {
            qry = "SELECT PRCODE,INSLNO,ITEMSLNO FROM SNTSTK WHERE INVRTYPE = '" + clsSntStk.INVRTYPE + "' AND INORDNO = " + clsSntStk.INORDNO + " AND INVRNO = " + clsSntStk.INVRNO + "";
            return await db.GetDatatable(qry);
        }

       


        public async Task<DataTable> SearchByPrCode(ClsSntStk clsSntStk)
        {
            qry = "SELECT * FROM dbo.SNTSTK WHERE PRCODE='" + clsSntStk.PRCODE + "'";
            return await db.GetDatatable(qry);
        }
    }
}

