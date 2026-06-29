using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class HoldingRequest
    {
        public Filter Filter { get; set; }
        public List<string> BalanceType { get; set; }
        public bool ShowValuation { get; set; }
        public string AsOn { get; set; }
    }
    public class HoldingResponse
    {
        public string Client { get; set; }
        public string Add1 { get; set; }
        public string Add2 { get; set; }
        public string Add3 { get; set; }
        public string City { get; set; }
        public string Pin { get; set; }
        public string Telephone { get; set; }
        public string Joint { get; set; }
        public string Status { get; set; }
        public string Category { get; set; }
        public string Scheme { get; set; }
        public string BranchName { get; set; }
        public string POAforPayIn { get; set; }
        public string BackOfficeCD { get; set; }
        public string UID { get; set; }
        public dynamic HoldingReport { get; set; }
    }
}
