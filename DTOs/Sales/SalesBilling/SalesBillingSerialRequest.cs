using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs.Sales.SalesBilling
{
    public class SalesBillingSerialRequest
    {
        // INVRTYPE - type of the voucher that brought this serial into stock.
        [Required]
        public string SourceVoucherType { get; set; } = string.Empty;

        // INVRNO, INORDNO, INSLNO - that voucher's id, number and line.
        public int SourceVoucherId { get; set; }
        public int SourceVoucherNo { get; set; }
        public int SourceLineNo { get; set; }

        // ITEMSLNO - the serial number itself.
        [Required]
        public string SerialNo { get; set; } = string.Empty;
    }
}
