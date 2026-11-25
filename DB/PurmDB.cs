using QuickBalance.API.CLS;
using System.Data;

namespace QuickBalance.API.DB
{
    public class PurmDB
    {
        private readonly DBConn db;
        public Variable Variables = new Variable();
        public Function Functions = new Function();
        string qry = "";

        public PurmDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public async Task<DataTable> GetTrTypeAll()
        {
            qry = "SELECT VRNO,TRTYPE FROM PURM WHERE TRTYPE<>'ST'";
            return await db.GetDatatable(qry);
        }

        public async Task<string> GetTrType(int VrNo)
        {
            qry = "SELECT TRTYPE FROM PURM WHERE VRNO=" + VrNo;
            return await db.ReturnStringValue(qry);
        }

        public async Task<DataTable> PurchaseSummary(ClsDate clsDate, ClsPurM clsPurM, string Type, int SupId, bool RemGrpTot, int PartyGrp, bool Unapproved)
        {
            string grpStmnt = PartyGrp == 0 ? "" : " AND PURM.SUPCODE IN (SELECT SUPMAS.ACCID FROM SUPMAS WHERE SUPMAS.PARTYGRPID IS NOT NULL AND SUPMAS.PARTYGRPID = " + PartyGrp + ")";
            DataTable PurSummary = new DataTable();
            try
            {
                string PurDate = Variables.fyStartDate.ToString(Variables.format);
                string SupName = "";
                string qry1 = "";
                PurSummary.Columns.Add("VrNo");
                PurSummary.Columns.Add("PurDate");
                PurSummary.Columns.Add("InvText");
                PurSummary.Columns.Add("BillDate");
                PurSummary.Columns.Add("BillNo");
                PurSummary.Columns.Add("SupplierName");
                PurSummary.Columns.Add("Amount");
                PurSummary.Columns.Add("TaxValue");
                PurSummary.Columns.Add("BillValue");
                PurSummary.Columns.Add("BType");
                PurSummary.Columns.Add("VrType");
                PurSummary.Columns.Add("Type");

                if ((clsPurM.PayMode == 1 || clsPurM.PayMode == 2) && Type == "Datewise" && SupId == 0)  //--CASH OR CREDIT AND DATEWISE
                    qry = @"SELECT DISTINCT(FORMAT(PURDATE,'" + Variables.format + "')) AS PURDATE,PURDATE AS DATE,'B'as Type FROM PURM WHERE  (PAYMODE='" + clsPurM.PayMode + "') AND (RETURNFLG='" + clsPurM.ReturnFlg + "') AND (PURDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "') AND (VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + "  ORDER BY DATE";
                else if (clsPurM.PayMode == 0 && Type == "Datewise" && SupId == 0)
                    qry = @"SELECT DISTINCT(FORMAT(PURDATE,'" + Variables.format + "')) AS PURDATE,PURDATE AS DATE,'B'as Type FROM PURM WHERE (RETURNFLG='" + clsPurM.ReturnFlg + "') AND (PURDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "')" + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + " ORDER BY DATE";
                else if ((clsPurM.PayMode == 1 || clsPurM.PayMode == 2) && Type == "Datewise" && SupId > 0)
                    qry = @"SELECT DISTINCT(FORMAT(PURDATE,'" + Variables.format + "')) AS PURDATE,PURDATE AS DATE,'B'as Type FROM PURM WHERE (PURDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "') AND (PAYMODE='" + clsPurM.PayMode + "') AND (SUPCODE='" + SupId + "') AND (RETURNFLG='" + clsPurM.ReturnFlg + "') AND (VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + " ORDER BY DATE";
                else if (clsPurM.PayMode == 0 && Type == "Datewise" && SupId > 0)
                    qry = @"SELECT DISTINCT(FORMAT(PURDATE,'" + Variables.format + "')) AS PURDATE,PURDATE AS DATE,'B'as Type FROM PURM WHERE (PURDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "') AND (SUPCODE='" + SupId + "') AND (RETURNFLG='" + clsPurM.ReturnFlg + "') AND (VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + " ORDER BY DATE";
                else if ((clsPurM.PayMode == 1 || clsPurM.PayMode == 2) && Type == "Supplierwise" && SupId == 0)
                    qry = @"SELECT DISTINCT ACCMAS.ACCNAME FROM (ACCMAS INNER JOIN PURM ON ACCMAS.ID = PURM.SUPCODE) WHERE (PURM.PURDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "') AND (PAYMODE='" + clsPurM.PayMode + "') AND (RETURNFLG='" + clsPurM.ReturnFlg + "') AND (VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + "";
                else if ((clsPurM.PayMode == 1 || clsPurM.PayMode == 2) && Type == "Supplierwise" && SupId > 0)
                    qry = @"SELECT DISTINCT ACCMAS.ACCNAME FROM (ACCMAS INNER JOIN PURM ON ACCMAS.ID = PURM.SUPCODE) WHERE (PURM.PURDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "') AND (PAYMODE='" + clsPurM.PayMode + "') AND (SUPCODE='" + SupId + "') AND (RETURNFLG='" + clsPurM.ReturnFlg + "') AND (VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + "";
                else if (clsPurM.PayMode == 0 && Type == "Supplierwise" && SupId == 0)
                    qry = @"SELECT DISTINCT ACCMAS.ACCNAME FROM (ACCMAS INNER JOIN PURM ON ACCMAS.ID = PURM.SUPCODE) WHERE (PURM.PURDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "') AND (PURM.RETURNFLG='" + clsPurM.ReturnFlg + "') AND (VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + "";
                else if (clsPurM.PayMode == 0 && Type == "Supplierwise" && SupId > 0)
                    qry = @"SELECT DISTINCT ACCMAS.ACCNAME FROM (ACCMAS INNER JOIN PURM ON ACCMAS.ID = PURM.SUPCODE) WHERE (PURM.PURDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "') AND (PURM.SUPCODE='" + SupId + "') AND (PURM.RETURNFLG='" + clsPurM.ReturnFlg + "') AND (VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + "";
                DataTable dt = await db.GetDatatable(qry);
                DataTable DT;
                if (dt.Rows.Count > 0)
                {
                    int i = 0;
                    do
                    {
                        DataRow dr = dt.Rows[i];

                        if (Type == "Datewise" && SupId == 0)
                            PurDate = dr["PURDATE"].ToString();
                        else if (Type == "Datewise" && SupId > 0)
                            PurDate = dr["PURDATE"].ToString();
                        else
                            SupName = dr["ACCNAME"].ToString();

                        if ((clsPurM.PayMode == 1 || clsPurM.PayMode == 2) && Type == "Datewise" && SupId == 0)
                            qry1 = @"SELECT PURM." + Functions.VSDOCNO() + " AS InvText, CONVERT(VARCHAR,PURM.ORDNO) AS VrNo, FORMAT(PURM.PURDATE,'" + Variables.format + "') AS PurDate, PURM.BILLNO AS BillNo, FORMAT(PURM.BILLDATE,'" + Variables.format + "') AS BillDate, IIF(PURM.CANCELFLG='TRUE',ACCMAS.ACCNAME +' - Cancelled',ACCMAS.ACCNAME) AS SupplierName,IIF(PURM.TVALUE=0,NULL,FORMAT(PURM.TVALUE,'" + Variables.DecString + "')) AS Amount,IIF(PURM.TAX=0,NULL,FORMAT(PURM.TAX,'" + Variables.DecString + "')) AS TaxValue,IIF(PURM.TVALUE=0,NULL,FORMAT(PURM.TVALUE+PURM.TAX,'" + Variables.DecString + "')) AS BillValue , PAYMODE.MODE AS BType,PURM.VRTYPE AS VrType,' ' AS Type FROM ((ACCMAS INNER JOIN PURM ON ACCMAS.ID = PURM.SUPCODE) INNER JOIN PAYMODE ON PURM.PAYMODE = PAYMODE.CODE) WHERE (PURM.PURDATE='" + PurDate + "') AND (PURM.PAYMODE='" + clsPurM.PayMode + "') AND (PURM.RETURNFLG='" + clsPurM.ReturnFlg + "') AND (PURM.VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + " UNION ALL SELECT '' AS INVTEXT, ' ' AS VrNo,'' AS PurDate,'' AS BillNo, '' AS BillDate,N'" + LngTB.Day_Total + "' AS SupplierName,IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE),'" + Variables.DecString + "')) AS Amount,IIF(SUM(PURM.TAX)=0,NULL,FORMAT(SUM(PURM.TAX),'" + Variables.DecString + "')) AS TaxValue,IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE+PURM.TAX),'" + Variables.DecString + "')) AS Total,'' AS BType,'' AS VrType,'B' as Type FROM PURM WHERE (PURDATE='" + PurDate + "') AND (PAYMODE='" + clsPurM.PayMode + "') AND (RETURNFLG='" + clsPurM.ReturnFlg + "') AND (VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + "";
                        else if ((clsPurM.PayMode == 1 || clsPurM.PayMode == 2) && Type == "Datewise" && SupId > 0)
                            qry1 = @"SELECT  PURM." + Functions.VSDOCNO() + " AS InvText,CONVERT(VARCHAR,PURM.ORDNO) AS VrNo, FORMAT(PURM.PURDATE,'" + Variables.format + "') AS PurDate, PURM.BILLNO AS BillNo, FORMAT(PURM.BILLDATE,'" + Variables.format + "') AS BillDate, IIF(PURM.CANCELFLG='TRUE',ACCMAS.ACCNAME +' - Cancelled',ACCMAS.ACCNAME) AS SupplierName,IIF(PURM.TVALUE=0,NULL,FORMAT(PURM.TVALUE,'" + Variables.DecString + "')) AS Amount,IIF(PURM.TAX=0,NULL,FORMAT(PURM.TAX,'" + Variables.DecString + "')) AS TaxValue,IIF(PURM.TVALUE=0,NULL,FORMAT(PURM.TVALUE+PURM.TAX,'" + Variables.DecString + "')) AS BillValue , PAYMODE.MODE AS BType,PURM.VRTYPE AS VrType,' ' AS Type FROM ((ACCMAS INNER JOIN PURM ON ACCMAS.ID = PURM.SUPCODE) INNER JOIN PAYMODE ON PURM.PAYMODE = PAYMODE.CODE) WHERE (PURM.SUPCODE='" + SupId + "') AND (PURM.PURDATE='" + PurDate + "') AND (PURM.PAYMODE='" + clsPurM.PayMode + "') AND (PURM.RETURNFLG='" + clsPurM.ReturnFlg + "') AND (PURM.VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + " UNION ALL SELECT '' AS INVTEXT,'' AS VrNo,'' AS PurDate,'' AS BillNo, '' AS BillDate,N'" + LngTB.Day_Total + "' AS SupplierName,IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE),'" + Variables.DecString + "')) AS Amount,IIF(SUM(PURM.TAX)=0,NULL,FORMAT(SUM(PURM.TAX),'" + Variables.DecString + "')) AS TaxValue,IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE+PURM.TAX),'" + Variables.DecString + "')) AS Total,'' AS BType,'' AS VrType,'B' as Type FROM PURM WHERE (PURM.SUPCODE='" + SupId + "') AND (PURM.PURDATE='" + PurDate + "') AND (PURM.PAYMODE='" + clsPurM.PayMode + "') AND (RETURNFLG='" + clsPurM.ReturnFlg + "') AND (VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + "";
                        else if (clsPurM.PayMode == 0 && Type == "Datewise" && SupId == 0)
                            qry1 = @"SELECT  PURM." + Functions.VSDOCNO() + " AS InvText,CONVERT(VARCHAR,PURM.ORDNO) AS VrNo, FORMAT(PURM.PURDATE,'" + Variables.format + "') AS PurDate, PURM.BILLNO AS BillNo, FORMAT(PURM.BILLDATE,'" + Variables.format + "') AS BillDate, IIF(PURM.CANCELFLG='TRUE',ACCMAS.ACCNAME +' - Cancelled',ACCMAS.ACCNAME) AS SupplierName,IIF(PURM.TVALUE=0,NULL,FORMAT(PURM.TVALUE,'" + Variables.DecString + "')) AS Amount,IIF(PURM.TAX=0,NULL,FORMAT(PURM.TAX,'" + Variables.DecString + "')) AS TaxValue,IIF(PURM.TVALUE=0,NULL,FORMAT(PURM.TVALUE+PURM.TAX,'" + Variables.DecString + "')) AS BillValue , PAYMODE.MODE AS BType,PURM.VRTYPE AS VrType,' ' AS Type FROM ((ACCMAS INNER JOIN PURM ON ACCMAS.ID = PURM.SUPCODE)INNER JOIN PAYMODE ON PURM.PAYMODE = PAYMODE.CODE) WHERE (PURM.PURDATE='" + PurDate + "') AND (PURM.PURDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "') AND (PURM.RETURNFLG='" + clsPurM.ReturnFlg + "') AND (PURM.VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + " UNION ALL SELECT '' AS INVTEXT,'' AS VrNo,'' AS PurDate,'' AS BillNo, '' AS BillDate,N'" + LngTB.Day_Total + "' AS SupplierName,IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE),'" + Variables.DecString + "')) AS Amount,IIF(SUM(PURM.TAX)=0,NULL,FORMAT(SUM(PURM.TAX),'" + Variables.DecString + "')) AS TaxValue,IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE+PURM.TAX),'" + Variables.DecString + "')) AS Total,'' AS BType,'' AS VrType,'B' as Type FROM PURM WHERE (PURM.PURDATE='" + PurDate + "') AND (PURM.PURDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "') AND (PURM.RETURNFLG='" + clsPurM.ReturnFlg + "') AND (VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + "";
                        else if (clsPurM.PayMode == 0 && Type == "Datewise" && SupId > 0)
                            qry1 = @"SELECT  PURM." + Functions.VSDOCNO() + " AS InvText,CONVERT(VARCHAR,PURM.ORDNO) AS VrNo, FORMAT(PURM.PURDATE,'" + Variables.format + "') AS PurDate, PURM.BILLNO AS BillNo, FORMAT(PURM.BILLDATE,'" + Variables.format + "') AS BillDate, IIF(PURM.CANCELFLG='TRUE',ACCMAS.ACCNAME +' - Cancelled',ACCMAS.ACCNAME) AS SupplierName,IIF(PURM.TVALUE=0,NULL,FORMAT(PURM.TVALUE,'" + Variables.DecString + "')) AS Amount,IIF(PURM.TAX=0,NULL,FORMAT(PURM.TAX,'" + Variables.DecString + "')) AS TaxValue,IIF(PURM.TVALUE=0,NULL,FORMAT(PURM.TVALUE+PURM.TAX,'" + Variables.DecString + "')) AS BillValue , PAYMODE.MODE AS BType,PURM.VRTYPE AS VrType,' ' AS Type FROM ((ACCMAS INNER JOIN PURM ON ACCMAS.ID = PURM.SUPCODE)INNER JOIN PAYMODE ON PURM.PAYMODE = PAYMODE.CODE) WHERE (PURM.SUPCODE='" + SupId + "') AND (PURM.PURDATE='" + PurDate + "') AND (PURM.RETURNFLG='" + clsPurM.ReturnFlg + "') AND (PURM.VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + " UNION ALL SELECT '' AS INVTEXT,'' AS VrNo,'' AS PurDate,'' AS BillNo, '' AS BillDate,N'" + LngTB.Day_Total + "' AS SupplierName,IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE),'" + Variables.DecString + "')) AS Amount,IIF(SUM(PURM.TAX)=0,NULL,FORMAT(SUM(PURM.TAX),'" + Variables.DecString + "')) AS TaxValue,IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE+PURM.TAX),'" + Variables.DecString + "')) AS Total,'' AS BType,'' AS VrType,'B' as Type FROM PURM WHERE (PURM.SUPCODE='" + SupId + "') AND (PURM.PURDATE='" + PurDate + "') AND (PURM.RETURNFLG='" + clsPurM.ReturnFlg + "') AND (VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + "";
                        else if ((clsPurM.PayMode == 1 || clsPurM.PayMode == 2) && Type == "Supplierwise" && SupId == 0)
                            qry1 = @"SELECT  PURM." + Functions.VSDOCNO() + " AS InvText,CONVERT(VARCHAR,PURM.ORDNO) AS VrNo, FORMAT(PURM.PURDATE,'" + Variables.format + "') AS PurDate, PURM.BILLNO AS BillNo, FORMAT(PURM.BILLDATE,'" + Variables.format + "') AS BillDate, IIF(PURM.CANCELFLG='TRUE',ACCMAS.ACCNAME +' - Cancelled',ACCMAS.ACCNAME) AS SupplierName,IIF(PURM.TVALUE=0,NULL,FORMAT(PURM.TVALUE,'" + Variables.DecString + "')) AS Amount,IIF(PURM.TAX=0,NULL,FORMAT(PURM.TAX,'" + Variables.DecString + "')) AS TaxValue,IIF(PURM.TVALUE=0,NULL,FORMAT(PURM.TVALUE+PURM.TAX,'" + Variables.DecString + "')) AS BillValue , PAYMODE.MODE AS BType,PURM.VRTYPE AS VrType,' ' AS Type FROM((ACCMAS INNER JOIN PURM ON ACCMAS.ID = PURM.SUPCODE) INNER JOIN PAYMODE ON PURM.PAYMODE = PAYMODE.CODE) WHERE (PURM.PURDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "') AND (ACCMAS.ACCNAME = '" + SupName + "') AND (PURM.PAYMODE='" + clsPurM.PayMode + "') AND (PURM.RETURNFLG='" + clsPurM.ReturnFlg + "') AND (PURM.VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + " UNION ALL SELECT '' AS INVTEXT,'' AS VrNo, '' AS PurDate, '' AS BillNo, '' AS BillDate, N'" + LngTB.Supplier_Total + "' AS SupplierName, IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE),'" + Variables.DecString + "')) AS Amount,IIF(SUM(PURM.TAX)=0,NULL,FORMAT(SUM(PURM.TAX),'" + Variables.DecString + "')) AS TaxValue,IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE+PURM.TAX),'" + Variables.DecString + "')) AS Total,'' AS BType,'' AS VrType,'B' as Type FROM ACCMAS INNER JOIN PURM ON ACCMAS.ID = PURM.SUPCODE WHERE (PURM.PAYMODE='" + clsPurM.PayMode + "')  AND(ACCMAS.ACCNAME = '" + SupName + "') AND (PURM.RETURNFLG='" + clsPurM.ReturnFlg + "') AND (PURM.VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + "";
                        else if ((clsPurM.PayMode == 1 || clsPurM.PayMode == 2) && Type == "Supplierwise" && SupId > 0)
                            qry1 = @"SELECT  PURM." + Functions.VSDOCNO() + " AS InvText,CONVERT(VARCHAR,PURM.ORDNO) AS VrNo, FORMAT(PURM.PURDATE,'" + Variables.format + "') AS PurDate, PURM.BILLNO AS BillNo, FORMAT(PURM.BILLDATE,'" + Variables.format + "') AS BillDate, IIF(PURM.CANCELFLG='TRUE',ACCMAS.ACCNAME +' - Cancelled',ACCMAS.ACCNAME) AS SupplierName,IIF(PURM.TVALUE=0,NULL,FORMAT(PURM.TVALUE,'" + Variables.DecString + "')) AS Amount,IIF(PURM.TAX=0,NULL,FORMAT(PURM.TAX,'" + Variables.DecString + "')) AS TaxValue,IIF(PURM.TVALUE=0,NULL,FORMAT(PURM.TVALUE+PURM.TAX,'" + Variables.DecString + "')) AS BillValue , PAYMODE.MODE AS BType,PURM.VRTYPE AS VrType,' ' AS Type FROM((ACCMAS INNER JOIN PURM ON ACCMAS.ID = PURM.SUPCODE) INNER JOIN PAYMODE ON PURM.PAYMODE = PAYMODE.CODE) WHERE (PURM.PURDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "') AND (PURM.SUPCODE='" + SupId + "') AND (PURM.PAYMODE='" + clsPurM.PayMode + "') AND (PURM.RETURNFLG='" + clsPurM.ReturnFlg + "') AND (PURM.VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + " UNION ALL SELECT '' AS INVTEXT,'' AS VrNo, '' AS PurDate, '' AS BillNo, '' AS BillDate, N'" + LngTB.Supplier_Total + "' AS SupplierName,IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE),'" + Variables.DecString + "')) AS Amount,IIF(SUM(PURM.TAX)=0,NULL,FORMAT(SUM(PURM.TAX),'" + Variables.DecString + "')) AS TaxValue,IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE+PURM.TAX),'" + Variables.DecString + "')) AS Total,'' AS BType,'' AS VrType,'B' as Type FROM ACCMAS INNER JOIN PURM ON ACCMAS.ID = PURM.SUPCODE WHERE (PURM.PAYMODE='" + clsPurM.PayMode + "')  AND (PURM.SUPCODE = '" + SupId + "') AND (PURM.RETURNFLG='" + clsPurM.ReturnFlg + "') AND (PURM.VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + "";
                        else if (clsPurM.PayMode == 0 && Type == "Supplierwise" && SupId == 0)
                            qry1 = @"SELECT  PURM." + Functions.VSDOCNO() + " AS InvText,CONVERT(VARCHAR,PURM.ORDNO) AS VrNo, FORMAT(PURM.PURDATE,'" + Variables.format + "') AS PurDate, PURM.BILLNO AS BillNo, FORMAT(PURM.BILLDATE,'" + Variables.format + "') AS BillDate, IIF(PURM.CANCELFLG='TRUE',ACCMAS.ACCNAME +' - Cancelled',ACCMAS.ACCNAME) AS SupplierName,IIF(PURM.TVALUE=0,NULL,FORMAT(PURM.TVALUE,'" + Variables.DecString + "')) AS Amount,IIF(PURM.TAX=0,NULL,FORMAT(PURM.TAX,'" + Variables.DecString + "')) AS TaxValue,IIF(PURM.TVALUE=0,NULL,FORMAT(PURM.TVALUE+PURM.TAX,'" + Variables.DecString + "')) AS BillValue , PAYMODE.MODE AS BType,PURM.VRTYPE AS VrType,' ' AS Type FROM((ACCMAS INNER JOIN PURM ON ACCMAS.ID = PURM.SUPCODE) INNER JOIN PAYMODE ON PURM.PAYMODE = PAYMODE.CODE) WHERE(PURM.PURDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "') AND (ACCMAS.ACCNAME = '" + SupName + "') AND (PURM.RETURNFLG='" + clsPurM.ReturnFlg + "') AND (PURM.VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + " UNION ALL SELECT '' AS INVTEXT,'' AS VrNo, '' AS PurDate, '' AS BillNo, '' AS BillDate, N'" + LngTB.Supplier_Total + "' AS SupplierName, IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE),'" + Variables.DecString + "')) AS Amount,IIF(SUM(PURM.TAX)=0,NULL,FORMAT(SUM(PURM.TAX),'" + Variables.DecString + "')) AS TaxValue,IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE+PURM.TAX),'" + Variables.DecString + "')) AS Total,'' AS BType,'' AS VrType,'B' as Type FROM ACCMAS INNER JOIN PURM ON ACCMAS.ID = PURM.SUPCODE WHERE(ACCMAS.ACCNAME = '" + SupName + "') AND (PURM.RETURNFLG='" + clsPurM.ReturnFlg + "') AND (PURM.VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + "";
                        else if (clsPurM.PayMode == 0 && Type == "Supplierwise" && SupId > 0)
                            qry1 = @"SELECT  PURM." + Functions.VSDOCNO() + " AS InvText,CONVERT(VARCHAR,PURM.ORDNO) AS VrNo, FORMAT(PURM.PURDATE,'" + Variables.format + "') AS PurDate, PURM.BILLNO AS BillNo, FORMAT(PURM.BILLDATE,'" + Variables.format + "') AS BillDate, IIF(PURM.CANCELFLG='TRUE',ACCMAS.ACCNAME +' - Cancelled',ACCMAS.ACCNAME) AS SupplierName,IIF(PURM.TVALUE=0,NULL,FORMAT(PURM.TVALUE,'" + Variables.DecString + "')) AS Amount,IIF(PURM.TAX=0,NULL,FORMAT(PURM.TAX,'" + Variables.DecString + "')) AS TaxValue,IIF(PURM.TVALUE=0,NULL,FORMAT(PURM.TVALUE+PURM.TAX,'" + Variables.DecString + "')) AS BillValue , PAYMODE.MODE AS BType,PURM.VRTYPE AS VrType,' ' AS Type FROM((ACCMAS INNER JOIN PURM ON ACCMAS.ID = PURM.SUPCODE) INNER JOIN PAYMODE ON PURM.PAYMODE = PAYMODE.CODE) WHERE(PURM.PURDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "') AND (PURM.SUPCODE='" + SupId + "') AND (PURM.RETURNFLG='" + clsPurM.ReturnFlg + "') AND (PURM.VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + " UNION ALL SELECT '' AS INVTEXT,'' AS VrNo, '' AS PurDate, '' AS BillNo, '' AS BillDate, N'" + LngTB.Supplier_Total + "' AS SupplierName, IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE),'" + Variables.DecString + "')) AS Amount,IIF(SUM(PURM.TAX)=0,NULL,FORMAT(SUM(PURM.TAX),'" + Variables.DecString + "')) AS TaxValue,IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE+PURM.TAX),'" + Variables.DecString + "')) AS Total,'' AS BType,'' AS VrType,'B' as Type FROM ACCMAS INNER JOIN PURM ON ACCMAS.ID = PURM.SUPCODE WHERE(PURM.SUPCODE='" + SupId + "') AND (PURM.RETURNFLG='" + clsPurM.ReturnFlg + "') AND (PURM.VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + "";
                        DT = await db.GetDatatable(qry1);
                        if (DT.Rows.Count > 1)
                        {
                            if (RemGrpTot)
                                DT.Rows.RemoveAt(DT.Rows.Count - 1);
                            else if (PurSummary.Rows.Count > 0)
                                PurSummary.Rows.Add();
                            PurSummary.Merge(DT, true, MissingSchemaAction.Ignore);
                        }
                        i++;
                    }
                    while (i < dt.Rows.Count);

                    if (!RemGrpTot)
                        PurSummary.Rows.Add();

                    string qry2 = "";
                    if ((clsPurM.PayMode == 1 || clsPurM.PayMode == 2) && Type == "Datewise" && SupId == 0)
                        qry2 = @"SELECT '' AS InvText,'' AS VrNo,'' AS PurDate,'' AS BillNo, '' AS BillDate,N'" + LngTB.Grand_Total + "' AS SupplierName,IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE),'" + Variables.DecString + "')) AS Amount,IIF(SUM(PURM.TAX)=0,NULL,FORMAT(SUM(PURM.TAX),'" + Variables.DecString + "')) AS TaxValue,IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE+PURM.TAX),'" + Variables.DecString + "')) AS BillValue,'' AS BType,'B' as Type FROM PURM WHERE (PURM.PURDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "') AND (PURM.PAYMODE='" + clsPurM.PayMode + "') AND (PURM.RETURNFLG='" + clsPurM.ReturnFlg + "') AND (PURM.VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + "";
                    else if ((clsPurM.PayMode == 1 || clsPurM.PayMode == 2) && Type == "Datewise" && SupId > 0)
                        qry2 = @"SELECT '' AS InvText,'' AS VrNo,'' AS PurDate,'' AS BillNo_BillDate,N'" + LngTB.Grand_Total + "' AS SupplierName,IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE),'" + Variables.DecString + "')) AS Amount,IIF(SUM(PURM.TAX)=0,NULL,FORMAT(SUM(PURM.TAX),'" + Variables.DecString + "')) AS TaxValue,IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE+PURM.TAX),'" + Variables.DecString + "')) AS BillValue,'' AS BType,'B' as Type FROM PURM WHERE (PURM.PURDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "') AND (PURM.SUPCODE='" + SupId + "') AND (PURM.PAYMODE='" + clsPurM.PayMode + "') AND (PURM.RETURNFLG='" + clsPurM.ReturnFlg + "') AND (PURM.VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + "";
                    else if (clsPurM.PayMode == 0 && Type == "Datewise" && SupId == 0)

                        qry2 = @"SELECT '' AS InvText,'' AS VrNo,'' AS PurDate,'' AS BillNo, '' AS BillDate,N'" + LngTB.Grand_Total + "' AS SupplierName,IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE),'" + Variables.DecString + "')) AS Amount,IIF(SUM(PURM.TAX)=0,NULL,FORMAT(SUM(PURM.TAX),'" + Variables.DecString + "')) AS TaxValue,IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE+PURM.TAX),'" + Variables.DecString + "')) AS BillValue,'' AS BType,'B' as Type FROM PURM WHERE (PURM.PURDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "') AND (PURM.RETURNFLG='" + clsPurM.ReturnFlg + "') AND (PURM.VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + "";
                    else if (clsPurM.PayMode == 0 && Type == "Datewise" && SupId > 0)
                        qry2 = @"SELECT '' AS InvText,'' AS VrNo,'' AS PurDate,'' AS BillNo, '' AS BillDate,N'" + LngTB.Grand_Total + "' AS SupplierName,IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE),'" + Variables.DecString + "')) AS Amount,IIF(SUM(PURM.TAX)=0,NULL,FORMAT(SUM(PURM.TAX),'" + Variables.DecString + "')) AS TaxValue,IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE+PURM.TAX),'" + Variables.DecString + "')) AS BillValue,'' AS BType,'B' as Type FROM PURM WHERE (PURM.PURDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "') AND (PURM.SUPCODE='" + SupId + "') AND (PURM.RETURNFLG='" + clsPurM.ReturnFlg + "') AND (PURM.VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + "";
                    else if ((clsPurM.PayMode == 1 || clsPurM.PayMode == 2) && Type == "Supplierwise" && SupId == 0)
                        qry2 = @"SELECT '' AS InvText,'' AS VrNo, '' AS PurDate, '' AS BillNo, '' AS BillDate, N'" + LngTB.Grand_Total + "' AS SupplierName, IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE),'" + Variables.DecString + "')) AS Amount,IIF(SUM(PURM.TAX)=0,NULL,FORMAT(SUM(PURM.TAX),'" + Variables.DecString + "')) AS TaxValue,IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE+PURM.TAX),'" + Variables.DecString + "')) AS BillValue,'' AS BType,'B' as Type FROM ACCMAS INNER JOIN PURM ON ACCMAS.ID = PURM.SUPCODE WHERE (PURM.PAYMODE='" + clsPurM.PayMode + "') AND (PURM.RETURNFLG='" + clsPurM.ReturnFlg + "') AND (PURM.VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + "";
                    else if ((clsPurM.PayMode == 1 || clsPurM.PayMode == 2) && Type == "Supplierwise" && SupId > 0)
                        qry2 = @"SELECT '' AS InvText,'' AS VrNo, '' AS PurDate, '' AS BillNo, '' AS BillDate, N'" + LngTB.Grand_Total + "' AS SupplierName, IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE),'" + Variables.DecString + "')) AS Amount,IIF(SUM(PURM.TAX)=0,NULL,FORMAT(SUM(PURM.TAX),'" + Variables.DecString + "')) AS TaxValue,IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE+PURM.TAX),'" + Variables.DecString + "')) AS BillValue,'' AS BType,'B' as Type FROM ACCMAS INNER JOIN PURM ON ACCMAS.ID = PURM.SUPCODE WHERE (PURM.PAYMODE='" + clsPurM.PayMode + "')  AND (PURM.SUPCODE = '" + SupId + "') AND (PURM.RETURNFLG='" + clsPurM.ReturnFlg + "') AND (PURM.VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + "";
                    else if (clsPurM.PayMode == 0 && Type == "Supplierwise" && SupId == 0)
                        qry2 = @"SELECT '' AS InvText,'' AS VrNo, '' AS PurDate, '' AS BillNo, '' AS BillDate, N'" + LngTB.Grand_Total + "' AS SupplierName, IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE),'" + Variables.DecString + "')) AS Amount,IIF(SUM(PURM.TAX)=0,NULL,FORMAT(SUM(PURM.TAX),'" + Variables.DecString + "')) AS TaxValue,IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE+PURM.TAX),'" + Variables.DecString + "')) AS BillValue,'' AS BType,'B' as Type FROM ACCMAS INNER JOIN PURM ON ACCMAS.ID = PURM.SUPCODE WHERE (PURM.RETURNFLG='" + clsPurM.ReturnFlg + "') AND (PURM.VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + "";
                    else if (clsPurM.PayMode == 0 && Type == "Supplierwise" && SupId > 0)
                        qry2 = @"SELECT '' AS InvText,'' AS VrNo, '' AS PurDate, '' AS BillNo, '' AS BillDate, N'" + LngTB.Grand_Total + "' AS SupplierName, IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE),'" + Variables.DecString + "')) AS Amount,IIF(SUM(PURM.TAX)=0,NULL,FORMAT(SUM(PURM.TAX),'" + Variables.DecString + "')) AS TaxValue,IIF(SUM(PURM.TVALUE)=0,NULL,FORMAT(SUM(PURM.TVALUE+PURM.TAX),'" + Variables.DecString + "')) AS BillValue,'' AS BType,'B' as Type FROM ACCMAS INNER JOIN PURM ON ACCMAS.ID = PURM.SUPCODE WHERE(PURM.SUPCODE='" + SupId + "') AND (PURM.RETURNFLG='" + clsPurM.ReturnFlg + "') AND (PURM.VRTYPE='" + clsPurM.VrType + "')" + grpStmnt + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + "";
                    DataTable DTTOT = await db.GetDatatable(qry2);
                    PurSummary.Merge(DTTOT, true, MissingSchemaAction.Ignore);
                }
                return PurSummary;
            }
            catch (Exception ex) { return PurSummary; }
        }

        public async Task<DataTable> BillPurchaseSummaryNew(ClsDate clsDate, ClsPurM clsPurM, string Type, int SupId, bool RemGrpTot, int GdwnCD, int PartyGrp, bool Unapproved)
        {
            DataTable BilPurSummary = new DataTable();
            try
            {
                string PurDate = Variables.fyStartDate.ToString(Variables.format);
                int SupCode = 0, BillNo = 0;
                string qry1 = "";
                string f = "'FALSE'";
                string d = "'";
                string arabic = "N";


                BilPurSummary.Columns.Add("VoucherDate");
                BilPurSummary.Columns.Add("VoucherNo");
                BilPurSummary.Columns.Add("BillDate");
                BilPurSummary.Columns.Add("BillNo");
                BilPurSummary.Columns.Add("SupplierName");
                BilPurSummary.Columns.Add("ProductName");
                BilPurSummary.Columns.Add("Qty");
                BilPurSummary.Columns.Add("Rate");
                BilPurSummary.Columns.Add("TValue");
                BilPurSummary.Columns.Add("VrNo");
                BilPurSummary.Columns.Add("Type");

                qry1 = @"SELECT DISTINCT " + (Type == "Billwise" ? "FORMAT(PURM.PURDATE,'" + Variables.format + "')" : "PURM.SUPCODE") + " AS CL" + (Type == "Billwise" ? ", PURM.PURDATE" : ", SUPMAS.SUPNAME") + " FROM ((PURM INNER JOIN PURT ON PURM.VRNO = PURT.VRNO) LEFT JOIN SUPMAS ON SUPMAS.ACCID = PURM.SUPCODE) WHERE PURM.PURDATE BETWEEN " + d + clsDate.DateFrom + d + " AND " + d + clsDate.ToDate + d + (clsPurM.PayMode == 0 ? "" : " AND PURM.PAYMODE=" + clsPurM.PayMode) + " AND PURM.RETURNFLG=" + f + " AND PURM.VRTYPE='P'" + (SupId == 0 ? "" : " AND PURM.SUPCODE=" + SupId) + (GdwnCD == 0 ? "" : " AND PURT.GDWCODE=" + GdwnCD) + (PartyGrp == 0 ? "" : " AND IIF(SUPMAS.PARTYGRPID IS NULL, 0,SUPMAS.PARTYGRPID)=" + PartyGrp) + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + " ORDER BY " + (Type == "Billwise" ? "PURM.PURDATE" : "SUPMAS.SUPNAME");

                DataTable dt = await db.GetDatatable(qry1);
                DataTable dtDet = new DataTable();

                if (dt.Rows.Count > 0)
                {
                    int i = 0;
                    do
                    {
                        string qry2 = "";
                        DataRow dr = dt.Rows[i];

                        if (Type == "Billwise")
                            PurDate = Convert.ToDateTime(dr["CL"]).ToString(Variables.format);
                        else
                            SupCode = Convert.ToInt32(dr["CL"]);

                        qry2 = @"SELECT DISTINCT PURM.ORDNO,PURM.PURDATE,ACCMAS.ACCNAME,PURM.CANCELFLG,PURM.VRNO,PURM." + Functions.VSDOCNO() + " AS INVNOTEXT,PURM.BILLNO,PURM.BILLDATE,PURM.VRNO FROM (((PURM INNER JOIN ACCMAS ON PURM.SUPCODE=ACCMAS.ID) INNER JOIN PURT ON PURM.VRNO = PURT.VRNO) LEFT JOIN SUPMAS ON SUPMAS.ACCID = PURM.SUPCODE) WHERE " + (Type == "Billwise" ? ("PURM.PURDATE=" + d + PurDate + d) : ("PURM.PURDATE BETWEEN " + d + clsDate.DateFrom + d + " AND " + d + clsDate.ToDate + d)) + (clsPurM.PayMode == 0 ? "" : " AND PURM.PAYMODE=" + clsPurM.PayMode) + " AND PURM.RETURNFLG=" + f + " AND PURM.VRTYPE='P'" + (Type == "Supplierwise" ? " AND PURM.SUPCODE=" + SupCode : (SupId == 0 ? "" : " AND PURM.SUPCODE=" + SupId)) + (GdwnCD == 0 ? "" : " AND PURT.GDWCODE=" + GdwnCD) + (PartyGrp == 0 ? "" : " AND IIF(SUPMAS.PARTYGRPID IS NULL, 0,SUPMAS.PARTYGRPID)=" + PartyGrp) + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + " ORDER BY PURM.VRNO";

                        DataTable DT = await db.GetDatatable(qry2);

                        if (DT.Rows.Count > 0)
                        {
                            int j = 0;
                            do
                            {
                                string qry3 = "";
                                DataRow DR = DT.Rows[j];

                                if (Convert.ToBoolean(DR["CANCELFLG"]) == true)
                                {
                                    BilPurSummary.Rows.Add(Convert.ToDateTime(DR["PURDATE"]).ToString(Variables.format), DR["INVNOTEXT"], Convert.ToDateTime(DR["BILLDATE"]).ToString(Variables.format), DR["BILLNO"], DR["ACCNAME"].ToString() + " - Cancelled", null, null, null, null, DR["VRNO"]);
                                    j++;
                                }
                                else
                                {
                                    BillNo = Convert.ToInt32(DR["ORDNO"]);

                                    qry3 = @"SELECT FORMAT(PURM.PURDATE,'" + Variables.format + "') AS VoucherDate, PURM." + Functions.VSDOCNO() + " AS VoucherNo, FORMAT(PURM.BILLDATE,'" + Variables.format + "') as BillDate, PURM.BILLNO as BillNo, ACCMAS.ACCNAME AS SupplierName, '' AS ProductName, '' AS Qty, '' AS Rate, FORMAT((SUM(IIF(PURM.INFORMAT<>3,PURT.QTY * (PURT.PRRATE-PURT.DISC-PURT.LDISCAMT),PURT.QTY * ((PURT.PRRATE/(1+(PURT.TAX/100)))-PURT.DISC-PURT.LDISCAMT)))),'" + Variables.DecString + "') AS TValue,PURM.VRNO AS VrNo FROM ((PURM INNER JOIN ACCMAS ON PURM.SUPCODE = ACCMAS.ID) INNER JOIN PURT ON PURM.ORDNO = PURT.ORDNO) WHERE PURM.ORDNO=" + BillNo + " AND PURM.RETURNFLG=" + f + " AND PURM.VRTYPE='P'" + (GdwnCD == 0 ? "" : " AND PURT.GDWCODE=" + GdwnCD) + " GROUP BY PURM.ORDNO, PURM.PURDATE, ACCMAS.ACCNAME,PURM.VRNO,PURM." + Functions.VSDOCNO() + @",PURM.BILLNO,PURM.BILLDATE
                             UNION ALL 
                             SELECT '' as VoucherDate, '' as VoucherNo, '' AS BillDate, '' AS BillNo, '' AS SupplierName, PRMAST.PRNAME AS ProductName, FORMAT(PURT.QTY,'" + Variables.QtyDecString + "') AS Qty, FORMAT(IIF(PURM.INFORMAT<>3,PURT.PRRATE,(PURT.PRRATE/(1+(PURT.TAX/100))))-PURT.DISC-PURT.LDISCAMT,'" + Variables.DecString + "') AS Rate, FORMAT(IIF(PURM.INFORMAT<>3,PURT.QTY * (PURT.PRRATE-PURT.DISC-PURT.LDISCAMT),PURT.QTY * ((PURT.PRRATE/(1+(PURT.TAX/100)))-PURT.DISC-PURT.LDISCAMT)),'" + Variables.DecString + "') AS TValue,PURT.VRNO AS VrNo FROM ((PRMAST INNER JOIN PURT ON PRMAST.PRCODE = PURT.PRCODE) INNER JOIN PURM ON PURT.VRNO = PURM.VRNO) WHERE PURT.ORDNO = " + BillNo + " AND PURT.RETURNFLG=" + f + " AND PURT.VRTYPE='P'" + (GdwnCD == 0 ? "" : " AND PURT.GDWCODE=" + GdwnCD);

                                    dtDet = await db.GetDatatable(qry3);

                                    j++;
                                    BilPurSummary.Merge(dtDet, true, MissingSchemaAction.Ignore);
                                }
                            }
                            while (j < DT.Rows.Count);
                        }

                        if (!RemGrpTot)
                        {
                            string qry4 = @"SELECT '' as VoucherDate, '' as VoucherNo, '' AS BillDate,'' AS BillNo, " + arabic + "'" + (Type == "Billwise" ? LngTB.Day_Total : LngTB.Supplier_Total) + "' AS SupplierName,'' AS ProductName, '' AS Qty, '' AS Rate,IIF(SUM(PURT.QTY * (PURT.PRRATE-PURT.DISC-PURT.LDISCAMT))=0,NULL,FORMAT(SUM(IIF(PURM.INFORMAT<>3,PURT.QTY * (PURT.PRRATE-PURT.DISC-PURT.LDISCAMT),PURT.QTY * ((PURT.PRRATE/(1+(PURT.TAX/100)))-PURT.DISC-PURT.LDISCAMT))),'" + Variables.DecString + "')) AS TValue,max(PURT.VRNO) AS VrNo,'B' as Type FROM ((PURM INNER JOIN PURT ON PURM.VRNO = PURT.VRNO) LEFT JOIN SUPMAS ON SUPMAS.ACCID = PURM.SUPCODE) WHERE " + (Type == "Billwise" ? ("PURM.PURDATE=" + d + PurDate + d) : ("PURM.PURDATE BETWEEN " + d + clsDate.DateFrom + d + " AND " + d + clsDate.ToDate + d)) + (clsPurM.PayMode == 0 ? "" : " AND PURM.PAYMODE=" + clsPurM.PayMode) + " AND PURM.RETURNFLG=" + f + " AND PURM.VRTYPE='P'" + (Type == "Supplierwise" ? " AND PURM.SUPCODE=" + SupCode : (SupId == 0 ? "" : " AND PURM.SUPCODE=" + SupId)) + (GdwnCD == 0 ? "" : " AND PURT.GDWCODE=" + GdwnCD) + (PartyGrp == 0 ? "" : " AND IIF(SUPMAS.PARTYGRPID IS NULL, 0,SUPMAS.PARTYGRPID)=" + PartyGrp) + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true);

                            DataTable dtTot = await db.GetDatatable(qry4);
                            BilPurSummary.Merge(dtTot, true, MissingSchemaAction.Ignore);
                        }

                        i++;
                    }
                    while (i < dt.Rows.Count);

                    if (!RemGrpTot)
                        BilPurSummary.Rows.Add();

                    string qry5 = @"SELECT '' as VoucherDate, '' as VoucherNo,'' AS BillDate, '' AS BillNo, " + arabic + "'" + LngTB.Grand_Total + "' AS SupplierName,'' AS ProductName, '' AS Qty, '' AS Rate,IIF(SUM( PURT.QTY * (PURT.PRRATE-PURT.DISC-PURT.LDISCAMT))=0,NULL,FORMAT(SUM(IIF(PURM.INFORMAT<>3,PURT.QTY * (PURT.PRRATE-PURT.DISC-PURT.LDISCAMT),PURT.QTY * ((PURT.PRRATE/(1+(PURT.TAX/100)))-PURT.DISC-PURT.LDISCAMT))),'" + Variables.DecString + "')) AS TValue,'B' As Type FROM ((PURM INNER JOIN PURT ON PURM.VRNO = PURT.VRNO) LEFT JOIN SUPMAS ON SUPMAS.ACCID = PURM.SUPCODE) WHERE PURM.PURDATE BETWEEN " + d + clsDate.DateFrom + d + " AND " + d + clsDate.ToDate + d + (clsPurM.PayMode == 0 ? "" : " AND PURM.PAYMODE=" + clsPurM.PayMode) + " AND PURM.RETURNFLG=" + f + " AND PURM.VRTYPE='P'" + (SupId == 0 ? "" : " AND PURM.SUPCODE=" + SupId) + (GdwnCD == 0 ? "" : " AND PURT.GDWCODE=" + GdwnCD) + (PartyGrp == 0 ? "" : " AND IIF(SUPMAS.PARTYGRPID IS NULL, 0,SUPMAS.PARTYGRPID)=" + PartyGrp) + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + "";

                    DataTable DTTOT = await db.GetDatatable(qry5);
                    BilPurSummary.Merge(DTTOT, true, MissingSchemaAction.Ignore);
                }

                return BilPurSummary;
            }
            catch (Exception ex) { return BilPurSummary; }
        }

        public async Task<DataTable> ProductwisePurcahseSummary(ClsDate clsDate, ClsPurM clsPurM, string Type, int PrId, int GrpId, string CmpName, int CatgId, int GdwnCD, int SuppId, int PartyGrp, bool Unapproved)
        {
            DataTable PrPurchaseSummary = new DataTable();
            PrPurchaseSummary.Columns.Add("Alias");
            PrPurchaseSummary.Columns.Add("ProductName");
            PrPurchaseSummary.Columns.Add("PrGroup");
            PrPurchaseSummary.Columns.Add("Qty");
            PrPurchaseSummary.Columns.Add("Amount");
            PrPurchaseSummary.Columns.Add("Discount");
            PrPurchaseSummary.Columns.Add("Tax");
            PrPurchaseSummary.Columns.Add("NetAmount");
            PrPurchaseSummary.Columns.Add("PrCode");
            try
            {
                string d = "'";
                string f = "'FALSE'";
                string t = "'TRUE'";
                string tot = "N'" + LngTB.Total + "'";

                if (Type == "Product")
                    qry = @"SELECT PRMAST.ALIAS AS Alias, PRMAST.PRNAME AS ProductName, PRGRPMAS.GRPNAME AS PrGroup, Format(Sum(PURT.QTY),'" + Variables.QtyDecString + "') AS Qty, Format(Sum(PURT.QTY*( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))),'" + Variables.DecString + "') AS Amount, Format(Sum(PURT.QTY *(PURT.DISC+PURT.LDISCAMT)),'" + Variables.DecString + "') AS Discount, Format(Sum(PURT.QTY*( IIF(PURM.INFORMAT=3, ((PURT.PRRATE/(1+(PURT.TAX/100)))-PURT.DISC-PURT.LDISCAMT), (PURT.PRRATE-PURT.DISC-PURT.LDISCAMT)))*PURT.TAX/100),'" + Variables.DecString + "') AS Tax, Format(Sum(PURT.QTY*(( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))-(PURT.DISC+PURT.LDISCAMT))+PURT.QTY*(( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))-(PURT.DISC+PURT.LDISCAMT))*PURT.TAX/100),'" + Variables.DecString + "') AS NetAmount,PRMAST.PRCODE AS PrCode FROM ((((PRGRPMAS INNER JOIN (PRMAST INNER JOIN PURT ON PRMAST.PRCODE = PURT.PRCODE) ON PRGRPMAS.PRGRPID = PRMAST.GROUPCODE) LEFT JOIN CATGMAS ON PRMAST.CATGCODE = CATGMAS.CATGCODE) INNER JOIN PURM ON PURM.VRNO = PURT.VRNO) LEFT JOIN SUPMAS ON PURT.SUPCODE = SUPMAS.ACCID) WHERE (PURT.PURDATE BETWEEN " + d + clsDate.DateFrom + d + " AND " + d + clsDate.ToDate + d + ") AND (PURT.RETURNFLG=" + (clsPurM.ReturnFlg ? t : f) + ") AND (PURT.VRTYPE='" + clsPurM.VrType + "') AND (PURT.PRCODE=" + PrId + " OR " + PrId + "=0) AND (PRGRPMAS.PRGRPID=" + GrpId + " OR " + GrpId + "=0)" + (CmpName == null ? "" : " AND (PRMAST.BRAND='" + CmpName + "')") + " AND (PRMAST.CATGCODE=" + CatgId + " OR " + CatgId + "=0) AND (PURT.GDWCODE=" + GdwnCD + " OR " + GdwnCD + "=0) AND (PURT.SUPCODE=" + SuppId + " OR " + SuppId + "=0) AND (IIF(SUPMAS.PARTYGRPID IS NULL, 0,SUPMAS.PARTYGRPID)=" + PartyGrp + " OR " + PartyGrp + "=0) AND (PURT.CANCELFLG=" + f + ")" + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + @" GROUP BY PRMAST.PRNAME,PRMAST.ALIAS, PRGRPMAS.GRPNAME,PRMAST.PRCODE 
                UNION ALL SELECT '' AS Alias, " + tot + @" AS ProductName, '' AS PrGroup, '' AS Qty, Format(Sum(PURT.QTY*( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))),'" + Variables.DecString + "') AS Amount, Format(Sum(PURT.QTY *(PURT.DISC+PURT.LDISCAMT)),'" + Variables.DecString + "') AS Discount, Format(Sum(PURT.QTY*( IIF(PURM.INFORMAT=3, ((PURT.PRRATE/(1+(PURT.TAX/100)))-PURT.DISC-PURT.LDISCAMT), (PURT.PRRATE-PURT.DISC-PURT.LDISCAMT)))*PURT.TAX/100),'" + Variables.DecString + "') AS Tax, Format(Sum(PURT.QTY*(( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))-(PURT.DISC+PURT.LDISCAMT))+PURT.QTY*(( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))-(PURT.DISC+PURT.LDISCAMT))*PURT.TAX/100),'" + Variables.DecString + "') AS NetAmount,NULL AS PrCode FROM ((((PRGRPMAS INNER JOIN (PRMAST INNER JOIN PURT ON PRMAST.PRCODE = PURT.PRCODE) ON PRGRPMAS.PRGRPID = PRMAST.GROUPCODE) LEFT JOIN CATGMAS ON PRMAST.CATGCODE = CATGMAS.CATGCODE) INNER JOIN PURM ON PURM.VRNO = PURT.VRNO) LEFT JOIN SUPMAS ON PURT.SUPCODE = SUPMAS.ACCID) WHERE (PURT.PURDATE BETWEEN " + d + clsDate.DateFrom + d + " AND " + d + clsDate.ToDate + d + ") AND (PURT.RETURNFLG=" + (clsPurM.ReturnFlg ? t : f) + ") AND (PURT.VRTYPE='" + clsPurM.VrType + "') AND (PURT.PRCODE=" + PrId + " OR " + PrId + "=0) AND (PRGRPMAS.PRGRPID=" + GrpId + " OR " + GrpId + "=0)" + (CmpName == null ? "" : " AND (PRMAST.BRAND='" + CmpName + "')") + " AND (PRMAST.CATGCODE=" + CatgId + " OR " + CatgId + "=0) AND (PURT.GDWCODE=" + GdwnCD + " OR " + GdwnCD + "=0) AND (PURT.SUPCODE=" + SuppId + " OR " + SuppId + "=0) AND (IIF(SUPMAS.PARTYGRPID IS NULL, 0,SUPMAS.PARTYGRPID)=" + PartyGrp + " OR " + PartyGrp + "=0) AND (PURT.CANCELFLG=" + f + ")" + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + "";
                else
                    qry = @"SELECT PRMAST.ALIAS AS Alias, PRMAST.PRNAME AS ProductName, PRGRPMAS.GRPNAME AS PrGroup, Format(Sum(PURT.QTY),'" + Variables.QtyDecString + "') AS Qty, Format(Sum(PURT.QTY*( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))),'" + Variables.DecString + "') AS Amount, Format(Sum(PURT.QTY *(PURT.DISC+PURT.LDISCAMT)),'" + Variables.DecString + "') AS Discount, Format(Sum(PURT.QTY*( IIF(PURM.INFORMAT=3, ((PURT.PRRATE/(1+(PURT.TAX/100)))-PURT.DISC-PURT.LDISCAMT), (PURT.PRRATE-PURT.DISC-PURT.LDISCAMT)))*PURT.TAX/100),'" + Variables.DecString + "') AS Tax, Format(Sum(PURT.QTY*(( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))-(PURT.DISC+PURT.LDISCAMT))+PURT.QTY*(( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))-(PURT.DISC+PURT.LDISCAMT))*PURT.TAX/100),'" + Variables.DecString + "') AS NetAmount,PRMAST.PRCODE AS PrCode FROM ((((PRGRPMAS INNER JOIN (PRMAST INNER JOIN PURT ON PRMAST.PRCODE = PURT.PRCODE) ON PRGRPMAS.PRGRPID = PRMAST.GROUPCODE) LEFT JOIN CATGMAS ON PRMAST.CATGCODE = CATGMAS.CATGCODE) INNER JOIN PURM ON PURM.VRNO = PURT.VRNO) LEFT JOIN SUPMAS ON PURT.SUPCODE = SUPMAS.ACCID) WHERE (PURT.PURDATE BETWEEN " + d + clsDate.DateFrom + d + " AND " + d + clsDate.ToDate + d + ") AND (PURT.RETURNFLG=" + (clsPurM.ReturnFlg ? t : f) + ") AND (PURT.VRTYPE='" + clsPurM.VrType + "') AND (PURT.PRCODE=" + PrId + " OR " + PrId + "=0) AND (PRGRPMAS.PRGRPID=" + GrpId + " OR " + GrpId + "=0)" + (CmpName == null ? "" : " AND (PRMAST.BRAND='" + CmpName + "')") + " AND (PRMAST.CATGCODE=" + CatgId + " OR " + CatgId + "=0) AND (PURT.GDWCODE=" + GdwnCD + " OR " + GdwnCD + "=0) AND (PURT.SUPCODE=" + SuppId + " OR " + SuppId + "=0) AND (IIF(SUPMAS.PARTYGRPID IS NULL, 0,SUPMAS.PARTYGRPID)=" + PartyGrp + " OR " + PartyGrp + "=0) AND (PURT.CANCELFLG=" + f + ")" + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + @" GROUP BY PRGRPMAS.GRPNAME,PRMAST.ALIAS, PRMAST.PRNAME,PRMAST.PRCODE
                UNION ALL SELECT '' AS Alias, " + tot + @" AS ProductName, '' AS PrGroup, '' AS Qty, Format(Sum(PURT.QTY*( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))),'" + Variables.DecString + "') AS Amount, Format(Sum(PURT.QTY *(PURT.DISC+PURT.LDISCAMT)),'" + Variables.DecString + "') AS Discount, Format(Sum(PURT.QTY*( IIF(PURM.INFORMAT=3, ((PURT.PRRATE/(1+(PURT.TAX/100)))-PURT.DISC-PURT.LDISCAMT), (PURT.PRRATE-PURT.DISC-PURT.LDISCAMT)))*PURT.TAX/100),'" + Variables.DecString + "') AS Tax, Format(Sum(PURT.QTY*(( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))-(PURT.DISC+PURT.LDISCAMT))+PURT.QTY*(( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))-(PURT.DISC+PURT.LDISCAMT))*PURT.TAX/100),'" + Variables.DecString + "') AS NetAmount,NULL AS PrCode FROM ((((PRGRPMAS INNER JOIN (PRMAST INNER JOIN PURT ON PRMAST.PRCODE = PURT.PRCODE) ON PRGRPMAS.PRGRPID = PRMAST.GROUPCODE) LEFT JOIN CATGMAS ON PRMAST.CATGCODE = CATGMAS.CATGCODE) INNER JOIN PURM ON PURM.VRNO = PURT.VRNO) LEFT JOIN SUPMAS ON PURT.SUPCODE = SUPMAS.ACCID) WHERE (PURT.PURDATE BETWEEN " + d + clsDate.DateFrom + d + " AND " + d + clsDate.ToDate + d + ") AND (PURT.RETURNFLG=" + (clsPurM.ReturnFlg ? t : f) + ") AND (PURT.VRTYPE='" + clsPurM.VrType + "') AND (PURT.PRCODE=" + PrId + " OR " + PrId + "=0) AND (PRGRPMAS.PRGRPID=" + GrpId + " OR " + GrpId + "=0)" + (CmpName == null ? "" : " AND (PRMAST.BRAND='" + CmpName + "')") + " AND (PRMAST.CATGCODE=" + CatgId + " OR " + CatgId + "=0) AND (PURT.GDWCODE=" + GdwnCD + " OR " + GdwnCD + "=0) AND (PURT.SUPCODE=" + SuppId + " OR " + SuppId + "=0) AND (IIF(SUPMAS.PARTYGRPID IS NULL, 0,SUPMAS.PARTYGRPID)=" + PartyGrp + " OR " + PartyGrp + "=0) AND (PURT.CANCELFLG=" + f + ")" + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + "";

                DataTable DT = await db.GetDatatable(qry);
                PrPurchaseSummary.Merge(DT, true, MissingSchemaAction.Ignore);
                return PrPurchaseSummary;
            }
            catch (Exception ex)
            {
                return PrPurchaseSummary;
            }
        }

        public async Task<DataTable> GroupwisePurchaseSummary(ClsDate clsDate, ClsPurM clsPurM, int GdwnCD, int SuppId, int PartyGrp, bool Unapproved)
        {
            DataTable GrpPurSummary = new DataTable();
            try
            {
                GrpPurSummary.Columns.Add("GroupName");
                GrpPurSummary.Columns.Add("Amount");
                GrpPurSummary.Columns.Add("Discount");
                GrpPurSummary.Columns.Add("Tax");
                GrpPurSummary.Columns.Add("NetAmount");
                GrpPurSummary.Columns.Add("GroupId");

                qry = @"SELECT PRGRPMAS.GRPNAME AS GroupName,Format(Sum(PURT.QTY*( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))),'" + Variables.DecString + "') AS Amount,Format(Sum(PURT.QTY * (PURT.DISC+PURT.LDISCAMT)),'" + Variables.DecString + @"') AS Discount,Format(Sum(PURT.QTY*( IIF(PURM.INFORMAT=3, ((PURT.PRRATE/(1+(PURT.TAX/100)))-PURT.DISC-PURT.LDISCAMT), (PURT.PRRATE-PURT.DISC-PURT.LDISCAMT)))*PURT.TAX/100),'" + Variables.DecString + @"') AS Tax, 
            Format(Sum(PURT.QTY*(( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))-(PURT.DISC+PURT.LDISCAMT))+PURT.QTY*(( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))-(PURT.DISC+PURT.LDISCAMT))*PURT.TAX/100),'" + Variables.DecString + @"') AS NetAmount,PRGRPMAS.PRGRPID AS GroupId
            FROM ((((PURT INNER JOIN PRMAST ON PURT.PRCODE = PRMAST.PRCODE) INNER JOIN PRGRPMAS ON PRMAST.GROUPCODE = PRGRPMAS.PRGRPID) INNER JOIN PURM ON PURM.VRNO = PURT.VRNO) LEFT JOIN SUPMAS ON PURT.SUPCODE = SUPMAS.ACCID) WHERE PURT.PURDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "' AND PURT.RETURNFLG='" + clsPurM.ReturnFlg + "' AND PURT.VRTYPE='" + clsPurM.VrType + "' AND (PURT.GDWCODE=" + GdwnCD + " OR " + GdwnCD + "=0) AND (PURT.SUPCODE=" + SuppId + " OR " + SuppId + "=0) AND (IIF(SUPMAS.PARTYGRPID IS NULL, 0,SUPMAS.PARTYGRPID)=" + PartyGrp + " OR " + PartyGrp + "=0)" + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + @" GROUP BY PRGRPMAS.GRPNAME,PRGRPMAS.PRGRPID 
            UNION ALL 
            SELECT N'" + LngTB.Total + "' AS GroupName,Format(Sum(PURT.QTY*( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))),'" + Variables.DecString + "') AS Amount,Format(Sum(PURT.QTY * (PURT.DISC+PURT.LDISCAMT)),'" + Variables.DecString + @"') AS Discount,Format(Sum(PURT.QTY*( IIF(PURM.INFORMAT=3, ((PURT.PRRATE/(1+(PURT.TAX/100)))-PURT.DISC-PURT.LDISCAMT), (PURT.PRRATE-PURT.DISC-PURT.LDISCAMT)))*PURT.TAX/100),'" + Variables.DecString + @"') AS Tax, Format(Sum(PURT.QTY*(( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))-(PURT.DISC+PURT.LDISCAMT))+PURT.QTY*(( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))-(PURT.DISC+PURT.LDISCAMT))*PURT.TAX/100),'" + Variables.DecString + @"') AS NetAmount,NULL AS GroupId 
            FROM ((PURT INNER JOIN PURM ON PURM.VRNO = PURT.VRNO) LEFT JOIN SUPMAS ON PURT.SUPCODE = SUPMAS.ACCID) WHERE PURT.PURDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "' AND PURT.RETURNFLG='" + clsPurM.ReturnFlg + "' AND PURT.VRTYPE='" + clsPurM.VrType + "' AND (PURT.GDWCODE=" + GdwnCD + " OR " + GdwnCD + "=0) AND (PURT.SUPCODE=" + SuppId + " OR " + SuppId + "=0) AND (IIF(SUPMAS.PARTYGRPID IS NULL, 0,SUPMAS.PARTYGRPID)=" + PartyGrp + " OR " + PartyGrp + "=0)" + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + "";

                DataTable DT = await db.GetDatatable(qry);
                GrpPurSummary.Merge(DT, true, MissingSchemaAction.Ignore);

                return GrpPurSummary;
            }
            catch (Exception ex)
            {
                return GrpPurSummary;
            }
        }

        public async Task<DataTable> LedgerGroupwisePurchaseSummary(ClsDate clsDate, ClsPurM clsPurM, bool Unapproved)

        {
            DataTable GrpPurSummary = new DataTable();

            try
            {
                GrpPurSummary.Columns.Add("GroupName");
                GrpPurSummary.Columns.Add("Amount");
                GrpPurSummary.Columns.Add("Discount");
                GrpPurSummary.Columns.Add("Tax");
                GrpPurSummary.Columns.Add("NetAmount");
                GrpPurSummary.Columns.Add("GroupId");

                qry = @"SELECT GRPLDG.GROUPNAME AS GroupName,Format(Sum(PURT.QTY*( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))),'" + Variables.DecString + "') AS Amount,Format(Sum(PURT.QTY * (PURT.DISC+PURT.LDISCAMT)),'" + Variables.DecString + @"') AS Discount,Format(Sum(PURT.QTY*( IIF(PURM.INFORMAT=3, ((PURT.PRRATE/(1+(PURT.TAX/100)))-PURT.DISC-PURT.LDISCAMT), (PURT.PRRATE-PURT.DISC-PURT.LDISCAMT)))*PURT.TAX/100),'" + Variables.DecString + @"') AS Tax, 
               Format(Sum(PURT.QTY*(( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))-(PURT.DISC+PURT.LDISCAMT))+PURT.QTY*(( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))-(PURT.DISC+PURT.LDISCAMT))*PURT.TAX/100),'" + Variables.DecString + @"') AS NetAmount,GRPLDG.GROUPID AS GroupId
               FROM (((PURM INNER JOIN ACCMAS ON PURM.SUPCODE=ACCMAS.ID) INNER JOIN GRPLDG ON ACCMAS.MGROUP=GRPLDG.GROUPID)INNER JOIN PURT ON PURT.VRNO=PURM.VRNO) WHERE (PURT.PURDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "') AND (PURT.RETURNFLG='" + clsPurM.ReturnFlg + "') AND (PURT.VRTYPE='" + clsPurM.VrType + "')" + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + @" GROUP BY GRPLDG.GROUPNAME,GRPLDG.GROUPID 
               UNION ALL SELECT N'" + LngTB.Total + "' AS GroupName,Format(Sum(PURT.QTY*( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))),'" + Variables.DecString + "') AS Amount,Format(Sum(PURT.QTY *(PURT.DISC+PURT.LDISCAMT)),'" + Variables.DecString + @"') AS Discount,Format(Sum(PURT.QTY*( IIF(PURM.INFORMAT=3, ((PURT.PRRATE/(1+(PURT.TAX/100)))-PURT.DISC-PURT.LDISCAMT), (PURT.PRRATE-PURT.DISC-PURT.LDISCAMT)))*PURT.TAX/100),'" + Variables.DecString + @"') AS Tax, Format(Sum(PURT.QTY*(( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))-(PURT.DISC+PURT.LDISCAMT))+PURT.QTY*(( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))-(PURT.DISC+PURT.LDISCAMT))*PURT.TAX/100),'" + Variables.DecString + @"') AS NetAmount,NULL AS GroupId 
               FROM (((PURM INNER JOIN ACCMAS ON PURM.SUPCODE=ACCMAS.ID) INNER JOIN GRPLDG ON ACCMAS.MGROUP=GRPLDG.GROUPID)INNER JOIN PURT ON PURT.VRNO=PURM.VRNO) WHERE (PURT.PURDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "') AND (PURT.RETURNFLG='" + clsPurM.ReturnFlg + "') AND (PURT.VRTYPE='" + clsPurM.VrType + "')" + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + "";

                DataTable DT = await db.GetDatatable(qry);
                GrpPurSummary.Merge(DT, true, MissingSchemaAction.Ignore);

                return GrpPurSummary;
            }
            catch (Exception ex)
            {
                return GrpPurSummary;
            }
        }

        public async Task<DataTable> PurchaseDetailReport(ClsDate clsDate, ClsPurT clsPurT, bool RemGrpTot, bool Unapproved)

        {
            string d = "'";
            string f = "'FALSE'";
            string iv = "N'" + LngTB.Invoice_Value + "'";
            string gt = "N'" + LngTB.Grand_Total + "'";

            qry = @"SELECT VRNO,VRTEXT,VRDATE,[DESC],QTY,UOM,AMT,DISC,TAX,NETAMT FROM (

             SELECT 1 AS ORD,PURM.VRNO AS VRNO,'' AS VRTEXT,'' AS VRDATE,'' AS [DESC],'' AS QTY,'' AS UOM,'' AS AMT, '' AS DISC,'' AS TAX, '' AS NETAMT 
             FROM PURM INNER JOIN ACCMAS ON PURM.SUPCODE = ACCMAS.ID WHERE PURM.PURDATE BETWEEN " + d + clsDate.DateFrom + d + " AND " + d + clsDate.ToDate + d + " AND PURM.VRTYPE='" + clsPurT.VrType + "' AND PURM.CANCELFLG=" + f + (clsPurT.SupCode > 0 ? " AND PURM.SUPCODE = " + clsPurT.SupCode : "") + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + @"

             UNION ALL

             SELECT 2 AS ORD,PURM.VRNO AS VRNO,PURM.INVNOTEXT AS VRTEXT,Format(PURM.PURDATE,'" + Variables.format + @"') AS VRDATE,ACCMAS.ACCNAME AS [DESC],'' AS QTY,'' AS UOM,'' AS AMT, '' AS DISC,'' AS TAX, '' AS NETAMT 
             FROM PURM INNER JOIN ACCMAS ON PURM.SUPCODE = ACCMAS.ID WHERE PURM.PURDATE BETWEEN " + d + clsDate.DateFrom + d + " AND " + d + clsDate.ToDate + d + " AND PURM.VRTYPE='" + clsPurT.VrType + "' AND PURM.CANCELFLG=" + f + (clsPurT.SupCode > 0 ? " AND PURM.SUPCODE = " + clsPurT.SupCode : "") + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + @"

               UNION ALL

             SELECT 3 AS ORD,PURT.VRNO AS VRNO,'' AS VRTEXT,'' AS VRDATE,PRMAST.PRNAME AS [DESC], Format((PURT.QTY),'" + Variables.QtyDecString + @"') AS QTY, PRUNITMAS.PRUNITNAME AS UOM
                , Format((PURT.QTY*(IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))),'" + Variables.DecString + "') AS AMT, Format((PURT.QTY*(PURT.DISC+PURT.LDISCAMT)),'" + Variables.DecString + @"') AS DISC
                , Format((PURT.QTY*(IIF(PURM.INFORMAT=3, ((PURT.PRRATE/(1+(PURT.TAX/100)))-PURT.DISC-PURT.LDISCAMT), (PURT.PRRATE-PURT.DISC-PURT.LDISCAMT)))*PURT.TAX/100),'" + Variables.DecString + @"') AS TAX
                , Format((PURT.QTY*(IIF(PURM.INFORMAT=3, ((PURT.PRRATE/(1+(PURT.TAX/100)))-PURT.DISC-PURT.LDISCAMT), (PURT.PRRATE-PURT.DISC-PURT.LDISCAMT)))+PURT.QTY*(IIF(PURM.INFORMAT=3, ((PURT.PRRATE/(1+(PURT.TAX/100)))-PURT.DISC-PURT.LDISCAMT), (PURT.PRRATE-PURT.DISC-PURT.LDISCAMT)))*PURT.TAX/100),'" + Variables.DecString + @"') AS NETAMT
             FROM (((PURM INNER JOIN PURT ON PURM.VRNO = PURT.VRNO) INNER JOIN PRMAST ON PURT.PRCODE = PRMAST.PRCODE) LEFT JOIN PRUNITMAS ON PURT.UNITCODE = PRUNITMAS.PRUNITID)
             WHERE PURT.PURDATE BETWEEN " + d + clsDate.DateFrom + d + " AND " + d + clsDate.ToDate + d + " AND PURT.VRTYPE='" + clsPurT.VrType + "' AND PURT.CANCELFLG=" + f + (clsPurT.SupCode > 0 ? " AND PURT.SUPCODE = " + clsPurT.SupCode : "") + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + @"

             UNION ALL

             SELECT 4 AS ORD,VRNO AS VRNO,'' AS VRTEXT,'' AS VRDATE," + iv + " AS [DESC],'' AS QTY,'' AS UOM,'' AS AMT, '' AS DISC,'' AS TAX, FORMAT(IIF(PURM.VRTYPE = 'P', AMOUNT, NTVALUE),'" + Variables.DecString + "') AS NETAMT FROM PURM WHERE PURM.PURDATE BETWEEN " + d + clsDate.DateFrom + d + " AND " + d + clsDate.ToDate + d + @" AND PURM.VRTYPE='" + clsPurT.VrType + "' AND PURM.CANCELFLG=" + f + (clsPurT.SupCode > 0 ? " AND PURM.SUPCODE = " + clsPurT.SupCode : "") + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + @"

             UNION ALL

             SELECT 5 AS ORD,MAX(VRNO)+1 AS VRNO,'' AS VRTEXT,'' AS VRDATE,'' AS [DESC],'' AS QTY,'' AS UOM,'' AS AMT, '' AS DISC,'' AS TAX, '' AS NETAMT FROM PURM WHERE PURM.PURDATE BETWEEN " + d + clsDate.DateFrom + d + " AND " + d + clsDate.ToDate + d + @" AND PURM.VRTYPE='" + clsPurT.VrType + "' AND PURM.CANCELFLG=" + f + (clsPurT.SupCode > 0 ? " AND PURM.SUPCODE = " + clsPurT.SupCode : "") + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + @"

             UNION ALL

             SELECT 6 AS ORD,MAX(VRNO)+1 AS VRNO,'' AS VRTEXT,'' AS VRDATE," + gt + " AS [DESC],'' AS QTY,'' AS UOM,'' AS AMT, '' AS DISC,'' AS TAX, FORMAT(SUM(IIF(PURM.VRTYPE = 'P', AMOUNT, NTVALUE)),'" + Variables.DecString + "') AS NETAMT FROM PURM WHERE PURM.PURDATE BETWEEN " + d + clsDate.DateFrom + d + " AND " + d + clsDate.ToDate + d + @" AND PURM.VRTYPE='" + clsPurT.VrType + "' AND PURM.CANCELFLG=" + f + (clsPurT.SupCode > 0 ? " AND PURM.SUPCODE = " + clsPurT.SupCode : "") + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true) + @"
                 ) DTLS" + (RemGrpTot ? " WHERE ORD NOT IN (1,4,5)" : "") + " ORDER BY VRNO,ORD";

            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> SupplierWiseProductReport(ClsDate clsDate, ClsPurM clsPurM, int PrId, int GrpId, string CmpName, int CatgId, int SupCode, int PartyGrp, bool Summary, bool LW, bool Unapproved)

        {
            DataTable PrPurchaseSummary = new DataTable();
            PrPurchaseSummary.Columns.Add("ProductName");
            PrPurchaseSummary.Columns.Add("SuppName");
            PrPurchaseSummary.Columns.Add("Alias");
            PrPurchaseSummary.Columns.Add("PrGroup");
            PrPurchaseSummary.Columns.Add("UOM");
            PrPurchaseSummary.Columns.Add("Qty");
            PrPurchaseSummary.Columns.Add("Amount");
            PrPurchaseSummary.Columns.Add("Discount");
            PrPurchaseSummary.Columns.Add("Tax");
            PrPurchaseSummary.Columns.Add("NetAmount");
            PrPurchaseSummary.Columns.Add("PrCode");

            try
            {
                string a = "N";
                string b = "'";
                string d = "'";

                string table = @"(((((PURM INNER JOIN PURT ON PURM.VRNO = PURT.VRNO) LEFT JOIN ACCMAS ON PURM.SUPCODE = ACCMAS.ID) INNER JOIN PRMAST ON PURT.PRCODE = PRMAST.PRCODE) INNER JOIN PRGRPMAS ON PRMAST.GROUPCODE = PRGRPMAS.PRGRPID) LEFT JOIN PRUNITMAS ON PRMAST.UNIT = PRUNITMAS.PRUNITID)"
                    + " WHERE PURM.PURDATE BETWEEN " + d + clsDate.DateFrom + d + " AND " + d + clsDate.ToDate + d + " AND PURM.RETURNFLG = " + b + clsPurM.ReturnFlg + b + " AND PURM.VRTYPE = '" + clsPurM.VrType + "' AND PURM.CANCELFLG = " + b + false + b
                    + (PrId == 0 ? "" : " AND PURT.PRCODE = " + PrId) + (GrpId == 0 ? "" : " AND PRMAST.GROUPCODE = " + GrpId) + (CmpName == null ? "" : " AND PRMAST.BRAND = '" + CmpName + "'") + (CatgId == 0 ? "" : " AND PRMAST.CATGCODE = " + CatgId)
                    + (SupCode == 0 ? "" : " AND PURM.SUPCODE = " + SupCode) + (PartyGrp > 0 ? " AND PURM.SUPCODE IN (SELECT SUPMAS.ACCID FROM SUPMAS WHERE SUPMAS.PARTYGRPID = " + PartyGrp + ")" : "") + Functions.APRVLR(true, Unapproved, "PURM") + Functions.VSSTMNT("PURM", true);


                string qry = @"SELECT ProductName, SuppName, Alias, PrGroup, UOM, Qty, Amount, Discount, Tax, NetAmount, PrCode FROM (" +
                (!LW ? "SELECT 1 AS ORDA, ACCMAS.ACCNAME AS ORDB,1 AS ORDC, ACCMAS.ACCNAME AS ProductName, '' AS SuppName, '' AS Alias, '' AS PrGroup, '' AS UOM," + (Summary ? " Format(Sum(PURT.QTY),'" + Variables.QtyDecString + "') AS Qty, Format(Sum(PURT.QTY*( IIF(PURM.INFORMAT = 3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))),'" + Variables.DecString + "') AS Amount, Format(Sum(PURT.QTY *(PURT.DISC+PURT.LDISCAMT)),'" + Variables.DecString + @"') AS Discount, Format(Sum(PURT.QTY*( IIF(PURM.INFORMAT=3, ((PURT.PRRATE/(1+(PURT.TAX/100)))-PURT.DISC-PURT.LDISCAMT), (PURT.PRRATE-PURT.DISC-PURT.LDISCAMT)))*PURT.TAX/100),'" + Variables.DecString + "') AS Tax, Format(Sum(PURT.QTY*(( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))-(PURT.DISC+PURT.LDISCAMT))+PURT.QTY*(( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))-(PURT.DISC+PURT.LDISCAMT))*PURT.TAX/100),'" + Variables.DecString + @"') AS NetAmount" : "  '' AS Qty,'' AS Amount, '' AS Discount, '' AS Tax, '' AS NetAmount") + ", 0 AS PrCode FROM " + table + @" GROUP BY ACCMAS.ACCNAME" : "") +
                (!Summary ? (!LW ? @"
                 UNION ALL
                   " : "") + @"SELECT 1 AS ORDA, ACCMAS.ACCNAME AS ORDB, 2 AS ORDC, PRMAST.PRNAME AS ProductName, " + (LW ? "ACCMAS.ACCNAME" : "''") + " AS SuppName, PRMAST.ALIAS AS Alias, PRGRPMAS.GRPNAME AS PrGroup, PRUNITMAS.PRUNITNAME AS UOM, Format(Sum(PURT.QTY),'" + Variables.QtyDecString + "') AS Qty, Format(Sum(PURT.QTY*( IIF(PURM.INFORMAT = 3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))),'" + Variables.DecString + "') AS Amount, Format(Sum(PURT.QTY *(PURT.DISC+PURT.LDISCAMT)),'" + Variables.DecString + @"') AS Discount, Format(Sum(PURT.QTY*( IIF(PURM.INFORMAT=3, ((PURT.PRRATE/(1+(PURT.TAX/100)))-PURT.DISC-PURT.LDISCAMT), (PURT.PRRATE-PURT.DISC-PURT.LDISCAMT)))*PURT.TAX/100),'" + Variables.DecString + "') AS Tax, Format(Sum(PURT.QTY*(( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))-(PURT.DISC+PURT.LDISCAMT))+PURT.QTY*(( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))-(PURT.DISC+PURT.LDISCAMT))*PURT.TAX/100),'" + Variables.DecString + @"') AS NetAmount,PRMAST.PRCODE AS PrCode FROM " + table + @" GROUP BY ACCMAS.ACCNAME, PRMAST.PRNAME, PRMAST.ALIAS, PRGRPMAS.GRPNAME, PRMAST.PRCODE, PRUNITMAS.PRUNITNAME" : "") +
                (!Summary && PrId == 0 ? @"
                 UNION ALL
                 SELECT 1 AS ORDA, ACCMAS.ACCNAME AS ORDB, 3 AS ORDC, '' AS ProductName, '' AS SuppName, '' AS Alias, " + a + "'" + (SupCode == 0 ? LngTB.Sub_Total : LngTB.Grand_Total) + "' AS PrGroup, '' AS UOM, '' AS Qty, Format(Sum(PURT.QTY*( IIF(PURM.INFORMAT = 3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))),'" + Variables.DecString + "') AS Amount, Format(Sum(PURT.QTY *(PURT.DISC+PURT.LDISCAMT)),'" + Variables.DecString + @"') AS Discount, Format(Sum(PURT.QTY*( IIF(PURM.INFORMAT=3, ((PURT.PRRATE/(1+(PURT.TAX/100)))-PURT.DISC-PURT.LDISCAMT), (PURT.PRRATE-PURT.DISC-PURT.LDISCAMT)))*PURT.TAX/100),'" + Variables.DecString + "') AS Tax, Format(Sum(PURT.QTY*(( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))-(PURT.DISC+PURT.LDISCAMT))+PURT.QTY*(( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))-(PURT.DISC+PURT.LDISCAMT))*PURT.TAX/100),'" + Variables.DecString + @"') AS NetAmount, 0 AS PrCode FROM " + table + @" GROUP BY ACCMAS.ACCNAME" : "") +
                (SupCode == 0 ? (@"
                 UNION ALL
                 SELECT 2 AS ORDA, '' AS ORDB, 1 AS ORDC, " + a + "'" + (Summary ? LngTB.Grand_Total : "") + "' AS ProductName, '' AS SuppName,'' AS Alias, " + a + "'" + (!Summary ? LngTB.Grand_Total : "") + "' AS PrGroup, '' AS UOM, " + (Summary ? "Format(Sum(PURT.QTY),'" + Variables.QtyDecString + "')" : "''") + " AS Qty, Format(Sum(PURT.QTY*( IIF(PURM.INFORMAT = 3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))),'" + Variables.DecString + "') AS Amount, Format(Sum(PURT.QTY *(PURT.DISC+PURT.LDISCAMT)),'" + Variables.DecString + @"') AS Discount, Format(Sum(PURT.QTY*( IIF(PURM.INFORMAT=3, ((PURT.PRRATE/(1+(PURT.TAX/100)))-PURT.DISC-PURT.LDISCAMT), (PURT.PRRATE-PURT.DISC-PURT.LDISCAMT)))*PURT.TAX/100),'" + Variables.DecString + "') AS Tax, Format(Sum(PURT.QTY*(( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))-(PURT.DISC+PURT.LDISCAMT))+PURT.QTY*(( IIF(PURM.INFORMAT=3, (PURT.PRRATE/(1+PURT.TAX/100)), PURT.PRRATE))-(PURT.DISC+PURT.LDISCAMT))*PURT.TAX/100),'" + Variables.DecString + @"') AS NetAmount, 0 AS PrCode FROM " + table) : "") + @") AS FINAL ORDER BY ORDA, ORDB, ORDC, ProductName";

                PrPurchaseSummary.Merge(
                    await db.GetDatatable(qry),
                    true,
                    MissingSchemaAction.Ignore);

                return PrPurchaseSummary;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
