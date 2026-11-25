using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.SalesMan;

namespace QuickBalance.API.Interfaces
{
    public interface ISalesmanGroupInterface
    {
        Task<ApiResponse<int>> CreateAsync(SalesmanGroupRequest request, int userId);

        Task<ApiResponse<IReadOnlyList<SalesmanGroupListItemDto>>> GetAllAsync(int page, int pageSize, string? search, int userId, bool? active = null);

        Task<ApiResponse<SalesmanGroupResponse>> GetByIdAsync(int id, int userId);

        Task<ApiResponse<bool>> UpdateAsync(int id, SalesmanGroupRequest request, int userId);

        Task<ApiResponse<IReadOnlyList<SalesmanGroupLookupResponse>>> GetLookupAsync(int userId);

        Task<ApiResponse<bool>> DeleteAsync(int id, int userId);
    }

}
