using Microsoft.Data.SqlClient;

namespace QuickBalance.API.DB
{
    public class Function
    {
        public Variable Variables = new Variable();

        #region Static Functions
        public static string GetStr(object obj)
        {
            try
            {
                if (obj != null)
                    return obj.ToString();
            }
            catch { }
            return "";
        }

        public static int GetInt(object obj)
        {
            try
            {
                if (obj != null && obj.ToString() != "")
                    return Convert.ToInt32(obj);
            }
            catch { }
            return 0;
        }

        public static decimal GetDec(object obj)
        {
            try
            {
                if (obj != null && obj.ToString() != "")
                    return Convert.ToDecimal(obj);
            }
            catch { }
            return 0;
        }

        public DateTime GetDt(object obj)
        {
            try
            {
                if (obj != null && obj.ToString() != "")
                {
                    return Convert.ToDateTime(obj);
                }
            }
            catch { }
            return Variables.fyStartDate;
        }

        public string GetDtStr(object obj)
        {
            try
            {
                if (obj != null && obj.ToString() != "")
                {
                    return Convert.ToDateTime(obj).ToString(Variables.format);
                }
            }
            catch { }
            return "";
        }

        public static bool GetBool(object obj)
        {
            try
            {
                if (obj != null && obj.ToString() != "")
                {
                    return Convert.ToBoolean(obj);
                }
            }
            catch { }
            return false;
        }

        public static string ExactFilter(string input)
        {
            if (input == null) return string.Empty;

            // Escape single quotes
            input = input.Replace("'", "''");

            return input;
        }
        public static string GetConnString(string serverName, string databaseName, string userName, string password)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = serverName,
                InitialCatalog = databaseName,
                UserID = userName,
                Password = password,
                Encrypt = true,
                TrustServerCertificate = true,
                MultipleActiveResultSets = true
            };
            return builder.ConnectionString;
        }
        #endregion Static Functions

        #region VoucherSystem
        public string VSDOCNO() // IF VOUCHER SYSTEM IS ACTIVATED THEN RETURN NEW INVOICE NUMBER FIELD(VSDOCNO) INSTEAD OF OLD INVOICE NUMBER FIELD(INVNOTEXT).
        {
            return Variables.VRSYS ? "VSDOCNO" : "INVNOTEXT";
        }
        public string VSDOCNOT()// IF VOUCHER SYSTEM IS ACTIVATED THEN RETURN NEW INVOICE NUMBER FIELD(VSDOCNO) INSTEAD OF OLD INVOICE ORDER NUMBER FIELD(ORDNO).
        {
            return Variables.VRSYS ? "VSDOCNO" : "ORDNO";
        }
        public string VSDOCORD()// IF VOUCHER SYSTEM IS ACTIVATED THEN RETURN NEW ORDER NUMBER NUMBER FIELD(VSORDCD) INSTEAD OF OLD INVOICE ORDER NUMBER FIELD(ORDNO).
        {
            return Variables.VRSYS ? "VSORDCD" : "ORDNO";
        }
        public string RVSDOCNO(string Old, string New)// IF VOUCHER SYSTEM IS ACTIVATED THEN RETURN NEW INVOICE NUMBER INSTEAD OF OLD (MAINLY USING IT FOR INVENTORY, SALES, PURCHASE AND ACCOUNTS FORMS REPORT REGIONS).
        {
            return Variables.VRSYS ? New : Old;
        }
        public string VSSTMNT(string Table, bool AND)// TRANSACTIONS MAIN TABLE FILTER BY VOUCHER TYPE.
        {
            return Variables.VRSYS && Variables.VoucherTypeCodes != "" ? (AND ? " AND " : " ") + ((Table == "" ? "" : Table + ".") + "VSVRTYPE IN (" + Variables.VoucherTypeCodes + ") ") : "";
        }
        public string SUBVSSTMNT(string STab, string MTab, string SCol, string MCol) // TRANSACTIONS SUB TABLE FILTER BY VOUCHER TYPE.
        {
            return Variables.VRSYS && Variables.VoucherTypeCodes != "" ? " AND " + STab + "." + SCol + " IN (SELECT " + MTab + "." + MCol + " FROM " + MTab + " WHERE " + MTab + ".VSVRTYPE IN (" + Variables.VoucherTypeCodes + ")) " : "";
        }
        public string LIST_ALLOW_VR_STMNT(bool? AND, string Table) // FILTER VOUCHER MAIN TABLE BY USER BRANCH
        {
            return Variables.VRSYS && Variables.BranchYN ? ((AND == null ? "" : Convert.ToBoolean(AND) ? " AND" : " WHERE") + " IIF(" + Table + ".VSVRTYPE IS NULL, 0, " + Table + ".VSVRTYPE) IN (SELECT VRSYSTEM.ID FROM VRSYSTEM WHERE IIF(VRSYSTEM.BRCD IS NULL, 0, VRSYSTEM.BRCD) = " + (Variables.VRSYS_DEFBRANCH == 0 ? "-1" : Variables.VRSYS_DEFBRANCH.ToString()) + ") ") : "";
        }

        public string VRSYS_STK_OP_EXTRA(string Table1, string Col1, string Table2, string Col2)
        {
            string RetQry = "";
            if (Variables.VRSYS && Variables.BranchYN && Variables.VoucherTypeCodes != "")
                RetQry = " AND ( IIF(" + Table1 + "." + Col1 + " IS NULL,0," + Table1 + "." + Col1 + ")<> 0 OR" + VRSYS_ALLOWBR_ALIAS_REPORT(null, Table2, true, Table2, Col2, true) + ")";
            return RetQry;
        }

        public string VRSYS_ALLOWBR_ALIAS_REPORT(bool? AND, string Table, bool GetNil, string TableAlias, string ColAlias, bool IN, bool AllBranch = false)
        {
            string RetQry = "";
            if (Variables.VRSYS && Variables.BranchYN && (Variables.VoucherTypeCodes != "" || AllBranch))
            {
                string BrCode = Variables.VoucherTypeCodes;
                if (AllBranch)
                    BrCode = (Variables.VRSYS_DEFBRANCH == 0 ? "-1" : Variables.VRSYS_DEFBRANCH.ToString()) + "," + (Variables.VRSYS_ALLOWEDBRANCHES == "" ? "-1" : Variables.VRSYS_ALLOWEDBRANCHES);

                string Col = Table == "PRMAST" ? "PRCODE" : Table == "BOMMAS" ? "CODE" : Table == "GRPLDG" ? "GROUPID" : Table == "PRGRPMAS" ? "PRGRPID" : Table == "PRUNITMAS" ? "PRUNITID" : Table == "PRUNITCONV" || Table == "TAXMAS" || Table == "PRICECATMAS" || Table == "COSTCMAS" || Table == "CURRENCYMAS" || Table == "CURRCONV" ? "CODE" : Table == "GDWMAS" ? "GDWID" : Table == "GDWGRP" ? "GRPID" : Table == "CATGMAS" ? "CATGCODE" : Table == "SMGRP" ? "SMGRPCODE" : Table == "SMMAST" ? "SMCODE" : "ID";
                string Table2 = Table == "PRMAST" ? "PRDBRDT" : "OTHBRDT";

                RetQry = (AND == null ? "" : Convert.ToBoolean(AND) ? " AND" : " WHERE") + " " + TableAlias + "." + ColAlias + (IN ? "" : " NOT ") + " IN (SELECT DISTINCT " + Table + "." + Col + " FROM " + Table + " LEFT JOIN (SELECT " + Table2 + ".FORMID," + Table2 + ".BRID FROM " + Table2 + " WHERE " + Table2 + ".FORMTYPE='" + Table + "')" + Table2 + " ON " + Table + "." + Col + " = " + Table2 + ".FORMID WHERE " + (GetNil ? Table2 + ".FORMID IS NULL OR " : "") + Table2 + ".BRID IN (" + BrCode + ")) ";
            }
            return RetQry;
        }

        public string VRSYS_ALLOWBR(bool AND, string Table, bool GetNil) // FILTER MASTER LIST BY USER BRANCH
        {
            string RetQry = "";
            if (Variables.BranchYN)
            {
                string BrCode = Variables.VRSYS_OTHBRANCH != 0 ? Variables.VRSYS_OTHBRANCH.ToString() : (Variables.VRSYS_DEFBRANCH == 0 ? "-1" : Variables.VRSYS_DEFBRANCH.ToString());
                string Col = Table == "PRMAST" ? "PRCODE" : Table == "BOMMAS" ? "CODE" : Table == "GRPLDG" ? "GROUPID" : Table == "PRGRPMAS" ? "PRGRPID" : Table == "PRUNITMAS" ? "PRUNITID" : Table == "PRUNITCONV" || Table == "TAXMAS" || Table == "PRICECATMAS" || Table == "COSTCMAS" || Table == "CURRENCYMAS" || Table == "CURRCONV" ? "CODE" : Table == "GDWMAS" ? "GDWID" : Table == "GDWGRP" ? "GRPID" : Table == "CATGMAS" ? "CATGCODE" : Table == "SMGRP" ? "SMGRPCODE" : Table == "SMMAST" ? "SMCODE" : "ID";
                string Table2 = Table == "PRMAST" ? "PRDBRDT" : "OTHBRDT";

                RetQry = (AND ? " AND" : " WHERE") + " " + Table + "." + Col + " IN (SELECT DISTINCT " + Table + "." + Col + " FROM " + Table + " LEFT JOIN (SELECT " + Table2 + ".FORMID," + Table2 + ".BRID FROM " + Table2 + " WHERE " + Table2 + ".FORMTYPE='" + Table + "')" + Table2 + " ON " + Table + "." + Col + " = " + Table2 + ".FORMID WHERE " + (GetNil ? Table2 + ".FORMID IS NULL OR " : "") + Table2 + ".BRID=" + BrCode + ") ";
            }
            return RetQry;
        }

        public string VRSYS_ALLOWBR_PRMAST(bool RetTable = false, bool DefBranch = false, bool Unapproved = true)
        {
            bool AprvlStat = APRVLR(false, Unapproved, "PRMAST") != "";

            string RetQry = "";
            if (Variables.VRSYS && Variables.BranchYN && (Variables.VoucherTypeCodes != "" || DefBranch))
            {
                string BrCode = "";
                if (DefBranch)
                    BrCode = Variables.VRSYS_DEFBRANCH == 0 ? "-1" : Variables.VRSYS_DEFBRANCH.ToString();
                else
                    BrCode = Variables.VoucherTypeCodes;

                RetQry = "(SELECT PRMAST.PRCODE, PRNAME, ALIAS, PRINTNAME, [DESCRIPTION], UPCCODE, SKUCODE, BARCODE, GROUPCODE, CATGCODE, BRAND, UNIT, ALTUNIT, UNITCONV, CONVTYPE, " + (AprvlStat ? "ROUND(IIF(IIF(APRVLST IS NULL, '', APRVLST) IN ('H', 'P'), 0, IIF(STK.CONVQTY IS NULL, 0, STK.CONVQTY)), 4)" : "ROUND(IIF(STK.CONVQTY IS NULL, 0, STK.CONVQTY), 4)") + " AS OPNSTOCK,ROUND(IIF(STK.CONVVALUE IS NULL,0,STK.CONVVALUE),2) as OPNSTKVAL, PURPRICETYPE," + (DefBranch && Variables.BrPrList ? " IIF(BRPRLIST.BASEPURPRICE IS NULL,PRMAST.BASEPURPRICE,BRPRLIST.BASEPURPRICE) AS BASEPURPRICE" : " PRMAST.BASEPURPRICE") + "," + (DefBranch && Variables.BrPrList ? " IIF(BRPRLIST.ALTPURPRICE IS NULL,PRMAST.ALTPURPRICE,BRPRLIST.ALTPURPRICE) AS ALTPURPRICE" : " PRMAST.ALTPURPRICE") + ", SALESPRICETYPE," + (DefBranch && Variables.BrPrList ? " IIF(BRPRLIST.BASESALEPRICE IS NULL,PRMAST.BASESALEPRICE,BRPRLIST.BASESALEPRICE) AS BASESALEPRICE" : " PRMAST.BASESALEPRICE") + ", BASEMINPRICE," + (DefBranch && Variables.BrPrList ? " IIF(BRPRLIST.ALTSALEPRICE IS NULL,PRMAST.ALTSALEPRICE,BRPRLIST.ALTSALEPRICE) AS ALTSALEPRICE" : " PRMAST.ALTSALEPRICE") + ", ALTMINPRICE, COSTMETH, TAXCODE, SALEACCODE, PURACCODE, MINQTY, REORDQTY, MAXQTY, SERVFLG, SUPCODE, BATCHFLG, MANDATEFLG, EXPDATEFLG, EXPDAYS, PRICECATFLG, ACTIVE, STOCK, LOGCD, UTIME, SECNAME, PRICECATCODE,IIF(IIF(STK.CONVQTY IS NULL,0,STK.CONVQTY)<>0,STK.CONVVALUE/STK.CONVQTY,0) AS OPNSTKRATE, CURCONVRATE, CURCODE, IMGPATH, DELFLG, POSFLG, PRNOTES, BMPL, BMPQ, BMPW, BASEMRP, ALTMRP, SLNOTRACK, BMEQN, APRVLABY, APRVLADT, APRVLBBY, APRVLBDT, APRVLST, APRVLRBY, APRVLRDT, APRVLJBY, APRVLJDT FROM (PRMAST LEFT JOIN (SELECT PRCODE,SUM(CONVQTY) AS CONVQTY,SUM(CONVVALUE) AS CONVVALUE FROM OPNSTK WHERE IIF(BRCD IS NULL,0,BRCD) IN (" + BrCode + ") GROUP BY PRCODE) STK on PRMAST.PRCODE=STK.PRCODE)LEFT JOIN (SELECT BRPRLIST.PRCODE,AVG(BASESALEPRICE) AS BASESALEPRICE,AVG(ALTSALEPRICE) AS ALTSALEPRICE,AVG(BASEPURPRICE) AS BASEPURPRICE,AVG(ALTPURPRICE) AS ALTPURPRICE FROM BRPRLIST WHERE IIF(BRPRLIST.BRCD IS NULL,0,BRPRLIST.BRCD) IN (" + BrCode + ") GROUP BY BRPRLIST.PRCODE)BRPRLIST ON PRMAST.PRCODE = BRPRLIST.PRCODE)";
            }
            else if (AprvlStat)
            {
                RetQry = "(SELECT PRCODE, PRNAME, ALIAS, PRINTNAME, [DESCRIPTION], UPCCODE, SKUCODE, BARCODE, GROUPCODE, CATGCODE, BRAND, UNIT, ALTUNIT, UNITCONV, CONVTYPE, ROUND(IIF(IIF(APRVLST IS NULL, '', APRVLST) IN ('H', 'P'), 0, OPNSTOCK), 4) AS OPNSTOCK, ROUND(IIF(IIF(APRVLST IS NULL, '', APRVLST) IN ('H', 'P'), 0, OPNSTKVAL), 4) AS OPNSTKVAL, PURPRICETYPE, BASEPURPRICE, ALTPURPRICE, SALESPRICETYPE, BASESALEPRICE, BASEMINPRICE, ALTSALEPRICE, ALTMINPRICE, COSTMETH, TAXCODE, SALEACCODE, PURACCODE, MINQTY, REORDQTY, MAXQTY, SERVFLG, SUPCODE, BATCHFLG, MANDATEFLG, EXPDATEFLG, EXPDAYS, PRICECATFLG, ACTIVE, STOCK, LOGCD, UTIME, SECNAME, PRICECATCODE, OPNSTKRATE, CURCONVRATE, CURCODE, IMGPATH, DELFLG, POSFLG, PRNOTES, BMPL, BMPQ, BMPW, BASEMRP, ALTMRP, SLNOTRACK, BMEQN, APRVLABY, APRVLADT, APRVLBBY, APRVLBDT, APRVLST, APRVLRBY, APRVLRDT, APRVLJBY, APRVLJDT FROM PRMAST)";
            }
            else
            {
                RetQry = RetTable ? "PRMAST" : "";
            }
            return RetQry;
        }

        public string VRSYS_ALLOWBR_STKDT(bool RetTable = false, bool DefBranch = false)
        {
            string RetQry = "";
            if (Variables.VRSYS && Variables.BranchYN && (Variables.VoucherTypeCodes != "" || DefBranch))
            {
                string BrCode = "";
                if (DefBranch)
                    BrCode = Variables.VRSYS_DEFBRANCH == 0 ? "-1" : Variables.VRSYS_DEFBRANCH.ToString();
                else
                    BrCode = Variables.VoucherTypeCodes;

                //13_Feb_2024
                RetQry = "(SELECT * FROM STKDT WHERE VSVRTYPE IN (SELECT VRSYSTEM.ID FROM VRSYSTEM WHERE IIF(VRSYSTEM.BRCD IS NULL,0,VRSYSTEM.BRCD) IN (" + BrCode + ")))";
            }
            else
            {
                RetQry = RetTable ? "STKDT" : "";
            }
            return RetQry;
        }

        public string ADDCOST_STKDT(bool RetTable = false, bool DefBranch = false, bool Unapproved = true)
        {
            bool AprvlStat = APRVLR(false, Unapproved, "PRMAST") != "";
            string AprvlQry = @"( (VRTYPE = 'AD' AND TRNNO IN (SELECT PURM.ORDNO FROM PURM WHERE PURM.VRTYPE = 'A' AND IIF(PURM.APRVLST IS NULL, '', PURM.APRVLST) NOT IN ('H', 'P'))) OR
 (VRTYPE = 'ST' AND TRNNO IN (SELECT PURM.VRNO FROM PURM WHERE PURM.VRTYPE = 'T' AND IIF(PURM.APRVLST IS NULL, '', PURM.APRVLST) NOT IN ('H', 'P'))) OR
 (VRTYPE = 'PR' AND TRNNO IN (SELECT PRDNM.ORDNO FROM PRDNM WHERE IIF(PRDNM.APRVLST IS NULL, '', PRDNM.APRVLST) NOT IN ('H', 'P'))) OR
 (VRTYPE = 'PHY' AND TRNNO IN (SELECT PURM.ORDNO FROM PURM WHERE PURM.VRTYPE = 'PHY' AND IIF(PURM.APRVLST IS NULL, '', PURM.APRVLST) NOT IN ('H', 'P'))) OR
 (VRTYPE = 'O' AND TYPE = 'R' AND ORDNO IN (SELECT PORDM.ORDNO FROM PORDM WHERE PORDM.VRTYPE = 'O' AND IIF(PORDM.APRVLST IS NULL, '', PORDM.APRVLST) NOT IN ('H', 'P'))) OR
 (VRTYPE = 'NT' AND TYPE = 'R' AND NTNO IN (SELECT GRNM.ORDNO FROM GRNM WHERE GRNM.VRTYPE = 'RN' AND IIF(GRNM.APRVLST IS NULL, '', GRNM.APRVLST) NOT IN ('H', 'P'))) OR
 (VRTYPE = 'NTR' AND TYPE = 'I' AND NTRNO IN (SELECT GRNM.ORDNO FROM GRNM WHERE GRNM.VRTYPE = 'R' AND IIF(GRNM.APRVLST IS NULL, '', GRNM.APRVLST) NOT IN ('H', 'P'))) OR
 (VRTYPE = 'TR' AND TYPE = 'R' AND TRNNO IN (SELECT PURM.ORDNO FROM PURM WHERE PURM.VRTYPE = 'P' AND IIF(PURM.APRVLST IS NULL, '', PURM.APRVLST) NOT IN ('H', 'P'))) OR
 (VRTYPE = 'RET' AND TYPE = 'I' AND TRNNO IN (SELECT PURM.ORDNO FROM PURM WHERE PURM.VRTYPE = 'R' AND IIF(PURM.APRVLST IS NULL, '', PURM.APRVLST) NOT IN ('H', 'P'))) OR
 (VRTYPE = 'O' AND TYPE = 'I' AND ORDNO IN (SELECT CORDM.ORDNO FROM CORDM WHERE CORDM.STYPE = 'O' AND IIF(CORDM.APRVLST IS NULL, '', CORDM.APRVLST) NOT IN ('H', 'P'))) OR
 (VRTYPE = 'NT' AND TYPE = 'I' AND NTNO IN (SELECT GDNM.ORDNO FROM GDNM WHERE GDNM.STYPE = 'DN' AND IIF(GDNM.APRVLST IS NULL, '', GDNM.APRVLST) NOT IN ('H', 'P'))) OR
 (VRTYPE = 'NTR' AND TYPE = 'R' AND NTRNO IN (SELECT GDNM.ORDNO FROM GDNM WHERE GDNM.STYPE = 'R' AND IIF(GDNM.APRVLST IS NULL, '', GDNM.APRVLST) NOT IN ('H', 'P'))) OR
 (VRTYPE = 'TR' AND TYPE = 'I' AND TRNNO IN (SELECT SALM.ORDNO FROM SALM WHERE SALM.STYPE = 'O' AND IIF(SALM.APRVLST IS NULL, '', SALM.APRVLST) NOT IN ('H', 'P'))) OR
 (VRTYPE = 'RET' AND TYPE = 'R' AND TRNNO IN (SELECT SALM.ORDNO FROM SALM WHERE SALM.STYPE = 'R' AND IIF(SALM.APRVLST IS NULL, '', SALM.APRVLST) NOT IN ('H', 'P'))) )";

            string RetQry = "";
            if (Variables.VRSYS && Variables.BranchYN && (Variables.VoucherTypeCodes != "" || DefBranch))
            {
                string BrCode = "";
                if (DefBranch)
                    BrCode = Variables.VRSYS_DEFBRANCH == 0 ? "-1" : Variables.VRSYS_DEFBRANCH.ToString();
                else
                    BrCode = Variables.VoucherTypeCodes;

                if (Variables.additionalCost)
                    RetQry = "(SELECT VRTYPE, PRCODE, VRDATE, ORDNO, NTNO, TRNNO, QTY, TYPE, PARTYCD, LOGCD, UTIME, RATE + ADDCOST AS RATE, UNITCODE, CONVTYPE, CONVFACT, CONVQTY, PRFLG, CONVRATE + CONVADDCOST AS CONVRATE, GDWCODE, LUTIME, VSVRTYPE, PRV, PRVN, PRVT, SNTSLNO, LM, ADDCOST, CONVADDCOST, CLOSES, ORDROWID, NTROWID, TRNROWID, QTNCODE, QTNROWID, PRVQ, PRVQY, PRVY, PRVNY, PRVTY, NTRNO, NTRROWID FROM STKDT WHERE VSVRTYPE IN (SELECT VRSYSTEM.ID FROM VRSYSTEM WHERE IIF(VRSYSTEM.BRCD IS NULL,0,VRSYSTEM.BRCD) IN (" + BrCode + "))" + (AprvlStat ? (" AND " + AprvlQry) : "") + ")";
                else
                    RetQry = "(SELECT * FROM STKDT WHERE VSVRTYPE IN (SELECT VRSYSTEM.ID FROM VRSYSTEM WHERE IIF(VRSYSTEM.BRCD IS NULL,0,VRSYSTEM.BRCD) IN (" + BrCode + "))" + (AprvlStat ? (" AND " + AprvlQry) : "") + ")";
            }
            else if (Variables.additionalCost)
            {
                RetQry = "(SELECT VRTYPE, PRCODE, VRDATE, ORDNO, NTNO, TRNNO, QTY, TYPE, PARTYCD, LOGCD, UTIME, RATE + ADDCOST AS RATE, UNITCODE, CONVTYPE, CONVFACT, CONVQTY, PRFLG, CONVRATE + CONVADDCOST AS CONVRATE, GDWCODE, LUTIME, VSVRTYPE, PRV, PRVN, PRVT, SNTSLNO, LM, ADDCOST, CONVADDCOST, CLOSES, ORDROWID, NTROWID, TRNROWID, QTNCODE, QTNROWID, PRVQ, PRVQY, PRVY, PRVNY, PRVTY, NTRNO, NTRROWID FROM STKDT" + (AprvlStat ? (" WHERE " + AprvlQry) : "") + ")";
            }
            else if (AprvlStat)
            {
                RetQry = "(SELECT * FROM STKDT WHERE " + AprvlQry + ")";
            }
            else
            {
                RetQry = RetTable ? "STKDT" : "";
            }
            return RetQry;
        }

        public string VRSYS_ALLOWBR_COSTCMAS(bool RetTable = false, bool DefBranch = false)
        {
            string RetQry = "";
            if (Variables.VRSYS && Variables.BranchYN && (Variables.VoucherTypeCodes != "" || DefBranch))
            {
                string BrCode = "";
                if (DefBranch)
                    BrCode = Variables.VRSYS_DEFBRANCH == 0 ? "-1" : Variables.VRSYS_DEFBRANCH.ToString();
                else
                    BrCode = Variables.VoucherTypeCodes;

                RetQry = "(SELECT COSTCMAS.CODE, COSTCNAME, ALIAS, IIF(COSTCOPNBAL.OPNBAL IS NULL,0,COSTCOPNBAL.OPNBAL) AS OPNBAL, IIF(COSTCOPNBAL.BALTYPE IS NULL,'C',COSTCOPNBAL.BALTYPE) AS BALTYPE, ACTIVE, LOGCD, UTIME, SECNAME, GROUPID FROM COSTCMAS LEFT JOIN(SELECT COSTCOPNBAL.COSTCCODE,IIF(SUM(IIF(COSTCOPNBAL.BALTYPE='C',COSTCOPNBAL.OPNBAL,-1*COSTCOPNBAL.OPNBAL))>0,SUM(IIF(COSTCOPNBAL.BALTYPE='C',COSTCOPNBAL.OPNBAL,-1*COSTCOPNBAL.OPNBAL)),-1*(SUM(IIF(COSTCOPNBAL.BALTYPE='C',COSTCOPNBAL.OPNBAL,-1*COSTCOPNBAL.OPNBAL)))) AS OPNBAL,IIF(SUM(IIF(COSTCOPNBAL.BALTYPE='C',COSTCOPNBAL.OPNBAL,-1*COSTCOPNBAL.OPNBAL))>0,'C','D') AS BALTYPE FROM COSTCOPNBAL WHERE IIF(BRCD IS NULL,0,BRCD) IN (" + BrCode + ")  GROUP BY COSTCOPNBAL.COSTCCODE) COSTCOPNBAL ON COSTCMAS.CODE=COSTCOPNBAL.COSTCCODE)";
            }
            else
            {
                RetQry = RetTable ? "COSTCMAS" : "";
            }
            return RetQry;
        }

        public string VRSYS_ALLOWBR_OPNSTK(bool RetTable = false, bool DefBranch = false, bool Unapproved = true)
        {
            bool AprvlStat = APRVLR(false, Unapproved, "PRMAST") != "";
            string AprvlQry = @"PRCODE IN (SELECT PRMAST.PRCODE FROM PRMAST WHERE IIF(PRMAST.APRVLST IS NULL, '', PRMAST.APRVLST) NOT IN ('H', 'P'))";

            string RetQry = "";
            if (Variables.VRSYS && Variables.BranchYN && (Variables.VoucherTypeCodes != "" || DefBranch))
            {
                string BrCode = "";
                if (DefBranch)
                    BrCode = Variables.VRSYS_DEFBRANCH == 0 ? "-1" : Variables.VRSYS_DEFBRANCH.ToString();
                else
                    BrCode = Variables.VoucherTypeCodes;

                RetQry = @"(SELECT * FROM OPNSTK WHERE IIF(BRCD IS NULL,0,BRCD) IN (" + BrCode + ")" + (AprvlStat ? (" AND " + AprvlQry) : "") + ")";
            }
            else if (AprvlStat)
            {
                RetQry = @"(SELECT * FROM OPNSTK WHERE " + AprvlQry + ")";
            }
            else
            {
                RetQry = RetTable ? "OPNSTK" : "";
            }
            return RetQry;
        }
        //-----------------------------------------

        public string BYBRANCH(string Table, bool AND)
        {
            return Variables.VRSYS && Variables.BranchYN && Variables.VoucherTypeCodes != "" ? (AND ? " AND " : " WHERE ") + ("IIF(" + Table + ".BRCD IS NULL,0," + Table + ".BRCD) IN (" + Variables.VoucherTypeCodes + ") ") : "";
        }
        public string BRSTMNT(string Table, bool AND)
        {
            return Variables.VRSYS && Variables.BranchYN && Variables.VoucherTypeCodes != "" ? (AND ? " AND " : " WHERE ") + ("IIF(" + Table + ".VSVRTYPE IS NULL,0," + Table + ".VSVRTYPE) IN (SELECT VRSYSTEM.ID FROM VRSYSTEM WHERE IIF(VRSYSTEM.BRCD IS NULL,0,VRSYSTEM.BRCD) IN (" + Variables.VoucherTypeCodes + ")) ") : "";
        }
        public string BRTBSTMNT_WITH_BRNAME(string Table) // MAINLY USED IT FOR LEDGER REPORT (1)
        {
            return Variables.VRSYS && Variables.BranchYN && Variables.VoucherTypeCodes != "" ? "(SELECT " + Table + ".*,BRMAST.BRNAME FROM ((" + Table + " LEFT JOIN VRSYSTEM ON " + Table + ".VSVRTYPE=VRSYSTEM.ID) LEFT JOIN BRMAST ON VRSYSTEM.BRCD=BRMAST.ID))" : "";
        }
        public string BRTBSTMNT_GET_BRNAME(string Table = "") // MAINLY USED IT FOR LEDGER REPORT (2) // pass table for Ambiguous column name 'VSDOCNO' issue fix.
        {
            return Variables.VRSYS && Variables.BranchYN && Variables.VoucherTypeCodes != "" ? ",BRNAME," + (Table == "" ? "" : Table + ".") + "VSORDCD" : "";
        }
        public string SUBBRSTMNT(string MTab, string STab, string MCol, string SCol, bool AND)
        {
            return Variables.VRSYS && Variables.BranchYN && Variables.VoucherTypeCodes != "" ? (AND ? " AND " : " WHERE ") + (STab + "." + SCol + " IN (SELECT " + MTab + "." + MCol + " FROM " + MTab + " WHERE IIF(" + MTab + ".VSVRTYPE IS NULL,0," + MTab + ".VSVRTYPE) IN (SELECT VRSYSTEM.ID FROM VRSYSTEM WHERE IIF(VRSYSTEM.BRCD IS NULL,0,VRSYSTEM.BRCD) IN (" + Variables.VoucherTypeCodes + "))) ") : "";
        }

        //-----------------------------------------

        public string VRSYS_ALLOWBR_ACCMAST(bool RetTable = false, bool DefBranch = false, bool Unapproved = true)
        {
            bool AprvlStat = APRVLR(false, Unapproved, "ACCMAS") != "";

            string RetQry = "";
            if (Variables.VRSYS && Variables.BranchYN && (Variables.VoucherTypeCodes != "" || DefBranch))
            {
                string BrCode = "";
                if (DefBranch)
                    BrCode = Variables.VRSYS_DEFBRANCH == 0 ? "-1" : Variables.VRSYS_DEFBRANCH.ToString();
                else
                    BrCode = Variables.VoucherTypeCodes ?? "";

                RetQry = "(SELECT ID, ALIAS, ACCNAME, [ADDRESS], PHONE, ACTIVE, LOGCD, UITIME, ACCODE, PRINTNAME, MGROUP, DEPRECIATION, VATFLAG, VATPERC, TAXRETFLAG, BANKACCNAME, BANKACCNO, IBANNO, SWIFTCODE, BRANCH, " + (AprvlStat ? "IIF(IIF(APRVLST IS NULL, '', APRVLST) IN ('H', 'P'), 0, IIF(OPN.OPNVAL IS NULL, 0, OPN.OPNVAL))" : "IIF(OPN.OPNVAL IS NULL, 0, OPN.OPNVAL)") + " AS OPENBAL, SECNAME, CURFLG, CURCONVRATE, CURCODE, DELFLG, APRVLABY, APRVLADT, APRVLBBY, APRVLBDT, APRVLST, APRVLRBY, APRVLRDT, APRVLJBY, APRVLJDT FROM ACCMAS LEFT JOIN (SELECT ACCID,SUM(CREDIT-DEBIT) as OPNVAL FROM OPN WHERE IIF(BRCD IS NULL,0,BRCD) IN (" + BrCode + ") GROUP BY ACCID) OPN on ACCMAS.ID=OPN.ACCID)";
            }
            else if (AprvlStat)
            {
                RetQry = "(SELECT ID, ALIAS, ACCNAME, [ADDRESS], PHONE, ACTIVE, LOGCD, UITIME, ACCODE, PRINTNAME, MGROUP, DEPRECIATION, VATFLAG, VATPERC, TAXRETFLAG, BANKACCNAME, BANKACCNO, IBANNO, SWIFTCODE, BRANCH, IIF(IIF(APRVLST IS NULL, '', APRVLST) IN ('H', 'P'), 0, OPENBAL) AS OPENBAL, SECNAME, CURFLG, CURCONVRATE, CURCODE, DELFLG, APRVLABY, APRVLADT, APRVLBBY, APRVLBDT, APRVLST, APRVLRBY, APRVLRDT, APRVLJBY, APRVLJDT FROM ACCMAS)";
            }
            else
            {
                RetQry = RetTable ? "ACCMAS" : "";
            }
            return RetQry;
        }

        public string VRSYS_ALLOWBR_REPORT(bool AND, string Table, bool GetNil, bool AllBranch = false)
        {
            string RetQry = "";
            if (Variables.VRSYS && Variables.BranchYN && (Variables.VoucherTypeCodes != "" || AllBranch))
            {
                string BrCode = Variables.VoucherTypeCodes;
                if (AllBranch)
                    BrCode = (Variables.VRSYS_DEFBRANCH == 0 ? "-1" : Variables.VRSYS_DEFBRANCH.ToString()) + "," + (Variables.VRSYS_ALLOWEDBRANCHES == "" ? "-1" : Variables.VRSYS_ALLOWEDBRANCHES);

                string Col = Table == "PRMAST" ? "PRCODE" : Table == "BOMMAS" ? "CODE" : Table == "GRPLDG" ? "GROUPID" : Table == "PRGRPMAS" ? "PRGRPID" : Table == "PRUNITMAS" ? "PRUNITID" : Table == "PRUNITCONV" || Table == "TAXMAS" || Table == "PRICECATMAS" || Table == "COSTCMAS" || Table == "CURRENCYMAS" || Table == "CURRCONV" ? "CODE" : Table == "GDWMAS" ? "GDWID" : Table == "GDWGRP" ? "GRPID" : Table == "CATGMAS" ? "CATGCODE" : Table == "SMGRP" ? "SMGRPCODE" : Table == "SMMAST" ? "SMCODE" : "ID";
                string Table2 = Table == "PRMAST" ? "PRDBRDT" : "OTHBRDT";

                RetQry = (AND ? " AND" : " WHERE") + " " + Table + "." + Col + " IN (SELECT DISTINCT " + Table + "." + Col + " FROM " + Table + " LEFT JOIN (SELECT " + Table2 + ".FORMID," + Table2 + ".BRID FROM " + Table2 + " WHERE " + Table2 + ".FORMTYPE='" + Table + "')" + Table2 + " ON " + Table + "." + Col + " = " + Table2 + ".FORMID WHERE " + (GetNil ? Table2 + ".FORMID IS NULL OR " : "") + Table2 + ".BRID IN (" + BrCode + ")) ";
            }
            return RetQry;
        }
        #endregion VoucherSystem

        #region ApprovalSystem
        public string APRVLP(bool? AND, string Table) // for pending list
        {
            return Variables.ApprovalSystem ? ((AND == null ? "" : Convert.ToBoolean(AND) ? " AND" : " WHERE") + " IIF(" + Table + ".APRVLST IS NULL, '', " + Table + ".APRVLST) NOT IN ('H', 'P') ") : "";
        }
        public string APRVLPI(bool? AND, string Table) // for pending list with inventory approval
        {
            return Variables.ApprovalSystem && Variables.InventoryApproval ? ((AND == null ? "" : Convert.ToBoolean(AND) ? " AND" : " WHERE") + " IIF(" + Table + ".APRVLST IS NULL, '', " + Table + ".APRVLST) NOT IN ('H', 'P') ") : "";
        }
        public string APRVLPI2(bool AND, string MTab, string STab, string MCol, string SCol) // for pending list with inventory approval - sub-table filter
        {
            return Variables.ApprovalSystem && Variables.InventoryApproval ? ((AND ? " AND " : " WHERE ") + (STab + "." + SCol + " IN (SELECT " + MTab + "." + MCol + " FROM " + MTab + " WHERE IIF(" + MTab + ".APRVLST IS NULL, '', " + MTab + ".APRVLST) NOT IN ('H', 'P')) ")) : "";
        }
        public string APRVLI(bool OR, bool Unapproved, string Table) // for master filter
        {
            return Variables.ApprovalSystem && Unapproved ? ((OR ? " OR" : " AND") + " IIF(" + Table + ".APRVLST IS NULL, '', " + Table + ".APRVLST) IN ('H', 'P')") : "";
        }
        public string APRVLR(bool AND, bool Unapproved, string Table) // for reports filter
        {
            return Variables.ApprovalSystem && !Unapproved ? ((AND ? " AND" : " WHERE") + " IIF(" + Table + ".APRVLST IS NULL, '', " + Table + ".APRVLST) NOT IN ('H', 'P') ") : "";
        }
        public string APRVLR2(bool AND, bool Unapproved, string MTab, string STab, string MCol, string SCol) // for reports sub-table filter
        {
            return Variables.ApprovalSystem && !Unapproved ? ((AND ? " AND " : " WHERE ") + (STab + "." + SCol + " IN (SELECT " + MTab + "." + MCol + " FROM " + MTab + " WHERE IIF(" + MTab + ".APRVLST IS NULL, '', " + MTab + ".APRVLST) NOT IN ('H', 'P')) ")) : "";
        }
        public string APRVLR_STKDT(bool AND, bool Unapproved, string Table, string VrType, string Type) // for pending reports filter
        {
            return Variables.ApprovalSystem && !Unapproved ? ((AND ? " AND " : " WHERE ") + (Table == "" ? "" : (Table + ".")) + (
                VrType == "O" && Type == "R" ? "ORDNO IN (SELECT PORDM.ORDNO FROM PORDM WHERE PORDM.VRTYPE = 'O' AND IIF(PORDM.APRVLST IS NULL, '', PORDM.APRVLST) NOT IN ('H', 'P'))" :
                VrType == "NT" && Type == "R" ? "NTNO IN (SELECT GRNM.ORDNO FROM GRNM WHERE GRNM.VRTYPE = 'RN' AND IIF(GRNM.APRVLST IS NULL, '', GRNM.APRVLST) NOT IN ('H', 'P'))" :
                VrType == "TR" && Type == "R" ? "TRNNO IN (SELECT PURM.ORDNO FROM PURM WHERE PURM.VRTYPE = 'P' AND IIF(PURM.APRVLST IS NULL, '', PURM.APRVLST) NOT IN ('H', 'P'))" :
                VrType == "O" && Type == "I" ? "ORDNO IN (SELECT CORDM.ORDNO FROM CORDM WHERE CORDM.STYPE = 'O' AND IIF(CORDM.APRVLST IS NULL, '', CORDM.APRVLST) NOT IN ('H', 'P'))" :
                VrType == "NT" && Type == "I" ? "NTNO IN (SELECT GDNM.ORDNO FROM GDNM WHERE GDNM.STYPE = 'DN' AND IIF(GDNM.APRVLST IS NULL, '', GDNM.APRVLST) NOT IN ('H', 'P'))" :
                VrType == "TR" && Type == "I" ? "TRNNO IN (SELECT SALM.ORDNO FROM SALM WHERE SALM.STYPE = 'O' AND IIF(SALM.APRVLST IS NULL, '', SALM.APRVLST) NOT IN ('H', 'P'))" : "")) : "";
        }
        #endregion ApprovalSystem
    }
}
