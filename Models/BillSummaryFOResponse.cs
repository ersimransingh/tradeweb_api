namespace TradeWeb.API.Models
{
    public class BillSummaryFOResponse
    {
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public double MTM { get; set; }
        public double Premium { get; set; }
        public double AxerAssgn { get; set; }
        public double Charges { get; set; }
        public double Margin { get; set; }
        public double DrCr { get; set; }
        public double Brokerage { get; set; }
    }

    public class BillSummaryFORequest
    {
        public string Date { get; set; }
        public string ExchSeg { get; set; }
        public Filter Filter { get; set; }
    }
}
