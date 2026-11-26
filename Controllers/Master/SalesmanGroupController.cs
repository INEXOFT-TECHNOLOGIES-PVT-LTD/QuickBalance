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

[Tags("23. Salesman Group")]
[Route("api/v1/salesman-groups")]
[Authorize]
public sealed class SalesmanGroupController : ControllerBase
{
    private readonly ISalesmanGroupInterface _service;
    private readonly ICurrentUser _currentUser;

    public SalesmanGroupController(
        ISalesmanGroupInterface service,
        ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    // =========================================================
    // GET ALL
    // GET: api/v1/salesman-groups?page=1&pageSize=20&search=
    // =========================================================

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SalesmanGroupListItemDto>>), StatusCodes.Status200OK,
        Description = "Salesman groups retrieved successfully with pagination.")]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest,
        Description = "Invalid pagination parameters or user ID.")]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized,
        Description = "Unauthorized access.")]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError,
        Description = "Internal server error.")]

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
    // GET: api/v1/salesman-groups/1
    // =========================================================

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseWithouPagination<SalesmanGroupResponse>), StatusCodes.Status200OK,
        Description = "Salesman group retrieved successfully.")]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest,
        Description = "Invalid salesman group ID or user ID.")]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized,
        Description = "Unauthorized access.")]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError,
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
    // POST: api/v1/salesman-groups
    // =========================================================

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK,
        Description = "Salesman group created successfully. Returns the new salesman group ID.")]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest,
        Description = "Invalid salesman group data, duplicate group name, or invalid user ID.")]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized,
        Description = "Unauthorized access.")]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError,
        Description = "Internal server error.")]

    public async Task<IActionResult> Create(
        [FromBody] SalesmanGroupRequest request)
    {
        var result = await _service.CreateAsync(
            request,
            _currentUser.UserId);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    // =========================================================
    // UPDATE
    // PUT: api/v1/salesman-groups/1
    // =========================================================

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK,
        Description = "Salesman group updated successfully.")]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest,
        Description = "Invalid salesman group data, duplicate group name, or invalid ID.")]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized,
        Description = "Unauthorized access.")]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError,
        Description = "Internal server error.")]

    public async Task<IActionResult> Update(
        int id,
        [FromBody] SalesmanGroupRequest request)
    {
        var result = await _service.UpdateAsync(
            id,
            request,
            _currentUser.UserId);

        return result.Status ? Ok(result) : BadRequest(result);
    }
    // =========================================================
    // DELETE - SOFT DELETE
    // DELETE: api/v1/salesman-groups/1
    // =========================================================

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK,
        Description = "Salesman group deleted successfully.")]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest,
        Description = "Invalid ID, group not found, or group is assigned to salesmen.")]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized,
        Description = "Unauthorized access.")]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError,
        Description = "Internal server error.")]

    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(
            id,
            _currentUser.UserId);

        return result.Status ? Ok(result) : BadRequest(result);
    }


    // =========================================================
    // LOOKUP
    // GET: api/v1/salesman-groups/lookup
    // =========================================================

    // [HttpGet("lookup")]
    // [ProducesResponseType(typeof(ApiResponseWithouPagination<SalesmanGroupLookupResponse>), StatusCodes.Status200OK)]
    //[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    // [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    // [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    // public async Task<IActionResult> Lookup()
    // {
    //     var result = await _service.GetLookupAsync(
    //         _currentUser.UserId);

    //     return result.Status ? Ok(result) : BadRequest(result);
    // }

    // =========================================================
    // COMMON RESPONSE HANDLER
    // =========================================================

    private IActionResult HandleResponse<T>(ApiResponse<T> result)
    {
        if (result.Status)
            return Ok(result);

        if (result.Message.Contains(
            "not found",
            StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(
                ApiResponseWithError.Failure(result.Message));
        }

        return BadRequest(
            ApiResponseWithError.Failure(result.Message));
    }
}

