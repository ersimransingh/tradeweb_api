using System.Collections.Generic;
using TradeWeb.API.Models;

namespace TradeWeb.API.Repository
{
    public interface ITradeNetRepository
    {
        public dynamic Validate_LoginAccess(string clientCode, string loginAccess);
        public dynamic GetFilterSql(FilterClient filter);
        public dynamic GetFilterSql(Filter filter);
        public dynamic Get_exchSeg();
        public dynamic OutstandingBalance(string userId, OutstandingBalanceReq req, string loginAccess);
        dynamic BrokerageCashSegment(string client, string cmbscheme, string exchange);
        dynamic BrokerageFOSegment(string mode, string scheme, string exchSeg, string client);
        public dynamic GetBrokeragScheme(string segment);
        public dynamic BillSummaryCash(BillSummaryModel req, string loginAccess);
        public dynamic BillSummaryFO(BillSummaryFORequest req, string loginAccess);
        public dynamic ClientMaster(ClientMasterModel req, string loginAccess);
        public dynamic EntryReceiptPayment(string userId, EntryReceiptPaymentReq req);
        dynamic Performance_Cash(PerformanceRequestModel model, string compCd, string loginAccess);
        public dynamic Performance_FO(PerformanceFORequestModel req, string compCd, string loginAccess);
        public dynamic Performance_Commex(PerformanceCommexRequestModel req);
        public dynamic EntryGSTInvoice(string userId, string compcd, EntryGSTInvoiceRequest req);
        public dynamic Outstanding_Ageing(OutstandingAgeingRequestModel req, string loginAccess);
        dynamic CommisionReport(string fromDate, string toDate, Filter filter, string ReportType, string loginAccess);
        public dynamic Transaction_Detail(TransactionDetailModel req, string loginAccess);
        public dynamic BrokerageSchemChange(List<BrokerageSchemeChange> listData, string userID);
        public dynamic DeliveryStatement(DeliveryStatementReq req, string compCd, string loginAccess);
        public dynamic CompanyPerformance(CompanyPerformanceRequest req);
        public dynamic GetMasters(string type);
    }
}
