using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using System.Data;

namespace QuickBalance.API.DB
{
    public class StkDtDB
    {
        private readonly DBConn db;
        public Variable Variables = new Variable();
        public Function Functions = new Function();
        string qry = "";
        string PL_table = "";
        private string tb = "";
        private string StkEntryOrder = "";

        public StkDtDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public async Task<int> CreateTBForPL(ClsStkDt clsStkDt, ClsDate clsDate, bool Unapproved = true)
        {
            PL_table = "ST_" + DateTime.Now.Hour.ToString() + "_" + DateTime.Now.Minute.ToString() + "_" + DateTime.Now.Second.ToString();

            qry = @"SELECT DISTINCT PRCODE,SUM(RNQTY) AS RNQTY ,SUM(PRQTY) AS PRQTY,SUM(DNQTY) AS DNQTY,SUM(SLQTY) AS SLQTY,SUM(PRTQTY) AS PRTQTY,SUM(SRTQTY) AS SRTQTY,SUM(ADRQTY) AS ADRQTY,SUM(ADIQTY) AS ADIQTY,SUM(PRDIQTY) AS PRDIQTY,SUM(PRDRQTY) AS PRDRQTY,SUM(AVGRATE) AS AVGRATE,SUM(AVGGRNRATE) AS AVGGRNRATE,MAX(PRPRICE) AS PRPRICE,MAX(OPNSTKVAL) AS OPNSTKVAL,PURCODE,SALCODE INTO " + PL_table + @"
            FROM(SELECT PRMAST.PRCODE, SUM(IIF((STKDT.VRTYPE = 'NT' AND STKDT.TYPE = 'R'), STKDT.CONVQTY, 0))AS RNQTY, SUM(IIF((STKDT.VRTYPE = 'TR' AND STKDT.TYPE = 'R'), STKDT.CONVQTY, 0))AS PRQTY, SUM(IIF((STKDT.VRTYPE = 'NT' AND STKDT.TYPE = 'I'), STKDT.CONVQTY, 0))AS DNQTY, SUM(IIF((STKDT.VRTYPE = 'TR' AND STKDT.TYPE = 'I') , STKDT.CONVQTY, 0))AS SLQTY, SUM(IIF((STKDT.VRTYPE = 'RET' AND STKDT.TYPE = 'I'), STKDT.CONVQTY, 0))AS PRTQTY, SUM(IIF((STKDT.VRTYPE = 'RET' AND STKDT.TYPE = 'R'), STKDT.CONVQTY, 0))AS SRTQTY, SUM(IIF((STKDT.VRTYPE = 'AD' AND STKDT.TYPE = 'R'), STKDT.CONVQTY, 0))AS ADRQTY, SUM(IIF((STKDT.VRTYPE = 'AD' AND STKDT.TYPE = 'I'), STKDT.CONVQTY, 0))AS ADIQTY, SUM(IIF((STKDT.VRTYPE = 'PR' AND STKDT.TYPE = 'I'), STKDT.CONVQTY, 0))AS PRDIQTY, SUM(IIF((STKDT.VRTYPE = 'PR' AND STKDT.TYPE = 'R'), STKDT.CONVQTY, 0))AS PRDRQTY, IIF(STKDT.VRTYPE = 'TR' AND STKDT.TYPE = 'R', AVG(STKDT.RATE), 0)AS AVGRATE, IIF(STKDT.VRTYPE = 'NT' AND STKDT.TYPE = 'R', AVG(STKDT.RATE), 0)AS AVGGRNRATE, IIF(PRMAST.PURPRICETYPE = 1, PRMAST.ALTPURPRICE, PRMAST.BASEPURPRICE) AS PRPRICE, IIF(SUM(OPNSTK.CONVVALUE) IS NULL,0, SUM(OPNSTK.CONVVALUE)) AS OPNSTKVAL,TAXMAS.PURCODE,TAXMAS.SALCODE
            FROM((((PRMAST LEFT JOIN " + Functions.VRSYS_ALLOWBR_OPNSTK(Unapproved: Unapproved) + "OPNSTK ON PRMAST.PRCODE=OPNSTK.PRCODE) LEFT JOIN " + Functions.ADDCOST_STKDT(Unapproved: Unapproved) + "STKDT  ON  PRMAST.PRCODE = STKDT.PRCODE)) LEFT JOIN TAXMAS ON PRMAST.TAXCODE = TAXMAS.CODE  )  WHERE STKDT.VRDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "' AND (STKDT.VRTYPE <> 'NT' OR STKDT.TYPE <> 'I' OR CLOSES = 'FALSE') GROUP BY PRMAST.PRCODE,PRMAST.BASEPURPRICE,PRMAST.ALTPURPRICE,PRMAST.PURPRICETYPE,TAXMAS.PURCODE,TAXMAS.SALCODE ,STKDT.VRTYPE, STKDT.TYPE) T GROUP BY T.PRCODE,T.PURCODE,T.SALCODE";
            return await db.Execute(qry);
        }

        public async Task<DataTable> GetSalQtyDiffByFOrder(string FOrder)
        {
            qry = "SELECT DISTINCT " + PL_table + ".PRCODE,PRMAST.COSTMETH,IIF(PURPRICETYPE=0,BASEPURPRICE,ALTPURPRICE) AS PURPRICE,SUM(" + PL_table + ".SLQTY) AS SLQTY,SUM(" + PL_table + ".DNQTY) AS DNQTY,SUM(" + PL_table + ".SLQTY-" + PL_table + ".DNQTY) AS DIFQTY FROM ((ACCMAS INNER JOIN GRPLDG ON ACCMAS.MGROUP=GRPLDG.GROUPID) INNER JOIN  " + PL_table + " ON ACCMAS.ID = " + PL_table + ".SALCODE) INNER JOIN PRMAST ON " + PL_table + ".PRCODE=PRMAST.PRCODE WHERE (GRPLDG.FORDER='" + FOrder + "' AND " + PL_table + ".SLQTY<>" + PL_table + ".DNQTY)  GROUP BY " + PL_table + ".PRCODE,PRMAST.COSTMETH,PRMAST.BASEPURPRICE,PRMAST.ALTPURPRICE,PRMAST.PURPRICETYPE";
            return await db.GetDatatable(qry);
        }

        public async Task<int> DropTBForPL()
        {
            qry = "DROP TABLE " + PL_table;
            return await db.Execute(qry);
        }

        public async Task<int> UpdateConvFact()
        {
            qry = "UPDATE STKDT SET CONVFACT='1' WHERE CONVFACT='0'";
            return await db.Execute(qry);
        }

        public async Task<decimal> GetPurDiffByFOrderAndDate(string FOrder, ClsDate clsDate, bool Unapproved = true)
        {
            qry = @"SELECT SUM(GDCOME) AS GDCOME FROM( SELECT SUM(GDCOME) AS GDCOME,SUM(QTY) AS QTY FROM( SELECT SUM( IIF( STKDT.VRTYPE='TR',( STKDT.CONVQTY* STKDT.CONVRATE),0))-SUM( IIF( STKDT.VRTYPE='NT',( STKDT.CONVQTY* STKDT.CONVRATE),0) ) AS GDCOME ,SUM( IIF( STKDT.VRTYPE='TR',( STKDT.CONVQTY),0))-SUM( IIF( STKDT.VRTYPE='NT',( STKDT.CONVQTY),0) ) AS QTY 
            FROM (( ((" + Functions.ADDCOST_STKDT(Unapproved: Unapproved) + @"STKDT INNER JOIN PRMAST ON STKDT.PRCODE = PRMAST.PRCODE) INNER JOIN PRGRPMAS ON PRMAST.GROUPCODE = PRGRPMAS.PRGRPID)) INNER JOIN ACCMAS ON ACCMAS.ID=PRMAST.PURACCODE) INNER JOIN GRPLDG ON ACCMAS.MGROUP=GRPLDG.GROUPID
            WHERE STKDT.TYPE='R' AND STKDT.VRDATE  BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "' AND PRMAST.PURACCODE<>0 AND GRPLDG.FORDER='" + FOrder + @"' AND STKDT.QTY <> '0'

            UNION ALL SELECT SUM( IIF( STKDT.VRTYPE='TR',( STKDT.CONVQTY* STKDT.CONVRATE),0))-SUM( IIF( STKDT.VRTYPE='NT',( STKDT.CONVQTY* STKDT.CONVRATE),0) ) AS GDCOME ,SUM( IIF( STKDT.VRTYPE='TR',( STKDT.CONVQTY),0))-SUM( IIF( STKDT.VRTYPE='NT',( STKDT.CONVQTY),0) ) AS QTY 
            FROM ((((" + Functions.ADDCOST_STKDT(Unapproved: Unapproved) + @"STKDT INNER JOIN PRMAST ON STKDT.PRCODE = PRMAST.PRCODE) INNER JOIN PRGRPMAS ON PRMAST.GROUPCODE = PRGRPMAS.PRGRPID)) INNER JOIN ACCMAS ON ACCMAS.ID=PRGRPMAS.PURACCID ) INNER JOIN GRPLDG ON ACCMAS.MGROUP=GRPLDG.GROUPID         
            WHERE STKDT.TYPE='R' AND STKDT.VRDATE  BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "' AND PRMAST.PURACCODE=0  AND GRPLDG.FORDER='" + FOrder + "' AND STKDT.QTY <> '0') AS T ) AS F"; //  WHERE QTY<>0
            return await db.ReturnDecValue(qry);
        }

        public async Task<decimal> GetPurDiffByGroupAndDate(int Group, ClsDate clsDate, bool Unapproved = true)
        {
            qry = @"SELECT SUM(GDCOME) AS GDCOME FROM( SELECT SUM( IIF( STKDT.VRTYPE='TR',( STKDT.CONVQTY* STKDT.CONVRATE),0))-SUM( IIF( STKDT.VRTYPE='NT',( STKDT.CONVQTY* STKDT.CONVRATE),0) ) AS GDCOME ,SUM( IIF( STKDT.VRTYPE='TR',( STKDT.CONVQTY),0))-SUM( IIF( STKDT.VRTYPE='NT',( STKDT.CONVQTY),0) ) AS QTYDIFF
            FROM (((" + Functions.ADDCOST_STKDT(Unapproved: Unapproved) + @"STKDT INNER JOIN PRMAST ON STKDT.PRCODE = PRMAST.PRCODE) INNER JOIN PRGRPMAS ON PRMAST.GROUPCODE = PRGRPMAS.PRGRPID)) INNER JOIN ACCMAS ON ACCMAS.ID=PRMAST.PURACCODE
            WHERE STKDT.TYPE='R' AND STKDT.VRDATE  BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "' AND PRMAST.PURACCODE<>0 AND ACCMAS.MGROUP='" + Group + @"' AND STKDT.QTY <> '0' GROUP BY STKDT.PRCODE

            UNION ALL SELECT SUM( IIF( STKDT.VRTYPE='TR',( STKDT.CONVQTY* STKDT.CONVRATE),0))-SUM( IIF( STKDT.VRTYPE='NT',( STKDT.CONVQTY* STKDT.CONVRATE),0) ) AS GDCOME ,SUM( IIF( STKDT.VRTYPE='TR',( STKDT.CONVQTY),0))-SUM( IIF( STKDT.VRTYPE='NT',( STKDT.CONVQTY),0) )  AS QTYDIFF
            FROM (((" + Functions.ADDCOST_STKDT(Unapproved: Unapproved) + @"STKDT INNER JOIN PRMAST ON STKDT.PRCODE = PRMAST.PRCODE) INNER JOIN PRGRPMAS ON PRMAST.GROUPCODE = PRGRPMAS.PRGRPID)) INNER JOIN ACCMAS ON ACCMAS.ID=PRGRPMAS.PURACCID           
            WHERE STKDT.TYPE='R' AND STKDT.VRDATE  BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "' AND PRMAST.PURACCODE=0 AND ACCMAS.MGROUP='" + Group + "' AND STKDT.QTY <> '0' GROUP BY STKDT.PRCODE) AS T "; // Where QTYDIFF<>'0'

            return await db.ReturnDecValue(qry);
        }

        public async Task<DataTable> SearchByVrNo(ClsStkDt clsStkDt)
        {
            if (clsStkDt.VrType == "TR")
                qry = "SELECT NTNO FROM STKDT WHERE VRTYPE='" + clsStkDt.VrType + "' AND TYPE='" + clsStkDt.Type + "' AND TRNNO='" + clsStkDt.TrnNo + "' AND PRCODE='" + clsStkDt.PrCode + "'";
            else
                qry = "SELECT TRNNO FROM STKDT WHERE VRTYPE='" + clsStkDt.VrType + "' AND TYPE='" + clsStkDt.Type + "' AND NTNO='" + clsStkDt.NtNo + "' AND PRCODE='" + clsStkDt.PrCode + "'";

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // Create Stk Entry Order
        // ============================================================

        public async Task CreateStkEntryOrder(string TYPE, string VRTYPE)
        {
            StkEntryOrder =
                "STKENTRYORDER_" +
                DateTime.Now.ToString("HHmmssfff") + "_" +
                Guid.NewGuid().ToString("N").Substring(0, 8);

            qry =
                "SELECT PRCODE, ORDNO, NTNO, TRNNO INTO " +
                StkEntryOrder +
                " FROM STKDT WHERE 1 = 0";

            await db.Execute(qry);

            qry =
                "ALTER TABLE " +
                StkEntryOrder +
                " ADD ENTRYID INT IDENTITY(1,1)";

            await db.Execute(qry);

            qry =
                "INSERT INTO " +
                StkEntryOrder +
                "(PRCODE, ORDNO, NTNO, TRNNO) " +
                "SELECT PRCODE, ORDNO, NTNO, TRNNO " +
                "FROM STKDT " +
                "WHERE TYPE = '" + TYPE +
                "' AND VRTYPE = '" + VRTYPE + "'";

            await db.Execute(qry);
        }

        public async Task<int> DropStkEntryOrder()
        {
            int R = 0;

            qry = "DROP TABLE " + StkEntryOrder;

            R = await db.Execute(qry);



            return R;
        }

        public async Task<DataTable> SearchByPrCode(ClsStkDt clsStkDt)
        {
            qry = "SELECT * FROM dbo.STKDT WHERE PRCODE='" + clsStkDt.PrCode + "'";

            return await db.GetDatatable(qry);
        }



    }
}
