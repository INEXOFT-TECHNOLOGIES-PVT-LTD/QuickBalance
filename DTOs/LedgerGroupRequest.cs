using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs
{
    public class LedgerGroupRequest
    {

        [Required(ErrorMessage = "Group name is required.")]
        public string GroupName { get; set; } = "";

        public string? SecondName { get; set; }

        /// <summary>Ignored on create (generated). On update, null or 0 keeps the existing code.</summary>
        public int? GroupCode { get; set; }

        // P = Profit & Loss
        // T = Trading
        // B = Balance Sheet
        // M = Manufacturing
        /// <summary>P, T, B or M. Required when mainGroup = true; sub-groups inherit it from the parent.</summary>
        public string? FinalAccount { get; set; }

        public bool SubLedgerFlag { get; set; } = false;

        public bool MainGroup { get; set; } = true;

        // Parent/Main group ID.
        // Required when MainGroup = false.
        public int MainGroupId { get; set; } = 0;

        /// <summary>Null or 0 means auto-generate (create) or keep (update).</summary>
        public int? SequenceNo { get; set; }

        // Dr / Cr
        /// <summary>CR or DR. Required when mainGroup = true; sub-groups inherit it from the parent.</summary>
        public string? CrDr { get; set; }

        public bool IsActive { get; set; } = true;
    }
}