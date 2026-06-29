using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Web.Mvc;

namespace TradeWeb.API.Models
{
    #region AccountClosureRequest
    public class AccountClosureRequest
    {
        public string ApplicationNumber { get; set; }
        public string ClientCode { get; set; }
        public string Date { get; set; }
        public string DpId { get; set; }
        public string ClientId { get; set; }
        public string FirstHolder { get; set; }
        public string SecondHolder { get; set; }
        public string ThirdHolder { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Pincode { get; set; }
        public string ReasonClosingAccount { get; set; }
        public string RemainingAmount { get; set; }
        public string ClosureInitiatedBy { get; set; }
        public string ClosureAccountType { get; set; }
        public CompanyDetail CompanyInfo { get; set; }
        public TransferAccountDetail TransferToOther { get; set; }
        public SignerDetail SignerDetail { get; set; }
    }

    public class CompanyDetail
    {
        public string CompanyName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Pincode { get; set; }
    }

    public class TransferAccountDetail
    {
        public string DpId { get; set; }
        public string ClientId { get; set; }
    }

    public class SignerDetail
    {
        public string SignerEmail { get; set; }
        public string SignerName { get; set; }
        public string ReasonForSigningDocument { get; set; }
    }
    #endregion

    #region AccountValidationViewModel
    public class AccountValidationViewModel
    {
        //[Required]
        //public string Name { get; set; }
        [Required]
        public string IfscCode { get; set; }
        [Required]
        public string AccountNumber { get; set; }
    }
    #endregion

    #region AdharDocumentModel
    public class AdharDocumentModel
    {
        public string Name { get; set; }
        public string Gender { get; set; }
        public string DOB { get; set; }
        public string Co { get; set; }
        public string AdharNo { get; set; }
        public string PinCode { get; set; }
        public string State { get; set; }
        public string Street { get; set; }
        public string Country { get; set; }
        public string House { get; set; }
        public string Dist { get; set; }
        public string Loc { get; set; }
        public string Vtc { get; set; }
        public string ImageFile { get; set; }
    }

    public class DocumentRequest
    {
        public IFormFile AdharPdf { get; set; }
        public IFormFile PanPdf { get; set; }
        public IFormFile CancelledChequeImage { get; set; }

        [AllowHtml]
        public string AdharXml { get; set; }
    }

    public class DigilockerDocumentRequest
    {
        public EsignInputModel EsignInputModel { get; set; }
        public string signerEmail { get; set; }
        public string signerName { get; set; }
        public string reasonForSigning { get; set; }

        public string AdharPdf { get; set; }
        public string PanPdf { get; set; }
        public string CancelledChequeImage { get; set; }

        [AllowHtml]
        public string AdharXml { get; set; }

    }
    #endregion

    #region ClientModifyModel
    public class ClientModifyModel
    {
        public decimal SrNo { get; set; }
        public string ClientCode { get; set; }
        public string Field { get; set; }
        public string Description { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string Filler1 { get; set; }
        public decimal NFiller1 { get; set; }
    }
    #endregion

    #region DigilockerResponse
    public class DigilockerResponse
    {
        public string Code { get; set; }
        public string State { get; set; }
    }
    #endregion

    #region DigioSignerModel
    public class DigioSignerModel
    {
        public string signerEmail { get; set; }
        public string signerName { get; set; }
        public string reasonForSigning { get; set; }
        public string file { get; set; }
        public string fileName { get; set; }
    }
    public class DigioSignResponse
    {
        public bool status { get; set; }
        public int statusCode { get; set; }
        public string message { get; set; }
        public dynamic data { get; set; }
    }
    public class SetuEsignModel
    {
        public IFormFile document { get; set; }
        //public string document { get; set; }
        public string name { get; set; }
        public string password { get; set; }
        public string returnUrl { get; set; }

    }


    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class EsignRequestModel
    {
        public string documentId { get; set; }
        public string redirectUrl { get; set; }
        public List<Signer> signers { get; set; }
    }

    public class Signature
    {
        public int height { get; set; }
        public List<string> onPages { get; set; }
        public string position { get; set; }
        public int width { get; set; }
    }

    public class Signer
    {
        public string identifier { get; set; }
        public string displayName { get; set; }
        public string birthYear { get; set; }
        public Signature signature { get; set; }
    }
    #endregion

    #region Pdf_RegistrationModel
    public class Pdf_RegistrationModel
    {
        public string CompanyLogo { get; set; }
        public string CompanyName { get; set; }
        public string ClientImage { get; set; }
        public string ApplicationType { get; set; }
        public string KycNumber { get; set; }
        public string KycMode { get; set; }
        public NameDetail UserName { get; set; }
        public NameDetail MaidenName { get; set; }
        public NameDetail FatherName { get; set; }
        public NameDetail MotherName { get; set; }
        public FormDateFormat BirthDate { get; set; }
        public string UID { get; set; }
        public string PAN_Number { get; set; }
        public string Gender { get; set; }
        public string MaritalStatus { get; set; }
        public string Nationality { get; set; }
        public string ResidentialStatus { get; set; }
        public AddressReKyc ProofOfAddress { get; set; }
        public IdentityDetail IdentityDetail { get; set; }
    }

    public class OutputParamSP
    {
        public string ParamValue { get; set; }
        public SqlDbType DbType { get; set; }
        public int Length { get; set; }
    }

    public class OutputValueReturnSP
    {
        public string errorFlag { get; set; }
        public string errorMessage { get; set; }
        public object jsonOutput { get; set; }
    }

    public class AddressReKyc
    {
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public string Line3 { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string PinCode { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string AddressType { get; set; }//residencial/business/offic
        public string AddressAttachmentType { get; set; }
    }

    public class IdentityDetail
    {
        public string DocType { get; set; }
        public string IdentityNumber { get; set; }
        public FormDateFormat IdentityExpiryDate { get; set; }
    }

    public class NameDetail
    {
        public string Prefix { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
    }

    public class FormDateFormat
    {
        public string Day { get; set; }
        public string Month { get; set; }
        public string Year { get; set; }
    }

    public class Pdf_ModificationModel
    {
        public string KycNumber { get; set; }
        public string KycMode { get; set; }
        public string CompanyLogo { get; set; }
        public string CompanyName { get; set; }
        public string CompanyFullAddress { get; set; }
        public string ClientCode { get; set; }
        public string DPID { get; set; }
        public string DPID8 { get; set; }
        public string ClientId8 { get; set; }
        public string ClientImage { get; set; }
        public string ClientSignature { get; set; }
        public string Prefix { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string DobDay { get; set; }
        public string DobMonth { get; set; }
        public string DobYear { get; set; }
        public string DOB_KRA { get; set; }
        public string CurDateKRA { get; set; }
        public string FatherName { get; set; }
        public string PAN_Number { get; set; }
        public string UID { get; set; }
        public string Gender { get; set; }
        public string MaritalStatus { get; set; }
        public string MobNo { get; set; }
        public string Mobile { get; set; }
        public string MobileOld { get; set; }
        public string Email { get; set; }
        public string EmailOld { get; set; }
        public string Income { get; set; }
        public string IncomeOld { get; set; }
        public string IncomeDate { get; set; }
        public string IncomeDateOld { get; set; }
        [DefaultValue(false)]
        public bool IsMobEmailIncomeModified { get; set; }

        public string FatherPrefix { get; set; }
        public string MotherPrefix { get; set; }
        public string MotherFname { get; set; }
        public string MaidenPrefix { get; set; }
        public string MaidenFname { get; set; }

        public string CorrAddress1 { get; set; }
        public string CorrAddress2 { get; set; }
        public string CorrAddress3 { get; set; }
        public string CorrCountry { get; set; }
        public string CorrState { get; set; }
        public string CorrCity { get; set; }
        public string CorrPincode { get; set; }

        public string CorrAddress1Old { get; set; }
        public string CorrAddress2Old { get; set; }
        public string CorrAddress3Old { get; set; }
        public string CorrCountryOld { get; set; }
        public string CorrStateOld { get; set; }
        public string CorrCityOld { get; set; }
        public string CorrPincodeOld { get; set; }

        public bool IsAddressModified { get; set; }
        public string AddressProof { get; set; }
        public string setuEsignReturnUrl { get; set; }

        public string MobileNoPrint { get; set; }
        public string EmailPrint { get; set; }
        public string PermanentAddressFlag { get; set; }


        public string PermAddress1 { get; set; }
        public string PermAddress2 { get; set; }
        public string PermAddress3 { get; set; }
        public string PermCountry { get; set; }
        public string PermState { get; set; }
        public string PermCity { get; set; }
        public string PermPincode { get; set; }
        public string PreviewFlag { get; set; }

        #region Nominee
        public string NomFullName1 { get; set; }
        public string NomAddress11 { get; set; }
        public string NomAddress21 { get; set; }
        public string NomAddress31 { get; set; }
        public string NomAddressCity1 { get; set; }
        public string NomAddressState1 { get; set; }
        public string NomAddressCountry1 { get; set; }
        public string NomPincode1 { get; set; }
        public string NomMobile1 { get; set; }
        public string NomEmail1 { get; set; }
        public string NomineePAN1 { get; set; }
        public string NomineeUID1 { get; set; }
        public string NomineeDOB1 { get; set; }
        public string NomPercentage1 { get; set; }
        public string NomRelation1 { get; set; }
        [DefaultValue(false)]
        public bool IsNomShow1 { get; set; }
        public string ResidualFlag1 { get; set; }

        public string NomFullName2 { get; set; }
        public string NomAddress12 { get; set; }
        public string NomAddress22 { get; set; }
        public string NomAddress32 { get; set; }
        public string NomAddressCity2 { get; set; }
        public string NomAddressState2 { get; set; }
        public string NomAddressCountry2 { get; set; }
        public string NomPincode2 { get; set; }
        public string NomMobile2 { get; set; }
        public string NomEmail2 { get; set; }
        public string NomineePAN2 { get; set; }
        public string NomineeUID2 { get; set; }
        public string NomineeDOB2 { get; set; }
        public string NomPercentage2 { get; set; }
        public string NomRelation2 { get; set; }
        [DefaultValue(false)]
        public bool IsNomShow2 { get; set; }
        public string ResidualFlag2 { get; set; }

        public string NomFullName3 { get; set; }
        public string NomAddress13 { get; set; }
        public string NomAddress23 { get; set; }
        public string NomAddress33 { get; set; }
        public string NomAddressCity3 { get; set; }
        public string NomAddressState3 { get; set; }
        public string NomAddressCountry3 { get; set; }
        public string NomPincode3 { get; set; }
        public string NomMobile3 { get; set; }
        public string NomEmail3 { get; set; }
        public string NomineePAN3 { get; set; }
        public string NomineeUID3 { get; set; }
        public string NomineeDOB3 { get; set; }
        public string NomPercentage3 { get; set; }
        public string NomRelation3 { get; set; }
        [DefaultValue(false)]
        public bool IsNomShow3 { get; set; }
        public string ResidualFlag3 { get; set; }

        #endregion

        #region Gaurdian
        public string NomGuardianFullName1 { get; set; }
        public string NomGuardianAddress11 { get; set; }
        public string NomGuardianAddress21 { get; set; }
        public string NomGuardianAddress31 { get; set; }
        public string NomGuardianCity1 { get; set; }
        public string NomGuardianState1 { get; set; }
        public string NomGuardianPincode1 { get; set; }
        public string NomGuardianMobile1 { get; set; }
        public string NomGuardianEmail1 { get; set; }
        public string NomGuardianRelation1 { get; set; }
        public string NomGuardianPAN1 { get; set; }
        public string NomGuardianUID1 { get; set; }
        public string NomGuardianDob1 { get; set; }
        [DefaultValue(false)]
        public bool IsGaurdnShow1 { get; set; }

        public string NomGuardianFullName2 { get; set; }
        public string NomGuardianAddress12 { get; set; }
        public string NomGuardianAddress22 { get; set; }
        public string NomGuardianAddress32 { get; set; }
        public string NomGuardianCity2 { get; set; }
        public string NomGuardianState2 { get; set; }
        public string NomGuardianPincode2 { get; set; }
        public string NomGuardianMobile2 { get; set; }
        public string NomGuardianEmail2 { get; set; }
        public string NomGuardianRelation2 { get; set; }
        public string NomGuardianPAN2 { get; set; }
        public string NomGuardianUID2 { get; set; }
        public string NomGuardianDob2 { get; set; }
        [DefaultValue(false)]
        public bool IsGaurdnShow2 { get; set; }

        public string NomGuardianFullName3 { get; set; }
        public string NomGuardianAddress13 { get; set; }
        public string NomGuardianAddress23 { get; set; }
        public string NomGuardianAddress33 { get; set; }
        public string NomGuardianCity3 { get; set; }
        public string NomGuardianState3 { get; set; }
        public string NomGuardianPincode3 { get; set; }
        public string NomGuardianMobile3 { get; set; }
        public string NomGuardianEmail3 { get; set; }
        public string NomGuardianRelation3 { get; set; }
        public string NomGuardianPAN3 { get; set; }
        public string NomGuardianUID3 { get; set; }
        public string NomGuardianDob3 { get; set; }
        [DefaultValue(false)]
        public bool IsGaurdnShow3 { get; set; }
        #endregion

        #region Bank
        public string BankAccNo1 { get; set; }
        public string BankIFSC1 { get; set; }
        public string BankMICR1 { get; set; }
        [DefaultValue(false)]
        public bool IsDefault1 { get; set; }
        public string AccountType1 { get; set; }
        [DefaultValue(false)]
        public bool IsBankShow1 { get; set; }

        public string BankAccNo2 { get; set; }
        public string BankIFSC2 { get; set; }
        public string BankMICR2 { get; set; }
        [DefaultValue(false)]
        public bool IsDefault2 { get; set; }
        public string AccountType2 { get; set; }
        [DefaultValue(false)]
        public bool IsBankShow2 { get; set; }

        public string BankAccNo3 { get; set; }
        public string BankIFSC3 { get; set; }
        public string BankMICR3 { get; set; }
        [DefaultValue(false)]
        public bool IsDefault3 { get; set; }
        public string AccountType3 { get; set; }
        [DefaultValue(false)]
        public bool IsBankShow3 { get; set; }
        #endregion

        #region Demat
        public string DPAcNo1 { get; set; }
        public string DPID1 { get; set; }
        public string DPType1 { get; set; }
        [DefaultValue(false)]
        public bool IsDematDefault1 { get; set; }
        [DefaultValue(false)]
        public bool IsDematShow1 { get; set; }

        public string DPAcNo2 { get; set; }
        public string DPID2 { get; set; }
        public string DPType2 { get; set; }
        [DefaultValue(false)]
        public bool IsDematDefault2 { get; set; }
        [DefaultValue(false)]
        public bool IsDematShow2 { get; set; }

        public string DPAcNo3 { get; set; }
        public string DPID3 { get; set; }
        public string DPType3 { get; set; }
        [DefaultValue(false)]
        public bool IsDematDefault3 { get; set; }
        [DefaultValue(false)]
        public bool IsDematShow3 { get; set; }

        #endregion


        #region Second page
        public string KycVerfName { get; set; }
        public string KycVerfDesignation { get; set; }
        public string KycVerfEmpCode { get; set; }
        public string KycVerfBranch { get; set; }
        public string CurtDD { get; set; }
        public string CurtMM { get; set; }
        public string CurtYYYY { get; set; }
        public string KycPlace { get; set; }
        #endregion

        public string SegmentDetailsOld { get; set; }
        public string SegmentDetailsNew { get; set; }
        [DefaultValue(false)]
        public bool IsSegmentModified { get; set; }
    }

    public class EsignInputModel
    {
        public string ApplicationType { get; set; }
        public string KycNumber { get; set; }
        public string KycMode { get; set; }
        public string ClientsNamePrefix { get; set; }
        public string ClientsFirstName { get; set; }
        public string ClientsMiddleName { get; set; }
        public string ClientLastName { get; set; }
        public string MaidenNamePrefix { get; set; }
        public string MaidenName { get; set; }
        public string FathersNamePrefix { get; set; }
        public string FathersFullName { get; set; }
        public string MothersNamePrefix { get; set; }
        public string MothersFullName { get; set; }
        public string DateOfBirth { get; set; }
        public string PanNumber { get; set; }
        public string Gender { get; set; }
        public string MaritalStatus { get; set; }
        public string Nationality { get; set; }
        public string ResidentialStatus { get; set; }
        public string ProofOfIdentity { get; set; }
        public string ProofOfIdentityNumber { get; set; }
        public string ProofOfIdentityExpiryDate { get; set; }
        public Boolean IsCorrespondenceOrLocalAddress { get; set; }
        public string ProofOfAddress { get; set; }
        public string ProofOfAddressNumber { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string PinCode { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string AddressType { get; set; }
        public string ClientImage { get; set; }
    }

    public class ThirdPartyCallApiModel
    {
        public string paramName { get; set; }
        public string vName { get; set; }
        public object jsonRequest { get; set; }
    }

    public class SignleObjectParamModel
    {
        public object request { get; set; }
    }


    public class FillPdfPersonalInfo
    {
        public string KycNumber { get; set; }
        public string KycMode { get; set; }
        public string Prefix { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string DobDay { get; set; }
        public string DobMonth { get; set; }
        public string DobYear { get; set; }
        public string FatherName { get; set; }
        public string PAN_Number { get; set; }
        public string UID { get; set; }
        public string Gender { get; set; }
        public string MaritalStatus { get; set; }
        public string MobNo { get; set; }
        public string Email { get; set; }

        public string CorrAddress1 { get; set; }
        public string CorrAddress2 { get; set; }
        public string CorrAddress3 { get; set; }
        public string CorrCountry { get; set; }
        public string CorrState { get; set; }
        public string CorrCity { get; set; }
        public string CorrPincode { get; set; }
        public string PermanentAddressFlag { get; set; }
        public bool IsAddressModified { get; set; }
        public string AddressProof { get; set; }
        public string setuEsignReturnUrl { get; set; }
        public string PreviewFlag { get; set; }
    }
    #endregion

    #region PersonalAttachmentDto
    public class PersonalAttachmentDto
    {
        public long SrNo { get; set; }
        public string ClientCode { get; set; }
        public string Date { get; set; }
        public string FileName { get; set; }
        public string Proof { get; set; }
        public string Field { get; set; }
        public string MkrDate { get; set; }
        public string MkrTime { get; set; }
        public string Nfiller { get; set; }
    }
    #endregion

    #region RekycApprovePost
    public class RekycApprovePost
    {
        [Required(ErrorMessage = "Reference number should not be blank")]
        public string RefNo { get; set; }

        [Required(ErrorMessage = "Client code should not be blank")]
        public string ClientCode { get; set; }
        public string Reason { get; set; }
        [Required(ErrorMessage = "Status code should not be blank")]
        public string Status { get; set; }
    }
    #endregion

    #region RekycCheckerDto
    public class RekycCheckerDto
    {
        public decimal SrNo { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string Field { get; set; }
        public string Description { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public decimal NFiller1 { get; set; }
        public decimal NFiller3 { get; set; }
        public string Filler3 { get; set; }
    }

    public class RekycAttachmentTable
    {
        public string fileName { get; set; }
        public string fieldName { get; set; }
        public object attachment { get; set; }
        public decimal NFiller1 { get; set; }

    }
    #endregion

    #region RekycCheckerModel
    public class RekycCheckerModel
    {
        public RekycPersonal Personal_Detail { get; set; }
        public List<RekycNominee> NomineeDetails { get; set; }
        public string NomineeOpt { get; set; }
        public List<RekycBank> BankDetails { get; set; }
        public List<RekycDemat> Demat_Detail { get; set; }
        public RekycCheckerSegmentOld Segment_Detail_Old { get; set; }
        public RekycCheckerSegmentNew Segment_Detail_New { get; set; }
    }

    public class RekycPersonal
    {
        public string Request_Ref_No { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string CorrAddressNew1 { get; set; }
        public string CorrAddressOld1 { get; set; }
        public string CorrAddressNew2 { get; set; }
        public string CorrAddressOld2 { get; set; }
        public string CorrAddressNew3 { get; set; }
        public string CorrAddressOld3 { get; set; }
        public string CorrCountryNew { get; set; }
        public string CorrCountryOld { get; set; }
        public string CorrStateNew { get; set; }
        public string CorrStateOld { get; set; }
        public string CorrCityNew { get; set; }
        public string CorrCityOld { get; set; }
        public string CorrPincodeNew { get; set; }
        public string CorrPincodeOld { get; set; }
        public string EmailNew { get; set; }
        public string EmailOld { get; set; }
        public string EmailRelation { get; set; }
        public string MobileNew { get; set; }
        public string MobileOld { get; set; }
        public string MobileRelation { get; set; }
        public string IncomeNew { get; set; }
        public string IncomeOld { get; set; }
        public string IncomeDateNew { get; set; }
        public string IncomeDateOld { get; set; }
        public string AddressAttachment { get; set; }
        public string AddressAttachmentType { get; set; } //UID/DL/VoterId/Passport
        public string IncomeAttachment { get; set; }
    }

    public class RekycNominee
    {
        public bool IsNomineeDeleted { get; set; }
        public int NomineeID { get; set; }
        public string NomFirstNameNew { get; set; }
        public string NomFirstNameOld { get; set; }
        public string NomMiddleNameNew { get; set; }
        public string NomMiddleNameOld { get; set; }
        public string NomLastNameNew { get; set; }
        public string NomLastNameOld { get; set; }
        public string NomAddressNew1 { get; set; }
        public string NomAddressOld1 { get; set; }
        public string NomAddressNew2 { get; set; }
        public string NomAddressOld2 { get; set; }
        public string NomAddressNew3 { get; set; }
        public string NomAddressOld3 { get; set; }
        public string NomAddressCountryNew { get; set; }
        public string NomAddressCountryOld { get; set; }
        public string NomAddressStateNew { get; set; }
        public string NomAddressStateOld { get; set; }
        public string NomAddressCityNew { get; set; }
        public string NomAddressCityOld { get; set; }
        public string NomDOBNew { get; set; }
        public string NomDOBOld { get; set; }
        public string NomMobileNew { get; set; }
        public string NomMobileOld { get; set; }
        public string NomPAN_New { get; set; }
        public string NomPAN_Old { get; set; }
        public string NomUID_New { get; set; }
        public string NomUID_Old { get; set; }
        public string NomRelationNew { get; set; }
        public string NomRelationOld { get; set; }
        public string NomSharePercentageNew { get; set; }
        public string NomSharePercentageOld { get; set; }
        public string NomAttachment { get; set; }
        public RekycGuardian GuardianDetails { get; set; }
    }

    public class RekycGuardian
    {
        public int NomGuardianld { get; set; }
        public int Nomineeld { get; set; }
        public string NomGuardianFirstNameNew { get; set; }
        public string NomGuardianFirstNameOld { get; set; }
        public string NomGuardianMiddleNameNew { get; set; }
        public string NomGuardianMiddleNameOld { get; set; }
        public string NomGuardianLastNameNew { get; set; }
        public string NomGuardianLastNameOld { get; set; }
        public string NomGuardianAddress1New { get; set; }
        public string NomGuardianAddress1Old { get; set; }
        public string NomGuardianAddress2New { get; set; }
        public string NomGuardianAddress2Old { get; set; }
        public string NomGuardianAddress3New { get; set; }
        public string NomGuardianAddress3Old { get; set; }
        public string NomGuardianCityNew { get; set; }
        public string NomGuardianCityOld { get; set; }
        public string NomGuardianStateNew { get; set; }
        public string NomGuardianStateOld { get; set; }
        public string NomGuardianPinCodeNew { get; set; }
        public string NomGuardianPinCodeOld { get; set; }
        public string NomGuardianMobileNew { get; set; }
        public string NomGuardianMobileOld { get; set; }
        public string NomGuardianPAN_UIDNew { get; set; }
        public string NomGuardianPAN_UIDOld { get; set; }
        public string NomGuardianEmailNew { get; set; }
        public string NomGuardianEmailOld { get; set; }
        public string NomGuardianDobNew { get; set; }
        public string NomGuardianDobOld { get; set; }
        public string NomGuardianRelationNew { get; set; }
        public string NomGuardianRelationOld { get; set; }
    }

    public class RekycBank
    {
        public bool IsBankDeleted { get; set; }
        public int BankID { get; set; }
        public string BankIFSCNew { get; set; }
        public string BankIFSCOld { get; set; }
        public string BankAccNoNew { get; set; }
        public string BankAccNoOld { get; set; }
        public string BankNameNew { get; set; }
        public string BankNameOld { get; set; }
        public string BankMICRNew { get; set; }
        public string BankMICROld { get; set; }
        public bool IsDefaultNew { get; set; }
        public bool IsDefaultOld { get; set; }
        public string BankCCAttachment { get; set; }
    }

    public class RekycDemat
    {
        public bool IsDematDeleted { get; set; }
        public int DematID { get; set; }
        //public string DpTypeOld { get; set; }
        public string DpIDNew { get; set; }
        public string DpIDOld { get; set; }
        public string DpAccNoNew { get; set; }
        public string DpAccNoOld { get; set; }
        public string DematAccountType { get; set; }
        public string DematAccountTypeOld { get; set; }
        public bool IsDefaultNew { get; set; }
        public bool IsDefaultOld { get; set; }
        public string DPAttachment { get; set; }
    }

    public class RekycCheckerSegmentOld
    {
        public List<string> CapitalMarket { get; set; }
        public List<string> FO { get; set; }
        public List<string> CurrencyDerivatives { get; set; }
        public List<string> Commodities { get; set; }
        public List<string> MF { get; set; }
        public List<string> Slb { get; set; }
    }

    public class RekycCheckerSegmentNew
    {
        public List<string> CapitalMarket { get; set; }
        public List<string> FO { get; set; }
        public List<string> CurrencyDerivatives { get; set; }
        public List<string> Commodities { get; set; }
        public List<string> MF { get; set; }
        public List<string> Slb { get; set; }
        public string SegmentAttachment { get; set; }
        public string SegmentAttachmentType { get; set; }

    }
    #endregion

    #region ReKycGetPostModel
    public class FinalReKycModel
    {
        public string KycNumber { get; set; }
        public string KycMode { get; set; }
        public string NomineeOpt { get; set; }
        [DefaultValue(false)]
        public bool IsNomineeModified { get; set; }
        public string ReqIdSetu { get; set; }
        public string ReturnUrlEsignSetu { get; set; }
        public string ApiCallAddress { get; set; }
        public string ApiCallBank { get; set; }
        public string CheckerMessage { get; set; }
        public string EsignPdf { get; set; }
        public Personal PersonalDetails { get; set; }
        public List<Nominee> NomineeDetails { get; set; }
        public List<Bank> BankDetails { get; set; }
        public List<Demat> DematDetails { get; set; }
        public Segment SegmentDetails { get; set; }
        public Dormant DormantDetails { get; set; }
    }

    public class Personal
    {
        public string ClientCode { get; set; }
        public string ClientsNamePrefix { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string CorrAddress1 { get; set; }
        public string CorrAddress2 { get; set; }
        public string CorrAddress3 { get; set; }
        public string CorrCountry { get; set; }
        public string CorrState { get; set; }
        public string CorrCity { get; set; }
        public string CorrPincode { get; set; }
        public string AddressType { get; set; } //residencial/business/offic
        public string Email { get; set; }
        public string EmailRelation { get; set; }
        public string Mobile { get; set; }
        public string MobileRelation { get; set; }
        public string FatherName { get; set; }
        public string UID { get; set; }
        public string PAN { get; set; }
        public string Gender { get; set; }
        public string DateOfBirth { get; set; }
        public string MaritalStatus { get; set; }
        public string Income { get; set; }
        public string IncomeValue { get; set; }
        public string IncomeDate { get; set; }
        public string AddressAttachment { get; set; }
        public string AddressAttachmentType { get; set; } //UID/DL/VoterId/Passport
        public string IncomeAttachment { get; set; }
        //public PersonalAttachment PersonalDetailsAttachment { get; set; }
        public string SignAttachment { get; set; }
    }

    public class PersonalAttachment
    {

    }

    public class Nominee
    {
        public string NomSerial { get; set; }
        public string NomFirstName { get; set; }
        public string NomMiddleName { get; set; }
        public string NomLastName { get; set; }
        public string NomAddress1 { get; set; }
        public string NomAddress2 { get; set; }
        public string NomAddress3 { get; set; }
        public string NomAddressCity { get; set; }
        public string NomAddressState { get; set; }
        public string NomAddressCountry { get; set; }
        public string NomPincode { get; set; }
        public string NomineePAN { get; set; }
        public string NomineeUID { get; set; }
        public string NomineeDOB { get; set; }
        public string NomMobile { get; set; }
        public string NomPercentage { get; set; }
        public string NomRelation { get; set; }
        public int NomineeId { get; set; }
        public bool IsNomDeleted { get; set; }
        public Guardian GuardianDetails { get; set; }
        public string NomineeAttachment { get; set; }
    }

    public class Guardian
    {
        public string NomGuardianSerial { get; set; }
        public string NomGuardianFirstName { get; set; }
        public string NomGuardianMiddleName { get; set; }
        public string NomGuardianLastName { get; set; }
        public string NomGuardianAddress1 { get; set; }
        public string NomGuardianAddress2 { get; set; }
        public string NomGuardianAddress3 { get; set; }
        public string NomGuardianCity { get; set; }
        public string NomGuardianState { get; set; }
        public string NomGuardianPincode { get; set; }
        public string NomGuardianMobile { get; set; }
        public string NomGuardianEmail { get; set; }
        public string NomGuardianRelation { get; set; }
        public string NomGuardianPAN { get; set; }
        public string NomGuardianUID { get; set; }
        public string NomGuardianDob { get; set; }
        public int NomGuardianId { get; set; }
        public int NomineeId { get; set; }
    }

    public class Bank
    {
        public string BankAccNo { get; set; }
        public string BankName { get; set; }
        public string BankIFSC { get; set; }
        public string BankMICR { get; set; }
        public bool IsDefault { get; set; }
        public string AccountType { get; set; }
        public int BankID { get; set; }
        public bool IsBankDeleted { get; set; }
        public string BankCCAttachment { get; set; }

    }

    public class Demat
    {
        public int DematId { get; set; }
        public string DPAcNo { get; set; }
        public string DPID { get; set; }
        public string DematAccountType { get; set; }
        public bool IsDefault { get; set; } = false;
        public bool IsDematDeleted { get; set; }
        public string DPAttachment { get; set; }
    }

    public class Segment
    {
        public List<string> CapitalMarket { get; set; }
        public List<string> FO { get; set; }
        public List<string> CurrencyDerivatives { get; set; }
        public List<string> Commodities { get; set; }
        public List<string> Slb { get; set; }
        public List<string> MF { get; set; }
        public string SegmentAttachment { get; set; }
        public string SegmentAttachmentType { get; set; }
    }

    public class Dormant
    {
        public string DormantStatus { get; set; }
        public bool DormantIsActive { get; set; }
    }


    public class ttt
    {
        public List<object> RekycObjData { get; set; }
        public List<object> PersonalDetails { get; set; }
        public List<object> NomineeDetails { get; set; }
        public List<object> BankDetails { get; set; }
        public List<object> DematDetails { get; set; }
        public List<object> DormantDetails { get; set; }
        public Dictionary<string, object> SegmentDetails { get; set; }
        public Dictionary<string, object> ReKycAttachments { get; set; }

    }

    public class RekycJsonObjModel
    {
        public object model { get; set; } = null;
    }

    public class RekycGetJsonModel
    {
        public string errorMsg { get; set; } = "";
        public object rekycJson { get; set; }
    }

    public class JsonBooleanConverter : JsonConverter
    {
        public override bool CanWrite { get { return false; } }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var value = reader.Value.ToString().ToLower().Trim();
            switch (value)
            {
                case "true":
                case "Y":
                case "True":
                case "TRUE":
                    return true;
            }
            return false;
        }

        public override bool CanConvert(Type objectType)
        {
            if (objectType == typeof(Boolean))
            {
                return true;
            }
            return false;
        }
    }
    #endregion

    #region RekycViewAll
    public class RekycViewAll
    {
        public string Request_Ref_No { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string RekycDate { get; set; }
        public string PersonalDetails { get; set; }
        public string NomineeDetails { get; set; }
        public string BankDetails { get; set; }
        public string DematDetails { get; set; }
        public string SegmentDetails { get; set; }
        //public string EmailAddress { get; set; }
        //public string FullName { get; set; }
        //public string FullAddress { get; set; }
        //public string Pan_UID { get; set; }
        //public string DateOfBirth { get; set; }
        //public string FatherName { get; set; }
        //public string Mobile { get; set; }
        //public string Email { get; set; }
        //public string NomFullName { get; set; }
        //public string NomAddress { get; set; }
        //public string NomRelation { get; set; }
        //public string NomPAN_UID { get; set; }
        //public string BankAccNo { get; set; }
        //public string BankIFSC { get; set; }
        //public string BankMICR { get; set; }
        //public string DpAcno { get; set; }
        //public string DpID { get; set; }
        //public string Segment { get; set; }
        //public string Pending_Days { get; set; }
        //public string AuthStatus { get; set; }
        //public string EsignStatus { get; set; }
        //public string EsignType { get; set; }
    }
    #endregion

    #region RekycViewAllDto
    public class RekycViewAllDto
    {
        public decimal SrNo { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string EmailAddress { get; set; }
        public string Field { get; set; }
        public string Description { get; set; }
        public string Value { get; set; }
        public string Date { get; set; }
    }
    #endregion

    #region ReKycViewData
    public class ReKycViewData
    {
        public List<ReKycPersonal> Personal_Details { get; set; }
        public List<ReKycNominee> NomineeDetails { get; set; }
        public List<ReKycBank> BankDetails { get; set; }
        public List<ReKycDemat> Demat_Detail { get; set; }
        public string NomineeOpt { get; set; }

    }

    public class ReKycPersonal
    {
        public string Request_Ref_No { get; set; }
        public string ClientName { get; set; }
        public string ClientCode { get; set; }
        public string FirstNameNew { get; set; }
        public string FirstNameOld { get; set; }
        public string MiddleNameNew { get; set; }
        public string MiddleNameOld { get; set; }
        public string LastNameNew { get; set; }
        public string LastNameOld { get; set; }
        public string Father_HusbandNameNew { get; set; }
        public string Father_HusbandNameOld { get; set; }
        public string CorrAddressNew1 { get; set; }
        public string CorrAddressOld1 { get; set; }
        public string CorrAddressNew2 { get; set; }
        public string CorrAddressOld2 { get; set; }
        public string CorrAddressNew3 { get; set; }
        public string CorrAddressOld3 { get; set; }
        public string CorrCountryNew { get; set; }
        public string CorrCountryOld { get; set; }
        public string CorrStateNew { get; set; }
        public string CorrStateOld { get; set; }
        public string CorrCityNew { get; set; }
        public string CorrCityOld { get; set; }
        public string EmailNew { get; set; }
        public string EmailOld { get; set; }
        public string MobileNew { get; set; }
        public string MobileOld { get; set; }
        public string UIDNew { get; set; }
        public string UIDOld { get; set; }
        public string PanNew { get; set; }
        public string PanOld { get; set; }
        public PersonalAttachment PersonalDetailsAttachment { get; set; }
    }

    public class ReKycNominee
    {
        public string Request_Ref_No { get; set; }
        public string ClientName { get; set; }
        public string ClientCode { get; set; }
        public bool IsNomineeDeleted { get; set; }
        public int NomineeId { get; set; }
        public string NomFirstNameNew { get; set; }
        public string NomFirstNameOld { get; set; }
        public string NomMiddleNameNew { get; set; }
        public string NomMiddleNameOld { get; set; }
        public string NomLastNameNew { get; set; }
        public string NomAddressNew1 { get; set; }
        public string NomLastNameOld { get; set; }
        public string NomAddressNew2 { get; set; }
        public string NomAddressOld2 { get; set; }
        public string NomAddressOld1 { get; set; }
        public string NomAddressCountryNew { get; set; }
        public string NomAddressCountryOld { get; set; }
        public string NomAddressStateNew { get; set; }
        public string NomAddressStateOld { get; set; }
        public string NomAddressCityNew { get; set; }
        public string NomAddressCityOld { get; set; }
        public string NomineeDOBNew { get; set; }
        public string NomineeDOBOld { get; set; }
        public string NomineePAN_UIDNew { get; set; }
        public string NomineePAN_UIDOld { get; set; }
        public string NomineeRelationNew { get; set; }
        public string NomineeRelationOld { get; set; }
        public string NomineeSharePercentageNew { get; set; }
        public string NomineeSharePercentageOld { get; set; }
        public ReKycGuardian GuardianDetails { get; set; }
    }

    public class ReKycGuardian
    {
        public int NomGuardianId { get; set; }
        public int NomineeId { get; set; }
        public string NomGuardianFirstNameNew { get; set; }
        public string NomGuardianFirstNameOld { get; set; }
        public string NomGuardianMiddleNameNew { get; set; }
        public string NomGuardianMiddleNameOld { get; set; }
        public string NomGuardianLastNameNew { get; set; }
        public string NomGuardianLastNameOld { get; set; }
        public string NomGuardianMobileNew { get; set; }
        public string NomGuardianMobileOld { get; set; }
        public string NomGuardianPAN_UIDNew { get; set; }
        public string NomGuardianPAN_UIDOld { get; set; }
        public string NomGuardianEmailNew { get; set; }
        public string NomGuardianEmailOld { get; set; }
        public string NomGuardianDobNew { get; set; }
        public string NomGuardianDobOld { get; set; }
        public string NomGuardianRelationNew { get; set; }
        public string NomGuardianRelationOld { get; set; }

    }

    public class ReKycBank
    {
        public string Request_Ref_No { get; set; }
        public string ClientName { get; set; }
        public string ClientCode { get; set; }
        public string IsBankDeleted { get; set; }
        public int BankId { get; set; }
        public string BankIFSCNew { get; set; }
        public string BankIFSCOld { get; set; }
        public string BankAccNoNew { get; set; }
        public string BankAccNoOld { get; set; }
        public string BankNameNew { get; set; }
        public string BankNameOld { get; set; }
        public string BankMICRNew { get; set; }
        public string BankMICROld { get; set; }
        public string BankCancelCheque { get; set; }
    }

    public class ReKycDemat
    {
        public string Request_Ref_No { get; set; }
        public string ClientName { get; set; }
        public string ClientCode { get; set; }
        public string IsDematDeleted { get; set; }
        public int DematId { get; set; }
        public string DPTypeNew { get; set; }
        public string DPTypeOld { get; set; }
        public string DPIdNew { get; set; }
        public string DPIdOld { get; set; }
        public string DPAcnoNew { get; set; }
        public string DPAcnoOld { get; set; }
        public string DematAccountType { get; set; }
    }
    #endregion

    #region ThirdPartyModel
    public class ThirdPartyModel
    {

        public object Request { get; set; }
        public string ContentType { get; set; }
        public string APIURL { get; set; }
        public string APIType { get; set; }
        public string APICallingType { get; set; }
        public string AuthKey { get; set; }
        public List<GetInputModel> FieldsForGET { get; set; }
        public List<GetInputModel> FieldsForHeader { get; set; }
        public string OutputType { get; set; }
        public string ProjectName { get; set; }
        public string APIVendorName { get; set; }
    }

    public class ThirdPartyServiceMultiPartModel
    {
        public object Request { get; set; }
        public string ContentType { get; set; }
        public string APIURL { get; set; }
        public string APIType { get; set; }
        public string APICallingType { get; set; }
        public string AuthKey { get; set; }
        public List<GetInputModel> FieldsForHeader { get; set; }
        public string OutputType { get; set; }
        public string FileType { get; set; } //PDF,IMAGE
        public string File { get; set; } //It May be FilePath or BASE64
        public string FileName { get; set; }
        public string FilePassword { get; set; }
        public string FileFormat { get; set; }//It Represents File Format i.e, Path or Base64
        public string ProjectName { get; set; }
        public string APIVendorName { get; set; }
    }

    public class GetInputModel
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class AuthenticateRequest
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }

    public class User
    {
        public string Username { get; set; }
        [JsonIgnore]
        public string Password { get; set; }
    }

    public class SingleStringModel
    {
        public string? request { get; set; }

    }

    public class EncodeBase64Model
    {
        public string type { get; set; }
        public string reqString { get; set; }
        public string format { get; set; }

    }

    public class SFTP_Model
    {
        public string hostName { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public int port { get; set; }
        public string filePath { get; set; }
        public List<FileDetailsSFTP> files { get; set; }

    }
    public class FileDetailsSFTP
    {
        public string fileName { get; set; }
        public string base64 { get; set; }
    }

    public class SFTP_DownloadModel
    {
        public string hostName { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public int port { get; set; }
        public string filePath { get; set; }
        public string fileName { get; set; }

    }
    public class EncryptModel
    {
        public string PlainText { get; set; }
        public string Key { get; set; }
    }

    public class EncryptKRA_Model
    {
        public string aesKey { get; set; }
        public dynamic data { get; set; }
    }

    public class DecryptKRA_Model
    {
        public dynamic encryptedText { get; set; }
        public string aesKey { get; set; }
        public string IV { get; set; }
    }

    public class CkycDownloadFileModel
    {
        public string baseFolderName { get; set; }
        public string baseFileName { get; set; }
        public string baseFileString { get; set; }
        public List<CkycFiles> subFileDetails { get; set; }
    }
    public class CkycFiles
    {
        public string fileName { get; set; }
        public string subFolderName { get; set; }
        public string fileBase64 { get; set; }
    }

    public class HmacRequest
    {
        public string Key { get; set; }
        public string Data { get; set; }
    }

    public class SendOTP
    {
        public string userId { get; set; }
        public string Type { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string SMSText { get; set; }
        public string EmailSubject { get; set; }
        public string EmailBody { get; set; }
        public string Product { get; set; }
        public string OTPType { get; set; }
    }
    public class VerifyOTP
    {
        public string userId { get; set; }
        public string otp { get; set; }
        public string otpFor { get; set; }
        public string Product { get; set; }
        public string OTPType { get; set; }
    }

    #endregion

    #region TotalDetail
    public class TotalDetail
    {
        public string ClientCode { get; set; }
        public long PersonalDetailTotal { get; set; }
        public long NomineeDetailTotal { get; set; }
        public long BankDetailTotal { get; set; }
        public long DematDetailTotal { get; set; }
        public long SegmentDetailTotal { get; set; }
        public long CountTotal { get; set; }

    }
    #endregion

    #region TotalDetailDto
    public class TotalDetailDto
    {
        public string ClientCode { get; set; }
        public string Type { get; set; }
        public long Total { get; set; }
    }
    #endregion

    #region TplusResponse
    public class TplusResponse
    {
        public string ClientCode { get; set; }
        public string Status { get; set; }
        public string Remark { get; set; }
        public string DigioId { get; set; }
        public string PdfData { get; set; }
    }

    public class EsignResponse
    {
        public string ClientCode { get; set; }
        public string Status { get; set; }
        public string Remark { get; set; }
        public string EsignUrl { get; set; }
        public string UnsignedPdf { get; set; }
    }
    #endregion


    public class AccountClosurePDF
    {
        public string AccountType { get; set; }
        public string UserId { get; set; }
        public string FormHeading { get; set; }
        public string CompanyName { get; set; }
        public string CompanyFullAddress { get; set; }
        public string ApplicationNo { get; set; }
        public string ApplicationDate { get; set; }
        public string DPID { get; set; }
        public string NewBOID { get; set; }
        public string NewClientID { get; set; }
        public string ClientId { get; set; }
        public string FirstHolderName { get; set; }
        public string SecondHolderName { get; set; }
        public string ThirdHolderName { get; set; }
        public string HolderAddress { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PinCode { get; set; }
        public string ClosingReason { get; set; }
        public string RemainingBalance { get; set; }

    }

    public class ImportLargeFileModel
    {
        public string fileSeqNo { get; set; }
        public string batchNo { get; set; }
        public string userId { get; set; }
        public object inputJson { get; set; }
    }

}
