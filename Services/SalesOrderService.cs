using System.Data;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Respons_DTO;
using QuickBalance.API.DTOs.Sales.SalesOrder;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Repository.Transactions.Sales;

namespace QuickBalance.API.Services;

/// <summary>
/// Business rules for the Sales Order API: validation, paging, messages, logging.
/// No SQL and no HTTP here.
/// </summary>
public sealed class SalesOrderService : ISalesOrderInterface
{
    private readonly SalesOrderRepository _repository;
    private readonly ILogger<SalesOrderService> _logger;

    public SalesOrderService(SalesOrderRepository repository, ILogger<SalesOrderService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    // ------------------------------------------------------------------
    // SAVE
    // ------------------------------------------------------------------
    public async Task<SalesOrderServiceResult<object>> SaveAsync(SalesOrderRequest request, int userId)
    {
        try
        {
            ValidateOrder(request);

            SalesOrderProcedureResult result = await _repository.SaveAsync(request, userId);

            if (!result.HasResponse)
                return SalesOrderServiceResult<object>.FailedResult("No response received from sales order procedure.");

            if (result.Success)
            {
                // Same as before: message is the procedure's "Saved" / "Updated"; "Saved" = 201.
                return result.Message == "Saved"
                    ? SalesOrderServiceResult<object>.CreatedResult(result.Message)
                    : SalesOrderServiceResult<object>.Ok(result.Message);
            }

            if (result.Message.StartsWith("Error"))
            {
                _logger.LogError("Sales order save failed in the procedure: {Message}", result.Message);
                return SalesOrderServiceResult<object>.FailedResult("Could not save the sales order. Please try again or contact support.");
            }

            // Business rule (not found, cancelled, invoiced, unknown product/customer, outside party, ...).
            return SalesOrderServiceResult<object>.Invalid(result.Message);
        }
        catch (ArgumentException ex)
        {
            return SalesOrderServiceResult<object>.Invalid(ex.Message);
        }
        catch (DatabaseUnavailableException ex)
        {
            return SalesOrderServiceResult<object>.FailedResult(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving sales order {SordNo}.", request?.SordNo);
            return SalesOrderServiceResult<object>.FailedResult("An error occurred while saving the sales order.");
        }
    }

    // ------------------------------------------------------------------
    // LIST (paged)
    // ------------------------------------------------------------------
    public async Task<SalesOrderServiceResult<List<Dictionary<string, object?>>>> GetListAsync(
        PaginationRequests request, DateTime? fromDate, DateTime? toDate, int userId)
    {
        try
        {
            request ??= new PaginationRequests();
            request.Normalizes();   // page >= 1, pageSize 1..100 (was commented out: page/pageSize 0 crashed)

            if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
                return SalesOrderServiceResult<List<Dictionary<string, object?>>>.Invalid("fromDate cannot be after toDate.");

            var (rows, totalRecords) = await _repository.GetListAsync(
                userId, request.Page, request.PageSize, request.Search, fromDate, toDate);

            PaginationResponse pagination = BuildPagination(request.Page, request.PageSize, totalRecords);

            // Based on the TOTAL, so an empty page 5 of 3 does not say "no orders".
            string message = totalRecords > 0 ? "Sales order details loaded successfully." : "No sales order details found.";

            return SalesOrderServiceResult<List<Dictionary<string, object?>>>.Ok(message, ToDictionaries(rows), pagination);
        }
        catch (DatabaseUnavailableException ex)
        {
            return SalesOrderServiceResult<List<Dictionary<string, object?>>>.FailedResult(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing sales orders.");
            return SalesOrderServiceResult<List<Dictionary<string, object?>>>.FailedResult("An error occurred while loading sales orders.");
        }
    }

    // ------------------------------------------------------------------
    // GET ONE ORDER (id = ORDNO)
    // ------------------------------------------------------------------
    public async Task<SalesOrderServiceResult<SelectedSalesOrderData>> GetByIdAsync(int id, int userId)
    {
        try
        {
            if (id <= 0)
                return SalesOrderServiceResult<SelectedSalesOrderData>.Invalid("Order number is required.");

            SelectedSalesOrderData? data = await _repository.GetByIdAsync(id, userId);

            if (data == null)
                return SalesOrderServiceResult<SelectedSalesOrderData>.NotFoundResult("Sales order not found.");

            return SalesOrderServiceResult<SelectedSalesOrderData>.Ok("Sales order retrieved successfully.", data);
        }
        catch (DatabaseUnavailableException ex)
        {
            return SalesOrderServiceResult<SelectedSalesOrderData>.FailedResult(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sales order {OrderNo}.", id);
            return SalesOrderServiceResult<SelectedSalesOrderData>.FailedResult("An error occurred while retrieving the sales order.");
        }
    }

    // ------------------------------------------------------------------
    // CANCEL
    // ------------------------------------------------------------------
    public async Task<SalesOrderServiceResult<object>> CancelAsync(int id, int userId)
    {
        try
        {
            if (id <= 0)
                return SalesOrderServiceResult<object>.Invalid("Order number is required.");

            SalesOrderProcedureResult result = await _repository.CancelAsync(id, userId);

            if (!result.HasResponse)
                return SalesOrderServiceResult<object>.FailedResult("No response received from sales order procedure.");

            if (result.Success)
                return SalesOrderServiceResult<object>.Ok(result.Message);

            if (result.Message.StartsWith("Error"))
            {
                _logger.LogError("Sales order cancel failed in the procedure for {OrderNo}: {Message}", id, result.Message);
                return SalesOrderServiceResult<object>.FailedResult("Could not cancel the sales order. Please try again or contact support.");
            }

            return SalesOrderServiceResult<object>.Invalid(result.Message);
        }
        catch (DatabaseUnavailableException ex)
        {
            return SalesOrderServiceResult<object>.FailedResult(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling sales order {OrderNo}.", id);
            return SalesOrderServiceResult<object>.FailedResult("An error occurred while cancelling the sales order.");
        }
    }

    // ------------------------------------------------------------------
    // PENDING ORDERS FOR INVOICING
    // orderId = 0 -> paged header list (Pagination filled); orderId > 0 -> lines (no pagination, as before)
    // ------------------------------------------------------------------
    public async Task<SalesOrderServiceResult<List<Dictionary<string, object?>>>> GetPendingAsync(PaginationRequest request, int userId)
    {
        try
        {
            request ??= new PaginationRequest();
            request.Normalize();

            // The procedure filters by customer: without it the answer is always empty.
            if (request.CustomerId <= 0)
                return SalesOrderServiceResult<List<Dictionary<string, object?>>>.Invalid("customerId is required.");

            if (request.OrderId < 0)
                return SalesOrderServiceResult<List<Dictionary<string, object?>>>.Invalid("Invalid orderId.");

            var (rows, totalRecords) = await _repository.GetPendingAsync(request, userId);
            var data = ToDictionaries(rows);

            if (request.OrderId == 0)
            {
                string headerMessage = totalRecords > 0
                    ? "Pending sales order details loaded successfully."
                    : "No pending sales order details found.";

                return SalesOrderServiceResult<List<Dictionary<string, object?>>>.Ok(
                    headerMessage, data, BuildPagination(request.Page, request.PageSize, totalRecords));
            }

            string lineMessage = data.Count > 0
                ? "Pending sales order item details loaded successfully."
                : "No pending sales order item details found.";

            return SalesOrderServiceResult<List<Dictionary<string, object?>>>.Ok(lineMessage, data);
        }
        catch (DatabaseUnavailableException ex)
        {
            return SalesOrderServiceResult<List<Dictionary<string, object?>>>.FailedResult(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading pending sales orders for customer {CustomerId}.", request?.CustomerId);
            return SalesOrderServiceResult<List<Dictionary<string, object?>>>.FailedResult("An error occurred while loading pending sales orders.");
        }
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    // Request checks that need no database (throws ArgumentException with a clear message).
    private static void ValidateOrder(SalesOrderRequest? request)
    {
        if (request == null)
            throw new ArgumentException("Order data is required.");

        if (request.Items == null || request.Items.Count == 0)
            throw new ArgumentException("At least one order item is required.");

        if (request.CustomerCode <= 0)
            throw new ArgumentException("Party details are required.");

        if (request.SordNo < 0)
            throw new ArgumentException("Invalid order number.");

        // Missing date arrives as 0001-01-01, which SQL Server DATETIME cannot store (was a raw 500).
        if (request.Date < new DateTime(1753, 1, 1))
            throw new ArgumentException("Order date is required.");

        if (request.Items.Any(i => i.SlNo <= 0))
            throw new ArgumentException("Each item needs a line number (slNo) greater than 0.");

        // The procedure uses slNo as the line id (ORDROWID); invoices link to it.
        if (request.Items.Select(i => i.SlNo).Distinct().Count() != request.Items.Count)
            throw new ArgumentException("Each item must have a unique slNo.");

        foreach (SalesOrderItemRequest item in request.Items)
        {
            if (item.PrCode <= 0)
                throw new ArgumentException($"Product is required (line {item.SlNo}).");

            if (item.Qty <= 0)
                throw new ArgumentException($"Quantity must be greater than 0 (line {item.SlNo}).");

            if (item.Rate < 0)
                throw new ArgumentException($"Rate cannot be negative (line {item.SlNo}).");
        }

        if (request.Additional != null && request.Additional.Any(a => a.AccId <= 0))
            throw new ArgumentException("Each additional charge needs an account (accId).");
    }

    private static PaginationResponse BuildPagination(int page, int pageSize, int totalRecords) =>
        new PaginationResponse
        {
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = totalRecords > 0 ? (int)Math.Ceiling(totalRecords / (double)pageSize) : 0
        };

    // Same conversion as before: one dictionary per row, DBNull -> null, column names unchanged.
    private static List<Dictionary<string, object?>> ToDictionaries(DataTable dt) =>
        dt.AsEnumerable()
          .Select(row => dt.Columns.Cast<DataColumn>()
              .ToDictionary(column => column.ColumnName, column => row[column] == DBNull.Value ? null : row[column]))
          .ToList();
}
