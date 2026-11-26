using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Product;
using QuickBalance.API.DTOs.Respons_DTO;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Services;

using System.Text.Json;
using static QuickBalance.API.DTOs.ProductRequest;

namespace QuickBalance.API.Controllers.Master;

[ApiController]
[Route("api/v1/products")]
[Tags("17. Products")]
[Authorize]
public sealed class ProductController : ControllerBase
{
    private readonly IProductInterface _service;
    private readonly ILogger<ProductController> _logger;
    private readonly ICurrentUser _currentUser;

    public ProductController(
        IProductInterface service,
        ILogger<ProductController> logger,
        ICurrentUser currentUser)
    {
        _service = service;
        _logger = logger;
        _currentUser = currentUser;
    }


    private static readonly JsonSerializerOptions JsonOptions =
    new(JsonSerializerDefaults.Web);   // camelCase, case-insensitive

    // Converts the "product" JSON text and runs the field rules.
    private (T? Request, string? Error) ReadProduct<T>(string json) where T : class
    {
        T? request;
        try
        {
            request = JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return (null, "Invalid product data.");
        }

        if (request is null)
            return (null, "Invalid product data.");

        // [Required]/[Range]/[StringLength] don't run automatically on JSON text
        if (!TryValidateModel(request))
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);
            return (null, string.Join(" ", errors));
        }

        return (request, null);
    }

    // ============================================================
    // CREATE PRODUCT
    // POST: api/v1/products
    // ============================================================

    //[HttpPost]
    //[ProducesResponseType(
    //    typeof(ApiResponseWithSuccess),
    //    StatusCodes.Status200OK)]
    //[ProducesResponseType(
    //    typeof(ApiResponseWithError),
    //    StatusCodes.Status400BadRequest)]
    //[ProducesResponseType(
    //    typeof(ApiResponseWithError),
    //    StatusCodes.Status401Unauthorized)]
    //[ProducesResponseType(
    //    typeof(ApiResponseWithError),
    //    StatusCodes.Status500InternalServerError)]
    //public async Task<IActionResult> Create(
    //    [FromBody] ProductRequests request)
    //{
    //    var result = await _service.CreateAsync(
    //        request,
    //        _currentUser.UserId);

    //    return result.Status
    //        ? Ok(result)
    //        : BadRequest(result);
    //}

    // ============================================================
    // GET ALL PRODUCTS - PAGINATED
    // GET: api/v1/products?page=1&pageSize=20&search=abc
    // ============================================================

    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResponse<ProductResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponseWithError),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponseWithError),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponseWithError),
        StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationRequests request, int? groupId,
        bool? active)
    {
        var result = await _service.GetAllAsync(
            request.Page,
            request.PageSize,
            request.Search,
            groupId,
            active
            );

        return result.Status ? Ok(result) : BadRequest(result);
    }

    // ============================================================
    // GET PRODUCT BY ID
    // GET: api/v1/products/10
    // ============================================================

    [HttpGet("{id:int}")]
    [ProducesResponseType(
        typeof(ApiResponseWithoutPagination<ProductResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponseWithError),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponseWithError),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponseWithError),
        StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);

        return result.Status ? Ok(result) : BadRequest(result);

    }

    // ============================================================
    // UPDATE PRODUCT
    // PUT: api/v1/products/10
    // ============================================================

    [HttpPut("{id:int}")]
    [ProducesResponseType(
        typeof(ApiResponseWithSuccess),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponseWithError),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponseWithError),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponseWithError),
        StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ProductUpdateRequests request)
    {
        var result = await _service.UpdateAsync(
            id,
            request,
            _currentUser.UserId);

        return result.Status ? Ok(result) : BadRequest(result);

    }

    // ============================================================
    // DELETE PRODUCT
    // DELETE: api/v1/products/10
    // ============================================================

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseWithSuccess),StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponseWithError),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponseWithError),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponseWithError),
        StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(
            id,
            _currentUser.UserId);

        return result.Status ? Ok(result) : BadRequest(result);

    }

    // ============================================================
    // ADD PRODUCT IMAGE
    // POST: api/v1/products/image
    // ============================================================

    [HttpPost("image")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(
        typeof(ApiResponseWithSuccess),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponseWithError),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponseWithError),
        StatusCodes.Status401Unauthorized)]

    [ProducesResponseType(
        typeof(ApiResponseWithError),
        StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddImage(
        [FromForm] ProductImageRequest request)
    {
        var result = await _service.AddImageAsync(
            request,
            _currentUser.UserId);

        return result.Status ? Ok(result) : BadRequest(result);

    }

    // ============================================================
    // UPDATE PRODUCT IMAGE
    // PUT: api/v1/products/image
    // ============================================================

    [HttpPut("image")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponseWithSuccess),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateImage(
        [FromForm] ProductImageRequest request)
    {
        var result = await _service.UpdateImageAsync(
            request,
            _currentUser.UserId);

        if (!result.Status)
            return BadRequest(result);

        return Ok(result);
    }

    // ============================================================
    // GET PRODUCT IMAGE
    // GET: api/v1/products/10/image
    // ============================================================

    [HttpGet("{id:int}/image")]
    //[Produces("image/jpeg", "image/png", "image/webp")]
    [ProducesResponseType(typeof(FileContentResult),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetImage(int id)
    {
        var result = await _service.GetImageAsync(id);

        if (!result.Status || result.Data == null)
            return BadRequest(result);

        return File(
            result.Data.ImageBytes,
            result.Data.ContentType);
  }
    // ============================================================
    // CREATE PRODUCT WITH IMAGE
    // POST: api/v1/products/with-image
    // ============================================================

    [HttpPost("with-image")]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = 5 * 1024 * 1024)]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateWithImage(
        [FromForm] ProductFormRequest form)
    {
        var (request, error) = ReadProduct<ProductRequests>(form.Product);
        if (request is null)
            return BadRequest(ApiResponse<int>.Failure(error!));

        var result = await _service.CreateAsync(
            request,
            _currentUser.UserId,
            form.ImageFile);

        return result.Status ? Ok(result) : BadRequest(result);
    }

    // ============================================================
    // UPDATE PRODUCT WITH IMAGE
    // PUT: api/v1/products/10/with-image
    // ============================================================

    [HttpPut("{id:int}/with-image")]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = 5 * 1024 * 1024)]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateWithImage(
        int id,
        [FromForm] ProductFormRequest form)
    {
        var (request, error) = ReadProduct<ProductUpdateRequests>(form.Product);
        if (request is null)
            return BadRequest(ApiResponse<bool>.Failure(error!));

        var result = await _service.UpdateAsync(
            id,
            request,
            _currentUser.UserId,
            form.ImageFile);

        return result.Status ? Ok(result) : BadRequest(result);
    }

    // ============================================================
    // DELETE PRODUCT IMAGE
    // DELETE: api/v1/products/10/image
    // ============================================================

    [HttpDelete("{id:int}/image")]
    [ProducesResponseType(
        typeof(ApiResponseWithSuccess),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponseWithError),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponseWithError),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponseWithError),
        StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteImage(int id)
    {
        var result = await _service.DeleteImageAsync(
            id,
            _currentUser.UserId);

        if (!result.Status)
            return BadRequest(result);

        return Ok(result);
    }
}



//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using QuickBalance.API.DTOs;
//using QuickBalance.API.Interfaces;
//using QuickBalance.API.Services;

//namespace QuickBalance.API.Controllers.Master;

//[ApiController]
////[ApiExplorerSettings(IgnoreApi = true)]

//[Route("api/v1/products")]
//[Tags("16.Products")]
//[Authorize]
//public sealed class ProductController : ControllerBase
//{
//    private readonly IProductInterface _service;
//    private readonly ILogger<ProductController> _logger;
//    private readonly ICurrentUser _currentUser;

//    public ProductController(
//        IProductInterface service,
//        ILogger<ProductController> logger,
//        ICurrentUser currentUser)
//    {
//        _service = service;
//        _logger = logger;
//        _currentUser = currentUser;
//    }

//    [HttpPost]
//    public async Task<IActionResult> Create([FromBody] ProductRequests request)
//        //[FromBody] ProductRequests request)
//    {
//        var result = await _service.CreateAsync(
//            request,
//            _currentUser.UserId);

//        return result.Status ? Ok(result) : BadRequest(result);
//    }

//    [HttpGet]
//    public async Task<IActionResult> GetAll(
//        [FromQuery] PaginationRequests request)
//    {
//        var result = await _service.GetAllAsync(
//            request.Page,
//            request.PageSize,
//            request.Search);

//        return Ok(result);
//    }

//    [HttpGet("{id:int}")]
//    public async Task<IActionResult> GetById(int id)
//    {
//        var result = await _service.GetByIdAsync(id);

//        if (!result.Status)
//        {
//            return result.Message.Contains(
//                "not found",
//                StringComparison.OrdinalIgnoreCase)
//                ? NotFound(result)
//                : BadRequest(result);
//        }

//        return Ok(result);
//    }

//    [HttpPut("{id:int}")]
//    public async Task<IActionResult> Update(
//        int id,
//        [FromBody] ProductRequests request)
//    {
//        var result = await _service.UpdateAsync(
//            id,
//            request,
//            _currentUser.UserId);

//        if (!result.Status)
//        {
//            return result.Message.Contains(
//                "not found",
//                StringComparison.OrdinalIgnoreCase)
//                ? NotFound(result)
//                : BadRequest(result);
//        }

//        return Ok(result);
//    }

//    [HttpDelete("{id:int}")]
//    public async Task<IActionResult> Delete(int id)
//    {
//        var result = await _service.DeleteAsync(
//            id,
//            _currentUser.UserId);

//        if (!result.Status)
//        {
//            return result.Message.Contains(
//                "not found",
//                StringComparison.OrdinalIgnoreCase)
//                ? NotFound(result)
//                : BadRequest(result);
//        }

//        return Ok(result);
//    }





//    // ============================================================
//    // ADD PRODUCT IMAGE
//    // POST: api/v1/products/image
//    // ============================================================

//    [HttpPost("image")]
//    [Consumes("multipart/form-data")]
//    public async Task<IActionResult> AddImage(
//        [FromForm] ProductImageRequest request)
//    {
//        var result = await _service.AddImageAsync(
//            request,
//            _currentUser.UserId);

//        if (!result.Status)
//            return BadRequest(result);

//        return Ok(result);
//    }


//    // ============================================================
//    // UPDATE PRODUCT IMAGE
//    // PUT: api/v1/products/image
//    // ============================================================

//    [HttpPut("image")]
//    [Consumes("multipart/form-data")]
//    public async Task<IActionResult> UpdateImage(
//        [FromForm] ProductImageRequest request)
//    {
//        var result = await _service.UpdateImageAsync(
//            request,
//            _currentUser.UserId);

//        if (!result.Status)
//            return BadRequest(result);

//        return Ok(result);
//    }


//    // ============================================================
//    // GET PRODUCT IMAGE
//    // GET: api/v1/products/{id}/image
//    // ============================================================

//    [HttpGet("{id:int}/image")]
//    //[Produces("image/png", "image/jpeg")]
//    public async Task<IActionResult> GetImage(int id)
//    {
//        var result = await _service.GetImageAsync(id);

//        if (!result.Status || result.Data == null)
//            return NotFound(result);

//        return File(
//            result.Data.ImageBytes,
//            result.Data.ContentType);
//    }


//    // ============================================================
//    // DELETE PRODUCT IMAGE
//    // DELETE: api/v1/products/{id}/image
//    // ============================================================

//    [HttpDelete("{id:int}/image")]
//    public async Task<IActionResult> DeleteImage(int id)
//    {
//        var result = await _service.DeleteImageAsync(
//            id,
//            _currentUser.UserId);

//        if (!result.Status)
//            return BadRequest(result);

//        return Ok(result);
//    }

    //[HttpGet("{id:int}/image")]
    //[Produces("image/png", "image/jpeg")]
    //[ProducesResponseType(StatusCodes.Status200OK)]
    //[ProducesResponseType(StatusCodes.Status404NotFound)]
    //public async Task<IActionResult> GetImage(int id)
    //{
    //    var result = await _service.GetImageAsync(id);

    //    if (!result.Status || result.Data == null)
    //    {
    //        return NotFound(result.Message);
    //    }

    //    return File(
    //        result.Data.ImageBytes,
    //        result.Data.ContentType
    //    );
    //}

    //[HttpGet("{id:int}/image")]
    //public async Task<IActionResult> GetImage(int id)
    //{
    //    var result = await _service.GetImageAsync(id);

    //    if (!result.Status || result.Data is null)
    //    {
    //        return result.Message.Contains(
    //            "not found",
    //            StringComparison.OrdinalIgnoreCase)
    //            ? NotFound(result)
    //            : BadRequest(result);
    //    }

    //    return File(
    //        result.Data.ImageBytes,
    //        result.Data.ContentType);
    //}

    //[HttpGet("{id:int}/thumbnail")]
    //public async Task<IActionResult> GetThumbnail(int id)
    //{
    //    var result = await _service.GetThumbnailAsync(id);

    //    if (!result.Status || result.Data is null)
    //    {
    //        return result.Message.Contains(
    //            "not found",
    //            StringComparison.OrdinalIgnoreCase)
    //            ? NotFound(result)
    //            : BadRequest(result);
    //    }

    //    return File(
    //        result.Data.ImageBytes,
    //        result.Data.ContentType);
    //}


//namespace QuickBalance.API.Controllers.Master
//{
//    using global::QuickBalance.API.DTOs;
//    using global::QuickBalance.API.Interfaces;
//    using global::QuickBalance.API.Services;
//    using Microsoft.AspNetCore.Authorization;
//    using Microsoft.AspNetCore.Mvc;
//    [ApiController]
//    [Route("api/v1/products")]
//    //[ApiExplorerSettings(IgnoreApi = true)]

//    [Tags("07.Products")]
//    [Authorize]
//    public sealed class ProductController : ControllerBase
//    {
//        private readonly IProductInterface _service;
//        private readonly ILogger<ProductController> _logger;
//        private readonly ICurrentUser _currentUser;

//        public ProductController(IProductInterface service,ILogger<ProductController> logger,ICurrentUser currentUser)
//        {
//            _service = service;
//            _logger = logger;
//            _currentUser = currentUser;
//        }

//        // CREATE
//        [HttpPost]
//        public async Task<IActionResult> Create(
//            [FromBody] ProductRequests request)
//        {
//            var result = await _service.CreateAsync(
//                request,
//                _currentUser.UserId);

//            return result.Status
//                ? Ok(result)
//                : BadRequest(result);
//        }

//        // GET ALL
//        [HttpGet]
//        public async Task<IActionResult> GetAll(
//            [FromQuery] PaginationRequests request)
//        {
//            var result = await _service.GetAllAsync(
//                request.Page,
//                request.PageSize,
//                request.Search);

//            return Ok(result);
//        }

//        // GET BY ID
//        [HttpGet("{id:int}")]
//        public async Task<IActionResult> GetById(int id)
//        {
//            var result = await _service.GetByIdAsync(id);

//            if (!result.Status)
//            {
//                if (result.Message.Contains(
//                    "not found",
//                    StringComparison.OrdinalIgnoreCase))
//                {
//                    return NotFound(result);
//                }

//                return BadRequest(result);
//            }

//            return Ok(result);
//        }

//        // UPDATE
//        [HttpPut("{id:int}")]
//        public async Task<IActionResult> Update(
//            int id,
//            [FromBody] ProductRequests request)
//        {
//            var result = await _service.UpdateAsync(
//                id,
//                request,
//                _currentUser.UserId);

//            if (!result.Status)
//            {
//                if (result.Message.Contains(
//                    "not found",
//                    StringComparison.OrdinalIgnoreCase))
//                {
//                    return NotFound(result);
//                }

//                return BadRequest(result);
//            }

//            return Ok(result);
//        }

//        // DELETE
//        [HttpDelete("{id:int}")]
//        public async Task<IActionResult> Delete(int id)
//        {
//            var result = await _service.DeleteAsync(id);

//            if (!result.Status)
//            {
//                if (result.Message.Contains(
//                    "not found",
//                    StringComparison.OrdinalIgnoreCase))
//                {
//                    return NotFound(result);
//                }

//                return BadRequest(result);
//            }

//            return Ok(result);
//        }

//        // PRODUCT IMAGE
//        [HttpGet("{id:int}/image")]
//        public async Task<IActionResult> GetImage(int id)
//        {
//            var result = await _service.GetImageAsync(id);

//            if (!result.Status || result.Data is null)
//            {
//                if (result.Message.Contains(
//                    "not found",
//                    StringComparison.OrdinalIgnoreCase))
//                {
//                    return NotFound(result);
//                }

//                return BadRequest(result);
//            }

//            return File(
//                result.Data.ImageBytes,
//                result.Data.ContentType);
//        }

//        // PRODUCT THUMBNAIL
//        [HttpGet("{id:int}/thumbnail")]
//        public async Task<IActionResult> GetThumbnail(int id)
//        {
//            var result = await _service.GetThumbnailAsync(id);

//            if (!result.Status || result.Data is null)
//            {
//                if (result.Message.Contains(
//                    "not found",
//                    StringComparison.OrdinalIgnoreCase))
//                {
//                    return NotFound(result);
//                }

//                return BadRequest(result);
//            }

//            return File(
//                result.Data.ImageBytes,
//                result.Data.ContentType);
//        }
//    }
//}
