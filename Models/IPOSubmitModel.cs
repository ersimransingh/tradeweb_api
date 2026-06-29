namespace TradeWeb.API.Models
{
    public class IPOSubmitModel
    {
        public string IPOName { get; set; }
        //public string PriceRange { get; set; }
        public string MinimumOrder { get; set; }
        //public string TickSize { get; set; }
        public string Discount { get; set; }
        public string CutoffPrice { get; set; }
        public string InvestorType { get; set; }
        public string UPIid { get; set; }
        public string Qty1 { get; set; }
        public string CutoffFlag1 { get; set; }
        public string Price1 { get; set; }
        public string Qty2 { get; set; }
        public string CutoffFlag2 { get; set; }
        public string Price2 { get; set; }
        public string Qty3 { get; set; }
        public string CutoffFlag3 { get; set; }
        public string Price3 { get; set; }
        public string totalAmount { get; set; }
        //public Boolean ChkAccept { get; set; }
        //public List<Bid1> Bid_1 { get; set; }
        //public List<Bid2> Bid_2 { get; set; }
        //public List<Bid3> Bid_3 { get; set; }
    }
    public class Bid1
    {
        public string Qty1 { get; set; }
        public string Cutoff_Price1 { get; set; }
    }
    public class Bid2
    {
        public string Qty2 { get; set; }
        public string Cutoff_Price2 { get; set; }
    }
    public class Bid3
    {
        public string Qty3 { get; set; }
        public string Cutoff_Price3 { get; set; }
    }
    public class IBBSResponse
    {
        public string Status { get; set; }
        public string Data { get; set; }
    }
}
