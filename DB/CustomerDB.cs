

using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Respons_DTO;
using System.Data;

namespace QuickBalance.API.DB
{
    public class CustomerDB
    {
        private readonly DBConn db;

        public CustomerDB(DBConn db)
        {
            this.db = db;
        }

        private static readonly (string Property, string Column)[] Fields =
        {
            (nameof(clsCustomer.Alias), "ALIAS"),
            (nameof(clsCustomer.CusName), "CUSNAME"),
            (nameof(clsCustomer.Address), "ADDRESS"),
            (nameof(clsCustomer.Phone), "PHONE"),
            (nameof(clsCustomer.Email), "EMAIL"),
            (nameof(clsCustomer.Active), "ACTIVE"),
            //(nameof(clsCustomer.LogCd), "LOGCD"),
            (nameof(clsCustomer.AccountId), "ACCID"),
            (nameof(clsCustomer.Disc), "DISC"),
            (nameof(clsCustomer.StateCode), "STATECODE"),
            (nameof(clsCustomer.PrintName), "PRINTNAME"),
            (nameof(clsCustomer.CountryCode), "COUNTRYCODE"),
            (nameof(clsCustomer.PriceLevel), "PRICELEVEL"),
            (nameof(clsCustomer.BnkAccName), "BNKACCNAME"),
            (nameof(clsCustomer.BnkAccNo), "BNKACCNO"),
            (nameof(clsCustomer.Branch), "BRANCH"),
            (nameof(clsCustomer.IbanNo), "IBANNO"),
            (nameof(clsCustomer.VatFlag), "VATFLAG"),
            (nameof(clsCustomer.BillFlag), "BILLFLAG"),
            (nameof(clsCustomer.CrPerFlag), "CRPERFLAG"),
            (nameof(clsCustomer.CrLimitFlag), "CRLIMITFLAG"),
            (nameof(clsCustomer.CrLimit), "CRLIMIT"),
            //(nameof(clsCustomer.UiTime), "UITIME"),
            (nameof(clsCustomer.VatRegNo), "VATREGNO"),
            (nameof(clsCustomer.ShipAddress), "SHIPADDRESS"),
            (nameof(clsCustomer.SecName), "SECNAME"),
            (nameof(clsCustomer.CurFlg), "CURFLG"),
            (nameof(clsCustomer.SalesManId), "SALESMAN"),
            (nameof(clsCustomer.CrPeriod), "CRPERIOD"),
            (nameof(clsCustomer.GrpItem), "GRPITEM"),
            (nameof(clsCustomer.ItemGroupId), "ITEMGRPCD"),
            (nameof(clsCustomer.GccFlg), "GCCFLG"),
            //(nameof(clsCustomer.PrTmpCode), "PRTMPCODE"),
            (nameof(clsCustomer.PntSysId), "PNTSYSID"),
            (nameof(clsCustomer.StreetOne), "STREETONE"),
            (nameof(clsCustomer.StreetTwo), "STREETTWO"),
            (nameof(clsCustomer.BuildingNo), "BULDINGNO"),
            (nameof(clsCustomer.PlotId), "PLOTID"),
            (nameof(clsCustomer.CityName), "CITYNAME"),
            (nameof(clsCustomer.PostalCode), "POSTALCODE"),
            (nameof(clsCustomer.District), "DISTRICT"),
            (nameof(clsCustomer.GrpVatNo), "GRPVATNO"),
            (nameof(clsCustomer.EinvIdType), "EINVIDTYPE"),
            (nameof(clsCustomer.EinvId), "EINVID"),
            (nameof(clsCustomer.VatType), "VATTYPE"),
            (nameof(clsCustomer.PrListCd), "PRLISTCD"),
            (nameof(clsCustomer.PrCategoryId), "CPRCATCD"),
            (nameof(clsCustomer.PartyGrpId), "PARTYGRPID"),
            (nameof(clsCustomer.CityNameSec), "CITYNAMESEC"),
            (nameof(clsCustomer.DistrictSec), "DISTRICTSEC"),
            (nameof(clsCustomer.AprvLst), "APRVLST")
        };
        private static void AddCustomerParameters(
        int userId,
        SqlCommand command,
        CustomerRequest customer)
        {
            command.Parameters.AddWithValue("@ALIAS", DbValue(customer.Alias));
            command.Parameters.AddWithValue("@CUSNAME", DbValue(customer.CusName));
            command.Parameters.AddWithValue("@ADDRESS", DbValue(customer.Address));
            command.Parameters.AddWithValue("@PHONE", DbValue(customer.Phone));
            command.Parameters.AddWithValue("@EMAIL", DbValue(customer.Email));
            command.Parameters.AddWithValue("@ACTIVE", customer.Active);
            command.Parameters.AddWithValue("@ACCID", 0);

            command.Parameters.AddWithValue("@DISC", 0);
            command.Parameters.AddWithValue("@STATECODE", customer.StateCode);
            command.Parameters.AddWithValue("@PRINTNAME", DbValue(customer.PrintName));
            command.Parameters.AddWithValue("@COUNTRYCODE", customer.CountryCode);
            command.Parameters.AddWithValue("@PRICELEVEL", 0);

            command.Parameters.AddWithValue("@BNKACCNAME", DbValue(customer.BnkAccName));
            command.Parameters.AddWithValue("@BNKACCNO", DbValue(customer.BnkAccNo));
            command.Parameters.AddWithValue("@BRANCH", DbValue(customer.Branch));
            command.Parameters.AddWithValue("@IBANNO", DbValue(customer.IbanNo));

            command.Parameters.AddWithValue("@VATFLAG", customer.VatFlag);
            command.Parameters.AddWithValue("@BILLFLAG", customer.BillFlag);
            command.Parameters.AddWithValue("@CRPERFLAG", customer.CrPerFlag);
            command.Parameters.AddWithValue("@CRLIMITFLAG", customer.CrLimitFlag);
            command.Parameters.AddWithValue("@CRLIMIT", customer.CrLimit);

            command.Parameters.AddWithValue("@UITIME", DateTime.Now);
            command.Parameters.AddWithValue("@VATREGNO", DbValue(customer.VatRegNo));
            command.Parameters.AddWithValue("@SHIPADDRESS", DbValue(customer.ShipAddress));
            command.Parameters.AddWithValue("@SECNAME", DbValue(customer.SecName));

            command.Parameters.AddWithValue("@CURFLG", customer.CurFlg);
            //command.Parameters.AddWithValue("@SALESMAN",customer.SalesMan);
            command.Parameters.Add("@SALESMAN", SqlDbType.Int).Value =(object?)customer.SalesMan ?? DBNull.Value;
            command.Parameters.AddWithValue("@CRPERIOD", customer.CrPeriod);

            command.Parameters.AddWithValue("@GRPITEM", false);
            command.Parameters.AddWithValue("@ITEMGRPCD", 0);
            command.Parameters.AddWithValue("@GCCFLG", customer.GccFlg);

            command.Parameters.AddWithValue("@PRTMPCODE", DBNull.Value);
            command.Parameters.AddWithValue("@PNTSYSID", DBNull.Value);

            command.Parameters.AddWithValue("@STREETONE", DbValue(customer.StreetOne));
            command.Parameters.AddWithValue("@STREETTWO", DbValue(customer.StreetTwo));
            command.Parameters.AddWithValue("@BULDINGNO", DbValue(customer.BuildingNo));
            command.Parameters.AddWithValue("@PLOTID", DbValue(customer.PlotId));
            command.Parameters.AddWithValue("@CITYNAME", DbValue(customer.CityName));
            command.Parameters.AddWithValue("@POSTALCODE", DbValue(customer.PostalCode));
            command.Parameters.AddWithValue("@DISTRICT", DbValue(customer.District));

            command.Parameters.AddWithValue("@GRPVATNO", DbValue(customer.GrpVatNo));
            command.Parameters.AddWithValue("@EINVIDTYPE", DbValue(customer.EinvIdType));
            command.Parameters.AddWithValue("@EINVID", DbValue(customer.EinvId));
            command.Parameters.AddWithValue("@VATTYPE", DbValue(customer.VatType));

            command.Parameters.AddWithValue("@PRLISTCD", DBNull.Value);
            command.Parameters.AddWithValue("@CPRCATCD", DBNull.Value);

            command.Parameters.AddWithValue("@PARTYGRPID", customer.PartyGrpId);
            command.Parameters.AddWithValue("@CITYNAMESEC", DBNull.Value);
            command.Parameters.AddWithValue("@DISTRICTSEC", DBNull.Value);
            command.Parameters.AddWithValue("@APRVLST", DBNull.Value);

            command.Parameters.Add("@LOGCD", SqlDbType.VarChar, 50)
                .Value = userId.ToString();
        }
        private static object DbValue(object? value)
        {
            return value ?? DBNull.Value;
        }

   
        // =========================================================
        // GET CUSTOMERS WITH PAGINATION AND SEARCH
        // =========================================================


        public async Task<(List<clsCustomer> Data, int TotalRecords)> GetCustomers(
        int page,
        int pageSize,
        string? search,
        bool? active = null)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 500);

            var searchValue = search?.Trim() ?? string.Empty;

            // =========================================================
            // 1. COUNT CUSTOMERS
            // =========================================================

            const string countSql = """
                SELECT COUNT(1)
                FROM dbo.CUSMAS C
                WHERE ISNULL(C.DELFLG, 0) = 0
                  AND (@Active IS NULL OR ISNULL(C.ACTIVE, 0) = @Active)
                  AND (
                        @Search = ''
                        OR C.CUSNAME LIKE '%' + @Search + '%'
                        OR C.ALIAS LIKE '%' + @Search + '%'
                        OR C.PRINTNAME LIKE '%' + @Search + '%'
                        OR C.PHONE LIKE '%' + @Search + '%'
                        OR C.EMAIL LIKE '%' + @Search + '%'
                  );
                """;

            int totalRecords = await db.QueryFirstOrDefaultAsync<int>(
                countSql,
                new { Search = searchValue, Active = active });

            if (totalRecords == 0)
                return (new List<clsCustomer>(), 0);


            // =========================================================
            // 2. GET PAGINATED CUSTOMERS
            // =========================================================

            const string customerSql = """
            SELECT
                C.ID AS Id,
                C.ALIAS AS Alias,
                C.CUSNAME AS CusName,
                C.ADDRESS AS Address,
                C.PHONE AS Phone,
                C.EMAIL AS Email,
                C.ACTIVE AS Active,
                C.LOGCD AS LogId,
                C.ACCID AS AccountId,

                C.PRINTNAME AS PrintName,
                C.DISC AS Disc,
                C.STATECODE AS StateCode,
                C.COUNTRYCODE AS CountryCode,
                C.PRICELEVEL AS PriceLevel,

                C.BNKACCNAME AS BnkAccName,
                C.BNKACCNO AS BnkAccNo,
                C.BRANCH AS Branch,
                C.IBANNO AS IbanNo,

                C.VATFLAG AS VatFlag,
                C.BILLFLAG AS BillFlag,
                C.CRPERFLAG AS CrPerFlag,
                C.CRLIMITFLAG AS CrLimitFlag,
                C.CRLIMIT AS CrLimit,

                C.VATREGNO AS VatRegNo,
                C.SHIPADDRESS AS ShipAddress,
                C.SECNAME AS SecName,
                C.CURFLG AS CurFlg,

                C.SALESMAN AS SalesManId,
                C.CRPERIOD AS CrPeriod,
                C.GRPITEM AS GrpItem,
                C.ITEMGRPCD AS ItemGroupId,
                C.GCCFLG AS GccFlg,

                C.PRTMPCODE AS PrTmplateId,
                C.PNTSYSID AS PntSysId,

                C.STREETONE AS StreetOne,
                C.STREETTWO AS StreetTwo,
                C.BULDINGNO AS BuildingNo,
                C.PLOTID AS PlotId,
                C.CITYNAME AS CityName,
                C.POSTALCODE AS PostalCode,
                C.DISTRICT AS District,

                C.GRPVATNO AS GrpVatNo,
                C.EINVIDTYPE AS EinvIdType,
                C.EINVID AS EinvId,
                C.VATTYPE AS VatType,

                C.PRLISTCD AS PrListCd,
                C.CPRCATCD AS PrCategoryId,
                C.PARTYGRPID AS PartyGrpId,

                C.CITYNAMESEC AS CityNameSec,
                C.DISTRICTSEC AS DistrictSec,
                C.APRVLST AS AprvLst,

                C.ROWVERSION AS RowVersion

            FROM dbo.CUSMAS C

            WHERE ISNULL(C.DELFLG, 0) = 0
              AND (@Active IS NULL OR ISNULL(C.ACTIVE, 0) = @Active)
              AND (
                    @Search = ''
                    OR C.CUSNAME LIKE '%' + @Search + '%'
                    OR C.ALIAS LIKE '%' + @Search + '%'
                    OR C.PRINTNAME LIKE '%' + @Search + '%'
                    OR C.PHONE LIKE '%' + @Search + '%'
                    OR C.EMAIL LIKE '%' + @Search + '%'
              )

            ORDER BY C.CUSNAME, C.ID

            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;
            """;

            var customers = await db.QueryListAsync<clsCustomer>(
                customerSql,
                new
                {
                    Search = searchValue,
                    Active = active,
                    Offset = (page - 1) * pageSize,
                    PageSize = pageSize
                });

            var result = customers.ToList();

            if (result.Count == 0)
                return (result, totalRecords);


            // =========================================================
            // 3. COLLECT LINKED ACCOUNT IDS
            // =========================================================

            var accountIds = result
                .Where(c => c.AccountId > 0)
                .Select(c => c.AccountId)
                .Distinct()
                .ToArray();

            if (accountIds.Length == 0)
                return (result, totalRecords);

            var customerByAccount = result
                .Where(c => c.AccountId > 0)
                .GroupBy(c => c.AccountId)
                .ToDictionary(g => g.Key, g => g.First());


            // =========================================================
            // 4. GET OPENING BALANCES
            // =========================================================

            const string openingSql = """
            SELECT
                ACCID AS AccountId,
                SUM(ISNULL(DEBIT, 0)) AS TotalDebit,
                SUM(ISNULL(CREDIT, 0)) AS TotalCredit,
                MAX(CURCODE) AS CurrencyCode,
                MAX(CURCONVRATE) AS ConversionRate
            FROM dbo.OPN
            WHERE ACCID IN @AccountIds
            GROUP BY ACCID;
            """;

            var openingBalances = await db.QueryListAsync<CustomerOpeningBalanceResult>(
                openingSql,
                new { AccountIds = accountIds });

            foreach (var opening in openingBalances)
            {
                if (!customerByAccount.TryGetValue(
                        opening.AccountId,
                        out var customer))
                    continue;

                customer.OpenBal = Math.Abs(
                    opening.TotalDebit - opening.TotalCredit);

                customer.CrDr = opening.TotalDebit > opening.TotalCredit
                    ? "DR"
                    : opening.TotalCredit > opening.TotalDebit
                        ? "CR"
                        : "DR";

                customer.CurCode = opening.CurrencyCode ?? 0;

                customer.CurConvRate = opening.ConversionRate ?? 1m;
            }


            // =========================================================
            // 5. GET PENDING BILLS
            // =========================================================

            const string pendingBillsSql = """
            SELECT
                ID AS Id,
                CODE AS AccountId,
                INVDATE AS InvoiceDate,
                NARRATION AS Narration,
                INVNO AS InvoiceNumber,
                AMOUNT AS Amount,
                DUEDATE AS DueDate,
                DRCR AS DrCr,
                CURCODE AS CurrencyCode,
                CURCONVRATE AS ConversionRate,
                BRCD AS BranchId
            FROM dbo.PENDBIL
            WHERE CODE IN @AccountIds
            ORDER BY CODE, INVDATE, ID;
            """;

            var pendingBills = await db.QueryListAsync<CustomerPendingBillResult>(
                pendingBillsSql,
                new { AccountIds = accountIds });

            foreach (var bill in pendingBills)
            {
                if (!customerByAccount.TryGetValue(
                        bill.AccountId,
                        out var customer))
                    continue;

                customer.PendingBills.Add(new CustomerPendingBillDto
                {
                    Id = bill.Id,
                    InvoiceDate = bill.InvoiceDate,
                    Narration = bill.Narration,
                    InvoiceNumber = bill.InvoiceNumber,
                    Amount = bill.Amount,
                    DueDate = bill.DueDate,
                    DrCr = bill.DrCr,
                    CurrencyCode = bill.CurrencyCode,
                    ConversionRate = bill.ConversionRate,
                    BranchId = bill.BranchId
                });
            }


            // =========================================================
            // 6. GET LEDGER GROUP, ACCOUNT CODE AND SWIFT CODE
            // =========================================================

            const string ledgerSql = """
                SELECT
                    ID AS AccountId,
                    MGROUP AS LedgerGroupId,
                    ACCODE AS AccountCode,
                    SWIFTCODE AS SwiftCode
                FROM dbo.ACCMAS
                WHERE ACTIVE = 1
                  AND ID IN @AccountIds;
                """;

            var ledgerDetails = await db.QueryListAsync<CustomerLedgerResult>(
                ledgerSql,
                new { AccountIds = accountIds });

            foreach (var ledger in ledgerDetails)
            {
                if (!customerByAccount.TryGetValue(
                        ledger.AccountId,
                        out var customer))
                    continue;

                customer.LedgerGroupId = ledger.LedgerGroupId ?? 0;
                customer.AccountCode = ledger.AccountCode;
                customer.SwiftCode = ledger.SwiftCode;
            }

            return (result, totalRecords);
        }


        public async Task<clsCustomer?> GetCustomerById(int id)
        {
            // =========================================================
            // 1. GET CUSTOMER MASTER
            // =========================================================

            const string customerSql = """
            SELECT
                C.ID AS Id,
                C.ALIAS AS Alias,
                C.CUSNAME AS CusName,
                C.ADDRESS AS Address,
                C.PHONE AS Phone,
                C.EMAIL AS Email,
                C.ACTIVE AS Active,
                C.LOGCD AS LogId,
                C.ACCID AS AccountId,

                C.PRINTNAME AS PrintName,
                TRY_CONVERT(decimal(18,4), C.DISC) AS Disc,
                C.STATECODE AS StateCode,
                C.COUNTRYCODE AS CountryCode,
                C.PRICELEVEL AS PriceLevel,

                C.BNKACCNAME AS BnkAccName,
                C.BNKACCNO AS BnkAccNo,
                C.BRANCH AS Branch,
                C.IBANNO AS IbanNo,

                C.VATFLAG AS VatFlag,
                C.BILLFLAG AS BillFlag,
                C.CRPERFLAG AS CrPerFlag,
                C.CRLIMITFLAG AS CrLimitFlag,
                C.CRLIMIT AS CrLimit,

                C.VATREGNO AS VatRegNo,
                C.SHIPADDRESS AS ShipAddress,
                C.SECNAME AS SecName,
                C.CURFLG AS CurFlg,

                C.SALESMAN AS SalesManId,
                C.CRPERIOD AS CrPeriod,
                C.GRPITEM AS GrpItem,
                C.ITEMGRPCD AS ItemGroupId,
                C.GCCFLG AS GccFlg,

                C.PRTMPCODE AS PrTmplateId,
                C.PNTSYSID AS PntSysId,

                C.STREETONE AS StreetOne,
                C.STREETTWO AS StreetTwo,
                C.BULDINGNO AS BuildingNo,
                C.PLOTID AS PlotId,
                C.CITYNAME AS CityName,
                C.POSTALCODE AS PostalCode,
                C.DISTRICT AS District,

                C.GRPVATNO AS GrpVatNo,
                C.EINVIDTYPE AS EinvIdType,
                C.EINVID AS EinvId,
                C.VATTYPE AS VatType,

                C.PRLISTCD AS PrListCd,
                C.CPRCATCD AS PrCategoryId,
                C.PARTYGRPID AS PartyGrpId,

                C.CITYNAMESEC AS CityNameSec,
                C.DISTRICTSEC AS DistrictSec,
                C.APRVLST AS AprvLst,

                C.ROWVERSION AS RowVersion

            FROM dbo.CUSMAS C
            WHERE C.ID = @Id
              AND ISNULL(C.DELFLG, 0) = 0;
            """;

            var customer = await db.QueryFirstOrDefaultAsync<clsCustomer>(
                customerSql,
                new { Id = id });

            if (customer == null)
                return null;


            // =========================================================
            // 2. INITIALIZE ADDITIONAL PROPERTIES
            // =========================================================

            customer.OpenBal = 0m;
            customer.CrDr = "DR";
            customer.CurCode = 0;
            customer.CurConvRate = 1m;
            customer.LedgerGroupId = 0;
            customer.AccountCode = null;
            customer.SwiftCode = null;
            customer.PendingBills = new List<CustomerPendingBillDto>();

            int accountId = customer.AccountId;

            if (accountId <= 0)
                return customer;


            // =========================================================
            // 3. GET LEDGER DETAILS FROM ACCMAS
            // =========================================================

            const string accountSql = """
            SELECT
                MGROUP AS LedgerGroupId,
                CURCODE AS CurrencyId,
                CURCONVRATE AS ConversionRate,
                ACCODE AS AccountId,
                SWIFTCODE AS SwiftCode
            FROM dbo.ACCMAS
            WHERE ID = @AccountId
              AND ISNULL(DELFLG, 0) = 0;
            """;

            var account = await db.QueryFirstOrDefaultAsync<CustomerAccountResult>(
                accountSql,
                new { AccountId = accountId });

            if (account != null)
            {
                customer.LedgerGroupId = account.LedgerGroupId ?? 0;
                customer.CurCode = account.CurrencyId ?? 0;
                customer.CurConvRate = account.ConversionRate ?? 1m;
                customer.AccountCode = account.AccountId;
                customer.SwiftCode = account.SwiftCode;
            }


            // =========================================================
            // 4. GET OPENING BALANCE FROM OPN
            // =========================================================

            const string openingSql = """
            SELECT
                SUM(ISNULL(DEBIT, 0)) AS TotalDebit,
                SUM(ISNULL(CREDIT, 0)) AS TotalCredit
            FROM dbo.OPN
            WHERE ACCID = @AccountId
            HAVING COUNT(*) > 0;
            """;

            var opening = await db.QueryFirstOrDefaultAsync<CustomerOpeningBalanceResult>(
                openingSql,
                new { AccountId = accountId });

            if (opening != null)
            {
                decimal debit = opening.TotalDebit;
                decimal credit = opening.TotalCredit;

                customer.OpenBal = Math.Abs(debit - credit);

                customer.CrDr = debit > credit
                    ? "DR"
                    : credit > debit
                        ? "CR"
                        : "DR";
            }


            // =========================================================
            // 5. GET PENDING BILLS
            // =========================================================

            const string pendingBillsSql = """
            SELECT
                ID AS Id,
                INVDATE AS InvoiceDate,
                NARRATION AS Narration,
                INVNO AS InvoiceNumber,
                AMOUNT AS Amount,
                DUEDATE AS DueDate,
                DRCR AS DrCr,
                CURCODE AS CurrencyId,
                CURCONVRATE AS ConversionRate,
                BRCD AS BranchId
            FROM dbo.PENDBIL
            WHERE CODE = @AccountId
            ORDER BY INVDATE, ID;
            """;

            var pendingBills = await db.QueryListAsync<CustomerPendingBillDto>(
                pendingBillsSql,
                new { AccountId = accountId });

            customer.PendingBills = pendingBills.ToList();

            return customer;
        }

        //public async Task<clsCustomer?> GetCustomerById(int id)
        //{
        //    const string customerSql = @"
        //SELECT *
        //FROM dbo.CUSMAS
        //WHERE  ACTIVE=1 AND ID = @Id
        //  AND ISNULL(DELFLG, 0) = 0;";

        //    using var connection = db.GetConnection();

        //    if (connection.State != ConnectionState.Open)
        //        await connection.OpenAsync();

        //    clsCustomer? customer;

        //    // 1. Get customer master.
        //    using (var command = new SqlCommand(customerSql, connection))
        //    {
        //        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        //        using var reader = await command.ExecuteReaderAsync();

        //        customer = await reader.ReadAsync()
        //            ? MapCustomer(reader)
        //            : null;
        //    }

        //    if (customer == null)
        //        return null;

        //    // 2. Initialize additional properties.
        //    customer.OpenBal = 0m;
        //    customer.CrDr = "DR";
        //    customer.CurCode = 0;
        //    customer.CurConvRate = 1m;
        //    customer.LedgerGroupId = 0;
        //    customer.PendingBills = new List<CustomerPendingBillDto>();

        //    int accountId = customer.AccId;

        //    if (accountId <= 0)
        //        return customer;

        //    // 3. Get ledger group ID and account currency from ACCMAS.
        //    const string accountSql = @"
        //SELECT MGROUP, CURCODE, CURCONVRATE,ACCODE,SWIFTCODE
        //FROM dbo.ACCMAS
        //WHERE ACTIVE=1 AND ID = @AccountId;";

        //    using (var command = new SqlCommand(accountSql, connection))
        //    {
        //        command.Parameters.Add("@AccountId", SqlDbType.Int).Value =
        //            accountId;

        //        using var reader = await command.ExecuteReaderAsync();

        //        if (await reader.ReadAsync())
        //        {
        //            customer.LedgerGroupId =
        //                reader["MGROUP"] == DBNull.Value
        //                    ? 0
        //                    : Convert.ToInt32(reader["MGROUP"]);

        //            customer.CurCode =
        //                reader["CURCODE"] == DBNull.Value
        //                    ? 0
        //                    : Convert.ToInt32(reader["CURCODE"]);

        //            customer.CurConvRate =
        //                reader["CURCONVRATE"] == DBNull.Value
        //                    ? 1m
        //                    : Convert.ToDecimal(reader["CURCONVRATE"]);
        //            customer.AccountCode = Convert.ToString(reader["ACCODE"]);
        //            customer.SwiftCode = reader["SWIFTCODE"] == DBNull.Value
        //               ? null
        //               : Convert.ToString(reader["SWIFTCODE"]);
        //        }
        //    }

        //    // 4. Get opening balance and debit/credit direction.
        //    const string openingSql = @"
        //SELECT
        //    SUM(ISNULL(DEBIT, 0)) AS TotalDebit,
        //    SUM(ISNULL(CREDIT, 0)) AS TotalCredit
        //FROM dbo.OPN
        //WHERE ACCID = @AccountId
        //HAVING COUNT(*) > 0;";

        //    using (var command = new SqlCommand(openingSql, connection))
        //    {
        //        command.Parameters.Add("@AccountId", SqlDbType.Int).Value =
        //            accountId;

        //        using var reader = await command.ExecuteReaderAsync();

        //        if (await reader.ReadAsync())
        //        {
        //            decimal debit = reader["TotalDebit"] == DBNull.Value
        //                ? 0m
        //                : Convert.ToDecimal(reader["TotalDebit"]);

        //            decimal credit = reader["TotalCredit"] == DBNull.Value
        //                ? 0m
        //                : Convert.ToDecimal(reader["TotalCredit"]);

        //            customer.OpenBal = Math.Abs(debit - credit);

        //            customer.CrDr = debit > credit
        //                ? "DR"
        //                : credit > debit
        //                    ? "CR"
        //                    : "DR";
        //        }
        //    }

        //    // 5. Get pending bills.
        //    const string pendingBillsSql = @"
        //    SELECT
        //        ID, CODE, INVDATE, NARRATION, INVNO,
        //        AMOUNT, DUEDATE, DRCR, CURCODE,
        //        CURCONVRATE, BRCD
        //    FROM dbo.PENDBIL
        //    WHERE CODE = @AccountId
        //    ORDER BY INVDATE, ID;";

        //    using (var command = new SqlCommand(pendingBillsSql, connection))
        //    {
        //        command.Parameters.Add("@AccountId", SqlDbType.Int).Value =
        //            accountId;

        //        using var reader = await command.ExecuteReaderAsync();

        //        while (await reader.ReadAsync())
        //        {
        //            customer.PendingBills.Add(new CustomerPendingBillDto
        //            {
        //                Id = Convert.ToInt32(reader["ID"]),

        //                InvoiceDate = reader["INVDATE"] == DBNull.Value
        //                    ? null
        //                    : Convert.ToDateTime(reader["INVDATE"]),

        //                Narration = reader["NARRATION"] == DBNull.Value
        //                    ? null
        //                    : Convert.ToString(reader["NARRATION"]),

        //                InvoiceNumber = reader["INVNO"] == DBNull.Value
        //                    ? null
        //                    : Convert.ToString(reader["INVNO"]),

        //                Amount = reader["AMOUNT"] == DBNull.Value
        //                    ? 0m
        //                    : Convert.ToDecimal(reader["AMOUNT"]),

        //                DueDate = reader["DUEDATE"] == DBNull.Value
        //                    ? null
        //                    : Convert.ToDateTime(reader["DUEDATE"]),

        //                DrCr = reader["DRCR"] == DBNull.Value
        //                    ? null
        //                    : Convert.ToString(reader["DRCR"]),

        //                CurrencyCode = reader["CURCODE"] == DBNull.Value
        //                    ? null
        //                    : Convert.ToInt32(reader["CURCODE"]),

        //                ConversionRate = reader["CURCONVRATE"] == DBNull.Value
        //                    ? null
        //                    : Convert.ToDecimal(reader["CURCONVRATE"]),

        //                BranchId = reader["BRCD"] == DBNull.Value
        //                    ? null
        //                    : Convert.ToInt32(reader["BRCD"])
        //            });
        //        }
        //    }

        //    return customer;
        //}


        //public async Task<clsCustomer?> GetCustomerById(int id)
        //{
        //    const string sql = @"
        //SELECT *
        //FROM dbo.CUSMAS
        //WHERE ID = @Id
        //  AND ISNULL(DELFLG, 0) = 0;";

        //    var connection = db.GetConnection();

        //    if (connection.State != ConnectionState.Open)
        //        await connection.OpenAsync();

        //    clsCustomer? customer;

        //    using (var command = new SqlCommand(sql, connection))
        //    {
        //        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        //        using var reader = await command.ExecuteReaderAsync();

        //        customer = await reader.ReadAsync()
        //            ? MapCustomer(reader)
        //            : null;
        //    }

        //    if (customer == null)
        //        return null;

        //    // Load related records using the linked account ID.
        //    customer.PendingBills =
        //        await GetPendingBillsAsync(connection, customer.AccId);

        //    // TODO: Load OpeningBalances using the actual
        //    // opening-balance table and its confirmed column names.

        //    return customer;
        //}

        // =========================================================
        // CHECK DUPLICATE CUSTOMER NAME
        // =========================================================
        public async Task<bool> CustomerNameExists(
            string customerName,
            int? excludeId = null)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM CUSMAS
                WHERE ACTIVE=1 AND UPPER(LTRIM(RTRIM(CUSNAME))) =
                      UPPER(LTRIM(RTRIM(@CustomerName)))
                  AND ISNULL(DELFLG, 0) = 0
                  AND (@ExcludeId IS NULL OR ID <> @ExcludeId);";

            var connection = db.GetConnection();

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                "@CustomerName", SqlDbType.NVarChar, 200)
                .Value = customerName.Trim();

            command.Parameters.Add(
                "@ExcludeId", SqlDbType.Int)
                .Value = (object?)excludeId ?? DBNull.Value;

            return Convert.ToInt32(
                await command.ExecuteScalarAsync()) > 0;
        }

        // =========================================================
        // INSERT CUSTOMER
        // =========================================================
   
        public class CustomerLedgerGroupDto
        {
            public int GroupId { get; set; }
            public string GroupCode { get; set; } = string.Empty;
        }

      


        public async Task<string> GenerateCustomerAccountCode(int ledgerGroupId,string groupCode,clsCustomer customer)
        {
            if (ledgerGroupId <= 0)
                throw new ArgumentException("Invalid ledger group ID.");

            if (string.IsNullOrWhiteSpace(groupCode))
                throw new ArgumentException("Ledger group code is required.");

            using var connection = db.GetConnection();

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            // Get the next customer ID.
            // This assumes CUSMAS.ID is an IDENTITY column.
            const string sql = @"
             SELECT IDENT_CURRENT('dbo.CUSMAS')
             + IDENT_INCR('dbo.CUSMAS');";

            using var command = new SqlCommand(sql, connection);

            object? result = await command.ExecuteScalarAsync();

            int nextCustomerId = result == null || result == DBNull.Value
                ? 1
                : Convert.ToInt32(result);

            // Match the legacy account-code format.
            string accountCode =
                $"C{groupCode.Trim()}-{nextCustomerId:D2}";

            return accountCode;
        }

       


        public async Task<CustomerLedgerGroupDto?> GetDefaultCustomerLedgerGroup(int? ledgerGroupId)
        {
            using var connection = db.GetConnection();

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            const string sql = @"
            SELECT
                GROUPID AS GroupId,
                GROUPCODE AS GroupCode
            FROM dbo.GRPLDG
            WHERE GACTIVE=1 AND GROUPID = @GroupId
              AND (@UseDefault = 0 OR GROUPCODE = @GroupCode);";

            bool useDefault = ledgerGroupId <= 0;

            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@GroupId", SqlDbType.Int).Value =
                useDefault ? 26 : ledgerGroupId;

            command.Parameters.Add("@UseDefault", SqlDbType.Bit).Value =
                useDefault;

            command.Parameters.Add("@GroupCode", SqlDbType.VarChar, 20).Value =
                "11000";

            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new CustomerLedgerGroupDto
            {
                GroupId = Convert.ToInt32(reader["GroupId"]),
                GroupCode = Convert.ToString(reader["GroupCode"]) ?? string.Empty
            };
        }






        public string GenerateCustomerAccountCode(int ledgerGroupId,string groupCode,int customerId)
        {
            if (ledgerGroupId <= 0)
                throw new ArgumentException(
                    "Invalid ledger group ID.");

            if (string.IsNullOrWhiteSpace(groupCode))
                throw new ArgumentException(
                    "Ledger group code is required.");

            if (customerId <= 0)
                throw new ArgumentException(
                    "Invalid customer ID.");

            return $"C{groupCode.Trim()}-{customerId:D2}";
        }


        public async Task<int> InsertCustomer(int userId,SqlConnection connection,SqlTransaction transaction,CustomerRequest customer)
        {
            var columns = string.Join(
                ", ",
                Fields.Select(f => f.Column)
                    .Concat(new[] { "LOGCD", "DELFLG" }));

            var values = string.Join(
                ", ",
                Fields.Select(f => "@" + f.Column)
                    .Concat(new[] { "@LOGCD", "0" }));

            var sql = $@"
            INSERT INTO dbo.CUSMAS ({columns})
            OUTPUT INSERTED.ID
            VALUES ({values});";

            using var command = new SqlCommand(sql, connection, transaction);

            // Directly assign values from CustomerRequest.
            AddCustomerParameters(userId, command, customer);

            var result = await command.ExecuteScalarAsync();

            return result == null || result == DBNull.Value
                ? 0
                : Convert.ToInt32(result);
        }
      

        public async Task<string?> ValidateCustomerLookupsAsync(
        CustomerRequest customer,
        int ledgerGroupId)
        {
            using var connection = db.GetConnection();

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            // 1. Validate currency.
            // Currency code 0 is allowed as the default.
            if (customer.CurCode > 0)
            {
                const string currencySql = @"
             SELECT COUNT(1)
              FROM dbo.CURRENCYMAS
                WHERE CODE = @Code;";

                using var command = new SqlCommand(currencySql, connection);

                command.Parameters.Add("@Code", SqlDbType.Int)
                    .Value = customer.CurCode;

                if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
                    return "Selected currency does not exist.";
            }

            // 2. Validate customer ledger group.
            if (ledgerGroupId <= 0)
                return "Invalid customer ledger group.";

            const string ledgerSql = @"
            SELECT COUNT(1)
            FROM dbo.GRPLDG
            WHERE GROUPID = @GroupId
                 AND ISNULL(GDELFLG, 0) = 0
              AND ISNULL(GACTIVE, 1) = 1;";

            using (var command = new SqlCommand(ledgerSql, connection))
            {
                command.Parameters.Add("@GroupId", SqlDbType.Int)
                    .Value = ledgerGroupId;

                if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
                    return "Selected customer ledger group does not exist or is inactive.";
            }

            // 3. Validate customer party group.
            if (customer.PartyGrpId > 0)
            {
                const string partyGroupSql = @"
            SELECT COUNT(1)
            FROM dbo.PARTYGROUPMAS
            WHERE ID = @Id
              AND UPPER(LTRIM(RTRIM(ISNULL(GROUPTYPE, '')))) = 'C';";

                using var command = new SqlCommand(partyGroupSql, connection);

                command.Parameters.Add("@Id", SqlDbType.Int)
                    .Value = customer.PartyGrpId;

                if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
                    return "Selected party group does not exist or is not a customer group.";
            }

            // 4. Validate country from main database.
            // 4. Validate country from main database.
            if (customer.CountryCode <= 0)
                return "Please select a valid country.";

                        const string countrySql = """
                SELECT COUNTRYID AS CountryId
                FROM dbo.COUNTRY
                WHERE COUNTRYID = @CountryId
                  AND ISNULL(ACTIVE, 1) = 1;
                """;

            var country = await db.QueryMasterFirstOrDefaultAsync<CountryDtos>(
                countrySql,
                new { CountryId = customer.CountryCode });

            if (country == null)
                return "Selected country does not exist or is inactive.";


            // 5. Validate region belongs to the selected country.
            if (customer.StateCode is null or <= 0)
                return "Please select a valid region.";

            const string regionSql = """
                SELECT
                    COUNTRYID AS CountryId,
                    REGIONID AS RegionId,
                    REGIONNAME AS RegionName,
                    REGIONSECNAME AS RegionSecName,
                    ACTIVE AS IsActive
                FROM dbo.REGION
                WHERE REGIONID = @RegionId
                  AND COUNTRYID = @CountryId
                  AND ISNULL(ACTIVE, 1) = 1;
                """;

            var region = await db.QueryMasterFirstOrDefaultAsync<RegionResponse>(
                regionSql,
                new
                {
                    RegionId = customer.StateCode,
                    CountryId = customer.CountryCode
                });

            if (region == null)
                return "Selected region does not exist, is inactive, or does not belong to the selected country.";

            // 6. Validate salesman.
            if (customer.SalesMan > 0)
            {
                const string salesmanSql = """
                SELECT SMGRPCODE
                FROM dbo.SMGRP
                WHERE SMGRPCODE = @SalesmanId
                  AND ISNULL(ACTIVE, 1) = 1;
                """;

                var salesman = await db.QueryFirstOrDefaultAsync<int?>(
                    salesmanSql,
                    new { SalesmanId = customer.SalesMan });

                if (salesman == null)
                    return "Selected salesman does not exist or is inactive.";
            }
            return null;
        }

        public async Task<int> InsertCustomerAccount(
        SqlConnection connection,
        SqlTransaction transaction,
        CustomerRequest customer,
        int ledgerGroupId,
        int userId,
        string accountCode,
        CustomerOpeningBalanceRequest? openingBalance)
        {
            const string sql = @"
        INSERT INTO dbo.ACCMAS
        (
            ALIAS,
            ACCNAME,
            ADDRESS,
            PHONE,
            ACTIVE,
            LOGCD,
            UITIME,
            ACCODE,
            PRINTNAME,
            MGROUP,
            DEPRECIATION,
            VATFLAG,
            VATPERC,
            TAXRETFLAG,
            BANKACCNAME,
            BANKACCNO,
            IBANNO,
            SWIFTCODE,
            BRANCH,
            OPENBAL,
            SECNAME,
            CURFLG,
            CURCONVRATE,
            CURCODE,
            DELFLG,
            APRVLST,
            APRVLRBY,
            APRVLRDT
        )
        OUTPUT INSERTED.ID
        VALUES
        (
            @Alias,
            @AccName,
            @Address,
            @Phone,
            @Active,
            @UserId,
            @UiTime,
            @AccCode,
            @PrintName,
            @MGroup,
            0,
            @VatFlag,
            0,
            0,
            @BankAccName,
            @BankAccNo,
            @IbanNo,
            @SwiftCode,
            @Branch,
            @OpeningBalance,
            @SecName,
            @CurFlg,
            @ConversionRate,
            @CurrencyCode,
            0,
            '',
            @UserId,
            GETDATE()
        );";

            using var command = new SqlCommand(sql, connection, transaction);

            command.Parameters.AddWithValue(
                "@Alias", (object?)customer.Alias ?? DBNull.Value);

            command.Parameters.AddWithValue(
                "@AccName", (object?)customer.CusName ?? DBNull.Value);

            command.Parameters.AddWithValue(
                "@Address", (object?)customer.Address ?? DBNull.Value);

            command.Parameters.AddWithValue(
                "@Phone", (object?)customer.Phone ?? DBNull.Value);

            command.Parameters.AddWithValue("@Active", customer.Active);
            command.Parameters.AddWithValue("@UserId", userId);

            command.Parameters.AddWithValue(
                "@UiTime", DateTime.Now);

            command.Parameters.AddWithValue("@AccCode", accountCode);

            command.Parameters.AddWithValue(
                "@PrintName", (object?)customer.PrintName ?? DBNull.Value);

            command.Parameters.AddWithValue("@MGroup", ledgerGroupId);
            command.Parameters.AddWithValue("@VatFlag", customer.VatFlag);

            command.Parameters.AddWithValue(
                "@BankAccName", (object?)customer.BnkAccName ?? DBNull.Value);

            command.Parameters.AddWithValue(
                "@BankAccNo", (object?)customer.BnkAccNo ?? DBNull.Value);

            command.Parameters.AddWithValue(
                "@IbanNo", (object?)customer.IbanNo ?? DBNull.Value);

            command.Parameters.AddWithValue(
               "@SwiftCode", (object?)customer.SwiftCode ?? DBNull.Value);
            
            command.Parameters.AddWithValue(
                "@Branch", (object?)customer.Branch ?? DBNull.Value);

            // OPENBAL is a scalar amount, not the OpeningBalances list.
            command.Parameters.AddWithValue(
                "@OpeningBalance", openingBalance?.Amount ?? 0m);

            command.Parameters.AddWithValue(
                "@SecName", (object?)customer.SecName ?? DBNull.Value);

            command.Parameters.AddWithValue("@CurFlg", customer.CurFlg);

            command.Parameters.AddWithValue(
                "@ConversionRate", openingBalance?.ConversionRate ?? 1m);

            command.Parameters.AddWithValue(
                "@CurrencyCode", openingBalance?.CurrencyCode ?? 0);

            var result = await command.ExecuteScalarAsync();

            return result == null || result == DBNull.Value
                ? 0
                : Convert.ToInt32(result);
        }






        public async Task<CustomerAccountInsertResult>InsertCustomerWithAccount(CustomerRequest customer,int ledgerGroupId,string groupCode,int userId, CustomerOpeningBalanceRequest? openingBalance,
            List<CustomerPendingBillRequest>? pendingBills)
        {
            using var connection = db.GetConnection();

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Insert customer and obtain its actual ID.
                int customerId = await InsertCustomer(
                    userId,
                    connection,
                    transaction,
                    customer);

                if (customerId <= 0)
                {
                    throw new InvalidOperationException(
                        "Customer insertion failed.");
                }

                // 2. Generate account code from the actual customer ID.
                string accountCode = GenerateCustomerAccountCode(
                    ledgerGroupId,
                    groupCode,
                    customerId);

                // 3. Insert account into ACCMAS.
                int accountId = await InsertCustomerAccount(
                    connection,
                    transaction,
                    customer,
                    ledgerGroupId,
                    userId,
                    accountCode, openingBalance);

                if (accountId <= 0)
                {
                    throw new InvalidOperationException(
                        "Account insertion failed.");
                }

                // 4. Link the newly created account to the customer.
                const string updateSql = @"
                UPDATE dbo.CUSMAS
                SET ACCID = @AccountId
                WHERE ID = @CustomerId
                  AND ISNULL(DELFLG, 0) = 0;";

                using (var command = new SqlCommand(
                    updateSql,
                    connection,
                    transaction))
                {
                    command.Parameters.Add(
                        "@AccountId",
                        SqlDbType.Int).Value = accountId;

                    command.Parameters.Add(
                        "@CustomerId",
                        SqlDbType.Int).Value = customerId;

                    int rowsUpdated =
                        await command.ExecuteNonQueryAsync();

                    if (rowsUpdated != 1)
                    {
                        throw new InvalidOperationException(
                            "Failed to link the account to the customer.");
                    }
                }

                // 2. Validate the final opening balance and pending bills
                // before making any database changes.





                // 4. Validate opening balance and pending bills.
                if (openingBalance != null || pendingBills?.Count > 0)
                {
                    string? balanceError =
                        await ValidateFinalCustomerBalancesAsync(
                            connection,
                            transaction,
                            accountId,
                            customer.CurFlg,
                            openingBalance,
                            pendingBills);

                    if (!string.IsNullOrWhiteSpace(balanceError))
                        throw new ArgumentException(balanceError);
                }

                // 5. Insert opening balance.
                if (openingBalance != null)
                {
                    decimal baseAmount = customer.CurFlg
                        ? openingBalance.Amount * openingBalance.ConversionRate
                        : openingBalance.Amount;

                    string drCr = openingBalance.DrCr.Trim().ToUpperInvariant();

                    decimal debit = drCr == "DR" ? baseAmount : 0m;
                    decimal credit = drCr == "CR" ? baseAmount : 0m;

                    await InsertOpeningBalanceAsync(
                        connection,
                        transaction,
                        accountId,
                        userId,
                        openingBalance,
                        debit,
                        credit);
                }

                // 6. Insert pending bills.
                foreach (var bill in pendingBills ?? new List<CustomerPendingBillRequest>())
                {
                    if (openingBalance == null)
                    {
                        throw new ArgumentException(
                            "Currency, conversion rate, and branch details are required for pending bills.");
                    }

                    decimal baseBillAmount = customer.CurFlg
                        ? bill.Amount * openingBalance.ConversionRate
                        : bill.Amount;

                    await InsertPendingBillAsync(
                        connection,
                        transaction,
                        accountId,
                        userId,
                        openingBalance.BranchId,
                        openingBalance.CurrencyCode,
                        openingBalance.ConversionRate,
                        bill,
                        baseBillAmount);
                }

                // Keep the existing commit AFTER all inserts succeed.
                //await transaction.CommitAsync();

                // 5. Commit both inserts and the account link.
                await transaction.CommitAsync();

                //customer.AccId = accountId;

                return new CustomerAccountInsertResult
                {
                    CustomerId = customerId,
                    AccountId = accountId,
                    AccountCode = accountCode
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }




        private static async Task InsertOpeningBalanceAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            int accountId,
            int userId,
            CustomerOpeningBalanceRequest opening,
            decimal debitBaseAmount,
            decimal creditBaseAmount)
        {
            if (opening.BranchId <= 0)
                throw new ArgumentException("A valid branch ID is required.");

            if (opening.ConversionRate <= 0)
                throw new ArgumentException("Conversion rate must be greater than zero.");

            if (opening.Amount < 0)
                throw new ArgumentException("Opening balance cannot be negative.");

            var drCr = opening.DrCr.Trim().ToUpperInvariant();

            if (drCr != "DR" && drCr != "CR")
                throw new ArgumentException("Opening balance type must be DR or CR.");

            const string sql = @"
            INSERT INTO dbo.OPN
            (
                ACCID, DEBIT, CREDIT, LOGCD, UTIME,
                CURCONVRATE, CURCODE, BRCD
            )
            VALUES
            (
                @AccountId, @Debit, @Credit, @UserId, @UpdateTime,
                @ConversionRate, @CurrencyCode, @BranchId
            );";

            using var command = new SqlCommand(sql, connection, transaction);

            command.Parameters.Add("@AccountId", SqlDbType.Int).Value = accountId;
            command.Parameters.Add("@Debit", SqlDbType.Decimal).Value = debitBaseAmount;
            command.Parameters.Add("@Credit", SqlDbType.Decimal).Value = creditBaseAmount;
            command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
            command.Parameters.Add("@UpdateTime", SqlDbType.DateTime).Value = DateTime.Now;

            var rateParameter = command.Parameters.Add(
                "@ConversionRate", SqlDbType.Decimal);
            rateParameter.Precision = 18;
            rateParameter.Scale = 6;
            rateParameter.Value = opening.ConversionRate;

            command.Parameters.Add("@CurrencyCode", SqlDbType.Int).Value =
                opening.CurrencyCode;
            command.Parameters.Add("@BranchId", SqlDbType.Int).Value =
                opening.BranchId;

            await command.ExecuteNonQueryAsync();
        }

        private static async Task InsertPendingBillAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            int accountId,
            int userId,
            int branchId,
            int currencyCode,
            decimal conversionRate,
            CustomerPendingBillRequest bill,
            decimal amountInBaseCurrency)
        {
            if (bill.InvoiceDate is null || bill.InvoiceDate == default(DateTime))
                throw new ArgumentException("Invoice date is required.");

            if (bill.DueDate is null || bill.DueDate == default(DateTime))
                throw new ArgumentException("Due date is required.");

            if (string.IsNullOrWhiteSpace(bill.InvoiceNumber))
                throw new ArgumentException("Invoice number is required.");

            if (amountInBaseCurrency < 0)
                throw new ArgumentException("Pending bill amount cannot be negative.");

            var drCr = bill.DrCr.Trim().ToUpperInvariant();

            if (drCr != "DR" && drCr != "CR")
                throw new ArgumentException("Pending bill type must be DR or CR.");

            const string sql = @"
            INSERT INTO dbo.PENDBIL
            (
                INVDATE, NARRATION, INVNO, AMOUNT, DUEDATE,
                INVTYPE, CODE, LOGCD, UTIME, BILLREFFLG,
                CURCONVRATE, CURCODE, BRCD, DRCR
            )
            VALUES
            (
                @InvoiceDate, @Narration, @InvoiceNumber, @Amount, @DueDate,
                @InvoiceType, @AccountId, @UserId, @UpdateTime, @BillRefFlag,
                @ConversionRate, @CurrencyCode, @BranchId, @DrCr
            );";

            using var command = new SqlCommand(sql, connection, transaction);

            command.Parameters.Add("@InvoiceDate", SqlDbType.DateTime).Value =
                bill.InvoiceDate!.Value;
            command.Parameters.Add("@Narration", SqlDbType.NVarChar, 500).Value =
                (object)bill.Narration ?? DBNull.Value;
            command.Parameters.Add("@InvoiceNumber", SqlDbType.NVarChar, 100).Value =
                bill.InvoiceNumber.Trim();

            var amountParameter = command.Parameters.Add(
                "@Amount", SqlDbType.Decimal);
            amountParameter.Precision = 18;
            amountParameter.Scale = 4;
            amountParameter.Value = amountInBaseCurrency;

            command.Parameters.Add("@DueDate", SqlDbType.DateTime).Value =
                bill.DueDate!.Value;
            command.Parameters.Add("@InvoiceType", SqlDbType.NVarChar, 10).Value = "CB";
            command.Parameters.Add("@AccountId", SqlDbType.Int).Value = accountId;
            command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
            command.Parameters.Add("@UpdateTime", SqlDbType.DateTime).Value = DateTime.Now;

            // Confirm the actual BILLREFFLG type/default from your PENDBIL schema.
            command.Parameters.Add("@BillRefFlag", SqlDbType.Bit).Value = false;

            var rateParameter = command.Parameters.Add(
                "@ConversionRate", SqlDbType.Decimal);
            rateParameter.Precision = 18;
            rateParameter.Scale = 6;
            rateParameter.Value = conversionRate;

            command.Parameters.Add("@CurrencyCode", SqlDbType.Int).Value = currencyCode;
            command.Parameters.Add("@BranchId", SqlDbType.Int).Value = branchId;
            command.Parameters.Add("@DrCr", SqlDbType.NVarChar, 2).Value = drCr;

            await command.ExecuteNonQueryAsync();
        }




        private static async Task<string?> ValidateFinalCustomerBalancesAsync(
    SqlConnection connection,
    SqlTransaction transaction,
    int accountId,
    bool currencyEnabled,
    CustomerOpeningBalanceRequest? openingBalance,
    List<CustomerPendingBillRequest>? pendingBills)
        {
            // ---------------------------------------------------------
            // 1. Read all existing opening balance rows.
            // ---------------------------------------------------------
            const string openingSql = @"
        SELECT
            ID,
            BRCD,
            CURCODE,
            ISNULL(DEBIT, 0) AS DEBIT,
            ISNULL(CREDIT, 0) AS CREDIT
        FROM dbo.OPN WITH (UPDLOCK, HOLDLOCK)
        WHERE ACCID = @AccountId
        ORDER BY ID;";

            var openingRows =
                new List<(int Id, int BranchId, int CurrencyCode,
                          decimal Debit, decimal Credit)>();

            using (var command = new SqlCommand(
                openingSql, connection, transaction))
            {
                command.Parameters.Add("@AccountId", SqlDbType.Int)
                    .Value = accountId;

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    openingRows.Add((
                        Convert.ToInt32(reader["ID"]),
                        Convert.ToInt32(reader["BRCD"]),
                        Convert.ToInt32(reader["CURCODE"]),
                        Convert.ToDecimal(reader["DEBIT"]),
                        Convert.ToDecimal(reader["CREDIT"])
                    ));
                }
            }

            // ---------------------------------------------------------
            // 2. Apply the requested opening balance to the final state.
            // ---------------------------------------------------------
            if (openingBalance != null)
            {
                decimal baseAmount = currencyEnabled
                    ? openingBalance.Amount * openingBalance.ConversionRate
                    : openingBalance.Amount;

                string drCr =
                    openingBalance.DrCr?.Trim().ToUpperInvariant() ?? "";

                decimal newDebit = drCr == "DR" ? baseAmount : 0m;
                decimal newCredit = drCr == "CR" ? baseAmount : 0m;

                // Match the same branch/currency row that
                // UpdateOpeningBalanceAsync will update.
                int existingIndex = openingRows.FindIndex(x =>
                    x.BranchId == openingBalance.BranchId &&
                    x.CurrencyCode == openingBalance.CurrencyCode);

                if (existingIndex >= 0)
                {
                    var existing = openingRows[existingIndex];

                    openingRows[existingIndex] = (
                        existing.Id,
                        existing.BranchId,
                        existing.CurrencyCode,
                        newDebit,
                        newCredit
                    );
                }
                else
                {
                    // UpdateOpeningBalanceAsync will insert a new OPN row.
                    openingRows.Add((
                        0,
                        openingBalance.BranchId,
                        openingBalance.CurrencyCode,
                        newDebit,
                        newCredit
                    ));
                }
            }

            decimal finalOpeningDebit = openingRows.Sum(x => x.Debit);
            decimal finalOpeningCredit = openingRows.Sum(x => x.Credit);

            // ---------------------------------------------------------
            // 3. Read all existing pending bills.
            // ---------------------------------------------------------
            const string pendingSql = @"
        SELECT
            ID,
            ISNULL(AMOUNT, 0) AS AMOUNT,
            UPPER(LTRIM(RTRIM(ISNULL(DRCR, '')))) AS DRCR
        FROM dbo.PENDBIL WITH (UPDLOCK, HOLDLOCK)
        WHERE CODE = @AccountId
        ORDER BY ID;";

            var pendingRows =
                new Dictionary<int, (decimal Amount, string DrCr)>();

            using (var command = new SqlCommand(
                pendingSql, connection, transaction))
            {
                command.Parameters.Add("@AccountId", SqlDbType.Int)
                    .Value = accountId;

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    int billId = Convert.ToInt32(reader["ID"]);
                    decimal amount = Convert.ToDecimal(reader["AMOUNT"]);
                    string drCr = Convert.ToString(reader["DRCR"]) ?? "";

                    pendingRows[billId] = (amount, drCr);
                }
            }

            // ---------------------------------------------------------
            // 4. Apply requested pending bill changes.
            // Existing IDs replace their old values.
            // New bills are added to the final total.
            // Omitted existing bills remain unchanged.
            // ---------------------------------------------------------
            if (pendingBills != null)
            {
                var processedIds = new HashSet<int>();

                foreach (var bill in pendingBills)
                {
                    if (bill.Amount < 0)
                    {
                        return
                            $"Pending bill '{bill.InvoiceNumber}' amount cannot be negative.";
                    }

                    string drCr =
                        bill.DrCr?.Trim().ToUpperInvariant() ?? "";

                    if (drCr != "DR" && drCr != "CR")
                    {
                        return
                            $"Pending bill '{bill.InvoiceNumber}' DrCr must be either DR or CR.";
                    }

                    if (string.IsNullOrWhiteSpace(bill.InvoiceNumber))
                    {
                        return "Pending bill invoice number is required.";
                    }

                    if (bill.Id.HasValue && bill.Id.Value > 0)
                    {
                        int billId = bill.Id.Value;

                        if (!processedIds.Add(billId))
                        {
                            return $"Pending bill ID {billId} was supplied more than once.";
                        }

                        if (!pendingRows.ContainsKey(billId))
                        {
                            return
                                $"Pending bill ID {billId} does not exist for this customer.";
                        }

                        // Replace existing bill amount and direction.
                        pendingRows[billId] = (bill.Amount, drCr);
                    }
                    else
                    {
                        // New pending bill.
                        // Use a temporary negative key so it cannot collide
                        // with an existing database ID.
                        int temporaryId = -1;

                        while (pendingRows.ContainsKey(temporaryId))
                        {
                            temporaryId--;
                        }

                        pendingRows[temporaryId] = (bill.Amount, drCr);
                    }
                }
            }

            // ---------------------------------------------------------
            // 5. Calculate final pending debit and credit totals.
            // ---------------------------------------------------------
            decimal finalPendingDebit = pendingRows.Values
                .Where(x => x.DrCr == "DR")
                .Sum(x => x.Amount);

            decimal finalPendingCredit = pendingRows.Values
                .Where(x => x.DrCr == "CR")
                .Sum(x => x.Amount);

            // ---------------------------------------------------------
            // 6. Compare final pending balances with final opening balance.
            // ---------------------------------------------------------
            if (finalPendingDebit > finalOpeningDebit)
            {
                return
                    $"Total pending debit bills ({finalPendingDebit:N2}) cannot exceed " +
                    $"the opening debit balance ({finalOpeningDebit:N2}).";
            }

            if (finalPendingCredit > finalOpeningCredit)
            {
                return
                    $"Total pending credit bills ({finalPendingCredit:N2}) cannot exceed " +
                    $"the opening credit balance ({finalOpeningCredit:N2}).";
            }

            return null;
        }


        //public async Task<bool> UpdateCustomer(int id,clsCustomer customer,byte[] originalRowVersion)
        //{
        //    using var connection = db.GetConnection();

        //    if (connection.State != ConnectionState.Open)
        //        await connection.OpenAsync();

        //    var setClause = string.Join(
        //        ", ",
        //        Fields
        //            .Where(f => f.Column != "ACCID")
        //            .Select(f => $"{f.Column} = @{f.Column}"));

        //    var sql = $@"
        //    UPDATE dbo.CUSMAS
        //    SET {setClause}
        //    WHERE ID = @Id
        //  AND ISNULL(DELFLG, 0) = 0
        //  AND RowVersion = @OriginalRowVersion;";

        //    using var command = new SqlCommand(sql, connection);

        //    AddCustomerParameters(command, customer);

        //    command.Parameters.Add(
        //        "@Id",
        //        SqlDbType.Int).Value = id;

        //    command.Parameters.Add(
        //        "@OriginalRowVersion",
        //        SqlDbType.Binary,
        //        8).Value = originalRowVersion;

        //    return await command.ExecuteNonQueryAsync() > 0;
        //}


        //    public async Task<bool> UpdateCustomer(int id,clsCustomer customer,byte[] originalRowVersion,int? ledgerGroupId = null,
        //        CustomerOpeningBalanceRequest? openingBalances = null,
        //List<CustomerPendingBillRequest>? pendingBills = null)
        //    {
        //        using var connection = db.GetConnection();

        //        if (connection.State != ConnectionState.Open)
        //            await connection.OpenAsync();

        //        using var transaction = connection.BeginTransaction();

        //        try
        //        {
        //            // Lock and retrieve the linked account ID.
        //            const string accountSql = @"
        //        SELECT ACCID
        //        FROM dbo.CUSMAS WITH (UPDLOCK, HOLDLOCK)
        //        WHERE ID = @Id
        //          AND ISNULL(DELFLG, 0) = 0
        //          AND RowVersion = @OriginalRowVersion;";

        //            int accountId;

        //            using (var command = new SqlCommand(
        //                accountSql, connection, transaction))
        //            {
        //                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        //                command.Parameters.Add("@OriginalRowVersion",
        //                    SqlDbType.Binary, 8).Value = originalRowVersion;

        //                object? result = await command.ExecuteScalarAsync();

        //                if (result == null || result == DBNull.Value)
        //                {
        //                    transaction.Rollback();
        //                    return false;
        //                }

        //                accountId = Convert.ToInt32(result);
        //            }

        //            // Validate a supplied ledger group inside the transaction too.
        //            if (ledgerGroupId.HasValue)
        //            {
        //                const string groupSql = @"
        //            SELECT COUNT(1)
        //            FROM dbo.GRPLDG
        //            WHERE GROUPID = @GroupId;";

        //                using var groupCommand = new SqlCommand(
        //                    groupSql, connection, transaction);

        //                groupCommand.Parameters.Add("@GroupId", SqlDbType.Int)
        //                    .Value = ledgerGroupId.Value;

        //                int groupExists = Convert.ToInt32(
        //                    await groupCommand.ExecuteScalarAsync());

        //                if (groupExists == 0)
        //                {
        //                    transaction.Rollback();
        //                    throw new ArgumentException(
        //                        "The specified ledger group does not exist.",
        //                        nameof(ledgerGroupId));
        //                }
        //            }

        //            // Update customer fields; never change ACCID or RowVersion directly.
        //            var updateFields = Fields
        //                .Where(f =>
        //                    !string.Equals(f.Column, "ACCID",
        //                        StringComparison.OrdinalIgnoreCase) &&
        //                    !string.Equals(f.Column, "RowVersion",
        //                        StringComparison.OrdinalIgnoreCase))
        //                .ToList();

        //            if (updateFields.Count == 0)
        //                throw new InvalidOperationException(
        //                    "No customer fields are configured for updating.");

        //            string setClause = string.Join(", ",
        //                updateFields.Select(f => $"[{f.Column}] = @{f.Column}"));

        //            string customerSql = $@"
        //            UPDATE dbo.CUSMAS
        //            SET {setClause}
        //            WHERE ID = @Id
        //          AND ISNULL(DELFLG, 0) = 0
        //          AND RowVersion = @OriginalRowVersion;";

        //            using (var command = new SqlCommand(
        //                customerSql, connection, transaction))
        //            {
        //                AddCustomerParameters(command, customer);

        //                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        //                command.Parameters.Add("@OriginalRowVersion",
        //                    SqlDbType.Binary, 8).Value = originalRowVersion;

        //                int rowsUpdated = await command.ExecuteNonQueryAsync();

        //                if (rowsUpdated != 1)
        //                {
        //                    transaction.Rollback();
        //                    return false;
        //                }
        //            }

        //            // Update the existing linked ledger account.
        //            // Preserve ACCID and ACCODE.
        //            string accountUpdateSql = ledgerGroupId.HasValue
        //                ? @"
        //            UPDATE dbo.ACCMAS
        //            SET ACCNAME = @AccountName,
        //                MGROUP = @GroupId
        //            WHERE ID = @AccountId;"
        //                : @"
        //            UPDATE dbo.ACCMAS
        //            SET ACCNAME = @AccountName
        //            WHERE ID = @AccountId;";

        //            using (var command = new SqlCommand(
        //                accountUpdateSql, connection, transaction))
        //            {
        //                command.Parameters.Add("@AccountName", SqlDbType.NVarChar, 200)
        //                    .Value = customer.CusName.Trim();

        //                command.Parameters.Add("@AccountId", SqlDbType.Int)
        //                    .Value = accountId;

        //                if (ledgerGroupId.HasValue)
        //                {
        //                    command.Parameters.Add("@GroupId", SqlDbType.Int)
        //                        .Value = ledgerGroupId.Value;
        //                }

        //                int accountRowsUpdated = await command.ExecuteNonQueryAsync();

        //                if (accountRowsUpdated != 1)
        //                {
        //                    throw new InvalidOperationException(
        //                        "The linked ledger account could not be updated.");
        //                }
        //            }

        //            transaction.Commit();
        //            return true;
        //        }
        //        catch
        //        {
        //            try
        //            {
        //                transaction.Rollback();
        //            }
        //            catch
        //            {
        //                // Preserve the original exception.
        //            }

        //            throw;
        //        }
        //    }


        public async Task<bool> UpdateCustomer(
    int userId,
    int id,
    CustomerUpdateRequest customer,
    byte[] originalRowVersion,
    int curCode,
    int? ledgerGroupId = null,
    CustomerOpeningBalanceRequest? openingBalance = null,
    List<CustomerPendingBillRequest>? pendingBills = null)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            if (id <= 0)
                throw new ArgumentException("Invalid customer ID.");

            if (originalRowVersion == null || originalRowVersion.Length != 8)
                throw new ArgumentException("Invalid RowVersion.");

            using var connection = db.GetConnection();

            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Get linked account ID and verify RowVersion.
                const string getAccountSql = @"
            SELECT ACCID
            FROM dbo.CUSMAS WITH (UPDLOCK, HOLDLOCK)
            WHERE ID = @Id
              AND ISNULL(DELFLG, 0) = 0
              AND RowVersion = @OriginalRowVersion;";

                int? accountId = await connection.QueryFirstOrDefaultAsync<int?>(
                    getAccountSql,
                    new
                    {
                        Id = id,
                        OriginalRowVersion = originalRowVersion
                    },
                    transaction);

                if (!accountId.HasValue || accountId.Value <= 0)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                // 2. Validate ledger group when supplied.
                if (ledgerGroupId.HasValue)
                {
                    const string groupSql = @"
                SELECT COUNT(1)
                FROM dbo.GRPLDG
                WHERE GROUPID = @GroupId;";

                    int groupExists = await connection.ExecuteScalarAsync<int>(
                        groupSql,
                        new { GroupId = ledgerGroupId.Value },
                        transaction);

                    if (groupExists == 0)
                        throw new ArgumentException("Invalid customer ledger group.");
                }

                // 3. Update customer master.
                const string updateCustomerSql = @"
            UPDATE dbo.CUSMAS
            SET
                ALIAS = @ALIAS,
                CUSNAME = @CUSNAME,
                ADDRESS = @ADDRESS,
                PHONE = @PHONE,
                EMAIL = @EMAIL,
                ACTIVE = @ACTIVE,
                LOGCD = @LOGCD,
                DISC = @DISC,
                STATECODE = @STATECODE,
                PRINTNAME = @PRINTNAME,
                COUNTRYCODE = @COUNTRYCODE,
                PRICELEVEL = @PRICELEVEL,
                BNKACCNAME = @BNKACCNAME,
                BNKACCNO = @BNKACCNO,
                BRANCH = @BRANCH,
                IBANNO = @IBANNO,
                VATFLAG = @VATFLAG,
                BILLFLAG = @BILLFLAG,
                CRPERFLAG = @CRPERFLAG,
                CRLIMITFLAG = @CRLIMITFLAG,
                CRLIMIT = @CRLIMIT,
                UITIME = @UITIME,
                VATREGNO = @VATREGNO,
                SHIPADDRESS = @SHIPADDRESS,
                SECNAME = @SECNAME,
                CURFLG = @CURFLG,
                SALESMAN = @SALESMAN,
                CRPERIOD = @CRPERIOD,
                GCCFLG = @GCCFLG,
                STREETONE = @STREETONE,
                STREETTWO = @STREETTWO,
                BULDINGNO = @BULDINGNO,
                PLOTID = @PLOTID,
                CITYNAME = @CITYNAME,
                POSTALCODE = @POSTALCODE,
                DISTRICT = @DISTRICT,
                GRPVATNO = @GRPVATNO,
                EINVIDTYPE = @EINVIDTYPE,
                EINVID = @EINVID,
                VATTYPE = @VATTYPE,
                PARTYGRPID = @PARTYGRPID
            WHERE ID = @Id
              AND ISNULL(DELFLG, 0) = 0
              AND RowVersion = @OriginalRowVersion;";

                using (var command = new SqlCommand(updateCustomerSql, connection, transaction))
                {
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                    command.Parameters.Add("@OriginalRowVersion", SqlDbType.Binary, 8)
                        .Value = originalRowVersion;

                    command.Parameters.Add("@ALIAS", SqlDbType.NVarChar, 255)
                        .Value = (object?)customer.Alias ?? DBNull.Value;

                    command.Parameters.Add("@CUSNAME", SqlDbType.NVarChar, 255)
                        .Value = customer.CusName!.Trim();

                    command.Parameters.Add("@ADDRESS", SqlDbType.NVarChar, 255)
                        .Value = (object?)customer.Address ?? DBNull.Value;

                    command.Parameters.Add("@PHONE", SqlDbType.NVarChar, 255)
                        .Value = (object?)customer.Phone ?? DBNull.Value;

                    command.Parameters.Add("@EMAIL", SqlDbType.NVarChar, 255)
                        .Value = (object?)customer.Email ?? DBNull.Value;

                    command.Parameters.Add("@ACTIVE", SqlDbType.Bit)
                        .Value = customer.Active;

                    command.Parameters.Add("@LOGCD", SqlDbType.NVarChar, 255)
                        .Value = userId.ToString();

                    command.Parameters.Add("@DISC", SqlDbType.NVarChar, 255)
                        .Value = "0";

                    command.Parameters.Add("@STATECODE", SqlDbType.Int)
                        .Value = (object?)customer.StateCode ?? DBNull.Value;

                    command.Parameters.Add("@PRINTNAME", SqlDbType.NVarChar, 255)
                        .Value = (object?)customer.PrintName ?? DBNull.Value;

                    command.Parameters.Add("@COUNTRYCODE", SqlDbType.Int)
                        .Value = customer.CountryCode;

                    var priceLevel = command.Parameters.Add("@PRICELEVEL", SqlDbType.Decimal);
                    priceLevel.Precision = 18;
                    priceLevel.Scale = 4;
                    priceLevel.Value = 0m;

                    command.Parameters.Add("@BNKACCNAME", SqlDbType.NVarChar, 255)
                        .Value = (object?)customer.BnkAccName ?? DBNull.Value;

                    command.Parameters.Add("@BNKACCNO", SqlDbType.NVarChar, 255)
                        .Value = (object?)customer.BnkAccNo ?? DBNull.Value;

                    command.Parameters.Add("@BRANCH", SqlDbType.NVarChar, 255)
                        .Value = (object?)customer.Branch ?? DBNull.Value;

                    command.Parameters.Add("@IBANNO", SqlDbType.NVarChar, 255)
                        .Value = (object?)customer.IbanNo ?? DBNull.Value;

                    command.Parameters.Add("@VATFLAG", SqlDbType.Bit)
                        .Value = customer.VatFlag;

                    command.Parameters.Add("@BILLFLAG", SqlDbType.Bit)
                        .Value = customer.BillFlag;

                    command.Parameters.Add("@CRPERFLAG", SqlDbType.Bit)
                        .Value = customer.CrPerFlag;

                    command.Parameters.Add("@CRLIMITFLAG", SqlDbType.Bit)
                        .Value = customer.CrLimitFlag;

                    var crLimit = command.Parameters.Add("@CRLIMIT", SqlDbType.Decimal);
                    crLimit.Precision = 18;
                    crLimit.Scale = 4;
                    crLimit.Value = customer.CrLimit;

                    command.Parameters.Add("@UITIME", SqlDbType.DateTime)
                        .Value = DateTime.Now;

                    command.Parameters.Add("@VATREGNO", SqlDbType.NVarChar, 255)
                        .Value = (object?)customer.VatRegNo ?? DBNull.Value;

                    command.Parameters.Add("@SHIPADDRESS", SqlDbType.NVarChar, 255)
                        .Value = (object?)customer.ShipAddress ?? DBNull.Value;

                    command.Parameters.Add("@SECNAME", SqlDbType.NVarChar, 255)
                        .Value = (object?)customer.SecName ?? DBNull.Value;

                    command.Parameters.Add("@CURFLG", SqlDbType.Bit)
                        .Value = customer.CurFlg;

                    // Important: nullable SalesMan must be sent as DBNull.Value.
                    command.Parameters.Add("@SALESMAN", SqlDbType.Int)
                        .Value = (object?)customer.SalesMan ?? DBNull.Value;

                    command.Parameters.Add("@CRPERIOD", SqlDbType.Int)
                        .Value = customer.CrPeriod;

                    command.Parameters.Add("@GCCFLG", SqlDbType.Bit)
                        .Value = customer.GccFlg;

                    command.Parameters.Add("@STREETONE", SqlDbType.NVarChar, 250)
                        .Value = (object?)customer.StreetOne ?? DBNull.Value;

                    command.Parameters.Add("@STREETTWO", SqlDbType.NVarChar, 250)
                        .Value = (object?)customer.StreetTwo ?? DBNull.Value;

                    command.Parameters.Add("@BULDINGNO", SqlDbType.NVarChar, 250)
                        .Value = (object?)customer.BuildingNo ?? DBNull.Value;

                    command.Parameters.Add("@PLOTID", SqlDbType.NVarChar, 250)
                        .Value = (object?)customer.PlotId ?? DBNull.Value;

                    command.Parameters.Add("@CITYNAME", SqlDbType.NVarChar, 250)
                        .Value = (object?)customer.CityName ?? DBNull.Value;

                    command.Parameters.Add("@POSTALCODE", SqlDbType.NVarChar, 250)
                        .Value = (object?)customer.PostalCode ?? DBNull.Value;

                    command.Parameters.Add("@DISTRICT", SqlDbType.NVarChar, 250)
                        .Value = (object?)customer.District ?? DBNull.Value;

                    command.Parameters.Add("@GRPVATNO", SqlDbType.NVarChar, 250)
                        .Value = (object?)customer.GrpVatNo ?? DBNull.Value;

                    command.Parameters.Add("@EINVIDTYPE", SqlDbType.NVarChar, 250)
                        .Value = (object?)customer.EinvIdType ?? DBNull.Value;

                    command.Parameters.Add("@EINVID", SqlDbType.NVarChar, 250)
                        .Value = (object?)customer.EinvId ?? DBNull.Value;

                    command.Parameters.Add("@VATTYPE", SqlDbType.NVarChar, 250)
                        .Value = (object?)customer.VatType ?? DBNull.Value;

                    command.Parameters.Add("@PARTYGRPID", SqlDbType.Int)
                        .Value = customer.PartyGrpId;

                    int affectedRows = await command.ExecuteNonQueryAsync();

                    if (affectedRows != 1)
                    {
                        await transaction.RollbackAsync();
                        return false;
                    }
                }

                // 4. Update linked ACCMAS account.
                if (ledgerGroupId.HasValue)
                {
                    const string updateAccountSql = @"
                UPDATE dbo.ACCMAS
                SET
                    ACCNAME = @AccountName,
                    MGROUP = @GroupId,
                    SWIFTCODE = @SwiftCode
                WHERE ID = @AccountId;";

                    int accountUpdated = await connection.ExecuteAsync(
                        updateAccountSql,
                        new
                        {
                            AccountName = customer.CusName!.Trim(),
                            GroupId = ledgerGroupId.Value,
                            SwiftCode = customer.SwiftCode,
                            AccountId = accountId.Value
                        },
                        transaction);

                    if (accountUpdated != 1)
                        throw new InvalidOperationException(
                            "Unable to update the customer ledger account.");
                }
                else
                {
                    const string updateAccountSql = @"
                UPDATE dbo.ACCMAS
                SET
                    ACCNAME = @AccountName,
                    SWIFTCODE = @SwiftCode
                WHERE ID = @AccountId;";

                    int accountUpdated = await connection.ExecuteAsync(
                        updateAccountSql,
                        new
                        {
                            AccountName = customer.CusName!.Trim(),
                            SwiftCode = customer.SwiftCode,
                            AccountId = accountId.Value
                        },
                        transaction);

                    if (accountUpdated != 1)
                        throw new InvalidOperationException(
                            "Unable to update the customer ledger account.");
                }

                // 5. Update opening balance.
                if (openingBalance != null)
                {
                    await UpdateOpeningBalanceAsync(
                        connection,
                        transaction,
                        accountId.Value,
                        customer.CurFlg,
                        userId,
                        openingBalance);
                }

                // 6. Update pending bills.
                if (pendingBills != null)
                {
                    await UpdatePendingBillsAsync(
                        connection,
                        transaction,
                        accountId.Value,
                        curCode,
                        userId,
                        pendingBills);
                }

                // 7. Commit transaction.
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }



        //public async Task<bool> UpdateCustomers(
        //    int userId,
        //    int id,
        //    CustomerUpdateRequest customer,
        //    byte[] originalRowVersion,
        //    int CurCode,
        //    int? ledgerGroupId = null,
        //    CustomerOpeningBalanceRequest? openingBalance = null,
        //    List<CustomerPendingBillRequest>? pendingBills = null)
        //{
        //    if (originalRowVersion == null || originalRowVersion.Length != 8)
        //        throw new ArgumentException("A valid RowVersion is required.");

        //    var connection = db.GetConnection();

        //    if (connection.State != ConnectionState.Open)
        //        await connection.OpenAsync();

        //    using var transaction = connection.BeginTransaction();

        //    try
        //    {
        //        var LogCd=Convert.ToString(userId);
        //        // 1. Lock customer and retrieve linked account.
        //        const string accountSql = @"
        //        SELECT ACCID
        //        FROM dbo.CUSMAS WITH (UPDLOCK, HOLDLOCK)
        //        WHERE ID = @Id
        //          AND ISNULL(DELFLG, 0) = 0
        //          AND RowVersion = @OriginalRowVersion;";

        //        int accountId;

        //        using (var command = new SqlCommand(accountSql, connection, transaction))
        //        {
        //            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        //            command.Parameters.Add(
        //                "@OriginalRowVersion", SqlDbType.Binary, 8)
        //                .Value = originalRowVersion;

        //            object? result = await command.ExecuteScalarAsync();

        //            if (result == null || result == DBNull.Value)
        //            {
        //                transaction.Rollback();
        //                return false;
        //            }

        //            accountId = Convert.ToInt32(result);
        //        }

        //        // 2. Validate ledger group.
        //        if (ledgerGroupId.HasValue)
        //        {
        //            if (ledgerGroupId.Value <= 0)
        //                throw new ArgumentException(
        //                    "Ledger group ID must be greater than zero.");

        //            const string groupSql = @"
        //        SELECT COUNT(1)
        //        FROM dbo.GRPLDG
        //        WHERE GROUPID = @GroupId;";

        //            using var command = new SqlCommand(
        //                groupSql, connection, transaction);

        //            command.Parameters.Add("@GroupId", SqlDbType.Int)
        //                .Value = ledgerGroupId.Value;

        //            int exists = Convert.ToInt32(
        //                await command.ExecuteScalarAsync());

        //            if (exists == 0)
        //                throw new ArgumentException(
        //                    "The specified ledger group does not exist.");
        //        }

        //        // 3. Update customer master.
        //        var updateFields = Fields
        //            .Where(f =>
        //                !string.Equals(
        //                    f.Column, "ACCID",
        //                    StringComparison.OrdinalIgnoreCase) &&
        //                !string.Equals(
        //                    f.Column, "RowVersion",
        //                    StringComparison.OrdinalIgnoreCase))
        //            .ToList();

        //        if (updateFields.Count == 0)
        //            throw new InvalidOperationException(
        //                "No customer fields are configured for updating.");

        //        string setClause = string.Join(
        //            ", ",
        //            updateFields.Select(f => $"[{f.Column}] = @{f.Column}"));

        //        string customerSql = $@"
        //    UPDATE dbo.CUSMAS
        //    SET {setClause}
        //    WHERE ID = @Id
        //      AND ISNULL(DELFLG, 0) = 0
        //      AND RowVersion = @OriginalRowVersion;";

        //        using (var command = new SqlCommand(
        //            customerSql, connection, transaction))
        //        {
        //            AddCustomerParameters(userId, command, customer);

        //            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        //            command.Parameters.Add(
        //                "@OriginalRowVersion", SqlDbType.Binary, 8)
        //                .Value = originalRowVersion;

        //            if (await command.ExecuteNonQueryAsync() != 1)
        //            {
        //                transaction.Rollback();
        //                return false;
        //            }
        //        }

        //        // 4. Update linked ledger account.
              
        //        // 4. Update linked ledger account, including SWIFTCODE.
        //        string accountUpdateSql = ledgerGroupId.HasValue
        //            ? @"
        //            UPDATE dbo.ACCMAS
        //            SET ACCNAME = @AccountName,
        //                MGROUP = @GroupId,
        //                SWIFTCODE = @SwiftCode
        //            WHERE ID = @AccountId;"
        //                        : @"
        //            UPDATE dbo.ACCMAS
        //            SET ACCNAME = @AccountName,
        //                SWIFTCODE = @SwiftCode
        //            WHERE ID = @AccountId;";

        //        using (var command = new SqlCommand(
        //            accountUpdateSql, connection, transaction))
        //        {
        //            command.Parameters.Add(
        //                "@AccountName", SqlDbType.NVarChar, 200)
        //                .Value = customer.CusName!.Trim();

        //            command.Parameters.Add(
        //                "@SwiftCode", SqlDbType.NVarChar, 50)
        //                .Value = string.IsNullOrWhiteSpace(customer.SwiftCode)
        //                    ? DBNull.Value
        //                    : customer.SwiftCode.Trim();

        //            command.Parameters.Add("@AccountId", SqlDbType.Int)
        //                .Value = accountId;

        //            if (ledgerGroupId.HasValue)
        //            {
        //                command.Parameters.Add("@GroupId", SqlDbType.Int)
        //                    .Value = ledgerGroupId.Value;
        //            }

        //            if (await command.ExecuteNonQueryAsync() != 1)
        //            {
        //                throw new InvalidOperationException(
        //                    "The linked ledger account could not be updated.");
        //            }
        //        }
        //        if (openingBalance != null)
        //        {
        //            await UpdateOpeningBalanceAsync(
        //                connection,
        //                transaction,
        //                accountId,
        //                customer.CurFlg,
        //                userId,
        //                openingBalance);
        //        }

        //        // 6. Replace pending bills if included in the request.
        //        if (pendingBills != null)
        //        {
        //            int? logCode = null;

        //            if (!string.IsNullOrWhiteSpace(LogCd))
        //            {
        //                if (!int.TryParse(LogCd, out int parsedLogCode))
        //                {
        //                    throw new ArgumentException(
        //                        "LogCd must be a valid integer.");
        //                }

        //                logCode = parsedLogCode;
        //            }
        //            await UpdatePendingBillsAsync(
        //                connection,
        //                transaction,
        //                accountId,
        //               //logCode,
        //                CurCode,
        //               userId,
        //                pendingBills);
        //        }

        //        // 7. Commit all changes together.
        //        transaction.Commit();
        //        return true;
        //    }
        //    catch
        //    {
        //        try
        //        {
        //            transaction.Rollback();
        //        }
        //        catch
        //        {
        //            // Preserve the original exception.
        //        }

        //        throw;
        //    }
        //}


        // ============================================================
        // OPENING BALANCE
        // ============================================================
        private static async Task UpdateOpeningBalanceAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            int accountId,
            bool currencyEnabled,
            int? logCode,
            CustomerOpeningBalanceRequest openingBalance)
        {
            if (openingBalance.Amount < 0)
                throw new ArgumentException(
                    "Opening balance amount cannot be negative.");

            //if (openingBalance.CurrencyCode <= 0)
            //    throw new ArgumentException(
            //        "A valid opening balance currency is required.");

            // Currency code is mandatory only when multi-currency is enabled.
            if (currencyEnabled && openingBalance.CurrencyCode <= 0)
            {
                throw new ArgumentException(
                    "A valid opening balance currency is required.");
            }

            // Conversion rate is mandatory only when currency conversion is enabled.
            if (currencyEnabled && openingBalance.ConversionRate <= 0)
            {
                throw new ArgumentException(
                    "A valid opening balance conversion rate is required.");
            }
            if (openingBalance.BranchId <= 0)
                throw new ArgumentException(
                    "A valid opening balance branch is required.");

            if (openingBalance.ConversionRate <= 0)
                throw new ArgumentException(
                    "Conversion rate must be greater than zero.");

            string drCr = openingBalance.DrCr.Trim().ToUpperInvariant();

            if (drCr != "DR" && drCr != "CR")
                throw new ArgumentException(
                    "Opening balance DrCr must be DR or CR.");

            // Confirm this conversion matches your legacy CurrConvDB.ToBase().
            decimal baseAmount = currencyEnabled
                ? openingBalance.Amount * openingBalance.ConversionRate
                : openingBalance.Amount;

            decimal debit = drCr == "DR" ? baseAmount : 0m;
            decimal credit = drCr == "CR" ? baseAmount : 0m;

                const string findSql = @"
            SELECT TOP 1 ID
            FROM dbo.OPN WITH (UPDLOCK, HOLDLOCK)
            WHERE ACCID = @AccountId
              AND BRCD = @BranchId
              AND CURCODE = @CurrencyCode
            ORDER BY ID;";

            int? openingId = null;

            using (var command = new SqlCommand(
                findSql, connection, transaction))
            {
                command.Parameters.Add("@AccountId", SqlDbType.Int)
                    .Value = accountId;

                command.Parameters.Add("@BranchId", SqlDbType.Int)
                    .Value = openingBalance.BranchId;

                command.Parameters.Add("@CurrencyCode", SqlDbType.Int)
                    .Value = openingBalance.CurrencyCode;

                object? result = await command.ExecuteScalarAsync();

                if (result != null && result != DBNull.Value)
                    openingId = Convert.ToInt32(result);
            }

            if (openingId.HasValue)
            {
                const string updateSql = @"
            UPDATE dbo.OPN
            SET DEBIT = @Debit,
                CREDIT = @Credit,
                CURCONVRATE = @ConversionRate,
                CURCODE = @CurrencyCode,
                BRCD = @BranchId,
                LOGCD = @LogCode,
                UTIME = GETDATE()
            WHERE ID = @Id
              AND ACCID = @AccountId;";

                using var command = new SqlCommand(
                    updateSql, connection, transaction);

                command.Parameters.Add("@Id", SqlDbType.Int)
                    .Value = openingId.Value;

                command.Parameters.Add("@AccountId", SqlDbType.Int)
                    .Value = accountId;

                command.Parameters.Add("@Debit", SqlDbType.Decimal)
                    .Value = debit;

                command.Parameters.Add("@Credit", SqlDbType.Decimal)
                    .Value = credit;

                command.Parameters.Add("@ConversionRate", SqlDbType.Decimal)
                    .Value = openingBalance.ConversionRate;

                command.Parameters.Add("@CurrencyCode", SqlDbType.Int)
                    .Value = openingBalance.CurrencyCode;

                command.Parameters.Add("@BranchId", SqlDbType.Int)
                    .Value = openingBalance.BranchId;

                command.Parameters.Add("@LogCode", SqlDbType.Int)
                    .Value = (object?)logCode ?? DBNull.Value;

                await command.ExecuteNonQueryAsync();
            }
            else
            {
                const string insertSql = @"
            INSERT INTO dbo.OPN
            (
                ACCID, DEBIT, CREDIT, LOGCD, UTIME,
                CURCONVRATE, CURCODE, BRCD
            )
            VALUES
            (
                @AccountId, @Debit, @Credit, @LogCode, GETDATE(),
                @ConversionRate, @CurrencyCode, @BranchId
            );";

                using var command = new SqlCommand(
                    insertSql, connection, transaction);

                command.Parameters.Add("@AccountId", SqlDbType.Int)
                    .Value = accountId;

                command.Parameters.Add("@Debit", SqlDbType.Decimal)
                    .Value = debit;

                command.Parameters.Add("@Credit", SqlDbType.Decimal)
                    .Value = credit;

                command.Parameters.Add("@LogCode", SqlDbType.Int)
                    .Value = (object?)logCode ?? DBNull.Value;

                command.Parameters.Add("@ConversionRate", SqlDbType.Decimal)
                    .Value = openingBalance.ConversionRate;

                command.Parameters.Add("@CurrencyCode", SqlDbType.Int)
                    .Value = openingBalance.CurrencyCode;

                command.Parameters.Add("@BranchId", SqlDbType.Int)
                    .Value = openingBalance.BranchId;

                await command.ExecuteNonQueryAsync();
            }
        }


        // ============================================================
        // PENDING BILLS
        // ============================================================



        private static async Task UpdatePendingBillsAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int accountId,int CurCode,
        int? logCode,
        List<CustomerPendingBillRequest> pendingBills)
            {
                const string updateSql = @"
            UPDATE dbo.PENDBIL
            SET
                INVDATE = @InvoiceDate,
                NARRATION = @Narration,
                INVNO = @InvoiceNumber,
                AMOUNT = @Amount,
                DUEDATE = @DueDate,
                LOGCD = @LogCode,
                UTIME = GETDATE(),
                DRCR = @DrCr
            WHERE ID = @Id
              AND CODE = @AccountId;";

                const string insertSql = @"
            INSERT INTO dbo.PENDBIL
            (
                INVDATE,
                NARRATION,
                INVNO,
                AMOUNT,
                DUEDATE,
                INVTYPE,
                CODE,
                LOGCD,
                UTIME,
                BILLREFFLG,
                CURCONVRATE,
                CURCODE,
                BRCD,
                DRCR
            )
            VALUES
            (
                @InvoiceDate,
                @Narration,
                @InvoiceNumber,
                @Amount,
                @DueDate,
                @InvoiceType,
                @AccountId,
                @LogCode,
                GETDATE(),
                0,
                1,
                @CurrencyCode,
                @BranchId,
                @DrCr
            );";

            foreach (var bill in pendingBills)
            {
                if (string.IsNullOrWhiteSpace(bill.InvoiceNumber))
                    throw new ArgumentException("Invoice number is required.");

                if (bill.Amount < 0)
                    throw new ArgumentException("Bill amount cannot be negative.");

                if (bill.InvoiceDate is null || bill.InvoiceDate == default(DateTime))
                    throw new ArgumentException("Invoice date is required.");

                if (bill.DueDate is null || bill.DueDate == default(DateTime))
                    throw new ArgumentException("Due date is required.");

                var drCr = bill.DrCr?.Trim().ToUpperInvariant();

                if (drCr is not ("DR" or "CR"))
                {
                    throw new ArgumentException(
                        "DrCr must be either DR or CR.");
                }

                var parameters = new
                {
                    Id = bill.Id,
                    AccountId = accountId,
                    InvoiceDate = bill.InvoiceDate!.Value.Date,
                    Narration = bill.Narration,
                    InvoiceNumber = bill.InvoiceNumber.Trim(),
                    Amount = bill.Amount,
                    DueDate = bill.DueDate!.Value.Date,
                    LogCode = logCode,
                    DrCr = drCr == "DR" ? "Dr" : "Cr",

                    // Existing legacy defaults retained.
                    InvoiceType = "CB",
                    CurrencyCode = CurCode,
                    BranchId = 0
                };

                if (bill.Id.HasValue && bill.Id.Value > 0)
                {
                    using var updateCommand = new SqlCommand(
                        updateSql, connection, transaction);

                    updateCommand.Parameters.Add("@Id", SqlDbType.Int)
                        .Value = bill.Id.Value;

                    updateCommand.Parameters.Add("@AccountId", SqlDbType.Int)
                        .Value = accountId;

                    updateCommand.Parameters.Add("@InvoiceDate", SqlDbType.Date)
                        .Value = parameters.InvoiceDate;

                    updateCommand.Parameters.Add("@Narration", SqlDbType.NVarChar)
                        .Value = (object?)parameters.Narration ?? DBNull.Value;

                    updateCommand.Parameters.Add("@InvoiceNumber", SqlDbType.NVarChar)
                        .Value = parameters.InvoiceNumber;

                    var amountParameter = updateCommand.Parameters.Add(
                        "@Amount", SqlDbType.Decimal);

                    amountParameter.Precision = 18;
                    amountParameter.Scale = 4;
                    amountParameter.Value = parameters.Amount;

                    updateCommand.Parameters.Add("@DueDate", SqlDbType.Date)
                        .Value = parameters.DueDate;

                    updateCommand.Parameters.Add("@LogCode", SqlDbType.Int)
                        .Value = (object?)parameters.LogCode ?? DBNull.Value;

                    updateCommand.Parameters.Add("@DrCr", SqlDbType.NVarChar)
                        .Value = parameters.DrCr;

                    int affected = await updateCommand.ExecuteNonQueryAsync();

                    if (affected != 1)
                    {
                        throw new InvalidOperationException(
                            $"Pending bill ID {bill.Id} was not found for account {accountId}.");
                    }
                }
                else
                {
                    using var insertCommand = new SqlCommand(
                        insertSql, connection, transaction);

                    insertCommand.Parameters.Add("@InvoiceDate", SqlDbType.Date)
                        .Value = parameters.InvoiceDate;

                    insertCommand.Parameters.Add("@Narration", SqlDbType.NVarChar)
                        .Value = (object?)parameters.Narration ?? DBNull.Value;

                    insertCommand.Parameters.Add("@InvoiceNumber", SqlDbType.NVarChar)
                        .Value = parameters.InvoiceNumber;

                    var amountParameter = insertCommand.Parameters.Add(
                        "@Amount", SqlDbType.Decimal);

                    amountParameter.Precision = 18;
                    amountParameter.Scale = 4;
                    amountParameter.Value = parameters.Amount;

                    insertCommand.Parameters.Add("@DueDate", SqlDbType.Date)
                        .Value = parameters.DueDate;

                    insertCommand.Parameters.Add("@InvoiceType", SqlDbType.NVarChar)
                        .Value = parameters.InvoiceType;

                    insertCommand.Parameters.Add("@AccountId", SqlDbType.Int)
                        .Value = accountId;

                    insertCommand.Parameters.Add("@LogCode", SqlDbType.Int)
                        .Value = (object?)parameters.LogCode ?? DBNull.Value;

                    insertCommand.Parameters.Add("@CurrencyCode", SqlDbType.Int)
                        .Value = parameters.CurrencyCode;

                    insertCommand.Parameters.Add("@BranchId", SqlDbType.Int)
                        .Value = parameters.BranchId;

                    insertCommand.Parameters.Add("@DrCr", SqlDbType.NVarChar)
                        .Value = parameters.DrCr;

                    await insertCommand.ExecuteNonQueryAsync();
                }
            }
        }
        //  // =========================================================
        // SOFT DELETE CUSTOMER
        // =========================================================
        public async Task<bool> DeleteCustomer(int id, string logCd)
        {
            const string sql = @"UPDATE dbo.CUSMAS SET DELFLG = 1, ACTIVE = 0, LOGCD = @LogCd, UITIME = GETDATE() WHERE ID = @Id AND ISNULL(DELFLG, 0) = 0;";

            var connection = db.GetConnection();

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            command.Parameters.Add("@LogCd", SqlDbType.NVarChar, 255).Value = logCd;

            return await command.ExecuteNonQueryAsync() > 0;
        }

        //lookup

        private List<LookupItem> GetLookupList(
            string sql,
            params SqlParameter[] parameters)
        {
            var result = new List<LookupItem>();

            using var connection = new SqlConnection(db.ConnectionString);
            connection.Open();

            using var command = new SqlCommand(sql, connection);

            if (parameters != null && parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }

            using var reader = command.ExecuteReader();

            int idOrdinal = reader.GetOrdinal("Id");
            int nameOrdinal = reader.GetOrdinal("Name");

            while (reader.Read())
            {
                result.Add(new LookupItem
                {
                    Id = reader.IsDBNull(idOrdinal)
                        ? 0
                        : Convert.ToInt32(reader.GetValue(idOrdinal)),

                    Name = reader.IsDBNull(nameOrdinal)
                        ? ""
                        : Convert.ToString(reader.GetValue(nameOrdinal)) ?? ""
                });
            }

            return result;
        }

        public List<LookupItem> GetCustomerLedgerGroups()
        {
            const string sql = @"
        SELECT GROUPID AS Id,GROUPNAME AS Name FROM GRPLDG WHERE GACTIVE = 1 AND (GROUPID = 26 OR MGROUP = 26)ORDER BY GROUPNAME";
            return GetLookupList(sql);
        }

        public async Task<DataTable> GetCountries()
        {
            const string sql = @"
            SELECT COUNTRYID,COUNTRYNAME,VATNOLIMIT,VATCHARALLOW FROM dbo.COUNTRY WHERE ISNULL(ACTIVE, 1) = 1 ORDER BY COUNTRYNAME;";
            return await db.GetDatatable(sql);
        }


        public async Task<PaginatedResult<CountryDto>> GetCountries(int page,int pageSize)
        {
            const string countSql = @"
            SELECT COUNT(*)
            FROM dbo.COUNTRY
            WHERE ISNULL(ACTIVE, 1) = 1;";

                const string dataSql = @"
            SELECT COUNTRYID, COUNTRYNAME, VATNOLIMIT, VATCHARALLOW
            FROM dbo.COUNTRY
            WHERE ISNULL(ACTIVE, 1) = 1
            ORDER BY COUNTRYNAME, COUNTRYID
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;";

            int offset = (page - 1) * pageSize;

            int totalRecords =
                await db.QuerySingleOrDefaultAsync<int>(countSql);

            var rows = await db.QueryListAsync<CountryDto>(
                dataSql,
                new
                {
                    Offset = offset,
                    PageSize = pageSize
                });

            return new PaginatedResult<CountryDto>
            {
                Data = rows,
                TotalRecords = totalRecords
            };
        }



        // =========================================================
        // REGIONS
        // GET: api/v1/suppliers/lookups/regions?countryId=1
        // =========================================================
        public async Task<DataTable> GetRegions(int countryId)
        {
            string sql = $@"
             SELECT REGIONID,REGIONNAME,REGIONSECNAME FROM dbo.REGION WHERE COUNTRYID = {countryId} ORDER BY REGIONNAME;";
            return await db.GetDatatable(sql);
        }

        // --------------------------------------------------
        // CUSTOMER CURRENCIES
        // --------------------------------------------------
        public async Task<DataTable> GetCustomerCurrencies()
        {
            const string sql = @"
        SELECT CODE, SYMBOL, CURRSTRING FROM dbo.CURRENCYMAS ORDER BY CURRSTRING;";
            return await db.GetDatatable(sql);
        }

        public List<LookupItem> GetCustomerItemGroups()
        {
            const string sql = @"
        SELECT PRGRPID AS Id,GRPNAME AS Name FROM dbo.PRGRPMAS ORDER BY GRPNAME;";
            return GetLookupList(sql);
        }

        public List<DropdownOption> GetCustomerVatTypes()
        {
            return new List<DropdownOption>
    {
        new() { Id = "Standard VAT", Name = "Standard VAT" },
        new() { Id = "Multi-rate VAT", Name = "Multi-rate VAT" },
        new() { Id = "Zero-rated VAT", Name = "Zero-rated VAT" },
        new() { Id = "Reverse-charge VAT", Name = "Reverse-charge VAT" },
        new() { Id = "Differential VAT", Name = "Differential VAT" },
        new() { Id = "Destination-based VAT", Name = "Destination-based VAT" },
        new() { Id = "Small Business VAT", Name = "Small Business VAT" }
    };
        }

        public List<DropdownOption> GetCustomerEInvoiceIdTypes()
        {
            return new List<DropdownOption>
    {
        new() { Id = "CRN", Name = "CRN" },
        new() { Id = "MOM", Name = "MOM" },
        new() { Id = "MLS", Name = "MLS" },
        new() { Id = "700", Name = "700" },
        new() { Id = "SAG", Name = "SAG" },
        new() { Id = "OTH", Name = "OTH" },
        new() { Id = "NAT", Name = "NAT" },
        new() { Id = "IQA", Name = "IQA" },
        new() { Id = "PAS", Name = "PAS" },
        new() { Id = "GCC", Name = "GCC" }
    };
        }

        public List<LookupItem> GetCustomerPartyGroups()
        {
            const string sql = @"
        SELECT ID AS Id, GROUPNAME AS Name FROM PARTYGROUPMAS WHERE GROUPTYPE = @GroupType ORDER BY GROUPNAME;";
            return GetLookupList(
                sql,
                new SqlParameter("@GroupType", "C")
            );
        }

        public List<LookupItem> GetCustomerSalesmen()
        {
            const string sql = @"
        SELECT ID AS Id, NAME AS Name FROM SALESMANMAS WHERE ACTIVE = 1 ORDER BY NAME;";
            return GetLookupList(sql);
        }

        public List<LookupItem> GetSalesmen()
        {
            const string sql = @"
        SELECT SMCODE AS Id, SMNAME AS Name FROM dbo.SMMAST WHERE ACTIVE = 1 ORDER BY SMNAME;";
            return GetLookupList(sql);
        }

        public List<LookupItem> GetProductTemplates()
        {
            const string sql = @"
        SELECT ID AS Id, PRTNAME AS Name FROM PRTM ORDER BY PRTNAME;";

            return GetLookupList(sql);
        }

        public List<LookupItem> GetPriceCategories()
        {
            const string sql = @"
        SELECT DISTINCT CODE AS Id, CATNAME AS Name FROM PRICECATMAS ORDER BY CATNAME;";
            return GetLookupList(sql);
        }
        public List<LookupItem> GetPriceLists()
        {
            const string sql = @"
        SELECT ID AS Id,LISTNAME AS Name FROM PRLISTM WHERE DELFLG = 'FALSE'ORDER BY LISTNAME;";
            return GetLookupList(sql);
        }
        public List<LookupItem> GetPointSystems()
        {
            const string sql = @"
        SELECT ID AS Id, POINTSYSNAME AS Name FROM POINTSYS WHERE DELFLG = 'FALSE' ORDER BY POINTSYSNAME;";
            return GetLookupList(sql);
        }
    }
}



//public async Task<(List<clsCustomer> Data, int TotalRecords)>
//    GetCustomers(int page, int pageSize, string? search)
//{
//    page = Math.Max(1, page);
//    pageSize = Math.Clamp(pageSize, 1, 500);

//    var result = new List<clsCustomer>();
//    var connection = db.GetConnection();

//    if (connection.State != ConnectionState.Open)
//        await connection.OpenAsync();

//    const string filter = @"
//        ISNULL(DELFLG, 0) = 0
//        AND (
//            @Search = ''
//            OR CUSNAME LIKE '%' + @Search + '%'
//            OR ALIAS LIKE '%' + @Search + '%'
//            OR PRINTNAME LIKE '%' + @Search + '%'
//            OR PHONE LIKE '%' + @Search + '%'
//            OR EMAIL LIKE '%' + @Search + '%'
//        )";

//    int totalRecords;

//    using (var countCommand = new SqlCommand(
//        $"SELECT COUNT(1) FROM CUSMAS WHERE {filter}",
//        connection))
//    {
//        countCommand.Parameters.Add(
//            "@Search", SqlDbType.NVarChar, 200)
//            .Value = search?.Trim() ?? "";

//        totalRecords = Convert.ToInt32(
//            await countCommand.ExecuteScalarAsync());
//    }

//    var sql = $@"
//        SELECT *
//        FROM CUSMAS
//        WHERE {filter}
//        ORDER BY CUSNAME, ID
//        OFFSET @Offset ROWS
//        FETCH NEXT @PageSize ROWS ONLY;";

//    using (var command = new SqlCommand(sql, connection))
//    {
//        command.Parameters.Add(
//            "@Search", SqlDbType.NVarChar, 200)
//            .Value = search?.Trim() ?? "";

//        command.Parameters.Add(
//            "@Offset", SqlDbType.Int)
//            .Value = (page - 1) * pageSize;

//        command.Parameters.Add(
//            "@PageSize", SqlDbType.Int)
//            .Value = pageSize;

//        using var reader = await command.ExecuteReaderAsync();

//        while (await reader.ReadAsync())
//            result.Add(MapCustomer(reader));
//    }

//    return (result, totalRecords);
//}



//public async Task<(List<clsCustomer> Data, int TotalRecords)>
//    GetCustomers(int page, int pageSize, string? search)
//{
//    page = Math.Max(1, page);
//    pageSize = Math.Clamp(pageSize, 1, 500);

//    var result = new List<clsCustomer>();
//    var connection = db.GetConnection();

//    if (connection.State != ConnectionState.Open)
//        await connection.OpenAsync();

//    string searchValue = search?.Trim() ?? string.Empty;

//    const string filter = @"
//ISNULL(DELFLG, 0) = 0
//AND (
//    @Search = ''
//    OR CUSNAME LIKE '%' + @Search + '%'
//    OR ALIAS LIKE '%' + @Search + '%'
//    OR PRINTNAME LIKE '%' + @Search + '%'
//    OR PHONE LIKE '%' + @Search + '%'
//    OR EMAIL LIKE '%' + @Search + '%'
//)";

//    // 1. Count customers.
//    int totalRecords;

//    using (var countCommand = new SqlCommand(
//        $"SELECT COUNT(1) FROM dbo.CUSMAS WHERE {filter}",
//        connection))
//    {
//        countCommand.Parameters.Add(
//            "@Search", SqlDbType.NVarChar, 200).Value = searchValue;

//        totalRecords = Convert.ToInt32(
//            await countCommand.ExecuteScalarAsync());
//    }

//    if (totalRecords == 0)
//        return (result, 0);

//    // 2. Retrieve the requested page using ROW_NUMBER().
//    const string customerSql = @"
//;WITH CustomerPage AS
//(
//    SELECT
//        ID,
//        ROW_NUMBER() OVER (ORDER BY CUSNAME, ID) AS RowNum
//    FROM dbo.CUSMAS
//    WHERE ISNULL(DELFLG, 0) = 0
//      AND (
//          @Search = ''
//          OR CUSNAME LIKE '%' + @Search + '%'
//          OR ALIAS LIKE '%' + @Search + '%'
//          OR PRINTNAME LIKE '%' + @Search + '%'
//          OR PHONE LIKE '%' + @Search + '%'
//          OR EMAIL LIKE '%' + @Search + '%'
//      )
//)
//SELECT C.*
//FROM CustomerPage CP
//INNER JOIN dbo.CUSMAS C ON C.ID = CP.ID
//WHERE CP.RowNum BETWEEN @StartRow AND @EndRow
//ORDER BY CP.RowNum;";

//    using (var command = new SqlCommand(customerSql, connection))
//    {
//        command.Parameters.Add(
//            "@Search", SqlDbType.NVarChar, 200).Value = searchValue;

//        command.Parameters.Add(
//            "@StartRow", SqlDbType.Int).Value =
//            ((page - 1) * pageSize) + 1;

//        command.Parameters.Add(
//            "@EndRow", SqlDbType.Int).Value = page * pageSize;

//        using var reader = await command.ExecuteReaderAsync();

//        while (await reader.ReadAsync())
//            result.Add(MapCustomer(reader));
//    }

//    // Initialize empty lists and collect account IDs.
//    var accountIds = new List<int>();
//    var customerByAccount = new Dictionary<int, clsCustomer>();

//    foreach (var customer in result)
//    {
//        customer.OpeningBalances = new List<CustomerOpeningBalanceDto>();
//        customer.PendingBills = new List<CustomerPendingBillDto>();

//        // Change AccId to your actual clsCustomer property name if needed.
//        int accountId = customer.AccId;

//        if (accountId > 0)
//        {
//            accountIds.Add(accountId);

//            if (!customerByAccount.ContainsKey(accountId))
//                customerByAccount.Add(accountId, customer);
//        }
//    }

//    accountIds = accountIds.Distinct().ToList();

//    if (accountIds.Count == 0)
//        return (result, totalRecords);

//    // Build parameterized account ID list.
//    var parameterNames = accountIds
//        .Select((_, index) => $"@AccountId{index}")
//        .ToArray();

//    string inClause = string.Join(", ", parameterNames);

//    // 3. Retrieve opening balances in one query.
//    string openingSql = $@"
//SELECT ID, ACCID, DEBIT, CREDIT, CURCODE, CURCONVRATE, BRCD
//FROM dbo.OPN
//WHERE ACCID IN ({inClause})
//ORDER BY ACCID, ID;";

//    using (var command = new SqlCommand(openingSql, connection))
//    {
//        for (int i = 0; i < accountIds.Count; i++)
//        {
//            command.Parameters.Add(
//                parameterNames[i], SqlDbType.Int).Value = accountIds[i];
//        }

//        using var reader = await command.ExecuteReaderAsync();

//        while (await reader.ReadAsync())
//        {
//            int accountId = Convert.ToInt32(reader["ACCID"]);

//            if (!customerByAccount.TryGetValue(accountId, out var customer))
//                continue;

//            customer.OpeningBalances.Add(new CustomerOpeningBalanceDto
//            {
//                Id = Convert.ToInt32(reader["ID"]),
//                Debit = reader["DEBIT"] == DBNull.Value
//                    ? 0m : Convert.ToDecimal(reader["DEBIT"]),
//                Credit = reader["CREDIT"] == DBNull.Value
//                    ? 0m : Convert.ToDecimal(reader["CREDIT"]),
//                CurrencyCode = reader["CURCODE"] == DBNull.Value
//                    ? null : Convert.ToInt32(reader["CURCODE"]),
//                ConversionRate = reader["CURCONVRATE"] == DBNull.Value
//                    ? null : Convert.ToDecimal(reader["CURCONVRATE"]),
//                BranchId = reader["BRCD"] == DBNull.Value
//                    ? null : Convert.ToInt32(reader["BRCD"])
//            });
//        }
//    }

//    // 4. Retrieve pending bills in one query.
//    string pendingBillsSql = $@"
//SELECT ID, CODE, INVDATE, NARRATION, INVNO, AMOUNT,
//       DUEDATE, DRCR, CURCODE, CURCONVRATE, BRCD
//FROM dbo.PENDBIL
//WHERE CODE IN ({inClause})
//ORDER BY CODE, INVDATE, ID;";

//    using (var command = new SqlCommand(pendingBillsSql, connection))
//    {
//        for (int i = 0; i < accountIds.Count; i++)
//        {
//            command.Parameters.Add(
//                parameterNames[i], SqlDbType.Int).Value = accountIds[i];
//        }

//        using var reader = await command.ExecuteReaderAsync();

//        while (await reader.ReadAsync())
//        {
//            int accountId = Convert.ToInt32(reader["CODE"]);

//            if (!customerByAccount.TryGetValue(accountId, out var customer))
//                continue;

//            customer.PendingBills.Add(new CustomerPendingBillDto
//            {
//                Id = Convert.ToInt32(reader["ID"]),
//                InvoiceDate = reader["INVDATE"] == DBNull.Value
//                    ? null : Convert.ToDateTime(reader["INVDATE"]),
//                Narration = reader["NARRATION"] == DBNull.Value
//                    ? null : Convert.ToString(reader["NARRATION"]),
//                InvoiceNumber = reader["INVNO"] == DBNull.Value
//                    ? null : Convert.ToString(reader["INVNO"]),
//                Amount = reader["AMOUNT"] == DBNull.Value
//                    ? 0m : Convert.ToDecimal(reader["AMOUNT"]),
//                DueDate = reader["DUEDATE"] == DBNull.Value
//                    ? null : Convert.ToDateTime(reader["DUEDATE"]),
//                DrCr = reader["DRCR"] == DBNull.Value
//                    ? null : Convert.ToString(reader["DRCR"]),
//                CurrencyCode = reader["CURCODE"] == DBNull.Value
//                    ? null : Convert.ToInt32(reader["CURCODE"]),
//                ConversionRate = reader["CURCONVRATE"] == DBNull.Value
//                    ? null : Convert.ToDecimal(reader["CURCONVRATE"]),
//                BranchId = reader["BRCD"] == DBNull.Value
//                    ? null : Convert.ToInt32(reader["BRCD"])
//            });
//        }
//    }

//    return (result, totalRecords);
//}



//public async Task<(List<clsCustomer> Data, int TotalRecords)>GetCustomers(int page, int pageSize, string? search)
//{
//    page = Math.Max(1, page);
//    pageSize = Math.Clamp(pageSize, 1, 500);

//    var result = new List<clsCustomer>();
//    using var connection = db.GetConnection();

//    if (connection.State != ConnectionState.Open)
//        await connection.OpenAsync();

//    string searchValue = search?.Trim() ?? string.Empty;

//    const string filter = @"
//    ISNULL(DELFLG, 0) = 0 AND ACTIVE=1
//    AND (
//        @Search = ''
//        OR CUSNAME LIKE '%' + @Search + '%'
//        OR ALIAS LIKE '%' + @Search + '%'
//        OR PRINTNAME LIKE '%' + @Search + '%'
//        OR PHONE LIKE '%' + @Search + '%'
//        OR EMAIL LIKE '%' + @Search + '%'
//    )";

//    // 1. Count customers.
//    int totalRecords;

//    using (var countCommand = new SqlCommand(
//        $"SELECT COUNT(1) FROM dbo.CUSMAS WHERE {filter}",
//        connection))
//    {
//        countCommand.Parameters.Add(
//            "@Search", SqlDbType.NVarChar, 200).Value = searchValue;

//        totalRecords = Convert.ToInt32(
//            await countCommand.ExecuteScalarAsync());
//    }

//    if (totalRecords == 0)
//        return (result, 0);

//    // 2. Retrieve the requested customer page.
//    const string customerSql = @"
//    ;WITH CustomerPage AS
//    (
//        SELECT
//            ID,
//            ROW_NUMBER() OVER (ORDER BY CUSNAME, ID) AS RowNum
//        FROM dbo.CUSMAS
//        WHERE ACTIVE=1 AND ISNULL(DELFLG, 0) = 0
//          AND (
//              @Search = ''
//              OR CUSNAME LIKE '%' + @Search + '%'
//              OR ALIAS LIKE '%' + @Search + '%'
//              OR PRINTNAME LIKE '%' + @Search + '%'
//              OR PHONE LIKE '%' + @Search + '%'
//              OR EMAIL LIKE '%' + @Search + '%'
//          )
//    )
//    SELECT C.*
//    FROM CustomerPage CP
//    INNER JOIN dbo.CUSMAS C ON C.ID = CP.ID

//    WHERE CP.RowNum BETWEEN @StartRow AND @EndRow
//    ORDER BY CP.RowNum;";

//    using (var command = new SqlCommand(customerSql, connection))
//    {
//        command.Parameters.Add(
//            "@Search", SqlDbType.NVarChar, 200).Value = searchValue;

//        command.Parameters.Add(
//            "@StartRow", SqlDbType.Int).Value =
//            ((page - 1) * pageSize) + 1;

//        command.Parameters.Add(
//            "@EndRow", SqlDbType.Int).Value = page * pageSize;

//        using var reader = await command.ExecuteReaderAsync();

//        while (await reader.ReadAsync())
//        {
//            var customer = MapCustomer(reader);

//            // Opening balance is now represented by scalar properties.
//            customer.OpenBal = 0m;
//            customer.CrDr = "DR";
//            customer.CurCode = 0;
//            customer.CurConvRate = 1m;
//            customer.PendingBills = new List<CustomerPendingBillDto>();

//            result.Add(customer);
//        }
//    }

//    // 3. Collect linked account IDs.
//    var accountIds = result
//        .Where(c => c.AccId > 0)
//        .Select(c => c.AccId)
//        .Distinct()
//        .ToList();

//    if (accountIds.Count == 0)
//        return (result, totalRecords);

//    var customerByAccount = result
//        .Where(c => c.AccId > 0)
//        .GroupBy(c => c.AccId)
//        .ToDictionary(g => g.Key, g => g.First());

//    var parameterNames = accountIds
//        .Select((_, index) => $"@AccountId{index}")
//        .ToArray();

//    string inClause = string.Join(", ", parameterNames);

//    // 4. Retrieve opening balances and map them to scalar properties.
//    //
//    // This assumes OPN rows for an account belong to the same currency
//    // and branch. If not, filter/group by the appropriate currency/branch.
//    string openingSql = $@"
//    SELECT
//        ACCID,
//        SUM(ISNULL(DEBIT, 0)) AS TotalDebit,
//        SUM(ISNULL(CREDIT, 0)) AS TotalCredit,
//        MAX(CURCODE) AS CURCODE,
//        MAX(CURCONVRATE) AS CURCONVRATE
//    FROM dbo.OPN
//    WHERE ACCID IN ({inClause})
//    GROUP BY ACCID;";

//    using (var command = new SqlCommand(openingSql, connection))
//    {
//        for (int i = 0; i < accountIds.Count; i++)
//        {
//            command.Parameters.Add(
//                parameterNames[i], SqlDbType.Int).Value = accountIds[i];
//        }

//        using var reader = await command.ExecuteReaderAsync();

//        while (await reader.ReadAsync())
//        {
//            int accountId = Convert.ToInt32(reader["ACCID"]);

//            if (!customerByAccount.TryGetValue(accountId, out var customer))
//                continue;

//            decimal debit = Convert.ToDecimal(reader["TotalDebit"]);
//            decimal credit = Convert.ToDecimal(reader["TotalCredit"]);

//            customer.OpenBal = Math.Abs(debit - credit);

//            customer.CrDr = debit > credit
//                ? "DR"
//                : credit > debit
//                    ? "CR"
//                    : "DR";

//            customer.CurCode = reader["CURCODE"] == DBNull.Value
//                ? 0
//                : Convert.ToInt32(reader["CURCODE"]);

//            customer.CurConvRate = reader["CURCONVRATE"] == DBNull.Value
//                ? 1m
//                : Convert.ToDecimal(reader["CURCONVRATE"]);
//        }
//    }

//    // 5. Retrieve pending bills.
//    string pendingBillsSql = $@"
//    SELECT
//        ID,
//        CODE,
//        INVDATE,
//        NARRATION,
//        INVNO,
//        AMOUNT,
//        DUEDATE,
//        DRCR,
//        CURCODE,
//        CURCONVRATE,
//        BRCD
//    FROM dbo.PENDBIL
//    WHERE CODE IN ({inClause})
//    ORDER BY CODE, INVDATE, ID;";

//    using (var command = new SqlCommand(pendingBillsSql, connection))
//    {
//        for (int i = 0; i < accountIds.Count; i++)
//        {
//            command.Parameters.Add(
//                parameterNames[i], SqlDbType.Int).Value = accountIds[i];
//        }

//        using var reader = await command.ExecuteReaderAsync();

//        while (await reader.ReadAsync())
//        {
//            int accountId = Convert.ToInt32(reader["CODE"]);

//            if (!customerByAccount.TryGetValue(accountId, out var customer))
//                continue;

//            customer.PendingBills.Add(new CustomerPendingBillDto
//            {
//                Id = Convert.ToInt32(reader["ID"]),

//                InvoiceDate = reader["INVDATE"] == DBNull.Value
//                    ? null
//                    : Convert.ToDateTime(reader["INVDATE"]),

//                Narration = reader["NARRATION"] == DBNull.Value
//                    ? null
//                    : Convert.ToString(reader["NARRATION"]),

//                InvoiceNumber = reader["INVNO"] == DBNull.Value
//                    ? null
//                    : Convert.ToString(reader["INVNO"]),

//                Amount = reader["AMOUNT"] == DBNull.Value
//                    ? 0m
//                    : Convert.ToDecimal(reader["AMOUNT"]),

//                DueDate = reader["DUEDATE"] == DBNull.Value
//                    ? null
//                    : Convert.ToDateTime(reader["DUEDATE"]),

//                DrCr = reader["DRCR"] == DBNull.Value
//                    ? null
//                    : Convert.ToString(reader["DRCR"]),

//                CurrencyCode = reader["CURCODE"] == DBNull.Value
//                    ? null
//                    : Convert.ToInt32(reader["CURCODE"]),

//                ConversionRate = reader["CURCONVRATE"] == DBNull.Value
//                    ? null
//                    : Convert.ToDecimal(reader["CURCONVRATE"]),

//                BranchId = reader["BRCD"] == DBNull.Value
//                    ? null
//                    : Convert.ToInt32(reader["BRCD"])
//            });
//        }
//    }

//    // 5. Load ledger group IDs from ACCMAS.
//    string ledgerGroupSql = $@"
//    SELECT ID, MGROUP,ACCODE,SWIFTCODE
//    FROM dbo.ACCMAS
//    WHERE ISACTIVE=1 AND ID IN ({inClause});";

//    using (var command = new SqlCommand(ledgerGroupSql, connection))
//    {
//        for (int i = 0; i < accountIds.Count; i++)
//        {
//            command.Parameters.Add(
//                parameterNames[i], SqlDbType.Int).Value = accountIds[i];
//        }

//        using var reader = await command.ExecuteReaderAsync();

//        while (await reader.ReadAsync())
//        {
//            int accountId = Convert.ToInt32(reader["ID"]);

//            if (customerByAccount.TryGetValue(accountId, out var customer))
//            {
//                customer.LedgerGroupId =
//                    reader["MGROUP"] == DBNull.Value
//                        ? 0
//                        : Convert.ToInt32(reader["MGROUP"]);
//                customer.AccountCode =Convert.ToString(reader["ACCODE"]);

//                customer.SwiftCode = reader["SWIFTCODE"] == DBNull.Value
//                ? null
//                : Convert.ToString(reader["SWIFTCODE"]);
//            }


//        }
//    }

//    return (result, totalRecords);
//}




// =========================================================
// GET CUSTOMER BY ID
// =========================================================

//public async Task<clsCustomer?> GetCustomerById(int id)
//{
//    const string sql = @"
//        SELECT *
//        FROM CUSMAS
//        WHERE ID = @Id
//          AND ISNULL(DELFLG, 0) = 0;";

//    var connection = db.GetConnection();

//    if (connection.State != ConnectionState.Open)
//        await connection.OpenAsync();

//    using var command = new SqlCommand(sql, connection);

//    command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

//    using var reader = await command.ExecuteReaderAsync();

//    return await reader.ReadAsync()
//        ? MapCustomer(reader)
//        : null;
//}




//public async Task<PaginatedDataTableResult> GetCustomerCurrencies()
//{
//    //int page,int pageSize
//    const string countSql = @"
//    SELECT COUNT(*)
//    FROM dbo.CURRENCYMAS;";

//        const string dataSql = @"
//    SELECT CODE, SYMBOL, CURRSTRING
//    FROM dbo.CURRENCYMAS
//    ORDER BY CURRSTRING, CODE;";
//    //OFFSET @Offset ROWS
//    //FETCH NEXT @PageSize ROWS ONLY;";

//    //int offset = (page - 1) * pageSize;

//    //int totalRecords =
//    //    await db.QuerySingleOrDefaultAsync<int>(countSql);

//    var rows = await db.QueryListAsync<CurrencyDto>(
//        dataSql);
//        //new
//        //{
//        //    Offset = offset,
//        //    PageSize = pageSize
//        //});

//    //var data = new DataTable();
//    //data.Columns.Add("CODE", typeof(int));    
//    //data.Columns.Add("SYMBOL", typeof(string));
//    //data.Columns.Add("CURRSTRING", typeof(string));

//    //foreach (var row in rows)
//    //{
//    //    data.Rows.Add(
//    //        row.CODE,
//    //        row.SYMBOL ?? (object)DBNull.Value,
//    //        row.CURRSTRING ?? (object)DBNull.Value
//    //    );
//    //}

//    return new PaginatedDataTableResult
//    {
//        Data = rows,
//        //TotalRecords = totalRecords
//    };
//}

// --------------------------------------------------
// CUSTOMER ITEM GROUPS
// --------------------------------------------------

//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Data.SqlClient;
//using QuickBalance.API.CLS;
//using QuickBalance.API.DTOs;
//using System.Data;

//namespace QuickBalance.API.DB
//{
//    public class CustomerDB
//    {
//        private readonly DBConn db;

//        public CustomerDB(DBConn db)
//        {
//            this.db = db;
//        }

//        private static readonly (string Property, string Column)[] Fields =
//        {
//            (nameof(clsCustomer.Alias), "ALIAS"),
//            (nameof(clsCustomer.CusName), "CUSNAME"),
//            (nameof(clsCustomer.Address), "ADDRESS"),
//            (nameof(clsCustomer.Phone), "PHONE"),
//            (nameof(clsCustomer.Email), "EMAIL"),
//            (nameof(clsCustomer.Active), "ACTIVE"),
//            (nameof(clsCustomer.LogCd), "LOGCD"),
//            (nameof(clsCustomer.AccId), "ACCID"),
//            (nameof(clsCustomer.Disc), "DISC"),
//            (nameof(clsCustomer.StateCode), "STATECODE"),
//            (nameof(clsCustomer.PrintName), "PRINTNAME"),
//            (nameof(clsCustomer.CountryCode), "COUNTRYCODE"),
//            (nameof(clsCustomer.PriceLevel), "PRICELEVEL"),
//            (nameof(clsCustomer.BnkAccName), "BNKACCNAME"),
//            (nameof(clsCustomer.BnkAccNo), "BNKACCNO"),
//            (nameof(clsCustomer.Branch), "BRANCH"),
//            (nameof(clsCustomer.IbanNo), "IBANNO"),
//            (nameof(clsCustomer.VatFlag), "VATFLAG"),
//            (nameof(clsCustomer.BillFlag), "BILLFLAG"),
//            (nameof(clsCustomer.CrPerFlag), "CRPERFLAG"),
//            (nameof(clsCustomer.CrLimitFlag), "CRLIMITFLAG"),
//            (nameof(clsCustomer.CrLimit), "CRLIMIT"),
//            (nameof(clsCustomer.UiTime), "UITIME"),
//            (nameof(clsCustomer.VatRegNo), "VATREGNO"),
//            (nameof(clsCustomer.ShipAddress), "SHIPADDRESS"),
//            (nameof(clsCustomer.SecName), "SECNAME"),
//            (nameof(clsCustomer.CurFlg), "CURFLG"),
//            (nameof(clsCustomer.SalesMan), "SALESMAN"),
//            (nameof(clsCustomer.CrPeriod), "CRPERIOD"),
//            (nameof(clsCustomer.GrpItem), "GRPITEM"),
//            (nameof(clsCustomer.ItemGroupCD), "ITEMGRPCD"),
//            (nameof(clsCustomer.GccFlg), "GCCFLG"),
//            (nameof(clsCustomer.PrTmpCode), "PRTMPCODE"),
//            (nameof(clsCustomer.PntSysId), "PNTSYSID"),
//            (nameof(clsCustomer.StreetOne), "STREETONE"),
//            (nameof(clsCustomer.StreetTwo), "STREETTWO"),
//            (nameof(clsCustomer.BuildingNo), "BULDINGNO"),
//            (nameof(clsCustomer.PlotId), "PLOTID"),
//            (nameof(clsCustomer.CityName), "CITYNAME"),
//            (nameof(clsCustomer.PostalCode), "POSTALCODE"),
//            (nameof(clsCustomer.District), "DISTRICT"),
//            (nameof(clsCustomer.GrpVatNo), "GRPVATNO"),
//            (nameof(clsCustomer.EinvIdType), "EINVIDTYPE"),
//            (nameof(clsCustomer.EinvId), "EINVID"),
//            (nameof(clsCustomer.VatType), "VATTYPE"),
//            (nameof(clsCustomer.PrListCd), "PRLISTCD"),
//            (nameof(clsCustomer.PrCatCd), "CPRCATCD"),
//            (nameof(clsCustomer.PartyGrpId), "PARTYGRPID"),
//            (nameof(clsCustomer.CityNameSec), "CITYNAMESEC"),
//            (nameof(clsCustomer.DistrictSec), "DISTRICTSEC"),
//            (nameof(clsCustomer.AprvLst), "APRVLST")
//        };

//        private static object DbValue(object? value)
//        {
//            return value ?? DBNull.Value;
//        }

//        private static void AddCustomerParameters(
//            SqlCommand command,
//            clsCustomer customer)
//        {
//            var type = typeof(clsCustomer);

//            foreach (var field in Fields)
//            {
//                var property = type.GetProperty(field.Property)!;

//                command.Parameters.AddWithValue(
//                    "@" + field.Column,
//                    DbValue(property.GetValue(customer)));
//            }
//        }

//        private static clsCustomer MapCustomer(SqlDataReader reader)
//        {
//            var customer = new clsCustomer
//            {
//                Id = Convert.ToInt32(reader["ID"])
//            };

//            foreach (var field in Fields)
//            {
//                var property = typeof(clsCustomer).GetProperty(field.Property)!;
//                var value = reader[field.Column];

//                if (value == DBNull.Value)
//                    continue;

//                var targetType =
//                    Nullable.GetUnderlyingType(property.PropertyType)
//                    ?? property.PropertyType;

//                try
//                {
//                    property.SetValue(
//                        customer,
//                        Convert.ChangeType(value, targetType));
//                }
//                catch (Exception ex)
//                {
//                    throw new InvalidOperationException(
//                        $"Mapping error: Column '{field.Column}', " +
//                        $"Property '{field.Property}', " +
//                        $"Value '{value}', Target type '{targetType.Name}'.",
//                        ex);
//                }
//            }

//            customer.DelFlg =
//                reader["DELFLG"] != DBNull.Value &&
//                Convert.ToBoolean(reader["DELFLG"]);

//            return customer;
//        }
//        // =========================================================
//        // GET CUSTOMERS WITH PAGINATION AND SEARCH
//        // =========================================================

//        public async Task<(List<clsCustomer> Data, int TotalRecords)>
//            GetCustomers(int page, int pageSize, string? search)
//        {
//            page = Math.Max(1, page);
//            pageSize = Math.Clamp(pageSize, 1, 500);

//            var result = new List<clsCustomer>();
//            var connection = db.GetConnection();

//            if (connection.State != ConnectionState.Open)
//                await connection.OpenAsync();

//            const string filter = @"
//                ISNULL(DELFLG, 0) = 0
//                AND (
//                    @Search = ''
//                    OR CUSNAME LIKE '%' + @Search + '%'
//                    OR ALIAS LIKE '%' + @Search + '%'
//                    OR PRINTNAME LIKE '%' + @Search + '%'
//                    OR PHONE LIKE '%' + @Search + '%'
//                    OR EMAIL LIKE '%' + @Search + '%'
//                )";

//            int totalRecords;

//            using (var countCommand = new SqlCommand(
//                $"SELECT COUNT(1) FROM CUSMAS WHERE {filter}",
//                connection))
//            {
//                countCommand.Parameters.Add(
//                    "@Search", SqlDbType.NVarChar, 200)
//                    .Value = search?.Trim() ?? "";

//                totalRecords = Convert.ToInt32(
//                    await countCommand.ExecuteScalarAsync());
//            }

//            var sql = $@"
//                SELECT *
//                FROM CUSMAS
//                WHERE {filter}
//                ORDER BY CUSNAME, ID
//                OFFSET @Offset ROWS
//                FETCH NEXT @PageSize ROWS ONLY;";

//            using (var command = new SqlCommand(sql, connection))
//            {
//                command.Parameters.Add(
//                    "@Search", SqlDbType.NVarChar, 200)
//                    .Value = search?.Trim() ?? "";

//                command.Parameters.Add(
//                    "@Offset", SqlDbType.Int)
//                    .Value = (page - 1) * pageSize;

//                command.Parameters.Add(
//                    "@PageSize", SqlDbType.Int)
//                    .Value = pageSize;

//                using var reader = await command.ExecuteReaderAsync();

//                while (await reader.ReadAsync())
//                    result.Add(MapCustomer(reader));
//            }

//            return (result, totalRecords);
//        }

//        // =========================================================
//        // GET CUSTOMER BY ID
//        // =========================================================

//        public async Task<clsCustomer?> GetCustomerById(int id)
//        {
//            const string sql = @"
//                SELECT *
//                FROM CUSMAS
//                WHERE ID = @Id
//                  AND ISNULL(DELFLG, 0) = 0;";

//            var connection = db.GetConnection();

//            if (connection.State != ConnectionState.Open)
//                await connection.OpenAsync();

//            using var command = new SqlCommand(sql, connection);

//            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

//            using var reader = await command.ExecuteReaderAsync();

//            return await reader.ReadAsync()
//                ? MapCustomer(reader)
//                : null;
//        }

//        // =========================================================
//        // CHECK DUPLICATE CUSTOMER NAME
//        // =========================================================
//        public async Task<bool> CustomerNameExists(
//            string customerName,
//            int? excludeId = null)
//        {
//            const string sql = @"
//                SELECT COUNT(1)
//                FROM CUSMAS
//                WHERE UPPER(LTRIM(RTRIM(CUSNAME))) =
//                      UPPER(LTRIM(RTRIM(@CustomerName)))
//                  AND ISNULL(DELFLG, 0) = 0
//                  AND (@ExcludeId IS NULL OR ID <> @ExcludeId);";

//            var connection = db.GetConnection();

//            if (connection.State != ConnectionState.Open)
//                await connection.OpenAsync();

//            using var command = new SqlCommand(sql, connection);

//            command.Parameters.Add(
//                "@CustomerName", SqlDbType.NVarChar, 200)
//                .Value = customerName.Trim();

//            command.Parameters.Add(
//                "@ExcludeId", SqlDbType.Int)
//                .Value = (object?)excludeId ?? DBNull.Value;

//            return Convert.ToInt32(
//                await command.ExecuteScalarAsync()) > 0;
//        }

//        // =========================================================
//        // INSERT CUSTOMER
//        // =========================================================
//        public async Task<int> InsertCustomer(clsCustomer customer)
//        {
//            var connection = db.GetConnection();

//            if (connection.State != ConnectionState.Open)
//                await connection.OpenAsync();

//            var columns = string.Join(
//                ", ",
//                Fields.Select(f => f.Column)
//                    .Concat(new[] { "DELFLG" }));

//            var values = string.Join(
//                ", ",
//                Fields.Select(f => "@" + f.Column)
//                    .Concat(new[] { "0" }));

//            var sql = $@"
//                INSERT INTO CUSMAS ({columns})
//                OUTPUT INSERTED.ID
//                VALUES ({values});";

//            using var command = new SqlCommand(sql, connection);

//            AddCustomerParameters(command, customer);

//            var result = await command.ExecuteScalarAsync();

//            return result == null || result == DBNull.Value
//                ? 0
//                : Convert.ToInt32(result);
//        }

//        // =========================================================
//        // UPDATE CUSTOMER
//        // =========================================================

//        public async Task<bool> UpdateCustomer(
//            int id,
//            clsCustomer customer)
//        {
//            var connection = db.GetConnection();

//            if (connection.State != ConnectionState.Open)
//                await connection.OpenAsync();

//            var setClause = string.Join(
//                ", ",
//                Fields
//                    .Where(f => f.Column != "ACCID")
//                    .Select(f => $"{f.Column} = @{f.Column}"));

//            var sql = $@"
//                UPDATE CUSMAS
//                SET {setClause}
//                WHERE ID = @Id
//                  AND ISNULL(DELFLG, 0) = 0;";

//            using var command = new SqlCommand(sql, connection);

//            AddCustomerParameters(command, customer);

//            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

//            return await command.ExecuteNonQueryAsync() > 0;
//        }

//        // =========================================================
//        // SOFT DELETE CUSTOMER
//        // =========================================================
//        public async Task<bool> DeleteCustomer(int id)
//        {
//            const string sql = @"UPDATE CUSMAS SET DELFLG = 1,ACTIVE = 0 WHERE ID = @Id AND ISNULL(DELFLG, 0) = 0;";

//            var connection = db.GetConnection();

//            if (connection.State != ConnectionState.Open)
//                await connection.OpenAsync();

//            using var command = new SqlCommand(sql, connection);

//            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

//            return await command.ExecuteNonQueryAsync() > 0;
//        }

//        //lookup

//        private List<LookupItem> GetLookupList(
//            string sql,
//            params SqlParameter[] parameters)
//        {
//            var result = new List<LookupItem>();

//            using var connection = new SqlConnection(db.ConnectionString);
//            connection.Open();

//            using var command = new SqlCommand(sql, connection);

//            if (parameters != null && parameters.Length > 0)
//            {
//                command.Parameters.AddRange(parameters);
//            }

//            using var reader = command.ExecuteReader();

//            int idOrdinal = reader.GetOrdinal("Id");
//            int nameOrdinal = reader.GetOrdinal("Name");

//            while (reader.Read())
//            {
//                result.Add(new LookupItem
//                {
//                    Id = reader.IsDBNull(idOrdinal)
//                        ? 0
//                        : Convert.ToInt32(reader.GetValue(idOrdinal)),

//                    Name = reader.IsDBNull(nameOrdinal)
//                        ? ""
//                        : Convert.ToString(reader.GetValue(nameOrdinal)) ?? ""
//                });
//            }

//            return result;
//        }

//        public List<LookupItem> GetCustomerLedgerGroups()
//        {
//            const string sql = @"
//        SELECT GROUPID AS Id,GROUPNAME AS Name FROM GRPLDG WHERE GACTIVE = 1 AND (GROUPID = 26 OR MGROUP = 26)ORDER BY GROUPNAME";
//            return GetLookupList(sql);
//        }

//        public async Task<DataTable> GetCountries()
//        {
//            const string sql = @"
//            SELECT COUNTRYID,COUNTRYNAME,VATNOLIMIT,VATCHARALLOW FROM dbo.COUNTRY WHERE ISNULL(ACTIVE, 1) = 1 ORDER BY COUNTRYNAME;";
//            return await db.GetDatatable(sql);
//        }



//        // =========================================================
//        // REGIONS
//        // GET: api/v1/suppliers/lookups/regions?countryId=1
//        // =========================================================
//        public async Task<DataTable> GetRegions(int countryId)
//        {
//            string sql = $@"
//        SELECT REGIONID,REGIONNAME FROM dbo.REGION WHERE COUNTRYID = {countryId} ORDER BY REGIONNAME;";
//            return await db.GetDatatable(sql);
//        }

//        // --------------------------------------------------
//        // CUSTOMER CURRENCIES
//        // --------------------------------------------------
//        public async Task<DataTable> GetCustomerCurrencies()
//        {
//            const string sql = @"
//        SELECT CODE, SYMBOL, CURRSTRING FROM dbo.CURRENCYMAS ORDER BY CURRSTRING;";
//            return await db.GetDatatable(sql);
//        }

//        // --------------------------------------------------
//        // CUSTOMER ITEM GROUPS
//        // --------------------------------------------------
//        public List<LookupItem> GetCustomerItemGroups()
//        {
//            const string sql = @"
//        SELECT PRGRPID AS Id,GRPNAME AS Name FROM dbo.PRGRPMAS ORDER BY GRPNAME;";
//            return GetLookupList(sql);
//        }

//        public List<DropdownOption> GetCustomerVatTypes()
//        {
//            return new List<DropdownOption>
//    {
//        new() { Id = "Standard VAT", Name = "Standard VAT" },
//        new() { Id = "Multi-rate VAT", Name = "Multi-rate VAT" },
//        new() { Id = "Zero-rated VAT", Name = "Zero-rated VAT" },
//        new() { Id = "Reverse-charge VAT", Name = "Reverse-charge VAT" },
//        new() { Id = "Differential VAT", Name = "Differential VAT" },
//        new() { Id = "Destination-based VAT", Name = "Destination-based VAT" },
//        new() { Id = "Small Business VAT", Name = "Small Business VAT" }
//    };
//        }

//        public List<DropdownOption> GetCustomerEInvoiceIdTypes()
//        {
//            return new List<DropdownOption>
//    {
//        new() { Id = "CRN", Name = "CRN" },
//        new() { Id = "MOM", Name = "MOM" },
//        new() { Id = "MLS", Name = "MLS" },
//        new() { Id = "700", Name = "700" },
//        new() { Id = "SAG", Name = "SAG" },
//        new() { Id = "OTH", Name = "OTH" },
//        new() { Id = "NAT", Name = "NAT" },
//        new() { Id = "IQA", Name = "IQA" },
//        new() { Id = "PAS", Name = "PAS" },
//        new() { Id = "GCC", Name = "GCC" }
//    };
//        }

//        public List<LookupItem> GetCustomerPartyGroups()
//        {
//            const string sql = @"
//        SELECT ID AS Id, GROUPNAME AS Name FROM PARTYGROUPMAS WHERE GROUPTYPE = @GroupType ORDER BY GROUPNAME;";
//            return GetLookupList(
//                sql,
//                new SqlParameter("@GroupType", "C")
//            );
//        }

//        public List<LookupItem> GetCustomerSalesmen()
//        {
//            const string sql = @"
//        SELECT ID AS Id, NAME AS Name FROM SALESMANMAS WHERE ACTIVE = 1 ORDER BY NAME;";
//            return GetLookupList(sql);
//        }

//        public List<LookupItem> GetSalesmen()
//        {
//            const string sql = @"
//        SELECT SMCODE AS Id, SMNAME AS Name FROM dbo.SMMAST WHERE ACTIVE = 1 ORDER BY SMNAME;";
//            return GetLookupList(sql);
//        }

//        public List<LookupItem> GetProductTemplates()
//        {
//            const string sql = @"
//        SELECT ID AS Id, PRTNAME AS Name FROM PRTM ORDER BY PRTNAME;";

//            return GetLookupList(sql);
//        }

//        public List<LookupItem> GetPriceCategories()
//        {
//            const string sql = @"
//        SELECT DISTINCT CODE AS Id, CATNAME AS Name FROM PRICECATMAS ORDER BY CATNAME;";
//            return GetLookupList(sql);
//        }
//        public List<LookupItem> GetPriceLists()
//        {
//            const string sql = @"
//        SELECT ID AS Id,LISTNAME AS Name FROM PRLISTM WHERE DELFLG = 'FALSE'ORDER BY LISTNAME;";
//            return GetLookupList(sql);
//        }
//        public List<LookupItem> GetPointSystems()
//        {
//            const string sql = @"
//        SELECT ID AS Id, POINTSYSNAME AS Name FROM POINTSYS WHERE DELFLG = 'FALSE' ORDER BY POINTSYSNAME;";
//            return GetLookupList(sql);
//        }
//    }
//}