using System.Text.Json.Serialization;

namespace QuickBalance.API.CLS
{
    // ============================================================
    // STANDARD RESPONSE (WITH OPTIONAL PAGINATION)
    // ============================================================

    public sealed class ApiResponse<T>
    {
        public bool Status { get; set; }

        public string Message { get; set; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public T? Data { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PaginationInfo? Pagination { get; set; }

        public static ApiResponse<T> Success(
            string message,
            T? data = default)
        {
            return new ApiResponse<T>
            {
                Status = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> SuccessWithPagination(
            string message,
            T? data,
            PaginationInfo pagination)
        {
            return new ApiResponse<T>
            {
                Status = true,
                Message = message,
                Data = data,
                Pagination = pagination
            };
        }

        public static ApiResponse<T> Failure(string message)
        {
            return new ApiResponse<T>
            {
                Status = false,
                Message = message
            };
        }
    }

    // ============================================================
    // STANDARD RESPONSE (WITHOUT PAGINATION)
    // ============================================================

    public sealed class ApiResponseWithoutPagination<T>
    {
        public bool Status { get; set; }

        public string Message { get; set; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public T? Data { get; set; }

        public static ApiResponseWithoutPagination<T> Success(
            string message,
            T? data = default)
        {
            return new ApiResponseWithoutPagination<T>
            {
                Status = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponseWithoutPagination<T> Failure(
            string message)
        {
            return new ApiResponseWithoutPagination<T>
            {
                Status = false,
                Message = message
            };
        }
    }

    // ============================================================
    // ERROR RESPONSE (NO DATA)
    // ============================================================

    public sealed class ApiResponseWithError
    {
        public bool Status { get; set; } = false;

        public string Message { get; set; } = string.Empty;

        public static ApiResponseWithError Failure(string message)
        {
            return new ApiResponseWithError
            {
                Status = false,
                Message = message
            };
        }
    }


    public sealed class ApiResponseWithSuccess
    {
        public bool Status { get; set; } = true;

        public string Message { get; set; } = string.Empty;

        public static ApiResponseWithError Success(string message)
        {
            return new ApiResponseWithError
            {
                Status = true,
                Message = message
            };
        }
    }
    // ============================================================
    // PAGINATION
    // ============================================================

    public sealed class PaginationInfo
    {
        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalRecords { get; set; }

        public int TotalPages { get; set; }
    }
}
