using Azure.Core;
using Microsoft.Data.SqlClient;
using QuickBalance.API.DTOs;
using System.Data;

namespace QuickBalance.API.DB
{
    public class SalesOrderDB
    {
        private readonly DBConn db;

        DataTable itemDetails = new();

        DataTable additionalDetails = new();

        public SalesOrderDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public void CreateItemTable(SalesOrderRequest request)
        {
            itemDetails = new();
            itemDetails.Columns.Add("SLNO", typeof(int));
            itemDetails.Columns.Add("PRCODE", typeof(int));
            itemDetails.Columns.Add("QTY", typeof(decimal));
            itemDetails.Columns.Add("RATE", typeof(decimal));
            itemDetails.Columns.Add("LDISCPERC", typeof(decimal));
            itemDetails.Columns.Add("LDISCPRICE", typeof(decimal));
            itemDetails.Columns.Add("DISCPERC", typeof(decimal));
            itemDetails.Columns.Add("DISCPRICE", typeof(decimal));
            itemDetails.Columns.Add("UNITCODE", typeof(int));
            itemDetails.Columns.Add("GDWCODE", typeof(int));
            itemDetails.Columns.Add("ALCODE", typeof(string));
            itemDetails.Columns.Add("ALNAME", typeof(string));
            itemDetails.Columns.Add("CONVTYPE", typeof(int));
            itemDetails.Columns.Add("CONVFACT", typeof(decimal));
            itemDetails.Columns.Add("TAXABLE", typeof(decimal));
            itemDetails.Columns.Add("TAXCODE", typeof(int));
            itemDetails.Columns.Add("TAXPERC", typeof(decimal));
            itemDetails.Columns.Add("TAXAMOUNT", typeof(decimal));
            itemDetails.Columns.Add("NETAMOUNT", typeof(decimal));

            List<SalesOrderItemRequest> items = request.Items ?? new List<SalesOrderItemRequest>();

            foreach (var item in items)
            {
                item.AlCode = "0";
                item.AlName = "";
                item.GdwCode = item.GdwCode == 0 ? 1 : item.GdwCode;
                item.ConvFact = item.ConvFact == 0 ? 1 : item.ConvFact;
                item.NetAmount = item.Taxable + item.TaxAmount;

                itemDetails.Rows.Add(
                    item.SlNo,
                    item.PrCode,
                    item.Qty,
                    item.Rate,
                    item.LDiscPerc,
                    item.LDiscPrice,
                    item.DiscPerc,
                    item.DiscPrice,
                    item.UnitCode,
                    item.GdwCode,
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
            }

            //--

            additionalDetails = new();
            additionalDetails.Columns.Add("ACCID", typeof(int));
            additionalDetails.Columns.Add("AMOUNT", typeof(decimal));

            List<SalesOrderAdditionalRequest> additional = request.Additional ?? new List<SalesOrderAdditionalRequest>();

            foreach (var item in additional)
            {
                additionalDetails.Rows.Add(
                    item.AccId,
                    item.Amount
                );
            }
        }

        public async Task<DataTable> ManageSalesOrder(SalesOrderRequest request, int UserId)
        {
            CreateItemTable(request);

            List<SqlParameter> parameters = new();

            parameters.Add(new SqlParameter("@SORDNO", SqlDbType.Int) { Value = request.SordNo });
            parameters.Add(new SqlParameter("@DATE", SqlDbType.DateTime) { Value = request.Date });
            parameters.Add(new SqlParameter("@CUSCODE", SqlDbType.Int) { Value = request.CustomerCode });
            parameters.Add(new SqlParameter("@SMCODE", SqlDbType.Int) { Value = request.SalesmanCode });
            parameters.Add(new SqlParameter("@DISCPERC", SqlDbType.Decimal) { Precision = 18, Scale = 6, Value = request.DiscPerc });
            parameters.Add(new SqlParameter("@DISCAMT", SqlDbType.Decimal) { Precision = 18, Scale = 6, Value = request.DiscAmt });
            parameters.Add(new SqlParameter("@ROUNDOFF", SqlDbType.Decimal) { Precision = 18, Scale = 6, Value = request.RoundOff });
            parameters.Add(new SqlParameter("@USERID", SqlDbType.Int) { Value = UserId });

            SqlParameter itemParameter = new SqlParameter("@ITEMDTLS", SqlDbType.Structured) { TypeName = "dbo.ASK_UDT_SalesOrderItem", Value = itemDetails };
            parameters.Add(itemParameter);

            SqlParameter additionalParameter = new SqlParameter("@ADDIDTLS", SqlDbType.Structured) { TypeName = "dbo.ASK_UDT_SalesOrderAdditionalInfo", Value = additionalDetails };
            parameters.Add(additionalParameter);

            return await db.ExecuteStoredProcedure("ASK_SP_ManageSalesOrderDetails", parameters);
        }

        public async Task<DataTable> CancelSalesOrder(int SordNo, int UserId)
        {
            List<SqlParameter> parameters = new();

            parameters.Add(new SqlParameter("@SORDNO", SqlDbType.Int) { Value = SordNo });
            parameters.Add(new SqlParameter("@USERID", SqlDbType.Int) { Value = UserId });

            return await db.ExecuteStoredProcedure("ASK_SP_CancelSalesOrderDetails", parameters);
        }

        public async Task<DataTable> PendingSalesOrderInInvoices(PaginationRequest request)
        {
            List<SqlParameter> parameters = new();

            parameters.Add(new SqlParameter("@CUSTOMERID", SqlDbType.Int) { Value = request.CustomerId });
            parameters.Add(new SqlParameter("@ORDERID", SqlDbType.Int) { Value = request.OrderId });
            parameters.Add(new SqlParameter("@PAGE", SqlDbType.Int) { Value = request.Page });
            parameters.Add(new SqlParameter("@PAGESIZE", SqlDbType.Int) { Value = request.PageSize });
            parameters.Add(new SqlParameter("@SEARCH", SqlDbType.NVarChar, -1) { Value = request.Search ?? "" });
            parameters.Add(new SqlParameter("@SORTBY", SqlDbType.NVarChar, 50) { Value = request.SortBy == null ? "" : request.SortBy == "ordNo" || request.SortBy == "orderNo" ? "no" : "date" });
            parameters.Add(new SqlParameter("@SORTDIRECTION", SqlDbType.NVarChar, 50) { Value = request.SortDirection == null ? "" : request.SortDirection.ToUpper() == "ASC" ? "ASC" : "DESC" });

            return await db.ExecuteStoredProcedure("ASK_SP_PendingSalesOrderInInvoices", parameters);
        }
    }
}