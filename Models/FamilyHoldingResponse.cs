using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class FamilyHoldingResponse
    {
        public string FamilyCode { get; set; }
        public string FamilyName { get; set; }
        public List<HoldingDetails> HoldingDetails { get; set; }
    }

    public class HoldingDetails
    {
        public string ISIN { get; set; }
        public string Name { get; set; }
        public string Qty { get; set; }
        public string Valuation { get; set; }
    }
}
