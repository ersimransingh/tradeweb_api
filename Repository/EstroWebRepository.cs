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
using System.Xml.Linq;
using TradeWeb.API.Data;
using TradeWeb.API.Models;

namespace TradeWeb.API.Repository
{
    public class EstroWebRepository : IEstroWebRepository
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
        public EstroWebRepository(IConfiguration configuration, UtilityCommon objUtility, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            this.objUtility = objUtility;
            _httpContextAccessor = httpContextAccessor;
            _environment = environment;
        }
        #endregion

        public dynamic GetReport(EstroReportModel model, string strClientCode)
        {
            try
            {
                TradeWebDataGridResponse response = new TradeWebDataGridResponse();
                string strProjectName = "TradeWebAPI";
                string strModuleName = "Estro";
                string ProcedureName = "";
                int SerialNo = 0;
                string strXML = "";
                string updatedXmlString = "";

                try
                {
                    strXML = model.XML;
                    XDocument xdoc = XDocument.Parse($"<root>{strXML}</root>");
                    xdoc.Root.Element("ClientCode")?.Remove();
                    xdoc.Root.AddFirst(new XElement("ClientCode", strClientCode));
                    if (_configuration["IsEstroOffLine"] == "Y" && _configuration["IsTradeWeb"] == "E")
                    {
                        strModuleName = "EstroWeb";
                        xdoc.Root.AddFirst(new XElement("DPID", _configuration["SessionDPID"]));
                    }
                    updatedXmlString = xdoc.Root.ToString(SaveOptions.DisableFormatting).Replace("<root>", "").Replace("</root>", "");
                }
                catch (Exception ex)
                {
                    response.Message = ex.Message.Trim();
                    response.Data = null;
                    return response;
                }
                strsql = "select SerialNo, ProcedureName, IsDebugEnabled from tbl_GenericAPIDefinition";
                strsql += " where ProjectName='" + strProjectName + "' and ModuleName='" + strModuleName + "' and FunctionName='" + model.FunctionName + "' and IsActive = 'Y'";
                DataTable dt = objUtility.OpenDataTable(strsql);
                if (dt.Rows.Count > 0)
                {
                    SerialNo = Convert.ToInt32(dt.Rows[0]["SerialNo"].ToString().Trim());
                    ProcedureName = dt.Rows[0]["ProcedureName"].ToString().Trim();
                }

                if (SerialNo > 0)
                {
                    string connetionString = objUtility.GetConnectionStr();
                    using (SqlConnection conn = new SqlConnection(connetionString))
                    {
                        using (SqlCommand cmd = new SqlCommand(ProcedureName))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.CommandTimeout = 0;
                            if (conn.State == ConnectionState.Closed)
                                conn.Open();
                            cmd.Connection = conn;
                            cmd.Parameters.Add(new SqlParameter("@vcFunctionName", SqlDbType.VarChar));
                            cmd.Parameters["@vcFunctionName"].Value = model.FunctionName;
                            cmd.Parameters.Add(new SqlParameter("@vcXML", SqlDbType.VarChar));
                            cmd.Parameters["@vcXML"].Value = updatedXmlString;

                            cmd.Parameters.Add(new SqlParameter("@o_vcErrorFlag", SqlDbType.VarChar, 2000000));
                            cmd.Parameters["@o_vcErrorFlag"].Direction = ParameterDirection.Output;
                            cmd.Parameters.Add(new SqlParameter("@o_vcErrorMessage", SqlDbType.VarChar, 2000000));
                            cmd.Parameters["@o_vcErrorMessage"].Direction = ParameterDirection.Output;

                            SqlDataAdapter adap = new SqlDataAdapter
                            {
                                SelectCommand = cmd
                            };
                            DataSet ds = new DataSet();
                            adap.Fill(ds);
                            cmd.ExecuteNonQuery();

                            string retErrFlag = cmd.Parameters["@o_vcErrorFlag"].Value.ToString();
                            string retErrMsg = cmd.Parameters["@o_vcErrorMessage"].Value.ToString();
                            cmd.Parameters.Clear();

                            if (retErrFlag.Trim() == "E")
                            {
                                response.Message = retErrMsg;
                                response.Data = null;
                            }
                            else
                            {
                                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0] != null && ds.Tables[0].Rows.Count > 0)
                                {
                                    List<DataSet> lstDs = new List<DataSet>();
                                    lstDs.Add(ds);
                                    response.Message = "";
                                    response.Data = lstDs;
                                }
                                else
                                {
                                    response.Message = "No Record Found";
                                    response.Data = null;
                                }
                            }
                            return response;
                        }
                    }
                }
                else
                {
                    response.Message = "Function Name not found";
                    response.Data = null;
                    return response;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetUserDetails(string clientWhere)
        {
            try
            {
                strsql = "Select cm_cd as 'ClientCode',  cm_name as 'ClientName', cm_add1 as 'Address1', cm_add2 as 'Address2', cm_add3 as 'Address3', cm_add4 as 'Address4', cm_pin as 'Pincode', (select cs_desc from Clientsub_master where cs_code=cb_StateCdPer and cs_module='CS22') as 'State', (select cs_desc from Clientsub_master where cs_code=cb_CountryCdPer and cs_module='CS21') as 'Country', " +
                        " cm_mobile as 'Mobile', cm_email as 'Email', bs_description as 'Status', Rtrim(cm_blsavingcd) as 'TradingCd', fm_desc as 'Family', cb_panno as 'PanNo', isNull(cb_dob,'') as 'Date_Of_Birth', " +
                        " case when cm_sech_name <> '' and cm_thih_name <> '' then cm_sech_name + ',' + cm_thih_name when cm_sech_name <> '' then cm_sech_name when cm_thih_name <> '' then cm_thih_name else '--' end as 'Joints' , " +
                        " cb_nominee as 'Nominee', (select bt_description from Beneficiary_type where bt_code=cm_clienttype) as 'ClientType', " +
                        " isNull(cm_bankactno, '') as 'BankAccountNo', isNull(cm_bankbranch,'') as 'IFSC', isNull(cm_micr,'') as 'MICR', isNull(cm_bankname,'') as 'BankName', case isNull(cm_bankacttype,'') when '10' then 'SAVING' when '11' then 'CURRENT' when '13' then 'OVER DRAFT' when '12' then 'OVER DRAFT' else '' end as BankAcType, " +
                        " case ISNULL(cb_sadd1,'') when '01' then 'Below 1 Lac' when '11' then 'Below 1 Lac' when '02' then '1-5 Lacs' when '12' then '1-5 Lacs' when '03' then '5-10 Lacs' when '13' then '5-10 Lacs'  " +
                        " when '04' then '10-25 Lacs' when '14' then '10-25 Lacs' when '05' then 'More than 25 Lacs' when '06' then 'More than 25 Lacs' when '15' then '25 Lacs – 1 Crore' when '16' then 'More than 1 Crore' end as 'Income', " +
                        " bm_branchname as 'Branch', gr_desc as 'Group', Rtrim(cm_chgsscheme) as 'Scheme' " +
                        " from Client_master, Client_Backoffice, Beneficiary_status, Family_master, Beneficiary_type, Branch_master, Group_master Where cb_cmcd=cm_cd and cm_active = bs_code and fm_cd = cm_familycd and bt_code = cm_clienttype and bm_branchcd = cm_brboffcode and gr_cd = cm_groupcd " + clientWhere;
                DataTable dt = objUtility.OpenDataTable(strsql);
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

        public dynamic GetHolding(string clientWhere, bool blnTradeNet = false)
        {
            try
            {
                string strConn = "";

                if (blnTradeNet)
                {
                    strConn = objUtility.GetDPConn("E");
                    if (strConn == null)
                    {
                        return "Estro Connection not Available";
                    }
                }
                else
                {
                    using (var db = new DataContext())
                    {
                        strConn = db.Database.GetDbConnection().ConnectionString;
                    }
                }

                using (SqlConnection sqlConn = new SqlConnection(strConn))
                {
                    strsql = "select hld_ac_code as 'ClientCode',  hld_isin_code as 'ISIN', sc_company_name as 'ISINName', bt_code as 'BalanceCode', bt_description as 'BalanceType', hld_ac_pos as 'Quantity', sc_rate as 'Rate',  (hld_ac_pos * sc_rate) as 'Value' " +
                            " from Holding, Beneficiary_Type , Security, Client_master  " +
                            " Where hld_ac_code = cm_cd and bt_code = hld_ac_type and hld_isin_code = sc_isincode " + clientWhere + " order by hld_ac_code, sc_company_name";
                    DataTable dt = objUtility.OpenDataTable(strsql, sqlConn);

                    if (dt.Rows.Count > 0)
                    {
                        return dt;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetLedger(string clientWhere, string fromDt, string toDt, bool blnTradeNet = false)
        {
            try
            {
                DataTable dt = new DataTable();
                string strConn = "";

                if (blnTradeNet)
                {
                    strConn = objUtility.GetDPConn("E");
                    if (strConn == null)
                    {
                        return "Estro Connection not Available";
                    }
                }
                else
                {
                    using (var db = new DataContext())
                    {
                        strConn = db.Database.GetDbConnection().ConnectionString;
                    }
                }

                using (SqlConnection sqlConn = new SqlConnection(strConn))
                {
                    strsql = "select ld_clientcd as 'ClientCode', '0' as Balance, convert(char,convert(datetime,'" + fromDt + "'),103) as 'Date', '20210401' as ld_dt," +
                    " '' as 'DocumentNo','Opening Balance' as 'Particular', sum(ld_amount) as 'Amount', case sign(sum(ld_amount)) when -1 then 'C' else 'D' end as 'DebitFlag', " +
                    " Case When sum(ld_amount) > 0 Then sum(ld_amount) else 0 end Debit, " +
                    " Case When sum(ld_amount) < 0 Then abs(sum(ld_amount)) else 0 end Credit, " +
                    " '0' as 'Flag' " +
                    " From Ledger, Client_master " +
                    " Where ld_clientcd = cm_cd " + clientWhere + " and ld_dt < '" + fromDt + "' Group by ld_clientcd Having sum(ld_amount) <> 0" +
                    " Union all " +
                    " select ld_clientcd as 'ClientCode', '0' as Balance, convert(char,convert(datetime,ld_dt),103)  as 'Date', ld_dt, " +
                    " case ld_documentno when '0' then '' else ld_documenttype + '/' + Ltrim(Rtrim(convert(char,ld_documentno))) end as 'DocumentNo', " +
                    " ltrim(rtrim(ld_particular)) as 'Particular', ld_amount as 'Amount',  ld_debitflag as 'DebitFlag', " +
                    " Case When ld_amount > 0 Then ld_amount else 0 end Debit, " +
                    " Case When ld_amount < 0 Then abs(ld_amount) else 0 end Credit, " +
                    " '1' as 'Flag' " +
                    " From Ledger, Client_master " +
                    " where ld_clientcd = cm_cd " + clientWhere + " and ld_dt between '" + fromDt + "' and '" + toDt + "' order by flag,ld_dt ";

                    dt = objUtility.OpenDataTable(strsql, sqlConn);
                }

                if (dt.Rows.Count > 0)
                {
                    var clients = dt.AsEnumerable().Select(r => r.Field<string>("ClientCode")).Distinct().ToList();
                    DataTable dtNew = new DataTable();
                    foreach (var client in clients)
                    {
                        double Balance = 0;
                        DataTable dtClient = dt.AsEnumerable().Where(r => r.Field<string>("ClientCode") == client).CopyToDataTable();
                        foreach (DataRow drow in dtClient.Rows)
                        {
                            if ((decimal)drow["Amount"] > 0)
                            {
                                Balance = Balance + Conversion.Val(drow["Amount"]);
                            }
                            else if ((decimal)drow["Amount"] < 0)
                            {
                                Balance = Balance + Conversion.Val(drow["Amount"]);
                            }

                            drow["Balance"] = Balance;
                        }
                        dtNew.Merge(dtClient);
                    }
                    List<EstroLedgerModel> ledgerResponse = dtNew.AsEnumerable().Select(row =>
                    new EstroLedgerModel
                    {
                        ClientCode = row["ClientCode"].ToString(),
                        Date = objUtility.dtos(row["Date"].ToString()),
                        ChequeNo = "",
                        Particular = row["Particular"].ToString(),
                        Debit = Convert.ToDouble(row["Debit"]),
                        Credit = Convert.ToDouble(row["Credit"]),
                        Balance = Convert.ToDouble(row["Balance"])
                    }).ToList();
                    return ledgerResponse;
                }
                return null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetTransaction(string clientWhere, string fromDt, string toDt, bool blnTradeNet)
        {
            try
            {
                DataTable dt = new DataTable();
                UtilityCommon.urdata urdata;
                string strISIN = "";
                string strAcctype = "";
                string strACtype = "";
                string strConn = "";

                if (blnTradeNet)
                {
                    strConn = objUtility.GetDPConn("E");
                    if (strConn == null)
                    {
                        return "Estro Connection not Available";
                    }
                }
                else
                {
                    using (var db = new DataContext())
                    {
                        strConn = db.Database.GetDbConnection().ConnectionString;
                    }
                }

                using (SqlConnection sqlConn = new SqlConnection(strConn))
                {
                    strsql = "select cm_cd,cm_name,cm_add1,cm_add2,cm_add3,cm_add4,cm_pin,cm_tele1,cm_familycd,cm_brboffcode,bc_description, ";
                    strsql += " case when cm_sech_name <> '' and cm_thih_name <> '' then cm_sech_name + ',' + cm_thih_name when cm_sech_name <> '' then cm_sech_name when cm_thih_name <> '' then cm_thih_name else '--' end as 'Joints' ,cm_groupcd,";
                    strsql += " ltrim(rtrim(convert(char,convert(datetime,td_curdate),112))) td_curdate,td_reference,td_beneficiery,td_countercmbpid,td_clear_corpn,td_isin_code,sc_isinname,td_description,bs_description, ";
                    strsql += " sc_company_name,sc_rate,td_settlement,td_market_type,td_ac_type,td_category,td_counterdp,td_cds, ";
                    strsql += " td_narration,td_blocked,td_blockedcd, td_booking_type,td_qty,td_debit_credit, ";
                    strsql += " CASE td_debit_credit when 'C' then td_qty else 0 end 'credit', ";
                    strsql += " CASE td_debit_credit when 'D' then td_qty else 0 end 'debit', ";
                    strsql += " sc_isinname,mt_description ,nr_description 'ndesc' ,bt_description 'acdesc', ";
                    strsql += " isnull( ";
                    strsql += " ( ";
                    strsql += " select case td_category ";
                    strsql += " when '03' then ";
                    strsql += " ( ";
                    strsql += " select sum(case td_debit_credit when 'C' then td_qty else td_qty * (-1) end) ";
                    strsql += " from Trxdetail where td_ac_code = a.td_ac_code and td_isin_code = a.td_isin_code and ";
                    strsql += " td_ac_type = a.td_ac_type and td_market_type = a.td_market_type and td_settlement = ";
                    strsql += " a.td_settlement and td_Narration not in ('001') and (td_booking_type <> '02') ";
                    strsql += " and td_curdate < '" + fromDt + "' ";
                    strsql += " ) ";
                    strsql += " else ";
                    strsql += " ( ";
                    strsql += " select sum(case td_debit_credit when 'C' then td_qty else td_qty * (-1) end) ";
                    strsql += " from Trxdetail where td_ac_code = a.td_ac_code and td_isin_code = a.td_isin_code ";
                    strsql += " and td_ac_type = a.td_ac_type and td_Narration not in ('001') and (td_booking_type <> '02' Or " + " Rtrim(td_booking_type)+Rtrim(td_blockedcd) in (Select '02'+Rtrim(blc_code) From Block_code Where blc_Flag = 'B' and blc_YesNo='Y') " + ") ";
                    strsql += " and td_curdate < '" + fromDt + "' ";
                    strsql += " ) ";
                    strsql += " end ";
                    strsql += " ) ,0) 'holding', isNull(td_BillCode, '') td_BillCode ";
                    strsql += " from Trxdetail a  Left Join Market_type on ( td_market_type=mt_code and  ltrim(rtrim(isnull(td_clear_corpn,'')))=ltrim(rtrim(mt_ccid))) , ";
                    strsql += " Security  ,Client_master ,Client_Backoffice, Narration ,group_master,family_master,branch_master ,Beneficiary_type  ,Beneficiary_category ,Beneficiary_status ";
                    strsql += " where td_ac_code = cm_cd  and cm_cd=cb_cmcd  and bc_code = td_category and bs_code = cm_active ";
                    strsql += " and td_isin_code = sc_isincode ";
                    strsql += " and  td_narration = nr_code " + clientWhere + " and cm_statements <> 'N'  and   td_ac_type = bt_code";
                    strsql += " and td_Narration not in ('001')  and td_ac_type <> '30' and (td_booking_type <> '02' Or " + " Rtrim(td_booking_type)+Rtrim(td_blockedcd) in (Select '02'+Rtrim(blc_code) From Block_code Where blc_Flag = 'B' and blc_YesNo='Y') " + ")  ";
                    strsql += " and td_curdate between '" + fromDt + "' and '" + toDt + "' ";
                    strsql += " and cm_groupcd=gr_cd and cm_familycd=fm_cd and cm_brboffcode=bm_branchcd order by ";
                    strsql += " sc_company_name,td_isin_code,td_ac_type, ";
                    strsql += " case td_category when '03' then '' else td_curdate end ,td_market_type,td_settlement ";

                    DataTable dtTrans = objUtility.OpenDataTable(strsql, sqlConn);

                    if (dtTrans.Rows.Count > 0)
                    {
                        List<EstroTransactionModel> transactionResponseData = new List<EstroTransactionModel>();
                        var clients = dtTrans.AsEnumerable().Select(r => r.Field<string>("cm_cd")).Distinct().ToList();

                        foreach (var client in clients)
                        {
                            dt = dtTrans.AsEnumerable().Where(r => r.Field<string>("cm_cd") == client).CopyToDataTable();
                            string clientType = "";
                            string clientSubType = "";

                            strsql = "select bt_description  from client_master,Beneficiary_type where cm_cd= '" + dt.Rows[0]["cm_cd"].ToString().Trim() + "' and cm_clienttype=bt_code";
                            DataTable dtType = objUtility.OpenDataTable(strsql, sqlConn);
                            if (dtType.Rows.Count > 0)
                            {
                                strsql = dtType.Rows[0][0].ToString().Trim();
                                if (Strings.InStr(1, strsql, "-") > 0)
                                {
                                    clientType = Strings.Left(strsql, Strings.InStr(1, strsql, "-") - 1).Trim();
                                    clientSubType = Strings.Mid(strsql, Strings.InStr(1, strsql, "-") + 1).Trim();
                                }
                                else
                                {
                                    clientType = strsql;
                                    clientSubType = "";
                                }
                            }

                            EstroTransactionHeader transactionHeader = new EstroTransactionHeader();
                            transactionHeader.BOID = dt.Rows[0]["cm_cd"].ToString().Trim();
                            transactionHeader.BOName = dt.Rows[0]["cm_name"].ToString().Trim();
                            transactionHeader.Add1 = dt.Rows[0]["cm_add1"].ToString().Trim();
                            transactionHeader.Add2 = dt.Rows[0]["cm_add2"].ToString().Trim();
                            transactionHeader.Add3 = dt.Rows[0]["cm_add3"].ToString().Trim();
                            transactionHeader.Add4 = dt.Rows[0]["cm_add4"].ToString().Trim();
                            transactionHeader.Pin = dt.Rows[0]["cm_pin"].ToString().Trim();
                            transactionHeader.Phone = dt.Rows[0]["cm_tele1"].ToString().Trim();
                            transactionHeader.Joints = dt.Rows[0]["joints"].ToString().Trim();
                            transactionHeader.FamilyCode = dt.Rows[0]["cm_familycd"].ToString().Trim() + "(" + objUtility.fnFireQueryTradeWeb("family_master", "fm_desc", "fm_cd", dt.Rows[0]["cm_familycd"].ToString().Trim(), true, sqlConn) + ")";
                            transactionHeader.BranchCode = dt.Rows[0]["cm_brboffcode"].ToString().Trim();
                            transactionHeader.Type = clientType.Trim() + "-" + clientSubType;
                            transactionHeader.Status = dt.Rows[0]["bs_description"].ToString().Trim();
                            transactionHeader.Category = dt.Rows[0]["bc_description"].ToString().Trim();
                            transactionHeader.GroupCode = dt.Rows[0]["cm_groupcd"].ToString().Trim() + "(" + objUtility.fnFireQueryTradeWeb("group_master", "gr_desc", "gr_cd", dt.Rows[0]["cm_groupcd"].ToString().Trim(), true, sqlConn) + ")";
                            transactionHeader.From = objUtility.mfnFormatdate(fromDt, UtilityCommon.eNewDateformat.eShortDate);
                            transactionHeader.To = objUtility.mfnFormatdate(toDt, UtilityCommon.eNewDateformat.eShortDate);

                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                string strMarketType = "";
                                string strParticular = "";
                                string strSettlement = "";
                                string strNarration = dt.Rows[i]["td_narration"].ToString().Trim();
                                string strMarkeType = dt.Rows[i]["td_market_type"].ToString().Trim();
                                //string strSettlement = dt.Rows[i]["td_settlement"].ToString().Trim();

                                if (strISIN != dt.Rows[i]["td_isin_code"].ToString().Trim())
                                {
                                    strAcctype = "";
                                    strMarketType = "";
                                    strSettlement = "";
                                    strACtype = "";
                                }
                                if (dt.Rows[i]["td_category"].ToString().Trim() == "03")
                                {

                                    if (strNarration == "051" && strMarkeType != "" && dt.Rows[i]["td_settlement"].ToString().Trim() != "")
                                    {
                                        strParticular = " Pay In";
                                    }
                                    else if (strNarration == "052" && strMarkeType != "" && dt.Rows[i]["td_settlement"].ToString().Trim() != "")
                                    {
                                        urdata = objUtility.fnFindBpName(dt.Rows[i]["td_clear_corpn"].ToString().Trim(), sqlConn);
                                        strParticular = urdata.bpname;
                                    }
                                    else if (strNarration == "054" && (dt.Rows[i]["td_ac_type"].ToString().Trim() == "30" || dt.Rows[i]["td_ac_type"].ToString().Trim() == "30"))
                                    {
                                        strParticular = " Pay In";
                                    }
                                    else if (strNarration == "071" || strNarration == "072" || strNarration == "074" || strNarration == "076")
                                    {
                                        strParticular = " Intersettlement";
                                    }
                                    else if (strNarration == "114")
                                    {
                                        strParticular = "delayed Payout";
                                    }
                                    else if (strNarration == "087")
                                    {
                                        strParticular = dt.Rows[i]["ndesc"].ToString().Trim();
                                    }
                                    else if (strNarration == "061")
                                    {
                                        strParticular = dt.Rows[i]["ndesc"].ToString().Trim();
                                    }
                                    else if (strNarration == "086")
                                    {
                                        strParticular = dt.Rows[i]["ndesc"].ToString().Trim();
                                    }
                                    else if (strNarration == "204")
                                    {
                                        strParticular = "Inter depository Transfer";
                                    }
                                    else if (strNarration == "062")
                                    {
                                        strParticular = "Pay out";
                                    }
                                    else if (strNarration == "042")
                                    {
                                        urdata = objUtility.fnFindBpName(dt.Rows[i]["td_counterdp"].ToString().Trim(), sqlConn);
                                        strParticular = urdata.bpname;
                                    }
                                    else if (strNarration == "044")
                                    {
                                        urdata = objUtility.fnFindBpName(dt.Rows[i]["td_counterdp"].ToString().Trim(), sqlConn);
                                        strParticular = urdata.bpname;
                                    }
                                    else if (strNarration == "203" && dt.Rows[i]["td_debit_credit"].ToString().Trim() == "D")
                                    {
                                        strParticular = "CDS";
                                    }
                                    else if (strNarration == "307" && dt.Rows[i]["td_ac_type"].ToString().Trim() == "22")
                                    {
                                        strParticular = "Pledge Request" + objUtility.fnFindBpName(dt.Rows[i]["td_counterdp"].ToString().Trim(), sqlConn);
                                    }
                                    else if (strNarration == "307" && dt.Rows[i]["td_ac_type"].ToString().Trim() == "26")
                                    {
                                        strParticular = "Pledge Request";
                                    }
                                    else if (strNarration == "307" && dt.Rows[i]["td_ac_type"].ToString().Trim() == "29")
                                    {
                                        strParticular = "Pledge Request";
                                    }
                                    else if (strNarration == "307" && dt.Rows[i]["td_debit_credit"].ToString().Trim() == "C")
                                    {
                                        strParticular = "Inter Depository rejection/CDS";
                                    }
                                    else if (strNarration == "201" || strNarration == "202")
                                    {
                                        if (dt.Rows[i]["td_booking_type"].ToString().Trim() == "04")
                                        {
                                            strParticular = dt.Rows[i]["td_counterdp"].ToString().Trim();
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                    else if (strNarration == "211" || strNarration == "212" || strNarration == "213" || strNarration == "214")
                                    {
                                        if (dt.Rows[i]["td_countercmbpid"].ToString().Trim() == "")
                                        {
                                            strParticular = @"Pool Pool\" + dt.Rows[i]["td_clear_corpn"].ToString().Trim();
                                        }
                                        else
                                        {
                                            strParticular = @"Pool Pool\" + dt.Rows[i]["td_countercmbpid"].ToString().Trim();
                                        }
                                    }
                                    else if (strNarration == "181" || strNarration == "182" || strNarration == "183" || strNarration == "184")
                                    {
                                        if (dt.Rows[i]["td_clear_corpn"].ToString().Trim() != "")
                                        {
                                            strParticular = dt.Rows[i]["td_clear_corpn"].ToString().Trim();
                                        }
                                        else if (dt.Rows[i]["td_counterdp"].ToString().Trim() != "")
                                        {
                                            strParticular = dt.Rows[i]["td_counterdp"].ToString().Trim();
                                        }
                                        else
                                        {
                                            strParticular = dt.Rows[i]["ndesc"].ToString().Trim();
                                        }
                                    }
                                    else if (strNarration == "082")
                                    {
                                        strParticular = objUtility.mfnFindCorporateAction(dt.Rows[i]["td_isin_code"].ToString().Trim(), "082", dt.Rows[i]["td_market_type"].ToString().Trim(), dt.Rows[i]["td_settlement"].ToString().Trim(), objUtility.mfnFormatdate(dt.Rows[i]["td_curdate"].ToString().Trim(), UtilityCommon.eNewDateformat.EDATABASE), dt.Rows[i]["td_debit_credit"].ToString().Trim(), dt.Rows[i]["td_reference"].ToString().Trim(), sqlConn);
                                    }
                                    else if (strNarration == "083")
                                    {
                                        strParticular = objUtility.mfnFindCorporateAction(dt.Rows[i]["td_isin_code"].ToString().Trim(), "083", dt.Rows[i]["td_market_type"].ToString().Trim(), dt.Rows[i]["td_settlement"].ToString().Trim(), objUtility.mfnFormatdate(dt.Rows[i]["td_curdate"].ToString().Trim(), UtilityCommon.eNewDateformat.EDATABASE), dt.Rows[i]["td_debit_credit"].ToString().Trim(), dt.Rows[i]["td_reference"].ToString().Trim(), sqlConn);
                                    }
                                    else
                                    {
                                        strParticular = dt.Rows[i]["ndesc"].ToString().Trim();
                                    }
                                }
                                else
                                {
                                    if ((strNarration == "042" || strNarration == "044") && strMarkeType != "" && dt.Rows[i]["td_settlement"].ToString().Trim() != "" && (dt.Rows[i]["td_booking_type"].ToString().Trim() != "02" || dt.Rows[i]["td_booking_type"].ToString().Trim() != "03"))
                                    {
                                        if (dt.Rows[i]["td_countercmbpid"] == null || dt.Rows[i]["td_countercmbpid"].ToString().Trim() == "")
                                        {
                                            urdata = objUtility.fnFindBpName(dt.Rows[i]["td_counterdp"].ToString().Trim(), sqlConn);
                                        }
                                        else
                                        {
                                            urdata = objUtility.fnFindBpName(dt.Rows[i]["td_countercmbpid"].ToString().Trim(), sqlConn);
                                        }
                                        if (urdata.bprole == "03")
                                        {
                                            strParticular = "CM " + Strings.Left(urdata.bpname.Trim(), 25) + " ";
                                            strMarketType = objUtility.fnFireQueryTradeWeb("Market_type", "mt_description", "mt_code = '" + dt.Rows[i]["td_market_type"].ToString().Trim() + "' and ltrim(rtrim(mt_ccid)) ", dt.Rows[i]["td_clear_corpn"].ToString().Trim(), true, sqlConn) + "/" + dt.Rows[i]["td_settlement"].ToString().Trim();
                                        }
                                        else
                                        {
                                            strParticular = urdata.bpname + "/ ";
                                            if (dt.Rows[i]["td_billcode"].ToString().Trim() == "11542" || dt.Rows[i]["td_billcode"].ToString().Trim() == "11543")
                                            {
                                                strParticular = strParticular + " eDIS/Block Mechanism / " + objUtility.fnFireQueryTradeWeb("Market_type", "mt_description", "mt_code", dt.Rows[i]["td_market_type"].ToString().Trim(), false, sqlConn) + dt.Rows[i]["td_settlement"].ToString().Trim();
                                            }
                                        }
                                    }
                                    else if ((strNarration == "042" || strNarration == "044") && strMarkeType == "" && dt.Rows[i]["td_settlement"].ToString().Trim() == "")
                                    {
                                        if (dt.Rows[i]["Counterdp"].ToString().Trim() != "")
                                        {
                                            urdata = objUtility.fnFindBpName(dt.Rows[i]["Counterdp"].ToString().Trim(), sqlConn);
                                            strParticular = urdata.bpname;
                                        }
                                        else
                                        {
                                            strParticular = dt.Rows[i]["ndesc"].ToString().Trim();
                                        }
                                    }
                                    else if (strNarration == "307" && dt.Rows[i]["td_ac_type"].ToString().Trim() == "22")
                                    {
                                        urdata = objUtility.fnFindBpName(dt.Rows[i]["td_counterdp"].ToString().Trim(), sqlConn);
                                        strParticular = "Pledge Request " + urdata.bpname;
                                    }
                                    else if (strNarration == "307" && dt.Rows[i]["td_ac_type"].ToString().Trim() == "26")
                                    {
                                        strParticular = "Pledge Request";
                                    }
                                    else if (strNarration == "307" && dt.Rows[i]["td_ac_type"].ToString().Trim() == "29")
                                    {
                                        strParticular = "Pledge Request";
                                    }
                                    else if (strNarration == "091" && dt.Rows[i]["td_ac_type"].ToString().Trim() == "26")
                                    {
                                        urdata = objUtility.fnFindBpName(dt.Rows[i]["td_counterdp"].ToString().Trim(), sqlConn);
                                        strParticular = "Pledge " + urdata.bpname;
                                    }
                                    else if (strNarration == "091" && dt.Rows[i]["td_ac_type"].ToString().Trim() != "22")
                                    {
                                        urdata = objUtility.fnFindBpName(dt.Rows[i]["td_counterdp"].ToString().Trim(), sqlConn);
                                        strParticular = "Pledge " + urdata.bpname;
                                    }
                                    else if (strNarration == "092")
                                    {
                                        strParticular = "Pledge Closure";
                                    }
                                    else if (strNarration == "321" || strNarration == "309")
                                    {
                                        urdata = objUtility.fnFindBpName(dt.Rows[i]["td_counterdp"].ToString().Trim(), sqlConn);
                                        strParticular = "Pledge Request" + " " + urdata.bpname;
                                    }
                                    else if (strNarration == "093")
                                    {
                                        urdata = objUtility.fnFindBpName(dt.Rows[i]["td_counterdp"].ToString().Trim(), sqlConn);
                                        strParticular = dt.Rows[i]["ndesc"].ToString().Trim() + " " + urdata.bpname;
                                    }
                                    else if (strNarration == "201" || strNarration == "202")
                                    {
                                        if (dt.Rows[i]["td_booking_type"].ToString().Trim() == "04")
                                        {
                                            urdata = objUtility.fnFindBpName(dt.Rows[i]["td_counterdp"].ToString().Trim(), sqlConn);
                                            strParticular = urdata.bpname;
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                    else if (strNarration == "203" && dt.Rows[i]["td_booking_type"].ToString().Trim() == "02")
                                    {
                                        strParticular = "Inter Dp Rejection";
                                    }
                                    else if (strNarration == "012" && dt.Rows[i]["td_debit_credit"].ToString().Trim() == "C")
                                    {
                                        strParticular = "Dematerilization";
                                    }
                                    else if (strNarration == "012" && dt.Rows[i]["td_debit_credit"].ToString().Trim() == "D")
                                    {
                                        strParticular = "Dematerialisation request confirmation";
                                    }
                                    else if (strNarration == "011" && dt.Rows[i]["td_debit_credit"].ToString().Trim() == "C")
                                    {
                                        strParticular = "Dematerilization Request";
                                    }
                                    else if (strNarration == "204")
                                    {
                                        strParticular = "Inter Depository transfer CDS";
                                    }
                                    else if (strNarration == "231" && dt.Rows[i]["td_booking_type"].ToString().Trim() == "02")
                                    {
                                        strParticular = "Freeze [" + objUtility.fnFireQueryTradeWeb("Block_code", "blc_description", "blc_flag='" + dt.Rows[i]["td_blocked"].ToString().Trim() + "' and blc_code", dt.Rows[i]["td_blockedcd"].ToString().Trim(), true, sqlConn) + "]";
                                    }
                                    else if (strNarration == "082")
                                    {
                                        strParticular = objUtility.mfnFindCorporateAction(dt.Rows[i]["td_isin_code"].ToString().Trim(), "082", "00", "", (dt.Rows[i]["td_isin_code"] == null ? "" : objUtility.mfnFormatdate(dt.Rows[i]["td_curdate"].ToString().Trim(), UtilityCommon.eNewDateformat.EDATABASE)), dt.Rows[i]["td_debit_credit"].ToString().Trim(), dt.Rows[i]["td_reference"].ToString().Trim(), sqlConn);
                                    }
                                    else if (strNarration == "083")
                                    {
                                        strParticular = objUtility.mfnFindCorporateAction(dt.Rows[i]["td_isin_code"].ToString().Trim(), "083", "00", "", objUtility.mfnFormatdate(dt.Rows[i]["td_curdate"].ToString().Trim(), UtilityCommon.eNewDateformat.EDATABASE), dt.Rows[i]["td_debit_credit"].ToString().Trim(), dt.Rows[i]["td_reference"].ToString().Trim(), sqlConn);
                                    }
                                    else if (strNarration == "999" || (strNarration == "000" && dt.Rows[i]["td_booking_type"].ToString().Trim() == "02" && dt.Rows[i]["td_blocked"].ToString().Trim() == "B" && dt.Rows[i]["td_market_type"].ToString().Trim() != ""))
                                    {
                                        urdata = objUtility.fnFindBpName(dt.Rows[i]["td_clear_corpn"].ToString().Trim(), sqlConn);
                                        strParticular = urdata.bpname + "/" + dt.Rows[i]["mt_description"].ToString().Trim() + "/" + dt.Rows[i]["td_settlement"].ToString().Trim();
                                    }
                                    else if (dt.Rows[i]["td_billcode"].ToString().Trim() == "11551" || dt.Rows[i]["td_billcode"].ToString().Trim() == "11552")
                                    {
                                        strParticular = "Margin Re-pledge " + dt.Rows[i]["td_counterdp"].ToString().Trim() + " / ";
                                    }
                                    else if (dt.Rows[i]["td_billcode"].ToString().Trim() == "11553" || dt.Rows[i]["td_billcode"].ToString().Trim() == "11554")
                                    {
                                        strParticular = "Margin Re-pledge " + dt.Rows[i]["td_counterdp"].ToString().Trim() + " / ";
                                    }
                                    else
                                    {
                                        strParticular = dt.Rows[i]["ndesc"].ToString().Trim();
                                    }
                                }
                                string strCode = "";
                                if (dt.Rows[i]["td_category"].ToString().Trim() == "03")
                                {
                                    if (dt.Rows[i]["td_beneficiery"].ToString().Trim() != "00000000")
                                    {
                                        strCode = dt.Rows[i]["td_beneficiery"].ToString().Trim();
                                    }
                                    else if (dt.Rows[i]["td_counterdp"].ToString().Trim() == "IN000026" && dt.Rows[i]["td_cds"].ToString().Trim() != "")
                                    {
                                        strCode = dt.Rows[i]["td_cds"].ToString().Trim();
                                        if (Strings.Len(strParticular.Trim()) > 30)
                                        {
                                            strParticular = Strings.Left(strParticular.Trim(), 30) + @"\";
                                        }
                                    }
                                }
                                else
                                {
                                    if (dt.Rows[i]["td_beneficiery"].ToString().Trim() != "00000000")
                                    {
                                        if (dt.Rows[i]["td_clear_corpn"].ToString().Trim() != "")
                                        {
                                            strCode = "";
                                        }
                                        else
                                        {
                                            strCode = dt.Rows[i]["td_beneficiery"].ToString().Trim();
                                        }
                                    }
                                    else if (dt.Rows[i]["td_counterdp"].ToString().Trim() == "IN000026" && dt.Rows[i]["td_cds"].ToString().Trim() != "")
                                    {
                                        strCode = dt.Rows[i]["td_cds"].ToString().Trim();
                                        if (Strings.Len(strParticular.Trim()) > 30)
                                        {
                                            strParticular = Strings.Left(strParticular.Trim(), 30) + @"\";
                                        }
                                    }
                                    else
                                    {

                                    }
                                }

                                if (dt.Rows[i]["td_debit_credit"].ToString().Trim() == "D")
                                {
                                    strParticular = "To " + strParticular + " " + strCode + " " + strMarketType;
                                }
                                else
                                {
                                    strParticular = "By " + strParticular.Trim() + " " + strCode.Trim() + " " + strMarketType.Trim();
                                }
                                dt.Rows[i]["td_description"] = strParticular;
                            }

                            EstroTransactionModel transactionResponse = new EstroTransactionModel();
                            List<EstroTransactionData> transactionData = new List<EstroTransactionData>();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                EstroTransactionData data = new EstroTransactionData();
                                data.Date = dt.Rows[i]["td_curdate"].ToString();
                                data.ISIN = dt.Rows[i]["td_isin_code"].ToString();
                                data.ISINName = dt.Rows[i]["sc_company_name"].ToString();
                                data.Type = dt.Rows[i]["acdesc"].ToString();
                                data.Particular = dt.Rows[i]["td_description"].ToString();
                                data.Debit = Convert.ToDecimal(dt.Rows[i]["debit"]);
                                data.Credit = Convert.ToDecimal(dt.Rows[i]["credit"]);
                                data.Balance = Convert.ToDecimal(dt.Rows[i]["holding"]);
                                transactionData.Add(data);
                            }
                            transactionResponse.Header = transactionHeader;
                            transactionResponse.Data = transactionData;
                            transactionResponseData.Add(transactionResponse);
                        }

                        return transactionResponseData;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic Security_Listing(string searchBy, string searchText, string alphabet, bool blnActive)
        {
            try
            {
                strsql = "Select sc_isincode as 'ISINCode',sc_isinname as 'ISINName',sc_company_name as 'CompanyName',cast(sc_rate as decimal(15,2)) as 'Rate', ";
                strsql = strsql + "(case sc_security_status when '01' then 'Active' when '03' then 'Permanently Inactive' when '04' then 'Suspended' when '05' then 'Frozen' else 'Inactive' end) as 'Status'";
                strsql = strsql + " from Security where ";
                if (!string.IsNullOrEmpty(searchText))
                {
                    if (searchBy.ToUpper() == "C")
                    {
                        strsql += " sc_company_name like '%" + searchText + "%' ";
                    }
                    else
                    {
                        strsql += " sc_isincode like '%" + searchText.Trim() + "%' ";
                    }
                }
                else
                {
                    if (alphabet.Trim() == "*")
                    {
                        strsql = strsql + " substring(sc_company_name,1,1) not like '[A-Z]%'";
                    }
                    else
                    {
                        strsql = strsql + " sc_company_name like '" + alphabet + "%' ";
                    }
                }
                if (blnActive)
                {
                    strsql = strsql + " and sc_security_status ='01'";
                }
                DataTable dt = objUtility.OpenDataTable(strsql);
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

        public dynamic GetBills(string clientWhere, string fromDt, string toDt)
        {
            try
            {
                strsql = "select cm_cd,cm_billcode,cm_chgsscheme,cm_name, ";
                strsql = strsql + "bl_amount,bl_bill_from,bl_bill_to,bl_series,bl_bill_dt,bl_bill_no";
                strsql = strsql + " from Client_master, Billing where ";
                strsql = strsql + " cm_cd = bl_client_id " + clientWhere + " And  bl_bill_dt between '" + fromDt + "' and '" + toDt + "' ";
                DataTable dt = objUtility.OpenDataTable(strsql);
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
    }
}
