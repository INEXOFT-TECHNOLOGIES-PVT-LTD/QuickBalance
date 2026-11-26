using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Repository.Master;

namespace QuickBalance.API.Services
{
    public class LocationGroupService : ILocationGroupService
    {
        private readonly GdwGrpRepository _repository;
        private readonly ILogger<LocationGroupService> _logger;

        public LocationGroupService(
            GdwGrpRepository repository,
            ILogger<LocationGroupService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        // =========================================================
        // CREATE
        // =========================================================

        public async Task<ApiResponse<int>> CreateAsync(
            LocationGroupMasterRequest request,
            int userId)
        {
            try
            {
                string validation = ValidateRequest(request);

                if (!string.IsNullOrEmpty(validation))
                    return ApiResponse<int>.Failure(validation);

                if (userId <= 0)
                    return ApiResponse<int>.Failure(
                        "Invalid user information.");

                string groupName = request.GroupName.Trim();

                if (await _repository.NameExistsAsync(groupName))
                {
                    return ApiResponse<int>.Failure(
                        "Location group name already exists.");
                }

                int groupId = await _repository.CreateAsync(
                    request,
                    userId);

                if (groupId <= 0)
                {
                    return ApiResponse<int>.Failure(
                        "Location group could not be created.");
                }

                return ApiResponse<int>.Success(
                    "Location group created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating location group.");

                // Unexpected error: rethrow so the controller answers 500.
                throw;
            }
        }

        // =========================================================
        // GET ALL
        // =========================================================

        public async Task<ApiResponse<IReadOnlyList<LocationGroupDto>>>
            GetAllAsync(
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

                return ApiResponse<IReadOnlyList<LocationGroupDto>>.SuccessWithPagination(
                    "Location groups retrieved successfully.",
                    result.Data,
                    result.Pagination);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving location groups.");

                // Unexpected error: rethrow so the controller answers 500.
                throw;
            }
        }

        // =========================================================
        // GET BY ID
        // =========================================================

        public async Task<ApiResponse<LocationGroupDto>> GetByIdAsync(
            int id)
        {
            try
            {
                if (id <= 0)
                {
                    return ApiResponse<LocationGroupDto>.Failure(
                        "Invalid location group ID.");
                }

                var result = await _repository.GetByIdAsync(id);

                if (result == null)
                {
                    return ApiResponse<LocationGroupDto>.Failure(
                        "Location group not found.");
                }

                return ApiResponse<LocationGroupDto>.Success(
                    "Location group retrieved successfully.",
                    result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving location group {GroupId}.",
                    id);

                // Unexpected error: rethrow so the controller answers 500.
                throw;
            }
        }

        // =========================================================
        // UPDATE
        // =========================================================

        public async Task<ApiResponse<int>> UpdateAsync(
            int id,
            LocationGroupMasterRequest request,
            int userId)
        {
            try
            {
                if (id <= 0)
                {
                    return ApiResponse<int>.Failure(
                        "Invalid location group ID.");
                }

                string validation = ValidateRequest(request);

                if (!string.IsNullOrEmpty(validation))
                    return ApiResponse<int>.Failure(validation);

                if (userId <= 0)
                {
                    return ApiResponse<int>.Failure(
                        "Invalid user information.");
                }

                var existing = await _repository.GetByIdAsync(id);

                if (existing == null)
                {
                    return ApiResponse<int>.Failure(
                        "Location group not found.");
                }

                string groupName = request.GroupName.Trim();

                if (await _repository.NameExistsAsync(
                    groupName,
                    id))
                {
                    return ApiResponse<int>.Failure(
                        "Location group name already exists.");
                }

                int affected = await _repository.UpdateAsync(
                    id,
                    request);

                if (affected <= 0)
                {
                    return ApiResponse<int>.Failure(
                        "Location group could not be updated because it no longer exists.");
                }

                return ApiResponse<int>.Success(
                    "Location group updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating location group {GroupId}.",
                    id);

                // Unexpected error: rethrow so the controller answers 500.
                throw;
            }
        }



        public async Task<ApiResponse<int>> DeleteAsync(
         int id,
         int userId)
        {
            try
            {
                if (id <= 0)
                {
                    return ApiResponse<int>.Failure(
                        "Invalid location group ID.");
                }

                var deleteState = await _repository.GetDeleteStateAsync(id);

                if (deleteState == null)
                {
                    return ApiResponse<int>.Failure(
                        "Location group not found.");
                }

                if (deleteState == true)
                {
                    return ApiResponse<int>.Failure(
                        "Location group is already deleted.");
                }

                if (await _repository.HasLocationsAsync(id))
                {
                    return ApiResponse<int>.Failure(
                        "Location group is used by one or more locations and cannot be deleted.");
                }

                var affected = await _repository.DeleteAsync(
                    id, userId);

                if (!affected)
                {
                    return ApiResponse<int>.Failure(
                        "Location group not found.");
                }

                return ApiResponse<int>.Success(
                    "Location group deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error deleting location group {GroupId}.",
                    id);

                // Unexpected errors are not business failures: rethrow so the
                // controller can answer HTTP 500 instead of 400.
                throw;
            }
        }

        // =========================================================
        // ACTIVE GROUPS
        // =========================================================

        public async Task<ApiResponse<IReadOnlyList<LocationGroupDropdownDto>>>
            GetActiveAsync()
        {
            try
            {
                var result = await _repository.GetActiveAsync();

                return ApiResponse<IReadOnlyList<LocationGroupDropdownDto>>.Success(
                    "Active location groups retrieved successfully.",
                    result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving active location groups.");

                return ApiResponse<IReadOnlyList<LocationGroupDropdownDto>>.Failure(
                    "Failed to retrieve active location groups.");
            }
        }

        // =========================================================
        // SEARCH
        // =========================================================

        public async Task<ApiResponse<IReadOnlyList<LocationGroupDropdownDto>>>
            SearchAsync(string? search)
        {
            try
            {
                var result = await _repository.SearchAsync(search);

                return ApiResponse<IReadOnlyList<LocationGroupDropdownDto>>.Success(
                    "Location groups retrieved successfully.",
                    result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error searching location groups.");

                return ApiResponse<IReadOnlyList<LocationGroupDropdownDto>>.Failure(
                    "Failed to search location groups.");
            }
        }

        // =========================================================
        // DROPDOWN
        // =========================================================

        public async Task<ApiResponse<IReadOnlyList<LocationGroupDropdownDto>>>
            GetDropdownAsync()
        {
            try
            {
                var result = await _repository.GetDropdownAsync();

                return ApiResponse<IReadOnlyList<LocationGroupDropdownDto>>.Success(
                    "Location group dropdown retrieved successfully.",
                    result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving location group dropdown.");

                return ApiResponse<IReadOnlyList<LocationGroupDropdownDto>>.Failure(
                    "Failed to retrieve location group dropdown.");
            }
        }

        // =========================================================
        // VALIDATION
        // =========================================================

        private static string ValidateRequest(
            LocationGroupMasterRequest? request)
        {
            if (request == null)
                return "Location group request is required.";

            if (string.IsNullOrWhiteSpace(request.GroupName))
                return "Location group name is required.";

            if (request.GroupName.Trim().Length > 100)
                return "Location group name cannot exceed 100 characters.";

            return string.Empty;
        }
    }
}
