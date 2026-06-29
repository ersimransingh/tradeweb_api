namespace TradeWeb.API.Models
{
    public class GainLossDivedendResponse
    {
        public string ClientCode { get; set; }
        public string DivDate { get; set; }
        public string ScripCode { get; set; }
        public string ScripName { get; set; }
        public double Qty { get; set; }
        public double Rate { get; set; }
        public double Amount { get; set; }
    }

    public class GainLossActualPLSummaryResponse
    {
        public string ClientCode { get; set; }
        public string ScripCode { get; set; }
        public string ScripName { get; set; }
        public string ISIN { get; set; }
        public double BuyQty { get; set; }
        public double BuyAmount { get; set; }
        public double SellQty { get; set; }
        public double SellAmount { get; set; }
        public double Trading { get; set; }
        public double ShortTerm { get; set; }
        public double LongTerm { get; set; }
        public double STT { get; set; }

    }


    public class GainLossActualPLDetailsResponse
    {
        public string ScriptCd { get; set; }
        public string ScripCode { get; set; }
        public string ScripName { get; set; }
        public string Type { get; set; }
        public string Qty { get; set; }//
        public string SellDate { get; set; }
        public string SellRate { get; set; }//
        public string SellValue { get; set; }//
        public string BuyDate { get; set; }
        public string BuyRate { get; set; }//
        public string BuyValue { get; set; }//
        public string Rate112A { get; set; }//
        public string Trading { get; set; }//
        public string ShortTerm { get; set; }//
        public string LongTerm { get; set; }//
        public string STT { get; set; }//
        public string QtrSlab { get; set; }//

    }

    public class GainLossActualPLDetailResponse
    {
        public string ClientCode { get; set; }
        public string ScripCode { get; set; }
        public string ScripName { get; set; }
        public string ISIN { get; set; }
        public double Qty { get; set; }
        public string SellDate { get; set; }
        public double SellRate { get; set; }
        public string BuyDate { get; set; }
        public double BuyRate { get; set; }
        public double Trading { get; set; }
        public double ShortTerm { get; set; }
        public double LongTerm { get; set; }
        public double STT { get; set; }
        public string QtrSlab { get; set; }
        public string LTCG { get; set; }
        public double Rate112A { get; set; }
        public double Cost { get; set; }
    }

    public class GainLossTradeListingSummaryResponse
    {
        public string ClientCode { get; set; }
        public string ScripCode { get; set; }
        public string ScripName { get; set; }
        public double BuyQuantity { get; set; }
        public double BuyRate { get; set; }
        public double BuyAmount { get; set; }
        public double SellQuantity { get; set; }
        public double SellRate { get; set; }
        public double SellAmount { get; set; }
        public double NetQuantity { get; set; }
        public double NetAmount { get; set; }
    }

    public class GainLossTradeListingDetailResponse
    {
        public string ClientCode { get; set; }
        public string SrNo { get; set; }
        public string Date { get; set; }
        public string TrxFlag { get; set; }
        public string Settlement { get; set; }
        public string TrdType { get; set; }
        public string BsFlag { get; set; }
        public double Quantity { get; set; }
        public double Rate { get; set; }
        public double Value { get; set; }
        public double OtherCharge1 { get; set; }
        public double ServiceTax { get; set; }
        public double STT { get; set; }
        public double OtherCharge2 { get; set; }
    }

    public class GainLossNationalDetailResponse
    {
        public string ClientCode { get; set; }
        public string date { get; set; }
        public string ScripCode { get; set; }
        public string ScripName { get; set; }
        public string ISIN { get; set; }
        public double Qty { get; set; }
        public double Rate { get; set; }
        public double Cost { get; set; }
        public double ClosingRate { get; set; }
        public double ClosingAmount { get; set; }
        public double ShortTerm { get; set; }
        public double LongTerm { get; set; }
        public double STT { get; set; }
        public double Days { get; set; }
        public double Rate112A { get; set; }
        public string LTCG { get; set; }
    }

    public class GainLossNationalSummaryResponse
    {
        public string ClientCode { get; set; }
        public string ScripCode { get; set; }
        public string ScripName { get; set; }
        public string ISIN { get; set; }
        public double Qty { get; set; }
        public double Rate { get; set; }
        public double Cost { get; set; }
        public double ClosingRate { get; set; }
        public double ClosingAmount { get; set; }
        public double ShortTerm { get; set; }
        public double LongTerm { get; set; }
        public double STT { get; set; }
    }

}
