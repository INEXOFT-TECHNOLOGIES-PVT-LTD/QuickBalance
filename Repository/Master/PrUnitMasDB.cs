using QuickBalance.API.CLS;
using QuickBalance.API.Configuration;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Respons_DTO;

namespace QuickBalance.API.Repository.Master;

public sealed class PrUnitMasDB
{
    private readonly DatabaseConnection _databaseConnection;

    public PrUnitMasDB(DatabaseConnection databaseConnection)
    {
        _databaseConnection = databaseConnection;
    }

    // =========================================================
    // CREATE USER-SPECIFIC DATABASE CONNECTION
    // =========================================================

    private async Task<DBConn> GetDatabaseAsync(int userId)
    {
        LngTB.InitLng();

        Variable variables = new()
        {
            UserId = userId
        };

        string connectionString =
            _databaseConnection.GetConnectionString(variables);

        DBConn db = new(connectionString);

        CommonFunction commonFunction = new(db);

        bool connected =
            await commonFunction.CheckDatabaseConnection();

        if (!connected)
            throw new InvalidOperationException(
                "Database connection failed.");

        await commonFunction.SetVariableValues(variables);

        return db;
    }

    // =========================================================
    // INSERT
    // =========================================================

    public async Task<int> InsertAsync(
        UnitMasterRequest request, int userId)
    {
        var db = await GetDatabaseAsync(userId);

        const string query = @"
            INSERT INTO dbo.PRUNITMAS
            (
                PRUNITNAME,
                ACTIVE,
                SECNAME,
                FORMALNAME
            )
            OUTPUT INSERTED.PRUNITID
            VALUES
            (
                @UnitName,
                @IsActive,
                @SecondName,
                @FormalName
            );";

        return await db.QueryFirstOrDefaultAsync<int>(
            query,
            new
            {
                request.UnitName,
                request.IsActive,
                SecondName = request.SecondName ?? string.Empty,
                FormalName = request.FormalName ?? string.Empty
            });
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<UnitMasterResponse?> GetByIdAsync(
        int id, int userId)
    {
        var db = await GetDatabaseAsync(userId);

        const string query = @"
            SELECT
                PRUNITID AS Id,
                PRUNITNAME AS UnitName,
                SECNAME AS SecondName,
                FORMALNAME AS FormalName,
                ACTIVE as IsActive
             
            FROM dbo.PRUNITMAS
            WHERE ISNULL(DELFLG, 0) = 0 AND PRUNITID = @Id;";

        return await db.QueryFirstOrDefaultAsync<UnitMasterResponse>(
            query,
            new { Id = id });
    }


    public async Task<bool> DeleteAsync(int Id, int userId)
    {
        var db = await GetDatabaseAsync(userId);

        const string sql = @"
                UPDATE dbo.PRUNITMAS
                SET
                    DELFLG = 1,
                    ACTIVE = 0,
                    LOGCD = @UserId,
                    UTIME = GETDATE()
                WHERE PRUNITID = @Id
                  AND ISNULL(DELFLG, 0) = 0";

        var rowsAffected = await db.ExecuteAsync(
            sql,
            new
            {
                Id = Id,
                UserId = userId
            });
        return rowsAffected > 0;

    }
    // =========================================================
    // GET ALL - PAGINATION AND SEARCH
    // =========================================================

    public async Task<PagedResponse<UnitMasterResponse>> GetAllAsync(
        int page,
        int pageSize,
        string? search,
        int userId,
        bool? active = null)
    {
        var db = await GetDatabaseAsync(userId);

        const string where = @"
        WHERE ISNULL(DELFLG,0)=0
          AND (@Active IS NULL OR ISNULL(ACTIVE, 0) = @Active)
          AND (
                @Search IS NULL
                OR PRUNITNAME LIKE @Search
                OR SECNAME LIKE @Search
                OR FORMALNAME LIKE @Search
                OR CAST(PRUNITID AS NVARCHAR(20)) LIKE @Search
          )";
        string countQuery = $@"
            SELECT COUNT(*)
            FROM dbo.PRUNITMAS
            {where};";

        string dataQuery = $@"
            SELECT
                PRUNITID AS Id,
                PRUNITNAME AS UnitName,
                SECNAME AS SecondName,
                FORMALNAME AS FormalName,
                ACTIVE As IsActive

            FROM dbo.PRUNITMAS
            {where}
            ORDER BY PRUNITNAME, PRUNITID
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;";
        //    CASE
        //WHEN ACTIVE = 1 THEN 'YES'
        //                ELSE 'NO'
        //            END AS IsActive
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
            await db.QueryFirstOrDefaultAsync<int>(
                countQuery, parameters);

        var data = await db.QueryListAsync<UnitMasterResponse>(
            dataQuery, parameters);

        int totalPages = totalRecords == 0
            ? 0
            : (int)Math.Ceiling(
                totalRecords / (double)pageSize);

        return new PagedResponse<UnitMasterResponse>
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
    // CHECK DUPLICATE UNIT NAME
    // =========================================================

    public async Task<bool> NameExistsAsync(
        string unitName,
        int userId,
        int? excludeUnitId = null)
    {
        var db = await GetDatabaseAsync(userId);

        const string query = @"
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM dbo.PRUNITMAS
                WHERE ACTIVE=1 AND PRUNITNAME = @UnitName
                  AND (@ExcludeUnitId IS NULL
                       OR PRUNITID <> @ExcludeUnitId)
            )
            THEN 1 ELSE 0 END;";

        return await db.QueryFirstOrDefaultAsync<bool>(
            query,
            new
            {
                UnitName = unitName,
                ExcludeUnitId = excludeUnitId
            });
    }

    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<int> UpdateAsync(
        int id,
        UnitMasterRequest request,
        int userId)
    {
        var db = await GetDatabaseAsync(userId);

        const string query = @"
            UPDATE dbo.PRUNITMAS
            SET
                PRUNITNAME = @UnitName,
                ACTIVE = @IsActive,
                SECNAME = @SecondName,
                FORMALNAME = @FormalName
            WHERE PRUNITID = @Id;";

        return await db.ExecuteAsync(
            query,
            new
            {
                Id = id,
                request.UnitName,
                request.IsActive,
                SecondName = request.SecondName ?? string.Empty,
                FormalName = request.FormalName ?? string.Empty
            });
    }





    // =========================================================
    // ACTIVE UNITS
    // =========================================================

    public async Task<List<UnitDropdownResponse>> GetActiveAsync(
        int userId)
    {
        var db = await GetDatabaseAsync(userId);

        const string query = @"
            SELECT
                PRUNITID AS Id,
                PRUNITNAME AS UnitName
            FROM dbo.PRUNITMAS
            WHERE ACTIVE = 1
            ORDER BY PRUNITNAME;";

        var result =
            await db.QueryListAsync<UnitDropdownResponse>(query);

        return result.ToList();
    }

    // =========================================================
    // PRODUCT UNIT LOOKUP - PRESERVED LEGACY METHOD
    // =========================================================

    public async Task<List<UnitDropdownResponse>> ProductUnitsAsync(
        int userId)
    {
        var db = await GetDatabaseAsync(userId);

        const string query = @"
            SELECT
                PRUNITID AS Id,
                PRUNITNAME AS UnitName
            FROM dbo.PRUNITMAS
            ORDER BY PRUNITNAME;";

        var result =
            await db.QueryListAsync<UnitDropdownResponse>(query);

        return result.ToList();
    }
}