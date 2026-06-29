namespace TradeWeb.API.Models
{
    public class ProfitLossCombinedModel
    {
        public dynamic CashSummary { get; set; }
        public dynamic FoSummary { get; set; }
        public dynamic CommoditySummary { get; set; }
    }
    public class ProfitLossCombinedInputModel
    {
        public string Exchange { get; set; }
        public string Segment { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
    }
}
