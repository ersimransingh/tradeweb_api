namespace TradeWeb.API.Models
{
    public class PledgeForMarginResponse
    {
        public string Securities_Code { get; set; }
        public string Securities_Name { get; set; }
        public string Securities_ISIN { get; set; }
        public double Holding_Rate { get; set; }
        public double Holding_Qty { get; set; }
        public double Holding_Value { get; set; }
        public double HairCut { get; set; }
        public double NetValue { get; set; }
        public double Request_Qty { get; set; }
        public double Request_Value { get; set; }
    }
}
