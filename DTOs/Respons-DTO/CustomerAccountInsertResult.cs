namespace QuickBalance.API.DTOs.Respons_DTO
{
        public sealed class CustomerAccountInsertResult
        {
            public int CustomerId { get; set; }

            public int AccountId { get; set; }

            public string AccountCode { get; set; } = string.Empty;
        }

        public sealed class SupplierAccountInsertResult
        {
            public int SupplierId { get; set; }

            public int AccountId { get; set; }

            public string AccountCode { get; set; } = string.Empty;
    }   
}
