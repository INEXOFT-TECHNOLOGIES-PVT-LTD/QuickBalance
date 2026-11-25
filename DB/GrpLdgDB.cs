using System.Data;

namespace QuickBalance.API.DB
{
    public class GrpLdgDB
    {
        private readonly DBConn db;
        public Variable Variables = new Variable();
        public Function Functions = new Function();
        string qry = "";

        public GrpLdgDB(DBConn dbConn)
        {
            db = dbConn;
        }

        public async Task<DataTable> AccGroupSeries(bool Unapproved)
        {
            DataTable dtGrpSeries = new DataTable();
            DataTable dtGrpSub = new DataTable();
            try
            {
                qry = @"SELECT IIF(GROUPID = 30, -6, IIF(GROUPID = 33, -5, IIF(GROUPID = 3, -4, IIF(GROUPID = 36, -3, IIF(GROUPID = 37, -2, IIF(GROUPID = 8, -1, GROUPID)))))) AS ORDGRP,GROUPID AS GRPID1,GROUPNAME AS GRPNAME1 FROM GRPLDG WHERE (GACTIVE='TRUE'" + Functions.APRVLI(true, Unapproved, "GRPLDG") + ") AND MAINGROUP='TRUE' ORDER BY IIF(GROUPID = 30, -6, IIF(GROUPID = 33, -5, IIF(GROUPID = 3, -4, IIF(GROUPID = 36, -3, IIF(GROUPID = 37, -2, IIF(GROUPID = 8, -1, GROUPID))))))";
                dtGrpSeries = await db.GetDatatable(qry);

                qry = @"SELECT GROUPID,GROUPNAME,MAINGROUP,MGROUP,'P' AS STATUS FROM GRPLDG WHERE (GACTIVE='TRUE'" + Functions.APRVLI(true, Unapproved, "GRPLDG") + ") AND MAINGROUP='FALSE'";
                dtGrpSub = await db.GetDatatable(qry);

                bool stat = true;
                bool colExist = false;
                int Loop = 1;
                DataTable dtExists = new DataTable();
                string idCol = "", nameCol = "";
                do
                {
                    stat = false;
                    idCol = "GRPID" + (Loop + 1);
                    nameCol = "GRPNAME" + (Loop + 1);
                    dtExists = new DataTable();
                    foreach (DataRow itemMain in dtGrpSeries.Rows)
                    {
                        foreach (DataRow itemSub in dtGrpSub.Rows)
                        {
                            if (itemSub["STATUS"].ToString() == "P")
                            {
                                if (itemMain["GRPID" + Loop].ToString() == itemSub["MGROUP"].ToString())
                                {
                                    itemSub["STATUS"] = "U";
                                    if (!colExist)
                                    {
                                        colExist = true;
                                        dtGrpSeries.Columns.Add(idCol, typeof(int));
                                        dtGrpSeries.Columns.Add(nameCol, typeof(string));
                                        dtExists = dtGrpSeries.Clone();
                                    }

                                    if (itemMain[idCol].ToString() != "")
                                        dtExists.ImportRow(itemMain);

                                    itemMain[idCol] = Convert.ToInt32(itemSub["GROUPID"]);
                                    itemMain[nameCol] = itemSub["GROUPNAME"].ToString();
                                }
                                else
                                {
                                    stat = true;
                                }
                            }
                        }
                    }

                    if (dtExists.Rows.Count > 0)
                        dtGrpSeries.Merge(dtExists, true, MissingSchemaAction.Ignore);

                    colExist = false;
                    Loop++;

                } while (stat);

                if (dtGrpSeries.Rows.Count > 0)
                {
                    string Filter = "ORDGRP";
                    for (int i = 1; i <= (dtGrpSeries.Columns.Count - 1) / 2; i++)
                    {
                        Filter += (Filter == "" ? "" : ",") + "GRPID" + i;
                    }

                    DataTable dtOut = null;
                    dtGrpSeries.DefaultView.Sort = Filter;
                    dtOut = dtGrpSeries.DefaultView.ToTable();
                    return dtOut;
                }
            }
            catch { }
            return dtGrpSeries;
        }

        public async Task<DataTable> GetAll()
        {
            qry = "SELECT * FROM GRPLDG ORDER BY GROUPNAME";
            return await db.GetDatatable(qry);
        }
    }
}
