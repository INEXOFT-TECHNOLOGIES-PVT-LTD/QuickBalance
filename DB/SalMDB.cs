using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs.Respons_DTO;
using System.Data;

namespace QuickBalance.API.DB
{
    public class SalMDB
    {
        private readonly DBConn db;
        public Variable Variables = new Variable();
        public Function Functions = new Function();
        string qry = "";

        public SalMDB(DBConn dbConn)
        {
            db = dbConn;
        }

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

        public async Task<DataTable> SearchByInvno(ClsSalM clsSalM)
        {
            qry = "SELECT ACCMAS.ACCNAME, SMMAST.SMNAME, SALM.*, DTUSER.USERNAME FROM (((ACCMAS INNER JOIN SALM ON ACCMAS.ID = SALM.CUSTCODE) LEFT JOIN SMMAST ON SALM.SMCODE = SMMAST.SMCODE) LEFT JOIN (SELECT USERCODE, FULLNAME AS USERNAME FROM TBUSER) AS DTUSER ON SALM.LOGCD = DTUSER.USERCODE) WHERE SALM.INVNO='" + clsSalM.InvNo + "'";
            return await db.GetDatatable(qry);
        }

        public async Task<DataTable> SearchByOrdNo(ClsSalM clsSalM)
        {
            qry = "SELECT * FROM SALM WHERE ORDNO='" + clsSalM.OrdNo + "' AND RETURNFLG='" + clsSalM.ReturnFlg + "'";
            return await db.GetDatatable(qry);
        }


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
    }
}
