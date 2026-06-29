namespace TradeWeb.API.Models
{
    public class CrossLedgerReq
    {
        public Filter Filter { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
    }
    public class CrossLedgerResponse
    {
        public dynamic Account { get; set; }
        public dynamic Ledger_Details { get; set; }
    }
    public class AccountData
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string Date { get; set; }
    }
}
