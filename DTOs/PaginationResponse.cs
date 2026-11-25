namespace QuickBalance.API.DTOs
{
    public class PaginationResponse
    {
        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalRecords { get; set; }

        public int TotalPages { get; set; }
    }

    public class PaginationResponse<T>
    {
        public List<T> Data { get; set; } = new();

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalRecords { get; set; }

        public int TotalPages { get; set; }
    }
}
