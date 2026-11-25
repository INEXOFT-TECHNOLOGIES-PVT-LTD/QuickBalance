using Microsoft.Data.SqlClient;
using System.Data;

namespace QuickBalance.API.DB
{
    public class DbModelDB
    {
        private readonly DBConn db;

        public Variable Variables = new Variable();
        public Function Functions = new Function();

        string qry = "";

        public DbModelDB(DBConn dbConn)
        {
            db = dbConn;
        }


        // =========================================================
        // INSERT / COPY TABLE
        // =========================================================

        public async Task<long> insertTable(
            string source,
            string dest,
            string tableName,
            string fieldname)
        {
            long countEnd = 0;

            try
            {
                // -------------------------------------------------
                // SOURCE CONNECTION
                // -------------------------------------------------

                using (SqlConnection sourceConnection =
                    new SqlConnection(source))
                {
                    await sourceConnection.OpenAsync();


                    // -------------------------------------------------
                    // GET SOURCE RECORD COUNT
                    // -------------------------------------------------

                    using (SqlCommand commandRowCount =
                        new SqlCommand(
                            "SELECT COUNT(*) FROM [" + tableName + "]",
                            sourceConnection))
                    {
                        object? result =
                            await commandRowCount.ExecuteScalarAsync();

                        long countStart =
                            result == null || result == DBNull.Value
                                ? 0
                                : Convert.ToInt64(result);
                    }


                    // -------------------------------------------------
                    // GET SOURCE DATA
                    // -------------------------------------------------

                    DataTable dt = new DataTable();

                    string destinationColumns =
                        await GetDestCols(
                            tableName,
                            source,
                            dest);


                    string selectQuery =
                        "SELECT " +
                        destinationColumns +
                        " FROM [" +
                        tableName +
                        "]";


                    // -------------------------------------------------
                    // SqlDataAdapter.Fill() DOES NOT HAVE
                    // FillAsync(), so this part remains synchronous.
                    // -------------------------------------------------

                    using (SqlDataAdapter dap =
                        new SqlDataAdapter(
                            selectQuery,
                            sourceConnection))
                    {
                        dap.Fill(dt);
                    }


                    // -------------------------------------------------
                    // KEEP IDENTITY
                    // -------------------------------------------------

                    if (fieldname == "KeepIdentity")
                    {
                        using (SqlBulkCopy bulkCopy =
                            new SqlBulkCopy(
                                dest,
                                SqlBulkCopyOptions.KeepIdentity))
                        {
                            bulkCopy.DestinationTableName =
                                "[" + tableName + "]";

                            await bulkCopy.WriteToServerAsync(dt);
                        }
                    }


                    // -------------------------------------------------
                    // NORMAL COPY
                    // -------------------------------------------------

                    else
                    {
                        using (SqlConnection destinationConnection =
                            new SqlConnection(dest))
                        {
                            await destinationConnection.OpenAsync();


                            using (SqlBulkCopy bulkCopy =
                                new SqlBulkCopy(destinationConnection))
                            {
                                bulkCopy.DestinationTableName =
                                    "[" + tableName + "]";

                                await bulkCopy.WriteToServerAsync(dt);
                            }


                            // -----------------------------------------
                            // GET DESTINATION RECORD COUNT
                            // -----------------------------------------

                            using (SqlCommand destinationCountCommand =
                                new SqlCommand(
                                    "SELECT COUNT(*) FROM [" +
                                    tableName +
                                    "]",
                                    destinationConnection))
                            {
                                object? result =
                                    await destinationCountCommand
                                        .ExecuteScalarAsync();

                                countEnd =
                                    result == null ||
                                    result == DBNull.Value
                                        ? 0
                                        : Convert.ToInt64(result);
                            }
                        }
                    }
                }


                return countEnd;
            }
            catch (Exception ex)
            {
                // You can add logging here later.
                throw;
            }
        }


        // =========================================================
        // GET DESTINATION COLUMNS
        // =========================================================

        public async Task<string> GetDestCols(
            string Table,
            string Source,
            string Dest)
        {
            string Coloums = "";

            try
            {
                // -------------------------------------------------
                // SOURCE COLUMNS
                // -------------------------------------------------

                using (SqlConnection Conn =
                    new SqlConnection(Source))
                {
                    await Conn.OpenAsync();

                    DataTable dtCl = new DataTable();


                    string sourceQuery =
                        @"SELECT COLUMN_NAME, ORDINAL_POSITION
                          FROM INFORMATION_SCHEMA.COLUMNS
                          WHERE TABLE_NAME = @Table
                          ORDER BY ORDINAL_POSITION";


                    using (SqlCommand sourceCommand =
                        new SqlCommand(
                            sourceQuery,
                            Conn))
                    {
                        sourceCommand.Parameters.AddWithValue(
                            "@Table",
                            Table);


                        // -------------------------------------------------
                        // SqlDataAdapter has no FillAsync()
                        // -------------------------------------------------

                        using (SqlDataAdapter sda =
                            new SqlDataAdapter(sourceCommand))
                        {
                            sda.Fill(dtCl);
                        }
                    }


                    // -------------------------------------------------
                    // DESTINATION COLUMNS
                    // -------------------------------------------------

                    using (SqlConnection Conn_dest =
                        new SqlConnection(Dest))
                    {
                        await Conn_dest.OpenAsync();

                        DataTable dtCl_dest =
                            new DataTable();


                        string destinationQuery =
                            @"SELECT COLUMN_NAME, ORDINAL_POSITION
                              FROM INFORMATION_SCHEMA.COLUMNS
                              WHERE TABLE_NAME = @Table
                              ORDER BY ORDINAL_POSITION";


                        using (SqlCommand destinationCommand =
                            new SqlCommand(
                                destinationQuery,
                                Conn_dest))
                        {
                            destinationCommand.Parameters.AddWithValue(
                                "@Table",
                                Table);


                            using (SqlDataAdapter sda_dest =
                                new SqlDataAdapter(
                                    destinationCommand))
                            {
                                sda_dest.Fill(dtCl_dest);
                            }
                        }


                        // -------------------------------------------------
                        // COMPARE COLUMN COUNT
                        // -------------------------------------------------

                        if (dtCl.Rows.Count !=
                            dtCl_dest.Rows.Count)
                        {
                            if (dtCl.Rows.Count > 0 &&
                                dtCl_dest.Rows.Count > 0)
                            {
                                foreach (
                                    DataRow item
                                    in dtCl_dest.Rows)
                                {
                                    Coloums +=
                                        (Coloums == ""
                                            ? ""
                                            : ", ")
                                        + "["
                                        + item["COLUMN_NAME"]
                                            .ToString()
                                        + "]";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // You can add logging here later.
                throw;
            }


            // -------------------------------------------------
            // IF NO SPECIAL COLUMN LIST
            // USE *
            // -------------------------------------------------

            return Coloums == ""
                ? "*"
                : Coloums;
        }
    }
}