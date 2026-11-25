namespace QuickBalance.API.DTOs
{
    public class LoginResponse
    {
        //public bool Status { get; set; }
        //public string Message { get; set; } = "";
        public string Token { get; set; } = "";
        //public LoginUserData? User { get; set; }
    }

    public class LoginUserData
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
    }

    public sealed class LoginUserRecord
    {
        public int UserCode { get; set; }

        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

    }
}