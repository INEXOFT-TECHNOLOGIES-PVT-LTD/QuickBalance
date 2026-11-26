using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.DTOs.Purchase;
using QuickBalance.API.Interfaces.Purchase;
// VERIFY: namespaces of ICurrentUser and ApiResponse<T>    
using QuickBalance.API.Services;
using QuickBalance.API.CLS;

namespace QuickBalance.API.Controllers.Transactions.Purchase
{
    [ApiController]
    [Tags("28. Purchase Invoices")]
    [Route("api/v1/purchase-invoices")]
    [Authorize]
    public class PurchaseInvoiceController : ControllerBase
    {
        private readonly IPurchaseInvoiceService _purchaseInvoiceService;
        private readonly ICurrentUser _currentUser;

        public PurchaseInvoiceController(
            IPurchaseInvoiceService purchaseInvoiceService,
            ICurrentUser currentUser)
        {
            _purchaseInvoiceService = purchaseInvoiceService;
            _currentUser = currentUser;
        }

        /// <summary>Create a new purchase invoice (PURM, PURT, PURD, STKDT).</summary>    
        [HttpPost("create")]
        [ProducesResponseType(typeof(ApiResponse<PurchaseInvoiceCreateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PurchaseInvoiceCreateResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] PurchaseInvoiceCreateRequest request)
        {
            var result = await _purchaseInvoiceService.CreateAsync(request, _currentUser.UserId);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        /// <summary>Update an existing purchase invoice (delete-and-reinsert of details).</summary>    
        [HttpPut("update/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<PurchaseInvoiceCreateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PurchaseInvoiceCreateResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] PurchaseInvoiceUpdateRequest request)
        {
            var result = await _purchaseInvoiceService.UpdateAsync(id, request, _currentUser.UserId);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        /// <summary>Get one purchase invoice by VRNO.</summary>    
        [HttpGet("get/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<PurchaseInvoiceResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PurchaseInvoiceResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _purchaseInvoiceService.GetByIdAsync(id);
            return result.Status ? Ok(result) : BadRequest(result);
        }
    }
}
