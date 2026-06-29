using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class CrossOffMarketReq
    {
        public string TransectionDate { get; set; } //
        public string ExecDate { get; set; } //
        public string InternalRefNo { get; set; } //
        public string ClientID { get; set; } //
        public string BranchCode { get; set; } //
        public string ReceiveMode { get; set; } //
        public string InstrumentType { get; set; } //
        public string TransectionType { get; set; } //
        public List<OffMarketData> Data { get; set; }
    }
    public class OffMarketData
    {
        public string ClientID { get; set; }
        public string ISIN { get; set; }
        public double Qty { get; set; }
        public string CounterSettNo { get; set; }
        public string FromSettNo { get; set; }
        public string Remarks { get; set; }
        public string Reason { get; set; }
        public string PaymentMode { get; set; }
        public string PayeeName { get; set; }
        public string ChequeOrRefNo { get; set; }
        public string DateOfIssue { get; set; }
        public string BankAccountNo { get; set; }
        public string BankName { get; set; }
        public string BranchName { get; set; }
        public string Consideration { get; set; }
        public string PaidBy { get; set; }
        public string Exchange { get; set; }
        public string Segment { get; set; }
        public string UCC { get; set; }
        public string CMId { get; set; }
        public string EntryBy { get; set; }
        public string TMID { get; set; }
    }
    public class OffMarketFindResponce
    {
        public string InstrumentTypeCode { get; set; }
        public string InstrumentType { get; set; }
        public string TransectionDate { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string InternalRefNo { get; set; }
        public string ClientID { get; set; }
        public string ClientName { get; set; }
        public string ExecDate { get; set; }
        public dynamic Data { get; set; }
    }
    public class InterDipositoryAddReq
    {
        public string InstrumentType { get; set; }
        public string TransectionType { get; set; }
        public string TransectionDate { get; set; }
        public string Branch { get; set; }
        public string InternalRefNo { get; set; }
        public string ClientID { get; set; }
        public string ExecutionDate { get; set; }
        public string ReceiveMode { get; set; }
        public List<InterDipositoryAddData> Data { get; set; }
    }
    public class InterDipositoryAddData
    {
        public string ClientID { get; set; }
        public string ISIN { get; set; }
        public string DPId { get; set; }
        public double Qty { get; set; }
        public string CounterSettNo { get; set; }
        public string FromSettNo { get; set; }
        public string Remarks { get; set; }
        public string Reason { get; set; }
        public string PaymentMode { get; set; }
        public string PayeeName { get; set; }
        public string ChequeOrRefNo { get; set; }
        public string DateOfIssue { get; set; }
        public string BankAccountNo { get; set; }
        public string BankName { get; set; }
        public string BranchName { get; set; }
        public string Consideration { get; set; }
        public string PaidBy { get; set; }
        public string Exchange { get; set; }
        public string Segment { get; set; }
        public string UCC { get; set; }
        public string CMId { get; set; }
        public string EntryBy { get; set; }
        public string EarlyPayin { get; set; }
        public string TMID { get; set; }
    }
}
