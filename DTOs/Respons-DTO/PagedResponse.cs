using QuickBalance.API.CLS;

namespace QuickBalance.API.DTOs.Respons_DTO
{
    public sealed class PagedResponse<T>
    {
        public IReadOnlyList<T> Data { get; init; } = [];

        public PaginationInfo Pagination { get; init; } = new();
    }


    //public sealed class PaginationInfo
    //{
    //    public int Page { get; init; }

    //    public int PageSize { get; init; }

    //    public int TotalRecords { get; init; }

    //    public int TotalPages { get; init; }

    //    //public bool HasPreviousPage => Page > 1;

    //    //public bool HasNextPage => Page < TotalPages;
    //}

}
