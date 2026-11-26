using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using QuickBalance.API.Configuration;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using System.Data;
using Microsoft.Extensions.Caching.Memory;
using QuickBalance.API.DTOs.Respons_DTO;
using System.Data.Common;
using QuickBalance.API.Services;

namespace QuickBalance.API.Controllers.Master
{
    [ApiController]
    [Route("api/v1/customers")]
    [Tags("09. Customers")]
    [Authorize]
    public class CustomerController : ControllerBase
    {
        private readonly CustomerDB _customerDB;
        private readonly IMemoryCache _memoryCache;
        private readonly DatabaseConnection _databaseConnection;
        private readonly ICurrentUser _currentUser;

        public CustomerController(CustomerDB customerDB,DatabaseConnection dbConnection, IMemoryCache memoryCache, ICurrentUser currentUser)

        {
            _customerDB = customerDB;
            _memoryCache = memoryCache;
            _databaseConnection = dbConnection;
            _currentUser = currentUser;
        }

        // =========================================================
        // GET CUSTOMER LIST (PAGINATED)
        // GET: api/v1/customers?page=1&pageSize=20&search=abc
        // =========================================================

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<clsCustomer>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCustomers([FromQuery] int page = 1,
            [FromQuery] int pageSize = 10, [FromQuery] string? search = null,
            [FromQuery] bool? active = null)
        {
            try
            {

                var result = await _customerDB.GetCustomers(
                    page,
                    pageSize,
                    search,
                    active
                );

                return Ok(new
                {
                    status = true,
                    data = result.Data,
                    pagination = new
                    {
                        page = page,
                        pageSize = pageSize,
                        totalRecords = result.TotalRecords,
                        totalPages = (int)Math.Ceiling(
                            (double)result.TotalRecords /
                            pageSize
                        )
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Error retrieving customers.",
                    error = ex.Message
                });
            }
        }

        // =========================================================
        // GET CUSTOMER BY ID
        // GET: api/v1/customers/{id}
        // =========================================================

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<clsCustomer>), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCustomerById(int id)
        {
            try
            {
                var customer = await _customerDB.GetCustomerById(id);

                if (customer == null)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Customer not found."
                    });
                }

                return Ok(new
                {
                    status = true,
                    data = customer
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Error retrieving customer.",
                    error = ex.Message
                });
            }
        }

        // =========================================================
        // CREATE CUSTOMER
        // POST: api/v1/customers
        // =========================================================






    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
    //[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> CreateCustomer(
    [FromBody] CustomerRequest request)
        {
            try
            {
                // 1. Validate request model.
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid customer data.",
                        errors = ModelState
                    });
                }

                // 2. Get authenticated user ID.
                var userIdClaim =
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                    ?? User.FindFirst("userId")
                    ?? User.FindFirst("userid");

                if (userIdClaim == null ||
                    !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(
                        ApiResponseWithError.Failure(
                            "Valid authenticated user ID was not found."));
                }

                // 3. Validate customer name.
                if (string.IsNullOrWhiteSpace(request.CusName))
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "Customer name is required."));
                }

                string customerName = request.CusName.Trim();

                // 4. Check duplicate customer name.
                bool exists =
                    await _customerDB.CustomerNameExists(customerName);

                if (exists)
                {
                    return Conflict(
                        ApiResponseWithError.Failure(
                            "Customer name already exists."));
                }

                // 5. Validate opening balance and pending bills.
                var openingBalance = new CustomerOpeningBalanceRequest
                {
                    Amount = request.OpenBal,
                    DrCr = request.CrDr,
                    CurrencyCode = request.CurCode,
                    ConversionRate = request.CurConvRate,
                    BranchId = 1
                };

                string? balanceValidationError =
                    ValidateOpeningBalanceAndPendingBills(
                        openingBalance,
                        request.PendingBills);

                if (balanceValidationError != null)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            balanceValidationError));
                }

                // 6. Get default or selected customer ledger group.
                var ledgerGroup =
                    await _customerDB.GetDefaultCustomerLedgerGroup(
                        request.LedgerGroupId);

                if (ledgerGroup == null)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "Selected customer ledger group does not exist or is inactive."));
                }

                // 7. Validate currency, ledger group and party group.
                string? lookupError =
                    await _customerDB.ValidateCustomerLookupsAsync(
                        request,
                        ledgerGroup.GroupId);

                if (lookupError != null)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(lookupError));
                }

                // 8. Insert customer and linked account.
                // Ensure the trimmed name is used for database insertion.
                request.CusName = customerName;

                var result =
                    await _customerDB.InsertCustomerWithAccount(
                        request,
                        ledgerGroup.GroupId,
                        ledgerGroup.GroupCode,
                        userId,
                        openingBalance,
                        request.PendingBills);

                return Ok(new
                {
                    status = true,
                    message = "Customer and account created successfully."
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(
                    ApiResponseWithError.Failure(ex.Message));
            }
            catch (Exception ex)
            {
                //_logger.LogError(
                //    ex,
                //    "Error updating customer with ID {CustomerId}",
                //    id);
                Console.WriteLine($"Customer update failed: {ex}");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseWithError.Failure("Error updating customer."));
            }
            //ca
        }
        private static string? ValidateOpeningBalanceAndPendingBills(
            CustomerOpeningBalanceRequest? openingBalance,
            List<CustomerPendingBillRequest>? pendingBills)
            {
                var bills = pendingBills ?? new List<CustomerPendingBillRequest>();

                // No opening balance: pending bills cannot be validated against a balance.
                if (openingBalance == null)
                {
                    if (bills.Count > 0)
                    {
                        return "Opening balance is required when pending bills are provided.";
                    }

                    return null;
                }

                if (openingBalance.Amount < 0)
                {
                    return "Opening balance amount cannot be negative.";
                }

                if (openingBalance.ConversionRate <= 0)
                {
                    return "Opening balance conversion rate must be greater than zero.";
                }

                string openingDrCr =
                    openingBalance.DrCr?.Trim().ToUpperInvariant() ?? "";

                if (openingDrCr != "DR" && openingDrCr != "CR")
                {
                    return "Opening balance DrCr must be either DR or CR.";
                }

                foreach (var bill in bills)
                {
                    if (bill.Amount < 0)
                    {
                        return $"Pending bill '{bill.InvoiceNumber}' amount cannot be negative.";
                    }

                    string billDrCr = bill.DrCr?.Trim().ToUpperInvariant() ?? "";

                    if (billDrCr != "DR" && billDrCr != "CR")
                    {
                        return $"Pending bill '{bill.InvoiceNumber}' DrCr must be either DR or CR.";
                    }
                }

                // Compare only bills with the same currency and debit/credit direction.
                // This avoids incorrectly combining unlike balances.
                decimal totalPendingBills = bills
                    .Where(b =>
                        string.Equals(
                            b.DrCr?.Trim(),
                            openingDrCr,
                            StringComparison.OrdinalIgnoreCase))
                    .Sum(b => b.Amount);

                if (totalPendingBills > openingBalance.Amount)
                {
                    return
                        $"Total pending bills ({totalPendingBills:N2}) cannot exceed " +
                        $"the opening balance ({openingBalance.Amount:N2}) " +
                        $"for direction {openingDrCr}.";
                }

                return null;
            }

     


[HttpPut("{id:int}")]
[ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
//[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status404NotFound)]
//[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status409Conflict)]
[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> UpdateCustomer(
    int id,
    [FromBody] CustomerUpdateRequest request)
        {
            try
            {
                // 1. Validate request model.
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid customer data.",
                        errors = ModelState
                    });
                }

                if (id <= 0)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure("Invalid customer ID."));
                }

                // 2. Get authenticated user ID.
                var userIdClaim =
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                    ?? User.FindFirst("userId")
                    ?? User.FindFirst("userid");

                if (userIdClaim == null ||
                    !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(
                        ApiResponseWithError.Failure(
                            "Valid authenticated user ID was not found."));
                }

                // 3. Validate customer name.
                if (string.IsNullOrWhiteSpace(request.CusName))
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "Customer name is required."));
                }

                string customerName = request.CusName.Trim();

                // 4. Validate RowVersion.
                if (request.RowVersion == null ||
                    request.RowVersion.Length != 8)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "A valid RowVersion is required. Reload the customer and try again."));
                }

                // 5. Get existing customer.
                var existingCustomer =
                    await _customerDB.GetCustomerById(id);

                if (existingCustomer == null)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "Customer not found."));
                }

                // 6. Validate duplicate customer name.
                bool nameExists =
                    await _customerDB.CustomerNameExists(
                        customerName,
                        id);

                if (nameExists)
                {
                    return Conflict(
                        ApiResponseWithError.Failure(
                            "Customer name already exists."));
                }

                // 7. Resolve effective ledger group.
                // If the request does not contain a ledger group,
                // retain the existing customer's ledger group.
                int effectiveLedgerGroupId =
                    request.LedgerGroupId ?? existingCustomer.LedgerGroupId;

                if (effectiveLedgerGroupId <= 0)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "A valid customer ledger group is required."));
                }

                var ledgerGroup =
                    await _customerDB.GetDefaultCustomerLedgerGroup(
                        effectiveLedgerGroupId);

                if (ledgerGroup == null)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "Selected customer ledger group does not exist or is inactive."));
                }

                // 8. Validate currency, ledger group and party group.
                string? lookupError =
                    await _customerDB.ValidateCustomerLookupsAsync(
                        request,
                        ledgerGroup.GroupId);

                if (lookupError != null)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(lookupError));
                }

                // 9. Validate opening balance and pending bills.
                var openingBalance = new CustomerOpeningBalanceRequest
                {
                    Amount = request.OpenBal,
                    DrCr = request.CrDr,
                    CurrencyCode = request.CurCode,
                    ConversionRate = request.CurConvRate,
                    BranchId = 1
                };

                string? balanceValidationError =
                    ValidateOpeningBalanceAndPendingBills(
                        openingBalance,
                        request.PendingBills);

                if (balanceValidationError != null)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            balanceValidationError));
                }

                // 10. Update customer and linked account.
                bool updated = await _customerDB.UpdateCustomer(
                    userId,
                    id,
                    request,
                    request.RowVersion,
                    request.CurCode,
                    effectiveLedgerGroupId,
                    openingBalance,
                    request.PendingBills);

                if (!updated)
                {
                    var currentCustomer =
                        await _customerDB.GetCustomerById(id);

                    if (currentCustomer == null)
                    {
                        return BadRequest(
                            ApiResponseWithError.Failure(
                                "Customer not found or has been deleted."));
                    }

                    return Conflict(
                        ApiResponseWithError.Failure(
                            "This customer was modified by another user. Reload the latest customer data before saving again."));
                }

                // 11. Retrieve updated customer.
                var updatedCustomer =
                    await _customerDB.GetCustomerById(id);

                return Ok(new
                {
                    status = true,
                    message = "Customer and linked account updated successfully."
                    //data = updatedCustomer
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(
                    ApiResponseWithError.Failure(ex.Message));
            }
            catch (Exception ex)
            {
                //_logger.LogError(
                //    ex,
                //    "Error updating customer with ID {CustomerId}",
                //    id);
                    Console.WriteLine($"Customer update failed: {ex}");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseWithError.Failure("Error updating customer."));
            }
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Customer update failed: {ex}");

            //    return StatusCode(
            //        StatusCodes.Status500InternalServerError,
            //        ApiResponseWithError.Failure(
            //            "Error updating customer."));
            //}
        }

        // =========================================================
        // DELETE CUSTOMER
        // DELETE: api/v1/customers/{id}
        // =========================================================

        [HttpDelete("{id:int}")]

        [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status404NotFound)]
        //[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            try
            {
                var customer = await _customerDB.GetCustomerById(id);

                if (customer == null)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Customer not found."
                    });
                }

                bool deleted = await _customerDB.DeleteCustomer(
                    id, _currentUser.UserId.ToString());

                if (!deleted)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Customer not found."
                    });
                }

                return Ok(new
                {
                    status = true,
                    message = "Customer deleted successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Error deleting customer.",
                    error = ex.Message
                });
            }
        }

        // =========================================================
        // CUSTOMER LOOKUP APIs
        // =========================================================

        //[HttpGet("lookups/currencies")]
        //public async Task<IActionResult> GetCurrencies([FromQuery] PaginationRequests pagination)
        //{
        //    try
        //    {
        //        pagination ??= new PaginationRequests();
        //        pagination.Normalizes();

        //        var dt = await _customerDB.GetCustomerCurrencies(
        //              //pagination.Page,
        //              //pagination.PageSize
        //          );

        //        var data = dt.AsEnumerable()
        //            .Select(row => dt.Columns.Cast<DataColumn>()
        //                .ToDictionary(
        //                    column => column.ColumnName,
        //                    column => row[column] == DBNull.Value
        //                        ? null
        //                        : row[column]
        //                ))
        //            .ToList();


        //        return Ok(new
        //        {
        //            status = true,
        //            message = "Currencies retrieved successfully.",
        //            data = data,
        //            //pagination = new
        //            //{
        //            //    page = pagination.Page,
        //            //    pageSize = pagination.PageSize,
        //            //    totalRecords = result.TotalRecords,
        //            //    totalPages = (int)Math.Ceiling(
        //            //        (double)result.TotalRecords / pagination.PageSize
        //            //    )
        //            //}
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.ToString());

        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            message = "Failed to retrieve currencies.",
        //            error = ex.Message
        //        });
        //    }
        //}
        // GET: api/v1/customers/lookups/ledger-groups
        [HttpGet("lookups/ledger-groups")]
        [ProducesResponseType(typeof(ApiResponseWithoutPagination<LookupItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public IActionResult GetLedgerGroups()
        {
            try
            {
                var data = _customerDB.GetCustomerLedgerGroups();

                return Ok(new
                {
                    status = true,
                    message = "Ledger groups retrieved successfully.",
                    data
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

                return StatusCode(500, new
                {
                    status = false,
                    message = "Failed to retrieve ledger groups.",
                    error = ex.Message
                });
            }
        }
        // GET: api/v1/customers/lookups/item-groups
        //[HttpGet("lookups/item-groups")]
        //[ProducesResponseType(typeof(ApiResponse<LookupItem>), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status404NotFound)]
        //[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status409Conflict)]
        //[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        //[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        //public IActionResult GetItemGroups()
        //{
        //    try
        //    {
        //        var data = _customerDB.GetCustomerItemGroups();

        //        return Ok(new
        //        {
        //            status = true,
        //            message = "Item groups retrieved successfully.",
        //            data
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.ToString());

        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            message = "Error retrieving item groups.",
        //            error = ex.Message
        //        });
        //    }
        //}






        // GET: api/v1/customers/lookups/vat-types
        //[HttpGet("lookups/vat-types")]
        //public IActionResult GetVatTypes()
        //{
        //    try
        //    {
        //        var data = _customerDB.GetCustomerVatTypes();

        //        return Ok(new
        //        {
        //            status = true,
        //            message = "VAT types retrieved successfully.",
        //            data
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            message = "Error retrieving VAT types.",
        //            error = ex.Message
        //        });
        //    }
        //}

        //[HttpGet("lookups/einvoice-id-types")]
        //public IActionResult GetEInvoiceIdTypes()
        //{
        //    try
        //    {
        //        var data = _customerDB.GetCustomerEInvoiceIdTypes();

        //        return Ok(new
        //        {
        //            status = true,
        //            message = "E-Invoice ID types retrieved successfully.",
        //            data
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            message = "Error retrieving E-Invoice ID types.",
        //            error = ex.Message
        //        });
        //    }
        //}
        // GET: api/v1/customers/lookups/countries
        //[HttpGet("lookups/countries")]
        //public async Task<IActionResult> GetCountries()
        //{
        //    //[FromQuery] PaginationRequest pagination
        //    try
        //    {
        //        Variable Variables = new Variable();
        //        //DatabaseConnection databaseConnection = new DatabaseConnection();
        //        string connectionString = _databaseConnection.GetConnectionString(Variables, isMasterDB: true);

        //        //string connectionString = databaseConnection.GetConnectionString(
        //        //Variables,
        //        //    isMasterDB: true
        //        //);
        //        //pagination ??= new PaginationRequest();
        //        //pagination.Normalize();

        //        DBConn db = new DBConn(connectionString);
        //        CustomerDB customerDB = new CustomerDB(db);

        //        DataTable dt = await customerDB.GetCountries();

        //        var tableData = dt.AsEnumerable()
        //            .Select(row => dt.Columns.Cast<DataColumn>()
        //                .ToDictionary(
        //                    column => column.ColumnName,
        //                    column => row[column] == DBNull.Value ? null : row[column]
        //                ))
        //            .ToList();
        //        //var result = await customerDB.GetCountries();

        //        return Ok(new
        //        {
        //            status = true,
        //            message = "Countries retrieved successfully.",
        //            data = tableData,
        //            //pagination = new
        //            //{
        //            //    page = pagination.Page,
        //            //    pageSize = pagination.PageSize,
        //            //    totalRecords = result.TotalRecords,
        //            //    totalPages = (int)Math.Ceiling(
        //            //        (double)result.TotalRecords / pagination.PageSize
        //            //    )
        //            //}
        //        });
        //        //return Ok(new
        //        //{
        //        //    status = true,
        //        //    data = tableData
        //        //});
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            message = "Failed to retrieve countries.",
        //            error = ex.Message
        //        });
        //    }
        //}


        // GET: api/v1/customers/lookups/regions?countryId=1
        //[HttpGet("lookups/regions")]
        //public async Task<IActionResult> GetRegions([FromQuery] int countryId)
        //{
        //    if (countryId <= 0)
        //    {
        //        return BadRequest(new
        //        {
        //            status = false,
        //            message = "Valid country ID is required."
        //        });
        //    }

        //    try
        //    {
        //        Variable Variables = new Variable();
        //        //DatabaseConnection databaseConnection = new DatabaseConnection();

        //        string connectionString = _databaseConnection.GetConnectionString(
        //            Variables,
        //            isMasterDB: true
        //        );

        //        DBConn db = new DBConn(connectionString);
        //        CustomerDB customerDB = new CustomerDB(db);

                //DataTable dt = await customerDB.GetRegions(countryId);

        //        var tableData = dt.AsEnumerable()
        //            .Select(row => dt.Columns.Cast<DataColumn>()
        //                .ToDictionary(
        //                    column => column.ColumnName,
        //                    column => row[column] == DBNull.Value ? null : row[column]
        //                ))
        //            .ToList();

        //        return Ok(new
        //        {
        //            status = true,
        //            data = tableData
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            message = "Failed to retrieve regions.",
        //            error = ex.Message
        //        });   
        //    }
        //}


        //[HttpGet("lookups/party-groups")]
        //public IActionResult GetPartyGroups()
        //{
        //    try
        //    {
        //        var data = _customerDB.GetCustomerPartyGroups();

        //        return Ok(new
        //        {
        //            status = true,
        //            message = "Party groups retrieved successfully.",
        //            data
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.ToString());

        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            message = "Error retrieving party groups.",
        //            error = ex.Message
        //        });
        //    }
        //}
        [HttpGet("lookups/salesman")]
        [ProducesResponseType(typeof(ApiResponseWithoutPagination<LookupItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public IActionResult GetSalesmen()
        {
            try
            {
                var data = _customerDB.GetSalesmen();

                return Ok(new
                {
                    status = true,
                    message = "salesman retrieved successfully.",
                    data
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Error retrieving salesman.",
                    error = ex.Message
                });
            }
        }

        //[HttpGet("lookups/product-templates")]
        //public IActionResult GetProductTemplates()
        //{
        //    try
        //    {
        //        var data = _customerDB.GetProductTemplates();

        //        return Ok(new
        //        {
        //            status = true,
        //            message = "Product templates retrieved successfully.",
        //            data
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            message = "Error retrieving product templates.",
        //            error = ex.Message
        //        });
        //    }
        //}

        //[HttpGet("lookups/price-categories")]
        //public IActionResult GetPriceCategories()
        //{
        //    try
        //    {
        //        var data = _customerDB.GetPriceCategories();

        //        return Ok(new
        //        {
        //            status = true,
        //            message = "Price categories retrieved successfully.",
        //            data
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            message = "Error retrieving price categories.",
        //            error = ex.Message
        //        });
        //    }
        //}

        //[HttpGet("lookups/price-lists")]
        //public IActionResult GetPriceLists()
        //{
        //    try
        //    {
        //        var data = _customerDB.GetPriceLists();

        //        return Ok(new
        //        {
        //            status = true,
        //            message = "Price lists retrieved successfully.",
        //            data
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            message = "Error retrieving price lists.",
        //            error = ex.Message
        //        });
        //    }
        //}

        //[HttpGet("lookups/points-systems")]
        //public IActionResult GetPointSystems()
        //{
        //    try
        //    {
        //        var data = _customerDB.GetPointSystems();

        //        return Ok(new
        //        {
        //            status = true,
        //            message = "Points systems retrieved successfully.",
        //            data
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            message = "Error retrieving points systems.",
        //            error = ex.Message
        //        });
        //    }
        //}

        // =========================================================
        // MAP REQUEST TO CUSTOMER
        // =========================================================

        private static clsCustomer MapRequestToCustomer(
            CustomerRequest request)
        {
            return new clsCustomer
            {
                Alias = request.Alias,
                CusName = request.CusName,
                Address = request.Address,
                Phone = request.Phone,
                Email = request.Email,
                Active = request.Active,
                //LogCd = request.LogCd,
                //AccId = request.AccId,
                //Disc = request.Disc,
                StateCode = request.StateCode,
                PrintName = request.PrintName,
                CountryCode = request.CountryCode,
                //PriceLevel = request.PriceLevel,
                BnkAccName = request.BnkAccName,
                BnkAccNo = request.BnkAccNo,
                Branch = request.Branch,
                IbanNo = request.IbanNo,
                SwiftCode=request.SwiftCode,
                VatFlag = request.VatFlag,
                BillFlag = request.BillFlag,
                CrPerFlag = request.CrPerFlag,
                CrLimitFlag = request.CrLimitFlag,
                CrLimit = request.CrLimit,
                //UiTime = request.UiTime,
                VatRegNo = request.VatRegNo,
                ShipAddress = request.ShipAddress,
                SecName = request.SecName,
                CurFlg = request.CurFlg,
                //SalesMan = request.SalesMan,
                CrPeriod = request.CrPeriod,
                //GrpItem = request.GrpItem,
                //ItemGroupCD = request.ItemGroupCD,
                GccFlg = request.GccFlg,
                //DelFlg = request.DelFlg,
                //PrTmpCode = request.PrTmpCode,
                //PntSysId = request.PntSysId,
                StreetOne = request.StreetOne,
                StreetTwo = request.StreetTwo,
                BuildingNo = request.BuildingNo,
                PlotId = request.PlotId,
                CityName = request.CityName,
                PostalCode = request.PostalCode,
                District = request.District,
                GrpVatNo = request.GrpVatNo,
                EinvIdType = request.EinvIdType,
                EinvId = request.EinvId,
                VatType = request.VatType,
                //PrListCd = request.PrListCd,
                //PrCatCd = request.PrCatCd,
                PartyGrpId = request.PartyGrpId,
                //CityNameSec = request.CityNameSec,
                //DistrictSec = request.DistrictSec,
                //AprvLst = request.AprvLst,
                //RowVersion = request.RowVersion,
                
            };
        }
        private async Task<List<Dictionary<string, object?>>> GetMasterCountries()
        {
            var variables = new Variable();
            //var databaseConnection = new DatabaseConnection();
            string connectionString = _databaseConnection.GetConnectionString(variables, isMasterDB: true);

            //string connectionString = databaseConnection.GetConnectionString(
                //variables,
                //isMasterDB: true
            //);

            var db = new DBConn(connectionString);
            var masterCustomerDB = new CustomerDB(db);

            DataTable dt = await masterCustomerDB.GetCountries();

            return dt.AsEnumerable()
                .Select(row => dt.Columns.Cast<DataColumn>()
                    .ToDictionary(
                        column => column.ColumnName,
                        column => row[column] == DBNull.Value
                            ? null
                            : row[column]
                    ))
                .ToList();
        }

        //[HttpGet("form-options")]
        //public async Task<IActionResult> GetCustomerFormOptions()
        //{
        //    try
        //    {
        //        // Use the tenant/company claim when available.
        //        // Fall back to the authenticated user's ID to avoid
        //        // sharing cached data between unidentified tenants.
        //        var tenantId =
        //            User.FindFirst("CompanyId")?.Value
        //            ?? User.FindFirst("company_id")?.Value
        //            ?? User.FindFirst(
        //                System.Security.Claims.ClaimTypes.NameIdentifier
        //            )?.Value;

        //        if (string.IsNullOrWhiteSpace(tenantId))
        //        {
        //            return Unauthorized(new
        //            {
        //                status = false,
        //                message = "Unable to identify the authenticated user."
        //            });
        //        }

        //        // Include the tenant identifier in the cache key.
        //        string cacheKey = $"CustomerFormOptions:{tenantId}";

        //        // Reuse cached JSON and ETag on a cache hit.
        //        if (!_memoryCache.TryGetValue(
        //                cacheKey,
        //                out CachedFormOptions? cached)
        //            || cached == null)
        //        {
        //            // 1. Currencies
        //            DataTable currenciesTable =
        //                await _customerDB.GetCustomerCurrencies();

        //            var currencies = currenciesTable.AsEnumerable()
        //                .Select(row => currenciesTable.Columns.Cast<DataColumn>()
        //                    .ToDictionary(
        //                        column => column.ColumnName,
        //                        column => row[column] == DBNull.Value
        //                            ? null
        //                            : row[column]
        //                    ))
        //                .ToList();

        //            // 2. Ledger groups
        //            var ledgerGroups =
        //                _customerDB.GetCustomerLedgerGroups();

        //            // 3. Item groups
        //            var itemGroups =
        //                _customerDB.GetCustomerItemGroups();

        //            // 4. VAT types
        //            var vatTypes =
        //                _customerDB.GetCustomerVatTypes();

        //            // 5. E-Invoice ID types
        //            var einvoiceIdTypes =
        //                _customerDB.GetCustomerEInvoiceIdTypes();

        //            // 6. Countries from MasterDB
        //            var countries = await GetMasterCountries();

        //            // 7. Party groups
        //            var partyGroups =
        //                _customerDB.GetCustomerPartyGroups();

        //            // 8. Salesmen
        //            var salesmen = _customerDB.GetSalesmen();

        //            // 9. Product templates
        //            var productTemplates =
        //                _customerDB.GetProductTemplates();

        //            // 10. Price categories
        //            var priceCategories =
        //                _customerDB.GetPriceCategories();

        //            // 11. Price lists
        //            var priceLists =
        //                _customerDB.GetPriceLists();

        //            // 12. Points systems
        //            var pointsSystems =
        //                _customerDB.GetPointSystems();

        //            var response = new
        //            {
        //                status = true,
        //                data = new
        //                { currencies, ledgerGroups, itemGroups, vatTypes, einvoiceIdTypes, countries, partyGroups, salesmen, productTemplates, priceCategories, priceLists, pointsSystems }

        //            };

        //            string json =
        //                System.Text.Json.JsonSerializer.Serialize(response);

        //            byte[] responseBytes =
        //                System.Text.Encoding.UTF8.GetBytes(json);

        //            string etag = "\"" + Convert.ToHexString(
        //                System.Security.Cryptography.SHA256.HashData(
        //                    responseBytes
        //                )
        //            ) + "\"";

        //            cached = new CachedFormOptions(json, etag);

        //            // Cache the complete JSON and ETag for five minutes.
        //            _memoryCache.Set(
        //                cacheKey,
        //                cached,
        //                new MemoryCacheEntryOptions
        //                {
        //                    AbsoluteExpirationRelativeToNow =
        //                        TimeSpan.FromMinutes(5)
        //                });
        //        }

        //        // Preserve existing HTTP cache headers.
        //        Response.Headers.ETag = cached.ETag;
        //        Response.Headers.CacheControl = "private, max-age=300";

        //        // Validate the client's ETag without querying the database.
        //        var ifNoneMatch = Request.Headers.IfNoneMatch;

        //        bool etagMatches = ifNoneMatch
        //            .SelectMany(value =>
        //                (value ?? string.Empty).Split(','))
        //            .Select(value => value.Trim())
        //            .Any(value =>
        //                value == cached.ETag ||
        //                value == "*" ||
        //                (value.StartsWith("W/", StringComparison.Ordinal)
        //                    && value.Substring(2) == cached.ETag));

        //        if (etagMatches)
        //        {
        //            return StatusCode(
        //                StatusCodes.Status304NotModified);
        //        }

        //        // Return the cached JSON without rebuilding the lookup data.
        //        return new ContentResult
        //        {
        //            StatusCode = StatusCodes.Status200OK,
        //            Content = cached.Json,
        //            ContentType = "application/json; charset=utf-8"
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(
        //            $"Customer form options failed: {ex}");

        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            message = "Failed to load customer form options.",
        //            error = ex.Message
        //        });
        //    }
        //}

        // Holds the serialized response and its ETag.
        private sealed record CachedFormOptions(
            string Json,
            string ETag
        );

    }

}
