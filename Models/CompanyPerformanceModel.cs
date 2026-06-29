using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class CompanyPerformanceModel
    {
    }

    public class CompanyPerformanceRequest
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public List<string> ExchSeg { get; set; }
        public bool CashDelNonDelBreakup { get; set; }
        public bool FOBreakup { get; set; }
        public Filter Filter { get; set; }
    }

    public class CompanyPerformanceDatabaseResponse
    {
        public string CExchsOrder { get; set; }
        public string ft_dpid { get; set; }
        public string ft_companycode { get; set; }
        public string ft_exchange { get; set; }
        public string ft_rptcd { get; set; }
        public string ft_rptnm { get; set; }
        public string ft_grpcd { get; set; }
        public string ft_grpnm { get; set; }
        public string ft_sname { get; set; }
        public double ft_specTotal { get; set; }
        public double ft_DelvTotal { get; set; }
        public double ft_cashTotal { get; set; }
        public double ft_Specbrokerage { get; set; }
        public double ft_DelvBrokerage { get; set; }
        public double ft_Cashbrokerage { get; set; }
        public double ft_CashFiller { get; set; }
        public double ft_FutTotal { get; set; }
        public double ft_OptTotal { get; set; }
        public double ft_FOTotal { get; set; }
        public double ft_FutBrokerage { get; set; }
        public double ft_OptBrokerage { get; set; }
        public double ft_FOBrokerage { get; set; }
        public double ft_FOFiller { get; set; }
        public double ft_FutTotalC { get; set; }
        public double ft_OptTotalC { get; set; }
        public double ft_FOTotalC { get; set; }
        public double ft_FutBrokerageC { get; set; }
        public double ft_OptBrokerageC { get; set; }
        public double ft_FOBrokerageC { get; set; }
        public double ft_ledger { get; set; }
        public string ft_segment { get; set; }
    }

    public class CompanyPerformanceWithSecurity
    {
        public string SecurityCode { get; set; }
        public string SecurityName { get; set; }
        public CashSegment BPSB_Cash { get; set; }
        public FoFsSegment BPSB_FO { get; set; }
        public FoFsSegment BPSB_FX { get; set; }
        public Commodity Com { get; set; }
        public TotalSegment Total { get; set; }
    }

    public class CashSegment
    {
        public double Spec_To { get; set; }
        public double Delv_To { get; set; }
        public double Cash_To { get; set; }
        public double Spec_Brok { get; set; }
        public double Delv_Brok { get; set; }
        public double Cash_Brok { get; set; }
    }

    public class CashSegmentBreakup
    {
        public string ScripCode { get; set; }
        public string ScripName { get; set; }
        public double Spec_To { get; set; }
        public double Delv_To { get; set; }
        public double Cash_To { get; set; }
        public double Spec_Brok { get; set; }
        public double Delv_Brok { get; set; }
        public double Cash_Brok { get; set; }
    }

    public class FOSegmentNoBreakup
    {
        public string SeriesID { get; set; }
        public string SeriesName { get; set; }
        public double FO_To { get; set; }
        public double FO_Brok { get; set; }
    }

    public class FOSegmentBreakup
    {
        public string SeriesID { get; set; }
        public string SeriesName { get; set; }
        public double Fut_To { get; set; }
        public double Opt_To { get; set; }
        public double FO_To { get; set; }
        public double Fut_Brok { get; set; }
        public double Opt_Brok { get; set; }
        public double FO_Brok { get; set; }
    }

    public class CashSegmentNoBreakup
    {
        public string ScripCode { get; set; }
        public string ScripName { get; set; }
        public double Cash_To { get; set; }
        public double Cash_Brok { get; set; }
    }

    public class CompanyPerfomanceWithDate1
    {
        public string Date { get; set; }
        public dynamic Data { get; set; }
    }

    public class FoFsSegment
    {
        public double Fut_To { get; set; }
        public double Opt_To { get; set; }
        public double Fo_To { get; set; }
        public double Fut_Brok { get; set; }
        public double Opt_Brok { get; set; }
        public double Fo_Brok { get; set; }
    }

    public class Commodity
    {
        public double Com_To1 { get; set; }
        public double Opt_To { get; set; }
        public double Com_To2 { get; set; }
        public double Com_Brok1 { get; set; }
        public double Opt_Brok { get; set; }
        public double Com_Brok2 { get; set; }
    }

    public class TotalSegment
    {
        public double Total_To { get; set; }
        public double Total_Brkg { get; set; }
    }

    public class CompanyPerfomanceWithDate
    {
        public string Date { get; set; }
        public List<CompanyPerformanceWithSecurity> Securities { get; set; }
    }

    public class CompanyPerformanceReportResponse
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public dynamic Data { get; set; }
        /*public CashSegment BPSB_Cash { get; set; }
        public FoFsSegment BPSB_FO { get; set; }
        public FoFsSegment BPSB_FX { get; set; }
        public TotalSegment Total { get; set; }*/
    }
}
