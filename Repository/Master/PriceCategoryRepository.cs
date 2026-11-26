
using QuickBalance.API.CLS;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.PriceCategory;
using QuickBalance.API.DTOs.Respons_DTO;

namespace QuickBalance.API.Repository.Master;

public sealed class PriceCategoryRepository
{
    private readonly DBConn _db;

    public PriceCategoryRepository(DBConn db)
    {
        _db = db;
    }

    // CREATE
    public async Task<int> InsertAsync(
        PriceCategoryRequest request,
        int userId)
    {
        const string codeQuery = """
            SELECT ISNULL(MAX(CODE), 0) + 1
            FROM dbo.PRICECATMAS;
            """;

        int code = await _db.QueryFirstOrDefaultAsync<int>(codeQuery);

        const string insertQuery = """
            INSERT INTO dbo.PRICECATMAS
            (
                CODE,
                CATNAME,
                DISCSTRUCT,
                LEVFROM,
                LEVTO,
                DISCPERC,
                EFFVALUE,
                LOGCD,
                UTIME,
                APPPRICE
            )
            VALUES
            (
                @Code,
                @CategoryName,
                @DiscountStructure,
                @LevelFrom,
                @LevelTo,
                @DiscountPercent,
                @EffectiveValue,
                @UserId,
                GETDATE(),
                @AppliedPrice
            );
            """;

        try
        {
            foreach (var level in request.Levels)
            {
                int rows = await _db.ExecuteAsync(
                    insertQuery,
                    new
                    {
                        Code = code,
                        CategoryName = request.CategoryName.Trim(),
                        DiscountStructure = level.DiscountStructure.Trim(),
                        level.LevelFrom,
                        level.LevelTo,
                        level.DiscountPercent,
                        level.EffectiveValue,
                        UserId = userId,
                        level.AppliedPrice
                    });

                if (rows <= 0)
                    throw new InvalidOperationException(
                        "Failed to insert price category level.");
            }

            return code;
        }
        catch
        {
            await DeleteByCodeAsync(code);
            throw;
        }
    }

    // GET ALL - PAGINATION
    public async Task<PagedResponse<PriceCategoryResponse>> GetAllAsync(
        int page,
        int pageSize,
        string? search)
    {
        const string countQuery = """
            SELECT COUNT(DISTINCT CODE)
            FROM dbo.PRICECATMAS
            WHERE ISNULL(DELFLG, 0) = 0
              AND (
                @Search IS NULL
                OR CATNAME LIKE @Search
              );
            """;

        const string categoryQuery = """
            SELECT
                CODE AS Id,
                MAX(CATNAME) AS CategoryName
            FROM dbo.PRICECATMAS
            WHERE ISNULL(DELFLG, 0) = 0
              AND (
                @Search IS NULL
                OR CATNAME LIKE @Search
              )
            GROUP BY CODE
            ORDER BY MAX(CATNAME), CODE
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;
            """;

        string? searchPattern = string.IsNullOrWhiteSpace(search)
            ? null
            : $"%{search.Trim()}%";

        var parameters = new
        {
            Search = searchPattern,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        };

        int totalRecords =
            await _db.QueryFirstOrDefaultAsync<int>(
                countQuery, parameters);

        var categories =
            await _db.QueryListAsync<PriceCategoryResponse>(
                categoryQuery, parameters);

        foreach (var category in categories)
        {
            category.Levels = await GetLevelsAsync(category.Id);
        }

        return new PagedResponse<PriceCategoryResponse>
        {
            Data = categories,
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

    // GET BY ID
    public async Task<PriceCategoryResponse?> GetByIdAsync(int id)
    {
        const string query = """
            SELECT
                CODE AS Id,
                MAX(CATNAME) AS CategoryName
            FROM dbo.PRICECATMAS
            WHERE CODE = @Id
              AND ISNULL(DELFLG, 0) = 0
            GROUP BY CODE;
            """;

        var category =
            await _db.QueryFirstOrDefaultAsync<PriceCategoryResponse>(
                query, new { Id = id });

        if (category is null)
            return null;

        category.Levels = await GetLevelsAsync(id);

        return category;
    }

    // GET CATEGORY LEVELS
    private async Task<IReadOnlyList<PriceCategoryLevelResponse>>
        GetLevelsAsync(int code)
    {
        const string query = """
            SELECT
                CODE AS Id,
                CATNAME AS CategoryName,
                DISCSTRUCT AS DiscountStructure,
                LEVFROM AS LevelFrom,
                LEVTO AS LevelTo,
                DISCPERC AS DiscountPercent,
                EFFVALUE AS EffectiveValue,
                APPPRICE AS AppliedPrice
            FROM dbo.PRICECATMAS
            WHERE CODE = @Code
              AND ISNULL(DELFLG, 0) = 0
            ORDER BY LEVFROM, LEVTO;
            """;

        return await _db.QueryListAsync<PriceCategoryLevelResponse>(
            query, new { Code = code });
    }

    // CHECK DUPLICATE NAME
    public async Task<bool> NameExistsAsync(
        string categoryName,
        int? excludeId = null)
    {
        const string query = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM dbo.PRICECATMAS
                WHERE CATNAME = @CategoryName
                  AND ISNULL(DELFLG, 0) = 0
                  AND (@ExcludeId IS NULL OR CODE <> @ExcludeId)
            )
            THEN 1 ELSE 0 END;
            """;

        int result = await _db.QueryFirstOrDefaultAsync<int>(
            query,
            new
            {
                CategoryName = categoryName.Trim(),
                ExcludeId = excludeId
            });

        return result == 1;
    }

    // UPDATE
    public async Task<int> UpdateAsync(
        int id,
        PriceCategoryRequest request,
        int userId)
    {
        // Legacy behaviour: remove all old levels and insert
        // the submitted levels again under the same category code.

        await DeleteByCodeAsync(id);

        const string insertQuery = """
            INSERT INTO dbo.PRICECATMAS
            (
                CODE,
                CATNAME,
                DISCSTRUCT,
                LEVFROM,
                LEVTO,
                DISCPERC,
                EFFVALUE,
                LOGCD,
                UTIME,
                APPPRICE
            )
            VALUES
            (
                @Code,
                @CategoryName,
                @DiscountStructure,
                @LevelFrom,
                @LevelTo,
                @DiscountPercent,
                @EffectiveValue,
                @UserId,
                GETDATE(),
                @AppliedPrice
            );
            """;

        try
        {
            int totalRows = 0;

            foreach (var level in request.Levels)
            {
                int rows = await _db.ExecuteAsync(
                    insertQuery,
                    new
                    {
                        Code = id,
                        CategoryName = request.CategoryName.Trim(),
                        DiscountStructure = level.DiscountStructure.Trim(),
                        level.LevelFrom,
                        level.LevelTo,
                        level.DiscountPercent,
                        level.EffectiveValue,
                        UserId = userId,
                        level.AppliedPrice
                    });

                if (rows <= 0)
                    throw new InvalidOperationException(
                        "Failed to insert updated price category level.");

                totalRows += rows;
            }

            return totalRows;
        }
        catch
        {
            await DeleteByCodeAsync(id);
            throw;
        }
    }

    // DELETE
    public async Task<int> DeleteAsync(int id, int userId)
    {
        const string query = """
            UPDATE dbo.PRICECATMAS
            SET DELFLG = 1,
                LOGCD = @UserId,
                UTIME = GETDATE()
            WHERE CODE = @Id
              AND ISNULL(DELFLG, 0) = 0;
            """;

        return await _db.ExecuteAsync(query, new { Id = id, UserId = userId });
    }

    private async Task<int> DeleteByCodeAsync(int code)
    {
        const string query = """
            DELETE FROM dbo.PRICECATMAS
            WHERE CODE = @Code;
            """;

        return await _db.ExecuteAsync(query, new { Code = code });
    }
}
