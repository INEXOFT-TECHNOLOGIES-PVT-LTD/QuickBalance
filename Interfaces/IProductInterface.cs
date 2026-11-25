using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Product;
using QuickBalance.API.DTOs.Respons_DTO;
using static QuickBalance.API.DTOs.ProductRequest;

namespace QuickBalance.API.Interfaces;

public interface IProductInterface
{
    //Task<ApiResponse<int>> CreateAsync(
    //    ProductRequests request,
    //    int userId);

    Task<ApiResponse<int>> CreateAsync(ProductRequests request,int userId,IFormFile? imageFile = null);

    Task<ApiResponse<ProductResponse>> GetByIdAsync(int id);

    Task<ApiResponse<IReadOnlyList<ProductResponse>>> GetAllAsync(
        int page,
        int pageSize,
        string? search,
         int? groupId,
        bool? active
        );

    Task<ApiResponse<bool>> UpdateAsync(
    int id,
    ProductUpdateRequests request,
    int userId,
    IFormFile? imageFile = null);

    Task<ApiResponse<bool>> DeleteAsync(
        int id,
        int userId);
    Task<ApiResponse<ProductImageResponses>> GetImageAsync(int id);

    Task<ApiResponse<bool>> AddImageAsync(ProductImageRequest request, int userId);

    Task<ApiResponse<bool>> UpdateImageAsync(ProductImageRequest request, int userId);

    Task<ApiResponse<bool>> DeleteImageAsync(int id, int userId);

     

    //Task<ApiResponse<ProductImageResponse>> GetImageAsync(int id);
    //Task<ApiResponse<ProductImageResponse>> GetImageAsync(int id);

    //Task<ApiResponse<ProductImageResponse>> GetThumbnailAsync(int id);
}


//using QuickBalance.API.CLS;
//using QuickBalance.API.DTOs;
//using QuickBalance.API.DTOs.Product;
//using QuickBalance.API.DTOs.Respons_DTO;

//namespace QuickBalance.API.Interfaces;

//public interface IProductInterface
//{
//    Task<ApiResponse<int>> CreateAsync(
//        ProductRequests request,
//        int userId);

//    Task<ApiResponse<ProductResponse>> GetByIdAsync(int id);

//    Task<ApiResponse<IReadOnlyList<ProductResponse>>> GetAllAsync(
//        int page,
//        int pageSize,
//        string? search);

//    Task<ApiResponse<bool>> UpdateAsync(
//        int id,
//        ProductRequests request,
//        int userId);

//    Task<ApiResponse<bool>> DeleteAsync(int id);

//    Task<ApiResponse<ProductImageResponse>> GetImageAsync(int id);

//    Task<ApiResponse<ProductImageResponse>> GetThumbnailAsync(int id);
//}