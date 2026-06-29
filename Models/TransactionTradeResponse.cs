using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class TransactionTradeResponse
    {
        public string FamilyCode { get; set; }
        public string FamilyName { get; set; }
        public List<TransactionTradeDetails> TransactionTradeDetails { get; set; }
    }

    public class TransactionTradeDetails
    {
        public string Script_Code { get; set; }
        public string Script_Name { get; set; }
        public string Qty { get; set; }
        public string Rate { get; set; }
    }

    public class TransactionRecieptResponse
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Receipt { get; set; }
        public string Payment { get; set; }
    }

    public class TransactionJournalResponse
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Debit { get; set; }
        public string Credit { get; set; }
    }

    public class TransactionDetailJournalResponse
    {
        public string RefNo { get; set; }
        public string Date { get; set; }
        public string Particulars { get; set; }
        public string Debit { get; set; }
        public string Credit { get; set; }
    }

    public class TransactionDetailReceiptResponse
    {
        public string RefNo { get; set; }
        public string Date { get; set; }
        public string Particulars { get; set; }
        public string Instrument { get; set; }
        public string Amount { get; set; }
    }
}