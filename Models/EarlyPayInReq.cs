using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class EarlyPayInReq
    {
        public string InstrumentType { get; set; }
        public string TransectionType { get; set; }
        public string TransectionDate { get; set; }
        public string BranchCode { get; set; }
        public string InternalRefNo { get; set; }
        public string ClientID { get; set; }
        public string ReceiveMode { get; set; }
        public List<EarlyPayInData> Data { get; set; }
    }
    public class EarlyPayInData
    {
        public string SettlementID { get; set; }
        public string ClientID { get; set; }
        public string ISIN { get; set; }
        public double Qty { get; set; }
        public string CounterClientID { get; set; }
        public string Remark { get; set; }
        public string Exchange { get; set; }
        public string Segment { get; set; }
        public string UCC { get; set; }
        public string CMId { get; set; }
        public string EntryBy { get; set; }
        public string TMId { get; set; }
    }
    public class EarlyPayInResponce
    {
        public string InstrumentTypeCode { get; set; }
        public string InstrumentType { get; set; }
        public string TransectionDate { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string InternalRefNo { get; set; }
        public string ClientID { get; set; }
        public string ClientName { get; set; }
        public dynamic Data { get; set; }
    }
    public class OnMarketReq
    {
        public string InstrumentType { get; set; }
        public string TransectionType { get; set; }
        public string TransectionDate { get; set; }
        public string BranchCode { get; set; }
        public string InternalRefNo { get; set; }
        public string ClientID { get; set; }
        public string ReceiveMode { get; set; }
        public List<OnMarketData> Data { get; set; }
    }
    public class OnMarketData
    {
        public string SettlementID { get; set; }
        public string ClientID { get; set; }
        public string ISIN { get; set; }
        public double Qty { get; set; }
        public decimal ObligNo { get; set; }
        public decimal SerialNo { get; set; }
        public string Remark { get; set; }
        public string Exchange { get; set; }
        public string Segment { get; set; }
        public string UCC { get; set; }
        public string CMId { get; set; }
        public string EntryBy { get; set; }
        public string TMId { get; set; }
    }
}
