using System.ComponentModel.DataAnnotations;

namespace TradeWeb.API.Models
{
    public class modification_upload_document
    {
        [Key]
        public string OTP { get; set; }
        //public string OldValue { get; set; }
        //public string Reason { get; set; }
        public string grossIncome { get; set; }
        public string AsOnDate { get; set; }
        public string image { get; set; }
    }
}
