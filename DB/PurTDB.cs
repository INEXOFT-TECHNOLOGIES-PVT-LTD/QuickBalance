using System.Data;
using QuickBalance.API.CLS;

namespace QuickBalance.API.DB
{
    public class PurTDB
    {
        DBConn db;
        string qry = "";

        public Variable Variables = new Variable();
        public Function Functions = new Function();

        string tbVAT = "";

        public PurTDB(DBConn db)
        {
            this.db = db;
        }

        public async Task<DataTable> CreateForVATReport(ClsPurT clsPurT, ClsDate clsDate, bool Unapproved)
        {
            tbVAT = "TBVATP_" + DateTime.Now.Hour.ToString() + "_" + DateTime.Now.Minute.ToString() + "_" + DateTime.Now.Second.ToString();

            qry = @"SELECT * INTO " + tbVAT + @" FROM(SELECT FORMAT(PURT.PURDATE, 'dd-MMM-yyyy') AS VRDATE, PURT.ORDNO AS VRNO, ACCMAS.ACCNAME AS ACCNAME, ACCMAS.SECNAME AS SECNAME, SUPMAS.VATREGNO AS VATNO, PRMAST.PRNAME AS ITEM, ROUND(PURT.QTY, " + Variables.QtyDecimalPoint + ") AS QTY, PRUNITMAS.PRUNITNAME AS UNIT, ROUND(IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100))-PURT.DISC-PURT.LDISCAMT, PURT.PRRATE-PURT.DISC-PURT.LDISCAMT), " + Variables.DecimalPoint + ") AS RATE, ROUND(IIF(PURM.INFORMAT<>3, PURT.QTY *(PURT.PRRATE-PURT.DISC-PURT.LDISCAMT), PURT.QTY *((PURT.PRRATE/(1+(PURT.TAX/100)))-PURT.DISC-PURT.LDISCAMT))," + Variables.QtyDecimalPoint + @") AS AMOUNT, ROUND(PURT.QTY*(PURT.DISC+PURT.LDISCAMT)," + Variables.QtyDecimalPoint + @") AS Discount, ROUND(PURT.QTY*(IIF(PURM.INFORMAT=3, ((PURT.PRRATE/(1+(PURT.TAX/100)))-PURT.DISC-PURT.LDISCAMT), (PURT.PRRATE-PURT.DISC-PURT.LDISCAMT)))*PURT.TAX/100," + Variables.QtyDecimalPoint + ") AS TAXAMT, ROUND(PURT.QTY*(( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))-(PURT.DISC+PURT.LDISCAMT))+PURT.QTY*(( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))-(PURT.DISC+PURT.LDISCAMT))*PURT.TAX/100," + Variables.QtyDecimalPoint + @") AS TOTAL, 'P' AS TYPE,PURM." + Functions.VSDOCNO() + @" AS INVNOTEXT
                FROM(((((PURT INNER JOIN PURM ON PURT.VRNO=PURM.VRNO) INNER JOIN ACCMAS ON PURT.SUPCODE = ACCMAS.ID) LEFT JOIN SUPMAS ON ACCMAS.ID = SUPMAS.ACCID) INNER JOIN PRMAST ON PURT.PRCODE = PRMAST.PRCODE) INNER JOIN PRUNITMAS ON PURT.UNITCODE = PRUNITMAS.PRUNITID)
                WHERE(PURT.VRTYPE = '" + clsPurT.VrType + "') AND (PURT.CANCELFLG='" + clsPurT.CancelFlg + "') AND (PURT.PURDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + @"')" + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + @"
                ) T ORDER BY T.VRNO";

            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> VATReportDetailed()
        {
            qry = @"SELECT VRDATE, VRNO, ACCNAME, SECNAME, VATNO, ITEM, QTY, UNIT, RATE, AMOUNT, TAXAMT, TOTAL, TYPE,CODE
                    FROM (SELECT DISTINCT VRDATE,INVNOTEXT AS VRNO, ACCNAME, SECNAME, VATNO, NULL AS ITEM, NULL AS QTY, NULL AS UNIT, NULL AS RATE, NULL AS AMOUNT, NULL AS TAXAMT, NULL AS TOTAL, TYPE, VRNO AS CODE, 0 AS ORDNO
                    FROM " + tbVAT + @"
                    UNION ALL
                    SELECT NULL AS VRDATE, NULL AS VRNO, NULL AS ACCMAS, NULL AS SECNAME, NULL AS VATNO, ITEM,Format(QTY,'" + Variables.QtyDecString + "'), UNIT, Format(RATE,'" + Variables.DecString + "'), Format(AMOUNT,'" + Variables.DecString + "'), Format(TAXAMT,'" + Variables.DecString + "'), Format(TOTAL,'" + Variables.DecString + @"'), TYPE, VRNO AS CODE, 1 AS ORDNO
                    FROM " + tbVAT + @" " + tbVAT + @"_1
                    UNION ALL
                    SELECT NULL AS VRDATE, NULL AS VRNO, N'" + LngTB.Total + @"' AS ACCNAME, NULL AS SECNAME, NULL AS VATNO, NULL AS ITEM, NULL AS QTY, NULL AS UNIT, NULL AS RATE,
                    FORMAT(SUM(AMOUNT),'" + Variables.DecString + @"') AS AMOUNT,
                    FORMAT(SUM(TAXAMT),'" + Variables.DecString + @"') AS TAXAMT,
                    FORMAT(SUM(TOTAL),'" + Variables.DecString + @"') AS TOTAL,
                    NULL AS TYPE, MAX(VRNO)+1 AS CODE, 2 AS ORDNO
                    FROM " + tbVAT + @" ) T ORDER BY CODE, ORDNO ";

            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> VATReportDetailedByVrNo(int OrdNo)
        {
            qry = @"SELECT VRDATE, VRNO, ACCNAME, SECNAME, VATNO, ITEM, Format( QTY,'" + Variables.QtyDecString + "') AS QTY, UNIT,Format( RATE,'" + Variables.DecString + "') AS RATE,Format( AMOUNT,'" + Variables.DecString + "') AS AMOUNT,Format( TAXAMT,'" + Variables.DecString + "') AS TAXAMT,Format( TOTAL,'" + Variables.DecString + @"') AS TOTAL, TYPE,CODE
                    FROM (SELECT DISTINCT VRDATE,INVNOTEXT AS VRNO, ACCNAME, SECNAME, VATNO, NULL AS ITEM, NULL AS QTY, NULL AS UNIT, NULL AS RATE, NULL AS AMOUNT, NULL AS TAXAMT, NULL AS TOTAL, TYPE, VRNO AS CODE, 0 AS ORDNO
                    FROM " + tbVAT + @"
                    UNION ALL
                    SELECT NULL AS VRDATE, NULL AS VRNO, NULL AS ACCMAS, NULL AS SECNAME, NULL AS VATNO, ITEM, QTY, UNIT, RATE, AMOUNT, TAXAMT, TOTAL, TYPE, VRNO AS CODE, 1 AS ORDNO
                    FROM " + tbVAT + @" " + tbVAT + @"_1) T WHERE T.CODE='" + OrdNo + "' ORDER BY CODE, ORDNO";

            return await db.GetDatatable(qry);
        }

        public async Task<int> DropTb()
        {
            qry = "DROP TABLE " + tbVAT + "";
            return await db.Execute(qry);
        }

        public async Task<DataTable> SearchByPrCode2(ClsPurT clsPurT)
        {
            qry = "SELECT * FROM dbo.PURT WHERE PRCODE = '" + clsPurT.PrCode + "' AND CANCELFLG = 'False'";
            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> SearchByPrCode_phy2(ClsPurT clsPurT)
        {
            qry = "SELECT * FROM dbo.PURTPHY WHERE PRCODE='" + clsPurT.PrCode + "' AND CANCELFLG = 'False'";
            return await db.GetDatatable(qry);
        }
    }
}