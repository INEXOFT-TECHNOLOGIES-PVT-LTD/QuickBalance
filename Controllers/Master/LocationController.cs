using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Respons_DTO;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Services;

namespace QuickBalance.API.Controllers
{
    [ApiController]
    [Route("api/v1/locations")]
    [Authorize]
    [Tags("21. Locations / Godown")]
    public class LocationController : ControllerBase
    {
        private readonly ILocationService _service;
        private readonly ICurrentUser _currentUser;

        public LocationController(
            ILocationService service,
            ICurrentUser currentUser)
        {
            _service = service;
            _currentUser = currentUser;
        }

        // =========================================================
        // CREATE
        // POST: api/v1/locations
        // =========================================================

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create(
            [FromBody] LocationMasterRequest request)
        {
            var result = await _service.CreateAsync(
                request,
                _currentUser.UserId);

            return result.Status ? Ok(result) : BadRequest(result);
            //return Ok(result);
        }

        // =========================================================
        // GET ALL - PAGINATED
        // GET: api/v1/locations?page=1&pageSize=20&search=abc&active=true|false (omit for both)
        // =========================================================

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<LocationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] bool? active = null)
        {
            var result = await _service.GetAllAsync(
                page,
                pageSize,
                search,
                active);

            return result.Status ? Ok(result) : BadRequest(result);
        }

        // =========================================================
        // GET BY ID
        // GET: api/v1/locations/10
        // =========================================================

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithoutPagination<LocationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);

            return result.Status ? Ok(result) : BadRequest(result);
        }

        // =========================================================
        // UPDATE
        // PUT: api/v1/locations/10
        // =========================================================

        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] LocationMasterRequest request)
        {
            var result = await _service.UpdateAsync(
                id,
                request,
                _currentUser.UserId);

            return result.Status ? Ok(result) : BadRequest(result);
        }


        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(
       int id)
        {
            var result = await _service.DeleteAsync(
                id, _currentUser.UserId);

            return result.Status ? Ok(result) : BadRequest(result);
        }
    }
}












//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using QuickBalance.API.CLS;
//using QuickBalance.API.Configuration;
//using QuickBalance.API.DB;
//using QuickBalance.API.DTOs;
//using System.Data;
//using System.Security.Claims;

//namespace QuickBalance.API.Controllers.Master
//{
//    [ApiController]
//    [ApiExplorerSettings(IgnoreApi = true)]

//    [Route("api/v1/locations")]
//    [Authorize]
//    public class LocationController : ControllerBase
//    {
//        private readonly GdwMasDB gdwMasDB;

//        public LocationController(GdwMasDB gdwMasDB)
//        {
//            this.gdwMasDB = gdwMasDB;
//        }

//        // =========================================================
//        // DATA TABLE -> JSON LIST
//        // =========================================================

//        private static List<Dictionary<string, object?>> ConvertToList(
//            DataTable table)
//        {
//            var result = new List<Dictionary<string, object?>>();

//            foreach (DataRow row in table.Rows)
//            {
//                var item = new Dictionary<string, object?>();

//                foreach (DataColumn column in table.Columns)
//                {
//                    object value = row[column];

//                    item[column.ColumnName] =
//                        value == DBNull.Value ? null : value;
//                }

//                result.Add(item);
//            }

//            return result;
//        }

//        // =========================================================
//        // CREATE
//        // POST: /api/v1/locations
//        // =========================================================

//        [HttpPost]
//        public async Task<IActionResult> Create(
//            [FromBody] LocationMasterRequest request)
//        {
//            try
//            {
//                if (request == null)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Request data is required."
//                    });
//                }

//                if (string.IsNullOrWhiteSpace(request.LocationName))
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Location name must not be blank."
//                    });
//                }

//                if (request.GroupId <= 0)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Select valid location group."
//                    });
//                }

//                if (request.StockAccountId <= 0)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Select valid stock ledger."
//                    });
//                }

//                // ====================================================
//                // LANGUAGE / VARIABLES
//                // ====================================================

//                LngTB.InitLng();

//                Variable Variables = new Variable();

//                // ====================================================
//                // USER ID FROM JWT
//                // ====================================================

//                var userIdClaim =
//                    User.FindFirst(ClaimTypes.NameIdentifier);

//                if (userIdClaim == null)
//                {
//                    return Unauthorized(new
//                    {
//                        status = "error",
//                        message = "User ID not found in token."
//                    });
//                }

//                Variables.UserId =
//                    int.Parse(userIdClaim.Value);

//                string locationName =
//                    request.LocationName.Trim();

//                // Duplicate check
//                if (await gdwMasDB.NameExists(locationName))
//                {
//                    return Conflict(new
//                    {
//                        status = false,
//                        message = "Location name already exists."
//                    });
//                }

//                // Logged-in user
//                int logCode = 0;

//                string? userId =
//                    User.FindFirstValue(
//                        ClaimTypes.NameIdentifier);

//                if (!string.IsNullOrWhiteSpace(userId))
//                {
//                    int.TryParse(userId, out logCode);
//                }

//                // Insert
//                int locationId =
//                    await gdwMasDB.Insert(
//                        locationName,
//                        request.GroupId,
//                        request.IsActive,
//                        request.StockAccountId,
//                        request.StockPLFlag,
//                        logCode,
//                        request.SecondName?.Trim() ?? ""
//                    );

//                if (locationId <= 0)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Failed to create location."
//                    });
//                }

//                DataTable dt =
//                    await gdwMasDB.GetById(locationId);

//                return Ok(new
//                {
//                    status = true,
//                    message = "Location created successfully.",
//                    data = ConvertToList(dt)
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = "Error while creating location.",
//                    error = ex.Message
//                });
//            }
//        }

//        // =========================================================
//        // GET ALL (PAGINATED)
//        // GET: /api/v1/locations?page=1&pageSize=20&search=abc&sortBy=GDWNAME&sortDirection=ASC
//        // =========================================================

//        [HttpGet]
//        public async Task<IActionResult> GetAll(
//            [FromQuery] PaginationRequest pagination = null!)
//        {
//            try
//            {
//                // ====================================================
//                // PAGINATION
//                // ====================================================

//                pagination ??= new PaginationRequest();

//                pagination.Normalize();

//                // ====================================================
//                // LANGUAGE / VARIABLES
//                // ====================================================

//                LngTB.InitLng();

//                Variable Variables = new Variable();

//                // ====================================================
//                // USER ID FROM JWT
//                // ====================================================

//                var userIdClaim =
//                    User.FindFirst(ClaimTypes.NameIdentifier);

//                if (userIdClaim == null)
//                {
//                    return Unauthorized(new
//                    {
//                        status = "error",
//                        message = "User ID not found in token."
//                    });
//                }

//                Variables.UserId =
//                    int.Parse(userIdClaim.Value);

//                DataTable dt =
//                    await gdwMasDB.GetAll();

//                // ====================================================
//                // SEARCH (ANY COLUMN, CASE-INSENSITIVE)
//                // ====================================================

//                IEnumerable<DataRow> rows =
//                    dt.AsEnumerable();

//                if (!string.IsNullOrWhiteSpace(pagination.Search))
//                {
//                    string search =
//                        pagination.Search.Trim();

//                    rows = rows.Where(row =>
//                        dt.Columns
//                          .Cast<DataColumn>()
//                          .Any(column =>
//                              row[column] != DBNull.Value &&
//                              (Convert.ToString(row[column]) ?? "")
//                                  .Contains(
//                                      search,
//                                      StringComparison.OrdinalIgnoreCase
//                                  )
//                          )
//                    );
//                }

//                // ====================================================
//                // SORTING
//                // Only columns that really exist in the result set
//                // are accepted; anything else is ignored.
//                // ====================================================

//                if (!string.IsNullOrWhiteSpace(pagination.SortBy))
//                {
//                    string sortBy =
//                        pagination.SortBy.Trim();

//                    DataColumn? sortColumn =
//                        dt.Columns
//                          .Cast<DataColumn>()
//                          .FirstOrDefault(column =>
//                              string.Equals(
//                                  column.ColumnName,
//                                  sortBy,
//                                  StringComparison.OrdinalIgnoreCase
//                              )
//                          );

//                    if (sortColumn != null)
//                    {
//                        var comparer =
//                            Comparer<object?>.Create((a, b) =>
//                            {
//                                if (a == null && b == null)
//                                {
//                                    return 0;
//                                }

//                                if (a == null)
//                                {
//                                    return -1;
//                                }

//                                if (b == null)
//                                {
//                                    return 1;
//                                }

//                                if (a is IComparable comparable &&
//                                    a.GetType() == b.GetType())
//                                {
//                                    return comparable.CompareTo(b);
//                                }

//                                return string.Compare(
//                                    Convert.ToString(a),
//                                    Convert.ToString(b),
//                                    StringComparison.OrdinalIgnoreCase
//                                );
//                            });

//                        if (pagination.SortDirection == "ASC")
//                        {
//                            rows = rows.OrderBy<DataRow, object?>(
//                                row =>
//                                    row[sortColumn] == DBNull.Value
//                                        ? null
//                                        : row[sortColumn],
//                                comparer
//                            );
//                        }
//                        else
//                        {
//                            rows = rows.OrderByDescending<DataRow, object?>(
//                                row =>
//                                    row[sortColumn] == DBNull.Value
//                                        ? null
//                                        : row[sortColumn],
//                                comparer
//                            );
//                        }
//                    }
//                }

//                // ====================================================
//                // TOTALS
//                // ====================================================

//                var filteredRows =
//                    rows.ToList();

//                int totalRecords =
//                    filteredRows.Count;

//                int totalPages =
//                    (int)Math.Ceiling(
//                        totalRecords / (double)pagination.PageSize
//                    );

//                // ====================================================
//                // CONVERT DATATABLE (CURRENT PAGE ONLY)
//                // ====================================================

//                var tableData =
//                    filteredRows
//                      .Skip(pagination.Offset)
//                      .Take(pagination.PageSize)
//                      .Select(row =>
//                          dt.Columns
//                            .Cast<DataColumn>()
//                            .ToDictionary(
//                                column => column.ColumnName,
//                                column =>
//                                    row[column] == DBNull.Value
//                                        ? null
//                                        : row[column]
//                            )
//                      )
//                      .ToList();

//                return Ok(new
//                {
//                    status = true,
//                    message = "Locations retrieved successfully.",
//                    data = tableData,
//                    page = pagination.Page,
//                    pageSize = pagination.PageSize,
//                    totalRecords = totalRecords,
//                    totalPages = totalPages
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = "Error while retrieving locations.",
//                    error = ex.Message
//                });
//            }
//        }

//        // =========================================================
//        // GET BY ID
//        // GET: /api/v1/locations/{id}
//        // =========================================================

//        [HttpGet("{id:int}")]
//        public async Task<IActionResult> GetById(int id)
//        {
//            try
//            {
//                if (id <= 0)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Invalid location ID."
//                    });
//                }

//                // ====================================================
//                // LANGUAGE / VARIABLES
//                // ====================================================

//                LngTB.InitLng();

//                Variable Variables = new Variable();

//                // ====================================================
//                // USER ID FROM JWT
//                // ====================================================

//                var userIdClaim =
//                    User.FindFirst(ClaimTypes.NameIdentifier);

//                if (userIdClaim == null)
//                {
//                    return Unauthorized(new
//                    {
//                        status = "error",
//                        message = "User ID not found in token."
//                    });
//                }

//                Variables.UserId =
//                    int.Parse(userIdClaim.Value);

//                DataTable dt =
//                    await gdwMasDB.GetById(id);

//                if (dt.Rows.Count == 0)
//                {
//                    return NotFound(new
//                    {
//                        status = false,
//                        message = "Location not found."
//                    });
//                }

//                return Ok(new
//                {
//                    status = true,
//                    message = "Location retrieved successfully.",
//                    data = ConvertToList(dt)
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = "Error while retrieving location.",
//                    error = ex.Message
//                });
//            }
//        }

//        // =========================================================
//        // UPDATE
//        // PUT: /api/v1/locations/{id}
//        // =========================================================

//        [HttpPut("{id:int}")]
//        public async Task<IActionResult> Update(
//            int id,
//            [FromBody] LocationMasterRequest request)
//        {
//            try
//            {
//                if (id <= 0)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Invalid location ID."
//                    });
//                }

//                if (request == null)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Request data is required."
//                    });
//                }

//                if (string.IsNullOrWhiteSpace(request.LocationName))
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Location name must not be blank."
//                    });
//                }

//                if (request.GroupId <= 0)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Select valid location group."
//                    });
//                }

//                if (request.StockAccountId <= 0)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Select valid stock ledger."
//                    });
//                }

//                // ====================================================
//                // LANGUAGE / VARIABLES
//                // ====================================================

//                LngTB.InitLng();

//                Variable Variables = new Variable();

//                // ====================================================
//                // USER ID FROM JWT
//                // ====================================================

//                var userIdClaim =
//                    User.FindFirst(ClaimTypes.NameIdentifier);

//                if (userIdClaim == null)
//                {
//                    return Unauthorized(new
//                    {
//                        status = "error",
//                        message = "User ID not found in token."
//                    });
//                }

//                Variables.UserId =
//                    int.Parse(userIdClaim.Value);

//                DataTable existing =
//                    await gdwMasDB.GetById(id);

//                if (existing.Rows.Count == 0)
//                {
//                    return NotFound(new
//                    {
//                        status = false,
//                        message = "Location not found."
//                    });
//                }

//                string locationName =
//                    request.LocationName.Trim();

//                if (await gdwMasDB.NameExists(
//                    locationName,
//                    id))
//                {
//                    return Conflict(new
//                    {
//                        status = false,
//                        message = "Location name already exists."
//                    });
//                }

//                int affected =
//                    await gdwMasDB.Update(
//                        id,
//                        locationName,
//                        request.GroupId,
//                        request.IsActive,
//                        request.StockAccountId,
//                        request.StockPLFlag,
//                        request.SecondName?.Trim() ?? ""
//                    );

//                if (affected <= 0)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Failed to update location."
//                    });
//                }

//                DataTable dt =
//                    await gdwMasDB.GetById(id);

//                return Ok(new
//                {
//                    status = true,
//                    message = "Location updated successfully.",
//                    data = ConvertToList(dt)
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = "Error while updating location.",
//                    error = ex.Message
//                });
//            }
//        }

//        // =========================================================
//        // ACTIVE
//        // GET: /api/v1/locations/active
//        // =========================================================

//        [HttpGet("active")]
//        public async Task<IActionResult> GetActive()
//        {
//            try
//            {
//                // ====================================================
//                // LANGUAGE / VARIABLES
//                // ====================================================

//                LngTB.InitLng();

//                Variable Variables = new Variable();

//                // ====================================================
//                // USER ID FROM JWT
//                // ====================================================

//                var userIdClaim =
//                    User.FindFirst(ClaimTypes.NameIdentifier);

//                if (userIdClaim == null)
//                {
//                    return Unauthorized(new
//                    {
//                        status = "error",
//                        message = "User ID not found in token."
//                    });
//                }

//                Variables.UserId =
//                    int.Parse(userIdClaim.Value);

//                DataTable dt =
//                    await gdwMasDB.GetActive();

//                return Ok(new
//                {
//                    status = true,
//                    message = "Active locations retrieved successfully.",
//                    data = ConvertToList(dt)
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = "Error while retrieving active locations.",
//                    error = ex.Message
//                });
//            }
//        }

//        // =========================================================
//        // SEARCH
//        // GET: /api/v1/locations/search?text=warehouse
//        // =========================================================

//        [HttpGet("search")]
//        public async Task<IActionResult> Search(
//            [FromQuery] string text = "")
//        {
//            try
//            {
//                // ====================================================
//                // LANGUAGE / VARIABLES
//                // ====================================================

//                LngTB.InitLng();

//                Variable Variables = new Variable();

//                // ====================================================
//                // USER ID FROM JWT
//                // ====================================================

//                var userIdClaim =
//                    User.FindFirst(ClaimTypes.NameIdentifier);

//                if (userIdClaim == null)
//                {
//                    return Unauthorized(new
//                    {
//                        status = "error",
//                        message = "User ID not found in token."
//                    });
//                }

//                Variables.UserId =
//                    int.Parse(userIdClaim.Value);

//                DataTable dt =
//                    await gdwMasDB.Search(text);

//                return Ok(new
//                {
//                    status = true,
//                    message = "Locations retrieved successfully.",
//                    data = ConvertToList(dt)
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = "Error while searching locations.",
//                    error = ex.Message
//                });
//            }
//        }

//        // =========================================================
//        // DROPDOWN
//        // GET: /api/v1/locations/dropdown
//        // =========================================================

//        [HttpGet("dropdown")]
//        public async Task<IActionResult> Dropdown()
//        {
//            try
//            {
//                // ====================================================
//                // LANGUAGE / VARIABLES
//                // ====================================================

//                LngTB.InitLng();

//                Variable Variables = new Variable();

//                // ====================================================
//                // USER ID FROM JWT
//                // ====================================================

//                var userIdClaim =
//                    User.FindFirst(ClaimTypes.NameIdentifier);

//                if (userIdClaim == null)
//                {
//                    return Unauthorized(new
//                    {
//                        status = "error",
//                        message = "User ID not found in token."
//                    });
//                }

//                Variables.UserId =
//                    int.Parse(userIdClaim.Value);

//                DataTable dt =
//                    await gdwMasDB.GetDropdown();

//                return Ok(new
//                {
//                    status = true,
//                    message = "Location dropdown retrieved successfully.",
//                    data = ConvertToList(dt)
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = "Error while retrieving location dropdown.",
//                    error = ex.Message
//                });
//            }
//        }
//    }
//}
