using System;
using System.Collections.Generic;
using System.Data;
using System.Xml;
using System.Xml.Serialization;

namespace TradeWeb.API.Models
{
    public class CommonModel
    {
        public string FunctionName { get; set; }
        public string ProjectName { get; set; }
        public string ModuleName { get; set; }
        public dynamic RequestString { get; set; }
        public string UserID { get; set; }
        public string RequestSource { get; set; }
        public string RequestUniqueID { get; set; }
    }

    public class ReKYCModel
    {
        public string FunctionName { get; set; }
        public dynamic RequestString { get; set; }
    }

    public class ReKYCRequest
    {
        public string ClientCode { get; set; }
        public string TemplateCode { get; set; }
        public string ProcessTag { get; set; }
        public string Method { get; set; }
        public dynamic JsonData { get; set; }
    }

    public class ReKYCResponse
    {
        public string ClientCode { get; set; }
        public string Status { get; set; }
        public string Remark { get; set; }
    }

    public class TradeWebDataGridRequest
    {
        public string XML { get; set; }
        public string ReportCode { get; set; }
        public string ReportCategory { get; set; }
    }

    public class TradeWebDataGridResponse
    {
        public string Message { get; set; }
        public List<DataSet> Data { get; set; }
    }

    public class EmailSmsReqModel
    {
        public string reqestName { get; set; }
        public dynamic requestObject { get; set; }
    }

    public class EstroReportModel
    {
        public string FunctionName { get; set; }
        public string XML { get; set; }
    }

    public class CommonAPIRequest
    {
        public string ProjectName { get; set; }
        public string ModuleName { get; set; }
        public string FunctionName { get; set; }
        public dynamic RequestString { get; set; }
    }

    public class CommonAPIResponse
    {
        public string ResponseFlag { get; set; }
        public string ResponseMessage { get; set; }
        public dynamic DATA { get; set; }
    }


    [XmlRoot("dsXml")]
    public class dsXml
    {
        [XmlElement("J_Ui")]
        public string J_Ui { get; set; }

        [XmlElement("Sql")]
        public string Sql { get; set; }

        [XmlAnyElement]
        public List<XmlElement> X_Filter { get; set; }

        [XmlAnyElement]
        public List<XmlElement> X_Data { get; set; }

        [XmlElement("X_GFilter")]
        public List<XmlElement> X_GFilter { get; set; }

        [XmlElement("J_Api")]
        public string J_Api { get; set; }
    }

    //public class XFilter
    //{
    //    [XmlElement("FromDate")]
    //    public string FromDate { get; set; }

    //    [XmlElement("ToDate")]
    //    public string ToDate { get; set; }

    //    [XmlElement("ClientCode")]
    //    public string ClientCode { get; set; }
    //}

    public class JUiModel
    {
        public string ActionName { get; set; }
        public string Option { get; set; }
        public int Level { get; set; }
        public string ReportDisplay { get; set; }
        public string RequestFrom { get; set; }
    }

    public class JApi
    {
        public string UserId { get; set; }
        public string UserType { get; set; }
        public string UserAccess { get; set; }
        private string _secretKey = "";

        public string SecretKey
        {
            get => _secretKey;
            set => _secretKey = value ?? "";
        }
        public int AccYear { get; set; }
        public string MyDbPrefix { get; set; }
        public long MenuCode { get; set; }
        public int ModuleID { get; set; }
        public string MyDb { get; set; }
        public string DenyRights { get; set; }
    }

    public class CrossNetCommonModel
    {
        public bool success { get; set; }
        public string message { get; set; }
        public DataSet data { get; set; }
        public List<string> datarows { get; set; }
    }

    public class CrossNetSPModel
    {
        public DataSet Data { get; set; }
        public List<string> DataRows { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class CommonXMLResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ModuleName { get; set; }
        public string FunctionName { get; set; }
        public string ReportDisplay { get; set; }
    }

    public class PDFResponse
    {
        public string PDFName { get; set; }
        public string Base64PDF { get; set; }
    }

    public class VersionReponse<T>
    {
        public bool Status { get; set; }
        public T Data { get; set; } //VersionData
    }

    public class VersionData
    {
        public string ApplicationName { get; set; }
        public string ApplicationVersion { get; set; }
        public string MobileAppVersion { get; set; }
        public string APIVersion { get; set; }
        public string SPDate { get; set; }
        public string DBDate { get; set; }
        public List<DBDetail> DBDetails { get; set; }
    }

    public class DBDetail
    {
        public string Date { get; set; }
        public string Remark { get; set; }
        public string Query { get; set; }
    }

    public class VersionInfo
    {
        public DateTime LocalSPFileDate { get; set; }
        public DateTime ParamSPFileDate { get; set; }
        public DateTime WebSPFileDate { get; set; }
        public string WebSPFileStatus { get; set; }
        public DateTime ParamDBDate { get; set; }
        public DateTime WebDBDate { get; set; }
    }

    public class FieldMapping
    {
        public string Column { get; set; }
        public string JsonKey { get; set; }
        public string Format { get; set; }
    }

    public class LoginRole
    {
        public string Role { get; set; }
        public string HO { get; set; }
        public string Net { get; set; }
        public string Web { get; set; }
        public bool Offline { get; set; }
        public string Message { get; set; }
        public string LicMsg { get; set; }
        public bool ContinueWithoutKey { get; set; }
    }

    public class ReadAndImportCSVModel
    {
        public string folderName { get; set; }
        public string fileName { get; set; }
        public string fileSeqNo { get; set; }
        public string batchNo { get; set; }
        public string userId { get; set; }
        public string importDate { get; set; }
    }

    public class EmailSMTPSettingModel
    {
        public string Username { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Password { get; set; }
        public string Tls { get; set; }

    }
    public class EmailModel
    {
        public string Id { get; set; }
        public string ToEmail { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public byte[] Document { get; set; }
        public string FileName { get; set; }
        public string CCEmail { get; set; }
        public string BCCEmail { get; set; }
        public string FilePassword { get; set; }
        public byte[] FinalDocument { get; set; }
    }

    public class DigitalSendEmailRequest
    {
        public string UserId { get; set; }
    }

    public class DionSSOResponse
    {
        public string d { get; set; }
    }

    public class DionSSOActualResponse
    {
        public string type { get; set; }
        public string code { get; set; }
        public string description { get; set; }
        public object result { get; set; }
    }

    public class EncryptDecryptEPIRequest
    {
        public string key { get; set; }
        public string mode { get; set; }
        public dynamic payload { get; set; }
    }

    public class SMTPSettings
    {
        public string Email { get; set; }
        public string Server { get; set; }
        public string Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string CCMail { get; set; }
        public string BCCEmail { get; set; }
        public string SSL { get; set; }
    }

    public class PostEmail
    {
        public class Attachment
        {
            public string name { get; set; }
            public string content { get; set; }
        }

        public class Attributes
        {
            public string LEAD { get; set; }
            public string BAND { get; set; }
        }

        public class Bcc
        {
            public string email { get; set; }
        }

        public class Cc
        {
            public string email { get; set; }
        }

        public class Content
        {
            public string type { get; set; }
            public string value { get; set; }
        }

        public class From
        {
            public string email { get; set; }
            public string name { get; set; }
        }

        public class Personalization
        {
            public Attributes attributes { get; set; }
            public List<To> to { get; set; }
            public List<Cc> cc { get; set; }
            public List<Bcc> bcc { get; set; }
            public string token_to { get; set; }
            public string token_cc { get; set; }
            public string token_bcc { get; set; }
            public List<Attachment> attachments { get; set; }
        }

        public class EmailReq
        {
            public From from { get; set; }
            public string reply_to { get; set; }
            public string subject { get; set; }
            public int template_id { get; set; }
            public List<string> tags { get; set; }
            public List<Content> content { get; set; }
            public List<Attachment> attachments { get; set; }
            public List<Personalization> personalizations { get; set; }
            public Settings settings { get; set; }
            public List<Bcc> bcc { get; set; }
            public int schedule { get; set; }
        }

        public class Settings
        {
            public bool open_track { get; set; }
            public bool click_track { get; set; }
            public bool unsubscribe_track { get; set; }
            public string ip_pool { get; set; }
        }

        public class To
        {
            public string email { get; set; }
            public string name { get; set; }
        }
    }

    public class NameMatchRequest
    {
        public string Name1 { get; set; }
        public string Name2 { get; set; }
    }

    public class NameMatchResult
    {
        public double TokenSortRatio { get; set; }
        public double TokenSetRatio { get; set; }
        public double JaroWinkler { get; set; }
        public double FinalScore { get; set; }
        public string MatchStatus { get; set; }
        public string Status { get; set; }
    }

    public class ValidatePDF
    {
        public string clientCode { get; set; }
        public string refNo { get; set; }
    }
}
