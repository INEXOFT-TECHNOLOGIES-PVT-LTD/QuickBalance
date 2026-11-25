namespace QuickBalance.API.DTOs
{
    public class ProductValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = "";

        public static ProductValidationResult Success()
        {
            return new ProductValidationResult
            {
                IsValid = true
            };
        }

        public static ProductValidationResult Failed(string message)
        {
            return new ProductValidationResult
            {
                IsValid = false,
                Message = message
            };
        }
    }
}