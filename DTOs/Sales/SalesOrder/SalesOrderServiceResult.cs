using QuickBalance.API.DTOs;

namespace QuickBalance.API.DTOs.Sales.SalesOrder
{
    /// <summary>What happened, in business terms. The controller turns it into an HTTP status.</summary>
    public enum SalesOrderOutcome
    {
        Success,    // 200
        Created,    // 201
        Invalid,    // 400 - validation or business rule
        NotFound,   // 404
        Failed      // 500 - technical problem (already logged)
    }

    /// <summary>
    /// Result of a Sales Order service call. The service does not know HTTP;
    /// the controller builds the (unchanged) JSON from this.
    /// </summary>
    public sealed class SalesOrderServiceResult<T>
    {
        public SalesOrderOutcome Outcome { get; init; }
        public string Message { get; init; } = string.Empty;
        public T? Data { get; init; }
        public PaginationResponse? Pagination { get; init; }

        public bool IsSuccess => Outcome == SalesOrderOutcome.Success || Outcome == SalesOrderOutcome.Created;

        public static SalesOrderServiceResult<T> Ok(string message, T? data = default, PaginationResponse? pagination = null) =>
            new() { Outcome = SalesOrderOutcome.Success, Message = message, Data = data, Pagination = pagination };

        public static SalesOrderServiceResult<T> CreatedResult(string message) =>
            new() { Outcome = SalesOrderOutcome.Created, Message = message };

        public static SalesOrderServiceResult<T> Invalid(string message) =>
            new() { Outcome = SalesOrderOutcome.Invalid, Message = message };

        public static SalesOrderServiceResult<T> NotFoundResult(string message) =>
            new() { Outcome = SalesOrderOutcome.NotFound, Message = message };

        public static SalesOrderServiceResult<T> FailedResult(string message) =>
            new() { Outcome = SalesOrderOutcome.Failed, Message = message };
    }
}
