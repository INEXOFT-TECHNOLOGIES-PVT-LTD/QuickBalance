
using QuickBalance.API.CLS;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Respons_DTO;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Repository.Master;

namespace QuickBalance.API.Services;

public sealed class ItemGroupService : IItemGroupInterface
{
    private readonly ItemGroupDBs _db;
    private readonly ILogger<ItemGroupService> _logger;

    public ItemGroupService(ItemGroupDBs db,ILogger<ItemGroupService> logger)
    {
        _db = db;
        _logger = logger;
    }

    private static void Validate(ItemGroupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.GroupName))
            throw new ArgumentException("Group name is required.");

        if ((request.GroupCode ?? 0) <= 0)
            throw new ArgumentException("Group code must be greater than zero.");

        if (!request.IsPrimaryGroup && (request.PrimaryGroupId ?? 0) <= 0)
            throw new ArgumentException("Please select a primary group.");

        if (request.TagWithAccount &&
            ((request.StockAccountId ?? 0) <= 0 ||
             (request.PurchaseAccountId ?? 0) <= 0 ||
             (request.SalesAccountId ?? 0) <= 0))
        {
            throw new ArgumentException(
                "Stock, purchase and sales accounts are required.");
        }
    }
    public async Task<ApiResponse<int>> CreateAsync(
    ItemGroupRequest request,
    int userId)
    {
        try
        {
            Validate(request);

            // Check duplicate group name
            if (await _db.GroupNameExistsAsync(request.GroupName.Trim()))
            {
                return ApiResponse<int>.Failure(
                    "Item group name already exists.");
            }

            // Check duplicate group code
            if (await _db.GroupCodeExistsAsync(request.GroupCode!.Value))
            {
                return ApiResponse<int>.Failure(
                    "Item group code already exists.");
            }

            // Validate accounts only when account tagging is enabled
            if (request.TagWithAccount)
            {
                var purchaseAccount = await _db.GetGroupByPurAcc(
                    request.PurchaseAccountId ?? 0);

                if (purchaseAccount==null || purchaseAccount==0)
                {
                    return ApiResponse<int>.Failure(
                        "Purchase account not found.");
                }

                var salesAccount = await _db.GetGroupBySaleAcc(
                    request.SalesAccountId ?? 0);

                if (salesAccount == null || salesAccount==0)
                {
                    return ApiResponse<int>.Failure(
                        "Sales account not found.");
                }
                var stockAccount = await _db.GetGroupByStockAcc(
                    request.StockAccountId ?? 0);

                if (stockAccount == null || stockAccount == 0)
                {
                    return ApiResponse<int>.Failure(
                        "Stock account not found.");
                }

                // Add stock account validation here if your repository
                // has a corresponding stock-account lookup method.
            }

            // Validate selected primary group only for child groups
            if (!request.IsPrimaryGroup)
            {
                if ((request.PrimaryGroupId ?? 0) <= 0)
                {
                    return ApiResponse<int>.Failure(
                        "Please select a primary group.");
                }

                var primaryGroup = await _db.GetByIdAsync(
                    request.PrimaryGroupId!.Value);

                if (primaryGroup == null)
                {
                    return ApiResponse<int>.Failure(
                        "Primary group not found.");
                }

                if (!primaryGroup.IsPrimaryGroup)
                {
                    return ApiResponse<int>.Failure(
                        "Selected group is not a primary group.");
                }

                if (!primaryGroup.IsActive)
                {
                    return ApiResponse<int>.Failure(
                        "Selected primary group is not active.");
                }
            }



            // Repository assigns its own generated ID as PRIMGRPID
            // when IsPrimaryGroup = true.
            var id = await _db.InsertAsync(request, userId);

            if (id <= 0)
            {
                return ApiResponse<int>.Failure(
                    "Item group could not be created.");
            }

            return ApiResponse<int>.Success(
                "Item group created successfully.");
        }
        catch (ArgumentException ex)
        {
            return ApiResponse<int>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Item group.");

            return ApiResponse<int>.Failure(
                "An error occurred while creating the Item group.");
        }
    }
    //public async Task<ApiResponse<int>> CreateAsync(
    //    ItemGroupRequest request,
    //    int userId)
    //{
    //    try
    //    {
    //        Validate(request);

    //        if (await _db.GroupNameExistsAsync(request.GroupName))
    //            return ApiResponse<int>.Failure(
    //                "Item group name already exists.");

    //        if (await _db.GroupCodeExistsAsync(request.GroupCode))
    //            return ApiResponse<int>.Failure(
    //                "Item group code already exists.");
    //        var purchaseAccount = await _db.GetGroupByPurAcc(request.PurchaseAccount);
    //        if(purchaseAccount == null)
    //        {
    //            return ApiResponse<int>.Failure(
    //                "Purchase account not found.");
    //        }
    //        var salesAccount = await _db.GetGroupBySaleAcc(request.SalesAccount);
    //        if(salesAccount == null)
    //        {
    //            return ApiResponse<int>.Failure(
    //                "Sales account not found.");
    //        }
    //        if(request.IsPrimaryGroup == true && request.GroupCode > 0)
    //        {

    //           var primaryGroup = await _db.GetByIdAsync(request.PrimaryGroupId);
    //            if(primaryGroup == null)
    //            {
    //                return ApiResponse<int>.Failure(
    //                    "Primary group not found.");
    //            }
    //        }

    //        var id = await _db.InsertAsync(request, userId);

    //        if (id <= 0)
    //            return ApiResponse<int>.Failure(
    //                "Item group could not be created.");

    //        return ApiResponse<int>.Success(
    //            "Item group created successfully.", id);
    //    }
    //    catch (ArgumentException ex)
    //    {
    //        return ApiResponse<int>.Failure(ex.Message);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error creating Item group.");

    //        return ApiResponse<int>.Failure(
    //            "An error occurred while creating the Item group.");
    //    }
    //}

    public async Task<ApiResponse<ItemGroupResponse>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return ApiResponse<ItemGroupResponse>.Failure(
                    "Invalid Item group ID.");

            var result = await _db.GetByIdAsync(id);

            if (result is null)
                return ApiResponse<ItemGroupResponse>.Failure(
                    "Item group not found.");

            return ApiResponse<ItemGroupResponse>.Success(
                "Item group retrieved successfully.", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving Item group {Id}.", id);

            return ApiResponse<ItemGroupResponse>.Failure(
                "Unable to retrieve Item group.");
        }
    }


    public async Task<ApiResponse<IReadOnlyList<ItemGroupListItemDto>>>GetAllAsync(int page, int pageSize, string? search, bool? active)
    {
        try
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var result = await _db.GetAllAsync(page, pageSize, search, active);

            return ApiResponse<IReadOnlyList<ItemGroupListItemDto>>.SuccessWithPagination(
                "Item groups retrieved successfully.",
                result.Data,
                result.Pagination
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Item group");

            return ApiResponse<IReadOnlyList<ItemGroupListItemDto>>.Failure(
                "Unable to retrieve Item group.");
        }
    }





    public async Task<ApiResponse<bool>> UpdateAsync(
    int id,
    ItemGroupRequest request,
    int userId)
    {
        try
        {
            if (id <= 0)
            {
                return ApiResponse<bool>.Failure(
                    "Invalid item group ID.");
            }

            Validate(request);

            var existing = await _db.GetByIdAsync(id);

            if (existing is null)
            {
                return ApiResponse<bool>.Failure(
                    "Item group not found.");
            }

            // Duplicate group name validation
            if (await _db.GroupNameExistsAsync(request.GroupName.Trim(), id))
            {
                return ApiResponse<bool>.Failure(
                    "Item group name already exists.");
            }

            // Duplicate group code validation
            if (await _db.GroupCodeExistsAsync(request.GroupCode!.Value, id))
            {
                return ApiResponse<bool>.Failure(
                    "Item group code already exists.");
            }

            // Account validation
            if (request.TagWithAccount)
            {
                var purchaseAccount = await _db.GetGroupByPurAcc(
                    request.PurchaseAccountId ?? 0);

                if (purchaseAccount == null || purchaseAccount == 0)
                {
                    return ApiResponse<bool>.Failure(
                        "Purchase account not found.");
                }

                var salesAccount = await _db.GetGroupBySaleAcc(
                    request.SalesAccountId ?? 0);

                if (salesAccount == null || salesAccount == 0)
                {
                    return ApiResponse<bool>.Failure(
                        "Sales account not found.");
                }



                var stockAccount = await _db.GetGroupByStockAcc(
                    request.StockAccountId ?? 0);

                if (stockAccount == null || stockAccount == 0)
                {
                    return ApiResponse<bool>.Failure(
                        "Stock account not found.");
                }
            }

            // Primary group validation
            if (request.IsPrimaryGroup)
            {
                // A primary group points to itself.
                request.PrimaryGroupId = id;
            }
            else
            {
                if ((request.PrimaryGroupId ?? 0) <= 0)
                {
                    return ApiResponse<bool>.Failure(
                        "Please select a primary group.");
                }

                if (request.PrimaryGroupId == id)
                {
                    return ApiResponse<bool>.Failure(
                        "An item group cannot be its own primary group.");
                }

                var primaryGroup = await _db.GetByIdAsync(
                    request.PrimaryGroupId!.Value);

                if (primaryGroup == null)
                {
                    return ApiResponse<bool>.Failure(
                        "Primary group not found.");
                }

                if (!primaryGroup.IsPrimaryGroup)
                {
                    return ApiResponse<bool>.Failure(
                        "Selected group is not a primary group.");
                }

                if (!primaryGroup.IsActive)
                {
                    return ApiResponse<bool>.Failure(
                        "Selected primary group is not active.");
                }
            }

            var rows = await _db.UpdateAsync(id, request, userId);

            if (rows <= 0)
            {
                return ApiResponse<bool>.Failure(
                    "Item group was not updated.");
            }

            return ApiResponse<bool>.Success(
                "Item group updated successfully.");
        }
        catch (ArgumentException ex)
        {
            return ApiResponse<bool>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error updating Item group {Id}.",
                id);

            return ApiResponse<bool>.Failure(
                "An error occurred while updating the Item group.");
        }
    }

    //public async Task<ApiResponse<bool>> UpdateAsync(int id,ItemGroupRequest request,int userId)
    //{
    //    try
    //    {
    //        if (id <= 0)
    //            return ApiResponse<bool>.Failure(
    //                "Invalid item group ID.");

    //        Validate(request);

    //        var existing = await _db.GetByIdAsync(id);

    //        if (existing is null)
    //            return ApiResponse<bool>.Failure(
    //                "Item group not found.");

    //        if (await _db.GroupNameExistsAsync(request.GroupName, id))
    //            return ApiResponse<bool>.Failure(
    //                "Item group not name already exists.");

    //        if (await _db.GroupCodeExistsAsync(request.GroupCode, id))
    //            return ApiResponse<bool>.Failure(
    //                "Item group not code already exists.");

    //        var rows = await _db.UpdateAsync(id, request, userId);

    //        if (rows == 0)
    //            return ApiResponse<bool>.Failure(
    //                "Item group not was not updated.");

    //        return ApiResponse<bool>.Success(
    //            "Item group updated successfully.");
    //    }
    //    catch (ArgumentException ex)
    //    {
    //        return ApiResponse<bool>.Failure(ex.Message);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex,
    //            "Error updating Item group {Id}.", id);

    //        return ApiResponse<bool>.Failure(
    //            "An error occurred while updating the Item group.");
    //    }
    //}

    public async Task<ApiResponse<bool>> DeleteAsync(int id, int userId)
    {
        try
        {
            if (id <= 0)
                return ApiResponse<bool>.Failure(
                    "Invalid Item group  ID.");

            var rows = await _db.DeleteAsync(id, userId);

            if (rows == 0)
                return ApiResponse<bool>.Failure(
                    "Item group not found.");

            return ApiResponse<bool>.Success(
                "Item group deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error deleting  Item group {Id}.", id);

            return ApiResponse<bool>.Failure(
                "Unable to delete Item group .");
        }
    }

    public async Task<ApiResponseWithoutPagination<IReadOnlyList<ItemGroupResponse>>>
        GetPrimaryGroupsAsync(string? search = null)
    {
        try
        {
            var result = await _db.GetPrimaryGroupsAsync(search);

            return ApiResponseWithoutPagination<IReadOnlyList<ItemGroupResponse>>.Success(
                "Primary groups retrieved successfully.",
                result ?? new List<ItemGroupResponse>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving primary groups.");

            return ApiResponseWithoutPagination<IReadOnlyList<ItemGroupResponse>>.Failure(
                "Unable to retrieve primary groups.");
        }
    }

    public async Task<ApiResponse<IReadOnlyList<ItemGroupResponse>>>
        GetPrimaryGroupsPagedAsync(int page, int pageSize, string? search)
    {
        try
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var result = await _db.GetPrimaryGroupsPagedAsync(page, pageSize, search);

            return ApiResponse<IReadOnlyList<ItemGroupResponse>>.SuccessWithPagination(
                "Primary groups retrieved successfully.",
                result.Data,
                result.Pagination);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving primary groups.");

            return ApiResponse<IReadOnlyList<ItemGroupResponse>>.Failure(
                "Unable to retrieve primary groups.");
        }
    }
}
