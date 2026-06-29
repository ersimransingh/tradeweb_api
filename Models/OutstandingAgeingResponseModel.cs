namespace TradeWeb.API.Models
{
    public class OutstandingAgeingResponseModel
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public double Outstanding { get; set; }
        public dynamic Data { get; set; }
    }

    public class OutstandingAgeingRequestModel
    {
        public string AsOnDate { get; set; }
        public string PeriodType { get; set; }
        public int Period { get; set; }
        public int Columns { get; set; }
        public Filter Filter { get; set; }
    }

    public class TempOutstandingAgeingRecords
    {
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public double MonthList { get; set; }
        public double Outstanding { get; set; }
        public double MonthWise { get; set; }
    }
}
