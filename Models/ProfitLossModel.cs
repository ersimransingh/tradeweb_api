namespace TradeWeb.API.Models
{
    public class ProfitLossCashSummaryRequest
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public int StockValuation { get; set; }
        public FilterClient Filter { get; set; }
    }

    public class ProfitLossCashDetailRequest
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string scripCode { get; set; }
        public FilterClient Filter { get; set; }
    }

    public class ProfitLossFOSummaryRequest
    {
        public string Exchange { get; set; }
        public string Segment { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public bool IncludeBfOptions { get; set; }
        public int BfOptionPL { get; set; }
        public FilterClient Filter { get; set; }
    }

    public class ProfitLossCommoditySummaryRequest
    {
        public string Exchange { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public FilterClient Filter { get; set; }
    }
}
