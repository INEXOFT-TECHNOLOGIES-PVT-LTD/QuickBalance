namespace QuickBalance.API.CLS
{
    public class ClsUser
    {
        private string? _userName;
        public string UserName
        {
            get => _userName?.Replace("'", "''") ?? string.Empty;
            set => _userName = value;
        }

        private string? _password;
        public string Password
        {
            get => _password?.Replace("'", "''") ?? string.Empty;
            set => _password = value;
        }
    }
}
