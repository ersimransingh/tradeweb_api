using System.Collections.Generic;
//using TradeWeb.API.ExtentionMethod;
using TradeWeb.API.Logs;
using TradeWeb.API.Models;
using TradeWeb.API.Repository;


namespace TradeWeb.API.Service
{
    public class ThirdPartyServiceDAO
    {
        private readonly UtilityCommon _objUtility;

        public ThirdPartyServiceDAO(UtilityCommon objUtility)
        {
            _objUtility = objUtility;
        }

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

            int i = _objUtility.InsertData("", "tbl_GenericAPILog", iData);
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

            int i = _objUtility.InsertData("", "tbl_GenericAPILog", iData);
        }
    }
}