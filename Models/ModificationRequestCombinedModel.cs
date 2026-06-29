using System.ComponentModel.DataAnnotations;

namespace TradeWeb.API.Models
{
    public class ModificationRequestCombinedModel
    {
        [Key]
        //public List<NewMobileDetails> NewMobileDetails { get; set; }
        //public List<NewEmailDetails> NewEmailDetails { get; set; }
        //public List<modification_upload_document> NewIncomeDetails { get; set; } 
        public string New_Mobile_OTP { get; set; }
        public string New_Email_OTP { get; set; }
        public string NewGrossIncome { get; set; }
        public string AsOnDate { get; set; }
        public string image { get; set; }

    }
}
