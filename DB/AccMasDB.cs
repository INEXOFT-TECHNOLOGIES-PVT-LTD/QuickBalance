using Dapper;
using Microsoft.VisualBasic;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs.Respons_DTO;
using System.Data;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace QuickBalance.API.DB
{
    public class AccMasDB
    {
        private readonly DBConn db;
        public Variable Variables = new Variable();
        public Function Functions = new Function();
        string qry = "";

        public AccMasDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public async Task<DataTable> SearchByID(ClsAccMas clsAccMas, bool DefBranch = true)
        {
            qry = "SELECT * FROM " + Functions.VRSYS_ALLOWBR_ACCMAST(DefBranch: DefBranch) + "ACCMAS WHERE ID='" + clsAccMas.Id + "'";
            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> ActiveAccountsForLedger(bool Unapproved, string MultiAccountCode)
        {
            qry = "SELECT ID,ACCNAME,OPENBAL,MGROUP FROM " + Functions.VRSYS_ALLOWBR_ACCMAST() + "ACCMAS WHERE (ACTIVE='" + true + "'" + Functions.APRVLI(true, Unapproved, "ACCMAS") + ")" + Functions.VRSYS_ALLOWBR_REPORT(true, "ACCMAS", true) + " AND ID IN (" + MultiAccountCode + ") ORDER BY ACCNAME";
            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> ActiveAccountsForLedger_Fast(int GrpCode, string Opn_table, string Trn_Table, bool Unapproved)
        {
            qry = "SELECT ID,ACCNAME,OPENBAL,MGROUP FROM " + Functions.VRSYS_ALLOWBR_ACCMAST() + "ACCMAS WHERE (ACTIVE='" + true + "'" + Functions.APRVLI(true, Unapproved, "ACCMAS") + ")" + (GrpCode == 0 ? "" : " AND MGROUP='" + GrpCode + "'") + Functions.VRSYS_ALLOWBR_REPORT(true, "ACCMAS", true) + " AND ACCMAS.ID IN (SELECT DISTINCT ACCID FROM (SELECT ACCID FROM " + Opn_table + " UNION ALL SELECT ADJCD FROM " + Opn_table + " UNION ALL SELECT ACCID FROM " + Trn_Table + " UNION ALL SELECT ADJCD FROM " + Trn_Table + " UNION ALL SELECT ID FROM ACCMAS WHERE OPENBAL <> 0 UNION ALL SELECT ACCID FROM OPN WHERE DEBIT > 0 OR CREDIT > 0 ) ACC) ORDER BY ACCNAME";
            return await db.GetDatatable(qry);


        }

        //public async Task<decimal> Opening(int AccId, bool DefBranch = false)
        //{
        //    qry = "SELECT FORMAT(OPENBAL,'" + Variables.DecString + "')  FROM " + Functions.VRSYS_ALLOWBR_ACCMAST(DefBranch: DefBranch) + "ACCMAS WHERE ID='" + AccId + "'";
        //    return await db.ReturnDecValue(qry);
        //}

        public async Task<DataTable> GetOPN_MultiCurr(ClsAccMas clsAccMas, int MultiCurrCode, bool MultiCurrMode)
        {
            if (Variables.VRSYS && Variables.BranchYN && Variables.VoucherTypeCodes != "")
                qry = "SELECT OPN.ACCID,SUM(" + (MultiCurrMode ? "IIF(OPN.CURCONVRATE<>0,(OPN.CREDIT-OPN.DEBIT)*OPN.CURCONVRATE,OPN.CREDIT-OPN.DEBIT)" : "IIF(OPN.CURCONVRATE<>0,(OPN.CREDIT-OPN.DEBIT)/OPN.CURCONVRATE,OPN.CREDIT-OPN.DEBIT)") + ") as OPNVAL FROM OPN INNER JOIN ACCMAS ON OPN.ACCID = ACCMAS.ID WHERE OPN.CURCODE = " + MultiCurrCode + (clsAccMas.Id != 0 ? " AND ACCMAS.ID=" + clsAccMas.Id : clsAccMas.LedgerGroupId != 0 ? " AND ACCMAS.MGROUP=" + clsAccMas.LedgerGroupId : "") + " AND IIF(OPN.BRCD IS NULL,0,OPN.BRCD) IN (" + Variables.VoucherTypeCodes + ") GROUP BY OPN.ACCID";
            else
                qry = "SELECT OPN.ACCID,SUM(" + (MultiCurrMode ? "IIF(OPN.CURCONVRATE<>0,(OPN.CREDIT-OPN.DEBIT)*OPN.CURCONVRATE,OPN.CREDIT-OPN.DEBIT)" : "IIF(OPN.CURCONVRATE<>0,(OPN.CREDIT-OPN.DEBIT)/OPN.CURCONVRATE,OPN.CREDIT-OPN.DEBIT)") + ") as OPNVAL FROM OPN INNER JOIN ACCMAS ON OPN.ACCID = ACCMAS.ID WHERE OPN.CURCODE = " + MultiCurrCode + (clsAccMas.Id != 0 ? " AND ACCMAS.ID=" + clsAccMas.Id : clsAccMas.LedgerGroupId != 0 ? " AND ACCMAS.MGROUP=" + clsAccMas.LedgerGroupId : "") + " GROUP BY OPN.ACCID";
            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> AccountsByGroupRpt(ClsAccMas clsAccMas, bool Unapproved)
        {
            qry = "SELECT ID,ACCNAME,OPENBAL,MGROUP,ALIAS,ACCODE FROM " + Functions.VRSYS_ALLOWBR_ACCMAST() + "ACCMAS WHERE MGROUP='" + clsAccMas.LedgerGroupId + "' AND (ACTIVE='" + true + "'" + Functions.APRVLI(true, Unapproved, "ACCMAS") + ")" + Functions.VRSYS_ALLOWBR_REPORT(true, "ACCMAS", true) + " ORDER BY ACCNAME";
            return await db.GetDatatable(qry);
        }
        public async Task<List<AccountLedgerDto>> AccountsByGroupAlls(
          ClsAccMas clsAccMas,
          bool Unapproved = false)
        {
            string query = $@"
                SELECT
                    ID AS Id,
                    ACCNAME AS AccName,
                    OPENBAL AS OpenBal,
                    MGROUP AS LedgerGroupId
                FROM ACCMAS
                WHERE MGROUP = @GroupId
                  AND (
                      ACTIVE = 1
                      {Functions.APRVLI(true, Unapproved, "ACCMAS")}
                  )
                ORDER BY ACCNAME;";

            return await db.QueryListAsync<AccountLedgerDto>(
                query,
                new { GroupId = clsAccMas.LedgerGroupId });
        }

        //        public async Task<DataTable> AccountsForSales()
        //        {
        //            int page = Variables.pgRequest.Page;
        //            int pageSize = Variables.pgRequest.PageSize;

        //            string orderBy = "ACCMAS.ACCNAME";
        //            switch (Variables.pgRequest.SortBy?.ToLower())
        //            {
        //                case "id":
        //                    orderBy = "ACCMAS.ID";
        //                    break;

        //                case "code":
        //                case "accode":
        //                    orderBy = "ACCMAS.ACCODE";
        //                    break;

        //                case "name":
        //                case "accname":
        //                case "customer":
        //                    orderBy = "ACCMAS.ACCNAME";
        //                    break;

        //                case "alias":
        //                    orderBy = "ACCMAS.ALIAS";
        //                    break;

        //                case "secname":
        //                    orderBy = "CUSMAS.SECNAME";
        //                    break;

        //                case "phone":
        //                    orderBy = "CUSMAS.PHONE";
        //                    break;

        //                case "vatregno":
        //                    orderBy = "CUSMAS.VATREGNO";
        //                    break;
        //            }
        //            string sortDirection = Variables.pgRequest.SortDirection == "DESC" ? "DESC" : "ASC";

        //            int offset = (page - 1) * pageSize;

        //            string? search = Variables.pgRequest.Search;
        //            string searchCondition = "";
        //            if (!string.IsNullOrWhiteSpace(search))
        //            {
        //                search = search.Replace("'", "''");
        //                searchCondition = $" AND (ACCMAS.ACCODE LIKE '%{search}%' OR ACCMAS.ACCNAME LIKE '%{search}%' OR ACCMAS.ALIAS LIKE '%{search}%' OR CUSMAS.SECNAME LIKE '%{search}%' OR CUSMAS.PHONE LIKE '%{search}%' OR CUSMAS.VATREGNO LIKE '%{search}%')";
        //            }

        //            qry = $@"SELECT ACCMAS.ID, ACCMAS.ACCODE, ACCMAS.ACCNAME, ACCMAS.ALIAS, ISNULL(CUSMAS.SECNAME, '') AS SECNAME, ISNULL(CUSMAS.PHONE, '') AS PHONE, ISNULL(CUSMAS.VATREGNO, '') AS VATREGNO, ISNULL(CUSMAS.ADDRESS, '') AS ADDRESS, ISNULL(CUSMAS.SHIPADDRESS, '') AS SHIPADDRESS, COUNT(*) OVER() AS TotalRecords
        //FROM ACCMAS LEFT JOIN CUSMAS ON ACCMAS.ID = CUSMAS.ACCID
        //WHERE (ACCMAS.MGROUP IN (26, 4) OR ACCMAS.MGROUP IN (SELECT GRPLDG.GROUPID FROM GRPLDG WHERE GRPLDG.MGROUP = 26)) AND ACCMAS.ACTIVE = 1
        //{Functions.VRSYS_ALLOWBR(true, "ACCMAS", true)}
        //{searchCondition}
        //ORDER BY {orderBy} {sortDirection}
        //OFFSET {offset} ROWS
        //FETCH NEXT {pageSize} ROWS ONLY";

        //            return await db.GetDatatable(qry);
        //        }

        //        public async Task<DataTable> AccountsForPurchase()
        //        {
        //            int page = Variables.pgRequest.Page;
        //            int pageSize = Variables.pgRequest.PageSize;

        //            string orderBy = "ACCMAS.ACCNAME";
        //            switch (Variables.pgRequest.SortBy?.ToLower())
        //            {
        //                case "id":
        //                    orderBy = "ACCMAS.ID";
        //                    break;

        //                case "code":
        //                case "accode":
        //                    orderBy = "ACCMAS.ACCODE";
        //                    break;

        //                case "name":
        //                case "accname":
        //                case "supplier":
        //                    orderBy = "ACCMAS.ACCNAME";
        //                    break;

        //                case "alias":
        //                    orderBy = "ACCMAS.ALIAS";
        //                    break;

        //                case "secname":
        //                    orderBy = "SUPMAS.SECNAME";
        //                    break;

        //                case "phone":
        //                    orderBy = "SUPMAS.PHONE";
        //                    break;

        //                case "vatregno":
        //                    orderBy = "SUPMAS.VATREGNO";
        //                    break;
        //            }
        //            string sortDirection = Variables.pgRequest.SortDirection == "DESC" ? "DESC" : "ASC";

        //            int offset = (page - 1) * pageSize;

        //            string? search = Variables.pgRequest.Search;
        //            string searchCondition = "";
        //            if (!string.IsNullOrWhiteSpace(search))
        //            {
        //                search = search.Replace("'", "''");
        //                searchCondition = $" AND (ACCMAS.ACCODE LIKE '%{search}%' OR ACCMAS.ACCNAME LIKE '%{search}%' OR ACCMAS.ALIAS LIKE '%{search}%' OR SUPMAS.SECNAME LIKE '%{search}%' OR SUPMAS.PHONE LIKE '%{search}%' OR SUPMAS.VATREGNO LIKE '%{search}%')";
        //            }

        //            qry = $@"SELECT ACCMAS.ID, ACCMAS.ACCODE, ACCMAS.ACCNAME, ACCMAS.ALIAS, ISNULL(SUPMAS.SECNAME, '') AS SECNAME, ISNULL(SUPMAS.PHONE, '') AS PHONE, ISNULL(SUPMAS.VATREGNO, '') AS VATREGNO, ISNULL(SUPMAS.ADDRESS, '') AS ADDRESS, ISNULL(SUPMAS.SHIPADDRESS, '') AS SHIPADDRESS, COUNT(*) OVER() AS TotalRecords
        //FROM ACCMAS LEFT JOIN SUPMAS ON ACCMAS.ID = SUPMAS.ACCID
        //WHERE (ACCMAS.MGROUP IN (27, 4) OR ACCMAS.MGROUP IN (SELECT GRPLDG.GROUPID FROM GRPLDG WHERE GRPLDG.MGROUP = 27)) AND ACCMAS.ACTIVE = 1
        //{Functions.VRSYS_ALLOWBR(true, "ACCMAS", true)}
        //{searchCondition}
        //ORDER BY {orderBy} {sortDirection}
        //OFFSET {offset} ROWS
        //FETCH NEXT {pageSize} ROWS ONLY";

        //            return await db.GetDatatable(qry);
        //        }



        public async Task<PartyQueryResult> GetPartyAccountsAsync(bool isSupplier)
        {
            int page = Variables.pgRequest.Page;
            int pageSize = Variables.pgRequest.PageSize;

            string masterTable = isSupplier ? "SUPMAS" : "CUSMAS";
            int groupId = isSupplier ? 27 : 26;

            string orderBy = Variables.pgRequest.SortBy?.Trim().ToLowerInvariant()
                switch
            {
                "id" => "ACCMAS.ID",
                "code" or "accode" => "ACCMAS.ACCODE",
                "name" or "accname" or "customer" or "supplier" => "ACCMAS.ACCNAME",
                "alias" => "ACCMAS.ALIAS",
                "secname" => $"{masterTable}.SECNAME",
                "phone" => $"{masterTable}.PHONE",
                "vatregno" => $"{masterTable}.VATREGNO",
                _ => "ACCMAS.ACCNAME"
            };

            string sortDirection =
                string.Equals(
                    Variables.pgRequest.SortDirection,
                    "DESC",
                    StringComparison.OrdinalIgnoreCase)
                ? "DESC"
                : "ASC";

            int offset = (page - 1) * pageSize;

            var parameters = new DynamicParameters();
            parameters.Add("Offset", offset);
            parameters.Add("PageSize", pageSize);
            parameters.Add("GroupId", groupId);

            string searchCondition = string.Empty;
            string? search = Variables.pgRequest.Search?.Trim();

            if (!string.IsNullOrWhiteSpace(search))
            {
                searchCondition = $@"
            AND (
                ACCMAS.ACCODE LIKE @Search
                OR ACCMAS.ACCNAME LIKE @Search
                OR ACCMAS.ALIAS LIKE @Search
                OR {masterTable}.SECNAME LIKE @Search
                OR {masterTable}.PHONE LIKE @Search
                OR {masterTable}.VATREGNO LIKE @Search
            )";

                parameters.Add("Search", $"%{search}%");
            }

            // Preserve the existing branch/user access filter.
            string branchFilter =
                Functions.VRSYS_ALLOWBR(true, "ACCMAS", true);

            string fromWhere = $@"
                FROM ACCMAS
                LEFT JOIN {masterTable}
                    ON ACCMAS.ID = {masterTable}.ACCID
                WHERE (
                    ACCMAS.MGROUP IN (@GroupId, 4)
                    OR ACCMAS.MGROUP IN (
                        SELECT GRPLDG.GROUPID
                        FROM GRPLDG
                        WHERE GRPLDG.MGROUP = @GroupId
                    )
                )
                AND ACCMAS.ACTIVE = 1
                {branchFilter}
                {searchCondition}";

                    string sql = $@"
                SELECT COUNT(DISTINCT ACCMAS.ID)
                {fromWhere};

                SELECT
                    ACCMAS.ID AS Id,
                    ACCMAS.ACCODE AS AccCode,
                    ACCMAS.ACCNAME AS AccName,
                    ACCMAS.ALIAS AS Alias,
                    ISNULL({masterTable}.SECNAME, '') AS SecName,
                    ISNULL({masterTable}.PHONE, '') AS Phone,
                    ISNULL({masterTable}.VATREGNO, '') AS VatRegNo,
                    ISNULL({masterTable}.ADDRESS, '') AS Address,
                    ISNULL({masterTable}.SHIPADDRESS, '') AS ShipAddress
                {fromWhere}
                ORDER BY {orderBy} {sortDirection}, ACCMAS.ID ASC
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY;";

            using var connection = db.GetConnection();

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            using var multi = await connection.QueryMultipleAsync(
                sql, parameters);

            int totalRecords = await multi.ReadFirstAsync<int>();

            var accounts = (await multi.ReadAsync<PartyAccountDto>())
                .ToList();

            return new PartyQueryResult
            {
                Data = accounts,
                TotalRecords = totalRecords
            };
        }

        public Task<PartyQueryResult> AccountsForSales()
        {
            return GetPartyAccountsAsync(isSupplier: false);
        }

        public Task<PartyQueryResult> AccountsForPurchase()
        {
            return GetPartyAccountsAsync(isSupplier: true);
        }
        public async Task<DataTable> AccountsByMulGroupRpt(string GrpIds, bool Unapproved)
        {
            qry = "SELECT ID,ACCNAME,OPENBAL,MGROUP FROM " + Functions.VRSYS_ALLOWBR_ACCMAST() + "ACCMAS WHERE MGROUP IN (" + GrpIds + ") AND (ACTIVE='" + true + "'" + Functions.APRVLI(true, Unapproved, "ACCMAS") + ")" + Functions.VRSYS_ALLOWBR_REPORT(true, "ACCMAS", true) + " ORDER BY ACCNAME";
            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> AccountsByGroupAll(ClsAccMas clsAccMas, bool Unapproved = false)
        {
            qry = "SELECT ID,ACCNAME,OPENBAL,MGROUP FROM ACCMAS WHERE MGROUP='" + clsAccMas.LedgerGroupId + "' AND (ACTIVE='" + true + "'" + Functions.APRVLI(true, Unapproved, "ACCMAS") + ")" + " ORDER BY ACCNAME";
            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> GetCashOrBankLedgers(bool Unapproved)
        {
            qry = "SELECT ID,ACCNAME,OPENBAL,MGROUP FROM " + Functions.VRSYS_ALLOWBR_ACCMAST() + "ACCMAS WHERE (ACTIVE='" + true + "'" + Functions.APRVLI(true, Unapproved, "ACCMAS") + ")" + " AND MGROUP IN (1,4) ORDER BY ACCNAME";
            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> GetLedgerOpeningBalanceReport(int AccGrp, int AccId, bool ZeroBal, bool Unapproved)
        {
            string tot = "N'" + LngTB.Total + "'";
            string t = "'TRUE'";

            qry = "SELECT ID, ACCODE, ACCNAME, FORMAT(IIF(OPENBAL < 0, -1*OPENBAL, 0), '" + Variables.DecString + "') AS DEBIT, FORMAT(IIF(OPENBAL >= 0, OPENBAL, 0), '" + Variables.DecString + "') AS CREDIT, '' AS PRVDEBIT, '' AS PRVCREDIT FROM " + Functions.VRSYS_ALLOWBR_ACCMAST(false) + "ACCMAS WHERE (ACTIVE = " + t + Functions.APRVLI(true, Unapproved, "ACCMAS") + ")" + (AccId > 0 ? " AND  ID = " + AccId : "") + (AccGrp > 0 ? " AND  MGROUP = " + AccGrp : "") + (ZeroBal ? "" : " AND OPENBAL <> 0") + " ORDER BY ACCNAME";
            DataTable dtls = await db.GetDatatable(qry);

            if (AccId == 0)
            {
                qry = "SELECT 0 AS ID, '' AS ACCODE, " + tot + " AS ACCNAME, FORMAT(SUM(IIF(OPENBAL < 0, -1*OPENBAL, 0)), '" + Variables.DecString + "') AS DEBIT, FORMAT(SUM(IIF(OPENBAL >= 0, OPENBAL, 0)), '" + Variables.DecString + "') AS CREDIT, '' AS PRVDEBIT, '' AS PRVCREDIT FROM " + Functions.VRSYS_ALLOWBR_ACCMAST(false) + "ACCMAS WHERE (ACTIVE = " + t + Functions.APRVLI(true, Unapproved, "ACCMAS") + ")" + (AccGrp > 0 ? " AND  MGROUP = " + AccGrp : "");
                dtls.Merge(await db.GetDatatable(qry), true, MissingSchemaAction.Ignore);
            }
            return dtls;
        }

        public async Task<int> GetGroupByPurAcc()
        {
            qry = "SELECT MGROUP FROM ACCMAS WHERE ID = 8";
            return await db.ReturnIntValue(qry);
        }
        public async Task<(List<AccountLedgerDtos> Data, int TotalRecords)>
            GetAllAccounts(
                int page = 1,
                int pageSize = 20,
                string? search = null,
                bool? active = null)
                //string? sortBy = "ACCNAME",
                //string? sortDirection = "ASC")
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            int offset = checked((page - 1) * pageSize);

            //string orderColumn = GetSortColumn(sortBy);
            //string direction = GetSortDirection(sortDirection);

            const string whereClause = @"
                WHERE ISNULL(DELFLG, 0) = 0
                  AND (@Active IS NULL OR ISNULL(ACTIVE, 0) = @Active)
                  AND (
                    @Search IS NULL
                    OR ACCNAME LIKE N'%' + @Search + N'%'
                    OR ISNULL(ALIAS, '') LIKE N'%' + @Search + N'%'
                    OR ISNULL(ACCODE, '') LIKE N'%' + @Search + N'%'
                    OR ISNULL(SECNAME, '') LIKE N'%' + @Search + N'%'
                  )";

            string countQuery = $@"
                SELECT COUNT(1)
                FROM ACCMAS
                {whereClause};";

            int totalRecords =
                await db.QueryFirstOrDefaultAsync<int>(
                    countQuery,
                    new
                    {
                        Search = string.IsNullOrWhiteSpace(search)
                            ? null
                            : search.Trim(),
                        Active = active
                    });

            
            string dataQuery = $@"
            SELECT
                ID AS Id,
                ACCNAME AS AccName,
                ALIAS AS Alias,
                ACCODE AS AcCode,
                SECNAME AS SecName,
                MGROUP AS LedgerGroupId,
                OPENBAL AS OpenBal,
                ACTIVE AS Active,
                CURFLG AS CurFlg,
                ADDRESS AS Address,
                PHONE AS Phone,
                PRINTNAME AS PrintName,
                DEPRECIATION AS Depreciation,
                VATPERC AS VatPerc,
                VATFLAG AS VatFlag,
                TAXRETFLAG AS TaxRetFlag,
                BANKACCNAME AS BankAccName,
                BANKACCNO AS BankAccNo,
                IBANNO AS IbanNo,
                SWIFTCode AS SwiftCode,
                BRANCH AS Branch,
                CURCONVRATE AS CurConvRate,
                CURCODE AS CurCode,
                CURFLG AS CurFlg,
                LOGCD AS CreatedBy,
                ROWVERSION AS RowVersion
            FROM ACCMAS
            {whereClause}
              ORDER BY  ID ASC
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;";
            //ORDER BY[{ orderColumn}] { direction}
            var data = await db.QueryListAsync<AccountLedgerDtos>(
                dataQuery,
                new
                {
                    Search = string.IsNullOrWhiteSpace(search)
                        ? null
                        : search.Trim(),
                    Active = active,
                    Offset = offset,
                    PageSize = pageSize
                });

            return (data, totalRecords);
        }


        public async Task<List<AccountLedgerDtos>> AccountsByGroupsIds(
 ClsAccMas clsAccMas)
        {
            string query = $@"
          SELECT
                ID AS Id,
                ACCNAME AS AccName,
                ALIAS AS Alias,
                ACCODE AS AcCode,
                SECNAME AS SecName,
                MGROUP AS LedgerGroupId,
                OPENBAL AS OpenBal,
                ACTIVE AS Active,
                CURFLG AS CurFlg,
                ADDRESS AS Address,
                PHONE AS Phone,
                PRINTNAME AS PrintName,
                DEPRECIATION AS Depreciation,
                VATPERC AS VatPerc,
                VATFLAG AS VatFlag,
                TAXRETFLAG AS TaxRetFlag,
                BANKACCNAME AS BankAccName,
                BANKACCNO AS BankAccNo,
                IBANNO AS IbanNo,
                SWIFTCode AS SwiftCode,
                BRANCH AS Branch,
                CURCONVRATE AS CurConvRate,
                CURCODE AS CurCode
        FROM ACCMAS

        WHERE MGROUP = @GroupId
          AND (
                ACTIVE = 1
              )

        ORDER BY ACCNAME;";

            return await db.QueryListAsync<AccountLedgerDtos>(
                query,
                new { GroupId = clsAccMas.LedgerGroupId });
        }

        public async Task<List<AccountLedgerDtos>> AccountsByGroupRpts(
          ClsAccMas clsAccMas,
          bool Unapproved)
        {
            string query = $@"
                SELECT
                    ID AS Id,
                    ACCNAME AS AccName,
                    OPENBAL AS OpenBal,
                    MGROUP AS LedgerGroupId,
                    ALIAS AS Alias,
                    ACCODE AS AcCode
                FROM {Functions.VRSYS_ALLOWBR_ACCMAST()}ACCMAS
                WHERE MGROUP = @GroupId
                  AND (
                      ACTIVE = 1
                      {Functions.APRVLI(true, Unapproved, "ACCMAS")}
                  )
                  {Functions.VRSYS_ALLOWBR_REPORT(true, "ACCMAS", true)}
                ORDER BY ACCNAME;";

            return await db.QueryListAsync<AccountLedgerDtos>(
                query,
                new { GroupId = clsAccMas.LedgerGroupId });
        }


        private static int[] ParseIds(string? values)
        {
            if (string.IsNullOrWhiteSpace(values))
                return Array.Empty<int>();

            var ids = new List<int>();

            foreach (string item in values.Split(','))
            {
                if (!int.TryParse(item.Trim(), out int id) || id <= 0)
                    throw new ArgumentException("The supplied ID list is invalid.");

                ids.Add(id);
            }

            return ids.Distinct().ToArray();
        }

        private static string ValidateTableName(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName) ||
                !System.Text.RegularExpressions.Regex.IsMatch(
                    tableName,
                    @"^[A-Za-z_][A-Za-z0-9_]*$"))
            {
                throw new ArgumentException("Invalid database table name.");
            }

            return $"[{tableName}]";
        }

        private static string GetSortColumn(string? sortBy)
        {
            var columns = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["ID"] = "ID",
                ["ACCNAME"] = "ACCNAME",
                ["ALIAS"] = "ALIAS",
                ["ACCODE"] = "ACCODE",
                ["SECNAME"] = "SECNAME",
                ["MGROUP"] = "MGROUP",
                ["OPENBAL"] = "OPENBAL",
                ["ACTIVE"] = "ACTIVE"
            };

            return columns.TryGetValue(sortBy ?? "", out string? column)
                ? column
                : "ACCNAME";
        }

        private static string GetSortDirection(string? sortDirection)
        {
            return string.Equals(
                sortDirection,
                "DESC",
                StringComparison.OrdinalIgnoreCase)
                    ? "DESC"
                    : "ASC";
        }


        // ------------------------------------------------------------
        // SEARCH ACCOUNT BY ID
        // ------------------------------------------------------------
        public async Task<AccountLedgerDtos?> SearchByIDs(
            ClsAccMas clsAccMas,
            bool DefBranch = true)
        {
            string query = $@"
        SELECT TOP (1)
            ID AS Id,
            ACCNAME AS AccName,
            ALIAS AS Alias,
            ACCODE AS AcCode,
            SECNAME AS SecName,
            ADDRESS AS Address,
            PHONE AS Phone,
            ACTIVE AS Active,
            MGROUP AS LedgerGroupId,
            OPENBAL AS OpenBal,
            CURFLG AS CurFlg,
            VATFLAG AS VatFlag,
            PRINTNAME AS PrintName,
            DEPRECIATION AS Depreciation,
            VATPERC AS VatPerc,
            TAXRETFLAG AS TaxRetFlag,
            BANKACCNAME AS BankAccName,
            BANKACCNO AS BankAccNo,
            IBANNO AS IbanNo,
            SWIFTCODE AS SwiftCode,
            BRANCH AS Branch,
            CURCONVRATE AS CurConvRate,
            CURCODE AS CurCode,
                ROWVERSION AS RowVersion

        FROM {Functions.VRSYS_ALLOWBR_ACCMAST(DefBranch: DefBranch)}ACCMAS
        WHERE ID = @Id;";

            return await db.QueryFirstOrDefaultAsync<AccountLedgerDtos>(
                query,
                new { Id = clsAccMas.Id });
        }

        // ------------------------------------------------------------
        // ACTIVE ACCOUNTS FOR LEDGER
        // ------------------------------------------------------------
        public async Task<List<AccountLedgerDto>> ActiveAccountsForLedgers(
            bool Unapproved,
            string MultiAccountCode)
        {
            int[] accountIds = ParseIds(MultiAccountCode);

            if (accountIds.Length == 0)
                return new List<AccountLedgerDto>();

            string query = $@"
        SELECT
            ID AS Id,
            ACCNAME AS AccName,
            OPENBAL AS OpenBal,
            MGROUP AS LedgerGroupId
        FROM {Functions.VRSYS_ALLOWBR_ACCMAST()}ACCMAS
        WHERE (
            ACTIVE = 1
            {Functions.APRVLI(true, Unapproved, "ACCMAS")}
        )
        {Functions.VRSYS_ALLOWBR_REPORT(true, "ACCMAS", true)}
        AND ID IN @AccountIds
        ORDER BY ACCNAME;";

            return await db.QueryListAsync<AccountLedgerDto>(
                query,
                new { AccountIds = accountIds });
        }

        // ------------------------------------------------------------
        // ACTIVE ACCOUNTS FOR LEDGER - FAST
        // ------------------------------------------------------------
        public async Task<List<AccountLedgerDto>> ActiveAccountsForLedger_Fasts(
            int GrpCode,
            string Opn_table,
            string Trn_Table,
            bool Unapproved)
        {
            string openingTable = ValidateTableName(Opn_table);
            string transactionTable = ValidateTableName(Trn_Table);

            string groupFilter = GrpCode == 0
                ? ""
                : " AND MGROUP = @GrpCode ";

            string query = $@"
        SELECT
            ID AS Id,
            ACCNAME AS AccName,
            OPENBAL AS OpenBal,
            MGROUP AS LedgerGroupId
        FROM {Functions.VRSYS_ALLOWBR_ACCMAST()}ACCMAS
        WHERE (
            ACTIVE = 1
            {Functions.APRVLI(true, Unapproved, "ACCMAS")}
        )
        {groupFilter}
        {Functions.VRSYS_ALLOWBR_REPORT(true, "ACCMAS", true)}
        AND ACCMAS.ID IN
        (
            SELECT DISTINCT ACCID
            FROM
            (
                SELECT ACCID FROM {openingTable}
                UNION ALL
                SELECT ADJCD FROM {openingTable}
                UNION ALL
                SELECT ACCID FROM {transactionTable}
                UNION ALL
                SELECT ADJCD FROM {transactionTable}
                UNION ALL
                SELECT ID FROM ACCMAS WHERE OPENBAL <> 0
                UNION ALL
                SELECT ACCID FROM OPN
                WHERE DEBIT > 0 OR CREDIT > 0
            ) AS ACC
        )
        ORDER BY ACCNAME;";

            return await db.QueryListAsync<AccountLedgerDto>(
                query,
                new { GrpCode });
        }

        // ------------------------------------------------------------
        // OPENING BALANCE
        // ------------------------------------------------------------
        public async Task<decimal> Opening(int AccId,bool DefBranch = false)
        {
            string query = $@"
        SELECT ISNULL(OPENBAL, 0)
        FROM {Functions.VRSYS_ALLOWBR_ACCMAST(DefBranch: DefBranch)}ACCMAS
        WHERE ID = @AccId;";

            decimal? result = await db.QueryFirstOrDefaultAsync<decimal?>(
                query,
                new { AccId });

            return result ?? 0;
        }
        //public async Task<decimal> Opening(int AccId, bool DefBranch = false)
        //{
        //    qry = "SELECT FORMAT(OPENBAL,'" + Variables.DecString + "')  FROM " + Functions.VRSYS_ALLOWBR_ACCMAST(DefBranch: DefBranch) + "ACCMAS WHERE ID='" + AccId + "'";
        //    return await db.ReturnDecValue(qry);
        //}

        // ------------------------------------------------------------
        // CASH OR BANK ACCOUNTS - DTO
        // ------------------------------------------------------------
        public async Task<List<AccountLedgerDtos>> GetCashOrBankLedgerss(
            bool Unapproved)
        {
            string query = $@"
        SELECT
            ID AS Id,
            ACCNAME AS AccName,
            OPENBAL AS OpenBal,
            MGROUP AS LedgerGroupId


        FROM {Functions.VRSYS_ALLOWBR_ACCMAST()}ACCMAS
        WHERE (
            ACTIVE = 1
            {Functions.APRVLI(true, Unapproved, "ACCMAS")}
        )
        AND MGROUP IN (1, 4)
        ORDER BY ACCNAME;";

            return await db.QueryListAsync<AccountLedgerDtos>(query);
        }

        // ------------------------------------------------------------
        // ACCOUNTS BY MULTIPLE GROUPS - DTO
        // ------------------------------------------------------------
        public async Task<List<AccountLedgerDto>> AccountsByMulGroupRpts(string GrpIds, bool Unapproved)
        {
            int[] groupIds = ParseIds(GrpIds);

            if (groupIds.Length == 0)
                return new List<AccountLedgerDto>();

            string query = $@"
        SELECT
            ID AS Id,
            ACCNAME AS AccName,
            OPENBAL AS OpenBal,
            MGROUP AS LedgerGroupId
        FROM {Functions.VRSYS_ALLOWBR_ACCMAST()}ACCMAS
        WHERE MGROUP IN @GroupIds
          AND (
              ACTIVE = 1
              {Functions.APRVLI(true, Unapproved, "ACCMAS")}
          )
          {Functions.VRSYS_ALLOWBR_REPORT(true, "ACCMAS", true)}
        ORDER BY ACCNAME;";

            return await db.QueryListAsync<AccountLedgerDto>(
                query,
                new { GroupIds = groupIds });
        }

        // ------------------------------------------------------------
        // MULTI-CURRENCY OPENING BALANCE - DTO
        // ------------------------------------------------------------
        //public async Task<List<AccountLedgerDto>> GetOPN_MultiCurrs(
        //    ClsAccMas clsAccMas,
        //    int MultiCurrCode
        //    bool MultiCurrMode)
        //        {
        //            string amountExpression = MultiCurrMode
        //                ? @"IIF(
        //                OPN.CURCONVRATE <> 0,
        //                (OPN.CREDIT - OPN.DEBIT) * OPN.CURCONVRATE,
        //                OPN.CREDIT - OPN.DEBIT)"
        //                : @"IIF(
        //                OPN.CURCONVRATE <> 0,
        //                (OPN.CREDIT - OPN.DEBIT) / OPN.CURCONVRATE,
        //                OPN.CREDIT - OPN.DEBIT)";

        //            string accountFilter = clsAccMas.Id != 0
        //                ? " AND ACCMAS.ID = @AccountId "
        //                : clsAccMas.MGroup != 0
        //                    ? " 
        //                    : "";
        //            //AND ACCMAS.MGROUP = @GroupId "
        //            string branchFilter = "";
        //            int[] branchIds = Array.Empty<int>();

        //            if (Variables.VRSYS &&
        //                Variables.BranchYN &&
        //                !string.IsNullOrWhiteSpace(Variables.VoucherTypeCodes))
        //            {
        //                branchIds = ParseIds(Variables.VoucherTypeCodes);
        //                branchFilter = " AND ISNULL(OPN.BRCD, 0) IN @BranchIds ";
        //            }

        //            string query = $@"
        //        SELECT
        //            OPN.ACCID AS Id,
        //            SUM({amountExpression}) AS OpnVal
        //        FROM OPN
        //        INNER JOIN ACCMAS ON OPN.ACCID = ACCMAS.ID
        //        WHERE OPN.CURCODE = @CurrencyCode
        //        {accountFilter}
        //        {branchFilter}
        //        GROUP BY OPN.ACCID;";

        //            return await db.QueryListAsync<AccountLedgerDto>(
        //                query,
        //                new
        //                {
        //                    CurrencyCode = MultiCurrCode,
        //                    AccountId = clsAccMas.Id,
        //                    //GroupId = clsAccMas.MGroup,
        //                    BranchIds = branchIds
        //                });
        //        }



        public async Task<List<AccountLedgerDto>> GetOPN_MultiCurrs(
    ClsAccMas clsAccMas,
    int MultiCurrCode,
    bool MultiCurrMode)
        {
            string amountExpression = MultiCurrMode
                ? @"CASE
                WHEN ISNULL(OPN.CURCONVRATE, 0) <> 0
                    THEN (OPN.CREDIT - OPN.DEBIT) * OPN.CURCONVRATE
                ELSE OPN.CREDIT - OPN.DEBIT
            END"
                : @"CASE
                WHEN ISNULL(OPN.CURCONVRATE, 0) <> 0
                    THEN (OPN.CREDIT - OPN.DEBIT) / NULLIF(OPN.CURCONVRATE, 0)
                ELSE OPN.CREDIT - OPN.DEBIT
            END";

            // Account filter only. No GroupId / MGroup filter.
            string accountFilter = clsAccMas.Id != 0
                ? " AND ACCMAS.ID = @AccountId "
                : "";

            string branchFilter = "";
            int[] branchIds = Array.Empty<int>();

            if (Variables.VRSYS &&
                Variables.BranchYN &&
                !string.IsNullOrWhiteSpace(Variables.VoucherTypeCodes))
            {
                branchIds = ParseIds(Variables.VoucherTypeCodes);
                branchFilter = " AND ISNULL(OPN.BRCD, 0) IN @BranchIds ";
            }

            string query = $@"
            SELECT
                OPN.ACCID AS Id,
                SUM({amountExpression}) AS OpnVal
            FROM OPN
            INNER JOIN ACCMAS ON OPN.ACCID = ACCMAS.ID
            WHERE OPN.CURCODE = @CurrencyCode
              AND ISNULL(ACCMAS.DELFLG, 0) = 0
              {accountFilter}
              {branchFilter}
            GROUP BY OPN.ACCID;";

            var parameters = new
            {
                CurrencyCode = MultiCurrCode,
                AccountId = clsAccMas.Id,
                BranchIds = branchIds
            };

            return await db.QueryListAsync<AccountLedgerDto>(
                query,
                parameters);
        }
        // ------------------------------------------------------------
        // ACCOUNTS FOR SALES - DTO
        // ------------------------------------------------------------
        public async Task<List<AccountLedgerDto>> AccountsForSaless()
        {
            string query = $@"
        SELECT
            ACCMAS.ID AS Id,
            ACCMAS.ACCNAME AS AccName,
            ACCMAS.CURFLG AS CurFlg,
            ISNULL(CUSMAS.ADDRESS, '') AS Address,
            ISNULL(CUSMAS.SHIPADDRESS, '') AS ShipAddress,
            ISNULL(CUSMAS.PHONE, '') AS Phone,
            ISNULL(CUSMAS.VATREGNO, '') AS VatRegNo,
            ISNULL(CUSMAS.SECNAME, '') AS SecName,
            ACCMAS.VATFLAG AS VatFlag,
            CUSMAS.GRPITEM AS GrpItem,
            ISNULL(CUSMAS.ITEMGRPCD, 0) AS ItemGrpCd,
            ISNULL(CUSMAS.CRPERIOD, '0') AS CrPeriod,
            CUSMAS.CRLIMITFLAG AS CrLimitFlag,
            ISNULL(CUSMAS.CRLIMIT, '0') AS CrLimit,
            ISNULL(CUSMAS.SALESMAN, '0') AS Salesman,
            ISNULL(CUSMAS.COUNTRYCODE, '0') AS CountryCode,
            ISNULL(ACCMAS.ALIAS, '') AS Alias,
            ACCMAS.ACCODE AS AcCode,
            ACCMAS.PRINTNAME AS PrintName
        FROM ACCMAS
        LEFT JOIN CUSMAS ON ACCMAS.ID = CUSMAS.ACCID
        WHERE
            (
                ACCMAS.MGROUP IN (26, 4)
                OR ACCMAS.MGROUP IN (
                    SELECT GROUPID
                    FROM GRPLDG
                    WHERE MGROUP = 26
                )
            )
            AND ACCMAS.ACTIVE = 1
            AND (ACCMAS.ALIAS = '' OR ACCMAS.ALIAS IS NULL)
            {Functions.VRSYS_ALLOWBR(true, "ACCMAS", true)}

        UNION ALL

        SELECT
            ACCMAS.ID AS Id,
            ACCMAS.ACCNAME AS AccName,
            ACCMAS.CURFLG AS CurFlg,
            ISNULL(CUSMAS.ADDRESS, '') AS Address,
            ISNULL(CUSMAS.SHIPADDRESS, '') AS ShipAddress,
            ISNULL(CUSMAS.PHONE, '') AS Phone,
            ISNULL(CUSMAS.VATREGNO, '') AS VatRegNo,
            ISNULL(CUSMAS.SECNAME, '') AS SecName,
            ACCMAS.VATFLAG AS VatFlag,
            CUSMAS.GRPITEM AS GrpItem,
            ISNULL(CUSMAS.ITEMGRPCD, 0) AS ItemGrpCd,
            ISNULL(CUSMAS.CRPERIOD, '0') AS CrPeriod,
            CUSMAS.CRLIMITFLAG AS CrLimitFlag,
            ISNULL(CUSMAS.CRLIMIT, '0') AS CrLimit,
            ISNULL(CUSMAS.SALESMAN, '0') AS Salesman,
            ISNULL(CUSMAS.COUNTRYCODE, '0') AS CountryCode,
            ISNULL(ACCMAS.ALIAS, '') AS Alias,
            ACCMAS.ACCODE AS AcCode,
            ACCMAS.PRINTNAME AS PrintName
        FROM ACCMAS
        LEFT JOIN CUSMAS ON ACCMAS.ID = CUSMAS.ACCID
        WHERE
            (
                ACCMAS.MGROUP IN (26, 4)
                OR ACCMAS.MGROUP IN (
                    SELECT GROUPID
                    FROM GRPLDG
                    WHERE MGROUP = 26
                )
            )
            AND ACCMAS.ACTIVE = 1
            AND ACCMAS.ALIAS <> ''
            AND ACCMAS.ALIAS IS NOT NULL
            {Functions.VRSYS_ALLOWBR(true, "ACCMAS", true)}
        ORDER BY AccName;";

            return await db.QueryListAsync<AccountLedgerDto>(query);
        }

        // ------------------------------------------------------------
        // ACCOUNTS FOR PURCHASE - DTO
        // ------------------------------------------------------------
        public async Task<List<AccountLedgerDto>> AccountsForPurchases()
        {
            string query = $@"
        SELECT
            ACCMAS.ID AS Id,
            ACCMAS.ACCNAME AS AccName,
            ACCMAS.CURFLG AS CurFlg,
            ISNULL(SUPMAS.ADDRESS, '') AS Address,
            ISNULL(SUPMAS.SHIPADDRESS, '') AS ShipAddress,
            ISNULL(SUPMAS.PHONE, '') AS Phone,
            ISNULL(SUPMAS.VATREGNO, '') AS VatRegNo,
            ISNULL(SUPMAS.SECNAME, '') AS SecName,
            ACCMAS.VATFLAG AS VatFlag,
            SUPMAS.GRPITEM AS GrpItem,
            ISNULL(SUPMAS.ITEMGRPCD, 0) AS ItemGrpCd,
            ISNULL(SUPMAS.CRPERIOD, '0') AS CrPeriod,
            SUPMAS.CRLIMITFLAG AS CrLimitFlag,
            ISNULL(SUPMAS.CRLIMIT, '0') AS CrLimit,
            ACCMAS.OPENBAL AS OpenBal,
            ISNULL(SUPMAS.COUNTRYCODE, '0') AS CountryCode,
            SUPMAS.GCCFLG AS GccFlg,
            ISNULL(ACCMAS.ALIAS, '') AS Alias,
            ACCMAS.ACCODE AS AcCode
        FROM ACCMAS
        LEFT JOIN SUPMAS ON ACCMAS.ID = SUPMAS.ACCID
        WHERE
            (
                ACCMAS.MGROUP IN (27, 4)
                OR ACCMAS.MGROUP IN (
                    SELECT GROUPID
                    FROM GRPLDG
                    WHERE MGROUP = 27
                )
            )
            AND ACCMAS.ACTIVE = 1
            AND (ACCMAS.ALIAS = '' OR ACCMAS.ALIAS IS NULL)
            {Functions.VRSYS_ALLOWBR(true, "ACCMAS", true)}

        UNION ALL

        SELECT
            ACCMAS.ID AS Id,
            ACCMAS.ACCNAME AS AccName,
            ACCMAS.CURFLG AS CurFlg,
            ISNULL(SUPMAS.ADDRESS, '') AS Address,
            ISNULL(SUPMAS.SHIPADDRESS, '') AS ShipAddress,
            ISNULL(SUPMAS.PHONE, '') AS Phone,
            ISNULL(SUPMAS.VATREGNO, '') AS VatRegNo,
            ISNULL(SUPMAS.SECNAME, '') AS SecName,
            ACCMAS.VATFLAG AS VatFlag,
            SUPMAS.GRPITEM AS GrpItem,
            ISNULL(SUPMAS.ITEMGRPCD, 0) AS ItemGrpCd,
            ISNULL(SUPMAS.CRPERIOD, '0') AS CrPeriod,
            SUPMAS.CRLIMITFLAG AS CrLimitFlag,
            ISNULL(SUPMAS.CRLIMIT, '0') AS CrLimit,
            ACCMAS.OPENBAL AS OpenBal,
            ISNULL(SUPMAS.COUNTRYCODE, '0') AS CountryCode,
            SUPMAS.GCCFLG AS GccFlg,
            ISNULL(ACCMAS.ALIAS, '') AS Alias,
            ACCMAS.ACCODE AS AcCode
        FROM ACCMAS
        LEFT JOIN SUPMAS ON ACCMAS.ID = SUPMAS.ACCID
        WHERE
            (
                ACCMAS.MGROUP IN (27, 4)
                OR ACCMAS.MGROUP IN (
                    SELECT GROUPID
                    FROM GRPLDG
                    WHERE MGROUP = 27
                )
            )
            AND ACCMAS.ACTIVE = 1
            AND ACCMAS.ALIAS <> ''
            AND ACCMAS.ALIAS IS NOT NULL
            {Functions.VRSYS_ALLOWBR(true, "ACCMAS", true)}
        ORDER BY AccName;";

            return await db.QueryListAsync<AccountLedgerDto>(query);
        }


        // ------------------------------------------------------------
        // CHECK ACCOUNT NAME - CREATE
        // ------------------------------------------------------------
        public async Task<List<AccountLedgerDto>> GetAllByName(
            ClsAccMasT clsAccMas)
        {
            const string query = @"
        SELECT ID AS Id, ACCNAME AS AccName
        FROM ACCMAS
        WHERE ACCNAME = @AccName
          AND ISNULL(DELFLG, 0) = 0;";

            return await db.QueryListAsync<AccountLedgerDto>(
                query,
                new { clsAccMas.AccName });
        }

        // ------------------------------------------------------------
        // CHECK ACCOUNT NAME - UPDATE
        // ------------------------------------------------------------
        public async Task<List<AccountLedgerDto>> CheckNameExists(
            ClsAccMasT clsAccMas)
        {
            const string query = @"
        SELECT ID AS Id, ACCNAME AS AccName
        FROM ACCMAS
        WHERE ACCNAME = @AccName
          AND ID <> @Id
          AND ISNULL(DELFLG, 0) = 0;";

            return await db.QueryListAsync<AccountLedgerDto>(
                query,
                new
                {
                    clsAccMas.AccName,
                    clsAccMas.Id
                });
        }

        // ------------------------------------------------------------
        // CHECK ACCOUNT CODE - CREATE
        // ------------------------------------------------------------
        public async Task<List<AccountLedgerDto>> GetAllByCode(
            ClsAccMasT clsAccMas)
        {
            const string query = @"
            SELECT ID AS Id, ACCODE AS AcCode
            FROM ACCMAS
            WHERE ACCODE = @AcCode
              AND ISNULL(DELFLG, 0) = 0;";

            return await db.QueryListAsync<AccountLedgerDto>(
                query,
                new { clsAccMas.AcCode });
        }

        // ------------------------------------------------------------
        // CHECK ACCOUNT CODE - UPDATE
        // ------------------------------------------------------------
        public async Task<List<AccountLedgerDto>> CheckCodeExists(
            ClsAccMasT clsAccMas)
        {
            const string query = @"
        SELECT ID AS Id, ACCODE AS AcCode
        FROM ACCMAS
        WHERE ACCODE = @AcCode
          AND ID <> @Id
          AND ISNULL(DELFLG, 0) = 0;";

            return await db.QueryListAsync<AccountLedgerDto>(
                query,
                new
                {
                    clsAccMas.AcCode,
                    clsAccMas.Id
                });
        }

        // ------------------------------------------------------------
        // GET NEXT ACCOUNT ID
        // ------------------------------------------------------------
        public async Task<int> MaxId()
        {
            const string query = @"
        SELECT ISNULL(MAX(ID), 0) + 1
        FROM ACCMAS;";

            return await db.QueryFirstOrDefaultAsync<int>(query);
        }

        // ------------------------------------------------------------
        // GET MAX ACCOUNT CODE FOR GROUP
        // ------------------------------------------------------------
        //public async Task<int> GetMaxCode(int len, ClsAccMasT clsAccMas)
        //{
        //    const string query = @"
        //SELECT MAX(
        //    TRY_CAST(
        //        SUBSTRING(
        //            ACCODE,
        //            CHARINDEX('-', ACCODE) + 1,
        //            LEN(ACCODE) - CHARINDEX('-', ACCODE)
        //        ) AS INT
        //    )
        //)
        //FROM ACCMAS
        //WHERE MGROUP = @GroupId
        //  AND ACCODE IS NOT NULL
        //  AND CHARINDEX('-', ACCCODE) > 0;";

        //    try
        //    {
        //        int? result = await db.QueryFirstOrDefaultAsync<int?>(
        //            query,
        //            new { GroupId = clsAccMas.LedgerGroupId });

        //        return result ?? 0;
        //    }
        //    catch
        //    {
        //        return 0;
        //    }
        //}



        public async Task<int> GetMaxCode(string groupCode)
        {
            const string query = @"
        SELECT ISNULL(
            MAX(
                TRY_CAST(
                    SUBSTRING(
                        ACCODE,
                        CHARINDEX('-', ACCODE) + 1,
                        LEN(ACCODE)
                    ) AS INT
                )
            ), 0
        )
        FROM ACCMAS
        WHERE ACCODE LIKE @CodePrefix
          AND CHARINDEX('-', ACCODE) > 0
          AND ISNULL(DELFLG, 0) = 0;";

            return await db.QueryFirstOrDefaultAsync<int>(
                query,
                new { CodePrefix = groupCode + "-%" }); 
        }


        // ------------------------------------------------------------
        // INSERT ACCOUNT
        // ------------------------------------------------------------
        public async Task<int> Insert(ClsAccMasT clsAccMas)
        {
            const string query = @"
        INSERT INTO ACCMAS
        (
            ALIAS, ACCNAME, SECNAME, ADDRESS, PHONE,
            ACTIVE, LOGCD, UITIME, ACCODE, PRINTNAME,
            MGROUP, DEPRECIATION, VATFLAG, VATPERC,
            TAXRETFLAG, BANKACCNAME, BANKACCNO, IBANNO,
            SWIFTCODE, BRANCH, OPENBAL, CURCONVRATE,
            CURCODE, CURFLG, APRVLST, APRVLABY,
            APRVLBBY, APRVLRDT, DELFLG
        )
        OUTPUT INSERTED.ID
        VALUES
        (
            @Alias, @AccName, @SecName, @Address, @Phone,
            @Active, @LogCd, GETDATE(), @AcCode, @PrintName,
            @LedgerGroupId, @Depreciation, @VatFlag, @VatPerc,
            @TaxRetFlag, @BankAccName, @BankAccno, @IbanNo,
            @SwiftCode, @Branch, @OpenBal, @CurConvRate,
            @CurCode, @CurFlg, @APRVLST, @APRVLABY,
            @APRVLBBY, GETDATE(), 0
        );";

            int? id = await db.QueryFirstOrDefaultAsync<int>(
                query,
                new
                {
                    clsAccMas.Alias,
                    clsAccMas.AccName,
                    clsAccMas.SecName,
                    clsAccMas.Address,
                    clsAccMas.Phone,
                    Active = clsAccMas.Active,
                    clsAccMas.LogCd,
                    clsAccMas.AcCode,
                    clsAccMas.PrintName,
                    clsAccMas.LedgerGroupId,
                    clsAccMas.Depreciation,
                    clsAccMas.VatFlag,
                    clsAccMas.VatPerc,
                    clsAccMas.TaxRetFlag,
                    clsAccMas.BankAccName,
                    clsAccMas.BankAccno,
                    clsAccMas.IbanNo,
                    clsAccMas.SwiftCode,
                    clsAccMas.Branch,
                    clsAccMas.OpenBal,
                    clsAccMas.CurConvRate,
                    clsAccMas.CurCode,
                    clsAccMas.CurFlg,
                    clsAccMas.APRVLST,
                    clsAccMas.APRVLABY,
                    clsAccMas.APRVLBBY
                });

            return id ?? 0;
        }

        // ------------------------------------------------------------
        // UPDATE ACCOUNT
        // ------------------------------------------------------------
        public async Task<int> Update(ClsAccMasT clsAccMas, byte[] rowVersion)
        {
            const string query = @"
        UPDATE ACCMAS
        SET
            ALIAS = @Alias,
            ACCNAME = @AccName,
            SECNAME = @SecName,
            ADDRESS = @Address,
            PHONE = @Phone,
            ACTIVE = @Active,
            LOGCD = @LogCd,
            UITIME = GETDATE(),
            ACCODE = @AcCode,
            PRINTNAME = @PrintName,
            MGROUP = @LedgerGroupId,
            DEPRECIATION = @Depreciation,
            VATFLAG = @VatFlag,
            VATPERC = @VatPerc,
            TAXRETFLAG = @TaxRetFlag,
            BANKACCNAME = @BankAccName,
            BANKACCNO = @BankAccno,
            IBANNO = @IbanNo,
            SWIFTCODE = @SwiftCode,
            BRANCH = @Branch,
            OPENBAL = @OpenBal,
            CURCONVRATE = @CurConvRate,
            CURCODE = @CurCode,
            CURFLG = @CurFlg
        WHERE ID = @Id  AND RowVersion = @RowVersion
          AND ISNULL(DELFLG, 0) = 0;";

            return await db.ExecuteAsync(
                query,
                new
                {
                    clsAccMas.Alias,
                    clsAccMas.AccName,
                    clsAccMas.SecName,
                    clsAccMas.Address,
                    clsAccMas.Phone,
                    Active = clsAccMas.Active,
                    clsAccMas.LogCd,
                    clsAccMas.AcCode,
                    clsAccMas.PrintName,
                    clsAccMas.LedgerGroupId,
                    clsAccMas.Depreciation,
                    clsAccMas.VatFlag,
                    clsAccMas.VatPerc,
                    clsAccMas.TaxRetFlag,
                    clsAccMas.BankAccName,
                    clsAccMas.BankAccno,
                    clsAccMas.IbanNo,
                    clsAccMas.SwiftCode,
                    clsAccMas.Branch,
                    clsAccMas.OpenBal,
                    clsAccMas.CurConvRate,
                    clsAccMas.CurCode,
                    clsAccMas.CurFlg,
                    clsAccMas.Id,
                    RowVersion = rowVersion
                });
        }

        // ------------------------------------------------------------
        // SOFT DELETE ACCOUNT
        // ------------------------------------------------------------
        public async Task<int> UpdateDelFlg(ClsAccMasT clsAccMas)
        {
            const string query = @"
    UPDATE ACCMAS
    SET
        DELFLG = 1,
        ACTIVE = 0,
        LOGCD = @LogCd,
        UITIME = GETDATE()
    WHERE ID = @Id
      AND ISNULL(DELFLG, 0) = 0;";

            return await db.ExecuteAsync(
                query,
                new
                {
                    clsAccMas.LogCd,
                    clsAccMas.Id
                });
        }
    }
}