using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs
{
    //public class BomAdditionalInfoRequest
    //{
    //    public int AccountId { get; set; }
    //    public decimal Amount { get; set; }
    //}
    public class BomAdditionalInfoRequest
    {
        [Range(1, int.MaxValue,
            ErrorMessage = "Please select a valid account.")]
        public int AccountId { get; set; }

        [Range(typeof(decimal), "0", "999999999",
            ErrorMessage = "Amount cannot be negative.")]
        public decimal Amount { get; set; }
    }

    //public class BomAdditionalInfoRequest
    //{
    //    //[Range(1, int.MaxValue)]
    //    public int AccountId { get; set; }

    //    //[Range(typeof(decimal), "0", "999999999")]
    //    public decimal Amount { get; set; }
    //}
}