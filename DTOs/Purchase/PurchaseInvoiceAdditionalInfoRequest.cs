using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs.Purchase
{
    /// <summary>    
    /// One row of the legacy dgv_add_info grid. RowIndex meaning (review claim 31):    
    /// 0 = Freight (Value), 1 = Discount (Percentage), 2 = Customs duty (Percentage or Value),    
    /// 3 = VAT (ignored: invoice-level VAT mode is not supported), 4 = Round-off (Value),    
    /// 5+ = other additional amounts (Value).    
    /// </summary>    
    public class PurchaseInvoiceAdditionalInfoRequest
    {
        [Range(0, 50)]
        public int RowIndex { get; set; }

        /// <summary>Ledger written to PURD.CODE.</summary>    
        public int AccountCode { get; set; }

        /// <summary>Col_GCCHead → PURD.GCCCD.</summary>    
        public int GccHeadCode { get; set; }

        public decimal? Percentage { get; set; }

        public decimal Value { get; set; }
    }
}
