using Microsoft.Extensions.Logging;
using QuickBalance.API.CLS;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Repository;
using QuickBalance.API.Repository.Master;

namespace QuickBalance.API.Services;

public class BomService : IBomService
{
    private readonly BomRepository _repository;
    private readonly ProductDB _productRepository;
    private readonly AccMasDB _account;
    private readonly ILogger<BomService> _logger;

    public BomService(BomRepository repository, ProductDB productDB , AccMasDB account, ILogger<BomService> logger)
    {
        _repository = repository;
        _logger = logger;
        _productRepository = productDB;
        _account = account;
    }

    // ---------------------------------------------------------
    // CREATE bill of materials
    // ---------------------------------------------------------

    //public async Task<ApiResponse<int>> CreateAsync(
    //    BomRequest request,
    //    int userId)
    //{
    //    try
    //    {
    //        string validation = ValidateRequest(request);

    //        if (!string.IsNullOrEmpty(validation))
    //            return ApiResponse<int>.Failure(validation);

    //        if (userId <= 0)
    //            return ApiResponse<int>.Failure("Invalid user information.");

    //        string bomName = request.BomName.Trim();

    //        if (await _repository.CheckDuplicateAsync("BOMNAME", bomName))
    //            return ApiResponse<int>.Failure("bill of materials name already exists.");

    //        if (!string.IsNullOrWhiteSpace(request.Alias) &&
    //            await _repository.CheckDuplicateAsync("ALIAS", request.Alias.Trim()))
    //        {
    //            return ApiResponse<int>.Failure("bill of materials alias already exists.");
    //        }
    //        var checkProductExist = await _productRepository.GetByIdAsync(request.PrCode);

    //        int bomCode = await _repository.CreateMasterAsync(request, userId);

    //        if (bomCode <= 0)
    //            return ApiResponse<int>.Failure("Failed to create bill of materials master.");

    //        foreach (var item in request.Items)
    //        {
    //            int result = await _repository.CreateItemAsync(bomCode, item);

    //            if (result <= 0)
    //            {
    //                await CleanupAsync(bomCode);
    //                return ApiResponse<int>.Failure("Failed to save bill of materials item details.");
    //            }
    //        }

    //        foreach (var info in request.AdditionalInfo)
    //        {
    //            if (info.Amount <= 0)
    //                continue;

    //            int result = await _repository.CreateAdditionalInfoAsync(bomCode, info);

    //            if (result <= 0)
    //            {
    //                await CleanupAsync(bomCode);
    //                return ApiResponse<int>.Failure(
    //                    "Failed to save bill of materials additional information.");
    //            }
    //        }

    //        return ApiResponse<int>.Success(
    //            "bill of materials created successfully.");
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error creating bill of materials.");
    //        return ApiResponse<int>.Failure("Failed to create bill of materials.");
    //    }
    //}

    public async Task<ApiResponse<int>> CreateAsync(
    BomRequest request,
    int userId)
    {
        try
        {
            // ============================================
            // 1. REQUEST VALIDATION
            // ============================================

            string validation = ValidateRequest(request);

            if (!string.IsNullOrEmpty(validation))
                return ApiResponse<int>.Failure(validation);

            if (userId <= 0)
                return ApiResponse<int>.Failure(
                    "Invalid user information.");

            string bomName = request.BomName.Trim();

            // ============================================
            // 2. DUPLICATE BOM NAME CHECK
            // ============================================

            if (await _repository.CheckDuplicateAsync(
                "BOMNAME", bomName))
            {
                return ApiResponse<int>.Failure(
                    "Bill of materials name already exists.");
            }

            // ============================================
            // 3. DUPLICATE ALIAS CHECK
            // ============================================

            if (!string.IsNullOrWhiteSpace(request.Alias) &&
                await _repository.CheckDuplicateAsync(
                    "ALIAS", request.Alias.Trim()))
            {
                return ApiResponse<int>.Failure(
                    "Bill of materials alias already exists.");
            }

            // ============================================
            // ACCOUNT EXISTENCE CHECK - ACCMAS
            // BOMD.CODE = ACCMAS.ID
            // ============================================

            foreach (var info in request.AdditionalInfo)
            {
                if (info?.AccountId == null || info.AccountId <= 0)
                {
                    return ApiResponse<int>.Failure(
                        "Invalid account ID in BOM additional information.");
                }

                bool accountExists = await _repository.CheckAccountExistsAsync(
                    info.AccountId);

                if (!accountExists)
                {
                    return ApiResponse<int>.Failure(
                        $"Account ID {info.AccountId} does not exist in ACCMAS.");
                }
            }            // ============================================
            // 4. MAIN PRODUCT EXISTENCE CHECK
            // ============================================

            var mainProduct = await _productRepository.GetByIdAsync(
                request.PrCode);

            if (mainProduct == null)
            {
                return ApiResponse<int>.Failure(
                    $"Invalid BOM product ID: {request.PrCode}.");
            }

            // ============================================
            // 5. BOM ITEM PRODUCT EXISTENCE CHECK
            // ============================================

            foreach (var item in request.Items)
            {
                if (item.ProductId <= 0)
                {
                    return ApiResponse<int>.Failure(
                        "Invalid product ID in BOM items.");
                }

                var product = await _productRepository.GetByIdAsync(
                    item.ProductId);

                if (product == null)
                {
                    return ApiResponse<int>.Failure(
                        $"Product ID {item.ProductId} does not exist in BOM items.");
                }
            }

            // ============================================
            // 6. INSERT BOM MASTER
            // ============================================

            int bomCode = await _repository.CreateMasterAsync(
                request, userId);

            if (bomCode <= 0)
            {
                return ApiResponse<int>.Failure(
                    "Failed to create bill of materials master.");
            }

            // ============================================
            // 7. INSERT BOM ITEMS
            // ============================================

            foreach (var item in request.Items)
            {
                int result = await _repository.CreateItemAsync(
                    bomCode, item);

                if (result <= 0)
                {
                    await CleanupAsync(bomCode, userId);

                    return ApiResponse<int>.Failure(
                        "Failed to save bill of materials item details.");
                }
            }

            // ============================================
            // 8. INSERT ADDITIONAL INFORMATION
            // ============================================

            foreach (var info in request.AdditionalInfo)
            {
                if (info.Amount <= 0)
                    continue;

                int result = await _repository.CreateAdditionalInfoAsync(
                    bomCode, info);

                if (result <= 0)
                {
                    await CleanupAsync(bomCode, userId);

                    return ApiResponse<int>.Failure(
                        "Failed to save bill of materials additional information.");
                }
            }

            // ============================================
            // 9. SUCCESS
            // ============================================

            return ApiResponse<int>.Success(
                "Bill of materials created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating bill of materials.");

            return ApiResponse<int>.Failure(
                "Failed to create bill of materials.");
        }
    }

    // ---------------------------------------------------------
    // GET ALL bill of materialss
    // ---------------------------------------------------------

    public async Task<ApiResponse<IReadOnlyList<BomListDetailsDto>>> GetAllAsync(
        int page,
        int pageSize,
        string? search,
        bool? active = null)
    {
        try
        {
            if (page <= 0 || pageSize <= 0)
            {
                return ApiResponse<IReadOnlyList<BomListDetailsDto>>.Failure(
                    "Page and page size must be greater than zero.");
            }
            if (pageSize > 100)
            {
                return ApiResponse<IReadOnlyList<BomListDetailsDto>>.Failure(
                    "Page size must not exceed 100.");
            }

            page = Math.Max(page, 0);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var result = await _repository.GetAllAsync(
                page,
                pageSize,
                search,
                active);

            return ApiResponse<IReadOnlyList<BomListDetailsDto>>.SuccessWithPagination(
                "bill of materials retrieved successfully.", result.Data,
                result.Pagination);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bill of materials list.");
            return ApiResponse<IReadOnlyList<BomListDetailsDto>>.Failure(
                "Failed to retrieve bill of materialss.");
        }
    }

    // ---------------------------------------------------------
    // GET bill of materials BY ID
    // ---------------------------------------------------------

    public async Task<ApiResponse<BomDetailsDto>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return ApiResponse<BomDetailsDto>.Failure("Invalid bill of materials id.");

            var master = await _repository.GetMasterByIdAsync(id);

            if (master == null)
                return ApiResponse<BomDetailsDto>.Failure("bill of materials not found.");

            var items = await _repository.GetItemsByBomCodeAsync(id);
            var additionalInfo = await _repository.GetAdditionalInfoAsync(id);

            var result = new BomDetailsDto
            {
                bilofm_details = master,
                Items = items,
                AdditionalInfo = additionalInfo
            };

            return ApiResponse<BomDetailsDto>.Success(
                "bill of materials retrieved successfully.",result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bill of materials {BomId}.", id);
            return ApiResponse<BomDetailsDto>.Failure("Failed to retrieve bill of materials.");
        }
    }

    // ---------------------------------------------------------
    // UPDATE bill of materials
    // ---------------------------------------------------------

    public async Task<ApiResponse<int>> UpdateAsync(
        int id,
        BomRequest request,
        int userId)
    {
        try
        {
            if (id <= 0)
                return ApiResponse<int>.Failure("Invalid bill of materials id.");

            string validation = ValidateRequest(request);

            if (!string.IsNullOrEmpty(validation))
                return ApiResponse<int>.Failure(validation);

            if (userId <= 0)
                return ApiResponse<int>.Failure("Invalid user information.");

            var existing = await _repository.GetMasterByIdAsync(id);

            if (existing == null)
                return ApiResponse<int>.Failure("bill of materials not found.");

            if (await _repository.CheckDuplicateAsync(
                    "BOMNAME",
                    request.BomName.Trim(),
                    id))
            {
                return ApiResponse<int>.Failure("bill of materials name already exists.");
            }

            if (!string.IsNullOrWhiteSpace(request.Alias) &&
                await _repository.CheckDuplicateAsync(
                    "ALIAS",
                    request.Alias.Trim(),
                    id))
            {
                return ApiResponse<int>.Failure("bill of materials alias already exists.");
            }

            // ============================================
            // ACCOUNT EXISTENCE CHECK - ACCMAS
            // BOMD.CODE = ACCMAS.ID
            // ============================================

            foreach (var info in request.AdditionalInfo)
            {
                if (info?.AccountId == null || info.AccountId <= 0)
                {
                    return ApiResponse<int>.Failure(
                        "Invalid account ID in BOM additional information.");
                }

                bool accountExists = await _repository.CheckAccountExistsAsync(
                    info.AccountId);

                if (!accountExists)
                {
                    return ApiResponse<int>.Failure(
                        $"Account ID {info.AccountId} does not exist in ACCMAS.");
                }
            }            // ============================================
            // 4. MAIN PRODUCT EXISTENCE CHECK
            // ============================================

            var mainProduct = await _productRepository.GetByIdAsync(
                request.PrCode);

            if (mainProduct == null)
            {
                return ApiResponse<int>.Failure(
                    $"Invalid BOM product ID: {request.PrCode}.");
            }

            // ============================================
            // 5. BOM ITEM PRODUCT EXISTENCE CHECK
            // ============================================

            foreach (var item in request.Items)
            {
                if (item.ProductId <= 0)
                {
                    return ApiResponse<int>.Failure(
                        "Invalid product ID in BOM items.");
                }

                var product = await _productRepository.GetByIdAsync(
                    item.ProductId);

                if (product == null)
                {
                    return ApiResponse<int>.Failure(
                        $"Product ID {item.ProductId} does not exist in BOM items.");
                }
            }

            int updated = await _repository.UpdateMasterAsync(id, request, userId);

            if (updated <= 0)
                return ApiResponse<int>.Failure("Failed to update bill of materials master.");

            await _repository.DeleteAdditionalInfoAsync(id);
            await _repository.DeleteItemsByBomCodeAsync(id);

            foreach (var item in request.Items)
            {
                int result = await _repository.CreateItemAsync(id, item);

                if (result <= 0)
                    return ApiResponse<int>.Failure(
                        "Failed to update bill of materials item details.");
            }

            foreach (var info in request.AdditionalInfo)
            {
                if (info.Amount <= 0)
                    continue;

                int result = await _repository.CreateAdditionalInfoAsync(id, info);

                if (result <= 0)
                    return ApiResponse<int>.Failure(
                        "Failed to update bill of materials additional information.");
            }

            return ApiResponse<int>.Success(
                "bill of materials updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bill of materials {BomId}.", id);
            return ApiResponse<int>.Failure("Failed to update bill of materials.");
        }
    }

    // ---------------------------------------------------------
    // DELETE bill of materials
    // ---------------------------------------------------------

    public async Task<ApiResponse<bool>> DeleteAsync(int id, int userId)
    {
        try
        {
            if (id <= 0)
                return ApiResponse<bool>.Failure("Invalid bill of materials id.");

            if (userId <= 0)
                return ApiResponse<bool>.Failure("Invalid user information.");

            var existing = await _repository.GetMasterByIdAsync(id);

            if (existing == null)
                return ApiResponse<bool>.Failure("bill of materials not found.");

            // Child rows in BOMT and BOMD are kept; the BOM is hidden through BOMMAS.ISDELETED.
            int deleted = await _repository.DeleteMasterAsync(id, userId);

            if (deleted <= 0)
                return ApiResponse<bool>.Failure("bill of materials not found.");

            return ApiResponse<bool>.Success(
                "bill of materials deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting bill of materials {BomId}.", id);
            return ApiResponse<bool>.Failure("Failed to delete bill of materials.");
        }
    }

    // ---------------------------------------------------------
    // GET bill of materials ITEMS BY PRODUCT
    // ---------------------------------------------------------

    public async Task<ApiResponse<List<BomItemDto>>> GetByProductCodeAsync(
        int productId)
    {
        try
        {
            if (productId <= 0)
                return ApiResponse<List<BomItemDto>>.Failure(
                    "Invalid product id.");

            var result = await _repository.GetByProductCodeAsync(productId);
            if(result.Count <= 0)
                return ApiResponse<List<BomItemDto>>.Failure(
                    "bill of materials items not found.");

            return ApiResponse<List<BomItemDto>>.Success(
                "bill of materials items retrieved successfully.",result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving bill of materials items for product {ProductId}.",
                productId);

            return ApiResponse<List<BomItemDto>>.Failure(
                "Failed to retrieve bill of materials items.");
        }
    }

    // ---------------------------------------------------------
    // VALIDATION
    // ---------------------------------------------------------

    private static string ValidateRequest(BomRequest? request)
    {
        if (request == null)
            return "bill of materials request is required.";

        if (string.IsNullOrWhiteSpace(request.BomName))
            return "bill of materials name is required.";

        if (request.PrCode <= 0)
            return "Produced product is required.";

        if (request.Unit <= 0)
            return "Produced unit is required.";

        if (request.Qty <= 0)
            return "Produced quantity must be greater than zero.";

        if (IsYes(request.McGenFlg) && (!request.McGen.HasValue || request.McGen <= 0))
            return "Generated godown is required.";

        if (IsYes(request.McConsFlg) &&
            (!request.McCons.HasValue || request.McCons <= 0))
        {
            return "Consumed godown is required.";
        }

        if (request.Items == null)
            return "bill of materials items are required.";

        var consumedItems = request.Items
            .Where(x => string.Equals(
                x.Type?.Trim(),
                "C",
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (consumedItems.Count == 0)
            return "At least one consumed raw material is required.";

        foreach (var item in request.Items)
        {
            string type = item.Type?.Trim().ToUpperInvariant() ?? "C";

            if (type is not ("C" or "G"))
                return "bill of materials item type must be C or G.";

            if (item.ProductId <= 0)
                return "Product is required for all bill of materials items.";

            if (item.Quantity <= 0)
                return "bill of materials item quantity must be greater than zero.";

            if (item.UnitId <= 0)
                return "bill of materials item unit is required.";

            if (type == "C" && item.ProductId == request.PrCode)
                return "Consumed product cannot be the same as the produced product.";
        }

        if (request.AdditionalInfo == null)
            return "Additional information cannot be null.";

        foreach (var info in request.AdditionalInfo)
        {
            if (info.Amount < 0)
                return "Additional information amount cannot be negative.";

            if (info.Amount > 0 && info.AccountId <= 0)
                return "Account is required when additional information amount is greater than zero.";
        }

        return string.Empty;
    }

    private static bool IsYes(string? value)
    {
        return value?.Trim().ToUpperInvariant() is "Y" or "YES" or "TRUE" or "1";
    }

    private async Task CleanupAsync(int bomCode, int userId)
    {
        try
        {
            await _repository.DeleteAdditionalInfoAsync(bomCode);
            await _repository.DeleteItemsByBomCodeAsync(bomCode);
            await _repository.DeleteMasterAsync(bomCode, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to clean up incomplete bill of materials {BomCode}.",
                bomCode);
        }
    }
}