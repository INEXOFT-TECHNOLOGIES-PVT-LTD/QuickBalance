using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Respons_DTO;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Services;
using System.Security.Claims;

[ApiController]
[Route("api/v1/boms")]
[Tags("19. Bill Of Matirial")]
[Authorize]
public class BomsController : ControllerBase
{
    private readonly IBomService _service;
    private readonly ICurrentUser _currentUser;

    public BomsController(IBomService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    // ---------------------------------------------------------
    // CREATE BOM
    // POST: api/v1/boms
    // ---------------------------------------------------------

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]

    public async Task<IActionResult> Create([FromBody] BomRequest request)
    {
        int userId = _currentUser.UserId;

        if (userId <= 0)
            return Unauthorized(new { status = false, message = "Invalid user information." });

        var response = await _service.CreateAsync(request, userId);

        return response.Status ? Ok(response) : BadRequest(response);
    }

    // ---------------------------------------------------------
    // GET ALL BOMs
    // GET: api/v1/boms?page=0&pageSize=20&search=chair
    // ---------------------------------------------------------

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BomListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? active = null)
    {
        var response = await _service.GetAllAsync(
            page,
            pageSize,
            search,
            active);

        return response.Status ? Ok(response) : BadRequest(response);
    }

    // ---------------------------------------------------------
    // GET BOM BY ID
    // GET: api/v1/boms/10
    // ---------------------------------------------------------

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseWithoutPagination<BomDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(int id)
    {
        var response = await _service.GetByIdAsync(id);
        return response.Status ? Ok(response) : BadRequest(response);
        //return Ok(response);
    }

    // ---------------------------------------------------------
    // UPDATE bill of materials
    // PUT: api/v1/boms/10
    // ---------------------------------------------------------

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] BomRequest request)
    {
        int userId = _currentUser.UserId;

        if (userId <= 0)
            return Unauthorized(new { status = false, message = "Invalid user information." });

        var response = await _service.UpdateAsync(id, request, userId);

        return response.Status ? Ok(response) : BadRequest(response);
    }

    // ---------------------------------------------------------
    // DELETE bill of materials
    // DELETE: api/v1/boms/10
    // ---------------------------------------------------------

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(int id)
    {
        int userId = _currentUser.UserId;

        if (userId <= 0)
            return Unauthorized(new { status = false, message = "Invalid user information." });

        var response = await _service.DeleteAsync(id, userId);

        return response.Status ? Ok(response) : BadRequest(response);
    }

    // ---------------------------------------------------------
    // GET bill of materials ITEMS BY PRODUCT
    // GET: api/v1/boms/product/15
    // ---------------------------------------------------------

    //[HttpGet("product/{productId:int}")]
    //[ProducesResponseType(typeof(ApiResponseWithoutPagination<BomItemDto>), StatusCodes.Status200OK)]
    //[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    //[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    //[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    //public async Task<IActionResult> GetByProductCode(int productId)
    //{
    //    var response = await _service.GetByProductCodeAsync(productId);

    //    return response.Status ? Ok(response) : BadRequest(response);
    //}
}


