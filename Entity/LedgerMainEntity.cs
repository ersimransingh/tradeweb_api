using System;
using System.Collections.Generic;
using System.Text;

namespace TradeWeb.Entity
{
    public class LedgerMainEntity
    {
        public string account { get; set; }
        public string ld_clientcd { get; set; }
        public string exch { get; set; }
        public string ld_dpid { get; set; }
        public decimal OpenBal { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Closing { get; set; }
        public string heading { get; set; }
        public string exchtitle { get; set; }
    }

    public class LedgerSummaryEntity
    {
        public string ClientCode { get; set; }
        public string Type { get; set; }
        public string ExchSeg { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
        public string CompanyCode { get; set; }
    }
}
