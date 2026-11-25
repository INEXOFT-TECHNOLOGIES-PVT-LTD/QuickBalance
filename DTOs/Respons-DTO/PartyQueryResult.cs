
using System.Text.Json.Serialization;

namespace QuickBalance.API.DTOs.Respons_DTO
{
    public class PartyQueryResult
    {
        [JsonIgnore]
        public List<PartyAccountDto> Data { get; set; } = new();

        [JsonIgnore]
        public int TotalRecords { get; set; }
    }

    public class PartyAccountDto
    {
        public int Id { get; set; }

        public string AccCode { get; set; } = string.Empty;

        public string AccName { get; set; } = string.Empty;

        public string Alias { get; set; } = string.Empty;

        public string SecName { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string VatRegNo { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string ShipAddress { get; set; } = string.Empty;
    }

    public class PartyListResponse
    {
        public bool Status { get; set; }

        public string Message { get; set; } = string.Empty;

        public List<PartyAccountDto> Data { get; set; } = new();

        public PaginationResponse Pagination { get; set; } = new();
    }
}
