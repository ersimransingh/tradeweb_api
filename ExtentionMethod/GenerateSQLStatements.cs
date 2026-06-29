using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace TradeWeb.API.ExtentionMethod
{
    public class GenerateSQLStatements
    {
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Function name : BuildAllFieldsSQL
        // Created on : 09APR2015
        // Cretaed by: Sukesh
        // Description : Returns a string containing all the fields in the table
        //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static string BuildAllFieldsSQL(DataTable table)
        {
            string sql = "";
            foreach (DataColumn column in table.Columns)
            {
                if (sql.Length > 0)
                    sql += ", ";
                sql += column.ColumnName;
            }
            return sql;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Function name : BuildInsertSQL
        // Created on : 09APR2015
        // Cretaed by: Sukesh
        // Description : Build insert Sql statements
        //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////     
        public static string BuildInsertSQL(DataTable table)
        {
            StringBuilder sql = new StringBuilder("INSERT INTO " + table.TableName + " (");
            StringBuilder values = new StringBuilder("VALUES (");
            bool bFirst = true;
            bool bIdentity = false;
            string identityType = null;

            foreach (DataColumn column in table.Columns)
            {
                if (column.AutoIncrement)
                {
                    bIdentity = true;

                    switch (column.DataType.Name)
                    {
                        case "Int16":
                            identityType = "smallint";
                            break;
                        case "SByte":
                            identityType = "tinyint";
                            break;
                        case "Int64":
                            identityType = "bigint";
                            break;
                        case "Decimal":
                            identityType = "decimal";
                            break;
                        default:
                            identityType = "int";
                            break;
                    }
                }
                else
                {
                    if (bFirst)
                        bFirst = false;
                    else
                    {
                        sql.Append(", ");
                        values.Append(", ");
                    }

                    sql.Append(column.ColumnName);
                    values.Append("@");
                    values.Append(column.ColumnName);
                }
            }
            sql.Append(") ");
            sql.Append(values.ToString());
            sql.Append(")");

            if (bIdentity)
            {
                sql.Append("; SELECT CAST(scope_identity() AS ");
                sql.Append(identityType);
                sql.Append(")");
            }

            return sql.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Function name : InsertParameter
        // Created on : 09APR2015
        // Cretaed by: Sukesh
        // Description : Insert Sql parameters
        //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////     
        public static void InsertParameter(SqlCommand command,
                                             string parameterName,
                                              string sourceColumn,
                                              object value)
        {
            SqlParameter parameter = new SqlParameter(parameterName, value);
            parameter.Direction = ParameterDirection.Input;
            parameter.ParameterName = parameterName;
            parameter.SourceColumn = sourceColumn;
            parameter.SourceVersion = DataRowVersion.Current;
            command.Parameters.Add(parameter);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Function name : CreateInsertCommand
        // Created on : 09APR2015
        // Cretaed by: Sukesh
        // Description : Creates a SqlCommand for inserting a DataRow
        //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////      
        public static SqlCommand CreateInsertCommand(DataRow row)
        {
            DataTable table = row.Table;
            string sql = BuildInsertSQL(table);
            SqlCommand command = new SqlCommand(sql);
            command.CommandType = System.Data.CommandType.Text;

            foreach (DataColumn column in table.Columns)
            {
                if (!column.AutoIncrement)
                {
                    string parameterName = "@" + column.ColumnName;
                    InsertParameter(command, parameterName, column.ColumnName, row[column.ColumnName]);
                }
            }
            return command;
        }
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Function name : InsertDataRow
        // Created on : 09APR2015
        // Cretaed by: Sukesh
        // Description : Inserts the DataRow for the connection, returning the identity
        //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////       
        public static object InsertDataRow(DataRow row, string connectionString)
        {
            SqlCommand command = CreateInsertCommand(row);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                command.Connection = connection;
                command.CommandType = System.Data.CommandType.Text;
                connection.Open();
                return command.ExecuteScalar();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Function name : SqlUpdate
        // Created on : 09APR2015
        // Cretaed by: Sukesh
        // Description : Sql Update Statement
        //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static string SqlUpdate(string table, Dictionary<string, object> values, string where)
        {
            var equals = new List<string>();
            var parameters = new List<SqlParameter>();

            var i = 0;

            foreach (var item in values)
            {
                //var pn = "@sp" + i.ToString();
                if (item.Value.ToString().Trim().Contains("GetDate()"))
                {
                    equals.Add(string.Format("{0}={1}", item.Key, item.Value));
                }
                else
                    if (item.Value.ToString().Contains("@Attach") || item.Value.ToString().Contains("@UserPhoto"))
                {
                    equals.Add(string.Format("{0}={1}", item.Key, item.Value));
                    // equals.Add(string.Format("{0}={1}", item.Key, item.Value));
                }
                else
                {
                    int Value;
                    bool isNumeric = int.TryParse(item.Value.ToString(), out Value);
                    if (isNumeric)
                    {
                        equals.Add(string.Format("{0}={1}", item.Key, Value));
                    }
                    else
                    {
                        equals.Add(string.Format("{0}='{1}'", item.Key, item.Value));
                    }
                }

                // parameters.Add(new SqlParameter(pn, item.Value));

                i++;
            }

            string command = string.Format("update {0} set {1} where {2}", table, string.Join(", ", equals.ToArray()), where);

            return command;
            // var sqlcommand = new SqlCommand(command);

            //  sqlcommand.Parameters.AddRange(parameters.ToArray());

            // sqlcommand.ExecuteNonQuery();
        }
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Function name : SqlInsert
        // Created on : 09APR2015
        // Cretaed by: Sukesh
        // Description : Sql Insert Statement
        //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static string SqlInsert(string table, Dictionary<string, object> values)
        {
            var equals = new List<string>();
            var parameters = new List<SqlParameter>();
            StringBuilder sql = new StringBuilder("INSERT INTO " + table + " (");
            StringBuilder sqlvalues = new StringBuilder("VALUES (");
            int i = 0;
            foreach (var item in values)
            {

                sql.Append(item.Key);
                if (item.Value.ToString() == string.Empty)
                {
                    sqlvalues.Append("'");
                    sqlvalues.Append("");
                    sqlvalues.Append("'");
                }
                else
                {
                    if (item.Value.ToString() == "GetDate()")
                    {
                        sqlvalues.Append("GetDate()");
                    }
                    else if (item.Value.ToString().Contains("@Attach") || item.Value.ToString().Contains("@UserPhoto"))
                    {
                        if (item.Value.ToString().Substring(1).ToUpper() == item.Key.ToString().ToUpper() || item.Key.ToUpper().ToString().Contains("ATTACHMENT") || (item.Key.ToString().ToUpper().Contains("DOCUMENT")) || (item.Key.ToString().ToUpper().Contains("IMAGE")))
                        {
                            sqlvalues.Append(item.Value);
                        }
                        else
                        {
                            sqlvalues.Append("'");
                            sqlvalues.Append(item.Value);
                            sqlvalues.Append("', ");
                        }
                    }

                    else
                    {
                        sqlvalues.Append("'");
                        sqlvalues.Append(item.Value);
                        sqlvalues.Append("'");
                    }
                }
                if (i != values.Count - 1)
                {
                    sql.Append(", ");
                    sqlvalues.Append(", ");
                }
                i++;
            }
            sql.Append(") ");
            sql.Append(sqlvalues.ToString());
            sql.Append(")");
            return sql.ToString();

        }

        /// <summary>
        /// Added By Sai Siva Prasad on 08OCT2020
        /// </summary>
        /// <param name="table"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static string SqlInsert(string table, Dictionary<string, object> values, string returnColumn)
        {
            var equals = new List<string>();
            var parameters = new List<SqlParameter>();
            StringBuilder sql = new StringBuilder("INSERT INTO " + table + " (");
            StringBuilder sqlvalues = new StringBuilder("VALUES (");
            int i = 0;
            foreach (var item in values)
            {

                sql.Append(item.Key);
                if (item.Value.ToString() == string.Empty)
                {
                    sqlvalues.Append("'");
                    sqlvalues.Append("");
                    sqlvalues.Append("'");
                }
                else
                {
                    if (item.Value.ToString() == "GetDate()")
                    {
                        sqlvalues.Append("GetDate()");
                    }
                    else if (item.Value.ToString().Contains("@Attach") || item.Value.ToString().Contains("@UserPhoto"))
                    {
                        if (item.Value.ToString().Substring(1).ToUpper() == item.Key.ToString().ToUpper() || item.Key.ToUpper().ToString().Contains("ATTACHMENT") || (item.Key.ToString().ToUpper().Contains("DOCUMENT")) || (item.Key.ToString().ToUpper().Contains("IMAGE")))
                        {
                            sqlvalues.Append(item.Value);
                        }
                        else
                        {
                            sqlvalues.Append("'");
                            sqlvalues.Append(item.Value);
                            sqlvalues.Append("', ");
                        }
                    }

                    else
                    {
                        sqlvalues.Append("'");
                        sqlvalues.Append(item.Value);
                        sqlvalues.Append("'");
                    }
                }
                if (i != values.Count - 1)
                {
                    sql.Append(", ");
                    sqlvalues.Append(", ");
                }
                i++;
            }
            sql.Append(") ");
            sql.Append(" Output Inserted." + returnColumn + " ");
            sql.Append(sqlvalues.ToString());
            sql.Append(")");
            return sql.ToString();

        }

        /// <summary>
        ///  added by Gouthami on Dec172016
        /// </summary>
        /// <param name="table"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static string PrepareInsertStat(string table, Dictionary<string, object> values)
        {
            var equals = new List<string>();
            StringBuilder sql = new StringBuilder("INSERT INTO " + table + " (");
            StringBuilder sqlvalues = new StringBuilder("VALUES (");
            string[] colNames = null;
            foreach (var item in values)
            {
                colNames = item.Key.Split('~');
                sql.Append(colNames[0] + ", ");
                /*if (item.Value.ToString() == string.Empty)
                {
                    sqlvalues.Append("'");
                    sqlvalues.Append("");
                    sqlvalues.Append("', ");
                }
                else
                {*/
                if (item.Value.ToString().ToUpper() == "SYSDATE")
                {
                    //sqlvalues.Append("SYSDATE, ");
                    sqlvalues.Append("SYSDATE, ");
                }
                else if (item.Value.ToString().Contains("@"))
                {
                    if (item.Value.ToString().Substring(1).ToUpper() == item.Key.ToString().ToUpper())
                    {
                        sqlvalues.Append(item.Value + ", ");
                    }
                    else
                    {
                        //sqlvalues.Append("'");
                        sqlvalues.Append(item.Value);
                        //sqlvalues.Append("', ");
                    }
                }

                //Added DP_UNIQUE_GEN.NEXTVAL by tejaswi on 14th july 2016 at 11:30AM

                else if (item.Value.ToString().ToUpper() == "DP_UNIQUE_GEN.NEXTVAL")
                {
                    //sqlvalues.Append("DP_UNIQUE_GEN.NEXTVAL, ");
                    sqlvalues.Append("DP_UNIQUE_GEN.NEXTVAL, ");

                }
                //added CLNT_DOC_SEQ.NEXTVAL by tejaswi on 1st Dec 2016, 
                else if (item.Value.ToString().ToUpper() == "CLNT_DOC_SEQ.NEXTVAL")
                {
                    //sqlvalues.Append("DP_UNIQUE_GEN.NEXTVAL, ");
                    sqlvalues.Append("CLNT_DOC_SEQ.NEXTVAL, ");

                }
                // ended by tejaswi on 1st dec 2016
                else
                {
                    if (colNames[1] != null && (colNames[1].Contains("VARCHAR") || colNames[1].Contains("DATE")))
                    {
                        sqlvalues.Append("'" + item.Value + "', ");
                    }
                    else if (colNames[1] != null && (colNames[1].Contains("NUMBER")))
                    {
                        if (item.Value == null || item.Value.ToString() == "")
                        {
                            sqlvalues.Append("NULL, ");
                        }
                        else
                        {
                            sqlvalues.Append(item.Value + ",");
                        }

                    }

                    //else
                    //{
                    //    sqlvalues.Append(item.Value + ", ");
                    //}
                }
            }//for each
            //}
            sql.Length--;
            sql.Length--;
            sql.Append(") ");
            //sqlvalues.Length--;
            //sqlvalues.Length--;
            sql.Append(sqlvalues.ToString().Substring(0, sqlvalues.Length));
            sql.Append(")");
            return sql.ToString();
        }

        /// <summary>
        /// added by Gouthami on Dec172016
        /// </summary>
        /// <param name="table"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static string PrepareUpdateStat(string table, Dictionary<string, object> values, string where)
        {
            var equals = new List<string>();
            string[] colNames = null;
            foreach (var item in values)
            {
                colNames = item.Key.Split('~');
                if (item.Value.ToString().Trim().Contains("SYSDATE"))
                {
                    equals.Add(string.Format("{0}={1}", colNames[0], item.Value));
                }
                else
                {
                    if (colNames[1] != null && (colNames[1].Contains("VARCHAR") || colNames[1].Contains("DATE")))
                    {
                        equals.Add(string.Format("{0}='{1}'", colNames[0], item.Value));
                    }
                    else if (colNames[1] != null && (colNames[1].Contains("NUMBER")))
                    {
                        if (item.Value == null || item.Value.ToString() == "")
                        {
                            equals.Add(string.Format("{0}={1}", colNames[0], "NULL"));
                        }
                        else
                        {
                            equals.Add(string.Format("{0}={1}", colNames[0], item.Value));
                        }

                    }
                    else
                    {
                        equals.Add(string.Format("{0}={1}", colNames[0], item.Value));
                    }
                }
            }
            string command = string.Format("UPDATE {0} set {1} where {2}", table, string.Join(", ", equals.ToArray()), where);
            return command;
        }

        public static string ImageUpdate(string table, Dictionary<string, object> values, string where)//ADDED BY GOUTHAMI ON DEC20-2016
        {
            var equals = new List<string>();
            var parameters = new List<SqlParameter>();

            var i = 0;

            foreach (var item in values)
            {
                //var pn = "@sp" + i.ToString();
                if (item.Value.ToString().Trim().Contains("GetDate()"))
                {
                    equals.Add(string.Format("{0}={1}", item.Key, item.Value));
                }
                else
                    if (item.Value.ToString().Contains("@Data"))
                {
                    equals.Add(string.Format("{0}={1}", item.Key, item.Value));
                    // equals.Add(string.Format("{0}={1}", item.Key, item.Value));
                }
                else
                {
                    int Value;
                    bool isNumeric = int.TryParse(item.Value.ToString(), out Value);
                    if (isNumeric)
                    {
                        equals.Add(string.Format("{0}={1}", item.Key, Value));
                    }
                    else
                    {
                        equals.Add(string.Format("{0}='{1}'", item.Key, item.Value));
                    }
                }

                // parameters.Add(new SqlParameter(pn, item.Value));

                i++;
            }

            string command = string.Format("update {0} set {1} where {2}", table, string.Join(", ", equals.ToArray()), where);

            return command;
            // var sqlcommand = new SqlCommand(command);

            //  sqlcommand.Parameters.AddRange(parameters.ToArray());

            // sqlcommand.ExecuteNonQuery();
        }

    }
}
