using System.Data;
using System.Threading.Tasks;

namespace QuickBalance.API.DB
{
    public class AccountDB
    {
        private readonly DBConn DBConn;

        public AccountDB(DBConn dbConn)
        {
            DBConn = dbConn;
        }

        public async Task<DataTable> GetActiveAccounts()
        {
            string qry = @"
                SELECT
                    ID,
                    ACCNAME,
                    ISNULL(ALIAS, '') AS ALIAS,
                    ACCODE
                FROM ACCMAS
                WHERE ACTIVE = 1
                ORDER BY ACCNAME;
            ";

            return await DBConn.GetDatatable(qry);
        }

        public async Task<(DataTable Data, int TotalRecords)> GetActiveAccounts(int page,int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            long offset = ((long)page - 1) * pageSize;

            const string countSql = @"
            SELECT COUNT(*)
            FROM ACCMAS
            WHERE ACTIVE = 1;";

                string qry = $@"
            SELECT
                ID,
                ACCNAME,
                ISNULL(ALIAS, '') AS ALIAS,
                ACCODE
            FROM ACCMAS
            WHERE ACTIVE = 1
            ORDER BY ACCNAME, ID
            OFFSET {offset} ROWS
            FETCH NEXT {pageSize} ROWS ONLY;";

            DataTable countTable = await DBConn.GetDatatable(countSql);

            int totalRecords = countTable.Rows.Count > 0
                ? Convert.ToInt32(countTable.Rows[0][0])
                : 0;

            DataTable data = await DBConn.GetDatatable(qry);

            return (data, totalRecords);
        }
    }
}