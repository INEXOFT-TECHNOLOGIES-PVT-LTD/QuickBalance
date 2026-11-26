using QuickBalance.API.CLS;
using QuickBalance.API.DTOs.Sales.SalesBilling;

namespace QuickBalance.API.Interfaces
{
    public interface ISalesBillingInterface
    {
        /// <summary>
        /// Creates (InvoiceId = 0) or updates (InvoiceId > 0) a sales invoice.
        /// </summary>
        Task<ApiResponse<SalesBillingSaveResponse>> SaveAsync(
            SalesBillingRequest request,
            string? idempotencyKey,
            int userId);

        /// <summary>
        /// Gets one sales invoice by its internal id (SALM.INVNO).
        /// </summary>
        Task<ApiResponse<SalesBillingDetailResponse>> GetByIdAsync(int id);

        /// <summary>
        /// Lists sales invoices with paging, search, date range and sorting.
        /// </summary>
        Task<ApiResponse<IReadOnlyList<SalesBillingListItemResponse>>> GetListAsync(
            SalesBillingListRequest request);

        /// <summary>
        /// Cancels a sales invoice (soft cancel: the invoice is kept with CANCELFLG = 1).
        /// </summary>
        Task<ApiResponse<bool>> CancelAsync(int id, int userId);

        /// <summary>Lines of a sales invoice that can still be returned (for the Sales Return screen).</summary>
        Task<ApiResponse<SalesBillingPendingResponse>> GetPendingForReturnAsync(SalesBillingPendingRequest request);
    }
}
