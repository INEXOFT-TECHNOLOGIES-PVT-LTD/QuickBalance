# Read / cancel input files

| # | Item | Path | Line range |
|---|------|------|------------|
| 1 | GetInvoiceAsync (no private helpers) | QuickBalance.API/DB/SalMDB.cs | 107-262 |
| 2 | SalesInvoiceResponseDto and nested classes (whole file) | QuickBalance.API/DTOs/Respons-DTO/SalesInvoiceResponseDto.cs | 1-111 |
| 3a | List | QuickBalance.API/DB/SalMDB.cs | 21-92 |
| 3b | helper VSDOCNO | QuickBalance.API/DB/Function.cs | 108-111 |
| 3c | helper LIST_ALLOW_VR_STMNT | QuickBalance.API/DB/Function.cs | 132-135 |
| 3d | helper GetDatatable(string qry) | QuickBalance.API/DB/DBConn.cs | 532-552 |
| 4 | PaginationRequest.cs (whole file) | QuickBalance.API/DTOs/PaginationRequest.cs | 1-86 |
| 5a | BrandDB.GetAllAsync | QuickBalance.API/Repository/Master/BrandDB.cs | 69-127 |
| 5b | helper SelectColumns | QuickBalance.API/Repository/Master/BrandDB.cs | 20-25 |
| 6a | QueryListAsync signature | QuickBalance.API/DB/DBConn.cs | 858 |
| 6b | QueryFirstOrDefaultAsync signature | QuickBalance.API/DB/DBConn.cs | 872 |
| 6c | QueryMultipleAsync signature | QuickBalance.API/DB/DBConn.cs | 908-910 |

## QuickBalance.API/DB/SalMDB.cs (lines 107-262)

````csharp
        public async Task<SalesInvoiceResponseDto?> GetInvoiceAsync(int invoiceNo)
        {
            const string sql = @"
            DECLARE @OrderNo INT;

            SELECT @OrderNo = ORDNO
            FROM SALM
            WHERE INVNO = @InvoiceNo;

            -- 1. Header: preserve original joins.
            SELECT
                SALM.INVNO AS InvoiceNo,
                SALM.INVDATE AS InvoiceDate,
                SALM.STYPE AS InvoiceType,
                SALM.BTYPE AS BillType,
                SALM.CUSTCODE AS CustomerCode,
                ACCMAS.ACCNAME AS CustomerName,
                SALM.SMCODE AS SalesmanCode,
                SMMAST.SMNAME AS SalesmanName,
                SALM.ORDNO AS OrderNo,
                SALM.DISCP AS DiscountPercentage,
                SALM.DISCVAL AS DiscountAmount,
                SALM.NTVALUE AS NetValue,
                SALM.TVALUE AS TaxableValue,
                SALM.TAX AS TaxAmount,
                SALM.AMOUNT AS Amount,
                SALM.FREIGHT AS Freight,
                SALM.COINADJ AS RoundOff,
                SALM.GDNNO AS GodownNo,
                SALM.PAYMODE AS PaymentMode,
                SALM.CASHPAID AS CashPaid,
                SALM.BANKPAID AS BankPaid,
                SALM.DUEDATE AS DueDate,
                SALM.REMARKS AS Remarks,
                SALM.CANCELFLG AS Cancelled,
                SALM.RETURNFLG AS Returned,
                DTUSER.USERNAME AS UserName
            FROM (((ACCMAS
                INNER JOIN SALM
                    ON ACCMAS.ID = SALM.CUSTCODE)
                LEFT JOIN SMMAST
                    ON SALM.SMCODE = SMMAST.SMCODE)
                LEFT JOIN (
                    SELECT USERCODE, FULLNAME AS USERNAME
                    FROM TBUSER
                ) AS DTUSER
                    ON SALM.LOGCD = DTUSER.USERCODE)
            WHERE SALM.INVNO = @InvoiceNo;

            -- 2. Items: preserve original joins.
            SELECT
                PRMAST.PRNAME AS ProductName,
                PRMAST.ALIAS AS Alias,
                PRMAST.SECNAME AS SecondaryName,
                PRMAST.BMEQN AS Bmeqn,
                PRUNITMAS.PRUNITNAME AS UnitName,
                PRUNITMAS.SECNAME AS UnitSecondaryName,
                PRMAST.UNIT AS Unit,
                PRMAST.ALTUNIT AS AltUnit,
                SALT.PRCODE AS ProductCode,
                SALT.UNITCODE AS UnitCode,
                SALT.QTY AS Quantity,
                SALT.RATE AS Rate,
                SALT.PRRATE AS ProductRate,
                SALT.LDISCPERC AS DiscountPercentage,
                SALT.LDISCAMT AS DiscountAmount,
                SALT.TOTDISC AS TotalDiscount,
                SALT.TAXCODE AS TaxCode,
                SALT.TAX AS TaxPercentage,
                SALT.BATCH AS Batch,
                SALT.EXPIRY AS ExpiryDate,
                SALT.MFGDATE AS ManufacturingDate,
                SALT.GDWCODE AS GodownCode,
                SALT.CONVFACT AS ConversionFactor,
                SALT.CONVQTY AS ConversionQuantity,
                SALT.CONVRATE AS ConversionRate,
                SALT.MRP AS Mrp,
                SALT.ALCODE AS AliasCode,
                SALT.ALNAME AS AliasName,
                SALT.PRDESC AS ProductDescription,
                SALT.CANCELFLG AS Cancelled,
                SALT.RETURNFLG AS Returned,
                SALT.SNTSLNO AS SerialNumberCount,
                SALT.ORDROWID AS OrderRowId,
                SALT.TRNROWID AS TransactionRowId
            FROM (PRMAST
                INNER JOIN SALT
                    ON PRMAST.PRCODE = SALT.PRCODE)
                LEFT JOIN PRUNITMAS
                    ON SALT.UNITCODE = PRUNITMAS.PRUNITID
            WHERE SALT.INVNO = @InvoiceNo;

            -- 3. Serial numbers: preserve original filters.
            SELECT
                INVRTYPE AS InvoiceReturnType,
                INVRNO AS InvoiceReturnNo,
                INORDNO AS InvoiceOrderNo,
                INSLNO AS InvoiceSerialNo,
                PRCODE AS ProductCode,
                OUTSLNO AS OutSerialNo,
                GDWCODE AS GodownCode,
                UNITCODE AS UnitCode,
                ITEMSLNO AS ItemSerialNo
            FROM SNTSTK
            WHERE OUTVRTYPE = @OutVrType
              AND OUTVRNO = @InvoiceNo
              AND OUTORDNO = @OrderNo;

            -- 4. Additional information: preserve original filters.
            SELECT
                CODE AS Code,
                TAX AS Tax,
                TAXAMT AS TaxAmount,
                SLCODE AS SalesLedgerCode
            FROM SALD
            WHERE VRNO = @InvoiceNo
              AND VRTYPE = @VrType
              AND DFLAG = @DFlag
              AND CODE <> '0'
              AND ETYPE <> 'D';
        ";



            using var results = await db.QueryMultipleAsync(sql,
            new
            {
                InvoiceNo = invoiceNo,
                OutVrType = "SI",
                VrType = "O",
                DFlag = false
            });

            var header =
                await results.ReadFirstOrDefaultAsync<SalesInvoiceHeaderDto>();

            if (header == null)
                return null;

            var items =
                (await results.ReadAsync<SalesInvoiceItemDto>()).ToList();

            var serialNumbers =
                (await results.ReadAsync<SalesInvoiceSerialDto>()).ToList();

            var additionalInformations =
                (await results.ReadAsync<SalesInvoiceAdditionalDto>()).ToList();

            return new SalesInvoiceResponseDto
            {
                Header = header,
                Items = items,
                SerialNumbers = serialNumbers,
                AdditionalInformations = additionalInformations
            };
        }
````

## QuickBalance.API/DTOs/Respons-DTO/SalesInvoiceResponseDto.cs (lines 1-111)

````csharp
namespace QuickBalance.API.DTOs.Respons_DTO
{
    public sealed class SalesInvoiceResponseDto
    {
        public SalesInvoiceHeaderDto Header { get; set; } = new();
        public List<SalesInvoiceItemDto> Items { get; set; } = new();
        public List<SalesInvoiceSerialDto> SerialNumbers { get; set; } = new();
        public List<SalesInvoiceAdditionalDto> AdditionalInformations { get; set; } = new();
    }

    public sealed class SalesInvoiceHeaderDto
    {
        public int InvoiceNo { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string? InvoiceType { get; set; }
        public string? BillType { get; set; }

        public int CustomerCode { get; set; }
        public string? CustomerName { get; set; }
        public int SalesmanCode { get; set; }
        public string? SalesmanName { get; set; }
        public int OrderNo { get; set; }

        public decimal DiscountPercentage { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetValue { get; set; }
        public decimal TaxableValue { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Amount { get; set; }
        public decimal Freight { get; set; }
        public decimal RoundOff { get; set; }

        public int GodownNo { get; set; }
        public int PaymentMode { get; set; }
        public decimal CashPaid { get; set; }
        public decimal BankPaid { get; set; }

        public DateTime? DueDate { get; set; }
        public string? Remarks { get; set; }
        public string? UserName { get; set; }

        public bool Cancelled { get; set; }
        public bool Returned { get; set; }
    }

    public sealed class SalesInvoiceItemDto
    {
        public int ProductCode { get; set; }
        public string? ProductName { get; set; }
        public string? Alias { get; set; }
        public string? SecondaryName { get; set; }
        public string? Bmeqn { get; set; }

        public string? UnitName { get; set; }
        public string? UnitSecondaryName { get; set; }
        public int Unit { get; set; }
        public int AltUnit { get; set; }
        public int UnitCode { get; set; }

        public decimal Quantity { get; set; }
        public decimal Rate { get; set; }
        public decimal ProductRate { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalDiscount { get; set; }

        public int TaxCode { get; set; }
        public decimal TaxPercentage { get; set; }

        public string? Batch { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime? ManufacturingDate { get; set; }

        public int GodownCode { get; set; }
        public decimal ConversionFactor { get; set; }
        public decimal ConversionQuantity { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal Mrp { get; set; }

        public string? AliasCode { get; set; }
        public string? AliasName { get; set; }
        public string? ProductDescription { get; set; }
        public bool Cancelled { get; set; }
        public bool Returned { get; set; }

        public int SerialNumberCount { get; set; }
        public int OrderRowId { get; set; }
        public int TransactionRowId { get; set; }
    }

    public sealed class SalesInvoiceSerialDto
    {
        public string? InvoiceReturnType { get; set; }
        public int? InvoiceReturnNo { get; set; }
        public int? InvoiceOrderNo { get; set; }
        public int? InvoiceSerialNo { get; set; }
        public int? ProductCode { get; set; }
        public int? OutSerialNo { get; set; }
        public int? GodownCode { get; set; }
        public int? UnitCode { get; set; }
        public int? ItemSerialNo { get; set; }
    }

    public sealed class SalesInvoiceAdditionalDto
    {
        public int Code { get; set; }
        public decimal Tax { get; set; }
        public decimal TaxAmount { get; set; }
        public int SalesLedgerCode { get; set; }
    }
}
````

## QuickBalance.API/DB/SalMDB.cs (lines 21-92)

````csharp
        public async Task<DataTable> List(ClsSalM clsSalM)
        {
            try
            {
                int page = Variables.pgRequest.Page;
                int pageSize = Variables.pgRequest.PageSize;

                string? search = Variables.pgRequest.Search;
                DateTime? fromDate = Variables.pgRequest.FromDate;
                DateTime? toDate = Variables.pgRequest.ToDate;

                string orderBy = "SALM.INVNO";
                switch (Variables.pgRequest.SortBy?.ToLower())
                {
                    case "invno":
                    case "invoiceno":
                        orderBy = "SALM.INVNO";
                        break;

                    case "date":
                    case "invdate":
                        orderBy = "SALM.INVDATE";
                        break;

                    case "customer":
                        orderBy = "ACCMAS.ACCNAME";
                        break;

                    case "amount":
                        orderBy = "SALM.AMOUNT";
                        break;

                    case "taxable":
                        orderBy = "SALM.TVALUE";
                        break;

                    case "tax":
                        orderBy = "SALM.TAX";
                        break;
                }
                string sortDirection = Variables.pgRequest.SortDirection == "ASC" ? "ASC" : "DESC";
                orderBy += $" {sortDirection}";

                int offset = (page - 1) * pageSize;

                string searchCondition = "";
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.Replace("'", "''");
                    searchCondition = $" AND (SALM.{Functions.VSDOCNO()} LIKE '%{search}%' OR ACCMAS.ACCNAME LIKE '%{search}%')";
                }

                string dateCondition = "";
                if (fromDate.HasValue)
                    dateCondition += $" AND SALM.INVDATE >= '{fromDate.Value.ToString(Variables.format)}'";
                if (toDate.HasValue)
                    dateCondition += $" AND SALM.INVDATE < '{toDate.Value.AddDays(1).ToString(Variables.format)}'";

                qry = $@"SELECT SALM.INVNO AS [Invoice_Id], SALM.{Functions.VSDOCNO()} AS [Inv_No], FORMAT(SALM.INVDATE, '{Variables.format}') AS [Date], ACCMAS.ACCNAME AS Customer {(clsSalM.SType == "O" ? $",FORMAT(SALM.TVALUE,'{Variables.DecString}') AS Taxable, FORMAT(SALM.TAX, '{Variables.DecString}') AS Tax" : "")}, FORMAT(SALM.AMOUNT,'{Variables.DecString}') AS Amount, COUNT(*) OVER() AS TotalRecords
FROM SALM INNER JOIN ACCMAS ON SALM.CUSTCODE = ACCMAS.ID
WHERE SALM.STYPE = '{clsSalM.SType}'{Functions.LIST_ALLOW_VR_STMNT(true, "SALM")}{searchCondition}{dateCondition}
ORDER BY {orderBy}
OFFSET {offset} ROWS
FETCH NEXT {pageSize} ROWS ONLY";

                return await db.GetDatatable(qry);
            }
            catch (Exception)
            {
                throw;
            }
        }
````

## QuickBalance.API/DB/Function.cs (lines 108-111)

````csharp
        public string VSDOCNO() // IF VOUCHER SYSTEM IS ACTIVATED THEN RETURN NEW INVOICE NUMBER FIELD(VSDOCNO) INSTEAD OF OLD INVOICE NUMBER FIELD(INVNOTEXT).
        {
            return Variables.VRSYS ? "VSDOCNO" : "INVNOTEXT";
        }
````

## QuickBalance.API/DB/Function.cs (lines 132-135)

````csharp
        public string LIST_ALLOW_VR_STMNT(bool? AND, string Table) // FILTER VOUCHER MAIN TABLE BY USER BRANCH
        {
            return Variables.VRSYS && Variables.BranchYN ? ((AND == null ? "" : Convert.ToBoolean(AND) ? " AND" : " WHERE") + " IIF(" + Table + ".VSVRTYPE IS NULL, 0, " + Table + ".VSVRTYPE) IN (SELECT VRSYSTEM.ID FROM VRSYSTEM WHERE IIF(VRSYSTEM.BRCD IS NULL, 0, VRSYSTEM.BRCD) = " + (Variables.VRSYS_DEFBRANCH == 0 ? "-1" : Variables.VRSYS_DEFBRANCH.ToString()) + ") ") : "";
        }
````

## QuickBalance.API/DB/DBConn.cs (lines 532-552)

````csharp
        public async Task<DataTable> GetDatatable(string qry)
        {
            var dt = new DataTable();

            await using var connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync();

            using var command = new SqlCommand(qry, connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 180
            };

            using var reader = await command.ExecuteReaderAsync();

            dt.Load(reader);

            return dt;
        }
````

## QuickBalance.API/DTOs/PaginationRequest.cs (lines 1-86)

````csharp
namespace QuickBalance.API.DTOs
{
        public class PaginationRequest
        {
            public string? Type { get; set; } = string.Empty;

            public int CustomerId { get; set; }

            public int OrderId { get; set; }

            public int Page { get; set; } = 1;

            public int PageSize { get; set; } = 20;

            public string? Search { get; set; }

            public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public string? SortBy { get; set; }

        public string? SortDirection { get; set; } = "DESC";

        public void Normalize()
        {
            if (Page < 1)
                Page = 1;

            if (PageSize < 1)
                PageSize = 20;

            if (PageSize > 100)
                PageSize = 100;

            //if (FromDate.HasValue && ToDate.HasValue && FromDate > ToDate)
            //    (FromDate, ToDate) = (ToDate, FromDate);

            if (string.IsNullOrWhiteSpace(SortDirection))
                SortDirection = "DESC";

            SortDirection = SortDirection.ToUpper();

            if (SortDirection != "ASC" && SortDirection != "DESC")
                SortDirection = "DESC";
        }
        public int Offset => (Page - 1) * PageSize;

    }

    public class PaginationRequests
    {

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public string? Search { get; set; }

        //public string? SortBy { get; set; }

        //public string? SortDirection { get; set; } = "DESC";

        public void Normalizes()
        {
            if (Page < 1)
                Page = 1;

            if (PageSize < 1)
                PageSize = 20;

            if (PageSize > 100)
                PageSize = 100;

            //if (FromDate.HasValue && ToDate.HasValue && FromDate > ToDate)
            //    (FromDate, ToDate) = (ToDate, FromDate);

            //SortDirection = SortDirection.ToUpper();

            //if (SortDirection != "ASC" && SortDirection != "DESC")
            //    SortDirection = "DESC";
        }
        //public int Offset => (Page - 1) * PageSize;

    }
}
````

## QuickBalance.API/Repository/Master/BrandDB.cs (lines 69-127)

````csharp
        public async Task<PagedResponse<BrandResponse>> GetAllAsync(
            int page, int pageSize, string? search)
        {
            string where = @"
            WHERE ISNULL(DELFLG, 0) = 0
              AND (
                @Search IS NULL
                OR BRANDNAME LIKE @Search
                OR SECNAME LIKE @Search
                OR ALIAS LIKE @Search
              )";

            string countQuery = $@"
            SELECT COUNT(*)
            FROM dbo.BRANDMAS
            {where};";

            string dataQuery = $@"
            SELECT {SelectColumns}
            FROM dbo.BRANDMAS
            {where}
            ORDER BY BRANDNAME, ID
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;";

            string? searchValue = string.IsNullOrWhiteSpace(search)
                ? null
                : $"%{search.Trim()}%";

            var parameters = new
            {
                Search = searchValue,
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            };

            int totalRecords =
                await _db.QueryFirstOrDefaultAsync<int>(
                    countQuery, parameters);

            var data = await _db.QueryListAsync<BrandResponse>(
                dataQuery, parameters);

            int totalPages = totalRecords == 0
                ? 0
                : (int)Math.Ceiling(totalRecords / (double)pageSize);

            return new PagedResponse<BrandResponse>
            {
                Data = data, //.ToList(),
                Pagination = new PaginationInfo
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    TotalPages = totalPages
                }
            };
        }
````

## QuickBalance.API/Repository/Master/BrandDB.cs (lines 20-25)

````csharp
        private const string SelectColumns = @"
        ID AS Id,
        BRANDNAME AS BrandName,
        SECNAME AS SecondName,
        ALIAS AS Alias
    ";
````

## QuickBalance.API/DB/DBConn.cs (line 858)

````csharp
        public async Task<List<T>> QueryListAsync<T>(string query, object? parameters = null)
````

## QuickBalance.API/DB/DBConn.cs (line 872)

````csharp
        public async Task<T?> QueryFirstOrDefaultAsync<T>(string query, object? parameters = null)
````

## QuickBalance.API/DB/DBConn.cs (lines 908-910)

````csharp
        public async Task<SqlMapper.GridReader> QueryMultipleAsync(
     string query,
     object? parameters = null)
````
