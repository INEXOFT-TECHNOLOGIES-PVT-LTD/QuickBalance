namespace QuickBalance.API.DTOs
{
    public class PurchaseOrderUpdateResult
    {
        public bool Success { get; set; }
        public bool NotFound { get; set; }
        public bool AlreadyLinked { get; set; }
    }
}
