
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.ItemCategory;
using QuickBalance.API.DTOs.Respons_DTO;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Repository.Master;
using QuickBalance.API.Repository.Master.QuickBalance.API.Repository.Master;

namespace QuickBalance.API.Services;

public sealed class ItemCategoryService : IItemCategoryInterface
{
    private readonly ItemCategoryDB _db;
    private readonly ILogger<ItemCategoryService> _logger;

    public ItemCategoryService(
        ItemCategoryDB db,
        ILogger<ItemCategoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    private static void Validate(ItemCategoryRequest request)
    {
        if (request == null)
            throw new ArgumentException("Request data is required.");

        if (string.IsNullOrWhiteSpace(request.CategoryName))
            throw new ArgumentException("Category name is required.");
    }

    public async Task<ApiResponse<int>> CreateAsync(
        ItemCategoryRequest request,
        int userId)
    {
        try
        {
            Validate(request);

            request.CategoryName = request.CategoryName.Trim();

            if (await _db.NameExistsAsync(request.CategoryName))
            {
                return ApiResponse<int>.Failure(
                    "Category name already exists.");
            }

            var id = await _db.InsertAsync(request, userId);

            if (id <= 0)
            {
                return ApiResponse<int>.Failure(
                    "Category could not be created.");
            }

            return ApiResponse<int>.Success(
                "Category created successfully.", id);
        }
        catch (ArgumentException ex)
        {
            return ApiResponse<int>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating item category.");

            return ApiResponse<int>.Failure(
                "An error occurred while creating the category.");
        }
    }

    public async Task<ApiResponse<ItemCategoryResponse>> GetByIdAsync(
        int id)
    {
        try
        {
            if (id <= 0)
            {
                return ApiResponse<ItemCategoryResponse>.Failure(
                    "Invalid category ID.");
            }

            var result = await _db.GetByIdAsync(id);

            if (result is null)
            {
                return ApiResponse<ItemCategoryResponse>.Failure(
                    "Category not found.");
            }

            return ApiResponse<ItemCategoryResponse>.Success(
                "Category retrieved successfully.", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving item category {Id}.", id);

            return ApiResponse<ItemCategoryResponse>.Failure(
                "Unable to retrieve category.");
        }
    }

    public async Task<ApiResponse<IReadOnlyList<ItemCategoryResponse>>>
        GetAllAsync(int page, int pageSize, string? search, bool? active = null)
    {
        try
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var result = await _db.GetAllAsync(
                page, pageSize, search, active);

            return ApiResponse<IReadOnlyList<ItemCategoryResponse>>.SuccessWithPagination(
                "Categories retrieved successfully.",
                result.Data,
                result.Pagination);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving item categories.");

            return ApiResponse<IReadOnlyList<ItemCategoryResponse>>.Failure(
                "Unable to retrieve categories.");
        }
    }

    public async Task<ApiResponse<bool>> UpdateAsync(
        int id,
        ItemCategoryRequest request,
        int userId)
    {
        try
        {
            if (id <= 0)
            {
                return ApiResponse<bool>.Failure(
                    "Invalid category ID.");
            }

            Validate(request);

            request.CategoryName = request.CategoryName.Trim();

            var existing = await _db.GetByIdAsync(id);

            if (existing is null)
            {
                return ApiResponse<bool>.Failure(
                    "Category not found.");
            }

            if (await _db.NameExistsAsync(request.CategoryName, id))
            {
                return ApiResponse<bool>.Failure(
                    "Category name already exists.");
            }

            var rows = await _db.UpdateAsync(id, request);

            if (rows == 0)
            {
                return ApiResponse<bool>.Failure(
                    "Category was not updated.");
            }

            return ApiResponse<bool>.Success(
                "Category updated successfully.");
        }
        catch (ArgumentException ex)
        {
            return ApiResponse<bool>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error updating item category {Id}.", id);

            return ApiResponse<bool>.Failure(
                "An error occurred while updating the category.");
        }
    }



    public async Task<ApiResponse<int>> DeleteAsync(int id, int userId)
    {
        try
        {
            if (id <= 0)
            {
                return ApiResponse<int>.Failure(
                    "Invalid item category ID.");
            }

            // No ACTIVE / DELFLG filter: inactive items may be deleted,
            // and an already-deleted item gets its own message.
            var deleteFlag = await _db.GetDeleteFlagAsync(id);

            if (deleteFlag is null)
            {
                return ApiResponse<int>.Failure(
                    "Item category not found.");
            }

            if (deleteFlag == 1)
            {
                return ApiResponse<int>.Failure(
                    "Item category is already deleted.");
            }

            var affected = await _db.DeleteAsync(
                id, userId);

            if (!affected)
            {
                // Row was deleted by another request between the check and the update.
                return ApiResponse<int>.Failure(
                    "Item category is already deleted.");
            }

            return ApiResponse<int>.Success(
                "Item category deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error deleting item category {ItemCategoryId}.",
                id);

            return ApiResponse<int>.Failure(
                "Failed to delete Item Category Id.");
        }
    }

    public async Task<ApiResponse<IReadOnlyList<ItemCategoryResponse>>>
        GetActiveAsync()
    {
        try
        {
            var result = await _db.GetActiveAsync();

            return ApiResponse<IReadOnlyList<ItemCategoryResponse>>.Success(
                "Active categories retrieved successfully.", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active categories.");

            return ApiResponse<IReadOnlyList<ItemCategoryResponse>>.Failure(
                "Unable to retrieve active categories.");
        }
    }
}

//using QuickBalance.API.CLS;
//using QuickBalance.API.DTOs.ItemCategory;
//using QuickBalance.API.DTOs.Respons_DTO;
//using QuickBalance.API.Interfaces;
//using QuickBalance.API.Repository.Master;
//using QuickBalance.API.Repository.Master.QuickBalance.API.Repository.Master;

//namespace QuickBalance.API.Services
//{
//    public sealed class ItemCategoryService : IItemCategoryInterface
//    {
//        private readonly ItemCategoryDB _db;
//        private readonly ILogger<ItemCategoryService> _logger;

//        public ItemCategoryService(
//            ItemCategoryDB itemCategoryDB,
//            ILogger<ItemCategoryService> logger)
//        {
//            _db = itemCategoryDB;
//            _logger = logger;
//        }

//        // CREATE
//        private static void Validate(ItemCategoryRequest request)
//        {
//            if (request == null)
//                throw new ArgumentException("Invalid category request.");

//            if (string.IsNullOrWhiteSpace(request.CategoryName))
//                throw new ArgumentException("Category name is required.");
//        }

//        // CREATE
//        public async Task<ApiResponse<int>> CreateAsync(
//            ItemCategoryRequest request,
//            int userId)
//        {
//            try
//            {
//                Validate(request);

//                request.CategoryName = request.CategoryName.Trim();
//                request.Alias = request.Alias?.Trim();
//                request.SecondName = request.SecondName?.Trim();

//                if (await _db.NameExistsAsync(request.CategoryName))
//                    return ApiResponse<int>.Failure(
//                        "Category name already exists.");

//                var id = await _db.InsertAsync(request, userId);

//                if (id <= 0)
//                    return ApiResponse<int>.Failure(
//                        "Category could not be created.");

//                return ApiResponse<int>.Success(
//                    "Category created successfully.", id);
//            }
//            catch (ArgumentException ex)
//            {
//                return ApiResponse<int>.Failure(ex.Message);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error creating Item category.");

//                return ApiResponse<int>.Failure(
//                    "An error occurred while creating the Item category.");
//            }
//        }

//        // GET BY ID
//        public async Task<ApiResponse<ItemCategoryResponse>> GetByIdAsync(int id)
//        {
//            try
//            {
//                if (id <= 0)
//                    return ApiResponse<ItemCategoryResponse>.Failure(
//                        "Invalid Item category ID.");

//                var result = await _db.GetByIdAsync(id);

//                if (result is null)
//                    return ApiResponse<ItemCategoryResponse>.Failure(
//                        "Item category not found.");

//                return ApiResponse<ItemCategoryResponse>.Success(
//                    "Item category retrieved successfully.", result);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex,
//                    "Error retrieving Item category {Id}.", id);

//                return ApiResponse<ItemCategoryResponse>.Failure(
//                    "Unable to retrieve Item category.");
//            }
//        }

//        // GET ALL
//        public async Task<ApiResponse<IReadOnlyList<ItemCategoryResponse>>>
//            GetAllAsync(int page, int pageSize, string? search)
//        {
//            try
//            {
//                page = Math.Max(page, 1);
//                pageSize = Math.Clamp(pageSize, 1, 100);

//                var result = await _db.GetAllAsync(page, pageSize, search);

//                return ApiResponse<IReadOnlyList<ItemCategoryResponse>>.Success(
//                    "Item categories retrieved successfully.",
//                    result.Data,
//                    result.Pagination);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error retrieving Item categories.");

//                return ApiResponse<IReadOnlyList<ItemCategoryResponse>>.Failure(
//                    "Unable to retrieve Item categories.");
//            }
//        }

//        // UPDATE
//        public async Task<ApiResponse<bool>> UpdateAsync(
//            int id,
//            ItemCategoryRequest request,
//            int userId)
//        {
//            try
//            {
//                if (id <= 0)
//                    return ApiResponse<bool>.Failure(
//                        "Invalid Item category ID.");

//                Validate(request);

//                var existing = await _db.GetByIdAsync(id);

//                if (existing is null)
//                    return ApiResponse<bool>.Failure(
//                        "Item category not found.");

//                request.CategoryName = request.CategoryName.Trim();
//                request.Alias = request.Alias?.Trim();
//                request.SecondName = request.SecondName?.Trim();

//                if (await _db.NameExistsAsync(request.CategoryName, id))
//                    return ApiResponse<bool>.Failure(
//                        "Item category name already exists.");

//                var rows = await _db.UpdateAsync(id, request);

//                if (rows == 0)
//                    return ApiResponse<bool>.Failure(
//                        "Item category was not updated.");

//                return ApiResponse<bool>.Success(
//                    "Item category updated successfully.");
//            }
//            catch (ArgumentException ex)
//            {
//                return ApiResponse<bool>.Failure(ex.Message);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex,
//                    "Error updating Item category {Id}.", id);

//                return ApiResponse<bool>.Failure(
//                    "An error occurred while updating the Item category.");
//            }
//        }

//        // GET ACTIVE CATEGORIES
//        public async Task<ApiResponse<IReadOnlyList<ItemCategoryResponse>>>
//            GetActiveAsync()
//        {
//            try
//            {
//                var result = await _db.GetActiveAsync();

//                return ApiResponse<IReadOnlyList<ItemCategoryResponse>>.Success(
//                    "Active item categories retrieved successfully.", result);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error retrieving active Item categories.");

//                return ApiResponse<IReadOnlyList<ItemCategoryResponse>>.Failure(
//                    "Unable to retrieve active Item categories.");
//            }
//        }
//    }
//}