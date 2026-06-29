namespace TradeWeb.API.Models
{
    public class SubscriptionAddModel
    {
        public string ClientCode { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string SpecialScheme { get; set; }
        public string NormalScheme { get; set; }
        public int NoOfOrder { get; set; }
        public int Amount { get; set; }
    }
    public class SubscriptionDiscontinueModel
    {
        public string SrNo { get; set; }
        public string ClientCode { get; set; }
    }
    public class SubscriptionStatusModel
    {
        public string ClientCode { get; set; }
        public string AsOn { get; set; }
    }
}
