using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TradeWeb.API.Models
{
    public class PledgeForMarginModel
    {
        public List<PledgeForMarginModel_MultipleValue> data { get; set; }
    }
    public class PledgeForMarginModel_MultipleValue
    {
        [Required(ErrorMessage = "Please Enter Demat Acc No")]
        public string DematActNo { get; set; }
        [Required(ErrorMessage = "Please Enter Scrip Code")]
        public string Securities_Code { get; set; }
        [Required(ErrorMessage = "Please Enter Requested Quantity")]
        public string Request_Qty { get; set; }
    }
}
