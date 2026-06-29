namespace TradeWeb.API.Models
{
    public class ReceiptPaymentEntriesReq
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string IsReceipt { get; set; }
        public string IsPayment { get; set; }
    }
    public class ReceiptPaymentEntriesResponse
    {
        public string SrNo { get; set; }
        public dynamic PaymentVoucher { get; set; }
        public string Voucher { get; set; }
        public string Date { get; set; }
        public string BO_ID { get; set; }
        public string BO_Name { get; set; }
        public string ChequeNo { get; set; }
        public string Paid { get; set; }
        public double ReceivedIn { get; set; }
    }

    public class PaymentVoucher
    {
        public dynamic Data1 { get; set; }
        public dynamic Data2 { get; set; }
    }
}
