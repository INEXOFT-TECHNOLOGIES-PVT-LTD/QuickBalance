using QuickBalance.API.CLS;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs.Respons_DTO;
using QuickBalance.API.DTOs.Tax;

namespace QuickBalance.API.Repository.Master;

public sealed class TaxMasDB
{
    private readonly DBConn _db;

    public TaxMasDB(DBConn db)
    {
        _db = db;
    }

    private const string SelectColumns = @"
        CODE AS Id,
        TAXNAME AS TaxName,
        TAXPERC AS TaxPercentage,
        ACTIVE AS IsActive,
        LOGCD AS UserId,
        PURCODE AS PurchaseAccountId,
        PRTNCODE AS PurchaseReturnAccountId,
        SALCODE AS SalesAccountId,
        SRTNCODE AS SalesReturnAccountId,
        UTIME AS UpdatedAt,
        VATCODE AS VatAccountId,
        PTAXCODE AS TaxAccountId,
        SECNAME AS SecondName,
        TTYPE AS TaxType
    ";

    // =========================================================
    // INSERT
    // =========================================================

    public async Task<int> InsertAsync(
        TaxMasterRequest request, int userId)
    {
        const string query = @"
            INSERT INTO dbo.TAXMAS
            (
                TAXNAME,
                TAXPERC,
                ACTIVE,
                LOGCD,
                PURCODE,
                PRTNCODE,
                SALCODE,
                SRTNCODE,
                UTIME,
                VATCODE,
                PTAXCODE,
                SECNAME,
                TTYPE
            )
            OUTPUT INSERTED.CODE
            VALUES
            (
                @TaxName,
                @TaxPercentage,
                @IsActive,
                @UserId,
                @PurchaseAccountId,
                @PurchaseReturnAccountId,
                @SalesAccountId,
                @SalesReturnAccountId,
                GETDATE(),
                @VatAccountId,
                @TaxAccountId,
                @SecondName,
                @TaxType
            );";

        return await _db.QueryFirstOrDefaultAsync<int>(
            query,
            new
            {
                request.TaxName,
                request.TaxPercentage,
                request.IsActive,
                UserId = userId,
                // Account IDs are validated (> 0) by TaxService before this call.
                PurchaseAccountId = request.PurchaseAccountId,
                PurchaseReturnAccountId = request.PurchaseReturnAccountId,
                SalesAccountId = request.SalesAccountId,
                SalesReturnAccountId = request.SalesReturnAccountId,
                VatAccountId = request.VatAccountId,
                TaxAccountId = request.TaxAccountId,
                SecondName = request.SecondName ?? string.Empty,
                request.TaxType
            });
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<TaxMasterResponse?> GetByIdAsync(int id)
    {
        string query = $@"
            SELECT {SelectColumns}
            FROM dbo.TAXMAS
            WHERE ISNULL(DELFLG, 0) = 0 AND CODE = @Id;";

        return await _db.QueryFirstOrDefaultAsync<TaxMasterResponse>(
            query, new { Id = id });
    }

    // =========================================================
    // GET ALL - PAGINATED
    // =========================================================

    public async Task<PagedResponse<TaxMasterResponse>> GetAllAsync(
        int page, int pageSize, string? search, bool? active = null)
    {
        const string where = @"
            WHERE ISNULL(DELFLG, 0) = 0
              AND (@Active IS NULL OR ISNULL(ACTIVE, 0) = @Active)
              AND (
                @Search IS NULL
                OR TAXNAME LIKE @Search
                OR SECNAME LIKE @Search
                OR TTYPE LIKE @Search
                OR CAST(CODE AS NVARCHAR(20)) LIKE @Search
                OR CAST(TAXPERC AS NVARCHAR(30)) LIKE @Search
              )";

        string countQuery = $@"
            SELECT COUNT(*)
            FROM dbo.TAXMAS
            {where};";

        string dataQuery = $@"
            SELECT {SelectColumns}
            FROM dbo.TAXMAS
            {where}
            ORDER BY TAXNAME, CODE
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;";

        string? searchValue = string.IsNullOrWhiteSpace(search)
            ? null
            : $"%{search.Trim()}%";

        var parameters = new
        {
            Search = searchValue,
            Active = active,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        };

        int totalRecords =
            await _db.QueryFirstOrDefaultAsync<int>(
                countQuery, parameters);

        var data = await _db.QueryListAsync<TaxMasterResponse>(
            dataQuery, parameters);

        int totalPages = totalRecords == 0
            ? 0
            : (int)Math.Ceiling(
                totalRecords / (double)pageSize);

        return new PagedResponse<TaxMasterResponse>
        {
            Data = data.ToList(),
            Pagination = new PaginationInfo
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = totalPages
            }
        };
    }

    // =========================================================
    // DUPLICATE TAX NAME
    // =========================================================

    public async Task<bool> NameExistsAsync(
        string taxName, int? excludeId = null)
    {
        const string query = @"
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM dbo.TAXMAS
                WHERE TAXNAME = @TaxName
               AND ACTIVE=1   AND (@ExcludeId IS NULL OR CODE <> @ExcludeId)
            )
            THEN 1 ELSE 0 END;";

        return await _db.QueryFirstOrDefaultAsync<bool>(
            query,
            new { TaxName = taxName, ExcludeId = excludeId });
    }

    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<int> UpdateAsync(
        int id, TaxMasterRequest request, int userId)
    {
        const string query = @"
            UPDATE dbo.TAXMAS
            SET
                TAXNAME = @TaxName,
                TAXPERC = @TaxPercentage,
                ACTIVE = @IsActive,
                LOGCD = @UserId,
                PURCODE = @PurchaseAccountId,
                PRTNCODE = @PurchaseReturnAccountId,
                SALCODE = @SalesAccountId,
                SRTNCODE = @SalesReturnAccountId,
                UTIME = GETDATE(),
                VATCODE = @VatAccountId,
                PTAXCODE = @TaxAccountId,
                SECNAME = @SecondName,
                TTYPE = @TaxType
            WHERE CODE = @Id;";

        return await _db.ExecuteAsync(
            query,
            new
            {
                Id = id,
                request.TaxName,
                request.TaxPercentage,
                request.IsActive,
                UserId = userId,
                // Account IDs are validated (> 0) by TaxService before this call.
                PurchaseAccountId = request.PurchaseAccountId,
                PurchaseReturnAccountId = request.PurchaseReturnAccountId,
                SalesAccountId = request.SalesAccountId,
                SalesReturnAccountId = request.SalesReturnAccountId,
                VatAccountId = request.VatAccountId,
                TaxAccountId = request.TaxAccountId,
                SecondName = request.SecondName ?? string.Empty,
                request.TaxType
            });
    }



    public async Task<bool> DeleteAsync(int Id, int userId)
    {
        const string sql = @"
                UPDATE dbo.TAXMAS
                SET
                    DELFLG = 1,
                    ACTIVE = 0,
                    LOGCD = @UserId,
                    UTIME = GETDATE()
                WHERE CODE = @Id
                  AND ISNULL(DELFLG, 0) = 0";

        var rowsAffected = await _db.ExecuteAsync(
            sql,
            new
            {
                Id = Id,
                UserId = userId
            });
        return rowsAffected > 0;

    }
    // =========================================================
    // SEARCH TAX GROUPS
    // =========================================================

    public async Task<List<TaxLookupResponse>> SearchAsync(
        string? search)
    {
        const string query = @"
            SELECT
                CODE AS Id,
                TAXNAME AS TaxName
            FROM dbo.TAXMAS
            WHERE ACTIVE=1 AND @Search IS NULL OR TAXNAME LIKE @Search
            ORDER BY TAXNAME;";

        string? searchValue = string.IsNullOrWhiteSpace(search)
            ? null
            : $"%{search.Trim()}%";

        var result = await _db.QueryListAsync<TaxLookupResponse>(
            query, new { Search = searchValue });

        return result.ToList();
    }

    // =========================================================
    // ACTIVE TAXES FOR ITEM MASTER
    // =========================================================

    public async Task<List<TaxLookupResponse>>
        GetActiveForItemMasterAsync()
    {
        const string query = @"
            SELECT
                CODE AS Id,
                TAXNAME AS TaxName
            FROM dbo.TAXMAS
            WHERE ACTIVE = 1
              AND TTYPE <> 'E'
              AND TTYPE <> 'UR'
            ORDER BY TAXNAME;";

        var result =
            await _db.QueryListAsync<TaxLookupResponse>(query);

        return result.ToList();
    }

    // =========================================================
    // ACTIVE ALL TAXES
    // =========================================================

    public async Task<List<TaxLookupResponse>> GetActiveAllAsync()
    {
        const string query = @"
            SELECT
                CODE AS Id,
                TAXNAME AS TaxName
            FROM dbo.TAXMAS
            WHERE ACTIVE = 1
            ORDER BY TAXNAME;";

        var result =
            await _db.QueryListAsync<TaxLookupResponse>(query);

        return result.ToList();
    }

    // =========================================================
    // TAX PERCENTAGES
    // =========================================================

    public async Task<List<TaxPercentageResponse>>
        GetPercentagesAsync()
    {
        const string query = @"
            SELECT TAXPERC AS TaxPercentage
            FROM dbo.TAXMAS
            WHERE ACTIVE=1
            ORDER BY TAXPERC;";

        var result =
            await _db.QueryListAsync<TaxPercentageResponse>(query);

        return result.ToList();
    }

    // =========================================================
    // TAX DETAILS BY PERCENTAGE
    // =========================================================

    public async Task<List<TaxMasterResponse>>
        GetDetailsByPercentageAsync(decimal percentage)
    {
        string query = $@"
            SELECT {SelectColumns}
            FROM dbo.TAXMAS
            WHERE TAXPERC = @Percentage
              AND ACTIVE = 1;";

        var result = await _db.QueryListAsync<TaxMasterResponse>(
            query, new { Percentage = percentage });

        return result.ToList();
    }

    // =========================================================
    // TAX DETAILS BY NAME AND PERCENTAGE
    // =========================================================

    public async Task<List<TaxMasterResponse>>
        GetDetailsByNameAndPercentageAsync(
            string taxName, decimal percentage)
    {
        string query = $@"
            SELECT {SelectColumns}
            FROM dbo.TAXMAS
            WHERE  ACTIVE=1 AND TAXPERC = @Percentage
              AND TAXNAME = @TaxName;";

        var result = await _db.QueryListAsync<TaxMasterResponse>(
            query,
            new
            {
                TaxName = taxName,
                Percentage = percentage
            });

        return result.ToList();
    }

    // =========================================================
    // ACTIVE TAXES BY TYPE
    // =========================================================

    public async Task<List<TaxMasterResponse>> GetActiveByTypeAsync(
        string taxType)
    {
        string query = $@"
            SELECT {SelectColumns}
            FROM dbo.TAXMAS
            WHERE ACTIVE = 1
              AND TTYPE = @TaxType
            ORDER BY TAXNAME;";

        var result = await _db.QueryListAsync<TaxMasterResponse>(
            query, new { TaxType = taxType });

        return result.ToList();
    }

    // =========================================================
    // VALIDATE ACCOUNT
    // =========================================================

    public async Task<List<TaxMasterResponse>>
        ValidateAccountAsync(int accountId)
    {
        string query = $@"
            SELECT {SelectColumns}
            FROM dbo.TAXMAS
            WHERE ACTIVE=1 AND PURCODE = @AccountId
               OR PRTNCODE = @AccountId
               OR SALCODE = @AccountId
               OR SRTNCODE = @AccountId
               OR VATCODE = @AccountId
               OR PTAXCODE = @AccountId;";

        var result = await _db.QueryListAsync<TaxMasterResponse>(
            query, new { AccountId = accountId });

        return result.ToList();
    }

    // =========================================================
    // GET EXEMPTED TAX ID
    // =========================================================

    public async Task<int> GetExemptedTaxIdAsync()
    {
        const string query = @"
            SELECT TOP 1 CODE
            FROM dbo.TAXMAS
            WHERE ACTIVE=1 AND TAXNAME = 'Exempted';";

        return await _db.QueryFirstOrDefaultAsync<int>(query);
    }

    // =========================================================
    // UPDATE DEFAULT TAX
    // =========================================================

    public async Task<int> UpdateDefaultTaxAsync(
        int id, string taxName, decimal percentage)
    {
        const string query = @"
            UPDATE dbo.TAXMAS
            SET
                TAXNAME = @TaxName,
                TAXPERC = @Percentage
            WHERE CODE = @Id;";

        return await _db.ExecuteAsync(
            query,
            new
            {
                Id = id,
                TaxName = taxName,
                Percentage = percentage
            });
    }
}