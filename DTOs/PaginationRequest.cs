namespace QuickBalance.API.DTOs
{
        public class PaginationRequest
        {
            public string? Type { get; set; } = string.Empty;

            public int CustomerId { get; set; }

            public int OrderId { get; set; }

            public int Page { get; set; } = 1;

            public int PageSize { get; set; } = 20;

            public string? Search { get; set; }

            public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public string? SortBy { get; set; }

        public string? SortDirection { get; set; } = "DESC";

        public void Normalize()
        {
            if (Page < 1)
                Page = 1;

            if (PageSize < 1)
                PageSize = 20;

            if (PageSize > 100)
                PageSize = 100;

            //if (FromDate.HasValue && ToDate.HasValue && FromDate > ToDate)
            //    (FromDate, ToDate) = (ToDate, FromDate);

            if (string.IsNullOrWhiteSpace(SortDirection))
                SortDirection = "DESC";

            SortDirection = SortDirection.ToUpper();

            if (SortDirection != "ASC" && SortDirection != "DESC")
                SortDirection = "DESC";
        }
        public int Offset => (Page - 1) * PageSize;

    }

    public class PaginationRequests
    {

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public string? Search { get; set; }

        //public string? SortBy { get; set; }

        //public string? SortDirection { get; set; } = "DESC";

        public void Normalizes()
        {
            if (Page < 1)
                Page = 1;

            if (PageSize < 1)
                PageSize = 20;

            if (PageSize > 100)
                PageSize = 100;

            //if (FromDate.HasValue && ToDate.HasValue && FromDate > ToDate)
            //    (FromDate, ToDate) = (ToDate, FromDate);

            //SortDirection = SortDirection.ToUpper();

            //if (SortDirection != "ASC" && SortDirection != "DESC")
            //    SortDirection = "DESC";
        }
        //public int Offset => (Page - 1) * PageSize;

    }
}
