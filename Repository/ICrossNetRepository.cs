using TradeWeb.API.Models;

namespace TradeWeb.API.Repository
{
    public interface ICrossNetRepository
    {
        public dynamic GetFilterSql(Filter filter);
        public dynamic OffMarketAdd(string userId, CrossOffMarketReq req);
        public dynamic OffMarketFind(string InstrumentType, string InternalRefNo, string TransectionType);
        public dynamic InterDipositoryAdd(string userId, InterDipositoryAddReq req);
        public dynamic InterDepositoryFind(string InstrumentType, string InternalRefNo, string TransectionType);
        public dynamic OnMarketAdd(string userId, OnMarketReq req);
        public dynamic OnMarketFind(string InstrumentType, string InternalRefNo, string TransectionType);
        public dynamic EarlyPayInAdd(string userID, EarlyPayInReq req);
        public dynamic EarlyPayInFind(string InstrumentType, string InternalRefNo, string TransectionType);
        public dynamic SlipIssueAdd(string UserId, SlipIssueReq req);
        public dynamic SlipIssueFind(SlipIssueRequest req);
        public dynamic SlipStop(string UserId, SlipStopReq req);
        public dynamic ClientListing(ClientListingReq req, string loginAccess);
        public dynamic TransactionStatus(TransactionStatusReq req, string LoginAccess);
        public dynamic BillBreakUp(BillBreakUpReq req, string LoginAccess);
        public dynamic ClientOutstanding(ClientOutstandingReq req, string LoginAccess);
        public dynamic BillSummary(CrossBillSummaryReq req, string LoginAccess);
        //public dynamic Ledger(CrossLedgerReq req, string LoginAccess);
        public dynamic ReceiptPaymentAdd(ReceiptAddReq req, string UserId);
        public dynamic ReceiptPaymentFind(string Type, string SerialNo);
        public dynamic ReceiptPaymentDelete(string Type, string SerialNo, string UserID);
        /*public dynamic PaymentAdd(ReceiptAddReq req, string UserId);
        public dynamic PaymentFind(string SerialNo);
        public dynamic PaymentDelete(string SerialNo, string UserId);*/
        public dynamic JournalAdd(JournalRequest req, string userId);
        public dynamic JournalFind(string SerialNo);
        public dynamic JournalDelete(string SerialNo, string userId);
        public dynamic DebitCreditNotesAdd(DebitNotesRequest req, string UserId);
        public dynamic DebitCreditNotesFind(string Type, string DebitNote);
        public dynamic DebitCreditNotesDelete(string Type, string DebitNote, string UserId);
        /*public dynamic HoldingFind(HoldingRequest req, string LoginAccess);
        public dynamic TransactionStatementFind(TransectionStatementRequest req, string LoginAccess);*/
        public dynamic PerformanceReport(PerformanceRepRequest req, string LoginAccess);
        public dynamic AuditFind(AuditRequest req, string UserId);
        public dynamic BranchSilpIssue(string BranchCd);
        public dynamic ReceiptPaymentEntries(ReceiptPaymentEntriesReq req, string loginAccess);
        public dynamic GroupSilpIssue(string GroupCd);
        public dynamic FamilySilpIssue(string FamilyCd);
        public dynamic POASlipIssue(string POA_Id, string SlipNo);
        public dynamic ClientSlipIssue(string ClientCd, string SlipNo);
        public dynamic GetInterDepositoryClientCd(string InstrumentType, string InternalRefNo);
        public dynamic GetBranchName(string BranchCode);
        public dynamic GetFamilyName(string FamilyCode);
        public dynamic GetGroupName(string GroupCode);
        public dynamic FromSettNoSearch(string ExecDate);
        public dynamic InstrumentTypeSearch(string InstCd);
        public dynamic ReceiveModeSearch(string ReceiveCd);
        public dynamic PaymentModeSearch(string PaymentCd);
        public dynamic ReasonSearch(string ReasonCd);
        public dynamic PaidBySearch(string PaidByCd);
        public dynamic EarlyPaySearch(string EarlyPayCd);
        public dynamic EntryBySearch(string EntryByCd);
        public dynamic ClientSearch(string ClientCode, string BranchCd);
        public dynamic GetBOIDName(string BOID);
        public dynamic GetDPName(string DPID);
        public dynamic GetISINName(string ISIN);
        public dynamic GetSegment(string Segment);
        public dynamic GetExchange(string Exch);
        public dynamic GetUCCDetails(string BOID);
        public dynamic GetBOID_and_DPName(string req);
        public dynamic GetMasterCmb(string Type, string LoginAccess);
        public dynamic GetHoldingDates();
        public dynamic GetStatusCode(string TrxType);
        public dynamic TransactionType();
        public dynamic ISD_Codes();
        public dynamic GetSubMaster(string ModuleDesc);
        public dynamic GetClientModification(string BOID);
        public dynamic ValidateClientModification(CrossClientModification json);
        public dynamic ClientModification(CrossClientModification json, string userId);
    }
}
