using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace QuickBalance.API.Services
{
    public sealed class TokenRevocationService
    {
        private readonly string _connectionString;

        public TokenRevocationService(string connectionString)
        {
            _connectionString = connectionString;
        }

        private static string GetTokenHash(string token)
        {
            byte[] tokenBytes = Encoding.UTF8.GetBytes(token);
            byte[] hashBytes = SHA256.HashData(tokenBytes);

            return Convert.ToHexString(hashBytes);
        }

        public async Task RevokeTokenAsync(
            string token,
            DateTime expiresAtUtc)
        {
            string tokenHash = GetTokenHash(token);

            const string sql = @"
                INSERT INTO dbo.RevokedTokens (TokenHash, ExpiresAt) VALUES  (@TokenHash, @ExpiresAt);";

            await using var connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync();

            await using var command =
                new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@TokenHash", tokenHash);
            command.Parameters.AddWithValue(
                "@ExpiresAt",
                expiresAtUtc
            );

            await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> IsTokenRevokedAsync(string token)
        {
            string tokenHash = GetTokenHash(token);

            const string sql = @"
                SELECT CASE WHEN EXISTS
                (
                    SELECT 1 FROM dbo.RevokedTokens WHERE TokenHash = @TokenHash AND ExpiresAt > SYSUTCDATETIME()
                )
                THEN 1 ELSE 0 END;";

            await using var connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync();

            await using var command =
                new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@TokenHash", tokenHash);

            object? result = await command.ExecuteScalarAsync();

            return Convert.ToInt32(result) == 1;
        }
    }
}
