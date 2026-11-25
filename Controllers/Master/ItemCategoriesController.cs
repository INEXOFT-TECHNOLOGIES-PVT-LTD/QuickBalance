
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.ItemCategory;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Services;

namespace QuickBalance.API.Controllers.Master;

[ApiController]
[Route("api/v1/item-categories")]
[Tags("12.Item Categories")]
[Authorize]
public sealed class ItemCategoriesController : ControllerBase
{
    private readonly IItemCategoryInterface _service;
    private readonly ILogger<ItemCategoriesController> _logger;
    private readonly ICurrentUser _currentUser;

    public ItemCategoriesController(
        IItemCategoryInterface service,
        ILogger<ItemCategoriesController> logger,
        ICurrentUser currentUser)
    {
        _service = service;
        _logger = logger;
        _currentUser = currentUser;
    }

    // CREATE
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] ItemCategoryRequest request)
    {
        var result = await _service.CreateAsync(
            request, _currentUser.UserId);

        return result.Status
            ? Ok(result)
            : BadRequest(result);
    }

    // GET ALL
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ItemCategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationRequests request,
        [FromQuery] bool? active = null)
    {
        var result = await _service.GetAllAsync(
            request.Page,
            request.PageSize,
            request.Search,
            active);

        return result.Status
                    ? Ok(result)
                    : BadRequest(result);
    }

    // GET ACTIVE
    //[HttpGet("active")]
    //[ProducesResponseType(typeof(ApiResponseWithouPagination<ItemCategoryResponse>), StatusCodes.Status200OK)]
    //[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    //[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    //[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    //public async Task<IActionResult> GetActive()
    //{
    //    var result = await _service.GetActiveAsync();

    //    return result.Status
    //        ? Ok(result)
    //        : BadRequest(result);
    //}

    // GET BY ID
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseWithoutPagination<ItemCategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result.Status
                    ? Ok(result)
                    : BadRequest(result);
    }

    

    // UPDATE
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ItemCategoryRequest request)
    {
        var result = await _service.UpdateAsync(
            id,
            request,
            _currentUser.UserId);

        return result.Status
             ? Ok(result)
             : BadRequest(result);
    }


    // UPDATE
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(
        int id)
    {
        var result = await _service.DeleteAsync(id, _currentUser.UserId);

        return result.Status
             ? Ok(result)
             : BadRequest(result);
    }
}


//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;

//namespace QuickBalance.API.Controllers.Master
//{
//    using global::QuickBalance.API.DTOs;
//    using global::QuickBalance.API.DTOs.ItemCategory;
//    using global::QuickBalance.API.Interfaces;
//    using global::QuickBalance.API.Repository.Master.QuickBalance.API.Repository.Master;
//    using global::QuickBalance.API.Services;
//    using Microsoft.AspNetCore.Authorization;
//    using Microsoft.AspNetCore.Mvc;

//    namespace QuickBalance.API.Controllers.Master
//    {
//        [Tags("08.Item Categories")]
//        [Route("api/item-categories")]
//        [ApiController]
//        [Authorize]
//        public class ItemCategoriesController : ControllerBase
//        {
//            private readonly IItemCategoryInterface _categoryService;
//            private readonly ICurrentUser _currentUser;

//            public ItemCategoriesController(
//                IItemCategoryInterface categoryService,
//                ICurrentUser currentUser)
//            {
//                _categoryService = categoryService;
//                _currentUser = currentUser;
//            }

//            // =========================================================
//            // POST: api/item-categories
//            // =========================================================

//            [HttpPost]
//            public async Task<IActionResult> Create(
//                [FromBody] ItemCategoryRequest request)
//            {
//                try
//                {
//                    if (request == null)
//                    {
//                        return BadRequest(new
//                        {
//                            status = false,
//                            message = "Request data is required."
//                        });
//                    }

//                    if (string.IsNullOrWhiteSpace(request.CategoryName))
//                    {
//                        return BadRequest(new
//                        {
//                            status = false,
//                            message = "Category name must not be blank."
//                        });
//                    }

//                    string categoryName = request.CategoryName.Trim();

//                    if (await _categoryService.NameExistsAsync(categoryName))
//                    {
//                        return BadRequest(new
//                        {
//                            status = false,
//                            message = "Category name already exists."
//                        });
//                    }

//                    int categoryId = await _categoryDB.InsertAsync(
//                        request,
//                        _currentUser.UserId);

//                    if (categoryId <= 0)
//                    {
//                        return BadRequest(new
//                        {
//                            status = false,
//                            message = "Category creation failed."
//                        });
//                    }

//                    //var category = await _categoryDB.GetByIdAsync(categoryId);

//                    return Ok(new
//                    {
//                        status = true,
//                        message = "Category created successfully.",
//                        data = categoryId
//                    });
//                }
//                catch (Exception)
//                {
//                    return StatusCode(500, new
//                    {
//                        status = false,
//                        message = "An error occurred while creating category."
//                    });
//                }
//            }

//            // =========================================================
//            // GET: api/item-categories
//            // =========================================================

//            [HttpGet]
//            public async Task<IActionResult> GetAll(
//                [FromQuery] PaginationRequests pagination)
//            {
//                try
//                {
//                    //pagination ??= new PaginationRequest();
//                    pagination.Normalizes();

//                    var result = await _categoryDB.GetAllAsync(
//                        pagination.Page,
//                        pagination.PageSize,
//                        pagination.Search);

//                    int totalPages = (int)Math.Ceiling(
//                        result.Pagination.TotalRecords / (double)pagination.PageSize);

//                    return Ok(new
//                    {
//                        status = true,
//                        message = "Categories retrieved successfully.",
//                        data = result.Data,
//                        pagination = new
//                        {
//                            page = pagination.Page,
//                            pageSize = pagination.PageSize,
//                            totalRecords = result.Pagination.TotalRecords,
//                            totalPages
//                        }
//                    });
//                }
//                catch (Exception)
//                {
//                    return StatusCode(500, new
//                    {
//                        status = false,
//                        message = "An error occurred while retrieving categories."
//                    });
//                }
//            }

//            // =========================================================
//            // GET: api/item-categories/active
//            // =========================================================

//            [HttpGet("active")]
//            public async Task<IActionResult> GetActive()
//            {
//                try
//                {
//                    var categories = await _categoryDB.GetActiveAsync();

//                    return Ok(new
//                    {
//                        status = true,
//                        message = "Active categories retrieved successfully.",
//                        data = categories
//                    });
//                }
//                catch (Exception)
//                {
//                    return StatusCode(500, new
//                    {
//                        status = false,
//                        message = "An error occurred while retrieving active categories."
//                    });
//                }
//            }

//            // =========================================================
//            // GET: api/item-categories/{id}
//            // =========================================================

//            [HttpGet("{id:int}")]
//            public async Task<IActionResult> GetById(int id)
//            {
//                try
//                {
//                    if (id <= 0)
//                    {
//                        return BadRequest(new
//                        {
//                            status = false,
//                            message = "Invalid category ID."
//                        });
//                    }

//                    var category = await _categoryDB.GetByIdAsync(id);

//                    if (category == null)
//                    {
//                        return NotFound(new
//                        {
//                            status = false,
//                            message = "Category not found."
//                        });
//                    }

//                    return Ok(new
//                    {
//                        status = true,
//                        message = "Category retrieved successfully.",
//                        data = category
//                    });
//                }
//                catch (Exception)
//                {
//                    return StatusCode(500, new
//                    {
//                        status = false,
//                        message = "An error occurred while retrieving category."
//                    });
//                }
//            }

//            // =========================================================
//            // PUT: api/item-categories/{id}
//            // =========================================================

//            [HttpPut("{id:int}")]
//            public async Task<IActionResult> Update(
//                int id,
//                [FromBody] ItemCategoryRequest request)
//            {
//                try
//                {
//                    if (id <= 0)
//                    {
//                        return BadRequest(new
//                        {
//                            status = false,
//                            message = "Invalid category ID."
//                        });
//                    }

//                    if (request == null ||
//                        string.IsNullOrWhiteSpace(request.CategoryName))
//                    {
//                        return BadRequest(new
//                        {
//                            status = false,
//                            message = "Category name must not be blank."
//                        });
//                    }

//                    var existing = await _categoryDB.GetByIdAsync(id);

//                    if (existing == null)
//                    {
//                        return NotFound(new
//                        {
//                            status = false,
//                            message = "Category not found."
//                        });
//                    }

//                    string categoryName = request.CategoryName.Trim();

//                    if (await _categoryDB.NameExistsAsync(categoryName, id))
//                    {
//                        return BadRequest(new
//                        {
//                            status = false,
//                            message = "Category name already exists."
//                        });
//                    }

//                    int affectedRows = await _categoryDB.UpdateAsync(id, request);

//                    if (affectedRows <= 0)
//                    {
//                        return BadRequest(new
//                        {
//                            status = false,
//                            message = "Category update failed."
//                        });
//                    }

//                    //var updated = await _categoryDB.GetByIdAsync(id);

//                    return Ok(new
//                    {
//                        status = true,
//                        message = "Category updated successfully.",
//                        //data = existing.Id
//                    });
//                }
//                catch (Exception)
//                {
//                    return StatusCode(500, new
//                    {
//                        status = false,
//                        message = "An error occurred while updating category."
//                    });
//                }
//            }
//        }
//    }
//}
