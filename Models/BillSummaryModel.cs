using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class BillSummaryModel
    {
        public string SettlementNo { get; set; }
        public Filter Filter { get; set; }
    }

    public class BillSummaryResponse
    {
        public string Settlement { get; set; }
        public string TradeDate { get; set; }
        public string SettlementDate { get; set; }

        public List<BillSummaryData> Data { get; set; }
    }
    public class BillSummaryData
    {
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string GroupCode { get; set; }
        public string GroupName { get; set; }
        public string FamilyCode { get; set; }
        public string FamilyName { get; set; }
        public string SubBrokerCode { get; set; }
        public string SubBrokerName { get; set; }
        public double DueToUs { get; set; }
        public double DueToYou { get; set; }
        public string BillNo { get; set; }

    }
}
