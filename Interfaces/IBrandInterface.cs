using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Brand;

namespace QuickBalance.API.Interfaces
{
    public interface IBrandInterface
    {
        Task<ApiResponse<int>> CreateAsync(BrandRequest request);

        Task<ApiResponse<BrandResponse>> GetByIdAsync(int id);

        Task<ApiResponse<IReadOnlyList<BrandResponse>>> GetAllAsync(
            int page, int pageSize, string? search);

        Task<ApiResponse<bool>> UpdateAsync(int id, BrandRequest request);

        Task<ApiResponse<bool>> DeleteAsync(int id, int userId);

        Task<ApiResponse<IReadOnlyList<BrandResponse>>> GetActiveAsync();
    }
}
