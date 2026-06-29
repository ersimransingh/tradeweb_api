using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class FundRquest
    {
        public string ExchSeg { get; set; }
        public string CESCd { get; set; }
        public double PayOut { get; set; }
        public double ReqAmt { get; set; }
    }

    public class PayOut_FundRequest
    {
        public double BranchRequest { get; set; }
        public double RmsAmount { get; set; }
        public List<FundRquest> Data { get; set; }
    }
}
