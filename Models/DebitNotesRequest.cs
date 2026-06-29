namespace TradeWeb.API.Models
{
    public class DebitNotesRequest
    {
        public string Type { get; set; }
        public string SrNo { get; set; }
        public string Date { get; set; }
        public string Account { get; set; }
        public double Amount { get; set; }
        public string CounterAccount { get; set; }
        public string Particular { get; set; }
    }

    public class CreditNotesRequest
    {
        public string Credit_Note { get; set; }
        public string Date { get; set; }
        public string Account_Code { get; set; }
        public double Amount { get; set; }
        public string Account_To_Be_Debited { get; set; }
        public string Description { get; set; }
    }
}
