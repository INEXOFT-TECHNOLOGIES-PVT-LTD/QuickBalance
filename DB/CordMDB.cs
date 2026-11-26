using Dapper;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs.Respons_DTO;
using System.Data;

namespace QuickBalance.API.DB
{
    public class CordMDB
    {
        private readonly DBConn db;
        public Variable Variables = new Variable();
        public Function Functions = new Function();
        string qry = "";

        public CordMDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public async Task<DataTable> ListAsync(string STYPE)
        {
            //int page = Variables.pgRequest.Page;
            //int pageSize = Variables.pgRequest.PageSize;

            //string? search = Variables.pgRequest.Search;
            //DateTime? fromDate = Variables.pgRequest.FromDate;
            //DateTime? toDate = Variables.pgRequest.ToDate;


            int page = Variables.Page;
            int pageSize = Variables.PageSize;

            string? search = Variables.Search;
            DateTime? fromDate = Variables.FromDate;
            DateTime? toDate = Variables.ToDate;

            int offset = (page - 1) * pageSize;

            string searchCondition = "";

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Replace("'", "''");

                searchCondition = $@"
            AND (
                CORDM.{Functions.VSDOCNO()} LIKE '%{search}%'
                OR ACCMAS.ACCNAME LIKE '%{search}%'
            )";
            }

            string dateCondition = "";

            if (fromDate.HasValue)
            {
                dateCondition += $@"
            AND CORDM.ORDDATE >= '{fromDate.Value.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture)}'";
            }

            if (toDate.HasValue)
            {
                dateCondition += $@"
            AND CORDM.ORDDATE < '{toDate.Value.AddDays(1).ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture)}'";
            }

            qry = $@"
        SELECT
            CORDM.ORDNO AS Id,
            CORDM.{Functions.VSDOCNO()} AS [Order No],
            FORMAT(CORDM.ORDDATE, '{Variables.format}') AS [Order Date],
            ACCMAS.ACCNAME AS Customer,
            FORMAT(CORDM.AMOUNT, '{Variables.DecString}') AS Amount,
            CAST(ISNULL(CORDM.CANCELFLG, 0) AS BIT) AS Cancelled,
            COUNT(*) OVER() AS TotalRecords
        FROM CORDM
        INNER JOIN ACCMAS ON CORDM.CUSCODE = ACCMAS.ID
        WHERE CORDM.STYPE = '{STYPE}'
            {Functions.LIST_ALLOW_VR_STMNT(true, "CORDM")}
            {searchCondition}
            {dateCondition}
        ORDER BY CORDM.ORDNO DESC
        OFFSET {offset} ROWS
        FETCH NEXT {pageSize} ROWS ONLY";

            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> List(string STYPE)
        {
            int page = Variables.pgRequest.Page;
            int pageSize = Variables.pgRequest.PageSize;

            string? search = Variables.pgRequest.Search;
            DateTime? fromDate = Variables.pgRequest.FromDate;
            DateTime? toDate = Variables.pgRequest.ToDate;

            string orderBy = "CORDM.ORDNO DESC";
            switch (Variables.pgRequest.SortBy?.ToLower())
            {
                case "ordno":
                case "orderno":
                    orderBy = "CORDM.ORDNO";
                    break;

                case "date":
                case "orderdate":
                    orderBy = "CORDM.ORDDATE";
                    break;

                case "customer":
                    orderBy = "ACCMAS.ACCNAME";
                    break;

                case "amount":
                    orderBy = "CORDM.AMOUNT";
                    break;
            }
            string sortDirection = Variables.pgRequest.SortDirection == "ASC" ? "ASC" : "DESC";
            orderBy += $" {sortDirection}";

            int offset = (page - 1) * pageSize;

            string searchCondition = "";

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Replace("'", "''");
                searchCondition = $@" AND (CORDM.{Functions.VSDOCNO()} LIKE '%{search}%' OR ACCMAS.ACCNAME LIKE '%{search}%')";
            }

            string dateCondition = "";
            if (fromDate.HasValue)
                dateCondition += $" AND CORDM.ORDDATE >= '{fromDate.Value.ToString(Variables.format)}'";
            if (toDate.HasValue)
                dateCondition += $" AND CORDM.ORDDATE < '{toDate.Value.AddDays(1).ToString(Variables.format)}'";

            qry = $@"SELECT CORDM.ORDNO AS Id, CORDM.{Functions.VSDOCNO()} AS [Order No], FORMAT(CORDM.ORDDATE, '{Variables.format}') AS [Order Date], ACCMAS.ACCNAME AS Customer, FORMAT(CORDM.AMOUNT, '{Variables.DecString}') AS Amount, COUNT(*) OVER() AS TotalRecords
FROM CORDM INNER JOIN ACCMAS ON CORDM.CUSCODE = ACCMAS.ID
WHERE CORDM.STYPE = '{STYPE}'{Functions.LIST_ALLOW_VR_STMNT(true, "CORDM")}{searchCondition}{dateCondition}
ORDER BY {orderBy}
OFFSET {offset} ROWS
FETCH NEXT {pageSize} ROWS ONLY";

            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> SearchByOrdNO(ClsCordM clsCordM)
        {
            qry = "SELECT SMMAST.SMNAME, ACCMAS.ACCNAME, CORDM.*, CORDM.INVNO AS SALNO, DTUSER.USERNAME FROM (((ACCMAS INNER JOIN CORDM ON ACCMAS.ID = CORDM.CUSCODE) LEFT JOIN SMMAST ON CORDM.SMCODE = SMMAST.SMCODE) LEFT JOIN (SELECT USERCODE, FULLNAME AS USERNAME FROM TBUSER) AS DTUSER ON CORDM.LOGCD = DTUSER.USERCODE) WHERE CORDM.ORDNO='" + clsCordM.OrdNo + "' AND CORDM.STYPE='" + clsCordM.SType + "'";
            return await db.GetDatatable(qry);
        }

        //public async Task<SalesOrderHeaderDto?> SearchByOrdNOAsync(ClsCordM request)
        //{
        //    const string sql = @"
        //SELECT
        //    SMMAST.SMNAME,
        //    ACCMAS.ACCNAME,
        //    CORDM.*,
        //    CORDM.INVNO AS SALNO,
        //    DTUSER.USERNAME
        //FROM ACCMAS
        //INNER JOIN CORDM
        //    ON ACCMAS.ID = CORDM.CUSCODE
        //LEFT JOIN SMMAST
        //    ON CORDM.SMCODE = SMMAST.SMCODE
        //LEFT JOIN
        //(
        //    SELECT
        //        USERCODE,
        //        FULLNAME AS USERNAME
        //    FROM TBUSER
        //) AS DTUSER
        //    ON CORDM.LOGCD = DTUSER.USERCODE
        //WHERE CORDM.ORDNO = @OrdNo
        //  AND CORDM.STYPE = @SType;";

        //    //using var connection = db.GetConnection();

        //    //if (connection.State != ConnectionState.Open)
        //    //    await connection.OpenAsync();

        //    return await db.QueryFirstOrDefaultAsync<SalesOrderHeaderDto>(
        //        sql,
        //        new
        //        {
        //            request.OrdNo,
        //            request.SType
        //        });
        //}

        public async Task<SalesOrderHeaderDto?> SearchByOrdNOAsync(ClsCordM request)
        {
            const string sql = @"
            SELECT
                CORDM.*,
                CORDM.INVNO AS SALNO,
                ACCMAS.ACCNAME AS ACCNAME,
                SMMAST.SMNAME AS SMNAME,
                DTUSER.USERNAME AS USERNAME
            FROM CORDM
            INNER JOIN ACCMAS
                ON ACCMAS.ID = CORDM.CUSCODE
            LEFT JOIN SMMAST
                ON SMMAST.SMCODE = CORDM.SMCODE
            LEFT JOIN
            (
                SELECT
                    USERCODE,
                    FULLNAME AS USERNAME
                FROM TBUSER
            ) AS DTUSER
                ON DTUSER.USERCODE = CORDM.LOGCD
            WHERE CORDM.ORDNO = @OrdNo
              AND CORDM.STYPE = @SType;";

            return await db.QueryFirstOrDefaultAsync<SalesOrderHeaderDto>(
                sql,
                new
                {
                    request.OrdNo,
                    request.SType
                });
        }


    }
}
