using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Respons_DTO;

namespace QuickBalance.API.Interfaces
{

    public interface IItemGroupInterface
    {
        Task<ApiResponse<int>> CreateAsync(
            ItemGroupRequest request, int userId);

        Task<ApiResponse<ItemGroupResponse>> GetByIdAsync(int id);

        Task<ApiResponse<IReadOnlyList<ItemGroupListItemDto>>> GetAllAsync(
            int page, int pageSize, string? search, bool? active);

        Task<ApiResponse<bool>> UpdateAsync(
            int id, ItemGroupRequest request, int userId);

        Task<ApiResponse<bool>> DeleteAsync(int id, int userId);

        Task<ApiResponseWithoutPagination<IReadOnlyList<ItemGroupResponse>>>
            GetPrimaryGroupsAsync(string? search = null);

        Task<ApiResponse<IReadOnlyList<ItemGroupResponse>>>
            GetPrimaryGroupsPagedAsync(int page, int pageSize, string? search);
    }
}
