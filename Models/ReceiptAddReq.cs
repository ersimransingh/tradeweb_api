using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class ReceiptAddReq
    {
        public string Type { get; set; }
        public string SrNo { get; set; }
        public string VoucherNo { get; set; }
        public string Date { get; set; }
        public string ClearedOn { get; set; }
        public string BankCode { get; set; }
        public List<ReceiptMultipleEntries> Entries { get; set; }
    }
    public class ReceiptMultipleEntries
    {
        public string Account { get; set; }
        public string ChequeNo { get; set; }
        public string MICR { get; set; }
        public string Particular { get; set; }
        public double Amount { get; set; }
    }
}
