
using QuickBalance.API.DTOs.Purchase;

namespace QuickBalance.API.Interfaces.Purchase
{
    public interface IPurchaseOrderRepository
    {
        Task<int> CreatePurchaseOrderAsync(
            PurchaseOrderCreateRequest request,
            int userId);
        Task<PurchaseOrderListResult> GetAllAsync(
            int page,
            int pageSize,
            string? search);
        Task<PurchaseOrderDetailResult?> GetPurchaseOrderByIdAsync(int id);
        Task<bool> DeletePurchaseOrderAsync(int id);

    }
}
