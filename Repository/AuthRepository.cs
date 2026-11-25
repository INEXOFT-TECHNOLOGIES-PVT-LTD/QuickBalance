using QuickBalance.API.DB;
using QuickBalance.API.DTOs;


    public sealed class AuthRepository
    {
        private readonly DBConn _db;

        public AuthRepository(DBConn db)
        {
            _db = db;
        }

        public async Task<LoginUserRecord?> LoginAsync(
            string username,
            string encodedPassword)
        {
            const string sql = """
            SELECT
                USERCODE AS UserCode,
                USERNAME AS UserName
            FROM dbo.TBUSER
            WHERE USERNAME = @UserName
              AND PSWD = @Password;
            """;

            return await _db.QueryFirstOrDefaultAsync<LoginUserRecord>(
                sql,
                new
                {
                    UserName = username,
                    Password = encodedPassword
                });
        }

        public async Task<LoginUserRecord?> GetUserByUsernameAsync(string username)
        {
            const string sql = """
                SELECT
                    USERCODE AS UserCode,
                    USERNAME AS UserName,
                    PSWD AS Password
                FROM dbo.TBUSER
                WHERE USERNAME = @UserName;
                """;

            return await _db.QueryFirstOrDefaultAsync<LoginUserRecord>(
                sql,
                new { UserName = username });
        }

        public async Task<bool> UpdatePasswordAsync(
        int userCode,
        string encryptedPassword)
        {
            const string sql = """
            UPDATE dbo.TBUSER
            SET PSWD = @Password
            WHERE USERCODE = @UserCode;
            """;

            var affectedRows = await _db.ExecuteAsync(
                sql,
                new
                {
                    UserCode = userCode,
                    Password = encryptedPassword
                });

            return affectedRows > 0;
        }
}