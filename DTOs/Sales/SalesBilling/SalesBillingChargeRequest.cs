using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs.Sales.SalesBilling
{
    public class SalesBillingChargeRequest
    {
        // ACCID - the ledger for this charge (e.g. Freight).
        [Range(1, int.MaxValue, ErrorMessage = "AccountCode is required.")]
        public int AccountCode { get; set; }

        // AMOUNT
        public decimal Amount { get; set; }
    }
}
