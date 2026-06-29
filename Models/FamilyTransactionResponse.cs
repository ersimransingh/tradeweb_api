using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class FamilyTransactionModel
    {
        public int Type { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public List<string> uccCodes { get; set; }
    }
}
