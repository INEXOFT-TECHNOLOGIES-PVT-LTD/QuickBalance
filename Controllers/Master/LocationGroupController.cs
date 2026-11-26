using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Services;

namespace QuickBalance.API.Controllers.Master
{
    [ApiController]
    [Route("api/v1/location-groups")]
    [Tags("20. Location Groups / Godown Group")]
    [Authorize]
    public class LocationGroupController : ControllerBase
    {
        private readonly ILocationGroupService _service;
        private readonly ICurrentUser _currentUser;

        public LocationGroupController(
            ILocationGroupService service,
            ICurrentUser currentUser)
        {
            _service = service;
            _currentUser = currentUser;
        }

        // =========================================================
        // CREATE
        // POST: api/v1/location-groups
        // =========================================================

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponseWithSuccess),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create(
            [FromBody] LocationGroupMasterRequest request)
        {
            int userId = _currentUser.UserId;

            if (userId <= 0)
            {
                return Unauthorized(
                    ApiResponseWithError.Failure(
                        "Invalid user information."));
            }

            try
            {
                var result = await _service.CreateAsync(
                    request,
                    userId);

                return result.Status ? Ok(result) : BadRequest(result);
            }
            catch (Exception)
            {
                // Already logged by the service.
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseWithError.Failure(
                        "An unexpected error occurred while creating the location group. Please try again."));
            }
        }

        // =========================================================
        // GET ALL
        // GET: api/v1/location-groups?page=1&pageSize=20&search=abc
        // =========================================================

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LocationGroupListItemDto>>),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] bool? active = null)
        {
            try
            {
                var result = await _service.GetAllAsync(
                    page,
                    pageSize,
                    search,
                    active);

                return result.Status ? Ok(result) : BadRequest(result);
            }
            catch (Exception)
            {
                // Already logged by the service.
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseWithError.Failure(
                        "Unable to load location groups. Please try again."));
            }
        }

        // =========================================================
        // GET BY ID
        // GET: api/v1/location-groups/10
        // =========================================================

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithoutPagination<LocationGroupDto>),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id);

                return result.Status ? Ok(result) : BadRequest(result);
            }
            catch (Exception)
            {
                // Already logged by the service.
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseWithError.Failure(
                        "Unable to load the location group. Please try again."));
            }
        }

        // =========================================================
        // UPDATE
        // PUT: api/v1/location-groups/10
        // =========================================================

        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithSuccess),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] LocationGroupMasterRequest request)
        {
            int userId = _currentUser.UserId;

            if (userId <= 0)
            {
                return Unauthorized(
                    ApiResponseWithError.Failure(
                        "Invalid user information."));
            }

            try
            {
                var result = await _service.UpdateAsync(
                    id,
                    request,
                    userId);

                return result.Status ? Ok(result) : BadRequest(result);
            }
            catch (Exception)
            {
                // Already logged by the service.
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseWithError.Failure(
                        "An unexpected error occurred while updating the location group. Please try again."));
            }
        }




        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithSuccess),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(
        int id)
        {
            int userId = _currentUser.UserId;

            if (userId <= 0)
            {
                return Unauthorized(
                    ApiResponseWithError.Failure(
                        "Invalid user information."));
            }

            try
            {
                var result = await _service.DeleteAsync(
                    id, userId);

                return result.Status ? Ok(result) : BadRequest(result);
            }
            catch (Exception)
            {
                // Already logged by the service.
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseWithError.Failure(
                        "An unexpected error occurred while deleting the location group. Please try again."));
            }
        }
    }
}
