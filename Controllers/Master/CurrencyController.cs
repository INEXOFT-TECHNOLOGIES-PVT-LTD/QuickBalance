using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs.Currencies;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Services;

namespace QuickBalance.API.Controllers.Master
{
    [ApiController]
    [Route("api/v1/currencies")]
    [Tags("05. Currencies")]

    [Authorize]
    public class CurrencyController : ControllerBase
    {
        private readonly ICurrencyInterface _currencyService;

        private readonly ILogger<CurrencyController> _logger;

        private readonly ICurrentUser _currentUser;

        public CurrencyController(
            ICurrencyInterface currencyService,
            ILogger<CurrencyController> logger,
            ICurrentUser currentUser)
        {
            _currencyService = currencyService;
            _logger = logger;
            _currentUser = currentUser;
        }


        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<CurrencyResponse>>),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCurrencies([FromQuery] int page = 1,
         [FromQuery] int pageSize = 10,
         [FromQuery] string? search = null,
         [FromQuery] bool? excludeBase = null)
        {
            if (page < 0)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Invalid page number."
                });
            }
            if (pageSize <= 0 || pageSize > 100)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Invalid page size."
                });
            }
            var response = await _currencyService.GetCurrenciesAsync(page, pageSize, search, excludeBase ?? false);

            if (!response.Status)
                return BadRequest(response);

            return Ok(response);
        }


        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithoutPagination<CurrencyResponse>), StatusCodes.Status200OK)]

        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]

        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCurrencyById(int id)
        {
            var response = await _currencyService.GetCurrencyByIdAsync(id);

            if (!response.Status)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        // CREATE CURRENCY
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]

        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]

        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateCurrency([FromBody] CurrencyRequest request)
        {
            var response = await _currencyService.CreateCurrencyAsync(request);

            if (!response.Status)
                return BadRequest(response);

            return Ok(response);
        }

        // UPDATE CURRENCY
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]

        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]

        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateCurrency(
            int id,
            [FromBody] CurrencyRequest request)
        {
            var response = await _currencyService.UpdateCurrencyAsync(
                id,
                request);

            if (!response.Status)
                return BadRequest(response);

            return Ok(response);
        }


        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]

        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]

        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteCurrency(
           int id)
        {
            var response = await _currencyService.DeleteCurrencyAsync(
                id, _currentUser.UserId);

            if (!response.Status)
                return BadRequest(response);

            return Ok(response);
        }


        // =========================================================
        // GET ALTERNATE CURRENCIES
        // GET: api/v1/currencies/alternate
        // =========================================================
        //[HttpGet("alternate")]
        //[ProducesResponseType(typeof(ApiResponse<List<CurrencyResponse>>),StatusCodes.Status200OK)]

        //[ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]

        //[ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status500InternalServerError)]
        //public async Task<IActionResult> GetAlternateCurrencies(
        //    [FromQuery] int page = 1,
        //    [FromQuery] int pageSize = 10,
        //    [FromQuery] string? search = null)
        //{
        //    try
        //    {
        //        var response =
        //            await _currencyService.GetAlternateCurrenciesAsync(
        //                page,
        //                pageSize,
        //                search);

        //        if (!response.Status)
        //        {
        //            return BadRequest(response.Message);
        //        }

        //        return Ok(response);
        //    }
        //    catch (Exception)
        //    {
        //        return StatusCode(
        //            StatusCodes.Status500InternalServerError,
        //            ApiResponseWithError.Failure(
        //                "Failed to retrieve alternate currencies."));
        //    }
        //}
    }
}