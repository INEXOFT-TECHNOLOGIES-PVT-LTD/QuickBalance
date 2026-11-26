using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Respons_DTO;
using QuickBalance.API.DTOs.Sales.SalesOrder;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Services;

namespace QuickBalance.API.Controllers.Transactions.Sales
{
    /// <summary>
    /// Sales Order API. Layered since 26 Nov 2026 (Controller -> ISalesOrderInterface -> SalesOrderService
    /// -> SalesOrderRepository). Same routes and the same JSON as before.
    /// This class only translates between HTTP and the service.
    /// </summary>
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = false)]   // set to true to hide this controller from Swagger
    [Tags("25. Sales Orders")]
    [Route("api/v1/sales-orders")]
    [Authorize]
    public class SalesOrderController : ControllerBase
    {
        private readonly ISalesOrderInterface _salesOrderService;
        private readonly ICurrentUser _currentUser;

        public SalesOrderController(ISalesOrderInterface salesOrderService, ICurrentUser currentUser)
        {
            _salesOrderService = salesOrderService;
            _currentUser = currentUser;
        }

        [HttpPost("manage")]
        public async Task<IActionResult> ManageSalesOrder([FromBody] SalesOrderRequest request)
        {
            var result = await _salesOrderService.SaveAsync(request, _currentUser.UserId);

            if (!result.IsSuccess)
                return ToFailure(result);

            var body = new { status = true, message = result.Message, data = new { message = result.Message } };
            return result.Outcome == SalesOrderOutcome.Created ? Created("", body) : Ok(body);
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListSalesOrder([FromQuery] PaginationRequests request, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var result = await _salesOrderService.GetListAsync(request, fromDate, toDate, _currentUser.UserId);

            if (!result.IsSuccess)
                return ToFailure(result);

            return Ok(new
            {
                status = true,
                message = result.Message,
                data = result.Data,
                pagination = result.Pagination
            });
        }

        [HttpGet("get/{id:int}")]
        public async Task<IActionResult> GetSelectedSalesOrder(int id)
        {
            var result = await _salesOrderService.GetByIdAsync(id, _currentUser.UserId);

            if (!result.IsSuccess)
                return ToFailure(result);

            return Ok(new SelectedSalesOrderResponse
            {
                Status = true,
                Message = result.Message,
                Data = result.Data!
            });
        }

        [HttpPost("cancel/{id:int}")]
        public async Task<IActionResult> CancelSalesOrder(int id)
        {
            var result = await _salesOrderService.CancelAsync(id, _currentUser.UserId);

            if (!result.IsSuccess)
                return ToFailure(result);

            return Ok(new { status = true, message = result.Message, data = new { message = result.Message } });
        }

        [HttpGet("pending-list")]
        public async Task<IActionResult> ListPendingSalesOrderInInvoices([FromQuery] PaginationRequest request)
        {
            var result = await _salesOrderService.GetPendingAsync(request, _currentUser.UserId);

            if (!result.IsSuccess)
                return ToFailure(result);

            // Header list (orderId = 0) has pagination; order lines (orderId > 0) do not - same as before.
            if (result.Pagination != null)
                return Ok(new { status = true, message = result.Message, data = result.Data, pagination = result.Pagination });

            return Ok(new { status = true, message = result.Message, data = result.Data });
        }

        // Same error shape as before: { status, message, errors }.
        private IActionResult ToFailure<T>(SalesOrderServiceResult<T> result)
        {
            var body = new { status = false, message = result.Message, errors = new List<object>() };

            return result.Outcome switch
            {
                SalesOrderOutcome.NotFound => NotFound(body),
                SalesOrderOutcome.Failed => StatusCode(500, body),
                _ => BadRequest(body)
            };
        }
    }
}
