using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;

namespace QuickBalance.API.Interfaces
{
    public interface ILocationService
    {
        Task<ApiResponse<int>> CreateAsync(
            LocationMasterRequest request,
            int userId);

        Task<ApiResponse<IReadOnlyList<LocationListItemDto>>> GetAllAsync(
            int page,
            int pageSize,
            string? search,
            bool? active = null);

        Task<ApiResponse<LocationDto>> GetByIdAsync(int id);

        Task<ApiResponse<int>> UpdateAsync(
            int id,
            LocationMasterRequest request,
            int userId);

        Task<ApiResponse<IReadOnlyList<LocationActiveDto>>> GetActiveAsync();

        Task<ApiResponse<IReadOnlyList<LocationDropdownDto>>> SearchAsync(
            string? search);

        Task<ApiResponse<IReadOnlyList<LocationDropdownDto>>> GetDropdownAsync();

        Task<ApiResponse<bool>> IsStockAccountUsedAsync(int accountId);

        Task<ApiResponse<int>> DeleteAsync(int id, int userId);
    }
}