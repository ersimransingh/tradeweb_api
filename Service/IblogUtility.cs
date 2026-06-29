using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.Data.SqlClient;
using TradeWeb.API.Data;
using TradeWeb.API.Logs;

namespace TradeWeb.API.Service
{
    public class IblogUtility
    {
        public IblogUtility()
        {
        }

        public dynamic SelectSqlExecuteScalar(string strStatement)
        {
            var result = new object();
            try
            {
                SqlConnection con;
                using (var db = new DataContext())
                {
                    con = new SqlConnection((db.Database.GetDbConnection()).ConnectionString);
                }
                con.Open();
                using (SqlCommand MyCmd = new SqlCommand(strStatement, con))
                {
                    LogInfo.WriteErrorLog("Sql Query Going To Execute is :" + strStatement);
                    MyCmd.CommandType = CommandType.Text;
                    result = MyCmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                LogInfo.WriteErrorLog("Exception is :: " + ex.GetBaseException());
                throw ex;
            }
            return result;
        }

        public dynamic Execute_QueryParam(string strFireQry, SqlParameter[] sqlPrm)
        {
            SqlConnection con = new SqlConnection();
            try
            {
                using (var db = new DataContext())
                {
                    con = new SqlConnection((db.Database.GetDbConnection()).ConnectionString);
                }
                if (con.State != ConnectionState.Open)
                    con.Open();
                using (SqlCommand cmd = new SqlCommand(strFireQry, con))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.Clear();
                    foreach (var param in sqlPrm)
                    {
                        cmd.Parameters.Add(param);
                    }
                    var rtnVal = cmd.ExecuteScalar();
                    con.Close();
                    return rtnVal;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }



    }
}
