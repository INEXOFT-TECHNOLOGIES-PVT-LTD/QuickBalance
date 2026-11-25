using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Security.Claims;
using QuickBalance.API.CLS;
using QuickBalance.API.Configuration;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;

namespace QuickBalance.API.Controllers.Master
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/v1/categories")]
    [Authorize]
    public class CategoryController : ControllerBase
    {
        private readonly CatgMasDB categoryDb;

        public CategoryController(CatgMasDB categoryDb)
        {
            this.categoryDb = categoryDb;
        }

        // ============================================================
        // POST: api/v1/categories
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CategoryMasterRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Request data is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(request.CategoryName))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Category name must not be blank."
                    });
                }

                // ====================================================
                // LANGUAGE / VARIABLES
                // ====================================================

                LngTB.InitLng();

                Variable Variables = new Variable();

                // ====================================================
                // USER ID FROM JWT
                // ====================================================

                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = "error",
                        message = "User ID not found in token."
                    });
                }

                Variables.UserId =
                    int.Parse(userIdClaim.Value);

                string categoryName = request.CategoryName.Trim();

                if (await categoryDb.NameExists(categoryName))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Category name already exists."
                    });
                }

                string? userId =
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                int logCode = 0;

                if (!int.TryParse(userId, out logCode))
                {
                    return Unauthorized(new
                    {
                        status = false,
                        message = "Invalid user information."
                    });
                }

                int categoryCode = await categoryDb.Insert(
                    categoryName,
                    request.Alias?.Trim() ?? "",
                    request.IsActive,
                    logCode,
                    request.SecondName?.Trim() ?? ""
                );

                if (categoryCode <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Category creation failed."
                    });
                }

                DataTable dt =
                    await categoryDb.GetById(categoryCode);

                return Ok(new
                {
                    status = true,
                    message = "Category created successfully.",
                    data = ConvertToList(dt).FirstOrDefault()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred while creating category.",
                    error = ex.Message
                });
            }
        }

        // ============================================================
        // GET (PAGINATED): api/v1/categories?page=1&pageSize=20&search=abc&sortBy=CATGNAME&sortDirection=ASC
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] PaginationRequest pagination = null!)
        {
            try
            {
                // ====================================================
                // PAGINATION
                // ====================================================

                pagination ??= new PaginationRequest();

                pagination.Normalize();

                // ====================================================
                // LANGUAGE / VARIABLES
                // ====================================================

                LngTB.InitLng();

                Variable Variables = new Variable();

                // ====================================================
                // USER ID FROM JWT
                // ====================================================

                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = "error",
                        message = "User ID not found in token."
                    });
                }

                Variables.UserId =
                    int.Parse(userIdClaim.Value);

                DataTable dt = await categoryDb.GetAll();

                // ====================================================
                // SEARCH (ANY COLUMN, CASE-INSENSITIVE)
                // ====================================================

                IEnumerable<DataRow> rows =
                    dt.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(pagination.Search))
                {
                    string search =
                        pagination.Search.Trim();

                    rows = rows.Where(row =>
                        dt.Columns
                          .Cast<DataColumn>()
                          .Any(column =>
                              row[column] != DBNull.Value &&
                              (Convert.ToString(row[column]) ?? "")
                                  .Contains(
                                      search,
                                      StringComparison.OrdinalIgnoreCase
                                  )
                          )
                    );
                }

                // ====================================================
                // SORTING
                // Only columns that really exist in the result set
                // are accepted; anything else is ignored.
                // ====================================================

                if (!string.IsNullOrWhiteSpace(pagination.SortBy))
                {
                    string sortBy =
                        pagination.SortBy.Trim();

                    DataColumn? sortColumn =
                        dt.Columns
                          .Cast<DataColumn>()
                          .FirstOrDefault(column =>
                              string.Equals(
                                  column.ColumnName,
                                  sortBy,
                                  StringComparison.OrdinalIgnoreCase
                              )
                          );

                    if (sortColumn != null)
                    {
                        var comparer =
                            Comparer<object?>.Create((a, b) =>
                            {
                                if (a == null && b == null)
                                {
                                    return 0;
                                }

                                if (a == null)
                                {
                                    return -1;
                                }

                                if (b == null)
                                {
                                    return 1;
                                }

                                if (a is IComparable comparable &&
                                    a.GetType() == b.GetType())
                                {
                                    return comparable.CompareTo(b);
                                }

                                return string.Compare(
                                    Convert.ToString(a),
                                    Convert.ToString(b),
                                    StringComparison.OrdinalIgnoreCase
                                );
                            });

                        if (pagination.SortDirection == "ASC")
                        {
                            rows = rows.OrderBy<DataRow, object?>(
                                row =>
                                    row[sortColumn] == DBNull.Value
                                        ? null
                                        : row[sortColumn],
                                comparer
                            );
                        }
                        else
                        {
                            rows = rows.OrderByDescending<DataRow, object?>(
                                row =>
                                    row[sortColumn] == DBNull.Value
                                        ? null
                                        : row[sortColumn],
                                comparer
                            );
                        }
                    }
                }

                // ====================================================
                // TOTALS
                // ====================================================

                var filteredRows =
                    rows.ToList();

                int totalRecords =
                    filteredRows.Count;

                int totalPages =
                    (int)Math.Ceiling(
                        totalRecords / (double)pagination.PageSize
                    );

                // ====================================================
                // CONVERT DATATABLE (CURRENT PAGE ONLY)
                // ====================================================

                var tableData =
                    filteredRows
                      .Skip(pagination.Page * pagination.PageSize)
                      .Take(pagination.PageSize)
                      .Select(row =>
                          dt.Columns
                            .Cast<DataColumn>()
                            .ToDictionary(
                                column => column.ColumnName,
                                column =>
                                    row[column] == DBNull.Value
                                        ? null
                                        : row[column]
                            )
                      )
                      .ToList();

                return Ok(new
                {
                    status = true,
                    message = "Categories retrieved successfully.",
                    data = tableData,
                    page = pagination.Page,
                    pageSize = pagination.PageSize,
                    totalRecords = totalRecords,
                    totalPages = totalPages
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred while retrieving categories.",
                    error = ex.Message
                });
            }
        }

        // ============================================================
        // GET: api/v1/categories/{id}
        // ============================================================
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid category ID."
                    });
                }

                // ====================================================
                // LANGUAGE / VARIABLES
                // ====================================================

                LngTB.InitLng();

                Variable Variables = new Variable();

                // ====================================================
                // USER ID FROM JWT
                // ====================================================

                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = "error",
                        message = "User ID not found in token."
                    });
                }

                Variables.UserId =
                    int.Parse(userIdClaim.Value);

                DataTable dt =
                    await categoryDb.GetById(id);

                if (dt.Rows.Count == 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Category not found."
                    });
                }

                return Ok(new
                {
                    status = true,
                    message = "Category retrieved successfully.",
                    data = ConvertToList(dt).FirstOrDefault()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred while retrieving category.",
                    error = ex.Message
                });
            }
        }

        // ============================================================
        // PUT: api/v1/categories/{id}
        // ============================================================
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] CategoryMasterRequest request)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid category ID."
                    });
                }

                if (request == null)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Request data is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(request.CategoryName))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Category name must not be blank."
                    });
                }

                // ====================================================
                // LANGUAGE / VARIABLES
                // ====================================================

                LngTB.InitLng();

                Variable Variables = new Variable();

                // ====================================================
                // USER ID FROM JWT
                // ====================================================

                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = "error",
                        message = "User ID not found in token."
                    });
                }

                Variables.UserId =
                    int.Parse(userIdClaim.Value);

                DataTable existing =
                    await categoryDb.GetById(id);

                if (existing.Rows.Count == 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Category not found."
                    });
                }

                string categoryName =
                    request.CategoryName.Trim();

                if (await categoryDb.NameExists(categoryName, id))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Category name already exists."
                    });
                }

                int result = await categoryDb.Update(
                    id,
                    categoryName,
                    request.Alias?.Trim() ?? "",
                    request.IsActive,
                    request.SecondName?.Trim() ?? ""
                );

                if (result <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Category update failed."
                    });
                }

                DataTable updated =
                    await categoryDb.GetById(id);

                return Ok(new
                {
                    status = true,
                    message = "Category updated successfully.",
                    data = ConvertToList(updated).FirstOrDefault()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred while updating category.",
                    error = ex.Message
                });
            }
        }

        // ============================================================
        // GET: api/v1/categories/active
        // ============================================================
        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            try
            {
                // ====================================================
                // LANGUAGE / VARIABLES
                // ====================================================

                LngTB.InitLng();

                Variable Variables = new Variable();

                // ====================================================
                // USER ID FROM JWT
                // ====================================================

                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = "error",
                        message = "User ID not found in token."
                    });
                }

                Variables.UserId =
                    int.Parse(userIdClaim.Value);

                DataTable dt =
                    await categoryDb.GetActive();

                return Ok(new
                {
                    status = true,
                    message = "Active categories retrieved successfully.",
                    data = ConvertToList(dt)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred while retrieving active categories.",
                    error = ex.Message
                });
            }
        }

        // ============================================================
        // GET: api/v1/categories/search?text=...
        // ============================================================
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string text = "")
        {
            try
            {
                // ====================================================
                // LANGUAGE / VARIABLES
                // ====================================================

                LngTB.InitLng();

                Variable Variables = new Variable();

                // ====================================================
                // USER ID FROM JWT
                // ====================================================

                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = "error",
                        message = "User ID not found in token."
                    });
                }

                Variables.UserId =
                    int.Parse(userIdClaim.Value);

                DataTable dt =
                    await categoryDb.Search(text ?? "");

                return Ok(new
                {
                    status = true,
                    message = "Categories retrieved successfully.",
                    data = ConvertToList(dt)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred while searching categories.",
                    error = ex.Message
                });
            }
        }

        // ============================================================
        // GET: api/v1/categories/dropdown
        // ============================================================
        [HttpGet("dropdown")]
        public async Task<IActionResult> GetDropdown()
        {
            try
            {
                // ====================================================
                // LANGUAGE / VARIABLES
                // ====================================================

                LngTB.InitLng();

                Variable Variables = new Variable();

                // ====================================================
                // USER ID FROM JWT
                // ====================================================

                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = "error",
                        message = "User ID not found in token."
                    });
                }

                Variables.UserId =
                    int.Parse(userIdClaim.Value);

                DataTable dt =
                    await categoryDb.GetDropdown();

                return Ok(new
                {
                    status = true,
                    message = "Category dropdown retrieved successfully.",
                    data = ConvertToList(dt)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred while retrieving category dropdown.",
                    error = ex.Message
                });
            }
        }

        // ============================================================
        // GET: api/v1/categories/export
        // ============================================================
        [HttpGet("export")]
        public async Task<IActionResult> Export()
        {
            try
            {
                // ====================================================
                // LANGUAGE / VARIABLES
                // ====================================================

                LngTB.InitLng();

                Variable Variables = new Variable();

                // ====================================================
                // USER ID FROM JWT
                // ====================================================

                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = "error",
                        message = "User ID not found in token."
                    });
                }

                Variables.UserId =
                    int.Parse(userIdClaim.Value);

                DataTable dt =
                    await categoryDb.ExportToJson();

                return Ok(new
                {
                    status = true,
                    message = "Categories exported successfully.",
                    data = ConvertToList(dt)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred while exporting categories.",
                    error = ex.Message
                });
            }
        }

        // ============================================================
        // DATATABLE -> LIST
        // ============================================================
        private List<Dictionary<string, object?>> ConvertToList(
            DataTable table)
        {
            var list =
                new List<Dictionary<string, object?>>();

            foreach (DataRow row in table.Rows)
            {
                var item =
                    new Dictionary<string, object?>();

                foreach (DataColumn column in table.Columns)
                {
                    object value = row[column];

                    item[column.ColumnName] =
                        value == DBNull.Value ? null : value;
                }

                list.Add(item);
            }

            return list;
        }
    }
}
