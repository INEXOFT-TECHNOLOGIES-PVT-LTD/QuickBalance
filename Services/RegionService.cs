using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Repositories;
using QuickBalance.API.Repository.Master;

namespace QuickBalance.API.Services;

public class RegionService : IRegionInterface
{
    private readonly RegionRepository _repository;

    private readonly CountryRepository _countryRepository;
    private readonly ILogger<RegionService> _logger;

    public RegionService(
        RegionRepository repository,
        CountryRepository countryRepository,        
        ILogger<RegionService> logger)
    {
        _repository = repository;
        _countryRepository = countryRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<List<RegionResponse>>> GetRegionsAsync(int countryId)
    {
        try
        {
            if (countryId <= 0)
                return ApiResponse<List<RegionResponse>>.Failure(
                    "Invalid country ID.");

            var regions = await _repository.GetRegionsAsyncRecords(countryId);

            return ApiResponse<List<RegionResponse>>.Success(
                "Regions retrieved successfully.",
                regions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving regions for CountryId: {CountryId}",
                countryId);

            return ApiResponse<List<RegionResponse>>.Failure(
                "An error occurred while retrieving regions.");
        }
    }


    public async Task<ApiResponse<IReadOnlyList<RegionResponse>>> GetRegionsAsync(
     int countryId,
     string? search,
     int page,
     int pageSize,
     bool? active = null)
    {
        try
        {
            if (countryId <= 0)
                return ApiResponse<IReadOnlyList<RegionResponse>>.Failure(
                    "Valid country Id is required.");

            if (page <= 0)
                return ApiResponse<IReadOnlyList<RegionResponse>>.Failure(
                    "Page must be greater than zero.");

            if (pageSize <= 0 || pageSize > 200)
                return ApiResponse<IReadOnlyList<RegionResponse>>.Failure(
                    "Page size must be between 1 and 200.");

            var regions =
                await _repository.GetRegionsAsync(
                    countryId, search, page, pageSize, active);


            return ApiResponse<IReadOnlyList<RegionResponse>>.SuccessWithPagination(
                "Regions retrieved successfully.",
                regions.Data,
                regions.Pagination);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving regions.");
            return ApiResponse<IReadOnlyList<RegionResponse>>.Failure(
                "An error occurred while retrieving regions.");
        }
    }
    public async Task<ApiResponse<RegionResponse>> GetRegionByIdAsync(int regionId)
    {
        try
        {
            if (regionId <= 0)
                return ApiResponse<RegionResponse>.Failure("Invalid region ID.");

            var region = await _repository.GetRegionByIdAsync(regionId);

            if (region == null)
                return ApiResponse<RegionResponse>.Failure("Region not found.");

            return ApiResponse<RegionResponse>.Success(
                "Region retrieved successfully.", region);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving region {RegionId}", regionId);

            return ApiResponse<RegionResponse>.Failure(
                "An error occurred while retrieving the region.");
        }   
    }

    public async Task<ApiResponse<RegionResponse>> GetRegionsByCoutryId(int countryId)
    {
        try
        {
            if (countryId <= 0)
                return ApiResponse<RegionResponse>.Failure("Invalid country ID.");

            var regions = await _repository.GetRegionsByCoutryId(countryId);

            if (regions == null)
                return ApiResponse<RegionResponse>.Failure("No regions found for the specified country.");

            return ApiResponse<RegionResponse>.Success(
                "Regions retrieved successfully.", regions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving regions for CountryId: {CountryId}", countryId);

            return ApiResponse<RegionResponse>.Failure(
                "An error occurred while retrieving the regions.");
        }
    }
    
    public async Task<ApiResponse<int>> CreateRegionAsync(RegionRequest request)
    {
        try
        {
            if (request == null)
                return ApiResponse<int>.Failure("Request is required.");

            if (request.CountryId <= 0)
                return ApiResponse<int>.Failure("Valid country ID is required.");

            if (string.IsNullOrWhiteSpace(request.RegionName))
                return ApiResponse<int>.Failure("Region name is required.");

            if (request.RegionName.Length > 50)
                return ApiResponse<int>.Failure("Region name cannot exceed 50 characters.");

            if (request.RegionSecName?.Length > 50)
                return ApiResponse<int>.Failure("Secondary region name cannot exceed 50 characters.");

            var countryExists = await _countryRepository.GetCountryById(request.CountryId);
            
            if (countryExists == null)
                return ApiResponse<int>.Failure("Invalid country ID.");
            var regioNamExist = await _repository.GetRegionByNameAsync(request.RegionName, request.CountryId);
            if (regioNamExist >=1)
                return ApiResponse<int>.Failure("Region name already exists for the specified country.");

            var regionId = await _repository.CreateRegionAsync(request);

            if (regionId <= 0)
                return ApiResponse<int>.Failure("Unable to create region.");

            return ApiResponse<int>.Success(
                "Region created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating region");

            return ApiResponse<int>.Failure(
                "An error occurred while creating the region.");
        }
    }

    public async Task<ApiResponse<bool>> UpdateRegionAsync(
        int regionId, RegionRequest request)
    {
        try
        {
            if (regionId <= 0)
                return ApiResponse<bool>.Failure("Invalid region ID.");

            if (request == null)
                return ApiResponse<bool>.Failure("Request is required.");

            if (request.CountryId <= 0)
                return ApiResponse<bool>.Failure("Valid country ID is required.");

            if (string.IsNullOrWhiteSpace(request.RegionName))
                return ApiResponse<bool>.Failure("Region name is required.");

            if (request.RegionName.Length > 50)
                return ApiResponse<bool>.Failure("Region name cannot exceed 50 characters.");

            if (request.RegionSecName?.Length > 50)
                return ApiResponse<bool>.Failure("Secondary region name cannot exceed 50 characters.");

            var countryExists = await _countryRepository.GetCountryById(request.CountryId);

            if (countryExists == null)
                return ApiResponse<bool>.Failure("Invalid country ID.");

            var regioNamExist = await _repository.GetRegionByNameAndIdAsync(request.RegionName, request.CountryId,regionId);
            if (regioNamExist >= 1)
                return ApiResponse<bool>.Failure("Region name already exists for the specified country.");

            var updated = await _repository.UpdateRegionAsync(regionId, request);

            if (!updated)
                return ApiResponse<bool>.Failure("Region not found.");

            return ApiResponse<bool>.Success(
                "Region updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating region {RegionId}", regionId);

            return ApiResponse<bool>.Failure(
                "An error occurred while updating the region.");
        }
    }
}