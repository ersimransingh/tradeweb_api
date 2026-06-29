using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class CrossBillModel
    {
        public CrossBillHeader Header { get; set; }
        public List<CrossBillData> Data { get; set; }
    }
    public class CrossBillHeader
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
        public string GSTNo { get; set; }
        public string GSTInvoice { get; set; }
        public string BillDate { get; set; }
        public string BillFrom { get; set; }
        public string BillTo { get; set; }
        public string Branch { get; set; }
        public string TradingCd { get; set; }
    }
    public class CrossBillData
    {
        public string Date { get; set; }
        public string Description { get; set; }
        public string Security { get; set; }
        public string ISIN { get; set; }
        public string BuySell { get; set; }
        public double Quantity { get; set; }
        public double Value { get; set; }
        public double Charges { get; set; }
        public string Flag { get; set; }
    }

    public class CrossNetBillRequest
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public Filter Filter { get; set; }

    }
}
