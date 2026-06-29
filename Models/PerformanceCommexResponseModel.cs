namespace TradeWeb.API.Models
{
    public class PerformanceCommexResponseModel
    {
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string SeriesID { get; set; }
        public string SeriesName { get; set; }
        public double BuyQty { get; set; }
        public double BuyValue { get; set; }
        public double BuyBrok { get; set; }
        public double SellQty { get; set; }
        public double SellValue { get; set; }
        public double SellBrok { get; set; }
        public double NetQty { get; set; }
        public double NetValue { get; set; }
        public string Type { get; set; }
    }

    public class PerformanceCommexRequestModel
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string ExchSeg { get; set; }
        public Filter Filter { get; set; }
    }
}
