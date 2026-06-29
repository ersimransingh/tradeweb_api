using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class PerformanceRepRequest
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public int DataType { get; set; }
        public List<string> Credit { get; set; }
        public List<string> Debit { get; set; }
    }
}
