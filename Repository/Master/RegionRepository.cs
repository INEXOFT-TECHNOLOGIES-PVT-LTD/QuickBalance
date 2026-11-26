using QuickBalance.API.CLS;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Respons_DTO;
using System.Data;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
namespace QuickBalance.API.Repositories;

public class RegionRepository
{
    private readonly DBConn _db;

    public RegionRepository(DBConn db)
    {
        _db = db;
    }

    public async Task<List<RegionResponse>> GetRegionsAsyncRecords(int countryId)
    {
        const string query = """
        SELECT
            COUNTRYID AS CountryId,
            REGIONID AS RegionId,
            REGIONNAME AS RegionName,
            REGIONSECNAME AS RegionSecName,
            ACTIVE AS IsActive
        FROM dbo.REGION
        WHERE COUNTRYID = @CountryId
        ORDER BY REGIONNAME;
        """;

        return await _db.QueryMasterListAsync<RegionResponse>(
            query,
            new { CountryId = countryId });
    }



    public async Task<PagedResponse<RegionResponse>> GetRegionsAsync(
    int countryId,
    string? search,
    int page,
    int pageSize)
    {
        search = string.IsNullOrWhiteSpace(search)
            ? null
            : search.Trim();

        const string countSql = """
        SELECT COUNT(1)
        FROM dbo.REGION
        WHERE ACTIVE = 1 and COUNTRYID = @CountryId
          AND
          (
              @Search IS NULL
              OR REGIONNAME LIKE '%' + @Search + '%'
              OR REGIONSECNAME LIKE '%' + @Search + '%'
          );
        """;

        int totalRecords = await _db.QueryMasterFirstOrDefaultAsync<int>(
            countSql,
            new
            {
                CountryId = countryId,
                Search = search
            });

        const string dataSql = """
        SELECT
            COUNTRYID AS CountryId,
            REGIONID AS RegionId,
            REGIONNAME AS RegionName,
            REGIONSECNAME AS RegionSecName,
            ACTIVE AS IsActive
        FROM dbo.REGION
        WHERE ACTIVE=1 and COUNTRYID = @CountryId
          AND
          (
              @Search IS NULL
              OR REGIONNAME LIKE '%' + @Search + '%'
              OR REGIONSECNAME LIKE '%' + @Search + '%'
          )
        ORDER BY REGIONNAME, REGIONID
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY;
        """;

        int offset = (page - 1) * pageSize;

        var data = (await _db.QueryMasterListAsync<RegionResponse>(
            dataSql,
            new
            {
                CountryId = countryId,
                Search = search,
                Offset = offset,
                PageSize = pageSize
            })).ToList();

        return new PagedResponse<RegionResponse>
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
    public async Task<RegionResponse?> GetRegionByIdAsync(int regionId)
    {
        const string query = """
        SELECT
            COUNTRYID AS CountryId,
            REGIONID AS RegionId,
            REGIONNAME AS RegionName,
            REGIONSECNAME AS RegionSecName
        FROM dbo.REGION
        WHERE ACTIVE=1 and REGIONID = @RegionId;
        """;

        return await _db.QueryMasterFirstOrDefaultAsync<RegionResponse>(
            query,
            new { RegionId = regionId });
    }

    public async Task<int?> GetRegionByNameAsync(
        string regionName,
        int countryId)
    {
        const string query = """
        SELECT TOP 1 REGIONID
        FROM dbo.REGION
        WHERE ACTIVE = 1
          AND COUNTRYID = @CountryId
          AND (
              REGIONNAME = @RegionName
              OR REGIONSECNAME = @RegionName
          );
        """;

        return await _db.QueryMasterFirstOrDefaultAsync<int?>(
            query,
            new
            {
                RegionName = regionName.Trim(),
                CountryId = countryId
            });
    }

    public async Task<int?> GetRegionByNameAndIdAsync(
    string regionName,
    int countryId,
    int excludeRegionId = 0)
    {
        const string query = """
        SELECT TOP 1 REGIONID
        FROM dbo.REGION
        WHERE ACTIVE = 1
          AND COUNTRYID = @CountryId
          AND REGIONID <> @ExcludeRegionId
          AND (
              REGIONNAME = @RegionName
              OR REGIONSECNAME = @RegionName
          );
        """;

        return await _db.QueryMasterFirstOrDefaultAsync<int?>(
            query,
            new
            {
                RegionName = regionName.Trim(),
                CountryId = countryId,
                ExcludeRegionId = excludeRegionId
            });
    }
    public async Task<RegionResponse?> GetRegionsByCoutryId(int countryId)
    {
        string query = $@"
             SELECT COUNTRYID as CountryId
             REGIONID AS RegionId,
            REGIONNAME AS RegionName,
            REGIONSECNAME AS RegionSecName
             FROM dbo.REGION WHERE ACTIVE=1 and COUNTRYID = @CountryId ORDER BY REGIONNAME;";
        return await _db.QueryMasterFirstOrDefaultAsync<RegionResponse>(
                query,
                new { countryId = countryId });
    }
    public async Task<int> CreateRegionAsync(RegionRequest request)
    {
        const string query = """
        INSERT INTO dbo.REGION
        (
            COUNTRYID,
            REGIONNAME,
            REGIONSECNAME,
            ACTIVE
        )
        OUTPUT INSERTED.REGIONID
        VALUES
        (
            @CountryId,
            @RegionName,
            @RegionSecName,
            @IsActive
        );
        """;

        return await _db.QueryMasterFirstOrDefaultAsync<int>(
            query,
            new
            {
                request.CountryId,
                request.RegionName,
                request.RegionSecName,
                request.IsActive
            });
    }

    public async Task<bool> UpdateRegionAsync(int regionId, RegionRequest request)
    {
        const string query = """
        UPDATE dbo.REGION
        SET
            COUNTRYID = @CountryId,
            REGIONNAME = @RegionName,
            REGIONSECNAME = @RegionSecName,
            ACTIVE = @IsActive
        WHERE REGIONID = @RegionId;
        """;

        var affectedRows = await _db.ExecuteMasterAsync(
            query,
            new
            {
                RegionId = regionId,
                request.CountryId,
                request.RegionName,
                request.RegionSecName,
                request.IsActive
            });

        return affectedRows > 0;
    }
}