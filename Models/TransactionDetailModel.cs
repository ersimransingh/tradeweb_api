namespace TradeWeb.API.Models
{
    public class TransactionDetailModel
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string Exch { get; set; }
        public string Seg { get; set; }
        public int Type { get; set; }
        public string ScripCode { get; set; }
        public Filter Filter { get; set; }
    }
}
