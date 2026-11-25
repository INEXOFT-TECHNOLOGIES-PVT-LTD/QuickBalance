using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Brand;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Services;

namespace QuickBalance.API.Controllers.Master
{
   
    [ApiController]
    [Tags("13.Item Brand")]
    [Route("api/v1/brands")]
    [Authorize]
    public sealed class BrandsController : ControllerBase
    {
        private readonly IBrandInterface _service;
        private readonly ICurrentUser _currentUser;

        public BrandsController(
            IBrandInterface service,
            ICurrentUser currentUser)
        {
            _service = service;
            _currentUser = currentUser;
        }

        // GET: api/brands?page=1&pageSize=20&search=sony
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BrandResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null)
        {
            var result = await _service.GetAllAsync(
                page, pageSize, search);

            return result.Status ? Ok(result) : BadRequest(result);
        }

        // GET: api/brands/1
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithoutPagination<BrandResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);

            return result.Status ? Ok(result) : BadRequest(result);
        }

        // POST: api/brands
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create(
            [FromBody] BrandRequest request)
        {
            var result = await _service.CreateAsync(request);
    
            return result.Status ? Ok(result) : BadRequest(result);
        }

        // PUT: api/brands/1
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] BrandRequest request)
        {
            var result = await _service.UpdateAsync(id, request);


            return result.Status ? Ok(result) : BadRequest(result);
        }

        // DELETE: api/brands/1
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id, _currentUser.UserId);

            return result.Status ? Ok(result) : BadRequest(result);
        }

        // GET: api/brands/active
        //[HttpGet("active")]
        //public async Task<IActionResult> GetActive()
        //{
        //    var result = await _service.GetActiveAsync();

        //    return Ok(result);
        //}
    }
}
