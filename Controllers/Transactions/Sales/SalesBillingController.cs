using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs.Sales.SalesBilling;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Services;

namespace QuickBalance.API.Controllers.Transactions.Sales
{
    [ApiController]
    [Tags("26. Sales Billing")]
    [Route("api/v1/sales-billing")]
    [Authorize]
    public sealed class SalesBillingController : ControllerBase
    {
        private readonly ISalesBillingInterface _service;
        private readonly ICurrentUser _currentUser;

        public SalesBillingController(
            ISalesBillingInterface service,
            ICurrentUser currentUser)
        {
            _service = service;
            _currentUser = currentUser;
        }

        // POST: api/v1/sales-billing/manage
        // InvoiceId = 0 creates a new invoice (Idempotency-Key header required).
        // InvoiceId > 0 updates that invoice.
        [HttpPost("manage")]
        [ProducesResponseType(typeof(ApiResponse<SalesBillingSaveResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<SalesBillingSaveResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Manage(
            [FromBody] SalesBillingRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
        {
            var result = await _service.SaveAsync(
                request,
                idempotencyKey,
                _currentUser.UserId);

            if (!result.Status)
                return BadRequest(result);

            return request.InvoiceId == 0
                ? StatusCode(StatusCodes.Status201Created, result)
                : Ok(result);
        }

        // GET: api/v1/sales-billing/get/24
        // 24 = the internal invoice id (InvoiceId), not the printed invoice number.
        [HttpGet("get/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<SalesBillingDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);

            return result.Status ? Ok(result) : BadRequest(result);
        }

        // GET: api/v1/sales-billing/list?page=1&pageSize=20&search=...&fromDate=...&toDate=...
        [HttpGet("list")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SalesBillingListItemResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> List([FromQuery] SalesBillingListRequest request)
        {
            var result = await _service.GetListAsync(request);

            return result.Status ? Ok(result) : BadRequest(result);
        }

        // POST: api/v1/sales-billing/cancel/24
        // Soft cancel: the invoice is kept (audit trail), stock is returned.
        [HttpPost("cancel/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await _service.CancelAsync(id, _currentUser.UserId);

            return result.Status ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Lines of a sales invoice that can still be returned (used by the Sales Return screen).
        /// Query: invoiceId OR invoiceNoText; returnId when editing a return; optional customerCode.
        /// </summary>
        [HttpGet("pending")]
        public async Task<IActionResult> Pending([FromQuery] SalesBillingPendingRequest request)
        {
            var result = await _service.GetPendingForReturnAsync(request);
            return result.Status ? Ok(result) : BadRequest(result);
        }
    }
}
