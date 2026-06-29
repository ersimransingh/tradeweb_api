using System;
using System.Collections.Generic;
using System.Data;

namespace TradeWeb.API.Models
{
    public class BrokerageReponseModel
    {
        public DataTable ClientList { get; set; }
        public BrokerageHeader Header { get; set; }
        public List<BrokerageCashRecordCategory> Detail { get; set; }
    }

    public class BrokerageRequestModel
    {
        public string Client { get; set; }
        public string Scheme { get; set; }
        public string Exchange { get; set; }
        //public string OrderBrokType { get; set; }
    }

    //public class BrokerageRecord
    //{
    //    public string CategoryName { get; set; }
    //    public List<BrokerageCashRecord> Details { get; set; }
    //}
    public class BrokerageCashRecord
    {
        public string Type { get; set; }
        public string UpTo { get; set; }
        public string Minimum { get; set; }
        public string Percent { get; set; }
        public string Fixed { get; set; }
        public string Maximum { get; set; }
        public string MinPerContract { get; set; }
    }

    public class BrokerageHeader
    {
        public string SchemeCode { get; set; }
        public string ExchSeg { get; set; }
        public string BrokerageType { get; set; }
        public string DuringTheDay { get; set; }
        public string SameSett { get; set; }
        public double Quantity { get; set; }
        public double BrokerageRounding { get; set; }
        public string AdvantageOf { get; set; }
        //public Boolean IsMax { get; set; }
        //public double PercentageOfTradeValue { get; set; }
        public Boolean IsDeliveryBrokerage { get; set; }
    }

    public class TempBrokerageCashRecord
    {
        public string Type { get; set; }
        public string UpTo { get; set; }
        public string Minimum { get; set; }
        public string Percent { get; set; }
        public string Fixed { get; set; }
        public string Maximum { get; set; }
        public string MinPerContract { get; set; }
        public string Category { get; set; }
    }

    public class BrokerageCashRecordCategory
    {
        public string CategoryName { get; set; }
        public List<BrokerageCashRecord> Details { get; set; }
    }

    public class BrokerageCashOrderByReponseModel
    {
        public DataTable ClientList { get; set; }
        public BrokerageCashHeader Header { get; set; }
        public List<BrokerageCashOrderByDetails> Detail { get; set; }
    }

    public class BrokerageCashOrderByDetails
    {
        public string Type { get; set; }
        public double BuyOrders { get; set; }
        public double SellOrders { get; set; }
        public double Percentage { get; set; }
        public double Minimum { get; set; }
    }

    public class BrokerageCashHeader
    {
        public string SchemeCode { get; set; }
        public string ExchSeg { get; set; }
        public string BrokerageType { get; set; }
        public double Quantity { get; set; }
        public double BrokerageRounding { get; set; }
        public string AdvantageOf { get; set; }
        public Boolean IsMax { get; set; }
        public double PercentageOfTradeValue { get; set; }
        public Boolean IsDeliveryBrokerage { get; set; }
    }
}
