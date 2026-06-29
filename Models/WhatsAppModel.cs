using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class WhatsAppBotRequest
    {
        public string user_id { get; set; }
        public string message { get; set; }
        public string action { get; set; }
        public WhatsAppBotRequestData data { get; set; }
    }

    public class WhatsAppBotRequestData
    {
        public string user_input { get; set; }
        public string additional_data { get; set; }
    }

    public class ValidationRules
    {
        public bool required { get; set; }
        public string format { get; set; }
    }

    public class WhatsAppResponseData
    {
        public string input_expected { get; set; }
        public string variable_name { get; set; }
        public string prompt { get; set; }
        public dynamic options { get; set; }
        public ValidationRules validation_rules { get; set; }
        public string file_format { get; set; }
        public string file_name { get; set; }
        public string file_data { get; set; }
    }

    public class WhatsAppResponse
    {
        public string user_id { get; set; }
        public string message { get; set; }
        public string action { get; set; }
        public WhatsAppResponseData data { get; set; }
    }

    public class AccountResponse
    {
        public List<string> TradingAccounts { get; set; }
        public List<string> DematAccounts { get; set; }
    }

    public class MenuResponse
    {
        public List<string> Menu { get; set; }
    }

    public class ReportSelectionResponse
    {
        public string Menu { get; set; }
        public string ReportSelection { get; set; }
    }

    public class ReportRequest
    {
        public string AccountType { get; set; }
        public string Code { get; set; }
        public string Menu { get; set; }
        public string ReportSelection { get; set; }
    }

    public class LedgerSelection
    {
        public string Selection { get; set; }
    }

    public class ReportSPResponse
    {
        public dynamic Data { get; set; }
        public dynamic Format { get; set; }
        public dynamic Details { get; set; }
    }

    public class ReportPDFResponse
    {
        public string Base64PDF { get; set; }
    }
}
