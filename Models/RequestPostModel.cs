using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TradeWeb.API.Models
{
    public class Request_Post_ShareRequestModel
    {
        public List<Request_Post_MultipleValue> data { get; set; }
    }


    public class Request_Post_MultipleValue
    {
        [Required(ErrorMessage = "Please Enter ScripCode")]
        public string scrip_Code { get; set; }
        [Required(ErrorMessage = "Please Enter Quantity")]
        public string request_Quantity { get; set; }

    }
}
