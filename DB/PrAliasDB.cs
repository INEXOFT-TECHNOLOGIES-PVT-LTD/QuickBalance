using QuickBalance.API.CLS;
using System.Data;

namespace QuickBalance.API.DB
{
    public class PrAliasDB
    {
        private readonly DBConn db;

        public Variable Variables = new Variable();

        string qry = "";

        public PrAliasDB(DBConn dbConn)
        {
            db = dbConn;
        }

        // ============================================================
        // INSERT ALIAS
        // ============================================================
        public async Task<int> Insert(ClsPrAlias clsPrAlias)
        {
            qry = "INSERT INTO PRALIAS " +
                  "(PRCODE,ALCODE,ALNAME,QTY,UNITCODE,SALESPRICE,PURPRICE,LOGCD,UTIME,ACTIVE,ALTYPE,INVUNITCODE) " +
                  "OUTPUT INSERTED.CODE " +
                  "VALUES " +
                  "('" + clsPrAlias.PrCode + "'," +
                  "'" + clsPrAlias.AlCode + "'," +
                  "N'" + clsPrAlias.AlName + "'," +
                  "'" + clsPrAlias.Qty + "'," +
                  "'" + clsPrAlias.UnitCode + "'," +
                  "'" + clsPrAlias.SalesPrice + "'," +
                  "'" + clsPrAlias.PurPrice + "'," +
                  "'" + Variables.UserId + "'," +
                  "'" + DateTime.Now.ToString(Variables.format) + "'," +
                  "'" + clsPrAlias.Active + "'," +
                  "'" + clsPrAlias.AlType + "'," +
                  "'" + clsPrAlias.InvUnitCode + "')";

            return await db.InsertScalar(qry);
        }


        // ============================================================
        // UPDATE ALIAS
        // ============================================================
        public async Task<int> Update(ClsPrAlias clsPrAlias)
        {
            qry = "UPDATE PRALIAS SET " +
                  "PRCODE='" + clsPrAlias.PrCode + "'," +
                  "ALCODE='" + clsPrAlias.AlCode + "'," +
                  "ALNAME=N'" + clsPrAlias.AlName + "'," +
                  "QTY='" + clsPrAlias.Qty + "'," +
                  "UNITCODE='" + clsPrAlias.UnitCode + "'," +
                  "SALESPRICE='" + clsPrAlias.SalesPrice + "'," +
                  "PURPRICE='" + clsPrAlias.PurPrice + "'," +
                  "LOGCD='" + Variables.UserId + "'," +
                  "UTIME='" + DateTime.Now.ToString(Variables.format) + "'," +
                  "ACTIVE='" + clsPrAlias.Active + "' " +
                  "WHERE CODE='" + clsPrAlias.Code + "'";

            return await db.Execute(qry);
        }


        // ============================================================
        // DELETE ALIASES BY PRODUCT CODE
        // ============================================================
        public async Task<int> DeleteByPrCode(ClsPrAlias clsPrAlias)
        {
            qry = "DELETE FROM PRALIAS " +
                  "WHERE PRCODE='" + clsPrAlias.PrCode + "'";

            return await db.Execute(qry);
        }


        // ============================================================
        // SEARCH ALIASES BY PRODUCT CODE
        // ============================================================
        public async Task<DataTable> SearchByPrCode(ClsPrAlias clsPrAlias)
        {
            qry = "SELECT PRALIAS.*,PRUNITMAS.PRUNITNAME " +
                  "FROM PRALIAS " +
                  "LEFT JOIN PRUNITMAS ON PRALIAS.UNITCODE=PRUNITMAS.PRUNITID " +
                  "WHERE PRCODE='" + clsPrAlias.PrCode + "'";

            return await db.GetDatatable(qry);
        }


        // ============================================================
        // ALIAS DETAILS BY PRODUCT CODE
        // ============================================================
        public async Task<DataTable> DetailsByPrCode(ClsPrAlias clsPrAlias)
        {
            qry = @"SELECT 
                    PRALIAS.ALNAME AS AlName,
                    PRALIAS.QTY AS Qty,
                    PRUNITMAS.PRUNITNAME AS UOM,
                    PRALIAS.SALESPRICE AS SalesPrice,
                    PRALIAS.PURPRICE AS PurPrice,
                    PRALIAS.CODE AS Code,
                    PRALIAS.UNITCODE AS UnitId
                FROM PRALIAS
                LEFT JOIN PRUNITMAS
                    ON PRALIAS.UNITCODE = PRUNITMAS.PRUNITID
                WHERE PRALIAS.PRCODE='" + clsPrAlias.PrCode + "'";

            return await db.GetDatatable(qry);
        }


        // ============================================================
        // CHECK OCCURRENCE
        // ============================================================
        public async Task<int> CheckOccurance(
            string field,
            string value)
        {
            qry = "SELECT COUNT(*) " +
                  "FROM PRALIAS " +
                  "WHERE " + field + "='" + value + "'";

            DataTable dt = await db.GetDatatable(qry);

            if (dt.Rows.Count > 0)
            {
                return Convert.ToInt32(dt.Rows[0][0]);
            }

            return 0;
        }


        // ============================================================
        // CHECK OCCURRENCE FOR UPDATE
        // ============================================================
        public async Task<int> CheckOccuranceForUpdate(
            string field,
            string value,
            ClsPrAlias clsPrAlias)
        {
            qry = "SELECT COUNT(*) " +
                  "FROM PRALIAS " +
                  "WHERE " + field + "='" + value + "'" +
                  " AND CODE<>'" + clsPrAlias.Code + "'";

            DataTable dt = await db.GetDatatable(qry);

            if (dt.Rows.Count > 0)
            {
                return Convert.ToInt32(dt.Rows[0][0]);
            }

            return 0;
        }


        // ============================================================
        // SEARCH BY ALIAS CODE
        // ============================================================
        public async Task<DataTable> SearchByAlCode(
            ClsPrAlias clsPrAlias)
        {
            qry = @"SELECT 
                    PRALIAS.CODE,
                    PRALIAS.ALCODE,
                    PRALIAS.ALNAME,
                    PRALIAS.PRCODE,
                    PRALIAS.UNITCODE,
                    PRALIAS.QTY,
                    PRALIAS.ALTYPE,
                    PRALIAS.INVUNITCODE,
                    PRMAST.PRNAME,
                    PRMAST.SECNAME,
                    PRMAST.STOCK AS STOCK,
                    PRMAST.UNIT,
                    PRMAST.ALTUNIT,
                    PRMAST.UNITCONV,
                    PRMAST.CONVTYPE,
                    PRALIAS.SALESPRICE,
                    PRALIAS.PURPRICE,
                    ISNULL(TAXMAS.TAXPERC,0) AS TAXPERC,
                    TAXMAS.CODE AS TAXCODE,
                    PRUNITMAS.PRUNITNAME AS UOM,
                    PRMAST.DESCRIPTION AS DESCRIPTION,
                    PRMAST.BASEPURPRICE,
                    PRMAST.ALTPURPRICE,
                    TAXMAS.VATCODE,
                    PRMAST.SALESPRICETYPE,
                    PRMAST.BMPL,
                    PRMAST.BMPQ,
                    PRMAST.BMPW,
                    PRMAST.BMEQN,
                    PRMAST.ALTSALEPRICE,
                    PRMAST.BASESALEPRICE
                FROM PRALIAS
                INNER JOIN PRMAST
                    ON PRALIAS.PRCODE = PRMAST.PRCODE
                LEFT OUTER JOIN TAXMAS
                    ON PRMAST.TAXCODE = TAXMAS.CODE
                LEFT OUTER JOIN PRUNITMAS
                    ON PRALIAS.UNITCODE = PRUNITMAS.PRUNITID
                WHERE PRALIAS.CODE='" + clsPrAlias.Code + "'";

            return await db.GetDatatable(qry);
        }


        // ============================================================
        // SEARCH BY ALIAS CODE OR NAME
        // ============================================================
        public async Task<DataTable> SearchByAlCodeOrName(
            ClsPrAlias clsPrAlias)
        {
            qry = @"SELECT 
                    PRALIAS.ALCODE,
                    PRALIAS.ALNAME,
                    PRALIAS.PRCODE,
                    PRALIAS.UNITCODE,
                    PRALIAS.QTY,
                    PRALIAS.ALTYPE,
                    PRALIAS.INVUNITCODE,
                    PRMAST.PRNAME,
                    PRMAST.SECNAME,
                    PRALIAS.SALESPRICE,
                    ISNULL(TAXMAS.TAXPERC,0) AS TAXPERC,
                    TAXMAS.CODE
                FROM PRALIAS
                INNER JOIN PRMAST
                    ON PRALIAS.PRCODE = PRMAST.PRCODE
                LEFT OUTER JOIN TAXMAS
                    ON PRMAST.TAXCODE = TAXMAS.CODE
                WHERE
                    PRALIAS.ALCODE='" + clsPrAlias.AlCode + "'" +
                " OR PRALIAS.ALNAME=N'" + clsPrAlias.AlName + "'";

            return await db.GetDatatable(qry);
        }


        // ============================================================
        // SEARCH BY PRODUCT CODE AND ALIAS NAME
        // ============================================================
        public async Task<DataTable> SearchByPrCodeAndName(
            ClsPrAlias clsPrAlias)
        {
            qry = @"SELECT 
                    PRALIAS.CODE,
                    PRALIAS.ALCODE,
                    PRALIAS.ALNAME,
                    PRALIAS.PRCODE,
                    PRALIAS.UNITCODE,
                    PRALIAS.QTY,
                    PRALIAS.ALTYPE,
                    PRALIAS.INVUNITCODE,
                    PRMAST.PRNAME,
                    PRMAST.SECNAME,
                    PRMAST.STOCK AS STOCK,
                    PRMAST.UNIT,
                    PRMAST.ALTUNIT,
                    PRMAST.UNITCONV,
                    PRMAST.CONVTYPE,
                    PRALIAS.SALESPRICE,
                    PRALIAS.PURPRICE,
                    ISNULL(TAXMAS.TAXPERC,0) AS TAXPERC,
                    TAXMAS.CODE AS TAXCODE,
                    PRUNITMAS.PRUNITNAME AS UOM,
                    PRMAST.DESCRIPTION AS DESCRIPTION,
                    PRMAST.BASEPURPRICE,
                    TAXMAS.VATCODE,
                    PRMAST.SALESPRICETYPE,
                    PRMAST.BMPL,
                    PRMAST.BMPQ,
                    PRMAST.BMPW,
                    PRMAST.BMEQN,
                    PRMAST.ALTSALEPRICE,
                    PRMAST.BASESALEPRICE
                FROM PRALIAS
                INNER JOIN PRMAST
                    ON PRALIAS.PRCODE = PRMAST.PRCODE
                LEFT OUTER JOIN TAXMAS
                    ON PRMAST.TAXCODE = TAXMAS.CODE
                LEFT OUTER JOIN PRUNITMAS
                    ON PRALIAS.UNITCODE = PRUNITMAS.PRUNITID
                WHERE
                    PRALIAS.PRCODE='" + clsPrAlias.PrCode + "'" +
                " AND PRALIAS.ALNAME=N'" + clsPrAlias.AlName + "'";

            return await db.GetDatatable(qry);
        }


        // ============================================================
        // GET ALL ALIASES BY ALIAS CODE
        // ============================================================
        public async Task<DataTable> AllByAlcode(
            ClsPrAlias clsPrAlias)
        {
            qry = "SELECT * " +
                  "FROM PRALIAS " +
                  "WHERE ALCODE='" + clsPrAlias.AlCode + "'";

            return await db.GetDatatable(qry);
        }


        // ============================================================
        // GET ALIAS TYPE BY PRODUCT CODE
        // ============================================================
        public async Task<int> AlTypeByPrCode(
            ClsPrAlias clsPrAlias)
        {
            qry = "SELECT ALTYPE " +
                  "FROM PRALIAS " +
                  "WHERE PRCODE='" + clsPrAlias.PrCode + "'";

            DataTable dt = await db.GetDatatable(qry);

            if (dt.Rows.Count > 0)
            {
                return Convert.ToInt32(dt.Rows[0][0]);
            }

            return 0;
        }


        // ============================================================
        // ALIAS AND ALIAS CODE
        // ============================================================
        public async Task<DataTable> AliasAndAlCode()
        {
            qry = "SELECT CODE,ALNAME AS ALIAS " +
                  "FROM PRALIAS " +
                  "WHERE ALNAME<>'' AND ACTIVE='True' " +
                  "UNION ALL " +
                  "SELECT CODE,ALCODE AS ALIAS " +
                  "FROM PRALIAS " +
                  "WHERE ALCODE<>'' AND ACTIVE='True'";

            return await db.GetDatatable(qry);
        }


        // ============================================================
        // SEARCH BY ALIAS NAME
        // ============================================================
        public async Task<DataTable> SearchByAlName(
            ClsPrAlias clsPrAlias)
        {
            qry = "SELECT * " +
                  "FROM PRALIAS " +
                  "WHERE ALNAME=N'" + clsPrAlias.AlName + "'" +
                  " OR ALNAME=N'" + clsPrAlias.AlName.ToUpper() + "'" +
                  " OR ALNAME=N'" + clsPrAlias.AlName.ToLower() + "'";

            return await db.GetDatatable(qry);
        }


        // ============================================================
        // VALIDATE ALIAS NAME FOR UPDATE
        // ============================================================
        public async Task<bool> ValidtaeAlNameForUpdate(
            ClsPrAlias clsPrAlias)
        {
            qry = "SELECT * " +
                  "FROM PRALIAS " +
                  "WHERE " +
                  "(ALNAME=N'" + clsPrAlias.AlName + "'" +
                  " OR ALNAME=N'" + clsPrAlias.AlName.ToUpper() + "'" +
                  " OR ALNAME=N'" + clsPrAlias.AlName.ToLower() + "')" +
                  " AND PRCODE<>'" + clsPrAlias.PrCode + "'";

            DataTable dt = await db.GetDatatable(qry);

            return dt.Rows.Count == 0;
        }

        public async Task<int> DeleteByCode(ClsPrAlias clsPrAlias)
        {
            qry = "DELETE FROM PRALIAS " +
                  "WHERE CODE='" + clsPrAlias.Code + "'";

            return await db.Execute(qry);
        }
    }
}