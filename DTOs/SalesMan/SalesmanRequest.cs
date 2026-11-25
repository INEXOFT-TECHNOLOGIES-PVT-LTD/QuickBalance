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

        [EmailAddress(ErrorMessage = "Incorrect email format.")]
        public string Email { get; set; } = "";

        public bool IsActive { get; set; } = true;

        [Range(1, int.MaxValue, ErrorMessage = "Invalid salesman group.")]
        public int SalesmanGroupId { get; set; }

        public bool ConnectWithAccount { get; set; } = false;

        public int AccountId { get; set; } = 0;
    }
}
