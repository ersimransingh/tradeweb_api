using System.ComponentModel;

namespace TradeWeb.API.Models
{
    public class GenerateOtpModel
    {
        [DefaultValue(false)]
        public bool IsChangeIncome { get; set; }
        [DefaultValue(false)]
        public bool IsChangeMobile { get; set; }
        [DefaultValue(false)]
        public bool IsChangeEmail { get; set; }
    }
    public class GenerateOtpNew
    {
        public string reqType { get; set; }
        public string reqValue { get; set; }
    }
    public class GenerateOtpResponseModel
    {
        public string Otp { get; set; }
        public string ChangeMethod { get; set; }
        public string Message { get; set; }
    }

    public class UpdateDetailModel
    {
        public string Otp { get; set; }
        public string NewMobileNo { get; set; }
        public int NewMobileOwner { get; set; }
        public string NewEmailId { get; set; }
        public int NewEmailOwner { get; set; }
        public int NewIncome { get; set; }
        public int NewIncomeYear { get; set; }
        public string NewIncomeImage { get; set; }
        public string NewIncomeExt { get; set; }
    }

    public class UpdateMobileNoModel
    {
        public string Otp { get; set; }
        public string NewMobileNo { get; set; }
    }

    public class UpdateEmailModel
    {
        public string Otp { get; set; }
        public string NewEmailId { get; set; }

    }
}
