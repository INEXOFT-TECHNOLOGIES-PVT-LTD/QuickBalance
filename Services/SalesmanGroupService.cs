using QuickBalance.API.CLS;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs.SalesMan;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Repository.Master;

namespace QuickBalance.API.Services
{
    public class SalesmanGroupService : ISalesmanGroupInterface
    {
        private readonly SalesManGroupRepository _smGrpDB;
        private readonly ILogger<SalesmanGroupService> _logger;

        public SalesmanGroupService(
            SalesManGroupRepository smGrpDB,
            ILogger<SalesmanGroupService> logger)
        {
            _smGrpDB = smGrpDB;
            _logger = logger;
        }

        // =========================================================
        // CREATE
        // =========================================================
        public async Task<ApiResponse<int>> CreateAsync(
            SalesmanGroupRequest request,
            int userId)
        {
            try
            {
                if (userId <= 0)
                    return ApiResponse<int>.Failure(
                        "Invalid user ID.");

                if (request == null)
                    return ApiResponse<int>.Failure(
                        "Request body is required.");

                if (string.IsNullOrWhiteSpace(request.GroupName))
                    return ApiResponse<int>.Failure(
                        "Group name must not be blank.");

                string groupName = request.GroupName.Trim();
                string secondName = request.SecondName?.Trim() ?? "";
                string printName = request.PrintName?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(printName))
                    printName = groupName;

                bool exists =
                    await _smGrpDB.GroupNameExistsAsync(groupName);

                if (exists)
                    return ApiResponse<int>.Failure(
                        "Group name already exists.");

                int newId = await _smGrpDB.InsertAsync(
                    groupName,
                    printName,
                    request.IsActive,
                    userId,
                    secondName);

                if (newId <= 0)
                    return ApiResponse<int>.Failure(
                        "Failed to create salesman group.");

                return ApiResponse<int>.Success(
                    "Salesman group created successfully.",
                    newId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, "Error creating salesman group.");

                return ApiResponse<int>.Failure(
                    "Unable to create salesman group.");
            }
        }

        // =========================================================
        // GET ALL
        // =========================================================
        public async Task<ApiResponse<IReadOnlyList<SalesmanGroupResponse>>>
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
                    return ApiResponse<IReadOnlyList<SalesmanGroupResponse>>
                        .Failure("Invalid user ID.");

                page = Math.Max(1, page);
                pageSize = Math.Max(1, pageSize);

                search = string.IsNullOrWhiteSpace(search)
                    ? null
                    : search.Trim();

                int totalRecords =
                    await _smGrpDB.GetTotalCountAsync(search, active);

                var groups = await _smGrpDB.GetAllAsync(
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

                return ApiResponse<IReadOnlyList<SalesmanGroupResponse>>
                    .SuccessWithPagination(
                        "Salesman groups retrieved successfully.",
                        groups,
                        pagination);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, "Error retrieving salesman groups.");

                return ApiResponse<IReadOnlyList<SalesmanGroupResponse>>
                    .Failure("Unable to retrieve salesman groups.");
            }
        }

        // =========================================================
        // GET BY ID
        // =========================================================
        public async Task<ApiResponse<SalesmanGroupResponse>>
            GetByIdAsync(int id, int userId)
        {
            try
            {
                if (userId <= 0)
                    return ApiResponse<SalesmanGroupResponse>
                        .Failure("Invalid user ID.");

                if (id <= 0)
                    return ApiResponse<SalesmanGroupResponse>.Failure(
                        "Invalid salesman group ID.");

                var group = await _smGrpDB.GetByIdAsync(id);

                if (group == null)
                    return ApiResponse<SalesmanGroupResponse>.Failure(
                        "Salesman group not found.");

                return ApiResponse<SalesmanGroupResponse>.Success(
                    "Salesman group retrieved successfully.",
                    group);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving salesman group {GroupId}.",
                    id);

                return ApiResponse<SalesmanGroupResponse>.Failure(
                    "Unable to retrieve salesman group.");
            }
        }

        // =========================================================
        // UPDATE
        // =========================================================
        public async Task<ApiResponse<bool>> UpdateAsync(
            int id,
            SalesmanGroupRequest request,
            int userId)
        {
            try
            {
                if (userId <= 0)
                    return ApiResponse<bool>.Failure(
                        "Invalid user ID.");

                if (id <= 0)
                    return ApiResponse<bool>.Failure(
                        "Invalid salesman group ID.");

                if (request == null)
                    return ApiResponse<bool>.Failure(
                        "Request body is required.");

                if (string.IsNullOrWhiteSpace(request.GroupName))
                    return ApiResponse<bool>.Failure(
                        "Group name must not be blank.");

                var existingGroup =
                    await _smGrpDB.GetByIdAsync(id);

                if (existingGroup == null)
                    return ApiResponse<bool>.Failure(
                        "Salesman group not found.");

                string groupName = request.GroupName.Trim();
                string secondName = request.SecondName?.Trim() ?? "";
                string printName = request.PrintName?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(printName))
                    printName = groupName;

                bool exists =
                    await _smGrpDB.GroupNameExistsAsync(groupName, id);

                if (exists)
                    return ApiResponse<bool>.Failure(
                        "Group name already exists.");

                int affectedRows = await _smGrpDB.UpdateAsync(
                    id,
                    groupName,
                    printName,
                    request.IsActive,
                    secondName);

                if (affectedRows <= 0)
                    return ApiResponse<bool>.Failure(
                        "Salesman group not found.");

                return ApiResponse<bool>.Success(
                    "Salesman group updated successfully.",
                    true);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating salesman group {GroupId}.",
                    id);

                return ApiResponse<bool>.Failure(
                    "Unable to update salesman group.");
            }
        }

        // =========================================================
        // LOOKUP
        // =========================================================
        public async Task<ApiResponse<IReadOnlyList<SalesmanGroupLookupResponse>>>
            GetLookupAsync(int userId)
        {
            try
            {
                if (userId <= 0)
                    return ApiResponse<IReadOnlyList<SalesmanGroupLookupResponse>>
                        .Failure("Invalid user ID.");

                var groups = await _smGrpDB.GetLookupAsync();

                return ApiResponse<IReadOnlyList<SalesmanGroupLookupResponse>>
                    .Success(
                        "Salesman group lookup retrieved successfully.",
                        groups);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, "Error retrieving salesman group lookup.");

                return ApiResponse<IReadOnlyList<SalesmanGroupLookupResponse>>
                    .Failure(
                        "Unable to retrieve salesman group lookup.");
            }
        }


        // =========================================================
        // DELETE - SOFT DELETE
        // =========================================================
        public async Task<ApiResponse<bool>> DeleteAsync(
            int id,
            int userId)
        {
            try
            {
                if (userId <= 0)
                    return ApiResponse<bool>.Failure(
                        "Invalid user ID.");

                if (id <= 0)
                    return ApiResponse<bool>.Failure(
                        "Invalid salesman group ID.");

                var existingGroup =
                    await _smGrpDB.GetByIdAsync(id);

                if (existingGroup == null)
                    return ApiResponse<bool>.Failure(
                        "Salesman group not found.");

                bool hasSalesmen =
                    await _smGrpDB.HasSalesmenAsync(id);

                if (hasSalesmen)
                    return ApiResponse<bool>.Failure(
                        "Cannot delete salesman group because it is assigned to one or more salesmen.");

                int affectedRows =
                    await _smGrpDB.SoftDeleteAsync(id);

                if (affectedRows <= 0)
                    return ApiResponse<bool>.Failure(
                        "Salesman group not found or already deleted.");

                return ApiResponse<bool>.Success(
                    "Salesman group deleted successfully.",
                    true);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error deleting salesman group {GroupId}.",
                    id);

                return ApiResponse<bool>.Failure(
                    "Unable to delete salesman group.");
            }
        }
    }
}