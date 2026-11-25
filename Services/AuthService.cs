using Microsoft.IdentityModel.Tokens;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace QuickBalance.API.Services;

public sealed class AuthService : IAuthInterface
{
    private readonly AuthRepository _authRepository;
    private readonly IConfiguration _configuration;
    private readonly TokenRevocationService _tokenRevocationService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AuthRepository authRepository,
        IConfiguration configuration,
        TokenRevocationService tokenRevocationService,
        ILogger<AuthService> logger)
    {
        _authRepository = authRepository;
        _configuration = configuration;
        _tokenRevocationService = tokenRevocationService;
        _logger = logger;
    }

    public async Task<ApiResponseWithoutPagination<LoginResponse>> LoginAsync(LoginRequest request)
    {
        try
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.LoginUserName) ||
                string.IsNullOrWhiteSpace(request.LoginPassword))
            {
                return ApiResponseWithoutPagination<LoginResponse>.Failure(
                    "Username and password are required.");
            }

            var user = await _authRepository.GetUserByUsernameAsync(
                request.LoginUserName.Trim());

            if (user == null ||
                string.IsNullOrWhiteSpace(user.Password))
            {
                return ApiResponseWithoutPagination<LoginResponse>.Failure(
                    "Invalid username or password.");
            }
            var isLegacyPassword = IsLegacyPassword(user.Password);

            var isPasswordValid = VerifyPassword(
                request.LoginPassword,
                user.Password);

            if (!isPasswordValid)
            {
                return ApiResponseWithoutPagination<LoginResponse>.Failure(
                    "Invalid username or password.");
            }


            //if (isLegacyPassword)
            //{
            //    var secretKey = _configuration[
            //        "DatabaseSettings:SecretKey"];

            //    if (string.IsNullOrWhiteSpace(secretKey))
            //    {
            //        throw new InvalidOperationException(
            //            "Password encryption key is not configured.");
            //    }

            //    var encryptedPassword = Encrypt(
            //        request.LoginPassword,
            //        secretKey);

            //    var updated = await _authRepository.UpdatePasswordAsync(
            //        user.UserCode,
            //        encryptedPassword);

            //    if (!updated)
            //    {
            //        _logger.LogError(
            //            "Password migration failed for user {UserCode}.",
            //            user.UserCode);

            //        return ApiResponse<LoginResponse>.Failure(
            //            "Unable to complete login. Please try again.");
            //    }
            //}

            var token = GenerateJwtToken(user);
            var result = new LoginResponse { Token = token };
            return ApiResponseWithoutPagination<LoginResponse>.Success(
                "Login successful.",
                result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during login.");

            return ApiResponseWithoutPagination<LoginResponse>.Failure(
                "Unable to complete login. Please try again.");
        }
    }

    public async Task<ApiResponseWithoutPagination<bool>> LogoutAsync(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return ApiResponseWithoutPagination<bool>.Failure(
                    "Authorization token is required.");
            }

            var tokenHandler = new JwtSecurityTokenHandler();

            if (!tokenHandler.CanReadToken(token))
            {
                return ApiResponseWithoutPagination<bool>.Failure("Invalid token.");
            }

            var jwtToken = tokenHandler.ReadJwtToken(token);

            var expiryClaim = jwtToken.Claims.FirstOrDefault(
                claim => claim.Type == JwtRegisteredClaimNames.Exp);

            if (expiryClaim == null ||
                !long.TryParse(expiryClaim.Value, out var expiry))
            {
                return ApiResponseWithoutPagination<bool>.Failure(
                    "Invalid token expiry.");
            }

            var expiryDate = DateTimeOffset.FromUnixTimeSeconds(expiry);

            if (expiryDate <= DateTimeOffset.UtcNow)
            {
                return ApiResponseWithoutPagination<bool>.Failure(
                    "Token has expired.");
            }

            await _tokenRevocationService.RevokeTokenAsync(
                token,
                expiryDate.UtcDateTime);

            return ApiResponseWithoutPagination<bool>.Success(
                "Logout successful.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during logout.");

            return ApiResponseWithoutPagination<bool>.Failure(
                "Unable to complete logout. Please try again.");
        }
    }

    // =========================================================
    // PASSWORD VERIFICATION
    // =========================================================

    private bool VerifyPassword(string enteredPassword, string storedPassword)
    {
        try
        {
            // New AES encrypted password
            if (storedPassword.StartsWith(
                "AES256GCM$",
                StringComparison.Ordinal))
            {
                var secretKey = _configuration[
                    "DatabaseSettings:SecretKey"];

                if (string.IsNullOrWhiteSpace(secretKey))
                {
                    throw new InvalidOperationException(
                        "Password encryption key is not configured.");
                }

                var decryptedPassword = Decrypt(
                    storedPassword,
                    secretKey);

                return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(enteredPassword),
                    Encoding.UTF8.GetBytes(decryptedPassword));
            }

            // Legacy Base64 password verification
            var legacyPassword = Encoding.UTF8.GetString(
                Convert.FromBase64String(storedPassword));

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(enteredPassword),
                Encoding.UTF8.GetBytes(legacyPassword));
        }
        catch (FormatException)
        {
            return false;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    // =========================================================
    // PASSWORD ENCRYPTION
    // =========================================================

    public static string Encrypt(string plainText, string secretKey)
    {
        byte[] key = Convert.FromBase64String(secretKey);

        if (key.Length != 32)
            throw new ArgumentException("AES key must be 32 bytes.");

        byte[] nonce = RandomNumberGenerator.GetBytes(12);
        byte[] plaintext = Encoding.UTF8.GetBytes(plainText);
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[16];

        using var aes = new AesGcm(key, 16);

        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        byte[] result = nonce
            .Concat(tag)
            .Concat(ciphertext)
            .ToArray();

        return "AES256GCM$" + Convert.ToBase64String(result);
    }

    // =========================================================
    // PASSWORD DECRYPTION
    // =========================================================

    public static string Decrypt(string encryptedText, string secretKey)
    {
        const string prefix = "AES256GCM$";

        if (!encryptedText.StartsWith(prefix, StringComparison.Ordinal))
            throw new CryptographicException("Invalid password format.");

        byte[] key = Convert.FromBase64String(secretKey);

        if (key.Length != 32)
            throw new ArgumentException("AES key must be 32 bytes.");

        byte[] data = Convert.FromBase64String(
            encryptedText[prefix.Length..]);

        if (data.Length < 28)
            throw new CryptographicException("Invalid encrypted password.");

        byte[] nonce = data[..12];
        byte[] tag = data[12..28];
        byte[] ciphertext = data[28..];

        byte[] plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, 16);

        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    // =========================================================
    // JWT GENERATION
    // =========================================================

    private string GenerateJwtToken(LoginUserRecord user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");

        var secretKey = jwtSettings["Key"]
            ?? throw new InvalidOperationException(
                "JWT signing key is not configured.");

        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];

        var expiryMinutes = int.TryParse(
            jwtSettings["ExpiryMinutes"],
            out var minutes)
                ? minutes
                : 480;

        var tenantId = _configuration["DatabaseSettings:TenantId"];

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new InvalidOperationException(
                "DatabaseSettings:TenantId is not configured.");
        }

        var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.UserCode.ToString()),
        new(ClaimTypes.Name, user.UserName),
        new("tenant_id", tenantId)
    };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private bool IsLegacyPassword(string storedPassword)
    {
        return !storedPassword.StartsWith(
            "AES256GCM$",
            StringComparison.Ordinal);
    }
}



//using Microsoft.IdentityModel.Tokens;
//using QuickBalance.API.CLS;
//using QuickBalance.API.DTOs;
//using QuickBalance.API.Interfaces;
//using QuickBalance.API.Services;
//using System.IdentityModel.Tokens.Jwt;
//using System.Security.Claims;
//using System.Security.Cryptography;
//using System.Text;

//namespace QuickBalance.API.Services;

//public sealed class AuthService : IAuthInterface
//{
//    private readonly AuthRepository _authRepository;
//    private readonly IConfiguration _configuration;
//    private readonly TokenRevocationService _tokenRevocationService;
//    private readonly ILogger<AuthService> _logger;

//    public AuthService(
//        AuthRepository authRepository,
//        IConfiguration configuration,
//        TokenRevocationService tokenRevocationService,
//        ILogger<AuthService> logger)
//    {
//        _authRepository = authRepository;
//        _configuration = configuration;
//        _tokenRevocationService = tokenRevocationService;
//        _logger = logger;
//    }

//    public async Task<ApiResponse<string>> LoginAsync(
//        LoginRequest request)
//    {
//        try
//        {
//            if (request == null ||
//                string.IsNullOrWhiteSpace(request.LoginUserName) ||
//                string.IsNullOrWhiteSpace(request.LoginPassword))
//            {
//                return ApiResponse<string>.Failure(
//                    "Username and password are required.");
//            }

//            var secretKey = _configuration["DatabaseSettings:SecretKey"]?? throw new InvalidOperationException(
//                    "Password encryption key is not configured.");

//            var encryptedPassword = Encrypt(
//                request.LoginPassword,
//                secretKey);

//            var encodedPassword = Convert.ToBase64String(
//                Encoding.UTF8.GetBytes(request.LoginPassword));

//            var user = await _authRepository.LoginAsync(
//                request.LoginUserName.Trim(),
//                encodedPassword);

//            if (user == null)
//            {
//                return ApiResponse<string>.Failure(
//                    "Invalid username or password.");
//            }

//            var token = GenerateJwtToken(user);

//            //var response = new LoginResponse
//            //{
//            //    //Status = true,
//            //    //Message = "Login successful.",

//            //};

//            return ApiResponse<string>.Success(
//                "Login successful.",
//                token);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error occurred during login.");

//            return ApiResponse<string>.Failure(
//                "Unable to complete login. Please try again.");
//        }
//    }

//    public async Task<ApiResponse<bool>> LogoutAsync(string token)
//    {
//        try
//        {
//            if (string.IsNullOrWhiteSpace(token))
//            {
//                return ApiResponse<bool>.Failure(
//                    "Authorization token is required.");
//            }

//            var tokenHandler = new JwtSecurityTokenHandler();

//            if (!tokenHandler.CanReadToken(token))
//            {
//                return ApiResponse<bool>.Failure("Invalid token.");
//            }

//            var jwtToken = tokenHandler.ReadJwtToken(token);

//            var expiryClaim = jwtToken.Claims.FirstOrDefault(
//                claim => claim.Type == JwtRegisteredClaimNames.Exp);

//            if (expiryClaim == null ||
//                !long.TryParse(expiryClaim.Value, out var expiry))
//            {
//                return ApiResponse<bool>.Failure(
//                    "Invalid token expiry.");
//            }

//            var expiryDate = DateTimeOffset.FromUnixTimeSeconds(expiry);

//            if (expiryDate <= DateTimeOffset.UtcNow)
//            {
//                return ApiResponse<bool>.Failure(
//                    "Token has expired.");
//            }

//            await _tokenRevocationService.RevokeTokenAsync(
//                token,
//                expiryDate.UtcDateTime);

//            return ApiResponse<bool>.Success(
//                "Logout successful.",
//                true);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error occurred during logout.");

//            return ApiResponse<bool>.Failure(
//                "Unable to complete logout. Please try again.");
//        }
//    }

//    private string GenerateJwtToken(LoginUserRecord user)
//    {
//        var jwtSettings = _configuration.GetSection("Jwt");

//        var secretKey = jwtSettings["Key"]
//            ?? throw new InvalidOperationException(
//                "JWT signing key is not configured.");

//        var issuer = jwtSettings["Issuer"];
//        var audience = jwtSettings["Audience"];

//        var expiryMinutes = int.TryParse(
//            jwtSettings["ExpiryMinutes"],
//            out var minutes)
//                ? minutes
//                : 480;

//        var claims = new List<Claim>
//        {
//            new(ClaimTypes.NameIdentifier, user.UserCode.ToString()),
//            new(ClaimTypes.Name, user.UserName)
//        };

//        var tenantId = jwtSettings["TenantId"];

//        if (!string.IsNullOrWhiteSpace(tenantId))
//        {
//            claims.Add(new Claim("tenant_id", tenantId));
//        }

//        var key = new SymmetricSecurityKey(
//            Encoding.UTF8.GetBytes(secretKey));

//        var credentials = new SigningCredentials(
//            key,
//            SecurityAlgorithms.HmacSha256);

//        var token = new JwtSecurityToken(
//            issuer: issuer,
//            audience: audience,
//            claims: claims,
//            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
//            signingCredentials: credentials);

//        return new JwtSecurityTokenHandler().WriteToken(token);
//    }


//    public static string Encrypt(string plainText, string secretKey)
//    {
//        byte[] key = Convert.FromBase64String(secretKey);

//        if (key.Length != 32)
//            throw new ArgumentException("AES key must be 32 bytes.");

//        byte[] nonce = RandomNumberGenerator.GetBytes(12);
//        byte[] plaintext = Encoding.UTF8.GetBytes(plainText);
//        byte[] ciphertext = new byte[plaintext.Length];
//        byte[] tag = new byte[16];

//        using var aes = new AesGcm(key, 16);

//        aes.Encrypt(nonce, plaintext, ciphertext, tag);

//        byte[] result = nonce.Concat(tag).Concat(ciphertext).ToArray();

//        return Convert.ToBase64String(result);
//    }

//    public static string Decrypt(string encryptedText, string secretKey)
//    {
//        byte[] key = Convert.FromBase64String(secretKey);

//        if (key.Length != 32)
//            throw new ArgumentException("AES key must be 32 bytes.");

//        byte[] data = Convert.FromBase64String(encryptedText);

//        byte[] nonce = data[..12];
//        byte[] tag = data[12..28];
//        byte[] ciphertext = data[28..];

//        byte[] plaintext = new byte[ciphertext.Length];

//        using var aes = new AesGcm(key, 16);

//        aes.Decrypt(nonce, ciphertext, tag, plaintext);

//        return Encoding.UTF8.GetString(plaintext);
//    }


//}

