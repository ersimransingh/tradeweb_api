using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class BrokerageOrderFOResponseModel
    {
        public List<ClientList> ClientList { get; set; }
        public Header Header { get; set; }
        public List<BrokerageOrderFORecordCategory> Detail { get; set; }
        public List<ProductWiseBrokerageRecord> ProductBrokerage { get; set; }
    }

    public class BrokerageOrderFORecordCategory
    {
        public string CategoryName { get; set; }
        public List<BrokerageOrderFORecord> Details { get; set; }
    }

    public class BrokerageOrderFORecord
    {
        public string Type { get; set; }
        public string Upto { get; set; }
        public string Fixed { get; set; }
    }
}
