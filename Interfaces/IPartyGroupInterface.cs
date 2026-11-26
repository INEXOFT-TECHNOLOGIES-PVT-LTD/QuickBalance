using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.PartyGroup;

namespace QuickBalance.API.Interfaces
{
    public interface IPartyGroupInterface
    {
        Task<ApiResponse<int>> CreateAsync(PartyGroupRequest request);

        Task<ApiResponse<bool>> UpdateAsync(
            int id,
            PartyGroupRequest request);

        Task<ApiResponse<bool>> DeleteAsync(
            int id,
            int userId);

        Task<ApiResponse<PartyGroupResponse>> GetByIdAsync(int id);

        Task<ApiResponse<IReadOnlyList<PartyGroupListItem>>> GetAllAsync(
            string groupType,
            int page,
            int pageSize,
            string? search);
    }
}