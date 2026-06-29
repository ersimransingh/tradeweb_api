using System.Data;

namespace TradeWeb.API.Models
{
    public class MarginResponse
    {
        public string ExchSeg { get; set; }
        public decimal Eod_Margin_Required { get; set; }
        public decimal Eod_Margin_Available { get; set; }
        public decimal Eod_ShortFall_Amount { get; set; }
        public decimal Eod_ShortFall_Percentage { get; set; }
        public decimal Peak_Margin_Required { get; set; }
        public decimal Peak_Margin_To_Be_Collected { get; set; }
        public decimal Peak_Margin_Available { get; set; }
        public decimal Peak_Margin_Shortfall { get; set; }
        public decimal Peak_Margin_Highest_Shortfall { get; set; }
    }

    public class MarginResponseOffline
    {
        public string Date { get; set; }
        public string ExchSeg { get; set; }
        public decimal SPAN { get; set; }
        public decimal Exposure { get; set; }
        public decimal Premium { get; set; }
        public decimal Initial { get; set; }
        public decimal Additional { get; set; }
        public decimal Collected { get; set; }
        public decimal Total { get; set; }
        public decimal Shortfall { get; set; }
    }

    public class CollateralAllocationModel
    {
        public string AsOnDate { get; set; }
        public DataTable WithCC { get; set; }
        public DataTable WithTM { get; set; }
    }
}
