namespace TradeWeb.API.Models
{
    public class JournalGetResponse
    {
        public string SrNo { get; set; }
        public string Date { get; set; }
        public string VoucherNo { get; set; }
        public string Particular { get; set; }
        public dynamic Entries { get; set; }
        public JournalTotalResponse Total { get; set; }
    }
    public class JournalTotalResponse
    {
        public dynamic Debit { get; set; }
        public dynamic Credit { get; set; }
    }
}
