using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class Modification_Read_Combined_OTP_Response
    {
        public List<NewDetails> NewDetails { get; set; }
    }
    public class NewDetails
    {
        public string New_Mobile_number { get; set; }
        public string New_Mobile_Owner_Type { get; set; }
        public string New_Email_Id { get; set; }
        public string New_Email_Owner_Type { get; set; }
        public string Old_Gross_Income { get; set; }
        public string AsOnDate { get; set; }
    }
}
