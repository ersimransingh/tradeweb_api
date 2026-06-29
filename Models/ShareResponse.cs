namespace TradeWeb.API.Models
{
    public class ShareResponse
    {
        public string Scrip_Code { get; set; }
        public string Scrip_Name { get; set; }
        public string ISIN { get; set; }
        public double Holding_Quantity { get; set; }
        public double Holding_Value { get; set; }
        public double Request_Quantity { get; set; }
        public double Request_Value { get; set; }
    }
}
