using System.Data;
using QuickBalance.API.CLS;

namespace QuickBalance.API.DB
{
    public class PurDDB
    {
        private readonly DBConn db;
        public Variable Variables = new Variable();
        public Function Functions = new Function();

        private string qry = "";

        public PurDDB(DBConn DBConn)
        {
            db = DBConn;
            Functions = new Function();
        }

        public async Task<DataTable> PurchaseTaxReport(ClsPurD clsPurD, ClsDate clsDate, string InvType, string TaxType, bool MultiTax, bool Unapproved)

        {
            string Particulars;

            try
            {
                if (clsPurD.VrType != "")
                {
                    Particulars = InvType != ""
                        ? LngTB.Grand_Total
                        : clsPurD.VrType == "P" && !MultiTax
                            ? LngTB.Purchase_Total
                            : clsPurD.VrType == "P" && MultiTax
                                ? LngTB.Purchase_Sub_Total
                                : MultiTax
                                    ? LngTB.Purchase_Return_Sub_Total
                                    : LngTB.Purchase_Return_Total;

                    qry = @"SELECT * FROM (
                SELECT FORMAT(PURD.PURDATE,'" + Variables.format + @"') AS DDATE,
                    PURM." + Functions.VSDOCNO() + @" AS DVchNo,
                    BAYANNO,
                    IIF(PURD.VRTYPE='P',N'" + LngTB.Purchase + @"',N'" + LngTB.Purchase_Return + @"') AS DVchType,
                    IIF(PURD.CANCELFLG='True',ACCMAS.ACCNAME+'-Cancelled',ACCMAS.ACCNAME) AS DParticulars,
                    SUPMAS.VATREGNO AS DVatNo,
                    PURD.BILLNO AS DSupInvDet,
                    FORMAT(PURD.BILLDATE,'" + Variables.format + @"') AS DSupInvDate,
                    FORMAT(PURD.AMOUNT,'" + Variables.DecString + @"') AS DAmt,
                    FORMAT(PURD.TAXAMT,'" + Variables.DecString + @"') AS DTaxAmt,
                    PURM.REMARKS,
                    PURD.ORDNO AS VRNO,
                    FORMAT(PURD.TAX,'" + Variables.DecString + @"') AS TAX,
                    PURD.PURDATE AS TDATE
                FROM (((PURD INNER JOIN PURM ON PURD.VRNO=PURM.VRNO)
                    INNER JOIN ACCMAS ON PURD.SUPCODE=ACCMAS.ID)
                    LEFT OUTER JOIN SUPMAS ON PURD.SUPCODE=SUPMAS.ACCID)
                    INNER JOIN TAXMAS ON PURD.CODE=TAXMAS.CODE
                WHERE PURD.VRTYPE='" + clsPurD.VrType + @"'
                    AND PURD.ETYPE='T'
                    AND PURD.TAX=" + clsPurD.Tax + @"
                    AND PURD.PURDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + @"'
                    AND TAXMAS.TTYPE<>'E'
                    AND TAXMAS.TTYPE<>'UR'
                    AND " + (TaxType == "D"
                                ? "PURM.INFORMAT<>4 AND PURM.INFORMAT<>6 AND PURM.INFORMAT<>8"
                                : TaxType == "G"
                                    ? "PURM.INFORMAT=6"
                                    : "PURM.INFORMAT=8")
                            + Functions.APRVLR(true, Unapproved, "PURM")
                            + Functions.VSSTMNT("PURM", true) + @"

                UNION ALL

                SELECT NULL,'','','',N'" + Particulars + @"','','',
                    '',
                    FORMAT(SUM(PURD.AMOUNT),'" + Variables.DecString + @"'),
                    FORMAT(SUM(PURD.TAXAMT),'" + Variables.DecString + @"'),
                    '',
                    MAX(PURM.ORDNO)+1,
                    NULL,
                    MAX(PURD.PURDATE)
                FROM (((PURD INNER JOIN PURM ON PURD.VRNO=PURM.VRNO)
                    INNER JOIN ACCMAS ON PURD.SUPCODE=ACCMAS.ID)
                    LEFT OUTER JOIN SUPMAS ON PURD.SUPCODE=SUPMAS.ACCID)
                    INNER JOIN TAXMAS ON PURD.CODE=TAXMAS.CODE
                WHERE PURD.VRTYPE='" + clsPurD.VrType + @"'
                    AND PURD.ETYPE='T'
                    AND PURD.TAX=" + clsPurD.Tax + @"
                    AND PURD.PURDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + @"'
                    AND TAXMAS.TTYPE<>'E'
                    AND TAXMAS.TTYPE<>'UR'
                    AND " + (TaxType == "D"
                                ? "PURM.INFORMAT<>4 AND PURM.INFORMAT<>6 AND PURM.INFORMAT<>8"
                                : TaxType == "G"
                                    ? "PURM.INFORMAT=6"
                                    : "PURM.INFORMAT=8")
                            + Functions.APRVLR(true, Unapproved, "PURM")
                            + Functions.VSSTMNT("PURM", true) + @"
            ) T ORDER BY T.TDATE,T.VRNO";
                }
                else
                {
                    Particulars = MultiTax
                        ? LngTB.Sub_Total_Purchase_Return
                        : LngTB.Grand_Total_Purchase_Return;

                    qry = @"SELECT NULL AS DDATE,'' AS DVchNo,'' AS BAYANNO,'' AS DVchType,
                N'" + Particulars + @"' AS DParticulars,'' AS DVatNo,'' AS DSupInvDet,'',
                FORMAT(SUM(IIF(PURD.VRTYPE='P',PURD.AMOUNT,-1*PURD.AMOUNT)),'" + Variables.DecString + @"') AS DAmt,
                FORMAT(SUM(IIF(PURD.VRTYPE='P',PURD.TAXAMT,-1*PURD.TAXAMT)),'" + Variables.DecString + @"') AS DTaxAmt,
                '' AS REMARKS,MAX(PURM.ORDNO)+1 AS VRNO,NULL AS TAX
            FROM (((PURD INNER JOIN PURM ON PURD.VRNO=PURM.VRNO)
                INNER JOIN ACCMAS ON PURD.SUPCODE=ACCMAS.ID)
                LEFT OUTER JOIN SUPMAS ON PURD.SUPCODE=SUPMAS.ACCID)
                INNER JOIN TAXMAS ON PURD.CODE=TAXMAS.CODE
            WHERE PURD.ETYPE='T'
                AND PURD.TAX=" + clsPurD.Tax + @"
                AND PURD.PURDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + @"'
                AND TAXMAS.TTYPE<>'E'
                AND TAXMAS.TTYPE<>'UR'
                AND " + (TaxType == "D"
                            ? "PURM.INFORMAT<>4 AND PURM.INFORMAT<>6 AND PURM.INFORMAT<>8"
                            : TaxType == "G"
                                ? "PURM.INFORMAT=6"
                                : "PURM.INFORMAT=8")
                        + Functions.APRVLR(true, Unapproved, "PURM")
                        + Functions.VSSTMNT("PURM", true);
                }

                return await db.GetDatatable(qry);
            }
            catch
            {
                return new DataTable();
            }
        }
    }
    
}