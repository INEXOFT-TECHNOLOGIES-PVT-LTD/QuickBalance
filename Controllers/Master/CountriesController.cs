using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.PriceCategory;
using QuickBalance.API.DTOs.Respons_DTO;
using QuickBalance.API.Interfaces;

namespace QuickBalance.API.Controllers.Master
{
    namespace QuickBalance.API.Controllers.Master
    {

        [Route("api/v1/countries")]
        [Tags("04. Countries")]

        [ApiController]
        [Authorize]
        public class CountriesController : ControllerBase
        {
            private readonly ICountryService _service;

            public CountriesController(ICountryService service)
            {
                _service = service;
            }


            [HttpGet]
            [ProducesResponseType(typeof(ApiResponse<CountryDtos>),StatusCodes.Status200OK)]
            [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status400BadRequest)]
            [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status401Unauthorized)]
            [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status500InternalServerError)]
            public async Task<IActionResult> GetCountries(int page=1, int pageSize=20, string? search=null)
            {

                var response = await _service.GetCountries(page, pageSize, search);
                return response.Status ? Ok(response) : BadRequest(response);
                //return Ok(response);
            }

            // GET: api/v1/countries/5
            [HttpGet("{id:int}")]
            [ProducesResponseType(typeof(ApiResponseWithoutPagination<CountryDtos>), StatusCodes.Status200OK)]
            [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
            [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
            [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
            public async Task<IActionResult> GetCountryById(int id)
            {
                var response = await _service.GetCountryById(id);
                return response.Status ? Ok(response) : BadRequest(response);

                //return Ok(response);
            }

            // POST: api/v1/countries
            [HttpPost]
            [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
            [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
            [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
            [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
            public async Task<IActionResult> CreateCountry(
                [FromBody] CountryRequest request)
            {
                var response = await _service.CreateCountry(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }

            // PUT: api/v1/countries/5
            [HttpPut("{id:int}")]
            [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
            [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
            [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
            [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
            public async Task<IActionResult> UpdateCountry(
                int id,
                [FromBody] CountryRequest request)
            {
                var response = await _service.UpdateCountry(id, request);
                return response.Status ? Ok(response) : BadRequest(response);
            }

            // DELETE: api/v1/countries/5
            [HttpDelete("{id:int}")]
            [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
            [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
            [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
            [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
            public async Task<IActionResult> DeleteCountry(int id)
            {
                var response = await _service.DeleteCountry(id);
                return response.Status ? Ok(response) : BadRequest(response);
            }
        }
    }
}