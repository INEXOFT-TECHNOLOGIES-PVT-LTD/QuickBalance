using QuickBalance.API.CLS;
using System;
using System.Data;
using System.Threading.Tasks;

namespace QuickBalance.API.DB
{
    public class OpnStkDB
    {
        private readonly DBConn db;

        public Variable Variables = new Variable();

        string qry = "";

        public OpnStkDB(DBConn dbConn)
        {
            db = dbConn;
        }

        // ============================================================
        // INSERT OPENING STOCK
        // ============================================================

        public async Task<int> Insert(ClsOpnStk clsOpnStk)
        {
            if (Variables.BranchYN && clsOpnStk.BrCd == 0)
            {
                clsOpnStk.BrCd = Variables.VRSYS_DEFBRANCH;
            }

            qry = @"INSERT INTO OPNSTK
          (
        PRCODE, GDWCODE, UOM, QTY, RATE, STVALUE,
        CONVQTY, CONVVALUE, UTIME, LOGCD, BATCH, MRP,
        STATUS, COSTCCD, SALRATE, EXPIRY, MFGDATE,
        EXPCHECK, BRCD, SNTSLNO
    )
    OUTPUT INSERTED.ID
    VALUES
    (
        '" + clsOpnStk.PrCode + @"',
        '" + clsOpnStk.GdwCode + @"',
        '" + clsOpnStk.Uom + @"',
        '" + clsOpnStk.Qty + @"',
        '" + clsOpnStk.Rate + @"',
        '" + clsOpnStk.StVal + @"',
        '" + clsOpnStk.ConvQty + @"',
        '" + clsOpnStk.ConvValue + @"',
        GETDATE(),
        '" + Variables.UserId + @"',
        N'" + clsOpnStk.Batch + @"',
        '" + clsOpnStk.Mrp + @"',
        N'" + clsOpnStk.Status + @"',
        '" + clsOpnStk.CostcCd + @"',
        '" + clsOpnStk.SalRate + @"',
        GETDATE(),
        GETDATE(),
        '" + clsOpnStk.ExpChk + @"',
        '" + clsOpnStk.BrCd + @"',
        '" + clsOpnStk.SNTSLNO + @"'
    )";

            return await db.InsertScalar(qry);
        }
        // ============================================================
        // UPDATE OPENING STOCK BY PRODUCT CODE
        // ============================================================

        public async Task<int> UpdateByPrCode(ClsOpnStk clsOpnStk)
        {
            qry = @"UPDATE OPNSTK
                    SET
                        QTY = '" + clsOpnStk.Qty + @"',
                        RATE = '" + clsOpnStk.Rate + @"',
                        STVALUE = '" + clsOpnStk.StVal + @"',
                        CONVQTY = '" + clsOpnStk.ConvQty + @"',
                        CONVVALUE = '" + clsOpnStk.ConvValue + @"'
                    WHERE PRCODE = '" + clsOpnStk.PrCode + @"'"
                    + (Variables.BranchYN
                        ? " AND BRCD=" + clsOpnStk.BrCd
                        : "");

            return await db.Execute(qry);
        }

        // ============================================================
        // GET TOTAL OPENING STOCK VALUE
        // ============================================================

        public async Task<decimal> GetTotalValue(int branchCode)
        {
            qry = @"SELECT SUM(
                        IIF(STVALUE IS NULL, 0, STVALUE)
                    ) AS OP
                    FROM OPNSTK"
                    + (Variables.BranchYN
                        ? " WHERE BRCD=" + branchCode
                        : "");

            DataTable dt = await db.GetDatatable(qry);

            if (dt.Rows.Count == 0 ||
                dt.Rows[0]["OP"] == DBNull.Value)
            {
                return 0;
            }

            return Convert.ToDecimal(dt.Rows[0]["OP"]);
        }

        // ============================================================
        // SEARCH BY PRODUCT CODE
        // ============================================================
       public async Task<DataTable> SearchByPrCode(ClsOpnStk clsOpnStk)
        {
            string sql = @"
        SELECT *
        FROM OPNSTK
        WHERE PRCODE = '" + clsOpnStk.PrCode + "'"
                + (Variables.BranchYN
                    ? " AND BRCD = " + clsOpnStk.BrCd
                    : "");

            return await db.GetDatatable(sql);
        }

        // ============================================================
        // SEARCH BY PRODUCT CODE AND GODOWN
        // ============================================================

        public async Task<DataTable> SearchByPrCodeAndGodown(
            ClsOpnStk clsOpnStk)
        {
            qry = @"SELECT *
                    FROM OPNSTK
                    WHERE PRCODE='" + clsOpnStk.PrCode + @"'
                    AND GDWCODE='" + clsOpnStk.GdwCode + @"'";

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // DELETE BY PRODUCT CODE
        // ============================================================

        public async Task<int> DeleteByPrCode(ClsOpnStk clsOpnStk)
        {
            qry = @"DELETE FROM OPNSTK
                    WHERE PRCODE='" + clsOpnStk.PrCode + @"'"
                    + (Variables.BranchYN
                        ? " AND BRCD=" + clsOpnStk.BrCd
                        : "");

            return await db.Execute(qry);
        }

        // ============================================================
        // DELETE BY PRODUCT CODE AND GODOWN
        // ============================================================

        public async Task<int> DeleteByPrCodeAndGodown(
            ClsOpnStk clsOpnStk)
        {
            qry = @"DELETE FROM OPNSTK
                    WHERE PRCODE='" + clsOpnStk.PrCode + @"'
                    AND GDWCODE='" + clsOpnStk.GdwCode + @"'"
                    + (Variables.BranchYN
                        ? " AND BRCD=" + clsOpnStk.BrCd
                        : "");

            return await db.Execute(qry);
        }

        // ============================================================
        // SELECT OPENING STOCK
        // ============================================================

        public async Task<DataTable> Select(int branchCode)
        {
            qry = @"SELECT
                        OPNSTK.PRCODE,
                        OPNSTK.GDWCODE,
                        OPNSTK.UOM,
                        OPNSTK.QTY,
                        OPNSTK.RATE,
                        OPNSTK.STVALUE,
                        OPNSTK.CONVQTY,
                        OPNSTK.CONVVALUE,
                        OPNSTK.BATCH,
                        OPNSTK.SALRATE,
                        OPNSTK.MRP,
                        OPNSTK.STATUS,
                        OPNSTK.COSTCCD,
                        OPNSTK.SNTSLNO,
                        PRMAST.PRNAME,
                        PRMAST.BATCHFLG,
                        PRMAST.SLNOTRACK
                    FROM OPNSTK
                    INNER JOIN PRMAST
                        ON PRMAST.PRCODE = OPNSTK.PRCODE"
                    + (Variables.BranchYN
                        ? " WHERE OPNSTK.BRCD=" + branchCode
                        : "")
                    + " ORDER BY PRNAME";

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // GODOWNS BY PRODUCT CODE
        // ============================================================

        public async Task<DataTable> GodownsByPrcode(
            ClsOpnStk clsOpnStk)
        {
            qry = @"SELECT DISTINCT GDWCODE
                    FROM OPNSTK
                    WHERE PRCODE='" + clsOpnStk.PrCode + @"'";

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // SUMMARY
        // ============================================================

        public async Task<DataTable> Summary(int branchCode)
        {
            qry = @"SELECT
                        DISTINCT PRCODE,
                        SUM(CONVQTY) AS CONVQTY,
                        SUM(CONVVALUE) AS CONVVALUE
                    FROM OPNSTK"
                    + (Variables.BranchYN
                        ? " WHERE BRCD=" + branchCode
                        : "")
                    + " GROUP BY PRCODE";

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // GET ALL
        // ============================================================

        public async Task<DataTable> GetAll()
        {
            qry = @"SELECT *
                    FROM OPNSTK"
                    + (Variables.BranchYN
                        ? " WHERE BRCD=" + Variables.VRSYS_DEFBRANCH
                        : "");

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // GET ALL BY GODOWN
        // ============================================================

        public async Task<DataTable> GetAllByGdwn(string gdwns)
        {
            qry = @"SELECT *
                    FROM OPNSTK
                    WHERE GDWCODE IN (" + gdwns + @")";

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // SELECT BY ITEM
        // ============================================================

        public async Task<DataTable> SelectByItem(
            int itemCode,
            int branchCode)
        {
            qry = @"SELECT PRCODE,
                        GDWCODE,
                        UOM,
                        QTY,
                        RATE,
                        STVALUE,
                        CONVQTY,
                        CONVVALUE,
                        BATCH,
                        SALRATE,
                        MRP,
                        STATUS,
                        COSTCCD,
                        SNTSLNO
                    FROM OPNSTK
                    WHERE PRCODE=" + itemCode
                    + (Variables.BranchYN
                        ? " AND BRCD=" + branchCode
                        : "");

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // TRUNCATE BY ITEM
        // ============================================================

        public async Task<int> TruncateByItem(
            int itemCode,
            int branchCode)
        {
            qry = @"DELETE FROM OPNSTK
                    WHERE PRCODE=" + itemCode
                    + (Variables.BranchYN
                        ? " AND BRCD=" + branchCode
                        : "");

            return await db.Execute(qry);
        }

        // ============================================================
        // SUMMARY BY ITEM
        // ============================================================

        public async Task<DataTable> SummaryByItem(
            int itemCode,
            int branchCode)
        {
            qry = @"SELECT PRCODE,SUM(CONVQTY) AS CONVQTY, SUM(CONVVALUE) AS CONVVALUE FROM OPNSTK
  
                    WHERE PRCODE=" + itemCode
                    + (Variables.BranchYN
                        ? " AND BRCD=" + branchCode
                        : "")
                    + " GROUP BY PRCODE";

            return await db.GetDatatable(qry);
        }
    }
}