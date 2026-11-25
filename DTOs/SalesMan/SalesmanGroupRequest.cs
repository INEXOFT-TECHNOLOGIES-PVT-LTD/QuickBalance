using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs.SalesMan
{
    public class SalesmanGroupRequest
    {
        [Required(ErrorMessage = "Group name is required.")]
        public string GroupName { get; set; } = "";

        public string? SecondName { get; set; } = "";

        public string? PrintName { get; set; } = "";

        public bool IsActive { get; set; } = true;
    }
}
