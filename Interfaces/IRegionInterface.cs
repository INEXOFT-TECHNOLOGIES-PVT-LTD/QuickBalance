using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;

namespace QuickBalance.API.Interfaces
{
    public interface IRegionInterface
    {

        Task<ApiResponse<IReadOnlyList<RegionResponse>>> GetRegionsAsync(int countryId, string? search, int page, int pageSize);
        Task<ApiResponse<List<RegionResponse>>> GetRegionsAsync(int countryId);

        Task<ApiResponse<RegionResponse>> GetRegionByIdAsync(int regionId);

        Task<ApiResponse<int>> CreateRegionAsync(RegionRequest request);
        Task<ApiResponse<RegionResponse>> GetRegionsByCoutryId(int countryId);
        Task<ApiResponse<bool>> UpdateRegionAsync(int regionId, RegionRequest request);
    }
}
