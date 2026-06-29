using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class AuditRequest
    {
        public AuditFilter Filter { get; set; }
        public int DateType { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public AccountType AccountType { get; set; }
        public double AmountFrom { get; set; }
        public double AmountTo { get; set; }
        public int Status { get; set; }
    }
    public class AuditFilter
    {
        public List<string> Client { get; set; }
        public List<string> Branch { get; set; }
        public List<string> Group { get; set; }
        public List<string> Family { get; set; }
        public List<string> Maker { get; set; }
    }
    public class AccountType
    {
        public bool Journal { get; set; }
        public bool Debit_Note { get; set; }
        public bool Credit_Note { get; set; }
        public bool Receipts { get; set; }
        public bool Payment { get; set; }
    }

    public class AuditResponse
    {
        public dynamic Journal { get; set; }
        public dynamic Debit_Note { get; set; }
        public dynamic Credit_Note { get; set; }
        public dynamic Receipts { get; set; }
        public dynamic Payment { get; set; }
    }
}
