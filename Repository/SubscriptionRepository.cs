using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using TradeWeb.API.Data;
using TradeWeb.API.Models;

namespace TradeWeb.API.Repository
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        #region Class level declarations.
        private readonly IConfiguration _configuration;
        private readonly UtilityCommon objUtility;
        private string strsql = "";
        private string strConnecton = "";
        string strToken = string.Empty;
        ////NVPLSoapClient nVPLSoapClient = new NVPLSoapClient(EndpointConfiguration.INVPLSoap);
        IHttpContextAccessor _httpContextAccessor;
        // private readonly IWebHostEnvironment _environment;
        private readonly IWebHostEnvironment _environment;
        #endregion

        #region Constructor
        public SubscriptionRepository(IConfiguration configuration, UtilityCommon objUtility, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            this.objUtility = objUtility;
            _httpContextAccessor = httpContextAccessor;
            _environment = environment;
        }
        #endregion

        public dynamic Subscription_Add(string userId, SubscriptionAddModel req)
        {
            string Code = req.ClientCode.ToUpper();
            string FromDt = req.FromDate;
            string ToDt = req.ToDate;
            string SpecialScheme = req.SpecialScheme.Trim();
            string NormalScheme = req.NormalScheme.Trim();
            int NoOfOrder = req.NoOfOrder;
            int Amount = req.Amount;
            string Status = "A";
            string Date = DateTime.Now.ToString("yyyyMMdd");
            string MkrId = userId.ToUpper();
            try
            {
                var CmSchedule = objUtility.fnFireQuery("Sysparameter", "sp_sysvalue", "sp_parmcd", "cmschedule", true);
                strsql = "select Count(*) from Client_master where cm_cd = '" + Code + "' and cm_schedule = '" + CmSchedule + "'";
                DataTable ds1 = objUtility.OpenDataTable(strsql);
                if (Convert.ToInt32(ds1.Rows[0][0].ToString().Trim()) == 0)
                {
                    return "Client Not Found";
                }
                strsql = "select * from CompanyExchangeSegments where RIGHT(CES_Cd,1) in ('C','F','K') and SUBSTRING(CES_Cd,2,1) in ('B','N')";
                DataTable ds2 = objUtility.OpenDataTable(strsql);
                for (int i = 0; i < ds2.Rows.Count; i++)
                {
                    string Companyacode = ds2.Rows[i]["CES_Cd"].ToString().Trim();
                    string ExchSeg = ds2.Rows[i]["CES_Exchange"].ToString().Trim() + "/" + ds2.Rows[i]["CES_Segment"].ToString().Trim();
                    if ("C" != Companyacode.Substring(2, 1))
                    {
                        strsql = "select COUNT(*) from CompanyExchangeSegments,FBrokerage_master where CES_Cd = '" + Companyacode + "' and CES_Cd=fb_companycode and fb_scheme = '" + SpecialScheme + "'";
                        DataTable ds3 = objUtility.OpenDataTable(strsql);
                        if (Convert.ToInt32(ds3.Rows[0][0].ToString().Trim()) == 0)
                        {
                            return "Special Scheme " + SpecialScheme + " Not Found In " + ExchSeg;
                        }
                    }
                    else
                    {
                        strsql = "select COUNT(*) from CompanyExchangeSegments,Brokerages where CES_Cd = '" + Companyacode + "' and CES_Cd=br_companycode and br_scheme = '" + SpecialScheme + "'";
                        DataTable ds3 = objUtility.OpenDataTable(strsql);
                        if (Convert.ToInt32(ds3.Rows[0][0].ToString().Trim()) == 0)
                        {
                            return "Special Scheme " + SpecialScheme + " Not Found In " + ExchSeg;
                        }
                    }
                    if ("C" != Companyacode.Substring(2, 1))
                    {
                        strsql = "select COUNT(*) from CompanyExchangeSegments,FBrokerage_master where CES_Cd = '" + Companyacode + "' and CES_Cd=fb_companycode and fb_scheme = '" + NormalScheme + "'";
                        DataTable ds4 = objUtility.OpenDataTable(strsql);
                        if (Convert.ToInt32(ds4.Rows[0][0].ToString().Trim()) == 0)
                        {
                            return "Normal Scheme " + NormalScheme + " Not Found In " + ExchSeg;
                        }
                    }
                    else
                    {
                        strsql = "select COUNT(*) from CompanyExchangeSegments,Brokerages where CES_Cd = '" + Companyacode + "' and CES_Cd=br_companycode and br_scheme = '" + NormalScheme + "'";
                        DataTable ds4 = objUtility.OpenDataTable(strsql);
                        if (Convert.ToInt32(ds4.Rows[0][0].ToString().Trim()) == 0)
                        {
                            return "Normal Scheme " + NormalScheme + " Not Found In " + ExchSeg;
                        }
                    }
                }
                strsql = "Insert Into Subscription_Order Values ('" + Code + "','" + FromDt + "','" + ToDt + "','" + SpecialScheme + "','" + NormalScheme + "'," + NoOfOrder + "," + Amount + ",'" + Status + "','','" + MkrId + "','" + Date + "','','','',0,0,0)";
                objUtility.ExecuteSQL(strsql);
                return "Success";
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic Subscription_Discontinue(SubscriptionDiscontinueModel req, string UserId)
        {
            string Code = req.ClientCode.ToUpper(), SrNo = req.SrNo;
            try
            {
                strsql = "select Suo_Status from Subscription_Order where Suo_ClientCode = '" + Code + "' and Suo_SrNo = '" + SrNo + "'";
                DataTable ds = objUtility.OpenDataTable(strsql);

                if (ds.Rows.Count == 0)
                {
                    return "Subscription Details Not Found";
                }
                if (ds.Rows[0]["Suo_Status"].ToString().Trim() == "I")
                {
                    return "Subscription is Already Discontinued";
                }
                strsql = "update Subscription_Order set Suo_Status = 'I', Suo_InactiveDt = '" + DateTime.Now.ToString("yyyyMMdd") + "' where Suo_ClientCode = '" + Code + "' and Suo_Status = 'A'";
                objUtility.ExecuteSQL(strsql);

                strsql = "insert into Subscription_Audit select Suo_SrNo,'" + Code + "',Suo_FromDate,Suo_ToDate,Suo_SpecialScheme, " +
                         "Suo_NormalScheme,Suo_NoOfOrder,Suo_Amount,Suo_Status,Suo_InactiveDt,'" + UserId.ToUpper() + "',Suo_MkrDt, " +
                         "Suo_Filler1,Suo_Filler2,Suo_Filler3,Suo_FillerN1,Suo_FillerN2,Suo_FillerN3,'M' " +
                         "from Subscription_Order " +
                         "where Suo_ClientCode = '" + Code + "' and Suo_SrNo = '" + SrNo + "'";
                objUtility.ExecuteSQL(strsql);
                return "Success";
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic Subscription_Delete(SubscriptionDiscontinueModel req, string UserId)
        {
            string Code = req.ClientCode.ToUpper(), SrNo = req.SrNo;
            try
            {
                strsql = "select Suo_Status from Subscription_Order where Suo_ClientCode = '" + Code + "' and Suo_SrNo = '" + SrNo + "'";
                DataTable ds = objUtility.OpenDataTable(strsql);

                if (ds.Rows.Count == 0)
                {
                    return "Subscription Details Not Found";
                }
                strsql = "insert into Subscription_Audit select Suo_SrNo,'" + Code + "',Suo_FromDate,Suo_ToDate,Suo_SpecialScheme, " +
                         "Suo_NormalScheme,Suo_NoOfOrder,Suo_Amount,Suo_Status,Suo_InactiveDt,'" + UserId.ToUpper() + "',Suo_MkrDt, " +
                         "Suo_Filler1,Suo_Filler2,Suo_Filler3,Suo_FillerN1,Suo_FillerN2,Suo_FillerN3,'D' " +
                         "from Subscription_Order " +
                         "where Suo_ClientCode = '" + Code + "' and Suo_SrNo = '" + SrNo + "'";
                objUtility.ExecuteSQL(strsql);

                strsql = "delete from Subscription_Order where Suo_SrNo = '" + SrNo + "' and Suo_ClientCode = '" + Code + "'";
                objUtility.ExecuteSQL(strsql);
                return "Success";
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic Subscription_Status(SubscriptionStatusModel req)
        {
            string Code = req.ClientCode.ToUpper(), AsOn = req.AsOn, strWhere = "";
            try
            {
                var db = new DataContext();
                using (SqlConnection sqlCon = new SqlConnection((db.Database.GetDbConnection()).ConnectionString))
                {
                    sqlCon.Open();
                    strsql = "create table #TempSuo_Order" +
                             "(t_srNo int," +
                             "t_clientcd char(8)," +
                             "t_fromDt char(8)," +
                             "t_toDt char(8)," +
                             "t_specialScheme char(8)," +
                             "t_normalScheme char(8)," +
                             "t_noOfOrder int," +
                             "t_amount money," +
                             "t_status char(1)," +
                             "t_inactiveDt char(8)," +
                             "t_executed_Order int," +
                             "t_pending_Order int)";
                    objUtility.ExecuteSQL(strsql, sqlCon);

                    strsql = "select Count(*) from Subscription_Order where Suo_ClientCode = '" + Code + "'";
                    DataTable ds = objUtility.OpenDataTable(strsql);
                    if (Convert.ToDouble(ds.Rows[0][0].ToString().Trim()) == 0 && Code != "")
                    {
                        return "Subscription Details Not Found";
                    }

                    if (AsOn.Trim() == "")
                    {
                        AsOn = DateTime.Now.ToString("yyyyMMdd");
                    }

                    if (Code == "" && AsOn != "")
                    {
                        strWhere = " where Suo_ToDate >= '" + AsOn + "' and (Suo_InactiveDt > '" + AsOn + "' or Suo_InactiveDt = '') and Suo_FromDate <= '" + AsOn + "'";
                    }
                    if (Code != "" && AsOn != "")
                    {
                        strWhere = " where Suo_ClientCode = '" + Code + "' and Suo_ToDate >= '" + AsOn + "' and (Suo_InactiveDt > '" + AsOn + "' or Suo_InactiveDt = '') and Suo_FromDate <= '" + AsOn + "'";
                    }
                    strsql = "insert into #TempSuo_Order select Suo_SrNo,Suo_ClientCode,Suo_FromDate,Suo_ToDate,Suo_SpecialScheme,Suo_NormalScheme,Suo_NoOfOrder,Suo_Amount,Suo_Status,Suo_InactiveDt,0,0 from Subscription_Order" + strWhere;
                    objUtility.ExecuteSQL(strsql, sqlCon);

                    strsql = "update #TempSuo_Order set t_executed_Order = (select count(distinct td_orderid) orders from trx where td_dt between t_fromdt and t_toDt and t_clientcd = td_clientcd)";
                    objUtility.ExecuteSQL(strsql, sqlCon);
                    strsql = "update #TempSuo_Order set t_executed_order += (select count(distinct td_orderid) orders from Trades where td_dt between t_fromdt and t_toDt and t_clientcd = td_clientcd)";
                    objUtility.ExecuteSQL(strsql, sqlCon);
                    strsql = "update #TempSuo_Order " +
                             "set t_pending_Order = t_noOfOrder - t_executed_Order";
                    objUtility.ExecuteSQL(strsql, sqlCon);
                    SqlDataAdapter sqlDtAdap = new SqlDataAdapter();
                    SqlCommandBuilder sqlCmdBld = new SqlCommandBuilder(sqlDtAdap);
                    strsql = "select t_srNo as SrNo,t_clientcd as ClientCode,t_fromDt as FromDate,t_toDt as ToDate,t_specialScheme as SpecialScheme,t_normalScheme as NormalScheme,t_noOfOrder as NoOfOrder,t_amount as Amount,t_status as Status,t_inactiveDt as InactiveDt,t_executed_Order as ExecutedOrder,t_pending_Order as PendingOrder from #TempSuo_Order";
                    DataSet ds2 = objUtility.OpenDataSet(sqlDtAdap, strsql, sqlCon);
                    return ds2;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
