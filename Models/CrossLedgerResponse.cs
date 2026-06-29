namespace TradeWeb.API.Models
{

    public class CrossLedgerRequest
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public Filter Filter { get; set; }
    }
}
