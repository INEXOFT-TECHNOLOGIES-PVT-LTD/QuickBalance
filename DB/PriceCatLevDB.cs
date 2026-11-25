using System.Data;
using QuickBalance.API.CLS;

namespace QuickBalance.API.DB
{
    public class PriceCatLevDB
    {
        private readonly DBConn db;

        public Variable Variables { get; set; } = new Variable();

        public CommonFunction Functions { get; set; }

        public PriceCatLevDB(DBConn db)
        {
            this.db = db;
            Functions = new CommonFunction(db);
        }

        // ============================================================
        // INSERT PRICE CATEGORY LEVEL
        // ============================================================
        public async Task<int> Insert(ClsPriceCatLev clsPriceCatLev)
        {
            string qry = "";

            try
            {
                qry = @"
INSERT INTO PRICECATLEV
(
    PRCODE,
    CATNAME,
    DISCSTRUCT,
    LEVFROM,
    LEVTO,
    DISCPERC,
    EFFVALUE,
    LOGCD,
    UTIME,
    CATCODE,
    APPPRICE
)
VALUES
(
    " + clsPriceCatLev.PrCode + @",
    '" + clsPriceCatLev.CatName + @"',
    '" + clsPriceCatLev.DiscStruct + @"',
    " + clsPriceCatLev.LevFrom + @",
    " + clsPriceCatLev.LevTo + @",
    " + clsPriceCatLev.DiscPerc + @",
    " + clsPriceCatLev.EffValue + @",
    " + clsPriceCatLev.LogCd + @",
    GETDATE(),
    " + clsPriceCatLev.CatCode + @",
    " + clsPriceCatLev.AppPrice + @"
)";

                return await db.Execute(qry);
            }
            catch
            {
                return -1;
            }
        }

        // ============================================================
        // DELETE ALL PRICE CATEGORY LEVELS FOR PRODUCT
        // ============================================================
        public async Task<int> DeleteByPrCode(ClsPriceCatLev clsPriceCatLev)
        {
            string qry = "";

            try
            {
                qry = @"
DELETE FROM PRICECATLEV
WHERE PRCODE = " + clsPriceCatLev.PrCode;

                return await db.Execute(qry);
            }
            catch
            {
                return -1;
            }
        }

        // ============================================================
        // SEARCH PRICE CATEGORY LEVELS BY PRODUCT
        // ============================================================
        public async Task<DataTable> SearchByPrCode(ClsPriceCatLev clsPriceCatLev)
        {
            string qry = "";

            try
            {
                qry = @"
SELECT
    CODE,
    LOGCD,
    PRCODE,
    CATNAME,
    DISCSTRUCT,
    LEVFROM,
    LEVTO,
    DISCPERC,
    EFFVALUE,
    UTIME,
    CATCODE,
    APPPRICE
FROM PRICECATLEV
WHERE PRCODE = " + clsPriceCatLev.PrCode + @"
ORDER BY CODE";

                return await db.GetDatatable(qry);
            }
            catch
            {
                return new DataTable();
            }
        }


        public async Task<int> Update(ClsPriceCatLev clsPriceCatLev)
        {
            string qry = "";

            try
            {
                qry = @"
UPDATE PRICECATLEV
SET
    CATNAME = '" + clsPriceCatLev.CatName + @"',
    DISCSTRUCT = '" + clsPriceCatLev.DiscStruct + @"',
    LEVFROM = " + clsPriceCatLev.LevFrom + @",
    LEVTO = " + clsPriceCatLev.LevTo + @",
    DISCPERC = " + clsPriceCatLev.DiscPerc + @",
    EFFVALUE = " + clsPriceCatLev.EffValue + @",
    LOGCD = " + clsPriceCatLev.LogCd + @",
    UTIME = GETDATE(),
    CATCODE = " + clsPriceCatLev.CatCode + @",
    APPPRICE = " + clsPriceCatLev.AppPrice + @"
WHERE CODE = " + clsPriceCatLev.Code + @"
AND PRCODE = " + clsPriceCatLev.PrCode;

                return await db.Execute(qry);
            }
            catch
            {
                return -1;
            }
        }

        // ============================================================
        // DELETE PRICE CATEGORY LEVEL BY CODE
        // ============================================================
        public async Task<int> DeleteByCode(ClsPriceCatLev clsPriceCatLev)
        {
            string qry = "";

            try
            {
                qry = @"
DELETE FROM PRICECATLEV
WHERE CODE = " + clsPriceCatLev.Code + @"
AND PRCODE = " + clsPriceCatLev.PrCode;

                return await db.Execute(qry);
            }
            catch
            {
                return -1;
            }
        }
    }
}