using System;
using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs
{
    public class LocationMasterRequest
    {
        [Required(ErrorMessage = "Location name is required.")]
        [StringLength(
            25,
            MinimumLength = 2,
            ErrorMessage = "Location name must be between 2 and 25 characters.")]
        public string LocationName { get; set; } = string.Empty;
        [Required]
        [Range(
            1,
            100000,
            ErrorMessage = "Please select a valid location group.")]
        public int GroupId { get; set; }

        public bool IsActive { get; set; } = true;
        [Required(ErrorMessage = "Stock Account is required.")]

        [Range(
            1,
            100000,
            ErrorMessage = "Please select a valid stock account.")]
        public int StockAccountId { get; set; }

        public bool StockPLFlag { get; set; }

        [StringLength(
            25,
            ErrorMessage = "Second name cannot exceed 25 characters.")]
        public string? SecondName { get; set; }
    }

    public class LocationDto
    {
        public int Id { get; set; }
        public string? LocationName { get; set; }   // GDWMAS.GDWNAME (same value as request.locationName)
        public int GroupId { get; set; }            // GDWMAS.GRPID  (same value as request.groupId)
        public bool IsActive { get; set; }
        public int StockAccountId { get; set; }
        public bool StockPLFlag { get; set; }
        public string? SecondName { get; set; }
        public int? LogCd { get; set; }
        //public DateTime? UTime { get; set; }
    }

    /// <summary>Row for GET /api/v1/locations (list only; no internal fields).</summary>
    public class LocationListItemDto
    {
        public int Id { get; set; }
        public string? LocationName { get; set; }
        public int GroupId { get; set; }
        public bool IsActive { get; set; }
        public int StockAccountId { get; set; }
        public bool StockPLFlag { get; set; }
        public string? SecondName { get; set; }
    }

    public class LocationActiveDto
    {
        public int Id { get; set; }
        public string? LocationName { get; set; }
    }

    public class LocationDropdownDto
    {
        public int Id { get; set; }
        public string? Godown { get; set; }
    }
}