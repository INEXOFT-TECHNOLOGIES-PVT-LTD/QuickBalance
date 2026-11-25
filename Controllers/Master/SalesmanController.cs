using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.SalesMan;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Services;

namespace QuickBalance.API.Controllers.Master;

[ApiController]
[ApiExplorerSettings(IgnoreApi = false)]

[Tags("24. Salesman")]
[Route("api/v1/salesman")]
[Authorize]
public sealed class SalesmanController : ControllerBase
{
    private readonly ISalesmanInterface _service;
    private readonly ICurrentUser _currentUser;

    public SalesmanController(
        ISalesmanInterface service,
        ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    // =========================================================
    // GET ALL
    // GET: api/v1/salesmen?page=1&pageSize=20&search=
    // =========================================================

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SalesmanResponse>>),StatusCodes.Status200OK,Description = "Salesmen retrieved successfully .")]
    [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status400BadRequest,Description = "Invalid pagination parameters or user ID.")]
    [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status401Unauthorized,Description = "Unauthorized access.")]
    [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status500InternalServerError,Description = "Internal server error.")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? active = null)
    {
        var result = await _service.GetAllAsync(
            page,
            pageSize,
            search,
            _currentUser.UserId,
            active);

        return result.Status ? Ok(result) : BadRequest(result);
    }

    // =========================================================
    // GET BY ID
    // GET: api/v1/salesmen/1
    // =========================================================

    [HttpGet("{id:int}")]
    [ProducesResponseType(
        typeof(ApiResponseWithouPagination<SalesmanResponse>),
        StatusCodes.Status200OK,
        Description = "Salesman retrieved successfully.")]
    [ProducesResponseType(
        typeof(ApiResponseWithError),
        StatusCodes.Status400BadRequest,
        Description = "Invalid salesman ID or user ID.")]
    [ProducesResponseType(
        typeof(ApiResponseWithError),
        StatusCodes.Status401Unauthorized,
        Description = "Unauthorized access.")]
    [ProducesResponseType(
        typeof(ApiResponseWithError),
        StatusCodes.Status500InternalServerError,
        Description = "Internal server error.")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(
            id,
            _currentUser.UserId);

        return result.Status ? Ok(result) : BadRequest(result);
    }

    // =========================================================
    // CREATE
    // POST: api/v1/salesmen
    // =========================================================

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseWithSuccess),StatusCodes.Status200OK,Description = "Salesman created successfully.")]
    [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status400BadRequest,Description = "Invalid salesman data, duplicate salesman name, or invalid user ID.")]
    [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status401Unauthorized,Description = "Unauthorized access.")]
    [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status500InternalServerError,Description = "Internal server error.")]
    public async Task<IActionResult> Create(
        [FromBody] SalesmanRequest request)
    {
        var result = await _service.CreateAsync(
            request,
            _currentUser.UserId);

        return result.Status ? Ok(result) : BadRequest(result);
    }

    // =========================================================
    // UPDATE
    // PUT: api/v1/salesmen/1
    // =========================================================

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseWithSuccess),StatusCodes.Status200OK,Description = "Salesman updated successfully.")]
    [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status400BadRequest,Description = "Invalid salesman data, duplicate salesman name, or invalid ID.")]
    [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status401Unauthorized,Description = "Unauthorized access.")]
    [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status500InternalServerError,Description = "Internal server error.")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SalesmanRequest request)
    {
        var result = await _service.UpdateAsync(
            id,
            request,
            _currentUser.UserId);

        return result.Status ? Ok(result) : BadRequest(result);
    }


    // =========================================================
    // DELETE - SOFT DELETE
    // DELETE: api/v1/salesmen/1
    // =========================================================

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK,Description = "Salesman deleted successfully.")]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest,Description = "Invalid salesman ID or salesman not found.")]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized,Description = "Unauthorized access.")]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError,Description = "Internal server error.")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(
            id,
            _currentUser.UserId);

        return result.Status ? Ok(result) : BadRequest(result);
    }

    // =========================================================
    // ACTIVE SALESMEN
    // GET: api/v1/salesmen/active
    // =========================================================

    //[HttpGet("active")]
    //[ProducesResponseType(
    //    typeof(ApiResponseWithouPagination<ActiveSalesmanResponse>),
    //    StatusCodes.Status200OK,
    //    Description = "Active salesmen retrieved successfully.")]
    //[ProducesResponseType(
    //    typeof(ApiResponseWithError),
    //    StatusCodes.Status400BadRequest)]
    //[ProducesResponseType(
    //    typeof(ApiResponseWithError),
    //    StatusCodes.Status401Unauthorized)]
    //[ProducesResponseType(
    //    typeof(ApiResponseWithError),
    //    StatusCodes.Status500InternalServerError)]
    //public async Task<IActionResult> GetActive()
    //{
    //    var result = await _service.GetActiveAsync(
    //        _currentUser.UserId);

    //    return result.Status ? Ok(result) : BadRequest(result);
    //}

    // =========================================================
    // LOOKUP
    // GET: api/v1/salesmen/lookup
    // =========================================================

    //[HttpGet("lookup")]
    //[ProducesResponseType(
    //    typeof(ApiResponseWithouPagination<SalesmanLookupResponse>),
    //    StatusCodes.Status200OK)]
    //[ProducesResponseType(
    //    typeof(ApiResponseWithError),
    //    StatusCodes.Status400BadRequest)]
    //[ProducesResponseType(
    //    typeof(ApiResponseWithError),
    //    StatusCodes.Status401Unauthorized)]
    //[ProducesResponseType(
    //    typeof(ApiResponseWithError),
    //    StatusCodes.Status500InternalServerError)]
    //public async Task<IActionResult> GetLookup()
    //{
    //    var result = await _service.GetLookupAsync(
    //        _currentUser.UserId);

    //    return result.Status ? Ok(result) : BadRequest(result);
    //}

    // =========================================================
    // ACCOUNT VALIDATION
    // GET: api/v1/salesmen/account/100
    // =========================================================

    //[HttpGet("account/{accountId:int}")]
    //[ProducesResponseType(
    //    typeof(ApiResponseWithouPagination<SalesmanAccountValidationResponse>),
    //    StatusCodes.Status200OK,
    //    Description = "Account validation completed successfully.")]
    //[ProducesResponseType(
    //    typeof(ApiResponseWithError),
    //    StatusCodes.Status400BadRequest,
    //    Description = "Invalid account ID or user ID.")]
    //[ProducesResponseType(
    //    typeof(ApiResponseWithError),
    //    StatusCodes.Status401Unauthorized)]
    //[ProducesResponseType(
    //    typeof(ApiResponseWithError),
    //    StatusCodes.Status500InternalServerError)]
    //public async Task<IActionResult> ValidateAccount(int accountId)
    //{
    //    var result = await _service.ValidateAccountAsync(
    //        accountId,
    //        _currentUser.UserId);

    //    return result.Status ? Ok(result) : BadRequest(result);
    //}
}

internal class ApiResponseWithouPagination<T>
{
}