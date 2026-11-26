
using System;
using System.Data;
using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs.Purchase;
using QuickBalance.API.Interfaces.Purchase;

namespace QuickBalance.API.Repository.Purchase
{
    public class PurchaseOrderRepository : IPurchaseOrderRepository
    {
        private readonly DBConn _db;

        public PurchaseOrderRepository(DBConn db)
        {
            _db = db;
        }

        public async Task<int> CreatePurchaseOrderAsync(
            PurchaseOrderCreateRequest request,
            int userId)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Master == null)
                throw new ArgumentException(
                    "Purchase order master details are required.");

            if (request.Items == null || request.Items.Count == 0)
                throw new ArgumentException(
                    "Purchase order must contain at least one item.");

            if (userId <= 0)
                throw new ArgumentException("Invalid user.");

            var master = request.Master;
            var now = DateTime.Now;

            await using var connection =
                new SqlConnection(_db.ConnectionString);

            await connection.OpenAsync();

            // =========================================
            // 1. CHECK CONNECTED DATABASE
            // =========================================

            var dbInfo = await connection.QuerySingleAsync<dynamic>(
                @"SELECT
                    @@SERVERNAME AS ServerName,
                    DB_NAME() AS DatabaseName");

            //Console.WriteLine("========== PURCHASE ORDER DB INFO ==========");
            //Console.WriteLine($"SERVER: {dbInfo.ServerName}");
            //Console.WriteLine($"DATABASE: {dbInfo.DatabaseName}");
            //Console.WriteLine("=============================================");

            await using var transaction =
                await connection.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                // =========================================
                // 2. GENERATE PURCHASE ORDER NUMBER
                // =========================================

                const string orderNumberSql = @"
                    SELECT ISNULL(MAX(ORDNO), 0) + 1
                    FROM PORDM WITH (UPDLOCK, HOLDLOCK)
                    WHERE VRTYPE = @VrType;";

                const string vrType = "O";

                int orderNo = await connection.QuerySingleAsync<int>(
                    orderNumberSql,
                    new { VrType = vrType },
                    transaction,
                    commandTimeout: 60);

                Console.WriteLine($"GENERATED ORDER NO: {orderNo}");

                // =========================================
                // 3. VALIDATE AND CALCULATE TOTAL
                // =========================================

                decimal subTotal = 0;

                foreach (var item in request.Items)
                {
                    if (item.ProductId <= 0)
                        throw new ArgumentException(
                            "Invalid product in purchase order.");

                    if (item.Quantity <= 0)
                        throw new ArgumentException(
                            "Item quantity must be greater than zero.");

                    if (item.UnitId <= 0)
                        throw new ArgumentException(
                            "Invalid unit in purchase order.");

                    if (item.Rate < 0)
                        throw new ArgumentException(
                            "Item rate cannot be negative.");

                    if (item.Discount < 0)
                        throw new ArgumentException(
                            "Item discount cannot be negative.");

                    decimal gross = item.Quantity * item.Rate;
                    decimal lineAmount = gross - item.Discount;

                    if (lineAmount < 0)
                        throw new ArgumentException(
                            "Item discount cannot exceed the item amount.");

                    subTotal += lineAmount;
                }

                if (master.DiscountValue < 0)
                    throw new ArgumentException(
                        "Order discount cannot be negative.");

                if (master.Freight < 0)
                    throw new ArgumentException(
                        "Freight cannot be negative.");

                decimal netAmount =
                    subTotal - master.DiscountValue + master.Freight;

                if (netAmount < 0)
                    throw new ArgumentException(
                        "Purchase order total cannot be negative.");

                // =========================================
                // 4. INSERT MASTER - PORDM
                // =========================================

                const string masterSql = @"
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
                    OUTPUT INSERTED.ID
                    VALUES
                    (
                        @VrType,
                        @OrderNo,
                        @OrderDate,
                        @SupplierId,
                        @DiscountPercentage,
                        @DiscountValue,
                        0,
                        0,
                        0,
                        0,
                        0,
                        @Amount,
                        @UserId,
                        @Remarks,
                        @Freight,
                        @CreatedDate,
                        @InvoiceFormatId,
                        @InvoiceNumber,
                        @InvoiceDate,
                        @CurrencyConversionRate,
                        @CurrencyId,
                        NULL,
                        NULL,
                        0,
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        0,
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        @UserId,
                        @CreatedDate,
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        0,
                        @UserId,
                        @CreatedDate
                    );";

                int orderId = await connection.QuerySingleAsync<int>(
                    masterSql,
                    new
                    {
                        VrType = vrType,
                        OrderNo = orderNo,
                        master.OrderDate,
                        master.SupplierId,
                        master.DiscountPercentage,
                        master.DiscountValue,
                        Amount = netAmount,
                        UserId = userId,
                        master.Remarks,
                        master.Freight,
                        CreatedDate = now,
                        master.InvoiceFormatId,
                        master.InvoiceNumber,
                        master.InvoiceDate,
                        CurrencyConversionRate =
                            master.CurrencyConversionRate > 0
                                ? master.CurrencyConversionRate
                                : 1,
                        master.CurrencyId
                    },
                    transaction,
                    commandTimeout: 60);

                Console.WriteLine($"INSERTED PORDM ID: {orderId}");

                // =========================================
                // 5. VERIFY PORDM INSERT
                // =========================================

                const string verifyMasterSql = @"
                    SELECT
                        ID,
                        VRTYPE,
                        ORDNO,
                        ORDDATE,
                        SUPCODE,
                        AMOUNT
                    FROM PORDM
                    WHERE ID = @OrderId
                      AND VRTYPE = @VrType
                      AND ORDNO = @OrderNo;";

                var insertedMaster =
                    await connection.QueryFirstOrDefaultAsync<dynamic>(
                        verifyMasterSql,
                        new
                        {
                            OrderId = orderId,
                            VrType = vrType,
                            OrderNo = orderNo
                        },
                        transaction,
                        commandTimeout: 60);

                if (insertedMaster == null)
                {
                    throw new Exception(
                        "Purchase order master insert verification failed.");
                }

                Console.WriteLine("PORDM INSERT VERIFIED:");
                Console.WriteLine(
                    JsonSerializer.Serialize(insertedMaster));

                // =========================================
                // 6. INSERT ITEMS - PORDT
                // =========================================

                const string itemSql = @"
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
                        @OrderNo,
                        @OrderDate,
                        @SupplierId,
                        @ProductId,
                        @Quantity,
                        @UnitId,
                        @Rate,
                        0,
                        @UserId,
                        @TaxCode,
                        @Discount,
                        0,
                        @CreatedDate,
                        @GodownId,
                        @CurrencyConversionRate,
                        @CurrencyId,
                        @OriginalQuantity,
                        @OriginalRate,
                        0,
                        0,
                        0,
                        @ConversionFactor,
                        @ConversionQuantity,
                        1,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        NULL
                    );";

                int insertedItemCount = 0;

                foreach (var item in request.Items)
                {
                    int affectedRows =
                        await connection.ExecuteAsync(
                            itemSql,
                            new
                            {
                                OrderNo = orderNo,
                                master.OrderDate,
                                master.SupplierId,

                                item.ProductId,
                                item.Quantity,
                                item.UnitId,
                                item.Rate,

                                UserId = userId,
                                item.TaxCode,
                                item.Discount,

                                CreatedDate = now,

                                GodownId = item.GodownId,

                                CurrencyConversionRate =
                                    master.CurrencyConversionRate > 0
                                        ? master.CurrencyConversionRate
                                        : 1,

                                master.CurrencyId,

                                OriginalQuantity = item.Quantity,
                                OriginalRate = item.Rate,

                                ConversionFactor =
                                    item.ConversionFactor > 0
                                        ? item.ConversionFactor
                                        : 1,

                                item.ConversionQuantity
                            },
                            transaction,
                            commandTimeout: 60);

                    insertedItemCount += affectedRows;
                }

                if (insertedItemCount != request.Items.Count)
                {
                    throw new Exception(
                        $"Purchase order item insert mismatch. " +
                        $"Expected: {request.Items.Count}, " +
                        $"Inserted: {insertedItemCount}");
                }

                // =========================================
                // 7. VERIFY PORDT INSERT
                // =========================================

                const string verifyItemsSql = @"
                    SELECT COUNT(*)
                    FROM PORDT
                    WHERE ORDNO = @OrderNo
                      AND SUPCODE = @SupplierId;";

                int savedItemCount =
                    await connection.QuerySingleAsync<int>(
                        verifyItemsSql,
                        new
                        {
                            OrderNo = orderNo,
                            master.SupplierId
                        },
                        transaction,
                        commandTimeout: 60);

                if (savedItemCount != request.Items.Count)
                {
                    throw new Exception(
                        $"PORDT verification failed. " +
                        $"Expected: {request.Items.Count}, " +
                        $"Found: {savedItemCount}");
                }

                // =========================================
                // 8. COMMIT TRANSACTION
                // =========================================

                await transaction.CommitAsync();

                Console.WriteLine("========== PURCHASE ORDER SAVED ==========");
                Console.WriteLine($"PORDM ID: {orderId}");
                Console.WriteLine($"ORDER NO: {orderNo}");
                Console.WriteLine($"ITEM COUNT: {savedItemCount}");
                Console.WriteLine($"AMOUNT: {netAmount}");
                Console.WriteLine("===========================================");

                return orderId;
            }
            catch (Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync();
                }
                catch (Exception rollbackEx)
                {
                    Console.WriteLine(
                        $"ROLLBACK ERROR: {rollbackEx.Message}");
                }

                Console.WriteLine(
                    $"PURCHASE ORDER ERROR: {ex}");

                throw;
            }
        }

        public async Task<PurchaseOrderListResult> GetAllAsync(
            int page,
            int pageSize,
            string? search)
        {
            using var connection = new SqlConnection(_db.ConnectionString);

            await connection.OpenAsync();

            int offset = (page - 1) * pageSize;

            string sql = @"
                SELECT COUNT(*)
                FROM PORDM
                WHERE VRTYPE = 'O'
                  AND (
                        @Search IS NULL
                        OR CAST(ORDNO AS VARCHAR(50)) LIKE '%' + @Search + '%'
                        OR CAST(SUPCODE AS VARCHAR(50)) LIKE '%' + @Search + '%'
                        OR REMARKS LIKE '%' + @Search + '%'
                      );

                SELECT
                    ID AS Id,
                    ORDNO AS OrderNo,
                    VRTYPE AS VrType,
                    SUPCODE AS SupplierCode,
                    ORDDATE AS OrderDate,
                    AMOUNT AS Amount,
                    REMARKS AS Remarks
                FROM PORDM
                WHERE VRTYPE = 'O'
                  AND (
                        @Search IS NULL
                        OR CAST(ORDNO AS VARCHAR(50)) LIKE '%' + @Search + '%'
                        OR CAST(SUPCODE AS VARCHAR(50)) LIKE '%' + @Search + '%'
                        OR REMARKS LIKE '%' + @Search + '%'
                      )
                ORDER BY ORDDATE DESC, ID DESC
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY;
            ";

            using var multi = await connection.QueryMultipleAsync(
                sql,
                new
                {
                    Search = string.IsNullOrWhiteSpace(search)
                        ? null
                        : search.Trim(),
                    Offset = offset,
                    PageSize = pageSize
                },
                commandTimeout: 60);

            int totalRecords = await multi.ReadFirstAsync<int>();

            var items = (await multi.ReadAsync<PurchaseOrderListResponse>())
                .ToList();

            return new PurchaseOrderListResult
            {
                Items = items,
                TotalRecords = totalRecords,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(
                    totalRecords / (double)pageSize)
            };
        }
        public async Task<PurchaseOrderDetailResult?> GetPurchaseOrderByIdAsync(int id)
        {
            using var connection = new SqlConnection(_db.ConnectionString);

            await connection.OpenAsync();

            string sql = @"
        SELECT
            ID AS Id,
            ORDNO AS OrderNo,
            VRTYPE AS VrType,
            ORDDATE AS OrderDate,
            SUPCODE AS SupplierCode,
            DISCP AS DiscountPercentage,
            DISCVAL AS DiscountValue,
            TAX AS Tax,
            NTVALUE AS NetValue,
            TVALUE AS TotalValue,
            AMOUNT AS Amount,
            FREIGHT AS Freight,
            REMARKS AS Remarks
        FROM PORDM
        WHERE ID = @Id
          AND VRTYPE = 'O';

        SELECT *
        FROM PORDT
        WHERE ORDNO = (
            SELECT ORDNO
            FROM PORDM
            WHERE ID = @Id
              AND VRTYPE = 'O'
        );
    ";

            using var multi = await connection.QueryMultipleAsync(
                sql,
                new { Id = id },
                commandTimeout: 60);

            var result = await multi.ReadFirstOrDefaultAsync<PurchaseOrderDetailResult>();

            if (result == null)
                return null;

            var items = await multi.ReadAsync();

            result.Items = items.Cast<dynamic>().ToList();

            return result;
        }
        public async Task<bool> DeletePurchaseOrderAsync(int id)
        {
            using var connection = new SqlConnection(_db.ConnectionString);

            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction(
                IsolationLevel.Serializable);

            try
            {
                // Get purchase order number using master ID.
                int? orderNo = await connection.QueryFirstOrDefaultAsync<int?>(
                    @"
                        SELECT ORDNO
                        FROM PORDM WITH (UPDLOCK, HOLDLOCK)
                        WHERE ID = @Id
                          AND VRTYPE = 'O'
                          AND ISNULL(CANCELFLG, 0) = 0;
                        ",
                    new { Id = id },
                    transaction,
                    commandTimeout: 60);

                // Not found or already cancelled.
                if (orderNo == null)
                {
                    transaction.Rollback();
                    return false;
                }

                // Cancel purchase order master.
                await connection.ExecuteAsync(
                    @"
                        UPDATE PORDM
                        SET CANCELFLG = 1
                        WHERE ID = @Id
                          AND VRTYPE = 'O';
                        ",
                    new { Id = id },
                    transaction,
                    commandTimeout: 60);

                // Cancel purchase order items.
                await connection.ExecuteAsync(
                            @"
                    UPDATE PORDT
                    SET CANCELFLG = 1
                    WHERE ORDNO = @OrderNo;
                    ",
                    new { OrderNo = orderNo.Value },
                    transaction,
                    commandTimeout: 60);

                // Cancel additional purchase details.
                await connection.ExecuteAsync(
                    @"
                        UPDATE PORDD
                        SET CANCELFLG = 1
                        WHERE ORDNO = @OrderNo;
                        ",
                    new { OrderNo = orderNo.Value },
                    transaction,
                    commandTimeout: 60);

                transaction.Commit();

                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
