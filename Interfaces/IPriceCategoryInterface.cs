
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.PriceCategory;
using QuickBalance.API.DTOs.Respons_DTO;

namespace QuickBalance.API.Interfaces;

public interface IPriceCategoryInterface
{
    Task<ApiResponse<int>> CreateAsync(
        PriceCategoryRequest request, int userId);

    Task<ApiResponse<PriceCategoryResponse>> GetByIdAsync(int id);

    Task<ApiResponse<IReadOnlyList<PriceCategoryResponse>>> GetAllAsync(
        int page, int pageSize, string? search);

    Task<ApiResponse<bool>> UpdateAsync(
        int id, PriceCategoryRequest request, int userId);

    Task<ApiResponse<bool>> DeleteAsync(int id, int userId);
}
