using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Net;
using System.Net.Mail;

namespace TradeWeb.Repo.Interface
{
    public interface IUtilityCommon
    {

        string EsignConnectionString(bool IsCommexES, string StrDt);
        DataView MergeDataTablesOnKey(DataView ObjView1, DataView ObjView2, string Sort);
        DataView MergeViews(dynamic ObjDataViewCollection, string Sort);
        bool InvalidDateCheck(dynamic txtobj);
        void SendEmail(MailMessage ObjMessage);
        string GetApplicationMode();
        DateTime GetDateTimeFromTimeString(string TimeString);
        string ParseDate(string Date);
        void prTempFOBill(SqlConnection ObjConnectionTmp);
        void prTempFOBill1(SqlConnection ObjConnectionTmp);
        DataSet fnForBill(string userId, string exchSeg, string StrFromDt, SqlConnection ObjConnectionTmp);
        string GetPaddedString(string StringToPad, int TotalExpectedLength);
        string GetLeftPaddedString(string StringToPad, int TotalExpectedLength);
        string GetFormattedDate(string Date);
        string dtos(string dDate);
        DateTime stod(string Date);
        string mfnFormatCurrency(Object objAmount, int intDecimal);
        string GetHtmlFromUrl(string url);
        HttpWebRequest GenerateHttpWebRequest(string UriString);
        decimal mfnRoundoffCashbill(string strClient, string strRefDt, decimal dblBillamount, string strExchange);
        string Encrypt(string strenc);
        string Decrypt(string strenc);
        bool mfnGetSysSplFeature(string strKeyCode);
        string GetSysParmSt(string strParmcd, string strTableName);
        string GetWebParameter(string strParmcd);
        string prAcWhere_DPID();
        string fnFireQueryTradeWeb(string strTable, string strSelect, string strParam1, string strParam2, bool strInt);
        DataSet OpenDataSet(string strFireQry);
        DataTable OpenDataTable(string strFireQry);
        void ExecuteSQL(string Str_Qry);
        //void ExecuteSQL(string Str_Qry, bool blnConnClose, int commandTimeout);
        string ColourScheme(string ColourFor, string ColourBack);
        string mfngetDemopass();
        string mfnGetDemoMonthpwd();
        void PrVisit(string datafield, string ClientId);
        string GetCommexConnectionNew(string Commex);
    }
}
