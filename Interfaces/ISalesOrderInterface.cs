using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Respons_DTO;
using QuickBalance.API.DTOs.Sales.SalesOrder;

namespace QuickBalance.API.Interfaces
{
    /// <summary>Contract for the Sales Order API (Controller -> Interface -> Service -> Repository).</summary>
    public interface ISalesOrderInterface
    {
        Task<SalesOrderServiceResult<object>> SaveAsync(SalesOrderRequest request, int userId);

        Task<SalesOrderServiceResult<List<Dictionary<string, object?>>>> GetListAsync(
            PaginationRequests request, DateTime? fromDate, DateTime? toDate, int userId);

        Task<SalesOrderServiceResult<SelectedSalesOrderData>> GetByIdAsync(int id, int userId);

        Task<SalesOrderServiceResult<object>> CancelAsync(int id, int userId);

        Task<SalesOrderServiceResult<List<Dictionary<string, object?>>>> GetPendingAsync(PaginationRequest request, int userId);
    }
}
