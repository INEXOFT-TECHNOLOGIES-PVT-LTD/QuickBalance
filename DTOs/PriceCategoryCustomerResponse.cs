namespace QuickBalance.API.DTOs
{
    public class PriceCategoryCustomerResponse
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = "";
        public string Alias { get; set; } = "";
        public string AccountCode { get; set; } = "";
        public string Phone { get; set; } = "";
        public int CategoryCode { get; set; }
        public string CategoryName { get; set; } = "";
    }
}