
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Respons_DTO;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Services;

namespace QuickBalance.API.Controllers;

[ApiController]
[Route("api/v1/item-group")]
[Tags("11.Item Groups")]
[Authorize]
public sealed class ItemGroupController : ControllerBase
{
    private readonly IItemGroupInterface _service;
    private readonly ILogger<ItemGroupController> _logger;
    private readonly ICurrentUser _currentUser;

    public ItemGroupController(
        IItemGroupInterface service,
        ILogger<ItemGroupController> logger,
        ICurrentUser currentUser)
    {
        _service = service;
        _logger = logger;
        _currentUser = currentUser;
    }

    // CREATE
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(ItemGroupRequest request)
    {
        var result = await _service.CreateAsync(
            request,
            _currentUser.UserId);

        return result.Status
            ? Ok(result)
            : BadRequest(result);
    }

    // GET ALL
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ItemGroupListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationRequests request,
        bool? active)
    {
        var result = await _service.GetAllAsync(
            request.Page,
            request.PageSize,
            request.Search,
            active);

        return result.Status
                    ? Ok(result)
                    : BadRequest(result);
    }

    // GET BY ID
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseWithoutPagination<ItemGroupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);

        return result.Status
             ? Ok(result)
             : BadRequest(result);

    }

    // UPDATE
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(int id,ItemGroupRequest request)
    {
        var result = await _service.UpdateAsync(
            id,
            request,
            _currentUser.UserId);
        return result.Status
            ? Ok(result)
            : BadRequest(result);

    }

    // DELETE
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id, _currentUser.UserId);

        return result.Status
            ? Ok(result)
            : BadRequest(result);

    }

    // GET PRIMARY GROUPS
    // Without `page`: full active list (ApiResponseWithoutPagination).
    // With `page`: paged list (ApiResponse with pagination). `search` works in both.
    [HttpGet("primary-groups")]
    [ProducesResponseType(typeof(ApiResponseWithoutPagination<IReadOnlyList<ItemGroupResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ItemGroupResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPrimaryGroups(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? search)
    {
        if (page.HasValue)
        {
            var paged = await _service.GetPrimaryGroupsPagedAsync(
                page.Value, pageSize ?? 20, search);

            return paged.Status
                ? Ok(paged)
                : BadRequest(paged);
        }

        var result = await _service.GetPrimaryGroupsAsync(search);

        return result.Status
            ? Ok(result)
            : BadRequest(result);
    }
}
