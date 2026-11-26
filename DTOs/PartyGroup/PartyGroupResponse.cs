namespace QuickBalance.API.DTOs.PartyGroup
{
    /// <summary>Single party group (GET /party-groups/{id}). Same field names as PartyGroupRequest plus id.</summary>
    public class PartyGroupResponse
    {
        public int Id { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? GroupShortName { get; set; }

        /// <summary>Customer or Supplier (same values as the request).</summary>
        public string GroupType { get; set; } = string.Empty;
    }

    /// <summary>List row. <c>Name</c> is kept for dropdown consumers; the other fields match the request names.</summary>
    public sealed class PartyGroupListItem : PartyGroupResponse
    {
        public string Name { get; set; } = string.Empty;
    }
}
