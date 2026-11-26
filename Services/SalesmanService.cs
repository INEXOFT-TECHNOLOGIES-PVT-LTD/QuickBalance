using Microsoft.Identity.Client;
using QuickBalance.API.CLS;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs.SalesMan;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Repository.Master;
using System.Net.Mail;

namespace QuickBalance.API.Services
{

    public class SalesmanService : ISalesmanInterface
    {
        private readonly SalesManRepository _smMastDB;
        private readonly ItemGroupDBs _account;
        private readonly ILogger<SalesmanService> _logger;

        public SalesmanService(
            SalesManRepository smMastDB, ItemGroupDBs accMasDB,
            ILogger<SalesmanService> logger)
        {
            _smMastDB = smMastDB;
            _logger = logger;
            _account = accMasDB;
        }

        // =========================================================
        // CREATE
        // =========================================================
        public async Task<ApiResponse<SalesmanSaveResponse>> CreateAsync(
            SalesmanRequest request,
            int userId)
        {
            try
            {
                if (userId <= 0)
                    return ApiResponse<SalesmanSaveResponse>.Failure(
                        "Invalid user ID.");

                if (request == null)
                    return ApiResponse<SalesmanSaveResponse>.Failure(
                        "Salesman data is required.");

                string? validation = await ValidateSalesmanAsync(request);

                if (validation != null)
                    return ApiResponse<SalesmanSaveResponse>.Failure(validation);

                string salesmanName = request.SalesmanName.Trim();
                string secondName = request.SecondName?.Trim() ?? "";
                string printName = request.PrintName?.Trim() ?? "";
                string address = request.Address?.Trim() ?? "";
                string phone = request.Phone?.Trim() ?? "";
                string email = request.Email?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(printName))
                    printName = salesmanName;

                int employeeCode = request.EmployeeCode
                    ?? await _smMastDB.GetMaxCodeAsync();

                bool connectWithAccount = request.ConnectWithAccount;
                if(connectWithAccount && request.AccountId <= 0)
                {
                    return ApiResponse<SalesmanSaveResponse>.Failure(
                        "Account ID is required when connecting with an account.");
                }

                int accountId = connectWithAccount ? request.AccountId : 0;
                var accouuntCheck = await _account.GetAccountById(request.AccountId);
                if(accouuntCheck==0)
                {
                    return ApiResponse<SalesmanSaveResponse>.Failure(
                        "Account not found.");
                }

                int newId = await _smMastDB.InsertAsync(
                    employeeCode,
                    salesmanName,
                    printName,
                    address,
                    phone,
                    email,
                    request.SalesmanGroupId,
                    request.IsActive,
                    connectWithAccount,
                    accountId,
                    userId,
                    secondName);

                if (newId <= 0)
                    return ApiResponse<SalesmanSaveResponse>.Failure(
                        "Failed to create salesman.");

                return ApiResponse<SalesmanSaveResponse>.Success(
                    "Salesman created successfully.",
                    new SalesmanSaveResponse
                    {
                        Id = newId,
                        SalesmanCode = newId,
                        EmployeeCode = employeeCode
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating salesman.");

                return ApiResponse<SalesmanSaveResponse>.Failure(
                    "Unable to create salesman.");
            }
        }

        // =========================================================
        // GET ALL
        // =========================================================
        public async Task<ApiResponse<IReadOnlyList<SalesmanResponse>>>
            GetAllAsync(
                int page,
                int pageSize,
                string? search,
                int userId,
                bool? active = null)
        {
            try
            {
                if (userId <= 0)
                    return ApiResponse<IReadOnlyList<SalesmanResponse>>
                        .Failure("Invalid user ID.");

                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);

                search = string.IsNullOrWhiteSpace(search)
                    ? null
                    : search.Trim();

                int totalRecords =
                    await _smMastDB.GetTotalCountAsync(search, active);

                var salesmen = await _smMastDB.GetAllAsync(
                    page,
                    pageSize,
                    search,
                    active);

                var pagination = new PaginationInfo
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    TotalPages = (int)Math.Ceiling(
                        (double)totalRecords / pageSize)
                };

                return ApiResponse<IReadOnlyList<SalesmanResponse>>
                    .SuccessWithPagination(
                        "Salesmen retrieved successfully.",
                        salesmen,
                        pagination);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, "Error retrieving salesmen.");

                return ApiResponse<IReadOnlyList<SalesmanResponse>>
                    .Failure("Unable to retrieve salesmen.");
            }
        }

        // =========================================================
        // GET BY ID
        // =========================================================
        public async Task<ApiResponse<SalesmanResponse>> GetByIdAsync(
            int id,
            int userId)
        {
            try
            {
                if (userId <= 0)
                    return ApiResponse<SalesmanResponse>.Failure(
                        "Invalid user ID.");

                if (id <= 0)
                    return ApiResponse<SalesmanResponse>.Failure(
                        "Invalid salesman ID.");

                var salesman = await _smMastDB.GetByIdAsync(id);

                if (salesman == null)
                    return ApiResponse<SalesmanResponse>.Failure(
                        "Salesman not found.");

                return ApiResponse<SalesmanResponse>.Success(
                    "Salesman retrieved successfully.",
                    salesman);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving salesman {SalesmanId}.",
                    id);

                return ApiResponse<SalesmanResponse>.Failure(
                    "Unable to retrieve salesman.");
            }
        }

        // =========================================================
        // UPDATE
        // =========================================================
        public async Task<ApiResponse<SalesmanSaveResponse>> UpdateAsync(
            int id,
            SalesmanRequest request,
            int userId)
        {
            try
            {
                if (userId <= 0)
                    return ApiResponse<SalesmanSaveResponse>.Failure(
                        "Invalid user ID.");

                if (id <= 0)
                    return ApiResponse<SalesmanSaveResponse>.Failure(
                        "Invalid salesman ID.");

                if (request == null)
                    return ApiResponse<SalesmanSaveResponse>.Failure(
                        "Salesman data is required.");

                var existing = await _smMastDB.GetByIdAsync(id);

                if (existing == null)
                    return ApiResponse<SalesmanSaveResponse>.Failure(
                        "Salesman not found.");

                string? validation = await ValidateSalesmanAsync(request, id);

                if (validation != null)
                    return ApiResponse<SalesmanSaveResponse>.Failure(validation);

                string salesmanName = request.SalesmanName.Trim();
                string secondName = request.SecondName?.Trim() ?? "";
                string printName = request.PrintName?.Trim() ?? "";
                string address = request.Address?.Trim() ?? "";
                string phone = request.Phone?.Trim() ?? "";
                string email = request.Email?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(printName))
                    printName = salesmanName;

                int employeeCode = request.EmployeeCode
                    ?? existing.EmployeeCode;

                bool connectWithAccount = request.ConnectWithAccount;
                int accountId = connectWithAccount ? request.AccountId : 0;

                int affectedRows = await _smMastDB.UpdateAsync(
                    id,
                    employeeCode,
                    salesmanName,
                    printName,
                    address,
                    phone,
                    email,
                    request.SalesmanGroupId,
                    request.IsActive,
                    connectWithAccount,
                    accountId,
                    secondName);

                if (affectedRows <= 0)
                    return ApiResponse<SalesmanSaveResponse>.Failure(
                        "Salesman not found.");

                return ApiResponse<SalesmanSaveResponse>.Success(
                    "Salesman updated successfully.",
                    new SalesmanSaveResponse
                    {
                        Id = id,
                        SalesmanCode = id,
                        EmployeeCode = employeeCode
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating salesman {SalesmanId}.",
                    id);

                return ApiResponse<SalesmanSaveResponse>.Failure(
                    "Unable to update salesman.");
            }
        }

        // =========================================================
        // ACTIVE SALESMEN
        // =========================================================
        public async Task<ApiResponse<IReadOnlyList<ActiveSalesmanResponse>>>
            GetActiveAsync(int userId)
        {
            try
            {
                if (userId <= 0)
                    return ApiResponse<IReadOnlyList<ActiveSalesmanResponse>>
                        .Failure("Invalid user ID.");

                var salesmen = await _smMastDB.GetActiveAsync();

                return ApiResponse<IReadOnlyList<ActiveSalesmanResponse>>
                    .Success(
                        "Active salesmen retrieved successfully.",
                        salesmen);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, "Error retrieving active salesmen.");

                return ApiResponse<IReadOnlyList<ActiveSalesmanResponse>>
                    .Failure("Unable to retrieve active salesmen.");
            }
        }

        // =========================================================
        // LOOKUP
        // =========================================================
        public async Task<ApiResponse<IReadOnlyList<SalesmanLookupResponse>>>
            GetLookupAsync(int userId)
        {
            try
            {
                if (userId <= 0)
                    return ApiResponse<IReadOnlyList<SalesmanLookupResponse>>
                        .Failure("Invalid user ID.");

                var salesmen = await _smMastDB.GetLookupAsync();

                return ApiResponse<IReadOnlyList<SalesmanLookupResponse>>
                    .Success(
                        "Salesman lookup retrieved successfully.",
                        salesmen);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, "Error retrieving salesman lookup.");

                return ApiResponse<IReadOnlyList<SalesmanLookupResponse>>
                    .Failure("Unable to retrieve salesman lookup.");
            }
        }

        // =========================================================
        // VALIDATE ACCOUNT
        // =========================================================
        public async Task<ApiResponse<SalesmanAccountValidationResponse>>
         ValidateAccountAsync(int accountId, int userId)
        {
            try
            {
                if (userId <= 0)
                    return ApiResponse<SalesmanAccountValidationResponse>
                        .Failure("Invalid user ID.");

                if (accountId <= 0)
                    return ApiResponse<SalesmanAccountValidationResponse>
                        .Failure("Invalid account ID.");

                var salesmen =
                    await _smMastDB.ValidateAccountAsync(accountId);

                string message = salesmen.Count > 0
                    ? "Account is linked with salesman."
                    : "Account is not linked with salesman.";

                return ApiResponse<SalesmanAccountValidationResponse>.Success(
                    message,
                    new SalesmanAccountValidationResponse
                    {
                        Exists = salesmen.Count > 0,
                        Salesmen = salesmen
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error validating salesman account {AccountId}.",
                    accountId);

                return ApiResponse<SalesmanAccountValidationResponse>
                    .Failure("Unable to validate salesman account.");
            }
        }

        // =========================================================
        // SALESMAN VALIDATION
        // =========================================================
        private async Task<string?> ValidateSalesmanAsync(
            SalesmanRequest request,
            int salesmanCode = 0)
        {
            if (request == null)
                return "Salesman data is required.";

            if (string.IsNullOrWhiteSpace(request.SalesmanName))
                return "Salesman name must not be blank.";

            string salesmanName = request.SalesmanName.Trim();

            bool exists = salesmanCode > 0
                ? await _smMastDB.NameExistsAsync(
                    salesmanName,
                    salesmanCode)
                : await _smMastDB.NameExistsAsync(salesmanName);

            if (exists)
                return "Salesman name already exists.";

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                try
                {
                    _ = new MailAddress(request.Email.Trim());
                }
                catch
                {
                    return "Incorrect email format.";
                }
            }

            if (request.SalesmanGroupId < 1)
                return "Invalid salesman group.";

            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                foreach (char c in request.Phone)
                {
                    if (!char.IsDigit(c) && c != '+')
                        return "Phone number can contain only digits and '+'.";
                }
            }

            return null;
        }

        // =========================================================
        // DELETE - SOFT DELETE
        // =========================================================
        public async Task<ApiResponse<bool>> DeleteAsync(int id, int userId)
        {
            try
            {
                if (userId <= 0)
                    return ApiResponse<bool>.Failure(
                        "Invalid user ID.");

                if (id <= 0)
                    return ApiResponse<bool>.Failure(
                        "Invalid salesman ID.");

                var existingSalesman =
                    await _smMastDB.GetByIdAsync(id);

                if (existingSalesman == null)
                    return ApiResponse<bool>.Failure(
                        "Salesman not found.");

                int affectedRows =
                    await _smMastDB.SoftDeleteAsync(id);

                if (affectedRows <= 0)
                    return ApiResponse<bool>.Failure(
                        "Salesman not found or already deleted.");

                return ApiResponse<bool>.Success(
                    "Salesman deleted successfully.",
                    true);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error deleting salesman {SalesmanId}.",
                    id);

                return ApiResponse<bool>.Failure(
                    "Unable to delete salesman.");
            }
        }
    }
}
