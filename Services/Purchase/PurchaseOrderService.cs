
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Purchase;
using QuickBalance.API.Interfaces.Purchase;

namespace QuickBalance.API.Services.Purchase
{
    public class PurchaseOrderService : IPurchaseOrderInterface
    {
        private readonly IPurchaseOrderRepository _repository;

        public PurchaseOrderService(IPurchaseOrderRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponseWithSuccess> CreatePurchaseOrderAsync(
            PurchaseOrderCreateRequest request,
            int userId)
        {
            try
            {
                if (request == null)
                    return Error("Invalid purchase order request.");

                if (userId <= 0)
                    return Error("Invalid user.");

                if (request.Master == null)
                    return Error("Purchase order master details are required.");

                if (request.Master.SupplierId <= 0)
                    return Error("Please select a valid supplier.");

                if (request.Master.OrderDate == default)
                    return Error("Please provide a valid purchase order date.");

                if (request.Items == null || request.Items.Count == 0)
                    return Error("Purchase order must contain at least one item.");

                if (request.Master.DiscountPercentage < 0 ||
                    request.Master.DiscountPercentage > 100)
                    return Error("Invalid discount percentage.");

                if (request.Master.DiscountValue < 0)
                    return Error("Discount value cannot be negative.");

                if (request.Master.Freight < 0)
                    return Error("Freight cannot be negative.");

                if (request.Master.CurrencyConversionRate <= 0)
                    return Error("Currency conversion rate must be greater than zero.");

                foreach (var item in request.Items)
                {
                    if (item.ProductId <= 0)
                        return Error("Please select a valid product.");

                    if (item.Quantity <= 0)
                        return Error("Item quantity must be greater than zero.");

                    if (item.UnitId <= 0)
                        return Error("Please select a valid unit.");

                    if (item.Rate < 0)
                        return Error("Item rate cannot be negative.");

                    if (item.Discount < 0)
                        return Error("Item discount cannot be negative.");

                    if (item.ConversionFactor <= 0)
                        return Error("Conversion factor must be greater than zero.");
                }

                int orderId = await _repository.CreatePurchaseOrderAsync(
                    request,
                    userId);

                return new ApiResponseWithSuccess
                {
                    Status = true,
                    Message = $"Purchase order created successfully. ID: {orderId}"
                };
            }
            catch (ArgumentException ex)
            {
                return Error(ex.Message);
            }
            catch (Exception)
            {
                return Error("Unable to create Purchase Order.");
            }
        }

      

        public async Task<ApiResponseWithSuccess> UpdatePurchaseOrderAsync(
            int id,
            PurchaseOrderCreateRequest request)
        {
            if (id <= 0)
                return Error("Invalid Purchase Order ID.");

            if (request == null)
                return Error("Invalid purchase order request.");

            return Error("Purchase order update is not implemented yet.");
        }

        private static ApiResponseWithSuccess Error(string message)
        {
            return new ApiResponseWithSuccess
            {
                Status = false,
                Message = message
            };
        }

        public async Task<ApiResponse<PurchaseOrderListResult>> GetAllAsync(
         int page,
         int pageSize,
         string? search)
        {
            try
            {
                if (page < 1)
                    return new ApiResponse<PurchaseOrderListResult>
                    {
                        Status = false,
                        Message = "Page must be greater than zero."
                    };

                if (pageSize < 1)
                    return new ApiResponse<PurchaseOrderListResult>
                    {
                        Status = false,
                        Message = "Page size must be greater than zero."
                    };

                var result = await _repository.GetAllAsync(
                    page,
                    pageSize,
                    search);

                return new ApiResponse<PurchaseOrderListResult>
                {
                    Status = true,
                    Message = "Purchase orders retrieved successfully.",
                    Data = result
                };
            }
            catch (Exception)
            {
                return new ApiResponse<PurchaseOrderListResult>
                {
                    Status = false,
                    Message = "Unable to retrieve Purchase Orders."
                };
            }
        }
        public async Task<ApiResponse<PurchaseOrderDetailResult>> GetPurchaseOrderByIdAsync(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return new ApiResponse<PurchaseOrderDetailResult>
                    {
                        Status = false,
                        Message = "Invalid Purchase Order ID."
                    };
                }

                var result = await _repository.GetPurchaseOrderByIdAsync(id);

                if (result == null)
                {
                    return new ApiResponse<PurchaseOrderDetailResult>
                    {
                        Status = false,
                        Message = "Purchase order not found."
                    };
                }

                return new ApiResponse<PurchaseOrderDetailResult>
                {
                    Status = true,
                    Message = "Purchase order retrieved successfully.",
                    Data = result
                };
            }
            catch (Exception)
            {
                return new ApiResponse<PurchaseOrderDetailResult>
                {
                    Status = false,
                    Message = "Unable to retrieve Purchase Order."
                };
            }
        }

        public async Task<ApiResponseWithSuccess> DeletePurchaseOrderAsync(int id)
        {
            try
            {
                if (id <= 0)
                    return Error("Invalid Purchase Order ID.");

                bool deleted = await _repository.DeletePurchaseOrderAsync(id);

                if (!deleted)
                    return Error("Purchase order not found or already cancelled.");

                return new ApiResponseWithSuccess
                {
                    Status = true,
                    Message = "Purchase order cancelled successfully."
                };
            }
            catch (Exception)
            {
                return Error("Unable to cancel Purchase Order.");
            }
        }
    }
}

