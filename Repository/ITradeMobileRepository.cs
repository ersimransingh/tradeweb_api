namespace TradeWeb.API.Repository
{
    public interface ITradeMobileRepository
    {
        public dynamic GetUserProfile(string clientCd);
        public dynamic GetLedgerYear(string clientCd);
        public dynamic GetLedgerBalance(string clientCd, string strYear);
        public dynamic GetLedgerDetailsM(string clientCd, string DpId, string FromDt, string ToDt);
        public dynamic GetTrxDates(string clientCd, string Seg);
        public dynamic GetTradesForDate(string clientCd, string StartDt, string Seg);
        public dynamic GetTrxItems(string clientCd, string Seg);
        public dynamic GetTrxItemsDetail(string clientCd, string Seg, string ScripCd);
        public dynamic GetHoldingSummary(string clientCd);
        public dynamic GetDPHolding(string clientCd, string DematActNo);
        public dynamic GetBenHolding(string clientCd);
        public dynamic GetCollateral(string clientCd);
        public dynamic GetMarginShortFall(string clientCd);
        public dynamic GetBillsYear(string clinetCd, string Seg);
        public dynamic GetBillsN(string clientcd, string Seg, string Year);
        public dynamic GetBillDetail(string clientCd, string Date, string Exch, string Seg, string compCd);
        public dynamic GetOutstandingSummary(string clientCd);
        public dynamic GetOutstandingDetail(string clientCd, string FutureOption, string ExchSeg);
        public dynamic GetRMSPayoutAmount(string clientCd, string Type);
        public dynamic GetInvestorPLCash(string clientCd, string FromDt, string ToDt, string ScripCd, string ReportType, string StockValuation);
        public dynamic GetParameter(string ParameterCd);
        public dynamic GetReportParm(string clientCd);
        public dynamic SMSSetting();
        public dynamic ForgetPasswordSMS(string clientCd);
        public dynamic ChangePassword(string clientCd, string OldPwd, string NewPwd);
        public dynamic PostFundRequest(string clientCd, string flag, string Type, string Value);
        public dynamic PostShareRequest(string clientCd, string Flag, string ScripCd, string Qty);
        public dynamic PostRequestForreport(string clientCd, string Report, string FromDt, string ToDt, string LastSeg);
        public dynamic ForgotPasswordVerifyOTP(string clientCd, string OTP, string NewPassword, string ConfirmPassword);

    }
}
