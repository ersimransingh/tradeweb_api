namespace TradeWeb.API.Models
{
    public class EstroLedgerModel
    {
        public string ClientCode { get; set; }
        public string Date { get; set; }
        public string ChequeNo { get; set; }
        public string Particular { get; set; }
        public double Debit { get; set; }
        public double Credit { get; set; }
        public double Balance { get; set; }
    }
}
