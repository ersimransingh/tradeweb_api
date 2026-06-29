using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class Holding_DematAct_Graph
    {
        public string ClientCode { get; set; }
        public string DematAc { get; set; }
        public string ISIN { get; set; }
        public string CompanyName { get; set; }
        public string ISINName { get; set; }
        public string Quantity { get; set; }
        public string BalanceType { get; set; }
        public string Rate { get; set; }
        public string Value { get; set; }
        public List<Holding_DematAct_GraphData> graphDetails { get; set; }
    }

    public class Holding_DematAct_GraphData
    {
        public string ISIN { get; set; }
        public string RateDt { get; set; }
        public string Rate { get; set; }
    }

    public class StockPortFolioData
    {
        public string security { get; set; }
        public double quantity { get; set; }
        public double value { get; set; }
        public double profitLoss { get; set; }

    }

    public class StockPortfolioConcModel
    {
        public bool isCapitalGL { get; set; }
        public List<StockPortFolioData> data { get; set; }

    }

}
