using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class EntryReceiptPaymentModel
    {

    }
    public class EntryReceiptPaymentReq
    {
        public string Type { get; set; }
        public string ExchSeg { get; set; }
        public string EntryDt { get; set; }
        public string ReceivedAs { get; set; }
        public string BankCode { get; set; }
        public string ClientCode { get; set; }
        public string VoucherNo { get; set; }
        public string ChequeNo { get; set; }
        public string Particulars { get; set; }
        public string MICR { get; set; }
        public string BankAccNo { get; set; }
        public double Amount { get; set; }
        public string ClearDt { get; set; }
    }

    public class EntryReceiptPaymentRes
    {
        public string Status { get; set; }
        public string Response { get; set; }
    }

    public class ReceiptTableModel
    {
        public string mode { get; set; }
        public double rc_srno { get; set; }
        public string rc_voucherno { get; set; }
        public string rc_clientcd { get; set; }
        public string cm_name { get; set; }
        public string rc_receiptdt { get; set; }
        public decimal rc_amount { get; set; }
        public string rc_debitflag { get; set; }
        public string rc_particular { get; set; }
        public string rc_bankclientcd { get; set; }
        public string rc_cleareddt { get; set; }
        public int rc_entryno { get; set; }
        public string rc_chequeno { get; set; }
        public string rc_micr { get; set; }
        public string mkrid { get; set; }
        public string mkrdt { get; set; }
        public string rc_dpid { get; set; }
        public string rc_accyear { get; set; }
        public string rc_authid1 { get; set; }
        public string rc_authid2 { get; set; }
        public string rc_authdt1 { get; set; }
        public string rc_authdt2 { get; set; }
        public string rc_status { get; set; }
        public string rc_authremarks { get; set; }
        public string rc_commondt { get; set; }
        public string rc_common { get; set; }
        public string rc_BankActNo { get; set; }
        public double rc_batchno { get; set; }
        public string mkrtm { get; set; }
        public string rc_authtm1 { get; set; }
        public string rc_authtm2 { get; set; }
        public string rc_costcenter { get; set; }
        public IEnumerable<JsonComboModel> GetBankName { get; set; }
        public IEnumerable<JsonComboModel> GetAllMicr { get; set; }
        public IEnumerable<JsonComboModel> GetAllAc { get; set; }
        public byte[] ReceiptImage { get; set; }
    }

    public class JsonComboModel
    {
        public string Display { get; set; }
        public string Value { get; set; }
    }
}
