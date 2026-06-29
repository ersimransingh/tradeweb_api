using System;
using System.Data;

namespace TradeWeb.API.Models
{
    public class commonResponse
    {
        public bool status { get; set; }
        public int status_code { get; set; }
        public string message { get; set; }
        //public string error_message { get; set; }
        public Object data { get; set; }
        //// dyname and object also are working
    }

    public class tokenResponse : commonResponse
    {
        public string token { get; set; }
        public string tokenExpireTime { get; set; }
    }

    public class tokenResponseNew : commonResponse
    {
        public string token { get; set; }
        public string tokenExpireTime { get; set; }
        public string refreshToken { get; set; }
        public string refreshTokenExpireTime { get; set; }
    }

    public class DynamicResponse
    {
        public dynamic response { get; set; }
    }

    public class commonResponse1
    {
        public bool status { get; set; }
        public int statusCode { get; set; }
        public string message { get; set; }
        public string errorMessage { get; set; }
        public dynamic data { get; set; }
    }

    public class returnReponseWithDT
    {
        public bool status { get; set; }
        public string message { get; set; }
        public DataTable data { get; set; }
    }
    public class returnResponseBlank
    {
        public bool status { get; set; }
        public string message { get; set; }
        public Object data { get; set; }
    }

    public class returnKeyValue
    {
        public string code { get; set; }
        public string desc { get; set; }
    }
}
