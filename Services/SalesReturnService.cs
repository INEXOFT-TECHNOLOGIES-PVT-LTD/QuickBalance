using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs.Sales.SalesReturn;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Repository.Transactions.Sales;

namespace QuickBalance.API.Services;

/// <summary>
/// Business rules for the NEW Sales Return API.
/// Validates and prepares the request, calls the repository, and turns the
/// procedure's answer into an ApiResponse. No SQL here.
/// </summary>
public sealed class SalesReturnService : ISalesReturnInterface
{
    private readonly SalesReturnRepository _repository;
    private readonly ILogger<SalesReturnService> _logger;

    public SalesReturnService(SalesReturnRepository repository, ILogger<SalesReturnService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    // ------------------------------------------------------------------
    // SAVE
    // ------------------------------------------------------------------
    public async Task<ApiResponse<SalesReturnSaveResponse>> SaveAsync(
        SalesReturnSaveRequest request,
        string? idempotencyKey,
        int userId)
    {
        try
        {
            // 1. Check the request (throws ArgumentException with a clear message).
            Validate(request, idempotencyKey);

            // 2. Fingerprint of the request, BEFORE normalising, so a retry of the
            //    exact same body always gives the exact same hash.
            string requestHash = ComputeRequestHash(request);

            // 3. Fill in defaults (same rules as Sales Billing).
            Normalize(request);

            // 4. Save. The procedure also checks: return exists / not cancelled,
            //    invoice exists / not cancelled / same customer, max returnable qty.
            SalesReturnSaveResult result = await _repository.SaveAsync(request, userId, idempotencyKey, requestHash);

            return ToApiResponse(result, request);
        }
        catch (ArgumentException ex)
        {
            return ApiResponse<SalesReturnSaveResponse>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving Sales Return {ReturnId}.", request?.ReturnId);
            return ApiResponse<SalesReturnSaveResponse>.Failure("An error occurred while saving the Sales Return.");
        }
    }

    // ------------------------------------------------------------------
    // GET BY ID
    // ------------------------------------------------------------------
    public async Task<ApiResponse<SalesReturnDetailResponse>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return ApiResponse<SalesReturnDetailResponse>.Failure("Invalid return ID.");

            SalesReturnDetailResponse? detail = await _repository.GetByIdAsync(id);

            if (detail is null)
                return ApiResponse<SalesReturnDetailResponse>.Failure("Sales return not found.");

            return ApiResponse<SalesReturnDetailResponse>.Success("Sales return retrieved successfully.", detail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Sales Return {ReturnId}.", id);
            return ApiResponse<SalesReturnDetailResponse>.Failure("Unable to retrieve the sales return.");
        }
    }

    // ------------------------------------------------------------------
    // LIST (paged)
    // ------------------------------------------------------------------
    public async Task<ApiResponse<IReadOnlyList<SalesReturnListItemResponse>>> GetListAsync(SalesReturnListRequest request)
    {
        try
        {
            request ??= new SalesReturnListRequest();
            request.Page = Math.Max(1, request.Page);
            request.PageSize = Math.Clamp(request.PageSize, 1, 100);

            if (request.FromDate.HasValue && request.ToDate.HasValue && request.FromDate.Value.Date > request.ToDate.Value.Date)
                return ApiResponse<IReadOnlyList<SalesReturnListItemResponse>>.Failure("FromDate cannot be after ToDate.");

            var (items, totalRecords) = await _repository.GetListAsync(request);

            PaginationInfo pagination = BuildPagination(request.Page, request.PageSize, totalRecords);

            // Based on the TOTAL, not on this page: page 5 of 3 is "empty page", not "no returns".
            string message = totalRecords > 0
                ? "Sales returns retrieved successfully."
                : "No sales returns found.";

            return ApiResponse<IReadOnlyList<SalesReturnListItemResponse>>.SuccessWithPagination(message, items, pagination);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing Sales Returns.");
            return ApiResponse<IReadOnlyList<SalesReturnListItemResponse>>.Failure("Unable to retrieve sales returns.");
        }
    }

    // ------------------------------------------------------------------
    // CANCEL
    // ------------------------------------------------------------------
    public async Task<ApiResponse<bool>> CancelAsync(int id, int userId)
    {
        try
        {
            if (id <= 0)
                return ApiResponse<bool>.Failure("Invalid return ID.");

            SalesReturnSaveResult result = await _repository.CancelAsync(id, userId);

            if (result.Success)
                return ApiResponse<bool>.Success("Sales return cancelled successfully.", true);

            string procedureMessage = result.Message ?? string.Empty;

            // Technical SQL error: log the details, show a safe message.
            if (procedureMessage.StartsWith("Error") || string.IsNullOrWhiteSpace(procedureMessage))
            {
                _logger.LogError("Sales Return cancel failed in the procedure for {ReturnId}: {Message}", id, procedureMessage);
                return ApiResponse<bool>.Failure("Could not cancel the sales return. Please try again or contact support.");
            }

            // Business rule message (not found / already cancelled / batch / serial) - show as is.
            return ApiResponse<bool>.Failure(procedureMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling Sales Return {ReturnId}.", id);
            return ApiResponse<bool>.Failure("An error occurred while cancelling the Sales Return.");
        }
    }

    // Build the pagination block EXACTLY like SalesBillingService.GetListAsync does (allowed adjustment).
    private static PaginationInfo BuildPagination(int page, int pageSize, int totalRecords)
    {
        return new PaginationInfo
        {
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = totalRecords == 0
                ? 0
                : (int)Math.Ceiling(totalRecords / (double)pageSize)
        };
    }

    // ------------------------------------------------------------------
    // Validation
    // ------------------------------------------------------------------
    private static void Validate(SalesReturnSaveRequest request, string? idempotencyKey)
    {
        if (request is null)
            throw new ArgumentException("Request body is required.");

        // A new return must carry an Idempotency-Key (protects against double saves on retry).
        if (request.ReturnId == 0 && string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency-Key header is required when creating a sales return.");

        if (idempotencyKey is not null && idempotencyKey.Length > 100)
            throw new ArgumentException("Idempotency-Key must not be longer than 100 characters.");

        if (request.ReturnDate is null)
            throw new ArgumentException("ReturnDate is required.");

        if (request.CustomerCode <= 0)
            throw new ArgumentException("CustomerCode is required.");

        if (request.Items is null || request.Items.Count == 0)
            throw new ArgumentException("At least one item is required.");

        string? payMode = NormalizePayMode(request.PayMode);
        if (payMode is null)
            throw new ArgumentException("PayMode must be Cash, Credit or Bank.");

        if (payMode == "Bank" && request.BankLedgerCode <= 0)
            throw new ArgumentException("BankLedgerCode is required when PayMode is Bank.");

        if (request.Items.Select(i => i.LineNo).Distinct().Count() != request.Items.Count)
            throw new ArgumentException("Each item must have a unique LineNo.");

        foreach (SalesReturnSaveItemRequest item in request.Items)
        {
            if (item.Quantity <= 0)
                throw new ArgumentException($"Quantity must be greater than 0 (line {item.LineNo}).");

            if (item.Rate < 0)
                throw new ArgumentException($"Rate cannot be negative (line {item.LineNo}).");

            if (item.SerialNumbers is not null && item.SerialNumbers.Any(s => string.IsNullOrWhiteSpace(s.SerialNo)))
                throw new ArgumentException($"Serial numbers cannot be empty (line {item.LineNo}).");
        }
    }

    // "cash" / "CASH" / "" -> "Cash"; "credit" -> "Credit"; "bank" -> "Bank"; anything else -> null (invalid).
    private static string? NormalizePayMode(string? payMode)
    {
        if (string.IsNullOrWhiteSpace(payMode))
            return "Cash";

        return payMode.Trim().ToLowerInvariant() switch
        {
            "cash" => "Cash",
            "credit" => "Credit",
            "bank" => "Bank",
            _ => null
        };
    }

    // ------------------------------------------------------------------
    // Prepare the request (defaults) - same rules as Sales Billing
    // ------------------------------------------------------------------
    private static void Normalize(SalesReturnSaveRequest request)
    {
        request.PayMode = NormalizePayMode(request.PayMode);
        request.DueDate ??= request.ReturnDate;
        request.Charges ??= new();

        foreach (SalesReturnSaveItemRequest item in request.Items)
        {
            if (item.Mrp == 0) item.Mrp = item.Rate;
            if (item.GodownCode == 0) item.GodownCode = 1;
            if (item.ConversionFactor == 0) item.ConversionFactor = 1;
            item.Batch = item.Batch?.Trim();
            item.SerialNumbers ??= new();

            foreach (SalesReturnSaveSerialRequest serial in item.SerialNumbers)
                serial.SerialNo = serial.SerialNo.Trim();
        }
    }

    // SHA-256 of the request JSON, as 64 hex characters (matches @REQUESTHASH CHAR(64)).
    private static string ComputeRequestHash(SalesReturnSaveRequest request)
    {
        string json = JsonSerializer.Serialize(request);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash);
    }

    // ------------------------------------------------------------------
    // Procedure answer -> ApiResponse
    // ------------------------------------------------------------------
    private ApiResponse<SalesReturnSaveResponse> ToApiResponse(SalesReturnSaveResult result, SalesReturnSaveRequest request)
    {
        bool isNew = request.ReturnId == 0;

        if (result.Success)
        {
            if (result.IsReplay)
                _logger.LogInformation("Sales Return idempotent replay: ReturnId {ReturnId}, ReturnNo {ReturnNo}.", result.ReturnId, result.ReturnNo);

            var data = new SalesReturnSaveResponse
            {
                IsNew = isNew,
                Message = result.Message,
                ReturnId = result.ReturnId,
                ReturnNo = result.ReturnNo
            };

            string message = isNew ? "Sales return saved successfully." : "Sales return updated successfully.";
            return ApiResponse<SalesReturnSaveResponse>.Success(message, data);
        }

        string procedureMessage = result.Message ?? string.Empty;

        if (procedureMessage.StartsWith("Error 50004"))
            return ApiResponse<SalesReturnSaveResponse>.Failure("This Idempotency-Key was already used with different return data.");

        if (procedureMessage.StartsWith("Error 50003"))
            return ApiResponse<SalesReturnSaveResponse>.Failure("The same request is still being processed. Please retry.");

        // Technical SQL error (or no message at all): log the details, show a safe message.
        if (procedureMessage.StartsWith("Error ") || string.IsNullOrWhiteSpace(procedureMessage))
        {
            _logger.LogError("Sales Return save failed in the procedure: {Message}", procedureMessage);
            return ApiResponse<SalesReturnSaveResponse>.Failure("Could not save the sales return. Please try again or contact support.");
        }

        // Business rule message from the procedure (e.g. "Return quantity is more than ...") - show as is.
        return ApiResponse<SalesReturnSaveResponse>.Failure(procedureMessage);
    }
}
