
using Microsoft.Extensions.Logging;
using QuickBalance.API.CLS;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Product;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Repository.Master;
using static QuickBalance.API.DTOs.ProductRequest;

namespace QuickBalance.API.Services;

public sealed class ProductService : IProductInterface
{
    private readonly ProductDB _db;
    private readonly ILogger<ProductService> _logger;
    private const long MaxImageBytes = 2 * 1024 * 1024;   // 2 MB
    public ProductService(
        ProductDB db,
        ILogger<ProductService> logger)
    {
        _db = db;
        _logger = logger;
    }

    private static void Validate(ProductRequests request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.PrName))
            throw new ArgumentException("Product name is required.");

        if (request.PrName.Contains('&') ||
            request.PrName.Contains('@'))
        {
            throw new ArgumentException(
                "Product name cannot contain '&' or '@'.");
        }

        if (request.GroupId <= 0)
            throw new ArgumentException(
                "Please select a product group.");

        if (request.Unit <= 0)
            throw new ArgumentException(
                "Please select a product unit.");

        if (request.AltUnit > 0 &&
            request.AltUnit == request.Unit)
        {
            throw new ArgumentException(
                "Alternate unit cannot be the same as the main unit.");
        }

        if (request.TaxCatId <= 0)
            throw new ArgumentException(
                "Please select a tax category.");

        if (request.OpnStkQty > 0 &&
            request.OpnStkValue == 0)
        {
            throw new ArgumentException(
                "Opening stock value is required.");
        }

        if (request.BatchFlg == true &&
            request.SLNOTRACK == true)
        {
            throw new ArgumentException(
                "Batch tracking and serial number tracking cannot both be enabled.");
        }

        if (request.Aliases is null)
            return;

        var aliases = request.Aliases
            .Where(x => !string.IsNullOrWhiteSpace(x.AlName))
            .Select(x => x.AlName!.Trim())
            .ToList();

        if (aliases.Distinct(
                StringComparer.OrdinalIgnoreCase).Count() != aliases.Count)
        {
            throw new ArgumentException(
                "Duplicate product aliases are not allowed.");
        }

        if (!string.IsNullOrWhiteSpace(request.Alias) &&
            aliases.Any(x => string.Equals(
                x,
                request.Alias.Trim(),
                StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException(
                "Product alias cannot be repeated in the alias list.");
        }
    }

    // ============================================================
    //// CREATE PRODUCT
    //// ============================================================



    // ============================================================
    // CREATE PRODUCT
    // ============================================================

    //public async Task<ApiResponse<int>> CreateAsync(
    //    ProductRequests request,
    //    int userId)
    //{
    //    try
    //    {
    //        // 1. Basic request validation
    //        Validate(request);

    //        // 2. Validate user
    //        if (userId <= 0)
    //            return ApiResponse<int>.Failure("Invalid user ID.");

    //        // 3. Validate aliases
    //        ValidateAliases(request);

    //        // 4. Validate lookup values
    //        await ValidateLookupValuesAsync(request);

    //        // 5. Check duplicate product name
    //        if (await _db.ProductNameExistsAsync(request.PrName))
    //        {
    //            return ApiResponse<int>.Failure(
    //                "Product name already exists.");
    //        }

    //        // 6. Check duplicate product alias
    //        if (!string.IsNullOrWhiteSpace(request.Alias) &&
    //            await _db.AliasExistsAsync(request.Alias))
    //        {
    //            return ApiResponse<int>.Failure(
    //                "Product alias already exists.");
    //        }

    //        // 7. Check duplicate barcode
    //        if (!string.IsNullOrWhiteSpace(request.BarCode) &&
    //            await _db.BarcodeExistsAsync(request.BarCode))
    //        {
    //            return ApiResponse<int>.Failure(
    //                "Product barcode already exists.");
    //        }

    //        // 8. Insert product and related records
    //        int productCode = await _db.InsertProductMasterAsync(
    //            request,
    //            userId);

    //        if (productCode <= 0)   
    //        {
    //            return ApiResponse<int>.Failure(
    //                "Product creation failed.");
    //        }

    //        return ApiResponse<int>.Success(
    //            "Product created successfully.");
    //    }
    //    catch (ArgumentException ex)
    //    {
    //        return ApiResponse<int>.Failure(ex.Message);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error creating product.");

    //        return ApiResponse<int>.Failure(
    //            "An error occurred while creating the product.");
    //    }
    //}

    public async Task<ApiResponse<int>> CreateAsync(
    ProductRequests request,
    int userId,
    IFormFile? imageFile = null)                         // NEW
    {
        try
        {
            Validate(request);

            if (userId <= 0)
                return ApiResponse<int>.Failure("Invalid user ID.");

            ValidateAliases(request);
            await ValidateLookupValuesAsync(request);

            if (await _db.ProductNameExistsAsync(request.PrName))
                return ApiResponse<int>.Failure("Product name already exists.");

            if (!string.IsNullOrWhiteSpace(request.Alias) &&
                await _db.AliasExistsAsync(request.Alias))
                return ApiResponse<int>.Failure("Product alias already exists.");

            if (!string.IsNullOrWhiteSpace(request.BarCode) &&
                await _db.BarcodeExistsAsync(request.BarCode))
                return ApiResponse<int>.Failure("Product barcode already exists.");

            // NEW: check the image BEFORE anything is saved
            var (base64Image, imageError) = await PrepareImageAsync(imageFile);
            if (imageError is not null)
                return ApiResponse<int>.Failure(imageError);

            int productCode = await _db.InsertProductMasterAsync(
                request,
                userId,
                base64Image);                                // NEW

            if (productCode <= 0)
                return ApiResponse<int>.Failure("Product creation failed.");

            // CHANGED: return the new product ID
            return ApiResponse<int>.Success(
                "Product created successfully.",
                productCode);
        }
        catch (ArgumentException ex)
        {
            return ApiResponse<int>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product.");
            return ApiResponse<int>.Failure(
                "An error occurred while creating the product.");
        }
    }
    private void ValidateAliases(ProductRequests request)
    {
        if (request.Aliases == null)
            return;

        foreach (var alias in request.Aliases)
        {
            if (string.IsNullOrWhiteSpace(alias.AlName))
                throw new ArgumentException(
                    "Product alias name is required.");

            if (alias.AlName.Length > 100)
                throw new ArgumentException(
                    "Product alias name cannot exceed 100 characters.");

            if (!string.IsNullOrWhiteSpace(alias.AlCode) &&
                alias.AlCode.Length > 25)
                throw new ArgumentException(
                    "Product alias code cannot exceed 25 characters.");

            if (alias.Qty.HasValue && alias.Qty.Value <= 0)
                throw new ArgumentException(
                    $"Invalid quantity for alias '{alias.AlName}'.");
        }
    }
    private async Task ValidateLookupValuesAsync(ProductRequests request)
    {
        if (request.GroupId.HasValue &&
            !await _db.ProductGroupExistsAsync(request.GroupId.Value))
        {
            throw new ArgumentException("Invalid product group.");
        }

        if (request.CatgId.HasValue &&
            !await _db.ProductCategoryExistsAsync(request.CatgId.Value))
        {
            throw new ArgumentException("Invalid product category.");
        }

        if (request.BrandId.HasValue &&
            !await _db.BrandExistsAsync(request.BrandId.Value))
        {
            throw new ArgumentException("Invalid brand.");
        }

        if (request.Unit.HasValue &&
            !await _db.UnitExistsAsync(request.Unit.Value))
        {
            throw new ArgumentException("Invalid main unit.");
        }

        if (request.AltUnit.HasValue &&
            !await _db.UnitExistsAsync(request.AltUnit.Value))
        {
            throw new ArgumentException("Invalid alternative unit.");
        }

        if (request.TaxCatId.HasValue &&
            !await _db.TaxCategoryExistsAsync(request.TaxCatId.Value))
        {
            throw new ArgumentException("Invalid tax category.");
        }

        if (request.PriceCatId.HasValue &&
            !await _db.PriceCategoryExistsAsync(request.PriceCatId.Value))
        {
            throw new ArgumentException("Invalid price category.");
        }


        if (request.SaleAccId.HasValue)
        {
            if (request.SaleAccId.Value <= 0 ||
                !await _db.AccountExistsAsync(request.SaleAccId.Value))
                throw new ArgumentException("Invalid sales account.");
        }

        if (request.PurAcctId.HasValue)
        {
            if (request.PurAcctId.Value <= 0 ||
                !await _db.AccountExistsAsync(request.PurAcctId.Value))
                throw new ArgumentException("Invalid purchase account.");
        }

        if (request.SupplierId.HasValue)
        {
            if (request.SupplierId.Value <= 0 ||
                !await _db.SupplierExistsAsync(request.SupplierId.Value))
                throw new ArgumentException("Invalid supplier.");
        }

        if (request.GdwnId.HasValue)
        {
            if (request.GdwnId.Value <= 0 ||
                !await _db.GodownExistsAsync(request.GdwnId.Value))
                throw new ArgumentException("Invalid godown Id.");
        }

        if (request.CurrencyId.HasValue)
        {
            if (request.CurrencyId.Value <= 0 ||
                !await _db.CurrencyExistsAsync(request.CurrencyId.Value))
                throw new ArgumentException("Invalid currency Id");
        }
        // Alias unit validation
        foreach (var alias in request.Aliases)
        {
            if (alias.UnitId.HasValue &&
                !await _db.UnitExistsAsync(alias.UnitId.Value))
            {
                throw new ArgumentException(
                    $"Invalid unit in product alias '{alias.AlName}'.");
            }

            if (alias.InvUnitCode.HasValue &&
                !await _db.UnitExistsAsync(alias.InvUnitCode.Value))
            {
                throw new ArgumentException(
                    $"Invalid inventory unit in product alias '{alias.AlName}'.");
            }
        }
    }



    // ============================================================
    // GET PRODUCT BY ID
    // ============================================================

    public async Task<ApiResponse<ProductResponse>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
            {
                return ApiResponse<ProductResponse>.Failure(
                    "Invalid product ID.");
            }

            var result = await _db.GetByIdDetailAsync(id);

            if (result is null)
            {
                return ApiResponse<ProductResponse>.Failure(
                    "Product not found.");
            }

            return ApiResponse<ProductResponse>.Success(
                "Product retrieved successfully.",
                result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving product {ProductId}.",
                id);

            return ApiResponse<ProductResponse>.Failure(
                "Unable to retrieve product.");
        }
    }

    // ============================================================
    // GET ALL PRODUCTS
    // ============================================================

    public async Task<ApiResponse<IReadOnlyList<ProductResponse>>> GetAllAsync(
        int page,
        int pageSize,
         string? search,
        int? groupId,
        bool? active
        )
    {
        try
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var result = await _db.GetAllAsync(
                page,
                pageSize,
                search, groupId, active);

            return ApiResponse<IReadOnlyList<ProductResponse>>.SuccessWithPagination(
                "Products retrieved successfully.",
                result.Data,
                result.Pagination);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products.");

            return ApiResponse<IReadOnlyList<ProductResponse>>.Failure(
                "Unable to retrieve products.");
        }
    }

    // ============================================================
    // UPDATE PRODUCT
    // ============================================================


    //public async Task<ApiResponse<bool>> UpdateAsync(
    //int id,
    //ProductUpdateRequests request,
    //int userId)
    //{
    //    try
    //    {
    //        if (id <= 0)
    //            return ApiResponse<bool>.Failure("Invalid product ID.");

    //        if (userId <= 0)
    //            return ApiResponse<bool>.Failure("Invalid user ID.");

    //        Validate(request);


    //        var existing = await _db.GetByIdAsync(id);

    //        if (existing is null)
    //            return ApiResponse<bool>.Failure("Product not found.");

    //        if (await _db.ProductNameExistsAsync(request.PrName, id))
    //            return ApiResponse<bool>.Failure(
    //                "Product name already exists.");
    //        ValidateAliases(request);

    //        await ValidateLookupValuesAsync(request);

    //        if (!string.IsNullOrWhiteSpace(request.Alias) &&
    //            await _db.AliasExistsAsync(request.Alias, id))
    //        {
    //            return ApiResponse<bool>.Failure(
    //                "Product alias already exists.");
    //        }



    //        if (!string.IsNullOrWhiteSpace(request.BarCode) &&
    //            await _db.BarcodeExistsAsync(request.BarCode, id))
    //        {
    //            return ApiResponse<bool>.Failure(
    //                "Product barcode already exists.");
    //        }

    //        bool updated = await _db.UpdateProductAsync(
    //            id,
    //            request,
    //            userId);

    //        if (!updated)
    //        {
    //            return ApiResponse<bool>.Failure(
    //                "Product was modified or could not be updated. Please refresh and try again.");
    //        }

    //        return ApiResponse<bool>.Success(
    //            "Product updated successfully.");
    //    }
    //    catch (ArgumentException ex)
    //    {
    //        return ApiResponse<bool>.Failure(ex.Message);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(
    //            ex,
    //            "Error updating product {ProductId}.",
    //            id);

    //        return ApiResponse<bool>.Failure(
    //            "An error occurred while updating the product.");
    //    }
    //}

    public async Task<ApiResponse<bool>> UpdateAsync(
    int id,
    ProductUpdateRequests request,
    int userId,
    IFormFile? imageFile = null)                         // NEW
    {
        try
        {
            if (id <= 0)
                return ApiResponse<bool>.Failure("Invalid product ID.");

            if (userId <= 0)
                return ApiResponse<bool>.Failure("Invalid user ID.");

            Validate(request);

            var existing = await _db.GetByIdAsync(id);
            if (existing is null)
                return ApiResponse<bool>.Failure("Product not found.");

            if (await _db.ProductNameExistsAsync(request.PrName, id))
                return ApiResponse<bool>.Failure("Product name already exists.");

            ValidateAliases(request);
            await ValidateLookupValuesAsync(request);

            if (!string.IsNullOrWhiteSpace(request.Alias) &&
                await _db.AliasExistsAsync(request.Alias, id))
                return ApiResponse<bool>.Failure("Product alias already exists.");

            if (!string.IsNullOrWhiteSpace(request.BarCode) &&
                await _db.BarcodeExistsAsync(request.BarCode, id))
                return ApiResponse<bool>.Failure("Product barcode already exists.");

            // NEW
            var (base64Image, imageError) = await PrepareImageAsync(imageFile);
            if (imageError is not null)
                return ApiResponse<bool>.Failure(imageError);

            bool updated = await _db.UpdateProductAsync(
                id,
                request,
                userId,
                base64Image);                                // NEW

            if (!updated)
                return ApiResponse<bool>.Failure(
                    "Product was modified or could not be updated. Please refresh and try again.");

            return ApiResponse<bool>.Success("Product updated successfully.");
        }
        catch (ArgumentException ex)
        {
            return ApiResponse<bool>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}.", id);
            return ApiResponse<bool>.Failure(
                "An error occurred while updating the product.");
        }
    }

    // ============================================================
    // DELETE PRODUCT
    // ============================================================

    public async Task<ApiResponse<bool>> DeleteAsync(
        int id,
        int userId)
    {
        try
        {
            if (id <= 0)
            {
                return ApiResponse<bool>.Failure(
                    "Invalid product ID.");
            }

            if (userId <= 0)
            {
                return ApiResponse<bool>.Failure(
                    "Invalid user ID.");
            }

            var existing = await _db.GetByIdAsync(id);

            if (existing is null)
            {
                return ApiResponse<bool>.Failure(
                    "Product not found.");
            }

            if (await _db.IsProductUsedAsync(id))
            {
                return ApiResponse<bool>.Failure(
                    "Product is already used and cannot be deleted.");
            }

            int rows = await _db.DeleteAsync(id, userId);

            if (rows == 0)
            {
                return ApiResponse<bool>.Failure(
                    "Product was not deleted.");
            }

            return ApiResponse<bool>.Success(
                "Product deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error deleting product {ProductId}.",
                id);

            return ApiResponse<bool>.Failure(
                "Unable to delete product.");
        }
    }

    // ============================================================
    // GET PRODUCT IMAGE
    // ============================================================

    public async Task<ApiResponse<ProductImageResponses>> GetImageAsync(int id)
    {
        try
        {
            if (id <= 0)
            {
                return ApiResponse<ProductImageResponses>.Failure(
                    "Invalid product ID.");
            }

            // Get Base64 image from database
            string? base64 = await _db.GetProductImageAsync(id);

            if (string.IsNullOrWhiteSpace(base64))
            {
                return ApiResponse<ProductImageResponses>.Failure(
                    "Product image not found.");
            }

            // Convert Base64 to image bytes
            byte[] imageBytes;

            try
            {
                imageBytes = Convert.FromBase64String(base64);
            }
            catch (FormatException)
            {
                return ApiResponse<ProductImageResponses>.Failure(
                    "Invalid image data.");
            }

            // Detect image content type
            string contentType = "application/octet-stream";

            if (imageBytes.Length >= 8 &&
                imageBytes[0] == 0x89 &&
                imageBytes[1] == 0x50 &&
                imageBytes[2] == 0x4E &&
                imageBytes[3] == 0x47)
            {
                contentType = "image/png";
            }
            else if (imageBytes.Length >= 3 &&
                     imageBytes[0] == 0xFF &&
                     imageBytes[1] == 0xD8 &&
                     imageBytes[2] == 0xFF)
            {
                contentType = "image/jpeg";
            }

            return ApiResponse<ProductImageResponses>.Success(
                "Product image retrieved successfully.",
                new ProductImageResponses
                {
                    PrCode = id,
                    ImageBytes = imageBytes,
                    ContentType = contentType
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving product image. ProductId: {ProductId}",
                id);

            return ApiResponse<ProductImageResponses>.Failure(
                "Failed to retrieve product image.");
        }
    }

    // ============================================================
    // GET PRODUCT THUMBNAIL
    // ============================================================


    public async Task<ApiResponse<bool>> AddImageAsync(
    ProductImageRequest request,
    int userId)
    {
        try
        {
            if (request.ProductId <= 0)
                return ApiResponse<bool>.Failure("Invalid product code.");

            if (request.ImageFile == null || request.ImageFile.Length == 0)
                return ApiResponse<bool>.Failure("Please select an image.");

            bool productExists = await _db.ProductExistsAsync(request.ProductId);

            if (!productExists)
                return ApiResponse<bool>.Failure("Product not found.");

            bool imageExists = await _db.ProductImageExistsAsync(request.ProductId);

            if (imageExists)
                return ApiResponse<bool>.Failure(
                    "Product image already exists. Use update to replace it.");

            string base64Image = await ConvertImageToBase64Async(request.ImageFile);

            int rows = await _db.InsertProductImageAsync(
                request.ProductId,
                base64Image);

            if (rows <= 0)
                return ApiResponse<bool>.Failure("Failed to add product image.");

            _logger.LogInformation(
                "Product image added. ProductId: {ProductId}, UserId: {UserId}",
                request.ProductId,
                userId);

            return ApiResponse<bool>.Success(
                "Product image added successfully.");
                
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<bool>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error adding image for product {ProductId}",
                request.ProductId);

            return ApiResponse<bool>.Failure(
                "Failed to add product image.");
        }
    }

    private static async Task<string> ConvertImageToBase64Async(IFormFile imageFile)
    {
        string extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

        if (extension is not ".jpg" and not ".jpeg" and not ".png")
            throw new InvalidOperationException(
                "Only JPG, JPEG and PNG images are allowed.");

        using var memoryStream = new MemoryStream();

        await imageFile.CopyToAsync(memoryStream);

        byte[] imageBytes = memoryStream.ToArray();

        bool isPng =
            imageBytes.Length >= 8 &&
            imageBytes[0] == 0x89 &&
            imageBytes[1] == 0x50 &&
            imageBytes[2] == 0x4E &&
            imageBytes[3] == 0x47 &&
            imageBytes[4] == 0x0D &&
            imageBytes[5] == 0x0A &&
            imageBytes[6] == 0x1A &&
            imageBytes[7] == 0x0A;

        bool isJpeg =
            imageBytes.Length >= 3 &&
            imageBytes[0] == 0xFF &&
            imageBytes[1] == 0xD8 &&
            imageBytes[2] == 0xFF;

        if (!isPng && !isJpeg)
            throw new InvalidOperationException(
                "Invalid image file. Please upload a valid JPG or PNG image.");

        if (extension == ".png" && !isPng)
            throw new InvalidOperationException(
                "The selected file is not a valid PNG image.");

        if (extension is ".jpg" or ".jpeg" && !isJpeg)
            throw new InvalidOperationException(
                "The selected file is not a valid JPEG image.");

        return Convert.ToBase64String(imageBytes);
    }


    // Returns (null, null) when no image was sent.
    private static async Task<(string? Base64, string? Error)> PrepareImageAsync(
        IFormFile? imageFile)
    {
        if (imageFile is null || imageFile.Length == 0)
            return (null, null);

        if (imageFile.Length > MaxImageBytes)
            return (null, "Image must be 2 MB or smaller.");

        try
        {
            return (await ConvertImageToBase64Async(imageFile), null);
        }
        catch (InvalidOperationException ex)   // existing JPG/PNG checks
        {
            return (null, ex.Message);
        }
    }

    public async Task<ApiResponse<bool>> UpdateImageAsync(
    ProductImageRequest request,
    int userId)
    {
        try
        {
            if (request.ProductId <= 0)
                return ApiResponse<bool>.Failure("Invalid product code.");

            if (request.ImageFile == null || request.ImageFile.Length == 0)
                return ApiResponse<bool>.Failure("Please select an image.");

            bool productExists = await _db.ProductExistsAsync(request.ProductId);

            if (!productExists)
                return ApiResponse<bool>.Failure("Product not found.");

            bool imageExists = await _db.ProductImageExistsAsync(request.ProductId);

            if (!imageExists)
                return ApiResponse<bool>.Failure(
                    "Product image not found. Use add image first.");

            string base64Image = await ConvertImageToBase64Async(request.ImageFile);

            int rows = await _db.UpdateProductImageAsync(
                request.ProductId,
                base64Image);

            if (rows <= 0)
                return ApiResponse<bool>.Failure("Failed to update product image.");

            _logger.LogInformation(
                "Product image updated. ProductId: {ProductId}, UserId: {UserId}",
                request.ProductId,
                userId);

            return ApiResponse<bool>.Success(
                "Product image updated successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<bool>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error updating image for product {ProductId}",
                request.ProductId);

            return ApiResponse<bool>.Failure(
                "Failed to update product image.");
        }
    }

    public async Task<ApiResponse<bool>> DeleteImageAsync(int id, int userId)
    {
        try
        {
            if (id <= 0)
                return ApiResponse<bool>.Failure("Invalid product code.");

            bool productExists = await _db.ProductExistsAsync(id);

            if (!productExists)
                return ApiResponse<bool>.Failure("Product not found.");

            bool imageExists = await _db.ProductImageExistsAsync(id);

            if (!imageExists)
                return ApiResponse<bool>.Failure("Product image not found.");

            int rows = await _db.DeleteProductImageAsync(id);

            if (rows <= 0)
                return ApiResponse<bool>.Failure("Failed to delete product image.");

            _logger.LogInformation(
                "Product image deleted. ProductId: {ProductId}, UserId: {UserId}",
                id,
                userId);

            return ApiResponse<bool>.Success(
                "Product image deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error deleting image for product {ProductId}",
                id);

            return ApiResponse<bool>.Failure(
                "Failed to delete product image.");
        }
    }

}




