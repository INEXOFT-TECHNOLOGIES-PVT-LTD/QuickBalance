using QuickBalance.API.DB;
using QuickBalance.API.DTOs.SalesMan;
using System.Data;

namespace QuickBalance.API.Repository.Master
{
    public class SalesManRepository
    {
        private readonly DBConn _db;

        public SalesManRepository(DBConn db)
        {
            _db = db;
        }

        private string SqlEscape(string? value)
        {
            return (value ?? "").Replace("'", "''");
        }

        private int BitValue(bool value)
        {
            return value ? 1 : 0;
        }

        // =========================================================
        // LEGACY INSERT
        // =========================================================
        public async Task<int> Insert(
            int empCode,
            string smName,
            string printName,
            string address,
            string phone,
            string email,
            int smGrpCode,
            bool active,
            bool smAccFlg,
            int acCode,
            int logCode,
            string secName)
        {
            string qry = $@"
                INSERT INTO SMMAST
                (
                    EMPCODE,
                    SMNAME,
                    PRINTNAME,
                    ADDRESS,
                    PHONE,
                    EMAIL,
                    SMGRPCODE,
                    ACTIVE,
                    SMACCFLG,
                    ACCODE,
                    LOGCD,
                    UTIME,
                    SECNAME
                )
                OUTPUT INSERTED.SMCODE
                VALUES
                (
                    {empCode},
                    '{SqlEscape(smName)}',
                    '{SqlEscape(printName)}',
                    '{SqlEscape(address)}',
                    '{SqlEscape(phone)}',
                    '{SqlEscape(email)}',
                    {smGrpCode},
                    {BitValue(active)},
                    {BitValue(smAccFlg)},
                    {acCode},
                    {logCode},
                    GETDATE(),
                    N'{SqlEscape(secName)}'
                )";

            return await _db.InsertScalar(qry);
        }

        // =========================================================
        // LEGACY UPDATE
        // =========================================================
        public async Task<int> Update(
            int smCode,
            int empCode,
            string smName,
            string printName,
            string address,
            string phone,
            string email,
            int smGrpCode,
            bool active,
            bool smAccFlg,
            int acCode,
            string secName)
        {
            string qry = $@"
                UPDATE SMMAST
                SET
                    EMPCODE = {empCode},
                    SMNAME = '{SqlEscape(smName)}',
                    PRINTNAME = '{SqlEscape(printName)}',
                    ADDRESS = '{SqlEscape(address)}',
                    PHONE = '{SqlEscape(phone)}',
                    EMAIL = '{SqlEscape(email)}',
                    SMGRPCODE = {smGrpCode},
                    ACTIVE = {BitValue(active)},
                    SMACCFLG = {BitValue(smAccFlg)},
                    ACCODE = {acCode},
                    SECNAME = N'{SqlEscape(secName)}'
                WHERE SMCODE = {smCode}";

            return await _db.Execute(qry);
        }

        // =========================================================
        // LEGACY GET BY ID
        // =========================================================
        public async Task<DataTable> SearchByID(int smCode)
        {
            string qry = $@"
                SELECT *
                FROM SMMAST
                WHERE SMCODE = {smCode}";

            return await _db.GetDatatable(qry);
        }

        // =========================================================
        // LEGACY DUPLICATE CHECK - CREATE
        // =========================================================
        public async Task<DataTable> GetAllByName(string smName)
        {
            string qry = $@"
                SELECT *
                FROM SMMAST
                WHERE SMNAME = '{SqlEscape(smName)}'";

            return await _db.GetDatatable(qry);
        }

        // =========================================================
        // LEGACY DUPLICATE CHECK - UPDATE
        // =========================================================
        public async Task<DataTable> CheckNameExists(
            string smName,
            int smCode)
        {
            string qry = $@"
                SELECT *
                FROM SMMAST
                WHERE SMNAME = '{SqlEscape(smName)}'
                  AND SMCODE <> {smCode}";

            return await _db.GetDatatable(qry);
        }

        // =========================================================
        // LEGACY NEXT SALESMAN CODE
        // =========================================================
        public async Task<int> MaxId()
        {
            string qry = @"
                SELECT ISNULL(MAX(SMCODE), 0) + 1
                FROM SMMAST";

            object? result = await _db.ExecuteScalar(qry);

            if (result == null || result == DBNull.Value)
                return 1;

            return Convert.ToInt32(result);
        }

        // =========================================================
        // LEGACY NEXT EMPLOYEE CODE
        // =========================================================
        public async Task<int> GetMaxCode()
        {
            string qry = @"
                SELECT ISNULL(MAX(EMPCODE), 0) + 1
                FROM SMMAST";

            object? result = await _db.ExecuteScalar(qry);

            if (result == null || result == DBNull.Value)
                return 1;

            return Convert.ToInt32(result);
        }

        // =========================================================
        // LEGACY PAGINATED LIST
        // =========================================================
        public async Task<DataTable> Search(
            string? search,
            int offset,
            int pageSize)
        {
            string where = "";

            if (!string.IsNullOrWhiteSpace(search))
            {
                string value = SqlEscape(search);

                where = $@"
                    WHERE
                        SMNAME LIKE '%{value}%'
                        OR PRINTNAME LIKE '%{value}%'
                        OR SECNAME LIKE '%{value}%'
                        OR PHONE LIKE '%{value}%'
                        OR EMAIL LIKE '%{value}%'";
            }

            string qry = $@"
                SELECT
                    SMCODE,
                    EMPCODE,
                    SMNAME,
                    SECNAME,
                    PRINTNAME,
                    ADDRESS,
                    PHONE,
                    EMAIL,
                    SMGRPCODE,
                    ACTIVE,
                    SMACCFLG,
                    ACCODE,
                    LOGCD,
                    UTIME
                FROM SMMAST
                {where}
                ORDER BY SMNAME
                OFFSET {offset} ROWS
                FETCH NEXT {pageSize} ROWS ONLY";

            return await _db.GetDatatable(qry);
        }

        // =========================================================
        // LEGACY TOTAL COUNT
        // =========================================================
        public async Task<int> Count(string? search)
        {
            string where = "";

            if (!string.IsNullOrWhiteSpace(search))
            {
                string value = SqlEscape(search);

                where = $@"
                    WHERE
                        SMNAME LIKE '%{value}%'
                        OR PRINTNAME LIKE '%{value}%'
                        OR SECNAME LIKE '%{value}%'
                        OR PHONE LIKE '%{value}%'
                        OR EMAIL LIKE '%{value}%'";
            }

            string qry = $@"
                SELECT COUNT(*)
                FROM SMMAST
                {where}";

            object? result = await _db.ExecuteScalar(qry);

            if (result == null || result == DBNull.Value)
                return 0;

            return Convert.ToInt32(result);
        }

        // =========================================================
        // LEGACY SIMPLE LOOKUP
        // =========================================================
        public async Task<DataTable> SearchLookup()
        {
            string qry = @"
                SELECT
                    SMCODE AS Id,
                    SMNAME AS Salesman
                FROM SMMAST
                ORDER BY SMNAME";

            return await _db.GetDatatable(qry);
        }

        // =========================================================
        // LEGACY ACTIVE SALESMEN
        // =========================================================
        public async Task<DataTable> ActiveSalesman()
        {
            string qry = @"
                SELECT
                    SMCODE,
                    SMNAME
                FROM SMMAST
                WHERE ACTIVE = 1
                ORDER BY SMNAME";

            return await _db.GetDatatable(qry);
        }

        // =========================================================
        // LEGACY VALIDATE ACCOUNT
        // =========================================================
        public async Task<DataTable> ValidateAcc(int accId)
        {
            string qry = $@"
                SELECT *
                FROM SMMAST
                WHERE ACCODE = {accId}";

            return await _db.GetDatatable(qry);
        }

        // =========================================================
        // NEW API - GET NEXT EMPLOYEE CODE
        // =========================================================
        public async Task<int> GetMaxCodeAsync()
        {
            const string query = """
                SELECT ISNULL(MAX(EMPCODE), 0) + 1
                FROM SMMAST
                """;

            return await _db.QueryFirstOrDefaultAsync<int>(query);
        }




        // =========================================================
        // NEW API - INSERT SALESMAN
        // =========================================================
        public async Task<int> InsertAsync(
            int empCode,
            string smName,
            string printName,
            string address,
            string phone,
            string email,
            int smGrpCode,
            bool active,
            bool smAccFlg,
            int acCode,
            int logCode,
            string secName)
        {
            const string query = """
                INSERT INTO SMMAST
                (
                    EMPCODE,
                    SMNAME,
                    PRINTNAME,
                    ADDRESS,
                    PHONE,
                    EMAIL,
                    SMGRPCODE,
                    ACTIVE,
                    SMACCFLG,
                    ACCODE,
                    LOGCD,
                    UTIME,
                    SECNAME
                )
                OUTPUT INSERTED.SMCODE
                VALUES
                (
                    @EmpCode,
                    @SmName,
                    @PrintName,
                    @Address,
                    @Phone,
                    @Email,
                    @SmGrpCode,
                    @Active,
                    @SmAccFlg,
                    @AcCode,
                    @LogCode,
                    GETDATE(),
                    @SecName
                )
                """;

            return await _db.QueryFirstOrDefaultAsync<int>(
                query,
                new
                {
                    EmpCode = empCode,
                    SmName = smName,
                    PrintName = printName,
                    Address = address,
                    Phone = phone,
                    Email = email,
                    SmGrpCode = smGrpCode,
                    Active = active,
                    SmAccFlg = smAccFlg,
                    AcCode = acCode,
                    LogCode = logCode,
                    SecName = secName
                });
        }

        // =========================================================
        // NEW API - TOTAL COUNT
        // =========================================================
        public async Task<int> GetTotalCountAsync(string? search, bool? active = null)
        {
            const string query = """
        SELECT COUNT(*)
        FROM SMMAST
        WHERE
            (@Active IS NULL OR ISNULL(ACTIVE, 0) = @Active)
            AND DELFLG = 0
            AND
            (
                @Search IS NULL
                OR SMNAME LIKE @Search
                OR PRINTNAME LIKE @Search
                OR SECNAME LIKE @Search
                OR PHONE LIKE @Search
                OR EMAIL LIKE @Search
            )
        """;

            string? searchValue = string.IsNullOrWhiteSpace(search)
                ? null
                : $"%{search.Trim()}%";

            return await _db.QueryFirstOrDefaultAsync<int>(
                query,
                new { Search = searchValue, Active = active });
        }

        // =========================================================
        // NEW API - GET ALL SALESMEN
        // =========================================================
        public async Task<List<SalesmanResponse>> GetAllAsync(
    int page,
    int pageSize,
    string? search,
    bool? active = null)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            int offset = (page - 1) * pageSize;

            const string query = """
        SELECT
            SMCODE AS Id,
            EMPCODE AS EmployeeCode,
            SMNAME AS SalesmanName,
            SECNAME AS SecondName,
            PRINTNAME AS PrintName,
            ADDRESS AS Address,
            PHONE AS Phone,
            EMAIL AS Email,
            SMGRPCODE AS SalesmanGroupId,
            ACTIVE AS IsActive,
            SMACCFLG AS ConnectWithAccount,
            ACCODE AS AccountId,
            LOGCD AS LogCode,
            UTIME AS UpdateTime
        FROM SMMAST
        WHERE
            (@Active IS NULL OR ISNULL(ACTIVE, 0) = @Active)
            AND DELFLG = 0
            AND
            (
                @Search IS NULL
                OR SMNAME LIKE @Search
                OR PRINTNAME LIKE @Search
                OR SECNAME LIKE @Search
                OR PHONE LIKE @Search
                OR EMAIL LIKE @Search
            )
        ORDER BY SMNAME
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY
        """;

            string? searchValue = string.IsNullOrWhiteSpace(search)
                ? null
                : $"%{search.Trim()}%";

            return await _db.QueryListAsync<SalesmanResponse>(
                query,
                new
                {
                    Search = searchValue,
                    Active = active,
                    Offset = offset,
                    PageSize = pageSize
                });
        }

        // =========================================================
        // NEW API - GET SALESMAN BY ID
        // =========================================================
        public async Task<SalesmanResponse?> GetByIdAsync(int id)
        {
            const string query = """
        SELECT
            SMCODE AS Id,
            EMPCODE AS EmployeeCode,
            SMNAME AS SalesmanName,
            SECNAME AS SecondName,
            PRINTNAME AS PrintName,
            ADDRESS AS Address,
            PHONE AS Phone,
            EMAIL AS Email,
            SMGRPCODE AS SalesmanGroupId,
            ACTIVE AS IsActive,
            SMACCFLG AS ConnectWithAccount,
            ACCODE AS AccountId,
            LOGCD AS LogCode,
            UTIME AS UpdateTime
          FROM SMMAST
          WHERE
            SMCODE = @Id
            AND DELFLG = 0
        """;

            return await _db.QueryFirstOrDefaultAsync<SalesmanResponse>(
                query,
                new { Id = id });
        }

        // =========================================================
        // NEW API - UPDATE SALESMAN
        // =========================================================
        public async Task<int> UpdateAsync(
            int smCode,
            int empCode,
            string smName,
            string printName,
            string address,
            string phone,
            string email,
            int smGrpCode,
            bool active,
            bool smAccFlg,
            int acCode,
            string secName)
        {
            const string query = """
                UPDATE SMMAST
                SET
                    EMPCODE = @EmpCode,
                    SMNAME = @SmName,
                    PRINTNAME = @PrintName,
                    ADDRESS = @Address,
                    PHONE = @Phone,
                    EMAIL = @Email,
                    SMGRPCODE = @SmGrpCode,
                    ACTIVE = @Active,
                    SMACCFLG = @SmAccFlg,
                    ACCODE = @AcCode,
                    SECNAME = @SecName
                WHERE SMCODE = @SmCode
                """;

            return await _db.ExecuteAsync(
                query,
                new
                {
                    SmCode = smCode,
                    EmpCode = empCode,
                    SmName = smName,
                    PrintName = printName,
                    Address = address,
                    Phone = phone,
                    Email = email,
                    SmGrpCode = smGrpCode,
                    Active = active,
                    SmAccFlg = smAccFlg,
                    AcCode = acCode,
                    SecName = secName
                });
        }

        // =========================================================
        // NEW API - GET ACTIVE SALESMEN
        // =========================================================
        public async Task<List<ActiveSalesmanResponse>> GetActiveAsync()
        {
            const string query = """
                SELECT
                    SMCODE AS Id,
                    SMCODE AS SalesmanCode,
                    SMNAME AS SalesmanName
                FROM SMMAST
                WHERE ACTIVE = 1
                ORDER BY SMNAME
                """;

            return await _db.QueryListAsync<ActiveSalesmanResponse>(
                query);
        }

        // =========================================================
        // NEW API - SALESMAN LOOKUP
        // =========================================================
        public async Task<List<SalesmanLookupResponse>> GetLookupAsync()
        {
            const string query = """
                SELECT
                    SMCODE AS Id,
                    SMNAME AS Salesman
                FROM SMMAST
                ORDER BY SMNAME
                """;

            return await _db.QueryListAsync<SalesmanLookupResponse>(
                query);
        }

        // =========================================================
        // NEW API - VALIDATE ACCOUNT
        // =========================================================
        public async Task<List<SalesmanResponse>> ValidateAccountAsync(
            int accountId)
        {
            const string query = """
                SELECT
                    SMCODE AS Id,
                    EMPCODE AS EmployeeCode,
                    SMNAME AS SalesmanName,
                    SECNAME AS SecondName,
                    PRINTNAME AS PrintName,
                    ADDRESS AS Address,
                    PHONE AS Phone,
                    EMAIL AS Email,
                    SMGRPCODE AS SalesmanGroupId,
                    ACTIVE AS IsActive,
                    SMACCFLG AS ConnectWithAccount,
                    ACCODE AS AccountId,
                    LOGCD AS LogCode,
                    UTIME AS UpdateTime
                FROM SMMAST
                WHERE ACCODE = @AccountId
                ORDER BY SMNAME
                """;

            return await _db.QueryListAsync<SalesmanResponse>(
                query,
                new { AccountId = accountId });
        }

        // =========================================================
        // NEW API - DUPLICATE CHECK FOR CREATE
        // =========================================================
        public async Task<bool> NameExistsAsync(string smName)
        {
            const string query = """
                SELECT CASE
                    WHEN EXISTS
                    (
                        SELECT 1
                        FROM SMMAST
                        WHERE SMNAME = @SmName
                    )
                    THEN CAST(1 AS BIT)
                    ELSE CAST(0 AS BIT)
                END
                """;

            return await _db.QueryFirstOrDefaultAsync<bool>(
                query,
                new { SmName = smName });
        }

        // =========================================================
        // NEW API - DUPLICATE CHECK FOR UPDATE
        // =========================================================
        public async Task<bool> NameExistsAsync(
            string smName,
            int smCode)
        {
            const string query = """
                SELECT CASE
                    WHEN EXISTS
                    (
                        SELECT 1
                        FROM SMMAST
                        WHERE SMNAME = @SmName
                          AND SMCODE <> @SmCode
                    )
                    THEN CAST(1 AS BIT)
                    ELSE CAST(0 AS BIT)
                END
                """;

            return await _db.QueryFirstOrDefaultAsync<bool>(
                query,
                new
                {
                    SmName = smName,
                    SmCode = smCode
                });
        }


        // =========================================================
        // SOFT DELETE SALESMAN
        // =========================================================
        public async Task<int> SoftDeleteAsync(int salesmanCode)
        {
            const string query = @"
        UPDATE SMMAST
        SET DELFLG = 1
        WHERE SMCODE = @SalesmanCode
          AND DELFLG = 0";

            return await _db.ExecuteAsync(
                query,
                new { SalesmanCode = salesmanCode });
        }
    }
}
