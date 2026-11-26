using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Currencies;
using QuickBalance.API.Interfaces;

namespace QuickBalance.API.Controllers.Master;

[ApiController]
[Tags("08. Regions")]
[Route("api/v1/regions")]
[Authorize]
public sealed class RegionController : ControllerBase
{
    private readonly IRegionInterface _regionService;

    public RegionController(IRegionInterface regionService)
    {
        _regionService = regionService;
    }

    // GET: api/regions?countryId=1
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<RegionResponse>>), StatusCodes.Status200OK)]

    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]

    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRegions([FromQuery] int countryId, [FromQuery] int page = 1,
      [FromQuery] int pageSize = 10,
     [FromQuery] string? search = null)
    {
        var response = await _regionService.GetRegionsAsync(countryId, search, page, pageSize);

        return response.Status ? Ok(response) : BadRequest(response);
    }

    // GET: api/regions/1
    [HttpGet("{regionId:int}")]
    [ProducesResponseType(typeof(ApiResponseWithoutPagination<RegionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRegionById(int regionId)
    {
        var response = await _regionService.GetRegionByIdAsync(regionId);

        //if (!response.Status)
        //    return regionId <= 0 ? BadRequest(response) : BadRequest(response);

        return response.Status ? Ok(response) : BadRequest(response);
    }


    //[HttpGet("/country")]
    //public async Task<IActionResult> GetRegionsByCountryId(int countryId)
    //{
    //    var response = await _regionService.GetRegionsByCoutryId(countryId);

    //    if (!response.Status)
    //        return countryId <= 0 ? BadRequest(response) : NotFound(response);

    //    return Ok(response);
    //}

    // POST: api/regions
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateRegion([FromBody] RegionRequest request)
    {
        var response = await _regionService.CreateRegionAsync(request);

        return response.Status ? Ok(response) : BadRequest(response);
    }

    // PUT: api/regions/1
    [HttpPut("{regionId:int}")]
    [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateRegion(
        int regionId,
        [FromBody] RegionRequest request)
    {
        var response = await _regionService.UpdateRegionAsync(regionId, request);
        return response.Status ? Ok(response) : BadRequest(response);

        //if (!response.Status)
        //    return regionId <= 0 ? BadRequest(response) : NotFound(response);

        //return Ok(response);
    }
}