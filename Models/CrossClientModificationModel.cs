using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class CrossClientModification
    {
        public string BOID { get; set; }
        public string RECEIPT_DATE_TIME { get; set; }
        public string REFERENCENO { get; set; }
        public Address CORRESPONDENCEADDRESS { get; set; }
        public Address PERMANENTADDRESS { get; set; }
        public Email EMAIL { get; set; }
        public Mobile MOBILE { get; set; }
        public DividendBankDetail DIVIDENDBANKDETAIL { get; set; }
        public OtherDetails OTHERDETAILS { get; set; }
        public List<NomineeDetails> NOMINEEGUARDIAN { get; set; }
    }

    public class Address
    {
        public string ADDRESS1 { get; set; }
        public string ADDRESS2 { get; set; }
        public string ADDRESS3 { get; set; }
        public string PIN { get; set; }
        public string CITY { get; set; }
        public string STATE { get; set; }
        public string COUNTRY { get; set; }
    }

    public class Email
    {
        public string EMAIL { get; set; }
        public string SECONDARY_EMAIL { get; set; }
        public string SECONDHOLDER_EMAIL { get; set; }
        public string THIRDHOLDER_EMAIL { get; set; }
    }

    public class Mobile
    {
        public string PRIMARY_MOBILE_ISD { get; set; }
        public string PRIMARY_MOBILE_NO { get; set; }
        public string SMART_REGISTRATION_INDICATOR { get; set; }
        public string SECONDARY_ISD { get; set; }
        public string SECONDARY_TEL_NO { get; set; }
        public string SECONDHOLDER_MOBILE_ISD { get; set; }
        public string SECONDHOLDER_MOBILE_NO { get; set; }
        public string THIRDHOLDER_MOBILE_ISD { get; set; }
        public string THIRDHOLDER_MOBILE_NO { get; set; }
    }

    public class OtherDetails
    {
        public string ANNUAL_INCOME { get; set; }
    }

    public class DividendBankDetail
    {
        public string BANK_CODE { get; set; }
        public string BANK_IFSCCODE { get; set; }
        public string BANK_AC_TYPE { get; set; }
        public string BANK_AC_NO { get; set; }
        public string BANK_CURRENCY { get; set; }
        public string BANK_NAME { get; set; }
        public string ECS_MANDATE { get; set; }
    }

    public class NomineeDetails
    {
        public string PURPOSE_CODE { get; set; }
        public string NOMINEE_SERIAL_NO { get; set; }
        public string NAME { get; set; }
        public string MIDDLE_NAME { get; set; }
        public string LAST_NAME { get; set; }
        public string TITLE { get; set; }
        public string SUFFIX { get; set; }
        public string FATHER_OR_HUSBAND_NAME { get; set; }
        public string ADDRESS1 { get; set; }
        public string ADDRESS2 { get; set; }
        public string ADDRESS3 { get; set; }
        public string PIN { get; set; }
        public string CITY { get; set; }
        public string STATE { get; set; }
        public string COUNTRY { get; set; }
        public string UID { get; set; }
        public string UID_VERIFY_FLAG { get; set; }
        public string PAN_NO { get; set; }
        public string MOBILE_ISD_CODE { get; set; }
        public string MOBILE_NO { get; set; }
        public string RELATIONSHIP { get; set; }
        public string DATE_OF_BIRTH { get; set; }
        public string RESIDUAL_SECURITIES { get; set; }
        public string SHARE_PERCENTAGE { get; set; }
        public string EMAIL { get; set; }
    }
}
