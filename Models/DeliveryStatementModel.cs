using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class DeliveryStatementModel
    {
        public string Settlement { get; set; }
        public List<DeliverySettData> Data { get; set; }
    }

    public class DeliverySettData
    {
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string GroupCode { get; set; }
        public string GroupName { get; set; }
        public string FamilyCode { get; set; }
        public string FamilyName { get; set; }
        public List<DeliveryStatementData> Data { get; set; }
    }

    public class DeliveryStatementData
    {
        public double ReceivedByUs { get; set; }
        public string ScripCode { get; set; }
        public string ScripName { get; set; }
        public string ISINCode { get; set; }
        public double GivenByUs { get; set; }
    }

    public class TempDeliveryStatementModel
    {
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string GroupCode { get; set; }
        public string GroupName { get; set; }
        public string FamilyCode { get; set; }
        public string FamilyName { get; set; }
        public string Settlement { get; set; }
        public double ReceivedByUs { get; set; }
        public string ScripCode { get; set; }
        public string ScripName { get; set; }
        public string ISINCode { get; set; }
        public double GivenByUs { get; set; }
    }

    public class DeliveryStatementReq
    {
        public string TradeDate { get; set; }
        public string Settlement { get; set; }
        //public string BuySell { get; set; }
        public string Security { get; set; }
        //public bool ShowISIN { get; set; }
        public Filter Filter { get; set; }
    }
}
