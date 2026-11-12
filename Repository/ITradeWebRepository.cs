using System.Data;
using System.Threading.Tasks;
using TradeWeb.API.Models;

namespace TradeWeb.API.Repository
{
    public interface ITradeWebRepository
    {
        public dynamic UserDetails(string userId, string password, bool keyBased, string product, string role);
        public dynamic BranchUserDetails(string userId, string password, string role);
        public dynamic Login_API_Authorize(string key, string product, string loginAs, string feature);
        public dynamic Get_CompanyName();
        public dynamic AddLog_Session(string userId, string strWebYN, string loginAs, string type = "");
        public dynamic CommonAPIrepoCall(string strModuleName, string strFunctionName, string strXML);
        public dynamic PostData(string strRequest);
        public dynamic CompileTypst(string ReportName, string ReportDisplayName, DataSet ds, string pdfType);
        public dynamic CompileTypstSeperateFiles(string ReportName, string ReportDisplayName, DataSet ds, string pdfType);
        public dynamic DownloadMultiplePdfZip(string ReportName, string ReportDisplayName, DataSet ds, string pdfType);
        public Task<dynamic> Login(string UserId, string Password, string EPassword, string Key, string LoginAs, string Product, string ICPV, string Feature, string isSSO, string IP = "");
        public dynamic GetDebugFlag(string ModuleName, string FunctionName);
        public dynamic Verify2FA(string userId, string password, string loginAs, bool branch, string loginRole, string role, string strIdentity, int expTime, int refreshExpTime, DataTable userData);
        public (string tokenString, string refreshToken) RefreshToken(string UserCode, string role, string guidVal, int expTime, string strUserAccess, string strLoginAs, int refExpTime, bool branch);
        public Task<dynamic> LoginSSO_Req(string xmlStr);
        public dynamic ImportLargeFileRepo(object request);
        public dynamic CompileExcelExport(string ReportName, string ReportDisplayName, DataSet dts);
        public dynamic VerifyBiometric(string userId, string loginAs, string role, string strModuleName);
        public dynamic CompileTypstSendMail(string ReportName, string ReportDisplayName, DataSet ds, string pdfType);
        public Task<DataSet> GenerateQuestPDF(string ReportName, string ReportDisplayName, DataSet ds, string pdfType, string userId);
        public Task<DataSet> GenerateQuestPDFusingDynamicJson(string ReportName, DataTable dtHeader, string userId, string clientCode = "");
        public dynamic GetCompanyBranding();

    }

}
