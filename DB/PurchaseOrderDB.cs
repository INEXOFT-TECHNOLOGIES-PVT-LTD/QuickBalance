using Microsoft.Data.SqlClient;
using QuickBalance.API.DTOs;
using System.Data;

namespace QuickBalance.API.DB
{
    public class PurchaseOrderDB
    {
        private readonly DBConn db;

        public PurchaseOrderDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public async Task<int> CreatePurchaseOrderAsync(
      PurchaseOrderDTO purchaseOrder,
      int userId)
        {
            var connection = db.GetConnection();

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            using var transaction =
                (SqlTransaction)await connection.BeginTransactionAsync();

            try
            {
                // ============================================================
                // 1. Generate Purchase Order Number
                // ============================================================

                Console.WriteLine(
                    "PORD CREATE: Generating purchase order number...");

                int ordNo = await GetNextPurchaseOrderNumberAsync(
                    connection,
                    transaction);

                Console.WriteLine(
                    $"PORD CREATE: Generated ORDNO = {ordNo}");

                if (ordNo <= 0)
                {
                    throw new Exception(
                        "Failed to generate Purchase Order number. " +
                        "GetNextPurchaseOrderNumberAsync returned 0 or negative value.");
                }

                // ============================================================
                // 2. Insert PORDM
                // ============================================================

                Console.WriteLine(
                    $"PORD CREATE: Inserting PORDM for ORDNO = {ordNo}");

                const string insertHeaderSql = @"
INSERT INTO PORDM
(
    VRTYPE,
    ORDNO,
    ORDDATE,
    SUPCODE,
    DISCP,
    DISCVAL,
    TAX,
    NTVALUE,
    TVALUE,
    COINADJ,
    CUSDUITY,
    AMOUNT,
    LOGCD,
    REMARKS,
    FREIGHT,
    CREATEDDT,
    INFORMAT,
    INVNO,
    INVDATE,
    CURCONRATE,
    CURCODE,
    SHIPADDRESS,
    GRNO,
    CANCELFLG,
    INVNOTEXT,
    VSVRTYPE,
    VSDOCNO,
    VSORDCD,
    REFNO,
    TRANSFERRED,
    POF1,
    POF2,
    POF3,
    POF4,
    POF5,
    POF6,
    ULOGCD,
    UDATE,
    POF7,
    POF8,
    POF9,
    POF10,
    POF11,
    POF12,
    POF13,
    POF14,
    POF15,
    POF16,
    POF17,
    POF18,
    POF19,
    POF20,
    APRVLST,
    APRVLRBY,
    APRVLRDT
)
VALUES
(
    @VrType,
    @OrdNo,
    @OrdDate,
    @SupCode,
    @DiscP,
    @DiscVal,
    @Tax,
    @NtValue,
    @TValue,
    @CoinAdj,
    @CusDuity,
    @Amount,
    @LogCd,
    @Remarks,
    @Freight,
    @CreatedDt,
    @InFormat,
    @InvNo,
    @InvDate,
    @CurConRate,
    @CurCode,
    @ShipAddress,
    @GrNo,
    @CancelFlg,
    @InvNoText,
    @VSVRTYPE,
    @VSDOCNO,
    @VSORDCD,
    @RefNo,
    @Transferred,
    @POF1,
    @POF2,
    @POF3,
    @POF4,
    @POF5,
    @POF6,
    @ULogCd,
    @UDate,
    @POF7,
    @POF8,
    @POF9,
    @POF10,
    @POF11,
    @POF12,
    @POF13,
    @POF14,
    @POF15,
    @POF16,
    @POF17,
    @POF18,
    @POF19,
    @POF20,
    @AprvlLst,
    @AprvlrBy,
    @AprvlrDt
);";

                using (var command = new SqlCommand(
                    insertHeaderSql,
                    connection,
                    transaction))
                {
                    command.Parameters.AddWithValue(
                        "@VrType",
                        "O");

                    command.Parameters.AddWithValue(
                        "@OrdNo",
                        ordNo);

                    command.Parameters.AddWithValue(
                        "@OrdDate",
                        purchaseOrder.OrdDate ?? DateTime.Now);

                    command.Parameters.AddWithValue(
                        "@SupCode",
                        purchaseOrder.SupCode);

                    command.Parameters.AddWithValue(
                        "@DiscP",
                        purchaseOrder.DiscP);

                    command.Parameters.AddWithValue(
                        "@DiscVal",
                        purchaseOrder.DiscVal);

                    command.Parameters.AddWithValue(
                        "@Tax",
                        purchaseOrder.Tax);

                    command.Parameters.AddWithValue(
                        "@NtValue",
                        purchaseOrder.NtValue);

                    command.Parameters.AddWithValue(
                        "@TValue",
                        purchaseOrder.TValue);

                    command.Parameters.AddWithValue(
                        "@CoinAdj",
                        purchaseOrder.CoinAdj);

                    command.Parameters.AddWithValue(
                        "@CusDuity",
                        purchaseOrder.CusDuity);

                    command.Parameters.AddWithValue(
                        "@Amount",
                        purchaseOrder.Amount);

                    command.Parameters.AddWithValue(
                        "@LogCd",
                        userId);

                    command.Parameters.AddWithValue(
                        "@Remarks",
                        (object?)purchaseOrder.Remarks ??
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@Freight",
                        purchaseOrder.Freight);

                    command.Parameters.AddWithValue(
                        "@CreatedDt",
                        DateTime.Now);

                    command.Parameters.AddWithValue(
                        "@InFormat",
                        purchaseOrder.InFormat);

                    command.Parameters.AddWithValue(
                        "@InvNo",
                        purchaseOrder.InvNo);

                    command.Parameters.AddWithValue(
                        "@InvDate",
                        (object?)purchaseOrder.InvDate ??
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@CurConRate",
                        purchaseOrder.CurConRate);

                    command.Parameters.AddWithValue(
                        "@CurCode",
                        purchaseOrder.CurCode);

                    command.Parameters.AddWithValue(
                        "@ShipAddress",
                        (object?)purchaseOrder.ShipAddress ??
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@GrNo",
                        purchaseOrder.GrNo);

                    command.Parameters.AddWithValue(
                        "@CancelFlg",
                        purchaseOrder.CancelFlg);

                    command.Parameters.AddWithValue(
                        "@InvNoText",
                        (object?)purchaseOrder.InvNoText ??
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@VSVRTYPE",
                        purchaseOrder.VSVRTYPE);

                    command.Parameters.AddWithValue(
                        "@VSDOCNO",
                        (object?)purchaseOrder.VSDOCNO ??
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@VSORDCD",
                        purchaseOrder.VSORDCD);

                    command.Parameters.AddWithValue(
                        "@RefNo",
                        (object?)purchaseOrder.RefNo ??
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@Transferred",
                        (object?)purchaseOrder.Transferred ??
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@POF1",
                        (object?)purchaseOrder.POF1 ??
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@POF2",
                        (object?)purchaseOrder.POF2 ??
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@POF3",
                        (object?)purchaseOrder.POF3 ??
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@POF4",
                        (object?)purchaseOrder.POF4 ??
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@POF5",
                        (object?)purchaseOrder.POF5 ??
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@POF6",
                        (object?)purchaseOrder.POF6 ??
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@ULogCd",
                        userId);

                    command.Parameters.AddWithValue(
                        "@UDate",
                        DateTime.Now);

                    command.Parameters.AddWithValue(
                        "@POF7",
                        (object?)purchaseOrder.POF7 ??
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@POF8",
                        (object?)purchaseOrder.POF8 ??
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@POF9",
                        (object?)purchaseOrder.POF9 ??
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@POF10",
                        (object?)purchaseOrder.POF10 ??
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@POF11",
                        (object?)purchaseOrder.POF11 ??
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@POF12",
                        (object?)purchaseOrder.POF12 ??
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@POF13",
                        (object?)purchaseOrder.POF13 ??
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@POF14",
                        (object?)purchaseOrder.POF14 ??
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@POF15",
                        (object?)purchaseOrder.POF15 ??
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@POF16",
                        (object?)purchaseOrder.POF16 ??
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@POF17",
                        (object?)purchaseOrder.POF17 ??
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@POF18",
                        (object?)purchaseOrder.POF18 ??
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@POF19",
                        (object?)purchaseOrder.POF19 ??
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@POF20",
                        (object?)purchaseOrder.POF20 ??
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@AprvlLst",
                        (object?)purchaseOrder.AprvlLst ??
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@AprvlrBy",
                        DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@AprvlrDt",
                        DBNull.Value);

                    await command.ExecuteNonQueryAsync();
                }

                Console.WriteLine(
                    $"PORD CREATE: PORDM inserted successfully. ORDNO = {ordNo}");

                // ============================================================
                // 3. Insert PORDT
                // ============================================================

                Console.WriteLine(
                    $"PORD CREATE: Inserting PORDT. Item count = {purchaseOrder.Items?.Count ?? 0}");

                await InsertPurchaseOrderItemsAsync(
                    connection,
                    transaction,
                    ordNo,
                    purchaseOrder,
                    userId);

                Console.WriteLine(
                    $"PORD CREATE: PORDT inserted successfully. ORDNO = {ordNo}");

                // ============================================================
                // 4. Insert PORDD
                // ============================================================

                Console.WriteLine(
                    $"PORD CREATE: Inserting PORDD. Additional count = {purchaseOrder.AdditionalDetails?.Count ?? 0}");

                await InsertPurchaseOrderAdditionalAsync(
                    connection,
                    transaction,
                    ordNo,
                    purchaseOrder,
                    userId);

                Console.WriteLine(
                    $"PORD CREATE: PORDD inserted successfully. ORDNO = {ordNo}");

                // ============================================================
                // 5. Insert STKDT
                // ============================================================

                Console.WriteLine(
                    $"PORD CREATE: Inserting STKDT. Item count = {purchaseOrder.Items?.Count ?? 0}");

                await InsertPurchaseOrderStockDetailsAsync(
                    connection,
                    transaction,
                    ordNo,
                    purchaseOrder,
                    userId);

                Console.WriteLine(
                    $"PORD CREATE: STKDT inserted successfully. ORDNO = {ordNo}");

                // ============================================================
                // 6. Commit
                // ============================================================

                Console.WriteLine(
                    $"PORD CREATE: Committing transaction. ORDNO = {ordNo}");

                await transaction.CommitAsync();

                Console.WriteLine(
                    $"PORD CREATE: Transaction committed successfully. ORDNO = {ordNo}");

                return ordNo;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "============================================================");

                Console.WriteLine(
                    "PORD CREATE ERROR");

                Console.WriteLine(
                    $"Message: {ex.Message}");

                Console.WriteLine(
                    $"Type: {ex.GetType().FullName}");

                Console.WriteLine(
                    $"StackTrace: {ex.StackTrace}");

                Console.WriteLine(
                    "============================================================");

                try
                {
                    await transaction.RollbackAsync();

                    Console.WriteLine(
                        "PORD CREATE: Transaction rolled back.");
                }
                catch (Exception rollbackEx)
                {
                    Console.WriteLine(
                        $"PORD CREATE: Rollback failed: {rollbackEx.Message}");
                }

                throw;
            }
        }
        private async Task InsertPurchaseOrderItemsAsync(
     SqlConnection connection,
     SqlTransaction transaction,
     int ordNo,
     PurchaseOrderDTO purchaseOrder,
     int userId)
        {
            const string sql = @"
        INSERT INTO PORDT
        (
            ORDNO,
            ORDDATE,
            SUPCODE,
            PRCODE,
            QTY,
            UNITCODE,
            RATE,
            BTYPE,
            LOGCD,
            TAXCODE,
            DISC,
            TAX,
            CREATEDDT,
            GDWCODE,
            CURCONRATE,
            CURCODE,
            ORGQTY,
            ORGRATE,
            CANCELFLG,
            PRFLG,
            CONVTYPE,
            CONVFACT,
            CONVQTY,
            CONVRATE,
            LDISCTYPE,
            LDISCPERC,
            LDISCAMT,
            DISCP,
            BML,
            BMQ,
            BMW,
            VSVRTYPE,
            PRDESC,
            ORDROWID,
            ALCODE,
            ALNAME
        )
        VALUES
        (
            @OrdNo,
            @OrdDate,
            @SupCode,
            @PrCode,
            @Qty,
            @UnitCode,
            @Rate,
            @BType,
            @LogCd,
            @TaxCode,
            @Disc,
            @Tax,
            @CreatedDt,
            @GdwCode,
            @CurConRate,
            @CurCode,
            @OrgQty,
            @OrgRate,
            @CancelFlg,
            @PrFlg,
            @ConvType,
            @ConvFact,
            @ConvQty,
            @ConvRate,
            @LDiscType,
            @LDiscPerc,
            @LDiscAmt,
            @DiscP,
            @BML,
            @BMQ,
            @BMW,
            @VSVRTYPE,
            @PrDesc,
            @OrdRowId,
            @AlCode,
            @AlName
        );";

            if (purchaseOrder.Items == null || purchaseOrder.Items.Count == 0)
                return;

            foreach (var item in purchaseOrder.Items)
            {
                /*
                 * ---------------------------------------------------------
                 * Legacy SavePordT() defaults
                 * ---------------------------------------------------------
                 */

                // Legacy:
                // OrgQty is not assigned in SavePordT().
                // It represents the original quantity, so preserve supplied
                // value or use current Qty for a new item.
                decimal orgQty =
                    item.OrgQty != 0
                        ? item.OrgQty
                        : item.Qty;

                // Legacy:
                // OrgRate is not assigned in SavePordT().
                // Preserve supplied value or use current Rate for a new item.
                decimal orgRate =
                    item.OrgRate != 0
                        ? item.OrgRate
                        : item.Rate;

                // Legacy:
                // ConvFact = supplied value when non-zero,
                // otherwise 1.
                decimal convFact =
                    item.ConvFact != 0
                        ? item.ConvFact
                        : 1m;

                // Legacy:
                // ConvQty starts from Qty.
                //
                // Any alias / alternative-unit adjustment should already be
                // calculated before this method if it is required.
                decimal convQty =
                    item.ConvQty != 0
                        ? item.ConvQty
                        : item.Qty;

                /*
                 * Legacy SavePordT() has:
                 *
                 * if (true)
                 * {
                 *     if (col_conv_rate supplied)
                 *         ConvRate = ToBaseActual(col_conv_rate)
                 *     else
                 *         ConvRate = ToBaseActual(price)
                 * }
                 *
                 * Therefore, at DB insertion level we preserve the value
                 * supplied by the DTO and fall back to Rate.
                 */
                decimal convRate =
                    item.ConvRate != 0
                        ? item.ConvRate
                        : item.Rate;

                /*
                 * ---------------------------------------------------------
                 * Legacy LDiscType
                 * ---------------------------------------------------------
                 *
                 * Exact legacy values:
                 *
                 * "Discount Based on %"
                 * "Absolute Value"
                 * ""
                 */
                string lDiscType;

                if (!string.IsNullOrWhiteSpace(item.LDiscType))
                {
                    lDiscType = item.LDiscType;
                }
                else if (item.LDiscPerc != 0)
                {
                    lDiscType = "Discount Based on %";
                }
                else if (item.LDiscAmt != 0)
                {
                    lDiscType = "Absolute Value";
                }
                else
                {
                    lDiscType = "";
                }

                /*
                 * ---------------------------------------------------------
                 * Create command
                 * ---------------------------------------------------------
                 */
                using var command = new SqlCommand(
                    sql,
                    connection,
                    transaction);

                command.Parameters.AddWithValue(
                    "@OrdNo",
                    ordNo);

                command.Parameters.AddWithValue(
                    "@OrdDate",
                    item.OrdDate
                        ?? purchaseOrder.OrdDate
                        ?? DateTime.Now);

                command.Parameters.AddWithValue(
                    "@SupCode",
                    item.SupCode != 0
                        ? item.SupCode
                        : purchaseOrder.SupCode);

                command.Parameters.AddWithValue(
                    "@PrCode",
                    item.PrCode);

                command.Parameters.AddWithValue(
                    "@Qty",
                    item.Qty);

                command.Parameters.AddWithValue(
                    "@UnitCode",
                    item.UnitCode);

                /*
                 * Legacy SavePordT():
                 *
                 * if (Variables.INVAMTEDIT_PI)
                 *     Rate = CurrConvDB.ToBaseActual(price,...)
                 * else
                 *     Rate = CurrConvDB.ToBase(price,...)
                 *
                 * The API receives item.Rate as the value to store.
                 * Do not convert it again here.
                 */
                command.Parameters.AddWithValue(
                    "@Rate",
                    item.Rate);

                command.Parameters.AddWithValue(
                    "@BType",
                    (object?)item.BType ?? DBNull.Value);

                /*
                 * Legacy:
                 * clsPordT.LogCd is not explicitly assigned in SavePordT(),
                 * but PORDT insert uses the current user/log code.
                 */
                command.Parameters.AddWithValue(
                    "@LogCd",
                    userId);

                command.Parameters.AddWithValue(
                    "@TaxCode",
                    item.TaxCode);

                /*
                 * Legacy:
                 *
                 * if discount supplied
                 *     Disc = CurrConvDB.ToBaseActual(discount,...)
                 * else
                 *     Disc = 0
                 *
                 * DTO contains the value to store.
                 */
                command.Parameters.AddWithValue(
                    "@Disc",
                    item.Disc);

                command.Parameters.AddWithValue(
                    "@Tax",
                    item.Tax);

                command.Parameters.AddWithValue(
                    "@CreatedDt",
                    item.CreatedDt
                        ?? DateTime.Now);

                /*
                 * Legacy:
                 * clsPordT.Gdwcode = selected godown.
                 */
                command.Parameters.AddWithValue(
                    "@GdwCode",
                    item.GdwCode);

                command.Parameters.AddWithValue(
                    "@CurConRate",
                    item.CurConRate != 0
                        ? item.CurConRate
                        : purchaseOrder.CurConRate);

                command.Parameters.AddWithValue(
                    "@CurCode",
                    item.CurCode != 0
                        ? item.CurCode
                        : purchaseOrder.CurCode);

                command.Parameters.AddWithValue(
                    "@OrgQty",
                    orgQty);

                command.Parameters.AddWithValue(
                    "@OrgRate",
                    orgRate);

                /*
                 * New PORDT row should normally be false.
                 * Preserve explicit DTO value for update/reinsert scenarios.
                 */
                command.Parameters.AddWithValue(
                    "@CancelFlg",
                    item.CancelFlg);

                command.Parameters.AddWithValue(
                    "@PrFlg",
                    item.PrFlg);

                command.Parameters.AddWithValue(
                    "@ConvType",
                    item.ConvType);

                command.Parameters.AddWithValue(
                    "@ConvFact",
                    convFact);

                command.Parameters.AddWithValue(
                    "@ConvQty",
                    convQty);

                /*
                 * Legacy ConvRate always uses ToBaseActual()
                 * because the legacy code currently contains:
                 *
                 * if (true)
                 */
                command.Parameters.AddWithValue(
                    "@ConvRate",
                    convRate);

                command.Parameters.AddWithValue(
                    "@LDiscType",
                    lDiscType);

                command.Parameters.AddWithValue(
                    "@LDiscPerc",
                    item.LDiscPerc);

                /*
                 * Legacy:
                 * LDiscAmt = CurrConvDB.ToBase(...)
                 *
                 * DTO contains the value to store.
                 */
                command.Parameters.AddWithValue(
                    "@LDiscAmt",
                    item.LDiscAmt);

                command.Parameters.AddWithValue(
                    "@DiscP",
                    item.DiscP);

                /*
                 * Legacy:
                 * BML = Length
                 * BMQ = QtyNos
                 * BMW = Weight
                 */
                command.Parameters.AddWithValue(
                    "@BML",
                    item.BML);

                command.Parameters.AddWithValue(
                    "@BMQ",
                    item.BMQ);

                command.Parameters.AddWithValue(
                    "@BMW",
                    item.BMW);

                command.Parameters.AddWithValue(
                    "@VSVRTYPE",
                    item.VSVRTYPE != 0
                        ? item.VSVRTYPE
                        : purchaseOrder.VSVRTYPE);

                command.Parameters.AddWithValue(
                    "@PrDesc",
                    (object?)item.PrDesc ?? DBNull.Value);

                command.Parameters.AddWithValue(
                    "@OrdRowId",
                    item.OrdRowId);

                command.Parameters.AddWithValue(
                    "@AlCode",
                    (object?)item.AlCode ?? DBNull.Value);

                command.Parameters.AddWithValue(
                    "@AlName",
                    (object?)item.AlName ?? DBNull.Value);

                await command.ExecuteNonQueryAsync();
            }
        }
        private async Task InsertPurchaseOrderAdditionalAsync(
     SqlConnection connection,
     SqlTransaction transaction,
     int ordNo,
     PurchaseOrderDTO purchaseOrder,
     int userId)
        {
            const string sql = @"
        INSERT INTO PORDD
        (
            ORDNO,
            ORDDATE,
            SUPCODE,
            ETYPE,
            CODE,
            AMOUNT,
            TAX,
            BTYPE,
            LOGCD,
            TAXAMT,
            VRTYPE,
            CURCONRATE,
            CURCODE,
            DFLAG,
            ORGAMT,
            ORGTAXAMT,
            CANCELFLG,
            VSVRTYPE
        )
        VALUES
        (
            @OrdNo,
            @OrdDate,
            @SupCode,
            @EType,
            @Code,
            @Amount,
            @Tax,
            @BType,
            @LogCd,
            @TaxAmt,
            @VrType,
            @CurConRate,
            @CurCode,
            @DFlag,
            @OrgAmt,
            @OrgTaxAmt,
            @CancelFlg,
            @VSVRTYPE
        )";

            foreach (var item in purchaseOrder.AdditionalDetails)
            {
                using var command = new SqlCommand(sql, connection, transaction);

                command.Parameters.AddWithValue("@OrdNo", ordNo);

                command.Parameters.AddWithValue(
                    "@OrdDate",
                    item.OrdDate
                        ?? purchaseOrder.OrdDate
                        ?? DateTime.Now);

                command.Parameters.AddWithValue(
                    "@SupCode",
                    item.SupCode != 0
                        ? item.SupCode
                        : purchaseOrder.SupCode);

                command.Parameters.AddWithValue(
                    "@EType",
                    (object?)item.EType ?? DBNull.Value);

                command.Parameters.AddWithValue(
                    "@Code",
                    item.Code);

                command.Parameters.AddWithValue(
                    "@Amount",
                    item.Amount);

                command.Parameters.AddWithValue(
                    "@Tax",
                    item.Tax);

                command.Parameters.AddWithValue(
                    "@BType",
                    (object?)item.BType ?? DBNull.Value);

                // Legacy: LOGCD = Variables.UserId
                command.Parameters.AddWithValue(
                    "@LogCd",
                    userId);

                command.Parameters.AddWithValue(
                    "@TaxAmt",
                    item.TaxAmt);

                // Legacy SavePordD(): VRTYPE = "O"
                command.Parameters.AddWithValue(
                    "@VrType",
                    string.IsNullOrWhiteSpace(item.VrType)
                        ? "O"
                        : item.VrType);

                command.Parameters.AddWithValue(
                    "@CurConRate",
                    item.CurConRate != 0
                        ? item.CurConRate
                        : purchaseOrder.CurConRate);

                command.Parameters.AddWithValue(
                    "@CurCode",
                    item.CurCode != 0
                        ? item.CurCode
                        : purchaseOrder.CurCode);

                command.Parameters.AddWithValue(
                    "@DFlag",
                    item.DFlag);

                // IMPORTANT:
                // Legacy SavePordD() does not set OrgAmt/OrgTaxAmt.
                // Therefore keep the DTO values exactly as supplied.
                command.Parameters.AddWithValue(
                    "@OrgAmt",
                    item.OrgAmt);

                command.Parameters.AddWithValue(
                    "@OrgTaxAmt",
                    item.OrgTaxAmt);

                command.Parameters.AddWithValue(
                    "@CancelFlg",
                    item.CancelFlg);

                command.Parameters.AddWithValue(
                    "@VSVRTYPE",
                    item.VSVRTYPE != 0
                        ? item.VSVRTYPE
                        : purchaseOrder.VSVRTYPE);

                await command.ExecuteNonQueryAsync();
            }
        }
        public async Task<PaginationResponse> GetPurchaseOrdersAsync(
            PaginationRequest pagination)
        {
            pagination.Normalize();

            var connection = db.GetConnection();

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            var where = "WHERE 1 = 1";

            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(pagination.Search))
            {
                where += @" AND (
                    CAST(ORDNO AS VARCHAR(50)) LIKE @Search
                    OR CAST(SUPCODE AS VARCHAR(50)) LIKE @Search
                    OR ISNULL(VRTYPE, '') LIKE @Search
                    OR ISNULL(REMARKS, '') LIKE @Search
                )";

                parameters.Add(
                    new SqlParameter(
                        "@Search",
                        $"%{pagination.Search.Trim()}%"));
            }

            var countSql = $@"
SELECT COUNT(*)
FROM PORDM
{where}";

            int totalRecords;

            using (var countCommand =
                new SqlCommand(countSql, connection))
            {
                foreach (var parameter in parameters)
                {
                    countCommand.Parameters.AddWithValue(
                        parameter.ParameterName,
                        parameter.Value);
                }

                totalRecords = Convert.ToInt32(
                    await countCommand.ExecuteScalarAsync());
            }

            var sortColumn = "ORDNO";

            if (!string.IsNullOrWhiteSpace(pagination.SortBy))
            {
                sortColumn = pagination.SortBy.ToLower() switch
                {
                    "ordno" => "ORDNO",
                    "orddate" => "ORDDATE",
                    "supcode" => "SUPCODE",
                    "amount" => "AMOUNT",
                    "tvalue" => "TVALUE",
                    "ntvalue" => "NTVALUE",
                    _ => "ORDNO"
                };
            }

            var sortDirection =
                pagination.SortDirection == "ASC"
                    ? "ASC"
                    : "DESC";

            var sql = $@"
SELECT ORDNO,SUPCODE,VRTYPE,ORDDATE,DISCP,DISCVAL,TAX,NTVALUE,TVALUE,COINADJ,CUSDUITY,AMOUNT,FREIGHT,REMARKS,SHIPADDRESS,INVNO,INVDATE,
    CURCONRATE,CURCODE,GRNO,CANCELFLG,INVNOTEXT,VSVRTYPE,VSDOCNO,VSORDCD,REFNO,TRANSFERRED,INFORMAT,POF1,POF2, POF3,POF4,POF5,POF6,POF7,POF8,
    POF9,POF10,POF11,POF12,POF13,POF14,POF15,POF16, POF17,POF18,POF19,POF20,
    APRVLST
FROM PORDM
{where}
ORDER BY {sortColumn} {sortDirection}
OFFSET @Offset ROWS
FETCH NEXT @PageSize ROWS ONLY";

            var result = new List<PurchaseOrderDTO>();

            using (var command =
                new SqlCommand(sql, connection))
            {
                foreach (var parameter in parameters)
                {
                    command.Parameters.AddWithValue(
                        parameter.ParameterName,
                        parameter.Value);
                }

                command.Parameters.AddWithValue(
                    "@Offset",
                    pagination.Page * pagination.PageSize);

                command.Parameters.AddWithValue(
                    "@PageSize",
                    pagination.PageSize);

                using var reader =
                    await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    result.Add(new PurchaseOrderDTO
                    {
                        OrdNo = reader.GetInt32(
                            reader.GetOrdinal("ORDNO")),

                        SupCode = reader.GetInt32(
                            reader.GetOrdinal("SUPCODE")),

                        VrType = reader["VRTYPE"] == DBNull.Value
                            ? null
                            : reader["VRTYPE"].ToString(),

                        OrdDate = reader["ORDDATE"] == DBNull.Value
                            ? null
                            : Convert.ToDateTime(
                                reader["ORDDATE"]),

                        DiscP = reader["DISCP"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(
                                reader["DISCP"]),

                        DiscVal = reader["DISCVAL"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(
                                reader["DISCVAL"]),

                        Tax = reader["TAX"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(
                                reader["TAX"]),

                        NtValue = reader["NTVALUE"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(
                                reader["NTVALUE"]),

                        TValue = reader["TVALUE"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(
                                reader["TVALUE"]),

                        CoinAdj = reader["COINADJ"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(
                                reader["COINADJ"]),

                        CusDuity = reader["CUSDUITY"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(
                                reader["CUSDUITY"]),

                        Amount = reader["AMOUNT"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(
                                reader["AMOUNT"]),

                        Freight = reader["FREIGHT"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(
                                reader["FREIGHT"]),

                        Remarks = reader["REMARKS"] == DBNull.Value
                            ? null
                            : reader["REMARKS"].ToString(),

                        ShipAddress = reader["SHIPADDRESS"] == DBNull.Value
                            ? null
                            : reader["SHIPADDRESS"].ToString(),

                        InvNo = reader["INVNO"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(
                                reader["INVNO"]),

                        InvDate = reader["INVDATE"] == DBNull.Value
                            ? null
                            : Convert.ToDateTime(
                                reader["INVDATE"]),

                        CurConRate = reader["CURCONRATE"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(
                                reader["CURCONRATE"]),

                        CurCode = reader["CURCODE"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(
                                reader["CURCODE"]),

                        GrNo = reader["GRNO"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(
                                reader["GRNO"]),

                        CancelFlg = reader["CANCELFLG"] != DBNull.Value &&
                                    Convert.ToBoolean(
                                        reader["CANCELFLG"]),

                        InvNoText = reader["INVNOTEXT"] == DBNull.Value
                            ? null
                            : reader["INVNOTEXT"].ToString(),

                        VSVRTYPE = reader["VSVRTYPE"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(
                                reader["VSVRTYPE"]),

                        VSDOCNO = reader["VSDOCNO"] == DBNull.Value
                            ? null
                            : reader["VSDOCNO"].ToString(),

                        VSORDCD = reader["VSORDCD"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(
                                reader["VSORDCD"]),

                        RefNo = reader["REFNO"] == DBNull.Value
                            ? null
                            : reader["REFNO"].ToString(),

                        Transferred = reader["TRANSFERRED"] == DBNull.Value
                            ? null
                            : reader["TRANSFERRED"].ToString(),

                        InFormat = reader["INFORMAT"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(
                                reader["INFORMAT"]),

                        POF1 = reader["POF1"] == DBNull.Value
                            ? null
                            : reader["POF1"].ToString(),

                        POF2 = reader["POF2"] == DBNull.Value
                            ? null
                            : reader["POF2"].ToString(),

                        POF3 = reader["POF3"] == DBNull.Value
                            ? null
                            : reader["POF3"].ToString(),

                        POF4 = reader["POF4"] == DBNull.Value
                            ? null
                            : reader["POF4"].ToString(),

                        POF5 = reader["POF5"] == DBNull.Value
                            ? null
                            : reader["POF5"].ToString(),

                        POF6 = reader["POF6"] == DBNull.Value
                            ? null
                            : reader["POF6"].ToString(),

                        POF7 = reader["POF7"] == DBNull.Value
                            ? null
                            : reader["POF7"].ToString(),

                        POF8 = reader["POF8"] == DBNull.Value
                            ? null
                            : reader["POF8"].ToString(),

                        POF9 = reader["POF9"] == DBNull.Value
                            ? null
                            : reader["POF9"].ToString(),

                        POF10 = reader["POF10"] == DBNull.Value
                            ? null
                            : reader["POF10"].ToString(),

                        POF11 = reader["POF11"] == DBNull.Value
                            ? null
                            : reader["POF11"].ToString(),

                        POF12 = reader["POF12"] == DBNull.Value
                            ? null
                            : reader["POF12"].ToString(),

                        POF13 = reader["POF13"] == DBNull.Value
                            ? null
                            : reader["POF13"].ToString(),

                        POF14 = reader["POF14"] == DBNull.Value
                            ? null
                            : reader["POF14"].ToString(),

                        POF15 = reader["POF15"] == DBNull.Value
                            ? null
                            : reader["POF15"].ToString(),

                        POF16 = reader["POF16"] == DBNull.Value
                            ? null
                            : reader["POF16"].ToString(),

                        POF17 = reader["POF17"] == DBNull.Value
                            ? null
                            : reader["POF17"].ToString(),

                        POF18 = reader["POF18"] == DBNull.Value
                            ? null
                            : reader["POF18"].ToString(),

                        POF19 = reader["POF19"] == DBNull.Value
                            ? null
                            : reader["POF19"].ToString(),

                        POF20 = reader["POF20"] == DBNull.Value
                            ? null
                            : reader["POF20"].ToString(),

                        AprvlLst = reader["APRVLST"] == DBNull.Value
                            ? null
                            : reader["APRVLST"].ToString(),

                        Items = new List<PurchaseOrderItemDTO>(),

                        AdditionalDetails =
                            new List<PurchaseOrderAdditionalDTO>()
                    });
                }
            }

            int totalPages = totalRecords == 0
                ? 0
                : (int)Math.Ceiling(
                    totalRecords /
                    (double)pagination.PageSize);

            return new PaginationResponse
            {
                //Data = result,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalRecords = totalRecords,
                TotalPages = totalPages
            };
        }

        public async Task<PurchaseOrderDTO?> GetPurchaseOrderByOrdNoAsync(int ordNo)
        {
            var connection = db.GetConnection();

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            PurchaseOrderDTO? purchaseOrder = null;

            const string headerSql = @"
SELECT  ORDNO,SUPCODE,VRTYPE,ORDDATE,DISCP,DISCVAL,TAX,NTVALUE,TVALUE,COINADJ,CUSDUITY,AMOUNT,FREIGHT,REMARKS,SHIPADDRESS,INVNO,INVDATE,
    CURCONRATE,CURCODE,GRNO,CANCELFLG,INVNOTEXT,VSVRTYPE,VSDOCNO,VSORDCD,REFNO,TRANSFERRED,INFORMAT,POF1,POF2,POF3,POF4,POF5,POF6,
    POF7,POF8,POF9,POF10,POF11,POF12,POF13,POF14,POF15,POF16,POF17,POF18,POF19,POF20,
    APRVLST
FROM PORDM
WHERE ORDNO = @OrdNo";

            using (var command = new SqlCommand(headerSql, connection))
            {
                command.Parameters.AddWithValue("@OrdNo", ordNo);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    purchaseOrder = new PurchaseOrderDTO
                    {
                        OrdNo = Convert.ToInt32(reader["ORDNO"]),
                        SupCode = Convert.ToInt32(reader["SUPCODE"]),

                        VrType = reader["VRTYPE"] == DBNull.Value
                            ? null
                            : reader["VRTYPE"].ToString(),

                        OrdDate = reader["ORDDATE"] == DBNull.Value
                            ? null
                            : Convert.ToDateTime(reader["ORDDATE"]),

                        DiscP = reader["DISCP"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["DISCP"]),

                        DiscVal = reader["DISCVAL"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["DISCVAL"]),

                        Tax = reader["TAX"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["TAX"]),

                        NtValue = reader["NTVALUE"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["NTVALUE"]),

                        TValue = reader["TVALUE"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["TVALUE"]),

                        CoinAdj = reader["COINADJ"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["COINADJ"]),

                        CusDuity = reader["CUSDUITY"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["CUSDUITY"]),

                        Amount = reader["AMOUNT"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["AMOUNT"]),

                        Freight = reader["FREIGHT"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["FREIGHT"]),

                        Remarks = reader["REMARKS"] == DBNull.Value
                            ? null
                            : reader["REMARKS"].ToString(),

                        ShipAddress = reader["SHIPADDRESS"] == DBNull.Value
                            ? null
                            : reader["SHIPADDRESS"].ToString(),

                        InvNo = reader["INVNO"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(reader["INVNO"]),

                        InvDate = reader["INVDATE"] == DBNull.Value
                            ? null
                            : Convert.ToDateTime(reader["INVDATE"]),

                        CurConRate = reader["CURCONRATE"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["CURCONRATE"]),

                        CurCode = reader["CURCODE"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(reader["CURCODE"]),

                        GrNo = reader["GRNO"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(reader["GRNO"]),

                        CancelFlg = reader["CANCELFLG"] != DBNull.Value &&
                                    Convert.ToBoolean(reader["CANCELFLG"]),

                        InvNoText = reader["INVNOTEXT"] == DBNull.Value
                            ? null
                            : reader["INVNOTEXT"].ToString(),

                        VSVRTYPE = reader["VSVRTYPE"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(reader["VSVRTYPE"]),

                        VSDOCNO = reader["VSDOCNO"] == DBNull.Value
                            ? null
                            : reader["VSDOCNO"].ToString(),

                        VSORDCD = reader["VSORDCD"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(reader["VSORDCD"]),

                        RefNo = reader["REFNO"] == DBNull.Value
                            ? null
                            : reader["REFNO"].ToString(),

                        Transferred = reader["TRANSFERRED"] == DBNull.Value
                            ? null
                            : reader["TRANSFERRED"].ToString(),

                        InFormat = reader["INFORMAT"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(reader["INFORMAT"]),

                        POF1 = reader["POF1"] == DBNull.Value ? null : reader["POF1"].ToString(),
                        POF2 = reader["POF2"] == DBNull.Value ? null : reader["POF2"].ToString(),
                        POF3 = reader["POF3"] == DBNull.Value ? null : reader["POF3"].ToString(),
                        POF4 = reader["POF4"] == DBNull.Value ? null : reader["POF4"].ToString(),
                        POF5 = reader["POF5"] == DBNull.Value ? null : reader["POF5"].ToString(),
                        POF6 = reader["POF6"] == DBNull.Value ? null : reader["POF6"].ToString(),
                        POF7 = reader["POF7"] == DBNull.Value ? null : reader["POF7"].ToString(),
                        POF8 = reader["POF8"] == DBNull.Value ? null : reader["POF8"].ToString(),
                        POF9 = reader["POF9"] == DBNull.Value ? null : reader["POF9"].ToString(),
                        POF10 = reader["POF10"] == DBNull.Value ? null : reader["POF10"].ToString(),
                        POF11 = reader["POF11"] == DBNull.Value ? null : reader["POF11"].ToString(),
                        POF12 = reader["POF12"] == DBNull.Value ? null : reader["POF12"].ToString(),
                        POF13 = reader["POF13"] == DBNull.Value ? null : reader["POF13"].ToString(),
                        POF14 = reader["POF14"] == DBNull.Value ? null : reader["POF14"].ToString(),
                        POF15 = reader["POF15"] == DBNull.Value ? null : reader["POF15"].ToString(),
                        POF16 = reader["POF16"] == DBNull.Value ? null : reader["POF16"].ToString(),
                        POF17 = reader["POF17"] == DBNull.Value ? null : reader["POF17"].ToString(),
                        POF18 = reader["POF18"] == DBNull.Value ? null : reader["POF18"].ToString(),
                        POF19 = reader["POF19"] == DBNull.Value ? null : reader["POF19"].ToString(),
                        POF20 = reader["POF20"] == DBNull.Value ? null : reader["POF20"].ToString(),

                        AprvlLst = reader["APRVLST"] == DBNull.Value
                            ? null
                            : reader["APRVLST"].ToString(),

                        Items = new List<PurchaseOrderItemDTO>(),
                        AdditionalDetails = new List<PurchaseOrderAdditionalDTO>()
                    };
                }
            }

            if (purchaseOrder == null)
                return null;

            const string itemSql = @"
SELECT ORDNO,SUPCODE,PRCODE,UNITCODE,LOGCD,TAXCODE,QTY, RATE,DISC,TAX,BTYPE, ORDDATE, CREATEDDT,GDWCODE,CURCONRATE,CURCODE,ORGQTY,ORGRATE,CANCELFLG,PRFLG,
    CONVTYPE,CONVFACT,CONVQTY,CONVRATE,LDISCTYPE,LDISCPERC,LDISCAMT,DISCP,BML,BMQ,BMW,VSVRTYPE,PRDESC,ORDROWID,ALCODE,ALNAME
FROM PORDT
WHERE ORDNO = @OrdNo
ORDER BY ORDROWID";

            using (var command = new SqlCommand(itemSql, connection))
            {
                command.Parameters.AddWithValue("@OrdNo", ordNo);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    purchaseOrder.Items.Add(new PurchaseOrderItemDTO
                    {
                        OrdNo = Convert.ToInt32(reader["ORDNO"]),
                        SupCode = Convert.ToInt32(reader["SUPCODE"]),
                        PrCode = Convert.ToInt32(reader["PRCODE"]),
                        UnitCode = Convert.ToInt32(reader["UNITCODE"]),
                        LogCd = reader["LOGCD"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(reader["LOGCD"]),

                        TaxCode = reader["TAXCODE"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(reader["TAXCODE"]),

                        Qty = reader["QTY"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QTY"]),
                        Rate = reader["RATE"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["RATE"]),
                        Disc = reader["DISC"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["DISC"]),
                        Tax = reader["TAX"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TAX"]),

                        BType = reader["BTYPE"] == DBNull.Value ? null : reader["BTYPE"].ToString(),

                        OrdDate = reader["ORDDATE"] == DBNull.Value
                            ? null
                            : Convert.ToDateTime(reader["ORDDATE"]),

                        CreatedDt = reader["CREATEDDT"] == DBNull.Value
                            ? null
                            : Convert.ToDateTime(reader["CREATEDDT"]),

                        GdwCode = reader["GDWCODE"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(reader["GDWCODE"]),

                        CurConRate = reader["CURCONRATE"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["CURCONRATE"]),

                        CurCode = reader["CURCODE"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(reader["CURCODE"]),

                        OrgQty = reader["ORGQTY"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["ORGQTY"]),

                        OrgRate = reader["ORGRATE"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["ORGRATE"]),

                        CancelFlg = reader["CANCELFLG"] != DBNull.Value &&
                                    Convert.ToBoolean(reader["CANCELFLG"]),

                        PrFlg = reader["PRFLG"] != DBNull.Value &&
                                Convert.ToBoolean(reader["PRFLG"]),

                        ConvType = reader["CONVTYPE"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(reader["CONVTYPE"]),

                        ConvFact = reader["CONVFACT"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["CONVFACT"]),

                        ConvQty = reader["CONVQTY"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["CONVQTY"]),

                        ConvRate = reader["CONVRATE"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["CONVRATE"]),

                        LDiscType = reader["LDISCTYPE"] == DBNull.Value
                            ? null
                            : reader["LDISCTYPE"].ToString(),

                        LDiscPerc = reader["LDISCPERC"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["LDISCPERC"]),

                        LDiscAmt = reader["LDISCAMT"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["LDISCAMT"]),

                        DiscP = reader["DISCP"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["DISCP"]),

                        BML = reader["BML"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["BML"]),

                        BMQ = reader["BMQ"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["BMQ"]),

                        BMW = reader["BMW"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["BMW"]),

                        VSVRTYPE = reader["VSVRTYPE"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(reader["VSVRTYPE"]),

                        PrDesc = reader["PRDESC"] == DBNull.Value
                            ? null
                            : reader["PRDESC"].ToString(),

                        OrdRowId = reader["ORDROWID"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(reader["ORDROWID"]),

                        AlCode = reader["ALCODE"] == DBNull.Value
                            ? null
                            : reader["ALCODE"].ToString(),

                        AlName = reader["ALNAME"] == DBNull.Value
                            ? null
                            : reader["ALNAME"].ToString()
                    });
                }
            }

            const string additionalSql = @"
         SELECT  ORDNO,ORDDATE,SUPCODE,ETYPE,CODE,AMOUNT,TAX,BTYPE,LOGCD,TAXAMT,VRTYPE,CURCONRATE,CURCODE,DFLAG,ORGAMT,ORGTAXAMT,CANCELFLG,VSVRTYPE
          FROM PORDD
            WHERE ORDNO = @OrdNo
              ORDER BY CODE";

            using (var command = new SqlCommand(additionalSql, connection))
            {
                command.Parameters.AddWithValue("@OrdNo", ordNo);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    purchaseOrder.AdditionalDetails.Add(
                        new PurchaseOrderAdditionalDTO
                        {
                            OrdNo = Convert.ToInt32(reader["ORDNO"]),

                            OrdDate = reader["ORDDATE"] == DBNull.Value
                                ? null
                                : Convert.ToDateTime(reader["ORDDATE"]),

                            SupCode = Convert.ToInt32(reader["SUPCODE"]),

                            EType = reader["ETYPE"] == DBNull.Value
                                ? null
                                : reader["ETYPE"].ToString(),

                            Code = reader["CODE"] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(reader["CODE"]),

                            Amount = reader["AMOUNT"] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(reader["AMOUNT"]),

                            Tax = reader["TAX"] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(reader["TAX"]),

                            BType = reader["BTYPE"] == DBNull.Value
                                ? null
                                : reader["BTYPE"].ToString(),

                            LogCd = reader["LOGCD"] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(reader["LOGCD"]),

                            TaxAmt = reader["TAXAMT"] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(reader["TAXAMT"]),

                            VrType = reader["VRTYPE"] == DBNull.Value
                                ? null
                                : reader["VRTYPE"].ToString(),

                            CurConRate = reader["CURCONRATE"] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(reader["CURCONRATE"]),

                            CurCode = reader["CURCODE"] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(reader["CURCODE"]),

                            DFlag = reader["DFLAG"] != DBNull.Value &&
                                    Convert.ToBoolean(reader["DFLAG"]),

                            OrgAmt = reader["ORGAMT"] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(reader["ORGAMT"]),

                            OrgTaxAmt = reader["ORGTAXAMT"] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(reader["ORGTAXAMT"]),

                            CancelFlg = reader["CANCELFLG"] != DBNull.Value &&
                                        Convert.ToBoolean(reader["CANCELFLG"]),

                            VSVRTYPE = reader["VSVRTYPE"] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(reader["VSVRTYPE"])
                        });
                }
            }

            return purchaseOrder;
        }

        public async Task<PurchaseOrderUpdateResult> UpdatePurchaseOrderAsync(
     int ordNo,
     PurchaseOrderDTO purchaseOrder,
     int userId)
        {
            var connection = db.GetConnection();

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            using var transaction =
                (SqlTransaction)await connection.BeginTransactionAsync();

            try
            {
                // ---------------------------------------------------------
                // 1. CHECK PURCHASE ORDER EXISTS
                // ---------------------------------------------------------
                const string checkSql = @"
SELECT COUNT(*)
FROM PORDM
WHERE ORDNO = @OrdNo";

                using (var checkCommand =
                    new SqlCommand(checkSql, connection, transaction))
                {
                    checkCommand.Parameters.AddWithValue("@OrdNo", ordNo);

                    int count = Convert.ToInt32(
                        await checkCommand.ExecuteScalarAsync());

                    if (count == 0)
                    {
                        await transaction.RollbackAsync();

                        return new PurchaseOrderUpdateResult
                        {
                            Success = false,
                            NotFound = true,
                            AlreadyLinked = false
                        };
                    }
                }

                // ---------------------------------------------------------
                // 2. LEGACY PROTECTION
                //
                // If Purchase Order already exists in STKDT,
                // it cannot be edited.
                // ---------------------------------------------------------
                const string checkStkDtSql = @"
SELECT COUNT(*)
FROM STKDT
WHERE ORDNO = @OrdNo
  AND VRTYPE = 'O'
  AND TYPE = 'R'";

                using (var stkDtCommand =
                    new SqlCommand(checkStkDtSql, connection, transaction))
                {
                    stkDtCommand.Parameters.AddWithValue("@OrdNo", ordNo);

                    int stkDtCount = Convert.ToInt32(
                        await stkDtCommand.ExecuteScalarAsync());

                    if (stkDtCount > 0)
                    {
                        await transaction.RollbackAsync();

                        return new PurchaseOrderUpdateResult
                        {
                            Success = false,
                            NotFound = false,
                            AlreadyLinked = true
                        };
                    }
                }

                // ---------------------------------------------------------
                // 3. UPDATE PORDM
                // ---------------------------------------------------------
                const string updateSql = @"
                    UPDATE PORDM
                     SET
                   VRTYPE = @VrType,
                   ORDDATE = @OrdDate,
                   SUPCODE = @SupCode,
                   DISCP = @DiscP,
                   DISCVAL = @DiscVal,
                   TAX = @Tax,
                   NTVALUE = @NtValue,
                   TVALUE = @TValue,
                   COINADJ = @CoinAdj,
                   CUSDUITY = @CusDuity,
                   AMOUNT = @Amount,
                   REMARKS = @Remarks,
                   FREIGHT = @Freight,
                   INFORMAT = @InFormat,
                   INVNO = @InvNo,
                   INVDATE = @InvDate,
                   CURCONRATE = @CurConRate,
                   CURCODE = @CurCode,
                   SHIPADDRESS = @ShipAddress,
                   GRNO = @GrNo,
                   CANCELFLG = @CancelFlg,
                   INVNOTEXT = @InvNoText,
                   VSVRTYPE = @VSVRTYPE,
                   VSDOCNO = @VSDOCNO,
                   VSORDCD = @VSORDCD,
                  REFNO = @RefNo,
                   TRANSFERRED = @Transferred,

                   POF1 = @POF1,
                  POF2 = @POF2,
                   POF3 = @POF3,
                    POF4 = @POF4,
                    POF5 = @POF5,
                    POF6 = @POF6,
                    POF7 = @POF7,
                    POF8 = @POF8,
                     POF9 = @POF9,
                      POF10 = @POF10,
                     POF11 = @POF11,
                     POF12 = @POF12,
                     POF13 = @POF13,
                     POF14 = @POF14,
                     POF15 = @POF15,
                     POF16 = @POF16,
                     POF17 = @POF17,
                     POF18 = @POF18,
                     POF19 = @POF19,
                       POF20 = @POF20,

                         APRVLST = @AprvlLst,
                         ULOGCD = @ULogCd,
                          UDATE = @UDate
                    WHERE ORDNO = @OrdNo";

                using (var command =
                    new SqlCommand(updateSql, connection, transaction))
                {
                    command.Parameters.AddWithValue("@OrdNo", ordNo);
                    command.Parameters.AddWithValue("@VrType", "O");
                    command.Parameters.AddWithValue(
                        "@OrdDate",
                        purchaseOrder.OrdDate ?? DateTime.Now);
                    command.Parameters.AddWithValue(
                        "@SupCode",
                        purchaseOrder.SupCode);

                    command.Parameters.AddWithValue(
                        "@DiscP",
                        purchaseOrder.DiscP);
                    command.Parameters.AddWithValue(
                        "@DiscVal",
                        purchaseOrder.DiscVal);
                    command.Parameters.AddWithValue(
                        "@Tax",
                        purchaseOrder.Tax);
                    command.Parameters.AddWithValue(
                        "@NtValue",
                        purchaseOrder.NtValue);
                    command.Parameters.AddWithValue(
                        "@TValue",
                        purchaseOrder.TValue);
                    command.Parameters.AddWithValue(
                        "@CoinAdj",
                        purchaseOrder.CoinAdj);
                    command.Parameters.AddWithValue(
                        "@CusDuity",
                        purchaseOrder.CusDuity);
                    command.Parameters.AddWithValue(
                        "@Amount",
                        purchaseOrder.Amount);

                    command.Parameters.AddWithValue(
                        "@Remarks",
                        (object?)purchaseOrder.Remarks ?? DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@Freight",
                        purchaseOrder.Freight);

                    command.Parameters.AddWithValue(
                        "@InFormat",
                        purchaseOrder.InFormat);

                    command.Parameters.AddWithValue(
                        "@InvNo",
                        purchaseOrder.InvNo);

                    command.Parameters.AddWithValue(
                        "@InvDate",
                        (object?)purchaseOrder.InvDate ?? DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@CurConRate",
                        purchaseOrder.CurConRate);

                    command.Parameters.AddWithValue(
                        "@CurCode",
                        purchaseOrder.CurCode);

                    command.Parameters.AddWithValue(
                        "@ShipAddress",
                        (object?)purchaseOrder.ShipAddress ?? DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@GrNo",
                        purchaseOrder.GrNo);

                    command.Parameters.AddWithValue(
                        "@CancelFlg",
                        purchaseOrder.CancelFlg);

                    command.Parameters.AddWithValue(
                        "@InvNoText",
                        (object?)purchaseOrder.InvNoText ?? DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@VSVRTYPE",
                        purchaseOrder.VSVRTYPE);

                    command.Parameters.AddWithValue(
                        "@VSDOCNO",
                        (object?)purchaseOrder.VSDOCNO ?? DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@VSORDCD",
                        purchaseOrder.VSORDCD);

                    command.Parameters.AddWithValue(
                        "@RefNo",
                        (object?)purchaseOrder.RefNo ?? DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@Transferred",
                        (object?)purchaseOrder.Transferred ?? DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@POF1",
                        (object?)purchaseOrder.POF1 ?? DBNull.Value);
                    command.Parameters.AddWithValue(
                        "@POF2",
                        (object?)purchaseOrder.POF2 ?? DBNull.Value);
                    command.Parameters.AddWithValue(
                        "@POF3",
                        (object?)purchaseOrder.POF3 ?? DBNull.Value);
                    command.Parameters.AddWithValue(
                        "@POF4",
                        (object?)purchaseOrder.POF4 ?? DBNull.Value);
                    command.Parameters.AddWithValue(
                        "@POF5",
                        (object?)purchaseOrder.POF5 ?? DBNull.Value);
                    command.Parameters.AddWithValue(
                        "@POF6",
                        (object?)purchaseOrder.POF6 ?? DBNull.Value);
                    command.Parameters.AddWithValue(
                        "@POF7",
                        (object?)purchaseOrder.POF7 ?? DBNull.Value);
                    command.Parameters.AddWithValue(
                        "@POF8",
                        (object?)purchaseOrder.POF8 ?? DBNull.Value);
                    command.Parameters.AddWithValue(
                        "@POF9",
                        (object?)purchaseOrder.POF9 ?? DBNull.Value);
                    command.Parameters.AddWithValue(
                        "@POF10",
                        (object?)purchaseOrder.POF10 ?? DBNull.Value);
                    command.Parameters.AddWithValue(
                        "@POF11",
                        (object?)purchaseOrder.POF11 ?? DBNull.Value);
                    command.Parameters.AddWithValue(
                        "@POF12",
                        (object?)purchaseOrder.POF12 ?? DBNull.Value);
                    command.Parameters.AddWithValue(
                        "@POF13",
                        (object?)purchaseOrder.POF13 ?? DBNull.Value);
                    command.Parameters.AddWithValue(
                        "@POF14",
                        (object?)purchaseOrder.POF14 ?? DBNull.Value);
                    command.Parameters.AddWithValue(
                        "@POF15",
                        (object?)purchaseOrder.POF15 ?? DBNull.Value);
                    command.Parameters.AddWithValue(
                        "@POF16",
                        (object?)purchaseOrder.POF16 ?? DBNull.Value);
                    command.Parameters.AddWithValue(
                        "@POF17",
                        (object?)purchaseOrder.POF17 ?? DBNull.Value);
                    command.Parameters.AddWithValue(
                        "@POF18",
                        (object?)purchaseOrder.POF18 ?? DBNull.Value);
                    command.Parameters.AddWithValue(
                        "@POF19",
                        (object?)purchaseOrder.POF19 ?? DBNull.Value);
                    command.Parameters.AddWithValue(
                        "@POF20",
                        (object?)purchaseOrder.POF20 ?? DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@AprvlLst",
                        (object?)purchaseOrder.AprvlLst ?? DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@ULogCd",
                        userId);

                    command.Parameters.AddWithValue(
                        "@UDate",
                        DateTime.Now);

                    await command.ExecuteNonQueryAsync();
                }

                // ---------------------------------------------------------
                // 4. DELETE OLD PORDT
                // ---------------------------------------------------------
                const string deleteItemsSql = @"
DELETE FROM PORDT
WHERE ORDNO = @OrdNo";

                using (var command =
                    new SqlCommand(deleteItemsSql, connection, transaction))
                {
                    command.Parameters.AddWithValue("@OrdNo", ordNo);

                    await command.ExecuteNonQueryAsync();
                }

                // ---------------------------------------------------------
                // 5. INSERT UPDATED PORDT
                // ---------------------------------------------------------
                await InsertPurchaseOrderItemsAsync(
                    connection,
                    transaction,
                    ordNo,
                    purchaseOrder,
                    userId);

                // ---------------------------------------------------------
                // 6. DELETE OLD PORDD
                // ---------------------------------------------------------
                const string deleteAdditionalSql = @"
DELETE FROM PORDD
WHERE ORDNO = @OrdNo";

                using (var command =
                    new SqlCommand(deleteAdditionalSql, connection, transaction))
                {
                    command.Parameters.AddWithValue("@OrdNo", ordNo);

                    await command.ExecuteNonQueryAsync();
                }

                // ---------------------------------------------------------
                // 7. INSERT UPDATED PORDD
                // ---------------------------------------------------------
                await InsertPurchaseOrderAdditionalAsync(
                    connection,
                    transaction,
                    ordNo,
                    purchaseOrder,
                    userId);

                // ---------------------------------------------------------
                // 8. COMMIT
                // ---------------------------------------------------------
                await transaction.CommitAsync();

                return new PurchaseOrderUpdateResult
                {
                    Success = true,
                    NotFound = false,
                    AlreadyLinked = false
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<bool> CancelPurchaseOrderAsync(int ordNo, int userId)
        {
            var connection = db.GetConnection();

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            using var transaction =
                (SqlTransaction)await connection.BeginTransactionAsync();

            try
            {
                // =========================================================
                // CHECK PURCHASE ORDER EXISTS
                // =========================================================
                const string checkSql = @"
SELECT COUNT(1)
FROM PORDM
WHERE ORDNO = @OrdNo";

                using (var checkCommand =
                    new SqlCommand(checkSql, connection, transaction))
                {
                    checkCommand.Parameters.AddWithValue("@OrdNo", ordNo);

                    int count = Convert.ToInt32(
                        await checkCommand.ExecuteScalarAsync());

                    if (count == 0)
                        return false;
                }

                // =========================================================
                // CANCEL PURCHASE ORDER HEADER
                // =========================================================
                const string headerSql = @"
UPDATE PORDM
SET
    DISCP = 0,
    DISCVAL = 0,
    TAX = 0,
    NTVALUE = 0,
    TVALUE = 0,
    COINADJ = 0,
    CUSDUITY = 0,
    AMOUNT = 0,
    FREIGHT = 0,
    CANCELFLG = 'True',
    ULOGCD = @UserId,
    UDATE = GETDATE()
WHERE ORDNO = @OrdNo";

                using (var headerCommand =
                    new SqlCommand(headerSql, connection, transaction))
                {
                    headerCommand.Parameters.AddWithValue("@OrdNo", ordNo);
                    headerCommand.Parameters.AddWithValue("@UserId", userId);

                    await headerCommand.ExecuteNonQueryAsync();
                }

                // =========================================================
                // CANCEL PURCHASE ORDER ITEMS
                // =========================================================
                const string itemSql = @"
UPDATE PORDT
SET
    ORGQTY = CASE
        WHEN ORGQTY = 0 THEN QTY
        ELSE ORGQTY
    END,
    ORGRATE = CASE
        WHEN ORGRATE = 0 THEN RATE
        ELSE ORGRATE
    END,
    QTY = 0,
    RATE = 0,
    CONVTYPE = 0,
    CONVFACT = 0,
    CONVQTY = 0,
    CONVRATE = 0,
    CANCELFLG = 'True'
WHERE ORDNO = @OrdNo";

                using (var itemCommand =
                    new SqlCommand(itemSql, connection, transaction))
                {
                    itemCommand.Parameters.AddWithValue("@OrdNo", ordNo);

                    await itemCommand.ExecuteNonQueryAsync();
                }

                // =========================================================
                // CANCEL ADDITIONAL DETAILS
                // =========================================================
                const string additionalSql = @"
UPDATE PORDD
SET
    ORGAMT = CASE
        WHEN ORGAMT = 0 THEN AMOUNT
        ELSE ORGAMT
    END,
    ORGTAXAMT = CASE
        WHEN ORGTAXAMT = 0 THEN TAXAMT
        ELSE ORGTAXAMT
    END,
    AMOUNT = 0,
    TAX = 0,
    TAXAMT = 0,
    CANCELFLG = 'True'
WHERE ORDNO = @OrdNo";

                using (var additionalCommand =
                    new SqlCommand(
                        additionalSql,
                        connection,
                        transaction))
                {
                    additionalCommand.Parameters.AddWithValue("@OrdNo", ordNo);

                    await additionalCommand.ExecuteNonQueryAsync();
                }

                // =========================================================
                // LEGACY STKDT CANCEL / CLOSE
                //
                // Purchase Order:
                // VRTYPE = 'O'
                // TYPE   = 'R'
                //
                // Legacy StkDtDB.Close() sets CLOSES = true.
                // =========================================================
                const string stkDtSql = @"
                  UPDATE STKDT
                  SET CLOSES = 1
                  WHERE ORDNO = @OrdNo
                  AND VRTYPE = 'O'
                  AND TYPE = 'R'";

                using (var stkDtCommand =
                    new SqlCommand(
                        stkDtSql,
                        connection,
                        transaction))
                {
                    stkDtCommand.Parameters.AddWithValue("@OrdNo", ordNo);

                    await stkDtCommand.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<bool> DeletePurchaseOrderAsync(int ordNo)
        {
            var connection = db.GetConnection();

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            using var transaction =
                (SqlTransaction)await connection.BeginTransactionAsync();

            try
            {
                // =========================================================
                // CHECK PURCHASE ORDER EXISTS
                // =========================================================
                const string checkSql = @"
SELECT COUNT(1)
FROM PORDM
WHERE ORDNO = @OrdNo";

                using (var checkCommand =
                    new SqlCommand(checkSql, connection, transaction))
                {
                    checkCommand.Parameters.AddWithValue("@OrdNo", ordNo);

                    int count = Convert.ToInt32(
                        await checkCommand.ExecuteScalarAsync());

                    if (count == 0)
                    {
                        await transaction.RollbackAsync();
                        return false;
                    }
                }

                // =========================================================
                // DELETE STKDT
                //
                // Legacy StkDtDB.DeleteByVr()
                // Purchase Order:
                // VRTYPE = 'O'
                // ORDNO  = current PO
                // TYPE   = 'R'
                // =========================================================
                const string stkDtSql = @"
DELETE FROM STKDT
WHERE ORDNO = @OrdNo
  AND TYPE = 'R'
  AND VRTYPE = 'O'";

                using (var stkDtCommand =
                    new SqlCommand(
                        stkDtSql,
                        connection,
                        transaction))
                {
                    stkDtCommand.Parameters.AddWithValue("@OrdNo", ordNo);

                    await stkDtCommand.ExecuteNonQueryAsync();
                }

                // =========================================================
                // DELETE PURCHASE ORDER ITEMS
                // =========================================================
                const string itemSql = @"
DELETE FROM PORDT
WHERE ORDNO = @OrdNo";

                using (var itemCommand =
                    new SqlCommand(
                        itemSql,
                        connection,
                        transaction))
                {
                    itemCommand.Parameters.AddWithValue("@OrdNo", ordNo);

                    await itemCommand.ExecuteNonQueryAsync();
                }

                // =========================================================
                // DELETE ADDITIONAL DETAILS
                // =========================================================
                const string additionalSql = @"
DELETE FROM PORDD
WHERE ORDNO = @OrdNo";

                using (var additionalCommand =
                    new SqlCommand(
                        additionalSql,
                        connection,
                        transaction))
                {
                    additionalCommand.Parameters.AddWithValue("@OrdNo", ordNo);

                    await additionalCommand.ExecuteNonQueryAsync();
                }

                // =========================================================
                // DELETE PURCHASE ORDER HEADER
                // =========================================================
                const string headerSql = @"
                  DELETE FROM PORDM
                  WHERE ORDNO = @OrdNo";

                using (var headerCommand =
                    new SqlCommand(
                        headerSql,
                        connection,
                        transaction))
                {
                    headerCommand.Parameters.AddWithValue("@OrdNo", ordNo);

                    await headerCommand.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task InsertPurchaseOrderStockDetailsAsync(
    SqlConnection connection,
    SqlTransaction transaction,
    int ordNo,
    PurchaseOrderDTO purchaseOrder,
    int userId)
        {
            if (purchaseOrder.Items == null || purchaseOrder.Items.Count == 0)
                return;

            const string sql = @"
INSERT INTO STKDT
(
    VRTYPE,
    PRCODE,
    VRDATE,
    ORDNO,
    NTNO,
    TRNNO,
    QTY,
    TYPE,
    PARTYCD,
    LOGCD,
    UTIME,
    RATE,
    UNITCODE,
    CONVTYPE,
    CONVFACT,
    CONVQTY,
    PRFLG,
    CONVRATE,
    GDWCODE,
    LUTIME,
    VSVRTYPE,
    PRV,
    PRVN,
    PRVT,
    SNTSLNO,
    LM,
    ADDCOST,
    CONVADDCOST,
    CLOSES,
    ORDROWID,
    NTROWID,
    TRNROWID,
    QTNCODE,
    QTNROWID,
    PRVQ,
    PRVQY,
    PRVY,
    PRVNY,
    PRVTY,
    NTRNO,
    NTRROWID
)
VALUES
(
    @VrType,
    @PrCode,
    @VrDate,
    @OrdNo,
    @NtNo,
    @TrnNo,
    @Qty,
    @Type,
    @PartyCd,
    @LogCd,
    @UTime,
    @Rate,
    @UnitCode,
    @ConvType,
    @ConvFact,
    @ConvQty,
    @PrFlg,
    @ConvRate,
    @GdwCode,
    @LUTime,
    @VSVRTYPE,
    @PRV,
    @PRVN,
    @PRVT,
    @SNTSLNO,
    @LM,
    @AddCost,
    @ConvAddCost,
    @Closes,
    @OrdRowId,
    @NtRowId,
    @TrnRowId,
    @QtnCode,
    @QtnRowId,
    @PRVQ,
    @PRVQY,
    @PRVY,
    @PRVNY,
    @PRVTY,
    @NtrNo,
    @NtrRowId
);";

            foreach (var item in purchaseOrder.Items)
            {
                using var command = new SqlCommand(
                    sql,
                    connection,
                    transaction);

                // ---------------------------------------------------------
                // Legacy SaveStkDt()
                // ---------------------------------------------------------

                // VRTYPE = "O"
                command.Parameters.AddWithValue(
                    "@VrType",
                    "O");

                command.Parameters.AddWithValue(
                    "@PrCode",
                    item.PrCode);

                // VRDATE
                command.Parameters.AddWithValue(
                    "@VrDate",
                    purchaseOrder.OrdDate ?? DateTime.Now);

                // ORDNO
                command.Parameters.AddWithValue(
                    "@OrdNo",
                    ordNo);

                // Legacy:
                // NtNo = 0
                // TrnNo = 0
                command.Parameters.AddWithValue(
                    "@NtNo",
                    0);

                command.Parameters.AddWithValue(
                    "@TrnNo",
                    0);

                // QTY
                command.Parameters.AddWithValue(
                    "@Qty",
                    item.Qty);

                // TYPE = "R"
                command.Parameters.AddWithValue(
                    "@Type",
                    "R");

                // PARTYCD = supplier
                command.Parameters.AddWithValue(
                    "@PartyCd",
                    purchaseOrder.SupCode);

                // LOGCD = current user
                command.Parameters.AddWithValue(
                    "@LogCd",
                    userId);

                // ---------------------------------------------------------
                // UTIME
                //
                // For a newly-created Purchase Order there should normally
                // be no existing STKDT record, so this is DateTime.Now,
                // matching legacy fallback behavior.
                // ---------------------------------------------------------
                DateTime uTime = DateTime.Now;

                command.Parameters.AddWithValue(
                    "@UTime",
                    uTime);

                // ---------------------------------------------------------
                // RATE
                //
                // Legacy:
                //
                // Rate = dgv_col_price
                // Rate -= LDiscValue
                // Rate -= Discount
                //
                // Currency conversion is already represented in the DTO
                // values supplied by the Purchase Order API.
                // ---------------------------------------------------------
                decimal rate = item.Rate;

                if (item.LDiscAmt != 0)
                    rate -= item.LDiscAmt;

                if (item.Disc != 0)
                    rate -= item.Disc;

                command.Parameters.AddWithValue(
                    "@Rate",
                    rate);

                // UNITCODE
                command.Parameters.AddWithValue(
                    "@UnitCode",
                    item.UnitCode);

                // CONVTYPE
                command.Parameters.AddWithValue(
                    "@ConvType",
                    item.ConvType);

                // CONVFACT
                decimal convFact =
                    item.ConvFact == 0
                        ? 1
                        : item.ConvFact;

                command.Parameters.AddWithValue(
                    "@ConvFact",
                    convFact);

                // ---------------------------------------------------------
                // CONVQTY
                //
                // The exact alias/alternative-unit calculation is handled
                // when building PurchaseOrderItemDTO.
                //
                // If ConvQty is not supplied, legacy starts with Qty.
                // ---------------------------------------------------------
                decimal convQty =
                    item.ConvQty == 0
                        ? item.Qty
                        : item.ConvQty;

                command.Parameters.AddWithValue(
                    "@ConvQty",
                    convQty);

                // PRFLG
                command.Parameters.AddWithValue(
                    "@PrFlg",
                    item.PrFlg);

                // ---------------------------------------------------------
                // CONVRATE
                //
                // Legacy SaveStkDt uses the already calculated ConvRate.
                // ---------------------------------------------------------
                command.Parameters.AddWithValue(
                    "@ConvRate",
                    item.ConvRate);

                // ---------------------------------------------------------
                // GDWCODE
                //
                // Legacy:
                //
                // if dgv_col_gdw has a value:
                //     use that value
                // else:
                //     Functions.GetGodownByBranch()
                //
                // The DTO currently carries GdwCode. We do not invent a
                // branch/godown lookup here.
                // ---------------------------------------------------------
                int gdwCode = item.GdwCode;

                command.Parameters.AddWithValue(
                    "@GdwCode",
                    gdwCode);

                // LUTIME
                command.Parameters.AddWithValue(
                    "@LUTime",
                    DateTime.Now);

                // VSVRTYPE
                command.Parameters.AddWithValue(
                    "@VSVRTYPE",
                    purchaseOrder.VSVRTYPE);

                // Legacy:
                // PRV = PRVN = PRVT = "N"
                command.Parameters.AddWithValue(
                    "@PRV",
                    "N");

                command.Parameters.AddWithValue(
                    "@PRVN",
                    "N");

                command.Parameters.AddWithValue(
                    "@PRVT",
                    "N");

                // Other legacy defaults
                command.Parameters.AddWithValue(
                    "@SNTSLNO",
                    0);

                command.Parameters.AddWithValue(
                    "@LM",
                    "N");

                command.Parameters.AddWithValue(
                    "@AddCost",
                    0);

                command.Parameters.AddWithValue(
                    "@ConvAddCost",
                    0);

                // New PO stock detail is open
                command.Parameters.AddWithValue(
                    "@Closes",
                    false);

                // Original Purchase Order row ID
                command.Parameters.AddWithValue(
                    "@OrdRowId",
                    item.OrdRowId);

                // Remaining legacy defaults
                command.Parameters.AddWithValue(
                    "@NtRowId",
                    0);

                command.Parameters.AddWithValue(
                    "@TrnRowId",
                    0);

                command.Parameters.AddWithValue(
                    "@QtnCode",
                    0);

                command.Parameters.AddWithValue(
                    "@QtnRowId",
                    0);

                command.Parameters.AddWithValue(
                    "@PRVQ",
                    "N");

                command.Parameters.AddWithValue(
                    "@PRVQY",
                    "");

                command.Parameters.AddWithValue(
                    "@PRVY",
                    "");

                command.Parameters.AddWithValue(
                    "@PRVNY",
                    "");

                command.Parameters.AddWithValue(
                    "@PRVTY",
                    "");

                command.Parameters.AddWithValue(
                    "@NtrNo",
                    0);

                command.Parameters.AddWithValue(
                    "@NtrRowId",
                    0);

                await command.ExecuteNonQueryAsync();
            }
        }
        private async Task<int> GetNextPurchaseOrderNumberAsync(
     SqlConnection connection,
     SqlTransaction transaction)
        {
            // ============================================================
            // Legacy equivalent:
            //
            // FyDetailsDB.ValidateVoucherNo("PORD", "GET", 0)
            //
            // Legacy SQL:
            //
            // DECLARE @Col int;
            // UPDATE FYDETAILS
            // SET @Col = PORDNO,
            //     PORDNO = CASE
            //                 WHEN PORDNO >= 0 THEN -1
            //                 ELSE PORDNO
            //              END;
            // SELECT @Col AS Col;
            // ============================================================

            const string getVoucherSql = @"
DECLARE @Col INT;

UPDATE FYDETAILS
SET
    @Col = PORDNO,
    PORDNO = CASE
                WHEN PORDNO >= 0 THEN -1
                ELSE PORDNO
             END;

SELECT @Col AS Col;";

            int oldVoucherNo;

            using (var command = new SqlCommand(
                getVoucherSql,
                connection,
                transaction))
            {
                command.CommandTimeout = 30;

                object? result = await command.ExecuteScalarAsync();

                if (result == null || result == DBNull.Value)
                {
                    throw new Exception(
                        "FYDETAILS.PORDNO could not be read.");
                }

                oldVoucherNo = Convert.ToInt32(result);
            }

            // ============================================================
            // Legacy behavior:
            // VoucherNo = OldVoucherNo + 1
            // ============================================================

            int voucherNo = oldVoucherNo + 1;

            // ============================================================
            // If FYDETAILS contains -1 because a previous request reserved
            // the number, use MAX(PORDM.ORDNO) + 1.
            // ============================================================

            if (voucherNo <= 0)
            {
                const string maxSql = @"
SELECT ISNULL(MAX(ORDNO), 0)
FROM PORDM;";

                using var maxCommand = new SqlCommand(
                    maxSql,
                    connection,
                    transaction);

                maxCommand.CommandTimeout = 30;

                int maxVoucherNo = Convert.ToInt32(
                    await maxCommand.ExecuteScalarAsync());

                voucherNo = maxVoucherNo + 1;
            }

            // ============================================================
            // Legacy:
            // ChckVrNoExists("PORD", voucherNo)
            // ============================================================

            const string checkSql = @"
SELECT COUNT(*)
FROM PORDM
WHERE ORDNO = @OrdNo;";

            int exists;

            using (var checkCommand = new SqlCommand(
                checkSql,
                connection,
                transaction))
            {
                checkCommand.CommandTimeout = 30;

                checkCommand.Parameters.Add(
                    "@OrdNo",
                    SqlDbType.Int).Value = voucherNo;

                exists = Convert.ToInt32(
                    await checkCommand.ExecuteScalarAsync());
            }

            // ============================================================
            // If generated number already exists:
            //
            // SELECT MAX(ORDNO) FROM PORDM
            // MaxVoucherNo++;
            // VoucherNo = MaxVoucherNo;
            // ============================================================

            if (exists > 0)
            {
                const string maxSql = @"
SELECT ISNULL(MAX(ORDNO), 0)
FROM PORDM;";

                using var maxCommand = new SqlCommand(
                    maxSql,
                    connection,
                    transaction);

                maxCommand.CommandTimeout = 30;

                int maxVoucherNo = Convert.ToInt32(
                    await maxCommand.ExecuteScalarAsync());

                voucherNo = maxVoucherNo + 1;
            }

            if (voucherNo <= 0)
            {
                throw new Exception(
                    "Unable to generate a valid Purchase Order number. " +
                    $"FYDETAILS.PORDNO = {oldVoucherNo}, " +
                    $"Generated ORDNO = {voucherNo}.");
            }

            return voucherNo;
        }
    }
}