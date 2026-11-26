using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;

namespace QuickBalance.API.Interfaces
{
    public interface IBomService
    {
        Task<ApiResponse<int>> CreateAsync(BomRequest request, int userId);

        Task<ApiResponse<IReadOnlyList<BomListDetailsDto>>> GetAllAsync(
            int page,
            int pageSize,
            string? search,
            bool? active = null);

        Task<ApiResponse<BomDetailsDto>> GetByIdAsync(int id);

        Task<ApiResponse<int>> UpdateAsync(int id, BomRequest request, int userId);

        Task<ApiResponse<bool>> DeleteAsync(int id, int userId);

        Task<ApiResponse<List<BomItemDto>>> GetByProductCodeAsync(int productId);
    }
}
