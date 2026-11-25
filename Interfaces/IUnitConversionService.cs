using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;

namespace QuickBalance.API.Interfaces
{
    public interface IUnitConversionService
    {
        Task<ApiResponse<List<UnitConversionDto>>> GetAll();
        Task<ApiResponse<IReadOnlyList<UnitConversionDto>>> GetAll(int page, int pageSize, string? search);

        Task<ApiResponse<UnitConversionDto>> GetByCode(int code);

        Task<ApiResponse<UnitConversionDto>> Create(int userId,
           UnitConversionRequestDto request);

        Task<ApiResponse<UnitConversionDto>> Update(int userId,
            int code,
            UnitConversionRequestDto request);

        Task<ApiResponse<bool>> Delete(int code, int userId);
    }
}
