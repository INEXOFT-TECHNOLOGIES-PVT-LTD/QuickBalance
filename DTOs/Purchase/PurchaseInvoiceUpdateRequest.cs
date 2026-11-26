namespace QuickBalance.API.DTOs.Purchase
{
    /// <summary>    
    /// Update uses the same payload as create. The voucher is identified by the route id (PURM.VRNO).    
    /// ORDNO, INVNOTEXT, LOGCD and CREATEDDT are never changed on update (review §5).    
    /// </summary>    
    public class PurchaseInvoiceUpdateRequest : PurchaseInvoiceCreateRequest
    {
    }
}
