using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class FamilyPositionResponse
    {
        public string FamilyCode { get; set; }
        public string FamilyName { get; set; }
        public List<PositionDetails> PositionDetails { get; set; }
    }

    public class PositionDetails
    {
        public string Description { get; set; }
        public string Exchange { get; set; }
        public string Net { get; set; }
        public string AvgRate { get; set; }
        public string Total_ClosingRate { get; set; }
        public string Total_Exposure { get; set; }

    }
}
