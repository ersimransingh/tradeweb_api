using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using TradeWeb.API.Models;
using TradeWeb.Service;
using static TradeWeb.API.Startup;
using Formatting = Newtonsoft.Json.Formatting;
using TradeWeb.API.QuestPdfServicesClass;
using iTextSharp.text;
using iTextSharp.text.pdf;


namespace TradeWeb.API.Repository
{

    public class TradeWebRepository : ITradeWebRepository
    {
        #region Class level declarations.
        private readonly IConfiguration _configuration;
        private readonly UtilityCommon objUtility;
        private string strsql = "";
        private string strConnecton = "";
        string strToken = string.Empty;
        IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _environment;
        private readonly TokenStore _tokenStore;
        private readonly QuestPdfServiceClass questService;
        private readonly QuestPdfCommonService _questServc;
        private readonly string _connStr;
        public const string UtcDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
        #endregion

        #region Constructor
        public TradeWebRepository(IConfiguration configuration, UtilityCommon objUtility, IHttpContextAccessor httpContextAccessor,
            IWebHostEnvironment environment, TokenStore tokenStore, QuestPdfServiceClass quest, QuestPdfCommonService questServ, ConnectionString connectionString)
        {
            _configuration = configuration;
            this.objUtility = objUtility;
            _httpContextAccessor = httpContextAccessor;
            _environment = environment;
            _tokenStore = tokenStore;
            questService = quest;
            _questServc = questServ;
            _connStr = connectionString.Default;
        }
        #endregion


        #region Method
        public dynamic UserDetails(string userId, string password, bool keyBased, string product, string role)
        {
            string[] userStr = userId.Split("|");
            userId = userStr[0];
            string dpid = "";
            bool blnOffline = Convert.ToInt16(objUtility.fnFireQueryTradeWeb("Information_Schema.Columns", "count(*)", "Table_Name='client_master' and Column_Name", "cm_companycode", true)) > 0;
            if (userStr.Length > 1)
            {
                dpid = userStr[0];
                userId = userStr[1];
            }
            else if (blnOffline)
            {
                dpid = objUtility.GetWebParameter("DPID");
            }
            loginResponseWithDT rtnValue = new loginResponseWithDT();
            rtnValue.message = "";
            string strWebYN = "";
            string strLocked = "";
            bool blnValid = true;

            ////// ***** For ReKYC Checker  ****** added by Hemant on 07-03-2024
            if (objUtility.mfnGetSysSplFeature("RKC") == true)
            {
                string[] checkerUsers = (objUtility.GetWebParameter("RekycChecker")).Split(",");
                for (int i = 0; i < checkerUsers.Length; i++)
                {
                    if (checkerUsers[i].ToUpper() == userId.ToUpper())
                    {
                        string sqlQry = "Select um_user_id ClientCode, um_passwd pwd, um_user_name ClientName, 'checker' UserType from User_master with (NoLock) Where um_user_id = '" + userId + "'  ";
                        var dtAdmin = objUtility.OpenDataTable(sqlQry);
                        if (dtAdmin.Rows.Count > 0)
                        {
                            if (objUtility.Decrypt(dtAdmin.Rows[0]["pwd"].ToString()) == password)
                            {
                                dtAdmin.Columns.Remove("pwd");
                                dtAdmin.AcceptChanges();
                                var addLog = AddLog_Session(userId, strWebYN, "B");
                                if (addLog != null)
                                {
                                    rtnValue.message = addLog;
                                    return rtnValue;
                                }
                                rtnValue.data = dtAdmin;
                                return rtnValue;
                            }
                        }
                        break;
                    }
                }
            }
            /////---- end rekyc logic -----  
            if (_configuration["IsTradeWeb"] == "O" && !blnOffline && (role == "Admin" || role == "Branch" || role == "TradeMobile"))
            {
                try
                {
                    strWebYN = objUtility.fnFireQueryTradeWeb("client_master", "cm_confirmwebyn", "cm_cd", userId.Trim(), true);
                }
                catch (Exception)
                { }
                if (strWebYN == "N")
                {
                    rtnValue.message = "You are not authorized to log in to TradeWeb. Kindly contact your Stock Broker.";
                    return rtnValue;
                }
                if (strWebYN == "L")
                {
                    rtnValue.message = "You have exceeded maximum attempts permitted. User Has Been Locked.";
                    return rtnValue;
                }
            }
            else if (_configuration["IsTradeWeb"] == "E" || blnOffline)
            {
                string strPwdSetting = objUtility.fnFireQueryTradeWeb("webparameter", "sp_sysvalue", "sp_parmcd", "PWDSETTING", true);
                if (strPwdSetting.Trim() != "" && Convert.ToInt16(objUtility.fnFireQueryTradeWeb("Information_Schema.Columns", "count(*)", "Table_Name='client_master' and Column_Name", "cm_locked", true)) > 0)
                {
                    strLocked = objUtility.fnFireQueryTradeWeb("client_master", "cm_locked", "cm_cd", userId.Trim(), true);
                    if (strLocked == "Y")
                    {
                        rtnValue.message = "You have exceeded maximum attempts permitted.User Has Been Locked.";
                        return rtnValue;
                    }
                }
            }
            else if (role == "CrossWeb")
            {
                double dblLockedOut = Conversion.Val(objUtility.GetSysParmSt("PWDLOCKEDATM", ""));
                if (dblLockedOut > 0)
                {
                    string strValue = objUtility.fnFireQueryTradeWeb("client_master", "cm_keepsettlement", "cm_cd", userId.Trim(), true);
                    blnValid = strValue.All(c => "FTMY".Contains(c));
                    if (blnValid)
                    {
                        rtnValue.message = "You have exceeded maximum attempts permitted.User Has Been Locked.";
                        return rtnValue;
                    }
                }
            }
            if (role == "Admin")
            {
                var addLog = AddLog_Session(userId, strWebYN, "C");
                if (addLog != null)
                {
                    rtnValue.message = addLog;
                    return rtnValue;
                }
                string strCompanyWhere = _configuration["IsTradeWeb"] == "C" || _configuration["IsTradeWeb"] == "E" ? " cm_companycode='" + objUtility.mfnReplaceForSQLInjection(dpid.Trim()) + "'" : " isnull(cm_companycode,'')=''";
                if (Convert.ToInt16(objUtility.fnFireQueryTradeWeb("sysobjects", "count(*)", "name", "LoginDetails", true)) > 0)
                {
                    if (objUtility.fnFireQueryTradeWeb("LoginDetails", "lg_cmcd", strCompanyWhere.Replace("cm_companycode", "lg_companyCode") + " and lg_cmcd", userId, true) == "")
                    {
                        strsql = " Insert Into LoginDetails Select cm_cd,isNull(cm_CompanyCode,''),convert(char,getdate(),112),'','','','','','','','','','','','',0,0,0,0,0 From Client_master where cm_cd='" + userId + "' and " + strCompanyWhere;
                        objUtility.ExecuteSQL(strsql);
                    }
                    else
                    {
                        strsql = "Update LoginDetails set lg_lastLoginDt=convert(char,getdate(),112) From Client_master where cm_cd='" + userId + "' and cm_cd=lg_cmcd and " + strCompanyWhere;
                        objUtility.ExecuteSQL(strsql);
                    }
                }
            }
            else if (role == "TradeMobile" || role == "EstroWeb" || role == "CrossWeb")
            {
                string type = "";
                if (role == "EstroWeb" && blnOffline)
                {
                    type = "EstroWebO";
                }
                else if (role == "CrossWeb")
                {
                    type = blnOffline ? "CrossWebO" : "CrossWeb";
                }

                if (blnOffline)
                {
                    if (strLocked == "N")
                    {
                        strWebYN = "Y";
                    }
                }
                else
                {
                    if (!blnValid)
                    {
                        strWebYN = "Y";
                    }
                }
                var addLog = AddLog_Session(userId, strWebYN, role == "TradeMobile" ? "M" : "C", type);
                if (addLog != null)
                {
                    rtnValue.message = addLog;
                    return rtnValue;
                }
            }
            string qury = "";
            string isPanPwd = "N"; //objUtility.GetWebParameter("PANASPASSWORD").ToString();
            string strExpPwd = "";
            bool blnChkCol = Convert.ToInt16(objUtility.fnFireQueryTradeWeb("sysobjects a,syscolumns b", "count(0)", "a.id=b.id and b.name in ('cm_panno','cm_dob') and a.name", "Client_master", true)) == 2;
            if (keyBased == true && string.IsNullOrWhiteSpace(password))
            {
                qury = "select LTrim(RTrim(cm_cd)) ClientCode, LTrim(RTrim(cm_Name)) ClientName, 'user' UserType, '' AS ExpectedPassword from Client_master with (nolock) where cm_cd='" + userId + "' ";
            }
            else
            {
                if ((product == "C" || product == "E") && blnOffline)
                {
                    qury = " select cm_pwd,  cm_Panno, LTrim(RTrim(cm_cd)) ClientCode, LTrim(RTrim(cm_Name)) ClientName, 'user' UserType, '' AS ExpectedPassword  from Client_master with (nolock) where cm_cd='" + userId.Trim() + "' and cm_companycode='" + dpid + "' ";
                }
                else
                {
                    qury = " select cm_pwd, '' cm_Panno, LTrim(RTrim(cm_cd)) ClientCode, LTrim(RTrim(cm_Name)) ClientName, 'user' UserType, " + ((role == "Admin" && blnChkCol) ? "LEFT(cm_panno, 4) + LEFT(cm_dob, 4)" : "''") + " AS ExpectedPassword  from Client_master with (nolock) where cm_cd='" + userId.Trim() + "' ";
                }
            }
            var dt = objUtility.OpenDataTable(qury);
            if (dt.Rows.Count > 0)
            {
                if (keyBased == true && string.IsNullOrWhiteSpace(password))
                {
                    rtnValue.data = dt;
                    return rtnValue;
                }
                if (objUtility.GetWebParameter("TWEBENCPWD") == "Y")
                {
                    password = objUtility.Encrypt(password);
                    strExpPwd = objUtility.Encrypt(dt.Rows[0]["ExpectedPassword"].ToString().Trim());
                }

                if (isPanPwd == "Y")
                {
                    if (dt.Rows[0]["cm_Panno"].ToString().ToUpper().Trim() == password.ToUpper().Trim())
                    {
                        dt.Columns.Remove("cm_pwd");
                        dt.Columns.Remove("cm_Panno");
                        dt.Columns.Remove("ExpectedPassword");
                        dt.AcceptChanges();
                        rtnValue.data = dt;
                        return rtnValue;
                    }
                }
                else if (dt.Rows[0]["cm_pwd"].ToString().Trim() == password.Trim())
                {
                    if (strExpPwd != "" && strExpPwd == dt.Rows[0]["cm_pwd"].ToString().Trim())
                    {
                        dt.Columns.Add("FirstLogin", typeof(string));
                        dt.Rows[0]["FirstLogin"] = "Y";
                    }
                    dt.Columns.Remove("cm_pwd");
                    dt.Columns.Remove("cm_Panno");
                    dt.Columns.Remove("ExpectedPassword");
                    dt.AcceptChanges();
                    rtnValue.data = dt;
                    return rtnValue;
                }
            }
            rtnValue.message = "Invalid Username or Password";
            return rtnValue;
        }

        public dynamic AddLog_Session(string userId, string strWebYN, string loginAs, string type = "")
        {
            string strMode = "";
            if (Convert.ToInt16(objUtility.fnFireQueryTradeWeb("SysObjects", "count(*)", "name", "Login_Session", true)) == 0)
            {
                strsql = " CREATE TABLE [dbo].[Login_Session]( ";
                strsql += " [ls_srno] [numeric](18, 0) IDENTITY(1,1) NOT NULL,";
                strsql += " [ls_code] [varchar](8) NOT NULL,";
                strsql += " [ls_logintype] [char](1) NOT NULL,";
                strsql += " [ls_hostName] [varchar](50) NOT NULL,";
                strsql += " [ls_token] [varchar](75) NOT NULL,";
                strsql += " [ls_logindt] [char](8) NOT NULL,";
                strsql += " [ls_logintm] [char](8) NOT NULL,";
                strsql += " [ls_logoutdt] [char](8) NOT NULL,";
                strsql += " [ls_logouttm] [char](8) NOT NULL";
                strsql += " ) ON [PRIMARY] ";
                objUtility.ExecuteSQL(strsql);
            }
            double intPwdAttempt = 0;
            if (type == "EstroWebO" || type == "CrossWebO")
            {
                string strPwdSetting = objUtility.fnFireQueryTradeWeb("webparameter", "sp_sysvalue", "sp_parmcd", "PWDSETTING", true);
                if (strPwdSetting != "")
                {
                    string[] parts = strPwdSetting.Split('/');
                    intPwdAttempt = int.Parse(parts.Length > 2 ? parts[2] : "0");
                }
            }
            else
            {
                intPwdAttempt = Conversion.Val(objUtility.GetSysParmSt("PWDLOCKEDATM", ""));
            }
            strsql = "Select Max(Cast(ls_token as Numeric)) Attmp From Login_Session Where ls_code = '" + userId + "'  And ls_logindt = '" + DateTime.Now.ToString("yyyyMMdd") + "'  And len(ls_token) < 3";
            var result = objUtility.ExecuteSQLReturn(strsql);
            int attmp = 0;
            if (result == null || result is DBNull)
            {
                attmp = 0;
                strMode = "I";
            }
            else
            {
                attmp = Convert.ToInt32(result);
                strMode = "U";
            }
            //int attmp = Convert.ToInt32(objUtility.ExecuteSQLReturn(strsql));
            //strsql = "Select  Cast(Isnull((ls_logindt+' '+ls_logintm),'') as datetime) AtmpDate From Login_Session Where ls_code = '" + userId + "'  And ls_logindt = '" + DateTime.Now.ToString("yyyyMMdd") + "'  And len(ls_token) < 3";
            //var lgnTime = objUtility.ExecuteSQLReturn(strsql);
            //if (lgnTime == null)
            //    lgnTime = DateTime.Now;
            //DateTime currentDate = DateTime.Now;
            //int result = (int)currentDate.Subtract(lgnTime).TotalMinutes;
            if ((attmp - 1) > 0 && (attmp - 1) % intPwdAttempt == 0 && strWebYN == "Y")
            {
                attmp = -1;
                strMode = "U";
            }
            if (attmp >= intPwdAttempt && intPwdAttempt > 0)  ////&& (result < 4)
            {
                if (_configuration["IsTradeWeb"] == "E" || type == "EstroWebO")
                {
                    objUtility.ExecuteSQL("update client_master set cm_locked='Y' where cm_cd='" + userId.Trim() + "'");
                }
                else if (type == "CrossWeb")
                {
                    string strValue = objUtility.fnFireQueryTradeWeb("client_master", "cm_keepsettlement", "cm_cd", userId.Trim(), true);
                    string strSett = "";
                    if (strValue == "N")
                    {
                        strSett = "F";
                    }
                    else if (strValue == "C")
                    {
                        strSett = "Y";
                    }
                    else if (strValue == "I")
                    {
                        strSett = "T";
                    }
                    else if (strValue == "S")
                    {
                        strSett = "M";
                    }

                    objUtility.ExecuteSQL("update client_master set cm_keepsettlement='" + strSett + "' where cm_cd='" + userId.Trim() + "'");
                }
                else
                {
                    if (Convert.ToInt16(objUtility.fnFireQueryTradeWeb("sysobjects a,syscolumns b", "count(0)", "a.id=b.id and b.name = 'cm_confirmwebyn' and a.name", "Client_master", true)) > 0)
                    {
                        DataTable dtClient = objUtility.OpenDataTable("select mkrid, mkrdt from Client_master where cm_cd='" + userId.Trim() + "'");
                        objUtility.ExecuteSQL("update client_master set cm_confirmwebyn = 'L', mkrid = '" + userId.Trim().ToUpper() + "', mkrdt = '" + DateTime.Now.ToString("yyyyMMdd") + "' where cm_cd = '" + userId.Trim() + "'");
                        objUtility.prCommonInsert("Client Master", "Client_master", "ANC", "cm_cd", "Client Code", userId.Trim().ToUpper(), "cm_confirmwebyn", "On Website", strWebYN, "L", "API", userId.Trim().ToUpper(), DateTime.Now.ToString("yyyyMMdd"), DateTime.Now.ToString("HH:mm:ss"), objUtility.GetColumnValue(dtClient, "mkrid"), objUtility.GetColumnValue(dtClient, "mkrdt"), "00:00:00");
                    }
                }
                objUtility.ExecuteSQL("update Login_Session set ls_token = '" + (attmp + 1) + "', ls_logintm = '" + DateTime.Now.ToString("HH:mm:ss") + "'  Where ls_code = '" + userId + "' And ls_logindt = '" + DateTime.Now.ToString("yyyyMMdd") + "'  And len(ls_token) < 3 ");
                return "You have exceeded maximum attempts permitted. User Has Been Locked.";
            }
            string hostName = System.Environment.MachineName;
            if (strMode == "U")
            {
                //if (attmp > intPwdAttempt)
                //    attmp = 0;
                objUtility.ExecuteSQL("update Login_Session set ls_token = '" + (attmp + 1) + "', ls_logintm = '" + DateTime.Now.ToString("HH:mm:ss") + "'  Where ls_code = '" + userId + "' And ls_logindt = '" + DateTime.Now.ToString("yyyyMMdd") + "'  And len(ls_token) < 3 ");
            }
            else
            {
                objUtility.ExecuteSQL("Insert Into Login_Session values ('" + userId + "','" + loginAs + "','" + hostName + "','" + (attmp + 1) + "','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HH:mm:ss") + "','','')");
            }
            return null;
        }

        public dynamic BranchUserDetails(string userId, string password, string role)
        {
            loginResponseWithDT rtnValue = new loginResponseWithDT();
            rtnValue.message = "";

            if (role == "ReKYC")
            {
                string[] arrUsers = objUtility.fnFireQuery("WebParameter", "sp_sysvalue", "sp_parmcd", "ReKycUser", true).Split(',');
                bool blnValid = arrUsers.Any(s => string.Equals(s, userId.Trim(), StringComparison.OrdinalIgnoreCase));

                if (!blnValid)
                {
                    rtnValue.message = "Invalid Username or Password.";
                    return rtnValue;
                }
            }

            if (role == "CommonAPI")
            {
                bool blnValid = false;
                string strAPIUser = objUtility.fnFireQuery("WebParameter", "sp_sysvalue", "sp_parmcd", "MRGPUSER", true);
                if (!string.IsNullOrEmpty(strAPIUser) && strAPIUser.ToUpper() == userId.Trim().ToUpper())
                {
                    blnValid = true;
                }

                if (!blnValid)
                {
                    rtnValue.message = "Invalid Username or Password.";
                    return rtnValue;
                }
            }

            if (role == "ComAPI")
            {
                bool blnValid = false;
                string strAPIUser = objUtility.fnFireQuery("WebParameter", "sp_sysvalue", "sp_parmcd", "APIUSER", true);
                if (!string.IsNullOrEmpty(strAPIUser) && strAPIUser.ToUpper() == userId.Trim().ToUpper())
                {
                    blnValid = true;
                }

                if (!blnValid)
                {
                    rtnValue.message = "Invalid Username or Password.";
                    return rtnValue;
                }
            }

            string qury = "select um_user_id UserCode, um_user_name UserName, um_passwd, 'branch' UserType, ";
            if (role == "Branch")
            {
                qury += " um_specialrights, ";
            }
            qury += " '' UserAccess, 'N' ShowUpdate, Um_Locked, um_status from user_master with (nolock) where um_user_id='" + userId + "' ";
            //var ds = CommonRepository.FillDataset(qury);
            var ds = objUtility.OpenDataSet(qury);
            if (ds != null)
            {
                if (ds?.Tables?.Count > 0 && ds?.Tables[0]?.Rows?.Count > 0)
                {
                    DataTable Dt = ds.Tables[0];
                    if (objUtility.Decrypt(Dt.Rows[0]["um_passwd"].ToString()) == password)
                    {
                        string strUpdateUser = "";
                        string strReportUser = "";

                        if (Dt.Rows[0]["Um_Locked"].ToString().Trim() == "Y")
                        {
                            rtnValue.message = "User Has Been Locked, Contact Head office to get it re-activated..";
                            return rtnValue;
                        }
                        else if (Dt.Rows[0]["um_status"].ToString().Trim() == "I")
                        {
                            rtnValue.message = "User Is Inactive.";
                            return rtnValue;
                        }

                        if (objUtility.fnchkTable("Webparameter"))
                        {
                            strUpdateUser = objUtility.GetWebParameter("TWebVerUSR");
                            strReportUser = objUtility.GetWebParameter("ReportUser");
                        }

                        if (role == "Branch")
                        {
                            Dt.Rows[0]["UserAccess"] = objUtility.GetUserAccess(Dt.Rows[0]["um_specialrights"].ToString().Trim());
                        }
                        else
                        {
                            Dt.Rows[0]["UserAccess"] = "Branch";
                        }
                        if (!string.IsNullOrWhiteSpace(strUpdateUser))
                        {
                            Dt.Rows[0]["ShowUpdate"] = string.Equals(strUpdateUser, Dt.Rows[0]["UserCode"].ToString().Trim(), StringComparison.OrdinalIgnoreCase) ? "Y" : "N";
                        }

                        Dt.Columns.Remove("um_passwd");
                        Dt.Columns.Remove("Um_Locked");
                        Dt.Columns.Remove("um_status");
                        if (role == "Branch")
                        {
                            Dt.Columns.Remove("um_specialrights");
                        }
                        if (!string.IsNullOrWhiteSpace(strReportUser) && string.Equals(strReportUser.Trim(), userId.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            Dt.Rows[0]["UserType"] = "ReportUser";
                        }
                        rtnValue.data = Dt;
                        return rtnValue;
                    }
                    rtnValue.message = "Invalid Username or Password.";
                    return rtnValue;
                }
            }
            rtnValue.message = "Invalid Username or Password.";
            return rtnValue;
        }

        public dynamic Login_API_Authorize(string key, string product, string loginAs, string feature)
        {
            if (string.IsNullOrEmpty(key) && loginAs == "C")
            {
                if (feature == "FundRequest" && product == "T" && objUtility.isSykes())
                {
                    return "FUP";
                }

                bool blnTBL_DP = objUtility.mfnGetSysSplFeatureDP("TBL");
                bool blnTBO_DP = objUtility.mfnGetSysSplFeatureDP("TBO");
                bool blnTBL = false;
                if (objUtility.fnchkTable("Entity_master"))
                {
                    blnTBL = objUtility.mfnGetSysSplFeature("TBL");
                }

                if (product == "C" && (blnTBL_DP || blnTBO_DP))
                {
                    return "CBW";
                }
                if (product == "E" && (blnTBL_DP || blnTBO_DP))
                {
                    return "EBW";
                }
                if (product == "T" && blnTBL)
                {
                    return "TBL";
                }
                else
                {
                    return "";
                }
            }
            if (string.IsNullOrEmpty(key) && loginAs.Trim() == "B")
            {
                if (product == "T")
                {
                    if (string.Equals(feature, "REKYC", StringComparison.OrdinalIgnoreCase))
                    {
                        if (objUtility.mfnGetSysSplFeature("TPI"))
                        {
                            return "TPI";
                        }
                        else if (objUtility.mfnGetSysSplFeature("RKM"))
                        {
                            return "RKM";
                        }
                    }

                    if (objUtility.mfnGetSysSplFeature("TBL"))
                    {
                        return "TBI";
                    }
                }
                else if (product == "#")
                {
                    if (objUtility.isRatnakar())
                    {
                        return "CMP";
                    }
                }

                //if (objUtility.isBPT())
                //{
                //    if (objUtility.fnchkTable("Subscription_Order") == false)
                //    {
                //        string strSQL = "Create Table Subscription_Order" +
                //             "(Suo_SrNO int identity(1, 1) Primary Key," +
                //             "Suo_ClientCode char(8) Not Null," +
                //             "Suo_FromDate char(8) Not Null," +
                //             "Suo_ToDate char(8) Not Null," +
                //             "Suo_SpecialScheme char(8) Not Null," +
                //             "Suo_NormalScheme char(8) Not Null," +
                //             "Suo_NoOfOrder int Not Null," +
                //             "Suo_Amount money Not Null," +
                //             "Suo_Status char(1) Not Null," +
                //             "Suo_InactiveDt char(8)," +
                //             "Suo_MkrID char(8)," +
                //             "Suo_MkrDt char(8)," +
                //             "Suo_Filler1 char(8)," +
                //             "Suo_Filler2 char(8)," +
                //             "Suo_Filler3 char(8)," +
                //             "Suo_FillerN1 Numeric(18,0)," +
                //             "Suo_FillerN2 Numeric(18,0)," +
                //             "Suo_FillerN3 Numeric(18,0))";
                //        objUtility.ExecuteSQL(strSQL);
                //    }
                //    if (objUtility.fnchkTable("Subscription_Audit") == false)
                //    {
                //        string strSQL = "Create Table Subscription_Audit" +
                //             "(Sua_SrNO int Not Null," +
                //             "Sua_ClientCode char(8) Not Null," +
                //             "Sua_FromDate char(8) Not Null," +
                //             "Sua_ToDate char(8) Not Null," +
                //             "Sua_SpecialScheme char(8) Not Null," +
                //             "Sua_NormalScheme char(8) Not Null," +
                //             "Sua_NoOfOrder int Not Null," +
                //             "Sua_Amount money Not Null," +
                //             "Sua_Status char(1) Not Null," +
                //             "Sua_InactiveDt char(8)," +
                //             "Sua_MkrID char(8)," +
                //             "Sua_MkrDt char(8)," +
                //             "Sua_Filler1 char(8)," +
                //             "Sua_Filler2 char(8)," +
                //             "Sua_Filler3 char(8)," +
                //             "Sua_FillerN1 Numeric(18,0)," +
                //             "Sua_FillerN2 Numeric(18,0)," +
                //             "Sua_FillerN3 Numeric(18,0)," +
                //             "Sua_RecType char(1))";
                //        objUtility.ExecuteSQL(strSQL);
                //    }
                //    return "SUB";
                //}
            }
            if (string.IsNullOrEmpty(key) && loginAs.Trim() == "M" && product == "T")
            {
                if (objUtility.mfnGetSysSplFeature("TMB"))
                {
                    return "TMB";
                }
            }
            int dblTmp;
            string strSql = "";
            string[] arrtmp = new string[] { };
            string keyName = "";

            if (key == null || key.Contains("-") == false)
            {
                return "";
            }

            var ds = Get_CompanyName();
            if (ds != null)
            {
                if (ds.Rows.Count > 0)
                {
                    var strcomname = Strings.Left(ds.Rows[0]["companyName"].ToString().ToUpper(), 20).Trim();
                    arrtmp = key.Split("-");
                    arrtmp = Strings.Split(key, "-");
                    var strTmp = "";
                    for (dblTmp = 0; dblTmp <= arrtmp.Length - 1; dblTmp++)
                    {
                        strSql = Convert.ToString(objUtility.fnAlpha2Num(arrtmp[dblTmp].ToString()));

                        if (Strings.Len(strSql) % 2 == 1)
                            strSql = "0" + strSql;

                        strTmp = strTmp + strSql;
                    }
                    arrtmp[1] = "";
                    int i = 0;
                    for (i = 1; i <= Strings.Len(strTmp); i += 2)
                    {
                        arrtmp[1] = arrtmp[1] + Strings.Chr(Convert.ToInt32(Conversion.Val(Strings.Mid(strTmp, i, 2))) + 64);
                    }
                    arrtmp[0] = Strings.UCase(strcomname);
                    for (i = Strings.Len(arrtmp[0]); i >= 1; i += -1)
                    {
                        strSql = Strings.Mid(arrtmp[0], i, 1);

                        if (strSql != "")
                        {
                            if (Strings.Asc(strSql) < 65 || Strings.Asc(strSql) > 85)
                            {
                                arrtmp[0] = Strings.Replace(arrtmp[0], strSql, "");
                            }
                        }
                    }
                    if (arrtmp[0] == Strings.Left(arrtmp[1], Strings.Len(arrtmp[0])))
                    {
                        keyName = Strings.Right(arrtmp[1], Strings.Len(arrtmp[1]) - Strings.Len(arrtmp[0]));
                        string strKey = "";
                        switch (loginAs.Trim())
                        {
                            case "B": //Branch
                                if (product.Trim() == "T")
                                {
                                    if (keyName == "TBI")
                                    {
                                        strKey = keyName;
                                    }
                                    else if (keyName == "INP")
                                    {
                                        if (Convert.ToInt32(objUtility.fnFireQueryTradeWeb("Entity_master", "count(0)", "em_bclearingno = '6283' Or em_nclearingno", "13264", true)) > 0)
                                        {
                                            strKey = "PBI";
                                        }
                                    }
                                    else if (keyName == "OPN" && objUtility.mfnGetSysSplFeature("OPN"))
                                    {
                                        strKey = "COM";
                                    }
                                    else if (keyName == "OPC" && objUtility.mfnGetSysSplFeature("OPC"))
                                    {
                                        strKey = "COM";
                                    }
                                }
                                else if (product.Trim() == "C")
                                {
                                    if (keyName == "CBI")
                                    {
                                        strKey = keyName;
                                    }
                                    else if (keyName == "CLM")
                                    {
                                        strKey = keyName;
                                    }
                                }
                                else if (product.Trim() == "E")
                                {
                                    if (keyName == "EBI")
                                    {
                                        strKey = keyName;
                                    }
                                }
                                break;
                            case "M": //TradeMobile
                                if (keyName == "TMB")
                                {
                                    strKey = keyName;
                                }
                                break;
                            case "C": //Client
                                if (keyName == "BPI" || keyName == "INP" || keyName == "TBL" || keyName == "TBI")
                                {
                                    strKey = keyName == "TBI" ? "BPI" : keyName;
                                }
                                break;
                        }
                        return strKey;
                    }
                }
            }
            return "";
        }

        public dynamic Get_CompanyName()
        {
            string whereCond = "  where em_cd =  (select min(em_cd) from Entity_master) ";
            if (objUtility.isSykes())
            {
                whereCond = " where em_cd = 'B' ";
            }
            ////// ******* comment code used for companyLogo
            //string qury = "If Not Exists (select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME=N'Entity_Master') " +
            //               " begin  select sp_sysvalue em_Name ,(Select img_logo from Images where img_desc='Company Logo' and img_logo is not null) CompanyLogo from sysparameter with (nolock) where sp_parmcd = 'NAME' end " +
            //               " Else begin  select em_Name ,(Select img_logo from Images where img_desc='Company Logo' and img_logo is not null) CompanyLogo from Entity_master where em_cd = (select min(em_cd) from Entity_master)  end ";
            string qury = "IF  Exists (Select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME=N'Entity_Master')  BEGIN " +
                          "   If Exists(Select * from Entity_master with (NoLock)) Begin select Ltrim(Rtrim(em_Name)) as companyName from Entity_master with (NoLock) " + whereCond + " end  " +
                          "   Else Begin Select Ltrim(Rtrim(sp_sysvalue))  as companyName from sysparameter with (nolock) where sp_parmcd = 'NAME'  end End " +
                          " ELSE Begin Select Ltrim(Rtrim(sp_sysvalue))  as companyName from sysparameter with(nolock) where sp_parmcd = 'NAME'  END ";
            DataTable dt = new DataTable();
            dt = objUtility.OpenDataTable(qury);
            if (dt.Rows.Count > 0)
            {
                //if (dt.Rows[0]["CompanyLogo"].ToString() != "")
                //{
                //    byte[] logobyte = (byte[])dt.Rows[0]["CompanyLogo"];
                //    string logoBase64 = Convert.ToBase64String(logobyte);
                //}
                dt.Columns.Add("menuFlag", typeof(string));
                dt.Rows[0]["menuFlag"] = "";
                //// Check ReKYC Menu Option
                if (objUtility.mfnGetSysSplFeature("RKC"))
                {
                    string sqlQry = "IF Not Exists (SELECT * FROM WEBPARAMETER WHERE SP_PARMCD='RKYCMENU') Begin Insert Into WebParameter Values ('RKYCMENU','Hide/Show Menu','Y') End";
                    objUtility.ExecuteSQL(sqlQry);
                    string rkycMenu = objUtility.GetWebParameter("RKYCMENU");
                    if (rkycMenu == "Y")
                    {
                        dt.Rows[0]["menuFlag"] = "Y";
                    }
                    dt.AcceptChanges();
                }
                return dt; // Strings.Left(ds.Tables[0].Rows[0]["em_Name"].ToString().ToUpper(), 20).Trim();
            }
            return null;
        }
        #endregion

        #region Repository call for Common API method
        public dynamic CommonAPIrepoCall(string strModuleName, string strFunctionName, string strXML)
        {
            string ProcedureName = "";
            int SerialNo = 0;

            string sql = "select SerialNo, ProcedureName, IsDebugEnabled, FunctionName from tbl_GenericAPIDefinition";
            sql += " where ProjectName='TradeWebAPI' and ModuleName='" + strModuleName + "' and FunctionName='" + strFunctionName + "'";
            DataTable dt = objUtility.OpenDataTable(sql);
            if (dt.Rows.Count > 0)
            {
                SerialNo = Convert.ToInt32(dt.Rows[0]["SerialNo"].ToString().Trim());
                ProcedureName = dt.Rows[0]["ProcedureName"].ToString().Trim();
            }

            if (SerialNo > 0)
            {
                string connetionString = objUtility.GetConnectionStr();
                using (SqlConnection conn = new SqlConnection(connetionString))
                {
                    using (SqlCommand cmd = new SqlCommand(ProcedureName))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        //cmd.CommandTimeout = 0;
                        if (conn.State == ConnectionState.Closed)
                            conn.Open();
                        cmd.Connection = conn;
                        cmd.CommandTimeout = 0;
                        if (strFunctionName.ToUpper() == "ADD" || strFunctionName.ToUpper() == "EDIT" || strFunctionName.ToUpper() == "DELETE")
                        {
                            cmd.Parameters.Add(new SqlParameter("@dsXml", SqlDbType.VarChar));
                            cmd.Parameters["@dsXml"].Value = strXML;
                            // Output parameters
                            SqlParameter returnMainSlNoParam = new SqlParameter("@o_iReturnMainSlNo", SqlDbType.Int)
                            {
                                Direction = ParameterDirection.Output
                            };
                            cmd.Parameters.Add(returnMainSlNoParam);

                            SqlParameter flagParam = new SqlParameter("@o_vcFlag", SqlDbType.VarChar, 1)
                            {
                                Direction = ParameterDirection.Output
                            };
                            cmd.Parameters.Add(flagParam);

                            SqlParameter messageParam = new SqlParameter("@o_vcMessage", SqlDbType.VarChar, 1000)
                            {
                                Direction = ParameterDirection.Output
                            };
                            cmd.Parameters.Add(messageParam);
                            cmd.ExecuteNonQuery();

                            //int returnMainSlNo = (int)cmd.Parameters["@o_iReturnMainSlNo"].Value;
                            int returnMainSlNo = cmd.Parameters["@o_iReturnMainSlNo"].Value != DBNull.Value
                            ? Convert.ToInt32(cmd.Parameters["@o_iReturnMainSlNo"].Value)
                            : 0;
                            ;
                            string flag = (string)cmd.Parameters["@o_vcFlag"].Value;
                            string message = (string)cmd.Parameters["@o_vcMessage"].Value;

                            if (flag == "S")
                            {
                                XmlDocument xmlDoc = new XmlDocument();
                                xmlDoc.LoadXml(strXML);

                                //XmlNodeList itemList = xmlDoc.SelectNodes("//SecondLevelData/item");
                                //int itemCount = itemList.Count;

                                DataSet dsTemp = new DataSet();
                                DataTable dtTemp = new DataTable();
                                dtTemp.Columns.Add("RowsAffected", typeof(int));
                                DataRow dr = dtTemp.NewRow();
                                dr["RowsAffected"] = 1;
                                dtTemp.Rows.Add(dr);
                                dsTemp.Tables.Add(dtTemp);
                                dsTemp.Tables[0].TableName = "rs0";
                                //return new CrossNetCommonModel { success = true, message = "", data = dsTemp, datarows = new List<string> { dtTemp.Rows.Count.ToString() } };
                                return new CrossNetSPModel { Data = dsTemp, DataRows = new List<string> { "1" }, Success = true, Message = message };
                            }
                            else
                            {
                                return new CrossNetSPModel { Data = new DataSet(), DataRows = null, Success = false, Message = message };
                            }
                        }
                        cmd.Parameters.Add(new SqlParameter("@dsXml", SqlDbType.VarChar));
                        cmd.Parameters["@dsXml"].Value = strXML;
                        SqlDataAdapter adap = new SqlDataAdapter
                        {
                            SelectCommand = cmd
                        };
                        DataSet ds = new DataSet();
                        adap.Fill(ds);
                        //cmd.ExecuteNonQuery();

                        //JArray jsonArray = null;

                        List<string> lstRows = new List<string>();
                        for (int i = 0; i < ds.Tables.Count; i++)
                        {
                            ds.Tables[i].TableName = "rs" + i;
                            lstRows.Add(ds.Tables[i].Rows.Count.ToString());
                        }
                        //ds.Tables[0].TableName = "rs0";
                        //ds.Tables[1].TableName = "rs1";
                        //lstRows.Add(ds.Tables[0].Rows.Count.ToString());
                        //lstRows.Add(ds.Tables[1].Rows.Count.ToString());

                        return new CrossNetSPModel { Data = ds, DataRows = lstRows, Success = true, Message = "" };
                        //return new CrossNetCommonModel { success = true, message = "", data = ds, datarows = lstRows };

                        //cmd.Parameters.Clear();
                        //return jsonArray;
                    }
                }
            }
            else
            {
                ProcedureName = objUtility.fnFireQueryTradeWeb("tbl_GenericAPIDefinition", "ProcedureName", "ModuleName='' and FunctionName", "Search", true);
                string connetionString = objUtility.GetConnectionStr();
                if (ProcedureName != "")
                {
                    using (SqlConnection conn = new SqlConnection(connetionString))
                    {
                        using (SqlCommand cmd = new SqlCommand(ProcedureName))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            //cmd.CommandTimeout = 0;
                            if (conn.State == ConnectionState.Closed)
                                conn.Open();
                            cmd.Connection = conn;
                            cmd.CommandTimeout = 0;
                            cmd.Parameters.Add(new SqlParameter("@dsXml", SqlDbType.VarChar));
                            cmd.Parameters["@dsXml"].Value = strXML;
                            SqlDataAdapter adap = new SqlDataAdapter
                            {
                                SelectCommand = cmd
                            };
                            DataSet ds = new DataSet();
                            adap.Fill(ds);
                            //cmd.ExecuteNonQuery();

                            List<string> lstRows = new List<string>();
                            for (int i = 0; i < ds.Tables.Count; i++)
                            {
                                ds.Tables[i].TableName = "rs" + i;
                                lstRows.Add(ds.Tables[i].Rows.Count.ToString());
                            }
                            //ds.Tables[0].TableName = "rs0";
                            //ds.Tables[1].TableName = "rs1";
                            //lstRows.Add(ds.Tables[0].Rows.Count.ToString());
                            //lstRows.Add(ds.Tables[1].Rows.Count.ToString());

                            return new CrossNetSPModel { Data = ds, DataRows = lstRows, Success = true, Message = "" };
                        }
                    }
                }
                else
                {
                    return new CrossNetSPModel { Data = null, DataRows = null, Success = false, Message = "Invalid Request Definition" };
                }

                //return JsonConvert.DeserializeObject("[{\"ResponseFlag\":\"E\", \"ResponseMessage\":\"Invalid Request Definition\"}]");
            }
        }

        public dynamic CompileTypst(string ReportName, string ReportDisplayName, DataSet dts, string pdfType)
        {
            byte[] fileBytes = new byte[] { };
            var pdfResponses = new List<PDFResponse> { };
            string strJSONPath = "";
            string strPDFPath = "", baseDir = "", strJSON = "";
            try
            {
                Guid id = Guid.NewGuid();
                string uniquename = "" + id;
                string strJSONRelPath = "";
                string strTypstCommand = "", minorJson = "";
                //strJSON = JsonConvert.SerializeObject(dts, Formatting.Indented);
                baseDir = AppDomain.CurrentDomain.BaseDirectory + "Typst";
                string strTypstExePath = Path.Combine(baseDir, "typst.exe");
                string strInputPath = Path.Combine(baseDir, "Inputs");
                string strOutputPath = Path.Combine(baseDir, "Outputs");

                if (dts == null || dts.Tables.Count == 0)
                    return CreateEmptyResultTable();

                if (dts.Tables[0].Rows.Count == 0)
                    return CreateEmptyResultTable();

                string clientCode = "";

                strJSON = JsonConvert.SerializeObject(dts, Formatting.Indented);

                objUtility.TypstCreateCompanyLogo(baseDir);

                if (!Directory.Exists(strInputPath))
                    Directory.CreateDirectory(strInputPath);

                if (!Directory.Exists(strOutputPath))
                    Directory.CreateDirectory(strOutputPath);

                /////////// *** Please comment below two lines before build release ***** (For debug code)
                //string jsonfilePath = Path.Combine(baseDir, "inputs", "Test_Json.json").Replace("\\", "/");  // HH
                //strJSON = File.ReadAllText(jsonfilePath); // HH
                if (ReportDisplayName == "MTFSummary")
                    ReportDisplayName = "MTF_Status_Detail";

                strPDFPath = Path.Combine(strOutputPath, ReportName + "_" + uniquename + ".pdf");

                strJSONRelPath = Path.Combine("..", "Inputs", ReportName + "_" + uniquename + ".json").Replace("\\", "/");

                if (pdfType == "KRAPDF")
                {
                    strTypstCommand = "\"" + Path.Combine(baseDir, "templates", "TypstKRA.typ") + "\"" + "  " + "\"" + strPDFPath + "\"";
                    strJSON = dts.Tables[0].Rows[0][0].ToString();

                    var desObj = AppendKRAimgTypst(baseDir, strJSON);
                    strJSON = JsonConvert.SerializeObject(desObj);
                }
                else if (pdfType == "FINALPDF")
                {
                    strTypstCommand = "\"" + Path.Combine(baseDir, "templates", "TypstRekyc.typ") + "\"" + "  " + "\"" + strPDFPath + "\"";
                    strJSON = dts.Tables[0].Rows[0][0].ToString();

                    var desObj = AppendImagePdfTypst(baseDir, strJSON, uniquename);
                    strJSON = JsonConvert.SerializeObject(desObj);
                }
                else if (pdfType == "CLOSUREPDF")
                {
                    strTypstCommand = "\"" + Path.Combine(baseDir, "templates", "TypstAccountClosure.typ") + "\"" + "  " + "\"" + strPDFPath + "\"";
                    strJSON = dts.Tables[0].Rows[0][0].ToString();

                    var desObj = AppendClosureImgTypst(baseDir, strJSON);
                    strJSON = JsonConvert.SerializeObject(desObj);
                }
                else if (pdfType == "CKYCINDV" || pdfType == "CKYCLE")
                {
                    strJSON = dts.Tables[0].Rows[0][0].ToString(); // HH uncomment
                    string typstFileName = pdfType == "CKYCINDV" ? "TypstCKYC_Individual.typ" : "TypstCKYC_LE.typ";
                    JArray rootArray = JArray.Parse(strJSON);
                    JObject mainObj = (JObject)rootArray[0];
                    var ifRelPersn = mainObj["IfMinor"];
                    if (ifRelPersn != null && ifRelPersn.HasValues)
                    {
                        minorJson = ifRelPersn.ToString();
                        if (minorJson != "")
                        {
                            var jsonObj = JsonConvert.DeserializeObject<dynamic>(minorJson);
                            foreach (var rpJson in jsonObj)
                            {
                                string typstFileNameRelPerson = pdfType == "CKYCINDV" ? "TypstCKYC_Ind_Rel.typ" : "TypstCKYC_LE_Rel.typ";
                                minorJson = JsonConvert.SerializeObject(rpJson);
                                minorJson = "[" + minorJson + "]";
                                var desObjMinor = AppendImagePdfTypst(baseDir, minorJson, uniquename);
                                clientCode = desObjMinor[0]["ClientCode"]?.ToString();
                                minorJson = JsonConvert.SerializeObject(desObjMinor);

                                strTypstCommand = "\"" + Path.Combine(baseDir, "templates", typstFileNameRelPerson) + "\"" + "  " + "\"" + strPDFPath + "\"";
                                strJSONPath = CreateJsonConfig(minorJson, ReportName + "_" + uniquename + ".json");
                                string argumentsMinro = $"compile {strTypstCommand} --input file_path={strJSONRelPath} --root \"{baseDir}\"";
                                try
                                {
                                    fileBytes = CommonTypstPdfPSI(argumentsMinro, strPDFPath, strTypstExePath, strJSONPath);
                                    pdfResponses.Add(new PDFResponse { PDFName = "CKYC_Indv_RelatedPerson_" + clientCode + ".pdf", Base64PDF = Convert.ToBase64String(fileBytes) });
                                }
                                catch (Exception)
                                { }
                                finally
                                {
                                    DeleteTypstFiles(baseDir, minorJson);
                                }
                            }
                        }
                    }
                    var desObj = AppendImagePdfTypst(baseDir, strJSON, uniquename);
                    strJSON = JsonConvert.SerializeObject(desObj);
                    strTypstCommand = "\"" + Path.Combine(baseDir, "templates", typstFileName) + "\"" + "  " + "\"" + strPDFPath + "\"";
                }
                else if (pdfType == "CVLKYCINDV")
                {
                    strTypstCommand = "\"" + Path.Combine(baseDir, "templates", "TypstCVLKYC_Indv.typ") + "\"" + "  " + "\"" + strPDFPath + "\"";
                    strJSON = dts.Tables[0].Rows[0][0].ToString();

                    var desObj = AppendImagePdfTypst(baseDir, strJSON, uniquename);
                    strJSON = JsonConvert.SerializeObject(desObj);

                }
                else if (pdfType.ToUpper() == "ALERTFORINCOMEUPDATION" || pdfType.ToUpper() == "ALERTFORDORMANT")
                {
                    strTypstCommand = "\"" + Path.Combine(baseDir, "templates", "AlertForIncomeDormant.typ") + "\"" + "  " + "\"" + strPDFPath + "\"";
                }
                else
                {
                    clientCode = dts.Tables[0].Rows[0]["ClientCode"]?.ToString();
                    strTypstCommand = $"\"{Path.Combine(baseDir, "templates", "common.typ")}\" \"{strPDFPath}\"";
                }

                strJSONPath = CreateJsonConfig(strJSON, ReportName + "_" + uniquename + ".json");
                string arguments = $"compile {strTypstCommand} --input file_path={strJSONRelPath} --root \"{baseDir}\"";

                fileBytes = CommonTypstPdfPSI(arguments, strPDFPath, strTypstExePath, strJSONPath);

                if (pdfType == "FINALPDF")
                {
                    AppenePdfAttachmentInFinalPDF(baseDir, strJSON, strPDFPath, ref strOutputPath);
                    fileBytes = new byte[] { };
                    fileBytes = File.ReadAllBytes(strOutputPath);
                }

                pdfResponses.Add(new PDFResponse { PDFName = clientCode.Trim() + "_" + ReportDisplayName.Trim() + ".pdf", Base64PDF = Convert.ToBase64String(fileBytes) });

                DataTable dt = new DataTable();
                dt.Columns.Add("PDFName", typeof(string));
                dt.Columns.Add("Base64PDF", typeof(string));
                dt.Columns.Add("TotalPages", typeof(int));

                foreach (var pdf in pdfResponses)
                {
                    PdfReader reader = new PdfReader(fileBytes); // or file path
                    int pageCount = reader.NumberOfPages;
                    reader.Close();
                    dt.Rows.Add(pdf.PDFName, pdf.Base64PDF, pageCount);
                }
                DataSet dsRes = new DataSet();
                dsRes.Tables.Add(dt);
                dsRes.Tables[0].TableName = "rs0";

                return dsRes;
            }
            catch (Exception)
            {
                if (strJSONPath != "" && File.Exists(strJSONPath))
                {
                    File.Delete(strJSONPath);
                }
                throw;
            }
            finally
            {
                if (strPDFPath != "" && File.Exists(strPDFPath))
                {
                    File.Delete(strPDFPath);
                }
                if (pdfType == "KRAPDF")
                {
                    DeleteKRATypstFiles(baseDir, strJSON);
                }
                else if (pdfType == "FINALPDF")
                {
                    DeleteTypstFiles(baseDir, strJSON);
                }
                else if (pdfType == "CLOSUREPDF")
                {
                    DeleteClosureTypstFiles(baseDir, strJSON);
                }
            }
        }

        public dynamic CompileTypstSeperateFiles(string ReportName, string ReportDisplayName, DataSet dts, string pdfType)
        {
            byte[] fileBytes = new byte[] { };
            var pdfResponses = new List<PDFResponse> { };
            string strJSONPath = "";
            string strPDFPath = "", baseDir = "", strJSON = "";
            try
            {
                Guid id = Guid.NewGuid();
                string uniquename = "" + id;
                string strJSONRelPath = "";
                string strTypstCommand = "";
                strJSON = JsonConvert.SerializeObject(dts, Formatting.Indented);
                baseDir = AppDomain.CurrentDomain.BaseDirectory + "Typst";
                string strTypstExePath = Path.Combine(baseDir, "typst.exe");
                string strInputPath = Path.Combine(baseDir, "inputs");
                string strOutputPath = Path.Combine(baseDir, "outputs");

                #region old logo logic
                /*if (!File.Exists(Path.Combine(baseDir, "assets", "CompanyLogo.png")))
                {
                    strsql = "Select img_code, img_logo logo from Images with (nolock) where img_desc = 'Company Logo' and img_logo is not null order by img_code";
                    DataTable dtLogo = objUtility.OpenDataTable(strsql);
                    if (dtLogo.Rows.Count > 0)
                    {
                        byte[] logoBytes = (byte[])dtLogo.Rows[0]["logo"];
                        using (var ms = new MemoryStream(logoBytes))
                        {
                            try
                            {
                                using (System.Drawing.Image image = System.Drawing.Image.FromStream(ms))
                                {
                                    string logofilePath = Path.Combine(baseDir, "assets", "CompanyLogo.png");
                                    image.Save(logofilePath, System.Drawing.Imaging.ImageFormat.Png);  // Save as PNG
                                }
                            }
                            catch (Exception ex)
                            {
                                string fallbackPath = Path.Combine(baseDir, "assets", "CompanyLogo.png");
                                File.WriteAllBytes(fallbackPath, logoBytes); // just dump it
                            }
                        }
                    }
                }*/
                #endregion

                objUtility.TypstCreateCompanyLogo(baseDir);

                if (!Directory.Exists(strInputPath))
                    Directory.CreateDirectory(strInputPath);

                if (!Directory.Exists(strOutputPath))
                    Directory.CreateDirectory(strOutputPath);

                /////////// *** Please comment below two lines before build release ***** (For debug code)
                //string jsonfilePath = Path.Combine(baseDir, "inputs", ReportName + "_Seprt.json").Replace("\\", "/");  // HH
                //strJSON = File.ReadAllText(jsonfilePath);

                JObject _source = JObject.Parse(strJSON);
                var len = _source.Count;
                var distinctClientCodes = _source["rs0"]
                    .Select(c => (string)c["ClientCode"])
                    .Distinct()
                    .ToList();

                #region
                foreach (var clientId in distinctClientCodes)
                {
                    //// For first Header json
                    var rs0 = _source["rs0"].Where(r => r["ClientCode"] != null && r["ClientCode"].ToString().Trim() == clientId);
                    // Create new JObject with filtered data
                    JObject jobjRs0 = new JObject
                    {
                        ["rs0"] = new JArray(rs0)
                    };
                    // Convert back to JSON string
                    //string rs0Json = jobjRs0.ToString(Formatting.Indented);

                    ////// For Property json
                    JObject objRs1 = new JObject();
                    string formatGrid = _source["rs0"][0]["FormatGrid"].ToString();
                    string[] propGrid = formatGrid.Split(",");
                    int r2 = 0;
                    foreach (string grid in propGrid)
                    {
                        string rs1Val = "rs" + grid[grid.Length - 1];
                        var rs1 = _source[rs1Val].Where(r => r["ColumnName"] != null);
                        JObject obj1 = new JObject
                        {
                            [rs1Val] = new JArray(rs1)
                        };
                        objRs1.Merge(obj1, new JsonMergeSettings
                        {
                            MergeArrayHandling = MergeArrayHandling.Concat
                        });
                        r2 = Convert.ToInt32("" + rs1Val[rs1Val.Length - 1]);
                    }
                    ////// For Data json
                    JObject objRs2 = new JObject();
                    for (int i = r2 + 1; i < len; i++)
                    {
                        string rs2Val = "rs" + i;
                        var rs2 = _source[rs2Val].Where(r => r["ClientCode"] != null && r["ClientCode"].ToString().Trim() == clientId);
                        // Create new JObject with filtered data
                        JObject obj2 = new JObject
                        {
                            [rs2Val] = new JArray(rs2)
                        };
                        objRs2.Merge(obj2, new JsonMergeSettings
                        {
                            MergeArrayHandling = MergeArrayHandling.Concat
                        });
                    }
                    //// Merge json
                    jobjRs0.Merge(objRs1, new JsonMergeSettings
                    {
                        MergeArrayHandling = MergeArrayHandling.Concat
                    });
                    jobjRs0.Merge(objRs2, new JsonMergeSettings
                    {
                        MergeArrayHandling = MergeArrayHandling.Concat
                    });

                    string finalJSON = jobjRs0.ToString();

                    ///// Start Typst code          
                    strPDFPath = Path.Combine(strOutputPath, ReportName + "_Seprt" + clientId + ".pdf");
                    strTypstCommand = $"\"{Path.Combine(baseDir, "templates", "commonSeprt.typ")}\" \"{strPDFPath}\"";
                    strJSONPath = CreateJsonConfig(finalJSON, ReportName + "_Seprt" + clientId + ".json");
                    strJSONRelPath = Path.Combine("..", "inputs", ReportName + "_Seprt" + clientId + ".json").Replace("\\", "/");

                    string arguments = $"compile {strTypstCommand} --input file_path={strJSONRelPath} --root \"{baseDir}\"";
                    try
                    {
                        var processStartInfo = new ProcessStartInfo
                        {
                            FileName = strTypstExePath,
                            Arguments = arguments,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        using var process = Process.Start(processStartInfo);
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        process.WaitForExit();

                        if (process.ExitCode == 0)
                        {
                            //throw new Exception($"Typst compile failed with exit code {process.ExitCode}: {error}");

                            fileBytes = File.ReadAllBytes(strPDFPath);

                            if (File.Exists(strJSONPath))
                            {
                                File.Delete(strJSONPath);
                            }
                            if (strPDFPath != "" && File.Exists(strPDFPath))
                            {
                                File.Delete(strPDFPath);
                            }
                            string compCode = _source?["rs0"]?[0]?["CompanyCode"]?.ToString() ?? "";
                            string documentType = _source?["rs0"]?[0]?["DocumentType"]?.ToString() ?? "";
                            string digitalid = _source?["rs0"]?[0]?["DigitalId"]?.ToString() ?? "";
                            string emailParamCode = _source?["rs0"]?[0]?["EmailParamCode"]?.ToString() ?? "";
                            string toEmailid = _source?["rs0"]?[0]?["ToEmailid"]?.ToString() ?? "";
                            string ccEmailid = _source?["rs0"]?[0]?["CcEmailid"]?.ToString() ?? "";
                            string emailsubject = _source?["rs0"]?[0]?["Emailsubject"]?.ToString() ?? "";
                            string bodyText = _source?["rs0"]?[0]?["BodyText"]?.ToString() ?? "";
                            string fileName = _source?["rs0"]?[0]?["FileName"]?.ToString() ?? "";
                            string SendFlag = _source?["rs0"]?[0]?["SendFlag"]?.ToString() ?? "";
                            string signedBy = _source?["rs0"]?[0]?["SignedBy"]?.ToString() ?? "";
                            string signedOn = _source?["rs0"]?[0]?["SignedOn"]?.ToString() ?? "";
                            string signedTm = _source?["rs0"]?[0]?["SignedTm"]?.ToString() ?? "";
                            string encPassword = _source?["rs0"]?[0]?["EncPassword"]?.ToString() ?? "";
                            string bounceReason = _source?["rs0"]?[0]?["BounceReason"]?.ToString() ?? "";
                            string sentDate = _source?["rs0"]?[0]?["SentDate"]?.ToString() ?? "";
                            string retryCount = _source?["rs0"]?[0]?["RetryCount"]?.ToString() ?? "";
                            string lastRetryDate = _source?["rs0"]?[0]?["LastRetryDate"]?.ToString() ?? "";
                            string priority = _source?["rs0"]?[0]?["Priority"]?.ToString() ?? "";
                            string duplicateYN = _source?["rs0"]?[0]?["DuplicateYN"]?.ToString() ?? "";
                            string mkrid = _source?["rs0"]?[0]?["Mkrid"]?.ToString() ?? "";

                            var ipAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString();
                            string clientHostName = string.Empty;
                            if (!string.IsNullOrEmpty(ipAddress))
                            {
                                try
                                {
                                    var hostEntry = Dns.GetHostEntry(ipAddress);
                                    clientHostName = hostEntry.HostName;
                                }
                                catch
                                {
                                    clientHostName = ipAddress; // fallback to IP
                                }
                            }
                            string strSQL = "INSERT INTO Digital_Emaildetails (";
                            strSQL += "dd_companycode,dd_clientcd,dd_dt,dd_DocumentType,dd_computername,dd_digitalid,dd_EmailParamCode,dd_toEmailid, " +
                                " dd_ccEmailid,dd_subject,dd_bodyText,dd_document,dd_Finaldocument,dd_filename,dd_SendFlag,dd_signedby,dd_signedon, " +
                                " dd_signedtm,dd_Encpassword,dd_BounceReason,dd_CreatedDate,dd_SentDate,dd_RetryCount,dd_LastRetryDate,dd_Priority," +
                                " dd_DuplicateYN,Mkrid)";
                            strSQL += "VALUES";
                            strSQL += "('" + compCode + "','" + clientId + "','" + DateTime.Now.ToString("yyyyMMdd") + "', '" + documentType + "'," +
                                  "  '" + clientHostName + "', '" + digitalid + "', '" + emailParamCode + "','" + toEmailid + "', '" + ccEmailid + "', " +
                                  "  '" + emailsubject + "', '" + bodyText + "', @proofJson, Convert(VarBinary(Max),''), '" + fileName + "', '" + SendFlag + "', " +
                                  "  '" + signedBy + "', '" + signedOn + "', '" + signedTm + "', '" + encPassword + "', '" + bounceReason + "', " +
                                  "  '" + DateTime.Now.ToString("yyyyMMdd") + "', '" + sentDate + "', " + retryCount + ", " + priority + ", " +
                                  "  '" + lastRetryDate + "','" + duplicateYN + "','" + mkrid + "'  )";

                            Dictionary<string, byte[]> sqlParams = new Dictionary<string, byte[]>();
                            sqlParams.Add("@proofJson", fileBytes);
                            objUtility.ExecuteSQL(strSQL, sqlParams);
                        }
                    }
                    catch (Exception) { }
                    finally
                    {
                        if (strPDFPath != "" && File.Exists(strPDFPath))
                        {
                            File.Delete(strPDFPath);
                        }
                        if (File.Exists(strJSONPath))
                        {
                            File.Delete(strJSONPath);
                        }
                    }
                }
                #endregion

                return "Success";
            }
            catch (Exception)
            {
                if (strJSONPath != "" && File.Exists(strJSONPath))
                {
                    File.Delete(strJSONPath);
                }
            }
            finally
            {
                if (strPDFPath != "" && File.Exists(strPDFPath))
                {
                    File.Delete(strPDFPath);
                }
            }
            return "Success";
        }

        public dynamic CompileExcelExport(string ReportName, string ReportDisplayName, DataSet dts)
        {
            byte[] fileBytes = new byte[] { };
            var pdfResponses = new List<PDFResponse> { };
            string strJSONPath = "";
            string strExcelPath = "", baseDir = "", strJSON = "";
            try
            {
                Guid id = Guid.NewGuid();
                string uniquename = "" + id;
                baseDir = AppDomain.CurrentDomain.BaseDirectory + "Typst";
                string strOutputPath = Path.Combine(baseDir, "Outputs");

                strJSON = JsonConvert.SerializeObject(dts, Formatting.Indented);

                strExcelPath = Path.Combine(strOutputPath, ReportName + "_" + uniquename + ".xlsx");

                /////////// *** Please comment below two lines before build release ***** (For debug code)
                //string jsonfilePath = Path.Combine(baseDir, "inputs", "Test_Json.json").Replace("\\", "/");  // HH
                //strJSON = File.ReadAllText(jsonfilePath); // HH

                if (!Directory.Exists(strOutputPath))
                    Directory.CreateDirectory(strOutputPath);


                dynamic jsonObject = JsonConvert.DeserializeObject<JObject>(strJSON);
                var jsonRS0 = jsonObject?["rs0"];

                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add(ReportDisplayName);

                    if (File.Exists(Path.Combine(baseDir, "Assets", "CompanyLogo.png")))
                    {
                        string logofilePath = Path.Combine(baseDir, "Assets", "CompanyLogo.png");
                        var image = ws.AddPicture(logofilePath)
                                      .MoveTo(ws.Cell("A1")) // position on the sheet
                                      .WithSize(100, 100);   // optio
                    }

                    int row = 9;
                    dynamic jsonCompHeader = jsonObject?["rs0"]?[0];
                    ///// **** Company Header Data *****
                    if (jsonRS0 == null || !jsonRS0.HasValues)
                    {
                        ws.Cell("C3").Value = " Report Header Not Available ";
                        ws.Range("C3:C3").Merge().Style
                            .Font.SetBold().Font.SetFontSize(13).Font.SetFontColor(XLColor.Red)
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    }
                    else
                    {
                        // === Company Name ===
                        ws.Cell("B1").Value = (string)jsonCompHeader.CompanyName;
                        ws.Range("B1:H1").Merge().Style
                            .Font.SetBold().Font.SetFontSize(14)
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                        // === Company Address ===
                        ws.Cell("B2").Value = (string)jsonCompHeader.Address1;
                        ws.Range("B2:H2").Merge().Style
                            .Font.SetFontSize(10)
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws.Cell("B3").Value = (string)jsonCompHeader.Address2;
                        ws.Range("B3:H3").Merge().Style
                            .Font.SetFontSize(10)
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws.Cell("B4").Value = (string)jsonCompHeader.Address3;
                        ws.Range("B4:H4").Merge().Style
                            .Font.SetFontSize(10)
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                        string billingAddress = (string)jsonCompHeader.BillingAddress;
                        ws.Cell("B5").Value = $"Billing Address{Environment.NewLine}{billingAddress}";
                        ws.Range("B5:H5").Merge().Style
                            .Font.SetFontSize(10)
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                        ws.Cell("B7").Value = (string)jsonCompHeader.ReportName;
                        ws.Range("B7:H7").Merge().Style
                             .Font.SetBold().Font.SetFontSize(12)
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                        // === Client Details ===

                        ws.Cell(row, 1).Value = "Client Code:";
                        ws.Cell(row, 2).Value = (string)jsonCompHeader.ClientCode;
                        row++;
                        ws.Cell(row, 1).Value = "Client Name:";
                        ws.Cell(row, 2).Value = (string)jsonCompHeader.Client;
                        row++;
                        ws.Cell(row, 1).Value = "Address:";
                        //ws.Cell(row, 2).Value = $"{(string)jsonCompHeader.ClientAddress1} {Environment.NewLine}{(string)jsonCompHeader.ClientAddress2}  {Environment.NewLine}{(string)jsonCompHeader.ClientAddress3} {Environment.NewLine}{(string)jsonCompHeader.ClientCity} {Environment.NewLine}{(string)jsonCompHeader.Clientpin}";
                        ////ws.Range("B11:D11").Merge();
                        ws.Range(11, 2, 11, 4).Merge();
                        ws.Cell(11, 2).Value = $"{(string)jsonCompHeader.ClientAddress1} {Environment.NewLine}{(string)jsonCompHeader.ClientAddress2}  {Environment.NewLine}{(string)jsonCompHeader.ClientAddress3} {Environment.NewLine}{(string)jsonCompHeader.ClientCity} {Environment.NewLine}{(string)jsonCompHeader.Clientpin}";
                        ws.Cell(11, 2).Style.Alignment.WrapText = true;
                        ws.Row(11).AdjustToContents();
                        ws.Row(11).Height = 75;
                    }
                    row += 2;

                    // === Table Header ===
                    int headerRow = row;

                    JArray rs1 = (JArray)jsonObject?["rs1"];
                    if (rs1 != null && rs1.Count > 0)
                    {

                        row++;
                        int colN = 0;
                        var jsonRS2 = jsonObject?["rs2"];
                        if (jsonRS2 == null || !jsonRS2.HasValues)
                        {
                            ws.Cell("A15").Value = " No Data Available ";
                            ws.Range("A15:H15").Merge().Style
                                .Font.SetBold().Font.SetFontSize(11).Font.SetFontColor(XLColor.Red)
                                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        }
                        else
                        {
                            // Get all visible column names
                            var headers = rs1
                            .Where(x => x["ColumnHide"]?.ToString() == "N") // Only ColumnHide = N
                            .Select(x => x["ColumnName"].ToString())
                            .ToList();
                            // Get all visible column names
                            var headerCols = rs1
                                .Where(x => x["ColumnHide"]?.ToString() == "N") // Only ColumnHide = N
                                .Select(x => x["ColumnHeading"].ToString())
                                .ToList();

                            // Get all decimal columns for float values.
                            var decimalCols = rs1
                                .Where(x => int.TryParse(x["DecimalPlace"]?.ToString(), out var dp) && dp > 0) // Only for integer column
                                .Select(x => x["ColumnName"].ToString()).ToList();

                            // Get all decimal columns for float values.
                            var formatCols = rs1
                                .Where(x => x["ColumnFormat"]?.ToString() != "") // For checkig date format
                                 .Select(x => new
                                 {
                                     ColumnName = x["ColumnName"]?.ToString(),
                                     ColumnFormat = x["ColumnFormat"]?.ToString()
                                 }).ToList();

                            // Convert to required string format:
                            int columnCnt = headers.Count;

                            for (int i = 0; i < columnCnt; i++)
                                ws.Cell(headerRow, i + 1).Value = headerCols[i];

                            ws.Range(headerRow, 1, headerRow, columnCnt).Style
                                .Font.SetBold()
                                .Fill.SetBackgroundColor(XLColor.LightGray)
                                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                            // === Table Data ===

                            foreach (var t in jsonRS2)
                            {
                                for (int i = 0; i < columnCnt; i++)
                                {
                                    colN = colN + 1;
                                    string colName = headers[i];
                                    if (decimalCols.Contains(colName))
                                    {
                                        //double colValue = double.TryParse(Convert.ToString(t[colName]), out double d) ? d : 0;
                                        double colValue = double.TryParse((string?)Convert.ToString(t[colName]) ?? string.Empty, NumberStyles.Any, CultureInfo.InvariantCulture, out double d) ? d : 0d;
                                        ws.Cell(row, colN).Value = colValue;
                                    }
                                    else if (formatCols.Select(f => f.ColumnName).ToList().Contains(colName))
                                    {
                                        string dateFormt = formatCols[0].ColumnFormat.ToString();
                                        string colValue = t[colName];
                                        if (colValue.Contains("-") || colValue.Contains("/"))
                                            colValue = DateTime.Parse(colValue).ToString("yyyyMMdd");
                                        if (!string.IsNullOrEmpty(colValue))
                                            colValue = dateFormt.Replace("dd", colValue.Substring(6, 2)).Replace("MM", colValue.Substring(4, 2)).Replace("yyyy", colValue.Substring(0, 4));
                                        ws.Cell(row, colN).Value = colValue;
                                    }
                                    else
                                    {
                                        string colValue = t[colName];
                                        ws.Cell(row, colN).Value = colValue;
                                    }
                                }
                                colN = 0;
                                row++;
                            }
                            // === Totals ===
                            var totals = rs1
                               .Where(x => x["ColumnTotal"]?.ToString() == "Y") // Only for Total column = Y
                               .Select(x => x["ColumnName"].ToString())
                               .ToList();
                            int totalColCnt = totals.Count;
                            if (totalColCnt > 0)
                            {
                                ws.Cell(row, 1).Value = "Total";
                                ws.Range(row, 1, row, columnCnt).Style
                                 .Font.SetBold();

                                for (int tl = 0; tl < totalColCnt; tl++)
                                {
                                    colN = 0;
                                    for (int i = 0; i < columnCnt; i++)
                                    {
                                        colN = colN + 1;
                                        if (totals[tl] == headers[i])
                                        {
                                            string colLetter = XLHelper.GetColumnLetterFromNumber(colN);
                                            ws.Cell(row, colN).FormulaA1 = $"=SUMPRODUCT(VALUE({colLetter}{headerRow + 1}:{colLetter}{row - 1}))";
                                            //  ws.Style.Font.SetBold();
                                            break;
                                        }
                                    }
                                }
                            }
                            // === Styling ===(lines)
                            ws.Range(headerRow, 1, row, columnCnt).Style
                                .Border.OutsideBorder = XLBorderStyleValues.Thin;
                            ws.Range(headerRow, 1, row, columnCnt).Style
                                .Border.InsideBorder = XLBorderStyleValues.Thin;
                        }
                    }
                    else
                    {
                        ws.Cell("A13").Value = " Column header not found ";
                        ws.Range("A13:H13").Merge().Style
                            .Font.SetBold().Font.SetFontSize(11).Font.SetFontColor(XLColor.Red)
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    }

                    // === Report Footer ===
                    row += 2; // leave one blank row after totals/data
                    string rptFooter = (string)jsonCompHeader.ReportFooter;
                    if (!string.IsNullOrEmpty(rptFooter))
                    {
                        // Convert HTML line breaks to Excel line breaks
                        rptFooter = rptFooter.Replace("<br>", Environment.NewLine)
                                               .Replace("<br/>", Environment.NewLine)
                                               .Replace("<br />", Environment.NewLine);

                        // Remove remaining HTML tags
                        rptFooter = Regex.Replace(rptFooter, "<.*?>", string.Empty);

                        ws.Cell(row, 1).Value = rptFooter;
                        ws.Range(row, 1, row, 12).Merge().Style
                            .Font.SetItalic()
                            .Font.SetFontSize(11)
                            .Alignment.SetVertical(XLAlignmentVerticalValues.Top)
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left)
                            .Alignment.WrapText = true;

                        // Better height calculation
                        int approxCharsPerLine = 90; // adjust based on merged width
                        int visualLines = (int)Math.Ceiling((double)rptFooter.Length / approxCharsPerLine);
                        // Count explicit line breaks too
                        visualLines += rptFooter.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Length - 1;
                        // Set row height
                        ws.Row(row).Height = Math.Max(20, visualLines * 15);
                    }

                    ws.Columns().AdjustToContents();

                    wb.SaveAs(strExcelPath);
                }

                fileBytes = File.ReadAllBytes(strExcelPath);
                pdfResponses.Add(new PDFResponse { PDFName = ReportName.Trim() + "_" + ReportDisplayName.Trim() + ".xlsx", Base64PDF = Convert.ToBase64String(fileBytes) });


                DataTable dt = new DataTable();
                dt.Columns.Add("ExcelFileName", typeof(string));
                dt.Columns.Add("Base64Excel", typeof(string));

                foreach (var pdf in pdfResponses)
                {
                    dt.Rows.Add(pdf.PDFName, pdf.Base64PDF);
                }
                DataSet dsRes = new DataSet();
                dsRes.Tables.Add(dt);
                dsRes.Tables[0].TableName = "rs0";

                return dsRes;
            }
            catch (Exception)
            {
                if (strJSONPath != "" && File.Exists(strJSONPath))
                {
                    File.Delete(strJSONPath);
                }
                throw;
            }
            finally
            {
                if (strExcelPath != "" && File.Exists(strExcelPath))
                {
                    File.Delete(strExcelPath);
                }
            }
        }

        public dynamic CompileTypstSendMail(string ReportName, string ReportDisplayName, DataSet dts, string reportType)
        {
            byte[] fileBytes = new byte[] { };
            var pdfResponses = new List<PDFResponse> { };
            string strJSONPath = "";
            string strPDFPath = "", baseDir = "", strJSON = "";
            try
            {
                Guid id = Guid.NewGuid();
                string uniquename = "" + id;
                string strJSONRelPath = "";
                string strTypstCommand = "";
                strJSON = JsonConvert.SerializeObject(dts, Formatting.Indented);
                baseDir = AppDomain.CurrentDomain.BaseDirectory + "Typst";
                string strTypstExePath = Path.Combine(baseDir, "typst.exe");
                string strInputPath = Path.Combine(baseDir, "inputs");
                string strOutputPath = Path.Combine(baseDir, "outputs");

                string successMsg = "", errorMsg = "", typstFileName = reportType;
                DataTable dt = new DataTable();
                dt.Columns.Add("MessageType", typeof(string));
                dt.Columns.Add("MessageText", typeof(string));

                objUtility.TypstCreateCompanyLogo(baseDir);

                if (!Directory.Exists(strInputPath))
                    Directory.CreateDirectory(strInputPath);

                if (!Directory.Exists(strOutputPath))
                    Directory.CreateDirectory(strOutputPath);

                /////////// *** Please comment below two lines before build release ***** (For debug code)
                //string jsonfilePath = Path.Combine(baseDir, "inputs", "Test_Json.json").Replace("\\", "/");  // HH
                //strJSON = File.ReadAllText(jsonfilePath); // HH

                JObject _source = JObject.Parse(strJSON);
                var len = _source.Count;
                var distinctClientCodes = _source["rs0"]
                    .Select(c => (string)c["ClientCode"])
                    .Distinct()
                    .ToList();

                #region
                foreach (var clientId in distinctClientCodes)
                {
                    //// For first Header json
                    var rs0 = _source["rs0"].Where(r => r["ClientCode"] != null && r["ClientCode"].ToString().Trim() == clientId);
                    // Create new JObject with filtered data
                    JObject jobjRs0 = new JObject
                    {
                        ["rs0"] = new JArray(rs0)
                    };
                    // Convert back to JSON string
                    //string rs0Json = jobjRs0.ToString(Formatting.Indented);

                    ////// For Property json
                    JObject objRs1 = new JObject();
                    JObject objRs2 = new JObject();
                    string formatGrid = _source["rs0"][0]["FormatGrid"].ToString();
                    string[] propGrid = formatGrid.Split(",");
                    int r2 = 0;

                    if (reportType == "AlertForIncomeUpdation" || reportType == "AlertForDormant")
                    {
                        var rs1 = _source["rs1"].Where(r => r["ClientCode"] != null && r["ClientCode"].ToString().Trim() == clientId.Trim());
                        // Create new JObject with filtered data
                        JObject jobjRs1 = new JObject
                        {
                            ["rs1"] = new JArray(rs1)
                        };
                        jobjRs0.Merge(jobjRs1, new JsonMergeSettings
                        {
                            MergeArrayHandling = MergeArrayHandling.Concat
                        });
                        typstFileName = "AlertForIncomeDormant";
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(formatGrid))
                        {
                            foreach (string grid in propGrid)
                            {
                                string rs1Val = "rs" + grid[grid.Length - 1];
                                var rs1 = _source[rs1Val].Where(r => r["ColumnName"] != null);
                                JObject obj1 = new JObject
                                {
                                    [rs1Val] = new JArray(rs1)
                                };
                                objRs1.Merge(obj1, new JsonMergeSettings
                                {
                                    MergeArrayHandling = MergeArrayHandling.Concat
                                });
                                r2 = Convert.ToInt32("" + rs1Val[rs1Val.Length - 1]);
                            }
                        }
                        ////// For Data json

                        for (int i = r2 + 1; i < len; i++)
                        {
                            string rs2Val = "rs" + i;
                            var rs2 = _source[rs2Val].Where(r => r["ClientCode"] != null && r["ClientCode"].ToString().Trim() == clientId);
                            // Create new JObject with filtered data
                            JObject obj2 = new JObject
                            {
                                [rs2Val] = new JArray(rs2)
                            };
                            objRs2.Merge(obj2, new JsonMergeSettings
                            {
                                MergeArrayHandling = MergeArrayHandling.Concat
                            });
                        }

                        //// Merge json
                        jobjRs0.Merge(objRs1, new JsonMergeSettings
                        {
                            MergeArrayHandling = MergeArrayHandling.Concat
                        });
                        jobjRs0.Merge(objRs2, new JsonMergeSettings
                        {
                            MergeArrayHandling = MergeArrayHandling.Concat
                        });
                    }
                    string finalJSON = jobjRs0.ToString();

                    ///// Start Typst code          
                    strPDFPath = Path.Combine(strOutputPath, ReportName + "_SendMail" + clientId + ".pdf");
                    strJSONPath = CreateJsonConfig(finalJSON, ReportName + "_SendMail" + clientId + ".json");
                    strTypstCommand = $"\"{Path.Combine(baseDir, "templates", typstFileName + ".typ")}\" \"{strPDFPath}\"";
                    strJSONRelPath = Path.Combine("..", "inputs", ReportName + "_SendMail" + clientId + ".json").Replace("\\", "/");

                    string arguments = $"compile {strTypstCommand} --input file_path={strJSONRelPath} --root \"{baseDir}\"";
                    try
                    {
                        var processStartInfo = new ProcessStartInfo
                        {
                            FileName = strTypstExePath,
                            Arguments = arguments,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        using var process = Process.Start(processStartInfo);
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        process.WaitForExit();

                        if (process.ExitCode != 0)
                        {
                            errorMsg = errorMsg + " Error for client: " + clientId + " in Typst <br> ";
                            //throw new Exception($"Typst compile failed with exit code {process.ExitCode}: {error}");
                            //throw new Exception(errorMsg = errorMsg + " Typst compile failed for client " + clientId + " Please check error: " + error + " \\n");
                        }

                        fileBytes = File.ReadAllBytes(strPDFPath);

                        // var model2 = JsonConvert.DeserializeObject<EmailTemplateModel>(finalJSON);
                        string compCode = _source?["rs1"]?[0]?["CompanyCode"]?.ToString() ?? "";
                        string documentType = _source?["rs1"]?[0]?["DocumentType"]?.ToString() ?? "";
                        string digitalid = _source?["rs1"]?[0]?["DigitalId"]?.ToString() ?? "";
                        string emailParamCode = _source?["rs1"]?[0]?["EmailParamCode"]?.ToString() ?? "";
                        string toEmailid = _source?["rs1"]?[0]?["ToEmailid"]?.ToString() ?? "";
                        string ccEmailid = _source?["rs1"]?[0]?["CcEmailid"]?.ToString() ?? "";
                        string emailsubject = _source?["rs1"]?[0]?["Emailsubject"]?.ToString() ?? "";
                        string bodyText = _source?["rs1"]?[0]?["EmailBodyText"]?.ToString() ?? "";
                        string fileName = _source?["rs1"]?[0]?["FileName"]?.ToString() ?? "";
                        string SendFlag = _source?["rs1"]?[0]?["SendFlag"]?.ToString() ?? "";
                        string signedBy = _source?["rs1"]?[0]?["SignedBy"]?.ToString() ?? "";
                        string signedOn = _source?["rs1"]?[0]?["SignedOn"]?.ToString() ?? "";
                        string signedTm = _source?["rs1"]?[0]?["SignedTm"]?.ToString() ?? "";
                        string encPassword = _source?["rs1"]?[0]?["EncPassword"]?.ToString() ?? "";
                        string bounceReason = _source?["rs1"]?[0]?["BounceReason"]?.ToString() ?? "";
                        string sentDate = _source?["rs1"]?[0]?["SentDate"]?.ToString() ?? "";
                        string retryCount = _source?["rs1"]?[0]?["RetryCount"]?.ToString() ?? "0";
                        string lastRetryDate = _source?["rs1"]?[0]?["LastRetryDate"]?.ToString() ?? "";
                        string priority = _source?["rs1"]?[0]?["Priority"]?.ToString() ?? "0";
                        string duplicateYN = _source?["rs1"]?[0]?["DuplicateYN"]?.ToString() ?? "";
                        string mkrid = _source?["rs1"]?[0]?["Mkrid"]?.ToString() ?? "";

                        var ipAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString();
                        string clientHostName = string.Empty;
                        if (!string.IsNullOrEmpty(ipAddress))
                        {
                            try
                            {
                                var hostEntry = Dns.GetHostEntry(ipAddress);
                                clientHostName = hostEntry.HostName;
                            }
                            catch
                            {
                                clientHostName = ipAddress; // fallback to IP
                            }
                        }
                        string strSQL = "INSERT INTO Digital_Emaildetails (";
                        strSQL += "dd_companycode,dd_clientcd,dd_dt,dd_DocumentType,dd_computername,dd_digitalid,dd_EmailParamCode,dd_toEmailid, " +
                            " dd_ccEmailid,dd_subject,dd_bodyText,dd_document,dd_Finaldocument,dd_filename,dd_SendFlag,dd_signedby,dd_signedon, " +
                            " dd_signedtm,dd_Encpassword,dd_BounceReason,dd_CreatedDate,dd_SentDate,dd_RetryCount,dd_LastRetryDate,dd_Priority," +
                            " dd_DuplicateYN,Mkrid)";
                        strSQL += "VALUES";
                        strSQL += "('" + compCode + "','" + clientId + "','" + DateTime.Now.ToString("yyyyMMdd") + "', '" + documentType + "'," +
                              "  '" + clientHostName + "', '" + digitalid + "', '" + emailParamCode + "','" + toEmailid + "', '" + ccEmailid + "', " +
                              "  '" + emailsubject + "', '" + bodyText + "', @proofJson, Convert(VarBinary(Max),''), '" + fileName + "', '" + SendFlag + "', " +
                              "  '" + signedBy + "', '" + signedOn + "', '" + signedTm + "', '" + encPassword + "', '" + bounceReason + "', " +
                              "  '" + DateTime.Now.ToString("yyyyMMdd") + "', '" + sentDate + "', " + retryCount + ", " + priority + ", " +
                              "  '" + lastRetryDate + "','" + duplicateYN + "','" + mkrid + "'  )";

                        Dictionary<string, byte[]> sqlParams = new Dictionary<string, byte[]>();
                        sqlParams.Add("@proofJson", fileBytes);
                        objUtility.ExecuteSQL(strSQL, sqlParams);
                        successMsg = successMsg + " Record inserted for client " + clientId + " <br>";
                        if (File.Exists(strJSONPath))
                        {
                            File.Delete(strJSONPath);
                        }
                        if (strPDFPath != "" && File.Exists(strPDFPath))
                        {
                            File.Delete(strPDFPath);
                        }
                    }
                    catch (Exception)
                    {
                        //errorMsg = errorMsg + " Error for client: " + clientId + "  " + e.Message.ToString() + " \\n";
                        errorMsg = errorMsg + " Error for client: " + clientId + " in catch  <br> ";
                    }
                    finally
                    {
                        if (strPDFPath != "" && File.Exists(strPDFPath))
                        {
                            File.Delete(strPDFPath);
                        }
                        if (File.Exists(strJSONPath))
                        {
                            File.Delete(strJSONPath);
                        }
                    }
                }
                #endregion
                dt.Rows.Add("SUCCESS", successMsg);
                dt.Rows.Add("ERROR", errorMsg);
                DataSet dsRes = new DataSet();
                dsRes.Tables.Add(dt);
                dsRes.Tables[0].TableName = "rs0";
                return dsRes;
            }
            catch (Exception ex)
            {
                if (strJSONPath != "" && File.Exists(strJSONPath))
                {
                    File.Delete(strJSONPath);
                }
                throw new Exception(ex.Message.ToString());
            }
            finally
            {
                if (strPDFPath != "" && File.Exists(strPDFPath))
                {
                    File.Delete(strPDFPath);
                }
            }

        }

        private dynamic AppendImagePdfTypst(string filePath, string jsonData, string uniqueGuid)
        {
            var desJson = JsonConvert.DeserializeObject<dynamic>(jsonData);
            string clientCode = desJson[0]["ClientCode"]?.ToString();
            // Get the "Attachments" object
            var attachments = desJson[0]["Attachments"];
            // Loop through the dictionary
            foreach (JObject item in attachments)
            {
                string imageType = item["ImageType"]?.ToString();
                string base64 = item["base64Image"]?.ToString();
                if (!string.IsNullOrEmpty(base64))
                {
                    string fileExt = GetFileExtTypst(base64);
                    string imageName = imageType + "_" + clientCode + fileExt;
                    //set the image path
                    string imgPath = Path.Combine(filePath + "/Assets", imageName);

                    byte[] imageBytes = Convert.FromBase64String(base64);
                    if (fileExt == ".pdf")
                    {
                        imageBytes = NormalizePdf(imageBytes);
                    }
                    File.WriteAllBytes(imgPath, imageBytes);

                    item["base64Image"] = fileExt;
                }
            }
            return desJson;
        }

        public byte[] NormalizePdf(byte[] inputPdf)
        {
            using (var inputStream = new MemoryStream(inputPdf))
            using (var outputStream = new MemoryStream())
            {
                var reader = new PdfReader(inputStream);
                var stamper = new PdfStamper(reader, outputStream);

                stamper.Close();
                reader.Close();

                return outputStream.ToArray();
            }
        }

        public static void AppenePdfAttachmentInFinalPDF(string filePath, string jsonData, string existingFile, ref string outputPdfPath)
        {
            var desJson = JsonConvert.DeserializeObject<dynamic>(jsonData);
            string clientCode = desJson[0]["ClientCode"]?.ToString();
            var attachments = desJson[0]["Attachments"];
            outputPdfPath = Path.Combine(outputPdfPath, "FinalRekycPdf_" + clientCode + ".pdf");

            using (FileStream stream = new FileStream(outputPdfPath, FileMode.Create))
            using (Document document = new Document())
            using (PdfCopy copy = new PdfCopy(document, stream))
            {
                document.Open();

                PdfReader reader1 = new PdfReader(existingFile);
                for (int i = 1; i <= reader1.NumberOfPages; i++)
                {
                    copy.AddPage(copy.GetImportedPage(reader1, i));
                }
                reader1.Close();

                foreach (JObject item in attachments)
                {
                    string imageType = item["ImageType"]?.ToString();
                    string base64 = item["base64Image"]?.ToString();
                    if (base64 == ".pdf")
                    {
                        string fileName = imageType + "_" + clientCode + ".pdf";
                        string pdfFilePath = Path.Combine(filePath + "/Assets", fileName);

                        PdfReader reader = new PdfReader(pdfFilePath);
                        try
                        {
                            for (int i = 1; i <= reader.NumberOfPages; i++)
                            {
                                copy.AddPage(copy.GetImportedPage(reader, i));
                            }
                        }
                        finally
                        {
                            reader.Close();
                        }
                    }
                }
            }
        }

        private dynamic DeleteTypstFiles(string filePath, string jsonData)
        {
            var desJson = JsonConvert.DeserializeObject<dynamic>(jsonData);
            string clientCode = desJson[0]["ClientCode"]?.ToString();
            // Get the "Attachments" object
            var attachments = desJson[0]["Attachments"];
            // Loop through the dictionary
            foreach (JObject item in attachments)
            {
                string imageType = item["ImageType"]?.ToString();
                string fileExt = item["base64Image"]?.ToString();
                string imageName = imageType + "_" + clientCode + fileExt;
                string imgPath = Path.Combine(filePath + "/Assets", imageName);
                if (imgPath != "" && File.Exists(imgPath))
                {
                    File.Delete(imgPath);
                }
            }
            return true;
        }

        private dynamic AppendKRAimgTypst(string filePath, string jsonData)
        {
            var desJson = JsonConvert.DeserializeObject<dynamic>(jsonData);
            string clientCode = desJson["PAN"]?.ToString();
            // Get the "Attachments" object
            var adharImg = desJson["AadhaarAtt"]?.ToString();
            if (!string.IsNullOrEmpty(adharImg))
            {
                string fileExt = GetFileExtTypst(adharImg);
                string imageName = "Aadhar_Img_" + clientCode + fileExt;
                string imgPath = Path.Combine(filePath + "/Assets", imageName);
                byte[] imageBytes = Convert.FromBase64String(adharImg);
                File.WriteAllBytes(imgPath, imageBytes);
                desJson["AadhaarAtt"] = fileExt;
            }
            var panImg = desJson["panAtt"]?.ToString();
            if (!string.IsNullOrEmpty(panImg))
            {
                string fileExt = GetFileExtTypst(panImg);
                string imageName = "PAN_Img_" + clientCode + fileExt;
                string imgPath = Path.Combine(filePath + "/Assets", imageName);
                byte[] imageBytes = Convert.FromBase64String(panImg);
                File.WriteAllBytes(imgPath, imageBytes);
                desJson["panAtt"] = fileExt;
            }
            var signImg = desJson["SignatureIMG"]?.ToString();
            if (!string.IsNullOrEmpty(signImg))
            {
                string fileExt = GetFileExtTypst(signImg);
                string imageName = "Sign_Img_" + clientCode + fileExt;
                string imgPath = Path.Combine(filePath + "/Assets", imageName);
                byte[] imageBytes = Convert.FromBase64String(signImg);
                File.WriteAllBytes(imgPath, imageBytes);
                desJson["SignatureIMG"] = fileExt;
            }
            var photoImg = desJson["PhotoImg"]?.ToString();
            if (!string.IsNullOrEmpty(photoImg))
            {
                string fileExt = GetFileExtTypst(photoImg);
                string imageName = "Photo_Img_" + clientCode + fileExt;
                string imgPath = Path.Combine(filePath + "/Assets", imageName);
                byte[] imageBytes = Convert.FromBase64String(photoImg);
                File.WriteAllBytes(imgPath, imageBytes);
                desJson["PhotoImg"] = fileExt;
            }
            return desJson;
        }

        private dynamic AppendClosureImgTypst(string filePath, string jsonData)
        {
            var desJson = JsonConvert.DeserializeObject<dynamic>(jsonData);
            string clientCode = desJson["UCC"]?.ToString();
            var cmr = desJson["CMRAttachment"]?.ToString();
            if (!string.IsNullOrEmpty(cmr))
            {
                string fileExt = GetFileExtTypst(cmr);
                string imageName = "CMRAttachment_" + clientCode + fileExt;
                string imgPath = Path.Combine(filePath + "/Assets", imageName);
                byte[] imageBytes = Convert.FromBase64String(cmr);
                File.WriteAllBytes(imgPath, imageBytes);
                desJson["CMRAttachment"] = fileExt;
            }
            return desJson;
        }

        private dynamic DeleteKRATypstFiles(string filePath, string jsonData)
        {
            var desJson = JsonConvert.DeserializeObject<dynamic>(jsonData);
            string clientCode = desJson["PAN"]?.ToString();

            var adharImg = desJson["AadhaarAtt"]?.ToString();
            string adharName = "Aadhar_Img_" + clientCode + adharImg;
            string adharPath = Path.Combine(filePath + "/Assets", adharName);
            if (adharPath != "" && File.Exists(adharPath))
            {
                File.Delete(adharPath);
            }
            var panImg = desJson["panAtt"]?.ToString();
            string panName = "PAN_Img_" + clientCode + panImg;
            string panPath = Path.Combine(filePath + "/Assets", panName);
            if (panPath != "" && File.Exists(panPath))
            {
                File.Delete(panPath);
            }
            var signImg = desJson["SignatureIMG"]?.ToString();
            string signName = "Sign_Img_" + clientCode + signImg;
            string signPath = Path.Combine(filePath + "/Assets", signName);
            if (signPath != "" && File.Exists(signPath))
            {
                File.Delete(signPath);
            }
            var photoImg = desJson["PhotoImg"]?.ToString();
            string phtoName = "Photo_Img_" + clientCode + photoImg;
            string phtoPath = Path.Combine(filePath + "/Assets", phtoName);
            if (phtoPath != "" && File.Exists(phtoPath))
            {
                File.Delete(phtoPath);
            }

            return true;
        }

        private dynamic DeleteClosureTypstFiles(string filePath, string jsonData)
        {
            var desJson = JsonConvert.DeserializeObject<dynamic>(jsonData);
            string clientCode = desJson["UCC"]?.ToString();
            var adharImg = desJson["CMRAttachment"]?.ToString();
            string adharName = "CMRAttachment_" + clientCode + adharImg;
            string adharPath = Path.Combine(filePath + "/Assets", adharName);
            if (adharPath != "" && File.Exists(adharPath))
            {
                File.Delete(adharPath);
            }
            return true;
        }

        private string GetFileExtTypst(string base64)
        {
            var data = base64.Substring(0, 5);

            switch (data.ToUpper())
            {
                case "IVBOR":
                    return ".png";
                case "/9J/4":
                    return ".jpg";
                case "AAAAF":
                    return ".mp4";
                case "JVBER":
                    return ".pdf";
                case "AAABA":
                    return ".ico";
                case "UMFYI":
                    return ".rar";
                case "E1XYD":
                    return ".rtf";
                case "U1PKC":
                    return ".txt";
                default:
                    return ".jpg";
            }
        }

        public dynamic DownloadMultiplePdfZip(string ReportName, string ReportDisplayName, DataSet dts, string pdfType)
        {
            byte[] fileBytes = new byte[] { };
            var pdfResponses = new List<PDFResponse> { };
            string strJSONPath = "";
            string strPDFPath = "", baseDir = "", strJSON = "";
            try
            {
                Guid id = Guid.NewGuid();
                string uniquename = "" + id;
                string strJSONRelPath = "";
                string strTypstCommand = "";
                //strJSON = JsonConvert.SerializeObject(dts, Formatting.Indented);
                baseDir = AppDomain.CurrentDomain.BaseDirectory + "Typst";
                string strTypstExePath = Path.Combine(baseDir, "typst.exe");
                string strInputPath = Path.Combine(baseDir, "Inputs");
                string strOutputPath = Path.Combine(baseDir, "Outputs");

                #region old logo logic
                /*if (!File.Exists(Path.Combine(baseDir, "Assets", "CompanyLogo.png")))
                {
                    strsql = "Select img_code, img_logo logo from Images with (nolock) where img_desc = 'Company Logo' and img_logo is not null order by img_code";
                    DataTable dtLogo = objUtility.OpenDataTable(strsql);
                    if (dtLogo.Rows.Count > 0)
                    {
                        byte[] logoBytes = (byte[])dtLogo.Rows[0]["logo"];
                        using (var ms = new MemoryStream(logoBytes))
                        {
                            try
                            {
                                using (System.Drawing.Image image = System.Drawing.Image.FromStream(ms))
                                {
                                    string logofilePath = Path.Combine(baseDir, "Assets", "CompanyLogo.png");
                                    image.Save(logofilePath, System.Drawing.Imaging.ImageFormat.Png);  // Save as PNG
                                }
                            }
                            catch (Exception ex)
                            {
                                // This happens if the image bytes are already compressed PNG or JPEG
                                string fallbackPath = Path.Combine(baseDir, "Assets", "CompanyLogo.png");
                                File.WriteAllBytes(fallbackPath, logoBytes); // just dump it
                            }
                        }
                        //string logofilePath = Path.Combine(baseDir, "assets", "CompanyLogo.BMP");
                        //File.WriteAllBytes(logofilePath, logoBytes);
                    }
                }*/
                #endregion

                objUtility.TypstCreateCompanyLogo(baseDir);

                if (!Directory.Exists(strInputPath))
                    Directory.CreateDirectory(strInputPath);

                if (!Directory.Exists(strOutputPath))
                    Directory.CreateDirectory(strOutputPath);

                /////////// *** Please comment below two lines before build release ***** (For debug code)
                //string jsonfilePath = Path.Combine(baseDir, "inputs", "TradeWeb_CKYC_Ind.json").Replace("\\", "/");  // HH
                //strJSON = File.ReadAllText(jsonfilePath);

                strPDFPath = Path.Combine(strOutputPath, ReportName + "_" + uniquename + ".pdf");
                strJSONRelPath = Path.Combine("..", "Inputs", ReportName + "_" + uniquename + ".json").Replace("\\", "/");

                if (pdfType == "KRAPDF")
                {
                    strTypstCommand = "\"" + Path.Combine(baseDir, "templates", "TypstKRA.typ") + "\"" + "  " + "\"" + strPDFPath + "\"";
                    strJSON = dts.Tables[0].Rows[0][0].ToString();

                    var desObj = AppendKRAimgTypst(baseDir, strJSON);
                    strJSON = JsonConvert.SerializeObject(desObj);
                }
                else if (pdfType == "FINALPDF")
                {
                    //strJSONRelPath = "\"" + Path.Combine("..", "inputs", ReportName + "_Json.json").Replace("\\", "/") + "\"";
                    strTypstCommand = "\"" + Path.Combine(baseDir, "templates", "TypstRekyc.typ") + "\"" + "  " + "\"" + strPDFPath + "\"";
                    strJSON = dts.Tables[0].Rows[0][0].ToString();

                    var desObj = AppendImagePdfTypst(baseDir, strJSON, uniquename);
                    strJSON = JsonConvert.SerializeObject(desObj);
                }
                else if (pdfType == "CLOSUREPDF")
                {
                    strTypstCommand = "\"" + Path.Combine(baseDir, "templates", "TypstAccountClosure.typ") + "\"" + "  " + "\"" + strPDFPath + "\"";
                    strJSON = dts.Tables[0].Rows[0][0].ToString();

                    var desObj = AppendClosureImgTypst(baseDir, strJSON);
                    strJSON = JsonConvert.SerializeObject(desObj);
                }
                else if (pdfType == "CKYCINDV")
                {
                    strTypstCommand = "\"" + Path.Combine(baseDir, "templates", "TypstCKYC_Individual.typ") + "\"" + "  " + "\"" + strPDFPath + "\"";
                    //strJSON = dts.Tables[0].Rows[0][0].ToString();

                    //var desObj = AppendClosureImgTypst(baseDir, strJSON);
                    //strJSON = JsonConvert.SerializeObject(desObj);
                }
                else
                {
                    //string jsonfilePath = Path.Combine(baseDir, "inputs", ReportName + "_Json.json").Replace("\\", "/");
                    //strJSON = File.ReadAllText(jsonfilePath);
                    strTypstCommand = $"\"{Path.Combine(baseDir, "templates", "common.typ")}\" \"{strPDFPath}\"";
                }
                //strJSONPath = CreateJsonConfig(strJSON, ReportName + "_" + uniquename + ".json");
                //strJSONPath = CreateJsonConfig(strJSON, ReportName + "_" + "Json" + ".json");
                strJSONRelPath = "\"" + Path.Combine("..", "inputs", "TradeWeb_CKYC_Ind_Json.json").Replace("\\", "/") + "\"";
                string arguments = $"compile {strTypstCommand} --input file_path={strJSONRelPath} --root \"{baseDir}\"";

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = strTypstExePath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(processStartInfo);
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"Typst compile failed with exit code {process.ExitCode}: {error}");
                }
                if (File.Exists(strJSONPath))
                {
                    // File.Delete(strJSONPath);
                }

                fileBytes = File.ReadAllBytes(strPDFPath);
                pdfResponses.Add(new PDFResponse { PDFName = ReportName.Trim() + "_" + ReportDisplayName.Trim() + ".pdf", Base64PDF = Convert.ToBase64String(fileBytes) });

                DataTable dt = new DataTable();
                dt.Columns.Add("PDFName", typeof(string));
                dt.Columns.Add("Base64PDF", typeof(string));
                dt.Columns.Add("TotalPages", typeof(int));

                foreach (var pdf in pdfResponses)
                {
                    PdfReader reader = new PdfReader(fileBytes); // or file path
                    int pageCount = reader.NumberOfPages;
                    reader.Close();
                    dt.Rows.Add(pdf.PDFName, pdf.Base64PDF, pageCount);
                }
                DataSet dsRes = new DataSet();
                dsRes.Tables.Add(dt);
                dsRes.Tables[0].TableName = "rs0";

                return dsRes;
            }
            catch (Exception)
            {
                if (strJSONPath != "" && File.Exists(strJSONPath))
                {
                    // File.Delete(strJSONPath);
                }
                throw;
            }
            finally
            {
                if (strPDFPath != "" && File.Exists(strPDFPath))
                {
                    // File.Delete(strPDFPath);
                }
                if (pdfType == "KRAPDF")
                {
                    DeleteKRATypstFiles(baseDir, strJSON);
                }
                else if (pdfType == "KRAPDF")
                {
                    DeleteTypstFiles(baseDir, strJSON);
                }
                else if (pdfType == "CLOSUREPDF")
                {
                    DeleteClosureTypstFiles(baseDir, strJSON);
                }
            }
        }

        private dynamic CommonTypstPdfPSI(string arguments, string strPDFPath, string strTypstExePath, string strJSONPath)
        {
            var psi = new ProcessStartInfo
            {
                FileName = strTypstExePath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Typst compile failed with exit code {process.ExitCode}: {error}");
            }
            if (File.Exists(strJSONPath))
            {
                File.Delete(strJSONPath);
            }
            return File.ReadAllBytes(strPDFPath);
        }

        public string CreateJsonConfig(string jsonobject, string filename)
        {
            string basePath = AppContext.BaseDirectory;
            string outputDir = Path.Combine(basePath, "Typst", "Inputs");

            Directory.CreateDirectory(outputDir);

            string jsonFilePath = Path.Combine(outputDir, filename);

            File.WriteAllText(jsonFilePath, jsonobject);
            return jsonFilePath;
        }
        #endregion

        public dynamic PostData(string strRequest)
        {
            string strResText = objUtility.GetWebParameter("SARALFPRES");
            try
            {
                Dictionary<string, string> dictValues = new Dictionary<string, string>();
                dynamic objReq = JsonConvert.DeserializeObject<dynamic>(strRequest);
                bool blnBankCol = Convert.ToInt16(objUtility.fnFireQueryTradeWeb("Information_Schema.Columns", "count(*)", "Table_Name='FundsRequest' and Column_Name", "rq_bankactno", true)) > 0;

                DataTable dtTemp = objUtility.OpenDataTable("SELECT isnull (IDENT_CURRENT('FundsRequest'),0)");
                long intcnt = 0;
                bool blnIdentityOn;
                if (Convert.ToInt64(dtTemp.Rows[0][0]) > 0)
                {
                    blnIdentityOn = true;
                    DataSet DsReqId = new DataSet();
                    DsReqId = objUtility.OpenDataSet("SELECT IDENT_CURRENT('FundsRequest')");
                    intcnt = Convert.ToInt64(DsReqId.Tables[0].Rows[0][0]);
                }
                else
                {
                    blnIdentityOn = false;
                    DataSet Dsmax = new DataSet();
                    Dsmax = objUtility.OpenDataSet("SELECT isNull(Max(Rq_srNo),0) from FundsRequest");
                    intcnt = Convert.ToInt64(Dsmax.Tables[0].Rows[0][0]) + 1;
                }

                string strClient = "";
                decimal dblAmount = 0;
                string strDate = "";
                string strParmValue = objUtility.GetWebParameter("SARALFPREQ");
                if (strParmValue != "")
                {
                    List<FieldMapping> mappings = JsonConvert.DeserializeObject<List<FieldMapping>>(strParmValue);

                    foreach (var map in mappings)
                    {
                        if (!objReq.TryGetValue(map.JsonKey, StringComparison.OrdinalIgnoreCase, out JToken valueToken))
                            continue;

                        string paramName = map.Column;
                        object value = DBNull.Value;

                        if (!string.IsNullOrWhiteSpace(valueToken?.ToString()))
                        {
                            if (paramName.ToUpper() == "CLIENT")
                            {
                                strClient = valueToken.ToString();
                            }
                            else if (paramName.ToUpper() == "AMOUNT")
                            {
                                dblAmount = Convert.ToDecimal(valueToken.ToString());
                            }
                            else if (paramName.ToUpper() == "DATE")
                            {
                                var dt = DateTime.ParseExact(
                                        valueToken.ToString(),
                                        map.Format ?? "dd/MM/yyyy",
                                        CultureInfo.InvariantCulture);

                                strDate = dt.ToString("yyyyMMdd");
                            }
                        }
                    }

                    if (string.IsNullOrWhiteSpace(strClient) || dblAmount == 0 || string.IsNullOrWhiteSpace(strDate))
                    {
                        strResText = strResText.Replace("<SUCCESS>", "False")
                            .Replace("<SUCCESSDES>", "")
                            .Replace("<ERROR>", "Input_Value_Validation")
                            .Replace("<ERRORDES>", "Invalid Input Values");
                        return new { Status = false, Data = strResText };
                    }

                    string StrConn = objUtility.GetConnectionStr();
                    string insertSql = "INSERT INTO FundsRequest (" + (blnIdentityOn ? "" : "Rq_SrNo,") + " Rq_Clientcd, Rq_Type, Rq_Amount, Rq_IpAddress, Rq_Date, Rq_Time, Rq_Satus1, Rq_Satus2, Rq_Satus3, Rq_Satus4, Rq_Note" + (blnBankCol ? ", Rq_bankactno" : "") + ") " +
                                       "VALUES (" + (blnIdentityOn ? "" : "@SrNo,") + " @Client, 'A', @Amount, 'API', @Date, convert(char(8),getdate(),108), 'P', 'P', 'P', @Status4, ''" + (blnBankCol ? ", ''" : "") + ")";
                    using (var conn = new SqlConnection(StrConn))
                    {
                        using (SqlCommand command = new SqlCommand(insertSql, conn))
                        {
                            if (!blnIdentityOn)
                            {
                                command.Parameters.Add(new SqlParameter("@SrNo", intcnt));
                            }
                            command.Parameters.Add(new SqlParameter("@Client", strClient));
                            command.Parameters.Add(new SqlParameter("@Amount", dblAmount));
                            command.Parameters.Add(new SqlParameter("@Date", strDate));
                            command.Parameters.Add(new SqlParameter("@Status4", objUtility.Encrypt(strDate)));
                            conn.Open();
                            command.ExecuteNonQuery();
                        }
                    }

                    strResText = strResText.Replace("<SUCCESS>", "True")
                        .Replace("<SUCCESSDES>", "Voucher No. - " + intcnt)
                        .Replace("<ERROR>", "")
                        .Replace("<ERRORDES>", "");
                    return new { Status = true, Data = strResText };
                }
                else
                {
                    strResText = strResText.Replace("<SUCCESS>", "False")
                        .Replace("<SUCCESSDES>", "")
                        .Replace("<ERROR>", "Database_Exception")
                        .Replace("<ERRORDES>", "Parameter value is blank");
                    return new { Status = false, Data = strResText };
                }
            }
            catch (Exception ex)
            {
                strResText = strResText.Replace("<SUCCESS>", "False")
                    .Replace("<SUCCESSDES>", "");
                if (ex is SqlException sqlEx)
                {
                    strResText = strResText.Replace("<ERROR>", "Database_Exception")
                        .Replace("<ERRORDES>", "SQL Error: " + sqlEx.Message);
                }
                else
                {
                    strResText = strResText.Replace("<ERROR>", "Exception")
                        .Replace("<ERRORDES>", "Error: " + ex.Message);
                }
                return new { Status = false, Data = strResText };
            }
        }

        public async Task<dynamic> Login(string UserId, string Password, string EPassword, string Key, string LoginAs, string Product, string ICPV, string Feature, string isSSO, string IPAdd = "")
        {
            ConvertData returnJson = new ConvertData();
            DataTable returnDt = new DataTable();
            string strDebug = string.IsNullOrWhiteSpace(_configuration["DebugLog"]) ? "" : _configuration["DebugLog"].Trim();
            if (strDebug == "Y")
            {
                var jsonObject = new
                {
                    UserId,
                    Password,
                    EPassword,
                    Key,
                    LoginAs,
                    Product,
                    ICPV
                };

                string jsonString = JsonConvert.SerializeObject(jsonObject);
                objUtility.InsertLog(jsonString, "");
            }

            string userId = objUtility.mfnReplaceForSQLInjection(UserId);
            string password = objUtility.mfnReplaceForSQLInjection(Password);
            string ePassword = objUtility.mfnReplaceForSQLInjection(EPassword);
            string key = objUtility.mfnReplaceForSQLInjection(Key);
            string loginAs = objUtility.mfnReplaceForSQLInjection(LoginAs);
            string product = objUtility.mfnReplaceForSQLInjection(Product);
            string strICPV = string.IsNullOrWhiteSpace(ICPV) ? "" : ICPV;
            string strFeature = string.IsNullOrWhiteSpace(Feature) ? "" : Feature;
            string strParmICPV = objUtility.fnchkTable("WebParameter") ? objUtility.GetWebParameter("ICPV") : "";
            bool keyBased = false;
            bool branch = false;
            var role = "";
            string strMobileVer = "";

            if (!string.IsNullOrWhiteSpace(loginAs) && loginAs.Substring(0, 1) == "M")
            {
                if (loginAs.Contains("~"))
                {
                    string[] parts = loginAs.Split('~');
                    strMobileVer = parts[1];
                    loginAs = parts[0];
                }

                if (strMobileVer != "2.0.0.1" && objUtility.GetWebParameter("SMS2FA").Trim() != "")
                {
                    return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "You are currently using an older version. Please update to the latest version.", returnDt, "");
                }
            }

            if (string.IsNullOrWhiteSpace(loginAs))
            {
                loginAs = objUtility.GetLoginAs(userId);
            }

            if (string.IsNullOrWhiteSpace(product))
            {
                product = "T";

                DataTable dtDB = objUtility.OpenDataTable("select count(*) from sysobjects with (nolock) where name='Trxdetail' and xType='U'");
                if (dtDB.Rows.Count > 0)
                {
                    if (Convert.ToInt16(dtDB.Rows[0][0].ToString()) != 0)
                    {
                        dtDB = objUtility.OpenDataTable("select b.length from Sysobjects a,Syscolumns b,Systypes c where a.id = b.id and a.name = 'Trxdetail' and b.name = 'td_ac_code' and b.xusertype = c.xtype");
                        if (dtDB.Rows.Count > 0)
                        {
                            if (dtDB.Rows[0][0].ToString() == "16")
                            {
                                product = "C";
                            }
                            else if (dtDB.Rows[0][0].ToString() == "8")
                            {
                                product = "E";
                            }
                        }
                    }
                }
                else
                {
                    dtDB = objUtility.OpenDataTable("select count(*) from sysobjects with (nolock) where name='Trxweb' and xType='U'");
                    if (dtDB.Rows.Count > 0)
                    {
                        if (Convert.ToInt16(dtDB.Rows[0][0].ToString()) != 0)
                        {
                            if (Strings.Left(objUtility.GetWebParameter("DPID"), 2) == "IN")
                            {
                                product = "E";
                            }
                            else
                            {
                                product = "C";
                            }
                        }
                    }
                }
            }
            product = product.ToUpper().Trim();

            if (!string.IsNullOrWhiteSpace(key) && (objUtility.isBPT() || (strICPV == "Y" && strParmICPV == "Y")))
            {
                keyBased = true;
            }
            if (string.IsNullOrWhiteSpace(loginAs))
            {
                loginAs = "C";
            }
            if (loginAs.Trim() == "B")
            {
                branch = true;
            }
            /*var autResp = Login_API_Authorize(key, product, loginAs, Feature);
            if (autResp == null || autResp == "")
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    string encKey = _configuration["encKey"];
                    encKey = objUtility.Decrypt(encKey);
                    encKey = encKey + DateTime.Now.ToString("yyyyMMdd");
                    var keybytes = Encoding.UTF8.GetBytes(encKey);
                    var iv = Encoding.UTF8.GetBytes(encKey);
                    var encrypted = Convert.FromBase64String(key);
                    var decriptedFromJavascript = DecryptStringFromBytes(encrypted, keybytes, iv);
                    key = string.Format(decriptedFromJavascript);
                }
                autResp = Login_API_Authorize(key, product, loginAs, Feature);
                if (autResp == null || autResp == "")
                {
                    return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "You are not authorise to use TradeWebAPI Web Service,Please contact SecMark.", returnDt, "");
                }
            }*/
            #region old code
            /*bool blnOffline = Convert.ToInt16(objUtility.fnFireQueryTradeWeb("Information_Schema.Columns", "count(*)", "Table_Name='client_master' and Column_Name", "cm_companycode", true)) > 0;
            //string autResp = "";
            //var (blnContinueWithouKey, licMsg, arrKey) = objUtility.chkAMCReceived();
            string strLicMessage = "";
            string strHO = "N";
            string strNet = "N";
            string strWeb = "N";
            if (blnOffline)
            {
                if (objUtility.mfnGetSysSplFeatureDP("TBO"))
                {
                    if (product == "C" || product == "E")
                    {
                        strWeb = "Y";
                    }
                }
            }
            else
            {
                bool blnInvalidDB = false;
                DataTable dtDB = new DataTable();
                if (product == "C" || product == "E")
                {
                    dtDB = objUtility.OpenDataTable("select count(*) from sysobjects with (nolock) where name='Trxdetail' and xType='U'");
                    if (dtDB.Rows.Count > 0)
                    {
                        if (Convert.ToInt16(dtDB.Rows[0][0].ToString()) == 0)
                        {
                            blnInvalidDB = true;
                        }
                    }

                    string strColLen = product == "C" ? "16" : "8";
                    dtDB = objUtility.OpenDataTable("select b.length from Sysobjects a,Syscolumns b,Systypes c where a.id = b.id and a.name = 'Trxdetail' and b.name = 'td_ac_code' and b.xusertype = c.xtype");
                    if (dtDB.Rows.Count > 0)
                    {
                        if (dtDB.Rows[0][0].ToString() != strColLen)
                        {
                            blnInvalidDB = true;
                        }
                    }

                    if (blnInvalidDB)
                    {
                        return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Invalid " + product == "C" ? "Cross" : "Estro" + " Database", returnDt, "");
                    }
                }

                var (blnContinueWithouKey, licMsg, arrKey) = objUtility.chkAMCReceived();
                strLicMessage = licMsg;
                foreach (var lickey in arrKey)
                {
                    if (product == "T")
                    {
                        if (lickey.StartsWith("10"))
                        {
                            if (lickey.Substring(2, 1) == "1")
                            {
                                strNet = "Y";
                                //role = "Branch";
                            }
                            if (lickey.Substring(3, 1) == "1")
                            {
                                strWeb = "Y";
                                //role = loginAs == "M" ? "TradeMobile" : "Admin";
                            }
                            strHO = "Y";
                            //role = "Branch";
                            break;
                        }
                    }
                    else if (product == "C")
                    {
                        if (lickey.StartsWith("55"))
                        {
                            if (lickey.Substring(2, 1) == "1")
                            {
                                strNet = "Y";
                                //role = "CrossNet";
                            }
                            if (lickey.Substring(3, 1) == "1")
                            {
                                strWeb = "Y";
                                //role = "CrossWeb";
                            }
                            strHO = "Y";
                            //role = "CrossNet";
                            break;
                        }
                    }
                    else if (product == "E")
                    {
                        if (lickey.StartsWith("56"))
                        {
                            if (lickey.Substring(2, 1) == "1")
                            {
                                strNet = "Y";
                                //role = "EstroNet";
                            }
                            if (lickey.Substring(3, 1) == "1")
                            {
                                strWeb = "Y";
                                //role = "EstroWeb";
                            }
                            strHO = "Y";
                            //role = "EstroNet";
                            break;
                        }
                    }
                }
            }

            if (product == "T")
            {
                if (loginAs == "C" && strWeb == "Y")
                {
                    role = "Admin";
                }
                else if (loginAs == "B" && (strNet == "Y" || strHO == "Y"))
                {
                    role = "Branch";
                }
                else if (loginAs == "M" && objUtility.mfnGetSysSplFeature("TMB"))
                {
                    role = "TradeMobile";
                }
            }
            else if (product == "C")
            {
                if (loginAs == "C" && strWeb == "Y")
                {
                    role = "CrossWeb";
                }
                else if (loginAs == "B" && strNet == "Y")
                {
                    role = "CrossNet";
                }
            }
            else if (product == "E")
            {
                if (loginAs == "C" && strWeb == "Y")
                {
                    role = "EstroWeb";
                }
                else if (loginAs == "B" && strNet == "Y")
                {
                    role = "EstroNet";
                }
            }*/
            #endregion
            /*LoginRole loginRole = objUtility.GetLoginRole(loginAs, product);
            role = loginRole.Role;*/
            LoginRole loginRole = new LoginRole();
            if (!string.IsNullOrWhiteSpace(key) && loginAs.Trim() != "M")
            {
                var autResp = Login_API_Authorize(key, product, loginAs, Feature);
                if (autResp == null || autResp == "")
                {
                    return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "You are not authorise to use TradeWebAPI Web Service,Please contact SecMark.", returnDt, "");
                }
                if (autResp == "TBI")
                {
                    role = "Branch";
                    loginRole.Net = "Y";
                    loginRole.HO = "Y";
                    loginRole.Web = "N";
                    loginRole.LicMsg = "";
                }
                else if (autResp == "BPI")
                {
                    role = "Admin";
                    loginRole.Net = "N";
                    loginRole.HO = "N";
                    loginRole.Web = "Y";
                    loginRole.LicMsg = "";
                }
                else if (autResp == "BPI")
                {
                    role = "Admin";
                    loginRole.Net = "N";
                    loginRole.HO = "N";
                    loginRole.Web = "Y";
                    loginRole.LicMsg = "";
                }
                else if (autResp == "CBI")
                {
                    role = "CrossNet";
                    loginRole.Net = "Y";
                    loginRole.HO = "Y";
                    loginRole.Web = "N";
                    loginRole.LicMsg = "";
                }
            }
            else
            {
                loginRole = objUtility.GetLoginRole(loginAs, product);
                role = loginRole.Role;
            }

            if (string.IsNullOrWhiteSpace(role) || !string.IsNullOrWhiteSpace(loginRole.Message))
            {
                return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, !string.IsNullOrWhiteSpace(loginRole.Message) ? loginRole.Message : "You are not authorise to use TradeWebAPI Web Service,Please contact SecMark.", returnDt, "");
            }
            string strHO = loginRole.HO;
            string strNet = loginRole.Net;
            string strWeb = loginRole.Web;
            string strLicMessage = loginRole.LicMsg;
            bool blnOffline = loginRole.Offline;

            /*switch (autResp)
            {
                case "BPI":
                    role = "Admin";
                    break;
                case "TBL":
                    role = "Admin";
                    break;
                case "INP":
                    role = "GainLoss";
                    break;
                case "TBI":
                    role = "Branch";
                    break;
                case "PBI":
                    role = "Performance";
                    break;
                case "SUB":
                    role = "Subscription";
                    break;
                case "TMB":
                    role = "TradeMobile";
                    break;
                case "CBI":
                    role = "CrossNet";
                    break;
                case "EBI":
                    role = "EstroNet";
                    break;
                case "FUP":
                    role = "FundPayout";
                    break;
                case "CLM":
                    role = "CrossModification";
                    break;
                case "TPI":
                    role = "ReKYC";
                    break;
                case "RKM":
                    role = "ReKYC";
                    break;
                case "COM":
                    role = "CommonAPI";
                    break;
                case "CMP":
                    role = "ComAPI";
                    break;
                case "EBW":
                    role = "EstroWeb";
                    break;
                case "CBW":
                    role = "CrossWeb";
                    break;
            }*/

            /*if (objUtility.mfnGetSysSplFeature("RKC") || objUtility.mfnGetSysSplFeature("RKM") || role == "ReKYC")
            {
                objUtility.CreateReKYCTables();
                if (objUtility.GetWebParameter("TRADEPLUSTEMPDB") != "")
                {
                    string strDatabase = objUtility.GetWebParameter("TRADEPLUSTEMPDB").Trim();
                    var dbCon = new DataContext();
                    string connectionString = dbCon.Database.GetDbConnection().ConnectionString;
                    SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString)
                    {
                        InitialCatalog = strDatabase
                    };
                    string newConnectionString = builder.ToString();

                    using (SqlConnection sqlCon = new SqlConnection(newConnectionString))
                    {
                        sqlCon.Open();
                        objUtility.CheckSP("json", sqlCon);
                        objUtility.CheckSP("ReKYC2", sqlCon);
                    }
                }
                objUtility.CheckSP("ReKYC");

            }
            objUtility.CreateMrgpTables();
            if (loginAs == "C" && product == "E") //For EstroWeb
            {
                if (objUtility.mfnGetSysSplFeatureDP("TBL") && role == "EstroWeb")
                {
                    objUtility.CreateTables(loginAs, product, "EstroWeb");
                    objUtility.CheckSP("EstroWeb");
                }
                else if (objUtility.mfnGetSysSplFeatureDP("TBO") && role == "EstroWeb")
                {
                    objUtility.CreateTables(loginAs, product, "EstroWebOffline");
                    objUtility.CheckSP("EstroWebO");
                }
                else
                {
                    objUtility.CreateTables(loginAs, product, "Estro");
                    objUtility.CheckSP("Estro");
                }
            }
            else if ((loginAs == "C" || loginAs == "B") && product == "C" && (role == "CrossWeb" || role == "CrossNet")) //For CrossWeb
            {
                if (objUtility.mfnGetSysSplFeatureDP("TBL") || role == "CrossNet")
                {
                    objUtility.CreateTables(loginAs, product, "CrossWeb");
                    if (objUtility.GetWebParameter("TRADEPLUSTEMPDB") != "")
                    {
                        string strDatabase = objUtility.GetWebParameter("TRADEPLUSTEMPDB").Trim();
                        var dbCon = new DataContext();
                        string connectionString = dbCon.Database.GetDbConnection().ConnectionString;
                        SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString)
                        {
                            InitialCatalog = strDatabase
                        };
                        string newConnectionString = builder.ToString();

                        using (SqlConnection sqlCon = new SqlConnection(newConnectionString))
                        {
                            sqlCon.Open();
                            objUtility.CheckSP("json", sqlCon);
                        }
                    }
                    objUtility.CheckSP("CrossWeb");
                }
                else if (objUtility.mfnGetSysSplFeatureDP("TBO"))
                {
                    objUtility.CreateTables(loginAs, product, "CrossWebOffline");
                    objUtility.CheckSP("CrossWebO");
                }
            }
            else if (loginAs == "B" && product == "#")
            {
                if (objUtility.isRatnakar())
                {
                    objUtility.CreateTables(loginAs, product, "TradeWeb");
                    if (objUtility.GetWebParameter("TRADEPLUSTEMPDB") != "")
                    {
                        string strDatabase = objUtility.GetWebParameter("TRADEPLUSTEMPDB").Trim();
                        var dbCon = new DataContext();
                        string connectionString = dbCon.Database.GetDbConnection().ConnectionString;
                        SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString)
                        {
                            InitialCatalog = strDatabase
                        };
                        string newConnectionString = builder.ToString();

                        using (SqlConnection sqlCon = new SqlConnection(newConnectionString))
                        {
                            sqlCon.Open();
                            objUtility.CheckSP("json", sqlCon);
                        }
                    }
                    objUtility.CheckSP("TradeWeb");
                }
            }
            else if ((loginAs == "C" || loginAs == "M") && product == "T" && (role == "Admin" || role == "TradeMobile"))
            {
                objUtility.CreateTables(loginAs, product, "TradeWeb");
                if (objUtility.GetWebParameter("TRADEPLUSTEMPDB") != "")
                {
                    string strDatabase = objUtility.GetWebParameter("TRADEPLUSTEMPDB").Trim();
                    var dbCon = new DataContext();
                    string connectionString = dbCon.Database.GetDbConnection().ConnectionString;
                    Microsoft.Data.SqlClient.SqlConnectionStringBuilder builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString)
                    {
                        InitialCatalog = strDatabase
                    };
                    string newConnectionString = builder.ToString();

                    using (Microsoft.Data.SqlClient.SqlConnection sqlCon = new Microsoft.Data.SqlClient.SqlConnection(newConnectionString))
                    {
                        sqlCon.Open();
                        objUtility.CheckSP("json", sqlCon);
                    }
                }
                objUtility.CheckSP("TradeWeb");
            }*/

            if (!string.IsNullOrEmpty(ePassword))
            {
                string encKey = _configuration["epwdKey"];
                encKey = objUtility.Decrypt(encKey);
                var keybytes = Encoding.UTF8.GetBytes(encKey);
                var iv = Encoding.UTF8.GetBytes(encKey);
                var encrypted = Convert.FromBase64String(ePassword);
                password = DecryptStringFromBytes(encrypted, keybytes, iv);
            }
            try
            {
                string encKey = _configuration["epwdKey"];
                encKey = objUtility.Decrypt(encKey);
                var keybytes = Encoding.UTF8.GetBytes(encKey);
                var iv = Encoding.UTF8.GetBytes(encKey);
                var encrypted = Convert.FromBase64String(userId);
                var decUserId = DecryptStringFromBytes(encrypted, keybytes, iv);
                if (decUserId != "keyError")
                {
                    userId = decUserId;
                }
            }
            catch (Exception) { }

            if (isSSO == "Y")
            {
                if (branch == true)
                {
                    //password = objUtility.ExecuteSQLString("select Top 1 um_passwd from User_master (NoLock) where um_user_id = '" + userId + "'");
                    password = objUtility.fnFireQueryTradeWeb("User_master", "um_passwd", "um_user_id", userId, true);
                }
                else
                {
                    //password = objUtility.ExecuteSQLString("select Top 1 cm_pwd from Client_master (NoLock) where cm_cd = '" + userId + "'");
                    password = objUtility.fnFireQueryTradeWeb("Client_master", "cm_pwd", "cm_cd", userId, true);
                }
                if (objUtility.GetWebParameter("TWEBENCPWD") == "Y")
                {
                    password = objUtility.Decrypt(password);
                }
            }
            if (string.IsNullOrWhiteSpace(userId) || (isSSO != "Y" && !keyBased && string.IsNullOrWhiteSpace(password)))
            {
                return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Invalid Username or Password", returnDt, "");
            }
            loginResponseWithDT userList = new loginResponseWithDT();
            userList = branch == true ? BranchUserDetails(userId, password, role) : UserDetails(userId, password, keyBased, product, role);

            if (!string.IsNullOrEmpty(userList.message))
            {
                return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, userList.message, returnDt, "");
            }
            else if (userList.data.Rows.Count > 0)
            {
                //string strLicMessage = "";
                if (!blnOffline)
                {
                    /*var (blnContinueWithouKey, licMsg, arrKey) = objUtility.chkAMCReceived();
                    strLicMessage = licMsg;
                    string strTradePlus = "N";
                    string strTradeNet = "N";
                    string strTradeWeb = "N";
                    foreach (var lickey in arrKey)
                    {
                        if (lickey.StartsWith("10"))
                        {
                            if (lickey.Substring(2, 1) == "1")
                            {
                                strTradeNet = "Y";
                            }
                            if (lickey.Substring(3, 1) == "1")
                            {
                                strTradeWeb = "Y";
                            }
                            strTradePlus = "Y";
                            break;
                        }
                    }*/

                    /*if (userList.data.Columns.Contains("UserAccess"))
                    {
                        if (userList.data.Rows[0]["UserAccess"].ToString().Trim() == "HO")
                        {
                            if (strHO == "N")
                            {
                                return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "You are not authorized to access Branch version", returnDt, "");
                            }
                        }
                        else if (userList.data.Rows[0]["UserAccess"].ToString().Trim() == "Branch")
                        {
                            if (strNet == "N")
                            {
                                return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "You are not authorized to access Branch version", returnDt, "");
                            }
                        }
                    }
                    else if (loginAs == "B")
                    {
                        if (strNet == "N")
                        {
                            return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "You are not authorized to access Branch version", returnDt, "");
                        }
                    }*/

                    if (branch)
                    {
                        if (userList.data.Rows[0]["UserAccess"].ToString().Trim() == "HO")
                        {
                            if (strHO == "N")
                            {
                                return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "You are not authorized to access Branch version", returnDt, "");
                            }
                        }
                        else if (userList.data.Rows[0]["UserAccess"].ToString().Trim() == "Branch")
                        {
                            if (strNet == "N")
                            {
                                return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "You are not authorized to access Branch version", returnDt, "");
                            }
                        }
                    }
                    else
                    {
                        if (strWeb == "N")
                        {
                            return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "You are not authorized to access Client version", returnDt, "");
                        }
                    }

                    if (role == "Branch" && IPAdd != "")
                    {
                        var (strChkParam, strMessage) = objUtility.CheckeIPAddressLogin(userId, IPAdd);
                        if (strMessage != "")
                        {
                            if (strChkParam == "C")
                            {
                                if (strLicMessage != "")
                                {
                                    strLicMessage += Environment.NewLine;
                                }
                                strLicMessage += strMessage;
                            }
                            else
                            {
                                return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, strMessage, returnDt, "");
                            }
                        }
                    }
                }

                userList.data.Columns.Add("Message", typeof(string));
                userList.data.Rows[0]["Message"] = strLicMessage;
                string strUserAccess = "";
                if (userList.data.Columns.Contains("UserAccess"))
                {
                    strUserAccess = userList.data.Rows[0]["UserAccess"].ToString().Trim();
                }

                int expTime;
                int refreshExpTime = 0;
                if (objUtility.GetWebParameter("AccTokenExpTm").Trim() != "")
                {
                    expTime = Convert.ToInt32(objUtility.GetWebParameter("AccTokenExpTm").Trim());
                }
                else
                {
                    expTime = Convert.ToInt32(_configuration["tokenExpireTime"]);
                }
                if (objUtility.GetWebParameter("RefTokenExpTm").Trim() != "")
                {
                    refreshExpTime = Convert.ToInt32(objUtility.GetWebParameter("RefTokenExpTm").Trim());
                }
                else
                {
                    refreshExpTime = Convert.ToInt32(_configuration["refreshTokenExpireTime"]);
                }
                //var datetimeExp = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                var datetimeExp = DateTime.UtcNow;
                string checkCondition = "N", sms2FA = "", product2FA = role;
                string[] allowedRoles = { "Admin", "CrossNet", "EstroNet", "CrossWeb", "EstroWeb", "TradeMobile", "Branch" };
                string userType = userList.data.Rows[0]["UserType"].ToString();
                //if (role == "TradeMobile")
                //{
                if (loginAs == "B" && (role == "CrossNet" || role == "EstroNet" || role == "Branch"))
                {
                    sms2FA = objUtility.GetSysParmSt("SMS2FA", "").Trim();
                    if (sms2FA != "")
                    {
                        if (role == "Branch")
                        {
                            product2FA = "TradeNet";
                        }
                    }
                }
                else
                {
                    sms2FA = objUtility.GetWebParameter("SMS2FA").Trim();
                }

                string str2FA = objUtility.GetWebParameter("2FA").Trim();
                if (str2FA != "")
                {
                    if (role == "Admin" && str2FA != "W" && str2FA != "B")
                    {
                        sms2FA = "";
                    }
                    else if (role == "TradeMobile" && str2FA != "M" && str2FA != "B")
                    {
                        sms2FA = "";
                    }
                }

                if (userType == "checker" || userType == "ReportUser")
                {
                    sms2FA = "";
                }

                if (!string.IsNullOrEmpty(sms2FA) && allowedRoles.Contains(role) && isSSO != "Y")
                {
                    string dpid = "", tempUserId = userId;
                    string[] userStr = userId.Split("|");
                    if (loginAs == "C" && product == "E" && role == "EstroWeb")
                    {
                        product2FA = "EstroWeb";
                        if (userStr.Length > 1)
                        {
                            dpid = userStr[0];
                            userId = userStr[1];
                        }
                    }
                    else if (loginAs == "C" && product == "C" && role == "CrossWeb")
                    {
                        product2FA = "CrossWeb";
                        if (userStr.Length > 1)
                        {
                            dpid = userStr[0];
                            userId = userStr[1];
                        }
                    }
                    else if (loginAs == "C" && product == "T")
                    {
                        product2FA = "TradeWeb";
                    }

                    /*strsql = "select count(0) from OTP_Master with (NoLock) where OTP_ClientCode='" + userId + "' and OTP_Product='" + product2FA + "' and OTP_Type = '2FA' and CAST(OTP_SentDate AS DATETIME) + CAST(OTP_SentTime AS DATETIME) >= DATEADD(MINUTE, -15, GETDATE())";
                    DataTable dtCheck = objUtility.OpenDataTable(strsql);
                    if (dtCheck.Rows.Count > 0)
                    {
                        if (Convert.ToInt32(dtCheck.Rows[0][0]) >= 10)
                        {
                            return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Too many Attempts", returnDt, ""));
                        }
                    }*/

                    DataTable dtResponse = await objUtility.TradeMobileSendOTP(userId, product2FA, "2FA", role);
                    userId = tempUserId;
                    string strStatus = dtResponse.Rows[0]["Status"].ToString();
                    string strMessage = dtResponse.Rows[0]["Message"].ToString();

                    if (strStatus == "Y")
                    {
                        string decryptJwtKey = objUtility.Decrypt(_configuration["Jwt:Key"].ToString());
                        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(decryptJwtKey));
                        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
                        int tokenExpTime = 5;
                        var claims = new[] {
                                        new Claim(JwtRegisteredClaimNames.Sub, userId.ToUpper()),
                                        new Claim(JwtRegisteredClaimNames.Jti, userId.ToUpper()),
                                        new Claim(type: "username", value: userId.ToUpper()),
                                        new Claim(type: "loginas", value: loginAs),
                                        new Claim(type: "product", value: product),
                                        new Claim(type: "loginrole", value: role),
                                        new Claim(type: "branch", value: branch ? "Y" : "N"),
                                        new Claim(type: "userData", value: JsonConvert.SerializeObject(userList.data)),
                                        new Claim(ClaimTypes.Role, "TradeWeb2FA"),
                                        new Claim(type: "otpproduct", value: product2FA),
                                    };
                        var token = new JwtSecurityToken(_configuration["Jwt:Issuer"],
                            _configuration["Jwt:Issuer"],
                            claims,
                            expires: DateTime.UtcNow.AddMinutes(tokenExpTime),
                            signingCredentials: credentials);

                        string str2FAToken = new JwtSecurityTokenHandler().WriteToken(token);
                        DataTable dtUserData = userList.data;
                        dtUserData.Columns.Add("LoginType", typeof(string));
                        dtUserData.Columns.Add("LoginMessage", typeof(string));
                        dtUserData.Rows[0]["LoginType"] = "2FA";
                        dtUserData.Rows[0]["LoginMessage"] = strMessage;

                        tokenResponse result2fa = new tokenResponse
                        {
                            status = true,
                            message = "success",
                            status_code = (int)HttpStatusCode.OK,
                            token = str2FAToken,
                            tokenExpireTime = datetimeExp.AddMinutes(5).ToString(UtcDateTimeFormat),
                            data = dtUserData
                        };
                        return JsonConvert.SerializeObject(result2fa, Formatting.Indented);
                    }
                    else
                    {
                        return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, strMessage, returnDt, "");
                    }
                }
                //}

                if (product.Trim() == "T")
                {
                    FillConfigParametersString();
                }
                else
                {
                    _configuration["IsTradeWeb"] = product.Trim();
                    if (loginAs == "C" && product == "E" && objUtility.mfnGetSysSplFeatureDP("TBL") == false)
                    {
                        string[] strUserID = userId.Split("|");
                        //userId = strUserID.Length > 1 ? strUserID[1] : strUserID[0];
                        _configuration["IsEstroOffLine"] = "Y";
                        _configuration["SessionDPID"] = strUserID[0];
                    }
                    else if (loginAs == "C" && product == "C" && objUtility.mfnGetSysSplFeatureDP("TBL") == false)
                    {
                        string[] strUserID = userId.Split("|");
                        _configuration["IsCrossOffLine"] = "Y";
                        _configuration["SessionDPID"] = strUserID[0];
                    }
                }
                var guidVal = Guid.NewGuid();
                /*if (role == "Admin" && branch == false)
                    objUtility.ExecuteSQL("update Login_Session set ls_token = '" + guidVal + "', ls_logintype = 'C', ls_logintm = '" + DateTime.Now.ToString("HH:mm:ss") + "'  Where ls_code = '" + userId + "' And ls_logindt = '" + DateTime.Now.ToString("yyyyMMdd") + "'  And len(ls_token) < 3");*/

                if ((role == "Admin" || role == "TradeMobile" || role == "EstroWeb" || role == "CrossWeb") && branch == false)
                {
                    objUtility.ExecuteSQL("update Login_Session set ls_token = '" + guidVal + "', ls_logintype = '" + (role == "TradeMobile" ? "M" : "C") + "', ls_logintm = '" + DateTime.Now.ToString("HH:mm:ss") + "'  Where ls_code = '" + userId + "' And ls_logindt = '" + DateTime.Now.ToString("yyyyMMdd") + "'  And len(ls_token) < 3");
                }

                var tokenString = branch == true ? GenerateJSONWebTokenUser(new TradeWebLoginModel { username = userId, password = password, role = role, guid = guidVal.ToString(), tokenExpTime = expTime, useraccess = strUserAccess }) : GenerateJSONWebToken(new TradeWebLoginModel { username = userId, password = password, role = role, guid = guidVal.ToString(), tokenExpTime = expTime });
                var refreshToken = objUtility.GenerateRefreshToken(userId, loginAs, role, refreshExpTime);

                if (isSSO == "Y")
                {
                    string strLogoutURL = objUtility.GetWebParameter("SSOLogoutURL");

                    if (!string.IsNullOrWhiteSpace(strLogoutURL))
                    {
                        userList.data.Columns.Add("SSOLogoutURL", typeof(string));
                        userList.data.Rows[0]["SSOLogoutURL"] = strLogoutURL;
                    }
                }

                tokenResponseNew result = new tokenResponseNew();
                result.status = true;
                result.message = "success";
                result.status_code = (int)HttpStatusCode.OK;
                result.token = tokenString;
                result.tokenExpireTime = datetimeExp.AddMinutes(expTime).ToString(UtcDateTimeFormat);
                result.refreshToken = refreshToken;
                result.refreshTokenExpireTime = datetimeExp.AddMinutes(refreshExpTime).ToString(UtcDateTimeFormat);
                result.data = userList.data;
                var jsonData = JsonConvert.SerializeObject(result, Formatting.Indented);
                return jsonData;
            }
            return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, userList.message, userList, "");
        }

        public async Task<dynamic> LoginSSO_Req(string xmlStr)
        {
            ConvertData returnJson = new ConvertData();
            DataTable returnDt = new DataTable();
            string strSSOLog = "";
            string connetionString = objUtility.GetConnectionStr();
            try
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(xmlStr);
                var dataXML = xml.GetElementsByTagName("X_Data")[0]?.InnerXml;
                XDocument xdoc = XDocument.Parse($"<root>{dataXML}</root>");
                string userId = xdoc.Root.Element("UserId")?.Value;

                string TradeWebUName = xdoc.Root.Element("TradeWebUName")?.Value;
                string PANNo = xdoc.Root.Element("PANNo")?.Value;
                string ID = xdoc.Root.Element("ID")?.Value;
                string PDate = xdoc.Root.Element("PDate")?.Value;
                string UserID = xdoc.Root.Element("UserID")?.Value;
                string SessionId = xdoc.Root.Element("SessionId")?.Value;
                string Link = xdoc.Root.Element("Link")?.Value;
                string DefaultMenu = xdoc.Root.Element("DefaultMenu")?.Value;
                string sUserId = xdoc.Root.Element("sUserId")?.Value; // Form request value
                string sToken = xdoc.Root.Element("sToken")?.Value; // Form request value
                string userID = xdoc.Root.Element("userID")?.Value;
                string token = xdoc.Root.Element("token")?.Value;
                string clientCode = xdoc.Root.Element("ClientCode")?.Value;  // For SARAL
                string sessionNo = xdoc.Root.Element("SessionNo")?.Value;  // For SARAL
                string reqType = xdoc.Root.Element("reqtype")?.Value;  // For SARAL

                string password = "", epassword = "";
                string key = xdoc.Root.Element("Key")?.Value;
                string loginAs = xdoc.Root.Element("LoginAs")?.Value;
                string product = xdoc.Root.Element("Product")?.Value;
                string icpv = xdoc.Root.Element("ICPV")?.Value;
                string feature = xdoc.Root.Element("Feature")?.Value;


                string Strsql = string.Empty;
                //// Using Query string with PanNo and userName
                if (TradeWebUName != null && PANNo != null)
                {
                    string strUname = string.Empty;
                    string strPanno = string.Empty;
                    strUname = objUtility.mfnReplaceForSQLInjection(objUtility.DecodeFrom64(TradeWebUName));
                    strPanno = objUtility.mfnReplaceForSQLInjection(objUtility.DecodeFrom64(PANNo));

                    Strsql = "select LTrim(RTrim(cm_cd)) ClientCode, LTrim(RTrim(cm_Name)) ClientName, 'user' UserType from client_master with (nolock) where cm_cd='" + strUname.ToString().Trim() + "' and cm_panno='" + strPanno.ToString().Trim() + "'";
                    DataTable dt = new DataTable();
                    dt = objUtility.OpenDataTable(Strsql);
                    if (dt.Rows.Count != 0)
                    {
                        var response = await Login(strUname, password, epassword, key, loginAs, product, icpv, feature, "Y");
                        return response;
                    }
                }

                //// Using Query string with UserID and PDate(current date)
                else if (ID != null && PDate != null)
                {
                    string strID = objUtility.mfnReplaceForSQLInjection(objUtility.DecodeFrom64(ID));
                    string strPDate = objUtility.DecodeFrom64(PDate);

                    if (_configuration["IsTradeWeb"] == "C" || _configuration["IsTradeWeb"] == "E")
                    {
                        Strsql = "select LTrim(RTrim(cm_cd)) ClientCode, LTrim(RTrim(cm_Name)) ClientName, 'user' UserType,convert(char,convert(datetime,getdate()),103) as DT from client_master with (nolock) where cm_companycode+cm_cd='" + strID.ToString().Trim() + "'";
                    }
                    else if (_configuration["IsTradeWeb"] == "T" || _configuration["IsTradeWeb"] == "O")
                    {
                        Strsql = "select LTrim(RTrim(cm_cd)) ClientCode, LTrim(RTrim(cm_Name)) ClientName, 'user' UserType,convert(char,convert(datetime,getdate()),103) as DT from client_master with (nolock) where cm_cd='" + strID.ToString().Trim() + "'";
                    }
                    DataTable dt = new DataTable();
                    dt = objUtility.OpenDataTable(Strsql);

                    if (dt.Rows.Count != 0)
                    {
                        if (strPDate == dt.Rows[0]["DT"].ToString().Replace("/", "").Trim())
                        {
                            var response = await Login(strID, password, epassword, key, loginAs, product, icpv, feature, "Y");
                            return response;
                        }
                    }
                }

                //// For FTSOSURL   *** Using Query string with UserID, SessionID, Link, Default Value
                string strparm = objUtility.GetWebParameter("FTSOSURL");
                if (!String.IsNullOrEmpty(strparm))
                {
                    if (UserID != null && SessionId != null)
                    {
                        char seperator = '?';
                        string[] separateURL = strparm.Split(seperator);
                        string struserid = objUtility.mfnReplaceForSQLInjection(UserID);
                        System.Collections.Specialized.NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(separateURL[1]);
                        queryString["UserId"] = UserID;
                        queryString["SessionId"] = SessionId;
                        queryString["Link"] = string.IsNullOrEmpty(queryString["Link"]) ? Link : queryString["Link"];
                        string strlnk = separateURL[0] + "?" + queryString.ToString();
                        string strresponse = await objUtility.WebRequestTestStr(strlnk);
                        if (!string.IsNullOrEmpty(strresponse))
                        {
                            XmlDocument doc = new XmlDocument();
                            doc.LoadXml(strresponse);
                            string strresponse1 = doc.InnerText;
                            if (strresponse1.Contains("10000AUTHENTICATION"))
                            {
                                Strsql = "select LTrim(RTrim(cm_cd)) ClientCode, LTrim(RTrim(cm_Name)) ClientName, 'user' UserType  from client_master with (nolock) where cm_cd='" + struserid.ToString().Trim() + "'";
                                DataTable dt = new DataTable();
                                dt = objUtility.OpenDataTable(Strsql);
                                if (dt.Rows.Count > 0)
                                {
                                    var response = await Login(struserid, password, epassword, key, loginAs, "", icpv, feature, "Y");
                                    return response;
                                }
                            }
                            else
                            {
                                return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Session has Expired", returnDt, "");
                            }
                        }
                    }
                }

                //// For SSOURL kmbala   *** Reqest Form with sUserId, sToken 
                else if (objUtility.GetWebParameter("SSOURLkmbala") != "")
                {
                    if (!string.IsNullOrEmpty(sUserId) && !string.IsNullOrEmpty(sToken))
                    {
                        string strSUserID = objUtility.mfnReplaceForSQLInjection(sUserId);
                        string strSToken = sToken;

                        string URI = objUtility.GetWebParameter("SSOURLkmbala");
                        URI = URI.Replace("<sUserId>", strSUserID);
                        URI = URI.Replace("<sToken>", strSToken);
                        var result = "";
                        try
                        {
                            /*ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                            var httpRequest = (HttpWebRequest)WebRequest.Create(URI);
                            httpRequest.Method = "POST";

                            var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                            {
                                result = streamReader.ReadToEnd();
                            }*/
                            var handler = new HttpClientHandler
                            {
                                SslProtocols = SslProtocols.Tls12
                            };

                            using (var client = new HttpClient(handler))
                            {
                                var response = await client.PostAsync(URI, null);

                                response.EnsureSuccessStatusCode();

                                result = await response.Content.ReadAsStringAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Token has Expired-" + ex.Message.ToString(), returnDt, "");
                        }
                        if (result.Trim() == "TRUE")
                        {
                            var response = await Login(strSUserID, password, epassword, key, loginAs, product, icpv, feature, "Y");
                            return response;
                        }
                        else
                        {
                            return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Token has Expired", returnDt, "");
                        }
                    }
                }

                //// For SSOURL SYMPNY   *** Using Query string with userID, token, Link, 
                else if (objUtility.GetWebParameter("SSOURLSYMPNY") != "")
                {
                    if (!string.IsNullOrEmpty(userID) && !string.IsNullOrEmpty(token))
                    {
                        string URI = objUtility.GetWebParameter("SSOURLSYMPNY");
                        string strUserID = objUtility.mfnReplaceForSQLInjection(userID);
                        string strToken = token;
                        try
                        {
                            JObject jObj = new JObject
                                    {
                                        new JProperty("UserId", strUserID),
                                        new JProperty("Token", strToken)
                                    };
                            string strJson = jObj.ToString();

                            #region oldversion
                            /*ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                            var httpRequest = (HttpWebRequest)WebRequest.Create(URI);
                            httpRequest.ContentType = "application/json";
                            httpRequest.Method = "POST";
                            httpRequest.Headers.Add("Authorization", strToken);
                            using (StreamWriter streamWriter = new StreamWriter(httpRequest.GetRequestStream()))
                            {
                                streamWriter.Write(strJson);
                            }
                            var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                            httpResponse.Close();
                            if (httpResponse.StatusCode == HttpStatusCode.OK)
                            {
                                var response = Login(strUserID, password, epassword, key, loginAs, product, icpv, feature, "Y");
                                return response;
                            }*/
                            #endregion

                            using var httpClient = new HttpClient();

                            var request = new HttpRequestMessage(HttpMethod.Post, URI);

                            request.Headers.Add("Authorization", strToken);

                            request.Content = new StringContent(
                                strJson,
                                Encoding.UTF8,
                                "application/json");

                            var response = await httpClient.SendAsync(request);

                            if (response.IsSuccessStatusCode)
                            {
                                return await Login(strUserID, password, epassword, key, loginAs,
                                             product, icpv, feature, "Y");
                            }
                        }
                        catch (WebException)
                        {
                            return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Token has Expired", returnDt, "");
                        }
                    }
                }

                //// For RupeeSeed   *** Using Query string with userID, sessionID 
                string strRupSedSSO = objUtility.GetWebParameter("SSOURLRupSed");
                if (strRupSedSSO != "")
                {
                    string strUserID = objUtility.mfnReplaceForSQLInjection(UserID);
                    string strSession = objUtility.mfnReplaceForSQLInjection(SessionId);

                    if (!string.IsNullOrEmpty(strUserID) && !string.IsNullOrEmpty(strSession))
                    {

                        JObject jObj = new JObject
                                {
                                    new JProperty("id", strUserID),
                                    new JProperty("sessionid", strSession)
                                };
                        string strJson = jObj.ToString();

                        var result = "";
                        try
                        {
                            #region oldregion
                            /*ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                            var httpRequest = (HttpWebRequest)WebRequest.Create(strRupSedSSO);
                            httpRequest.ContentType = "application/json";
                            httpRequest.Method = "POST";
                            using (StreamWriter streamWriter = new StreamWriter(httpRequest.GetRequestStream()))
                            {
                                streamWriter.Write(strJson);
                            }

                            var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                            {
                                result = streamReader.ReadToEnd();
                            }
                            httpResponse.Close();*/
                            #endregion

                            using var httpClient = new HttpClient();

                            var content = new StringContent(
                                strJson,
                                Encoding.UTF8,
                                "application/json");

                            var response = await httpClient.PostAsync(strRupSedSSO, content);

                            response.EnsureSuccessStatusCode();

                            result = await response.Content.ReadAsStringAsync();
                        }
                        catch (Exception ex)
                        {
                            return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Token has Expired - " + ex.Message.ToString(), returnDt, "");
                        }

                        if (result.Trim() == "true")
                        {
                            var response = await Login(strUserID, password, epassword, key, loginAs, product, icpv, feature, "Y");
                            return response;
                        }
                        else
                        {
                            return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Token has Expired", returnDt, "");
                        }
                    }
                }

                //// For DION   *** Using Query string with UserID, token 
                string strDionSSO = objUtility.GetWebParameter("SSOURLDion");
                if (strDionSSO != "")
                {
                    string strDionKey = objUtility.GetWebParameter("SSODionKey");

                    if (string.IsNullOrWhiteSpace(strDionKey))
                    {
                        return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "Key not found", returnDt, "");
                    }

                    string strUserID = objUtility.mfnReplaceForSQLInjection(UserID);
                    strUserID = objUtility.FixBase64(strUserID);
                    string strToken = objUtility.mfnReplaceForSQLInjection(token);

                    strSSOLog = "strUserID = '" + (strUserID ?? "").Replace("\\", "\\\\").Replace("'", "\\'") + "', strToken = '" + (strToken ?? "").Replace("\\", "\\\\").Replace("'", "\\'") + "', ";

                    if (!string.IsNullOrWhiteSpace(strUserID) && !string.IsNullOrWhiteSpace(strToken))
                    {
                        var obj = new
                        {
                            request = new
                            {
                                UserId = strUserID,
                                Token = strToken
                            }
                        };

                        string jsonString = JsonConvert.SerializeObject(obj);

                        string result = "";
                        string type = "";
                        string description = "";
                        string code = "";
                        HttpStatusCode statusCode;

                        //HttpWebResponse httpResponse = null;

                        //try
                        //{
                        //    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                        //    var httpRequest = (HttpWebRequest)WebRequest.Create(strDionSSO);
                        //    httpRequest.Method = "POST";
                        //    httpRequest.ContentType = "application/json";
                        //    httpRequest.Timeout = 30000; // 30 seconds

                        //    using (var streamWriter = new StreamWriter(httpRequest.GetRequestStream()))
                        //    {
                        //        streamWriter.Write(jsonString);
                        //    }

                        //    httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                        //    statusCode = httpResponse.StatusCode;

                        //    using (var reader = new StreamReader(httpResponse.GetResponseStream()))
                        //    {
                        //        result = reader.ReadToEnd();
                        //        strSSOLog += "result = '" + result.Replace("\\", "\\\\").Replace("'", "\\'") + "', ";
                        //        //result = @"{""d"":""{\""type\"":\""success\"",\""code\"":\""s-backoffice-001\"",\""description\"":\""Valid Token Provided\"",\""result\"":null}""}";
                        //    }
                        //}
                        //catch (WebException ex)
                        //{
                        //    //HTTP 400 will come here
                        //    if (ex.Response != null)
                        //    {
                        //        httpResponse = (HttpWebResponse)ex.Response;
                        //        statusCode = httpResponse.StatusCode;

                        //        using (var reader = new StreamReader(httpResponse.GetResponseStream()))
                        //        {
                        //            result = reader.ReadToEnd();
                        //            strSSOLog += "result1 = '" + result.Replace("\\", "\\\\").Replace("'", "\\'") + "', ";
                        //            //result = @"{""d"":""{\""type\"":\""error\"",\""code\"":\""e-token-0001\"",\""description\"":\""Your session has been expired\"",\""result\"":null}""}";
                        //        }
                        //    }
                        //    else
                        //    {
                        //        return returnJson.ConvertDataIntoJson(
                        //            false,
                        //            (int)HttpStatusCode.BadRequest,
                        //            "SSO service unreachable.",
                        //            returnDt,
                        //            ""
                        //        );
                        //    }
                        //}
                        //catch (Exception)
                        //{
                        //    return returnJson.ConvertDataIntoJson(
                        //        false,
                        //        (int)HttpStatusCode.BadRequest,
                        //        "Unexpected error while calling SSO service.",
                        //        returnDt,
                        //        ""
                        //    );
                        //}

                        try
                        {
                            using var httpClient = new HttpClient
                            {
                                Timeout = TimeSpan.FromSeconds(30)
                            };

                            var contentData = new StringContent(
                                jsonString,
                                Encoding.UTF8,
                                "application/json");

                            var response = await httpClient.PostAsync(strDionSSO, contentData);

                            statusCode = response.StatusCode;

                            result = await response.Content.ReadAsStringAsync();

                            strSSOLog += "result = '" +
                                         result.Replace("\\", "\\\\")
                                               .Replace("'", "\\'") +
                                         "', ";

                            // Optional: Handle non-success status codes
                            if (!response.IsSuccessStatusCode)
                            {
                                // HTTP 400, 401, 500 etc.
                                // result already contains response body
                            }
                        }
                        catch (TaskCanceledException ex)
                        {
                            strSSOLog += $"SSO request timed out: {ex}";

                            return returnJson.ConvertDataIntoJson(
                                false,
                                (int)HttpStatusCode.RequestTimeout,
                                "SSO request timed out.",
                                returnDt,
                                ""
                            );
                        }
                        catch (HttpRequestException ex)
                        {
                            strSSOLog += $"SSO service unreachable: {ex}";

                            return returnJson.ConvertDataIntoJson(
                                false,
                                (int)HttpStatusCode.BadRequest,
                                "SSO service unreachable.",
                                returnDt,
                                ""
                            );
                        }
                        catch (Exception ex)
                        {
                            strSSOLog += $"Unexpected error while calling SSO service: {ex}";

                            return returnJson.ConvertDataIntoJson(
                                false,
                                (int)HttpStatusCode.BadRequest,
                                "Unexpected error while calling SSO service.",
                                returnDt,
                                ""
                            );
                        }

                        try
                        {
                            if (string.IsNullOrWhiteSpace(result))
                            {
                                strSSOLog += "Empty response from SSO service.";

                                return returnJson.ConvertDataIntoJson(
                                    false,
                                    (int)HttpStatusCode.BadRequest,
                                    "Empty response from SSO service.",
                                    returnDt,
                                    ""
                                );
                            }

                            var outer = JsonConvert.DeserializeObject<DionSSOResponse>(result);

                            if (outer == null || string.IsNullOrWhiteSpace(outer.d))
                            {
                                strSSOLog += $"Invalid SSO response format. Result : {result}";

                                return returnJson.ConvertDataIntoJson(
                                    false,
                                    (int)HttpStatusCode.BadRequest,
                                    "Invalid SSO response format.",
                                    returnDt,
                                    ""
                                );
                            }

                            var finalResponse = JsonConvert.DeserializeObject<DionSSOActualResponse>(outer.d);

                            if (finalResponse == null)
                            {
                                strSSOLog += $"Invalid SSO response structure. Result : {result}";

                                return returnJson.ConvertDataIntoJson(
                                    false,
                                    (int)HttpStatusCode.BadRequest,
                                    "Invalid SSO response structure.",
                                    returnDt,
                                    ""
                                );
                            }

                            type = finalResponse.type?.Trim();
                            description = finalResponse.description;
                            code = finalResponse.code;

                            strSSOLog += "type = '" + type.Replace("\\", "\\\\").Replace("'", "\\'") + "', description = '" + description.Replace("\\", "\\\\").Replace("'", "\\'") + "', code = '" + code.Replace("\\", "\\\\").Replace("'", "\\'") + "', ";
                        }
                        catch (JsonException)
                        {
                            strSSOLog += $"Invalid JSON received from SSO service. Result : {result}";

                            return returnJson.ConvertDataIntoJson(
                                false,
                                (int)HttpStatusCode.BadRequest,
                                "Invalid JSON received from SSO service.",
                                returnDt,
                                ""
                            );
                        }

                        //finally
                        //{
                        //    httpResponse?.Close();
                        //}


                        //Business Logic
                        if (statusCode == HttpStatusCode.OK && string.Equals(type, "success", StringComparison.OrdinalIgnoreCase))
                        {
                            strUserID = objUtility.DecryptAES(strUserID, strDionKey);

                            strSSOLog += "DecryptedUserID = '" + strUserID + "', password = '" + password + "', epassword = '" + epassword + "', key = '" + key + "', loginAs = '" + loginAs + "', product = '" + product + "', icpv = '" + icpv + "', feature = '" + feature + "'";

                            using (SqlConnection conn = new SqlConnection(connetionString))
                            {
                                conn.Open();
                                objUtility.InsertLog(0, "TradeWebSSO", "", strSSOLog, "", "", "", conn);
                            }

                            return await Login(
                                strUserID,
                                password,
                                epassword,
                                key,
                                loginAs,
                                product,
                                icpv,
                                feature,
                                "Y"
                            );
                        }
                        else
                        {
                            string message = $"{code ?? ""} - {description ?? "SSO validation failed."}";
                            strSSOLog += message;
                            return returnJson.ConvertDataIntoJson(
                                false,
                                (int)HttpStatusCode.BadRequest, message,
                                returnDt,
                                ""
                            );
                        }
                    }
                }

                //// For SARAL / MoneyMaker server   *** Using Query string with ClientCode, sessionNo, Request Type 
                string saral = objUtility.GetWebParameter("SSOURLSARAL");
                if (saral != "")
                {
                    string strClientCode = objUtility.mfnReplaceForSQLInjection(clientCode);
                    string strSessionNo = objUtility.mfnReplaceForSQLInjection(sessionNo);
                    string strReqType = objUtility.mfnReplaceForSQLInjection(reqType);

                    if (!string.IsNullOrEmpty(strClientCode) && !string.IsNullOrEmpty(strSessionNo))
                    {
                        var result = "{\"ErrorCode\":1,\"Message\":\"Token has Expired\"}";
                        dynamic responseObj = JsonConvert.DeserializeObject(result);
                        try
                        {
                            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                            // Append query parameters to URL if needed
                            //string url = saral + "?ClientCode="+ strClientCode + "&SessionNo="+ strSessionNo + "&reqtype=" + strReqType;
                            string url = $"{saral}?ClientCode={clientCode}&SessionNo={sessionNo}&reqtype={reqType}";

                            var httpRequest = (HttpWebRequest)WebRequest.Create(url);
                            httpRequest.Method = "GET";
                            httpRequest.ContentType = "application/json";

                            var httpResponse = (HttpWebResponse)httpRequest.GetResponse();

                            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                            {
                                result = streamReader.ReadToEnd();
                                responseObj = JsonConvert.DeserializeObject(result);
                            }
                            httpResponse.Close();
                        }
                        catch (WebException ex)
                        {
                            var responseStream = ex.Response?.GetResponseStream();
                            if (responseStream != null)
                            {
                                using (var reader = new StreamReader(responseStream))
                                {
                                    result = reader.ReadToEnd();
                                    responseObj = JsonConvert.DeserializeObject(result);
                                }
                            }
                        }
                        if (responseObj.ErrorCode == 0)
                        {
                            var response = await Login(strClientCode, password, epassword, key, loginAs, product, icpv, feature, "Y");
                            return response;
                        }
                        else
                        {
                            return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, responseObj.Message.ToString(), returnDt, "");
                        }
                    }
                }

                //// For SUSHIL   *** Using Query string with userID, token 
                string strSushilSSO = objUtility.GetWebParameter("SSOURLSushil");
                if (!string.IsNullOrWhiteSpace(strSushilSSO))
                {
                    string strUserID = userID?.Trim();
                    string strToken = token?.Trim();

                    if (!string.IsNullOrWhiteSpace(strUserID) &&
                        !string.IsNullOrWhiteSpace(strToken))
                    {
                        string result = "";
                        string type = "";
                        string description = "";
                        string code = "";

                        try
                        {
                            var requestObj = new
                            {
                                userID = strUserID,
                                token = strToken
                            };

                            string jsonString = JsonConvert.SerializeObject(requestObj);

                            using var httpClient = new HttpClient
                            {
                                Timeout = TimeSpan.FromSeconds(30)
                            };

                            using var content = new StringContent(
                                jsonString,
                                Encoding.UTF8,
                                "application/json"
                            );

                            using HttpResponseMessage response =
                                await httpClient.PostAsync(strSushilSSO, content);

                            result = await response.Content.ReadAsStringAsync();

                            // Log only safe information.
                            strSSOLog += $"SSOStatusCode = {(int)response.StatusCode}, ";

                            if (string.IsNullOrWhiteSpace(result))
                            {
                                return returnJson.ConvertDataIntoJson(
                                    false,
                                    (int)HttpStatusCode.BadRequest,
                                    "Empty response from SSO service.",
                                    returnDt,
                                    ""
                                );
                            }

                            try
                            {
                                var finalResponse =
                                    JsonConvert.DeserializeObject<DionSSOActualResponse>(result);

                                if (finalResponse == null)
                                {
                                    return returnJson.ConvertDataIntoJson(
                                        false,
                                        (int)HttpStatusCode.BadRequest,
                                        "Invalid SSO response structure.",
                                        returnDt,
                                        ""
                                    );
                                }

                                type = finalResponse.type?.Trim() ?? "";
                                description = finalResponse.description ?? "";
                                code = finalResponse.code ?? "";

                                strSSOLog +=
                                    $"type = '{type.Replace("\\", "\\\\").Replace("'", "\\'")}', " +
                                    $"description = '{description.Replace("\\", "\\\\").Replace("'", "\\'")}', " +
                                    $"code = '{code.Replace("\\", "\\\\").Replace("'", "\\'")}', ";

                                if (response.IsSuccessStatusCode &&
                                    string.Equals(
                                        type,
                                        "success",
                                        StringComparison.OrdinalIgnoreCase))
                                {
                                    // Do NOT log password/token/epassword.
                                    strSSOLog +=
                                        $"loginAs = '{loginAs}', " +
                                        $"product = '{product}', " +
                                        $"icpv = '{icpv}', " +
                                        $"feature = '{feature}'";

                                    using (SqlConnection conn =
                                           new SqlConnection(connetionString))
                                    {
                                        await conn.OpenAsync();

                                        objUtility.InsertLog(
                                            0,
                                            "TradeWebSSO",
                                            "",
                                            strSSOLog,
                                            "",
                                            "",
                                            "",
                                            conn
                                        );
                                    }

                                    return await Login(
                                        strUserID,
                                        password,
                                        epassword,
                                        key,
                                        loginAs,
                                        product,
                                        icpv,
                                        feature,
                                        "Y"
                                    );
                                }

                                string message =
                                    $"{code} - " +
                                    (!string.IsNullOrWhiteSpace(description)
                                        ? description
                                        : "SSO validation failed.");

                                return returnJson.ConvertDataIntoJson(
                                    false,
                                    (int)HttpStatusCode.BadRequest,
                                    message,
                                    returnDt,
                                    ""
                                );
                            }
                            catch (JsonException)
                            {
                                return returnJson.ConvertDataIntoJson(
                                    false,
                                    (int)HttpStatusCode.BadRequest,
                                    "Invalid JSON received from SSO service.",
                                    returnDt,
                                    ""
                                );
                            }
                        }
                        catch (TaskCanceledException)
                        {
                            return returnJson.ConvertDataIntoJson(
                                false,
                                (int)HttpStatusCode.BadRequest,
                                "SSO service request timed out.",
                                returnDt,
                                ""
                            );
                        }
                        catch (HttpRequestException)
                        {
                            return returnJson.ConvertDataIntoJson(
                                false,
                                (int)HttpStatusCode.BadRequest,
                                "SSO service unreachable.",
                                returnDt,
                                ""
                            );
                        }
                        catch (Exception)
                        {
                            return returnJson.ConvertDataIntoJson(
                                false,
                                (int)HttpStatusCode.BadRequest,
                                "Unexpected error while calling SSO service.",
                                returnDt,
                                ""
                            );
                        }
                    }
                }

                return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "UserID/Record not found.", returnDt, "");
            }
            catch (Exception ex)
            {
                return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, ex.Message.ToString(), returnDt, "");
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(strSSOLog))
                {
                    using (SqlConnection conn = new SqlConnection(connetionString))
                    {
                        conn.Open();
                        objUtility.InsertLog(0, "TradeWebSSOFinal", "", strSSOLog, "", "", "", conn);
                    }
                }
            }
        }

        private static string DecryptStringFromBytes(byte[] cipherText, byte[] key, byte[] iv)
        {
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException(nameof(cipherText));

            if (key == null || key.Length <= 0)
                throw new ArgumentNullException(nameof(key));

            if (iv == null || iv.Length <= 0)
                throw new ArgumentNullException(nameof(iv));

            string plaintext = null;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;

                aesAlg.KeySize = 256;
                aesAlg.BlockSize = 128;

                aesAlg.Key = key;
                aesAlg.IV = iv;

                using (ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV))
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                {
                    plaintext = srDecrypt.ReadToEnd();
                }
            }

            return plaintext;
        }

        private string GenerateJSONWebToken(TradeWebLoginModel userInfo)
        {
            string decryptJwtKey = objUtility.Decrypt(_configuration["Jwt:Key"].ToString());
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(decryptJwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            string Strsql = "";
            if (_configuration["IsTradeWeb"] == "T" || _configuration["IsTradeWeb"] == "O")
            {
                string apiRes3 = objUtility.fnFireQueryTradeWeb("Entity_master", "count(0)", " em_cd='B' and em_name like 'SYKES & RAY EQUITIES%' and 1", "1", true);
                if (Convert.ToInt16(apiRes3) > 0)
                    Strsql = " select em_name OrgName,em_cd CompnyCd from Entity_Master with (nolock) where em_cd='B'";
                else
                    Strsql = " select em_name OrgName,em_cd CompnyCd from Entity_Master with (nolock) where em_cd =(select min(em_cd) from Entity_master Where len(em_cd) = 1)";
            }
            else if (_configuration["IsTradeWeb"] == "C")
            {
                Strsql = "select sp_sysvalue OrgName,'' CompnyCd from sysparameter with (nolock) where sp_parmcd='NAME'";
            }

            else if (_configuration["IsTradeWeb"] == "E")
            {
                Strsql = "select sp_sysvalue OrgName,'' CompnyCd from sysparameter with (nolock) where sp_parmcd='NAME'";
            }

            else if (_configuration["IsTradeWeb"] == "B")
            {
                Strsql = " select em_name OrgName,em_cd CompnyCd from Entity_Master with (nolock) where em_cd =(select min(em_cd) from Entity_master )";
            }

            var ObjDataSet = objUtility.OpenDataSet(Strsql);
            int tokenExpTime = userInfo.tokenExpTime;
            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, userInfo.username),
                new Claim(type: "companyCode", value: ObjDataSet.Tables[0].Rows[0]["CompnyCd"].ToString().Trim()),
                new Claim(JwtRegisteredClaimNames.Jti, userInfo.guid),
                new Claim(type: "username", value: userInfo.username),
                new Claim(type: "OFLV", value: _configuration["IsTradeWeb"].Trim() !="O" ? "Y" : "N"),
                new Claim(ClaimTypes.Role, userInfo.role),
            };
            var token = new JwtSecurityToken(_configuration["Jwt:Issuer"],
                _configuration["Jwt:Issuer"],
                claims,
                expires: DateTime.UtcNow.AddMinutes(tokenExpTime),
                signingCredentials: credentials);

            if (userInfo.role == "TradeMobile" || userInfo.role == "EstroWeb" || userInfo.role == "Admin")
            {
                _tokenStore.StoreToken(userInfo.username, new JwtSecurityTokenHandler().WriteToken(token));
            }

            //_configuration["Segments"] = ObjDataSet.Tables[0].Rows[0]["CompnyCd"].ToString().Trim();
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateJSONWebTokenUser(TradeWebLoginModel userInfo)
        {
            string decryptJwtKey = objUtility.Decrypt(_configuration["Jwt:Key"].ToString());
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(decryptJwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            string Strsql = "";
            if (_configuration["IsTradeWeb"] == "T" || _configuration["IsTradeWeb"] == "O")
            {
                Strsql = "select em_cd CompanyCode,em_name OrgName from Entity_Master";
                Strsql += " where em_cd in (select min(em_cd) from Entity_master)";
            }
            else
            {
                Strsql = "select (select sp_sysvalue from Sysparameter where sp_parmcd = 'DPID') CompanyCode, ";
                Strsql += " (select sp_sysvalue from Sysparameter where sp_parmcd = 'NAME') OrgName";
            }

            var ObjDataSet = objUtility.OpenDataSet(Strsql);

            string loginAccess = "";
            if (userInfo.useraccess != "HO")
            {
                if (_configuration["IsTradeWeb"] == "T" || _configuration["IsTradeWeb"] == "O")
                {
                    loginAccess = LoginAccess(userInfo.username, "T");
                }
                else
                {
                    loginAccess = LoginAccess(userInfo.username, "");
                }
            }

            int tokenExpTime = userInfo.tokenExpTime;
            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, userInfo.username),
                new Claim(type: "companyCode", value: ObjDataSet.Tables[0].Rows[0]["CompanyCode"].ToString().Trim()),
                new Claim(JwtRegisteredClaimNames.Jti, userInfo.guid),
                new Claim(type: "username", value: userInfo.username),
                new Claim(ClaimTypes.Role, userInfo.role),
                new Claim(type: "loginaccess", value: loginAccess),
                new Claim(type: "useraccess", value: userInfo.useraccess),
            };
            var token = new JwtSecurityToken(_configuration["Jwt:Issuer"],
                _configuration["Jwt:Issuer"],
                claims,
                expires: DateTime.UtcNow.AddMinutes(tokenExpTime),
                signingCredentials: credentials);

            if (userInfo.role == "Branch")
            {
                _tokenStore.StoreToken(userInfo.username, new JwtSecurityTokenHandler().WriteToken(token));
            }

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string LoginAccess(string userId, string product)
        {
            string Strsql = "";
            string AccessFilter = "";
            if (product == "T")
            {
                Strsql = "if (select count(*) from sysobjects where name='LoginAccess')>0 select * from loginAccess where la_userID='" + userId + "' order by la_grouping else select * from group_master where 1=2";
                DataTable dtLogin = objUtility.OpenDataTable(Strsql);
                if (dtLogin.Rows.Count > 0)
                {
                    short j;
                    string strCatg = "";
                    string strTmp;
                    j = 0;
                    strsql = "";
                    while (j < dtLogin.Rows.Count)
                    {
                        strCatg = dtLogin.Rows[j]["La_grouping"].ToString();
                        strTmp = "";
                        while (strCatg == dtLogin.Rows[j]["La_grouping"].ToString())
                        {
                            strTmp = strTmp + "'" + Strings.Trim(dtLogin.Rows[j]["la_grcode"].ToString()) + "',";
                            j = Conversions.ToShort(j + 1);
                            if (j >= dtLogin.Rows.Count)
                            {
                                break;
                            }
                        }

                        strsql = strTmp;    // for Branches 
                        strTmp = Strings.Mid(strTmp, 1, Strings.Len(strTmp) - 1);
                        var switchExpr = Strings.UCase(strCatg);
                        switch (switchExpr)
                        {
                            case "B":
                                {
                                    strCatg = "cm_brboffcode";

                                    strsql = strsql.Replace("'", "").Replace(",", "|");
                                    //HttpContext.Current.Session["Branch"] = strsql;
                                    break;
                                }

                            case "G":
                                {
                                    strCatg = "cm_groupcd";
                                    break;
                                }

                            case "F":
                                {
                                    strCatg = "cm_familycd";
                                    break;
                                }

                            case "R":
                                {
                                    strCatg = "cm_Subbroker";
                                    break;
                                }

                            case "A":
                                {
                                    break;
                                }

                            case "C":
                                {
                                    strCatg = "cm_cd";
                                    break;
                                }

                            case "M":
                                {
                                    strCatg = "cm_dpactno";
                                    break;
                                }
                            case "D":
                                {
                                    strCatg = "cm_margintype";
                                    break;
                                }
                        }

                        //AccessFilter = Conversions.ToString(Conversions.ToString(AccessFilter + Interaction.IIf(Strings.Len(AccessFilter) > 0, " or ", "") + strCatg) + Interaction.IIf(Strings.InStr(1, strTmp, ",") > 0, " in (" + strTmp + ")", "=" + strTmp));
                        if (switchExpr == "R")
                        {
                            AccessFilter = Conversions.ToString(Conversions.ToString(AccessFilter + Interaction.IIf(Strings.Len(AccessFilter) > 0, " or (", "(") + strCatg) + Interaction.IIf(Strings.InStr(1, strTmp, ",") > 0, " in (" + strTmp + ")", "=" + strTmp) + " or exists ( select cm2_cd from client_info where cm2_cd = cm_cd and cm_remissier2 " + Interaction.IIf(Strings.InStr(1, strTmp, ",") > 0, " in (" + strTmp + ")))", "=" + strTmp + "))"));
                        }
                        else
                        {
                            AccessFilter = Conversions.ToString(Conversions.ToString(AccessFilter + Interaction.IIf(Strings.Len(AccessFilter) > 0, " or ", "") + strCatg) + Interaction.IIf(Strings.InStr(1, strTmp, ",") > 0, " in (" + strTmp + ")", "=" + strTmp));
                        }
                    }

                    if ((Strings.UCase(strCatg) ?? "") == "A")
                    {
                        return "";
                    }
                    else
                    {
                        return " and ( " + AccessFilter + " )";
                    }
                }
            }
            else
            {
                Strsql = "if (select count(*) from sysobjects with (noLock) where name='LoginAccess') > 0 select * from loginAccess with (noLock) where la_userID='" + userId + "' order by la_grouping else select * from group_master with (noLock) where 1=2";
                DataTable dtLogin = objUtility.OpenDataTable(Strsql);

                if (dtLogin.Rows.Count > 0)
                {
                    short j;
                    string strCatg = "";
                    string strTmp;
                    j = 0;
                    strsql = "";
                    while (j < dtLogin.Rows.Count)
                    {
                        strCatg = dtLogin.Rows[j]["La_grouping"].ToString();
                        strTmp = "";
                        while (strCatg == dtLogin.Rows[j]["La_grouping"].ToString())
                        {
                            strTmp = strTmp + "'" + Strings.Trim(dtLogin.Rows[j]["la_grcode"].ToString()) + "',";
                            j = Conversions.ToShort(j + 1);
                            if (j >= dtLogin.Rows.Count)
                            {
                                break;
                            }
                        }

                        strsql = strTmp;    // for Branches 
                        strTmp = Strings.Mid(strTmp, 1, Strings.Len(strTmp) - 1);
                        var switchExpr = Strings.UCase(strCatg);
                        switch (switchExpr)
                        {
                            case "B":
                                strCatg = "cm_brboffcode";
                                break;
                            case "G":
                                strCatg = "cm_groupcd";
                                break;
                            case "F":
                                strCatg = "cm_familycd";
                                break;
                            case "R":
                                strCatg = "cm_remisser";
                                break;
                            case "A":
                                break; // TODO: might not be correct. Was : Exit Do
                            case "C":
                                strCatg = "cm_cd";
                                break;
                            case "M":
                                strCatg = "cm_dpactno";
                                break;
                        }
                        AccessFilter = AccessFilter + (Strings.Len(AccessFilter) > 0 ? " or " : "") + strCatg + (Strings.InStr(1, strTmp, ",", CompareMethod.Text) > 0 ? " in (" + strTmp + ")" : "=" + strTmp);
                    }

                    if (Strings.UCase(strCatg) == "A")
                    {
                        return "";
                    }
                    else
                    {
                        return "and (" + AccessFilter + ")";
                    }
                }
                else
                {
                    Strsql = "select um_brcode from user_master with (noLock) where um_user_id='" + userId + "'";
                    DataTable dtBranch = objUtility.OpenDataTable(Strsql);

                    if (dtBranch.Rows.Count > 0)
                    {
                        if (dtBranch.Rows[0][0].ToString().Trim() == "<ALL>")
                        {
                            return "";
                        }
                        string[] Branch = dtBranch.Rows[0][0].ToString().Split('|');
                        for (int i = 0; i < Branch.Length - 1; i++)
                        {
                            AccessFilter = AccessFilter + "'" + Branch[i] + "',";
                        }
                        if (AccessFilter != string.Empty)
                        {
                            AccessFilter = AccessFilter.Substring(0, AccessFilter.Length - 1);
                            AccessFilter = " and cm_brboffcode in (" + AccessFilter + ")";
                            return AccessFilter.ToString();
                        }
                        else
                        {
                            return "";
                        }
                    }
                    else
                    {
                        return "";
                    }
                }
            }
            return null;
        }

        private string FillConfigParametersString()
        {
            try
            {
                _configuration["Segments"] = objUtility.GetWebParameter("segments");
                _configuration["ColourTitle"] = objUtility.GetWebParameter("cLR_TITLE");
                _configuration["ColourMenu"] = objUtility.GetWebParameter("cLR_MENU");
                _configuration["ColourHeader"] = objUtility.GetWebParameter("cLR_HEADER");
                _configuration["ColourTotals"] = objUtility.GetWebParameter("cLR_TOTALS");
                _configuration["IsTradeWeb"] = objUtility.GetWebParameter("isTradeWeb");
                _configuration["TPlus"] = objUtility.AddBracket(objUtility.GetWebParameter("tPlus"));
                _configuration["TPlusES"] = objUtility.AddBracket(objUtility.GetWebParameter("tPlusES"));
                _configuration["Cross"] = objUtility.AddBracket(objUtility.GetWebParameter("cross"));
                _configuration["CrossEs"] = objUtility.AddBracket(objUtility.GetWebParameter("crossEs"));
                _configuration["Estro"] = objUtility.AddBracket(objUtility.GetWebParameter("estro"));
                _configuration["EstroEs"] = objUtility.AddBracket(objUtility.GetWebParameter("estroEs"));
                _configuration["Commex"] = objUtility.AddBracket(objUtility.GetWebParameter("commex"));
                _configuration["CommexEs"] = objUtility.AddBracket(objUtility.GetWebParameter("commexEs"));
                _configuration["CAPTCHA"] = objUtility.AddBracket(objUtility.GetWebParameter("cAPTCHA"));

                _configuration["SMTPHOST"] = objUtility.AddBracket(objUtility.GetWebParameter("sMTPHOST"));
                _configuration["PANASPASSWORD"] = objUtility.AddBracket(objUtility.GetWebParameter("pANASPASSWORD"));
                _configuration["GroupLogin"] = objUtility.AddBracket(objUtility.GetWebParameter("groupLogin"));
                _configuration["ShowRupeeSymbol"] = objUtility.AddBracket(objUtility.GetWebParameter("showRupeeSymbol"));
                _configuration["CLR_WEBSKIN"] = objUtility.AddBracket(objUtility.GetWebParameter("cLR_WEBSKIN"));
                _configuration["FTSOSURL"] = objUtility.AddBracket(objUtility.GetWebParameter("fTSOSURL"));
                _configuration["PMSUrl"] = objUtility.AddBracket(objUtility.GetWebParameter("pMSUrl"));
                _configuration["KYCPDF"] = objUtility.AddBracket(objUtility.GetWebParameter("kYCPDF"));
                _configuration["OrganizationName"] = objUtility.AddBracket(objUtility.GetWebParameter("organizationName"));
                _configuration["TNetInvplUrl"] = objUtility.AddBracket(objUtility.GetWebParameter("tNetInvplUrl"));
                _configuration["OUTPOSURL"] = objUtility.AddBracket(objUtility.GetWebParameter("oUTPOSURL"));
                _configuration["SMTPSERVER"] = objUtility.AddBracket(objUtility.GetWebParameter("sMTPSERVER"));
                _configuration["SECURITYPROT"] = objUtility.AddBracket(objUtility.GetWebParameter("sECURITYPROT"));
                _configuration["TCM"] = objUtility.mfnGetSysSplFeatureCommodity("TCM").ToString();

                return "success";
            }
            catch (Exception ex)
            {
                return ex.Message.ToString();

            }
        }

        public dynamic GetDebugFlag(string ModuleName, string FunctionName)
        {
            string strDebugFlag = "N";
            string sql = "select SerialNo, ProcedureName, IsDebugEnabled, FunctionName from tbl_GenericAPIDefinition";
            sql += " where ProjectName='TradeWebAPI' and ModuleName='" + ModuleName + "' and FunctionName='" + FunctionName + "'";
            DataTable dt = objUtility.OpenDataTable(sql);
            if (dt.Rows.Count > 0)
            {
                strDebugFlag = dt.Rows[0]["IsDebugEnabled"].ToString().Trim();
            }

            return strDebugFlag;
        }

        public dynamic Verify2FA(string userId, string password, string loginAs, bool branch, string loginRole, string role, string strIdentity, int expTime, int refreshExpTime, DataTable userData)
        {
            var guidVal = Guid.NewGuid();

            if ((loginRole == "Admin" || loginRole == "TradeMobile" || loginRole == "EstroWeb" || loginRole == "CrossWeb") && branch == false)
            {
                objUtility.ExecuteSQL("update Login_Session set ls_token = '" + guidVal + "', ls_logintype = '" + (loginRole == "TradeMobile" ? "M" : "C") + "', ls_logintm = '" + DateTime.Now.ToString("HH:mm:ss") + "'  Where ls_code = '" + userId + "' And ls_logindt = '" + DateTime.Now.ToString("yyyyMMdd") + "'  And len(ls_token) < 3");
            }

            string strUserAccess = "";
            if (userData.Columns.Contains("UserAccess"))//branch
            {
                strUserAccess = userData.Rows[0]["UserAccess"].ToString().Trim();
            }
            var tokenString = branch == true ? GenerateJSONWebTokenUser(new TradeWebLoginModel { username = userId, password = password, role = loginRole, guid = guidVal.ToString(), tokenExpTime = expTime, useraccess = strUserAccess }) : GenerateJSONWebToken(new TradeWebLoginModel { username = userId, password = password, role = loginRole, guid = guidVal.ToString(), tokenExpTime = expTime });

            string Strsql = "update OTP_Master set OTP_Status='M' where OTP_Identity='" + strIdentity + "'";
            objUtility.ExecuteSQL(Strsql);

            //var datetimeExp = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
            var datetimeExp = DateTime.UtcNow;
            var refreshToken = objUtility.GenerateRefreshToken(userId, loginAs, loginRole, refreshExpTime);

            tokenResponseNew result = new tokenResponseNew
            {
                status = true,
                message = "success",
                status_code = (int)HttpStatusCode.OK,
                token = tokenString,
                tokenExpireTime = datetimeExp.AddMinutes(expTime).ToString(UtcDateTimeFormat),
                refreshToken = refreshToken,
                refreshTokenExpireTime = datetimeExp.AddMinutes(refreshExpTime).ToString(UtcDateTimeFormat),
                data = userData
            };
            return JsonConvert.SerializeObject(result, Formatting.Indented);
        }

        public (string tokenString, string refreshToken) RefreshToken(string UserCode, string role, string guidVal, int expTime, string strUserAccess, string strLoginAs, int refExpTime, bool branch)
        {
            var tokenString = branch == true ? GenerateJSONWebTokenUser(new TradeWebLoginModel { username = UserCode, password = "", role = role, guid = guidVal.ToString(), tokenExpTime = expTime, useraccess = strUserAccess }) : GenerateJSONWebToken(new TradeWebLoginModel { username = UserCode, password = "", role = role, guid = guidVal.ToString(), tokenExpTime = expTime });
            var refreshToken = objUtility.GenerateRefreshToken(UserCode, strLoginAs, role, refExpTime);
            return (tokenString, refreshToken);
        }

        public dynamic ImportLargeFileRepo(object request)
        {
            string ProcedureName = "", fileSeqNo = "", batchNo = "", userId = "", procParamJson = "", strFilters = "";
            int SerialNo = 0;
            System.Data.DataSet dsSP = new DataSet();

            var jsonObj = JsonConvert.DeserializeObject<dynamic>(request.ToString());
            fileSeqNo = jsonObj["FileSeqNo"];
            batchNo = jsonObj["BatchNo"];
            userId = jsonObj["UserId"];
            strFilters = jsonObj["X_Filter"].ToString();
            procParamJson = jsonObj["InputJson"].ToString();
            string sql = "select SerialNo, ProcedureName, IsDebugEnabled, FunctionName from tbl_GenericAPIDefinition";
            sql += " where ProjectName='TradeWebAPI' and ModuleName='GENERICFILE' and FunctionName='IMPORT'";
            DataTable dt = objUtility.OpenDataTable(sql);
            if (dt.Rows.Count > 0)
            {
                SerialNo = Convert.ToInt32(dt.Rows[0]["SerialNo"].ToString().Trim());
                ProcedureName = dt.Rows[0]["ProcedureName"].ToString().Trim();
            }
            if (SerialNo > 0)
            {
                Dictionary<string, string> dictParam = new Dictionary<string, string>();
                {
                    dictParam.Add("@StrFileSeqNo", fileSeqNo);
                    dictParam.Add("@strBatchNo", batchNo);
                    dictParam.Add("@str_Userid", userId);
                    dictParam.Add("@i_vcInputJson", procParamJson.Replace("'", "''"));
                    dictParam.Add("@StrFiltters", strFilters);
                }
                dsSP = objUtility.ExecuteCommonSP("stpr_ImportFileAPI", dictParam);
                return dsSP;
            }
            return dsSP;
        }

        public dynamic VerifyBiometric(string userId, string loginAs, string role, string strModuleName)
        {
            //var datetimeExp = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
            var datetimeExp = DateTime.UtcNow;
            var userList = UserDetails(userId, "", true, strModuleName, role);

            int expTime = Convert.ToInt32(_configuration["accessTokenExpireTime"]);
            int refreshExpTime = 0;

            if (objUtility.GetWebParameter("AccTokenExpTm").Trim() != "")
            {
                expTime = Convert.ToInt32(objUtility.GetWebParameter("AccTokenExpTm").Trim());
            }
            else
            {
                expTime = Convert.ToInt32(_configuration["tokenExpireTime"]);
            }
            if (objUtility.GetWebParameter("RefTokenExpTm").Trim() != "")
            {
                refreshExpTime = Convert.ToInt32(objUtility.GetWebParameter("RefTokenExpTm").Trim());
            }
            else
            {
                refreshExpTime = Convert.ToInt32(_configuration["refreshTokenExpireTime"]);
            }

            Guid guidVal = Guid.NewGuid();
            var tokenString = GenerateJSONWebToken(new TradeWebLoginModel { username = userId, password = "", role = role, guid = guidVal.ToString(), tokenExpTime = expTime });
            var refreshToken = objUtility.GenerateRefreshToken(userId, loginAs, role, refreshExpTime);

            tokenResponseNew result = new tokenResponseNew
            {
                status = true,
                message = "success",
                status_code = (int)HttpStatusCode.OK,
                token = tokenString,
                tokenExpireTime = datetimeExp.AddMinutes(expTime).ToString(UtcDateTimeFormat),
                refreshToken = refreshToken,
                refreshTokenExpireTime = datetimeExp.AddMinutes(refreshExpTime).ToString(UtcDateTimeFormat),
                data = userList.data
            };
            var jsonData = JsonConvert.SerializeObject(result, Formatting.Indented);
            return jsonData;
        }

        public async Task<DataSet> GenerateQuestPDF(string reportName, string reportDisplayName, DataSet dts, string reportType, string userId)
        {
            if (dts == null || dts.Tables.Count == 0)
                return CreateEmptyResultTable();

            try
            {
                string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Typst");
                string logoPath = Path.Combine(baseDir, "Assets", "CompanyLogo.png");
                string headerImagePath = string.Empty;

                objUtility.TypstCreateCompanyLogo(baseDir);

                DataTable sourceTable = dts.Tables[0];

                if (sourceTable.Rows.Count == 0)
                    return CreateEmptyResultTable();

                // 🔹 Header Image Handling
                string compCode = sourceTable.Rows[0]["CompanyCode"].ToString();
                if (objUtility.GetSysParmSt("WKYHDRIMG", "").Trim() == "Y")
                {
                    baseDir = Path.Combine(baseDir, "Assets");
                    headerImagePath = objUtility.GetCompanyHeaderImageAsync(baseDir, compCode);
                }
                if (reportType == "P")
                {
                    var pdfResult = await questService.DownloadSamplePdfQuestForIncomeDormant(sourceTable, headerImagePath, logoPath);
                    return pdfResult;
                }
                else
                {
                    // 🔹 Process PDF + Email Insert
                    var result = await questService.ProcessAsync(
                        sourceTable,
                        headerImagePath,
                        logoPath, userId);

                    return result;
                }
            }
            catch (Exception)
            {
                // Let original stack trace remain intact
                throw;
            }
        }

        public async Task<DataSet> GenerateQuestPDFusingDynamicJson(string processType, DataTable dtHeader, string userId, string clientCode = "")
        {
            try
            {
                string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Typst");
                string logoPath = Path.Combine(baseDir, "Assets", "CompanyLogo.png");
                string headerImagePath = string.Empty;

                // Create company logo
                objUtility.TypstCreateCompanyLogo(baseDir);

                // Get header image (if enabled)
                if (objUtility.GetWebParameter("LTRHDRIMG").Trim().Equals("Y", StringComparison.OrdinalIgnoreCase))
                {
                    string companyCode = dtHeader.Rows[0]["CompanyCode"].ToString();

                    string assetsPath = Path.Combine(baseDir, "Assets");
                    headerImagePath = objUtility.GetCompanyHeaderImageAsync(assetsPath, companyCode);
                }

                // Get PDF JSON definition
                var dsJson = new DataSet();

                SqlParameter[] parameters =
                {
                    new SqlParameter("@DocumentName", processType)
                };

                objUtility.Execute_SP_DataSet("usp_GeneratePdfDocumentJson", parameters, dsJson);

                if (dsJson.Tables.Count == 0 || dsJson.Tables[0].Rows.Count == 0)
                {
                    return CreateResultTable("E", "Document definition not found.");
                }

                string jsonData = Convert.ToString(dsJson.Tables[0].Rows[0][0]);

                if (string.IsNullOrWhiteSpace(jsonData))
                {
                    return CreateResultTable("E", "JSON document definition is empty.");
                }

                if (jsonData.Contains("##DPSERVER##"))
                {
                    string dpPrefix = "";

                    if (objUtility.fnchkTable("Other_Products"))
                    {
                        DataTable dtDP = objUtility.OpenDataTable("select CASE WHEN NULLIF(LTRIM(RTRIM(OP_Server)), '') IS NOT NULL THEN QUOTENAME(LTRIM(RTRIM(OP_Server))) + '.' ELSE '' END + QUOTENAME(RTRIM(LTRIM(OP_DataBase))) + '.' + QUOTENAME(RTRIM(LTRIM(OP_Owner))) AS FullTablePrefix from Other_Products where OP_Product in ('Cross','Estro') and OP_Status='A'");
                        if (dtDP.Rows.Count > 0)
                        {
                            dpPrefix = dtDP.Rows[0]["FullTablePrefix"].ToString().Trim();
                        }
                    }

                    // The value is spliced into SQL, so whitelist its characters
                    if (!Regex.IsMatch(dpPrefix, @"^[A-Za-z0-9_\[\]\.,]+$"))
                        return CreateResultTable("E", "DP database parameter is missing or invalid.");

                    jsonData = jsonData.Replace("##DPSERVER##", dpPrefix);
                }

                if (!string.IsNullOrWhiteSpace(clientCode))
                {
                    return await _questServc.QuestPDFForClientAsync(dtHeader, jsonData, clientCode, logoPath, headerImagePath);
                }

                // Generate PDF
                //return await cflService.ClientFundLedgerProcessAsync(dtHeader, jsonData, userId, logoPath, headerImagePath);
                return await _questServc.QuestPDFProcessAsync(
                   dtHeader,
                   jsonData,
                   userId,
                   logoPath,
                   headerImagePath);
            }
            catch (Exception ex)
            {
                return CreateResultTable("E", ex.Message);
            }
        }

        private static DataSet CreateResultTable(string messageType, string message)
        {
            DataSet dsRtn = new DataSet();
            var dt = new DataTable();
            dt.Columns.Add("Flag", typeof(string));
            dt.Columns.Add("Message", typeof(string));

            dt.Rows.Add(messageType, message);
            dsRtn.Tables.Add(dt);
            dsRtn.Tables[0].TableName = "rs0";
            return dsRtn;
        }

        private DataSet CreateEmptyResultTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("MessageType", typeof(string));
            dt.Columns.Add("MessageText", typeof(string));
            dt.Rows.Add("Error", "Records not found");
            DataSet dsrtn = new DataSet();
            dsrtn.Tables.Add(dt);
            dsrtn.Tables[0].TableName = "rs0";
            return dsrtn;
        }

        public dynamic GetCompanyBranding()
        {
            string strsql = @"
DECLARE @DPID VARCHAR(500) = ''
DECLARE @CompanyName VARCHAR(50) = '', @strBase64 NVARCHAR(MAX) = ''

DECLARE @companyCount INT = 0
SELECT @companyCount = ISNULL(SUM(ISNULL(cnt,0)),0)
FROM ( SELECT COUNT(0) Cnt From Entity_master with (NoLock)
WHERE em_bse <> 'N' and isNull(em_bclearingno,'') in ('189')
UNION ALL
SELECT COUNT(0) Cnt From Entity_master with (NoLock)
WHERE em_nse <> 'N' and isNull(em_nclearingno,'') in ('07277') ) a

IF @companyCount = 0
BEGIN
    SELECT @DPID = em_cd, @CompanyName = em_name
    FROM Entity_Master with (nolock) where em_cd = (select min(em_cd) from Entity_master)
END
ELSE
BEGIN
    SELECT @DPID = em_cd, @CompanyName = em_name
    FROM Entity_Master with (nolock) where em_cd = 'B'
END

SELECT @strBase64 =
   (SELECT CAST(img_logo AS VARBINARY(MAX))
    FOR XML PATH(''), BINARY BASE64)
FROM Images(NOLOCK)
WHERE img_desc = 'Company Logo'

SELECT @DPID AS CompanyCode, @CompanyName AS CompanyName, CompanyLogo = @strBase64
";
            DataTable dt = objUtility.OpenDataTable(strsql);
            if (dt != null && dt.Rows.Count > 0)
            {
                return dt;
            }
            return null;
        }
    }
}
