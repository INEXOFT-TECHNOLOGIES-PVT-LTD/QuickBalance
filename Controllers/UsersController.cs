using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Currencies;
using QuickBalance.API.Interfaces;

namespace QuickBalance.API.Controllers.Authentication;

[ApiController]
[Route("api/v1/auth")]
[ApiExplorerSettings(GroupName = "v1")]
[Tags("01. Authentication")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthInterface _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthInterface authService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    // =========================================================
    // LOGIN
    // =========================================================
        
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("LoginLimit")]

    [ProducesResponseType(
    typeof(ApiResponseWithoutPagination<LoginResponse>),
    StatusCodes.Status200OK,
    Description = "Login successful. Returns JWT access token.")]

    [ProducesResponseType(
    typeof(ApiResponseWithoutPagination<LoginResponse>),
    StatusCodes.Status400BadRequest,
    Description = "Username and password are required.")]

    [ProducesResponseType(
    typeof(ApiResponseWithoutPagination<LoginResponse>),
    StatusCodes.Status401Unauthorized,
    Description = "Invalid username or password.")]

    [ProducesResponseType(
    typeof(ApiResponseWithoutPagination<LoginResponse>),
    StatusCodes.Status429TooManyRequests,
    Description = "Login rate limit exceeded.")]

    [ProducesResponseType(
    typeof(ApiResponseWithoutPagination<LoginResponse>),
    StatusCodes.Status500InternalServerError,
    Description = "Unable to complete login.")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        if (result.Data == null)
        {
            if (result.Message == "Username and password are required.")
                return BadRequest(result);

            if (result.Message == "Invalid username or password.")
                return Unauthorized(result);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                result);
        }

        return Ok(result);
    }

    // =========================================================
    // LOGOUT
    // =========================================================

    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]

    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]

    [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var authorizationHeader = Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(authorizationHeader) ||
            !authorizationHeader.StartsWith(
                "Bearer ",
                StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized(new
            {
                status = false,
                message = "Authorization token is required."
            });
        }

        var token = authorizationHeader["Bearer ".Length..].Trim();

        var result = await _authService.LogoutAsync(token);

        if (!result.Status)
        {
            if (result.Message is "Invalid token." or
                "Invalid token expiry." or
                "Token has expired.")
            {
                return Unauthorized(result);
            }

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                result);
        }

        return Ok(result);
    }
}


//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.RateLimiting;
//using QuickBalance.API;
//using QuickBalance.API.CLS;
//using QuickBalance.API.Configuration;
//using QuickBalance.API.DB;
//using QuickBalance.API.DTOs;
//using QuickBalance.API.Services;
//using System.Data;
//using System.IdentityModel.Tokens.Jwt;
//using System.Security.Claims;
//using System.Text;

//namespace QuickBalance.API.Controllers
//{
//    [ApiExplorerSettings(GroupName = "v1")]
//    [Tags("01. Authentication")]
//    [ApiController]
//    [Route("api/v1/auth")]
//    public class UsersController : ControllerBase
//    {
//        private readonly DatabaseConnection _databaseConnection;

//        private readonly IConfiguration _configuration;
//        private readonly TokenRevocationService _revocationService;

//        public UsersController(DatabaseConnection  databaseConnection,
//            IConfiguration configuration,
//            TokenRevocationService revocationService)
//        {
//            _configuration = configuration;
//            _revocationService = revocationService;
//            _databaseConnection = databaseConnection;

//        }

//        [AllowAnonymous]
//        [EnableRateLimiting("LoginLimit")]
//        [HttpPost("login")]
//        public async Task<IActionResult> Login(
//            [FromBody] LoginRequest request)
//        {
//            try
//            {
//                // 1. VALIDATE LOGIN REQUEST
//                if (request == null ||
//                    string.IsNullOrWhiteSpace(request.LoginUserName) ||
//                    string.IsNullOrWhiteSpace(request.LoginPassword))
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Username and password are required."
//                    });
//                }

//                // 2. DATABASE CONNECTION
//                Variable Variables = new Variable();

//                //DatabaseConnection databaseConnection =
//                //    new DatabaseConnection();

//                //string connectionString =
//                //    databaseConnection.GetConnectionString(Variables);
//                string connectionString = _databaseConnection.GetConnectionString(Variables);

//                //DBConn db = new DBConn(connectionString);

//                DBConn DBConn = new DBConn(connectionString);

//                CommonFunction commonFunction =
//                    new CommonFunction(DBConn);

//                // 3. CHECK DATABASE CONNECTION
//                if (!await commonFunction.CheckDatabaseConnection())
//                {
//                    return StatusCode(500, new
//                    {
//                        status = false,
//                        message = "Database connection failed."
//                    });
//                }

//                // 4. PREPARE USER LOGIN
//                UserDB userDB = new UserDB(DBConn);




//                ClsUser clsUser = new ClsUser
//                {
//                    UserName = request.LoginUserName,
//                    Password = Convert.ToBase64String(
//                        Encoding.UTF8.GetBytes(request.LoginPassword)
//                    )
//                };

//                // 5. CHECK USERNAME + PASSWORD
//                DataTable dt = await userDB.Login(clsUser);

//                if (dt.Rows.Count == 0)
//                {
//                    return Unauthorized(new
//                    {
//                        status = false,
//                        message ="Invalid username or password." //"Username or password is incorrect."
//                    });
//                }

//                // 6. GENERATE JWT TOKEN
//                string token = GenerateToken(Variables, dt.Rows[0]);

//                // 7. GET USER ID + FULL NAME
//                LoginUserData userData = new LoginUserData
//                {
//                    UserId = Convert.ToInt32(dt.Rows[0]["USERCODE"]),
//                    FullName = Convert.ToString(dt.Rows[0]["USERNAME"]) ?? ""
//                };

//                // 8. CREATE LOGIN RESPONSE
//                LoginResponse response = new LoginResponse
//                {
//                    Status = true,
//                    Message = "Login successful.",
//                    Token = token,
//                    //User = userData
//                };

//                // 9. RETURN RESPONSE
//                return Ok(response);
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(
//                    StatusCodes.Status500InternalServerError,
//                    new
//                    {
//                        status = false,
//                        message = ex.Message
//                    }
//                );
//            }
//        }

//        // LOGOUT: Persistently revoke the current JWT until expiry.
//        [Authorize]
//        [HttpPost("logout")]
//        public async Task<IActionResult> Logout()
//        {
//            string authorizationHeader =
//                Request.Headers.Authorization.ToString();

//            if (string.IsNullOrWhiteSpace(authorizationHeader) ||
//                !authorizationHeader.StartsWith(
//                    "Bearer ",
//                    StringComparison.OrdinalIgnoreCase))
//            {
//                return Unauthorized(new
//                {
//                    status = false,
//                    message = "Authorization token is required."
//                });
//            }

//            string token = authorizationHeader.Substring(7).Trim();

//            if (string.IsNullOrWhiteSpace(token))
//            {
//                return Unauthorized(new
//                {
//                    status = false,
//                    message = "Authorization token is required."
//                });
//            }

//            try
//            {
//                var handler = new JwtSecurityTokenHandler();
//                var jwt = handler.ReadJwtToken(token);

//                // Get token expiry.
//                var expiryClaim = jwt.Claims
//                    .FirstOrDefault(c => c.Type == "exp")?.Value;

//                if (!long.TryParse(expiryClaim, out long expiry))
//                {
//                    return Unauthorized(new
//                    {
//                        status = false,
//                        message = "Invalid token expiry."
//                    });
//                }

//                DateTimeOffset expiresAt =
//                    DateTimeOffset.FromUnixTimeSeconds(expiry);

//                if (expiresAt <= DateTimeOffset.UtcNow)
//                {
//                    return Unauthorized(new
//                    {
//                        status = false,
//                        message = "Token has expired."
//                    });
//                }

//                // Save token hash and expiry in sql server 
//                await _revocationService.RevokeTokenAsync(
//                    token,
//                    expiresAt.UtcDateTime
//                );

//                return Ok(new
//                {
//                    status = true,
//                    message = "Logout successful."
//                });
//            }
//            catch (Microsoft.Data.SqlClient.SqlException)
//            {
//                // Do not report logout success if persistence failed.
//                return StatusCode(
//                    StatusCodes.Status500InternalServerError,
//                    new
//                    {
//                        status = false,
//                        message = "Unable to complete logout. Please try again."
//                    }
//                );
//            }
//            catch (Exception)
//            {
//                return Unauthorized(new
//                {
//                    status = false,
//                    message = "Invalid token."
//                });
//            }
//        }

//        // GENERATE JWT TOKEN
//        private string GenerateToken(Variable Variables, DataRow userRow)
//        {
//            int userId = Convert.ToInt32(userRow["USERCODE"]);

//            string userName =
//                Convert.ToString(userRow["USERNAME"]) ?? "";

//            // Read tenant ID from server-side configuration.
//            string? tenantId =
//                _configuration["DatabaseSettings:TenantId"];

//            if (string.IsNullOrWhiteSpace(tenantId))
//            {
//                throw new Exception(
//                    "DatabaseSettings:TenantId is not configured."
//                );
//            }

//            // Include identity and opaque tenant ID only.
//            var claims = new List<Claim>
//            {
//                new Claim( ClaimTypes.NameIdentifier,userId.ToString() ),
//                new Claim(ClaimTypes.Name, userName),
//                new Claim("tenant_id",tenantId)
//            };

//            var jwtKey = _configuration["Jwt:Key"];

//            if (string.IsNullOrWhiteSpace(jwtKey))
//            {
//                throw new Exception("JWT Key is not configured.");
//            }

//            var key =
//                new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
//                    Encoding.UTF8.GetBytes(jwtKey)
//                );

//            var credentials =
//                new Microsoft.IdentityModel.Tokens.SigningCredentials(
//                    key,
//                    Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256
//                );

//            var issuer = _configuration["Jwt:Issuer"];
//            var audience = _configuration["Jwt:Audience"];

//            double expireMinutes = Convert.ToDouble(
//                _configuration["Jwt:ExpireMinutes"] ?? "480"
//            );

//            var token = new JwtSecurityToken(
//                issuer: issuer,
//                audience: audience,
//                claims: claims,
//                notBefore: DateTime.UtcNow,
//                expires: DateTime.UtcNow.AddMinutes(expireMinutes),
//                signingCredentials: credentials
//            );

//            return new JwtSecurityTokenHandler().WriteToken(token);
//        }
//    }
//}




////using Microsoft.AspNetCore.Authorization;
////using Microsoft.AspNetCore.Mvc;
////using Microsoft.IdentityModel.Tokens;
////using QuickBalance.API.CLS;
////using QuickBalance.API.Configuration;
////using QuickBalance.API.DB;
////using QuickBalance.API.DTOs;
////using System.Data;
////using System.IdentityModel.Tokens.Jwt;
////using System.Security.Claims;
////using System.Text;

////namespace QuickBalance.API.Controllers
////{
////    [ApiController]
////    [Route("api/v1/auth")]
////    public class UsersController : ControllerBase
////    {
////        private readonly IConfiguration _configuration;

////        public UsersController(IConfiguration configuration)
////        {
////            _configuration = configuration;
////        }
////        [AllowAnonymous]
////        [HttpPost("login")]
////        public async Task<IActionResult> Login([FromBody] LoginRequest request)
////        {
////            try
////            {
////                // ============================================================
////                // 1. VALIDATE LOGIN REQUEST
////                // ============================================================

////                if (string.IsNullOrWhiteSpace(request.LoginUserName) ||
////                    string.IsNullOrWhiteSpace(request.LoginPassword))
////                {
////                    return BadRequest(new
////                    {
////                        status = false,
////                        message = "Username and password are required."
////                    });
////                }


////                // ============================================================
////                // 2. DATABASE CONNECTION
////                // ============================================================

////                Variable Variables = new Variable();

////                DatabaseConnection databaseConnection =
////                    new DatabaseConnection();

////                string connectionString =
////                    databaseConnection.GetConnectionString(Variables);

////                DBConn DBConn =
////                    new DBConn(connectionString);

////                CommonFunction commonFunction =
////                    new CommonFunction(DBConn);


////                // ============================================================
////                // 3. CHECK DATABASE CONNECTION
////                // ============================================================

////                if (!await commonFunction.CheckDatabaseConnection())
////                {
////                    return StatusCode(500, new
////                    {
////                        status = false,
////                        message = "Database connection failed."
////                    });
////                }


////                // ============================================================
////                // 4. PREPARE USER LOGIN
////                // ============================================================

////                UserDB userDB =
////                    new UserDB(DBConn);

////                ClsUser clsUser =
////                    new ClsUser();

////                clsUser.UserName =
////                    request.LoginUserName;

////                clsUser.Password =
////                    Convert.ToBase64String(
////                        Encoding.UTF8.GetBytes(
////                            request.LoginPassword
////                        )
////                    );


////                // ============================================================
////                // 5. CHECK USERNAME + PASSWORD
////                // ============================================================

////                DataTable dt =
////                    await userDB.Login(clsUser);

////                if (dt.Rows.Count == 0)
////                {
////                    return Unauthorized(new
////                    {
////                        status = false,
////                        message = "Username or password is incorrect."
////                    });
////                }


////                // ============================================================
////                // 6. GENERATE JWT TOKEN
////                // ============================================================

////                string token =
////                    GenerateToken(
////                        Variables,
////                        dt.Rows[0]
////                    );


////                // ============================================================
////                // 7. GET USER ID + FULL NAME
////                // ============================================================

////                LoginUserData userData =
////                    new LoginUserData
////                    {
////                        UserId =
////                            Convert.ToInt32(
////                                dt.Rows[0]["USERCODE"]
////                            ),

////                        FullName =
////                            Convert.ToString(
////                                dt.Rows[0]["USERNAME"]
////                            ) ?? ""
////                    };


////                // ============================================================
////                // 8. CREATE LOGIN RESPONSE
////                // ============================================================

////                LoginResponse response =
////                    new LoginResponse
////                    {
////                        Status = true,

////                        Message =
////                            "Login successful.",

////                        Token =
////                            token,

////                        User =
////                            userData
////                    };


////                // ============================================================
////                // 9. RETURN RESPONSE
////                // ============================================================

////                return Ok(response);
////            }
////            catch (Exception ex)
////            {
////                return StatusCode(
////                    StatusCodes.Status500InternalServerError,
////                    new
////                    {
////                        status = false,
////                        message = ex.Message
////                    }
////                );
////            }
////        }

////        // ============================================================
////        // GENERATE JWT TOKEN
////        // ============================================================
////        private string GenerateToken(Variable Variables, DataRow userRow)
////        {
////            int userId = Convert.ToInt32(userRow["USERCODE"]);
////            string userName = Convert.ToString(userRow["USERNAME"]) ?? "";

////            var claims = new List<Claim>
////            {
////                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
////                new Claim(ClaimTypes.Name,userName),
////                new Claim("companyCode", Variables.CompCode.ToString()),
////                new Claim("databaseName", Variables.DatabaseName)
////            };

////            var jwtKey = _configuration["Jwt:Key"];

////            if (string.IsNullOrWhiteSpace(jwtKey))
////                throw new Exception("JWT Key is not configured.");

////            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
////            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
////            var token = new JwtSecurityToken(_configuration["Jwt:Issuer"], _configuration["Jwt:Audience"], claims, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpireMinutes"] ?? "480")), credentials);
////            return new JwtSecurityTokenHandler().WriteToken(token);
////        }
////    }
////}