using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TradeWeb.API.Data;
using TradeWeb.API.ExtentionMethod;
using TradeWeb.API.Models;

namespace TradeWeb.API.Repository
{
    public class TradeNetRepository : ITradeNetRepository
    {
        #region Class level declarations.
        private readonly IConfiguration _configuration;
        private readonly UtilityCommon objUtility;
        private string strsql = "";
        string strToken = string.Empty;
        ////NVPLSoapClient nVPLSoapClient = new NVPLSoapClient(EndpointConfiguration.INVPLSoap);
        IHttpContextAccessor _httpContextAccessor;
        // private readonly IWebHostEnvironment _environment;
        private readonly IWebHostEnvironment _environment;
        #endregion

        #region Constructor
        public TradeNetRepository(IConfiguration configuration, UtilityCommon objUtility, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            this.objUtility = objUtility;
            _httpContextAccessor = httpContextAccessor;
            _environment = environment;
        }
        #endregion
        public dynamic Validate_LoginAccess(string clientCode, string loginAccess)
        {
            var query = "select cm_cd from client_master where cm_cd='" + clientCode + "' and cm_type <> 'C' and cm_Schedule =  '49843750' " + loginAccess;
            try
            {
                var ds = objUtility.OpenDataSet(query);
                if (ds?.Tables?.Count > 0 && ds?.Tables[0]?.Rows?.Count > 0)
                {
                    //var json = ds.Tables[0];
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetFilterSql(FilterClient filter)
        {
            try
            {
                string strClientWhere = "";
                if (filter.Client != null)
                {
                    if (filter.Client.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(filter.Client.ToArray(), "##"));
                        strClientWhere += " or cm_cd in('" + fltr.Replace("##", "','") + "')";
                    }

                    if (strClientWhere.Length > 0)
                    {
                        strClientWhere = " and (" + strClientWhere.Substring(3) + ") ";
                    }
                }
                return strClientWhere;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetFilterSql(Filter filter)
        {
            try
            {
                string strClientWhere = "";
                if (filter.Client != null)
                {
                    if (filter.Client.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(filter.Client.ToArray(), "##"));
                        strClientWhere += " or cm_cd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (filter.Branch != null)
                {
                    if (filter.Branch.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(filter.Branch.ToArray(), "##"));
                        strClientWhere += " or cm_brboffcode in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (filter.Group != null)
                {
                    if (filter.Group.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(filter.Group.ToArray(), "##"));
                        strClientWhere += " or cm_groupcd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (filter.Family != null)
                {
                    if (filter.Family.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(filter.Family.ToArray(), "##"));
                        strClientWhere += " or cm_familycd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (strClientWhere.Length > 0)
                {
                    strClientWhere = " and (" + strClientWhere.Substring(3) + ") ";
                }
                return strClientWhere;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic Get_exchSeg()
        {
            var query = "select CES_Cd CESCd ,Rtrim(CES_Exchange) exchange,Rtrim(CES_Segment) segment from CompanyExchangeSegments";
            try
            {
                var ds = objUtility.OpenDataSet(query);
                if (ds?.Tables?.Count > 0 && ds?.Tables[0]?.Rows?.Count > 0)
                {
                    var json = ds.Tables[0];
                    return json;
                }
                return null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic OutstandingBalance(string userId, OutstandingBalanceReq req, string loginAccess)
        {
            try
            {
                List<OutstandingBalanceModel> outstandingBalance = new List<OutstandingBalanceModel>();
                string strClientWhere = "", Commexconn = "", strDpId = "", strWhere = "", strNewDpId = "", strDpIdComm = "";
                bool blnTCM = false, blnComm = false, blnCommInc = false, blnCommLenght = false, blnTplusLength = false;
                string strCmschedule = objUtility.GetSysParmSt("CMSCHEDULE", "");

                if (req.Selection.Filter.Client != null)
                {
                    if (req.Selection.Filter.Client.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Selection.Filter.Client.ToArray(), "##"));
                        strClientWhere += " or cm_cd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Selection.Filter.Branch != null)
                {
                    if (req.Selection.Filter.Branch.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Selection.Filter.Branch.ToArray(), "##"));
                        strClientWhere += " or cm_brboffcode in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Selection.Filter.Group != null)
                {
                    if (req.Selection.Filter.Group.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Selection.Filter.Group.ToArray(), "##"));
                        strClientWhere += " or cm_groupcd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Selection.Filter.Family != null)
                {
                    if (req.Selection.Filter.Family.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Selection.Filter.Family.ToArray(), "##"));
                        strClientWhere += " or cm_familycd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (strClientWhere.Length > 0)
                {
                    strClientWhere = " and (" + strClientWhere.Substring(3) + ") ";
                }

                strWhere = "and ld_dt<= '" + req.AsOnDate + "'";
                if (req.Selection.ExchSeg != null)
                {
                    if (req.Selection.ExchSeg.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Selection.ExchSeg.ToArray(), "##"));
                        strDpId = fltr.Replace("##", "','");
                        strDpId = " ld_dpid in ('" + strDpId + "')";
                    }
                }

                strsql = "select * from other_products where op_product='Commex' and op_status='A'";
                DataTable d = objUtility.OpenDataTable(strsql);
                if (d.Rows.Count > 0)
                {
                    blnComm = true;
                    Commexconn = "[" + d.Rows[0]["Op_Server"].ToString().Trim() + "].[" + d.Rows[0]["Op_DataBase"].ToString().Trim() + "].[" + d.Rows[0]["Op_Owner"].ToString().Trim() + "]";
                    blnTCM = objUtility.mfnGetSysSplFeatureCommodity("TCM");
                }
                strsql = "";

                if (req.Selection.ExchSeg != null)
                {
                    if (req.Selection.ExchSeg.All(y => y != ""))
                    {
                        strDpId = strDpId.Substring(13);
                        strDpId = strDpId.Remove(strDpId.Length - 1);
                        string[] DpId = strDpId.Split(',', 5);
                        if (blnComm)
                        {
                            if (blnTCM)
                            {
                                for (int i = 0; i < DpId.Length; i++)
                                {
                                    blnTplusLength = true;
                                    strNewDpId += DpId[i] + ",";
                                }
                            }
                            else
                            {
                                for (int i = 0; i < DpId.Length; i++)
                                {
                                    string Substring = DpId[i].Substring(3, 1);
                                    if (Substring == "X")
                                    {
                                        blnCommLenght = true;
                                        DpId[i] = DpId[i].Substring(1, 2);
                                        strDpIdComm += "'" + DpId[i] + "',";
                                    }
                                    else
                                    {
                                        blnTplusLength = true;
                                        strNewDpId += DpId[i] + ",";
                                    }
                                }
                            }
                        }
                        else
                        {
                            strDpId = Strings.Join(req.Selection.ExchSeg.ToArray(), "','");
                            strNewDpId = "'" + strDpId + "'";
                        }
                    }
                }
                string strSelect = "", strOuterSelect = "";
                if (req.Selection.ExchSeg == null || req.Selection.ExchSeg.All(y => y == ""))
                {
                    if (blnComm && !blnTCM)
                    {
                        blnCommInc = true;
                    }
                }
                if (blnTplusLength)
                {
                    strNewDpId = strNewDpId.Remove(strNewDpId.Length - 2);
                    strNewDpId = "'" + strNewDpId.Substring(1) + "'";
                }
                if (blnCommLenght)
                {
                    strDpIdComm = strDpIdComm.Remove(strDpIdComm.Length - 1);
                }

                strsql = "select CES_Cd,Rtrim(CES_Exchange) + '-' + Rtrim(CES_Segment) as 'ExchSeg' from CompanyExchangeSegments ";
                if (strNewDpId.Trim() != "")
                {
                    strsql += " where CES_Cd in (" + strNewDpId + ") ";
                }
                if (!blnTCM && strNewDpId.Trim() == "")
                {
                    strsql += " where substring(CES_Cd,3,1) != 'X'";
                }
                if (strDpIdComm != "" || blnCommInc)
                {
                    strsql += "union ";
                    strsql += "select Left(CES_Cd,2) + 'X' CES_Cd,Rtrim(CES_Exchange) + '-' + Rtrim(CES_Segment) as 'ExchSeg' from " + Commexconn + ".CompanyExchangeSegments";
                    if (strDpIdComm.Trim() != "")
                    {
                        strsql += " where substring(CES_Cd,1,2) in (" + strDpIdComm + ") ";
                    }
                }

                DataTable dtExch = objUtility.OpenDataTable(strsql);
                List<string> strExch = new List<string>();
                string strExchSeg = "", strCheckTCM = "";
                foreach (DataRow dr in dtExch.Rows)
                {
                    strExchSeg = dr["ExchSeg"].ToString().Trim();
                    /*strsql = "select count(*) from " + Commexconn + ".CompanyExchangeSegments";
                    strsql += " where Rtrim(CES_Exchange) + '-' + Rtrim(CES_Segment) = '" + strExchSeg + "'";
                    DataTable dtCheck = objUtility.OpenDataTable(strsql);
                    strCheckTCM = dr["CES_Cd"].ToString().Trim();*/

                    if (Strings.Right(dr["CES_Cd"].ToString().Trim(), 1) == "X")
                    {
                        string[] ListstrExchSeg = strExchSeg.Split("-");
                        strExchSeg = ListstrExchSeg[0] + "-Comm";
                        strSelect += " Case when ld_dpid = '" + dr["CES_Cd"].ToString().Trim() + "' and product = 'C' then sum(Amount) else 0 end '" + strExchSeg + "',";
                        strOuterSelect += " sum(d.[" + strExchSeg + "]) as '" + strExchSeg + "',";
                        strExch.Add(strExchSeg);
                    }
                    else
                    {
                        strSelect += " Case when ld_dpid = '" + dr["CES_Cd"].ToString().Trim() + "' and product = 'T' then sum(Amount) else 0 end '" + strExchSeg + "',";
                        strOuterSelect += " sum(d.[" + strExchSeg + "]) as '" + strExchSeg + "',";
                        strExch.Add(strExchSeg);
                    }
                }
                strSelect = strSelect.Remove(strSelect.Length - 1);
                strOuterSelect = strOuterSelect.Remove(strOuterSelect.Length - 1);

                strsql = "select cm_cd,cm_name,cm_brboffcode,bm_branchname,sum(Outstanding) as 'Total'," + strOuterSelect;
                strsql += " from ";
                strsql += "(select cm_cd,cm_name,cm_brboffcode,bm_branchname,sum(Amount) as 'Outstanding'," + strSelect;
                strsql += "from ";
                strsql += "( ";
                strsql += "select cm_cd,cm_name,cm_brboffcode,bm_branchname,sum(ld_amount) Amount,ld_dpid,'T' product from Branch_master, Client_master ";
                if (req.Selection.IncMTFAct)
                {
                    strsql += " left join MrgTdgFin_Clients on cm_cd=MTFC_CMCD , ledger ";
                }
                else
                {
                    strsql += " , ledger ";
                }
                if (req.Selection.IncMarginAct)
                {
                    strsql += " where (cm_cd = ld_clientcd OR cm_brkggroup = ld_clientcd ";
                }
                else
                {
                    strsql += " where (cm_cd = ld_clientcd ";
                }
                if (req.Selection.IncMTFAct)
                {
                    strsql += " or  MTFC_FillerB=ld_clientcd ";
                }
                strsql += " ) ";
                strsql += " and cm_brboffcode = bm_branchcd and cm_schedule = " + strCmschedule + " and ld_dt <= '" + req.AsOnDate + "' ";
                if (strNewDpId.Trim() != "")
                {
                    strsql += " and ld_dpid in (" + strNewDpId + ") ";
                }
                strsql += strClientWhere + loginAccess + " and cm_cd not in(select cm_brkggroup from Client_master where cm_brkggroup <> '') group by cm_cd,cm_name,cm_brboffcode,bm_branchname,ld_dpid " + (req.Selection.IncZeroBalance ? "" : "having SUM(ld_amount) <> 0 ");
                if (!string.IsNullOrEmpty(Commexconn))
                {
                    strsql += "union all ";
                    strsql += "select cm_cd,cm_name,cm_brboffcode,bm_branchname,sum(ld_amount) Amount,Left(ld_dpid,2) + 'X' ld_dpid,'C' product from " + Commexconn + ".ledger b, " + Commexconn + ".Client_master, " + Commexconn + ".Branch_master where ld_dt <= '" + req.AsOnDate + "' " + strClientWhere + loginAccess + "and b.ld_clientcd = cm_cd and cm_brboffcode = bm_branchcd and cm_cd not in(select cm_brkggroup from " + Commexconn + ".Client_master where cm_brkggroup <> '') group by cm_cd,cm_name,cm_brboffcode,bm_branchname,b.ld_dpid " + (req.Selection.IncZeroBalance ? "" : "having SUM(b.ld_amount) <> 0 ");
                }
                strsql += ") a ";
                strsql += "group by cm_cd,cm_name,cm_brboffcode,bm_branchname,ld_dpid,product) d group by cm_cd,cm_name,cm_brboffcode,bm_branchname order by cm_cd";

                DataTable dt = objUtility.OpenDataTable(strsql);
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        var dict = new Dictionary<string, object>();
                        OutstandingBalanceModel data = new OutstandingBalanceModel();
                        data.ClientCode = dr["cm_cd"].ToString().Trim();
                        data.ClientName = dr["cm_name"].ToString().Trim();
                        data.BranchCode = dr["cm_brboffcode"].ToString().Trim();
                        data.BranchName = dr["bm_branchname"].ToString().Trim();
                        foreach (DataColumn dc in dt.Columns)
                        {
                            if (strExch.Contains(dc.ColumnName) || dc.ColumnName == "Total")
                            {
                                dict.Add(dc.ColumnName, Convert.ToDouble(dr[dc.ColumnName]));
                            }
                        }
                        data.Data = dict;
                        outstandingBalance.Add(data);
                    }
                    return outstandingBalance;
                }
                return null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic BrokerageCashSegment(string client, string scheme, string exchSeg)
        {
            try
            {
                BrokerageReponseModel brokerageReponse = new BrokerageReponseModel();

                Boolean blnOrdDelvBrok = false;
                Boolean blnDeliveryBrk = false;
                Boolean blnAdd = false;
                string strSameday = "No Reduced Brokerage";
                string strSamesett = "No Reduced Brokerage";
                string strAdvantage = "Normal";
                string strQty = "";
                string strRoundto = "";
                string brokType = "";
                string brokTypeText = "";
                string tmpbrokType = "";

                if (!string.IsNullOrEmpty(client))
                {
                    strsql = "select ce_brkscheme from client_details where ce_clientcd='" + client.Trim() + "' and ce_companycode= '" + exchSeg + "'";
                    DataTable dt = objUtility.OpenDataTable(strsql);
                    if (dt.Rows.Count > 0)
                    {
                        scheme = dt.Rows[0][0].ToString().Trim();
                    }
                    else
                    {
                        return "Client ID Not Found";
                    }
                }

                if (string.IsNullOrWhiteSpace(scheme))
                {
                    return "Invalid parameter,enter Client/Scheme";
                }

                string strSql = "select * from Brokerages where br_companycode = '" + exchSeg + "' and br_scheme='" + scheme + "'";
                DataTable rstemp = objUtility.OpenDataTable(strSql);

                if (rstemp.Rows.Count > 0)
                {
                    brokType = rstemp.Rows[0]["br_BrokType"].ToString().Trim();
                    tmpbrokType = rstemp.Rows[0]["br_BrokType"].ToString().Trim();
                }
                else
                {
                    return "No records found";
                }

                brokTypeText = brokType switch
                {
                    "T" => "Total Turnover",
                    "S" => "Scripwise Turnover",
                    "O" => "Order Wise (Old)",
                    "P" => "Order Wise",
                    "X" => "Buy/Sell",
                    _ => "",
                };

                //usefull
                int intdec = 0;
                string strTemp = "";
                DataTable rsOther = new DataTable();

                intdec = Convert.ToInt32(objUtility.fnFireQuery("syscolumns,sysobjects", "syscolumns.scale", "sysobjects.id=syscolumns.id and sysobjects.name='Brokerages' and syscolumns.name", "br_percent", true));

                var exch = exchSeg.Substring(1, 1);
                strSql = fnGetSql(scheme, exch, brokType, exchSeg);
                rstemp = objUtility.OpenDataTable(strSql);

                DataTable brokData = new DataTable();


                strTemp = "";
                if (rstemp.Rows.Count == 0 || (scheme.Trim() == "" && blnAdd == false))
                {
                    return "No Record found";
                }
                else
                {
                    if (blnAdd)
                    {
                        //cmbSameday = ("Buy/Sell".Contains(brokType) || (fnGetOrderWise(brokType) == "O") || (fnGetOrderWise(brokType) == "P") ? "No Reduced Brokerage" : "Lower Quantity");
                        strSameday = brokType.Trim() == "X" || (brokType.Trim() == "O") || (brokType.Trim() == "P") ? "No Reduced Brokerage" : "Lower Quantity";
                        strSamesett = "No Reduced Brokerage";
                    }
                    else
                    {
                        strSql = "select * from Brokerages where br_companycode = '" + exchSeg + "' and br_scheme = '" + scheme + "'";
                        rsOther = objUtility.OpenDataTable(strSql);

                        if (rsOther.Rows.Count == 0)
                        {
                            strQty = "0";
                            strRoundto = "0.1";
                            strTemp = "N";
                            brokType = "Normal";
                        }
                        else
                        {
                            //prFillBrokageTypeCombo(Trim(CStr(rsOther.Rows(0).Item("br_BrokType"))) = "O");
                            if (rsOther.Rows[0]["br_BrokType"].ToString().Trim().ToUpper() == "T")
                                brokType = "Total Turnover";
                            else if (rsOther.Rows[0]["br_BrokType"].ToString().Trim().ToUpper() == "S")
                                brokType = "Scripwise Turnover";
                            else if (rsOther.Rows[0]["br_BrokType"].ToString().Trim().ToUpper() == "O")
                                brokType = "Order Wise (Old)";
                            else if (rsOther.Rows[0]["br_BrokType"].ToString().Trim().ToUpper() == "P")
                                brokType = "Order Wise";
                            else if (rsOther.Rows[0]["br_BrokType"].ToString().Trim().ToUpper() == "X")
                                brokType = "Buy/Sell";
                            else
                                brokType = "Normal";


                            strTemp = rsOther.Rows[0]["br_sdremove"].ToString().Trim().ToUpper();
                            if (strTemp == "N")
                            {
                                strSameday = "No Reduced Brokerage";
                                strSamesett = "No Reduced Brokerage";
                            }
                            else if (strTemp == "B")
                            {
                                strSameday = "Buy Trades";
                                strSamesett = "Buy Trades";
                            }
                            else if (strTemp == "S")
                            {
                                strSameday = "Sell Trades";
                                strSamesett = "Sell Trades";
                            }
                            else if (strTemp == "L")
                            {
                                strSameday = "Lower Quantity";
                                strSamesett = "Lower Quantity";
                            }
                            else if (strTemp == "R")
                                strSameday = "Rate Based";


                            if (tmpbrokType.Trim() == "O" || tmpbrokType.Trim() == "P")
                            {
                                strTemp = rsOther.Rows[0]["br_delvbrk"].ToString().Trim().ToUpper();
                                blnOrdDelvBrok = (strTemp == "Y");
                            }
                            else
                            {
                                strTemp = rsOther.Rows[0]["br_delvbrk"].ToString().Trim().ToUpper();
                                blnDeliveryBrk = (strTemp == "T");
                            }

                            strTemp = rsOther.Rows[0]["br_roffadvantage"].ToString().Trim().ToUpper();
                            if (strTemp == "N")
                                strAdvantage = "Normal";
                            else if (strTemp == "C")
                                strAdvantage = "Client";
                            else if (strTemp == "B")
                                strAdvantage = "Broker";

                            strQty = rsOther.Rows[0]["br_minsqroffqty"].ToString();
                            strRoundto = rsOther.Rows[0]["br_roffnearest"].ToString();
                        }
                    }
                }

                rsOther = new DataTable();
                strTemp = "";

                strSql = "select rtrim(cm_cd) ClientCode , rtrim(cm_name) Name  from Client_master, Client_details where cm_cd=ce_clientcd and ce_companycode='" + exchSeg + "' and ce_brkscheme='" + scheme + "'";
                var clientRecord = objUtility.OpenDataTable(strSql);

                if (tmpbrokType == "P")
                {
                    BrokerageCashHeader brokerageHeadermodel = new BrokerageCashHeader()
                    { SchemeCode = scheme, ExchSeg = exchSeg, BrokerageType = brokType, AdvantageOf = strAdvantage, Quantity = Convert.ToDouble(strQty), BrokerageRounding = Convert.ToDouble(strRoundto), IsDeliveryBrokerage = blnOrdDelvBrok, PercentageOfTradeValue = 0, IsMax = false };

                    BrokerageCashOrderByReponseModel brokerageCashOrderResponse = new BrokerageCashOrderByReponseModel();

                    List<BrokerageCashOrderByDetails> brokerageCashOrders = new List<BrokerageCashOrderByDetails>();

                    for (int i = 0; i < rstemp.Rows.Count; i++)
                    {
                        brokerageCashOrders.Add(new BrokerageCashOrderByDetails
                        {
                            Type = rstemp.Rows[i]["sy_desc"].ToString().Trim(),
                            BuyOrders = Convert.ToDouble(rstemp.Rows[i]["BuyOrder"]),
                            SellOrders = Convert.ToDouble(rstemp.Rows[i]["SellOrder"]),
                            Minimum = Convert.ToDouble(rstemp.Rows[i]["br_min"]),
                            Percentage = Convert.ToDouble(rstemp.Rows[i]["br_percent"]),
                        });

                        if (rstemp.Rows[0]["brtype"].ToString() == "CEB" || rstemp.Rows[0]["brtype"].ToString() == "CES")
                        {
                            if (Convert.ToDouble(rstemp.Rows[0]["MaxPerOrder"]) > 0)
                            {
                                brokerageHeadermodel.IsMax = true;
                                brokerageHeadermodel.PercentageOfTradeValue = Convert.ToDouble(rstemp.Rows[0]["MaxPerOrder"]);
                            }
                            else
                            {
                                brokerageHeadermodel.IsMax = false;
                                brokerageHeadermodel.PercentageOfTradeValue = 0;
                            }
                        }
                    }

                    brokerageCashOrderResponse.ClientList = clientRecord;
                    brokerageCashOrderResponse.Header = brokerageHeadermodel;
                    brokerageCashOrderResponse.Detail = brokerageCashOrders;

                    return brokerageCashOrderResponse;
                }
                else
                {

                    List<TempBrokerageCashRecord> tempBrokerageRecords = new List<TempBrokerageCashRecord>();

                    BrokerageHeader brokerageHeadermodel = new BrokerageHeader
                    { SchemeCode = scheme, ExchSeg = exchSeg, BrokerageType = brokType, AdvantageOf = strAdvantage, Quantity = Convert.ToDouble(strQty), BrokerageRounding = Convert.ToDouble(strRoundto), IsDeliveryBrokerage = blnOrdDelvBrok, DuringTheDay = strSameday, SameSett = strSamesett };

                    for (int i = 0; i < rstemp.Rows.Count; i++)
                    {
                        TempBrokerageCashRecord brokerage = new TempBrokerageCashRecord()
                        {
                            Category = rstemp.Rows[i]["sy_desc"].ToString().Trim(),
                            Type = rstemp.Rows[i]["brdesc"].ToString(),
                            Fixed = rstemp.Rows[i]["br_fixed"].ToString(),
                            Maximum = rstemp.Rows[i]["br_max"].ToString(),
                            Minimum = rstemp.Rows[i]["br_min"].ToString(),
                            MinPerContract = rstemp.Rows[i]["br_minpercontract"].ToString(),
                            Percent = rstemp.Rows[i]["br_percent"].ToString(),
                            UpTo = rstemp.Rows[i]["br_upto"].ToString()
                        };
                        tempBrokerageRecords.Add(brokerage);
                    }

                    List<BrokerageCashRecordCategory> brokerageRecords = new List<BrokerageCashRecordCategory>();

                    List<string> categories = tempBrokerageRecords.Select(x => x.Category).Distinct().ToList();

                    foreach (var category in categories)
                    {
                        var brokerageDetails = tempBrokerageRecords.Where(x => x.Category == category).ToList();
                        List<BrokerageCashRecord> brokerageRecord = new List<BrokerageCashRecord>();

                        BrokerageCashRecordCategory tempBrokerageFoRecord = new BrokerageCashRecordCategory();
                        tempBrokerageFoRecord.CategoryName = category;

                        foreach (var brokerageDetail in brokerageDetails)
                        {
                            BrokerageCashRecord brokerage = new BrokerageCashRecord()
                            {
                                Type = brokerageDetail.Type,
                                UpTo = brokerageDetail.UpTo,
                                Minimum = brokerageDetail.Minimum,
                                Percent = brokerageDetail.Percent,
                                Fixed = brokerageDetail.Fixed,
                                Maximum = brokerageDetail.Maximum,
                                MinPerContract = brokerageDetail.MinPerContract,
                            };
                            brokerageRecord.Add(brokerage);
                        }
                        tempBrokerageFoRecord.Details = brokerageRecord;
                        brokerageRecords.Add(tempBrokerageFoRecord);
                    }

                    brokerageReponse.ClientList = clientRecord;
                    brokerageReponse.Header = brokerageHeadermodel;
                    brokerageReponse.Detail = brokerageRecords;

                    return brokerageReponse;
                }

            }
            catch (Exception)
            {
                throw;
            }
        }

        public dynamic BrokerageFOSegment(string mode, string scheme, string exchSeg, string client)
        {
            try
            {
                string strTempOrderWise = string.Empty;
                string strSql = string.Empty;
                string strTemp = string.Empty;
                DataTable rstemp = new DataTable();
                BrokerageFOResponseModel responseModel = new BrokerageFOResponseModel();
                BrokerageOrderFOResponseModel orderResponseModel = new BrokerageOrderFOResponseModel();

                //if (string.IsNullOrEmpty(selectType))
                //{
                //    return "Please select type";
                //}
                //if (selectType.Trim().ToUpper() == "CLIENT" && string.IsNullOrEmpty(client))
                //{
                //    return "Please enter client id";
                //}

                if (!string.IsNullOrEmpty(client))
                {
                    strsql = "select ce_brkscheme from client_details where ce_clientcd='" + client.Trim() + "' and ce_companycode= '" + exchSeg + "'";
                    DataTable dt = objUtility.OpenDataTable(strsql);
                    if (dt.Rows.Count > 0)
                    {
                        scheme = dt.Rows[0][0].ToString().Trim();
                    }
                    else
                    {
                        return "Client ID Not Found";
                    }
                }

                if (string.IsNullOrWhiteSpace(scheme))
                {
                    return "Invalid parameter,enter Client/Scheme";
                }

                int intdec = int.Parse(objUtility.fnFireQuery("syscolumns,sysobjects", "syscolumns.scale", "sysobjects.id=syscolumns.id and sysobjects.name='FBrokerage_master' and syscolumns.name", "fb_roffnearest", true));
                intdec = (intdec == 6) ? 6 : 5;

                string connetionString = objUtility.GetConnectionStr();
                using (SqlConnection con = new SqlConnection(connetionString))
                {
                    con.Open();

                    string strTmpTable = "#tmptblfBrokerage" + System.Math.Abs(System.Threading.Thread.CurrentThread.ManagedThreadId);
                    prCreateTempTable2(strTmpTable, con);

                    //string compId = exchSeg;
                    string exchange = exchSeg.Substring(1, 1);
                    string segment = exchSeg.Substring(2, 1);

                    if (mode != "ADD")
                    {
                        strSql = " Insert into " + strTmpTable;
                        strSql += " select fb_sdremove, fb_adremove,fb_minpercontract, fb_FixbrkType,fb_roffnearest,fb_roundoffadvantage ,bt_category,bt_brokdesc,isnull(br_upto,999999) br_upto,br_min1,br_percent1,br_perlot1,br_max1,br_min2,br_percent2,br_perlot2,br_max2 ";
                        strSql += " ,br_min3,br_percent3,br_max3,br_perlot3,bt_brokgroup, bt_allowsqr,";
                        strSql += " case when bt_allowsqr >= 1 then 'Y' else 'N' end as Side1,";
                        strSql += " case when bt_allowsqr >= 2 then 'Y' else 'N' end as Side2,";
                        strSql += " case when bt_allowsqr >= 3 then 'Y' else 'N' end as Side3, bt_order, fb_minpercontractOpt ";
                        strSql += " from fBrokerage_types a , fBrokerages b, FBrokerage_master c ";
                        strSql += " where b.br_companycode = fb_companycode ";
                        strSql += " and b.br_scheme = c.fb_scheme And b.br_scheme = '" + scheme + "'";
                        strSql += " and b.br_companycode = '" + exchSeg + "'";
                        strSql += " and a.bt_brokgroup = b.br_brokgroup ";
                        strSql += " and ";
                        if (segment == "F")
                            strSql += " a.bt_Type = 'E' ";
                        else if (segment == "X")
                            strSql += " a.bt_Type = 'X' ";
                        else
                            strSql += " a.bt_Type = 'C' ";
                        strSql += " Order by bt_order, br_upto ";
                        objUtility.ExecuteSQLTmp(strSql, con);
                    }

                    strSql = " Insert into " + strTmpTable;
                    strSql += " select 'L'fb_sdremove, 'L'fb_adremove,0 fb_minpercontract, 0 fb_FixbrkType,'.0001' fb_roffnearest,'N'fb_roundoffadvantage ,bt_category,bt_brokdesc,999999 br_upto,0 br_min1,0 br_percent1,0 br_perlot1,0 br_max1,0 br_min2,0 br_percent2,0 br_perlot2,0 br_max2  ,0 br_min3,0 br_percent3,0 br_max3,0 br_perlot3,bt_brokgroup, bt_allowsqr, case when bt_allowsqr >= 1 then 'Y' else 'N' end as Side1, case when bt_allowsqr >= 2 then 'Y' else 'N' end as Side2, case when bt_allowsqr >= 3 then 'Y' else 'N' end as Side3, bt_order, 0 fb_minpercontractOpt   ";
                    strSql += " from fBrokerage_types a where ";
                    if (segment == "F")
                        strSql += " a.bt_Type = 'E' ";
                    else if (segment == "X")
                        strSql += " a.bt_Type = 'X' ";
                    else
                        strSql += " a.bt_Type = 'C' ";
                    strSql += " and bt_brokgroup not in (select bt_brokgroup from " + strTmpTable + ")";
                    strSql += " Order by bt_order, br_upto ";
                    objUtility.ExecuteSQLTmp(strSql, con);

                    strSql = "Select fb_sdremove, fb_adremove,fb_FixBrkType, fb_roffnearest, ";
                    strSql += " fb_minpercontract, fb_roundoffadvantage , fb_minpercontractOpt,fb_minpercontractOpt, fb_MinPerLotSquarOff ";
                    strSql += " from fBrokerage_master  ";
                    strSql += " where fb_companycode = '" + exchSeg + "'";
                    strSql += " and fb_scheme = '" + scheme + "'";
                    rstemp = new DataTable();
                    rstemp = objUtility.OpenDataTableTmp(strSql, con);
                    strTemp = "";

                    if (rstemp.Rows.Count == 0 && mode != "ADD")
                    {
                        return "Record not found";
                    }
                    else
                    {
                        if (mode == "ADD")
                        {
                            Header brokerageFOHeader = new Header
                            {
                                SchemeCode = scheme,
                                ExchSeg = exchSeg,
                                Advantage = "Normal",
                                AdvantageId = 0,
                                AnyDay = "No Reduced Brokerage",
                                AnyDayId = 0,
                                MinPerContract = "0",
                                MinPerContractOpt = "0",
                                SameDay = "Sell Trades",
                                SameDayId = 2,
                                RoundTo = (intdec == 6) ? "0.000001" : "0.0001"
                            };

                            responseModel.Header = brokerageFOHeader;
                        }
                        else
                        {
                            Header brokerageFOHeader = new Header();

                            brokerageFOHeader.SchemeCode = scheme;
                            brokerageFOHeader.ExchSeg = exchSeg;
                            strTemp = rstemp.Rows[0]["fb_sdremove"].ToString().Trim().ToUpper();

                            if (strTemp == "N")
                            {
                                brokerageFOHeader.SameDay = "No Reduced Brokerage";
                                strTempOrderWise = "No Reduced Brokerage";
                                brokerageFOHeader.SameDayId = 0;
                            }
                            else if (strTemp == "B")
                            {
                                brokerageFOHeader.SameDay = "Buy Trades";
                                strTempOrderWise = "Buy Trades";
                                brokerageFOHeader.SameDayId = 1;
                            }
                            else if (strTemp == "S")
                            {
                                brokerageFOHeader.SameDay = "Sell Trades";
                                strTempOrderWise = "Sell Trades";
                                brokerageFOHeader.SameDayId = 2;
                            }
                            else if (strTemp == "L")
                            {
                                brokerageFOHeader.SameDay = "Lower Quantity";
                                strTempOrderWise = "Lower Quantity";
                                brokerageFOHeader.SameDayId = 3;
                            }
                            else if (strTemp == "R")
                            {
                                brokerageFOHeader.SameDay = "Rate Based";
                                strTempOrderWise = "Rate Based";
                                brokerageFOHeader.SameDayId = 4;
                            }
                            else if (strTemp == "O")
                            {
                                brokerageFOHeader.SameDay = "OrderWise";
                                strTempOrderWise = "OrderWise";
                                brokerageFOHeader.SameDayId = 5;
                            }

                            strTemp = rstemp.Rows[0]["fb_adremove"].ToString().Trim().ToUpper();
                            if (strTemp == "N")
                            {
                                brokerageFOHeader.AnyDay = "No Reduced Brokerage";
                                brokerageFOHeader.AnyDayId = 0;
                            }
                            else if (strTemp == "B")
                            {
                                brokerageFOHeader.AnyDay = "Buy Trades";
                                brokerageFOHeader.AnyDayId = 1;
                            }
                            else if (strTemp == "S")
                            {
                                brokerageFOHeader.AnyDay = "Sell Trades";
                                brokerageFOHeader.AnyDayId = 2;
                            }
                            else if (strTemp == "L")
                            {
                                brokerageFOHeader.AnyDay = "Lower Quantity";
                                brokerageFOHeader.AnyDayId = 3;
                            }
                            else if (strTemp == "R")
                            {
                                brokerageFOHeader.AnyDay = "Rate Based";
                                brokerageFOHeader.AnyDayId = 4;
                            }

                            if (intdec == 6)
                                brokerageFOHeader.RoundTo = Convert.ToDouble(rstemp.Rows[0]["fb_roffnearest"]).ToString("0.000000");
                            else
                                brokerageFOHeader.RoundTo = Convert.ToDouble(rstemp.Rows[0]["fb_roffnearest"]).ToString();

                            brokerageFOHeader.MinPerContract = Convert.ToString(rstemp.Rows[0]["fb_minpercontract"]);
                            brokerageFOHeader.MinPerContractOpt = Convert.ToString(rstemp.Rows[0]["fb_minpercontractOpt"]);
                            brokerageFOHeader.IsPerLotBrokerage = Convert.ToDouble(rstemp.Rows[0]["fb_FixBrkType"]) == 1 ? true : false;

                            strTemp = Convert.ToString(rstemp.Rows[0]["fb_roundoffadvantage"]).Trim().ToUpper();
                            if (strTemp == "N")
                            {
                                brokerageFOHeader.Advantage = "Normal";
                                brokerageFOHeader.AdvantageId = 0;
                            }
                            else if (strTemp == "C")
                            {
                                brokerageFOHeader.Advantage = "Client";
                                brokerageFOHeader.AdvantageId = 1;
                            }
                            else if (strTemp == "B")
                            {
                                brokerageFOHeader.Advantage = "Broker";
                                brokerageFOHeader.AdvantageId = 2;
                            }

                            if (strTempOrderWise.ToUpper() == "ORDERWISE")
                            {
                                orderResponseModel.Header = brokerageFOHeader;
                            }
                            else
                            {
                                responseModel.Header = brokerageFOHeader;
                            }
                        }
                    }

                    strSql = "select cm_cd, cm_name   from Client_master, Client_details where cm_cd=ce_clientcd and ce_companycode='" + exchSeg + "' and ce_brkscheme='" + scheme + "'";
                    DataTable adoClientRecordset = new DataTable();
                    adoClientRecordset = objUtility.OpenDataTable(strSql);

                    if (strTempOrderWise.ToUpper() == "ORDERWISE")
                    {
                        orderResponseModel.ClientList = new List<ClientList>();

                        for (int i = 0; i < adoClientRecordset.Rows.Count; i++)
                        {
                            orderResponseModel.ClientList.Add(new ClientList() { ClientCode = adoClientRecordset.Rows[i]["cm_cd"].ToString().Trim(), Name = adoClientRecordset.Rows[i]["cm_name"].ToString().Trim() });
                        }
                    }
                    else
                    {
                        responseModel.ClientList = new List<ClientList>();

                        for (int i = 0; i < adoClientRecordset.Rows.Count; i++)
                        {
                            responseModel.ClientList.Add(new ClientList() { ClientCode = adoClientRecordset.Rows[i]["cm_cd"].ToString().Trim(), Name = adoClientRecordset.Rows[i]["cm_name"].ToString().Trim() });
                        }

                    }

                    strSql = " Select * from " + strTmpTable + " Order by bt_order, isnull(br_upto,99999999)";
                    rstemp = objUtility.OpenDataTableTmp(strSql, con);

                    #region create temp list
                    List<TempBrokerageRecord> tempBrokerageRecords = new List<TempBrokerageRecord>();

                    for (int i = 0; i < rstemp.Rows.Count; i++)
                    {
                        tempBrokerageRecords.Add(new TempBrokerageRecord()
                        {
                            Type = rstemp.Rows[i]["bt_brokdesc"].ToString().Trim(),
                            Upto = rstemp.Rows[i]["br_upto"].ToString().Trim(),
                            Category = rstemp.Rows[i]["bt_category"].ToString().Trim(),
                            FSideMin = rstemp.Rows[i]["br_min1"].ToString().Trim(),
                            FSidePercent = rstemp.Rows[i]["br_percent1"].ToString().Trim(),
                            FSidePerlot = rstemp.Rows[i]["br_perlot1"].ToString().Trim(),
                            FSideMax = rstemp.Rows[i]["br_max1"].ToString().Trim(),
                            SecSideMin = rstemp.Rows[i]["br_min2"].ToString().Trim(),
                            SecSidePercent = rstemp.Rows[i]["br_percent2"].ToString().Trim(),
                            SecSidePerlot = rstemp.Rows[i]["br_perlot2"].ToString().Trim(),
                            SecSideMax = rstemp.Rows[i]["br_max2"].ToString().Trim(),
                            AnySideMin = rstemp.Rows[i]["br_min3"].ToString().Trim(),
                            AnySidePercent = rstemp.Rows[i]["br_percent3"].ToString().Trim(),
                            AnySidePerlot = rstemp.Rows[i]["br_perlot3"].ToString().Trim(),
                            AnySideMax = rstemp.Rows[i]["br_max3"].ToString().Trim(),
                        });
                    }
                    #endregion

                    List<BrokerageFORecordCategory> brokerageRecords = new List<BrokerageFORecordCategory>();
                    List<BrokerageOrderFORecordCategory> orderBrokerageRecoreds = new List<BrokerageOrderFORecordCategory>();

                    List<string> categories = tempBrokerageRecords.Select(x => x.Category).Distinct().ToList();

                    foreach (var category in categories)
                    {
                        var brokerageDetails = tempBrokerageRecords.Where(x => x.Category == category).ToList();

                        if (strTempOrderWise.ToUpper() == "ORDERWISE")
                        {
                            List<BrokerageOrderFORecord> brokerageRecord = new List<BrokerageOrderFORecord>();

                            BrokerageOrderFORecordCategory tempBrokerageFoRecord = new BrokerageOrderFORecordCategory();
                            tempBrokerageFoRecord.CategoryName = category;

                            foreach (var brokerageDetail in brokerageDetails)
                            {
                                BrokerageOrderFORecord brokerage = new BrokerageOrderFORecord()
                                {
                                    Type = brokerageDetail.Type,
                                    Upto = brokerageDetail.Upto,
                                    Fixed = brokerageDetail.FSidePerlot
                                };
                                brokerageRecord.Add(brokerage);
                            }
                            tempBrokerageFoRecord.Details = brokerageRecord;
                            orderBrokerageRecoreds.Add(tempBrokerageFoRecord);
                        }
                        else
                        {
                            List<BrokerageFORecord> brokerageRecord = new List<BrokerageFORecord>();

                            BrokerageFORecordCategory tempBrokerageFoRecord = new BrokerageFORecordCategory();
                            tempBrokerageFoRecord.CategoryName = category;

                            foreach (var brokerageDetail in brokerageDetails)
                            {
                                BrokerageFORecord brokerage = new BrokerageFORecord()
                                {
                                    Type = brokerageDetail.Type,
                                    Upto = brokerageDetail.Upto,
                                    SameDay1stSide = new BrokerageDayWiseRecord
                                    {
                                        Min = brokerageDetail.FSideMin,
                                        Percent = brokerageDetail.FSidePercent,
                                        Perlot = brokerageDetail.FSidePerlot,
                                        Max = brokerageDetail.FSideMax,
                                    },
                                    SameDay2ndSide = new BrokerageDayWiseRecord
                                    {
                                        Min = brokerageDetail.SecSideMin,
                                        Percent = brokerageDetail.SecSidePercent,
                                        Perlot = brokerageDetail.SecSidePerlot,
                                        Max = brokerageDetail.SecSideMax,
                                    },
                                    AnyDaySide = new BrokerageDayWiseRecord
                                    {
                                        Min = brokerageDetail.AnySideMin,
                                        Percent = brokerageDetail.AnySidePercent,
                                        Perlot = brokerageDetail.AnySidePerlot,
                                        Max = brokerageDetail.AnySideMax,
                                    },
                                };
                                brokerageRecord.Add(brokerage);
                            }
                            tempBrokerageFoRecord.Details = brokerageRecord;
                            brokerageRecords.Add(tempBrokerageFoRecord);
                        }
                    }

                    if (strTempOrderWise.ToUpper() == "ORDERWISE")
                    {
                        orderResponseModel.Detail = orderBrokerageRecoreds;
                        orderResponseModel.ProductBrokerage = ProductWiseBrokerage(scheme, exchSeg).ProductBrokerageRecord;
                        return orderResponseModel;
                    }
                    else
                    {
                        responseModel.Detail = brokerageRecords;
                        responseModel.ProductBrokerage = ProductWiseBrokerage(scheme, exchSeg).ProductBrokerageRecord;
                        return responseModel;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public ProductWiseBrokerageResponse ProductWiseBrokerage(string cmbScheme, string cmbExchSeg)
        {
            ProductWiseBrokerageResponse brokerageResponse = new ProductWiseBrokerageResponse();
            DataTable rsX = new DataTable();
            string strSql = string.Empty;

            string _gDPID = cmbExchSeg;
            string exchange = cmbExchSeg.Substring(1, 1);
            string segment = cmbExchSeg.Substring(2, 1);

            string connectionString = objUtility.GetConnectionStr();
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                prTempProduct(con);

                strSql = "Insert Into #TempProduct ";
                strSql += " Select pb_company_code,pb_Product, pb_scheme , pb_brokgroup , pb_fixed1 , pb_fixed2 , pb_fixed3, pb_per1, pb_per2, pb_per3 ,Case pb_brokgroup when 'F' then 1 else 2 end  ";
                strSql += " From Product_brokerages where pb_company_code = '" + _gDPID + "' ";
                strSql += " and pb_scheme = '" + cmbScheme + "'";
                objUtility.ExecuteSQLTmp(strSql, con);

                strSql = " Select * from #TempProduct Order by tp_Product, tp_order";
                rsX = objUtility.OpenDataTableTmp(strSql, con);

                brokerageResponse.ProductBrokerageRecord = new List<ProductWiseBrokerageRecord>();

                for (int i = 0; i < rsX.Rows.Count; i++)
                {
                    brokerageResponse.ProductBrokerageRecord.Add(
                        new ProductWiseBrokerageRecord
                        {
                            Product = rsX.Rows[i]["tp_Product"].ToString().Trim(),
                            Type = GetTradingType(rsX.Rows[i]["tp_brokgroup"].ToString().Trim()),
                            SD1 = new ProductWiseBrokerageDayWise { Percent = rsX.Rows[i]["tp_per1"].ToString().Trim(), Perlot = rsX.Rows[i]["tp_fixed1"].ToString().Trim() },
                            SD2 = new ProductWiseBrokerageDayWise { Percent = rsX.Rows[i]["tp_per2"].ToString().Trim(), Perlot = rsX.Rows[i]["tp_fixed2"].ToString().Trim() },
                            ADS = new ProductWiseBrokerageDayWise { Percent = rsX.Rows[i]["tp_per3"].ToString().Trim(), Perlot = rsX.Rows[i]["tp_fixed3"].ToString().Trim() }
                        });
                }

                //strSql = "select distinct pm_assetcd from Product_master Where pm_exchange = '" + exchange + "' and pm_segment = '" + segment + "'";
                //strSql += " and pm_assetcd not in (Select tp_product from #TempProduct) ";
                //strSql += " Order By pm_assetcd  ";
                //DataTable rstemp = objUtility.OpenDataTableTmp(strSql, con);

                //brokerageResponse.ProductAssetCd = new List<string>();

                //for (int i = 0; i < rstemp.Rows.Count; i++)
                //{
                //    brokerageResponse.ProductAssetCd.Add(rstemp.Rows[i]["pm_assetcd"].ToString()?.Trim());
                //}
            }

            return brokerageResponse;
        }

        #region Brokerage helper method

        private string GetTradingType(string charType)
        {
            string tradeType = string.Empty;
            switch (charType)
            {
                case "F":
                    tradeType = "Future";
                    break;
                case "O":
                    tradeType = "Options";
                    break;
                case "FB":
                    tradeType = "Fut Buy";
                    break;
                case "FS":
                    tradeType = "Fut Sell";
                    break;
                case "OB":
                    tradeType = "Opt Buy";
                    break;
                case "OS":
                    tradeType = "Opt Sell";
                    break;
            }

            return tradeType;
        }

        private string fnGetSql(string scheme, string exchange, string brokType, string exchSeg)
        {
            string strSQL = "";
            if (brokType == "O")
                strSQL = "select *, 'SD1' as brtype, 'All Trades' as brdesc, 1 as ordertype, Case sy_maptype When 'N' then 1 when 'C' then 2 when 'R' then 3 else 4 end as Ordr from Settlement_type left join Brokerages on (br_settgroup= sy_maptype and br_companycode = '" + exchSeg + "' and br_scheme='" + scheme + "' and br_type='SD1') where sy_exchange='" + exchange + "' and sy_maptype not in ('') and sy_type not in ('Q','U') Order By Ordr ";
            else if (brokType == "P")
            {
                strSQL = "select Case sy_maptype When 'N' then 1 else 2 end as Ordr,sy_exchange,sy_maptype,sy_desc,br_minpercontract, 'CEB' as brtype, 'All Trades' as brdesc, 1 as ordertype, Max(Case br_type when 'CEB' then br_fixed else 0 end) as BuyOrder, Max(Case br_type when 'CES' then br_fixed else 0 end) as SellOrder, Max(Case br_type when 'DLV' then br_percent else 0 end) br_percent, Max(Case br_type when 'DLV' then br_min else 0 end) br_min , ";
                strSQL += "max(br_fixed) br_fixed, Max(br_max) br_max, max(br_minpercontract) br_minpercontract, max(br_upto) br_upto, Max(Case when br_type in ('CEB','CES') then br_percent else 0 end) as MaxPerOrder ";
                strSQL += "from Settlement_type left join Brokerages on (br_settgroup= sy_maptype and br_companycode = '" + exchSeg + "' and br_scheme='" + scheme + "' and br_type in ('CEB','CES','DLV')) where sy_exchange='" + exchange + "' and sy_maptype in ('N','C') and sy_maptype not in ('') and sy_type not in ('Q','U') " + "\r\n";
                strSQL += "Group By sy_exchange,sy_maptype,sy_desc,br_minpercontract " + "\r\n";
                strSQL += "Union All" + "\r\n";
                strSQL += "select Case sy_maptype when 'R' then 3 else 4 end as Ordr, sy_exchange,sy_maptype,sy_desc,br_minpercontract, 'DLV' as brtype, 'All Trades' as brdesc, 1 as ordertype, 0 BuyOrder,0 SellOrder,br_percent , br_min , br_fixed,  br_max,  br_minpercontract, br_upto, 0 as MaxPerOrder from Settlement_type left join Brokerages on (br_settgroup= sy_maptype and br_companycode = '" + exchSeg + "' and br_scheme='" + scheme + "' and br_type='DLV') where sy_exchange='" + exchange + "' and sy_maptype not in ('N','C') and sy_maptype not in ('') and sy_type not in ('Q','U') " + "\r\n";
                strSQL += "Order By Ordr";
            }
            else if (brokType == "X")
            {
                strSQL = "select *, case when sy_maptype in ('N','C') then 'CEB' else 'SD1' end as brtype,case when sy_maptype in ('N','C') then 'Equity Buy' else 'All Trades' end as brdesc, 1 as ordertype from Settlement_type left join Brokerages on (br_settgroup= sy_maptype and br_companycode = '" + exchSeg + "' and br_scheme='" + scheme + "' and br_type in ('CEB','SD1')) " + "\r\n";
                strSQL += " where sy_exchange='" + exchange + "' and sy_maptype not in ('') and sy_type not in ('Q','U') " + "\r\n";
                strSQL += " union " + "\r\n";
                strSQL += " select *,case when sy_maptype in ('N','C') then 'CES' else 'SD1' end as brtype, 'Equity Sell' as brdesc,2 as ordertype from Settlement_type left join Brokerages on (br_settgroup= sy_maptype and br_companycode = '" + exchSeg + "' and br_scheme='" + scheme + "' and br_type in ('CES','SD1')) " + "\r\n";
                strSQL += " where sy_exchange='" + exchange + "'  and sy_maptype in('N','C')  and sy_maptype not in ('') and sy_type not in ('Q','U')  " + "\r\n";
                strSQL += " order by sy_srno,ordertype, br_upto";
            }
            else
            {
                strSQL = "select *, 'SD1' as brtype,case when sy_maptype = 'C' then 'Delivery' when sy_maptype in ('N','F') then 'Same Day 1st Side' else 'All Trades' end as brdesc, 1 as ordertype,case sy_maptype When 'N' Then 0 When 'C' Then 1 When 'R' Then 2 else 3 end sy_maptype2 from Settlement_type left join Brokerages on (br_settgroup= sy_maptype and br_companycode = '" + exchSeg + "' and br_scheme='" + scheme + "' and br_type='SD1') " + "\r\n";
                strSQL += " where sy_exchange='" + exchange + "' and sy_maptype not in ('') and sy_type not in ('Q','U') " + "\r\n";
                strSQL += " union " + "\r\n";
                strSQL += " select *,'SD2'  as brtype,'Same Day 2nd Side' as brdesc,2 as ordertype,case sy_maptype When 'N' Then 0 When 'C' Then 1 When 'R' Then 2 else 3 end sy_maptype2 from Settlement_type left join Brokerages on (br_settgroup= sy_maptype and br_companycode = '" + exchSeg + "' and br_scheme='" + scheme + "' and br_type='SD2') ";
                strSQL += " where sy_exchange='" + exchange + "'  and sy_maptype in('N','F') and sy_maptype not in ('') and sy_type not in ('Q','U')  " + "\r\n";
                strSQL += " union " + "\r\n";
                strSQL += " select *,'DLV'  as brtype,'Delivery' as brdesc,4 as ordertype,case sy_maptype When 'N' Then 0 When 'C' Then 1 When 'R' Then 2 else 3 end sy_maptype2 from Settlement_type left join Brokerages on (br_settgroup= sy_maptype  and br_companycode = '" + exchSeg + "' and br_scheme='" + scheme + "'  and br_type='DLV' ) where sy_exchange='" + exchange + "'  and sy_maptype in('N','F') and sy_maptype not in ('') and sy_type not in ('Q','U')  " + "\r\n";
                strSQL += " order by sy_maptype2,sy_srno,ordertype, br_upto";
            }
            return strSQL;
        }

        private string fnGetOrderWise(string CmbBrokType)
        {
            if (CmbBrokType == "Order Wise (Old)")
                return "O";
            else if (CmbBrokType == "Order Wise")
                return "P";
            else
                return "";
        }

        private string FormatCurr(string str, int intDecimal = 0, string strComma = "N")
        {
            if (!Information.IsNumeric(str))
                str = "0";

            string strR = Strings.FormatNumber(str, intDecimal);
            if (strComma == "N" | Conversion.Val(str) < 1000)
                return strR;

            string strI = strR.Split(".")[0];
            str += ".";
            string strF = strR.Split(".")[1];
            int iX = 4;
            while (iX <= strI.Length)
            {
                strI = Strings.Mid(strI, 1, strI.Length - iX + 1) + "," + Strings.Right(strI, iX - 1);
                iX += 3;
            }
            strR = strI + "." + strF;
            return strR;
        }

        private void prCreateTempTable2(string strTmpTable, SqlConnection connection)
        {
            string strSql = string.Empty;
            try
            {
                strSql = "Drop TABLE " + strTmpTable;
                objUtility.ExecuteSQLTmp(strSql, connection);
            }
            catch (Exception) { }
            finally
            {
                strSql = "CREATE TABLE " + strTmpTable + " ( ";
                strSql += "[fb_sdremove] [char](1) NULL,";
                strSql += "[fb_adremove] [char](1) NULL,";
                strSql += "[fb_minpercontract] [money] NULL,";
                strSql += "[fb_FixbrkType] [numeric](18, 0) NULL,";
                strSql += "[fb_roffnearest] [numeric](18, 6) NULL,";
                strSql += "[fb_roundoffadvantage] [char](1) NULL,";
                strSql += "[bt_category] [char](20) NOT NULL,";
                strSql += "[bt_brokdesc] [char](30) NOT NULL,";
                strSql += "[br_upto] [money] NOT NULL,";
                strSql += "[br_min1] [numeric](18, 6) NULL,";
                strSql += "[br_percent1] [numeric](18, 6) NULL,";
                strSql += "[br_perlot1] [money] NULL,";
                strSql += "[br_max1] [numeric](18, 6) NULL,";
                strSql += "[br_min2] [numeric](18, 6) NULL,";
                strSql += "[br_percent2] [numeric](18, 6) NULL,";
                strSql += "[br_perlot2] [money] NULL,";
                strSql += "[br_max2] [numeric](18, 6) NULL,";
                strSql += "[br_min3] [numeric](18, 6) NULL,";
                strSql += "[br_percent3] [numeric](18, 6) NULL,";
                strSql += "[br_max3] [numeric](18, 6) NULL,";
                strSql += "[br_perlot3] [money] NULL,";
                strSql += "[bt_brokgroup] [char](3) NOT NULL,";
                strSql += "[bt_allowsqr] [numeric](18, 0) NOT NULL,";
                strSql += "[Side1] [varchar](1) NOT NULL,";
                strSql += "[Side2] [varchar](1) NOT NULL,";
                strSql += "[Side3] [varchar](1) NOT NULL,";
                strSql += "[bt_order] [numeric](18, 0) NOT NULL,";
                strSql += "[fb_minpercontractOpt] [money] NULL,";
                strSql += "[RowNo] [int] IDENTITY (1,1) NOT NULL ) ";
                objUtility.ExecuteSQLTmp(strSql, connection);
            }
        }

        private void prTempProduct(SqlConnection connection)
        {
            string strSql = "";
            try
            {
                objUtility.ExecuteSQLTmp("Drop Table #TempProduct", connection);
            }
            catch (Exception) { }
            finally
            {
                strSql = "Create Table #TempProduct ( ";
                strSql += " tp_company_code char(3) Not null,  ";
                strSql += " tp_Product varchar(10) Not null,  ";
                strSql += " tp_scheme varchar(8) Not null,  ";
                strSql += " tp_brokgroup varchar(3) Not null,  ";
                strSql += " tp_fixed1 Money Not null,  ";
                strSql += " tp_fixed2 Money Not null,  ";
                strSql += " tp_fixed3 Money Not null,  ";
                strSql += " tp_per1 Numeric(18,6) Not null,  ";
                strSql += " tp_per2 Numeric(18,6) Not null,  ";
                strSql += " tp_per3 Numeric(18,6) Not null,  ";
                strSql += " tp_Order Numeric  ";
                strSql += " ) ";
                objUtility.ExecuteSQLTmp(strSql, connection);
            }
        }
        #endregion

        public dynamic BillSummaryCash(BillSummaryModel req, string loginAccess)
        {
            try
            {
                BillSummaryModel BlModel = new BillSummaryModel();
                string strClientWhere = "";
                string strSettWhere = "";
                if (req.SettlementNo != null)
                {
                    if (req.SettlementNo.Trim() != "")
                    {
                        strSettWhere += " and bl_stlmnt = '" + req.SettlementNo.Trim() + "' ";
                    }
                }
                if (req.Filter.Client != null)
                {
                    if (req.Filter.Client.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Client.ToArray(), "##"));
                        strClientWhere += " or cm_cd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Branch != null)
                {
                    if (req.Filter.Branch.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Branch.ToArray(), "##"));
                        strClientWhere += " or cm_brboffcode in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Group != null)
                {
                    if (req.Filter.Group.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Group.ToArray(), "##"));
                        strClientWhere += " or cm_groupcd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Family != null)
                {
                    if (req.Filter.Family.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Family.ToArray(), "##"));
                        strClientWhere += " or cm_familycd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (strClientWhere.Length > 0)
                {
                    strClientWhere = " and (" + strClientWhere.Substring(3) + ") ";
                }

                if (string.IsNullOrEmpty(req.SettlementNo))
                {
                    return "Invalid parameter,Enter Settlement";
                }
                else
                {
                    strsql = "SELECT cm_cd,cm_name,cm_groupcd,gr_desc,cm_mobile, cm_email,cm_familycd,fm_desc, cm_brboffcode,bm_branchname,cm_subbroker,RM_Name,Sum((sign(bl_amount) + 1) / 2 * bl_amount) as DueToUs, ABS(Sum(-(sign(bl_amount) - 1) / 2 * bl_amount)) as DueToYou, bl_stlmnt, bl_billno, bl_billdt,se_stdt " +
                             "FROM Bills, Client_master, Settlements, Family_master, Group_master, Branch_master, SubBrokers " +
                             "WHERE bl_clientcd = cm_cd and bl_clientcd = cm_cd and bl_stlmnt = se_stlmnt and fm_cd = cm_familycd and cm_groupcd = gr_cd and cm_brboffcode = bm_branchcd and cm_subbroker = RM_CD " + strClientWhere + strSettWhere + loginAccess +
                             "GROUP BY cm_cd,cm_name,cm_groupcd,gr_desc,cm_mobile,cm_email,cm_familycd,fm_desc, cm_brboffcode,bm_branchname,cm_subbroker,RM_Name,bl_stlmnt,bl_billno, bl_billdt,se_stdt";
                }

                DataTable ds = objUtility.OpenDataTable(strsql);

                if (ds.Rows.Count > 0)
                {
                    BillSummaryResponse billSummary = new BillSummaryResponse();
                    List<BillSummaryData> billSummaryData = new List<BillSummaryData>();
                    billSummary.Settlement = ds.Rows[0]["bl_stlmnt"].ToString().Trim();
                    billSummary.TradeDate = ds.Rows[0]["se_stdt"].ToString().Trim();
                    billSummary.SettlementDate = ds.Rows[0]["bl_billdt"].ToString().Trim();
                    for (int i = 0; i < ds.Rows.Count; i++)
                    {
                        BillSummaryData item = new BillSummaryData();
                        item.ClientCode = ds.Rows[i]["cm_cd"].ToString().Trim();
                        item.ClientName = ds.Rows[i]["cm_name"].ToString().Trim();
                        item.BranchCode = ds.Rows[i]["cm_brboffcode"].ToString().Trim();
                        item.BranchName = ds.Rows[i]["bm_branchname"].ToString().Trim();
                        item.GroupCode = ds.Rows[i]["cm_groupcd"].ToString().Trim();
                        item.GroupName = ds.Rows[i]["gr_desc"].ToString().Trim();
                        item.FamilyCode = ds.Rows[i]["cm_familycd"].ToString().Trim();
                        item.FamilyName = ds.Rows[i]["fm_desc"].ToString().Trim();
                        item.SubBrokerCode = ds.Rows[i]["cm_subbroker"].ToString().Trim();
                        item.SubBrokerName = ds.Rows[i]["RM_Name"].ToString().Trim();
                        item.BillNo = ds.Rows[i]["bl_billno"].ToString().Trim();
                        item.DueToUs = Convert.ToDouble(ds.Rows[i]["DueToUs"].ToString());
                        item.DueToYou = Convert.ToDouble(ds.Rows[i]["DueToYou"].ToString());
                        billSummaryData.Add(item);
                    }
                    billSummary.Data = billSummaryData;
                    return billSummary;

                }
                return null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic BillSummaryFO(BillSummaryFORequest req, string loginAccess)
        {
            try
            {
                string strClientWhere = "", strMain = "", strCompanyCode = "", strExchange = "", strSegment = "";
                if (req.Filter.Client != null)
                {
                    if (req.Filter.Client.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Client.ToArray(), "##"));
                        strClientWhere += " or cm_cd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Branch != null)
                {
                    if (req.Filter.Branch.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Branch.ToArray(), "##"));
                        strClientWhere += " or cm_brboffcode in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Group != null)
                {
                    if (req.Filter.Group.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Group.ToArray(), "##"));
                        strClientWhere += " or cm_groupcd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Family != null)
                {
                    if (req.Filter.Family.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Family.ToArray(), "##"));
                        strClientWhere += " or cm_familycd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (strClientWhere.Length > 0)
                {
                    strClientWhere = " and (" + strClientWhere.Substring(3) + ") ";
                }
                if (req.ExchSeg != null)
                {
                    if (req.ExchSeg.Trim() != "")
                    {
                        strMain = req.ExchSeg.Trim();
                        strCompanyCode = "'" + strMain.Substring(0, 1) + "'";
                        strExchange = "'" + strMain.Substring(1, 1) + "'";
                        strSegment = "'" + strMain.Substring(2, 1) + "'";
                    }
                }
                strsql = "select fb_clientcd as code,cm_name as name,cm_brboffcode,bm_branchname,fb_mtm as mtm,fb_premium as premium,fb_exerassign as exerassgn,fb_charges1 as charges, " +
                         "Case When fb_postmrgyn = 'Y'  then fb_margin1 else 0 end + Case When fb_postExpmrgyn = 'Y'  then fb_Expmargin1 else 0 end as Margin, " +
                         "fb_amount as drcr, fb_charges2 as brokerage " +
                         "from fbills With (nolock),Client_master,Branch_master Where fb_clientcd = cm_cd and cm_brboffcode = bm_branchcd and fb_companycode = " + strCompanyCode + " and fb_exchange = " + strExchange + " and fb_segment = " + strSegment +
                         "and fb_billdt = '" + req.Date + "' and fb_clientcd = cm_cd " + strClientWhere + loginAccess;
                DataTable ds = objUtility.OpenDataTable(strsql);
                if (ds.Rows.Count > 0)
                {
                    List<BillSummaryFOResponse> List = new List<BillSummaryFOResponse>();
                    for (int i = 0; i < ds.Rows.Count; i++)
                    {
                        BillSummaryFOResponse ObjFO = new BillSummaryFOResponse();
                        ObjFO.ClientCode = ds.Rows[i]["code"].ToString().Trim();
                        ObjFO.ClientName = ds.Rows[i]["name"].ToString().Trim();
                        ObjFO.BranchCode = ds.Rows[i]["cm_brboffcode"].ToString().Trim();
                        ObjFO.BranchName = ds.Rows[i]["bm_branchname"].ToString().Trim();
                        ObjFO.MTM = Convert.ToDouble(ds.Rows[i]["mtm"].ToString().Trim());
                        ObjFO.Premium = Convert.ToDouble(ds.Rows[i]["premium"].ToString().Trim());
                        ObjFO.AxerAssgn = Convert.ToDouble(ds.Rows[i]["exerassgn"].ToString().Trim());
                        ObjFO.Charges = Convert.ToDouble(ds.Rows[i]["charges"].ToString().Trim());
                        ObjFO.Margin = Convert.ToDouble(ds.Rows[i]["Margin"].ToString().Trim());
                        ObjFO.DrCr = Convert.ToDouble(ds.Rows[i]["drcr"].ToString().Trim());
                        ObjFO.Brokerage = Convert.ToDouble(ds.Rows[i]["brokerage"].ToString().Trim());
                        List.Add(ObjFO);
                    }
                    return List;
                }
                return null;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic ClientMaster(ClientMasterModel req, string loginAccess)
        {
            try
            {
                string strClientWhere = "";
                string clientCode = "";
                if (req.Filter.Client != null)
                {
                    if (req.Filter.Client.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Client.ToArray(), "##"));
                        strClientWhere = " or cm_cd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Branch != null)
                {
                    if (req.Filter.Branch.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Branch.ToArray(), "##"));
                        strClientWhere += " or cm_brboffcode in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Group != null)
                {
                    if (req.Filter.Group.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Group.ToArray(), "##"));
                        strClientWhere += " or cm_groupcd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Family != null)
                {
                    if (req.Filter.Family.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Family.ToArray(), "##"));
                        strClientWhere += " or cm_familycd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (strClientWhere.Length > 0)
                {
                    strClientWhere = " and (" + strClientWhere.Substring(3) + ") ";
                }

                strsql = "Select cm_cd,  isNull(cm_name,'') cm_name, isNull(cm_panno,'') cm_panno, isNull(cm_mobile,'')  cm_mobile, isNull(cm_email,'')  cm_email, " +
                        " isNull(cm_dob,'') cm_dob, isNull(cm_sex,'') cm_sex, isNull(cm_maritalstatus,'') cm_maritalstatus, isNull(cm_flag,'')  cm_flag, " +
                        " isNull(cm_brboffcode,'') cm_brboffcode, isNull(cm_constitution,'') cm_constitution, isNull(cm_faherhusguar,'') cm_faherhusguar, " +
                        " isNull(cm_freezeyn,'') cm_freezeyn, isNull(cm_nationality,'') cm_nationality, isNull(cm_add1,'') cm_add1, isNull(cm_add2,'') cm_add2, " +
                        " isNull(cm_add3,'') cm_add3, isNull(cm_add4,'') cm_add4, isNull(cm_state,'') cm_state, isNull(cm_pincode,'') cm_pincode , " +
                        " isNull(cm_grossincome,'') cm_grossincome, isNull(cm_grossincomedt,'') cm_grossincomedt, isNull(cm_networth, 0) cm_networth, isNull(cm_networthdt,'') cm_networthdt, " +
                        " isNull(cm_occup,'') cm_occup, isNull(cm_residentialstatus,'') cm_residentialstatus, isNull(cm_uid,'') cm_uid, isNull(cm_subbroker,'') cm_subbroker, " +
                        " isNull(cm_BankActNo,'') cm_BankActNo, isNull(cm_padd1,'') cm_padd1, isNull(cm_padd2,'') cm_padd2, isNull(cm_padd3,'') cm_padd3 , isNull(cm_padd4,'') cm_padd4 , " +
                        " isNull(cm_pstate,'') cm_pstate , isNull(cm_pcountry,'') cm_pcountry , isNull(cm_ppincode,'') cm_ppincode, " +
                        " Case isNull(cm_nationalcode,'') When '1' then 'Indian [1]' when '2' then 'Other [2]' else '' end as cm_nationalcode, isNull(cm_dpactno,'') cm_dpactno , " +
                        " isNull(cn_ucc,'') cn_ucc , isNull(cn_name,'') cn_name , isNull(cn_add1,'') cn_add1 , isNull(cn_add2,'') cn_add2 ,  isNull(cn_city,'') cn_city ,  " +
                        " isNull(cn_state,'') cn_state , isNull(cn_pin,'') cn_pin , isNull(cn_tel,'') cn_tel ,  isNull(cn_pan,'') cn_pan ,  isNull(cn_regdt,'') cn_regdt , isNull(cn_dob,'') cn_dob ,  " +
                        " isNull(cn_gname,'') cn_gname , isNull(cn_gadd1,'') cn_gadd1 , isNull(cn_gadd2,'') cn_gadd2 , isNull(cn_gcity,'') cn_gcity , isNull(cn_gstate,'') cn_gstate ,  " +
                        " isNull(cn_gpin,'') cn_gpin , isNull(cn_gtel,'') cn_gtel ,  isNull(cn_gpan,'') cn_gpan ,  isNull(cn_gpan,'') cn_gpan  " +
                        " from Client_master Left Join Client_Nominee on (cm_cd=cn_cd) , Client_Info  Where cm2_cd=cm_cd " + strClientWhere + loginAccess;

                DataTable dtCode = objUtility.OpenDataTable(strsql);
                if (dtCode.Rows.Count > 0)
                {
                    List<ClientMasterResponse> data = new List<ClientMasterResponse>();

                    for (int i = 0; i < dtCode.Rows.Count; i++)
                    {
                        bool blnIFSC = false;
                        string BankName = "";
                        string BankIFSC = "";
                        string BankAccNo = "";
                        string BankMICR = "s";
                        string BankAccType = "";

                        clientCode = dtCode.Rows[i]["cm_cd"].ToString().Trim();

                        if (Convert.ToInt32(objUtility.fnFireQuery("sysobjects a, syscolumns b", "count(0)", "a.id=b.id and a.name='Bankact' and b.name", "ba_ifsccode", true)) > 0)
                        {
                            blnIFSC = true;
                        }
                        strsql = "select cm_cd, ba_clientcd, isnull(ba_micr, '') ba_micr, isNull(ba_acttype,'') ba_acttype, isNull(ba_actno,'') ba_actno, isNull(ba_default,'') ba_default, isNull(bk_micr,'') bk_micr, " +
                                     " isNull(bk_name,'') bk_name, isNull(bk_add1,'') bk_add1, isNull(ba_proof,'') ba_proof, isNull(ba_acttype,'') ba_acttype ";
                        if (blnIFSC == true)
                        {
                            strsql += ", ba_ifsccode ";
                        }
                        strsql += " from Client_master, Bankact, Bank_master ";
                        strsql += " where cm_cd = ba_clientcd and bk_micr = ba_micr and ba_clientcd = '" + clientCode + "' and ba_default = 'Y' ";
                        if (blnIFSC == true)
                        {
                            strsql += " and bk_IFCCode=ba_ifsccode ";
                        }
                        strsql += " order by ba_default desc ";
                        DataTable dtTemp = objUtility.OpenDataTable(strsql);
                        if (dtTemp.Rows.Count > 0)
                        {
                            BankName = dtTemp.Rows[0]["bk_name"].ToString().Trim();
                            BankIFSC = blnIFSC == true ? dtTemp.Rows[0]["ba_ifsccode"].ToString().Trim() : "";
                            BankAccNo = dtTemp.Rows[0]["ba_actno"].ToString().Trim();
                            BankMICR = dtTemp.Rows[0]["bk_micr"].ToString().Trim();
                            BankAccType = dtTemp.Rows[0]["ba_acttype"].ToString().Trim();
                        }


                        string strDPID = "";
                        string strDPName = "";
                        string strDPType = "";
                        string strClientID = "";
                        strsql = "select isNull(da_dpid,'') da_dpid ,isNull(da_actno,'') da_actno, isNull(case left(da_dpid,2) when 'IN' then  rtrim(da_dpid)+rtrim(da_actno) else da_actno end , '') as BOId, " +
                                    " isNull(case left(da_dpid,2) when 'IN' then  'NSDL' else 'CDSL' end, '') as Type, isNull(dp_name,'') dp_name " +
                                    " from Dematact, Dps " +
                                    " where da_dpid = dp_dpid and da_clientcd='" + clientCode + "' and da_defaultyn='y' ";
                        dtTemp = objUtility.OpenDataTable(strsql);
                        if (dtTemp.Rows.Count != 0)
                        {
                            strDPID = dtTemp.Rows[0]["da_dpid"].ToString().Trim();
                            strDPName = dtTemp.Rows[0]["dp_name"].ToString().Trim();
                            strDPType = dtTemp.Rows[0]["Type"].ToString().Trim();
                            strClientID = dtTemp.Rows[0]["BOId"].ToString().Trim();
                        }

                        string strFATCA = "";
                        string strKRA = "";
                        strsql = "select isNull(cn_fillerN0 ,0) cn_fillerN0 ,isNull(cn_KRAStatus,'') cn_KRAStatus from client_Nominee where cn_cd = '" + clientCode + "'";
                        dtTemp = objUtility.OpenDataTable(strsql);
                        if (dtTemp.Rows.Count != 0)
                        {
                            strFATCA = dtTemp.Rows[0]["cn_fillerN0"].ToString().Trim();
                            switch (strFATCA)
                            {
                                case "0":
                                    strFATCA = "Not Reportable";
                                    break;
                                case "1":
                                    strFATCA = "Reportable";
                                    break;
                                default:
                                    strFATCA = "";
                                    break;
                            }

                            strKRA = dtTemp.Rows[0]["cn_KRAStatus"].ToString().Trim();
                            switch (strKRA)
                            {
                                case "Y":
                                    strKRA = "Y";
                                    break;
                                default:
                                    strKRA = "N";
                                    break;
                            }
                        }

                        string strBranchName = "";
                        strsql = "Select isNull(bm_branchname,'') bm_branchname from branch_master where bm_branchcd='" + dtCode.Rows[i]["cm_brboffcode"].ToString().Trim() + "'";
                        dtTemp = objUtility.OpenDataTable(strsql);
                        if (dtTemp.Rows.Count != 0)
                        {
                            strBranchName = dtTemp.Rows[0]["bm_branchname"].ToString().Trim();
                        }

                        string strRemissier = "";
                        strsql = "select RM_CD , isNull(RM_Name,'') RM_Name, isNull(RM_Freezeyn,'') RM_Freezeyn from SubBrokers where rm_cd ='" + dtCode.Rows[i]["cm_subbroker"].ToString().Trim() + "'";
                        dtTemp = objUtility.OpenDataTable(strsql);
                        if (dtTemp.Rows.Count != 0)
                        {
                            strRemissier = dtTemp.Rows[0]["RM_Name"].ToString().Trim();
                        }

                        string strRmcode = "";
                        strsql = "select rm_cd , isNull(rm_name,'') rm_name from RM_master where rm_cd ='" + dtCode.Rows[i]["cm_dpactno"].ToString().Trim() + "'";
                        dtTemp = objUtility.OpenDataTable(strsql);
                        if (dtTemp.Rows.Count != 0)
                        {
                            strRmcode = dtTemp.Rows[0]["RM_Name"].ToString().Trim() + "[" + dtTemp.Rows[0]["rm_cd"].ToString().Trim() + "]";
                        }

                        string strCategory = "";
                        strsql = "select isNull(cc_descrip,'') cc_descrip from client_category where cc_cd='" + dtCode.Rows[i]["cm_constitution"].ToString().Trim() + "'";
                        dtTemp = objUtility.OpenDataTable(strsql);
                        if (dtTemp.Rows.Count != 0)
                        {
                            strCategory = dtTemp.Rows[0]["cc_descrip"].ToString().Trim() + " [" + dtCode.Rows[0]["cm_constitution"].ToString().Trim() + "]";
                        }

                        string strGender = dtCode.Rows[i]["cm_sex"].ToString().Trim();
                        if (strGender == "M")
                        {
                            strGender = "Male [M]";
                        }
                        else if (strGender == "F")
                        {
                            strGender = "Female [F]";
                        }
                        else
                        {
                            strGender = "";
                        }

                        string strIncome = "";
                        switch (dtCode.Rows[i]["cm_grossincome"].ToString().Trim())
                        {
                            case "1":
                                strIncome = "Below Rs. 1  Lac";
                                break;
                            case "2":
                                strIncome = "Btw Rs. 1 to Rs. 5 Lacs";
                                break;
                            case "3":
                                strIncome = "Btw Rs. 5 to Rs. 10 Lacs";
                                break;
                            case "4":
                                strIncome = "Btw Rs. 10 to Rs. 25 Lacs";
                                break;
                            case "5":
                                strIncome = "Btw Rs. 25 Lacs to Rs. 1 Crore";
                                break;
                            case "6":
                                strIncome = "More than Rs. 1 Crore";
                                break;
                            default:
                                strIncome = "";
                                break;
                        }
                        strIncome += " [" + dtCode.Rows[i]["cm_grossincome"].ToString().Trim() + "]";

                        string strMaritalStatus = "";
                        switch (dtCode.Rows[i]["cm_maritalstatus"].ToString().Trim())
                        {
                            case "S":
                                strMaritalStatus = "Single [S]";
                                break;
                            case "M":
                                strMaritalStatus = "Married [M]";
                                break;
                            case "W":
                                strMaritalStatus = "Widow/Widower [W]";
                                break;
                            case "D":
                                strMaritalStatus = "Divorce [D]";
                                break;
                            case "NA":
                                strMaritalStatus = "Not Applicable [NA]";
                                break;
                            default:
                                strMaritalStatus = "";
                                break;
                        }
                        strMaritalStatus += " [" + dtCode.Rows[i]["cm_maritalstatus"].ToString().Trim() + "]";

                        string strResStatus = "";
                        switch (dtCode.Rows[i]["cm_residentialstatus"].ToString().Trim())
                        {
                            case "I":
                                strResStatus = "Indian";
                                break;
                            case "N":
                                strResStatus = "NRI";
                                break;
                            case "F":
                                strResStatus = "Foreign National";
                                break;
                            default:
                                strResStatus = "";
                                break;
                        }
                        strResStatus += " [" + dtCode.Rows[i]["cm_residentialstatus"].ToString().Trim() + "]";


                        string strAccStatus = "";
                        switch (dtCode.Rows[i]["cm_freezeyn"].ToString().Trim())
                        {
                            case "A":
                                strAccStatus = "Freeze for All";
                                break;
                            case "Y":
                                strAccStatus = "Freeze for Trades";
                                break;
                            case "B":
                                strAccStatus = "Freeze for Branches";
                                break;
                            default:
                                strAccStatus = "Active";
                                break;
                        }
                        strAccStatus += " [" + dtCode.Rows[i]["cm_freezeyn"].ToString().Trim() + "]";

                        string strCKYCStatus = "";
                        string strCKYCReffNo = "";
                        string strCKYCRes = "";
                        string strCKYCNo = "";
                        string strCKYCDate = "";
                        string firstname = "";
                        string middlename = "";
                        string lastname = "";

                        strsql = "Select count(0) From sysobjects Where name = 'Client_CKYC'";
                        dtTemp = objUtility.OpenDataTable(strsql);
                        if (Conversion.Val(dtTemp.Rows[0][0].ToString()) != 0)
                        {
                            strsql = "select isNull(case ck_status when 'Y' then 'Pending' when 'E' then 'Exported' when 'S' then 'Success' else 'Pending' end, '') as ck_status, " +
                                        " isNull(Ck_Reference,'') Ck_Reference, isNull(case Ck_RespType when '02' then 'Post De-Duplication' when '03' then 'Post FIR' when '04' then 'Post confirmation with ID issuer' " +
                                        " when '05' then 'Post KYC Generation' else '' end, '') as ckresponsetype, isNull(Ck_Nfiller1,0) as ckycnumber, isNull(mkrdt,'') mkrdt " +
                                        " from Client_CKYC " +
                                        " where CK_SRNO in (select MAX(ck_SRNo) From Client_CKYC  Group By CK_Panno) And ck_panno ='" + dtCode.Rows[i]["cm_panno"].ToString().Trim() + "' ";
                            dtTemp = objUtility.OpenDataTable(strsql);
                            if (dtTemp.Rows.Count != 0)
                            {
                                strCKYCStatus = dtTemp.Rows[0]["ck_status"].ToString().Trim();
                                strCKYCReffNo = dtTemp.Rows[0]["Ck_Reference"].ToString().Trim();
                                strCKYCRes = dtTemp.Rows[0]["ckresponsetype"].ToString().Trim();
                                strCKYCNo = dtTemp.Rows[0]["ckycnumber"].ToString().Trim();
                                strCKYCDate = dtTemp.Rows[0]["mkrdt"].ToString().Trim();
                            }
                        }

                        strsql = "Select isNull(CK_Fname,'') CK_Fname, isNull(CK_Middlename,'') CK_Middlename, isNull(CK_Lname,'') CK_Lname From CLient_KYC Where CK_ClientCd = '" + clientCode + "'";
                        DataTable dtKYC = objUtility.OpenDataTable(strsql);
                        if (dtKYC.Rows.Count > 0)
                        {
                            firstname = dtKYC.Rows[0]["CK_Fname"].ToString().Trim();
                            middlename = dtKYC.Rows[0]["CK_Middlename"].ToString().Trim();
                            lastname = dtKYC.Rows[0]["CK_Lname"].ToString().Trim();
                        }

                        string strCompanyCd = "";
                        if (Convert.ToInt32(objUtility.fnFireQuery("Entity_master", "count(0)", " em_cd='B' and em_name like 'SYKES & RAY EQUITIES%' and 1", "1", true)) > 0)
                        {
                            strsql = "select em_name OrgName,em_cd CompnyCd from Entity_Master with (nolock) where em_cd='B'";
                        }
                        else
                        {
                            strsql = "select em_name OrgName,em_cd CompnyCd from Entity_Master with (nolock) where em_cd =(select min(em_cd) from Entity_master Where len(em_cd) = 1)";
                        }
                        dtTemp = objUtility.OpenDataTable(strsql);
                        if (dtTemp.Rows.Count != 0)
                        {
                            strCompanyCd = dtTemp.Rows[0]["CompnyCd"].ToString().Trim();
                        }

                        string strSegments = "";
                        strsql = "Select Rtrim(CES_Exchange) + '-' + Rtrim(CES_Segment) + ' [' + Rtrim(ce_companycode) + ']' as  Exchange_Segment, isNull(ce_regDt,'') ce_regDt " +
                                    " from client_details,CompanyExchangeSegments " +
                                    " where CES_Cd = ce_companycode and ce_clientcd = '" + clientCode + "' and CES_CompanyCd = '" + strCompanyCd + "' and ce_regDt<>'' " +
                                    " order by CES_Cd ";
                        dtTemp = objUtility.OpenDataTable(strsql);
                        if (dtTemp.Rows.Count != 0)
                        {
                            strSegments = string.Join(",", dtTemp.AsEnumerable().Select(r => r.Field<string>("Exchange_Segment").Trim()).ToArray());
                        }

                        string strMotherName = "";
                        strsql = "select isNull(CK_Motherfname,'') CK_Motherfname, isNull(CK_MotherMname,'') CK_MotherMname, isNull(CK_MotherLname, '') CK_MotherLname  from Client_CKYC where CK_Status='Y' and CK_ActType <> '03' and CK_Panno='" + dtCode.Rows[0]["cm_panno"].ToString().Trim() + "'";
                        dtTemp = objUtility.OpenDataTable(strsql);
                        if (dtTemp.Rows.Count != 0)
                        {
                            strMotherName = dtTemp.Rows[0]["CK_Motherfname"].ToString().Trim() + " " + dtTemp.Rows[0]["CK_MotherMname"].ToString().Trim() + " " + dtTemp.Rows[0]["CK_MotherLname"].ToString().Trim();
                        }

                        List<BrokerageSchemes> brkdata = new List<BrokerageSchemes>();

                        strsql = "Select (Rtrim(CES_Exchange) + '/' + Rtrim(CES_Segment)) as ExchSeg,ce_regDt,ce_brkscheme from client_details,CompanyExchangeSegments where ce_clientcd = '" + clientCode + "' and ce_companycode =CES_cd";
                        DataTable Tempdt = objUtility.OpenDataTable(strsql);
                        if (Tempdt.Rows.Count > 0)
                        {
                            for (int j = 0; j < Tempdt.Rows.Count; j++)
                            {
                                BrokerageSchemes brkscheme = new BrokerageSchemes()
                                {
                                    ExchSeg = Tempdt.Rows[j]["ExchSeg"].ToString().Trim(),
                                    RegDate = Tempdt.Rows[j]["ce_regDt"].ToString().Trim(),
                                    Scheme = Tempdt.Rows[j]["ce_brkscheme"].ToString().Trim(),
                                };
                                brkdata.Add(brkscheme);
                            }
                        }

                        string lastTrdOn = "";
                        strsql = "select max(td_dt) from( ";
                        strsql += " select isNull(max(td_dt),'') td_dt from trades where td_clientcd='" + clientCode + "' ";
                        strsql += " union all ";
                        strsql += " select isNull(max(td_dt),'') td_dt from trx where td_clientcd='" + clientCode + "' ";
                        strsql += "  ) a";
                        dtTemp = objUtility.OpenDataTable(strsql);
                        if (dtTemp.Rows.Count > 0)
                        {
                            lastTrdOn = dtTemp.Rows[0][0].ToString().Trim();
                        }

                        string strNomName = "";
                        string strNomAdd1 = "";
                        string strNomAdd2 = "";
                        string strNomCity = "";
                        string strNomState = "";
                        string strNomPin = "";
                        string strNomPAN = "";
                        string strNomDOB = "";
                        string strGuardName = "";
                        string strGuardAdd1 = "";
                        string strGuardAdd2 = "";
                        string strGuardCity = "";
                        string strGuardState = "";
                        string strGuardPin = "";
                        string strGuardPAN = "";
                        string strGuardDOB = "";

                        if (objUtility.fnchkTable("Client_NomineeDetails"))
                        {
                            strsql = "select cn_Srno, cn_name, cn_Add1, cn_Add2, cn_City, cn_State, cn_Pin, cn_PAN, cn_DOB from Client_NomineeDetails where cn_Cmcd='" + clientCode + "' and cn_Srno in (1,2)";
                            dtTemp = objUtility.OpenDataTable(strsql);

                            foreach (DataRow dr in dtTemp.Rows)
                            {
                                if (dr["cn_Srno"].ToString().Trim() == "1")
                                {
                                    strNomName = dr["cn_name"].ToString().Trim();
                                    strNomAdd1 = dr["cn_Add1"].ToString().Trim();
                                    strNomAdd2 = dr["cn_Add2"].ToString().Trim();
                                    strNomCity = dr["cn_City"].ToString().Trim();
                                    strNomState = dr["cn_State"].ToString().Trim();
                                    strNomPin = dr["cn_Pin"].ToString().Trim();
                                    strNomPAN = dr["cn_PAN"].ToString().Trim();
                                    strNomDOB = dr["cn_DOB"].ToString().Trim();
                                }
                                else if (dr["cn_Srno"].ToString().Trim() == "2")
                                {
                                    strGuardName = dr["cn_name"].ToString().Trim();
                                    strGuardAdd1 = dr["cn_Add1"].ToString().Trim();
                                    strGuardAdd2 = dr["cn_Add2"].ToString().Trim();
                                    strGuardCity = dr["cn_City"].ToString().Trim();
                                    strGuardState = dr["cn_State"].ToString().Trim();
                                    strGuardPin = dr["cn_Pin"].ToString().Trim();
                                    strGuardPAN = dr["cn_PAN"].ToString().Trim();
                                    strGuardDOB = dr["cn_DOB"].ToString().Trim();
                                }
                            }
                        }
                        else
                        {
                            strNomName = dtCode.Rows[i]["cn_name"].ToString().Trim();
                            strNomAdd1 = dtCode.Rows[i]["cn_add1"].ToString().Trim();
                            strNomAdd2 = dtCode.Rows[i]["cn_add2"].ToString().Trim();
                            strNomCity = dtCode.Rows[i]["cn_city"].ToString().Trim();
                            strNomState = dtCode.Rows[i]["cn_state"].ToString().Trim();
                            strNomPin = dtCode.Rows[i]["cn_pin"].ToString().Trim();
                            strNomPAN = dtCode.Rows[i]["cn_regdt"].ToString().Trim();
                            strNomDOB = dtCode.Rows[i]["cn_dob"].ToString().Trim();
                            strGuardName = dtCode.Rows[i]["cn_gname"].ToString().Trim();
                            strGuardAdd1 = dtCode.Rows[i]["cn_gadd1"].ToString().Trim();
                            strGuardAdd2 = dtCode.Rows[i]["cn_gadd2"].ToString().Trim();
                            strGuardCity = dtCode.Rows[i]["cn_gcity"].ToString().Trim();
                            strGuardState = dtCode.Rows[i]["cn_gstate"].ToString().Trim();
                            strGuardPin = dtCode.Rows[i]["cn_gpin"].ToString().Trim();
                            strGuardPAN = dtCode.Rows[i]["cn_gpan"].ToString().Trim();
                        }

                        ClientMasterResponse clientMaster = new ClientMasterResponse()
                        {
                            ClientCode = dtCode.Rows[i]["cm_cd"].ToString().Trim(),
                            ClientName = dtCode.Rows[i]["cm_name"].ToString().Trim(),
                            FirstName = firstname,
                            MiddleName = middlename,
                            LastName = lastname,
                            Branch = dtCode.Rows[i]["cm_brboffcode"].ToString().Trim(),
                            BranchName = strBranchName.Trim(),
                            FatherName = dtCode.Rows[i]["cm_faherhusguar"].ToString().Trim(),
                            Nationality = dtCode.Rows[i]["cm_nationalcode"].ToString().Trim(),
                            PAN = dtCode.Rows[i]["cm_panno"].ToString().Trim(),
                            UID = dtCode.Rows[i]["cm_uid"].ToString().Trim(),
                            Constitution = strCategory,
                            Mobile = dtCode.Rows[i]["cm_mobile"].ToString().Trim(),
                            Email = dtCode.Rows[i]["cm_email"].ToString().Trim(),
                            DateofBirth = dtCode.Rows[i]["cm_dob"].ToString().Trim(),
                            Gender = strGender,
                            MaritalStatus = strMaritalStatus,
                            AccountStatus = strAccStatus,
                            ResidentialStatus = strResStatus,
                            ExchangeSegment = strSegments,
                            FATCAStatus = strFATCA,
                            FATCADeclaration = "",
                            FATCADueDiligence = "",
                            FATCACountry = "",
                            FATCATaxIdentify = "",
                            FATCATypeIdentify = "",
                            CKYCStatus = strCKYCStatus,
                            CKYCReffNo = strCKYCReffNo,
                            CKYCRespType = strCKYCRes,
                            CKYCNumber = strCKYCNo,
                            CKYCDate = strCKYCDate,
                            KRAStatus = strKRA,
                            KRAAddressUpdate = "",
                            BankName = BankName.Trim(),
                            BankIFSC = BankIFSC.Trim(),
                            BankAccNo = BankAccNo.Trim(),
                            BankMICR = BankMICR.Trim(),
                            BankAccType = BankAccType.Trim(),
                            CorrAddress1 = dtCode.Rows[i]["cm_add1"].ToString().Trim(),
                            CorrAddress2 = dtCode.Rows[i]["cm_add2"].ToString().Trim(),
                            CorrAddress3 = dtCode.Rows[i]["cm_add3"].ToString().Trim(),
                            CorrCity = dtCode.Rows[i]["cm_add4"].ToString().Trim(),
                            CorrState = dtCode.Rows[i]["cm_state"].ToString().Trim(),
                            CorrCountry = dtCode.Rows[i]["cm_BankActNo"].ToString().Trim(),
                            CorrPincode = dtCode.Rows[i]["cm_pincode"].ToString().Trim(),
                            PerAddress1 = dtCode.Rows[i]["cm_padd1"].ToString().Trim(),
                            PerAddress2 = dtCode.Rows[i]["cm_padd2"].ToString().Trim(),
                            PerAddress3 = dtCode.Rows[i]["cm_padd3"].ToString().Trim(),
                            PerCity = dtCode.Rows[i]["cm_padd4"].ToString().Trim(),
                            PerState = dtCode.Rows[i]["cm_pstate"].ToString().Trim(),
                            PerCountry = dtCode.Rows[i]["cm_pcountry"].ToString().Trim(),
                            PerPincode = dtCode.Rows[i]["cm_ppincode"].ToString().Trim(),
                            NetWorth = dtCode.Rows[i]["cm_networth"].ToString().Trim(),
                            NetworthDate = dtCode.Rows[i]["cm_networthdt"].ToString().Trim(),
                            GrossAnnualIncome = strIncome,
                            GrossAnnualIncomeDate = dtCode.Rows[i]["cm_grossincomedt"].ToString().Trim(),
                            RM = strRmcode,
                            DPID = strDPID,
                            DPAcno = strClientID,
                            DPType = strDPType,
                            Occupation = dtCode.Rows[i]["cm_occup"].ToString().Trim(),
                            NomFirstName = strNomName,
                            NomAddress1 = strNomAdd1,
                            NomAddress2 = strNomAdd2,
                            NomAddressCity = strNomCity,
                            NomAddressState = strNomState,
                            NomAddressPin = strNomPin,
                            NomineePAN = strNomPAN,
                            NomineeDOB = strNomDOB,
                            NomGuardianFirstName = strGuardName,
                            NomGuardianAddress1 = strGuardAdd1,
                            NomGuardianAddress2 = strGuardAdd2,
                            NomGuardianAddressCity = strGuardCity,
                            NomGuardianAddressState = strGuardState,
                            NomGuardianAddressPin = strGuardPin,
                            NomGuardianPAN = strGuardPAN,
                            LastTradedDate = lastTrdOn,
                            BrokerageScheme = brkdata
                        };

                        data.Add(clientMaster);
                    }
                    return data;
                }
                return null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetBrokeragScheme(string segment)
        {
            try
            {
                string strSql = "", table = "";
                if (segment.Substring(2, 1) == "C")
                {
                    table = " Brokerages";
                    strSql = " select  br_scheme Col0,ltrim(str(max(case when br_type= 'SD1' then  br_min else 0 end),4,2)) Col1, ";
                    strSql += " ltrim(str(max(case when br_type= 'SD1' then  br_percent else 0 end),6,4)) Col2,";
                    strSql += " ltrim(str(max(case when br_type= 'DLV' then  br_min else 0 end),4,2)) Col3,";
                    strSql += " ltrim(str(max(case when br_type= 'DLV' then  br_percent else 0 end),6,4)) Col4,";
                    strSql += " ltrim(str(max(case when br_type= 'SD2' then  br_min else 0 end),4,2))  Col5,";
                    strSql += " ltrim(str(max(case when br_type= 'SD2' then  br_percent else 0 end),6,4)) Col6";
                    strSql += " from " + table + " Where br_Companycode = '" + segment + "' and br_settgroup = 'N' group by br_scheme";
                }
                else if (segment.Substring(2, 1) == "M")
                {
                    table = " MFbrokerages";
                    strSql = " select  mbr_scheme Col0,ltrim(str(max(case when mbr_type= 'Buy' then  mbr_percent else 0 end),4,2)) Col1 ,";
                    strSql += " ltrim(str(max(case when mbr_type= 'Buy' then  mbr_min else 0 end),6,4))  Col2,";
                    strSql += " ltrim(str(max(case when mbr_type= 'Buy' then  mbr_max else 0 end),4,2)) Col3,";
                    strSql += " ltrim(str(max(case when mbr_type= 'Sell' then mbr_percent else 0 end),6,4)) Col4,";
                    strSql += " ltrim(str(max(case when mbr_type= 'Sell' then mbr_min else 0 end),4,2)) Col5,";
                    strSql += " ltrim(str(max(case when mbr_type= 'Sell' then mbr_max else 0 end),6,4)) Col6";
                    strSql += " from " + table + " Where mbr_Companycode = '" + segment + "' and mbr_settgroup = 'M' group by mbr_scheme";
                }
                else
                {
                    table = " FBrokerages,fbrokerage_master";
                    strSql = " select  br_scheme Col0,ltrim(str(max(br_percent1),6,4)) Col1 ,";
                    strSql += " case max(br_min1) when 0 then '' else ltrim(str(max(br_min1),6,4)) end  Col2,";
                    strSql += " case max(br_max1) when 0 then '' else ltrim(str(max(br_max1),6,4)) end Col3,";
                    strSql += " case fb_sdremove when 'N' then 'No' else ltrim(str(max(br_percent2),6,4)) end Col4,";
                    strSql += " case fb_sdremove when 'N' then 'Sqr' else case max(br_min2) when 0 then '' else ltrim(str(max(br_min2),6,4)) end end Col5,";
                    strSql += " case fb_sdremove when 'N' then 'off' else case max(br_max1) when 0 then '' else ltrim(str(max(br_max1),6,4)) end end Col6,";
                    strSql += " fb_minpercontractOpt Col7";
                    strSql += " from " + table + " Where br_Companycode = '" + segment + "'and br_companycode=fb_companycode and br_scheme=fb_scheme group by br_scheme, fb_sdremove,fb_minpercontractOpt";
                }
                var dt = objUtility.OpenDataTable(strSql);
                if (dt.Rows.Count > 0)
                {
                    return dt;
                }
                return null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic EntryReceiptPayment(string userId, EntryReceiptPaymentReq req)
        {
            try
            {
                var mode = "ADD";
                long lngSrno = 1;
                var strMICRMNT = objUtility.GetSysParmSt("MICRMNT", "");
                string StrNoAuthReq = objUtility.GetSysParmSt("TPLUSNOAUTH", "").Trim();
                bool BlnNoAuthReq = Convert.ToInt32(objUtility.fnFireQueryTradeWeb("Sysparameter", "Count(0)", "sp_parmcd", "TPLUSNOAUTH", true)) > 0;
                string strCompWiseSrnoR = "";
                string strCompWiseSrnoL = "";
                DataTable dt = new DataTable();
                DataSet ds = new DataSet();
                SqlTransaction objTrans;
                EntryReceiptPaymentRes recRes = new EntryReceiptPaymentRes();

                if (objUtility.GetSysParmSt("COMPWISESRNO", "") == "Y")
                {
                    strCompWiseSrnoR = " and left(rc_dpid,1)='" + Strings.Left(req.ExchSeg, 1) + "'";
                    strCompWiseSrnoL = " and left(ld_dpid,1)='" + Strings.Left(req.ExchSeg, 1) + "'";
                }
                else
                {
                    strCompWiseSrnoR = "";
                    strCompWiseSrnoL = "";
                }

                if (Convert.ToInt32(objUtility.fnFireQueryTradeWeb("client_master", "count(0)", "cm_cd", req.ClientCode.Trim(), true)) == 0)
                {
                    recRes.Status = "error";
                    recRes.Response = "Client Code Not Found";
                    return recRes;
                }

                if (req.Amount <= 0)
                {
                    recRes.Status = "error";
                    recRes.Response = "Amount Cannot Be A Negative or Zero";
                    return recRes;
                }

                if (!objUtility.validateDt(req.EntryDt.Trim(), "yyyyMMdd"))
                {
                    recRes.Status = "error";
                    recRes.Response = "Enter Valid Entry Date";
                    return recRes;
                }

                if (req.ClearDt.Trim() != "")
                {
                    if (!objUtility.validateDt(req.EntryDt.Trim(), "yyyyMMdd"))
                    {
                        recRes.Status = "error";
                        recRes.Response = "Enter Valid Clear Date";
                        return recRes;
                    }
                    if (Conversion.Val(req.ClearDt.Trim()) < Conversion.Val(req.EntryDt.Trim()))
                    {
                        recRes.Status = "error";
                        recRes.Response = "Clear Date cannot be less than Entry Date";
                        return recRes;
                    }
                }

                if (req.ReceivedAs.Trim() == "")
                {
                    recRes.Status = "error";
                    recRes.Response = "Received As Cannot be Left Blank";
                    return recRes;
                }

                string strRecAs = req.ReceivedAs.Trim();
                if (req.Type.Trim() == "R")
                {
                    if (strRecAs != "0" && strRecAs != "1" && strRecAs != "2" && strRecAs != "3" && strRecAs != "4")
                    {
                        recRes.Status = "error";
                        recRes.Response = "Enter Valid Received As value";
                        return recRes;
                    }
                }
                else
                {
                    if (strRecAs != "0" && strRecAs != "1" && strRecAs != "2")
                    {
                        recRes.Status = "error";
                        recRes.Response = "Enter Valid Received As value";
                        return recRes;
                    }
                }

                int intChequeLen = 0;
                intChequeLen = Convert.ToInt32(objUtility.fnFireQueryTradeWeb("information_schema.columns", "character_maximum_length", "table_name='Receipts' and COLUMN_NAME", "rc_chequeno", true));
                req.ChequeNo = Strings.Left(req.ChequeNo.Trim(), intChequeLen);

                var db = new DataContext();
                using (SqlConnection sqlCon = new SqlConnection((db.Database.GetDbConnection()).ConnectionString))
                {
                    sqlCon.Open();
                    objTrans = sqlCon.BeginTransaction();
                    SqlDataAdapter sqlDtAdap = new SqlDataAdapter();
                    SqlCommandBuilder sqlCmdBld = new SqlCommandBuilder(sqlDtAdap);
                    string strCostCenter = "";

                    strsql = "select isNull(sum(ld_amount),0) ledgerbal ,cm_cd, cm_name,cm_openingbal, cm_freezeyn,cm_schedule,cm_cfstdratesyn from Client_master left join Ledger on (cm_cd=ld_clientcd and ld_dpid like 'A_C' and left(ld_dpid,1) = 'A'), Schedule where cm_cd ='" + req.ClientCode.Trim() + "'  and sc_cd=cm_schedule and sc_bankflag='N'  group by cm_cd,cm_name,cm_openingbal, cm_freezeyn,cm_schedule,cm_cfstdratesyn";
                    dt = objUtility.OpenDataTable(strsql);
                    if (dt.Rows.Count > 0)
                    {
                        if (dt.Rows[0]["cm_freezeyn"].ToString().Trim() == "A")
                        {
                            recRes.Status = "error";
                            recRes.Response = "[ " + req.ClientCode.Trim() + " ] is freezed for all operations.";
                            return recRes;
                        }
                        if (Convert.ToInt32(dt.Rows[0]["cm_openingbal"]) != 1)
                        {
                            strCostCenter = "000";
                        }
                    }

                    strsql = "Select * from BankAct where ba_ActNo like '%" + req.BankAccNo.Trim() + "%' " + (req.MICR != "" ? " and BA_MICR='" + req.MICR.Trim() + "'" : "");
                    dt = objUtility.OpenDataTable(strsql);
                    //if (dt.Rows.Count > 0)
                    //{
                    //    return "No Match found";
                    //}
                    if (mode == "ADD")
                    {
                        if (req.Type.Trim() == "R")
                        {
                            if ((req.MICR.Trim() == "" || req.BankAccNo.Trim() == "") && strMICRMNT == "F")
                            {
                                recRes.Status = "error";
                                recRes.Response = "Client's Bank A/c details not entered.";
                                return recRes;
                            }

                            //if (req.MICR.Trim() != "" && strMICRMNT != "N")
                            //{
                            //    if (objUtility.fnFireQueryTradeWeb("Bank_Master", "BK_MICR", "BK_MICR", req.MICR.Trim(), true) != req.MICR.Trim())
                            //    {
                            //        recRes.Status = "error";
                            //        recRes.Response = "Invalid Bank MICR";
                            //        return recRes;
                            //    }
                            //}

                            //if (strMICRMNT != "N")
                            //{
                            //    if (Convert.ToInt16(objUtility.fnFireQueryTradeWeb("BankAct", "count(0)", "ba_actno='" + req.BankAccNo.Trim() + "' and ba_micr='" + req.MICR.Trim() + "' and ba_clientcd", req.ClientCode.Trim(), true)) == 0)
                            //    {
                            //        recRes.Status = "error";
                            //        recRes.Response = "Specified Client's Bank a/c is not registered with us";
                            //        return recRes;
                            //    }
                            //}
                        }

                        //if (req.ChequeNo != "")
                        //{
                        //    strsql = " select ld_chequeNo from Ledger ";
                        //    strsql += "where  ld_documentType = '" + req.Type + "' and  ld_clientcd = '" + req.ClientCode.Trim() + "' ";
                        //    strsql += "and charindex('" + req.ChequeNo.Trim() + "',Right(ld_chequeNo,Len('" + req.ChequeNo.Trim() + "')),1)>0 ";
                        //    if (Conversion.Val(req.ChequeNo.Trim()) > 0)
                        //    {
                        //        strsql += " union select ld_chequeNo from Ledger ";
                        //        strsql += "where  ld_documentType = '" + req.Type.Trim() + "' and  ld_clientcd = '" + req.ClientCode.Trim() + "' ";
                        //        strsql += "and charindex('" + Conversion.Val(req.ChequeNo.Trim()) + "',Right(ld_chequeNo,Len('" + Conversion.Val(req.ChequeNo.Trim()) + "')),1)>0 ";
                        //    }
                        //    DataTable dtTemp = objUtility.OpenDataTable(strsql);
                        //    if (dtTemp.Rows.Count > 0)
                        //    {
                        //        strsql = "Entry for cheque Number " + req.ChequeNo.Trim() + " found in ledger of " + req.ClientCode.Trim();
                        //        recRes.Status = "error";
                        //        recRes.Response = strsql;
                        //        return recRes;
                        //    }
                        //}

                        //if (req.Type.Trim() == "R")
                        //{
                        //    strsql = " select Count(0) from Receipts Where rc_debitflag = 'C'";
                        //    strsql += " and rc_clientcd = '" + req.ClientCode.Trim() + "'";
                        //    strsql += " and rc_amount = " + (-1) * System.Math.Round(Convert.ToDouble(req.Amount), 2) + "";
                        //    strsql += " and rc_receiptdt = '" + req.EntryDt.Trim() + "'";
                        //    DataTable dtTemp = objUtility.OpenDataTable(strsql);
                        //    if (Convert.ToInt32(dtTemp.Rows[0][0]) > 0)
                        //    {
                        //        recRes.Status = "error";
                        //        recRes.Response = " Receipt Entry of " + objUtility.mfnFormatCurrency(req.Amount, 2) + " already Entered  For " + req.ClientCode.Trim() + " For " + DateTime.ParseExact(req.EntryDt.Trim(), "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture).ToString("dd/MM/yyyy");
                        //        return recRes;
                        //    }
                        //}

                        if (objUtility.fnAllowEntry_Acc(req.EntryDt, UtilityCommon.eMode_acc.geEntry) == false)
                        {
                            recRes.Status = "error";
                            recRes.Response = "Enter proper date";
                            return recRes;
                        }

                        if (req.Type.Trim() == "P")
                        {
                            if (req.ReceivedAs.Trim() == "0")
                            {
                                if (objUtility.mfnGetSysSplFeature("BLK"))
                                {
                                    strsql = "Select isNull(sum(bp_amount),0) from BlockPayment where bp_clientcd='" + req.ClientCode.Trim() + "' and bp_block='B'";
                                    dt = objUtility.OpenDataTable(strsql);
                                }
                            }
                        }

                        if (req.Type.Trim() == "R")
                        {
                            strsql = "select isnull(max(rc_srno),0)+1 as maxsrno from Receipts where rc_debitflag ='C' and rc_accyear='" + objUtility.mfnGetAccYearFromDate(req.EntryDt) + "' " + strCompWiseSrnoR;
                        }
                        else
                        {
                            strsql = "select isnull(max(rc_srno),0)+1 as maxsrno from Receipts where rc_debitflag ='D' and rc_accyear='" + objUtility.mfnGetAccYearFromDate(req.EntryDt) + "' " + strCompWiseSrnoR;
                        }
                        DataTable dtSr = objUtility.OpenDataTable(strsql);
                        if (dtSr.Rows.Count == 0)
                        {
                            lngSrno = 1;
                        }
                        else
                        {
                            lngSrno = long.Parse(dtSr.Rows[0]["maxsrno"].ToString());
                        }

                        try
                        {
                            string strDateEdit = objUtility.mfnGetAccYearFromDate(objUtility.stod(req.EntryDt).ToString());
                            if (req.Type == "R")
                            {
                                strsql = "select * from Receipts where rc_debitflag='C' and rc_srno=" + lngSrno + " and rc_accyear='" + objUtility.mfnGetAccYearFromDate(req.EntryDt) + "' and rc_dpid='" + req.ExchSeg + "' and rc_entryno=" + 0;
                            }
                            else
                            {
                                strsql = "select * from Receipts where rc_debitflag='D' and rc_srno=" + lngSrno + " and rc_accyear='" + objUtility.mfnGetAccYearFromDate(req.EntryDt) + "' and rc_dpid='" + req.ExchSeg + "' and rc_entryno=" + 0;
                            }
                            DataSet dsReceipt = objUtility.OpenDataSet(sqlDtAdap, strsql, sqlCon, objTrans);

                            DataRow drow;
                            if (dsReceipt.Tables[0].Rows.Count > 0)
                            {
                                drow = dsReceipt.Tables[0].Rows[0];
                            }
                            else
                            {
                                drow = dsReceipt.Tables[0].NewRow();
                            }

                            if (req.Type.Trim() == "R")
                            {
                                drow["rc_debitflag"] = "C";
                                drow["rc_amount"] = (-1) * Math.Round(req.Amount);
                            }
                            else
                            {
                                drow["rc_debitflag"] = "D";
                                drow["rc_amount"] = Math.Round(req.Amount);
                            }
                            drow["rc_srno"] = lngSrno;
                            drow["rc_voucherno"] = req.VoucherNo.Trim();
                            drow["rc_receiptdt"] = req.EntryDt;
                            drow["mkrdt"] = DateTime.Today.Date.ToString("yyyyMMdd");
                            if (req.ClearDt.Trim() != "")
                            {
                                drow["rc_cleareddt"] = req.ClearDt.Trim();
                            }
                            else
                            {
                                drow["rc_cleareddt"] = DBNull.Value;
                            }
                            if (req.Type == "R")
                            {
                                drow["rc_micr"] = req.MICR;
                            }
                            else
                            {
                                drow["rc_micr"] = "";
                            }
                            drow["rc_clientcd"] = req.ClientCode;
                            drow["rc_particular"] = Strings.Left(req.Particulars.Trim(), 200);
                            drow["rc_bankclientcd"] = req.BankCode.Trim();
                            drow["rc_chequeno"] = req.ChequeNo.Trim();
                            drow["rc_costcenter"] = strCostCenter;
                            drow["mkrid"] = userId.ToUpper();
                            drow["mkrtm"] = objUtility.mfnGettime(objTrans);
                            drow["rc_accyear"] = objUtility.mfnGetAccYearFromDate(req.EntryDt);
                            drow["rc_dpid"] = req.ExchSeg.Trim();
                            drow["rc_commondt"] = req.EntryDt.Trim();
                            if (mode == "ADD")
                            {
                                drow["rc_common"] = "";
                            }
                            else
                            {
                                drow["rc_common"] = "";
                            }
                            drow["rc_common"] = "";
                            if (req.Type.Trim() == "R")
                            {
                                drow["rc_BankActNo"] = req.BankAccNo.Trim();
                            }
                            else
                            {
                                drow["rc_BankActNo"] = "";
                            }
                            drow["rc_batchno"] = req.ReceivedAs;
                            drow["rc_authid1"] = "";
                            drow["rc_authid2"] = "";
                            drow["rc_authtm1"] = "";
                            drow["rc_authtm2"] = "";
                            drow["rc_authremarks"] = "";
                            drow["rc_status"] = "N";
                            drow["rc_entryno"] = 1;

                            if (dsReceipt.Tables[0].Rows.Count == 0)
                            {
                                dsReceipt.Tables[0].Rows.Add(drow);
                            }
                            sqlDtAdap.Update(dsReceipt);
                            sqlDtAdap.Dispose();

                            if (req.Type == "R")
                            {
                                objUtility.ExecuteSQL("Delete from Ledger where ld_documenttype='R' and ld_documentno='" + lngSrno + "'  and ld_accyear='" + objUtility.mfnGetAccYearFromDate(req.EntryDt) + "' and ld_dpid='" + req.ExchSeg + "'", sqlCon, objTrans);
                                strsql = "select * from Auth_accounts where aa_documenttype='R'  and aa_amount<=(select abs(sum(rc_amount)) from Receipts where rc_debitflag='C' and rc_srno=" + lngSrno + " and rc_accyear='" + objUtility.mfnGetAccYearFromDate(req.EntryDt) + "'" + strCompWiseSrnoR + ")";
                                DataTable dtAuth = objUtility.OpenDataTable(strsql, sqlCon, objTrans);
                                if (dtAuth.Rows.Count == 0 || (BlnNoAuthReq && Strings.InStr(1, StrNoAuthReq, "R") > 0))
                                {
                                    objUtility.ExecuteSQL("update Receipts set rc_status='Y', rc_authid1='', rc_authid2='' where rc_dpid = '" + req.ExchSeg + "' and rc_debitflag='C' and rc_srno=" + lngSrno + " and rc_accyear='" + objUtility.mfnGetAccYearFromDate(req.EntryDt) + "'" + strCompWiseSrnoR, sqlCon, objTrans);
                                    strsql = "insert into Ledger (ld_clientcd,ld_dt,ld_amount,ld_particular,ld_chequeno,ld_debitflag,ld_documenttype,ld_documentno,ld_entryno,ld_costcenter,";
                                    strsql += " mkrid,mkrdt,ld_accyear,ld_dpid,ld_commondt,ld_common)";
                                    strsql += "select rc_clientcd, rc_receiptdt, rc_amount, rc_particular, rc_chequeno, rc_debitflag, 'R' , rc_srno, rc_entryno, rc_costcenter,";
                                    strsql += " mkrid,mkrdt, rc_accyear,rc_dpid, rc_commondt, rc_common from receipts where rc_debitflag='C' and rc_srno=" + lngSrno + " and rc_accyear='" + objUtility.mfnGetAccYearFromDate(req.EntryDt) + "' and rc_dpid='" + req.ExchSeg.Trim() + "'";
                                    objUtility.ExecuteSQL(strsql, sqlCon, objTrans);
                                }
                                else
                                {
                                    objUtility.ExecuteSQL("update Receipts set rc_status='N', rc_authid1='', rc_authid2='' where rc_dpid = '" + req.ExchSeg + "' and rc_debitflag='C' and rc_srno=" + lngSrno + " and rc_accyear='" + objUtility.mfnGetAccYearFromDate(req.EntryDt) + "'" + strCompWiseSrnoR, sqlCon, objTrans);
                                }
                            }
                            else
                            {
                                objUtility.ExecuteSQL("Delete from Ledger where ld_documenttype='P' and ld_documentno='" + lngSrno + "'  and ld_accyear='" + objUtility.mfnGetAccYearFromDate(req.EntryDt) + "' and ld_dpid='" + req.ExchSeg + "'", sqlCon, objTrans);
                                strsql = "select * from Auth_accounts where aa_documenttype='P'  and aa_amount<=(select abs(sum(rc_amount)) from Receipts where rc_debitflag='D' and rc_srno=" + lngSrno + " and rc_accyear='" + objUtility.mfnGetAccYearFromDate(req.EntryDt) + "'" + strCompWiseSrnoR + ")";
                                DataTable dtAuth = objUtility.OpenDataTable(strsql, sqlCon, objTrans);
                                if (dtAuth.Rows.Count == 0 || (BlnNoAuthReq && Strings.InStr(1, StrNoAuthReq, "P") > 0))
                                {
                                    objUtility.ExecuteSQL("update Receipts set rc_status='Y', rc_authid1='', rc_authid2='' where rc_dpid = '" + req.ExchSeg + "' and rc_debitflag='D' and rc_srno=" + lngSrno + " and rc_accyear='" + objUtility.mfnGetAccYearFromDate(req.EntryDt) + "'" + strCompWiseSrnoR, sqlCon, objTrans);
                                    strsql = "insert into Ledger (ld_clientcd,ld_dt,ld_amount,ld_particular,ld_chequeno,ld_debitflag,ld_documenttype,ld_documentno,ld_entryno,ld_costcenter,";
                                    strsql += " mkrid,mkrdt,ld_accyear,ld_dpid,ld_commondt,ld_common)";
                                    strsql += "select rc_clientcd, rc_receiptdt, rc_amount, rc_particular, rc_chequeno, rc_debitflag, 'P' , rc_srno, rc_entryno, rc_costcenter,";
                                    strsql += " mkrid,mkrdt, rc_accyear,rc_dpid, rc_commondt, rc_common from receipts where rc_debitflag='D' and rc_srno=" + lngSrno + " and rc_accyear='" + objUtility.mfnGetAccYearFromDate(req.EntryDt) + "' and rc_dpid='" + req.ExchSeg.Trim() + "'";
                                    objUtility.ExecuteSQL(strsql, sqlCon, objTrans);
                                }
                                else
                                {
                                    objUtility.ExecuteSQL("update Receipts set rc_status='N', rc_authid1='', rc_authid2='' where rc_dpid = '" + req.ExchSeg.Trim() + "' and rc_debitflag='D' and rc_srno=" + lngSrno + " and rc_accyear='" + objUtility.mfnGetAccYearFromDate(req.EntryDt) + "'" + strCompWiseSrnoR, sqlCon, objTrans);
                                }
                                if (Conversion.Val(req.ChequeNo) > 0)
                                {
                                    objUtility.ExecuteSQL("Update Client_master set cm_bankname='" + Convert.ToInt32(Conversion.Val(req.ChequeNo)) + "' where cm_cd='" + req.BankCode.Trim() + "'", sqlCon, objTrans);
                                }
                            }

                            objTrans.Commit();

                            string strBankName = "";
                            string strBillFlag = "";

                            strsql = "select cm_cd,cm_name from Client_master,Schedule where sc_cd = cm_schedule and sc_bankflag='B'";
                            strsql += " and cm_dpid='" + Strings.Left(req.ExchSeg.Trim(), 1) + "' and (cm_occup='' or cm_occup like '%" + Strings.Right(req.ExchSeg.Trim(), 2) + "%') and cm_cd='" + req.BankCode + "'";
                            if (mode == "ADD")
                            {
                                strsql += " and cm_freezeyn = 'N'";
                            }
                            strsql += " order by cm_name ";
                            dt = objUtility.OpenDataTable(strsql);

                            if (dt.Rows.Count > 0)
                            {
                                strBankName = dt.Rows[0]["cm_name"].ToString().Trim();
                            }

                            strsql = "Select cm_billflag from Client_Master where cm_name = '" + Strings.Left(strBankName, 50) + "'";
                            dt = objUtility.OpenDataTable(strsql);
                            if (dt.Rows.Count > 0)
                            {
                                strBillFlag = dt.Rows[0]["cm_billflag"].ToString().Trim() == "" ? "N" : dt.Rows[0]["cm_billflag"].ToString().Trim();
                            }

                            if (req.Type.Trim() == "R")
                            {
                                recRes.Status = "success";
                                recRes.Response = lngSrno.ToString();
                                return recRes;
                            }
                            else if (req.Type.Trim() == "P")
                            {
                                recRes.Status = "success";
                                recRes.Response = lngSrno.ToString();
                                return recRes;
                            }
                        }
                        catch (Exception ex)
                        {
                            objTrans.Rollback();
                            throw ex;
                        }
                    }
                    else
                    {
                        //lngSrno = Convert.ToInt64(23);
                    }

                    return null;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic Performance_Cash(PerformanceRequestModel model, string compCd, string loginAccess)
        {
            PerformanceCashResponseModel response = new PerformanceCashResponseModel();
            string strSQL = "";
            string strWhere = "";
            string strClientWhere = "";

            if (model.Filter.Client != null)
            {
                if (model.Filter.Client.All(y => y != ""))
                {
                    var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(model.Filter.Client.ToArray(), "##"));
                    strClientWhere += " or cm_cd in('" + fltr.Replace("##", "','") + "')";
                }
            }
            if (model.Filter.Branch != null)
            {
                if (model.Filter.Branch.All(y => y != ""))
                {
                    var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(model.Filter.Branch.ToArray(), "##"));
                    strClientWhere += " or cm_brboffcode in('" + fltr.Replace("##", "','") + "')";
                }
            }
            if (model.Filter.Group != null)
            {
                if (model.Filter.Group.All(y => y != ""))
                {
                    var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(model.Filter.Group.ToArray(), "##"));
                    strClientWhere += " or cm_groupcd in('" + fltr.Replace("##", "','") + "')";
                }
            }
            if (model.Filter.Family != null)
            {
                if (model.Filter.Family.All(y => y != ""))
                {
                    var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(model.Filter.Family.ToArray(), "##"));
                    strClientWhere += " or cm_familycd in('" + fltr.Replace("##", "','") + "')";
                }
            }
            if (strClientWhere.Length > 0)
            {
                strClientWhere = " and (" + strClientWhere.Substring(3) + ") ";
            }

            strWhere += " and td_dt between '" + model.FromDate + "' and  '" + model.ToDate + "'";

            strSQL = "Select cm_groupcd, cm_familycd, cm_cd, cm_name,cm_brboffcode,bm_branchname,'N' as cm_type,isnull(cm_dpactno,''), td_scripcd, ss_name, sum(td_bqty) as 'BuyQty', sum(td_sqty) as 'SellQty' , ";
            strSQL += " sum(td_bqty) as buy,sum(td_bqty* td_marketrate) as buyvalue, ";
            strSQL += " sum(td_sqty) as sell,sum(td_sqty* td_marketrate) as sellvalue, ";
            strSQL += " sum((td_bqty* td_marketrate) + (td_sqty* td_marketrate)) as turnover, ";
            strSQL += " sum(td_bqty - td_sqty ) nqty, ";
            strSQL += " sum((td_bqty - td_sqty )* td_marketrate)as nvalue, ";
            strSQL += " (sum((td_bqty - td_sqty )* td_marketrate) * (-1)) as nvalue1, ";
            strSQL += " 0 as jobing ,0 as delivery, ";
            strSQL += " sum(td_bqty*td_brokerage) as 'BuyBrok', sum(td_sqty*td_brokerage) as 'SellBrok', 0 as cprate, cm_brboffcode, isnull(cm_subbroker,'') as cm_subbroker ";
            strSQL += " from trx with (index(idx_trx_dt_clientcd)) ,Client_master,Branch_master,Settlements, Securities ";
            strSQL += " where cm_cd = td_clientcd and cm_brboffcode = bm_branchcd and td_scripcd = ss_cd and td_stlmnt = se_stlmnt ";
            strSQL += " and td_companycode = '" + compCd + "' and td_cfflag = 'N' and td_marketrate <> 0 " + strClientWhere + strWhere + loginAccess;
            strSQL += " and cm_type <> 'C' ";
            strSQL += " group by  cm_name, cm_groupcd,cm_brboffcode,bm_branchname, cm_familycd, cm_cd,isnull(cm_dpactno,''), cm_brboffcode, cm_subbroker, td_scripcd, ss_name ";

            DataTable dt = objUtility.OpenDataTable(strSQL);

            List<TmpPerformanceCashData> tmpPerformanceCashData = new List<TmpPerformanceCashData>();
            List<PerformanceCashResponseModel> responseModel = new List<PerformanceCashResponseModel>();
            if (dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    TmpPerformanceCashData tmpPerformanceCash = new TmpPerformanceCashData()
                    {
                        ClientCode = dt.Rows[i]["cm_cd"].ToString().Trim(),
                        ClientName = dt.Rows[i]["cm_name"].ToString().Trim(),
                        BranchCode = dt.Rows[i]["cm_brboffcode"].ToString().Trim(),
                        BranchName = dt.Rows[i]["bm_branchname"].ToString().Trim(),
                        ScripCode = dt.Rows[i]["td_scripcd"].ToString().Trim(),
                        ScripName = dt.Rows[i]["ss_name"].ToString().Trim(),
                        SellQty = Convert.ToDouble(dt.Rows[i]["SellQty"].ToString().Trim()),
                        SellValue = Convert.ToDouble(dt.Rows[i]["sellvalue"]),
                        BuyQty = Convert.ToDouble(dt.Rows[i]["BuyQty"].ToString().Trim()),
                        BuyValue = Convert.ToDouble(dt.Rows[i]["buyvalue"]),
                        NetQty = Convert.ToDouble(dt.Rows[i]["nqty"]),
                        NetValue = Convert.ToDouble(dt.Rows[i]["nvalue1"]),
                        BuyBrok = Convert.ToDouble(dt.Rows[i]["BuyBrok"]),
                        SellBrok = Convert.ToDouble(dt.Rows[i]["SellBrok"])
                    };
                    tmpPerformanceCashData.Add(tmpPerformanceCash);
                }

                var lstClientCode = tmpPerformanceCashData.Select(x => x.ClientCode).Distinct().ToList();
                var strClientName = "";
                foreach (var client in lstClientCode)
                {
                    var clientData = tmpPerformanceCashData.Where(x => x.ClientCode == client).ToList();
                    var scripCode = clientData.Select(x => x.ScripCode).Distinct().ToList();
                    strClientName = clientData.FirstOrDefault().ClientName.Trim();
                    List<PerformanceCashData> data = new List<PerformanceCashData>();

                    foreach (var scrip in scripCode)
                    {
                        var scripData = clientData.Where(x => x.ScripCode == scrip).ToList();
                        foreach (var scripRecord in scripData)
                        {
                            data.Add(new PerformanceCashData()
                            {
                                ScripCode = scrip,
                                ScripName = scripData.FirstOrDefault().ScripName.Trim(),
                                SellQty = Convert.ToDouble(scripRecord.SellQty),
                                SellValue = Convert.ToDouble(scripRecord.SellValue),
                                BuyQty = Convert.ToDouble(scripRecord.BuyQty),
                                BuyValue = Convert.ToDouble(scripRecord.BuyValue),
                                NetQty = Convert.ToDouble(scripRecord.NetQty),
                                NetValue = Convert.ToDouble(scripRecord.NetValue),
                                BuyBrok = Convert.ToDouble(scripRecord.BuyBrok),
                                SellBrok = Convert.ToDouble(scripRecord.SellBrok)
                            });
                        }
                    }

                    strSQL = "select sh_clientcd,sh_recordsource,sh_desc,sum(sh_amount) as 'Amount' from Specialcharges,settlements ";
                    strSQL += " where sh_stlmnt=se_stlmnt and sh_companycode = '" + compCd + "' ";
                    strSQL += " and sh_clientcd='" + client + "' and se_endt between '" + model.FromDate + "' and '" + model.ToDate + "' ";
                    strSQL += " group by sh_clientcd,sh_recordsource,sh_desc";
                    DataTable dtCharges = objUtility.OpenDataTable(strSQL);

                    List<PerformanceCashCharges> chargesData = new List<PerformanceCashCharges>();
                    foreach (DataRow dr in dtCharges.Rows)
                    {
                        chargesData.Add(new PerformanceCashCharges()
                        {
                            ChargeCode = dr["sh_recordsource"].ToString().Trim(),
                            Description = dr["sh_desc"].ToString().Trim(),
                            Amount = Convert.ToDouble(dr["Amount"])
                        });
                    }

                    responseModel.Add(new PerformanceCashResponseModel()
                    {
                        ClientCode = client,
                        Name = strClientName,
                        Data = data,
                        Charges = chargesData
                    });
                }

                return responseModel;
            }

            return response;
        }

        public dynamic Performance_FO(PerformanceFORequestModel req, string compCd, string loginAccess)
        {
            try
            {
                string strCompanyCode = "", strExchange = "", strSegment = "", strMain = "", strFromDt = "", strToDt = "", strClientWhere = "";
                string strCode;
                if (req.Filter.Client != null)
                {
                    if (req.Filter.Client.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Client.ToArray(), "##"));
                        strClientWhere += " or cm_cd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Branch != null)
                {
                    if (req.Filter.Branch.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Branch.ToArray(), "##"));
                        strClientWhere += " or cm_brboffcode in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Group != null)
                {
                    if (req.Filter.Group.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Group.ToArray(), "##"));
                        strClientWhere += " or cm_groupcd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Family != null)
                {
                    if (req.Filter.Family.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Family.ToArray(), "##"));
                        strClientWhere += " or cm_familycd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (strClientWhere.Length > 0)
                {
                    strClientWhere = " and (" + strClientWhere.Substring(3) + ") ";
                }
                if (req.FromDate != null)
                {
                    if (req.FromDate.Trim() != "")
                    {
                        strFromDt = req.FromDate.Trim();
                    }
                }
                if (req.ToDate != null)
                {
                    if (req.ToDate.Trim() != "")
                    {
                        strToDt = req.ToDate.Trim();
                    }
                }
                if (req.ExchSeg != null)
                {
                    if (req.ExchSeg.Trim() != "")
                    {
                        strMain = req.ExchSeg.Trim();
                        strCompanyCode = compCd;
                        strExchange = strMain.Substring(1, 1);
                        strSegment = strMain.Substring(2, 1);
                    }
                }

                strsql = "select cm_cd,cm_name,cm_brboffcode,bm_branchname,td_seriesid,sm_sname,sum(td_bqty) as BuyQty, " +
                    "sum(case td_bsflag when 'B' then(case right(sm_prodtype, 1) when 'O' then td_bqty * (sm_strikeprice + td_mainbrrate) * sm_multiplier else td_bqty * td_marketrate * sm_multiplier end) else 0 end) as BuyValue, " +
                    "sum(td_sqty) as SellQty, " +
                    "sum(case td_bsflag when 'S' then(case right(sm_prodtype, 1) when 'O' then td_sqty * (sm_strikeprice + td_mainbrrate) * sm_multiplier else td_sqty* td_marketrate *sm_multiplier end) else 0 end) as SellValue, " +
                    "right(sm_prodtype,1) as 'Type', " +
                    "sum(td_bqty - td_sqty) as NetQty, " +
                    "sum((td_bqty - td_sqty) * td_marketrate) * -1 as NetValue, " +
                    "sum(case td_bsflag when 'B' then(round((td_sqty + td_bqty) * td_brokerage* sm_multiplier,4)) else 0 end) as 'BuyBrok', " +
                    "sum(case td_bsflag when 'S' then(round((td_sqty + td_bqty) * td_brokerage* sm_multiplier,4)) else 0 end) as 'SellBrok' " +
                    "from Trades,Series_master,Client_master,Branch_master where  td_companycode = '" + strCompanyCode + "' and td_Segment = '" + strSegment + "' and td_exchange = '" + strExchange + "'  and td_dt between '" + strFromDt + "' and '" + strToDt + "' " + strClientWhere +
                    " and td_seriesid=sm_seriesid and td_clientcd=cm_cd and cm_brboffcode = bm_branchcd and td_broktype <> 'CPA' and td_trxflag <> 'C' group by td_seriesid,sm_prodtype,sm_sname,cm_cd,cm_name,cm_brboffcode,bm_branchname order by cm_cd";

                DataTable ds = objUtility.OpenDataTable(strsql);

                List<TempPerformanceFOData> Tempdata = new List<TempPerformanceFOData>();
                for (int i = 0; i < ds.Rows.Count; i++)
                {
                    Tempdata.Add(new TempPerformanceFOData()
                    {
                        cm_cd = ds.Rows[i]["cm_cd"].ToString().Trim(),
                        cm_name = ds.Rows[i]["cm_name"].ToString().Trim(),
                        cm_brboffcode = ds.Rows[i]["cm_brboffcode"].ToString().Trim(),
                        bm_branchname = ds.Rows[i]["bm_branchname"].ToString().Trim(),
                        td_seriesid = ds.Rows[i]["td_seriesid"].ToString().Trim(),
                        sm_sname = ds.Rows[i]["sm_sname"].ToString().Trim(),
                        BuyQty = Convert.ToDouble(ds.Rows[i]["BuyQty"].ToString().Trim()),
                        BuyValue = Convert.ToDouble(ds.Rows[i]["BuyValue"].ToString().Trim()),
                        SellQty = Convert.ToDouble(ds.Rows[i]["SellQty"].ToString().Trim()),
                        SellValue = Convert.ToDouble(ds.Rows[i]["SellValue"].ToString().Trim()),
                        Type = ds.Rows[i]["Type"].ToString().Trim(),
                        NetQty = Convert.ToDouble(ds.Rows[i]["NetQty"].ToString().Trim()),
                        NetValue = Convert.ToDouble(ds.Rows[i]["NetValue"].ToString().Trim()),
                        BuyBrok = Convert.ToDouble(ds.Rows[i]["BuyBrok"].ToString().Trim()),
                        SellBrok = Convert.ToDouble(ds.Rows[i]["SellBrok"].ToString().Trim()),
                    });

                }
                List<string> Codes = Tempdata.Select(x => x.cm_cd).Distinct().ToList();
                List<string> Names = Tempdata.Select(x => x.cm_name).Distinct().ToList();
                double Total = Tempdata.Sum(x => x.BuyQty);

                int count = 0;
                List<PerformanceFOResponseModel> ListPerFO = new List<PerformanceFOResponseModel>();
                foreach (var code in Codes)
                {
                    PerformanceFOResponseModel ObjPerFO = new PerformanceFOResponseModel();
                    var Codesdetails = Tempdata.Where(x => x.cm_cd == code).ToList();
                    strCode = code;
                    ObjPerFO.ClientCode = code;
                    ObjPerFO.ClientName = Names[count];
                    ObjPerFO.BranchCode = Tempdata.Where(x => x.cm_cd == code).Select(x => x.cm_brboffcode).FirstOrDefault();
                    ObjPerFO.BranchName = Tempdata.Where(x => x.cm_cd == code).Select(x => x.bm_branchname).FirstOrDefault();
                    List<FuturesOptions> ListFutureOption = new List<FuturesOptions>();
                    List<Charges> ListCharges = new List<Charges>();
                    foreach (var PerCode in Codesdetails)
                    {
                        FuturesOptions ObjFutureOption = new FuturesOptions()
                        {
                            SeriesID = PerCode.td_seriesid,
                            SeriesName = PerCode.sm_sname,
                            BuyQty = PerCode.BuyQty,
                            BuyValue = PerCode.BuyValue,
                            BuyBrok = PerCode.BuyBrok,
                            SellQty = PerCode.SellQty,
                            SellValue = PerCode.SellValue,
                            SellBrok = PerCode.SellBrok,
                            NetQty = PerCode.NetQty,
                            NetValue = PerCode.NetValue,
                            Type = PerCode.Type,
                        };
                        ListFutureOption.Add(ObjFutureOption);
                    }

                    strsql = "select fc_chargecode,fc_desc, sum(fc_amount) as amount " +
                         "from FSpecialcharges " +
                         " where  fc_companycode = '" + strCompanyCode + "' and fc_Segment = '" + strSegment + "' and fc_exchange = '" + strExchange + "' " +
                         " and fc_dt between '" + strFromDt + "' and '" + strToDt + "' and fc_clientcd = '" + strCode + "'" +
                         " group by fc_chargecode,fc_desc";

                    DataTable dss = objUtility.OpenDataTable(strsql);
                    for (int t = 0; t < dss.Rows.Count; t++)
                    {
                        Charges ObjCharges = new Charges();
                        string TempChargCode = dss.Rows[t]["fc_chargecode"].ToString().Trim();
                        ObjCharges.ChargeCode = dss.Rows[t]["fc_chargecode"].ToString().Trim();
                        ObjCharges.Description = dss.Rows[t]["fc_desc"].ToString().Trim();
                        ObjCharges.Amount = Convert.ToDouble(dss.Rows[t]["amount"].ToString().Trim());
                        ListCharges.Add(ObjCharges);
                    }
                    count++;
                    ObjPerFO.FuturesOptions = ListFutureOption;
                    ObjPerFO.Charges = ListCharges;
                    ListPerFO.Add(ObjPerFO);
                }
                return ListPerFO;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic Performance_Commex(PerformanceCommexRequestModel req)
        {
            try
            {
                string strCompanyCode = "", strExchange = "", strMain = "", strFromDt = "", strToDt = "", strClientWhere = "";
                if (req.Filter.Client != null)
                {
                    if (req.Filter.Client.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Client.ToArray(), "##"));
                        strClientWhere += " or cm_cd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Branch != null)
                {
                    if (req.Filter.Branch.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Branch.ToArray(), "##"));
                        strClientWhere += " or cm_brboffcode in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Group != null)
                {
                    if (req.Filter.Group.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Group.ToArray(), "##"));
                        strClientWhere += " or cm_groupcd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Family != null)
                {
                    if (req.Filter.Family.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Family.ToArray(), "##"));
                        strClientWhere += " or cm_familycd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (strClientWhere.Length > 0)
                {
                    strClientWhere = " and (" + strClientWhere.Substring(3) + ") ";
                }
                if (req.FromDate != null)
                {
                    if (req.FromDate.Trim() != "")
                    {
                        strFromDt = req.FromDate.Trim();
                    }
                }
                if (req.ToDate != null)
                {
                    if (req.ToDate.Trim() != "")
                    {
                        strToDt = req.ToDate.Trim();
                    }
                }
                if (req.ExchSeg != null)
                {
                    if (req.ExchSeg.Trim() != "")
                    {
                        strMain = req.ExchSeg.Trim();
                        strCompanyCode = "'" + strMain.Substring(0, 1) + "'";
                        strExchange = "'" + strMain.Substring(1, 1) + "'";
                    }
                }
                strsql = "select * from other_products where op_product='Commex' and op_status='A'";
                DataTable d = objUtility.OpenDataTable(strsql);
                string Commexconn = "[" + d.Rows[0]["Op_Server"].ToString().Trim() + "].[" + d.Rows[0]["Op_DataBase"].ToString().Trim() + "].[" + d.Rows[0]["Op_Owner"].ToString().Trim() + "]";

                strsql = "select cm_cd,cm_name,cm_brboffcode,bm_branchname,td_seriesid,sm_sname, " +
                         "case right(sm_prodtype, 1) when 'F' then SUM(td_bqty) else 0 end as BuyQty, " +
                         "case right(sm_prodtype, 1) when 'F' then(sum(case td_bsflag when 'B' then(case right(sm_prodtype, 1) when 'O' then td_bqty * (sm_strikeprice + td_mainbrrate) * sm_multiplier else td_bqty* td_marketrate *sm_multiplier end) else 0 end)) else 0 end BuyValue, " +
                         "case right(sm_prodtype, 1) when 'F' then SUM(td_sqty) else 0 end as SellQty, " +
                         "case right(sm_prodtype, 1) when 'F' then(sum(case td_bsflag when 'S' then(case right(sm_prodtype, 1) when 'O' then td_sqty * (sm_strikeprice + td_mainbrrate) * sm_multiplier else td_sqty* td_marketrate *sm_multiplier end) else 0 end)) else 0 end SellValue, " +
                         "right(sm_prodtype, 1) as ProductType, " +
                         "case right(sm_prodtype, 1) when 'F' then sum(td_bqty -td_sqty) else 0 end as NetQty, " +
                         "case right(sm_prodtype, 1) when 'F' then sum((td_bqty -td_sqty) *td_marketrate) *-1 else 0 end as NetValue, " +
                         "case right(sm_prodtype, 1) when 'F' then sum(case td_bsflag when 'B' then(round((td_sqty + td_bqty) * td_brokerage * sm_multiplier, 4)) else 0 end) else 0 end as BuyBrok, " +
                         "case right(sm_prodtype, 1) when 'F' then sum(case td_bsflag when 'S' then(round((td_sqty + td_bqty) * td_brokerage * sm_multiplier, 4)) else 0 end) else 0 end as SellBrok " +
                         "from " + Commexconn + ".Trades," + Commexconn + ".Series_master," + Commexconn + ".Client_master," + Commexconn + ".Branch_master where td_companycode = " + strCompanyCode + " and td_exchange = " + strExchange + strClientWhere + " and cm_cd = td_clientcd and cm_brboffcode = bm_branchcd and td_dt between '" + strFromDt + "' and '" + strToDt + "'  and td_seriesid = sm_seriesid " +
                         "group by td_seriesid,sm_prodtype,sm_sname,cm_cd,cm_name,cm_brboffcode,bm_branchname " +
                         "order by cm_cd";

                DataTable ds = objUtility.OpenDataTable(strsql);

                List<PerformanceCommexResponseModel> tempItom = new List<PerformanceCommexResponseModel>();
                for (int i = 0; i < ds.Rows.Count; i++)
                {
                    tempItom.Add(new PerformanceCommexResponseModel()
                    {
                        ClientCode = ds.Rows[i]["cm_cd"].ToString().Trim(),
                        ClientName = ds.Rows[i]["cm_name"].ToString().Trim(),
                        BranchCode = ds.Rows[i]["cm_brboffcode"].ToString().Trim(),
                        BranchName = ds.Rows[i]["bm_branchname"].ToString().Trim(),
                        SeriesID = ds.Rows[i]["td_seriesid"].ToString().Trim(),
                        SeriesName = ds.Rows[i]["sm_sname"].ToString().Trim(),
                        BuyQty = Convert.ToDouble(ds.Rows[i]["BuyQty"].ToString().Trim()),
                        BuyValue = Convert.ToDouble(ds.Rows[i]["BuyValue"].ToString().Trim()),
                        SellQty = Convert.ToDouble(ds.Rows[i]["SellQty"].ToString().Trim()),
                        SellValue = Convert.ToDouble(ds.Rows[i]["SellValue"].ToString().Trim()),
                        BuyBrok = Convert.ToDouble(ds.Rows[i]["BuyBrok"].ToString().Trim()),
                        SellBrok = Convert.ToDouble(ds.Rows[i]["SellBrok"].ToString().Trim()),
                        NetQty = Convert.ToDouble(ds.Rows[i]["NetQty"].ToString().Trim()),
                        NetValue = Convert.ToDouble(ds.Rows[i]["NetValue"].ToString().Trim()),
                        Type = ds.Rows[i]["ProductType"].ToString().Trim(),
                    });
                }

                List<string> Codes = tempItom.Select(x => x.ClientCode).Distinct().ToList();
                List<string> Names = tempItom.Select(x => x.ClientName).Distinct().ToList();

                int count = 0;
                List<PerformanceFOResponseModel> ListPerFO = new List<PerformanceFOResponseModel>();
                foreach (var code in Codes)
                {
                    PerformanceFOResponseModel ObjPerFO = new PerformanceFOResponseModel();
                    var Codesdetails = tempItom.Where(x => x.ClientCode == code).ToList();
                    ObjPerFO.ClientCode = code;
                    ObjPerFO.ClientName = Names[count];
                    List<FuturesOptions> ListFutureOption = new List<FuturesOptions>();
                    List<Charges> ListCharges = new List<Charges>();
                    foreach (var PerCode in Codesdetails)
                    {
                        FuturesOptions ObjFutureOption = new FuturesOptions()
                        {
                            SeriesID = PerCode.SeriesID,
                            SeriesName = PerCode.SeriesName,
                            BuyQty = PerCode.BuyQty,
                            BuyValue = PerCode.BuyValue,
                            BuyBrok = PerCode.BuyBrok,
                            SellQty = PerCode.SellQty,
                            SellValue = PerCode.SellValue,
                            SellBrok = PerCode.SellBrok,
                            NetQty = PerCode.NetQty,
                            NetValue = PerCode.NetValue,
                            Type = PerCode.Type,
                        };
                        ListFutureOption.Add(ObjFutureOption);
                    }
                    strsql = "select fc_chargecode,fc_desc, sum(fc_amount) as amount " +
                         "from " + Commexconn + ".FSpecialcharges " +
                         " where  fc_companycode = " + strCompanyCode + " and fc_exchange = " + strExchange +
                         " and fc_dt between '" + strFromDt + "' and '" + strToDt + "' and fc_clientcd = '" + code + "'" +
                         " group by fc_chargecode,fc_desc";

                    DataTable dss = objUtility.OpenDataTable(strsql);
                    for (int t = 0; t < dss.Rows.Count; t++)
                    {
                        Charges ObjCharges = new Charges();
                        string TempChargCode = dss.Rows[t]["fc_chargecode"].ToString().Trim();
                        ObjCharges.ChargeCode = dss.Rows[t]["fc_chargecode"].ToString().Trim();
                        ObjCharges.Description = dss.Rows[t]["fc_desc"].ToString().Trim();
                        ObjCharges.Amount = Convert.ToDouble(dss.Rows[t]["amount"].ToString().Trim());
                        ListCharges.Add(ObjCharges);
                    }
                    count++;
                    ObjPerFO.FuturesOptions = ListFutureOption;
                    ObjPerFO.Charges = ListCharges;
                    ListPerFO.Add(ObjPerFO);
                }
                return ListPerFO;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic EntryGSTInvoice(string userId, string compcd, EntryGSTInvoiceRequest req)
        {
            try
            {
                string strSQL = "";
                DataTable dt;
                SqlTransaction objTrans;
                string strMessage = "";
                string strCmschedule = "";
                string CurrentDate = DateTime.UtcNow.ToString("yyyyMMdd");
                userId = userId.ToUpper();
                EntryGSTInvoiceResponse BillsRes = new EntryGSTInvoiceResponse();
                List<EntryGSTInvoiceRes> lstResponse = new List<EntryGSTInvoiceRes>();

                strCmschedule = objUtility.GetSysParmSt("CMSCHEDULE", "");

                int intCount = 0;
                foreach (var item in req.Data)
                {
                    intCount++;
                    strMessage = "";

                    if (item.Clientcode.Trim() == "")
                    {
                        strMessage += "Client Code cannot be blank" + Environment.NewLine;
                    }
                    else
                    {
                        strSQL = "select count(0) from client_master where cm_schedule = '" + strCmschedule + "' and cm_type <> 'C' and cm_cd = '" + objUtility.mfnReplaceForSQLInjection(item.Clientcode).Trim() + "'";
                        dt = objUtility.OpenDataTable(strSQL);
                        if (Convert.ToInt32(dt.Rows[0][0].ToString()) == 0)
                        {
                            strMessage += "Invalid Client code [" + item.Clientcode.Trim() + "]" + Environment.NewLine;
                        }
                    }

                    if (item.CreditAccount.ToUpper().Trim() == "")
                    {
                        strMessage += "Credit Account Code Cannot Be Blank. Enter A Valid Account Code." + Environment.NewLine;
                    }
                    else
                    {
                        strSQL = "select cm_cd, cm_name from client_master ,Schedule where sc_cd=cm_schedule and cm_schedule <> " + strCmschedule + " and cm_cd='" + objUtility.mfnReplaceForSQLInjection(item.CreditAccount).Trim() + "'";
                        dt = objUtility.OpenDataTable(strSQL);
                        if (dt.Rows.Count == 0)
                        {
                            strMessage += "Credit Account Code Not Found. Enter A Valid Account Code.[" + item.CreditAccount.Trim() + "]" + Environment.NewLine;
                        }
                    }

                    if (strMessage != "")
                    {
                        lstResponse.Add(new EntryGSTInvoiceRes { LineNo = intCount.ToString(), Response = strMessage });
                    }
                }

                if (lstResponse.Count > 0)
                {
                    BillsRes.Status = "error";
                    BillsRes.Data = new ErrorResponse { Message = "", Data = lstResponse };
                    return BillsRes;
                }

                bool blnGSTApplicable = false;
                string strGSTEffDt = "";
                if (Strings.Right(req.ExchSeg.Trim(), 1) == "C")
                {
                    strGSTEffDt = objUtility.fnFireQueryTradeWeb("Charges_master", "cg_InactiveDt", "cg_companycode+cg_exchange='" + Strings.Left(req.ExchSeg.Trim(), 2) + "' and cg_cd", "01", true).Trim();
                }
                else
                {
                    strGSTEffDt = objUtility.fnFireQueryTradeWeb("FCharges_master", "fc_InactiveDt", "fc_companycode+fc_exchange+fc_segment='" + req.ExchSeg + "' and fc_cd", "01", true).Trim();
                }
                if (DateTime.ParseExact(req.Date, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture) >= DateTime.ParseExact(strGSTEffDt, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture))
                {
                    strSQL = "select Count(0) from GST_Rates Where GR_EffDate in ( select isNull(MAX(GR_EffDate),'') from GST_Rates Where GR_EffDate <='" + req.Date + "' ) ";
                    dt = objUtility.OpenDataTable(strSQL);
                    if (Convert.ToInt32(dt.Rows[0][0]) > 0)
                    {
                        blnGSTApplicable = true;
                    }
                }

                string strMaxDt = GetMaxGSTBillDt(compcd, req.Date);
                if (blnGSTApplicable)
                {
                    if (strMaxDt != "")
                    {
                        if (DateTime.ParseExact(req.Date, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture) < DateTime.ParseExact(strMaxDt, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture))
                        {
                            BillsRes.Status = "error";
                            BillsRes.Data = new ErrorResponse { Message = "GST Bills already generated for " + objUtility.mfnFormatdate(strMaxDt, UtilityCommon.eNewDateformat.eShortDate) + ", Post cannot be prior to that.", Data = new List<EntryGSTInvoiceRes>() { } };
                            return BillsRes;
                        }
                    }
                }

                strSQL = "Select count(0) from GSTBills Where GB_CompanyCode='" + req.ExchSeg.Trim() + "'  and GB_Series='OTH' ";
                strSQL += " and GB_CommonDt = '" + req.Date.Trim() + "' and GB_Common = 'OtherCharges' ";
                dt = objUtility.OpenDataTable(strSQL);
                if (Convert.ToInt32(dt.Rows[0][0]) > 0)
                {
                    BillsRes.Status = "error";
                    BillsRes.Data = new ErrorResponse { Message = "GST Bills already generated for " + objUtility.mfnFormatdate(req.Date.Trim(), UtilityCommon.eNewDateformat.eShortDate) + ", Post cannot be prior to that.", Data = new List<EntryGSTInvoiceRes>() { } };
                    return BillsRes;
                }

                string connectionString = objUtility.GetConnectionStr();
                using (SqlConnection sqlConn = new SqlConnection(connectionString))
                {
                    sqlConn.Open();
                    objTrans = sqlConn.BeginTransaction();
                    SqlCommand cmd = sqlConn.CreateCommand();
                    cmd.Transaction = objTrans;
                    SqlDataAdapter sqlDtAdap = new SqlDataAdapter();
                    SqlCommandBuilder sqlCmdBld = new SqlCommandBuilder(sqlDtAdap);
                    BulkAccEntCreateTmpTable(sqlConn, objTrans);
                    double DblDTtl = 0;
                    objUtility.ExecuteSQL("truncate table #tempj", sqlConn, objTrans);

                    string tr_Costcenter = "000";
                    string strClnt = "";
                    string strNarr = "";
                    double dblAmt = 0;
                    string strDrCr = "";
                    int intRecs = 0;
                    int intEntNo = 0;
                    int intRsp2 = 0;
                    int intEntNocost = 0;

                    foreach (var item in req.Data)
                    {
                        strClnt = item.Clientcode.ToUpper();
                        strNarr = item.Narration.Trim();
                        if (item.Amount == 0)
                        {
                            continue;
                        }
                        dblAmt = Math.Abs(item.Amount);

                        DblDTtl = DblDTtl + dblAmt;

                        intRecs++;
                        intEntNo++;
                        if (tr_Costcenter.Trim() == "")
                        {
                            intEntNocost++;
                        }

                        strSQL = "insert into #tempj values ('" + strDrCr + "', " + intRecs + "," + intEntNo + ",'" + strClnt + "'," + (strDrCr == "C" ? (-1) * dblAmt : dblAmt) + ", 1," + intRsp2 + "," + (dblAmt > 0 ? 0 : 1) + ",'','" + strNarr + "','','','" + tr_Costcenter + "'," + intEntNocost + ",'',0,'" + item.CreditAccount.ToUpper().Trim() + "')";
                        objUtility.ExecuteSQL(strSQL, sqlConn, objTrans);
                    }

                    strSQL = "update a set tr_response1=0 from #tempj a, client_master where cm_cd=tr_clientcd and cm_schedule=" + strCmschedule + " and cm_type<> 'C' ";
                    objUtility.ExecuteSQL(strSQL, sqlConn, objTrans);

                    strSQL = "update #tempj set tr_response2=2 ";
                    strSQL += " from #tempj , Client_master ";
                    strSQL += " where tr_clientcd = cm_cd and cm_freezeyn = 'A'";
                    objUtility.ExecuteSQL(strSQL, sqlConn, objTrans);

                    objTrans.Commit();

                    string strX = "and cm_cd in (select distinct tr_clientcd from #tempj)";
                    strMessage = mfnValidateGST(strX, sqlConn, objTrans);
                    if (strMessage.Trim() != "")
                    {
                        BillsRes.Status = "error";
                        BillsRes.Data = new ErrorResponse { Message = strMessage, Data = new List<EntryGSTInvoiceRes>() { } };
                        return BillsRes;
                    }

                    objTrans = sqlConn.BeginTransaction();
                    string strType = objUtility.GetSysParmSt("GST_COLLACT", "");

                    if (strType.Trim() == "")
                    {
                        BillsRes.Status = "error";
                        BillsRes.Data = new ErrorResponse { Message = "Impoper GST Settings", Data = new List<EntryGSTInvoiceRes>() { } };
                        return BillsRes;
                    }

                    try
                    {
                        objUtility.ExecuteSQL("alter table #tempj add tr_brboffcode varchar(6) ", sqlConn, objTrans);
                        objUtility.ExecuteSQL("alter table #tempj add tr_ClientGSTCd VarChar(2) ", sqlConn, objTrans);
                        objUtility.ExecuteSQL("alter table #tempj add tr_ClientStType VarChar(1) ", sqlConn, objTrans);
                        objUtility.ExecuteSQL("alter table #tempj add tr_BrGSTCd VarChar(2) ", sqlConn, objTrans);
                        objUtility.ExecuteSQL("alter table #tempj add tr_NetAmt money ", sqlConn, objTrans);
                        objUtility.ExecuteSQL("alter table #tempj add tr_SGST money ", sqlConn, objTrans);
                        objUtility.ExecuteSQL("alter table #tempj add tr_IGST money ", sqlConn, objTrans);
                        objUtility.ExecuteSQL("alter table #tempj add tr_UGST money ", sqlConn, objTrans);
                        objUtility.ExecuteSQL("alter table #tempj add tr_CGST money ", sqlConn, objTrans);
                        objUtility.ExecuteSQL("alter table #tempj add tr_gbNO numeric ", sqlConn, objTrans);
                        objUtility.ExecuteSQL("alter table #tempj add tr_INVNO numeric ", sqlConn, objTrans);
                        objUtility.ExecuteSQL("alter table #tempj add tr_entrySrno numeric IDENTITY(1,1) ", sqlConn, objTrans);

                        objUtility.ExecuteSQL("Update #tempj set tr_SGST = 0 , tr_IGST = 0 , tr_UGST = 0 , tr_CGST = 0,tr_BrGSTCd='',tr_gbNO=0,tr_INVNO=0,tr_NetAmt=0", sqlConn, objTrans);

                        strSQL = "Update #tempj ";
                        strSQL += " Set tr_brboffcode=cm_brboffcode, tr_ClientGSTCd=st_GSTCd, tr_ClientStType=st_Type,tr_BrGSTCd=left(bm_pwd,2) ";
                        strSQL += " From Client_master,Client_info,Branch_master,State_Master";
                        strSQL += " Where tr_clientcd=cm_cd and cm_brboffcode = bm_Branchcd and cm_cd = cm2_cd and cm_State = st_State ";
                        objUtility.ExecuteSQL(strSQL, sqlConn, objTrans);

                        strSQL = "Update #tempj ";
                        strSQL += " Set tr_brboffcode = RM_regno , tr_ClientGSTCd=st_GSTCd, tr_ClientStType=st_Type,tr_BrGSTCd=left(bm_pwd,2) ";
                        strSQL += " From SubBrokers,Branch_master,State_Master";
                        strSQL += " Where tr_clientcd = ISNULL(RM_GLActCode,'') and bm_branchcd =  RM_regno and isnull(rm_state,'') = st_State  ";
                        objUtility.ExecuteSQL(strSQL, sqlConn, objTrans);

                        strSQL = "Update #tempj ";
                        strSQL += " set tr_ClientGSTCd = tr_BrGSTCd ,tr_ClientStType= st_Type From State_Master ";
                        strSQL += " Where tr_ClientStType = '' and tr_BrGSTCd = st_GSTCd ";
                        objUtility.ExecuteSQL(strSQL, sqlConn, objTrans);

                        strSQL = "Update #tempj set ";
                        strSQL += " tr_IGST = Round(tr_amount*case When tr_ClientGSTCD  <> tr_BrGSTCD Then GR_IGST else 0 end/100,2), ";
                        strSQL += " tr_UGST = Round(tr_amount*case When tr_ClientGSTCD  = tr_BrGSTCD and tr_ClientStType= 'U' Then GR_UGST else 0 end/100,2), ";
                        strSQL += " tr_SGST = Round(tr_amount*case When tr_ClientGSTCD  = tr_BrGSTCD and tr_ClientStType= 'S' Then GR_SGST else 0 end/100,2), ";
                        strSQL += " tr_CGST = Round(tr_amount*case When tr_ClientGSTCD  = tr_BrGSTCD Then GR_CGST else 0 end/100,2) ";
                        strSQL += " from GST_Rates ";
                        strSQL += " Where GR_EffDate = (Select max(GR_EffDate) from GST_Rates  Where GR_EffDate <= '" + req.Date.Trim() + "')";
                        objUtility.ExecuteSQL(strSQL, sqlConn, objTrans);

                        objUtility.ExecuteSQL("Update #tempj set tr_NetAmt = Round((tr_amount + tr_SGST+tr_IGST+tr_UGST+tr_CGST),2) ", sqlConn, objTrans);

                        string strBRGSTCD = "";
                        int intContract;
                        strSQL = "Select tr_entrySrno,tr_BrGSTCd from #tempj Where tr_InvNO = 0 and tr_BrGSTCd <> '' Order by tr_BrGSTCd,tr_entrySrno";
                        dt = objUtility.OpenDataTable(strSQL, sqlConn, objTrans);
                        if (dt.Rows.Count > 0)
                        {
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                strBRGSTCD = dt.Rows[i]["tr_BrGSTCd"].ToString().Trim();
                                intContract = 1;
                                while (strBRGSTCD == Convert.ToString(dt.Rows[i]["tr_BrGSTCd"]))
                                {
                                    strSQL = "Update #tempj set tr_InvNO = " + intContract;
                                    strSQL += " Where tr_entrySrno = " + dt.Rows[i]["tr_entrySrno"];
                                    objUtility.ExecuteSQL(strSQL, sqlConn, objTrans);
                                    intContract++;

                                    i += 1;
                                    if (i >= dt.Rows.Count)
                                        break;
                                }
                            }

                            strSQL = "Update #tempj set tr_InvNO = tr_InvNO + gb_INVNO from ( ";

                            strSQL += " select st_GSTCd,Max(isNull(GB_INVNO,0)) gb_INVNO ";
                            strSQL += " From State_master left join GSTBills on st_GSTCd = gb_BrSTCd ";
                            strSQL += " and left(gb_companycode,1) = '" + compcd + "'";
                            strSQL += " and gb_date between '" + objUtility.mfnGetAccstartdatefromdate(req.Date.Trim(), UtilityCommon.eNewDateformat.EDATABASE) + "'";
                            strSQL += " and '" + objUtility.mfnGetAccenddatefromdate(req.Date.Trim(), UtilityCommon.eNewDateformat.EDATABASE) + "'" + " and GB_Series='OTH' ";
                            strSQL += " Group By st_GSTCd  ";

                            strSQL += " ) a ";
                            strSQL += " Where tr_BrGSTCd = st_GSTCd and tr_InvNO > 0 ";
                            objUtility.ExecuteSQL(strSQL, sqlConn, objTrans);
                        }
                        strSQL = "Update #tempj set tr_InvNO = abs(tr_InvNO) ";
                        objUtility.ExecuteSQL(strSQL, sqlConn, objTrans);
                        long lngGstSrNo;
                        strSQL = "select isnull(max(GB_SrNo),0) as maxsrno from GSTBills ";
                        strSQL += " where GB_AccYear='" + objUtility.mfnGetAccYearFromDate(req.Date.Trim()) + "'";
                        if (objUtility.fnFireQuery("SysParameter", "SP_SysValue", "SP_ParmCd", "COMPWISESRNO", true) == "Y")
                        {
                            strSQL += " and left(GB_CompanyCode,1)='" + Strings.Left(req.ExchSeg, 1) + "'";
                        }
                        else
                        {
                            strSQL += " and GB_CompanyCode ='" + req.ExchSeg + "'";
                        }

                        dt = objUtility.OpenDataTable(strSQL, sqlConn, objTrans);

                        lngGstSrNo = Convert.ToInt64(dt.Rows[0]["maxsrno"].ToString().Trim());

                        objUtility.ExecuteSQL("Update #tempj set tr_gbNO = tr_entrySrno +" + lngGstSrNo + " Where tr_gbNO = 0 ", sqlConn, objTrans);

                        strSQL = "insert into GSTBills select tr_gbNO GB_SrNo,'" + req.Date.Trim() + "' GB_Date,tr_BrGSTCd,tr_INVNO,tr_ClientCd,";
                        strSQL += " tr_CreditAcc GB_CClientCd,tr_narr GB_particular,";
                        strSQL += " tr_amount GB_GrossAmt,tr_SGST GB_SGST,tr_IGST GB_IGST,tr_UGST GB_UGST, tr_CGST GB_CGST,";
                        strSQL += " tr_NetAmt GB_NetAmt,'" + req.ExchSeg + "' GB_CompanyCode,'" + objUtility.mfnGetAccYearFromDate(req.Date.Trim()) + "' GB_AccYear,";
                        strSQL += " '" + req.Date.Trim() + "' GB_CommonDt,'" + "OtherCharges" + "' GB_Common,";
                        strSQL += " '" + userId + "' GB_mkrid,'" + CurrentDate + "' GB_mkrdt,  ";
                        strSQL += "'OTH' GB_Series, '' GB_Filler1, '' GB_Filler2, '' GB_Filler3, 0 GB_FillerN1, 0 GB_FillerN2, 0 GB_FillerN3 ";
                        strSQL += " from #tempj ";
                        objUtility.ExecuteSQL(strSQL, sqlConn, objTrans);

                        strSQL = " Insert into Ledger ";
                        strSQL += " select tr_ClientCd ld_clientcd,'" + req.Date.Trim() + "' ld_dt,tr_NetAmt ld_amount,tr_narr ld_particular,";
                        strSQL += " '' ld_chequeno,'D' ld_debitflag,'G' ld_documenttype,tr_gbNO ld_documentno,1 ld_entryno,'000' ld_costcenter,";
                        strSQL += " '" + userId + "' mkrid,'" + CurrentDate + "' mkrdt,'" + objUtility.mfnGetAccYearFromDate(req.Date.Trim()) + "' ld_accyear,";
                        strSQL += " '" + req.ExchSeg + "' ld_dpid,'" + req.Date.Trim() + "' ld_commondt,'" + "OtherCharges" + "' ld_common  from #tempj ";
                        objUtility.ExecuteSQL(strSQL, sqlConn, objTrans);

                        strSQL = " Insert into Ledger ";
                        strSQL += " select tr_CreditAcc ld_clientcd,'" + req.Date.Trim() + "' ld_dt,-tr_amount ld_amount,";
                        strSQL += " Left(ltrim(rtrim(tr_narr)) + ' [' + ltrim(rtrim(tr_ClientCd)) + ']', 200) ld_particular,'' ld_chequeno,'C' ld_debitflag,'G' ld_documenttype,tr_gbNO ld_documentno,2 ld_entryno,'000' ld_costcenter,";
                        strSQL += " '" + userId + "' mkrid,'" + CurrentDate + "' mkrdt,'" + objUtility.mfnGetAccYearFromDate(req.Date.Trim()) + "' ld_accyear,";
                        strSQL += " '" + req.ExchSeg + "' ld_dpid,'" + req.Date.Trim() + "' ld_commondt,'" + "OtherCharges" + "' ld_common from #tempj ";
                        objUtility.ExecuteSQL(strSQL, sqlConn, objTrans);

                        var strSelectfield = new string[4];
                        string strCollectACT;

                        strCollectACT = objUtility.fnFireQuery("SysParameter", "SP_SysValue", "SP_ParmCd", "GST_COLLACT", true);
                        int iGST;
                        if (strCollectACT == "C")
                        {
                            for (iGST = 0; iGST <= 3; iGST++)
                                strSelectfield[iGST] = "'" + objUtility.fnFireQuery("SysParameter", "SP_SysValue", "SP_ParmCd", fnChoose(iGST));
                        }
                        else if (strCollectACT == "S")
                        {
                            for (iGST = 0; iGST <= 3; iGST++)
                                strSelectfield[iGST] = "'" + objUtility.fnFireQuery("SysParameter", "SP_SysValue", "SP_ParmCd", fnChoose(iGST)) + "' +tr_BrGSTCd ";
                        }
                        else if (strCollectACT == "B")
                        {
                            for (iGST = 0; iGST <= 3; iGST++)
                                strSelectfield[iGST] = "'" + objUtility.fnFireQuery("SysParameter", "SP_SysValue", "SP_ParmCd", fnChoose(iGST)) + "' +tr_brboffcode ";
                        }

                        strSQL = " Insert into Ledger ";
                        strSQL += " select " + strSelectfield[0] + " ld_clientcd,'" + req.Date.Trim() + "' ld_dt,-tr_SGST ld_amount,Left(ltrim(rtrim(tr_narr)) + ' [' + ltrim(rtrim(tr_ClientCd)) + ']', 200) ld_particular,";
                        strSQL += " '' ld_chequeno,'C' ld_debitflag,'G' ld_documenttype,tr_gbNO ld_documentno,3 ld_entryno,'000' ld_costcenter,";
                        strSQL += " '" + userId + "' mkrid,'" + CurrentDate + "' mkrdt,'" + objUtility.mfnGetAccYearFromDate(req.Date.Trim()) + "' ld_accyear,";
                        strSQL += " '" + req.ExchSeg + "' ld_dpid,'" + req.Date.Trim() + "' ld_commondt,'" + "OtherCharges" + "' ld_common ";
                        strSQL += " from #tempj Where tr_SGST > 0 ";

                        objUtility.ExecuteSQL(strSQL, sqlConn, objTrans);

                        strSQL = " Insert into Ledger ";
                        strSQL += " select " + strSelectfield[1] + " ld_clientcd,'" + req.Date.Trim() + "' ld_dt,-tr_IGST ld_amount,Left(ltrim(rtrim(tr_narr)) + ' [' + ltrim(rtrim(tr_ClientCd)) + ']', 200) ld_particular,";
                        strSQL += " '' ld_chequeno,'C' ld_debitflag,'G' ld_documenttype,tr_gbNO ld_documentno,4 ld_entryno,'000' ld_costcenter,";
                        strSQL += " '" + userId + "' mkrid,'" + CurrentDate + "' mkrdt,'" + objUtility.mfnGetAccYearFromDate(req.Date.Trim()) + "' ld_accyear,";
                        strSQL += " '" + req.ExchSeg + "' ld_dpid,'" + req.Date.Trim() + "' ld_commondt,'" + "OtherCharges" + "' ld_common ";
                        strSQL += " from #tempj Where tr_IGST > 0 ";

                        objUtility.ExecuteSQL(strSQL, sqlConn, objTrans);

                        strSQL = " Insert into Ledger ";
                        strSQL += " select " + strSelectfield[2] + " ld_clientcd,'" + req.Date.Trim() + "' ld_dt,-tr_UGST ld_amount,Left(ltrim(rtrim(tr_narr)) + ' [' + ltrim(rtrim(tr_ClientCd)) + ']', 200) ld_particular,";
                        strSQL += " '' ld_chequeno,'C' ld_debitflag,'G' ld_documenttype,tr_gbNO ld_documentno,5 ld_entryno,'000' ld_costcenter,";
                        strSQL += " '" + userId + "' mkrid,'" + CurrentDate + "' mkrdt,'" + objUtility.mfnGetAccYearFromDate(req.Date.Trim()) + "' ld_accyear,";
                        strSQL += " '" + req.ExchSeg + "' ld_dpid,'" + req.Date.Trim() + "' ld_commondt,'" + "OtherCharges" + "' ld_common ";
                        strSQL += " from #tempj Where tr_UGST > 0 ";

                        objUtility.ExecuteSQL(strSQL, sqlConn, objTrans);

                        strSQL = " Insert into Ledger ";
                        strSQL += " select " + strSelectfield[3] + " ld_clientcd,'" + req.Date.Trim() + "' ld_dt,-tr_CGST ld_amount,Left(ltrim(rtrim(tr_narr)) + ' [' + ltrim(rtrim(tr_ClientCd)) + ']', 200) ld_particular,";
                        strSQL += " '' ld_chequeno,'C' ld_debitflag,'G' ld_documenttype,tr_gbNO ld_documentno,6 ld_entryno,'000' ld_costcenter,";
                        strSQL += " '" + userId + "' mkrid,'" + CurrentDate + "' mkrdt,'" + objUtility.mfnGetAccYearFromDate(req.Date.Trim()) + "' ld_accyear,";
                        strSQL += " '" + req.ExchSeg + "' ld_dpid,'" + req.Date.Trim() + "' ld_commondt,'" + "OtherCharges" + "' ld_common ";
                        strSQL += " from #tempj Where tr_CGST > 0 ";
                        objUtility.ExecuteSQL(strSQL, sqlConn, objTrans);

                        strSQL = "select Min(tr_gbNO), Max(tr_gbNO) from #tempj";
                        dt = objUtility.OpenDataTable(strSQL, sqlConn, objTrans);

                        BillsRes.Status = "success";
                        BillsRes.Data = new ErrorResponse { Message = "Data inserted as GST Invoice (SrNo From " + dt.Rows[0][0].ToString().Trim() + " To " + dt.Rows[0][1].ToString().Trim() + ")", Data = new List<EntryGSTInvoiceRes>() { } };
                        objTrans.Commit();
                        return BillsRes;
                    }
                    catch (Exception ex)
                    {
                        objTrans.Rollback();
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region Bulk Accounting Entries

        public void BulkAccEntCreateTmpTable(SqlConnection sqlConn, SqlTransaction SqlTrans)
        {
            string strSQL;
            DataTable dt;
            int intChqLength = 8;

            objUtility.ExecuteSQL("if OBJECT_ID('tempdb..#tempj') is not null Drop Table #tempj", sqlConn, SqlTrans);

            strSQL = "Select col_length('Receipts','rc_chequeno') as Length";
            dt = objUtility.OpenDataTable(strSQL, sqlConn, SqlTrans);
            if (dt.Rows.Count > 0)
            {
                intChqLength = Convert.ToInt32(dt.Rows[0]["Length"]);
            }

            strSQL = "create table #tempj (tr_debitflag char(25), ";
            strSQL += "tr_LineNo numeric, ";
            strSQL += "tr_entryno numeric, ";
            strSQL += "tr_clientcd char(16), ";
            strSQL += "tr_amount money, ";
            strSQL += "tr_response1 numeric, ";
            strSQL += "tr_response2 numeric, ";
            strSQL += "tr_response3 numeric, ";
            strSQL += "tr_ChqNo varchar (" + intChqLength + "), ";
            strSQL += "tr_narr varchar (200),";
            strSQL += "tr_MICR char (9), ";
            strSQL += "tr_ActNo char (20), ";
            strSQL += "tr_Costcenter char (3), ";
            strSQL += "tr_response4 numeric, ";
            strSQL += "tr_ClearDate char(30), ";
            strSQL += "tr_response5 numeric, ";
            strSQL += "tr_CreditAcc char(8) ";
            strSQL += " )";
            objUtility.ExecuteSQL(strSQL, sqlConn, SqlTrans);
        }

        public string mfnValidateGST(string strClientWhere, SqlConnection SqlCon, SqlTransaction SqlTr)
        {
            string strSQL;
            string strMessage = "";
            DataTable dtGST;

            strSQL = "Select cm_cd, cm_name From Client_master Where Ltrim(Rtrim(cm_brboffcode)) not in (Select Ltrim(Rtrim(bm_branchcd)) from Branch_Master) " + strClientWhere;
            dtGST = objUtility.OpenDataTable(strSQL, SqlCon, SqlTr);
            if (dtGST.Rows.Count > 0)
            {
                strMessage += "Improper Branch detail for following clients: \n";
                foreach (DataRow dr in dtGST.Rows)
                {
                    strMessage += dr["cm_cd"].ToString().Trim() + " (" + dr["cm_name"].ToString().Trim() + ") \n";
                }
            }

            strSQL = "select cm_cd, cm_name from Client_master,client_info Where cm_cd=cm2_cd and Ltrim(Rtrim(cm_State)) not in (Select Ltrim(Rtrim(st_State)) from state_master)" + strClientWhere;
            dtGST = objUtility.OpenDataTable(strSQL, SqlCon, SqlTr);
            if (dtGST.Rows.Count > 0)
            {
                strMessage += "Improper State in address of following clients: \n";
                foreach (DataRow dr in dtGST.Rows)
                {
                    strMessage += dr["cm_cd"].ToString().Trim() + " (" + dr["cm_name"].ToString().Trim() + ") \n";
                }
            }

            strSQL = "select distinct cm_brboffcode, bm_branchname from Client_master, Branch_master Where cm_brboffcode = bm_branchcd " + strClientWhere + " and Rtrim(bm_pwd) = ''";
            dtGST = objUtility.OpenDataTable(strSQL, SqlCon, SqlTr);
            if (dtGST.Rows.Count > 0)
            {
                strMessage += "GST Number is not available for following Branches: \n";
                foreach (DataRow dr in dtGST.Rows)
                {
                    strMessage += dr["bm_branchname"].ToString().Trim() + " (" + dr["cm_brboffcode"].ToString().Trim() + ") \n";
                }
            }

            return strMessage;
        }
        public string fnChoose(int idx)
        {
            if (idx == 0)
            {
                return "GST_SGSTGLCD";
            }
            if (idx == 1)
            {
                return "GST_IGSTGLCD";
            }
            if (idx == 2)
            {
                return "GST_UGSTGLCD";
            }
            if (idx == 3)
            {
                return "GST_CGSTGLCD";
            }
            return null;
        }

        public string GetMaxGSTBillDt(string compCd, string date)
        {
            string strSQL = "";
            strSQL = " select isnull(MAX(GB_Date),'')";
            strSQL += " From GSTBills ";
            strSQL += " Where left(gb_companycode,1) = '" + compCd + "'";
            strSQL += " and gb_date between '" + objUtility.mfnGetAccstartdatefromdate(date, UtilityCommon.eNewDateformat.EDATABASE) + "'";
            strSQL += " and '" + objUtility.mfnGetAccenddatefromdate(date, UtilityCommon.eNewDateformat.EDATABASE) + "' and GB_Series='OTH' ";
            DataTable dt = objUtility.OpenDataTable(strSQL);
            if (dt.Rows.Count > 0)
            {
                return dt.Rows[0][0].ToString().Trim();
            }
            return "";
        }

        #endregion

        public dynamic Outstanding_Ageing(OutstandingAgeingRequestModel req, string loginAccess)
        {
            SqlTransaction ObjTrans;
            int NoOfColumn = req.Columns;
            int Period = req.Period;
            int Max = NoOfColumn + 1;
            string PeriodType = req.PeriodType;
            string strCmschedule = objUtility.GetSysParmSt("cmschedule", "");
            try
            {
                string strClientWhere = "";
                if (req.Filter.Client != null)
                {
                    if (req.Filter.Client.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Client.ToArray(), "##"));
                        strClientWhere += " or cm_cd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Branch != null)
                {
                    if (req.Filter.Branch.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Branch.ToArray(), "##"));
                        strClientWhere += " or cm_brboffcode in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Group != null)
                {
                    if (req.Filter.Group.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Group.ToArray(), "##"));
                        strClientWhere += " or cm_groupcd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Family != null)
                {
                    if (req.Filter.Family.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Family.ToArray(), "##"));
                        strClientWhere += " or cm_familycd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (strClientWhere.Length > 0)
                {
                    strClientWhere = " and (" + strClientWhere.Substring(3) + ") ";
                }
                string ConnectionString = objUtility.GetConnectionStr();
                using (SqlConnection Sqlcon = new SqlConnection(ConnectionString))
                {
                    Sqlcon.Open();
                    ObjTrans = Sqlcon.BeginTransaction();
                    SqlCommand cmd = Sqlcon.CreateCommand();
                    cmd.Transaction = ObjTrans;
                    SqlDataAdapter sqlDtAdap = new SqlDataAdapter();
                    SqlCommandBuilder sqlCmdBld = new SqlCommandBuilder(sqlDtAdap);
                    OutstandingAgeingTable(Sqlcon, ObjTrans);
                    if (req.PeriodType.ToUpper() == "M")
                    {
                        Period = Period * 30;
                    }
                    strsql = "insert into #tmpoutstanding select ld_clientcd , Sum(ld_amount),'', 0,'1' " +
                        " from ( select ld_clientcd , Sum(ld_amount) ld_amount  from Ledger, Client_master  " +
                        "where   ld_dpid like 'A_C' and left(ld_dpid,1) = 'A' and ld_clientcd = cm_cd  and ld_dt<='" + req.AsOnDate + "'" + strClientWhere + loginAccess +
                        " and cm_schedule in ( '" + strCmschedule + "') group by cm_cd, ld_clientcd  )  a  Group By ld_clientcd  " +
                        "having abs(sum(ld_amount))> 0";
                    objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                    strsql = "select ld_clientcd, cm_name, cm_brboffcode, bm_branchname, cm_groupcd, cm_familycd , bm_email , gr_email,  fm_email,  ( select abs(to_Balance) " +
                        "From #tmpoutstanding Where to_Clientcd = ld_clientcd and to_Balance > 0) sum1 , sum ( case ld_debitflag when 'C' then 0 else abs(ld_amount) end) as sum2 , " +
                        "case sign(datediff(d,ld_dt,'" + req.AsOnDate + "' )/" + Period + " - " + NoOfColumn + ")  when -1 then datediff(d,ld_dt,'" + req.AsOnDate + "')/" + Period + " else " + Max + " end  as listorder1, " +
                        "cm_brboffcode from (select ld_clientcd , ld_dt, ld_debitflag, ld_amount, ld_dpid from ledger,#tmpoutstanding " +
                        "where ld_clientcd=to_clientcd and to_Balance > 0 and ld_common not in ('ISFT','CONTRACL')  ) ldgr, Client_master, Branch_master , Group_master, Family_master  " +
                        "where   ld_dpid like 'A_C' and left(ld_dpid,1) = 'A' and ld_clientcd = cm_cd  and cm_brboffcode = bm_branchcd and cm_groupcd = gr_cd and cm_familycd = fm_cd  " +
                        "and ld_dt<='" + req.AsOnDate + "' and cm_schedule in ( '" + strCmschedule + "') group by ld_clientcd, cm_name, cm_groupcd, cm_familycd ,cm_brboffcode,bm_branchname, " +
                        "bm_email, gr_email,  fm_email, case sign(datediff(d,ld_dt,'" + req.AsOnDate + "' )/" + Period + " - " + NoOfColumn + ")  when -1 then datediff(d,ld_dt,'" + req.AsOnDate + "')/" + Period + " else " + Max + " end";
                    DataTable ds = objUtility.OpenDataTable(strsql, Sqlcon, ObjTrans);

                    if (ds.Rows.Count == 0)
                    {
                        return "Record Not Found";
                    }
                    string[] ListColumn = new string[NoOfColumn + 1];
                    int Prev = 0, nxt = 0;
                    int j = 1;
                    if (req.PeriodType.ToUpper() == "M")
                    {
                        for (int i = 0; i <= NoOfColumn; i++)
                        {
                            if (i != NoOfColumn)
                            {
                                Prev = i * req.Period;
                                nxt = j * req.Period;
                                ListColumn[i] = (Prev + " - " + nxt);
                                j++;
                            }
                            else
                            {
                                ListColumn[i] = (nxt + " Above");
                            }
                        }
                    }
                    if (req.PeriodType.ToUpper() == "D")
                    {
                        for (int i = 0; i <= NoOfColumn; i++)
                        {
                            if (i != NoOfColumn)
                            {
                                Prev = Period * i;
                                nxt = Period * j;
                                ListColumn[i] = (Prev + 1 + " - " + nxt);
                                j++;
                            }
                            else
                            {
                                ListColumn[i] = (nxt + 1 + " Above");
                            }
                        }
                    }

                    List<TempOutstandingAgeingRecords> ListTempRec = new List<TempOutstandingAgeingRecords>();
                    for (int i = 0; i < ds.Rows.Count; i++)
                    {
                        ListTempRec.Add(new TempOutstandingAgeingRecords()
                        {
                            ClientCode = ds.Rows[i]["ld_clientcd"].ToString().Trim(),
                            ClientName = ds.Rows[i]["cm_name"].ToString().Trim(),
                            BranchCode = ds.Rows[i]["cm_brboffcode"].ToString().Trim(),
                            BranchName = ds.Rows[i]["bm_branchname"].ToString().Trim(),
                            MonthList = Convert.ToDouble(ds.Rows[i]["listorder1"].ToString().Trim()),
                            Outstanding = Convert.ToDouble(ds.Rows[i]["sum1"].ToString().Trim()),
                            MonthWise = Convert.ToDouble(ds.Rows[i]["sum2"].ToString().Trim()),
                        });
                    }
                    List<OutstandingAgeingResponseModel> ListAORes = new List<OutstandingAgeingResponseModel>();
                    List<string> Codes = ListTempRec.Select(x => x.ClientCode).Distinct().ToList();

                    #region Before Code
                    foreach (var code in Codes)
                    {
                        OutstandingAgeingResponseModel ObjAORep = new OutstandingAgeingResponseModel();
                        var Codedetails = ListTempRec.Where(x => x.ClientCode == code).ToList();
                        double AddMonthWise = 0, PrevMonthAdd = 0, First = 0;
                        int i = 0;
                        j = 0;
                        int q = 0;
                        var dict = new Dictionary<string, object>();
                        for (int k = 0; k < NoOfColumn; k++)
                        {
                            if (k != i)
                            {
                                dict.Add(ListColumn[i], 0);
                                i++;
                                q++;
                                break;
                            }
                            var perCode = Codedetails[k];
                            ObjAORep.Code = perCode.ClientCode;
                            ObjAORep.Name = perCode.ClientName;
                            ObjAORep.BranchCode = perCode.BranchCode;
                            ObjAORep.BranchName = perCode.BranchName;
                            ObjAORep.Outstanding = perCode.Outstanding;
                            if (Codedetails.Count == 1 && perCode.MonthList == 0)
                            {
                                for (; i < NoOfColumn;)
                                {
                                    dict.Add(ListColumn[i], 0);
                                    i++;
                                }
                                if (perCode.Outstanding <= perCode.MonthWise)
                                {
                                    if (CheckNextOutstanding(Codedetails, i, NoOfColumn) == false && j == 0)
                                    {
                                        dict.Add(ListColumn[i], Math.Abs(perCode.Outstanding - AddMonthWise));
                                    }
                                    else
                                    {
                                        dict.Add(ListColumn[i], 0);
                                    }
                                }
                                i++;
                                break;
                            }
                            for (int l = 0; l <= NoOfColumn; l++)
                            {
                                if (perCode.MonthList == i)
                                {
                                    if (perCode.Outstanding > 0 && j == 0 && i == perCode.MonthList)
                                    {
                                        if (perCode.Outstanding > perCode.MonthWise)
                                        {
                                            AddMonthWise += perCode.MonthWise;
                                            if (AddMonthWise < perCode.Outstanding)
                                            {
                                                dict.Add(ListColumn[i], perCode.MonthWise);
                                                i++;
                                                First++;
                                            }
                                            else
                                            {
                                                dict.Add(ListColumn[i], Math.Abs(perCode.Outstanding - PrevMonthAdd));
                                                j++;
                                                i++;
                                            }
                                        }
                                        if (First == 0)
                                        {
                                            dict.Add(ListColumn[i], Math.Abs(perCode.Outstanding - PrevMonthAdd));
                                            j++;
                                            i++;
                                        }
                                        else if (perCode.Outstanding <= perCode.MonthWise)
                                        {
                                            if (CheckNextOutstanding(Codedetails, i, NoOfColumn) == false)
                                            {
                                                dict.Add(ListColumn[i], Math.Abs(perCode.Outstanding - AddMonthWise));
                                                j++;
                                                i++;
                                            }
                                            else
                                            {
                                                dict.Add(ListColumn[i], 0);
                                                i++;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        dict.Add(ListColumn[i], 0);
                                        i++;
                                    }

                                    PrevMonthAdd = AddMonthWise;
                                    break;
                                }
                                else if (perCode.MonthList == Max)
                                {
                                    break;
                                }
                                else
                                {
                                    dict.Add(ListColumn[i], 0);
                                    i++;
                                }
                            }
                            if (perCode.MonthList == Max)
                            {
                                for (; i < NoOfColumn;)
                                {
                                    dict.Add(ListColumn[i], 0);
                                    i++;
                                }
                                if (perCode.Outstanding <= perCode.MonthWise && perCode.MonthList == Max)
                                {
                                    if (CheckNextOutstanding(Codedetails, i, NoOfColumn) == false && j == 0)
                                    {
                                        dict.Add(ListColumn[i], Math.Abs(perCode.Outstanding - AddMonthWise));
                                    }
                                    else
                                    {
                                        dict.Add(ListColumn[i], 0);
                                    }
                                }
                                i++;
                                break;
                            }
                            if (Codedetails.Count == i)
                            {
                                for (; i < NoOfColumn;)
                                {
                                    dict.Add(ListColumn[i], 0);
                                    i++;
                                }
                            }
                        }
                        if (i == NoOfColumn && q == 0)
                        {
                            if (i != Codedetails.Count)
                            {
                                var Above = Codedetails[i];
                                if (Above.Outstanding <= Above.MonthWise && Above.MonthList == Max)
                                {
                                    if (CheckNextOutstanding(Codedetails, i, NoOfColumn) == false && j == 0)
                                    {
                                        dict.Add(ListColumn[i], Math.Abs(Above.Outstanding - AddMonthWise));
                                    }
                                    else
                                    {
                                        dict.Add(ListColumn[i], 0);
                                    }
                                }
                            }
                            else
                            {
                                dict.Add(ListColumn[i], 0);
                            }
                        }

                        ObjAORep.Data = dict;
                        ListAORes.Add(ObjAORep);
                    }
                    #endregion
                    return ListAORes;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region Outstanding Ageing Helpers
        public void OutstandingAgeingTable(SqlConnection Sqlcon, SqlTransaction ObjTrans)
        {
            strsql = "Create Table #tmpoutstanding (to_Clientcd VarChar(8), to_Balance money, to_MarginCd char (8), to_margin money, to_flag char (1))";
            objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);
        }

        public bool CheckNextOutstanding(List<TempOutstandingAgeingRecords> CodeDetails, int idx, int NoOfColumn)
        {
            int count = 0;
            for (int i = idx + 1; i < CodeDetails.Count; i++)
            {
                if (CodeDetails[i].Outstanding < CodeDetails[i].MonthWise && CodeDetails[i].MonthList <= NoOfColumn)
                {
                    count++;
                }
            }
            if (count >= 1)
            {
                return true;
            }
            return false;
        }
        #endregion

        public dynamic CommisionReport(string fromDate, string toDate, Filter filter, string ReportType, string loginAccess)
        {
            try
            {
                if (ReportType.Trim() != "S")
                {
                    return null;
                }

                using (var con = new SqlConnection(objUtility.GetConnectionStr()))
                {
                    con.Open();
                    string strSql = GetCommissionfnQuery(fromDate, toDate, ReportType, filter, loginAccess, con);
                    DataTable data = objUtility.OpenDataTableTmp(strSql, con);

                    strSql = " select distinct rs_ExChange,CES_CompanyCd, CES_Exchange, CES_Segment ";
                    strSql = strSql + " from #TmpSharing,CompanyExchangeSegments ";
                    strSql = strSql + " where ces_cd=rs_ExChange ";
                    strSql = strSql + " order by  CES_CompanyCd, CES_Exchange, CES_Segment";
                    DataTable dtExchSeg = objUtility.OpenDataTable(strSql, con);

                    List<string> lstExchSeg = new List<string>();
                    var dictExchSeg = new Dictionary<string, string>();
                    foreach (DataRow dr in dtExchSeg.Rows)
                    {
                        lstExchSeg.Add(dr["rs_ExChange"].ToString().Trim());
                        dictExchSeg.Add(dr["rs_ExChange"].ToString().Trim(), dr["CES_Exchange"].ToString().Trim() + "_" + dr["CES_Segment"].ToString().Trim());
                    }

                    List<TempCommissionReportRecords> tempCompanyCommisionReportList = new List<TempCommissionReportRecords>();

                    for (int i = 0; i < data.Rows.Count; i++)
                    {
                        double totalBrokerage = 0;
                        double totalShare1 = 0;
                        double totalShare2 = 0;

                        Dictionary<string, object> dictNew = new Dictionary<string, object>();
                        dictNew.Add("Date", data.Rows[i]["rs_dt"].ToString().Trim());
                        foreach (var dictItem in dictExchSeg)
                        {
                            SegmentWiseDetail segmentWise = new SegmentWiseDetail();
                            segmentWise.Brokerage = data.Rows[i][dictItem.Key + "BROKERAGE"].ToDouble();
                            segmentWise.Remmisier1_Share = data.Rows[i][dictItem.Key + "Share"].ToDouble();
                            segmentWise.Remmisier2_Share = data.Rows[i][dictItem.Key + "remissier2share"].ToDouble();
                            segmentWise.Net = data.Rows[i][dictItem.Key + "BROKERAGE"].ToDouble() - data.Rows[i][dictItem.Key + "Share"].ToDouble() - data.Rows[i][dictItem.Key + "remissier2share"].ToDouble();
                            dictNew.Add(dictItem.Value, segmentWise);
                            totalBrokerage += data.Rows[i][dictItem.Key + "BROKERAGE"].ToDouble();
                            totalShare1 += data.Rows[i][dictItem.Key + "Share"].ToDouble();
                            totalShare2 += data.Rows[i][dictItem.Key + "remissier2share"].ToDouble();
                        }

                        SegmentWiseDetail segment = new SegmentWiseDetail
                        {
                            Brokerage = totalBrokerage,
                            Remmisier1_Share = totalShare1,
                            Remmisier2_Share = totalShare2,
                            Net = totalBrokerage - totalShare1 - totalShare2
                        };
                        dictNew.Add("All", segment);

                        tempCompanyCommisionReportList.Add(new TempCommissionReportRecords()
                        {
                            ClientName = data.Rows[i]["cm_name"].ToString().Trim(),
                            ClientCode = data.Rows[i]["rs_Clientcd"].ToString().Trim(),
                            Date = data.Rows[i]["rs_dt"].ToString().Trim(),
                            BranchCode = data.Rows[i]["cm_brboffcode"].ToString().Trim(),
                            BranchName = data.Rows[i]["bm_branchname"].ToString().Trim(),
                            GroupCode = data.Rows[i]["cm_groupcd"].ToString().Trim(),
                            GroupName = data.Rows[i]["gr_desc"].ToString().Trim(),
                            FamilyCode = data.Rows[i]["cm_familycd"].ToString().Trim(),
                            FamilyName = data.Rows[i]["fm_desc"].ToString().Trim(),
                            Data = dictNew
                        });
                    }

                    List<CommissionReportResponseModel> companyCommisionReportList = new List<CommissionReportResponseModel>();

                    var tempClientCodes = tempCompanyCommisionReportList.Select(x => x.ClientCode).Distinct();

                    foreach (var clientCode in tempClientCodes)
                    {
                        var tempClientWiseCommisionReport = tempCompanyCommisionReportList.Where(x => x.ClientCode.Trim() == clientCode.Trim());
                        List<dynamic> lstData = new List<dynamic>();

                        foreach (var clientWiseCommision in tempClientWiseCommisionReport)
                        {
                            lstData.Add(clientWiseCommision.Data);
                        }

                        companyCommisionReportList.Add(new CommissionReportResponseModel
                        {
                            ClientCode = tempClientWiseCommisionReport?.FirstOrDefault().ClientCode ?? "",
                            ClientName = tempClientWiseCommisionReport?.FirstOrDefault().ClientName ?? "",
                            BranchCode = tempClientWiseCommisionReport?.FirstOrDefault().BranchCode ?? "",
                            BranchName = tempClientWiseCommisionReport?.FirstOrDefault().BranchName ?? "",
                            GroupCode = tempClientWiseCommisionReport?.FirstOrDefault().GroupCode ?? "",
                            GroupName = tempClientWiseCommisionReport?.FirstOrDefault().GroupName ?? "",
                            FamilyCode = tempClientWiseCommisionReport?.FirstOrDefault().FamilyCode ?? "",
                            FamilyName = tempClientWiseCommisionReport?.FirstOrDefault().FamilyName ?? "",
                            Data = lstData
                        });
                    }

                    return companyCommisionReportList;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region commision report helper method

        private void prCreateCommisiontmptable(SqlConnection con)
        {
            objUtility.ExecuteSQLTmp("if OBJECT_ID('tempdb..#TmpSharing') is not null Drop Table #TmpSharing", con);

            string strSql = "CREATE TABLE #TmpSharing ( ";
            strSql += "[rs_clientcd] [char] (8) Not NULL , ";
            strSql += "[rs_dt] [char] (8) Not NULL , ";
            strSql += "[rs_qty] Numeric Not NULL , ";
            strSql += "[rs_brokeragetype] [char] (3) Not NULL , ";
            strSql += "[rs_brokerage] [money] Not NULL , ";
            strSql += "[rs_subbroker] [char] (8) Not NULL , ";
            strSql += "[rs_subbrokerName] [char] (50) Not NULL , ";
            strSql += "[rs_hisshare] [money] Not NULL ,";
            strSql += "[rs_remissier2] [char] (8) Not NULL , ";
            strSql += "[rs_remissier2Name] [char] (50) Not NULL ,  ";
            strSql += "[rs_remissier2share] [money] Not NULL ,";
            strSql += "[rs_ExChange] [Char] (3) not null,";
            strSql += "[rs_Yearmonth] [char](6) Not Null,";
            strSql += "[rs_datedisplay] [char](20) Not Null )";
            objUtility.ExecuteSQLTmp(strSql, con);

            strSql = " Create index #idx_TmpSharing_clientcd_ExChange on #TmpSharing (rs_clientcd,rs_ExChange)";
            objUtility.ExecuteSQLTmp(strSql, con);
        }

        private string GetCommissionfnQuery(string fromDate, string toDate, string ReportType, Filter filter, string loginAccess, SqlConnection con)
        {
            string strFields = string.Empty;
            string strTemp = string.Empty;
            string strGrpBy = string.Empty;
            string strOrderBy = string.Empty;
            string strwhere = string.Empty;
            string strBranchType = string.Empty;
            string strAdvanceFilter = string.Empty;
            string strGroupby = string.Empty;
            string strSql = string.Empty;

            DataTable rsExchange = new DataTable();

            if (ReportType.Trim() == "S")
            {
                strFields = "rs_clientcd,cm_name";
                strOrderBy = " Order by " + strFields;

                if (strFields != "")
                    strGroupby = " group by " + strFields;

                strSql = "";
                if (strFields != "")
                    strGrpBy = " group by " + strFields;
                strwhere = " rs_dt between '" + fromDate + "' and '" + toDate + "'";
                string strClientWhere = "";
                if (filter.Client != null)
                {
                    if (filter.Client.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(filter.Client.ToArray(), "##"));
                        strClientWhere += " or cm_cd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (filter.Branch != null)
                {
                    if (filter.Branch.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(filter.Branch.ToArray(), "##"));
                        strClientWhere += " or cm_brboffcode in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (filter.Group != null)
                {
                    if (filter.Group.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(filter.Group.ToArray(), "##"));
                        strClientWhere += " or cm_groupcd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (filter.Family != null)
                {
                    if (filter.Family.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(filter.Family.ToArray(), "##"));
                        strClientWhere += " or cm_familycd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (strClientWhere.Length > 0)
                {
                    strClientWhere = " and (" + strClientWhere.Substring(3) + ") ";
                }
                strwhere += strClientWhere;

                strBranchType = "";

                prCreateCommisiontmptable(con);

                strSql = "insert into #TmpSharing  ";
                strSql += " select rs_clientcd,rs_dt,rs_qty,rs_brokeragetype, rs_brokerage,rs_subbroker,'',rs_hisshare,";
                strSql += " rs_remissier2, '',rs_remissier2share,rs_companycode + left(rs_stlmnt,1)+'C','','' ";
                strSql += " From vwCrevenue_sharing,client_master a,branch_master b Where  rs_clientcd = a.cm_cd and a.cm_brboffcode = b.bm_branchcd and " + strwhere.Replace("cm_", "a.cm_") + strBranchType + strAdvanceFilter + loginAccess.Replace("cm_", "a.cm_");
                objUtility.ExecuteSQLTmp(strSql, con);

                strSql = "insert into #TmpSharing";
                strSql += " select rs_clientcd,rs_dt,rs_qty,rs_brokeragetype, rs_brokerage*sm_multiplier,rs_subbroker,'',rs_hisshare,";
                strSql += " rs_remissier2, '',rs_remissier2share  ,rs_companycode+rs_exchange+rs_segment ,'','' ";
                strSql += " From vwFrevenue_sharing ,client_master a,branch_master b,series_master Where rs_exchange = sm_exchange and rs_segment = sm_segment and rs_seriesid = sm_seriesid and rs_clientcd = a.cm_cd and a.cm_brboffcode = b.bm_branchcd and " + strwhere.Replace("cm_", "a.cm_") + strBranchType + strAdvanceFilter + loginAccess.Replace("cm_", "a.cm_");
                objUtility.ExecuteSQLTmp(strSql, con);

                strSql = "Update #TmpSharing Set rs_subbrokerName = left(Rm_Name,50) From SubBrokers Where Rm_Cd = rs_subbroker ";
                objUtility.ExecuteSQLTmp(strSql, con);

                strSql = "Update #TmpSharing Set rs_remissier2Name = left(Rm_Name,50) From SubBrokers Where Rm_Cd = rs_remissier2 ";
                objUtility.ExecuteSQLTmp(strSql, con);

                strFields = "rs_clientcd,rs_dt,cm_name,cm_groupcd,gr_desc,cm_familycd,fm_desc,cm_brboffcode,bm_branchname";
                strGrpBy = " group by " + strFields;

                strSql = "select " + strFields;
                strTemp = " select distinct rs_ExChange,CES_CompanyCd, CES_Exchange, CES_Segment ";
                strTemp = strTemp + " from #TmpSharing,CompanyExchangeSegments ";
                strTemp = strTemp + " where ces_cd=rs_ExChange ";
                strTemp = strTemp + " order by  CES_CompanyCd, CES_Exchange, CES_Segment";
                rsExchange = objUtility.OpenDataTableTmp(strTemp, con);
                if (rsExchange.Rows.Count > 0)
                {
                    int intTotalColumn = rsExchange.Rows.Count;
                    foreach (DataRow dtR in rsExchange.Rows)
                    {
                        strSql += " , Round(sum(case rs_exchange when '" + dtR["rs_exchange"].ToString().Trim() + "' then rs_brokerage else 0 end),2) as " + dtR["rs_exchange"].ToString().Trim() + "BROKERAGE";
                        strSql += " , Round(sum(case rs_exchange when '" + dtR["rs_exchange"].ToString().Trim() + "' then rs_hisshare else 0 end),2) as " + dtR["rs_exchange"].ToString().Trim() + "Share";
                        strSql += " , Round(sum(case rs_exchange when '" + dtR["rs_exchange"].ToString().Trim() + "' then rs_remissier2share else 0 end),2) as " + dtR["rs_exchange"].ToString().Trim() + "remissier2share";
                    }
                }
                strSql = strSql.Replace("select  ,", "select ");
                strSql += " from #TmpSharing ,Client_master,branch_master,group_master,family_master";
                strSql += " Where cm_brboffcode = bm_branchcd And rs_clientcd = cm_cd And cm_groupcd = gr_cd And cm_familycd = fm_cd" + strGrpBy + strOrderBy;
            }

            return strSql;
        }


        #endregion

        public dynamic Transaction_Detail(TransactionDetailModel req, string loginAccess)
        {
            try
            {
                string strClientWhere = "";
                if (req.Filter.Client != null)
                {
                    if (req.Filter.Client.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Client.ToArray(), "##"));
                        strClientWhere += " or cm_cd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Branch != null)
                {
                    if (req.Filter.Branch.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Branch.ToArray(), "##"));
                        strClientWhere += " or cm_brboffcode in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Group != null)
                {
                    if (req.Filter.Group.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Group.ToArray(), "##"));
                        strClientWhere += " or cm_groupcd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Family != null)
                {
                    if (req.Filter.Family.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Family.ToArray(), "##"));
                        strClientWhere += " or cm_familycd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (strClientWhere.Length > 0)
                {
                    strClientWhere = " and (" + strClientWhere.Substring(3) + ") ";
                }

                var query = objUtility.Transaction_Detail_Query(strClientWhere, req.Exch, req.Seg, req.Type, req.FromDate, req.ToDate, req.ScripCode, loginAccess, "O");
                var ds = objUtility.OpenDataSet(query);
                if (ds?.Tables?.Count > 0 && ds?.Tables[0]?.Rows?.Count > 0)
                {
                    var json = ds.Tables[0];
                    return json;
                }
                return null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic BrokerageSchemChange(List<BrokerageSchemeChange> listData, string userID)
        {
            try
            {
                SqlTransaction objTrans;
                string oldVal = "", mkrOldId = "", mkrOldDate = "";
                var db = new DataContext();
                using (SqlConnection sqlCon = new SqlConnection((db.Database.GetDbConnection()).ConnectionString))
                {
                    if (sqlCon.State == ConnectionState.Closed)
                        sqlCon.Open();
                    objTrans = sqlCon.BeginTransaction();
                    foreach (var data in listData)
                    {
                        string strSql = "", table = "", cDate = "", cTime = "";
                        data.clientCode = objUtility.mfnReplaceForSQLInjection(data.clientCode);
                        data.brkgScheme = objUtility.mfnReplaceForSQLInjection(data.brkgScheme);
                        data.exchSeg = objUtility.mfnReplaceForSQLInjection(data.exchSeg);
                        if (data.exchSeg.Substring(2, 1) == "C")
                        {
                            table = " Brokerages";
                            strSql = " select  br_scheme Col0,ltrim(str(max(case when br_type= 'SD1' then  br_min else 0 end),4,2)) Col1, ";
                            strSql += " ltrim(str(max(case when br_type= 'SD1' then  br_percent else 0 end),6,4)) Col2,";
                            strSql += " ltrim(str(max(case when br_type= 'DLV' then  br_min else 0 end),4,2)) Col3,";
                            strSql += " ltrim(str(max(case when br_type= 'DLV' then  br_percent else 0 end),6,4)) Col4,";
                            strSql += " ltrim(str(max(case when br_type= 'SD2' then  br_min else 0 end),4,2))  Col5,";
                            strSql += " ltrim(str(max(case when br_type= 'SD2' then  br_percent else 0 end),6,4)) Col6";
                            strSql += " from " + table + " Where br_Companycode = '" + data.exchSeg + "' and br_settgroup = 'N' and br_scheme = '" + data.brkgScheme + "' group by br_scheme";
                        }
                        else if (data.exchSeg.Substring(2, 1) == "M")
                        {
                            table = " MFbrokerages";
                            strSql = " select  mbr_scheme Col0,ltrim(str(max(case when mbr_type= 'Buy' then  mbr_percent else 0 end),4,2)) Col1 ,";
                            strSql += " ltrim(str(max(case when mbr_type= 'Buy' then  mbr_min else 0 end),6,4))  Col2,";
                            strSql += " ltrim(str(max(case when mbr_type= 'Buy' then  mbr_max else 0 end),4,2)) Col3,";
                            strSql += " ltrim(str(max(case when mbr_type= 'Sell' then mbr_percent else 0 end),6,4)) Col4,";
                            strSql += " ltrim(str(max(case when mbr_type= 'Sell' then mbr_min else 0 end),4,2)) Col5,";
                            strSql += " ltrim(str(max(case when mbr_type= 'Sell' then mbr_max else 0 end),6,4)) Col6";
                            strSql += " from " + table + " Where mbr_Companycode = '" + data.exchSeg + "' and mbr_settgroup = 'M' and mbr_scheme = '" + data.brkgScheme + "' group by mbr_scheme";
                        }
                        else
                        {
                            table = " FBrokerages,fbrokerage_master";
                            strSql = " select  br_scheme Col0,ltrim(str(max(br_percent1),6,4)) Col1 ,";
                            strSql += " case max(br_min1) when 0 then '' else ltrim(str(max(br_min1),6,4)) end  Col2,";
                            strSql += " case max(br_max1) when 0 then '' else ltrim(str(max(br_max1),6,4)) end Col3,";
                            strSql += " case fb_sdremove when 'N' then 'No' else ltrim(str(max(br_percent2),6,4)) end Col4,";
                            strSql += " case fb_sdremove when 'N' then 'Sqr' else case max(br_min2) when 0 then '' else ltrim(str(max(br_min2),6,4)) end end Col5,";
                            strSql += " case fb_sdremove when 'N' then 'off' else case max(br_max1) when 0 then '' else ltrim(str(max(br_max1),6,4)) end end Col6,";
                            strSql += " fb_minpercontractOpt Col7";
                            strSql += " from " + table + " Where br_Companycode = '" + data.exchSeg + "'and br_companycode=fb_companycode and br_scheme=fb_scheme   and br_scheme = '" + data.brkgScheme + "' group by br_scheme, fb_sdremove,fb_minpercontractOpt";
                        }
                        var dtData = objUtility.OpenDataTable(strSql);
                        if (dtData.Rows.Count < 1)
                        {
                            throw new Exception(data.brkgScheme + ": Brokerage Scheme Not Found");
                        }

                        string newstr = " Select Distinct  ce_brkscheme,mkrdt,mkrid ,Left(Replace(CONVERT(nvarchar, getdate(),21),'-',''),8) as cDate, substring(Replace(CONVERT(nvarchar, getdate(),21),'-',''),10,8) as cTime  ";
                        newstr += "   From Client_details a inner Join Client_master b ON a.ce_clientcd=b.cm_cd  Where a.ce_clientcd='" + data.clientCode + "' and ce_companycode='" + data.exchSeg + "' order by mkrdt desc";
                        var dt = objUtility.OpenDataTable(newstr);
                        if (dt.Rows.Count < 1)
                        {
                            throw new Exception(data.clientCode + ": Client Not Found");
                        }
                        if (dt.Rows.Count > 0)
                        {
                            oldVal = dt.Rows[0]["ce_brkscheme"].ToString();
                            mkrOldId = dt.Rows[0]["mkrid"].ToString();
                            mkrOldDate = dt.Rows[0]["mkrdt"].ToString();
                            cDate = dt.Rows[0]["cDate"].ToString();
                            cTime = dt.Rows[0]["cTime"].ToString();
                        }
                        try
                        {
                            strSql = " Update Client_Details set ce_brkScheme = '" + data.brkgScheme + "' ";
                            strSql += " Where ce_companycode='" + data.exchSeg + "'   and  ce_clientcd='" + data.clientCode + "'";
                            objUtility.ExecuteSQL(strSql);

                            strSql = " Insert into Common_audit(ca_master, ca_table,ca_dpid,ca_keyfield,ca_keyname,ca_keyvalue,ca_field,";
                            strSql += " ca_fielddescription,ca_oldvalue, ca_newvalue,ca_computername,mkrid,mkrdt,mkrtm, mkridold,mkrdtold,mkrtmold)";
                            strSql += " Values ('Client Master','Client_Details', '" + data.exchSeg + "', 'ce_clientcd','Client Code',";
                            strSql += " '" + data.clientCode + "','ce_brkScheme','" + data.exchSeg + "-Brokerage Scheme', '" + oldVal + "', ";
                            strSql += " '" + data.brkgScheme + "', '" + System.Environment.MachineName + "', '" + userID + "','" + cDate + "','" + cTime + "',";
                            strSql += " '" + mkrOldId + "','" + mkrOldDate + "','00:00:00')";
                            objUtility.ExecuteSQL(strSql);
                        }
                        catch (Exception e)
                        {
                            objTrans.Rollback();
                            throw new Exception("Error on client: " + data.clientCode + " /Exception:=" + e.Message.ToString());
                        }
                    }
                    objTrans.Commit();
                }
                return "Data updated successfully!!";
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic DeliveryStatement(DeliveryStatementReq req, string compCode, string loginAccess)
        {
            try
            {
                DataTable dt = new DataTable();
                List<DeliveryStatementModel> delStatement = new List<DeliveryStatementModel>();
                List<TempDeliveryStatementModel> tempDelStatementData = new List<TempDeliveryStatementModel>();
                bool blnDST2T = false;
                bool blnDSInterOP = false;
                string strDSStlmntWhere = "";
                string strDSSort = "";
                string strDSViewname = "vwcDeliveryList" + (objUtility.mfnDistProcess(req.Settlement, compCode) == "Y" ? "Daily" : "");
                //string gCompanyCode = "A";
                string strSql = "";
                string strClientWhere = "";
                if (req.Filter.Client != null)
                {
                    if (req.Filter.Client.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Client.ToArray(), "##"));
                        strClientWhere += " or cm_cd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Branch != null)
                {
                    if (req.Filter.Branch.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Branch.ToArray(), "##"));
                        strClientWhere += " or cm_brboffcode in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Group != null)
                {
                    if (req.Filter.Group.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Group.ToArray(), "##"));
                        strClientWhere += " or cm_groupcd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Family != null)
                {
                    if (req.Filter.Family.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Family.ToArray(), "##"));
                        strClientWhere += " or cm_familycd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (strClientWhere.Length > 0)
                {
                    strClientWhere = " and (" + strClientWhere.Substring(3) + ") ";
                }

                string strClgHSG = objUtility.fnGetClearingHouse(req.TradeDate, "C", compCode);
                string strSettlement = "";

                strSql = "select se_stlmnt from Settlements ";
                strSql += " Where se_stdt = '" + req.TradeDate + "' and lefT(se_stlmnt,2) not in " + (strClgHSG == "N" ? "('BW', 'BC', 'BR', 'BQ', 'BU')" : "('NN', 'NZ', 'NA', 'NQ', 'NU') ");
                if (!string.IsNullOrWhiteSpace(req.Settlement))
                {
                    strSql += " and se_stlmnt='" + req.Settlement + "' ";
                }
                strSql += " Order by case When left(se_stlmnt,2) in ('BW','NN','BQ','NQ') Then 0 When left(se_stlmnt,2) in ('BC','NZ','BU','NU')  Then 1 When left(se_stlmnt,2) in ('BR','NA') Then 2 else 1 end";
                DataTable dtSett = objUtility.OpenDataTable(strSql);

                foreach (DataRow dr in dtSett.Rows)
                {
                    strSettlement = dr["se_stlmnt"].ToString().Trim();

                    strSql = " select sy_maptype From SEttlements With (nolock) , Settlement_type With (nolock) ";
                    strSql += " Where se_exchange = sy_exchange and se_type = sy_type ";
                    strSql += "  and se_stlmnt = '" + strSettlement + "' ";
                    dt = objUtility.OpenDataTable(strSql);
                    if (dt.Rows.Count > 0)
                    {
                        if (dt.Rows[0]["sy_maptype"].ToString().Trim() == "C")
                        {
                            blnDST2T = true;
                        }
                    }
                    blnDSInterOP = objUtility.fnCheckInterOperability(req.TradeDate, "C") == "TRUE";
                    strDSStlmntWhere = " and td_stlmnt ='" + strSettlement + "' ";
                    strDSSort = "td_clientcd, ss_name";
                    if (blnDSInterOP)
                    {
                        var sett = objUtility.fnGetInterOpStlmnts(strSettlement, req.TradeDate, false);
                        var arrSett = sett.Split(',');
                        var strSett = string.Join("','", arrSett);
                        if (sett.Trim() != "")
                        {
                            strDSStlmntWhere = " and td_stlmnt in ('" + strSett.Trim() + "')";
                        }
                    }

                    strSql = "select a.td_stlmnt, a.td_scripcd, a.td_clientcd, a.cm_groupcd, a.gr_desc, a.cm_name, a.ss_name, a.nQty, a.sr_nodelyn, a.im_isin, a.cm_brboffcode, a.bm_branchname, a.cm_poa ,b.cm_familycd, fm_desc";
                    strSql += " from ( ";
                    if (blnDST2T)
                    {
                        strSql += " select td_companycode, td_stlmnt,td_scripcd,td_clientcd,cm_name,cm_email,cm_poa,ss_name,nQty,sr_nodelyn,ss_demat,im_isin,cm_groupcd,gr_desc,cm_brboffcode,bm_branchname ";
                        strSql += " From " + strDSViewname + "";
                        strSql += " where td_companycode='" + compCode + "'" + strDSStlmntWhere;
                    }
                    else
                    {
                        if (blnDSInterOP)
                        {
                            strSql += " select td_companycode,'" + strSettlement + "' td_stlmnt,td_scripcd,td_clientcd,cm_name,cm_email,cm_poa,ss_name,Sum(nQty) nQty,sr_nodelyn,ss_demat,im_isin,cm_groupcd,gr_desc,cm_brboffcode,bm_branchname ";
                            strSql += " From " + strDSViewname + " left join Client_info on td_clientcd = cm2_cd ";
                            strSql += " where td_companycode='" + compCode + "' and isNull(cm_constitution,0) <> 18 " + strDSStlmntWhere;
                            strSql += " Group by td_companycode,td_scripcd,td_clientcd,cm_name,cm_email,cm_poa,ss_name,sr_nodelyn,ss_demat,im_isin,cm_groupcd,gr_desc,cm_brboffcode,bm_branchname";
                            strSql += " Having Sum(nQty) <> 0 ";
                            strSql += " union ";
                            strSql += " select td_companycode,'" + strSettlement + "' td_stlmnt,td_scripcd,td_clientcd,cm_name,cm_email,cm_poa,ss_name,nQty,sr_nodelyn,ss_demat,im_isin,cm_groupcd,gr_desc,cm_brboffcode,bm_branchname ";
                            strSql += " From " + strDSViewname + " left join Client_info on td_clientcd = cm2_cd ";
                            strSql += " where td_companycode='" + compCode + "' and isNull(cm_constitution,0) = 18 " + strDSStlmntWhere;
                        }
                        else
                        {
                            strSql += " select td_companycode,'" + strSettlement + "' td_stlmnt,td_scripcd,td_clientcd,cm_name,cm_email,cm_poa,ss_name,Sum(nQty) nQty,sr_nodelyn,ss_demat,im_isin,cm_groupcd,gr_desc,cm_brboffcode,bm_branchname ";
                            strSql += " From " + strDSViewname + "";
                            strSql += " where td_companycode='" + compCode + "'" + strDSStlmntWhere;
                            strSql += " Group by td_companycode,td_scripcd,td_clientcd,cm_name,cm_email,cm_poa,ss_name,sr_nodelyn,ss_demat,im_isin,cm_groupcd,gr_desc,cm_brboffcode,bm_branchname";
                            strSql += " Having Sum(nQty) <> 0 ";
                        }
                    }
                    strSql += " ) a ";
                    strSql += ", client_master b ,family_master";
                    strSql += " where b.cm_cd = a.td_clientcd " + "and b.cm_familycd=fm_cd " + strClientWhere.Replace("cm_", "b.cm_") + loginAccess.Replace("cm_", "b.cm_");
                    if (!string.IsNullOrWhiteSpace(req.Security))
                    {
                        strSql += " and a.td_scripcd= '" + req.Security.Trim() + "' ";
                    }
                    strSql += " Order By " + strDSSort;
                    dt = objUtility.OpenDataTable(strSql);

                    foreach (DataRow drTmp in dt.Rows)
                    {
                        tempDelStatementData.Add(new TempDeliveryStatementModel
                        {
                            ClientCode = drTmp["td_clientcd"].ToString().Trim(),
                            ClientName = drTmp["cm_name"].ToString().Trim(),
                            BranchCode = drTmp["cm_brboffcode"].ToString().Trim(),
                            BranchName = drTmp["bm_branchname"].ToString().Trim(),
                            GroupCode = drTmp["cm_groupcd"].ToString().Trim(),
                            GroupName = drTmp["gr_desc"].ToString().Trim(),
                            FamilyCode = drTmp["cm_familycd"].ToString().Trim(),
                            FamilyName = drTmp["fm_desc"].ToString().Trim(),
                            Settlement = drTmp["td_stlmnt"].ToString().Trim(),
                            ReceivedByUs = Convert.ToDouble(drTmp["nQty"]) < 0 ? Math.Abs(Convert.ToDouble(drTmp["nQty"])) : 0,
                            ScripCode = drTmp["td_scripcd"].ToString().Trim(),
                            ScripName = drTmp["ss_name"].ToString().Trim(),
                            ISINCode = drTmp["im_isin"].ToString().Trim(),
                            GivenByUs = Convert.ToDouble(drTmp["nQty"]) > 0 ? Math.Abs(Convert.ToDouble(drTmp["nQty"])) : 0
                        });
                    }
                }

                List<string> lstSett = tempDelStatementData.Select(x => x.Settlement).Distinct().ToList();

                foreach (string strSett in lstSett)
                {
                    List<DeliverySettData> data = new List<DeliverySettData>();
                    var lstSettData = tempDelStatementData.Where(x => x.Settlement == strSett).ToList();
                    var lstClient = lstSettData.Select(x => x.ClientCode).Distinct().ToList();
                    foreach (string strClient in lstClient)
                    {
                        var clientData = lstSettData.Where(x => x.ClientCode == strClient.Trim()).ToList();
                        data.Add(new DeliverySettData
                        {
                            ClientCode = strClient,
                            ClientName = clientData.FirstOrDefault().ClientName.Trim(),
                            BranchCode = clientData.FirstOrDefault().BranchCode.Trim(),
                            BranchName = clientData.FirstOrDefault().BranchName.Trim(),
                            GroupCode = clientData.FirstOrDefault().GroupCode.Trim(),
                            GroupName = clientData.FirstOrDefault().GroupName.Trim(),
                            FamilyCode = clientData.FirstOrDefault().FamilyCode.Trim(),
                            FamilyName = clientData.FirstOrDefault().FamilyName.Trim(),
                            Data = clientData.Select(row => new DeliveryStatementData
                            {
                                GivenByUs = row.GivenByUs,
                                ISINCode = row.ISINCode,
                                ScripCode = row.ScripCode,
                                ScripName = row.ScripName,
                                ReceivedByUs = row.ReceivedByUs
                            }).ToList()
                        });
                    }

                    DeliveryStatementModel delStatementModel = new DeliveryStatementModel
                    {
                        Settlement = strSett,
                        Data = data
                    };
                    delStatement.Add(delStatementModel);
                }
                return delStatement;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic CompanyPerformance(CompanyPerformanceRequest req)
        {
            try
            {
                List<CompanyPerformanceReportResponse> companyPerformanceReportResponses = new List<CompanyPerformanceReportResponse>();
                DataTable dt = new DataTable();
                string strClientWhere = "";
                bool ChkCashBreakup = req.CashDelNonDelBreakup;
                bool ChkCashTurnover = true;
                bool chkCashBrokerage = true;
                bool ChkFoTurnover = true;
                bool chkFOBreakup = req.FOBreakup;
                bool ChkFOBrokerage = true;
                bool BlnCommex = false;
                bool blnTplusCommex = objUtility.mfnGetSysSplFeatureCommodity("TCM");

                if (req.ExchSeg.Count == 0 || req.ExchSeg.All(y => y == ""))
                {
                    strsql = "select distinct CES_Name Name , CES_Segment Segment , left(CES_Cd,1) + 'N' + Right(Rtrim(CES_Cd),1) Company,";
                    strsql += " Case Right(Rtrim(CES_Cd),1) when 'X' Then 4 when 'K' Then 3 When 'C' Then 1 When 'F' Then 2  end Segment1 ";
                    strsql += " from CompanyExchangeSegments ";
                    strsql += " Where Right(Rtrim(CES_Cd),1) Not in ('M','X') ";
                    if (blnTplusCommex)
                    {
                        strsql += " Union all ";
                        strsql += " select CES_Name Name , CES_Segment Segment , left(CES_Cd,1) + 'N'  + Right(Rtrim(CES_Cd),1) Company , ";
                        strsql += " Case Right(Rtrim(CES_Cd),1) when 'X' Then 4 when 'K' Then 3 When 'C' Then 1 When 'F' Then 2  end Segment1 ";
                        strsql += " from CompanyExchangeSegments ";
                        strsql += " Where Right(Rtrim(CES_Cd),2) in ('BX','NX') ";
                    }
                    strsql += " Order by Segment1 ";
                    dt = objUtility.OpenDataTable(strsql);
                    foreach (DataRow dr in dt.Rows)
                    {
                        req.ExchSeg.Add(dr["Company"].ToString().Trim());
                    }

                    dt = objUtility.OpenDataTable("select * from other_products where op_product='Commex' and op_status='A'");
                    if (dt.Rows.Count > 0)
                    {
                        string strCommex = "[" + dt.Rows[0]["op_server"].ToString().Trim() + "]" + "." + "[" + dt.Rows[0]["op_database"].ToString().Trim() + "]" + "." + dt.Rows[0]["op_owner"].ToString().Trim();
                        strsql = " select distinct CES_Name Name , CES_Segment Segment , left(CES_Cd,1) + 'X' Company ";
                        strsql += " from " + strCommex + ".CompanyExchangeSegments ";
                        strsql += " Where Right(Rtrim(CES_Cd),1) <> 'M' ";
                        strsql += " Order by left(CES_Cd,1) + 'X' ";
                        DataTable dtCommex = objUtility.OpenDataTable(strsql);
                        foreach (DataRow dr in dtCommex.Rows)
                        {
                            req.ExchSeg.Add(dr["Company"].ToString().Trim());
                        }
                    }
                }

                foreach (var comseg in req.ExchSeg)
                {
                    if (Strings.Right(comseg, 1) == "X")
                    {
                        BlnCommex = true;
                    }
                }

                if (req.Filter.Client != null)
                {
                    if (req.Filter.Client.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Client.ToArray(), "##"));
                        strClientWhere += " or cm_cd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Branch != null)
                {
                    if (req.Filter.Branch.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Branch.ToArray(), "##"));
                        strClientWhere += " or cm_brboffcode in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Group != null)
                {
                    if (req.Filter.Group.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Group.ToArray(), "##"));
                        strClientWhere += " or cm_groupcd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Family != null)
                {
                    if (req.Filter.Family.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Family.ToArray(), "##"));
                        strClientWhere += " or cm_familycd in('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (strClientWhere.Length > 0)
                {
                    strClientWhere = " and (" + strClientWhere.Substring(3) + ") ";
                }

                using (var con = new SqlConnection(objUtility.GetConnectionStr()))
                {
                    con.Open();
                    string strSql = fnTurnOverQuery(strClientWhere, req.FromDate, req.ToDate, req.ExchSeg, false, "2", "1", ChkCashBreakup, ChkCashTurnover, chkCashBrokerage, ChkFoTurnover, chkFOBreakup, ChkFOBrokerage, BlnCommex, con);
                    DataTable ds = objUtility.OpenDataTableTmp(strSql, con);

                    List<CompanyPerformanceDatabaseResponse> companyPerformances = new List<CompanyPerformanceDatabaseResponse>();

                    for (int i = 0; i < ds.Rows.Count; i++)
                    {
                        companyPerformances.Add(new CompanyPerformanceDatabaseResponse
                        {

                            ft_rptcd = (ds.Columns.Contains("ft_rptcd")) ? ds.Rows[i]["ft_rptcd"].ToString().Trim() : "",
                            ft_rptnm = (ds.Columns.Contains("ft_rptnm")) ? ds.Rows[i]["ft_rptnm"].ToString().Trim() : "",
                            ft_companycode = (ds.Columns.Contains("ft_companycode")) ? ds.Rows[i]["ft_companycode"].ToString() : "",
                            ft_grpcd = (ds.Columns.Contains("ft_grpcd")) ? ds.Rows[i]["ft_grpcd"].ToString() : "",
                            ft_grpnm = (ds.Columns.Contains("ft_grpnm")) ? ds.Rows[i]["ft_grpnm"].ToString().Trim() : "",
                            ft_sname = (ds.Columns.Contains("ft_sname")) ? ds.Rows[i]["ft_sname"].ToString().Trim() : "",
                            ft_dpid = (ds.Columns.Contains("ft_dpid")) ? ds.Rows[i]["ft_dpid"].ToString() : "",
                            ft_exchange = (ds.Columns.Contains("ft_exchange")) ? ds.Rows[i]["ft_exchange"].ToString() : "",
                            ft_Cashbrokerage = (ds.Columns.Contains("ft_Cashbrokerage")) ? ds.Rows[i]["ft_Cashbrokerage"].ToDouble() : 0,
                            ft_CashFiller = (ds.Columns.Contains("ft_CashFiller")) ? ds.Rows[i]["ft_CashFiller"].ToDouble() : 0,
                            ft_cashTotal = (ds.Columns.Contains("ft_cashTotal")) ? ds.Rows[i]["ft_cashTotal"].ToDouble() : 0,
                            ft_DelvBrokerage = (ds.Columns.Contains("ft_DelvBrokerage")) ? ds.Rows[i]["ft_DelvBrokerage"].ToDouble() : 0,
                            ft_DelvTotal = (ds.Columns.Contains("ft_DelvTotal")) ? ds.Rows[i]["ft_DelvTotal"].ToDouble() : 0,
                            ft_FOBrokerage = (ds.Columns.Contains("ft_FOBrokerage")) ? ds.Rows[i]["ft_FOBrokerage"].ToDouble() : 0,
                            ft_FOFiller = (ds.Columns.Contains("ft_FOFiller")) ? ds.Rows[i]["ft_FOFiller"].ToDouble() : 0,
                            ft_FOTotal = (ds.Columns.Contains("ft_FOTotal")) ? ds.Rows[i]["ft_FOTotal"].ToDouble() : 0,
                            ft_FutBrokerage = (ds.Columns.Contains("ft_FutBrokerage")) ? ds.Rows[i]["ft_FutBrokerage"].ToDouble() : 0,
                            ft_FutTotal = (ds.Columns.Contains("ft_FutTotal")) ? ds.Rows[i]["ft_FutTotal"].ToDouble() : 0,
                            ft_ledger = (ds.Columns.Contains("ft_ledger")) ? ds.Rows[i]["ft_ledger"].ToDouble() : 0,
                            ft_OptBrokerage = (ds.Columns.Contains("ft_OptBrokerage")) ? ds.Rows[i]["ft_OptBrokerage"].ToDouble() : 0,
                            ft_OptTotal = (ds.Columns.Contains("ft_OptTotal")) ? ds.Rows[i]["ft_OptTotal"].ToDouble() : 0,
                            ft_Specbrokerage = (ds.Columns.Contains("ft_Specbrokerage")) ? ds.Rows[i]["ft_Specbrokerage"].ToDouble() : 0,
                            ft_specTotal = (ds.Columns.Contains("ft_specTotal")) ? ds.Rows[i]["ft_specTotal"].ToDouble() : 0,
                            ft_FutTotalC = (ds.Columns.Contains("ft_FutTotalC")) ? ds.Rows[i]["ft_FutTotalC"].ToDouble() : 0,
                            ft_OptTotalC = (ds.Columns.Contains("ft_OptTotalC")) ? ds.Rows[i]["ft_OptTotalC"].ToDouble() : 0,
                            ft_FOTotalC = (ds.Columns.Contains("ft_FOTotalC")) ? ds.Rows[i]["ft_FOTotalC"].ToDouble() : 0,
                            ft_FutBrokerageC = (ds.Columns.Contains("ft_FutBrokerageC")) ? ds.Rows[i]["ft_FutBrokerageC"].ToDouble() : 0,
                            ft_OptBrokerageC = (ds.Columns.Contains("ft_OptBrokerageC")) ? ds.Rows[i]["ft_OptBrokerageC"].ToDouble() : 0,
                            ft_FOBrokerageC = (ds.Columns.Contains("ft_FOBrokerageC")) ? ds.Rows[i]["ft_FOBrokerageC"].ToDouble() : 0,
                            ft_segment = (ds.Columns.Contains("ft_segment")) ? ds.Rows[i]["ft_segment"].ToString() : "",
                        });
                    }

                    var companyClients = companyPerformances.Select(x => x.ft_rptnm).Distinct();

                    foreach (var companyClient in companyClients)
                    {
                        var companyPerformanceList = companyPerformances.Where(x => x.ft_rptnm == companyClient);
                        var dict = new Dictionary<string, object>();
                        string compName = "";

                        var dateList = companyPerformanceList.Select(x => x.ft_grpcd).Distinct().ToList();
                        List<CompanyPerfomanceWithDate1> perDate1 = new List<CompanyPerfomanceWithDate1>();
                        foreach (var date in dateList)
                        {
                            var dateData = companyPerformanceList.Where(x => x.ft_grpcd == date).ToList();
                            var dictExDate = new Dictionary<string, object>();
                            foreach (var item in req.ExchSeg)
                            {
                                if (item.Contains("C"))
                                {
                                    compName = objUtility.fnFireQueryTradeWeb("CompanyExchangeSegments", " left(ltrim(Rtrim(CES_ShortNm)),4) + ' ' + Rtrim(CES_Segment) ", "Right(CES_Cd,1) = '" + Strings.Right(item, 1) + "' and left(CES_Cd,1)", Strings.Left(item, 1), true);
                                    var dateExData = dateData.Where(x => x.ft_segment == "C").ToList();
                                    if (dateExData.Count > 0)
                                    {
                                        if (req.CashDelNonDelBreakup)
                                        {
                                            dictExDate.Add(compName, dateExData.Select(x =>
                                           new CashSegmentBreakup
                                           {
                                               ScripCode = x.ft_grpnm,
                                               ScripName = x.ft_sname,
                                               Spec_To = x.ft_specTotal,
                                               Cash_Brok = x.ft_Cashbrokerage,
                                               Cash_To = x.ft_cashTotal,
                                               Delv_Brok = x.ft_DelvBrokerage,
                                               Delv_To = x.ft_DelvTotal,
                                               Spec_Brok = x.ft_Specbrokerage
                                           }) ?? new List<CashSegmentBreakup>());
                                        }
                                        else
                                        {
                                            dictExDate.Add(compName, dateExData.Select(x =>
                                           new CashSegmentNoBreakup
                                           {
                                               ScripCode = x.ft_grpnm,
                                               ScripName = x.ft_sname,
                                               Cash_Brok = x.ft_Cashbrokerage,
                                               Cash_To = x.ft_cashTotal
                                           }) ?? new List<CashSegmentNoBreakup>());
                                        }
                                    }
                                }
                                else if (item.Contains("F"))
                                {
                                    if (Strings.Right(item, 1) == "Y")
                                    {
                                        compName = Strings.Left(item.Trim(), 1) + "X";
                                    }
                                    else
                                    {
                                        compName = Strings.Left(item.Trim(), 1);
                                    }
                                    compName = objUtility.fnFireQueryTradeWeb("CompanyExchangeSegments", " left(ltrim(Rtrim(CES_ShortNm)),4) + ' ' + Rtrim(CES_Segment) ", "Right(CES_Cd,1) = '" + Strings.Right(item, 1) + "' and left(CES_Cd,1)", Strings.Left(item, 1), true);
                                    var dateExData = dateData.Where(x => x.ft_segment == "F").ToList();
                                    if (dateExData.Count > 0)
                                    {
                                        if (req.FOBreakup)
                                        {
                                            dictExDate.Add(compName, dateExData.Select(x =>
                                           new FOSegmentBreakup
                                           {
                                               SeriesID = x.ft_grpnm,
                                               SeriesName = x.ft_sname,
                                               Fut_To = x.ft_FutTotal,
                                               Opt_To = x.ft_OptTotal,
                                               FO_To = x.ft_FOTotal,
                                               Fut_Brok = x.ft_FutBrokerage,
                                               Opt_Brok = x.ft_OptBrokerage,
                                               FO_Brok = x.ft_FOBrokerage
                                           }) ?? new List<FOSegmentBreakup>());
                                        }
                                        else
                                        {
                                            dictExDate.Add(compName, dateExData.Select(x =>
                                             new FOSegmentNoBreakup
                                             {
                                                 SeriesID = x.ft_grpnm,
                                                 SeriesName = x.ft_sname,
                                                 FO_To = x.ft_FOTotal,
                                                 FO_Brok = x.ft_FOBrokerage
                                             }) ?? new List<FOSegmentNoBreakup>());
                                        }
                                    }
                                }
                                else if (item.Contains("K"))
                                {
                                    if (Strings.Right(item, 1) == "Y")
                                    {
                                        compName = Strings.Left(item.Trim(), 1) + "X";
                                    }
                                    else
                                    {
                                        compName = Strings.Left(item.Trim(), 1);
                                    }
                                    compName = objUtility.fnFireQueryTradeWeb("CompanyExchangeSegments", " left(ltrim(Rtrim(CES_ShortNm)),4) + ' ' + Rtrim(CES_Segment) ", "Right(CES_Cd,1) = '" + Strings.Right(item, 1) + "' and left(CES_Cd,1)", Strings.Left(item, 1), true);
                                    var dateExData = dateData.Where(x => x.ft_segment == "K").ToList();
                                    if (dateExData.Count > 0)
                                    {
                                        dictExDate.Add(compName, dateExData.Select(x =>
                                       new FOSegmentBreakup
                                       {
                                           SeriesID = x.ft_grpnm,
                                           SeriesName = x.ft_sname,
                                           Fut_To = x.ft_FutTotal,
                                           Opt_To = x.ft_OptTotal,
                                           FO_To = x.ft_FOTotal,
                                           Fut_Brok = x.ft_FutBrokerage,
                                           Opt_Brok = x.ft_OptBrokerage,
                                           FO_Brok = x.ft_FOBrokerage
                                       }) ?? new List<FOSegmentBreakup>());
                                    }
                                }
                                else if (item.Contains("X"))
                                {
                                    if (Strings.Right(item, 1) == "Y")
                                    {
                                        compName = Strings.Left(item.Trim(), 1) + "X";
                                    }
                                    else
                                    {
                                        compName = Strings.Left(item.Trim(), 1);
                                    }
                                    compName = objUtility.fnFireQueryTradeWeb("CompanyExchangeSegments", " left(ltrim(Rtrim(CES_ShortNm)),4) + ' ' + Rtrim(CES_Segment) ", "Right(CES_Cd,1) = '" + Strings.Right(item, 1) + "' and left(CES_Cd,1)", Strings.Left(item, 1), true);
                                    var dateExData = dateData.Where(x => x.ft_segment == "X").ToList();
                                    if (dateExData.Count > 0)
                                    {
                                        dictExDate.Add(compName, dateExData.Select(x =>
                                       new FOSegmentBreakup
                                       {
                                           SeriesID = x.ft_grpnm,
                                           SeriesName = x.ft_sname,
                                           Fut_To = x.ft_FutTotalC,
                                           Opt_To = x.ft_OptTotalC,
                                           FO_To = x.ft_FOTotalC,
                                           Fut_Brok = x.ft_FutBrokerageC,
                                           Opt_Brok = x.ft_OptBrokerageC,
                                           FO_Brok = x.ft_FOBrokerageC
                                       }) ?? new List<FOSegmentBreakup>());
                                    }
                                }
                            }
                            perDate1.Add(new CompanyPerfomanceWithDate1
                            {
                                Date = date,
                                Data = dictExDate
                            });
                        }

                        companyPerformanceReportResponses.Add(new CompanyPerformanceReportResponse
                        {
                            Name = companyPerformanceList.FirstOrDefault()?.ft_rptnm,
                            Code = companyPerformanceList.FirstOrDefault()?.ft_rptcd,
                            Data = perDate1
                        });
                    }

                    return companyPerformanceReportResponses;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region Company Performance Helpers
        public dynamic fnTurnOverQuery(string strWhere, string strFromDt, string strToDt, List<string> lstComSeg, bool DeductBrokerageOn, string strAETurnOverOn, string strOTurnOverOn, Boolean ChkCashBreakup, Boolean ChkCashTurnover, Boolean chkCashBrokerage, Boolean ChkFoTurnover, Boolean chkFOBreakup, Boolean ChkFOBrokerage, Boolean BlnCommex, SqlConnection con)
        {
            string strSql = "";
            var strlistCom = "";
            var strAscSql = "";
            var strFieldList = fnCreatePerformanceTempTable(ChkCashBreakup, ChkCashTurnover, chkCashBrokerage, ChkFoTurnover, chkFOBreakup, ChkFOBrokerage, BlnCommex, con, out strlistCom);

            var strField = "td_clientcd, '', ";
            var strRptcd = "GR_CLIENTCD";
            strField += "td_dt, td_scripcd, ss_name, ";

            string strOptSql = "";

            string strExerSql = "";
            switch (strAETurnOverOn.ToLower())
            {
                case "0":
                    {
                        strExerSql = "sm_strikeprice"; // Strike Price
                        break;
                    }

                case "1":
                    {
                        strExerSql = "ex_settlerate"; // Settle Price
                        break;
                    }

                case "2":
                    {
                        strExerSql = "abs(ex_diffrate)"; // Difference
                        break;
                    }

                case "3":
                    {
                        strExerSql = "(ex_settlerate + abs(ex_diffrate))"; // Settle Price + Difference
                        break;
                    }

                case "4":
                    {
                        strExerSql = "0"; //Exclude Exer/Assgn
                        break;
                    }
            }

            strOptSql = "";
            switch (strOTurnOverOn.ToLower())
            {
                case "0"://strike price
                    {
                        strOptSql = "sm_strikeprice";
                        break;
                    }

                case "1"://premium
                    {
                        strOptSql = "td_marketrate";
                        break;
                    }

                case "2"://strike price + premium
                    {
                        strOptSql = "(sm_strikeprice + td_mainbrrate)";
                        break;
                    }
            }

            string strCommex = "";
            strSql = "select * from other_products where op_product='Commex' and op_status='A'";
            DataTable dt = objUtility.OpenDataTable(strSql);
            if (dt.Rows.Count > 0)
            {
                strCommex = "[" + dt.Rows[0]["op_server"].ToString().Trim() + "]" + "." + "[" + dt.Rows[0]["op_database"].ToString().Trim() + "]" + "." + dt.Rows[0]["op_owner"].ToString().Trim();
            }

            Boolean blnCash = false;
            Boolean blnFo = false;

            objUtility.ExecuteSQLTmp("truncate table #cftover", con);

            strAscSql = "Insert into #cftover (ft_dpid, ft_companycode, ft_exchange, ft_segment, ft_rptcd, ft_rptnm, ft_grpcd, ft_grpnm, ft_sname, ";

            string strGSTField;
            for (var i = 0; i <= lstComSeg.Count - 1; i++)
            {
                string strComSeg = lstComSeg[i].ToString().Trim();
                strComSeg = Strings.Left(lstComSeg[i], 1) + Strings.Right(lstComSeg[i], 1);
                if (Strings.Right(strComSeg, 1) == "C")
                {
                    if (ChkCashTurnover == true || chkCashBrokerage == true)
                        blnCash = true;
                    else
                        blnCash = false;
                    if (blnCash)
                    {
                        if (Strings.InStr(1, strField, "td_seriesid") != 0)
                            strField = strField.Replace("td_seriesid", "td_scripcd");
                        if (Strings.InStr(1, strField, "sm_desc") != 0)
                        {
                            strField = strField.Replace("sm_desc", "ss_name");
                        }
                        strSql = strAscSql + Strings.Left(strFieldList, Strings.InStr(1, strFieldList, "ft_CashFiller") + 12) + ") select td_companycode+'C',td_companycode,'','C' ," + strField;
                        if (ChkCashBreakup == false)
                        {
                            if (ChkCashTurnover)
                                strSql += " sum((td_bqty+td_sqty)*td_marketrate), ";
                            if (chkCashBrokerage)
                                strSql += " sum(Round((td_bqty+td_sqty)*td_brokerage,4)), ";
                            strSql += "0 from trx with(index(idx_trx_dt_clientcd)),Settlements,settlement_type, Securities ";
                        }
                        else
                        {
                            if (ChkCashTurnover)
                                strSql += " 0, Case When sy_maptype = 'C' Or td_marginYN = 'B' then sum((td_bqty+td_sqty)*td_marketrate) else abs(sum(td_bqty-td_sqty) * case sign(sum(td_bqty-td_sqty)) when 1 then sum(td_bqty*td_marketrate)/sum(td_bqty) else case sign(sum(td_bqty-td_sqty)) when -1 then sum(td_sqty*td_marketrate)/sum(td_sqty) else 0 end end) end , sum((td_bqty+td_sqty)*td_marketrate), ";
                            if (chkCashBrokerage)
                                strSql += " 0, 0, 0, ";
                            strSql += "0 from trx with(index(idx_trx_dt_clientcd)),Settlements,settlement_type, Securities ";
                        }
                        if ((Strings.InStr(1, strSql, "cm_") > 0) || Strings.InStr(1, strWhere, "cm_").ToBoolean())
                            strSql += ", client_master ";
                        strSql += " where td_stlmnt=se_stlmnt and se_type=sy_type and se_exchange = sy_exchange and td_scripcd = ss_cd ";
                        strSql += " and td_companycode = '" + Strings.Left(strComSeg, 1) + "' and ";
                        strSql += " td_dt >= '" + strFromDt + "' and td_dt <= '" + strToDt + "'" + strWhere;
                        if (Strings.InStr(1, strSql, "client_master") > 0)
                            strSql += " and td_clientcd=cm_cd ";
                        strSql += "group by td_companycode,  " + strField.Replace("'',", "") + " case When  Left(td_stlmnt,2) in ('BW','NN','BQ','NQ') Then sy_maptype else td_stlmnt end,td_companycode+'C',sy_maptype,td_marginYN,td_dt" + (ChkCashTurnover ? ",td_clientcd,td_scripcd" : "");
                        objUtility.ExecuteSQLTmp(strSql, con);

                        if (DeductBrokerageOn == true && strRptcd.Trim() != "")
                        {
                            strGSTField = strField.Replace("td_clientcd", "gr_clientcd");
                            strGSTField = Strings.Replace(strGSTField, "td_scripcd", "''");
                            strGSTField = Strings.Replace(strGSTField, "ss_name", "''");
                            strGSTField = Strings.Replace(strGSTField, "td_dt", "Gr_Date");

                            strSql = "Insert Into #cftover ";
                            strSql += " Select Right(GR_CompanyCode,2) ft_dpid, Left(GR_CompanyCode,1) ft_companycode, '' ft_exchange , Right(GR_CompanyCode,1) ft_segment , ";
                            strSql += strGSTField;
                            if (ChkCashTurnover == true)
                            {
                                if (ChkCashBreakup == true)
                                {
                                    strSql += " 0 ft_specTotal ,";
                                    strSql += " 0 ft_DelvTotal ,";
                                }
                                strSql += " 0 ft_CashTotal ,";
                            }
                            if (chkCashBrokerage == true)
                            {
                                if (ChkCashBreakup == true)
                                {
                                    strSql += " 0 ft_Specbrokerage ,";
                                    strSql += " 0 ft_DelvBrokerage ,";
                                }
                                strSql += " - GR_GrossAmt ft_CashBrokerage ,";
                            }
                            strSql += " 0 ft_CashFiller ,";
                            if (ChkFoTurnover == true)
                            {
                                if (chkFOBreakup == true)
                                {
                                    strSql += " 0 ft_FutTotal ,";
                                    strSql += " 0 ft_OptTotal ,";
                                }
                                strSql += " 0 ft_FOTotal ,";
                            }
                            if (ChkFOBrokerage == true)
                            {
                                if (chkFOBreakup == true)
                                {
                                    strSql += " 0 ft_FutBrokerage ,";
                                    strSql += " 0 ft_OptBrokerage ,";
                                }
                                strSql += " 0 ft_FOBrokerage ,";
                            }
                            strSql += " 0 ft_FOFiller , ";
                            if (BlnCommex)
                            {
                                if (ChkFoTurnover == true)
                                {
                                    if (chkFOBreakup == true)
                                    {
                                        strSql += " 0 ft_FutTotalC ,";
                                        strSql += " 0 ft_OptTotalC ,";
                                    }
                                    strSql += " 0 ft_FOTotalC ,";
                                }
                                if (ChkFOBrokerage == true)
                                {
                                    if (chkFOBreakup == true)
                                    {
                                        strSql += " 0 ft_FutBrokerageC ,";
                                        strSql += " 0 ft_OptBrokerageC ,";
                                    }
                                    strSql += " 0 ft_FOBrokerageC ,";
                                }
                                strSql += " 0 ft_FOFillerC , ";
                            }
                            strSql += " 0 ft_ledger ";
                            strSql += " From GSt_REVERSAL, client_master, client_Info ";
                            strSql += " Where  cm_cd=cm2_cd and GR_ClientCd=cm_cd and GR_Date >= '" + strFromDt + "' and GR_Date <= '" + strToDt + "' ";
                            strSql += " and Right(GR_CompanyCode,2) = '" + strComSeg + "' " + strWhere;
                            strSql += " and Gr_common = 'Settlement' and GR_Status = 'Y' and GR_Flag='F' ";
                            objUtility.ExecuteSQLTmp(strSql, con);
                        }
                    }
                }
                else if (Strings.Right(strComSeg, 1) == "X")
                {
                    if (ChkFoTurnover == true || ChkFOBrokerage == true)
                        blnFo = true;
                    else
                        blnFo = false;
                    if (blnFo == true)
                    {
                        if (Strings.InStr(1, strField, "td_scripcd") != 0)
                            strField = strField.Replace("td_scripcd", "td_seriesid");
                        if (Strings.InStr(1, strField, "ss_name") != 0)
                            strField = strField.Replace("ss_name", "sm_desc");
                        if (Strings.InStr(1, strField, "td_CtclId") != 0)
                            strField = strField.Replace("td_CtclId", "''");
                        strSql = strAscSql + strlistCom + ")" + " select td_companycode+'X', td_companycode, '' , 'X' , " + strField;

                        if (!chkFOBreakup == true)
                        {
                            if (ChkFoTurnover)
                                strSql += " sum((td_bqty+td_sqty)*sm_multiplier*" + strOptSql + "), ";
                            if (ChkFOBrokerage)
                                strSql += " sum((td_bqty+td_sqty)*sm_multiplier*td_brokerage), ";
                            strSql += "0 from " + (Strings.InStr("NX,BX", Strings.Right(strComSeg, 2)) > 0 ? "" : strCommex + ".") + "trades  ";
                        }
                        else
                        {
                            if (ChkFoTurnover == true)
                                strSql += " sum(case sign(sm_strikeprice) when 0 then " + strOptSql + "*(td_bqty+td_sqty)*sm_multiplier else 0 end), sum(case sign(sm_strikeprice) when 0 then 0 else " + strOptSql + "*(td_bqty+td_sqty)*sm_multiplier end), sum(" + strOptSql + "*(td_bqty+td_sqty)*sm_multiplier), ";
                            if (ChkFOBrokerage)
                                strSql += " sum(case sign(sm_strikeprice) when 0 then td_brokerage*(td_bqty+td_sqty)*sm_multiplier else 0 end), sum(case sign(sm_strikeprice) when 0 then 0 else td_brokerage*(td_bqty+td_sqty)*sm_multiplier end), sum(td_brokerage*(td_bqty+td_sqty)*sm_multiplier), ";
                            strSql += "0 from " + (Strings.InStr("NX,BX", Strings.Right(strComSeg, 2)) > 0 ? "" : strCommex + ".") + "trades ";
                        }
                        strSql += ", client_master ";
                        if (Strings.InStr(1, strSql, "sm_") > 0 || Strings.InStr(1, strWhere, "sm_").ToBoolean())
                            strSql += ", " + (Strings.InStr("NX,BX", Strings.Right(strComSeg, 2)) > 0 ? "" : strCommex + ".") + "series_master ";
                        strSql += " where td_companycode = '" + Strings.Left(strComSeg, 1) + "' and ";
                        strSql += " td_dt >= '" + strFromDt + "' and td_dt <= '" + strToDt + "'" + strWhere;
                        strSql += " and td_clientcd=cm_cd ";
                        if (Strings.InStr(1, strSql, "series_master") > 0)
                        {
                            strSql += " and td_seriesid=sm_seriesid and td_exchange = sm_exchange ";
                            if (Strings.InStr("NX,BX", Strings.Right(strComSeg, 2)) > 0)
                                strSql += " and td_segment = sm_segment and td_segment = '" + Strings.Right(strComSeg, 1) + "'";
                        }
                        strSql += "group by td_companycode,  " + strField.Replace("'',", "") + " left(td_companycode,1)";
                        objUtility.ExecuteSQLTmp(strSql, con);
                        if (DeductBrokerageOn == true && strRptcd.Trim() != "")
                        {
                            strGSTField = "";

                            strGSTField = strField.Replace("td_clientcd", "gr_clientcd");
                            strGSTField = Strings.Replace(strGSTField, "td_seriesid", "''");
                            strGSTField = Strings.Replace(strGSTField, "td_scripcd", "''");
                            strGSTField = Strings.Replace(strGSTField, "td_dt", "Gr_date");

                            strSql = "Insert Into #cftover ";
                            strSql += " Select Left(GR_CompanyCode,2) ft_dpid, Left(GR_CompanyCode,1) ft_companycode, Substring(GR_CompanyCode,2,1) ft_exchange , 'X' ft_segment , ";
                            strSql += strGSTField;
                            if (ChkCashTurnover == true)
                            {
                                if (ChkCashBreakup == true)
                                {
                                    strSql += " 0 ft_specTotal ,";
                                    strSql += " 0 ft_DelvTotal ,";
                                }
                                strSql += " 0 ft_CashTotal ,";
                            }
                            if (chkCashBrokerage == true)
                            {
                                if (ChkCashBreakup == true)
                                {
                                    strSql += " 0 ft_Specbrokerage ,";
                                    strSql += " 0 ft_DelvBrokerage ,";
                                }
                                strSql += " 0 ft_CashBrokerage ,";
                            }
                            strSql += " 0 ft_CashFiller ,";
                            if (ChkFoTurnover == true)
                            {
                                if (chkFOBreakup == true)
                                {
                                    strSql += " 0 ft_FutTotal ,";
                                    strSql += " 0 ft_OptTotal ,";
                                }
                                strSql += " 0  ft_FOTotal,";
                            }
                            if (ChkFOBrokerage == true)
                            {
                                if (chkFOBreakup == true)
                                {
                                    strSql += " 0 ft_FutBrokerage ,";
                                    strSql += " 0 ft_OptBrokerage ,";
                                }
                                strSql += " 0 ft_FOBrokerage ,";
                            }
                            strSql += " 0 ft_FOFiller , ";
                            if (BlnCommex)
                            {
                                if (ChkFoTurnover == true)
                                {
                                    if (chkFOBreakup == true)
                                    {
                                        strSql += " 0 ft_FutTotalC ,";
                                        strSql += " 0 ft_OptTotalC ,";
                                    }
                                    strSql += " 0 ft_FOTotalC ,";
                                }
                                if (ChkFOBrokerage == true)
                                {
                                    if (chkFOBreakup == true)
                                    {
                                        strSql += " 0 ft_FutBrokerageC ,";
                                        strSql += " 0 ft_OptBrokerageC ,";
                                    }
                                    strSql += " - GR_GrossAmt ft_FOBrokerageC ,";
                                }
                                strSql += " 0 ft_FOFillerC , ";
                            }
                            strSql += " 0 ft_ledger ";
                            strSql += " From " + (Strings.InStr("NX,BX", Strings.Right(strComSeg, 2)) > 0 ? "" : strCommex) + ".GSt_REVERSAL, " + (Strings.InStr("NX,BX", Strings.Right(strComSeg, 2)) > 0 ? "" : strCommex) + ".client_master, " + (Strings.InStr("NX,BX", Strings.Right(strComSeg, 2)) > 0 ? "" : strCommex) + ".client_Info ";
                            strSql += " Where  cm_cd=cm2_cd and GR_ClientCd=cm_cd and GR_Date >= '" + strFromDt + "' and GR_Date <= '" + strToDt + "' ";
                            strSql += " and Left(GR_CompanyCode,2) = '" + Strings.Left(strComSeg, 2) + "' " + strWhere;
                            strSql += " and Gr_common = 'Settlement' and GR_Status = 'Y' and GR_Flag='F' ";
                            objUtility.ExecuteSQLTmp(strSql, con);
                        }
                    }
                }
                else
                {
                    if (ChkFoTurnover == true || ChkFOBrokerage == true)
                        blnFo = true;
                    else
                        blnFo = false;
                    if (blnFo == true)
                    {
                        if (Strings.InStr(1, strField, "td_scripcd") != 0)
                            strField = strField.Replace("td_scripcd", "td_seriesid");
                        if (Strings.InStr(1, strField, "ss_name") != 0)
                            strField = strField.Replace("ss_name", "sm_desc");
                        if (BlnCommex)
                            strSql = strAscSql + Strings.Mid(strFieldList, Strings.InStr(1, strFieldList, "ft_CashFiller") + 15, Strings.InStr(1, strFieldList, "ft_FOFiller") + 15) + ") select td_companycode+td_Segment,td_companycode, '', td_segment, " + strField;
                        else
                            strSql = strAscSql + Strings.Mid(strFieldList, Strings.InStr(1, strFieldList, "ft_CashFiller") + 15) + ") select td_companycode+td_Segment,td_companycode, '', td_segment, " + strField;
                        if (!chkFOBreakup == true)
                        {
                            if (ChkFoTurnover)
                                strSql += " sum(round((td_bqty+td_sqty)*sm_multiplier*" + strOptSql + ",2)), ";
                            if (ChkFOBrokerage)
                                strSql += " sum(round((td_bqty+td_sqty)*sm_multiplier*td_brokerage,4)), ";
                            strSql += "0 from trades with(index(idx_trades_dt_clientcd)) ";
                        }
                        else
                        {
                            if (ChkFoTurnover == true)
                                strSql += " sum(case sign(sm_strikeprice) when 0 then " + strOptSql + "*(td_bqty+td_sqty)*sm_multiplier else 0 end), sum(case sign(sm_strikeprice) when 0 then 0 else " + strOptSql + "*(td_bqty+td_sqty)*sm_multiplier end), sum(" + strOptSql + "*(td_bqty+td_sqty)*sm_multiplier), ";
                            if (ChkFOBrokerage)
                                strSql += " sum(case sign(sm_strikeprice) when 0 then Round(td_brokerage*(td_bqty+td_sqty)*sm_multiplier,4) else 0 end), sum(case sign(sm_strikeprice) when 0 then 0 else Round(td_brokerage*(td_bqty+td_sqty)*sm_multiplier,4) end), sum(Round(td_brokerage*(td_bqty+td_sqty)*sm_multiplier,4)), ";
                            strSql += "0 from trades with(index(idx_trades_dt_clientcd)) ";
                        }

                        if (Strings.InStr(1, strSql, "cm_") > 0 || Strings.InStr(1, strWhere, "cm_").ToBoolean())
                            strSql += ", client_master ";
                        if (Strings.InStr(1, strSql, "sm_") > 0 | Strings.InStr(1, strWhere, "sm_").ToBoolean())
                            strSql += ", series_master ";
                        strSql += " where td_companycode = '" + Strings.Left(strComSeg, 1) + "' and ";
                        strSql += " td_segment = '" + (Strings.Right(strComSeg, 1) == "Y" ? "X" : Strings.Right(strComSeg, 1)) + "' and ";
                        if ((Strings.Right(strComSeg, 1) == "F" || Strings.Right(strComSeg, 1) == "K" || Strings.Right(strComSeg, 1) == "Y") && Strings.InStr(1, strWhere, "td_scripcd") > 0)
                            strSql += " td_dt >= '" + strFromDt + "' and td_dt <= '" + strToDt + "'" + Strings.Mid(strWhere, Strings.InStr(1, strWhere, " and cm")).Replace(")", "");
                        else
                            strSql += " td_dt >= '" + strFromDt + "' and td_dt <= '" + strToDt + "'" + strWhere;
                        if (Strings.InStr(1, strSql, "client_master") > 0)
                            strSql += " and td_clientcd=cm_cd ";
                        if (Strings.InStr(1, strSql, "series_master") > 0)
                            strSql += " and td_seriesid=sm_seriesid and td_exchange = sm_exchange and td_segment = sm_segment ";
                        strSql += "group by td_companycode,  " + strField.Replace("'',", "") + " td_segment";
                        objUtility.ExecuteSQLTmp(strSql, con);

                        if (DeductBrokerageOn == true & strRptcd.Trim() != "")
                        {
                            strGSTField = "";

                            strGSTField = strField.Replace("td_clientcd", "gr_clientcd");
                            strGSTField = Strings.Replace(strGSTField, "td_seriesid", "''");
                            strGSTField = Strings.Replace(strGSTField, "td_scripcd", "''");
                            strGSTField = Strings.Replace(strGSTField, "td_dt", "Gr_date");

                            strSql = "Insert Into #cftover ";
                            strSql += " Select GR_CompanyCode ft_dpid, Left(GR_CompanyCode,1) ft_companycode, Substring(GR_CompanyCode,2,1) ft_exchange , Right(GR_CompanyCode,1) ft_segment , ";
                            strSql += strGSTField;
                            if (ChkCashTurnover == true)
                            {
                                if (ChkCashBreakup == true)
                                {
                                    strSql += " 0 ft_specTotal ,";
                                    strSql += " 0 ft_DelvTotal ,";
                                }
                                strSql += " 0 ft_CashTotal ,";
                            }
                            if (chkCashBrokerage == true)
                            {
                                if (ChkCashBreakup == true)
                                {
                                    strSql += " 0 ft_Specbrokerage ,";
                                    strSql += " 0 ft_DelvBrokerage ,";
                                }
                                strSql += " 0 ft_CashBrokerage ,";
                            }
                            strSql += " 0 ft_CashFiller ,";
                            if (ChkFoTurnover == true)
                            {
                                if (chkFOBreakup == true)
                                {
                                    strSql += " 0 ft_FutTotal ,";
                                    strSql += " 0 ft_OptTotal ,";
                                }
                                strSql += " 0 ft_FoTotal ,";
                            }
                            if (ChkFOBrokerage == true)
                            {
                                if (chkFOBreakup == true)
                                {
                                    strSql += " 0 ft_FutBrokerage ,";
                                    strSql += " 0 ft_OptBrokerage ,";
                                }
                                strSql += " - GR_GrossAmt ft_FOBrokerage ,";
                            }
                            strSql += " 0 ft_FOFiller , ";
                            if (BlnCommex)
                            {
                                if (ChkFoTurnover == true)
                                {
                                    if (chkFOBreakup == true)
                                    {
                                        strSql += " 0 ft_FutTotalC ,";
                                        strSql += " 0 ft_OptTotalC ,";
                                    }
                                    strSql += " 0 ft_FOTotalC ,";
                                }
                                if (ChkFOBrokerage == true)
                                {
                                    if (chkFOBreakup == true)
                                    {
                                        strSql += " 0 ft_FutBrokerageC ,";
                                        strSql += " 0 ft_OptBrokerageC ,";
                                    }
                                    strSql += " 0 ft_FOBrokerageC ,";
                                }
                                strSql += " 0 ft_FOFillerC , ";
                            }
                            strSql += " 0 ft_ledger ";
                            strSql += " From GSt_REVERSAL, client_master, client_Info ";
                            strSql += " Where  cm_cd=cm2_cd and GR_ClientCd=cm_cd and GR_Date >= '" + strFromDt + "' and GR_Date <= '" + strToDt + "' ";
                            strSql += " and Right(GR_CompanyCode,2) = '" + strComSeg + "' " + strWhere;
                            strSql += " and Gr_common = 'Settlement' and GR_Status = 'Y' and GR_Flag='F' ";
                            objUtility.ExecuteSQLTmp(strSql, con);
                        }
                    }
                }
                if (Strings.Right(strComSeg, 1) == "C" & ChkCashBreakup == true)
                {
                    strSql = strAscSql + Strings.Left(strFieldList, Strings.InStr(1, strFieldList, "ft_CashFiller") + 12) + ") select td_companycode+'C',td_companycode, '','C', " + strField;
                    if (ChkCashTurnover)
                        strSql += " 0, 0, 0, ";
                    if (chkCashBrokerage)
                        strSql += " 0, sum( Case When sy_maptype = 'C' Or td_marginYN = 'B' then (td_brokerage*(td_bqty+td_sqty)) else case td_brokeragetype when 'DLV' then (td_bqty+td_sqty)*td_brokerage else 0 end end ), sum(td_brokerage*(td_bqty+td_sqty)), ";
                    strSql += "0 from trx with(index(idx_trx_dt_clientcd)) , settlements , Settlement_type, securities ";
                    if (Strings.InStr(1, strSql, "cm_") > 0 || Strings.InStr(1, strWhere, "cm_").ToBoolean())
                        strSql += ", client_master ";
                    strSql += " where td_stlmnt = se_stlmnt and se_type = sy_type and sy_exchange = se_exchange and td_scripcd = ss_cd and td_companycode = '" + Strings.Left(strComSeg, 1) + "' and ";
                    strSql += " td_dt >= '" + strFromDt + "' and td_dt <= '" + strToDt + "'" + strWhere;
                    if (Strings.InStr(1, strSql, "client_master") > 0)
                        strSql += " and td_clientcd=cm_cd ";
                    strSql += "group by td_companycode,  " + Strings.Replace(strField, "'',", "") + " case When  Left(td_stlmnt,2) in ('BW','NN','BQ','NQ') Then sy_maptype else td_stlmnt end ";
                    objUtility.ExecuteSQLTmp(strSql, con);
                    objUtility.ExecuteSQLTmp("update #cftover set " + (ChkCashTurnover == true ? "ft_spectotal=ft_cashtotal-ft_delvtotal," : "") + (chkCashBrokerage == true ? " ft_specbrokerage=ft_cashbrokerage-ft_delvbrokerage," : "") + " ft_cashfiller=0 where ft_companycode+ft_segment='" + Strings.Left(strComSeg, 3) + "'", con);
                }
            }
            strSql += "select " + strField;

            //ClientWise
            strSql = "update a set ft_rptnm=left(cm_name,50) from #cftover a, client_master where ft_rptcd=cm_cd";
            objUtility.ExecuteSQLTmp(strSql, con);

            //GroupCP
            strSql = "update a set ft_grpnm=left(cm_name,30) from #cftover a, client_master where ft_grpcd=cm_cd";
            objUtility.ExecuteSQLTmp(strSql, con);

            //GroupWise

            string strFieldList1 = "";
            string strOrderBy = "";
            var arrStr = strFieldList.Split(",");
            for (var i = 0; i <= arrStr.Length - 1; i++)
            {
                arrStr[i] = "sum(" + arrStr[i].Trim() + ") " + arrStr[i].Trim();
                strFieldList1 = strFieldList1 + arrStr[i] + ", ";
            }

            if (BlnCommex)
            {
                arrStr = strlistCom.Split(",");
                for (var i = 0; i <= arrStr.Length - 1; i++)
                {
                    arrStr[i] = "sum(" + arrStr[i].Trim() + ") " + arrStr[i].Trim();
                    strFieldList1 = strFieldList1 + arrStr[i] + ", ";
                }
            }

            strFieldList1 = Strings.Mid(strFieldList1, 1, Strings.Len(strFieldList1) - 2);
            strOrderBy = " order by ft_rptcd,ft_grpcd,Case ft_segment when 'K' Then 3 When 'C' Then 1 When 'F' Then 2 else 4 end,  ";
            strOrderBy = strOrderBy + " Case ft_exchange when 'B' then 1 when 'N' then 2 when 'M' then 3 else 4 end ";

            strSql = "select Case When Right(Rtrim(ft_dpid),1) ='C' Then 1 When Right(Rtrim(ft_dpid),1) ='F' Then 2 When Right(Rtrim(ft_dpid),1) ='K' Then 3 When Right(Rtrim(ft_dpid),1) ='X' Then 4 else 5 end ExchsOrder, ft_dpid, ft_companycode, ft_exchange, ft_rptcd, ft_rptnm, ft_grpcd, ft_grpnm, ft_sname, " + strFieldList1 + ", sum(ft_ledger) ft_ledger, ft_segment from #cftover group by ft_companycode,ft_exchange,ft_rptcd,ft_rptnm,ft_grpcd,ft_grpnm,ft_dpid,ft_segment,ft_sname";
            strSql += strOrderBy;

            return strSql;
        }

        public dynamic fnCreatePerformanceTempTable(Boolean ChkCashBreakup, Boolean ChkCashTurnover, Boolean chkCashBrokerage, Boolean ChkFoTurnover, Boolean chkFOBreakup, Boolean ChkFOBrokerage, Boolean BlnCommex, SqlConnection con, out string strlistCom)
        {
            string strList = "";
            string strSql = "";
            strlistCom = "";

            try
            { objUtility.ExecuteSQLTmp("drop table #cftover", con); }
            catch { }
            finally
            {
                strSql = "create table #cftover (";

                strSql += " ft_dpid char(5) not null,";
                strSql += " ft_companycode char(1) not null,";
                strSql += " ft_exchange char(1) not null,";
                strSql += " ft_segment char(1) not null,";
                strSql += " ft_rptcd char(20) not null ,";
                strSql += " ft_rptnm char(50) not null,";
                strSql += " ft_grpcd char(8) not null,";
                strSql += " ft_grpnm char(30) not null,";
                strSql += " ft_sname char(100) not null,";
                if (ChkCashTurnover == true)
                {
                    if (ChkCashBreakup == true)
                    {
                        strSql += " ft_specTotal Money not null default 0,";
                        strSql += " ft_DelvTotal Money not null default 0,";
                        strList = strList + "ft_specTotal, ft_DelvTotal , ";
                    }
                    strSql += " ft_CashTotal Money not null default 0,";
                    strList = strList + "ft_cashTotal, ";
                }
                if (chkCashBrokerage == true)
                {
                    if (ChkCashBreakup == true)
                    {
                        strSql += " ft_Specbrokerage Money not null default 0,";
                        strSql += " ft_DelvBrokerage Money not null default 0,";
                        strList = strList + "ft_Specbrokerage, ft_DelvBrokerage, ";
                    }
                    strList = strList + "ft_Cashbrokerage, ";
                    strSql += " ft_CashBrokerage Money not null default 0,";
                }
                strList = strList + "ft_CashFiller, ";
                strSql += "ft_CashFiller Money not null default 0,";

                if (ChkFoTurnover == true)
                {
                    if (chkFOBreakup == true)
                    {
                        strSql += " ft_FutTotal Money not null default 0,";
                        strSql += " ft_OptTotal Money not null default 0,";
                        strList = strList + "ft_FutTotal, ft_OptTotal, ";
                    }
                    strList = strList + "ft_FOTotal, ";
                    strSql += " ft_FOTotal Money not null default 0,";
                }
                if (ChkFOBrokerage == true)
                {
                    if (chkFOBreakup == true)
                    {
                        strSql += " ft_FutBrokerage Money not null default 0,";
                        strSql += " ft_OptBrokerage Money not null default 0,";
                        strList = strList + "ft_FutBrokerage, ft_OptBrokerage, ";
                    }
                    strList = strList + "ft_FOBrokerage, ";
                    strSql += " ft_FOBrokerage Money not null default 0,";
                }
                strList = strList + "ft_FOFiller";
                strSql += "ft_FOFiller Money not null default 0, ";

                if (BlnCommex)
                {
                    strlistCom = "";
                    if (ChkFoTurnover == true)
                    {
                        if (chkFOBreakup == true)
                        {
                            strSql += " ft_FutTotalC Money not null default 0,";
                            strSql += " ft_OptTotalC Money not null default 0,";
                            strlistCom = strlistCom + "ft_FutTotalC, ft_OptTotalC, ";
                        }
                        strlistCom = strlistCom + "ft_FOTotalC, ";
                        strSql += " ft_FOTotalC Money not null default 0,";
                    }
                    if (ChkFOBrokerage == true)
                    {
                        if (chkFOBreakup == true)
                        {
                            strSql += " ft_FutBrokerageC Money not null default 0,";
                            strSql += " ft_OptBrokerageC Money not null default 0,";
                            strlistCom = strlistCom + "ft_FutBrokerageC, ft_OptBrokerageC, ";
                        }
                        strlistCom = strlistCom + "ft_FOBrokerageC, ";
                        strSql += " ft_FOBrokerageC Money not null default 0,";
                    }
                    strlistCom = strlistCom + "ft_FOFillerC";
                    strSql += "ft_FOFillerC Money not null default 0, ";
                }
                strSql += " ft_ledger Money not null default 0";
                strSql += " )";
                objUtility.ExecuteSQLTmp(strSql, con);
            }
            return strList;
        }
        #endregion

        public dynamic GetMasters(string type)
        {
            try
            {
                DataTable dt = new DataTable();

                if (string.IsNullOrWhiteSpace(type) || !"BGFAR".Contains(Strings.Left(type.Trim(), 1).ToUpper()))
                {
                    return "Invalid Type";
                }

                if (type.Trim().ToUpper() == "B")
                {
                    strsql = "select distinct rtrim(bm_branchcd) as Code, rtrim(bm_branchname) as Name, rtrim(bm_contact) as ContactPerson, rtrim(bm_phone) as Mobile, rtrim(bm_email) as Email, rtrim(bm_add1) as Address1, ";
                    strsql += " rtrim(bm_add2) as Address2, rtrim(bm_add3) as Address3, rtrim(bm_city) as City, rtrim(bm_pin) as Pin, rtrim(bm_pwd) as GSTNo ";
                    strsql += " from branch_master where bm_status='A' ";
                }
                else if (type.Trim().ToUpper() == "G")
                {
                    strsql = "select distinct rtrim(gr_cd) as Code, rtrim(gr_desc) as Name, rtrim(gr_email) as Email ";
                    strsql += " from Group_master where gr_freezeyn = 'N' ";
                }
                else if (type.Trim().ToUpper() == "F")
                {
                    strsql = "select distinct rtrim(fm_cd) as Code, rtrim(fm_desc) as Name, rtrim(fm_email) as Email ";
                    strsql += " from Family_master where fm_freezeyn = 'N' ";
                }
                else if (type.Trim().ToUpper() == "A")
                {
                    strsql = "Select rtrim(RM_Cd) as Code, rtrim(RM_Name) as Name, rtrim(RM_tele1) as Mobile, rtrim(RM_Email) as Email, rtrim(RM_add1) as Address1, rtrim(RM_add2) as Address2, rtrim(RM_add3) as Address3, ";
                    strsql += " rtrim(RM_add4) as Address4, rtrim(rm_State) as State, rtrim(rm_pincode) as Pin, rtrim(RM_PANNo) as PanNo, rtrim(rm_regno) as AssocBrcd, rtrim(ISNULL((select bm_branchname from Branch_master where bm_branchcd=RM_regno),'')) as AssocBrnm ";
                    strsql += " From Subbrokers Where RM_freezeyn = 'A' order by RM_Name ";
                }
                else if (type.Trim().ToUpper() == "R")
                {
                    strsql = "Select rtrim(RM_Cd) as Code, rtrim(RM_Name) as Name, rtrim(RM_mobile) as Mobile, rtrim(RM_Email) as Email, rtrim(RM_add1) as Address1, rtrim(RM_add2) as Address2, rtrim(RM_add3) as Address3, ";
                    strsql += " rtrim(RM_add4) as Address4, rtrim(rm_State) as State, rtrim(rm_pincode) as Pin ";
                    strsql += " From RM_Master Where RM_status = 'A' order by RM_Name ";
                }

                dt = objUtility.OpenDataTable(strsql);
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
