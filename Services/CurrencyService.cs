using QuickBalance.API.CLS;
using QuickBalance.API.DTOs.Currencies;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Repository;

namespace QuickBalance.API.Services
{
    public class CurrencyService : ICurrencyInterface
    {
        private readonly CurrencyRepository _repository;
        private readonly ILogger<CurrencyService> _logger;

        public CurrencyService(
            CurrencyRepository repository,
            ILogger<CurrencyService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        //public async Task<ApiResponse<List<CurrencyResponse>>> GetCurrenciesAsync()
        //{
        //    try
        //    {
        //        var currencies = await _repository.GetCurrenciesAsync();

        //        return ApiResponse<List<CurrencyResponse>>.Success(
        //            "Currencies retrieved successfully.",
        //            currencies);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error retrieving currencies.");

        //        return ApiResponse<List<CurrencyResponse>>.Failure(
        //            "An error occurred while retrieving currencies.");
        //    }
        //}

        public async Task<ApiResponse<IReadOnlyList<CurrencyResponse>>> GetCurrenciesAsync(int page, int pageSize, string? search = null, bool excludeBase = false)
        {
            try
            {
                if (page < 1)
                {
                    return ApiResponse<IReadOnlyList<CurrencyResponse>>.Failure(
                        "Page must be greater than 0.");
                }

                if (pageSize < 1 || pageSize > 500)
                {
                    return ApiResponse<IReadOnlyList<CurrencyResponse>>.Failure(
                        "Page size must be between 1 and 500.");
                }

                var data = await _repository.GetCurrenciesAsync(page, pageSize, search, excludeBase);

                //int totalPages = (int)Math.Ceiling(data.Pagination.TotalPages / (double)pageSize);

                //var pagination = new PaginationInfo
                //{
                //    Page = page,
                //    PageSize = pageSize,
                //    TotalRecords = data.Pagination.TotalRecords,
                //    TotalPages = totalPages
                //};

                return ApiResponse<IReadOnlyList<CurrencyResponse>>.SuccessWithPagination(
                    "Currencies retrieved successfully.",
                    data.Data,
                    data.Pagination);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving currencies.");

                return ApiResponse<IReadOnlyList<CurrencyResponse>>.Failure(
                    "An error occurred while retrieving currencies.");
            }
        }


        // =========================================================
        // GET ALTERNATE CURRENCIES
        // =========================================================
        public async Task<ApiResponse<IReadOnlyList<CurrencyResponse>>>GetAlternateCurrenciesAsync(
                int page,
                int pageSize,
                string? search = null)
        {
            try
            {
                if (page < 1)
                {
                    return ApiResponse<IReadOnlyList<CurrencyResponse>>.Failure(
                        "Page must be greater than 0.");
                }

                if (pageSize < 1 || pageSize > 500)
                {
                    return ApiResponse<IReadOnlyList<CurrencyResponse>>.Failure(
                        "Page size must be between 1 and 500.");
                }

                var data =
                    await _repository.GetAlternateCurrenciesAsync(
                        page,
                        pageSize,
                        search);

               

                return ApiResponse<IReadOnlyList<CurrencyResponse>>.SuccessWithPagination(
                    "Alternate currencies retrieved successfully.",
                    data.Data,
                    data.Pagination);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving alternate currencies.");

                return ApiResponse<IReadOnlyList<CurrencyResponse>>.Failure(
                    "An error occurred while retrieving alternate currencies.");
            }
        }

        public async Task<ApiResponse<CurrencyResponse>> GetCurrencyByIdAsync(int code)
        {
            try
            {
                if (code <= 0)
                {
                    return ApiResponse<CurrencyResponse>.Failure(
                        "Invalid currency code.");
                }

                var currency = await _repository.GetCurrencyByIdAsync(code);

                if (currency == null)
                {
                    return ApiResponse<CurrencyResponse>.Failure(
                        "Currency not found.");
                }

                return ApiResponse<CurrencyResponse>.Success(
                    "Currency retrieved successfully.",
                    currency);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving currency. Code: {Code}",
                    code);

                return ApiResponse<CurrencyResponse>.Failure(
                    "An error occurred while retrieving currency.");
            }
        }
        // CREATE CURRENCY
        public async Task<ApiResponse<int>> CreateCurrencyAsync(
            CurrencyRequest request)
        {
            try
            {
                if (request == null)
                    return ApiResponse<int>.Failure(
                        "Currency details are required.");

                if (string.IsNullOrWhiteSpace(request.Symbol))
                    return ApiResponse<int>.Failure(
                        "Currency symbol is required.");

                if (request.Symbol.Length > 50)
                    return ApiResponse<int>.Failure(
                        "Currency symbol cannot exceed 50 characters.");

                if (string.IsNullOrWhiteSpace(request.CurrString))
                    return ApiResponse<int>.Failure(
                        "Currency string is required.");

                if (request.CurrString.Length > 50)
                    return ApiResponse<int>.Failure(
                        "Currency string cannot exceed 50 characters.");

                if (request.SubString?.Length > 50 ||
                    request.SecSymbol?.Length > 50 ||
                    request.SecCurrString?.Length > 50 ||
                    request.SecSubString?.Length > 50)
                {
                    return ApiResponse<int>.Failure(
                        "Currency text fields cannot exceed 50 characters.");
                }

                var symbolExists =
                   await _repository.CurrencySymbolExistsAsync(
                       request.Symbol.Trim());

                if (symbolExists)
                {
                    return ApiResponse<int>.Failure(
                        "Currency symbol already exists.");
                }

                var code = await _repository.CreateCurrencyAsync(request);

                if (code <= 0)
                    return ApiResponse<int>.Failure(
                        "Failed to create currency.");

                _logger.LogInformation(
                    "Currency created successfully. Code: {Code}",
                    code);

                return ApiResponse<int>.Success(
                    "Currency created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating currency.");

                return ApiResponse<int>.Failure(
                    "An error occurred while creating currency.");
            }
        }

        // UPDATE CURRENCY
        public async Task<ApiResponse<bool>> UpdateCurrencyAsync(
            int code,
            CurrencyRequest request)
        {
            try
            {
                if (code <= 0)
                    return ApiResponse<bool>.Failure(
                        "Invalid currency Id.");
                var existing = await _repository.GetCurrencyByIdAsync(code);
                if(existing==null)
                    return ApiResponse<bool>.Failure(
                   "currency Id not found.");
                if (request == null)
                    return ApiResponse<bool>.Failure(
                        "Currency details are required.");

                if (string.IsNullOrWhiteSpace(request.Symbol))
                    return ApiResponse<bool>.Failure(
                        "Currency symbol is required.");

                if (request.Symbol.Length > 50)
                    return ApiResponse<bool>.Failure(
                        "Currency symbol cannot exceed 50 characters.");

                if (string.IsNullOrWhiteSpace(request.CurrString))
                    return ApiResponse<bool>.Failure(
                        "Currency string is required.");

                if (request.CurrString.Length > 50)
                    return ApiResponse<bool>.Failure(
                        "Currency string cannot exceed 50 characters.");

                if (request.SubString?.Length > 50 ||
                    request.SecSymbol?.Length > 50 ||
                    request.SecCurrString?.Length > 50 ||
                    request.SecSubString?.Length > 50)
                {
                    return ApiResponse<bool>.Failure(
                        "Currency text fields cannot exceed 50 characters.");
                }

                var updated = await _repository.UpdateCurrencyAsync(
                    code,
                    request);

                if (!updated)
                    return ApiResponse<bool>.Failure(
                        "Currency not found or update failed.");

                _logger.LogInformation(
                    "Currency updated successfully. Code: {Code}",
                    code);

                return ApiResponse<bool>.Success(
                    "Currency updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating currency. Code: {Code}",
                    code);

                return ApiResponse<bool>.Failure(
                    "An error occurred while updating currency.");
            }
        }


        public async Task<ApiResponse<bool>> DeleteCurrencyAsync(int code, int userId)
        {
            try
            {
                if (code <= 0)
                    return ApiResponse<bool>.Failure(
                        "Invalid currency code.");

                // Check whether currency exists.
                var existing = await _repository.GetCurrencyByIdAsync(code);

                if (existing == null)
                    return ApiResponse<bool>.Failure(
                        "Currency not found.");

                // Prevent deletion of base currency.
                if (existing.BaseFlg)
                    return ApiResponse<bool>.Failure(
                        "Base currency cannot be deleted.");

                var deleted = await _repository.DeleteCurrencyAsync(code, userId);

                if (!deleted)
                    return ApiResponse<bool>.Failure(
                        "Currency not found.");

                _logger.LogInformation(
                    "Currency deleted successfully. Code: {Code}",
                    code);

                return ApiResponse<bool>.Success(
                    "Currency deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error deleting currency. Code: {Code}",
                    code);

                return ApiResponse<bool>.Failure(
                    "An error occurred while deleting currency.");
            }
        }
    }
}
