using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;

namespace QuickBalance.API.Interfaces
{
    public interface ILocationGroupService
    {
        Task<ApiResponse<int>> CreateAsync(
            LocationGroupMasterRequest request,
            int userId);

        Task<ApiResponse<IReadOnlyList<LocationGroupDto>>> GetAllAsync(
            int page,
            int pageSize,
            string? search,
            bool? active = null);

        Task<ApiResponse<LocationGroupDto>> GetByIdAsync(int id);

        Task<ApiResponse<int>> UpdateAsync(
            int id,
            LocationGroupMasterRequest request,
            int userId);

        Task<ApiResponse<IReadOnlyList<LocationGroupDropdownDto>>>
            GetActiveAsync();

        Task<ApiResponse<IReadOnlyList<LocationGroupDropdownDto>>>
            SearchAsync(string? search);

        Task<ApiResponse<IReadOnlyList<LocationGroupDropdownDto>>>
            GetDropdownAsync();

        Task<ApiResponse<int>> DeleteAsync(
        int id,
        int userId);
    }
}