using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class JournalRequest
    {
        public string SrNo { get; set; }
        public string Date { get; set; }
        public string VoucherNo { get; set; }
        public string Particular { get; set; }
        public List<JournalEntries> Entries { get; set; }
    }

    public class JournalEntries
    {
        public string Account { get; set; }
        public string DrCr { get; set; }
        public double Amount { get; set; }
        public string Particular { get; set; }
    }
}
