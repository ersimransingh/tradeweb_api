using System.Collections.Generic;
using System.Data;

namespace TradeWeb.API.Models
{
    public class FamilyRetainedStokeResponse
    {
        public string FamilyCode { get; set; }
        public string FamilyName { get; set; }
        public List<StockDetails> StockDetails { get; set; }
    }

    public class StockDetails
    {
        public string ISIN { get; set; }
        public string Name { get; set; }
        public string Qty { get; set; }
        public string Valuation { get; set; }
    }

    public class FamilyData
    {
        public string Code { get; set; }
        public string Name { get; set; }
    }

    public class FamilyCommonJson
    {
        public List<FamilyData> Header { get; set; }

        public DataTable Data { get; set; }
    }
}
