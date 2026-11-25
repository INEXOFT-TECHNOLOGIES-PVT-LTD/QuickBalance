namespace QuickBalance.API.DTOs
{
    public class AuthRegisterRequest
    {
        public string Name { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string UserType { get; set; } = string.Empty;

        public string? SqlServerName { get; set; }

        public string? SqlUserName { get; set; }

        public string? SqlPassword { get; set; }
    }
}
