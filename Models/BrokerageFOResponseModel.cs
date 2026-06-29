using System;
using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class BrokerageFORequestModel
    {
        public string ExchSeg { get; set; }
        public string Client { get; set; }
        public string Scheme { get; set; }
    }

    public class BrokerageFOResponseModel
    {
        public List<ClientList> ClientList { get; set; }
        public Header Header { get; set; }
        public List<BrokerageFORecordCategory> Detail { get; set; }
        public List<ProductWiseBrokerageRecord> ProductBrokerage { get; set; }
    }

    public class BrokerageFORecordCategory
    {
        public string CategoryName { get; set; }
        public List<BrokerageFORecord> Details { get; set; }
    }

    public class ClientList
    {
        public string ClientCode { get; set; }
        public string Name { get; set; }
    }

    public class Header
    {
        public string SchemeCode { get; set; }
        public string ExchSeg { get; set; }
        public int SameDayId { get; set; }
        public string SameDay { get; set; }
        public int AnyDayId { get; set; }
        public string AnyDay { get; set; }
        public int AdvantageId { get; set; }
        public string Advantage { get; set; }
        public string RoundTo { get; set; }
        public string MinPerContract { get; set; }
        public string MinPerContractOpt { get; set; }
        public Boolean IsPerLotBrokerage { get; set; }
    }

    public class BrokerageFORecord
    {
        public string Type { get; set; }
        public string Upto { get; set; }
        public BrokerageDayWiseRecord SameDay1stSide { get; set; }
        public BrokerageDayWiseRecord SameDay2ndSide { get; set; }
        public BrokerageDayWiseRecord AnyDaySide { get; set; }
    }

    public class BrokerageDayWiseRecord
    {
        public string Min { get; set; }
        public string Percent { get; set; }
        public string Perlot { get; set; }
        public string Max { get; set; }
    }

    public class TempBrokerageRecord
    {
        public string Type { get; set; }
        public string Upto { get; set; }
        public string Category { get; set; }
        public string FSideMin { get; set; }
        public string FSidePercent { get; set; }
        public string FSidePerlot { get; set; }
        public string FSideMax { get; set; }
        public string SecSideMin { get; set; }
        public string SecSidePercent { get; set; }
        public string SecSidePerlot { get; set; }
        public string SecSideMax { get; set; }
        public string AnySideMin { get; set; }
        public string AnySidePercent { get; set; }
        public string AnySidePerlot { get; set; }
        public string AnySideMax { get; set; }
    }
}
