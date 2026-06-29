namespace TradeWeb.API.Models
{
    public class TransactionStatusReq
    {
        public Filter Filter { get; set; }
        public string DateType { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
    }
    public class TransactionStatusResponse
    {
        public string SlipNo { get; set; }
        public dynamic SlipData { get; set; }
        public string TransactionDate { get; set; }
        public string ClientID { get; set; }
        public string ClientName { get; set; }
        public string ISIN { get; set; }
        public string CompanyName { get; set; }
        public double Qty { get; set; }
        public string Status { get; set; }
        public string ExecutionDate { get; set; }
        public string NoOfTransection { get; set; }
    }
}
