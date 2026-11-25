using QuickBalance.API.CLS;
using QuickBalance.API.DTOs.ItemCategory;
using QuickBalance.API.DTOs.Respons_DTO;

namespace QuickBalance.API.Interfaces
{
 
    public interface IItemCategoryInterface
    {
        Task<ApiResponse<int>> CreateAsync(
            ItemCategoryRequest request, int userId);

        Task<ApiResponse<ItemCategoryResponse>> GetByIdAsync(int id);

        Task<ApiResponse<IReadOnlyList<ItemCategoryResponse>>> GetAllAsync(
            int page, int pageSize, string? search, bool? active = null);

        Task<ApiResponse<bool>> UpdateAsync(
            int id, ItemCategoryRequest request, int userId);

        Task<ApiResponse<IReadOnlyList<ItemCategoryResponse>>> GetActiveAsync();
        Task<ApiResponse<int>> DeleteAsync(int id, int userId);
    }
}
