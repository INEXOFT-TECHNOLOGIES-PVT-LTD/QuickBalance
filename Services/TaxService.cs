using QuickBalance.API.CLS;
using QuickBalance.API.DTOs.Respons_DTO;
using QuickBalance.API.DTOs.Tax;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Repository.Master;

namespace QuickBalance.API.Services;

public sealed class TaxService : ITaxInterface
{
    private readonly TaxMasDB _db;

    private readonly ItemGroupDBs _account;

    private readonly ILogger<TaxService> _logger;

    public TaxService( 
        TaxMasDB db,
        ItemGroupDBs account,
        ILogger<TaxService> logger)
    {
        _db = db;
        _account = account;
        _logger = logger;
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    private static void Validate(TaxMasterRequest request)
    {
        if (request == null)
            throw new ArgumentException("Invalid tax request.");

        // Collect every problem so the client sees them all in one response.
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.TaxName))
            errors.Add("Tax name is required.");

        if (request.SalesAccountId is not > 0)
            errors.Add("Sales account is required.");

        if (request.SalesReturnAccountId is not > 0)
            errors.Add("Sales return account is required.");

        if (request.PurchaseAccountId is not > 0)
            errors.Add("Purchase account is required.");

        if (request.PurchaseReturnAccountId is not > 0)
            errors.Add("Purchase return account is required.");

        if (request.VatAccountId is not > 0)
            errors.Add("VAT account is required.");

        if (request.TaxAccountId is not > 0)
            errors.Add("Tax account is required.");

        if (request.TaxPercentage < 0)
            errors.Add("Tax percentage cannot be negative.");
        else if (request.TaxPercentage > 100)
            errors.Add("Tax percentage cannot exceed 100.");

        if (errors.Count > 0)
            throw new ArgumentException(string.Join(" ", errors));
    }

    private static void Normalize(TaxMasterRequest request)
    {
        request.TaxName = request.TaxName.Trim();
        request.SecondName = request.SecondName?.Trim();

        if (string.IsNullOrWhiteSpace(request.TaxType))
            request.TaxType = "T";
        else
            request.TaxType = request.TaxType.Trim();
    }

    // =========================================================
    // CREATE
    // =========================================================

    public async Task<ApiResponse<int>> CreateAsync(
        TaxMasterRequest request, int userId)
    {
        try
        {
            Validate(request);
            Normalize(request);

            if (userId <= 0)
                return ApiResponse<int>.Failure("Invalid user ID.");

            if (await _db.NameExistsAsync(request.TaxName))
                return ApiResponse<int>.Failure(
                    "Tax name already exists.");

            // Validate() guarantees every account ID has a value > 0.
            var salesAccountExists = await _account.GetAccountById(request.SalesAccountId!.Value);
            if(salesAccountExists==0)
                return ApiResponse<int>.Failure(
                    "Sales account does not exist.");
            var purchaseAccountExists = await _account.GetAccountById(request.PurchaseAccountId!.Value);
            if(purchaseAccountExists == 0)
                return ApiResponse<int>.Failure(
                    "Purchase account does not exist.");

            var salesReturnAccountExists = await _account.GetAccountById(request.SalesReturnAccountId!.Value);
            if(salesReturnAccountExists == 0)
                return ApiResponse<int>.Failure(
                    "Sales return account does not exist.");

            var purchaseReturnAccountExists = await _account.GetAccountById(request.PurchaseReturnAccountId!.Value);
            if (purchaseReturnAccountExists == 0)
                return ApiResponse<int>.Failure(
                    "Purchase return account does not exist.");
            var vatAccountExistsSale = await _account.GetAccountById(request.VatAccountId!.Value);
            if (vatAccountExistsSale == 0)
                return ApiResponse<int>.Failure(
                    "VAT (Sales) account does not exist.");
            var taxAccountExistsPurchase = await _account.GetAccountById(request.TaxAccountId!.Value);
            if (taxAccountExistsPurchase == 0)
                return ApiResponse<int>.Failure(
                    "Tax(Purchase) account does not exist.");

            var id = await _db.InsertAsync(request, userId);

            if (id <= 0)
                return ApiResponse<int>.Failure(
                    "Tax creation failed.");

            return ApiResponse<int>.Success(
                "Tax created successfully.", id);
        }
        catch (ArgumentException ex)
        {
            return ApiResponse<int>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Tax.");

            return ApiResponse<int>.Failure(
                "An error occurred while creating the Tax.");
        }
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<ApiResponse<TaxMasterResponse>> GetByIdAsync(
        int id)
    {
        try
        {
            if (id <= 0)
                return ApiResponse<TaxMasterResponse>.Failure(
                    "Invalid Tax ID.");

            var result = await _db.GetByIdAsync(id);

            if (result is null)
                return ApiResponse<TaxMasterResponse>.Failure(
                    "Tax not found.");

            return ApiResponse<TaxMasterResponse>.Success(
                "Tax retrieved successfully.", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving Tax {Id}.", id);

            return ApiResponse<TaxMasterResponse>.Failure(
                "Unable to retrieve Tax.");
        }
    }

    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<ApiResponse<IReadOnlyList<TaxMasterResponse>>>
        GetAllAsync(int page, int pageSize, string? search, bool? active = null)
    {
        try
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var result = await _db.GetAllAsync(
                page, pageSize, search, active);

            return ApiResponse<IReadOnlyList<TaxMasterResponse>>.SuccessWithPagination(
                "Taxes retrieved successfully.",
                result.Data,
                result.Pagination);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Taxes.");

            return ApiResponse<IReadOnlyList<TaxMasterResponse>>.Failure(
                "Unable to retrieve Taxes.");
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<ApiResponse<bool>> UpdateAsync(
        int id, TaxMasterRequest request, int userId)
    {
        try
        {
            if (id <= 0)
                return ApiResponse<bool>.Failure("Invalid Tax ID.");

            if (userId <= 0)
                return ApiResponse<bool>.Failure("Invalid user ID.");

            Validate(request);
            Normalize(request);

            var existing = await _db.GetByIdAsync(id);

            if (existing is null)
                return ApiResponse<bool>.Failure("Tax not found.");

            if (await _db.NameExistsAsync(request.TaxName, id))
                return ApiResponse<bool>.Failure(
                    "Tax name already exists.");

            // Validate() guarantees every account ID has a value > 0.
            var salesAccountExists = await _account.GetAccountById(request.SalesAccountId!.Value);
            if (salesAccountExists == 0)
                return ApiResponse<bool>.Failure(
                    "Sales account does not exist.");
            var purchaseAccountExists = await _account.GetAccountById(request.PurchaseAccountId!.Value);
            if (purchaseAccountExists == 0)
                return ApiResponse<bool>.Failure(
                    "Purchase account does not exist.");

            var salesReturnAccountExists = await _account.GetAccountById(request.SalesReturnAccountId!.Value);
            if (salesReturnAccountExists == 0)
                return ApiResponse<bool>.Failure(
                    "Sales return account does not exist.");

            var purchaseReturnAccountExists = await _account.GetAccountById(request.PurchaseReturnAccountId!.Value);
            if (purchaseReturnAccountExists == 0)
                return ApiResponse<bool>.Failure(
                    "Purchase return account does not exist.");
            var vatAccountExistsSale = await _account.GetAccountById(request.VatAccountId!.Value);
            if (vatAccountExistsSale == 0)
                return ApiResponse<bool>.Failure(
                    "VAT (Sales) account does not exist.");
            var taxAccountExistsPurchase = await _account.GetAccountById(request.TaxAccountId!.Value);
            if (taxAccountExistsPurchase == 0)
                return ApiResponse<bool>.Failure(
                    "Tax(Purchase) account does not exist.");

            var rows = await _db.UpdateAsync(
                id, request, userId);

            if (rows == 0)
                return ApiResponse<bool>.Failure(
                    "Tax update failed.");

            return ApiResponse<bool>.Success(
                "Tax updated successfully.");
        }
        catch (ArgumentException ex)
        {
            return ApiResponse<bool>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error updating Tax {Id}.", id);

            return ApiResponse<bool>.Failure(
                "An error occurred while updating the Tax.");
        }
    }

    // =========================================================
    // ACTIVE TAXES FOR ITEM MASTER
    // =========================================================


    public async Task<ApiResponse<int>> DeleteAsync(int id, int userId)
    {
        try
        {
            if (id <= 0)
            {
                return ApiResponse<int>.Failure(
                    "Invalid Tax Id.");
            }

            var existing = await _db.GetByIdAsync(id);

            if (existing == null)
            {
                return ApiResponse<int>.Failure(
                    "Tax Id not found.");
            }

            var affected = await _db.DeleteAsync(
                id, userId);

            if (!affected)
            {
                return ApiResponse<int>.Failure(
                    "Tax Id not found.");
            }

            return ApiResponse<int>.Success(
                "Tax  delete successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error updating Tax {GroupId}.",
                id);

            return ApiResponse<int>.Failure(
                "Failed to delete Tax Id.");
        }
    }





    public async Task<ApiResponse<IReadOnlyList<TaxLookupResponse>>>
        GetActiveForItemMasterAsync()
    {
        try
        {
            var result = await _db.GetActiveForItemMasterAsync();

            return ApiResponse<IReadOnlyList<TaxLookupResponse>>.Success(
                "Active item master taxes retrieved successfully.",
                result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving active item master taxes.");

            return ApiResponse<IReadOnlyList<TaxLookupResponse>>.Failure(
                "Unable to retrieve active item master taxes.");
        }
    }

    // =========================================================
    // ACTIVE ALL TAXES
    // =========================================================

    public async Task<ApiResponse<IReadOnlyList<TaxLookupResponse>>>
        GetActiveAllAsync()
    {
        try
        {
            var result = await _db.GetActiveAllAsync();

            return ApiResponse<IReadOnlyList<TaxLookupResponse>>.Success(
                "Active taxes retrieved successfully.", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active Taxes.");

            return ApiResponse<IReadOnlyList<TaxLookupResponse>>.Failure(
                "Unable to retrieve active Taxes.");
        }
    }

    // =========================================================
    // TAX PERCENTAGES
    // =========================================================

    public async Task<ApiResponse<IReadOnlyList<TaxPercentageResponse>>>
        GetPercentagesAsync()
    {
        try
        {
            var result = await _db.GetPercentagesAsync();

            return ApiResponse<IReadOnlyList<TaxPercentageResponse>>.Success(
                "Tax percentages retrieved successfully.", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving tax percentages.");

            return ApiResponse<IReadOnlyList<TaxPercentageResponse>>.Failure(
                "Unable to retrieve tax percentages.");
        }
    }

    // =========================================================
    // TAX SEARCH
    // =========================================================

    public async Task<ApiResponse<IReadOnlyList<TaxLookupResponse>>>
        SearchAsync(string? search)
    {
        try
        {
            var result = await _db.SearchAsync(search);

            return ApiResponse<IReadOnlyList<TaxLookupResponse>>.Success(
                "Tax groups retrieved successfully.", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Tax groups.");

            return ApiResponse<IReadOnlyList<TaxLookupResponse>>.Failure(
                "Unable to retrieve Tax groups.");
        }
    }

    // =========================================================
    // TAX DETAILS BY PERCENTAGE
    // =========================================================

    public async Task<ApiResponse<IReadOnlyList<TaxMasterResponse>>>
        GetDetailsByPercentageAsync(decimal percentage)
    {
        try
        {
            if (percentage < 0)
                return ApiResponse<IReadOnlyList<TaxMasterResponse>>
                    .Failure("Invalid tax percentage.");

            var result =
                await _db.GetDetailsByPercentageAsync(percentage);

            return ApiResponse<IReadOnlyList<TaxMasterResponse>>.Success(
                "Tax details retrieved successfully.", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving tax details by percentage.");

            return ApiResponse<IReadOnlyList<TaxMasterResponse>>
                .Failure("Unable to retrieve tax details.");
        }
    }

    // =========================================================
    // TAX DETAILS BY NAME AND PERCENTAGE
    // =========================================================

    public async Task<ApiResponse<IReadOnlyList<TaxMasterResponse>>>
        GetDetailsByNameAndPercentageAsync(
            string taxName, decimal percentage)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(taxName))
                return ApiResponse<IReadOnlyList<TaxMasterResponse>>
                    .Failure("Tax name is required.");

            if (percentage < 0)
                return ApiResponse<IReadOnlyList<TaxMasterResponse>>
                    .Failure("Invalid tax percentage.");

            var result =
                await _db.GetDetailsByNameAndPercentageAsync(
                    taxName.Trim(), percentage);

            return ApiResponse<IReadOnlyList<TaxMasterResponse>>.Success(
                "Tax details retrieved successfully.", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving tax details by name and percentage.");

            return ApiResponse<IReadOnlyList<TaxMasterResponse>>
                .Failure("Unable to retrieve tax details.");
        }
    }

    // =========================================================
    // ACTIVE TAXES BY TYPE
    // =========================================================

    public async Task<ApiResponse<IReadOnlyList<TaxMasterResponse>>>
        GetActiveByTypeAsync(string taxType)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(taxType))
                return ApiResponse<IReadOnlyList<TaxMasterResponse>>
                    .Failure("Tax type is required.");

            var result =
                await _db.GetActiveByTypeAsync(taxType.Trim());

            return ApiResponse<IReadOnlyList<TaxMasterResponse>>.Success(
                "Active taxes retrieved successfully.", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving active Taxes by type.");

            return ApiResponse<IReadOnlyList<TaxMasterResponse>>
                .Failure("Unable to retrieve active Taxes.");
        }
    }

    // =========================================================
    // VALIDATE ACCOUNT
    // =========================================================

    public async Task<ApiResponse<IReadOnlyList<TaxMasterResponse>>>
        ValidateAccountAsync(int accountId)
    {
        try
        {
            if (accountId <= 0)
                return ApiResponse<IReadOnlyList<TaxMasterResponse>>
                    .Failure("Invalid account ID.");

            var result =
                await _db.ValidateAccountAsync(accountId);

            return ApiResponse<IReadOnlyList<TaxMasterResponse>>.Success(
                "Account tax references retrieved successfully.",
                result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error validating tax account {AccountId}.", accountId);

            return ApiResponse<IReadOnlyList<TaxMasterResponse>>
                .Failure("Unable to validate tax account.");
        }
    }

    // =========================================================
    // EXEMPTED TAX ID
    // =========================================================

    public async Task<ApiResponse<int>> GetExemptedTaxIdAsync()
    {
        try
        {
            var id = await _db.GetExemptedTaxIdAsync();

            if (id <= 0)
                return ApiResponse<int>.Failure(
                    "Exempted tax not found.");

            return ApiResponse<int>.Success(
                "Exempted tax retrieved successfully.", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving exempted Tax ID.");

            return ApiResponse<int>.Failure(
                "Unable to retrieve exempted Tax ID.");
        }
    }

    // =========================================================
    // UPDATE DEFAULT TAX
    // =========================================================

    public async Task<ApiResponse<bool>> UpdateDefaultTaxAsync(
        int id, string taxName, decimal percentage)
    {
        try
        {
            if (id <= 0)
                return ApiResponse<bool>.Failure("Invalid Tax ID.");

            if (string.IsNullOrWhiteSpace(taxName))
                return ApiResponse<bool>.Failure(
                    "Tax name is required.");

            if (percentage < 0)
                return ApiResponse<bool>.Failure(
                    "Tax percentage cannot be negative.");

            var rows = await _db.UpdateDefaultTaxAsync(
                id, taxName.Trim(), percentage);

            if (rows == 0)
                return ApiResponse<bool>.Failure(
                    "Default tax was not updated.");

            return ApiResponse<bool>.Success(
                "Default tax updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error updating default Tax {Id}.", id);

            return ApiResponse<bool>.Failure(
                "Unable to update default Tax.");
        }
    }
}