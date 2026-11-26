using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Repository.Master;

namespace QuickBalance.API.Services
{
    public class LocationService : ILocationService
    {
        private readonly GdwMasRepository _repository;
        private readonly ILogger<LocationService> _logger;

        public LocationService(
            GdwMasRepository repository,
            ILogger<LocationService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        // =========================================================
        // CREATE
        // =========================================================

        public async Task<ApiResponse<int>> CreateAsync(
            LocationMasterRequest request,
            int userId)
        {
            try
            {
                if (request == null)
                    return ApiResponse<int>.Failure(
                        "Request data is required.");

                if (string.IsNullOrWhiteSpace(request.LocationName))
                    return ApiResponse<int>.Failure(
                        "Location name must not be blank.");

                if (request.GroupId <= 0)
                    return ApiResponse<int>.Failure(
                        "Select valid location group.");

                if (request.StockAccountId <= 0)
                    return ApiResponse<int>.Failure(
                        "Select valid stock ledger.");

                if (userId <= 0)
                    return ApiResponse<int>.Failure(
                        "Invalid logged-in user.");

                string locationName = request.LocationName.Trim();

                if (await _repository.NameExistsAsync(locationName))
                    return ApiResponse<int>.Failure(
                        "Location name already exists.");

                if (!await _repository.GroupExistsAsync(request.GroupId))
                    return ApiResponse<int>.Failure(
                        "Selected location group does not exist.");

                if (!await _repository.StockAccountExistsAsync(
                    request.StockAccountId))
                {
                    return ApiResponse<int>.Failure(
                        "Selected stock ledger does not exist.");
                }

                int locationId = await _repository.CreateAsync(
                    request,
                    userId);

                if (locationId <= 0)
                    return ApiResponse<int>.Failure(
                        "Failed to create location.");

                return ApiResponse<int>.Success(
                    "Location created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating location.");

                return ApiResponse<int>.Failure(
                    "An error occurred while creating location.");
            }
        }

        // =========================================================
        // GET ALL
        // =========================================================

        public async Task<ApiResponse<IReadOnlyList<LocationListItemDto>>> GetAllAsync(
            int page,
            int pageSize,
            string? search,
            bool? active = null)
        {
            try
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);

                var result = await _repository.GetAllAsync(
                    page,
                    pageSize,
                    search,
                    active);

                return ApiResponse<IReadOnlyList<LocationListItemDto>>.SuccessWithPagination(
                    "Locations retrieved successfully.",
                    result.Data,
                    result.Pagination);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving locations.");

                return ApiResponse<IReadOnlyList<LocationListItemDto>>.Failure(
                    "An error occurred while retrieving locations.");
            }
        }

        // =========================================================
        // GET BY ID
        // =========================================================

        public async Task<ApiResponse<LocationDto>> GetByIdAsync(int id)
        {
            try
            {
                if (id <= 0)
                    return ApiResponse<LocationDto>.Failure(
                        "Invalid location ID.");

                var location = await _repository.GetByIdAsync(id);

                if (location == null)
                    return ApiResponse<LocationDto>.Failure(
                        "Location not found.");

                return ApiResponse<LocationDto>.Success(
                    "Location retrieved successfully.",
                    location);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving location {LocationId}.", id);

                return ApiResponse<LocationDto>.Failure(
                    "An error occurred while retrieving location.");
            }
        }

        // =========================================================
        // UPDATE
        // =========================================================

        public async Task<ApiResponse<int>> UpdateAsync(
            int id,
            LocationMasterRequest request,
            int userId)
        {
            try
            {
                if (id <= 0)
                    return ApiResponse<int>.Failure(
                        "Invalid location ID.");

                if (request == null)
                    return ApiResponse<int>.Failure(
                        "Request data is required.");

                if (string.IsNullOrWhiteSpace(request.LocationName))
                    return ApiResponse<int>.Failure(
                        "Location name must not be blank.");

                if (request.GroupId <= 0)
                    return ApiResponse<int>.Failure(
                        "Select valid location group.");

                if (request.StockAccountId <= 0)
                    return ApiResponse<int>.Failure(
                        "Select valid stock ledger.");

                if (userId <= 0)
                    return ApiResponse<int>.Failure(
                        "Invalid logged-in user.");

                var existing = await _repository.GetByIdAsync(id);

                if (existing == null)
                    return ApiResponse<int>.Failure(
                        "Location not found.");

                string locationName = request.LocationName.Trim();

                if (await _repository.NameExistsAsync(locationName, id))
                    return ApiResponse<int>.Failure(
                        "Location name already exists.");

                if (!await _repository.GroupExistsAsync(request.GroupId))
                    return ApiResponse<int>.Failure(
                        "Selected location group does not exist.");

                if (!await _repository.StockAccountExistsAsync(
                    request.StockAccountId))
                {
                    return ApiResponse<int>.Failure(
                        "Selected stock ledger does not exist.");
                }

                int affected = await _repository.UpdateAsync(id, request);

                if (affected <= 0)
                    return ApiResponse<int>.Failure(
                        "Failed to update location.");

                return ApiResponse<int>.Success(
                    "Location updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating location {LocationId}.", id);

                return ApiResponse<int>.Failure(
                    "An error occurred while updating location.");
            }
        }

        // delete



        public async Task<ApiResponse<int>> DeleteAsync(int id, int userId)
        {
            try
            {
                if (id <= 0)
                    return ApiResponse<int>.Failure(
                        "Invalid location ID.");

              
                var existing = await _repository.GetByIdAsync(id);

                if (existing == null)
                    return ApiResponse<int>.Failure(
                        "Location not found.");

                var affected = await _repository.DeleteAsync(id, userId);

                if (!affected)
                    return ApiResponse<int>.Failure(
                        "Location not found.");

                return ApiResponse<int>.Success(
                    "Location deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleted location {LocationId}.", id);

                return ApiResponse<int>.Failure(
                    "An error occurred while delete location.");
            }
        }

        // =========================================================
        // ACTIVE LOCATIONS
        // =========================================================

        public async Task<ApiResponse<IReadOnlyList<LocationActiveDto>>>
            GetActiveAsync()
        {
            try
            {
                var result = await _repository.GetActiveAsync();

                return ApiResponse<IReadOnlyList<LocationActiveDto>>.Success(
                    "Active locations retrieved successfully.",
                    result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active locations.");

                return ApiResponse<IReadOnlyList<LocationActiveDto>>.Failure(
                    "An error occurred while retrieving active locations.");
            }
        }

        // =========================================================
        // SEARCH
        // =========================================================

        public async Task<ApiResponse<IReadOnlyList<LocationDropdownDto>>>
            SearchAsync(string? search)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(search))
                {
                    return ApiResponse<IReadOnlyList<LocationDropdownDto>>.Success(
                        "Locations retrieved successfully.",
                        Array.Empty<LocationDropdownDto>());
                }

                var result = await _repository.SearchAsync(search.Trim());

                return ApiResponse<IReadOnlyList<LocationDropdownDto>>.Success(
                    "Locations retrieved successfully.",
                    result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching locations.");

                return ApiResponse<IReadOnlyList<LocationDropdownDto>>.Failure(
                    "An error occurred while searching locations.");
            }
        }

        // =========================================================
        // DROPDOWN
        // =========================================================

        public async Task<ApiResponse<IReadOnlyList<LocationDropdownDto>>>
            GetDropdownAsync()
        {
            try
            {
                var result = await _repository.GetDropdownAsync();

                return ApiResponse<IReadOnlyList<LocationDropdownDto>>.Success(
                    "Location dropdown retrieved successfully.",
                    result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving location dropdown.");

                return ApiResponse<IReadOnlyList<LocationDropdownDto>>.Failure(
                    "An error occurred while retrieving location dropdown.");
            }
        }

        // =========================================================
        // STOCK ACCOUNT USAGE
        // =========================================================

        public async Task<ApiResponse<bool>> IsStockAccountUsedAsync(
            int accountId)
        {
            try
            {
                if (accountId <= 0)
                    return ApiResponse<bool>.Failure(
                        "Invalid stock account ID.");

                bool isUsed =
                    await _repository.IsStockAccountUsedAsync(accountId);

                return ApiResponse<bool>.Success(
                    "Stock account usage checked successfully.",
                    isUsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error checking stock account usage for {AccountId}.",
                    accountId);

                return ApiResponse<bool>.Failure(
                    "An error occurred while checking stock account usage.");
            }
        }
    }
}