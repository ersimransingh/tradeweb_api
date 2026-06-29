namespace TradeWeb.API.Models
{
    public class SlipStopReq
    {
        public string ClientID { get; set; }
        public string Instrument { get; set; }
        public double From { get; set; }
        public double To { get; set; }
        public string Reference { get; set; }
        public string RefDate { get; set; }
        public string Date { get; set; }
        public string Remarks { get; set; }
    }
}
