using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class SlipIssueReq
    {
        public string Slip_Issue_Type { get; set; }
        public string Id { get; set; }
        public string Instrument { get; set; }
        public double Leaves { get; set; }
        public string Ref_No { get; set; }
        public string Ref_Date { get; set; }
        public string Date { get; set; }
        public double Slip_No { get; set; }
    }
    public class SlipIssueRequest
    {
        public Filter Filter { get; set; }
        public string Issued_To { get; set; }
        public string From_Date { get; set; }
        public string To_Date { get; set; }
        public List<string> Instrument_Type { get; set; }
    }
    public class SlipIssueResponce
    {
        public string Id { get; set; }
        public string Id_Name { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Pin_Code { get; set; }
        public string Scheme { get; set; }
        public string Instrument_Code { get; set; }
        public string Instrument_Name { get; set; }
        public string Leaves { get; set; }
        public string Ref_No { get; set; }
        public string Ref_Date { get; set; }
        public string Date { get; set; }
        public string Slip_No { get; set; }
        public string To_No { get; set; }
        public string No_Of_Books { get; set; }
        public string Status { get; set; }
    }
}
