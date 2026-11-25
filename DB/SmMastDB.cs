using System.Data;

namespace QuickBalance.API.DB
{
    public class SmMastDB
    {
        private readonly DBConn _db;

        public SmMastDB(DBConn db)
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
        // INSERT
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
        // UPDATE
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
        // GET BY ID
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
        // DUPLICATE CHECK - CREATE
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
        // DUPLICATE CHECK - UPDATE
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
        // NEXT SALESMAN CODE
        // Legacy:
        // db.GetnewId("SMCODE", "SMMAST")
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
        // NEXT EMPLOYEE CODE
        // Legacy:
        // db.GetnewId("EMPCODE", "SMMAST")
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
        // PAGINATED LIST
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
        // TOTAL COUNT
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
        // SIMPLE LOOKUP
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
        // ACTIVE SALESMEN
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
        // VALIDATE ACCOUNT
        // Legacy:
        // SELECT * FROM SMMAST WHERE ACCODE = AccId
        // =========================================================
        public async Task<DataTable> ValidateAcc(int accId)
        {
            string qry = $@"
                SELECT *
                FROM SMMAST
                WHERE ACCODE = {accId}";

            return await _db.GetDatatable(qry);
        }
    }
}