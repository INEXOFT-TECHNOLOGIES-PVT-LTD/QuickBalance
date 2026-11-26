using QuickBalance.API.CLS;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Respons_DTO;

namespace QuickBalance.API.Repository.Master
{
    public sealed class ItemGroupDBs 
    {
        private readonly DBConn _db;

        public ItemGroupDBs(DBConn db)
        {
            _db = db;
        }

        private const string SelectColumns = """
        PRGRPID AS Id,
        GRPNAME AS GroupName,
        GRPCODE AS GroupCode,
        PRIMGRPFLG AS IsPrimaryGroup,
        PRIMGRPID AS PrimaryGroupId,
        TAGACCFLG AS TagWithAccount,
        PURACCID AS PurchaseAccountId,
        SALESACCID AS SalesAccountId,
        STOCKACCID AS StockAccountId,
        LOGCD AS LogCode,
        UTIME AS UpdateTime,
        ACTIVE AS IsActive,
        SECNAME AS SecName
        """;

        // List rows: same as SelectColumns without the internal LOGCD / UTIME.
        private const string ListColumns = """
        PRGRPID AS Id,
        GRPNAME AS GroupName,
        GRPCODE AS GroupCode,
        PRIMGRPFLG AS IsPrimaryGroup,
        PRIMGRPID AS PrimaryGroupId,
        TAGACCFLG AS TagWithAccount,
        PURACCID AS PurchaseAccountId,
        SALESACCID AS SalesAccountId,
        STOCKACCID AS StockAccountId,
        ACTIVE AS IsActive,
        SECNAME AS SecName
        """;



        public async Task<int> InsertAsync(ItemGroupRequest request, int userId)
        {
            var sql = """
            INSERT INTO dbo.PRGRPMAS
            (
                GRPNAME, GRPCODE, PRIMGRPFLG, PRIMGRPID,
                TAGACCFLG, PURACCID, SALESACCID, STOCKACCID,
                LOGCD, UTIME, ACTIVE, SECNAME
            )
            VALUES
            (
                @GroupName, @GroupCode, @IsPrimaryGroup, @PrimaryGroupId,
                @TagWithAccount, @PurchaseAccount, @SalesAccount, @StockAccount,
                @UserId, GETDATE(), @IsActive, @SecName
            );

            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

            return await _db.QueryFirstOrDefaultAsync<int>(sql, new
            {
                GroupName = request.GroupName.Trim(),
                GroupCode = request.GroupCode ?? 0,
                request.IsPrimaryGroup,

                // Primary groups always store PRIMGRPID = 0.
                PrimaryGroupId = request.IsPrimaryGroup ? 0 : request.PrimaryGroupId ?? 0,

                request.TagWithAccount,

                PurchaseAccount = request.TagWithAccount
                    ? request.PurchaseAccountId ?? 0 : 0,

                SalesAccount = request.TagWithAccount
                    ? request.SalesAccountId ?? 0 : 0,

                StockAccount = request.TagWithAccount
                    ? request.StockAccountId ?? 0 : 0,

                UserId = userId,
                request.IsActive,
                request.SecName
            });
        }

        //public async Task<int> InsertAsync(ItemGroupRequest request,int userId)
        //{
        //    var sql = """
        //    INSERT INTO dbo.PRGRPMAS
        //    (
        //        GRPNAME, GRPCODE, PRIMGRPFLG, PRIMGRPID,
        //        TAGACCFLG, PURACCID, SALESACCID, STOCKACCID,
        //        LOGCD, UTIME, ACTIVE, SECNAME
        //    )
        //    OUTPUT INSERTED.PRGRPID
        //    VALUES
        //    (
        //        @GroupName, @GroupCode, @IsPrimaryGroup, @PrimaryGroupId,
        //        @TagWithAccount, @PurchaseAccount, @SalesAccount, @StockAccount,
        //        @UserId, GETDATE(), @IsActive, @SecName
        //    );
        //    """;

        //    return await _db.QueryFirstOrDefaultAsync<int>(sql, new
        //    {
        //        GroupName = request.GroupName.Trim(),
        //        request.GroupCode,
        //        request.IsPrimaryGroup,
        //        PrimaryGroupId = request.IsPrimaryGroup ? 0 : request.PrimaryGroupId,
        //        request.TagWithAccount,
        //        PurchaseAccount = request.TagWithAccount ? request.PurchaseAccount : 0,
        //        SalesAccount = request.TagWithAccount ? request.SalesAccount : 0,
        //        StockAccount = request.TagWithAccount ? request.StockAccount : 0,
        //        UserId = userId,
        //        request.IsActive,
        //        request.SecName
        //    });
        //}

        public async Task<ItemGroupResponse?> GetByIdAsync(int id)
        {
            var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.PRGRPMAS
            WHERE PRGRPID = @Id
              AND ISNULL(DELFLG, 0) = 0;
            """;

            return await _db.QueryFirstOrDefaultAsync<ItemGroupResponse>(
                sql, new { Id = id });
        }

        public async Task<PagedResponse<ItemGroupListItemDto>> GetAllAsync(int page,int pageSize,string? search,bool? active)
        {
            var offset = (page - 1) * pageSize;

            var where = """
            WHERE ISNULL(DELFLG, 0) = 0
              AND (@Active IS NULL OR ISNULL(ACTIVE, 0) = @Active)
              AND (@Search IS NULL
                 OR GRPNAME LIKE @SearchPattern
                 OR CAST(GRPCODE AS NVARCHAR(30)) LIKE @SearchPattern)
            """;

            var parameters = new
            {
                Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
                SearchPattern = $"%{search?.Trim()}%",
                Active = active,
                Offset = offset,
                PageSize = pageSize
            };

            var countSql = $"""
            SELECT COUNT(1)
            FROM dbo.PRGRPMAS
            {where};
            """;

            var dataSql = $"""
            SELECT {ListColumns}
            FROM dbo.PRGRPMAS
            {where}
            ORDER BY PRGRPID DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

            var totalRecords =
                await _db.QueryFirstOrDefaultAsync<int>(countSql, parameters);

            var data =
                await _db.QueryListAsync<ItemGroupListItemDto>(
                    dataSql, parameters);

            return new PagedResponse<ItemGroupListItemDto>
            {
                Data = data,
                Pagination = new PaginationInfo
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    TotalPages = (int)Math.Ceiling(
              totalRecords / (double)pageSize)
                }
            };
        }

        public async Task<int> UpdateAsync(
            int id,
            ItemGroupRequest request,
            int userId)
        {
            var sql = """
            UPDATE dbo.PRGRPMAS
            SET
                GRPNAME = @GroupName,
                GRPCODE = @GroupCode,
                PRIMGRPFLG = @IsPrimaryGroup,
                PRIMGRPID = @PrimaryGroupId,
                TAGACCFLG = @TagWithAccount,
                PURACCID = @PurchaseAccount,
                SALESACCID = @SalesAccount,
                STOCKACCID = @StockAccount,
                LOGCD = @UserId,
                UTIME = GETDATE(),
                ACTIVE = @IsActive,
                SECNAME = @SecName
            WHERE PRGRPID = @Id;
            """;

            return await _db.ExecuteAsync(sql, new
            {
                Id = id,
                GroupName = request.GroupName.Trim(),
                GroupCode = request.GroupCode ?? 0,
                request.IsPrimaryGroup,
                PrimaryGroupId = request.IsPrimaryGroup ? 0 : request.PrimaryGroupId ?? 0,
                request.TagWithAccount,
                PurchaseAccount = request.TagWithAccount ? request.PurchaseAccountId ?? 0 : 0,
                SalesAccount = request.TagWithAccount ? request.SalesAccountId ?? 0 : 0,
                StockAccount = request.TagWithAccount ? request.StockAccountId ?? 0 : 0,
                UserId = userId,
                request.IsActive,
                request.SecName
            });
        }

        public async Task<int> DeleteAsync(int id, int userId)
        {
            const string sql = """
            UPDATE dbo.PRGRPMAS
            SET DELFLG = 1,
                ACTIVE = 0,
                LOGCD = @UserId,
                UTIME = GETDATE()
            WHERE PRGRPID = @Id
              AND ISNULL(DELFLG, 0) = 0;
            """;

            return await _db.ExecuteAsync(sql, new { Id = id, UserId = userId });
        }

        public async Task<bool> GroupNameExistsAsync(
            string groupName,
            int excludeId = 0)
        {
            const string sql = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM dbo.PRGRPMAS
                WHERE UPPER(LTRIM(RTRIM(GRPNAME))) =
                      UPPER(LTRIM(RTRIM(@GroupName)))
                  AND PRGRPID <> @ExcludeId
                  AND ISNULL(DELFLG, 0) = 0
            )
            THEN 1 ELSE 0 END;
            """;

            return await _db.QueryFirstOrDefaultAsync<int>(sql, new
            {
                GroupName = groupName,
                ExcludeId = excludeId
            }) == 1;
        }

        public async Task<bool> GroupCodeExistsAsync(
            int groupCode,
            int excludeId = 0)
        {
            const string sql = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM dbo.PRGRPMAS
                WHERE GRPCODE = @GroupCode
                  AND PRGRPID <> @ExcludeId
                  AND ISNULL(DELFLG, 0) = 0
            )
            THEN 1 ELSE 0 END;
            """;

            return await _db.QueryFirstOrDefaultAsync<int>(sql, new
            {
                GroupCode = groupCode,
                ExcludeId = excludeId
            }) == 1;
        }


        public async Task<int?> GetGroupByPurAcc(int accountId)
        {
            var qry = "SELECT ID FROM ACCMAS WHERE ISNULL(DELFLG,0)=0 AND ACTIVE=1 AND ID =@AccountId";  //8;
            return await _db.QueryFirstOrDefaultAsync<int>(qry, new { AccountId = accountId });
        }


        public async Task<int?> GetGroupBySaleAcc(int accountId)
        {
            var qry = "SELECT ID FROM ACCMAS WHERE ISNULL(DELFLG,0)=0 AND ACTIVE=1 AND ID =@AccountId";  //8;
            return await _db.QueryFirstOrDefaultAsync<int>(qry, new { AccountId = accountId });
        }


        public async Task<int?> GetGroupByStockAcc(int accountId)
        {
            var qry = "SELECT ID FROM ACCMAS WHERE ISNULL(DELFLG,0)=0 AND ACTIVE=1 AND ID =@AccountId";  //8;
            return await _db.QueryFirstOrDefaultAsync<int>(qry, new { AccountId = accountId });
        }



        public async Task<int?> GetAccountById(int accountId)
        {
            var qry = "SELECT ID FROM ACCMAS WHERE ACTIVE=1 AND ID =@AccountId";  //8;
            return await _db.QueryFirstOrDefaultAsync<int>(qry, new { AccountId = accountId });
        }

        public async Task<IReadOnlyList<ItemGroupResponse>>
            GetPrimaryGroupsAsync()
        {
            var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.PRGRPMAS
            WHERE ISNULL(DELFLG, 0) = 0
              AND ACTIVE = 1
              AND PRIMGRPFLG = 1
            ORDER BY GRPNAME;
            """;

            return await _db.QueryListAsync<ItemGroupResponse>(sql);
        }
    }

}
