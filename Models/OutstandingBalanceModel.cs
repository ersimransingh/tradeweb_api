using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class OutstandingBalanceModel
    {
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public dynamic Data { get; set; }
        //public double Total { get; set; }
        //public List<KeyValuePair<string,double>> Data { get; set; }
        //public List<OutstandingBalanceData> Data { get; set; }
        //public string ExchSeg { get; set; }
        //public decimal Balance { get; set; }
    }

    public class OutstandingBalanceData
    {
        public string ExchSeg { get; set; }
        public double Balance { get; set; }
    }

    public class OutstandingBalanceReq1
    {
        public string AsOnDate { get; set; }
        public List<string> Client { get; set; }
        public List<string> Branch { get; set; }
        public List<string> Group { get; set; }
        public List<string> Family { get; set; }
    }

    public class Filter
    {
        public List<string> Client { get; set; }
        public List<string> Branch { get; set; }
        public List<string> Group { get; set; }
        public List<string> Family { get; set; }
    }

    public class FilterClient
    {
        public List<string> Client { get; set; }
    }

    public class OutstandingBalanceReq
    {
        public string AsOnDate { get; set; }
        public Selection Selection { get; set; }
    }

    public class Selection
    {
        public List<string> ExchSeg { get; set; }
        public bool IncMarginAct { get; set; }
        public bool IncMTFAct { get; set; }
        public bool IncZeroBalance { get; set; }
        public Filter Filter { get; set; }
    }
}
