namespace TradeWeb.API.Models
{
    public class ClientOutstandingReq
    {
        public Filter Filter { get; set; }
        public double BalanceBetween { get; set; }
        public double BalanceTo { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
    }
    public class ClientOutstandingResponse
    {
        public string ClientID { get; set; }
        public dynamic Data { get; set; }
        public string ClientName { get; set; }
        public string BackOfficeCD { get; set; }
        public double Balance { get; set; }
        public string Scheme { get; set; }
        public string Branch { get; set; }
        public string Email { get; set; }
        public string Group { get; set; }
        public string Family { get; set; }
        public string Telephone { get; set; }
    }
    public class TempClientOutstandingResponse
    {
        public string ClientID { get; set; }
        public dynamic Data { get; set; }
        public string ClientName { get; set; }
        public string BackOfficeCD { get; set; }
        public double Balance { get; set; }
        public string Scheme { get; set; }
        public string Branch { get; set; }
        public string Email { get; set; }
        public string Group { get; set; }
        public string Family { get; set; }
        public string Telephone { get; set; }
    }
}
