using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs.Sales.SalesBilling;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Repository.Transactions.Sales;

namespace QuickBalance.API.Services;

public sealed class SalesBillingService : ISalesBillingInterface
{
    private const int MaxIdempotencyKeyLength = 100;
    private const int DefaultGodownCode = 1;

    private readonly SalesBillingRepository _repository;
    private readonly ILogger<SalesBillingService> _logger;

    public SalesBillingService(
        SalesBillingRepository repository,
        ILogger<SalesBillingService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ApiResponse<SalesBillingSaveResponse>> SaveAsync(
        SalesBillingRequest request,
        string? idempotencyKey,
        int userId)
    {
        try
        {
            // 1. Business rules. Throws ArgumentException when a rule is broken.
            Validate(request, idempotencyKey);

            // 1b. An update must target an existing invoice that is not cancelled.
            //     (The save procedure itself does not stop this.)
            if (request.InvoiceId > 0)
            {
                bool? isCancelled = await _repository.IsCancelledAsync(request.InvoiceId);

                if (isCancelled == null)
                    return Failure("Invoice not found.");

                if (isCancelled == true)
                    return Failure("Cancelled invoices cannot be edited.");
            }

            // 2. Fingerprint the request BEFORE Normalize changes anything.
            string requestHash = ComputeRequestHash(request);

            // 3. Fill in the defaults.
            Normalize(request);

            // 4. Save. The procedure runs everything inside one transaction.
            SalesBillingSaveResult result = await _repository.SaveAsync(
                request,
                userId,
                idempotencyKey ?? string.Empty,
                requestHash);

            // 5. Turn the procedure's answer into an API response.
            return ToApiResponse(request, result, userId);
        }
        catch (ArgumentException ex)
        {
            return Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving Sales Invoice {InvoiceId}.", request?.InvoiceId);

            return Failure("An error occurred while saving the Sales Invoice.");
        }
    }

    public async Task<ApiResponse<SalesBillingDetailResponse>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return ApiResponse<SalesBillingDetailResponse>.Failure("Invalid invoice ID.");

            SalesBillingDetailResponse? invoice = await _repository.GetByIdAsync(id);

            if (invoice is null)
                return ApiResponse<SalesBillingDetailResponse>.Failure("Invoice not found.");

            return ApiResponse<SalesBillingDetailResponse>.Success(
                "Sales invoice retrieved successfully.",
                invoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Sales Invoice {Id}.", id);

            return ApiResponse<SalesBillingDetailResponse>.Failure(
                "Unable to retrieve the sales invoice.");
        }
    }

    public async Task<ApiResponse<IReadOnlyList<SalesBillingListItemResponse>>> GetListAsync(
        SalesBillingListRequest request)
    {
        try
        {
            request ??= new SalesBillingListRequest();

            // Same paging limits as BrandService.
            request.Page = Math.Max(request.Page, 1);
            request.PageSize = Math.Clamp(request.PageSize, 1, 100);

            if (request.FromDate.HasValue && request.ToDate.HasValue &&
                request.FromDate.Value.Date > request.ToDate.Value.Date)
            {
                return ApiResponse<IReadOnlyList<SalesBillingListItemResponse>>.Failure(
                    "FromDate cannot be after ToDate.");
            }

            var (items, totalRecords) = await _repository.GetListAsync(request);

            PaginationInfo pagination = new PaginationInfo
            {
                Page = request.Page,
                PageSize = request.PageSize,
                TotalRecords = totalRecords,
                TotalPages = totalRecords == 0
                    ? 0
                    : (int)Math.Ceiling(totalRecords / (double)request.PageSize)
            };

            return ApiResponse<IReadOnlyList<SalesBillingListItemResponse>>.SuccessWithPagination(
                items.Count > 0 ? "Sales invoices retrieved successfully." : "No sales invoices found.",
                items,
                pagination);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Sales Invoices.");

            return ApiResponse<IReadOnlyList<SalesBillingListItemResponse>>.Failure(
                "Unable to retrieve sales invoices.");
        }
    }

    public async Task<ApiResponse<bool>> CancelAsync(int id, int userId)
    {
        try
        {
            if (id <= 0)
                return ApiResponse<bool>.Failure("Invalid invoice ID.");

            SalesBillingSaveResult result = await _repository.CancelAsync(id, userId);

            if (result.Success)
                return ApiResponse<bool>.Success("Sales invoice cancelled successfully.", true);

            string procedureMessage = result.Message ?? string.Empty;

            // The cancel procedure's CATCH block returns "Error: <details>".
            if (procedureMessage.Length == 0 ||
                procedureMessage.StartsWith("Error", StringComparison.Ordinal))
            {
                _logger.LogError(
                    "Sales invoice cancel failed for user {UserId}, invoice {InvoiceId}: {ProcedureMessage}",
                    userId, id, procedureMessage);

                return ApiResponse<bool>.Failure(
                    "Could not cancel the sales invoice. Please try again or contact support.");
            }

            // Business message (not found, already cancelled, return exists): safe to show.
            return ApiResponse<bool>.Failure(procedureMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling Sales Invoice {Id}.", id);

            return ApiResponse<bool>.Failure("An error occurred while cancelling the Sales Invoice.");
        }
    }

    // ------------------------------------------------------------------
    // PENDING INVOICE FOR RETURN
    // ------------------------------------------------------------------
    public async Task<ApiResponse<SalesBillingPendingResponse>> GetPendingForReturnAsync(SalesBillingPendingRequest request)
    {
        try
        {
            request ??= new SalesBillingPendingRequest();

            if (request.InvoiceId < 0 || request.ReturnId < 0 || request.CustomerCode < 0)
                return ApiResponse<SalesBillingPendingResponse>.Failure("Invalid query values.");

            if (request.InvoiceId == 0 && string.IsNullOrWhiteSpace(request.InvoiceNoText))
                return ApiResponse<SalesBillingPendingResponse>.Failure("Provide invoiceId or invoiceNoText.");

            SalesBillingPendingResponse? pending = await _repository.GetPendingForReturnAsync(request);

            if (pending == null)
                return ApiResponse<SalesBillingPendingResponse>.Failure("Sales invoice not found.");

            if (pending.Header.IsCancelled)
                return ApiResponse<SalesBillingPendingResponse>.Failure("This Bill is Cancelled");

            if (request.CustomerCode > 0 && pending.Header.CustomerCode != request.CustomerCode)
                return ApiResponse<SalesBillingPendingResponse>.Failure("The selected sales invoice belongs to a different customer.");

            // Same wording as the desktop application.
            if (pending.Items.Count == 0)
                return ApiResponse<SalesBillingPendingResponse>.Failure("Return completed against this bill");

            return ApiResponse<SalesBillingPendingResponse>.Success("Pending sales invoice details loaded successfully.", pending);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading pending sales invoice {InvoiceId} / {InvoiceNoText}.", request?.InvoiceId, request?.InvoiceNoText);
            return ApiResponse<SalesBillingPendingResponse>.Failure("Unable to load the pending sales invoice.");
        }
    }

    // ------------------------------------------------------------------
    // 1. Validation (business rules)
    // ------------------------------------------------------------------

    private static void Validate(SalesBillingRequest request, string? idempotencyKey)
    {
        if (request == null)
            throw new ArgumentException("Invalid sales invoice request.");

        // The procedure requires a key for NEW invoices (it uses it to stop duplicates).
        if (request.InvoiceId == 0 && string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("A valid Idempotency-Key header is required.");

        if (idempotencyKey != null && idempotencyKey.Length > MaxIdempotencyKeyLength)
            throw new ArgumentException("Idempotency-Key must be 100 characters or fewer.");

        if (request.InvoiceDate == null)
            throw new ArgumentException("InvoiceDate is required.");

        if (request.CustomerCode <= 0)
            throw new ArgumentException("CustomerCode is required.");

        if (request.Items == null || request.Items.Count == 0)
            throw new ArgumentException("At least one item is required.");

        string? payMode = NormalizePayMode(request.PayMode);

        if (payMode == null)
            throw new ArgumentException("PayMode must be Cash, Credit or Bank.");

        if (payMode == "Bank" && request.BankLedgerCode <= 0)
            throw new ArgumentException("BankLedgerCode is required for Bank payments.");

        HashSet<int> lineNumbers = new HashSet<int>();

        foreach (SalesBillingItemRequest item in request.Items)
        {
            if (item == null)
                throw new ArgumentException("Invalid item line.");

            // Serial numbers point to their line through LineNo, so it must be unique.
            if (!lineNumbers.Add(item.LineNo))
                throw new ArgumentException($"Duplicate LineNo {item.LineNo}.");

            if (item.Quantity <= 0)
                throw new ArgumentException($"Quantity must be greater than 0 (line {item.LineNo}).");

            if (item.Rate < 0)
                throw new ArgumentException($"Rate cannot be negative (line {item.LineNo}).");
        }
    }

    /// <summary>
    /// Returns exactly "Cash", "Credit" or "Bank" (any capitalisation accepted).
    /// Empty means "Cash". Any other value returns null (= invalid).
    /// </summary>
    private static string? NormalizePayMode(string? payMode)
    {
        if (string.IsNullOrWhiteSpace(payMode))
            return "Cash";

        return payMode.Trim().ToUpperInvariant() switch
        {
            "CASH" => "Cash",
            "CREDIT" => "Credit",
            "BANK" => "Bank",
            _ => null
        };
    }

    // ------------------------------------------------------------------
    // 2. Request fingerprint (used by the procedure's duplicate check)
    // ------------------------------------------------------------------

    private static string ComputeRequestHash(SalesBillingRequest request)
    {
        string json = JsonSerializer.Serialize(request);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));

        // 64 hexadecimal characters - matches the procedure's CHAR(64) parameter.
        return Convert.ToHexString(hash);
    }

    // ------------------------------------------------------------------
    // 3. Defaults (same rules as the existing API, kept in one place)
    // ------------------------------------------------------------------

    private static void Normalize(SalesBillingRequest request)
    {
        request.PayMode = NormalizePayMode(request.PayMode);
        request.DueDate ??= request.InvoiceDate;
        request.Charges ??= new List<SalesBillingChargeRequest>();

        foreach (SalesBillingItemRequest item in request.Items)
        {
            if (item.Mrp == 0)
                item.Mrp = item.Rate;

            if (item.GodownCode == 0)
                item.GodownCode = DefaultGodownCode;

            if (item.ConversionFactor == 0)
                item.ConversionFactor = 1;

            item.Batch = item.Batch?.Trim();
            item.SerialNumbers ??= new List<SalesBillingSerialRequest>();
        }
    }

    // ------------------------------------------------------------------
    // 5. Procedure result -> API response
    // ------------------------------------------------------------------

    private ApiResponse<SalesBillingSaveResponse> ToApiResponse(
        SalesBillingRequest request,
        SalesBillingSaveResult result,
        int userId)
    {
        bool isNew = request.InvoiceId == 0;

        if (result.Success)
        {
            if (result.IsReplay)
            {
                _logger.LogInformation(
                    "Repeated Sales Invoice request for user {UserId}; existing invoice {InvoiceId} returned.",
                    userId, result.InvoiceId);
            }

            SalesBillingSaveResponse response = new SalesBillingSaveResponse
            {
                IsNew = isNew,
                Message = result.Message
            };

            return ApiResponse<SalesBillingSaveResponse>.Success(
                isNew ? "Sales invoice saved successfully." : "Sales invoice updated successfully.",
                response);
        }

        string procedureMessage = result.Message ?? string.Empty;

        // Technical errors from the procedure's CATCH block start with "Error <number>".
        if (procedureMessage.StartsWith("Error 50004", StringComparison.Ordinal))
            return Failure("This Idempotency-Key was already used with different invoice data.");

        if (procedureMessage.StartsWith("Error 50003", StringComparison.Ordinal))
            return Failure("The same request is still being processed. Please retry.");

        if (procedureMessage.Length == 0 ||
            procedureMessage.StartsWith("Error ", StringComparison.Ordinal))
        {
            _logger.LogError(
                "Sales invoice procedure failed for user {UserId}, invoice {InvoiceId}: {ProcedureMessage}",
                userId, request.InvoiceId, procedureMessage);

            return Failure("Could not save the sales invoice. Please try again or contact support.");
        }

        // A business-rule message (stock, batch, serial number, date...): safe and useful to show.
        return Failure(procedureMessage);
    }

    private static ApiResponse<SalesBillingSaveResponse> Failure(string message)
    {
        return ApiResponse<SalesBillingSaveResponse>.Failure(message);
    }
}
