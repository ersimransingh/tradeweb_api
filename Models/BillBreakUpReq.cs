namespace TradeWeb.API.Models
{
    public class BillBreakUpReq
    {
        public Filter Filter { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
    }
}
