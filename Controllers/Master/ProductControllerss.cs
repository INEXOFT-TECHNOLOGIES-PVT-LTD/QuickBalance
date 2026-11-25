using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using QuickBalance.API.CLS;
using QuickBalance.API.Configuration;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using SkiaSharp;
using System.Data;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace QuickBalance.API.Controllers.Master
{
    [ApiController]
    //[ApiExplorerSettings(IgnoreApi = true)]

    [Route("api/v1/makedblank")]
    [ApiExplorerSettings(IgnoreApi = true)]

    //[Tags("05. Products")]

    [Authorize]
    public class ProductControllers : ControllerBase
    {
        private readonly DatabaseConnection _databaseConnection;

        public ProductControllers(
            DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }   

        // ============================================================
        // GET PRODUCT LIST (PAGINATED)
        // GET: /api/v1/products?page=1&pageSize=20&search=abc&sortBy=PRNAME&sortDirection=ASC
        // ============================================================

        [HttpGet("products")]
        public async Task<IActionResult> GetProductList(
           // [FromQuery] string voucherTypeCodes = "",
            [FromQuery] PaginationRequests pagination)
        {
            try
            {
                // ====================================================
                // PAGINATION
                // ====================================================

                pagination ??= new PaginationRequests();
                pagination.Normalizes();

                // ====================================================
                // LANGUAGE / VARIABLES
                // ====================================================

                LngTB.InitLng();

                Variable Variables = new Variable();
                //Variables.VoucherTypeCodes = voucherTypeCodes;

                // ====================================================
                // USER ID FROM JWT
                // ====================================================

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = "error",
                        message = "User ID not found in token."
                    });
                }

                Variables.UserId = int.Parse(userIdClaim.Value);

                // ====================================================
                // DATABASE CONNECTION
                // ====================================================

                //DatabaseConnection databaseConnection = new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(Variables);
                string connectionString = _databaseConnection.GetConnectionString(Variables);

                DBConn db = new DBConn(connectionString);

                CommonFunction commonFunction = new CommonFunction(db);


                WriteBulkDebugLog("STEP 2: Before CheckDatabaseConnection");

                bool connectionResult = await commonFunction.CheckDatabaseConnection();

                WriteBulkDebugLog($"STEP 3: Connection check returned: {connectionResult}");


                if (!connectionResult)
                {
                    return StatusCode(500, new
                    {
                        status = "error",
                        message = "Database connection failed."
                    });
                }

                // ====================================================
                // SET VARIABLE VALUES
                // ====================================================

                await commonFunction.SetVariableValues(Variables);

                commonFunction.Variables = Variables;
                commonFunction.Functions.Variables = Variables;

                // ====================================================
                // PRODUCT DB
                // ====================================================

                PrMastDB prMastDB = new PrMastDB(db);

                prMastDB.Variables = Variables;
                prMastDB.Functions.Variables = Variables;

                // ====================================================
                // GET PAGED PRODUCT LIST
                // ====================================================

                var result = await prMastDB.ListPaged(
                    pagination.Page,
                    pagination.PageSize,
                    pagination.Search
                    //pagination.SortBy,
                    //pagination.SortDirection
                );

                DataTable dt = result.Data;

                int totalRecords = result.TotalRecords;

                int totalPages = (int)Math.Ceiling(
                    totalRecords / (double)pagination.PageSize
                );

                // ====================================================
                // CONVERT CURRENT PAGE TO RESPONSE DATA
                // ====================================================
                var tableData = new List<Dictionary<string, object?>>();

                foreach (DataRow row in dt.Rows)
                {
                    var product = dt.Columns
                        .Cast<DataColumn>()
                        .ToDictionary(
                            column => column.ColumnName.ToLowerInvariant(),
                            column => row[column] == DBNull.Value ? null : row[column]
                        );

                    int productId = Convert.ToInt32(row["PRCODE"]);

                    // Default both URLs to null.
                    product["imageUrl"] = null;
                    product["thumbnailUrl"] = null;

                    ClsPrMast clsPrMast = new ClsPrMast
                    {
                        PrCode = productId
                    };

                    PrImgDB prImgDB = new PrImgDB(db)
                    {
                        Variables = Variables
                    };
                    prImgDB.Functions.Variables = Variables;

                    DataTable imageTable = await prImgDB.SearchByID(clsPrMast);

                    if (imageTable != null &&
                        imageTable.Rows.Count > 0 &&
                        imageTable.Columns.Contains("PRIMG") &&
                        imageTable.Rows[0]["PRIMG"] != DBNull.Value &&
                        TryReadStoredImage(
                            imageTable.Rows[0]["PRIMG"],
                            out byte[] imageBytes) &&
                        imageBytes.Length > 0)
                    {
                        product["imageUrl"] = Url.Action(
                            nameof(GetProductImage),
                            new { id = productId }
                        );

                        product["thumbnailUrl"] = Url.Action(
                            nameof(GetProductThumbnail),
                            new { id = productId }
                        );
                    }

                    tableData.Add(product);
                }

                // ====================================================
                // PAGINATION RESPONSE
                // ====================================================

                var paginationResponse =
                    new PaginationResponse<Dictionary<string, object?>>
                    {
                        Data = tableData,
                        Page = pagination.Page,
                        PageSize = pagination.PageSize,
                        TotalRecords = totalRecords,
                        TotalPages = totalPages
                    };

                // ====================================================
                // RESPONSE
                // ====================================================

                return Ok(new
                {
                    status = "success",
                    message = "Product list retrieved successfully.",
                    data = paginationResponse.Data,
                    page = paginationResponse.Page,
                    pageSize = paginationResponse.PageSize,
                    totalRecords = paginationResponse.TotalRecords,
                    totalPages = paginationResponse.TotalPages
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = "error",
                    message = "Database error occurred.",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "error",
                    message = "Unexpected error occurred.",
                    error = ex.Message
                });
            }
        }


        // ============================================================
        // GET PRODUCT
        // ============================================================

        [HttpGet("products/{id:int}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            try
            {
                // ====================================================
                // LANGUAGE / VARIABLES
                // ====================================================

                LngTB.InitLng();

                Variable Variables = new Variable();

                // ====================================================
                // USER ID FROM JWT
                // ====================================================

                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = "error",
                        message = "User ID not found in token."
                    });
                }

                Variables.UserId =
                    int.Parse(userIdClaim.Value);

                // ====================================================
                // DATABASE CONNECTION
                // ====================================================

                //DatabaseConnection databaseConnection =
                //    new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(Variables);
                string connectionString = _databaseConnection.GetConnectionString(Variables);

                DBConn db =
                    new DBConn(connectionString);

                CommonFunction commonFunction =
                    new CommonFunction(db);

                bool connectionResult =
                    await commonFunction.CheckDatabaseConnection();

                if (!connectionResult)
                {
                    return StatusCode(500, new
                    {
                        status = "error",
                        message = "Database connection failed."
                    });
                }

                // ====================================================
                // SET VARIABLE VALUES
                // ====================================================

                await commonFunction.SetVariableValues(Variables);

                commonFunction.Variables = Variables;

                commonFunction.Functions.Variables = Variables;

                // ====================================================
                // PRODUCT DB
                // ====================================================

                PrMastDB prMastDB =
                    new PrMastDB(db);

                prMastDB.Variables = Variables;

                prMastDB.Functions.Variables = Variables;

                // ====================================================
                // OPENING STOCK DB
                // ====================================================

                OpnStkDB opnStkDB =
                    new OpnStkDB(db);

                opnStkDB.Variables = Variables;

                // ====================================================
                // ALIAS DB
                // ====================================================

                PrAliasDB prAliasDB =
                    new PrAliasDB(db);

                prAliasDB.Variables = Variables;

                // ====================================================
                // PRICE CATEGORY DB
                // ====================================================

                PriceCatLevDB priceCatLevDB =
                    new PriceCatLevDB(db);

                priceCatLevDB.Variables = Variables;

                if (priceCatLevDB.Functions != null)
                {
                    priceCatLevDB.Functions.Variables = Variables;
                }

                // ====================================================
                // PRODUCT IMAGE DB
                // ====================================================

                PrImgDB prImgDB =
                    new PrImgDB(db);

                prImgDB.Variables = Variables;

                if (prImgDB.Functions != null)
                {
                    prImgDB.Functions.Variables = Variables;
                }

                // ====================================================
                // PRODUCT
                // ====================================================

                ClsPrMast clsPrMast =
                    new ClsPrMast();

                clsPrMast.PrCode = id;

                DataTable productTable =
                    await prMastDB.SearchByCode(clsPrMast);

                if (productTable == null ||
                    productTable.Rows.Count == 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Product not found."
                    });
                }

                // ====================================================
                // OPENING STOCK
                // ====================================================

                ClsOpnStk clsOpnStk =
                    new ClsOpnStk();

                clsOpnStk.PrCode = id;

                DataTable openingStockTable =
                    await opnStkDB.SearchByPrCode(clsOpnStk);

                // ====================================================
                // ALIASES
                // ====================================================

                ClsPrAlias clsPrAlias =
                    new ClsPrAlias();

                clsPrAlias.PrCode = id;

                DataTable aliasTable =
                    await prAliasDB.SearchByPrCode(clsPrAlias);

                // ====================================================
                // PRICE CATEGORIES
                // ====================================================

                ClsPriceCatLev clsPriceCatLev =
                    new ClsPriceCatLev();

                clsPriceCatLev.PrCode = id;

                DataTable priceCategoryTable =
                    await priceCatLevDB.SearchByPrCode(
                        clsPriceCatLev
                    );

                // ====================================================
                // PRODUCT DATA
                // ====================================================

                var product =
                    ConvertDataTable(productTable)
                    .FirstOrDefault();

                // ====================================================
                // RESPONSE
                // ====================================================


                return Ok(new
                {
                    status = true,
                    message = "Product retrieved successfully.",
                    data = new
                    {
                        product = product,

                        openingStock =
                            ConvertDataTable(openingStockTable),

                        aliases =
                            ConvertDataTable(aliasTable),

                        priceCategories =
                            ConvertDataTable(priceCategoryTable),

                        // Original product image
                        imageUrl = Url.Action(
                            nameof(GetProductImage),
                            new { id }
                        ),

                        // Optimized product thumbnail
                        thumbnailUrl = Url.Action(
                            nameof(GetProductThumbnail),
                            new { id }
                        )
                    }
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = "error",
                    message = "Database error occurred.",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "error",
                    message = "Unexpected error occurred.",
                    error = ex.Message
                });
            }
        }

        [HttpPost("products")]
        public async Task<IActionResult> CreateProduct(
        [FromBody] ProductRequest request)
        {
            DBConn? db = null;

            try
            {
                // ====================================================
                // LANGUAGE
                // ====================================================
                LngTB.InitLng();

                // ====================================================
                // VARIABLES
                // ====================================================
                Variable Variables = new Variable();
                Variables.VoucherTypeCodes = request.VoucherTypeCodes;

                // ====================================================
                // USER ID FROM JWT
                // ====================================================
                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = "error",
                        message = "User ID not found in token."
                    });
                }

                Variables.UserId = int.Parse(userIdClaim.Value);

                // ====================================================
                // DATABASE CONNECTION
                // ====================================================
                //DatabaseConnection databaseConnection =
                //    new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(Variables);
                string connectionString = _databaseConnection.GetConnectionString(Variables);

                db = new DBConn(connectionString);

                CommonFunction commonFunction =
                    new CommonFunction(db);

                bool connectionResult =
                    await commonFunction.CheckDatabaseConnection();

                if (!connectionResult)
                {
                    return StatusCode(500, new
                    {
                        status = "error",
                        message = "Database connection failed."
                    });
                }

                // ====================================================
                // SET VARIABLE VALUES
                // ====================================================
                await commonFunction.SetVariableValues(Variables);

                commonFunction.Variables = Variables;
                commonFunction.Functions.Variables = Variables;

                // ====================================================
                // PRODUCT DB
                // ====================================================
                PrMastDB prMastDB = new PrMastDB(db);
                prMastDB.Variables = Variables;

                if (prMastDB.Functions != null)
                {
                    prMastDB.Functions.Variables = Variables;
                }

                // ====================================================
                // OPENING STOCK DB
                // ====================================================
                OpnStkDB opnStkDB = new OpnStkDB(db);
                opnStkDB.Variables = Variables;

                // ====================================================
                // ALIAS DB
                // ====================================================
                PrAliasDB prAliasDB = new PrAliasDB(db);
                prAliasDB.Variables = Variables;

                // ====================================================
                // PRICE CATEGORY DB
                // ====================================================
                PriceCatLevDB priceCatLevDB =
                    new PriceCatLevDB(db);

                priceCatLevDB.Variables = Variables;

                if (priceCatLevDB.Functions != null)
                {
                    priceCatLevDB.Functions.Variables = Variables;
                }

                // ====================================================
                // PRODUCT IMAGE DB
                // ====================================================
                PrImgDB prImgDB = new PrImgDB(db);
                prImgDB.Variables = Variables;

                if (prImgDB.Functions != null)
                {
                    prImgDB.Functions.Variables = Variables;
                }

                // ====================================================
                // MAP PRODUCT
                // ====================================================
                ClsPrMast clsPrMast = MapToCls(request);

                Console.WriteLine(
                 $"Incoming PrName: [{request.PrName}]");

                Console.WriteLine(
                    $"Mapped PrName: [{clsPrMast.PrName}]");

                // ====================================================
                // ALIASES SENT
                // ====================================================
                bool aliasesSent = request.Aliases != null;

                // ====================================================
                // VALIDATION - BEFORE TRANSACTION
                // ====================================================
                ProductValidationResult validation =
                    await ValidateProduct(
                        clsPrMast,
                        request,
                        prMastDB,
                        prAliasDB,
                        0,
                        false,
                        aliasesSent,
                        Variables
                    );

                if (!validation.IsValid)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = validation.Message
                    });
                }

                // ====================================================
                // BEGIN ATOMIC PRODUCT CREATION
                // ====================================================
                await db.BeginTransaction();

                try
                {
                    // =================================================
                    // 1. INSERT PRODUCT
                    // =================================================
                    int productCode =
                        await prMastDB.Insert(clsPrMast);

                    if (productCode <= 0)
                    {
                        db.RollbackTransaction();

                        return BadRequest(new
                        {
                            status = false,
                            message = "Product creation failed."
                        });
                    }

                    // =================================================
                    // 2. SAVE OPENING STOCK
                    // =================================================
                    var openingStockResult =
                        await SaveOpeningStock(
                            opnStkDB,
                            request,
                            productCode,
                            Variables
                        );

                    if (!openingStockResult.Success)
                    {
                        db.RollbackTransaction();

                        return BadRequest(new
                        {
                            status = false,
                            message =
                                "Product creation failed because opening stock could not be saved. All changes were rolled back.",
                            error = openingStockResult.Error
                        });
                    }

                    // =================================================
                    // 3. SAVE ALIASES
                    // =================================================
                    bool aliasesSaved =
                        await SaveAliases(
                            prAliasDB,
                            request,
                            productCode
                        );

                    if (!aliasesSaved)
                    {
                        db.RollbackTransaction();

                        return BadRequest(new
                        {
                            status = false,
                            message =
                                "Product creation failed because aliases could not be saved. All changes were rolled back."
                        });
                    }

                    // =================================================
                    // 4. SAVE PRICE CATEGORIES
                    // =================================================
                    bool priceCategorySaved =
                        await SavePriceCategory(
                            priceCatLevDB,
                            request,
                            productCode
                        );

                    if (!priceCategorySaved)
                    {
                        db.RollbackTransaction();

                        return BadRequest(new
                        {
                            status = false,
                            message =
                                "Product creation failed because price categories could not be saved. All changes were rolled back."
                        });
                    }

                    // =================================================
                    // 5. SAVE IMAGE
                    // =================================================
                    if (!string.IsNullOrWhiteSpace(request.Image))
                    {
                        int imageResult = await prImgDB.Insert(productCode, request.Image);
                        if (imageResult == -5)
                        {
                            db.RollbackTransaction();

                            return BadRequest(new
                            {
                                status = false,
                                message = "Product creation failed because the image could not be saved. All changes were rolled back."
                            });
                        }
                    }

                    // =================================================
                    // COMMIT - ONLY AFTER EVERY OPERATION SUCCEEDS
                    // ==================================================            
                    db.CommitTransaction();

                    return Ok(new
                    {
                        status = true,
                        message = "Product created successfully",
                        prCode = productCode
                    });
                }
                catch
                {
                    // Any unexpected exception during creation:
                    // roll back all writes in this transaction.
                    try
                    {
                        db.RollbackTransaction();
                    }
                    catch
                    {
                        // Preserve the original exception.
                    }

                    throw;
                }
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Database error occurred.",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Unexpected error occurred.",
                    error = ex.Message
                });
            }
            finally
            {
                db?.Dispose();
            }
        }

        [HttpPut("products/{id:int}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] JsonElement requestJson, [FromQuery] string voucherTypeCodes = "")
        {
            try
            {
                // ====================================================
                // CONVERT JSON TO PRODUCT REQUEST
                // ====================================================

                ProductRequest? request =
                    JsonSerializer.Deserialize<ProductRequest>(
                        requestJson.GetRawText(),
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (request == null)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid product request."
                    });
                }

                // ====================================================
                // ROWVERSION REQUIRED
                // ====================================================

                if (request.RowVersion == null || request.RowVersion.Length != 8)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "A valid RowVersion is required. Reload the product and try again."
                    });
                }

                // ====================================================
                // LANGUAGE
                // ====================================================

                LngTB.InitLng();

                // ====================================================
                // VARIABLES
                // ====================================================

                Variable Variables =
                    new Variable();

                Variables.VoucherTypeCodes =
                    voucherTypeCodes;

                // ====================================================
                // USER ID FROM JWT
                // ====================================================

                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = "error",
                        message = "User ID not found in token."
                    });
                }

                Variables.UserId =
                    int.Parse(userIdClaim.Value);

                // ====================================================
                // DATABASE CONNECTION
                // ====================================================

                //DatabaseConnection databaseConnection =
                //new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(
                //        Variables
                //    );

                string connectionString = _databaseConnection.GetConnectionString(Variables);

                DBConn db =
                    new DBConn(connectionString);

                CommonFunction commonFunction =
                    new CommonFunction(db);

                bool connectionResult =
                    await commonFunction.CheckDatabaseConnection();

                if (!connectionResult)
                {
                    return StatusCode(500, new
                    {
                        status = "error",
                        message = "Database connection failed."
                    });
                }

                // ====================================================
                // SET VARIABLE VALUES
                // ====================================================

                await commonFunction.SetVariableValues(
                    Variables
                );

                commonFunction.Variables =
                    Variables;

                commonFunction.Functions.Variables =
                    Variables;

                // ====================================================
                // PRODUCT DB
                // ====================================================

                PrMastDB prMastDB = new PrMastDB(db); prMastDB.Variables = Variables;

                if (prMastDB.Functions != null)
                {
                    prMastDB.Functions.Variables = Variables;
                }

                // ====================================================
                // OPENING STOCK DB
                // ====================================================

                OpnStkDB opnStkDB = new OpnStkDB(db);

                opnStkDB.Variables = Variables;

                // ====================================================
                // ALIAS DB
                // ====================================================

                PrAliasDB prAliasDB =
                    new PrAliasDB(db);

                prAliasDB.Variables =
                    Variables;

                // ====================================================
                // PRICE CATEGORY DB
                // ====================================================

                PriceCatLevDB priceCatLevDB =
                    new PriceCatLevDB(db);

                priceCatLevDB.Variables =
                    Variables;

                if (priceCatLevDB.Functions != null)
                {
                    priceCatLevDB.Functions.Variables =
                        Variables;
                }

                // ====================================================
                // PRODUCT IMAGE DB
                // ====================================================

                PrImgDB prImgDB = new PrImgDB(db);
                prImgDB.Variables = Variables;

                if (prImgDB.Functions != null)
                {
                    prImgDB.Functions.Variables =
                        Variables;
                }

                // ====================================================
                // GET EXISTING PRODUCT
                // ====================================================

                ClsPrMast searchProduct =
                    new ClsPrMast();

                searchProduct.PrCode =
                    id;

                DataTable existingProductTable =
                    await prMastDB.SearchByCode(
                        searchProduct
                    );

                if (existingProductTable == null ||
                    existingProductTable.Rows.Count == 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Product not found."
                    });
                }

                // ====================================================
                // MAP EXISTING PRODUCT
                // ====================================================

                ClsPrMast clsPrMast =
                    MapDataRowToCls(
                        existingProductTable.Rows[0]
                    );

                clsPrMast.PrCode =
                    id;

                // ====================================================
                // EXISTING STOCK VALUES
                // ====================================================

                decimal existingStock = clsPrMast.Stock;

                decimal existingOpeningStock = clsPrMast.OpnStock;

                // ====================================================
                // CHECK OPENING STOCK FIELDS
                // ====================================================

                bool openingStockQtySent = HasProperty(requestJson, "OpnStkQty");

                bool openingStockValueSent = HasProperty(requestJson, "OpnStkValue");

                bool openingStockRateSent = HasProperty(requestJson, "OpnStkRate");

                bool openingStockOtherFieldsSent = HasAnyProperty(requestJson, "GdwCode", "BrCd");

                bool openingStockFieldsSent =
                    openingStockQtySent ||
                    openingStockValueSent ||
                    openingStockRateSent ||
                    openingStockOtherFieldsSent;

                // ====================================================
                // CHECK PRODUCT IMAGE
                // ====================================================

                bool imageSent = HasProperty(requestJson, "Image");
                // ====================================================
                // APPLY ONLY FIELDS SENT IN JSON
                // ====================================================
                ApplyProvidedFields(clsPrMast, request, requestJson);

                // ====================================================
                // PRODUCT CODE MUST ALWAYS BE URL ID
                // ====================================================
                clsPrMast.PrCode = id;
                // ====================================================
                // OPENING STOCK CALCULATION
                // ====================================================
                if (openingStockQtySent)
                {
                    clsPrMast.Stock = existingStock - existingOpeningStock + clsPrMast.OpnStock;
                }
                // ====================================================
                // CHECK WHETHER ALIASES WERE SENT
                // ====================================================
                bool aliasesSent = HasProperty(requestJson, "Aliases");

                // ====================================================
                // VALIDATION
                // ====================================================

                ProductValidationResult validation =
                    await ValidateProduct(clsPrMast, request, prMastDB, prAliasDB, id, true, aliasesSent, Variables);

                if (!validation.IsValid)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = validation.Message
                    });
                }

                // ====================================================
                // UPDATE PRODUCT
                // ====================================================

                int updateResult = await prMastDB.Update(clsPrMast, request.RowVersion);

                if (updateResult <= 0)
                {
                    // The row may have changed or been deleted after the GET.
                    DataTable currentProductTable =
                        await prMastDB.SearchByCode(searchProduct);

                    if (currentProductTable == null ||
                        currentProductTable.Rows.Count == 0)
                    {
                        return BadRequest(new
                        {
                            status = false,
                            message = "Product not found or has been deleted."
                        });
                    }

                    return Conflict(new
                    {
                        status = false,
                        message = "This product was modified by another user. Reload the latest product data before saving again."
                    });
                }

                // ====================================================
                // OPENING STOCK
                // ====================================================

                if (openingStockFieldsSent)
                {
                    var openingStockResult =
                        await SaveOpeningStock(opnStkDB, request, id, Variables, requestJson);

                    if (!openingStockResult.Success)
                    {
                        return BadRequest(new
                        {
                            status = false,
                            message =
                                "Product updated but opening stock could not be saved.",
                            error =
                                openingStockResult.Error,
                            prCode =
                                id
                        });
                    }
                }

                // ====================================================
                // ALIASES
                // ====================================================

                if (aliasesSent)
                {
                    bool aliasesSaved =
                        await SaveAliases(
                            prAliasDB,
                            request,
                            id
                        );

                    if (!aliasesSaved)
                    {
                        return BadRequest(new
                        {
                            status = false,
                            message =
                                "Product updated but aliases could not be saved.",
                            prCode =
                                id
                        });
                    }
                }

                // ====================================================
                // PRICE CATEGORIES
                // ====================================================

                bool priceCategoriesSent =
                    HasProperty(
                        requestJson,
                        "PriceCategories"
                    );

                if (priceCategoriesSent)
                {
                    bool priceCategorySaved =
                        await SavePriceCategory(
                            priceCatLevDB,
                            request,
                            id
                        );

                    if (!priceCategorySaved)
                    {
                        return BadRequest(new
                        {
                            status = false,
                            message =
                                "Product updated but price categories could not be saved.",
                            prCode =
                                id
                        });
                    }
                }

                // ====================================================
                // PRODUCT IMAGE
                // ====================================================

                if (imageSent)
                {
                    if (!string.IsNullOrWhiteSpace(request.Image))
                    {
                        int imageUpdateResult =
                            await prImgDB.Update(
                                id,
                                request.Image
                            );

                        if (imageUpdateResult <= 0)
                        {
                            int imageInsertResult =
                                await prImgDB.Insert(
                                    id,
                                    request.Image
                                );

                            if (imageInsertResult <= 0)
                            {
                                return BadRequest(new
                                {
                                    status = false,
                                    message =
                                        "Product updated but image could not be saved.",
                                    prCode =
                                        id
                                });
                            }
                        }
                    }
                }

                // ====================================================
                // SUCCESS
                // ====================================================

                return Ok(new
                {
                    status = true,
                    message =
                        "Product updated successfully",
                    prCode =
                        id
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message =
                        "Database error occurred.",
                    error =
                        ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message =
                        "Unexpected error occurred.",
                    error =
                        ex.Message
                });
            }
        }

        // ============================================================
        // DELETE PRODUCT
        // ============================================================

        [HttpDelete("products/{id:int}")]
        public async Task<IActionResult> DeleteProduct(
     int id,
     [FromQuery] string voucherTypeCodes = "")
        {
            try
            {
                // ====================================================
                // LANGUAGE
                // ====================================================

                LngTB.InitLng();

                // ====================================================
                // VARIABLES
                // ====================================================

                Variable Variables =
                    new Variable();

                Variables.VoucherTypeCodes =
                    voucherTypeCodes;

                // ====================================================
                // USER ID FROM JWT
                // ====================================================

                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = "error",
                        message = "User ID not found in token."
                    });
                }

                Variables.UserId =
                    int.Parse(userIdClaim.Value);

                // ====================================================
                // DATABASE CONNECTION
                // ====================================================

                //DatabaseConnection databaseConnection =
                //    new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(
                //        Variables
                //    );
                string connectionString = _databaseConnection.GetConnectionString(Variables);

                DBConn db =
                    new DBConn(connectionString);

                CommonFunction commonFunction =
                    new CommonFunction(db);

                bool connectionResult =
                    await commonFunction.CheckDatabaseConnection();

                if (!connectionResult)
                {
                    return StatusCode(500, new
                    {
                        status = "error",
                        message = "Database connection failed."
                    });
                }

                // ====================================================
                // SET VARIABLE VALUES
                // ====================================================

                await commonFunction.SetVariableValues(
                    Variables
                );

                commonFunction.Variables =
                    Variables;

                commonFunction.Functions.Variables =
                    Variables;

                // ====================================================
                // PRODUCT DB
                // ====================================================

                PrMastDB prMastDB =
                    new PrMastDB(db);

                prMastDB.Variables =
                    Variables;

                if (prMastDB.Functions != null)
                {
                    prMastDB.Functions.Variables =
                        Variables;
                }

                // ====================================================
                // GET PRODUCT
                // ====================================================

                ClsPrMast clsPrMast =
                    new ClsPrMast();

                clsPrMast.PrCode =
                    id;

                DataTable productTable =
                    await prMastDB.SearchByCode(
                        clsPrMast
                    );

                if (productTable == null ||
                    productTable.Rows.Count == 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Product not found."
                    });
                }

                // ====================================================
                // CHECK PRODUCT USAGE
                // ====================================================

                bool productUsed =
                    await GetProductUsedStat(
                        db,
                        id,
                        Variables
                    );

                if (productUsed)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message =
                            "Transactions found for this product. Can't delete product.",
                        prCode = id
                    });
                }

                // ====================================================
                // SOFT DELETE
                // ====================================================

                clsPrMast.PrCode =
                    id;

                clsPrMast.DelFlg =
                    true;

                clsPrMast.Active =
                    false;

                int deleteResult =
                    await prMastDB.UpdateForDel(
                        clsPrMast
                    );

                if (deleteResult <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message =
                            "Product deletion failed.",
                        prCode = id
                    });
                }

                // ====================================================
                // SUCCESS
                // ====================================================

                return Ok(new
                {
                    status = true,
                    message =
                        "Product deleted successfully",
                    prCode =
                        id
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message =
                        "Database error occurred.",
                    error =
                        ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message =
                        "Unexpected error occurred.",
                    error =
                        ex.Message
                });
            }
        }

        // ============================================================
        // CHECK PRODUCT USAGE
        // ============================================================

        private async Task<bool> GetProductUsedStat(DBConn db, int productCode, Variable Variables)
        {
            // ============================================================
            // 1. BOMT
            // ============================================================

            BomTDB bomTDB =
                new BomTDB(db);

            bomTDB.Variables = Variables;

            ClsBomT clsBomT =
                new ClsBomT();

            clsBomT.PrCode =
                productCode;

            DataTable bomTable =
                await bomTDB.SearchByPrCode(
                    clsBomT
                );

            if (bomTable != null &&
                bomTable.Rows.Count > 0)
            {
                return true;
            }

            // ============================================================
            // 2. STKDT
            // ============================================================

            StkDtDB stkDtDB =
                new StkDtDB(db);

            stkDtDB.Variables = Variables;

            ClsStkDt clsStkDt =
                new ClsStkDt();

            clsStkDt.PrCode =
                productCode;

            DataTable stkTable =
                await stkDtDB.SearchByPrCode(
                    clsStkDt
                );

            if (stkTable != null &&
                stkTable.Rows.Count > 0)
            {
                return true;
            }

            // ============================================================
            // 3. PRST
            // ============================================================

            PrstDB prstDB =
                new PrstDB(db);

            prstDB.Variables = Variables;

            ClsPrst clsPrst =
                new ClsPrst();

            clsPrst.PrCode =
                productCode;

            DataTable prstTable =
                await prstDB.SearchByPrCode(
                    clsPrst
                );

            if (prstTable != null &&
                prstTable.Rows.Count > 0)
            {
                return true;
            }

            // ============================================================
            // 4. SNTSTK
            // ============================================================

            SntStkDB sntStkDB =
                new SntStkDB(db);

            sntStkDB.Variables = Variables;

            ClsSntStk clsSntStk =
                new ClsSntStk();

            clsSntStk.PRCODE =
                productCode;

            DataTable sntStkTable =
                await sntStkDB.SearchByPrCode(
                    clsSntStk
                );

            if (sntStkTable != null &&
                sntStkTable.Rows.Count > 0)
            {
                return true;
            }

            // ============================================================
            // 5. CORDT
            // ============================================================

            CordTDB cordTDB =
                new CordTDB(db);

            cordTDB.Variables = Variables;

            ClsCordT clsCordT =
                new ClsCordT();

            clsCordT.PrCode =
                productCode;

            DataTable cordTable =
                await cordTDB.SearchByPrCode2(
                    clsCordT
                );

            if (cordTable != null &&
                cordTable.Rows.Count > 0)
            {
                return true;
            }

            // ============================================================
            // 6. PARTYDBCR
            // ============================================================

            PartyDbCrDB partyDbCrDB =
                new PartyDbCrDB(db);

            partyDbCrDB.Variables = Variables;

            ClsPartyDbCr clsPartyDbCr =
                new ClsPartyDbCr();

            clsPartyDbCr.PRCODE =
                productCode;

            DataTable partyDbCrTable =
                await partyDbCrDB.SearchByPrCode2(
                    clsPartyDbCr
                );

            if (partyDbCrTable != null &&
                partyDbCrTable.Rows.Count > 0)
            {
                return true;
            }

            // ============================================================
            // 7. PURT
            // ============================================================

            PurTDB purTDB =
                new PurTDB(db);

            purTDB.Variables = Variables;

            ClsPurT clsPurT =
                new ClsPurT();

            clsPurT.PrCode =
                productCode;

            DataTable purTable =
                await purTDB.SearchByPrCode2(
                    clsPurT
                );

            if (purTable != null &&
                purTable.Rows.Count > 0)
            {
                return true;
            }

            // ============================================================
            // 8. PURTPHY
            // ============================================================

            DataTable purPhyTable =
                await purTDB.SearchByPrCode_phy2(
                    clsPurT
                );

            if (purPhyTable != null &&
                purPhyTable.Rows.Count > 0)
            {
                return true;
            }

            // ============================================================
            // PRODUCT NOT USED
            // ============================================================

            return false;
        }

        private async Task<(bool Success, string Error)> SaveOpeningStock(
              OpnStkDB opnStkDB,
             ProductRequest request,
                int productCode,
                Variable Variables,
             JsonElement? requestJson = null)
        {
            string operation = "Starting opening stock save";

            try
            {
                WriteBulkDebugLog(
                    $"OPENING STOCK: Start. ProductCode={productCode}, " +
                    $"ProductName=[{request.PrName}]");

                // ============================================================
                // 1. SEARCH EXISTING OPENING STOCK
                // ============================================================

                operation = "SearchByPrCode";

                WriteBulkDebugLog(
                    $"OPENING STOCK: Before SearchByPrCode. " +
                    $"ProductCode={productCode}, BranchCode={request.BrCd}");

                ClsOpnStk searchOpeningStock = new ClsOpnStk
                {
                    PrCode = productCode,
                    BrCd = request.BrCd
                };

                DataTable existingOpeningStockTable =
                    await opnStkDB.SearchByPrCode(searchOpeningStock);

                WriteBulkDebugLog(
                    $"OPENING STOCK: SearchByPrCode completed. " +
                    $"ProductCode={productCode}, " +
                    $"Rows={existingOpeningStockTable?.Rows.Count ?? 0}");

                bool openingStockExists =
                    existingOpeningStockTable != null &&
                    existingOpeningStockTable.Rows.Count > 0;

                // ============================================================
                // 2. PREPARE OPENING STOCK OBJECT
                // ============================================================

                operation = "Preparing opening stock";

                ClsOpnStk clsOpnStk = new ClsOpnStk
                {
                    PrCode = productCode
                };

                // ============================================================
                // 3. PRESERVE EXISTING VALUES
                // ============================================================

                if (openingStockExists)
                {
                    DataRow row = existingOpeningStockTable!.Rows[0];

                    clsOpnStk.GdwCode = GetValue<int>(row, "GDWCODE");
                    clsOpnStk.Uom = GetValue<int>(row, "UOM");
                    clsOpnStk.Qty = GetValue<decimal>(row, "QTY");
                    clsOpnStk.Rate = GetValue<decimal>(row, "RATE");
                    clsOpnStk.StVal = GetValue<decimal>(row, "STVALUE");
                    clsOpnStk.ConvQty = GetValue<decimal>(row, "CONVQTY");
                    clsOpnStk.ConvValue = GetValue<decimal>(row, "CONVVALUE");
                    clsOpnStk.Batch = GetValue<string>(row, "BATCH");
                    clsOpnStk.Mrp = GetValue<decimal>(row, "MRP");
                    clsOpnStk.Status = GetValue<string>(row, "STATUS");
                    clsOpnStk.CostcCd = GetValue<int>(row, "COSTCCD");
                    clsOpnStk.SalRate = GetValue<decimal>(row, "SALRATE");
                    clsOpnStk.Expiry = GetValue<DateTime>(row, "EXPIRY");
                    clsOpnStk.MfgDate = GetValue<DateTime>(row, "MFGDATE");
                    clsOpnStk.ExpChk = GetValue<bool>(row, "EXPCHECK");
                    clsOpnStk.BrCd = GetValue<int>(row, "BRCD");
                }

                // ============================================================
                // 4. APPLY REQUEST VALUES
                // ============================================================

                operation = "Applying request values";

                if (!requestJson.HasValue)
                {
                    clsOpnStk.GdwCode = request.GdwCode;
                    clsOpnStk.Uom = request.Unit;
                    clsOpnStk.Qty = request.OpnStkQty;
                    clsOpnStk.Rate = request.OpnStkRate;
                    clsOpnStk.StVal = request.OpnStkValue;
                    clsOpnStk.ConvQty = request.OpnStkQty;
                    clsOpnStk.ConvValue = request.OpnStkValue;
                    clsOpnStk.Mrp = request.BaseSalePrice;
                    clsOpnStk.SalRate = request.BaseSalePrice;
                    clsOpnStk.BrCd = request.BrCd;
                }
                else
                {
                    JsonElement json = requestJson.Value;

                    if (HasProperty(json, "GdwCode"))
                    {
                        clsOpnStk.GdwCode = request.GdwCode;
                    }

                    if (HasProperty(json, "Unit"))
                    {
                        clsOpnStk.Uom = request.Unit;
                    }

                    if (HasProperty(json, "OpnStkQty"))
                    {
                        clsOpnStk.Qty = request.OpnStkQty;
                        clsOpnStk.ConvQty = request.OpnStkQty;
                    }

                    if (HasProperty(json, "OpnStkRate"))
                    {
                        clsOpnStk.Rate = request.OpnStkRate;
                    }

                    if (HasProperty(json, "OpnStkValue"))
                    {
                        clsOpnStk.StVal = request.OpnStkValue;
                        clsOpnStk.ConvValue = request.OpnStkValue;
                    }

                    if (HasProperty(json, "BaseSalePrice"))
                    {
                        clsOpnStk.Mrp = request.BaseSalePrice;
                        clsOpnStk.SalRate = request.BaseSalePrice;
                    }

                    if (HasProperty(json, "BrCd"))
                    {
                        clsOpnStk.BrCd = request.BrCd;
                    }
                }

                // ============================================================
                // 5. APPLY DEFAULT VALUES
                // ============================================================

                operation = "Setting opening stock defaults";

                clsOpnStk.Batch ??= "";

                if (string.IsNullOrWhiteSpace(clsOpnStk.Status))
                {
                    clsOpnStk.Status = "Updated";
                }

                if (clsOpnStk.Expiry == default)
                {
                    clsOpnStk.Expiry = Variables.fyStartDate;
                }

                if (clsOpnStk.MfgDate == default)
                {
                    clsOpnStk.MfgDate = Variables.fyStartDate;
                }

                WriteBulkDebugLog(
                    $"OPENING STOCK: Values prepared. " +
                    $"ProductCode={productCode}, " +
                    $"Exists={openingStockExists}, " +
                    $"Qty={clsOpnStk.Qty}, " +
                    $"Rate={clsOpnStk.Rate}, " +
                    $"Warehouse={clsOpnStk.GdwCode}, " +
                    $"Branch={clsOpnStk.BrCd}");

                // ============================================================
                // 6. UPDATE EXISTING OPENING STOCK
                // ============================================================

                if (openingStockExists)
                {
                    operation = "UpdateByPrCode";

                    WriteBulkDebugLog(
                        $"OPENING STOCK: Before UpdateByPrCode. " +
                        $"ProductCode={productCode}");

                    int updateResult =
                        await opnStkDB.UpdateByPrCode(clsOpnStk);

                    WriteBulkDebugLog(
                        $"OPENING STOCK: UpdateByPrCode returned " +
                        $"{updateResult}. ProductCode={productCode}");

                    if (updateResult == -5)
                    {
                        return (
                            false,
                            "Opening stock update failed due to a database error."
                        );
                    }

                    WriteBulkDebugLog(
                        $"OPENING STOCK: Update completed. " +
                        $"ProductCode={productCode}");

                    return (true, "");
                }

                // ============================================================
                // 7. INSERT NEW OPENING STOCK
                // ============================================================

                operation = "Insert";

                WriteBulkDebugLog(
                    $"OPENING STOCK: Before Insert. ProductCode={productCode}");

                int insertResult =
                    await opnStkDB.Insert(clsOpnStk);

                WriteBulkDebugLog(
                    $"OPENING STOCK: Insert returned " +
                    $"{insertResult}. ProductCode={productCode}");

                if (insertResult == -5 || insertResult == 0)
                {
                    return (
                        false,
                        "Opening stock insert failed."
                    );
                }

                WriteBulkDebugLog(
                    $"OPENING STOCK: Insert completed. " +
                    $"ProductCode={productCode}");

                return (true, "");
            }
            catch (Exception ex)
            {
                WriteBulkDebugLog(
                    $"OPENING STOCK FAILED: " +
                    $"Operation=[{operation}], " +
                    $"ProductCode={productCode}, " +
                    $"ProductName=[{request.PrName}], " +
                    $"Exception=[{ex}]");

                return (false, $"{operation}: {ex.Message}");
            }
        }

        // ============================================================
        // SAVE ALIASES
        // ============================================================
        private async Task<bool> SaveAliases(
            PrAliasDB prAliasDB,
            ProductRequest request,
            int productCode)
        {
            try
            {
                // ====================================================
                // DELETE EXISTING ALIASES
                // ====================================================

                ClsPrAlias deleteAlias = new ClsPrAlias
                {
                    PrCode = productCode
                };

                int deleteResult =
                    await prAliasDB.DeleteByPrCode(deleteAlias);

                if (deleteResult == -5)
                    return false;

                // ====================================================
                // NO ALIASES
                // ====================================================

                if (request.Aliases == null ||
                    request.Aliases.Count == 0)
                {
                    return true;
                }

                // ====================================================
                // INSERT ALIASES
                // ====================================================

                foreach (var alias in request.Aliases)
                {
                    if (alias == null ||
                        string.IsNullOrWhiteSpace(alias.AlName))
                    {
                        continue;
                    }

                    ClsPrAlias clsPrAlias = new ClsPrAlias
                    {
                        Code = alias.Code,
                        PrCode = productCode,
                        AlCode = alias.AlCode,
                        AlName = alias.AlName,
                        Qty = alias.Qty,
                        UnitCode = alias.UnitCode,
                        SalesPrice = alias.SalesPrice,
                        PurPrice = alias.PurPrice,
                        AlType = alias.AlType,
                        InvUnitCode = alias.InvUnitCode,
                        Active = alias.Active
                    };

                    int result =
                        await prAliasDB.Insert(clsPrAlias);

                    // Alias Insert returns an inserted identifier.
                    if (result <= 0)
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }


        // ============================================================
        // SAVE PRICE CATEGORY
        // ============================================================
        private async Task<bool> SavePriceCategory(
            PriceCatLevDB priceCatLevDB,
            ProductRequest request,
            int productCode)
        {
            try
            {
                // ====================================================
                // DELETE EXISTING PRICE CATEGORIES
                // ====================================================

                ClsPriceCatLev delete = new ClsPriceCatLev
                {
                    PrCode = productCode
                };

                int deleteResult =
                    await priceCatLevDB.DeleteByPrCode(delete);

                if (deleteResult == -5)
                    return false;

                // ====================================================
                // NO PRICE CATEGORIES
                // ====================================================

                if (request.PriceCategories == null ||
                    request.PriceCategories.Count == 0)
                {
                    return true;
                }

                // ====================================================
                // INSERT PRICE CATEGORIES
                // ====================================================

                foreach (var priceCategory in request.PriceCategories)
                {
                    if (priceCategory == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(priceCategory.CatName) ||
                        priceCategory.CatCode <= 0)
                    {
                        continue;
                    }

                    ClsPriceCatLev cls = new ClsPriceCatLev
                    {
                        Code = priceCategory.Code,
                        PrCode = productCode,
                        CatName = priceCategory.CatName,
                        DiscStruct = priceCategory.DiscStruct,
                        LevFrom = priceCategory.LevFrom,
                        LevTo = priceCategory.LevTo,
                        DiscPerc = priceCategory.DiscPerc,
                        EffValue = priceCategory.EffValue,
                        CatCode = priceCategory.CatCode,
                        AppPrice = priceCategory.AppPrice
                    };

                    int result =
                        await priceCatLevDB.Insert(cls);

                    // Execute() may return 0 or -1 for a successful
                    // statement, so only -5 indicates failure here.
                    if (result == -5)
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
        // ============================================================
        // MAP PRODUCT REQUEST TO CLASS
        // ============================================================

        private ClsPrMast MapToCls(
     ProductRequest request)
        {
            ClsPrMast cls =
                new ClsPrMast();

            cls.PrCode =
                request.PrCode;

            cls.PrName =
                request.PrName;

            cls.Alias =
                request.Alias;

            cls.PrintName =
                request.PrintName;

            cls.Desc =
                request.Desc;

            cls.UpcCode =
                request.UpcCode;

            cls.SkuCode =
                request.SkuCode;

            cls.BarCode =
                request.BarCode;

            cls.GroupCode =
                request.GroupCode;

            cls.CatgCode =
                request.CatgCode;

            cls.Brand =
                request.Brand;

            cls.Unit =
                request.Unit;

            cls.AltUnit =
                request.AltUnit;

            cls.UnitConv =
                request.UnitConv;

            cls.ConvType =
                request.ConvType;

            // ====================================================
            // STOCK
            // ====================================================

            cls.Stock =
                request.OpnStkQty;

            cls.OpnStock =
                request.OpnStkQty;

            cls.OpnStkval =
                request.OpnStkValue;

            cls.OpnStkRate =
                request.OpnStkRate;

            // ====================================================
            // PURCHASE
            // ====================================================

            cls.PurPriceType =
                request.PurPriceType;

            cls.BasePurPrice =
                request.BasePurPrice;

            cls.AltPurPrice =
                request.AltPurPrice;

            // ====================================================
            // SALES
            // ====================================================

            cls.SalesPriceType =
                request.SalesPriceType;

            cls.BaseSalePrice =
                request.BaseSalePrice;

            cls.BaseMinPrice =
                request.BaseMinPrice;

            cls.AltSalePrice =
                request.AltSalePrice;

            cls.AltMinPrice =
                request.AltMinPrice;

            // ====================================================
            // ACCOUNT / TAX
            // ====================================================

            cls.CostMeth =
                request.CostMeth;

            cls.TaxCatCode =
                request.TaxCatCode;

            cls.SaleAcCode =
                request.SaleAcCode;

            cls.PurAcCode =
                request.PurAcCode;

            // ====================================================
            // STOCK LEVEL
            // ====================================================

            cls.MinQty =
                request.MinQty;

            cls.ReOrdQty =
                request.ReOrdQty;

            cls.MaxQty =
                request.MaxQty;

            // ====================================================
            // FLAGS
            // ====================================================

            cls.ServFlg =
                request.ServFlg;

            cls.SupCode =
                request.SupCode;

            cls.BatchFlg =
                request.BatchFlg;

            cls.ManDateFlg =
                request.ManDateFlg;

            cls.ExpDateFlg =
                request.ExpDateFlg;

            cls.ExpDays =
                request.ExpDays;

            // ====================================================
            // PRICE CATEGORY
            // ====================================================

            cls.PriceCatFlg =
                request.PriceCatFlg;

            cls.Active =
                request.Active;

            cls.PriceCatCode =
                request.PriceCatCode;

            // ====================================================
            // OTHER
            // ====================================================

            cls.SecName =
                request.SecName;

            cls.CurConvRate =
                request.CurConvRate;

            cls.CurCode =
                request.CurCode;

            cls.ImgPath =
                request.ImgPath;

            cls.PosFlg =
                request.PosFlg;

            cls.Notes =
                request.Notes;

            // ====================================================
            // BMP
            // ====================================================

            cls._BMPL =
                request._BMPL;

            cls._BMPQ =
                request._BMPQ;

            cls._BMPW =
                request._BMPW;

            // ====================================================
            // MRP
            // ====================================================

            cls.BaseMrp =
                request.BaseMrp;

            cls.AltMrp =
                request.AltMrp;

            // ====================================================
            // OTHER FLAGS
            // ====================================================

            cls._SLNOTRACK =
                request._SLNOTRACK;

            cls._BMEQN =
                request._BMEQN;

            cls.APRVLST =
                request.APRVLST;

            return cls;
        }

        // ============================================================
        // MAP DATABASE ROW TO PRODUCT CLASS
        // ============================================================

        private ClsPrMast MapDataRowToCls(
    DataRow row)
        {
            ClsPrMast cls =
                new ClsPrMast();

            cls.PrCode = GetValue<int>(row, "PRCODE");

            cls.PrName = GetValue<string>(row, "PRNAME");

            cls.Alias = GetValue<string>(row, "ALIAS");

            cls.PrintName = GetValue<string>(row, "PRINTNAME");

            cls.Desc = GetValue<string>(row, "DESCRIPTION");

            cls.UpcCode = GetValue<string>(row, "UPCCODE");

            cls.SkuCode = GetValue<string>(row, "SKUCODE");

            cls.BarCode = GetValue<string>(row, "BARCODE");

            cls.GroupCode = GetValue<int>(row, "GROUPCODE");

            cls.CatgCode = GetValue<int>(row, "CATGCODE");

            cls.Brand = GetValue<string>(row, "BRAND");

            cls.Unit = GetValue<int>(row, "UNIT");

            cls.AltUnit = GetValue<int>(row, "ALTUNIT");

            cls.UnitConv =
                GetValue<decimal>(
                    row,
                    "UNITCONV"
                );

            cls.ConvType =
                GetValue<int>(
                    row,
                    "CONVTYPE"
                );

            cls.OpnStock =
                GetValue<decimal>(
                    row,
                    "OPNSTOCK"
                );

            cls.OpnStkval =
                GetValue<decimal>(
                    row,
                    "OPNSTKVAL"
                );

            cls.PurPriceType =
                GetValue<int>(
                    row,
                    "PURPRICETYPE"
                );

            cls.BasePurPrice =
                GetValue<decimal>(
                    row,
                    "BASEPURPRICE"
                );

            cls.AltPurPrice =
                GetValue<decimal>(
                    row,
                    "ALTPURPRICE"
                );

            cls.SalesPriceType =
                GetValue<int>(
                    row,
                    "SALESPRICETYPE"
                );

            cls.BaseSalePrice =
                GetValue<decimal>(
                    row,
                    "BASESALEPRICE"
                );

            cls.BaseMinPrice =
                GetValue<decimal>(
                    row,
                    "BASEMINPRICE"
                );

            cls.AltSalePrice =
                GetValue<decimal>(
                    row,
                    "ALTSALEPRICE"
                );

            cls.AltMinPrice =
                GetValue<decimal>(
                    row,
                    "ALTMINPRICE"
                );

            cls.CostMeth =
                GetValue<int>(
                    row,
                    "COSTMETH"
                );

            cls.TaxCatCode =
                GetValue<int>(
                    row,
                    "TAXCODE"
                );

            cls.SaleAcCode =
                GetValue<int>(
                    row,
                    "SALEACCODE"
                );

            cls.PurAcCode =
                GetValue<int>(
                    row,
                    "PURACCODE"
                );

            cls.MinQty =
                GetValue<decimal>(
                    row,
                    "MINQTY"
                );

            cls.ReOrdQty =
                GetValue<decimal>(
                    row,
                    "REORDQTY"
                );

            cls.MaxQty =
                GetValue<decimal>(
                    row,
                    "MAXQTY"
                );

            cls.ServFlg =
                GetValue<bool>(
                    row,
                    "SERVFLG"
                );

            cls.SupCode =
                GetValue<int>(
                    row,
                    "SUPCODE"
                );

            cls.BatchFlg =
                GetValue<bool>(
                    row,
                    "BATCHFLG"
                );

            cls.ManDateFlg =
                GetValue<bool>(
                    row,
                    "MANDATEFLG"
                );

            cls.ExpDateFlg =
                GetValue<bool>(
                    row,
                    "EXPDATEFLG"
                );

            cls.ExpDays =
                GetValue<decimal>(
                    row,
                    "EXPDAYS"
                );

            cls.PriceCatFlg =
                GetValue<bool>(
                    row,
                    "PRICECATFLG"
                );

            cls.Active =
                GetValue<bool>(
                    row,
                    "ACTIVE"
                );

            cls.Stock =
                GetValue<decimal>(
                    row,
                    "STOCK"
                );

            cls.SecName =
                GetValue<string>(
                    row,
                    "SECNAME"
                );

            cls.PriceCatCode =
                GetValue<int>(
                    row,
                    "PRICECATCODE"
                );

            cls.OpnStkRate =
                GetValue<decimal>(
                    row,
                    "OPNSTKRATE"
                );

            cls.CurConvRate =
                GetValue<decimal>(
                    row,
                    "CURCONVRATE"
                );

            cls.CurCode =
                GetValue<int>(
                    row,
                    "CURCODE"
                );

            cls.ImgPath =
                GetValue<string>(
                    row,
                    "IMGPATH"
                );

            cls.DelFlg =
                GetValue<bool>(
                    row,
                    "DELFLG"
                );

            cls.PosFlg =
                GetValue<bool>(
                    row,
                    "POSFLG"
                );

            cls.Notes =
                GetValue<string>(
                    row,
                    "PRNOTES"
                );

            cls._BMPL =
                GetValue<decimal>(
                    row,
                    "BMPL"
                );

            cls._BMPQ =
                GetValue<decimal>(
                    row,
                    "BMPQ"
                );

            cls._BMPW =
                GetValue<decimal>(
                    row,
                    "BMPW"
                );

            cls.BaseMrp =
                GetValue<decimal>(
                    row,
                    "BASEMRP"
                );

            cls.AltMrp =
                GetValue<decimal>(
                    row,
                    "ALTMRP"
                );

            cls._SLNOTRACK =
                GetValue<bool>(
                    row,
                    "SLNOTRACK"
                );

            cls._BMEQN =
                GetValue<string>(
                    row,
                    "BMEQN"
                );

            cls.APRVLST =
                GetValue<string>(
                    row,
                    "APRVLST"
                );

            return cls;
        }

        [HttpGet("products/{id:int}/aliases")]
        public async Task<IActionResult> GetProductAliases(int id)
        {
            try
            {
                // ====================================================
                // LANGUAGE
                // ====================================================

                LngTB.InitLng();

                // ====================================================
                // VARIABLES
                // ====================================================

                Variable Variables =
                    new Variable();

                // ====================================================
                // USER ID FROM JWT
                // ====================================================

                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = "error",
                        message = "User ID not found in token."
                    });
                }

                Variables.UserId =
                    int.Parse(userIdClaim.Value);

                // ====================================================
                // DATABASE CONNECTION
                // ====================================================

                //DatabaseConnection databaseConnection =
                //    new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(
                //        Variables
                //    );
                string connectionString = _databaseConnection.GetConnectionString(Variables);

                DBConn db =
                    new DBConn(connectionString);

                CommonFunction commonFunction =
                    new CommonFunction(db);

                bool connectionResult =
                    await commonFunction.CheckDatabaseConnection();

                if (!connectionResult)
                {
                    return StatusCode(500, new
                    {
                        status = "error",
                        message = "Database connection failed."
                    });
                }

                // ====================================================
                // SET VARIABLE VALUES
                // ====================================================

                await commonFunction.SetVariableValues(
                    Variables
                );

                commonFunction.Variables =
                    Variables;

                commonFunction.Functions.Variables =
                    Variables;

                // ====================================================
                // PRODUCT ALIAS DB
                // ====================================================

                PrAliasDB prAliasDB =
                    new PrAliasDB(db);

                prAliasDB.Variables =
                    Variables;

                // ====================================================
                // SEARCH BY PRODUCT CODE
                // ====================================================

                ClsPrAlias clsPrAlias =
                    new ClsPrAlias();

                clsPrAlias.PrCode =
                    id;

                DataTable aliasTable =
                    await prAliasDB.SearchByPrCode(
                        clsPrAlias
                    );

                // ====================================================
                // RESPONSE
                // ====================================================

                var aliases =
                    aliasTable.AsEnumerable()
                        .Select(row =>
                            aliasTable.Columns
                                .Cast<DataColumn>()
                                .ToDictionary(
                                    column =>
                                        column.ColumnName,
                                    column =>
                                        row[column] ==
                                        DBNull.Value
                                            ? null
                                            : row[column]
                                )
                        )
                        .ToList();

                return Ok(new
                {
                    status = true,
                    message =
                        "Product aliases retrieved successfully.",
                    data = aliases
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message =
                        "Database error occurred.",
                    error =
                        ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message =
                        "Unexpected error occurred.",
                    error =
                        ex.Message
                });
            }
        }

        [HttpPost("products/{id:int}/aliases")]
        public async Task<IActionResult> CreateProductAlias(
    int id,
    [FromBody] PrAliasRequest request)
        {
            try
            {
                // ====================================================
                // LANGUAGE
                // ====================================================

                LngTB.InitLng();

                // ====================================================
                // VARIABLES
                // ====================================================

                Variable Variables =
                    new Variable();

                // ====================================================
                // USER ID FROM JWT
                // ====================================================

                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = "error",
                        message = "User ID not found in token."
                    });
                }

                Variables.UserId =
                    int.Parse(userIdClaim.Value);

                // ====================================================
                // DATABASE CONNECTION
                // ====================================================

                //DatabaseConnection databaseConnection =
                //    new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(
                //        Variables
                //    );

                string connectionString = _databaseConnection.GetConnectionString(Variables);

                DBConn db =
                    new DBConn(connectionString);

                CommonFunction commonFunction =
                    new CommonFunction(db);

                bool connectionResult =
                    await commonFunction.CheckDatabaseConnection();

                if (!connectionResult)
                {
                    return StatusCode(500, new
                    {
                        status = "error",
                        message = "Database connection failed."
                    });
                }

                // ====================================================
                // SET VARIABLE VALUES
                // ====================================================

                await commonFunction.SetVariableValues(
                    Variables
                );

                commonFunction.Variables =
                    Variables;

                commonFunction.Functions.Variables =
                    Variables;

                // ====================================================
                // ALIAS DB
                // ====================================================

                PrAliasDB prAliasDB =
                    new PrAliasDB(db);

                prAliasDB.Variables =
                    Variables;

                // ====================================================
                // PRODUCT CHECK
                // ====================================================

                ClsPrMast clsPrMast =
                    new ClsPrMast();

                clsPrMast.PrCode =
                    id;

                PrMastDB prMastDB =
                    new PrMastDB(db);

                prMastDB.Variables =
                    Variables;

                DataTable productTable =
                    await prMastDB.SearchByCode(
                        clsPrMast
                    );

                if (productTable == null ||
                    productTable.Rows.Count == 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Product not found.",
                        prCode = id
                    });
                }

                // ====================================================
                // VALIDATION
                // ====================================================

                if (string.IsNullOrWhiteSpace(
                        request.AlName))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Alias name must not be blank."
                    });
                }

                // ====================================================
                // DUPLICATE ALIAS
                // ====================================================

                int aliasCount =
                    await prAliasDB.CheckOccurance(
                        "ALNAME",
                        request.AlName
                    );

                if (aliasCount > 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message =
                            "Alias already in Use: " +
                            request.AlName
                    });
                }

                // ====================================================
                // MAP REQUEST
                // ====================================================

                ClsPrAlias clsPrAlias =
                    new ClsPrAlias();

                clsPrAlias.PrCode =
                    id;

                clsPrAlias.AlCode =
                    request.AlCode;

                clsPrAlias.AlName =
                    request.AlName;

                clsPrAlias.Qty =
                    request.Qty;

                clsPrAlias.UnitCode =
                    request.UnitCode;

                clsPrAlias.SalesPrice =
                    request.SalesPrice;

                clsPrAlias.PurPrice =
                    request.PurPrice;

                clsPrAlias.Active =
                    request.Active;

                clsPrAlias.AlType =
                    request.AlType;

                clsPrAlias.InvUnitCode =
                    request.InvUnitCode;

                // ====================================================
                // INSERT
                // ====================================================

                int aliasCode =
                    await prAliasDB.Insert(
                        clsPrAlias
                    );

                if (aliasCode <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message =
                            "Alias creation failed.",
                        prCode = id
                    });
                }

                // ====================================================
                // SUCCESS
                // ====================================================

                return Ok(new
                {
                    status = true,
                    message =
                        "Product alias created successfully.",
                    code =
                        aliasCode,
                    prCode =
                        id
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message =
                        "Database error occurred.",
                    error =
                        ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message =
                        "Unexpected error occurred.",
                    error =
                        ex.Message
                });
            }
        }

        // ============================================================
        // APPLY ONLY JSON FIELDS THAT WERE ACTUALLY SENT
        // ============================================================

        private void ApplyProvidedFields(ClsPrMast cls, ProductRequest request, JsonElement requestJson)

        {
            PropertyInfo[] requestProperties =
                typeof(ProductRequest)
                    .GetProperties(
                        BindingFlags.Public |
                        BindingFlags.Instance
                    );

            PropertyInfo[] clsProperties =
                typeof(ClsPrMast)
                    .GetProperties(
                        BindingFlags.Public |
                        BindingFlags.Instance
                    );

            foreach (PropertyInfo requestProperty
                     in requestProperties)
            {
                // ====================================================
                // IGNORE NON PRODUCT FIELDS
                // ====================================================

                if (requestProperty.Name == "ServerName" ||
                  requestProperty.Name == "DatabaseName" ||
                  requestProperty.Name == "UserName" ||
                  requestProperty.Name == "Password" ||
                  requestProperty.Name == "CompCode" ||
                  requestProperty.Name == "UserId" ||
                  requestProperty.Name == "VoucherTypeCodes" ||
                  requestProperty.Name == "Aliases" ||
                  requestProperty.Name == "PriceCategories" ||
                  requestProperty.Name == "GdwCode" ||
                  requestProperty.Name == "BrCd" ||
                  requestProperty.Name == "OpnStkQty" ||
                  requestProperty.Name == "OpnStkValue" ||
                  requestProperty.Name == "RowVersion")
                {
                    continue;
                }

                // ====================================================
                // ONLY APPLY FIELDS ACTUALLY SENT
                // ====================================================

                if (!HasProperty(
                        requestJson,
                        requestProperty.Name))
                {
                    continue;
                }

                PropertyInfo? clsProperty =
                    clsProperties.FirstOrDefault(
                        x =>
                            string.Equals(
                                x.Name,
                                requestProperty.Name,
                                StringComparison.OrdinalIgnoreCase
                            )
                    );

                if (clsProperty == null ||
                    !clsProperty.CanWrite)
                {
                    continue;
                }

                object? value =
                    requestProperty.GetValue(request);

                clsProperty.SetValue(
                    cls,
                    value
                );
            }

            // ========================================================
            // OPENING STOCK QUANTITY
            // ========================================================

            if (HasProperty(
                    requestJson,
                    "OpnStkQty"))
            {
                cls.OpnStock =
                    request.OpnStkQty;
            }

            // ========================================================
            // OPENING STOCK VALUE
            // ========================================================

            if (HasProperty(
                    requestJson,
                    "OpnStkValue"))
            {
                cls.OpnStkval =
                    request.OpnStkValue;
            }

            // ========================================================
            // OPENING STOCK RATE
            // ========================================================

            if (HasProperty(
                    requestJson,
                    "OpnStkRate"))
            {
                cls.OpnStkRate =
                    request.OpnStkRate;
            }
        }
        // ============================================================
        // CHECK JSON PROPERTY
        // ============================================================

        private static bool HasProperty(
            JsonElement json,
            string propertyName)
        {
            if (json.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            foreach (JsonProperty property
                     in json.EnumerateObject())
            {
                if (string.Equals(
                    property.Name,
                    propertyName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // ============================================================
        // CHECK ANY JSON PROPERTY
        // ============================================================

        private static bool HasAnyProperty(
            JsonElement json,
            params string[] propertyNames)
        {
            foreach (string propertyName
                     in propertyNames)
            {
                if (HasProperty(
                    json,
                    propertyName))
                {
                    return true;
                }
            }

            return false;
        }

        // ============================================================
        // DATAROW VALUE
        // ============================================================

        private static T GetValue<T>(
            DataRow row,
            string columnName)
        {
            if (!row.Table.Columns.Contains(columnName))
            {
                return default!;
            }

            object value =
                row[columnName];

            if (value == DBNull.Value ||
                value == null)
            {
                return default!;
            }

            Type targetType =
                Nullable.GetUnderlyingType(
                    typeof(T)
                ) ?? typeof(T);

            if (targetType == typeof(string))
            {
                return (T)(object)value.ToString()!;
            }

            if (targetType == typeof(bool))
            {
                if (value is bool boolValue)
                {
                    return (T)(object)boolValue;
                }

                return (T)(object)
                    Convert.ToBoolean(value);
            }

            return (T)
                Convert.ChangeType(
                    value,
                    targetType
                );
        }

        // ============================================================
        // DATATABLE TO LIST
        // ============================================================

        private static List<Dictionary<string, object?>>
            ConvertDataTable(DataTable table)
        {
            var rows =
                new List<Dictionary<string, object?>>();

            if (table == null)
            {
                return rows;
            }

            foreach (DataRow row in table.Rows)
            {
                var dict =
                    new Dictionary<string, object?>();

                foreach (DataColumn column in table.Columns)
                {
                    object value =
                        row[column];

                    dict[column.ColumnName] =
                        value == DBNull.Value
                            ? null
                            : value;
                }

                rows.Add(dict);
            }

            return rows;
        }

        private async Task<ProductValidationResult> ValidateProduct(ClsPrMast product, ProductRequest request, PrMastDB prMastDB, PrAliasDB prAliasDB, int productCode, bool isUpdate, bool aliasesSent, Variable Variables)

        {
            // ============================================================
            // 1. PRODUCT NAME
            // ============================================================

            bool einvValidation =
              Variables.EInvoice &&
              Variables.EInvoice_sec &&
             (
             Variables.EinvoiceMode == "Direct" ||
            !string.IsNullOrWhiteSpace(Variables.APIURL)
             );

            // Product Name is always mandatory
            if (string.IsNullOrWhiteSpace(product.PrName))
            {
                return ProductValidationResult.Failed(
                    "PrName must not be blank");
            }

            // Product Name: always reject & and @
            if (product.PrName.Contains("&") ||
                product.PrName.Contains("@"))
            {
                return ProductValidationResult.Failed(
                    "Special characters '&' and '@' are not allowed in Product Name");
            }

            // Secondary Name: mandatory only for E-Invoice
            if (einvValidation &&
                string.IsNullOrWhiteSpace(product.SecName))
            {
                return ProductValidationResult.Failed(
                    "Secondary Name must not be blank");
            }

            // Secondary Name: validate only when a value is provided
            if (!string.IsNullOrWhiteSpace(product.SecName))
            {
                if (product.SecName.Contains("&") ||
                    product.SecName.Contains("@"))
                {
                    return ProductValidationResult.Failed(
                        "Special characters '&' and '@' are not allowed in Secondary Name");
                }
            }
            // ============================================================
            // 3. GROUP
            // ============================================================

            if (product.GroupCode < 1)
            {
                return ProductValidationResult.Failed(
                    "Invalid Group Selected");
            }

            // ============================================================
            // 4. BASE UNIT
            // ============================================================

            if (product.Unit < 1)
            {
                return ProductValidationResult.Failed(
                    "Invalid Unit Selected");
            }

            // ============================================================
            // 5. DUPLICATE PRODUCT NAME
            // ============================================================

            if (!string.IsNullOrWhiteSpace(product.PrName))
            {

                int count;

                if (!isUpdate)
                {
                    count = await prMastDB.CheckOccurance(
                        "PRNAME",
                        product.PrName
                    );
                }

                else
                {
                    count = await prMastDB.CheckExistName(
                        "PRNAME",
                        product.PrName,
                        "PRCODE",
                        productCode
                    );
                }

                if (count > 0)
                {
                    return ProductValidationResult.Failed(
                        "Master Name already in Use");
                }
            }

            // ============================================================
            // 6. DUPLICATE MAIN ALIAS
            // ============================================================

            if (!string.IsNullOrWhiteSpace(product.Alias))
            {
                int count;

                if (!isUpdate)
                {
                    count = await prMastDB.CheckOccurance(
                        "ALIAS",
                        product.Alias
                    );
                }
                else
                {
                    count = await prMastDB.CheckExistName(
                        "ALIAS",
                        product.Alias,
                        "PRCODE",
                        productCode
                    );
                }

                if (count > 0)
                {
                    return ProductValidationResult.Failed(
                        "Alias already in Use");
                }
            }

            // ============================================================
            // 7. DUPLICATE BARCODE
            // ============================================================

            if (!string.IsNullOrWhiteSpace(product.BarCode))
            {
                int count;

                if (!isUpdate)
                {
                    count = await prMastDB.CheckOccurance(
                        "BARCODE",
                        product.BarCode
                    );
                }
                else
                {
                    count = await prMastDB.CheckExistName(
                        "BARCODE",
                        product.BarCode,
                        "PRCODE",
                        productCode
                    );
                }

                if (count > 0)
                {
                    return ProductValidationResult.Failed(
                        "Bar Code already in Use");
                }
            }

            // ============================================================
            // 8. ALTERNATE UNIT
            // ============================================================

            if (product.AltUnit > 0 &&
                product.Unit == product.AltUnit)
            {
                return ProductValidationResult.Failed(
                    "Main Unit and Alternate Unit may not be same");
            }

            // ============================================================
            // 9. TAX CATEGORY
            // ============================================================

            if (product.TaxCatCode < 1)
            {
                return ProductValidationResult.Failed(
                    "Invalid Tax Category Selected");
            }

            // ============================================================
            // 10. OPENING STOCK
            // ============================================================

            if (product.OpnStock > 0 &&
                product.OpnStkval == 0)
            {
                return ProductValidationResult.Failed(
                    "Opening stock value must not be blank");
            }

            // ============================================================
            // 11. BATCH + SERIAL TRACKING
            // ============================================================

            if (product.BatchFlg &&
                product._SLNOTRACK)
            {
                return ProductValidationResult.Failed(
                    "Serial Tracking and Batch cannot be enabled together");
            }
            //us sen a alias  
            // ============================================================
            // 12. MULTIPLE ALIAS VALIDATION
            // ============================================================

            if (aliasesSent &&
                request.Aliases != null &&
                request.Aliases.Count > 0)
            {
                foreach (var alias in request.Aliases)
                {
                    if (alias == null)
                    {
                        continue;
                    }

                    // ----------------------------------------------------
                    // ALIAS NAME
                    // ----------------------------------------------------

                    if (string.IsNullOrWhiteSpace(alias.AlName))
                    {
                        continue;
                    }
                    //counts how many times the current alias name appears in the submitted alias list
                    // ----------------------------------------------------
                    // DUPLICATE ALIAS INSIDE REQUEST
                    // ----------------------------------------------------

                    int duplicateCount =
                        request.Aliases.Count(x =>
                            x != null &&
                            !string.IsNullOrWhiteSpace(x.AlName) &&
                            x.AlName.Equals(
                                alias.AlName,
                                StringComparison.OrdinalIgnoreCase
                            )
                        );
                    //same validation appears more than ones 
                    if (duplicateCount > 1)
                    {
                        return ProductValidationResult.Failed(
                            "Duplicate Alias found: " +
                            alias.AlName
                        );
                    }

                    // ---------------------------------------------------
                    // CHECK ALIAS IN PRALIAS
                    // ---------------------------------------------------
                    //checks whether the alias you entered is already used by another record.

                    int aliasCount;

                    if (!isUpdate)
                    {
                        aliasCount = await prAliasDB.CheckOccurance(
                            "ALNAME",
                            alias.AlName
                        );
                    }
                    else
                    {
                        ClsPrAlias clsPrAlias =
                            new ClsPrAlias();

                        clsPrAlias.Code =
                            alias.Code;

                        aliasCount =
                            await prAliasDB.CheckOccuranceForUpdate(
                                "ALNAME",
                                alias.AlName,
                                clsPrAlias
                            );
                    }

                    if (aliasCount > 0)
                    {
                        return ProductValidationResult.Failed(
                            "Alias already in Use: " +
                            alias.AlName
                        );
                    }

                    // ----------------------------------------------------
                    // ALIAS CANNOT BE MAIN ALIAS
                    // ----------------------------------------------------

                    if (!string.IsNullOrWhiteSpace(product.Alias) &&
                        alias.AlName.Equals(
                            product.Alias,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return ProductValidationResult.Failed(
                            "Alias cannot be same as main Alias: " +
                            alias.AlName
                        );
                    }

                    // ----------------------------------------------------
                    // DIFFERENT UNIT ALIAS
                    // ----------------------------------------------------

                    if (alias.AlType == 1)
                    {
                        // ------------------------------------------------
                        // SALES PRICE
                        // ------------------------------------------------

                        if (alias.SalesPrice <= 0)
                        {
                            return ProductValidationResult.Failed(
                                "Sales Price must be greater than zero for alias: " +
                                alias.AlName
                            );
                        }

                        // ------------------------------------------------
                        // QUANTITY
                        // ------------------------------------------------

                        if (alias.Qty <= 0)
                        {
                            return ProductValidationResult.Failed(
                                "Quantity must be greater than zero for alias: " +
                                alias.AlName
                            );
                        }

                        // ------------------------------------------------
                        // UNIT
                        // ------------------------------------------------

                        if (alias.UnitCode <= 0)
                        {
                            return ProductValidationResult.Failed(
                                "Invalid Unit Selected for alias: " +
                                alias.AlName
                            );
                        }

                        // ------------------------------------------------
                        // UNIT MUST BE BASE OR ALTERNATE UNIT
                        // ------------------------------------------------

                        if (alias.UnitCode != product.Unit &&
                            alias.UnitCode != product.AltUnit)
                        {
                            return ProductValidationResult.Failed(
                                "Alias Unit must be Base Unit or Alternate Unit: " +
                                alias.AlName
                            );
                        }
                    }
                }
            }

            // ============================================================
            // 13. BOM Validation
            // ============================================================

            if (Variables.BM &&
                !string.IsNullOrWhiteSpace(product._BMEQN))
            {
                string formula =
                    product._BMEQN.Trim();

                // Formula must contain L, Q and W
                if (!formula.Contains("L",
                        StringComparison.OrdinalIgnoreCase) ||
                    !formula.Contains("Q",
                        StringComparison.OrdinalIgnoreCase) ||
                    !formula.Contains("W",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return ProductValidationResult.Failed(
                        "Invalid Equation Formula. Formula must contain L, Q and W");
                }

                try
                {
                    DataTable dt =
                        new DataTable();

                    dt.Columns.Add(
                        "L",
                        typeof(decimal));

                    dt.Columns.Add(
                        "Q",
                        typeof(decimal));

                    dt.Columns.Add(
                        "W",
                        typeof(decimal));

                    DataRow row =
                        dt.NewRow();

                    row["L"] =
                        product._BMPL;

                    row["Q"] =
                        product._BMPQ;

                    row["W"] =
                        product._BMPW;

                    dt.Rows.Add(row);

                    dt.Compute(
                        formula,
                        ""
                    );
                }
                catch
                {
                    return ProductValidationResult.Failed(
                        "Invalid Equation Formula");
                }
            }

            // ============================================================
            // 14. VALIDATION SUCCESS
            // ============================================================

            return ProductValidationResult.Success();
        }

        [HttpPut("products/{id:int}/aliases/{code:int}")]
        public async Task<IActionResult> UpdateProductAlias(int id, int code, [FromBody] PrAliasRequest request)
        {
            try
            {
                LngTB.InitLng();

                Variable Variables =
                    new Variable();

                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = "error",
                        message = "User ID not found in token."
                    });
                }

                Variables.UserId =
                    int.Parse(userIdClaim.Value);

                //DatabaseConnection databaseConnection =
                //    new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(
                //        Variables
                //    );
                string connectionString = _databaseConnection.GetConnectionString(Variables);

                DBConn db =
                    new DBConn(connectionString);

                CommonFunction commonFunction =
                    new CommonFunction(db);

                bool connectionResult =
                    await commonFunction.CheckDatabaseConnection();

                if (!connectionResult)
                {
                    return StatusCode(500, new
                    {
                        status = "error",
                        message = "Database connection failed."
                    });
                }

                await commonFunction.SetVariableValues(
                    Variables
                );

                commonFunction.Variables =
                    Variables;

                commonFunction.Functions.Variables =
                    Variables;

                PrAliasDB prAliasDB =
                    new PrAliasDB(db);

                prAliasDB.Variables =
                    Variables;

                PrMastDB prMastDB =
                    new PrMastDB(db);

                prMastDB.Variables =
                    Variables;

                // Check product
                ClsPrMast clsPrMast =
                    new ClsPrMast();

                clsPrMast.PrCode = id;

                DataTable productTable =
                    await prMastDB.SearchByCode(
                        clsPrMast
                    );

                if (productTable == null ||
                    productTable.Rows.Count == 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Product not found.",
                        prCode = id
                    });
                }

                // Check alias
                ClsPrAlias searchAlias =
                    new ClsPrAlias();

                searchAlias.Code =
                    code;

                DataTable aliasTable =
                    await prAliasDB.SearchByAlCode(
                        searchAlias
                    );

                if (aliasTable == null ||
                    aliasTable.Rows.Count == 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Alias not found.",
                        code = code,
                        prCode = id
                    });
                }

                // Make sure alias belongs to this product
                int aliasProductCode =
                    Convert.ToInt32(
                        aliasTable.Rows[0]["PRCODE"]
                    );

                if (aliasProductCode != id)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message =
                            "Alias does not belong to this product.",
                        code = code,
                        prCode = id
                    });
                }

                if (string.IsNullOrWhiteSpace(request.AlName))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Alias name must not be blank."
                    });
                }

                // Check duplicate alias name during update
                ClsPrAlias duplicateCheck =
                    new ClsPrAlias();

                duplicateCheck.Code =
                    code;

                int aliasCount =
                    await prAliasDB.CheckOccuranceForUpdate(
                        "ALNAME",
                        request.AlName,
                        duplicateCheck
                    );

                if (aliasCount > 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message =
                            "Alias already in Use: " +
                            request.AlName
                    });
                }

                ClsPrAlias clsPrAlias =
                    new ClsPrAlias();

                clsPrAlias.Code =
                    code;

                clsPrAlias.PrCode =
                    id;

                clsPrAlias.AlCode =
                    request.AlCode;

                clsPrAlias.AlName =
                    request.AlName;

                clsPrAlias.Qty =
                    request.Qty;

                clsPrAlias.UnitCode =
                    request.UnitCode;

                clsPrAlias.SalesPrice =
                    request.SalesPrice;

                clsPrAlias.PurPrice =
                    request.PurPrice;

                clsPrAlias.Active =
                    request.Active;

                clsPrAlias.AlType =
                    request.AlType;

                clsPrAlias.InvUnitCode =
                    request.InvUnitCode;

                int updateResult =
                    await prAliasDB.Update(
                        clsPrAlias
                    );

                if (updateResult <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message =
                            "Alias update failed.",
                        code = code,
                        prCode = id
                    });
                }

                return Ok(new
                {
                    status = true,
                    message =
                        "Product alias updated successfully.",
                    code = code,
                    prCode = id
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message =
                        "Database error occurred.",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message =
                        "Unexpected error occurred.",
                    error = ex.Message
                });
            }
        }

        [HttpDelete("products/{id:int}/aliases/{code:int}")]
        public async Task<IActionResult> DeleteProductAlias(
    int id,
    int code)
        {
            try
            {
                LngTB.InitLng();

                Variable Variables =
                    new Variable();

                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = "error",
                        message = "User ID not found in token."
                    });
                }

                Variables.UserId =
                    int.Parse(userIdClaim.Value);

                //DatabaseConnection databaseConnection =
                //    new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(
                //        Variables
                //    );

                string connectionString = _databaseConnection.GetConnectionString(Variables);

                DBConn db =
                    new DBConn(connectionString);

                CommonFunction commonFunction =
                    new CommonFunction(db);

                bool connectionResult =
                    await commonFunction.CheckDatabaseConnection();

                if (!connectionResult)
                {
                    return StatusCode(500, new
                    {
                        status = "error",
                        message = "Database connection failed."
                    });
                }

                await commonFunction.SetVariableValues(
                    Variables
                );

                commonFunction.Variables =
                    Variables;

                commonFunction.Functions.Variables =
                    Variables;

                PrAliasDB prAliasDB =
                    new PrAliasDB(db);

                prAliasDB.Variables =
                    Variables;

                PrMastDB prMastDB =
                    new PrMastDB(db);

                prMastDB.Variables =
                    Variables;

                // Check product
                ClsPrMast clsPrMast =
                    new ClsPrMast();

                clsPrMast.PrCode =
                    id;

                DataTable productTable =
                    await prMastDB.SearchByCode(
                        clsPrMast
                    );

                if (productTable == null ||
                    productTable.Rows.Count == 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Product not found.",
                        prCode = id
                    });
                }

                // Check alias
                ClsPrAlias clsPrAlias =
                    new ClsPrAlias();

                clsPrAlias.Code =
                    code;

                DataTable aliasTable =
                    await prAliasDB.SearchByAlCode(
                        clsPrAlias
                    );

                if (aliasTable == null ||
                    aliasTable.Rows.Count == 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Alias not found.",
                        code = code,
                        prCode = id
                    });
                }

                // Make sure alias belongs to this product
                int aliasProductCode =
                    Convert.ToInt32(
                        aliasTable.Rows[0]["PRCODE"]
                    );

                if (aliasProductCode != id)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message =
                            "Alias does not belong to this product.",
                        code = code,
                        prCode = id
                    });
                }

                int deleteResult =
                    await prAliasDB.DeleteByCode(
                        clsPrAlias
                    );

                if (deleteResult <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Alias deletion failed.",
                        code = code,
                        prCode = id
                    });
                }

                return Ok(new
                {
                    status = true,
                    message =
                        "Product alias deleted successfully.",
                    code = code,
                    prCode = id
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message =
                        "Database error occurred.",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message =
                        "Unexpected error occurred.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("products/{id:int}/price-categories")]
        public async Task<IActionResult> GetProductPriceCategories(int id)
        {
            try
            {
                LngTB.InitLng();

                Variable Variables =
                    new Variable();

                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = "error",
                        message = "User ID not found in token."
                    });
                }

                Variables.UserId =
                    int.Parse(userIdClaim.Value);

                //DatabaseConnection databaseConnection =
                //    new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(
                //        Variables
                //    );
                string connectionString = _databaseConnection.GetConnectionString(Variables);

                DBConn db =
                    new DBConn(connectionString);

                CommonFunction commonFunction =
                    new CommonFunction(db);

                bool connectionResult =
                    await commonFunction.CheckDatabaseConnection();

                if (!connectionResult)
                {
                    return StatusCode(500, new
                    {
                        status = "error",
                        message = "Database connection failed."
                    });
                }

                await commonFunction.SetVariableValues(
                    Variables
                );

                commonFunction.Variables =
                    Variables;

                commonFunction.Functions.Variables =
                    Variables;

                // Check product
                PrMastDB prMastDB =
                    new PrMastDB(db);

                prMastDB.Variables =
                    Variables;

                ClsPrMast clsPrMast =
                    new ClsPrMast();

                clsPrMast.PrCode =
                    id;

                DataTable productTable =
                    await prMastDB.SearchByCode(
                        clsPrMast
                    );

                if (productTable == null ||
                    productTable.Rows.Count == 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Product not found.",
                        prCode = id
                    });
                }

                // Get price categories
                PriceCatLevDB priceCatLevDB =
                    new PriceCatLevDB(db);

                priceCatLevDB.Variables =
                    Variables;

                if (priceCatLevDB.Functions != null)
                {
                    priceCatLevDB.Functions.Variables =
                        Variables;
                }

                ClsPriceCatLev clsPriceCatLev =
                    new ClsPriceCatLev();

                clsPriceCatLev.PrCode =
                    id;

                DataTable priceCategoryTable =
                    await priceCatLevDB.SearchByPrCode(
                        clsPriceCatLev
                    );

                var priceCategories =
                    priceCategoryTable
                        .AsEnumerable()
                        .Select(row =>
                            priceCategoryTable.Columns
                                .Cast<DataColumn>()
                                .ToDictionary(
                                    column =>
                                        column.ColumnName,
                                    column =>
                                        row[column] == DBNull.Value
                                            ? null
                                            : row[column]
                                )
                        )
                        .ToList();

                return Ok(new
                {
                    status = true,
                    message =
                        "Product price categories retrieved successfully.",
                    data = priceCategories
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message =
                        "Database error occurred.",
                    error =
                        ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message =
                        "Unexpected error occurred.",
                    error =
                        ex.Message
                });
            }
        }

        [HttpPost("products/{id:int}/price-categories")]
        public async Task<IActionResult> CreateProductPriceCategory(
    int id,
    [FromBody] PriceCategoryLevelRequest request)
        {
            try
            {
                LngTB.InitLng();

                Variable Variables =
                    new Variable();

                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = "error",
                        message = "User ID not found in token."
                    });
                }

                Variables.UserId =
                    int.Parse(userIdClaim.Value);

                //DatabaseConnection databaseConnection =
                //    new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(
                //        Variables
                //    );

                string connectionString = _databaseConnection.GetConnectionString(Variables);

                DBConn db =
                    new DBConn(connectionString);

                CommonFunction commonFunction =
                    new CommonFunction(db);

                bool connectionResult =
                    await commonFunction.CheckDatabaseConnection();

                if (!connectionResult)
                {
                    return StatusCode(500, new
                    {
                        status = "error",
                        message = "Database connection failed."
                    });
                }

                await commonFunction.SetVariableValues(
                    Variables
                );

                commonFunction.Variables =
                    Variables;

                commonFunction.Functions.Variables =
                    Variables;

                // Check product
                PrMastDB prMastDB =
                    new PrMastDB(db);

                prMastDB.Variables =
                    Variables;

                ClsPrMast clsPrMast =
                    new ClsPrMast();

                clsPrMast.PrCode =
                    id;

                DataTable productTable =
                    await prMastDB.SearchByCode(
                        clsPrMast
                    );

                if (productTable == null ||
                    productTable.Rows.Count == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        message = "Product not found.",
                        prCode = id
                    });
                }

                // Price Category DB
                PriceCatLevDB priceCatLevDB =
                    new PriceCatLevDB(db);

                priceCatLevDB.Variables =
                    Variables;

                if (priceCatLevDB.Functions != null)
                {
                    priceCatLevDB.Functions.Variables =
                        Variables;
                }

                // Create price category object
                ClsPriceCatLev clsPriceCatLev =
                    new ClsPriceCatLev();

                clsPriceCatLev.PrCode =
                    id;

                clsPriceCatLev.CatName =
                    request.CatName;

                clsPriceCatLev.DiscStruct =
                    request.DiscStruct;

                clsPriceCatLev.LevFrom =
                    request.LevFrom;

                clsPriceCatLev.LevTo =
                    request.LevTo;

                clsPriceCatLev.DiscPerc =
                    request.DiscPerc;

                clsPriceCatLev.EffValue =
                    request.EffValue;

                clsPriceCatLev.LogCd =
                    Variables.UserId;

                clsPriceCatLev.CatCode =
                    request.CatCode;

                clsPriceCatLev.AppPrice =
                    request.AppPrice;

                int insertResult =
                    await priceCatLevDB.Insert(
                        clsPriceCatLev
                    );

                if (insertResult < 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message =
                            "Price category creation failed.",
                        prCode = id
                    });
                }

                return Ok(new
                {
                    status = true,
                    message =
                        "Product price category created successfully.",
                    prCode = id
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message =
                        "Database error occurred.",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message =
                        "Unexpected error occurred.",
                    error = ex.Message
                });
            }
        }
        [HttpPut("products/{id:int}/price-categories/{code:int}")]
        public async Task<IActionResult> UpdateProductPriceCategory(
    int id,
    int code,
    [FromBody] PriceCategoryLevelRequest request)
        {
            try
            {
                LngTB.InitLng();

                Variable Variables =
                    new Variable();

                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = "error",
                        message = "User ID not found in token."
                    });
                }

                Variables.UserId =
                    int.Parse(userIdClaim.Value);

                //DatabaseConnection databaseConnection =
                //    new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(
                //        Variables
                //    );

                string connectionString = _databaseConnection.GetConnectionString(Variables);

                DBConn db =
                    new DBConn(connectionString);

                CommonFunction commonFunction =
                    new CommonFunction(db);

                bool connectionResult =
                    await commonFunction.CheckDatabaseConnection();

                if (!connectionResult)
                {
                    return StatusCode(500, new
                    {
                        status = "error",
                        message = "Database connection failed."
                    });
                }

                await commonFunction.SetVariableValues(
                    Variables
                );

                commonFunction.Variables =
                    Variables;

                commonFunction.Functions.Variables =
                    Variables;

                // Check product
                PrMastDB prMastDB =
                    new PrMastDB(db);

                prMastDB.Variables =
                    Variables;

                ClsPrMast clsPrMast =
                    new ClsPrMast();

                clsPrMast.PrCode =
                    id;

                DataTable productTable =
                    await prMastDB.SearchByCode(
                        clsPrMast
                    );

                if (productTable == null ||
                    productTable.Rows.Count == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        message = "Product not found.",
                        prCode = id
                    });
                }

                // Price Category DB
                PriceCatLevDB priceCatLevDB =
                    new PriceCatLevDB(db);

                priceCatLevDB.Variables =
                    Variables;

                if (priceCatLevDB.Functions != null)
                {
                    priceCatLevDB.Functions.Variables =
                        Variables;
                }

                // Check price category
                ClsPriceCatLev searchPriceCategory =
                    new ClsPriceCatLev();

                searchPriceCategory.PrCode =
                    id;

                DataTable priceCategoryTable =
                    await priceCatLevDB.SearchByPrCode(
                        searchPriceCategory
                    );

                DataRow? priceCategoryRow =
                    priceCategoryTable
                        .AsEnumerable()
                        .FirstOrDefault(
                            row =>
                                Convert.ToInt32(
                                    row["CODE"]
                                ) == code
                        );

                if (priceCategoryRow == null)
                {
                    return NotFound(new
                    {
                        status = false,
                        message = "Price category not found.",
                        code = code,
                        prCode = id
                    });
                }

                // Create update object
                ClsPriceCatLev clsPriceCatLev =
                    new ClsPriceCatLev();

                clsPriceCatLev.Code =
                    code;

                clsPriceCatLev.PrCode =
                    id;

                clsPriceCatLev.CatName =
                    request.CatName;

                clsPriceCatLev.DiscStruct =
                    request.DiscStruct;

                clsPriceCatLev.LevFrom =
                    request.LevFrom;

                clsPriceCatLev.LevTo =
                    request.LevTo;

                clsPriceCatLev.DiscPerc =
                    request.DiscPerc;

                clsPriceCatLev.EffValue =
                    request.EffValue;

                clsPriceCatLev.LogCd =
                    Variables.UserId;

                clsPriceCatLev.CatCode =
                    request.CatCode;

                clsPriceCatLev.AppPrice =
                    request.AppPrice;

                int updateResult =
                    await priceCatLevDB.Update(
                        clsPriceCatLev
                    );

                if (updateResult <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message =
                            "Price category update failed.",
                        code = code,
                        prCode = id
                    });
                }

                return Ok(new
                {
                    status = true,
                    message =
                        "Product price category updated successfully.",
                    code = code,
                    prCode = id
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message =
                        "Database error occurred.",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message =
                        "Unexpected error occurred.",
                    error = ex.Message
                });
            }
        }


        [HttpDelete("products/{id:int}/price-categories/{code:int}")]
        public async Task<IActionResult> DeleteProductPriceCategory(
    int id,
    int code)
        {
            try
            {
                LngTB.InitLng();

                Variable Variables =
                    new Variable();

                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = "error",
                        message = "User ID not found in token."
                    });
                }

                Variables.UserId =
                    int.Parse(userIdClaim.Value);

                //DatabaseConnection databaseConnection =
                //    new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(
                //        Variables
                //    );

                string connectionString = _databaseConnection.GetConnectionString(Variables);

                DBConn db =
                    new DBConn(connectionString);

                CommonFunction commonFunction =
                    new CommonFunction(db);

                bool connectionResult =
                    await commonFunction.CheckDatabaseConnection();

                if (!connectionResult)
                {
                    return StatusCode(500, new
                    {
                        status = "error",
                        message = "Database connection failed."
                    });
                }

                await commonFunction.SetVariableValues(
                    Variables
                );

                commonFunction.Variables =
                    Variables;

                commonFunction.Functions.Variables =
                    Variables;

                // Check product
                PrMastDB prMastDB =
                    new PrMastDB(db);

                prMastDB.Variables =
                    Variables;

                ClsPrMast clsPrMast =
                    new ClsPrMast();

                clsPrMast.PrCode =
                    id;

                DataTable productTable =
                    await prMastDB.SearchByCode(
                        clsPrMast
                    );

                if (productTable == null ||
                    productTable.Rows.Count == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        message = "Product not found.",
                        prCode = id
                    });
                }

                // Price Category DB
                PriceCatLevDB priceCatLevDB =
                    new PriceCatLevDB(db);

                priceCatLevDB.Variables =
                    Variables;

                if (priceCatLevDB.Functions != null)
                {
                    priceCatLevDB.Functions.Variables =
                        Variables;
                }

                // Check price category
                ClsPriceCatLev clsPriceCatLev =
                    new ClsPriceCatLev();

                clsPriceCatLev.Code =
                    code;

                clsPriceCatLev.PrCode =
                    id;

                DataTable priceCategoryTable =
                    await priceCatLevDB.SearchByPrCode(
                        clsPriceCatLev
                    );

                bool categoryExists =
                    priceCategoryTable
                        .AsEnumerable()
                        .Any(
                            row =>
                                Convert.ToInt32(
                                    row["CODE"]
                                ) == code
                        );

                if (!categoryExists)
                {
                    return NotFound(new
                    {
                        status = false,
                        message =
                            "Price category not found.",
                        code = code,
                        prCode = id
                    });
                }

                int deleteResult =
                    await priceCatLevDB.DeleteByCode(
                        clsPriceCatLev
                    );

                if (deleteResult <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message =
                            "Price category deletion failed.",
                        code = code,
                        prCode = id
                    });
                }

                return Ok(new
                {
                    status = true,
                    message =
                        "Product price category deleted successfully.",
                    code = code,
                    prCode = id
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message =
                        "Database error occurred.",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message =
                        "Unexpected error occurred.",
                    error = ex.Message
                });
            }
        }
        //---------------------------
        //Image latest
        //---------------------------

        private const long MaxProductImageSize = 5 * 1024 * 1024;

        private static string? DetectImageContentType(byte[] bytes)
        {
            if (bytes.Length >= 3 &&
                bytes[0] == 0xFF &&
                bytes[1] == 0xD8 &&
                bytes[2] == 0xFF)
            {
                return "image/jpeg";
            }

            if (bytes.Length >= 8 &&
                bytes[0] == 0x89 &&
                bytes[1] == 0x50 &&
                bytes[2] == 0x4E &&
                bytes[3] == 0x47 &&
                bytes[4] == 0x0D &&
                bytes[5] == 0x0A &&
                bytes[6] == 0x1A &&
                bytes[7] == 0x0A)
            {
                return "image/png";
            }

            if (bytes.Length >= 12 &&
                System.Text.Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" &&
                System.Text.Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP")
            {
                return "image/webp";
            }

            if (bytes.Length >= 6)
            {
                string signature =
                    System.Text.Encoding.ASCII.GetString(bytes, 0, 6);

                if (signature == "GIF87a" || signature == "GIF89a")
                {
                    return "image/gif";
                }
            }

            return null;
        }

        private static bool TryReadStoredImage(
            object imageValue,
            out byte[] imageBytes)
        {
            imageBytes = Array.Empty<byte>();

            if (imageValue is byte[] binaryImage)
            {
                imageBytes = binaryImage;
                return imageBytes.Length > 0;
            }

            string storedValue =
                Convert.ToString(imageValue) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(storedValue))
                return false;

            if (storedValue.StartsWith(
                "data:",
                StringComparison.OrdinalIgnoreCase))
            {
                int commaIndex = storedValue.IndexOf(',');

                if (commaIndex < 0)
                    return false;

                storedValue = storedValue[(commaIndex + 1)..];
            }

            try
            {
                imageBytes = Convert.FromBase64String(storedValue);
                return imageBytes.Length > 0;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private void SetImageCacheHeaders(byte[] imageBytes)
        {
            string etag = "\"" +
                Convert.ToHexString(SHA256.HashData(imageBytes)) +
                "\"";

            Response.Headers["ETag"] = etag;
            Response.Headers["Cache-Control"] = "private, max-age=86400";
        }

        private bool ImageETagMatches()
        {
            string etag = Response.Headers["ETag"].ToString();
            string requestHeader =
                Request.Headers["If-None-Match"].ToString();

            if (string.IsNullOrWhiteSpace(etag) ||
                string.IsNullOrWhiteSpace(requestHeader))
            {
                return false;
            }

            return requestHeader
                .Split(',')
                .Select(value => value.Trim())
                .Any(value =>
                    value == "*" ||
                    value == etag ||
                    value == "W/" + etag);
        }



        //[HttpPost("products/{id:int}/image")]
        //[Consumes("multipart/form-data")]
        //[HttpPost("image")]

        //[HttpPost("{id}/image")]
        //[Consumes("multipart/form-data")]
        //[HttpPost("image")]
        //[Consumes("multipart/form-data")]
        //public async Task<IActionResult> AddProductImage(
        //    int id,
        //    [FromForm] IFormFile image)

        [HttpPost("image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddProductImage(
    int id,
    IFormFile image)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid product ID."
                    });
                }

                LngTB.InitLng();

                Variable Variables = new Variable();

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null ||
                    !int.TryParse(userIdClaim.Value, out int userId) ||
                    userId <= 0)
                {
                    return Unauthorized(new
                    {
                        status = "error",
                        message = "Invalid or missing user ID in token."
                    });
                }

                Variables.UserId = userId;

                if (image == null || image.Length == 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Image file is required."
                    });
                }

                if (image.Length > MaxProductImageSize)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Image size cannot exceed 5 MB."
                    });
                }

                byte[] imageBytes;

                await using (var memoryStream = new MemoryStream())
                {
                    await image.CopyToAsync(memoryStream);
                    imageBytes = memoryStream.ToArray();
                }

                string? contentType = DetectImageContentType(imageBytes);

                if (contentType != "image/jpeg" &&
                    contentType != "image/png" &&
                    contentType != "image/webp")
                {
                    return BadRequest(new
                    {
                        status = false,
                        message =
                            "Only valid JPEG, PNG and WebP image signatures are supported."
                    });
                }

                //DatabaseConnection databaseConnection =
                //    new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(Variables);

                string connectionString = _databaseConnection.GetConnectionString(Variables);

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Database connection string is not configured."
                    });
                }

                using DBConn db = new DBConn(connectionString);

                CommonFunction commonFunction = new CommonFunction(db);

                if (!await commonFunction.CheckDatabaseConnection())
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Database connection failed."
                    });
                }

                await commonFunction.SetVariableValues(Variables);

                commonFunction.Variables = Variables;

                if (commonFunction.Functions != null)
                {
                    commonFunction.Functions.Variables = Variables;
                }

                PrMastDB prMastDB = new PrMastDB(db)
                {
                    Variables = Variables
                };

                if (prMastDB.Functions != null)
                {
                    prMastDB.Functions.Variables = Variables;
                }

                ClsPrMast clsPrMast = new ClsPrMast
                {
                    PrCode = id
                };

                DataTable productTable =
                    await prMastDB.SearchByCode(clsPrMast);

                if (productTable == null || productTable.Rows.Count == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        message = "Product not found.",
                        prCode = id
                    });
                }

                PrImgDB prImgDB = new PrImgDB(db)
                {
                    Variables = Variables
                };

                if (prImgDB.Functions != null)
                {
                    prImgDB.Functions.Variables = Variables;
                }

                DataTable existingImageTable =
                    await prImgDB.SearchByID(clsPrMast);

                if (existingImageTable != null &&
                    existingImageTable.Rows.Count > 0)
                {
                    return Conflict(new
                    {
                        status = false,
                        message = "Product image already exists.",
                        prCode = id
                    });
                }

                // Keep the existing database storage format.
                string base64Image = Convert.ToBase64String(imageBytes);

                int result = await prImgDB.Insert(id, base64Image);

                if (result <= 0)
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Failed to add product image.",
                        prCode = id
                    });
                }

                return Ok(new
                {
                    status = true,
                    message = "Product image added successfully.",
                    prCode = id,
                    imageUrl = Url.Action(
                        nameof(GetProductImage),
                        values: new { id }),
                    contentType,
                    size = imageBytes.Length
                });
            }
            catch (SqlException ex)
            {
                // Log ex internally in production.
                return StatusCode(500, new
                {
                    status = false,
                    message = "Database error occurred."
                });
            }
            catch (Exception ex)
            {
                // Log ex internally in production.
                return StatusCode(500, new
                {
                    status = false,
                    message = "Unexpected error occurred."
                });
            }
        }


        //[HttpPut("products/{id:int}/image")]
        //[Consumes("multipart/form-data")]

        [HttpPut("products/{id:int}/image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateProductImage(
            int id,
           IFormFile image)
        {
            //[FromForm]
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid product ID."
                    });
                }

                LngTB.InitLng();

                Variable Variables = new Variable();

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null ||
                    !int.TryParse(userIdClaim.Value, out int userId) ||
                    userId <= 0)
                {
                    return Unauthorized(new
                    {
                        status = "error",
                        message = "Invalid or missing user ID in token."
                    });
                }

                Variables.UserId = userId;

                if (image == null || image.Length == 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Image file is required."
                    });
                }

                if (image.Length > MaxProductImageSize)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Image size cannot exceed 5 MB."
                    });
                }

                byte[] imageBytes;

                await using (var memoryStream = new MemoryStream())
                {
                    await image.CopyToAsync(memoryStream);
                    imageBytes = memoryStream.ToArray();
                }

                string? contentType = DetectImageContentType(imageBytes);

                if (contentType != "image/jpeg" &&
                    contentType != "image/png" &&
                    contentType != "image/webp")
                {
                    return BadRequest(new
                    {
                        status = false,
                        message =
                            "Only valid JPEG, PNG and WebP image signatures are supported."
                    });
                }

                //DatabaseConnection databaseConnection =
                //    new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(Variables);

                string connectionString = _databaseConnection.GetConnectionString(Variables);

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Database connection string is not configured."
                    });
                }

                using DBConn db = new DBConn(connectionString);

                CommonFunction commonFunction = new CommonFunction(db);

                if (!await commonFunction.CheckDatabaseConnection())
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Database connection failed."
                    });
                }

                await commonFunction.SetVariableValues(Variables);

                commonFunction.Variables = Variables;

                if (commonFunction.Functions != null)
                {
                    commonFunction.Functions.Variables = Variables;
                }

                PrMastDB prMastDB = new PrMastDB(db)
                {
                    Variables = Variables
                };

                if (prMastDB.Functions != null)
                {
                    prMastDB.Functions.Variables = Variables;
                }

                ClsPrMast clsPrMast = new ClsPrMast
                {
                    PrCode = id
                };

                DataTable productTable =
                    await prMastDB.SearchByCode(clsPrMast);

                if (productTable == null || productTable.Rows.Count == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        message = "Product not found.",
                        prCode = id
                    });
                }

                PrImgDB prImgDB = new PrImgDB(db)
                {
                    Variables = Variables
                };

                if (prImgDB.Functions != null)
                {
                    prImgDB.Functions.Variables = Variables;
                }

                DataTable existingImageTable =
                    await prImgDB.SearchByID(clsPrMast);

                if (existingImageTable == null ||
                    existingImageTable.Rows.Count == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        message = "Product image not found. Use POST to add an image.",
                        prCode = id
                    });
                }

                string base64Image = Convert.ToBase64String(imageBytes);

                int result = await prImgDB.Update(id, base64Image);

                if (result <= 0)
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Failed to update product image.",
                        prCode = id
                    });
                }

                return Ok(new
                {
                    status = true,
                    message = "Product image updated successfully.",
                    prCode = id,
                    imageUrl = Url.Action(
                        nameof(GetProductImage),
                        values: new { id }),
                    contentType,
                    size = imageBytes.Length
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Database error occurred."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Unexpected error occurred."
                });
            }
        }

        [HttpGet("products/{id:int}/image")]
        public async Task<IActionResult> GetProductImage(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid product ID."
                    });
                }

                LngTB.InitLng();

                Variable Variables = new Variable();

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null ||
                    !int.TryParse(userIdClaim.Value, out int userId) ||
                    userId <= 0)
                {
                    return Unauthorized(new
                    {
                        status = "error",
                        message = "Invalid or missing user ID in token."
                    });
                }

                Variables.UserId = userId;

                //DatabaseConnection databaseConnection =
                //    new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(Variables);

                string connectionString = _databaseConnection.GetConnectionString(Variables);

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Database connection string is not configured."
                    });
                }

                using DBConn db = new DBConn(connectionString);

                CommonFunction commonFunction = new CommonFunction(db);

                if (!await commonFunction.CheckDatabaseConnection())
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Database connection failed."
                    });
                }

                await commonFunction.SetVariableValues(Variables);

                commonFunction.Variables = Variables;

                if (commonFunction.Functions != null)
                {
                    commonFunction.Functions.Variables = Variables;
                }

                PrMastDB prMastDB = new PrMastDB(db)
                {
                    Variables = Variables
                };

                if (prMastDB.Functions != null)
                {
                    prMastDB.Functions.Variables = Variables;
                }

                ClsPrMast clsPrMast = new ClsPrMast
                {
                    PrCode = id
                };

                DataTable productTable =
                    await prMastDB.SearchByCode(clsPrMast);

                if (productTable == null || productTable.Rows.Count == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        message = "Product not found.",
                        prCode = id
                    });
                }

                PrImgDB prImgDB = new PrImgDB(db)
                {
                    Variables = Variables
                };

                if (prImgDB.Functions != null)
                {
                    prImgDB.Functions.Variables = Variables;
                }

                DataTable imageTable =
                    await prImgDB.SearchByID(clsPrMast);

                if (imageTable == null ||
                    imageTable.Rows.Count == 0 ||
                    !imageTable.Columns.Contains("PRIMG") ||
                    imageTable.Rows[0]["PRIMG"] == DBNull.Value)
                {
                    return NotFound(new
                    {
                        status = false,
                        message = "Product image not found.",
                        prCode = id
                    });
                }

                if (!TryReadStoredImage(
                    imageTable.Rows[0]["PRIMG"],
                    out byte[] imageBytes))
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Stored product image has an invalid format."
                    });
                }

                string? contentType = DetectImageContentType(imageBytes);

                if (contentType == null)
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Stored product image format is unsupported."
                    });
                }

                SetImageCacheHeaders(imageBytes);

                if (ImageETagMatches())
                {
                    return StatusCode(StatusCodes.Status304NotModified);
                }

                return File(imageBytes, contentType);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Database error occurred."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Unexpected error occurred."
                });
            }
        }

        [HttpDelete("products/{id:int}/image")]
        public async Task<IActionResult> DeleteProductImage(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid product ID."
                    });
                }

                LngTB.InitLng();

                Variable Variables = new Variable();

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null ||
                    !int.TryParse(userIdClaim.Value, out int userId) ||
                    userId <= 0)
                {
                    return Unauthorized(new
                    {
                        status = "error",
                        message = "Invalid or missing user ID in token."
                    });
                }

                Variables.UserId = userId;

                //DatabaseConnection databaseConnection =
                //    new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(Variables);
                string connectionString = _databaseConnection.GetConnectionString(Variables);

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Database connection string is not configured."
                    });
                }

                using DBConn db = new DBConn(connectionString);

                CommonFunction commonFunction = new CommonFunction(db);

                if (!await commonFunction.CheckDatabaseConnection())
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Database connection failed."
                    });
                }

                await commonFunction.SetVariableValues(Variables);

                commonFunction.Variables = Variables;

                if (commonFunction.Functions != null)
                {
                    commonFunction.Functions.Variables = Variables;
                }

                PrMastDB prMastDB = new PrMastDB(db)
                {
                    Variables = Variables
                };

                if (prMastDB.Functions != null)
                {
                    prMastDB.Functions.Variables = Variables;
                }

                ClsPrMast clsPrMast = new ClsPrMast
                {
                    PrCode = id
                };

                DataTable productTable =
                    await prMastDB.SearchByCode(clsPrMast);

                if (productTable == null || productTable.Rows.Count == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        message = "Product not found.",
                        prCode = id
                    });
                }

                PrImgDB prImgDB = new PrImgDB(db)
                {
                    Variables = Variables
                };

                if (prImgDB.Functions != null)
                {
                    prImgDB.Functions.Variables = Variables;
                }

                DataTable existingImageTable =
                    await prImgDB.SearchByID(clsPrMast);

                if (existingImageTable == null ||
                    existingImageTable.Rows.Count == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        message = "Product image not found.",
                        prCode = id
                    });
                }

                int result = await prImgDB.Delete(id);

                if (result <= 0)
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Failed to delete product image.",
                        prCode = id
                    });
                }

                return Ok(new
                {
                    status = true,
                    message = "Product image deleted successfully.",
                    prCode = id
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Database error occurred."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Unexpected error occurred."
                });
            }
        }


        [HttpGet("products/{id:int}/thumbnail")]
        public async Task<IActionResult> GetProductThumbnail(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid product ID."
                    });
                }

                LngTB.InitLng();

                Variable Variables = new Variable();

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null ||
                    !int.TryParse(userIdClaim.Value, out int userId) ||
                    userId <= 0)
                {
                    return Unauthorized(new
                    {
                        status = "error",
                        message = "Invalid or missing user ID in token."
                    });
                }

                Variables.UserId = userId;

                //DatabaseConnection databaseConnection =
                //    new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(Variables);

                string connectionString = _databaseConnection.GetConnectionString(Variables);

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Database connection string is not configured."
                    });
                }

                using DBConn db = new DBConn(connectionString);

                CommonFunction commonFunction = new CommonFunction(db);

                if (!await commonFunction.CheckDatabaseConnection())
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Database connection failed."
                    });
                }

                await commonFunction.SetVariableValues(Variables);
                commonFunction.Variables = Variables;

                if (commonFunction.Functions != null)
                {
                    commonFunction.Functions.Variables = Variables;
                }

                PrMastDB prMastDB = new PrMastDB(db)
                {
                    Variables = Variables
                };

                if (prMastDB.Functions != null)
                {
                    prMastDB.Functions.Variables = Variables;
                }

                ClsPrMast clsPrMast = new ClsPrMast
                {
                    PrCode = id
                };

                DataTable productTable =
                    await prMastDB.SearchByCode(clsPrMast);

                if (productTable == null || productTable.Rows.Count == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        message = "Product not found.",
                        prCode = id
                    });
                }

                PrImgDB prImgDB = new PrImgDB(db)
                {
                    Variables = Variables
                };

                if (prImgDB.Functions != null)
                {
                    prImgDB.Functions.Variables = Variables;
                }

                DataTable imageTable =
                    await prImgDB.SearchByID(clsPrMast);

                if (imageTable == null ||
                    imageTable.Rows.Count == 0 ||
                    !imageTable.Columns.Contains("PRIMG") ||
                    imageTable.Rows[0]["PRIMG"] == DBNull.Value)
                {
                    return NotFound(new
                    {
                        status = false,
                        message = "Product image not found.",
                        prCode = id
                    });
                }

                if (!TryReadStoredImage(
                    imageTable.Rows[0]["PRIMG"],
                    out byte[] originalBytes))
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Stored product image has an invalid format.",
                        prCode = id
                    });
                }

                using SKBitmap? originalBitmap =
                    SKBitmap.Decode(originalBytes);

                if (originalBitmap == null)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Stored product image could not be decoded.",
                        prCode = id
                    });
                }

                const int maxSize = 200;

                float scale = Math.Min(
                    (float)maxSize / originalBitmap.Width,
                    (float)maxSize / originalBitmap.Height);

                int width = Math.Max(
                    1, (int)(originalBitmap.Width * scale));

                int height = Math.Max(
                    1, (int)(originalBitmap.Height * scale));

                using SKBitmap? resizedBitmap = originalBitmap.Resize(
                    new SKImageInfo(width, height),
                    SKFilterQuality.Medium);

                if (resizedBitmap == null)
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Could not generate the thumbnail.",
                        prCode = id
                    });
                }

                using SKImage thumbnailImage =
                    SKImage.FromBitmap(resizedBitmap);

                using SKData encodedImage =
                    thumbnailImage.Encode(
                        SKEncodedImageFormat.Jpeg, 80);

                byte[] thumbnailBytes = encodedImage.ToArray();

                string etag = "\"" +
                    Convert.ToHexString(
                        SHA256.HashData(thumbnailBytes)) +
                    "\"";

                Response.Headers["Cache-Control"] = "private, no-cache";
                Response.Headers["ETag"] = etag;

                if (Request.Headers.IfNoneMatch.Any(
                    value => string.Equals(
                        value, etag, StringComparison.Ordinal)))
                {
                    return StatusCode(StatusCodes.Status304NotModified);
                }

                return File(thumbnailBytes, "image/jpeg");
            }
            catch (SqlException)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Database error occurred."
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Unexpected error occurred."
                });
            }
        }

        //

        [HttpGet("lookups/categories")]
        public async Task<IActionResult> GetCategoryComboBox()
        {
            try
            {
                LngTB.InitLng();

                Variable Variables = new Variable();

                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = "error",
                        message = "User ID not found in token."
                    });
                }

                Variables.UserId =
                    int.Parse(userIdClaim.Value);

                //DatabaseConnection databaseConnection =
                //    new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(Variables);
                string connectionString = _databaseConnection.GetConnectionString(Variables);

                DBConn db =
                    new DBConn(connectionString);

                CommonFunction commonFunction =
                    new CommonFunction(db);

                bool connectionResult =
                    await commonFunction.CheckDatabaseConnection();

                if (!connectionResult)
                {
                    return StatusCode(500, new
                    {
                        status = "error",
                        message = "Database connection failed."
                    });
                }

                await commonFunction.SetVariableValues(Variables);

                commonFunction.Variables = Variables;
                commonFunction.Functions.Variables = Variables;

                CatgMasDB catgMasDB =
                    new CatgMasDB(db);

                catgMasDB.Variables = Variables;
                catgMasDB.Functions.Variables = Variables;

                DataTable dt =
                    await catgMasDB.Search();

                var data =
                    dt.AsEnumerable()
                      .Select(row => new
                      {
                          Id = row["Id"] == DBNull.Value
                              ? 0
                              : Convert.ToInt32(row["Id"]),

                          Category = row["Category"] == DBNull.Value
                              ? ""
                              : row["Category"].ToString()
                      })
                      .ToList();

                return Ok(new
                {
                    status = true,
                    message = "Category list retrieved successfully.",
                    data = data
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = "error",
                    message = "Database error occurred.",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "error",
                    message = "Unexpected error occurred.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("lookups/item-groups")]
        public async Task<IActionResult> GroupCombobox()
        {
            try
            {
                LngTB.InitLng();

                Variable Variables = new Variable();

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = false,
                        message = "User is not authenticated."
                    });
                }

                Variables.UserId = int.Parse(userIdClaim.Value);

                //DatabaseConnection databaseConnection = new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(Variables);
                string connectionString = _databaseConnection.GetConnectionString(Variables);

                DBConn db = new DBConn(connectionString);

                CommonFunction commonFunction = new CommonFunction(db);

                bool connectionResult =
                    await commonFunction.CheckDatabaseConnection();

                if (!connectionResult)
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Database connection failed."
                    });
                }

                await commonFunction.SetVariableValues(Variables);

                commonFunction.Variables = Variables;
                commonFunction.Functions.Variables = Variables;

                PrGrpMasDB prGrpMasDB =
                    new PrGrpMasDB(db);

                DataTable dt =
                    await prGrpMasDB.ActiveGroups();

                var data = dt.AsEnumerable()
                    .Select(row => new
                    {
                        Id = row["PRGRPID"],
                        Group = row["GRPNAME"]
                    })
                    .ToList();

                return Ok(new
                {
                    status = true,
                    message = "Product Group list retrieved successfully.",
                    data = data
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Database error occurred.",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Unexpected error occurred.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("lookups/units")]
        public async Task<IActionResult> GetUnitComboBox()
        {
            try
            {
                LngTB.InitLng();

                Variable Variables = new Variable();

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = false,
                        message = "User not authenticated."
                    });
                }

                Variables.UserId = int.Parse(userIdClaim.Value);

                //DatabaseConnection databaseConnection = new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(Variables);

                string connectionString = _databaseConnection.GetConnectionString(Variables);

                DBConn db = new DBConn(connectionString);

                CommonFunction commonFunction = new CommonFunction(db);

                bool connectionResult =
                    await commonFunction.CheckDatabaseConnection();

                if (!connectionResult)
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Database connection failed."
                    });
                }

                await commonFunction.SetVariableValues(Variables);

                commonFunction.Variables = Variables;
                commonFunction.Functions.Variables = Variables;

                PrUnitMasDBs prUnitMasDB = new PrUnitMasDBs(db);

                DataTable dt = await prUnitMasDB.ProductUnits();

                var data = dt.AsEnumerable()
                    .Select(row => new
                    {
                        Id = row["Id"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(row["Id"]),

                        UOM = row["UOM"] == DBNull.Value
                            ? ""
                            : row["UOM"].ToString()
                    })
                    .ToList();

                return Ok(new
                {
                    status = true,
                    message = "Unit list retrieved successfully.",
                    data = data
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Database error occurred.",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Unexpected error occurred.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("lookups/brands")]
        public async Task<IActionResult> GetBrandComboBox()
        {
            try
            {
                LngTB.InitLng();

                Variable Variables = new Variable();

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = false,
                        message = "User not authenticated."
                    });
                }

                Variables.UserId = int.Parse(userIdClaim.Value);

                //DatabaseConnection databaseConnection = new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(Variables);

                string connectionString = _databaseConnection.GetConnectionString(Variables);

                DBConn db = new DBConn(connectionString);

                CommonFunction commonFunction = new CommonFunction(db);

                bool connectionResult =
                    await commonFunction.CheckDatabaseConnection();

                if (!connectionResult)
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Database connection failed."
                    });
                }

                await commonFunction.SetVariableValues(Variables);

                commonFunction.Variables = Variables;
                commonFunction.Functions.Variables = Variables;

                BrandMasDB brandMasDB = new BrandMasDB(db);

                DataTable dt = await brandMasDB.ActiveBrandsAll();

                var data = dt.AsEnumerable()
                    .Select(row => new
                    {
                        Id = row["ID"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(row["ID"]),

                        BrandName = row["BRANDNAME"] == DBNull.Value
                            ? ""
                            : row["BRANDNAME"].ToString()
                    })
                    .ToList();

                return Ok(new
                {
                    status = true,
                    message = "Brand list retrieved successfully.",
                    data = data
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Database error occurred.",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Unexpected error occurred.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("lookups/tax-categories")]
        public async Task<IActionResult> GetTaxCategoryComboBox()
        {
            try
            {
                LngTB.InitLng();

                Variable Variables = new Variable();

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = false,
                        message = "User not authenticated."
                    });
                }

                Variables.UserId = int.Parse(userIdClaim.Value);

                //DatabaseConnection databaseConnection = new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(Variables);

                string connectionString = _databaseConnection.GetConnectionString(Variables);

                DBConn db = new DBConn(connectionString);

                CommonFunction commonFunction = new CommonFunction(db);

                bool connectionResult =
                    await commonFunction.CheckDatabaseConnection();

                if (!connectionResult)
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Database connection failed."
                    });
                }

                await commonFunction.SetVariableValues(Variables);

                commonFunction.Variables = Variables;
                commonFunction.Functions.Variables = Variables;

                TaxMasDBbkp taxMasDB = new TaxMasDBbkp(db);

                DataTable dt =
                    await taxMasDB.ActiveTaxesForItemMaster();

                var data = dt.AsEnumerable()
                    .Select(row => new
                    {
                        Id = row["Id"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(row["Id"]),

                        TaxGroup = row["Tax Group"] == DBNull.Value
                            ? ""
                            : row["Tax Group"].ToString()
                    })
                    .ToList();

                return Ok(new
                {
                    status = true,
                    message = "Tax category list retrieved successfully.",
                    data = data
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Database error occurred.",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Unexpected error occurred.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("lookups/price-categories")]
        public async Task<IActionResult> PriceCategoryCombobox()
        {
            try
            {
                LngTB.InitLng();

                Variable Variables = new Variable();

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = false,
                        message = "User is not authenticated."
                    });
                }

                Variables.UserId = int.Parse(userIdClaim.Value);

                //DatabaseConnection databaseConnection = new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(Variables);

                string connectionString = _databaseConnection.GetConnectionString(Variables);

                DBConn db = new DBConn(connectionString);

                CommonFunction commonFunction = new CommonFunction(db);

                bool connectionResult =
                    await commonFunction.CheckDatabaseConnection();

                if (!connectionResult)
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Database connection failed."
                    });
                }

                await commonFunction.SetVariableValues(Variables);

                commonFunction.Variables = Variables;
                commonFunction.Functions.Variables = Variables;

                PriceCatMasDB priceCatMasDB =
                    new PriceCatMasDB(db);

                DataTable dt =
                    await priceCatMasDB.Categories();

                var data = dt.AsEnumerable()
                    .Select(row => new
                    {
                        Id = row["CODE"],
                        Category = row["CATNAME"]
                    })
                    .ToList();

                return Ok(new
                {
                    status = true,
                    message = "Price Category loaded successfully.",
                    data = data
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Database error occurred.",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Unexpected error occurred.",
                    error = ex.Message
                });
            }
        }

        // ============================================================
        // CREATE OPENING STOCK
        // POST: /api/v1/opening-stock
        // ============================================================
        [HttpPost("opening-stock")]
        public async Task<IActionResult> CreateOpeningStock(
            [FromBody] ClsOpnStk request)
        {
            try
            {
                LngTB.InitLng();

                Variable Variables = new Variable();

                // --------------------------------------------------------
                // GET USER ID FROM TOKEN
                // --------------------------------------------------------
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = false,
                        message = "User ID not found in token."
                    });
                }

                Variables.UserId = int.Parse(userIdClaim.Value);

                // --------------------------------------------------------
                // DATABASE CONNECTION
                // --------------------------------------------------------
                //DatabaseConnection databaseConnection = new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(Variables);

                string connectionString = _databaseConnection.GetConnectionString(Variables);

                DBConn db = new DBConn(connectionString);

                CommonFunction commonFunction = new CommonFunction(db);

                bool connectionResult =
                    await commonFunction.CheckDatabaseConnection();

                if (!connectionResult)
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Database connection failed."
                    });
                }

                // --------------------------------------------------------
                // SET VARIABLES
                // --------------------------------------------------------
                await commonFunction.SetVariableValues(Variables);

                commonFunction.Variables = Variables;
                commonFunction.Functions.Variables = Variables;

                // --------------------------------------------------------
                // OPENING STOCK DB
                // --------------------------------------------------------
                OpnStkDB opnStkDB = new OpnStkDB(db);

                opnStkDB.Variables = Variables;

                // --------------------------------------------------------
                // BASIC VALIDATION
                // --------------------------------------------------------
                if (request.PrCode <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Product code (PrCode) is required."
                    });
                }

                if (request.Qty < 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Opening stock quantity cannot be negative."
                    });
                }

                if (request.Rate < 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Opening stock rate cannot be negative."
                    });
                }

                // --------------------------------------------------------
                // DEFAULT VALUES
                // --------------------------------------------------------
                if (string.IsNullOrWhiteSpace(request.Batch))
                    request.Batch = "";

                if (string.IsNullOrWhiteSpace(request.Status))
                    request.Status = "Updated";

                if (request.Expiry == default)
                    request.Expiry = Variables.fyStartDate;

                if (request.MfgDate == default)
                    request.MfgDate = Variables.fyStartDate;

                // --------------------------------------------------------
                // SET CONVERTED VALUES
                // --------------------------------------------------------
                if (request.ConvQty == 0)
                    request.ConvQty = request.Qty;

                if (request.ConvValue == 0)
                    request.ConvValue = request.StVal;

                // --------------------------------------------------------
                // SET BRANCH
                // --------------------------------------------------------
                if (Variables.BranchYN && request.BrCd == 0)
                {
                    request.BrCd = Variables.VRSYS_DEFBRANCH;
                }

                // --------------------------------------------------------
                // INSERT
                // --------------------------------------------------------
                int result = await opnStkDB.Insert(request);

                if (result <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Opening stock could not be created."
                    });
                }

                // --------------------------------------------------------
                // SUCCESS
                // --------------------------------------------------------
                return Ok(new
                {
                    status = true,
                    message = "Opening stock created successfully.",
                    id = result,
                    prCode = request.PrCode
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Database error occurred.",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred while creating opening stock.",
                    error = ex.Message
                });
            }
        }

        // ============================================================
        // GET OPENING STOCK BY PRODUCT CODE
        // GET: /api/v1/opening-stock?productId={id}
        // ============================================================
        [HttpGet("opening-stock")]
        public async Task<IActionResult> GetOpeningStock(
            [FromQuery(Name = "productId")] int prCode)
        {
            try
            {
                LngTB.InitLng();

                Variable Variables = new Variable();

                // --------------------------------------------------------
                // GET USER ID FROM TOKEN
                // --------------------------------------------------------
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = false,
                        message = "User ID not found in token."
                    });
                }

                Variables.UserId = int.Parse(userIdClaim.Value);

                // --------------------------------------------------------
                // DATABASE CONNECTION
                // --------------------------------------------------------
                //DatabaseConnection databaseConnection =
                //    new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(Variables);

                string connectionString = _databaseConnection.GetConnectionString(Variables);

                DBConn db = new DBConn(connectionString);

                CommonFunction commonFunction =
                    new CommonFunction(db);

                bool connectionResult =
                    await commonFunction.CheckDatabaseConnection();

                if (!connectionResult)
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Database connection failed."
                    });
                }

                // --------------------------------------------------------
                // SET VARIABLES
                // --------------------------------------------------------
                await commonFunction.SetVariableValues(Variables);

                commonFunction.Variables = Variables;
                commonFunction.Functions.Variables = Variables;

                // --------------------------------------------------------
                // OPENING STOCK DB
                // --------------------------------------------------------
                OpnStkDB opnStkDB = new OpnStkDB(db);

                opnStkDB.Variables = Variables;

                // --------------------------------------------------------
                // VALIDATE PRODUCT CODE
                // --------------------------------------------------------
                if (prCode <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid product code."
                    });
                }

                // --------------------------------------------------------
                // SEARCH OPENING STOCK
                // --------------------------------------------------------
                ClsOpnStk clsOpnStk = new ClsOpnStk();

                clsOpnStk.PrCode = prCode;

                DataTable table =
                    await opnStkDB.SearchByPrCode(clsOpnStk);

                // --------------------------------------------------------
                // NO DATA
                // --------------------------------------------------------
                if (table == null || table.Rows.Count == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        message = "Opening stock not found.",
                        prCode = prCode
                    });
                }

                // --------------------------------------------------------
                // SUCCESS
                // --------------------------------------------------------
                return Ok(new
                {
                    status = true,
                    message = "Opening stock retrieved successfully.",
                    prCode = prCode,
                    data = ConvertDataTable(table)
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Database error occurred.",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred while retrieving opening stock.",
                    error = ex.Message
                });
            }
        }
        // ============================================================
        // UPDATE OPENING STOCK BY ID
        // PUT: /api/v1/opening-stock/{id}
        // ============================================================
        [HttpPut("opening-stock/{id:int}")]
        public async Task<IActionResult> UpdateOpeningStock(
            int id,
            [FromBody] ClsOpnStk request)
        {
            try
            {
                LngTB.InitLng();

                Variable Variables = new Variable();

                // --------------------------------------------------------
                // USER
                // --------------------------------------------------------
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = false,
                        message = "User ID not found in token."
                    });
                }

                Variables.UserId = int.Parse(userIdClaim.Value);

                // --------------------------------------------------------
                // DATABASE CONNECTION
                // --------------------------------------------------------
                // DatabaseConnection databaseConnection =
                //     new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(Variables);
                string connectionString = _databaseConnection.GetConnectionString(Variables);

                DBConn db = new DBConn(connectionString);

                CommonFunction commonFunction =
                    new CommonFunction(db);

                bool connectionResult =
                    await commonFunction.CheckDatabaseConnection();

                if (!connectionResult)
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Database connection failed."
                    });
                }

                // --------------------------------------------------------
                // VARIABLES
                // --------------------------------------------------------
                await commonFunction.SetVariableValues(Variables);

                commonFunction.Variables = Variables;
                commonFunction.Functions.Variables = Variables;

                // --------------------------------------------------------
                // OPENING STOCK DB
                // --------------------------------------------------------
                OpnStkDB opnStkDB = new OpnStkDB(db);

                opnStkDB.Variables = Variables;

                // --------------------------------------------------------
                // VALIDATE ID
                // --------------------------------------------------------
                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid opening stock ID."
                    });
                }

                // --------------------------------------------------------
                // CHECK EXISTING RECORD
                // --------------------------------------------------------
                string checkQuery = @"
            SELECT ID
            FROM OPNSTK
            WHERE ID = " + id;

                DataTable existingTable =
                    await db.GetDatatable(checkQuery);

                if (existingTable == null ||
                    existingTable.Rows.Count == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        message = "Opening stock not found.",
                        id = id
                    });
                }

                // --------------------------------------------------------
                // VALIDATION
                // --------------------------------------------------------
                if (request.PrCode <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Product code (PrCode) is required."
                    });
                }

                if (request.Qty < 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Opening stock quantity cannot be negative."
                    });
                }

                if (request.Rate < 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Opening stock rate cannot be negative."
                    });
                }

                // --------------------------------------------------------
                // DEFAULT VALUES
                // --------------------------------------------------------
                if (string.IsNullOrWhiteSpace(request.Batch))
                {
                    request.Batch = "";
                }

                if (string.IsNullOrWhiteSpace(request.Status))
                {
                    request.Status = "Updated";
                }

                if (request.ConvQty == 0)
                {
                    request.ConvQty = request.Qty;
                }

                if (request.ConvValue == 0)
                {
                    request.ConvValue = request.StVal;
                }

                if (request.Expiry == default)
                {
                    request.Expiry = Variables.fyStartDate;
                }

                if (request.MfgDate == default)
                {
                    request.MfgDate = Variables.fyStartDate;
                }

                if (Variables.BranchYN && request.BrCd == 0)
                {
                    request.BrCd = Variables.VRSYS_DEFBRANCH;
                }

                // --------------------------------------------------------
                // ESCAPE STRING VALUES
                // --------------------------------------------------------
                string batch =
                    (request.Batch ?? "")
                        .Replace("'", "''");

                string status =
                    (request.Status ?? "Updated")
                        .Replace("'", "''");

                // --------------------------------------------------------
                // DECIMAL VALUES
                // IMPORTANT:
                // Always use "." regardless of Windows culture.
                // --------------------------------------------------------
                string qty =
                    request.Qty.ToString(
                        System.Globalization.CultureInfo.InvariantCulture);

                string rate =
                    request.Rate.ToString(
                        System.Globalization.CultureInfo.InvariantCulture);

                string stVal =
                    request.StVal.ToString(
                        System.Globalization.CultureInfo.InvariantCulture);

                string convQty =
                    request.ConvQty.ToString(
                        System.Globalization.CultureInfo.InvariantCulture);

                string convValue =
                    request.ConvValue.ToString(
                        System.Globalization.CultureInfo.InvariantCulture);

                string mrp =
                    request.Mrp.ToString(
                        System.Globalization.CultureInfo.InvariantCulture);

                string salRate =
                    request.SalRate.ToString(
                        System.Globalization.CultureInfo.InvariantCulture);

                // --------------------------------------------------------
                // SQL SERVER ISO DATE
                // Example:
                // 2026-12-31T00:00:00
                // --------------------------------------------------------
                string expiry =
                    request.Expiry.ToString(
                        "yyyy-MM-ddTHH:mm:ss",
                        System.Globalization.CultureInfo.InvariantCulture);

                string mfgDate =
                    request.MfgDate.ToString(
                        "yyyy-MM-ddTHH:mm:ss",
                        System.Globalization.CultureInfo.InvariantCulture);

                // --------------------------------------------------------
                // UPDATE
                // --------------------------------------------------------
                string qry = @"
            UPDATE OPNSTK
            SET
                PRCODE = " + request.PrCode + @",
                GDWCODE = " + request.GdwCode + @",
                UOM = " + request.Uom + @",
                QTY = " + qty + @",
                RATE = " + rate + @",
                STVALUE = " + stVal + @",
                CONVQTY = " + convQty + @",
                CONVVALUE = " + convValue + @",
                BATCH = N'" + batch + @"',
                MRP = " + mrp + @",
                STATUS = N'" + status + @"',
                COSTCCD = " + request.CostcCd + @",
                SALRATE = " + salRate + @",
                EXPIRY = '" + expiry + @"',
                MFGDATE = '" + mfgDate + @"',
                EXPCHECK = " +
                            (request.ExpChk ? "1" : "0") + @",
                BRCD = " + request.BrCd + @",
                SNTSLNO = " + request.SNTSLNO + @"
            WHERE ID = " + id + @";

            SELECT @@ROWCOUNT AS AffectedRows;
        ";

                // --------------------------------------------------------
                // EXECUTE
                // --------------------------------------------------------
                DataTable updateTable =
                    await db.GetDatatable(qry);

                int affectedRows = 0;

                if (updateTable != null &&
                    updateTable.Rows.Count > 0)
                {
                    affectedRows =
                        Convert.ToInt32(
                            updateTable.Rows[0]["AffectedRows"]);
                }

                // --------------------------------------------------------
                // RESULT
                // --------------------------------------------------------
                if (affectedRows <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Opening stock could not be updated.",
                        id = id
                    });
                }

                // --------------------------------------------------------
                // SUCCESS
                // --------------------------------------------------------
                return Ok(new
                {
                    status = true,
                    message = "Opening stock updated successfully.",
                    id = id,
                    prCode = request.PrCode
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Database error occurred.",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred while updating opening stock.",
                    error = ex.Message
                });
            }
        }
        // ============================================================
        // DELETE OPENING STOCK BY ID
        // DELETE: /api/v1/opening-stock/{id}
        // ============================================================
        [HttpDelete("opening-stock/{id:int}")]
        public async Task<IActionResult> DeleteOpeningStock(int id)
        {
            try
            {
                LngTB.InitLng();

                Variable Variables = new Variable();

                // --------------------------------------------------------
                // GET USER ID
                // --------------------------------------------------------
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        status = false,
                        message = "User ID not found in token."
                    });
                }

                Variables.UserId = int.Parse(userIdClaim.Value);

                // --------------------------------------------------------
                // DATABASE CONNECTION
                // --------------------------------------------------------
                string connectionString = _databaseConnection.GetConnectionString(Variables);

                //string connectionString =
                //    databaseConnection.GetConnectionString(Variables);

                DBConn db = new DBConn(connectionString);

                CommonFunction commonFunction =
                    new CommonFunction(db);

                bool connectionResult =
                    await commonFunction.CheckDatabaseConnection();

                if (!connectionResult)
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Database connection failed."
                    });
                }

                // --------------------------------------------------------
                // SET VARIABLES
                // --------------------------------------------------------
                await commonFunction.SetVariableValues(Variables);

                commonFunction.Variables = Variables;
                commonFunction.Functions.Variables = Variables;

                // --------------------------------------------------------
                // VALIDATE ID
                // --------------------------------------------------------
                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid opening stock ID."
                    });
                }

                // --------------------------------------------------------
                // CHECK RECORD EXISTS
                // --------------------------------------------------------
                string checkQuery = @"
            SELECT ID
            FROM OPNSTK
            WHERE ID = " + id;

                DataTable existingTable =
                    await db.GetDatatable(checkQuery);

                if (existingTable == null ||
                    existingTable.Rows.Count == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        message = "Opening stock not found.",
                        id = id
                    });
                }

                // --------------------------------------------------------
                // DELETE
                // --------------------------------------------------------
                string qry = @"
            DELETE FROM OPNSTK
            WHERE ID = " + id + @";

            SELECT @@ROWCOUNT AS AffectedRows;
        ";

                DataTable deleteTable =
                    await db.GetDatatable(qry);

                int affectedRows = 0;

                if (deleteTable != null &&
                    deleteTable.Rows.Count > 0)
                {
                    affectedRows =
                        Convert.ToInt32(
                            deleteTable.Rows[0]["AffectedRows"]);
                }

                // --------------------------------------------------------
                // RESULT
                // --------------------------------------------------------
                if (affectedRows <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Opening stock could not be deleted.",
                        id = id
                    });
                }

                // --------------------------------------------------------
                // SUCCESS
                // --------------------------------------------------------
                return Ok(new
                {
                    status = true,
                    message = "Opening stock deleted successfully.",
                    id = id
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Database error occurred.",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred while deleting opening stock.",
                    error = ex.Message
                });
            }
        }

        // ============================================================
        //                 =======GROUP=======
        // ============================================================

        // ============================================================
        // CREATE ITEM GROUP
        // ============================================================

        [HttpPost("item-groups")]
        public async Task<IActionResult> CreateItemGroup(
            [FromBody] ItemGroupMasterRequest request)
        {
            try
            {
                // ----------------------------------------------------
                // VALIDATION
                // ----------------------------------------------------

                if (request == null)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Request data is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(request.GroupName))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Group name is required."
                    });
                }

                if (request.GroupCode <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Group code is required."
                    });
                }

                // ----------------------------------------------------
                // GET USER ID FROM JWT
                // ----------------------------------------------------

                string? userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new
                    {
                        status = false,
                        message = "Invalid user information."
                    });
                }

                // ----------------------------------------------------
                // CONVERT YES / NO
                // ----------------------------------------------------

                bool isPrimaryGroup =
                    request.IsPrimaryGroup.Equals(
                        "YES",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    request.IsPrimaryGroup.Equals(
                        "Y",
                        StringComparison.OrdinalIgnoreCase);

                bool tagWithAccount =
                    request.TagWithAccount.Equals(
                        "YES",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    request.TagWithAccount.Equals(
                        "Y",
                        StringComparison.OrdinalIgnoreCase);

                bool isActive =
                    request.IsActive.Equals(
                        "YES",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    request.IsActive.Equals(
                        "Y",
                        StringComparison.OrdinalIgnoreCase);

                // ----------------------------------------------------
                // PRIMARY GROUP VALIDATION
                // ----------------------------------------------------

                if (!isPrimaryGroup && request.PrimaryGroupId <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Primary group is required."
                    });
                }

                // ----------------------------------------------------
                // ACCOUNT VALIDATION
                // ----------------------------------------------------

                if (tagWithAccount)
                {
                    if (request.StockAccount <= 0)
                    {
                        return BadRequest(new
                        {
                            status = false,
                            message = "Stock account is required."
                        });
                    }

                    if (request.PurchaseAccount <= 0)
                    {
                        return BadRequest(new
                        {
                            status = false,
                            message = "Purchase account is required."
                        });
                    }

                    if (request.SalesAccount <= 0)
                    {
                        return BadRequest(new
                        {
                            status = false,
                            message = "Sales account is required."
                        });
                    }
                }

                // ----------------------------------------------------
                // DATABASE CONNECTION
                // ----------------------------------------------------

                Variable Variables = new Variable();

                //DatabaseConnection databaseConnection =
                //    new DatabaseConnection();

                //DBConn db = new DBConn(
                //    databaseConnection.GetConnectionString(Variables)
                //);
                string connectionString = _databaseConnection.GetConnectionString(Variables);

                DBConn db = new DBConn(connectionString);
                CommonFunction commonFunction =
                    new CommonFunction(db);

                bool connected =
                    await commonFunction.CheckDatabaseConnection();

                if (!connected)
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Database connection failed."
                    });
                }

                // ----------------------------------------------------
                // SET DATABASE VARIABLES
                // ----------------------------------------------------

                await commonFunction.SetVariableValues(Variables);

                // ----------------------------------------------------
                // ITEM GROUP DB
                // ----------------------------------------------------

                ItemGroupDB itemGroupDB =
                    new ItemGroupDB(db);

                // ----------------------------------------------------
                // CHECK DUPLICATE GROUP NAME
                // ----------------------------------------------------

                if (await itemGroupDB.GroupNameExists(
                        request.GroupName.Trim()))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Group name already exists."
                    });
                }

                // ----------------------------------------------------
                // CHECK DUPLICATE GROUP CODE
                // ----------------------------------------------------

                if (await itemGroupDB.GroupCodeExists(
                        request.GroupCode))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Group code already exists."
                    });
                }
                // ----------------------------------------------------
                // INSERT ITEM GROUP
                // ----------------------------------------------------

                int newGroupId =
                    await itemGroupDB.Insert(
                        request.GroupName.Trim(),
                        request.SecondName?.Trim() ?? "",
                        request.GroupCode,
                        isPrimaryGroup,
                        request.PrimaryGroupId,
                        tagWithAccount,
                        request.StockAccount,
                        request.PurchaseAccount,
                        request.SalesAccount,
                        isActive,
                        userId
                    );

                // ----------------------------------------------------
                // INSERT FAILED
                // ----------------------------------------------------

                if (newGroupId <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Item group could not be created."
                    });
                }

                // ----------------------------------------------------
                // SUCCESS
                // ----------------------------------------------------

                return Ok(new
                {
                    status = true,
                    message = "Item group created successfully.",
                    groupId = newGroupId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        // ============================================================
        // GET ITEM GROUPS
        // ============================================================

        //[HttpGet("item-groups")]
        //public async Task<IActionResult> GetItemGroups([FromQuery] PaginationRequest request)
        //{
        //    try
        //    {
        //        // ----------------------------------------------------
        //        // DATABASE CONNECTION
        //        // ----------------------------------------------------

        //        Variable Variables = new Variable();

        //        DatabaseConnection databaseConnection =
        //            new DatabaseConnection();

        //        DBConn db = new DBConn(
        //            databaseConnection.GetConnectionString(Variables)
        //        );
        //        request.Normalize();

        //        CommonFunction commonFunction =
        //            new CommonFunction(db);

        //        bool connected =
        //            await commonFunction.CheckDatabaseConnection();

        //        if (!connected)
        //        {
        //            return StatusCode(500, new
        //            {
        //                status = false,
        //                message = "Database connection failed."
        //            });
        //        }

        //        // ----------------------------------------------------
        //        // SET DATABASE VARIABLES
        //        // ----------------------------------------------------

        //        await commonFunction.SetVariableValues(Variables);

        //        // ----------------------------------------------------
        //        // GET ITEM GROUPS
        //        // ----------------------------------------------------

        //        ItemGroupDB itemGroupDB =
        //            new ItemGroupDB(db);

        //        //DataTable dt =
        //        //    await itemGroupDB.GetAll();
        //        var result = await itemGroupDB.GetAll(request.Page, request.PageSize);
        //        // ----------------------------------------------------
        //        // CONVERT DATA
        //        // ----------------------------------------------------

        //        var data = new List<object>();

        //        foreach (DataRow row in dt.Rows)
        //        {
        //            data.Add(new
        //            {
        //                groupId = Convert.ToInt32(row["PRGRPID"]),
        //                groupName = Convert.ToString(row["GRPNAME"]) ?? "",
        //                secondName = Convert.ToString(row["SECNAME"]) ?? "",
        //                groupCode = Convert.ToInt32(row["GRPCODE"]),
        //                isPrimaryGroup =
        //                    Convert.ToInt32(row["PRIMGRPFLG"]) == 1
        //                        ? "YES"
        //                        : "NO",
        //                primaryGroupId =
        //                    Convert.ToInt32(row["PRIMGRPID"]),
        //                tagWithAccount =
        //                    Convert.ToInt32(row["TAGACCFLG"]) == 1
        //                        ? "YES"
        //                        : "NO",
        //                stockAccount =
        //                    Convert.ToInt32(row["STOCKACCID"]),
        //                purchaseAccount =
        //                    Convert.ToInt32(row["PURACCID"]),
        //                salesAccount =
        //                    Convert.ToInt32(row["SALESACCID"]),
        //                isActive =
        //                    Convert.ToInt32(row["ACTIVE"]) == 1
        //                        ? "YES"
        //                        : "NO"
        //            });
        //        }

        //        // ----------------------------------------------------
        //        // SUCCESS
        //        // ----------------------------------------------------

        //        return Ok(new
        //        {
        //            status = true,
        //            message = "Item groups retrieved successfully.",
        //            data = data
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            message = ex.Message
        //        });
        //    }
        //}

        [HttpGet("lookups/primary-item-groups")]
        public async Task<IActionResult> GetPrimaryGroups()
        {
            Variable Variables = new Variable();

            //DatabaseConnection databaseConnection =
            //    new DatabaseConnection();

            //DBConn db = new DBConn(
            //    databaseConnection.GetConnectionString(Variables)
            //);
            string connectionString = _databaseConnection.GetConnectionString(Variables);

            DBConn db = new DBConn(connectionString);

            CommonFunction commonFunction =
                new CommonFunction(db);

            bool connected =
                await commonFunction.CheckDatabaseConnection();

            if (!connected)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Database connection failed."
                });
            }

            await commonFunction.SetVariableValues(Variables);

            ItemGroupDB itemGroupDB =
                new ItemGroupDB(db);

            DataTable dt =
                await itemGroupDB.GetPrimaryGroups();

            var data = dt.AsEnumerable()
                .Select(row => new
                {
                    id = row.Field<int>("PRGRPID"),
                    name = row.Field<string>("GRPNAME") ?? ""
                })
                .ToList();

            return Ok(new
            {
                status = true,
                message = "Primary groups retrieved successfully.",
                data = data
            });
        }

        [HttpGet("lookups/stock-accounts")]
        public async Task<IActionResult> GetStockAccounts([FromQuery] PaginationRequest request)
        {
            Variable Variables = new Variable();

            //DatabaseConnection databaseConnection =
            //    new DatabaseConnection();

            //DBConn db = new DBConn(
            //    databaseConnection.GetConnectionString(Variables)
            //);
            string connectionString = _databaseConnection.GetConnectionString(Variables);

            DBConn db = new DBConn(connectionString);
            CommonFunction commonFunction =
                new CommonFunction(db);

            bool connected 
                =
                await commonFunction.CheckDatabaseConnection();

            if (!connected)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Database connection failed."
                });
            }

            await commonFunction.SetVariableValues(Variables);

            AccountDB accountDB =
                new AccountDB(db);

            var result = await accountDB.GetActiveAccounts(
                request.Page,
                request.PageSize
            );

            //var data = result.Data.AsEnumerable()
            //    .Select(row => new
            //    {
            //        id = row.Field<int>("ID"),
            //        name = row.Field<string>("ACCNAME") ?? "",
            //        alias = row.Field<string>("ALIAS") ?? "",
            //        code = row.Field<string>("ACCODE") ?? ""
            //    })
            //    .ToList();

            int totalPages = (int)Math.Ceiling(
                result.TotalRecords / (double)request.PageSize
            );

            return Ok(new
            {
                status = true,
                message = "Stock accounts retrieved successfully.",
                result.Data,
                pagination = new
                {
                    page = request.Page,
                    pageSize = request.PageSize,
                    totalRecords = result.TotalRecords,
                    totalPages
                }
            });
            //DataTable dt =
            //    await accountDB.GetActiveAccounts();

            //var data = dt.AsEnumerable()
            //    .Select(row => new
            //    {
            //        id = row.Field<int>("ID"),
            //        name = row.Field<string>("ACCNAME") ?? "",
            //        alias = row.Field<string>("ALIAS") ?? "",
            //        code = row.Field<string>("ACCODE") ?? ""
            //    })
            //    .ToList();

            //return Ok(new
            //{
            //    status = true,
            //    message = "Stock accounts retrieved successfully.",
            //    data = data
            //});
        }

        [HttpGet("lookups/purchase-accounts")]
        public async Task<IActionResult> GetPurchaseAccounts([FromQuery] PaginationRequest request)
        {
            Variable Variables = new Variable();

            //DatabaseConnection databaseConnection =
            //    new DatabaseConnection();

            //DBConn db = new DBConn(
            //    databaseConnection.GetConnectionString(Variables)
            //);

            string connectionString = _databaseConnection.GetConnectionString(Variables);

            DBConn db = new DBConn(connectionString);
            CommonFunction commonFunction =
                new CommonFunction(db);

            bool connected =
                await commonFunction.CheckDatabaseConnection();

            if (!connected)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Database connection failed."
                });
            }

            await commonFunction.SetVariableValues(Variables);

            AccountDB accountDB =
                new AccountDB(db);
            var result = await accountDB.GetActiveAccounts(request.Page,request.PageSize);

            int totalPages = (int)Math.Ceiling(
                result.TotalRecords / (double)request.PageSize
            );

            return Ok(new
            {
                status = true,
                message = "Purchase accounts retrieved successfully.",
                data = result.Data,
                pagination = new
                {
                    page = request.Page,
                    pageSize = request.PageSize,
                    totalRecords = result.TotalRecords,
                    totalPages = totalPages
                }
            });
        }
            //DataTable dt =
            //    await accountDB.GetActiveAccounts();

            //var data = dt.AsEnumerable()
            //    .Select(row => new
            //    {
            //        id = row.Field<int>("ID"),
            //        name = row.Field<string>("ACCNAME") ?? "",
            //        alias = row.Field<string>("ALIAS") ?? "",
            //        code = row.Field<string>("ACCODE") ?? ""
            //    })
            //    .ToList();

            //return Ok(new
            //{
            //    status = true,
            //    message = "Purchase accounts retrieved successfully.",
            //    data = data
            //});

        [HttpGet("lookups/sales-accounts")]
        public async Task<IActionResult> GetSalesAccounts()
        {
            Variable Variables = new Variable();

            //DatabaseConnection databaseConnection =
            //    new DatabaseConnection();

            //DBConn db = new DBConn(
            //    databaseConnection.GetConnectionString(Variables)
            //);

            string connectionString = _databaseConnection.GetConnectionString(Variables);

            DBConn db = new DBConn(connectionString);
            CommonFunction commonFunction =
                new CommonFunction(db);

            bool connected =
                await commonFunction.CheckDatabaseConnection();

            if (!connected)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Database connection failed."
                });
            }

            await commonFunction.SetVariableValues(Variables);

            AccountDB accountDB =
                new AccountDB(db);

            DataTable dt =
                await accountDB.GetActiveAccounts();

            var data = dt.AsEnumerable()
                .Select(row => new
                {
                    id = row.Field<int>("ID"),
                    name = row.Field<string>("ACCNAME") ?? "",
                    alias = row.Field<string>("ALIAS") ?? "",
                    code = row.Field<string>("ACCODE") ?? ""
                })
                .ToList();

            return Ok(new
            {
                status = true,
                message = "Sales accounts retrieved successfully.",
                data = data
            });
        }


        [HttpPost("products/bulk")]
        public async Task<IActionResult> BulkCreateProducts(
            [FromBody] BulkProductRequest request)
        {
            WriteBulkDebugLog("STEP 1: BulkCreateProducts entered");

            DBConn? db = null;
            string currentProductName = string.Empty;
            string currentOperation = "Entering bulk product API";
            bool transactionStarted = false;

            try
            {
                WriteBulkDebugLog($"STEP 2: Request path: {HttpContext.Request.Path}");

                // 1. Validate request
                currentOperation = "Validating request";
                WriteBulkDebugLog("STEP 3: Validating request");

                if (request?.Products == null || request.Products.Count == 0)
                {
                    WriteBulkDebugLog("Request rejected: no products");
                    return BadRequest(new
                    {
                        status = false,
                        message = "At least one product is required."
                    });
                }

                if (request.Products.Count > 500)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Maximum 500 products per request."
                    });
                }

                if (request.Products.Any(p => p == null))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "The products list cannot contain null entries."
                    });
                }

                WriteBulkDebugLog(
                    $"STEP 4: Request validated. Product count: {request.Products.Count}");

                for (int i = 0; i < request.Products.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(request.Products[i].PrName))
                    {
                        WriteBulkDebugLog($"Request rejected: empty name at index {i}");

                        return BadRequest(new
                        {
                            status = false,
                            message = $"Product name is required at index {i}.",
                            productIndex = i,
                            expectedProperty = "prName"
                        });
                    }
                }

                // 2. Initialize user
                currentOperation = "Initializing language and user";
                WriteBulkDebugLog("STEP 5: Before LngTB.InitLng");

                LngTB.InitLng();

                WriteBulkDebugLog("STEP 6: LngTB.InitLng completed");

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null ||
                    !int.TryParse(userIdClaim.Value, out int userId))
                {
                    WriteBulkDebugLog("Request rejected: valid user ID not found");

                    return Unauthorized(new
                    {
                        status = false,
                        message = "Valid user ID not found in token."
                    });
                }

                Variable Variables = new Variable { UserId = userId };

                WriteBulkDebugLog($"STEP 7: User initialized. UserId: {userId}");

                // 3. Create database connection
                currentOperation = "Creating database connection";
                WriteBulkDebugLog("STEP 8: Before GetConnectionString");

                //DatabaseConnection databaseConnection = new DatabaseConnection();

                //string connectionString =
                //    databaseConnection.GetConnectionString(Variables);
                string connectionString = _databaseConnection.GetConnectionString(Variables);

                //DBConn db = new DBConn(connectionString);
                WriteBulkDebugLog("STEP 9: Connection string obtained");

                db = new DBConn(connectionString);

                WriteBulkDebugLog("STEP 10: DBConn created");

                CommonFunction commonFunction = new CommonFunction(db);

                // 4. Check database connection
                currentOperation = "Checking database connection";
                WriteBulkDebugLog("STEP 11: Before CheckDatabaseConnection");

                bool connectionResult =
                    await commonFunction.CheckDatabaseConnection();

                WriteBulkDebugLog(
                    $"STEP 12: CheckDatabaseConnection returned: {connectionResult}");

                if (!connectionResult)
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Database connection failed."
                    });
                }

                // 5. Load database variables
                currentOperation = "Loading database variables";
                WriteBulkDebugLog("STEP 13: Before SetVariableValues");

                await commonFunction.SetVariableValues(Variables);

                WriteBulkDebugLog("STEP 14: SetVariableValues completed");

                commonFunction.Variables = Variables;

                if (commonFunction.Functions != null)
                    commonFunction.Functions.Variables = Variables;

                // 6. Initialize database helpers
                currentOperation = "Initializing database helpers";
                WriteBulkDebugLog("STEP 15: Initializing database helpers");

                PrMastDB prMastDB = new PrMastDB(db);
                prMastDB.Variables = Variables;

                if (prMastDB.Functions != null)
                    prMastDB.Functions.Variables = Variables;

                OpnStkDB opnStkDB = new OpnStkDB(db);
                opnStkDB.Variables = Variables;

                PrAliasDB prAliasDB = new PrAliasDB(db);
                prAliasDB.Variables = Variables;

                PriceCatLevDB priceCatLevDB = new PriceCatLevDB(db);
                priceCatLevDB.Variables = Variables;

                if (priceCatLevDB.Functions != null)
                    priceCatLevDB.Functions.Variables = Variables;

                PrImgDB prImgDB = new PrImgDB(db);
                prImgDB.Variables = Variables;

                if (prImgDB.Functions != null)
                    prImgDB.Functions.Variables = Variables;

                WriteBulkDebugLog("STEP 16: Database helpers initialized");

                var createdProductCodes = new List<int>();

                // 7. Begin transaction
                currentOperation = "Beginning transaction";
                WriteBulkDebugLog("STEP 17: Before BeginTransaction");

                await db.BeginTransaction();
                transactionStarted = true;

                WriteBulkDebugLog("STEP 18: Transaction started");

                // 8. Process each product
                for (int i = 0; i < request.Products.Count; i++)
                {
                    ProductRequest productRequest = request.Products[i];

                    currentProductName =
                        productRequest.PrName?.Trim() ?? string.Empty;

                    Variables.VoucherTypeCodes =
                        productRequest.VoucherTypeCodes;

                    WriteBulkDebugLog(
                        $"PRODUCT {i + 1}/{request.Products.Count}: Starting [{currentProductName}]");

                    try
                    {
                        // Map DTO
                        currentOperation = "Mapping product";

                        WriteBulkDebugLog(
                            $"PRODUCT {i + 1}: Before ToClsPrMast");

                        ClsPrMast clsPrMast = productRequest.ToClsPrMast();

                        WriteBulkDebugLog(
                            $"PRODUCT {i + 1}: Mapping completed. " +
                            $"DTO name=[{productRequest.PrName}], " +
                            $"mapped name=[{clsPrMast.PrName}], " +
                            $"GroupCode={clsPrMast.GroupCode}");

                        if (string.IsNullOrWhiteSpace(clsPrMast.PrName))
                        {
                            throw new InvalidOperationException(
                                $"Product name is empty after mapping at index {i}.");
                        }

                        if (clsPrMast.GroupCode < 1)
                        {
                            throw new InvalidOperationException(
                                $"Invalid GroupCode for '{currentProductName}'.");
                        }

                        // Validate product
                        currentOperation = "Validating product";

                        bool aliasesSent = productRequest.Aliases != null;

                        WriteBulkDebugLog(
                            $"PRODUCT {i + 1}: Before ValidateProduct");

                        ProductValidationResult validation =
                            await ValidateProduct(
                                clsPrMast,
                                productRequest,
                                prMastDB,
                                prAliasDB,
                                0,
                                false,
                                aliasesSent,
                                Variables);

                        WriteBulkDebugLog(
                            $"PRODUCT {i + 1}: ValidateProduct completed. " +
                            $"IsValid={validation.IsValid}");

                        if (!validation.IsValid)
                        {
                            throw new InvalidOperationException(validation.Message);
                        }

                        // Insert product
                        currentOperation = "Creating product";

                        WriteBulkDebugLog(
                            $"PRODUCT {i + 1}: Before prMastDB.Insert");

                        int productCode = await prMastDB.Insert(clsPrMast);

                        WriteBulkDebugLog(
                            $"PRODUCT {i + 1}: prMastDB.Insert returned {productCode}");

                        if (productCode <= 0)
                        {
                            throw new InvalidOperationException(
                                $"Product insert failed for '{currentProductName}'.");
                        }

                        // Save opening stock
                        currentOperation = "Saving opening stock";

                        WriteBulkDebugLog(
                            $"PRODUCT {i + 1}: Before SaveOpeningStock");

                        var openingStockResult = await SaveOpeningStock(
                            opnStkDB,
                            productRequest,
                            productCode,
                            Variables);

                        WriteBulkDebugLog(
                            $"PRODUCT {i + 1}: SaveOpeningStock completed. " +
                            $"Success={openingStockResult.Success}");

                        if (!openingStockResult.Success)
                        {
                            throw new InvalidOperationException(
                                openingStockResult.Error ??
                                $"Opening stock could not be saved for '{currentProductName}'.");
                        }

                        // Save aliases
                        currentOperation = "Saving aliases";

                        WriteBulkDebugLog(
                            $"PRODUCT {i + 1}: Before SaveAliases");

                        bool aliasesSaved = await SaveAliases(
                            prAliasDB,
                            productRequest,
                            productCode);

                        WriteBulkDebugLog(
                            $"PRODUCT {i + 1}: SaveAliases completed. Success={aliasesSaved}");

                        if (!aliasesSaved)
                        {
                            throw new InvalidOperationException(
                                $"Aliases could not be saved for '{currentProductName}'.");
                        }

                        // Save price categories
                        currentOperation = "Saving price categories";

                        WriteBulkDebugLog(
                            $"PRODUCT {i + 1}: Before SavePriceCategory");

                        bool priceCategoriesSaved = await SavePriceCategory(
                            priceCatLevDB,
                            productRequest,
                            productCode);

                        WriteBulkDebugLog(
                            $"PRODUCT {i + 1}: SavePriceCategory completed. " +
                            $"Success={priceCategoriesSaved}");

                        if (!priceCategoriesSaved)
                        {
                            throw new InvalidOperationException(
                                $"Price categories could not be saved for '{currentProductName}'.");
                        }

                        // Save image, if provided
                        if (!string.IsNullOrWhiteSpace(productRequest.Image))
                        {
                            currentOperation = "Saving product image";

                            WriteBulkDebugLog(
                                $"PRODUCT {i + 1}: Before image insert");

                            int imageResult = await prImgDB.Insert(
                                productCode,
                                productRequest.Image);

                            WriteBulkDebugLog(
                                $"PRODUCT {i + 1}: Image insert returned {imageResult}");

                            if (imageResult == -5)
                            {
                                throw new InvalidOperationException(
                                    $"Image could not be saved for '{currentProductName}'.");
                            }
                        }

                        createdProductCodes.Add(productCode);

                        WriteBulkDebugLog(
                            $"PRODUCT {i + 1}: Completed successfully. Code={productCode}");
                    }
                    catch (Exception ex)
                    {
                        WriteBulkDebugLog(
                            $"PRODUCT FAILED: Name=[{currentProductName}], " +
                            $"Operation=[{currentOperation}], Error={ex}");

                        throw;
                    }
                }

                // 9. Commit transaction
                currentOperation = "Committing transaction";

                WriteBulkDebugLog("STEP 19: Before CommitTransaction");

                db.CommitTransaction();
                transactionStarted = false;

                WriteBulkDebugLog("STEP 20: CommitTransaction completed");

                WriteBulkDebugLog(
                    $"STEP 21: Bulk completed. Created={createdProductCodes.Count}");

                return Ok(new
                {
                    status = true,
                    message = "Bulk product creation completed successfully.",
                    totalRequested = request.Products.Count,
                    totalCreated = createdProductCodes.Count,
                    productCodes = createdProductCodes
                });
            }
            catch (Exception ex)
            {
                WriteBulkDebugLog(
                    $"BULK FAILED: Operation=[{currentOperation}], " +
                    $"Product=[{currentProductName}], Exception={ex}");

                if (transactionStarted && db != null)
                {
                    try
                    {
                        WriteBulkDebugLog("Before RollbackTransaction");

                        db.RollbackTransaction();
                        transactionStarted = false;

                        WriteBulkDebugLog("RollbackTransaction completed");
                    }
                    catch (Exception rollbackEx)
                    {
                        WriteBulkDebugLog(
                            $"Rollback failed: {rollbackEx}");
                    }
                }

                if (ex is SqlException)
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "Database error occurred during bulk import.",
                        operation = currentOperation,
                        product = currentProductName,
                        error = ex.Message
                    });
                }

                if (ex is InvalidOperationException)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Bulk product creation failed. The batch was rolled back.",
                        operation = currentOperation,
                        product = currentProductName,
                        error = ex.Message
                    });
                }

                return StatusCode(500, new
                {
                    status = false,
                    message = "An unexpected server error occurred during bulk import.",
                    operation = currentOperation,
                    product = currentProductName,
                    error = ex.Message
                });
            }
            finally
            {
                WriteBulkDebugLog("STEP 22: Entering finally block");

                if (db != null)
                {
                    WriteBulkDebugLog("STEP 23: Before db.Dispose");

                    try
                    {
                        db.Dispose();
                        WriteBulkDebugLog("STEP 24: db.Dispose completed");
                    }
                    catch (Exception disposeEx)
                    {
                        WriteBulkDebugLog($"db.Dispose failed: {disposeEx}");
                    }
                }

                WriteBulkDebugLog("STEP 25: Request processing finished");
            }
        }

        private static void WriteBulkDebugLog(string message)
        {
            try
            {
                string logPath = Path.Combine(
                    Path.GetTempPath(),
                    "bulk-product-debug.log"
                );

                System.IO.File.AppendAllText(
                 logPath,
                 $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {message}{Environment.NewLine}"
                );
            }
            catch
            {
                // Do not interrupt API execution if logging fails.
            }
        }

        private static bool TryGetStoredImage(
            DataTable imageTable,
            out byte[] imageBytes)
        {
            imageBytes = Array.Empty<byte>();

            if (imageTable == null || imageTable.Rows.Count == 0)
                return false;

            DataRow row = imageTable.Rows[0];

            foreach (DataColumn column in imageTable.Columns)
            {
                object value = row[column];

                if (value == DBNull.Value || value == null)
                    continue;

                // Image stored as binary data
                if (value is byte[] bytes && bytes.Length > 0)
                {
                    imageBytes = bytes;
                    return true;
                }

                // Image stored as Base64 text
                if (value is string text &&
                    !string.IsNullOrWhiteSpace(text))
                {
                    if (text.StartsWith(
                        "data:",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        int commaIndex = text.IndexOf(',');

                        if (commaIndex < 0)
                            continue;

                        text = text[(commaIndex + 1)..];
                    }

                    try
                    {
                        byte[] decoded = Convert.FromBase64String(text);

                        if (decoded.Length > 0)
                        {
                            imageBytes = decoded;
                            return true;
                        }
                    }
                    catch (FormatException)
                    {
                        // This column is not valid Base64. Check the next column.
                    }
                }
            }

            return false;
        }
    }
}
