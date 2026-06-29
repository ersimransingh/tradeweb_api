using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class TransectionStatementRequest
    {
        public Filter Filter { get; set; }
        public string ISIN { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public List<string> TransactionType { get; set; }
    }
    public class TransectionStatementResponse
    {
        public string Date { get; set; }
        public string TrxNo { get; set; }
        public string ClientID { get; set; }
        public string ClientName { get; set; }
        public string ISIN { get; set; }
        public string ISINName { get; set; }
        public string Type { get; set; }
        public string Particular { get; set; }
        public string Debit { get; set; }
        public string Credit { get; set; }
    }
}
