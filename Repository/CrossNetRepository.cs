using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using TradeWeb.API.Data;
using TradeWeb.API.Models;

namespace TradeWeb.API.Repository
{
    public class CrossNetRepository : ICrossNetRepository
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
        public CrossNetRepository(IConfiguration configuration, UtilityCommon objUtility, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            this.objUtility = objUtility;
            _httpContextAccessor = httpContextAccessor;
            _environment = environment;
        }
        #endregion

        public dynamic GetFilterSql(Filter filter)
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

        public dynamic OffMarketAdd(string userId, CrossOffMarketReq req)
        {
            try
            {
                DataTable Ds;
                var db = new DataContext();
                SqlTransaction objTrans;
                string strChId, ReceiveMode, MkrDate, CurrentTime, secType, InstCd, strAuth, strRate, strSearch, strCounterSett;
                int PrimaryKey;
                bool blnIsUpdated = false;
                double StampDuty;
                MkrDate = DateTime.Now.ToString("yyyyMMdd");
                CurrentTime = DateTime.Now.ToString("hh:mm:ss");
                int instcd = Convert.ToInt32(req.InstrumentType);
                double CheqNo = Conversion.Val(req.InternalRefNo);
                #region Data List
                List<string> ListBoid = req.Data.Select(x => x.ClientID).ToList();
                List<string> Listisin = req.Data.Select(x => x.ISIN).ToList();
                List<double> ListQty = req.Data.Select(x => x.Qty).ToList();
                List<string> ListFromSettNo = req.Data.Select(x => x.FromSettNo).ToList();
                List<string> ListCounterSettNo = req.Data.Select(x => x.CounterSettNo).ToList();
                List<string> ListRemark = req.Data.Select(x => x.Remarks).ToList();
                List<string> ListReason = req.Data.Select(x => x.Reason).ToList();
                List<string> ListPaymentMode = req.Data.Select(x => x.PaymentMode).ToList();
                List<string> ListPayeeName = req.Data.Select(x => x.PayeeName).ToList();
                List<string> ListChequeNo = req.Data.Select(x => x.ChequeOrRefNo).ToList();
                List<string> ListDateOfIssue = req.Data.Select(x => x.DateOfIssue).ToList();
                List<string> ListBankAccNo = req.Data.Select(x => x.BankAccountNo).ToList();
                List<string> ListBankName = req.Data.Select(x => x.BankName).ToList();
                List<string> ListBranchName = req.Data.Select(x => x.BranchName).ToList();
                List<string> ListConsideration = req.Data.Select(x => x.Consideration).ToList();
                List<string> ListPaidBy = req.Data.Select(x => x.PaidBy).ToList();
                List<string> ListExchange = req.Data.Select(x => x.Exchange).ToList();
                List<string> ListSegment = req.Data.Select(x => x.Segment).ToList();
                List<string> ListUCC = req.Data.Select(x => x.UCC).ToList();
                List<string> ListCMid = req.Data.Select(x => x.CMId).ToList();
                List<string> ListEntryBy = req.Data.Select(x => x.EntryBy).ToList();
                List<string> ListTMid = req.Data.Select(x => x.TMID).ToList();
                #endregion
                if (OffMarketValidation(req.InternalRefNo, req.ClientID, req.TransectionDate, req.ExecDate, req.TransectionType, req.BranchCode, req.ReceiveMode, req.InstrumentType, req.TransectionType) == "Valid" || OffMarketValidation(req.InternalRefNo, req.ClientID, req.TransectionDate, req.ExecDate, req.TransectionType, req.BranchCode, req.ReceiveMode, req.InstrumentType, req.TransectionType) == "Internal Ref No Already Present")
                {
                    if (mfnSlipCheck(req.ClientID, req.TransectionType, instcd, CheqNo) != "Valid")
                    {
                        return mfnSlipCheck(req.ClientID, req.TransectionType, instcd, CheqNo);
                    }
                    using (SqlConnection sqlCon = new SqlConnection((db.Database.GetDbConnection()).ConnectionString))
                    {
                        sqlCon.Open();
                        objTrans = sqlCon.BeginTransaction();
                        SqlDataAdapter sqlDtAdap = new SqlDataAdapter();
                        SqlCommandBuilder sqlCmdBld = new SqlCommandBuilder(sqlDtAdap);

                        if (OffMarketValidation(req.InternalRefNo, req.ClientID, req.TransectionDate, req.ExecDate, req.TransectionType, req.BranchCode, req.ReceiveMode, req.InstrumentType, req.TransectionType) == "Internal Ref No Already Present")
                        {
                            strsql = "delete from Trxbackoffice where tb_internal_refno = '" + req.InternalRefNo + "'";
                            objUtility.ExecuteSQL(strsql, sqlCon, objTrans);
                            blnIsUpdated = true;
                        }

                        for (int i = 0; i < req.Data.Count; i++)
                        {
                            if (OffMarketValidation(req.ClientID, ListReason[i], ListPaymentMode[i], ListPaidBy[i], ListEntryBy[i], ListBoid[i], Listisin[i], ListQty[i], ListCounterSettNo[i], ListFromSettNo[i], ListExchange[i], ListSegment[i], ListUCC[i], ListCMid[i], ListTMid[i], sqlDtAdap, sqlCon, objTrans) != "Valid")
                            {
                                return OffMarketValidation(req.ClientID, ListReason[i], ListPaymentMode[i], ListPaidBy[i], ListEntryBy[i], ListBoid[i], Listisin[i], ListQty[i], ListCounterSettNo[i], ListFromSettNo[i], ListExchange[i], ListSegment[i], ListUCC[i], ListCMid[i], ListTMid[i], sqlDtAdap, sqlCon, objTrans);
                            }
                            ReceiveMode = fnReceiveMode(req.ReceiveMode.Trim(), "OffMarket");
                            strCounterSett = ListCounterSettNo[i];

                            InstCd = req.InstrumentType;

                            strsql = "select MAX(tb_pri_key) from Trxbackoffice";
                            Ds = objUtility.OpenDataTable(strsql, sqlCon, objTrans);
                            PrimaryKey = Convert.ToInt32(Ds.Rows[0][0].ToString()) + 1;

                            strsql = "select sc_security_type from Security where sc_isincode = '" + Listisin[i] + "'";
                            secType = objUtility.OpenDataTable(strsql, sqlCon, objTrans).Rows[0][0].ToString().Trim();
                            StampDuty = 0;
                            StampDuty = fnStampDutyRate(secType);
                            StampDuty = Convert.ToDouble(string.Format("{0:#.00}", Conversion.Val(ListConsideration[i]) * StampDuty / 100));

                            strAuth = GetSysParm("FILE_AUTH");
                            strsql = "select sc_rate from Security where sc_isincode = '" + Listisin[i].Trim() + "'";
                            strRate = objUtility.OpenDataTable(strsql, sqlCon, objTrans).Rows[0][0].ToString().Trim();
                            strsql = "select count(*) from Auth_master where am_code = '904' and am_amount <= " + strRate + " * " + ListQty[i];
                            strSearch = objUtility.OpenDataTable(strsql, sqlCon, objTrans).Rows[0][0].ToString().Trim();

                            if (ListCounterSettNo[i].Trim() == "11")
                            {
                                strChId = "10";
                            }
                            else
                            {
                                strChId = "11";
                            }

                            strsql = "select * from Trxbackoffice where tb_internal_refno = '" + req.InternalRefNo.Trim() + "'";
                            DataSet dsOffMarket = objUtility.OpenDataSet(sqlDtAdap, strsql, sqlCon, objTrans);
                            DataRow drow;
                            drow = dsOffMarket.Tables[0].NewRow();

                            drow["tb_pri_key"] = PrimaryKey;
                            drow["tb_internal_refno"] = req.InternalRefNo;
                            drow["tb_instcd"] = InstCd;
                            drow["tb_trx_type"] = req.TransectionType;
                            drow["tb_trx_date"] = req.TransectionDate;
                            drow["tb_trx_flag"] = "A";
                            drow["tb_client_id"] = req.ClientID;
                            drow["tb_isin"] = Listisin[i];
                            drow["tb_qty"] = ListQty[i];
                            drow["tb_other_client_id"] = ListBoid[i];
                            drow["mkrid"] = userId.ToUpper();
                            drow["mkrdt"] = "";
                            drow["tb_exec_date"] = req.ExecDate;
                            drow["tb_status"] = "01";
                            drow["tb_remark"] = ListRemark[i].ToUpper();
                            drow["mkrtm"] = "";
                            drow["tb_instreceivemode"] = ReceiveMode;
                            drow["tb_recoslipyn"] = "N";
                            drow["tb_entrymode"] = "E";
                            drow["tb_EarlyPayIden"] = "";
                            drow["tb_branchcd"] = req.BranchCode.ToUpper();
                            drow["tb_authdt1"] = "";
                            drow["tb_authdt2"] = "";

                            if (ListCounterSettNo[i] != "")
                            {
                                drow["tb_settlement"] = ListCounterSettNo[i];
                                drow["tb_other_settle_no"] = ListFromSettNo[i];
                                drow["tb_UCCEXid"] = ListExchange[i];
                                drow["tb_SegmentID"] = ListSegment[i].ToUpper();
                                drow["tb_UCC"] = ListUCC[i].ToUpper();
                                drow["tb_UCCCmid"] = ListCMid[i].ToUpper();
                                drow["tb_EntityIden"] = ListEntryBy[i].ToUpper();
                                drow["tb_UCCTMCPCode"] = ListTMid[i].ToUpper();
                                drow["tb_market_type"] = "";
                                drow["tb_exchangeid"] = "";
                                drow["tb_chid"] = strChId;
                            }
                            else
                            {
                                drow["tb_settlement"] = null;
                                drow["tb_other_settle_no"] = "";
                                drow["tb_UCCEXid"] = "";
                                drow["tb_SegmentID"] = "";
                                drow["tb_UCC"] = "";
                                drow["tb_UCCCmid"] = "";
                                drow["tb_EntityIden"] = "";
                                drow["tb_UCCTMCPCode"] = "";
                                drow["tb_market_type"] = null;
                                drow["tb_exchangeid"] = null;
                                drow["tb_chid"] = null;
                            }
                            if (strAuth == "N")
                            {
                                drow["tb_trx_allow"] = "Y";
                                drow["tb_authcode1"] = "N";
                                drow["tb_authcode2"] = "N";
                                drow["tb_authcode3"] = "N";
                                drow["tb_authuserid1"] = "";
                                drow["tb_authuserid2"] = "";
                                drow["tb_authuserid3"] = "";
                                drow["tb_authtm1"] = "00:00:00";
                                drow["tb_authtm2"] = "00:00:00";
                                drow["tb_authtm3"] = "00:00:00";
                                drow["tb_cash"] = "X";
                            }
                            else
                            {
                                if (Conversion.Val(strSearch) != 0)
                                {
                                    drow["tb_trx_allow"] = "N";
                                    drow["tb_authcode1"] = "Y";
                                    drow["tb_authcode2"] = "Y";
                                    drow["tb_authcode3"] = "Y";
                                    drow["tb_authuserid1"] = "";
                                    drow["tb_authuserid2"] = "";
                                    drow["tb_authuserid3"] = "";
                                    drow["tb_authtm1"] = "00:00:00";
                                    drow["tb_authtm2"] = "00:00:00";
                                    drow["tb_authtm3"] = "00:00:00";
                                    drow["tb_cash"] = "X";
                                }
                                else
                                {
                                    drow["tb_trx_allow"] = "Y";
                                    drow["tb_authcode1"] = "N";
                                    drow["tb_authcode2"] = "N";
                                    drow["tb_authcode3"] = "N";
                                    drow["tb_authuserid1"] = "";
                                    drow["tb_authuserid2"] = "";
                                    drow["tb_authuserid3"] = "";
                                    drow["tb_authtm1"] = "00:00:00";
                                    drow["tb_authtm2"] = "00:00:00";
                                    drow["tb_authtm3"] = "00:00:00";
                                    drow["tb_cash"] = "X";
                                }
                            }
                            if (ListReason[i] == "2")
                            {
                                if (ListPaymentMode[i] == "1" || ListPaymentMode[i] == "2")
                                {
                                    drow["tb_reasfortrade"] = ListReason[i];
                                    drow["tb_PaymentMode"] = ListPaymentMode[i];
                                    drow["tb_PayeeName"] = ListPayeeName[i].ToUpper();
                                    drow["tb_ChequeNo"] = ListChequeNo[i].ToUpper();
                                    drow["tb_Paymentdate"] = ListDateOfIssue[i];
                                    drow["tb_BankActNo"] = ListBankAccNo[i].ToUpper();
                                    drow["tb_Bankname"] = ListBankName[i].ToUpper();
                                    drow["tb_BankBranch"] = ListBranchName[i].ToUpper();
                                    drow["tb_Consideration"] = ListConsideration[i];
                                    drow["tb_NFiller1"] = StampDuty;
                                    drow["tb_Filler1"] = ListPaidBy[i];
                                }
                                else
                                {
                                    drow["tb_reasfortrade"] = ListReason[i];
                                    drow["tb_PaymentMode"] = ListPaymentMode[i];
                                    drow["tb_PayeeName"] = ListPayeeName[i].ToUpper();
                                    drow["tb_ChequeNo"] = "";
                                    drow["tb_Paymentdate"] = ListDateOfIssue[i];
                                    drow["tb_BankActNo"] = "";
                                    drow["tb_Bankname"] = "";
                                    drow["tb_BankBranch"] = "";
                                    drow["tb_Consideration"] = ListConsideration[i];
                                    drow["tb_NFiller1"] = StampDuty;
                                    drow["tb_Filler1"] = ListPaidBy[i];
                                }
                            }
                            else
                            {
                                drow["tb_reasfortrade"] = ListReason[i];
                                drow["tb_PaymentMode"] = 0;
                                drow["tb_PayeeName"] = "";
                                drow["tb_ChequeNo"] = "";
                                drow["tb_Paymentdate"] = "";
                                drow["tb_BankActNo"] = "";
                                drow["tb_Bankname"] = "";
                                drow["tb_BankBranch"] = "";
                                drow["tb_Consideration"] = 0;
                                drow["tb_NFiller1"] = 0;
                                drow["tb_Filler1"] = "";
                            }
                            dsOffMarket.Tables[0].Rows.Add(drow);
                            sqlDtAdap.Update(dsOffMarket);
                        }
                        sqlDtAdap.Dispose();
                        mfnInsertUsed_slip(sqlCon, objTrans, req.TransectionType, req.InstrumentType, req.InternalRefNo, req.ExecDate, "A", req.ClientID, "", userId.ToUpper(), MkrDate, CurrentTime);
                        objTrans.Commit();
                        strsql = "update Trxbackoffice set mkrdt = CONVERT(varchar, GETDATE(), 112),mkrtm = CONVERT(TIME, GETDATE()) where tb_internal_refno = '" + req.InternalRefNo + "'";
                        objUtility.ExecuteSQL(strsql);

                        if (blnIsUpdated)
                        {
                            return "Record Updated.";
                        }
                        return "Record Inserted.";
                    }
                }
                else
                {
                    return OffMarketValidation(req.InternalRefNo, req.ClientID, req.TransectionDate, req.ExecDate, req.TransectionType, req.BranchCode, req.ReceiveMode, req.InstrumentType, req.TransectionType);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic OffMarketFind(string InstrumentType, string InternalRefNo, string TransectionType)
        {
            try
            {
                if (FindOffMarketValidation(InstrumentType, InternalRefNo) == "Valid")
                {
                    SqlTransaction ObjTrans;
                    DataTable Dt;
                    string ClientCd, PaymentMode, PaidBy, Bo_id, Exch_Desc, Seg_Desc;
                    OffMarketFindResponce objResp = new OffMarketFindResponce();

                    strsql = "select * from Trxbackoffice where tb_internal_refno = '" + InternalRefNo + "' and tb_instcd = '" + InstrumentType + "'";
                    DataTable Dss = objUtility.OpenDataTable(strsql);
                    if (Dss.Rows.Count == 0)
                    {
                        return "Invalid Internal Reference No.";
                    }

                    strsql = "select * from Trxbackoffice where tb_internal_refno = '" + InternalRefNo + "' and tb_instcd = '" + InstrumentType + "' and tb_trx_type = '" + TransectionType + "'";
                    Dt = objUtility.OpenDataTable(strsql);
                    if (Dt.Rows.Count == 0)
                    {
                        return "Record not found";
                    }
                    objResp.TransectionDate = Dt.Rows[0]["tb_trx_date"].ToString().Trim();
                    objResp.ExecDate = Dt.Rows[0]["tb_exec_date"].ToString().Trim();
                    objResp.InternalRefNo = InternalRefNo;
                    ClientCd = Dt.Rows[0]["tb_client_id"].ToString().Trim();
                    objResp.ClientID = ClientCd;
                    Bo_id = Dt.Rows[0]["tb_other_client_id"].ToString().Trim();
                    objResp.BranchCode = Dt.Rows[0]["tb_branchcd"].ToString().Trim();
                    objResp.InstrumentTypeCode = InstrumentType;

                    strsql = "select bm_branchname from Branch_master where bm_branchcd = '" + Dt.Rows[0]["tb_branchcd"].ToString().Trim() + "'";
                    Dt = objUtility.OpenDataTable(strsql);
                    objResp.BranchName = Dt.Rows[0][0].ToString().Trim();

                    strsql = "select cm_name from Client_master where cm_cd = '" + ClientCd + "'";
                    Dt = objUtility.OpenDataTable(strsql);
                    objResp.ClientName = Dt.Rows[0][0].ToString().Trim();

                    strsql = "select im_desc from Instrument_master where im_instcd = (select distinct tb_instcd from Trxbackoffice where tb_internal_refno = '" + InternalRefNo + "')";
                    Dt = objUtility.OpenDataTable(strsql);
                    objResp.InstrumentType = Dt.Rows[0][0].ToString().Trim();

                    string ConnectionString = objUtility.GetConnectionStr();
                    using (SqlConnection Sqlcon = new SqlConnection(ConnectionString))
                    {
                        Sqlcon.Open();
                        ObjTrans = Sqlcon.BeginTransaction();
                        SqlCommand cmd = Sqlcon.CreateCommand();
                        cmd.Transaction = ObjTrans;
                        SqlDataAdapter sqlDtAdap = new SqlDataAdapter();
                        SqlCommandBuilder sqlCmdBld = new SqlCommandBuilder(sqlDtAdap);

                        TempOffMarketTable(Sqlcon, ObjTrans);

                        strsql = "insert into #TempOffMraket (Transection_Id,BO_Id,ISIN_Code,Qty,Cash,CounterSett,Flag,Auth,Trade_No,Remark,Time,FromSett,Payment_Mode_Code,PayeeName,Cheque_No,DateOfIssue,BankAccNo,Bank_Name,Branch_Name,Consideration,Stamp_Duty,Paid_By_Code,Exchange,Segment,UCC,CM_Id,Entry_By,TM_Id)  " +
                            "(select tb_pri_key , tb_other_client_id, tb_isin, tb_qty," +
                            "case (tb_cash)when 'X' then 'NONE' end, tb_settlement, tb_trx_allow, tb_authcode1, tb_tradeno, tb_remark, mkrtm, tb_other_settle_no,tb_PaymentMode,tb_PayeeName,tb_ChequeNo,tb_Paymentdate,tb_BankActNo,tb_Bankname,tb_BankBranch,tb_consideration,tb_NFiller1,tb_Filler1,tb_UCCEXid,tb_SegmentID,tb_UCC,tb_UCCCmid,tb_EntityIden,tb_UCCTMCPCode " +
                            "from Trxbackoffice where tb_internal_refno = '" + InternalRefNo.Trim() + "' and tb_instcd = '" + InstrumentType + "' and tb_trx_type = '" + TransectionType + "')";
                        objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                        strsql = "select * from #TempOffMraket";
                        Dt = objUtility.OpenDataTable(strsql, Sqlcon, ObjTrans);

                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            Exch_Desc = Dt.Rows[i]["Exchange"].ToString().Trim();
                            Seg_Desc = Dt.Rows[i]["Segment"].ToString().Trim();
                            strsql = "update #TempOffMraket set ISIN_Name = (select sc_isinname from Security where sc_isincode = '" + Dt.Rows[i]["ISIN_Code"].ToString().Trim() + "') where Transection_Id = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "'"; //ISIN Name
                            objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                            strsql = "update #TempOffMraket set DP_Name = (select bp_name from Bpmaster where bp_id = '" + Bo_id.Substring(3, 5) + "')";
                            objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                            strsql = "update #TempOffMraket set BOId_Name = (select cm_name from Client_master where cm_cd = '" + Dt.Rows[i]["BO_Id"].ToString().Trim() + "')";
                            objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                            strsql = "update #TempOffMraket set Rate = (select sc_rate from Security where sc_isincode = '" + Dt.Rows[i]["ISIN_Code"].ToString().Trim() + "') where Transection_Id = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "'"; //Rate
                            objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                            strsql = "update #TempOffMraket set Value = (Qty * Rate) where Transection_Id = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "'"; //Value
                            objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                            strsql = "update #TempOffMraket set Reason_Code = (select tb_reasfortrade from Trxbackoffice where tb_internal_refno = '" + InternalRefNo.Trim() + "' and tb_pri_key = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "')";
                            objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                            strsql = "update #TempOffMraket set Reason = (select rt_desc from Reasonfortrade where rt_code = " +
                                "(select tb_reasfortrade from Trxbackoffice where tb_internal_refno = '" + InternalRefNo.Trim() + "' and tb_pri_key = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "')) " +
                                "where Transection_Id = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "'"; //Reason
                            objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                            strsql = "select sx_description from statusof_trx where sx_code = (select distinct tb_status from Trxbackoffice where tb_internal_refno = '" + InternalRefNo.Trim() + "' and tb_pri_key = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "')";
                            DataTable Ds = objUtility.OpenDataTable(strsql, Sqlcon, ObjTrans);

                            strsql = "update #TempOffMraket set Status = '" + Ds.Rows[0][0].ToString().Trim() + "' " +
                                "where Transection_Id = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "'"; //Status
                            objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                            strsql = "select tb_PaymentMode,tb_Filler1 from Trxbackoffice where tb_internal_refno = '" + InternalRefNo.Trim() + "'";
                            PaymentMode = objUtility.OpenDataTable(strsql, Sqlcon, ObjTrans).Rows[0]["tb_PaymentMode"].ToString().Trim();

                            //strsql = "select tb_Filler1 from Trxbackoffice where tb_internal_refno = '" + InternalRefNo.Trim() + "'";
                            PaidBy = objUtility.OpenDataTable(strsql, Sqlcon, ObjTrans).Rows[0]["tb_Filler1"].ToString().Trim();

                            PaymentMode = fnPaymentMode(PaymentMode);
                            PaidBy = fnPaidBy(PaidBy);
                            Exch_Desc = fnExchangeName(Exch_Desc);
                            Seg_Desc = fnSegmentName(Seg_Desc);

                            strsql = "update #TempOffMraket set Payment_Mode_Desc = '" + PaymentMode + "', Paid_By_Desc = '" + PaidBy + "', Exchange_Desc = '" + Exch_Desc + "', Segment_Desc = '" + Seg_Desc + "' where Transection_Id = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "'";
                            objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);
                        }
                        strsql = "select * from #TempOffMraket";
                        Dt = objUtility.OpenDataTable(strsql, Sqlcon, ObjTrans);

                        foreach (DataColumn col in Dt.Columns)
                        {
                            foreach (DataRow row in Dt.Rows)
                            {
                                if (row.IsNull(col))
                                {
                                    strsql = "update #TempOffMraket set " + col + " = ''";
                                    objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);
                                }
                            }
                        }

                        Dt = fnRTrimTable(Sqlcon, ObjTrans);
                        objResp.Data = Dt;
                        return objResp;
                    }
                }
                else
                {
                    return FindOffMarketValidation(InstrumentType, InternalRefNo);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region Off Market Functions
        public string FindOffMarketValidation(string InstrumentType, string InternalRefNo)
        {
            DataTable ds;
            if (InternalRefNo != "") //internal ref no
            {
                strsql = "select COUNT(*) from Trxbackoffice where tb_internal_refno = '" + InternalRefNo + "'";
                ds = objUtility.OpenDataTable(strsql);
                if (Convert.ToInt16(ds.Rows[0][0].ToString()) == 0)
                {
                    return "Please Enter Valid Internal Ref No.";
                }
                strsql = "select * from Trxbackoffice where tb_internal_refno ='" + InternalRefNo + "' and tb_instcd = '" + InstrumentType + "' and tb_trx_type = '904'";
                ds = objUtility.OpenDataTable(strsql);
                if (ds.Rows.Count == 0)
                {
                    return "Internal Ref No not found";
                }
            }
            if (InternalRefNo == "")
            {
                return "Please Enter Internal Ref No.";
            }
            if (InstrumentType != "")
            {
                strsql = "select COUNT(*) from Instrument_master where im_instcd = " + InstrumentType;
                ds = objUtility.OpenDataTable(strsql);
                if (Convert.ToInt16(ds.Rows[0][0].ToString()) == 0)
                {
                    return "Please Enter Valid Instrument Type";
                }
            }
            else
            {
                return "Please Enter Instrument Type";
            }
            return "Valid";
        }
        public string fnReceiveMode(string idx, string Mode)
        {
            if (Mode == "OffMarket")
            {
                if (idx == "0")
                {
                    return "S";
                }
                if (idx == "1")
                {
                    return "F";
                }
                if (idx == "2")
                {
                    return "T";
                }
                if (idx == "3")
                {
                    return "O";
                }
                if (idx == "4")
                {
                    return "E";
                }
                if (idx == "5")
                {
                    return "V";
                }
                if (idx == "6")
                {
                    return "P";
                }
                if (idx == "7")
                {
                    return "C";
                }
                if (idx == "8")
                {
                    return "H";
                }
                if (idx == "9")
                {
                    return "D";
                }
                if (idx == "10")
                {
                    return "0";
                }
                if (idx == "11")
                {
                    return "1";
                }
            }
            else if (Mode == "InterDepository")
            {
                if (idx == "0")
                {
                    return "S";
                }
                if (idx == "1")
                {
                    return "F";
                }
                if (idx == "2")
                {
                    return "T";
                }
                if (idx == "3")
                {
                    return "O";
                }
                if (idx == "4")
                {
                    return "E";
                }
                if (idx == "5")
                {
                    return "V";
                }
                if (idx == "6")
                {
                    return "P";
                }
                if (idx == "7")
                {
                    return "C";
                }
                if (idx == "8")
                {
                    return "H";
                }
                if (idx == "9")
                {
                    return "D";
                }
            }
            return "S";
        }
        public string OffMarketValidation(string InternalRefNo, string clientCd, string TrxDate, string ExecDate, string Type, string BranchCd, string ReceiveMode, string InstrumentType, string TransectionType)
        {
            // For only once
            DataTable ds;
            DataTable rsCheck1;
            DataTable rstemp;
            string strSlipmnt = "", strPROMPTSEQ, strInwardentry, strSlipMaintanace, strInwardstatus;
            int intiPos;
            if (InternalRefNo != "") //internal ref no
            {
                strsql = "select COUNT(*) from Trxbackoffice where tb_internal_refno = '" + InternalRefNo + "'";
                ds = objUtility.OpenDataTable(strsql);
                if (Conversion.Val(ds.Rows[0][0].ToString().Trim()) > 0)
                {
                    return "Internal Ref No Already Present";
                }
                if (InternalRefNo == "0")
                {
                    return "Zero can't be an Internal Ref No";
                }

                strInwardstatus = "N";
                strInwardentry = Strings.Trim(Strings.UCase(GetSysParm("INWARDENTRY")));
                if (strInwardentry != "")
                {
                    intiPos = Strings.InStr(strInwardentry, TransectionType);
                    if (intiPos > 0)
                    {
                        strInwardstatus = Strings.Mid(strInwardentry, intiPos + 4, 1);
                    }
                }
                SlipCheck et = new SlipCheck();
                et.strInwardstatus = strInwardstatus;

                if (strInwardstatus == "A" | strInwardstatus == "O")
                {

                    strsql = "select ie_cmcd,cm_name,ie_lotno,ie_slipno,ie_nooftrx,ie_rejected,ie_mode,";
                    strsql = strsql + " lz_status,ie_execdt, cm_acctype, cm_brboffcode from Inward_entry,Lot_size, ";
                    strsql = strsql + " Instrument_master,Client_master where ie_cmcd = cm_cd ";
                    strsql = strsql + " and ie_lotno = lz_lotno   and ie_trxtype = lz_type ";
                    strsql = strsql + " and ie_instno = im_instcd  ";
                    strsql = strsql + " and ie_slipno = '" + InternalRefNo + "'";
                    strsql = strsql + " and im_instcd = " + InstrumentType;
                    strsql = strsql + " and ie_trxtype= " + Strings.Replace(Strings.Replace(TransectionType, "(", ""), ")", "") + " ";
                    rstemp = objUtility.OpenDataTable(strsql);

                    if (rstemp.Rows.Count > 0)
                    {
                        if (rstemp.Rows[0][0].ToString() != "")
                        {
                            et.strClientcd = Strings.Trim(rstemp.Rows[0]["ie_cmcd"].ToString());
                            et.strClient_Name = Strings.Trim(rstemp.Rows[0]["cm_name"].ToString());
                            et.strClient_type = Strings.Trim(rstemp.Rows[0]["cm_acctype"].ToString());
                            et.strbranchcd = Strings.Trim(rstemp.Rows[0]["cm_brboffcode"].ToString());
                            et.strExecution_date = Strings.Trim(rstemp.Rows[0]["ie_execdt"].ToString());
                            et.lngLotno = rstemp.Rows[0]["ie_lotno"].ToString();
                            et.intLotsize = Interaction.IIf(Convert.ToInt32(rstemp.Rows[0]["ie_nooftrx"].ToString()) - Convert.ToInt32(rstemp.Rows[0]["ie_rejected"].ToString()) == 0, 30000, Convert.ToInt32(rstemp.Rows[0]["ie_nooftrx"].ToString()) - Convert.ToInt32(rstemp.Rows[0]["ie_rejected"].ToString())).ToString();
                            et.strSlipMode = rstemp.Rows[0]["ie_mode"].ToString();
                        }
                        else if (strInwardstatus == "A")
                        {
                            return "Inward entry not found for current slip";
                        }
                    }
                }
                et.strInwardstatus = strInwardstatus;
                strsql = "SELECT ";
                strsql = strsql + " us_trxtype, us_instcd, us_irn,us_clientcd, us_execdt ,us_archiveyn ";
                strsql = strsql + " FROM ";
                strsql = strsql + " Used_slip ";
                strsql = strsql + " WHERE ";
                strsql = strsql + " us_instcd='" + InstrumentType + "' and us_irn = " + InternalRefNo;
                rstemp = objUtility.OpenDataTable(strsql);

                if (rstemp.Rows.Count == 0)
                {
                    strsql = "select chm_cmcd, chm_chqno, chm_instcd, chm_status, chm_branchcd,isNull(chm_allow,'') chm_allow,isNull(chm_issuedate,'') chm_issuedate from Chequemaster where ";
                    strsql = strsql + InternalRefNo + " between chm_chqno and chm_chqno + chm_booksize -1 and chm_instcd = '" + InstrumentType + "'";
                    rsCheck1 = objUtility.OpenDataTable(strsql);

                    if (rsCheck1.Rows.Count > 0)
                    {
                        if (rsCheck1.Rows[0][0].ToString() != "")
                        {
                            if (rsCheck1.Rows[0]["chm_status"].ToString() == "N")
                            {
                                return "Slip Not Issued";
                            }
                            else if (rsCheck1.Rows[0]["chm_status"].ToString() == "D")
                            {
                                return "Slip Destroyed";
                            }
                            else if (rsCheck1.Rows[0]["chm_status"].ToString() == "A" | rsCheck1.Rows[0]["chm_status"].ToString() == "L")
                            {
                                return "Used as Loose Slip";
                            }
                            else if (rsCheck1.Rows[0]["chm_status"].ToString() == "B")
                            {
                                return "Slip is Issued to Branch";
                            }
                            else if (rsCheck1.Rows[0]["chm_status"].ToString() == "P")
                            {
                                strsql = "select chs_status from Chequestop where chs_chqno =" + InternalRefNo;
                                strsql = strsql + " and chs_instcd=" + InstrumentType;
                                rstemp = objUtility.OpenDataTable(strsql);

                                if (rstemp.Rows.Count > 0)
                                {
                                    if (rstemp.Rows[0][0].ToString() != "")
                                    {
                                        if (rstemp.Rows[0]["chs_status"].ToString() == "S")
                                        {
                                            return "Slip is Stop";
                                        }
                                        else if (rstemp.Rows[0]["chs_status"].ToString() == "D")
                                        {
                                            return "Slip is Destroyed";
                                        }
                                    }
                                }
                            }
                            else if (rsCheck1.Rows[0]["chm_status"].ToString() == "I")
                            {
                                strsql = "select chs_status from Chequestop where chs_chqno =" + InternalRefNo;
                                strsql = strsql + " and chs_instcd=" + InstrumentType;
                                rstemp = objUtility.OpenDataTable(strsql);
                                if (rstemp.Rows.Count > 0)
                                {
                                    if (rstemp.Rows[0][0].ToString() != "")
                                    {
                                        if (rstemp.Rows[0]["chs_status"].ToString() == "S")
                                        {
                                            return "Slip is Stop";
                                        }
                                        else if (rstemp.Rows[0]["chs_status"].ToString() == "D")
                                        {
                                            return "Slip is Destroyed";
                                        }
                                    }
                                }
                            }
                            else if (rsCheck1.Rows[0]["chm_status"].ToString() == "R")
                            {
                                return "Slip has been Sent to Printer";
                            }
                        }
                        else
                        {
                            intiPos = 0;
                            strSlipMaintanace = Strings.Trim(Strings.UCase(GetSysParm("SLIPMNT")));
                            if (strSlipMaintanace != "")
                            {
                                intiPos = Strings.InStr(strSlipMaintanace, TransectionType);
                                if (intiPos > 0)
                                {
                                    strSlipmnt = Strings.Mid(strSlipMaintanace, intiPos + 4, 1);
                                }
                            }

                            if (strSlipmnt == "F")
                            {
                                return "Couldn't find slip no in Stock";
                            }
                            else if (strSlipmnt == "P")
                            {
                                return "Couldn't find slip number in Stock";
                            }
                            else if (strSlipmnt != "N")
                            {
                                return "Invalid system parameter found in slip maintenance";
                            }
                        }
                        if (et.strClientcd != "")
                        {
                            strPROMPTSEQ = GetSysParm("PROMPTSEQ");
                            if (strPROMPTSEQ == "Y")
                            {
                                if (rstemp.Rows[0][0].ToString() != "")
                                {
                                    if (rstemp.Rows[0][0].ToString() != "")
                                    {
                                        if (rstemp.Rows[0]["chm_chqno"].ToString() != InternalRefNo)
                                        {
                                            return "Not using Slip in sequence";
                                        }
                                    }
                                    else
                                    {
                                        return "Not using Slip in sequence";
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                return "Please Enter Internal Ref No.";
            }
            if (clientCd != "") //client id
            {
                DateTime TrxDt, FreezeDt;
                TrxDt = DateTime.ParseExact(TrxDate, "yyyyMMdd", null);
                int x = Strings.InStr(1, clientCd, ",");
                if (x > 0 || clientCd.Length != 16)
                {
                    clientCd = AutoNumber(clientCd);
                }
                strsql = "select cm_cd, cm_name , cm_freezeyn , cm_freezedt , cm_active ,cm_sech_name ,cm_thih_name,cm_acctype,cm_chgsscheme,cm_allowcredit,cm_poaforpayin from Client_master where cm_cd = '" + clientCd + "'";
                ds = objUtility.OpenDataTable(strsql);
                if (ds.Rows.Count == 0)
                {
                    return "Client Id Not Found [" + clientCd + "]";
                }

                FreezeDt = DateTime.ParseExact(ds.Rows[0]["cm_freezedt"].ToString().Trim(), "yyyyMMdd", null);

                if (ds.Rows[0]["cm_freezeyn"].ToString() != "" || ds.Rows[0]["cm_freezedt"].ToString() != "")
                {
                    if ((ds.Rows[0]["cm_freezeyn"].ToString() == "1" || ds.Rows[0]["cm_freezeyn"].ToString() == "3") && (TrxDt >= FreezeDt))
                    {
                        return "This client [" + clientCd + "] is under Freeze Status";
                    }
                    if ((ds.Rows[0]["cm_freezeyn"].ToString() == "0" || ds.Rows[0]["cm_freezeyn"].ToString() == "2") && (TrxDt < FreezeDt))
                    {
                        return "This client [" + clientCd + "] is under Freeze Status, will be active on" + FreezeDt;
                    }
                }
                if (Strings.InStr(1, "01,02", ds.Rows[0]["cm_active"].ToString(), Constants.vbTextCompare) == 0)
                {
                    return "Client Is Not Active";
                }
                if (ds.Rows[0]["cm_poaforpayin"].ToString() == "N")
                {
                    return "Client [" + clientCd + "] has offered POA for Market Sell Obligation";
                }
            }
            else
            {
                return "Client Id can not be left blank";
            }
            if (TrxDate == "") // Transection Date
            {
                return "Please Enter Transection Date";
            }
            if (ExecDate == "") // Execution Date
            {
                return "Please Enter Execute Date";
            }
            if (TrxDate != "" && ExecDate != "")
            {
                DateTime ExecDt, TrxDt;
                ExecDt = DateTime.ParseExact(ExecDate, "yyyyMMdd", null);
                TrxDt = DateTime.ParseExact(TrxDate, "yyyyMMdd", null);
                if (ExecDt < TrxDt)
                {
                    return "Transection Date Can't Be Greater Than Execution Date";
                }
            }
            if (Type != "") //Type
            {
                strsql = "select COUNT(*) from Instrument_master where im_instcd = '" + Type + "'";
                ds = objUtility.OpenDataTable(strsql);
                if (ds.Rows.Count == 0)
                {
                    return "Invalid Instrument Type [ " + Type + " ]";
                }
            }
            if (BranchCd != "") //Branch
            {
                strsql = "select count(*) from Branch_master where bm_branchcd = '" + BranchCd + "'";
                ds = objUtility.OpenDataTable(strsql);
                if (ds.Rows[0][0].ToString() == "0")
                {
                    return "Invalid Branch Code [ " + BranchCd + " ]";
                }
            }
            else
            {
                return "Please Enter Branch Code";
            }
            if (Conversion.Val(ReceiveMode) > 12) // Receive Mode
            {
                return "Invalid Receive Mode [ " + ReceiveMode + " ]";
            }
            return "Valid";
        }
        public string OffMarketValidation(string ClientCd, string ReasonCd, string PaymentMode, string PaidBy, string EntryBy, string BOId, string ISIN, double Qty, string CounterSettNo, string FromSettNo, string Exchange, string Segment, string UCC, string CMID, string TMID, SqlDataAdapter SqlAdptr, SqlConnection SqlCon, SqlTransaction SqlTrans)
        {
            //For multiple entries
            DataTable Dt;
            if (ReasonCd == "") //Reason Code
            {
                return "Please Enter Reason Code";
            }
            else
            {
                strsql = "select COUNT(*) from Reasonfortrade where rt_code = '" + ReasonCd + "'";
                Dt = objUtility.OpenDataTable(strsql, SqlCon, SqlTrans);
                if (Conversion.Val(Dt.Rows[0][0].ToString().Trim()) == 0)
                {
                    return "Invalid Reason Code [ " + ReasonCd + " ]";
                }
            }
            if (PaymentMode != "") // Payment Mode
            {
                if (PaymentMode != "1" && PaymentMode != "2" && PaymentMode != "3")
                {
                    return "Invalid Payment Mode";
                }
            }
            else if (PaymentMode == "" && ReasonCd == "2")
            {
                return "Payment Mode Cannot Be Blank";
            }
            if (PaidBy != "") // Paid By
            {
                if (PaidBy != "0" && PaidBy != "1")
                {
                    return "Invalid Paid By Code";
                }
            }
            else if (PaidBy == "" && ReasonCd == "2")
            {
                return "Paid By Cannot Be blank";
            }
            if (EntryBy != "") // Entry By
            {
                if (EntryBy != "TM" && EntryBy != "CP")
                {
                    return "Invalid Entry By Code";
                }
            }
            else if (EntryBy == "" && CounterSettNo != "")
            {
                return "Entry By Cannot Be Blank";
            }
            if (Segment != "") // Segment
            {
                strsql = "select * from Clientsub_master where cs_module ='CS26' and cs_code = '" + Segment + "'";
                Dt = objUtility.OpenDataTable(strsql);
                if (Dt.Rows.Count == 0)
                {
                    return "Invalid Segment Code";
                }
            }
            else if (Segment == "" && CounterSettNo != "")
            {
                return "Segment Code Cannot Be Blank";
            }
            if (Exchange != "") // Exchange
            {
                strsql = "select " +
                         "a.bp_id 'STOCK EXCHENGE', " +
                         "a.bp_name 'NAME', " +
                         "a.bp_assd_cc_cmid 'CC ID', " +
                         "p.bp_name 'NAME' " +
                         "from Bpmaster a, Bpmaster p " +
                         "where a.bp_id = p.bp_id and a.bp_role = '02' and p.bp_role = '02' and a.bp_id = '" + Exchange + "'";
                Dt = objUtility.OpenDataTable(strsql);
                if (Dt.Rows.Count == 0)
                {
                    return "Invalid Exchange Code";
                }
            }
            else if (Exchange == "" && CounterSettNo != "")
            {
                return "Exchange Code Cannot Be Blank";
            }
            if (UCC != "") // UCC
            {
                strsql = "select " +
                     "Case cud_Exchnge when '11' then 'BSE' when '12' then 'NSE' when '23' then 'MCX' when '22' then 'NCDEX' else cud_Exchnge end as 'Exchange', " +
                     "Case cud_segment when '01' then 'Cash' when '02' then 'F&O' when '03' then 'Currency' when '04' then 'SLB' when '05' then 'Commodity' when '06' then 'Debt' else cud_segment end as 'Segment', " +
                     "cud_UCC, " +
                     "cud_cmid, " +
                     "cud_tmid " +
                     "from Client_UCC_Details where cud_UCC = '" + UCC + "'";
                Dt = objUtility.OpenDataTable(strsql);
                if (Dt.Rows.Count == 0)
                {
                    return "Invalid UCC Code";
                }
            }
            else if (UCC == "" && CounterSettNo != "")
            {
                return "UCC Code Cannot Be Blank";
            }
            if (CMID != "") //CMID
            {
                strsql = "select " +
                     "Case cud_Exchnge when '11' then 'BSE' when '12' then 'NSE' when '23' then 'MCX' when '22' then 'NCDEX' else cud_Exchnge end as 'Exchange', " +
                     "Case cud_segment when '01' then 'Cash' when '02' then 'F&O' when '03' then 'Currency' when '04' then 'SLB' when '05' then 'Commodity' when '06' then 'Debt' else cud_segment end as 'Segment', " +
                     "cud_UCC, " +
                     "cud_cmid, " +
                     "cud_tmid " +
                     "from Client_UCC_Details where cud_UCC = '" + UCC + "' and cud_cmid = '" + CMID + "'";
                Dt = objUtility.OpenDataTable(strsql);
                if (Dt.Rows.Count == 0)
                {
                    return "Invalid CM Id Code";
                }
            }
            else if (CMID == "" && CounterSettNo != "")
            {
                return "CM id Code Cannot Be Blank";
            }
            if (TMID != "") // TMID
            {
                strsql = "select " +
                     "Case cud_Exchnge when '11' then 'BSE' when '12' then 'NSE' when '23' then 'MCX' when '22' then 'NCDEX' else cud_Exchnge end as 'Exchange', " +
                     "Case cud_segment when '01' then 'Cash' when '02' then 'F&O' when '03' then 'Currency' when '04' then 'SLB' when '05' then 'Commodity' when '06' then 'Debt' else cud_segment end as 'Segment', " +
                     "cud_UCC, " +
                     "cud_cmid, " +
                     "cud_tmid " +
                     "from Client_UCC_Details where cud_UCC = '" + UCC + "' and cud_tmid = '" + TMID + "'";
                Dt = objUtility.OpenDataTable(strsql);
                if (Dt.Rows.Count == 0)
                {
                    return "Invalid TM Id Code";
                }
            }
            else if (TMID == "" && CounterSettNo != "")
            {
                return "TM id Code Cannot Be Blank";
            }
            if (CounterSettNo != "") // Counter sett no
            {
                strsql = "select COUNT(*) from Cc_calender where cc_settle_no = '" + CounterSettNo + "'";
                Dt = objUtility.OpenDataTable(strsql, SqlCon, SqlTrans);
                if (Conversion.Val(Dt.Rows[0][0].ToString().Trim()) == 0)
                {
                    return "Invalid Counter Sett No  [ " + CounterSettNo + " ]";
                }
            }
            if (FromSettNo != "") // From sett no
            {
                strsql = "select COUNT(*) from Cc_calender where cc_settle_no = '" + FromSettNo + "'";
                Dt = objUtility.OpenDataTable(strsql, SqlCon, SqlTrans);
                if (Conversion.Val(Dt.Rows[0][0].ToString().Trim()) == 0)
                {
                    return "Invalid From Sett No [ " + FromSettNo + " ]";
                }
            }
            if (BOId != "") //BO/CM id
            {
                strsql = "select cm_cd,cm_name ,cm_acctype , cm_active,cm_billcode from Client_master where cm_cd ='" + BOId + "'";
                Dt = objUtility.OpenDataTable(strsql, SqlCon, SqlTrans);
                if (Dt.Rows.Count == 0)
                {
                    return "Entered Client is not found";
                }
                if (BOId.Length < 16)
                {
                    return "Enter 16 Digits Client ID";
                }
                if (Dt.Rows[0]["cm_active"].ToString().Trim() != "01")
                {
                    return "BO Id is not active";
                }
                if (BOId == ClientCd)
                {
                    return "Both Clients Can't be Same";
                }
            }
            if (ISIN != "") //ISIN code
            {
                if (Strings.Left(ISIN, 2) != "IN")
                {
                    ISIN = "IN" + ISIN;
                }
                if (ISIN.Length < 12)
                {
                    ISIN = fnfindisin(ISIN.Trim());
                }
                if (ISIN.Length < 12)
                {
                    return "Invalid ISIN";
                }
                strsql = "select * from Security where sc_isincode = '" + ISIN + "'";
                Dt = objUtility.OpenDataTable(strsql, SqlCon, SqlTrans);
                if (Dt.Rows.Count > 0)
                {
                    if (Dt.Rows[0]["sc_security_status"].ToString().Trim() != "01")
                    {
                        return "ISIN is not active";
                    }
                }
                else
                {
                    return "Entered ISIN is not found";
                }
            }
            else
            {
                return "Please Enter ISIN";
            }
            if (Qty == 0) //Qty
            {
                return "Please Enter Qty";
            }

            return "Valid";
        }
        public void TempOffMarketTable(SqlConnection Con, SqlTransaction objTrans)
        {
            strsql = "create table #TempOffMraket (" +
                     "Sr_No int Identity(1,1)," +
                     "Transection_Id varchar(8)," +
                     "BO_Id char(16)," +
                     "BOId_Name char(30)," +
                     "DP_Name char(30)," +
                     "ISIN_Code char(12)," +
                     "ISIN_Name char(30)," +
                     "Qty numeric(18,3)," +
                     "Rate money," +
                     "Value money," +
                     "Cash char(4)," +
                     "CounterSett char(13)," +
                     "Reason_Code char(4)," +
                     "Reason char(30)," +
                     "Flag char(1)," +
                     "Charges money," +
                     "Auth char(1)," +
                     "Trade_No char(16)," +
                     "Status char(30)," +
                     "Remark varchar(50)," +
                     "Time char(8)," +
                     "FromSett char(13)," +
                     "Payment_Mode_Code char(1)," +
                     "Payment_Mode_Desc char(20)," +
                     "PayeeName char(30)," +
                     "Cheque_No char(15)," +
                     "DateOfIssue char(8)," +
                     "BankAccNo char(30)," +
                     "Bank_Name char(30)," +
                     "Branch_Name char(30)," +
                     "Consideration money," +
                     "Stamp_Duty char(15)," +
                     "Paid_By_Code char(1)," +
                     "Paid_By_Desc char(25)," +
                     "Exchange char(15)," +
                     "Exchange_Desc char(50)," +
                     "Segment char(15)," +
                     "Segment_Desc char(30)," +
                     "UCC char(30)," +
                     "CM_Id char(8)," +
                     "Entry_By char(15)," +
                     "TM_Id char(30)" +
                     ")";
            objUtility.ExecuteSQL(strsql, Con, objTrans);
        }
        public dynamic fnRTrimTable(SqlConnection Con, SqlTransaction objTrans)
        {
            strsql = "select " +
                                 "rtrim(Sr_No) Sr_No," +
                                 "rtrim(Transection_Id) Transection_Id," +
                                 "rtrim(BO_Id) BO_Id," +
                                 "rtrim(BOId_Name) BOId_Name," +
                                 "rtrim(DP_Name) DP_Name," +
                                 "rtrim(ISIN_Code) ISIN_Code," +
                                 "rtrim(ISIN_Name) ISIN_Name," +
                                 "rtrim(Qty) Qty," +
                                 "Rate," +
                                 "Value," +
                                 "rtrim(Cash) Cash," +
                                 "rtrim(CounterSett) Counter_Sett_No," +
                                 "rtrim(Reason_Code) Reason_Code," +
                                 "rtrim(Reason) Reason," +
                                 "rtrim(Flag) Flag," +
                                 "rtrim(Charges) Charges," +
                                 "rtrim(Trade_No) Trade_No," +
                                 "rtrim(Status) Status," +
                                 "rtrim(Remark) Remark," +
                                 "rtrim(Time) Time," +
                                 "rtrim(FromSett) From_Sett_No," +
                                 "rtrim(Payment_Mode_Code) Payment_Mode_Code," +
                                 "rtrim(Payment_Mode_Desc) Payment_Mode_Desc," +
                                 "rtrim(PayeeName) Payee_Name," +
                                 "rtrim(Cheque_No) Cheque_No," +
                                 "rtrim(DateOfIssue) Date_Of_Issue," +
                                 "rtrim(BankAccNo) Bank_Acc_No," +
                                 "rtrim(Bank_Name) Bank_Name," +
                                 "rtrim(Branch_Name) Branch_Name," +
                                 "Consideration," +
                                 "rtrim(Stamp_Duty) Stamp_Duty," +
                                 "rtrim(Paid_By_Code) Paid_By_Code," +
                                 "rtrim(Paid_By_Desc) Paid_By_Desc," +
                                 "rtrim(Exchange) Exchange," +
                                 "rtrim(Exchange_Desc) Exchange_Desc," +
                                 "rtrim(Segment) Segment," +
                                 "rtrim(Segment_Desc) Segment_Desc," +
                                 "rtrim(UCC) UCC," +
                                 "rtrim(CM_Id) CM_Id," +
                                 "rtrim(Entry_By) Entry_By," +
                                 "rtrim(TM_Id) TM_Id" +
                                 " from #TempOffMraket order by Sr_No";
            DataTable Dt = objUtility.OpenDataTable(strsql, Con, objTrans);
            return Dt;
        }
        public string fnExchangeName(string Exch)
        {
            if (Exch != "")
            {
                strsql = "select a.bp_name 'NAME' from Bpmaster a, Bpmaster p where a.bp_id  = p.bp_id and a.bp_role = '02' and p.bp_role ='02' and a.bp_id = '" + Exch + "'";
                DataTable Dt = objUtility.OpenDataTable(strsql);
                if (Dt.Rows.Count > 0)
                {
                    return Dt.Rows[0][0].ToString().Trim();
                }
                return "";
            }
            return "";
        }
        public string fnSegmentName(string Seg)
        {
            if (Seg != "")
            {
                strsql = "select cs_desc from Clientsub_master where cs_module ='CS26' and cs_code = '" + Seg + "'";
                DataTable Dt = objUtility.OpenDataTable(strsql);
                if (Dt.Rows.Count > 0)
                {
                    return Dt.Rows[0][0].ToString().Trim();
                }
                return "";
            }
            return "";
        }
        public string fnPaymentMode(string PCode)
        {
            if (PCode == "1")
            {
                return "Cheque Payment";
            }
            else if (PCode == "2")
            {
                return "Electronic Payment";
            }
            else if (PCode == "3")
            {
                return "Cash";
            }
            return "";
        }
        public string fnPaidBy(string PBCode)
        {
            if (PBCode == "0")
            {
                return "Dipository Particepant";
            }
            else if (PBCode == "1")
            {
                return "Client";
            }
            return "";
        }
        public string GetSysParm(string strParmcd)
        {
            string res;
            strsql = "Select sp_sysvalue from Sysparameter where sp_parmcd= '" + strParmcd + "'";
            DataTable ds = objUtility.OpenDataTable(strsql);
            if (ds.Rows.Count == 0)
            {
                return "";
            }
            res = ds.Rows[0][0].ToString().Trim();
            return res;
        }
        public double fnStampDutyRate(string strSecType)
        {
            double dblRate;
            string strPer;
            strPer = Strings.Trim(GetSysParm("STAMPDUTY"));
            if (Strings.InStr(1, ",12,13,14,15,19,", "," + Strings.Trim(strSecType) + ",") > 0)
            {
                dblRate = Conversion.Val(Strings.Split(strPer, "~")[1]);
            }
            else if (Strings.InStr(1, ",16,20,25,", "," + Strings.Trim(strSecType) + ",") > 0)
            {
                dblRate = Conversion.Val(Strings.Split(strPer, "~")[2]);
            }
            else
            {
                dblRate = Conversion.Val(Strings.Split(strPer, "~")[0]);
            }
            return dblRate;
        }
        public double fnLateFeeCharges(string ClientCd, int Qty)
        {
            strsql = "select cd_perc_amount,cd_min_amount,cd_fixed_amount,cd_max_amount from Chargesdetail where cd_scheme = (select cm_chgsscheme from Client_master where cm_Cd = '" + ClientCd + "') and cd_code = '43'";
            DataTable Dt = objUtility.OpenDataTable(strsql);
            int d1, d2, d3, d4;
            d1 = Convert.ToInt32(Dt.Rows[0]["cd_perc_amount"].ToString());
            d2 = Convert.ToInt32(Dt.Rows[0]["cd_min_amount"].ToString());
            d3 = Convert.ToInt32(Dt.Rows[0]["cd_fixed_amount"].ToString());
            d4 = Convert.ToInt32(Dt.Rows[0]["cd_max_amount"].ToString());
            if (d1 > 0)
            {
                return Qty * d1 / 100;
            }
            if (d2 > 0)
            {
                if (Qty < d2)
                {
                    return d2;
                }
            }
            if (d3 > 0)
            {
                return d3 + Qty;
            }
            if (d4 > 0)
            {
                if (Qty < d4)
                {
                    return d4;
                }
            }
            return 0;
        }
        public string fnfindisin(string misin)
        {
            int x;
            int y;
            string mvar;
            string mvar1;
            var mfind = default(string);
            int intlength;
            if (!string.IsNullOrEmpty(misin))
            {
                mvar = "/";
                x = Strings.InStr(1, misin, mvar);
                if (x > 0)
                {
                    intlength = Strings.Len(misin);
                    y = intlength - x;
                    mvar = Strings.Left(misin, x - 1);
                    mvar1 = Strings.Right(misin, y);
                    if (Strings.Asc(Strings.Mid(misin, x - 1, 1)) >= 65 & Strings.Asc(Strings.Mid(misin, x - 1, 1)) <= 90)
                    {
                        mfind = mvar + "010" + mvar1;
                    }
                    else
                    {
                        mfind = mvar + "A010" + mvar1;
                    }
                }
                return mfind;
            }
            else
            {
                return Conversions.ToString(' ');
            }
        }
        public dynamic mfnSlipCheck(string strClientCd, string strtrxtype, int intinstcd, double lngChqno)
        {
            DataTable rsCheck1;
            DataTable rstemp;
            string strPROMPTSEQ, strTrxcodes, strInwardentry, strInwardstatus;
            int intiPos, eOffmarket = 904, eInterDepository = 925, eOnmarket = 906, eEarlypayin = 903, eDemat = 901, epledge = 908, eRemat = 902;

            if (Conversion.Val(strtrxtype) == eOffmarket)
            {
                strTrxcodes = "('904')";
            }
            else if (Conversion.Val(strtrxtype) == eEarlypayin)
            {
                strTrxcodes = "('903')";
            }
            else if (Conversion.Val(strtrxtype) == eInterDepository)
            {
                strTrxcodes = "('925')";
            }
            else if (Conversion.Val(strtrxtype) == eOnmarket)
            {
                strTrxcodes = "('906')";
            }
            else if (Conversion.Val(strtrxtype) == eDemat)
            {
                strTrxcodes = "('901')";
            }
            else if (Conversion.Val(strtrxtype) == epledge)
            {
                strTrxcodes = "('908')";
            }
            else if (Conversion.Val(strtrxtype) == eRemat)
            {
                strTrxcodes = "('902')";
            }
            else
            {
                strTrxcodes = "('" + strtrxtype + "')";
            }
            strInwardstatus = "N";
            strInwardentry = Strings.Trim(Strings.UCase(GetSysParm("INWARDENTRY")));
            if (strInwardentry != "")
            {
                intiPos = Strings.InStr(strInwardentry, strtrxtype);
                if (intiPos > 0)
                {
                    strInwardstatus = Strings.Mid(strInwardentry, intiPos + 4, 1);
                }
            }
            SlipCheck et = new SlipCheck();
            et.strInwardstatus = strInwardstatus;

            if (strInwardstatus == "A" | strInwardstatus == "O")
            {

                strsql = "select ie_cmcd,cm_name,ie_lotno,ie_slipno,ie_nooftrx,ie_rejected,ie_mode,";
                strsql = strsql + " lz_status,ie_execdt, cm_acctype, cm_brboffcode from Inward_entry,Lot_size, ";
                strsql = strsql + " Instrument_master,Client_master where ie_cmcd = cm_cd ";
                strsql = strsql + " and ie_lotno = lz_lotno   and ie_trxtype = lz_type ";
                strsql = strsql + " and ie_instno = im_instcd  ";
                strsql = strsql + " and ie_slipno = '" + lngChqno + "'";
                strsql = strsql + " and im_instcd = " + intinstcd;
                strsql = strsql + " and ie_trxtype= " + Strings.Replace(Strings.Replace(strTrxcodes, "(", ""), ")", "") + " ";
                rstemp = objUtility.OpenDataTable(strsql);

                if (rstemp.Rows[0][0].ToString() != "")
                {
                    et.strClientcd = Strings.Trim(rstemp.Rows[0]["ie_cmcd"].ToString());
                    et.strClient_Name = Strings.Trim(rstemp.Rows[0]["cm_name"].ToString());
                    et.strClient_type = Strings.Trim(rstemp.Rows[0]["cm_acctype"].ToString());
                    et.strbranchcd = Strings.Trim(rstemp.Rows[0]["cm_brboffcode"].ToString());
                    et.strExecution_date = Strings.Trim(rstemp.Rows[0]["ie_execdt"].ToString());
                    et.lngLotno = rstemp.Rows[0]["ie_lotno"].ToString();
                    et.intLotsize = Interaction.IIf(Convert.ToInt32(rstemp.Rows[0]["ie_nooftrx"].ToString()) - Convert.ToInt32(rstemp.Rows[0]["ie_rejected"].ToString()) == 0, 30000, Convert.ToInt32(rstemp.Rows[0]["ie_nooftrx"].ToString()) - Convert.ToInt32(rstemp.Rows[0]["ie_rejected"].ToString())).ToString();
                    et.strSlipMode = rstemp.Rows[0]["ie_mode"].ToString();
                }
                else if (strInwardstatus == "A")
                {
                    return "Inward entry not found for current slip";
                }
            }
            et.strInwardstatus = strInwardstatus;
            strsql = "SELECT ";
            strsql = strsql + " us_trxtype, us_instcd, us_irn,us_clientcd, us_execdt ,us_archiveyn ";
            strsql = strsql + " FROM ";
            strsql = strsql + " Used_slip ";
            strsql = strsql + " WHERE ";
            strsql = strsql + " us_instcd='" + intinstcd + "' and us_irn = " + lngChqno;
            rstemp = objUtility.OpenDataTable(strsql);
            if (rstemp.Rows.Count == 0)
            {
                strsql = "select chm_cmcd, chm_chqno, chm_instcd, chm_status, chm_branchcd,isNull(chm_allow,'') chm_allow,isNull(chm_issuedate,'') chm_issuedate from Chequemaster where ";
                strsql = strsql + lngChqno + " between chm_chqno and chm_chqno + chm_booksize -1 and chm_instcd = '" + intinstcd + "'";
                rsCheck1 = objUtility.OpenDataTable(strsql);
                if (rsCheck1.Rows.Count > 0)
                {
                    if (rsCheck1.Rows[0]["chm_status"].ToString() == "N")
                    {
                        return "Slip Not Issued";
                    }
                    else if (rsCheck1.Rows[0]["chm_status"].ToString() == "D")
                    {
                        return "Slip Destroyed";
                    }
                    else if (rsCheck1.Rows[0]["chm_status"].ToString() == "A" || rsCheck1.Rows[0]["chm_status"].ToString() == "L")
                    {
                        return "Used as Loose Slip";
                    }
                    else if (rsCheck1.Rows[0]["chm_status"].ToString() == "B")
                    {
                        return "Slip is Issued to Branch";
                    }
                    else if (rsCheck1.Rows[0]["chm_status"].ToString() == "P")
                    {
                        strsql = "select chs_status from Chequestop where chs_chqno =" + lngChqno;
                        strsql = strsql + " and chs_instcd=" + intinstcd;
                        rstemp = objUtility.OpenDataTable(strsql);

                        if (rstemp.Rows.Count > 0)
                        {
                            if (rstemp.Rows[0]["chs_status"].ToString() == "S")
                            {
                                return "Slip No. is under Stop Status";
                            }
                            else if (rstemp.Rows[0]["chs_status"].ToString() == "D")
                            {
                                return "Slip is Destroyed";
                            }
                            if (strClientCd != rstemp.Rows[0]["chs_cmcd"].ToString())
                            {
                                return "Slip No. is not Mapped to this Client.";
                            }
                        }
                    }
                    else if (rsCheck1.Rows[0]["chm_status"].ToString() == "I")
                    {
                        strsql = "select chs_status from Chequestop where chs_chqno =" + lngChqno;
                        strsql = strsql + " and chs_instcd=" + intinstcd;
                        rstemp = objUtility.OpenDataTable(strsql);

                        if (rstemp.Rows.Count > 0)
                        {
                            if (rstemp.Rows[0]["chs_status"].ToString() == "S")
                            {
                                return "Slip No. is under Stop Status";
                            }
                            else if (rstemp.Rows[0]["chs_status"].ToString() == "D")
                            {
                                return "Slip is Destroyed";
                            }
                            if (strClientCd != rstemp.Rows[0]["chs_cmcd"].ToString())
                            {
                                return "Slip No. is not Mapped to this Client.";
                            }
                        }
                    }
                    else if (rsCheck1.Rows[0]["chm_status"].ToString() == "R")
                    {
                        return "Slip has been Sent to Printer";
                    }
                    else
                    {
                        return "Invalid Slip Status";
                    }
                    if (et.strClientcd != "")
                    {
                        strPROMPTSEQ = Strings.UCase(Strings.Trim(GetSysParm("PROMPTSEQ")));
                        if (strPROMPTSEQ == "Y")
                        {
                            if (rstemp.Rows[0][0].ToString() != "")
                            {
                                if (rstemp.Rows[0][0].ToString() != "")
                                {
                                    if (Conversion.Val(rstemp.Rows[0]["chm_chqno"]) != lngChqno)
                                    {
                                        return "Not using Slip in sequence";
                                    }
                                }
                                else
                                {
                                    return "Not using Slip in sequence";
                                }
                            }
                        }
                    }
                    if (strClientCd != rsCheck1.Rows[0]["chm_cmcd"].ToString())
                    {
                        return "Slip No. is not Mapped to this Client.";
                    }
                }
                else
                {
                    return "Couldn't find slip number in Stock";
                }
            }
            return "Valid";
        }
        public void mfnInsertUsed_slip(SqlConnection con, SqlTransaction objTrans, string strtrxtype, string intinstcd, string lngChqno, string strExec_date, string strEntrymode, string strClientcd, string strRemark, string UserId, string MkrDt, string Mkrtm)
        {
            string strSQL;
            strSQL = "select * from Used_slip where us_trxtype = '" + strtrxtype + "' and us_instcd = '" + intinstcd + "' and us_irn = " + lngChqno;
            DataTable Dt = objUtility.OpenDataTable(strSQL, con, objTrans);
            if (Dt.Rows.Count == 0)
            {
                strSQL = "insert into Used_slip (us_trxtype,us_instcd,us_irn,us_execdt,us_mode,us_remarks,us_archiveyn,us_clientcd, us_filler1,mkrid,mkrdt,mkrtm)";
                strSQL = strSQL + " values('" + strtrxtype + "','" + intinstcd + "'," + lngChqno + ",'" + strExec_date + "','" + strEntrymode + "','" + strRemark + "','N','" + strClientcd + "','','" + UserId + "','" + MkrDt + "','" + Mkrtm + "')";
                objUtility.ExecuteSQL(strSQL, con, objTrans);
            }
            else
            {
                strSQL = "update Used_slip set us_irn = " + lngChqno + ", us_clientcd = '" + strClientcd + "', us_execdt = '" + strExec_date + "', mkrid = '" + UserId + "', mkrdt = '" + MkrDt + "', mkrtm = '" + Mkrtm + "' where us_trxtype = '" + strtrxtype + "' and us_instcd = '" + intinstcd + "' and us_irn = " + lngChqno;
                objUtility.ExecuteSQL(strSQL, con, objTrans);
            }
        }
        public string AutoNumber(string idno)
        {
            string AutoNumberRet = default;
            string strAuto, GstrCdslDpid = "";
            int x;
            int Length;
            int y;
            string Temp;
            string temp1;
            var result = default(string);
            if (Strings.InStr(1, idno, ",") > 0 | Strings.InStr(1, idno, ".") > 0 | Strings.Len(idno) > 0 & Strings.Len(idno) < 8)
            {
                if (Strings.InStr(1, idno, ",") > 0)
                {
                    Temp = ",";
                }
                else if (Strings.InStr(1, idno, ".") > 0)
                {
                    Temp = ".";
                }
                else
                {
                    Temp = ",";
                }

                x = Strings.InStr(1, idno, Temp);
                strAuto = GstrCdslDpid;
                if (x > 0)
                {
                    Length = Strings.Len(idno);
                    y = Length - x;
                    Temp = Strings.Left(idno, x - 1);
                    Temp = (Conversion.Val(strAuto) + Conversion.Val(Strings.Left(idno, x - 1))).ToString();
                    temp1 = Strings.Right(idno, y);
                    if (Strings.Len(Temp) + Strings.Len(temp1) < 16)
                    {
                        result = (16 - (Strings.Len(Temp) + Strings.Len(temp1)), "0").ToString();
                    }
                    result = Temp + result + temp1;
                }
                else
                {
                    result = "0" + (7 - Strings.Len(idno), "0").ToString() + idno;
                    result = strAuto + result;
                }
                AutoNumberRet = result;
            }
            else if (Strings.Len(idno) == 8 & Information.IsNumeric(idno))
            {
                AutoNumberRet = idno;
                AutoNumberRet = GstrCdslDpid + AutoNumberRet;
            }
            else if (Strings.Len(idno) == 16 & Information.IsNumeric(idno))
            {
                AutoNumberRet = idno;
            }
            else
            {
                AutoNumberRet = "";
            }
            return AutoNumberRet;
        }
        #endregion

        public dynamic InterDipositoryAdd(string userId, InterDipositoryAddReq req)
        {
            try
            {
                DataTable Ds;
                var db = new DataContext();
                SqlTransaction objTrans;
                string ReceiveMode, secType, InstCd, strAuth, strRate, strSearch, strCounterSett;
                int PrimaryKey;
                double StampDuty;
                bool blnIsUpdated = false;
                string MkrDt = DateTime.Now.ToString("yyyyMMdd");
                string Mkrtm = DateTime.Now.ToString("hh:mm:ss");
                int instcd = Convert.ToInt32(req.InstrumentType);
                double CheqNo = Convert.ToDouble(req.InternalRefNo);
                #region Data List
                List<string> ListBoid = req.Data.Select(x => x.ClientID).ToList();
                List<string> Listisin = req.Data.Select(x => x.ISIN).ToList();
                List<string> ListDPid = req.Data.Select(x => x.DPId).ToList();
                List<double> ListQty = req.Data.Select(x => x.Qty).ToList();
                List<string> ListFromSettNo = req.Data.Select(x => x.FromSettNo).ToList();
                List<string> ListCounterSettNo = req.Data.Select(x => x.CounterSettNo).ToList();
                List<string> ListRemark = req.Data.Select(x => x.Remarks).ToList();
                List<string> ListReason = req.Data.Select(x => x.Reason).ToList();
                List<string> ListPaymentMode = req.Data.Select(x => x.PaymentMode).ToList();
                List<string> ListPayeeName = req.Data.Select(x => x.PayeeName).ToList();
                List<string> ListChequeNo = req.Data.Select(x => x.ChequeOrRefNo).ToList();
                List<string> ListDateOfIssue = req.Data.Select(x => x.DateOfIssue).ToList();
                List<string> ListBankAccNo = req.Data.Select(x => x.BankAccountNo).ToList();
                List<string> ListBankName = req.Data.Select(x => x.BankName).ToList();
                List<string> ListBranchName = req.Data.Select(x => x.BranchName).ToList();
                List<string> ListConsideration = req.Data.Select(x => x.Consideration).ToList();
                List<string> ListPaidBy = req.Data.Select(x => x.PaidBy).ToList();
                List<string> ListExchange = req.Data.Select(x => x.Exchange).ToList();
                List<string> ListSegment = req.Data.Select(x => x.Segment).ToList();
                List<string> ListUCC = req.Data.Select(x => x.UCC).ToList();
                List<string> ListCMid = req.Data.Select(x => x.CMId).ToList();
                List<string> ListEntryBy = req.Data.Select(x => x.EntryBy).ToList();
                List<string> ListEarlyPayin = req.Data.Select(x => x.EarlyPayin).ToList();
                List<string> ListTMid = req.Data.Select(x => x.TMID).ToList();
                #endregion

                if (fnInterDepositoryAddValidations(req.InstrumentType, req.TransectionType, req.TransectionDate, req.ExecutionDate, req.Branch, req.InternalRefNo, req.ClientID, req.ReceiveMode) == "Valid" || fnInterDepositoryAddValidations(req.InstrumentType, req.TransectionType, req.TransectionDate, req.ExecutionDate, req.Branch, req.InternalRefNo, req.ClientID, req.ReceiveMode) == "Internal Ref No Already Present")
                {
                    if (mfnSlipCheck(req.ClientID, req.TransectionType, instcd, CheqNo) != "Valid")
                    {
                        return mfnSlipCheck(req.ClientID, req.TransectionType, instcd, CheqNo);
                    }
                    using (SqlConnection sqlCon = new SqlConnection((db.Database.GetDbConnection()).ConnectionString))
                    {
                        sqlCon.Open();
                        objTrans = sqlCon.BeginTransaction();
                        SqlDataAdapter sqlDtAdap = new SqlDataAdapter();
                        SqlCommandBuilder sqlCmdBld = new SqlCommandBuilder(sqlDtAdap);

                        if (fnInterDepositoryAddValidations(req.InstrumentType, req.TransectionType, req.TransectionDate, req.ExecutionDate, req.Branch, req.InternalRefNo, req.ClientID, req.ReceiveMode) == "Internal Ref No Already Present")
                        {
                            strsql = "select * from Interdepository where id_internalrefno = '" + req.InternalRefNo + "'";
                            Ds = objUtility.OpenDataTable(strsql);
                            if (Ds.Rows[0]["Id_allow"].ToString().Trim() == "S" || Ds.Rows[0]["Id_allow"].ToString().Trim() == "E")
                            {
                                return "Data Already Exported.";
                            }
                            else
                            {
                                strsql = "delete from Interdepository where id_internalrefno = '" + req.InternalRefNo + "'";
                                objUtility.ExecuteSQL(strsql, sqlCon, objTrans);
                                blnIsUpdated = true;
                            }
                        }

                        for (int i = 0; i < req.Data.Count; i++)
                        {
                            if (fnInterDepositoryAddValidations(req.ClientID, ListDPid[i], req.Branch, ListCounterSettNo[i], ListBoid[i], Listisin[i], req.TransectionDate, req.ExecutionDate, ListQty[i], req.TransectionType, req.InstrumentType) != "Valid")
                            {
                                return fnInterDepositoryAddValidations(req.ClientID, ListDPid[i], req.Branch, ListCounterSettNo[i], ListBoid[i], Listisin[i], req.TransectionDate, req.ExecutionDate, ListQty[i], req.TransectionType, req.InstrumentType);
                            }
                            if (fnInterDepositoryDropDownValid(ListCounterSettNo[i], ListPaymentMode[i], ListPaidBy[i], ListEntryBy[i], ListEarlyPayin[i], ListReason[i], ListSegment[i], ListExchange[i], ListUCC[i], ListCMid[i], ListTMid[i]) != "Valid")
                            {
                                return fnInterDepositoryDropDownValid(ListCounterSettNo[i], ListPaymentMode[i], ListPaidBy[i], ListEntryBy[i], ListEarlyPayin[i], ListReason[i], ListSegment[i], ListExchange[i], ListUCC[i], ListCMid[i], ListTMid[i]);
                            }
                            ReceiveMode = fnReceiveMode(req.ReceiveMode.Trim(), "InterDepository");
                            strCounterSett = ListCounterSettNo[i];

                            InstCd = req.InstrumentType;
                            strsql = "select MAX(id_pri_key) from Interdepository";
                            Ds = objUtility.OpenDataTable(strsql, sqlCon, objTrans);
                            PrimaryKey = Convert.ToInt32(Ds.Rows[0][0].ToString()) + 1;

                            strsql = "select sc_security_type from Security where sc_isincode = '" + Listisin[i] + "'";
                            secType = objUtility.OpenDataTable(strsql, sqlCon, objTrans).Rows[0][0].ToString().Trim();
                            StampDuty = 0;
                            StampDuty = fnStampDutyRate(secType);
                            StampDuty = Convert.ToDouble(string.Format("{0:#.00}", Conversion.Val(ListConsideration[i]) * StampDuty / 100));

                            strAuth = GetSysParm("IMPORT_AUTH");
                            int intiPos = Strings.InStr(strAuth, "925");
                            if (intiPos > 0)
                            {
                                strAuth = Strings.Mid(strAuth, intiPos + 4, 1);
                            }
                            else
                            {
                                strAuth = "N";
                            }
                            strsql = "select sc_rate from Security where sc_isincode = '" + Listisin[i].Trim() + "'";
                            strRate = objUtility.OpenDataTable(strsql, sqlCon, objTrans).Rows[0][0].ToString().Trim();
                            strsql = "select count(*) from Auth_master where am_code = '925' and am_amount <= " + strRate + " * " + ListQty[i];
                            strSearch = objUtility.OpenDataTable(strsql, sqlCon, objTrans).Rows[0][0].ToString().Trim();

                            strsql = "select * from Interdepository where id_internalrefno = '" + req.InternalRefNo.Trim() + "'";
                            DataSet dsInterDepository = objUtility.OpenDataSet(sqlDtAdap, strsql, sqlCon, objTrans);
                            DataRow drow;
                            drow = dsInterDepository.Tables[0].NewRow();

                            drow["id_pri_key"] = PrimaryKey;
                            drow["id_internalrefno"] = req.InternalRefNo;
                            drow["id_instcd"] = InstCd;
                            drow["id_trxtype"] = req.TransectionType;
                            drow["id_trxdate"] = req.TransectionDate;
                            drow["id_clientid"] = req.ClientID;
                            drow["id_isin"] = Listisin[i];
                            drow["id_qty"] = ListQty[i];
                            drow["mkrid"] = userId.ToUpper();
                            drow["mkrdt"] = "";
                            drow["id_exec_date"] = req.ExecutionDate;
                            drow["id_status"] = "01";
                            drow["id_remark"] = ListRemark[i].ToUpper();
                            drow["mkrtm"] = "";
                            drow["id_instreceivemode"] = ReceiveMode;
                            drow["id_recoslipyn"] = "N";
                            drow["id_entrymode"] = "E";
                            drow["id_EarlyPayIden"] = ListEarlyPayin[i];
                            drow["id_branchcd"] = req.Branch.ToUpper();
                            drow["id_authdate1"] = "";
                            drow["id_authdate2"] = "";
                            if (ListBoid[i] != "")
                            {
                                drow["id_otherclientid"] = ListBoid[i];
                                drow["id_otherdpid"] = ListDPid[i];
                            }
                            else
                            {
                                drow["id_otherclientid"] = ListDPid[i];
                                drow["id_otherdpid"] = "";
                            }
                            if (ListCounterSettNo[i] != "")
                            {
                                drow["id_settlementno"] = ListCounterSettNo[i];
                                drow["id_other_settno"] = ListFromSettNo[i];
                                drow["id_UCCEXid"] = ListExchange[i];
                                drow["id_SegmentID"] = ListSegment[i].ToUpper();
                                drow["id_UCC"] = ListUCC[i].ToUpper();
                                drow["id_UCCCmid"] = ListCMid[i].ToUpper();
                                drow["id_EntityIden"] = ListEntryBy[i].ToUpper();
                                drow["id_UCCTMCPCode"] = ListTMid[i].ToUpper();
                            }
                            else
                            {
                                drow["id_settlementno"] = "";
                                drow["id_other_settno"] = "";
                                drow["id_UCCEXid"] = "";
                                drow["id_SegmentID"] = "";
                                drow["id_UCC"] = "";
                                drow["id_UCCCmid"] = "";
                                drow["id_EntityIden"] = "";
                                drow["id_UCCTMCPCode"] = "";
                            }
                            if (strAuth == "N")
                            {
                                drow["id_allow"] = "Y";
                                drow["id_authcode1"] = "N";
                                drow["id_authcode2"] = "N";
                                drow["id_authcode3"] = "N";
                            }
                            else
                            {
                                if (Conversion.Val(strSearch) != 0)
                                {
                                    drow["id_allow"] = "N";
                                    drow["id_authcode1"] = "Y";
                                    drow["id_authcode2"] = "Y";
                                    drow["id_authcode3"] = "Y";
                                }
                                else
                                {
                                    drow["id_allow"] = "Y";
                                    drow["id_authcode1"] = "N";
                                    drow["id_authcode2"] = "N";
                                    drow["id_authcode3"] = "N";
                                }
                            }
                            drow["id_authuserid1"] = "";
                            drow["id_authuserid2"] = "";
                            drow["id_authuserid3"] = "";
                            drow["id_authtm1"] = "00:00:00";
                            drow["id_authtm2"] = "00:00:00";
                            drow["id_authtm3"] = "00:00:00";
                            drow["id_cash"] = "X";
                            if (ListReason[i] == "2")
                            {
                                if (ListPaymentMode[i] == "1" || ListPaymentMode[i] == "2")
                                {
                                    drow["id_reasfortrade"] = ListReason[i];
                                    drow["id_PaymentMode"] = ListPaymentMode[i];
                                    drow["id_PayeeName"] = ListPayeeName[i].ToUpper();
                                    drow["id_ChequeNo"] = ListChequeNo[i].ToUpper();
                                    drow["id_Paymentdate"] = ListDateOfIssue[i];
                                    drow["id_BankActNo"] = ListBankAccNo[i].ToUpper();
                                    drow["id_Bankname"] = ListBankName[i].ToUpper();
                                    drow["id_BankBranch"] = ListBranchName[i].ToUpper();
                                    drow["id_Consideration"] = ListConsideration[i];
                                    drow["id_NFiller1"] = StampDuty;
                                    drow["id_Filler1"] = ListPaidBy[i];
                                }
                                else
                                {
                                    drow["id_reasfortrade"] = ListReason[i];
                                    drow["id_PaymentMode"] = ListPaymentMode[i];
                                    drow["id_PayeeName"] = ListPayeeName[i].ToUpper();
                                    drow["id_ChequeNo"] = "";
                                    drow["id_Paymentdate"] = ListDateOfIssue[i];
                                    drow["id_BankActNo"] = "";
                                    drow["id_Bankname"] = "";
                                    drow["id_BankBranch"] = "";
                                    drow["id_Consideration"] = ListConsideration[i];
                                    drow["id_NFiller1"] = StampDuty;
                                    drow["id_Filler1"] = ListPaidBy[i];
                                }
                            }
                            else
                            {
                                drow["id_reasfortrade"] = ListReason[i];
                                drow["id_PaymentMode"] = 0;
                                drow["id_PayeeName"] = "";
                                drow["id_ChequeNo"] = "";
                                drow["id_Paymentdate"] = "";
                                drow["id_BankActNo"] = "";
                                drow["id_Bankname"] = "";
                                drow["id_BankBranch"] = "";
                                drow["id_Consideration"] = 0;
                                drow["id_NFiller1"] = 0;
                                drow["id_Filler1"] = "";
                            }
                            dsInterDepository.Tables[0].Rows.Add(drow);
                            sqlDtAdap.Update(dsInterDepository);
                        }
                        sqlDtAdap.Dispose();
                        mfnInsertUsed_slip(sqlCon, objTrans, req.TransectionType, req.InstrumentType, req.InternalRefNo, req.ExecutionDate, "A", req.ClientID, "", userId.ToUpper(), MkrDt, Mkrtm);
                        objTrans.Commit();
                        strsql = "update Interdepository set mkrdt = CONVERT(varchar, GETDATE(), 112),mkrtm = CONVERT(TIME, GETDATE()) where id_internalrefno = '" + req.InternalRefNo + "'";
                        objUtility.ExecuteSQL(strsql);
                        if (blnIsUpdated)
                        {
                            return "Record Updated.";
                        }
                        return "Record Inserted.";
                    }
                }
                else
                {
                    return fnInterDepositoryAddValidations(req.InstrumentType, req.TransectionType, req.TransectionDate, req.ExecutionDate, req.Branch, req.InternalRefNo, req.ClientID, req.ReceiveMode);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic InterDepositoryFind(string InstrumentType, string InternalRefNo, string TransectionType)
        {
            try
            {
                if (fnFindInterDepositoryValidation(InternalRefNo, InstrumentType) == "Valid")
                {
                    SqlTransaction ObjTrans;
                    DataTable Dt;
                    string ClientCd, PaymentMode, PaidBy, Bo_id, Dp_id, Exch_Desc, Seg_Desc;
                    OffMarketFindResponce objResp = new OffMarketFindResponce();

                    strsql = "select * from Interdepository where id_internalrefno = '" + InternalRefNo + "' and id_instcd = '" + InstrumentType + "' and id_trxtype = '" + TransectionType + "'";
                    Dt = objUtility.OpenDataTable(strsql);
                    if (Dt.Rows.Count == 0)
                    {
                        return "Record not found";
                    }

                    objResp.TransectionDate = Dt.Rows[0]["id_trxdate"].ToString().Trim();
                    objResp.ExecDate = Dt.Rows[0]["id_exec_date"].ToString().Trim();
                    objResp.InternalRefNo = InternalRefNo;
                    ClientCd = Dt.Rows[0]["id_clientid"].ToString().Trim();
                    objResp.ClientID = ClientCd;
                    Bo_id = Dt.Rows[0]["id_otherclientid"].ToString().Trim();
                    Dp_id = Dt.Rows[0]["id_otherdpid"].ToString().Trim();
                    objResp.BranchCode = Dt.Rows[0]["id_branchcd"].ToString().Trim();
                    objResp.InstrumentTypeCode = InstrumentType;
                    Exch_Desc = Dt.Rows[0]["id_UCCEXid"].ToString().Trim();
                    Seg_Desc = Dt.Rows[0]["id_SegmentID"].ToString().Trim();

                    strsql = "select bm_branchname from Branch_master where bm_branchcd = '" + Dt.Rows[0]["id_branchcd"].ToString().Trim() + "'";
                    Dt = objUtility.OpenDataTable(strsql);
                    objResp.BranchName = Dt.Rows[0][0].ToString().Trim();

                    strsql = "select cm_name from Client_master where cm_cd = '" + ClientCd + "'";
                    Dt = objUtility.OpenDataTable(strsql);
                    objResp.ClientName = Dt.Rows[0][0].ToString().Trim();

                    strsql = "select im_desc from Instrument_master where im_instcd = (select distinct id_instcd from interdepository where id_internalrefno = '" + InternalRefNo + "')";
                    Dt = objUtility.OpenDataTable(strsql);
                    objResp.InstrumentType = Dt.Rows[0][0].ToString().Trim();

                    string ConnectionString = objUtility.GetConnectionStr();
                    using (SqlConnection Sqlcon = new SqlConnection(ConnectionString))
                    {
                        Sqlcon.Open();
                        ObjTrans = Sqlcon.BeginTransaction();
                        SqlCommand cmd = Sqlcon.CreateCommand();
                        cmd.Transaction = ObjTrans;
                        SqlDataAdapter sqlDtAdap = new SqlDataAdapter();
                        SqlCommandBuilder sqlCmdBld = new SqlCommandBuilder(sqlDtAdap);

                        fnTempInterDepositoryTable(Sqlcon, ObjTrans);

                        strsql = "insert into #TempInterdepository (Transection_Id,BO_Id,DP_Id,ISIN_Code,Qty,Cash,CounterSett,Flag,Auth,Remark,Time,FromSett,Payment_Mode_Code,PayeeName,Cheque_No,DateOfIssue,BankAccNo,Bank_Name,Branch_Name,Consideration,Stamp_Duty,Paid_By_Code,Exchange,Segment,UCC,CM_Id,Entry_By,TM_Id)  " +
                            "(select id_pri_key , id_otherclientid, id_otherdpid, id_isin, id_qty," +
                            "case (id_cash)when 'X' then 'NONE' end, id_settlementno, id_allow, id_authcode1, id_remark, mkrtm, id_other_settno,id_PaymentMode,id_PayeeName,id_ChequeNo,id_Paymentdate,id_BankActNo,id_Bankname,id_BankBranch,id_consideration,id_NFiller1,id_Filler1,id_UCCEXid,id_SegmentID,id_UCC,id_UCCCmid,id_EntityIden,id_UCCTMCPCode " +
                            "from Interdepository where id_internalrefno = '" + InternalRefNo.Trim() + "' and id_instcd = '" + InstrumentType + "' and id_trxtype = '" + TransectionType + "')";
                        objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                        strsql = "select * from #TempInterdepository";
                        Dt = objUtility.OpenDataTable(strsql, Sqlcon, ObjTrans);

                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            Exch_Desc = Dt.Rows[i]["Exchange"].ToString().Trim();
                            Seg_Desc = Dt.Rows[i]["Segment"].ToString().Trim();

                            strsql = "update #TempInterdepository set ISIN_Name = (select sc_isinname from Security where sc_isincode = '" + Dt.Rows[i]["ISIN_Code"].ToString().Trim() + "') where Transection_Id = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "'"; //ISIN Name
                            objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                            strsql = "update #TempInterdepository set BOId_Name = (select cm_name from Client_master where cm_cd = '" + Dt.Rows[i]["BO_Id"].ToString().Trim() + "')";
                            objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                            strsql = "update #TempInterdepository set Rate = (select sc_rate from Security where sc_isincode = '" + Dt.Rows[i]["ISIN_Code"].ToString().Trim() + "') where Transection_Id = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "'"; //Rate
                            objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                            strsql = "update #TempInterdepository set Value = (Qty * Rate) where Transection_Id = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "'"; //Value
                            objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                            strsql = "update #TempInterdepository set Reason_Code = (select id_reasfortrade from Interdepository where id_internalrefno = '" + InternalRefNo.Trim() + "' and id_pri_key = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "')";
                            objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                            strsql = "update #TempInterdepository set Reason = (select rt_desc from Reasonfortrade where rt_code = " +
                                "(select id_reasfortrade from Interdepository where id_internalrefno = '" + InternalRefNo.Trim() + "' and id_pri_key = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "')) " +
                                "where Transection_Id = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "'"; //Reason
                            objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                            strsql = "select sx_description from statusof_trx where sx_code = (select distinct id_status from Interdepository where id_internalrefno = '" + InternalRefNo.Trim() + "' and id_pri_key = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "')";
                            DataTable Ds = objUtility.OpenDataTable(strsql, Sqlcon, ObjTrans);

                            strsql = "update #TempInterdepository set Status = '" + Ds.Rows[0][0].ToString().Trim() + "' " +
                                "where Transection_Id = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "'"; //Status
                            objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                            strsql = "select id_PaymentMode,id_Filler1 from Interdepository where id_internalrefno = '" + InternalRefNo.Trim() + "'";
                            PaymentMode = objUtility.OpenDataTable(strsql, Sqlcon, ObjTrans).Rows[0]["id_PaymentMode"].ToString().Trim();

                            //strsql = "select id_Filler1 from Interdepository where id_internalrefno = '" + InternalRefNo.Trim() + "'";
                            PaidBy = objUtility.OpenDataTable(strsql, Sqlcon, ObjTrans).Rows[0]["id_Filler1"].ToString().Trim();

                            PaymentMode = fnPaymentMode(PaymentMode);
                            PaidBy = fnPaidBy(PaidBy);
                            Exch_Desc = fnExchangeName(Exch_Desc);
                            Seg_Desc = fnSegmentName(Seg_Desc);

                            strsql = "update #TempInterdepository set Payment_Mode_Desc = '" + PaymentMode + "', Paid_By_Desc = '" + PaidBy + "', Exchange_Desc = '" + Exch_Desc + "', Segment_Desc = '" + Seg_Desc + "' where Transection_Id = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "'";
                            objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);
                        }
                        strsql = "select * from #TempInterdepository";
                        Dt = objUtility.OpenDataTable(strsql, Sqlcon, ObjTrans);

                        foreach (DataColumn col in Dt.Columns)
                        {
                            foreach (DataRow row in Dt.Rows)
                            {
                                if (row.IsNull(col))
                                {
                                    strsql = "update #TempInterdepository set " + col + " = ''";
                                    objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);
                                }
                            }
                        }

                        Dt = fnRTrimTableInterDepository(Sqlcon, ObjTrans);
                        objResp.Data = Dt;
                        return objResp;
                    }
                }
                else
                {
                    return fnFindInterDepositoryValidation(InternalRefNo, InstrumentType);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region Inter Depository Functions
        public void fnUpdateInterDepository(SqlConnection con, SqlTransaction objTrans, string ColumnName, dynamic NewValue, string InternalRefNo, string ClientId, string PrimaryKey)
        {
            try
            {
                strsql = "update Interdepository set " + ColumnName + " = '" + NewValue + "' where id_internalrefno = '" + InternalRefNo + "' and id_clientid = '" + ClientId + "' and id_pri_key = '" + PrimaryKey + "'";
                objUtility.ExecuteSQL(strsql, con, objTrans);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public string fnInterDepositoryAddValidations(string clientCd, string DpId, string BranchCd, string CounterSettNo, string BOId, string ISIN, string TrxDate, string ExecDate, double Qty, string TransectionType, string InstrumentType)
        {
            //For Multiple Rows
            DataTable rstemp;
            if (DpId != "") // DP Id
            {
                if (DpId.Length < 7)
                {
                    DpId = "IN" + Strings.Trim(DpId);
                }
                if (DpId.Length != 8)
                {
                    return "Invalid DP Id";
                }
                else if (DpId.Length == 8)
                {
                    if (Strings.Left(DpId.Trim(), 2) != "IN")
                    {
                        return "Invalid DP Id";
                    }
                    strsql = "Select count(*) from nsdlbpmaster ";
                    rstemp = objUtility.OpenDataTable(strsql);
                    if (rstemp.Rows.Count > 0)
                    {
                        strsql = "select bp_id,bp_name from nsdlbpmaster where bp_role = '04' and bp_id = '" + DpId.Trim() + "'";
                        if (rstemp.Rows.Count == 0)
                        {
                            return "DP Id not found";
                        }
                    }
                }
            }
            if (CounterSettNo != "") // Settlement No
            {
                DateTime TrxDt, PayOutDate;
                TrxDt = DateTime.ParseExact(TrxDate, "yyyyMMdd", null);
                if (GetSysParm("STLMNTPOCKET") == "Y")
                {
                    if (CounterSettNo.Length != 13)
                    {
                        return "Enter 13 Digits Settlement No.";
                    }
                    else
                    {
                        strsql = "select * from Cc_calender  where cc_settle_no = '" + CounterSettNo.Trim() + "' and left(ltrim(cc_settle_no),2) <> '98'";
                        rstemp = objUtility.OpenDataTable(strsql);
                        if (rstemp.Rows.Count > 0)
                        {
                            string PayoutDt = rstemp.Rows[0]["cc_payout_dt"].ToString().Trim();
                            PayOutDate = DateTime.ParseExact(PayoutDt, "yyyyMMdd", null);
                            if (PayOutDate < TrxDt)
                            {
                                //return "Settement is not active.";
                            }
                        }
                        else
                        {
                            return "Settement is not Found.";
                        }
                    }
                }
                else if (CounterSettNo.Length != 9)
                {
                    return "Enter 9 Digits Settlement No.";
                }
            }
            #region Client Code
            /* if (clientCd != "") //client id
            {
                DateTime TrxDt, FreezeDt;
                TrxDt = DateTime.ParseExact(TrxDate, "yyyyMMdd", null);
                int x = Strings.InStr(1, clientCd, ",");
                if (x > 0 || clientCd.Length != 16)
                {
                    clientCd = AutoNumber(clientCd);
                }
                strsql = "select cm_cd, cm_name , cm_freezeyn , cm_freezedt , cm_active ,cm_sech_name ,cm_thih_name,cm_acctype,cm_chgsscheme,cm_allowcredit,cm_poaforpayin from Client_master where cm_cd = '" + clientCd + "'";
                ds = objUtility.OpenDataTable(strsql);
                if (ds.Rows.Count == 0)
                {
                    return "Client Id Not Found [" + clientCd + "]";
                }

                FreezeDt = DateTime.ParseExact(ds.Rows[0]["cm_freezedt"].ToString().Trim(), "yyyyMMdd", null);

                if (ds.Rows[0]["cm_freezeyn"].ToString() != "" || ds.Rows[0]["cm_freezedt"].ToString() != "")
                {
                    if ((ds.Rows[0]["cm_freezeyn"].ToString() == "1" || ds.Rows[0]["cm_freezeyn"].ToString() == "3") && (TrxDt >= FreezeDt))
                    {
                        return "This client [" + clientCd + "] is under Freeze Status";
                    }
                    if ((ds.Rows[0]["cm_freezeyn"].ToString() == "0" || ds.Rows[0]["cm_freezeyn"].ToString() == "2") && (TrxDt < FreezeDt))
                    {
                        return "This client [" + clientCd + "] is under Freeze Status, will be active on" + FreezeDt;
                    }
                }
                if (Strings.InStr(1, "01,02", ds.Rows[0]["cm_active"].ToString(), Constants.vbTextCompare) == 0)
                {
                    return "Client Is Not Active";
                }
                if (ds.Rows[0]["cm_poaforpayin"].ToString() == "N")
                {
                    return "Client [" + clientCd + "] has offered POA for Market Sell Obligation";
                }
            }
            else
            {
                return "Client Id can not be left blank";
            } */
            #endregion
            if (BOId != "") // BO Id
            {
                if (BOId.Length < 8)
                {
                    return "Enter 8 Digits Target Client ID.";
                }
                if (Strings.Left(BOId.Trim(), 2) == "IN")
                {
                    strsql = "Select count(*) from nsdlbpmaster ";
                    rstemp = objUtility.OpenDataTable(strsql);
                    if (rstemp.Rows.Count > 0)
                    {
                        strsql = "select bp_id,bp_name from nsdlbpmaster where bp_role = '01' and bp_id = '" + BOId.Trim() + "'";
                        if (rstemp.Rows.Count == 0)
                        {
                            return "DP Id not found";
                        }
                    }
                }
            }
            else
            {
                return "BO/CM ID Cannot Be Blank.";
            }
            if (ISIN != "") // ISIN No
            {
                if (Strings.Left(ISIN.Trim(), 2) != "IN")
                {
                    ISIN = "IN" + ISIN;
                }
                if (ISIN.Trim().Length < 12)
                {
                    ISIN = fnfindisin(ISIN);
                }
                if (ISIN.Length < 12)
                {
                    return "Invalid ISIN.";
                }
                string res = fncheckisin(ISIN);
                if (res != "Valid")
                {
                    return res;
                }
                strsql = "select * from Holding where hld_ac_code = '" + clientCd + "' and hld_isin_code = '" + ISIN + "' and hld_ac_type = '11' ";
                rstemp = objUtility.OpenDataTable(strsql);
                if (rstemp.Rows.Count == 0)
                {
                    //return "Holding does not exist for " + clientCd + " in " + ISIN + ".";
                }
            }
            else
            {
                return "ISIN Cannot Be Blank.";
            }
            #region Internal Ref No
            /*if (InternalRefNo != "") //internal ref no
            {
                strsql = "select COUNT(*) from Trxbackoffice where tb_internal_refno = '" + InternalRefNo + "'";
                ds = objUtility.OpenDataTable(strsql);
                if (Conversion.Val(ds.Rows[0][0].ToString().Trim()) > 0)
                {
                    return "Internal Ref No Already Present [ " + InternalRefNo + " ]";
                }
                if (InternalRefNo == "0")
                {
                    return "Zero can't be an Internal Ref No";
                }

                strInwardstatus = "N";
                strInwardentry = Strings.Trim(Strings.UCase(GetSysParm("INWARDENTRY")));
                if (strInwardentry != "")
                {
                    intiPos = Strings.InStr(strInwardentry, TransectionType);
                    if (intiPos > 0)
                    {
                        strInwardstatus = Strings.Mid(strInwardentry, intiPos + 4, 1);
                    }
                }
                SlipCheck et = new SlipCheck();
                et.strInwardstatus = strInwardstatus;

                if (strInwardstatus == "A" | strInwardstatus == "O")
                {

                    strsql = "select ie_cmcd,cm_name,ie_lotno,ie_slipno,ie_nooftrx,ie_rejected,ie_mode,";
                    strsql = strsql + " lz_status,ie_execdt, cm_acctype, cm_brboffcode from Inward_entry,Lot_size, ";
                    strsql = strsql + " Instrument_master,Client_master where ie_cmcd = cm_cd ";
                    strsql = strsql + " and ie_lotno = lz_lotno   and ie_trxtype = lz_type ";
                    strsql = strsql + " and ie_instno = im_instcd  ";
                    strsql = strsql + " and ie_slipno = '" + InternalRefNo + "'";
                    strsql = strsql + " and im_instcd = " + InstrumentType;
                    strsql = strsql + " and ie_trxtype= " + Strings.Replace(Strings.Replace(TransectionType, "(", ""), ")", "") + " ";
                    rstemp = objUtility.OpenDataTable(strsql);

                    if (rstemp.Rows[0][0].ToString() != "")
                    {
                        et.strClientcd = Strings.Trim(rstemp.Rows[0]["ie_cmcd"].ToString());
                        et.strClient_Name = Strings.Trim(rstemp.Rows[0]["cm_name"].ToString());
                        et.strClient_type = Strings.Trim(rstemp.Rows[0]["cm_acctype"].ToString());
                        et.strbranchcd = Strings.Trim(rstemp.Rows[0]["cm_brboffcode"].ToString());
                        et.strExecution_date = Strings.Trim(rstemp.Rows[0]["ie_execdt"].ToString());
                        et.lngLotno = rstemp.Rows[0]["ie_lotno"].ToString();
                        et.intLotsize = Interaction.IIf(Convert.ToInt32(rstemp.Rows[0]["ie_nooftrx"].ToString()) - Convert.ToInt32(rstemp.Rows[0]["ie_rejected"].ToString()) == 0, 30000, Convert.ToInt32(rstemp.Rows[0]["ie_nooftrx"].ToString()) - Convert.ToInt32(rstemp.Rows[0]["ie_rejected"].ToString())).ToString();
                        et.strSlipMode = rstemp.Rows[0]["ie_mode"].ToString();
                    }
                    else if (strInwardstatus == "A")
                    {
                        return "Inward entry not found for current slip";
                    }
                }
                et.strInwardstatus = strInwardstatus;
                strsql = "SELECT ";
                strsql = strsql + " us_trxtype, us_instcd, us_irn,us_clientcd, us_execdt ,us_archiveyn ";
                strsql = strsql + " FROM ";
                strsql = strsql + " Used_slip ";
                strsql = strsql + " WHERE ";
                strsql = strsql + " us_instcd='" + InstrumentType + "' and us_irn = " + InternalRefNo;
                rstemp = objUtility.OpenDataTable(strsql);

                if (rstemp.Rows.Count == 0)
                {
                    strsql = "select chm_cmcd, chm_chqno, chm_instcd, chm_status, chm_branchcd,isNull(chm_allow,'') chm_allow,isNull(chm_issuedate,'') chm_issuedate from Chequemaster where ";
                    strsql = strsql + InternalRefNo + " between chm_chqno and chm_chqno + chm_booksize -1 and chm_instcd = '" + InstrumentType + "'";
                    rsCheck1 = objUtility.OpenDataTable(strsql);

                    if (rsCheck1.Rows[0][0].ToString() != "")
                    {
                        if (rsCheck1.Rows[0]["chm_status"].ToString() == "N")
                        {
                            return "Slip Not Issued";
                        }
                        else if (rsCheck1.Rows[0]["chm_status"].ToString() == "D")
                        {
                            return "Slip Destroyed";
                        }
                        else if (rsCheck1.Rows[0]["chm_status"].ToString() == "A" | rsCheck1.Rows[0]["chm_status"].ToString() == "L")
                        {
                            return "Used as Loose Slip";
                        }
                        else if (rsCheck1.Rows[0]["chm_status"].ToString() == "B")
                        {
                            return "Slip is Issued to Branch";
                        }
                        else if (rsCheck1.Rows[0]["chm_status"].ToString() == "P")
                        {
                            strsql = "select chs_status from Chequestop where chs_chqno =" + InternalRefNo;
                            strsql = strsql + " and chs_instcd=" + InstrumentType;
                            rstemp = objUtility.OpenDataTable(strsql);

                            if (rstemp.Rows[0][0].ToString() != "")
                            {
                                if (rstemp.Rows[0]["chs_status"].ToString() == "S")
                                {
                                    return "Slip is Stop";
                                }
                                else if (rstemp.Rows[0]["chs_status"].ToString() == "D")
                                {
                                    return "Slip is Destroyed";
                                }
                            }
                        }
                        else if (rsCheck1.Rows[0]["chm_status"].ToString() == "I")
                        {
                            strsql = "select chs_status from Chequestop where chs_chqno =" + InternalRefNo;
                            strsql = strsql + " and chs_instcd=" + InstrumentType;
                            rstemp = objUtility.OpenDataTable(strsql);

                            if (rstemp.Rows[0][0].ToString() != "")
                            {
                                if (rstemp.Rows[0]["chs_status"].ToString() == "S")
                                {
                                    return "Slip is Stop";
                                }
                                else if (rstemp.Rows[0]["chs_status"].ToString() == "D")
                                {
                                    return "Slip is Destroyed";
                                }
                            }
                        }
                        else if (rsCheck1.Rows[0]["chm_status"].ToString() == "R")
                        {
                            return "Slip has been Sent to Printer";
                        }
                    }
                    else
                    {
                        intiPos = 0;
                        strSlipMaintanace = Strings.Trim(Strings.UCase(GetSysParm("SLIPMNT")));
                        if (strSlipMaintanace != "")
                        {
                            intiPos = Strings.InStr(strSlipMaintanace, TransectionType);
                            if (intiPos > 0)
                            {
                                strSlipmnt = Strings.Mid(strSlipMaintanace, intiPos + 4, 1);
                            }
                        }

                        if (strSlipmnt == "F")
                        {
                            return "Couldn't find slip no in Stock";
                        }
                        else if (strSlipmnt == "P")
                        {
                            return "Couldn't find slip number in Stock";
                        }
                        else if (strSlipmnt != "N")
                        {
                            return "Invalid system parameter found in slip maintenance";
                        }
                    }
                    if (et.strClientcd != "")
                    {
                        strPROMPTSEQ = GetSysParm("PROMPTSEQ");
                        if (strPROMPTSEQ == "Y")
                        {
                            if (rstemp.Rows[0][0].ToString() != "")
                            {
                                if (rstemp.Rows[0][0].ToString() != "")
                                {
                                    if (rstemp.Rows[0]["chm_chqno"].ToString() != InternalRefNo)
                                    {
                                        return "Not using Slip in sequence";
                                    }
                                }
                                else
                                {
                                    return "Not using Slip in sequence";
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                return "Internal Ref No Cannot Be Blank.";
            }*/
            #endregion
            if (Qty != 0) // Qty
            {
                strsql = "select hld_ac_pos from Holding where hld_ac_code = '" + clientCd + "' and hld_ac_type = '11' and  hld_isin_code = '" + ISIN + "'";
                rstemp = objUtility.OpenDataTable(strsql);
                if (rstemp.Rows.Count > 0)
                {
                    if (Conversion.Val(rstemp.Rows[0]["hld_ac_pos"].ToString().Trim()) < Qty)
                    {
                        //return "Quantity exceeds actual holding [ " + rstemp.Rows[0]["hld_ac_pos"].ToString().Trim() + " ].";
                    }
                    double Holding = Convert.ToDouble(rstemp.Rows[0][0].ToString().Trim());
                    if (fnValidateFreeze(1, Holding, clientCd, ISIN, Qty) != "Valid")
                    {
                        return fnValidateFreeze(1, Holding, clientCd, ISIN, Qty);
                    }
                }
            }
            else
            {
                return "Qty Cannot Be Blank.";
            }
            return "Valid";
        }
        public string fnInterDepositoryAddValidations(string InstrumentType, string TransectionType, string TrxDate, string ExecDate, string BranchCd, string InternalRefNo, string clientCd, string ReceiveMode)
        {
            //For Only Once
            DataTable ds;
            DataTable rstemp;
            string strInwardentry, strInwardstatus;
            int intiPos;
            if (InstrumentType != "") //Instrument Type
            {
                if (InstrumentType != "11" && InstrumentType != "3" && InstrumentType != "103" && InstrumentType != "111")
                {
                    return "Invalid Instrument Type";
                }
            }
            else
            {
                return "Instrument Type Cannot Be Blank";
            }
            if (TransectionType != "") // Transection Type
            {
                if (TransectionType != "925")
                {
                    return "Invalid Transection Type";
                }
            }
            else
            {
                return "Transection Type Cannot Be Blank";
            }
            if (ReceiveMode != "") // Receive Mode
            {
                if (ReceiveMode != "0" && ReceiveMode != "1" && ReceiveMode != "2" && ReceiveMode != "3" && ReceiveMode != "4" && ReceiveMode != "5" && ReceiveMode != "6" && ReceiveMode != "7" && ReceiveMode != "8" && ReceiveMode != "9")
                {
                    return "Invalid Receive Mode";
                }
            }
            else
            {
                return "Receive Mode Cannot Be Blank";
            }
            if (InternalRefNo != "") //internal ref no
            {
                strsql = "select COUNT(*) from Interdepository where id_internalrefno = '" + InternalRefNo + "'";
                ds = objUtility.OpenDataTable(strsql);
                if (Conversion.Val(ds.Rows[0][0].ToString().Trim()) > 0)
                {
                    return "Internal Ref No Already Present";
                }
                if (InternalRefNo == "0")
                {
                    return "Zero can't be an Internal Ref No";
                }

                strInwardstatus = "N";
                strInwardentry = Strings.Trim(Strings.UCase(GetSysParm("INWARDENTRY")));
                if (strInwardentry != "")
                {
                    intiPos = Strings.InStr(strInwardentry, TransectionType);
                    if (intiPos > 0)
                    {
                        strInwardstatus = Strings.Mid(strInwardentry, intiPos + 4, 1);
                    }
                }
                SlipCheck et = new SlipCheck();
                et.strInwardstatus = strInwardstatus;

                if (strInwardstatus == "A" | strInwardstatus == "O")
                {

                    strsql = "select ie_cmcd,cm_name,ie_lotno,ie_slipno,ie_nooftrx,ie_rejected,ie_mode,";
                    strsql = strsql + " lz_status,ie_execdt, cm_acctype, cm_brboffcode from Inward_entry,Lot_size, ";
                    strsql = strsql + " Instrument_master,Client_master where ie_cmcd = cm_cd ";
                    strsql = strsql + " and ie_lotno = lz_lotno   and ie_trxtype = lz_type ";
                    strsql = strsql + " and ie_instno = im_instcd  ";
                    strsql = strsql + " and ie_slipno = '" + InternalRefNo + "'";
                    strsql = strsql + " and im_instcd = " + InstrumentType;
                    strsql = strsql + " and ie_trxtype= " + Strings.Replace(Strings.Replace(TransectionType, "(", ""), ")", "") + " ";
                    rstemp = objUtility.OpenDataTable(strsql);

                    if (rstemp.Rows[0][0].ToString() != "")
                    {
                        et.strClientcd = Strings.Trim(rstemp.Rows[0]["ie_cmcd"].ToString());
                        et.strClient_Name = Strings.Trim(rstemp.Rows[0]["cm_name"].ToString());
                        et.strClient_type = Strings.Trim(rstemp.Rows[0]["cm_acctype"].ToString());
                        et.strbranchcd = Strings.Trim(rstemp.Rows[0]["cm_brboffcode"].ToString());
                        et.strExecution_date = Strings.Trim(rstemp.Rows[0]["ie_execdt"].ToString());
                        et.lngLotno = rstemp.Rows[0]["ie_lotno"].ToString();
                        et.intLotsize = Interaction.IIf(Convert.ToInt32(rstemp.Rows[0]["ie_nooftrx"].ToString()) - Convert.ToInt32(rstemp.Rows[0]["ie_rejected"].ToString()) == 0, 30000, Convert.ToInt32(rstemp.Rows[0]["ie_nooftrx"].ToString()) - Convert.ToInt32(rstemp.Rows[0]["ie_rejected"].ToString())).ToString();
                        et.strSlipMode = rstemp.Rows[0]["ie_mode"].ToString();
                    }
                    else if (strInwardstatus == "A")
                    {
                        return "Inward entry not found for current slip";
                    }
                }
            }
            else
            {
                return "Internal Ref No Cannot Be Blank.";
            }
            if (TrxDate == "") // Transection Date
            {
                return "Please Enter Transection Date";
            }
            if (ExecDate == "") // Execution Date
            {
                return "Please Enter Execute Date";
            }
            if (TrxDate != "" && ExecDate != "")
            {
                DateTime ExecDt, TrxDt;
                ExecDt = DateTime.ParseExact(ExecDate, "yyyyMMdd", null);
                TrxDt = DateTime.ParseExact(TrxDate, "yyyyMMdd", null);
                if (ExecDt < TrxDt)
                {
                    return "Transection Date Can't Be Greater Than Execution Date";
                }
            }
            if (clientCd != "") //client id
            {
                DateTime TrxDt, FreezeDt;
                TrxDt = DateTime.ParseExact(TrxDate, "yyyyMMdd", null);
                int x = Strings.InStr(1, clientCd, ",");
                if (x > 0 || clientCd.Length != 16)
                {
                    clientCd = AutoNumber(clientCd);
                }
                strsql = "select cm_cd, cm_name , cm_freezeyn , cm_freezedt , cm_active ,cm_sech_name ,cm_thih_name,cm_acctype,cm_chgsscheme,cm_allowcredit,cm_poaforpayin from Client_master where cm_cd = '" + clientCd + "'";
                ds = objUtility.OpenDataTable(strsql);
                if (ds.Rows.Count == 0)
                {
                    return "Client Id Not Found";
                }

                FreezeDt = DateTime.ParseExact(ds.Rows[0]["cm_freezedt"].ToString().Trim(), "yyyyMMdd", null);

                if (ds.Rows[0]["cm_freezeyn"].ToString() != "" || ds.Rows[0]["cm_freezedt"].ToString() != "")
                {
                    if ((ds.Rows[0]["cm_freezeyn"].ToString() == "1" || ds.Rows[0]["cm_freezeyn"].ToString() == "3") && (TrxDt >= FreezeDt))
                    {
                        //return "This client [" + clientCd + "] is under Freeze Status";
                    }
                    if ((ds.Rows[0]["cm_freezeyn"].ToString() == "0" || ds.Rows[0]["cm_freezeyn"].ToString() == "2") && (TrxDt < FreezeDt))
                    {
                        //return "This client [" + clientCd + "] is under Freeze Status, will be active on" + FreezeDt;
                    }
                }
                if (Strings.InStr(1, "01,02", ds.Rows[0]["cm_active"].ToString(), Constants.vbTextCompare) == 0)
                {
                    return "Client Is Not Active";
                }
                if (ds.Rows[0]["cm_poaforpayin"].ToString() == "N")
                {
                    //return "Client [" + clientCd + "] has offered POA for Market Sell Obligation";
                }
            }
            else
            {
                return "Client Id can not be left blank";
            }
            if (BranchCd != "") //Branch
            {
                strsql = "select count(*) from Branch_master where bm_branchcd = '" + BranchCd + "'";
                ds = objUtility.OpenDataTable(strsql);
                if (ds.Rows[0][0].ToString() == "0")
                {
                    return "Invalid Branch Code [ " + BranchCd + " ]";
                }
            }
            else
            {
                return "Please Enter Branch Code";
            }
            return "Valid";
        }
        public string fnInterDepositoryDropDownValid(string ConterSettNo, string PaymentMode, string PaidBy, string EntryBy, string EarlyPayin, string Reason, string Segment, string Exchange, string UCC, string CMid, string TMid)
        {
            DataTable Dt;
            if (PaymentMode != "")
            {
                if (PaymentMode != "1" && PaymentMode != "2" && PaymentMode != "3")
                {
                    return "Invalid Payment Mode";
                }
            }
            else if (PaymentMode == "" && Reason == "2")
            {
                return "Payment Mode Cannot Be Blank";
            }
            if (PaidBy != "")
            {
                if (PaidBy != "0" && PaidBy != "1")
                {
                    return "Invalid Paid By Code";
                }
            }
            else if (PaidBy == "" && Reason == "2")
            {
                return "Paid By Cannot Be blank";
            }
            if (EntryBy != "")
            {
                if (EntryBy != "TM" && EntryBy != "CP")
                {
                    return "Invalid Entry By Code";
                }
            }
            else if (EntryBy == "" && ConterSettNo != "")
            {
                return "Entry By Cannot Be Blank";
            }
            if (EarlyPayin != "")
            {
                if (EarlyPayin != "Y" && EarlyPayin != "N")
                {
                    return "Invalid Early Payin";
                }
            }
            if (Reason != "")
            {
                strsql = "select * from Reasonfortrade where rt_code = '" + Reason + "'";
                Dt = objUtility.OpenDataTable(strsql);
                if (Dt.Rows.Count == 0)
                {
                    return "Invalid Reason Code";
                }
            }
            else
            {
                return "Reason Cannot Be Blank";
            }
            if (Segment != "")
            {
                strsql = "select * from Clientsub_master where cs_module ='CS26' and cs_code = '" + Segment + "'";
                Dt = objUtility.OpenDataTable(strsql);
                if (Dt.Rows.Count == 0)
                {
                    return "Invalid Segment Code";
                }
            }
            else if (Segment == "" && ConterSettNo != "")
            {
                return "Segment Code Cannot Be Blank";
            }
            if (Exchange != "")
            {
                strsql = "select " +
                         "a.bp_id 'STOCK EXCHENGE', " +
                         "a.bp_name 'NAME', " +
                         "a.bp_assd_cc_cmid 'CC ID', " +
                         "p.bp_name 'NAME' " +
                         "from Bpmaster a, Bpmaster p " +
                         "where a.bp_id = p.bp_id and a.bp_role = '02' and p.bp_role = '02' and a.bp_id = '" + Exchange + "'";
                Dt = objUtility.OpenDataTable(strsql);
                if (Dt.Rows.Count == 0)
                {
                    return "Invalid Exchange Code";
                }
            }
            else if (Exchange == "" && ConterSettNo != "")
            {
                return "Exchange Code Cannot Be Blank";
            }
            if (UCC != "")
            {
                strsql = "select " +
                     "Case cud_Exchnge when '11' then 'BSE' when '12' then 'NSE' when '23' then 'MCX' when '22' then 'NCDEX' else cud_Exchnge end as 'Exchange', " +
                     "Case cud_segment when '01' then 'Cash' when '02' then 'F&O' when '03' then 'Currency' when '04' then 'SLB' when '05' then 'Commodity' when '06' then 'Debt' else cud_segment end as 'Segment', " +
                     "cud_UCC, " +
                     "cud_cmid, " +
                     "cud_tmid " +
                     "from Client_UCC_Details where cud_UCC = '" + UCC + "'";
                Dt = objUtility.OpenDataTable(strsql);
                if (Dt.Rows.Count == 0)
                {
                    return "Invalid UCC Code";
                }
            }
            else if (UCC == "" && ConterSettNo != "")
            {
                return "UCC Code Cannot Be Blank";
            }
            if (CMid != "")
            {
                strsql = "select " +
                     "Case cud_Exchnge when '11' then 'BSE' when '12' then 'NSE' when '23' then 'MCX' when '22' then 'NCDEX' else cud_Exchnge end as 'Exchange', " +
                     "Case cud_segment when '01' then 'Cash' when '02' then 'F&O' when '03' then 'Currency' when '04' then 'SLB' when '05' then 'Commodity' when '06' then 'Debt' else cud_segment end as 'Segment', " +
                     "cud_UCC, " +
                     "cud_cmid, " +
                     "cud_tmid " +
                     "from Client_UCC_Details where cud_UCC = '" + UCC + "' and cud_cmid = '" + CMid + "'";
                Dt = objUtility.OpenDataTable(strsql);
                if (Dt.Rows.Count == 0)
                {
                    return "Invalid CM Id Code";
                }
            }
            else if (CMid == "" && ConterSettNo != "")
            {
                return "CM id Code Cannot Be Blank";
            }
            if (TMid != "")
            {
                strsql = "select " +
                     "Case cud_Exchnge when '11' then 'BSE' when '12' then 'NSE' when '23' then 'MCX' when '22' then 'NCDEX' else cud_Exchnge end as 'Exchange', " +
                     "Case cud_segment when '01' then 'Cash' when '02' then 'F&O' when '03' then 'Currency' when '04' then 'SLB' when '05' then 'Commodity' when '06' then 'Debt' else cud_segment end as 'Segment', " +
                     "cud_UCC, " +
                     "cud_cmid, " +
                     "cud_tmid " +
                     "from Client_UCC_Details where cud_UCC = '" + UCC + "' and cud_tmid = '" + TMid + "'";
                Dt = objUtility.OpenDataTable(strsql);
                if (Dt.Rows.Count == 0)
                {
                    return "Invalid TM Id Code";
                }
            }
            else if (TMid == "" && ConterSettNo != "")
            {
                return "TM id Code Cannot Be Blank";
            }
            return "Valid";
        }
        public string fncheckisin(string ISIN)
        {
            string strSQL;
            strSQL = "select sc_isinname,sc_security_status,sc_rate , sc_bsesymbol,sc_decimal_allow from Security ";
            strSQL = strSQL + " where sc_isincode = '" + ISIN + "'";
            DataTable rstemp = objUtility.OpenDataTable(strSQL);
            if (rstemp.Rows.Count > 0)
            {
                if (rstemp.Rows[0]["sc_security_status"].ToString().Trim() != "01")
                {
                    //return "ISIN is not active.";
                }
                if (rstemp.Rows[0]["sc_security_status"].ToString().Trim() == "01" && rstemp.Rows[0]["sc_bsesymbol"].ToString().Trim() == "NON NSDL")
                {
                    //return "Not Listed In NSDL.";
                }
                if (rstemp.Rows[0]["sc_security_status"].ToString().Trim() != "01" && rstemp.Rows[0]["sc_bsesymbol"].ToString().Trim() == "NSDL")
                {
                    //return "This, NSDL listed ISIN Is Not Active.";
                }
                if (rstemp.Rows[0]["sc_security_status"].ToString().Trim() != "01" && rstemp.Rows[0]["sc_bsesymbol"].ToString().Trim() == "NON NSDL")
                {
                    //return "ISIN Is Not Active And also Not Listed IN NSDL.";
                }
                return "Valid";
            }
            else
            {
                return "Couldn't find ISIN.";
            }
        }
        private string fnValidateFreeze(int index, double dblHolding, string ClientCd, string ISIN, double Qty)
        {
            double dblFreezeQty;
            switch (index)
            {
                case 0: // ISIN Freeze Checking
                    {
                        if (mfnCheckIsinFreeze(ClientCd, ISIN, "") == true)
                        {
                            //return "ISIN is under Freeze Status.";
                        }
                        break;
                    }
                case 1:
                    {
                        if (dblHolding != 0)
                        {
                            dblFreezeQty = mfnGetFreezeIsinQty(ClientCd, ISIN, "");
                            if (dblFreezeQty > 0)
                            {
                                if (Qty > (Conversion.Val(dblHolding) - dblFreezeQty))
                                {
                                    //return "Quantity exceeds allowed limit [" + (Conversion.Val(dblHolding) - dblFreezeQty) + "].";
                                }
                            }
                        }
                        break;
                    }
            }
            return "Valid";
        }
        public bool mfnCheckIsinFreeze(string strClient, string strisin, string strBranchCode)
        {
            DataTable rsFreeze;
            strsql = "Select count(*) from freeze_isin ";
            strsql = strsql + " Where fi_clientcd = '" + strClient + "'";
            strsql = strsql + " and fi_isin_code = '" + strisin + "'";
            if (Strings.Trim(strBranchCode) != "")
                strsql = strsql + " and fi_branchcd = '" + strBranchCode + "'";
            strsql = strsql + " and fi_level = '03' and fi_trxtype = '936'";
            strsql = strsql + " and fi_mode = 'A'";

            rsFreeze = objUtility.OpenDataTable(strsql);
            if (rsFreeze.Rows.Count > 0)
                return true;
            else
                return false;
        }
        public double mfnGetFreezeIsinQty(string strClient, string strisin, string strBranchCode)
        {
            DataTable rsFreeze;
            strsql = "Select isnull(sum(a.fi_pendingqty),0) from freeze_isin a ";
            strsql = strsql + " Where a.fi_clientcd = '" + strClient + "'";
            strsql = strsql + " and a.fi_isin_code = '" + strisin + "'";
            if (Strings.Trim(strBranchCode) != "")
                strsql = strsql + " and a.fi_branchcd = '" + strBranchCode + "'";
            strsql = strsql + " and a.fi_level = '04' and a.fi_trxtype = '936'";
            strsql = strsql + " and a.fi_mode = 'A'";
            strsql = strsql + " and a.fi_clientcd not in (";
            strsql = strsql + " Select b.fi_clientcd from Freeze_isin b ";
            strsql = strsql + " where b.fi_clientcd = a.fi_clientcd ";
            strsql = strsql + " and b.fi_isin_code = a.fi_isin_code ";
            strsql = strsql + " and b.fi_level = '03' and b.fi_trxtype = '936'";
            strsql = strsql + " and b.fi_mode = a.fi_mode)";
            rsFreeze = objUtility.OpenDataTable(strsql);

            if (rsFreeze.Rows.Count > 0)
                return Convert.ToDouble(rsFreeze.Rows[0][0].ToString().Trim());
            else
                return 0;
        }
        public string fnFindInterDepositoryValidation(string InternalRefNo, string InstrumentType)
        {
            DataTable dt;
            if (InternalRefNo != null)
            {
                strsql = "select * from Interdepository where id_internalrefno = '" + InternalRefNo + "'";
                dt = objUtility.OpenDataTable(strsql);

                if (dt.Rows.Count == 0)
                {
                    return "Invalid Internal Reference No.";
                }
            }
            else
            {
                return "Internal Reference No. Cannot Be Blank.";
            }
            if (InstrumentType != null)
            {
                strsql = "select COUNT(*) from Instrument_master where im_instcd = " + InstrumentType;
                dt = objUtility.OpenDataTable(strsql);
                if (Convert.ToInt16(dt.Rows[0][0].ToString()) == 0)
                {
                    return "Invalid Instrument Type.";
                }
            }
            else
            {
                return "Instrument Type Cannot Be Blank.";
            }
            return "Valid";
        }
        public void fnTempInterDepositoryTable(SqlConnection Con, SqlTransaction objTrans)
        {
            strsql = "create table #TempInterdepository (" +
                     "Sr_No int Identity(1,1)," +
                     "Transection_Id char(8)," +
                     "BO_Id char(16)," +
                     "BOId_Name char(30)," +
                     "DP_Id char(16)," +
                     "ISIN_Code char(12)," +
                     "ISIN_Name char(30)," +
                     "Qty numeric(18,3)," +
                     "Rate money," +
                     "Value money," +
                     "Cash char(4)," +
                     "CounterSett char(13)," +
                     "Reason_Code char(4)," +
                     "Reason char(30)," +
                     "Flag char(1)," +
                     "Charges money," +
                     "Auth char(1)," +
                     "Trade_No char(16)," +
                     "Status char(30)," +
                     "Remark varchar(50)," +
                     "Time char(8)," +
                     "FromSett char(13)," +
                     "Payment_Mode_Code char(1)," +
                     "Payment_Mode_Desc char(20)," +
                     "PayeeName char(30)," +
                     "Cheque_No char(15)," +
                     "DateOfIssue char(8)," +
                     "BankAccNo char(30)," +
                     "Bank_Name char(30)," +
                     "Branch_Name char(30)," +
                     "Consideration money," +
                     "Stamp_Duty char(15)," +
                     "Paid_By_Code char(1)," +
                     "Paid_By_Desc char(25)," +
                     "Exchange char(15)," +
                     "Exchange_Desc char(50)," +
                     "Segment char(15)," +
                     "Segment_Desc char(15)," +
                     "UCC char(30)," +
                     "CM_Id char(8)," +
                     "Entry_By char(15)," +
                     "TM_Id char(30)" +
                     ")";
            objUtility.ExecuteSQL(strsql, Con, objTrans);
        }
        public dynamic fnRTrimTableInterDepository(SqlConnection Con, SqlTransaction objTrans)
        {
            strsql = "select " +
                     "rtrim(Sr_No) Sr_No," +
                     "rtrim(Transection_Id) Transection_Id," +
                     "rtrim(BO_Id) BO_Id," +
                     "rtrim(BOId_Name) BOId_Name," +
                     "rtrim(DP_Id) DP_Id," +
                     "rtrim(ISIN_Code) ISIN_Code," +
                     "rtrim(ISIN_Name) ISIN_Name," +
                     "rtrim(Qty) Qty," +
                     "Rate," +
                     "Value," +
                     "rtrim(Cash) Cash," +
                     "rtrim(CounterSett) Counter_Sett_No," +
                     "rtrim(Reason_Code) Reason_Code," +
                     "rtrim(Reason) Reason," +
                     "rtrim(Flag) Flag," +
                     "rtrim(Charges) Charges," +
                     "rtrim(Trade_No) Trade_No," +
                     "rtrim(Status) Status," +
                     "rtrim(Remark) Remark," +
                     "rtrim(Time) Time," +
                     "rtrim(FromSett) From_Sett_No," +
                     "rtrim(Payment_Mode_Code) Payment_Mode_Code," +
                     "rtrim(Payment_Mode_Desc) Payment_Mode_Desc," +
                     "rtrim(PayeeName) Payee_Name," +
                     "rtrim(Cheque_No) Cheque_No," +
                     "rtrim(DateOfIssue) Date_Of_Issue," +
                     "rtrim(BankAccNo) Bank_Acc_No," +
                     "rtrim(Bank_Name) Bank_Name," +
                     "rtrim(Branch_Name) Branch_Name," +
                     "Consideration," +
                     "rtrim(Stamp_Duty) Stamp_Duty," +
                     "rtrim(Paid_By_Code) Paid_By_Code," +
                     "rtrim(Paid_By_Desc) Paid_By_Desc," +
                     "rtrim(Exchange) Exchange," +
                     "rtrim(Exchange_Desc) Exchange_Desc," +
                     "rtrim(Segment) Segment," +
                     "rtrim(Segment_Desc) Segment_Desc," +
                     "rtrim(UCC) UCC," +
                     "rtrim(CM_Id) CM_Id," +
                     "rtrim(Entry_By) Entry_By," +
                     "rtrim(TM_Id) TM_Id" +
                     " from #TempInterdepository order by Sr_No";
            DataTable Dt = objUtility.OpenDataTable(strsql, Con, objTrans);
            return Dt;
        }
        #endregion

        public dynamic OnMarketAdd(string UserId, OnMarketReq req)
        {
            try
            {
                DataTable Ds;
                var db = new DataContext();
                SqlTransaction objTrans;
                string MkrDate, CurrentTime, ReceiveMode, InstCd, strAuth, strRate, Settlement, strSearch, MarketType, ExchangeId, ChId;
                int PrimaryKey;
                bool blnIsUpdated = false;
                MkrDate = DateTime.Now.ToString("yyyyMMdd");
                CurrentTime = DateTime.Now.ToString("hh:mm:ss");
                int instcd = Convert.ToInt32(req.InstrumentType);
                double CheqNo = Conversion.Val(req.InternalRefNo);
                #region Data List
                List<string> ListSettNo = req.Data.Select(x => x.SettlementID).ToList();
                List<string> ListCMID = req.Data.Select(x => x.ClientID).ToList();
                List<string> ListISIN = req.Data.Select(x => x.ISIN).ToList();
                List<double> ListQty = req.Data.Select(x => x.Qty).ToList();
                List<decimal> ListObligNo = req.Data.Select(x => x.ObligNo).ToList();
                List<decimal> ListSerialNo = req.Data.Select(x => x.SerialNo).ToList();
                List<string> ListRemark = req.Data.Select(x => x.Remark).ToList();
                List<string> ListExchange = req.Data.Select(x => x.Exchange).ToList();
                List<string> ListSegment = req.Data.Select(x => x.Segment).ToList();
                List<string> ListUCC = req.Data.Select(x => x.UCC).ToList();
                List<string> ListCMId = req.Data.Select(x => x.CMId).ToList();
                List<string> ListEntryBy = req.Data.Select(x => x.EntryBy).ToList();
                List<string> ListTMId = req.Data.Select(x => x.TMId).ToList();
                #endregion
                if (OnMarketValidation(req.InstrumentType, req.TransectionType, req.TransectionDate, req.BranchCode, req.InternalRefNo, req.ClientID, req.ReceiveMode) == "Valid" || OnMarketValidation(req.InstrumentType, req.TransectionType, req.TransectionDate, req.BranchCode, req.InternalRefNo, req.ClientID, req.ReceiveMode) == "Internal Ref No Already Present")
                {
                    if (mfnSlipCheck(req.ClientID, req.TransectionType, instcd, CheqNo) != "Valid")
                    {
                        return mfnSlipCheck(req.ClientID, req.TransectionType, instcd, CheqNo);
                    }
                    string Connection = objUtility.GetConnectionStr();
                    using (SqlConnection sqlCon = new SqlConnection(Connection))
                    {
                        sqlCon.Open();
                        objTrans = sqlCon.BeginTransaction();
                        SqlDataAdapter sqlDtAdap = new SqlDataAdapter();
                        SqlCommandBuilder sqlCmdBld = new SqlCommandBuilder(sqlDtAdap);

                        if (OnMarketValidation(req.InstrumentType, req.TransectionType, req.TransectionDate, req.BranchCode, req.InternalRefNo, req.ClientID, req.ReceiveMode) == "Internal Ref No Already Present")
                        {
                            strsql = "select * from Trxbackoffice where tb_internal_refno = '" + req.InternalRefNo + "'";
                            Ds = objUtility.OpenDataTable(strsql);
                            if (Ds.Rows[0]["tb_trx_allow"].ToString().Trim() == "E")
                            {
                                return "Data Already Exported.";
                            }
                            else if (Ds.Rows[0]["tb_trx_allow"].ToString().Trim() == "S")
                            {
                                return "Data Already Authorised.";
                            }
                            else
                            {
                                strsql = "delete from Trxbackoffice where tb_internal_refno = '" + req.InternalRefNo + "'";
                                objUtility.ExecuteSQL(strsql, sqlCon, objTrans);
                                blnIsUpdated = true;
                            }
                        }

                        for (int i = 0; i < req.Data.Count; i++)
                        {
                            if (OnMarketValidation(req.ClientID, ListSettNo[i], req.TransectionDate, ListCMID[i], ListISIN[i], ListQty[i], ListExchange[i], ListSegment[i], ListUCC[i], ListCMId[i], ListEntryBy[i], ListTMId[i]) != "Valid")
                            {
                                return OnMarketValidation(req.ClientID, ListSettNo[i], req.TransectionDate, ListCMID[i], ListISIN[i], ListQty[i], ListExchange[i], ListSegment[i], ListUCC[i], ListCMId[i], ListEntryBy[i], ListTMId[i]);
                            }
                            Settlement = ListSettNo[i];
                            char IsChar = Convert.ToChar(Settlement.Substring(0, 1));
                            if (!Char.IsDigit(IsChar))
                            {
                                MarketType = IsChar.ToString();
                                ExchangeId = "12";
                                ChId = "11";
                            }
                            else
                            {
                                MarketType = Settlement.Substring(4, 2);
                                ExchangeId = Settlement.Substring(0, 2);
                                ChId = Settlement.Substring(2, 2);
                            }
                            ReceiveMode = fnReceiveMode(req.ReceiveMode.Trim(), "OffMarket");

                            InstCd = req.InstrumentType;

                            strsql = "select MAX(tb_pri_key) from Trxbackoffice";
                            Ds = objUtility.OpenDataTable(strsql, sqlCon, objTrans);
                            PrimaryKey = Convert.ToInt32(Ds.Rows[0][0].ToString().Trim()) + 1;

                            strAuth = GetSysParm("IMPORT_AUTH");
                            strsql = "select sc_rate from Security where sc_isincode = '" + ListISIN[i].Trim() + "'";
                            strRate = objUtility.OpenDataTable(strsql, sqlCon, objTrans).Rows[0][0].ToString().Trim();
                            strsql = "select count(*) from Auth_master where am_code = '906' and am_amount <= " + strRate + " * " + ListQty[i];
                            strSearch = objUtility.OpenDataTable(strsql, sqlCon, objTrans).Rows[0][0].ToString().Trim();

                            strsql = "select * from Trxbackoffice where tb_internal_refno = '" + req.InternalRefNo.Trim() + "'";
                            DataSet dsOnMarket = objUtility.OpenDataSet(sqlDtAdap, strsql, sqlCon, objTrans);
                            DataRow drow;
                            drow = dsOnMarket.Tables[0].NewRow();

                            drow["tb_pri_key"] = PrimaryKey;
                            drow["tb_internal_refno"] = req.InternalRefNo;
                            drow["tb_instcd"] = InstCd;
                            drow["tb_trx_type"] = req.TransectionType;
                            drow["tb_trx_date"] = req.TransectionDate;
                            drow["tb_trx_flag"] = "A";
                            drow["tb_client_id"] = req.ClientID;
                            drow["tb_isin"] = ListISIN[i];
                            drow["tb_qty"] = ListQty[i];
                            drow["tb_obligationid"] = ListObligNo[i];
                            drow["tb_serialno"] = ListSerialNo[i];
                            drow["mkrid"] = UserId.ToUpper();
                            drow["mkrdt"] = "";
                            drow["tb_exec_date"] = req.TransectionDate;
                            drow["tb_status"] = "01";
                            drow["tb_remark"] = ListRemark[i].ToUpper();
                            drow["mkrtm"] = "";
                            drow["tb_instreceivemode"] = ReceiveMode;
                            drow["tb_branchcd"] = req.BranchCode.ToUpper();

                            if (Strings.Len(Strings.Trim(ListSettNo[i])) == 13)
                            {
                                drow["tb_market_type"] = Strings.Mid(ListSettNo[i], 5, 2);
                            }
                            else
                            {
                                drow["tb_market_type"] = Strings.Left(ListSettNo[i], 1);
                            }

                            drow["tb_settlement"] = ListSettNo[i];
                            drow["tb_exchangeid"] = ExchangeId;
                            drow["tb_chid"] = ChId;
                            drow["tb_other_cmbp_id"] = ListCMID[i];
                            drow["tb_cash"] = "X";
                            drow["tb_authuserid1"] = "";
                            drow["tb_authuserid2"] = "";
                            drow["tb_authuserid3"] = "";
                            drow["tb_authdt1"] = "";
                            drow["tb_authdt2"] = "";
                            drow["tb_serialno"] = "0";

                            if (strAuth == "N")
                            {
                                drow["tb_trx_allow"] = "Y";
                                drow["tb_authcode1"] = "N";
                                drow["tb_authcode2"] = "N";
                                drow["tb_authcode3"] = "N";
                            }
                            else
                            {
                                if (Conversion.Val(strSearch) != 0)
                                {
                                    drow["tb_trx_allow"] = "N";
                                    drow["tb_authcode1"] = "Y";
                                    drow["tb_authcode2"] = "Y";
                                    drow["tb_authcode3"] = "Y";
                                }
                                else
                                {
                                    drow["tb_trx_allow"] = "Y";
                                    drow["tb_authcode1"] = "N";
                                    drow["tb_authcode2"] = "N";
                                    drow["tb_authcode3"] = "N";
                                }
                            }

                            drow["tb_recoslipyn"] = "N";
                            drow["tb_entrymode"] = "E";
                            drow["tb_authtm1"] = "00:00:00";
                            drow["tb_authtm2"] = "00:00:00";
                            drow["tb_authtm3"] = "00:00:00";
                            drow["tb_EarlyPayIden"] = "";
                            drow["tb_EntityIden"] = ListEntryBy[i];
                            drow["tb_UCC"] = ListUCC[i];
                            drow["tb_SegmentID"] = ListSegment[i];
                            drow["tb_UCCCmid"] = ListCMId[i];
                            drow["tb_UCCTMCPCode"] = ListTMId[i];
                            drow["tb_UCCEXid"] = ListExchange[i];

                            dsOnMarket.Tables[0].Rows.Add(drow);
                            sqlDtAdap.Update(dsOnMarket);
                        }
                        sqlDtAdap.Dispose();
                        mfnInsertUsed_slip(sqlCon, objTrans, req.TransectionType, req.InstrumentType, req.InternalRefNo, req.TransectionDate, "A", req.ClientID, "", UserId.ToUpper(), MkrDate, CurrentTime);
                        objTrans.Commit();
                        strsql = "update Trxbackoffice set mkrdt = CONVERT(varchar, GETDATE(), 112),mkrtm = CONVERT(TIME, GETDATE()) where tb_internal_refno = '" + req.InternalRefNo + "'";
                        objUtility.ExecuteSQL(strsql);
                        if (blnIsUpdated)
                        {
                            return "Record Updated.";
                        }
                        return "Record Inserted.";
                    }
                }
                else
                {
                    return OnMarketValidation(req.InstrumentType, req.TransectionType, req.TransectionDate, req.BranchCode, req.InternalRefNo, req.ClientID, req.ReceiveMode);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic OnMarketFind(string InstrumentType, string InternalRefNo, string TransectionType)
        {
            try
            {
                if (fnEarlyPayInFindValidation(InstrumentType, InternalRefNo, TransectionType) == "Valid")
                {
                    SqlTransaction ObjTrans;
                    DataTable Dt;
                    string ClientCd, Bo_id, Exch_Desc, Seg_Desc, Branch, BranchName;
                    EarlyPayInResponce objResp = new EarlyPayInResponce();

                    strsql = "select * from Trxbackoffice where tb_internal_refno = '" + InternalRefNo + "' and tb_instcd = '" + InstrumentType + "'";
                    DataTable Dss = objUtility.OpenDataTable(strsql);
                    if (Dss.Rows.Count == 0)
                    {
                        return "Invalid Internal Reference No.";
                    }

                    strsql = "select * from Trxbackoffice where tb_internal_refno = '" + InternalRefNo + "' and tb_instcd = '" + InstrumentType + "' and tb_trx_type = '" + TransectionType + "'";
                    Dt = objUtility.OpenDataTable(strsql);
                    if (Dt.Rows.Count == 0)
                    {
                        return "Record not found";
                    }
                    objResp.TransectionDate = Dt.Rows[0]["tb_trx_date"].ToString().Trim();
                    objResp.InternalRefNo = InternalRefNo;
                    ClientCd = Dt.Rows[0]["tb_client_id"].ToString().Trim();
                    objResp.ClientID = ClientCd;
                    Bo_id = Dt.Rows[0]["tb_other_client_id"].ToString().Trim();
                    Branch = Dt.Rows[0]["tb_branchcd"].ToString().Trim();
                    objResp.BranchCode = Branch;
                    objResp.InstrumentTypeCode = InstrumentType;

                    strsql = "select bm_branchname from Branch_master where bm_branchcd = '" + Dt.Rows[0]["tb_branchcd"].ToString().Trim() + "'";
                    Dt = objUtility.OpenDataTable(strsql);
                    BranchName = Dt.Rows[0][0].ToString().Trim();
                    objResp.BranchName = BranchName;

                    strsql = "select cm_name from Client_master where cm_cd = '" + ClientCd + "'";
                    Dt = objUtility.OpenDataTable(strsql);
                    objResp.ClientName = Dt.Rows[0][0].ToString().Trim();

                    strsql = "select im_desc from Instrument_master where im_instcd = (select distinct tb_instcd from Trxbackoffice where tb_internal_refno = '" + InternalRefNo + "')";
                    Dt = objUtility.OpenDataTable(strsql);
                    objResp.InstrumentType = Dt.Rows[0][0].ToString().Trim();

                    string ConnectionString = objUtility.GetConnectionStr();
                    using (SqlConnection Sqlcon = new SqlConnection(ConnectionString))
                    {
                        Sqlcon.Open();
                        ObjTrans = Sqlcon.BeginTransaction();
                        SqlCommand cmd = Sqlcon.CreateCommand();
                        cmd.Transaction = ObjTrans;
                        SqlDataAdapter sqlDtAdap = new SqlDataAdapter();
                        SqlCommandBuilder sqlCmdBld = new SqlCommandBuilder(sqlDtAdap);

                        fnOnMarketTempTable(Sqlcon, ObjTrans);

                        strsql = "Insert into #TempOnMarket ( Transection_Id, CMBO_ID, ISIN, Qty, Cash, Settlement, Flag, Charges, Auth, Oblig_No, Serial_No, Branch, Branch_Name, Remark, ExchangeID, CC_Id, Market_Type, Time) " +
                                 "(select tb_pri_key , tb_other_cmbp_id, tb_isin, tb_qty, " +
                                 "case (tb_cash)when 'X' then 'NONE' end, tb_settlement, tb_trx_allow, '', tb_authcode1, tb_obligationid, tb_serialno, '" + Branch + "', '" + BranchName + "', tb_remark, tb_exchangeid, tb_chid, tb_market_type, mkrtm " +
                                 "from Trxbackoffice where tb_internal_refno = '" + InternalRefNo.Trim() + "' and tb_instcd = '" + InstrumentType + "' and tb_trx_type = '" + TransectionType + "')";
                        objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                        strsql = "select * from #TempOnMarket";
                        Dt = objUtility.OpenDataTable(strsql, Sqlcon, ObjTrans);

                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            Exch_Desc = Dt.Rows[i]["Exchange"].ToString().Trim();
                            Seg_Desc = Dt.Rows[i]["Segment"].ToString().Trim();
                            strsql = "update #TempOnMarket set ISIN_Name = (select sc_isinname from Security where sc_isincode = '" + Dt.Rows[i]["ISIN"].ToString().Trim() + "') where Transection_Id = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "'"; //ISIN Name
                            objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                            strsql = "update #TempOnMarket set CMBOID_Name = (select top(1) bp_name from Bpmaster where bp_role = '01' and bp_id = '" + Dt.Rows[i]["CMBO_ID"].ToString().Trim() + "')";
                            objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                            strsql = "update #TempOnMarket set Rate = (select sc_rate from Security where sc_isincode = '" + Dt.Rows[i]["ISIN"].ToString().Trim() + "') where Transection_Id = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "'"; //Rate
                            objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                            strsql = "update #TempOnMarket set Value = (Qty * Rate) where Transection_Id = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "'"; //Value
                            objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                            strsql = "select sx_description from statusof_trx where sx_code = (select distinct tb_status from Trxbackoffice where tb_internal_refno = '" + InternalRefNo.Trim() + "' and tb_pri_key = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "')";
                            DataTable Ds = objUtility.OpenDataTable(strsql, Sqlcon, ObjTrans);

                            strsql = "update #TempOnMarket set Status = '" + Ds.Rows[0][0].ToString().Trim() + "' " +
                                "where Transection_Id = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "'"; //Status
                            objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                            //Exch_Desc = fnExchangeName(Exch_Desc);
                            //Seg_Desc = fnSegmentName(Seg_Desc);

                            //strsql = "update #TempEarlyPayIn set Exchange_Name = '" + Exch_Desc + "', Segment_Name = '" + Seg_Desc + "' where Transection_Id = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "'";
                            //objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);
                        }
                        strsql = "select * from #TempOnMarket";
                        Dt = objUtility.OpenDataTable(strsql, Sqlcon, ObjTrans);

                        foreach (DataColumn col in Dt.Columns)
                        {
                            foreach (DataRow row in Dt.Rows)
                            {
                                if (row.IsNull(col))
                                {
                                    strsql = "update #TempOnMarket set " + col + " = ''";
                                    objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);
                                }
                            }
                        }

                        Dt = fnRTrimOnMarketTable(Sqlcon, ObjTrans);
                        objResp.Data = Dt;
                        return objResp;
                    }
                }
                else
                {
                    return fnEarlyPayInFindValidation(InstrumentType, InternalRefNo, TransectionType);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region On Market Functions
        public string OnMarketValidation(string clientCd, string SettlementNo, string TrxDate, string CMID, string ISIN, double Qty, string Exchange, string Segment, string UCC, string CMid, string EntryBy, string TMId)
        {
            DataTable rstemp;
            int count = 0;
            if (SettlementNo != "") // Settlement No
            {
                DateTime TrxDt;
                TrxDt = DateTime.ParseExact(TrxDate, "yyyyMMdd", null);
                if (GetSysParm("STLMNTPOCKET") == "Y")
                {
                    strsql = "select cc_id 'CC id',cc_settle_no 'Settlement No',cc_mkt_type 'Market Type',mt_description 'Market Desc',cc_settle_periodfrom 'Period From',cc_settle_periodto 'Period To',cc_payout_dt 'Pay Out Date' from Cc_calender , Market_type where mt_code = cc_mkt_type and isnumeric(mt_code) = 1  and Left(cc_settle_no,2) = mt_exchangeid ";
                    strsql += " and '" + TrxDate + "' between  cc_settle_periodfrom and cc_payout_dt";
                    rstemp = objUtility.OpenDataTable(strsql);
                    if (rstemp.Rows.Count > 0)
                    {
                        for (int i = 0; i < rstemp.Rows.Count; i++)
                        {
                            if (rstemp.Rows[i]["Settlement No"].ToString().Trim() == SettlementNo)
                            {
                                count++;
                            }
                        }
                        if (count == 0)
                        {
                            return "Settlement Is Not Active.";
                        }
                    }
                    else
                    {
                        return "Settement is not Found.";
                    }
                }
                else if (SettlementNo.Length != 9)
                {
                    return "Enter 9 Digits Settlement No.";
                }
            }
            else
            {
                return "Settlement No Cannot Be Blank";
            }
            if (CMID != "")
            {
                strsql = "select bp_name from Bpmaster where bp_id = '" + CMID + "' and bp_role = '01'";
                rstemp = objUtility.OpenDataTable(strsql);
                if (rstemp.Rows.Count == 0)
                {
                    return "Invalid CM ID";
                }
            }
            else
            {
                return "CM ID Cannot Be Blank";
            }
            if (ISIN != "") // ISIN No
            {
                if (Strings.Left(ISIN.Trim(), 2) != "IN")
                {
                    ISIN = "IN" + ISIN;
                }
                if (ISIN.Trim().Length < 12)
                {
                    ISIN = fnfindisin(ISIN);
                }
                if (ISIN.Length < 12)
                {
                    return "Invalid ISIN.";
                }
                string res = fncheckisin(ISIN);
                if (res != "Valid")
                {
                    return res;
                }
                strsql = "select * from Holding where hld_ac_code = '" + clientCd + "' and hld_isin_code = '" + ISIN + "' and hld_ac_type = '11' ";
                rstemp = objUtility.OpenDataTable(strsql);
                if (rstemp.Rows.Count == 0)
                {
                    //return "Holding does not exist for " + clientCd + " in " + ISIN + ".";
                }
            }
            else
            {
                return "ISIN Cannot Be Blank.";
            }
            if (Qty == 0) //Qty
            {
                return "Qty Cannot Be Zero";
            }
            if (Exchange != "") // Exchange
            {
                strsql = "select " +
                         "a.bp_id 'STOCK EXCHENGE', " +
                         "a.bp_name 'NAME', " +
                         "a.bp_assd_cc_cmid 'CC ID', " +
                         "p.bp_name 'NAME' " +
                         "from Bpmaster a, Bpmaster p " +
                         "where a.bp_id = p.bp_id and a.bp_role = '02' and p.bp_role = '02' and a.bp_id = '" + Exchange + "'";
                rstemp = objUtility.OpenDataTable(strsql);
                if (rstemp.Rows.Count == 0)
                {
                    return "Invalid Exchange Code";
                }
            }
            if (Segment != "") // Segment
            {
                strsql = "select * from Clientsub_master where cs_module ='CS26' and cs_code = '" + Segment + "'";
                rstemp = objUtility.OpenDataTable(strsql);
                if (rstemp.Rows.Count == 0)
                {
                    return "Invalid Segment Code";
                }
            }
            if (UCC != "") // UCC
            {
                strsql = "select " +
                     "Case cud_Exchnge when '11' then 'BSE' when '12' then 'NSE' when '23' then 'MCX' when '22' then 'NCDEX' else cud_Exchnge end as 'Exchange', " +
                     "Case cud_segment when '01' then 'Cash' when '02' then 'F&O' when '03' then 'Currency' when '04' then 'SLB' when '05' then 'Commodity' when '06' then 'Debt' else cud_segment end as 'Segment', " +
                     "cud_UCC, " +
                     "cud_cmid, " +
                     "cud_tmid " +
                     "from Client_UCC_Details where cud_UCC = '" + UCC + "'";
                rstemp = objUtility.OpenDataTable(strsql);
                if (rstemp.Rows.Count == 0)
                {
                    return "Invalid UCC Code";
                }
            }
            if (CMid != "") // CM Id
            {
                strsql = "select " +
                     "Case cud_Exchnge when '11' then 'BSE' when '12' then 'NSE' when '23' then 'MCX' when '22' then 'NCDEX' else cud_Exchnge end as 'Exchange', " +
                     "Case cud_segment when '01' then 'Cash' when '02' then 'F&O' when '03' then 'Currency' when '04' then 'SLB' when '05' then 'Commodity' when '06' then 'Debt' else cud_segment end as 'Segment', " +
                     "cud_UCC, " +
                     "cud_cmid, " +
                     "cud_tmid " +
                     "from Client_UCC_Details where cud_UCC = '" + UCC + "' and cud_cmid = '" + CMid + "'";
                rstemp = objUtility.OpenDataTable(strsql);
                if (rstemp.Rows.Count == 0)
                {
                    return "Invalid CM Id Code";
                }
            }
            if (EntryBy != "") // Entry By
            {
                if (EntryBy != "TM" && EntryBy != "CP")
                {
                    return "Invalid Entry By Code";
                }
            }
            if (TMId != "") // TM Id
            {
                strsql = "select " +
                     "Case cud_Exchnge when '11' then 'BSE' when '12' then 'NSE' when '23' then 'MCX' when '22' then 'NCDEX' else cud_Exchnge end as 'Exchange', " +
                     "Case cud_segment when '01' then 'Cash' when '02' then 'F&O' when '03' then 'Currency' when '04' then 'SLB' when '05' then 'Commodity' when '06' then 'Debt' else cud_segment end as 'Segment', " +
                     "cud_UCC, " +
                     "cud_cmid, " +
                     "cud_tmid " +
                     "from Client_UCC_Details where cud_UCC = '" + UCC + "' and cud_tmid = '" + TMId + "'";
                rstemp = objUtility.OpenDataTable(strsql);
                if (rstemp.Rows.Count == 0)
                {
                    return "Invalid TM Id Code";
                }
            }
            return "Valid";
        }
        public string OnMarketValidation(string InstrumentType, string TransectionType, string TrxDate, string Branch, string InternalRefNo, string clientCd, string ReceiveMode)
        {
            DataTable ds;
            DataTable rstemp;
            string strInwardentry, strInwardstatus;
            int intiPos;
            if (InstrumentType != "") //Instrument Type
            {
                if (InstrumentType != "11" && InstrumentType != "111" && InstrumentType != "4" && InstrumentType != "104")
                {
                    return "Invalid Instrument Type";
                }
            }
            else
            {
                return "Instrument Type Cannot Be Blank";
            }
            if (TransectionType != "") // Transection Type
            {
                if (TransectionType != "906")
                {
                    return "Invalid Transection Type";
                }
            }
            else
            {
                return "Transection Type Cannot Be Blank";
            }
            if (TrxDate == "")
            {
                return "Transection Date Cannot Be Blank";
            }
            if (Branch != "") //Branch
            {
                strsql = "select count(*) from Branch_master where bm_branchcd = '" + Branch + "'";
                ds = objUtility.OpenDataTable(strsql);
                if (ds.Rows[0][0].ToString() == "0")
                {
                    return "Invalid Branch Code [ " + Branch + " ]";
                }
            }
            else
            {
                return "Branch Code Cannot Be Blank";
            }
            if (InternalRefNo != "") //internal ref no
            {
                strsql = "select COUNT(*) from Trxbackoffice where tb_internal_refno = '" + InternalRefNo + "'";
                ds = objUtility.OpenDataTable(strsql);
                if (Conversion.Val(ds.Rows[0][0].ToString().Trim()) > 0)
                {
                    return "Internal Ref No Already Present";
                }
                if (InternalRefNo == "0")
                {
                    return "Zero can't be an Internal Ref No";
                }

                strInwardstatus = "N";
                strInwardentry = Strings.Trim(Strings.UCase(GetSysParm("INWARDENTRY")));
                if (strInwardentry != "")
                {
                    intiPos = Strings.InStr(strInwardentry, TransectionType);
                    if (intiPos > 0)
                    {
                        strInwardstatus = Strings.Mid(strInwardentry, intiPos + 4, 1);
                    }
                }
                SlipCheck et = new SlipCheck();
                et.strInwardstatus = strInwardstatus;

                if (strInwardstatus == "A" | strInwardstatus == "O")
                {

                    strsql = "select ie_cmcd,cm_name,ie_lotno,ie_slipno,ie_nooftrx,ie_rejected,ie_mode,";
                    strsql = strsql + " lz_status,ie_execdt, cm_acctype, cm_brboffcode from Inward_entry,Lot_size, ";
                    strsql = strsql + " Instrument_master,Client_master where ie_cmcd = cm_cd ";
                    strsql = strsql + " and ie_lotno = lz_lotno   and ie_trxtype = lz_type ";
                    strsql = strsql + " and ie_instno = im_instcd  ";
                    strsql = strsql + " and ie_slipno = '" + InternalRefNo + "'";
                    strsql = strsql + " and im_instcd = " + InstrumentType;
                    strsql = strsql + " and ie_trxtype= " + Strings.Replace(Strings.Replace(TransectionType, "(", ""), ")", "") + " ";
                    rstemp = objUtility.OpenDataTable(strsql);

                    if (rstemp.Rows[0][0].ToString() != "")
                    {
                        et.strClientcd = Strings.Trim(rstemp.Rows[0]["ie_cmcd"].ToString());
                        et.strClient_Name = Strings.Trim(rstemp.Rows[0]["cm_name"].ToString());
                        et.strClient_type = Strings.Trim(rstemp.Rows[0]["cm_acctype"].ToString());
                        et.strbranchcd = Strings.Trim(rstemp.Rows[0]["cm_brboffcode"].ToString());
                        et.strExecution_date = Strings.Trim(rstemp.Rows[0]["ie_execdt"].ToString());
                        et.lngLotno = rstemp.Rows[0]["ie_lotno"].ToString();
                        et.intLotsize = Interaction.IIf(Convert.ToInt32(rstemp.Rows[0]["ie_nooftrx"].ToString()) - Convert.ToInt32(rstemp.Rows[0]["ie_rejected"].ToString()) == 0, 30000, Convert.ToInt32(rstemp.Rows[0]["ie_nooftrx"].ToString()) - Convert.ToInt32(rstemp.Rows[0]["ie_rejected"].ToString())).ToString();
                        et.strSlipMode = rstemp.Rows[0]["ie_mode"].ToString();
                    }
                    else if (strInwardstatus == "A")
                    {
                        return "Inward entry not found for current slip";
                    }
                }
            }
            else
            {
                return "Internal Ref No Cannot Be Blank.";
            }
            if (clientCd != "") //client id
            {
                DateTime TrxDt, FreezeDt;
                TrxDt = DateTime.ParseExact(TrxDate, "yyyyMMdd", null);
                int x = Strings.InStr(1, clientCd, ",");
                if (x > 0 || clientCd.Length != 16)
                {
                    clientCd = AutoNumber(clientCd);
                }
                strsql = "select cm_cd, cm_name , cm_freezeyn , cm_freezedt , cm_active ,cm_sech_name ,cm_thih_name,cm_acctype,cm_chgsscheme,cm_allowcredit,cm_poaforpayin from Client_master where cm_cd = '" + clientCd + "'";
                ds = objUtility.OpenDataTable(strsql);
                if (ds.Rows.Count == 0)
                {
                    return "Client Id Not Found";
                }

                FreezeDt = DateTime.ParseExact(ds.Rows[0]["cm_freezedt"].ToString().Trim(), "yyyyMMdd", null);

                if (ds.Rows[0]["cm_freezeyn"].ToString() != "" || ds.Rows[0]["cm_freezedt"].ToString() != "")
                {
                    if ((ds.Rows[0]["cm_freezeyn"].ToString() == "1" || ds.Rows[0]["cm_freezeyn"].ToString() == "3") && (TrxDt >= FreezeDt))
                    {
                        //return "This client is under Freeze Status";
                    }
                    if ((ds.Rows[0]["cm_freezeyn"].ToString() == "0" || ds.Rows[0]["cm_freezeyn"].ToString() == "2") && (TrxDt < FreezeDt))
                    {
                        //return "This client is under Freeze Status, will be active on" + FreezeDt;
                    }
                }
                if (Strings.InStr(1, "01,02", ds.Rows[0]["cm_active"].ToString(), Constants.vbTextCompare) == 0)
                {
                    //return "Client Is Not Active";
                }
                if (ds.Rows[0]["cm_poaforpayin"].ToString() == "N")
                {
                    //return "Client [" + clientCd + "] has offered POA for Market Sell Obligation";
                }
            }
            else
            {
                return "Client Id Cannot Be Blank";
            }
            if (ReceiveMode != "") // Receive Mode
            {
                if (ReceiveMode != "0" && ReceiveMode != "1" && ReceiveMode != "2" && ReceiveMode != "3" && ReceiveMode != "4" && ReceiveMode != "5" && ReceiveMode != "6" && ReceiveMode != "7" && ReceiveMode != "8" && ReceiveMode != "9" && ReceiveMode != "10" && ReceiveMode != "11")
                {
                    return "Invalid Receive Mode";
                }
            }
            else
            {
                return "Receive Mode Cannot Be Blank";
            }
            return "Valid";
        }
        public void fnOnMarketTempTable(SqlConnection con, SqlTransaction objTrans)
        {
            strsql = "Create Table #TempOnMarket (";
            strsql += "SrNo int identity(1,1),";
            strsql += "Transection_Id char(15),";
            strsql += "CMBO_ID char(25),";
            strsql += "CMBOID_Name char(50),";
            strsql += "ISIN char(15),";
            strsql += "ISIN_Name char(50),";
            strsql += "Qty numeric(18,3),";
            strsql += "Settlement char(15),";
            strsql += "Status char(30),";
            strsql += "Branch char(8),";
            strsql += "Branch_Name char(30),";
            strsql += "Rate money,";
            strsql += "Value money,";
            strsql += "Cash char(4),";
            strsql += "Flag char(1),";
            strsql += "Charges money,";
            strsql += "Auth char(1),";
            strsql += "Oblig_No char(8),";
            strsql += "Serial_No char(8),";
            strsql += "Remark char(100),";
            strsql += "ExchangeID char(8),";
            strsql += "CC_Id char(8),";
            strsql += "Market_Type char(8),";
            strsql += "Time char(8),";
            strsql += "Exchange char(8),";
            strsql += "Exchange_Name char(30),";
            strsql += "Segment char(8),";
            strsql += "Segment_Name char(15),";
            strsql += "UCC char(8),";
            strsql += "CM_Id char(8),";
            strsql += "Entry_By char(8),";
            strsql += "TM_Id char(8)";
            strsql += ")";

            objUtility.ExecuteSQL(strsql, con, objTrans);
        }
        public dynamic fnRTrimOnMarketTable(SqlConnection con, SqlTransaction objTrans)
        {
            strsql = "select ";
            strsql += "rtrim(SrNo) SrNo,";
            strsql += "rtrim(Transection_Id) Transection_Id,";
            strsql += "rtrim(CMBO_ID) CMBO_ID,";
            strsql += "rtrim(CMBOID_Name) CMBOID_Name,";
            strsql += "rtrim(ISIN) ISIN,";
            strsql += "rtrim(ISIN_Name) ISIN_Name,";
            strsql += "rtrim(Qty) Qty,";
            strsql += "rtrim(Settlement) Settlement,";
            strsql += "rtrim(Status) Status,";
            strsql += "rtrim(Branch) Branch,";
            strsql += "rtrim(Branch_Name) Branch_Name,";
            strsql += "rtrim(Rate) Rate,";
            strsql += "rtrim(Value) Value,";
            strsql += "rtrim(Cash) Cash,";
            strsql += "rtrim(Flag) Flag,";
            strsql += "rtrim(Charges) Charges,";
            strsql += "rtrim(Auth) Auth,";
            strsql += "rtrim(Oblig_No) Oblig_No,";
            strsql += "rtrim(Serial_No) Serial_No,";
            strsql += "rtrim(Remark) Remark,";
            strsql += "rtrim(ExchangeID) ExchangeID,";
            strsql += "rtrim(CC_Id) CC_Id,";
            strsql += "rtrim(Market_Type) Market_Type,";
            strsql += "rtrim(Time) Time,";
            strsql += "rtrim(Exchange) Exchange,";
            strsql += "rtrim(Exchange_Name) Exchange_Name,";
            strsql += "rtrim(Segment) Segment,";
            strsql += "rtrim(Segment_Name) Segment_Name,";
            strsql += "rtrim(UCC) UCC,";
            strsql += "rtrim(CM_Id) CM_Id,";
            strsql += "rtrim(Entry_By) Entry_By,";
            strsql += "rtrim(TM_Id) TM_Id ";
            strsql += "from #TempOnMarket";

            DataTable Dt = objUtility.OpenDataTable(strsql, con, objTrans);
            return Dt;
        }
        #endregion

        public dynamic EarlyPayInAdd(string UserId, EarlyPayInReq req)
        {
            try
            {
                DataTable Ds;
                var db = new DataContext();
                SqlTransaction objTrans;
                string MkrDate, CurrentTime, ReceiveMode, InstCd, strAuth, strRate, Settlement, strSearch, MarketType, ExchangeId, ChId;
                int PrimaryKey;
                bool blnIsUpdated = false;
                MkrDate = DateTime.Now.ToString("yyyyMMdd");
                CurrentTime = DateTime.Now.ToString("hh:mm:ss");
                int instcd = Convert.ToInt32(req.InstrumentType);
                double CheqNo = Conversion.Val(req.InternalRefNo);
                #region Data List
                List<string> ListSettNo = req.Data.Select(x => x.SettlementID).ToList();
                List<string> ListCMID = req.Data.Select(x => x.ClientID).ToList();
                List<string> ListISIN = req.Data.Select(x => x.ISIN).ToList();
                List<double> ListQty = req.Data.Select(x => x.Qty).ToList();
                List<string> ListCounterBOid = req.Data.Select(x => x.CounterClientID).ToList();
                List<string> ListRemark = req.Data.Select(x => x.Remark).ToList();
                List<string> ListExchange = req.Data.Select(x => x.Exchange).ToList();
                List<string> ListSegment = req.Data.Select(x => x.Segment).ToList();
                List<string> ListUCC = req.Data.Select(x => x.UCC).ToList();
                List<string> ListCMId = req.Data.Select(x => x.CMId).ToList();
                List<string> ListEntryBy = req.Data.Select(x => x.EntryBy).ToList();
                List<string> ListTMId = req.Data.Select(x => x.TMId).ToList();
                #endregion
                if (EarlyPayInValidation(req.InstrumentType, req.TransectionType, req.TransectionDate, req.BranchCode, req.InternalRefNo, req.ClientID, req.ReceiveMode) == "Valid" || EarlyPayInValidation(req.InstrumentType, req.TransectionType, req.TransectionDate, req.BranchCode, req.InternalRefNo, req.ClientID, req.ReceiveMode) == "Internal Ref No Already Present")
                {
                    if (mfnSlipCheck(req.ClientID, req.TransectionType, instcd, CheqNo) != "Valid")
                    {
                        return mfnSlipCheck(req.ClientID, req.TransectionType, instcd, CheqNo);
                    }
                    string Connection = objUtility.GetConnectionStr();
                    using (SqlConnection sqlCon = new SqlConnection(Connection))
                    {
                        sqlCon.Open();
                        objTrans = sqlCon.BeginTransaction();
                        SqlDataAdapter sqlDtAdap = new SqlDataAdapter();
                        SqlCommandBuilder sqlCmdBld = new SqlCommandBuilder(sqlDtAdap);

                        if (EarlyPayInValidation(req.InstrumentType, req.TransectionType, req.TransectionDate, req.BranchCode, req.InternalRefNo, req.ClientID, req.ReceiveMode) == "Internal Ref No Already Present")
                        {
                            strsql = "select * from Trxbackoffice where tb_internal_refno = '" + req.InternalRefNo + "'";
                            Ds = objUtility.OpenDataTable(strsql);
                            if (Ds.Rows[0]["tb_trx_allow"].ToString().Trim() == "E")
                            {
                                return "Data Already Exported.";
                            }
                            else if (Ds.Rows[0]["tb_trx_allow"].ToString().Trim() == "Y")
                            {
                                return "Data Already Authorised.";
                            }
                            else
                            {
                                strsql = "delete from Trxbackoffice where tb_internal_refno = '" + req.InternalRefNo + "'";
                                objUtility.ExecuteSQL(strsql, sqlCon, objTrans);
                                blnIsUpdated = true;
                            }
                        }

                        for (int i = 0; i < req.Data.Count; i++)
                        {
                            if (EarlyPayInValidation(req.ClientID, ListSettNo[i], req.TransectionDate, ListCMID[i], ListISIN[i], ListQty[i], ListCounterBOid[i], ListExchange[i], ListSegment[i], ListUCC[i], ListCMId[i], ListEntryBy[i], ListTMId[i]) != "Valid")
                            {
                                return EarlyPayInValidation(req.ClientID, ListSettNo[i], req.TransectionDate, ListCMID[i], ListISIN[i], ListQty[i], ListCounterBOid[i], ListExchange[i], ListSegment[i], ListUCC[i], ListCMId[i], ListEntryBy[i], ListTMId[i]);
                            }
                            Settlement = ListSettNo[i];
                            char IsChar = Convert.ToChar(Settlement.Substring(0, 1));
                            if (!Char.IsDigit(IsChar))
                            {
                                MarketType = IsChar.ToString();
                                ExchangeId = "12";
                                ChId = "11";
                            }
                            else
                            {
                                MarketType = Settlement.Substring(4, 2);
                                ExchangeId = Settlement.Substring(0, 2);
                                ChId = Settlement.Substring(2, 2);
                            }
                            ReceiveMode = fnReceiveMode(req.ReceiveMode.Trim(), "OffMarket");

                            InstCd = req.InstrumentType;

                            strsql = "select MAX(tb_pri_key) from Trxbackoffice";
                            Ds = objUtility.OpenDataTable(strsql, sqlCon, objTrans);
                            PrimaryKey = Convert.ToInt32(Ds.Rows[0][0].ToString().Trim()) + 1;

                            strAuth = GetSysParm("IMPORT_AUTH");
                            strsql = "select sc_rate from Security where sc_isincode = '" + ListISIN[i].Trim() + "'";
                            strRate = objUtility.OpenDataTable(strsql, sqlCon, objTrans).Rows[0][0].ToString().Trim();
                            strsql = "select count(*) from Auth_master where am_code = '903' and am_amount <= " + strRate + " * " + ListQty[i];
                            strSearch = objUtility.OpenDataTable(strsql, sqlCon, objTrans).Rows[0][0].ToString().Trim();

                            strsql = "select * from Trxbackoffice where tb_internal_refno = '" + req.InternalRefNo.Trim() + "'";
                            DataSet dsOnMarket = objUtility.OpenDataSet(sqlDtAdap, strsql, sqlCon, objTrans);
                            DataRow drow;
                            drow = dsOnMarket.Tables[0].NewRow();

                            drow["tb_pri_key"] = PrimaryKey;
                            drow["tb_internal_refno"] = req.InternalRefNo;
                            drow["tb_instcd"] = InstCd;
                            drow["tb_trx_type"] = req.TransectionType;
                            drow["tb_trx_date"] = req.TransectionDate;
                            drow["tb_trx_flag"] = "A";
                            drow["tb_client_id"] = req.ClientID;
                            drow["tb_isin"] = ListISIN[i];
                            drow["tb_qty"] = ListQty[i];
                            drow["tb_other_client_id"] = ListCounterBOid[i];
                            drow["mkrid"] = UserId.ToUpper();
                            drow["mkrdt"] = "";
                            drow["tb_exec_date"] = req.TransectionDate;
                            drow["tb_status"] = "01";
                            drow["tb_remark"] = ListRemark[i].ToUpper();
                            drow["mkrtm"] = "";
                            drow["tb_instreceivemode"] = ReceiveMode;
                            drow["tb_branchcd"] = req.BranchCode.ToUpper();

                            if (Strings.Len(Strings.Trim(ListSettNo[i])) == 13)
                            {
                                drow["tb_market_type"] = Strings.Mid(ListSettNo[i], 5, 2);
                            }
                            else
                            {
                                drow["tb_market_type"] = Strings.Left(ListSettNo[i], 1);
                            }

                            drow["tb_settlement"] = ListSettNo[i];
                            drow["tb_exchangeid"] = ExchangeId;
                            drow["tb_chid"] = ChId;
                            drow["tb_other_cmbp_id"] = ListCMID[i];
                            drow["tb_cash"] = "X";
                            drow["tb_authuserid1"] = "";
                            drow["tb_authuserid2"] = "";
                            drow["tb_authuserid3"] = "";
                            drow["tb_authdt1"] = "";
                            drow["tb_authdt2"] = "";
                            drow["tb_serialno"] = "0";

                            if (strAuth == "N")
                            {
                                drow["tb_trx_allow"] = "Y";
                                drow["tb_authcode1"] = "N";
                                drow["tb_authcode2"] = "N";
                                drow["tb_authcode3"] = "N";
                            }
                            else
                            {
                                if (Conversion.Val(strSearch) != 0)
                                {
                                    drow["tb_trx_allow"] = "N";
                                    drow["tb_authcode1"] = "Y";
                                    drow["tb_authcode2"] = "Y";
                                    drow["tb_authcode3"] = "Y";
                                }
                                else
                                {
                                    drow["tb_trx_allow"] = "Y";
                                    drow["tb_authcode1"] = "N";
                                    drow["tb_authcode2"] = "N";
                                    drow["tb_authcode3"] = "N";
                                }
                            }

                            drow["tb_recoslipyn"] = "N";
                            drow["tb_entrymode"] = "E";
                            drow["tb_authtm1"] = "00:00:00";
                            drow["tb_authtm2"] = "00:00:00";
                            drow["tb_authtm3"] = "00:00:00";
                            drow["tb_EarlyPayIden"] = "";
                            drow["tb_EntityIden"] = ListEntryBy[i];
                            drow["tb_UCC"] = ListUCC[i];
                            drow["tb_SegmentID"] = ListSegment[i];
                            drow["tb_UCCCmid"] = ListCMId[i];
                            drow["tb_UCCTMCPCode"] = ListTMId[i];
                            drow["tb_UCCEXid"] = ListExchange[i];

                            dsOnMarket.Tables[0].Rows.Add(drow);
                            sqlDtAdap.Update(dsOnMarket);
                        }
                        sqlDtAdap.Dispose();
                        mfnInsertUsed_slip(sqlCon, objTrans, req.TransectionType, req.InstrumentType, req.InternalRefNo, req.TransectionDate, "A", req.ClientID, "", UserId.ToUpper(), MkrDate, CurrentTime);
                        objTrans.Commit();
                        strsql = "update Trxbackoffice set mkrdt = CONVERT(varchar, GETDATE(), 112),mkrtm = CONVERT(TIME, GETDATE()) where tb_internal_refno = '" + req.InternalRefNo + "'";
                        objUtility.ExecuteSQL(strsql);
                        if (blnIsUpdated)
                        {
                            return "Record Updated.";
                        }
                        return "Record Inserted.";
                    }
                }
                else
                {
                    return EarlyPayInValidation(req.InstrumentType, req.TransectionType, req.TransectionDate, req.BranchCode, req.InternalRefNo, req.ClientID, req.ReceiveMode);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic EarlyPayInFind(string InstrumentType, string InternalRefNo, string TransectionType)
        {
            try
            {
                if (fnEarlyPayInFindValidation(InstrumentType, InternalRefNo, TransectionType) == "Valid")
                {
                    SqlTransaction ObjTrans;
                    DataTable Dt;
                    string ClientCd, Bo_id, Exch_Desc, Seg_Desc, Branch, BranchName;
                    EarlyPayInResponce objResp = new EarlyPayInResponce();

                    strsql = "select * from Trxbackoffice where tb_internal_refno = '" + InternalRefNo + "' and tb_instcd = '" + InstrumentType + "'";
                    DataTable Dss = objUtility.OpenDataTable(strsql);
                    if (Dss.Rows.Count == 0)
                    {
                        return "Invalid Internal Reference No.";
                    }

                    strsql = "select * from Trxbackoffice where tb_internal_refno = '" + InternalRefNo + "' and tb_instcd = '" + InstrumentType + "' and tb_trx_type = '" + TransectionType + "'";
                    Dt = objUtility.OpenDataTable(strsql);
                    if (Dt.Rows.Count == 0)
                    {
                        return "Record not found";
                    }
                    objResp.TransectionDate = Dt.Rows[0]["tb_trx_date"].ToString().Trim();
                    objResp.InternalRefNo = InternalRefNo;
                    ClientCd = Dt.Rows[0]["tb_client_id"].ToString().Trim();
                    objResp.ClientID = ClientCd;
                    Bo_id = Dt.Rows[0]["tb_other_client_id"].ToString().Trim();
                    Branch = Dt.Rows[0]["tb_branchcd"].ToString().Trim();
                    objResp.BranchCode = Branch;
                    objResp.InstrumentTypeCode = InstrumentType;

                    strsql = "select bm_branchname from Branch_master where bm_branchcd = '" + Dt.Rows[0]["tb_branchcd"].ToString().Trim() + "'";
                    Dt = objUtility.OpenDataTable(strsql);
                    BranchName = Dt.Rows[0][0].ToString().Trim();
                    objResp.BranchName = BranchName;

                    strsql = "select cm_name from Client_master where cm_cd = '" + ClientCd + "'";
                    Dt = objUtility.OpenDataTable(strsql);
                    objResp.ClientName = Dt.Rows[0][0].ToString().Trim();

                    strsql = "select im_desc from Instrument_master where im_instcd = (select distinct tb_instcd from Trxbackoffice where tb_internal_refno = '" + InternalRefNo + "')";
                    Dt = objUtility.OpenDataTable(strsql);
                    objResp.InstrumentType = Dt.Rows[0][0].ToString().Trim();

                    string ConnectionString = objUtility.GetConnectionStr();
                    using (SqlConnection Sqlcon = new SqlConnection(ConnectionString))
                    {
                        Sqlcon.Open();
                        ObjTrans = Sqlcon.BeginTransaction();
                        SqlCommand cmd = Sqlcon.CreateCommand();
                        cmd.Transaction = ObjTrans;
                        SqlDataAdapter sqlDtAdap = new SqlDataAdapter();
                        SqlCommandBuilder sqlCmdBld = new SqlCommandBuilder(sqlDtAdap);

                        fnEarlyPayInTempTable(Sqlcon, ObjTrans);

                        strsql = "Insert into #TempEarlyPayIn ( Transection_Id, CMBO_ID, ISIN, Qty, Cash, Settlement, Flag, Charges, Auth, Counter_BO_id, Branch, Branch_Name, Remark, ExchangeID, CC_Id, Market_Type, Time) " +
                                 "(select tb_pri_key , tb_other_cmbp_id, tb_isin, tb_qty, " +
                                 "case (tb_cash)when 'X' then 'NONE' end, tb_settlement, tb_trx_allow, '', tb_authcode1, tb_other_client_id, '" + Branch + "', '" + BranchName + "', tb_remark, tb_exchangeid, tb_chid, tb_market_type, mkrtm " +
                                 "from Trxbackoffice where tb_internal_refno = '" + InternalRefNo.Trim() + "' and tb_instcd = '" + InstrumentType + "' and tb_trx_type = '" + TransectionType + "')";
                        objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                        strsql = "select * from #TempEarlyPayIn";
                        Dt = objUtility.OpenDataTable(strsql, Sqlcon, ObjTrans);

                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            Exch_Desc = Dt.Rows[i]["Exchange"].ToString().Trim();
                            Seg_Desc = Dt.Rows[i]["Segment"].ToString().Trim();
                            strsql = "update #TempEarlyPayIn set ISIN_Name = (select sc_isinname from Security where sc_isincode = '" + Dt.Rows[i]["ISIN"].ToString().Trim() + "') where Transection_Id = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "'"; //ISIN Name
                            objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                            strsql = "update #TempEarlyPayIn set CMBOID_Name = (select top(1) bp_name from Bpmaster where bp_role = '01' and bp_id = '" + Dt.Rows[i]["CMBO_ID"].ToString().Trim() + "')";
                            objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                            strsql = "update #TempEarlyPayIn set Rate = (select sc_rate from Security where sc_isincode = '" + Dt.Rows[i]["ISIN"].ToString().Trim() + "') where Transection_Id = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "'"; //Rate
                            objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                            strsql = "update #TempEarlyPayIn set Value = (Qty * Rate) where Transection_Id = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "'"; //Value
                            objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                            strsql = "select sx_description from statusof_trx where sx_code = (select distinct tb_status from Trxbackoffice where tb_internal_refno = '" + InternalRefNo.Trim() + "' and tb_pri_key = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "')";
                            DataTable Ds = objUtility.OpenDataTable(strsql, Sqlcon, ObjTrans);

                            strsql = "update #TempEarlyPayIn set Status = '" + Ds.Rows[0][0].ToString().Trim() + "' " +
                                "where Transection_Id = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "'"; //Status
                            objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);

                            //Exch_Desc = fnExchangeName(Exch_Desc);
                            //Seg_Desc = fnSegmentName(Seg_Desc);

                            //strsql = "update #TempEarlyPayIn set Exchange_Name = '" + Exch_Desc + "', Segment_Name = '" + Seg_Desc + "' where Transection_Id = '" + Dt.Rows[i]["Transection_Id"].ToString().Trim() + "'";
                            //objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);
                        }
                        strsql = "select * from #TempEarlyPayIn";
                        Dt = objUtility.OpenDataTable(strsql, Sqlcon, ObjTrans);

                        foreach (DataColumn col in Dt.Columns)
                        {
                            foreach (DataRow row in Dt.Rows)
                            {
                                if (row.IsNull(col))
                                {
                                    strsql = "update #TempEarlyPayIn set " + col + " = ''";
                                    objUtility.ExecuteSQL(strsql, Sqlcon, ObjTrans);
                                }
                            }
                        }

                        Dt = fnRTrimEarlyPayInTable(Sqlcon, ObjTrans);
                        objResp.Data = Dt;
                        return objResp;
                    }
                }
                else
                {
                    return fnEarlyPayInFindValidation(InstrumentType, InternalRefNo, TransectionType);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region Early Pay In Functions
        public string EarlyPayInValidation(string InstrumentType, string TransectionType, string TrxDate, string Branch, string InternalRefNo, string clientCd, string ReceiveMode)
        {
            DataTable ds;
            DataTable rstemp;
            string strInwardentry, strInwardstatus;
            int intiPos;
            if (InstrumentType != "") //Instrument Type
            {
                if (InstrumentType != "11" && InstrumentType != "111")
                {
                    return "Invalid Instrument Type";
                }
            }
            else
            {
                return "Instrument Type Cannot Be Blank";
            }
            if (TransectionType != "") // Transection Type
            {
                if (TransectionType != "903")
                {
                    return "Invalid Transection Type";
                }
            }
            else
            {
                return "Transection Type Cannot Be Blank";
            }
            if (TrxDate == "")
            {
                return "Transection Date Cannot Be Blank";
            }
            if (Branch != "") //Branch
            {
                strsql = "select count(*) from Branch_master where bm_branchcd = '" + Branch + "'";
                ds = objUtility.OpenDataTable(strsql);
                if (ds.Rows[0][0].ToString() == "0")
                {
                    return "Invalid Branch Code [ " + Branch + " ]";
                }
            }
            else
            {
                return "Branch Code Cannot Be Blank";
            }
            if (InternalRefNo != "") //internal ref no
            {
                strsql = "select COUNT(*) from Trxbackoffice where tb_internal_refno = '" + InternalRefNo + "'";
                ds = objUtility.OpenDataTable(strsql);
                if (Conversion.Val(ds.Rows[0][0].ToString().Trim()) > 0)
                {
                    return "Internal Ref No Already Present";
                }
                if (InternalRefNo == "0")
                {
                    return "Zero can't be an Internal Ref No";
                }

                strInwardstatus = "N";
                strInwardentry = Strings.Trim(Strings.UCase(GetSysParm("INWARDENTRY")));
                if (strInwardentry != "")
                {
                    intiPos = Strings.InStr(strInwardentry, TransectionType);
                    if (intiPos > 0)
                    {
                        strInwardstatus = Strings.Mid(strInwardentry, intiPos + 4, 1);
                    }
                }
                SlipCheck et = new SlipCheck();
                et.strInwardstatus = strInwardstatus;

                if (strInwardstatus == "A" | strInwardstatus == "O")
                {

                    strsql = "select ie_cmcd,cm_name,ie_lotno,ie_slipno,ie_nooftrx,ie_rejected,ie_mode,";
                    strsql = strsql + " lz_status,ie_execdt, cm_acctype, cm_brboffcode from Inward_entry,Lot_size, ";
                    strsql = strsql + " Instrument_master,Client_master where ie_cmcd = cm_cd ";
                    strsql = strsql + " and ie_lotno = lz_lotno   and ie_trxtype = lz_type ";
                    strsql = strsql + " and ie_instno = im_instcd  ";
                    strsql = strsql + " and ie_slipno = '" + InternalRefNo + "'";
                    strsql = strsql + " and im_instcd = " + InstrumentType;
                    strsql = strsql + " and ie_trxtype= " + Strings.Replace(Strings.Replace(TransectionType, "(", ""), ")", "") + " ";
                    rstemp = objUtility.OpenDataTable(strsql);

                    if (rstemp.Rows[0][0].ToString() != "")
                    {
                        et.strClientcd = Strings.Trim(rstemp.Rows[0]["ie_cmcd"].ToString());
                        et.strClient_Name = Strings.Trim(rstemp.Rows[0]["cm_name"].ToString());
                        et.strClient_type = Strings.Trim(rstemp.Rows[0]["cm_acctype"].ToString());
                        et.strbranchcd = Strings.Trim(rstemp.Rows[0]["cm_brboffcode"].ToString());
                        et.strExecution_date = Strings.Trim(rstemp.Rows[0]["ie_execdt"].ToString());
                        et.lngLotno = rstemp.Rows[0]["ie_lotno"].ToString();
                        et.intLotsize = Interaction.IIf(Convert.ToInt32(rstemp.Rows[0]["ie_nooftrx"].ToString()) - Convert.ToInt32(rstemp.Rows[0]["ie_rejected"].ToString()) == 0, 30000, Convert.ToInt32(rstemp.Rows[0]["ie_nooftrx"].ToString()) - Convert.ToInt32(rstemp.Rows[0]["ie_rejected"].ToString())).ToString();
                        et.strSlipMode = rstemp.Rows[0]["ie_mode"].ToString();
                    }
                    else if (strInwardstatus == "A")
                    {
                        return "Inward entry not found for current slip";
                    }
                }
            }
            else
            {
                return "Internal Ref No Cannot Be Blank.";
            }
            if (clientCd != "") //client id
            {
                DateTime TrxDt, FreezeDt;
                TrxDt = DateTime.ParseExact(TrxDate, "yyyyMMdd", null);
                int x = Strings.InStr(1, clientCd, ",");
                if (x > 0 || clientCd.Length != 16)
                {
                    clientCd = AutoNumber(clientCd);
                }
                strsql = "select cm_cd, cm_name , cm_freezeyn , cm_freezedt , cm_active ,cm_sech_name ,cm_thih_name,cm_acctype,cm_chgsscheme,cm_allowcredit,cm_poaforpayin from Client_master where cm_cd = '" + clientCd + "'";
                ds = objUtility.OpenDataTable(strsql);
                if (ds.Rows.Count == 0)
                {
                    return "Client Id Not Found";
                }

                FreezeDt = DateTime.ParseExact(ds.Rows[0]["cm_freezedt"].ToString().Trim(), "yyyyMMdd", null);

                if (ds.Rows[0]["cm_freezeyn"].ToString() != "" || ds.Rows[0]["cm_freezedt"].ToString() != "")
                {
                    if ((ds.Rows[0]["cm_freezeyn"].ToString() == "1" || ds.Rows[0]["cm_freezeyn"].ToString() == "3") && (TrxDt >= FreezeDt))
                    {
                        //return "This client is under Freeze Status";
                    }
                    if ((ds.Rows[0]["cm_freezeyn"].ToString() == "0" || ds.Rows[0]["cm_freezeyn"].ToString() == "2") && (TrxDt < FreezeDt))
                    {
                        //return "This client is under Freeze Status, will be active on" + FreezeDt;
                    }
                }
                if (Strings.InStr(1, "01,02", ds.Rows[0]["cm_active"].ToString(), Constants.vbTextCompare) == 0)
                {
                    //return "Client Is Not Active";
                }
                if (ds.Rows[0]["cm_poaforpayin"].ToString() == "N")
                {
                    //return "Client [" + clientCd + "] has offered POA for Market Sell Obligation";
                }
            }
            else
            {
                return "Client Id Cannot Be Blank";
            }
            if (ReceiveMode != "") // Receive Mode
            {
                if (ReceiveMode != "0" && ReceiveMode != "1" && ReceiveMode != "2" && ReceiveMode != "3" && ReceiveMode != "4" && ReceiveMode != "5" && ReceiveMode != "6" && ReceiveMode != "7" && ReceiveMode != "8" && ReceiveMode != "9" && ReceiveMode != "10" && ReceiveMode != "11")
                {
                    return "Invalid Receive Mode";
                }
            }
            else
            {
                return "Receive Mode Cannot Be Blank";
            }
            return "Valid";
        }
        public string EarlyPayInValidation(string clientCd, string SettlementNo, string TrxDate, string CMID, string ISIN, double Qty, string CouterBOid, string Exchange, string Segment, string UCC, string CMid, string EntryBy, string TMId)
        {
            DataTable rstemp;
            int count = 0;
            if (SettlementNo != "") // Settlement No
            {
                DateTime TrxDt;
                TrxDt = DateTime.ParseExact(TrxDate, "yyyyMMdd", null);
                if (GetSysParm("STLMNTPOCKET") == "Y")
                {
                    strsql = "select cc_id 'CC id',cc_settle_no 'Settlement No',cc_mkt_type 'Market Type',mt_description 'Market Desc',cc_settle_periodfrom 'Period From',cc_settle_periodto 'Period To',cc_payout_dt 'Pay Out Date' from Cc_calender , Market_type where mt_code = cc_mkt_type and isnumeric(mt_code) = 1  and Left(cc_settle_no,2) = mt_exchangeid ";
                    strsql += " and '" + TrxDate + "' between  cc_settle_periodfrom and cc_payout_dt";
                    rstemp = objUtility.OpenDataTable(strsql);
                    if (rstemp.Rows.Count > 0)
                    {
                        for (int i = 0; i < rstemp.Rows.Count; i++)
                        {
                            if (rstemp.Rows[i]["Settlement No"].ToString().Trim() == SettlementNo)
                            {
                                count++;
                            }
                        }
                        if (count == 0)
                        {
                            return "Settlement Is Not Active.";
                        }
                    }
                    else
                    {
                        return "Settement is not Found.";
                    }
                }
                else if (SettlementNo.Length != 9)
                {
                    return "Enter 9 Digits Settlement No.";
                }
            }
            else
            {
                return "Settlement No Cannot Be Blank";
            }
            if (CMID != "")
            {
                strsql = "select bp_name from Bpmaster where bp_id = '" + CMID + "' and bp_role = '01'";
                rstemp = objUtility.OpenDataTable(strsql);
                if (rstemp.Rows.Count == 0)
                {
                    return "Invalid CM ID";
                }
            }
            else
            {
                return "CM ID Cannot Be Blank";
            }
            if (ISIN != "") // ISIN No
            {
                if (Strings.Left(ISIN.Trim(), 2) != "IN")
                {
                    ISIN = "IN" + ISIN;
                }
                if (ISIN.Trim().Length < 12)
                {
                    ISIN = fnfindisin(ISIN);
                }
                if (ISIN.Length < 12)
                {
                    return "Invalid ISIN.";
                }
                string res = fncheckisin(ISIN);
                if (res != "Valid")
                {
                    return res;
                }
                strsql = "select * from Holding where hld_ac_code = '" + clientCd + "' and hld_isin_code = '" + ISIN + "' and hld_ac_type = '11' ";
                rstemp = objUtility.OpenDataTable(strsql);
                if (rstemp.Rows.Count == 0)
                {
                    //return "Holding does not exist for " + clientCd + " in " + ISIN + ".";
                }
            }
            else
            {
                return "ISIN Cannot Be Blank.";
            }
            if (Qty == 0) //Qty
            {
                return "Qty Cannot Be Zero";
            }
            if (CouterBOid != "") // Counter BOID
            {
                strsql = "select * from Client_master where cm_cd = '" + CouterBOid + "' and cm_active = '01'";
                rstemp = objUtility.OpenDataTable(strsql);
                if (rstemp.Rows.Count == 0)
                {
                    return "Invalid Counter BO Id";
                }
            }
            else
            {
                return "Counter BO Id Cannot Be Blank";
            }
            if (Exchange != "") // Exchange
            {
                strsql = "select " +
                         "a.bp_id 'STOCK EXCHENGE', " +
                         "a.bp_name 'NAME', " +
                         "a.bp_assd_cc_cmid 'CC ID', " +
                         "p.bp_name 'NAME' " +
                         "from Bpmaster a, Bpmaster p " +
                         "where a.bp_id = p.bp_id and a.bp_role = '02' and p.bp_role = '02' and a.bp_id = '" + Exchange + "'";
                rstemp = objUtility.OpenDataTable(strsql);
                if (rstemp.Rows.Count == 0)
                {
                    return "Invalid Exchange Code";
                }
            }
            if (Segment != "") // Segment
            {
                strsql = "select * from Clientsub_master where cs_module ='CS26' and cs_code = '" + Segment + "'";
                rstemp = objUtility.OpenDataTable(strsql);
                if (rstemp.Rows.Count == 0)
                {
                    return "Invalid Segment Code";
                }
            }
            if (UCC != "") // UCC
            {
                strsql = "select " +
                     "Case cud_Exchnge when '11' then 'BSE' when '12' then 'NSE' when '23' then 'MCX' when '22' then 'NCDEX' else cud_Exchnge end as 'Exchange', " +
                     "Case cud_segment when '01' then 'Cash' when '02' then 'F&O' when '03' then 'Currency' when '04' then 'SLB' when '05' then 'Commodity' when '06' then 'Debt' else cud_segment end as 'Segment', " +
                     "cud_UCC, " +
                     "cud_cmid, " +
                     "cud_tmid " +
                     "from Client_UCC_Details where cud_UCC = '" + UCC + "'";
                rstemp = objUtility.OpenDataTable(strsql);
                if (rstemp.Rows.Count == 0)
                {
                    return "Invalid UCC Code";
                }
            }
            if (CMid != "") // CM Id
            {
                strsql = "select " +
                     "Case cud_Exchnge when '11' then 'BSE' when '12' then 'NSE' when '23' then 'MCX' when '22' then 'NCDEX' else cud_Exchnge end as 'Exchange', " +
                     "Case cud_segment when '01' then 'Cash' when '02' then 'F&O' when '03' then 'Currency' when '04' then 'SLB' when '05' then 'Commodity' when '06' then 'Debt' else cud_segment end as 'Segment', " +
                     "cud_UCC, " +
                     "cud_cmid, " +
                     "cud_tmid " +
                     "from Client_UCC_Details where cud_UCC = '" + UCC + "' and cud_cmid = '" + CMid + "'";
                rstemp = objUtility.OpenDataTable(strsql);
                if (rstemp.Rows.Count == 0)
                {
                    return "Invalid CM Id Code";
                }
            }
            if (EntryBy != "") // Entry By
            {
                if (EntryBy != "TM" && EntryBy != "CP")
                {
                    return "Invalid Entry By Code";
                }
            }
            if (TMId != "") // TM Id
            {
                strsql = "select " +
                     "Case cud_Exchnge when '11' then 'BSE' when '12' then 'NSE' when '23' then 'MCX' when '22' then 'NCDEX' else cud_Exchnge end as 'Exchange', " +
                     "Case cud_segment when '01' then 'Cash' when '02' then 'F&O' when '03' then 'Currency' when '04' then 'SLB' when '05' then 'Commodity' when '06' then 'Debt' else cud_segment end as 'Segment', " +
                     "cud_UCC, " +
                     "cud_cmid, " +
                     "cud_tmid " +
                     "from Client_UCC_Details where cud_UCC = '" + UCC + "' and cud_tmid = '" + TMId + "'";
                rstemp = objUtility.OpenDataTable(strsql);
                if (rstemp.Rows.Count == 0)
                {
                    return "Invalid TM Id Code";
                }
            }
            return "Valid";
        }
        public string fnEarlyPayInFindValidation(string InstrumentType, string InternalRefNo, string TransectionType)
        {
            DataTable ds;
            if (InternalRefNo != "") //internal ref no
            {
                strsql = "select COUNT(*) from Trxbackoffice where tb_internal_refno = '" + InternalRefNo + "'";
                ds = objUtility.OpenDataTable(strsql);
                if (Convert.ToInt16(ds.Rows[0][0].ToString()) == 0)
                {
                    return "Invalid Internal Ref No.";
                }
                strsql = "select * from Trxbackoffice where tb_internal_refno ='" + InternalRefNo + "' and tb_instcd = '" + InstrumentType + "' and tb_trx_type = '" + TransectionType + "'";
                ds = objUtility.OpenDataTable(strsql);
                if (ds.Rows.Count == 0)
                {
                    return "Internal Ref No not found";
                }
            }
            else
            {
                return "Please Enter Internal Ref No.";
            }
            if (InstrumentType != "")
            {
                strsql = "select COUNT(*) from Instrument_master where im_instcd = " + InstrumentType;
                ds = objUtility.OpenDataTable(strsql);
                if (Convert.ToInt16(ds.Rows[0][0].ToString()) == 0)
                {
                    return "Please Enter Valid Instrument Type";
                }
            }
            else
            {
                return "Please Enter Instrument Type";
            }
            if (TransectionType == "")
            {
                return "Transection Type Cannot Be Blank";
            }
            return "Valid";
        }
        public void fnEarlyPayInTempTable(SqlConnection con, SqlTransaction objTrans)
        {
            strsql = "Create Table #TempEarlyPayIn (";
            strsql += "SrNo int identity(1,1),";
            strsql += "Transection_Id char(15),";
            strsql += "CMBO_ID char(25),";
            strsql += "CMBOID_Name char(50),";
            strsql += "ISIN char(15),";
            strsql += "ISIN_Name char(50),";
            strsql += "Qty numeric(18,3),";
            strsql += "Settlement char(15),";
            strsql += "Status char(30),";
            strsql += "Branch char(8),";
            strsql += "Branch_Name char(30),";
            strsql += "Rate money,";
            strsql += "Value money,";
            strsql += "Cash char(4),";
            strsql += "Flag char(1),";
            strsql += "Charges money,";
            strsql += "Auth char(1),";
            strsql += "Counter_BO_id char(25),";
            strsql += "Remark char(100),";
            strsql += "ExchangeID char(8),";
            strsql += "CC_Id char(8),";
            strsql += "Market_Type char(8),";
            strsql += "Time char(8),";
            strsql += "Exchange char(8),";
            strsql += "Exchange_Name char(30),";
            strsql += "Segment char(8),";
            strsql += "Segment_Name char(15),";
            strsql += "UCC char(8),";
            strsql += "CM_Id char(8),";
            strsql += "Entry_By char(8),";
            strsql += "TM_Id char(8)";
            strsql += ")";

            objUtility.ExecuteSQL(strsql, con, objTrans);
        }
        public dynamic fnRTrimEarlyPayInTable(SqlConnection con, SqlTransaction objTrans)
        {
            strsql = "select ";
            strsql += "rtrim(SrNo) SrNo,";
            strsql += "rtrim(Transection_Id) Transection_Id,";
            strsql += "rtrim(CMBO_ID) CMBO_ID,";
            strsql += "rtrim(CMBOID_Name) CMBOID_Name,";
            strsql += "rtrim(ISIN) ISIN,";
            strsql += "rtrim(ISIN_Name) ISIN_Name,";
            strsql += "rtrim(Qty) Qty,";
            strsql += "rtrim(Settlement) Settlement,";
            strsql += "rtrim(Status) Status,";
            strsql += "rtrim(Branch) Branch,";
            strsql += "rtrim(Branch_Name) Branch_Name,";
            strsql += "rtrim(Rate) Rate,";
            strsql += "rtrim(Value) Value,";
            strsql += "rtrim(Cash) Cash,";
            strsql += "rtrim(Flag) Flag,";
            strsql += "rtrim(Charges) Charges,";
            strsql += "rtrim(Auth) Auth,";
            strsql += "rtrim(Counter_BO_id) Counter_BO_id,";
            strsql += "rtrim(Remark) Remark,";
            strsql += "rtrim(ExchangeID) ExchangeID,";
            strsql += "rtrim(CC_Id) CC_Id,";
            strsql += "rtrim(Market_Type) Market_Type,";
            strsql += "rtrim(Time) Time,";
            strsql += "rtrim(Exchange) Exchange,";
            strsql += "rtrim(Exchange_Name) Exchange_Name,";
            strsql += "rtrim(Segment) Segment,";
            strsql += "rtrim(Segment_Name) Segment_Name,";
            strsql += "rtrim(UCC) UCC,";
            strsql += "rtrim(CM_Id) CM_Id,";
            strsql += "rtrim(Entry_By) Entry_By,";
            strsql += "rtrim(TM_Id) TM_Id ";
            strsql += "from #TempEarlyPayIn";

            DataTable Dt = objUtility.OpenDataTable(strsql, con, objTrans);
            return Dt;
        }
        #endregion

        public dynamic SlipIssueAdd(string UserId, SlipIssueReq req)
        {
            try
            {
                //Slip_Issue_Type = "I" , "B", "P"
                if (SlipIssueValidation(req.Slip_Issue_Type, req.Id, req.Date, req.Ref_Date, req.Slip_No, req.Instrument, req.Leaves) == "Valid")
                {
                    string Status = req.Slip_Issue_Type.Trim().ToUpper();
                    string Id = req.Id.Trim();
                    string IssueDate = req.Date.Trim();
                    string RefDate = req.Ref_Date.Trim();
                    string RefNo = req.Ref_No.Trim();
                    string InstCd = req.Instrument.Trim();
                    double ChequeNo = req.Slip_No;
                    string MkrDt = DateTime.Now.ToString("yyyyMMdd");
                    double ToNo = req.Slip_No + req.Leaves - 1;

                    strsql = "update Chequemaster set ";
                    if (Status != "B")
                    {
                        strsql += "chm_cmcd = '" + Id + "',";
                    }
                    strsql += "chm_refno = '" + RefNo + "',";
                    strsql += "chm_refdate = '" + RefDate + "',";
                    strsql += "chm_status = '" + Status + "',";
                    strsql += "chm_issuedate = '" + IssueDate + "',";
                    if (Status == "P")
                    {
                        strsql += "chm_remarks = 'POA',";
                    }
                    else
                    {
                        strsql += "chm_remarks = '',";
                    }
                    strsql += "mkrid = '" + UserId + "',";
                    strsql += "mkrdt = '" + MkrDt + "',";
                    strsql += "chm_fromno = " + ChequeNo + ",";
                    strsql += "chm_tono = " + ToNo + ",";
                    if (Status != "B")
                    {
                        strsql += "chm_allow = 'Y' ";
                    }
                    strsql += "where chm_chqno = " + ChequeNo + " and chm_instcd = " + InstCd;
                    objUtility.ExecuteSQL(strsql);

                    return "Success";
                }
                else
                {
                    return SlipIssueValidation(req.Slip_Issue_Type, req.Id, req.Date, req.Ref_Date, req.Slip_No, req.Instrument, req.Leaves);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic SlipIssueFind(SlipIssueRequest req)
        {
            try
            {
                DataTable Dt;
                DateTime FromDt, ToDt;
                string strWhere = "", strInstcd = "", FromClient;
                if (req.Issued_To == "1")
                {
                    if (req.Filter.Client != null)
                    {
                        if (req.Filter.Client.All(y => y != ""))
                        {
                            var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Client.ToArray(), "##"));
                            strWhere += " and chm_cmcd = '" + fltr.Replace("##", "','") + "'";

                            FromClient = fltr.Replace("##", "','");

                            strsql = "select * from Client_master where cm_cd = '" + FromClient + "'";
                            Dt = objUtility.OpenDataTable(strsql);
                            if (Dt.Rows.Count == 0)
                            {
                                return "Invalid Client Code";
                            }
                        }
                    }
                }
                if (req.Issued_To == "1" || req.Issued_To == "2")
                {
                    if (req.Filter.Group != null)
                    {
                        if (req.Filter.Group.All(y => y != ""))
                        {
                            var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Group.ToArray(), "##"));
                            strWhere += " and cm_groupcd = '" + fltr.Replace("##", "','") + "'";

                            strsql = "select * from Group_master where gr_cd = '" + fltr.Replace("##", "','") + "'";
                            Dt = objUtility.OpenDataTable(strsql);

                            if (Dt.Rows.Count == 0)
                            {
                                return "Invalid Group Code";
                            }
                        }
                    }
                    if (req.Filter.Family != null)
                    {
                        if (req.Filter.Family.All(y => y != ""))
                        {
                            var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Family.ToArray(), "##"));
                            strWhere += " and cm_familycd = '" + fltr.Replace("##", "','") + "'";

                            strsql = "select * from Family_master where fm_cd = '" + fltr.Replace("##", "','") + "'";
                            Dt = objUtility.OpenDataTable(strsql);

                            if (Dt.Rows.Count == 0)
                            {
                                return "Invalid Family Code";
                            }
                        }
                    }
                }
                if (req.Issued_To == "2")
                {
                    if (req.Filter.Family != null)
                    {
                        if (req.Filter.Branch.All(y => y != ""))
                        {
                            var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Branch.ToArray(), "##"));
                            strWhere += " and bm_branchcd = '" + fltr.Replace("##", "','") + "'";

                            strsql = "select * from Branch_master where bm_branchcd = '" + fltr.Replace("##", "','") + "'";
                            Dt = objUtility.OpenDataTable(strsql);

                            if (Dt.Rows.Count == 0)
                            {
                                return "Invalid Branch Code";
                            }
                        }
                    }
                }
                if (req.Instrument_Type != null)
                {
                    if (req.Instrument_Type.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Instrument_Type.ToArray(), "##"));
                        strInstcd += " and im_instcd in (" + fltr.Replace("##", "','") + ")";

                        for (int i = 0; i < req.Instrument_Type.Count; i++)
                        {
                            strsql = "select * from Instrument_master where im_instcd = '" + objUtility.mfnReplaceForSQLInjection(req.Instrument_Type[i]) + "'";
                            Dt = objUtility.OpenDataTable(strsql);

                            if (Dt.Rows.Count == 0)
                            {
                                return "Invalid [ " + req.Instrument_Type[i] + " ] Instrument Type";
                            }
                        }
                    }
                    else
                    {
                        return "Select At Least One Instrument Type";
                    }
                }
                else
                {
                    return "Select At Least One Instrument Type";
                }

                if (req.From_Date == "")
                {
                    return "From Date Cannot Be Blank";
                }

                if (req.To_Date == "")
                {
                    return "To Date Cannot Be Blank";
                }

                FromDt = DateTime.ParseExact(req.From_Date, "yyyyMMdd", null);
                ToDt = DateTime.ParseExact(req.To_Date, "yyyyMMdd", null);

                if (FromDt > ToDt)
                {
                    return "From Date Cannot Be Grater Than To Date";
                }

                if (strWhere.Length > 0)
                {
                    strWhere = strWhere.Substring(4);
                }

                if (req.Issued_To == "1")
                {
                    strsql = "select chm_cmcd 'ClientID',cm_name 'ClientName',";
                }
                else if (req.Issued_To == "2")
                {
                    strsql = "select chm_branchcd 'BranchID',bm_branchname 'BranchName',";
                }
                else
                {
                    strsql = "select chm_cmcd 'ClientID',cpm_firstname 'ClientName',";
                }

                strsql += "im_desc 'Instrument', " +
                    "chm_issuedate 'IssueDate',chm_fromno 'FromNo',chm_tono 'ToNo',chm_booksize 'Forms',Chequemaster.mkrid 'MakerID',chm_refno 'ReferenceNo',Chequemaster.mkrdt 'MakerDate', " +
                    "case chm_allow when 'Y' then 'Ready To Export' when 'S' then 'Success' when 'E' then 'Exported' else 'Slips Not Exported [Old Slip]' end 'Status' ";

                if (req.Issued_To == "1")
                {
                    strsql += "from Chequemaster,Client_master,Instrument_master " +
                    "where cm_cd = chm_cmcd and im_instcd = chm_instcd and chm_status in ('I') " + strInstcd + " and " + strWhere + " and chm_issuedate between '" + req.From_Date + "' and '" + req.To_Date + "' " +
                    "order by chm_cmcd";
                }
                else if (req.Issued_To == "2")
                {
                    strsql += "from Chequemaster,Branch_master,Instrument_master,Client_master " +
                        "where chm_branchcd = bm_branchcd and cm_brboffcode = bm_branchcd and chm_cmcd is null and chm_issuedate between '" + req.From_Date + "' and '" + req.To_Date + "' and chm_instcd = im_instcd and chm_status = 'B' and " + strWhere + strInstcd +
                        "order by chm_branchcd";
                }
                else
                {
                    strsql += "from Chequemaster,Corporate_poa_master,Instrument_master " +
                        "where cpm_poaid = chm_cmcd and im_instcd= chm_instcd and chm_status in ('I','P') and chm_issuedate between '" + req.From_Date + "' and '" + req.To_Date + "' " + strInstcd +
                        "order by chm_cmcd";
                }

                Dt = objUtility.OpenDataTable(strsql);

                if (Dt.Rows.Count > 0)
                {
                    return Dt;
                }
                return "Record Not Found";

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region Slip Issue Functions
        public string SlipIssueValidation(string SlipType, string Id, string IssueDate, string RefDate, double SlipNo, string InstCd, double Leaves)
        {
            DataTable Dt;
            DateTime ExecDt, FreezDt, RefDt;

            strsql = "select sp_sysvalue from Sysparameter where sp_parmcd = 'cmschedule'";
            string cm_schedule = objUtility.OpenDataTable(strsql).Rows[0][0].ToString().Trim();
            if (SlipType != "")
            {
                if (SlipType == "I" || SlipType == "B" || SlipType == "P")
                {
                }
                else
                {
                    return "Invalid Slip Type";
                }
            }
            if (SlipType == "I")
            {
                if (Id != "")
                {
                    strsql = "Select * from Client_master,Beneficiary_status where cm_active = bs_code and cm_cd = '" + Id + "' and cm_schedule = '" + cm_schedule + "'";
                    Dt = objUtility.OpenDataTable(strsql);
                    if (Dt.Rows.Count == 0)
                    {
                        return "User Client Not Found";
                    }
                    else if (Dt.Rows[0]["cm_active"].ToString().Trim() == "03" || Dt.Rows[0]["cm_active"].ToString().Trim() == "04" || Dt.Rows[0]["cm_active"].ToString().Trim() == "05")
                    {
                        return "Client Is " + Dt.Rows[0]["bs_description"].ToString().Trim();
                    }
                    if (Dt.Rows[0]["cm_freezeyn"].ToString().Trim() != "" && Dt.Rows[0]["cm_freezedt"].ToString().Trim() != "")
                    {
                        ExecDt = DateTime.ParseExact(IssueDate, "yyyyMMdd", null);
                        FreezDt = DateTime.ParseExact((Dt.Rows[0]["cm_freezedt"].ToString().Trim()), "yyyyMMdd", null);
                        if ((Dt.Rows[0]["cm_freezeyn"].ToString().Trim() == "1" || Dt.Rows[0]["cm_freezeyn"].ToString().Trim() == "3") && (FreezDt > ExecDt))
                        {
                            return "User Client Freeze";
                        }
                        else if ((Dt.Rows[0]["cm_freezeyn"].ToString().Trim() == "1" || Dt.Rows[0]["cm_freezeyn"].ToString().Trim() == "3") && (FreezDt <= ExecDt))
                        {
                            return "User Client Freeze";
                        }
                    }
                }
                else
                {
                    return "Client Code Cannot Be Blank";
                }
            }
            else if (SlipType == "P")
            {
                if (Id != "")
                {
                    strsql = "select cpm_firstname from corporate_poa_master where cpm_poaid = '" + Id + "'";
                    Dt = objUtility.OpenDataTable(strsql);
                    if (Dt.Rows.Count == 0)
                    {
                        return "User Client Not Found";
                    }
                }
                else
                {
                    return "User Client Code Cannot Be Blank";
                }
            }
            else if (SlipType == "B")
            {
                if (Id != "")
                {
                    strsql = "select bm_branchname from Branch_master where bm_branchcd = '" + Id + "'";
                    Dt = objUtility.OpenDataTable(strsql);
                    if (Dt.Rows.Count == 0)
                    {
                        return "Branch Not Found";
                    }
                }
                else
                {
                    return "Branch Code Cannot Be Blank";
                }
            }
            if (SlipNo != 0)
            {
                strsql = "select top 1 * from Chequemaster where chm_instcd = '" + InstCd + "' and chm_booksize = '" + Leaves + "' and chm_status = 'N'";
                Dt = objUtility.OpenDataTable(strsql);
                if (Dt.Rows.Count > 0)
                {
                    if (Conversion.Val(Dt.Rows[0]["chm_chqno"].ToString().Trim()) != SlipNo)
                    {
                        return "Invalid Slip No";
                    }
                }
                else
                {
                    return "Cheque Book is Not Entered in Stock";
                }
                //strsql = "select * from Chequemaster where chm_chqno = '" + SlipNo + "' and chm_instcd = '" + InstCd + "' and chm_booksize = '" + Leaves + "'";
                //Dt = objUtility.OpenDataTable(strsql);
                //if(Dt.Rows.Count == 0)
                //{
                //    return "Cheque book is not entered in stock.";
                //}
                //else
                //{
                //    if(Dt.Rows[0]["chm_status"].ToString().Trim() == "I")
                //    {
                //        return "Cheque book is issue to " + Dt.Rows[0]["chm_cmcd"].ToString().Trim();
                //    }
                //    else if(Dt.Rows[0]["chm_status"].ToString().Trim() == "L" || Dt.Rows[0]["chm_status"].ToString().Trim() == "A")
                //    {
                //        return "Cheque book is created as loose cheque";
                //    }
                //    else if(Dt.Rows[0]["chm_status"].ToString().Trim() == "D")
                //    {
                //        return "Cheque book is destroyed";
                //    }
                //    else if(Dt.Rows[0]["chm_status"].ToString().Trim() == "N")
                //    {
                //        return "You are not Issuing in Sequence";
                //    }
                //    else if(Dt.Rows[0]["chm_status"].ToString().Trim() == "B")
                //    {
                //        return "Cheque book is Issued to Branch";
                //    }
                //    else if(Dt.Rows[0]["chm_status"].ToString().Trim() == "P")
                //    {
                //        return "Cheque book is issued to " + Dt.Rows[0]["chm_cmcd"].ToString().Trim();
                //    }
                //    else
                //    {
                //        return "This Cheque book cannot be issued";
                //    }
                //}
                //strsql = "select * from Export_details where ed_batchno = '" + SlipNo + "'";
                //Dt = objUtility.OpenDataTable(strsql);
                //if(Dt.Rows.Count > 0)
                //{
                //    if(Dt.Rows[0]["ed_status"].ToString().Trim() == "S")
                //    {
                //        return "Slip No. " + Dt.Rows[0]["ed_batchno"].ToString().Trim() + " is already issued to " + Dt.Rows[0]["ed_cmcd"].ToString().Trim() + " and has already been used";
                //    }
                //    else if(Dt.Rows[0]["ed_status"].ToString().Trim() == "N")
                //    {
                //        return "Slip No. " + Dt.Rows[0]["ed_batchno"].ToString().Trim() + " is already issued to " + Dt.Rows[0]["ed_cmcd"].ToString().Trim() + " .";
                //    }
                //    else
                //    {
                //        return "Slip No. " + Dt.Rows[0]["ed_batchno"].ToString().Trim() + " is already issued to " + Dt.Rows[0]["ed_cmcd"].ToString().Trim() + " and has already been destroyed";
                //    }
                //}
            }
            else
            {
                return "Slip No Cannot Be Blank";
            }
            if (InstCd != "")
            {
                strsql = "select im_desc from Instrument_master where im_instcd = '" + InstCd + "'";
                Dt = objUtility.OpenDataTable(strsql);
                if (Dt.Rows.Count == 0)
                {
                    return "Invalid Instrument Code";
                }
            }
            else
            {
                return "Instrument Code Cannot Be Blank";
            }
            if (Leaves == 0)
            {
                return "Leaves Cannot Be Blank";
            }
            if (RefDate == "")
            {
                return "Reference Date Cannot Be Blank";
            }
            else if (IssueDate == "")
            {
                return "Issue Date Cannot Be Blank";
            }
            if (RefDate != "" && IssueDate != "")
            {
                ExecDt = DateTime.ParseExact(IssueDate, "yyyyMMdd", null);
                RefDt = DateTime.ParseExact(RefDate, "yyyyMMdd", null);
                if (RefDt > ExecDt)
                {
                    return "Reference Date Cannot Be Greater than Issue Date";
                }
            }
            return "Valid";
        }
        public string SlipIssueFindValidation(string SlipType, string Id, string SlipNo)
        {
            DataTable Dt;
            if (SlipType != "")
            {
                if (SlipType == "I" || SlipType == "B" || SlipType == "P")
                {
                }
                else
                {
                    return "Invalid Slip Type";
                }
            }
            else
            {
                return "Slip Issue Type Cannot Be Blank";
            }
            if (SlipType == "I")
            {
                if (Id != "")
                {
                    strsql = "select sp_sysvalue from Sysparameter where sp_parmcd = 'cmschedule'";
                    string cm_schedule = objUtility.OpenDataTable(strsql).Rows[0][0].ToString().Trim();

                    strsql = "Select * from Client_master,Beneficiary_status where cm_active = bs_code and cm_cd = '" + Id + "' and cm_schedule = '" + cm_schedule + "'";
                    Dt = objUtility.OpenDataTable(strsql);
                    if (Dt.Rows.Count == 0)
                    {
                        return "Invalid Client Id";
                    }
                }
                else
                {
                    return "Client Id Cannot Be Blank";
                }
            }
            else if (SlipType == "P")
            {
                if (Id != "")
                {
                    strsql = "select cpm_firstname from corporate_poa_master where cpm_poaid = '" + Id + "'";
                    Dt = objUtility.OpenDataTable(strsql);
                    if (Dt.Rows.Count == 0)
                    {
                        return "Invalid Client Id";
                    }
                }
                else
                {
                    return "Client Id Cannot Be Blank";
                }
            }
            else if (SlipType == "B")
            {
                if (Id != "")
                {
                    strsql = "select bm_branchname from Branch_master where bm_branchcd = '" + Id + "'";
                    Dt = objUtility.OpenDataTable(strsql);
                    if (Dt.Rows.Count == 0)
                    {
                        return "Invalid Branch Id";
                    }
                }
                else
                {
                    return "Branch Id Cannot Be Blank";
                }
            }
            if (SlipNo != "")
            {
                strsql = "select * from Chequemaster where chm_chqno = '" + SlipNo + "'";
                Dt = objUtility.OpenDataTable(strsql);
                if (Dt.Rows.Count == 0)
                {
                    return "Invalid Slip No";
                }
            }
            else
            {
                return "Slip No. Cannot Be Blank";
            }
            return "Valid";
        }
        #endregion

        public dynamic SlipStop(string UserId, SlipStopReq req)
        {
            try
            {
                DataTable DT;
                bool blnAdd;
                string MkrDt = DateTime.Now.ToString("yyyyMMdd");
                DateTime ToDay, RefDt, Dt;
                ToDay = DateTime.ParseExact(MkrDt, "yyyyMMdd", null);

                if (req.RefDate != "")
                {
                    RefDt = DateTime.ParseExact(req.RefDate, "yyyyMMdd", null);
                    if (RefDt > ToDay)
                    {
                        return "Ref. Date Cannot Be Past Date";
                    }
                }
                if (req.Date != "")
                {
                    Dt = DateTime.ParseExact(req.Date, "yyyyMMdd", null);
                    if (Dt > ToDay)
                    {
                        return "Date Cannot Be Past Date";
                    }
                }
                if (req.To != 0)
                {
                    strsql = "Select * from Chequemaster, Instrument_master where im_instcd = '" + req.Instrument + "' and chm_instcd = im_instcd and chm_status ='I' and chm_cmcd ='" + req.ClientID + "' and " + req.To + " between chm_chqno and chm_chqno + chm_booksize-1";
                    DT = objUtility.OpenDataTable(strsql);
                    if (DT.Rows.Count == 0)
                    {
                        return "Invalid To No.";
                    }
                }
                else
                {
                    return "To No. Cannot Be Zero";
                }
                if (req.From != 0 && req.To != 0)
                {
                    if (req.From > req.To)
                    {
                        return "From No. Cannot Be Greater than To No.";
                    }
                }
                if (req.From != 0)
                {
                    strsql = "Select * from Chequemaster, Instrument_master where im_instcd = '" + req.Instrument + "' and chm_instcd = im_instcd and chm_status ='I' and chm_cmcd ='" + req.ClientID + "' and " + req.From + " between chm_chqno and chm_chqno + chm_booksize-1";
                    DT = objUtility.OpenDataTable(strsql);
                    if (DT.Rows.Count == 0)
                    {
                        return "Invali From No.";
                    }

                    string strSetData = string.Empty;
                    strsql = "select * from Chequestop, Instrument_master where im_instcd = '" + req.Instrument + "' and chs_instcd = im_instcd and chs_chqno between " + req.From + " and " + req.To + " and chs_cmcd='" + req.ClientID + "'";
                    DataSet DSChqst = new DataSet();
                    DSChqst = objUtility.OpenDataSet(strsql);
                    if (DSChqst.Tables[0].Rows.Count == 0)
                        strSetData = "STOP" + "|" + "NEW" + "|";
                    else
                    {
                        int intstatR = 0;
                        int intstatS = 0;
                        int intstat = 0;
                        do
                        {
                            if (DSChqst.Tables[0].Rows[intstat]["chs_status"].ToString() == "R")
                                intstatR = intstatR + 1;
                            else if (DSChqst.Tables[0].Rows[intstat]["chs_status"].ToString() == "S")
                                intstatS = intstatS + 1;
                            intstat++;
                        } while (intstat <= DSChqst.Tables[0].Rows.Count - 1);
                        if (intstatR == req.To - req.From + 1)
                            strSetData = "STOP" + "|";
                        else if (intstatS == req.To - req.From + 1)
                            strSetData = "REVOKE" + "|";
                        else
                            strSetData = "NONE" + "|";
                        strSetData = strSetData + DSChqst.Tables[0].Rows[0]["chs_chqdate"].ToString().Trim() + "|" + DSChqst.Tables[0].Rows[0]["chs_refdate"].ToString().Trim() + "|" + DSChqst.Tables[0].Rows[0]["chs_date"].ToString().Trim() + "|";
                        strSetData = strSetData + DSChqst.Tables[0].Rows[0]["chs_reference"].ToString().Trim() + "|" + DSChqst.Tables[0].Rows[0]["chs_remark"].ToString().Trim() + "|";
                    }
                    string[] arrSetData = strSetData.Trim().Split('|');
                    if (arrSetData[0] == "NONE")
                    {
                        return "Cannot Revoke/Stop in the Range.";
                    }
                }

                if (req.Instrument != "")
                {
                    strsql = "Select * from Chequemaster,Instrument_master where chm_instcd = im_instcd and chm_cmcd = '" + req.ClientID + "' and im_instcd = '" + req.Instrument + "'";
                    DT = objUtility.OpenDataTable(strsql);
                    if (DT.Rows.Count == 0)
                    {
                        return "Instrument not issued to this Client";
                    }
                }
                else
                {
                    return "Instrument Cannot Be Blank";
                }
                if (req.ClientID != "")
                {
                    string Text = SlipAutoNumber(req.ClientID.Trim());
                    string NameText = fnBOIDvalidate(Text, 1);
                    if (NameText.Trim() == "")
                    {
                        return "Invalid Client ID";
                    }
                }
                else
                {
                    return "Client Id Cannot Be Blank";
                }
                strsql = "Select im_trtype from Instrument_master where im_instcd= '" + req.Instrument + "'";
                string StrTrxType = objUtility.OpenDataTable(strsql).Rows[0][0].ToString().Trim();

                strsql = "select * from Used_slip where us_instcd = '" + req.Instrument + "' and us_trxtype = '" + StrTrxType + "' and us_irn between  " + req.From + " and " + req.To;
                DT = objUtility.OpenDataTable(strsql);
                if (DT.Rows.Count > 0)
                {
                    return "The Slip Cannot be Stopped as there is a transaction for the Slip";
                }
                strsql = "select * from Chequestop where chs_instcd=" + req.Instrument + " and chs_chqno between " + req.From + " and " + req.To + " order by chs_chqno";
                DT = objUtility.OpenDataTable(strsql);
                if (DT.Rows.Count == 0)
                {
                    blnAdd = true;
                }
                else
                {
                    blnAdd = false;
                }
                string RefDate;
                if (req.RefDate == "")
                {
                    RefDate = MkrDt;
                }
                else
                {
                    RefDate = req.RefDate;
                }

                string SysParam = GetSysParm("SLPISSUEVER");
                if (blnAdd)
                {
                    strsql = "insert into Chequestop (chs_cmcd,chs_chqno,chs_chqdate,chs_reference,";
                    strsql += " chs_refdate,chs_status,chs_date,chs_instcd,chs_remark,mkrid,mkrdt,chs_allow)";
                    strsql += " values ('" + req.ClientID + "','" + req.From + "',";
                    strsql += " '" + DateTime.Today.ToString("yyyyMMdd") + "','" + req.Reference + "','" + RefDate + "',";
                    strsql += " 'S','" + req.Date + "','" + req.Instrument + "',";
                    strsql += " '" + req.Remarks + "',";
                    strsql += " '" + UserId.ToUpper() + "','" + DateTime.Today.ToString("yyyyMMdd") + "',";
                    if (SysParam.Trim() == "Y")
                    {
                        strsql += "'Y'";
                    }
                    else
                    {
                        strsql += "''";
                    }
                    strsql += ")";

                    objUtility.ExecuteSQL(strsql);
                }
                else
                {
                    strsql = "update Chequestop set";
                    strsql += " chs_cmcd = '" + req.ClientID + "',chs_chqno='" + req.From + "',";
                    strsql += " chs_chqdate='" + DateTime.Today.ToString("yyyyMMdd") + "',chs_reference='" + req.Reference + "',chs_refdate='" + RefDate + "',";
                    strsql += " chs_status='S',chs_date='" + req.Date + "',chs_instcd='" + req.Instrument + "',";
                    strsql += " chs_remark='" + req.Remarks + "', mkrdt='" + DateTime.Today.ToString("yyyyMMdd") + "', ";
                    strsql += " mkrid='" + UserId.ToUpper() + "', chs_allow = ";
                    if (SysParam.Trim() == "Y")
                    {
                        strsql += " 'Y'";
                    }
                    else
                    {
                        strsql += " ''";
                    }
                    strsql += " where chs_chqno='" + req.From + "'";
                    objUtility.ExecuteSQL(strsql);
                }

                if (blnAdd)
                {
                    return "Slip Saved Successfully";
                }
                return "Slip Stopped Succesfully";
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region Slip Stop Functions
        public string SlipAutoNumber(string idno)
        {
            string strAuto, temp, temp1, result = string.Empty;
            int X, length, Y = 0;

            string GstrCdslDpid = GetSysParm("Dpid");
            if ((Strings.InStr(1, idno, ",", CompareMethod.Text) > 0) || (Strings.InStr(1, idno, ".", CompareMethod.Text) > 0) || (idno.Trim().Length > 0 && idno.Trim().Length < 8))
            {
                if (Strings.InStr(1, idno, ",", CompareMethod.Text) > 0)
                    temp = ",";
                else if (Strings.InStr(1, idno, ".", CompareMethod.Text) > 0)
                    temp = ".";
                else
                    temp = ",";

                X = Strings.InStr(1, idno, temp.ToString(), CompareMethod.Text);
                strAuto = GstrCdslDpid;
                if (X > 0)
                {
                    length = idno.Trim().Length;
                    Y = length - X;
                    temp = Strings.Left(idno, X - 1);
                    temp = strAuto + int.Parse(Strings.Left(idno, X - 1));
                    temp1 = Strings.Right(idno, Y);
                    if (temp.Length + temp1.Length < 16)
                        result = Strings.StrDup(16 - temp.Trim().Length + temp1.Trim().Length, "0");

                    result = temp + result + temp1;
                }
                else
                {
                    result = "0" + Strings.StrDup(7 - idno.Trim().Length, "0") + idno;
                    result = strAuto + result;
                }
                return result;
            }
            else if (idno.Trim().Length == 8 && Information.IsNumeric(idno))
            {
                return GstrCdslDpid + idno;
            }
            else if (idno.Trim().Length == 16 && Information.IsNumeric(idno))
                return idno;
            else
                return "";
        }
        public string fnBOIDvalidate(string strvalue, int intindex)
        {
            string strsql = string.Empty;
            string strName = string.Empty;
            if (intindex == 1)
            {
                strsql = "select cm_name from Client_master with (noLock)";
                strsql += " where cm_cd = '" + strvalue + "' and cm_active = '01' ";// +HttpContext.Current.Session("loginaccess");
            }
            else if (intindex == 2)
            {
                //strsql = "Select ss_name from Securities where ss_cd ='" + strvalue + "'";
                strsql = "Select sc_isinname from Security with (noLock) where sc_isincode ='" + strvalue + "'";
            }
            else if (intindex == 3)
            {
                strsql = "Select se_stlmnt,se_stdt,se_endt  from  Settlements with (noLock) where se_stlmnt = '" + strvalue + "'";
            }
            else if (intindex == 4)
            {
                strsql = "Select gr_desc  from  Group_master with (noLock) where gr_cd ='" + strvalue + "'";
            }
            else if (intindex == 5)
            {
                strsql = "Select fm_desc  from  Family_master with (noLock) where fm_cd ='" + strvalue + "'";
            }
            else if (intindex == 6)
            {
                strsql = "Select cm_name  from  Client_master with (noLock) where cm_cd ='" + strvalue + "' and cm_type='B'";
            }
            else if (intindex == 7)
            {
                strsql = "select bm_branchname from Branch_master with (noLock) where bm_flag='B'  and bm_branchcd= '" + strvalue + "'";
            }
            else if (intindex == 8)
            {
                strsql = "select dp_name from Dps with (noLock) where dp_dpid= '" + strvalue + "'";
            }
            else if (intindex == 9)
            {
                strsql = "select dp_name from Ourdps with (noLock) ,Dps with (noLock) where od_cd= '" + strvalue + "' and od_dpid = dp_dpid ";
            }
            else if (intindex == 10)
            {
                strsql = "Select sm_desc from series_master with (noLock) where sm_seriesid = " + strvalue + "";
            }
            else if (intindex == 11)
            {
                strsql = "Select rm_cd from subbrokers with (noLock) where rm_cd ='" + strvalue + "'";
            }
            else if (intindex == 12)
            {
                strsql = "Select bp_name from bpmaster with (noLock) where bp_id ='" + strvalue + "'";
            }

            DataTable Dt;

            if (GetSysParm("PRODUCT").ToUpper().Trim() == "KYC")
            { Dt = objUtility.OpenDataTable(strsql); }
            else
            { Dt = objUtility.OpenDataTable(strsql); }

            if (Dt.Rows.Count > 0)
            {
                if (intindex != 3)
                {
                    strName = Dt.Rows[0][0].ToString().Trim();
                }
                else if (intindex == 3)
                {
                    strName = (Dt.Rows[0][0].ToString().Trim()) + "|" + (Dt.Rows[0][1]) + "|" + (Dt.Rows[0][2].ToString().Trim()) + "|";
                }
            }
            else
            {
                strName = "";
            }
            return strName;
        }
        #endregion

        public dynamic ClientListing(ClientListingReq req, string loginAccess)
        {
            try
            {
                SqlTransaction SQLTrans;
                DataTable Dt;
                string strRegNo, strRegDt, strClientWhere = "", IsActive = "", strFromDt = "";
                bool IsOpenDt = false, IsCloseDt = false;

                if (req.Filter.ClientID != null)
                {
                    if (req.Filter.ClientID.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.ClientID.ToArray(), "##"));
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
                if (req.Filter.Family != null)
                {
                    if (req.Filter.Family.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Family.ToArray(), "##"));
                        strClientWhere += " or cm_familycd in('" + fltr.Replace("##", "','") + "')";
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
                if (req.Filter.AccOpenDate != null)
                {
                    if (req.Filter.AccOpenDate.FromDate != "")
                    {
                        strFromDt = "'" + req.Filter.AccOpenDate.FromDate + "' and ";
                    }
                    if (req.Filter.AccOpenDate.FromDate != "")
                    {
                        strFromDt += "'" + req.Filter.AccOpenDate.ToDate + "'";
                        IsOpenDt = true;
                    }
                }
                if (req.Filter.CloseDate != null)
                {
                    if (req.Filter.CloseDate.FromDate != "")
                    {
                        strFromDt = "'" + req.Filter.CloseDate.FromDate + "' and ";
                    }
                    if (req.Filter.CloseDate.FromDate != "")
                    {
                        strFromDt += "'" + req.Filter.CloseDate.ToDate + "'";
                        IsCloseDt = true;
                    }
                }

                if (strFromDt != "")
                {
                    if (IsOpenDt)
                    {
                        strFromDt = " and cm_opendate  between " + strFromDt;
                    }
                    if (IsCloseDt)
                    {
                        strFromDt = " and cm_acc_closuredate between " + strFromDt;
                    }
                }
                else if (strFromDt == "" && (IsOpenDt || IsCloseDt))
                {
                    return "From/To Date Cannot Be Blank.";
                }

                if (strClientWhere.Length > 0)
                {
                    strClientWhere = " and (" + strClientWhere.Substring(3) + ") ";

                    if (req.Status == "A")
                    {
                        IsActive = " and cm_active = '01' ";
                    }
                    else if (req.Status == "I")
                    {
                        IsActive = " and cm_active != '01' ";
                    }
                    strsql = "select * from Client_master where " + strClientWhere.Substring(4) + IsActive + loginAccess;
                    Dt = objUtility.OpenDataTable(strsql);
                    if (Dt.Rows.Count == 0)
                    {
                        return "Invalid ID";
                    }
                }


                if (strClientWhere == "" && strFromDt == "")
                {
                    return "Select At Least One.";
                }

                strsql = "select sp_sysvalue from Sysparameter where sp_parmcd = 'cmschedule'";
                string CMSchedule = objUtility.OpenDataTable(strsql).Rows[0][0].ToString().Trim();

                strsql = "select  cm_cd 'ClientID', " +
                    "cm_Name 'ClientName'," +
                    "cm_chgsscheme 'Scheme'," +
                    "(select Ltrim(Rtrim(convert(char,sum(ld_amount)))) From Ledger with (nolock)  Where ld_clientcd = cm_cd) 'Ledger'," +
                    "cm_blsavingcd 'BackOfficeCD'  " +
                    "From Client_master with (nolock)  " +
                    "where cm_schedule = " + CMSchedule + strClientWhere + strFromDt + loginAccess;
                if (req.Status == "A")
                {
                    strsql += " and cm_active = '01' ";
                }
                else if (req.Status == "I")
                {
                    strsql += " and cm_active != '01' ";
                }
                strsql += " Order by cm_cd";

                Dt = objUtility.OpenDataTable(strsql);
                if (Dt.Rows.Count == 0)
                {
                    return "Record Not Found";
                }
                List<ClientListingResponse> ListClientListing = new List<ClientListingResponse>();
                List<TempClientListingRes> TempList = new List<TempClientListingRes>();

                for (int k = 0; k < Dt.Rows.Count; k++)
                {
                    TempClientListingRes TempObj = new TempClientListingRes()
                    {
                        ClientID = Dt.Rows[k]["ClientID"].ToString().Trim(),
                        ClientName = Dt.Rows[k]["ClientName"].ToString().Trim(),
                        Ledger = Dt.Rows[k]["Ledger"].ToString().Trim(),
                        BackOfficeCD = Dt.Rows[k]["BackOfficeCD"].ToString().Trim(),
                        Scheme = Dt.Rows[k]["Scheme"].ToString().Trim(),
                    };
                    TempList.Add(TempObj);
                }

                List<string> Codes = TempList.Select(x => x.ClientID).ToList();
                List<string> Names = TempList.Select(x => x.ClientName).ToList();
                List<string> Ledgers = TempList.Select(x => x.Ledger).ToList();
                List<string> BackofficeCDs = TempList.Select(x => x.BackOfficeCD).ToList();
                List<string> Schemes = TempList.Select(x => x.Scheme).ToList();
                int i = 0;
                foreach (var Code in Codes)
                {
                    ClientListingResponse ObjListing = new ClientListingResponse();
                    string Connection = objUtility.GetConnectionStr();
                    using (SqlConnection SQLCon = new SqlConnection(Connection))
                    {
                        SQLCon.Open();
                        SQLTrans = SQLCon.BeginTransaction();
                        SqlDataAdapter sqlDtAdaptr = new SqlDataAdapter();
                        SqlCommandBuilder sqlCmdBld = new SqlCommandBuilder(sqlDtAdaptr);

                        ObjListing.ClientID = Code;
                        ObjListing.ClientName = Names[i];
                        ObjListing.Ledger = Ledgers[i];
                        ObjListing.BackOfficeCD = BackofficeCDs[i];

                        strsql = "select cg_name 'Description'," +
                            "case When cd_fixed_amount = 0 Then '-' else ltrim(rtrim(convert(char,cast(cd_fixed_amount as decimal(15,2))))) end as 'FixedAmount'," +
                            "case When cd_perc_amount = 0 Then '-' else ltrim(rtrim(convert(char,cast(cd_perc_amount as decimal(15,2))))) end as 'Percentage'," +
                            "case When cd_min_amount = 0 Then '-' else ltrim(rtrim(convert(char,cast(cd_min_amount as decimal(15,2))))) end as 'MinAmount'," +
                            "case When cd_max_amount = 0 Then '-' else ltrim(rtrim(convert(char,cast(cd_max_amount as decimal(15,2))))) end as 'MaxAmount'," +
                            "case When cd_per_certificate = 0 Then '-' else ltrim(rtrim(convert(char,cast(cd_per_certificate as decimal(15,2))))) end as 'PerCertificate' " +
                            "from Chargesdetail with (nolock) , chargesmaster with (nolock)  " +
                            "where cd_code = cg_code and cd_scheme ='" + Schemes[i].Trim() + "' " +
                            "order by case cd_code when 4 then 1 else 2 end, cd_code";

                        Dt = objUtility.OpenDataTable(strsql, SQLCon, SQLTrans);
                        if (Dt.Rows.Count > 0)
                        {
                            ObjListing.Scheme = Dt;
                        }
                        else
                        {
                            ObjListing.Scheme = "Record Not Found";
                        }

                        strsql = "select hld_isin_code as 'ISIN'," +
                            "sc_isinname 'ISINName'," +
                            "bt_description 'BalanceType'," +
                            "Ltrim(RTrim(convert(char, cast((hld_ac_pos) as decimal(15,3))))) as 'Qty'," +
                            "cast((sc_rate) as decimal(15,2)) as 'Rate'," +
                            "cast((sc_rate * hld_ac_pos) as decimal(15,2)) as 'Value' " +
                            "From Holding ,Security ,Client_master ,Beneficiary_type,branch_master  " +
                            "where cm_brboffcode = bm_branchcd  and hld_isin_code = sc_isincode And hld_ac_code = cm_cd and bt_code = hld_ac_type  and hld_ac_code = '" + Code + "' " +
                            "Order By cm_cd, hld_ac_type, sc_isinname";

                        Dt = objUtility.OpenDataTable(strsql, SQLCon, SQLTrans);
                        if (Dt.Rows.Count > 0)
                        {
                            ObjListing.Holding = Dt;
                        }
                        else
                        {
                            ObjListing.Holding = "Record Not Found";
                        }

                        strsql = "Select distinct cpm_firstname 'POAName'," +
                            "cpd_poaid 'POAId'," +
                            "cpd_holderno 'HolderNo'," +
                            "case cpd_POAStatus When 'D' Then 'Deleted' When 'E' then 'Expired' else 'Setup' End 'Status'," +
                            "cpd_setupdate 'POARegistrationDate'  " +
                            "from client_poa_details with (nolock) ,Corporate_poa_master with (nolock)   " +
                            "where cpd_poaid = cpm_poaid and cpd_boid ='" + Code + "'";

                        Dt = objUtility.OpenDataTable(strsql, SQLCon, SQLTrans);
                        if (Dt.Rows.Count > 0)
                        {
                            ObjListing.POA = Dt;
                        }
                        else
                        {
                            ObjListing.POA = "Record Not Found";
                        }

                        strsql = "select cm_clienttype from client_master where cm_schedule = " + CMSchedule + " " + loginAccess + " and cm_cd = '" + Code + "'";
                        string ClientType = objUtility.OpenDataTable(strsql, SQLCon, SQLTrans).Rows[0][0].ToString().Trim();

                        if (",2187,2169,2181,2184,2187,2470,2471,2772,24100,24101,".IndexOf(ClientType) > 0)
                        {
                            strRegNo = "Not Applicable";
                            strRegDt = "Not Applicable";
                        }
                        else
                        {
                            strsql = "select nd_regno, nd_regdate from Nomineedetails with (nolock)  where nd_cmcd = '" + Code + "'";
                            Dt = objUtility.OpenDataTable(strsql, SQLCon, SQLTrans);
                            if (Dt.Rows.Count > 0)
                            {
                                strRegNo = Dt.Rows[0]["nd_regno"].ToString().Trim();
                                strRegDt = Dt.Rows[0]["nd_regdate"].ToString().Trim();
                            }
                            else
                            {
                                strRegNo = "Not Applicable";
                                strRegDt = "Not Applicable";
                            }
                        }

                        strsql = "Create table #TempNominee ( SrNo int identity(1,1), RegistrationNo char(15), RegistrationDate char(15), Name char(45), Address1 char(45), Address2 char(45), Address3 char(45), City char(15), State char(20), Country char(15), Pin char(8) )";
                        objUtility.ExecuteSQL(strsql, SQLCon, SQLTrans);

                        strsql = "Insert into #TempNominee ( Name, Address1, Address2, Address3, City, State, Country, Pin ) " +
                            "( select (cn_NomName + Case when cn_NomMidNm = '' then '' else ' ' + cn_NomMidNm End + Case When cn_NomlastNm = '' then '' Else ' ' + cn_NomlastNm End ) as 'Name',cn_NomAdd1 'Address1',cn_NomAdd2 'Address2',cn_NomAdd3 'Address3', cn_City 'City', cn_State 'State', cn_Country 'Country', cn_NomPin 'Pin' From Client_NomineeDetails with (nolock)  Where cn_cmcd = '" + Code + "' )";
                        objUtility.ExecuteSQL(strsql, SQLCon, SQLTrans);

                        strsql = "update #TempNominee set RegistrationNo = '" + strRegNo + "', RegistrationDate = '" + strRegDt + "'";
                        objUtility.ExecuteSQL(strsql, SQLCon, SQLTrans);

                        strsql = "select * from #TempNominee";

                        Dt = objUtility.OpenDataTable(strsql, SQLCon, SQLTrans);
                        if (Dt.Rows.Count > 0)
                        {
                            ObjListing.Nominee = Dt;
                        }
                        else
                        {
                            ObjListing.Nominee = "Record Not Found";
                        }


                        strsql = "select cm_divbankacno 'BankAccNo'," +
                            "cm_divbankcode 'MICR'," +
                            "bk_name 'Bank'," +
                            "cb_voicemail 'IFSCCode'," +
                            "(case cm_divbranchno when '10' then 'Saving A/C' else (case cm_divbranchno when '11' then 'Current A/C' else (case cm_divbranchno when '13' then 'Cash Credit' else '' end) end) end ) as 'AccType'," +
                            "Case isNull(cb_ecs,'') When 'Y' Then 'Yes' When 'N' Then 'No' Else '' End as 'ECSMandate' " +
                            "from Client_Master with (nolock),Bank_master with (nolock),Client_Backoffice with (nolock)  " +
                            "where cm_cd = cb_cmcd and cb_voicemail = bk_branch and cm_cd ='" + Code + "'";

                        Dt = objUtility.OpenDataTable(strsql, SQLCon, SQLTrans);
                        if (Dt.Rows.Count > 0)
                        {
                            ObjListing.Bank = Dt;
                        }
                        else
                        {
                            ObjListing.Bank = "Record Not Found";
                        }

                        strsql = "select cb_fadd1 'Address1'," +
                            "cb_fadd2 'Address2'," +
                            "cb_fadd3 'Address3'," +
                            "cb_fcity 'City'," +
                            "cb_fstate 'State'," +
                            "cb_fcountry 'Country'," +
                            "cb_fpin 'PinCode'," +
                            "cb_tele1 'Telephone'," +
                            "cb_ffax 'Fax' " +
                            "from Client_Backoffice with (nolock)  " +
                            "where cb_cmcd ='" + Code + "'";

                        Dt = objUtility.OpenDataTable(strsql, SQLCon, SQLTrans);
                        if (Dt.Rows.Count > 0)
                        {
                            ObjListing.PA = Dt;
                        }
                        else
                        {
                            ObjListing.PA = "Record Not Found";
                        }

                        strsql = "select sg_sign from signature with (nolock)  where sg_ac_code = '" + Code + "' and sg_bpflag = 'B'";
                        Dt = objUtility.OpenDataTable(strsql, SQLCon, SQLTrans);
                        if (Dt.Rows.Count > 0)
                        {
                            string B64String = Dt.Rows[0][0].ToString().Trim();
                            byte[] toEncodeAsBytes = ASCIIEncoding.ASCII.GetBytes(B64String);
                            ObjListing.Signature = Convert.ToBase64String(toEncodeAsBytes);
                        }
                        else
                        {
                            ObjListing.Signature = "Signature Not Found";
                        }

                        strsql = "select cm_cd, cm_name,convert(varchar,chm_instcd) as chm_instcd, ";
                        strsql += " convert(varchar,im_desc) as im_desc, chm_chqno,chm_booksize, ";
                        strsql += " chm_issuedate,Chequemaster.mkrid as mkrid, chm_fromno,chm_tono,  ";
                        strsql += " (chm_chqno + chm_booksize) - 1  as chequeto ";
                        strsql += " from Chequemaster with (nolock) ,Instrument_master with (nolock) , Client_master with (nolock)  ";
                        strsql += " where cm_cd= chm_cmcd  and im_instcd= chm_instcd ";
                        strsql += " and chm_status='I' ";
                        strsql += " and chm_cmcd = '" + Code + "'";
                        strsql += " Order by convert(datetime,chm_issuedate) desc,im_desc";
                        DataSet dset = new DataSet();
                        dset = objUtility.OpenDataSet(sqlDtAdaptr, strsql, SQLCon, SQLTrans);

                        if (dset.Tables[0].Rows.Count > 0)
                        {
                            strsql = "create table #TmpStatus ( chm_cmcd char(16), chm_instcd numeric, chm_booksize int, chm_fromno int, chm_tono int, chm_status varchar(10), chm_issuedate char(8))";
                            objUtility.ExecuteSQL(strsql, SQLCon, SQLTrans);

                            for (int iR = 0; iR <= dset.Tables[0].Rows.Count - 1; iR++)
                            {
                                long iFrom = Convert.ToInt64(dset.Tables[0].Rows[iR]["chm_fromno"].ToString().Trim());
                                long iOldFrom = Convert.ToInt64(dset.Tables[0].Rows[iR]["chm_fromno"].ToString().Trim());
                                long iTo = Convert.ToInt64(dset.Tables[0].Rows[iR]["chm_tono"].ToString().Trim());
                                int iloop = 1;
                                string strOldstatus = string.Empty;
                                string strStatus = string.Empty;
                                bool blnlast = false;
                                do
                                {
                                    strStatus = "Unused";
                                    strsql = "select iCount= count(*) from Used_slip where us_instcd=" + dset.Tables[0].Rows[iR]["chm_instcd"].ToString().Trim() + " and us_irn=" + iFrom;
                                    DataSet dsChk = new DataSet();
                                    dsChk = objUtility.OpenDataSet(sqlDtAdaptr, strsql, SQLCon, SQLTrans);

                                    if (dsChk.Tables[0].Rows.Count > 0)
                                    {
                                        if (Convert.ToInt16(dsChk.Tables[0].Rows[0]["iCount"].ToString().Trim()) > 0)
                                        {
                                            strStatus = "Used";
                                        }
                                    }
                                    blnlast = false;
                                    if (strOldstatus != strStatus)
                                    {
                                        if (iloop == 1)
                                        {
                                            strsql = " Insert Into #TmpStatus Values ( ";
                                            strsql += " '" + Code + "', " + dset.Tables[0].Rows[iR]["chm_instcd"].ToString().Trim() + ", " + dset.Tables[0].Rows[iR]["chm_booksize"].ToString().Trim() + ", ";
                                            strsql += iFrom + ", " + dset.Tables[0].Rows[iR]["chm_tono"].ToString().Trim() + ",'" + strStatus + "','" + dset.Tables[0].Rows[iR]["chm_issuedate"].ToString().Trim() + "')";
                                            objUtility.ExecuteSQL(strsql, SQLCon, SQLTrans);
                                            iOldFrom = iFrom;
                                        }
                                        else
                                        {
                                            strsql = "Update #TmpStatus Set chm_tono = " + (iFrom - 1) + " where chm_fromno = " + iOldFrom;
                                            objUtility.ExecuteSQL(strsql, SQLCon, SQLTrans);
                                            iOldFrom = iFrom;

                                            strsql = " Insert Into #TmpStatus Values ( ";
                                            strsql += " '" + Code + "', " + dset.Tables[0].Rows[iR]["chm_instcd"].ToString().Trim() + ", " + dset.Tables[0].Rows[iR]["chm_booksize"].ToString().Trim() + ", ";
                                            strsql += iFrom + ", " + iFrom + ",'" + strStatus + "','" + dset.Tables[0].Rows[iR]["chm_issuedate"].ToString().Trim() + "')";
                                            objUtility.ExecuteSQL(strsql, SQLCon, SQLTrans);
                                            iloop = 0;
                                        }
                                        strOldstatus = strStatus;
                                        blnlast = true;
                                    }
                                    iFrom += 1;
                                    iloop += 1;
                                } while (iFrom <= iTo);
                                if (iFrom > iTo && blnlast == false)
                                {
                                    strsql = "Update #TmpStatus Set chm_tono = " + (iFrom - 1) + " where chm_fromno = " + iOldFrom;
                                    objUtility.ExecuteSQL(strsql, SQLCon, SQLTrans);
                                }
                            }
                            strsql = "Select im_desc 'Instrument', chm_issuedate as 'IssueDate',chm_fromno 'FromNo',chm_tono 'ToNo',chm_booksize 'Forms',chm_status 'Used' ";
                            strsql += " from  #TmpStatus,Instrument_master with (nolock) , Client_master with (nolock) ";
                            strsql += " where cm_cd= chm_cmcd  and im_instcd= chm_instcd ";
                            strsql += " Order by convert(datetime,chm_issuedate) desc,im_desc";
                            DataTable dsX = new DataTable();
                            dsX = objUtility.OpenDataTable(strsql, SQLCon, SQLTrans);

                            if (dsX.Rows.Count > 0)
                            {
                                ObjListing.Slip = dsX;
                            }
                            else
                            {
                                ObjListing.Slip = "Record Not Found";
                            }
                        }
                        else
                        {
                            ObjListing.Slip = "Record Not Found";
                        }

                        strsql = "select cud_boid,cm_name,case cud_Exchnge When '11' Then 'BSE' When '12' Then 'NSE' When '29' Then 'MCX' else bp_name end cud_Exchnge,cud_segment,cud_ucc,cud_cmid,cud_tmid From client_master, client_ucc_details, bpmaster where cm_cd = cud_boid and bp_id=cud_Exchnge and bp_role='02' and cud_boid='" + Code + "' order by cud_Exchnge,cud_tmid,cud_cmid DESC";
                        string strsql1 = "select * from Clientsub_master where cs_module = 'CS25'";

                        DataSet objdataset = objUtility.OpenDataSet(strsql);
                        DataSet objdataset1 = objUtility.OpenDataSet(strsql1);
                        if (objdataset.Tables[0].Rows.Count > 0)
                        {
                            DataTable dt = objdataset.Tables[0];

                            string cud_boid = string.Empty;
                            string cud_Exchnge = string.Empty;
                            string cud_UCC = string.Empty;
                            string cud_cmid = string.Empty;
                            string cud_tmid = string.Empty;

                            DataTable dtNew = new DataTable();
                            dtNew.Clear();
                            dtNew.Columns.Add("ClientID");
                            dtNew.Columns.Add("ClientName");
                            dtNew.Columns.Add("Exchange");
                            dtNew.Columns.Add("UCC");
                            dtNew.Columns.Add("CMID");
                            dtNew.Columns.Add("TMID");
                            for (int j = 0; j <= objdataset1.Tables[0].Rows.Count - 1; j++)
                            {
                                dtNew.Columns.Add(objdataset1.Tables[0].Rows[j]["cs_desc"].ToString());
                            }

                            for (int j = 0; j <= dt.Rows.Count - 1; j++)
                            {
                                if (cud_boid == dt.Rows[j]["cud_boid"].ToString() && cud_Exchnge == dt.Rows[j]["cud_Exchnge"].ToString() && cud_UCC == dt.Rows[j]["cud_ucc"].ToString() && cud_cmid == dt.Rows[j]["cud_cmid"].ToString() && cud_tmid == dt.Rows[j]["cud_tmid"].ToString())
                                {
                                    DataRow row = dtNew.Select("ClientID='" + dt.Rows[j]["cud_boid"].ToString() + "' AND Exchange='" + dt.Rows[j]["cud_Exchnge"].ToString() + "' AND UCC ='" + dt.Rows[j]["cud_ucc"].ToString() + "' AND CMID='" + dt.Rows[j]["cud_cmid"].ToString() + "' AND TMID='" + dt.Rows[j]["cud_tmid"].ToString() + "'").LastOrDefault();
                                    if (row != null)
                                    {
                                        if (row[4 + Convert.ToInt32(dt.Rows[j]["cud_segment"])].ToString() != "")
                                        {
                                            DataRow dr;
                                            dr = dtNew.NewRow();
                                            dr["ClientID"] = dt.Rows[j]["cud_boid"].ToString();
                                            dr["ClientName"] = dt.Rows[j]["cm_name"].ToString();
                                            dr["Exchange"] = dt.Rows[j]["cud_Exchnge"].ToString();
                                            dr["UCC"] = dt.Rows[j]["cud_ucc"].ToString();
                                            dr["CMID"] = dt.Rows[j]["cud_cmid"].ToString();
                                            dr["TMID"] = dt.Rows[j]["cud_tmid"].ToString();
                                            dr[4 + Convert.ToInt32(dt.Rows[j]["cud_segment"])] = dt.Rows[j]["cud_ucc"].ToString();
                                            dtNew.Rows.Add(dr);
                                        }
                                        else
                                        {
                                            row[4 + Convert.ToInt32(dt.Rows[j]["cud_segment"])] = dt.Rows[j]["cud_ucc"].ToString();
                                        }

                                    }
                                }
                                else
                                {
                                    DataRow dr;
                                    dr = dtNew.NewRow();
                                    dr["ClientID"] = dt.Rows[j]["cud_boid"].ToString();
                                    dr["ClientName"] = dt.Rows[j]["cm_name"].ToString();
                                    dr["Exchange"] = dt.Rows[j]["cud_Exchnge"].ToString();
                                    dr["UCC"] = dt.Rows[j]["cud_ucc"].ToString();
                                    dr["CMID"] = dt.Rows[j]["cud_cmid"].ToString();
                                    dr["TMID"] = dt.Rows[j]["cud_tmid"].ToString();

                                    dr[4 + Convert.ToInt32(dt.Rows[j]["cud_segment"])] = dt.Rows[j]["cud_ucc"].ToString();
                                    dtNew.Rows.Add(dr);
                                }
                                cud_boid = dt.Rows[j]["cud_boid"].ToString();
                                cud_Exchnge = dt.Rows[j]["cud_Exchnge"].ToString();
                                cud_UCC = dt.Rows[j]["cud_ucc"].ToString();
                                cud_cmid = dt.Rows[j]["cud_cmid"].ToString();
                                cud_tmid = dt.Rows[j]["cud_tmid"].ToString();
                            }

                            for (int j = 0; j <= dtNew.Rows.Count - 1; j++)
                            {
                                if (dtNew.Rows[j][6].ToString().Trim() != "")
                                {
                                    dtNew.Rows[j]["Cash"] = "Yes";
                                }
                                else
                                {
                                    dtNew.Rows[j]["Cash"] = "";
                                }
                                if (dtNew.Rows[j][7].ToString().Trim() != "")
                                {
                                    dtNew.Rows[j]["F&O"] = "Yes";
                                }
                                else
                                {
                                    dtNew.Rows[j]["F&O"] = "";
                                }
                                if (dtNew.Rows[j][8].ToString().Trim() != "")
                                {
                                    dtNew.Rows[j]["Currency"] = "Yes";
                                }
                                else
                                {
                                    dtNew.Rows[j]["Currency"] = "";
                                }
                                if (dtNew.Rows[j][9].ToString().Trim() != "")
                                {
                                    dtNew.Rows[j]["SLB"] = "Yes";
                                }
                                else
                                {
                                    dtNew.Rows[j]["SLB"] = "";
                                }
                                if (dtNew.Rows[j][10].ToString().Trim() != "")
                                {
                                    dtNew.Rows[j]["Commodity"] = "Yes";
                                }
                                else
                                {
                                    dtNew.Rows[j]["Commodity"] = "";
                                }
                                if (dtNew.Rows[j][11].ToString().Trim() != "")
                                {
                                    dtNew.Rows[j]["Debt"] = "Yes";
                                }
                                else
                                {
                                    dtNew.Rows[j]["Debt"] = "";
                                }
                            }
                            if (dtNew.Rows.Count > 0)
                            {
                                ObjListing.UCCMapping = dtNew;
                            }
                            else
                            {
                                ObjListing.UCCMapping = "Record Not Found";
                            }
                        }
                        else
                        {
                            ObjListing.UCCMapping = "Record Not Found";
                        }
                        i++;
                        SQLTrans.Commit();
                        ListClientListing.Add(ObjListing);
                    }
                }
                return ListClientListing;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic TransactionStatus(TransactionStatusReq req, string LoginAccess)
        {
            try
            {
                string InternalRefNo, ISIN, InstCd, strClientWhere = "";
                DataTable Dt, Dt2;

                if (req.Filter.Client != null)
                {
                    if (req.Filter.Client.All(y => y != ""))
                    {
                        strClientWhere += " or cm_cd in('" + Strings.Join(req.Filter.Client.ToArray(), "','") + "')";
                    }
                }
                if (req.Filter.Branch != null)
                {
                    if (req.Filter.Branch.All(y => y != ""))
                    {
                        strClientWhere += " or cm_brboffcode in('" + Strings.Join(req.Filter.Branch.ToArray(), "','") + "')";
                    }
                }
                if (req.Filter.Family != null)
                {
                    if (req.Filter.Family.All(y => y != ""))
                    {
                        strClientWhere += " or cm_familycd in('" + Strings.Join(req.Filter.Family.ToArray(), "','") + "')";
                    }
                }
                if (req.Filter.Group != null)
                {
                    if (req.Filter.Group.All(y => y != ""))
                    {
                        strClientWhere += " or cm_groupcd in('" + Strings.Join(req.Filter.Group.ToArray(), "','") + "')";
                    }
                }
                if (strClientWhere.Length > 0)
                {
                    strClientWhere = " and (" + strClientWhere.Substring(3) + ") ";
                }

                if (req.DateType != "")
                {
                    if (req.DateType != "1" && req.DateType != "0")
                    {
                        return "Invalid Date Type";
                    }
                }
                else
                {
                    return "Date Type Cannot Be Null/Blank";
                }
                if (req.FromDate == "")
                {
                    return "From Date Cannot Be Null/Blank";
                }
                if (req.ToDate == "")
                {
                    return "To Date Cannot Be Null/Blank";
                }

                DateTime frmDt, toDt;
                frmDt = DateTime.ParseExact(req.FromDate, "yyyyMMdd", null);
                toDt = DateTime.ParseExact(req.ToDate, "yyyyMMdd", null);

                if (frmDt > toDt)
                {
                    return "From Date Cannot Be Greater Than To Date";
                }

                if (req.Type != "")
                {
                    if (req.Type != "925" && req.Type != "901" && req.Type != "903" && req.Type != "904" && req.Type != "906")
                    {
                        return "Invalid Transaction Type";
                    }
                }
                else
                {
                    return "Transaction Type Cannot Be Null/Blank";
                }

                if (req.Status != "")
                {
                    strsql = "select * from Statusof_trx where sx_code = '" + req.Status + "'";
                    Dt = objUtility.OpenDataTable(strsql);
                    if (Dt.Rows.Count == 0)
                    {
                        return "Invalid Status Code";
                    }
                }
                else
                {
                    return "Status Code Cannot Be Null/Blank";
                }

                if (strClientWhere != "")
                {
                    strsql = "select * from Client_master where " + strClientWhere.Substring(5);
                    Dt = objUtility.OpenDataTable(strsql);
                    if (Dt.Rows.Count == 0)
                    {
                        return "Invalid ID";
                    }
                }

                if (req.Type == "925")
                {
                    strsql = "select id_internalrefno as 'Slip No', id_trxdate 'Transection Date', ";
                    strsql += " id_clientid 'BO ID',cm_name 'BO Name',id_isin as 'ISIN',sc_isinname as 'Company Name',id_qty as 'Qty',sx_description as 'Status', ";
                    strsql += " id_exec_date 'Execution Date',Count(0) 'No Of Transection',id_instcd 'InstCd' ";
                    strsql += " from  Interdepository with (nolock), Security with (nolock),Statusof_trx with (nolock),client_master  with (nolock)";
                    strsql += " where  id_isin = sc_isincode and sx_code = id_status  and sx_trxtype = id_trxtype " + strClientWhere + " and id_clientid  = cm_cd ";
                    if (req.Status != "13")
                    {
                        strsql += " and sx_code = '" + req.Status + "'";
                    }
                    if (req.DateType == "0")
                    {
                        strsql += " and id_trxdate  between '" + req.FromDate + "'  and '" + req.ToDate + "' ";
                    }
                    else
                    {
                        strsql += " and id_exec_date  between '" + req.FromDate + "'  and '" + req.ToDate + "' ";
                    }
                    strsql += LoginAccess;
                    strsql += " Group By id_internalrefno , id_clientid ,  cm_name ,  id_trxdate , id_exec_date , id_instcd , id_trxtype , id_isin , sc_isinname , id_qty , sx_description";
                }
                else if (req.Type == "901")
                {
                    strsql = " select dm_irn as 'Slip No', dm_dmat_date 'Transection Date',dm_client_id 'BO ID',cm_name 'BO Name', ";
                    strsql += " dm_isin_code 'ISIN',sc_company_name 'Company Name',dm_dematqty 'Qty',sx_description 'Status', ";
                    strsql += " dm_dmat_date 'Execution Date', sum(dm_total_certificate) 'No Of Transection', dm_instcd 'InstCd' ";
                    strsql += " from  DematMaster, Security, Statusof_trx, client_master  ";
                    strsql += " where dm_isin_code = sc_isincode and dm_status = sx_code and sx_trxtype = '901' " + strClientWhere + " and dm_client_id  = cm_cd ";
                    if (req.Status != "13")
                    {
                        strsql += " and sx_code = '" + req.Status + "'";
                    }

                    if (req.DateType == "0")
                    {
                        strsql += " and dm_dmat_date  between '" + req.FromDate + "'  and '" + req.ToDate + "' ";
                    }
                    else
                    {
                        strsql += " and dm_dmat_date  between '" + req.FromDate + "'  and '" + req.ToDate + "' ";
                    }
                    strsql += LoginAccess;
                    strsql += " Group By dm_irn , dm_client_id ,  cm_name ,  dm_dmat_date , dm_instcd ,dm_isin_code,sc_company_name,dm_dematqty,sx_description";
                }
                else if (req.Type == "906" || req.Type == "904" || req.Type == "903")
                {
                    strsql = " select tb_internal_refno 'Slip No',";
                    strsql += " tb_trx_date 'Transection Date', ";
                    strsql += " tb_client_id 'BO ID',cm_name 'BO Name',tb_isin 'ISIN',sc_isinname 'Company Name',tb_qty 'Qty', ";
                    strsql += " sx_description 'Status', tb_exec_date 'Execution Date',Count(0) 'No Of Transection',tb_instcd 'InstCd' ";
                    strsql += " from  TrxBackoffice with (nolock), Security with (nolock),Statusof_trx with (nolock),client_master with (nolock)";
                    strsql += " where  tb_isin = sc_isincode and sx_code = tb_status and sx_trxtype = tb_trx_type " + strClientWhere + " and tb_client_id  = cm_cd  ";
                    if (req.Status != "13")
                    {
                        strsql += " and sx_code = '" + req.Status + "'";
                    }

                    if (req.DateType == "0")
                    {
                        strsql += " and tb_trx_date  between '" + req.FromDate + "'  and '" + req.ToDate + "' and ";
                    }
                    else
                    {
                        strsql += " and tb_exec_date  between '" + req.FromDate + "'  and '" + req.ToDate + "' and ";
                    }
                    strsql += "tb_trx_type = '" + req.Type + "' ";
                    strsql += LoginAccess;
                    strsql += " Group By tb_internal_refno , tb_client_id ,  cm_name ,  tb_trx_date , tb_exec_date , tb_trx_type , tb_instcd , tb_isin , sc_isinname , tb_qty , sx_description";
                }

                Dt = objUtility.OpenDataTable(strsql);
                if (Dt.Rows.Count == 0)
                {
                    return "Record Not Found";
                }

                List<TransactionStatusResponse> ListTrxStatus = new List<TransactionStatusResponse>();

                for (int i = 0; i < Dt.Rows.Count; i++)
                {
                    TransactionStatusResponse ObjTran = new TransactionStatusResponse();
                    ObjTran.SlipNo = Dt.Rows[i]["Slip No"].ToString().Trim();
                    ObjTran.TransactionDate = Dt.Rows[i]["Transection Date"].ToString().Trim();
                    ObjTran.ClientID = Dt.Rows[i]["BO ID"].ToString().Trim();
                    ObjTran.ClientName = Dt.Rows[i]["BO Name"].ToString().Trim();
                    ObjTran.ISIN = Dt.Rows[i]["ISIN"].ToString().Trim();
                    ObjTran.CompanyName = Dt.Rows[i]["Company Name"].ToString().Trim();
                    ObjTran.Qty = Convert.ToDouble(Dt.Rows[i]["Qty"].ToString().Trim());
                    ObjTran.Status = Dt.Rows[i]["Status"].ToString().Trim();
                    ObjTran.ExecutionDate = Dt.Rows[i]["Execution Date"].ToString().Trim();
                    ObjTran.NoOfTransection = Dt.Rows[i]["No Of Transection"].ToString().Trim();

                    InternalRefNo = Dt.Rows[i]["Slip No"].ToString().Trim();
                    ISIN = Dt.Rows[i]["ISIN"].ToString().Trim();
                    InstCd = Dt.Rows[i]["InstCd"].ToString().Trim();

                    if (req.Type == "925")
                    {
                        strsql = "select id_otherclientid 'CM',id_otherdpid 'DPID',id_settlementno 'Settlement',id_reference 'Reference'," +
                            "Case isNull( id_recoslipyn,'')when 'Y'then 'Yes' when 'N'then 'No' else ''end as 'Recouncilation'," +
                            "Case isNull(id_instreceivemode,'')when 'S'then 'Slip' when 'F'then 'Fax' when 'T'then 'Telephone' when 'V'then 'Verbal' when 'E'then 'Email' when 'P'then 'POA' else ''end as 'RecMode'," +
                            "Case isNull(id_entrymode,'')when 'W'then 'Web' when 'E'then 'Exe' when 'D'then ' Direct Client' when 'T'then 'Tradeplus File' when 'C'then 'CDSL File' when 'B'then 'Branch File' else ''end as 'EntryMode'," +
                            "Interdepository.mkrid as 'MakerID', Interdepository.mkrdt as 'MakerDate', id_authuserid1 as 'CheckerID', id_authdate1 as 'CheckerDate'," +
                            "id_batch_no as 'BatchNo',isnull((select rt_desc from Reasonfortrade where rt_code = id_reasfortrade), '') as 'Reason',isnull(id_consideration, 0) as 'Consideration',id_authremarks as 'AutheriseRemark',isnull(id_remark, '') as 'Remark' " +
                            "from Interdepository with (nolock), Security with (nolock),Statusof_trx with (nolock),client_master with (nolock) " +
                            "where id_isin = sc_isincode and sx_code = id_status and sx_trxtype = id_trxtype and id_clientid  = cm_cd and id_trxtype = '" + req.Type + "' and id_instcd = '" + InstCd + "' and id_internalrefno = '" + InternalRefNo + "' and id_isin = '" + ISIN + "'";
                    }
                    else if (req.Type == "901")
                    {
                        strsql = "select '' DRN, Case isNull(dm_instreceivemode,'') when 'S'then 'Slip' when 'F'then 'Fax' when 'T'then 'Telephone' when 'V'then 'Verbal' when 'E'then 'Email' when 'P'then 'POA' else ''end 'RecMode'," +
                            "Case isnull(dm_entrymode,'') when 'W'then 'Web' when 'E'then 'Exe' when 'D'then ' Direct Client' when 'T'then 'Tradeplus File' when 'C'then 'CDSL File' when 'B'then 'Branch File' else ''end 'EntryMode'," +
                            "Case isnull(dm_recoslipyn,'') when 'Y'then 'Yes' when 'N'then 'No' else ''end 'Recouncilation',dm_batch_no 'BatchNo',DematMaster.mkrid as 'MakerID', DematMaster.mkrdt 'MakerDate'," +
                            "isnull(dm_authuserid1, '') as 'CheckerID', dm_authdate1 as 'CheckerDate',dm_authremarks 'AutheriseRemark' " +
                            "from DematMaster with (nolock), Security with (nolock),Statusof_trx with (nolock), client_master with (nolock) " +
                            "where dm_isin_code = sc_isincode and sx_code = dm_status and sx_trxtype = '" + req.Type + "' and dm_client_id  = cm_cd and dm_instcd = '" + InstCd + "' and dm_irn = '" + InternalRefNo + "' and dm_isin_code = '" + ISIN + "'";
                    }
                    else if (req.Type == "906" || req.Type == "903")
                    {
                        strsql = "select tb_other_cmbp_id 'CM',tb_settlement 'Counter Settlement',tb_reference 'Reference No'," +
                            "Case isNull( tb_recoslipyn,'')when 'Y'then 'Yes' when 'N'then 'No' else ''end as 'Recouncilation'," +
                            "Case isNull(tb_instreceivemode,'')when 'S'then 'Slip' when 'F'then 'Fax' when 'T'then 'Telephone' when 'V'then 'Verbal' when 'E'then 'Email' when 'P'then 'POA' else ''end as 'RecMode'," +
                            "Case isNull(tb_entrymode,'')when 'W'then 'Web' when 'E'then 'Exe' when 'D'then ' Direct Client' when 'T'then 'Tradeplus File' when 'C'then 'CDSL File' when 'B'then 'Branch File' else ''end as 'EntryMode', " +
                            "TrxBackOffice.mkrid as 'MakerID', TrxBackOffice.mkrdt as 'MakerDate', tb_authuserid1 as 'CheckerID', tb_authdt1 as 'CheckerDate',tb_batch_no as 'BatchNo',tb_authremarks 'Autherise Remark' " +
                            "from TrxBackOffice with (nolock), Security with (nolock),Statusof_trx with (nolock),client_master with (nolock) " +
                            "where tb_isin = sc_isincode and sx_code = tb_status and sx_trxtype = tb_trx_type and tb_client_id  = cm_cd   and tb_trx_type = '" + req.Type + "' and tb_instcd = '" + InstCd + "' and tb_internal_refno = '" + InternalRefNo + "' and tb_isin = '" + ISIN + "'";
                    }
                    else if (req.Type == "904")
                    {
                        strsql = "select tb_other_client_id 'CM',isnull(tb_settlement,'') 'Counter Settlement',tb_reference 'Reference No'," +
                            "Case isNull( tb_recoslipyn,'')when 'Y'then 'Yes' when 'N'then 'No' else ''end as 'Recouncilation'," +
                            "Case isNull(tb_instreceivemode,'')when 'S'then 'Slip' when 'F'then 'Fax' when 'T'then 'Telephone' when 'V'then 'Verbal' when 'E'then 'Email' when 'P'then 'POA' else ''end as 'RecMode'," +
                            "Case isNull(tb_entrymode,'')when 'W'then 'Web' when 'E'then 'Exe' when 'D'then ' Direct Client' when 'T'then 'Tradeplus File' when 'C'then 'CDSL File' when 'B'then 'Branch File' else ''end as 'EntryMode', " +
                            "TrxBackOffice.mkrid as 'MakerID', TrxBackOffice.mkrdt as 'MakerDate', tb_authuserid1 as 'CheckerID', tb_authdt1 as 'CheckerDate',tb_batch_no as 'BatchNo'," +
                            "tb_authremarks 'Autherise Remark',isnull((select rt_desc from Reasonfortrade where rt_code = tb_reasfortrade), '') as 'Reason',isnull(tb_remark, '') as 'Remark' ,isnull(tb_consideration, 0) as 'Consideration'  " +
                            "from TrxBackOffice with (nolock), Security with (nolock),Statusof_trx with (nolock),client_master with (nolock) " +
                            "where tb_isin = sc_isincode and sx_code = tb_status and sx_trxtype = tb_trx_type and tb_client_id  = cm_cd   and tb_trx_type = '" + req.Type + "' and tb_instcd = '" + InstCd + "' and tb_internal_refno = '" + InternalRefNo + "' and tb_isin = '" + ISIN + "'";
                    }
                    Dt2 = objUtility.OpenDataTable(strsql);

                    if (Dt2.Rows.Count == 0)
                    {
                        ObjTran.SlipData = "Record Not Found";
                    }
                    else
                    {
                        ObjTran.SlipData = Dt2;
                    }

                    ListTrxStatus.Add(ObjTran);
                }
                return ListTrxStatus;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic BillBreakUp(BillBreakUpReq req, string LoginAccess)
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
                if (req.Filter.Family != null)
                {
                    if (req.Filter.Family.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Family.ToArray(), "##"));
                        strClientWhere += " or cm_familycd in('" + fltr.Replace("##", "','") + "')";
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
                if (strClientWhere.Length > 0)
                {
                    strClientWhere = " and (" + strClientWhere.Substring(3) + ") ";
                }

                DateTime FromDt, ToDt;
                if (req.FromDate == "")
                {
                    return "From Date Cannot Be Blank";
                }
                if (req.ToDate == "")
                {
                    return "To Date Cannot Be Blank";
                }
                FromDt = DateTime.ParseExact(req.FromDate, "yyyyMMdd", null);
                ToDt = DateTime.ParseExact(req.ToDate, "yyyyMMdd", null);
                if (FromDt > ToDt)
                {
                    return "From Date Cannot Be Greater Than To Date";
                }
                if (strClientWhere != null)
                {
                    strsql = "select * from Client_master where cm_active = '01' " + strClientWhere;
                    DataTable Dt = objUtility.OpenDataTable(strsql);
                    if (Dt.Rows.Count == 0)
                    {
                        return "Invalid ID";
                    }
                }
                else
                {
                    return "ID Cannot Be Blank";
                }

                SqlTransaction ObjTrans;
                string Connection = objUtility.GetConnectionStr();
                using (SqlConnection con = new SqlConnection(Connection))
                {
                    con.Open();
                    ObjTrans = con.BeginTransaction();
                    SqlDataAdapter sqlDtAdaptr = new SqlDataAdapter();
                    SqlCommandBuilder sqlCmdBld = new SqlCommandBuilder(sqlDtAdaptr);

                    string Charges_OUR = string.Empty;
                    string Charges_CDSL = string.Empty;
                    string strString = string.Empty;
                    string strChHeader = string.Empty;
                    string strChColmn = string.Empty;
                    string strCDSL = string.Empty;
                    string strCDSLHeader = string.Empty;
                    string strCDSLColmn = string.Empty;
                    string ColumnTextAlign = string.Empty;
                    string ColumnTextLength = string.Empty;
                    string CDSLTxtAlign = string.Empty;
                    string CDSLTxtLength = string.Empty;
                    string strFrmDt = req.FromDate;
                    string strToDt = req.ToDate;

                    prCreateTable(con, ObjTrans);

                    strsql = "insert into #TempBilling (bl_client_id,bl_amount,bl_charge_code) ";
                    strsql = strsql + " select bl_client_id, sum(isnull(bl_amount,0)) as bl_amount,'XX' ";
                    strsql = strsql + " from Billing with (nolock) ";
                    strsql = strsql + " ,Client_master where bl_bill_dt between '" + strFrmDt + "' and '" + strToDt + "' and cm_cd= bl_client_id ";
                    strsql = strsql + " and cm_schedule = '" + GetSysParm("CMSCHEDULE").Trim() + "' " + LoginAccess; // + loginAccess
                    if (strClientWhere != "")
                    {
                        strsql += " " + strClientWhere;
                    }
                    strsql = strsql + " group by bl_client_id ";
                    objUtility.ExecuteSQL(strsql, con, ObjTrans);

                    strsql = "select td_ac_code, cg_schedule , sum(isNull(td_amount,0)+isNull(td_cdslcharge,0)) as td_ourbill , ";
                    strsql = strsql + "sum(isNull(td_actualcdslcharge,0)) as td_cdslbill ";
                    strsql = strsql + "from trxdetail with (nolock) , Chargesmaster  with (nolock)";
                    strsql = strsql + " ,Client_master where cast(cg_code as numeric) = td_charge_code and cm_cd= td_ac_code and td_curdate between '" + strFrmDt + "' and '" + strToDt + "'";
                    strsql = strsql + " and cm_schedule = '" + GetSysParm("CMSCHEDULE").Trim() + "' " + LoginAccess; // + loginAccess
                    if (strClientWhere != "")
                    {
                        strsql += " " + strClientWhere;
                    }
                    strsql = strsql + " group by td_ac_code , cg_schedule ";

                    objUtility.ExecuteSQL("insert into #TempBilling(bl_client_id,bl_charge_code,bl_ourbill,bl_cdslbill) " + strsql, con, ObjTrans);


                    strsql = "select oc_clientid , oc_chargecode , sum(isNull(oc_amt,0)) as oc_amt , Sum(case oc_chargecode When '04' then 0 else oc_actualcdslcharge end ) ";
                    strsql = strsql + "from Other_charges with (nolock) ";
                    strsql = strsql + " ,Client_master where cm_cd= oc_clientid and oc_date between '" + strFrmDt + "' and '" + strToDt + "'";
                    if (strClientWhere != "")
                    {
                        strsql += " " + strClientWhere;
                    }
                    strsql = strsql + "group by oc_clientid , oc_chargecode ";
                    objUtility.ExecuteSQL("insert into #TempBilling(bl_client_id,bl_charge_code,bl_ourbill,bl_cdslbill) " + strsql, con, ObjTrans);

                    strsql = "select oc_clientid , oc_chargecode , 0 as oc_amt , Sum(oc_amt) ";
                    strsql = strsql + "from Other_CDSL with (nolock) ";
                    strsql = strsql + " ,Client_master where cm_cd= oc_clientid and oc_dtto between '" + strFrmDt + "' and '" + strToDt + "'";
                    if (strClientWhere != "")
                    {
                        strsql += " " + strClientWhere;
                    }
                    strsql = strsql + "group by oc_clientid , oc_chargecode ";
                    objUtility.ExecuteSQL("insert into #TempBilling(bl_client_id,bl_charge_code,bl_ourbill,bl_cdslbill) " + strsql, con, ObjTrans);

                    DateTime FrDate = DateTime.ParseExact(req.FromDate, "yyyyMMdd", null);
                    if (Convert.ToDateTime(FrDate) >= Convert.ToDateTime(objUtility.GetSlashDate("20100401")))
                    {
                        strsql = "select ad_cd , am_name, ad_amount from Additionalscheme_detail  a  with (nolock), Additionalscheme_master";
                        strsql = strsql + " where am_cd = ad_cd and  ad_cd+ad_effdt = (select max(ad_cd+ad_effdt)  from Additionalscheme_detail b with (nolock)";
                        strsql = strsql + " where a.ad_cd = b.ad_cd and b.ad_name = 'FALL' and b.ad_effdt <= '" + strToDt + "')";
                        strsql = strsql + " and ad_name = 'FALL' and ad_cd in ('99','98','97') ";
                        strsql = strsql + " and ad_cd not in (select ad_cd from Additionalscheme_detail Where ad_effdt <= '" + strToDt + "' and ad_name = 'FDISC')";

                        DataSet DsSearch = new DataSet();
                        DsSearch = objUtility.OpenDataSet(sqlDtAdaptr, strsql, con, ObjTrans);
                        if (DsSearch.Tables[0].Rows.Count > 0)
                        {
                            int i = 0;
                            do
                            {
                                strsql = "insert into #TempBilling(bl_client_id,bl_charge_code,bl_ourbill,bl_cdslbill) ";
                                strsql = strsql + " select bl_client_id , '" + DsSearch.Tables[0].Rows[i]["ad_cd"].ToString().Trim() + "', 0 as oc_amt , ";
                                strsql = strsql + " Round( Sum(bl_cdslbill) * " + Convert.ToDouble(DsSearch.Tables[0].Rows[i]["ad_amount"].ToString().Trim()) + "/100 , 2 ) ";
                                strsql = strsql + " From #TempBilling ";
                                strsql = strsql + " group by bl_client_id ";
                                strsql = strsql + " Having Round( Sum(bl_cdslbill) * " + Convert.ToDouble(DsSearch.Tables[0].Rows[i]["ad_amount"].ToString().Trim()) + "/100 , 2 ) > 0 ";
                                objUtility.ExecuteSQL(strsql, con, ObjTrans);
                                i++;
                            } while (DsSearch.Tables[0].Rows.Count > i);
                        }
                    }

                    strsql = "select  '' cg_code,cg_schedule, cm_name as Na, 1 type  from Chargesmaster with (nolock), Client_master with (nolock) where cg_schedule= cm_cd  and cg_code not in ('01','04','06','08','09','41','42','43','44','45','00','99','97','95','98','50')";
                    strsql = strsql + " group by cg_schedule, cm_name Union";
                    strsql = strsql + " select  cg_code,cg_schedule, cg_name as Na, 2 type  from Chargesmaster with (nolock) where cg_code in ('01','04','06','08','09','41','42','43','44','45','00','99','97','95','98','50','62')";
                    strsql = strsql + " order by type,Na";
                    DataSet Dscharges = new DataSet();
                    Dscharges = objUtility.OpenDataSet(sqlDtAdaptr, strsql, con, ObjTrans);
                    if (Dscharges.Tables[0].Rows.Count > 0)
                    {
                        for (int ch = 0; ch <= Dscharges.Tables[0].Rows.Count - 1; ch++)
                        {
                            string strCg_Code = Dscharges.Tables[0].Rows[ch]["cg_code"].ToString().Trim();
                            if (strCg_Code == "00" || strCg_Code == "05" || strCg_Code == "06" || strCg_Code == "41" || strCg_Code == "42" || strCg_Code == "50" || strCg_Code == "96" || strCg_Code == "95" || strCg_Code == "43" || strCg_Code == "09" || strCg_Code == "08" || strCg_Code == "01")
                            {
                                Charges_OUR = Charges_OUR + Dscharges.Tables[0].Rows[ch]["Na"].ToString().Trim().Replace("-", " ") + "|OUR" + " |" + Dscharges.Tables[0].Rows[ch]["cg_schedule"].ToString().Trim().ToUpper() + "|" + Dscharges.Tables[0].Rows[ch]["type"].ToString().Trim() + "-";
                            }
                            else
                            {
                                Charges_OUR = Charges_OUR + Dscharges.Tables[0].Rows[ch]["Na"].ToString().Trim().Replace("-", " ") + "|OUR" + " |" + Dscharges.Tables[0].Rows[ch]["cg_schedule"].ToString().Trim().ToUpper() + "|" + Dscharges.Tables[0].Rows[ch]["type"].ToString().Trim() + "-";
                                Charges_CDSL = Charges_CDSL + Dscharges.Tables[0].Rows[ch]["Na"].ToString().Trim().Replace("-", " ") + "|CDSL" + " |" + Dscharges.Tables[0].Rows[ch]["cg_schedule"].ToString().Trim().ToUpper() + "|" + Dscharges.Tables[0].Rows[ch]["type"].ToString().Trim() + "-";
                            }
                        }
                        string[] arr_Charges = (Charges_CDSL + Charges_OUR).Split('-');

                        for (int icount = 0; icount < arr_Charges.Length - 1; icount++)
                        {
                            string[] arrtemp = arr_Charges[icount].Split('|');
                            if (arrtemp[3].Trim() == "1")
                            {
                                objUtility.ExecuteSQL(" insert into #TmpSelect values('" + arrtemp[2].Trim().ToUpper() + "','" + arrtemp[1].Trim() + "', '" + arrtemp[0].Trim() + "')", con, ObjTrans);
                            }
                            else
                            {
                                objUtility.ExecuteSQL(" insert into #TmpSelect values('" + objUtility.fnFireQueryCross("chargesmaster", "cg_code", "cg_name", arrtemp[0].Trim().Trim(), true).Trim() + "','" + arrtemp[1].Trim() + "', '" + arrtemp[0].Trim() + "')", con, ObjTrans);
                            }
                        }

                        strsql = " select tmp_charge_code,tmp_our_cdsl,tmp_name from #TmpSelect,#TempBilling ";
                        strsql = strsql + " where bl_charge_code= tmp_charge_code ";
                        strsql = strsql + " and ( bl_ourbill <> 0 or bl_cdslbill <> 0) ";
                        strsql = strsql + " group by tmp_charge_code,tmp_our_cdsl,tmp_name ";
                        strsql = strsql + " order by case when tmp_charge_code ='99' then 3 else case when tmp_charge_code ='98' then 2 else";
                        strsql = strsql + " case when tmp_charge_code ='97' then 1 else 0 end end end";

                        Dscharges.Tables[0].Clear();
                        Dscharges = objUtility.OpenDataSet(sqlDtAdaptr, strsql, con, ObjTrans);
                        if (Dscharges.Tables[0].Rows.Count > 0)
                        {
                            for (int ch = 0; ch <= Dscharges.Tables[0].Rows.Count - 1; ch++)
                            {
                                if (Dscharges.Tables[0].Rows[ch]["tmp_our_cdsl"].ToString().Trim().ToUpper() == "OUR")
                                {
                                    strChColmn = strChColmn + ",OUR_" + Dscharges.Tables[0].Rows[ch]["tmp_charge_code"].ToString().Trim().ToUpper();
                                    strChHeader = strChHeader + ", " + (Dscharges.Tables[0].Rows[ch]["tmp_name"].ToString().Trim().Replace("-", " "));
                                    strString = strString + ", cast((Sum(Case bl_charge_code When '" + Dscharges.Tables[0].Rows[ch]["tmp_charge_code"].ToString().Trim().ToUpper() + "' Then bl_ourbill else 0 end))as decimal(15,2)) '" + Dscharges.Tables[0].Rows[ch]["tmp_name"].ToString().Trim().ToUpper() + "' ";
                                    ColumnTextAlign = ColumnTextAlign + ",R";
                                    ColumnTextLength = ColumnTextLength + ",10";
                                }
                                else
                                {
                                    strCDSLColmn = ",CDSLCHARGES";
                                    strCDSLHeader = ",CDSL Charges";
                                    strCDSL = strCDSL + "+ cast((Sum(Case bl_charge_code When '" + Dscharges.Tables[0].Rows[ch]["tmp_charge_code"].ToString().Trim().ToUpper() + "' Then bl_cdslbill else 0 end))as decimal(15,2)) ";
                                    CDSLTxtAlign = ",R";
                                    CDSLTxtLength = "," + (Dscharges.Tables[0].Rows[ch]["tmp_name"].ToString().Trim().Replace("-", " ")).Length;
                                }
                            }

                            strChColmn = strChColmn + strCDSLColmn;
                            strChHeader = strChHeader + strCDSLHeader;
                            strCDSL = (strCDSL == "" ? ",0" : ",(" + (strCDSL.Remove(0, 2)) + ")") + " CDSLCHARGES ";
                            strString = strString + strCDSL;
                            ColumnTextAlign = ColumnTextAlign + CDSLTxtAlign;
                            ColumnTextLength = ColumnTextLength + CDSLTxtLength;

                            strChColmn = strChColmn + ",SGST";
                            strChHeader = strChHeader + ",SGST";
                            strString = strString + ",cast(Sum(Case bl_charge_code When 'SG' Then bl_ourbill else 0 end)as decimal(15,2)) SGST ";
                            ColumnTextAlign = ColumnTextAlign + ",R";
                            ColumnTextLength = ColumnTextLength + ",10";

                            strChColmn = strChColmn + ",UGST";
                            strChHeader = strChHeader + ",UGST";
                            strString = strString + ", cast(Sum(Case bl_charge_code When 'UG' Then bl_ourbill else 0 end)as decimal(15,2)) UGST ";
                            ColumnTextAlign = ColumnTextAlign + ",R";
                            ColumnTextLength = ColumnTextLength + ",10";

                            strChColmn = strChColmn + ",IGST";
                            strChHeader = strChHeader + ",IGST";
                            strString = strString + ", cast(Sum(Case bl_charge_code When 'IG' Then bl_ourbill else 0 end)as decimal(15,2)) IGST ";
                            ColumnTextAlign = ColumnTextAlign + ",R";
                            ColumnTextLength = ColumnTextLength + ",10";

                            strChColmn = strChColmn + ",CGST";
                            strChHeader = strChHeader + ",CGST";
                            strString = strString + ", cast(Sum(Case bl_charge_code When 'CG' Then bl_ourbill else 0 end)as decimal(15,2)) CGST ";
                            ColumnTextAlign = ColumnTextAlign + ",R";
                            ColumnTextLength = ColumnTextLength + ",10";
                        }
                    }

                    strsql = "select bl_client_id 'ClientID',cm_name 'ClientName',isnull(cast((sum(bl_amount))as decimal(15,2)), 0) 'BillAmount' ";
                    strsql += strString.Trim() == "" ? "" : strString;
                    strsql = strsql + " from #TempBilling ,Client_master,Branch_master";
                    strsql = strsql + " where cm_cd = bl_client_id and cm_brboffcode=bm_branchcd ";
                    strsql = strsql + " Group By bl_client_id,cm_name,cm_blsavingcd,cm_brboffcode,bm_branchname ,cm_groupcd,cm_chgsscheme,cm_opendate,cm_acc_closuredate";
                    strsql = strsql + " Order by bl_client_id";
                    DataSet DsPrint = new DataSet();
                    DsPrint = objUtility.OpenDataSet(sqlDtAdaptr, strsql, con, ObjTrans);


                    //----------------------------------- This Logic for Grand Total ---------------------------------------
                    //DataRow drow;
                    //DataTable table = DsPrint.Tables[0];
                    //drow = table.NewRow();

                    //foreach(DataColumn col in DsPrint.Tables[0].Columns)
                    //{
                    //    if(col.ColumnName == "BO_ID")
                    //    {
                    //        drow["BO_ID"] = "Grand Total";
                    //    }
                    //    else if(col.ColumnName == "BO_NAME")
                    //    {
                    //        drow["BO_NAME"] = "";
                    //    }
                    //    else if(col.ColumnName == "BILL_AMOUNT")
                    //    {
                    //        object sum = Convert.ToDecimal(table.Compute("sum(" + col.ColumnName + ")", string.Empty));
                    //        drow["BILL_AMOUNT"] = sum;
                    //    }
                    //    else
                    //    {
                    //        object sum = Convert.ToDecimal(table.Compute("sum([" + col.ColumnName + "])", string.Empty));
                    //        drow[col.ColumnName] = sum;
                    //    }
                    //}

                    //table.Rows.Add(drow);

                    //return table;
                    //----------------------------------------------------------------------------------------------------------------


                    return DsPrint;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region Bill Break Up Functions
        private void prCreateTable(SqlConnection con, SqlTransaction Trans)
        {
            strsql = "Create table #TempBilling( bl_client_id char(16), bl_amount money , bl_charge_code char(8), bl_ourbill money , bl_cdslbill money)";
            objUtility.ExecuteSQL(strsql, con, Trans);

            strsql = "create index idx_1_1234 on #TempBilling ( bl_client_id )";
            objUtility.ExecuteSQL(strsql, con, Trans);

            strsql = "Create table #TmpSelect( tmp_charge_code char(8), tmp_our_cdsl char(5) , tmp_name varchar(50))";
            objUtility.ExecuteSQL(strsql, con, Trans);
        }
        #endregion

        public dynamic ClientOutstanding(ClientOutstandingReq req, string LoginAccess)
        {
            try
            {
                string strClientWhere = "";
                string strType = req.Type.Trim().ToUpper();

                if (req.Filter.Client != null)
                {
                    if (req.Filter.Client.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Client.ToArray(), "##"));
                        strClientWhere += " or cm_cd in ('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Branch != null)
                {
                    if (req.Filter.Branch.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Client.ToArray(), "##"));
                        strClientWhere += " or cm_brboffcode in ('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Family != null)
                {
                    if (req.Filter.Family.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Client.ToArray(), "##"));
                        strClientWhere += " or cm_familycd in ('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Group != null)
                {
                    if (req.Filter.Group.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Client.ToArray(), "##"));
                        strClientWhere += " or cm_groupcd in ('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (strClientWhere.Length > 0)
                {
                    strClientWhere = " and (" + strClientWhere.Substring(3) + ") ";
                }

                strsql = "select ld_clientcd 'BO_ID',cm_Name 'BO_Name',cm_blsavingcd 'BackOffice_CD',cast((sum(ld_amount)) as decimal (15,2)) 'Balance',cm_chgsscheme 'Scheme',bm_branchname 'Branch',cm_email 'Email'," +
                    "case gr_desc when 'Individual' then '' else gr_desc end 'Group',case fm_desc when 'Individual' then '' else fm_desc end 'Family',cm_tele1 'Telephone' " +
                    "From Ledger,Client_master,group_master,branch_master,family_master " +
                    "where ld_clientcd = cm_cd and cm_groupcd = gr_cd and cm_familycd = fm_cd and cm_brboffcode = bm_branchcd  and cm_schedule = '" + GetSysParm("CMSCHEDULE").Trim() + "' ";
                if (req.Status == "A")
                {
                    strsql += "and cm_active = '01' ";
                }
                else if (req.Status == "I")
                {
                    strsql += "and cm_active != '01' ";
                }
                strsql += strClientWhere + LoginAccess;
                strsql += "Group By ld_clientcd,cm_Name,cm_tele1,cm_email,cm_blsavingcd,cm_chgsscheme,bm_branchname,gr_desc,fm_desc  ";
                strsql += "Having sum(ld_amount)  <> 0  and  abs(sum(ld_amount)) between " + req.BalanceBetween + " and " + req.BalanceTo + " and  abs(sum(ld_amount)) >= 0 ";
                if (strType == "D")
                {
                    strsql += "and sum(ld_amount) > 0 ";
                }
                else if (strType == "C")
                {
                    strsql += "and sum(ld_amount) < 0 ";
                }
                strsql += "Order By cm_name";

                DataTable Dt = objUtility.OpenDataTable(strsql);

                if (Dt.Rows.Count == 0)
                {
                    return "Record Not Found";
                }

                List<ClientOutstandingResponse> ResList = new List<ClientOutstandingResponse>();
                List<TempClientOutstandingResponse> ListTemp = new List<TempClientOutstandingResponse>();
                for (int i = 0; i < Dt.Rows.Count; i++)
                {
                    TempClientOutstandingResponse ObjTemp = new TempClientOutstandingResponse()
                    {
                        ClientID = Dt.Rows[i]["BO_ID"].ToString().Trim(),
                        ClientName = Dt.Rows[i]["BO_Name"].ToString().Trim(),
                        BackOfficeCD = Dt.Rows[i]["BackOffice_CD"].ToString().Trim(),
                        Balance = Convert.ToDouble(Dt.Rows[i]["Balance"].ToString().Trim()),
                        Scheme = Dt.Rows[i]["Scheme"].ToString().Trim(),
                        Branch = Dt.Rows[i]["Branch"].ToString().Trim(),
                        Email = Dt.Rows[i]["Email"].ToString().Trim(),
                        Group = Dt.Rows[i]["Group"].ToString().Trim(),
                        Family = Dt.Rows[i]["Family"].ToString().Trim(),
                        Telephone = Dt.Rows[i]["Telephone"].ToString().Trim(),
                    };
                    ListTemp.Add(ObjTemp);
                }

                List<string> boids = ListTemp.Select(x => x.ClientID).ToList();
                List<string> names = ListTemp.Select(x => x.ClientName).ToList();
                List<string> backoffices = ListTemp.Select(x => x.BackOfficeCD).ToList();
                List<double> balances = ListTemp.Select(x => x.Balance).ToList();
                List<string> schemes = ListTemp.Select(x => x.Scheme).ToList();
                List<string> branches = ListTemp.Select(x => x.Branch).ToList();
                List<string> emails = ListTemp.Select(x => x.Email).ToList();
                List<string> groups = ListTemp.Select(x => x.Group).ToList();
                List<string> familys = ListTemp.Select(x => x.Family).ToList();
                List<string> telephones = ListTemp.Select(x => x.Telephone).ToList();

                int j = 0;
                string strStartDt = "";

                foreach (var code in boids)
                {
                    ClientOutstandingResponse ResObj = new ClientOutstandingResponse();
                    ResObj.ClientID = code;
                    ResObj.ClientName = names[j];
                    ResObj.BackOfficeCD = backoffices[j];
                    ResObj.Balance = balances[j];
                    ResObj.Scheme = schemes[j];
                    ResObj.Branch = branches[j];
                    ResObj.Email = emails[j];
                    ResObj.Group = groups[j];
                    ResObj.Family = familys[j];
                    ResObj.Telephone = telephones[j];

                    strStartDt = "0401";
                    if (System.DateTime.Today.Month < 6)
                    {
                        strStartDt = (System.DateTime.Today.Year - 1).ToString() + strStartDt;
                    }
                    else
                    {
                        strStartDt = System.DateTime.Today.Year.ToString() + strStartDt;
                    }

                    strsql = "select cm_name +' [ ' +ld_clientcd +' ]' 'ClientName','" + strStartDt + "' 'Date','' 'ChequeNo','Opening Balance' 'Particular'," +
                        "Case When sum(ld_amount)  > 0 Then cast((sum(ld_amount)) as decimal (15,2))  else 0 end 'Debit'," +
                        "Case When sum(ld_amount)  < 0 Then cast(abs(sum(ld_amount)) as decimal (15,2))  else 0 end 'Credit'," +
                        "Case When sum(ld_amount)  > 0 Then cast((sum(ld_amount)) as decimal (15,2))*-1  else cast(abs(sum(ld_amount)) as decimal (15,2))*1 end 'Balance' " +
                        "From Ledger,Client_master " +
                        "Where ld_clientcd = cm_Cd and cm_Cd= '" + code + "' and ld_dt < '" + strStartDt + "' " +
                        "Group By ld_clientcd,cm_name " +
                        "Having sum(ld_amount) <> 0 " +
                        "union all " +
                        "select '','','','',0, 0,0 " +
                        "From Ledger,Client_master  " +
                        "Where ld_clientcd = cm_Cd and cm_Cd= '" + code + "' and ld_dt>= '" + strStartDt + "' " +
                        "Order by ClientName ";

                    Dt = objUtility.OpenDataTable(strsql);
                    if (Dt.Rows.Count > 0)
                    {
                        ResObj.Data = Dt;
                    }
                    else
                    {
                        ResObj.Data = "Record Not Found";
                    }

                    ResList.Add(ResObj);
                    j++;
                }
                return ResList;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic BillSummary(CrossBillSummaryReq req, string LoginAccess)
        {
            try
            {
                string strClientWhere = "";

                if (req.Filter.Client != null)
                {
                    if (req.Filter.Client.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Client.ToArray(), "##"));
                        strClientWhere += " or cm_cd in ('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Branch != null)
                {
                    if (req.Filter.Branch.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Branch.ToArray(), "##"));
                        strClientWhere += " or cm_brboffcode in ('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Family != null)
                {
                    if (req.Filter.Family.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Family.ToArray(), "##"));
                        strClientWhere += " or cm_familycd in ('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Group != null)
                {
                    if (req.Filter.Group.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Group.ToArray(), "##"));
                        strClientWhere += " or cm_groupcd in ('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (strClientWhere.Length > 0)
                {
                    strClientWhere = " and (" + strClientWhere.Substring(3) + ") ";
                }

                strsql = "select Rtrim(Ltrim(convert(char,bl_series)))  + '/' + Rtrim(Ltrim(convert(char,bl_bill_no))) 'BillNo', " +
                    "bl_client_id 'ClientID', cm_name 'ClientName',cast((bl_amount)as decimal(15,2)) 'BillAmount' ";
                if (req.LedgerBalance != false)
                {
                    strsql += ",cast((select sum(ld_amount) from Ledger with (nolock) where ld_clientcd = cm_cd)as decimal(15,2)) 'LedgerBalance' ";
                }
                strsql += "From Billing with (nolock), Client_Master with (nolock) " +
                    "Where bl_client_id = cm_cd  and bl_bill_dt = '" + req.BillDate + "' and cm_schedule = '" + GetSysParm("CMSCHEDULE").Trim() + "' " + LoginAccess;
                if (strClientWhere != "")
                {
                    strsql += strClientWhere;
                }
                if (req.BillType.ToUpper() == "O")
                {
                    strsql = strsql.ToString().Replace("Billing", "Bill_obg");
                }
                strsql += " Order By bl_bill_no ";

                DataTable Dt = objUtility.OpenDataTable(strsql);

                if (Dt.Rows.Count == 0)
                {
                    return "Record Not Found";
                }

                return Dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //public dynamic Ledger(CrossLedgerReq req, string LoginAccess)
        //{
        //    try
        //    {
        //        DataTable Dt;
        //        string strClientWhere = "";

        //        if (req.Filter.Client != null)
        //        {
        //            if (req.Filter.Client.All(y => y != ""))
        //            {
        //                strClientWhere += " or cm_cd in ('" + Strings.Join(req.Filter.Client.ToArray(), "','") + "')";
        //            }
        //        }
        //        if (req.Filter.Branch != null)
        //        {
        //            if (req.Filter.Branch.All(y => y != ""))
        //            {
        //                strClientWhere += " or cm_brboffcode in ('" + Strings.Join(req.Filter.Branch.ToArray(), "','") + "')";
        //            }
        //        }
        //        if (req.Filter.Family != null)
        //        {
        //            if (req.Filter.Family.All(y => y != ""))
        //            {
        //                strClientWhere += " or cm_familycd in ('" + Strings.Join(req.Filter.Family.ToArray(), "','") + "')";
        //            }
        //        }
        //        if (req.Filter.Group != null)
        //        {
        //            if (req.Filter.Group.All(y => y != ""))
        //            {
        //                strClientWhere += " or cm_groupcd in ('" + Strings.Join(req.Filter.Group.ToArray(), "','") + "')";
        //            }
        //        }
        //        if (strClientWhere.Length > 0)
        //        {
        //            strClientWhere = " and (" + strClientWhere.Substring(3) + ") ";
        //        }
        //        else
        //        {
        //            return "ID Cannot Be Left Blank";
        //        }

        //        //List<CrossLedgerResponse> LdrList = new List<CrossLedgerResponse>();

        //        //strsql = "select distinct cm_name +' [ ' + ld_clientcd +' ]' 'Name', ld_clientcd, " +
        //        //    "cm_add1+', '+cm_add2+', '+cm_add3+', '+cm_city+', '+cm_pin 'Address'," +
        //        //    "CONVERT(VARCHAR(10),GETDATE(),111) 'Date' " +
        //        //    "From Ledger,ClienT_master,client_backoffice " +
        //        //    "Where cm_cd=cb_cmcd and ld_clientcd = cm_Cd  " + strClientWhere + " and cm_schedule = '" + GetSysParm("CMSCHEDULE").Trim() + "'";

        //        //Dt = objUtility.OpenDataTable(strsql);


        //        //if (Dt.Rows.Count > 0)
        //        //{
        //        //    for (int i = 0; i < Dt.Rows.Count; i++)
        //        //    {
        //        //        CrossLedgerResponse LdrObj = new CrossLedgerResponse();
        //        //        List<AccountData> listAccDate = new List<AccountData>();
        //        //        AccountData objAccData = new AccountData();

        //        //        objAccData.Name = Dt.Rows[i]["Name"].ToString().Trim();
        //        //        objAccData.Address = Dt.Rows[i]["Address"].ToString().Trim();
        //        //        objAccData.Date = Dt.Rows[i]["Date"].ToString().Trim();
        //        //        listAccDate.Add(objAccData);

        //        //        LdrObj.Account = listAccDate;

        //        //        strsql = "select * from (select ld_clientcd,'" + req.From_Date + "' as Date,'' Voucher_No,'Opening Balance' Particular," +
        //        //        "Case When sum(ld_amount)  > 0 Then cast((sum(ld_amount)) as decimal (15,2))  else 0 end Debit," +
        //        //        "Case When sum(ld_amount) < 0 Then cast(abs(sum(ld_amount)) as decimal (15,2))  else 0 end Credit,'0' Balance " +
        //        //        "From Ledger,ClienT_master,client_backoffice " +
        //        //        "Where cm_cd=cb_cmcd and cm_cd=ld_clientcd and ld_clientcd = '" + Dt.Rows[i]["ld_clientcd"].ToString().Trim() + "' and ld_dt < '" + req.From_Date + "' and cm_schedule = '" + GetSysParm("CMSCHEDULE").Trim() + "' " +
        //        //        "Group By ld_clientcd " +
        //        //        "Having sum(ld_amount) <> 0 " +
        //        //        "union all " +
        //        //        "select ld_clientcd,ld_dt," +
        //        //        "case ld_documentno when '0' then '' else ld_documenttype + '/' + Ltrim(Rtrim(convert(char,ld_documentno))) end ld_documentno, " +
        //        //        "ltrim(rtrim(case ld_chequeno when '0' then ld_particular when '' then ld_particular else ld_particular +' [ ' +ld_chequeno +' ]' end)) ld_particular, " +
        //        //        "Case When ld_amount > 0 Then cast((ld_amount) as decimal (15,2)) else 0 end Debit," +
        //        //        "Case When ld_amount < 0 Then cast((abs(ld_amount)) as decimal (15,2)) else 0 end Credit,'0' Balance " +
        //        //        "From Ledger,ClienT_master,client_backoffice " +
        //        //        "Where cm_cd=cb_cmcd and cm_cd=ld_clientcd and ld_clientcd = '" + Dt.Rows[i]["ld_clientcd"].ToString().Trim() + "' and cm_schedule = '" + GetSysParm("CMSCHEDULE").Trim() + "' and ld_dt between '" + req.From_Date + "' and '" + req.To_Date + "') A " +
        //        //        "Order by Date ";

        //        //        DataTable Dt2 = objUtility.OpenDataTable(strsql);

        //        //        if (Dt2.Rows.Count > 0)
        //        //        {
        //        //            decimal Balance = 0;
        //        //            string strClient = "";
        //        //            foreach (DataRow ObjRow in Dt2.Rows)
        //        //            {
        //        //                if (strClient != (string)ObjRow["ld_clientcd"])
        //        //                {
        //        //                    Balance = 0;
        //        //                }
        //        //                if ((decimal)ObjRow["Credit"] > 0)
        //        //                {
        //        //                    ObjRow["Credit"] = (decimal)ObjRow["Credit"];
        //        //                    Balance = Balance - (decimal)ObjRow["Credit"];
        //        //                }
        //        //                else
        //        //                {
        //        //                    ObjRow["Debit"] = (decimal)ObjRow["Debit"];
        //        //                    Balance = Balance + (decimal)ObjRow["Debit"];
        //        //                }
        //        //                if ((decimal)Balance < 0 && (decimal)Balance != 0)
        //        //                    ObjRow["Balance"] = (-Balance).ToString() + " Cr";
        //        //                else if ((decimal)Balance > 0 && (decimal)Balance != 0)
        //        //                    ObjRow["Balance"] = Balance.ToString() + " Dr";
        //        //                else if ((decimal)Balance == 0)
        //        //                    ObjRow["Balance"] = Balance.ToString() + "   ";

        //        //                strClient = (string)ObjRow["ld_clientcd"];
        //        //            }
        //        //            string[] SelectedCol = new[] { "Date", "Voucher_No", "Particular", "Debit", "Credit", "Balance" };
        //        //            Dt2 = new DataView(Dt2).ToTable(false, SelectedCol);
        //        //            LdrObj.Ledger_Details = Dt2;
        //        //        }
        //        //        else
        //        //        {
        //        //            LdrObj.Ledger_Details = "Record Not Found";
        //        //        }

        //        //        LdrList.Add(LdrObj);
        //        //    }
        //        //}
        //        //return LdrList;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        public dynamic ReceiptPaymentAdd(ReceiptAddReq req, string UserId)
        {
            try
            {
                DataTable Dt;
                DateTime fromDt, toDt;
                bool IsUpdated = false;
                if (req.Date == "")
                {
                    return "Receipt Date Cannot Be Left Blank";
                }
                fromDt = DateTime.ParseExact(req.Date, "yyyyMMdd", null);
                if (req.ClearedOn != "")
                {
                    toDt = DateTime.ParseExact(req.ClearedOn, "yyyyMMdd", null);
                    if (fromDt > toDt)
                    {
                        return "Receipt Date Cannot Be Greater Than Cleared Date";
                    }
                }
                if (req.BankCode != "")
                {
                    strsql = "select distinct cm_cd from Client_master , Receipts where  cm_cd = rc_bankclientcd and rc_dpid='" + GetSysParm("dpid") + "' and rc_status = 'Y' and cm_cd = '" + req.BankCode + "' " +
                        "group by cm_cd,cm_bankname,cm_name, convert(char(8),convert(datetime, isnull(cm_acc_closuredate,'')),112)";
                    Dt = objUtility.OpenDataTable(strsql);
                    if (Dt.Rows.Count == 0)
                    {
                        return "Invalid Received In";
                    }
                }
                else
                {
                    return "Received In Cannot Be Blank";
                }
                if (req.Type != "R" && req.Type != "P")
                {
                    return "Invalid Type";
                }

                string DebitFlag = req.Type == "R" ? "C" : "D";
                string DocumentType = req.Type == "R" ? "R" : "P";
                string DocType = DocumentType == "R" ? "Receipts" : "Payments";

                strsql = "select  isnull(max(rc_srno),0)+1 rc_srno from receipts where rc_debitflag = '" + DebitFlag + "' and rc_accyear= '" + objUtility.mfnGetAccYearFromDate(req.Date) + "' and rc_dpid= '" + GetSysParm("dpid") + "'";
                string SrNo = objUtility.OpenDataTable(strsql).Rows[0][0].ToString();

                SqlTransaction SQLTrans;
                string ConnectionString = objUtility.GetConnectionStr();
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    con.Open();
                    SQLTrans = con.BeginTransaction();

                    int i = 1;

                    foreach (var item in req.Entries)
                    {
                        if (item.Account != "")
                        {
                            strsql = "select count(*) from client_master where cm_cd = '" + item.Account + "'";
                            Dt = objUtility.OpenDataTable(strsql, con, SQLTrans);
                            if (Conversion.Val(Dt.Rows[0][0].ToString()) == 0)
                            {
                                return "Invalid BOID";
                            }
                        }
                        else
                        {
                            return "BOID Cannot Be Blank";
                        }
                        if (item.Amount == 0)
                        {
                            return "Amount Cannot Be Zero";
                        }

                        string AuthDt = objUtility.GetDBDate();

                        strsql = "select rc_status,rc_authid1,rc_common,rc_cleareddt from receipts where";
                        if (req.SrNo == "")
                        {
                            strsql += " rc_srno = 0";
                        }
                        else
                        {
                            strsql += " rc_srno = " + req.SrNo;
                        }
                        strsql += " and rc_entryno = '" + i + "' and rc_debitflag = '" + DebitFlag + "'  and rc_accyear='" + objUtility.mfnGetAccYearFromDate(req.Date) + "' and rc_dpid='" + GetSysParm("dpid") + "'";

                        Dt = objUtility.OpenDataTable(strsql, con, SQLTrans);

                        if (Dt.Rows.Count == 0)
                        {
                            strsql = "insert into receipts values(" + SrNo + ",'" + req.VoucherNo + "','" + item.Account + "','" + req.Date + "'," + (req.Type == "R" ? (item.Amount * -1) : (item.Amount)) + ",'" + DebitFlag + "','" + item.Particular + "','" + req.BankCode + "','" + req.ClearedOn + "','" + i + "','" + item.ChequeNo + "','" + item.MICR + "','" + UserId.Trim().ToUpper() + "',CONVERT(VARCHAR(10),GETDATE(),112),'" + objUtility.mfnGetAccYearFromDate(req.Date) + "','" + GetSysParm("dpid") + "','','','" + AuthDt + "','" + AuthDt + "','Y','','" + DateTime.Now.ToString("yyyyMMdd") + "','','','0','" + DateTime.Now.ToString("HH:mm:ss") + "','','','000')";

                            objUtility.ExecuteSQL(strsql, con, SQLTrans);
                        }
                        else
                        {
                            if (Dt.Rows[0]["rc_status"].ToString().Trim() == "N" && Dt.Rows[0]["rc_authid1"].ToString().Trim() != "")
                            {
                                return "Receipt Entry is Partially Authorised";
                            }
                            else if (Dt.Rows[0]["rc_status"].ToString().Trim() == "Y" && Dt.Rows[0]["rc_authid1"].ToString().Trim() != "")
                            {
                                return "Receipt Entry is Authorised";
                            }
                            else if (Strings.InStr(Dt.Rows[0]["rc_common"].ToString().Trim(), "CONTRA", CompareMethod.Text) > 0)
                            {
                                return "Contra Entry cannot be edited";
                            }
                            else if (Conversion.Val(req.Date) < Conversion.Val(GetSysParm("LOCKDATA")))
                            {
                                return "Entries Locked for this Date";
                            }
                            else if (!string.IsNullOrEmpty(Dt.Rows[0]["rc_cleareddt"].ToString().Trim()))
                            {
                                return "Receipt is Not Editable";
                            }

                            SrNo = req.SrNo;

                            strsql = "update receipts set rc_bankclientcd='" + req.BankCode + "',";
                            strsql += "rc_voucherno='" + req.VoucherNo + "',rc_receiptdt='" + req.Date + "',";
                            strsql += "rc_cleareddt='" + req.ClearedOn + "',";
                            strsql += "rc_clientcd='" + item.Account + "',";
                            strsql += "rc_particular='" + item.Particular + "',";
                            strsql += "rc_chequeno='" + item.ChequeNo + "',";
                            strsql += "rc_amount=" + item.Amount + " ,";
                            strsql += "rc_dpid='" + GetSysParm("dpid") + "',mkrid='" + UserId.ToUpper() + "',mkrdt='" + DateTime.Now.ToString("yyyyMMdd") + "',";
                            strsql += "rc_micr='" + item.MICR + "'";
                            strsql += "where rc_srno = " + req.SrNo + " and  rc_dpid='" + GetSysParm("dpid") + "'and rc_debitflag = '" + DebitFlag + "' and rc_accyear='" + objUtility.mfnGetAccYearFromDate(req.Date) + "' and rc_entryno='" + i + "'";

                            objUtility.ExecuteSQL(strsql, con, SQLTrans);

                            IsUpdated = true;
                        }
                        i++;
                    }

                    strsql = "Delete from Ledger where ld_documenttype = '" + DocumentType + "' and ld_documentno = " + SrNo + "  and ld_accyear='" + objUtility.mfnGetAccYearFromDate(req.Date) + "' and ld_dpid='" + GetSysParm("dpid") + "'";
                    objUtility.ExecuteSQL(strsql, con, SQLTrans);
                    strsql = "select * from Auth_accounts where aa_documenttype = '" + DocumentType + "'  and aa_amount<=(select abs(sum(rc_amount)) from Receipts where rc_debitflag = '" + DebitFlag + "' and rc_srno = " + SrNo + " and rc_accyear='" + objUtility.mfnGetAccYearFromDate(req.Date) + "'and rc_dpid='" + GetSysParm("dpid") + "')";
                    DataTable dtset = objUtility.OpenDataTable(strsql, con, SQLTrans);
                    if (dtset.Rows.Count == 0)
                    {
                        objUtility.ExecuteSQL("update Receipts set rc_status='Y', rc_authid1='', rc_authid2='' where rc_debitflag = '" + DebitFlag + "' and rc_srno=" + SrNo + " and rc_accyear='" + objUtility.mfnGetAccYearFromDate(req.Date) + "'and rc_dpid='" + GetSysParm("dpid") + "'", con, SQLTrans);
                        if (mfnPostToLedger(req.Type == "R" ? "R" : "P", Convert.ToInt64(SrNo), objUtility.mfnGetAccYearFromDate(req.Date), con, SQLTrans).ToString().Trim() == "False")
                        {
                            return "Error in Posting to Ledger";
                        }
                    }
                    else
                    {
                        objUtility.ExecuteSQL("update Receipts set rc_status='N', rc_authid1='', rc_authid2='' where rc_debitflag = '" + DebitFlag + "' and rc_srno=" + SrNo + " and rc_accyear='" + objUtility.mfnGetAccYearFromDate(req.Date) + "'and rc_dpid='" + GetSysParm("dpid") + "'", con, SQLTrans);
                    }

                    SQLTrans.Commit();
                }

                if (IsUpdated)
                {
                    return "Serial No. for " + DocType + " voucher is " + req.SrNo;
                }
                return "Serial No. for " + DocType + " voucher is " + SrNo;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic ReceiptPaymentFind(string Type, string SerialNo)
        {
            try
            {
                DataTable Dt;
                string BankID, ReceivedIn, DPID, AccYear, strClientCd = "";
                double LedgerBal, Total = 0, TotalBal = 0;
                DPID = GetSysParm("dpid");
                AccYear = objUtility.mfnGetAccYearFromDate(DateTime.Now.ToString("yyyyMMdd"));

                if (Type != "R" && Type != "P")
                {
                    return "Invalid Type";
                }

                string DebitFlag = Type == "R" ? "C" : "D";


                strsql = "select 0 Balance, rc_clientcd Account, cm_name AccountName, rc_chequeno ChequeNo, rc_micr MICR, cast(rc_amount as decimal(15,2)) as Amount, rc_particular Particular, rc_bankclientcd, " +
                    "rc_srno, rc_receiptdt, rc_cleareddt, rc_voucherno, " +
                    "ledgerbal = (select isnull(cast(sum(ld_amount) as decimal(15,2)),0) from Ledger where ld_dpid = '" + DPID + "' and ld_clientcd = rc_clientcd) " +
                    "from Receipts, Client_master " +
                    "where cm_cd = rc_clientcd and rc_debitflag = '" + DebitFlag + "' and rc_srno = " + SerialNo + " and rc_dpid = '" + DPID + "' and rc_accyear = '" + AccYear + "' " +
                    "order by rc_entryno";

                Dt = objUtility.OpenDataTable(strsql);

                if (Dt.Rows.Count > 0)
                {
                    List<ReceiptFind> ListRes = new List<ReceiptFind>();
                    ReceiptFind ObjRes = new ReceiptFind();

                    BankID = Dt.Rows[0]["rc_bankclientcd"].ToString().Trim();

                    strsql = "select cast(sum(rc_amount)*(-1.0000) as decimal(15,2)) ledgerbal from Client_master , Receipts where  cm_cd = rc_bankclientcd and rc_dpid = '" + GetSysParm("dpid") + "' and rc_status = 'Y'  and cm_cd = '" + BankID + "' group by cm_cd,cm_bankname, convert(char(8),convert(datetime, isnull(cm_acc_closuredate,'')),112)";
                    LedgerBal = Convert.ToDouble(objUtility.OpenDataTable(strsql).Rows[0][0].ToString());

                    strsql = "select distinct cm_name from Client_master, Receipts where cm_cd = rc_bankclientcd and rc_dpid ='" + GetSysParm("dpid") + "' and rc_status = 'Y' and cm_cd = '" + BankID + "'";
                    ReceivedIn = objUtility.OpenDataTable(strsql).Rows[0][0].ToString().Trim();

                    ObjRes.SrNo = Dt.Rows[0]["rc_srno"].ToString().Trim();
                    ObjRes.Date = Dt.Rows[0]["rc_receiptdt"].ToString().Trim();
                    ObjRes.ClearedOn = Dt.Rows[0]["rc_cleareddt"].ToString().Trim();
                    ObjRes.VoucherNo = Dt.Rows[0]["rc_voucherno"].ToString().Trim();


                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        strClientCd = Dt.Rows[i]["Account"].ToString();

                        Total += Math.Abs(Convert.ToDouble(Dt.Rows[i]["Amount"].ToString().Trim()));

                        strsql = "select isnull(sum(ld_amount),0) ledgerbal from Client_master left join Ledger on (cm_cd=ld_clientcd and ld_dpid = '" + DPID + "'), Schedule where cm_cd = '" + strClientCd + "' " +
                            "and sc_cd=cm_schedule and sc_bankflag = 'N'  " +
                            "group by cm_cd,cm_name,cm_openingbal";
                        Dt.Rows[i]["Balance"] = Convert.ToDecimal(objUtility.OpenDataTable(strsql).Rows[0][0].ToString());
                    }
                    if (DebitFlag == "C")
                    {
                        TotalBal = LedgerBal - Total;
                    }
                    else
                    {
                        TotalBal = LedgerBal + Total;
                    }

                    ObjRes.Total = Total;
                    ObjRes.TotalBalance = LedgerBal;
                    ObjRes.Balance = TotalBal;
                    ObjRes.BankCode = ReceivedIn;

                    string[] SelectedCol = new[] { "Account", "AccountName", "Balance", "ChequeNo", "MICR", "Amount", "Particular" };
                    Dt = new DataView(Dt).ToTable(false, SelectedCol);
                    ObjRes.Entries = Dt;
                    ListRes.Add(ObjRes);

                    return ListRes;
                }
                else
                {
                    return "Record Not Found";
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic ReceiptPaymentDelete(string Type, string SerialNo, string UserId)
        {
            try
            {
                SqlTransaction Trans;
                string DPID = GetSysParm("dpid");
                string AccYear = objUtility.mfnGetAccYearFromDate(DateTime.Now.ToString("yyyyMMdd"));
                string DBTime = objUtility.GetDBTime();
                string PCName = Environment.MachineName;
                string DebitFlag = Type == "R" ? "C" : "D";
                string DocType = Type == "R" ? "R" : "P";

                if (Type != "R" && Type != "P")
                {
                    return "Invalid Type";
                }

                strsql = "select * from Receipts where rc_srno = " + SerialNo + " and rc_debitflag = '" + DebitFlag + "' and rc_accyear = '" + AccYear + "' and rc_dpid = '" + DPID + "'";
                DataTable Dt = objUtility.OpenDataTable(strsql);

                if (Dt.Rows.Count == 0)
                {
                    return "Record Not Found.";
                }

                int strDate = Convert.ToInt32(Dt.Rows[0]["rc_receiptdt"].ToString());

                if (strDate < Convert.ToInt32(GetSysParm("ACFROMDT")))
                {
                    return "Date Cannot be prior to " + GetSysParm("ACFROMDT") + ", so Entry Cannot Be Deleted.";
                }
                else if (strDate > Convert.ToInt32(GetSysParm("ACTODT")))
                {
                    return "Date more than allowed range of upto " + GetSysParm("ACTODT") + ", so Entry Cannot Be Deleted.";
                }
                else if (strDate < Convert.ToInt32(GetSysParm("LOCKDATA")))
                {
                    return "Account Data prior to " + GetSysParm("LOCKDATA") + " Locked, so Entry Cannot Be Deleted.";
                }

                string Connection = objUtility.GetConnectionStr();
                using (SqlConnection con = new SqlConnection(Connection))
                {
                    con.Open();
                    Trans = con.BeginTransaction();

                    strsql = "insert into Account_audit (au_documenttype,au_edflag,au_dpid,au_accyear,au_debitflag,au_srno,au_entryno,au_field,au_oldvalue,au_newvalue,au_voucherno,au_clientcd,au_receiptdt,au_amount,";
                    strsql = strsql + " au_particular,au_bankclientcd,au_cleareddt,au_chequeno,au_micr,au_commondt,au_common,mkrdt,mkrid,mkrtm,au_computername,mkridold, mkrdtold, mkrtmold) select 'R','D', rc_dpid, rc_accyear, rc_debitflag,rc_srno , rc_entryno,'','','', rc_voucherno , rc_clientcd, convert(char(8),rc_receiptdt ,112), ";
                    strsql = strsql + " rc_amount , rc_particular,";
                    strsql = strsql + " rc_bankclientcd , convert(char(8),isnull(rc_cleareddt,''),112) , rc_chequeno , rc_micr ,  rc_commondt, rc_common , mkrdt='" + DateTime.Now.ToString("yyyyMMdd") + "','" + UserId + "','" + DBTime + "','" + PCName + "',mkrid, convert(char(8),mkrdt,112),mkrtm from Receipts ";
                    strsql = strsql + " where rc_srno=  " + SerialNo + " and rc_debitflag = '" + DebitFlag + "' and rc_accyear='" + AccYear + "'  and rc_dpid='" + DPID + "' ";
                    objUtility.ExecuteSQL(strsql);

                    strsql = "delete from Receipts where rc_debitflag = '" + DebitFlag + "' and rc_srno = " + SerialNo + " and rc_dpid = '" + DPID + "' and rc_accyear = '" + AccYear + "'";
                    objUtility.ExecuteSQL(strsql, con, Trans);

                    strsql = "Delete from Ledger where ld_documenttype = '" + DocType + "' and ld_documentno ='" + SerialNo + "'  and ld_accyear = '" + AccYear + "' and ld_dpid='" + DPID + "'";
                    objUtility.ExecuteSQL(strsql, con, Trans);
                }
                return "Success";
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /*public dynamic PaymentAdd(ReceiptAddReq req, string UserId)
        {
            try
            {
                DataTable Dt;
                DateTime fromDt, toDt;
                bool IsUpdated = false;
                if (req.Date == "")
                {
                    return "Payment Date Cannot Be Left Blank";
                }
                fromDt = DateTime.ParseExact(req.Date, "yyyyMMdd", null);
                if (req.Cleared_On != "")
                {
                    toDt = DateTime.ParseExact(req.Cleared_On, "yyyyMMdd", null);
                    if (fromDt > toDt)
                    {
                        return "Payment Date Cannot Be Greater Than Cleared Date";
                    }
                }
                if (req.Received_In != "")
                {
                    strsql = "select distinct cm_cd from Client_master , Receipts where  cm_cd = rc_bankclientcd and rc_dpid='" + GetSysParm("dpid") + "' and rc_status = 'Y' and cm_cd = '" + req.Received_In + "' " +
                        "group by cm_cd,cm_bankname,cm_name, convert(char(8),convert(datetime, isnull(cm_acc_closuredate,'')),112)";
                    Dt = objUtility.OpenDataTable(strsql);
                    if (Dt.Rows.Count == 0)
                    {
                        return "Invalid Received In";
                    }
                }
                else
                {
                    return "Received In Cannot Be Blank";
                }

                List<string> ListBoid = req.Entries.Select(x => x.BOID).ToList();
                List<string> ListChqNo = req.Entries.Select(x => x.Cheque_No).ToList();
                List<string> ListMICR = req.Entries.Select(x => x.MICR).ToList();
                List<string> ListPerticuler = req.Entries.Select(x => x.Perticuler).ToList();
                List<double> ListAmount = req.Entries.Select(x => Math.Abs(x.Amount)).ToList();

                strsql = "select  isnull(max(rc_srno),0)+1 rc_srno from receipts where rc_debitflag = 'D' and rc_accyear= '" + objUtility.mfnGetAccYearFromDate(req.Date) + "' and rc_dpid= '" + GetSysParm("dpid") + "'";
                string SrNo = objUtility.OpenDataTable(strsql).Rows[0][0].ToString();

                SqlTransaction SQLTrans;
                string ConnectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    con.Open();
                    SQLTrans = con.BeginTransaction();

                    for (int i = 0; i < req.Entries.Count; i++)
                    {
                        if (ListBoid[i] != "")
                        {
                            strsql = "select count(*) from client_master where cm_cd = '" + ListBoid[i] + "'";
                            Dt = objUtility.OpenDataTable(strsql);
                            if (Conversion.Val(Dt.Rows[0][0].ToString()) == 0)
                            {
                                return "Invalid BOID";
                            }
                        }
                        else
                        {
                            return "BOID Cannot Be Blank";
                        }
                        if (ListAmount[i] == 0)
                        {
                            return "Amount Cannot Be Zero";
                        }

                        strsql = "SELECT CONVERT(DATETIME, '')";
                        string AuthDt = objUtility.OpenDataTable(strsql).Rows[0][0].ToString();

                        strsql = "select rc_status,rc_authid1,rc_common,rc_cleareddt from receipts where";
                        if (req.Sr_No == "")
                        {
                            strsql += " rc_srno = 0";
                        }
                        else
                        {
                            strsql += " rc_srno = " + req.Sr_No;
                        }
                        strsql += " and rc_entryno = '" + (i + 1) + "' and rc_debitflag = 'D'  and rc_accyear='" + objUtility.mfnGetAccYearFromDate(req.Date) + "' and rc_dpid='" + GetSysParm("dpid") + "'";
                        Dt = objUtility.OpenDataTable(strsql);

                        if (Dt.Rows.Count == 0)
                        {
                            strsql = "insert into receipts values('" + SrNo + "','" + req.Voucher_No + "','" + ListBoid[i] + "','" + req.Date + "'," + ListAmount[i] + ",'D','" + ListPerticuler[i] + "','" + req.Received_In + "','" + req.Cleared_On + "','" + (i + 1) + "','" + ListChqNo[i] + "','" + ListMICR[i] + "','" + UserId.Trim().ToUpper() + "',CONVERT(VARCHAR(10),GETDATE(),112),'" + objUtility.mfnGetAccYearFromDate(req.Date) + "','" + GetSysParm("dpid") + "','','','" + AuthDt + "','" + AuthDt + "','Y','','" + DateTime.Now.ToString("yyyyMMdd") + "','','','0','" + DateTime.Now.ToString("HH:mm:ss") + "','','','000')";

                            objUtility.ExecuteSQL(strsql, con, SQLTrans);
                        }
                        else
                        {
                            if (Dt.Rows[0]["rc_status"].ToString().Trim() == "N" && Dt.Rows[0]["rc_authid1"].ToString().Trim() != "")
                            {
                                return "Payment Entry is Partially Authorised";
                            }
                            else if (Dt.Rows[0]["rc_status"].ToString().Trim() == "Y" && Dt.Rows[0]["rc_authid1"].ToString().Trim() != "")
                            {
                                return "Payment Entry is Authorised";
                            }
                            else if (Strings.InStr(Dt.Rows[0]["rc_common"].ToString().Trim(), "CONTRA", CompareMethod.Text) > 0)
                            {
                                return "Contra Entry cannot be edited";
                            }
                            else if (Conversion.Val(req.Date) < Conversion.Val(GetSysParm("LOCKDATA")))
                            {
                                return "Entries Locked for this Date";
                            }
                            else if (!string.IsNullOrEmpty(Dt.Rows[0]["rc_cleareddt"].ToString().Trim()))
                            {
                                return "Payment is Not Editable";
                            }

                            SrNo = req.Sr_No;

                            strsql = "update receipts set rc_bankclientcd='" + req.Received_In + "',";
                            strsql += "rc_voucherno='" + req.Voucher_No + "',rc_receiptdt='" + req.Date + "',";
                            strsql += "rc_cleareddt='" + req.Cleared_On + "',";
                            strsql += "rc_clientcd='" + ListBoid[i] + "',";
                            strsql += "rc_particular='" + ListPerticuler[i] + "',";
                            strsql += "rc_chequeno='" + ListChqNo[i] + "',";
                            strsql += "rc_amount=" + ListAmount[i] + " ,";
                            strsql += "rc_dpid='" + GetSysParm("dpid") + "',mkrid='" + UserId.ToUpper() + "',mkrdt='" + DateTime.Now.ToString("yyyyMMdd") + "',";
                            strsql += "rc_micr='" + ListMICR[i] + "'";
                            strsql += "where rc_srno='" + req.Sr_No + "'and  rc_dpid='" + GetSysParm("dpid") + "'and rc_debitflag = 'D' and rc_accyear='" + objUtility.mfnGetAccYearFromDate(req.Date) + "' and rc_entryno='" + (i + 1) + "'";

                            objUtility.ExecuteSQL(strsql, con, SQLTrans);

                            IsUpdated = true;
                        }
                    }

                    strsql = "Delete from Ledger where ld_documenttype = 'P' and ld_documentno='" + SrNo + "'  and ld_accyear='" + objUtility.mfnGetAccYearFromDate(req.Date) + "' and ld_dpid='" + GetSysParm("dpid") + "'";
                    objUtility.ExecuteSQL(strsql, con, SQLTrans);
                    strsql = "select * from Auth_accounts where aa_documenttype = 'P'  and aa_amount<=(select abs(sum(rc_amount)) from Receipts where rc_debitflag = 'D' and rc_srno=" + SrNo + " and rc_accyear='" + objUtility.mfnGetAccYearFromDate(req.Date) + "'and rc_dpid='" + GetSysParm("dpid") + "')";
                    DataTable dtset = objUtility.OpenDataTable(strsql, con, SQLTrans);
                    if (dtset.Rows.Count == 0)
                    {
                        objUtility.ExecuteSQL("update Receipts set rc_status='Y', rc_authid1='', rc_authid2 = '' where rc_debitflag='D' and rc_srno = " + SrNo + " and rc_accyear='" + objUtility.mfnGetAccYearFromDate(req.Date) + "'and rc_dpid='" + GetSysParm("dpid") + "'", con, SQLTrans);
                        if (mfnPostToLedger("P", Convert.ToInt64(SrNo), objUtility.mfnGetAccYearFromDate(req.Date), con, SQLTrans).ToString().Trim() == "False")
                        {
                            return "Error in Posting to Ledger";
                        }
                    }
                    else
                    {
                        objUtility.ExecuteSQL("update Receipts set rc_status='N', rc_authid1='', rc_authid2='' where rc_debitflag='D' and rc_srno=" + SrNo + " and rc_accyear='" + objUtility.mfnGetAccYearFromDate(req.Date) + "'and rc_dpid='" + GetSysParm("dpid") + "'", con, SQLTrans);
                    }

                    SQLTrans.Commit();
                }
                if (IsUpdated)
                {
                    return "Serial No. for Receipts(s) voucher is " + req.Sr_No;
                }
                return "Serial No. for Receipts(s) voucher is " + SrNo;
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        public dynamic PaymentFind(string SerialNo)
        {
            try
            {
                DataTable Dt;
                string BankID, ReceivedIn, DPID, AccYear, strClientCd = "";
                double LedgerBal, Total = 0, TotalBal = 0;
                DPID = GetSysParm("dpid");
                AccYear = objUtility.mfnGetAccYearFromDate(DateTime.Now.ToString("yyyyMMdd"));

                strsql = "select 0 balance, cast(rc_amount as decimal(15,2)) as Amount, *, cm_name, " +
                    "ledgerbal = (select isnull(cast(sum(ld_amount) as decimal(15,2)),0) from Ledger where ld_dpid = '" + DPID + "' and ld_clientcd = rc_clientcd) " +
                    "from Receipts, Client_master " +
                    "where cm_cd = rc_clientcd and rc_debitflag = 'D' and rc_srno = " + SerialNo + " and rc_dpid = '" + DPID + "' and rc_accyear = '" + AccYear + "' " +
                    "order by rc_entryno";

                Dt = objUtility.OpenDataTable(strsql);

                if (Dt.Rows.Count > 0)
                {
                    List<ReceiptFind> ListRes = new List<ReceiptFind>();
                    ReceiptFind ObjRes = new ReceiptFind();

                    BankID = Dt.Rows[0]["rc_bankclientcd"].ToString().Trim();

                    strsql = "select cast(sum(rc_amount)*(-1.0000) as decimal(15,2)) ledgerbal from Client_master , Receipts where  cm_cd = rc_bankclientcd and rc_dpid = '" + GetSysParm("dpid") + "' and rc_status = 'Y'  and cm_cd = '" + BankID + "' group by cm_cd,cm_bankname, convert(char(8),convert(datetime, isnull(cm_acc_closuredate,'')),112)";
                    LedgerBal = Convert.ToDouble(objUtility.OpenDataTable(strsql).Rows[0][0].ToString());

                    strsql = "select distinct cm_name from Client_master, Receipts where cm_cd = rc_bankclientcd and rc_dpid ='" + GetSysParm("dpid") + "' and rc_status = 'Y' and cm_cd = '" + BankID + "'";
                    ReceivedIn = objUtility.OpenDataTable(strsql).Rows[0][0].ToString().Trim();

                    ObjRes.Serial_No = Dt.Rows[0]["rc_srno"].ToString().Trim();
                    ObjRes.Date = Dt.Rows[0]["rc_receiptdt"].ToString().Trim();
                    ObjRes.Cleared_On = Dt.Rows[0]["rc_cleareddt"].ToString().Trim();
                    ObjRes.Voucher_No = Dt.Rows[0]["rc_voucherno"].ToString().Trim();
                    ObjRes.Balance = LedgerBal;
                    ObjRes.Received_In = ReceivedIn;

                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        strClientCd = Dt.Rows[i]["rc_clientcd"].ToString();

                        Total += Math.Abs(Convert.ToDouble(Dt.Rows[i]["rc_amount"].ToString().Trim()));

                        strsql = "select isnull(sum(ld_amount),0) ledgerbal from Client_master left join Ledger on (cm_cd=ld_clientcd and ld_dpid = '" + DPID + "'), Schedule where cm_cd = '" + strClientCd + "' " +
                            "and sc_cd=cm_schedule and sc_bankflag = 'N'  " +
                            "group by cm_cd,cm_name,cm_openingbal";
                        Dt.Rows[i]["balance"] = Convert.ToDecimal(objUtility.OpenDataTable(strsql).Rows[0][0].ToString());
                    }

                    TotalBal = LedgerBal + Total;

                    ObjRes.Total = Total;
                    ObjRes.TotalBalance = LedgerBal;
                    ObjRes.Balance = TotalBal;
                    ObjRes.Received_In = ReceivedIn;

                    string[] SelectedCol = new[] { "rc_clientcd", "cm_name", "balance", "rc_chequeno", "rc_micr", "rc_amount", "rc_particular" };
                    Dt = new DataView(Dt).ToTable(false, SelectedCol);
                    ObjRes.Entries = Dt;
                    ListRes.Add(ObjRes);

                    return ListRes;
                }
                else
                {
                    return "Record Not Found";
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        public dynamic PaymentDelete(string SerialNo, string UserId)
        {
            try
            {
                SqlTransaction Trans;
                string DPID = GetSysParm("dpid");
                string AccYear = objUtility.mfnGetAccYearFromDate(DateTime.Now.ToString("yyyyMMdd"));
                string DBTime = objUtility.GetDBTime();
                string PCName = Environment.MachineName;

                strsql = "select * from Receipts where rc_srno = " + SerialNo + " and rc_debitflag = 'D' and rc_accyear = '" + AccYear + "' and rc_dpid = '" + DPID + "'";
                DataTable Dt = objUtility.OpenDataTable(strsql);

                int strDate = Convert.ToInt32(Dt.Rows[0]["rc_receiptdt"].ToString());

                if (strDate < Convert.ToInt32(GetSysParm("ACFROMDT")))
                {
                    return "Date Cannot be prior to " + GetSysParm("ACFROMDT") + ", so Entry Cannot Be Deleted.";
                }
                else if (strDate > Convert.ToInt32(GetSysParm("ACTODT")))
                {
                    return "Date more than allowed range of upto " + GetSysParm("ACTODT") + ", so Entry Cannot Be Deleted.";
                }
                else if (strDate < Convert.ToInt32(GetSysParm("LOCKDATA")))
                {
                    return "Account Data prior to " + GetSysParm("LOCKDATA") + " Locked, so Entry Cannot Be Deleted.";
                }

                string Connection = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection con = new SqlConnection(Connection))
                {
                    con.Open();
                    Trans = con.BeginTransaction();

                    strsql = "insert into Account_audit (au_documenttype,au_edflag,au_dpid,au_accyear,au_debitflag,au_srno,au_entryno,au_field,au_oldvalue,au_newvalue,au_voucherno,au_clientcd,au_receiptdt,au_amount,";
                    strsql = strsql + " au_particular,au_bankclientcd,au_cleareddt,au_chequeno,au_micr,au_commondt,au_common,mkrdt,mkrid,mkrtm,au_computername,mkridold, mkrdtold, mkrtmold) select 'P','D', rc_dpid, rc_accyear, rc_debitflag,rc_srno , rc_entryno,'','','', rc_voucherno , rc_clientcd, convert(char(8),rc_receiptdt ,112), ";
                    strsql = strsql + " rc_amount , rc_particular,";
                    strsql = strsql + " rc_bankclientcd , convert(char(8),isnull(rc_cleareddt,''),112) , rc_chequeno , rc_micr ,  rc_commondt, rc_common , mkrdt='" + DateTime.Now.ToString("yyyyMMdd") + "','" + UserId + "','" + DBTime + "','" + PCName + "',mkrid, convert(char(8),mkrdt,112),mkrtm from Receipts ";
                    strsql = strsql + " where rc_srno=  " + SerialNo + " and rc_debitflag='D' and rc_accyear='" + AccYear + "'  and rc_dpid='" + DPID + "' ";
                    objUtility.ExecuteSQL(strsql);

                    strsql = "delete from Receipts where rc_srno =  " + SerialNo + " and rc_debitflag = 'D' and rc_accyear = '" + AccYear + "' and rc_dpid = '" + DPID + "'";
                    objUtility.ExecuteSQL(strsql, con, Trans);

                    strsql = "Delete from Ledger where ld_documenttype = 'P' and ld_documentno = '" + SerialNo + "'  and ld_accyear = '" + AccYear + "' and ld_dpid = '" + DPID + "'";
                    objUtility.ExecuteSQL(strsql, con, Trans);
                }
                return "Success";
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }*/

        public dynamic ReceiptPaymentEntries(ReceiptPaymentEntriesReq req, string loginAccess)
        {
            try
            {
                DataTable Dt;
                string strDebitFlag = "", strReceiptDt = "", strSrNo = "";
                if (req.IsReceipt == "Y")
                {
                    strDebitFlag += ",'C'";
                }
                if (req.IsPayment == "Y")
                {
                    strDebitFlag += ",'D'";
                }
                if (strDebitFlag != "")
                {
                    strDebitFlag = "rc_debitflag in ( " + strDebitFlag.Substring(1) + " )";
                }
                if (req.FromDate != "")
                {
                    if (!IsValidDate(req.FromDate))
                    {
                        return "Invalid From Date";
                    }
                }
                else
                {
                    return "From Date Cannot Be Left Blank";
                }
                if (req.ToDate != "")
                {
                    if (!IsValidDate(req.ToDate))
                    {
                        return "Invalid To Date";
                    }
                }
                else
                {
                    return "To Date Cannot Be Left Blank";
                }
                if (req.IsReceipt == "" && req.IsPayment == "")
                {
                    return "Select at least One from IsReceipt and IsPayment";
                }

                string DPID = GetSysParm("dpid");

                strsql = "select rc_srno SrNo,";
                strsql += "case rc_debitflag when 'C' then 'Receipt' when 'D' then 'Payment' else '' end Voucher,";
                strsql += "rc_receiptdt Date, rc_clientcd BOID, c.cm_name Name, rc_chequeno ChequeNo,"; // '' Paid, cast((rc_amount)as decimal(15,2)) ReceivedIn ";
                strsql += "case rc_debitflag when 'D' then cast((rc_amount)as varchar(8)) else '' end Paid,";
                strsql += "cast((rc_amount)as decimal(15,2)) ReceivedIn ";
                strsql += "from Receipts b, Client_master c , Schedule ";
                strsql += "where c.cm_cd = rc_clientcd  and c.cm_schedule = sc_cd and  rc_dpid ='" + DPID + "' and " + strDebitFlag;
                strsql += "and rc_status = 'Y' and rc_receiptdt between '" + req.FromDate + "' and '" + req.ToDate + "' " + loginAccess;
                strsql += " order by rc_receiptdt,rc_bankclientcd,rc_srno";

                Dt = objUtility.OpenDataTable(strsql);

                if (Dt.Rows.Count > 0)
                {
                    List<ReceiptPaymentEntriesResponse> listRes = new List<ReceiptPaymentEntriesResponse>();
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        ReceiptPaymentEntriesResponse objRes = new ReceiptPaymentEntriesResponse();
                        objRes.SrNo = Dt.Rows[i]["SrNo"].ToString().Trim();
                        objRes.Voucher = Dt.Rows[i]["Voucher"].ToString().Trim();
                        objRes.Date = Dt.Rows[i]["Date"].ToString().Trim();
                        objRes.BO_ID = Dt.Rows[i]["BOID"].ToString().Trim();
                        objRes.BO_Name = Dt.Rows[i]["Name"].ToString().Trim();
                        objRes.ChequeNo = Dt.Rows[i]["ChequeNo"].ToString().Trim();
                        objRes.ReceivedIn = Math.Abs(Convert.ToDouble(Dt.Rows[i]["ReceivedIn"].ToString()));
                        objRes.Paid = Dt.Rows[i]["Paid"].ToString().Trim();

                        strReceiptDt = Dt.Rows[i]["Date"].ToString().Trim();
                        strSrNo = Dt.Rows[i]["SrNo"].ToString().Trim();

                        strsql = "select a.cm_name+', '+a.cm_add1+', '+a.cm_add2+', '+a.cm_add3+', '+a.cm_city+ ' - ' +a.cm_pin+', '+a.cm_state+', '+a.cm_country Debit, " +
                            "a.cm_tele1 Tele,a.cm_mobile MobileNumber,rc_srno SrNo,rc_receiptdt Date,rc_clientcd Acc_Code,bm_branchcd Branch, " +
                            "rc_particular Particular,rc_chequeno ChequeNo, rc_amount Amount " +
                            "from Client_master a,Client_master b,Receipts,Branch_master " +
                            "where a.cm_cd = rc_clientcd and b.cm_cd = rc_bankclientcd and a.cm_brboffcode=bm_branchcd and " +
                            "rc_dpid = '" + DPID + "' and rc_status = 'Y' and " + strDebitFlag + " and rc_srno = '" + strSrNo + "'  and rc_receiptdt = '" + strReceiptDt + "' " +
                            "order by rc_srno";

                        DataTable Dt1 = objUtility.OpenDataTable(strsql);

                        List<PaymentVoucher> listPayment = new List<PaymentVoucher>();

                        if (Dt1.Rows.Count > 0)
                        {
                            PaymentVoucher objPayment = new PaymentVoucher();

                            string[] SelectedCol = new[] { "Debit", "Tele", "MobileNumber", "SrNo", "Date", "Acc_Code", "Branch" };
                            DataTable Dt2 = new DataView(Dt1).ToTable(false, SelectedCol);

                            objPayment.Data1 = Dt2;

                            SelectedCol = new[] { "Particular", "ChequeNo", "Amount" };
                            Dt2 = new DataView(Dt1).ToTable(false, SelectedCol);

                            objPayment.Data2 = Dt2;

                            listPayment.Add(objPayment);
                        }

                        objRes.PaymentVoucher = listPayment;

                        listRes.Add(objRes);
                    }
                    return listRes;
                }
                else
                {
                    return "Record Not Found";
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region Receipt and Payment Functions
        public bool mfnPostToLedger(string strDocumentType, long lngSerial, string strAccyear, SqlConnection con, SqlTransaction SQLTrans)
        {
            string strsql = "";
            string strDebitflag = "";

            DataTable dtrs = new DataTable();
            strsql = "Delete from Ledger where ld_documenttype='" + strDocumentType + "' and ld_documentno= '" + lngSerial + "' and ld_accyear='" + strAccyear + "' and ld_dpid='" + GetSysParm("dpid") + "'";

            objUtility.ExecuteSQL(strsql, con, SQLTrans);
            if (strDocumentType.Trim() == "R" || strDocumentType.Trim() == "P")
            {
                strDebitflag = (strDocumentType.Trim() == "R" ? "C" : "D");
            }
            strsql = "select * from Receipts where rc_debitflag='" + strDebitflag + "' and  rc_accyear='" + strAccyear + "' and rc_srno=" + lngSerial + " and rc_dpid='" + GetSysParm("dpid") + "' and rc_status='Y'";
            DataTable dttemp = objUtility.OpenDataTable(strsql, con, SQLTrans);
            if (dttemp.Rows.Count == 0)
            {
                return false;
            }
            else
            {
                for (int i = 0; i <= dttemp.Rows.Count - 1; i++)
                {
                    strsql = "select * from Ledger where ld_documenttype='" + strDocumentType.Trim() + "' and ld_documentno='" + dttemp.Rows[i]["rc_srno"].ToString().Trim() + "' and ld_entryno=" + dttemp.Rows[i]["rc_Entryno"].ToString().Trim() + " and ld_accyear='" + dttemp.Rows[i]["rc_accyear"].ToString().Trim() + "' and ld_dpid='" + GetSysParm("dpid") + "'";
                    dtrs = objUtility.OpenDataTable(strsql, con, SQLTrans);
                    if (dtrs.Rows.Count == 0)
                    {

                        strsql = "insert into Ledger values('" + dttemp.Rows[i]["rc_clientcd"].ToString().Trim() + "', ";
                        strsql += " '" + dttemp.Rows[i]["rc_receiptdt"].ToString().Trim() + "',";
                        strsql += " " + dttemp.Rows[i]["rc_amount"].ToString().Trim() + ",";
                        strsql += " '" + dttemp.Rows[i]["rc_particular"].ToString().Trim() + "',";
                        strsql += " '" + dttemp.Rows[i]["rc_chequeno"].ToString().Trim() + "',";
                        strsql += " '" + (strDocumentType.Trim() == "R" ? "C" : "D") + "',";
                        strsql += " '" + strDocumentType.Trim() + "',";
                        strsql += " '" + dttemp.Rows[i]["rc_srno"].ToString().Trim() + "',";
                        strsql += " '" + dttemp.Rows[i]["rc_entryno"].ToString().Trim() + "',";
                        strsql += " '" + dttemp.Rows[i]["rc_costcenter"].ToString().Trim() + "',";
                        strsql += " '" + dttemp.Rows[i]["mkrid"].ToString().Trim() + "',";
                        strsql += " '" + dttemp.Rows[i]["mkrdt"].ToString().Trim() + "',";
                        strsql += " '" + dttemp.Rows[i]["rc_accyear"].ToString().Trim() + "',";
                        strsql += " '" + dttemp.Rows[i]["rc_dpid"].ToString().Trim() + "',";
                        strsql += " '" + dttemp.Rows[i]["rc_commondt"].ToString().Trim() + "',";
                        strsql += " '" + dttemp.Rows[i]["rc_common"].ToString().Trim() + "' )";
                        objUtility.ExecuteSQL(strsql, con, SQLTrans);
                    }
                    else
                    {
                        strsql = "update Ledger set ";
                        strsql += " ld_clientcd='" + dttemp.Rows[i]["rc_clientcd"].ToString().Trim() + "',";
                        strsql += " ld_dt='" + dttemp.Rows[i]["rc_receiptdt"].ToString().Trim() + "',";
                        strsql += " ld_amount=" + dttemp.Rows[i]["rc_amount"].ToString().Trim() + " ,";
                        strsql += " ld_particular='" + dttemp.Rows[i]["rc_particular"].ToString().Trim() + "',";
                        strsql += " ld_chequeno='" + dttemp.Rows[i]["rc_chequeno"].ToString().Trim() + "',";
                        strsql += " ld_debitflag='" + (strDocumentType.Trim() == "R" ? "C" : "D") + "',";
                        strsql += " ld_documenttype='" + strDocumentType.Trim() + "',";
                        strsql += " ld_documentno='" + dttemp.Rows[i]["rc_srno"].ToString().Trim() + "',";
                        strsql += " ld_entryno='" + dttemp.Rows[i]["rc_entryno"].ToString().Trim() + "',";
                        strsql += " ld_costcenter='" + dttemp.Rows[i]["rc_costcenter"].ToString().Trim() + "',";
                        strsql += " mkrid='" + dttemp.Rows[i]["mkrid"].ToString().Trim() + "',";
                        strsql += " mkrdt='" + dttemp.Rows[i]["mkrdt"].ToString().Trim() + "',";
                        strsql += " ld_accyear='" + dttemp.Rows[i]["rc_accyear"].ToString().Trim() + "',";
                        strsql += " ld_dpid='" + dttemp.Rows[i]["rc_dpid"].ToString().Trim() + "',";
                        strsql += " ld_commondt='" + dttemp.Rows[i]["rc_commondt"].ToString().Trim() + "',";
                        strsql += " ld_common='" + dttemp.Rows[i]["rc_common"].ToString().Trim() + "',";
                        strsql += " where ld_documenttype='" + strDocumentType.Trim() + "' and ld_documentno='" + dttemp.Rows[i]["rc_srno"].ToString().Trim() + "' and ld_entryno=" + dttemp.Rows[i]["rc_Entryno"].ToString().Trim() + " and ld_accyear='" + dttemp.Rows[i]["rc_accyear"].ToString().Trim() + "' and ld_dpid='" + GetSysParm("dpid") + "'";
                        objUtility.ExecuteSQL(strsql, con, SQLTrans);
                    }
                }
                return true;
            }
        }
        public bool IsValidDate(string Date)
        {
            strsql = "Select IsDate(" + Date.Trim() + ")";
            DataTable Dt = objUtility.OpenDataTable(strsql);

            if (Dt.Rows[0][0].ToString() == "1")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        public dynamic JournalAdd(JournalRequest req, string UserId)
        {
            try
            {
                string AcYear = "";
                string DPID = "", strCompWiseSrnoJ, strCompWiseSrnoL;
                string strDBDate = objUtility.GetDBDate();
                string strDBTime = objUtility.GetDBTime();
                string strPCName = System.Environment.MachineName;
                double CreditTotal = 0, DebitTotal = 0;
                string gsBankName = "Cross";
                bool IsUpdated = false;

                SqlTransaction ObjTrans;
                string ConnectionString = objUtility.GetConnectionStr();
                using (SqlConnection ObjCon = new SqlConnection(ConnectionString))
                {
                    ObjCon.Open();
                    ObjTrans = ObjCon.BeginTransaction();
                    SqlDataAdapter sqlDtAdap = new SqlDataAdapter();
                    SqlCommandBuilder sqlCmdBld = new SqlCommandBuilder(sqlDtAdap);

                    AcYear = objUtility.mfnGetAccYearFromDate(req.Date);
                    DPID = GetSysParm("dpid");

                    if (GetSysParm("COMPWISESRNO") == "Y")
                    {
                        if (Strings.UCase(Strings.Trim(gsBankName)) == "TRADEPLUS")
                        {
                            strCompWiseSrnoJ = " and left(jr_dpid,1)='" + Strings.Left(DPID, 1) + "'";
                            strCompWiseSrnoL = " and left(ld_dpid,1)='" + Strings.Left(DPID, 1) + "'";
                        }
                        else
                        {
                            strCompWiseSrnoJ = " and jr_dpid='" + DPID + "'";
                            strCompWiseSrnoL = " and ld_dpid='" + DPID + "'";
                        }
                    }
                    else
                    {
                        strCompWiseSrnoJ = "";
                        strCompWiseSrnoL = "";
                    }

                    strsql = "select isnull(max(jr_srno),0)+1 as maxsrno from Journal where jr_accyear = '" + AcYear + "' ";
                    int SrNo = Convert.ToInt32(objUtility.OpenDataTable(strsql).Rows[0][0].ToString());

                    List<string> ListCodes = req.Entries.Select(x => x.Account).ToList();
                    List<string> ListDrCr = req.Entries.Select(x => x.DrCr).ToList();
                    List<double> ListAmount = req.Entries.Select(x => Math.Abs(x.Amount)).ToList();
                    List<string> ListParticular = req.Entries.Select(x => x.Particular).ToList();

                    for (int i = 0; i < req.Entries.Count; i++)
                    {
                        for (int j = 0; j < req.Entries.Count; j++)
                        {
                            if (ListCodes[i] == ListCodes[j])
                            {
                                if (ListDrCr[i] == "C")
                                {
                                    CreditTotal += ListAmount[i];
                                }
                                else
                                {
                                    DebitTotal += ListAmount[i];
                                }
                                break;
                            }
                        }
                    }

                    //foreach(var item1 in req.Entries)
                    //{
                    //    foreach(var item2 in req.Entries)
                    //    {
                    //        if(item1.Account == item2.Account)
                    //        {
                    //            CreditTotal += item1.Credit;
                    //            DebitTotal += item1.Debit;
                    //        }
                    //    }
                    //}

                    if (DebitTotal == CreditTotal && DebitTotal > 0d)
                    {
                    }
                    else
                    {
                        return "Debit/Credit total not match.";
                    }

                    strsql = "select *  from Journal where jr_accyear='" + AcYear + "' and jr_dpid='" + DPID + "'";
                    if (req.SrNo == "")
                    {
                        strsql += " and jr_srno = 0 ";
                    }
                    else
                    {
                        strsql += " and jr_srno = " + req.SrNo;
                    }

                    DataTable Dt = objUtility.OpenDataTable(strsql, ObjCon, ObjTrans);
                    if (Dt.Rows.Count > 0)
                    {
                        string strOldValue, strNewValue, strMkrId, strMkrDt, strMkrTm;

                        strsql = "Delete from Account_audit where au_srno = '" + req.SrNo + "' and au_accyear = '" + AcYear + "' and au_documenttype = 'J' and mkrdt = '" + DateTime.Now.ToString("yyyyMMdd") + "'";
                        objUtility.ExecuteSQL(strsql);

                        for (int i = 0; i < req.Entries.Count; i++)
                        {
                            double Amount = 0;
                            string Flag = "";
                            if (ListAmount[i] > 0)
                            {
                                Amount = ListAmount[i];
                                Flag = ListDrCr[i];
                            }
                            else
                            {
                                Amount = ListAmount[i];
                                Flag = ListDrCr[i];
                            }

                            if (i < Dt.Rows.Count)
                            {
                                strMkrId = Dt.Rows[i]["Mkrid"].ToString().Trim();
                                strMkrDt = Dt.Rows[i]["Mkrdt"].ToString().Trim();
                                strMkrTm = Dt.Rows[i]["Mkrtm"].ToString().Trim();
                            }
                            else
                            {
                                strMkrId = "";
                                strMkrDt = "";
                                strMkrTm = "";
                            }

                            if (req.Date != "")
                            {
                                if (i < Dt.Rows.Count)
                                {
                                    strOldValue = Dt.Rows[i]["jr_dt"].ToString().Trim();
                                }
                                else
                                {
                                    strOldValue = "";
                                }
                                strNewValue = req.Date.Trim();
                                if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                                {
                                    fnInsertAudit(DPID, "J", AcYear, Flag, req.SrNo, (i + 1).ToString(), "E", "jr_dt", strOldValue, strNewValue, UserId, strDBDate, strDBTime, strMkrId, strMkrDt, strMkrTm, strPCName, ObjCon, ObjTrans);
                                }
                            }
                            if (req.VoucherNo != "")
                            {
                                if (i < Dt.Rows.Count)
                                {
                                    strOldValue = Dt.Rows[i]["jr_voucherno"].ToString().Trim();
                                }
                                else
                                {
                                    strOldValue = "";
                                }
                                strNewValue = req.VoucherNo.Trim();
                                if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                                {
                                    fnInsertAudit(DPID, "J", AcYear, Flag, req.SrNo, (i + 1).ToString(), "E", "jr_voucherno", strOldValue, strNewValue, UserId, strDBDate, strDBTime, strMkrId, strMkrDt, strMkrTm, strPCName, ObjCon, ObjTrans);
                                }
                            }
                            if (req.Particular != "")
                            {
                                if (i < Dt.Rows.Count)
                                {
                                    strOldValue = Dt.Rows[i]["jr_sparticular"].ToString().Trim();
                                }
                                else
                                {
                                    strOldValue = "";
                                }
                                strNewValue = req.Particular.Trim();
                                if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                                {
                                    fnInsertAudit(DPID, "J", AcYear, Flag, req.SrNo, (i + 1).ToString(), "E", "jr_sparticular", strOldValue, strNewValue, UserId, strDBDate, strDBTime, strMkrId, strMkrDt, strMkrTm, strPCName, ObjCon, ObjTrans);
                                }
                            }
                            if (ListCodes[i] != "")
                            {
                                if (i < Dt.Rows.Count)
                                {
                                    strOldValue = Dt.Rows[i]["jr_clientcd"].ToString().Trim();
                                }
                                else
                                {
                                    strOldValue = "";
                                }
                                strNewValue = ListCodes[i];
                                if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                                {
                                    fnInsertAudit(DPID, "J", AcYear, Flag, req.SrNo, (i + 1).ToString(), "E", "jr_clientcd", strOldValue, strNewValue, UserId, strDBDate, strDBTime, strMkrId, strMkrDt, strMkrTm, strPCName, ObjCon, ObjTrans);
                                }
                            }
                            if (Flag == "D")
                            {
                                if (i < Dt.Rows.Count)
                                {
                                    strOldValue = Dt.Rows[i]["jr_amount"].ToString().Trim();
                                }
                                else
                                {
                                    strOldValue = "";
                                }
                                strNewValue = Amount.ToString();
                                if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                                {
                                    fnInsertAudit(DPID, "J", AcYear, Flag, req.SrNo, (i + 1).ToString(), "E", "jr_amount", strOldValue, strNewValue, UserId, strDBDate, strDBTime, strMkrId, strMkrDt, strMkrTm, strPCName, ObjCon, ObjTrans);
                                }
                            }
                            else
                            {
                                if (i < Dt.Rows.Count)
                                {
                                    strOldValue = Dt.Rows[i]["jr_amount"].ToString().Trim();
                                }
                                else
                                {
                                    strOldValue = "";
                                }
                                strNewValue = (Amount * -1).ToString();
                                if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                                {
                                    fnInsertAudit(DPID, "J", AcYear, Flag, req.SrNo, (i + 1).ToString(), "E", "jr_amount", strOldValue, strNewValue, UserId, strDBDate, strDBTime, strMkrId, strMkrDt, strMkrTm, strPCName, ObjCon, ObjTrans);
                                }
                            }
                            if (ListParticular[i] != "")
                            {
                                if (i < Dt.Rows.Count)
                                {
                                    strOldValue = Dt.Rows[i]["jr_eparticular"].ToString().Trim();
                                }
                                else
                                {
                                    strOldValue = "";
                                }
                                strNewValue = ListParticular[i];
                                if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                                {
                                    fnInsertAudit(DPID, "J", AcYear, Flag, req.SrNo, (i + 1).ToString(), "E", "jr_eparticular", strOldValue, strNewValue, UserId, strDBDate, strDBTime, strMkrId, strMkrDt, strMkrTm, strPCName, ObjCon, ObjTrans);
                                }
                            }

                            IsUpdated = true;
                        }
                        #region foreach loop comment
                        /*foreach(var item in req.Entries)
                        {
                            int i = 0;
                            double Amount = 0;
                            string Flag = "";
                            if (item.Debit > 0)
                            {
                                Amount = item.Debit;
                                Flag = "D";
                            }
                            else
                            {
                                Amount = item.Credit;
                                Flag = "C";
                            }

                            if (i < Dt.Rows.Count)
                            {
                                strMkrId = Dt.Rows[i]["Mkrid"].ToString().Trim();
                                strMkrDt = Dt.Rows[i]["Mkrdt"].ToString().Trim();
                                strMkrTm = Dt.Rows[i]["Mkrtm"].ToString().Trim();
                            }
                            else
                            {
                                strMkrId = "";
                                strMkrDt = "";
                                strMkrTm = "";
                            }

                            if (req.Date != "")
                            {
                                if (i < Dt.Rows.Count)
                                {
                                    strOldValue = Dt.Rows[i]["jr_dt"].ToString().Trim();
                                }
                                else
                                {
                                    strOldValue = "";
                                }
                                strNewValue = req.Date.Trim();
                                if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                                {
                                    fnInsertAudit(DPID, "J", AcYear, Flag, req.SrNo, (i + 1).ToString(), "E", "jr_dt", strOldValue, strNewValue, UserId, strDBDate, strDBTime, strMkrId, strMkrDt, strMkrTm, strPCName, ObjCon, ObjTrans);
                                }
                            }
                            if (req.VoucherNo != "")
                            {
                                if (i < Dt.Rows.Count)
                                {
                                    strOldValue = Dt.Rows[i]["jr_voucherno"].ToString().Trim();
                                }
                                else
                                {
                                    strOldValue = "";
                                }
                                strNewValue = req.VoucherNo.Trim();
                                if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                                {
                                    fnInsertAudit(DPID, "J", AcYear, Flag, req.SrNo, (i + 1).ToString(), "E", "jr_voucherno", strOldValue, strNewValue, UserId, strDBDate, strDBTime, strMkrId, strMkrDt, strMkrTm, strPCName, ObjCon, ObjTrans);
                                }
                            }
                            if (req.Particular != "")
                            {
                                if (i < Dt.Rows.Count)
                                {
                                    strOldValue = Dt.Rows[i]["jr_sparticular"].ToString().Trim();
                                }
                                else
                                {
                                    strOldValue = "";
                                }
                                strNewValue = req.Particular.Trim();
                                if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                                {
                                    fnInsertAudit(DPID, "J", AcYear, Flag, req.SrNo, (i + 1).ToString(), "E", "jr_sparticular", strOldValue, strNewValue, UserId, strDBDate, strDBTime, strMkrId, strMkrDt, strMkrTm, strPCName, ObjCon, ObjTrans);
                                }
                            }
                            if (item.Account != "")
                            {
                                if (i < Dt.Rows.Count)
                                {
                                    strOldValue = Dt.Rows[i]["jr_clientcd"].ToString().Trim();
                                }
                                else
                                {
                                    strOldValue = "";
                                }
                                strNewValue = item.Account;
                                if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                                {
                                    fnInsertAudit(DPID, "J", AcYear, Flag, req.SrNo, (i + 1).ToString(), "E", "jr_clientcd", strOldValue, strNewValue, UserId, strDBDate, strDBTime, strMkrId, strMkrDt, strMkrTm, strPCName, ObjCon, ObjTrans);
                                }
                            }
                            if (Flag == "D")
                            {
                                if (i < Dt.Rows.Count)
                                {
                                    strOldValue = Dt.Rows[i]["jr_amount"].ToString().Trim();
                                }
                                else
                                {
                                    strOldValue = "";
                                }
                                strNewValue = Amount.ToString();
                                if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                                {
                                    fnInsertAudit(DPID, "J", AcYear, Flag, req.SrNo, (i + 1).ToString(), "E", "jr_amount", strOldValue, strNewValue, UserId, strDBDate, strDBTime, strMkrId, strMkrDt, strMkrTm, strPCName, ObjCon, ObjTrans);
                                }
                            }
                            else
                            {
                                if (i < Dt.Rows.Count)
                                {
                                    strOldValue = Dt.Rows[i]["jr_amount"].ToString().Trim();
                                }
                                else
                                {
                                    strOldValue = "";
                                }
                                strNewValue = (Amount * -1).ToString();
                                if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                                {
                                    fnInsertAudit(DPID, "J", AcYear, Flag, req.SrNo, (i + 1).ToString(), "E", "jr_amount", strOldValue, strNewValue, UserId, strDBDate, strDBTime, strMkrId, strMkrDt, strMkrTm, strPCName, ObjCon, ObjTrans);
                                }
                            }
                            if (item.Particular != "")
                            {
                                if (i < Dt.Rows.Count)
                                {
                                    strOldValue = Dt.Rows[i]["jr_eparticular"].ToString().Trim();
                                }
                                else
                                {
                                    strOldValue = "";
                                }
                                strNewValue = item.Particular;
                                if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                                {
                                    fnInsertAudit(DPID, "J", AcYear, Flag, req.SrNo, (i + 1).ToString(), "E", "jr_eparticular", strOldValue, strNewValue, UserId, strDBDate, strDBTime, strMkrId, strMkrDt, strMkrTm, strPCName, ObjCon, ObjTrans);
                                }
                            }

                            i++;
                            IsUpdated = true;
                        }*/
                        #endregion
                        strsql = "delete from Journal where jr_srno = " + req.SrNo + " and jr_accyear = '" + AcYear + "' and jr_dpid = '" + DPID + "' and sign(jr_entryno) = 1";
                        objUtility.ExecuteSQL(strsql, ObjCon, ObjTrans);

                        SrNo = Convert.ToInt32(req.SrNo);
                    }

                    for (int i = 0; i < req.Entries.Count; i++)
                    {
                        strsql = "select *  from Journal where jr_accyear = '" + AcYear + "' and jr_dpid = '" + DPID + "'";
                        if (req.SrNo == "")
                        {
                            strsql += " and jr_srno = 0 ";
                        }
                        else
                        {
                            strsql += " and jr_srno = " + req.SrNo;
                        }

                        DataSet Journal = objUtility.OpenDataSet(sqlDtAdap, strsql, ObjCon, ObjTrans);
                        DataRow rsJournal;
                        rsJournal = Journal.Tables[0].NewRow();

                        rsJournal["jr_clientcd"] = ListCodes[i];
                        rsJournal["jr_dt"] = req.Date;
                        rsJournal["mkrdt"] = strDBDate;
                        rsJournal["jr_voucherno"] = req.VoucherNo;
                        rsJournal["jr_sparticular"] = req.Particular;
                        rsJournal["jr_accyear"] = AcYear;
                        rsJournal["jr_dpid"] = DPID;

                        if (ListDrCr[i] == "D")
                        {
                            rsJournal["jr_debitflag"] = "D";
                            rsJournal["jr_amount"] = ListAmount[i];
                        }
                        else
                        {
                            rsJournal["jr_debitflag"] = "C";
                            rsJournal["jr_amount"] = (double)-1 * ListAmount[i];
                        }

                        rsJournal["jr_eparticular"] = ListParticular[i];
                        rsJournal["jr_srno"] = SrNo;
                        rsJournal["jr_costcenter"] = "000";

                        rsJournal["mkrid"] = UserId;
                        rsJournal["mkrtm"] = strDBTime;
                        rsJournal["jr_commondt"] = strDBDate;

                        rsJournal["jr_common"] = "";

                        rsJournal["jr_authid1"] = "";
                        rsJournal["jr_authid2"] = "";

                        rsJournal["jr_authtm1"] = "";
                        rsJournal["jr_authtm2"] = "";
                        rsJournal["jr_authremarks"] = "";
                        rsJournal["jr_status"] = "N";
                        rsJournal["jr_entryno"] = (i + 1);

                        Journal.Tables[0].Rows.Add(rsJournal);
                        sqlDtAdap.Update(Journal);
                    }

                    sqlDtAdap.Dispose();

                    if (!IsUpdated)
                    {
                        strsql = "insert into Account_audit (au_documenttype,au_edflag,au_dpid,au_accyear,au_debitflag,au_srno,au_entryno,au_field,au_oldvalue,au_newvalue,au_voucherno,au_clientcd,au_receiptdt,au_amount,";
                        strsql = strsql + " au_particular,au_bankclientcd,au_cleareddt,au_chequeno,au_micr,au_commondt,au_common,mkrdt,mkrid,mkrtm,au_computername,mkridold, mkrdtold, mkrtmold) ";
                        strsql = strsql + " select 'J','D', jr_dpid, jr_accyear, jr_debitflag, jr_srno , jr_entryno,'','','', jr_voucherno , jr_clientcd, convert(char(8),jr_dt,112) , ";
                        strsql = strsql + " jr_amount , jr_eparticular,";
                        strsql = strsql + " '', '', '', '',  jr_commondt, jr_common , mkrdt='" + strDBDate + "','" + UserId + "','" + strDBTime + "','" + strPCName + "',mkrid,convert(char(8), mkrdt,112), mkrtm from Journal ";
                        strsql = strsql + " where jr_srno=  " + SrNo + " and jr_accyear='" + AcYear + "' and jr_dpid='" + DPID + "' and sign(jr_entryno)=1";
                        objUtility.ExecuteSQL(strsql, ObjCon, ObjTrans);
                    }

                    strsql = " update Journal set jr_entryno = abs(jr_entryno) where jr_srno=" + SrNo + " and jr_accyear='" + AcYear + "' and jr_dpid='" + DPID + "' ";
                    objUtility.ExecuteSQL(strsql, ObjCon, ObjTrans);

                    strsql = "Delete from Ledger where ld_documenttype='J' and ld_documentno='" + SrNo + "'  and ld_accyear='" + AcYear + "' and ld_dpid='" + DPID + "'";
                    objUtility.ExecuteSQL(strsql, ObjCon, ObjTrans);

                    strsql = "select * from Auth_accounts where aa_documenttype='J'  and aa_amount<=(select sum(jr_amount) from Journal where jr_debitflag='D' and jr_srno=" + SrNo + " and jr_accyear='" + AcYear + "'" + strCompWiseSrnoJ + ")";
                    DataTable Dtt = objUtility.OpenDataTable(strsql, ObjCon, ObjTrans);

                    if (Dtt.Rows.Count == 0)
                    {
                        objUtility.ExecuteSQL("update Journal set jr_status='Y', jr_authid1='', jr_authid2='' where jr_srno=" + SrNo + " and jr_accyear='" + AcYear + "'" + strCompWiseSrnoJ, ObjCon, ObjTrans);
                        //if (mfnPostToLedger("J", SrNo , AcYear, ObjCon, ObjTrans) == false)
                        //{
                        //    return "Error in Posting to Ledger.";
                        //}
                    }
                    else
                    {
                        objUtility.ExecuteSQL("update Journal set jr_status='N', jr_authid1='', jr_authid2='' where  jr_srno=" + SrNo + " and jr_accyear='" + AcYear + "'" + strCompWiseSrnoJ, ObjCon, ObjTrans);
                    }

                    ObjTrans.Commit();

                    if (IsUpdated)
                    {
                        return "Record Updated and Serial No For This Report Is " + SrNo;
                    }
                    return "Serial No For This Report Is " + SrNo;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic JournalFind(string SerialNo)
        {
            try
            {
                DataTable Dt;
                string DPID, AccYear;
                double TotalDebit = 0, TotalCredit = 0;
                DPID = GetSysParm("dpid");
                AccYear = objUtility.mfnGetAccYearFromDate(DateTime.Now.ToString("yyyyMMdd"));

                strsql = "select jr_clientcd Account,cm_name AccountName,0 Balance,jr_debitflag DrCr,jr_amount Amount,jr_eparticular Particular," +
                    "jr_srno SrNo,jr_dt Date,jr_voucherno VoucherNo,jr_sparticular SParticular,jr_costcenter CostCenter " +
                    "from Journal,Client_master " +
                    "where cm_cd = jr_clientcd and jr_srno = " + SerialNo + " and jr_accyear = '" + AccYear + "' and jr_dpid = '" + DPID + "' " +
                    "order by jr_srno,jr_entryno";

                Dt = objUtility.OpenDataTable(strsql);

                if (Dt.Rows.Count > 0)
                {
                    List<JournalGetResponse> ListJRes = new List<JournalGetResponse>();
                    JournalGetResponse ObjJRes = new JournalGetResponse();

                    ObjJRes.SrNo = Dt.Rows[0]["SrNo"].ToString();
                    ObjJRes.Date = Dt.Rows[0]["Date"].ToString();
                    ObjJRes.VoucherNo = Dt.Rows[0]["VoucherNo"].ToString();
                    ObjJRes.Particular = Dt.Rows[0]["SParticular"].ToString();

                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        strsql = "select isnull(sum(ld_amount),0) ledgerbal from Client_master left join Ledger on (cm_cd=ld_clientcd and ld_dpid = '" + DPID + "'), Schedule where cm_cd = '" + Dt.Rows[i]["Account"].ToString() + "' " +
                            "and sc_cd=cm_schedule and sc_bankflag = 'N'  " +
                            "group by cm_cd,cm_name,cm_openingbal";
                        Dt.Rows[i]["Balance"] = Convert.ToDecimal(objUtility.OpenDataTable(strsql).Rows[0][0].ToString());

                        if (Dt.Rows[i]["DrCr"].ToString() == "D")
                        {
                            TotalDebit += Convert.ToDouble(Dt.Rows[i]["Amount"].ToString());
                        }
                        else
                        {
                            TotalCredit += Convert.ToDouble(Dt.Rows[i]["Amount"].ToString());
                        }
                    }

                    JournalTotalResponse ObjTotal = new JournalTotalResponse();
                    ObjTotal.Debit = TotalDebit;
                    ObjTotal.Credit = TotalCredit;

                    string[] SelectedCol = { "Account", "AccountName", "Balance", "DrCr", "Amount", "Particular", "CostCenter" };
                    Dt = new DataView(Dt).ToTable(false, SelectedCol);

                    ObjJRes.Entries = Dt;
                    ObjJRes.Total = ObjTotal;

                    ListJRes.Add(ObjJRes);
                    return ListJRes;
                }
                else
                {
                    return "Record Not Found";
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic JournalDelete(string SerialNo, string UserId)
        {
            try
            {
                string AcYear = objUtility.mfnGetAccYearFromDate(DateTime.Now.ToString("yyyyMMdd"));
                string Dpid = GetSysParm("dpid");
                string strDBDate = objUtility.GetDBDate();
                string strDBTime = objUtility.GetDBTime();
                string strPCName = System.Environment.MachineName;
                int strDate = 0;

                strsql = "select * from Journal where jr_srno= " + SerialNo + " and jr_accyear='" + AcYear + "' and jr_dpid='" + Dpid + "'";
                DataTable Dt = objUtility.OpenDataTable(strsql);

                strDate = Convert.ToInt32(Dt.Rows[0]["jr_dt"].ToString());

                if (strDate < Convert.ToInt32(GetSysParm("ACFROMDT")))
                {
                    return "Date Cannot be prior to " + GetSysParm("ACFROMDT") + ", so Entry Cannot Be Deleted.";
                }
                else if (strDate > Convert.ToInt32(GetSysParm("ACTODT")))
                {
                    return "Date more than allowed range of upto " + GetSysParm("ACTODT") + ", so Entry Cannot Be Deleted.";
                }
                else if (strDate < Convert.ToInt32(GetSysParm("LOCKDATA")))
                {
                    return "Account Data prior to " + GetSysParm("LOCKDATA") + " Locked, so Entry Cannot Be Deleted.";
                }

                SqlTransaction ObjTrans;
                string ConnectionString = objUtility.GetConnectionStr();
                using (SqlConnection ObjCon = new SqlConnection(ConnectionString))
                {
                    ObjCon.Open();
                    ObjTrans = ObjCon.BeginTransaction();

                    strsql = "insert into Account_audit (au_documenttype,au_edflag,au_dpid,au_accyear,au_debitflag,au_srno,au_entryno,au_field,au_oldvalue,au_newvalue,au_voucherno,au_clientcd,au_receiptdt,au_amount,";
                    strsql = strsql + " au_particular,au_bankclientcd,au_cleareddt,au_chequeno,au_micr,au_commondt,au_common,mkrdt,mkrid,mkrtm,au_computername,mkridold, mkrdtold, mkrtmold) ";
                    strsql = strsql + " select 'J','D', jr_dpid, jr_accyear, jr_debitflag, jr_srno , jr_entryno,'','','', jr_voucherno , jr_clientcd, convert(char(8),jr_dt,112) , ";
                    strsql = strsql + " jr_amount , jr_eparticular,";
                    strsql = strsql + " '', '', '', '',  jr_commondt, jr_common , mkrdt='" + strDBDate + "','" + UserId + "','" + strDBTime + "','" + strPCName + "',mkrid,convert(char(8), mkrdt,112), mkrtm from Journal ";
                    strsql = strsql + " where jr_srno=  " + SerialNo + " and jr_accyear='" + AcYear + "' and jr_dpid='" + Dpid + "' ";
                    objUtility.ExecuteSQL(strsql, ObjCon, ObjTrans);

                    objUtility.ExecuteSQL(" Delete from Journal where jr_srno= " + SerialNo + " and jr_accyear='" + AcYear + "' and jr_dpid='" + Dpid + "'", ObjCon, ObjTrans);
                    objUtility.ExecuteSQL(" Delete from Ledger where ld_documenttype='J' and ld_documentno='" + SerialNo + "' and ld_accyear='" + AcYear + "'  and ld_dpid='" + Dpid + "'", ObjCon, ObjTrans);

                    ObjTrans.Commit();
                    return "Record Deleted.";
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic DebitCreditNotesAdd(DebitNotesRequest req, string UserId)
        {
            try
            {
                DataTable Dt;
                if (req.Date != "")
                {
                    if (!IsValidDate(req.Date))
                    {
                        return "Invalid Date.";
                    }
                }
                else
                {
                    return "Date Cannot Be Blank.";
                }
                if (req.Amount == 0)
                {
                    return "Amount Cannot Be Blank.";
                }
                if (req.Account != "")
                {
                    strsql = "select * from Client_master where cm_cd = '" + req.Account + "'";
                    Dt = objUtility.OpenDataTable(strsql);

                    if (Dt.Rows.Count == 0)
                    {
                        return "Invalid Account Code.";
                    }
                }
                else
                {
                    return "Account Code Cannot Be Blank.";
                }
                if (req.CounterAccount != "")
                {
                    strsql = "select * from Client_master where cm_cd = '" + req.CounterAccount + "'";
                    Dt = objUtility.OpenDataTable(strsql);

                    if (Dt.Rows.Count == 0)
                    {
                        return "Invalid Account To Be Credit Code.";
                    }
                }
                else
                {
                    return "Account To Be Credited Cannot Be Blank.";
                }
                if (req.Type != "D" && req.Type != "C")
                {
                    return "Invalid Type";
                }
                string AcYear = objUtility.mfnGetAccYearFromDate(req.Date);
                string Dpid = GetSysParm("dpid");
                string strDBDate = objUtility.GetDBDate();
                string strDBTime = objUtility.GetDBTime();
                string strPCName = System.Environment.MachineName;
                string DebitFlag = req.Type == "D" ? "D" : "C";
                string DocType = req.Type == "D" ? "Debit " : "Credit ";
                bool IsUpdated = false;

                strsql = "select isnull(max(nt_srno),0) + 1 as srno1 from Notes where nt_debitflag = '" + DebitFlag + "' and nt_accyear = '" + AcYear + "'";
                int SrNo = Convert.ToInt32(objUtility.OpenDataTable(strsql).Rows[0][0].ToString());

                SqlTransaction ObjTrans;
                string ConnectionString = objUtility.GetConnectionStr();
                using (SqlConnection ObjCon = new SqlConnection(ConnectionString))
                {
                    ObjCon.Open();
                    ObjTrans = ObjCon.BeginTransaction();
                    SqlDataAdapter sqlDtAdap = new SqlDataAdapter();
                    SqlCommandBuilder sqlCmdBld = new SqlCommandBuilder(sqlDtAdap);

                    if (req.SrNo != "")
                    {
                        strsql = "select * from Notes where nt_srno = '" + req.SrNo + "' and nt_debitflag = '" + DebitFlag + "' and nt_accyear = '" + AcYear + "' and nt_dpid = '" + Dpid + "'";
                        Dt = objUtility.OpenDataTable(strsql);

                        if (Dt.Rows.Count > 0)
                        {
                            string strOldValue, strNewValue, strMkrId, strMkrDt, strMkrTm;

                            strsql = "Delete from Account_audit where au_srno = '" + req.SrNo + "' and au_accyear = '" + AcYear + "' and au_documenttype = '" + DebitFlag + "' and mkrdt = '" + DateTime.Now.ToString("yyyyMMdd") + "'";
                            objUtility.ExecuteSQL(strsql);

                            strMkrId = Dt.Rows[0]["Mkrid"].ToString().Trim();
                            strMkrDt = Dt.Rows[0]["Mkrdt"].ToString().Trim();
                            strMkrTm = Dt.Rows[0]["Mkrtm"].ToString().Trim();

                            if (req.Date != "")
                            {
                                strOldValue = Dt.Rows[0]["nt_dt"].ToString().Trim();
                                strNewValue = req.Date.Trim();
                                if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                                {
                                    fnInsertAudit(Dpid, DebitFlag, AcYear, DebitFlag, req.SrNo, "1", "E", "nt_dt", strOldValue, strNewValue, UserId, strDBDate, strDBTime, strMkrId, strMkrDt, strMkrTm, strPCName, ObjCon, ObjTrans);
                                }
                            }
                            if (req.Account != "")
                            {
                                strOldValue = Dt.Rows[0]["nt_clientcd"].ToString().Trim();
                                strNewValue = req.Account.Trim();
                                if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                                {
                                    fnInsertAudit(Dpid, DebitFlag, AcYear, DebitFlag, req.SrNo, "1", "E", "nt_clientcd", strOldValue, strNewValue, UserId, strDBDate, strDBTime, strMkrId, strMkrDt, strMkrTm, strPCName, ObjCon, ObjTrans);
                                }
                            }
                            if (req.Amount != 0)
                            {
                                strOldValue = Dt.Rows[0]["nt_amount"].ToString().Trim();
                                strNewValue = (Math.Abs(req.Amount)).ToString();
                                if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                                {
                                    fnInsertAudit(Dpid, DebitFlag, AcYear, DebitFlag, req.SrNo, "1", "E", "nt_amount", strOldValue, strNewValue, UserId, strDBDate, strDBTime, strMkrId, strMkrDt, strMkrTm, strPCName, ObjCon, ObjTrans);
                                }
                            }
                            if (req.CounterAccount != "")
                            {
                                strOldValue = Dt.Rows[0]["nt_cclientcd"].ToString().Trim();
                                strNewValue = req.CounterAccount.Trim();
                                if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                                {
                                    fnInsertAudit(Dpid, DebitFlag, AcYear, DebitFlag, req.SrNo, "1", "E", "nt_cclientcd", strOldValue, strNewValue, UserId, strDBDate, strDBTime, strMkrId, strMkrDt, strMkrTm, strPCName, ObjCon, ObjTrans);
                                }
                            }
                            if (req.Particular != "")
                            {
                                strOldValue = Dt.Rows[0]["nt_particular"].ToString().Trim();
                                strNewValue = req.Particular.Trim();
                                if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                                {
                                    fnInsertAudit(Dpid, DebitFlag, AcYear, DebitFlag, req.SrNo, "1", "E", "nt_particular", strOldValue, strNewValue, UserId, strDBDate, strDBTime, strMkrId, strMkrDt, strMkrTm, strPCName, ObjCon, ObjTrans);
                                }
                            }

                            strsql = "delete from Notes where nt_srno = " + req.SrNo + " and nt_debitflag = '" + DebitFlag + "' and nt_accyear = '" + AcYear + "' and nt_dpid = '" + Dpid + "'";
                            objUtility.ExecuteSQL(strsql, ObjCon, ObjTrans);

                            SrNo = Convert.ToInt32(req.SrNo);

                            IsUpdated = true;
                        }
                    }

                    strsql = "Select * from Notes where nt_accyear = '" + AcYear + "' and nt_dpid = '" + Dpid + "'";
                    if (req.SrNo != "")
                    {
                        strsql += " and nt_srno = " + req.SrNo;
                    }
                    else
                    {
                        strsql += " and nt_srno = 0 ";
                    }
                    DataSet Notes = objUtility.OpenDataSet(sqlDtAdap, strsql, ObjCon, ObjTrans);
                    DataRow rsNotes = Notes.Tables[0].NewRow();

                    rsNotes["nt_srno"] = SrNo;

                    rsNotes["nt_debitflag"] = DebitFlag;
                    rsNotes["nt_amount"] = Math.Abs(req.Amount);

                    rsNotes["nt_clientcd"] = req.Account;
                    rsNotes["nt_cclientcd"] = req.CounterAccount;
                    rsNotes["nt_particular"] = req.Particular;
                    rsNotes["nt_accyear"] = AcYear;
                    rsNotes["nt_dpid"] = Dpid;
                    rsNotes["nt_dt"] = req.Date;
                    rsNotes["mkrdt"] = strDBDate;
                    rsNotes["mkrid"] = UserId;
                    rsNotes["nt_commondt"] = strDBDate;
                    rsNotes["nt_common"] = "";

                    rsNotes["nt_authid1"] = "";
                    rsNotes["nt_authid2"] = "";

                    rsNotes["nt_authtm1"] = "";
                    rsNotes["nt_authtm2"] = "";
                    rsNotes["nt_authremarks"] = "";
                    rsNotes["nt_status"] = "N";

                    Notes.Tables[0].Rows.Add(rsNotes);
                    sqlDtAdap.Update(Notes);

                    objUtility.ExecuteSQL("Delete from Ledger where ld_documenttype = '" + DebitFlag + "' and ld_documentno = '" + SrNo + "'  and ld_accyear = '" + AcYear + "' and ld_dpid = '" + Dpid + "'", ObjCon, ObjTrans);

                    strsql = "select * from Auth_accounts where aa_documenttype = '" + DebitFlag + "'  and aa_amount <= (select abs(sum(nt_amount)) from Notes where nt_debitflag = '" + DebitFlag + "' and nt_srno = ";
                    strsql = strsql + SrNo + " and nt_accyear = '" + AcYear + "' and nt_dpid = '" + Dpid + "')";
                    Dt = objUtility.OpenDataTable(strsql, ObjCon, ObjTrans);

                    if (Dt.Rows.Count == 0)
                    {
                        objUtility.ExecuteSQL("update Notes set nt_status='Y', nt_authid1='', nt_authid2='' where nt_debitflag = '" + DebitFlag + "' and nt_srno = " + SrNo + " and nt_accyear = '" + AcYear + "' and nt_dpid = '" + Dpid + "'", ObjCon, ObjTrans);
                        //if (mfnPostToLedger("D", SrNo, AcYear, ObjCon, ObjTrans) == false)
                        //{
                        //    return "Error in Posting to Ledger.";
                        //}
                    }
                    else
                    {
                        objUtility.ExecuteSQL("update Notes  set nt_status='N', nt_authid1='', nt_authid2='' where nt_debitflag = '" + DebitFlag + "' and nt_srno=" + SrNo + " and nt_accyear='" + AcYear + "' and nt_dpid = '" + Dpid + "'", ObjCon, ObjTrans);
                    }

                    ObjTrans.Commit();

                    if (IsUpdated)
                    {
                        return DocType + "Record Updated and Serial No For This Report Is " + SrNo;
                    }

                    return DocType + "Serial No For This Report Is " + SrNo;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic DebitCreditNotesFind(string Type, string DebitNote)
        {
            try
            {
                string Dpid = GetSysParm("dpid");
                string AccYear = objUtility.mfnGetAccYearFromDate(DateTime.Now.ToString("yyyyMMdd"));

                if (Type != "D" && Type != "C")
                {
                    return "Invalid Type";
                }

                string debitFlag = Type == "D" ? "D" : "C";
                strsql = "select nt_srno SrNo,nt_dt Date,nt_clientcd Account,nt_amount Amount,nt_cclientcd CounterAccount,nt_particular Particular " +
                    "from Notes " +
                    "where nt_srno = " + DebitNote + " and nt_debitflag = '" + debitFlag + "' and nt_accyear = '" + AccYear + "' and nt_dpid = '" + Dpid + "'";

                DataTable Dt = objUtility.OpenDataTable(strsql);

                if (Dt.Rows.Count > 0)
                {
                    return Dt;
                }
                return "Debit/Credit Note Not Found";
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic DebitCreditNotesDelete(string Type, string DebitNote, string UserId)
        {
            try
            {
                if (Type != "D" && Type != "C")
                {
                    return "Invalid Type";
                }

                string Dpid = GetSysParm("dpid");
                string AccYear = objUtility.mfnGetAccYearFromDate(DateTime.Now.ToString("yyyyMMdd"));
                string strDBDate = objUtility.GetDBDate();
                string strDBTime = objUtility.GetDBTime();
                string strPCName = Environment.MachineName;
                string DebitFlag = Type == "D" ? "D" : "C";
                string DocType = Type == "D" ? "Debit " : "Credit ";

                strsql = "select * from Notes where nt_srno = " + DebitNote + " and nt_debitflag = '" + DebitFlag + "' and nt_accyear = '" + AccYear + "' and nt_dpid = '" + Dpid + "'";

                DataTable Dt = objUtility.OpenDataTable(strsql);

                if (Dt.Rows.Count == 0)
                {
                    return "Record Not Found.";
                }

                int strDate = Convert.ToInt32(Dt.Rows[0]["nt_dt"].ToString());

                if (strDate < Convert.ToInt32(GetSysParm("ACFROMDT")))
                {
                    return "Date Cannot be prior to " + GetSysParm("ACFROMDT") + ", so Entry Cannot Be Deleted.";
                }
                else if (strDate > Convert.ToInt32(GetSysParm("ACTODT")))
                {
                    return "Date more than allowed range of upto " + GetSysParm("ACTODT") + ", so Entry Cannot Be Deleted.";
                }
                else if (strDate < Convert.ToInt32(GetSysParm("LOCKDATA")))
                {
                    return "Account Data prior to " + GetSysParm("LOCKDATA") + " Locked, so Entry Cannot Be Deleted.";
                }

                strsql = "insert into Account_audit (au_documenttype,au_edflag,au_dpid,au_accyear,au_debitflag,au_srno,au_entryno,au_field,au_oldvalue,au_newvalue,au_voucherno,au_clientcd,au_receiptdt,au_amount,";
                strsql = strsql + " au_particular,au_bankclientcd,au_cleareddt,au_chequeno,au_micr,au_commondt,au_common,mkrdt,mkrid,mkrtm,au_computername,mkridold, mkrdtold, mkrtmold) ";
                strsql = strsql + " select  '" + DebitFlag + "', 'D'";
                strsql = strsql + " ,nt_dpid ,nt_accyear, nt_debitflag ,nt_srno ,0,'','','','',nt_clientcd, convert(char(8),nt_dt,112) , nt_amount ";
                strsql = strsql + " , nt_particular ,nt_cclientcd ,'','','','','','" + strDBDate + "','" + UserId + "','" + strDBTime + "','" + strPCName + "',mkrid, convert(char,nt_dt,112),mkrtm  from Notes where nt_srno =" + DebitNote + " and nt_debitflag = '" + DebitFlag + "' and nt_accyear='" + AccYear + "'  and nt_dpid='" + Dpid + "'";
                objUtility.ExecuteSQL(strsql);

                strsql = "delete from Notes where nt_srno = " + DebitNote + " and nt_debitflag = '" + DebitFlag + "' and nt_accyear = '" + AccYear + "' and nt_dpid = '" + Dpid + "'";
                objUtility.ExecuteSQL(strsql);

                objUtility.ExecuteSQL("Delete from Ledger where ld_documentno = '" + DebitNote + "' and ld_documenttype = '" + DebitFlag + "' and ld_accyear='" + AccYear + "'  and ld_dpid='" + Dpid + "'");

                return DocType + "Note Deleted.";
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /*public dynamic HoldingFind(HoldingRequest req, string LoginAccess)
        {
            try
            {
                DataTable Dt;
                string strClientWhere = "", strHoldingType = "", strTable;

                if (req.Filter.Client != null)
                {
                    if (req.Filter.Client.All(y => y != ""))
                    {
                        strClientWhere += " or cm_cd in ('" + Strings.Join(req.Filter.Client.ToArray(), "','") + "')";
                    }
                }
                if (req.Filter.Branch != null)
                {
                    if (req.Filter.Branch.All(y => y != ""))
                    {
                        strClientWhere += " or cm_brboffcode in ('" + Strings.Join(req.Filter.Branch.ToArray(), "','") + "')";
                    }
                }
                if (req.Filter.Family != null)
                {
                    if (req.Filter.Family.All(y => y != ""))
                    {
                        strClientWhere += " or cm_familycd in ('" + Strings.Join(req.Filter.Family.ToArray(), "','") + "')";
                    }
                }
                if (req.Filter.Group != null)
                {
                    if (req.Filter.Group.All(y => y != ""))
                    {
                        strClientWhere += " or cm_groupcd in ('" + Strings.Join(req.Filter.Group.ToArray(), "','") + "')";
                    }
                }

                if (strClientWhere.Length > 0)
                {
                    strClientWhere = " and (" + strClientWhere.Substring(3) + ") ";
                }
                else
                {
                    return "Id Cannot Be Left Blank.";
                }

                if (req.BalanceType.All(y => y == ""))
                {
                    strHoldingType = " and hld_ac_type in ( '10','11','20','30','14','12','51','13','61','50','62','63','52' )";
                }
                else if (req.BalanceType.All(y => y != ""))
                {
                    strHoldingType = " and hld_ac_type in ('" + Strings.Join(req.BalanceType.ToArray(), "','") + "')";
                }

                if (req.AsOn == "")
                {
                    strTable = "Holding";
                }
                else
                {
                    strTable = "Holding_" + req.AsOn.Trim();

                    strsql = "IF EXISTS (SELECT 1 " +
                        "FROM INFORMATION_SCHEMA.TABLES " +
                        "WHERE TABLE_TYPE='BASE TABLE' " +
                        "AND TABLE_NAME='" + strTable + "') " +
                        "SELECT 1 AS res ELSE SELECT 0 AS res;";

                    Dt = objUtility.OpenDataTable(strsql);
                    if (Dt.Rows[0][0].ToString() == "0")
                    {
                        return "As On Holdings For This [ " + req.AsOn.Trim() + " ] Date Is Not Found.";
                    }
                }

                strsql = "select cm_name+' ['+hld_ac_code+']' Client,cm_add1 Add1,cm_add2 Add2,cm_add3 Add3,cm_city City,cm_pin Pin,cm_tele1 Telephone,(cm_sech_name + case when Len(cm_thih_name) > 0 Then ','+cm_thih_name else '' end) as Joint," +
                    "bs_description Status,bc_description Category,cm_chgsscheme Scheme,bm_branchname Branch,case cm_poaforpayin when 'Y' then 'Yes' else case cm_poaforpayin when 'N' then 'No' else '' end end POAforPayIn,cm_blsavingcd BackOfficeCD,cb_UID1 UID," +
                    "hld_isin_code ISIN,sc_isinname ISINName,bt_description BalanceType,hld_ac_pos Qty ";
                if (req.ShowValuation)
                {
                    strsql += ",sc_rate Rate,cast(Round((sc_Rate * hld_ac_pos),2) as decimal(15,2)) Value ";
                }
                strsql += "From " + strTable + ", Security, Client_master, Beneficiary_type, branch_master, Beneficiary_status, Beneficiary_category, Client_Backoffice " +
                    "where  cm_cd=cb_cmcd and  cm_brboffcode = bm_branchcd  and hld_isin_code = sc_isincode And hld_ac_code = cm_cd and bt_code = hld_ac_type and cm_active  in ( '01','02') " +
                    "and cm_active = bs_code and cm_acctype = bc_code " + LoginAccess + strClientWhere + strHoldingType + " Order By cm_cd, hld_ac_type, sc_isinname";

                Dt = objUtility.OpenDataTable(strsql);

                if (Dt.Rows.Count > 0)
                {
                    List<HoldingResponse> ListRes = new List<HoldingResponse>();
                    HoldingResponse ObjRes = new HoldingResponse();

                    ObjRes.Client = Dt.Rows[0]["Client"].ToString();
                    ObjRes.Add1 = Dt.Rows[0]["Add1"].ToString();
                    ObjRes.Add2 = Dt.Rows[0]["Add2"].ToString();
                    ObjRes.Add3 = Dt.Rows[0]["Add3"].ToString();
                    ObjRes.City = Dt.Rows[0]["City"].ToString();
                    ObjRes.Pin = Dt.Rows[0]["Pin"].ToString();
                    ObjRes.Telephone = Dt.Rows[0]["Telephone"].ToString();
                    ObjRes.Joint = Dt.Rows[0]["Joint"].ToString();
                    ObjRes.Status = Dt.Rows[0]["Status"].ToString();
                    ObjRes.Category = Dt.Rows[0]["Category"].ToString();
                    ObjRes.Scheme = Dt.Rows[0]["Scheme"].ToString();
                    ObjRes.BranchName = Dt.Rows[0]["Branch"].ToString();
                    ObjRes.POAforPayIn = Dt.Rows[0]["POAforPayIn"].ToString();
                    ObjRes.BackOfficeCD = Dt.Rows[0]["BackOfficeCD"].ToString();
                    ObjRes.UID = Dt.Rows[0]["UID"].ToString();

                    List<string> SelectedCol = new List<string>();
                    SelectedCol.Add("ISIN");
                    SelectedCol.Add("ISINName");
                    SelectedCol.Add("BalanceType");
                    SelectedCol.Add("Qty");
                    if (req.ShowValuation)
                    {
                        SelectedCol.Add("Rate");
                        SelectedCol.Add("Value");
                    }
                    string[] SelectedColumn = SelectedCol.ToArray();
                    Dt = new DataView(Dt).ToTable(false, SelectedColumn);

                    ObjRes.HoldingReport = Dt;

                    ListRes.Add(ObjRes);

                    return ListRes;
                }
                else
                {
                    return "Record Not Found.";
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }*/

        /*public dynamic TransactionStatementFind(TransectionStatementRequest req, string LoginAccess)
        {
            try
            {
                DataTable Dt;
                string strClientWhere = "", strTrxType = "", strParticular = "";

                if (req.Filter.Client != null)
                {
                    if (req.Filter.Client.All(y => y != ""))
                    {
                        strClientWhere += " or cm_cd in ('" + Strings.Join(req.Filter.Client.ToArray(), "','") + "')";
                    }
                }
                if (req.Filter.Branch != null)
                {
                    if (req.Filter.Branch.All(y => y != ""))
                    {
                        strClientWhere += " or cm_brboffcode in ('" + Strings.Join(req.Filter.Branch.ToArray(), "','") + "')";
                    }
                }
                if (req.Filter.Family != null)
                {
                    if (req.Filter.Family.All(y => y != ""))
                    {
                        strClientWhere += " or cm_familycd in ('" + Strings.Join(req.Filter.Family.ToArray(), "','") + "')";
                    }
                }
                if (req.Filter.Group != null)
                {
                    if (req.Filter.Group.All(y => y != ""))
                    {
                        strClientWhere += " or cm_groupcd in ('" + Strings.Join(req.Filter.Group.ToArray(), "','") + "')";
                    }
                }

                if (strClientWhere.Length > 0)
                {
                    strClientWhere = " and (" + strClientWhere.Substring(3) + ") ";
                }

                if (req.TransactionType.All(y => y != ""))
                {
                    strTrxType = " and td_narration in ('" + Strings.Join(req.TransactionType.ToArray(), "','") + "') ";
                }
                else
                {
                    strTrxType = " and td_narration in ('052','054','011','012','013','044','042','091','092','093','202','204','082') ";
                }

                strsql = "select td_trxdate Date,td_reference TrxNo,td_ac_code ClientID,cm_name ClientName,td_isin_code ISIN,sc_isinname ISINName,bt_description Type,'' Particular,td_qty Qty,td_debit_credit DrCr,";
                //strsql += "Case td_debit_credit  when 'D' then cast((td_qty)as decimal(15,3)) else 0 end  'Debit',";
                //strsql += "Case td_debit_credit  when 'C' then cast((td_qty)as decimal(15,3)) else 0 end  'Credit',";
                strsql += "sc_isinname,td_market_type,td_beneficiery,td_narration,td_settlement,td_counterdp,td_debit_credit,td_description ";
                strsql += "From  Security, Client_master , Beneficiary_type,Trxdetail ";
                strsql += "Where td_ac_code = cm_cd and td_booking_type not in ('13') and  td_isin_code = sc_isincode and  td_ac_type = bt_code " + LoginAccess + strTrxType;
                if (strClientWhere != "")
                {
                    strsql += strClientWhere;
                }
                if (req.ISIN != "")
                {
                    strsql += " and td_isin_code = '" + req.ISIN + "' ";
                }
                strsql += " and td_curdate between '" + req.FromDate + "' and '" + req.ToDate + "' ";
                strsql += "Order By cm_cd, sc_company_name,sc_isinname , td_isin_code, td_ac_type,  td_curdate, td_trxdate,td_debit_credit,td_market_type, td_settlement ";

                Dt = objUtility.OpenDataTable(strsql);

                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        strParticular = Dt.Rows[i]["td_description"].ToString();
                        if (Strings.Trim(Dt.Rows[i]["td_narration"].ToString()) == "044" || Strings.Trim(Dt.Rows[i]["td_narration"].ToString()) == "042")
                        {
                            strParticular = strParticular + " / " + Dt.Rows[i]["td_beneficiery"].ToString();
                            if (String.IsNullOrEmpty(Dt.Rows[i]["td_settlement"].ToString()) == false)
                                if (Dt.Rows[i]["td_settlement"].ToString() != "")
                                    strParticular = strParticular + " / " + Dt.Rows[i]["td_settlement"].ToString();
                        }
                        else if (Strings.Trim(Dt.Rows[i]["td_narration"].ToString()) == "204")
                        {
                            if (Strings.Left(Strings.UCase(strParticular), 5) == "INTER")
                            {
                                strParticular = "INTDEP-CR";
                                strParticular = strParticular + "/" + Strings.Trim(Dt.Rows[i]["td_counterdp"].ToString()) + " " + Strings.Trim(Dt.Rows[i]["td_beneficiery"].ToString());
                            }
                            if (Strings.Trim(Dt.Rows[i]["td_settlement"].ToString()) != "")
                                strParticular = strParticular + "/" + Strings.Trim(Dt.Rows[i]["td_settlement"].ToString());
                        }
                        else if (Strings.Trim(Dt.Rows[i]["td_narration"].ToString()) == "202")
                        {
                            if (Strings.UCase(Strings.Left(strParticular, 5)) == "INTER")
                            {
                                strParticular = "INTDEP-DR";
                                strParticular = strParticular + "/" + Strings.Trim(Dt.Rows[i]["td_counterdp"].ToString()) + " " + Strings.Trim(Dt.Rows[i]["td_beneficiery"].ToString());
                            }
                            if (Strings.Trim(Dt.Rows[i]["td_settlement"].ToString()) != "")
                                strParticular = strParticular + "/" + Strings.Trim(Dt.Rows[i]["td_settlement"].ToString());
                        }
                        else if (Strings.Trim(Dt.Rows[i]["td_narration"].ToString()) == "052")
                        {
                            if (Strings.Trim(Dt.Rows[i]["td_settlement"].ToString()) != "")
                                strParticular = strParticular + "/" + Dt.Rows[i]["td_settlement"].ToString() + "/" + objUtility.fnFireQueryCross("Market_type", "mt_description", "mt_code", Strings.Trim(Dt.Rows[i]["td_market_type"].ToString()), true);
                            if (Dt.Rows[i]["td_beneficiery"].ToString() != "")
                                strParticular = strParticular + "/" + Dt.Rows[i]["td_beneficiery"].ToString();
                        }
                        else if (Strings.Trim(Dt.Rows[i]["td_narration"].ToString()) == "054")
                        {
                            if (Strings.Len(Strings.Trim(Dt.Rows[i]["td_settlement"].ToString())) == 13)
                                strParticular = strParticular + "/" + Dt.Rows[i]["td_settlement"].ToString() + "/" + objUtility.fnFireQueryCross("Market_type", "mt_description", "mt_code", Strings.Trim(Dt.Rows[i]["td_market_type"].ToString()), true);
                            else
                                strParticular = strParticular + "/" + Dt.Rows[i]["td_settlement"];

                            if (Strings.Trim(Dt.Rows[i]["td_beneficiery"].ToString()) != "")
                                strParticular = strParticular + "/" + Strings.Trim(Dt.Rows[i]["td_beneficiery"].ToString());
                        }
                        else if (Strings.Trim(Dt.Rows[i]["td_narration"].ToString()) == "011")
                            strParticular = objUtility.fnFireQueryCross("Narration", "nr_description", "nr_code", Strings.Trim(Dt.Rows[i]["td_narration"].ToString()), true);
                        else if (Strings.Trim(Dt.Rows[i]["td_narration"].ToString()) == "013")
                            strParticular = objUtility.fnFireQueryCross("Narration", "nr_description", "nr_code", Strings.Trim(Dt.Rows[i]["td_narration"].ToString()), true);

                        if (Strings.Left(Strings.UCase(strParticular), 7) == "OVERDUE")
                            strParticular = strParticular + " " + Strings.Trim(Dt.Rows[i]["td_beneficiery"].ToString());

                        if (Dt.Rows[i]["td_debit_credit"].ToString() == "D")
                            strParticular = " To " + strParticular;
                        else
                            strParticular = " By " + strParticular;

                        if (Strings.Right(Strings.Trim(strParticular), 1) == "/")
                            strParticular = Strings.Left(Strings.Trim(strParticular), Strings.Len(Strings.Trim(strParticular)) - 1);
                        Dt.Rows[i]["Particular"] = strParticular;
                    }

                    string[] SelectedCol = { "Date", "TrxNo", "ClientID", "ClientName", "ISIN", "ISINName", "Type", "Particular", "Qty", "DrCr" };
                    Dt = new DataView(Dt).ToTable(false, SelectedCol);

                    return Dt;
                }
                else
                {
                    return "Record Not Found.";
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }*/

        public dynamic PerformanceReport(PerformanceRepRequest req, string LoginAccess)
        {
            try
            {
                if (req.FromDate.Trim() == "")
                {
                    return "From Date Cannot Be Left Blank.";
                }
                else
                {
                    if (!IsValidDate(req.FromDate))
                    {
                        return "Invalid From Date.";
                    }
                }
                if (req.ToDate.Trim() == "")
                {
                    return "To Date Cannot Be Left Blank.";
                }
                else
                {
                    if (!IsValidDate(req.ToDate))
                    {
                        return "Invalid To Date.";
                    }
                }
                if (Conversion.Val(req.FromDate) > Conversion.Val(req.ToDate))
                {
                    return "From Date Cannot Be Greater Than To Date.";
                }

                string strFields = string.Empty;
                string strGroupby = string.Empty;
                string strgroupbyInter = string.Empty;
                string strColHeader = string.Empty, strColHeaderTitle = string.Empty;
                string strColAlign = string.Empty, strColLength = string.Empty;
                string strHavingWhere = string.Empty;
                string strHavingCr = "", strHavingDr = "";

                SqlTransaction ObjTrans;
                string strConn = objUtility.GetConnectionStr();
                using (SqlConnection StrConn = new SqlConnection(strConn))
                {
                    StrConn.Open();
                    ObjTrans = StrConn.BeginTransaction();

                    if (req.Credit.All(y => y != ""))
                    {
                        strHavingCr = Strings.Join(req.Credit.ToArray(), ",");
                    }
                    if (req.Debit.All(y => y != ""))
                    {
                        strHavingDr += Strings.Join(req.Debit.ToArray(), ",");
                    }
                    if (strHavingCr.Length > 0)
                    {
                        string[] ArrHaving = strHavingCr.Split(",");

                        for (int i = 0; i < ArrHaving.Length; i++)
                        {
                            string Value = ArrHaving[i].ToString();
                            if (Value.ToUpper() == "ON MARKET")
                            {
                                strHavingWhere += "Or Sum(OnMktCr) > 0 ";
                            }
                            if (Value.ToUpper() == "OFF MARKET")
                            {
                                strHavingWhere += "Or Sum(OffMktCr) > 0 ";
                            }
                            if (Value.ToUpper() == "CORPORATE ACTION")
                            {
                                strHavingWhere += "Or Sum(CACr) > 0 ";
                            }
                            if (Value.ToUpper() == "DEMAT CONFIRMATION")
                            {
                                strHavingWhere += "Or Sum(DematCr) > 0 ";
                            }
                            if (Value.ToUpper() == "IPO")
                            {
                                strHavingWhere += "Or Sum(IPOCr) > 0 ";
                            }
                            if (Value.ToUpper() == "INTERDEPOSITORY (OFF MARKET)")
                            {
                                strHavingWhere += "Or Sum(InterDPBOCr) > 0 ";
                            }
                            if (Value.ToUpper() == "INTERDEPOSITORY (ON MARKET)")
                            {
                                strHavingWhere += "Or Sum(InterDPExCr) > 0 ";
                            }
                        }
                    }
                    if (strHavingDr.Length > 0)
                    {
                        string[] ArrHaving = strHavingDr.Split(",");

                        for (int i = 0; i < ArrHaving.Length; i++)
                        {
                            string Value = ArrHaving[i].ToString();

                            if (Value.ToUpper() == "ON MARKET")
                            {
                                strHavingWhere += "Or Sum(OnMktDr) > 0 ";
                            }
                            if (Value.ToUpper() == "OFF MARKET")
                            {
                                strHavingWhere += "Or Sum(OffMktDr) > 0 ";
                            }
                            if (Value.ToUpper() == "EARLY PAYIN")
                            {
                                strHavingWhere += "Or Sum(EarlypayinDr) > 0 ";
                            }
                            if (Value.ToUpper() == "INTERDEPOSITORY (OFF MARKET)")
                            {
                                strHavingWhere += "Or Sum(InterDPBODr) > 0 ";
                            }
                            if (Value.ToUpper() == "INTERDEPOSITORY (ON MARKET)")
                            {
                                strHavingWhere += "Or Sum(InterDPExDr) > 0 ";
                            }
                        }
                    }
                    if (strHavingWhere.Trim() != "")
                    {
                        strHavingWhere = " Having (" + strHavingWhere.Remove(0, 2) + ")";
                    }
                    else
                    {
                        return "Select Atleast one Transaction type to proceed.";
                    }

                    strFields = "cm_cd as ClientID, cm_name as ClientName";
                    strGroupby = "cm_cd, cm_name ";
                    string DataValue;
                    if (req.DataType == 0)
                    {
                        DataValue = " td_qty ";
                    }
                    else if (req.DataType == 1)
                    {
                        DataValue = " td_narration ";
                    }
                    else
                    {
                        DataValue = " td_qty*sc_rate ";
                    }

                    CreateTempTrxDetail(StrConn, ObjTrans, strHavingCr.ToUpper(), strHavingDr.ToUpper());

                    strsql = " insert into #TempTrxDetail ";
                    strsql = strsql + " select td_ac_code,td_isin_code,";
                    if (strHavingCr.ToUpper().Contains("ON MARKET"))
                    {
                        strsql = strsql + " isnull(" + (req.DataType == 1 ? "Count" : "Sum");
                        strsql = strsql + " (case td_debit_credit when 'C' then case td_narration when '052' then ";
                        strsql += DataValue + " end end),0) 'OnMktCr',";
                    }
                    if (strHavingDr.ToUpper().Contains("ON MARKET"))
                    {
                        strsql = strsql + " isnull(" + (req.DataType == 1 ? "Count" : "Sum");
                        strsql = strsql + " (case td_debit_credit when 'D' then case td_narration when '052' then ";
                        strsql += DataValue + " end end),0) 'OnMktDr',";
                    }
                    if (strHavingCr.ToUpper().Contains("OFF MARKET"))
                    {
                        strsql = strsql + " isnull(" + (req.DataType == 1 ? "Count" : "Sum");
                        strsql = strsql + " (case td_debit_credit when 'C' then case when td_narration in ('044','042') then ";
                        strsql += DataValue + " end end),0) 'OffMktCr',";
                    }
                    if (strHavingDr.ToUpper().Contains("OFF MARKET"))
                    {
                        strsql = strsql + " isnull(" + (req.DataType == 1 ? "Count" : "Sum");
                        strsql = strsql + " (case td_debit_credit when 'D' then case when td_narration in ('044','042') then ";
                        strsql += DataValue + " end end),0) 'OffMktDr',";
                    }
                    if (strHavingCr.ToUpper().Contains("CORPORATE ACTION"))
                    {
                        strsql = strsql + " isnull(" + (req.DataType == 1 ? "Count" : "Sum");
                        strsql = strsql + " (case td_debit_credit when 'C' then case td_narration when '082' then ";
                        strsql += DataValue + " end end),0) 'CACr',";
                    }
                    if (strHavingDr.ToUpper().Contains("EARLY PAYIN"))
                    {
                        strsql = strsql + " isnull(" + (req.DataType == 1 ? "Count" : "Sum");
                        strsql = strsql + " (case td_debit_credit when 'D' then case td_narration when '054' then ";
                        strsql += DataValue + " end end),0) 'EarlypayinDr',";
                    }
                    if (strHavingCr.ToUpper().Contains("DEMAT CONFIRMATION"))
                    {
                        strsql = strsql + " isnull(" + (req.DataType == 1 ? "Count" : "Sum");
                        strsql = strsql + " (case td_debit_credit when 'C' then case td_narration when '011' then ";
                        strsql += DataValue + " end end),0) 'DematCr', ";
                    }
                    if (strHavingDr.ToUpper().Contains("INTERDEPOSITORY (OFF MARKET)"))
                    {
                        strsql = strsql + " isnull(sum(Case when td_debit_credit ='D' and td_narration in ('202','204') Then case When Len(ltrim(Rtrim(td_counterdp))) = 8 and Len(ltrim(Rtrim(td_beneficiery))) = 8 and Left(Ltrim(Rtrim(td_counterdp)),2) = 'IN' Then ";
                        strsql += DataValue + " else 0 end else 0 end),0) 'InterDPBODr',";
                    }
                    if (strHavingCr.ToUpper().Contains("IPO"))
                    {
                        strsql = strsql + " isnull(" + (req.DataType == 1 ? "Count" : "Sum");
                        strsql = strsql + " (case td_debit_credit when 'C' then case td_narration when '082' then ";
                        strsql += DataValue + " end end),0) 'IPOCr',";
                    }
                    if (strHavingDr.ToUpper().Contains("INTERDEPOSITORY (ON MARKET)"))
                    {
                        strsql = strsql + " isnull(sum(Case when td_debit_credit ='D' and td_narration in ('202','204') Then case When Len(ltrim(Rtrim(td_counterdp))) = 8 and Len(ltrim(Rtrim(td_beneficiery))) = 8 and Left(Ltrim(Rtrim(td_counterdp)),2) = 'IN' Then 0 else ";
                        strsql += DataValue + " end else 0 end),0) 'InterDPExDr',";
                    }
                    if (strHavingCr.ToUpper().Contains("INTERDEPOSITORY (OFF MARKET)"))
                    {
                        strsql = strsql + " isnull(sum(Case when td_debit_credit ='C' and td_narration in ('202','204') Then Case when Len(ltrim(Rtrim(td_counterdp))) = 8 and Len(ltrim(Rtrim(td_beneficiery))) = 8 and Left(Ltrim(Rtrim(td_counterdp)),2) = 'IN' Then ";
                        strsql += DataValue + " else 0 end else 0 end ),0) 'InterDPBOCr',";
                    }
                    if (strHavingCr.ToUpper().Contains("INTERDEPOSITORY (ON MARKET)"))
                    {
                        strsql = strsql + " isnull(sum(Case when td_debit_credit ='C' and td_narration in ('202','204') Then Case when Len(ltrim(Rtrim(td_counterdp))) = 8 and Len(ltrim(Rtrim(td_beneficiery))) = 8 and Left(Ltrim(Rtrim(td_counterdp)),2) = 'IN' Then 0 else ";
                        strsql += DataValue + " end else 0 end),0) 'InterDPExCr',";
                    }
                    strsql = strsql.Substring(0, strsql.Length - 1);
                    strsql = strsql + "  From trxdetail with (nolock), Client_master with (nolock), security with (nolock), Branch_master with (nolock), Family_master with (nolock), Group_master with (nolock)";
                    strsql = strsql + " Where td_ac_code = cm_cd ";
                    strsql = strsql + " and td_isin_code=sc_isincode and cm_groupcd=gr_cd and cm_familycd=fm_cd and cm_brboffcode=bm_branchcd ";
                    strsql = strsql + " and td_curdate between '" + req.FromDate + "' and '" + req.ToDate + "' ";
                    strsql = strsql + " group by td_ac_code,td_isin_code";

                    objUtility.ExecuteSQL(strsql, StrConn, ObjTrans);

                    string strdeciaml = string.Empty;
                    strdeciaml = (req.DataType != 2 ? "(15,0)" : "(15,2)");

                    strsql = " select " + strFields + ",";
                    if (strHavingCr.ToUpper().Contains("ON MARKET"))
                    {
                        strsql += " cast((sum(OnMktCr))as decimal" + strdeciaml + ") 'OnMarketCr',";
                    }
                    if (strHavingDr.ToUpper().Contains("ON MARKET"))
                    {
                        strsql += " cast((sum(OnMktDr))as decimal" + strdeciaml + ") 'OnMarketDr',";
                    }
                    if (strHavingCr.ToUpper().Contains("OFF MARKET"))
                    {
                        strsql += " cast((sum(OffMktCr))as decimal" + strdeciaml + ") 'OffMarketCr',";
                    }
                    if (strHavingDr.ToUpper().Contains("OFF MARKET"))
                    {
                        strsql += " cast((sum(OffMktDr))as decimal" + strdeciaml + ") 'OffMarketDr',";
                    }
                    if (strHavingCr.ToUpper().Contains("CORPORATE ACTION"))
                    {
                        strsql += " cast((sum(CACr))as decimal" + strdeciaml + ") 'CorporateActionCr' ,";
                    }
                    if (strHavingDr.ToUpper().Contains("EARLY PAYIN"))
                    {
                        strsql += " cast((sum(EarlypayinDr))as decimal" + strdeciaml + ") 'EarlyPayInDr',";
                    }
                    if (strHavingCr.ToUpper().Contains("DEMAT CONFIRMATION"))
                    {
                        strsql += " cast((sum(DematCr))as decimal" + strdeciaml + ") 'DematConfirmationCr',";
                    }
                    if (strHavingDr.ToUpper().Contains("INTERDEPOSITORY (OFF MARKET)"))
                    {
                        strsql += " cast((sum(InterDPBODr))as decimal" + strdeciaml + ") 'InterDepositoryOffMarketDr',";
                    }
                    if (strHavingCr.ToUpper().Contains("IPO"))
                    {
                        strsql += " cast((sum(IPOCr))as decimal" + strdeciaml + ") 'IPOCr',";
                    }
                    if (strHavingDr.ToUpper().Contains("INTERDEPOSITORY (ON MARKET)"))
                    {
                        strsql += "cast((sum(InterDPExDr))as decimal" + strdeciaml + ") 'InterDepositoryOnMarketDr',";
                    }
                    if (strHavingCr.ToUpper().Contains("INTERDEPOSITORY (OFF MARKET)"))
                    {
                        strsql += " cast((sum(InterDPBOCr))as decimal" + strdeciaml + ") 'InterDepositoryOffMarketCr',";
                    }
                    if (strHavingCr.ToUpper().Contains("INTERDEPOSITORY (ON MARKET)"))
                    {
                        strsql += "cast((sum(InterDPExCr))as decimal" + strdeciaml + ") 'InterDepositoryOnMarketCr',";
                    }
                    strsql = strsql.Substring(0, strsql.Length - 1);
                    //strsql += " td_isin_code, cm_groupcd, cm_familycd, cm_brboffcode, cm_chgsscheme ";
                    strsql += " From #TempTrxDetail , Client_master with (nolock), security with (nolock), Branch_master with (nolock), Family_master with (nolock), Group_master with (nolock) , ChargesDetail with (nolock)";
                    strsql += " Where td_ac_code = cm_cd and td_isin_code=sc_isincode and cm_groupcd=gr_cd ";
                    strsql += " and cm_familycd=fm_cd and cm_brboffcode=bm_branchcd and cm_chgsscheme = cd_scheme and cd_code = 4 ";
                    strsql += " group by " + strGroupby + strHavingWhere;
                    strsql += " order by " + strGroupby;

                    DataTable DSTrx = objUtility.OpenDataTable(strsql, StrConn, ObjTrans);
                    if (DSTrx.Rows.Count > 0)
                    {
                        return DSTrx;
                    }
                    else
                    {
                        return "Record Not Found.";
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region Performance Report Functions
        public void CreateTempTrxDetail(SqlConnection objConn, SqlTransaction objTrans, string HavingCr, string HavingDr)
        {
            SqlCommand MyCmd = new SqlCommand();
            try
            {
                strsql = "Create Table #TempTrxDetail ( ";
                strsql += " td_ac_code VArChar(16),";
                strsql = strsql + " td_isin_code VArChar(12),";
                if (HavingCr.Contains("ON MARKET"))
                {
                    strsql = strsql + " OnMktCr Numeric(18,3) ,";
                }
                if (HavingDr.Contains("ON MARKET"))
                {
                    strsql = strsql + " OnMktDr Numeric(18,3) ,";
                }
                if (HavingCr.Contains("OFF MARKET"))
                {
                    strsql = strsql + " OffMktCr Numeric(18,3) ,";
                }
                if (HavingDr.Contains("OFF MARKET"))
                {
                    strsql = strsql + " OffMktDr Numeric(18,3) ,";
                }
                if (HavingCr.Contains("CORPORATE ACTION"))
                {
                    strsql = strsql + " CACr Numeric(18,3) ,";
                }
                if (HavingDr.Contains("EARLY PAYIN"))
                {
                    strsql = strsql + " EarlypayinDr Numeric(18,3) ,";
                }
                if (HavingCr.Contains("DEMAT CONFIRMATION"))
                {
                    strsql = strsql + " DematCr Numeric(18,3) ,";
                }
                if (HavingDr.Contains("INTERDEPOSITORY (OFF MARKET)"))
                {
                    strsql = strsql + " InterDPBODr Numeric(18,3) ,";
                }
                if (HavingCr.Contains("IPO"))
                {
                    strsql = strsql + " IPOCr Numeric(18,3) ,";
                }
                if (HavingDr.Contains("INTERDEPOSITORY (ON MARKET)"))
                {
                    strsql = strsql + " InterDPExDr Numeric(18,3) ,";
                }
                if (HavingCr.Contains("INTERDEPOSITORY (OFF MARKET)"))
                {
                    strsql = strsql + " InterDPBOCr Numeric(18,3) ,";
                }
                if (HavingCr.Contains("INTERDEPOSITORY (ON MARKET)"))
                {
                    strsql = strsql + " InterDPExCr Numeric(18,3)";
                }
                strsql += ")";
                MyCmd.Connection = objConn;
                MyCmd.Transaction = objTrans;
                MyCmd.CommandText = strsql;
                MyCmd.CommandTimeout = 1000;
                MyCmd.ExecuteNonQuery();
                MyCmd.Dispose();
            }
            catch
            {
                strsql = " drop Table #TempTrxDetail";

                MyCmd.Connection = objConn;
                MyCmd.Transaction = objTrans;
                MyCmd.CommandText = strsql;
                MyCmd.CommandTimeout = 1000;
                MyCmd.ExecuteNonQuery();
                MyCmd.Dispose();

                strsql = "Create Table #TempTrxDetail ( ";
                strsql += " td_ac_code VArChar(16),";
                strsql = strsql + " td_isin_code VArChar(12),";
                if (HavingCr.Contains("ON MARKET"))
                {
                    strsql = strsql + " OnMktCr Numeric(18,3) ,";
                }
                if (HavingDr.Contains("ON MARKET"))
                {
                    strsql = strsql + " OnMktDr Numeric(18,3) ,";
                }
                if (HavingCr.Contains("OFF MARKET"))
                {
                    strsql = strsql + " OffMktCr Numeric(18,3) ,";
                }
                if (HavingDr.Contains("OFF MARKET"))
                {
                    strsql = strsql + " OffMktDr Numeric(18,3) ,";
                }
                if (HavingCr.Contains("CORPORATE ACTION"))
                {
                    strsql = strsql + " CACr Numeric(18,3) ,";
                }
                if (HavingDr.Contains("EARLY PAYIN"))
                {
                    strsql = strsql + " EarlypayinDr Numeric(18,3) ,";
                }
                if (HavingCr.Contains("DEMAT CONFIRMATION"))
                {
                    strsql = strsql + " DematCr Numeric(18,3) ,";
                }
                if (HavingDr.Contains("INTERDEPOSITORY (OFF MARKET)"))
                {
                    strsql = strsql + " InterDPBODr Numeric(18,3) ,";
                }
                if (HavingCr.Contains("IPO"))
                {
                    strsql = strsql + " IPOCr Numeric(18,3) ,";
                }
                if (HavingDr.Contains("INTERDEPOSITORY (ON MARKET)"))
                {
                    strsql = strsql + " InterDPExDr Numeric(18,3) ,";
                }
                if (HavingCr.Contains("INTERDEPOSITORY (OFF MARKET)"))
                {
                    strsql = strsql + " InterDPBOCr Numeric(18,3) ,";
                }
                if (HavingCr.Contains("INTERDEPOSITORY (ON MARKET)"))
                {
                    strsql = strsql + " InterDPExCr Numeric(18,3)";
                }

                MyCmd.Connection = objConn;
                MyCmd.Transaction = objTrans;
                MyCmd.CommandText = strsql;
                MyCmd.CommandTimeout = 1000;
                MyCmd.ExecuteNonQuery();
                MyCmd.Dispose();
            }
        }
        #endregion

        /*public dynamic CreditNotesAdd(CreditNotesRequest req, string UserId)
        {
            try
            {
                DataTable Dt;
                if (req.Date != "")
                {
                    if (!IsValidDate(req.Date))
                    {
                        return "Invalid Date.";
                    }
                }
                else
                {
                    return "Date Cannot Be Blank.";
                }
                if(req.Amount == 0)
                {
                    return "Amount Cannot Be Blank.";
                }
                if (req.Account_Code != "")
                {
                    strsql = "select * from Client_master where cm_cd = '" + req.Account_Code + "'";
                    Dt = objUtility.OpenDataTable(strsql);

                    if (Dt.Rows.Count == 0)
                    {
                        return "Invalid Account Code.";
                    }
                }
                else
                {
                    return "Account Code Cannot Be Blank.";
                }
                if (req.Account_To_Be_Debited != "")
                {
                    strsql = "select * from Client_master where cm_cd = '" + req.Account_To_Be_Debited + "'";
                    Dt = objUtility.OpenDataTable(strsql);

                    if (Dt.Rows.Count == 0)
                    {
                        return "Invalid Account To Be Credit Code.";
                    }
                }
                else
                {
                    return "Account To Be Credited Cannot Be Blank.";
                }
                string AcYear = objUtility.mfnGetAccYearFromDate(req.Date);
                string Dpid = GetSysParm("dpid");
                string strDBDate = objUtility.GetDBDate();
                string strDBTime = objUtility.GetDBTime();
                string strPCName = System.Environment.MachineName;
                bool IsUpdated = false;

                strsql = "select isnull(max(nt_srno),0) + 1 as srno1 from Notes where nt_debitflag = 'C' and nt_accyear = '" + AcYear + "'";
                int SrNo = Convert.ToInt32(objUtility.OpenDataTable(strsql).Rows[0][0].ToString());

                SqlTransaction ObjTrans;
                string ConnectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection ObjCon = new SqlConnection(ConnectionString))
                {
                    ObjCon.Open();
                    ObjTrans = ObjCon.BeginTransaction();
                    SqlDataAdapter sqlDtAdap = new SqlDataAdapter();
                    SqlCommandBuilder sqlCmdBld = new SqlCommandBuilder(sqlDtAdap);

                    if (req.Credit_Note != "")
                    {
                        strsql = "select * from Notes where nt_srno = '" + req.Credit_Note + "' and nt_debitflag = 'C' and nt_accyear = '" + AcYear + "' and nt_dpid = '" + Dpid + "'";
                        Dt = objUtility.OpenDataTable(strsql);

                        if (Dt.Rows.Count > 0)
                        {
                            string strOldValue, strNewValue, strMkrId, strMkrDt, strMkrTm;

                            strsql = "Delete from Account_audit where au_srno = '" + req.Credit_Note + "' and au_accyear = '" + AcYear + "' and au_documenttype = 'C' and mkrdt = '" + DateTime.Now.ToString("yyyyMMdd") + "'";
                            objUtility.ExecuteSQL(strsql);

                            strMkrId = Dt.Rows[0]["Mkrid"].ToString().Trim();
                            strMkrDt = Dt.Rows[0]["Mkrdt"].ToString().Trim();
                            strMkrTm = Dt.Rows[0]["Mkrtm"].ToString().Trim();

                            if (req.Date != "")
                            {
                                strOldValue = Dt.Rows[0]["nt_dt"].ToString().Trim();
                                strNewValue = req.Date.Trim();
                                if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                                {
                                    fnInsertAudit(Dpid, "C", AcYear, "C", req.Credit_Note, "1", "E", "nt_dt", strOldValue, strNewValue, UserId, strDBDate, strDBTime, strMkrId, strMkrDt, strMkrTm, strPCName, ObjCon, ObjTrans);
                                }
                            }
                            if (req.Account_Code != "")
                            {
                                strOldValue = Dt.Rows[0]["nt_clientcd"].ToString().Trim();
                                strNewValue = req.Account_Code.Trim();
                                if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                                {
                                    fnInsertAudit(Dpid, "C", AcYear, "C", req.Credit_Note, "1", "E", "nt_clientcd", strOldValue, strNewValue, UserId, strDBDate, strDBTime, strMkrId, strMkrDt, strMkrTm, strPCName, ObjCon, ObjTrans);
                                }
                            }
                            if (req.Amount != 0)
                            {
                                strOldValue = Dt.Rows[0]["nt_amount"].ToString().Trim();
                                strNewValue = Math.Abs(req.Amount).ToString();
                                if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                                {
                                    fnInsertAudit(Dpid, "C", AcYear, "C", req.Credit_Note, "1", "E", "nt_amount", strOldValue, strNewValue, UserId, strDBDate, strDBTime, strMkrId, strMkrDt, strMkrTm, strPCName, ObjCon, ObjTrans);
                                }
                            }
                            if (req.Account_To_Be_Debited != "")
                            {
                                strOldValue = Dt.Rows[0]["nt_cclientcd"].ToString().Trim();
                                strNewValue = req.Account_To_Be_Debited.Trim();
                                if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                                {
                                    fnInsertAudit(Dpid, "C", AcYear, "C", req.Credit_Note, "1", "E", "nt_cclientcd", strOldValue, strNewValue, UserId, strDBDate, strDBTime, strMkrId, strMkrDt, strMkrTm, strPCName, ObjCon, ObjTrans);
                                }
                            }
                            if (req.Description != "")
                            {
                                strOldValue = Dt.Rows[0]["nt_particular"].ToString().Trim();
                                strNewValue = req.Description.Trim();
                                if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                                {
                                    fnInsertAudit(Dpid, "C", AcYear, "C", req.Credit_Note, "1", "E", "nt_particular", strOldValue, strNewValue, UserId, strDBDate, strDBTime, strMkrId, strMkrDt, strMkrTm, strPCName, ObjCon, ObjTrans);
                                }
                            }

                            strsql = "delete from Notes where nt_srno = " + req.Credit_Note + " and nt_accyear = '" + AcYear + "' and nt_dpid = '" + Dpid + "'";
                            objUtility.ExecuteSQL(strsql, ObjCon, ObjTrans);

                            SrNo = Convert.ToInt32(req.Credit_Note);

                            IsUpdated = true;
                        }
                    }

                    strsql = "Select * from Notes where nt_accyear = '" + AcYear + "' and nt_dpid = '" + Dpid + "'";
                    if (req.Credit_Note != "")
                    {
                        strsql += " and nt_srno = " + req.Credit_Note;
                    }
                    else
                    {
                        strsql += " and nt_srno = 0 ";
                    }
                    DataSet Notes = objUtility.OpenDataSet(sqlDtAdap, strsql, ObjCon, ObjTrans);
                    DataRow rsNotes = Notes.Tables[0].NewRow();

                    rsNotes["nt_srno"] = SrNo;

                    rsNotes["nt_debitflag"] = "C";
                    rsNotes["nt_amount"] = Math.Abs(req.Amount);

                    rsNotes["nt_clientcd"] = req.Account_Code;
                    rsNotes["nt_cclientcd"] = req.Account_To_Be_Debited;
                    rsNotes["nt_particular"] = req.Description;
                    rsNotes["nt_accyear"] = AcYear;
                    rsNotes["nt_dpid"] = Dpid;
                    rsNotes["nt_dt"] = req.Date;
                    rsNotes["mkrdt"] = strDBDate;
                    rsNotes["mkrid"] = UserId;
                    rsNotes["nt_commondt"] = strDBDate;
                    rsNotes["nt_common"] = "";

                    rsNotes["nt_authid1"] = "";
                    rsNotes["nt_authid2"] = "";

                    rsNotes["nt_authtm1"] = "";
                    rsNotes["nt_authtm2"] = "";
                    rsNotes["nt_authremarks"] = "";
                    rsNotes["nt_status"] = "N";

                    Notes.Tables[0].Rows.Add(rsNotes);
                    sqlDtAdap.Update(Notes);

                    objUtility.ExecuteSQL("Delete from Ledger where ld_documenttype = 'C' and ld_documentno = '" + SrNo + "'  and ld_accyear = '" + AcYear + "' and ld_dpid = '" + Dpid + "'", ObjCon, ObjTrans);

                    strsql = "select * from Auth_accounts where aa_documenttype='C'  and aa_amount <= (select abs(sum(nt_amount)) from Notes where nt_debitflag = 'C' and nt_srno = ";
                    strsql = strsql + SrNo + " and nt_accyear = '" + AcYear + "' and nt_dpid = '" + Dpid + "')";
                    Dt = objUtility.OpenDataTable(strsql, ObjCon, ObjTrans);

                    if (Dt.Rows.Count == 0)
                    {
                        objUtility.ExecuteSQL("update Notes set nt_status='Y', nt_authid1='', nt_authid2='' where nt_debitflag = 'C' and nt_srno = " + SrNo + " and nt_accyear = '" + AcYear + "' and nt_dpid = '" + Dpid + "'", ObjCon, ObjTrans);
                        //if (mfnPostToLedger("D", SrNo, AcYear, ObjCon, ObjTrans) == false)
                        //{
                        //    return "Error in Posting to Ledger.";
                        //}
                    }
                    else
                    {
                        objUtility.ExecuteSQL("update Notes  set nt_status='N', nt_authid1='', nt_authid2='' where nt_debitflag='C' and nt_srno=" + SrNo + " and nt_accyear='" + AcYear + "' and nt_dpid = '" + Dpid + "'", ObjCon, ObjTrans);
                    }

                    ObjTrans.Commit();

                    if (IsUpdated)
                    {
                        return "Record Updated and Serial No For This Report Is " + SrNo;
                    }

                    return "Serial No For This Report Is " + SrNo;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic CreditNotesFind(string CreditNote)
        {
            try
            {
                string Dpid = GetSysParm("dpid");
                string AccYear = objUtility.mfnGetAccYearFromDate(DateTime.Now.ToString("yyyyMMdd"));
                strsql = "select nt_srno Credit_Note,nt_dt Date,nt_clientcd Account_Code,nt_amount Amount,nt_cclientcd Account_To_Be_Debited,nt_particular Description " +
                    "from Notes " +
                    "where nt_srno = " + CreditNote + " and nt_debitflag = 'C' and nt_accyear = '" + AccYear + "' and nt_dpid = '" + Dpid + "'";

                DataTable Dt = objUtility.OpenDataTable(strsql);

                if (Dt.Rows.Count > 0)
                {
                    return Dt;
                }
                return "Debit/Credit Note Not Found";
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        public dynamic CreditNotesDelete(string CreditNote, string UserId)
        {
            try
            {
                string Dpid = GetSysParm("dpid");
                string AccYear = objUtility.mfnGetAccYearFromDate(DateTime.Now.ToString("yyyyMMdd"));
                string strDBDate = objUtility.GetDBDate();
                string strDBTime = objUtility.GetDBTime();
                string strPCName = Environment.MachineName;

                strsql = "select * from Notes where nt_srno = " + CreditNote + " and nt_debitflag = 'C' and nt_accyear = '" + AccYear + "' and nt_dpid = '" + Dpid + "'";

                DataTable Dt = objUtility.OpenDataTable(strsql);

                if (Dt.Rows.Count == 0)
                {
                    return "Credit Note Not Found.";
                }

                int strDate = Convert.ToInt32(Dt.Rows[0]["nt_dt"].ToString());

                if (strDate < Convert.ToInt32(GetSysParm("ACFROMDT")))
                {
                    return "Date Cannot be prior to " + GetSysParm("ACFROMDT") + ", so Entry Cannot Be Deleted.";
                }
                else if (strDate > Convert.ToInt32(GetSysParm("ACTODT")))
                {
                    return "Date more than allowed range of upto " + GetSysParm("ACTODT") + ", so Entry Cannot Be Deleted.";
                }
                else if (strDate < Convert.ToInt32(GetSysParm("LOCKDATA")))
                {
                    return "Account Data prior to " + GetSysParm("LOCKDATA") + " Locked, so Entry Cannot Be Deleted.";
                }

                strsql = "insert into Account_audit (au_documenttype,au_edflag,au_dpid,au_accyear,au_debitflag,au_srno,au_entryno,au_field,au_oldvalue,au_newvalue,au_voucherno,au_clientcd,au_receiptdt,au_amount,";
                strsql = strsql + " au_particular,au_bankclientcd,au_cleareddt,au_chequeno,au_micr,au_commondt,au_common,mkrdt,mkrid,mkrtm,au_computername,mkridold, mkrdtold, mkrtmold) ";
                strsql = strsql + " select  'C', 'D'";
                strsql = strsql + " ,nt_dpid ,nt_accyear, nt_debitflag ,nt_srno ,0,'','','','',nt_clientcd, convert(char(8),nt_dt,112) , nt_amount ";
                strsql = strsql + " , nt_particular ,nt_cclientcd ,'','','','','','" + strDBDate + "','" + UserId + "','" + strDBTime + "','" + strPCName + "',mkrid, convert(char,nt_dt,112),mkrtm  from Notes where nt_srno=" + CreditNote + " and nt_debitflag = 'C' and nt_accyear='" + AccYear + "'  and nt_dpid='" + Dpid + "'";
                objUtility.ExecuteSQL(strsql);

                strsql = "delete from Notes where nt_srno = " + CreditNote + " and nt_debitflag = 'C' and nt_accyear = '" + AccYear + "' and nt_dpid = '" + Dpid + "'";
                objUtility.ExecuteSQL(strsql);

                objUtility.ExecuteSQL("Delete from Ledger where ld_documentno = '" + CreditNote + "' and ld_documenttype = 'C' and ld_accyear='" + AccYear + "'  and ld_dpid='" + Dpid + "'");

                return "Credit Note Deleted.";
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }*/

        #region Insert Account Audit
        public void fnInsertAudit(string Dpid, string Tflag, string AccYear, string Flag, string SrNo, string EntryNo, string EntryFlag, string Field, string OldValue, string NewValue, string Mkrid, string Mkrdt, string Mkrtm, string OldMkrid, string OldMkrdt, string OldMkrtm, string PCname, SqlConnection con, SqlTransaction Trans)
        {
            strsql = "insert into Account_audit " +
                     "(au_dpid,au_documenttype,au_accyear,au_debitflag,au_srno,au_entryno,au_edflag,au_field,au_oldvalue,au_newvalue,mkrid,mkrdt,mkrtm,mkridold,mkrdtold,mkrtmold,au_computername) " +
                     "values " +
                     "('" + Dpid + "','" + Tflag + "','" + AccYear + "','" + Flag + "'," + SrNo + "," + EntryNo + ",'" + EntryFlag + "','" + Field + "','" + OldValue + "','" + NewValue + "','" + Mkrid + "','" + Mkrdt + "','" + Mkrtm + "','" + OldMkrid + "','" + OldMkrdt + "','" + OldMkrtm + "','" + PCname + "')";

            objUtility.ExecuteSQL(strsql, con, Trans);
        }
        #endregion

        public dynamic AuditFind(AuditRequest req, string UserId)
        {
            try
            {
                DataTable Dt;
                string strwhere = "", strMkrID = "";
                string strJournal = "", strDebit = "", strCredit = "", strReceipt = "", strPayment = "", strStatus = "";

                if (req.Filter.Client != null)
                {
                    if (req.Filter.Client.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Client.ToArray(), "##"));
                        strwhere += " or cm_cd in ('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Branch != null)
                {
                    if (req.Filter.Branch.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Branch.ToArray(), "##"));
                        strwhere += " or cm_brboffcode in ('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Family != null)
                {
                    if (req.Filter.Family.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Family.ToArray(), "##"));
                        strwhere += " or cm_familycd in ('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Group != null)
                {
                    if (req.Filter.Group.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Group.ToArray(), "##"));
                        strwhere += " or cm_groupcd in ('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (req.Filter.Maker != null)
                {
                    if (req.Filter.Maker.All(y => y != ""))
                    {
                        var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(req.Filter.Maker.ToArray(), "##"));
                        strwhere += " or mkrid in ('" + fltr.Replace("##", "','") + "')";
                        strMkrID = "mkrid in ('" + fltr.Replace("##", "','") + "')";
                    }
                }
                if (strwhere.Length > 0)
                {
                    strwhere = " and (" + strwhere.Substring(3) + ") ";
                }

                if (req.Filter.Maker.All(y => y != ""))
                {
                    if (req.AccountType.Journal)
                    {
                        strJournal = " and Journal.mkrid= '" + strMkrID + "'";
                    }
                    else
                    {
                        strJournal = "";
                    }
                    if (req.AccountType.Credit_Note)
                    {
                        strCredit = " and notes.mkrid= '" + strMkrID + "'";
                    }
                    else
                    {
                        strCredit = "";
                    }
                    if (req.AccountType.Debit_Note)
                    {
                        strDebit = " and notes.mkrid= '" + strMkrID + "'";
                    }
                    else
                    {
                        strDebit = "";
                    }
                    if (req.AccountType.Receipts)
                    {
                        strReceipt = " and Receipts.mkrid= '" + strMkrID + "'";
                    }
                    else
                    {
                        strReceipt = "";
                    }
                    if (req.AccountType.Payment)
                    {
                        strPayment = " and Receipts.mkrid= '" + strMkrID + "'";
                    }
                    else
                    {
                        strPayment = "";
                    }
                }

                switch (Convert.ToInt32(req.Status))
                {
                    case 0:  // All
                        {
                            strStatus = "";
                            break;
                        }
                    case 1:  // Data Entered (N)
                        {
                            strStatus = "N";
                            break;
                        }
                    case 2:  // Partial Authorised(N and authid1 <> "")
                        {
                            strStatus = "N";
                            break;
                        }
                    case 3:  // Authorised(Y)
                        {
                            strStatus = "Y";
                            break;
                        }
                    case 4:  // Rejected(R)
                        {
                            strStatus = "R";
                            break;
                        }
                }

                SqlTransaction ObjTrans;
                string ConnectionString = objUtility.GetConnectionStr();
                using (SqlConnection ObjCon = new SqlConnection(ConnectionString))
                {
                    ObjCon.Open();
                    ObjTrans = ObjCon.BeginTransaction();
                    string strDate;
                    string strFromdate = req.FromDate, strTodate = req.ToDate;
                    string strbal = "";
                    string strSrNoJoint = "";
                    string strsqlSr = "";
                    string strTable1 = "";
                    string gdpid = GetSysParm("dpid");

                    fnAuditTempTable(ObjCon, ObjTrans);

                    List<AuditResponse> ListAudit = new List<AuditResponse>();
                    AuditResponse ObjAudit = new AuditResponse();

                    if (req.AccountType.Journal)
                    {
                        if (req.DateType == 0)
                        {
                            strDate = " and jr_dt between '" + strFromdate + "' and '" + strTodate + "'";
                        }
                        else
                        {
                            strDate = " and Journal.mkrdt between '" + strFromdate + "' and '" + strTodate + "'";
                        }
                        if (!string.IsNullOrEmpty(Strings.Trim(strStatus)))
                        {
                            strDate = strDate + " and jr_status ='" + strStatus + "'";
                            if (req.Status == 2)
                            {
                                strDate = strDate + " and jr_authid1 <> ''";
                            }
                        }

                        if ((req.AmountFrom != 0) && (req.AmountTo == 0))
                        {
                            strbal = " having isnull(abs(sum(case jr_debitflag when 'D' then jr_amount else 0 end)),0) >= " + req.AmountFrom;
                        }
                        else if ((req.AmountFrom == 0) && (req.AmountTo != 0))
                        {
                            strbal = " having isnull(abs(sum(case jr_debitflag when 'D' then jr_amount else 0 end)),0) <= " + req.AmountTo;
                        }
                        else if ((req.AmountFrom != 0) && (req.AmountTo != 0))
                        {
                            strbal = " having isnull(abs(sum(case jr_debitflag when 'D' then jr_amount else 0 end)),0) between " + req.AmountFrom + " and " + req.AmountTo;
                        }



                        if (strbal != "")
                        {
                            strsqlSr = "insert into #tempsrno ";
                            strsqlSr = strsqlSr + " select jr_srno,jr_accyear,jr_dpid,'','J' from Journal";
                            strsqlSr = strsqlSr + " where jr_dpid ='" + gdpid + "'" + strwhere + strDate;
                            strsqlSr = strsqlSr + " group by jr_srno,jr_accyear,jr_dpid " + strbal;
                            objUtility.ExecuteSQL(strsqlSr, ObjCon, ObjTrans);

                            strSrNoJoint = " and srno=jr_srno and accyear=jr_accyear and dpid=jr_dpid ";
                            strTable1 = ",#tempsrno";
                        }

                        strsql = "select distinct jr_srno Sr_No,jr_voucherno Voucher_No,jr_clientcd Acc_Code,a.cm_name Acc_Name,'' Bank_Cd,'' Bank_Name ,jr_amount Amount,jr_dt Date,";
                        strsql = strsql + " jr_eparticular Particular,Journal.mkrid MkrID,Journal.mkrdt MkrDate, Journal.mkrtm MkrTime,jr_authid1 Checker1,jr_authdt1 CheckerDt1,jr_authtm1 CheckerTm1,jr_authid2 Checker2, ";
                        strsql = strsql + " jr_authdt2 CheckerDt2, jr_authtm2 CheckerTm2,jr_status Status,null ClearDt,'' Cheque_No,''MICR,0 Flag,a.cm_chgsscheme Scheme,a.cm_groupcd GroupCode,gr_desc GroupName from Journal,group_master, Client_master a" + strTable1;
                        strsql = strsql + " where jr_clientcd = a.cm_cd and a.cm_groupcd = gr_cd and jr_dpid ='" + gdpid + "'" + strwhere + strDate + strJournal + strSrNoJoint;
                        strsql = strsql + " order by jr_clientcd";

                        Dt = objUtility.OpenDataTable(strsql, ObjCon, ObjTrans);

                        if (Dt.Rows.Count > 0)
                        {
                            ObjAudit.Journal = Dt;
                        }
                        else
                        {
                            ObjAudit.Journal = "";
                        }
                    }
                    if (req.AccountType.Debit_Note)
                    {
                        if (req.DateType == 0)
                        {
                            strDate = " and nt_dt between '" + strFromdate + "' and '" + strTodate + "'";
                        }
                        else
                        {
                            strDate = " and Notes.mkrdt between '" + strFromdate + "' and '" + strTodate + "'";
                        }
                        if (!string.IsNullOrEmpty(Strings.Trim(strStatus)))
                        {
                            strDate = strDate + " and nt_status ='" + strStatus + "'";
                            if (req.Status == 2)
                            {
                                strDate = strDate + " and nt_authid1 <> ''";
                            }
                        }
                        strbal = "";
                        if ((req.AmountFrom != 0) && (req.AmountTo == 0))
                        {
                            strbal = " having isnull(abs(sum(nt_amount)),0)>=" + req.AmountFrom;
                        }
                        else if ((req.AmountFrom == 0) && (req.AmountTo != 0))
                        {
                            strbal = " having isnull(abs(sum(nt_amount)),0)<=" + req.AmountTo;
                        }
                        else if ((req.AmountFrom != 0) && (req.AmountTo != 0))
                        {
                            strbal = " having isnull(abs(sum(nt_amount)),0) between " + req.AmountFrom + " and " + req.AmountTo;
                        }

                        strSrNoJoint = "";
                        strsqlSr = "";
                        if (strbal != "")
                        {
                            strsqlSr = "insert into #tempsrno ";
                            strsqlSr = strsqlSr + " select nt_srno,nt_accyear,nt_dpid,nt_debitflag,'D' from Notes";
                            strsqlSr = strsqlSr + " where nt_dpid ='" + gdpid + "'" + strwhere + strDate;
                            strsqlSr = strsqlSr + " group by nt_srno,nt_accyear,nt_dpid,nt_debitflag " + strbal;
                            objUtility.ExecuteSQL(strsqlSr, ObjCon, ObjTrans);

                            strSrNoJoint = " and srno=nt_srno and accyear=nt_accyear and dpid=nt_dpid ";
                            strTable1 = ",#tempsrno";
                        }

                        strsql = "";
                        strsql = strsql + " select distinct nt_srno Sr_No,'' Voucher_No,nt_clientcd Acc_Code,a.cm_name Acc_Name,nt_cclientcd Bank_Cd, b.cm_name Bank_Name,nt_amount Amount, nt_dt Date,nt_particular Particular,Notes.mkrid MkrID,Notes.mkrdt MkrDate, Notes.mkrtm MkrTime,";
                        strsql = strsql + " nt_authid1 Checker1,nt_authdt1 CheckerDt1, nt_authtm1 CheckerTm1, nt_authid2 Checker2,nt_authdt2 CheckerDt2, nt_authtm2 CheckerTm2, nt_status Status,null ClearDt,'' Cheque_No,''MICR,1 Flag,a.cm_chgsscheme Scheme,a.cm_groupcd GroupCode,gr_desc GroupName from Notes,Group_Master,Client_master a, Client_master b " + strTable1;
                        strsql = strsql + " where nt_clientcd = a.cm_cd and nt_cclientcd = b.cm_cd and a.cm_groupcd = gr_cd and nt_dpid ='" + gdpid + "' and nt_debitflag = 'D'" + strwhere + strDate + strDebit + strSrNoJoint;
                        strsql = strsql + " order by nt_clientcd";

                        Dt = objUtility.OpenDataTable(strsql, ObjCon, ObjTrans);

                        if (Dt.Rows.Count > 0)
                        {
                            ObjAudit.Debit_Note = Dt;
                        }
                        else
                        {
                            ObjAudit.Debit_Note = "";
                        }
                    }
                    if (req.AccountType.Credit_Note)
                    {
                        if (req.DateType == 0)
                        {
                            strDate = " and nt_dt between '" + strFromdate + "' and '" + strTodate + "'";
                        }
                        else
                        {
                            strDate = " and Notes.mkrdt between '" + strFromdate + "' and '" + strTodate + "'";
                        }
                        if (!string.IsNullOrEmpty(Strings.Trim(strStatus)))
                        {
                            strDate = strDate + " and nt_status ='" + strStatus + "'";
                            if (req.Status == 2)
                            {
                                strDate = strDate + " and nt_authid1 <> ''";
                            }
                        }
                        strbal = "";
                        if ((req.AmountFrom != 0) && (req.AmountTo == 0))
                        {
                            strbal = " having isnull(abs(sum(nt_amount)),0)>=" + req.AmountFrom;
                        }
                        else if ((req.AmountFrom == 0) && (req.AmountTo != 0))
                        {
                            strbal = " having isnull(abs(sum(nt_amount)),0)<=" + req.AmountTo;
                        }
                        else if ((req.AmountFrom != 0) && (req.AmountTo != 0))
                        {
                            strbal = " having isnull(abs(sum(nt_amount)),0) between " + req.AmountFrom + " and " + req.AmountTo;
                        }

                        strSrNoJoint = "";
                        strsqlSr = "";
                        if (strbal != "")
                        {
                            strsqlSr = "insert into #tempsrno ";
                            strsqlSr = strsqlSr + " select nt_srno,nt_accyear,nt_dpid,nt_debitflag,'C' from Notes";
                            strsqlSr = strsqlSr + " where nt_dpid ='" + gdpid + "'" + strwhere + strDate;
                            strsqlSr = strsqlSr + " group by nt_srno,nt_accyear,nt_dpid,nt_debitflag " + strbal;
                            objUtility.ExecuteSQL(strsqlSr, ObjCon, ObjTrans);

                            strSrNoJoint = " and srno=nt_srno and accyear=nt_accyear and dpid=nt_dpid ";
                            strTable1 = ",#tempsrno";
                        }

                        strsql = "";
                        strsql = strsql + " select distinct nt_srno Sr_No,'' Voucher_No,nt_clientcd Acc_Code,a.cm_name Acc_Name,nt_cclientcd Bank_Cd, b.cm_name Bank_Name,nt_amount Amount, nt_dt Date,nt_particular Particular,Notes.mkrid MkrID,Notes.mkrdt MkrDate, Notes.mkrtm MkrTime, ";
                        strsql = strsql + " nt_authid1 Checker1, nt_authdt1 CheckerDt1, nt_authtm1 CheckerTm1, nt_authid2 Checker2,nt_authdt2 CheckerDt2, nt_authtm2 CheckerTm2, nt_status Status,null ClearDt,'' Cheque_No,''MICR,2 Flag, a.cm_chgsscheme Scheme,a.cm_groupcd GroupCode,gr_desc GroupName from Notes,Group_master,Client_master a, Client_master b " + strTable1;
                        strsql = strsql + " where nt_clientcd = a.cm_cd and nt_cclientcd = b.cm_cd and a.cm_groupcd = gr_cd and nt_dpid ='" + gdpid + "' and nt_debitflag = 'C'" + strwhere + strDate + strCredit + strSrNoJoint;
                        strsql = strsql + " order by nt_clientcd";

                        Dt = objUtility.OpenDataTable(strsql, ObjCon, ObjTrans);

                        if (Dt.Rows.Count > 0)
                        {
                            ObjAudit.Credit_Note = Dt;
                        }
                        else
                        {
                            ObjAudit.Credit_Note = "";
                        }
                    }
                    if (req.AccountType.Receipts)
                    {
                        if (req.DateType == 0)
                        {
                            strDate = " and rc_receiptdt between '" + strFromdate + "' and '" + strTodate + "'";
                        }
                        else
                        {
                            strDate = " and Receipts.mkrdt between '" + strFromdate + "' and '" + strTodate + "'";
                        }
                        if (!string.IsNullOrEmpty(Strings.Trim(strStatus)))
                        {
                            strDate = strDate + " and rc_status ='" + strStatus + "'";
                            if (req.Status == 2)
                            {
                                strDate = strDate + " and rc_authid1 <> ''";
                            }
                        }
                        strbal = "";
                        if ((req.AmountFrom != 0) && (req.AmountTo == 0))
                        {
                            strbal = " having isnull(abs(sum(rc_amount)),0)>=" + req.AmountFrom;
                        }
                        else if ((req.AmountFrom == 0) && (req.AmountTo != 0))
                        {
                            strbal = " having isnull(abs(sum(rc_amount)),0)<=" + req.AmountTo;
                        }
                        else if ((req.AmountFrom != 0) && (req.AmountTo != 0))
                        {
                            strbal = " having isnull(abs(sum(rc_amount)),0) between " + req.AmountFrom + " and " + req.AmountTo;
                        }

                        strSrNoJoint = "";
                        strsqlSr = "";
                        if (strbal != "")
                        {
                            strsqlSr = "insert into #tempsrno ";
                            strsqlSr = strsqlSr + " select rc_srno,rc_accyear,rc_dpid,rc_debitflag,'R' from Receipts";
                            strsqlSr = strsqlSr + " where rc_dpid ='" + gdpid + "'" + strwhere + strDate;
                            strsqlSr = strsqlSr + " group by rc_srno,rc_accyear,rc_dpid,rc_debitflag " + strbal;
                            objUtility.ExecuteSQL(strsqlSr, ObjCon, ObjTrans);

                            strSrNoJoint = " and srno=rc_srno and accyear=rc_accyear and dpid=rc_dpid ";
                            strTable1 = ",#tempsrno";
                        }

                        strsql = "";
                        strsql = strsql + " select distinct rc_srno Sr_No,rc_voucherno Voucher_No, rc_clientcd Acc_Code,a.cm_name Acc_Name,rc_bankclientcd Bank_Cd,b.cm_name Bank_Name,rc_amount Amount,rc_receiptdt Date,rc_particular Particular,";
                        strsql = strsql + " Receipts.mkrid MkrID,Receipts.mkrdt MkrDate,Receipts.mkrtm MkrTime,rc_authid1 Checker1,rc_authdt1 CheckerDt1,rc_authtm1 CheckerTm1,rc_authid2 Checker2,rc_authdt2 CheckerDt2,rc_authtm2 CheckerTm2,rc_status Status,rc_cleareddt ClearDt,rc_chequeno Cheque_No,rc_micr MICR,3 Flag, a.cm_chgsscheme Scheme,a.cm_groupcd GroupCode,gr_desc GroupName ";
                        strsql = strsql + " from Receipts,Group_master , Client_master a, Client_master b " + strTable1;
                        strsql = strsql + " where rc_clientcd = a.cm_cd and rc_bankclientcd = b.cm_cd and a.cm_groupcd = gr_cd ";
                        strsql = strsql + " and rc_dpid = '" + gdpid + "' and rc_debitflag = 'C' " + strwhere + strDate + strReceipt + strSrNoJoint;
                        strsql = strsql + " order by rc_clientcd";

                        Dt = objUtility.OpenDataTable(strsql, ObjCon, ObjTrans);

                        if (Dt.Rows.Count > 0)
                        {
                            ObjAudit.Receipts = Dt;
                        }
                        else
                        {
                            ObjAudit.Receipts = "";
                        }
                    }
                    if (req.AccountType.Payment)
                    {
                        if (req.DateType == 0)
                        {
                            strDate = " and rc_receiptdt between '" + strFromdate + "' and '" + strTodate + "'";
                        }
                        else
                        {
                            strDate = " and Receipts.mkrdt between '" + strFromdate + "' and '" + strTodate + "'";
                        }
                        if (!string.IsNullOrEmpty(Strings.Trim(strStatus)))
                        {
                            strDate = strDate + " and rc_status ='" + strStatus + "'";
                            if (req.Status == 2)
                            {
                                strDate = strDate + " and rc_authid1 <> ''";
                            }
                        }
                        strbal = "";
                        if ((req.AmountFrom != 0) && (req.AmountTo == 0))
                        {
                            strbal = " having isnull(abs(sum(rc_amount)),0)>=" + req.AmountFrom;
                        }
                        else if ((req.AmountFrom == 0) && (req.AmountTo != 0))
                        {
                            strbal = " having isnull(abs(sum(rc_amount)),0)<=" + req.AmountTo;
                        }
                        else if ((req.AmountFrom != 0) && (req.AmountTo != 0))
                        {
                            strbal = " having isnull(abs(sum(rc_amount)),0) between " + req.AmountFrom + " and " + req.AmountTo;
                        }

                        strSrNoJoint = "";
                        strsqlSr = "";
                        if (strbal != "")
                        {
                            strsqlSr = "insert into #tempsrno ";
                            strsqlSr = strsqlSr + " select rc_srno,rc_accyear,rc_dpid,rc_debitflag,'P' from Receipts";
                            strsqlSr = strsqlSr + " where rc_dpid ='" + gdpid + "'" + strwhere + strDate;
                            strsqlSr = strsqlSr + " group by rc_srno,rc_accyear,rc_dpid,rc_debitflag " + strbal;
                            objUtility.ExecuteSQL(strsqlSr, ObjCon, ObjTrans);

                            strSrNoJoint = " and srno=rc_srno and accyear=rc_accyear and dpid=rc_dpid ";
                            strTable1 = ",#tempsrno";
                        }

                        strsql = "";
                        strsql = strsql + " select distinct rc_srno Sr_No,rc_voucherno Voucher_No, rc_clientcd Acc_Code,a.cm_name Acc_Name,rc_bankclientcd Bank_Cd,b.cm_name Bank_Name,rc_amount Amount,rc_receiptdt Date,rc_particular Particular,";
                        strsql = strsql + " Receipts.mkrid MkrID,Receipts.mkrdt MkrDate,Receipts.mkrtm MkrTime,rc_authid1 Checker1,rc_authdt1 CheckerDt1,rc_authtm1 CheckerTm1,rc_authid2 Checker2,rc_authdt2 CheckerDt2,rc_authtm2 CheckerTm2,rc_status Status,rc_cleareddt ClearDt,rc_chequeno Cheque_No,rc_micr MICR,4 Flag,a.cm_chgsscheme Scheme,a.cm_groupcd GroupCode,gr_desc GroupName ";
                        strsql = strsql + " from Receipts,Group_master , Client_master a, Client_master b " + strTable1;
                        strsql = strsql + " where rc_clientcd = a.cm_cd and rc_bankclientcd = b.cm_cd and a.cm_groupcd = gr_cd and rc_dpid = '" + gdpid + "' and rc_debitflag = 'D' " + strwhere + strDate + strPayment + strSrNoJoint;
                        strsql = strsql + " order by rc_clientcd";

                        Dt = objUtility.OpenDataTable(strsql, ObjCon, ObjTrans);

                        if (Dt.Rows.Count > 0)
                        {
                            ObjAudit.Payment = Dt;
                        }
                        else
                        {
                            ObjAudit.Payment = "";
                        }
                    }

                    ListAudit.Add(ObjAudit);
                    return ListAudit;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region Audit Functions
        public void fnAuditTempTable(SqlConnection Con, SqlTransaction Trans)
        {
            string strTempSQl = "select object_id('tempdb..#tempSrno')";
            DataTable rstemp = objUtility.OpenDataTable(strTempSQl, Con, Trans);
            if (rstemp.Rows[0][0] == null)
            {
                objUtility.ExecuteSQL("drop table #tempSrno", Con, Trans);
            }

            strTempSQl = "create table #tempSrno(";
            strTempSQl = strTempSQl + " srno varchar(10),";
            strTempSQl = strTempSQl + " accyear varchar(10),";
            strTempSQl = strTempSQl + " dpid char(12),";
            strTempSQl = strTempSQl + " debitflag char(1),";
            strTempSQl = strTempSQl + " flag char(1)";
            strTempSQl = strTempSQl + ")";

            objUtility.ExecuteSQL(strTempSQl, Con, Trans);

            strTempSQl = "";
        }
        #endregion
        public dynamic BranchSilpIssue(string BranchCd)
        {
            try
            {
                strsql = "select bm_branchcd 'Branch Code'," +
                    "bm_branchname 'Branch Name'," +
                    "im_desc 'Instrument Name'," +
                    "chm_chqno 'From'," +
                    "chm_tono 'To'," +
                    "chm_booksize 'Book Size'," +
                    "chm_issuedate 'Issue Date'," +
                    "chm_refno 'Ref No.'," +
                    "chm_refdate 'Ref Date'," +
                    "im_instcd 'Inst No' " +
                    "from Chequemaster, Branch_master, Instrument_master " +
                    "where bm_branchcd = chm_branchcd and chm_instcd = im_instcd and chm_branchcd <> '000000' and chm_chqno = chm_fromno";
                if (BranchCd != null)
                {
                    strsql += " and chm_branchcd = '" + BranchCd + "'";
                }
                DataTable Dt = objUtility.OpenDataTable(strsql);
                if (Dt.Rows.Count > 0)
                {
                    return Dt;
                }
                else
                {
                    return "Record Not Found";
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic GroupSilpIssue(string GroupCd)
        {
            try
            {
                DataTable Dt;
                strsql = "select gr_cd 'Code',gr_desc 'Name', Case rtrim(ltrim(gr_freezeyn)) when 'N' then 'Active' when 'Y' then 'Inactive' else '' end 'Status' from Group_master ";
                if (GroupCd != null)
                {
                    strsql += "where gr_cd = '" + GroupCd + "'";
                }

                Dt = objUtility.OpenDataTable(strsql);

                if (Dt.Rows.Count > 0)
                {
                    return Dt;
                }
                return "Branch Details Not Found";
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic FamilySilpIssue(string FamilyCd)
        {
            try
            {
                DataTable Dt;
                strsql = "select fm_cd 'Code', fm_desc 'Name', Case rtrim(ltrim(fm_freezeyn)) when 'N' then 'Active' when 'Y' then 'Inactive' else '' end  'Status' from Family_master ";
                if (FamilyCd != null)
                {
                    strsql += "where fm_cd = '" + FamilyCd + "'";
                }

                Dt = objUtility.OpenDataTable(strsql);

                if (Dt.Rows.Count > 0)
                {
                    return Dt;
                }
                return "Family Details Not Found";
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic POASlipIssue(string POA_Id, string SlipNo)
        {
            try
            {
                strsql = "select cpm_poaid 'Client Code'," +
                    "cpm_firstname 'Client Name'," +
                    "im_desc 'Instrument Name'," +
                    "chm_chqno 'From'," +
                    "chm_chqno +chm_booksize-1 'To'," +
                    "chm_issuedate 'Issue Date'," +
                    "chm_refno 'Ref No.'," +
                    "chm_refdate 'Ref Date'," +
                    "im_instcd 'Inst No'," +
                    "Case Rtrim(isnull(chm_allow,'')) when 'E' then 'Exported' when 'S' then 'Success' when 'Y' Then 'Pending' else 'Old' end 'Status'," +
                    "chm_Batch_No 'Bacth No'," +
                    "chm_Reference 'CDSL Ref' " +
                    "from Chequemaster, Corporate_poa_master, Instrument_master " +
                    "where cpm_poaid = chm_cmcd and chm_instcd=im_instcd and chm_status = 'P' and left(isNull(chm_remarks,''),3) = 'POA'";
                if (POA_Id != null)
                {
                    strsql += " and chm_cmcd = '" + POA_Id + "'";
                }
                if (SlipNo != null)
                {
                    strsql += " and chm_chqno = '" + SlipNo + "'";
                }

                DataTable Dt = objUtility.OpenDataTable(strsql);
                if (Dt.Rows.Count > 0)
                {
                    return Dt;
                }
                else
                {
                    return "Record Not Found";
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic ClientSlipIssue(string ClientCd, string SlipNo)
        {
            try
            {
                strsql = "select cm_cd 'Client Code'," +
                    "cm_name 'Client Name'," +
                    "im_desc 'Instrument Name'," +
                    "chm_chqno 'From'," +
                    "chm_chqno +chm_booksize-1 'To'," +
                    "chm_issuedate 'Issue Date'," +
                    "chm_refno 'Ref No.'," +
                    "chm_refdate 'Ref Date'," +
                    "im_instcd 'Inst No'," +
                    "Case Rtrim(isnull(chm_allow,'')) when 'E' then 'Exported' when 'S' then 'Success' when 'Y' Then 'Pending' else 'Old' end 'Status'," +
                    "chm_Batch_No 'Bacth No'," +
                    "chm_Reference 'CDSL Ref' " +
                    "from Chequemaster, Client_master, Instrument_master " +
                    "where cm_cd = chm_cmcd and chm_instcd = im_instcd and chm_status = 'I' and left(isNull(chm_remarks,''),3) <> 'POA'";
                if (ClientCd != null)
                {
                    strsql += " and chm_cmcd = '" + ClientCd + "'";
                }
                if (SlipNo != null)
                {
                    strsql += " and chm_chqno = '" + SlipNo + "'";
                }

                DataTable Dt = objUtility.OpenDataTable(strsql);
                if (Dt.Rows.Count > 0)
                {
                    return Dt;
                }
                else
                {
                    return "Record Not Found";
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic GetInterDepositoryClientCd(string InstrumentType, string InternalRefNo)
        {
            try
            {
                strsql = "select chm_cmcd 'Client Code',cm_name 'Client Name' from Chequemaster,Client_master where chm_cmcd = cm_cd and " + InternalRefNo + " between chm_chqno and chm_chqno + chm_booksize -1 and chm_instcd = '" + InstrumentType + "'";
                DataTable Dt = objUtility.OpenDataTable(strsql);
                return Dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic GetBranchName(string BranchCd)
        {
            try
            {
                strsql = "select bm_branchcd,bm_branchname from Branch_master";
                if (BranchCd != null)
                {
                    strsql += " where bm_branchcd = '" + BranchCd + "'";
                }
                DataTable Dt = objUtility.OpenDataTable(strsql);
                if (Dt.Rows.Count == 0)
                {
                    return "Record Not Found.";
                }
                return Dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic GetFamilyName(string FamilyCd)
        {
            try
            {
                strsql = "select fm_cd FamilyCode, fm_desc FamilyName from Family_master";
                if (FamilyCd != null)
                {
                    strsql += " where fm_cd = '" + FamilyCd + "'";
                }
                DataTable Dt = objUtility.OpenDataTable(strsql);
                if (Dt.Rows.Count == 0)
                {
                    return "Record Not Found.";
                }
                return Dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic GetGroupName(string GroupCd)
        {
            try
            {
                strsql = "select gr_cd GroupCode, gr_desc GroupName from Group_master";
                if (GroupCd != null)
                {
                    strsql += " where gr_cd = '" + GroupCd + "'";
                }
                DataTable Dt = objUtility.OpenDataTable(strsql);
                if (Dt.Rows.Count == 0)
                {
                    return "Record Not Found.";
                }
                return Dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic ClientSearch(string ClientCd, string BranchCd)
        {
            try
            {
                DataTable Dt;
                strsql = "select sp_sysvalue from Sysparameter where sp_parmcd = 'cmschedule'";
                string CmSchedule = objUtility.OpenDataTable(strsql).Rows[0][0].ToString().Trim();

                strsql = "select cm_cd,cm_name,cb_panno,cm_lastname,cm_opendate,cm_blsavingcd from Client_master, client_backoffice where cm_cd=cb_cmcd and cm_schedule = '" + CmSchedule + "' and cm_opendate is not null and cm_active ='01'";
                if (BranchCd != null)
                {
                    strsql += " and cm_brboffcode = '" + BranchCd + "'";
                }
                if (ClientCd != null)
                {
                    strsql += " and cm_cd = '" + ClientCd + "'";
                }
                Dt = objUtility.OpenDataTable(strsql);
                if (Dt.Rows.Count == 0)
                {
                    return "Client InActive/Invalid";
                }
                return Dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic GetBOIDName(string BOID)
        {
            try
            {
                strsql = "select cm_name from Client_master where cm_cd = '" + BOID + "'";
                DataTable Dt = objUtility.OpenDataTable(strsql);
                return Dt.Rows[0][0].ToString().Trim();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic GetDPName(string ClientCd)
        {
            try
            {
                strsql = "select bp_name from Bpmaster where bp_id = '" + ClientCd.Substring(3, 5) + "'";
                DataTable Dt = objUtility.OpenDataTable(strsql);
                return Dt.Rows[0][0].ToString().Trim();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic GetISINName(string ISINCode)
        {
            try
            {
                strsql = "select sc_company_name as 'Company Name',sc_isincode as 'ISIN',sc_isinname as 'ISIN Name',ss_description as 'Sec. Type',st_description as 'Sec. Status' from Security ,Security_status ,Security_type where sc_security_status=ss_code and sc_security_type=st_code ";
                if (ISINCode != null)
                {
                    strsql += "and sc_isincode like '%" + ISINCode + "%'";
                }
                DataTable Dt = objUtility.OpenDataTable(strsql);
                return Dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic GetSegment(string Segment)
        {
            try
            {
                strsql = "select cs_code,cs_desc from Clientsub_master where cs_module ='CS26'";
                if (Segment != null)
                {
                    strsql += "and cs_code = '" + Segment + "'";
                }
                DataTable Dt = objUtility.OpenDataTable(strsql);
                return Dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic GetExchange(string Exch)
        {
            try
            {
                strsql = "select " +
                         "a.bp_id 'STOCK EXCHENGE', " +
                         "a.bp_name 'NAME', " +
                         "a.bp_assd_cc_cmid 'CC ID', " +
                         "p.bp_name 'NAME' " +
                         "from Bpmaster a, Bpmaster p " +
                         "where a.bp_id = p.bp_id and a.bp_role = '02' and p.bp_role = '02'";
                if (Exch != null)
                {
                    strsql += " a.bp_id = '" + Exch + "'";
                }
                DataTable Dt = objUtility.OpenDataTable(strsql);
                return Dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic GetUCCDetails(string BOID)
        {
            try
            {
                strsql = "select " +
                     "Case cud_Exchnge when '11' then 'BSE' when '12' then 'NSE' when '23' then 'MCX' when '22' then 'NCDEX' else cud_Exchnge end as 'Exchange', " +
                     "Case cud_segment when '01' then 'Cash' when '02' then 'F&O' when '03' then 'Currency' when '04' then 'SLB' when '05' then 'Commodity' when '06' then 'Debt' else cud_segment end as 'Segment', " +
                     "cud_UCC as 'UCC', " +
                     "cud_cmid as 'CMID', " +
                     "cud_tmid as 'TMID' " +
                     "from Client_UCC_Details";
                if (BOID != null)
                {
                    strsql += " where cud_boid = '" + BOID + "'";
                }
                DataTable Dt = objUtility.OpenDataTable(strsql);
                return Dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic FromSettNoSearch(string ExecDate)
        {
            try
            {
                strsql = "select cc_id 'CC id',cc_settle_no 'Settlement No',cc_mkt_type 'Market Type',mt_description 'Market Desc',cc_settle_periodfrom 'Period From',cc_settle_periodto 'Period To',cc_payout_dt 'Pay Out Date' from Cc_calender , Market_type where mt_code = cc_mkt_type and isnumeric(mt_code) = 1  and Left(cc_settle_no,2) = mt_exchangeid ";
                if (ExecDate != null)
                {
                    strsql += " and '" + ExecDate + "' between  cc_settle_periodfrom and cc_payout_dt";
                }
                DataTable Dt = objUtility.OpenDataTable(strsql);
                return Dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic InstrumentTypeSearch(string InstCd)
        {
            try
            {
                strsql = "select im_instcd,im_desc from Instrument_master ";
                if (InstCd != null)
                {
                    strsql += " where im_instcd = '" + InstCd + "'";
                }
                else
                {
                    strsql += " where im_instcd in('3','11','103','111')";
                }
                DataTable Dt = objUtility.OpenDataTable(strsql);
                return Dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic ReceiveModeSearch(string idx)
        {
            try
            {
                string[,] List = { { "Receive Code : S", "Receive Mode : Slip" }, { "Receive Code : F", "Receive Mode : Fax" }, { "Receive Code : T", "Receive Mode : Telephone" }, { "Receive Code : O", "Receive Mode : Other" }, { "Receive Code : E", "Receive Mode : Email" }, { "Receive Code : V", "Receive Mode : Verbal" }, { "Receive Code : P", "Receive Mode : POA" }, { "Receive Code : C", "Receive Mode : Courior" }, { "Receive Code : H", "Receive Mode : Hand-Delivery" }, { "Receive Code : D", "Receive Mode : Digital" } };
                if (idx != null)
                {
                    if (idx == "0")
                    {
                        return "Slip";
                    }
                    else if (idx == "1")
                    {
                        return "Fax";
                    }
                    else if (idx == "2")
                    {
                        return "Telephone";
                    }
                    else if (idx == "3")
                    {
                        return "Other";
                    }
                    else if (idx == "4")
                    {
                        return "Email";
                    }
                    else if (idx == "5")
                    {
                        return "Verbal";
                    }
                    else if (idx == "6")
                    {
                        return "POA";
                    }
                    else if (idx == "7")
                    {
                        return "Courior";
                    }
                    else if (idx == "8")
                    {
                        return "Hand-Delivery";
                    }
                    else if (idx == "9")
                    {
                        return "Digital";
                    }
                    else
                    {
                        return "Slip";
                    }
                }
                else
                {
                    return List;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic PaymentModeSearch(string PaymentCd)
        {
            try
            {
                if (PaymentCd != null)
                {
                    if (PaymentCd == "1")
                    {
                        return "Cheque Payment";
                    }
                    else if (PaymentCd == "2")
                    {
                        return "Electronic Payment";
                    }
                    else if (PaymentCd == "3")
                    {
                        return "Cash";
                    }
                    return "";
                }
                else
                {
                    string[,] List = { { "Payment Code : 1", "Payment Mode : Cheque Payment" }, { "Payment Code : 2", "Payment Mode : Electronic Payment" }, { "Payment Code : 3", "Payment Mode : Cash" } };
                    return List;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic ReasonSearch(string ReasonCd)
        {
            try
            {
                strsql = "select rt_code,rt_desc from Reasonfortrade";
                if (ReasonCd != null)
                {
                    strsql += " where rt_code = '" + ReasonCd + "'";
                }
                DataTable Dt = objUtility.OpenDataTable(strsql);
                return Dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic PaidBySearch(string PBCode)
        {
            try
            {
                string[,] List = { { "Paid By Code : 0", "Paid By Desc : Dipository_Particepant" }, { "Paid By Code : 1", "Paid By Desc : Client" } };
                if (PBCode == "0")
                {
                    return "Dipository Particepant";
                }
                else if (PBCode == "1")
                {
                    return "Client";
                }
                else
                {
                    return List;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic EarlyPaySearch(string EarlyPayCd)
        {
            try
            {
                if (EarlyPayCd != null)
                {
                    if (EarlyPayCd == "Y")
                    {
                        return "Yes";
                    }
                    else if (EarlyPayCd == "N")
                    {
                        return "No";
                    }
                    else
                    {
                        return "";
                    }
                }
                else
                {
                    string[,] List = { { "Early Payin Code : Y", "Early Payin Desc : Yes" }, { "Early Payin Code : N", "Early Payin Desc : No" } };
                    return List;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic EntryBySearch(string EntryByCd)
        {
            try
            {
                string[,] List = { { "Entry By Code : TM", "Entry By Desc : TM" }, { "Entry By Code : CP", "Entry By Desc : CP" } };
                if (EntryByCd == null)
                {
                    return List;
                }
                else if (EntryByCd == "TM")
                {
                    return "TM";
                }
                else if (EntryByCd == "CP")
                {
                    return "CP";
                }
                else
                {
                    return "";
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic GetBOID_and_DPName(string ClientCd)
        {
            try
            {
                strsql = "select cm_name,bp_name from Client_master,Bpmaster where cm_cd = '" + ClientCd.Trim() + "' and bp_id = substring(cm_cd,4,5)";
                DataTable Dt = objUtility.OpenDataTable(strsql);
                return Dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public dynamic GetMasterCmb(string Type, string LoginAccess)
        {
            try
            {
                string CmSchedule = GetSysParm("CMSCHEDULE");
                if (Type.Trim().ToUpper() != "CLIENT" && Type.Trim().ToUpper() != "BRANCH" && Type.Trim().ToUpper() != "FAMILY" && Type.Trim().ToUpper() != "GROUP" && Type.Trim().ToUpper() != "SCHEME")
                {
                    return "Invalid Type.";
                }
                if (Type.Trim().ToUpper() == "CLIENT")
                {
                    strsql = "select cm_cd Value, cm_cd + '~' + rtrim(cm_blsavingcd) + '~' + cm_name Name from Client_master with (noLock) " +
                        "where cm_schedule = " + CmSchedule + " and cm_freezeyn <> 'Y' and cm_productcd = '1' and cm_active = '01' " + LoginAccess +
                        " order by cm_name";
                }
                if (Type.Trim().ToUpper() == "BRANCH")
                {
                    strsql = "select distinct bm_branchcd Value, bm_branchcd + '~' + bm_branchname Name " +
                        "from branch_master with (noLock),client_master  with (noLock) where bm_branchcd = cm_brboffcode and bm_branchcd <>'' " + LoginAccess;
                }
                if (Type.Trim().ToUpper() == "FAMILY")
                {
                    strsql = "select distinct fm_cd Value, fm_cd + '~' + fm_desc Name " +
                        "from Family_master  with (noLock), Client_master with (noLock), Branch_master with (nolock) where fm_cd <> '' and bm_branchcd = cm_brboffcode and cm_familycd = fm_cd " + LoginAccess;
                }
                if (Type.Trim().ToUpper() == "GROUP")
                {
                    strsql = "select distinct gr_cd Value, gr_cd + '~' + gr_desc Name " +
                        "from Group_master  with (noLock), Client_master with (noLock), Branch_master with (nolock) where bm_branchcd = cm_brboffcode and cm_groupcd = gr_cd " + LoginAccess;
                }
                if (Type.Trim().ToUpper() == "SCHEME")
                {
                    strsql = "select distinct rtrim(cd_scheme) Value, rtrim(cd_scheme) +'~'+ cd_scheme Name " +
                        "from ChargesDetail  with (noLock) where  cd_scheme <>'' " +
                        "and cd_scheme in ( select distinct cm_chgsscheme from Client_master with (noLock), Branch_master  with (noLock) Where cm_brboffcode = bm_branchcd " + LoginAccess + ")";
                }

                DataTable Dt = objUtility.OpenDataTable(strsql);

                if (Dt.Rows.Count > 0)
                {
                    return Dt;
                }
                else
                {
                    return "Record Not Found.";
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetHoldingDates()
        {
            try
            {
                strsql = "SELECT CASE LEN(SUBSTRING(TABLE_NAME,9,8)) WHEN 8 THEN CONVERT(VARCHAR, SUBSTRING(TABLE_NAME,9,8), 112) ELSE '' END AS name " +
                    "FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE 'Holding_%' AND LEN(SUBSTRING(TABLE_NAME,9,8)) = 8";
                DataTable Dt = objUtility.OpenDataTable(strsql);
                if (Dt.Rows.Count > 0)
                {
                    return Dt;
                }
                return null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetStatusCode(string TrxType)
        {
            try
            {
                if (TrxType == null)
                {
                    return "Transaction Type Cannot Be Left Blank.";
                }
                strsql = "select sx_code StatusCode,sx_description StatusName from Statusof_trx where sx_trxtype = '" + TrxType + "'";
                DataTable Dt = objUtility.OpenDataTable(strsql);
                if (Dt.Rows.Count > 0)
                {
                    return Dt;
                }
                return "Record Not Found.";
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic TransactionType()
        {
            try
            {
                List<JObject> lstObj = new List<JObject>();
                JObject jObject1 = new JObject();
                jObject1["Code"] = "925";
                jObject1["Desc"] = "InterDepository";
                lstObj.Add(jObject1);
                JObject jObject2 = new JObject();
                jObject2["Code"] = "906";
                jObject2["Desc"] = "OnMarket";
                lstObj.Add(jObject2);
                JObject jObject3 = new JObject();
                jObject3["Code"] = "904";
                jObject3["Desc"] = "OffMarket";
                lstObj.Add(jObject3);
                JObject jObject4 = new JObject();
                jObject4["Code"] = "903";
                jObject4["Desc"] = "EarlyPayIn";
                lstObj.Add(jObject4);
                JObject jObject5 = new JObject();
                jObject5["Code"] = "901";
                jObject5["Desc"] = "Demat";
                lstObj.Add(jObject5);
                return lstObj;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic ISD_Codes()
        {
            try
            {
                strsql = "select Co_ISDCode 'Code', Co_Name 'Desc' from Country_Master";
                DataTable dt = objUtility.OpenDataTable(strsql);
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetSubMaster(string ModuleDesc)
        {
            try
            {
                string strModuleCd = "";
                switch (ModuleDesc)
                {
                    case "RELATIONSHIP":
                        strModuleCd = "CS19";
                        break;
                    case "ANNUALINCOME":
                        strModuleCd = "CS03";
                        break;
                    case "BANKCURRENCY":
                        strModuleCd = "CS05";
                        break;
                }

                strsql = "select Rtrim(cs_code) 'Code', cs_desc 'Desc' from Clientsub_master where cs_module = '" + strModuleCd + "'";
                DataTable dt = objUtility.OpenDataTable(strsql);
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetClientModification(string BOID)
        {
            try
            {
                string strSql = "";
                Dictionary<string, object> Properties = new Dictionary<string, object>();
                List<Dictionary<string, object>> lstRes = new List<Dictionary<string, object>>();
                DataTable dtTemp = new DataTable();
                string strParm = objUtility.GetSysParmSt("CLMOPTIONS", "");

                strSql = "Select cm_name,cm_middlename,cm_lastname,cm_title,cm_suffix,cm_clienttype,cm_add1,cm_add2,cm_add3,cm_city,cm_state,cm_country,cm_pin,cm_email,cb_BonafideFlag,cm_brboffcode,cm_groupcd,cm_familycd,cm_dateofbirth,cb_fh_panverify,cb_MobileISD,cm_tele1,cb_SmartIndicator,cm_tele2,cb_sechmiddle,cb_sechlastname,cb_sechtitle,cb_sechsuffix,cb_sh_panverify,cb_sechitcircle,cb_MobileISD2,cm_fax,cm_phoneindicator,cm_phoneindicator2,cm_chgsscheme,cm_familycd,cb_fadd1,cb_fadd2,cb_fadd3,cb_fcity,cb_fstate,cb_fcountry,cb_fpin,cb_ftele,cb_ffax,cb_panno,cb_sechpanno,cb_thirdpanno, ";
                strSql += " cb_poaadd3,cb_poacity,cb_poauserfield1,cb_poauserfield2,cb_sexcode,cb_cacharfield,cb_poaadd1,cb_poastate,cb_poaadd2,cm_confirmationwaived,cb_annualincome,cm_occupation,cm_rbirefno,cm_rbiappdate,cb_UID1,cb_UIDVerifyFlag,cb_SecondaryMobileISD,cb_fathername,cb_sechfathername,cb_thirdfathername,cm_email,isNull(cm_chgsscheme,''),cm_familycd,cb_secondaryemail,isNull(cb_bankstate,'') SecHoldEmail, isNull(cb_bankpin,'') ThrHoldEmail, ";
                strSql += " cb_UID2,cb_UIDVerifyFlag2,cb_UID3,cb_UIDVerifyFlag3,cb_bankcity,cb_MobileISD3,cb_bankcountry,cm_sech_name,cm_thih_name,cb_thirdmiddle,cb_thirdlastname,cb_th_panverify,isNull(cm_lastbill,'') cm_lastbill,cm_chgsscheme,cm_familycd,cm_divbankcode,cm_divbankacno,cm_divbankccy,cm_divbranchno,cb_voicemail,cm_productcd,cm_clienttype,cb_nationality ";
                strSql += " from client_master, client_Backoffice Where cm_cd=cb_cmcd and cm_cd = '" + BOID + "' ";
                DataTable dt = objUtility.OpenDataTable(strSql);
                if (dt.Rows.Count > 0)
                {
                    Properties["BOID"] = BOID;
                    Properties["RECEIPT_DATE_TIME"] = "";
                    Properties["REFERENCENO"] = "";
                    if (strParm.Contains("CORRADD"))
                    {
                        Properties["CORRESPONDENCEADDRESS"] = new Address
                        {
                            ADDRESS1 = dt.Rows[0]["cm_add1"].ToString().Trim(),
                            ADDRESS2 = dt.Rows[0]["cm_add2"].ToString().Trim(),
                            ADDRESS3 = dt.Rows[0]["cm_add3"].ToString().Trim(),
                            PIN = dt.Rows[0]["cm_pin"].ToString().Trim(),
                            CITY = dt.Rows[0]["cm_city"].ToString().Trim(),
                            STATE = dt.Rows[0]["cm_state"].ToString().Trim(),
                            COUNTRY = dt.Rows[0]["cm_country"].ToString().Trim()
                        };
                    }

                    if (strParm.Contains("PERMADD"))
                    {
                        Properties["PERMANENTADDRESS"] = new Address
                        {
                            ADDRESS1 = dt.Rows[0]["cb_fadd1"].ToString().Trim(),
                            ADDRESS2 = dt.Rows[0]["cb_fadd2"].ToString().Trim(),
                            ADDRESS3 = dt.Rows[0]["cb_fadd3"].ToString().Trim(),
                            PIN = dt.Rows[0]["cb_fpin"].ToString().Trim(),
                            CITY = dt.Rows[0]["cb_fcity"].ToString().Trim(),
                            STATE = dt.Rows[0]["cb_fstate"].ToString().Trim(),
                            COUNTRY = dt.Rows[0]["cb_fcountry"].ToString().Trim()
                        };
                    }

                    if (strParm.Contains("EMAIL"))
                    {
                        Properties["EMAIL"] = new Email
                        {
                            EMAIL = dt.Rows[0]["cm_email"].ToString().Trim(),
                            SECONDARY_EMAIL = dt.Rows[0]["cb_secondaryemail"].ToString().Trim(),
                            SECONDHOLDER_EMAIL = dt.Rows[0]["SecHoldEmail"].ToString().Trim(),
                            THIRDHOLDER_EMAIL = dt.Rows[0]["ThrHoldEmail"].ToString().Trim()
                        };
                    }

                    if (strParm.Contains("MOBILE"))
                    {
                        Properties["MOBILE"] = new Mobile
                        {
                            PRIMARY_MOBILE_ISD = dt.Rows[0]["cb_MobileISD"].ToString().Trim(),
                            PRIMARY_MOBILE_NO = dt.Rows[0]["cm_tele1"].ToString().Trim(),
                            SMART_REGISTRATION_INDICATOR = dt.Rows[0]["cb_SmartIndicator"].ToString().Trim(),
                            SECONDARY_ISD = dt.Rows[0]["cb_SecondaryMobileISD"].ToString().Trim(),
                            SECONDARY_TEL_NO = dt.Rows[0]["cm_tele2"].ToString().Trim(),
                            SECONDHOLDER_MOBILE_ISD = dt.Rows[0]["cb_MobileISD2"].ToString().Trim(),
                            SECONDHOLDER_MOBILE_NO = dt.Rows[0]["cb_bankcity"].ToString().Trim(),
                            THIRDHOLDER_MOBILE_ISD = dt.Rows[0]["cb_MobileISD3"].ToString().Trim(),
                            THIRDHOLDER_MOBILE_NO = dt.Rows[0]["cb_bankcountry"].ToString().Trim()
                        };
                    }

                    if (strParm.Contains("BANK"))
                    {
                        string strBankAccType;
                        strBankAccType = dt.Rows[0]["cm_divbranchno"].ToString().Trim() switch
                        {
                            "10" => "Saving Account",
                            "11" => "Current Account",
                            "13" => "Cash Credit",
                            _ => "",
                        };
                        if (strBankAccType != "")
                        {
                            strBankAccType += " [" + dt.Rows[0]["cm_divbranchno"].ToString().Trim() + "]";
                        }
                        string strBankName = objUtility.fnFireQueryTradeWeb("Bank_master", "bk_name", "bk_micr", dt.Rows[0]["cm_divbankcode"].ToString().Trim(), true).Trim();
                        if (string.IsNullOrWhiteSpace(strBankName))
                        {
                            strBankName = objUtility.fnFireQueryTradeWeb("Bank_master", "bk_name", "bk_branch", dt.Rows[0]["cb_voicemail"].ToString().Trim(), true).Trim();
                        }
                        Properties["DIVIDENDBANKDETAIL"] = new DividendBankDetail
                        {
                            BANK_CODE = dt.Rows[0]["cm_divbankcode"].ToString().Trim(),
                            BANK_AC_NO = dt.Rows[0]["cm_divbankacno"].ToString().Trim(),
                            BANK_CURRENCY = objUtility.fnFireQueryCross("Clientsub_master", "cs_desc", "cs_module='CS05' and cs_code", dt.Rows[0]["cm_divbankccy"].ToString().Trim(), true) + " [" + dt.Rows[0]["cm_divbankccy"].ToString().Trim() + "]",
                            BANK_IFSCCODE = dt.Rows[0]["cb_voicemail"].ToString().Trim(),
                            BANK_AC_TYPE = strBankAccType,
                            ECS_MANDATE = objUtility.fnFireQueryTradeWeb("Client_Backoffice", "cb_ecs", "cb_cmcd", BOID, true).Trim(),
                            BANK_NAME = strBankName
                        };
                    }

                    if (strParm.Contains("NOMGUR"))
                    {
                        List<NomineeDetails> lstNominee = new List<NomineeDetails>();
                        strsql = "select * from Client_NomineeDetails where cn_Cmcd='" + BOID + "'";
                        dtTemp = objUtility.OpenDataTable(strsql);
                        foreach (DataRow dr in dtTemp.Rows)
                        {
                            NomineeDetails nominee = new NomineeDetails()
                            {
                                PURPOSE_CODE = dr["cn_purposecd"].ToString().Trim(),
                                NOMINEE_SERIAL_NO = dr["cn_nomsrno"].ToString().Trim(),
                                NAME = dr["cn_nomname"].ToString().Trim(),
                                MIDDLE_NAME = dr["cn_nommidnm"].ToString().Trim(),
                                LAST_NAME = dr["cn_nomlastnm"].ToString().Trim(),
                                TITLE = dr["cn_NomTitle"].ToString().Trim(),
                                SUFFIX = dr["cn_NomSuffix"].ToString().Trim(),
                                FATHER_OR_HUSBAND_NAME = dr["cn_FathHusbnm"].ToString().Trim(),
                                ADDRESS1 = dr["cn_NomAdd1"].ToString().Trim(),
                                ADDRESS2 = dr["cn_NomAdd2"].ToString().Trim(),
                                ADDRESS3 = dr["cn_NomAdd3"].ToString().Trim(),
                                PIN = dr["cn_NomPin"].ToString().Trim(),
                                CITY = dr["cn_City"].ToString().Trim(),
                                STATE = dr["cn_State"].ToString().Trim(),
                                COUNTRY = dr["cn_Country"].ToString().Trim(),
                                UID = dr["cn_NomUID"].ToString().Trim(),
                                UID_VERIFY_FLAG = dr["cn_NomUIDVerifyFlag"].ToString().Trim(),
                                PAN_NO = dr["cn_NomPAN"].ToString().Trim(),
                                MOBILE_ISD_CODE = dr["cn_NomMobileISD"].ToString().Trim(),
                                MOBILE_NO = dr["cn_PH1"].ToString().Trim(),
                                RELATIONSHIP = objUtility.fnFireQueryCross("Clientsub_master", "cs_desc", "cs_module='CS19' and cs_code", dr["cn_Relation"].ToString().Trim(), true) + " [" + dr["cn_Relation"].ToString().Trim() + "]",
                                DATE_OF_BIRTH = dr["cn_NomDOB"].ToString().Trim(),
                                RESIDUAL_SECURITIES = dr["cn_ResidualFlag"].ToString().Trim(),
                                SHARE_PERCENTAGE = dr["cn_NomPershare"].ToString().Trim(),
                                EMAIL = dr["cn_NomEmail"].ToString().Trim()
                            };
                            lstNominee.Add(nominee);
                        }
                        Properties["NOMINEEGUARDIAN"] = lstNominee;
                    }

                    if (strParm.Contains("ANUINC"))
                    {
                        Properties["OTHERDETAILS"] = new OtherDetails
                        {
                            ANNUAL_INCOME = objUtility.fnFireQueryCross("Clientsub_master", "cs_desc", "cs_module='CS03' and cs_code", dt.Rows[0]["cb_annualincome"].ToString().Trim(), true) + " [" + dt.Rows[0]["cb_annualincome"].ToString().Trim() + "]"
                        };
                    }

                    lstRes.Add(Properties);
                    return lstRes;
                }
                return null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic ValidateClientModification(CrossClientModification jsonObject)
        {
            try
            {
                DataTable dtChk = new DataTable();
                string strParm = objUtility.GetSysParmSt("CLMOPTIONS", "");
                string strValue = "";
                string strError = "";

                string strBOID = isNull(jsonObject.BOID);
                strsql = "Select cm_cd, cm_active From Client_Master Where cm_cd = '" + strBOID + "'";
                dtChk = objUtility.OpenDataTable(strsql);
                if (dtChk.Rows.Count == 0)
                {
                    return "Invalid Client Code, Not Found in Master" + Environment.NewLine;
                }
                else if (!(Strings.InStr(1, "01,02,04", dtChk.Rows[0]["cm_active"].ToString().Trim()) > 0))
                {
                    return "Client is not active" + Environment.NewLine;
                }

                string strRecDateTime = isNull(jsonObject.RECEIPT_DATE_TIME);
                if (strRecDateTime.Trim() != "")
                {
                    string strDate = isNull(strRecDateTime, 14).Trim();
                    string strTime = Strings.Right(strRecDateTime, 6);
                    strDate = Strings.Left(strDate, 8);
                    if (!CheckDate(strDate, "yyyyMMdd"))
                    {
                        strError += "Invalid Date Format in RECEIPT_DATE_TIME" + Environment.NewLine;
                    }

                    if (strTime.Trim() == "")
                    {
                        strError += "Invalid Time Format in RECEIPT_DATE_TIME" + Environment.NewLine;
                    }
                    if (Conversion.Val(Strings.Left(strTime, 2)) > 23 || Conversion.Val(Strings.Mid(strTime, 3, 2)) > 59 || Conversion.Val(Strings.Right(strTime, 2)) > 59)
                    {
                        strError += "Invalid Time Format in RECEIPT_DATE_TIME" + Environment.NewLine;
                    }
                }

                if (jsonObject.CORRESPONDENCEADDRESS != null && strParm.Contains("CORRADD"))
                {
                    Address corrAddObject = jsonObject.CORRESPONDENCEADDRESS;
                    List<string> lstMissingKeys = GetMissingFields(corrAddObject);

                    if (lstMissingKeys.Count > 0)
                    {
                        strError += "CORRESPONDENCEADDRESS : Following Fields are missing - " + string.Join(", ", lstMissingKeys) + Environment.NewLine;
                    }
                    else
                    {
                        if (isNull(corrAddObject.STATE) == "")
                        {
                            strError += "Invalid/ Blank Correspondance State" + Environment.NewLine;
                        }
                        else
                        {
                            if (!ValidateField("STATE", isNull(corrAddObject.STATE).Trim()))
                            {
                                strError += "Invalid/ Blank Correspondance State" + Environment.NewLine;
                            }
                        }

                        if (isNull(corrAddObject.COUNTRY) == "")
                        {
                            strError += "Invalid/ Blank Correspondance COUNTRY" + Environment.NewLine;
                        }
                        else
                        {
                            if (!ValidateField("COUNTRY", isNull(corrAddObject.COUNTRY).Trim()))
                            {
                                strError += "Invalid/ Blank Correspondance COUNTRY" + Environment.NewLine;
                            }
                        }

                        if (isNull(corrAddObject.CITY).Trim() == "")
                        {
                            strError += "Invalid/ Blank Correspondance City" + Environment.NewLine;
                        }
                        else
                        {
                            if (!ValidateField("CITY", isNull(corrAddObject.CITY).Trim()))
                            {
                                strError += "Invalid/ Blank Correspondance City" + Environment.NewLine;
                            }
                        }

                        if (isNull(corrAddObject.PIN).Trim() == "")
                        {
                            strError += "Invalid/ Blank Correspondance Pin" + Environment.NewLine;
                        }
                        else
                        {
                            if (!ValidateField("PIN", isNull(corrAddObject.PIN).Trim()))
                            {
                                strError += "Invalid/ Blank Correspondance Pin" + Environment.NewLine;
                            }
                        }

                        strValue = isNull(corrAddObject.PIN);
                        if (strValue.Trim() != "")
                        {
                            string strCity = "";

                            strCity = ValidatePinCity(strValue, isNull(corrAddObject.CITY));

                            if (strCity.Trim() != "")
                            {
                                strCity = Strings.Left(strCity, strCity.Length - 1);
                                strError += "For Correspondance Pin Code " + corrAddObject.PIN + " Correspondance City Should Be " + strCity.Trim() + ". Change Correspondance City To " + strCity.Trim() + " (From " + corrAddObject.CITY.Trim() + " At Present)" + Environment.NewLine;
                            }
                        }
                    }
                }

                if (jsonObject.PERMANENTADDRESS != null && strParm.Contains("PERMADD"))
                {
                    Address perAddObject = jsonObject.PERMANENTADDRESS;

                    Address address = new Address();
                    List<string> lstMissingKeys = GetMissingFields(perAddObject);

                    if (lstMissingKeys.Count > 0)
                    {
                        strError += "PERMANENTADDRESS : Following Fields are missing - " + string.Join(", ", lstMissingKeys) + Environment.NewLine;
                    }
                    else
                    {
                        if (isNull(perAddObject.STATE) == "")
                        {
                            strError += "Invalid/ Blank Permanent State" + Environment.NewLine;
                        }
                        else
                        {
                            if (!ValidateField("STATE", isNull(perAddObject.STATE).Trim()))
                            {
                                strError += "Invalid/ Blank Permanent State" + Environment.NewLine;
                            }
                        }

                        if (isNull(perAddObject.COUNTRY) == "")
                        {
                            strError += "Invalid/ Blank Permanent COUNTRY" + Environment.NewLine;
                        }
                        else
                        {
                            if (!ValidateField("COUNTRY", isNull(perAddObject.COUNTRY).Trim()))
                            {
                                strError += "Invalid/ Blank Permanent COUNTRY" + Environment.NewLine;
                            }
                        }

                        if (isNull(perAddObject.CITY).Trim() == "")
                        {
                            strError += "Invalid Permanent City" + Environment.NewLine;
                        }
                        else
                        {
                            if (!ValidateField("CITY", isNull(perAddObject.CITY).Trim()))
                            {
                                strError += "Invalid Permanent City" + Environment.NewLine;
                            }
                        }

                        if (isNull(perAddObject.PIN).Trim() == "")
                        {
                            strError += "Invalid Permanent Pin" + Environment.NewLine;
                        }
                        else
                        {
                            if (!ValidateField("PIN", isNull(perAddObject.PIN).Trim()))
                            {
                                strError += "Invalid Permanent Pin" + Environment.NewLine;
                            }
                        }

                        strValue = isNull(perAddObject.PIN);
                        if (strValue.Trim() != "")
                        {
                            string strCity = "";

                            strCity = ValidatePinCity(strValue, isNull(perAddObject.CITY));

                            if (strCity.Trim() != "")
                            {
                                strCity = Strings.Left(strCity, strCity.Length - 1);
                                strError += "For Permanent Pin Code " + perAddObject.PIN + " Permanent City Should Be " + strCity.Trim() + ". Change Permanent City To " + strCity.Trim() + " (From " + perAddObject.CITY.Trim() + " At Present)" + Environment.NewLine;
                            }
                        }
                    }
                }

                if (jsonObject.MOBILE != null && strParm.Contains("MOBILE"))
                {
                    Mobile mobileObject = jsonObject.MOBILE;

                    List<string> lstMissingKeys = GetMissingFields(mobileObject);

                    if (lstMissingKeys.Count > 0)
                    {
                        strError += "MOBILE : Following Fields are missing - " + string.Join(", ", lstMissingKeys) + Environment.NewLine;
                    }
                    else
                    {
                        if (isNull(mobileObject.PRIMARY_MOBILE_NO).Trim() != "")
                        {
                            if (!FnISDValidate(mobileObject.PRIMARY_MOBILE_ISD))
                            {
                                strError += "Invalid/ Blank primary mobile ISD" + Environment.NewLine;
                            }
                        }

                        if (isNull(mobileObject.SECONDARY_TEL_NO).Trim() != "")
                        {
                            if (!FnISDValidate(mobileObject.SECONDARY_ISD))
                            {
                                strError += "Invalid/ Blank secondary ISD" + Environment.NewLine;
                            }
                        }

                        if (isNull(mobileObject.SECONDHOLDER_MOBILE_NO).Trim() != "")
                        {
                            if (!FnISDValidate(mobileObject.SECONDHOLDER_MOBILE_ISD))
                            {
                                strError += "Invalid/ Blank Second Holder mobile ISD" + Environment.NewLine;
                            }
                        }

                        if (isNull(mobileObject.THIRDHOLDER_MOBILE_NO).Trim() != "")
                        {
                            if (!FnISDValidate(mobileObject.THIRDHOLDER_MOBILE_ISD))
                            {
                                strError += "Invalid/ Blank Third Holder mobile ISD" + Environment.NewLine;
                            }
                        }
                    }
                }

                if (jsonObject.EMAIL != null && strParm.Contains("EMAIL"))
                {
                    Email emailObject = jsonObject.EMAIL;

                    List<string> lstMissingKeys = GetMissingFields(emailObject);

                    if (lstMissingKeys.Count > 0)
                    {
                        strError += "EMAIL : Following Fields are missing - " + string.Join(", ", lstMissingKeys) + Environment.NewLine;
                    }
                    else
                    {
                        if (isNull(emailObject.EMAIL).Trim() != "")
                        {
                            strValue = isNull(emailObject.EMAIL).Trim();
                            if (!IsValidEmail(strValue))
                            {
                                strError += "Invalid Client Email ID" + Environment.NewLine;
                            }
                        }

                        if (isNull(emailObject.SECONDARY_EMAIL).Trim() != "")
                        {
                            strValue = isNull(emailObject.SECONDARY_EMAIL).Trim();
                            if (!IsValidEmail(strValue))
                            {
                                strError += "Invalid Client Secondary Email ID" + Environment.NewLine;
                            }
                        }

                        if (isNull(emailObject.SECONDHOLDER_EMAIL).Trim() != "")
                        {
                            strValue = isNull(emailObject.SECONDHOLDER_EMAIL).Trim();
                            if (!IsValidEmail(strValue))
                            {
                                strError += "Invalid Second Holder Email ID" + Environment.NewLine;
                            }
                        }

                        if (isNull(emailObject.THIRDHOLDER_EMAIL).Trim() != "")
                        {
                            strValue = isNull(emailObject.THIRDHOLDER_EMAIL).Trim();
                            if (!IsValidEmail(strValue))
                            {
                                strError += "Invalid Third Holder Email ID" + Environment.NewLine;
                            }
                        }
                    }
                }

                if (jsonObject.DIVIDENDBANKDETAIL != null && strParm.Contains("BANK"))
                {
                    DividendBankDetail bankObject = jsonObject.DIVIDENDBANKDETAIL;

                    DividendBankDetail bank = new DividendBankDetail();
                    List<string> lstMissingKeys = GetMissingFields(bankObject);

                    if (lstMissingKeys.Count > 0)
                    {
                        strError += "DIVIDENDBANKDETAIL : Following Fields are missing - " + string.Join(", ", lstMissingKeys) + Environment.NewLine;
                    }
                    else if (isNull(bankObject.BANK_CODE).Trim() == "")
                    {
                        strError += "Invalid/ Blank Dividend Bank Code" + Environment.NewLine;
                    }
                    else
                    {
                        strsql = "Select count(0) from Bank_master where bk_micr = '" + isNull(bankObject.BANK_CODE).Trim() + "' and bk_branch = '" + isNull(bankObject.BANK_IFSCCODE).Trim() + "'";
                        strValue = objUtility.OpenDataTable(strsql).Rows[0][0].ToString();
                        if (Conversion.Val(strValue) == 0)
                        {
                            strError += "Invalid Dividend Bank Code/ IFSC" + Environment.NewLine;
                        }

                        strValue = isNull(bankObject.BANK_AC_TYPE).Trim();
                        if (strValue != "10" && strValue != "11" && strValue != "13")
                        {
                            strError += "Invalid Dividend Bank Ac type" + Environment.NewLine;
                        }

                        strValue = isNull(bankObject.BANK_CURRENCY).Trim();
                        strsql = "Select count(0) from Clientsub_master where cs_module = 'CS05' and cs_code = '" + isNull(bankObject.BANK_CURRENCY).Trim() + "'";
                        strValue = objUtility.OpenDataTable(strsql).Rows[0][0].ToString();
                        if (Conversion.Val(strValue) == 0)
                        {
                            strError += "Invalid Dividend Bank Currency" + Environment.NewLine;
                        }

                        strValue = isNull(bankObject.ECS_MANDATE).Trim();
                        if (strValue != "N" && strValue != "Y")
                        {
                            strError += "Invalid Value in Dividend Bank ECS Mandate" + Environment.NewLine;
                        }
                    }
                }

                if (jsonObject.NOMINEEGUARDIAN != null && strParm.Contains("NOMGUR"))
                {
                    List<NomineeDetails> nomArr = jsonObject.NOMINEEGUARDIAN;

                    string date = objUtility.fnFireQueryCross("client_master", "cm_dateofbirth", "cm_cd", strBOID, true);
                    string strDt = Strings.Right(date.Trim(), 2) + "/" + Strings.Mid(date.Trim(), 5, 2) + "/" + Strings.Left(date.Trim(), 4);
                    DateTime dtdate = DateTime.ParseExact(strDt, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None);

                    int years = Years(dtdate, DateTime.Today);
                    bool isClientMinor = years < 18;

                    Double dblSharePer = 0;

                    bool blnNominee = false;
                    bool blnMinorGuardian = false;
                    string strType = objUtility.fnFireQueryCross("Client_Master", "cm_productcd", "cm_cd", strBOID, true);
                    int intClientType = Convert.ToInt16(strType == "" ? "0" : strType);
                    string strDesc = "";
                    string strMinorNominee = "";
                    string strNomErr = "";
                    int intCount = 0;

                    foreach (var item in nomArr)
                    {
                        if (isNull(item.PURPOSE_CODE, 8) == "6" || item.PURPOSE_CODE == "7" || item.PURPOSE_CODE == "8")
                        {
                            if (item.PURPOSE_CODE.Trim() == "6" && item.NOMINEE_SERIAL_NO.Trim() == "1")
                            {
                                strDesc = "(1st) Nominee";
                            }
                            else if (item.PURPOSE_CODE.Trim() == "8" && item.NOMINEE_SERIAL_NO.Trim() == "1")
                            {
                                strDesc = "(1st) Guardian";
                            }
                            else if (item.PURPOSE_CODE.Trim() == "6" && item.NOMINEE_SERIAL_NO.Trim() == "2")
                            {
                                strDesc = "(2nd) Nominee";
                            }
                            else if (item.PURPOSE_CODE.Trim() == "8" && item.NOMINEE_SERIAL_NO.Trim() == "2")
                            {
                                strDesc = "(2nd) Guardian";
                            }
                            else if (item.PURPOSE_CODE.Trim() == "8" && item.NOMINEE_SERIAL_NO.Trim() == "3")
                            {
                                strDesc = "(3rd) Nominee";
                            }
                            else if (item.PURPOSE_CODE.Trim() == "8" && item.NOMINEE_SERIAL_NO.Trim() == "3")
                            {
                                strDesc = "(3rd) Guardian";
                            }
                            else if (item.PURPOSE_CODE.Trim() == "7" && item.NOMINEE_SERIAL_NO.Trim() == "1")
                            {
                                strDesc = "Guardian";
                            }

                            List<string> lstMissingKeys = GetMissingFields(item);
                            if (lstMissingKeys.Count > 0)
                            {
                                strNomErr += "Following Fields are missing in " + strDesc + " - " + string.Join(", ", lstMissingKeys) + Environment.NewLine;
                            }
                            else
                            {
                                if (isNull(item.PURPOSE_CODE, 8) == "6")
                                {
                                    blnNominee = true;
                                    strValue = isNull(item.SHARE_PERCENTAGE).Trim();
                                    if (Conversion.Val(strValue) == 0)
                                    {
                                        strError += "Invalid value specified in " + strDesc + " ( SHARE_PERCENTAGE ) " + Environment.NewLine;
                                    }

                                    dblSharePer += Conversion.Val(isNull(item.SHARE_PERCENTAGE));

                                    strValue = isNull(item.RESIDUAL_SECURITIES).Trim() == "Y" ? "Y" : "N";
                                    if (strValue == "Y")
                                    {
                                        intCount++;
                                    }
                                }
                                else if (isNull(item.PURPOSE_CODE, 8) == "7")
                                {
                                    blnMinorGuardian = true;
                                }
                                strValue = isNull(item.NAME).Trim();
                                if (strValue.Trim() == "")
                                {
                                    strError += "Invalid value specified in " + strDesc + " ( NAME ) " + Environment.NewLine;
                                }
                                strValue = isNull(item.LAST_NAME).Trim();
                                if (strValue.Trim() == "")
                                {
                                    strError += "Invalid value specified in " + strDesc + " ( LAST_NAME ) " + Environment.NewLine;
                                }
                                strValue = isNull(item.ADDRESS1).Trim();
                                if (strValue.Trim() == "")
                                {
                                    strError += "Invalid value specified in " + strDesc + " ( ADDRESS_LINE_1 ) " + Environment.NewLine;
                                }
                                strValue = isNull(item.CITY).Trim();
                                if (strValue.Trim() == "")
                                {
                                    strError += "Invalid value specified in " + strDesc + " ( ADDRESS_CITY ) " + Environment.NewLine;
                                }
                                else
                                {
                                    if (!ValidateField("CITY", strValue))
                                    {
                                        strError += "Invalid value specified in " + strDesc + " ( ADDRESS_CITY ) " + Environment.NewLine;
                                    }
                                }
                                strValue = isNull(item.STATE).Trim();
                                if (strValue.Trim() == "")
                                {
                                    strError += "Invalid value specified in " + strDesc + " ( ADDRESS_STATE ) " + Environment.NewLine;
                                }
                                else
                                {
                                    if (!ValidateField("STATE", strValue))
                                    {
                                        strError += "Invalid value specified in " + strDesc + " ( ADDRESS_STATE ) " + Environment.NewLine;
                                    }
                                }
                                strValue = isNull(item.COUNTRY).Trim();
                                if (strValue.Trim() == "")
                                {
                                    strError += "Invalid value specified in " + strDesc + " ( ADDRESS_COUNTRY ) " + Environment.NewLine;
                                }
                                else
                                {
                                    if (!ValidateField("COUNTRY", strValue))
                                    {
                                        strError += "Invalid value specified in " + strDesc + " ( ADDRESS_COUNTRY ) " + Environment.NewLine;
                                    }
                                }
                                strValue = isNull(item.PIN).Trim();
                                if (strValue.Trim() == "")
                                {
                                    strError += "Invalid value specified in " + strDesc + " ( ADDRESS_PIN ) " + Environment.NewLine;
                                }
                                else
                                {
                                    if (!ValidateField("PIN", strValue))
                                    {
                                        strError += "Invalid value specified in " + strDesc + " ( ADDRESS_PIN ) " + Environment.NewLine;
                                    }
                                }
                                strValue = isNull(item.RELATIONSHIP).Trim();
                                if (strValue.Trim() == "")
                                {
                                    strError += "Invalid value specified in " + strDesc + " ( RELATIONSHIP ) " + Environment.NewLine;
                                }
                                else
                                {
                                    strsql = "select count(0) from Clientsub_master where cs_module ='CS19' and cs_code ='" + strValue + "'";
                                    strValue = objUtility.OpenDataTable(strsql).Rows[0][0].ToString();
                                    if (Conversion.Val(strValue) == 0)
                                    {
                                        strError += "Invalid value specified in " + strDesc + " ( RELATIONSHIP ) " + Environment.NewLine;
                                    }
                                }
                                strValue = isNull(item.EMAIL).Trim();
                                if (strValue.Trim() != "")
                                {
                                    if (!IsValidEmail(strValue))
                                    {
                                        strError += "Invalid value specified in " + strDesc + " ( EMAIL ) " + Environment.NewLine;
                                    }
                                }
                                strValue = isNull(item.PAN_NO).Trim();
                                if (strValue.Trim() != "")
                                {
                                    if (!isValidPAN(strValue))
                                    {
                                        strError += "Invalid value specified in " + strDesc + " ( PAN_NO ) " + Environment.NewLine;
                                    }
                                }

                                strValue = isNull(item.MOBILE_NO).Trim();
                                if (strValue.Trim() != "")
                                {
                                    if (strValue.Trim().Length < 10)
                                    {
                                        strError += "Invalid value specified in " + strDesc + " ( MOBILE_NO ) " + Environment.NewLine;
                                    }
                                }

                                strValue = isNull(item.MOBILE_ISD_CODE).Trim();
                                if (strValue.Trim() != "")
                                {
                                    strsql = "Select count(0) From Country_Master Where Co_ISDCode = '" + isNull(item.MOBILE_ISD_CODE).Trim() + "'";
                                    strValue = objUtility.OpenDataTable(strsql).Rows[0][0].ToString();
                                    if (Conversion.Val(strValue) == 0)
                                    {
                                        strError += "Invalid value specified in " + strDesc + " ( Mobile ISD Code ) " + Environment.NewLine;
                                    }
                                }

                                if (isNull(item.PURPOSE_CODE, 8).Trim() == "6")
                                {
                                    strValue = isNull(item.DATE_OF_BIRTH);
                                    if (strValue.Trim() != "")
                                    {
                                        if (!CheckDate(strValue, "yyyyMMdd"))
                                        {
                                            strError += "Invalid " + strDesc + " Date of Birth" + Environment.NewLine;
                                        }
                                        else
                                        {
                                            String strdt = Strings.Left(strValue, 4) + "-" + Strings.Mid(strValue, 5, 2) + "-" + Strings.Right(strValue, 2);

                                            DateTime dt = Convert.ToDateTime(strdt, CultureInfo.InvariantCulture);
                                            DateTime dt_now = DateTime.Now;

                                            DateTime dt_18 = dt.AddYears(18);
                                            if (dt_18.Date >= dt_now.Date)
                                            {
                                                strMinorNominee += item.NOMINEE_SERIAL_NO.Trim() + "/";
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (intCount == 0)
                    {
                        strError += "Residual Flag should Marked in atleast One Nominee" + Environment.NewLine;
                    }
                    else if (intCount > 1)
                    {
                        strError += "Residual Flag should Marked only in One Nominee" + Environment.NewLine;
                    }

                    if (strNomErr != "")
                    {
                        strError += "NOMINEEGUARDIAN : " + strNomErr;
                    }
                    else
                    {
                        string strGuardianName = "";
                        if (strMinorNominee.Contains("1"))
                        {
                            strGuardianName = nomArr.AsEnumerable().Where(x => x.PURPOSE_CODE.Trim() == "8" && x.NOMINEE_SERIAL_NO.Trim() == "1").Select(x => x.NAME).FirstOrDefault();
                            if (string.IsNullOrWhiteSpace(strGuardianName))
                            {
                                strError += "(1st) Nominee Guardian Details are Mandatory if Nominee is Minor" + Environment.NewLine;
                            }
                        }

                        if (strMinorNominee.Contains("2"))
                        {
                            strGuardianName = nomArr.AsEnumerable().Where(x => x.PURPOSE_CODE.Trim() == "8" && x.NOMINEE_SERIAL_NO.Trim() == "2").Select(x => x.NAME).FirstOrDefault();
                            if (string.IsNullOrWhiteSpace(strGuardianName))
                            {
                                strError += "(2nd) Nominee Guardian Details are Mandatory if Nominee is Minor" + Environment.NewLine;
                            }
                        }

                        if (strMinorNominee.Contains("3"))
                        {
                            strGuardianName = nomArr.AsEnumerable().Where(x => x.PURPOSE_CODE.Trim() == "8" && x.NOMINEE_SERIAL_NO.Trim() == "3").Select(x => x.NAME).FirstOrDefault();
                            if (string.IsNullOrWhiteSpace(strGuardianName))
                            {
                                strError += "(3rd) Nominee Guardian Details are Mandatory if Nominee is Minor" + Environment.NewLine;
                            }
                        }

                        if (!blnNominee)
                        {
                            strError += "Nominee Details Not Found" + Environment.NewLine;
                        }
                        if (dblSharePer != 100)
                        {
                            strError += "Sum of All Nominee Share Percentage should be 100 ( SHARE_PERCENTAGE )" + Environment.NewLine;
                        }
                        if (isClientMinor)
                        {
                            if (!blnMinorGuardian)
                            {
                                strError += "Client is Minor, Guradian Details Not Found" + Environment.NewLine;
                            }
                        }
                    }
                }

                if (jsonObject.OTHERDETAILS != null && strParm.Contains("ANUINC"))
                {
                    OtherDetails othDetailsObject = jsonObject.OTHERDETAILS;
                    List<string> lstMissingKeys = GetMissingFields(othDetailsObject);

                    if (lstMissingKeys.Count > 0)
                    {
                        strError += "OTHERDETAILS : Following Fields are missing - " + string.Join(", ", lstMissingKeys) + Environment.NewLine;
                    }
                    else
                    {
                        strValue = isNull(othDetailsObject.ANNUAL_INCOME).Trim();
                        if (strValue != "")
                        {
                            if (Conversion.Val(objUtility.fnFireQueryCross("Clientsub_master", "count(0)", "cs_module='CS03' and cs_code", strValue, true)) == 0)
                            {
                                strError += "Invalid Value in Annual Income" + Environment.NewLine;
                            }
                        }
                    }
                }

                return strError;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic ClientModification(CrossClientModification jsonObject, string userId)
        {
            try
            {
                SqlTransaction objTrans;
                var db = new DataContext();
                string strBOID = "";
                string strOldValue = "";
                string strNewValue = "";
                string strGroupAuth = "";
                bool blnAuthorise;
                string strRefNo = "";
                string strInwardNo = "";
                string strRefDt = "";
                string strRefTime = "";
                string strParm = objUtility.GetSysParmSt("CLMOPTIONS", "");

                if (isNull(jsonObject.BOID) == "")
                {
                    return "BOID cannot be blank";
                }

                strBOID = isNull(jsonObject.BOID);

                string strValue = isNull(jsonObject.RECEIPT_DATE_TIME, 14);
                if (strValue != "")
                {
                    strRefDt = Strings.Left(strValue, 8);
                    strRefTime = Strings.Right(strValue, 6);
                    if (strRefTime.Trim() != "")
                    {
                        strRefTime = Strings.Left(strRefTime, 2) + ":" + Strings.Mid(strRefTime, 3, 2) + ":" + Strings.Right(strRefTime, 2);
                    }
                    else
                    {
                        strRefTime = "00:00:00";
                    }
                }

                strValue = isNull(jsonObject.REFERENCENO);
                if (strValue != "")
                {
                    strRefNo = strValue.Trim();
                }

                userId = userId.ToUpper();

                using (SqlConnection sqlCon = new SqlConnection((db.Database.GetDbConnection()).ConnectionString))
                {
                    sqlCon.Open();
                    objTrans = sqlCon.BeginTransaction();
                    SqlDataAdapter sqlDtAdap = new SqlDataAdapter();
                    SqlCommandBuilder sqlCmdBld = new SqlCommandBuilder(sqlDtAdap);

                    DataSet rsTemp = objUtility.OpenDataSet("select * from Auth_master where am_code = 'CLM'");
                    if (rsTemp.Tables[0].Rows.Count == 0)
                    {
                        strGroupAuth = "";
                        blnAuthorise = false;
                    }
                    else
                    {
                        strGroupAuth = rsTemp.Tables[0].Rows[0]["am_group1"].ToString().Trim();
                        if (strGroupAuth != objUtility.fnFireQueryCross("User_master", "um_group_id", "um_user_id", userId, true).Trim())
                            blnAuthorise = false;
                        else
                            blnAuthorise = true;
                    }

                    strsql = "select * from Client_Master, Client_Backoffice Where cm_cd = cb_cmcd and cm_cd = '" + strBOID + "'";
                    DataTable dtCrossMasterOld = objUtility.OpenDataTable(strsql, sqlCon, objTrans);
                    if (dtCrossMasterOld.Rows.Count > 0)
                    {
                        if (jsonObject.CORRESPONDENCEADDRESS != null && strParm.Contains("CORRADD"))
                        {
                            Address corrAddObject = jsonObject.CORRESPONDENCEADDRESS;
                            strOldValue = dtCrossMasterOld.Rows[0]["cm_add1"].ToString().Trim();
                            strNewValue = isNull(corrAddObject.ADDRESS1, 55).Trim();
                            prInsertCAudit(strBOID, "cm_add1", strOldValue, strNewValue, "Client Address", "", "Correspondance Address", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                            strOldValue = dtCrossMasterOld.Rows[0]["cm_add2"].ToString().Trim();
                            strNewValue = isNull(corrAddObject.ADDRESS2, 55).Trim();
                            prInsertCAudit(strBOID, "cm_add2", strOldValue, strNewValue, "Client Address", "", "Correspondance Address", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                            strOldValue = dtCrossMasterOld.Rows[0]["cm_add3"].ToString().Trim();
                            strNewValue = isNull(corrAddObject.ADDRESS3, 55).Trim();
                            prInsertCAudit(strBOID, "cm_add3", strOldValue, strNewValue, "Client Address", "", "Correspondance Address", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                            strOldValue = dtCrossMasterOld.Rows[0]["cm_pin"].ToString().Trim();
                            strNewValue = isNull(corrAddObject.PIN, 10).Trim();
                            prInsertCAudit(strBOID, "cm_pin", strOldValue, strNewValue, "Client Address", "", "Correspondance Address", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                            strOldValue = dtCrossMasterOld.Rows[0]["cm_city"].ToString().Trim();
                            strNewValue = isNull(corrAddObject.CITY, 25).Trim();
                            prInsertCAudit(strBOID, "cm_city", strOldValue, strNewValue, "Client Address", "", "Correspondance Address", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                            strOldValue = dtCrossMasterOld.Rows[0]["cm_state"].ToString().Trim();
                            strNewValue = isNull(corrAddObject.STATE, 25).Trim();
                            prInsertCAudit(strBOID, "cm_state", strOldValue, strNewValue, "Client Address", "", "Correspondance Address", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                            strOldValue = dtCrossMasterOld.Rows[0]["cm_country"].ToString().Trim();
                            strNewValue = isNull(corrAddObject.COUNTRY, 25).Trim();
                            prInsertCAudit(strBOID, "cm_country", strOldValue, strNewValue, "Client Address", "", "Correspondance Address", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);
                        }

                        if (jsonObject.PERMANENTADDRESS != null && strParm.Contains("PERMADD"))
                        {
                            Address perAddObject = jsonObject.PERMANENTADDRESS;

                            strOldValue = dtCrossMasterOld.Rows[0]["cb_fadd1"].ToString().Trim();
                            strNewValue = isNull(perAddObject.ADDRESS1, 55).Trim();
                            prInsertCAudit(strBOID, "cb_fadd1", strOldValue, strNewValue, "Client Permanent Address", "M", "Permanent Address", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                            strOldValue = dtCrossMasterOld.Rows[0]["cb_fadd2"].ToString().Trim();
                            strNewValue = isNull(perAddObject.ADDRESS2, 55).Trim();
                            prInsertCAudit(strBOID, "cb_fadd2", strOldValue, strNewValue, "Client Permanent Address", "M", "Permanent Address", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                            strOldValue = dtCrossMasterOld.Rows[0]["cb_fadd3"].ToString().Trim();
                            strNewValue = isNull(perAddObject.ADDRESS3, 55).Trim();
                            prInsertCAudit(strBOID, "cb_fadd3", strOldValue, strNewValue, "Client Permanent Address", "M", "Permanent Address", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                            strOldValue = dtCrossMasterOld.Rows[0]["cb_fpin"].ToString().Trim();
                            strNewValue = isNull(perAddObject.PIN, 10).Trim();
                            prInsertCAudit(strBOID, "cb_fpin", strOldValue, strNewValue, "Client Permanent Address", "M", "Permanent Address", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                            strOldValue = dtCrossMasterOld.Rows[0]["cb_fcity"].ToString().Trim();
                            strNewValue = isNull(perAddObject.CITY, 25).Trim();
                            prInsertCAudit(strBOID, "cb_fcity", strOldValue, strNewValue, "Client Permanent Address", "M", "Permanent Address", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                            strOldValue = dtCrossMasterOld.Rows[0]["cb_fstate"].ToString().Trim();
                            strNewValue = isNull(perAddObject.STATE, 25).Trim();
                            prInsertCAudit(strBOID, "cb_fstate", strOldValue, strNewValue, "Client Permanent Address", "M", "Permanent Address", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                            strOldValue = dtCrossMasterOld.Rows[0]["cb_fcountry"].ToString().Trim();
                            strNewValue = isNull(perAddObject.COUNTRY, 25).Trim();
                            prInsertCAudit(strBOID, "cb_fcountry", strOldValue, strNewValue, "Client Permanent Address", "M", "Permanent Address", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);
                        }

                        if (jsonObject.EMAIL != null && strParm.Contains("EMAIL"))
                        {
                            Email emailObject = jsonObject.EMAIL;

                            strOldValue = dtCrossMasterOld.Rows[0]["cm_email"].ToString().Trim();
                            strNewValue = isNull(emailObject.EMAIL, 100).Trim();
                            if (strOldValue.Trim() != strNewValue.Trim())
                            {
                                prInsertCAudit(strBOID, "cm_email", strOldValue, strNewValue, "Client Email", "", "Email", strGroupAuth, "1", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);
                            }

                            strOldValue = dtCrossMasterOld.Rows[0]["cb_secondaryemail"].ToString().Trim();
                            strNewValue = isNull(emailObject.SECONDARY_EMAIL, 100).Trim();
                            if (strOldValue.Trim() != strNewValue.Trim())
                            {
                                prInsertCAudit(strBOID, "cb_secondaryemail", strOldValue, strNewValue, "Secondary Email", "", "Email", strGroupAuth, "1", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);
                            }

                            strOldValue = dtCrossMasterOld.Rows[0]["cb_bankstate"].ToString().Trim();
                            strNewValue = isNull(emailObject.SECONDHOLDER_EMAIL, 100).Trim();
                            if (strOldValue.Trim() != strNewValue.Trim())
                            {
                                prInsertCAudit(strBOID, "cb_bankstate", strOldValue, strNewValue, "Second Holder Email", "", "Email", strGroupAuth, "2", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);
                            }

                            strOldValue = dtCrossMasterOld.Rows[0]["cb_bankpin"].ToString().Trim();
                            strNewValue = isNull(emailObject.THIRDHOLDER_EMAIL, 100).Trim();
                            if (strOldValue.Trim() != strNewValue.Trim())
                            {
                                prInsertCAudit(strBOID, "cb_bankpin", strOldValue, strNewValue, "Third Holder Email", "", "Email", strGroupAuth, "3", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);
                            }
                        }

                        if (jsonObject.MOBILE != null && strParm.Contains("MOBILE"))
                        {
                            Mobile mobileObject = jsonObject.MOBILE;

                            if (dtCrossMasterOld.Rows[0]["cb_MobileISD"].ToString().Trim() != isNull(mobileObject.PRIMARY_MOBILE_ISD, 6).Trim() || dtCrossMasterOld.Rows[0]["cm_tele1"].ToString().Trim() != isNull(mobileObject.PRIMARY_MOBILE_NO, 17).Trim())
                            {
                                strOldValue = dtCrossMasterOld.Rows[0]["cb_MobileISD"].ToString().Trim();
                                strNewValue = isNull(mobileObject.PRIMARY_MOBILE_ISD, 6).Trim();
                                prInsertCAudit(strBOID, "cb_MobileISD", strOldValue, strNewValue, "Primary Mobile ISD", "", "Mobile", strGroupAuth, "1", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = dtCrossMasterOld.Rows[0]["cm_tele1"].ToString().Trim();
                                strNewValue = isNull(mobileObject.PRIMARY_MOBILE_NO, 17).Trim();
                                prInsertCAudit(strBOID, "cm_tele1", strOldValue, strNewValue, "Primary Mobile", "", "Mobile", strGroupAuth, "1", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);
                            }

                            strOldValue = dtCrossMasterOld.Rows[0]["cb_SmartIndicator"].ToString().Trim().ToUpper();
                            strNewValue = isNull(mobileObject.SMART_REGISTRATION_INDICATOR, 1).Trim().ToUpper();
                            if (strOldValue.Trim() != strNewValue.Trim())
                            {
                                prInsertCAudit(strBOID, "cb_SmartIndicator", strOldValue, strNewValue, "Smart Registration Indicator", "", "Mobile", strGroupAuth, "1", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);
                            }

                            if (dtCrossMasterOld.Rows[0]["cb_SecondaryMobileISD"].ToString().Trim() != isNull(mobileObject.SECONDARY_ISD, 6).Trim() || dtCrossMasterOld.Rows[0]["cm_tele2"].ToString().Trim() != isNull(mobileObject.SECONDARY_TEL_NO, 17).Trim())
                            {
                                strOldValue = dtCrossMasterOld.Rows[0]["cb_SecondaryMobileISD"].ToString().Trim();
                                strNewValue = isNull(mobileObject.SECONDARY_ISD, 6).Trim();
                                prInsertCAudit(strBOID, "cb_SecondaryMobileISD", strOldValue, strNewValue, "Secondary Mobile ISD No", "", "Mobile", strGroupAuth, "1", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = dtCrossMasterOld.Rows[0]["cm_tele2"].ToString().Trim();
                                strNewValue = isNull(mobileObject.SECONDARY_TEL_NO, 17).Trim();
                                prInsertCAudit(strBOID, "cm_tele2", strOldValue, strNewValue, "Client Telephone no 2", "", "Mobile", strGroupAuth, "1", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);
                            }

                            if (dtCrossMasterOld.Rows[0]["cb_MobileISD2"].ToString().Trim() != isNull(mobileObject.SECONDHOLDER_MOBILE_ISD, 6).Trim() || dtCrossMasterOld.Rows[0]["cb_bankcity"].ToString().Trim() != isNull(mobileObject.SECONDHOLDER_MOBILE_NO, 31).Trim())
                            {
                                strOldValue = dtCrossMasterOld.Rows[0]["cb_MobileISD2"].ToString().Trim();
                                strNewValue = isNull(mobileObject.SECONDHOLDER_MOBILE_ISD, 6).Trim();
                                prInsertCAudit(strBOID, "cb_MobileISD2", strOldValue, strNewValue, "Second Holder Mobile ISD", "", "Mobile", strGroupAuth, "2", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = dtCrossMasterOld.Rows[0]["cb_bankcity"].ToString().Trim();
                                strNewValue = isNull(mobileObject.SECONDHOLDER_MOBILE_NO, 31).Trim();
                                prInsertCAudit(strBOID, "cb_bankcity", strOldValue, strNewValue, "Second Holder Mobile", "", "Mobile", strGroupAuth, "2", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);
                            }

                            if (dtCrossMasterOld.Rows[0]["cb_MobileISD3"].ToString().Trim() != isNull(mobileObject.THIRDHOLDER_MOBILE_ISD, 6).Trim() || dtCrossMasterOld.Rows[0]["cb_bankcountry"].ToString().Trim() != isNull(mobileObject.THIRDHOLDER_MOBILE_NO, 31).Trim())
                            {
                                strOldValue = dtCrossMasterOld.Rows[0]["cb_MobileISD3"].ToString().Trim();
                                strNewValue = isNull(mobileObject.THIRDHOLDER_MOBILE_ISD, 6).Trim();
                                prInsertCAudit(strBOID, "cb_MobileISD3", strOldValue, strNewValue, "Third Holder Mobile ISD", "", "Mobile", strGroupAuth, "3", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = dtCrossMasterOld.Rows[0]["cb_bankcountry"].ToString().Trim();
                                strNewValue = isNull(mobileObject.THIRDHOLDER_MOBILE_NO, 31).Trim();
                                prInsertCAudit(strBOID, "cb_bankcountry", strOldValue, strNewValue, "Third Holder Mobile", "", "Mobile", strGroupAuth, "3", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);
                            }
                        }

                        if (jsonObject.DIVIDENDBANKDETAIL != null && strParm.Contains("BANK"))
                        {
                            DividendBankDetail bankObject = jsonObject.DIVIDENDBANKDETAIL;

                            strOldValue = dtCrossMasterOld.Rows[0]["cm_divbankcode"].ToString().Trim();
                            strNewValue = isNull(bankObject.BANK_CODE, 12).Trim();
                            prInsertCAudit(strBOID, "cm_divbankcode", strOldValue, strNewValue, "Dividend Bank Code", "", "Dividend Bank Detail", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                            strOldValue = dtCrossMasterOld.Rows[0]["cm_divbankacno"].ToString().Trim();
                            strNewValue = isNull(bankObject.BANK_AC_NO).Trim();
                            prInsertCAudit(strBOID, "cm_divbankacno", strOldValue, strNewValue, "Dividend Bank A/c No.", "", "Dividend Bank Detail", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                            strOldValue = dtCrossMasterOld.Rows[0]["cm_divbankccy"].ToString().Trim();
                            strNewValue = isNull(bankObject.BANK_CURRENCY, 9).Trim();
                            prInsertCAudit(strBOID, "cm_divbankccy", strOldValue, strNewValue, "Dividend Bank Currency", "", "Dividend Bank Detail", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                            strOldValue = dtCrossMasterOld.Rows[0]["cb_voicemail"].ToString().Trim().ToUpper();
                            strNewValue = isNull(bankObject.BANK_IFSCCODE, 15).Trim().ToUpper();
                            prInsertCAudit(strBOID, "cb_voicemail", strOldValue, strNewValue, "Dividend Bank IFSC", "", "Dividend Bank Detail", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                            strOldValue = dtCrossMasterOld.Rows[0]["cm_divbranchno"].ToString().Trim();
                            strNewValue = isNull(bankObject.BANK_AC_TYPE, 12).Trim();
                            prInsertCAudit(strBOID, "cm_divbranchno", strOldValue, strNewValue, "Dividend A/c Type", "", "Dividend Bank Detail", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                            if (isNull(bankObject.ECS_MANDATE) != "")
                            {
                                strOldValue = dtCrossMasterOld.Rows[0]["cb_ecs"].ToString().Trim().ToUpper();
                                strNewValue = isNull(bankObject.ECS_MANDATE, 1).Trim().ToUpper();
                                if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                                {
                                    prInsertCAudit(strBOID, "cb_ecs", strOldValue, strNewValue, "ECS / Mandata", "", "Dividend Bank Detail", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);
                                }
                            }
                        }

                        if (jsonObject.NOMINEEGUARDIAN != null && strParm.Contains("NOMGUR"))
                        {
                            List<NomineeDetails> nomArr = jsonObject.NOMINEEGUARDIAN;

                            double dblSrNo;
                            string strDesc = "";
                            string strNomFlag = "M";
                            string strUploadFlag = "";
                            List<(string Key, string Value)> dictNomCd = new List<(string Key, string Value)>();

                            foreach (var item in nomArr)
                            {
                                strDesc = "";
                                strNomFlag = "M";

                                if (item.PURPOSE_CODE.Trim() == "" || item.NOMINEE_SERIAL_NO.Trim() == "")
                                {
                                    break;
                                }

                                dictNomCd.Add((item.PURPOSE_CODE.Trim(), item.NOMINEE_SERIAL_NO.Trim()));

                                dblSrNo = Conversion.Val(item.NOMINEE_SERIAL_NO.Trim());
                                dblSrNo = dblSrNo == 0 ? 1 : dblSrNo;

                                strsql = "select * from Client_NomineeDetails Where cn_cmcd = '" + strBOID + "' and cn_PurposeCd = " + item.PURPOSE_CODE.Trim() + " and cn_NomSrno = " + item.NOMINEE_SERIAL_NO;
                                DataTable dtNomCross = objUtility.OpenDataTable(strsql, sqlCon, objTrans);
                                if (dtNomCross.Rows.Count > 0)
                                {
                                    strNomFlag = "M";
                                }
                                else
                                {
                                    strNomFlag = "S";
                                }

                                strUploadFlag = strNomFlag;

                                #region nomineecode
                                if (item.PURPOSE_CODE.Trim() == "6" && item.NOMINEE_SERIAL_NO.Trim() == "1")
                                {
                                    strDesc = "Nominee (1st)";
                                }
                                else if (item.PURPOSE_CODE.Trim() == "8" && item.NOMINEE_SERIAL_NO.Trim() == "1")
                                {
                                    strDesc = "Guardian (1st)";
                                }
                                else if (item.PURPOSE_CODE.Trim() == "6" && item.NOMINEE_SERIAL_NO.Trim() == "2")
                                {
                                    strDesc = "Nominee (2nd)";
                                }
                                else if (item.PURPOSE_CODE.Trim() == "8" && item.NOMINEE_SERIAL_NO.Trim() == "2")
                                {
                                    strDesc = "Guardian (2nd)";
                                }
                                else if (item.PURPOSE_CODE.Trim() == "6" && item.NOMINEE_SERIAL_NO.Trim() == "3")
                                {
                                    strDesc = "Nominee (3rd)";
                                }
                                else if (item.PURPOSE_CODE.Trim() == "8" && item.NOMINEE_SERIAL_NO.Trim() == "3")
                                {
                                    strDesc = "Guardian (3rd)";
                                }
                                else if (item.PURPOSE_CODE.Trim() == "7" && item.NOMINEE_SERIAL_NO.Trim() == "1")
                                {
                                    strDesc = "Guardian";
                                }

                                strOldValue = "";
                                strNewValue = isNull(item.NAME).Trim();
                                if (strNomFlag == "M")
                                {
                                    strOldValue = dtNomCross.Rows[0]["cn_NomName"].ToString().Trim();
                                }
                                prInsertCAudit(strBOID, "cn_NomName", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = "";
                                strNewValue = isNull(item.MIDDLE_NAME).Trim();
                                if (strNomFlag == "M")
                                {
                                    strOldValue = dtNomCross.Rows[0]["cn_NomMidNm"].ToString().Trim();
                                }
                                prInsertCAudit(strBOID, "cn_NomMidNm", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = "";
                                strNewValue = isNull(item.LAST_NAME).Trim();
                                if (strNomFlag == "M")
                                {
                                    strOldValue = dtNomCross.Rows[0]["cn_NomlastNm"].ToString().Trim();
                                }
                                prInsertCAudit(strBOID, "cn_NomlastNm", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = "";
                                strNewValue = isNull(item.ADDRESS1).Trim();
                                if (strNomFlag == "M")
                                {
                                    strOldValue = dtNomCross.Rows[0]["cn_NomAdd1"].ToString().Trim();
                                }
                                prInsertCAudit(strBOID, "cn_NomAdd1", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = "";
                                strNewValue = isNull(item.ADDRESS2).Trim();
                                if (strNomFlag == "M")
                                {
                                    strOldValue = dtNomCross.Rows[0]["cn_NomAdd2"].ToString().Trim();
                                }
                                prInsertCAudit(strBOID, "cn_NomAdd2", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = "";
                                strNewValue = isNull(item.ADDRESS3).Trim();
                                if (strNomFlag == "M")
                                {
                                    strOldValue = dtNomCross.Rows[0]["cn_NomAdd3"].ToString().Trim();
                                }
                                prInsertCAudit(strBOID, "cn_NomAdd3", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = "";
                                strNewValue = isNull(item.CITY).Trim();
                                if (strNomFlag == "M")
                                {
                                    strOldValue = dtNomCross.Rows[0]["cn_City"].ToString().Trim();
                                }
                                prInsertCAudit(strBOID, "cn_City", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = "";
                                strNewValue = isNull(item.STATE).Trim();
                                if (strNomFlag == "M")
                                {
                                    strOldValue = dtNomCross.Rows[0]["cn_State"].ToString().Trim();
                                }
                                prInsertCAudit(strBOID, "cn_State", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = "";
                                strNewValue = isNull(item.COUNTRY).Trim();
                                if (strNomFlag == "M")
                                {
                                    strOldValue = dtNomCross.Rows[0]["cn_Country"].ToString().Trim();
                                }
                                prInsertCAudit(strBOID, "cn_Country", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = "";
                                strNewValue = isNull(item.PIN).Trim();
                                if (strNomFlag == "M")
                                {
                                    strOldValue = dtNomCross.Rows[0]["cn_NomPin"].ToString().Trim();
                                }
                                prInsertCAudit(strBOID, "cn_NomPin", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = "";
                                strNewValue = isNull(item.RELATIONSHIP).Trim();
                                if (strNomFlag == "M")
                                {
                                    strOldValue = dtNomCross.Rows[0]["cn_Relation"].ToString().Trim();
                                }
                                prInsertCAudit(strBOID, "cn_Relation", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                if (item.PURPOSE_CODE == "6")
                                {
                                    strOldValue = "";
                                    strNewValue = isNull(item.SHARE_PERCENTAGE).Trim();
                                    if (strNomFlag == "M")
                                    {
                                        strOldValue = dtNomCross.Rows[0]["cn_NomPershare"].ToString().Trim();
                                    }
                                    prInsertCAudit(strBOID, "cn_NomPershare", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                    strOldValue = "";
                                    strNewValue = isNull(item.RESIDUAL_SECURITIES).Trim() == "Y" ? "Y" : "N";
                                    if (strNomFlag == "M")
                                    {
                                        strOldValue = dtNomCross.Rows[0]["cn_ResidualFlag"].ToString().Trim();
                                    }
                                    prInsertCAudit(strBOID, "cn_ResidualFlag", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                    strOldValue = "";
                                    strNewValue = isNull(item.DATE_OF_BIRTH).Trim();
                                    if (strNomFlag == "M")
                                    {
                                        strOldValue = dtNomCross.Rows[0]["cn_NomDOB"].ToString().Trim();
                                    }
                                    prInsertCAudit(strBOID, "cn_NomDOB", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);
                                }

                                strOldValue = "";
                                strNewValue = isNull(item.TITLE).Trim();
                                if (strNomFlag == "M")
                                {
                                    strOldValue = dtNomCross.Rows[0]["cn_NomTitle"].ToString().Trim();
                                }
                                prInsertCAudit(strBOID, "cn_NomTitle", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = "";
                                strNewValue = isNull(item.SUFFIX).Trim();
                                if (strNomFlag == "M")
                                {
                                    strOldValue = dtNomCross.Rows[0]["cn_NomSuffix"].ToString().Trim();
                                }
                                prInsertCAudit(strBOID, "cn_NomSuffix", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = "";
                                strNewValue = isNull(item.FATHER_OR_HUSBAND_NAME).Trim();
                                if (strNomFlag == "M")
                                {
                                    strOldValue = dtNomCross.Rows[0]["cn_FathHusbnm"].ToString().Trim();
                                }
                                prInsertCAudit(strBOID, "cn_FathHusbnm", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = "";
                                strNewValue = isNull(item.MOBILE_ISD_CODE).Trim();
                                if (strNomFlag == "M")
                                {
                                    strOldValue = dtNomCross.Rows[0]["cn_NomMobileISD"].ToString().Trim();
                                }
                                prInsertCAudit(strBOID, "cn_NomMobileISD", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = "";
                                strNewValue = isNull(item.MOBILE_NO).Trim();
                                if (strNomFlag == "M")
                                {
                                    strOldValue = dtNomCross.Rows[0]["cn_PH1"].ToString().Trim();
                                }
                                prInsertCAudit(strBOID, "cn_PH1", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = "";
                                strNewValue = isNull(item.PAN_NO).Trim();
                                if (strNomFlag == "M")
                                {
                                    strOldValue = dtNomCross.Rows[0]["cn_NomPAN"].ToString().Trim();
                                }
                                prInsertCAudit(strBOID, "cn_NomPAN", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = "";
                                strNewValue = isNull(item.UID).Trim();
                                if (strNomFlag == "M")
                                {
                                    strOldValue = dtNomCross.Rows[0]["cn_NomUID"].ToString().Trim();
                                }
                                prInsertCAudit(strBOID, "cn_NomUID", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = "";
                                strNewValue = isNull(item.UID_VERIFY_FLAG).Trim();
                                if (strNomFlag == "M")
                                {
                                    strOldValue = dtNomCross.Rows[0]["cn_NomUIDVerifyFlag"].ToString().Trim();
                                }
                                prInsertCAudit(strBOID, "cn_NomUIDVerifyFlag", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = "";
                                strNewValue = isNull(item.EMAIL).Trim();
                                if (strNomFlag == "M")
                                {
                                    strOldValue = dtNomCross.Rows[0]["cn_NomEmail"].ToString().Trim();
                                }
                                prInsertCAudit(strBOID, "cn_NomEmail", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);
                                #endregion
                            }

                            string strWhere = "";

                            foreach (var (Key, Value) in dictNomCd)
                            {
                                strWhere += " and not (cn_PurposeCd = " + Key + " and cn_NomSrno = " + Value + ")";
                            }

                            strsql = "select * from Client_NomineeDetails where cn_Cmcd='" + strBOID + "' " + strWhere;
                            DataTable dtTemp = objUtility.OpenDataTable(strsql);
                            foreach (DataRow dr in dtTemp.Rows)
                            {
                                if (dr["cn_PurposeCd"].ToString().Trim() == "6" && dr["cn_NomSrno"].ToString().Trim() == "1")
                                {
                                    strDesc = "Nominee (1st)";
                                }
                                else if (dr["cn_PurposeCd"].ToString().Trim() == "8" && dr["cn_NomSrno"].ToString().Trim() == "1")
                                {
                                    strDesc = "Guardian (1st)";
                                }
                                else if (dr["cn_PurposeCd"].ToString().Trim() == "6" && dr["cn_NomSrno"].ToString().Trim() == "2")
                                {
                                    strDesc = "Nominee (2nd)";
                                }
                                else if (dr["cn_PurposeCd"].ToString().Trim() == "8" && dr["cn_NomSrno"].ToString().Trim() == "2")
                                {
                                    strDesc = "Guardian (2nd)";
                                }
                                else if (dr["cn_PurposeCd"].ToString().Trim() == "8" && dr["cn_NomSrno"].ToString().Trim() == "3")
                                {
                                    strDesc = "Nominee (3rd)";
                                }
                                else if (dr["cn_PurposeCd"].ToString().Trim() == "8" && dr["cn_NomSrno"].ToString().Trim() == "3")
                                {
                                    strDesc = "Guardian (3rd)";
                                }
                                else if (dr["cn_PurposeCd"].ToString().Trim() == "7" && dr["cn_NomSrno"].ToString().Trim() == "1")
                                {
                                    strDesc = "Guardian";
                                }

                                strNomFlag = "M";
                                strUploadFlag = "D";

                                strOldValue = dr["cn_NomName"].ToString().Trim();
                                strNewValue = strOldValue;
                                prInsertCAudit(strBOID, "cn_NomName", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = dr["cn_NomMidNm"].ToString().Trim();
                                strNewValue = strOldValue;
                                prInsertCAudit(strBOID, "cn_NomMidNm", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = dr["cn_NomlastNm"].ToString().Trim();
                                strNewValue = strOldValue;
                                prInsertCAudit(strBOID, "cn_NomlastNm", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = dr["cn_NomAdd1"].ToString().Trim();
                                strNewValue = strOldValue;
                                prInsertCAudit(strBOID, "cn_NomAdd1", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = dr["cn_NomAdd2"].ToString().Trim();
                                strNewValue = strOldValue;
                                prInsertCAudit(strBOID, "cn_NomAdd2", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = dr["cn_NomAdd3"].ToString().Trim();
                                strNewValue = strOldValue;
                                prInsertCAudit(strBOID, "cn_NomAdd3", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = dr["cn_City"].ToString().Trim();
                                strNewValue = strOldValue;
                                prInsertCAudit(strBOID, "cn_City", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = dr["cn_State"].ToString().Trim();
                                strNewValue = strOldValue;
                                prInsertCAudit(strBOID, "cn_State", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = dr["cn_Country"].ToString().Trim();
                                strNewValue = strOldValue;
                                prInsertCAudit(strBOID, "cn_Country", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = dr["cn_NomPin"].ToString().Trim();
                                strNewValue = strOldValue;
                                prInsertCAudit(strBOID, "cn_NomPin", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = dr["cn_Relation"].ToString().Trim();
                                strNewValue = strOldValue;
                                prInsertCAudit(strBOID, "cn_Relation", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                if (dr["cn_PurposeCd"].ToString().Trim() == "6")
                                {
                                    strOldValue = dr["cn_NomPershare"].ToString().Trim();
                                    strNewValue = strOldValue;
                                    prInsertCAudit(strBOID, "cn_NomPershare", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                    strOldValue = dr["cn_ResidualFlag"].ToString().Trim();
                                    strNewValue = strOldValue;
                                    prInsertCAudit(strBOID, "cn_ResidualFlag", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                    strOldValue = dr["cn_NomDOB"].ToString().Trim();
                                    strNewValue = strOldValue;
                                    prInsertCAudit(strBOID, "cn_NomDOB", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);
                                }

                                strOldValue = dr["cn_NomTitle"].ToString().Trim();
                                strNewValue = strOldValue;
                                prInsertCAudit(strBOID, "cn_NomTitle", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = dr["cn_NomSuffix"].ToString().Trim();
                                strNewValue = strOldValue;
                                prInsertCAudit(strBOID, "cn_NomSuffix", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = dr["cn_FathHusbnm"].ToString().Trim();
                                strNewValue = strOldValue;
                                prInsertCAudit(strBOID, "cn_FathHusbnm", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = dr["cn_NomMobileISD"].ToString().Trim();
                                strNewValue = strOldValue;
                                prInsertCAudit(strBOID, "cn_NomMobileISD", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = dr["cn_PH1"].ToString().Trim();
                                strNewValue = strOldValue;
                                prInsertCAudit(strBOID, "cn_PH1", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = dr["cn_NomPAN"].ToString().Trim();
                                strNewValue = strOldValue;
                                prInsertCAudit(strBOID, "cn_NomPAN", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = dr["cn_NomUID"].ToString().Trim();
                                strNewValue = strOldValue;
                                prInsertCAudit(strBOID, "cn_NomUID", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = dr["cn_NomUIDVerifyFlag"].ToString().Trim();
                                strNewValue = strOldValue;
                                prInsertCAudit(strBOID, "cn_NomUIDVerifyFlag", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);

                                strOldValue = dr["cn_NomEmail"].ToString().Trim();
                                strNewValue = strOldValue;
                                prInsertCAudit(strBOID, "cn_NomEmail", strOldValue, strNewValue, strDesc, strUploadFlag, "Nominee & Guardian", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);
                            }
                        }

                        if (jsonObject.OTHERDETAILS != null && strParm.Contains("ANUINC"))
                        {
                            OtherDetails othDetailsObject = jsonObject.OTHERDETAILS;

                            strOldValue = dtCrossMasterOld.Rows[0]["cb_annualincome"].ToString().Trim();
                            strNewValue = isNull(othDetailsObject.ANNUAL_INCOME).Trim();
                            if (strOldValue != strNewValue)
                            {
                                prInsertCAudit(strBOID, "cb_annualincome", strOldValue, strNewValue, "Annual Income", "", "Other Details", strGroupAuth, "", "", strRefDt, strRefTime, strRefNo, strInwardNo, userId, sqlCon, objTrans);
                            }
                        }

                        objTrans.Commit();
                        return "Records updated successfully";
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region Client Modification Methods

        private void prInsertCAudit(string strclient, string strField, string strOldValue, string strNewvalue, string strFDesc, string struploadflag, string strType, string strGroupAuth, string strHolder, string strScheme, string strDated, string strTime, string strRefNo, string strInwardNo, string strUserID, SqlConnection sqlCon, SqlTransaction sqlTrans)
        {
            string strFlag = "M";
            string strtrxtype = string.Empty;
            string strclosure = string.Empty;
            string strclosurereason = string.Empty;
            string strrembal = string.Empty;
            string strSQL = "";
            string CurrentTime = DateTime.Now.ToString("HH:mm:ss");

            if (strType == "PAN" || strType == "Father/Husband Name" || strType == "Email" || strType == "Mobile" || strType == "POA" || strType == "Dividend Bank Detail" || strType == "BO Sub Status" || strType == "Other Details" || strType == "UID" || strType == "Close Account" || strType == "Permanent Address" || strType == "Correspondance Address" || strType == "Nominee & Guardian")
            {
                strSQL = " delete from Client_modification where ca_cmcd = '" + strclient + "'";
                if (strFlag != "C")
                    strSQL = strSQL + " and ca_field = '" + strField + "' ";
                if (strType == "Nominee & Guardian")
                {
                    strSQL += " and ca_trxtype in ('6','7','8')";
                    switch (strFDesc)
                    {
                        case "Nominee (1st)":
                            strSQL += " and ca_trxtype = '6' and ca_closure = '1' ";
                            break;
                        case "Guardian (1st)":
                            strSQL += " and ca_trxtype = '8' and ca_closure = '1' ";
                            break;
                        case "Nominee (2nd)":
                            strSQL += " and ca_trxtype = '6' and ca_closure = '2' ";
                            break;
                        case "Guardian (2nd)":
                            strSQL += " and ca_trxtype = '8' and ca_closure = '2' ";
                            break;
                        case "Nominee (3rd)":
                            strSQL += " and ca_trxtype = '6' and ca_closure = '3' ";
                            break;
                        case "Guardian (3rd)":
                            strSQL += " and ca_trxtype = '8' and ca_closure = '3' ";
                            break;
                        case "Guardian":
                            strSQL += " and ca_trxtype = '7' and ca_closure = '1' ";
                            break;
                    }
                }
                strSQL = strSQL + " and ca_flag = '" + strFlag + "'";
                if (strGroupAuth.Trim() == "")
                    strSQL = strSQL + " and ca_allow = 'Y' ";
                else
                    strSQL = strSQL + " and ca_allow = 'N' ";
                objUtility.ExecuteSQL(strSQL, sqlCon, sqlTrans);
            }
            strSQL = "Insert into Client_modification ";
            strSQL = strSQL + " values (";
            strSQL = strSQL + " '" + strclient + "', ";  //'ca_cmcd
            if (strFlag == "C")
            {
                strSQL = strSQL + " '', ";  //'ca_field
                strSQL = strSQL + " '', ";  //'ca_oldvalue
                strSQL = strSQL + " '', ";  //'ca_newvalue
            }
            else
            {
                strSQL = strSQL + " '" + strField + "', ";  //'ca_field
                strSQL = strSQL + " '" + strOldValue + "', ";  //'ca_oldvalue
                strSQL = strSQL + " '" + strNewvalue + "', ";  //'ca_newvalue
            }
            strSQL = strSQL + " '" + strUserID + "', ";  //'mkrid
            strSQL = strSQL + " '" + DateTime.Now.ToString("yyyyMMdd").Trim() + "', ";   //'mkrdt
            strSQL = strSQL + " '" + "API" + "', ";  //'ca_computername
            strSQL = strSQL + " '" + CurrentTime + "', ";  //'ca_time
            if (strGroupAuth.Trim() == "")
            {
                strSQL = strSQL + " 'Y', ";// 'ca_allow
                if (strType == "POA" || strType == "UID" || strType == "HOLDER NAME" || strType == "Email" || strType == "Mobile")
                {
                    strSQL += " '" + strHolder + "', ";// 'ca_brcode
                }
                else
                {
                    strSQL = strSQL + " '', ";// 'ca_brcode
                }
                strSQL = strSQL + " '" + strUserID + "', ";// 'ca_authid
                strSQL = strSQL + " '" + System.DateTime.Now.ToString("yyyyMMdd").Trim() + "', ";// 'ca_authdt
            }
            else
            {
                strSQL = strSQL + " 'N', ";// 'ca_allow
                if (strType == "POA" || strType == "UID" || strType == "HOLDER NAME" || strType == "Email" || strType == "Mobile")
                {
                    strSQL = strSQL + " '" + strHolder + "', ";// 'ca_brcode
                }
                else
                {
                    strSQL = strSQL + " '', ";// 'ca_brcode
                }
                strSQL = strSQL + " '', ";// 'ca_authid
                strSQL = strSQL + " Null, ";// 'ca_authdt
            }
            strSQL = strSQL + " 0, ";// 'ca_batchno
            strSQL = strSQL + " '" + strFDesc + "', ";// 'Field Description
            strSQL = strSQL + " '" + strRefNo + "', ";// 'Reference No.
            if (strDated.Trim() != "") // 'Reference Dt
                strSQL = strSQL + " '" + strDated.Trim() + "',";
            else
                strSQL = strSQL + " '" + "" + "',";
            strSQL = strSQL + " '" + strInwardNo + "', ";// 'Inward No
            strSQL = strSQL + " '" + strFlag + "', ";
            if (strFlag == "C")
            {

            }
            else
            {
                if (strFlag == "P")
                {

                }
                else if (strType == "Nominee & Guardian")
                {
                    switch (strFDesc)
                    {
                        case "Nominee (1st)":
                            strSQL += " '6','1','','','','',";
                            break;
                        case "Guardian (1st)":
                            strSQL += " '8','1','','','','', ";
                            break;
                        case "Nominee (2nd)":
                            strSQL += " '6','2','','','','', ";
                            break;
                        case "Guardian (2nd)":
                            strSQL += " '8','2','','','','', ";
                            break;
                        case "Nominee (3rd)":
                            strSQL += " '6','3','','','','', ";
                            break;
                        case "Guardian (3rd)":
                            strSQL += " '8','3','','','','', ";
                            break;
                        case "Guardian":
                            strSQL += " '7','1','','','','', ";
                            break;
                    }
                }
                else
                    strSQL = strSQL + " '','','','','','', ";

                if (strType == "Holder Name")
                {

                }
                else
                    strSQL = strSQL + " '','' ,";

                if (strType == "SMS Alert" || strType == "POA" || strType == "Nominee & Guardian" || strType == "Permanent Address" || strType == "Correspondance Address")
                {
                    strSQL = strSQL + " '" + struploadflag + "'";
                }
                else
                    strSQL = strSQL + " ''";
            }

            strSQL = strSQL + " ,'" + strTime + "'";
            strSQL = strSQL + " ) ";
            objUtility.ExecuteSQL(strSQL, sqlCon, sqlTrans);
        }

        public bool CheckKey(JObject jObj, string strKey)
        {
            return jObj.Property(strKey, StringComparison.OrdinalIgnoreCase) != null;
        }

        public string GetValue(JObject jObj, string strKey)
        {
            return jObj.GetValue(strKey, StringComparison.OrdinalIgnoreCase)?.Value<string>() ?? "";
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

        public dynamic GetMissingFields(dynamic obj)
        {
            List<string> lstMissingKeys = new List<string>();
            Type type = obj.GetType();
            System.Reflection.PropertyInfo[] properties = type.GetProperties();
            foreach (System.Reflection.PropertyInfo property in properties)
            {
                object value = property.GetValue(obj);
                if (value == null)
                {
                    lstMissingKeys.Add(property.Name);
                }
            }
            return lstMissingKeys;
        }

        public bool CheckDate(String date, string format)
        {
            if (DateTime.TryParseExact(date, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean FnISDValidate(string StrISDCode)
        {
            if (StrISDCode.Trim() != "")
            {
                return Convert.ToInt16(objUtility.fnFireQueryCross("Country_Master", "COUNT(*)", "Co_ISDCode", StrISDCode.Trim(), true)) > 0;
            }
            else
                return false;
        }

        public bool IsValidEmail(string Value)
        {
            bool isEmail = System.Text.RegularExpressions.Regex.IsMatch(Value, @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return isEmail;
        }

        public int Years(DateTime start, DateTime end)
        {
            return (end.Year - start.Year - 1) +
                (((end.Month > start.Month) ||
                ((end.Month == start.Month) && (end.Day >= start.Day))) ? 1 : 0);
        }

        public bool ValidateField(string strType, string strValue)
        {
            string strCount = "";

            if (strType == "PIN")
            {
                strsql = "Select count(0) from PinCode_master where PI_PINCODE='" + strValue + "'";
            }
            else if (strType == "CITY")
            {
                strsql = "Select count(0) from PinCode_master where PI_DISTRICTNAME='" + strValue + "'";
            }
            else if (strType == "STATE")
            {
                strsql = "Select count(0) from State_Master where st_state = '" + strValue + "'";
            }
            else if (strType == "COUNTRY")
            {
                strsql = "Select count(0) from Country_Master where co_name = '" + strValue + "'";
            }
            strCount = objUtility.OpenDataTable(strsql).Rows[0][0].ToString();
            return Conversion.Val(strCount) > 0;
        }

        public string ValidatePinCity(string strPin, string strCity)
        {
            DataTable dtChk = new DataTable();
            string strValue = "";
            string strNewCity = "";

            strsql = " select upper(rtrim(ltrim(PI_DISTRICTNAME))) PI_DISTRICTNAME from PINCODE_MASTER where  PI_PINCODE = " + strPin + "";
            dtChk = objUtility.OpenDataTable(strsql);
            if (dtChk.Rows.Count > 0)
            {
                strValue = isNull(strCity);
                foreach (DataRow dr in dtChk.Rows)
                {
                    strNewCity += dr["PI_DISTRICTNAME"].ToString().Trim() + ", ";
                    if (strValue.Trim() == dr["PI_DISTRICTNAME"].ToString().Trim())
                    {
                        strNewCity = "";
                        break;
                    }
                }
            }

            return strNewCity;
        }

        public bool isValidPAN(string Value)
        {
            if (Value.Trim().Length < 10)
            {
                return false;
            }
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("^[a-zA-Z]{5}[0-9]{4}[a-zA-Z]{1}$");
            System.Text.RegularExpressions.Match match = regex.Match(Value);
            return match.Success;
        }
        #endregion

        /*public dynamic GetUserDetails(Filter filter)
        {
            try
            {
                string strClientWhere = "";
                if (filter.Client != null)
                {
                    if (filter.Client.All(y => y != ""))
                    {
                        strClientWhere += " or cm_cd in('" + Strings.Join(filter.Client.ToArray(), "','") + "')";
                    }
                }
                if (filter.Branch != null)
                {
                    if (filter.Branch.All(y => y != ""))
                    {
                        strClientWhere += " or cm_brboffcode in('" + Strings.Join(filter.Branch.ToArray(), "','") + "')";
                    }
                }
                if (filter.Group != null)
                {
                    if (filter.Group.All(y => y != ""))
                    {
                        strClientWhere += " or cm_groupcd in('" + Strings.Join(filter.Group.ToArray(), "','") + "')";
                    }
                }
                if (filter.Family != null)
                {
                    if (filter.Family.All(y => y != ""))
                    {
                        strClientWhere += " or cm_familycd in('" + Strings.Join(filter.Family.ToArray(), "','") + "')";
                    }
                }
                if (strClientWhere.Length > 0)
                {
                    strClientWhere = " and (" + strClientWhere.Substring(3) + ") ";
                }
                var result = objUtility.OpenDataTable(strsql);
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

        public dynamic GetHolding(Filter filter)
        {
            try
            {
                string strClientWhere = "";
                if (filter.Client != null)
                {
                    if (filter.Client.All(y => y != ""))
                    {
                        strClientWhere += " or cm_cd in('" + Strings.Join(filter.Client.ToArray(), "','") + "')";
                    }
                }
                if (filter.Branch != null)
                {
                    if (filter.Branch.All(y => y != ""))
                    {
                        strClientWhere += " or cm_brboffcode in('" + Strings.Join(filter.Branch.ToArray(), "','") + "')";
                    }
                }
                if (filter.Group != null)
                {
                    if (filter.Group.All(y => y != ""))
                    {
                        strClientWhere += " or cm_groupcd in('" + Strings.Join(filter.Group.ToArray(), "','") + "')";
                    }
                }
                if (filter.Family != null)
                {
                    if (filter.Family.All(y => y != ""))
                    {
                        strClientWhere += " or cm_familycd in('" + Strings.Join(filter.Family.ToArray(), "','") + "')";
                    }
                }
                if (strClientWhere.Length > 0)
                {
                    strClientWhere = " and (" + strClientWhere.Substring(3) + ") ";
                }

                return null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }*/
    }

    #region Slip Check Class
    public class SlipCheck
    {
        public string strInwardstatus, strClientcd, strClient_Name, strClient_type, strbranchcd, strExecution_date, lngLotno, intLotsize, strSlipMode;
    }
    #endregion
}
