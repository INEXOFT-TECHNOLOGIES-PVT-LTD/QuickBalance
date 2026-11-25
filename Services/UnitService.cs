using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Respons_DTO;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Repository.Master;

namespace QuickBalance.API.Services;

public sealed class UnitService : IUnitInterface
{
    private readonly PrUnitMasDB _db;
    private readonly ILogger<UnitService> _logger;

    public UnitService(
        PrUnitMasDB db,
        ILogger<UnitService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    private static void Validate(UnitMasterRequest request)
    {
        if (request == null)
            throw new ArgumentException("Invalid unit request.");

        if (string.IsNullOrWhiteSpace(request.UnitName))
            throw new ArgumentException("Unit name is required.");

        if (request.UnitName.Trim().Length > 50)
            throw new ArgumentException(
                "Unit name cannot exceed 50 characters.");

        if (!string.IsNullOrWhiteSpace(request.SecondName) &&
            request.SecondName.Trim().Length > 50)
            throw new ArgumentException(
                "Second name cannot exceed 50 characters.");

        if (!string.IsNullOrWhiteSpace(request.FormalName) &&
            request.FormalName.Trim().Length > 50)
            throw new ArgumentException(
                "Formal name cannot exceed 50 characters.");
    }

    private static void Normalize(UnitMasterRequest request)
    {
        request.UnitName = request.UnitName.Trim();
        request.SecondName = request.SecondName?.Trim();
        request.FormalName = request.FormalName?.Trim();
    }

    // =========================================================
    // CREATE
    // =========================================================

    public async Task<ApiResponse<int>> CreateAsync(
        UnitMasterRequest request, int userId)
    {
        try
        {
            Validate(request);
            Normalize(request);

            if (userId <= 0)
                return ApiResponse<int>.Failure(
                    "Invalid user ID.");

            if (await _db.NameExistsAsync(
                request.UnitName, userId))
            {
                return ApiResponse<int>.Failure(
                    "unit with this name already exists.");
            }

            var id = await _db.InsertAsync(request, userId);

            if (id <= 0)
                return ApiResponse<int>.Failure(
                    "Failed to create the unit. Please try again.");

            return ApiResponse<int>.Success(
                "Unit created successfully.");
        }
        catch (ArgumentException ex)
        {
            return ApiResponse<int>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Unit.");

            return ApiResponse<int>.Failure(
                "An error occurred while creating the Unit. Please try again later.");
        }
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<ApiResponse<UnitMasterResponse>> GetByIdAsync(
        int id, int userId)
    {
        try
        {
            if (id <= 0)
                return ApiResponse<UnitMasterResponse>.Failure(
                    "Invalid Unit ID.");

            if (userId <= 0)
                return ApiResponse<UnitMasterResponse>.Failure(
                    "Invalid user ID.");

            var result = await _db.GetByIdAsync(id, userId);

            if (result is null)
                return ApiResponse<UnitMasterResponse>.Failure(
                    "Unit not found.");

            return ApiResponse<UnitMasterResponse>.Success(
                "Unit retrieved successfully.", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving Unit {Id}.", id);

            return ApiResponse<UnitMasterResponse>.Failure(
                "Unable to retrieve Unit.");
        }
    }

    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<ApiResponse<IReadOnlyList<UnitMasterResponse>>>
        GetAllAsync(
            int page,
            int pageSize,
            string? search,
            int userId,
            bool? active = null)
    {
        try
        {
            if (userId <= 0)
                return ApiResponse<IReadOnlyList<UnitMasterResponse>>
                    .Failure("Invalid user ID.");

            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var result = await _db.GetAllAsync(
                page, pageSize, search, userId, active);

            return ApiResponse<IReadOnlyList<UnitMasterResponse>>.SuccessWithPagination(
                "Units retrieved successfully.",
                result.Data,
                result.Pagination);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Units.");

            return ApiResponse<IReadOnlyList<UnitMasterResponse>>.Failure(
                "Unable to retrieve Units.");
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<ApiResponse<bool>> UpdateAsync(int id, UnitMasterRequest request, int userId)
    {
        try
        {
            if (id <= 0)
                return ApiResponse<bool>.Failure(
                    "Invalid Unit ID.");

            if (userId <= 0)
                return ApiResponse<bool>.Failure(
                    "Invalid user ID.");

            Validate(request);
            Normalize(request);

            var existing = await _db.GetByIdAsync(id, userId);

            if (existing is null)
                return ApiResponse<bool>.Failure(
                    "Unit not found.");

            if (await _db.NameExistsAsync(
                request.UnitName, userId, id))
            {
                return ApiResponse<bool>.Failure(
                    "Unit name already exists.");
            }

            var rows = await _db.UpdateAsync(
                id, request, userId);

            if (rows == 0)
                return ApiResponse<bool>.Failure(
                    "Unit was not updated.");

            return ApiResponse<bool>.Success(
                "Unit updated successfully.");
        }
        catch (ArgumentException ex)
        {
            return ApiResponse<bool>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error updating Unit {Id}.", id);

            return ApiResponse<bool>.Failure(
                "An error occurred while updating the Unit.");
        }
    }



    public async Task<ApiResponse<int>> DeleteAsync(int id, int userId)
    {
        try
        {
            if (id <= 0)
            {
                return ApiResponse<int>.Failure(
                    "Unit of Meassure  Id.");
            }

            var existing = await _db.GetByIdAsync(id, userId);

            if (existing == null)
            {
                return ApiResponse<int>.Failure(
                    "Unit of Meassure Id not found.");
            }

            var affected = await _db.DeleteAsync(id, userId);

            if (!affected)
            {
                return ApiResponse<int>.Failure(
                    "Unit of Meassure Id not found.");
            }

            return ApiResponse<int>.Success(
                "Unit of Meassure delete successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error  Unit of Meassure {GroupId}.",
                id);

            return ApiResponse<int>.Failure(
                "Failed to delete Unit of Meassure Id.");
        }
    }
    // =========================================================
    // ACTIVE UNITS
    // =========================================================

    public async Task<ApiResponse<IReadOnlyList<UnitDropdownResponse>>>
        GetActiveAsync(int userId)
    {
        try
        {
            if (userId <= 0)
                return ApiResponse<IReadOnlyList<UnitDropdownResponse>>
                    .Failure("Invalid user ID.");

            var result = await _db.GetActiveAsync(userId);

            return ApiResponse<IReadOnlyList<UnitDropdownResponse>>.Success(
                "Active units retrieved successfully.", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active Units.");

            return ApiResponse<IReadOnlyList<UnitDropdownResponse>>
                .Failure("Unable to retrieve active Units.");
        }
    }
}