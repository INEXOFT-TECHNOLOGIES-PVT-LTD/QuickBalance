
using Dapper;
using Microsoft.Data.SqlClient;
using QuickBalance.API.CLS;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Product;
using QuickBalance.API.DTOs.Respons_DTO;
using System.Data;
using System.Runtime.Intrinsics.X86;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using static QuickBalance.API.DTOs.ProductRequest;

namespace QuickBalance.API.Repository.Master;

public sealed class ProductDB
{
    private readonly DBConn _db;

    public ProductDB(DBConn db)
    {
        _db = db;
    }

    
    private const string SelectColumns = """
    P.PRCODE AS Id,
    P.PRNAME AS PrName,
    P.ALIAS AS Alias,
    P.PRINTNAME AS PrintName,
    P.DESCRIPTION AS Description,
    P.UPCCODE AS UpcCode,
    P.SKUCODE AS SkuCode,
    P.BARCODE AS BarCode,

    P.GROUPCODE AS GroupId,
    P.CATGCODE AS CategoryId,
    P.BRAND AS BrandId,

    P.UNIT AS Unit,
    P.ALTUNIT AS AltUnit,
    P.UNITCONV AS UnitConv,
    P.CONVTYPE AS ConvType,

    P.OPNSTOCK AS OpnStock,
    P.OPNSTKVAL AS OpnStkval,

    P.PURPRICETYPE AS PurPriceType,
    P.BASEPURPRICE AS BasePurPrice,
    P.ALTPURPRICE AS AltPurPrice,

    P.SALESPRICETYPE AS SalesPriceType,
    P.BASESALEPRICE AS BaseSalePrice,
    P.BASEMINPRICE AS BaseMinPrice,
    P.ALTSALEPRICE AS AltSalePrice,
    P.ALTMINPRICE AS AltMinPrice,

    P.COSTMETH AS CostMeth,
    P.TAXCODE AS TaxCatId,
    P.SALEACCODE AS SaleAcId,
    P.PURACCODE AS PurAcId,

    P.MINQTY AS MinQty,
    P.REORDQTY AS ReOrdQty,
    P.MAXQTY AS MaxQty,

    P.SERVFLG AS ServFlg,
    P.SUPCODE AS SupplierId,
    P.BATCHFLG AS BatchFlg,
    P.MANDATEFLG AS ManDateFlg,
    P.EXPDATEFLG AS ExpDateFlg,
    P.EXPDAYS AS ExpDays,
    P.PRICECATFLG AS PriceCatFlg,

    P.ACTIVE AS Active,
    P.STOCK AS Stock,

    P.LOGCD AS LogCd,
    P.UTIME AS UTime,
    P.SECNAME AS SecName,
    P.PRICECATCODE AS PriceCatId,

    P.OPNSTKRATE AS OpnStkRate,
    P.CURCONVRATE AS CurConvRate,
    P.CURCODE AS CurrencyId,

    P.IMGPATH AS ImgPath,
    P.DELFLG AS DelFlg,
    P.POSFLG AS PosFlg,
    P.PRNOTES AS PrNotes,

    P.BMPL AS Bmpl,
    P.BMPQ AS Bmpq,
    P.BMPW AS Bmpw,

    P.BASEMRP AS BaseMrp,
    P.ALTMrp AS AltMrp,

    P.SLNOTRACK AS SlNoTrack,
    P.BMEQN AS BmEqn,
    P.APRVLST AS Aprvlst,
    P.APRVLABY AS AprvlAby,
    P.APRVLADT AS AprvlAdt,
    P.APRVLBBY AS AprvlBby,
    P.APRVLBDT AS AprvlBdt,
    P.APRVLRBY AS AprvlRby,
    P.APRVLRDT AS AprvlRdt,
    P.APRVLJBY AS AprvlJby,
    P.APRVLJDT AS AprvlJdt,
    P.ROWVERSION AS RowVersion
    """;



    public async Task<PagedResponse<ProductResponse>> GetAllAsync(
    int page,
    int pageSize,
    string? search,
    int? groupId,
    bool? active)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        int offset = (page - 1) * pageSize;

        const string where = """
    WHERE ISNULL(P.DELFLG, 0) = 0
      AND (@GroupId IS NULL OR P.GROUPCODE = @GroupId)
      AND (@Active IS NULL OR ISNULL(P.ACTIVE, 0) = @Active)
      AND
      (
          @Search IS NULL
          OR P.PRNAME LIKE @SearchPattern
          OR P.ALIAS LIKE @SearchPattern
          OR P.BARCODE LIKE @SearchPattern
          OR P.UPCCODE LIKE @SearchPattern
          OR P.SKUCODE LIKE @SearchPattern
          OR CAST(P.PRCODE AS NVARCHAR(30)) LIKE @SearchPattern
      )
    """;
        //{ "Invalid column name 'CATCODE'."}
        string? searchTerm = string.IsNullOrWhiteSpace(search)
            ? null
            : search.Trim();

        var parameters = new
        {
            Search = searchTerm,
            SearchPattern = searchTerm == null
                ? null
                : $"%{searchTerm}%",

            GroupId = groupId,
            Active = active,

            Offset = offset,
            PageSize = pageSize
        };

        // ============================================================
        // TOTAL COUNT
        // ============================================================

        string countSql = $"""
    SELECT COUNT(1)
    FROM dbo.PRMAST P
    {where};
    """;

        int totalRecords =
            await _db.QueryFirstOrDefaultAsync<int>(
                countSql,
                parameters);

        // ============================================================
        // PRODUCT MASTER
        // ============================================================

        string dataSql = $"""
    SELECT {SelectColumns}
    FROM dbo.PRMAST P
    {where}
    ORDER BY P.PRNAME ASC, P.PRCODE ASC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
    """;

        var data = (await _db.QueryListAsync<ProductResponse>(
            dataSql,
            parameters)).ToList();

        // ============================================================
        // ALIASES AND PRICE CATEGORIES
        // ============================================================

        if (data.Count > 0)
        {
            var productCodes = data
                .Select(x => x.Id)
                .Distinct()
                .ToArray();

            // PRODUCT ALIASES
            const string aliasSql = """
        SELECT
            CODE AS Id,
            PRCODE AS ProductId,
            ALCODE,
            ALNAME,
            QTY,
            UNITCODE,
            SALESPRICE,
            PURPRICE,
            ALTYPE,
            INVUNITCODE,
            ACTIVE
        FROM dbo.PRALIAS
        WHERE ACTIVE = 1
          AND PRCODE IN @ProductCodes
        ORDER BY PRCODE, CODE;
        """;

            var aliases = await _db.QueryListAsync<ProductAliasResponse>(
                aliasSql,
                new { ProductCodes = productCodes });

            var aliasLookup = aliases
                .GroupBy(x => x.ProductId)
                .ToDictionary(x => x.Key, x => x.ToList());

            // PRODUCT PRICE CATEGORY LEVELS
            const string priceCategorySql = """
        SELECT
            CODE AS Id,
            PRCODE AS ProductId,
            CATCODE,
            CATNAME,
            DISCSTRUCT,
            LEVFROM,
            LEVTO,
            DISCPERC,
            EFFVALUE,
            APPPRICE
        FROM dbo.PRICECATLEV
        WHERE PRCODE IN @ProductCodes
        ORDER BY PRCODE, CODE;
        """;

            var priceCategories =
                await _db.QueryListAsync<ProductPriceCategoryResponse>(
                    priceCategorySql,
                    new { ProductCodes = productCodes });

            var priceCategoryLookup = priceCategories
                .GroupBy(x => x.ProductId)
                .ToDictionary(x => x.Key, x => x.ToList());

            // PRODUCT IMAGES
            const string imageSql = """
        SELECT DISTINCT PRCODE
        FROM dbo.PRIMG
        WHERE PRCODE IN @ProductCodes
          AND NULLIF(PRIMG, '') IS NOT NULL;
        """;

            var imageProductCodes = await _db.QueryListAsync<int>(
                imageSql,
                new { ProductCodes = productCodes });

            var imageLookup = imageProductCodes.ToHashSet();

            // MAP CHILD RECORDS TO PRODUCTS
            foreach (var product in data)
            {
                if (aliasLookup.TryGetValue(product.Id, out var productAliases))
                    product.Aliases = productAliases;

                if (priceCategoryLookup.TryGetValue(
                    product.Id, out var productPriceCategories))
                {
                    product.PriceCategories = productPriceCategories;
                }

                product.Image = imageLookup.Contains(product.Id)
                    ? $"/api/v1/products/{product.Id}/image"
                    : null;
            }
        }

        return new PagedResponse<ProductResponse>
        {
            Data = data,

            Pagination = new PaginationInfo
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = totalRecords
            }
        };
    }


    public async Task<PagedResponse<ProductResponse>> GetAllAsync(
    int page,
    int pageSize,
    string? search)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        int offset = (page - 1) * pageSize;

        const string where = """
        WHERE P.ACTIVE = 1
          AND ISNULL(P.DELFLG, 0) = 0
          AND
          (
              @Search IS NULL
              OR P.PRNAME LIKE @SearchPattern
              OR P.ALIAS LIKE @SearchPattern
              OR P.BARCODE LIKE @SearchPattern
              OR P.UPCCODE LIKE @SearchPattern
              OR P.SKUCODE LIKE @SearchPattern
              OR CAST(P.PRCODE AS NVARCHAR(30)) LIKE @SearchPattern
          )
        """;

        string? searchTerm = string.IsNullOrWhiteSpace(search)? null: search.Trim();

        var parameters = new
        {
            Search = searchTerm,
            SearchPattern = searchTerm == null
                ? null
                : $"%{searchTerm}%",
            Offset = offset,
            PageSize = pageSize
        };
        // ============================================================
        // TOTAL COUNT
        // ============================================================

        string countSql = $"""
        SELECT COUNT(1)
        FROM dbo.PRMAST P
        {where};
        """;

        int totalRecords =
            await _db.QueryFirstOrDefaultAsync<int>(
                countSql,
                parameters);

        // ============================================================
        // PRODUCT MASTER
        // ============================================================

        string dataSql = $"""
        SELECT {SelectColumns}
        FROM dbo.PRMAST P
        {where}
        ORDER BY P.PRNAME ASC, P.PRCODE ASC
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY;
        """;

        var data = (await _db.QueryListAsync<ProductResponse>(
            dataSql,
            parameters)).ToList();

        // ============================================================
        // ALIASES AND PRICE CATEGORIES
        // ============================================================

        if (data.Count > 0)
        {
            var productCodes = data
                .Select(x => x.Id)
                .Distinct()
                .ToArray();

            // --------------------------------------------------------
            // PRODUCT ALIASES
            // --------------------------------------------------------

            const string aliasSql = """
            SELECT
                CODE As Id,
                PRCODE As ProductId,
                ALCODE,
                ALNAME,
                QTY,
                UNITCODE,
                SALESPRICE,
                PURPRICE,
                ALTYPE,
                INVUNITCODE,
                ACTIVE
            FROM dbo.PRALIAS
            WHERE ACTIVE = 1 and PRCODE IN @ProductCodes
            ORDER BY PRCODE, CODE;
            """;

            var aliases = await _db.QueryListAsync<ProductAliasResponse>(
                aliasSql,
                new
                {
                    ProductCodes = productCodes
                });

            var aliasLookup = aliases
                .GroupBy(x => x.ProductId)
                .ToDictionary(
                    x => x.Key,
                    x => x.ToList());

            // --------------------------------------------------------
            // PRODUCT PRICE CATEGORY LEVELS
            // --------------------------------------------------------

            const string priceCategorySql = """
            SELECT
                CODE As Id,
                PRCODE As ProductId,
                CATCODE,
                CATNAME,
                DISCSTRUCT,
                LEVFROM,
                LEVTO,
                DISCPERC,
                EFFVALUE,
                APPPRICE
            FROM dbo.PRICECATLEV
            WHERE PRCODE IN @ProductCodes
            ORDER BY PRCODE, CODE;
            """;

            var priceCategories =
                await _db.QueryListAsync<ProductPriceCategoryResponse>(
                    priceCategorySql,
                    new
                    {
                        ProductCodes = productCodes
                    });

            var priceCategoryLookup = priceCategories
                .GroupBy(x => x.ProductId)
                .ToDictionary(
                    x => x.Key,
                    x => x.ToList());



            // --------------------------------------------------------
            // PRODUCT IMAGES
            // --------------------------------------------------------

            //const string imageSql = """
            //    SELECT
            //        PRCODE AS PrCode,
            //        PRIMG AS Image
            //    FROM dbo.PRIMG
            //    WHERE PRCODE IN @ProductCodes;
            //    """;

            //var images = await _db.QueryListAsync<ProductImageResponse>(
            //    imageSql,
            //    new { ProductCodes = productCodes });

            //var imageLookup = images
            //    .GroupBy(x => x.PrCode)
            //    .ToDictionary(
            //        x => x.Key,
            //        x => x.First().Image);

            // --------------------------------------------------------
            // MAP CHILD RECORDS TO PRODUCTS
            // --------------------------------------------------------
            const string imageSql = """
            SELECT DISTINCT PRCODE
            FROM dbo.PRIMG
            WHERE PRCODE IN @ProductCodes
              AND NULLIF(PRIMG, '') IS NOT NULL;
            """;

            var imageProductCodes = await _db.QueryListAsync<int>(
                imageSql,
                new { ProductCodes = productCodes });

            var imageLookup = imageProductCodes.ToHashSet();
            foreach (var product in data)
            {
                if (aliasLookup.TryGetValue(
                    product.Id,
                    out var productAliases))
                {
                    product.Aliases = productAliases;
                }

                if (priceCategoryLookup.TryGetValue(
                    product.Id,
                    out var productPriceCategories))
                {
                    product.PriceCategories = productPriceCategories;
                }

                product.Image = imageLookup.Contains(product.Id)? $"/api/v1/products/{product.Id}/image" : null;

                //if (imageLookup.TryGetValue(product.PrCode, out var imageBytes))
                //{
                //    product.Image = $"/api/v1/products/{product.PrCode}/image";
                //    //product.Image = imageBytes;
                //}
            }


        }
     

        // ============================================================
        // RESPONSE
        // ============================================================

        return new PagedResponse<ProductResponse>
        {
            Data = data,

            Pagination = new PaginationInfo
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(
                    totalRecords / (double)pageSize)
            }
        };
    }

    // ============================================================
    // GET PRODUCT BY ID
    // ============================================================

    public async Task<ProductResponse?> GetByIdAsync(int id)
    {
        string sql = $"""
            SELECT {SelectColumns}
            FROM dbo.PRMAST P
            WHERE P.PRCODE = @Id
              AND ISNULL(P.DELFLG, 0) = 0;
            """;

        return await _db.QueryFirstOrDefaultAsync<ProductResponse>(
            sql,
            new { Id = id });
    }


    public async Task<ProductResponse?> GetByIdDetailAsync(int id)
    {
        // ============================================================
        // 1. PRODUCT MASTER
        // ============================================================

        string productSql = $"""
        SELECT {SelectColumns}
        FROM dbo.PRMAST P
        WHERE P.PRCODE = @Id
          AND P.ACTIVE = 1
          AND ISNULL(P.DELFLG, 0) = 0;
        """;

        var product = await _db.QueryFirstOrDefaultAsync<ProductResponse>(
            productSql,
            new { Id = id });

        if (product is null)
            return null;

        // ============================================================
        // 2. PRODUCT ALIASES
        // ============================================================

        const string aliasSql = """
        SELECT
            CODE as Id,
            PRCODE as ProductId,
            ALCODE,
            ALNAME,
            QTY,
            UNITCODE As UnitId,
            SALESPRICE,
            PURPRICE,
            ALTYPE,
            INVUNITCODE,
            ACTIVE
        FROM dbo.PRALIAS
        WHERE ACTIVE = 1
          AND PRCODE = @ProductCode
        ORDER BY CODE;
        """;

        var aliases = await _db.QueryListAsync<ProductAliasResponse>(
            aliasSql,
            new { ProductCode = id });

        product.Aliases = aliases.ToList();

        // ============================================================
        // 3. PRODUCT PRICE CATEGORIES
        // ============================================================

        const string priceCategorySql = """
        SELECT
            CODE as Id,
            PRCODE as ProductId,
            CATCODE as categoryId,
            CATNAME,
            DISCSTRUCT,
            LEVFROM,
            LEVTO,
            DISCPERC,
            EFFVALUE,
            APPPRICE
        FROM dbo.PRICECATLEV
        WHERE PRCODE = @ProductCode
        ORDER BY CODE;
        """;

        var priceCategories =
            await _db.QueryListAsync<ProductPriceCategoryResponse>(
                priceCategorySql,
                new { ProductCode = id });

        product.PriceCategories = priceCategories.ToList();

        // ============================================================
        // 4. PRODUCT IMAGE URL
        // ============================================================

        const string imageSql = """
        SELECT CASE
            WHEN EXISTS (
                SELECT 1
                FROM dbo.PRIMG
                WHERE PRCODE = @ProductCode
                  AND NULLIF(PRIMG, '') IS NOT NULL
            )
            THEN 1
            ELSE 0
        END;
        """;

        int hasImage = await _db.QueryFirstOrDefaultAsync<int>(
            imageSql,
            new { ProductCode = id });

        product.Image = hasImage == 1
            ? $"/api/v1/products/{id}/image"
            : null;

        return product;
    }
    // ============================================================
    // CHECK PRODUCT NAME
    // ============================================================

    public async Task<bool> ProductNameExistsAsync(
        string name,
        int excludeId = 0)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM dbo.PRMAST
                WHERE UPPER(LTRIM(RTRIM(PRNAME))) =
                      UPPER(LTRIM(RTRIM(@Name)))
                  AND PRCODE <> @ExcludeId
                  AND ISNULL(DELFLG, 0) = 0
            )
            THEN 1 ELSE 0 END;
            """;

        int result = await _db.QueryFirstOrDefaultAsync<int>(
            sql,
            new
            {
                Name = name.Trim(),
                ExcludeId = excludeId
            });

        return result == 1;
    }

    // ============================================================
    // CHECK PRODUCT ALIAS
    // ============================================================

    public async Task<bool> AliasExistsAsync(
        string alias,
        int excludeProductId = 0)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM dbo.PRMAST
                WHERE UPPER(LTRIM(RTRIM(ALIAS))) =
                      UPPER(LTRIM(RTRIM(@Alias)))
                  AND PRCODE <> @ExcludeProductId
                  AND ISNULL(DELFLG, 0) = 0
            )
            THEN 1 ELSE 0 END;
            """;

        int result = await _db.QueryFirstOrDefaultAsync<int>(
            sql,
            new
            {
                Alias = alias.Trim(),
                ExcludeProductId = excludeProductId
            });

        return result == 1;
    }

    // ============================================================
    // CHECK PRODUCT BARCODE
    // ============================================================

    public async Task<bool> BarcodeExistsAsync(
        string barcode,
        int excludeProductId = 0)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM dbo.PRMAST
                WHERE ACTIVE=1 AND UPPER(LTRIM(RTRIM(BARCODE))) =
                      UPPER(LTRIM(RTRIM(@Barcode)))
                  AND PRCODE <> @ExcludeProductId
                  AND ISNULL(DELFLG, 0) = 0
            )
            THEN 1 ELSE 0 END;
            """;

        int result = await _db.QueryFirstOrDefaultAsync<int>(
            sql,
            new
            {
                Barcode = barcode.Trim(),
                ExcludeProductId = excludeProductId
            });

        return result == 1;
    }

    // ============================================================
    // CHECK PRODUCT USAGE
    // ============================================================

    public async Task<bool> IsProductUsedAsync(int productId)
    {
        const string sql = """
            SELECT CASE WHEN
            (
                EXISTS (SELECT 1 FROM dbo.BOMT WHERE PRCODE = @Id)
                OR EXISTS (SELECT 1 FROM dbo.STKDT WHERE PRCODE = @Id)
                OR EXISTS (SELECT 1 FROM dbo.PRST WHERE PRCODE = @Id)
                OR EXISTS (SELECT 1 FROM dbo.SNTSTK WHERE PRCODE = @Id)
                OR EXISTS (SELECT 1 FROM dbo.CORDT WHERE PRCODE = @Id)
                OR EXISTS (SELECT 1 FROM dbo.PARTYDBCR WHERE PRCODE = @Id)
                OR EXISTS (SELECT 1 FROM dbo.PURT WHERE PRCODE = @Id)
                OR EXISTS (SELECT 1 FROM dbo.PURTPHY WHERE PRCODE = @Id)
            )
            THEN 1 ELSE 0 END;
            """;

        int result = await _db.QueryFirstOrDefaultAsync<int>(
            sql,
            new { Id = productId });

        return result == 1;
    }

    // ============================================================
    // SOFT DELETE
    // ============================================================

    public async Task<int> DeleteAsync(int id, int userId)
    {
        const string sql = """
            UPDATE dbo.PRMAST
            SET
                DELFLG = 1,
                ACTIVE = 0,
                LOGCD = @UserId,
                UTIME = GETDATE()
            WHERE PRCODE = @Id
              AND ISNULL(DELFLG, 0) = 0;
            """;

        return await _db.ExecuteAsync(
            sql,
            new
            {
                Id = id,
                UserId = userId
            });
    }

    public async Task<string?> GetProductImageAsync(int id)
    {
        const string sql = @"
        SELECT PRIMG
        FROM dbo.PRIMG
        WHERE PRCODE = @Id";

        return await _db.QueryFirstOrDefaultAsync<string>(
            sql,
            new { Id = id }
        );
    }


    public async Task<bool> ProductExistsAsync(int id)
    {
        const string sql = @"
        SELECT PRCODE
        FROM dbo.PRMAST
        WHERE ACTIVE=1 AND PRCODE = @Id
          AND ISNULL(DELFLG, 0) = 0";

        int? productCode = await _db.QueryFirstOrDefaultAsync<int?>(
            sql,
            new { Id = id }
        );

        return productCode.HasValue;
    }
    public async Task<bool> ProductImageExistsAsync(int id)
    {
        const string sql = @"
        SELECT CASE
            WHEN EXISTS (
                SELECT 1
                FROM dbo.PRIMG
                WHERE PRCODE = @Id
            )
            THEN 1 ELSE 0
        END";

        int result = await _db.QueryFirstOrDefaultAsync<int>(
            sql,
            new { Id = id }
        );

        return result == 1;
    }


    public async Task<int> InsertProductImageAsync(int id, string base64Image)
    {
        const string sql = @"
        INSERT INTO dbo.PRIMG
        (
            PRCODE,
            PRIMG
        )
        VALUES
        (
            @PrCode,
            @Image
        )";

        return await _db.ExecuteAsync(
            sql,
            new
            {
                PrCode = id,
                Image = base64Image
            }
        );
    }


    public async Task<int> UpdateProductImageAsync(int id, string base64Image)
    {
        const string sql = @"
        UPDATE dbo.PRIMG
        SET PRIMG = @Image
        WHERE PRCODE = @PrCode";

        return await _db.ExecuteAsync(
            sql,
            new
            {
                PrCode = id,
                Image = base64Image
            }
        );
    }
    public async Task<int> DeleteProductImageAsync(int id)
    {
        const string sql = @"
        DELETE FROM dbo.PRIMG
        WHERE PRCODE = @Id";

        return await _db.ExecuteAsync(
            sql,
            new { Id = id }
        );
    }
 

    private async Task InsertProductPriceCategoryAsync(
    SqlConnection connection,
    SqlTransaction transaction,
    int productCode,
    int priceCategoryCode,
    int userId)
    {
        const string sql = """
        INSERT INTO dbo.PRICECATLEV
        (
            PRCODE, CATNAME, DISCSTRUCT,
            LEVFROM, LEVTO, DISCPERC,
            EFFVALUE, LOGCD, UTIME,
            CATCODE, APPPRICE
        )
        SELECT
            @PrCode, CATNAME, DISCSTRUCT,
            LEVFROM, LEVTO, DISCPERC,
            EFFVALUE, @UserId, GETDATE(),
            CODE, APPPRICE
        FROM dbo.PRICECATMAS
        WHERE CODE = @PriceCatCode;
        """;

        int rows = await connection.ExecuteAsync(
            sql,
            new
            {
                PrCode = productCode,
                PriceCatCode = priceCategoryCode,
                UserId = userId
            },
            transaction: transaction,
            commandTimeout: 60);

        if (rows == 0)
        {
            throw new InvalidOperationException(
                "Selected price category does not exist.");
        }
    }



    public async Task<bool> ProductGroupExistsAsync(int id)
    {
        const string sql = """
        SELECT CASE WHEN EXISTS
        (
            SELECT 1 FROM PRGRPMAS
            WHERE ACTIVE=1 AND PRGRPID = @Id
        )
        THEN 1 ELSE 0 END;
        """;

        return await _db.QuerySingleOrDefaultAsync<bool>(
            sql, new { Id = id });
    }

    public async Task<bool> ProductCategoryExistsAsync(int id)
    {
        const string sql = """
        SELECT CASE WHEN EXISTS
        (
            SELECT 1 FROM CATGMAS
            WHERE ACTIVE=1 AND ISNULL(DELFLG,0)=0 AND CATGCODE = @Id
        )
        THEN 1 ELSE 0 END;
        """;

        return await _db.QuerySingleOrDefaultAsync<bool>(
            sql, new { Id = id });
    }

    public async Task<bool> BrandExistsAsync(int id)
    {
        const string sql = """
        SELECT CASE WHEN EXISTS
        (
            SELECT 1 FROM BRANDMAS
            WHERE  ID = @Id
        )
        THEN 1 ELSE 0 END;
        """;

        return await _db.QuerySingleOrDefaultAsync<bool>(
            sql, new { Id = id });
    }

    public async Task<bool> UnitExistsAsync(int id)
    {
        const string sql = """
        SELECT CASE WHEN EXISTS
        (
            SELECT 1 FROM PRUNITMAS
            WHERE ACTIVE=1 AND PRUNITID = @Id
        )
        THEN 1 ELSE 0 END;
        """;

        return await _db.QuerySingleOrDefaultAsync<bool>(
            sql, new { Id = id });
    }

    public async Task<bool> TaxCategoryExistsAsync(int id)
    {
        const string sql = """
        SELECT CASE WHEN EXISTS
        (
            SELECT 1 FROM TAXMAS
            WHERE ACTIVE=1 AND CODE = @Id
        )
        THEN 1 ELSE 0 END;
        """;

        return await _db.QuerySingleOrDefaultAsync<bool>(
            sql, new { Id = id });
    }

    public async Task<bool> PriceCategoryExistsAsync(int id)
    {
        const string sql = """
        SELECT CASE WHEN EXISTS
        (
            SELECT 1 FROM PRICECATMAS
            WHERE  CODE = @Id
        )
        THEN 1 ELSE 0 END;
        """;

        return await _db.QuerySingleOrDefaultAsync<bool>(
            sql, new { Id = id });
    }

    public async Task<bool> AccountExistsAsync(int id)
    {
        const string sql = """
        SELECT CASE WHEN EXISTS
        (
            SELECT 1 FROM dbo.ACCMAS
            WHERE ACTIVE=1 AND ID = @Id
        )
        THEN 1 ELSE 0 END;
        """;

        return await _db.QuerySingleOrDefaultAsync<bool>(
            sql, new { Id = id });
    }

    public async Task<bool> SupplierExistsAsync(int id)
    {
        const string sql = """
        SELECT CASE WHEN EXISTS
        (
            SELECT 1 FROM dbo.SUPMAS
            WHERE ACTIVE=1 AND ID = @Id
        )
        THEN 1 ELSE 0 END;
        """;

        return await _db.QuerySingleOrDefaultAsync<bool>(
            sql, new { Id = id });
    }

    public async Task<bool> GodownExistsAsync(int id)
    {
        const string sql = """
        SELECT CASE WHEN EXISTS
        (
            SELECT 1 FROM dbo.GDWMAS
            WHERE ACTIVE=1 AND GDWID = @Id
        )
        THEN 1 ELSE 0 END;
        """;

        return await _db.QuerySingleOrDefaultAsync<bool>(
            sql, new { Id = id });
    }

    public async Task<bool> CurrencyExistsAsync(int id)
    {
        const string sql = """
        SELECT CASE WHEN EXISTS
        (
            SELECT 1 FROM dbo.CURRENCYMAS
            WHERE CODE = @Id
        )
        THEN 1 ELSE 0 END;
        """;

        return await _db.QuerySingleOrDefaultAsync<bool>(
            sql, new { Id = id });
    }

    public async Task<int> InsertProductMasterAsync(
        ProductRequests request,
        int userId, string? base64Image = null)
    {
        await _db.BeginTransaction();

        try
        {
            const string productSql = """
            INSERT INTO dbo.PRMAST
            (
                PRNAME, ALIAS, PRINTNAME, DESCRIPTION,
                UPCCODE, SKUCODE, BARCODE,
                GROUPCODE, CATGCODE, BRAND,
                UNIT, ALTUNIT, UNITCONV, CONVTYPE,
                OPNSTOCK, OPNSTKVAL,
                PURPRICETYPE, BASEPURPRICE, ALTPURPRICE,
                SALESPRICETYPE, BASESALEPRICE, BASEMINPRICE,
                ALTSALEPRICE, ALTMINPRICE,
                COSTMETH, TAXCODE, SALEACCODE, PURACCODE,
                MINQTY, REORDQTY, MAXQTY,
                SERVFLG, SUPCODE, BATCHFLG, MANDATEFLG,
                EXPDATEFLG, EXPDAYS, PRICECATFLG, ACTIVE,
                STOCK, LOGCD, UTIME, SECNAME, PRICECATCODE,
                OPNSTKRATE, CURCONVRATE, CURCODE, IMGPATH,
                DELFLG, POSFLG, PRNOTES, BMPL, BMPQ, BMPW,
                BASEMRP, ALTMRP, SLNOTRACK, BMEQN, APRVLST
            )
            OUTPUT INSERTED.PRCODE
            VALUES
            (
                @PrName, @Alias, @PrintName, @Description,
                @UpcCode, @SkuCode, @BarCode,
                @GroupId, @CatgId, @Brand,
                @Unit, @AltUnit, @UnitConv, @ConvType,
                @OpnStkQty, @OpnStkValue,
                @PurPriceType, @BasePurPrice, @AltPurPrice,
                @SalesPriceType, @BaseSalePrice, @BaseMinPrice,
                @AltSalePrice, @AltMinPrice,
                @CostMeth, @TaxCatId, @SaleAcId, @PurAcctId,
                @MinQty, @ReOrdQty, @MaxQty,
                @ServFlg, @SupplierId, @BatchFlg, @ManDateFlg,
                @ExpDateFlg, @ExpDays, @PriceCatFlg, @Active,
                @Stock, @UserId, GETDATE(), @SecName, @PriceCatId,
                @OpnStkRate, @CurConvRate, @CurrencyId, '',
                @DelFlg, @PosFlg, @Notes, @BMPL, @BMPQ, @BMPW,
                @BaseMrp, @AltMrp, @SLNOTRACK, @BMEQN, @Aprvlst
            );
            """;

            var parameters = new
            {
                request.PrName,
                request.Alias,
                request.PrintName,
                Description = request.Desc,
                request.UpcCode,
                request.SkuCode,
                request.BarCode,
                request.GroupId,
                request.CatgId,
                Brand = request.BrandId?.ToString(),
                request.Unit,
                request.AltUnit,
                request.UnitConv,
                request.ConvType,
                request.OpnStkQty,
                request.OpnStkValue,
                request.PurPriceType,
                request.BasePurPrice,
                request.AltPurPrice,
                request.SalesPriceType,
                request.BaseSalePrice,
                request.BaseMinPrice,
                request.AltSalePrice,
                request.AltMinPrice,
                request.CostMeth,
                TaxCatId = request.TaxCatId,
                SaleAcId = request.SaleAccId,
                PurAcctId = request.PurAcctId,
                request.MinQty,
                request.ReOrdQty,
                request.MaxQty,
                request.ServFlg,
                SupplierId = request.SupplierId,
                request.BatchFlg,
                ManDateFlg = request.ManDateFlg,
                request.ExpDateFlg,
                request.ExpDays,
                request.PriceCatFlg,
                request.Active,
                request.Stock,
                UserId = userId,
                request.SecName,
                request.PriceCatId,
                request.OpnStkRate,
                CurConvRate = request.CurConvRate,
                CurrencyId = request.CurrencyId,
                //ImgPath = request.ImgPath ?? string.Empty,
                request.DelFlg,
                request.PosFlg,
                Notes = request.Notes,
                BMPL = request.BMPL,
                BMPQ = request.BMPQ,
                BMPW = request.BMPW,
                request.BaseMrp,
                request.AltMrp,
                SLNOTRACK = request.SLNOTRACK,
                BMEQN = request.BMEQN,
                Aprvlst = request.APRVLST
            };

            var connection = _db.TransactionConnections;
            var transaction = _db.ActiveTransaction;

            int productCode = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    productSql,
                    parameters,
                    transaction: transaction,
                    commandTimeout: 60));



            if (productCode <= 0)
            {
                throw new InvalidOperationException(
                    "Product master insertion failed.");
            }




            // ========================================================
            // INSERT PRODUCT IMAGE
            // ========================================================
            if (!string.IsNullOrEmpty(base64Image))
            {
                const string imageSql = """
        INSERT INTO dbo.PRIMG (PRCODE, PRIMG)
        VALUES (@PrCode, @Image);
        """;

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        imageSql,
                        new { PrCode = productCode, Image = base64Image },
                        transaction: transaction,
                        commandTimeout: 60));
            }
            // ========================================================
            // INSERT PRODUCT ALIASES
            // ========================================================

            if (request.Aliases is not null &&
                request.Aliases.Count > 0)
            {
                foreach (var alias in request.Aliases)
                {
                    if (string.IsNullOrWhiteSpace(alias.AlName))
                        continue;

                    const string aliasSql = """
                    INSERT INTO dbo.PRALIAS
                    (
                        PRCODE,
                        ALCODE,
                        ALNAME,
                        QTY,
                        UNITCODE,
                        SALESPRICE,
                        PURPRICE,
                        LOGCD,
                        UTIME,
                        ACTIVE,
                        ALTYPE,
                        INVUNITCODE
                    )
                    OUTPUT INSERTED.CODE
                    VALUES
                    (
                        @PrCode,
                        @AlCode,
                        @AlName,
                        @Qty,
                        @UnitId,
                        @SalesPrice,
                        @PurPrice,
                        @UserId,
                        GETDATE(),
                        @Active,
                        @AlType,
                        @InvUnitCode
                    );
                    """;

                    int aliasCode =
                        await connection.ExecuteScalarAsync<int>(
                            new CommandDefinition(
                                aliasSql,
                                new
                                {
                                    PrCode = productCode,
                                    alias.AlCode,
                                    AlName = alias.AlName.Trim(),
                                    alias.Qty,
                                    alias.UnitId,
                                    alias.SalesPrice,
                                    alias.PurPrice,
                                    UserId = userId,
                                    alias.Active,
                                    alias.AlType,
                                    alias.InvUnitCode
                                },
                                transaction: transaction,
                                commandTimeout: 60));

                    if (aliasCode <= 0)
                    {
                        throw new InvalidOperationException(
                            $"Failed to insert product alias: {alias.AlName}");
                    }
                }
            }

            // Insert selected price category details
            if (request.PriceCatFlg == true && request.PriceCatId > 0)
            {
                await InsertProductPriceCategoryAsync(
                  connection,
                  transaction,
                  productCode,
                  (int)request.PriceCatId,
                  userId);
            }


            //const string stockSql = """
            //INSERT INTO dbo.OPNSTK
            //(
            //    PRCODE,
            //    GDWCODE,
            //    UOM,
            //    QTY,
            //    RATE,
            //    STVALUE,
            //    CONVQTY,
            //    CONVVALUE,
            //    MRP,
            //    STATUS,
            //    SALRATE,
            //    UTIME,
            //    LOGCD,
            //    BRCD,
            //    EXPIRY
            //)
            //VALUES
            //(
            //    @PrCode,
            //    @GdwCode,
            //    @Uom,
            //    @Qty,
            //    @Rate,
            //    @StValue,
            //    @Mrp,
            //    'Saved'
            //    @SalRate,
            //    GETDATE(),
            //    @UserId,
            //    @BrCd,
            //@ExpireDate
            //);
            //""";


            const string stockSql = """
            INSERT INTO dbo.OPNSTK
            (
                PRCODE,
                GDWCODE,
                UOM,
                QTY,
                RATE,
                STVALUE,
                CONVQTY,
                CONVVALUE,
                MRP,
                STATUS,
                SALRATE,
                UTIME,
                LOGCD,
                BRCD,
                EXPIRY
            )
            VALUES
            (
                @PrCode,
                @GdwCode,
                @Uom,
                @Qty,
                @Rate,
                @StValue,
                @ConvQty,
                @ConvValue,
                @Mrp,
                @Status,
                @SalRate,
                GETDATE(),
                @UserId,
                @BrCd,
                @ExpireDate
            );
            """;
            var stockParameters = new
            {
                PrCode = productCode,
                GdwCode = request.GdwnId,
                Uom = request.Unit,

                Qty = request.OpnStkQty,
                Rate = request.OpnStkRate,
                StValue = request.OpnStkValue,

                // Confirm these two mappings against legacy conversion logic.
                ConvQty = request.OpnStkQty,
                ConvValue = request.OpnStkValue,

                Mrp = request.BaseMrp,
                Status = "Saved",
                SalRate = request.BaseSalePrice,

                UserId = userId,
                BrCd = request.BrCd,

                // ExpDays is a number of days, not a DateTime.
                ExpireDate = request.ExpDays.HasValue && request.ExpDays.Value > 0
        ? DateTime.Today.AddDays(request.ExpDays.Value)
        : (DateTime?)null
            };
            //var stockParameters = new
            //{
            //    PrCode = productCode,
            //    GdwCode = request.GdwnId,
            //    Uom = request.Unit,
            //    Qty = request.OpnStkQty,
            //    Rate = request.OpnStkRate,
            //    StValue = request.OpnStkval,
            //    ConvQty = request.OpnStock,
            //    ConValue = request.OpnStkval,
            //    Mrp = request.BaseMrp,
            //    SalRate = request.BaseSalePrice,
            //    UserId = userId,
            //    BrCd = request.BrCd,
            //    ExpireDate=DateTime(request.ExpDays)
            //};

            await connection.ExecuteAsync(
                new CommandDefinition(
                    stockSql,
                    stockParameters,
                    transaction: transaction,
                    commandTimeout: 60));
            _db.CommitTransaction();

            return productCode;
        }
        catch
        {
            _db.RollbackTransaction();
            throw;
        }
    }
 
    public async Task<bool> UpdateProductAsync(
        int id,
        ProductUpdateRequests request,
        int userId,
        string? base64Image = null)
    {
        if (request.RowVersion is null ||
            request.RowVersion.Length != 8)
        {
            throw new ArgumentException(
                "A valid 8-byte RowVersion is required.");
        }

        await _db.BeginTransaction();

        try
        {
            const string productSql = """
            UPDATE dbo.PRMAST
            SET
                PRNAME = @PrName,
                ALIAS = @Alias,
                PRINTNAME = @PrintName,
                DESCRIPTION = @Description,
                UPCCODE = @UpcCode,
                SKUCODE = @SkuCode,
                BARCODE = @BarCode,
                GROUPCODE = @GroupId,
                CATGCODE = @CatgId,
                BRAND = @Brand,
                UNIT = @Unit,
                ALTUNIT = @AltUnit,
                UNITCONV = @UnitConv,
                CONVTYPE = @ConvType,
                OPNSTOCK = @OpnStkQty,
                OPNSTKVAL = @OpnStkValue,
                PURPRICETYPE = @PurPriceType,
                BASEPURPRICE = @BasePurPrice,
                ALTPURPRICE = @AltPurPrice,
                SALESPRICETYPE = @SalesPriceType,
                BASESALEPRICE = @BaseSalePrice,
                BASEMINPRICE = @BaseMinPrice,
                ALTSALEPRICE = @AltSalePrice,
                ALTMINPRICE = @AltMinPrice,
                COSTMETH = @CostMeth,
                TAXCODE = @TaxCatId,
                SALEACCODE = @SaleAccId,
                PURACCODE = @PurAcctId,
                MINQTY = @MinQty,
                REORDQTY = @ReOrdQty,
                MAXQTY = @MaxQty,
                SERVFLG = @ServFlg,
                SUPCODE = @SupplierId,
                BATCHFLG = @BatchFlg,
                MANDATEFLG = @ManDateFlg,
                EXPDATEFLG = @ExpDateFlg,
                EXPDAYS = @ExpDays,
                PRICECATFLG = @PriceCatFlg,
                ACTIVE = @Active,
                STOCK = @Stock,
                SECNAME = @SecName,
                PRICECATCODE = @PriceCatId,
                OPNSTKRATE = @OpnStkRate,
                CURCONVRATE = @CurConvRate,
                CURCODE = @CurrencyId,
                IMGPATH = @ImgPath,
                POSFLG = @PosFlg,
                PRNOTES = @Notes,
                BMPL = @BMPL,
                BMPQ = @BMPQ,
                BMPW = @BMPW,
                BASEMRP = @BaseMrp,
                ALTMRP = @AltMrp,
                SLNOTRACK = @SLNOTRACK,
                BMEQN = @BMEQN,
                APRVLST = @Aprvlst,
                LOGCD = @UserId,
                UTIME = GETDATE()
            WHERE PRCODE = @Id
              AND ISNULL(DELFLG, 0) = 0
              AND ROWVERSION = @RowVersion;
            """;

            var parameters = new
            {
                Id = id,
                UserId = userId,
                RowVersion = request.RowVersion,

                request.PrName,
                request.Alias,
                request.PrintName,
                Description = request.Desc,
                request.UpcCode,
                request.SkuCode,
                request.BarCode,

                request.GroupId,
                request.CatgId,
                Brand = request.BrandId?.ToString(),

                request.Unit,
                request.AltUnit,
                request.UnitConv,
                request.ConvType,

                OpnStkQty = request.OpnStkQty,
                OpnStkValue = request.OpnStkValue,

                request.PurPriceType,
                request.BasePurPrice,
                request.AltPurPrice,
                request.SalesPriceType,
                request.BaseSalePrice,
                request.BaseMinPrice,
                request.AltSalePrice,
                request.AltMinPrice,
                request.CostMeth,

                TaxCatId = request.TaxCatId,
                SaleAccId = request.SaleAccId,
                PurAcctId = request.PurAcctId,

                request.MinQty,
                request.ReOrdQty,
                request.MaxQty,
                request.ServFlg,
                SupplierId = request.SupplierId,
                request.BatchFlg,

                ManDateFlg = request.ManDateFlg,
                request.ExpDateFlg,
                request.ExpDays,
                request.PriceCatFlg,
                request.Active,
                request.Stock,
                request.SecName,

                PriceCatId = request.PriceCatId,
                request.OpnStkRate,

                CurConvRate = request.CurConvRate,
                CurrencyId = request.CurrencyId,
                ImgPath="",
                request.PosFlg,
                Notes = request.Notes,

                BMPL = request.BMPL,
                BMPQ = request.BMPQ,
                BMPW = request.BMPW,

                request.BaseMrp,
                request.AltMrp,

                SLNOTRACK = request.SLNOTRACK,
                BMEQN = request.BMEQN,
                Aprvlst = request.APRVLST
            };

            var connection = _db.TransactionConnections;
            var transaction = _db.ActiveTransaction;

            int rows = await connection.ExecuteAsync(
                new CommandDefinition(
                    productSql,
                    parameters,
                    transaction: transaction,
                    commandTimeout: 60));

            if (rows == 0)
            {
                _db.RollbackTransaction();
                return false;
            }

            // ========================================================
            // UPDATE OR INSERT PRODUCT ALIASES
            // ========================================================

            if (request.Aliases is not null)
            {
                foreach (var alias in request.Aliases)
                {
                    if (string.IsNullOrWhiteSpace(alias.AlName))
                        continue;

                    if (alias.Id > 0)
                    {
                        const string updateAliasSql = """
                        UPDATE dbo.PRALIAS
                        SET
                            PRCODE = @PrCode,
                            ALCODE = @AlCode,
                            ALNAME = @AlName,
                            QTY = @Qty,
                            UNITCODE = @UnitId,
                            SALESPRICE = @SalesPrice,
                            PURPRICE = @PurPrice,
                            LOGCD = @UserId,
                            UTIME = GETDATE(),
                            ACTIVE = @Active,
                            ALTYPE = @AlType,
                            INVUNITCODE = @InvUnitCode
                        WHERE CODE = @Code
                          AND PRCODE = @PrCode;
                        """;

                        int aliasRows = await connection.ExecuteAsync(
                            new CommandDefinition(
                                updateAliasSql,
                                new
                                {
                                    Code = alias.Id,
                                    PrCode = id,
                                    alias.AlCode,
                                    AlName = alias.AlName.Trim(),
                                    alias.Qty,
                                    alias.UnitId,
                                    alias.SalesPrice,
                                    alias.PurPrice,
                                    UserId = userId,
                                    alias.Active,
                                    alias.AlType,
                                    alias.InvUnitCode
                                },
                                transaction: transaction,
                                commandTimeout: 60));

                        if (aliasRows == 0)
                        {
                            throw new InvalidOperationException(
                                $"Product alias {alias.Id} was not found.");
                        }
                    }
                    else
                    {
                        const string insertAliasSql = """
                        INSERT INTO dbo.PRALIAS
                        (
                            PRCODE,
                            ALCODE,
                            ALNAME,
                            QTY,
                            UNITCODE,
                            SALESPRICE,
                            PURPRICE,
                            LOGCD,
                            UTIME,
                            ACTIVE,
                            ALTYPE,
                            INVUNITCODE
                        )
                        OUTPUT INSERTED.CODE
                        VALUES
                        (
                            @PrCode,
                            @AlCode,
                            @AlName,
                            @Qty,
                            @UnitId,
                            @SalesPrice,
                            @PurPrice,
                            @UserId,
                            GETDATE(),
                            @Active,
                            @AlType,
                            @InvUnitCode
                        );
                        """;

                        int aliasCode =
                            await connection.ExecuteScalarAsync<int>(
                                new CommandDefinition(
                                    insertAliasSql,
                                    new
                                    {
                                        PrCode = id,
                                        alias.AlCode,
                                        AlName = alias.AlName.Trim(),
                                        alias.Qty,
                                        alias.UnitId,
                                        alias.SalesPrice,
                                        alias.PurPrice,
                                        UserId = userId,
                                        alias.Active,
                                        alias.AlType,
                                        alias.InvUnitCode
                                    },
                                    transaction: transaction,
                                    commandTimeout: 60));

                        if (aliasCode <= 0)
                        {
                            throw new InvalidOperationException(
                                $"Failed to insert product alias: {alias.AlName}");
                        }
                    }
                }
            }


            // ============================================================
            // UPDATE PRODUCT PRICE CATEGORY
            // ============================================================
            //if (request.PriceCatFlg != true || request.PriceCatId <= 0)
            //{
            //    const string deleteSql = """
            //        DELETE FROM dbo.PRICECATLEV
            //        WHERE PRCODE = @ProductCode;
            //        """;

            //    await _db.ExecuteAsync(deleteSql, new
            //    {

            //        ProductCode = id
            //    });

            //}


            //if (request.PriceCatFlg == true && request.PriceCatId > 0)
            //{
            //    const string checkSql = """
            //    SELECT COUNT(1)
            //    FROM dbo.PRICECATLEV
            //    WHERE PRCODE = @PrCode
            //      AND CATCODE = @PriceCatCode;
            //    """;

            //    int existingCount = await _db.QueryFirstOrDefaultAsync<int>(
            //        checkSql,
            //        new
            //        {
            //            PrCode = id,
            //            PriceCatCode = request.PriceCatId
            //        });

            //    if (existingCount == 0)
            //    {
            //        const string deleteSql = """
            //        DELETE FROM dbo.PRICECATLEV
            //        WHERE PRCODE = @PrCode;
            //        """;

            //        await _db.ExecuteAsync(
            //            deleteSql,
            //            new { PrCode = id });

            //        //await InsertProductPriceCategoryAsync(
            //        //    id,
            //        //    (int)request.PriceCatCode,
            //        //    userId);
            //        await InsertProductPriceCategoryAsync(
            //        connection,
            //        transaction,
            //        id,
            //        (int)request.PriceCatId,
            //        userId);
            //    }

            // ============================================================
            // UPDATE PRODUCT PRICE CATEGORY
            // ============================================================

            if (request.PriceCatFlg != true || request.PriceCatId <= 0)
            {
                const string deleteSql = """
                DELETE FROM dbo.PRICECATLEV
                WHERE PRCODE = @PrCode;
                """;

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        deleteSql,
                        new { PrCode = id },
                        transaction: transaction,
                        commandTimeout: 60));
            }
            else
            {
                const string checkSql = """
                SELECT COUNT(1)
                FROM dbo.PRICECATLEV
                WHERE PRCODE = @PrCode
                  AND CATCODE = @PriceCatCode;
                """;

                int existingCount = await connection.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        checkSql,
                        new
                        {
                            PrCode = id,
                            PriceCatCode = request.PriceCatId
                        },
                        transaction: transaction,
                        commandTimeout: 60));

                if (existingCount == 0)
                {
                    const string deleteSql = """
                    DELETE FROM dbo.PRICECATLEV
                    WHERE PRCODE = @PrCode;
                    """;

                    await connection.ExecuteAsync(
                        new CommandDefinition(
                            deleteSql,
                            new { PrCode = id },
                            transaction: transaction,
                            commandTimeout: 60));

                    await InsertProductPriceCategoryAsync(
                        connection,
                        transaction,
                        id,
                        (int)request.PriceCatId,
                        userId);
                }
            }


            // ============================================================
            // UPDATE / INSERT OPENING STOCK
            // ============================================================

            var stockParameters = new
            {
                PrCode = id,
                GdwCode = request.GdwnId,
                Uom = request.Unit,

                Qty = request.OpnStkQty,
                Rate = request.OpnStkRate,
                StValue = request.OpnStkValue,

                // Keep these mappings only if they match legacy conversion logic.
                ConvQty = request.OpnStkQty,
                ConvValue = request.OpnStkValue,

                Mrp = request.BaseMrp,
                //Status = "Saved",
                SalRate = request.BaseSalePrice,

                UserId = userId,
                BrCd = request.BrCd,

                ExpireDate = request.ExpDateFlg == true &&
                             request.ExpDays.HasValue &&
                             request.ExpDays.Value > 0
                    ? DateTime.Today.AddDays(request.ExpDays.Value)
                    : (DateTime?)null
            };

            const string updateStockSql = """
                UPDATE dbo.OPNSTK
                SET
                    GDWCODE = @GdwCode,
                    UOM = @Uom,
                    QTY = @Qty,
                    RATE = @Rate,
                    STVALUE = @StValue,
                    CONVQTY = @ConvQty,
                    CONVVALUE = @ConvValue,
                    MRP = @Mrp,
                    STATUS = 'Updated',
                    SALRATE = @SalRate,
                    EXPIRY = @ExpireDate,
                    UTIME = GETDATE(),
                    LOGCD = @UserId
                WHERE PRCODE = @PrCode
                  AND
                  (
                      BRCD = @BrCd
                      OR (BRCD IS NULL AND @BrCd IS NULL)
                  );
                """;

            int stockRows = await connection.ExecuteAsync(
                new CommandDefinition(
                    updateStockSql,
                    stockParameters,
                    transaction: transaction,
                    commandTimeout: 60));

            if (stockRows == 0)
            {
                const string insertStockSql = """
                INSERT INTO dbo.OPNSTK
                (
                    PRCODE,
                    GDWCODE,
                    UOM,
                    QTY,
                    RATE,
                    STVALUE,
                    CONVQTY,
                    CONVVALUE,
                    MRP,
                    STATUS,
                    SALRATE,
                    UTIME,
                    LOGCD,
                    BRCD,
                    EXPIRY
                )
                VALUES
                (
                    @PrCode,
                    @GdwCode,
                    @Uom,
                    @Qty,
                    @Rate,
                    @StValue,
                    @ConvQty,
                    @ConvValue,
                    @Mrp,
                    'Saved',
                    @SalRate,
                    GETDATE(),
                    @UserId,
                    @BrCd,
                    @ExpireDate
                );
                """;

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        insertStockSql,
                        stockParameters,
                        transaction: transaction,
                        commandTimeout: 60));
        }

            // ============================================================
            // UPDATE / INSERT PRODUCT IMAGE (only when a new image is sent)
            // ============================================================

            if (!string.IsNullOrEmpty(base64Image))
            {
                const string imageSql = """
        UPDATE dbo.PRIMG
        SET PRIMG = @Image
        WHERE PRCODE = @PrCode;

        IF @@ROWCOUNT = 0
            INSERT INTO dbo.PRIMG (PRCODE, PRIMG)
            VALUES (@PrCode, @Image);
        """;

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        imageSql,
                        new { PrCode = id, Image = base64Image },
                        transaction: transaction,
                        commandTimeout: 60));
            }

            _db.CommitTransaction();

        return true;
    }
        catch
        {
            _db.RollbackTransaction();
            throw;
        }
    }
}
            // ============================================================
            // UPDATE PRODUCT IMAGE
            // ============================================================

            //if (request.ImageFile != null && request.ImageFile.Length > 0)
            //{
            //    // Validate image extension
            //    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

            //    var extension = Path.GetExtension(request.ImageFile.FileName);

            //    if (!allowedExtensions.Contains(
            //        extension,
            //        StringComparer.OrdinalIgnoreCase))
            //    {
            //        throw new InvalidOperationException(
            //            "Only JPG, JPEG and PNG images are allowed.");
            //    }

            //    // Read uploaded image
            //    await using var memoryStream = new MemoryStream();

            //    await request.ImageFile.CopyToAsync(memoryStream);

            //    byte[] imageBytes = memoryStream.ToArray();

            //    // Convert image to Base64
            //    string base64Image = Convert.ToBase64String(imageBytes);

            //    // Check whether image already exists
            //    const string imageExistsSql = """
            //    SELECT CASE WHEN EXISTS
            //    (
            //        SELECT 1
            //        FROM dbo.PRIMG
            //        WHERE PRCODE = @PrCode
            //    )
            //    THEN CAST(1 AS BIT)
            //    ELSE CAST(0 AS BIT)
            //    END;
            //    """;

            //    bool imageExists = await connection.ExecuteScalarAsync<bool>(
            //        new CommandDefinition(
            //            imageExistsSql,
            //            new { PrCode = id },
            //            transaction: transaction,
            //            commandTimeout: 60));

            //    if (imageExists)
            //    {
            //        // Update existing image
            //        const string updateImageSql = """
            //        UPDATE dbo.PRIMG
            //        SET PRIMG = @Image
            //        WHERE PRCODE = @PrCode;
            //        """;

            //        await connection.ExecuteAsync(
            //            new CommandDefinition(
            //                updateImageSql,
            //                new
            //                {
            //                    PrCode = id,
            //                    Image = base64Image
            //                },
            //                transaction: transaction,
            //                commandTimeout: 60));
            //    }
            //    else
            //    {
            //                // Insert new image
            //                const string insertImageSql = """
            //        INSERT INTO dbo.PRIMG (PRCODE, PRIMG)
            //        VALUES (@PrCode, @Image);
            //        """;

            //        await connection.ExecuteAsync(
            //            new CommandDefinition(
            //                insertImageSql,
            //                new
            //                {
            //                    PrCode = id,
            //                    Image = base64Image
            //                },
            //                transaction: transaction,
            //                commandTimeout: 60));
            //    }
            //}
         


//public async Task<bool> UpdateProductAsync(int id,ProductRequests request,int userId)
//{
//    if (request.RowVersion is null || request.RowVersion.Length != 8)
//        throw new ArgumentException("A valid 8-byte RowVersion is required.");

//    const string sql = """
//    UPDATE dbo.PRMAST
//    SET
//        PRNAME = @PrName,
//        ALIAS = @Alias,
//        PRINTNAME = @PrintName,
//        DESCRIPTION = @Description,
//        UPCCODE = @UpcCode,
//        SKUCODE = @SkuCode,
//        BARCODE = @BarCode,
//        GROUPCODE = @GroupCode,
//        CATGCODE = @CatgCode,
//        BRAND = @Brand,
//        UNIT = @Unit,
//        ALTUNIT = @AltUnit,
//        UNITCONV = @UnitConv,
//        CONVTYPE = @ConvType,
//        OPNSTOCK = @OpnStock,
//        OPNSTKVAL = @OpnStkval,
//        PURPRICETYPE = @PurPriceType,
//        BASEPURPRICE = @BasePurPrice,
//        ALTPURPRICE = @AltPurPrice,
//        SALESPRICETYPE = @SalesPriceType,
//        BASESALEPRICE = @BaseSalePrice,
//        BASEMINPRICE = @BaseMinPrice,
//        ALTSALEPRICE = @AltSalePrice,
//        ALTMINPRICE = @AltMinPrice,
//        COSTMETH = @CostMeth,
//        TAXCODE = @TaxCatCode,
//        SALEACCODE = @SaleAcCode,
//        PURACCODE = @PurAcCode,
//        MINQTY = @MinQty,
//        REORDQTY = @ReOrdQty,
//        MAXQTY = @MaxQty,
//        SERVFLG = @ServFlg,
//        SUPCODE = @SupCode,
//        BATCHFLG = @BatchFlg,
//        MANDATEFLG = @ManDateFlg,
//        EXPDATEFLG = @ExpDateFlg,
//        EXPDAYS = @ExpDays,
//        PRICECATFLG = @PriceCatFlg,
//        ACTIVE = @Active,
//        STOCK = @Stock,
//        SECNAME = @SecName,
//        PRICECATCODE = @PriceCatCode,
//        OPNSTKRATE = @OpnStkRate,
//        CURCONVRATE = @CurConvRate,
//        CURCODE = @CurCode,
//        IMGPATH = @ImgPath,
//        POSFLG = @PosFlg,
//        PRNOTES = @Notes,
//        BMPL = @BMPL,
//        BMPQ = @BMPQ,
//        BMPW = @BMPW,
//        BASEMRP = @BaseMrp,
//        ALTMRP = @AltMrp,
//        SLNOTRACK = @SLNOTRACK,
//        BMEQN = @BMEQN,
//        APRVLST = @Aprvlst,
//        LOGCD = @UserId,
//        UTIME = GETDATE()
//    WHERE PRCODE = @Id
//      AND ISNULL(DELFLG, 0) = 0
//      AND ROWVERSION = @RowVersion;
//    """;

//    var parameters = new
//    {
//        Id = id,
//        UserId = userId,
//        RowVersion = request.RowVersion,

//        request.PrName,
//        request.Alias,
//        request.PrintName,
//        Description = request.Desc,
//        request.UpcCode,
//        request.SkuCode,
//        request.BarCode,    
//        request.GroupCode,
//        request.CatgCode,

//        // BRAND is nvarchar in PRMAST, but represents the Brand ID.
//        Brand = request.Brand?.ToString(),

//        request.Unit,
//        request.AltUnit,
//        request.UnitConv,
//        request.ConvType,
//        request.OpnStock,
//        request.OpnStkval,
//        request.PurPriceType,
//        request.BasePurPrice,
//        request.AltPurPrice,
//        request.SalesPriceType,
//        request.BaseSalePrice,
//        request.BaseMinPrice,
//        request.AltSalePrice,
//        request.AltMinPrice,
//        request.CostMeth,
//        request.TaxCatCode,
//        request.SaleAcCode,
//        request.PurAcCode,
//        request.MinQty,
//        request.ReOrdQty,
//        request.MaxQty,
//        request.ServFlg,
//        request.SupCode,
//        request.BatchFlg,
//        ManDateFlg = request.ManDateFlg,
//        request.ExpDateFlg,
//        request.ExpDays,
//        request.PriceCatFlg,
//        request.Active,
//        request.Stock,
//        request.SecName,
//        request.PriceCatCode,
//        request.OpnStkRate,
//        CurConvRate = request.CurConvRate,
//        request.CurCode,
//        ImgPath = request.ImgPath ?? string.Empty,
//        request.PosFlg,
//        Notes = request.Notes,
//        BMPL = request.BMPL,
//        BMPQ = request.BMPQ,
//        BMPW = request.BMPW,
//        request.BaseMrp,
//        request.AltMrp,
//        SLNOTRACK = request.SLNOTRACK,
//        BMEQN = request.BMEQN,
//        Aprvlst = request.APRVLST
//    };

//    int rows = await _db.ExecuteAsync(sql, parameters);

//    return rows > 0;
//}






//namespace QuickBalance.API.Repository.Master
//{
//    using Dapper;
//    using global::QuickBalance.API.CLS;
//    using global::QuickBalance.API.DB;
//    using global::QuickBalance.API.DTOs;
//    using global::QuickBalance.API.DTOs.Product;
//    using global::QuickBalance.API.DTOs.Respons_DTO;
//    using System.Data;

//    public sealed class ProductDB
//    {
//        private readonly DBConn _db;

//        public ProductDB(DBConn db)
//        {
//            _db = db;
//        }

//        private const string SelectColumns = """
//        P.PRCODE AS PrCode,
//        P.PRNAME AS PrName,
//        P.ALIAS AS Alias,
//        P.PRINTNAME AS PrintName,
//        P.DESCRIPTION AS Description,
//        P.UPCCODE AS UpcCode,
//        P.SKUCODE AS SkuCode,
//        P.BARCODE AS BarCode,
//        P.GROUPCODE AS GroupCode,
//        P.CATGCODE AS CatgCode,
//        P.BRAND AS Brand,
//        P.UNIT AS Unit,
//        P.ALTUNIT AS AltUnit,
//        P.UNITCONV AS UnitConv,
//        P.CONVTYPE AS ConvType,
//        P.OPNSTOCK AS OpnStock,
//        P.OPNSTKVAL AS OpnStkval,
//        P.BASEPURPRICE AS BasePurPrice,
//        P.ALTPURPRICE AS AltPurPrice,
//        P.BASESALEPRICE AS BaseSalePrice,
//        P.BASEMINPRICE AS BaseMinPrice,
//        P.ALTSALEPRICE AS AltSalePrice,
//        P.ALTMINPRICE AS AltMinPrice,
//        P.TAXCODE AS TaxCatCode,
//        P.SALEACCODE AS SaleAcCode,
//        P.PURACCODE AS PurAcCode,
//        P.MINQTY AS MinQty,
//        P.REORDQTY AS ReOrdQty,
//        P.MAXQTY AS MaxQty,
//        P.SERVFLG AS ServFlg,
//        P.BATCHFLG AS BatchFlg,
//        P.MANDATEFLG AS ManDateFlg,
//        P.EXPDATEFLG AS ExpDateFlg,
//        P.PRICECATFLG AS PriceCatFlg,
//        P.ACTIVE AS Active,
//        P.DELFLG AS DelFlg,
//        P.STOCK AS Stock,
//        P.SECNAME AS SecName,
//        P.PRICECATCODE AS PriceCatCode,
//        P.OPNSTKRATE AS OpnStkRate,
//        P.IMGPATH AS ImgPath,
//        P.ROWVERSION AS RowVersion
//        """;

//        // ============================================================
//        // GET ALL PRODUCTS
//        // ============================================================

//        public async Task<PagedResponse<ProductResponse>> GetAllAsync(
//            int page,
//            int pageSize,
//            string? search)
//        {
//            page = Math.Max(1, page);
//            pageSize = Math.Clamp(pageSize, 1, 100);

//            var offset = (page - 1) * pageSize;

//            const string where = """
//            WHERE P.ACTIVE = 1
//              AND ISNULL(P.DELFLG, 0) = 0
//              AND
//              (
//                  @Search IS NULL
//                  OR P.PRNAME LIKE @SearchPattern
//                  OR P.ALIAS LIKE @SearchPattern
//                  OR P.BARCODE LIKE @SearchPattern
//                  OR P.UPCCODE LIKE @SearchPattern
//                  OR P.SKUCODE LIKE @SearchPattern
//                  OR CAST(P.PRCODE AS NVARCHAR(30)) LIKE @SearchPattern
//              )
//            """;

//            var parameters = new
//            {
//                Search = string.IsNullOrWhiteSpace(search)
//                    ? null
//                    : search.Trim(),

//                SearchPattern = $"%{search?.Trim()}%",
//                Offset = offset,
//                PageSize = pageSize
//            };

//            var countSql = $"""
//            SELECT COUNT(1)
//            FROM dbo.PRMAST P
//            {where};
//            """;

//            var dataSql = $"""
//            SELECT {SelectColumns}
//            FROM dbo.PRMAST P
//            {where}
//            ORDER BY P.PRNAME ASC, P.PRCODE ASC
//            OFFSET @Offset ROWS
//            FETCH NEXT @PageSize ROWS ONLY;
//            """;

//            var totalRecords =
//                await _db.QueryFirstOrDefaultAsync<int>(
//                    countSql, parameters);

//            var data =
//                await _db.QueryListAsync<ProductResponse>(
//                    dataSql, parameters);

//            return new PagedResponse<ProductResponse>
//            {
//                Data = data,
//                Pagination = new PaginationInfo
//                {
//                    Page = page,
//                    PageSize = pageSize,
//                    TotalRecords = totalRecords,
//                    TotalPages = (int)Math.Ceiling(
//                        totalRecords / (double)pageSize)
//                }
//            };
//        }

//        // ============================================================
//        // GET PRODUCT BY ID
//        // ============================================================

//        public async Task<ProductResponse?> GetByIdAsync(int id)
//        {
//            var sql = $"""
//            SELECT {SelectColumns}
//            FROM dbo.PRMAST P
//            WHERE P.PRCODE = @Id
//              AND ISNULL(P.DELFLG, 0) = 0;
//            """;

//            return await _db.QueryFirstOrDefaultAsync<ProductResponse>(
//                sql, new { Id = id });
//        }

//        // ============================================================
//        // CHECK PRODUCT NAME
//        // ============================================================

//        public async Task<bool> ProductNameExistsAsync(
//            string name,
//            int excludeId = 0)
//        {
//            const string sql = """
//            SELECT CASE WHEN EXISTS
//            (
//                SELECT 1
//                FROM dbo.PRMAST
//                WHERE UPPER(LTRIM(RTRIM(PRNAME))) =
//                      UPPER(LTRIM(RTRIM(@Name)))
//                  AND PRCODE <> @ExcludeId
//                  AND ISNULL(DELFLG, 0) = 0
//            )
//            THEN 1 ELSE 0 END;
//            """;

//            return await _db.QueryFirstOrDefaultAsync<int>(
//                sql,
//                new
//                {
//                    Name = name.Trim(),
//                    ExcludeId = excludeId
//                }) == 1;
//        }

//        // ============================================================
//        // CHECK ALIAS
//        // ============================================================

//        public async Task<bool> AliasExistsAsync(
//            string alias,
//            int excludeProductId = 0)
//        {
//            const string sql = """
//            SELECT CASE WHEN EXISTS
//            (
//                SELECT 1
//                FROM dbo.PRMAST
//                WHERE UPPER(LTRIM(RTRIM(ALIAS))) =
//                      UPPER(LTRIM(RTRIM(@Alias)))
//                  AND PRCODE <> @ExcludeProductId
//                  AND ISNULL(DELFLG, 0) = 0
//            )
//            THEN 1 ELSE 0 END;
//            """;

//            return await _db.QueryFirstOrDefaultAsync<int>(
//                sql,
//                new
//                {
//                    Alias = alias.Trim(),
//                    ExcludeProductId = excludeProductId
//                }) == 1;
//        }

//        // ============================================================
//        // CHECK BARCODE
//        // ============================================================

//        public async Task<bool> BarcodeExistsAsync(
//            string barcode,
//            int excludeProductId = 0)
//        {
//            const string sql = """
//            SELECT CASE WHEN EXISTS
//            (
//                SELECT 1
//                FROM dbo.PRMAST
//                WHERE UPPER(LTRIM(RTRIM(BARCODE))) =
//                      UPPER(LTRIM(RTRIM(@Barcode)))
//                  AND PRCODE <> @ExcludeProductId
//                  AND ISNULL(DELFLG, 0) = 0
//            )
//            THEN 1 ELSE 0 END;
//            """;

//            return await _db.QueryFirstOrDefaultAsync<int>(
//                sql,
//                new
//                {
//                    Barcode = barcode.Trim(),
//                    ExcludeProductId = excludeProductId
//                }) == 1;
//        }

//        // ============================================================
//        // CHECK PRODUCT USAGE
//        // ============================================================

//        public async Task<bool> IsProductUsedAsync(int productId)
//        {
//            const string sql = """
//            SELECT CASE WHEN
//            (
//                EXISTS (SELECT 1 FROM dbo.BOMT WHERE PRCODE = @Id)
//                OR EXISTS (SELECT 1 FROM dbo.STKDT WHERE PRCODE = @Id)
//                OR EXISTS (SELECT 1 FROM dbo.PRST WHERE PRCODE = @Id)
//                OR EXISTS (SELECT 1 FROM dbo.SNTSTK WHERE PRCODE = @Id)
//                OR EXISTS (SELECT 1 FROM dbo.CORDT WHERE PRCODE = @Id)
//                OR EXISTS (SELECT 1 FROM dbo.PARTYDBCR WHERE PRCODE = @Id)
//                OR EXISTS (SELECT 1 FROM dbo.PURT WHERE PRCODE = @Id)
//                OR EXISTS (SELECT 1 FROM dbo.PURTPHY WHERE PRCODE = @Id)
//            )
//            THEN 1 ELSE 0 END;
//            """;

//            return await _db.QueryFirstOrDefaultAsync<int>(
//                sql, new { Id = productId }) == 1;
//        }

//        // ============================================================
//        // SOFT DELETE
//        // ============================================================

//        public async Task<int> DeleteAsync(int id, int userId)
//        {
//            const string sql = """
//            UPDATE dbo.PRMAST
//            SET
//                DELFLG = 1,
//                ACTIVE = 0,
//                LOGCD = @UserId,
//                UTIME = GETDATE()
//            WHERE PRCODE = @Id
//              AND ISNULL(DELFLG, 0) = 0;
//            """;

//            return await _db.ExecuteAsync(sql, new
//            {
//                Id = id,
//                UserId = userId
//            });
//        }



//        public async Task<int> InsertProductMasterAsync(
//      ProductRequests request,
//      int userId)
//        {
//            await _db.BeginTransaction();

//            try
//            {
//                const string sql = """
//            INSERT INTO dbo.PRMAST
//            (
//                PRNAME, ALIAS, PRINTNAME, DESCRIPTION,
//                UPCCODE, SKUCODE, BARCODE,
//                GROUPCODE, CATGCODE, BRAND,
//                UNIT, ALTUNIT, UNITCONV, CONVTYPE,
//                OPNSTOCK, OPNSTKVAL,
//                PURPRICETYPE, BASEPURPRICE, ALTPURPRICE,
//                SALESPRICETYPE, BASESALEPRICE, BASEMINPRICE,
//                ALTSALEPRICE, ALTMINPRICE,
//                COSTMETH, TAXCODE, SALEACCODE, PURACCODE,
//                MINQTY, REORDQTY, MAXQTY,
//                SERVFLG, SUPCODE, BATCHFLG, MANDATEFLG,
//                EXPDATEFLG, EXPDAYS, PRICECATFLG, ACTIVE,
//                STOCK, LOGCD, UTIME, SECNAME, PRICECATCODE,
//                OPNSTKRATE, CURCONVRATE, CURCODE, IMGPATH,
//                DELFLG, POSFLG, PRNOTES, BMPL, BMPQ, BMPW,
//                BASEMRP, ALTMRP, SLNOTRACK, BMEQN, APRVLST
//            )
//            OUTPUT INSERTED.PRCODE
//            VALUES
//            (
//                @PrName, @Alias, @PrintName, @Description,
//                @UpcCode, @SkuCode, @BarCode,
//                @GroupCode, @CatgCode, @Brand,
//                @Unit, @AltUnit, @UnitConv, @ConvType,
//                @OpnStock, @OpnStkval,
//                @PurPriceType, @BasePurPrice, @AltPurPrice,
//                @SalesPriceType, @BaseSalePrice, @BaseMinPrice,
//                @AltSalePrice, @AltMinPrice,
//                @CostMeth, @TaxCatCode, @SaleAcCode, @PurAcCode,
//                @MinQty, @ReOrdQty, @MaxQty,
//                @ServFlg, @SupCode, @BatchFlg, @ManDateFlg,
//                @ExpDateFlg, @ExpDays, @PriceCatFlg, @Active,
//                @Stock, @UserId, GETDATE(), @SecName, @PriceCatCode,
//                @OpnStkRate, @CurConvRate, @CurCode, @ImgPath,
//                @DelFlg, @PosFlg, @Notes, @BMPL, @BMPQ, @BMPW,
//                @BaseMrp, @AltMrp, @SLNOTRACK, @BMEQN, @Aprvlst
//            );
//            """;

//                var parameters = new
//                {
//                    request.PrName,
//                    request.Alias,
//                    request.PrintName,
//                    Description = request.Desc,
//                    request.UpcCode,
//                    request.SkuCode,
//                    request.BarCode,
//                    request.GroupCode,
//                    request.CatgCode,
//                    request.Brand,
//                    request.Unit,
//                    request.AltUnit,
//                    request.UnitConv,
//                    request.ConvType,
//                    request.OpnStock,
//                    request.OpnStkval,
//                    request.PurPriceType,
//                    request.BasePurPrice,
//                    request.AltPurPrice,
//                    request.SalesPriceType,
//                    request.BaseSalePrice,
//                    request.BaseMinPrice,
//                    request.AltSalePrice,
//                    request.AltMinPrice,
//                    request.CostMeth,
//                    TaxCatCode = request.TaxCatCode,
//                    request.SaleAcCode,
//                    request.PurAcCode,
//                    request.MinQty,
//                    request.ReOrdQty,
//                    request.MaxQty,
//                    request.ServFlg,
//                    request.SupCode,
//                    request.BatchFlg,
//                    ManDateFlg = request.ManDateFlg,
//                    request.ExpDateFlg,
//                    request.ExpDays,
//                    request.PriceCatFlg,
//                    request.Active,
//                    request.Stock,
//                    UserId = userId,
//                    request.SecName,
//                    request.PriceCatCode,
//                    request.OpnStkRate,
//                    request.CurConvRate,
//                    request.CurCode,
//                    ImgPath = request.ImgPath ?? string.Empty,
//                    request.DelFlg,
//                    request.PosFlg,
//                    Notes = request.Notes,
//                    BMPL = request.BMPL,
//                    BMPQ = request.BMPQ,
//                    BMPW = request.BMPW,
//                    request.BaseMrp,
//                    request.AltMrp,
//                    SLNOTRACK = request.SLNOTRACK,
//                    BMEQN = request.BMEQN,
//                    Aprvlst = request.APRVLST // APRVLST
//                };

//                // Get the connection that owns the active transaction.
//                var connection = _db.TransactionConnections;

//                var transaction = _db.ActiveTransaction;

//                int productCode = await connection.ExecuteScalarAsync<int>(
//                    new CommandDefinition(
//                        sql,
//                        parameters,
//                        transaction: transaction,
//                        commandTimeout: 60));

//                if (productCode <= 0)
//                {
//                    throw new InvalidOperationException(
//                        "Product master insertion failed.");
//                }

//                _db.CommitTransaction();

//                return productCode;
//            }
//            catch
//            {
//                _db.RollbackTransaction();
//                throw;
//            }
//        }


//    }
//}




//Without transaction: Use _db.ExecuteAsync(), _db.QueryFirstOrDefaultAsync(), _db.QueryListAsync(), etc.



//With transaction: Use _db.BeginTransaction() and execute every SQL command through _db.TransactionConnections with _db.ActiveTransaction.