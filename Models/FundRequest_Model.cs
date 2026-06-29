using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TradeWeb.API.Models
{
    public class FundRequest_Model
    {
        public double totalReqAmt { get; set; }
        public double rmsAmt { get; set; }
        public double branchReqAmt { get; set; }
        public List<FundRequest_Model_MultipleValue> data { get; set; }
    }
    public class FundRequest_Model_MultipleValue
    {
        [Required(ErrorMessage = "Please Enter Amount")]
        public string Amount { get; set; }
        [Required(ErrorMessage = "Please Enter CES_ID")]
        public string CESCd { get; set; }
    }
}
