using QuickBalance.API.CLS;
using System.Data;

namespace QuickBalance.API.DB
{
    public class PrImgDB
    {
        private readonly DBConn db;

        public Variable Variables { get; set; } = new Variable();

        public CommonFunction Functions { get; set; }

        public PrImgDB(DBConn db)
        {
            this.db = db;
            Functions = new CommonFunction(db);
        }

        // ============================================================
        // INSERT
        // ============================================================

        public async Task<int> Insert(
            int PrCode,
            string imgarr)
        {
            string qry =
                "INSERT INTO PRIMG (PRCODE,PRIMG) " +
                "VALUES('" + PrCode + "','" + imgarr + "')";

            return await db.Execute(qry);
        }

        // ============================================================
        // UPDATE
        // ============================================================

        public async Task<int> Update(
            int PrCode,
            string imgarr)
        {
            string qry =
                "UPDATE PRIMG SET PRIMG='" + imgarr +
                "' WHERE PRCODE='" + PrCode + "'";

            return await db.Execute(qry);
        }

        // ============================================================
        // SEARCH BY ID
        // ============================================================

        public async Task<DataTable> SearchByID(
            ClsPrMast clsPrMast)
        {
            string qry =
                "SELECT * FROM PRIMG " +
                "WHERE PRCODE='" +
                clsPrMast.PrCode +
                "'";

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // GET ALL
        // ============================================================

        public async Task<DataTable> GetAll()
        {
            string qry =
                "SELECT * FROM PRIMG";

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // SEARCH BY ID - REQUEST
        // ============================================================

        public async Task<DataTable> SearchByID_Req(
            ClsPrMast clsPrMast)
        {
            string qry =
                "SELECT * FROM PRIMG_TMP " +
                "WHERE PRCODE='" +
                clsPrMast.PrCode +
                "'";

            return await db.GetDatatable(qry);
        }

        // ============================================================
        // INSERT - REQUEST
        // ============================================================

        public async Task<int> Insert_Req(
            int PrCode,
            string imgarr,
            int hbBrcd)
        {
            string qry =
                "INSERT INTO PRIMG_TMP " +
                "(PRCODE,PRIMG,BRCD,REQID) " +
                "VALUES('" +
                PrCode +
                "','" +
                imgarr +
                "'," +
                hbBrcd +
                "," +
                PrCode +
                ")";

            return await db.Execute(qry);
        }

        // ============================================================
        // UPDATE - REQUEST
        // ============================================================

        public async Task<int> Update_Req(
            int PrCode,
            string imgarr)
        {
            string qry =
                "UPDATE PRIMG_TMP SET PRIMG='" +
                imgarr +
                "' WHERE PRCODE='" +
                PrCode +
                "'";

            return await db.Execute(qry);
        }

        // ============================================================
        // DELETE - REQUEST
        // ============================================================

        public async Task<int> Delete_Req(
            int PrCode)
        {
            string qry =
                "DELETE FROM PRIMG_TMP " +
                "WHERE PRCODE='" +
                PrCode +
                "'";

            return await db.Execute(qry);
        }

        public async Task<int> Delete(int PrCode)
        {
            string qry =
                "DELETE FROM PRIMG " +
                "WHERE PRCODE='" +
                PrCode +
                "'";

            return await db.Execute(qry);
        }
    }
}