using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Net;
using System.Net.Mail;

namespace TradeWeb.Repo.Interface
{
    public interface IUtilityCommon
    {
        DateTime stod(string Date);
        string Encrypt(string strenc);
        string Decrypt(string strenc);
        bool mfnGetSysSplFeature(string strKeyCode);
        string GetSysParmSt(string strParmcd, string strTableName);
        string GetWebParameter(string strParmcd);
        string fnFireQueryTradeWeb(string strTable, string strSelect, string strParam1, string strParam2, bool strInt);
        DataSet OpenDataSet(string strFireQry);
        DataTable OpenDataTable(string strFireQry);
        void ExecuteSQL(string Str_Qry);
        string mfnGetDemoMonthpwd();
    }
}
