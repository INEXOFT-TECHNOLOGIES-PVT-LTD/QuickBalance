using QuickBalance.API.CLS;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Respons_DTO;

namespace QuickBalance.API.Repository.Master
{
    public class BomRepository
    {
        private readonly DBConn _db;

        public BomRepository(DBConn db)
        {
            _db = db;
        }

        // ---------------------------------------------------------
        // DUPLICATE CHECK
        // ---------------------------------------------------------

        public async Task<bool> CheckDuplicateAsync(
            string field,
            string value,
            int? excludeCode = null)
        {
            string column = field.Trim().ToUpperInvariant();

            if (column != "BOMNAME" && column != "ALIAS")
                throw new ArgumentException("Invalid duplicate-check field.");

            string sql = $@"
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM dbo.BOMMAS
                WHERE {column} = @Value
                  AND ISNULL(ISDELETED, 0) = 0
                  AND (@ExcludeCode IS NULL OR CODE <> @ExcludeCode)
            )
            THEN 1 ELSE 0 END;";

            int result = await _db.QueryFirstOrDefaultAsync<int>(
                sql,
                new
                {
                    Value = value,
                    ExcludeCode = excludeCode
                });

            return result == 1;
        }



        public async Task<bool> CheckAccountExistsAsync(int accountId)
        {
            const string sql = @"
              SELECT COUNT(1)
            FROM dbo.ACCMAS
            WHERE ID = @AccountId;";

            int count = await _db.QueryFirstOrDefaultAsync<int>(
                sql,
                new { AccountId = accountId });

            return count > 0;
        }
        // ---------------------------------------------------------
        // CREATE BOM MASTER
        // ---------------------------------------------------------

        public async Task<int> CreateMasterAsync(
            BomRequest request,
            int userId)
        {
            const string sql = @"
            INSERT INTO dbo.BOMMAS
            (
                BOMNAME, ALIAS, PRCODE, QTY, UNIT, EXPENCES,
                MCGENFLG, MCGEN, MCCONSFLG, MCCONS,
                LOGCD, UTIME, SECNAME, ACTIVE
            )
            OUTPUT INSERTED.CODE
            VALUES
            (
                @BomName, @Alias, @PrCode, @Qty, @Unit, @Expences,
                @McGenFlg, @McGen, @McConsFlg, @McCons,
                @UserId, GETDATE(), @SecondName, @IsActive
            );";

            return await _db.QueryFirstOrDefaultAsync<int>(
                sql,
                new
                {
                    request.BomName,
                    request.Alias,
                    request.PrCode,
                    request.Qty,
                    request.Unit,
                    request.Expences,
                    request.McGenFlg,
                    request.McGen,
                    request.McConsFlg,
                    request.McCons,
                    UserId = userId,
                    request.SecondName,
                    request.IsActive
                });
        }

        // ---------------------------------------------------------
        // UPDATE BOM MASTER
        // ---------------------------------------------------------

        public async Task<int> UpdateMasterAsync(
            int id,
            BomRequest request,
            int userId)
        {
            const string sql = @"
            UPDATE dbo.BOMMAS
            SET
                BOMNAME = @BomName,
                ALIAS = @Alias,
                PRCODE = @PrCode,
                QTY = @Qty,
                UNIT = @Unit,
                EXPENCES = @Expences,
                MCGENFLG = @McGenFlg,
                MCGEN = @McGen,
                MCCONSFLG = @McConsFlg,
                MCCONS = @McCons,
                LOGCD = @UserId,
                UTIME = GETDATE(),
                SECNAME = @SecondName,
                ACTIVE = @IsActive
            WHERE CODE = @Id;";

            return await _db.ExecuteAsync(
                sql,
                new
                {
                    Id = id,
                    request.BomName,
                    request.Alias,
                    request.PrCode,
                    request.Qty,
                    request.Unit,
                    request.Expences,
                    request.McGenFlg,
                    request.McGen,
                    request.McConsFlg,
                    request.McCons,
                    UserId = userId,
                    request.SecondName,
                    request.IsActive
                });
        }

        // ---------------------------------------------------------
        // GET BOM MASTER
        // ---------------------------------------------------------

        public async Task<BomMasterDto?> GetMasterByIdAsync(int id)
        {
            const string sql = @"
            SELECT
                B.CODE AS Id,
                B.BOMNAME AS BomName,
                B.ALIAS AS Alias,
                B.PRCODE AS PrCode,
                B.QTY AS Qty,
                B.UNIT AS Unit,
                B.EXPENCES AS Expences,
                B.MCGENFLG AS McGenFlg,
                B.MCGEN AS McGen,
                B.MCCONSFLG AS McConsFlg,
                B.MCCONS AS McCons,
                B.LOGCD AS LogCd,
                B.UTIME AS UTime,
                B.SECNAME AS SecondName,
                P.PRNAME AS ProductName,
                U.PRUNITNAME AS UnitName,
                CAST(ISNULL(B.ACTIVE, 0) AS BIT) AS IsActive
            FROM dbo.BOMMAS B
            LEFT JOIN dbo.PRMAST P ON B.PRCODE = P.PRCODE
            LEFT JOIN dbo.PRUNITMAS U ON B.UNIT = U.PRUNITID
            WHERE B.CODE = @Id
              AND ISNULL(B.ISDELETED, 0) = 0;";

            return await _db.QueryFirstOrDefaultAsync<BomMasterDto>(
                sql,
                new { Id = id });
        }

        // ---------------------------------------------------------
        // GET ALL BOMs - PAGINATED
        // ---------------------------------------------------------

        //public async Task<PagedResponse<BomListDetailsDto>> GetAllAsync(
        // int page,
        // int pageSize,
        // string? search)
        //    {
        //        page = Math.Max(1, page);
        //        pageSize = Math.Clamp(pageSize, 1, 100);

        //        string whereSql = @"
        //    WHERE
        //    (
        //        @Search IS NULL
        //        OR BOMNAME LIKE '%' + @Search + '%'
        //        OR ALIAS LIKE '%' + @Search + '%'
        //        OR CONVERT(VARCHAR(20), CODE) LIKE '%' + @Search + '%'
        //    )";

        //        var parameters = new
        //        {
        //            Search = string.IsNullOrWhiteSpace(search)
        //                ? null
        //                : search.Trim()
        //        };

        //        string countSql = $@"
        //    SELECT COUNT(1)
        //    FROM dbo.BOMMAS
        //    {whereSql};";

        //        int totalRecords = await _db.QueryFirstOrDefaultAsync<int>(
        //            countSql,
        //            parameters);

        //        string dataSql = $@"
        //    SELECT
        //        CODE AS Id,
        //        BOMNAME AS Bom,
        //        ALIAS AS Alias
        //    FROM dbo.BOMMAS
        //    {whereSql}
        //    ORDER BY CODE DESC
        //    OFFSET @Offset ROWS
        //    FETCH NEXT @PageSize ROWS ONLY;";

        //        var data = await _db.QueryListAsync<BomListDetailsDto>(
        //            dataSql,
        //            new
        //            {
        //                Search = parameters.Search,
        //                Offset = (page - 1) * pageSize,
        //                PageSize = pageSize
        //            });

        //        int totalPages = totalRecords == 0
        //            ? 0
        //            : (int)Math.Ceiling(totalRecords / (double)pageSize);

        //        return new PagedResponse<BomListItemDto>
        //        {
        //            Data = data.ToList(),
        //            Pagination = new PaginationInfo
        //            {
        //                Page = page,
        //                PageSize = pageSize,
        //                TotalRecords = totalRecords,
        //                TotalPages = totalPages
        //            }
        //    };
        //}


        public async Task<PagedResponse<BomListItemDto>> GetAllAsync(
      int page,
      int pageSize,
      string? search,
      bool? active = null)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var parameters = new
            {
                Search = string.IsNullOrWhiteSpace(search)
                    ? null
                    : search.Trim(),
                Active = active
            };

            string whereSql = @"
            WHERE ISNULL(BM.ISDELETED, 0) = 0
            AND (@Active IS NULL OR ISNULL(BM.ACTIVE, 0) = @Active)
            AND
            (
                @Search IS NULL
                OR BM.BOMNAME LIKE '%' + @Search + '%'
                OR BM.ALIAS LIKE '%' + @Search + '%'
                OR CONVERT(VARCHAR(20), BM.CODE) LIKE '%' + @Search + '%'
            )";

                // ============================================
                // TOTAL RECORDS
                // ============================================

                string countSql = $@"
            SELECT COUNT(1)
            FROM dbo.BOMMAS BM
            {whereSql};";

            int totalRecords = await _db.QueryFirstOrDefaultAsync<int>(
                countSql,
                parameters);

            // ============================================
            // BOM MASTER
            // ============================================

            string masterSql = $@"
            SELECT
                BM.CODE AS Id,
                BM.BOMNAME AS BomName,
                BM.ALIAS AS Alias,
                P.PRNAME AS ProductName,
                BM.QTY AS Qty,
                U.PRUNITNAME AS UnitName,
                CAST(ISNULL(BM.ACTIVE, 0) AS BIT) AS IsActive
            FROM dbo.BOMMAS BM
            LEFT JOIN dbo.PRMAST P ON BM.PRCODE = P.PRCODE
            LEFT JOIN dbo.PRUNITMAS U ON BM.UNIT = U.PRUNITID
            {whereSql}
            ORDER BY BM.CODE DESC
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;";

            var result = (await _db.QueryListAsync<BomListItemDto>(
                masterSql,
                new
                {
                    Search = parameters.Search,
                    Active = parameters.Active,
                    Offset = (page - 1) * pageSize,
                    PageSize = pageSize
                })).ToList();

            // ============================================
            // PAGINATION
            // ============================================

            int totalPages = totalRecords == 0
                ? 0
                : (int)Math.Ceiling(totalRecords / (double)pageSize);

            return new PagedResponse<BomListItemDto>
            {
                Data = result,

                Pagination = new PaginationInfo
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    TotalPages = totalPages
                }
            };
        }
        
        // ---------------------------------------------------------
        // BOM ITEM INSERT
        // ---------------------------------------------------------

        public async Task<int> CreateItemAsync(
            int bomCode,
            BomItemRequest request)
        {
            const string sql = @"
            INSERT INTO dbo.BOMT
            (
                BOMCODE, PRCODE, QTY, UNIT, TYPE
            )
            VALUES
            (
                @BomCode, @ProductId, @Quantity, @UnitId, @Type
            );";

            return await _db.ExecuteAsync(
                sql,
                new
                {
                    BomCode = bomCode,
                    request.ProductId,
                    request.Quantity,
                    request.UnitId,
                    Type = NormalizeType(request.Type)
                });
        }

        // ---------------------------------------------------------
        // GET BOM ITEMS
        // ---------------------------------------------------------

        public async Task<List<BomItemDto>> GetItemsByBomCodeAsync(int bomCode)
        {
            const string sql = @"
            SELECT
                T.BOMCODE AS Id,
                T.PRCODE AS ProductId,
                T.QTY AS Quantity,
                T.UNIT AS UnitId,
                T.TYPE AS Type,
                P.BASEPURPRICE AS Price,
                P.PURPRICETYPE AS PurPriceType,
                P.ALTPURPRICE AS AltPurPrice,
                P.UNIT AS MainUnit,
                P.ALTUNIT AS AltUnit,
                P.PRNAME AS PrName,
                P.BASESALEPRICE AS BaseSalePrice,
                P.ALTSALEPRICE AS AltSalePrice
            FROM dbo.BOMT T
            INNER JOIN dbo.PRMAST P ON T.PRCODE = P.PRCODE
            WHERE T.BOMCODE = @BomCode;";

            var result = await _db.QueryListAsync<BomItemDto>(
                sql,
                new { BomCode = bomCode });

            return result;
        }

        // ---------------------------------------------------------
        // GET BOM RECORDS BY PRODUCT
        // ---------------------------------------------------------

        public async Task<List<BomItemDto>> GetByProductCodeAsync(int productId)
        {
            const string sql = @"
            SELECT
                T.BOMCODE AS Id,
                T.PRCODE AS ProductId,
                T.QTY AS Quantity,
                T.UNIT AS UnitId,
                T.TYPE AS Type
            FROM dbo.BOMT T
            INNER JOIN dbo.BOMMAS BM ON BM.CODE = T.BOMCODE
            WHERE T.PRCODE = @ProductId
              AND ISNULL(BM.ISDELETED, 0) = 0;";

            return await _db.QueryListAsync<BomItemDto>(
                sql,
                new { ProductId = productId });
        }

        // ---------------------------------------------------------
        // DELETE BOM ITEMS
        // ---------------------------------------------------------

        public Task<int> DeleteItemsByBomCodeAsync(int bomCode)
        {
            const string sql = @"
            DELETE FROM dbo.BOMT
            WHERE BOMCODE = @BomCode;";

            return _db.ExecuteAsync(sql, new { BomCode = bomCode });
        }

        // ---------------------------------------------------------
        // ADDITIONAL INFO INSERT
        // ---------------------------------------------------------

        public Task<int> CreateAdditionalInfoAsync(
            int bomCode,
            BomAdditionalInfoRequest request)
        {
            const string sql = @"
            INSERT INTO dbo.BOMD (BOMCODE, CODE, AMOUNT)
            VALUES (@BomCode, @AccountId, @Amount);";

            return _db.ExecuteAsync(
                sql,
                new
                {
                    BomCode = bomCode,
                    request.AccountId,
                    request.Amount
                });
        }

        // ---------------------------------------------------------
        // GET ADDITIONAL INFO
        // ---------------------------------------------------------

        public async Task<List<BomAdditionalInfoDto>> GetAdditionalInfoAsync(
            int bomCode)
        {
            const string sql = @"
            SELECT
                BOMCODE AS Id,
                CODE AS AccountId,
                AMOUNT AS Amount
            FROM dbo.BOMD
            WHERE BOMCODE = @BomCode;";

            return await _db.QueryListAsync<BomAdditionalInfoDto>(
                sql,
                new { BomCode = bomCode });
        }

        // ---------------------------------------------------------
        // DELETE ADDITIONAL INFO
        // ---------------------------------------------------------

        public Task<int> DeleteAdditionalInfoAsync(int bomCode)
        {
            const string sql = @"
            DELETE FROM dbo.BOMD
            WHERE BOMCODE = @BomCode;";

            return _db.ExecuteAsync(sql, new { BomCode = bomCode });
        }

        // ---------------------------------------------------------
        // DELETE BOM MASTER
        // ---------------------------------------------------------

        public Task<int> DeleteMasterAsync(int id, int userId)
        {
            const string sql = @"
            UPDATE dbo.BOMMAS
            SET ISDELETED = 1,
                ACTIVE = 0,
                LOGCD = @UserId,
                UTIME = GETDATE()
            WHERE CODE = @Id
              AND ISNULL(ISDELETED, 0) = 0;";

            return _db.ExecuteAsync(sql, new { Id = id, UserId = userId });
        }

        // ---------------------------------------------------------
        // HELPERS
        // ---------------------------------------------------------

        private static string NormalizeType(string? value)
        {
            string type = value?.Trim().ToUpperInvariant() ?? "C";
            return type is "C" or "G" ? type : "C";
        }
    }
}
