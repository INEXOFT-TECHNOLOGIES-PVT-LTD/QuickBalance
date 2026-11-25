using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Tax;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Services;

namespace QuickBalance.API.Controllers.Master;

[ApiController]
[Route("api/v1/taxes")]
[Tags("15. Tax")]
[Authorize]
public sealed class TaxesController : ControllerBase
{
    private readonly ITaxInterface _service;
    private readonly ICurrentUser _currentUser;

    public TaxesController(
        ITaxInterface service,
        ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    // =========================================================
    // CREATE TAX
    // POST: api/taxes
    // =========================================================

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] TaxMasterRequest request)
    {
        var result = await _service.CreateAsync(
            request, _currentUser.UserId);

        return result.Status ? Ok(result) : BadRequest(result);
        //return Ok(result);
    }

    // =========================================================
    // GET ALL TAXES
    // GET: api/taxes?page=1&pageSize=20&search=GST
    // =========================================================

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TaxMasterResponse>>), StatusCodes.Status200OK)]
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
            page, pageSize, search, active);

        return result.Status ? Ok(result) : BadRequest(result);
    }

    // =========================================================
    // GET TAX BY ID
    // GET: api/taxes/1
    // =========================================================

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseWithoutPagination<TaxMasterResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);

        return result.Status ? Ok(result) : BadRequest(result);
    }

    // =========================================================
    // UPDATE TAX
    // PUT: api/taxes/1
    // =========================================================

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] TaxMasterRequest request)
    {
        var result = await _service.UpdateAsync(
            id, request, _currentUser.UserId);

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
            id, _currentUser.UserId);

        return result.Status ? Ok(result) : BadRequest(result);
    }

    // =========================================================
    // ACTIVE TAXES FOR ITEM MASTER
    // GET: api/taxes/active-for-item-master
    // =========================================================

    //[HttpGet("active-for-item-master")]
    //public async Task<IActionResult> GetActiveForItemMaster()
    //{
    //    var result =
    //        await _service.GetActiveForItemMasterAsync();

    //    return Ok(result);
    //}

    // =========================================================
    // ACTIVE ALL TAXES
    // GET: api/taxes/active-all
    // =========================================================

    //[HttpGet("active-all")]
    //public async Task<IActionResult> GetActiveAll()
    //{
    //    var result = await _service.GetActiveAllAsync();

    //    return Ok(result);
    //}

    //// =========================================================
    //// TAX PERCENTAGES
    //// GET: api/taxes/percentages
    //// =========================================================

    //[HttpGet("percentages")]
    //public async Task<IActionResult> GetPercentages()
    //{
    //    var result = await _service.GetPercentagesAsync();

    //    return Ok(result);
    //}
}