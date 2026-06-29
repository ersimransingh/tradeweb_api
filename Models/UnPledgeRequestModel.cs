using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TradeWeb.API.Models
{
    public class UnPledgeRequestModel
    {
        public List<UnPledgeResponseModel_MultipleValue> data { get; set; }
    }

    public class UnPledgeResponseModel_MultipleValue
    {
        [Required(ErrorMessage = "Please Enter Scrip Code")]
        public string ScripCode { get; set; }
        [Required(ErrorMessage = "Please Enter Quantity")]
        public string Request_Qty { get; set; }
    }
}
