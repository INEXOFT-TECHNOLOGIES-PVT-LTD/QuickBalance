
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs.Purchase;
using QuickBalance.API.Interfaces.Purchase;
using QuickBalance.API.Services;

namespace QuickBalance.API.Controllers.Transactions.Purchase
{
    [Authorize]
    [ApiController]
    [Route("api/v1/purchase-orders")]
    public class PurchaseOrderController : ControllerBase
    {
        private readonly IPurchaseOrderInterface _purchaseOrderService;
        private readonly ICurrentUser _currentUser;

        public PurchaseOrderController(
            IPurchaseOrderInterface purchaseOrderService,
            ICurrentUser currentUser)
        {
            _purchaseOrderService = purchaseOrderService;
            _currentUser = currentUser;
        }

        // CREATE PURCHASE ORDER
        [HttpPost]
        public async Task<IActionResult> CreatePurchaseOrder(
            [FromBody] PurchaseOrderCreateRequest request)
        {
            int userId = _currentUser.UserId;

            if (userId <= 0)
                return Unauthorized(new
                {
                    status = false,
                    message = "Invalid user information."
                });
            var result = await _purchaseOrderService.CreatePurchaseOrderAsync(request, userId);

            return Ok(new
            {
                status = "success",
                message = "Purchase order created successfully.",
                data = result
            });
        }

        // ---------------------------------------------------------
        // GET ALL PURCHASE ORDERS
        // GET: api/v1/purchase/orders?page=1&pageSize=20&search=chair
        // ---------------------------------------------------------

        [HttpGet]
        [ProducesResponseType(
            typeof(ApiResponse<PurchaseOrderListResult>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            typeof(ApiResponseWithError),
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            typeof(ApiResponseWithError),
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            typeof(ApiResponseWithError),
            StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null)
        {
            var response = await _purchaseOrderService.GetAllAsync(
                page,
                pageSize,
                search);

            return response.Status
                ? Ok(response)
                : BadRequest(response);
        }



        // ---------------------------------------------------------
        // GET PURCHASE ORDER BY ID
        // GET: api/v1/purchase/orders/10
        // ---------------------------------------------------------

        [HttpGet("{id:int}")]
        [ProducesResponseType(
            typeof(ApiResponseWithoutPagination<PurchaseOrderDetailResult>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            typeof(ApiResponseWithError),
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            typeof(ApiResponseWithError),
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            typeof(ApiResponseWithError),
            StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _purchaseOrderService.GetPurchaseOrderByIdAsync(id);

            return response.Status ? Ok(response) : BadRequest(response);
        }

        // ---------------------------------------------------------
        // DELETE PURCHASE ORDER (SOFT DELETE)
        // DELETE: api/v1/purchase/orders/10
        // ---------------------------------------------------------

        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeletePurchaseOrder(int id)
        {
            var response = await _purchaseOrderService.DeletePurchaseOrderAsync(id);

            return response.Status ? Ok(response) : BadRequest(response);
        }





        // UPDATE PURCHASE ORDER
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePurchaseOrder(
            int id,
            [FromBody] PurchaseOrderCreateRequest request)
        {
            var result = await _purchaseOrderService.UpdatePurchaseOrderAsync(
                id, request);

            return Ok(new
            {
                status = "success",
                message = "Purchase order updated successfully.",
                data = result
            });
        }
    }
}
