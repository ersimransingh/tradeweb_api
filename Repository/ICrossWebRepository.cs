using System.Collections.Generic;

namespace TradeWeb.API.Repository
{
    public interface ICrossWebRepository
    {
        public dynamic GetUserDetails(string clientWhere);
        public dynamic GetHolding(string clientWhere, bool blnTradeNet = false, List<string> balanceType = null, bool showValuation = true, string asOn = "");
        public dynamic GetLedger(string clientWhere, string fromDt, string toDt, bool blnTradeNet = false);
        public dynamic GetTransaction(string clientWhere, string fromDt, string toDt, bool blnTradeNet = false, string isin = "", List<string> transactionType = null);
        public dynamic Security_Listing(string searchBy, string searchText, string Alphabet, bool blnActive);
        public dynamic Bill(string clientWhere, string fromDt, string toDt, bool blnTradeNet = false);

    }
}
