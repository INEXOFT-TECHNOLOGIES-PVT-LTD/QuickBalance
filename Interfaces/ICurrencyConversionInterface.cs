
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;

namespace QuickBalance.API.Interfaces
{
    public interface ICurrencyConversionInterface
    {
        Task<ApiResponse<int>> CreateConversionAsync(
            CurrencyConversionRequest request,
            int userId);

        Task<ApiResponse<bool>> UpdateConversionAsync(
            int code,
            CurrencyConversionRequest request,
            int userId);

        Task<ApiResponse<List<CurrencyConversionResponse>>> GetConversionsAsync(
            int page,
            int pageSize,
            string? search = null);

        Task<ApiResponse<CurrencyConversionDetailsResponse>> GetConversionByIdAsync(
            int code);

        Task<ApiResponse<List<CurrencyConversionCurrencyResponse>>>
            GetCurrenciesAsync(
                int page,
                int pageSize,
                string? search = null);
        Task<ApiResponse<bool>> DeleteConversionAsync(int code, int userId);
    }
}
