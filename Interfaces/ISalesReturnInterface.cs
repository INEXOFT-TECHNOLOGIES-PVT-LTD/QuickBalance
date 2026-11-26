using QuickBalance.API.CLS;
using QuickBalance.API.DTOs.Sales.SalesReturn;

namespace QuickBalance.API.Interfaces
{
    /// <summary>
    /// Contract for the NEW Sales Return API (layered: Controller -> Interface -> Service -> Repository).
    /// Get / List / Cancel will be added later.
    /// </summary>
    public interface ISalesReturnInterface
    {
        /// <summary>Create (ReturnId = 0) or update (ReturnId > 0) a sales return.</summary>
        Task<ApiResponse<SalesReturnSaveResponse>> SaveAsync(
            SalesReturnSaveRequest request,
            string? idempotencyKey,
            int userId);

        /// <summary>One sales return with its lines, charges and serial numbers.</summary>
        Task<ApiResponse<SalesReturnDetailResponse>> GetByIdAsync(int id);

        /// <summary>Paged list of sales returns.</summary>
        Task<ApiResponse<IReadOnlyList<SalesReturnListItemResponse>>> GetListAsync(SalesReturnListRequest request);

        /// <summary>Cancel a sales return (soft cancel: stock is reversed, the voucher stays for audit).</summary>
        Task<ApiResponse<bool>> CancelAsync(int id, int userId);
    }
}
