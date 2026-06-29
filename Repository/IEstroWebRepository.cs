using TradeWeb.API.Models;

namespace TradeWeb.API.Repository
{
    public interface IEstroWebRepository
    {
        public dynamic GetUserDetails(string clientWhere);
        public dynamic GetHolding(string clientWhere, bool blnTradeNet = false);
        public dynamic GetLedger(string clientWhere, string fromDt, string toDt, bool blnTradeNet = false);
        public dynamic GetTransaction(string clientWhere, string fromDt, string toDt, bool blnTradeNet = false);
        public dynamic Security_Listing(string searchBy, string searchText, string Alphabet, bool blnActive);
        public dynamic GetBills(string clientWhere, string fromDt, string toDt);
        public dynamic GetReport(EstroReportModel model, string strClientCode);
    }
}
