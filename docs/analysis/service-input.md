# Service input files

| # | Path | Lines |
|---|------|-------|
| 1 | QuickBalance.API/CLS/ApiResponse.cs | 143 |
| 2 | QuickBalance.API/Services/BrandService.cs | 232 |
| 3 | QuickBalance.API/Controllers/Master/BrandController.cs | 112 |
| 4 | QuickBalance.API/Interfaces/IBrandInterface.cs | 22 |
| 5 | QuickBalance.API/Services/CurrentUser.cs (defines ICurrentUser) | 77 |

## QuickBalance.API/CLS/ApiResponse.cs

````csharp
using System.Text.Json.Serialization;

namespace QuickBalance.API.CLS
{
    // ============================================================
    // STANDARD RESPONSE (WITH OPTIONAL PAGINATION)
    // ============================================================

    public sealed class ApiResponse<T>
    {
        public bool Status { get; set; }

        public string Message { get; set; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public T? Data { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PaginationInfo? Pagination { get; set; }

        public static ApiResponse<T> Success(
            string message,
            T? data = default)
        {
            return new ApiResponse<T>
            {
                Status = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> SuccessWithPagination(
            string message,
            T? data,
            PaginationInfo pagination)
        {
            return new ApiResponse<T>
            {
                Status = true,
                Message = message,
                Data = data,
                Pagination = pagination
            };
        }

        public static ApiResponse<T> Failure(string message)
        {
            return new ApiResponse<T>
            {
                Status = false,
                Message = message
            };
        }
    }

    // ============================================================
    // STANDARD RESPONSE (WITHOUT PAGINATION)
    // ============================================================

    public sealed class ApiResponseWithoutPagination<T>
    {
        public bool Status { get; set; }

        public string Message { get; set; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public T? Data { get; set; }

        public static ApiResponseWithoutPagination<T> Success(
            string message,
            T? data = default)
        {
            return new ApiResponseWithoutPagination<T>
            {
                Status = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponseWithoutPagination<T> Failure(
            string message)
        {
            return new ApiResponseWithoutPagination<T>
            {
                Status = false,
                Message = message
            };
        }
    }

    // ============================================================
    // ERROR RESPONSE (NO DATA)
    // ============================================================

    public sealed class ApiResponseWithError
    {
        public bool Status { get; set; } = false;

        public string Message { get; set; } = string.Empty;

        public static ApiResponseWithError Failure(string message)
        {
            return new ApiResponseWithError
            {
                Status = false,
                Message = message
            };
        }
    }


    public sealed class ApiResponseWithSuccess
    {
        public bool Status { get; set; } = true;

        public string Message { get; set; } = string.Empty;

        public static ApiResponseWithError Success(string message)
        {
            return new ApiResponseWithError
            {
                Status = true,
                Message = message
            };
        }
    }
    // ============================================================
    // PAGINATION
    // ============================================================

    public sealed class PaginationInfo
    {
        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalRecords { get; set; }

        public int TotalPages { get; set; }
    }
}
````

## QuickBalance.API/Services/BrandService.cs

````csharp
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Brand;
using QuickBalance.API.DTOs.Respons_DTO;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Repository.Master;
using QuickBalance.API.Repository.Master.QuickBalance.API.Repository.Master;

namespace QuickBalance.API.Services;

public sealed class BrandService : IBrandInterface
{
    private readonly BrandDB _db;
    private readonly ILogger<BrandService> _logger;

    public BrandService(
        BrandDB db,
        ILogger<BrandService> logger)
    {
        _db = db;
        _logger = logger;
    }

    private static void Validate(BrandRequest request)
    {
        if (request == null)
            throw new ArgumentException("Invalid brand request.");

        if (string.IsNullOrWhiteSpace(request.BrandName))
            throw new ArgumentException("Brand name is required.");
    }

    private static void Normalize(BrandRequest request)
    {
        request.BrandName = request.BrandName.Trim();
        request.SecondName = request.SecondName?.Trim();
        request.Alias = request.Alias?.Trim();
    }

    private async Task<string?> GetDuplicateMessageAsync(
        BrandRequest request, int? excludeId = null)
    {
        if (await _db.NameExistsAsync(request.BrandName, excludeId))
            return "Brand name already exists.";

        if (!string.IsNullOrWhiteSpace(request.SecondName) &&
            await _db.SecondNameExistsAsync(request.SecondName, excludeId))
            return "Second name already exists.";

        if (!string.IsNullOrWhiteSpace(request.Alias) &&
            await _db.AliasExistsAsync(request.Alias, excludeId))
            return "Alias already exists.";

        return null;
    }

    public async Task<ApiResponse<int>> CreateAsync(BrandRequest request)
    {
        try
        {
            Validate(request);
            Normalize(request);

            var duplicateMessage = await GetDuplicateMessageAsync(request);

            if (duplicateMessage != null)
                return ApiResponse<int>.Failure(duplicateMessage);

            var id = await _db.InsertAsync(request);

            if (id <= 0)
                return ApiResponse<int>.Failure(
                    "Brand could not be created.");

            return ApiResponse<int>.Success(
                "Brand created successfully.");
        }
        catch (ArgumentException ex)
        {
            return ApiResponse<int>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Brand.");

            return ApiResponse<int>.Failure(
                "An error occurred while creating the Brand.");
        }
    }

    public async Task<ApiResponse<BrandResponse>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return ApiResponse<BrandResponse>.Failure(
                    "Invalid Brand ID.");

            var result = await _db.GetByIdAsync(id);

            if (result is null)
                return ApiResponse<BrandResponse>.Failure(
                    "Brand not found.");

            return ApiResponse<BrandResponse>.Success(
                "Brand retrieved successfully.",result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Brand {Id}.", id);

            return ApiResponse<BrandResponse>.Failure(
                "Unable to retrieve Brand.");
        }
    }

    public async Task<ApiResponse<IReadOnlyList<BrandResponse>>> GetAllAsync(
        int page, int pageSize, string? search)
    {
        try
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var result = await _db.GetAllAsync(page, pageSize, search);

            return ApiResponse<IReadOnlyList<BrandResponse>>.SuccessWithPagination(
                "Brands retrieved successfully.",
                result.Data,
                result.Pagination);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Brands.");

            return ApiResponse<IReadOnlyList<BrandResponse>>.Failure(
                "Unable to retrieve Brands.");
        }
    }

    public async Task<ApiResponse<bool>> UpdateAsync(
        int id, BrandRequest request)
    {
        try
        {
            if (id <= 0)
                return ApiResponse<bool>.Failure("Invalid Brand ID.");

            Validate(request);
            Normalize(request);

            var existing = await _db.GetByIdAsync(id);

            if (existing is null)
                return ApiResponse<bool>.Failure("Brand not found.");

            var duplicateMessage =
                await GetDuplicateMessageAsync(request, id);

            if (duplicateMessage != null)
                return ApiResponse<bool>.Failure(duplicateMessage);

            var rows = await _db.UpdateAsync(id, request);

            if (rows == 0)
                return ApiResponse<bool>.Failure(
                    "Brand was not updated.");

            return ApiResponse<bool>.Success(
                "Brand updated successfully.");
        }
        catch (ArgumentException ex)
        {
            return ApiResponse<bool>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Brand {Id}.", id);

            return ApiResponse<bool>.Failure(
                "An error occurred while updating the Brand.");
        }
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, int userId)
    {
        try
        {
            if (id <= 0)
                return ApiResponse<bool>.Failure("Invalid Brand ID.");

            var existing = await _db.GetByIdAsync(id);

            if (existing is null)
                return ApiResponse<bool>.Failure("Brand not found.");

            var rows = await _db.DeleteAsync(id, userId);

            if (rows == 0)
                return ApiResponse<bool>.Failure(
                    "Brand not found.");

            return ApiResponse<bool>.Success(
                "Brand deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Brand {Id}.", id);

            return ApiResponse<bool>.Failure(
                "Unable to delete Brand.");
        }
    }

    public async Task<ApiResponse<IReadOnlyList<BrandResponse>>> GetActiveAsync()
    {
        try
        {
            var result = await _db.GetActiveAsync();

            return ApiResponse<IReadOnlyList<BrandResponse>>.Success(
                "Brands retrieved successfully.", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Brands.");

            return ApiResponse<IReadOnlyList<BrandResponse>>.Failure(
                "Unable to retrieve Brands.");
        }
    }
}
````

## QuickBalance.API/Controllers/Master/BrandController.cs

````csharp
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
        [ProducesResponseType(typeof(ApiResponse<BrandResponse>), StatusCodes.Status200OK)]
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
````

## QuickBalance.API/Interfaces/IBrandInterface.cs

````csharp
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Brand;

namespace QuickBalance.API.Interfaces
{
    public interface IBrandInterface
    {
        Task<ApiResponse<int>> CreateAsync(BrandRequest request);

        Task<ApiResponse<BrandResponse>> GetByIdAsync(int id);

        Task<ApiResponse<IReadOnlyList<BrandResponse>>> GetAllAsync(
            int page, int pageSize, string? search);

        Task<ApiResponse<bool>> UpdateAsync(int id, BrandRequest request);

        Task<ApiResponse<bool>> DeleteAsync(int id, int userId);

        Task<ApiResponse<IReadOnlyList<BrandResponse>>> GetActiveAsync();
    }
}
````

## QuickBalance.API/Services/CurrentUser.cs

````csharp
using System.Security.Claims;

namespace QuickBalance.API.Services
{
    public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
    {
        private ClaimsPrincipal? Principal =>
        accessor.HttpContext?.User;

        public bool IsAuthenticated =>
            Principal?.Identity?.IsAuthenticated == true;

        public int UserId =>
            GetRequiredInt(ClaimTypes.NameIdentifier);

        public string? UserName =>
            GetClaim(ClaimTypes.Name) ?? GetClaim("name");

        public string? TenantId =>
            GetClaim("tenant_id");

        public int? CompanyId =>
            GetOptionalInt("company_id");

        public int? BranchId =>
            GetOptionalInt("branch_id");

        public string? Role =>
            GetClaim(ClaimTypes.Role) ?? GetClaim("role");

        private string? GetClaim(string claimType) =>
            Principal?.FindFirst(claimType)?.Value;

        private int GetRequiredInt(string claimType)
        {
            if (!IsAuthenticated ||
                !int.TryParse(GetClaim(claimType), out int value) ||
                value <= 0)
            {
                throw new UnauthorizedAccessException(
                    $"Required claim '{claimType}' is missing or invalid.");
            }

            return value;
        }

        private int? GetOptionalInt(string claimType)
        {
            var value = GetClaim(claimType);

            if (string.IsNullOrWhiteSpace(value))
                return null;

            return int.TryParse(value, out int result)
                ? result
                : throw new UnauthorizedAccessException(
                    $"Claim '{claimType}' is invalid.");
        }
    }

    public interface ICurrentUser
    {
        bool IsAuthenticated { get; }

        int UserId { get; }

        string? UserName { get; }

        string? TenantId { get; }

        int? CompanyId { get; }

        int? BranchId { get; }

        string? Role { get; }
    }
}
````
