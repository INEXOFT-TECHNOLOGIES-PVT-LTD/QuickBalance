using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.SalesMan;

namespace QuickBalance.API.Interfaces
{
    public interface ISalesmanInterface
    {

        Task<ApiResponse<SalesmanSaveResponse>> CreateAsync(SalesmanRequest request, int userId);
        Task<ApiResponse<IReadOnlyList<SalesmanResponse>>> GetAllAsync(int page, int pageSize, string? search, int userId, bool? active = null);
        Task<ApiResponse<SalesmanResponse>> GetByIdAsync(int id, int userId);
        Task<ApiResponse<SalesmanSaveResponse>> UpdateAsync(int id, SalesmanRequest request, int userId);
        Task<ApiResponse<IReadOnlyList<ActiveSalesmanResponse>>> GetActiveAsync(int userId);
        Task<ApiResponse<IReadOnlyList<SalesmanLookupResponse>>> GetLookupAsync(int userId);
        Task<ApiResponse<SalesmanAccountValidationResponse>> ValidateAccountAsync(int accountId, int userId);
        Task<ApiResponse<bool>> DeleteAsync(int id, int userId);
    }
}
