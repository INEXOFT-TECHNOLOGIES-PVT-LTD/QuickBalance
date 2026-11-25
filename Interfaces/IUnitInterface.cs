using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;

namespace QuickBalance.API.Interfaces
{
    public interface IUnitInterface
    {
        Task<ApiResponse<int>> CreateAsync(
            UnitMasterRequest request, int userId);

        Task<ApiResponse<UnitMasterResponse>> GetByIdAsync(
            int id, int userId);

        Task<ApiResponse<IReadOnlyList<UnitMasterResponse>>> GetAllAsync(
            int page, int pageSize, string? search, int userId, bool? active = null);

        Task<ApiResponse<bool>> UpdateAsync(
            int id, UnitMasterRequest request, int userId);

        Task<ApiResponse<IReadOnlyList<UnitDropdownResponse>>> GetActiveAsync(
            int userId);

        Task<ApiResponse<int>> DeleteAsync(int id, int userId);
    }
}
