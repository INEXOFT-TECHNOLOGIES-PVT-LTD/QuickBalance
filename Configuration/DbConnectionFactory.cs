using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using QuickBalance.API.DB;

namespace QuickBalance.API.Configuration
{
    public sealed class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly DatabaseSettings _settings;

        public DbConnectionFactory(
            IOptions<DatabaseSettings> options)
        {
            _settings = options.Value;
        }

        public string GetConnectionString(
            Variable variables,
            bool isMasterDB = false)
        {
            ArgumentNullException.ThrowIfNull(variables);

            var databaseName = isMasterDB
                ? "MasterDB"
                : _settings.DatabaseName;

            var builder = new SqlConnectionStringBuilder
            {
                DataSource = _settings.ServerName,
                InitialCatalog = databaseName,
                UserID = _settings.UserName,
                Password = _settings.Password,
                Encrypt = true,
                TrustServerCertificate = true,
                MultipleActiveResultSets = true,
                PersistSecurityInfo = false
            };

            // Preserve existing Variable initialization.
            //variables.ServerName = _settings.ServerName;
            //variables.DatabaseName = _settings.DatabaseName;
            //variables.UserName = _settings.UserName;
            //variables.Password = _settings.Password;

            variables.ServerName = _settings.ServerName;
            variables.DatabaseName = databaseName;
            variables.UserName = _settings.UserName;
            variables.Password = _settings.Password;
            // Preserve the existing CompCode derivation.
            string value = variables.DatabaseName;
            int number = 1;

            if (!string.IsNullOrEmpty(value))
            {
                string part = value
                    .Split('_')[0]
                    .Replace("COMP", "");

                if (int.TryParse(part, out int result))
                {
                    number = result;
                }
            }

            variables.CompCode = number;

            return builder.ConnectionString;
        }

        public SqlConnection CreateConnection(
            Variable variables,
            bool isMasterDB = false)
        {
            string connectionString =
                GetConnectionString(variables, isMasterDB);

            return new SqlConnection(connectionString);
        }
    }
}