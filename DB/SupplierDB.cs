using Dapper;
using Microsoft.Data.SqlClient;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Respons_DTO;
using SkiaSharp;
using System.Data;

namespace QuickBalance.API.DB
{
    public class SupplierDB
    {
        private readonly DBConn db;

        public SupplierDB(DBConn db)
        {
            this.db = db;
        }

        // Maps C# property names to SUPMAS column names.
        private static readonly (string Property, string Column)[] Fields =
        {
            (nameof(clsSupplier.Alias), "ALIAS"),
            (nameof(clsSupplier.SupName), "SUPNAME"),
            (nameof(clsSupplier.Address), "ADDRESS"),
            (nameof(clsSupplier.Phone), "PHONE"),
            (nameof(clsSupplier.Email), "EMAIL"),
            (nameof(clsSupplier.Active), "ACTIVE"),
            (nameof(clsSupplier.LogId), "LOGCD"),
            (nameof(clsSupplier.AccountId), "ACCID"),
            (nameof(clsSupplier.Disc), "DISC"),
            (nameof(clsSupplier.StateCode), "STATECODE"),
            (nameof(clsSupplier.PrintName), "PRINTNAME"),
            (nameof(clsSupplier.CountryCode), "COUNTRYCODE"),
            (nameof(clsSupplier.PriceLevel), "PRICELEVEL"),
            (nameof(clsSupplier.BnkAccName), "BNKACCNAME"),
            (nameof(clsSupplier.BnkAccNo), "BNKACCNO"),
            (nameof(clsSupplier.Branch), "BRANCH"),
            (nameof(clsSupplier.IbanNo), "IBANNO"),
            (nameof(clsSupplier.VatFlag), "VATFLAG"),
            (nameof(clsSupplier.BillFlag), "BILLFLAG"),
            (nameof(clsSupplier.CrPerFlag), "CRPERFLAG"),
            (nameof(clsSupplier.CrLimitFlag), "CRLIMITFLAG"),
            (nameof(clsSupplier.CrLimit), "CRLIMIT"),
            //(nameof(clsSupplier.UiTime), "UTIME"),
            (nameof(clsSupplier.VatRegNo), "VATREGNO"),
            (nameof(clsSupplier.ShipAddress), "SHIPADDRESS"),
            (nameof(clsSupplier.SecName), "SECNAME"),
            (nameof(clsSupplier.CurFlg), "CURFLG"),
            (nameof(clsSupplier.CrPeriod), "CRPERIOD"),
            (nameof(clsSupplier.GrpItem), "GRPITEM"),
            (nameof(clsSupplier.ItemGroupId), "ITEMGRPCD"),
            (nameof(clsSupplier.GccFlg), "GCCFLG"),
            (nameof(clsSupplier.StreetOne), "STREETONE"),
            (nameof(clsSupplier.StreetTwo), "STREETTWO"),
            (nameof(clsSupplier.BuildingNo), "BULDINGNO"),
            (nameof(clsSupplier.PlotId), "PLOTID"),
            (nameof(clsSupplier.CityName), "CITYNAME"),
            (nameof(clsSupplier.PostalCode), "POSTALCODE"),
            (nameof(clsSupplier.District), "DISTRICT"),
            (nameof(clsSupplier.GrpVatNo), "GRPVATNO"),
            (nameof(clsSupplier.EinvIdType), "EINVIDTYPE"),
            (nameof(clsSupplier.EinvId), "EINVID"),
            (nameof(clsSupplier.VatType), "VATTYPE"),
            (nameof(clsSupplier.PartyGrpId), "PARTYGRPID"),
            (nameof(clsSupplier.CityNameSec), "CITYNAMESEC"),
            (nameof(clsSupplier.DistrictSec), "DISTRICTSEC")
        };

        private static object DbValue(object? value)
        {
            return value ?? DBNull.Value;
        }

        private static void AddSupplierParameters(
            SqlCommand command,
            clsSupplier supplier)
        {
            var type = typeof(clsSupplier);

            foreach (var field in Fields)
            {
                var property = type.GetProperty(field.Property)!;
                var value = property.GetValue(supplier);

                command.Parameters.AddWithValue(
                    "@" + field.Column,
                    DbValue(value));
            }

        }

        private static clsSupplier MapSupplier(SqlDataReader reader)
        {
            var supplier = new clsSupplier();

            supplier.Id = Convert.ToInt32(reader["ID"]);

            foreach (var field in Fields)
            {
                var property = typeof(clsSupplier).GetProperty(field.Property)!;
                var value = reader[field.Column];

                if (value == DBNull.Value)
                {
                    property.SetValue(supplier, null);
                    continue;
                }

                var targetType = Nullable.GetUnderlyingType(property.PropertyType)
                                 ?? property.PropertyType;

                property.SetValue(
                    supplier,
                    Convert.ChangeType(value, targetType));
            }

            //supplier.DelFlg = reader["DELFLG"] != DBNull.Value
                              //&& Convert.ToBoolean(reader["DELFLG"]);

            supplier.AprvLst = reader["APRVLST"] == DBNull.Value
                ? null
                : Convert.ToString(reader["APRVLST"]);

            // Map RowVersion for concurrency protection
            var rowVersionOrdinal = reader.GetOrdinal("RowVersion");

            supplier.RowVersion = reader.IsDBNull(rowVersionOrdinal)
                ? null
                : (byte[])reader.GetValue(rowVersionOrdinal);

            return supplier;
        }
        // =========================================================
        // GET SUPPLIERS WITH PAGINATION AND SEARCH
        // =========================================================




        //public async Task<(List<clsSupplier> Data, int TotalRecords)> GetSuppliers(int page,int pageSize,string? search)
        //{
        //    page = Math.Max(1, page);
        //    pageSize = Math.Clamp(pageSize, 1, 500);

        //    var suppliers = new List<clsSupplier>();
        //    int totalRecords;

        //    using var connection = db.GetConnection();

        //    if (connection.State != ConnectionState.Open)
        //        await connection.OpenAsync();

        //    string searchText = search?.Trim() ?? string.Empty;

        //    // 1. Count active suppliers matching the search.
        //    const string countSql = @"
        //    SELECT COUNT(1)
        //    FROM dbo.SUPMAS
        //    WHERE ISNULL(DELFLG, 0) = 0
        //      AND (
        //        @Search = ''
        //        OR SUPNAME LIKE '%' + @Search + '%'
        //        OR ALIAS LIKE '%' + @Search + '%'
        //        OR PRINTNAME LIKE '%' + @Search + '%'
        //        OR PHONE LIKE '%' + @Search + '%'
        //        OR EMAIL LIKE '%' + @Search + '%'
        //  );";

        //    using (var command = new SqlCommand(countSql, connection))
        //    {
        //        command.Parameters.Add("@Search", SqlDbType.NVarChar, 200)
        //            .Value = searchText;

        //        totalRecords = Convert.ToInt32(
        //            await command.ExecuteScalarAsync());
        //    }

        //    if (totalRecords == 0)
        //        return (suppliers, 0);

        //    // 2. Retrieve only the requested supplier page.
        //    const string dataSql = @"
        //SELECT *
        //FROM dbo.SUPMAS
        //WHERE ISNULL(DELFLG, 0) = 0
        //  AND (
        //        @Search = ''
        //        OR SUPNAME LIKE '%' + @Search + '%'
        //        OR ALIAS LIKE '%' + @Search + '%'
        //        OR PRINTNAME LIKE '%' + @Search + '%'
        //        OR PHONE LIKE '%' + @Search + '%'
        //        OR EMAIL LIKE '%' + @Search + '%'
        //  )
        //ORDER BY SUPNAME, ID
        //OFFSET @Offset ROWS
        //FETCH NEXT @PageSize ROWS ONLY;";

        //    using (var command = new SqlCommand(dataSql, connection))
        //    {
        //        command.Parameters.Add("@Search", SqlDbType.NVarChar, 200)
        //            .Value = searchText;

        //        command.Parameters.Add("@Offset", SqlDbType.Int)
        //            .Value = (page - 1) * pageSize;

        //        command.Parameters.Add("@PageSize", SqlDbType.Int)
        //            .Value = pageSize;

        //        using var reader = await command.ExecuteReaderAsync();

        //        while (await reader.ReadAsync())
        //        {
        //            var supplier = MapSupplier(reader);

        //            supplier.OpenBal = 0m;
        //            supplier.CrDr = "CR";
        //            supplier.CurCode = 0;
        //            supplier.CurConvRate = 1m;
        //            supplier.LedgerGroupId = 0;
        //            supplier.AccountCode = string.Empty;
        //            supplier.PendingBills = new List<PendingBillRequest>();

        //            suppliers.Add(supplier);
        //        }
        //    }

        //    // 3. Collect distinct linked account IDs.
        //    var accountIds = suppliers
        //        .Where(s => s.AccId > 0)
        //        .Select(s => s.AccId)
        //        .Distinct()
        //        .ToList();

        //    if (accountIds.Count == 0)
        //        return (suppliers, totalRecords);

        //    // Multiple suppliers may reference the same account.
        //    var supplierByAccount = suppliers
        //        .Where(s => s.AccId > 0)
        //        .GroupBy(s => s.AccId)
        //        .ToDictionary(g => g.Key, g => g.ToList());

        //    // Safe parameter names are generated locally; account IDs are
        //    // always supplied as SQL parameters, never interpolated as values.
        //    var parameterNames = accountIds
        //        .Select((_, index) => $"@AccId{index}")
        //        .ToArray();

        //    string inClause = string.Join(", ", parameterNames);

        //    // 4. Load account details, including ledger group, in one query.
        //    string accountSql = $@"
        //    SELECT
        //        ID,
        //        OPENBAL,
        //        CURCODE,
        //        CURCONVRATE,
        //        ACCODE,
        //        MGROUP,SWIFTCODE
        //    FROM dbo.ACCMAS
        //    WHERE ID IN ({inClause});";

        //    using (var command = new SqlCommand(accountSql, connection))
        //    {
        //        for (int i = 0; i < accountIds.Count; i++)
        //        {
        //            command.Parameters.Add(parameterNames[i], SqlDbType.Int)
        //                .Value = accountIds[i];
        //        }

        //        using var reader = await command.ExecuteReaderAsync();

        //        while (await reader.ReadAsync())
        //        {
        //            int accountId = Convert.ToInt32(reader["ID"]);

        //            if (!supplierByAccount.TryGetValue(accountId, out var linkedSuppliers))
        //                continue;

        //            decimal openingBalance = reader["OPENBAL"] == DBNull.Value
        //                ? 0m
        //                : Convert.ToDecimal(reader["OPENBAL"]);

        //            int currencyCode = reader["CURCODE"] == DBNull.Value
        //                ? 0
        //                : Convert.ToInt32(reader["CURCODE"]);

        //            decimal conversionRate = reader["CURCONVRATE"] == DBNull.Value
        //                ? 1m
        //                : Convert.ToDecimal(reader["CURCONVRATE"]);

        //            string accountCode = reader["ACCODE"] == DBNull.Value
        //                ? string.Empty
        //                : Convert.ToString(reader["ACCODE"]) ?? string.Empty;

        //            int ledgerGroupId = reader["MGROUP"] == DBNull.Value
        //                ? 0
        //                : Convert.ToInt32(reader["MGROUP"]);
        //            string swiftcode = reader["SWIFTCODE"] == DBNull.Value
        //                   ? string.Empty : Convert.ToString(reader["SWIFTCODE"]) ?? string.Empty;

        //            foreach (var supplier in linkedSuppliers)
        //            {
        //                supplier.OpenBal = openingBalance;
        //                supplier.CurCode = currencyCode;
        //                supplier.CurConvRate = conversionRate;
        //                supplier.AccountCode = accountCode;
        //                supplier.LedgerGroupId = ledgerGroupId;
        //                supplier.SwiftCode = swiftcode;
        //            }
        //        }
        //    }

        //    // 5. Load opening debit/credit totals in one query.
        //    string openingSql = $@"
        //    SELECT
        //        ACCID,
        //        SUM(ISNULL(DEBIT, 0)) AS TotalDebit,
        //        SUM(ISNULL(CREDIT, 0)) AS TotalCredit
        //    FROM dbo.OPN
        //    WHERE ACCID IN ({inClause})
        //      AND BRCD = 0
        //    GROUP BY ACCID;";

        //    using (var command = new SqlCommand(openingSql, connection))
        //    {
        //        for (int i = 0; i < accountIds.Count; i++)
        //        {
        //            command.Parameters.Add(parameterNames[i], SqlDbType.Int)
        //                .Value = accountIds[i];
        //        }

        //        using var reader = await command.ExecuteReaderAsync();

        //        while (await reader.ReadAsync())
        //        {
        //            int accountId = Convert.ToInt32(reader["ACCID"]);

        //            if (!supplierByAccount.TryGetValue(accountId, out var linkedSuppliers))
        //                continue;

        //            decimal debit = reader["TotalDebit"] == DBNull.Value
        //                ? 0m
        //                : Convert.ToDecimal(reader["TotalDebit"]);

        //            decimal credit = reader["TotalCredit"] == DBNull.Value
        //                ? 0m
        //                : Convert.ToDecimal(reader["TotalCredit"]);

        //            string crDr = credit >= debit ? "Cr" : "Dr";

        //            foreach (var supplier in linkedSuppliers)
        //            {
        //                supplier.CrDr = crDr;
        //            }
        //        }
        //    }

        //    // 6. Load pending supplier bills in one query.
        //    string billsSql = $@"
        //    SELECT
        //        CODE,
        //        ID,
        //        INVDATE,
        //        NARRATION,
        //        INVNO,
        //        AMOUNT,
        //        DUEDATE,
        //        DRCR
        //    FROM dbo.PENDBIL
        //    WHERE CODE IN ({inClause})
        //      AND INVTYPE = 'SB'
        //    ORDER BY CODE, INVDATE, INVNO, ID;";

        //    using (var command = new SqlCommand(billsSql, connection))
        //    {
        //        for (int i = 0; i < accountIds.Count; i++)
        //        {
        //            command.Parameters.Add(parameterNames[i], SqlDbType.Int)
        //                .Value = accountIds[i];
        //        }

        //        using var reader = await command.ExecuteReaderAsync();

        //        while (await reader.ReadAsync())
        //        {
        //            int accountId = Convert.ToInt32(reader["CODE"]);

        //            if (!supplierByAccount.TryGetValue(accountId, out var linkedSuppliers))
        //                continue;

        //            var bill = new PendingBillRequest
        //            {
        //                    Id = reader["ID"] == DBNull.Value
        //                    ? null
        //                    : Convert.ToInt32(reader["ID"]),

        //                InvDate = reader["INVDATE"] == DBNull.Value
        //                    ? default
        //                    : Convert.ToDateTime(reader["INVDATE"]),

        //                Narration = reader["NARRATION"] == DBNull.Value
        //                    ? string.Empty
        //                    : Convert.ToString(reader["NARRATION"]) ?? string.Empty,

        //                InvNo = reader["INVNO"] == DBNull.Value
        //                    ? string.Empty
        //                    : Convert.ToString(reader["INVNO"]) ?? string.Empty,

        //                Amount = reader["AMOUNT"] == DBNull.Value
        //                    ? 0m
        //                    : Convert.ToDecimal(reader["AMOUNT"]),

        //                DueDate = reader["DUEDATE"] == DBNull.Value
        //                    ? default
        //                    : Convert.ToDateTime(reader["DUEDATE"]),

        //                DrCr = reader["DRCR"] == DBNull.Value
        //                    ? "Cr"
        //                    : Convert.ToString(reader["DRCR"]) ?? "Cr"
        //            };

        //            foreach (var supplier in linkedSuppliers)
        //            {
        //                // Each supplier gets its own list.
        //                supplier.PendingBills.Add(new PendingBillRequest
        //                {
        //                    Id = bill.Id,
        //                    InvDate = bill.InvDate,
        //                    Narration = bill.Narration,
        //                    InvNo = bill.InvNo,
        //                    Amount = bill.Amount,
        //                    DueDate = bill.DueDate,
        //                    DrCr = bill.DrCr
        //                });
        //            }
        //        }
        //    }

        //    return (suppliers, totalRecords);
        //}


        public async Task<(List<clsSupplierResponse> Data, int TotalRecords)> GetSuppliers(
            int page,
            int pageSize,
            string? search,
            bool? active = null)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 500);

            var searchValue = search?.Trim() ?? string.Empty;

            // 1. COUNT SUPPLIERS
            const string countSql = """
            SELECT COUNT(1)
            FROM dbo.SUPMAS S
            WHERE ISNULL(S.DELFLG, 0) = 0
              AND (@Active IS NULL OR ISNULL(S.ACTIVE, 0) = @Active)
              AND (
                    @Search = ''
                    OR S.SUPNAME LIKE '%' + @Search + '%'
                    OR S.ALIAS LIKE '%' + @Search + '%'
                    OR S.PRINTNAME LIKE '%' + @Search + '%'
                    OR S.PHONE LIKE '%' + @Search + '%'
                    OR S.EMAIL LIKE '%' + @Search + '%'
              );
            """;

            int totalRecords = await db.QueryFirstOrDefaultAsync<int>(
                countSql,
                new { Search = searchValue, Active = active });

            if (totalRecords == 0)
                return (new List<clsSupplierResponse>(), 0);

            // 2. GET PAGINATED SUPPLIERS
            const string supplierSql = """
                    SELECT
                        S.ID AS Id,
                        S.ALIAS AS Alias,
                        S.SUPNAME AS SupName,
                        S.ADDRESS AS Address,
                        S.PHONE AS Phone,
                        S.EMAIL AS Email,
                        S.ACTIVE AS Active,
                        S.LOGCD AS LogId,
                        S.ACCID AS AccountId,

                        S.PRINTNAME AS PrintName,
                        TRY_CONVERT(decimal(18,4), S.DISC) AS Disc,
                        S.STATECODE AS StateCode,
                        S.COUNTRYCODE AS CountryCode,
                        S.PRICELEVEL AS PriceLevel,

                        S.BNKACCNAME AS BnkAccName,
                        S.BNKACCNO AS BnkAccNo,
                        S.BRANCH AS Branch,
                        S.IBANNO AS IbanNo,

                        S.VATFLAG AS VatFlag,
                        S.BILLFLAG AS BillFlag,
                        S.CRPERFLAG AS CrPerFlag,
                        S.CRLIMITFLAG AS CrLimitFlag,
                        S.CRLIMIT AS CrLimit,

                        S.VATREGNO AS VatRegNo,
                        S.SHIPADDRESS AS ShipAddress,
                        S.SECNAME AS SecName,
                        S.CURFLG AS CurFlg,

                        S.CRPERIOD AS CrPeriod,
                        S.GRPITEM AS GrpItem,
                        S.ITEMGRPCD AS ItemGroupId,
                        S.GCCFLG AS GccFlg,

                        S.STREETONE AS StreetOne,
                        S.STREETTWO AS StreetTwo,
                        S.BULDINGNO AS BuildingNo,
                        S.PLOTID AS PlotId,
                        S.CITYNAME AS CityName,
                        S.POSTALCODE AS PostalCode,
                        S.DISTRICT AS District,

                        S.GRPVATNO AS GrpVatNo,
                        S.EINVIDTYPE AS EinvIdType,
                        S.EINVID AS EinvId,
                        S.VATTYPE AS VatType,

                        S.PARTYGRPID AS PartyGrpId,

                        S.CITYNAMESEC AS CityNameSec,
                        S.DISTRICTSEC AS DistrictSec,
                        S.APRVLST AS AprvLst,

                        S.ROWVERSION AS RowVersion

                    FROM dbo.SUPMAS S
                    WHERE ISNULL(S.DELFLG, 0) = 0
                      AND (@Active IS NULL OR ISNULL(S.ACTIVE, 0) = @Active)
                      AND (
                            @Search = ''
                            OR S.SUPNAME LIKE '%' + @Search + '%'
                            OR S.ALIAS LIKE '%' + @Search + '%'
                            OR S.PRINTNAME LIKE '%' + @Search + '%'
                            OR S.PHONE LIKE '%' + @Search + '%'
                            OR S.EMAIL LIKE '%' + @Search + '%'
                      )
                    ORDER BY S.SUPNAME, S.ID
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY;
                    """;
            var suppliers = await db.QueryListAsync<clsSupplierResponse>(
                supplierSql,
                new
                {
                    Search = searchValue,
                    Active = active,
                    Offset = (page - 1) * pageSize,
                    PageSize = pageSize
                });

            var result = suppliers.ToList();

            if (result.Count == 0)
                return (result, totalRecords);

            // 3. COLLECT LINKED ACCOUNT IDS
            var accountIds = result
                .Where(s => s.AccountId > 0)
                .Select(s => s.AccountId)
                .Distinct()
                .ToArray();

            if (accountIds.Length == 0)
                return (result, totalRecords);

            var supplierByAccount = result
                .Where(s => s.AccountId > 0)
                .GroupBy(s => s.AccountId)
                .ToDictionary(g => g.Key, g => g.First());

            // 4. GET OPENING BALANCES
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

            var openingBalances =
                await db.QueryListAsync<SupplierOpeningBalanceResult>(
                    openingSql,
                    new { AccountIds = accountIds });

            foreach (var opening in openingBalances)
            {
                if (!supplierByAccount.TryGetValue(opening.AccountId, out var supplier))
                    continue;

                supplier.OpenBal = Math.Abs(
                    opening.TotalDebit - opening.TotalCredit);

                supplier.CrDr = opening.TotalDebit > opening.TotalCredit
                    ? "DR"
                    : opening.TotalCredit > opening.TotalDebit
                        ? "CR"
                        : "DR";

                supplier.CurCode = opening.CurrencyCode ?? 0;
                supplier.CurConvRate = opening.ConversionRate ?? 1m;
            }

            // 5. GET PENDING BILLS
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
              AND INVTYPE = 'SB'
            ORDER BY CODE, INVDATE, INVNO, ID;
            """;
                
            var pendingBills =
                await db.QueryListAsync<SupplierPendingBillResult>(
                    pendingBillsSql,
                    new { AccountIds = accountIds });

            foreach (var bill in pendingBills)
            {
                if (!supplierByAccount.TryGetValue(bill.AccountId, out var supplier))
                    continue;

                supplier.PendingBills.Add(bill);
            }

            // 6. GET LEDGER GROUP, ACCOUNT CODE AND SWIFT CODE
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

            var ledgerDetails =
                await db.QueryListAsync<SupplierLedgerResult>(
                    ledgerSql,
                    new { AccountIds = accountIds });

            foreach (var ledger in ledgerDetails)
            {
                if (!supplierByAccount.TryGetValue(ledger.AccountId, out var supplier))
                    continue;

                supplier.LedgerGroupId = ledger.LedgerGroupId;
                supplier.AccountCode = ledger.AccountCode;
                supplier.SwiftCode = ledger.SwiftCode;
            }

            return (result, totalRecords);
        }


        // =========================================================
        // GET SUPPLIER BY ID
        // =========================================================
        public async Task<int?> GetSupplierByIdExist(int id)
        {
            if (id <= 0)
                return 0;

            const string supplierSql = """
                SELECT S.ID
                FROM dbo.SUPMAS S
                WHERE S.ACTIVE = 1
                  AND S.ID = @Id
                  AND ISNULL(S.DELFLG, 0) = 0;
                """;

            return await db.QueryFirstOrDefaultAsync<int?>(
                supplierSql,
                new { Id = id });
        }
        public async Task<clsSupplierResponse?> GetSupplierById(int id)
        {
            if (id <= 0)
                return null;

            // =========================================================
            // 1. GET SUPPLIER
            // =========================================================

            const string supplierSql = """
        SELECT
            S.ID AS Id,
            S.ALIAS AS Alias,
            S.SUPNAME AS SupName,
            S.ADDRESS AS Address,
            S.PHONE AS Phone,
            S.EMAIL AS Email,
            S.ACTIVE AS Active,
            S.LOGCD AS LogId,
            S.ACCID AS AccountId,
        
            S.PRINTNAME AS PrintName,
            TRY_CONVERT(decimal(18,4), S.DISC) AS Disc,
            S.STATECODE AS StateCode,
            S.COUNTRYCODE AS CountryCode,
            S.PRICELEVEL AS PriceLevel,
        
            S.BNKACCNAME AS BnkAccName,
            S.BNKACCNO AS BnkAccNo,
            S.BRANCH AS Branch,
            S.IBANNO AS IbanNo,
        
            S.VATFLAG AS VatFlag,
            S.BILLFLAG AS BillFlag,
            S.CRPERFLAG AS CrPerFlag,
            S.CRLIMITFLAG AS CrLimitFlag,
            S.CRLIMIT AS CrLimit,
        
            S.VATREGNO AS VatRegNo,
            S.SHIPADDRESS AS ShipAddress,
            S.SECNAME AS SecName,
            S.CURFLG AS CurFlg,
        
            S.CRPERIOD AS CrPeriod,
            S.GRPITEM AS GrpItem,
            S.ITEMGRPCD AS ItemGroupId,
            S.GCCFLG AS GccFlg,
        
            S.STREETONE AS StreetOne,
            S.STREETTWO AS StreetTwo,
            S.BULDINGNO AS BuildingNo,
            S.PLOTID AS PlotId,
            S.CITYNAME AS CityName,
            S.POSTALCODE AS PostalCode,
            S.DISTRICT AS District,
        
            S.GRPVATNO AS GrpVatNo,
            S.EINVIDTYPE AS EinvIdType,
            S.EINVID AS EinvId,
            S.VATTYPE AS VatType,
        
            S.PARTYGRPID AS PartyGrpId,
        
            S.CITYNAMESEC AS CityNameSec,
            S.DISTRICTSEC AS DistrictSec,
            S.APRVLST AS AprvLst,
        
            S.ROWVERSION AS RowVersion

        FROM dbo.SUPMAS S
        WHERE S.ID = @Id
          AND ISNULL(S.DELFLG, 0) = 0;
        """;

            var supplier = await db.QueryFirstOrDefaultAsync<clsSupplierResponse>(
                supplierSql,
                new { Id = id });

            if (supplier == null)
                return null;

            // =========================================================
            // 2. GET OPENING BALANCE DIRECTION
            // Preserve supplier legacy BRCD = 0 condition
            // =========================================================

            if (supplier.AccountId > 0)
            {
                const string openingSql = """
            SELECT
                ACCID AS AccountId,
                SUM(ISNULL(DEBIT, 0)) AS TotalDebit,
                SUM(ISNULL(CREDIT, 0)) AS TotalCredit
            FROM dbo.OPN
            WHERE ACCID = @AccountId
              AND BRCD = 0
            GROUP BY ACCID;
            """;

                var opening = await db.QueryFirstOrDefaultAsync<SupplierOpeningBalanceResult>(
                    openingSql,
                    new { AccountId = supplier.AccountId });

                if (opening != null)
                {
                    supplier.CrDr = opening.TotalDebit > opening.TotalCredit
                        ? "DR"
                        : opening.TotalCredit > opening.TotalDebit
                            ? "CR"
                            : "DR";
                }

                // =====================================================
                // 3. GET ACCOUNT DETAILS
                // =====================================================

                const string accountSql = """
            SELECT
                OPENBAL AS OpenBal,
                CURCODE AS CurCode,
                CURCONVRATE AS CurConvRate,
                MGROUP AS LedgerGroupId,
                ACCODE AS AccountCode,
                SWIFTCODE AS SwiftCode
            FROM dbo.ACCMAS
            WHERE ISNULL(DELFLG, 0) = 0 AND ID = @AccountId;
            """;

                var account = await db.QueryFirstOrDefaultAsync<SupplierAccountDetailsResult>(
                    accountSql,
                    new { AccountId = supplier.AccountId });

                if (account != null)
                {
                    supplier.OpenBal = account.OpenBal ?? 0m;
                    supplier.CurCode = account.CurCode ?? 0;
                    supplier.CurConvRate = account.CurConvRate ?? 1m;

                    supplier.LedgerGroupId = account.LedgerGroupId;
                    supplier.AccountCode = account.AccountCode;
                    supplier.SwiftCode = account.SwiftCode;
                }

                // =====================================================
                // 4. GET PENDING BILLS
                // =====================================================

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
            WHERE CODE = @AccountId
              AND INVTYPE = 'SB'
            ORDER BY INVDATE, INVNO, ID;
            """;

                var pendingBills =
                    await db.QueryListAsync<SupplierPendingBillResult>(
                        pendingBillsSql,
                        new { AccountId = supplier.AccountId });

                supplier.PendingBills = pendingBills.ToList();
            }

            return supplier;
        }


        //public async Task<clsSupplier?> GetSupplierById(int id)
        //{
        //    const string sql = @"
        //    SELECT *
        //    FROM dbo.SUPMAS
        //    WHERE ID = @Id
        //      AND ISNULL(DELFLG, 0) = 0;";

        //    using var connection = db.GetConnection();

        //    if (connection.State != ConnectionState.Open)
        //        await connection.OpenAsync();

        //    clsSupplier? supplier;

        //    using (var command = new SqlCommand(sql, connection))
        //    {
        //        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        //        using var reader = await command.ExecuteReaderAsync();

        //        supplier = await reader.ReadAsync()
        //            ? MapSupplier(reader)
        //            : null;
        //    }

        //    if (supplier == null)
        //        return null;

        //    // Load opening balance, currency, debit/credit direction and pending bills.
        //    await LoadSupplierOpeningData(connection, supplier);
        //    const string accountSql = @"
        //    SELECT
        //        OPENBAL,
        //        CURCODE,
        //        CURCONVRATE,
        //        MGROUP,ACCODE,SWIFTCODE

        //    FROM dbo.ACCMAS
        //    WHERE ID = @AccId;";

        //    using (var command = new SqlCommand(accountSql, connection))
        //    {
        //        command.Parameters.Add("@AccId", SqlDbType.Int)
        //            .Value = supplier.AccountId;

        //        using var reader = await command.ExecuteReaderAsync();

        //        if (await reader.ReadAsync())
        //        {
        //            supplier.OpenBal = reader["OPENBAL"] == DBNull.Value
        //                ? 0m
        //                : Convert.ToDecimal(reader["OPENBAL"]);

        //            supplier.CurCode = reader["CURCODE"] == DBNull.Value
        //                ? 0
        //                : Convert.ToInt32(reader["CURCODE"]);

        //            supplier.CurConvRate = reader["CURCONVRATE"] == DBNull.Value
        //                ? 1m
        //                : Convert.ToDecimal(reader["CURCONVRATE"]);

        //            supplier.LedgerGroupId = reader["MGROUP"] == DBNull.Value
        //                ? 0
        //                : Convert.ToInt32(reader["MGROUP"]);
        //            supplier.AccountCode = Convert.ToString(reader["ACCODE"]);
        //            supplier.SwiftCode = reader["SWIFTCODE"] == DBNull.Value
        //              ? null
        //              : Convert.ToString(reader["SWIFTCODE"]);
        //        }
        //    }
        //    return supplier;
        //}


        // =========================================================
        // CHECK DUPLICATE SUPPLIER NAME
        // =========================================================

        public async Task<bool> SupplierNameExists(
            string supplierName,
            int? excludeId = null)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM SUPMAS
                WHERE UPPER(LTRIM(RTRIM(SUPNAME))) =
                      UPPER(LTRIM(RTRIM(@SupplierName)))
                  AND ISNULL(DELFLG, 0) = 0 and ACTIVE=1
                  AND (@ExcludeId IS NULL OR ID <> @ExcludeId);";

            var connection = db.GetConnection();

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@SupplierName", SqlDbType.NVarChar, 200)
                .Value = supplierName.Trim();

            command.Parameters.Add("@ExcludeId", SqlDbType.Int)
                .Value = (object?)excludeId ?? DBNull.Value;

            return Convert.ToInt32(
                await command.ExecuteScalarAsync()) > 0;
        }



        public async Task<string?> ValidateSupplierLookupsAsync(SupplierRequest supplier, int ledgerGroupId)
        {
            using var connection = db.GetConnection();

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            // 1. Validate currency.
            // Currency code 0 is allowed as your existing GET implementation
            // uses 0 as its default.
            if (supplier.CurCode > 0)
            {
                const string sql = @"
                SELECT COUNT(1)
                FROM dbo.CURRENCYMAS
                WHERE CODE = @Code;";

                using var command = new SqlCommand(sql, connection);

                command.Parameters.Add("@Code", SqlDbType.Int)
                    .Value = supplier.CurCode;

                if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
                    return "Selected currency does not exist.";
            }

            // 2. Validate supplier ledger group.
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
                    return "Selected supplier ledger group does not exist or is inactive.";
            }

            // 3. Validate item group only when group-item option is enabled.
            //if (supplier.GrpItem)
            //{
            //    if (supplier.ItemGroupCD <= 0)
            //        return "Item group is required when group item is enabled.";

            //    const string itemGroupSql = @"
            //    SELECT COUNT(1)
            //    FROM dbo.PRGRPMAS
            //    WHERE PRGRPID = @GroupId
            //      AND ISNULL(ACTIVE, 1) = 1;";

            //    using var command = new SqlCommand(itemGroupSql, connection);

            //    command.Parameters.Add("@GroupId", SqlDbType.Int)
            //        .Value = supplier.ItemGroupCD;

            //    if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
            //        return "Selected item group does not exist or is inactive.";
            //}

            // 4. Validate supplier party group.
            if (supplier.PartyGrpId > 0)
            {
                const string partyGroupSql = @"
                SELECT COUNT(1)
                FROM dbo.PARTYGROUPMAS
                WHERE ID = @Id
                  AND UPPER(LTRIM(RTRIM(ISNULL(GROUPTYPE, '')))) = 'S';";

                using var command = new SqlCommand(partyGroupSql, connection);

                command.Parameters.Add("@Id", SqlDbType.Int)
                    .Value = supplier.PartyGrpId;

                if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
                    return "Selected party group does not exist or is not a supplier group.";
            }


            // 4. Validate country from main database.
            if (supplier.CountryCode <= 0)
                return "Please select a valid country.";

            const string countrySql = """
                SELECT COUNTRYID AS CountryId
                FROM dbo.COUNTRY
                WHERE COUNTRYID = @CountryId
                  AND ISNULL(ACTIVE, 1) = 1;
                """;

            var country = await db.QueryMasterFirstOrDefaultAsync<CountryDtos>(
                countrySql,
                new { CountryId = supplier.CountryCode });

            if (country == null)
                return "Selected country does not exist or is inactive.";


            // 5. Validate region belongs to the selected country.
            if (supplier.StateCode <= 0)
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
                    RegionId = supplier.StateCode,
                    CountryId = supplier.CountryCode
                });

            if (region == null)
                return "Selected region does not exist, is inactive, or does not belong to the selected country.";

            return null;
        }
        // =========================================================
        // INSERT SUPPLIER WITH LINKED ACCOUNT
        // =========================================================
        //public async Task<SupplierAccountInsertResult> InsertSupplier(int userId,
        //clsSupplier supplier,
        //int ledgerGroupId,
        //string groupCode)
        //{
        //    using var connection = db.GetConnection();

        //    if (connection.State != ConnectionState.Open)
        //        await connection.OpenAsync();

        //    using var transaction = connection.BeginTransaction();

        //    try
        //    {
        //        // 1. Validate opening balance and currency values.
        //        if (supplier.OpenBal < 0)
        //        {
        //            throw new InvalidOperationException(
        //                "Opening balance cannot be negative.");
        //        }

        //        if (supplier.CurConvRate <= 0)
        //        {
        //            throw new InvalidOperationException(
        //                "Currency conversion rate must be greater than zero.");
        //        }
        //        supplier.Logcd = userId;

        //        // 2. Insert supplier into SUPMAS.
        //        var columns = string.Join(
        //            ", ",
        //            Fields.Select(f => f.Column)
        //                .Concat(new[] { "DELFLG" }));

        //        var values = string.Join(
        //            ", ",
        //            Fields.Select(f => "@" + f.Column)
        //                .Concat(new[] { "0" }));

        //        var insertSupplierSql = $@"
        //        INSERT INTO dbo.SUPMAS ({columns})
        //        OUTPUT INSERTED.ID
        //        VALUES ({values});";

        //        int supplierId;

        //        using (var command = new SqlCommand(
        //            insertSupplierSql,
        //            connection,
        //            transaction))
        //        {
        //            AddSupplierParameters(command, supplier);

        //            var result = await command.ExecuteScalarAsync();

        //            if (result == null || result == DBNull.Value)
        //            {
        //                throw new InvalidOperationException(
        //                    "Supplier insertion failed.");
        //            }

        //            supplierId = Convert.ToInt32(result);
        //        }

        //        // 3. Generate the account code from the actual supplier ID.
        //        string accountCode = GenerateSupplierAccountCode(
        //            ledgerGroupId,
        //            groupCode,
        //            supplierId);

        //        // 4. Create the supplier account in ACCMAS.
        //        int accountId = await InsertSupplierAccount(
        //            connection,
        //            transaction,
        //            supplier,
        //            ledgerGroupId,
        //            accountCode);

        //        if (accountId <= 0)
        //        {
        //            throw new InvalidOperationException(
        //                "Supplier account creation failed.");
        //        }

        //        // 5. Link the account to the supplier.
        //        const string updateSupplierSql = @"
        //        UPDATE dbo.SUPMAS
        //        SET ACCID = @AccountId
        //        WHERE ID = @SupplierId
        //          AND ISNULL(DELFLG, 0) = 0;";

        //        using (var command = new SqlCommand(
        //            updateSupplierSql,
        //            connection,
        //            transaction))
        //        {
        //            command.Parameters.Add("@AccountId", SqlDbType.Int)
        //                .Value = accountId;

        //            command.Parameters.Add("@SupplierId", SqlDbType.Int)
        //                .Value = supplierId;

        //            int rowsUpdated = await command.ExecuteNonQueryAsync();

        //            if (rowsUpdated != 1)
        //            {
        //                throw new InvalidOperationException(
        //                    "Failed to link the account to the supplier.");
        //            }
        //        }

        //        // 6. Explicitly update opening balance and currency in ACCMAS.
        //        // This ensures the final account values match the supplier request.
        //        await UpdateAccmasOpening(
        //            connection,
        //            transaction,
        //            accountId,
        //            supplier.OpenBal,
        //            supplier.CurCode,
        //            supplier.CurConvRate);

        //        // 7. Save the opening balance in OPN.
        //        const int brCd = 0;

        //        await SaveOrUpdateOpn(
        //            connection,
        //            transaction,
        //            accountId,
        //            supplier.OpenBal,
        //            supplier.CrDr,
        //            supplier.CurCode,
        //            supplier.CurConvRate,
        //            supplier.Logcd,
        //            brCd);

        //        // 8. Save pending bills.
        //        await ReplacePendingBills(
        //            connection,
        //            transaction,
        //            accountId,
        //            supplier.CurCode,
        //            supplier.CurConvRate,
        //            supplier.Logcd,
        //            brCd,
        //            supplier.PendingBills);

        //        // 9. Read back the saved account values before committing.
        //        const string verifySql = @"
        //            SELECT OPENBAL, CURCODE, CURCONVRATE
        //            FROM dbo.ACCMAS
        //            WHERE ID = @AccountId;";

        //        using (var command = new SqlCommand(
        //            verifySql,
        //            connection,
        //            transaction))
        //        {
        //            command.Parameters.Add("@AccountId", SqlDbType.Int)
        //                .Value = accountId;

        //            using var reader = await command.ExecuteReaderAsync();

        //            if (!await reader.ReadAsync())
        //            {
        //                throw new InvalidOperationException(
        //                    "Created supplier account could not be verified.");
        //            }

        //            decimal savedOpenBal = reader["OPENBAL"] == DBNull.Value
        //                ? 0m
        //                : Convert.ToDecimal(reader["OPENBAL"]);

        //            int savedCurCode = reader["CURCODE"] == DBNull.Value
        //                ? 0
        //                : Convert.ToInt32(reader["CURCODE"]);

        //            decimal savedCurConvRate =
        //                reader["CURCONVRATE"] == DBNull.Value
        //                    ? 0m
        //                    : Convert.ToDecimal(reader["CURCONVRATE"]);

        //            if (savedOpenBal != supplier.OpenBal ||
        //                savedCurCode != supplier.CurCode ||
        //                savedCurConvRate != supplier.CurConvRate)
        //            {
        //                throw new InvalidOperationException(
        //                    $"Supplier account verification failed. " +
        //                    $"Expected OpenBal={supplier.OpenBal}, " +
        //                    $"CurCode={supplier.CurCode}, " +
        //                    $"CurConvRate={supplier.CurConvRate}. " +
        //                    $"Saved OpenBal={savedOpenBal}, " +
        //                    $"CurCode={savedCurCode}, " +
        //                    $"CurConvRate={savedCurConvRate}.");
        //            }
        //        }

        //        // 10. Commit all changes.
        //        await transaction.CommitAsync();

        //        supplier.AccId = accountId;

        //        return new SupplierAccountInsertResult
        //        {
        //            SupplierId = supplierId,
        //            AccountId = accountId,
        //            AccountCode = accountCode
        //        };
        //    }
        //    catch
        //    {
        //        await transaction.RollbackAsync();
        //        throw;
        //    }
        //}



        public async Task<SupplierAccountInsertResult> InsertSupplier(
    SupplierRequest supplier,
    int ledgerGroupId,
    string groupCode,
    int userId)
        {
            using var connection = db.GetConnection();

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                // =====================================================
                // 1. BASIC VALIDATION
                // =====================================================

                if (supplier == null)
                    throw new ArgumentException("Supplier details are required.");

                if (string.IsNullOrWhiteSpace(supplier.SupName))
                    throw new ArgumentException("Supplier name is required.");

                if (ledgerGroupId <= 0)
                    throw new ArgumentException("Invalid supplier ledger group.");

                if (string.IsNullOrWhiteSpace(groupCode))
                    throw new ArgumentException("Supplier ledger group code is required.");

                if (supplier.OpenBal < 0)
                    throw new ArgumentException("Opening balance cannot be negative.");

                if (supplier.CurFlg && supplier.CurCode <= 0)
                    throw new ArgumentException("A valid currency is required.");

                if (supplier.CurCode < 0)
                    throw new ArgumentException("Currency code cannot be negative.");

                if (supplier.CurConvRate <= 0)
                    throw new ArgumentException("Currency conversion rate must be greater than zero.");

                string openingDrCr = (supplier.CrDr ?? "").Trim().ToUpperInvariant();

                if (openingDrCr != "DR" && openingDrCr != "CR")
                    throw new ArgumentException("Opening balance type must be DR or CR.");

                if (supplier.PendingBills == null)
                    throw new ArgumentException("Pending bills list is required.");

                // =====================================================
                // 2. VALIDATE OPENING BALANCE AGAINST PENDING BILLS
                // =====================================================

                decimal openingBaseAmount = supplier.CurFlg
                    ? supplier.OpenBal * supplier.CurConvRate
                    : supplier.OpenBal;

                decimal openingDebit = openingDrCr == "DR"
                    ? openingBaseAmount
                    : 0m;

                decimal openingCredit = openingDrCr == "CR"
                    ? openingBaseAmount
                    : 0m;

                decimal pendingDebit = 0m;
                decimal pendingCredit = 0m;

                foreach (var bill in supplier.PendingBills)
                {
                    if (bill == null)
                        throw new ArgumentException("Invalid pending bill.");

                    if (string.IsNullOrWhiteSpace(bill.InvoiceNumber))
                        throw new ArgumentException("Pending bill invoice number is required.");

                    if (bill.Amount <= 0)
                        throw new ArgumentException(
                            $"Pending bill amount must be greater than zero. Invoice: {bill.InvoiceNumber}");

                    if (bill.DueDate < bill.InvoiceDate)
                        throw new ArgumentException(
                            $"Due date cannot be earlier than invoice date. Invoice: {bill.InvoiceNumber}");

                    string billDrCr = (bill.DrCr ?? "").Trim().ToUpperInvariant();

                    if (billDrCr != "DR" && billDrCr != "CR")
                        throw new ArgumentException(
                            $"Pending bill type must be DR or CR. Invoice: {bill.InvoiceNumber}");

                    decimal billBaseAmount = supplier.CurFlg
                        ? bill.Amount * supplier.CurConvRate
                        : bill.Amount;

                    if (billDrCr == "DR")
                        pendingDebit += billBaseAmount;
                    else
                        pendingCredit += billBaseAmount;
                }

                if (pendingDebit > openingDebit)
                {
                    throw new ArgumentException(
                        "Total pending debit bills cannot exceed the debit opening balance.");
                }

                if (pendingCredit > openingCredit)
                {
                    throw new ArgumentException(
                        "Total pending credit bills cannot exceed the credit opening balance.");
                }

                // =====================================================
                // 3. INSERT SUPPLIER MASTER
                // =====================================================
                const string supplierSql = @"
                    INSERT INTO dbo.SUPMAS
                    (
                        SUPNAME,
                        ALIAS,
                        ADDRESS,
                        PHONE,
                        EMAIL,
                        ACTIVE,
                        LOGCD,
                        UTIME,
                        ACCID,
                        PRINTNAME,
                        STATECODE,
                        COUNTRYCODE,
                        BNKACCNAME,
                        BNKACCNO,
                        BRANCH,
                        IBANNO,
                        VATFLAG,
                        BILLFLAG,
                        CRPERFLAG,
                        CRLIMITFLAG,
                        CRLIMIT,
                        VATREGNO,
                        SHIPADDRESS,
                        SECNAME,
                        CURFLG,
                        CRPERIOD,
                        GCCFLG,
                        STREETONE,
                        STREETTWO,
                        BULDINGNO,
                        PLOTID,
                        CITYNAME,
                        POSTALCODE,
                        DISTRICT,
                        GRPVATNO,
                        EINVIDTYPE,
                        EINVID,
                        VATTYPE,
                        PARTYGRPID
                    )
                    OUTPUT INSERTED.ID
                    VALUES
                    (
                        @SupName,
                        @Alias,
                        @Address,
                        @Phone,
                        @Email,
                        @Active,
                        @UserId,
                        GETDATE(),
                        0,
                        @PrintName,
                        @StateCode,
                        @CountryCode,
                        @BnkAccName,
                        @BnkAccNo,
                        @Branch,
                        @IbanNo,
                        @VatFlag,
                        @BillFlag,
                        @CrPerFlag,
                        @CrLimitFlag,
                        @CrLimit,
                        @VatRegNo,
                        @ShipAddress,
                        @SecName,
                        @CurFlg,
                        @CrPeriod,
                        @GccFlg,
                        @StreetOne,
                        @StreetTwo,
                        @BuildingNo,
                        @PlotId,
                        @CityName,
                        @PostalCode,
                        @District,
                        @GrpVatNo,
                        @EinvIdType,
                        @EinvId,
                        @VatType,
                        @PartyGrpId
                    );";

                int supplierId = await connection.ExecuteScalarAsync<int>(
                    supplierSql,
                    new
                    {
                        supplier.SupName,
                        supplier.Alias,
                        supplier.Address,
                        supplier.Phone,
                        supplier.Email,
                        supplier.Active,
                        UserId = userId,
                        supplier.PrintName,
                        supplier.StateCode,
                        supplier.CountryCode,
                        supplier.BnkAccName,
                        supplier.BnkAccNo,
                        supplier.Branch,
                        supplier.IbanNo,
                        supplier.VatFlag,
                        supplier.BillFlag,
                        supplier.CrPerFlag,
                        supplier.CrLimitFlag,
                        supplier.CrLimit,
                        supplier.VatRegNo,
                        supplier.ShipAddress,
                        supplier.SecName,
                        supplier.CurFlg,
                        supplier.CrPeriod,
                        supplier.GccFlg,
                        supplier.StreetOne,
                        supplier.StreetTwo,
                        supplier.BuildingNo,
                        supplier.PlotId,
                        supplier.CityName,
                        supplier.PostalCode,
                        supplier.District,
                        supplier.GrpVatNo,
                        supplier.EinvIdType,
                        supplier.EinvId,
                        supplier.VatType,
                        supplier.PartyGrpId
                    },
                    transaction);

                if (supplierId <= 0)
                    throw new InvalidOperationException("Supplier could not be created.");

                // =====================================================
                // 4. GENERATE ACCOUNT CODE
                // =====================================================

                string accountCode = GenerateSupplierAccountCode(ledgerGroupId,
                    groupCode,
                    supplierId);

                if (string.IsNullOrWhiteSpace(accountCode))
                    throw new InvalidOperationException("Supplier account code could not be generated.");

                // =====================================================
                // 5. INSERT LINKED ACCOUNT
                // =====================================================
                const string accountSql = @"
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
                var accountId = await connection.ExecuteScalarAsync<int>(
                      accountSql,
                      new
                      {
                          supplier.Alias,
                          AccName = supplier.SupName,
                          supplier.Address,
                          supplier.Phone,
                          supplier.Active,

                          UserId = userId,
                          UiTime = DateTime.Now,

                          AccCode = accountCode,
                          supplier.PrintName,

                          MGroup = ledgerGroupId,

                          supplier.VatFlag,

                          BankAccName = supplier.BnkAccName,
                          BankAccNo = supplier.BnkAccNo,
                          supplier.IbanNo,
                          supplier.SwiftCode,
                          supplier.Branch,

                          OpeningBalance = supplier.OpenBal,
                          supplier.SecName,

                          supplier.CurFlg,
                          ConversionRate = supplier.CurConvRate,
                          CurrencyCode = supplier.CurFlg ? supplier.CurCode : 0
                      },
                      transaction);

                if (accountId <= 0)
                    throw new InvalidOperationException("Supplier ledger account could not be created.");

                // =====================================================
                // 6. LINK SUPPLIER WITH ACCOUNT
                // =====================================================

                const string linkSql = @"
            UPDATE SUPMAS
            SET ACCID = @AccountId
            WHERE ID = @SupplierId;";

                int linkedRows = await connection.ExecuteAsync(
                    linkSql,
                    new
                    {
                        AccountId = accountId,
                        SupplierId = supplierId
                    },
                    transaction);

                if (linkedRows != 1)
                    throw new InvalidOperationException("Supplier account linking failed.");

                // =====================================================
                // 7. SAVE OPENING BALANCE IN OPN
                // =====================================================
                await SaveOrUpdateOpn(
                     connection,
                     transaction,
                     accountId,
                    supplier.OpenBal,
                    openingDrCr,
                    supplier.CurCode,
                    supplier.CurConvRate,
                    userId,
                    0
                 //supplier.BrCd
                 );


                // =====================================================
                // 8. SAVE PENDING BILLS
                // =====================================================

                if (supplier.PendingBills.Count > 0)
                {
                    await ReplacePendingBills(
                        connection,
                        transaction,
                        accountId,
                        supplier.CurCode,
                        supplier.CurConvRate,
                        userId,
                        0, supplier.PendingBills);
                }

                // =====================================================
                // 9. VERIFY LINKED ACCOUNT
                // =====================================================

                const string verifySql = @"
            SELECT COUNT(1)
            FROM ACCMAS
            WHERE ID = @AccountId
              AND DELFLG = 0;";

                int accountExists = await connection.ExecuteScalarAsync<int>(
                    verifySql,
                    new { AccountId = accountId },
                    transaction);

                if (accountExists != 1)
                    throw new InvalidOperationException("Created supplier account verification failed.");

                // =====================================================
                // 10. COMMIT
                // =====================================================

                transaction.Commit();

                return new SupplierAccountInsertResult
                {
                    SupplierId = supplierId,
                    AccountId = accountId,
                    AccountCode = accountCode
                };
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        // =========================================================
        // INSERT LINKED SUPPLIER ACCOUNT------
        // =========================================================


        //    public async Task<bool> UpdateSupplier(
        //int id,
        //clsSupplier supplier,
        //byte[] originalRowVersion,
        //int userId)
        //    {
        //        if (originalRowVersion == null ||
        //            originalRowVersion.Length != 8)
        //        {
        //            throw new ArgumentException("A valid 8-byte RowVersion is required.");
        //        }

        //        await using var connection = db.GetConnection();

        //        if (connection.State != ConnectionState.Open)
        //            await connection.OpenAsync();

        //        await using var transaction =
        //            (SqlTransaction)await connection.BeginTransactionAsync();

        //        try
        //        {
        //            // Use the real branch code selected by your application.
        //            // Keep 0 only if it is the correct default branch.
        //            const int branchCode = 0;

        //            // 1. Lock supplier and obtain its existing ledger account.
        //            const string accountSql = @"
        //        SELECT ACCID
        //        FROM dbo.SUPMAS WITH (UPDLOCK, HOLDLOCK)
        //        WHERE ID = @Id
        //          AND ISNULL(DELFLG, 0) = 0
        //          AND RowVersion = @OriginalRowVersion;";

        //            int accountId;

        //            await using (var command =
        //                new SqlCommand(accountSql, connection, transaction))
        //            {
        //                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        //                command.Parameters.Add("@OriginalRowVersion",
        //                    SqlDbType.Binary, 8).Value = originalRowVersion;

        //                object? result = await command.ExecuteScalarAsync();

        //                if (result == null || result == DBNull.Value)
        //                {
        //                    await transaction.RollbackAsync();
        //                    return false;
        //                }

        //                accountId = Convert.ToInt32(result);
        //            }

        //            if (accountId <= 0)
        //            {
        //                throw new InvalidOperationException(
        //                    "Supplier has no valid linked ledger account.");
        //            }

        //            // 2. Update supplier master, excluding linked account and RowVersion.
        //            var updateFields = Fields
        //                .Where(f =>
        //                    !string.Equals(f.Column, "ACCID",
        //                        StringComparison.OrdinalIgnoreCase) &&
        //                    !string.Equals(f.Column, "RowVersion",
        //                        StringComparison.OrdinalIgnoreCase))
        //                .ToList();

        //            if (updateFields.Count == 0)
        //            {
        //                throw new InvalidOperationException(
        //                    "No supplier fields are configured for updating.");
        //            }

        //            string setClause = string.Join(
        //                ", ",
        //                updateFields.Select(f => $"[{f.Column}] = @{f.Column}"));

        //            string supplierSql = $@"
        //        UPDATE dbo.SUPMAS
        //        SET {setClause}
        //        WHERE ID = @Id
        //          AND ISNULL(DELFLG, 0) = 0
        //          AND RowVersion = @OriginalRowVersion;";

        //            await using (var command =
        //                new SqlCommand(supplierSql, connection, transaction))
        //            {
        //                AddSupplierParameters(command, supplier);

        //                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        //                command.Parameters.Add("@OriginalRowVersion",
        //                    SqlDbType.Binary, 8).Value = originalRowVersion;

        //                if (await command.ExecuteNonQueryAsync() != 1)
        //                {
        //                    await transaction.RollbackAsync();
        //                    return false;
        //                }
        //            }

        //            // 3. Verify the ledger account still exists and update its name.
        //            //    const string accountUpdateSql = @"
        //            //UPDATE dbo.ACCMAS
        //            //SET ACCNAME = @AccountName
        //            //WHERE ID = @AccountId;";

        //            //    await using (var command =
        //            //        new SqlCommand(accountUpdateSql, connection, transaction))
        //            //    {
        //            //        command.Parameters.Add("@AccountName",
        //            //            SqlDbType.NVarChar, 200).Value = supplier.SupName.Trim();

        //            //        command.Parameters.Add("@AccountId", SqlDbType.Int).Value =
        //            //            accountId;

        //            //        if (await command.ExecuteNonQueryAsync() != 1)
        //            //        {
        //            //            throw new InvalidOperationException(
        //            //                "The linked supplier ledger account could not be updated.");
        //            //        }
        //            //    }


        //            // 3. Update the linked supplier ledger account.
        //            const string accountUpdateSql = @"
        //            UPDATE dbo.ACCMAS
        //            SET ACCNAME = @AccountName,
        //                SWIFTCODE = @SwiftCode
        //            WHERE ID = @AccountId;";

        //            await using (var command = new SqlCommand(
        //                accountUpdateSql, connection, transaction))
        //            {
        //                command.Parameters.Add("@AccountName", SqlDbType.NVarChar, 200)
        //                    .Value = supplier.SupName.Trim();

        //                command.Parameters.Add("@SwiftCode", SqlDbType.NVarChar, 50)
        //                    .Value = string.IsNullOrWhiteSpace(supplier.SwiftCode)
        //                        ? DBNull.Value
        //                        : supplier.SwiftCode.Trim();

        //                command.Parameters.Add("@AccountId", SqlDbType.Int)
        //                    .Value = accountId;

        //                if (await command.ExecuteNonQueryAsync() != 1)
        //                {
        //                    throw new InvalidOperationException(
        //                        "The linked supplier ledger account could not be updated.");
        //                }
        //            }

        //            // 4. Save/update opening balance in OPN.
        //            await UpdateSupplierOpeningBalanceAsync(
        //                connection,
        //                transaction,
        //                accountId,
        //                supplier.OpenBal,
        //                supplier.CrDr,
        //                supplier.CurFlg,
        //                supplier.CurCode,
        //                supplier.CurConvRate,
        //                userId,
        //                branchCode);

        //            // 5. Keep ACCMAS opening amount and currency in sync.
        //            await UpdateSupplierAccmasOpeningAsync(
        //                connection,
        //                transaction,
        //                accountId,
        //                supplier.OpenBal,
        //                supplier.CurCode,
        //                supplier.CurConvRate);

        //            // 6. Update existing bills by ID, insert new bills, and delete
        //            //    omitted opening bills. Everything is in the same transaction.
        //            await UpdateSupplierPendingBillsAsync(
        //                connection,
        //                transaction,
        //                accountId,
        //                supplier.CurCode,
        //                supplier.CurConvRate,
        //                userId,
        //                branchCode,
        //                supplier.PendingBills);

        //            await transaction.CommitAsync();
        //            return true;
        //        }
        //        catch
        //        {
        //            try
        //            {
        //                await transaction.RollbackAsync();
        //            }
        //            catch
        //            {
        //                // Preserve the original exception.
        //            }

        //            throw;
        //        }
        //    }


        public async Task<bool> UpdateSupplier(
       int id,
       SupplierEditRequest supplier,
       byte[] originalRowVersion,
       int userId)
        {
            if (originalRowVersion == null || originalRowVersion.Length != 8)
                throw new ArgumentException("A valid 8-byte RowVersion is required.");

            await using var connection = db.GetConnection();

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();
            await using var transaction =
                    (SqlTransaction)await connection.BeginTransactionAsync();
            //await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                const int branchCode = 0;

                // 1. Lock supplier and verify RowVersion.
                const string accountSql = @"
            SELECT ACCID
            FROM dbo.SUPMAS WITH (UPDLOCK, HOLDLOCK)
            WHERE ID = @Id
              AND ISNULL(DELFLG, 0) = 0
              AND RowVersion = @OriginalRowVersion;";

                int? accountIdResult = await connection.QueryFirstOrDefaultAsync<int?>(
                    accountSql,
                    new
                    {
                        Id = id,
                        OriginalRowVersion = originalRowVersion
                    },
                    transaction);

                if (!accountIdResult.HasValue)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                int accountId = accountIdResult.Value;

                if (accountId <= 0)
                    throw new InvalidOperationException(
                        "Supplier has no valid linked ledger account.");

                // 2. Validate opening balance and pending bills.
                await ValidateSupplierBalancesAsync(
                    connection,
                    transaction,
                    accountId,
                    supplier.OpenBal,
                    supplier.CrDr,
                    supplier.CurFlg,
                    supplier.CurCode,
                    supplier.CurConvRate,
                    branchCode,
                    supplier.PendingBills);

                // 3. Update SUPMAS directly.
                const string supplierSql = @"
            UPDATE dbo.SUPMAS
            SET
                ALIAS = @Alias,
                SUPNAME = @SupName,
                ADDRESS = @Address,
                PHONE = @Phone,
                EMAIL = @Email,
                ACTIVE = @Active,
                LOGCD = @LogCd,
                PRINTNAME = @PrintName,
                STATECODE = @StateCode,
                COUNTRYCODE = @CountryCode,
                BNKACCNAME = @BnkAccName,
                BNKACCNO = @BnkAccNo,
                BRANCH = @Branch,
                IBANNO = @IbanNo,
                VATFLAG = @VatFlag,
                BILLFLAG = @BillFlag,
                CRPERFLAG = @CrPerFlag,
                CRLIMITFLAG = @CrLimitFlag,
                CRLIMIT = @CrLimit,
                UTIME = GETDATE(),
                VATREGNO = @VatRegNo,
                SHIPADDRESS = @ShipAddress,
                SECNAME = @SecName,
                CURFLG = @CurFlg,
                CRPERIOD = @CrPeriod,
                GRPITEM = 0,
                ITEMGRPCD = 0,
                GCCFLG = @GccFlg,
                STREETONE = @StreetOne,
                STREETTWO = @StreetTwo,
                BULDINGNO = @BuildingNo,
                PLOTID = @PlotId,
                CITYNAME = @CityName,
                POSTALCODE = @PostalCode,
                DISTRICT = @District,
                GRPVATNO = @GrpVatNo,
                EINVIDTYPE = @EinvIdType,
                EINVID = @EinvId,
                VATTYPE = @VatType,
                PARTYGRPID = @PartyGrpId
           
            WHERE ID = @Id
              AND ISNULL(DELFLG, 0) = 0
              AND RowVersion = @OriginalRowVersion;";
                //CITYNAMESEC = @CityNameSec,
                //DISTRICTSEC = @DistrictSec
                int supplierRows = await connection.ExecuteAsync(
                    supplierSql,
                    new
                    {
                        Id = id,
                        OriginalRowVersion = originalRowVersion,

                        supplier.Alias,
                        supplier.SupName,
                        supplier.Address,
                        supplier.Phone,
                        supplier.Email,
                        supplier.Active,

                        LogCd = userId.ToString(),

                        supplier.PrintName,
                        supplier.StateCode,
                        supplier.CountryCode,

                        supplier.BnkAccName,
                        supplier.BnkAccNo,
                        supplier.Branch,
                        supplier.IbanNo,

                        supplier.VatFlag,
                        supplier.BillFlag,
                        supplier.CrPerFlag,
                        supplier.CrLimitFlag,
                        supplier.CrLimit,

                        supplier.VatRegNo,
                        supplier.ShipAddress,
                        supplier.SecName,
                        supplier.CurFlg,
                        supplier.CrPeriod,
                        //supplier.GrpItem,
                        //supplier.ItemGroupId,
                        supplier.GccFlg,

                        supplier.StreetOne,
                        supplier.StreetTwo,
                        BuildingNo = supplier.BuildingNo,
                        supplier.PlotId,
                        supplier.CityName,
                        supplier.PostalCode,
                        supplier.District,

                        supplier.GrpVatNo,
                        supplier.EinvIdType,
                        supplier.EinvId,
                        supplier.VatType,
                        supplier.PartyGrpId,
                        //supplier.CityNameSec,
                        //supplier.DistrictSec
                    },
                    transaction);

                if (supplierRows != 1)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                // 4. Update linked ACCMAS account.
                const string accountUpdateSql = @"
            UPDATE dbo.ACCMAS
            SET ACCNAME = @AccountName,
                SWIFTCODE = @SwiftCode
            WHERE ID = @AccountId;";

                int accountRows = await connection.ExecuteAsync(
                    accountUpdateSql,
                    new
                    {
                        AccountName = supplier.SupName.Trim(),
                        supplier.SwiftCode,
                        AccountId = accountId
                    },
                    transaction);

                if (accountRows != 1)
                    throw new InvalidOperationException(
                        "The linked supplier ledger account could not be updated.");

                // 5. Update OPN opening balance.
                await UpdateSupplierOpeningBalanceAsync(
                    connection,
                    transaction,
                    accountId,
                    supplier.OpenBal,
                    supplier.CrDr,
                    supplier.CurFlg,
                    supplier.CurCode,
                    supplier.CurConvRate,
                    userId,
                    branchCode);

                // 6. Update ACCMAS opening details.
                await UpdateSupplierAccmasOpeningAsync(
                    connection,
                    transaction,
                    accountId,
                    supplier.OpenBal,
                    supplier.CurCode,
                    supplier.CurConvRate);

                // 7. Update pending bills.
                await UpdateSupplierPendingBillsAsync(
                    connection,
                    transaction,
                    accountId,
                    supplier.CurCode,
                    supplier.CurConvRate,
                    userId,
                    branchCode,
                    supplier.PendingBills);

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                try
                {
                    await transaction.RollbackAsync();
                }
                catch
                {
                    // Preserve original exception.
                }

                throw;
            }
        }
        //public async Task<bool> UpdateSupplier(
        //    int id,
        //    clsSupplier supplier,
        //    byte[] originalRowVersion,
        //    int userId)
        //{
        //    if (originalRowVersion == null || originalRowVersion.Length != 8)
        //        throw new ArgumentException("A valid 8-byte RowVersion is required.");

        //    await using var connection = db.GetConnection();

        //    if (connection.State != ConnectionState.Open)
        //        await connection.OpenAsync();

        //    await using var transaction =
        //        (SqlTransaction)await connection.BeginTransactionAsync();

        //    try
        //    {
        //        const int branchCode = 0;

        //        // 1. Lock supplier and verify RowVersion.
        //        const string accountSql = @"
        //    SELECT ACCID
        //    FROM dbo.SUPMAS WITH (UPDLOCK, HOLDLOCK)
        //    WHERE ID = @Id
        //      AND ISNULL(DELFLG, 0) = 0
        //      AND RowVersion = @OriginalRowVersion;";

        //        int accountId;

        //        await using (var command =
        //            new SqlCommand(accountSql, connection, transaction))
        //        {
        //            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        //            command.Parameters.Add("@OriginalRowVersion",
        //                SqlDbType.Binary, 8).Value = originalRowVersion;

        //            object? result = await command.ExecuteScalarAsync();

        //            if (result == null || result == DBNull.Value)
        //            {
        //                await transaction.RollbackAsync();
        //                return false;
        //            }

        //            accountId = Convert.ToInt32(result);
        //        }

        //        if (accountId <= 0)
        //            throw new InvalidOperationException(
        //                "Supplier has no valid linked ledger account.");

        //        // 2. Validate the final opening balance and pending bills.
        //        // This must happen before changing any master or financial records.
        //        await ValidateSupplierBalancesAsync(
        //            connection,
        //            transaction,
        //            accountId,
        //            supplier.OpenBal,
        //            supplier.CrDr,
        //            supplier.CurFlg,
        //            supplier.CurCode,
        //            supplier.CurConvRate,
        //            branchCode,
        //            supplier.PendingBills);

        //        // 3. Update supplier master.
        //        var updateFields = Fields
        //            .Where(f =>
        //                !string.Equals(f.Column, "ACCID",
        //                    StringComparison.OrdinalIgnoreCase) &&
        //                !string.Equals(f.Column, "RowVersion",
        //                    StringComparison.OrdinalIgnoreCase))
        //            .ToList();

        //        if (updateFields.Count == 0)
        //            throw new InvalidOperationException(
        //                "No supplier fields are configured for updating.");

        //        string setClause = string.Join(
        //            ", ",
        //            updateFields.Select(f => $"[{f.Column}] = @{f.Column}"));

        //        string supplierSql = $@"
        //    UPDATE dbo.SUPMAS
        //    SET {setClause}
        //    WHERE ID = @Id
        //      AND ISNULL(DELFLG, 0) = 0
        //      AND RowVersion = @OriginalRowVersion;";

        //        await using (var command =
        //            new SqlCommand(supplierSql, connection, transaction))
        //        {
        //            AddSupplierParameters(command, supplier);

        //            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        //            command.Parameters.Add("@OriginalRowVersion",
        //                SqlDbType.Binary, 8).Value = originalRowVersion;

        //            if (await command.ExecuteNonQueryAsync() != 1)
        //            {
        //                await transaction.RollbackAsync();
        //                return false;
        //            }
        //        }

        //        // 4. Update linked ACCMAS account.
        //        const string accountUpdateSql = @"
        //    UPDATE dbo.ACCMAS
        //    SET ACCNAME = @AccountName,
        //        SWIFTCODE = @SwiftCode
        //    WHERE ID = @AccountId;";

        //        await using (var command =
        //            new SqlCommand(accountUpdateSql, connection, transaction))
        //        {
        //            command.Parameters.Add("@AccountName",
        //                SqlDbType.NVarChar, 200).Value = supplier.SupName.Trim();

        //            command.Parameters.Add("@SwiftCode",
        //                SqlDbType.NVarChar, 50).Value =
        //                string.IsNullOrWhiteSpace(supplier.SwiftCode)
        //                    ? DBNull.Value
        //                    : supplier.SwiftCode.Trim();

        //            command.Parameters.Add("@AccountId", SqlDbType.Int).Value =
        //                accountId;

        //            if (await command.ExecuteNonQueryAsync() != 1)
        //                throw new InvalidOperationException(
        //                    "The linked supplier ledger account could not be updated.");
        //        }

        //        // 5. Update OPN opening balance.
        //        await UpdateSupplierOpeningBalanceAsync(
        //            connection,
        //            transaction,
        //            accountId,
        //            supplier.OpenBal,
        //            supplier.CrDr,
        //            supplier.CurFlg,
        //            supplier.CurCode,
        //            supplier.CurConvRate,
        //            userId,
        //            branchCode);

        //        // 6. Keep ACCMAS opening details in sync.
        //        await UpdateSupplierAccmasOpeningAsync(
        //            connection,
        //            transaction,
        //            accountId,
        //            supplier.OpenBal,
        //            supplier.CurCode,
        //            supplier.CurConvRate);

        //        // 7. Replace pending bills with the submitted list.
        //        // Existing IDs are updated, new rows inserted, omitted rows deleted.
        //        await UpdateSupplierPendingBillsAsync(
        //            connection,
        //            transaction,
        //            accountId,
        //            supplier.CurCode,
        //            supplier.CurConvRate,
        //            userId,
        //            branchCode,
        //            supplier.PendingBills);

        //        await transaction.CommitAsync();
        //        return true;
        //    }
        //    catch
        //    {
        //        try
        //        {
        //            await transaction.RollbackAsync();
        //        }
        //        catch
        //        {
        //            // Preserve the original exception.
        //        }

        //        throw;
        //    }
        //}




        private static async Task ValidateSupplierBalancesAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            int accountId,
            decimal openingAmount,
            string crDr,
            bool currencyEnabled,
            int currencyCode,
            decimal conversionRate,
            int branchId,
            List<PendingBillRequest>? bills)
        {
            if (accountId <= 0)
                throw new ArgumentException("Invalid supplier account ID.");

            if (openingAmount < 0)
                throw new ArgumentException(
                    "Opening balance cannot be negative.");

            if (currencyEnabled && currencyCode <= 0)
                throw new ArgumentException(
                    "A valid currency is required when multicurrency is enabled.");

            if (conversionRate <= 0)
                throw new ArgumentException(
                    "Conversion rate must be greater than zero.");

            if (!currencyEnabled && currencyCode < 0)
                throw new ArgumentException("Currency code cannot be negative.");

            if (string.IsNullOrWhiteSpace(crDr))
                throw new ArgumentException("Opening balance type is required.");

            string openingDrCr = crDr.Trim().ToUpperInvariant();

            if (openingDrCr != "DR" && openingDrCr != "CR")
                throw new ArgumentException(
                    "Opening balance type must be DR or CR.");

            if (bills == null)
                throw new ArgumentException(
                    "PendingBills is required. Send [] when there are no pending bills.");

            // Lock the existing pending bill rows while validating their IDs.
            const string existingSql = @"
        SELECT ID
        FROM dbo.PENDBIL WITH (UPDLOCK, HOLDLOCK)
        WHERE CODE = @AccountId
          AND INVTYPE = 'SB'
          AND BRCD = @BranchId;";

            var existingIds = new HashSet<int>();

            await using (var command =
                new SqlCommand(existingSql, connection, transaction))
            {
                command.Parameters.Add("@AccountId", SqlDbType.Int).Value =
                    accountId;

                command.Parameters.Add("@BranchId", SqlDbType.Int).Value =
                    branchId;

                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                    existingIds.Add(reader.GetInt32(0));
            }

            var requestedIds = bills
                .Where(b => b.Id.Value>0 && b.Id.Value > 0)
                .Select(b => b.Id!.Value)
                .ToList();

            if (requestedIds.Count != requestedIds.Distinct().Count())
                throw new ArgumentException(
                    "Duplicate pending bill IDs are not allowed.");

            foreach (int requestedId in requestedIds)
            {
                if (!existingIds.Contains(requestedId))
                    throw new ArgumentException(
                        $"Pending bill ID {requestedId} does not belong to this supplier account.");
            }

            decimal openingBaseAmount = currencyEnabled
                ? openingAmount * conversionRate
                : openingAmount;

            decimal openingDebit = openingDrCr == "DR"
                ? openingBaseAmount
                : 0m;

            decimal openingCredit = openingDrCr == "CR"
                ? openingBaseAmount
                : 0m;

            decimal pendingDebit = 0m;
            decimal pendingCredit = 0m;

            foreach (var bill in bills)
            {
                if (string.IsNullOrWhiteSpace(bill.InvoiceNumber))
                    throw new ArgumentException(
                        "Pending bill invoice number is required.");

                if (bill.Amount <= 0)
                    throw new ArgumentException(
                        "Pending bill amount must be greater than zero.");

                if (bill.DueDate < bill.InvoiceDate)
                    throw new ArgumentException(
                        $"Due date cannot be earlier than invoice date for invoice {bill.InvoiceNumber}.");

                if (string.IsNullOrWhiteSpace(bill.DrCr))
                    throw new ArgumentException(
                        $"Debit/Credit type is required for invoice {bill.InvoiceNumber}.");

                string billDrCr = bill.DrCr.Trim().ToUpperInvariant();

                if (billDrCr != "DR" && billDrCr != "CR")
                    throw new ArgumentException(
                        $"Invalid Debit/Credit type for invoice {bill.InvoiceNumber}. Use DR or CR.");

                decimal billBaseAmount = currencyEnabled
                    ? bill.Amount * conversionRate
                    : bill.Amount;

                if (billDrCr == "DR")
                    pendingDebit += billBaseAmount;
                else
                    pendingCredit += billBaseAmount;
            }

            if (pendingDebit > openingDebit)
            {
                throw new ArgumentException(
                    $"Total pending debit bills ({pendingDebit:N2}) cannot exceed the opening debit balance ({openingDebit:N2}).");
            }

            if (pendingCredit > openingCredit)
            {
                throw new ArgumentException(
                    $"Total pending credit bills ({pendingCredit:N2}) cannot exceed the opening credit balance ({openingCredit:N2}).");
            }
        }



        private static async Task UpdateSupplierOpeningBalanceAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            int accountId,
            decimal amount,
            string crDr,
            bool currencyEnabled,
            int currencyCode,
            decimal conversionRate,
            int userId,
            int branchId)
        {
            if (accountId <= 0)
                throw new ArgumentException("Invalid supplier account ID.");

            if (amount < 0)
                throw new ArgumentException(
                    "Opening balance cannot be negative.");

            if (currencyEnabled && currencyCode <= 0)
                throw new ArgumentException(
                    "A valid opening balance currency is required.");

            if (currencyCode < 0)
                throw new ArgumentException("Currency code cannot be negative.");

            if (conversionRate <= 0)
                throw new ArgumentException(
                    "Conversion rate must be greater than zero.");

            string normalizedDrCr = crDr.Trim().ToUpperInvariant();

            if (normalizedDrCr != "DR" && normalizedDrCr != "CR")
                throw new ArgumentException("DrCr must be DR or CR.");

            decimal baseAmount = currencyEnabled
                ? amount * conversionRate
                : amount;

            decimal debit = normalizedDrCr == "DR" ? baseAmount : 0m;
            decimal credit = normalizedDrCr == "CR" ? baseAmount : 0m;

            const string findSql = @"
        SELECT TOP (1) ID
        FROM dbo.OPN WITH (UPDLOCK, HOLDLOCK)
        WHERE ACCID = @AccountId
          AND BRCD = @BranchId
          AND CURCODE = @CurrencyCode
        ORDER BY ID;";

            int? openingId = null;

            await using (var command =
                new SqlCommand(findSql, connection, transaction))
            {
                command.Parameters.Add("@AccountId", SqlDbType.Int).Value =
                    accountId;

                command.Parameters.Add("@BranchId", SqlDbType.Int).Value =
                    branchId;

                command.Parameters.Add("@CurrencyCode", SqlDbType.Int).Value =
                    currencyCode;

                object? result = await command.ExecuteScalarAsync();

                if (result != null && result != DBNull.Value)
                    openingId = Convert.ToInt32(result);
            }

            string sql = openingId.HasValue
                ? @"
            UPDATE dbo.OPN
            SET DEBIT = @Debit,
                CREDIT = @Credit,
                CURCONVRATE = @ConversionRate,
                CURCODE = @CurrencyCode,
                LOGCD = @LogCode,
                UTIME = GETDATE()
            WHERE ID = @Id
              AND ACCID = @AccountId
              AND BRCD = @BranchId;"
                : @"
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

            await using var saveCommand =
                new SqlCommand(sql, connection, transaction);

            saveCommand.Parameters.Add("@AccountId", SqlDbType.Int).Value =
                accountId;

            var debitParam = saveCommand.Parameters.Add("@Debit", SqlDbType.Decimal);
            debitParam.Precision = 18;
            debitParam.Scale = 2;
            debitParam.Value = debit;

            var creditParam = saveCommand.Parameters.Add("@Credit", SqlDbType.Decimal);
            creditParam.Precision = 18;
            creditParam.Scale = 2;
            creditParam.Value = credit;

            saveCommand.Parameters.Add("@LogCode", SqlDbType.Int).Value = userId;

            var rateParam = saveCommand.Parameters.Add(
                "@ConversionRate", SqlDbType.Decimal);
            rateParam.Precision = 18;
            rateParam.Scale = 6;
            rateParam.Value = conversionRate;

            saveCommand.Parameters.Add("@CurrencyCode", SqlDbType.Int).Value =
                currencyCode;

            saveCommand.Parameters.Add("@BranchId", SqlDbType.Int).Value =
                branchId;

            if (openingId.HasValue)
            {
                saveCommand.Parameters.Add("@Id", SqlDbType.Int).Value =
                    openingId.Value;
            }

            await saveCommand.ExecuteNonQueryAsync();
        }



        //private static async Task UpdateSupplierOpeningBalanceAsync(
        //SqlConnection connection,
        //SqlTransaction transaction,
        //int accountId,
        //decimal amount,
        //string crDr,
        //bool currencyEnabled,
        //int currencyCode,
        //decimal conversionRate,
        //int userId,
        //int branchId)
        //{
        //    if (accountId <= 0)
        //        throw new ArgumentException("Invalid supplier account ID.");

        //    if (amount < 0)
        //        throw new ArgumentException("Opening balance cannot be negative.");

        //    if (currencyCode <= 0)
        //        throw new ArgumentException("A valid currency is required.");

        //    if (conversionRate <= 0)
        //        throw new ArgumentException("Conversion rate must be greater than zero.");

        //    string normalizedDrCr = crDr.Trim().ToUpperInvariant();

        //    if (normalizedDrCr != "DR" && normalizedDrCr != "CR")
        //        throw new ArgumentException("DrCr must be Dr or Cr.");

        //    // Confirm this conversion rule against your legacy accounting logic.
        //    decimal baseAmount = currencyEnabled
        //        ? amount * conversionRate
        //        : amount;

        //    decimal debit = normalizedDrCr == "DR" ? baseAmount : 0m;
        //    decimal credit = normalizedDrCr == "CR" ? baseAmount : 0m;

        //    const string findSql = @"
        //SELECT TOP (1) ID
        //FROM dbo.OPN WITH (UPDLOCK, HOLDLOCK)
        //WHERE ACCID = @AccountId
        //  AND BRCD = @BranchId
        //  AND CURCODE = @CurrencyCode
        //ORDER BY ID;";

        //    int? openingId = null;

        //    await using (var command =
        //        new SqlCommand(findSql, connection, transaction))
        //    {
        //        command.Parameters.Add("@AccountId", SqlDbType.Int).Value = accountId;
        //        command.Parameters.Add("@BranchId", SqlDbType.Int).Value = branchId;
        //        command.Parameters.Add("@CurrencyCode", SqlDbType.Int).Value = currencyCode;

        //        object? result = await command.ExecuteScalarAsync();

        //        if (result != null && result != DBNull.Value)
        //            openingId = Convert.ToInt32(result);
        //    }

        //    string sql = openingId.HasValue
        //        ? @"
        //    UPDATE dbo.OPN
        //    SET DEBIT = @Debit,
        //        CREDIT = @Credit,
        //        CURCONVRATE = @ConversionRate,
        //        CURCODE = @CurrencyCode,
        //        LOGCD = @LogCode,
        //        UTIME = GETDATE()
        //    WHERE ID = @Id
        //      AND ACCID = @AccountId
        //      AND BRCD = @BranchId;"
        //        : @"
        //    INSERT INTO dbo.OPN
        //    (
        //        ACCID, DEBIT, CREDIT, LOGCD, UTIME,
        //        CURCONVRATE, CURCODE, BRCD
        //    )
        //    VALUES
        //    (
        //        @AccountId, @Debit, @Credit, @LogCode, GETDATE(),
        //        @ConversionRate, @CurrencyCode, @BranchId
        //    );";

        //    await using var saveCommand =
        //        new SqlCommand(sql, connection, transaction);

        //    saveCommand.Parameters.Add("@AccountId", SqlDbType.Int).Value = accountId;
        //    saveCommand.Parameters.Add("@Debit", SqlDbType.Decimal).Value = debit;
        //    saveCommand.Parameters.Add("@Credit", SqlDbType.Decimal).Value = credit;
        //    saveCommand.Parameters.Add("@LogCode", SqlDbType.Int).Value = userId;
        //    saveCommand.Parameters.Add("@ConversionRate", SqlDbType.Decimal).Value =
        //        conversionRate;
        //    saveCommand.Parameters.Add("@CurrencyCode", SqlDbType.Int).Value =
        //        currencyCode;
        //    saveCommand.Parameters.Add("@BranchId", SqlDbType.Int).Value = branchId;

        //    if (openingId.HasValue)
        //    {
        //        saveCommand.Parameters.Add("@Id", SqlDbType.Int).Value =
        //            openingId.Value;
        //    }

        //    await saveCommand.ExecuteNonQueryAsync();
        //}




        private static async Task UpdateSupplierAccmasOpeningAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            int accountId,
            decimal openingBalance,
            int currencyCode,
            decimal conversionRate)
        {
            const string sql = @"
        UPDATE dbo.ACCMAS
        SET OPENBAL = @OpenBal,
            CURCODE = @CurCode,
            CURCONVRATE = @CurConvRate
        WHERE ID = @AccountId;";

            await using var command =
                new SqlCommand(sql, connection, transaction);

            command.Parameters.Add("@AccountId", SqlDbType.Int).Value =
                accountId;

            var amountParam = command.Parameters.Add(
                "@OpenBal", SqlDbType.Decimal);
            amountParam.Precision = 18;
            amountParam.Scale = 2;
            amountParam.Value = openingBalance;

            command.Parameters.Add("@CurCode", SqlDbType.Int).Value =
                currencyCode;

            var rateParam = command.Parameters.Add(
                "@CurConvRate", SqlDbType.Decimal);
            rateParam.Precision = 18;
            rateParam.Scale = 6;
            rateParam.Value = conversionRate;

            if (await command.ExecuteNonQueryAsync() != 1)
            {
                throw new InvalidOperationException(
                    "Failed to update the supplier opening balance in ACCMAS.");
            }
        }


        private static async Task UpdateSupplierPendingBillsAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            int accountId,
            int currencyCode,
            decimal conversionRate,
            int userId,
            int branchId,
            List<PendingBillRequest>? bills)
        {
            if (bills == null)
                throw new ArgumentException(
                    "PendingBills is required. Send [] when there are no bills.");

            const string selectSql = @"
        SELECT ID
        FROM dbo.PENDBIL WITH (UPDLOCK, HOLDLOCK)
        WHERE CODE = @AccountId
          AND INVTYPE = 'SB'
          AND BRCD = @BranchId;";

            var existingIds = new HashSet<int>();

            await using (var command =
                new SqlCommand(selectSql, connection, transaction))
            {
                command.Parameters.Add("@AccountId", SqlDbType.Int).Value =
                    accountId;

                command.Parameters.Add("@BranchId", SqlDbType.Int).Value =
                    branchId;

                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                    existingIds.Add(reader.GetInt32(0));
            }

            var requestedIds = bills
                .Where(b => b.Id.HasValue && b.Id.Value > 0)
                .Select(b => b.Id!.Value)
                .ToList();

            if (requestedIds.Count != requestedIds.Distinct().Count())
                throw new ArgumentException(
                    "Duplicate pending bill IDs are not allowed.");

            foreach (int requestedId in requestedIds)
            {
                if (!existingIds.Contains(requestedId))
                {
                    throw new ArgumentException(
                        $"Pending bill ID {requestedId} does not belong to this supplier account.");
                }
            }

            foreach (var bill in bills)
            {
                if (string.IsNullOrWhiteSpace(bill.InvoiceNumber))
                    throw new ArgumentException(
                        "Pending bill invoice number is required.");

                if (bill.Amount <= 0)
                    throw new ArgumentException(
                        "Pending bill amount must be greater than zero.");

                if (bill.DueDate.Date < bill.InvoiceDate.Date)
                    throw new ArgumentException(
                        "Pending bill due date cannot be earlier than invoice date.");

                string drCr = bill.DrCr.Trim().ToUpperInvariant();

                if (drCr != "DR" && drCr != "CR")
                    throw new ArgumentException(
                        "Pending bill DrCr must be DR or CR.");

                bool updateExisting =
                    bill.Id.HasValue && bill.Id.Value > 0;

                string sql = updateExisting
                    ? @"
                UPDATE dbo.PENDBIL
                SET INVDATE = @InvDate,
                    NARRATION = @Narration,
                    INVNO = @InvNo,
                    AMOUNT = @Amount,
                    DUEDATE = @DueDate,
                    LOGCD = @LogCode,
                    UTIME = GETDATE(),
                    CURCONVRATE = @ConversionRate,
                    CURCODE = @CurrencyCode,
                    DRCR = @DrCr
                WHERE ID = @Id
                  AND CODE = @AccountId
                  AND INVTYPE = 'SB'
                  AND BRCD = @BranchId;"
                    : @"
                INSERT INTO dbo.PENDBIL
                (
                    INVDATE, NARRATION, INVNO, AMOUNT, DUEDATE,
                    INVTYPE, CODE, LOGCD, UTIME, BILLREFFLG,
                    CURCONVRATE, CURCODE, BRCD, DRCR
                )
                VALUES
                (
                    @InvDate, @Narration, @InvNo, @Amount, @DueDate,
                    'SB', @AccountId, @LogCode, GETDATE(), @BillRefFlg,
                    @ConversionRate, @CurrencyCode, @BranchId, @DrCr
                );";

                await using var saveCommand =
                    new SqlCommand(sql, connection, transaction);

                if (updateExisting)
                {
                    saveCommand.Parameters.Add("@Id", SqlDbType.Int).Value =
                        bill.Id!.Value;
                }

                saveCommand.Parameters.Add("@AccountId", SqlDbType.Int).Value =
                    accountId;

                saveCommand.Parameters.Add("@BranchId", SqlDbType.Int).Value =
                    branchId;

                saveCommand.Parameters.Add("@InvDate", SqlDbType.Date).Value =
                    bill.InvoiceDate.Date;

                saveCommand.Parameters.Add("@Narration",
                    SqlDbType.NVarChar, -1).Value =
                    (object?)bill.Narration ?? DBNull.Value;

                saveCommand.Parameters.Add("@InvNo",
                    SqlDbType.NVarChar, 100).Value =
                    bill.InvoiceNumber.Trim();

                var amountParam = saveCommand.Parameters.Add(
                    "@Amount", SqlDbType.Decimal);
                amountParam.Precision = 18;
                amountParam.Scale = 2;
                amountParam.Value = bill.Amount;

                saveCommand.Parameters.Add("@DueDate", SqlDbType.Date).Value =
                    bill.DueDate.Date;

                saveCommand.Parameters.Add("@LogCode", SqlDbType.Int).Value =
                    userId;

                var rateParam = saveCommand.Parameters.Add(
                    "@ConversionRate", SqlDbType.Decimal);
                rateParam.Precision = 18;
                rateParam.Scale = 6;
                rateParam.Value = conversionRate;

                saveCommand.Parameters.Add("@CurrencyCode", SqlDbType.Int).Value =
                    currencyCode;

                saveCommand.Parameters.Add("@DrCr", SqlDbType.NVarChar, 2).Value =
                    drCr == "DR" ? "Dr" : "Cr";

                if (!updateExisting)
                {
                    saveCommand.Parameters.Add("@BillRefFlg", SqlDbType.Bit).Value =
                        false;
                }

                int affected = await saveCommand.ExecuteNonQueryAsync();

                if (updateExisting && affected != 1)
                {
                    throw new InvalidOperationException(
                        $"Failed to update pending bill ID {bill.Id}.");
                }
            }

            // Delete existing opening bills omitted from the submitted list.
            var idsToDelete = existingIds.Except(requestedIds).ToList();

            if (idsToDelete.Count > 0)
            {
                const string deleteSql = @"
            DELETE FROM dbo.PENDBIL
            WHERE ID = @Id
              AND CODE = @AccountId
              AND INVTYPE = 'SB'
              AND BRCD = @BranchId;";

                foreach (int oldId in idsToDelete)
                {
                    await using var deleteCommand =
                        new SqlCommand(deleteSql, connection, transaction);

                    deleteCommand.Parameters.Add("@Id", SqlDbType.Int).Value =
                        oldId;

                    deleteCommand.Parameters.Add("@AccountId", SqlDbType.Int).Value =
                        accountId;

                    deleteCommand.Parameters.Add("@BranchId", SqlDbType.Int).Value =
                        branchId;

                    await deleteCommand.ExecuteNonQueryAsync();
                }
            }
        }


        //    private static async Task UpdateSupplierAccmasOpeningAsync(
        //SqlConnection connection,
        //SqlTransaction transaction,
        //int accountId,
        //decimal openingBalance,
        //int currencyCode,
        //decimal conversionRate)
        //    {
        //        const string sql = @"
        //    UPDATE dbo.ACCMAS
        //    SET OPENBAL = @OpenBal,
        //        CURCODE = @CurCode,
        //        CURCONVRATE = @CurConvRate
        //    WHERE ID = @AccountId;";

        //        await using var command =
        //            new SqlCommand(sql, connection, transaction);

        //        command.Parameters.Add("@AccountId", SqlDbType.Int).Value = accountId;
        //        command.Parameters.Add("@OpenBal", SqlDbType.Decimal).Value = openingBalance;
        //        command.Parameters.Add("@CurCode", SqlDbType.Int).Value = currencyCode;
        //        command.Parameters.Add("@CurConvRate", SqlDbType.Decimal).Value =
        //            conversionRate;

        //        if (await command.ExecuteNonQueryAsync() != 1)
        //        {
        //            throw new InvalidOperationException(
        //                "Failed to update the supplier opening balance in ACCMAS.");
        //        }
        //    }



        //    private static async Task UpdateSupplierPendingBillsAsync(
        //SqlConnection connection,
        //SqlTransaction transaction,
        //int accountId,
        //int currencyCode,
        //decimal conversionRate,
        //int userId,
        //int branchId,
        //List<PendingBillRequest>? bills)
        //    {
        //        if (bills == null)
        //        {
        //            throw new ArgumentException(
        //                "PendingBills is required. Send an empty list if there are no bills.");
        //        }

        //        // Read and lock the existing supplier opening bills.
        //        const string selectSql = @"
        //    SELECT ID
        //    FROM dbo.PENDBIL WITH (UPDLOCK, HOLDLOCK)
        //    WHERE CODE = @AccountId
        //      AND INVTYPE = 'SB'
        //      AND BRCD = @BranchId;";

        //        var existingIds = new HashSet<int>();

        //        await using (var command =
        //            new SqlCommand(selectSql, connection, transaction))
        //        {
        //            command.Parameters.Add("@AccountId", SqlDbType.Int).Value = accountId;
        //            command.Parameters.Add("@BranchId", SqlDbType.Int).Value = branchId;

        //            await using var reader = await command.ExecuteReaderAsync();

        //            while (await reader.ReadAsync())
        //                existingIds.Add(reader.GetInt32(0));
        //        }

        //        var requestedIds = bills
        //            .Where(b => b.Id.HasValue && b.Id.Value > 0)
        //            .Select(b => b.Id!.Value)
        //            .ToList();

        //        if (requestedIds.Count != requestedIds.Distinct().Count())
        //        {
        //            throw new ArgumentException(
        //                "Duplicate pending bill IDs are not allowed.");
        //        }

        //        foreach (int requestedId in requestedIds)
        //        {
        //            if (!existingIds.Contains(requestedId))
        //            {
        //                throw new ArgumentException(
        //                    $"Pending bill ID {requestedId} does not belong to this supplier account.");
        //            }
        //        }

        //        foreach (var bill in bills)
        //        {
        //            if (string.IsNullOrWhiteSpace(bill.InvNo))
        //                throw new ArgumentException("Pending bill invoice number is required.");

        //            if (bill.Amount <= 0)
        //                throw new ArgumentException("Pending bill amount must be greater than zero.");

        //            if (bill.DueDate.Date < bill.InvDate.Date)
        //                throw new ArgumentException("Pending bill due date cannot precede its invoice date.");

        //            string drCr = bill.DrCr.Trim().ToUpperInvariant();

        //            if (drCr != "CR" && drCr != "DR")
        //                throw new ArgumentException("Pending bill DrCr must be Cr or Dr.");

        //            bool updateExisting = bill.Id.HasValue && bill.Id.Value > 0;

        //            string sql = updateExisting
        //                ? @"
        //            UPDATE dbo.PENDBIL
        //            SET INVDATE = @InvDate,
        //                NARRATION = @Narration,
        //                INVNO = @InvNo,
        //                AMOUNT = @Amount,
        //                DUEDATE = @DueDate,
        //                LOGCD = @LogCode,
        //                UTIME = GETDATE(),
        //                CURCONVRATE = @ConversionRate,
        //                CURCODE = @CurrencyCode,
        //                DRCR = @DrCr
        //            WHERE ID = @Id
        //              AND CODE = @AccountId
        //              AND INVTYPE = 'SB'
        //              AND BRCD = @BranchId;"
        //                : @"
        //            INSERT INTO dbo.PENDBIL
        //            (
        //                INVDATE, NARRATION, INVNO, AMOUNT, DUEDATE,
        //                INVTYPE, CODE, LOGCD, UTIME, BILLREFFLG,
        //                CURCONVRATE, CURCODE, BRCD, DRCR
        //            )
        //            VALUES
        //            (
        //                @InvDate, @Narration, @InvNo, @Amount, @DueDate,
        //                'SB', @AccountId, @LogCode, GETDATE(), @BillRefFlg,
        //                @ConversionRate, @CurrencyCode, @BranchId, @DrCr
        //            );";

        //            await using var saveCommand =
        //                new SqlCommand(sql, connection, transaction);

        //            if (updateExisting)
        //            {
        //                saveCommand.Parameters.Add("@Id", SqlDbType.Int).Value =
        //                    bill.Id!.Value;
        //            }

        //            saveCommand.Parameters.Add("@AccountId", SqlDbType.Int).Value = accountId;
        //            saveCommand.Parameters.Add("@BranchId", SqlDbType.Int).Value = branchId;
        //            saveCommand.Parameters.Add("@InvDate", SqlDbType.Date).Value = bill.InvDate.Date;

        //            // Change the type/length if your database column is VARCHAR
        //            // or has a smaller maximum length.
        //            saveCommand.Parameters.Add("@Narration", SqlDbType.NVarChar, -1).Value =
        //                (object?)bill.Narration ?? DBNull.Value;

        //            saveCommand.Parameters.Add("@InvNo", SqlDbType.NVarChar, 100).Value =
        //                bill.InvNo.Trim();

        //            saveCommand.Parameters.Add("@Amount", SqlDbType.Decimal).Value = bill.Amount;
        //            saveCommand.Parameters.Add("@DueDate", SqlDbType.Date).Value = bill.DueDate.Date;
        //            saveCommand.Parameters.Add("@LogCode", SqlDbType.Int).Value = userId;
        //            saveCommand.Parameters.Add("@ConversionRate", SqlDbType.Decimal).Value =
        //                conversionRate;
        //            saveCommand.Parameters.Add("@CurrencyCode", SqlDbType.Int).Value =
        //                currencyCode;
        //            saveCommand.Parameters.Add("@DrCr", SqlDbType.NVarChar, 2).Value =
        //                drCr == "DR" ? "Dr" : "Cr";

        //            if (!updateExisting)
        //            {
        //                saveCommand.Parameters.Add("@BillRefFlg", SqlDbType.Bit).Value = false;
        //            }

        //            int affected = await saveCommand.ExecuteNonQueryAsync();

        //            if (updateExisting && affected != 1)
        //            {
        //                throw new InvalidOperationException(
        //                    $"Failed to update pending bill ID {bill.Id}.");
        //            }
        //        }

        //        // Delete existing bills omitted from the submitted replacement list.
        //        var idsToDelete = existingIds.Except(requestedIds).ToList();

        //        foreach (int oldId in idsToDelete)
        //        {
        //            const string deleteSql = @"
        //        DELETE FROM dbo.PENDBIL
        //        WHERE ID = @Id
        //          AND CODE = @AccountId
        //          AND INVTYPE = 'SB'
        //          AND BRCD = @BranchId;";

        //            await using var deleteCommand =
        //                new SqlCommand(deleteSql, connection, transaction);

        //            deleteCommand.Parameters.Add("@Id", SqlDbType.Int).Value = oldId;
        //            deleteCommand.Parameters.Add("@AccountId", SqlDbType.Int).Value = accountId;
        //            deleteCommand.Parameters.Add("@BranchId", SqlDbType.Int).Value = branchId;

        //            await deleteCommand.ExecuteNonQueryAsync();
        //        }
        //    }
        // =========================================================
        // OPN — insert or update a supplier's opening balance,
        // matching OpnDB.insert / OpnDB.UpdateByAccId / OpnDB.ValidateAcc exactly
        // =========================================================

        private static async Task SaveOrUpdateOpn(
            SqlConnection connection, SqlTransaction transaction,
            int accId, decimal openBal, string crDr, int curCode, decimal curConvRate,
            int logCd, int brCd)
        {
            decimal credit = string.Equals(crDr, "CR", StringComparison.OrdinalIgnoreCase) ? openBal : 0m;
            decimal debit = string.Equals(crDr, "CR", StringComparison.OrdinalIgnoreCase) ? 0m : openBal;

            const string checkSql = "SELECT COUNT(1) FROM OPN WHERE ACCID = @AccId AND BRCD = @BrCd;";
            using var checkCmd = new SqlCommand(checkSql, connection, transaction);
            checkCmd.Parameters.AddWithValue("@AccId", accId);
            checkCmd.Parameters.AddWithValue("@BrCd", brCd);
            bool exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;

            string sql = exists
                ? @"UPDATE OPN SET DEBIT=@Debit, CREDIT=@Credit, LOGCD=@LogCd, UTIME=@UTime,
                        CURCONVRATE=@CurConvRate, CURCODE=@CurCode
                    WHERE ACCID=@AccId AND BRCD=@BrCd;"
                : @"INSERT INTO OPN (ACCID, DEBIT, CREDIT, LOGCD, UTIME, CURCONVRATE, CURCODE, BRCD)
                    VALUES (@AccId, @Debit, @Credit, @LogCd, @UTime, @CurConvRate, @CurCode, @BrCd);";

            using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@AccId", accId);
            command.Parameters.AddWithValue("@Debit", debit);
            command.Parameters.AddWithValue("@Credit", credit);
            command.Parameters.AddWithValue("@LogCd", logCd);
            command.Parameters.AddWithValue("@UTime", DateTime.Now);
            command.Parameters.AddWithValue("@CurConvRate", curConvRate);
            command.Parameters.AddWithValue("@CurCode", curCode);
            command.Parameters.AddWithValue("@BrCd", brCd);
            await command.ExecuteNonQueryAsync();
        }

        // =========================================================
        // Keep ACCMAS.OPENBAL / CURCODE / CURCONVRATE in sync,
        // matching AccMasDB.UpdateOpening exactly
        // =========================================================

        private static async Task UpdateAccmasOpening(
            SqlConnection connection, SqlTransaction transaction,
            int accId, decimal openBal, int curCode, decimal curConvRate)
        {
            const string sql = @"
                UPDATE ACCMAS
                SET OPENBAL = @OpenBal, CURCODE = @CurCode, CURCONVRATE = @CurConvRate
                WHERE ID = @AccId;";

            using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@AccId", accId);
            command.Parameters.AddWithValue("@OpenBal", openBal);
            command.Parameters.AddWithValue("@CurCode", curCode);
            command.Parameters.AddWithValue("@CurConvRate", curConvRate);
            await command.ExecuteNonQueryAsync();
        }

        // =========================================================
        // PENDBIL — delete all supplier-opening ("SB") rows for this account,
        // re-insert the current list, matching PendBilDB.insert /
        // SaveBills() in the old SupplierMaster form
        // =========================================================
        private static async Task ReplacePendingBills(
    SqlConnection connection,
    SqlTransaction transaction,
    int accId,
    int curCode,
    decimal curConvRate,
    int logCd,
    int brCd,
    List<PendingBillRequest>? bills)
        {
            if (bills == null)
            {
                throw new InvalidOperationException(
                    "PendingBills must be provided when updating a supplier.");
            }

            // Delete only the opening pending bills managed for this
            // supplier account and branch.
            const string deleteSql = @"
        DELETE FROM dbo.PENDBIL
        WHERE CODE = @AccId
          AND INVTYPE = 'SB'
          AND BRCD = @BrCd;";

            await using (var deleteCommand = new SqlCommand(
                deleteSql, connection, transaction))
            {
                deleteCommand.Parameters.Add("@AccId", SqlDbType.Int).Value = accId;
                deleteCommand.Parameters.Add("@BrCd", SqlDbType.Int).Value = brCd;

                await deleteCommand.ExecuteNonQueryAsync();
            }

            // An empty list intentionally clears all matching opening bills.
            foreach (var bill in bills)
            {
                // The controller should already have validated these values.
                // Throwing here protects against invalid data from other callers.
                if (string.IsNullOrWhiteSpace(bill.InvoiceNumber))
                {
                    throw new InvalidOperationException(
                        "Pending bill invoice number is required.");
                }

                if (bill.Amount <= 0)
                {
                    throw new InvalidOperationException(
                        "Pending bill amount must be greater than zero.");
                }

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
                @InvDate,
                @Narration,
                @InvNo,
                @Amount,
                @DueDate,
                'SB',
                @Code,
                @LogCd,
                @UTime,
                @BillRefFlg,
                @CurConvRate,
                @CurCode,
                @BrCd,
                @DrCr
            );";

                await using var command = new SqlCommand(
                    insertSql, connection, transaction);

                command.Parameters.Add("@InvDate", SqlDbType.Date).Value =
                    bill.InvoiceDate.Date;

                command.Parameters.Add("@Narration", SqlDbType.NVarChar, -1).Value =
                    (object?)bill.Narration ?? DBNull.Value;

                command.Parameters.Add("@InvNo", SqlDbType.NVarChar, 100).Value =
                    bill.InvoiceNumber.Trim();

                var amountParameter =
                    command.Parameters.Add("@Amount", SqlDbType.Decimal);
                amountParameter.Precision = 18;
                amountParameter.Scale = 2;
                amountParameter.Value = bill.Amount;

                command.Parameters.Add("@DueDate", SqlDbType.Date).Value =
                    bill.DueDate.Date;

                command.Parameters.Add("@Code", SqlDbType.Int).Value = accId;
                command.Parameters.Add("@LogCd", SqlDbType.Int).Value = logCd;
                command.Parameters.Add("@UTime", SqlDbType.DateTime).Value = DateTime.Now;
                command.Parameters.Add("@BillRefFlg", SqlDbType.Bit).Value = false;

                var rateParameter =
                    command.Parameters.Add("@CurConvRate", SqlDbType.Decimal);
                rateParameter.Precision = 18;
                rateParameter.Scale = 6;
                rateParameter.Value = curConvRate;

                command.Parameters.Add("@CurCode", SqlDbType.Int).Value = curCode;
                command.Parameters.Add("@BrCd", SqlDbType.Int).Value = brCd;

                command.Parameters.Add("@DrCr", SqlDbType.NVarChar, 2).Value =
                    string.Equals(bill.DrCr, "Dr",
                        StringComparison.OrdinalIgnoreCase)
                        ? "Dr"
                        : "Cr";

                await command.ExecuteNonQueryAsync();
            }
        }
        //private static async Task ReplacePendingBills(
        //    SqlConnection connection, SqlTransaction transaction,
        //    int accId, int curCode, decimal curConvRate, int logCd, int brCd,
        //    List<PendingBillRequest>? bills)
        //{
        //    const string deleteSql = "DELETE FROM PENDBIL WHERE CODE = @AccId AND INVTYPE = 'SB';";
        //    using (var deleteCmd = new SqlCommand(deleteSql, connection, transaction))
        //    {
        //        deleteCmd.Parameters.AddWithValue("@AccId", accId);
        //        await deleteCmd.ExecuteNonQueryAsync();
        //    }

        //    if (bills == null) return;

        //    foreach (var bill in bills)
        //    {
        //        if (string.IsNullOrWhiteSpace(bill.InvNo) || bill.Amount <= 0)
        //            continue; // mirrors old code's break-on-missing-field validation

        //        const string insertSql = @"
        //            INSERT INTO PENDBIL
        //                (INVDATE, NARRATION, INVNO, AMOUNT, DUEDATE, INVTYPE, CODE, LOGCD, UTIME, BILLREFFLG, CURCONVRATE, CURCODE, BRCD, DRCR)
        //            VALUES
        //                (@InvDate, @Narration, @InvNo, @Amount, @DueDate, 'SB', @Code, @LogCd, @UTime, @BillRefFlg, @CurConvRate, @CurCode, @BrCd, @DrCr);";

        //        using var command = new SqlCommand(insertSql, connection, transaction);
        //        command.Parameters.AddWithValue("@InvDate", bill.InvDate);
        //        command.Parameters.AddWithValue("@Narration", bill.Narration ?? "");
        //        command.Parameters.AddWithValue("@InvNo", bill.InvNo);
        //        command.Parameters.AddWithValue("@Amount", bill.Amount);
        //        command.Parameters.AddWithValue("@DueDate", bill.DueDate);
        //        command.Parameters.AddWithValue("@Code", accId);
        //        command.Parameters.AddWithValue("@LogCd", logCd);
        //        command.Parameters.AddWithValue("@UTime", DateTime.Now);
        //        command.Parameters.AddWithValue("@BillRefFlg", false);
        //        command.Parameters.AddWithValue("@CurConvRate", curConvRate);
        //        command.Parameters.AddWithValue("@CurCode", curCode);
        //        command.Parameters.AddWithValue("@BrCd", brCd);
        //        command.Parameters.AddWithValue("@DrCr", bill.DrCr);
        //        await command.ExecuteNonQueryAsync();
        //    }
        //}


        // =========================================================
        // SOFT DELETE SUPPLIER
        // =========================================================




        public async Task<(bool Success, bool NotFound, string? Message)>
            DeleteSupplierWithValidations(int id, string logCd)
        {
            var connection = db.GetConnection();

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Find and lock supplier.
        //        const string supplierSql = """
        //SELECT ACCID
        //FROM dbo.SUPMAS WITH (UPDLOCK, HOLDLOCK)
        //WHERE ID = @Id
        //  AND ACTIVE = 1
        //  AND ISNULL(DELFLG, 0) = 0;
        //""";

        //        int accountId;

        //        using (var command = new SqlCommand(
        //            supplierSql, connection, transaction))
        //        {
        //            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        //            var result = await command.ExecuteScalarAsync();

        //            if (result == null || result == DBNull.Value)
        //            {
        //                await transaction.RollbackAsync();

        //                return (false, true,
        //                    "Supplier not found or already deleted.");
        //            }

        //            accountId = Convert.ToInt32(result);
        //        }

        //        // 2. Validate linked account.
        //        if (accountId <= 0)
        //        {
        //            await transaction.RollbackAsync();

        //            return (false, false,
        //                "Supplier does not have a valid linked ledger account.");
        //        }

        //        // 3. Check whether another active supplier shares this account.
        //        const string sharedAccountSql = """
        //SELECT COUNT_BIG(*)
        //FROM dbo.SUPMAS WITH (UPDLOCK, HOLDLOCK)
        //WHERE ACCID = @AccId
        //  AND ID <> @Id
        //  AND ACTIVE = 1
        //  AND ISNULL(DELFLG, 0) = 0;
        //""";

        //        using (var command = new SqlCommand(
        //            sharedAccountSql, connection, transaction))
        //        {
        //            command.Parameters.Add("@AccId", SqlDbType.Int).Value = accountId;
        //            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        //            long sharedSuppliers = Convert.ToInt64(
        //                await command.ExecuteScalarAsync());

        //            if (sharedSuppliers > 0)
        //            {
        //                await transaction.RollbackAsync();

        //                return (false, false,
        //                    "Cannot delete supplier. The linked ledger account is shared with another active supplier.");
        //            }
        //        }

        //        // 4. Check account references individually.
        //        // Return the actual reason preventing deletion.

        //        const string usageSql = """
        //SELECT TOP (1)
        //    Reason
        //FROM
        //(
        //    SELECT 1 AS Priority,
        //           'Supplier account has existing bill-by-bill records.' AS Reason,
        //           COUNT_BIG(*) AS RefCount
        //    FROM dbo.BILLBYBILL
        //    WHERE ACCID = @AccId

        //    UNION ALL

        //    SELECT 2, 'Supplier account is used in bank account details.',
        //           COUNT_BIG(*)
        //    FROM dbo.BNKACCDET
        //    WHERE ACCID = @AccId

        //    UNION ALL

        //    SELECT 3, 'Supplier account is linked to a branch.',
        //           COUNT_BIG(*)
        //    FROM dbo.BRMAST
        //    WHERE ACCID = @AccId

        //    UNION ALL

        //    SELECT 4, 'Supplier account is used in budget configuration.',
        //           COUNT_BIG(*)
        //    FROM dbo.BUDGETD
        //    WHERE GRPORACCID = @AccId

        //    UNION ALL

        //    SELECT 5, 'Supplier account is used in system configuration.',
        //           COUNT_BIG(*)
        //    FROM dbo.CONFIGTB
        //    WHERE RDMACCID = @AccId

        //    UNION ALL

        //    SELECT 6, 'Supplier account is used in cost centre opening balance.',
        //           COUNT_BIG(*)
        //    FROM dbo.COSTCOPNBAL
        //    WHERE ACCODE = @AccId

        //    UNION ALL

        //    SELECT 7, 'Supplier account is linked to a customer.',
        //           COUNT_BIG(*)
        //    FROM dbo.CUSMAS
        //    WHERE ACCID = @AccId

        //    UNION ALL

        //    SELECT 8, 'Supplier account is linked to a temporary customer record.',
        //           COUNT_BIG(*)
        //    FROM dbo.CUSMAS_TMP
        //    WHERE ACCID = @AccId

        //    UNION ALL

        //    SELECT 9, 'Supplier account is used in debit/credit notes.',
        //           COUNT_BIG(*)
        //    FROM dbo.DBCRNOTE
        //    WHERE ADJCD = @AccId

        //    UNION ALL

        //    SELECT 10, 'Supplier account is used as a stock account in godown master.',
        //           COUNT_BIG(*)
        //    FROM dbo.GDWMAS
        //    WHERE STKACCID = @AccId

        //    UNION ALL

        //    SELECT 11, 'Supplier account is used as a stock account in temporary godown master.',
        //           COUNT_BIG(*)
        //    FROM dbo.GDWMAS_TMP
        //    WHERE STKACCID = @AccId

        //    UNION ALL

        //    SELECT 12, 'Supplier account has an opening balance.',
        //           COUNT_BIG(*)
        //    FROM dbo.OPN
        //    WHERE ACCID = @AccId

        //    UNION ALL

        //    SELECT 13, 'Supplier account is used in production configuration.',
        //           COUNT_BIG(*)
        //    FROM dbo.PRDNM
        //    WHERE CRACC = @AccId OR DRACC = @AccId

        //    UNION ALL

        //    SELECT 14, 'Supplier account is used in product group account configuration.',
        //           COUNT_BIG(*)
        //    FROM dbo.PRGRPMAS
        //    WHERE PURACCID = @AccId
        //       OR SALESACCID = @AccId
        //       OR STOCKACCID = @AccId

        //    UNION ALL

        //    SELECT 15, 'Supplier account is used in purchase details.',
        //           COUNT_BIG(*)
        //    FROM dbo.PURD
        //    WHERE ADJCD = @AccId

        //    UNION ALL

        //    SELECT 16, 'Supplier account is used in purchase transactions.',
        //           COUNT_BIG(*)
        //    FROM dbo.PURM
        //    WHERE ADJCD = @AccId
        //       OR CRACC = @AccId
        //       OR DRACC = @AccId

        //    UNION ALL

        //    SELECT 17, 'Supplier account is used in sales details.',
        //           COUNT_BIG(*)
        //    FROM dbo.SALD
        //    WHERE ADJCD = @AccId

        //    UNION ALL

        //    SELECT 18, 'Supplier account is used in sales transactions.',
        //           COUNT_BIG(*)
        //    FROM dbo.SALM
        //    WHERE ADJCD = @AccId

        //    UNION ALL

        //    SELECT 19, 'Supplier account is linked to salesman master.',
        //           COUNT_BIG(*)
        //    FROM dbo.SMMAST
        //    WHERE ACCODE = @AccId

        //    UNION ALL

        //    SELECT 20, 'Supplier account is linked to a temporary supplier record.',
        //           COUNT_BIG(*)
        //    FROM dbo.SUPMAS_TMP
        //    WHERE ACCID = @AccId

        //    UNION ALL

        //    SELECT 21, 'Supplier account is used in pending supplier bills.',
        //           COUNT_BIG(*)
        //    FROM dbo.PENDBIL
        //    WHERE CODE = @AccId
        //      AND INVTYPE = 'SB'

        //    UNION ALL

        //    SELECT 22, 'Supplier account is used in voucher system configuration.',
        //           COUNT_BIG(*)
        //    FROM dbo.VRSYSTEM
        //    WHERE SUBACCID = @AccId

        //    UNION ALL

        //    SELECT 23, 'Supplier account is used in year voucher records.',
        //           COUNT_BIG(*)
        //    FROM dbo.YVR
        //    WHERE ADJCD = @AccId

        //) AS UsageChecks
        //WHERE RefCount > 0
        //ORDER BY Priority;
        //""";

        //        string? validationMessage;

        //        using (var command = new SqlCommand(
        //            usageSql, connection, transaction))
        //        {
        //            command.Parameters.Add("@AccId", SqlDbType.Int).Value = accountId;

        //            var result = await command.ExecuteScalarAsync();

        //            validationMessage = result == null || result == DBNull.Value
        //                ? null
        //                : Convert.ToString(result);
        //        }

        //        if (!string.IsNullOrWhiteSpace(validationMessage))
        //        {
        //            await transaction.RollbackAsync();

        //            return (false, false, validationMessage);
        //        }

                // 5. Soft delete supplier.
                const string deleteSql = """
                UPDATE dbo.SUPMAS
                SET
                    DELFLG = 1,
                    ACTIVE = 0,
                    LOGCD = @LogCd,
                    UTIME = GETDATE()
                WHERE ID = @Id
                  AND ISNULL(DELFLG, 0) = 0;
                """;

                using (var command = new SqlCommand(
                    deleteSql, connection, transaction))
                {
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                    command.Parameters.Add("@LogCd", SqlDbType.NVarChar, 255).Value = logCd;

                    int rows = await command.ExecuteNonQueryAsync();

                    if (rows != 1)
                    {
                        await transaction.RollbackAsync();

                        return (false, true,
                            "Supplier not found.");
                    }
                }

                // 6. Commit transaction.
                await transaction.CommitAsync();

                return (true, false, null);
            }
            catch
            {
                if (transaction.Connection != null)
                    await transaction.RollbackAsync();

                throw;
            }
        }


        public async Task<(bool Success, bool NotFound, bool InUse)> DeleteSupplierWithValidation(int id)
        {
            var connection = db.GetConnection();

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Find and lock the supplier.
                const string supplierSql = @"
            SELECT ACCID
            FROM dbo.SUPMAS WITH (UPDLOCK, HOLDLOCK)
            WHERE ID = @Id
              AND ISNULL(DELFLG, 0) = 0;";

                int accountId;

                using (var command = new SqlCommand(
                    supplierSql, connection, transaction))
                {
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                    var result = await command.ExecuteScalarAsync();

                    if (result == null || result == DBNull.Value)
                    {
                        await transaction.RollbackAsync();
                        return (false, true, false);
                    }

                    accountId = Convert.ToInt32(result);
                }

                // 2. Check whether another active supplier shares this account.
                const string sharedAccountSql = @"
            SELECT COUNT_BIG(*)
            FROM dbo.SUPMAS WITH (UPDLOCK, HOLDLOCK)
            WHERE ACCID = @AccId
              AND ID <> @Id and ACTIVE=1
              AND ISNULL(DELFLG, 0) = 0;";

                using (var command = new SqlCommand(
                    sharedAccountSql, connection, transaction))
                {
                    command.Parameters.Add("@AccId", SqlDbType.Int).Value =
                        accountId;

                    command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                    long sharedSuppliers = Convert.ToInt64(
                        await command.ExecuteScalarAsync());

                    if (sharedSuppliers > 0)
                    {
                        await transaction.RollbackAsync();
                        return (false, false, true);
                    }
                }

                // 3. Check known tables referencing the linked account.
                // Complete set of tables that reference ACCMAS.ID,
                // matching the Accounts in-use check.
                const string usageSql = @"
               SELECT
                   (SELECT COUNT_BIG(*) FROM dbo.BILLBYBILL
                    WHERE ACCID = @AccId) +
                   (SELECT COUNT_BIG(*) FROM dbo.BNKACCDET
                    WHERE ACCID = @AccId) +
                   (SELECT COUNT_BIG(*) FROM dbo.BRMAST
                    WHERE ACCID = @AccId) +
                  (SELECT COUNT_BIG(*) FROM dbo.BUDGETD
                     WHERE GRPORACCID = @AccId) +
                  (SELECT COUNT_BIG(*) FROM dbo.CONFIGTB
                     WHERE RDMACCID = @AccId) +
                   (SELECT COUNT_BIG(*) FROM dbo.COSTCOPNBAL
                    WHERE ACCODE = @AccId) +
                 (SELECT COUNT_BIG(*) FROM dbo.CUSMAS
                  WHERE ACCID = @AccId) +
                 (SELECT COUNT_BIG(*) FROM dbo.CUSMAS_TMP
                 WHERE ACCID = @AccId) +
                 (SELECT COUNT_BIG(*) FROM dbo.DBCRNOTE
                 WHERE ADJCD = @AccId) +
                 (SELECT COUNT_BIG(*) FROM dbo.GDWMAS
                 WHERE STKACCID = @AccId) +
                 (SELECT COUNT_BIG(*) FROM dbo.GDWMAS_TMP
                  WHERE STKACCID = @AccId) +
                  (SELECT COUNT_BIG(*) FROM dbo.OPN
                   WHERE ACCID = @AccId) +
                 (SELECT COUNT_BIG(*) FROM dbo.PRDNM
                 WHERE CRACC = @AccId OR DRACC = @AccId) +
                 (SELECT COUNT_BIG(*) FROM dbo.PRGRPMAS
                  WHERE PURACCID = @AccId OR SALESACCID = @AccId OR STOCKACCID = @AccId) +
                 (SELECT COUNT_BIG(*) FROM dbo.PURD
                  WHERE ADJCD = @AccId) +
                  (SELECT COUNT_BIG(*) FROM dbo.PURM
                   WHERE ADJCD = @AccId OR CRACC = @AccId OR DRACC = @AccId) +
                  (SELECT COUNT_BIG(*) FROM dbo.SALD
                   WHERE ADJCD = @AccId) +
                 (SELECT COUNT_BIG(*) FROM dbo.SALM
                  WHERE ADJCD = @AccId) +
                 (SELECT COUNT_BIG(*) FROM dbo.SMMAST
                 WHERE ACCODE = @AccId) +
                 (SELECT COUNT_BIG(*) FROM dbo.SUPMAS_TMP
                  WHERE ACCID = @AccId) +
                 (SELECT COUNT_BIG(*) FROM dbo.VRSYSTEM
                 WHERE SUBACCID = @AccId) +
                  (SELECT COUNT_BIG(*) FROM dbo.YVR
                  WHERE ADJCD = @AccId);";

                using (var command = new SqlCommand(
                    usageSql, connection, transaction))
                {
                    command.Parameters.Add("@AccId", SqlDbType.Int).Value =
                        accountId;

                    long references = Convert.ToInt64(
                        await command.ExecuteScalarAsync());

                    if (references > 0)
                    {
                        await transaction.RollbackAsync();
                        return (false, false, true);
                    }
                }
                // 4. Soft-delete the supplier. Never physically delete the row.
                const string deleteSql = @"
            UPDATE dbo.SUPMAS
            SET DELFLG = 1 , ACTIVE=0
            WHERE ID = @Id
              AND ISNULL(DELFLG, 0) = 0;";

                using (var command = new SqlCommand(
                    deleteSql, connection, transaction))
                {
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                    int rows = await command.ExecuteNonQueryAsync();

                    if (rows == 0)
                    {
                        await transaction.RollbackAsync();
                        return (false, true, false);
                    }
                }

                await transaction.CommitAsync();

                return (true, false, false);
            }
            catch
            {
                if (transaction.Connection != null)
                    await transaction.RollbackAsync();

                throw;
            }
        }


        // =========================================================
        // SUPPLIER MASTER LOOKUP / DROPDOWN DATABASE METHODS
        // =========================================================

        // Common lookup query helper
        private async Task<List<LookupItem>> GetLookupList(
            string sql,
              params SqlParameter[] parameters)
        {
            var result = new List<LookupItem>();
            var connection = db.GetConnection();

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);

            if (parameters.Length > 0)
                command.Parameters.AddRange(parameters);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new LookupItem
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Name = Convert.ToString(reader["Name"]) ?? ""
                });
            }

            return result;
        }

        // =========================================================
        // CURRENCIES
        // GET: api/v1/suppliers/lookups/currencies
        // =========================================================
        public async Task<DataTable> GetCurrencies()
        {
            const string sql = @"
            SELECT
                CODE,
                SYMBOL,
                CURRSTRING
            FROM dbo.CURRENCYMAS
            ORDER BY CURRSTRING;";

            return await db.GetDatatable(sql);
        }

        // =========================================================
        // COUNTRIES
        // GET: api/v1/suppliers/lookups/countries
        // =========================================================
        public async Task<List<CountryLookupItem>> GetCountriesOld()
        {
            var result = new List<CountryLookupItem>();
            var connection = db.GetConnection();

            try
            {
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                // Database diagnostics
                const string diagnosticSql = @"
            SELECT
                DB_NAME() AS DatabaseName,
                OBJECT_ID('dbo.COUNTRY', 'U') AS CountryObjectId;";

                using (var checkCommand = new SqlCommand(diagnosticSql, connection))
                using (var checkReader = await checkCommand.ExecuteReaderAsync())
                {
                    if (await checkReader.ReadAsync())
                    {
                        Console.WriteLine($"SQL Server: {connection.DataSource}");
                        Console.WriteLine($"Database: {checkReader["DatabaseName"]}");
                        Console.WriteLine(
                            $"Country Object ID: " +
                            $"{(checkReader["CountryObjectId"] == DBNull.Value ? "NULL" : checkReader["CountryObjectId"])}");
                    }
                }

                // Get countries
                const string sql = @"
            SELECT
                COUNTRYID,
                COUNTRYNAME,
                VATNOLIMIT,
                VATCHARALLOW
            FROM dbo.COUNTRY
            WHERE ISNULL(ACTIVE, 1) = 1
            ORDER BY COUNTRYNAME;";

                using var command = new SqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    int limit = reader["VATNOLIMIT"] == DBNull.Value
                        ? 0
                        : Convert.ToInt32(reader["VATNOLIMIT"]);

                    result.Add(new CountryLookupItem
                    {
                        Id = Convert.ToInt32(reader["COUNTRYID"]),
                        Name = Convert.ToString(reader["COUNTRYNAME"]) ?? "",
                        VatNoLimit = limit <= 0 ? 15 : limit,
                        VatCharAllow =
                            reader["VATCHARALLOW"] != DBNull.Value &&
                            Convert.ToBoolean(reader["VATCHARALLOW"])
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetCountries Error: {ex}");
                throw;
            }
        }


        public async Task<DataTable> GetCountries()
        {
            const string sql = @"
            SELECT
                COUNTRYID,
                COUNTRYNAME,
                VATNOLIMIT,
                VATCHARALLOW
            FROM dbo.COUNTRY
            WHERE ISNULL(ACTIVE, 1) = 1
            ORDER BY COUNTRYNAME;";

            return await db.GetDatatable(sql);
        }



        // =========================================================
        // REGIONS
        // GET: api/v1/suppliers/lookups/regions?countryId=1
        // =========================================================
        public async Task<DataTable> GetRegions(int countryId)
        {
            string sql = $@"
            SELECT
                REGIONID,
                REGIONNAME,
            REGIONSECNAME
            FROM dbo.REGION
            WHERE COUNTRYID = {countryId}
            ORDER BY REGIONNAME;";

            return await db.GetDatatable(sql);
        }
        // =========================================================
        // LEDGER GROUPS
        // GET: api/v1/suppliers/lookups/ledger-groups
        // =========================================================
        public Task<List<LookupItem>> GetLedgerGroups()
        {
            //    SELECT
            // GROUPID AS Id,
            // GROUPNAME AS Name
            //FROM GRPLDG
            //ORDER BY GROUPNAME; ";

            const string sql = @"
            SELECT GROUPID AS Id, GROUPNAME AS Name, 
            GACTIVE, MGROUP, GROUPID FROM GRPLDG WHERE
            GACTIVE = 1 AND(GROUPID = 27 OR MGROUP = 33)
            ORDER BY GROUPNAME ;";

            return GetLookupList(sql);
        }

        // ========================================================
        // SUPPLIER GROUPS
        // GET: api/v1/suppliers/lookups/supplier-groups
        // ========================================================
        public Task<List<LookupItem>> GetSupplierGroups()
        {
            const string sql = @"
        SELECT
            ID AS Id,
            GROUPNAME AS Name
        FROM PARTYGROUPMAS
        WHERE GROUPTYPE = @Type
        ORDER BY GROUPNAME;";

            return GetLookupList(
                sql,
                new SqlParameter("@Type", SqlDbType.NVarChar, 10)
                {
                    Value = "S"
                });
        }

        // ========================================================
        // ITEM GROUPS
        // GET: api/v1/suppliers/lookups/item-groups
        // =========================================================
        public Task<List<LookupItem>> GetItemGroups()
        {
            const string sql = @"
        SELECT
            PRGRPID AS Id,
            GRPNAME AS Name
        FROM PRGRPMAS
        WHERE ISNULL(ACTIVE, 1) = 1
        ORDER BY GRPNAME;";

            return GetLookupList(sql);
        }

        // =========================================================
        // BRANCHES
        // GET: api/v1/suppliers/lookups/branches
        // =========================================================
        // BRANCHES
        public Task<List<LookupItem>> GetBranches()
        {
            const string sql = @"
        SELECT
            ID AS Id,
            BRNAME AS Name
        FROM dbo.BRMAST
        WHERE ISNULL(DELFLG, 0) = 0
        ORDER BY BRNAME;";

            return GetLookupList(sql);
        }

        public Task<List<string>> GetVatTypes()
        {
            var data = new List<string>
        {
            "Standard VAT",
            "Multi-rate VAT",
            "Zero-rated VAT",
            "Reverse-charge VAT",
            "Differential VAT",
            "Destination-based VAT",
            "Small Business VAT"
        };

            return Task.FromResult(data);
        }

        public Task<List<string>> GetIdTypes()
        {
            var data = new List<string>
    {
        "CRN", "MOM", "MLS", "700","SAG", "OTH", "NAT", "IQA", "PAS", "GCC"
    };

            return Task.FromResult(data);

        }

        public async Task<SupplierLedgerGroupDto?> GetDefaultCustomerLedgerGroup()
        {
            using var connection = db.GetConnection();

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            const string sql = @"
           SELECT
              GROUPID AS GroupId,
          GROUPCODE AS GroupCode
           FROM dbo.GRPLDG
          WHERE GROUPID = @GroupId
           AND GROUPCODE = @GroupCode;";

            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@GroupId", SqlDbType.Int).Value = 26;
            command.Parameters.Add("@GroupCode", SqlDbType.VarChar, 20).Value = "11000";

            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new SupplierLedgerGroupDto
            {
                GroupId = Convert.ToInt32(reader["GroupId"]),
                GroupCode = Convert.ToString(reader["GroupCode"]) ?? string.Empty
            };
        }

        public string GenerateCustomerAccountCode(int ledgerGroupId, string groupCode, int supplierId)
        {
            if (ledgerGroupId <= 0)
                throw new ArgumentException(
                    "Invalid ledger group ID.");

            if (string.IsNullOrWhiteSpace(groupCode))
                throw new ArgumentException(
                    "Ledger group code is required.");

            if (supplierId <= 0)
                throw new ArgumentException(
                    "Invalid customer ID.");

            return $"S{groupCode.Trim()}-{supplierId:D2}";
        }

        public async Task<SupplierLedgerGroupDto?> GetDefaultSupplierLedgerGroup()
        {
            using var connection = db.GetConnection();

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            const string sql = @"
        SELECT
            GROUPID AS GroupId,
            GROUPCODE AS GroupCode
        FROM dbo.GRPLDG
        WHERE GROUPID = @GroupId
          AND GROUPCODE = @GroupCode;";

            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@GroupId", SqlDbType.Int).Value = 27;
            command.Parameters.Add("@GroupCode", SqlDbType.VarChar, 20).Value = "20000";

            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new SupplierLedgerGroupDto
            {
                GroupId = Convert.ToInt32(reader["GroupId"]),
                GroupCode = Convert.ToString(reader["GroupCode"]) ?? string.Empty
            };
        }

        public string GenerateSupplierAccountCode(int ledgerGroupId, string groupCode, int supplierId)
        {
            if (ledgerGroupId <= 0)
                throw new ArgumentException("Invalid supplier ledger group ID.");

            if (string.IsNullOrWhiteSpace(groupCode))
                throw new ArgumentException("Supplier ledger group code is required.");

            if (supplierId <= 0)
                throw new ArgumentException("Invalid supplier ID.");

            return $"S{groupCode.Trim()}-{supplierId:D2}";
        }
        //    public async Task<SupplierAccountInsertResult> InsertSupplier(
        //    clsSupplier supplier,
        //    int ledgerGroupId,
        //    string groupCode)
        //    {
        //        using var connection = db.GetConnection();

        //        if (connection.State != ConnectionState.Open)
        //            await connection.OpenAsync();

        //        using var transaction = connection.BeginTransaction();

        //        try
        //        {
        //            // 1. Insert supplier and get its actual ID.
        //            var columns = string.Join(
        //                ", ",
        //                Fields.Select(f => f.Column)
        //                    .Concat(new[] { "DELFLG" }));

        //            var values = string.Join(
        //                ", ",
        //                Fields.Select(f => "@" + f.Column)
        //                    .Concat(new[] { "0" }));

        //            var insertSupplierSql = $@"
        //INSERT INTO dbo.SUPMAS ({columns})OUTPUT INSERTED.ID
        //VALUES ({values});";

        //            int supplierId;

        //            using (var command = new SqlCommand(
        //                insertSupplierSql, connection, transaction))
        //            {
        //                AddSupplierParameters(command, supplier);

        //                var result = await command.ExecuteScalarAsync();

        //                if (result == null || result == DBNull.Value)
        //                    throw new InvalidOperationException(
        //                        "Supplier insertion failed.");

        //                supplierId = Convert.ToInt32(result);
        //            }

        //            // 2. Generate account code using the real supplier ID.
        //            string accountCode = GenerateSupplierAccountCode(
        //                ledgerGroupId,
        //                groupCode,
        //                supplierId);

        //            // 3. Insert supplier account.
        //            int accountId = await InsertSupplierAccount(
        //                connection,
        //                transaction,
        //                supplier,
        //                ledgerGroupId,
        //                accountCode);

        //            if (accountId <= 0)
        //                throw new InvalidOperationException(
        //                    "Supplier account creation failed.");

        //            // 4. Link account to supplier.
        //            const string updateSql = @"
        //    UPDATE dbo.SUPMAS
        //    SET ACCID = @AccountId
        //    WHERE ID = @SupplierId
        //     AND ISNULL(DELFLG, 0) = 0;";

        //            using (var command = new SqlCommand(
        //                updateSql, connection, transaction))
        //            {
        //                command.Parameters.Add("@AccountId", SqlDbType.Int)
        //                    .Value = accountId;

        //                command.Parameters.Add("@SupplierId", SqlDbType.Int)
        //                    .Value = supplierId;

        //                int rowsUpdated = await command.ExecuteNonQueryAsync();

        //                if (rowsUpdated != 1)
        //                    throw new InvalidOperationException(
        //                        "Failed to link the account to the supplier.");
        //            }

        //            // 5. Commit supplier, account, and link together.
        //            await transaction.CommitAsync();

        //            supplier.AccId = accountId;
        //            return new SupplierAccountInsertResult
        //            {
        //                SupplierId = supplierId,

        //                AccountId = accountId,
        //                AccountCode = accountCode
        //            };
        //            //return supplierId;
        //        }
        //        catch
        //        {
        //            await transaction.RollbackAsync();
        //            throw;
        //        }
        // }
        private static async Task<int> InsertSupplierAccount(SqlConnection connection, SqlTransaction transaction, clsSupplier supplier, int ledgerGroupId, string accountCode)
        {
            const string sql = @"
            INSERT INTO ACCMAS
            (
                ALIAS,ACCNAME, SECNAME, ADDRESS, PHONE,ACTIVE,LOGCD,UITIME,ACCODE,PRINTNAME,  MGROUP, DEPRECIATION,  VATFLAG,  VATPERC,  TAXRETFLAG, BANKACCNAME,  BANKACCNO, IBANNO, SWIFTCODE, BRANCH,  OPENBAL, CURCONVRATE, CURCODE, CURFLG, APRVLST,  APRVLABY, APRVLBBY, APRVLRDT, DELFLG
            )
            OUTPUT INSERTED.ID
            VALUES
            (
                @Alias, @AccName, @SecName, @Address, @Phone, @Active, @LogCd, GETDATE(),  @AcCode, @PrintName, @MGroup, @Depreciation, @VatFlag,  @VatPerc, @TaxRetFlag, @BankAccName, @BankAccNo, @IbanNo, @SwiftCode, @Branch, @OpenBal,
          @CurConvRate, @CurCode,@CurFlg,@Aprvlst,@Aprvlaby,@Aprvlbby, GETDATE(),0);";

            using var command = new SqlCommand(
                sql,
                connection,
                transaction);

            command.Parameters.AddWithValue(
                "@Alias", supplier.Alias ?? "");

            command.Parameters.AddWithValue(
                "@AccName", supplier.SupName ?? "");

            command.Parameters.AddWithValue(
                "@SecName", supplier.SecName ?? "");

            command.Parameters.AddWithValue(
                "@Address", supplier.Address ?? "");

            command.Parameters.AddWithValue(
                "@Phone", supplier.Phone ?? "");

            command.Parameters.AddWithValue(
                "@Active", supplier.Active);

            command.Parameters.AddWithValue(
                "@LogCd", supplier.LogId);

            // Placeholder until the account-code generation
            // rules are confirmed.
            command.Parameters.AddWithValue(
                "@AcCode", accountCode);

            command.Parameters.AddWithValue(
                "@PrintName", supplier.PrintName ?? "");

            // Supplier account group.
            //command.Parameters.AddWithValue(
            //    "@MGroup", 27);
            command.Parameters.Add("@MGroup", SqlDbType.Int).Value = ledgerGroupId;

            command.Parameters.AddWithValue(
                "@Depreciation", 0m);

            command.Parameters.AddWithValue(
                "@VatFlag", supplier.VatFlag);

            command.Parameters.AddWithValue(
                "@VatPerc", 0m);

            command.Parameters.AddWithValue(
                "@TaxRetFlag", false);

            command.Parameters.AddWithValue(
    "@BankAccName", supplier.BnkAccName ?? "");

            command.Parameters.AddWithValue(
                "@BankAccNo", supplier.BnkAccNo ?? "");

            command.Parameters.AddWithValue(
                "@IbanNo", supplier.IbanNo ?? "");

            command.Parameters.AddWithValue(
                "@SwiftCode", supplier.SwiftCode ?? "");

            command.Parameters.AddWithValue(
                "@Branch", supplier.Branch ?? "");

            command.Parameters.AddWithValue("@OpenBal", supplier.OpenBal);
            command.Parameters.AddWithValue("@CurConvRate", supplier.CurConvRate);
            command.Parameters.AddWithValue("@CurCode", supplier.CurCode);

            command.Parameters.AddWithValue(
                "@CurFlg", supplier.CurFlg);

            command.Parameters.AddWithValue(
                "@Aprvlst", supplier.AprvLst ?? "");

            command.Parameters.AddWithValue(
                "@Aprvlaby", 0);

            command.Parameters.AddWithValue(
                "@Aprvlbby", 0);

            var result = await command.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
                return 0;

            return Convert.ToInt32(result);
        }



        private static async Task LoadSupplierOpeningData(
    SqlConnection connection,
    clsSupplier supplier)
        {
            // Defaults
            supplier.OpenBal = 0m;
            supplier.CrDr = "CR";
            supplier.CurCode = 0;
            supplier.CurConvRate = 1m;
            supplier.PendingBills = new List<PendingBillRequest>();

            if (supplier.AccountId <= 0)
                return;

            // 1. Load opening balance and currency from ACCMAS.
            // 1. Load account details, including ledger group ID.
            const string accountSql = @"
            SELECT
                OPENBAL,
                CURCODE,
                CURCONVRATE,
                ACCODE,
                MGROUP
            FROM dbo.ACCMAS
            WHERE ID = @AccId;";

            using (var command = new SqlCommand(accountSql, connection))
            {
                command.Parameters.Add("@AccId", SqlDbType.Int).Value = supplier.AccountId;

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    supplier.OpenBal = reader["OPENBAL"] == DBNull.Value
                        ? 0m
                        : Convert.ToDecimal(reader["OPENBAL"]);

                    supplier.CurCode = reader["CURCODE"] == DBNull.Value
                        ? 0
                        : Convert.ToInt32(reader["CURCODE"]);

                    supplier.CurConvRate = reader["CURCONVRATE"] == DBNull.Value
                        ? 1m
                        : Convert.ToDecimal(reader["CURCONVRATE"]);

                    supplier.AccountCode = reader["ACCODE"] == DBNull.Value
                        ? string.Empty
                        : Convert.ToString(reader["ACCODE"]) ?? string.Empty;

                    supplier.LedgerGroupId = reader["MGROUP"] == DBNull.Value
                        ? 0
                        : Convert.ToInt32(reader["MGROUP"]);
                }
            }

            // 2. Determine debit/credit direction from opening entries.
            const string openingSql = @"
        SELECT
            SUM(ISNULL(DEBIT, 0)) AS TotalDebit,
            SUM(ISNULL(CREDIT, 0)) AS TotalCredit
        FROM dbo.OPN
        WHERE ACCID = @AccId
          AND BRCD = 0;";

            using (var command = new SqlCommand(openingSql, connection))
            {
                command.Parameters.Add("@AccId", SqlDbType.Int)
                    .Value = supplier.AccountId;

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    decimal debit = reader["TotalDebit"] == DBNull.Value
                        ? 0m
                        : Convert.ToDecimal(reader["TotalDebit"]);

                    decimal credit = reader["TotalCredit"] == DBNull.Value
                        ? 0m
                        : Convert.ToDecimal(reader["TotalCredit"]);

                    supplier.CrDr = credit >= debit ? "CR" : "DR";
                }
            }

            // 3. Load pending supplier bills.
            const string billsSql = @"
        SELECT
            ID,
            INVDATE,
            NARRATION,
            INVNO,
            AMOUNT,
            DUEDATE,
            DRCR
        FROM dbo.PENDBIL
        WHERE CODE = @AccId
          AND INVTYPE = 'SB'
        ORDER BY INVDATE, INVNO;";

            var bills = new List<PendingBillRequest>();

            using (var command = new SqlCommand(billsSql, connection))
            {
                command.Parameters.Add("@AccId", SqlDbType.Int)
                    .Value = supplier.AccountId;

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    bills.Add(new PendingBillRequest
                    {
                        Id = reader["ID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ID"]),

                        InvoiceDate = reader["INVDATE"] == DBNull.Value
                            ? default
                            : Convert.ToDateTime(reader["INVDATE"]),

                        Narration = reader["NARRATION"] == DBNull.Value
                            ? string.Empty
                            : Convert.ToString(reader["NARRATION"])
                              ?? string.Empty,

                        InvoiceNumber = reader["INVNO"] == DBNull.Value
                            ? string.Empty
                            : Convert.ToString(reader["INVNO"])
                              ?? string.Empty,

                        Amount = reader["AMOUNT"] == DBNull.Value
                            ? 0m
                            : Convert.ToDecimal(reader["AMOUNT"]),

                        DueDate = reader["DUEDATE"] == DBNull.Value
                            ? default
                            : Convert.ToDateTime(reader["DUEDATE"]),

                        DrCr = reader["DRCR"] == DBNull.Value
                            ? "Cr"
                            : Convert.ToString(reader["DRCR"])
                              ?? "Cr"
                    });
                }
            }

            supplier.PendingBills = bills;
        }
    }
}