namespace TradeWeb.API.Models
{
    public class ReceiptFind
    {
        public string SrNo { get; set; }
        public string Date { get; set; }
        public string ClearedOn { get; set; }
        public string VoucherNo { get; set; }
        public double Balance { get; set; }
        public string BankCode { get; set; }
        public dynamic Entries { get; set; }
        public double Total { get; set; }
        public double TotalBalance { get; set; }
    }
}
