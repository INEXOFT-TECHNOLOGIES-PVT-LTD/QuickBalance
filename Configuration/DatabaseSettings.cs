namespace QuickBalance.API.Configuration
{
    public sealed class DatabaseSettings
    {
        public const string SectionName = "DatabaseSettings";

        public string ServerName { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}