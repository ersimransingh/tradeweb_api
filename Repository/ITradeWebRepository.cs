using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using TradeWeb.API.Models;

namespace TradeWeb.API.Repository
{
    public interface ITradeWebRepository
    {
        public dynamic TestAPIRepo();
        public dynamic UserDetails(string userId, string password, bool keyBased, string product, string role);
        public dynamic BranchUserDetails(string userId, string password, string role);

        public dynamic Login_validate_USER(string userId);

        public dynamic Login_Password_GenerateOTP(string userId, string mode);

        public dynamic Login_Password_Update(string userId, string OTP, string oldPassword, string newPassword);
        public dynamic ChangePassword(string clientCd, string compCode, string OldPwd, string NewPwd);
        public dynamic UserRole(string userId);
        public dynamic XtreamLivePOS(string userId);
        public dynamic AdditionalMenu(string userId);
        public dynamic Product();
        public dynamic Login_validate_Password(string userId, string password);

        public dynamic Login_API_Authorize(string key, string product, string loginAs, string feature);

        public dynamic Get_CompanyName();
        public dynamic Get_CompanyDetail();
        public dynamic Login_API_Encrypt(string compName);

        public dynamic Login_GetPassword(string userId, string otpType);

        public dynamic GetUserDetais(string userId);

        public dynamic Transaction_Summary(string userId, string tradeType, string type, string FromDate, string ToDate);

        public dynamic Transaction_Accounts(string userId, string type, string fromDate, string toDate);

        public dynamic Transaction_AGTS(string userId, string seg, string fromDate, string toDate);

        public DataTable Ledger_Summary(string userId, string type, string fromdate, string toDate);

        public dynamic Ledger_Year();

        public dynamic Ledger_Type();

        public dynamic Ledger_Detail(string userId, LedgerDetailsModel model, string fromDate, string toDate);

        public dynamic OutStandingPosition(string userId, string AsOnDt, List<string> ExchSeg = null, bool blnBranch = false);

        public dynamic OutStandingPosition_Detail(string userId, string seriesid, string CESCd);

        public dynamic ProfitLoss_Segment();
        public dynamic ProfitLoss_Exchange(string segment);
        public dynamic ProfitLoss_CashSummary(string userId, string fromDate, string toDate, int stockValuation = 0);

        public dynamic ProfitLoss_CashDetail(string userId, string fromDate, string toDate, string scripcd);

        public dynamic ProfitLoss_FO_Summary(string userId, string exchange, string segment, string fromDate, string toDate, bool includeBfOptions, int bfOptionPL);

        public dynamic ProfitLoss_Commodity_Summary(string userId, string exchange, string fromDate, string toDate);

        public dynamic Holding_Broker_Current(string userid);

        public dynamic Holding_Broker_Ason(string userid, string AsOnDt);

        public dynamic Holding_MyDematAct_List(string userid);

        public dynamic Holding_MyDemat_Current(string userid, string dematActNo, string strtable);
        public dynamic Holding_MyDemat_Current_Graph(string userid, string dematActNo, string strtable, int graphDays);

        public dynamic Holding_MyDematAct_HoldingDates_Execute();

        public dynamic ISIN_Rates_Graph(string ISIN, string fromDate, string strtabletoDate);
        public dynamic Get_Holding_DpDetail(string BOID);
        public dynamic Request_Post_UnPledge_UnRepledge(string userId, string unPledge, string dmScripcd, string txtReqQty);
        public dynamic Bills_exchSeg();
        public dynamic Bills_cash_settTypes_list(string syExchange);
        public dynamic Bill_data(string userId, string exchSeg, string settType, string dt);

        public dynamic CommonGetSysParmStHandler(string param1, string param2);

        public dynamic Confirmation(string userId, int type, string dt);

        public dynamic Transaction_Detail(string userId, string exch, string seg, int type, string fromdate, string todate, string scripcode);

        public dynamic Margin(string userId, string date);

        public dynamic Request_Get_PledgeForMargin(string userId, string dematActNo);

        public dynamic Request_Post_PledgeForMargin(string UserId, string CmbDPID_Value, string lblScripcd, string txtQty);

        public dynamic Get_Family_List_(string cm_cd);

        public dynamic Get_Buttons_Data(List<ButtonCombinedInputModel> model);
        public dynamic BindGrid(List<ButtonCombinedInputModel> model);

        public dynamic Get_Transaction_Btn_Data(string BtnClick, string SelectedCLCode, string SelectedValue, string FromDate, string ToDate);

        public dynamic Get_Transaction_Btn_RPJ_Detailed_Data(string Client, string Type, string FromDate, string ToDate);



        public dynamic CapitalGainLoss_Dividend_Process(string ClientWhere, string FromDate, string ToDate);

        public dynamic CapitalGainLoss_ActualPLSummary_Process(string ClientWhere, string FromDate, string ToDate, bool ignore112A, bool isDetails);

        public dynamic CapitalGainLoss_TradeListingSummary_Process(string ClientWhere, string FromDate, string ToDate);

        public dynamic CapitalGainLoss_ActualPLDetail_Process(string ClientWhere, string FromDate, string ToDate, bool ignore112A, string scripCd);

        public dynamic CapitalGainLoss_TradeListingDetail_Process(string ClientWhere, string fromDate, string toDate, string scripcode);

        public dynamic GetINVPLTradeListingDelete(string userId, string srNo);

        public dynamic CapitalGainLoss_TradeInsert_Process(string userId, string date, string settelment, string bsFlag, string tradeType, double quantity, double netRate, double serviceTax, double STT, double otherCharge1, double otherCharge2, string sccdPostBack);

        public dynamic CapitalGainLoss_NationalDetail_Process(string ClientWhere, string FromDate, bool ignore112A, string scripCd);

        public dynamic CapitalGainLoss_NationalSummary_Process(string ClientWhere, string strDate, bool ignore112A);


        //public dynamic GetINVPLDivListing(string userId, string FromDate, string ToDate);

        //public dynamic GetINVPLGainLoss(string userId, string FromDate, string ToDate, Boolean chkjobing, Boolean chkdelivery, Boolean chkIgnoreSection, string TrxType);

        //public dynamic GetINVPLTradeListing(string userId, string FromDate, string ToDate);

        //public dynamic GetINVPLGainLossDetails(string userId, string FromDate, string ToDate, string reportFor, string ignore112A, string scripCd);

        //public dynamic GeTINVPLTradeListingDetails(string userId, string fromDate, string toDate, string sccdPostBack);

        //public dynamic GetINVPLTradeListingDelete(string userId, string srNo);

        //public dynamic GetINVPLTradeListingSave(string userId, string date, string settelment, string bsFlag, string tradeType, double quantity, double netRate, double serviceTax, double STT, double otherCharge1, double otherCharge2, string sccdPostBack);

        //public dynamic GeTINVPLNationalDetail(string userId, string FromDate, string reportFor, string ignore112A, string scripCd);

        //public dynamic GetINVPLNationalSummary(string userId, string strDate, Boolean ignore112A, string strType);

        public dynamic Request_Get_ShareRequest(string userId);

        public dynamic Request_Post_ShareRequest(string userId, string scripCode, string quantity);

        public dynamic Request_Get_FundRequest(string userId);

        public dynamic Request_Post_Report(string userId, string ExchSeg, string Report, string strFromDt, string strToDt);

        public dynamic Request_Post_FundRequest(FundRequest_Model model, string userId);

        public dynamic Request_Report_Setting();

        public dynamic GetTradeListingData(string cm_cd, string SelectedIndex, string FromDate, string ToDate);

        public dynamic GetStatusMainGridData(string cm_cd, string strCompanyCode);

        public dynamic GetStatusFundData(string cm_cd, string strCompanyCode);

        public dynamic GetStatusCollateralData(string cm_cd, string strCompanyCode);

        public dynamic GetprSecurityListRptHandler();

        public dynamic GetShortFallMainGridData(string cm_cd, string frDate);

        public dynamic DigitalDocument_List(string userId, int intProduct, string fromDate, string toDate, string docType);

        public dynamic DigitalDocument_Type(string userId);
        public dynamic Family_List(string userId);

        public dynamic Family_Add(string userId, string password, string UCC_Code);

        public dynamic Family_Remove(string UCC_Code);

        public dynamic Family_Balance(List<string> UCC_Codes);

        public dynamic Family_RetainedStock(List<string> UCC_Codes);

        public dynamic Family_Holding(List<string> UCC_Codes);

        public dynamic Family_Position(List<string> UCC_Codes);

        public dynamic Family_Transaction(FamilyTransactionModel uccCode);

        public dynamic Family_Transaction_Details(string client, string type, string fromDate, string toDate);
        public dynamic Family_BalanceJson(List<string> UCC_Codes);
        public dynamic Family_RetainedStokeJson(List<string> UCC_Codes);

        public dynamic Family_PositionJson(List<string> UCC_Codes);

        public dynamic Family_HoldingJson(List<string> UCC_Codes);

        public dynamic Family_TransactionJson(FamilyTransactionModel model);

        public dynamic Family_TransactionDetailJson(string Client, string Type, string FromDate, string ToDate);

        public dynamic DigitalDocument_File(int Product, string date, string srNo);
        public dynamic GetTradeListingDetailData(string cm_cd, string FromDate, string ToDate, string ScripCode);

        public dynamic ProfitLoss_Combined(string userId, List<ProfitLossCombinedInputModel> model);
        public dynamic Margin_LastMarginDate();

        public dynamic StockPortfolioConc(string FromDate, string ToDate, string userId);

        public GenerateOtpResponseModel GetOtpForChangeDetail(string userId, GenerateOtpModel model);
        public GenerateOtpResponseModel GetGenerateOtpNew(string userId, GenerateOtpNew model);
        public dynamic UpdateDetail(string userId, UpdateDetailModel model);
        public dynamic UpdateMobileNo(string userId, UpdateMobileNoModel model);
        public dynamic UpdateEmail(string userId, UpdateEmailModel model);
        public dynamic GetIncomeDetail();
        public dynamic Get_Change_Relation();
        public dynamic GetExchSeg();
        public dynamic GetDPID();
        public dynamic GetCompanyBranding();
        public dynamic GetIPOMainData(string userId);

        public dynamic GetIPORemark(string userId, string IPOName, string InvestorType);

        public dynamic GetIPO_Category();

        public dynamic GetIPOSubmit(string userId, IPOSubmitModel model);

        public dynamic GetIPOStatus(string userId, string IPOName, string InvestorType);

        public dynamic GetIPODelete(string userId, string IPOName, string InvestorType);

        public dynamic GetModificationRequestDetailsData(string userId);

        public dynamic ModificationRequest_Generate_OTP(string userId, string mode);

        public dynamic Generate_New_Mobile_OTP(string userId, string OTP, string New_MobileNumber, string Mobile_Type);

        public dynamic ModificationRequest_MobileNumber_Update(string userId, string OTP);

        public dynamic Generate_New_Email_OTP(string userId, string OTP, string New_EmailId, string Email_Type);

        public dynamic ModificationRequest_Email_Update(string userId, string OTP);

        public dynamic ModificationRequest_Income_Update(string userId, modification_upload_document upload);

        public dynamic Modification_Generate_Combined_OTP(string userId, string Old_OTP, string New_Mobile_Number, string Mobile_Owner_Type, string New_Email_Id, string Email_Owner_Type);

        public dynamic ModificationRequest_Combined_Update(string userId, ModificationRequestCombinedModel combined);
        public dynamic CollateralAllocation(string userId);
        public dynamic Client_Closer(AccountCloserModel data, string userId);
        dynamic GetKyc_Details(FinalReKycModel respModel, string userId);
        public dynamic ClientMasterReKyc(string userId);
        dynamic AddKyc_Details(string userId, object model, string status, string refNo);
        dynamic ReKYC_MasterTable(string userId, string status, string rekyc, int step, string desc, string requestType);
        public dynamic ReKYC_GetSteps(string userId, string returnValue, string reqType);
        dynamic AddEsignPdf(string userId, byte[] pdfFile, string fileName, string fieldName, string refNo);

        dynamic RekycViewAllModification(string fromDate, string toDate, string status);
        dynamic RekycCheckerFullContent(string ClientCode, string refNo);
        dynamic RekycApprove(string loginUser, RekycApprovePost model, string compCode);
        dynamic GetStateMaster();
        dynamic GetNomineeRelation();

        dynamic GetMasterData(string mastName);

        public dynamic Execute(CommonModel model);

        public dynamic TradeWebCommonGrid(string userId, TradeWebDataGridRequest model);
        public bool CommonAPI_Authorize(string ModuleName);
        public dynamic AddLog_Session(string userId, string strWebYN, string loginAs, string type = "");
        public dynamic CommonAPIrepoCall(string strModuleName, string strFunctionName, string strXML);
        public dynamic XMLCommonSPCall(string strXML, string procedureName);
        public string UploadPdfForEsignSetu(string refNo, string pdfBase64, string identifierMobileNo, string DispName, string yob, string esignRedirectUrl, string fileName, string clientCode, string modulName);
        public dynamic SendOTPcommon(string userId, EmailSmsReqModel reqObject);
        public dynamic UpdatePasswordFP(string userId, string otp, string newPassword);
        public dynamic PostData(string strRequest);
        public dynamic CompileTypst(string ReportName, string ReportDisplayName, DataSet ds, string pdfType);
        public dynamic CompileTypstSeperateFiles(string ReportName, string ReportDisplayName, DataSet ds, string pdfType);
        public dynamic DownloadMultiplePdfZip(string ReportName, string ReportDisplayName, DataSet ds, string pdfType);
        public dynamic Login(string UserId, string Password, string EPassword, string Key, string LoginAs, string Product, string ICPV, string Feature, string isSSO, string IP = "");
        public dynamic GetDebugFlag(string ModuleName, string FunctionName);
        public dynamic Verify2FA(string userId, string password, string loginAs, bool branch, string loginRole, string role, string strIdentity, int expTime, int refreshExpTime, DataTable userData);
        public (string tokenString, string refreshToken) RefreshToken(string UserCode, string role, string guidVal, int expTime, string strUserAccess, string strLoginAs, int refExpTime, bool branch);
        public dynamic LoginSSO_Req(string xmlStr);
        public dynamic ImportLargeFileRepo(object request);
        public dynamic CompileExcelExport(string ReportName, string ReportDisplayName, DataSet dts);
        public dynamic VerifyBiometric(string userId, string loginAs, string role, string strModuleName);
        public dynamic UpdateImportSeqFilter(object request);
        public dynamic CompileTypstSendMail(string ReportName, string ReportDisplayName, DataSet ds, string pdfType);
        public Task<DataSet> GenerateQuestPDF(string ReportName, string ReportDisplayName, DataSet ds, string pdfType, string userId);
        
        
       // public dynamic GenerateTypstUsingDBTable(string ReportName, string ReportDisplayName, DataSet ds, string reportType);
        public dynamic GenerateTypstAccountOpeningJson(string ReportName, string ReportDisplayName, string typstJSON);

        public dynamic TestPDFHardcode(string ReportName, string ReportDisplayName, string json, string pdfType);
    }

}
