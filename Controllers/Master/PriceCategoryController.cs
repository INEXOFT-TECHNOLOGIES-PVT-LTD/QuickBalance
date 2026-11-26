
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.PriceCategory;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Services;

namespace QuickBalance.API.Controllers.Master;

[ApiController]
[Route("api/v1/price-categories")]
[Tags("16. Price Categories")]
[Authorize]
public sealed class PriceCategoryController : ControllerBase
{
    private readonly IPriceCategoryInterface _service;
    private readonly ILogger<PriceCategoryController> _logger;
    private readonly ICurrentUser _currentUser;

    public PriceCategoryController(
        IPriceCategoryInterface service,
        ILogger<PriceCategoryController> logger,
        ICurrentUser currentUser)
    {
        _service = service;
        _logger = logger;
        _currentUser = currentUser;
    }

    // CREATE
    [HttpPost]
    [ProducesResponseType(
    typeof(ApiResponseWithSuccess),
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
    public async Task<IActionResult> Create(
        [FromBody] PriceCategoryRequest request)
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
    [ProducesResponseType(
    typeof(ApiResponse<IReadOnlyList<PriceCategoryListItemDto>>),
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
        [FromQuery] PaginationRequests request)
    {
        var result = await _service.GetAllAsync(
            request.Page,
            request.PageSize,
            request.Search);

        return result.Status
                  ? Ok(result)
                  : BadRequest(result);
    }

    // GET BY ID
    [HttpGet("{id:int}")]

    [ProducesResponseType(
    typeof(ApiResponseWithoutPagination<PriceCategoryController>),
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
        var result = await _service.GetByIdAsync(id);

        if (!result.Status)
        {
            if (result.Message.Contains(
                "not found",
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(result);
            }

            return BadRequest(result);
        }

        return Ok(result);
    }

    // UPDATE
    [HttpPut("{id:int}")]
    [ProducesResponseType(
    typeof(ApiResponseWithSuccess),
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
    public async Task<IActionResult> Update(
        int id,
        [FromBody] PriceCategoryRequest request)
    {
        var result = await _service.UpdateAsync(
            id,
            request,
            _currentUser.UserId);

        if (!result.Status)
        {
            if (result.Message.Contains(
                "not found",
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(result);
            }

            return BadRequest(result);
        }

        return Ok(result);
    }

    // DELETE
    [HttpDelete("{id:int}")]
    [ProducesResponseType(
    typeof(ApiResponseWithSuccess),
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
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id, _currentUser.UserId);

        if (!result.Status)
        {
            if (result.Message.Contains(
                "not found",
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(result);
            }

            return BadRequest(result);
        }

        return Ok(result);
    }
}
