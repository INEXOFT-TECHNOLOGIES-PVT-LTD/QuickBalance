using QuickBalance.API.CLS;
using QuickBalance.API.DTOs.Respons_DTO;
using QuickBalance.API.DTOs.Tax;

namespace QuickBalance.API.Interfaces;

public interface ITaxInterface
{
    Task<ApiResponse<int>> CreateAsync(
        TaxMasterRequest request, int userId);

    Task<ApiResponse<TaxMasterResponse>> GetByIdAsync(int id);

    Task<ApiResponse<IReadOnlyList<TaxMasterResponse>>> GetAllAsync(
        int page, int pageSize, string? search, bool? active = null);

    Task<ApiResponse<bool>> UpdateAsync(
        int id, TaxMasterRequest request, int userId);

    Task<ApiResponse<IReadOnlyList<TaxLookupResponse>>>
        GetActiveForItemMasterAsync();

    Task<ApiResponse<IReadOnlyList<TaxLookupResponse>>>
        GetActiveAllAsync();

    Task<ApiResponse<IReadOnlyList<TaxPercentageResponse>>>
        GetPercentagesAsync();

    Task<ApiResponse<IReadOnlyList<TaxLookupResponse>>>
        SearchAsync(string? search);

    Task<ApiResponse<IReadOnlyList<TaxMasterResponse>>>
        GetDetailsByPercentageAsync(decimal percentage);

    Task<ApiResponse<IReadOnlyList<TaxMasterResponse>>>
        GetDetailsByNameAndPercentageAsync(
            string taxName, decimal percentage);

    Task<ApiResponse<IReadOnlyList<TaxMasterResponse>>>
        GetActiveByTypeAsync(string taxType);

    Task<ApiResponse<IReadOnlyList<TaxMasterResponse>>>
        ValidateAccountAsync(int accountId);

    Task<ApiResponse<int>> GetExemptedTaxIdAsync();

    Task<ApiResponse<bool>> UpdateDefaultTaxAsync(
        int id, string taxName, decimal percentage);

    Task<ApiResponse<int>> DeleteAsync(int id, int userId);
}