using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs.SalesMan
{
    public class SalesmanRequest
    {
        public int? EmployeeCode { get; set; }

        [Required(ErrorMessage = "Salesman name is required.")]
        [StringLength(100, ErrorMessage = "Salesman name cannot exceed 100 characters.")]
        public string SalesmanName { get; set; } = "";

        public string? SecondName { get; set; } = "";

        public string? PrintName { get; set; } = "";

        public string? Address { get; set; } = "";

        public string? Phone { get; set; } = "";

        // Optional. The format is checked in SalesmanService only when not blank
        // (EmailAddressAttribute rejects "" so it must not be applied here).
        public string? Email { get; set; } = "";

        public bool IsActive { get; set; } = true;

        [Range(1, int.MaxValue, ErrorMessage = "Invalid salesman group.")]
        public int SalesmanGroupId { get; set; }

        public bool ConnectWithAccount { get; set; } = false;

        public int AccountId { get; set; } = 0;
    }
}
