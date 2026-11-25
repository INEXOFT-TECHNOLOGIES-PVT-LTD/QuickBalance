using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Brand;
using QuickBalance.API.DTOs.Respons_DTO;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Repository.Master;
using QuickBalance.API.Repository.Master.QuickBalance.API.Repository.Master;

namespace QuickBalance.API.Services;

public sealed class BrandService : IBrandInterface
{
    private readonly BrandDB _db;
    private readonly ILogger<BrandService> _logger;

    public BrandService(
        BrandDB db,
        ILogger<BrandService> logger)
    {
        _db = db;
        _logger = logger;
    }

    private static void Validate(BrandRequest request)
    {
        if (request == null)
            throw new ArgumentException("Invalid brand request.");

        if (string.IsNullOrWhiteSpace(request.BrandName))
            throw new ArgumentException("Brand name is required.");
    }

    private static void Normalize(BrandRequest request)
    {
        request.BrandName = request.BrandName.Trim();
        request.SecondName = request.SecondName?.Trim();
        request.Alias = request.Alias?.Trim();
    }

    private async Task<string?> GetDuplicateMessageAsync(
        BrandRequest request, int? excludeId = null)
    {
        if (await _db.NameExistsAsync(request.BrandName, excludeId))
            return "Brand name already exists.";

        if (!string.IsNullOrWhiteSpace(request.SecondName) &&
            await _db.SecondNameExistsAsync(request.SecondName, excludeId))
            return "Second name already exists.";

        if (!string.IsNullOrWhiteSpace(request.Alias) &&
            await _db.AliasExistsAsync(request.Alias, excludeId))
            return "Alias already exists.";

        return null;
    }

    public async Task<ApiResponse<int>> CreateAsync(BrandRequest request)
    {
        try
        {
            Validate(request);
            Normalize(request);

            var duplicateMessage = await GetDuplicateMessageAsync(request);

            if (duplicateMessage != null)
                return ApiResponse<int>.Failure(duplicateMessage);

            var id = await _db.InsertAsync(request);

            if (id <= 0)
                return ApiResponse<int>.Failure(
                    "Brand could not be created.");

            return ApiResponse<int>.Success(
                "Brand created successfully.");
        }
        catch (ArgumentException ex)
        {
            return ApiResponse<int>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Brand.");

            return ApiResponse<int>.Failure(
                "An error occurred while creating the Brand.");
        }
    }

    public async Task<ApiResponse<BrandResponse>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return ApiResponse<BrandResponse>.Failure(
                    "Invalid Brand ID.");

            var result = await _db.GetByIdAsync(id);

            if (result is null)
                return ApiResponse<BrandResponse>.Failure(
                    "Brand not found.");

            return ApiResponse<BrandResponse>.Success(
                "Brand retrieved successfully.",result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Brand {Id}.", id);

            return ApiResponse<BrandResponse>.Failure(
                "Unable to retrieve Brand.");
        }
    }

    public async Task<ApiResponse<IReadOnlyList<BrandResponse>>> GetAllAsync(
        int page, int pageSize, string? search)
    {
        try
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var result = await _db.GetAllAsync(page, pageSize, search);

            return ApiResponse<IReadOnlyList<BrandResponse>>.SuccessWithPagination(
                "Brands retrieved successfully.",
                result.Data,
                result.Pagination);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Brands.");

            return ApiResponse<IReadOnlyList<BrandResponse>>.Failure(
                "Unable to retrieve Brands.");
        }
    }

    public async Task<ApiResponse<bool>> UpdateAsync(
        int id, BrandRequest request)
    {
        try
        {
            if (id <= 0)
                return ApiResponse<bool>.Failure("Invalid Brand ID.");

            Validate(request);
            Normalize(request);

            var existing = await _db.GetByIdAsync(id);

            if (existing is null)
                return ApiResponse<bool>.Failure("Brand not found.");

            var duplicateMessage =
                await GetDuplicateMessageAsync(request, id);

            if (duplicateMessage != null)
                return ApiResponse<bool>.Failure(duplicateMessage);

            var rows = await _db.UpdateAsync(id, request);

            if (rows == 0)
                return ApiResponse<bool>.Failure(
                    "Brand was not updated.");

            return ApiResponse<bool>.Success(
                "Brand updated successfully.");
        }
        catch (ArgumentException ex)
        {
            return ApiResponse<bool>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Brand {Id}.", id);

            return ApiResponse<bool>.Failure(
                "An error occurred while updating the Brand.");
        }
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, int userId)
    {
        try
        {
            if (id <= 0)
                return ApiResponse<bool>.Failure("Invalid Brand ID.");

            var existing = await _db.GetByIdAsync(id);

            if (existing is null)
                return ApiResponse<bool>.Failure("Brand not found.");

            var rows = await _db.DeleteAsync(id, userId);

            if (rows == 0)
                return ApiResponse<bool>.Failure(
                    "Brand not found.");

            return ApiResponse<bool>.Success(
                "Brand deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Brand {Id}.", id);

            return ApiResponse<bool>.Failure(
                "Unable to delete Brand.");
        }
    }

    public async Task<ApiResponse<IReadOnlyList<BrandResponse>>> GetActiveAsync()
    {
        try
        {
            var result = await _db.GetActiveAsync();

            return ApiResponse<IReadOnlyList<BrandResponse>>.Success(
                "Brands retrieved successfully.", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Brands.");

            return ApiResponse<IReadOnlyList<BrandResponse>>.Failure(
                "Unable to retrieve Brands.");
        }
    }
}