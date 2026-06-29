using System.Collections.Generic;
using TradeWeb.API.ExtentionMethod;
using TradeWeb.API.Logs;
using TradeWeb.API.Models;


namespace TradeWeb.API.Service
{
    public class ThirdPartyServiceDAO
    {
        InsertOrUpdate iuObj = new InsertOrUpdate();

        public void InsertThirdPartyServiceData(ThirdPartyModel fModel, string reqDetail, string outputResponse)
        {
            string outputString = outputResponse ?? "";
            outputString = outputString.Replace("'", " ");
            LogInfo.WriteErrorLog("DAO InsertThirdPartyServiceData Response 1 " + outputString);
            Dictionary<string, object> iData = new Dictionary<string, object>();
            iData.Add("ProjectName", fModel.ProjectName ?? "");
            iData.Add("APIVendorName", fModel.APIVendorName ?? "");
            //iData.Add("Request", fModel.Request.ToString());
            iData.Add("Request", reqDetail);
            iData.Add("Response", outputString);
            iData.Add("UpdateTimeStamp", "GetDate()");

            int i = iuObj.InsertData("", "tbl_GenericAPILog", iData);
        }
        public void InsertThirdPartyServiceData(ThirdPartyServiceMultiPartModel fModel, string reqDetail, string outputResponse)
        {
            string outputString = outputResponse ?? "";
            outputString = outputString.Replace("'", " ");
            Dictionary<string, object> iData = new Dictionary<string, object>();
            iData.Add("ProjectName", fModel.ProjectName ?? "");
            iData.Add("APIVendorName", fModel.APIVendorName ?? "");
            //iData.Add("Request", fModel.Request.ToString());
            iData.Add("Request", reqDetail);
            iData.Add("Response", outputString);
            iData.Add("UpdateTimeStamp", "GetDate()");

            int i = iuObj.InsertData("", "tbl_GenericAPILog", iData);
        }

        public dynamic Authenticate(AuthenticateRequest model)
        {
            //var user = _users.SingleOrDefault(x => x.Username == model.Username && x.Password == model.Password);
            var user = new User { Username = model.Username, Password = model.Password };
            if (iuObj.checkPassword(user))
            {
                // return null if user not found
                if (user == null)
                    return null;

                return user;
            }
            else
            {
                return null;
            }
        }



    }
}