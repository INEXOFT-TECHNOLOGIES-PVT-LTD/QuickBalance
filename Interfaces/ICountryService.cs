using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Respons_DTO;

namespace QuickBalance.API.Interfaces
{
    public interface ICountryService
    {
        //Task<ApiResponse<List<CountryDtos>>> GetCountries();

        Task<ApiResponse<IReadOnlyList<CountryDtos>>> GetCountries(int page, int pageSize, string? search);
        Task<ApiResponse<CountryDtos>> GetCountryById(int id);

        Task<ApiResponse<int>> CreateCountry(CountryRequest request);

        Task<ApiResponse<bool>> UpdateCountry(int id, CountryRequest request);

        Task<ApiResponse<bool>> DeleteCountry(int id);
    }
}
