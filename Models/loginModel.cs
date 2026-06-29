using System;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace TradeWeb.API.Models
{
    public class loginModel
    {
        public string username { get; set; }
        public string password { get; set; }
        public string companycode { get; set; } = String.Empty;
    }

    public class TradeWebLoginModel
    {
        public string username { get; set; }
        public string password { get; set; }
        public string role { get; set; }
        public string guid { get; set; }
        public int tokenExpTime { get; set; }
        public string useraccess { get; set; }
    }

    public class ChangePassword
    {
        public string currentPassword { get; set; }
        public string newPassword { get; set; }
    }

    public class loginResponseWithDT
    {
        public string message { get; set; }
        public DataTable data { get; set; }
    }

    public class GetOtpResponseFP
    {
        public string status { get; set; }
        public string message { get; set; }
        public string additionalMessage { get; set; }
        public string isAlphaNum { get; set; } = "N";
        public int maxLength { get; set; } = 0;
        public int minLength { get; set; } = 0;
    }
    public class VerifyOtpResponseFP
    {
        public string status { get; set; }
        public string message { get; set; }
        public bool redirectLogin { get; set; } = false;
    }
    public class UpdatePasswordFP
    {
        [Required] public string userId { get; set; }
        [Required] public string otp { get; set; }
        [Required] public string newPassword { get; set; }
    }

    public class LoginRequest
    {
        public string UserId { get; set; }
        public string Password { get; set; }
        public string EPassword { get; set; }
        public string Key { get; set; }
        public string LoginAs { get; set; }
        public string Product { get; set; }
        public string ICPV { get; set; }
        public string Feature { get; set; }
    }

    public class GenerateTokenModel
    {
        public string username { get; set; }
        public string companyCode { get; set; }
        public string role { get; set; }
        public string loginAs { get; set; }
    }

}
