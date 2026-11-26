using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Services;

namespace QuickBalance.API.Controllers.Master;

[ApiController]

[Tags("14. Unit Of Measure")]
[Route("api/v1/units")]
[Authorize]
public sealed class UnitsController : ControllerBase
{
    private readonly IUnitInterface _service;
    private readonly ICurrentUser _currentUser;

    public UnitsController(
        IUnitInterface service,
        ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    // =========================================================
    // GET ALL
    // GET: api/units?page=1&pageSize=20&search=kg
    // =========================================================

    [HttpGet]

    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UnitMasterResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
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
    // GET: api/units/1
    // =========================================================

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseWithoutPagination<UnitMasterResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(
            id,
            _currentUser.UserId);

        return result.Status ? Ok(result) : BadRequest(result);
    }

    // =========================================================
    // CREATE
    // POST: api/units
    // =========================================================

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] UnitMasterRequest request)
    {
        var result = await _service.CreateAsync(
            request,
            _currentUser.UserId);

        return result.Status ? Ok(result) : BadRequest(result);
    }

    // =========================================================
    // UPDATE
    // PUT: api/units/1
    // =========================================================

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UnitMasterRequest request)
    {
        var result = await _service.UpdateAsync(
            id,
            request,
            _currentUser.UserId);

        return result.Status ? Ok(result) : BadRequest(result);
    }



    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(
        int id)
    {
        var result = await _service.DeleteAsync(
            id,
            _currentUser.UserId);
            
        return result.Status ? Ok(result) : BadRequest(result);
    }


    // =========================================================
    // ACTIVE UNITS
    // GET: api/units/active
    // =========================================================

    //[HttpGet("active")]
    //[ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
    //[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    //[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    //[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    //public async Task<IActionResult> GetActive()
    //{
    //    var result = await _service.GetActiveAsync(
    //        _currentUser.UserId);

    //    return Ok(result);
    //}
}