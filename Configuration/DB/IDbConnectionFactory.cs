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