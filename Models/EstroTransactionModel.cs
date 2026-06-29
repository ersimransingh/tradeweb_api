using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class EstroTransactionModel
    {
        public EstroTransactionHeader Header { get; set; }
        public List<EstroTransactionData> Data { get; set; }
    }
    public class EstroTransactionHeader
    {
        public string BOID { get; set; }
        public string BOName { get; set; }
        public string Add1 { get; set; }
        public string Add2 { get; set; }
        public string Add3 { get; set; }
        public string Add4 { get; set; }
        public string Pin { get; set; }
        public string Phone { get; set; }
        public string Joints { get; set; }
        public string FamilyCode { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string Category { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string BranchCode { get; set; }
        public string GroupCode { get; set; }
    }
    public class EstroTransactionData
    {
        public string Date { get; set; }
        public string ISIN { get; set; }
        public string ISINName { get; set; }
        public string Type { get; set; }
        public string Particular { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
    }
}
