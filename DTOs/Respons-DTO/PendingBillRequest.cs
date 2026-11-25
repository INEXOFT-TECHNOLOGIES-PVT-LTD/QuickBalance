using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs.Respons_DTO
{
    //public class PendingBillRequest
    //{
    //    public int? ID { get; set; }

    //    public DateTime InvDate { get; set; }
    //    public string? Narration { get; set; }
    //    public string InvNo { get; set; } = "";
    //    public decimal Amount { get; set; }
    //    public DateTime DueDate { get; set; }
    //    public string DrCr { get; set; } = "Cr"; // "Dr" or "Cr"

    //}


    public class PendingBillRequest
    {
        // Existing bill ID; null or 0 means create a new bill.
        [Range(1, int.MaxValue,
            ErrorMessage = "Pending bill ID must be greater than zero.")]
        public int? Id { get; set; }

        [Required(ErrorMessage = "Invoice date is required.")]
        public DateTime InvoiceDate { get; set; }

        [StringLength(250,
            ErrorMessage = "Narration cannot exceed 250 characters.")]
        public string? Narration { get; set; }

        [Required(ErrorMessage = "Invoice number is required.")]
        [StringLength(50,
            ErrorMessage = "Invoice number cannot exceed 50 characters.")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Range(typeof(decimal), "0", "1000000",
            ErrorMessage = "Pending bill amount must be between 0 and 1000000.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Due date is required.")]
        public DateTime DueDate { get; set; }

        // DR = Debit, CR = Credit
        [Required(ErrorMessage = "Debit/Credit type is required.")]
        [RegularExpression(@"^(?i:DR|CR)$",
            ErrorMessage = "Debit/Credit type must be DR or CR.")]
        public string DrCr { get; set; } = "DR";
    }



    public class PendingBillUpdateRequest
    {
        // Existing bill ID; null or 0 means create a new bill.
        [Range(1, int.MaxValue,
            ErrorMessage = "Pending bill ID must be greater than zero.")]
        public int? Id { get; set; }

        [Required(ErrorMessage = "Invoice date is required.")]
        public DateTime InvDate { get; set; }

        [StringLength(250,
            ErrorMessage = "Narration cannot exceed 250 characters.")]
        public string? Narration { get; set; }

        [Required(ErrorMessage = "Invoice number is required.")]
        [StringLength(50,
            ErrorMessage = "Invoice number cannot exceed 50 characters.")]
        public string InvNo { get; set; } = string.Empty;

        [Range(typeof(decimal), "0", "1000000",
            ErrorMessage = "Pending bill amount must be between 0 and 1000000.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Due date is required.")]
        public DateTime DueDate { get; set; }

        // DR = Debit, CR = Credit
        [Required(ErrorMessage = "Debit/Credit type is required.")]
        [RegularExpression(@"^(?i:DR|CR)$",
            ErrorMessage = "Debit/Credit type must be DR or CR.")]
        public string DrCr { get; set; } = "DR";
    }

    //public class PendingBillUpdateRequest
    //{
    //    public int? ID { get; set; }

    //    public DateTime InvDate { get; set; }
    //    public string? Narration { get; set; }
    //    public string InvNo { get; set; } = "";
    //    public decimal Amount { get; set; }
    //    public DateTime DueDate { get; set; }
    //    public string DrCr { get; set; } = "Cr"; // "Dr" or "Cr"

    //}
}
