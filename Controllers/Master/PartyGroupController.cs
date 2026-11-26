using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.PartyGroup;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Services;

namespace QuickBalance.API.Controllers.Master
{
    [ApiController]
    [Route("api/v1/party-groups")]
    [Tags("22. Party Groups")]
    [Authorize]
    public sealed class PartyGroupController : ControllerBase
    {
        private readonly IPartyGroupInterface _service;
        private readonly ICurrentUser _currentUser;

        public PartyGroupController(
            IPartyGroupInterface service,
            ICurrentUser currentUser)
        {
            _service = service;
            _currentUser = currentUser;
        }

        // GET ALL
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PartyGroupListItem>>),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll(
            [FromQuery] string groupType,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null)
        {
            var result = await _service.GetAllAsync(groupType, page, pageSize, search);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        // GET BY ID
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<PartyGroupResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        // CREATE
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponseWithSuccess),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] PartyGroupRequest request)
        {
            var result = await _service.CreateAsync(request);
            return result.Status ? Ok(result) : BadRequest(result);
        }




        // UPDATE
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithSuccess),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(int id,[FromBody] PartyGroupRequest request)
        {
            var result = await _service.UpdateAsync(id, request);

            return result.Status ? Ok(result) : BadRequest(result);

        }

        // DELETE
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithSuccess),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id, _currentUser.UserId);
            return result.Status ? Ok(result) : BadRequest(result);

        }
    }
}