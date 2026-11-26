# DI setup (verbatim copies)

## Program.cs lines 40-95
Source: `Program.cs:40-95`

```csharp
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

```

## IDbConnectionFactory (full file, 16 lines)
Source: `Configuration/DB/IDbConnectionFactory.cs:1-16`

```csharp
using Microsoft.Data.SqlClient;
using QuickBalance.API.DB;

namespace QuickBalance.API.Configuration
{
    public interface IDbConnectionFactory
    {
        string GetConnectionString(
            Variable variables,
            bool isMasterDB = false);

        SqlConnection CreateConnection(
            Variable variables,
            bool isMasterDB = false);
    }
}
```

## DbConnectionFactory (full file, 80 lines)
Source: `Configuration/DbConnectionFactory.cs:1-80`

```csharp
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
```
