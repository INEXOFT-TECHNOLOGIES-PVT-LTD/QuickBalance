using System.Security.Claims;

namespace QuickBalance.API.Services
{
    public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
    {
        private ClaimsPrincipal? Principal =>
        accessor.HttpContext?.User;

        public bool IsAuthenticated =>
            Principal?.Identity?.IsAuthenticated == true;

        public int UserId =>
            GetRequiredInt(ClaimTypes.NameIdentifier);

        public string? UserName =>
            GetClaim(ClaimTypes.Name) ?? GetClaim("name");

        public string? TenantId =>
            GetClaim("tenant_id");

        public int? CompanyId =>
            GetOptionalInt("company_id");

        public int? BranchId =>
            GetOptionalInt("branch_id");

        public string? Role =>
            GetClaim(ClaimTypes.Role) ?? GetClaim("role");

        private string? GetClaim(string claimType) =>
            Principal?.FindFirst(claimType)?.Value;

        private int GetRequiredInt(string claimType)
        {
            if (!IsAuthenticated ||
                !int.TryParse(GetClaim(claimType), out int value) ||
                value <= 0)
            {
                throw new UnauthorizedAccessException(
                    $"Required claim '{claimType}' is missing or invalid.");
            }

            return value;
        }

        private int? GetOptionalInt(string claimType)
        {
            var value = GetClaim(claimType);

            if (string.IsNullOrWhiteSpace(value))
                return null;

            return int.TryParse(value, out int result)
                ? result
                : throw new UnauthorizedAccessException(
                    $"Claim '{claimType}' is invalid.");
        }
    }

    public interface ICurrentUser
    {
        bool IsAuthenticated { get; }

        int UserId { get; }

        string? UserName { get; }

        string? TenantId { get; }

        int? CompanyId { get; }

        int? BranchId { get; }

        string? Role { get; }
    }
}