
using System.Collections.Generic;

namespace QuickBalance.API.DTOs.Purchase
{
    public class PurchaseOrderCreateRequest
    {
        public PurchaseOrderMasterDto Master { get; set; } = new();

        public List<PurchaseOrderItemDto> Items { get; set; } = new();
    }
}