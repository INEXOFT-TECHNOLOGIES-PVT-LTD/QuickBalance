using Microsoft.Data.SqlClient;
using QuickBalance.API.DTOs;
using System.Data;

namespace QuickBalance.API.DB
{
    public class SalesReturnDB
    {
        private readonly DBConn db;

        DataTable itemDetails = new();

        DataTable serialDetails = new();

        DataTable additionalDetails = new();

        public SalesReturnDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public void CreateItemTable(SalesReturnRequest request)
        {
            itemDetails = new();
            itemDetails.Columns.Add("SLNO", typeof(int));
            itemDetails.Columns.Add("PRCODE", typeof(int));
            itemDetails.Columns.Add("QTY", typeof(decimal));
            itemDetails.Columns.Add("RATE", typeof(decimal));
            itemDetails.Columns.Add("PRRATE", typeof(decimal));
            itemDetails.Columns.Add("MRP", typeof(decimal));
            itemDetails.Columns.Add("LDISCPERC", typeof(decimal));
            itemDetails.Columns.Add("LDISCPRICE", typeof(decimal));
            itemDetails.Columns.Add("DISCPERC", typeof(decimal));
            itemDetails.Columns.Add("DISCPRICE", typeof(decimal));
            itemDetails.Columns.Add("BATCH", typeof(string));
            itemDetails.Columns.Add("MFGDATE", typeof(DateTime));
            itemDetails.Columns.Add("EXPDATE", typeof(DateTime));
            itemDetails.Columns.Add("UNITCODE", typeof(int));
            itemDetails.Columns.Add("GDWCODE", typeof(int));
            itemDetails.Columns.Add("SORDNO", typeof(int));
            itemDetails.Columns.Add("ORDROWID", typeof(int));
            itemDetails.Columns.Add("ALCODE", typeof(string));
            itemDetails.Columns.Add("ALNAME", typeof(string));
            itemDetails.Columns.Add("CONVTYPE", typeof(int));
            itemDetails.Columns.Add("CONVFACT", typeof(decimal));
            itemDetails.Columns.Add("TAXABLE", typeof(decimal));
            itemDetails.Columns.Add("TAXCODE", typeof(int));
            itemDetails.Columns.Add("TAXPERC", typeof(decimal));
            itemDetails.Columns.Add("TAXAMOUNT", typeof(decimal));
            itemDetails.Columns.Add("NETAMOUNT", typeof(decimal));

            serialDetails = new();
            serialDetails.Columns.Add("INVRTYPE", typeof(string));
            serialDetails.Columns.Add("INVRNO", typeof(int));
            serialDetails.Columns.Add("INORDNO", typeof(int));
            serialDetails.Columns.Add("INSLNO", typeof(int));
            serialDetails.Columns.Add("PRCODE", typeof(int));
            serialDetails.Columns.Add("OUTSLNO", typeof(int));
            serialDetails.Columns.Add("UNITCODE", typeof(int));
            serialDetails.Columns.Add("GDWCODE", typeof(int));
            serialDetails.Columns.Add("ITEMSLNO", typeof(string));

            List<SalesReturnItemRequest> items = request.Items ?? new List<SalesReturnItemRequest>();

            foreach (var item in items)
            {
                item.Mrp = item.Mrp == 0 ? item.Rate : item.Mrp;
                item.AlCode = "0";
                item.AlName = "";
                item.GdwCode = item.GdwCode == 0 ? 1 : item.GdwCode;
                item.ConvFact = item.ConvFact == 0 ? 1 : item.ConvFact;
                item.MfgDate = item.MfgDate ?? request.Date;
                item.ExpDate = item.ExpDate ?? request.Date;
                item.NetAmount = item.Taxable + item.TaxAmount;

                itemDetails.Rows.Add(
                    item.SlNo,
                    item.PrCode,
                    item.Qty,
                    item.Rate,
                    item.PrRate,
                    item.Mrp,
                    item.LDiscPerc,
                    item.LDiscPrice,
                    item.DiscPerc,
                    item.DiscPrice,
                    item.Batch ?? string.Empty,
                    item.MfgDate.HasValue ? item.MfgDate.Value : DBNull.Value,
                    item.ExpDate.HasValue ? item.ExpDate.Value : DBNull.Value,
                    item.UnitCode,
                    item.GdwCode,
                    item.SordNo,
                    item.OrdRowId,
                    item.AlCode ?? string.Empty,
                    item.AlName ?? string.Empty,
                    item.ConvType,
                    item.ConvFact,
                    item.Taxable,
                    item.TaxCode,
                    item.TaxPerc,
                    item.TaxAmount,
                    item.NetAmount
                );

                //--

                List<SalesReturnSerialRequest> serialNumbers = item.SerialNumbers ?? new List<SalesReturnSerialRequest>();

                foreach (var serial in serialNumbers)
                {
                    serialDetails.Rows.Add(
                        serial.InvRType ?? string.Empty,
                        serial.InVrNo,
                        serial.InOrdNo,
                        serial.InSlNo,
                        item.PrCode,
                        item.SlNo,
                        item.UnitCode,
                        item.GdwCode,
                        serial.ItemSlNo ?? string.Empty
                    );
                }
            }

            //--

            additionalDetails = new();
            additionalDetails.Columns.Add("ACCID", typeof(int));
            additionalDetails.Columns.Add("AMOUNT", typeof(decimal));

            List<SalesReturnAdditionalRequest> additional = request.Additional ?? new List<SalesReturnAdditionalRequest>();

            foreach (var item in additional)
            {
                additionalDetails.Rows.Add(
                    item.AccId,
                    item.Amount
                );
            }
        }

        public async Task<DataTable> ManageSalesReturn(SalesReturnRequest request, int UserId)
        {
            request.PayMode = request.PayMode == null || request.PayMode.ToUpper() == "CASH" ? "Cash" : request.PayMode.ToString().ToUpper() == "CREDIT" ? "Credit" : "Bank";
            request.DueDate = request.DueDate ?? request.Date;

            CreateItemTable(request);

            List<SqlParameter> parameters = new();

            parameters.Add(new SqlParameter("@SRETNO", SqlDbType.Int) { Value = request.SretNo });
            parameters.Add(new SqlParameter("@SINVNO", SqlDbType.Int) { Value = request.SinvNo });
            parameters.Add(new SqlParameter("@DATE", SqlDbType.DateTime) { Value = request.Date });
            parameters.Add(new SqlParameter("@CUSCODE", SqlDbType.Int) { Value = request.CustomerCode });
            parameters.Add(new SqlParameter("@PAYMODE", SqlDbType.NVarChar, 100) { Value = request.PayMode });
            parameters.Add(new SqlParameter("@CLCODE", SqlDbType.Int) { Value = request.CashLedgerCode });
            parameters.Add(new SqlParameter("@BLCODE", SqlDbType.Int) { Value = request.BankLedgerCode });
            parameters.Add(new SqlParameter("@DUEDATE", SqlDbType.DateTime) { Value = request.DueDate });
            parameters.Add(new SqlParameter("@SMCODE", SqlDbType.Int) { Value = request.SalesmanCode });
            parameters.Add(new SqlParameter("@DISCPERC", SqlDbType.Decimal) { Precision = 18, Scale = 6, Value = request.DiscPerc });
            parameters.Add(new SqlParameter("@DISCAMT", SqlDbType.Decimal) { Precision = 18, Scale = 6, Value = request.DiscAmt });
            parameters.Add(new SqlParameter("@ROUNDOFF", SqlDbType.Decimal) { Precision = 18, Scale = 6, Value = request.RoundOff });
            parameters.Add(new SqlParameter("@USERID", SqlDbType.Int) { Value = UserId });

            SqlParameter itemParameter = new SqlParameter("@ITEMDTLS", SqlDbType.Structured) { TypeName = "dbo.ASK_UDT_SalesReturnItem", Value = itemDetails };
            parameters.Add(itemParameter);

            SqlParameter serialParameter = new SqlParameter("@SLNODTLS", SqlDbType.Structured) { TypeName = "dbo.ASK_UDT_SalesReturnSerialNumber", Value = serialDetails };
            parameters.Add(serialParameter);

            SqlParameter additionalParameter = new SqlParameter("@ADDIDTLS", SqlDbType.Structured) { TypeName = "dbo.ASK_UDT_SalesReturnAdditionalInfo", Value = additionalDetails };
            parameters.Add(additionalParameter);

            return await db.ExecuteStoredProcedure("ASK_SP_ManageSalesReturnDetails", parameters);
        }

        public async Task<DataTable> CancelSalesReturn(int SretNo, int UserId)
        {
            List<SqlParameter> parameters = new();

            parameters.Add(new SqlParameter("@SRETNO", SqlDbType.Int) { Value = SretNo });
            parameters.Add(new SqlParameter("@USERID", SqlDbType.Int) { Value = UserId });

            return await db.ExecuteStoredProcedure("ASK_SP_CancelSalesReturnDetails", parameters);
        }
    }
}