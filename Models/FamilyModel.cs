using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TradeWeb.API.Models
{
    public class Family_Transaction_DetailsModel : Family_Transaction_MultipleDetails
    {
        public List<Family_Common_Model> uccCode { get; set; }
    }

    public class Family_Transaction_MultipleDetails
    {
        //[Required(ErrorMessage = "Please ClientID")]
        //public string uccCode { get; set; }
        [Required(ErrorMessage = "Please Enter Type")]
        public string Type { get; set; }
        [Required(ErrorMessage = "Please Enter From Date")]
        public string FromDate { get; set; }
        [Required(ErrorMessage = "Please Enter To Date")]
        public string ToDate { get; set; }
    }
    public class ButtonCombinedModel
    {
        public dynamic ShowLedger { get; set; }
        public dynamic ShowHolding { get; set; }
        public dynamic ShowDPHolding { get; set; }
        public dynamic ShowOutstanding { get; set; }
    }
    public class ButtonCombinedInputModel
    {
        [Required(ErrorMessage = "Please Enter ClickValue")]
        public string clickValue { get; set; }
        [Required(ErrorMessage = "Please Enter SelectedValue")]
        public string selectedCLCode { get; set; }
    }
    public class FamilyAddButtonModel
    {
        [Required(ErrorMessage = "Please Enter User Code")]
        public string uccCode { get; set; }
        [Required(ErrorMessage = "Please Enter Password")]
        public string password { get; set; }
    }
    public class Family_Remove_Model
    {
        [Required(ErrorMessage = "Please Enter ucc Code")]
        public string uccCode { get; set; }
    }
    public class Family_Common_Model
    {
        [Required(ErrorMessage = "Please Enter ucc Code")]
        public string uccCode { get; set; }
    }


}
