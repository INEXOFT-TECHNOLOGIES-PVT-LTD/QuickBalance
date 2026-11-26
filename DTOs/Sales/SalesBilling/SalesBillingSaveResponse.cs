namespace QuickBalance.API.DTOs.Sales.SalesBilling
{
    public class SalesBillingSaveResponse
    {
        public bool IsNew { get; set; }                       // true = created, false = updated
        public string Message { get; set; } = string.Empty;   // message from the procedure
    }
}
