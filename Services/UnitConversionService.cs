using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Repository.Master;

namespace QuickBalance.API.Services
{
    public class UnitConversionService
         : IUnitConversionService
    {
        private readonly UnitConversionRepository _repository;
        private readonly ILogger<UnitConversionService> _logger;
        private readonly PrUnitMasDB _prUnit;
        public UnitConversionService(UnitConversionRepository repository, PrUnitMasDB prUnit,
            ILogger<UnitConversionService> logger)
        {
            _repository = repository;
            _logger = logger;
            _repository = repository;
            _prUnit = prUnit;
        }
       
        public async Task<ApiResponse<List<UnitConversionDto>>> GetAll()
        {
            try
            {
                var result = await _repository.GetAllWithoutPagination();

                return ApiResponse<List<UnitConversionDto>>.Success(
                    "Unit conversions retrieved successfully.",result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unit conversions.");

                return ApiResponse<List<UnitConversionDto>>.Failure(
                    "Failed to retrieve unit conversions.");
            }
        }

        public async Task<ApiResponse<IReadOnlyList<UnitConversionDto>>> GetAll(int page, int pageSize, string? search = null)
        {
            try
            {

                if(page <= 0 || pageSize <= 0)
                {
                    return ApiResponse<IReadOnlyList<UnitConversionDto>>.Failure(
                        "Page and page size must be greater than zero.");
                }
                if(pageSize > 100)
                {
                    return ApiResponse<IReadOnlyList<UnitConversionDto>>.Failure(
                        "Page size must not exceed 100.");
                }
                var result = await _repository.GetAll(page, pageSize, search);

                return ApiResponse<IReadOnlyList<UnitConversionDto>>.SuccessWithPagination(
                    "Unit conversions retrieved successfully.", result.Data,result.Pagination);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unit conversions.");

                return ApiResponse<IReadOnlyList<UnitConversionDto>>.Failure(
                    "Failed to retrieve unit conversions.");
            }
        }

        public async Task<ApiResponse<UnitConversionDto>> GetByCode(
            int code)
        {
            try
            {
                if (code <= 0)
                {
                    return ApiResponse<UnitConversionDto>.Failure(
                        "Invalid conversion code.");
                }

                var result = await _repository.GetByCode(code);

                if (result == null)
                {
                    return ApiResponse<UnitConversionDto>.Failure(
                        "Unit conversion not found.");
                }

                return ApiResponse<UnitConversionDto>.Success(
                    
                    "Unit conversion retrieved successfully.", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving unit conversion. Code: {Code}",
                    code);

                return ApiResponse<UnitConversionDto>.Failure(
                    "Failed to retrieve unit conversion.");
            }
        }

        public async Task<ApiResponse<UnitConversionDto>> Create(int userId,UnitConversionRequestDto request)
        {
            try
            {
                //if (request.Code <= 0)
                //{
                //    return ApiResponse<UnitConversionDto>.Failure(
                //        "Conversion code is required.");
                //}

                if (!IsValidRequest(request))
                {
                    return ApiResponse<UnitConversionDto>.Failure(
                        "Main unit, sub unit and conversion factor must be valid.");
                }

                if(request.MainUnit !=null)
                {
                    
                    var existing = await _prUnit.GetByIdAsync((int)request.MainUnit, userId);

                    if (existing == null)
                    {
                        return ApiResponse<UnitConversionDto>.Failure(
                            "Main unit does not exist.");

                    }
                }
                if (request.SubUnit != null)
                {
                    var existingsubunit = await _prUnit.GetByIdAsync((int)request.SubUnit, userId);

                    if (existingsubunit == null)
                    {
                        return ApiResponse<UnitConversionDto>.Failure(
                            "Sub unit does not exist.");
                    }
                }
                var result = await _repository.Create(userId, request);

                if (result == null)
                {
                    return ApiResponse<UnitConversionDto>.Failure(
                        "Unable to create unit conversion.");
                }

                return ApiResponse<UnitConversionDto>.Success(
                    
                    "Unit conversion created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating unit conversion. Code: {Code}",
                    request);

                return ApiResponse<UnitConversionDto>.Failure(
                    "Failed to create unit conversion.");
            }
        }

        public async Task<ApiResponse<UnitConversionDto>> Update(int userId,
            int code,
            UnitConversionRequestDto request)
        {
            try
            {
                if (code <= 0)
                {
                    return ApiResponse<UnitConversionDto>.Failure(
                        "Invalid conversion code.");
                }

                if (!IsValidRequest(request))
                {
                    return ApiResponse<UnitConversionDto>.Failure(
                        "Main unit, sub unit and conversion factor must be valid.");
                }

                var existing = await _repository.GetByCode(code);

                if (existing == null)
                {
                    return ApiResponse<UnitConversionDto>.Failure(
                        "Unit conversion not found.");
                }

                if (request.MainUnit != null)
                {

                    var exist= await _prUnit.GetByIdAsync((int)request.MainUnit, userId);

                    if (exist == null)
                    {
                        return ApiResponse<UnitConversionDto>.Failure(
                            "Main unit does not exist.");

                    }
                }
                if (request.SubUnit != null)
                {
                    var existingsubunit = await _prUnit.GetByIdAsync((int)request.SubUnit, userId);

                    if (existingsubunit == null)
                    {
                        return ApiResponse<UnitConversionDto>.Failure(
                            "Sub unit does not exist.");
                    }
                }

                var result = await _repository.Update(userId, code, request);

                if (result == null)
                {
                    return ApiResponse<UnitConversionDto>.Failure(
                        "Unable to update unit conversion.");
                }

                return ApiResponse<UnitConversionDto>.Success(
                    
                    "Unit conversion updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating unit conversion. Code: {Code}",
                    code);

                return ApiResponse<UnitConversionDto>.Failure(
                    "Failed to update unit conversion.");
            }
        }

        public async Task<ApiResponse<bool>> Delete(int code, int userId)
        {
            try
            {
                if (code <= 0)
                {
                    return ApiResponse<bool>.Failure(
                        "Invalid conversion code.");
                }

                var existing = await _repository.GetByCode(code);

                if (existing == null)
                {
                    return ApiResponse<bool>.Failure(
                        "Unit conversion not found.");
                }

                var rowsAffected = await _repository.Delete(code, userId);

                if (rowsAffected == 0)
                {
                    return ApiResponse<bool>.Failure(
                        "Unit conversion not found.");
                }

                return ApiResponse<bool>.Success(
                    "Unit conversion deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error deleting unit conversion. Code: {Code}",
                    code);

                return ApiResponse<bool>.Failure(
                    "Failed to delete unit conversion.");
            }
        }

        private static bool IsValidRequest(
            UnitConversionRequestDto request)
        {
            return request.MainUnit.HasValue &&
                   request.MainUnit.Value > 0 &&
                   request.SubUnit.HasValue &&
                   request.SubUnit.Value > 0 &&
                   request.ConvFact.HasValue &&
                   request.ConvFact.Value > 0;
        }
    }
}
