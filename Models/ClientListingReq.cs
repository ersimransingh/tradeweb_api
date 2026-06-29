using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class ClientListingReq
    {
        public ClientListingFilter Filter { get; set; }
        public string Status { get; set; }
    }
    public class ClientListingFilter
    {
        public List<string> ClientID { get; set; }
        public List<string> Branch { get; set; }
        public List<string> Group { get; set; }
        public List<string> Family { get; set; }
        public AccDate AccOpenDate { get; set; }
        public AccDate CloseDate { get; set; }
    }
    public class AccDate
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
    }
    public class ClientListingResponse
    {
        public string ClientID { get; set; }
        public string ClientName { get; set; }
        public string Ledger { get; set; }
        public string BackOfficeCD { get; set; }
        public dynamic Scheme { get; set; }
        public dynamic Holding { get; set; }
        public dynamic POA { get; set; }
        public dynamic Nominee { get; set; }
        public dynamic Bank { get; set; }
        public dynamic PA { get; set; }
        public dynamic Signature { get; set; }
        public dynamic Slip { get; set; }
        public dynamic UCCMapping { get; set; }
    }
    public class TempClientListingRes
    {
        public string ClientID { get; set; }
        public string ClientName { get; set; }
        public string Ledger { get; set; }
        public string BackOfficeCD { get; set; }
        public string Scheme { get; set; }
    }
}
