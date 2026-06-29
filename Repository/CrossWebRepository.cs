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
using TradeWeb.API.Models;

namespace TradeWeb.API.Repository
{
    public class CrossWebRepository : ICrossWebRepository
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
        public CrossWebRepository(IConfiguration configuration, UtilityCommon objUtility, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            this.objUtility = objUtility;
            _httpContextAccessor = httpContextAccessor;
            _environment = environment;
        }
        #endregion

        public dynamic GetUserDetails(string clientWhere)
        {
            try
            {
                strsql = "Select cm_cd as 'ClientCode',  cm_name as 'ClientName', cm_add1 as 'Address1', cm_add2 as 'Address2', cm_add3 as 'Address3', cm_city as 'City', cm_state as 'State', cm_country as 'Country', cm_pin as 'Pincode', " +
                        " cm_mobile as 'Mobile', cm_email as 'Email', (select bs_description from Beneficiary_status where bs_code = cm_active) as 'Status', Rtrim(cm_blsavingcd) as 'TradingCd', cb_panno as 'PanNo', Rtrim(cm_chgsscheme) as 'Scheme', isNull(cm_dateofbirth,'') as 'DOB', " +
                        " ( isnull((select (cn_NomTitle + ' ' + cn_NomName + ' ' + cn_NomMidNm + ' ' + cn_NomlastNm) from Client_NomineeDetails where cn_PurposeCd = '6' and cn_NomSrno = '1' and cn_Cmcd=cm_cd),'') + '' + " +
                        " isnull((select', ' + (cn_NomTitle + ' ' + cn_NomName + ' ' + cn_NomMidNm + ' ' + cn_NomlastNm) from Client_NomineeDetails where cn_PurposeCd = '6' and cn_NomSrno = '2' and cn_Cmcd = cm_cd),'') + '' + " +
                        " isnull((select', ' + (cn_NomTitle + ' ' + cn_NomName + ' ' + cn_NomMidNm + ' ' + cn_NomlastNm) from Client_NomineeDetails where cn_PurposeCd = '6' and cn_NomSrno = '3' and cn_Cmcd = cm_cd),'')) as NomineeName," +
                        " case when cm_sech_name <> '' and cm_thih_name <> '' then cm_sech_name + ',' + cm_thih_name when cm_sech_name <> '' then cm_sech_name when cm_thih_name <> '' then cm_thih_name else '--' end as 'Joints' ," +
                        " (select bm_branchname from Branch_master where bm_branchcd=cm_brboffcode) as 'Branch', (select gr_desc from Group_master where gr_cd=cm_groupcd) as 'Group',(select fm_desc from Family_master where fm_cd=cm_familycd) as 'Family', " +
                        " (select bt_description from Beneficiary_type where bt_code=cm_clienttype) as 'ClientType', isNull(cm_divbankacno, '') as 'BankAccountNo', isNull(cb_voicemail,'') as 'IFSC', " +
                        " isNull(cm_divbankcode,'') as 'MICR', (select bk_name from Bank_master where bk_micr = cm_divbankcode and bk_branch = cb_voicemail) as 'BankName', " +
                        " case isNull(cm_divbranchno,'') when '10' then 'SAVING Account' when '11' then 'CURRENT Account' when '13' then 'Cash Credit' else '' end as BankAcType " +
                        " from Client_master, Client_Backoffice Where cb_cmcd=cm_cd " + clientWhere;
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
        public dynamic GetHolding(string clientWhere, bool blnTradeNet = false, List<string> balanceType = null, bool showValuation = true, string asOn = "")
        {
            try
            {
                string strConn = "";
                string strHoldingType = "";
                string strTable = "";
                DataTable dt = new DataTable();
                //string strWhere = "";
                if (blnTradeNet)
                {
                    strConn = objUtility.GetDPConn("C");
                    if (strConn == null)
                    {
                        return "Cross Connection not Available";
                    }
                }
                else
                {
                    using (var db = new DataContext())
                    {
                        strConn = db.Database.GetDbConnection().ConnectionString;
                    }
                }

                if (balanceType != null)
                {
                    if (balanceType.All(y => y == ""))
                    {
                        strHoldingType = " and hld_ac_type in ( '10','11','20','30','14','12','51','13','61','50','62','63','52' )";
                    }
                    else if (balanceType.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(balanceType.ToArray(), "##"));
                        strHoldingType = " and hld_ac_type in ('" + fltr.Replace("##", "','") + "')";
                    }
                }

                if (string.IsNullOrWhiteSpace(asOn))
                {
                    strTable = "Holding";
                }
                else
                {
                    if (!blnTradeNet)
                    {
                        strTable = "Holding_" + asOn.Trim();

                        strsql = "IF EXISTS (SELECT 1 " +
                            "FROM INFORMATION_SCHEMA.TABLES " +
                            "WHERE TABLE_TYPE='BASE TABLE' " +
                            "AND TABLE_NAME='" + strTable + "') " +
                            "SELECT 1 AS res ELSE SELECT 0 AS res;";

                        dt = objUtility.OpenDataTable(strsql);
                        if (dt.Rows[0][0].ToString() == "0")
                        {
                            return "As On Holdings For This [ " + asOn.Trim() + " ] Date Is Not Found.";
                        }
                    }
                }

                using (SqlConnection sqlConn = new SqlConnection(strConn))
                {
                    if (blnTradeNet && !string.IsNullOrWhiteSpace(asOn))
                    {
                        strHoldingType = " and td_ac_type in ( '10','11','20','30','14','12','51','13','61','50','62','63','52' )";
                        try
                        {
                            if (sqlConn.State == ConnectionState.Closed)
                            {
                                sqlConn.Open();
                            }
                            strsql = "drop table #TmpHolding";
                            objUtility.ExecuteSQL(strsql, sqlConn);
                        }
                        catch (Exception ex)
                        {

                        }

                        strsql = "Create Table #TmpHolding (hld_ac_code Char(16),hld_isin_code Char(12),hld_ac_pos money,hld_ac_type char(4),hld_Rate Money,hld_hold_date char(8))"; //,hld_settlement char(13)
                        objUtility.ExecuteSQL(strsql, sqlConn);

                        strsql = "Insert into #TmpHolding(hld_ac_code,hld_isin_code,hld_ac_pos,hld_ac_type,hld_rate,hld_hold_date) "; //,hld_settlement
                        strsql += " select cm_cd ,td_isin_code,sum(case td_debit_credit when 'C' then isnull(td_qty,0) else (-1) * isnull(td_qty,0) end),td_ac_type,0,''"; //,td_settlement
                        strsql += " From TrxDetail, Client_master, Security,Branch_master";
                        strsql += " Where td_ac_code = cm_cd And td_isin_code = sc_isincode and cm_brboffcode = bm_branchcd and cm_active = '01'";
                        strsql += " and td_curdate <='" + asOn.Trim() + "' " + clientWhere + strHoldingType;
                        strsql += " Group by cm_cd ,td_ac_type ,td_isin_code,sc_isinname,cm_clienttype"; //,td_settlement
                        strsql += " having sum(case td_debit_credit when 'C' then isnull(td_qty,0) else (-1) * isnull(td_qty,0) end) > 0 ";
                        objUtility.ExecuteSQL(strsql, sqlConn);

                        strsql = "Update #TmpHolding set hld_Rate = IsNull((select rm_rate From Rate_master Where rm_isin_code = hld_isin_code and rm_trx_date = (select Max(rm_trx_date) From Rate_master Where rm_isin_code = hld_isin_code and rm_trx_date <= '" + asOn.Trim() + "')),0) ";
                        objUtility.ExecuteSQL(strsql, sqlConn);

                        strsql = "select hld_ac_code as 'ClientCode',hld_isin_code as ISIN,rtrim(sc_isinname) AS 'ISINName',rtrim(bt_code) as 'BalanceCode', bt_description as 'BalanceType', cast(round(abs(hld_ac_pos),3) as decimal (15,3)) as Quantity, ";
                        strsql += " hld_Rate as 'Rate',cast(round(abs(Sum(hld_ac_pos * hld_Rate)),2) as decimal (15,2)) as  Value "; //, rtrim(hld_settlement) as 'Settlement'
                        strsql += " From #TmpHolding ,Security ,Client_master ,Beneficiary_type,branch_master ";
                        strsql += " where cm_brboffcode = bm_branchcd  and hld_isin_code = sc_isincode And hld_ac_code = cm_cd and bt_code = hld_ac_type and cm_active = '01' ";
                        strsql += " Group By hld_ac_code,hld_hold_date,bt_code,hld_isin_code,sc_isinname,hld_ac_pos,hld_Rate,bt_description,sc_decimal_allow,hld_ac_type"; //,hld_settlement
                        strsql += " Order by rtrim(sc_isinname), hld_ac_type ";
                    }
                    else
                    {
                        strsql = "select hld_ac_code as 'ClientCode',  hld_isin_code as 'ISIN', sc_isinname as 'ISINName', bt_code as 'BalanceCode', bt_description as 'BalanceType', hld_ac_pos as 'Quantity', " + (showValuation ? "sc_rate as 'Rate',  (hld_ac_pos * sc_rate) as 'Value' ," : "") + "  hld_settlement as 'Settlement' ";
                        //strsql += " cm_brboffcode, cm_add1, cm_add2, cm_add3, cm_city, cm_pin, cm_tele1, cm_tele2, cm_tele3, cm_sech_name, cm_thih_name, bc_description ";
                        strsql += " from " + strTable + ", Beneficiary_Type , Security, Client_master, Beneficiary_category ";
                        strsql += " Where hld_ac_code = cm_cd and bt_code = hld_ac_type and bc_code = cm_acctype and hld_isin_code = sc_isincode " + clientWhere + strHoldingType + " order by hld_ac_code, sc_isinname";
                    }
                    dt = objUtility.OpenDataTable(strsql, sqlConn);

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
                //string strWhere = "";

                if (blnTradeNet)
                {
                    strConn = objUtility.GetDPConn("C");
                    if (strConn == null)
                    {
                        return "Cross Connection not Available";
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
                    strsql = "select ld_clientcd as 'ClientCode', '0' as Balance, '" + fromDt + "' as 'Date'," +
                        " '000000' as 'Voucher', " +
                        " '' as 'DocumentNo','Opening Balance' as 'Particular', sum(ld_amount) as 'Amount', case sign(sum(ld_amount)) when -1 then 'C' else 'D' end as 'DebitCreditFlag', " +
                        " Case When sum(ld_amount) > 0 Then sum(ld_amount) else 0 end Debit, " +
                        " Case When sum(ld_amount) < 0 Then abs(sum(ld_amount)) else 0 end Credit, " +
                        " '0' as 'Flag' " +
                        " From Ledger, Client_master " +
                        " Where ld_clientcd = cm_cd " + clientWhere + " and ld_dt < '" + fromDt + "' Group by ld_clientcd Having sum(ld_amount) <> 0" +
                        " Union all " +
                        " select ld_clientcd as 'ClientCode', '0' as Balance, ld_dt as 'Date', " +
                        " (ld_documenttype+''+ld_documentno) as 'Voucher', " +
                        " case ld_documentno when '0' then '' else ld_documenttype + '/' + Ltrim(Rtrim(convert(char,ld_documentno))) end 'DocumentNo', " +
                        " ltrim(rtrim(ld_particular)) as 'Particular', ld_amount as 'Amount',  ld_debitflag as 'DebitCreditFlag', " +
                        " Case When ld_amount > 0 Then ld_amount else 0 end Debit, " +
                        " Case When ld_amount < 0 Then abs(ld_amount) else 0 end Credit, " +
                        " '1' as 'Flag' " +
                        " From Ledger, Client_master " +
                        " where ld_clientcd = cm_cd " + clientWhere + " and ld_dt between '" + fromDt + "' and '" + toDt + "' order by ld_clientcd,flag,Date ";
                    dt = objUtility.OpenDataTable(strsql, sqlConn);
                }

                if (dt.Rows.Count > 0)
                {
                    var clients = dt.AsEnumerable().Select(r => r.Field<string>("ClientCode")).Distinct().ToList();
                    string strClientcd = "";
                    foreach (var client in clients)
                    {
                        double Balance = 0;
                        //DataTable dtClient = dt.AsEnumerable().Where(r => r.Field<string>("ClientCode") == client).CopyToDataTable();
                        foreach (DataRow drow in dt.Rows)
                        {
                            if (strClientcd != drow["ClientCode"].ToString().Trim())
                            {
                                Balance = 0;
                            }
                            if ((decimal)drow["Amount"] > 0)
                            {
                                Balance = Balance + Conversion.Val(drow["Amount"]);
                            }
                            else if ((decimal)drow["Amount"] < 0)
                            {
                                Balance = Balance + Conversion.Val(drow["Amount"]);
                            }

                            drow["Balance"] = Balance;
                            strClientcd = drow["ClientCode"].ToString().Trim();
                        }
                    }
                    dt.AcceptChanges();
                    return dt;
                }
                return null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetTransaction(string clientWhere, string fromDt, string toDt, bool blnTradeNet, string isin = "", List<string> transactionType = null)
        {
            try
            {
                DataTable dt = new DataTable();
                string strConn = "";
                string strTrxType = "";
                //string strIsin = "";
                //string strWhere = "";

                if (blnTradeNet)
                {
                    strConn = objUtility.GetDPConn("C");
                    if (strConn == null)
                    {
                        return "Cross Connection not Available";
                    }
                }
                else
                {
                    using (var db = new DataContext())
                    {
                        strConn = db.Database.GetDbConnection().ConnectionString;
                    }
                }

                if (transactionType != null)
                {
                    if (transactionType.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(transactionType.ToArray(), "##"));
                        strTrxType = " and td_narration in ('" + fltr.Replace("##", "','") + "') ";
                    }
                    else
                    {
                        strTrxType = " and td_narration in ('052','054','011','012','013','044','042','091','092','093','202','204','082') ";
                    }
                }

                using (SqlConnection sqlConn = new SqlConnection(strConn))
                {
                    strsql = "select cm_cd, cm_name, cm_brboffcode, cm_add1, bc_description, td_ac_code, '0' as Balance, td_isin_code, sc_isinname, bt_code, bt_description,td_curdate td_trxdate, td_reference, td_description, td_debit_credit, " +
                      " cm_add2,cm_add3,cm_city,cm_pin,cm_country,cm_blsavingcd, cm_tele1,cm_tele2,cm_tele3, cm_email, cm_sech_name,cm_thih_name,cm_acctype, " +
                      " td_qty, td_narration, td_beneficiery, td_countercmbpid, td_settlement, td_market_type, isnull(td_rejcode,'') td_rejcode, td_counterdp, td_ac_type, " +
                      " Rtrim(td_UCC) + case When Rtrim(isNull(td_segment,'')) <> '' Then  '/' + RTrim(isNull(td_segment,''))  else '' end + " +
                      " case When Rtrim(isNull(td_cmid,'')) <> '' Then  '/' + RTrim(isNull(td_cmid,'')) else '' end  + " +
                      " case When Rtrim(isNull(td_tmid,'')) <> '' Then  '/' + RTrim(isNull(td_tmid,'')) else '' end td_PledgeDesc, " +
                      " Case td_debit_credit  when 'D' then td_qty else 0 end 'debit', " +
                      " Case td_debit_credit when 'C' then td_qty else 0 end 'credit', " +
                      " isnull((select sum(case td_debit_credit when 'C' then td_qty else td_qty * (-1) end) " +
                      " From Trxdetail Where td_ac_code = a.td_ac_code and td_booking_type not in ('13')   and  td_isin_code = a.td_isin_code and Case isNull(de_SettPocket,'') When 'Y' then td_settlement else '' end  = Case isNull(de_SettPocket,'') When 'Y' then a.td_settlement else '' end and " +
                      " td_ac_type = a.td_ac_type and td_curdate < '" + fromDt + "'),0) 'holding' " +
                      " From Trxdetail a Left Join DayEnd on (td_ac_code = de_cmcd), Client_master, Security, Beneficiary_type, Beneficiary_category " +
                      " Where td_ac_code = cm_cd and bc_code = td_category and td_ac_type = bt_code and td_isin_code = sc_isincode " +
                      " " + clientWhere + "and td_booking_type not in ('13') and td_curdate between '" + fromDt + "' and '" + toDt + "' " + (isin != "" ? " and td_isin_code = '" + isin + "' " : "") +
                      " order by sc_isinname, td_isin_code, td_ac_type, case isNull(de_SettPocket,'N') when 'Y' then a.td_settlement else '' end, td_curdate, td_debit_credit, td_market_type, td_settlement ";
                    DataTable dtTrans = objUtility.OpenDataTable(strsql, sqlConn);

                    if (dtTrans.Rows.Count > 0)
                    {
                        List<CrossTransactionModel> transactionResponseData = new List<CrossTransactionModel>();
                        var clients = dtTrans.AsEnumerable().Select(r => r.Field<string>("cm_cd")).Distinct().ToList();
                        foreach (var client in clients)
                        {
                            dt = dtTrans.AsEnumerable().Where(r => r.Field<string>("cm_cd") == client).CopyToDataTable();
                            CrossTransactionHeader transactionHeader = new CrossTransactionHeader();
                            string tele = "";
                            string joints = "";
                            if (dt.Rows[0]["cm_tele1"] != null && dt.Rows[0]["cm_tele1"].ToString().Trim() != "")
                            {
                                tele += dt.Rows[0]["cm_tele1"].ToString().Trim();
                            }
                            if (dt.Rows[0]["cm_tele2"] != null && dt.Rows[0]["cm_tele2"].ToString().Trim() != "")
                            {
                                tele += "/" + dt.Rows[0]["cm_tele2"].ToString().Trim();
                            }
                            if (dt.Rows[0]["cm_tele3"] != null && dt.Rows[0]["cm_tele3"].ToString().Trim() != "")
                            {
                                tele += "/" + dt.Rows[0]["cm_tele3"].ToString().Trim();
                            }
                            joints = dt.Rows[0]["cm_sech_name"] != null && dt.Rows[0]["cm_sech_name"].ToString().Trim() != "" ? dt.Rows[0]["cm_sech_name"].ToString().Trim() : "";
                            joints += dt.Rows[0]["cm_thih_name"] != null && dt.Rows[0]["cm_thih_name"].ToString().Trim() != "" ? "," + dt.Rows[0]["cm_thih_name"].ToString().Trim() : "";
                            transactionHeader.BOID = dt.Rows[0]["cm_cd"].ToString().Trim();
                            transactionHeader.BOName = dt.Rows[0]["cm_name"].ToString().Trim();
                            transactionHeader.Add1 = dt.Rows[0]["cm_add1"].ToString().Trim();
                            transactionHeader.Add2 = dt.Rows[0]["cm_add2"].ToString().Trim();
                            transactionHeader.Add3 = dt.Rows[0]["cm_add3"].ToString().Trim();
                            transactionHeader.City = dt.Rows[0]["cm_city"].ToString().Trim();
                            transactionHeader.Pin = dt.Rows[0]["cm_pin"].ToString().Trim();
                            transactionHeader.Telephone = tele;
                            transactionHeader.Joints = joints;
                            transactionHeader.Category = dt.Rows[0]["bc_description"].ToString().Trim();
                            transactionHeader.BranchCode = dt.Rows[0]["cm_brboffcode"].ToString().Trim();
                            transactionHeader.From = objUtility.mfnFormatdate(fromDt, UtilityCommon.eNewDateformat.eShortDate);
                            transactionHeader.To = objUtility.mfnFormatdate(toDt, UtilityCommon.eNewDateformat.eShortDate);
                            string strAccType = "";
                            string strSPSettlement = "";
                            string strISIN = "";
                            string strClientSP = objUtility.fnFireQueryTradeWeb("DayEnd", "de_SettPocket", "DE_CmCd", client, true, sqlConn) == "Y" ? "Y" : "N";
                            decimal balance = 0;
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                if (strISIN != dt.Rows[i]["td_isin_code"].ToString().Trim())
                                {
                                    strSPSettlement = "";
                                    strISIN = dt.Rows[i]["td_isin_code"].ToString().Trim();
                                    strAccType = "";
                                }
                                if (strAccType != dt.Rows[i]["td_ac_type"].ToString().Trim() || (strClientSP == "Y" ? strSPSettlement != dt.Rows[i]["td_settlement"].ToString().Trim() : false))
                                {
                                    if (strClientSP == "Y")
                                    {
                                        if (strSPSettlement != dt.Rows[i]["td_settlement"].ToString().Trim())
                                        {
                                            strSPSettlement = dt.Rows[i]["td_settlement"].ToString().Trim();
                                        }
                                    }
                                    else
                                    {
                                        strSPSettlement = "";
                                    }
                                    strISIN = dt.Rows[i]["td_isin_code"].ToString().Trim();
                                    strAccType = dt.Rows[i]["td_ac_type"].ToString().Trim();

                                    if ((decimal)dt.Rows[i]["holding"] > 0)
                                    {
                                        balance = Convert.ToDecimal(string.IsNullOrEmpty(dt.Rows[i]["holding"].ToString()) ? 0 : dt.Rows[i]["holding"]);
                                        DataRow dr = dt.NewRow();
                                        dr["td_ac_code"] = dt.Rows[i]["td_ac_code"].ToString().Trim();
                                        dr["td_isin_code"] = strISIN;
                                        dr["sc_isinname"] = dt.Rows[i]["sc_isinname"].ToString().Trim();
                                        dr["bt_code"] = dt.Rows[i]["bt_code"].ToString().Trim();
                                        dr["bt_description"] = dt.Rows[i]["bt_description"].ToString().Trim();
                                        dr["td_trxdate"] = fromDt;
                                        dr["td_reference"] = "";
                                        dr["td_description"] = "Opening Balance";
                                        dr["debit"] = 0;
                                        dr["credit"] = 0;
                                        dr["balance"] = dt.Rows[i]["holding"].ToString().Trim();
                                        dt.Rows.InsertAt(dr, i);
                                    }
                                    else
                                    {
                                        balance = 0;
                                        DataRow dr = dt.NewRow();
                                        dr["td_ac_code"] = dt.Rows[i]["td_ac_code"].ToString().Trim();
                                        dr["td_isin_code"] = strISIN;
                                        dr["sc_isinname"] = dt.Rows[i]["sc_isinname"].ToString().Trim();
                                        dr["bt_code"] = dt.Rows[i]["bt_code"].ToString().Trim();
                                        dr["bt_description"] = dt.Rows[i]["bt_description"].ToString().Trim();
                                        dr["td_trxdate"] = fromDt;
                                        dr["td_reference"] = "";
                                        dr["td_description"] = "Opening Balance";
                                        dr["debit"] = 0;
                                        dr["credit"] = 0;
                                        dr["balance"] = dt.Rows[i]["holding"].ToString().Trim();
                                        dt.Rows.InsertAt(dr, i);
                                    }
                                }
                                if (dt.Rows[i]["td_debit_credit"].ToString().Trim() == "D")
                                {
                                    balance = balance - Convert.ToDecimal(string.IsNullOrEmpty(dt.Rows[i]["td_qty"].ToString()) ? 0 : dt.Rows[i]["td_qty"]);
                                }
                                else
                                {
                                    balance = balance + Convert.ToDecimal(string.IsNullOrEmpty(dt.Rows[i]["td_qty"].ToString()) ? 0 : dt.Rows[i]["td_qty"]);
                                }
                                dt.Rows[i]["balance"] = balance;
                                //dt.AcceptChanges();
                            }
                            foreach (DataRow drow in dt.Rows)
                            {
                                string strParticular = drow["td_description"].ToString().Trim();
                                string strNarration = drow["td_narration"].ToString().Trim();
                                if (strNarration == "044" || strNarration == "042")
                                {
                                    strParticular = strParticular + "/" + drow["td_beneficiery"].ToString().Trim();
                                    if (strClientSP == "Y" && isNull(drow["td_countercmbpid"]) != "" && Convert.ToString(mfnStlmntA2N(drow["td_countercmbpid"].ToString().Trim())).Length == 13)
                                    {
                                        strParticular = strParticular + "/" + mfnStlmntA2N(drow["td_countercmbpid"].ToString().Trim());
                                    }
                                    else
                                    {
                                        if (isNull(drow["td_settlement"]) != "")
                                        {
                                            if (drow["td_settlement"].ToString().Trim() != "")
                                            {
                                                strParticular = strParticular + "/" + drow["td_settlement"].ToString().Trim();
                                            }
                                        }
                                    }
                                }
                                else if (strNarration == "204")
                                {
                                    if (Strings.Left(strParticular, 5).ToUpper() == "INTER")
                                    {
                                        strParticular = "INTDEP-CR";
                                    }
                                    strParticular = strParticular + "/" + drow["td_counterdp"].ToString().Trim() + " " + drow["td_beneficiery"].ToString().Trim();
                                    if (drow["td_settlement"].ToString().Trim() != "")
                                    {
                                        strParticular = strParticular + "/" + drow["td_settlement"].ToString().Trim();
                                    }
                                }
                                else if (strNarration == "202")
                                {
                                    if (Strings.Left(strParticular, 5).ToUpper() == "INTER")
                                    {
                                        strParticular = "INTDEP-DR";
                                    }
                                    strParticular = strParticular + "/" + drow["td_counterdp"].ToString().Trim() + " " + drow["td_beneficiery"].ToString().Trim();
                                    if (drow["td_settlement"].ToString().Trim() != "")
                                    {
                                        strParticular = strParticular + "/" + drow["td_settlement"].ToString().Trim();
                                    }
                                }
                                else if (strNarration == "052")
                                {
                                    if (strClientSP == "Y" && isNull(drow["td_countercmbpid"]) != "" && Convert.ToString(mfnStlmntA2N(drow["td_countercmbpid"].ToString().Trim())).Length == 13)
                                    {
                                        strParticular = strParticular + "/" + mfnStlmntA2N(drow["td_countercmbpid"].ToString().Trim());
                                    }
                                    else
                                    {
                                        if (drow["td_settlement"].ToString().Trim() != "")
                                        {
                                            if (drow["td_settlement"].ToString().Trim().Length == 13)
                                            {
                                                string strMktTypeDesc = "";
                                                strMktTypeDesc = objUtility.fnFireQueryTradeWeb("Market_type", "mt_description", "mt_exchangeID = '" + Strings.Left(drow["td_settlement"].ToString().Trim(), 2) + "' and mt_code", drow["td_market_type"].ToString().Trim(), true, sqlConn);
                                                if (strMktTypeDesc.Trim() != "")
                                                {
                                                    strParticular = strParticular + "/" + drow["td_settlement"].ToString() + "/" + strMktTypeDesc.Trim();
                                                }
                                                else
                                                {
                                                    strParticular = strParticular + "/" + drow["td_settlement"].ToString().Trim();
                                                }
                                            }
                                            else
                                            {
                                                strParticular = strParticular + "/" + drow["td_settlement"].ToString().Trim() + "/" + objUtility.fnFireQueryTradeWeb("Market_type", "mt_description", "mt_code", drow["td_market_type"].ToString().Trim(), true, sqlConn).Trim();
                                            }
                                        }
                                    }
                                    if (drow["td_beneficiery"].ToString().Trim() != "")
                                    {
                                        strParticular = strParticular + "/" + drow["td_beneficiery"].ToString().Trim();
                                    }
                                    if (drow["td_counterdp"].ToString().Trim() != "" && drow["td_description"].ToString().Trim() == "SETTLEMENT-DR")
                                    {
                                        strParticular = strParticular + "/" + objUtility.fnFireQueryTradeWeb("Bpmaster", "bp_name", "bp_role ='01' and bp_id", drow["td_counterdp"].ToString().Trim(), true, sqlConn);
                                    }
                                }
                                else if (strNarration == "054" || strNarration == "241" || strNarration == "242")
                                {
                                    strParticular = strParticular + "/" + drow["td_settlement"].ToString().Trim();
                                    if (drow["td_beneficiery"].ToString().Trim() != "")
                                    {
                                        strParticular = strParticular + "/" + drow["td_beneficiery"].ToString().Trim();
                                    }
                                }
                                else if (strNarration == "011")
                                {
                                    strParticular = objUtility.fnFireQueryTradeWeb("Narration", "nr_description", "nr_code", drow["td_narration"].ToString().Trim(), true, sqlConn).Trim();
                                }
                                else if (strNarration == "013")
                                {
                                    strParticular = objUtility.fnFireQueryTradeWeb("Narration", "nr_description", "nr_code", drow["td_narration"].ToString().Trim(), true, sqlConn).Trim();
                                }
                                else if (strNarration == "090")
                                {
                                    if (drow["td_beneficiery"].ToString().Trim() != "")
                                    {
                                        strParticular = strParticular + "/" + drow["td_beneficiery"].ToString().Trim();
                                    }
                                }
                                else if (strNarration == "082" && drow["td_beneficiery"].ToString().Trim().ToUpper() == "Confiscate".ToUpper())
                                {
                                    if (drow["td_beneficiery"].ToString().Trim() != "")
                                    {
                                        strParticular = strParticular + "/" + drow["td_beneficiery"].ToString().Trim();
                                    }
                                }
                                else if (strNarration == "082" && drow["td_rejcode"].ToString().Trim() != "")
                                {
                                    string strDesc = "";
                                    strDesc = objUtility.fnFireQueryTradeWeb("ClientSub_Master", "cs_desc", "cs_module='CS20' and cs_code", drow["td_rejcode"].ToString().Trim(), false, sqlConn);
                                    if (strDesc.Trim() != "")
                                    {
                                        strParticular = strParticular + "/" + strDesc;
                                    }
                                }
                                else if (strNarration == "091" || strNarration == "092" || strNarration == "093" || strNarration == "094" || strNarration == "096" ||
                                    strNarration == "097" || strNarration == "098" || strNarration == "099")
                                {
                                    if (drow["td_beneficiery"].ToString().Trim() != "")
                                    {
                                        strParticular = strParticular + " /" + drow["td_beneficiery"].ToString().Trim();
                                    }
                                    if (drow["td_PledgeDesc"].ToString().Trim() != "")
                                    {
                                        strParticular = strParticular + " [" + drow["td_PledgeDesc"].ToString().Trim() + "]";
                                    }
                                }

                                if (Strings.Left(strParticular, 7).ToUpper() == "OVERDUE")
                                {
                                    strParticular = strParticular + " " + drow["td_beneficiery"].ToString().Trim();
                                }

                                if (drow["td_debit_credit"].ToString() == "D")
                                {
                                    strParticular = " To " + strParticular;
                                }
                                else if (drow["td_debit_credit"].ToString() == "C")
                                {
                                    strParticular = " By " + strParticular;
                                }
                                drow["td_description"] = strParticular;
                            }

                            List<TempCrossTransactionData> tmpTransactionData = new List<TempCrossTransactionData>();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                TempCrossTransactionData data = new TempCrossTransactionData
                                {
                                    Date = dt.Rows[i]["td_trxdate"].ToString(),
                                    TrxNo = dt.Rows[i]["td_reference"].ToString(),
                                    ISIN = dt.Rows[i]["td_isin_code"].ToString(),
                                    ISINName = dt.Rows[i]["sc_isinname"].ToString(),
                                    Type = dt.Rows[i]["bt_description"].ToString(),
                                    Particular = dt.Rows[i]["td_description"].ToString(),
                                    Debit = Convert.ToDecimal(dt.Rows[i]["debit"]),
                                    Credit = Convert.ToDecimal(dt.Rows[i]["credit"]),
                                    Balance = Convert.ToDecimal(dt.Rows[i]["Balance"])
                                };
                                tmpTransactionData.Add(data);
                            }

                            List<CrossTransactionData> root = new List<CrossTransactionData>();
                            List<string> ISINs = tmpTransactionData.Select(x => x.ISIN).Distinct().ToList();
                            //List<string> ISINNames = tmpTransactionData.Select(x => x.ISINName).Distinct().ToList();
                            int c = 0;
                            foreach (var ISIN in ISINs)
                            {
                                CrossTransactionData isinData = new CrossTransactionData();
                                List<CrossTransactionISINData> dataa = new List<CrossTransactionISINData>();
                                var brokerageDetails = tmpTransactionData.Where(x => x.ISIN == ISIN).ToList();
                                var type = brokerageDetails.Select(x => x.Type).Distinct().ToList();

                                isinData.ISIN = ISIN;
                                isinData.ISINName = brokerageDetails.FirstOrDefault().ISINName.Trim();
                                c++;
                                foreach (var typeItem in type)
                                {
                                    var data = brokerageDetails.Where(x => x.Type == typeItem).ToList();
                                    CrossTransactionISINData type2 = new CrossTransactionISINData();

                                    List<CrossTransactionTypeData> data2 = new List<CrossTransactionTypeData>();
                                    type2.Type = typeItem;
                                    foreach (var item in data)
                                    {
                                        CrossTransactionTypeData data1 = new CrossTransactionTypeData();
                                        data1.Date = item.Date;
                                        data1.TrxNo = item.TrxNo;
                                        data1.Particular = item.Particular;
                                        data1.Debit = item.Debit;
                                        data1.Credit = item.Credit;
                                        data1.Balance = item.Balance;
                                        data2.Add(data1);
                                    }
                                    type2.Data = data2;
                                    dataa.Add(type2);
                                }
                                isinData.Data = dataa;
                                root.Add(isinData);
                            }
                            CrossTransactionModel transactionResponse = new CrossTransactionModel();
                            transactionResponse.Header = transactionHeader;
                            transactionResponse.Data = root;
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
                strsql = "Select sc_isincode as 'ISIN',sc_isinname as 'ISINName',sc_company_name as 'CompanyName',cast(sc_rate as decimal(15,2)) as 'Rate', ";
                strsql = strsql + "(case sc_security_status when '01' then 'Active' when '03' then 'Permanently Inactive' when '04' then 'Suspended' when '05' then 'Frozen' else 'Inactive' end)as 'Status'";
                strsql = strsql + " from Security where ";
                if (!string.IsNullOrEmpty(searchText))
                {
                    if (searchBy.Trim() == "C")
                    {
                        strsql = strsql + " sc_company_name like '%" + searchText + "%' ";
                    }
                    else
                    {
                        strsql = strsql + " sc_isincode='" + searchText.Trim() + "'";
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

        public dynamic Bill(string clientWhere, string fromDt, string toDt, bool blnTradeNet)
        {
            try
            {
                string strConn = "";

                if (blnTradeNet)
                {
                    strConn = objUtility.GetDPConn("C");
                    if (strConn == null)
                    {
                        return "Cross Connection not Available";
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
                    string strSchedule = objUtility.fnFireQueryTradeWeb("sysparameter", "sp_sysvalue", "sp_parmcd", "CMSCHEDULE", true, sqlConn);

                    strsql = "select a.cm_cd,a.cm_name,a.cm_brboffcode,a.cm_add1,b.bl_bill_no,bl_series,b.bl_bill_dt, convert(char,convert(datetime, b.bl_bill_from),103) bl_bill_from, convert(char,convert(datetime, b.bl_bill_to),103) bl_bill_to, ";
                    strsql += " a.cm_add2,a.cm_add3,a.cm_city,a.cm_pin,a.cm_country,a.cm_blsavingcd, cm_tele1,cm_tele2,cm_tele3, a.cm_email, a.cm_sech_name,a.cm_thih_name,a.cm_acctype,";
                    strsql += " b.bl_bill_dt, convert(char,convert(datetime, b.bl_bill_from),103) bl_bill_from, convert(char,convert(datetime, b.bl_bill_to),103) bl_bill_to,";
                    strsql += " cm_groupcd , cm_familycd ,cm_chgsscheme ,cm_collectioncode,";
                    strsql += " b.bl_amount ,cm_billcycle, cm_chgsscheme, isnull(bl_INVNO,0) bl_INVNO , isnull(bl_BRSTCD,'') bl_BRSTCD  ";
                    strsql += " from Client_master a,  billing   b ";
                    strsql += " where a.cm_cd = b.bl_client_id and cm_Schedule = '" + strSchedule + "' " + clientWhere;
                    strsql += " and bl_bill_dt between '" + fromDt + "' and '" + toDt + "' ";
                    DataTable dtBills = objUtility.OpenDataTable(strsql, sqlConn);
                    if (dtBills.Rows.Count > 0)
                    {
                        List<CrossBillModel> billModelData = new List<CrossBillModel>();
                        var clients = dtBills.AsEnumerable().Select(r => r.Field<string>("cm_cd")).Distinct().ToList();
                        foreach (var client in clients)
                        {
                            DataTable dt = dtBills.AsEnumerable().Where(r => r.Field<string>("cm_cd") == client).CopyToDataTable();
                            //userId = client;
                            string tele = "";
                            string joints = "";
                            string gstInvoice = "";
                            if (dt.Rows[0]["cm_tele1"] != null && dt.Rows[0]["cm_tele1"].ToString().Trim() != "")
                            {
                                tele += dt.Rows[0]["cm_tele1"].ToString().Trim();
                            }
                            if (dt.Rows[0]["cm_tele2"] != null && dt.Rows[0]["cm_tele2"].ToString().Trim() != "")
                            {
                                tele += "/" + dt.Rows[0]["cm_tele2"].ToString().Trim();
                            }
                            if (dt.Rows[0]["cm_tele3"] != null && dt.Rows[0]["cm_tele3"].ToString().Trim() != "")
                            {
                                tele += "/" + dt.Rows[0]["cm_tele3"].ToString().Trim();
                            }
                            if (Convert.ToInt16(dt.Rows[0]["bl_INVNO"]) > 0)
                            {
                                gstInvoice = "CR/" + dt.Rows[0]["bl_BRSTCD"].ToString().Trim() + "/" + dt.Rows[0]["bl_INVNO"].ToString().Trim();
                            }
                            else
                            {
                                gstInvoice = "NA";
                            }
                            joints = dt.Rows[0]["cm_sech_name"] != null && dt.Rows[0]["cm_sech_name"].ToString().Trim() != "" ? dt.Rows[0]["cm_sech_name"].ToString().Trim() : "";
                            joints += dt.Rows[0]["cm_thih_name"] != null && dt.Rows[0]["cm_thih_name"].ToString().Trim() != "" ? "," + dt.Rows[0]["cm_thih_name"].ToString().Trim() : "";

                            CrossBillHeader billHeader = new CrossBillHeader();
                            billHeader.BOID = dt.Rows[0]["cm_cd"].ToString().Trim();
                            billHeader.BOName = dt.Rows[0]["cm_name"].ToString().Trim();
                            billHeader.Add1 = dt.Rows[0]["cm_add1"].ToString().Trim();
                            billHeader.Add2 = dt.Rows[0]["cm_add2"].ToString().Trim();
                            billHeader.Add3 = dt.Rows[0]["cm_add3"].ToString().Trim();
                            billHeader.City = dt.Rows[0]["cm_city"].ToString().Trim();
                            billHeader.Pin = dt.Rows[0]["cm_pin"].ToString().Trim();
                            billHeader.Telephone = tele;
                            billHeader.Joints = joints;
                            billHeader.GSTNo = objUtility.fnFireQueryTradeWeb("Client_GST", "Cg_GST", "Cg_cmcd", dt.Rows[0]["cm_cd"].ToString().Trim(), true, sqlConn);
                            billHeader.GSTInvoice = gstInvoice;
                            billHeader.BillDate = dt.Rows[0]["bl_series"].ToString().Trim() + "/" + objUtility.mfnFormatdate(dt.Rows[0]["bl_bill_dt"].ToString().Trim(), UtilityCommon.eNewDateformat.eShortDate);
                            billHeader.BillFrom = objUtility.mfnFormatdate(fromDt, UtilityCommon.eNewDateformat.eShortDate);
                            billHeader.BillTo = objUtility.mfnFormatdate(toDt, UtilityCommon.eNewDateformat.eShortDate);
                            billHeader.Branch = objUtility.fnFireQueryTradeWeb("branch_master", "bm_branchname", "bm_branchcd", dt.Rows[0]["cm_brboffcode"].ToString().Trim(), true, sqlConn) + "(" + dt.Rows[0]["cm_brboffcode"].ToString().Trim() + ")";

                            billHeader.TradingCd = dt.Rows[0]["cm_blsavingcd"].ToString().Trim();

                            strsql = "select td_ac_code, isnull(td_curdate,'') td_curdate, sc_isincode, sc_isinname, isnull(td_qty,0) td_qty, " +
                            " cm_cd,cm_name,cm_brboffcode,cm_add1,cm_add2,cm_add3,cm_city,cm_pin,cm_country,cm_blsavingcd, cm_tele1,cm_tele2,cm_tele3, cm_email, cm_sech_name, cm_thih_name, " +
                            " case isnull(td_debit_credit,'') when 'D' then 'Sell' else 'Buy' end 'BuySell', " +
                            " isnull(td_rate * td_qty,0) 'Value', isnull(td_amount,0) + isnull(td_cdslcharge,0)  td_amount, isnull(td_description,'') td_description, 'T' flag " +
                            " from Trxdetail, Security, Client_master " +
                            " where td_isin_code = sc_isincode and td_ac_code = cm_cd and td_ac_code = '" + client + "' and td_chargeflag = 'Y' and " +
                            " td_curdate between '" + fromDt + "' and '" + toDt + "' " +
                            " order by td_narration, td_description,td_curdate ";
                            DataTable dtBill = objUtility.OpenDataTable(strsql, sqlConn);

                            strsql = "select oc_date,oc_amt,isnull(oc_description ,'') oc_description,isnull(oc_cdslcharge,0) oc_cdslcharge, " +
                                    " isnull(oc_billno,'') as billno,case oc_chargecode when '99' then '1' when '98' then '2' when '97' then'3' else '0' end  as code, 'Additional Charges' as type, 'O' flag " +
                                    " from Other_charges " +
                                    " where oc_date between  '" + fromDt + "' and '" + toDt + "' " +
                                    " and oc_clientId = '" + client + "' and (isNull(oc_amt,0) + isNull(oc_cdslcharge,0)) <> 0 " +
                                    " order by code,oc_chargecode ";
                            DataTable dtOthercharges = objUtility.OpenDataTable(strsql, sqlConn);

                            if (dtOthercharges.Rows.Count != 0)
                            {
                                foreach (DataRow drow in dtOthercharges.Rows)
                                {
                                    DataRow dr = dtBill.NewRow();
                                    dr["td_ac_code"] = "";
                                    dr["td_curdate"] = drow["oc_date"].ToString();
                                    dr["sc_isincode"] = "";
                                    dr["sc_isinname"] = drow["oc_description"].ToString();
                                    dr["BuySell"] = "";
                                    dr["td_qty"] = 0;
                                    dr["Value"] = 0;
                                    dr["td_amount"] = drow["oc_amt"].ToString();
                                    dr["td_description"] = drow["type"].ToString();
                                    dr["flag"] = "O";
                                    dtBill.Rows.Add(dr);
                                }
                            }
                            if (dtBill.Rows.Count > 0)
                            {
                                List<CrossBillData> billData = new List<CrossBillData>();
                                for (int i = 0; i < dtBill.Rows.Count; i++)
                                {
                                    if (Convert.ToDouble(dtBill.Rows[i]["td_amount"].ToString()) > 0)
                                    {
                                        CrossBillData bill = new CrossBillData();
                                        bill.Date = dtBill.Rows[i]["td_curdate"].ToString();
                                        bill.ISIN = dtBill.Rows[i]["sc_isincode"].ToString();
                                        bill.Security = dtBill.Rows[i]["sc_isinname"].ToString();
                                        bill.BuySell = dtBill.Rows[i]["BuySell"].ToString();
                                        bill.Quantity = Convert.ToDouble(dtBill.Rows[i]["td_qty"].ToString());
                                        bill.Value = Convert.ToDouble(dtBill.Rows[i]["Value"].ToString());
                                        bill.Charges = Convert.ToDouble(dtBill.Rows[i]["td_amount"].ToString());
                                        bill.Description = dtBill.Rows[i]["td_description"].ToString();
                                        bill.Flag = dtBill.Rows[i]["flag"].ToString();
                                        billData.Add(bill);
                                    }
                                }
                                CrossBillModel billModel = new CrossBillModel();
                                billModel.Header = billHeader;
                                billModel.Data = billData;

                                billModelData.Add(billModel);
                            }
                        }
                        return billModelData;

                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public double mfnStlmntA2N(string strAlp)
        {
            double intRtn;
            double mult;
            string strA;
            intRtn = 0;
            mult = 1;

            intRtn = 0;
            mult = 1;
            while (Strings.Len(Strings.RTrim(strAlp)) > 0)
            {
                strA = Strings.Mid(strAlp, Strings.Len(Strings.RTrim(strAlp)), 1);
                intRtn = intRtn + ((Strings.Asc(strA) - 65) * mult);
                mult = mult * 55;
                strAlp = Strings.Mid(strAlp, 1, Strings.Len(Strings.RTrim(strAlp)) - 1);
            }
            return intRtn;
        }
        public string isNull(object objvalue, int intlen = 0)
        {
            if (objvalue == null)
            {
                return "";
            }
            else
            {
                string value = objvalue.ToString();
                if (String.IsNullOrEmpty(value))
                {
                    return "";
                }
                else
                {
                    if (intlen == 0)
                    {
                        return value.Trim();
                    }
                    else
                    {
                        return Strings.Left(value.Trim(), intlen).Trim();
                    }
                }
            }
        }
    }
}
