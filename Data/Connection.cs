using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data;

namespace TradeWeb.API.Data
{
    public class Connection
    {
        //string ConnectionString = @"Data SoUrce=61.12.61.163,3324;Initial Catalog=TplusCrmUAT;User ID=sa;Password=Bpw@Acer$074;Max Pool Size=100;Min Pool Size=1;Connect Timeout=2000;MultipleActiveResultSets=True;";

        //string ConnectionString = "";
        SqlConnection con;

        public Connection()
        {
            using (var db = new DataContext())
            {
                con = new SqlConnection((db.Database.GetDbConnection()).ConnectionString);
            }
        }

        public void OpenConection()
        {
            // con = new SqlConnection(ConnectionString);
            con.Open();
        }
        public void CloseConnection()
        {
            con.Close();
        }
        public void DisposeConnection()
        {
            con.Dispose();
        }
        public void ExecuteQueries(string Query_)
        {
            SqlCommand cmd = new SqlCommand(Query_, con);
            cmd.ExecuteNonQuery();
        }
        public SqlDataReader DataReader(string Query_)
        {
            SqlCommand cmd = new SqlCommand(Query_, con);
            SqlDataReader dr = cmd.ExecuteReader();
            return dr;
        }
        public object GetDataInDataset(string Query_)
        {
            //SqlDataAdapter dr = new SqlDataAdapter(Query_, ConnectionString);
            SqlDataAdapter dr = new SqlDataAdapter(Query_, con);
            dr.SelectCommand.CommandTimeout = 5000;
            DataSet ds = new DataSet();
            dr.Fill(ds);
            object dataum = ds;//.Tables[0];
            return dataum;
        }

        public object GetDataInDataTable(string Query_)
        {
            //SqlDataAdapter dr = new SqlDataAdapter(Query_, ConnectionString);
            SqlDataAdapter dr = new SqlDataAdapter(Query_, con);
            DataTable ds = new DataTable();
            dr.Fill(ds);
            //DataTable dt=
            return ds;
        }

        public DataSet ReturnDataSet(string Query_)
        {
            try
            {
                SqlDataAdapter dr = new SqlDataAdapter(Query_, con);
                DataSet ds = new DataSet();
                dr.Fill(ds);
                return ds;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public string ExecuteQuery(string Query_)
        {
            try
            {
                SqlCommand cmd = new SqlCommand(Query_, con);
                if (con.State == ConnectionState.Closed)
                    con.Open();
                var retVal = cmd.ExecuteScalar();
                con.Close();
                return retVal.ToString();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public DataTable ReturnNewDataTable(string Query_)
        {
            try
            {
                SqlDataAdapter dr = new SqlDataAdapter(Query_, con);
                DataTable ds = new DataTable();
                dr.Fill(ds);
                return ds;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public SqlConnection GetDataSource()
        {
            SqlConnection sc = con;
            return sc;
        }

        public object OpenDataSetTmp(string Query_)
        {
            using (SqlCommand objSqlCommand = new SqlCommand(Query_, con))
            {
                objSqlCommand.Connection = con;
                objSqlCommand.CommandText = Query_;
                objSqlCommand.CommandTimeout = 5000;
                DataSet MyDataSet = new DataSet();
                using (SqlDataAdapter MyAdapter = new SqlDataAdapter(objSqlCommand))
                {
                    MyAdapter.Fill(MyDataSet);
                    MyAdapter.Dispose();
                    return MyDataSet;
                }
            }

        }
    }

}
