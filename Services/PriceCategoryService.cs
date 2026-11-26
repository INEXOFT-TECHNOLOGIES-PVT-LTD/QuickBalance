
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.PriceCategory;
using QuickBalance.API.DTOs.Respons_DTO;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Repository.Master;

namespace QuickBalance.API.Services;

public sealed class PriceCategoryService : IPriceCategoryInterface
{
    private readonly PriceCategoryRepository _db;
    private readonly ILogger<PriceCategoryService> _logger;

    private static readonly string[] AllowedStructures =
    [
        "Discount Based on %",
        "Absolute Value",
        "Direct Price"
    ];

    public PriceCategoryService(
        PriceCategoryRepository db,
        ILogger<PriceCategoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    private static void Validate(PriceCategoryRequest request)
    {
        if (request == null)
            throw new ArgumentException("Request data is required.");

        if (string.IsNullOrWhiteSpace(request.CategoryName))
            throw new ArgumentException("Category name is required.");

        if (request.Levels == null || request.Levels.Count == 0)
            throw new ArgumentException(
                "At least one price category level is required.");

        foreach (var level in request.Levels)
        {
            if (string.IsNullOrWhiteSpace(level.DiscountStructure))
                throw new ArgumentException(
                    "Discount structure is required.");

            if (!AllowedStructures.Contains(
                level.DiscountStructure.Trim(),
                StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"Invalid discount structure. Sample values: {string.Join(", ", AllowedStructures)}");
            }

            if (level.LevelFrom <= 0)
                throw new ArgumentException(
                    "Level From must be greater than zero.");

            if (level.LevelTo <= 0)
                throw new ArgumentException(
                    "Level To must be greater than zero.");

            if (level.LevelFrom > level.LevelTo)
                throw new ArgumentException(
                    "Level From cannot be greater than Level To.");

            if (level.DiscountStructure.Equals(
                "Discount Based on %",
                StringComparison.OrdinalIgnoreCase)
                && level.DiscountPercent < 0)
            {
                throw new ArgumentException(
                    "Discount percentage cannot be negative.");
            }

            if (level.DiscountStructure.Equals(
                "Absolute Value",
                StringComparison.OrdinalIgnoreCase)
                && level.EffectiveValue < 0)
            {
                throw new ArgumentException(
                    "Effective value cannot be negative.");
            }

            if (level.DiscountStructure.Equals(
                "Direct Price",
                StringComparison.OrdinalIgnoreCase)
                && level.AppliedPrice < 0)
            {
                throw new ArgumentException(
                    "Applied price cannot be negative.");
            }
        }
    }

    // CREATE
    public async Task<ApiResponse<int>> CreateAsync(
        PriceCategoryRequest request,
        int userId)
    {
        try
        {
            Validate(request);

            request.CategoryName = request.CategoryName.Trim();

            if (await _db.NameExistsAsync(request.CategoryName))
            {
                return ApiResponse<int>.Failure(
                    "Price category already exists.");
            }

            int code = await _db.InsertAsync(request, userId);

            if (code <= 0)
            {
                return ApiResponse<int>.Failure(
                    "Failed to create price category.");
            }

            return ApiResponse<int>.Success(
                "Price category created successfully.");
        }
        catch (ArgumentException ex)
        {
            return ApiResponse<int>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating price category.");

            return ApiResponse<int>.Failure(
                "An error occurred while creating price category.");
        }
    }

    // GET BY ID
    public async Task<ApiResponse<PriceCategoryResponse>> GetByIdAsync(
        int id)
    {
        try
        {
            if (id <= 0)
            {
                return ApiResponse<PriceCategoryResponse>.Failure(
                    "Invalid price category ID.");
            }

            var result = await _db.GetByIdAsync(id);

            if (result is null)
            {
                return ApiResponse<PriceCategoryResponse>.Failure(
                    "Price category not found.");
            }

            return ApiResponse<PriceCategoryResponse>.Success(
                "Price category retrieved successfully.", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving price category {Id}.", id);

            return ApiResponse<PriceCategoryResponse>.Failure(
                "Unable to retrieve price category.");
        }
    }

    // GET ALL
    public async Task<ApiResponse<IReadOnlyList<PriceCategoryResponse>>>
        GetAllAsync(int page, int pageSize, string? search)
    {
        try
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var result = await _db.GetAllAsync(
                page, pageSize, search);

            return ApiResponse<IReadOnlyList<PriceCategoryResponse>>.SuccessWithPagination(
                "Price categories retrieved successfully.",
                result.Data,
                result.Pagination);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving price categories.");

            return ApiResponse<IReadOnlyList<PriceCategoryResponse>>.Failure(
                "Unable to retrieve price categories.");
        }
    }

    // UPDATE
    public async Task<ApiResponse<bool>> UpdateAsync(
        int id,
        PriceCategoryRequest request,
        int userId)
    {
        try
        {
            if (id <= 0)
                return ApiResponse<bool>.Failure(
                    "Invalid price category ID.");

            Validate(request);

            request.CategoryName = request.CategoryName.Trim();

            var existing = await _db.GetByIdAsync(id);

            if (existing is null)
            {
                return ApiResponse<bool>.Failure(
                    "Price category not found.");
            }

            if (await _db.NameExistsAsync(request.CategoryName, id))
            {
                return ApiResponse<bool>.Failure(
                    "Price category already exists.");
            }

            int rows = await _db.UpdateAsync(id, request, userId);

            if (rows <= 0)
            {
                return ApiResponse<bool>.Failure(
                    "Failed to update price category.");
            }

            return ApiResponse<bool>.Success(
                "Price category updated successfully.");
        }
        catch (ArgumentException ex)
        {
            return ApiResponse<bool>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error updating price category {Id}.", id);

            return ApiResponse<bool>.Failure(
                "An error occurred while updating price category.");
        }
    }

    // DELETE
    public async Task<ApiResponse<bool>> DeleteAsync(int id, int userId)
    {
        try
        {
            if (id <= 0)
                return ApiResponse<bool>.Failure(
                    "Invalid price category ID.");

            var existing = await _db.GetByIdAsync(id);

            if (existing is null)
            {
                return ApiResponse<bool>.Failure(
                    "Price category not found.");
            }

            int rows = await _db.DeleteAsync(id, userId);

            if (rows <= 0)
            {
                return ApiResponse<bool>.Failure(
                    "Price category not found.");
            }

            return ApiResponse<bool>.Success(
                "Price category deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error deleting price category {Id}.", id);

            return ApiResponse<bool>.Failure(
                "Unable to delete price category.");
        }
    }
}
