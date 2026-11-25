using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using QuickBalance.API.DB;

namespace QuickBalance.API.Configuration;

public sealed class DatabaseConnection
{
    private readonly IConfiguration _configuration;

    public DatabaseConnection(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetConnectionString(
        Variable variables,
        bool isMasterDB = false)
    {
        var settings = _configuration.GetSection("DatabaseSettings");

        var server = settings["ServerName"];
        var database = isMasterDB
            ? "MasterDB"
            : settings["DatabaseName"];

        var username = settings["UserName"];
        var password = settings["Password"];

        if (string.IsNullOrWhiteSpace(server) ||
            string.IsNullOrWhiteSpace(database) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Database configuration is incomplete.");
        }

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database,
            UserID = username,
            Password = password,
            Encrypt = true,
            TrustServerCertificate = true,
            MultipleActiveResultSets = true
        };

        variables.ServerName = server;
        variables.DatabaseName = database;
        variables.UserName = username;
        variables.Password = password;

        if (!isMasterDB)
        {
            var companyPart = database.Split('_')[0]
                .Replace("COMP", "");

            variables.CompCode =
                int.TryParse(companyPart, out var code) ? code : 1;
        }

        return builder.ConnectionString;
    }
}


//using Microsoft.Data.SqlClient;
//using Microsoft.Extensions.Configuration;
//using QuickBalance.API.DB;

//namespace QuickBalance.API.Configuration
//{
//    public class DatabaseConnection
//    {
//        //public string GetConnectionString(Variable Variables)
//        //{
//        //    var configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json", optional: false, reloadOnChange: false).Build();
//        //    var settings = configuration.GetSection("DatabaseSettings");
//        //    var builder = new SqlConnectionStringBuilder
//        //    {
//        //        DataSource = settings["ServerName"],
//        //        InitialCatalog = settings["DatabaseName"],
//        //        UserID = settings["UserName"],
//        //        Password = settings["Password"],
//        //        Encrypt = true,
//        //        TrustServerCertificate = true,
//        //        MultipleActiveResultSets = true
//        //    };

//        //    Variables.ServerName = settings["ServerName"];
//        //    Variables.DatabaseName = settings["DatabaseName"];
//        //    Variables.UserName = settings["UserName"];
//        //    Variables.Password = settings["Password"];
//        //    Variables.ServerName = settings["ServerName"];

//        //    string value = Variables.DatabaseName;
//        //    int number = 1;
//        //    if (!string.IsNullOrEmpty(value))
//        //    {
//        //        string part = value.Split('_')[0].Replace("COMP", "");
//        //        if (int.TryParse(part, out int result))
//        //            number = result;
//        //    }
//        //    Variables.CompCode = number;

//        //    return builder.ConnectionString;
//        //}
//        public string GetConnectionString(Variable Variables, bool isMasterDB = false)
//        {
//            var configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json", optional: false, reloadOnChange: false).Build();
//            var settings = configuration.GetSection("DatabaseSettings");
//            var builder = new SqlConnectionStringBuilder
//            {
//                DataSource = settings["ServerName"],
//                InitialCatalog = isMasterDB ? "MasterDB" : settings["DatabaseName"],
//                UserID = settings["UserName"],
//                Password = settings["Password"],
//                Encrypt = true,
//                TrustServerCertificate = true,
//                MultipleActiveResultSets = true
//            };

//            Variables.ServerName = settings["ServerName"];
//            Variables.DatabaseName = settings["DatabaseName"];
//            Variables.UserName = settings["UserName"];
//            Variables.Password = settings["Password"];
//            Variables.ServerName = settings["ServerName"];

//            string value = Variables.DatabaseName;
//            int number = 1;
//            if (!string.IsNullOrEmpty(value))
//            {
//                string part = value.Split('_')[0].Replace("COMP", "");
//                if (int.TryParse(part, out int result))
//                    number = result;
//            }
//            Variables.CompCode = number;

//            return builder.ConnectionString;
//        }
//    }
//}