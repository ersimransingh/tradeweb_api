using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class OutstandingPositionModel
    {
        public string Type { get; set; }
        public string LongName { get; set; }
        public string ShortName { get; set; }
        public double Buy { get; set; }
        public double Sell { get; set; }
        public double Net { get; set; }
        public string CESCD { get; set; }
        public double AvgRate { get; set; }
        public double Closeprice { get; set; }
        public double Closing { get; set; }
        public int SortOrder { get; set; }
        public string ExchSeg { get; set; }
        public double SeriesID { get; set; }
        public double Multiplier { get; set; }
    }

    public class OutstandingPositionReq
    {
        public string AsOnDt { get; set; }
        public List<string> ExchSeg { get; set; }
        public Filter Filter { get; set; }
    }

    public class OutstandingPositionRes
    {
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public double SpanMargin { get; set; }
        public double ExposureMargin { get; set; }
        public List<OutstandingPositionModel> Data { get; set; }
    }

    public class TempOutstandingPositionRecords
    {
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string Type { get; set; }
        public string LongName { get; set; }
        public string ShortName { get; set; }
        public double Buy { get; set; }
        public double Sell { get; set; }
        public double Net { get; set; }
        public string CESCD { get; set; }
        public double AvgRate { get; set; }
        public double Closeprice { get; set; }
        public double Closing { get; set; }
        public int SortOrder { get; set; }
        public string ExchSeg { get; set; }
        public double SeriesID { get; set; }
        public double Multiplier { get; set; }
    }
}
