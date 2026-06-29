using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class PerformanceCashResponseModel
    {
        public string ClientCode { get; set; }
        public string Name { get; set; }
        public List<PerformanceCashData> Data { get; set; }
        public List<PerformanceCashCharges> Charges { get; set; }
    }

    public class PerformanceCashData
    {
        public string ScripCode { get; set; }
        public string ScripName { get; set; }
        public double SellQty { get; set; }
        public double SellValue { get; set; }
        public double SellBrok { get; set; }
        public double BuyQty { get; set; }
        public double BuyValue { get; set; }
        public double BuyBrok { get; set; }
        public double NetQty { get; set; }
        public double NetValue { get; set; }
    }

    public class PerformanceCashCharges
    {
        public string ChargeCode { get; set; }
        public string Description { get; set; }
        public double Amount { get; set; }
    }

    public class TmpPerformanceCashData
    {
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string ScripCode { get; set; }
        public string ScripName { get; set; }
        public double SellQty { get; set; }
        public double SellValue { get; set; }
        public double SellBrok { get; set; }
        public double BuyQty { get; set; }
        public double BuyValue { get; set; }
        public double BuyBrok { get; set; }
        public double NetQty { get; set; }
        public double NetValue { get; set; }
    }

    public class PerformanceRequestModel
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public Filter Filter { get; set; }
    }
}
