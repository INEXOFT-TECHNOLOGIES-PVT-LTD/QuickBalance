using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using QuickBalance.API;
using QuickBalance.API.CLS;
using QuickBalance.API.Configuration;
using QuickBalance.API.DB;
using QuickBalance.API.Interfaces;
using QuickBalance.API.Repositories;
using QuickBalance.API.Repository;
using QuickBalance.API.Repository.Master;
using QuickBalance.API.Repository.Master.QuickBalance.API.Repository.Master;
using QuickBalance.API.Repository.Transactions.Sales;
using QuickBalance.API.Services;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// DATABASE CONFIGURATION
// ============================================================
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
};
builder.Services
    .AddOptions<DatabaseSettings>()
    .Bind(builder.Configuration.GetSection(
        DatabaseSettings.SectionName))
    .Validate(
        settings =>
            !string.IsNullOrWhiteSpace(settings.ServerName) &&
            !string.IsNullOrWhiteSpace(settings.DatabaseName) &&
            !string.IsNullOrWhiteSpace(settings.UserName) &&
            !string.IsNullOrWhiteSpace(settings.Password),
        "DatabaseSettings must contain ServerName, DatabaseName, UserName and Password.")
    .ValidateOnStart();

builder.Services.AddSingleton<
    IDbConnectionFactory,
    DbConnectionFactory>();

builder.Services.AddScoped<DBConn>(serviceProvider =>
{
    var connectionFactory =
        serviceProvider.GetRequiredService<IDbConnectionFactory>();

    var variables = new Variable();

    string connectionString =
        connectionFactory.GetConnectionString(variables);

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "DbConnectionFactory returned an empty connection string.");
    }

    return new DBConn(connectionString);
});
//builder.Services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();

// ============================================================
// TOKEN REVOCATION
// ============================================================

builder.Services.AddSingleton<TokenRevocationService>(
    serviceProvider =>
    {
        var connectionFactory =
            serviceProvider.GetRequiredService<IDbConnectionFactory>();

        var variables = new Variable();

        string connectionString =
            connectionFactory.GetConnectionString(variables);

        return new TokenRevocationService(connectionString);
    });

// ============================================================
// APPLICATION SERVICES
// ============================================================

builder.Services.AddScoped<DBConn>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// ============================================================
// DATABASE CLASSES
// ============================================================
builder.Services.AddScoped<AuthRepository>();
builder.Services.AddScoped<IAuthInterface, AuthService>();
//
//builder.Services.AddScoped<GdwMasDB>();
//builder.Services.AddScoped<GdwGrpDB>();
//builder.Services.AddScoped<CatgMasDB>();
//builder.Services.AddScoped<TaxMasDB>();

// BOM
//builder.Services.AddScoped<BomMasDB>();
//builder.Services.AddScoped<BomTDB>();
//builder.Services.AddScoped<BomDDB>();

//builder.Services.AddScoped<PrMastDB>();
//builder.Services.AddScoped<SmGrpDB>();
//builder.Services.AddScoped<SmMastDB>();
//builder.Services.AddScoped<CurrConvDB>();
//builder.Services.AddScoped<CurrMasDB>();
builder.Services.AddScoped<AccMasDB>();
builder.Services.AddScoped<LedgerGroupDB>();
builder.Services.AddScoped<SupplierDB>();
builder.Services.AddScoped<CustomerDB>();
//builder.Services.AddScoped<PurchaseOrderDB>();

///
builder.Services.AddSingleton<DatabaseConnection>();

builder.Services.AddScoped<ItemGroupDBs>();

builder.Services.AddScoped<IItemGroupInterface,ItemGroupService>();
builder.Services.AddScoped<PartyGroupRepository>();
builder.Services.AddScoped<IPartyGroupInterface, PartyGroupService>();
builder.Services.AddScoped<ProductDB>();
builder.Services.AddScoped<IProductInterface, ProductService>();
builder.Services.AddScoped<ItemCategoryDB>();
builder.Services.AddScoped<IItemCategoryInterface, ItemCategoryService>();
builder.Services.AddScoped<BrandDB>();
builder.Services.AddScoped<IBrandInterface, BrandService>();
builder.Services.AddScoped<IUnitInterface, UnitService>();
builder.Services.AddScoped<PrUnitMasDB>();
builder.Services.AddScoped<ITaxInterface, TaxService>();
builder.Services.AddScoped<TaxMasDB>();
builder.Services.AddScoped<PriceCategoryRepository>();
builder.Services.AddScoped<IPriceCategoryInterface, PriceCategoryService>();
builder.Services.AddScoped<ICurrencyInterface, CurrencyService>();
builder.Services.AddScoped<CurrencyRepository>();
builder.Services.AddScoped<IRegionInterface, RegionService>();
builder.Services.AddScoped<RegionRepository>();
builder.Services.AddScoped<CountryRepository>();
builder.Services.AddScoped<ICountryService, CountryService>();


builder.Services.AddScoped<UnitConversionRepository>();

builder.Services.AddScoped<IUnitConversionService,UnitConversionService>();
builder.Services.AddScoped<BomRepository>();
builder.Services.AddScoped<IBomService, BomService>();
builder.Services.AddScoped<GdwGrpRepository>();
builder.Services.AddScoped<ILocationGroupService, LocationGroupService>();
builder.Services.AddScoped<GdwMasRepository>();
builder.Services.AddScoped<ILocationService, LocationService>();

builder.Services.AddScoped<CurrencyConversionRepository>();
builder.Services.AddScoped<ICurrencyConversionInterface, CurrencyConversionService>();

builder.Services.AddScoped<SalesManGroupRepository>();
builder.Services.AddScoped<ISalesmanGroupInterface, SalesmanGroupService>();


builder.Services.AddScoped<SalesManRepository>();
builder.Services.AddScoped<ISalesmanInterface, SalesmanService>();
builder.Services.AddScoped<SalesBillingRepository>();
builder.Services.AddScoped<ISalesBillingInterface, SalesBillingService>();
// ============================================================
// CONTROLLERS AND JSON
// ============================================================

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            JsonNamingPolicy.CamelCase;

        options.JsonSerializerOptions.DictionaryKeyPolicy =
            JsonNamingPolicy.CamelCase;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors)
                .Select(x => x.ErrorMessage)
                .Distinct();

            string message = string.Join(" ", errors);

            return new BadRequestObjectResult(
                ApiResponseWithError.Failure(message));
        };
    });

builder.Services.AddEndpointsApiExplorer();

// ============================================================
// SWAGGER
// ============================================================

builder.Services.AddSwaggerGen(options =>
{
    options.OperationFilter<ApiResponseOperationFilter>();

    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API Documentation",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token."
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});

builder.Services.AddMemoryCache();

// ============================================================
// JWT CONFIGURATION
// ============================================================

var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

var configuredTenantId =
    builder.Configuration["DatabaseSettings:TenantId"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "Jwt:Key is not configured.");
}

if (string.IsNullOrWhiteSpace(jwtIssuer))
{
    throw new InvalidOperationException(
        "Jwt:Issuer is not configured.");
}

if (string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "Jwt:Audience is not configured.");
}

if (string.IsNullOrWhiteSpace(configuredTenantId))
{
    throw new InvalidOperationException(
        "DatabaseSettings:TenantId is not configured.");
}
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,

                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)),

                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

        options.Events = new JwtBearerEvents
        {
            // 1. Validate token, tenant and revocation.
            OnTokenValidated = async context =>
            {
                string? tokenTenantId =
                    context.Principal?
                        .FindFirst("tenant_id")?.Value;

                if (string.IsNullOrWhiteSpace(tokenTenantId) ||
                    !string.Equals(
                        tokenTenantId,
                        configuredTenantId,
                        StringComparison.Ordinal))
                {
                    context.Fail("Invalid tenant.");
                    return;
                }

                string authorizationHeader =
                    context.Request.Headers.Authorization.ToString();

                if (!authorizationHeader.StartsWith(
                    "Bearer ",
                    StringComparison.OrdinalIgnoreCase))
                {
                    context.Fail("Authorization token is required.");
                    return;
                }

                string token =
                    authorizationHeader.Substring(7).Trim();

                if (string.IsNullOrWhiteSpace(token))
                {
                    context.Fail("Authorization token is required.");
                    return;
                }

                var revocationService =
                    context.HttpContext.RequestServices
                        .GetRequiredService<TokenRevocationService>();

                try
                {
                    bool isRevoked =
                        await revocationService.IsTokenRevokedAsync(token);

                    if (isRevoked)
                    {
                        context.Fail("This token has been logged out.");
                    }
                }
                catch (Exception ex)
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("JwtRevocation");

                    logger.LogError(
                        ex,
                        "Failed to check JWT revocation in SQL Server.");

                    context.Fail(
                        "Unable to validate token revocation.");
                }
            },

            // 2. Global 401 Unauthorized response.
            OnChallenge = async context =>
            {
                context.HandleResponse();

                context.Response.StatusCode =
                    StatusCodes.Status401Unauthorized;

                context.Response.ContentType =
                    "application/json";

                var response = ApiResponseWithError.Failure(
                    "Unauthorized user.");

                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(response, jsonOptions));
            },

            // 3. Global 403 Forbidden response.
            OnForbidden = async context =>
            {
                context.Response.StatusCode =
                    StatusCodes.Status403Forbidden;

                context.Response.ContentType =
                    "application/json";

                var response = ApiResponseWithError.Failure(
                    "You are not authorized to access this resource.");

                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(response, jsonOptions));
            }
        };
    });
// ============================================================
// AUTHORIZATION
// ============================================================

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy =
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireAssertion(context =>
            {
                var value = context.User
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                return int.TryParse(value, out int userId) &&
                       userId > 0;
            })
            .Build();
});

// ============================================================
// CORS
// ============================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins("http://localhost:8081")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ============================================================
// RATE LIMITING
// ============================================================

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode =
            StatusCodes.Status429TooManyRequests;

        context.HttpContext.Response.ContentType =
            "application/json";

        var response = new
        {
            status = false,
            message = "Too many login attempts. Please try again later."
        };

        await context.HttpContext.Response.WriteAsync(
            JsonSerializer.Serialize(response),
            cancellationToken);
    };

    options.AddPolicy("LoginLimit", httpContext =>
    {
        string ipAddress =
            httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ipAddress,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 4,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

// ============================================================
// BUILD APPLICATION
// ============================================================
var app = builder.Build();

app.UseStaticFiles();

app.UseCors("FrontendPolicy");

// Keep Swagger accessible without authentication.
app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "API Documentation");

    options.DocumentTitle = "API Documentation";
    options.ConfigObject.PersistAuthorization = true;
    options.InjectStylesheet("/swagger/custom.css");
});

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.Run();
