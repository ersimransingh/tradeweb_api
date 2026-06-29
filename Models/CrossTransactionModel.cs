using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class CrossTransactionModel
    {
        public CrossTransactionHeader Header { get; set; }
        public List<CrossTransactionData> Data { get; set; }
    }
    public class CrossTransactionHeader
    {
        public string BOID { get; set; }
        public string BOName { get; set; }
        public string Add1 { get; set; }
        public string Add2 { get; set; }
        public string Add3 { get; set; }
        public string City { get; set; }
        public string Pin { get; set; }
        public string Telephone { get; set; }
        public string Joints { get; set; }
        public string Category { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string BranchCode { get; set; }
    }
    public class CrossTransactionData
    {
        public string ISIN { get; set; }
        public string ISINName { get; set; }
        public List<CrossTransactionISINData> Data { get; set; }
    }
    public class CrossTransactionISINData
    {
        public string Type { get; set; }
        public List<CrossTransactionTypeData> Data { get; set; }
    }
    public class CrossTransactionTypeData
    {
        public string Date { get; set; }
        public string TrxNo { get; set; }
        public string Particular { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
    }
    public class TempCrossTransactionData
    {
        public string Date { get; set; }
        public string ISIN { get; set; }
        public string TrxNo { get; set; }
        public string ISINName { get; set; }
        public string Type { get; set; }
        public string Particular { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
    }

    public class CrossNetTransactionRequest
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public Filter Filter { get; set; }
    }
}
