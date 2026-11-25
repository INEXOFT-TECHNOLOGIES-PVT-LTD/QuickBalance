namespace QuickBalance.API.Repository.Master
{
    using global::QuickBalance.API.CLS;
    using global::QuickBalance.API.DB;
    using global::QuickBalance.API.DTOs.ItemCategory;
    using global::QuickBalance.API.DTOs.Respons_DTO;

    namespace QuickBalance.API.Repository.Master
    {
        public sealed class ItemCategoryDB
        {
            private readonly DBConn _db;

            public ItemCategoryDB(DBConn db)
            {
                _db = db;
            }

            private const string SelectColumns = """
            CATGCODE AS Id,
            CATGNAME AS CategoryName,
            ALIAS AS Alias,
            SECNAME AS SecondName,
            ACTIVE AS IsActive
            """;
            //LOGCD AS LogId

            // =========================================================
            // CREATE
            // =========================================================
            public async Task<int> InsertAsync(
            ItemCategoryRequest request,
            int userId)
                    {
                    const string sql = """
            INSERT INTO dbo.CATGMAS
            (
                CATGNAME,
                ALIAS,
                ACTIVE,
                LOGCD,
                UTIME,
                SECNAME
            )
            OUTPUT INSERTED.CATGCODE
            VALUES
            (
                @CategoryName,
                @Alias,
                @IsActive,
                @UserId,
                GETDATE(),
                @SecondName
            );
            """;

                var categoryId = await _db.QueryFirstOrDefaultAsync<int?>(
                    sql,
                    new
                    {
                        CategoryName = request.CategoryName.Trim(),
                        Alias = request.Alias?.Trim() ?? string.Empty,
                        IsActive = request.IsActive,
                        UserId = userId,
                        SecondName = request.SecondName?.Trim() ?? string.Empty
                    });

                return categoryId ?? 0;
            }
            //public async Task<int> InsertAsync(
            //    ItemCategoryRequest request,
            //    int userId)
            //{
            //    const string sql = """
            //    INSERT INTO dbo.CATGMAS
            //    (
            //        CATGNAME,
            //        ALIAS,
            //        ACTIVE,
            //        LOGCD,
            //        UTIME,
            //        SECNAME
            //    )
            //    OUTPUT INSERTED.CATGCODE
            //    VALUES
            //    (
            //        @CategoryName,
            //        @Alias,
            //        @IsActive,
            //        @UserId,
            //        GETDATE(),
            //        @SecondName
            //    );
            //    """;

            //    int? categoryId =
            //        await _db.QueryFirstOrDefaultAsync<int>(
            //            sql,
            //            new
            //            {
            //                CategoryName = request.CategoryName.Trim(),
            //                Alias = request.Alias?.Trim() ?? string.Empty,
            //                IsActive = request.IsActive,
            //                UserId = userId,
            //                SecondName = request.SecondName?.Trim() ?? string.Empty
            //            });

            //    return categoryId ?? 0;
            //}

            // =========================================================
            // GET ALL - PAGINATION AND SEARCH
            // =========================================================

            public async Task<PagedResponse<ItemCategoryResponse>> GetAllAsync(
                    int page,
                    int pageSize,
                    string? search,
                    bool? active = null)
            {
                var sortColumns = new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    ["Id"] = "CATGCODE",
                    ["CategoryName"] = "CATGNAME",
                    ["Alias"] = "ALIAS",
                    ["SecondName"] = "SECNAME",
                    ["IsActive"] = "ACTIVE"
                    //["UpdatedAt"] = "UTIME"
                };

                //string orderColumn =
                //    !string.IsNullOrWhiteSpace(sortBy) &&
                //    sortColumns.TryGetValue(sortBy.Trim(), out var column)
                //        ? column
                //        : "CATGNAME";

                //string direction =
                //    string.Equals(
                //        sortDirection,
                //        "DESC",
                //        StringComparison.OrdinalIgnoreCase)
                //        ? "DESC"
                //        : "ASC";

                string whereSql = $"""
                WHERE ISNULL(DELFLG, 0) = 0
                    AND (@Active IS NULL OR ISNULL(ACTIVE, 0) = @Active)
                    AND
                    (
                        @Search IS NULL
                        OR CATGNAME LIKE @Search
                        OR ALIAS LIKE @Search
                        OR SECNAME LIKE @Search
                        OR CAST(CATGCODE AS NVARCHAR(20)) LIKE @Search
                    )
                """;

                string countSql = $"""
                SELECT COUNT(1)
                FROM dbo.CATGMAS
                {whereSql};
                """;

                string dataSql = $"""
                SELECT {SelectColumns}
                FROM dbo.CATGMAS
                {whereSql}
                ORDER BY CATGCODE DESC
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY;
                """;

                string? searchPattern =
                    string.IsNullOrWhiteSpace(search)
                        ? null
                        : $"%{search.Trim()}%";

                var parameters = new
                {
                    Search = searchPattern,
                    Active = active,
                    Offset = (page - 1) * pageSize,
                    PageSize = pageSize
                };

                var totalRecords =
                    await _db.QueryFirstOrDefaultAsync<int>(countSql, parameters);
                //int? totalRecords =
                //    await _db.QueryFirstOrDefaultAsync<int>(
                //        countSql,
                //        parameters);

                var data =
                    await _db.QueryListAsync<ItemCategoryResponse>(
                        dataSql,
                        parameters);

                //return (data, totalRecords ?? 0);



                return new PagedResponse<ItemCategoryResponse>
                {
                    Data = data,
                    Pagination = new PaginationInfo
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalRecords = totalRecords,
                        TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize)
                    }
                };
            }

            // =========================================================
            // GET BY ID
            // =========================================================

            public async Task<ItemCategoryResponse?> GetByIdAsync(int id)
            {
                string sql = $"""
                SELECT {SelectColumns}
                FROM dbo.CATGMAS
                WHERE CATGCODE = @Id
                  AND ISNULL(DELFLG, 0) = 0;
                """;

                return await _db.QueryFirstOrDefaultAsync<ItemCategoryResponse>(
                    sql,
                    new { Id = id });
            }

            // =========================================================
            // CHECK DUPLICATE NAME - CREATE
            // =========================================================

            public async Task<bool> NameExistsAsync(string categoryName)
            {
                const string sql = """
                SELECT CASE WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.CATGMAS
                    WHERE CATGNAME = @CategoryName
                      AND ISNULL(DELFLG, 0) = 0
                )
                THEN 1 ELSE 0 END;
                """;

                int? result = await _db.QueryFirstOrDefaultAsync<int>(
                    sql,
                    new { CategoryName = categoryName.Trim() });

                return result == 1;
            }

            // =========================================================
            // CHECK DUPLICATE NAME - UPDATE
            // =========================================================

            public async Task<bool> NameExistsAsync(
                string categoryName,
                int categoryId)
            {
                const string sql = """
                SELECT CASE WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.CATGMAS
                    WHERE CATGNAME = @CategoryName
                      AND CATGCODE <> @CategoryId
                      AND ISNULL(DELFLG, 0) = 0
                )
                THEN 1 ELSE 0 END;
                """;

                int? result = await _db.QueryFirstOrDefaultAsync<int>(
                    sql,
                    new
                    {
                        CategoryName = categoryName.Trim(),
                        CategoryId = categoryId
                    });

                return result == 1;
            }

            // =========================================================
            // UPDATE
            // =========================================================

            public async Task<int> UpdateAsync(
                int categoryId,
                ItemCategoryRequest request)
            {
                const string sql = """
                UPDATE dbo.CATGMAS
                SET
                    CATGNAME = @CategoryName,
                    ALIAS = @Alias,
                    ACTIVE = @IsActive,
                    SECNAME = @SecondName
                WHERE CATGCODE = @CategoryId;
                """;

                return await _db.ExecuteAsync(
                    sql,
                    new
                    {
                        CategoryId = categoryId,
                        CategoryName = request.CategoryName.Trim(),
                        Alias = request.Alias?.Trim() ?? string.Empty,
                        IsActive = request.IsActive,
                        SecondName = request.SecondName?.Trim() ?? string.Empty
                    });
            }

            // =========================================================
            // ACTIVE CATEGORIES - DROPDOWN
            // =========================================================
            public async Task<bool> DeleteAsync(int Id, int userId)
            {
                const string sql = @"
                UPDATE dbo.CATGMAS
                SET
                    DELFLG = 1,
                    ACTIVE = 0,
                    LOGCD = @UserId,
                    UTIME = GETDATE()
                WHERE CATGCODE = @Id
                  AND ISNULL(DELFLG, 0) = 0";

                var rowsAffected = await _db.ExecuteAsync(
                    sql,
                    new
                    {
                        Id = Id,
                        UserId = userId
                    });
                return rowsAffected > 0;

            }

            // =========================================================
            // DELETE FLAG LOOKUP - NO ACTIVE / DELFLG FILTER
            // Returns null when the row does not exist, otherwise 0/1.
            // =========================================================
            public async Task<int?> GetDeleteFlagAsync(int id)
            {
                const string sql = """
                SELECT CAST(ISNULL(DELFLG, 0) AS INT)
                FROM dbo.CATGMAS
                WHERE CATGCODE = @Id;
                """;

                return await _db.QueryFirstOrDefaultAsync<int?>(
                    sql,
                    new { Id = id });
            }

            public async Task<List<ItemCategoryResponse>> GetActiveAsync()
            {
                string sql = $"""
                SELECT {SelectColumns}
                FROM dbo.CATGMAS
                WHERE ACTIVE = 1
                  AND ISNULL(DELFLG, 0) = 0
                ORDER BY CATGNAME;
                """;

                return await _db.QueryListAsync<ItemCategoryResponse>(sql);
            }
        }
    }
}
