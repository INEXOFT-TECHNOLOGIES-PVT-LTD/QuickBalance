using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Purchase;

namespace QuickBalance.API.Interfaces.Purchase
{
    public interface IPurchaseOrderInterface
    {
        Task<ApiResponseWithSuccess> CreatePurchaseOrderAsync(
            PurchaseOrderCreateRequest request,
            int userId);
        Task<ApiResponse<PurchaseOrderListResult>> GetAllAsync(
        int page,
        int pageSize,
        string? search);
        Task<ApiResponse<PurchaseOrderDetailResult>> GetPurchaseOrderByIdAsync(int id);
        Task<ApiResponseWithSuccess> DeletePurchaseOrderAsync(int id);
        Task<ApiResponseWithSuccess> UpdatePurchaseOrderAsync(
            int id,
            PurchaseOrderCreateRequest request);
    }
}