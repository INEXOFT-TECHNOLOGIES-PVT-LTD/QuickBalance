using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using QuickBalance.API.CLS;
using System.Data;

namespace QuickBalance.API.DB
{
    public class PrMastDB
    {
        private readonly DBConn db;
        public Variable Variables = new Variable();
        public Function Functions = new Function();
        string qry = "";
        public string STK_TB = "";
        string Stk_Det = "";

        public PrMastDB(DBConn dbConn)
        {
            db = dbConn;
        }

        /// ============================================================
        // SQL HELPERS
        // ============================================================

        private string SqlEscape(string? value)
        {
            return (value ?? "").Replace("'", "''");
        }

        private int BitValue(bool value)
        {
            return value ? 1 : 0;
        }
        public async Task<DataTable> SearchByCode(ClsPrMast clsPrMast)
        {
            qry = "SELECT * FROM " + Functions.VRSYS_ALLOWBR_PRMAST(DefBranch: true) + "PRMAST WHERE PRCODE='" + clsPrMast.PrCode + "'";
            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> List()
        {
            int page = Variables.pgRequest.Page;
            int pageSize = Variables.pgRequest.PageSize;

            string? search = Variables.pgRequest.Search;

            string orderBy = "PRMAST.PRNAME";
            switch (Variables.pgRequest.SortBy?.ToLower())
            {
                case "name":
                case "prname":
                case "product":
                case "productname":
                    orderBy = "PRMAST.PRNAME";
                    break;

                case "alias":
                    orderBy = "PRMAST.ALIAS";
                    break;

                case "barcode":
                    orderBy = "PRMAST.BARCODE";
                    break;
            }
            string sortDirection = Variables.pgRequest.SortDirection == "ASC" ? "ASC" : "DESC";
            orderBy += $" {sortDirection}";

            int offset = (page - 1) * pageSize;

            string searchCondition = "";
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Replace("'", "''");
                searchCondition = $@" AND (PRMAST.PRNAME LIKE '%{search}%' OR PRMAST.ALIAS LIKE '%{search}%' OR PRMAST.BARCODE LIKE '%{search}%')";
            }

            qry = @$"SELECT PRMAST.PRCODE, PRMAST.PRNAME, PRMAST.ALIAS, PRMAST.BARCODE, PRMAST.UNIT AS MAINUNITCODE, PU1.PRUNITNAME AS MAINUNITNAME, ISNULL(PRMAST.ALTUNIT, 0) AS ALTUNITCODE, ISNULL(PU2.PRUNITNAME, '') AS ALTUNITNAME,
PRMAST.CONVTYPE, PRMAST.UNITCONV AS CONVFACT, PRMAST.SALESPRICETYPE, PRMAST.PURPRICETYPE, PRMAST.BASESALEPRICE, PRMAST.ALTSALEPRICE, PRMAST.BASEPURPRICE, PRMAST.ALTPURPRICE, PRMAST.BASEMRP, PRMAST.ALTMRP, PRMAST.TAXCODE, TX.TAXNAME, TX.TAXPERC,
PRMAST.BATCHFLG, PRMAST.SLNOTRACK, COUNT(*) OVER() AS TotalRecords
FROM {Functions.VRSYS_ALLOWBR_PRMAST(DefBranch: true)}PRMAST INNER JOIN PRUNITMAS PU1 ON PRMAST.UNIT = PU1.PRUNITID
LEFT JOIN PRUNITMAS PU2 ON PRMAST.ALTUNIT = PU2.PRUNITID
INNER JOIN TAXMAS TX ON PRMAST.TAXCODE = TX.CODE
WHERE PRMAST.ACTIVE = 1
{Functions.VRSYS_ALLOWBR(true, "PRMAST", true)}
{searchCondition}ORDER BY {orderBy}
OFFSET {offset} ROWS
FETCH NEXT {pageSize} ROWS ONLY";

            return await db.GetDatatable(qry);
        }

        public async Task<decimal> OpeningStock_PL(bool Unapproved)
        {
            qry = @"SELECT SUM(IIF(STVALUE IS NULL, 0, STVALUE)) AS STVALUE FROM " + Functions.VRSYS_ALLOWBR_OPNSTK(Unapproved: Unapproved) + "OPNSTK";
            return await db.ReturnDecValue(qry);
        }

        public async Task<int> CreateForClosingStock(ClsDate clsDate, bool Alt, int Pr_Code = 0, bool PLReport = false, bool Unapproved = true)
        {
            STK_TB = "CSTK_" + DateTime.Now.Hour.ToString() + "_" + DateTime.Now.Minute.ToString() + "_" + DateTime.Now.Second.ToString();

            if (Alt == true)
            {
                qry = @"SELECT PRCODE,BARCODE, PRNAME,SUM( OPNSTOCK) AS OPNSTOCK,SUM( STOCK) AS STOCK,UNIT, GROUPCODE, TAXCODE, CATGCODE,  BRAND,SUM(QTY) AS QTY,
                IIF(COSTMETH=3 AND ALTPURPRICE <> 0, ALTPURPRICE, CASE  WHEN CONVTYPE=0 AND CONVFACT<>0 THEN AVGRATE*CONVFACT WHEN CONVTYPE=1 AND CONVFACT<>0 THEN AVGRATE/CONVFACT WHEN CONVTYPE=-1 OR CONVFACT=0 THEN AVGRATE END) AS AVGRATE,CASE WHEN CONVTYPE=0 AND CONVFACT<>0 THEN AVGGRNRATE*CONVFACT WHEN CONVTYPE=1 AND CONVFACT<>0 THEN AVGGRNRATE/CONVFACT WHEN CONVTYPE=-1 OR CONVFACT=0 THEN AVGGRNRATE END  AS AVGGRNRATE,CASE WHEN CONVTYPE=0 AND CONVFACT<>0 THEN PRPRICE*CONVFACT WHEN CONVTYPE=1 AND CONVFACT<>0 THEN PRPRICE/CONVFACT WHEN CONVTYPE=-1 OR CONVFACT=0 THEN PRPRICE END AS PRPRICE,SUM(OPNSTKVAL) AS OPNSTKVAL INTO " + STK_TB + @"  FROM(
                SELECT DISTINCT PRCODE, BARCODE, PRNAME,SUM(OPNSTOCK) AS OPNSTOCK, SUM(STOCK) AS STOCK,UNIT, GROUPCODE, TAXCODE, CATGCODE,  BRAND,CONVFACT,CONVTYPE, (SUM(RNQTY)+SUM(SRTQTY)+SUM(ADRQTY)+SUM(PRDRQTY)+SUM(STRQTY)+SUM(PHYRQTY)+SUM(DNRQTY)-SUM(PRTQTY)-SUM(DNQTY)-SUM(ADIQTY)-SUM(PRDIQTY)-SUM(STIQTY)-SUM(PHYIQTY)-SUM(RNRQTY)) AS QTY ,
                IIF(SUM(AVGRATE)<>0 AND MAX(OPNSTKRATE)<>0 ,(SUM(AVGRATE)+MAX(OPNSTKRATE))/2,SUM(AVGRATE)) AS AVGRATE,IIF(SUM(AVGGRNRATE)<>0 AND MAX(OPNSTKRATE)<>0,(SUM(AVGGRNRATE)+MAX(OPNSTKRATE))/2,SUM(AVGGRNRATE)) AS AVGGRNRATE,CASE WHEN AVG(PRPRICE)<>0 AND MAX(OPNSTKRATE)<>0 THEN (AVG(PRPRICE)+MAX(OPNSTKRATE))/2 WHEN AVG(PRPRICE)<>0 THEN MAX(PRPRICE) WHEN MAX(OPNSTKRATE)<>0 THEN MAX(OPNSTKRATE) WHEN AVG(PRPRICE)=0 AND MAX(OPNSTKRATE)=0 THEN 0 END AS PRPRICE,MAX(OPNSTKVAL) AS OPNSTKVAL,COSTMETH,ALTPURPRICE  FROM(" +
                @" SELECT PRMAST.PRCODE, PRMAST.BARCODE,PRMAST.PRNAME,PRUNITMAS.PRUNITNAME AS UNIT ,0 AS STOCK, 0 AS OPNSTOCK, PRMAST.GROUPCODE, PRMAST.TAXCODE, PRMAST.CATGCODE, PRMAST.BRAND,PRMAST.UNITCONV AS CONVFACT,PRMAST.CONVTYPE,SUM(IIF((STKDT.VRTYPE = 'NT' AND STKDT.TYPE = 'R'), CASE WHEN STKDT.UNITCODE=PRMAST.ALTUNIT THEN STKDT.QTY WHEN STKDT.CONVTYPE=1 THEN STKDT.CONVQTY*STKDT.CONVFACT WHEN STKDT.CONVTYPE=0 AND STKDT.CONVFACT<>0 THEN STKDT.CONVQTY/STKDT.CONVFACT WHEN STKDT.CONVTYPE=-1 OR STKDT.CONVFACT=0 THEN STKDT.CONVQTY END , 0))AS RNQTY, SUM(IIF((STKDT.VRTYPE = 'TR' AND STKDT.TYPE = 'R'), CASE WHEN STKDT.UNITCODE=PRMAST.ALTUNIT THEN STKDT.QTY WHEN STKDT.CONVTYPE=1 THEN STKDT.CONVQTY*STKDT.CONVFACT WHEN STKDT.CONVTYPE=0 AND STKDT.CONVFACT<>0 THEN STKDT.CONVQTY/STKDT.CONVFACT WHEN STKDT.CONVTYPE=-1 OR STKDT.CONVFACT=0 THEN STKDT.CONVQTY END, 0))AS PRQTY, SUM(IIF((STKDT.VRTYPE = 'NT' AND STKDT.TYPE = 'I'), CASE WHEN STKDT.UNITCODE=PRMAST.ALTUNIT THEN STKDT.QTY WHEN STKDT.CONVTYPE=1 THEN STKDT.CONVQTY*STKDT.CONVFACT WHEN STKDT.CONVTYPE=0 AND STKDT.CONVFACT<>0 THEN STKDT.CONVQTY/STKDT.CONVFACT WHEN STKDT.CONVTYPE=-1 OR STKDT.CONVFACT=0 THEN STKDT.CONVQTY END, 0))AS DNQTY, SUM(IIF((STKDT.VRTYPE = 'TR' AND STKDT.TYPE = 'I') , CASE WHEN STKDT.UNITCODE=PRMAST.ALTUNIT THEN STKDT.QTY WHEN STKDT.CONVTYPE=1 THEN STKDT.CONVQTY*STKDT.CONVFACT WHEN STKDT.CONVTYPE=0 AND STKDT.CONVFACT<>0 THEN STKDT.CONVQTY/STKDT.CONVFACT WHEN STKDT.CONVTYPE=-1 OR STKDT.CONVFACT=0 THEN STKDT.CONVQTY END , 0))AS SLQTY, SUM(IIF((STKDT.VRTYPE = 'RET' AND STKDT.TYPE = 'I'), CASE WHEN STKDT.UNITCODE=PRMAST.ALTUNIT THEN STKDT.QTY WHEN STKDT.CONVTYPE=1 THEN STKDT.CONVQTY*STKDT.CONVFACT WHEN STKDT.CONVTYPE=0 AND STKDT.CONVFACT<>0 THEN STKDT.CONVQTY/STKDT.CONVFACT WHEN STKDT.CONVTYPE=-1 OR STKDT.CONVFACT=0 THEN STKDT.CONVQTY END, 0))AS PRTQTY, SUM(IIF((STKDT.VRTYPE = 'RET' AND STKDT.TYPE = 'R'), CASE WHEN STKDT.UNITCODE=PRMAST.ALTUNIT THEN STKDT.QTY WHEN STKDT.CONVTYPE=1 THEN STKDT.CONVQTY*STKDT.CONVFACT WHEN STKDT.CONVTYPE=0 AND STKDT.CONVFACT<>0 THEN STKDT.CONVQTY/STKDT.CONVFACT WHEN STKDT.CONVTYPE=-1 OR STKDT.CONVFACT=0 THEN STKDT.CONVQTY END, 0))AS SRTQTY, SUM(IIF((STKDT.VRTYPE = 'AD' AND STKDT.TYPE = 'R'), CASE WHEN STKDT.UNITCODE=PRMAST.ALTUNIT THEN STKDT.QTY WHEN STKDT.CONVTYPE=1  THEN STKDT.CONVQTY*STKDT.CONVFACT WHEN STKDT.CONVTYPE=0 AND STKDT.CONVFACT<>0 THEN STKDT.CONVQTY/STKDT.CONVFACT WHEN STKDT.CONVTYPE=-1 OR STKDT.CONVFACT=0 THEN STKDT.CONVQTY END, 0))AS ADRQTY, SUM(IIF((STKDT.VRTYPE = 'AD' AND STKDT.TYPE = 'I'), CASE WHEN STKDT.UNITCODE=PRMAST.ALTUNIT THEN STKDT.QTY WHEN STKDT.CONVTYPE=1 THEN STKDT.CONVQTY*STKDT.CONVFACT WHEN STKDT.CONVTYPE=0 AND STKDT.CONVFACT<>0 THEN STKDT.CONVQTY/STKDT.CONVFACT WHEN STKDT.CONVTYPE=-1 OR STKDT.CONVFACT=0 THEN STKDT.CONVQTY END, 0))AS ADIQTY, SUM(IIF((STKDT.VRTYPE = 'PR' AND STKDT.TYPE = 'I'), CASE WHEN STKDT.UNITCODE=PRMAST.ALTUNIT THEN STKDT.QTY WHEN STKDT.CONVTYPE=1 THEN STKDT.CONVQTY*STKDT.CONVFACT WHEN STKDT.CONVTYPE=0 AND STKDT.CONVFACT<>0 THEN STKDT.CONVQTY/STKDT.CONVFACT WHEN STKDT.CONVTYPE=-1 OR STKDT.CONVFACT=0 THEN STKDT.CONVQTY END, 0))AS PRDIQTY, SUM(IIF((STKDT.VRTYPE = 'PR' AND STKDT.TYPE = 'R'), CASE WHEN STKDT.UNITCODE=PRMAST.ALTUNIT THEN STKDT.QTY WHEN STKDT.CONVTYPE=1 THEN STKDT.CONVQTY*STKDT.CONVFACT WHEN STKDT.CONVTYPE=0 AND STKDT.CONVFACT<>0 THEN STKDT.CONVQTY/STKDT.CONVFACT WHEN STKDT.CONVTYPE=-1 OR STKDT.CONVFACT=0 THEN STKDT.CONVQTY END, 0))AS PRDRQTY, SUM(IIF((STKDT.VRTYPE = 'ST' AND STKDT.TYPE = 'I'), CASE WHEN STKDT.UNITCODE=PRMAST.ALTUNIT THEN STKDT.QTY WHEN STKDT.CONVTYPE=1 THEN STKDT.CONVQTY*STKDT.CONVFACT WHEN STKDT.CONVTYPE=0 AND STKDT.CONVFACT<>0 THEN STKDT.CONVQTY/STKDT.CONVFACT WHEN STKDT.CONVTYPE=-1 OR STKDT.CONVFACT=0 THEN STKDT.CONVQTY END, 0))AS STIQTY, SUM(IIF((STKDT.VRTYPE = 'ST' AND STKDT.TYPE = 'R'), CASE WHEN STKDT.UNITCODE=PRMAST.ALTUNIT THEN STKDT.QTY WHEN STKDT.CONVTYPE=1 THEN STKDT.CONVQTY*STKDT.CONVFACT WHEN STKDT.CONVTYPE=0 AND STKDT.CONVFACT<>0 THEN STKDT.CONVQTY/STKDT.CONVFACT WHEN STKDT.CONVTYPE=-1 OR STKDT.CONVFACT=0 THEN STKDT.CONVQTY END, 0))AS STRQTY,
                SUM(IIF((STKDT.VRTYPE = 'PHY' AND STKDT.TYPE = 'I'), CASE WHEN STKDT.UNITCODE = PRMAST.ALTUNIT THEN STKDT.QTY WHEN STKDT.CONVTYPE = 1 THEN STKDT.CONVQTY * STKDT.CONVFACT WHEN STKDT.CONVTYPE = 0 AND STKDT.CONVFACT <> 0 THEN STKDT.CONVQTY / STKDT.CONVFACT WHEN STKDT.CONVTYPE = -1 OR STKDT.CONVFACT = 0 THEN STKDT.CONVQTY END, 0))AS PHYIQTY, SUM(IIF((STKDT.VRTYPE = 'PHY' AND STKDT.TYPE = 'R'), CASE WHEN STKDT.UNITCODE = PRMAST.ALTUNIT THEN STKDT.QTY WHEN STKDT.CONVTYPE = 1 THEN STKDT.CONVQTY * STKDT.CONVFACT WHEN STKDT.CONVTYPE = 0 AND STKDT.CONVFACT <> 0 THEN STKDT.CONVQTY / STKDT.CONVFACT WHEN STKDT.CONVTYPE = -1 OR STKDT.CONVFACT = 0 THEN STKDT.CONVQTY END, 0))AS PHYRQTY, SUM(IIF((STKDT.VRTYPE = 'NTR' AND STKDT.TYPE = 'R'), CASE WHEN STKDT.UNITCODE=PRMAST.ALTUNIT THEN STKDT.QTY WHEN STKDT.CONVTYPE=1 THEN STKDT.CONVQTY*STKDT.CONVFACT WHEN STKDT.CONVTYPE=0 AND STKDT.CONVFACT<>0 THEN STKDT.CONVQTY/STKDT.CONVFACT WHEN STKDT.CONVTYPE=-1 OR STKDT.CONVFACT=0 THEN STKDT.CONVQTY END, 0))AS DNRQTY,SUM(IIF((STKDT.VRTYPE = 'NTR' AND STKDT.TYPE = 'I'), CASE WHEN STKDT.UNITCODE=PRMAST.ALTUNIT THEN STKDT.QTY WHEN STKDT.CONVTYPE=1 THEN STKDT.CONVQTY*STKDT.CONVFACT WHEN STKDT.CONVTYPE=0 AND STKDT.CONVFACT<>0 THEN STKDT.CONVQTY/STKDT.CONVFACT WHEN STKDT.CONVTYPE=-1 OR STKDT.CONVFACT=0 THEN STKDT.CONVQTY END , 0))AS RNRQTY,"

                + @" IIF(STKDT.VRTYPE = 'TR' AND STKDT.TYPE = 'R', AVG(STKDT.CONVRATE), 0)AS AVGRATE, IIF(STKDT.VRTYPE = 'NT' AND STKDT.TYPE = 'R', AVG(STKDT.CONVRATE), 0)AS AVGGRNRATE, CASE WHEN PRMAST.PURPRICETYPE=0 THEN PRMAST.BASEPURPRICE WHEN PRMAST.PURPRICETYPE = 1 AND PRMAST.CONVTYPE=0 THEN PRMAST.ALTPURPRICE*PRMAST.UNITCONV WHEN PRMAST.PURPRICETYPE = 1 AND PRMAST.CONVTYPE=1 THEN PRMAST.ALTPURPRICE/PRMAST.UNITCONV END AS PRPRICE ,0 AS OPNSTKVAL,0 AS OPNSTKRATE,PRMAST.COSTMETH,IIF(PRMAST.ALTPURPRICE <> 0 OR PRMAST.PURPRICETYPE<>0 OR PRMAST.UNITCONV = 0,PRMAST.ALTPURPRICE, IIF(PRMAST.PURPRICETYPE = 1 AND PRMAST.CONVTYPE=0, PRMAST.BASEPURPRICE/PRMAST.UNITCONV, PRMAST.BASEPURPRICE*PRMAST.UNITCONV )) AS ALTPURPRICE FROM((PRMAST LEFT JOIN " + Functions.ADDCOST_STKDT(Unapproved: Unapproved) + "STKDT  ON  PRMAST.PRCODE = STKDT.PRCODE) LEFT JOIN PRUNITMAS ON PRUNITMAS.PRUNITID=(IIF(PRMAST.ALTUNIT IS NOT NULL AND PRMAST.ALTUNIT<>0,PRMAST.ALTUNIT ,PRMAST.UNIT))) WHERE STKDT.VRDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "' AND PRMAST.COSTMETH<>4 AND PRMAST.COSTMETH<>5 AND PRMAST.SERVFLG='" + false + "' GROUP BY PRMAST.PRCODE, PRMAST.BARCODE,PRMAST.PRNAME,PRUNITMAS.PRUNITNAME, PRMAST.GROUPCODE, PRMAST.TAXCODE, PRMAST.CATGCODE, PRMAST.BRAND,PRMAST.BASEPURPRICE, PRMAST.ALTPURPRICE, PRMAST.PURPRICETYPE, STKDT.VRTYPE, STKDT.TYPE,PRMAST.UNITCONV,PRMAST.CONVTYPE,PRMAST.COSTMETH" +
                " UNION ALL SELECT PRMAST.PRCODE, PRMAST.BARCODE,PRMAST.PRNAME,IIF(PRUNITMAS_1.PRUNITNAME  IS NOT NULL,PRUNITMAS_1.PRUNITNAME, PRUNITMAS.PRUNITNAME) AS UNIT ,SUM(OPNSTK.CONVQTY) AS STOCK, SUM(OPNSTK.CONVQTY) AS OPNSTOCK, PRMAST.GROUPCODE, PRMAST.TAXCODE, PRMAST.CATGCODE, PRMAST.BRAND,PRMAST.UNITCONV AS CONVFACT,PRMAST.CONVTYPE,0 AS RNQTY,0 AS PRQTY, 0 AS DNQTY, 0 AS SLQTY, 0 AS PRTQTY, 0 AS SRTQTY, 0 AS ADRQTY, 0 AS ADIQTY, 0 AS PRDIQTY, 0 AS PRDRQTY,0 AS STIQTY,0 AS STRQTY,0 AS PHYIQTY,0 AS PHYRQTY,0 AS DNRQTY, 0 AS RNRQTY, '0' AS AVGRATE, '0' AS AVGGRNRATE, 0 AS PRPRICE,SUM(OPNSTK.CONVVALUE) AS OPNSTKVAL,AVG(OPNSTK.RATE) AS OPNSTKRATE,PRMAST.COSTMETH,IIF(PRMAST.ALTPURPRICE <> 0 OR PRMAST.PURPRICETYPE<>0 OR PRMAST.UNITCONV = 0,PRMAST.ALTPURPRICE, IIF(PRMAST.PURPRICETYPE = 1 AND PRMAST.CONVTYPE=0, PRMAST.BASEPURPRICE/PRMAST.UNITCONV, PRMAST.BASEPURPRICE*PRMAST.UNITCONV )) AS ALTPURPRICE FROM((PRMAST INNER JOIN " + Functions.VRSYS_ALLOWBR_OPNSTK(Unapproved: Unapproved) + "OPNSTK  ON  PRMAST.PRCODE = OPNSTK.PRCODE) LEFT JOIN PRUNITMAS ON PRMAST.UNIT = PRUNITMAS.PRUNITID) LEFT JOIN PRUNITMAS PRUNITMAS_1 ON PRMAST.ALTUNIT = PRUNITMAS_1.PRUNITID WHERE  PRMAST.COSTMETH<>4 AND PRMAST.COSTMETH<>5 AND PRMAST.SERVFLG='" + false + "' GROUP BY PRMAST.PRCODE, PRMAST.BARCODE,PRMAST.PRNAME,PRUNITMAS.PRUNITNAME, PRMAST.GROUPCODE, PRMAST.TAXCODE, PRMAST.CATGCODE, PRMAST.BRAND,PRMAST.BASEPURPRICE, PRMAST.ALTPURPRICE, PRMAST.PURPRICETYPE,PRMAST.UNITCONV,PRMAST.CONVTYPE,PRUNITMAS_1.PRUNITNAME,PRMAST.COSTMETH" +
                " ) T GROUP BY PRCODE,BARCODE,PRNAME,UNIT,GROUPCODE,TAXCODE,CATGCODE,BRAND,CONVFACT,CONVTYPE,COSTMETH,ALTPURPRICE) T1 GROUP BY PRCODE,BARCODE,PRNAME,UNIT,GROUPCODE,TAXCODE,CATGCODE,BRAND,CONVFACT,CONVTYPE,AVGRATE,AVGGRNRATE,PRPRICE,COSTMETH,ALTPURPRICE";
            }
            else
            {
                qry = @"SELECT * INTO  " + STK_TB + @" FROM( SELECT DISTINCT PRCODE, BARCODE, PRNAME,SUM( OPNSTOCK) AS OPNSTOCK,SUM( STOCK) AS STOCK,UNIT, GROUPCODE, TAXCODE, CATGCODE,  BRAND, (SUM(RNQTY)+SUM(SRTQTY)+SUM(ADRQTY)+SUM(PRDRQTY)+SUM(STRQTY)+SUM(PHYRQTY)+SUM(DNRQTY)-SUM(PRTQTY)-SUM(DNQTY)-SUM(ADIQTY)-SUM(PRDIQTY)-SUM(STIQTY)-SUM(PHYIQTY)-SUM(RNRQTY)) AS QTY ,
                IIF(COSTMETH=3 AND BASEPURPRICE <> 0, BASEPURPRICE, IIF(SUM(AVGRATE)<>0 AND MAX(OPNSTKRATE)<>0 ,(SUM(AVGRATE)+MAX(OPNSTKRATE))/2,SUM(AVGRATE)) ) AS AVGRATE,IIF(SUM(AVGGRNRATE)<>0 AND MAX(OPNSTKRATE)<>0,(SUM(AVGGRNRATE)+MAX(OPNSTKRATE))/2,SUM(AVGGRNRATE)) AS AVGGRNRATE,(CASE WHEN MAX(PRPRICE)<>0 AND MAX(OPNSTKRATE)<>0 THEN (MAX(PRPRICE)+MAX(OPNSTKRATE))/2 WHEN MAX(PRPRICE)<>0 THEN MAX(PRPRICE) WHEN MAX(OPNSTKRATE)<>0 THEN MAX(OPNSTKRATE) WHEN MAX(PRPRICE)=0 AND MAX(OPNSTKRATE)=0 THEN 0 END) AS PRPRICE,MAX(OPNSTKVAL) AS OPNSTKVAL  FROM(" +
                " SELECT PRMAST.PRCODE, PRMAST.BARCODE,PRMAST.PRNAME,PRUNITMAS.PRUNITNAME AS UNIT ,0 AS STOCK, 0 AS OPNSTOCK, PRMAST.GROUPCODE, PRMAST.TAXCODE, PRMAST.CATGCODE, PRMAST.BRAND,SUM(IIF((STKDT.VRTYPE = 'NT' AND STKDT.TYPE = 'R'), STKDT.CONVQTY, 0))AS RNQTY, SUM(IIF((STKDT.VRTYPE = 'TR' AND STKDT.TYPE = 'R'), STKDT.CONVQTY, 0))AS PRQTY, SUM(IIF((STKDT.VRTYPE = 'NT' AND STKDT.TYPE = 'I'), STKDT.CONVQTY, 0))AS DNQTY, SUM(IIF((STKDT.VRTYPE = 'TR' AND STKDT.TYPE = 'I') , STKDT.CONVQTY, 0))AS SLQTY, SUM(IIF((STKDT.VRTYPE = 'RET' AND STKDT.TYPE = 'I'), STKDT.CONVQTY, 0))AS PRTQTY, SUM(IIF((STKDT.VRTYPE = 'RET' AND STKDT.TYPE = 'R'), STKDT.CONVQTY, 0))AS SRTQTY, SUM(IIF((STKDT.VRTYPE = 'AD' AND STKDT.TYPE = 'R'), STKDT.CONVQTY, 0))AS ADRQTY, SUM(IIF((STKDT.VRTYPE = 'AD' AND STKDT.TYPE = 'I'), STKDT.CONVQTY, 0))AS ADIQTY, SUM(IIF((STKDT.VRTYPE = 'PR' AND STKDT.TYPE = 'I'), STKDT.CONVQTY, 0))AS PRDIQTY, SUM(IIF((STKDT.VRTYPE = 'PR' AND STKDT.TYPE = 'R'), STKDT.CONVQTY, 0))AS PRDRQTY,SUM(IIF((STKDT.VRTYPE = 'ST' AND STKDT.TYPE = 'I'), STKDT.CONVQTY, 0))AS STIQTY, SUM(IIF((STKDT.VRTYPE = 'ST' AND STKDT.TYPE = 'R'), STKDT.CONVQTY, 0))AS STRQTY,SUM(IIF((STKDT.VRTYPE = 'PHY' AND STKDT.TYPE = 'I'), STKDT.CONVQTY, 0))AS PHYIQTY, SUM(IIF((STKDT.VRTYPE = 'PHY' AND STKDT.TYPE = 'R'), STKDT.CONVQTY, 0))AS PHYRQTY, SUM(IIF((STKDT.VRTYPE = 'NTR' AND STKDT.TYPE = 'R'), STKDT.CONVQTY, 0))AS DNRQTY,SUM(IIF((STKDT.VRTYPE = 'NTR' AND STKDT.TYPE = 'I'), STKDT.CONVQTY, 0))AS RNRQTY, IIF(STKDT.VRTYPE = 'TR' AND STKDT.TYPE = 'R', AVG(STKDT.CONVRATE), 0)AS AVGRATE, IIF(STKDT.VRTYPE = 'NT' AND STKDT.TYPE = 'R', AVG(STKDT.CONVRATE), 0)AS AVGGRNRATE, (CASE WHEN PRMAST.PURPRICETYPE=0 THEN PRMAST.BASEPURPRICE WHEN PRMAST.PURPRICETYPE = 1 AND PRMAST.CONVTYPE=0 THEN PRMAST.ALTPURPRICE*PRMAST.UNITCONV WHEN PRMAST.PURPRICETYPE = 1 AND PRMAST.CONVTYPE=1 THEN PRMAST.ALTPURPRICE/PRMAST.UNITCONV END) AS PRPRICE ,0 AS OPNSTKVAL,0 AS OPNSTKRATE,PRMAST.COSTMETH,IIF(PRMAST.BASEPURPRICE <> 0 OR PRMAST.PURPRICETYPE=0 OR PRMAST.UNITCONV = 0,PRMAST.BASEPURPRICE, IIF(PRMAST.PURPRICETYPE = 1 AND PRMAST.CONVTYPE=0, PRMAST.ALTPURPRICE*PRMAST.UNITCONV, PRMAST.ALTPURPRICE/PRMAST.UNITCONV )) AS BASEPURPRICE FROM((PRMAST LEFT JOIN " + Functions.ADDCOST_STKDT(Unapproved: Unapproved) + "STKDT  ON  PRMAST.PRCODE = STKDT.PRCODE) LEFT JOIN PRUNITMAS ON PRMAST.UNIT = PRUNITMAS.PRUNITID) WHERE PRMAST.COSTMETH<>4 AND PRMAST.COSTMETH<>5 AND PRMAST.COSTMETH<>2 AND STKDT.VRDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "' AND PRMAST.SERVFLG='" + false + "' GROUP BY PRMAST.PRCODE, PRMAST.BARCODE,PRMAST.PRNAME,PRUNITMAS.PRUNITNAME, PRMAST.GROUPCODE, PRMAST.TAXCODE, PRMAST.CATGCODE, PRMAST.BRAND,PRMAST.BASEPURPRICE, PRMAST.ALTPURPRICE, PRMAST.PURPRICETYPE, STKDT.VRTYPE, STKDT.TYPE,PRMAST.UNITCONV,PRMAST.CONVTYPE,PRMAST.COSTMETH" +
                " UNION ALL SELECT PRMAST.PRCODE, PRMAST.BARCODE,PRMAST.PRNAME,PRUNITMAS.PRUNITNAME AS UNIT ,SUM(OPNSTK.CONVQTY) AS STOCK, SUM(OPNSTK.CONVQTY) AS OPNSTOCK, PRMAST.GROUPCODE, PRMAST.TAXCODE, PRMAST.CATGCODE, PRMAST.BRAND,0 AS RNQTY,0 AS PRQTY, 0 AS DNQTY, 0 AS SLQTY, 0 AS PRTQTY, 0 AS SRTQTY, 0 AS ADRQTY, 0 AS ADIQTY, 0 AS PRDIQTY, 0 AS PRDRQTY, 0 AS STIQTY, 0 AS STRQTY ,0 AS PHYIQTY,0 AS PHYRQTY,0 AS DNRQTY, 0 AS RNRQTY,'0' AS AVGRATE, '0' AS AVGGRNRATE, 0 AS PRPRICE,SUM(OPNSTK.CONVVALUE) AS OPNSTKVAL,AVG(OPNSTK.RATE) AS OPNSTKRATE,PRMAST.COSTMETH,IIF(PRMAST.BASEPURPRICE <> 0 OR PRMAST.PURPRICETYPE=0 OR PRMAST.UNITCONV = 0,PRMAST.BASEPURPRICE, IIF(PRMAST.PURPRICETYPE = 1 AND PRMAST.CONVTYPE=0, PRMAST.ALTPURPRICE*PRMAST.UNITCONV, PRMAST.ALTPURPRICE/PRMAST.UNITCONV )) AS BASEPURPRICE FROM((PRMAST INNER JOIN " + Functions.VRSYS_ALLOWBR_OPNSTK(Unapproved: Unapproved) + "OPNSTK   ON  PRMAST.PRCODE = OPNSTK.PRCODE) LEFT JOIN PRUNITMAS ON PRMAST.UNIT = PRUNITMAS.PRUNITID) WHERE PRMAST.COSTMETH<>4 AND PRMAST.COSTMETH<>5 AND PRMAST.COSTMETH<>2  AND PRMAST.SERVFLG='" + false + "' GROUP BY PRMAST.PRCODE, PRMAST.BARCODE,PRMAST.PRNAME,PRUNITMAS.PRUNITNAME, PRMAST.GROUPCODE, PRMAST.TAXCODE, PRMAST.CATGCODE, PRMAST.BRAND,PRMAST.BASEPURPRICE, PRMAST.ALTPURPRICE, PRMAST.PURPRICETYPE,PRMAST.UNITCONV,PRMAST.CONVTYPE,PRMAST.COSTMETH" +
                " ) T GROUP BY T.PRCODE,T.BARCODE,T.PRNAME,T.UNIT,T.GROUPCODE,T.TAXCODE,T.CATGCODE,T.BRAND,T.COSTMETH,T.BASEPURPRICE) AS F " + (PLReport ? "" : "WHERE (QTY<>0 OR OPNSTOCK<>0)") + (Pr_Code > 0 ? " AND PRCODE=" + Pr_Code : "");
            }
            return await db.Execute(qry);
        }

        public async Task insertToCSTK(ClsDate clsDate, bool ZeroQty, bool Alt, int Pr_Code = 0, int Cost_Meth = 0, bool Unapproved = true)
        {
            PurmDB purmDB = new PurmDB(db);
            ClsPrMast clsPrMast = new ClsPrMast();
            await CreateForCStkFIFO(clsDate, Pr_Code: Pr_Code, Unapproved: Unapproved);
            DataTable dtF = await ClosingStockFIFO_LIFO();
            DataTable dtRec = await Receiptst();
            foreach (DataRow drF in dtF.Rows)
            {
                clsPrMast.PrCode = Convert.ToInt32(drF["PRCODE"]);
                decimal RTot = Convert.ToDecimal(drF["RQTY"]);
                decimal ITot = Convert.ToDecimal(drF["IQTY"]);
                decimal Stock = Convert.ToDecimal(drF["STOCK"]);
                decimal StockValue = 0;

                DataRow[] drTr = dtRec.Select("PRCODE='" + Convert.ToInt32(drF["PRCODE"]) + "'");
                if (drTr.Count() > 0)
                {
                    StockValue = 0;
                    if (Convert.ToInt32(drF["COSTMETH"]) == 4 || Convert.ToInt32(drF["COSTMETH"]) == 5)
                    {
                        DataTable _dtRec = drTr.CopyToDataTable();
                        _dtRec.Columns.Add("USED");

                        if (_dtRec.Rows.Count > 1 && Convert.ToInt32(drF["COSTMETH"]) == 5)
                        {
                            DataTable dtRec_Rev = _dtRec.Clone();
                            for (int i = _dtRec.Rows.Count - 1; i >= 0; i--)
                            {
                                dtRec_Rev.ImportRow(_dtRec.Rows[i]);
                            }
                            _dtRec = dtRec_Rev.Copy();
                        }

                        DataTable dtIssue = await IssueByPrCode(clsPrMast, Unapproved: Unapproved);
                        decimal rTot = 0, _rRate = 0;
                        foreach (DataRow drIssue in dtIssue.Rows)
                        {
                            decimal _iQty = Convert.ToDecimal(drIssue["IQTY"]);
                            if ((rTot - _iQty < 0) || (rTot == 0 && _iQty == 0))
                            {
                                foreach (DataRow drRec in _dtRec.Rows)
                                {
                                    if (Convert.ToDateTime(drIssue["VRDATE"]) >= Convert.ToDateTime(drRec["DT"]))
                                    {
                                        if (drRec["VRTYPE"].ToString() == "RET")
                                        {
                                            if (drRec["USED"].ToString() == "")
                                            {
                                                StockValue += Convert.ToDecimal(drRec["RQTY"]) * _rRate;
                                                rTot += Convert.ToDecimal(drRec["RQTY"]);
                                                drRec["USED"] = "1";
                                                if (rTot > _iQty)
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            _rRate = Convert.ToDecimal(drRec["RATE"]);
                                            if (drRec["USED"].ToString() == "")
                                            {
                                                StockValue += Convert.ToDecimal(drRec["RQTY"]) * _rRate;
                                                rTot += Convert.ToDecimal(drRec["RQTY"]);
                                                drRec["USED"] = "1";
                                                if (rTot > _iQty)
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }

                            if (_iQty > 0 && drIssue["VRTYPE"].ToString() != "TR")
                            {
                                rTot -= _iQty;
                                StockValue -= _iQty * _rRate;
                            }
                        }

                        foreach (DataRow drRec in _dtRec.Rows)
                        {
                            if (drRec["VRTYPE"].ToString() != "RET" && drRec["VRTYPE"].ToString() != "NTR")
                                _rRate = Convert.ToDecimal(drRec["RATE"]);

                            if (drRec["USED"].ToString() == "")
                            {
                                StockValue += Convert.ToDecimal(drRec["RQTY"]) * _rRate;
                            }
                        }
                    }
                }

                decimal AvgRate = 0;
                if (Stock != 0)
                    AvgRate = Math.Round(StockValue / Stock, Variables.DecimalPoint);

                string prname = drF["PRNAME"].ToString().Replace("'", "''");

                qry = "UPDATE " + STK_TB + " SET STOCK='" + Stock + "',UNIT='" + drF["UNIT"] + "',QTY='" + Stock + "',PRPRICE='" + AvgRate + "' WHERE PRCODE='" + drF["PRCODE"] + "'";
                if (await db.Execute(qry) <= 0)
                {
                    qry = @"INSERT INTO " + STK_TB + " (PRCODE, BARCODE, PRNAME, OPNSTOCK, STOCK,UNIT, GROUPCODE, TAXCODE, CATGCODE,  BRAND, QTY , AVGRATE, AVGGRNRATE, PRPRICE, OPNSTKVAL) " +
                    "VALUES('" + drF["PRCODE"] + "','" + drF["BARCODE"] + "',N'" + prname + "','0','" + Stock + "','" + drF["UNIT"] + "','" + drF["GROUPCODE"] + "','" + drF["TAXCODE"] + "','" + drF["CATGCODE"] + "','" + drF["BRAND"] + "','" + Stock + "','0','0','" + AvgRate + "','0')";
                    await db.Execute(qry);
                }
            }

            await UpdateLastPurchaseCost(clsDate, false, Alt, false, Pr_Code: Pr_Code, Unapproved: Unapproved);

            string WACInsQry = "";
            int wi = 0;
            DataTable dtStkData = await FullDataFromSTKTB();
            DataTable dtWACTrans = await WACTransactions();
            DataTable dtWAC = await ClosingStockWAC();

            if (ZeroQty == true)
                await InsertWACWithZeroStock();

            foreach (DataRow drW in dtWAC.Rows)
            {
                clsPrMast.PrCode = Convert.ToInt32(drW["PRCODE"]);
                decimal RTot = 0;
                decimal ITot = 0;
                decimal Stock = 0;
                decimal StockValue = 0;
                decimal Rate = 0;
                decimal Stot = 0;

                DataRow[] dtRW1 = dtWACTrans.Select("PRCODE='" + Convert.ToInt32(drW["PRCODE"]) + "'", "VRDATE,UTIME ASC");
                var v = dtRW1.CopyToDataTable();
                string Calc = "";
                foreach (DataRow drW1 in dtRW1)
                {
                    decimal RQty = Convert.ToDecimal(drW1["RQTY"]);
                    decimal IQty = Convert.ToDecimal(drW1["IQTY"]);
                    decimal TRate = Convert.ToDecimal(drW1["RATE"]);
                    if (RQty != 0)
                    {
                        if (RTot == 0 && ITot > 0 && drW1["VRTYPE"].ToString() != "RET" && drW1["VRTYPE"].ToString() != "NTR" && (drW1["VRTYPE"].ToString() != "ST" || (Variables.StkIssRec && await purmDB.GetTrType(Convert.ToInt32(drW1["VRNO"].ToString() == "" ? "0" : drW1["VRNO"].ToString())) != "ST")) && Rate == 0)
                            StockValue += (Stock * TRate);

                        RTot += RQty;
                        if (drW1["VRTYPE"] != DBNull.Value && drW1["VRTYPE"].ToString() != "RET" && drW1["VRTYPE"].ToString() != "NTR" && (drW1["VRTYPE"].ToString() != "ST" || (Variables.StkIssRec && await purmDB.GetTrType(Convert.ToInt32(drW1["VRNO"].ToString() == "" ? "0" : drW1["VRNO"].ToString())) != "ST")))
                        {
                            Rate = TRate;
                            StockValue += (RQty * TRate);
                        }
                        else
                        {
                            StockValue += (RQty * Rate);
                        }

                        bool isPrevNegBal = Stock < 0 && (Stock + RQty) > 0; // 28_04_2025

                        Stock += RQty;
                        if (Stock < 0)
                        {
                            StockValue = 0;
                            Rate = 0;
                        }
                        else if (Stock == 0)
                        {
                            StockValue = 0;
                        }

                        if (Stock != 0 && (drW1["VRTYPE"].ToString() != "ST" || (Variables.StkIssRec && await purmDB.GetTrType(Convert.ToInt32(drW1["VRNO"].ToString() == "" ? "0" : drW1["VRNO"].ToString())) != "ST")) && drW1["VRTYPE"].ToString() != "RET" && drW1["VRTYPE"].ToString() != "NTR")
                        {
                            if (isPrevNegBal)
                            {
                                Rate = TRate;
                                StockValue = Stock * Rate;
                            }
                            else
                            {
                                Rate = Math.Round(StockValue / Stock, Variables.DecimalPoint, MidpointRounding.AwayFromZero);
                            }
                        }
                        else if (Stock > 0 && Rate == 0 && isPrevNegBal)
                        {
                            Rate = TRate;
                            StockValue = Stock * Rate;
                        }
                    }
                    else if (IQty != 0)
                    {
                        if (drW1["VRTYPE"] != DBNull.Value && (drW1["VRTYPE"].ToString() == "RET" || drW1["VRTYPE"].ToString() == "NTR" || drW1["VRTYPE"].ToString() == "OS"))
                        {
                            ITot += IQty;
                            StockValue -= (IQty * TRate);
                            Stock -= IQty;
                            if (Stock != 0)
                                Rate = Math.Round(StockValue / Stock, Variables.DecimalPoint, MidpointRounding.AwayFromZero);
                        }
                        else
                        {
                            ITot += IQty;
                            StockValue -= (IQty * Rate);
                            Stock -= IQty;
                        }

                        if (Stock < 0)
                        {
                            StockValue = 0;
                            Rate = 0;
                        }
                        else if (Stock == 0)
                        {
                            StockValue = 0;
                        }

                        Stot += IQty * TRate;
                    }
                    if (Rate < 0)
                        Rate = -1 * Rate;

                    Calc += (Calc == "" ? "" : "\nUNION ALL\n") + " SELECT '" + RQty + "' AS RQty,'" + IQty + "' AS RQty,'" + TRate + "' AS TRate,'" + StockValue + "' AS StockValue,'" + Stock + "' AS Stock,'" + ITot + "' AS ITot,'" + RTot + "' AS RTot,'" + Rate + "' AS Rate";
                }

                DataRow[] drStk = dtStkData.Select("PRCODE='" + drW["PRCODE"] + "'");
                if (drStk.Count() > 0)
                {
                    DataRow drS = drStk[0];
                    if (drS["UNIT"].ToString() == drW["UNIT"].ToString())
                    {
                        qry = "UPDATE " + STK_TB + " SET STOCK='" + Stock + "',QTY='" + Stock + "',PRPRICE='" + Rate + "',AVGRATE='" + Rate + "',AVGGRNRATE='" + Rate + "' WHERE PRCODE='" + drW["PRCODE"] + "' AND UNIT='" + drW["UNIT"] + "'";
                    }
                    else
                    {
                        clsPrMast.PrCode = Convert.ToInt32(drW["PRCODE"]);
                        DataTable dt = await UnitConDetailsByCode(clsPrMast);
                        if (dt.Rows.Count > 0)
                        {
                            DataRow dr = dt.Rows[0];
                            int ConvType = Convert.ToInt32(dr["CONVTYPE"]);
                            decimal ConvRate = Convert.ToDecimal(dr["UNITCONV"]);
                            if (ConvType == 0)
                            {
                                if (ConvRate != 0)
                                {
                                    Stock = Math.Round(Stock / ConvRate, Variables.QtyDecimalPoint, MidpointRounding.AwayFromZero);
                                    Rate = Math.Round(Rate * ConvRate, Variables.DecimalPoint, MidpointRounding.AwayFromZero);
                                }
                            }
                            else if (ConvType == 1)
                            {
                                if (ConvRate != 0)
                                {
                                    Stock = Math.Round(Stock * ConvRate, Variables.QtyDecimalPoint, MidpointRounding.AwayFromZero);
                                    Rate = Math.Round(Rate / ConvRate, Variables.DecimalPoint, MidpointRounding.AwayFromZero);
                                }
                            }
                        }
                        qry = "UPDATE " + STK_TB + " SET STOCK='" + Stock + "',QTY='" + Stock + "',PRPRICE='" + Rate + "',AVGRATE='" + Rate + "',AVGGRNRATE='" + Rate + "' WHERE PRCODE='" + drS["PRCODE"] + "' AND UNIT='" + drS["UNIT"] + "'";
                    }
                    db.Execute(qry);
                }
                else
                {

                    string prname = drW["PRNAME"].ToString().Replace("'", "''");

                    if (WACInsQry == "")
                        WACInsQry = "SELECT TOP 1 '" + drW["PRCODE"] + "' AS PRCODE,'" + drW["BARCODE"] + "' AS BARCODE,N'" + prname + "' AS PRNAME,'0' AS OPNSTOCK,'" + Convert.ToDecimal(Stock.ToString(Variables.QtyDecString)) + "' AS STOCK,'" + drW["UNIT"] + "' AS UNIT,'" + drW["GROUPCODE"] + "' AS GROUPCODE,'" + drW["TAXCODE"] + "' AS TAXCODE,'" + drW["CATGCODE"] + "' AS CATGCODE,'" + drW["BRAND"] + "' AS BRAND,'" + Convert.ToDecimal(Stock.ToString(Variables.QtyDecString)) + "' AS QTY,'0' AS AVGRATE,'0' AS AVGGRNRATE,'" + Convert.ToDecimal(Rate.ToString(Variables.DecString)) + "' AS PRPRICE,'0' AS OPNSTKVAL FROM PRMAST ";
                    else
                        WACInsQry += " UNION ALL (" + "SELECT TOP 1 '" + drW["PRCODE"] + "' AS PRCODE,'" + drW["BARCODE"] + "' AS BARCODE,N'" + prname + "' AS PRNAME,'0' AS OPNSTOCK,'" + Convert.ToDecimal(Stock.ToString(Variables.QtyDecString)) + "' AS STOCK,'" + drW["UNIT"] + "' AS UNIT,'" + drW["GROUPCODE"] + "' AS GROUPCODE,'" + drW["TAXCODE"] + "' AS TAXCODE,'" + drW["CATGCODE"] + "' AS CATGCODE,'" + drW["BRAND"] + "' AS BRAND,'" + Convert.ToDecimal(Stock.ToString(Variables.QtyDecString)) + "' AS QTY,'0' AS AVGRATE,'0' AS AVGGRNRATE,'" + Convert.ToDecimal(Rate.ToString(Variables.DecString)) + "' AS PRPRICE,'0' AS OPNSTKVAL FROM PRMAST) ";
                }

                if ((wi % 30 == 0 && wi > 0) || wi == dtWAC.Rows.Count - 1)
                {
                    qry = @"INSERT INTO " + STK_TB + @"   (PRCODE, BARCODE, PRNAME, OPNSTOCK, STOCK, UNIT, GROUPCODE, TAXCODE, CATGCODE, BRAND, QTY, AVGRATE, AVGGRNRATE, PRPRICE, OPNSTKVAL)
                        SELECT PRCODE, BARCODE, PRNAME, OPNSTOCK, STOCK, UNIT, GROUPCODE, TAXCODE, CATGCODE, BRAND, QTY, AVGRATE, AVGGRNRATE, PRPRICE, OPNSTKVAL
                        FROM( " + WACInsQry + " )T";
                    db.Execute(qry);
                    WACInsQry = "";
                }
                wi++;
            }
        }

        public async Task<int> CreateForCStkFIFO(ClsDate clsDate, int Pr_Code = 0, bool Unapproved = true)
        {
            Stk_Det = "CSTKDet_" + DateTime.Now.Hour.ToString() + "_" + DateTime.Now.Minute.ToString() + "_" + DateTime.Now.Second.ToString();

            qry = @"SELECT* INTO " + Stk_Det + @" FROM(SELECT '" + Variables.fyStartDate.ToString(Variables.format) + @"' AS VRDATE, 'OS' AS VRTYPE, 0 AS VRNO, PRMAST.PRCODE, PRMAST.BARCODE, PRMAST.PRNAME, PRUNITMAS.PRUNITNAME AS UNIT, SUM(IIF(OPNSTK.CONVQTY IS NULL,0,OPNSTK.CONVQTY)) AS STOCK, PRMAST.GROUPCODE, PRMAST.TAXCODE, PRMAST.CATGCODE, PRMAST.BRAND, PRMAST.COSTMETH,
            IIF(SUM(IIF(OPNSTK.CONVQTY IS NULL,0,OPNSTK.CONVQTY))   > 0, SUM(OPNSTK.CONVQTY), 0) AS RQTY, IIF(SUM(IIF(OPNSTK.CONVQTY IS NULL,0,OPNSTK.CONVQTY)) < 0, -1 * SUM(OPNSTK.CONVQTY), 0) AS IQTY, ROUND(IIF(SUM(IIF(OPNSTK.CONVQTY IS NULL,0,OPNSTK.CONVQTY))<>0,SUM(IIF(OPNSTK.CONVVALUE IS NULL,0,OPNSTK.CONVVALUE))/SUM(IIF(OPNSTK.CONVQTY IS NULL,0,OPNSTK.CONVQTY)),0)," + Variables.DecimalPoint + ") AS RATE ,'" + Variables.fyStartDate.ToString(Variables.format) + @"' AS UTIME,SUM(IIF(OPNSTK.CONVVALUE IS NULL,0,OPNSTK.CONVVALUE)) AS OpStkVal
            FROM((PRMAST LEFT JOIN " + Functions.VRSYS_ALLOWBR_OPNSTK(Unapproved: Unapproved) + "OPNSTK ON PRMAST.PRCODE=OPNSTK.PRCODE)  INNER JOIN PRUNITMAS ON PRMAST.UNIT = PRUNITMAS.PRUNITID) WHERE(PRMAST.COSTMETH = 4 OR PRMAST.COSTMETH = 5 OR PRMAST.COSTMETH=2)  AND PRMAST.SERVFLG='" + false + @"'" + Functions.APRVLP(true, "PRMAST") + Functions.VRSYS_STK_OP_EXTRA("OPNSTK", "PRCODE", "PRMAST", "PRCODE") + @" GROUP BY PRMAST.PRCODE, PRMAST.BARCODE, PRMAST.PRNAME, PRUNITMAS.PRUNITNAME, PRMAST.GROUPCODE, PRMAST.TAXCODE, PRMAST.CATGCODE, PRMAST.BRAND, PRMAST.COSTMETH
            UNION ALL SELECT STKDT.VRDATE, STKDT.VRTYPE, (CASE WHEN STKDT.VRTYPE = 'NT' THEN STKDT.NTNO WHEN  STKDT.VRTYPE = 'PR' THEN STKDT.TRNNO WHEN STKDT.VRTYPE = 'AD' OR STKDT.VRTYPE = 'ST' OR STKDT.VRTYPE = 'PHY' THEN STKDT.TRNNO WHEN STKDT.VRTYPE = 'RET' THEN STKDT.TRNNO WHEN STKDT.VRTYPE = 'NTR' THEN STKDT.NTRNO END) AS VRNO, PRMAST.PRCODE, PRMAST.BARCODE, PRMAST.PRNAME, PRUNITMAS.PRUNITNAME AS UNIT, 0 AS STOCK, PRMAST.GROUPCODE, PRMAST.TAXCODE, PRMAST.CATGCODE, PRMAST.BRAND, PRMAST.COSTMETH,
            IIF((STKDT.TYPE = 'R'), STKDT.CONVQTY, 0)AS RQTY,
            IIF(STKDT.TYPE = 'I', STKDT.CONVQTY, 0)AS IQTY, STKDT.CONVRATE AS RATE,STKDT.UTIME,0 as OpStkVal
            FROM((PRMAST INNER  JOIN " + Functions.ADDCOST_STKDT(Unapproved: Unapproved) + @"STKDT  ON  PRMAST.PRCODE = STKDT.PRCODE) INNER JOIN PRUNITMAS ON PRMAST.UNIT = PRUNITMAS.PRUNITID)
            WHERE STKDT.VRDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "' AND (PRMAST.COSTMETH=4 OR PRMAST.COSTMETH=5 OR PRMAST.COSTMETH=2) AND PRMAST.SERVFLG='" + false + "' AND  (STKDT.VRTYPE = 'NT' OR STKDT.VRTYPE = 'RET' OR STKDT.VRTYPE = 'AD' OR STKDT.VRTYPE = 'PR' OR STKDT.VRTYPE = 'ST' OR STKDT.VRTYPE = 'PHY' OR STKDT.VRTYPE = 'NTR')) TB" + (Pr_Code > 0 ? " WHERE PRCODE=" + Pr_Code : "") + " ORDER BY VRDATE,UTIME,VRNO";

            return await db.Execute(qry);
        }

        public async Task<DataTable> ClosingStockFIFO_LIFO()
        {
            qry = "SELECT DISTINCT PRCODE,COSTMETH,BARCODE,PRNAME,UNIT , GROUPCODE, TAXCODE, CATGCODE, BRAND,SUM(RQTY) AS RQTY,SUM(IQTY) AS IQTY ,SUM(RQTY)-SUM(IQTY) AS STOCK FROM " + Stk_Det + " WHERE COSTMETH=5 OR COSTMETH=4 GROUP BY PRCODE,COSTMETH,BARCODE,PRNAME,UNIT , GROUPCODE, TAXCODE, CATGCODE, BRAND";
            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> Receiptst()
        {
            qry = "SELECT *,VRDATE AS DT FROM " + Stk_Det + " WHERE  RQTY<>'0'";
            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> IssueByPrCode(ClsPrMast clsPrMast, bool Unapproved = true)
        {
            qry = "SELECT * FROM ( SELECT VRDATE,VRTYPE,VRNO,PRCODE,RQTY,IQTY,RATE,IIF(RQTY<>0,0,1) AS ORD,UTIME FROM " + Stk_Det + " WHERE PRCODE='" + clsPrMast.PrCode + "'" +
                "   UNION ALL SELECT VRDATE,VRTYPE,TRNNO AS VRNO,PRCODE,0 AS RQTY,CONVQTY AS IQTY,CONVRATE AS RATE,1 AS ORD,UTIME FROM " + Functions.ADDCOST_STKDT(Unapproved: Unapproved) + "STKDT WHERE PRCODE=" + clsPrMast.PrCode + " AND VRTYPE='TR' AND TYPE='I') AS T ORDER BY VRDATE,UTIME,ORD";
            return await db.GetDatatable(qry);
        }

        public async Task UpdateLastPurchaseCost(ClsDate clsDate, bool Gdwn, bool Alt, bool Dtls, int Pr_Code = 0, bool Unapproved = true)
        {
            string LPC_TABLE = "LPC_TABLE_" + DateTime.Now.Hour.ToString() + "_" + DateTime.Now.Minute.ToString() + "_" + DateTime.Now.Second.ToString();
            if (Alt)
            {
                qry = @"UPDATE DTL SET DTL.STOCK = IIF(CONVTYPE=0 AND UNITCONV<>0,DTL.STOCK/UNITCONV, IIF(CONVTYPE=1 AND UNITCONV<>0,DTL.STOCK*UNITCONV,DTL.STOCK))
,DTL.OPNSTOCK = IIF(CONVTYPE=0 AND UNITCONV<>0,DTL.OPNSTOCK/UNITCONV, IIF(CONVTYPE=1 AND UNITCONV<>0,DTL.OPNSTOCK*UNITCONV,DTL.OPNSTOCK)) FROM " + STK_TB + @" DTL INNER JOIN PRMAST ON DTL.PRCODE = PRMAST.PRCODE WHERE COSTMETH = 1 OR COSTMETH = 3 OR COSTMETH = 6";
                await db.Execute(qry);
            }

            qry = "UPDATE " + STK_TB + " SET QTY = QTY + STOCK WHERE PRCODE IN (SELECT PRCODE FROM PRMAST WHERE COSTMETH = 1 OR COSTMETH = 3 OR COSTMETH = 6)";
            await db.Execute(qry);

            qry = "CREATE TABLE " + LPC_TABLE + "(SLNO INT IDENTITY(1,1), PRCODE INT" + (Gdwn ? ", GDWCODE INT" : "") + ", PRRATE MONEY, OPNRATE MONEY)";
            await db.Execute(qry);

            string Tbqry = "";
            if (Gdwn)
            {
                Tbqry = @"INSERT INTO " + LPC_TABLE + @" (PRCODE, GDWCODE, PRRATE, OPNRATE) SELECT PRMAST.PRCODE, PRMAST.GDWCODE, PURT.PRRATE, OPNSTK.OPNRATE
FROM (((SELECT * FROM PRMAST,(SELECT GDWID AS GDWCODE FROM GDWMAS) AS GDWMAS) PRMAST LEFT JOIN (SELECT PRCODE,GDWCODE,IIF(SUM(CONVQTY) <> 0, SUM(CONVVALUE)/SUM(CONVQTY), 0) AS OPNRATE FROM " + Functions.VRSYS_ALLOWBR_OPNSTK(Unapproved: Unapproved) + @"OPNSTK GROUP BY PRCODE,GDWCODE)OPNSTK ON PRMAST.PRCODE = OPNSTK.PRCODE AND PRMAST.GDWCODE = OPNSTK.GDWCODE) 
LEFT JOIN (SELECT PRCODE,GDWCODE,PURDATE,CONVRATE AS PRRATE,ORDNO FROM PURT WHERE CANCELFLG = 'FALSE' AND PURDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "'" + Functions.APRVLR2(true, Unapproved, "PURM", "PURT", "VRNO", "VRNO") + Functions.BRSTMNT("PURT", true) + @") AS PURT ON PRMAST.PRCODE = PURT.PRCODE AND PRMAST.GDWCODE = PURT.GDWCODE)
WHERE PRMAST.COSTMETH = 3 ORDER BY PRMAST.PRCODE ASC,PURT.PURDATE DESC,PURT.ORDNO DESC";
            }
            else
            {
                Tbqry = @"INSERT INTO " + LPC_TABLE + @" (PRCODE, PRRATE, OPNRATE) SELECT PRMAST.PRCODE, PURT.PRRATE, OPNSTK.OPNRATE
FROM ((PRMAST LEFT JOIN (SELECT PRCODE,IIF(SUM(CONVQTY) <> 0, SUM(CONVVALUE)/SUM(CONVQTY), 0) AS OPNRATE FROM " + Functions.VRSYS_ALLOWBR_OPNSTK(Unapproved: Unapproved) + @"OPNSTK GROUP BY PRCODE)OPNSTK ON PRMAST.PRCODE = OPNSTK.PRCODE) 
LEFT JOIN (SELECT PRCODE,PURDATE,CONVRATE AS PRRATE,ORDNO FROM PURT WHERE CANCELFLG = 'FALSE' AND PURDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "'" + Functions.APRVLR2(true, Unapproved, "PURM", "PURT", "VRNO", "VRNO") + Functions.BRSTMNT("PURT", true) + @") AS PURT ON PRMAST.PRCODE = PURT.PRCODE)
WHERE PRMAST.COSTMETH = 3" + (Pr_Code > 0 ? " AND PRMAST.PRCODE = " + Pr_Code : "") + " ORDER BY PRMAST.PRCODE ASC,PURT.PURDATE DESC,PURT.ORDNO DESC";
            }
            await db.Execute(Tbqry);

            string Rate = "IIF(LPC.PRRATE IS NULL, IIF(LPC.OPNRATE IS NULL, 0, LPC.OPNRATE), LPC.PRRATE)";
            string dtls_Stmnt = Dtls ? ", RAVGRATE = " + Rate + ", RPRPRICE = " + Rate + ", RAVGGRNRATE = " + Rate + ", IAVGRATE = " + Rate + ", IPRPRICE = " + Rate + ", IAVGGRNRATE = " + Rate + "" : "";
            if (Gdwn)
            {
                qry = @"UPDATE STK SET STK.AVGRATE = " + Rate + ", STK.PRPRICE = " + Rate + ", STK.AVGGRNRATE = " + Rate + dtls_Stmnt + " FROM " + STK_TB + @" AS STK INNER JOIN 
(SELECT DTLS.* FROM " + LPC_TABLE + @" AS DTLS 
	INNER JOIN (SELECT PRCODE,GDWCODE,MIN(SLNO) AS SLNO FROM " + LPC_TABLE + @" GROUP BY PRCODE,GDWCODE
) LPC ON DTLS.PRCODE = LPC.PRCODE AND DTLS.GDWCODE = LPC.GDWCODE AND DTLS.SLNO = LPC.SLNO ) LPC ON STK.PRCODE = LPC.PRCODE AND STK.GDWCODE = LPC.GDWCODE";
            }
            else
            {
                qry = @"UPDATE STK SET STK.AVGRATE = " + Rate + ", STK.PRPRICE = " + Rate + ", STK.AVGGRNRATE = " + Rate + dtls_Stmnt + " FROM " + STK_TB + @" AS STK INNER JOIN 
(SELECT DTLS.* FROM " + LPC_TABLE + @" AS DTLS 
	INNER JOIN (SELECT PRCODE,MIN(SLNO) AS SLNO FROM " + LPC_TABLE + @" GROUP BY PRCODE
) LPC ON DTLS.PRCODE = LPC.PRCODE AND DTLS.SLNO = LPC.SLNO ) LPC ON STK.PRCODE = LPC.PRCODE";
            }
            await db.Execute(qry);

            if (Alt)
            {
                Rate = "IIF(CONVTYPE=0 AND UNITCONV<>0,AVGRATE*UNITCONV, IIF(CONVTYPE=1 AND UNITCONV<>0,AVGRATE/UNITCONV,AVGRATE))";
                dtls_Stmnt = Dtls ? ", RAVGRATE = " + Rate + ", RPRPRICE = " + Rate + ", RAVGGRNRATE = " + Rate + ", IAVGRATE = " + Rate + ", IPRPRICE = " + Rate + ", IAVGGRNRATE = " + Rate + "" : "";

                qry = @"UPDATE DTL SET DTL.AVGRATE = " + Rate + ",DTL.PRPRICE = " + Rate + dtls_Stmnt + " FROM " + STK_TB + @" DTL INNER JOIN PRMAST ON DTL.PRCODE = PRMAST.PRCODE WHERE COSTMETH = 3";
                await db.Execute(qry);
            }

            qry = "DROP TABLE " + LPC_TABLE;
            await db.Execute(qry);
        }


        public async Task<DataTable> FullDataFromSTKTB()
        {
            qry = "SELECT * FROM " + STK_TB + " ";
            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> WACTransactions()
        {
            qry = "SELECT VRDATE, VRTYPE, VRNO, PRCODE, BARCODE, PRNAME, UNIT, STOCK, GROUPCODE, TAXCODE, CATGCODE, BRAND, COSTMETH, RQTY, IQTY, RATE, UTIME FROM " + Stk_Det + " WHERE COSTMETH='2' AND (RQTY<>'0' OR IQTY<>'0')  ORDER BY VRDATE,UTIME";
            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> ClosingStockWAC()
        {
            qry = "SELECT PRCODE,COSTMETH,BARCODE,PRNAME,UNIT , GROUPCODE, TAXCODE, CATGCODE, BRAND,SUM(RQTY) AS RQTY,SUM(IQTY) AS IQTY ,SUM(RQTY)-SUM(IQTY) AS STOCK FROM " + Stk_Det + " WHERE COSTMETH='2' AND (RQTY<>0 OR IQTY<>0) GROUP BY PRCODE,COSTMETH,BARCODE,PRNAME,UNIT , GROUPCODE, TAXCODE, CATGCODE, BRAND ";
            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> InsertWACWithZeroStock()
        {
            qry = @"INSERT INTO " + STK_TB + @"  (PRCODE, BARCODE, PRNAME, OPNSTOCK, STOCK, UNIT, GROUPCODE, TAXCODE, CATGCODE, BRAND, QTY, AVGRATE, AVGGRNRATE, PRPRICE, OPNSTKVAL)
            SELECT PRCODE, BARCODE, PRNAME, '0' AS OPNSTOCK, '0' AS STOCK, UNIT, GROUPCODE, TAXCODE, CATGCODE, BRAND, '0' AS QTY, '0' AS AVGRATE, '0' AS AVGGRNRATE, '0' AS PRPRICE, '0' AS OPNSTKVAL
            FROM( SELECT PRCODE, BARCODE, PRNAME, SUM(RQTY) AS RQTY, SUM(IQTY) AS IQTY,SUM(STOCK) AS STOCK, UNIT, GROUPCODE, TAXCODE, CATGCODE, BRAND, '0' AS QTY, '0' AS AVGRATE, '0' AS AVGGRNRATE, '0' AS PRPRICE, '0' AS OPNSTKVAL
            FROM " + Stk_Det + " WHERE (COSTMETH = 2)  GROUP BY PRCODE, COSTMETH, BARCODE, PRNAME, UNIT, GROUPCODE, TAXCODE, CATGCODE, BRAND) AS T WHERE (RQTY = 0) AND (IQTY = 0) AND (STOCK = 0)";
            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> UnitConDetailsByCode(ClsPrMast clsPrMast)
        {
            qry = "SELECT UNIT,ALTUNIT,UNITCONV,CONVTYPE FROM PRMAST WHERE PRCODE='" + clsPrMast.PrCode + "'";
            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> ClosingStock_PL()
        {
            qry = @"SELECT " + STK_TB + ".GROUPCODE, '' AS DS, FORMAT(SUM(QTY),'" + Variables.QtyDecString + "') AS Stock, CAST(SUM(((QTY)*(CASE WHEN AVGRATE<>0 THEN AVGRATE WHEN PRPRICE<>0 THEN PRPRICE ELSE AVGGRNRATE END))) AS DECIMAL(18," + Variables.DecimalPoint + ")) AS Tvalue FROM " + STK_TB + " WHERE QTY <>0 GROUP BY " + STK_TB + ".GROUPCODE " +
                " UNION ALL SELECT  0 As GROUPCODE,'Total' AS DS, FORMAT(SUM(QTY),'" + Variables.QtyDecString + "')  AS Stock, CAST(SUM(((QTY)*(CASE WHEN AVGRATE<>0 THEN AVGRATE WHEN PRPRICE<>0 THEN PRPRICE ELSE AVGGRNRATE END))) AS DECIMAL(18," + Variables.DecimalPoint + ")) AS Tvalue FROM " + STK_TB + " WHERE QTY <>0 ";

            return await db.GetDatatable(qry);
        }

        public async Task DropTable()
        {
            qry = "DROP TABLE " + STK_TB;
            int R = await db.Execute(qry);

            qry = "DROP TABLE " + Stk_Det;
            R = await db.Execute(qry);

            STK_TB = "";
            Stk_Det = "";
        }

        public async Task<DataTable> GetPendingSales(ClsDate clsDate, bool Unapproved = true)
        {
            qry = @"SELECT GDNT.PRCODE,GDNT.QTY,GDNT.CONVQTY,GDNT.PRRATE FROM (((SELECT DISTINCT STKDT.VRTYPE, STKDT.PRCODE, STKDT.NTNO, STKDT.TRNNO FROM " + Functions.ADDCOST_STKDT(Unapproved: Unapproved) + @" STKDT WHERE STKDT.VRTYPE = 'NT' 
            AND STKDT.TYPE = 'I' AND STKDT.VRDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + @"' AND STKDT.CLOSES = 'FALSE') STKDT1 INNER JOIN GDNT ON GDNT.INVNO = STKDT1.NTNO )
            LEFT JOIN (SELECT DISTINCT STKDT2.NTNO FROM " + Functions.ADDCOST_STKDT(RetTable: true, Unapproved: Unapproved) + " STKDT2 WHERE (STKDT2.VRTYPE = 'TR' OR STKDT2.VRTYPE = 'NT') AND TYPE = 'I' AND STKDT2.VRDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + @"' AND ((STKDT2.VRTYPE = 'TR' AND STKDT2.NTNO > 0) OR (STKDT2.VRTYPE = 'NT' AND STKDT2.TRNNO > 0)) ) 
            STKDT4 ON STKDT1.NTNO = STKDT4.NTNO) WHERE GDNT.PRCODE = STKDT1.PRCODE AND STKDT4.NTNO IS NULL 
            UNION ALL
            SELECT STKDT1.PRCODE,STKDT1.NTQTY-IIF(STKDT2.NTQTY IS NULL,0,STKDT2.NTQTY),STKDT1.NTQTY-IIF(STKDT2.NTQTY IS NULL,0,STKDT2.NTQTY),GDNT.RATE 
            FROM ((((SELECT STKDT.NTNO,STKDT.PRCODE,sum(IIF(STKDT.CONVQTY is null or STKDT.CONVQTY=0,STKDT.QTY,STKDT.CONVQTY)) as NTQTY FROM " + Functions.ADDCOST_STKDT(Unapproved: Unapproved) + " STKDT WHERE STKDT.VRTYPE = 'NT' AND STKDT.TYPE = 'I' AND STKDT.VRDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + @"' GROUP BY STKDT.NTNO,STKDT.PRCODE) STKDT1
            LEFT JOIN (SELECT STKDT.NTNO,STKDT.PRCODE,sum(IIF(STKDT.CONVQTY is null or STKDT.CONVQTY=0,STKDT.QTY,STKDT.CONVQTY)) as NTQTY FROM " + Functions.ADDCOST_STKDT(Unapproved: Unapproved) + " STKDT WHERE STKDT.VRTYPE = 'TR' AND STKDT.TYPE = 'I' AND STKDT.VRDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + @"' GROUP BY STKDT.NTNO,STKDT.PRCODE) STKDT2  ON STKDT1.NTNO=STKDT2.NTNO)
            INNER JOIN (SELECT DISTINCT GDNT.INVNO,GDNT.PRCODE,AVG(GDNT.PRRATE) as RATE FROM GDNT GROUP BY GDNT.INVNO,GDNT.PRCODE) GDNT ON GDNT.INVNO = STKDT1.NTNO)
            INNER JOIN (SELECT DISTINCT STKDT.NTNO FROM " + Functions.ADDCOST_STKDT(Unapproved: Unapproved) + " STKDT WHERE STKDT.VRTYPE = 'TR' AND STKDT.TYPE = 'I' AND STKDT.VRDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + @"') STKDT3 on STKDT3.NTNO=STKDT1.NTNO )
            WHERE STKDT1.PRCODE=STKDT2.PRCODE AND STKDT1.PRCODE=GDNT.PRCODE AND STKDT1.NTQTY<>IIF(STKDT2.NTQTY IS NULL,0,STKDT2.NTQTY)";

            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> GetStockByPrCode(ClsPrMast clsPrMast)
        {
            qry = "SELECT * FROM " + STK_TB + " WHERE PRCODE='" + clsPrMast.PrCode + "'";
            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> ReceiptNotesByPrCode(ClsPrMast clsPrMast)
        {
            qry = "SELECT * FROM " + Stk_Det + " WHERE " + Stk_Det + ".PRCODE='" + clsPrMast.PrCode + "' AND RQTY<>'0' AND " + Stk_Det + ".VRTYPE='NT'";
            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> TransactionsWithSalesByPrCode(ClsPrMast clsPrmast, ClsDate clsDate, bool Unapproved = true)
        {
            qry = "SELECT * FROM ( SELECT VRDATE,VRTYPE,VRNO,PRCODE,RQTY,IQTY,RATE,IIF(RQTY<>0,0,1) AS ORD,UTIME FROM " + Stk_Det + " WHERE COSTMETH='2' AND PRCODE='" + clsPrmast.PrCode + "' AND CAST(VRDATE AS DATETIME) BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "' " +
                "   UNION ALL SELECT VRDATE,VRTYPE,TRNNO AS VRNO,PRCODE,0 AS RQTY,CONVQTY AS IQTY,CONVRATE AS RATE,1 AS ORD,UTIME FROM " + Functions.ADDCOST_STKDT(Unapproved: Unapproved) + "STKDT WHERE PRCODE=" + clsPrmast.PrCode + " AND VRTYPE='TR' AND TYPE='I' AND VRDATE BETWEEN '" + clsDate.DateFrom + "' AND '" + clsDate.ToDate + "' ) AS T ORDER BY VRDATE,UTIME,ORD";
            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> ClosingStockValueLedgerWise()
        {
            qry = "SELECT ACCMAS.ID AS ACCID,PRGRPMAS.PRGRPID,ACCMAS.ACCNAME,FORMAT(0,'" + Variables.DecString + "') AS STOCK FROM ((PRGRPMAS INNER JOIN  " + STK_TB + " ON PRGRPMAS.PRGRPID = " + STK_TB + ".GROUPCODE) INNER JOIN  ACCMAS ON PRGRPMAS.STOCKACCID = ACCMAS.ID)   GROUP BY ACCMAS.ACCNAME,ACCMAS.ID ,PRGRPMAS.PRGRPID";
            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> ReceiptstyByPrCode(ClsPrMast clsPrMast)
        {
            qry = "SELECT *,VRDATE AS DT FROM " + Stk_Det + " WHERE " + Stk_Det + ".PRCODE='" + clsPrMast.PrCode + "' AND RQTY<>'0'";
            return await db.GetDatatable(qry);
        }

        public async Task<int> Update(ClsPrMast clsPrMast, byte[] originalRowVersion)
        {
            int servFlg = BitValue(clsPrMast.ServFlg);
            int batchFlg = BitValue(clsPrMast.BatchFlg);
            int manDateFlg = BitValue(clsPrMast.ManDateFlg);
            int expDateFlg = BitValue(clsPrMast.ExpDateFlg);
            int priceCatFlg = BitValue(clsPrMast.PriceCatFlg);
            int active = BitValue(clsPrMast.Active);
            int posFlg = BitValue(clsPrMast.PosFlg);
            int slNoTrack = BitValue(clsPrMast._SLNOTRACK);

            qry =
                "UPDATE PRMAST SET " +
                "PRNAME=N'" + SqlEscape(clsPrMast.PrName) + "'," +
                "ALIAS=N'" + SqlEscape(clsPrMast.Alias) + "'," +
                "PRINTNAME=N'" + SqlEscape(clsPrMast.PrintName) + "'," +
                "DESCRIPTION=N'" + SqlEscape(clsPrMast.Desc) + "'," +
                "UPCCODE=N'" + SqlEscape(clsPrMast.UpcCode) + "'," +
                "SKUCODE=N'" + SqlEscape(clsPrMast.SkuCode) + "'," +
                "BARCODE=N'" + SqlEscape(clsPrMast.BarCode) + "'," +
                "GROUPCODE=" + clsPrMast.GroupCode + "," +
                "CATGCODE=" + clsPrMast.CatgCode + "," +
                "BRAND=N'" + SqlEscape(clsPrMast.Brand) + "'," +
                "UNIT=" + clsPrMast.Unit + "," +
                "ALTUNIT=" + clsPrMast.AltUnit + "," +
                "UNITCONV=" + clsPrMast.UnitConv.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                "CONVTYPE=" + clsPrMast.ConvType + "," +
                "OPNSTOCK=" + clsPrMast.OpnStock.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                "OPNSTKVAL=" + clsPrMast.OpnStkval.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                "PURPRICETYPE=" + clsPrMast.PurPriceType + "," +
                "BASEPURPRICE=" + clsPrMast.BasePurPrice.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                "ALTPURPRICE=" + clsPrMast.AltPurPrice.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                "SALESPRICETYPE=" + clsPrMast.SalesPriceType + "," +
                "BASESALEPRICE=" + clsPrMast.BaseSalePrice.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                "BASEMINPRICE=" + clsPrMast.BaseMinPrice.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                "ALTSALEPRICE=" + clsPrMast.AltSalePrice.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                "ALTMINPRICE=" + clsPrMast.AltMinPrice.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                "COSTMETH=" + clsPrMast.CostMeth + "," +
                "TAXCODE=" + clsPrMast.TaxCatCode + "," +
                "SALEACCODE=" + clsPrMast.SaleAcCode + "," +
                "PURACCODE=" + clsPrMast.PurAcCode + "," +
                "MINQTY=" + clsPrMast.MinQty.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                "REORDQTY=" + clsPrMast.ReOrdQty.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                "MAXQTY=" + clsPrMast.MaxQty.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                "SERVFLG=" + servFlg + "," +
                "SUPCODE=" + clsPrMast.SupCode + "," +
                "BATCHFLG=" + batchFlg + "," +
                "MANDATEFLG=" + manDateFlg + "," +
                "EXPDATEFLG=" + expDateFlg + "," +
                "EXPDAYS=" + clsPrMast.ExpDays.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                "PRICECATFLG=" + priceCatFlg + "," +
                "ACTIVE=" + active + "," +
                "SECNAME=N'" + SqlEscape(clsPrMast.SecName) + "'," +
                "PRICECATCODE=" + clsPrMast.PriceCatCode + "," +
                "STOCK=" + clsPrMast.Stock.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                "OPNSTKRATE=" + clsPrMast.OpnStkRate.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                "CURCONVRATE=" + clsPrMast.CurConvRate.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                "CURCODE=" + clsPrMast.CurCode + "," +
                "IMGPATH=N'" + SqlEscape(clsPrMast.ImgPath) + "'," +
                "POSFLG=" + posFlg + "," +
                "PRNOTES=N'" + SqlEscape(clsPrMast.Notes) + "'," +
                "BMPL=" + clsPrMast._BMPL.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                "BMPQ=" + clsPrMast._BMPQ.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                "BASEMRP=" + clsPrMast.BaseMrp.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                "ALTMRP=" + clsPrMast.AltMrp.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                "SLNOTRACK=" + slNoTrack + "," +
                "BMEQN=N'" + SqlEscape(clsPrMast._BMEQN) + "'" +
                " WHERE PRCODE=" + clsPrMast.PrCode +
                " AND RowVersion=@OriginalRowVersion";

            var parameter = new Microsoft.Data.SqlClient.SqlParameter(
                "@OriginalRowVersion",
                System.Data.SqlDbType.Binary,
                8)
            {
                Value = originalRowVersion
            };

            return await db.ExecuteWithParams(qry, parameter);
        }

//        public async Task<(DataTable Data, int TotalRecords)> ListPaged(
//           int page,
//           int pageSize,
//           string? search,
//           string? sortBy,
//           string? sortDirection)
//        {
//            page = Math.Max(1, page);
//            pageSize = Math.Clamp(pageSize, 1, 100);

//            string fromSql = $@"
//        FROM {Functions.VRSYS_ALLOWBR_PRMAST(DefBranch: true)}PRMAST
//        INNER JOIN PRUNITMAS PU1
//            ON PRMAST.UNIT = PU1.PRUNITID
//        LEFT JOIN PRUNITMAS PU2
//            ON PRMAST.ALTUNIT = PU2.PRUNITID
//        INNER JOIN TAXMAS TX
//            ON PRMAST.TAXCODE = TX.CODE
//        WHERE PRMAST.ACTIVE = 1
//        {Functions.VRSYS_ALLOWBR(true, "PRMAST", true)}";

//                    var parameters = new List<SqlParameter>();
//                    string searchSql = "";

//                    if (!string.IsNullOrWhiteSpace(search))
//                    {
//                        searchSql = @"
//        AND (
//            CONVERT(NVARCHAR(100), PRMAST.PRCODE) LIKE @Search OR
//            COALESCE(CONVERT(NVARCHAR(4000), PRMAST.PRNAME), '') LIKE @Search OR
//            COALESCE(CONVERT(NVARCHAR(4000), PRMAST.ALIAS), '') LIKE @Search OR
//            COALESCE(CONVERT(NVARCHAR(4000), PRMAST.BARCODE), '') LIKE @Search OR
//            CONVERT(NVARCHAR(100), PRMAST.UNIT) LIKE @Search OR
//            COALESCE(CONVERT(NVARCHAR(4000), PU1.PRUNITNAME), '') LIKE @Search OR
//            CONVERT(NVARCHAR(100), ISNULL(PRMAST.ALTUNIT, 0)) LIKE @Search OR
//            COALESCE(CONVERT(NVARCHAR(4000), PU2.PRUNITNAME), '') LIKE @Search OR
//            COALESCE(CONVERT(NVARCHAR(100), PRMAST.CONVTYPE), '') LIKE @Search OR
//            CONVERT(NVARCHAR(100), PRMAST.UNITCONV) LIKE @Search OR
//            COALESCE(CONVERT(NVARCHAR(100), PRMAST.SALESPRICETYPE), '') LIKE @Search OR
//            COALESCE(CONVERT(NVARCHAR(100), PRMAST.PURPRICETYPE), '') LIKE @Search OR
//            CONVERT(NVARCHAR(100), PRMAST.BASESALEPRICE) LIKE @Search OR
//            CONVERT(NVARCHAR(100), PRMAST.ALTSALEPRICE) LIKE @Search OR
//            CONVERT(NVARCHAR(100), PRMAST.BASEPURPRICE) LIKE @Search OR
//            CONVERT(NVARCHAR(100), PRMAST.ALTPURPRICE) LIKE @Search OR
//            CONVERT(NVARCHAR(100), PRMAST.BASEMRP) LIKE @Search OR
//            CONVERT(NVARCHAR(100), PRMAST.ALTMRP) LIKE @Search OR
//            CONVERT(NVARCHAR(100), PRMAST.TAXCODE) LIKE @Search OR
//            COALESCE(CONVERT(NVARCHAR(4000), TX.TAXNAME), '') LIKE @Search OR
//            CONVERT(NVARCHAR(100), TX.TAXPERC) LIKE @Search OR
//            CONVERT(NVARCHAR(100), PRMAST.BATCHFLG) LIKE @Search OR
//            CONVERT(NVARCHAR(100), PRMAST.SLNOTRACK) LIKE @Search
//        )";

//                parameters.Add(new SqlParameter(
//                    "@Search",
//                    System.Data.SqlDbType.NVarChar,
//                    4000)
//                {
//                    Value = "%" + search.Trim() + "%"
//                });
//            }

//            var sortColumns = new Dictionary<string, string>(
//                StringComparer.OrdinalIgnoreCase)
//            {
//                ["PRCODE"] = "PRMAST.PRCODE",
//                ["PRNAME"] = "PRMAST.PRNAME",
//                ["ALIAS"] = "PRMAST.ALIAS",
//                ["BARCODE"] = "PRMAST.BARCODE",
//                ["MAINUNITCODE"] = "PRMAST.UNIT",
//                ["MAINUNITNAME"] = "PU1.PRUNITNAME",
//                ["ALTUNITCODE"] = "ISNULL(PRMAST.ALTUNIT, 0)",
//                ["ALTUNITNAME"] = "PU2.PRUNITNAME",
//                ["CONVTYPE"] = "PRMAST.CONVTYPE",
//                ["CONVFACT"] = "PRMAST.UNITCONV",
//                ["SALESPRICETYPE"] = "PRMAST.SALESPRICETYPE",
//                ["PURPRICETYPE"] = "PRMAST.PURPRICETYPE",
//                ["BASESALEPRICE"] = "PRMAST.BASESALEPRICE",
//                ["ALTSALEPRICE"] = "PRMAST.ALTSALEPRICE",
//                ["BASEPURPRICE"] = "PRMAST.BASEPURPRICE",
//                ["ALTPURPRICE"] = "PRMAST.ALTPURPRICE",
//                ["BASEMRP"] = "PRMAST.BASEMRP",
//                ["ALTMRP"] = "PRMAST.ALTMRP",
//                ["TAXCODE"] = "PRMAST.TAXCODE",
//                ["TAXNAME"] = "TX.TAXNAME",
//                ["TAXPERC"] = "TX.TAXPERC",
//                ["BATCHFLG"] = "PRMAST.BATCHFLG",
//                ["SLNOTRACK"] = "PRMAST.SLNOTRACK"
//            };

//            string orderBy = "PRMAST.PRNAME ASC";

//            if (!string.IsNullOrWhiteSpace(sortBy) &&
//                sortColumns.TryGetValue(sortBy.Trim(), out string? sortExpression))
//            {
//                string direction = string.Equals(
//                    sortDirection?.Trim(),
//                    "ASC",
//                    StringComparison.OrdinalIgnoreCase)
//                    ? "ASC"
//                    : "DESC";

//                orderBy = $"{sortExpression} {direction}";
//            }

//            string countSql = $@"
//                  SELECT COUNT(1)
//                     {fromSql}
//                     {searchSql};";

//            int totalRecords = await db.ExecuteScalarIntAsync(
//                countSql,
//                parameters);

//            string dataSql = $@"
//                 SELECT
//    PRMAST.PRCODE,
//    PRMAST.PRNAME,
//    PRMAST.ALIAS,
//    PRMAST.BARCODE,
//    PRMAST.UNIT AS MAINUNITCODE,
//    PU1.PRUNITNAME AS MAINUNITNAME,
//    ISNULL(PRMAST.ALTUNIT, 0) AS ALTUNITCODE,
//    ISNULL(PU2.PRUNITNAME, '') AS ALTUNITNAME,
//    PRMAST.CONVTYPE,
//    PRMAST.UNITCONV AS CONVFACT,
//    PRMAST.SALESPRICETYPE,
//    PRMAST.PURPRICETYPE,
//    PRMAST.BASESALEPRICE,
//    PRMAST.ALTSALEPRICE,
//    PRMAST.BASEPURPRICE,
//    PRMAST.ALTPURPRICE,
//    PRMAST.BASEMRP,
//    PRMAST.ALTMRP,
//    PRMAST.TAXCODE,
//    TX.TAXNAME,
//    TX.TAXPERC,
//    PRMAST.BATCHFLG,
//    PRMAST.SLNOTRACK
//{fromSql}
//{searchSql}
//ORDER BY {orderBy}, PRMAST.PRCODE ASC
//OFFSET @Offset ROWS
//FETCH NEXT @PageSize ROWS ONLY;";

//            var dataParameters = parameters
//                .Select(parameter =>
//                    new SqlParameter(parameter.ParameterName, parameter.SqlDbType)
//                    {
//                        Value = parameter.Value ?? DBNull.Value,
//                        Size = parameter.Size,
//                        Precision = parameter.Precision,
//                        Scale = parameter.Scale
//                    })
//                .ToList();

//            dataParameters.Add(new SqlParameter(
//                "@Offset",
//                System.Data.SqlDbType.Int)
//            {
//                Value = (page - 1) * pageSize
//            });

//            dataParameters.Add(new SqlParameter(
//                "@PageSize",
//                System.Data.SqlDbType.Int)
//            {
//                Value = pageSize
//            });

//            DataTable data = await db.GetDatatable(
//                dataSql,
//                dataParameters);

//            return (data, totalRecords);
//        }

    public async Task<(DataTable Data, int TotalRecords)> ListPaged(
    int page,
    int pageSize,
    string? search
    //string? sortBy,
    //string? sortDirection
        )
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            // ------------------------------------------------------------
            // 1. Build the product query.
            // ------------------------------------------------------------
            string fromSql = $@"
        FROM {Functions.VRSYS_ALLOWBR_PRMAST(DefBranch: true)}PRMAST
        INNER JOIN PRUNITMAS PU1
            ON PRMAST.UNIT = PU1.PRUNITID
        LEFT JOIN PRUNITMAS PU2
            ON PRMAST.ALTUNIT = PU2.PRUNITID
        INNER JOIN TAXMAS TX
            ON PRMAST.TAXCODE = TX.CODE
        WHERE PRMAST.ACTIVE = 1
         {Functions.VRSYS_ALLOWBR(true, "PRMAST", true)}
            ";
            //{Functions.VRSYS_ALLOWBR(true, ""PRMAST"", true)}";


            var parameters = new List<SqlParameter>();
            string searchSql = string.Empty;

            // ------------------------------------------------------------
            // 2. Search filter.
            // ------------------------------------------------------------
            if (!string.IsNullOrWhiteSpace(search))
            {
                searchSql = @"
            AND
            (
                CONVERT(NVARCHAR(100), PRMAST.PRCODE) LIKE @Search
                OR COALESCE(CONVERT(NVARCHAR(4000), PRMAST.PRNAME), '') LIKE @Search
                OR COALESCE(CONVERT(NVARCHAR(4000), PRMAST.ALIAS), '') LIKE @Search
                OR COALESCE(CONVERT(NVARCHAR(4000), PRMAST.BARCODE), '') LIKE @Search
                OR CONVERT(NVARCHAR(100), PRMAST.UNIT) LIKE @Search
                OR COALESCE(CONVERT(NVARCHAR(4000), PU1.PRUNITNAME), '') LIKE @Search
                OR CONVERT(NVARCHAR(100), ISNULL(PRMAST.ALTUNIT, 0)) LIKE @Search
                OR COALESCE(CONVERT(NVARCHAR(4000), PU2.PRUNITNAME), '') LIKE @Search
                OR COALESCE(CONVERT(NVARCHAR(100), PRMAST.CONVTYPE), '') LIKE @Search
                OR CONVERT(NVARCHAR(100), PRMAST.UNITCONV) LIKE @Search
                OR COALESCE(CONVERT(NVARCHAR(100), PRMAST.SALESPRICETYPE), '') LIKE @Search
                OR COALESCE(CONVERT(NVARCHAR(100), PRMAST.PURPRICETYPE), '') LIKE @Search
                OR CONVERT(NVARCHAR(100), PRMAST.BASESALEPRICE) LIKE @Search
                OR CONVERT(NVARCHAR(100), PRMAST.ALTSALEPRICE) LIKE @Search
                OR CONVERT(NVARCHAR(100), PRMAST.BASEPURPRICE) LIKE @Search
                OR CONVERT(NVARCHAR(100), PRMAST.ALTPURPRICE) LIKE @Search
                OR CONVERT(NVARCHAR(100), PRMAST.BASEMRP) LIKE @Search
                OR CONVERT(NVARCHAR(100), PRMAST.ALTMRP) LIKE @Search
                OR CONVERT(NVARCHAR(100), PRMAST.TAXCODE) LIKE @Search
                OR COALESCE(CONVERT(NVARCHAR(4000), TX.TAXNAME), '') LIKE @Search
                OR CONVERT(NVARCHAR(100), TX.TAXPERC) LIKE @Search
                OR CONVERT(NVARCHAR(100), PRMAST.BATCHFLG) LIKE @Search
                OR CONVERT(NVARCHAR(100), PRMAST.SLNOTRACK) LIKE @Search
            )";

                parameters.Add(new SqlParameter(
                    "@Search",
                    SqlDbType.NVarChar,
                    4000)
                {
                    Value = "%" + search.Trim() + "%"
                });
            }

            // ------------------------------------------------------------
            // 3. Whitelist sort columns.
            // Never insert a client-supplied SQL expression directly.
            // ------------------------------------------------------------
            var sortColumns = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["PRCODE"] = "PRMAST.PRCODE",
                ["PRNAME"] = "PRMAST.PRNAME",
                ["ALIAS"] = "PRMAST.ALIAS",
                ["BARCODE"] = "PRMAST.BARCODE",
                ["MAINUNITCODE"] = "PRMAST.UNIT",
                ["MAINUNITNAME"] = "PU1.PRUNITNAME",
                ["ALTUNITCODE"] = "ISNULL(PRMAST.ALTUNIT, 0)",
                ["ALTUNITNAME"] = "PU2.PRUNITNAME",
                ["CONVTYPE"] = "PRMAST.CONVTYPE",
                ["CONVFACT"] = "PRMAST.UNITCONV",
                ["SALESPRICETYPE"] = "PRMAST.SALESPRICETYPE",
                ["PURPRICETYPE"] = "PRMAST.PURPRICETYPE",
                ["BASESALEPRICE"] = "PRMAST.BASESALEPRICE",
                ["ALTSALEPRICE"] = "PRMAST.ALTSALEPRICE",
                ["BASEPURPRICE"] = "PRMAST.BASEPURPRICE",
                ["ALTPURPRICE"] = "PRMAST.ALTPURPRICE",
                ["BASEMRP"] = "PRMAST.BASEMRP",
                ["ALTMRP"] = "PRMAST.ALTMRP",
                ["TAXCODE"] = "PRMAST.TAXCODE",
                ["TAXNAME"] = "TX.TAXNAME",
                ["TAXPERC"] = "TX.TAXPERC",
                ["BATCHFLG"] = "PRMAST.BATCHFLG",
                ["SLNOTRACK"] = "PRMAST.SLNOTRACK"
            };

            string orderBy = "PRMAST.PRNAME ASC";

            //if (!string.IsNullOrWhiteSpace(sortBy) &&
                //sortColumns.TryGetValue(sortBy.Trim(), out string? sortExpression))
            //{
            //    string direction = string.Equals(
            //        sortDirection?.Trim(),
            //        "ASC",
            //        StringComparison.OrdinalIgnoreCase)
            //        ? "ASC"
            //        : "DESC";

            //    orderBy = $"{sortExpression} {direction}";
            //}

            // ------------------------------------------------------------
            // 4. Count query.
            // ------------------------------------------------------------
            string countSql = $@"
        SELECT COUNT(1)
        {fromSql}
        {searchSql};";

            // ------------------------------------------------------------
            // 5. Paginated data query.
            // ------------------------------------------------------------
            string dataSql = $@"
        SELECT
            PRMAST.PRCODE,
            PRMAST.PRNAME,
            PRMAST.ALIAS,
            PRMAST.BARCODE,
            PRMAST.UNIT AS MAINUNITCODE,
            PU1.PRUNITNAME AS MAINUNITNAME,
            ISNULL(PRMAST.ALTUNIT, 0) AS ALTUNITCODE,
            ISNULL(PU2.PRUNITNAME, '') AS ALTUNITNAME,
            PRMAST.CONVTYPE,
            PRMAST.UNITCONV AS CONVFACT,
            PRMAST.SALESPRICETYPE,
            PRMAST.PURPRICETYPE,
            PRMAST.BASESALEPRICE,
            PRMAST.ALTSALEPRICE,
            PRMAST.BASEPURPRICE,
            PRMAST.ALTPURPRICE,
            PRMAST.BASEMRP,
            PRMAST.ALTMRP,
            PRMAST.TAXCODE,
            TX.TAXNAME,
            TX.TAXPERC,
            PRMAST.BATCHFLG,
            PRMAST.SLNOTRACK
        {fromSql}
        {searchSql}
        ORDER BY {orderBy}, PRMAST.PRCODE ASC
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY;";

            // ------------------------------------------------------------
            // 6. Execute both queries using one explicitly opened connection.
            // ------------------------------------------------------------
            var data = new DataTable();
            int totalRecords;

            await using (var connection = new SqlConnection(db.ConnectionString))
            {
                await connection.OpenAsync();

                // Count.
                await using (var countCommand = new SqlCommand(
                    countSql, connection))
                {
                    foreach (var parameter in parameters)
                    {
                        countCommand.Parameters.Add(
                            new SqlParameter(
                                parameter.ParameterName,
                                parameter.SqlDbType)
                            {
                                Value = parameter.Value ?? DBNull.Value,
                                Size = parameter.Size,
                                Precision = parameter.Precision,
                                Scale = parameter.Scale
                            });
                    }

                    object? countResult =
                        await countCommand.ExecuteScalarAsync();

                    totalRecords =
                        countResult == null || countResult == DBNull.Value
                            ? 0
                            : Convert.ToInt32(countResult);
                }

                // Do not run the page query when there are no matching records.
                if (totalRecords > 0)
                {
                    await using var dataCommand = new SqlCommand(
                        dataSql, connection);

                    foreach (var parameter in parameters)
                    {
                        dataCommand.Parameters.Add(
                            new SqlParameter(
                                parameter.ParameterName,
                                parameter.SqlDbType)
                            {
                                Value = parameter.Value ?? DBNull.Value,
                                Size = parameter.Size,
                                Precision = parameter.Precision,
                                Scale = parameter.Scale
                            });
                    }

                    dataCommand.Parameters.Add(
                        "@Offset", SqlDbType.Int).Value =
                        (page - 1) * pageSize;

                    dataCommand.Parameters.Add(
                        "@PageSize", SqlDbType.Int).Value = pageSize;

                    await using var reader =
                        await dataCommand.ExecuteReaderAsync();

                    data.Load(reader);
                }
            }

            return (data, totalRecords);
        }


        public async Task<int> UpdateForDel(ClsPrMast clsPrMast)
        {
            int delFlg = BitValue(clsPrMast.DelFlg);
            int active = BitValue(clsPrMast.Active);

            qry =
                "UPDATE PRMAST SET " +
                "DELFLG=" + delFlg + "," +
                "ACTIVE=" + active +
                " WHERE PRCODE=" + clsPrMast.PrCode;

            return await db.Execute(qry);
        }


        // ============================================================
        // INSERT PRODUCT
        // ============================================================

        public async Task<int> Insert(ClsPrMast clsPrMast)
        {
            int servFlg = BitValue(clsPrMast.ServFlg);
            int batchFlg = BitValue(clsPrMast.BatchFlg);
            int manDateFlg = BitValue(clsPrMast.ManDateFlg);
            int expDateFlg = BitValue(clsPrMast.ExpDateFlg);
            int priceCatFlg = BitValue(clsPrMast.PriceCatFlg);
            int active = BitValue(clsPrMast.Active);
            int delFlg = BitValue(clsPrMast.DelFlg);
            int posFlg = BitValue(clsPrMast.PosFlg);
            int slNoTrack = BitValue(clsPrMast._SLNOTRACK);

            qry = @"
INSERT INTO PRMAST
(
    PRNAME,ALIAS, PRINTNAME, DESCRIPTION,UPCCODE,SKUCODE, BARCODE, GROUPCODE, CATGCODE,BRAND, UNIT, ALTUNIT,UNITCONV,CONVTYPE,OPNSTOCK,
    OPNSTKVAL,
    PURPRICETYPE,
    BASEPURPRICE,
    ALTPURPRICE,
    SALESPRICETYPE,
    BASESALEPRICE,
    BASEMINPRICE,
    ALTSALEPRICE,
    ALTMINPRICE,
    COSTMETH,
    TAXCODE,
    SALEACCODE,
    PURACCODE,
    MINQTY,
    REORDQTY,
    MAXQTY,
    SERVFLG,
    SUPCODE,
    BATCHFLG,
    MANDATEFLG,
    EXPDATEFLG,
    EXPDAYS,
    PRICECATFLG,
    ACTIVE,
    STOCK,
    LOGCD,
    UTIME,
    SECNAME,
    PRICECATCODE,
    OPNSTKRATE,
    CURCONVRATE,
    CURCODE,
    IMGPATH,
    DELFLG,
    POSFLG,
    PRNOTES,
    BMPL,
    BMPQ,
    BMPW,
    BASEMRP,
    ALTMRP,
    SLNOTRACK,
    BMEQN,
    APRVLST,
    APRVLRBY,
    APRVLRDT
)
OUTPUT INSERTED.PRCODE
VALUES
(
    N'" + SqlEscape(clsPrMast.PrName) + @"',
    N'" + SqlEscape(clsPrMast.Alias) + @"',
    N'" + SqlEscape(clsPrMast.PrintName) + @"',
    N'" + SqlEscape(clsPrMast.Desc) + @"',
    N'" + SqlEscape(clsPrMast.UpcCode) + @"',
    N'" + SqlEscape(clsPrMast.SkuCode) + @"',
    N'" + SqlEscape(clsPrMast.BarCode) + @"',
    " + clsPrMast.GroupCode + @",
    " + clsPrMast.CatgCode + @",
    N'" + SqlEscape(clsPrMast.Brand) + @"',
    " + clsPrMast.Unit + @",
    " + clsPrMast.AltUnit + @",
    " + clsPrMast.UnitConv.ToString(System.Globalization.CultureInfo.InvariantCulture) + @",
    " + clsPrMast.ConvType + @",
    " + clsPrMast.OpnStock.ToString(System.Globalization.CultureInfo.InvariantCulture) + @",
    " + clsPrMast.OpnStkval.ToString(System.Globalization.CultureInfo.InvariantCulture) + @",
    " + clsPrMast.PurPriceType + @",
    " + clsPrMast.BasePurPrice.ToString(System.Globalization.CultureInfo.InvariantCulture) + @",
    " + clsPrMast.AltPurPrice.ToString(System.Globalization.CultureInfo.InvariantCulture) + @",
    " + clsPrMast.SalesPriceType + @",
    " + clsPrMast.BaseSalePrice.ToString(System.Globalization.CultureInfo.InvariantCulture) + @",
    " + clsPrMast.BaseMinPrice.ToString(System.Globalization.CultureInfo.InvariantCulture) + @",
    " + clsPrMast.AltSalePrice.ToString(System.Globalization.CultureInfo.InvariantCulture) + @",
    " + clsPrMast.AltMinPrice.ToString(System.Globalization.CultureInfo.InvariantCulture) + @",
    " + clsPrMast.CostMeth + @",
    " + clsPrMast.TaxCatCode + @",
    " + clsPrMast.SaleAcCode + @",
    " + clsPrMast.PurAcCode + @",
    " + clsPrMast.MinQty.ToString(System.Globalization.CultureInfo.InvariantCulture) + @",
    " + clsPrMast.ReOrdQty.ToString(System.Globalization.CultureInfo.InvariantCulture) + @",
    " + clsPrMast.MaxQty.ToString(System.Globalization.CultureInfo.InvariantCulture) + @",
    " + servFlg + @",
    " + clsPrMast.SupCode + @",
    " + batchFlg + @",
    " + manDateFlg + @",
    " + expDateFlg + @",
    " + clsPrMast.ExpDays.ToString(System.Globalization.CultureInfo.InvariantCulture) + @",
    " + priceCatFlg + @",
    " + active + @",
    " + clsPrMast.Stock.ToString(System.Globalization.CultureInfo.InvariantCulture) + @",
    " + Variables.UserId + @",
    GETDATE(),
    N'" + SqlEscape(clsPrMast.SecName) + @"',
    " + clsPrMast.PriceCatCode + @",
    " + clsPrMast.OpnStkRate.ToString(System.Globalization.CultureInfo.InvariantCulture) + @",
    " + clsPrMast.CurConvRate.ToString(System.Globalization.CultureInfo.InvariantCulture) + @",
    " + clsPrMast.CurCode + @",
    N'" + SqlEscape(clsPrMast.ImgPath) + @"',
    " + delFlg + @",
    " + posFlg + @",
    N'" + SqlEscape(clsPrMast.Notes) + @"',
    " + clsPrMast._BMPL.ToString(System.Globalization.CultureInfo.InvariantCulture) + @",
    " + clsPrMast._BMPQ.ToString(System.Globalization.CultureInfo.InvariantCulture) + @",
    " + clsPrMast._BMPW.ToString(System.Globalization.CultureInfo.InvariantCulture) + @",
    " + clsPrMast.BaseMrp.ToString(System.Globalization.CultureInfo.InvariantCulture) + @",
    " + clsPrMast.AltMrp.ToString(System.Globalization.CultureInfo.InvariantCulture) + @",
    " + slNoTrack + @",
    N'" + SqlEscape(clsPrMast._BMEQN) + @"',
    N'" + SqlEscape(clsPrMast.APRVLST) + @"',
    " + Variables.UserId + @",
    GETDATE()
)";

            return await db.InsertScalar(qry);
        }



        // ============================================================
        // CHECK OCCURRENCE
        // ============================================================

        public async Task<int> CheckOccurance(
            string FiledName,
            string Value)
        {
            Value = SqlEscape(Value);

            qry =
                "SELECT COUNT(*) " +
                "FROM PRMAST " +
                "WHERE " +
                FiledName +
                "=N'" +
                Value +
                "'";

            return await db.ReturnIntValue(qry);
        }
        // ============================================================
        // CHECK EXISTING NAME DURING UPDATE
        // ============================================================

        public async Task<int> CheckExistName(
            string FiledName,
            string Value,
            string FiledName1,
            int Value1)
        {
            Value = SqlEscape(Value);

            qry =
                "SELECT COUNT(*) " +
                "FROM PRMAST " +
                "WHERE " +
                FiledName +
                "=N'" +
                Value +
                "' " +
                "AND " +
                FiledName1 +
                "<>" +
                Value1;

            return await db.ReturnIntValue(qry);
        }

    }
}
