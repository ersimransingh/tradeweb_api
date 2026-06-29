using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using TradeWeb.API.Data;

namespace TradeWeb.API.Repository
{
    public class CommonRepository
    {
        public static DataSet ExecuteSP(string _spName)
        {
            DataSet ds = new DataSet();
            try
            {
                using (var db = new DataContext())
                {
                    SqlConnection currsqlConnection = new SqlConnection((db.Database.GetDbConnection()).ConnectionString);
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandText = _spName;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Connection = currsqlConnection;
                    SqlDataAdapter da = new SqlDataAdapter(_spName, currsqlConnection);
                    da.Fill(ds);
                    return ds;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in executing stored procedure {0}\n{1}", _spName, ex.ToString());
                return ds;
            }
        }

        public static DataSet ExecuteSP(string _spName, List<SqlParameter> _parameters)
        {
            DataSet ds = new DataSet();
            try
            {
                using (var db = new DataContext())
                {
                    SqlConnection currsqlConnection = new SqlConnection((db.Database.GetDbConnection()).ConnectionString);
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandText = _spName;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Connection = currsqlConnection;

                    foreach (SqlParameter sqlParam in _parameters)
                    {
                        cmd.Parameters.Add(sqlParam);
                    }
                    SqlDataAdapter da = new SqlDataAdapter();
                    da.SelectCommand = cmd;
                    da.Fill(ds);
                    return ds;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in executing stored procedure {0}\n{1}", _spName, ex.ToString());
                return ds;
            }
        }

        public static void ExecuteStoredProcedureVoid(string _spName, List<SqlParameter> _parameters)
        {
            try
            {
                using (var db = new DataContext())
                {
                    SqlConnection currsqlConnection = new SqlConnection((db.Database.GetDbConnection()).ConnectionString);
                    currsqlConnection.Open();
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandText = _spName;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Connection = currsqlConnection;
                    foreach (SqlParameter sqlParam in _parameters)
                    {
                        cmd.Parameters.Add(sqlParam);
                    }
                    cmd.ExecuteNonQuery();
                    currsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in executing stored procedure {0}\n{1}", _spName, ex.ToString());
                throw ex;
            }
        }

        // TODO : This method used with appsetting connection
        public static dynamic ExecuteQuery(string Query_)
        {
            DataSet ds = new DataSet();
            try
            {
                using (var db = new DataContext())
                {
                    SqlConnection currsqlConnection = new SqlConnection((db.Database.GetDbConnection()).ConnectionString);
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandText = Query_;
                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = currsqlConnection;
                    SqlDataAdapter da = new SqlDataAdapter(Query_, currsqlConnection);
                    da.Fill(ds);
                    return ds;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in executing query {0}\n{1}", Query_, ex.ToString());
                throw ex;
            }
        }

        #region Methods for external Connection class used.
        // TODO : This method used with connection class connection For getting dataset.
        public static dynamic FillDataset(string Query_)
        {
            DataSet ds = new DataSet();
            try
            {
                Connection con = new Connection();
                try
                {
                    con.OpenConection();
                    var data = con.GetDataInDataset(Query_);
                    con.CloseConnection();
                    return data;
                }
                catch (Exception e)
                {
                    con.DisposeConnection();
                    throw e;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in executing query {0}\n{1}", Query_, ex.ToString());
                throw ex;
            }
        }

        public static dynamic FillDataTable(string Query_)
        {
            DataSet ds = new DataSet();
            try
            {
                Connection con = new Connection();
                try
                {
                    con.OpenConection();
                    var data = con.GetDataInDataTable(Query_);
                    con.CloseConnection();
                    return data;
                }
                catch (Exception e)
                {
                    con.DisposeConnection();
                    throw e;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in executing query {0}\n{1}", Query_, ex.ToString());
                throw ex;
            }
        }

        // TODO : This method used with connection class connection For getting dataset.
        public static dynamic ExecuteQueries(string Query_)
        {
            DataSet ds = new DataSet();
            try
            {
                Connection con = new Connection();
                try
                {
                    con.OpenConection();
                    con.ExecuteQueries(Query_);
                    con.CloseConnection();
                    return "Success";
                }
                catch (Exception e)
                {
                    con.DisposeConnection();
                    throw e;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in executing query {0}\n{1}", Query_, ex.ToString());
                throw ex;
            }
        }


        public static dynamic OpenDataSetTmp(string Query_)
        {
            DataSet ds = new DataSet();
            try
            {
                Connection con = new Connection();
                try
                {
                    //con.OpenConection();
                    var data = con.OpenDataSetTmp(Query_);
                    //con.CloseConnection();
                    return data;
                }
                catch (Exception e)
                {
                    con.DisposeConnection();
                    throw e;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in executing query {0}\n{1}", Query_, ex.ToString());
                throw ex;
            }
        }
        #endregion
    }
}
