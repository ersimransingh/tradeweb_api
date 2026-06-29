using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class DpHoldingDetail
    {
        public string BoId { get; set; }
        public string Name { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string City { get; set; }
        public string Pincode { get; set; }
        public string AccountFlag { get; set; }
        public string MobileNo { get; set; }
        public string EmailId { get; set; }
        public string SmartIndicator { get; set; }
        public string AccountType { get; set; }
        public string PanNo { get; set; }
        public string UID { get; set; }
        public string Occupation { get; set; }
        public string AnnualIncome { get; set; }
        public string POAForPayin { get; set; }
        public string BankAccountNo { get; set; }
        public string MICRCode { get; set; }
        public string IFSCCode { get; set; }
        public string BankName { get; set; }
        public string BankAccountType { get; set; }

        public string SecondHolderName { get; set; }
        public string SecondHolderMobileNo { get; set; }
        public string SecondHolderEmailId { get; set; }
        public string ThirdHolderName { get; set; }
        public string ThirdHolderMobileNo { get; set; }
        public string ThirdHolderEmailId { get; set; }
        public List<POADetail> POADetails { get; set; }
        public List<UCCMapping> UCCMappings { get; set; }
        public List<NomineeDetail> NomineeDetails { get; set; }
    }

    public class POADetail
    {
        public string POAID { get; set; }
        public string POAType { get; set; }
        public string POAHolder { get; set; }
        public string POAStatus { get; set; }
    }

    public class UCCMapping
    {
        public string ExchangeSegment { get; set; }
        public string UCC { get; set; }
        public string TmId { get; set; }
        public string CmId { get; set; }
    }

    public class NomineeDetail
    {
        public string SrNo { get; set; }
        public string Name { get; set; }
        public string MobileNo { get; set; }
        public string EmailId { get; set; }
        public string Relation { get; set; }
    }
}
