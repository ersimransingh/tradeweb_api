using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class EntryGSTInvoiceResponse
    {
        public string Status { get; set; }
        public ErrorResponse Data { get; set; }
    }

    public class ErrorResponse
    {
        public string Message { get; set; }
        public List<EntryGSTInvoiceRes> Data { get; set; }
    }

    public class EntryGSTInvoiceRes
    {
        public string LineNo { get; set; }
        public string Response { get; set; }
    }

    public class EntryGSTInvoiceRequest
    {
        public string ExchSeg { get; set; }
        public string Date { get; set; }
        public List<BulkData> Data { get; set; }
    }

    public class BulkData
    {
        public string Narration { get; set; }
        public string CreditAccount { get; set; }
        public string Clientcode { get; set; }
        public double Amount { get; set; }
    }
}
