using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;

namespace QuickBalance.API.Interfaces
{
    public interface IAuthInterface
    {
        Task<ApiResponseWithoutPagination<LoginResponse>> LoginAsync(LoginRequest request);

        Task<ApiResponseWithoutPagination<bool>> LogoutAsync(string token);
    }
}
