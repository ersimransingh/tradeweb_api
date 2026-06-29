namespace TradeWeb.API.Models
{
    public class CrossBillSummaryReq
    {
        public Filter Filter { get; set; }
        public string BillDate { get; set; }
        public string BillType { get; set; }
        public bool LedgerBalance { get; set; }
    }
    public class CrossBillSummaryResponse
    {
        public string Bill_No { get; set; }
        public string BO_ID { get; set; }
        public dynamic Data { get; set; }
        public string BO_Name { get; set; }
        public string Bill_Amount { get; set; }
        public string Ledger_Balance { get; set; }
    }
    public class TempCrossBillSummaryResponse
    {
        public string Bill_No { get; set; }
        public string BO_ID { get; set; }
        public string BO_Name { get; set; }
        public string Bill_Amount { get; set; }
        public string Ledger_Balance { get; set; }
    }
    public class BillSData
    {
        public dynamic BO_Details { get; set; }
        public dynamic Transection_Details { get; set; }
        public dynamic Transection_Charges_Details { get; set; }
        public dynamic Statement_Holdings { get; set; }
        public dynamic Ledger_Summary { get; set; }
        public dynamic Additional_Charges { get; set; }
    }
    public class TempHoldingData
    {
        public string Type { get; set; }
    }
    public class HoldingData
    {
        public string Type { get; set; }
        public dynamic Data { get; set; }
    }
}
