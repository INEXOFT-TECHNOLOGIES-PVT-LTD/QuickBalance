using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;

namespace QuickBalance.API.Interfaces
{
    public interface ISupplierInterface
    {
        Task<ApiResponse<List<clsSupplier>>> GetSuppliersAsync(
            int page,
            int pageSize,
            string? search);

        Task<ApiResponseWithoutPagination<clsSupplier>> GetSupplierByIdAsync(
            int id);

        Task<ApiResponseWithoutPagination<string>> CreateSupplierAsync(
            SupplierRequest request,
            int userId);

        Task<ApiResponseWithoutPagination<clsSupplier>> UpdateSupplierAsync(
            int id,
            SupplierEditRequest request,
            int userId);

        Task<ApiResponseWithoutPagination<string>> DeleteSupplierAsync(
            int id,
            int userId);
    }
}
