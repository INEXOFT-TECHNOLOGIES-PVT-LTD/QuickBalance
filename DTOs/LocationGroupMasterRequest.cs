using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs
{
    //public class LocationGroupMasterRequest
    //{
    //    public string GroupName { get; set; } = "";
    //    public string SecondName { get; set; } = "";
    //    public bool IsActive { get; set; } = true;
    //}



    public class LocationGroupMasterRequest
    {
        [Required(ErrorMessage = "Location group name is required.")]
        [StringLength(
            20,
            MinimumLength = 2,
            ErrorMessage = "Location group name must be between 2 and 20 characters.")]
        public string GroupName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        [StringLength(
            20,
            ErrorMessage = "Second name cannot exceed 20 characters.")]
        public string? SecondName { get; set; }
    }

    public class LocationGroupDto
    {
        public int Id { get; set; }

        public string? GroupName { get; set; }

        public bool IsActive { get; set; }

        public string? SecondName { get; set; }

        public int? LogCd { get; set; }

        //public DateTime? UTime { get; set; }
    }

    public class LocationGroupDropdownDto
    {
        public int Id { get; set; }

        public string? GroupName { get; set; }
    }
}