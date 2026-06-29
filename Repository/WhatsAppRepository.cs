using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Reporting.NETCore;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TradeWeb.API.Models;
using Document = iTextSharp.text.Document;

namespace TradeWeb.API.Repository
{
    public class WhatsAppRepository : IWhatsAppRepository
    {
        #region Class level declarations.
        private readonly IConfiguration _configuration;
        private readonly UtilityCommon objUtility;
        private string strsql = "";
        private string strConnecton = "";
        string strToken = string.Empty;
        IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _environment;
        #endregion

        #region Constructor
        public WhatsAppRepository(IConfiguration configuration, UtilityCommon objUtility, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            this.objUtility = objUtility;
            _httpContextAccessor = httpContextAccessor;
            _environment = environment;
        }
        #endregion

        public dynamic ChatBot(WhatsAppBotRequest req)
        {
            long srNo = 0;
            try
            {
                WhatsAppResponse response = new WhatsAppResponse();
                string connetionString = objUtility.GetConnectionStr();
                string[] values = new string[] { };

                srNo = objUtility.InsertLog(JsonConvert.SerializeObject(req), "");

                if (req.data != null && req.data.user_input != null)
                {
                    values = req.data.user_input.Split(',');
                }
                //string[] values = req.data.user_input.Split(',');
                string strActType = "";
                string strCode = "";
                string strMenu = "";
                string strRepDuration = "";
                string strSubMenu = "";

                foreach (var item in values)
                {
                    var parm = item.Split(':');
                    if (parm[0].ToString().Trim().ToUpper() == "ACCOUNTTYPE")
                    {
                        strActType = parm[1].ToString().Trim();
                    }

                    if (parm[0].ToString().Trim().ToUpper() == "CODE")
                    {
                        strCode = parm[1].ToString().Trim();
                    }

                    if (parm[0].ToString().Trim().ToUpper() == "REPORT_CHOICE")
                    {
                        strMenu = parm[1].ToString().Trim();
                    }

                    if (parm[0].ToString().Trim().ToUpper() == "REPORT_DURATION")
                    {
                        strRepDuration = parm[1].ToString().Trim();
                    }

                    if (parm[0].ToString().Trim().ToUpper() == "SUBREPORT_CHOICE")
                    {
                        strSubMenu = parm[1].ToString().Trim();
                    }
                }

                if (objUtility.mfnGetSysSplFeature("BOT"))
                {
                    if (!objUtility.fnchkTable("tbl_WhatsAppChatbotConfig"))
                    {
                        strsql = "CREATE TABLE tbl_WhatsAppChatbotConfig(";
                        strsql += "[SerialNo] [int] IDENTITY(1,1) NOT NULL,";
                        strsql += "[ReportName] [varchar](50) NOT NULL,";
                        strsql += "[Product] [varchar](50) NOT NULL,";
                        strsql += "[ProcedureName] [varchar](100) NOT NULL,";
                        strsql += "[ProcedureParam] [varchar](500) NOT NULL,";
                        strsql += "[FileFormat] [char](10) NOT NULL,";
                        strsql += "[ReportMessage] [varchar](500) NOT NULL,";
                        strsql += "[PrintAddress] [char](1) NOT NULL,";
                        strsql += "[ReportHeading] [varchar](100) NOT NULL,";
                        strsql += "[ReportOrientation] [char](1) NOT NULL,";
                        strsql += "[IsActive] [varchar](1) NOT NULL,";
                        strsql += "[Userid] [varchar](50) NOT NULL,";
                        strsql += "[UpdateTimeStamp] [datetime] NOT NULL";
                        strsql += ")";
                        objUtility.ExecuteSQL(strsql);

                        strsql = "Insert into tbl_WhatsAppChatbotConfig(ReportName, Product, ProcedureName, ProcedureParam, FileFormat, ReportMessage, PrintAddress, ReportHeading, ReportOrientation, IsActive, Userid, UpdateTimeStamp)";
                        strsql += "values('Ledger Balance', 'Trading', 'stpr_GetChatbotData', '<ReportCode>{report_choice}</ReportCode><Product>{accounttype}</Product><ParamValue></ParamValue><UserId>{code}</UserId>', 'text', 'Ledger Balance for Client <1> : <2>', 'N', 'Ledger Balance As on <ASONDATE>', 'P', 'Y', 'SUPER', GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_WhatsAppChatbotConfig(ReportName, Product, ProcedureName, ProcedureParam, FileFormat, ReportMessage, PrintAddress, ReportHeading, ReportOrientation, IsActive, Userid, UpdateTimeStamp)";
                        strsql += "values('Ledger Report', 'Trading', 'stpr_GetChatbotData', '<ReportCode>{report_choice}</ReportCode><Product>{accounttype}</Product><ParamValue>{report_duration}</ParamValue><UserId>{code}</UserId>', 'base64', 'Here is your Ledger Report. Please enter your PAN in Capital Letters as the password, when prompted, to open the .pdf file.', 'N', 'Ledger Report from <FROMDATE> to <TODATE>', 'P', 'Y', 'SUPER', GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_WhatsAppChatbotConfig(ReportName, Product, ProcedureName, ProcedureParam, FileFormat, ReportMessage, PrintAddress, ReportHeading, ReportOrientation, IsActive, Userid, UpdateTimeStamp)";
                        strsql += "values('Ledger Report', 'Demat', 'stpr_GetChatbotData', '<ReportCode>{report_choice}</ReportCode><Product>{accounttype}</Product><ParamValue>{report_duration}</ParamValue><UserId>{code}</UserId>', 'base64', 'Here is your Ledger Report. Please enter your PAN in Capital Letters as the password, when prompted, to open the .pdf file.', 'Y', 'DP Ledger Report from <FROMDATE> to <TODATE>', 'P', 'Y', 'SUPER', GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_WhatsAppChatbotConfig(ReportName, Product, ProcedureName, ProcedureParam, FileFormat, ReportMessage, PrintAddress, ReportHeading, ReportOrientation, IsActive, Userid, UpdateTimeStamp)";
                        strsql += "values('Holding', 'Trading', 'stpr_GetChatbotData', '<ReportCode>{report_choice}</ReportCode><Product>{accounttype}</Product><ParamValue></ParamValue><UserId>{code}</UserId>', 'base64', 'Here is your Holding Report. Please enter your PAN in Capital Letters as the password, when prompted, to open the .pdf file.', 'N', 'Holding Report As on <ASONDATE>', 'L', 'Y', 'SUPER', GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_WhatsAppChatbotConfig(ReportName, Product, ProcedureName, ProcedureParam, FileFormat, ReportMessage, PrintAddress, ReportHeading, ReportOrientation, IsActive, Userid, UpdateTimeStamp)";
                        strsql += "values('Holding', 'Demat', 'stpr_GetChatbotData', '<ReportCode>{report_choice}</ReportCode><Product>{accounttype}</Product><ParamValue></ParamValue><UserId>{code}</UserId>', 'base64', 'Here is your Holding Report. Please enter your PAN in Capital Letters as the password, when prompted, to open the .pdf file.', 'N', 'DP Holding Report As on <ASONDATE>', 'P', 'Y', 'SUPER', GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_WhatsAppChatbotConfig(ReportName, Product, ProcedureName, ProcedureParam, FileFormat, ReportMessage, PrintAddress, ReportHeading, ReportOrientation, IsActive, Userid, UpdateTimeStamp)";
                        strsql += "values('Outstanding Position', 'Trading', 'stpr_GetChatbotData', '<ReportCode>{report_choice}</ReportCode><Product>{accounttype}</Product><ParamValue></ParamValue><UserId>{code}</UserId>', 'base64', 'Here is your Outstanding Position Report. Please enter your PAN in Capital Letters as the password, when prompted, to open the .pdf file.', 'N', 'Outstanding Position As on <ASONDATE>', 'L', 'Y', 'SUPER', GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_WhatsAppChatbotConfig(ReportName, Product, ProcedureName, ProcedureParam, FileFormat, ReportMessage, PrintAddress, ReportHeading, ReportOrientation, IsActive, Userid, UpdateTimeStamp)";
                        strsql += "values('Margin Statement', 'Trading', 'stpr_GetChatbotData', '<ReportCode>{report_choice}</ReportCode><Product>{accounttype}</Product><ParamValue></ParamValue><UserId>{code}</UserId>', 'Direct', 'Here is your Margin Statement. Please enter your PAN in Capital Letters as the password, when prompted, to open the .pdf file.', 'N', '', 'P', 'Y', 'SUPER', GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_WhatsAppChatbotConfig(ReportName, Product, ProcedureName, ProcedureParam, FileFormat, ReportMessage, PrintAddress, ReportHeading, ReportOrientation, IsActive, Userid, UpdateTimeStamp)";
                        strsql += "values('Combined Contract Note', 'Trading', 'stpr_GetChatbotData', '<ReportCode>{report_choice}</ReportCode><Product>{accounttype}</Product><ParamValue></ParamValue><UserId>{code}</UserId>', 'Direct', 'Here is your Combined Contract Note. Please enter your PAN in Capital Letters as the password, when prompted, to open the .pdf file.', 'N', '', 'P', 'Y', 'SUPER', GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_WhatsAppChatbotConfig(ReportName, Product, ProcedureName, ProcedureParam, FileFormat, ReportMessage, PrintAddress, ReportHeading, ReportOrientation, IsActive, Userid, UpdateTimeStamp)";
                        strsql += "values('CML', 'Demat', 'stpr_GetChatbotData', '<ReportCode>{report_choice}</ReportCode><Product>{accounttype}</Product><ParamValue></ParamValue><UserId>{code}</UserId>', 'RDL', 'Here is your CML Report', 'N', '', 'P', 'Y', 'SUPER', GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        //strsql = "Insert into tbl_WhatsAppChatbotConfig(ReportName, Product, ProcedureName, ProcedureParam, FileFormat, ReportMessage, PrintAddress, ReportHeading, ReportOrientation, IsActive, Userid, UpdateTimeStamp)";
                        //strsql += "values('Capital Gain Loss', 'Trading', 'stpr_GetChatbotData', '<ReportCode>{report_choice}</ReportCode><Product>{accounttype}</Product><ParamValue>{report_duration}</ParamValue><SubReport>{subreport_choice}</SubReport><UserId>{code}</UserId>', 'base64', 'Here is your Capital Gain Loss Report. Please enter your PAN as the password, when prompted, to open the .pdf file.', 'N', '<REPORTCHOICE> <SUBREPORTCHOICE> Report <REPORTDURATION>|Missing purchase details', 'L', 'Y', 'SUPER', GETDATE())";
                    }

                    if (!objUtility.fnchkTable("tbl_WhatsAppChatbotParam"))
                    {
                        strsql = "CREATE TABLE tbl_WhatsAppChatbotParam(";
                        strsql += "[SerialNo] [int] IDENTITY(1,1) NOT NULL,";
                        strsql += "[ReportName] [varchar](50) NOT NULL,";
                        strsql += "[Product] [varchar](50) NOT NULL,";
                        strsql += "[ParamValue] [varchar](100) NOT NULL,";
                        strsql += "[Userid] [varchar](50) NOT NULL,";
                        strsql += "[UpdateTimeStamp] [datetime] NOT NULL";
                        strsql += ")";
                        objUtility.ExecuteSQL(strsql);

                        strsql = "Insert into tbl_WhatsAppChatbotParam values('Ledger Balance1','Trading','As on Date','SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_WhatsAppChatbotParam values('Ledger Report','Trading','Last 7 Days','SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_WhatsAppChatbotParam values('Ledger Report','Trading','Current Month','SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_WhatsAppChatbotParam values('Ledger Report','Trading','Current FY','SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_WhatsAppChatbotParam values('Ledger Report','Trading','Previous FY','SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_WhatsAppChatbotParam values('Ledger Report','Demat','Last 7 Days','SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_WhatsAppChatbotParam values('Ledger Report','Demat','Current Month','SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_WhatsAppChatbotParam values('Ledger Report','Demat','Current FY','SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_WhatsAppChatbotParam values('Ledger Report','Demat','Previous FY','SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                    }

                    if (!objUtility.fnchkTable("tbl_WhatsAppChatbotActions"))
                    {
                        strsql = "CREATE TABLE tbl_WhatsAppChatbotActions(";
                        strsql += "[SerialNo] [int] IDENTITY(1,1) NOT NULL,";
                        strsql += "[ActionName] [varchar](50) NOT NULL,";
                        strsql += "[NextAction] [varchar](50) NOT NULL,";
                        strsql += "[Message] [varchar](500) NOT NULL,";
                        strsql += "[Prompt] [varchar](500) NOT NULL,";
                        strsql += "[Userid] [varchar](50) NOT NULL,";
                        strsql += "[UpdateTimeStamp] [datetime] NOT NULL";
                        strsql += ")";
                        objUtility.ExecuteSQL(strsql);

                        strsql = "Insert into tbl_WhatsAppChatbotActions values('start_interaction','accounttype_code','Welcome to Abc Securities Ltd.\nMember:-NSE/BSE(Equity,FO,CDS), Mutual Fund\nDepository Participant(NSDL,CDSL).','Kindly Select Your DEMAT or TRADING CODE from the below list','SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_WhatsAppChatbotActions values('accounttype_code','report_choice','Now You can get your Back-Office Reports on Your Fingertips.','Please select the Report as per your requirement.','SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_WhatsAppChatbotActions values('report_choice','report_duration','You have selected <REPORT_CHOICE>','Now, please choose the report duration:','SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_WhatsAppChatbotActions values('report_duration','','Here is your <REPORT_CHOICE>','','SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_WhatsAppChatbotActions values('start_interaction_none','','Thank you for contacting Abc Securities Ltd. You do not have an account with Abc, contact us to open an account.','','SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                    }

                    if (!objUtility.fnchkTable("tbl_ChatbotPDFConfig"))
                    {
                        strsql = "CREATE TABLE tbl_ChatbotPDFConfig(";
                        strsql += "[SerialNo] [int] IDENTITY(1,1) NOT NULL,";
                        strsql += "[ReportName] [varchar](50) NOT NULL,";
                        strsql += "[Product] [varchar](50) NOT NULL,";
                        strsql += "[ColumnType] [varchar](50) NOT NULL,";
                        strsql += "[ColumnName] [varchar](50) NOT NULL,";
                        strsql += "[ColumnHeading] [varchar](50) NOT NULL,";
                        strsql += "[ColumnWidth] [int] NOT NULL,";
                        strsql += "[ColumnAlignement] [varchar](1) NOT NULL,";
                        strsql += "[ColumnFormat] [varchar](50) NOT NULL,";
                        strsql += "[DecimalPlace] [int] NOT NULL,";
                        strsql += "[ColumnTotal] [varchar](1) NOT NULL,";
                        strsql += "[OrderBy] [int] NOT NULL,";
                        strsql += "[Userid] [varchar](50) NOT NULL,";
                        strsql += "[UpdateTimeStamp] [datetime] NOT NULL";
                        strsql += ")";
                        objUtility.ExecuteSQL(strsql);

                        strsql = "Insert into tbl_ChatbotPDFConfig values('Holding','Demat','Grid','ScripName','ScripName',28,'L','',0,'N',1,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Holding','Demat','Grid','ISIN','ISIN',12,'L','',0,'N',2,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Holding','Demat','Grid','AccountType','AccountType',12,'L','',0,'N',3,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Holding','Demat','Grid','Holding','Qty',15,'R','',2,'Y',4,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Holding','Demat','Grid','ClosingPrice','ClosingPrice',12,'R','',2,'Y',5,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Holding','Demat','Grid','MarketValue','MarketValue',12,'R','',2,'Y',6,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Holding','Trading','Grid','ScripCode','ScripCode',10,'L','',0,'N',1,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Holding','Trading','Grid','ScripName','ScripName',12,'L','',0,'N',2,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Holding','Trading','Grid','ISIN','ISIN',13,'L','',0,'N',3,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Holding','Trading','Grid','FOCOLL','Pledge',8,'R','',0,'Y',4,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Holding','Trading','Grid','CUSPA','CUSPA',9,'R','',0,'Y',5,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Holding','Trading','Grid','DP','DP',8,'R','',0,'Y',6,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Holding','Trading','Grid','EXP','EXP',8,'R','',0,'Y',7,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Holding','Trading','Grid','UNDEL','UNDEL',8,'R','',0,'Y',8,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Holding','Trading','Grid','MTFBENF','MTF (Funded)',8,'R','',0,'Y',9,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Holding','Trading','Grid','MTFCOLL','MTF (Coll)',8,'R','',0,'Y',10,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Holding','Trading','Grid','POOL','POOL',8,'R','',0,'Y',11,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Holding','Trading','Grid','BEN','BEN',8,'R','',0,'Y',12,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Holding','Trading','Grid','TotalQty','TotalQty',10,'R','',0,'Y',13,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Holding','Trading','Grid','ClosingPrice','ClosingPrice',12,'R','',2,'Y',14,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Holding','Trading','Grid','MarketValue','MarketValue',12,'R','',2,'Y',15,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Holding','Trading','Grid','HairCut','HairCut',8,'R','',2,'N',16,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Holding','Trading','Grid','NetValue','NetValue',12,'R','',2,'Y',17,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Ledger Report','Demat','Grid','Date','Date',10,'L','dd/MM/yyyy',0,'0',1,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Ledger Report','Demat','Grid','Voucher','Voucher',11,'L','',0,'N',2,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Ledger Report','Demat','Grid','Chequeno','Cheque No',8,'L','',0,'N',3,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Ledger Report','Demat','Grid','Particular','Particular',32,'L','',0,'N',4,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Ledger Report','Demat','Grid','DebitAmount','Debit',11,'R','',2,'Y',5,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Ledger Report','Demat','Grid','CreditAmount','Credit',11,'R','',2,'Y',6,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Ledger Report','Demat','Grid','Balance','Balance',12,'R','',2,'Y',7,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Ledger Report','Demat','Grid','BalanceTag','Flag',5,'L','',0,'N',8,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Ledger Report','Trading','Grid','Date','Date',10,'L','dd/MM/yyyy',0,'0',1,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Ledger Report','Trading','Grid','Voucher','Voucher',8,'L','',0,'N',2,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Ledger Report','Trading','Grid','Chequeno','Cheque No',8,'L','',0,'N',3,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Ledger Report','Trading','Grid','Particular','Particular',32,'L','',0,'N',4,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Ledger Report','Trading','Grid','DebitAmount','Debit',12,'R','',2,'Y',5,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Ledger Report','Trading','Grid','CreditAmount','Credit',12,'R','',2,'Y',6,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Ledger Report','Trading','Grid','Balance','Balance',13,'R','',2,'Y',7,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Ledger Report','Trading','Grid','BalanceTag','Flag',5,'L','',0,'N',8,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Outstanding Position','Trading','Grid','Exchange','Exchange',7,'L','',0,'N',2,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Outstanding Position','Trading','Grid','Segment','Segment',6,'L','',0,'N',3,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Outstanding Position','Trading','Grid','SymbolDesc','SymbolDesc',10,'L','',0,'N',4,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Outstanding Position','Trading','Grid','ExpiryDdate','ExpiryDdate',6,'L','dd/MM/yyyy',0,'N',5,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Outstanding Position','Trading','Grid','Buy','BuyQty',5,'R','',0,'Y',6,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Outstanding Position','Trading','Grid','BuyAvgRate','BuyAvgRate',5,'R','',2,'N',7,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Outstanding Position','Trading','Grid','BuyValue','BuyValue',8,'R','',2,'Y',8,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Outstanding Position','Trading','Grid','Sale','Sale',5,'R','',0,'Y',9,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Outstanding Position','Trading','Grid','SaleAvgRate','SaleAvgRate',5,'R','',2,'N',10,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Outstanding Position','Trading','Grid','SaleValue','SaleValue',8,'R','',2,'Y',11,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Outstanding Position','Trading','Grid','Net','Net',5,'R','',0,'Y',12,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Outstanding Position','Trading','Grid','AvgRate','AvgRate',7,'R','',2,'N',13,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Outstanding Position','Trading','Grid','NetValue','NetValue',8,'R','',2,'Y',14,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Outstanding Position','Trading','Grid','CloseRate','CloseRate',7,'R','',2,'N',15,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                        strsql = "Insert into tbl_ChatbotPDFConfig values('Outstanding Position','Trading','Grid','ProfitLoss','ProfitLoss',8,'R','',2,'Y',16,'SUPER',GETDATE())";
                        objUtility.ExecuteSQL(strsql);
                    }

                    if (!objUtility.fnchkTable("tbl_GenericAPIDebugLog"))
                    {
                        strsql = "CREATE TABLE tbl_GenericAPIDebugLog(";
                        strsql += "[SerialNo] [int] IDENTITY(1,1) NOT NULL,";
                        strsql += "[ParentSerialNo] [int] NOT NULL,";
                        strsql += "[RequestSource] [varchar](50) NOT NULL,";
                        strsql += "[RequestUniqueID] [varchar](100) NOT NULL,";
                        strsql += "[RequestString] [varchar](max) NOT NULL,";
                        strsql += "[ResponseString] [varchar](max) NOT NULL,";
                        strsql += "[ExceptionMessage] [varchar](max) NOT NULL,";
                        strsql += "[UpdateBy] [varchar](50) NOT NULL,";
                        strsql += "[UpdateTimeStamp] [datetime] NOT NULL,";
                        strsql += " CONSTRAINT [PK_tbl_GenericAPIDebugLog] PRIMARY KEY NONCLUSTERED ";
                        strsql += "(SerialNo)";
                        strsql += ") ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]";
                        objUtility.ExecuteSQL(strsql);
                    }

                    if (Convert.ToInt16(objUtility.OpenDataTable("select count(0) from tbl_WhatsAppChatbotConfig where ReportName = 'Capital Gain Loss' and Product = 'Trading'").Rows[0][0].ToString()) > 0)
                    {
                        if (Convert.ToInt16(objUtility.OpenDataTable("select count(0) from tbl_WhatsAppChatbotParam where ReportName like 'Capital Gain Loss%' and Product = 'Trading'").Rows[0][0].ToString()) == 0)
                        {
                            strsql = "Insert into tbl_WhatsAppChatbotParam values('Capital Gain Loss','Trading','Actual PL Summary','SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_WhatsAppChatbotParam values('Capital Gain Loss','Trading','Actual PL Detail','SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_WhatsAppChatbotParam values('Capital Gain Loss','Trading','Notional Summary','SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_WhatsAppChatbotParam values('Capital Gain Loss','Trading','Notional Detail','SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_WhatsAppChatbotParam values('Capital Gain Loss','Trading','Dividend','SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_WhatsAppChatbotParam values('Capital Gain Loss|Actual PL Summary','Trading','Current Month','SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_WhatsAppChatbotParam values('Capital Gain Loss|Actual PL Summary','Trading','Current FY','SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_WhatsAppChatbotParam values('Capital Gain Loss|Actual PL Summary','Trading','Previous FY','SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_WhatsAppChatbotParam values('Capital Gain Loss|Actual PL Detail','Trading','Current Month','SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_WhatsAppChatbotParam values('Capital Gain Loss|Actual PL Detail','Trading','Current FY','SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_WhatsAppChatbotParam values('Capital Gain Loss|Actual PL Detail','Trading','Previous FY','SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_WhatsAppChatbotParam values('Capital Gain Loss|Notional Summary','Trading','Today Date','SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_WhatsAppChatbotParam values('Capital Gain Loss|Notional Summary','Trading','Previous FY End Date','SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_WhatsAppChatbotParam values('Capital Gain Loss|Notional Detail','Trading','Today Date','SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_WhatsAppChatbotParam values('Capital Gain Loss|Notional Detail','Trading','Previous FY End Date','SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_WhatsAppChatbotParam values('Capital Gain Loss|Dividend','Trading','Current Month','SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_WhatsAppChatbotParam values('Capital Gain Loss|Dividend','Trading','Current FY','SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_WhatsAppChatbotParam values('Capital Gain Loss|Dividend','Trading','Previous FY','SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                        }

                        if (Convert.ToInt16(objUtility.OpenDataTable("select count(0) from tbl_WhatsAppChatbotActions where ActionName = 'subreport_choice'").Rows[0][0].ToString()) == 0)
                        {
                            strsql = "Insert into tbl_WhatsAppChatbotActions values('subreport_choice','report_duration','You have selected <REPORT_CHOICE>','Now, please choose the report duration:','SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                        }

                        if (Convert.ToInt16(objUtility.OpenDataTable("select count(0) from tbl_ChatbotPDFConfig where ReportName like 'Capital Gain Loss%' and Product = 'Trading'").Rows[0][0].ToString()) == 0)
                        {
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Summary','Trading','Grid','TAG','Type',10,'L','',0,'N',1,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Summary','Trading','Grid','Scrip_Code','Scrip Code',12,'L','',0,'N',2,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Summary','Trading','Grid','ScripName','Scrip Name',10,'L','',0,'N',3,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Summary','Trading','Grid','BuyQty','Buy Qty',10,'R','',0,'Y',4,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Summary','Trading','Grid','BuyRate','Buy Rate',10,'R','',2,'N',5,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Summary','Trading','Grid','BuyValue','Buy Value',12,'R','',2,'Y',6,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Summary','Trading','Grid','SaleQty','Sell Qty',10,'R','',0,'Y',7,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Summary','Trading','Grid','SaleRate','Sell Rate',10,'R','',2,'N',8,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Summary','Trading','Grid','SaleValue','Sell Value',12,'R','',2,'Y',9,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Summary','Trading','Grid','SP_TERM_PL','Trading',12,'R','',2,'Y',10,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Summary','Trading','Grid','S_TERM_PL','Short Term',12,'R','',2,'Y',11,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Summary','Trading','Grid','L_TERM_PL','Long Term',12,'R','',2,'Y',12,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Summary','Trading','Grid','STT','STT',12,'R','',2,'Y',13,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Detail','Trading','Grid','Scrip_Code','Scrip Code',8,'L','',0,'N',1,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Detail','Trading','Grid','ScripName','Scrip Name',20,'L','',0,'N',2,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Detail','Trading','Grid','Tag','TrxType',10,'L','',0,'N',3,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Detail','Trading','Grid','SaleDate','Sell Date',10,'L','',0,'N',4,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Detail','Trading','Grid','SaleRate','Sell Rate',10,'R','',2,'N',5,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Detail','Trading','Grid','SaleValue','Sell Value',12,'R','',2,'Y',6,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Detail','Trading','Grid','NetQty','Traded Qty',10,'R','',0,'Y',7,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Detail','Trading','Grid','BuyDate','Buy Date',10,'L','',0,'N',8,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Detail','Trading','Grid','BuyRate','Buy Cost',10,'R','',2,'N',9,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Detail','Trading','Grid','BuyValue','Buy Value',12,'R','',2,'Y',10,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Detail','Trading','Grid','A112A_Rate','112A Rate',10,'R','',2,'N',11,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Detail','Trading','Grid','SP_TERM_PL','Trading',12,'R','',2,'Y',12,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Detail','Trading','Grid','S_TERM_PL','ShortTerm',12,'R','',2,'Y',13,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Detail','Trading','Grid','L_TERM_PL','LongTerm',12,'R','',2,'Y',14,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Detail','Trading','Grid','STT','STT',12,'R','',2,'Y',15,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Detail','Trading','Grid1','Scrip_Code','Scrip Code',8,'L','',0,'N',1,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Detail','Trading','Grid1','ScripName','Scrip Name',25,'L','',0,'N',2,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Detail','Trading','Grid1','BuyQty','Buy Qty',10,'R','',0,'N',3,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Detail','Trading','Grid1','SaleQty','Sell Qty',10,'L','',0,'N',4,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Detail','Trading','Grid1','NetQty','Net Qty',10,'R','',0,'N',5,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Detail','Trading','Grid1','NetValue','Net Value',12,'R','',2,'Y',6,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Detail','Trading','Grid1','CMP','Current Rate',10,'R','',2,'N',7,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Actual PL Detail','Trading','Grid1','CurrentValue','Current Value',12,'R','',2,'Y',8,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Notional Summary','Trading','Grid','ScripCode','Scrip Code',8,'L','',0,'N',1,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Notional Summary','Trading','Grid','ScripName','Scrip Name',20,'L','',0,'N',2,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Notional Summary','Trading','Grid','BuyQty','Holding Qty',8,'R','',0,'N',3,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Notional Summary','Trading','Grid','BuyRate','Holding Rate',8,'R','',2,'N',4,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Notional Summary','Trading','Grid','BuyValue','Holding Cost',12,'R','',2,'Y',5,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Notional Summary','Trading','Grid','CloseRate','Current Rate',8,'R','',2,'N',6,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Notional Summary','Trading','Grid','CurrentValue','Current Amount',12,'R','',2,'Y',7,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Notional Summary','Trading','Grid','ShortTerm','Short Term',12,'R','',2,'Y',8,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Notional Summary','Trading','Grid','LongTerm','Long Term',12,'R','',2,'Y',9,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Notional Summary','Trading','Grid','STT','STT',12,'R','',2,'Y',10,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Notional Detail','Trading','Grid','ScripCode','Scrip Code',8,'L','',0,'N',1,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Notional Detail','Trading','Grid','ScripName','Scrip Name',12,'L','',0,'N',2,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Notional Detail','Trading','Grid','TradeDate','Trade Date',10,'L','dd/MM/yyyy',0,'N',3,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Notional Detail','Trading','Grid','BuyQty','Holding Qty',8,'R','',0,'Y',4,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Notional Detail','Trading','Grid','BuyRate','Holding Rate',10,'R','',2,'N',5,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Notional Detail','Trading','Grid','A112A_Rate','112A Rate',8,'R','',2,'N',6,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Notional Detail','Trading','Grid','BuyValue','Holding Cost',12,'R','',2,'Y',7,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Notional Detail','Trading','Grid','CloseRate','Current Rate',8,'R','',2,'N',8,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Notional Detail','Trading','Grid','CurrentValue','Current Value',12,'R','',2,'Y',9,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Notional Detail','Trading','Grid','ShortTerm','Short Term',12,'R','',2,'Y',10,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Notional Detail','Trading','Grid','LongTerm','Long Term',12,'R','',2,'Y',11,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Notional Detail','Trading','Grid','STT','STT',12,'R','',2,'Y',12,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Notional Detail','Trading','Grid','PositionDays','Days',5,'R','',0,'N',13,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Dividend','Trading','Grid','DividendDate','Date',10,'L','dd/MM/yyyy',0,'N',1,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Dividend','Trading','Grid','ScripCode','Scrip Code',10,'L','',0,'N',2,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Dividend','Trading','Grid','ScripName','Scrip Name',30,'L','',0,'N',3,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Dividend','Trading','Grid','NoOfShare','Dividend Qty',8,'R','',2,'Y',4,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Dividend','Trading','Grid','DivRate','Dividend Rate',8,'R','',2,'N',5,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                            strsql = "Insert into tbl_ChatbotPDFConfig values('Capital Gain Loss|Dividend','Trading','Grid','Amount','Dividend Amount',8,'R','',2,'Y',6,'SUPER',GETDATE())";
                            objUtility.ExecuteSQL(strsql);
                        }
                    }
                }
                else
                {
                    return "You are not authorise to use TradeWebAPI Web Service,Please contact SecMark.";
                }

                string strUserID = Strings.Right(req.user_id.Trim(), 10);

                if (!string.IsNullOrWhiteSpace(_configuration["FixedMobile"]))
                {
                    strUserID = Strings.Right(_configuration["FixedMobile"].Trim(), 10);
                }

                string strCmSchedule = objUtility.GetSysParmSt("CMSCHEDULE", "");
                DataTable dtTemp = new DataTable();
                strsql = "select * from tbl_WhatsAppChatbotActions with (nolock) where ActionName='" + req.action.Trim() + "'";
                DataTable dtAction = objUtility.OpenDataTable(strsql);

                if (dtAction.Rows.Count > 0)
                {
                    if (dtAction.Rows[0]["ActionName"].ToString().Trim().ToUpper() == "START_INTERACTION")
                    {
                        Dictionary<string, List<string>> dictAccount = new Dictionary<string, List<string>>();
                        strsql = "select distinct case Product when 'Trading' then 1 else 2 end Ord, Product from tbl_WhatsAppChatbotConfig with (nolock) where IsActive = 'Y' order by Ord";
                        dtTemp = objUtility.OpenDataTable(strsql);

                        strsql = "";
                        foreach (DataRow dr in dtTemp.Rows)
                        {
                            strsql += strsql == "" ? "" : " union all ";
                            if (dr["Product"].ToString().Trim().ToUpper() == "TRADING")
                            {
                                strsql += "select Distinct Rtrim(cm_cd) Code, 'Trading' Product from client_master with (nolock) where cm_schedule = '" + strCmSchedule + "' and cm_mobile='" + strUserID + "' and cm_freezeyn <> 'A'";
                            }
                            else if (dr["Product"].ToString().Trim().ToUpper() == "DEMAT")
                            {
                                string strConn = GetDPTableConnection();
                                string strrDPs = Strings.Trim(objUtility.GetSysParmSt("POADPIDS", ""));
                                //strsql += "select Distinct Rtrim(da_actno) Code, 'Demat' Product from client_master with (nolock), dematact with (nolock) where cm_cd=da_clientcd and da_status = 'A' and sign(patindex('%'+rtrim(da_dpid)+'%','" + strrDPs + "')) = 1 and cm_mobile='" + strUserID + "' and cm_active = '01'";
                                strsql += "select Distinct Rtrim(da_actno) Code, 'Demat' Product from client_master t with (nolock), dematact with (nolock), " + strConn.Trim() + ".Client_master d where t.cm_cd=da_clientcd and d.cm_cd = da_actno and da_status = 'A' and sign(patindex('%'+rtrim(da_dpid)+'%','" + strrDPs + "')) = 1 and t.cm_mobile='" + strUserID + "' and d.cm_active = '01'";
                            }
                        }

                        strsql = "select top 10 * from (" + strsql + ") a order by case Product when 'Trading' then 1 else 2 end";
                        DataTable dt = objUtility.OpenDataTable(strsql);

                        dictAccount = dt.AsEnumerable()
                            .GroupBy(row => row.Field<string>("Product"))
                            .ToDictionary(
                            group => group.Key,
                            group => group.Select(row => row.Field<string>("Code")).ToList());

                        if (dictAccount.Count == 0)
                        {
                            string strMessage = objUtility.fnFireQueryTradeWeb("tbl_WhatsAppChatbotActions", "Message", "ActionName", "start_interaction_none", true);
                            response.user_id = req.user_id;
                            response.message = strMessage;
                            response.action = "next_step";
                            response.data = new WhatsAppResponseData { file_format = "text" };
                        }
                        else
                        {
                            response.user_id = req.user_id;
                            response.message = dtAction.Rows[0]["Message"].ToString().Trim();
                            response.action = "next_step";
                            response.data = new WhatsAppResponseData
                            {
                                input_expected = "text_choice",
                                variable_name = dtAction.Rows[0]["NextAction"].ToString().Trim(),
                                prompt = dtAction.Rows[0]["Prompt"].ToString().Trim(),
                                options = new List<dynamic> { dictAccount },
                                validation_rules = new ValidationRules { required = true }
                            };
                        }
                    }
                    else if (dtAction.Rows[0]["ActionName"].ToString().Trim().ToUpper() == "ACCOUNTTYPE_CODE")
                    {
                        List<string> lstMenus = new List<string>();

                        strsql = "select ReportName from tbl_WhatsAppChatbotConfig with (nolock) where Product='" + strActType.Trim().ToUpper() + "' and IsActive = 'Y' order by SerialNo";
                        DataTable dt = objUtility.OpenDataTable(strsql);

                        foreach (DataRow dr in dt.Rows)
                        {
                            lstMenus.Add(dr["ReportName"].ToString().Trim());
                        }

                        response.user_id = req.user_id.Trim();
                        response.message = dtAction.Rows[0]["Message"].ToString().Trim();
                        response.action = "next_step";
                        response.data = new WhatsAppResponseData
                        {
                            input_expected = "text_choice",
                            variable_name = dtAction.Rows[0]["NextAction"].ToString().Trim(),
                            prompt = dtAction.Rows[0]["Prompt"].ToString().Trim(),
                            options = lstMenus,
                            validation_rules = new ValidationRules { required = true }
                        };
                    }
                    else if (dtAction.Rows[0]["ActionName"].ToString().Trim().ToUpper() == "REPORT_CHOICE")
                    {
                        string strMessage = "";
                        string strNextAction = "";
                        string strPrompt = "";
                        WhatsAppResponseData resData = new WhatsAppResponseData();
                        List<string> lstSelection = new List<string>();

                        strsql = "select * from tbl_WhatsAppChatbotParam with (nolock) where Product='" + strActType.Trim().ToUpper() + "' and ReportName='" + strMenu.Trim().ToUpper() + "'";
                        DataTable dt = objUtility.OpenDataTable(strsql);

                        if (dt.Rows.Count > 0)
                        {
                            strMessage = objUtility.fnFireQueryTradeWeb("tbl_WhatsAppChatbotActions", "Message", "ActionName", "report_choice", true);
                            strMessage = strMessage.Replace("<REPORT_CHOICE>", strMenu.Trim());
                            if (Convert.ToInt16(objUtility.OpenDataTable("select count(*) from tbl_WhatsAppChatbotParam where Product='" + strActType.Trim().ToUpper() + "' and ReportName LIKE '" + strMenu.Trim() + "|%'").Rows[0][0]) > 0)
                            {
                                strNextAction = "subreport_choice";
                                strPrompt = "Please select the Sub Report as per your requirement.";
                            }
                            else
                            {
                                strNextAction = dtAction.Rows[0]["NextAction"].ToString().Trim();
                                strPrompt = dtAction.Rows[0]["Prompt"].ToString().Trim();
                            }
                            foreach (DataRow dr in dt.Rows)
                            {
                                lstSelection.Add(dr["ParamValue"].ToString().Trim());
                            }
                            resData = new WhatsAppResponseData
                            {
                                input_expected = "text_choice",
                                variable_name = strNextAction,
                                prompt = strPrompt,
                                options = lstSelection,
                                validation_rules = new ValidationRules { required = true }
                            };
                            response.user_id = req.user_id.Trim();
                            response.message = strMessage;
                            response.action = "next_step";
                            response.data = resData;
                        }
                        else
                        {
                            response = GetReport(req.user_id.Trim(), strActType, strMenu, req.data.user_input, strCode);
                        }
                    }
                    else if (dtAction.Rows[0]["ActionName"].ToString().Trim().ToUpper() == "SUBREPORT_CHOICE")
                    {
                        string strMessage = "";
                        string strNextAction = "";
                        string strPrompt = "";
                        WhatsAppResponseData resData = new WhatsAppResponseData();
                        List<string> lstSelection = new List<string>();

                        strsql = "select * from tbl_WhatsAppChatbotParam with (nolock) where Product='" + strActType.Trim().ToUpper() + "' and ReportName='" + strMenu.Trim().ToUpper() + "|" + strSubMenu + "'";
                        DataTable dt = objUtility.OpenDataTable(strsql);

                        if (dt.Rows.Count > 0)
                        {
                            strMessage = objUtility.fnFireQueryTradeWeb("tbl_WhatsAppChatbotActions", "Message", "ActionName", "report_choice", true);
                            strMessage = strMessage.Replace("<REPORT_CHOICE>", strMenu.Trim());
                            strNextAction = dtAction.Rows[0]["NextAction"].ToString().Trim();
                            strPrompt = dtAction.Rows[0]["Prompt"].ToString().Trim();
                            foreach (DataRow dr in dt.Rows)
                            {
                                lstSelection.Add(dr["ParamValue"].ToString().Trim());
                            }
                            resData = new WhatsAppResponseData
                            {
                                input_expected = "text_choice",
                                variable_name = strNextAction,
                                prompt = strPrompt,
                                options = lstSelection,
                                validation_rules = new ValidationRules { required = true }
                            };
                            response.user_id = req.user_id.Trim();
                            response.message = strMessage;
                            response.action = "next_step";
                            response.data = resData;
                        }
                        else
                        {
                            response = GetReport(req.user_id.Trim(), strActType, strMenu, req.data.user_input, strCode);
                        }
                    }
                    else if (dtAction.Rows[0]["ActionName"].ToString().Trim().ToUpper() == "REPORT_DURATION")
                    {
                        response = GetReport(req.user_id.Trim(), strActType, strMenu, req.data.user_input, strCode);
                    }
                    //InsertLog(JsonConvert.SerializeObject(req), "");
                    objUtility.UpdateLog(srNo, JsonConvert.SerializeObject(response), "");
                    return response;
                }
                return null;
            }
            catch (Exception ex)
            {
                //InsertLog(JsonConvert.SerializeObject(req), ex.Message);
                objUtility.UpdateLog(srNo, "", ex.Message);
                throw ex;
            }
        }

        public dynamic GetReport(string strUserID, string strActType, string strMenu, string strUserInput, string strCode)
        {
            WhatsAppResponse response = new WhatsAppResponse();
            WhatsAppResponseData data = new WhatsAppResponseData();
            string strReportType = objUtility.fnFireQueryTradeWeb("tbl_WhatsAppChatbotConfig", "FileFormat", "Product = '" + strActType.Trim() + "' and ReportName", strMenu.Trim(), true);
            string strMessage = objUtility.fnFireQueryTradeWeb("tbl_WhatsAppChatbotConfig", "ReportMessage", "Product = '" + strActType.Trim() + "' and ReportName", strMenu.Trim(), true);

            string reportPDF = "";
            string strFileName = "";

            strsql = "select ReportName, ProcedureName, ProcedureParam, FileFormat, PrintAddress, ReportHeading from tbl_WhatsAppChatbotConfig with (nolock) where Product = '" + strActType.Trim().ToUpper() + "' and ReportName = '" + strMenu.Trim().ToUpper() + "'";
            DataTable dtRep = objUtility.OpenDataTable(strsql);

            if (dtRep.Rows.Count > 0)
            {
                string strSPName = dtRep.Rows[0]["ProcedureName"].ToString().Trim();
                string strSelection = dtRep.Rows[0]["ProcedureParam"].ToString().Trim();

                Dictionary<string, string> userValues = ParseUserInput(strUserInput);
                string xmlString = ReplacePlaceholders(strSelection, userValues);

                DataSet dsData = ReportSP(strSPName, xmlString);
                if (dsData.Tables[0].Rows.Count > 0)
                {
                    if (strReportType == "base64")
                    {
                        reportPDF = ReportPDF(strCode.Trim(), strActType.Trim(), dsData);
                        strFileName = strMenu.Trim() + "_" + strCode.Trim() + ".pdf";
                        data = new WhatsAppResponseData { file_format = "base64", file_name = strFileName, file_data = reportPDF };
                    }
                    else if (strReportType == "text")
                    {
                        string strText = "";
                        MatchCollection matches = Regex.Matches(strMessage, @"\<(\d+)\>");
                        foreach (var item in matches)
                        {
                            int intIndex = Convert.ToInt16(item.ToString().Replace("<", "").Replace(">", "")) - 1;
                            if (dsData.Tables[0].Columns[intIndex].DataType == typeof(decimal))
                            {
                                strText = Convert.ToDecimal(dsData.Tables[0].Rows[0][intIndex]).ToString("0.00").Trim();
                            }
                            else
                            {
                                strText = dsData.Tables[0].Rows[0][intIndex].ToString().Trim();
                            }
                            strMessage = strMessage.Replace(item.ToString(), strText);
                        }
                        data = new WhatsAppResponseData { file_format = "text" };
                    }
                    else if (strReportType == "RDL")
                    {
                        string strReportName = dtRep.Rows[0]["ReportName"].ToString().Trim();
                        reportPDF = GetRDLPDF(strReportName, dsData);
                        strFileName = strMenu.Trim() + "_" + strCode.Trim() + ".pdf";
                        data = new WhatsAppResponseData { file_format = "base64", file_name = strFileName, file_data = reportPDF };
                    }
                    else
                    {
                        reportPDF = dsData.Tables[0].Rows[0][0].ToString();
                        strFileName = dsData.Tables[0].Rows[0][1].ToString();
                        data = new WhatsAppResponseData { file_format = "base64", file_name = strFileName, file_data = reportPDF };
                    }
                }
                else
                {
                    strMessage = "No Data Found";
                    data = new WhatsAppResponseData { file_format = "text" };
                }

                response.user_id = strUserID;
                response.message = strMessage;
                response.action = "next_step";
                response.data = data;
            }

            return response;
        }

        static Dictionary<string, string> ParseUserInput(string userInput)
        {
            Dictionary<string, string> values = new Dictionary<string, string>();

            // Split the user input string into key-value pairs
            string[] pairs = userInput.Split(',');
            foreach (string pair in pairs)
            {
                string[] keyValue = pair.Split(':');
                if (keyValue.Length == 2)
                {
                    string key = keyValue[0].Trim();
                    string value = keyValue[1].Trim();
                    values[key] = value;
                }
            }

            return values;
        }

        static string ReplacePlaceholders(string xmlTemplate, Dictionary<string, string> userValues)
        {
            // Replace placeholders with actual values
            foreach (var kvp in userValues)
            {
                xmlTemplate = xmlTemplate.Replace($"{{{kvp.Key}}}", kvp.Value);
            }

            return xmlTemplate;
        }

        public dynamic ValidateReport(string AccountType, string Code, string Menu, string ReportSelection)
        {
            string strError = "";

            if (string.IsNullOrWhiteSpace(AccountType))
            {
                strError += "Account Type is Blank" + Environment.NewLine;
            }

            if (string.IsNullOrWhiteSpace(Menu))
            {
                strError += "Menu is Blank" + Environment.NewLine;
            }

            if (!string.IsNullOrWhiteSpace(AccountType) && AccountType.Trim().ToUpper() == "TRADING")
            {
                if (!string.IsNullOrWhiteSpace(Menu) && Menu.Trim().ToUpper() == "LEDGER")
                {
                    if (string.IsNullOrWhiteSpace(ReportSelection))
                    {
                        strError += "Report Selection is Blank" + Environment.NewLine;
                    }
                }
            }

            return strError;
        }

        public dynamic ReportSP(string SPName, string ReportSelection)
        {
            try
            {
                string connetionString = objUtility.GetConnectionStr();
                using (SqlConnection connection = new SqlConnection(connetionString))
                {
                    using (SqlCommand command = new SqlCommand(SPName, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add(new SqlParameter("@vcXML", SqlDbType.VarChar) { Value = ReportSelection });
                        command.CommandTimeout = 0;
                        connection.Open();
                        DataSet resultSet = new DataSet();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            adapter.Fill(resultSet);
                        }
                        return resultSet;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic ReportSPEsign(string SPName, string ReportSelection)
        {
            try
            {
                string connetionString = _configuration.GetConnectionString("EsignConnection");
                using (SqlConnection connection = new SqlConnection(connetionString))
                {
                    using (SqlCommand command = new SqlCommand(SPName, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add(new SqlParameter("@vcXML", SqlDbType.VarChar) { Value = ReportSelection });
                        command.CommandTimeout = 0;
                        connection.Open();
                        DataSet resultSet = new DataSet();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            adapter.Fill(resultSet);
                        }
                        return resultSet;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic ReportPDF(string strClient, string strAccountType, DataSet dsData)
        {
            try
            {
                DataTable dataTable = new DataTable();
                DataTable dtFormat = new DataTable();
                DataTable dtDetails = new DataTable();
                DataTable dtFormat1 = new DataTable();
                DataTable dataTable1 = new DataTable();
                if (dsData.Tables.Count == 4)
                {
                    dataTable = dsData.Tables[0];
                    dataTable1 = dsData.Tables[1];
                    dtFormat = dsData.Tables[2].AsEnumerable().Where(row => row.Field<string>("ColumnType") == "Grid").CopyToDataTable();
                    dtFormat1 = dsData.Tables[2].AsEnumerable().Where(row => row.Field<string>("ColumnType") == "Grid1").CopyToDataTable();
                    dtDetails = dsData.Tables[3];
                }
                else
                {
                    dataTable = dsData.Tables[0];
                    dtFormat = dsData.Tables[1];
                    dtDetails = dsData.Tables[2];
                }
                string strPass = dtDetails.Rows[0]["PassWord"].ToString().Trim();
                string strReportHeading = "";
                if (dtDetails.Rows[0]["ReportHeading"].ToString().Trim().Contains("|"))
                {
                    strReportHeading = dtDetails.Rows[0]["ReportHeading"].ToString().Split("|")[0].Trim();
                }
                bool blnPrintAdd = dtDetails.Rows[0]["PrintAddress"].ToString().Trim() == "Y";
                string strOrientation = dtDetails.Rows[0]["Orientation"].ToString().Trim();

                int[] width = new int[dtFormat.Rows.Count];

                for (int i = 0; i < dtFormat.Rows.Count; i++)
                {
                    width[i] = Convert.ToInt32(dtFormat.Rows[i]["ColumnWidth"]);
                }
                byte[] buff = null;
                DataTable dtLogo = new DataTable();
                DataTable dtComp = objUtility.OpenDataTable("select * from Entity_master with (nolock) where em_cd=(select min(em_cd) from Entity_master with (nolock))");
                string CName = dtComp.Rows[0]["em_name"].ToString().Trim();
                if (strAccountType == "Trading")
                {
                    strsql = "Select img_code, img_logo logo from Images with (nolock) where img_desc = 'Company Logo' and img_logo is not null order by img_code";
                    dtLogo = objUtility.OpenDataTable(strsql);
                }
                else
                {
                    string strDPCon = GetDPConnection();
                    if (strDPCon != "")
                    {
                        strsql = "Select si_image logo from Slip_Image with (nolock) where si_trx_type='CMP'";
                        dtLogo = objUtility.OpenDataTable(strsql, new SqlConnection(strDPCon));
                    }
                }
                if (dtLogo.Rows.Count > 0)
                {
                    buff = (byte[])dtLogo.Rows[0]["logo"];
                }

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    Document document = new Document();
                    PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
                    writer.SetEncryption(
                        Encoding.ASCII.GetBytes(strPass),
                        Encoding.ASCII.GetBytes(strPass),
                        PdfWriter.ALLOW_PRINTING,
                        PdfWriter.ENCRYPTION_AES_256
                    );
                    //document.SetPageSize(PageSize.A4.Rotate());
                    Image imgLogo = null;
                    if (buff != null)
                    {
                        imgLogo = Image.GetInstance(buff);
                        if (strOrientation == "L")
                        {
                            imgLogo.SetAbsolutePosition(25, 530);
                        }
                        else
                        {
                            imgLogo.SetAbsolutePosition(25, 765);
                        }
                        imgLogo.ScaleAbsolute(300, 600);
                        imgLogo.ScaleToFit(700f, 40f);
                    }
                    if (strOrientation == "L")
                    {
                        document.SetPageSize(PageSize.LEGAL.Rotate());
                    }
                    document.Open();
                    BaseFont baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                    Font headerFont = new Font(baseFont, 10);
                    Font infoFont = new Font(baseFont, 8);
                    Paragraph companyName = new Paragraph(CName, headerFont)
                    {
                        Alignment = Element.ALIGN_CENTER
                    };
                    if (imgLogo != null)
                    {
                        document.Add(imgLogo);
                    }
                    document.Add(companyName);
                    if (blnPrintAdd)
                    {
                        document.Add(new Paragraph(dtComp.Rows[0]["em_add1"].ToString().Trim(), headerFont) { Alignment = Element.ALIGN_CENTER });
                        document.Add(new Paragraph(dtComp.Rows[0]["em_add2"].ToString().Trim(), headerFont) { Alignment = Element.ALIGN_CENTER });
                        document.Add(new Paragraph(dtComp.Rows[0]["em_add3"].ToString().Trim(), headerFont) { Alignment = Element.ALIGN_CENTER });
                    }

                    if (strReportHeading.Trim() != "")
                    {
                        document.Add(new Paragraph(strReportHeading, headerFont) { Alignment = Element.ALIGN_CENTER });
                    }
                    string strClientName = objUtility.fnFireQueryTradeWeb("client_master", "cm_name", "cm_cd", strClient, true);
                    if (strAccountType == "Trading")
                    {
                        strClientName = objUtility.fnFireQueryTradeWeb("client_master", "cm_name", "cm_cd", strClient, true);
                    }
                    else
                    {
                        strClientName = fnFireQueryDP("client_master", "cm_name", "cm_cd", strClient, true);
                    }

                    document.Add(new Paragraph(strClientName + " [" + strClient + "]", headerFont) { Alignment = Element.ALIGN_CENTER });
                    document.Add(new Paragraph("\n"));
                    //PdfPTable pdfTable = new PdfPTable(dtFormat.Rows.Count);
                    //int[] columnWidths = width;
                    //pdfTable.SetWidths(columnWidths);
                    //pdfTable.WidthPercentage = 100;
                    //pdfTable.HorizontalAlignment = Element.ALIGN_CENTER;

                    //PdfPCell headerCell;
                    //PdfPCell dataCell;
                    //string strFormat = "";
                    //string strAlign = "";
                    //string colName = "";
                    //int intFormat = 0;
                    //bool blnTotal = false;
                    //decimal[] decimalArray = new decimal[dtFormat.Rows.Count];
                    //bool[] blnIsDecimal = new bool[dtFormat.Rows.Count];
                    //string strColFormat = "";

                    //int intCount = 0;
                    //foreach (DataRow dr in dtFormat.Rows)
                    //{
                    //    strAlign = dr["ColumnAlignement"].ToString().Trim();
                    //    headerCell = new PdfPCell(new Phrase(dr["ColumnHeading"].ToString().Trim(), headerFont))
                    //    {
                    //        HorizontalAlignment = Element.ALIGN_CENTER,
                    //        BackgroundColor = BaseColor.LIGHT_GRAY
                    //    };
                    //    headerCell.HorizontalAlignment = strAlign == "R" ? Element.ALIGN_RIGHT : strAlign == "L" ? Element.ALIGN_LEFT : Element.ALIGN_CENTER;
                    //    pdfTable.AddCell(headerCell);
                    //    if (dataTable.Columns[dr["ColumnName"].ToString().Trim()].DataType == typeof(decimal))
                    //    {
                    //        blnIsDecimal[intCount] = true;
                    //    }
                    //    else
                    //    {
                    //        blnIsDecimal[intCount] = false;
                    //    }
                    //    intCount++;
                    //}

                    //for (int i = 0; i < dataTable.Rows.Count; i++)
                    //{
                    //    for (int j = 0; j < dtFormat.Rows.Count; j++)
                    //    {
                    //        strAlign = dtFormat.Rows[j]["ColumnAlignement"].ToString().Trim();
                    //        colName = dtFormat.Rows[j]["ColumnName"].ToString().Trim();
                    //        intFormat = Convert.ToInt16(dtFormat.Rows[j]["DecimalPlace"].ToString().Trim());
                    //        strColFormat = dtFormat.Rows[j]["ColumnFormat"].ToString().Trim();

                    //        if (blnIsDecimal[j])
                    //        {
                    //            if (intFormat > 0)
                    //            {
                    //                strFormat = $"0.{new string('0', intFormat)}";
                    //                dataCell = new PdfPCell(new Phrase(Convert.ToDecimal(dataTable.Rows[i][colName]).ToString(strFormat).Trim(), infoFont));
                    //            }
                    //            else
                    //            {
                    //                dataCell = new PdfPCell(new Phrase(Convert.ToInt32(dataTable.Rows[i][colName]).ToString().Trim(), infoFont));
                    //            }
                    //        }
                    //        else
                    //        {
                    //            if (strColFormat != "")
                    //            {
                    //                dataCell = new PdfPCell(new Phrase(DateTime.ParseExact(dataTable.Rows[i][colName].ToString().Trim(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString(strColFormat), infoFont));
                    //            }
                    //            else
                    //            {
                    //                dataCell = new PdfPCell(new Phrase(dataTable.Rows[i][colName].ToString().Trim(), infoFont));
                    //            }
                    //        }

                    //        dataCell.HorizontalAlignment = strAlign == "R" ? Element.ALIGN_RIGHT : strAlign == "L" ? Element.ALIGN_LEFT : Element.ALIGN_CENTER;
                    //        pdfTable.AddCell(dataCell);

                    //        if (dtFormat.Rows[j]["ColumnTotal"].ToString().Trim() == "Y")
                    //        {
                    //            blnTotal = true;
                    //            decimalArray[j] = decimalArray[j] + Convert.ToDecimal(dataTable.Rows[i][colName]);
                    //        }
                    //    }
                    //}

                    //if (blnTotal)
                    //{
                    //    for (int j = 0; j < dtFormat.Rows.Count; j++)
                    //    {
                    //        if (dtFormat.Rows[j]["ColumnTotal"].ToString().Trim() == "Y")
                    //        {
                    //            strAlign = dtFormat.Rows[j]["ColumnAlignement"].ToString().Trim();
                    //            colName = dtFormat.Rows[j]["ColumnName"].ToString().Trim();
                    //            intFormat = Convert.ToInt16(dtFormat.Rows[j]["DecimalPlace"].ToString().Trim());
                    //            if (blnIsDecimal[j])
                    //            {
                    //                if (intFormat > 0)
                    //                {
                    //                    strFormat = $"0.{new string('0', intFormat)}";
                    //                    dataCell = new PdfPCell(new Phrase(decimalArray[j].ToString(strFormat).Trim(), infoFont));
                    //                }
                    //                else
                    //                {
                    //                    dataCell = new PdfPCell(new Phrase(Convert.ToInt32(decimalArray[j]).ToString().Trim(), infoFont));
                    //                }
                    //            }
                    //            else
                    //            {
                    //                dataCell = new PdfPCell(new Phrase(decimalArray[j].ToString().Trim(), infoFont));
                    //            }

                    //            dataCell.HorizontalAlignment = strAlign == "R" ? Element.ALIGN_RIGHT : strAlign == "L" ? Element.ALIGN_LEFT : Element.ALIGN_CENTER;
                    //            dataCell.BackgroundColor = BaseColor.LIGHT_GRAY;
                    //        }
                    //        else
                    //        {
                    //            dataCell = new PdfPCell(new Phrase(""))
                    //            {
                    //                BackgroundColor = BaseColor.LIGHT_GRAY
                    //            };
                    //        }
                    //        pdfTable.AddCell(dataCell);
                    //    }
                    //}

                    PdfPTable pdfTable = createPDFTable(dtFormat, dataTable);
                    document.Add(pdfTable);

                    if (dtFormat1.Rows.Count > 0 && dataTable1.Rows.Count > 0)
                    {
                        document.Add(new Paragraph("\n"));
                        //string strReportHeading1 = dtDetails.Rows[0]["ReportHeading"].ToString().Split("|")[1].Trim();
                        if (dtDetails.Rows[0]["ReportHeading"].ToString().Trim().Contains("|"))
                        {
                            if (dtDetails.Rows[0]["ReportHeading"].ToString().Split("|")[1].Trim() != "")
                            {
                                document.Add(new Paragraph(dtDetails.Rows[0]["ReportHeading"].ToString().Split("|")[1].Trim(), headerFont) { Alignment = Element.ALIGN_CENTER });
                            }
                            document.Add(new Paragraph("\n"));
                        }
                        PdfPTable pdfTable2 = createPDFTable(dtFormat1, dataTable1);
                        document.Add(pdfTable2);
                    }

                    writer.PageEvent = new CustomPageEvent();
                    document.Close();
                    byte[] pdfBytes = memoryStream.ToArray();
                    return Convert.ToBase64String(pdfBytes);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic createPDFTable(DataTable dtFormat, DataTable dataTable)
        {
            PdfPTable pdfTable = new PdfPTable(dtFormat.Rows.Count);

            BaseFont baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
            Font headerFont = new Font(baseFont, 10);
            Font infoFont = new Font(baseFont, 8);
            int[] width = new int[dtFormat.Rows.Count];

            for (int i = 0; i < dtFormat.Rows.Count; i++)
            {
                width[i] = Convert.ToInt32(dtFormat.Rows[i]["ColumnWidth"]);
            }

            int[] columnWidths = width;
            pdfTable.SetWidths(columnWidths);
            pdfTable.WidthPercentage = 100;
            pdfTable.HorizontalAlignment = Element.ALIGN_CENTER;

            PdfPCell headerCell;
            PdfPCell dataCell;
            string strFormat = "";
            string strAlign = "";
            string colName = "";
            int intFormat = 0;
            bool blnTotal = false;
            decimal[] decimalArray = new decimal[dtFormat.Rows.Count];
            bool[] blnIsDecimal = new bool[dtFormat.Rows.Count];
            string strColFormat = "";

            int intCount = 0;
            foreach (DataRow dr in dtFormat.Rows)
            {
                strAlign = dr["ColumnAlignement"].ToString().Trim();
                headerCell = new PdfPCell(new Phrase(dr["ColumnHeading"].ToString().Trim(), headerFont))
                {
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    BackgroundColor = BaseColor.LIGHT_GRAY
                };
                headerCell.HorizontalAlignment = strAlign == "R" ? Element.ALIGN_RIGHT : strAlign == "L" ? Element.ALIGN_LEFT : Element.ALIGN_CENTER;
                pdfTable.AddCell(headerCell);
                if (dataTable.Columns[dr["ColumnName"].ToString().Trim()].DataType == typeof(decimal))
                {
                    blnIsDecimal[intCount] = true;
                }
                else
                {
                    blnIsDecimal[intCount] = false;
                }
                intCount++;
            }

            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                for (int j = 0; j < dtFormat.Rows.Count; j++)
                {
                    strAlign = dtFormat.Rows[j]["ColumnAlignement"].ToString().Trim();
                    colName = dtFormat.Rows[j]["ColumnName"].ToString().Trim();
                    intFormat = Convert.ToInt16(dtFormat.Rows[j]["DecimalPlace"].ToString().Trim());
                    strColFormat = dtFormat.Rows[j]["ColumnFormat"].ToString().Trim();

                    if (blnIsDecimal[j])
                    {
                        if (intFormat > 0)
                        {
                            strFormat = $"0.{new string('0', intFormat)}";
                            dataCell = new PdfPCell(new Phrase(Convert.ToDecimal(dataTable.Rows[i][colName]).ToString(strFormat).Trim(), infoFont));
                        }
                        else
                        {
                            dataCell = new PdfPCell(new Phrase(Convert.ToInt32(dataTable.Rows[i][colName]).ToString().Trim(), infoFont));
                        }
                    }
                    else
                    {
                        if (strColFormat != "")
                        {
                            dataCell = new PdfPCell(new Phrase(DateTime.ParseExact(dataTable.Rows[i][colName].ToString().Trim(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString(strColFormat), infoFont));
                        }
                        else
                        {
                            dataCell = new PdfPCell(new Phrase(dataTable.Rows[i][colName].ToString().Trim(), infoFont));
                        }
                    }

                    dataCell.HorizontalAlignment = strAlign == "R" ? Element.ALIGN_RIGHT : strAlign == "L" ? Element.ALIGN_LEFT : Element.ALIGN_CENTER;
                    pdfTable.AddCell(dataCell);

                    if (dtFormat.Rows[j]["ColumnTotal"].ToString().Trim() == "Y")
                    {
                        blnTotal = true;
                        decimalArray[j] = decimalArray[j] + Convert.ToDecimal(dataTable.Rows[i][colName]);
                    }
                }
            }

            if (blnTotal)
            {
                for (int j = 0; j < dtFormat.Rows.Count; j++)
                {
                    if (dtFormat.Rows[j]["ColumnTotal"].ToString().Trim() == "Y")
                    {
                        strAlign = dtFormat.Rows[j]["ColumnAlignement"].ToString().Trim();
                        colName = dtFormat.Rows[j]["ColumnName"].ToString().Trim();
                        intFormat = Convert.ToInt16(dtFormat.Rows[j]["DecimalPlace"].ToString().Trim());
                        if (blnIsDecimal[j])
                        {
                            if (intFormat > 0)
                            {
                                strFormat = $"0.{new string('0', intFormat)}";
                                dataCell = new PdfPCell(new Phrase(decimalArray[j].ToString(strFormat).Trim(), infoFont));
                            }
                            else
                            {
                                dataCell = new PdfPCell(new Phrase(Convert.ToInt32(decimalArray[j]).ToString().Trim(), infoFont));
                            }
                        }
                        else
                        {
                            dataCell = new PdfPCell(new Phrase(decimalArray[j].ToString().Trim(), infoFont));
                        }

                        dataCell.HorizontalAlignment = strAlign == "R" ? Element.ALIGN_RIGHT : strAlign == "L" ? Element.ALIGN_LEFT : Element.ALIGN_CENTER;
                        dataCell.BackgroundColor = BaseColor.LIGHT_GRAY;
                    }
                    else
                    {
                        dataCell = new PdfPCell(new Phrase(""))
                        {
                            BackgroundColor = BaseColor.LIGHT_GRAY
                        };
                    }
                    pdfTable.AddCell(dataCell);
                }
            }

            return pdfTable;
        }

        public dynamic GetRDLPDF(string ReportName, DataSet dsData)
        {
            //string reportPath = Path.Combine(ReportName.Trim() + ".rdl");
            //string reportPath = Path.Combine(this._environment.WebRootPath, "RDL", ReportName.Trim() + ".rdl");
            string reportPath = Path.Combine(Directory.GetCurrentDirectory(), "RDL", ReportName.Trim() + ".rdl");

            LocalReport localReport = new LocalReport
            {
                ReportPath = reportPath,
                EnableExternalImages = true
            };

            if (dsData != null && dsData.Tables.Count >= 4)
            {
                ReportDataSource rd = new ReportDataSource("ClientData", dsData.Tables[0]);
                ReportDataSource rd1 = new ReportDataSource("NomineeData", dsData.Tables[1]);
                ReportDataSource rd2 = new ReportDataSource("POAData", dsData.Tables[2]);
                ReportDataSource rd3 = new ReportDataSource("HolderData", dsData.Tables[3]);

                localReport.DataSources.Add(rd);
                localReport.DataSources.Add(rd1);
                localReport.DataSources.Add(rd2);
                localReport.DataSources.Add(rd3);
            }
            else
            {
                throw new Exception("Invalid dataset for RDL report generation.");
            }

            byte[] pdfBytes1 = localReport.Render("PDF", null, out string mimeType, out string encoding, out string fileNameExtension, out string[] streams, out Warning[] warnings);
            string strPDFBase64 = Convert.ToBase64String(pdfBytes1);

            //var fileName = $"Report_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            //var filePath = Path.Combine("PDF", fileName);

            //// Write the bytes to the file
            //File.WriteAllBytes(filePath, pdfBytes1);

            return strPDFBase64;
        }

        class CustomPageEvent : PdfPageEventHelper
        {
            int intPageNo = 0;
            private float headerHeight = 20;
            public override void OnEndPage(PdfWriter writer, Document document)
            {
                intPageNo++;
                Rectangle pageSize = writer.PageSize;

                BaseFont baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                Font infoFont = new Font(baseFont, 8);

                // Set the table width to 100% of the page width
                float tableWidth = pageSize.Width - document.LeftMargin - document.RightMargin;

                // Calculate the Y-coordinate for the bottom center
                float yPosition = document.BottomMargin;

                // Add content to the footer (you can customize this part based on your needs)
                PdfPTable table = new PdfPTable(2);
                table.TotalWidth = tableWidth;
                table.AddCell(new PdfPCell(new Phrase("Print Date : " + DateAndTime.Today.ToString("dd-MM-yyyy"), infoFont)) { HorizontalAlignment = Element.ALIGN_LEFT, Border = Rectangle.NO_BORDER });
                table.AddCell(new PdfPCell(new Phrase("TradeWebAPI [" + intPageNo + "]", infoFont)) { HorizontalAlignment = Element.ALIGN_RIGHT, Border = Rectangle.NO_BORDER });

                // Position the table at the desired location
                table.WriteSelectedRows(0, -1, (pageSize.Width - table.TotalWidth) / 2, yPosition, writer.DirectContent);

                PdfContentByte content = writer.DirectContent;
                content.SetLineWidth(0.5f); // Set line width as needed
                content.MoveTo(document.LeftMargin, document.BottomMargin); // Adjusted the Y-coordinate
                content.LineTo(pageSize.Width - document.RightMargin, document.BottomMargin); // Adjusted the Y-coordinate
                content.Stroke();
            }
        }

        public dynamic fnFireQueryDP(string strTable, string strSelect, string strParam1, string strParam2, bool strInt)
        {
            string strReturn = string.Empty;
            string strsql = string.Empty;

            if ((strParam1.Length == 0) || (strParam2.Length == 0))
            { strReturn = ""; }

            if (strInt == true)
                strsql = "select " + strSelect + " from " + strTable + " where " + strParam1 + " = '" + strParam2.Trim() + "'";
            else
                strsql = "select " + strSelect + " from " + strTable + " where " + strParam1 + " = " + strParam2.Trim();

            DataTable ObjDataSet = new DataTable();
            DataTable dt = objUtility.OpenDataTable("select * from other_products with (nolock) where op_product in ('Cross', 'Estro') and op_status='A'");

            if (dt.Rows.Count > 0)
            {
                string strConnection = "server=" + dt.Rows[0]["op_server"].ToString().Trim() + ";Database=" + dt.Rows[0]["op_database"].ToString().Trim() + ";Uid=" + dt.Rows[0]["op_user"].ToString().Trim() + ";Pwd=" + dt.Rows[0]["op_pwd"].ToString().Trim() + ";Max Pool Size=200;Connect Timeout=20000;pooling='true';";
                ObjDataSet = objUtility.OpenDataTable(strsql, new SqlConnection { ConnectionString = strConnection });

                if (ObjDataSet.Rows.Count < 1)
                {
                    if (strInt == true)
                    { strReturn = ""; }
                    else
                    { strReturn = "0"; }
                }
                else
                {
                    strReturn = ObjDataSet.Rows[0][0].ToString().Trim();
                }
                return strReturn;
            }
            else
            {
                return "";
            }
        }

        public dynamic GetDPConnection()
        {
            string strConnection = "";
            DataTable dt = objUtility.OpenDataTable("select * from other_products with (nolock) where op_product in ('Cross', 'Estro') and op_status='A'");
            if (dt.Rows.Count > 0)
            {
                strConnection = "server=" + dt.Rows[0]["op_server"].ToString().Trim() + ";Database=" + dt.Rows[0]["op_database"].ToString().Trim() + ";Uid=" + dt.Rows[0]["op_user"].ToString().Trim() + ";Pwd=" + dt.Rows[0]["op_pwd"].ToString().Trim() + ";Max Pool Size=200;Connect Timeout=20000;pooling='true';";
            }
            return strConnection;
        }

        public dynamic GetDPTableConnection()
        {
            string strConnection = "";
            DataTable dt = objUtility.OpenDataTable("select * from other_products with (nolock) where op_product in ('Cross', 'Estro') and op_status='A'");
            if (dt.Rows.Count > 0)
            {
                strConnection = "[" + dt.Rows[0]["op_server"].ToString().Trim() + "].[" + dt.Rows[0]["op_database"].ToString().Trim() + "].[" + dt.Rows[0]["OP_Owner"].ToString().Trim() + "]";
            }
            return strConnection;
        }

        public void InsertLog(string strRequest, string strException)
        {
            strsql = "insert into tbl_GenericAPIDebugLog(ParentSerialNo, RequestSource, RequestUniqueID, RequestString, ResponseString, ExceptionMessage, UpdateBy, UpdateTimeStamp) values('', '', '', '" + strRequest + "', '', '" + strException + "', '', GETDATE())";
            objUtility.ExecuteSQL(strsql);
        }
    }
}
