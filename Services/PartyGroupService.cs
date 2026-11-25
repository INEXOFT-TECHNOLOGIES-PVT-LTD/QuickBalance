using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.PartyGroup;
using QuickBalance.API.DTOs.Product;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Repositories;
using QuickBalance.API.Repository.Master;

namespace QuickBalance.API.Services
{
    public sealed class PartyGroupService : IPartyGroupInterface
    {
        private readonly PartyGroupRepository _repository;
        private readonly ILogger<PartyGroupService> _logger;

        public PartyGroupService(
            PartyGroupRepository repository,
            ILogger<PartyGroupService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        private static string GetGroupType(string groupType)
        {
            return groupType.Trim().Equals(
                "Customer",
                StringComparison.OrdinalIgnoreCase)
                ? "C"
                : "S";
        }

        private static bool IsValidGroupType(string groupType)
        {
            return groupType.Equals(
                       "Customer",
                       StringComparison.OrdinalIgnoreCase)
                   || groupType.Equals(
                       "Supplier",
                       StringComparison.OrdinalIgnoreCase);
        }

        public async Task<ApiResponse<int>> CreateAsync(
            PartyGroupRequest request)
        {
            try
            {
                if (!IsValidGroupType(request.GroupType))
                    return ApiResponse<int>.Failure(
                        "Group type must be Customer or Supplier.");

                if (string.IsNullOrWhiteSpace(request.GroupName))
                    return ApiResponse<int>.Failure(
                        "Group name is required.");

                var name = request.GroupName.Trim();
                var type = GetGroupType(request.GroupType);

                if (await _repository.ExistsAsync(name, type))
                    return ApiResponse<int>.Failure(
                        "Group name already exists.");

                var id = await _repository.CreateAsync(
                    request.GroupName.Trim(),
                    request.GroupShortName?.Trim(),
                    type);
                return ApiResponse<int>.Success(
                    "Party group created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating party group.");

                return ApiResponse<int>.Failure(
                    "Unable to create party group.");
            }
        }

        public async Task<ApiResponse<bool>> UpdateAsync(
            int id,
            PartyGroupRequest request)
        {
            try
            {
                if (id <= 0)
                    return ApiResponse<bool>.Failure(
                        "Invalid group ID.");

                if (!IsValidGroupType(request.GroupType))
                    return ApiResponse<bool>.Failure(
                        "Group type must be Customer or Supplier.");

                if (string.IsNullOrWhiteSpace(request.GroupName))
                    return ApiResponse<bool>.Failure(
                        "Group name is required.");

                var name = request.GroupName.Trim();
                var type = GetGroupType(request.GroupType);

                if (await _repository.ExistsAsync(name, type, id))
                    return ApiResponse<bool>.Failure(
                        "Another group already uses this name.");

                var updated = await _repository.UpdateAsync(
                  id,
                  request.GroupName.Trim(),
                  request.GroupShortName?.Trim(),
                  type);

                if (!updated)
                    return ApiResponse<bool>.Failure(
                        "Party group not found.");

                return ApiResponse<bool>.Success(
                    "Party group updated successfully.");
            }

            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating party group. ID: {Id}",
                    id);

                return ApiResponse<bool>.Failure(
                    "Unable to update party group.");
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(
       int id,
       int userId)
        {
            try
            {
                if (id <= 0)
                    return ApiResponse<bool>.Failure("Invalid group ID.");

                //if (!IsValidGroupType(groupType))
                //    return ApiResponse<bool>.Failure(
                //        "Group type must be Customer or Supplier.");

                //var type = GetGroupType(groupType);

                var deleted = await _repository.DeleteAsync(id, userId);

                if (!deleted)
                    return ApiResponse<bool>.Failure("Party group not found.");

                return ApiResponse<bool>.Success(
                    "Party group deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting party group. ID: {Id}", id);

                return ApiResponse<bool>.Failure(
                    "Unable to delete party group. It may be in use.");
            }
        }

        public async Task<ApiResponse<PartyGroupResponse>> GetByIdAsync(int id)
        {
            try
            {
                if (id <= 0)
                    return ApiResponse<PartyGroupResponse>.Failure("Invalid group ID.");

                var group = await _repository.GetByIdAsync(id);

                if (group == null)
                    return ApiResponse<PartyGroupResponse>.Failure("Party group not found.");

                return ApiResponse<PartyGroupResponse>.Success(
                    "Party group retrieved successfully.", group);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving party group. ID: {Id}", id);

                return ApiResponse<PartyGroupResponse>.Failure(
                    "Unable to retrieve party group.");
            }
        }

        public async Task<ApiResponse<IReadOnlyList<PartyGroupListItem>>> GetAllAsync(
            string groupType,int page,int pageSize,string? search)
        {
            try
            {
                if (!IsValidGroupType(groupType))
                    return ApiResponse<IReadOnlyList<PartyGroupListItem>>.Failure(
                        "Group type must be Customer or Supplier.");

                var type = GetGroupType(groupType);

                //var result = await _repository.GetAllAsync(type);
                var result = await _repository.GetAllAsync(type, page, pageSize, search);
                return ApiResponse<IReadOnlyList<PartyGroupListItem>>.SuccessWithPagination(
               "Party groups retrieved successfully.",
               result.Data,
               result.Pagination);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving party groups.");

                return ApiResponse<IReadOnlyList<PartyGroupListItem>>.Failure(
                    "Unable to retrieve party groups.");
            }
        }


    }
}