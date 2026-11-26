
using Dapper;
using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore.Infrastructure;
using QuickBalance.API.Configuration;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs.Purchase;
using QuickBalance.API.Interfaces.Purchase;
using System.Data;

namespace QuickBalance.API.Repository.Purchase
{
    public sealed class PurchaseInvoiceRepository : IPurchaseInvoiceRepository
    {
        private readonly DatabaseConnection _databaseConnection;

        public PurchaseInvoiceRepository(DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }

        private SqlConnection CreateConnection()
        {
            var variables = new Variable();
            var connectionString = _databaseConnection.GetConnectionString(variables);
            return new SqlConnection(connectionString);
        }

        // ================================================================
        // LOOKUPS
        // ================================================================

        public async Task<PurchaseSettings> GetSettingsAsync()
        {
            const string sql = @"
                    SELECT TOP 1
                           FYDATEFR AS FyStartDate,
                           FYDATETO AS FyEndDate,
                           ISNULL(CASHCD, 0) AS CashCode,
                           ISNULL(VATCD, 0) AS VatCode
                    FROM FYDETAILS
                    WHERE PREVFIN = 0;

                    SELECT TOP 1
                           DECPOINTS, QTYDECPOINTS, INVAMTEDITPI,
                           DATEBLOCK, BLOCKDT, LASTVRDATE, TAGACCMSTR
                    FROM CONFIGTB;";

            await using var conn = CreateConnection();
            using var multi = await conn.QueryMultipleAsync(sql);

            var fy = await multi.ReadFirstOrDefaultAsync<dynamic>();
            var cfg = await multi.ReadFirstOrDefaultAsync<dynamic>();

            var settings = new PurchaseSettings();

            if (fy is not null)
            {
                settings.FyStartDate = fy.FyStartDate is DateTime fs ? fs : default;
                settings.FyEndDate = fy.FyEndDate is DateTime fe ? fe : default;
                settings.CashCode = ToInt(fy.CashCode);
                settings.VatCode = ToInt(fy.VatCode);
            }

            if (cfg is not null)
            {
                settings.DecimalPoint = ToInt(cfg.DECPOINTS, 2);
                settings.QtyDecimalPoint = ToInt(cfg.QTYDECPOINTS, 2);
                settings.InvAmtEditPi = ToBool(cfg.INVAMTEDITPI);
                settings.DateBlock = ToBool(cfg.DATEBLOCK);
                settings.BlockDate = cfg.BLOCKDT is DateTime bd ? bd : null;
                settings.LastVrDate = cfg.LASTVRDATE is DateTime lv ? lv : null;
                settings.TagAccMstr = ToBool(cfg.TAGACCMSTR);
            }

            settings.VatFlg = true;
            return settings;
        }

        public async Task<SupplierInfo?> GetSupplierAsync(int supplierCode)
        {
            //const string sql = @"
                //SELECT A.ACCID AS Code,
                //       A.ACCNAME AS Name,
                //       CAST(CASE WHEN ISNULL(A.VATNO, '') <> '' THEN 1 ELSE 0 END AS BIT) AS IsVatParty,
                //       CAST(CASE WHEN ISNULL(A.FOREIGNFLG, 0) = 1 THEN 1 ELSE 0 END AS BIT) AS IsForeign,
                //       CAST(CASE WHEN ISNULL(A.GCCFLG, 0) = 1 THEN 1 ELSE 0 END AS BIT) AS IsGcc
                //FROM ACCMAS A
                //WHERE A.ACCID = @SupplierCode;
                    //SELECT
                        //    A.ID AS Code,
                       //     A.ACCNAME AS Name
                        ////FROM ACCMAS A
                       // WHERE A.ID = @SupplierCode";
            const string sql = @"
                  SELECT
                            A.ID AS Code,
                            A.ACCNAME AS Name
                        FROM ACCMAS A
                        WHERE A.ID = @SupplierCode";

            await using var conn = CreateConnection();

            return await conn.QueryFirstOrDefaultAsync<SupplierInfo>(
                sql,
                new { SupplierCode = supplierCode });
        }

        public async Task<IReadOnlyDictionary<int, ProductInfo>> GetProductsAsync(
            IEnumerable<int> productCodes,
            bool tagAccMstr)
        {
            const string sql = @"
                SELECT
                    P.PRCODE AS ProductCode,
                    ISNULL(P.DESCRIPTION, '') AS Description,
                    ISNULL(P.UNIT, 0) AS MainUnit,
                    ISNULL(P.ALTUNIT, 0) AS AltUnit,
                    ISNULL(P.UNITCONV, 1) AS ConvFact,
                    ISNULL(P.CONVTYPE, 0) AS ConvType,
                    ISNULL(P.PURPRICETYPE, 0) AS PurPriceType,
                    ISNULL(P.TAXCODE, 0) AS TaxCode,
                    ISNULL(T.TAXPERC, 0) AS TaxPercentage,
                    ISNULL(T.TTYPE, '') AS TaxType,
                    ISNULL(
                        CASE
                            WHEN @TagAccMstr = 1 THEN
                                CASE
                                    WHEN ISNULL(P.PURACCODE, 0) <> 0
                                        THEN P.PURACCODE
                                    WHEN G.TAGACCFLG = 1
                                        THEN G.PURACCID
                                    ELSE T.PURCODE
                                END
                            ELSE
                                CASE
                                    WHEN G.TAGACCFLG = 1
                                         AND ISNULL(G.PURACCID, 0) <> 0
                                        THEN G.PURACCID
                                    WHEN G.TAGACCFLG = 0
                                        THEN T.PURCODE
                                    ELSE NULL
                                END
                        END,
                        0
                    ) AS TaxAccountCode
                FROM PRMAST P
                LEFT JOIN PRGRPMAS G
    ON G.PRGRPID = P.GROUPCODE
                LEFT JOIN TAXMAS T
                    ON T.CODE = P.TAXCODE
                WHERE P.PRCODE IN @Codes";

            await using var conn = CreateConnection();

            var rows = await conn.QueryAsync<ProductInfo>(
                sql,
                new
                {
                    Codes = productCodes.ToArray(),
                    TagAccMstr = tagAccMstr ? 1 : 0
                });

            return rows.ToDictionary(r => r.ProductCode);
        }

        public async Task<CurrencyInfo?> GetCurrencyAsync(int currencyCode)
        {
            const string sql = @"
SELECT CODE AS Code,
       ISNULL(BASEFLG, 0) AS BaseFlag,
       ISNULL(CONVMODE, 0) AS ConvMode
FROM CURRMAS
WHERE CODE = @CurrencyCode;";

            await using var conn = CreateConnection();

            return await conn.QueryFirstOrDefaultAsync<CurrencyInfo>(
                sql,
                new { CurrencyCode = currencyCode });
        }

        public async Task<ExistingPurchaseHeader?> GetExistingHeaderAsync(int vrNo)
        {
            const string sql = @"
SELECT VRNO AS VrNo,
       ORDNO AS OrdNo,
       ISNULL(INVNOTEXT, '') AS InvNoText,
       ISNULL(GRNNO, 0) AS GrnNo,
       ISNULL(YVRNO, 0) AS YvrNo
FROM PURM
WHERE VRNO = @VrNo AND VRTYPE = 'P';";

            await using var conn = CreateConnection();

            return await conn.QueryFirstOrDefaultAsync<ExistingPurchaseHeader>(
                sql,
                new { VrNo = vrNo });
        }

        //        public async Task<bool> SupplierBillNoExistsAsync(
        //            int supplierCode,
        //            string billNo,
        //            int excludeVrNo)
        //        {
        //            const string sql = @"
        //SELECT CASE WHEN EXISTS (
        //    SELECT 1
        //    FROM PURM
        //    WHERE VRTYPE = 'P'
        //      AND SUPCODE = @SupplierCode
        //      AND BILLNO = @BillNo
        //      AND VRNO <> @ExcludeVrNo
        //)
        //THEN 1 ELSE 0 END;";

        //            await using var conn = CreateConnection();

        //            return await conn.ExecuteScalarAsync<int>(
        //                sql,
        //                new
        //                {
        //                    SupplierCode = supplierCode,
        //                    BillNo = billNo,
        //                    ExcludeVrNo = excludeVrNo
        //                }) == 1;
        //        }
        public async Task<bool> SupplierBillNoExistsAsync(
            int supplierCode,
            string billNo,
            int excludeVrNo)
        {
            const string sql = @"
        SELECT CASE WHEN EXISTS (
            SELECT 1
            FROM PURM
            WHERE VRTYPE = 'P'
              AND SUPCODE = @SupplierCode
              AND BILLNO = @BillNo
              AND VRNO <> @ExcludeVrNo
        )
        THEN 1 ELSE 0 END;";

            await using var conn = CreateConnection();

            var database = await conn.ExecuteScalarAsync<string>(
                "SELECT DB_NAME()");

            var parameters = new
            {
                SupplierCode = supplierCode,
                BillNo = billNo?.Trim(),
                ExcludeVrNo = excludeVrNo
            };

            var result = await conn.ExecuteScalarAsync<int>(sql, parameters);

            Console.WriteLine($"Database: {database}");
            Console.WriteLine($"SupplierCode: {supplierCode}");
            Console.WriteLine($"BillNo: [{billNo}]");
            Console.WriteLine($"ExcludeVrNo: {excludeVrNo}");
            Console.WriteLine($"Duplicate result: {result}");

            return result == 1;
        }

        // ================================================================
        // CREATE
        // ================================================================

        public async Task<PurchaseInvoiceSaveResult> CreateAsync(
            PurchaseInvoiceSaveModel model)
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();

            await using var tx = (SqlTransaction)
                await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted);

            try
            {
                int ordNo = await NextOrderNumberAsync(conn, tx);

                string invNoText = await BuildInvNoTextAsync(
                    conn, tx, ordNo, model.Settings);

                var h = model.Header;
                h.OrdNo = ordNo;
                h.InvNoText = invNoText;

                int vrNo = await conn.ExecuteScalarAsync<int>(
                    InsertPurmSql,
                    new
                    {
                        h.OrdNo,
                        h.VrType,
                        h.InvNoText,
                        h.PurDate,
                        h.SupplierCode,
                        h.BillNo,
                        h.BillDate,
                        h.InFormat,
                        h.BType,
                        h.AdjCd,
                        h.SlCode,
                        h.CurCode,
                        h.CurConRate,
                        h.NtValue,
                        h.TValue,
                        h.Tax,
                        h.Amount,
                        h.Freight,
                        h.DiscVal,
                        h.DiscP,
                        h.CusDuity,
                        h.CoinAdj,
                        h.Mrp,
                        h.CashPaid,
                        h.YvrNo,
                        h.GrnNo,
                        h.Remarks,
                        h.ShipAddress,
                        h.AdditionalCost,
                        h.AddCostAcCd,
                        h.Transferred,
                        h.AprvlSt,
                        UserId = model.UserId,
                        Now = model.Now
                    },
                    tx);

                if (vrNo <= 0)
                    throw new InvalidOperationException(
                        "PURM insert did not return a VRNO.");

                await InsertDetailsAsync(
                    conn, tx, model, vrNo, ordNo, keepUTime: null);

                await UpdateProductPricesAsync(
                    conn, tx, model.PriceUpdates);

                await conn.ExecuteAsync(
                    "UPDATE CONFIGTB SET LASTVRDATE = @Date;",
                    new { Date = h.PurDate },
                    tx);

                await InsertLogNoteAsync(
                    conn, tx, vrNo, model.UserId, model.Now, "Create ");

                await tx.CommitAsync();

                return new PurchaseInvoiceSaveResult
                {
                    VrNo = vrNo,
                    OrdNo = ordNo,
                    InvNoText = invNoText
                };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // ================================================================
        // UPDATE
        // ================================================================

        public async Task<PurchaseInvoiceSaveResult> UpdateAsync(
            PurchaseInvoiceSaveModel model)
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();

            await using var tx = (SqlTransaction)
                await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted);

            try
            {
                int vrNo = model.VrNo;
                int ordNo = model.OrdNo;
                var h = model.Header;

                var oldUTime = await conn.ExecuteScalarAsync<DateTime?>(@"
SELECT TOP 1 UTIME
FROM STKDT WITH (UPDLOCK)
WHERE TRNNO = @OrdNo AND TYPE = 'R' AND VRTYPE = 'TR';",
                    new { OrdNo = ordNo },
                    tx);

                int affected = await conn.ExecuteAsync(
                    UpdatePurmSql,
                    new
                    {
                        VrNo = vrNo,
                        h.PurDate,
                        h.SupplierCode,
                        h.BillNo,
                        h.BillDate,
                        h.InFormat,
                        h.BType,
                        h.AdjCd,
                        h.SlCode,
                        h.CurCode,
                        h.CurConRate,
                        h.NtValue,
                        h.TValue,
                        h.Tax,
                        h.Amount,
                        h.Freight,
                        h.DiscVal,
                        h.DiscP,
                        h.CusDuity,
                        h.CoinAdj,
                        h.Mrp,
                        h.CashPaid,
                        h.YvrNo,
                        h.GrnNo,
                        h.Remarks,
                        h.ShipAddress,
                        h.AdditionalCost,
                        h.AddCostAcCd,
                        UserId = model.UserId,
                        Now = model.Now
                    },
                    tx);

                if (affected != 1)
                    throw new InvalidOperationException(
                        $"PURM update affected {affected} rows for VRNO {vrNo}.");

                await conn.ExecuteAsync(@"
DELETE FROM PURT WHERE VRNO = @VrNo;
DELETE FROM PURD WHERE VRNO = @VrNo;
DELETE FROM STKDT WHERE TRNNO = @OrdNo AND TYPE = 'R' AND VRTYPE = 'TR';
DELETE FROM STKDTPHY WHERE TRNNO = @OrdNo AND TYPE = 'R' AND VRTYPE = 'TR';",
                    new { VrNo = vrNo, OrdNo = ordNo },
                    tx);

                await InsertDetailsAsync(
                    conn, tx, model, vrNo, ordNo, keepUTime: oldUTime);

                await UpdateProductPricesAsync(
                    conn, tx, model.PriceUpdates);

                await InsertLogNoteAsync(
                    conn, tx, vrNo, model.UserId, model.Now, "Update");

                await tx.CommitAsync();

                return new PurchaseInvoiceSaveResult
                {
                    VrNo = vrNo,
                    OrdNo = ordNo,
                    InvNoText = h.InvNoText
                };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // ================================================================
        // READ
        // ================================================================

        public async Task<PurchaseInvoiceResponse?> GetByIdAsync(int vrNo)
        {
            const string sql = @"
SELECT M.VRNO AS VrNo,
       M.ORDNO AS OrdNo,
       ISNULL(M.INVNOTEXT, '') AS InvNoText,
       M.PURDATE AS PurchaseDate,
       M.SUPCODE AS SupplierCode,
       ISNULL(A.ACCNAME, '') AS SupplierName,
       ISNULL(M.BILLNO, '') AS SupplierBillNo,
       M.BILLDATE AS SupplierBillDate,
       ISNULL(M.INFORMAT, 0) AS InvoiceFormat,
       ISNULL(M.BTYPE, '') AS PaymentType,
       ISNULL(M.ADJCD, 0) AS AdjustmentLedgerCode,
       ISNULL(M.CURCODE, 0) AS CurrencyCode,
       ISNULL(M.CURCONRATE, 1) AS ConversionRate,
       ISNULL(M.NTVALUE, 0) AS TotalAmount,
       ISNULL(M.TVALUE, 0) AS TaxableAmount,
       ISNULL(M.TAX, 0) AS TaxAmount,
       ISNULL(M.FREIGHT, 0) AS Freight,
       ISNULL(M.DISCP, 0) AS DiscountPercentage,
       ISNULL(M.DISCVAL, 0) AS DiscountValue,
       ISNULL(M.CUSDUITY, 0) AS CustomsDuty,
       ISNULL(M.COINADJ, 0) AS RoundOff,
       ISNULL(M.AMOUNT, 0) AS NetAmount,
       ISNULL(M.ADDITIONALCOST, 0) AS AdditionalCost,
       M.REMARKS AS Remarks,
       M.SHIPADDRESS AS ShipAddress,
       M.CREATEDDT AS CreatedDate,
       M.UDATE AS UpdatedDate
FROM PURM M
LEFT JOIN ACCMAS A
    ON A.ID = M.SUPCODE
WHERE M.VRNO = @VrNo AND M.VRTYPE = 'P';

SELECT
    T.CODE AS SlNo,
    T.PRCODE AS ProductCode,
    ISNULL(T.PRDESC, '') AS ProductName,
    T.UNITCODE AS UnitCode,
    T.QTY AS Qty,
    T.CONVQTY AS ConvQty,
    T.PRRATE AS Price,
    T.CONVRATE AS ConvRate,
    T.MRP AS Mrp,
    T.LDISCPERC AS LineDiscountPercentage,
    T.LDISCAMT AS LineDiscountAmount,
    T.DISCP AS DiscountPercentage,
    T.DISC AS DiscountAmount,
    T.TAXCODE AS TaxCode,
    T.TAX AS TaxPercentage,
    T.GDWCODE AS GodownCode,
    T.BATCH AS Batch,
    T.MFGDATE AS MfgDate,
    T.EXPIRY AS ExpiryDate,
    T.ADDCOST AS AddCost
FROM PURT T
WHERE T.VRNO = @VrNo
ORDER BY T.CODE;

SELECT D.ETYPE AS EType,
       D.CODE AS Code,
       D.AMOUNT AS Amount,
       D.TAX AS TaxPercentage,
       D.TAXAMT AS TaxAmount,
       ISNULL(D.GCCCD, 0) AS GccHeadCode
FROM PURD D
WHERE D.VRNO = @VrNo;";

            await using var conn = CreateConnection();

            using var multi = await conn.QueryMultipleAsync(
                sql,
                new { VrNo = vrNo });

            var header =
                await multi.ReadFirstOrDefaultAsync<PurchaseInvoiceResponse>();

            if (header is null)
                return null;

            header.Items =
                (await multi.ReadAsync<PurchaseInvoiceItemResponse>()).ToList();

            header.Details =
                (await multi.ReadAsync<PurchaseInvoiceDetailRowResponse>()).ToList();

            return header;
        }

        // ================================================================
        // HELPERS
        // ================================================================

        private static async Task<int> NextOrderNumberAsync(
            SqlConnection conn,
            SqlTransaction tx)
        {
            var current = await conn.ExecuteScalarAsync<int?>(
                "SELECT PURVRNO FROM FYDETAILS WITH (UPDLOCK, ROWLOCK) WHERE PREVFIN = 0;",
                transaction: tx);

            if (current is null)
                throw new InvalidOperationException(
                    "Current financial year (FYDETAILS) not found.");

            if (current < 0)
                throw new PurchaseInvoiceValidationException(
                    "Purchase numbering is in use by another user. Please try again.");

            int next = current.Value + 1;

            bool taken = await conn.ExecuteScalarAsync<int>(@"
SELECT CASE WHEN EXISTS (
    SELECT 1
    FROM PURM WITH (UPDLOCK, HOLDLOCK)
    WHERE VRTYPE = 'P' AND ORDNO = @Next
) THEN 1 ELSE 0 END;",
                new { Next = next },
                tx) == 1;

            if (taken)
            {
                next = await conn.ExecuteScalarAsync<int>(
                    "SELECT ISNULL(MAX(ORDNO), 0) + 1 FROM PURM WITH (UPDLOCK, HOLDLOCK) WHERE VRTYPE = 'P';",
                    transaction: tx);
            }

            await conn.ExecuteAsync(
                "UPDATE FYDETAILS SET PURVRNO = @Next WHERE PREVFIN = 0;",
                new { Next = next },
                tx);

            return next;
        }

        private static async Task<string> BuildInvNoTextAsync(
            SqlConnection conn,
            SqlTransaction tx,
            int ordNo,
            PurchaseSettings settings)
        {
            var cfg = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
SELECT TOP 1 PREFIX, SEPERATOR, FIXLENGTH, LENGTH,
       SUFFIX, STARTNO, EMBDATE, EMBFORMAT
FROM VRNOCONF
WHERE VRTYPE = 'Purchase' AND SERIES = 'Main';",
                transaction: tx);

            if (cfg is null)
                return ordNo.ToString();

            string prefix = (string?)cfg.PREFIX ?? string.Empty;
            string sep = (string?)cfg.SEPERATOR ?? string.Empty;
            string suffix = (string?)cfg.SUFFIX ?? string.Empty;

            bool fixLength = string.Equals(
                (string?)cfg.FIXLENGTH,
                "Y",
                StringComparison.OrdinalIgnoreCase);

            int length = ToInt(cfg.LENGTH);
            int startNo = ToInt(cfg.STARTNO);

            string embDate = (string?)cfg.EMBDATE ?? string.Empty;
            string embFormat = (string?)cfg.EMBFORMAT ?? string.Empty;

            int number = ordNo == 1 && startNo > 0 ? startNo : ordNo;

            string numberText = fixLength && length > 0
                ? number.ToString().PadLeft(length, '0')
                : number.ToString();

            string fyText = BuildFyText(
                embFormat,
                settings.FyStartDate,
                settings.FyEndDate);

            if (string.Equals(
                embDate, "Prefix", StringComparison.OrdinalIgnoreCase)
                && fyText.Length > 0)
            {
                prefix = prefix.Length > 0
                    ? prefix + sep + fyText
                    : fyText;
            }
            else if (string.Equals(
                embDate, "Suffix", StringComparison.OrdinalIgnoreCase)
                && fyText.Length > 0)
            {
                suffix = suffix.Length > 0
                    ? fyText + sep + suffix
                    : fyText;
            }

            var parts = new[] { prefix, numberText, suffix }
                .Where(x => !string.IsNullOrEmpty(x));

            return string.Join(sep, parts);
        }

        private static string BuildFyText(
            string format,
            DateTime start,
            DateTime end)
        {
            if (start == default || end == default)
                return string.Empty;

            return format.ToUpperInvariant() switch
            {
                "YYYY-YY" => $"{start:yyyy}-{end:yy}",
                "YY-YY" => $"{start:yy}-{end:yy}",
                "YYYY" => $"{start:yyyy}",
                _ => string.Empty
            };
        }

        //private static async Task InsertDetailsAsync(
        //    SqlConnection conn,
        //    SqlTransaction tx,
        //    PurchaseInvoiceSaveModel model,
        //    int vrNo,
        //    int ordNo,
        //    DateTime? keepUTime)
        //{
        //    foreach (var t in model.Items)
        //        t.VrNo = vrNo;

        //    foreach (var d in model.Details)
        //        d.VrNo = vrNo;

        //    foreach (var s in model.Stock)
        //    {
        //        s.TrnNo = ordNo;

        //        if (keepUTime.HasValue)
        //            s.UTime = keepUTime.Value;
        //    }

        //    int purt = await conn.ExecuteAsync(InsertPurtSql, model.Items, tx);



        //    if (purt != model.Items.Count)
        //        throw new InvalidOperationException(
        //            "PURT insert count mismatch.");

        //    int purd = await conn.ExecuteAsync(
        //        InsertPurdSql, model.Details, tx);

        //    if (purd != model.Details.Count)
        //        throw new InvalidOperationException(
        //            "PURD insert count mismatch.");

        //    int stk = await conn.ExecuteAsync(
        //        InsertStkdtSql, model.Stock, tx);

        //    if (stk != model.Stock.Count)
        //        throw new InvalidOperationException(
        //            "STKDT insert count mismatch.");
        //}
        private static async Task InsertDetailsAsync(
    SqlConnection conn,
    SqlTransaction tx,
    PurchaseInvoiceSaveModel model,
    int vrNo,
    int ordNo,
    DateTime? keepUTime)
        {
            var h = model.Header;

            // Assign header values to every PURT item
            foreach (var t in model.Items)
            {
                t.VrNo = vrNo;
                t.PurDate = h.PurDate;
            }

            // Assign VRNO to PURD rows
            foreach (var d in model.Details)
            {
                d.VrNo = vrNo;
            }

            // Assign values to stock rows
            foreach (var s in model.Stock)
            {
                s.TrnNo = ordNo;

                if (keepUTime.HasValue)
                    s.UTime = keepUTime.Value;
            }

            // Prepare PURT parameters using both Header and Items
            var purtRows = model.Items.Select(t => new
            {
                t.VrNo,
                VrType = h.VrType,
                t.PurDate,
                BillNo = h.BillNo,
                BillDate = h.BillDate,
                SupCode = h.SupplierCode,

                t.PrCode,
                t.Qty,
                UnitCode = t.Unit,
                Rate = t.PrRate,
                t.PrRate,
                BType = h.BType,
                LogCd = model.UserId,

                t.TaxCode,
                t.Disc,
                t.Tax,
                t.Batch,
                Expiry = t.ExpDate,
                t.GdwCode,
                t.PrDesc,

                OrdNo = ordNo,
                t.StkFlg,
                t.ConvType,
                t.ConvFact,
                t.ConvQty,
                t.ConvRate,
                t.LDiscType,
                t.LDiscPerc,
                t.LDiscAmt,
                DiscP = t.DiscP,
                t.Mrp,
                t.MfgDate,
                t.AddCost,
                t.ConvAddCost,
                t.VatReasonCd,
                SlNo = t.SlNo
            }).ToList();

            int purt = await conn.ExecuteAsync(
                InsertPurtSql, purtRows, tx);

            if (purt != model.Items.Count)
                throw new InvalidOperationException(
                    "PURT insert count mismatch.");

            // Insert PURD
            int purd = await conn.ExecuteAsync(
                InsertPurdSql, model.Details, tx);

            if (purd != model.Details.Count)
                throw new InvalidOperationException(
                    "PURD insert count mismatch.");

            // Insert STKDT
            int stk = await conn.ExecuteAsync(
                InsertStkdtSql, model.Stock, tx);

            if (stk != model.Stock.Count)
                throw new InvalidOperationException(
                    "STKDT insert count mismatch.");
        }
        private static async Task UpdateProductPricesAsync(
            SqlConnection conn,
            SqlTransaction tx,
            List<ProductPriceUpdate> updates)
        {
            var main = updates
                .Where(u => u.IsMainUnit)
                .Select(u => new { u.ProductCode, u.Price })
                .ToList();

            var alt = updates
                .Where(u => !u.IsMainUnit)
                .Select(u => new { u.ProductCode, u.Price })
                .ToList();

            if (main.Count > 0)
            {
                await conn.ExecuteAsync(
                    "UPDATE PRMAST SET BASEPURPRICE = @Price WHERE PRCODE = @ProductCode;",
                    main,
                    tx);
            }

            if (alt.Count > 0)
            {
                await conn.ExecuteAsync(
                    "UPDATE PRMAST SET ALTPURPRICE = @Price WHERE PRCODE = @ProductCode;",
                    alt,
                    tx);
            }
        }

        private static Task InsertLogNoteAsync(
            SqlConnection conn,
            SqlTransaction tx,
            int vrNo,
            int userId,
            DateTime now,
            string notes)
        {
            return conn.ExecuteAsync(@"
INSERT INTO LOGNOTE
    (VRTYPE, VRNO, EDATE, USERCODE, NOTES, CFLAG, VSVRTYPE)
VALUES
    ('P', @VrNo, @Now, @UserId, @Notes, 1, '');",
                new
                {
                    VrNo = vrNo,
                    Now = now,
                    UserId = userId,
                    Notes = notes
                },
                tx);
        }

        private static int ToInt(
            object? value,
            int fallback = 0)
        {
            return value is null || value is DBNull
                ? fallback
                : Convert.ToInt32(value);
        }

        private static bool ToBool(object? value)
        {
            return value switch
            {
                null or DBNull => false,
                bool b => b,
                string s =>
                    s.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase)
                    || s.Trim().Equals("TRUE", StringComparison.OrdinalIgnoreCase)
                    || s.Trim() == "1",
                _ => Convert.ToInt32(value) != 0
            };
        }

        // ================================================================
        // SQL
        // ================================================================

        private const string InsertPurmSql = @"
                INSERT INTO PURM
                (
                    ORDNO, VRTYPE, INVNOTEXT, PURDATE, SUPCODE, BILLNO, BILLDATE,
                    INFORMAT, BTYPE, ADJCD, SLCODE,
                    CURCODE, CURCONRATE, NTVALUE, TVALUE, TAX, AMOUNT, FREIGHT,
                    DISCVAL, DISCP, CUSDUITY, COINADJ, MRP,
                    CASHPAID, YVRNO, GRNNO, REMARKS, SHIPADDRESS,
                    ADDITIONALCOST, ADDCOSTACCD, TRANSFERRED, APRVLST,
                    RETURNFLG, LOGCD, ULOGCD, UDATE, CREATEDDT, APRVLRDT, APRVLRBY
                )
                OUTPUT INSERTED.VRNO
                VALUES
                (
                    @OrdNo, @VrType, @InvNoText, @PurDate, @SupplierCode, @BillNo,
                    @BillDate, @InFormat, @BType, @AdjCd, @SlCode,
                    @CurCode, @CurConRate, @NtValue, @TValue, @Tax, @Amount,
                    @Freight, @DiscVal, @DiscP, @CusDuity, @CoinAdj, @Mrp,
                    @CashPaid, @YvrNo, @GrnNo, @Remarks, @ShipAddress,
                    @AdditionalCost, @AddCostAcCd, @Transferred, @AprvlSt,
                    0, @UserId, @UserId, @Now, @Now, @Now, @UserId
                );";
        private const string UpdatePurmSql = @"
UPDATE PURM SET
  PURDATE = @PurDate, CODE = @SupplierCode, BILLNO = @BillNo, BILLDATE = @BillDate, INVFORMAT = @InvFormat,
  BTYPE = @BType, ADJCD = @AdjCd, SLCODE = @SlCode, CURCODE = @CurCode, CURCONRATE = @CurConRate,
  NTVALUE = @NtValue, TVALUE = @TValue, TAX = @Tax, AMOUNT = @Amount, FREIGHT = @Freight,
  DISCVAL = @DiscVal, DISCP = @DiscP, CUSDUITY = @CusDuity, COINADJ = @CoinAdj, MRP = @Mrp,
  CASHPAID = @CashPaid, YVRNO = @YvrNo, GRNNO = @GrnNo, REMARKS = @Remarks, SHIPADDRESS = @ShipAddress,
  ADDITIONALCOST = @AdditionalCost, ADDCOSTACCD = @AddCostAcCd,
  ULOGCD = @UserId, UDATE = @Now
WHERE VRNO = @VrNo AND VRTYPE = 'P';";

        //        private const string InsertPurtSql = @"
        //INSERT INTO PURT
        // (VRNO, SLNO, PRCODE, PRDESC, UNIT, ALTUNIT, QTY, CONVQTY, CONVFACT, CONVTYPE, PRRATE, CONVRATE, MRP,
        //  DISC, DISCP, LDISCPERC, LDISCAMT, LDISCTYPE, AMOUNT, TAX, TAXCODE, TAXAMT, GDWCODE, BATCH,
        //  MFGDATE, EXPDATE, ADDCOST, CONVADDCOST, BTYPE, STKFLG, RETURNFLG, ORGQTY, ORGRATE, CANCELFLG, VATREASONCD)
        //VALUES
        // (@VrNo, @SlNo, @PrCode, @PrDesc, @Unit, @AltUnit, @Qty, @ConvQty, @ConvFact, @ConvType, @PrRate, @ConvRate, @Mrp,
        //  @Disc, @DiscP, @LDiscPerc, @LDiscAmt, @LDiscType, @Amount, @Tax, @TaxCode, @TaxAmt, @GdwCode, @Batch,
        //  @MfgDate, @ExpDate, @AddCost, @ConvAddCost, @BType, @StkFlg, 0, 0, 0, 0, @VatReasonCd);";
        private const string InsertPurtSql = @"
                        INSERT INTO PURT
                        (
                            VRNO, VRTYPE, PURDATE, BILLNO, BILLDATE, SUPCODE,
                            PRCODE, QTY, UNITCODE, RATE, PRRATE, BTYPE, LOGCD,
                            TAXCODE, DISC, TAX, BATCH, EXPIRY, GDWCODE, PRDESC,
                            ORDNO, STKFLG, RETURNFLG, CONVTYPE, CONVFACT,
                            CONVQTY, CONVRATE, LDISCTYPE, LDISCPERC, LDISCAMT,
                            DISCP, MRP, MFGDATE, ADDCOST, CONVADDCOST,
                            VATREASONCD, SNTSLNO
                        )
                        VALUES
                        (
                            @VrNo, @VrType, @PurDate, @BillNo, @BillDate, @SupCode,
                            @PrCode, @Qty, @UnitCode, @Rate, @PrRate, @BType, @LogCd,
                            @TaxCode, @Disc, @Tax, @Batch, @Expiry, @GdwCode, @PrDesc,
                            @OrdNo, @StkFlg, 0, @ConvType, @ConvFact,
                            @ConvQty, @ConvRate, @LDiscType, @LDiscPerc, @LDiscAmt,
                            @DiscP, @Mrp, @MfgDate, @AddCost, @ConvAddCost,
                            @VatReasonCd, @SlNo
                        );";

        private const string InsertPurdSql = @"
INSERT INTO PURD
    (VRNO, ETYPE, CODE, AMOUNT, TAX, TAXAMT, ORGTAXAMT, DFLAG, SLCODE, ADJCD, GCCCD)
VALUES
    (@VrNo, @EType, @Code, @Amount, @Tax, @TaxAmt, @OrgTaxAmt, @DFlag, @SlCode, @AdjCd, @GccCd);";

        private const string InsertStkdtSql = @"
INSERT INTO STKDT
 (TRNNO, TYPE, VRTYPE, VRDATE, PRCODE, QTY, CONVQTY, GDWCODE, RATE, CONVRATE,
  ADDCOST, CONVADDCOST, UTIME, LUTIME)
VALUES
 (@TrnNo, @Type, @VrType, @VrDate, @PrCode, @Qty, @ConvQty, @GdwCode, @Rate, @ConvRate,
  @AddCost, @ConvAddCost, @UTime, @LuTime);";
    }
}
