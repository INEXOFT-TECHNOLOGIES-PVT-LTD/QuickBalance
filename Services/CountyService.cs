using Azure;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Respons_DTO;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Repository.Master;

namespace QuickBalance.API.Services
{
    public class CountryService : ICountryService
    {
        private readonly CountryRepository _repository;
        private readonly ILogger<CountryService> _logger;

        public CountryService(CountryRepository repository,
                        ILogger<CountryService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<ApiResponse<IReadOnlyList<CountryDtos>>> GetCountries(int page, int pageSize, string? search)
        {
            try
            {
                if (page < 1)
                    return ApiResponse<IReadOnlyList<CountryDtos>>.Failure(
                     "Page Number must be greater than zero.");

                if (pageSize < 1 || pageSize > 100)
                    return ApiResponse<IReadOnlyList<CountryDtos>>.Failure(
                  "Page size must be between 1 and 100.");

                var result = await _repository.GetCountrieFull(page,pageSize,search);


                return ApiResponse<IReadOnlyList<CountryDtos>>.SuccessWithPagination(
                    "Countries retrieved successfully.",
                    result.Data,
                    result.Pagination);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving countries.");

                return ApiResponse<IReadOnlyList<CountryDtos>>.Failure(
                    "Failed to retrieve countries."
                );
            }
        }


        public async Task<ApiResponse<List<CountryDtos>>> GetCountriesDetails()
        {
            try
            {
                var countries = await _repository.GetCountries();

                return ApiResponse<List<CountryDtos>>.Success(
                    "Countries retrieved successfully.",
                    countries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving countries.");

                return ApiResponse<List<CountryDtos>>.Failure(
                    "Failed to retrieve countries.");
            }
        }

        public async Task<ApiResponse<CountryDtos>> GetCountryById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return ApiResponse<CountryDtos>.Failure(
                        "Invalid country ID.");
                }

                var country = await _repository.GetCountryById(id);

                if (country == null)
                {
                    return ApiResponse<CountryDtos>.Failure(
                        "Country not found.");
                }

                return ApiResponse<CountryDtos>.Success(
                    "Country retrieved successfully.",
                    country);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving country with ID {CountryId}.", id);

                return ApiResponse<CountryDtos>.Failure(
                    "Failed to retrieve country.");
            }
        }

        public async Task<ApiResponse<int>> CreateCountry(CountryRequest request)
        {
            try
            {
                if (request == null)
                {
                    return ApiResponse<int>.Failure(
                        "Country details are required.");
                }

                request.CountryName = request.CountryName?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(request.CountryName))
                {
                    return ApiResponse<int>.Failure(
                        "Country name is required.");
                }

                if (await _repository.CountryExists(request.CountryName))
                {
                    return ApiResponse<int>.Failure(
                        "Country name already exists.");
                }

                int countryId = await _repository.InsertCountry(request);

                return ApiResponse<int>.Success(
                    "Country created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating country.");

                return ApiResponse<int>.Failure(
                    "Failed to create country.");
            }
        }

        public async Task<ApiResponse<bool>> UpdateCountry(
            int id,
            CountryRequest request)
        {
            try
            {
                if (id <= 0)
                {
                    return ApiResponse<bool>.Failure(
                        "Invalid country ID.");
                }

                if (request == null)
                {
                    return ApiResponse<bool>.Failure(
                        "Country details are required.");
                }

                request.CountryName = request.CountryName?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(request.CountryName))
                {
                    return ApiResponse<bool>.Failure(
                        "Country name is required.");
                }

                var existingCountry = await _repository.GetCountryById(id);

                if (existingCountry == null)
                {
                    return ApiResponse<bool>.Failure(
                        "Country not found.");
                }

                if (await _repository.CountryExists(request.CountryName, id))
                {
                    return ApiResponse<bool>.Failure(
                        "Country name already exists.");
                }

                int rowsAffected = await _repository.UpdateCountry(id, request);

                if (rowsAffected == 0)
                {
                    return ApiResponse<bool>.Failure(
                        "Country could not be updated.");
                }

                return ApiResponse<bool>.Success(
                    "Country updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating country with ID {CountryId}.", id);

                return ApiResponse<bool>.Failure(
                    "Failed to update country.");
            }
        }

        public async Task<ApiResponse<bool>> DeleteCountry(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return ApiResponse<bool>.Failure(
                        "Invalid country ID.");
                }

                var existingCountry = await _repository.GetCountryById(id);

                if (existingCountry == null)
                {
                    return ApiResponse<bool>.Failure(
                        "Country not found.");
                }

                int rowsAffected = await _repository.DeleteCountry(id);

                if (rowsAffected == 0)
                {
                    return ApiResponse<bool>.Failure(
                        "Country could not be deleted.");
                }

                return ApiResponse<bool>.Success(
                    "Country deleted successfully.",
                    true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting country with ID {CountryId}.", id);

                return ApiResponse<bool>.Failure(
                    "Failed to delete country.");
            }
        }



    }
}