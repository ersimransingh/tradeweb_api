using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class ClientMasterModel
    {
        public Filter Filter { get; set; }
    }

    public class ClientMasterResponse
    {
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Branch { get; set; }
        public string BranchName { get; set; }
        public string FatherName { get; set; }
        public string Nationality { get; set; }
        public string PAN { get; set; }
        public string UID { get; set; }
        public string Constitution { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string DateofBirth { get; set; }
        public string Gender { get; set; }
        public string MaritalStatus { get; set; }
        public string AccountStatus { get; set; }
        public string ResidentialStatus { get; set; }
        public string ExchangeSegment { get; set; }
        public string FATCAStatus { get; set; }
        public string FATCADeclaration { get; set; }
        public string FATCADueDiligence { get; set; }
        public string FATCACountry { get; set; }
        public string FATCATaxIdentify { get; set; }
        public string FATCATypeIdentify { get; set; }
        public string CKYCStatus { get; set; }
        public string CKYCReffNo { get; set; }
        public string CKYCRespType { get; set; }
        public string CKYCNumber { get; set; }
        public string CKYCDate { get; set; }
        public string KRAStatus { get; set; }
        public string KRAAddressUpdate { get; set; }
        public string BankName { get; set; }
        public string BankIFSC { get; set; }
        public string BankAccNo { get; set; }
        public string BankMICR { get; set; }
        public string BankAccType { get; set; }
        public string CorrAddress1 { get; set; }
        public string CorrAddress2 { get; set; }
        public string CorrAddress3 { get; set; }
        public string CorrCity { get; set; }
        public string CorrState { get; set; }
        public string CorrCountry { get; set; }
        public string CorrPincode { get; set; }
        public string PerAddress1 { get; set; }
        public string PerAddress2 { get; set; }
        public string PerAddress3 { get; set; }
        public string PerCity { get; set; }
        public string PerState { get; set; }
        public string PerCountry { get; set; }
        public string PerPincode { get; set; }
        public string NetWorth { get; set; }
        public string NetworthDate { get; set; }
        public string GrossAnnualIncome { get; set; }
        public string GrossAnnualIncomeValue { get; set; }
        public string GrossAnnualIncomeDate { get; set; }
        public string RM { get; set; }
        public string DPID { get; set; }
        public string DPAcno { get; set; }
        public string DPType { get; set; }
        public string Occupation { get; set; }
        public string NomFirstName { get; set; }
        public string NomAddress1 { get; set; }
        public string NomAddress2 { get; set; }
        public string NomAddressCity { get; set; }
        public string NomAddressState { get; set; }
        public string NomAddressPin { get; set; }
        public string NomineePAN { get; set; }
        public string NomineeDOB { get; set; }
        public string NomGuardianFirstName { get; set; }
        public string NomGuardianAddress1 { get; set; }
        public string NomGuardianAddress2 { get; set; }
        public string NomGuardianAddressCity { get; set; }
        public string NomGuardianAddressState { get; set; }
        public string NomGuardianAddressPin { get; set; }
        public string NomGuardianPAN { get; set; }
        public string LastTradedDate { get; set; }
        public List<BrokerageSchemes> BrokerageScheme { get; set; }
    }

    public class BrokerageSchemes
    {
        public string ExchSeg { get; set; }
        public string RegDate { get; set; }
        public string Scheme { get; set; }
        public string CEScd { get; set; }
    }

    public class DynamicSegments
    {
        public string Segment { get; set; }
        public string ce_companycode { get; set; }
        public string ExchVal { get; set; }
    }
}
