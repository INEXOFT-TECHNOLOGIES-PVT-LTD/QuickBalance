using System.Data;

namespace QuickBalance.API.DB
{
    public class SmGrpDB
    {
        private readonly DBConn _db;

        public SmGrpDB(DBConn db)
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
            string groupName,
            string printName,
            bool isActive,
            int logCode,
            string secondName)
        {
            string qry = $@"
                INSERT INTO SMGRP
                (
                    SMGRPNAME,
                    PRINTNAME,
                    ACTIVE,
                    LOGCD,
                    UTIME,
                    SECNAME
                )
                OUTPUT INSERTED.SMGRPCODE
                VALUES
                (
                    '{SqlEscape(groupName)}',
                    '{SqlEscape(printName)}',
                    {BitValue(isActive)},
                    {logCode},
                    GETDATE(),
                    '{SqlEscape(secondName)}'
                )";

            return await _db.InsertScalar(qry);
        }

        // =========================================================
        // UPDATE
        // =========================================================
        public async Task<int> Update(
            int groupCode,
            string groupName,
            string printName,
            bool isActive,
            string secondName)
        {
            string qry = $@"
                UPDATE SMGRP
                SET
                    SMGRPNAME = '{SqlEscape(groupName)}',
                    PRINTNAME = '{SqlEscape(printName)}',
                    ACTIVE = {BitValue(isActive)},
                    SECNAME = '{SqlEscape(secondName)}'
                WHERE SMGRPCODE = {groupCode}";

            return await _db.Execute(qry);
        }

        // =========================================================
        // CHECK NAME - CREATE
        // =========================================================
        public async Task<DataTable> GetAllByName(string groupName)
        {
            string qry = $@"
                SELECT *
                FROM SMGRP
                WHERE SMGRPNAME = '{SqlEscape(groupName)}'";

            return await _db.GetDatatable(qry);
        }

        // =========================================================
        // CHECK NAME - UPDATE
        // =========================================================
        public async Task<DataTable> CheckNameExists(
            string groupName,
            int groupCode)
        {
            string qry = $@"
                SELECT *
                FROM SMGRP
                WHERE SMGRPNAME = '{SqlEscape(groupName)}'
                  AND SMGRPCODE <> {groupCode}";

            return await _db.GetDatatable(qry);
        }

        // =========================================================
        // SEARCH / PAGINATED LIST
        // =========================================================
        public async Task<DataTable> Search(
            string? search = null)
        {
            string where = "";

            if (!string.IsNullOrWhiteSpace(search))
            {
                string value = SqlEscape(search);

                where = $@"
                    WHERE
                        SMGRPNAME LIKE '%{value}%'
                        OR SECNAME LIKE '%{value}%'
                        OR PRINTNAME LIKE '%{value}%'";
            }

            string qry = $@"
                SELECT
                    SMGRPCODE,
                    SMGRPNAME,
                    SECNAME,
                    PRINTNAME,
                    ACTIVE,
                    LOGCD,
                    UTIME
                FROM SMGRP
                {where}
                ORDER BY SMGRPNAME";

            return await _db.GetDatatable(qry);
        }

        // =========================================================
        // GROUP LOOKUP
        // =========================================================
        public async Task<DataTable> Groups()
        {
            string qry = @"
                SELECT
                    SMGRPCODE,
                    SMGRPNAME
                FROM SMGRP
                ORDER BY SMGRPNAME";

            return await _db.GetDatatable(qry);
        }

        // =========================================================
        // SEARCH BY ID
        // =========================================================
        public async Task<DataTable> SearchByID(int groupCode)
        {
            string qry = $@"
                SELECT *
                FROM SMGRP
                WHERE SMGRPCODE = {groupCode}";

            return await _db.GetDatatable(qry);
        }

        // =========================================================
        // MAX ID
        // =========================================================
        public async Task<int> MaxId()
        {
            string qry = @"
                SELECT ISNULL(MAX(SMGRPCODE), 0) + 1
                FROM SMGRP";

            object? result = await _db.ExecuteScalar(qry);

            if (result == null || result == DBNull.Value)
                return 1;

            return Convert.ToInt32(result);
        }
    }
}