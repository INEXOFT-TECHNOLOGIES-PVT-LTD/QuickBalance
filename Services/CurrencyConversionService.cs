
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Repository;

namespace QuickBalance.API.Services
{
    public class CurrencyConversionService : ICurrencyConversionInterface
    {
        private readonly CurrencyConversionRepository _repository;
        private readonly ILogger<CurrencyConversionService> _logger;

        public CurrencyConversionService(
            CurrencyConversionRepository repository,
            ILogger<CurrencyConversionService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        // ---------------------------------------------------------
        // GET PAGINATED CURRENCY CONVERSIONS
        // ---------------------------------------------------------
        public async Task<ApiResponse<List<CurrencyConversionResponse>>>
            GetConversionsAsync(
                int page,
                int pageSize,
                string? search = null)
        {
            try
            {
                if (page < 1)
                {
                    return ApiResponse<List<CurrencyConversionResponse>>
                        .Failure("Page must be greater than zero.");
                }

                if (pageSize < 1 || pageSize > 500)
                {
                    return ApiResponse<List<CurrencyConversionResponse>>
                        .Failure("Page size must be between 1 and 500.");
                }

                var (data, totalRecords) =
                    await _repository.GetConversionsAsync(
                        page,
                        pageSize,
                        search);

                int totalPages = (int)Math.Ceiling(
                    totalRecords / (double)pageSize);

                var pagination = new PaginationInfo
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    TotalPages = totalPages
                };

                return ApiResponse<List<CurrencyConversionResponse>>
                    .SuccessWithPagination(
                        "Currency conversions retrieved successfully.",
                        data,
                        pagination);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving currency conversions.");

                return ApiResponse<List<CurrencyConversionResponse>>
                    .Failure(
                        "An error occurred while retrieving currency conversions.");
            }
        }

        // ---------------------------------------------------------
        // GET CONVERSION BY ID
        // Returns all conversion rows belonging to that date.
        // ---------------------------------------------------------
        public async Task<ApiResponse<CurrencyConversionDetailsResponse>>GetConversionByIdAsync(int code)
        {
            try
            {
                if (code <= 0)
                {
                    return ApiResponse<CurrencyConversionDetailsResponse>
                        .Failure("Invalid conversion ID.");
                }

                var conversion =
                    await _repository.GetConversionByIdAsync(code);

                if (conversion == null ||
                    conversion.ConversionDate == null)
                {
                    return ApiResponse<CurrencyConversionDetailsResponse>
                        .Failure("Currency conversion not found.");
                }

                var data =
                    await _repository.GetConversionsByDateAsync(
                        conversion.ConversionDate.Value);

                if (data.Count == 0)
                {
                    return ApiResponse<CurrencyConversionDetailsResponse>
                        .Failure("Currency conversion not found.");
                }

                var result = new CurrencyConversionDetailsResponse
                {
                    ConversionDate = conversion.ConversionDate,
                    Details = data.Select(x => new CurrencyConversionDetailResponse
                    {
                        Id = x.Id,
                        CurrencyId = x.CurrencyId,
                        Symbol = x.Symbol,
                        StandardRate = x.StandardRate,
                        SellRate = x.SellRate,
                        BuyRate = x.BuyRate
                    }).ToList()
                };

                return ApiResponse<CurrencyConversionDetailsResponse>
                    .Success(
                        "Currency conversion retrieved successfully.",
                        result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving conversion. Code: {Code}",
                    code);

                return ApiResponse<CurrencyConversionDetailsResponse>
                    .Failure(
                        "An error occurred while retrieving currency conversion.");
            }
        }

        // ---------------------------------------------------------
        // GET PAGINATED CURRENCIES

        public async Task<ApiResponse<List<CurrencyConversionCurrencyResponse>>>
            GetCurrenciesAsync(
                int page,
                int pageSize,
                string? search = null)
        {
            try
            {
                if (page < 1)
                {
                    return ApiResponse<List<CurrencyConversionCurrencyResponse>>
                        .Failure("Page must be greater than zero.");
                }

                if (pageSize < 1 || pageSize > 500)
                {
                    return ApiResponse<List<CurrencyConversionCurrencyResponse>>
                        .Failure("Page size must be between 1 and 500.");
                }

                var (data, totalRecords) =
                    await _repository.GetCurrenciesAsync(
                        page,
                        pageSize,
                        search);

                int totalPages = (int)Math.Ceiling(
                    totalRecords / (double)pageSize);

                var pagination = new PaginationInfo
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    TotalPages = totalPages
                };

                return ApiResponse<List<CurrencyConversionCurrencyResponse>>
                    .SuccessWithPagination(
                        "Currencies retrieved successfully.",
                        data,
                        pagination);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving available currencies.");

                return ApiResponse<List<CurrencyConversionCurrencyResponse>>
                    .Failure(
                        "An error occurred while retrieving currencies.");
            }
        }

        // ---------------------------------------------------------
        // CREATE CURRENCY CONVERSION
        // ---------------------------------------------------------
        public async Task<ApiResponse<int>> CreateConversionAsync(
            CurrencyConversionRequest request,
            int userId)
        {
            try
            {
                string? validationMessage =
                    await ValidateConversionRequestAsync(
                        request,
                        null);

                if (validationMessage != null)
                {
                    return ApiResponse<int>.Failure(validationMessage);
                }

                var baseCurrency =
                    await _repository.GetBaseCurrencyAsync();

                if (baseCurrency == null)
                {
                    return ApiResponse<int>
                        .Failure("Base currency is not configured.");
                }

                if (userId <= 0)
                {
                    return ApiResponse<int>
                        .Failure("Invalid authenticated user.");
                }

                int firstInsertedCode = 0;

                foreach (var detail in request.Details)
                {
                    int insertedCode =
                        await _repository.CreateConversionAsync(
                            baseCurrency.Id,
                            detail.CurrencyId,
                            detail.StandardRate,
                            detail.SellRate,
                            detail.BuyRate,
                            request.ConversionDate!.Value,
                            userId);

                    if (insertedCode <= 0)
                    {
                        _logger.LogError(
                            "Failed to insert conversion for {Date}, currency {CurrencyId}.",
                            request.ConversionDate,
                            detail.CurrencyId);

                        return ApiResponse<int>
                            .Failure(
                                "Currency conversion could not be saved completely.");
                    }

                    if (firstInsertedCode == 0)
                    {
                        firstInsertedCode = insertedCode;
                    }
                }

                _logger.LogInformation(
                    "Currency conversion created successfully for {Date}.",
                    request.ConversionDate);

                return ApiResponse<int>.Success(
                    "Currency conversion created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating currency conversion.");

                return ApiResponse<int>
                    .Failure(
                        "An error occurred while creating currency conversion.");
            }
        }


        // ---------------------------------------------------------
        // UPDATE CURRENCY CONVERSION
        // ---------------------------------------------------------
        public async Task<ApiResponse<bool>> UpdateConversionAsync(
            int code,
            CurrencyConversionRequest request,
            int userId)
        {
            try
            {
                if (code <= 0)
                {
                    return ApiResponse<bool>
                        .Failure("Invalid conversion ID.");
                }

                if (userId <= 0)
                {
                    return ApiResponse<bool>
                        .Failure("Invalid authenticated user.");
                }

                DateTime? oldDate =
                    await _repository.GetConversionDateByIdAsync(code);

                if (oldDate == null)
                {
                    return ApiResponse<bool>
                        .Failure("Currency conversion not found.");
                }

                string? validationMessage =
                    await ValidateConversionRequestAsync(
                        request,
                        oldDate.Value);

                if (validationMessage != null)
                {
                    return ApiResponse<bool>
                        .Failure(validationMessage);
                }

                var baseCurrency =
                    await _repository.GetBaseCurrencyAsync();

                if (baseCurrency == null)
                {
                    return ApiResponse<bool>
                        .Failure("Base currency is not configured.");
                }

                // Execute update using database transaction.
                bool updated =
                    await _repository.UpdateConversionsAsync(
                        oldDate.Value,
                        baseCurrency.Id,
                        request,
                        userId);

                if (!updated)
                {
                    return ApiResponse<bool>
                        .Failure("Currency conversion could not be updated.");
                }

                _logger.LogInformation(
                    "Currency conversion updated successfully. Code: {Code}, Date: {Date}",
                    code,
                    request.ConversionDate);

                return ApiResponse<bool>.Success(
                    "Currency conversion updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating currency conversion. Code: {Code}",
                    code);

                return ApiResponse<bool>
                    .Failure(
                        "An error occurred while updating currency conversion.");
            }
        }


        // ---------------------------------------------------------
        // DELETE CURRENCY CONVERSION
        // Deletes all rows belonging to the selected date.
        // ---------------------------------------------------------
        public async Task<ApiResponse<bool>> DeleteConversionAsync(int code, int userId)
        {
            try
            {
                if (code <= 0)
                {
                    return ApiResponse<bool>
                        .Failure("Invalid conversion ID.");
                }

                DateTime? conversionDate =
                    await _repository.GetConversionDateByIdAsync(code);

                if (conversionDate == null)
                {
                    return ApiResponse<bool>
                        .Failure("Currency conversion not found.");
                }

                bool deleted =
                    await _repository.DeleteConversionsByDateAsync(
                        conversionDate.Value,
                        userId);

                if (!deleted)
                {
                    return ApiResponse<bool>
                        .Failure("Currency conversion not found.");
                }

                _logger.LogInformation(
                    "Currency conversion deleted for {Date}.",
                    conversionDate.Value);

                return ApiResponse<bool>.Success(
                    "Currency conversion deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error deleting currency conversion. Code: {Code}",
                    code);

                return ApiResponse<bool>
                    .Failure(
                        "An error occurred while deleting currency conversion.");
            }
        }

        // ---------------------------------------------------------
        // COMMON REQUEST VALIDATION
        // ---------------------------------------------------------
        private async Task<string?> ValidateConversionRequestAsync(
            CurrencyConversionRequest request,
            DateTime? excludeDate)
        {
            if (request == null)
            {
                return "Request cannot be empty.";
            }

            if (request.ConversionDate == default)
            {
                return "Conversion date is required.";
            }

            if (request.Details == null ||
                request.Details.Count == 0)
            {
                return "At least one currency conversion detail is required.";
            }

            // Check duplicate date.
            bool dateExists =
                await _repository.ConversionDateExistsAsync(
                   request.ConversionDate!.Value);

            if (dateExists &&
                (!excludeDate.HasValue ||
                 request.ConversionDate.Value.Date != excludeDate.Value.Date))
            {
                return "Currency conversion already exists for this date.";
            }

            var baseCurrency =
                await _repository.GetBaseCurrencyAsync();

            if (baseCurrency == null)
            {
                return "Base currency is not configured.";
            }

            // Check duplicate alternate currencies.
            var duplicateCurrency = request.Details
                .GroupBy(x => x.CurrencyId)
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicateCurrency != null)
            {
                return "Duplicate alternate currency is not allowed.";
            }

            // Validate every detail.
            foreach (var detail in request.Details)
            {
                if (detail.CurrencyId <= 0)
                {
                    return "Invalid currency ID.";
                }

                if (detail.CurrencyId == baseCurrency.Id)
                {
                    return "Base currency cannot be selected as an alternate currency.";
                }

                if (detail.StandardRate <= 0 ||
                    detail.SellRate <= 0 ||
                    detail.BuyRate <= 0)
                {
                    return "Standard rate, sell rate and buy rate must be greater than zero.";
                }

                var currency =
                    await _repository.GetCurrencyByIdAsync(
                        detail.CurrencyId);

                if (currency == null)
                {
                    return $"Currency id {detail.CurrencyId} does not exist.";
                }

                if (currency.IsBaseCurrency)
                {
                    return "Base currency cannot be selected as an alternate currency.";
                }
            }

            return null;
        }
    }
}
