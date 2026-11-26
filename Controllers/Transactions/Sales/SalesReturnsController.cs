using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs.Sales.SalesReturn;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Services;

namespace QuickBalance.API.Controllers.Transactions.Sales
{
    /// <summary>
    /// NEW Sales Return API (layered). Replaces the old, hidden SalesReturnController
    /// (api/v1/sales-returns), which is left untouched.
    /// Transactions use RPC-style routes (manage / list / get / cancel), see CLAUDE.md.
    /// </summary>
    [ApiController]
    [Tags("27. Sales Returns")]
    [Route("api/v2/sales-returns")]
    [Authorize]
    public sealed class SalesReturnsController : ControllerBase
    {
        private readonly ISalesReturnInterface _salesReturnService;
        private readonly ICurrentUser _currentUser;

        public SalesReturnsController(ISalesReturnInterface salesReturnService, ICurrentUser currentUser)
        {
            _salesReturnService = salesReturnService;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Create (ReturnId = 0) or update (ReturnId > 0) a sales return.
        /// A new return needs the Idempotency-Key header (one new key per new return; reuse it only for retries).
        /// </summary>
        /// <response code="201">New sales return created.</response>
        /// <response code="200">Sales return updated (or an exact retry of a create).</response>
        /// <response code="400">Validation or business rule failed (message explains why).</response>
        [HttpPost("manage")]
        public async Task<IActionResult> Manage(
            [FromBody] SalesReturnSaveRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
        {
            var result = await _salesReturnService.SaveAsync(request, idempotencyKey, _currentUser.UserId);

            if (!result.Status)
                return BadRequest(result);

            return request.ReturnId == 0
                ? StatusCode(StatusCodes.Status201Created, result)
                : Ok(result);
        }

        /// <summary>One sales return (header, items, charges, serial numbers). id = ReturnId (SALM.INVNO).</summary>
        [HttpGet("get/{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _salesReturnService.GetByIdAsync(id);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        /// <summary>Paged list of sales returns. Sort: returnNo / date / customer / amount.</summary>
        [HttpGet("list")]
        public async Task<IActionResult> List([FromQuery] SalesReturnListRequest request)
        {
            var result = await _salesReturnService.GetListAsync(request);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Cancel a sales return (soft cancel). id = ReturnId (SALM.INVNO).
        /// Stock is reversed; the voucher stays in the database with IsCancelled = true.
        /// </summary>
        [HttpPost("cancel/{id:int}")]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await _salesReturnService.CancelAsync(id, _currentUser.UserId);
            return result.Status ? Ok(result) : BadRequest(result);
        }
    }
}
