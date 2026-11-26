namespace QuickBalance.API.DTOs.Sales.SalesReturn
{
    public class SalesReturnSaveResponse
    {
        public bool IsNew { get; set; }                       // true = created, false = updated
        public string Message { get; set; } = string.Empty;   // message from the procedure
        public int ReturnId { get; set; }                     // internal id (SALM.INVNO) of the saved return
        public int ReturnNo { get; set; }                     // visible return number (SALM.ORDNO)
    }
}
