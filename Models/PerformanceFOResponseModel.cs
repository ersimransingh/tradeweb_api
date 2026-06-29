using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class PerformanceFOResponseModel
    {
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public List<FuturesOptions> FuturesOptions { get; set; }
        public List<Charges> Charges { get; set; }
    }

    public class TempPerformanceFOData
    {
        public string cm_cd { get; set; }
        public string cm_name { get; set; }
        public string cm_brboffcode { get; set; }
        public string bm_branchname { get; set; }
        public string td_seriesid { get; set; }
        public string sm_sname { get; set; }
        public double BuyQty { get; set; }
        public double BuyValue { get; set; }
        public double SellQty { get; set; }
        public double SellValue { get; set; }
        public string Type { get; set; }
        public double NetQty { get; set; }
        public double NetValue { get; set; }
        public double BuyBrok { get; set; }
        public double SellBrok { get; set; }
    }

    public class PerformanceFORequestModel
    {
        public string ExchSeg { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public Filter Filter { get; set; }
    }

    public class FuturesOptions
    {
        public string SeriesID { get; set; }
        public string SeriesName { get; set; }
        public double BuyQty { get; set; }
        public double BuyValue { get; set; }
        public double BuyBrok { get; set; }
        public double SellQty { get; set; }
        public double SellValue { get; set; }
        public double SellBrok { get; set; }
        public double NetQty { get; set; }
        public double NetValue { get; set; }
        public string Type { get; set; }
    }

    public class Charges
    {
        public string ChargeCode { get; set; }
        public string Description { get; set; }
        public double Amount { get; set; }
    }

    public class TempRecord
    {
        public string Code { get; set; }
        public string Name { get; set; }
    }
}
