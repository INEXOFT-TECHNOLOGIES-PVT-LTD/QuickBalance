using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using QuickBalance.API.CLS;
using QuickBalance.API.Configuration;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Respons_DTO;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;


namespace QuickBalance.API.Controllers.Master
{
    [Route("api/v1/suppliers")]
    [ApiController]
    [Tags("10. Supplier")]
    [Authorize]
    public class SupplierController : ControllerBase
    {
        private readonly DatabaseConnection _databaseConnection;
        private readonly SupplierDB _supplierDB;
        private readonly IMemoryCache _memoryCache;

        public SupplierController(DatabaseConnection databaseConnection, SupplierDB supplierDB, IMemoryCache memoryCache)
        {
            _databaseConnection = databaseConnection;
            _supplierDB = supplierDB;
            _memoryCache = memoryCache;
        }
        // =========================================================
        // MAP REQUEST DTO TO CLS
        // =========================================================

        

        // =========================================================
        // GET ALL SUPPLIERS
        // GET: api/v1/suppliers?page=1&pageSize=10&search=
        // =========================================================

        


        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<clsSupplier>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSuppliers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] bool? active = null)
        {
            try
            {
                if (page < 1)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "Page must be greater than 0."));
                }

                if (pageSize < 1 || pageSize > 500)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "Page size must be between 1 and 500."));
                }

                var result = await _supplierDB.GetSuppliers(
                    page,
                    pageSize,
                    search,
                    active);

                int totalPages = (int)Math.Ceiling(
                    result.TotalRecords / (double)pageSize);

                var pagination = new PaginationInfo
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalRecords = result.TotalRecords,
                    TotalPages = totalPages
                };

                return Ok(
                    ApiResponse<List<clsSupplierResponse>>.SuccessWithPagination(
                        "Suppliers retrieved successfully.",
                        result.Data,
                        pagination));
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseWithError.Failure(
                        "Failed to retrieve suppliers."));
            }
        }
        // =========================================================
        // GET SUPPLIER BY ID
        // GET: api/v1/suppliers/1
        // =========================================================

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithoutPagination<clsSupplier>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSupplierById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid supplier ID."
                    });
                }

                var supplier = await _supplierDB.GetSupplierById(id);

                if (supplier == null)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Supplier not found."
                    });
                }

                return Ok(new
                {
                    status = true,
                    data = supplier
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Failed to retrieve supplier.",
                    error = ex.Message
                });
            }
        }

        // =========================================================
        // CREATE SUPPLIER
        // POST: api/v1/suppliers
        // =========================================================

    [HttpPost]
        [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateSupplier(
    [FromBody] SupplierRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(
                        ApiResponseWithoutPagination<object>.Failure(
                            "Invalid supplier data."));
                }

                if (string.IsNullOrWhiteSpace(request.SupName))
                {
                    return BadRequest(
                        ApiResponseWithoutPagination<object>.Failure(
                            "Supplier name is required."));
                }

                var userIdClaim =
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                    ?? User.FindFirst("userId")
                    ?? User.FindFirst("userid");

                if (userIdClaim == null ||
                    !int.TryParse(userIdClaim.Value, out int userId) ||
                    userId <= 0)
                {
                    return Unauthorized(
                        ApiResponseWithoutPagination<object>.Failure(
                            "Unauthorized user."));
                }

                string supplierName = request.SupName.Trim();

                bool exists = await _supplierDB.SupplierNameExists(
                    supplierName);

                if (exists)
                {
                    return BadRequest(
                        ApiResponseWithoutPagination<object>.Failure(
                            "Supplier name already exists."));
                }

                //decimal openingBalance = request.OpenBal;

                //decimal totalPendingBalance = request.PendingBills?
                //    .Sum(b => Math.Abs(b.Amount)) ?? 0m;

                //if (totalPendingBalance > openingBalance)
                //{
                //    return BadRequest(
                //        ApiResponseWithouPagination<object>.Failure(
                //            "Total pending bills balance cannot exceed the opening balance."));
                //}

                string? financialError = ValidateSupplierFinancials(
                    request,
                    requirePendingBills: false);

                if (financialError != null)
                {
                    return BadRequest(
                        ApiResponseWithoutPagination<object>.Failure(financialError));
                }
                var ledgerGroup =await _supplierDB.GetDefaultSupplierLedgerGroup();

                if (ledgerGroup == null ||
                    string.IsNullOrWhiteSpace(ledgerGroup.GroupCode) ||
                    ledgerGroup.GroupId <= 0)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "Default supplier ledger group was not found."));
                }

                //var supplier =  (userId, request);
                request.SupName = supplierName;

                string? lookupError =
                    await _supplierDB.ValidateSupplierLookupsAsync(
                        request,
                        ledgerGroup.GroupId);

                if (lookupError != null)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(lookupError));
                }

                var result = await _supplierDB.InsertSupplier(
                    request,
                    ledgerGroup.GroupId,
                    ledgerGroup.GroupCode,
                    userId);
                ////supplier.SupName = supplierName;

                //// Validate master/lookup values before inserting.
                //string? lookupError =
                //    await _supplierDB.ValidateSupplierLookupsAsync(
                //        supplier,
                //        ledgerGroup.GroupId);

                //if (lookupError != null)
                //{
                //    return BadRequest(
                //        ApiResponseWithError.Failure(lookupError));
                //}

                //var result = await _supplierDB.InsertSupplier(
                //    supplier,
                //    ledgerGroup.GroupId,
                //    ledgerGroup.GroupCode,userId);


                if (result.SupplierId <= 0)
                {
                    return StatusCode(
                        StatusCodes.Status500InternalServerError,
                        ApiResponseWithoutPagination<object>.Failure(
                            "Supplier creation failed."));
                }

                ////var responseData = new SupplierCreateResult
                ////{
                ////    Id = result.SupplierId,
                ////    AccountId = result.AccountId,
                ////    AccountCode = result.AccountCode
                ////};

                return Ok(
                    ApiResponseWithoutPagination<string>.Success(
                        "Supplier and account created successfully."));
            }
            catch (ArgumentException ex)
            {
              return StatusCode(StatusCodes.Status500InternalServerError,
                 ApiResponseWithoutPagination<object>.Failure(
                    $"Failed to create supplier.{ex.Message}."));
            }
            catch (Exception)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseWithoutPagination<object>.Failure(
                        "Failed to create supplier."));
            }
        }



        // =========================================================
        // UPDATE SUPPLIER
        // PUT: api/v1/suppliers/1
        // =========================================================




        // =========================================================
        // UPDATE SUPPLIER
        // PUT: api/v1/suppliers/1
        // =========================================================

        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]


        public async Task<IActionResult> UpdateSupplier(
            int id,
            [FromBody] SupplierEditRequest request)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "Invalid supplier ID."));
                }

                if (request == null || !ModelState.IsValid)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "Invalid supplier data."));
                }

                if (string.IsNullOrWhiteSpace(request.SupName))
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "Supplier name is required."));
                }

                // Get authenticated user.
                var userIdClaim =
                    User.FindFirst(
                        System.Security.Claims.ClaimTypes.NameIdentifier)
                    ?? User.FindFirst("userId")
                    ?? User.FindFirst("userid");

                if (userIdClaim == null ||
                    !int.TryParse(userIdClaim.Value, out int userId) ||
                    userId <= 0)
                {
                    return Unauthorized(
                        ApiResponseWithError.Failure(
                            "Unauthorized user."));
                }

                // RowVersion is required for concurrency protection.
                if (request.RowVersion == null ||
                    request.RowVersion.Length != 8)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "A valid RowVersion is required. Reload the supplier and try again."));
                }

                // Opening balance validation.
                if (request.OpenBal < 0)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "Opening balance cannot be negative."));
                }

                // Currency validation.
                if (request.CurCode <= 0)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "A valid currency is required."));
                }

                if (request.CurConvRate <= 0)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "Currency conversion rate must be greater than zero."));
                }

                // Debit / Credit validation.
                if (!string.Equals(request.CrDr, "Cr",
                        StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(request.CrDr, "Dr",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "Opening balance type must be Cr or Dr."));
                }

                // PendingBills must be supplied during update.
                // Send [] when there are no pending bills.
                if (request.PendingBills == null)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "PendingBills is required. Send [] when there are no pending bills."));
                }

                var pendingBillError = ValidatePendingBills(
                    request.PendingBills,
                    request.OpenBal,
                    request.CrDr,
                    requireList: true);

                if (pendingBillError != null)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(pendingBillError));
                }

                string supplierName = request.SupName.Trim();

                // Get existing supplier.
                var existing = await _supplierDB.GetSupplierById(id);

                if (existing == null)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "Supplier not found."));
                }

                // Check duplicate supplier name, excluding current supplier.
                bool nameExists = await _supplierDB.SupplierNameExists(
                    supplierName,
                    id);

                if (nameExists)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "Supplier name already exists."));
                }

                // Existing linked ledger account must be preserved.
                // Existing linked ledger account must be preserved.
                if (existing.AccountId <= 0)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "This supplier has no valid linked ledger account. Verify SUPMAS.ACCID before updating."));
                }

                // Use the request directly. No MapToSupplier().
                request.SupName = supplierName;

                // Normalize CrDr.
                request.CrDr = string.Equals(
                    request.CrDr,
                    "Cr",
                    StringComparison.OrdinalIgnoreCase)
                        ? "Cr"
                        : "Dr";

                // Update supplier and linked account.
                bool updated = await _supplierDB.UpdateSupplier(
                    id,
                    request,
                    request.RowVersion,
                    userId);

                if (!updated)
                {
                    var current = await _supplierDB.GetSupplierById(id);

                    if (current == null)
                    {
                        return BadRequest(
                            ApiResponseWithError.Failure(
                                "Supplier not found or deleted."));
                    }

                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "Supplier was modified by another user. Reload and try again."));
                }

                return Ok(
                    ApiResponseWithoutPagination<object>.Success(
                        "Supplier and linked ledger account updated successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(
                    ApiResponseWithError.Failure(ex.Message));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Supplier update failed: {ex}");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseWithError.Failure(
                        "Failed to update supplier."));
            }
        }

        private static string? ValidatePendingBills(
    List<PendingBillRequest>? pendingBills,
    decimal openingBalance,
    string openingDrCr,
    bool requireList)
        {
            if (pendingBills == null)
            {
                return requireList
                    ? "PendingBills is required. Send [] when there are no pending bills."
                    : null;
            }

            if (openingBalance < 0)
                return "Opening balance cannot be negative.";

            openingDrCr = openingDrCr.Trim().ToUpperInvariant();

            if (openingDrCr != "DR" && openingDrCr != "CR")
                return "Opening balance type must be DR or CR.";

            decimal pendingDebit = 0m;
            decimal pendingCredit = 0m;

            for (int i = 0; i < pendingBills.Count; i++)
            {
                var bill = pendingBills[i];

                if (bill == null)
                    return $"Invalid pending bill at position {i + 1}.";

                if (string.IsNullOrWhiteSpace(bill.InvoiceNumber))
                    return $"Invoice number is required for pending bill {i + 1}.";

                if (bill.Amount <= 0)
                    return $"Amount must be greater than zero for pending bill {i + 1}.";

                string drCr = bill.DrCr?.Trim().ToUpperInvariant() ?? "";

                if (drCr != "DR" && drCr != "CR")
                    return $"Invalid DrCr for pending bill {i + 1}. It must be DR or CR.";

                if (bill.DueDate.Date < bill.InvoiceDate.Date)
                    return $"Due date cannot be earlier than invoice date for pending bill {i + 1}.";

                if (drCr == "DR")
                    pendingDebit += bill.Amount;
                else
                    pendingCredit += bill.Amount;
            }

            decimal openingDebit = openingDrCr == "DR" ? openingBalance : 0m;
            decimal openingCredit = openingDrCr == "CR" ? openingBalance : 0m;

            if (pendingDebit > openingDebit)
                return "Total pending debit bills cannot exceed the opening debit balance.";

            if (pendingCredit > openingCredit)
                return "Total pending credit bills cannot exceed the opening credit balance.";

            return null;
        }


        private static string? ValidateSupplierFinancials(
    SupplierRequest request,
    bool requirePendingBills)
        {
            if (request.OpenBal < 0)
                return "Opening balance cannot be negative.";

            // Currency validation is required only for multicurrency suppliers.
            if (request.CurFlg)
            {
                if (request.CurCode <= 0)
                    return "A valid currency is required when multicurrency is enabled.";

                if (request.CurConvRate <= 0)
                    return "Currency conversion rate must be greater than zero.";
            }

            string openingDrCr = request.CrDr?.Trim().ToUpperInvariant() ?? "";

            if (openingDrCr != "DR" && openingDrCr != "CR")
                return "Opening balance type must be DR or CR.";

            return ValidatePendingBills(
                request.PendingBills,
                request.OpenBal,
                openingDrCr,
                requirePendingBills);
        }
        //    private static string? ValidatePendingBills(
        //List<PendingBillRequest>? pendingBills,
        //decimal openingBalance,
        //bool requireList)
        //    {
        //        if (pendingBills == null)
        //        {
        //            return requireList
        //                ? "PendingBills is required. Send [] when there are no pending bills."
        //                : null;
        //        }

        //        decimal totalPendingBalance = 0m;

        //        for (int i = 0; i < pendingBills.Count; i++)
        //        {
        //            var bill = pendingBills[i];

        //            if (string.IsNullOrWhiteSpace(bill.InvNo))
        //            {
        //                return $"Invoice number is required for pending bill {i + 1}.";
        //            }

        //            if (bill.Amount <= 0)
        //            {
        //                return $"Amount must be greater than zero for pending bill {i + 1}.";
        //            }

        //            if (!string.Equals(bill.DrCr, "Cr",
        //                    StringComparison.OrdinalIgnoreCase) &&
        //                !string.Equals(bill.DrCr, "Dr",
        //                    StringComparison.OrdinalIgnoreCase))
        //            {
        //                return $"Invalid DrCr for pending bill {i + 1}. It must be Cr or Dr.";
        //            }

        //            if (bill.DueDate.Date < bill.InvDate.Date)
        //            {
        //                return $"Due date cannot be earlier than invoice date for pending bill {i + 1}.";
        //            }

        //            totalPendingBalance += bill.Amount;
        //        }

        //        if (totalPendingBalance > openingBalance)
        //        {
        //            return "Total pending bills balance cannot exceed the opening balance.";
        //        }

        //        return null;
        //    }

        // =========================================================
        // DELETE SUPPLIER
        // DELETE: api/v1/suppliers/1
        // =========================================================
        // =========================================================
        // DELETE SUPPLIER
        // DELETE: api/v1/suppliers/1
        // =========================================================

        //[HttpDelete("{id:int}")]
        //[ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        //[ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]

        //public async Task<IActionResult> DeleteSupplier(int id)
        //{
        //    try
        //    {
        //        // Validate supplier ID.
        //        if (id <= 0)
        //        {
        //            return BadRequest(
        //                ApiResponseWithError.Failure(
        //                    "Invalid supplier ID."));
        //        }

        //        // Get authenticated user.
        //        var userIdClaim =
        //            User.FindFirst(
        //                System.Security.Claims.ClaimTypes.NameIdentifier)
        //            ?? User.FindFirst("userId")
        //            ?? User.FindFirst("userid");

        //        if (userIdClaim == null ||
        //            !int.TryParse(userIdClaim.Value, out int userId) ||
        //            userId <= 0)
        //        {
        //            return Unauthorized(
        //                ApiResponseWithError.Failure(
        //                    "Unauthorized user."));
        //        }

        //        // Check whether supplier exists.
        //        var existing = await _supplierDB.GetSupplierById(id);

        //        if (existing == null)
        //        {
        //            return BadRequest(
        //                ApiResponseWithError.Failure(
        //                    "Supplier not found."));
        //        }

        //        // Delete supplier using database method.
        //        var deleted = await _supplierDB.DeleteSupplierWithValidations(id);

        //        if (deleted.NotFound)
        //        {
        //            return BadRequest(
        //                ApiResponseWithError.Failure(
        //                    "Supplier not found or already deleted."));
        //        }

        //        return Ok(
        //            ApiResponseWithoutPagination<string>.Success(
        //                "Supplier deleted successfully."));
        //    }
        //    catch (InvalidOperationException ex)
        //    {
        //        return Conflict(
        //            ApiResponseWithError.Failure(ex.Message));
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Supplier deletion failed: {ex}");

        //        return StatusCode(
        //            StatusCodes.Status500InternalServerError,
        //            ApiResponseWithError.Failure(
        //                "Failed to delete supplier."));
        //    }
        //}



        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithoutPagination<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            try
            {
                // 1. Validate supplier ID.
                if (id <= 0)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "Invalid supplier ID."));
                }

                // 2. Validate authenticated user.
                var userIdClaim =
                    User.FindFirst(
                        System.Security.Claims.ClaimTypes.NameIdentifier)
                    ?? User.FindFirst("userId")
                    ?? User.FindFirst("userid");

                if (userIdClaim == null ||
                    !int.TryParse(userIdClaim.Value, out int userId) ||
                    userId <= 0)
                {
                    return Unauthorized(
                        ApiResponseWithError.Failure(
                            "Unauthorized user."));
                }
                var existing = await _supplierDB.GetSupplierByIdExist(id);

                if (existing == null || existing <= 0)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            "Supplier not found."));
                }
                // 3. Delete supplier with validations.
                var deleted =
                    await _supplierDB.DeleteSupplierWithValidations(
                        id, userId.ToString());

                // 4. Supplier not found.
                if (deleted.NotFound)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            deleted.Message ?? "Supplier not found."));
                }

                // 5. Validation failed.
                if (!deleted.Success)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure(
                            deleted.Message ?? "Supplier cannot be deleted."));
                }

                // 6. Successfully deleted.
                return Ok(
                    ApiResponseWithoutPagination<string>.Success(
                        "Supplier deleted successfully."));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Supplier deletion failed: {ex}");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseWithError.Failure(
                        "Failed to delete supplier."));
            }
        }

        //===========================================
        // GET: api/v1/suppliers/lookups/ledger-groups
        //===========================================

        [HttpGet("lookups/ledger-groups")]

        [ProducesResponseType(typeof(ApiResponseWithoutPagination<List<LookupItem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetLedgerGroups()
        {
            try
            {
                var data = await _supplierDB.GetLedgerGroups();

                return Ok(
                    ApiResponseWithoutPagination<List<LookupItem>>.Success(
                        "Ledger groups retrieved successfully.",
                        data
                    )
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseWithError.Failure(
                        "Failed to retrieve ledger groups."
                    )
                );
            }
        }



        //private static clsSupplier MapToSupplier(int Logcd, SupplierRequest request)
        //{
        //    return new clsSupplier
        //    {
        //        Alias = request.Alias,
        //        SupName = request.SupName,
        //        Address = request.Address,
        //        Phone = request.Phone,
        //        Email = request.Email,
        //        Active = request.Active,
        //        LogId = Logcd,
        //        LedgerGroupId = request.LedgerGroupId,
        //        //AccountId = request.AccountId,
        //        //Disc = request.Disc,
        //        StateCode = request.StateCode,
        //        PrintName = request.PrintName,
        //        CountryCode = request.CountryCode,
        //        //PriceLevel = request.PriceLevel,
        //        BnkAccName = request.BnkAccName,
        //        BnkAccNo = request.BnkAccNo,
        //        Branch = request.Branch,
        //        IbanNo = request.IbanNo,
        //        VatFlag = request.VatFlag,
        //        BillFlag = request.BillFlag,
        //        CrPerFlag = request.CrPerFlag,
        //        CrLimitFlag = request.CrLimitFlag,
        //        CrLimit = request.CrLimit,
        //        VatRegNo = request.VatRegNo,
        //        ShipAddress = request.ShipAddress,
        //        SecName = request.SecName,
        //        CurFlg = request.CurFlg,
        //        CrPeriod = request.CrPeriod,
        //        //GrpItem = request.GrpItem,
        //        //ItemGroupCD = request.ItemGroupCD,
        //        GccFlg = request.GccFlg,
        //        StreetOne = request.StreetOne,
        //        StreetTwo = request.StreetTwo,
        //        BuildingNo = request.BuildingNo,
        //        PlotId = request.PlotId,
        //        CityName = request.CityName,
        //        PostalCode = request.PostalCode,
        //        District = request.District,
        //        GrpVatNo = request.GrpVatNo,
        //        EinvIdType = request.EinvIdType,
        //        EinvId = request.EinvId,
        //        VatType = request.VatType,
        //        PartyGrpId = request.PartyGrpId,
        //        //CityNameSec = request.CityNameSec,
        //        //DistrictSec = request.DistrictSec,
        //        OpenBal = request.OpenBal,
        //        CrDr = request.CrDr,
        //        CurCode = request.CurCode,
        //        CurConvRate = request.CurConvRate <= 0 ? 1m : request.CurConvRate,
        //        PendingBills = request.PendingBills,
        //        SwiftCode = request.SwiftCode

        //    };
        //}


        // GET: api/v1/suppliers/lookups/supplier-groups
        //[HttpGet("lookups/supplier-groups")]
        //public async Task<IActionResult> GetSupplierGroups()
        //{
        //    try
        //    {
        //        var data = await _supplierDB.GetSupplierGroups();

        //        return Ok(new
        //        {
        //            status = true,
        //            data
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            message = "Failed to retrieve supplier groups.",
        //            error = ex.Message
        //        });
        //    }
        //}

        // GET: api/v1/suppliers/lookups/item-groups
        //[HttpGet("lookups/item-groups")]
        //public async Task<IActionResult> GetItemGroups()
        //{
        //    try
        //    {
        //        var data = await _supplierDB.GetItemGroups();

        //        return Ok(new
        //        {
        //            status = true,
        //            data
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            message = "Failed to retrieve item groups.",
        //            error = ex.Message
        //        });
        //    }
        //}

        // GET: api/v1/suppliers/lookups/currencies

        //[HttpGet("lookups/currencies")]
        //public async Task<IActionResult> GetCurrencies()
        //{
        //    try
        //    {
        //        var table = await _supplierDB.GetCurrencies();
        //        var data = ConvertDataTable(table);

        //        return Ok(new
        //        {
        //            status = true,
        //            message = "Currencies retrieved successfully.",
        //            data
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            message = "Failed to retrieve currencies.",
        //            error = ex.Message
        //        });
        //    }
        //}
        //[HttpGet("lookups/vat-types")]
        //public async Task<IActionResult> GetVatTypes()
        //{
        //    try
        //    {
        //        var data = await _supplierDB.GetVatTypes();

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
        //            message = "Failed to retrieve VAT types.",
        //            error = ex.Message
        //        });
        //    }
        //}

        //[HttpGet("lookups/id-types")]
        //public async Task<IActionResult> GetIdTypes()
        //{
        //    try
        //    {
        //        var data = await _supplierDB.GetIdTypes();

        //        return Ok(new
        //        {
        //            status = true,
        //            message = "ID types retrieved successfully.",
        //            data
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            message = "Failed to retrieve ID types.",
        //            error = ex.Message
        //        });
        //    }
        //}


        // =========================================================
        // GET: api/v1/suppliers/form-options
        // Loads all initial Supplier form dropdown options.
        // =========================================================


        //[HttpGet("form-options")]
        //public async Task<IActionResult> GetSupplierFormOptions()
        //{
        //    try
        //    {
        //        // Use the tenant claim validated by Program.cs.
        //        var tenantId = User.FindFirst("tenant_id")?.Value;

        //        if (string.IsNullOrWhiteSpace(tenantId))
        //        {
        //            return Unauthorized(new
        //            {
        //                status = false,
        //                message = "Unable to identify the authenticated tenant."
        //            });
        //        }

        //        string cacheKey = $"SupplierFormOptions:{tenantId}";

        //        CachedFormOptions? cached;

        //        // Check the server cache before querying the databases.
        //        if (!_memoryCache.TryGetValue(cacheKey, out cached)
        //            || cached == null)
        //        {
        //            // Countries are read from the master database.
        //            var variables = new Variable();
        //            var databaseConnection = new DatabaseConnection();

        //            string connectionString =
        //                databaseConnection.GetConnectionString(
        //                    variables,
        //                    isMasterDB: true
        //                );

        //            var masterDb = new DBConn(connectionString);
        //            var masterSupplierDb = new SupplierDB(masterDb);

        //            // Keep database calls sequential.
        //            var countriesTable =
        //                await masterSupplierDb.GetCountries();

        //            var ledgerGroups =
        //                await _supplierDB.GetLedgerGroups();

        //            var supplierGroups =
        //                await _supplierDB.GetSupplierGroups();

        //            var itemGroups =
        //                await _supplierDB.GetItemGroups();

        //            var vatTypes =
        //                await _supplierDB.GetVatTypes();

        //            var idTypes =
        //                await _supplierDB.GetIdTypes();

        //            var currenciesTable =
        //                await _supplierDB.GetCurrencies();

        //            var response = new
        //            {
        //                status = true,
        //                data = new
        //                {
        //                    countries = ConvertDataTable(countriesTable),
        //                    ledgerGroups,
        //                    supplierGroups,
        //                    itemGroups,
        //                    vatTypes,
        //                    idTypes,
        //                    currencies = ConvertDataTable(currenciesTable)
        //                }
        //            };

        //            string json = JsonSerializer.Serialize(response);

        //            byte[] responseBytes = Encoding.UTF8.GetBytes(json);

        //            string etag = "\"" + Convert.ToHexString(
        //                SHA256.HashData(responseBytes)
        //            ) + "\"";

        //            cached = new CachedFormOptions(json, etag);

        //            // Cache the full JSON response and ETag for five minutes.
        //            _memoryCache.Set(
        //                cacheKey,
        //                cached,
        //                new MemoryCacheEntryOptions
        //                {
        //                    AbsoluteExpirationRelativeToNow =
        //                        TimeSpan.FromMinutes(5)
        //                });
        //        }

        //        // Apply HTTP cache headers for both cache hits and misses.
        //        Response.Headers["ETag"] = cached.ETag;
        //        Response.Headers["Cache-Control"] = "private, max-age=300";

        //        // Return 304 if the client already has this version.
        //        string ifNoneMatch = Request.Headers.IfNoneMatch.ToString();

        //        bool etagMatches = ifNoneMatch
        //            .Split(',')
        //            .Select(value => value.Trim())
        //            .Any(value =>
        //                value == "*" ||
        //                value == cached.ETag ||
        //                (value.StartsWith(
        //                    "W/",
        //                    StringComparison.Ordinal
        //                ) && value.Substring(2) == cached.ETag));

        //        if (etagMatches)
        //        {
        //            return StatusCode(StatusCodes.Status304NotModified);
        //        }

        //        // Return the cached JSON without querying the databases again.
        //        return new ContentResult
        //        {
        //            StatusCode = StatusCodes.Status200OK,
        //            Content = cached.Json,
        //            ContentType = "application/json; charset=utf-8"
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Supplier form options failed: {ex}");

        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            message = "Failed to retrieve supplier form options."
        //        });
        //    }
        //}
        private sealed record CachedFormOptions(string Json, string ETag);
    }
}


//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Http.HttpResults;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Caching.Memory;
//using QuickBalance.API.CLS;
//using QuickBalance.API.Configuration;
//using QuickBalance.API.DB;
//using QuickBalance.API.DTOs;
//using System.Data;
//using System.Security.Cryptography;
//using System.Text;
//using System.Text.Json;


//namespace QuickBalance.API.Controllers.Master
//{
//    [Route("api/v1/suppliers")]
//    [ApiController]
//    [Authorize]
//    public class SupplierController : ControllerBase
//    {
//        private readonly SupplierDB _supplierDB;
//        private readonly IMemoryCache _memoryCache;

//        public SupplierController(SupplierDB supplierDB, IMemoryCache memoryCache)
//        {
//            _supplierDB = supplierDB;
//            _memoryCache = memoryCache;
//        }
//        // =========================================================
//        // MAP REQUEST DTO TO CLS
//        // =========================================================

//        private static clsSupplier MapToSupplier(SupplierRequest request)
//        {
//            return new clsSupplier
//            {
//                Alias = request.Alias,
//                SupName = request.SupName,
//                Address = request.Address,
//                Phone = request.Phone,
//                Email = request.Email,
//                Active = request.Active,
//                Logcd = request.Logcd,
//                //AccId = request.AccId,
//                Disc = request.Disc,
//                StateCode = request.StateCode,
//                PrintName = request.PrintName,
//                CountryCode = request.CountryCode,
//                PriceLevel = request.PriceLevel,
//                BnkAccName = request.BnkAccName,
//                BnkAccNo = request.BnkAccNo,
//                Branch = request.Branch,
//                IbanNo = request.IbanNo,
//                VatFlag = request.VatFlag,
//                BillFlag = request.BillFlag,
//                CrPerFlag = request.CrPerFlag,
//                CrLimitFlag = request.CrLimitFlag,
//                CrLimit = request.CrLimit,
//                VatRegNo = request.VatRegNo,
//                ShipAddress = request.ShipAddress,
//                SecName = request.SecName,
//                CurFlg = request.CurFlg,
//                CrPeriod = request.CrPeriod,
//                GrpItem = request.GrpItem,
//                ItemGroupCD = request.ItemGroupCD,
//                GccFlg = request.GccFlg,
//                StreetOne = request.StreetOne,
//                StreetTwo = request.StreetTwo,
//                BuildingNo = request.BuildingNo,
//                PlotId = request.PlotId,
//                CityName = request.CityName,
//                PostalCode = request.PostalCode,
//                District = request.District,
//                GrpVatNo = request.GrpVatNo,
//                EinvIdType = request.EinvIdType,
//                EinvId = request.EinvId,
//                VatType = request.VatType,
//                PartyGrpId = request.PartyGrpId,
//                CityNameSec = request.CityNameSec,
//                DistrictSec = request.DistrictSec
//            };
//        }

//        // =========================================================
//        // GET ALL SUPPLIERS
//        // GET: api/v1/suppliers?page=1&pageSize=10&search=
//        // =========================================================

//        [HttpGet]
//        public async Task<IActionResult> GetSuppliers(
//            [FromQuery] int page = 1,
//            [FromQuery] int pageSize = 10,
//            [FromQuery] string? search = null)
//        {
//            try
//            {
//                if (page < 1)
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Page must be greater than 0."
//                    });

//                if (pageSize < 1 || pageSize > 500)
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Page size must be between 1 and 500."
//                    });

//                var result = await _supplierDB.GetSuppliers(
//                    page,
//                    pageSize,
//                    search
//                );

//                int totalPages = (int)Math.Ceiling(
//                    result.TotalRecords / (double)pageSize
//                );

//                return Ok(new
//                {
//                    status = true,
//                    data = result.Data,
//                    pagination = new
//                    {
//                        page,
//                        pageSize,
//                        totalRecords = result.TotalRecords,
//                        totalPages
//                    }
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = "Failed to retrieve suppliers.",
//                    error = ex.Message
//                });
//            }
//        }

//        // =========================================================
//        // GET SUPPLIER BY ID
//        // GET: api/v1/suppliers/1
//        // =========================================================

//        [HttpGet("{id:int}")]
//        public async Task<IActionResult> GetSupplierById(int id)
//        {
//            try
//            {
//                if (id <= 0)
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Invalid supplier ID."
//                    });

//                var supplier = await _supplierDB.GetSupplierById(id);

//                if (supplier == null)
//                    return NotFound(new
//                    {
//                        status = false,
//                        message = "Supplier not found."
//                    });

//                return Ok(new
//                {
//                    status = true,
//                    data = supplier
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = "Failed to retrieve supplier.",
//                    error = ex.Message
//                });
//            }
//        }

//        // =========================================================
//        // CREATE SUPPLIER
//        // POST: api/v1/suppliers
//        // =========================================================

//        //[HttpPost]
//        //public async Task<IActionResult> CreateSupplier(
//        //    [FromBody] SupplierRequest request)
//        //{
//        //    try
//        //    {
//        //        if (string.IsNullOrWhiteSpace(request.SupName))
//        //            return BadRequest(new
//        //            {
//        //                status = false,
//        //                message = "Supplier name is required."
//        //            });

//        //        bool exists = await _supplierDB.SupplierNameExists(
//        //            request.SupName
//        //        );

//        //        if (exists)
//        //            return Conflict(new
//        //            {
//        //                status = false,
//        //                message = "Supplier name already exists."
//        //            });

//        //        var supplier = MapToSupplier(request);

//        //        int id = await _supplierDB.InsertSupplier(supplier);

//        //        if (id <= 0)
//        //            return StatusCode(500, new
//        //            {
//        //                status = false,
//        //                message = "Supplier creation failed."
//        //            });

//        //        return CreatedAtAction(
//        //            nameof(GetSupplierById),
//        //            new { id },
//        //            new
//        //            {
//        //                status = true,
//        //                message = "Supplier created successfully.",
//        //                id
//        //            }
//        //        );
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        return StatusCode(500, new
//        //        {
//        //            status = false,
//        //            message = "Failed to create supplier.",
//        //            error = ex.Message
//        //        });
//        //    }
//        //}


//        [HttpPost]
//        public async Task<IActionResult> CreateSupplier([FromBody] SupplierRequest request)
//        {
//            try
//            {
//                if (!ModelState.IsValid)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Invalid supplier data.",
//                        errors = ModelState
//                    });
//                }

//                if (string.IsNullOrWhiteSpace(request.SupName))
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Supplier name is required."
//                    });
//                }

//                string supplierName = request.SupName.Trim();

//                bool exists = await _supplierDB.SupplierNameExists(
//                    supplierName);

//                if (exists)
//                {
//                    return Conflict(new
//                    {
//                        status = false,
//                        message = "Supplier name already exists."
//                    });
//                }

//                var ledgerGroup =
//                    await _supplierDB.GetDefaultSupplierLedgerGroup(request.LedgerGroupId);

//                if (ledgerGroup == null ||
//                    string.IsNullOrWhiteSpace(ledgerGroup.GroupCode))
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Supplier ledger group was not found."
//                    });
//                }

//                var supplier = MapToSupplier(request);
//                supplier.SupName = supplierName;

//                var result = await _supplierDB.InsertSupplier(
//                    supplier,
//                    ledgerGroup.GroupId,
//                    ledgerGroup.GroupCode);

//                if (result.SupplierId <= 0)
//                {
//                    return StatusCode(500, new
//                    {
//                        status = false,
//                        message = "Supplier creation failed."
//                    });
//                }

//                return Ok(new
//                {
//                    status = true,
//                    message = "Supplier and account created successfully.",
//                    data = new
//                    {
//                        id = result.SupplierId,
//                        accountId = result.AccountId,
//                        accountCode = result.AccountCode
//                    }
//                });

//                //return CreatedAtAction(
//                //    nameof(GetSupplierById),
//                //    new { id = supplierId },
//                //    new
//                //    {
//                //        status = true,
//                //        message = "Supplier and account created successfully.",
//                //        data = new
//                //        {
//                //            id = supplierId,
//                //            accountId = supplier.AccId,
//                //            accountCode = result.
//                //                ledgerGroup.GroupId,
//                //                ledgerGroup.GroupCode,
//                //                supplierId)
//                //        }
//                //    });
//            }
//            catch (Exception ex)
//            {
//                // Log ex with ILogger in production.
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = "Failed to create supplier.",
//                    error = ex.Message
//                });
//            }
//        }
//        // =========================================================
//        // UPDATE SUPPLIER
//        // PUT: api/v1/suppliers/1
//        // =========================================================

//        [HttpPut("{id:int}")]
//        public async Task<IActionResult> UpdateSupplier(
//            int id,
//            [FromBody] SupplierRequest request)
//        {
//            try
//            {
//                if (id <= 0)
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Invalid supplier ID."
//                    });

//                if (string.IsNullOrWhiteSpace(request.SupName))
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Supplier name is required."
//                    });

//                var existing = await _supplierDB.GetSupplierById(id);

//                if (existing == null)
//                    return NotFound(new
//                    {
//                        status = false,
//                        message = "Supplier not found."
//                    });

//                bool exists = await _supplierDB.SupplierNameExists(
//                    request.SupName,
//                    id
//                );

//                if (exists)
//                    return Conflict(new
//                    {
//                        status = false,
//                        message = "Supplier name already exists."
//                    });

//                var supplier = MapToSupplier(request);
//                supplier.Id = id;

//                bool updated = await _supplierDB.UpdateSupplier(
//                    id,
//                    supplier
//                );

//                if (!updated)
//                    return NotFound(new
//                    {
//                        status = false,
//                        message = "Supplier not found or not updated."
//                    });

//                return Ok(new
//                {
//                    status = true,
//                    message = "Supplier updated successfully.",
//                    id
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = "Failed to update supplier.",
//                    error = ex.Message
//                });
//            }
//        }

//        // =========================================================
//        // DELETE SUPPLIER
//        // DELETE: api/v1/suppliers/1
//        // =========================================================

//        [HttpDelete("{id:int}")]
//        public async Task<IActionResult> DeleteSupplier(int id)
//        {
//            try
//            {
//                if (id <= 0)
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Invalid supplier ID."
//                    });

//                var existing = await _supplierDB.GetSupplierById(id);

//                if (existing == null)
//                    return NotFound(new
//                    {
//                        status = false,
//                        message = "Supplier not found."
//                    });

//                bool deleted = await _supplierDB.DeleteSupplier(id);

//                if (!deleted)
//                    return NotFound(new
//                    {
//                        status = false,
//                        message = "Supplier not found or already deleted."
//                    });

//                return Ok(new
//                {
//                    status = true,
//                    message = "Supplier deleted successfully.",
//                    id
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = "Failed to delete supplier.",
//                    error = ex.Message
//                });
//            }
//        }


//        // =========================================================
//        // SUPPLIER MASTER DROPDOWN / LOOKUP APIs
//        // =========================================================
//        //[FromQuery]
//        //PaginationRequest request
//        // GET: api/v1/suppliers/lookups/countries
//        [HttpGet("lookups/countries")]
//        public async Task<IActionResult> GetCountries()
//        {
//            try
//            {
//                Variable Variables = new Variable();
//                DatabaseConnection databaseConnection = new DatabaseConnection();

//                string connectionString = databaseConnection.GetConnectionString(
//                    Variables,
//                    isMasterDB: true
//                );
//                //request.Normalize();
//                DBConn db = new DBConn(connectionString);
//                SupplierDB supplierDB = new SupplierDB(db);

//                DataTable dt = await supplierDB.GetCountries();
//                int totalRecords = await supplierDB.GetCountriesCount();

//                var tableData = dt.AsEnumerable()
//                    .Select(row => dt.Columns.Cast<DataColumn>()
//                        .ToDictionary(
//                            column => column.ColumnName,
//                            column => row[column] == DBNull.Value ? null : row[column]
//                        ))
//                    .ToList();
//                //int totalPages = (int)Math.Ceiling(totalRecords / (double)request.PageSize);

//                return Ok(new
//                {
//                    status = true,
//                    message = "Countries retrieved successfully.",
//                    data = tableData,
//                    //pagination = new
//                    //{
//                    //    page = request.Page,
//                    //    pageSize = request.PageSize,
//                    //    totalRecords,
//                    //    totalPages
//                    //}
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = "Failed to retrieve countries.",
//                    error = ex.Message
//                });
//            }
//        }

//        // GET: api/v1/suppliers/lookups/regions?countryId=1
//        [HttpGet("lookups/regions")]
//        public async Task<IActionResult> GetRegions([FromQuery] int countryId)
//        {
//            if (countryId <= 0)
//            {
//                return BadRequest(new
//                {
//                    status = false,
//                    message = "Valid country ID is required."
//                });
//            }

//            try
//            {
//                Variable Variables = new Variable();
//                DatabaseConnection databaseConnection = new DatabaseConnection();

//                string connectionString = databaseConnection.GetConnectionString(
//                    Variables,
//                    isMasterDB: true
//                );

//                DBConn db = new DBConn(connectionString);
//                SupplierDB supplierDB = new SupplierDB(db);

//                DataTable dt = await supplierDB.GetRegions(countryId);

//                var tableData = dt.AsEnumerable()
//                    .Select(row => dt.Columns.Cast<DataColumn>()
//                        .ToDictionary(
//                            column => column.ColumnName,
//                            column => row[column] == DBNull.Value ? null : row[column]
//                        ))
//                    .ToList();

//                return Ok(new
//                {
//                    status = true,
//                    data = tableData
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = "Failed to retrieve regions.",
//                    error = ex.Message
//                });
//            }
//        }


//        private static List<Dictionary<string, object?>> ConvertDataTable(DataTable table)
//        {
//            var result = new List<Dictionary<string, object?>>();

//            foreach (DataRow row in table.Rows)
//            {
//                var item = new Dictionary<string, object?>();

//                foreach (DataColumn column in table.Columns)
//                {
//                    item[column.ColumnName] =
//                        row[column] == DBNull.Value ? null : row[column];
//                }

//                result.Add(item);
//            }

//            return result; 
//        }
//        //===========================================
//        // GET: api/v1/suppliers/lookups/ledger-groups
//        //===========================================
//        [HttpGet("lookups/ledger-groups")]
//        public async Task<IActionResult> GetLedgerGroups([FromQuery] PaginationRequest request)
//        {
//            try
//            {
//                request.Normalize();

//                var result = await _supplierDB.GetLedgerGroups(request.Page,request.PageSize);

//                int totalPages = (int)Math.Ceiling(
//                    result.TotalRecords / (double)request.PageSize
//                );

//                return Ok(new
//                {
//                    status = true,
//                    data = result.Data,
//                    pagination = new
//                    {
//                        page = request.Page,
//                        pageSize = request.PageSize,
//                        totalRecords = result.TotalRecords,
//                        totalPages
//                    }
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = "Failed to retrieve ledger groups.",
//                    error = ex.Message
//                });
//            }
//        }

//        // GET: api/v1/suppliers/lookups/supplier-groups
//        [HttpGet("lookups/supplier-groups")]
//        public async Task<IActionResult> GetSupplierGroups()
//        {
//            try
//            {
//                var data = await _supplierDB.GetSupplierGroups();

//                return Ok(new
//                {
//                    status = true,
//                    data
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = "Failed to retrieve supplier groups.",
//                    error = ex.Message
//                });
//            }
//        }

//        // GET: api/v1/suppliers/lookups/item-groups
//        [HttpGet("lookups/item-groups")]
//        public async Task<IActionResult> GetItemGroups()
//        {
//            try
//            {
//                var data = await _supplierDB.GetItemGroups();

//                return Ok(new
//                {
//                    status = true,
//                    data
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = "Failed to retrieve item groups.",
//                    error = ex.Message
//                });
//            }
//        }

//        // GET: api/v1/suppliers/lookups/currencies

//        [HttpGet("lookups/currencies")]
//        public async Task<IActionResult> GetCurrencies()
//        {
//            try
//            {
//                var table = await _supplierDB.GetCurrencies();
//                var data = ConvertDataTable(table);

//                return Ok(new
//                {
//                    status = true,
//                    message = "Currencies retrieved successfully.",
//                    data
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = "Failed to retrieve currencies.",
//                    error = ex.Message
//                });
//            }
//        }
//        [HttpGet("lookups/vat-types")]
//        public async Task<IActionResult> GetVatTypes()
//        {
//            try
//            {
//                var data = await _supplierDB.GetVatTypes();

//                return Ok(new
//                {
//                    status = true,
//                    message = "VAT types retrieved successfully.",
//                    data
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = "Failed to retrieve VAT types.",
//                    error = ex.Message
//                });
//            }
//        }

//        [HttpGet("lookups/id-types")]
//        public async Task<IActionResult> GetIdTypes()
//        {
//            try
//            {
//                var data = await _supplierDB.GetIdTypes();

//                return Ok(new
//                {
//                    status = true,
//                    message = "ID types retrieved successfully.",
//                    data
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = "Failed to retrieve ID types.",
//                    error = ex.Message
//                });
//            }
//        }


//        // =========================================================
//        // GET: api/v1/suppliers/form-options
//        // Loads all initial Supplier form dropdown options.
//        // =========================================================


//        [HttpGet("form-options")]
//        public async Task<IActionResult> GetSupplierFormOptions()
//        {
//            try
//            {
//                // Use the tenant claim validated by Program.cs.
//                var tenantId = User.FindFirst("tenant_id")?.Value;

//                if (string.IsNullOrWhiteSpace(tenantId))
//                {
//                    return Unauthorized(new
//                    {
//                        status = false,
//                        message = "Unable to identify the authenticated tenant."
//                    });
//                }

//                string cacheKey = $"SupplierFormOptions:{tenantId}";

//                CachedFormOptions? cached;

//                // Check the server cache before querying the databases.
//                if (!_memoryCache.TryGetValue(cacheKey, out cached)
//                    || cached == null)
//                {
//                    // Countries are read from the master database.
//                    var variables = new Variable();
//                    var databaseConnection = new DatabaseConnection();

//                    string connectionString =
//                        databaseConnection.GetConnectionString(
//                            variables,
//                            isMasterDB: true
//                        );

//                    var masterDb = new DBConn(connectionString);
//                    var masterSupplierDb = new SupplierDB(masterDb);

//                    // Keep database calls sequential.
//                    var countriesTable =
//                        await masterSupplierDb.GetCountries();

//                    var ledgerGroups =
//                        await _supplierDB.GetLedgerGroups();

//                    var supplierGroups =
//                        await _supplierDB.GetSupplierGroups();

//                    var itemGroups =
//                        await _supplierDB.GetItemGroups();

//                    var vatTypes =
//                        await _supplierDB.GetVatTypes();

//                    var idTypes =
//                        await _supplierDB.GetIdTypes();

//                    var currenciesTable =
//                        await _supplierDB.GetCurrencies();

//                    var response = new
//                    {
//                        status = true,
//                        data = new
//                        {
//                            countries = ConvertDataTable(countriesTable),
//                            ledgerGroups,
//                            supplierGroups,
//                            itemGroups,
//                            vatTypes,
//                            idTypes,
//                            currencies = ConvertDataTable(currenciesTable)
//                        }
//                    };

//                    string json = JsonSerializer.Serialize(response);

//                    byte[] responseBytes = Encoding.UTF8.GetBytes(json);

//                    string etag = "\"" + Convert.ToHexString(
//                        SHA256.HashData(responseBytes)
//                    ) + "\"";

//                    cached = new CachedFormOptions(json, etag);

//                    // Cache the full JSON response and ETag for five minutes.
//                    _memoryCache.Set(
//                        cacheKey,
//                        cached,
//                        new MemoryCacheEntryOptions
//                        {
//                            AbsoluteExpirationRelativeToNow =
//                                TimeSpan.FromMinutes(5)
//                        });
//                }

//                // Apply HTTP cache headers for both cache hits and misses.
//                Response.Headers["ETag"] = cached.ETag;
//                Response.Headers["Cache-Control"] = "private, max-age=300";

//                // Return 304 if the client already has this version.
//                string ifNoneMatch = Request.Headers.IfNoneMatch.ToString();

//                bool etagMatches = ifNoneMatch
//                    .Split(',')
//                    .Select(value => value.Trim())
//                    .Any(value =>
//                        value == "*" ||
//                        value == cached.ETag ||
//                        (value.StartsWith(
//                            "W/",
//                            StringComparison.Ordinal
//                        ) && value.Substring(2) == cached.ETag));

//                if (etagMatches)
//                {
//                    return StatusCode(StatusCodes.Status304NotModified);
//                }

//                // Return the cached JSON without querying the databases again.
//                return new ContentResult
//                {
//                    StatusCode = StatusCodes.Status200OK,
//                    Content = cached.Json,
//                    ContentType = "application/json; charset=utf-8"
//                };
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Supplier form options failed: {ex}");

//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = "Failed to retrieve supplier form options."
//                });
//            }
//        }
//        private sealed record CachedFormOptions(string Json, string ETag);
//    }
//}   
