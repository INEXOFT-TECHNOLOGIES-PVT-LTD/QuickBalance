using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Services;

namespace QuickBalance.API.Controllers.Master
{
    [Route("api/v1/unit-conversions")]
    [Tags("18. UnitConversions")]
    [ApiController]
    [Authorize]
    public class UnitConversionsController : ControllerBase
    {
        private readonly IUnitConversionService _service;
        private readonly ICurrentUser _currentUser;

        private readonly ILogger<UnitConversionsController> _logger;

        public UnitConversionsController (ICurrentUser currentUser,IUnitConversionService service, ILogger<UnitConversionsController> logger)
        {
            _service = service;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet]

        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UnitConversionDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll(int page=1, int pageSize=20,string? search=null)
        {
            var response = await _service.GetAll(page, pageSize, search);
            return response.Status ? Ok(response) : BadRequest(response);
            //return Ok(response);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithoutPagination<UnitConversionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetByCode(int id)
        {
            var response = await _service.GetByCode(id);
            return response.Status ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create(
            [FromBody] UnitConversionRequestDto request)
        {
            var userId = _currentUser.UserId;
            var response = await _service.Create(userId,request);
            return response.Status ? Ok(response) : BadRequest(response);
        }

        [HttpPut("{id:int}")]

        [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UnitConversionRequestDto request)
        {
            var userId = _currentUser.UserId;
            var response = await _service.Update(userId, id, request);
            return response.Status ? Ok(response) : BadRequest(response);
        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(int id)
        {
            
            var response = await _service.Delete(id, _currentUser.UserId);
            return response.Status ? Ok(response) : BadRequest(response);
        }
    }
}