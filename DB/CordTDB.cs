using Dapper;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs.Respons_DTO;
using System.Data;

namespace QuickBalance.API.DB
{
    public class CordTDB
    {
        private readonly DBConn db;
        public Variable Variables = new Variable();
        public Function Functions = new Function();
        string qry = "";

        public CordTDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public async Task<DataTable> SearchByOrdNo(ClsCordT clsCordT)
        {
            qry = "SELECT PRMAST.PRNAME AS PRNAME,PRMAST.ALIAS,PRMAST.SECNAME AS SECNAME,PRMAST.UNIT,PRMAST.ALTUNIT,PRMAST.SALESPRICETYPE,PRMAST.BMEQN,PRUNITMAS.PRUNITNAME,PRUNITMAS.SECNAME AS UNITSECNAME, CORDT.* FROM ((PRMAST INNER JOIN (SELECT *, ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS SL FROM CORDT) AS CORDT ON PRMAST.PRCODE = CORDT.PRCODE)LEFT JOIN PRUNITMAS ON CORDT.UNITCODE = PRUNITMAS.PRUNITID) WHERE ORDNO='" + clsCordT.OrdNo + "' AND STYPE='" + clsCordT.SType + "' ORDER BY SL";
            return await db.GetDatatable(qry);
        }
        //public async Task<List<SalesOrderItemDto>> SearchByOrdNoAsync(ClsCordT request)
        //{
        //    const string sql = @"
        //    SELECT
        //        PRMAST.PRNAME AS PRNAME,
        //        PRMAST.ALIAS,
        //        PRMAST.SECNAME AS SECNAME,
        //        PRMAST.UNIT,
        //        PRMAST.ALTUNIT,
        //        PRMAST.SALESPRICETYPE,
        //        PRMAST.BMEQN,
        //        PRUNITMAS.PRUNITNAME,
        //        PRUNITMAS.SECNAME AS UNITSECNAME,
        //        CORDT.*
        //    FROM PRMAST
        //    INNER JOIN
        //    (
        //        SELECT
        //            *,
        //            ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS SL
        //        FROM CORDT
        //    ) AS CORDT
        //        ON PRMAST.PRCODE = CORDT.PRCODE
        //    LEFT JOIN PRUNITMAS
        //        ON CORDT.UNITCODE = PRUNITMAS.PRUNITID
        //    WHERE CORDT.ORDNO = @OrdNo
        //      AND CORDT.STYPE = @SType
        //    ORDER BY CORDT.SL;";

        //    //using var connection = db.GetConnection();

        //    //if (connection.State != ConnectionState.Open)
        //    //    await connection.OpenAsync();

        //    var result = await db.QueryListAsync<SalesOrderItemDto>(
        //        sql,
        //        new
        //        {
        //            request.OrdNo,
        //            request.SType
        //        });

        //    return result.AsList();
        //}
        //      public async Task<List<SalesOrderItemDto>> SearchByOrdNoAsync(
        //ClsCordT request)
        //      {
        //          const string sql = @"
        //      SELECT
        //          PRMAST.PRNAME AS PRNAME,
        //          PRMAST.ALIAS AS ALIAS,
        //          PRMAST.SECNAME AS SECNAME,
        //          PRMAST.UNIT AS UNIT,
        //          PRMAST.ALTUNIT AS ALTUNIT,
        //          PRMAST.SALESPRICETYPE AS SALESPRICETYPE,
        //          PRMAST.BMEQN AS BMEQN,
        //          PRUNITMAS.PRUNITNAME AS PRUNITNAME,
        //          PRUNITMAS.SECNAME AS UNITSECNAME,
        //          CORDT.*
        //      FROM CORDT
        //      LEFT JOIN PRMAST
        //          ON PRMAST.PRCODE = CORDT.PRCODE
        //      LEFT JOIN PRUNITMAS
        //          ON CORDT.UNITCODE = PRUNITMAS.PRUNITID
        //      WHERE CORDT.ORDNO = @OrdNo
        //        AND CORDT.STYPE = @SType
        //      ORDER BY CORDT.ORDNO;";

        //          var result = await db.QueryListAsync<SalesOrderItemDto>(
        //              sql,
        //              new
        //              {
        //                  request.OrdNo,
        //                  request.SType
        //              });

        //          return result.AsList();
        //      }


        public async Task<List<SalesOrderItemDto>> SearchByOrdNoAsync(ClsCordT request)
        {
            // Lines in the order they were entered (ORDROWID = slNo from the save).
            // Before: ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) over the WHOLE CORDT table -> no guaranteed order.
            // LEFT JOIN PRMAST: a line is still returned even if its product was deleted later.
            const string sql = @"
          SELECT
              CORDT.*,
              PRMAST.PRNAME AS PRNAME,
              PRMAST.ALIAS AS ALIAS,
              PRMAST.SECNAME AS SECNAME,
              PRMAST.UNIT AS UNIT,
              PRMAST.ALTUNIT AS ALTUNIT,
              PRMAST.SALESPRICETYPE AS SALESPRICETYPE,
              PRMAST.BMEQN AS BMEQN,
              PRUNITMAS.PRUNITNAME AS PRUNITNAME,
              PRUNITMAS.SECNAME AS UNITSECNAME,
              CAST(ISNULL(CORDT.ORDROWID, 0) AS BIGINT) AS SL
          FROM CORDT
          LEFT JOIN PRMAST
              ON PRMAST.PRCODE = CORDT.PRCODE
          LEFT JOIN PRUNITMAS
              ON PRUNITMAS.PRUNITID = CORDT.UNITCODE
          WHERE CORDT.ORDNO = @OrdNo
            AND CORDT.STYPE = @SType
          ORDER BY CORDT.ORDROWID;";

            var result = await db.QueryListAsync<SalesOrderItemDto>(
                sql,
                new
                {
                    request.OrdNo,
                    request.SType
                });

            return result.AsList();
        }

        public async Task<DataTable> SearchByPrCode2(ClsCordT clsCordT)
        {
            qry = "SELECT * FROM dbo.CORDT WHERE PRCODE = '" + clsCordT.PrCode + "' AND CANCELFLG = 'False'";
            return await db.GetDatatable(qry);
        }
    }
}
