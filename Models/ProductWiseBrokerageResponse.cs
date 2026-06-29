using System.Collections.Generic;

namespace TradeWeb.API.Models
{

    public class ProductWiseBrokerageRequest
    {
        public string ExchSeg { get; set; }
        public string Scheme { get; set; }
    }

    public class ProductWiseBrokerageResponse
    {
        public List<ProductWiseBrokerageRecord> ProductBrokerageRecord { get; set; }
        //public List<string> ProductAssetCd { get; set; }
    }

    public class ProductWiseBrokerageRecord
    {
        public string Product { get; set; }
        public string Type { get; set; }
        public ProductWiseBrokerageDayWise SD1 { get; set; }
        public ProductWiseBrokerageDayWise SD2 { get; set; }
        public ProductWiseBrokerageDayWise ADS { get; set; }
    }

    public class ProductWiseBrokerageDayWise
    {
        public string Percent { get; set; }
        public string Perlot { get; set; }
    }
}
