
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.Interfaces;
using System.Security.Claims;

namespace QuickBalance.API.Controllers.Master
{
    [ApiController]
    [Route("api/v1/currency-conversions")]
    [Tags("06. Currency Conversions")]
    [Authorize]
    public class CurrencyConversionController : ControllerBase
    {
        private readonly ICurrencyConversionInterface _service;
        private readonly ILogger<CurrencyConversionController> _logger;

        public CurrencyConversionController(
            ICurrencyConversionInterface service,
            ILogger<CurrencyConversionController> logger)
        {
            _service = service;
            _logger = logger;
        }



        // =========================================================
        // GET PAGINATED CURRENCY CONVERSIONS
        // GET: api/v1/currency-conversions?page=1&pageSize=10
        // =========================================================
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<CurrencyConversionResponse>>),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]

        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            try
            {
                var result =
                    await _service.GetConversionsAsync(
                        page,
                        pageSize,
                        search);

                return result.Status
             ? Ok(result)
             : BadRequest(result);

                //return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving currency conversion list.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseWithError.Failure(
                        "An unexpected error occurred while retrieving currency conversions."));
            }
        }

        // =========================================================
        // GET CURRENCY CONVERSION BY ID
        // GET: api/v1/currency-conversions/1
        // Returns all details belonging to the selected date.
        // =========================================================
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithoutPagination<CurrencyConversionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]

        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(
                        ApiResponseWithoutPagination<object>.Failure(
                            "Invalid conversion ID."));
                }

                var response =
                    await _service.GetConversionByIdAsync(id);

                if (!response.Status)
                {
                    if (response.Message.Contains(
                        "not found",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return BadRequest(
                            ApiResponseWithoutPagination<object>.Failure(
                                response.Message));
                    }

                    return BadRequest(
                        ApiResponseWithoutPagination<object>.Failure(
                            response.Message));
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving currency conversion. ID: {Id}",
                    id);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseWithoutPagination<object>.Failure(
                        "An unexpected error occurred while retrieving currency conversion."));
            }
        }


        // =========================================================
        // CREATE CURRENCY CONVERSION
        // POST: api/v1/currency-conversions
        // =========================================================
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create(
            [FromBody] CurrencyConversionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "Invalid request data."));
                }

                if (request == null)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "Request data is required."));
                }

                int userId = GetUserId();

                if (userId <= 0)
                {
                    return Unauthorized(
                        ApiResponseWithError.Failure(
                            "Invalid authenticated user."));
                }

                var response =
                    await _service.CreateConversionAsync(
                        request,
                        userId);

                if (!response.Status)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating currency conversion.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseWithError.Failure(
                        "An unexpected error occurred while creating currency conversion."));
            }
        }

        // =========================================================
        // UPDATE CURRENCY CONVERSION
        // PUT: api/v1/currency-conversions/1
        // =========================================================
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] CurrencyConversionRequest request)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "Invalid conversion ID."));
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "Invalid request data."));
                }

                if (request == null)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "Request data is required."));
                }

                int userId = GetUserId();

                if (userId <= 0)
                {
                    return Unauthorized(
                        ApiResponseWithError.Failure(
                            "Invalid authenticated user."));
                }

                var response =
                    await _service.UpdateConversionAsync(
                        id,
                        request,
                        userId);

                if (!response.Status)
                {
                    if (response.Message.Contains(
                        "not found",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return BadRequest(
                            ApiResponseWithError.Failure(
                                response.Message));
                    }

                    return BadRequest(
                        ApiResponseWithError.Failure(
                            response.Message));
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating currency conversion. ID: {Id}",
                    id);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseWithError.Failure(
                        "An unexpected error occurred while updating currency conversion."));
            }
        }

        // =========================================================
        // DELETE CURRENCY CONVERSION
        // DELETE: api/v1/currency-conversions/1
        // Deletes all details belonging to the selected date.
        // =========================================================
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "Invalid conversion ID."));
                }

                int userId = GetUserId();

                if (userId <= 0)
                {
                    return Unauthorized(
                        ApiResponseWithError.Failure(
                            "Invalid authenticated user."));
                }

                var response =
                    await _service.DeleteConversionAsync(id, userId);

                if (!response.Status)
                {
                    if (response.Message.Contains(
                        "not found",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return BadRequest(
                            ApiResponseWithError.Failure(
                                response.Message));
                    }

                    return BadRequest(
                        ApiResponseWithError.Failure(
                            response.Message));
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error deleting currency conversion. ID: {Id}",
                    id);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseWithError.Failure(
                        "An unexpected error occurred while deleting currency conversion."));
            }
        }

        // =========================================================
        // GET AVAILABLE CURRENCIES
        // GET: api/v1/currency-conversions/currencies
        // =========================================================
        //[HttpGet("currencies")]
     
        //[ProducesResponseType(typeof(ApiResponse<List<CurrencyConversionCurrencyResponse>>), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        //[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        //public async Task<IActionResult> GetCurrencies(
        //    [FromQuery] int page = 1,
        //    [FromQuery] int pageSize = 10,
        //    [FromQuery] string? search = null)
        //{
        //    try
        //    {
        //        var response =
        //            await _service.GetCurrenciesAsync(page, pageSize, search);

        //        if (!response.Status)
        //        {
        //            return BadRequest(
        //                ApiResponseWithError.Failure(
        //                    response.Message));
        //        }

        //        return Ok(response);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(
        //            ex,
        //            "Error retrieving available currencies.");

        //        return StatusCode(
        //            StatusCodes.Status500InternalServerError,
        //            ApiResponseWithError.Failure(
        //                "An unexpected error occurred while retrieving currencies."));
        //    }
        //}

        // =========================================================
        // GET AUTHENTICATED USER ID
        // =========================================================
        private int GetUserId()
        {
            string? userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            return int.TryParse(userId, out int id)
                ? id
                : 0;
        }
    }
}

