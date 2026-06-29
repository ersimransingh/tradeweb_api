using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class MargingFinanceShortFallModel
    {
        public List<ShortFall> ShortFall { get; set; }
        public List<Funded> Funded { get; set; }
    }
    public class ShortFall
    {
        public string Client { get; set; }
        public string ClientName { get; set; }
        public string Limit { get; set; }
        public string TradingBalance { get; set; }
        public string LoanBalance { get; set; }
        public string FundedAmount { get; set; }
        public string Initial_MarginReq { get; set; }
        public string Collateral_Fund { get; set; }
        public string Collateral_Stock { get; set; }
        public string ShortFallExcess { get; set; }
        public string TradeValue { get; set; }
        public string M2MLoss { get; set; }
        public string ContinuousShortfall { get; set; }
    }
    public class Funded
    {
        public string ScripCode { get; set; }
        public string ScripName { get; set; }
        public string Qty { get; set; }
        public string ActualCost { get; set; }
        public string ClosePrice { get; set; }
        public string MTM { get; set; }
        public string MarginPerc { get; set; }
        public string MarginReq { get; set; }
        public string Exchange { get; set; }

    }
}
