using QuickBalance.API.CLS;
using QuickBalance.API.DTOs.Currencies;

namespace QuickBalance.API.Interfaces
{
    public interface ICurrencyInterface
    {
        Task<ApiResponse<int>> CreateCurrencyAsync(
            CurrencyRequest request);

        Task<ApiResponse<bool>> UpdateCurrencyAsync(
            int code,
            CurrencyRequest request);


        //Task<ApiResponse<List<CurrencyResponse>>> GetCurrenciesAsync();

        Task<ApiResponse<IReadOnlyList<CurrencyResponse>>> GetCurrenciesAsync(
           int page,
           int pageSize,
           string? search = null,
           bool excludeBase = false);

        Task<ApiResponse<CurrencyResponse>> GetCurrencyByIdAsync(int code);

        Task<ApiResponse<IReadOnlyList<CurrencyResponse>>> GetAlternateCurrenciesAsync(
                int page,
                int pageSize,
                string? search = null);


        Task<ApiResponse<bool>> DeleteCurrencyAsync(int code, int userId);
}
}
