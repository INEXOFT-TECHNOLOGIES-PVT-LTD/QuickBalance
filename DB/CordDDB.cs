using Dapper;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs.Respons_DTO;
using System.Data;

namespace QuickBalance.API.DB
{
    public class CordDDB
    {
        private readonly DBConn db;
        public Variable Variables = new Variable();
        public Function Functions = new Function();
        string qry = "";

        public CordDDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public async Task<DataTable> SearchAddInfo(ClsCordD clsCordD)
        {
            qry = "SELECT CODE,TAX,TAXAMT FROM CORDD WHERE VRNO='" + clsCordD.VrNo + "' AND VRTYPE='" + clsCordD.VrType + "' AND DFLAG='" + clsCordD.DFlag + "' AND CODE<>'0'";
            return await db.GetDatatable(qry);
        }
        public async Task<List<AddInfoDto>> SearchAddInfoAsync(ClsCordD request)
        {
            const string sql = @"
            SELECT CODE, TAX, TAXAMT
            FROM CORDD
            WHERE VRNO = @VrNo
              AND VRTYPE = @VrType
              AND DFLAG = @DFlag
              AND CODE <> '0';";



            var result = await db.QueryListAsync<AddInfoDto>(
                sql,
                new
                {
                    request.VrNo,
                    request.VrType,
                    request.DFlag
                });

            return result.AsList();
        }
    }
}
