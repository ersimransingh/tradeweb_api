using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class LedgerDetailsModel
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }

        public List<SegmentModel> type_ExchSeg { get; set; }
    }

    public class SegmentModel
    {
        public int type { get; set; }

        public List<string> exchSeg { get; set; }
    }

    public class TradeNetLedgerDetailReqModel : LedgerDetailsModel
    {
        public string ClientCode { get; set; }
    }
}
