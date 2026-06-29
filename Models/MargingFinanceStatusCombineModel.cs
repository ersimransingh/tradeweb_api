using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class MargingFinanceStatusCombineModel
    {
        public List<MTFRMSReport> Status { get; set; }
        public dynamic Funded { get; set; }
        public dynamic Collateral { get; set; }
    }
    public class MTFRMSReport
    {
        public string Client { get; set; }
        public string ClientName { get; set; }
        public string Limit { get; set; }
        public string TradingBalance { get; set; }
        public string LoanBalance { get; set; }
        public string FundedAmount { get; set; }
        public string InitialMarginReq { get; set; }
        public string CollateralFund { get; set; }
        public string CollateralStock { get; set; }
        public string ShortFallExcess { get; set; }
        public string TradeValue { get; set; }
        public string M2MLoss { get; set; }

    }
}
