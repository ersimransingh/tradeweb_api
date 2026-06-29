namespace TradeWeb.API.Models
{
    public class CommissionReportResponseModel
    {
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string GroupCode { get; set; }
        public string GroupName { get; set; }
        public string FamilyCode { get; set; }
        public string FamilyName { get; set; }
        public dynamic Data { get; set; }
    }

    public class TempCommissionReportRecords
    {
        public string ClientCode { get; set; }
        public string Date { get; set; }
        public string ClientName { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string GroupCode { get; set; }
        public string GroupName { get; set; }
        public string FamilyCode { get; set; }
        public string FamilyName { get; set; }
        public dynamic Data { get; set; }
    }

    public class CommissionReportRequestModel
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string ReportType { get; set; }
        public Filter Filter { get; set; }
    }

    public class SegmentWiseDetail
    {
        public double Brokerage { get; set; }
        public double Remmisier1_Share { get; set; }
        public double Remmisier2_Share { get; set; }
        public double Net { get; set; }
    }
}
