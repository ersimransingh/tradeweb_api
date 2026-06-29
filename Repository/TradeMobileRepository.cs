using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;
using System;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace TradeWeb.API.Repository
{
    public class TradeMobileRepository : ITradeMobileRepository
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
        public TradeMobileRepository(IConfiguration configuration, UtilityCommon objUtility, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            this.objUtility = objUtility;
            _httpContextAccessor = httpContextAccessor;
            _environment = environment;
        }
        #endregion

        public dynamic GetUserProfile(string clientCd)
        {
            string strSQL;
            string strCompanyName;

            try
            {
                if (Convert.ToInt32(objUtility.fnFireQueryTradeWeb("Entity_master", "count(0)", " em_cd='B' and em_name like 'SYKES & RAY EQUITIES%' and 1", "1", true)) > 0)
                {
                    strCompanyName = objUtility.fnFireQueryTradeWeb("Entity_master", "em_Name", "em_cd= 'B' and 1", "1", true).Trim();
                }
                else
                {
                    strCompanyName = objUtility.fnFireQueryTradeWeb("Entity_master", "em_Name", "em_cd=(select min(em_cd) from Entity_master Where Len(Ltrim(Rtrim(em_cd))) = 1) and 1", "1", true).Trim();
                }

                strSQL = " Select '" + strCompanyName + "' CompanyName,cm_cd,Rtrim(cm_Name) as cm_name,Replace(Rtrim(cm_add1),'\','/') cm_add1, Replace(Rtrim(cm_add2),'\','/') cm_add2, Replace(Rtrim(cm_add3),'\','/') cm_add3, Replace(Rtrim(cm_add4),'\','/') cm_add4 ,Rtrim(cm_pincode) as cm_pincode,Rtrim(cm_state) as cm_state,Rtrim(cm_pcountry) as cm_pcountry,Rtrim(cm_mobile) cm_tele1,Rtrim(cm_tele2) as cm_tele2,Rtrim(cm_mobile) as cm_mobile,Rtrim(cm_email) as cm_email,Rtrim(cm_panno) as cm_panno,";
                strSQL += " '' DPID, '' DpName ,'' DPActNo,'' BankMICR,'' BankIFSC,'' BankName,'' BankActType,'' BankActNo, Rtrim(cm_dob) as cm_dob ";
                strSQL += " from client_master with (nolock) inner join client_info with (nolock) on cm_cd=cm2_cd and cm_confirmwebyn <> 'N' ";
                strSQL += " and cm_cd = '" + clientCd + "' ";
                DataTable dt = objUtility.OpenDataTable(strSQL);
                if (dt.Rows.Count > 0)
                {
                    string[] arEmailPart = Strings.Split(dt.Rows[0]["cm_email"].ToString().Trim(), "@");
                    if (arEmailPart[0].Length > 2)
                    {
                        arEmailPart[0] = Strings.Mid(arEmailPart[0], 1, 2) + Strings.StrDup(arEmailPart[0].Length - 2, "x");
                    }
                    string strEmail = arEmailPart[0] + "@";
                    if (arEmailPart[1].Contains("."))
                    {
                        string[] arEmailPart2 = Strings.Split(arEmailPart[1], ".");
                        for (int i = 0; i <= arEmailPart2.Length - 1; i++)
                        {
                            if (arEmailPart2[i].Length > 2)
                            {
                                arEmailPart2[i] = Strings.Mid(arEmailPart2[i], 1, 2) + Strings.StrDup(arEmailPart2[i].Length - 2, "x");
                            }
                            strEmail += arEmailPart2[i] + ".";
                        }
                        if (strEmail.EndsWith("."))
                        {
                            strEmail = Strings.Mid(strEmail, 1, strEmail.Length - 1);
                        }
                    }
                    else
                    {
                        if (arEmailPart[1].Length > 2)
                        {
                            arEmailPart[1] = Strings.Mid(arEmailPart[1], 1, 2) + Strings.StrDup(arEmailPart[1].Length - 2, "x");
                        }
                        strEmail += arEmailPart[1];
                    }

                    dt.Rows[0]["cm_tele1"] = (dt.Rows[0]["cm_tele1"].ToString().Trim().Length > 8 ? "XXXXXX" : "XXXX") + Strings.Right(dt.Rows[0]["cm_tele1"].ToString().Trim(), 4);
                    dt.Rows[0]["cm_tele2"] = (dt.Rows[0]["cm_tele2"].ToString().Trim().Length > 8 ? "XXXXXX" : "XXXX") + Strings.Right(dt.Rows[0]["cm_tele2"].ToString().Trim(), 4);
                    dt.Rows[0]["cm_mobile"] = Strings.Mid(dt.Rows[0]["cm_mobile"].ToString().Trim(), 1, 2) + "XXXX" + Strings.Mid(dt.Rows[0]["cm_mobile"].ToString().Trim(), 7);
                    dt.Rows[0]["cm_email"] = strEmail;
                    dt.Rows[0]["cm_panno"] = "XX" + Strings.Mid(dt.Rows[0]["cm_panno"].ToString().Trim(), 3, 2) + "XX" + Strings.Mid(dt.Rows[0]["cm_panno"].ToString().Trim(), 7, 2) + "XX";
                    dt.AcceptChanges();
                }

                strSQL = "select da_dpid ,dp_name ,da_actno from Dematact,Dps ";
                strSQL += "where  da_clientcd='" + clientCd + "' and da_defaultyn='y' and da_dpid=dp_dpid ";
                DataTable dtDP = objUtility.OpenDataTable(strSQL);
                if (dtDP.Rows.Count > 0)
                {
                    string strDematNo = Strings.Left(dtDP.Rows[0]["da_actno"].ToString().Trim(), 2);
                    for (int i = 2; i < dtDP.Rows[0]["da_actno"].ToString().Trim().Length - 3; i++)
                    {
                        strDematNo += "X";
                    }
                    strDematNo += Strings.Right(dtDP.Rows[0]["da_actno"].ToString().Trim(), 2);
                    dt.Rows[0]["DPID"] = dtDP.Rows[0]["da_dpid"];
                    dt.Rows[0]["DpName"] = dtDP.Rows[0]["dp_name"];
                    dt.Rows[0]["DPActNo"] = strDematNo;
                    dt.AcceptChanges();
                }

                strSQL = "select ba_micr,ba_ifsccode,bk_name,ba_acttype ,ba_actno from  Bankact,Bank_master ";
                strSQL += " where  ba_clientcd='" + clientCd + "' and ba_default='y' and ba_micr=bk_micr ";
                if (Convert.ToInt32(objUtility.fnFireQueryTradeWeb("sysobjects a , syscolumns b", "count(0)", "a.id = b.id and b.name = 'ba_ifsccode' and a.name", "Bankact", true)) > 0)
                {
                    strSQL += " and ba_ifsccode = bk_IFCCode ";
                }
                DataTable dtBank = objUtility.OpenDataTable(strSQL);
                if (dtBank.Rows.Count > 0)
                {
                    dt.Rows[0]["BankMICR"] = dtBank.Rows[0]["ba_micr"].ToString().Trim();
                    dt.Rows[0]["BankIFSC"] = dtBank.Rows[0]["ba_ifsccode"].ToString().Trim();
                    dt.Rows[0]["BankName"] = dtBank.Rows[0]["bk_name"].ToString().Trim();
                    dt.Rows[0]["BankActType"] = dtBank.Rows[0]["ba_acttype"].ToString().Trim();
                    string strActNo = "";
                    for (int k = 0; k < dtBank.Rows[0]["ba_actno"].ToString().Trim().Length - 3; k++)
                    {
                        strActNo += "X";
                    }
                    strActNo += Strings.Right(dtBank.Rows[0]["ba_actno"].ToString().Trim(), 3);
                    dt.Rows[0]["BankActNo"] = strActNo;
                    dt.AcceptChanges();
                }

                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetLedgerYear(string clientCd)
        {
            string strSQL, strCommex = "";
            try
            {
                if (GetWebParameter("IsTradeWeb") == "O")
                {
                    strCommex = GetWebParameter("Commex");
                }
                if (GetWebParameter("IsTradeWeb") == "O")
                {
                    strSQL = " select distinct '20'+LEFT(ld_accYear ,2) + '-' + SUBSTRING(ld_accYear ,5,2) cYEar From Ledger where ld_dt <= Convert(char(8),getdate(),112) ";
                    if (strCommex != "null" & strCommex != string.Empty)
                    {
                        strSQL += " union select distinct '20'+LEFT(ld_accYear ,2) + '-' + SUBSTRING(ld_accYear ,5,2) ";
                        var ArrCommex = Strings.Split(strCommex, "/");
                        strSQL += " from   [" + ArrCommex[0] + "].[" + ArrCommex[1] + "].[" + ArrCommex[2] + "].[ledger] with (nolock) where ld_dt <= Convert(char(8),getdate(),112) ";
                    }
                }
                else
                {
                    strSQL = " select distinct '20'+LEFT(ld_accYear ,2) + '-' + SUBSTRING(ld_accYear ,5,2) cYEar From Ledger where ld_dt <= Convert(char(8),getdate(),112) ";
                    strSQL += " union select distinct '20'+LEFT(ld_accYear ,2) + '-' + SUBSTRING(ld_accYear ,5,2) From CLedger where ld_dt <= Convert(char(8),getdate(),112) ";
                }
                strSQL += " Order by cYear desc ";
                DataTable dt = objUtility.OpenDataTable(strSQL);
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetLedgerBalance(string clientCd, string strYear)
        {
            try
            {
                DataTable dtLedgerBalance = objUtility.OpenDataTable(fnGetLedgerBalanceSQL(clientCd, strYear));
                if (dtLedgerBalance.Columns.Count > 1)
                {
                    double dblOPBAL = 0;
                    double dblDebit = 0;
                    double dblCredit = 0;
                    double dblClosing = 0;
                    for (int intRow = 0, loopTo = dtLedgerBalance.Rows.Count - 1; intRow <= loopTo; intRow++)
                    {
                        dblOPBAL += Math.Round(Convert.ToDouble(Strings.Left(dtLedgerBalance.Rows[intRow][5].ToString(), dtLedgerBalance.Rows[intRow][5].ToString().Length - 3)) * (Strings.Right(dtLedgerBalance.Rows[intRow][5].ToString(), 2) == "Dr" ? 1 : -1), 2);
                        dblDebit += Math.Round(Convert.ToDouble(dtLedgerBalance.Rows[intRow][6]), 2);
                        dblCredit += Math.Round(Convert.ToDouble(dtLedgerBalance.Rows[intRow][7]), 2);
                        dblClosing += Math.Round(Convert.ToDouble(Strings.Left(dtLedgerBalance.Rows[intRow][8].ToString(), dtLedgerBalance.Rows[intRow][8].ToString().Length - 3)) * (Strings.Right(dtLedgerBalance.Rows[intRow][5].ToString(), 2) == "Dr" ? 1 : -1), 2); // + Interaction.IIf(Strings.Right(dtLedgerBalance.Rows[intRow][8].ToString(), 2) == "Dr", 1, -1)), 2);
                    }
                    DataRow dtRow = dtLedgerBalance.NewRow();
                    dtRow[0] = dtLedgerBalance.Rows[0][0];
                    dtRow[1] = "";
                    dtRow[2] = dtLedgerBalance.Rows[0][2];
                    dtRow[3] = "";
                    dtRow[4] = "";
                    dtRow[5] = Math.Abs(Math.Round(Convert.ToDouble(dblOPBAL))).ToString() + " " + ((dblOPBAL > 0) ? "Dr" : "Cr");
                    dtRow[6] = dblDebit.ToString();
                    dtRow[7] = dblCredit.ToString();
                    dtRow[8] = Math.Abs(Math.Round(Convert.ToDouble(dblClosing))).ToString() + " " + ((dblClosing > 0) ? "Dr" : "Cr");
                    dtRow[9] = "Total";
                    dtRow[10] = 99;
                    dtLedgerBalance.Rows.Add(dtRow);
                }
                return dtLedgerBalance;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetLedgerDetailsM(string clientCd, string strDPID, string strFromDt, string strToDt)
        {
            try
            {
                DataTable dtLEdgerDetails = objUtility.OpenDataTable(fnGetLedgerDetailSQL(clientCd, "", strDPID, strFromDt, strToDt, true, false));
                double dblBalance = 0d;
                //int lngRow = 0, loopTo = dtLEdgerDetails.Rows.Count - 1; lngRow <= loopTo; lngRow++
                for (int lngRow = dtLEdgerDetails.Rows.Count - 1; lngRow >= 0; lngRow--)
                {
                    dblBalance += Math.Round(Convert.ToDouble(dtLEdgerDetails.Rows[lngRow]["Debit"]), 2) - Math.Round(Convert.ToDouble(dtLEdgerDetails.Rows[lngRow]["Credit"]), 2);
                    dtLEdgerDetails.Rows[lngRow]["Balance"] = (Math.Abs(Math.Round(dblBalance, 2)) + " " + Interaction.IIf(dblBalance > 0d, " Dr", " Cr"));
                    dtLEdgerDetails.AcceptChanges();
                }
                return dtLEdgerDetails;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetTrxDates(string clientCd, string Seg)
        {
            try
            {
                var strsql = new StringBuilder();
                DataTable dtTable;
                string strCross = "", strEstro = "", strCommex = "", strCommTable = "", strBOID = "", strEstroTable = "", strCrossTable = "";
                if (objUtility.GetWebParameter("IsTradeWeb") == "O")
                {
                    strCross = objUtility.GetWebParameter("Cross");
                    strEstro = objUtility.GetWebParameter("Estro");
                    strCommex = objUtility.GetWebParameter("Commex");
                    if (strCommex != "null" & strCommex != string.Empty)
                    {
                        var ArrCommex = Strings.Split(strCommex, "/");
                        strCommTable = "[" + ArrCommex[0] + "].[" + ArrCommex[1] + "].[" + ArrCommex[2] + "].[Trades]";
                    }
                    strBOID = objUtility.fnFireQuery("dematact", "da_DPID", "da_defaultYN = 'Y' and da_clientcd", Strings.Trim(clientCd));
                    if (Strings.Left(strBOID, 2) == "IN")
                    {
                        if (strEstro != string.Empty | strEstro != "null")
                        {
                            var ArrEstro = Strings.Split(strEstro, "/");
                            strEstroTable = "[" + ArrEstro[0] + "].[" + ArrEstro[1] + "].[" + ArrEstro[2] + "].[TrxDetail]";
                        }
                    }
                    else
                    {
                        var ArrCross = Strings.Split(strCross, "/");
                        strCrossTable = "[" + ArrCross[0] + "].[" + ArrCross[1] + "].[" + ArrCross[2] + "].[TrxDetail]";
                    }
                    Seg = Seg.Trim().ToUpper();
                    if (Seg.Trim().ToUpper() == "F")
                    {
                        strsql.Append(" select distinct left(TD_Dt,4) TD_Year, SubString(TD_Dt,5,2) TD_Month, Right(TD_Dt,2) TD_Day From  ");
                        strsql.Append(" (select distinct td_dt from Trades with (nolock) where td_clientcd = '" + clientCd + "' and td_trxflag='N' ");
                        strsql.Append(" union All ");
                        strsql.Append(" Select Distinct EX_Dt from Exercise  with (nolock) where ex_clientcd = '" + clientCd + "'");
                        strsql.Append(" ) xyz ");
                        strsql.Append(" order by TD_Year Desc,SubString(TD_Dt,5,2) Desc,Right(TD_Dt,2) Desc ");
                    }
                    else if (Seg == "S")
                    {
                        strsql.Append(" Select distinct left(TD_Dt,4) TD_Year, SubString(TD_Dt,5,2) TD_Month, Right(TD_Dt,2) TD_Day from Trx with (nolock) where td_clientcd='" + clientCd + "' order by left(TD_Dt,4) Desc,SubString(TD_Dt,5,2) desc,Right(TD_Dt,2) desc ");
                    }
                    else if (Seg == "C" & strCommTable != "")
                    {
                        strsql.Append(" select distinct left(TD_Dt,4) TD_Year, SubString(TD_Dt,5,2) TD_Month, Right(TD_Dt,2) TD_Day from " + strCommTable + " with (nolock) where td_clientcd='" + clientCd + "' and td_trxflag='N' order by left(TD_Dt,4) Desc,SubString(TD_Dt,5,2) desc,Right(TD_Dt,2) desc ");
                    }
                    else if (Seg == "D")
                    {
                        //strsql.Append(((((" select distinct left(td_trxdate ,4) TD_Year, SubString(td_trxdate ,5,2) TD_Month, Right(td_trxdate ,2) TD_Day from ", Interaction.IIf(strEstroTable != "", strEstroTable, strCrossTable)), " with (nolock) , DematAct with (nolock) where TD_Ac_Code=DA_ActNo and da_clientcd='"), clientCd), "' order by left(td_TrxDate,4) Desc,SubString(td_TrxDate,5,2) desc,Right(td_TrxDate,2) desc "));
                        strsql.Append(" select distinct left(td_trxdate ,4) TD_Year, SubString(td_trxdate ,5,2) TD_Month, Right(td_trxdate ,2) TD_Day from " + ((strEstroTable != "") ? strEstroTable : strCrossTable) + " with (nolock) , DematAct with (nolock) where TD_Ac_Code=DA_ActNo and da_clientcd='" + clientCd + "' order by left(td_TrxDate,4) Desc,SubString(td_TrxDate,5,2) desc,Right(td_TrxDate,2) desc ");
                    }
                    else if (Seg == "R" | Seg == "P" | Seg == "J" | Seg == "B")
                    {
                        strsql.Append("Select distinct left(LD_Dt ,4) TD_Year, SubString(LD_Dt ,5,2) TD_Month, Right(Rtrim(LD_Dt),2) TD_Day from Ledger where ld_Documenttype='" + Seg + "' and ld_ClientCd='" + clientCd + "' order by left(LD_Dt,4) Desc,SubString(LD_Dt,5,2) desc,Right(Rtrim(LD_Dt),2) desc ");
                    }
                }
                else
                {
                    // '''tradeweb
                    Seg = Seg.Trim().ToUpper();
                    if (Seg.Trim().ToUpper() == "F")
                    {
                        strsql.Append(" select distinct left(TD_Dt,4) TD_Year, SubString(TD_Dt,5,2) TD_Month, Right(TD_Dt,2) TD_Day From  ");
                        strsql.Append(" (select distinct td_dt from Trades with (nolock) where td_clientcd = '" + clientCd + "' and td_trxflag='N' ");
                        strsql.Append(" union All ");
                        strsql.Append(" Select Distinct EX_Dt from Exercise  with (nolock) where ex_clientcd = '" + clientCd + "'");
                        strsql.Append(" ) xyz ");
                        strsql.Append(" order by TD_Year Desc,SubString(TD_Dt,5,2) Desc,Right(TD_Dt,2) Desc ");
                    }
                    else if (Seg == "S")
                    {
                        strsql.Append(" Select distinct left(TD_Dt,4) TD_Year, SubString(TD_Dt,5,2) TD_Month, Right(TD_Dt,2) TD_Day from Trx with (nolock) where td_clientcd='" + clientCd + "' order by left(TD_Dt,4) Desc,SubString(TD_Dt,5,2) desc,Right(TD_Dt,2) desc ");

                    }
                    else if (Seg == "C")
                    {
                        strsql.Append(" select distinct left(TD_Dt,4) TD_Year, SubString(TD_Dt,5,2) TD_Month, Right(TD_Dt,2) TD_Day from cTrades with (nolock) where td_clientcd='" + clientCd + "' and td_trxflag='N' order by left(TD_Dt,4) Desc,SubString(TD_Dt,5,2) desc,Right(TD_Dt,2) desc ");
                    }
                    else if (Seg == "D")
                    {
                        strsql.Append(" select distinct left(td_trxdate ,4) TD_Year, SubString(td_trxdate ,5,2) TD_Month, Right(td_trxdate ,2) TD_Day from trxweb with (nolock) , DematAct with (nolock) where td_DPID+TD_Ac_Code=DA_ActNo and da_clientcd='" + clientCd + "' order by left(td_TrxDate,4) Desc,SubString(td_TrxDate,5,2) desc,Right(td_TrxDate,2) desc ");
                    }
                    else if (Seg == "R" | Seg == "P" | Seg == "J" | Seg == "B")
                    {
                        strsql.Append(" Select distinct left(LD_Dt ,4) TD_Year, SubString(LD_Dt ,5,2) TD_Month, Right(Rtrim(LD_Dt),2) TD_Day from Ledger where ld_Documenttype='" + Seg + "' and ld_ClientCd='" + clientCd + "' order by left(LD_Dt,4) Desc,SubString(LD_Dt,5,2) desc,Right(Rtrim(LD_Dt),2) desc ");
                    }
                }
                if (strsql.Length == 0)
                    strsql.Append("Select 'No Record Found' ErrorMG");
                dtTable = objUtility.OpenDataTable(Convert.ToString(strsql));
                return dtTable;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetTradesForDate(string clientCd, string StartDt, string Seg)
        {
            try
            {
                var strsql = new StringBuilder();
                DataTable dtTable;
                Seg = Seg.Trim().ToUpper();
                string StrTRXIndex = "", strCross = "", strEstro = "", strCommex = "", strBOID = "", strEstroTable = "", strCrossTable = "", strCommTable = "";
                if (Convert.ToInt16(objUtility.fnFireQuery("sysobjects a, sysindexes b", "COUNT(0)", "a.id = b.id and a.name = 'Trx' and b.name", "idx_Trx_Clientcd", true)) == 1)
                {
                    StrTRXIndex = "index(idx_trx_clientcd),";
                }

                string StrTradesIndex = "";
                if (Convert.ToInt16(objUtility.fnFireQuery("sysobjects a, sysindexes b", "COUNT(0)", "a.id = b.id and a.name = 'trades' and b.name", "idx_trades_clientcd", true)) == 1)
                {
                    StrTradesIndex = "index(idx_trades_clientcd),";
                }

                if (objUtility.GetWebParameter("IsTradeWeb") == "O")
                {
                    strCross = objUtility.GetWebParameter("Cross");
                    strEstro = objUtility.GetWebParameter("Estro");
                    strCommex = objUtility.GetWebParameter("Commex");
                    strBOID = objUtility.fnFireQuery("dematact", "da_DPID", "da_defaultYN = 'Y' and da_clientcd", Strings.Trim(clientCd));
                    if (Strings.Left(strBOID, 2) == "IN")
                    {
                        if (strEstro != string.Empty | strEstro != "null")
                        {
                            var ArrEstro = Strings.Split(strEstro, "/");
                            strEstroTable = "[" + ArrEstro[0] + "].[" + ArrEstro[1] + "].[" + ArrEstro[2] + "]";
                        }
                    }
                    else
                    {
                        var ArrCross = Strings.Split(strCross, "/");
                        strCrossTable = "[" + ArrCross[0] + "].[" + ArrCross[1] + "].[" + ArrCross[2] + "]";
                    }

                    if (Seg.Trim().ToUpper() == "F")
                    {
                        strsql.Append(" select Rtrim(sm_productcd) + ' ' + Rtrim(sm_symbol)  + case When Right(Rtrim(sm_prodtype),1) = 'O' Then ' ' + Ltrim(Rtrim(Convert(char,sm_strikeprice))) + ' ' + sm_callput + sm_optionstyle else '' end ss_lname, sm_expirydt td_stlmnt , sum(td_bqty) Bqty, convert(decimal(15,2),sum(td_bqty*td_rate*sm_multiplier)) BAmt,   sum(td_sqty) Sqty,  ");
                        strsql.Append(" convert(decimal(15,2),sum(td_sqty*td_rate*sm_multiplier)) SAmt, sum(td_bqty-td_sqty) NQty, convert(decimal(15,2),sum((td_bqty-td_sqty)*td_rate *sm_multiplier)) NAmt, ");
                        strsql.Append(" cast((case when  sum(td_bqty-td_sqty)=0 then 0 else sum((td_bqty-td_sqty)*td_rate)/sum(td_bqty-td_sqty) end)as decimal(15,2) ) Avgrate ");
                        strsql.Append(" from trades with(" + StrTradesIndex + "nolock), series_master with (nolock)   ");
                        strsql.Append(" where td_clientcd='" + clientCd + "' and sm_exchange=td_exchange and sm_Segment=td_Segment and td_seriesid=sm_seriesid and td_dt='" + StartDt + "' and td_trxflag <> 'O' ");
                        strsql.Append(" group by td_exchange,td_Segment,td_dt, sm_productcd,sm_prodtype, sm_desc,sm_expirydt, ");
                        strsql.Append(" sm_productcd,sm_symbol,sm_strikeprice,sm_prodtype,sm_callput,sm_optionstyle ");
                        strsql.Append(" order by sm_desc ");
                    }
                    else if (Seg == "S")
                    {
                        strsql.Append(" select ss_lname, Case left(td_stlmnt,1) When 'B' Then 'BSE' When 'N' Then 'NSE' When 'M' Then 'MCX' else '?' end +'/'+rtrim(td_stlmnt) td_stlmnt,  ");
                        strsql.Append(" sum(td_bqty) Bqty, convert(decimal(15,2),sum(td_bqty*td_rate)) BAmt, sum(td_sqty) Sqty, convert(decimal(15,2),sum(td_sqty*td_rate)) SAmt,  ");
                        strsql.Append(" sum(td_bqty-td_sqty) NQty, convert(decimal(15,2),sum((td_bqty-td_sqty)*td_rate)) NAmt,");
                        strsql.Append(" cast((case when sum(td_bqty-td_sqty)=0 then 0 else sum((td_bqty-td_sqty)*td_rate)/sum(td_bqty-td_sqty) end)as decimal(15,2) ) as Avgrate ");
                        strsql.Append(" from trx with(" + StrTRXIndex + "nolock) ,securities  with(nolock) ");
                        strsql.Append(" where td_clientcd='" + clientCd + "' and td_Scripcd = ss_cd and td_dt='" + StartDt + "'");
                        strsql.Append(" group by td_dt , td_stlmnt, ss_lname ");
                    }
                    else if (Seg == "C")
                    {
                        if (strCommex != "NULL" & strCommex != string.Empty)
                        {
                            var ArrCommex = Strings.Split(strCommex, "/");
                            strCommTable = "[" + ArrCommex[0] + "].[" + ArrCommex[1] + "].[" + ArrCommex[2] + "]";
                            strsql.Append(" select Rtrim(sm_productcd) + ' ' + Rtrim(sm_symbol)  + case When Right(Rtrim(sm_prodtype),1) = 'O' Then ' ' + Ltrim(Rtrim(Convert(char,sm_strikeprice))) + ' ' + sm_callput + sm_optionstyle else '' end ss_lname, sm_expirydt td_stlmnt ,  sum(td_bqty) Bqty, convert(decimal(15,2), sum(td_bqty*td_rate *sm_multiplier)) BAmt,   ");
                            strsql.Append(" sum(td_sqty) Sqty, convert(decimal(15,2), sum(td_sqty*td_rate*sm_multiplier)) SAmt,   ");
                            strsql.Append(" sum(td_bqty-td_sqty) NQty, convert(decimal(15,2),sum((td_bqty-td_sqty)*td_rate*sm_multiplier)) NAmt, ");
                            strsql.Append(" cast((case when  sum(td_bqty-td_sqty)=0 then 0 else  sum((td_bqty-td_sqty)*td_rate)/sum(td_bqty-td_sqty) end)as decimal(15,2) ) as Avgrate ");
                            strsql.Append(" from " + strCommTable + ".Trades with(nolock), " + strCommTable + " .series_master with(nolock)  ");
                            strsql.Append(" where td_clientcd='" + clientCd + "' and sm_exchange=td_exchange and td_seriesid=sm_seriesid and td_dt='" + StartDt + "' and td_trxflag <> 'O'");
                            strsql.Append(" group by sm_desc, sm_productcd,td_exchange, td_dt, sm_prodtype,sm_expirydt, ");
                            strsql.Append(" sm_productcd,sm_symbol,sm_strikeprice,sm_prodtype,sm_callput,sm_optionstyle ");
                            strsql.Append(" order by sm_desc ");
                        }
                    }
                    else if (Seg.Trim().ToUpper() == "R" | Seg.Trim().ToUpper() == "P" | Seg.Trim().ToUpper() == "J" | Seg.Trim().ToUpper() == "B")
                    {
                        strsql.Append(" select ld_documentno RefNo,Replace(ld_particular,'\','/') Particular ,  ");
                        strsql.Append(" Rtrim(Ltrim(CONVERT(char,convert(decimal(15,2), abs(ld_amount))))) + Case ld_DocumentType When 'J' Then ' ' + ld_debitFlag + 'r' When 'B' Then Case ld_debitFlag When 'D' Then ' [PayIn]' else ' [PayOut]' End else '' end Amount ");
                        strsql.Append(" from ledger with(nolock) ");
                        strsql.Append(" where ld_clientcd= '" + clientCd + "' and ld_documentType = '" + Seg + "' and ld_dt='" + StartDt + "'");
                        strsql.Append(" order by ld_documentno ");
                    }
                    else if (Seg == "D")
                    {
                        strsql.Append(" select sc_isincode ISIN,sc_isinname ss_lname,'' Particulars,");
                        strsql.Append(" (case td_debit_credit When 'C' Then td_qty else 0 end) Inward, (case td_debit_credit When 'D' Then td_qty else 0 end) Outward,td_trxdate td_dt");
                        strsql.Append(" from  " + ((strEstroTable != "") ? strEstroTable : strCrossTable) + ".TrxDetail with (nolock), DematAct with(nolock), " + ((strEstroTable != "") ? strEstroTable : strCrossTable) + " .Security with(nolock)");
                        strsql.Append(" where TD_Ac_Code=DA_ActNo and td_isin_code = sc_isincode and da_clientcd='" + clientCd + "' and td_trxdate='" + StartDt + "'");
                        strsql.Append(" order by sc_company_name ");
                    }
                }
                else if (Seg.Trim().ToUpper() == "F")
                {
                    strsql.Append(" select Rtrim(sm_productcd) + ' ' + Rtrim(sm_symbol)  + case When Right(Rtrim(sm_prodtype),1) = 'O' Then ' ' + Ltrim(Rtrim(Convert(char,sm_strikeprice))) + ' ' + sm_callput + sm_optionstyle else '' end ss_lname, sm_expirydt td_stlmnt , sum(td_bqty) Bqty, convert(decimal(15,2),sum(td_bqty*td_rate*sm_multiplier)) BAmt,   sum(td_sqty) Sqty,  ");
                    strsql.Append(" convert(decimal(15,2),sum(td_sqty*td_rate*sm_multiplier)) SAmt, sum(td_bqty-td_sqty) NQty, convert(decimal(15,2),sum((td_bqty-td_sqty)*td_rate *sm_multiplier)) NAmt, ");
                    strsql.Append(" cast((case when  sum(td_bqty-td_sqty)=0 then 0 else sum((td_bqty-td_sqty)*td_rate)/sum(td_bqty-td_sqty) end)as decimal(15,2) ) Avgrate ");
                    strsql.Append(" from trades with(" + StrTradesIndex + "nolock), series_master with (nolock)   ");
                    strsql.Append(" where td_clientcd='" + clientCd + "' and sm_exchange=td_exchange and sm_Segment=td_Segment and td_seriesid=sm_seriesid and td_dt='" + StartDt + "' and td_trxflag <> 'O' ");
                    strsql.Append(" group by td_exchange,td_Segment,td_dt, sm_productcd,sm_prodtype, sm_desc,sm_expirydt, ");
                    strsql.Append(" sm_productcd,sm_symbol,sm_strikeprice,sm_prodtype,sm_callput,sm_optionstyle ");
                    strsql.Append(" order by sm_desc ");
                }
                else if (Seg == "S")
                {
                    strsql.Append("  select ss_lname, Case left(td_stlmnt,1) When 'B' Then 'BSE' When 'N' Then 'NSE' When 'M' Then 'MCX' else '?' end +'/'+rtrim(td_stlmnt) td_stlmnt,  ");
                    strsql.Append(" sum(td_bqty) Bqty, convert(decimal(15,2),sum(td_bqty*td_rate)) BAmt, sum(td_sqty) Sqty, convert(decimal(15,2),sum(td_sqty*td_rate)) SAmt,  ");
                    strsql.Append(" sum(td_bqty-td_sqty) NQty, convert(decimal(15,2),sum((td_bqty-td_sqty)*td_rate)) NAmt,");
                    strsql.Append(" cast((case when sum(td_bqty-td_sqty)=0 then 0 else sum((td_bqty-td_sqty)*td_rate)/sum(td_bqty-td_sqty) end)as decimal(15,2) ) as Avgrate ");
                    strsql.Append(" from trx with(" + StrTRXIndex + "nolock) , TPsecurities  with(nolock) ");
                    strsql.Append(" where td_clientcd='" + clientCd + "' and td_Scripcd = ss_cd and td_dt='" + StartDt + "'");
                    strsql.Append(" group by td_dt , td_stlmnt, ss_lname ");
                }
                else if (Seg == "C")
                {
                    strsql.Append(" select Rtrim(sm_productcd) + ' ' + Rtrim(sm_symbol)  + case When Right(Rtrim(sm_prodtype),1) = 'O' Then ' ' + Ltrim(Rtrim(Convert(char,sm_strikeprice))) + ' ' + sm_callput + sm_optionstyle else '' end ss_lname, sm_expirydt td_stlmnt ,  sum(td_bqty) Bqty, convert(decimal(15,2), sum(td_bqty*td_rate *sm_multiplier)) BAmt,   ");
                    strsql.Append(" sum(td_sqty) Sqty, convert(decimal(15,2), sum(td_sqty*td_rate*sm_multiplier)) SAmt,   ");
                    strsql.Append(" sum(td_bqty-td_sqty) NQty, convert(decimal(15,2),sum((td_bqty-td_sqty)*td_rate*sm_multiplier)) NAmt, ");
                    strsql.Append(" cast((case when  sum(td_bqty-td_sqty)=0 then 0 else  sum((td_bqty-td_sqty)*td_rate)/sum(td_bqty-td_sqty) end)as decimal(15,2) ) as Avgrate ");
                    strsql.Append(" from Ctrades with(nolock), Cseries_master with(nolock)  ");
                    strsql.Append(" where td_clientcd='" + clientCd + "' and sm_exchange=td_exchange and td_seriesid=sm_seriesid and td_dt='" + StartDt + "' and td_trxflag <> 'O'");
                    strsql.Append(" group by sm_desc, sm_productcd,td_exchange, td_dt, sm_prodtype,sm_expirydt, ");
                    strsql.Append(" sm_productcd,sm_symbol,sm_strikeprice,sm_prodtype,sm_callput,sm_optionstyle ");
                    strsql.Append(" order by sm_desc ");
                }
                else if (Seg.Trim().ToUpper() == "R" | Seg.Trim().ToUpper() == "P" | Seg.Trim().ToUpper() == "J" | Seg.Trim().ToUpper() == "B")
                {
                    strsql.Append(" select ld_documentno RefNo,Replace(ld_particular,'\','/') Particular ,  ");
                    strsql.Append(" Rtrim(Ltrim(CONVERT(char,convert(decimal(15,2), abs(ld_amount))))) + Case ld_DocumentType When 'J' Then ' ' + ld_debitFlag + 'r' When 'B' Then Case ld_debitFlag When 'D' Then ' [PayIn]' else ' [PayOut]' End else '' end Amount ");
                    strsql.Append(" from ledger with(nolock) ");
                    strsql.Append(" where ld_clientcd= '" + clientCd + "' and ld_documentType = '" + Seg + "' and ld_dt='" + StartDt + "'");
                    strsql.Append(" order by ld_documentno ");
                }
                else if (Seg == "D")
                {
                    strsql.Append(" select sc_isincode ISIN,sc_isinname ss_lname,td_text Particulars,");
                    strsql.Append(" (case td_debit_credit When 'C' Then td_qty else 0 end) Inward, (case td_debit_credit When 'D' Then td_qty else 0 end) Outward,td_trxdate td_dt");
                    strsql.Append(" from trxweb with (nolock), DematAct with(nolock),Security with(nolock)");
                    strsql.Append(" where td_DPID+TD_Ac_Code=DA_ActNo and td_isin_code = sc_isincode and da_clientcd='" + clientCd + "' and td_trxdate='" + StartDt + "'");
                    strsql.Append(" order by sc_company_name ");
                }
                if (strsql.Length == 0)
                    strsql.Append("Select 'No Record Found' ErrorMG");
                dtTable = objUtility.OpenDataTable(Convert.ToString(strsql));
                return dtTable;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetTrxItems(string clientCd, string Seg)
        {
            try
            {
                DataTable dtTable;
                var strsql = new StringBuilder();
                Seg = Seg.Trim().ToUpper();
                string strCross = "", strEstro = "", strCommex = "", strBOID = "", strEstroTable = "", strCrossTable = "", strCommTable = "";
                if (objUtility.GetWebParameter("IsTradeWeb") == "O")
                {
                    strCross = objUtility.GetWebParameter("Cross");
                    strEstro = objUtility.GetWebParameter("Estro");
                    strCommex = objUtility.GetWebParameter("Commex");
                    strBOID = objUtility.fnFireQuery("dematact", "da_DPID", "da_defaultYN = 'Y' and da_clientcd", Strings.Trim(clientCd));
                    if (Strings.Left(strBOID, 2) == "IN")
                    {
                        if (strEstro != string.Empty | strEstro != "null")
                        {
                            var ArrEstro = Strings.Split(strEstro, "/");
                            strEstroTable = "[" + ArrEstro[0] + "].[" + ArrEstro[1] + "].[" + ArrEstro[2] + "]";
                        }
                    }
                    else
                    {
                        var ArrCross = Strings.Split(strCross, "/");
                        strCrossTable = "[" + ArrCross[0] + "].[" + ArrCross[1] + "].[" + ArrCross[2] + "]";
                    }
                    if (Seg.Trim().ToUpper() == "F")
                    {
                        strsql.Append(" select distinct TD_Security , SS_Cd From  ");
                        strsql.Append(" (select distinct SM_Underlying TD_Security,SM_Underlying SS_Cd  from Trades with (nolock), series_master with (nolock) ");
                        strsql.Append(" where td_exchange+TD_Segment=SM_Exchange+SM_Segment and TD_SeriesId=SM_SeriesID and td_clientcd='" + clientCd + "'");
                        strsql.Append(" union All ");
                        strsql.Append(" Select Distinct SM_Underlying TD_Security,SM_Underlying SS_Cd from Exercise with (nolock), Series_Master with (nolock) ");
                        strsql.Append("  where ex_exchange+ex_Segment=SM_Exchange+SM_Segment and ex_SeriesId=SM_SeriesID and ex_clientcd='" + clientCd + "'");
                        strsql.Append(" ) xyz  ");
                        strsql.Append(" order by TD_Security ");
                    }
                    else if (Seg == "S")
                    {
                        strsql.Append(" Select distinct case ss_lName when '' then ss_name else ss_lname end TD_Security, SS_Cd from Trx with (nolock), securities with (nolock) where td_scripcd=ss_cd and td_clientcd='" + clientCd + "' order by td_security ");
                    }
                    else if (Seg == "C")
                    {
                        if (strCommex != "NULL" & strCommex != string.Empty)
                        {
                            var ArrCommex = Strings.Split(strCommex, "/");
                            strCommTable = "[" + ArrCommex[0] + "].[" + ArrCommex[1] + "].[" + ArrCommex[2] + "]";
                            strsql.Append(" select distinct SM_Underlying TD_Security, SM_Underlying SS_Cd  from " + strCommTable + " .Trades with (nolock),  " + strCommTable + ".series_master with (nolock)  where td_exchange=SM_Exchange and TD_SeriesId=SM_SeriesID and td_clientcd='" + clientCd + "' order by sm_underlying ");
                        }
                    }
                    else if (Seg == "D")
                    {
                        strsql.Append(" select distinct sc_isinname TD_Security, sc_isincode SS_Cd from " + ((strEstroTable != "") ? strEstroTable : strCrossTable) + ".trxdetail with (nolock), DematAct with (nolock) ," + ((strEstroTable != "") ? strEstroTable : strCrossTable) + " .Security with (nolock)  where TD_Ac_Code=DA_ActNo and td_isin_code=sc_isincode and da_clientcd='" + clientCd + "' order by sc_isinname ");
                    }
                }

                // ''''''''''''''''''''''''''tradeweb''''''''''''''''''''''''''''''''''''
                else if (Seg.Trim().ToUpper() == "F")
                {
                    strsql.Append(" select distinct TD_Security , SS_Cd From  ");
                    strsql.Append(" (select distinct SM_Underlying TD_Security,SM_Underlying SS_Cd  from Trades with (nolock), series_master with (nolock) ");
                    strsql.Append(" where td_exchange+TD_Segment=SM_Exchange+SM_Segment and TD_SeriesId=SM_SeriesID and td_clientcd='" + clientCd + "'");
                    strsql.Append(" union All ");
                    strsql.Append(" Select Distinct SM_Underlying TD_Security,SM_Underlying SS_Cd from Exercise with (nolock), Series_Master with (nolock) ");
                    strsql.Append("  where ex_exchange+ex_Segment=SM_Exchange+SM_Segment and ex_SeriesId=SM_SeriesID and ex_clientcd='" + clientCd + "'");
                    strsql.Append(" ) xyz  ");
                    strsql.Append(" order by TD_Security ");
                }
                else if (Seg == "S")
                {
                    strsql.Append(" Select distinct case ss_lName when '' then ss_name else ss_lname end TD_Security, SS_Cd from Trx with (nolock), TPsecurities with (nolock) where td_scripcd=ss_cd and td_clientcd='" + clientCd + "' order by td_security ");
                }
                else if (Seg == "C")
                {
                    strsql.Append(" select distinct SM_Underlying TD_Security, SM_Underlying SS_Cd  from cTrades with (nolock), cseries_master with (nolock)  where td_exchange=SM_Exchange and TD_SeriesId=SM_SeriesID and td_clientcd='" + clientCd + "' order by sm_underlying ");
                }
                else if (Seg == "D")
                {
                    strsql.Append(" select distinct sc_isinname TD_Security, sc_isincode SS_Cd from trxweb with (nolock), DematAct with (nolock) ,Security with (nolock)  where td_DPID+TD_Ac_Code=DA_ActNo and td_isin_code=sc_isincode and da_clientcd='" + clientCd + "' order by sc_isinname ");
                }
                if (strsql.Length == 0)
                    strsql.Append("Select 'NO Record found' ErrorMG");
                dtTable = objUtility.OpenDataTable(Convert.ToString(strsql));
                return dtTable;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetTrxItemsDetail(string clientCd, string Seg, string ScripCd)
        {
            try
            {
                DataTable dtTable;
                var strsql = new StringBuilder();
                string StrTRXIndex = "", StrTradesIndex = "", strCross = "", strEstro = "", strCommex = "", strBOID = "", strEstroTable = "", strCrossTable = "", strCommTable = "";
                if (Convert.ToInt16(objUtility.fnFireQuery("sysobjects a, sysindexes b", "COUNT(0)", "a.id = b.id and a.name = 'Trx' and b.name", "idx_Trx_Clientcd", true)) == 1)
                {
                    StrTRXIndex = "index(idx_trx_clientcd),";
                }
                if (Convert.ToInt16(objUtility.fnFireQuery("sysobjects a, sysindexes b", "COUNT(0)", "a.id = b.id and a.name = 'trades' and b.name", "idx_trades_clientcd", true)) == 1)
                {
                    StrTradesIndex = "index(idx_trades_clientcd),";
                }

                Seg = Seg.Trim().ToUpper();
                if (objUtility.GetWebParameter("IsTradeWeb") == "O")  // ''''''''live db
                {
                    strCross = objUtility.GetWebParameter("Cross");
                    strEstro = objUtility.GetWebParameter("Estro");
                    strCommex = objUtility.GetWebParameter("Commex");
                    strBOID = objUtility.fnFireQuery("dematact", "da_DPID", "da_defaultYN = 'Y' and da_clientcd", Strings.Trim(clientCd));
                    if (Strings.Left(strBOID, 2) == "IN")
                    {
                        if (strEstro != string.Empty | strEstro != "null")
                        {
                            var ArrEstro = Strings.Split(strEstro, "/");
                            strEstroTable = "[" + ArrEstro[0] + "].[" + ArrEstro[1] + "].[" + ArrEstro[2] + "]";
                        }
                    }
                    else
                    {
                        var ArrCross = Strings.Split(strCross, "/");
                        strCrossTable = "[" + ArrCross[0] + "].[" + ArrCross[1] + "].[" + ArrCross[2] + "]";
                    }

                    if (Seg.Trim().ToUpper() == "F")
                    {
                        strsql.Append("Select convert(char,convert(dateTime,td_dt),103) TradeDate,Ltrim(Rtrim(convert(char,convert(dateTime,sm_Expirydt ),103))) + case left(sm_productcd,1) when 'F' then '' else ' ' + rtrim(sm_callput)+' '+ltrim(convert(char,round(sm_strikeprice,0))) end td_stlmnt,");
                        strsql.Append(" sum(td_bqty) Bqty, convert(decimal(15,2),sum(td_bqty*td_rate*sm_multiplier)) BAmt,   sum(td_sqty) Sqty,  ");
                        strsql.Append(" convert(decimal(15,2),sum(td_sqty*td_rate*sm_multiplier)) SAmt, sum(td_bqty-td_sqty) NQty, convert(decimal(15,2),sum((td_bqty-td_sqty)*td_rate *sm_multiplier)) NAmt, ");
                        strsql.Append(" cast((case when  sum(td_bqty-td_sqty)=0 then 0 else sum((td_bqty-td_sqty)*td_rate)/sum(td_bqty-td_sqty) end)as decimal(15,2) ) Avgrate,td_dt");
                        strsql.Append(" from trades with(" + StrTradesIndex + "nolock), series_master with (nolock) ");
                        strsql.Append(" where td_exchange+TD_Segment=SM_Exchange+SM_Segment and TD_SeriesId=SM_SeriesID and td_trxflag <> 'O'");
                        strsql.Append(" and td_clientcd='" + clientCd + "' and sm_symbol = '" + ScripCd + "'");
                        strsql.Append(" Group by td_dt,sm_Expirydt ,case left(sm_productcd,1) when 'F' then '' else ' ' + rtrim(sm_callput)+' '+ltrim(convert(char,round(sm_strikeprice,0))) end ");
                        strsql.Append(" union all ");
                        strsql.Append(" Select convert(char,convert(dateTime,ex_dt),103) TradeDate,Ltrim(Rtrim(convert(char,convert(dateTime,sm_Expirydt ),103))) + case left(sm_productcd,1) when 'F' then '' else ' ' + rtrim(sm_callput)+' '+ltrim(convert(char,round(sm_strikeprice,0))) end td_stlmnt,");
                        strsql.Append(" sum(ex_aqty) Bqty, convert(decimal(15,2),sum(ex_aqty*ex_diffrate*sm_multiplier)) BAmt,   sum(ex_eqty) Sqty,  ");
                        strsql.Append(" convert(decimal(15,2),sum(ex_eqty*ex_diffrate*sm_multiplier)) SAmt, sum(ex_aqty-ex_eqty) NQty, convert(decimal(15,2),sum((ex_aqty-ex_eqty)*ex_diffrate *sm_multiplier)) NAmt, ");
                        strsql.Append(" cast((case when  sum(ex_aqty-ex_eqty)=0 then 0 else sum((ex_aqty-ex_eqty)*ex_diffrate)/sum(ex_aqty-ex_eqty) end)as decimal(15,2) ) Avgrate,EX_dt");
                        strsql.Append(" from Exercise with (nolock) , series_master with (nolock) ");
                        strsql.Append(" where ex_exchange+ex_Segment=SM_Exchange+SM_Segment and ex_SeriesId=SM_SeriesID ");
                        strsql.Append(" and ex_clientcd='" + clientCd + "' and sm_symbol = '" + ScripCd + "'");
                        strsql.Append(" Group by ex_dt,sm_Expirydt ,case left(sm_productcd,1) when 'F' then '' else ' ' + rtrim(sm_callput)+' '+ltrim(convert(char,round(sm_strikeprice,0))) end ");
                        strsql.Append(" order by td_dt desc,td_stlmnt");
                    }
                    else if (Seg == "S")
                    {
                        strsql.Append(" Select convert(char,convert(dateTime,td_dt),103) TradeDate, Case left(td_stlmnt,1) When 'B' Then 'BSE' When 'N' Then 'NSE' When 'M' Then 'MCX' else '?' end +'/'+rtrim(td_stlmnt) td_stlmnt,");
                        strsql.Append(" sum(td_bqty) Bqty, convert(decimal(15,2),sum(td_bqty*td_rate)) BAmt, sum(td_sqty) Sqty, convert(decimal(15,2),sum(td_sqty*td_rate)) SAmt,  ");
                        strsql.Append(" sum(td_bqty-td_sqty) NQty, convert(decimal(15,2),sum((td_bqty-td_sqty)*td_rate)) NAmt,");
                        strsql.Append(" cast((case when sum(td_bqty-td_sqty)=0 then 0 else sum((td_bqty-td_sqty)*td_rate)/sum(td_bqty-td_sqty) end)as decimal(15,2) ) as Avgrate,td_dt");
                        strsql.Append(" from  trx with(" + StrTRXIndex + "nolock), securities with (nolock)  ");
                        strsql.Append(" where td_scripcd=ss_cd and td_clientcd='" + clientCd + "' and td_Scripcd = '" + ScripCd + "'");
                        strsql.Append(" Group by td_dt ,td_stlmnt  ");
                        strsql.Append(" order by td_dt desc,td_stlmnt");
                    }
                    else if (Seg == "C")
                    {
                        if (strCommex != "NULL" & strCommex != string.Empty)
                        {
                            var ArrCommex = Strings.Split(strCommex, "/");
                            strCommTable = "[" + ArrCommex[0] + "].[" + ArrCommex[1] + "].[" + ArrCommex[2] + "]";
                            strsql.Append(" Select convert(char,convert(dateTime,td_dt),103) TradeDate,Ltrim(Rtrim(convert(char,convert(dateTime,sm_Expirydt ),103))) + case left(sm_productcd,1) when 'F' then '' else ' ' + rtrim(sm_callput)+' '+ltrim(convert(char,round(sm_strikeprice,0))) end td_stlmnt,");
                            strsql.Append(" sum(td_bqty) Bqty, convert(decimal(15,2),sum(td_bqty*td_rate*sm_multiplier)) BAmt,   sum(td_sqty) Sqty,  ");
                            strsql.Append(" convert(decimal(15,2),sum(td_sqty*td_rate*sm_multiplier)) SAmt, sum(td_bqty-td_sqty) NQty, convert(decimal(15,2),sum((td_bqty-td_sqty)*td_rate *sm_multiplier)) NAmt, ");
                            strsql.Append(" cast((case when  sum(td_bqty-td_sqty)=0 then 0 else sum((td_bqty-td_sqty)*td_rate)/sum(td_bqty-td_sqty) end)as decimal(15,2) ) Avgrate,td_dt");
                            strsql.Append(" from  " + strCommTable + ".trades with (nolock), " + strCommTable + ".series_master with (nolock)");
                            strsql.Append(" where td_exchange=SM_Exchange and TD_SeriesId=SM_SeriesID and td_trxflag <> 'O'");
                            strsql.Append(" and td_clientcd='" + clientCd + "' and sm_symbol = '" + ScripCd + "'");
                            strsql.Append(" Group by td_dt,Ltrim(Rtrim(convert(char,convert(dateTime,sm_Expirydt ),103))) + case left(sm_productcd,1) when 'F' then '' else ' ' + rtrim(sm_callput)+' '+ltrim(convert(char,round(sm_strikeprice,0))) end");
                            strsql.Append(" order by td_dt desc,td_stlmnt");
                        }
                    }
                    else if (Seg == "D")
                    {
                        strsql.Append(" select convert(char,convert(dateTime,td_trxdate),103) TradeDate ,td_description Particulars,");
                        strsql.Append(" (case td_debit_credit When 'C' Then td_qty else 0 end) Inward, (case td_debit_credit When 'D' Then td_qty else 0 end) Outward,td_trxdate td_dt");
                        strsql.Append(" from " + ((strEstroTable != "") ? strEstroTable : strCrossTable) + ".TrxDetail with (nolock), DematAct with (nolock), " + ((strEstroTable != "") ? strEstroTable : strCrossTable) + ".Security with (nolock) where TD_Ac_Code=DA_ActNo and td_isin_code=sc_isincode ");
                        strsql.Append(" and da_clientcd='" + clientCd + "' and sc_isincode= '" + ScripCd + "'");
                        strsql.Append(" order by td_trxdate desc ");
                    }
                }
                // '''''''''''''''''''''tradeweb'''''''''''''''
                else if (Seg.Trim().ToUpper() == "F")
                {
                    strsql.Append("Select convert(char,convert(dateTime,td_dt),103) TradeDate,Ltrim(Rtrim(convert(char,convert(dateTime,sm_Expirydt ),103))) + case left(sm_productcd,1) when 'F' then '' else ' ' + rtrim(sm_callput)+' '+ltrim(convert(char,round(sm_strikeprice,0))) end td_stlmnt,");
                    strsql.Append(" sum(td_bqty) Bqty, convert(decimal(15,2),sum(td_bqty*td_rate*sm_multiplier)) BAmt,   sum(td_sqty) Sqty,  ");
                    strsql.Append(" convert(decimal(15,2),sum(td_sqty*td_rate*sm_multiplier)) SAmt, sum(td_bqty-td_sqty) NQty, convert(decimal(15,2),sum((td_bqty-td_sqty)*td_rate *sm_multiplier)) NAmt, ");
                    strsql.Append(" cast((case when  sum(td_bqty-td_sqty)=0 then 0 else sum((td_bqty-td_sqty)*td_rate)/sum(td_bqty-td_sqty) end)as decimal(15,2) ) Avgrate,td_dt");
                    strsql.Append(" from trades with(" + StrTradesIndex + "nolock), series_master with (nolock) ");
                    strsql.Append(" where td_exchange+TD_Segment=SM_Exchange+SM_Segment and TD_SeriesId=SM_SeriesID and td_trxflag <> 'O'");
                    strsql.Append(" and td_clientcd='" + clientCd + "' and sm_symbol = '" + ScripCd + "'");
                    strsql.Append(" Group by td_dt,sm_Expirydt ,case left(sm_productcd,1) when 'F' then '' else ' ' + rtrim(sm_callput)+' '+ltrim(convert(char,round(sm_strikeprice,0))) end ");
                    strsql.Append(" union all ");
                    strsql.Append(" Select convert(char,convert(dateTime,ex_dt),103) TradeDate,Ltrim(Rtrim(convert(char,convert(dateTime,sm_Expirydt ),103))) + case left(sm_productcd,1) when 'F' then '' else ' ' + rtrim(sm_callput)+' '+ltrim(convert(char,round(sm_strikeprice,0))) end td_stlmnt,");
                    strsql.Append(" sum(ex_aqty) Bqty, convert(decimal(15,2),sum(ex_aqty*ex_diffrate*sm_multiplier)) BAmt,   sum(ex_eqty) Sqty,  ");
                    strsql.Append(" convert(decimal(15,2),sum(ex_eqty*ex_diffrate*sm_multiplier)) SAmt, sum(ex_aqty-ex_eqty) NQty, convert(decimal(15,2),sum((ex_aqty-ex_eqty)*ex_diffrate *sm_multiplier)) NAmt, ");
                    strsql.Append(" cast((case when  sum(ex_aqty-ex_eqty)=0 then 0 else sum((ex_aqty-ex_eqty)*ex_diffrate)/sum(ex_aqty-ex_eqty) end)as decimal(15,2) ) Avgrate,EX_dt");
                    strsql.Append(" from Exercise with (nolock) , series_master with (nolock) ");
                    strsql.Append(" where ex_exchange+ex_Segment=SM_Exchange+SM_Segment and ex_SeriesId=SM_SeriesID ");
                    strsql.Append(" and ex_clientcd='" + clientCd + "' and sm_symbol = '" + ScripCd + "'");
                    strsql.Append(" Group by ex_dt,sm_Expirydt ,case left(sm_productcd,1) when 'F' then '' else ' ' + rtrim(sm_callput)+' '+ltrim(convert(char,round(sm_strikeprice,0))) end ");
                    strsql.Append(" order by td_dt desc,td_stlmnt");
                }
                else if (Seg == "S")
                {
                    strsql.Append(" Select convert(char,convert(dateTime,td_dt),103) TradeDate, Case left(td_stlmnt,1) When 'B' Then 'BSE' When 'N' Then 'NSE' When 'M' Then 'MCX' else '?' end +'/'+rtrim(td_stlmnt) td_stlmnt,");
                    strsql.Append(" sum(td_bqty) Bqty, convert(decimal(15,2),sum(td_bqty*td_rate)) BAmt, sum(td_sqty) Sqty, convert(decimal(15,2),sum(td_sqty*td_rate)) SAmt,  ");
                    strsql.Append(" sum(td_bqty-td_sqty) NQty, convert(decimal(15,2),sum((td_bqty-td_sqty)*td_rate)) NAmt,");
                    strsql.Append(" cast((case when sum(td_bqty-td_sqty)=0 then 0 else sum((td_bqty-td_sqty)*td_rate)/sum(td_bqty-td_sqty) end)as decimal(15,2) ) as Avgrate,td_dt");
                    strsql.Append(" from trx with(" + StrTRXIndex + "nolock) , TPsecurities with (nolock)  ");
                    strsql.Append(" where td_scripcd=ss_cd and td_clientcd='" + clientCd + "' and td_Scripcd = '" + ScripCd + "'");
                    strsql.Append(" Group by td_dt ,td_stlmnt  ");
                    strsql.Append(" order by td_dt desc,td_stlmnt");
                }
                else if (Seg == "C")
                {
                    strsql.Append(" Select convert(char,convert(dateTime,td_dt),103) TradeDate,Ltrim(Rtrim(convert(char,convert(dateTime,sm_Expirydt ),103))) + case left(sm_productcd,1) when 'F' then '' else ' ' + rtrim(sm_callput)+' '+ltrim(convert(char,round(sm_strikeprice,0))) end td_stlmnt,");
                    strsql.Append(" sum(td_bqty) Bqty, convert(decimal(15,2),sum(td_bqty*td_rate*sm_multiplier)) BAmt,   sum(td_sqty) Sqty,  ");
                    strsql.Append(" convert(decimal(15,2),sum(td_sqty*td_rate*sm_multiplier)) SAmt, sum(td_bqty-td_sqty) NQty, convert(decimal(15,2),sum((td_bqty-td_sqty)*td_rate *sm_multiplier)) NAmt, ");
                    strsql.Append(" cast((case when  sum(td_bqty-td_sqty)=0 then 0 else sum((td_bqty-td_sqty)*td_rate)/sum(td_bqty-td_sqty) end)as decimal(15,2) ) Avgrate,td_dt");
                    strsql.Append(" from CTrades with (nolock), Cseries_master with (nolock)");
                    strsql.Append(" where td_exchange=SM_Exchange and TD_SeriesId=SM_SeriesID and td_trxflag <> 'O'");
                    strsql.Append(" and td_clientcd='" + clientCd + "' and sm_symbol = '" + ScripCd + "'");
                    strsql.Append(" Group by td_dt,Ltrim(Rtrim(convert(char,convert(dateTime,sm_Expirydt ),103))) + case left(sm_productcd,1) when 'F' then '' else ' ' + rtrim(sm_callput)+' '+ltrim(convert(char,round(sm_strikeprice,0))) end");
                    strsql.Append(" order by td_dt desc,td_stlmnt");
                }
                else if (Seg == "D")
                {
                    strsql.Append(" select convert(char,convert(dateTime,td_trxdate),103) TradeDate ,td_text Particulars,");
                    strsql.Append(" (case td_debit_credit When 'C' Then td_qty else 0 end) Inward, (case td_debit_credit When 'D' Then td_qty else 0 end) Outward,td_trxdate td_dt");
                    strsql.Append(" from trxweb with (nolock), DematAct with (nolock),Security with (nolock) where td_DPID+TD_Ac_Code=DA_ActNo and td_isin_code=sc_isincode ");
                    strsql.Append(" and da_clientcd='" + clientCd + "' and sc_isincode= '" + ScripCd + "'");
                    strsql.Append(" order by td_trxdate desc ");
                }
                if (strsql.Length == 0)
                    strsql.Append("Select 'No Record Found' ErrorMG");
                dtTable = objUtility.OpenDataTable(Convert.ToString(strsql));
                return dtTable;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetHoldingSummary(string clientCd)
        {
            try
            {
                DataTable dtTable;
                SqlTransaction objTrans;
                string strsql = "", strcollat = "", strCross = "", strEstro = "", strCrossTable = "", strEstroTable = "";
                if (GetWebParameter("IsTradeWeb", false) == "O")
                {
                    string connetionString = objUtility.GetConnectionStr();
                    using (SqlConnection objConnection = new SqlConnection(connetionString))
                    {
                        objConnection.Open();
                        objTrans = objConnection.BeginTransaction();
                        SqlCommand cmd = objConnection.CreateCommand();
                        cmd.Transaction = objTrans;
                        SqlDataAdapter sqlDtAdap = new SqlDataAdapter();
                        SqlCommandBuilder sqlCmdBld = new SqlCommandBuilder(sqlDtAdap);
                        strcollat = PrIProcessBenHolding(objConnection, objTrans, clientCd);
                        string strsubsql = FnGetBenHolding(objConnection, objTrans, strcollat, clientCd);

                        strCross = GetWebParameter("Cross", false);
                        strEstro = GetWebParameter("Estro", false);
                        if (strCross != "")
                        {
                            var ArrCross = Strings.Split(strCross, "/");
                            strCrossTable = " [" + ArrCross[0] + "].[" + ArrCross[1] + "].[" + ArrCross[2] + "] ";
                            strsql = " select rtrim(da_name) Name , hld_ac_code as  BOId , count(0) Recs, '1' ordr";
                            strsql += " from (";
                            strsql += " select hld_ac_code , hld_ac_pos ,da_name";
                            strsql += " from Dematact, " + strCrossTable + ".Holding  Where da_clientcd = '" + clientCd + "' and da_actno = hld_ac_code ";
                        }
                        if (strEstro != "")
                        {
                            var ArrEstro = Strings.Split(strEstro, "/");
                            strEstroTable = " [" + ArrEstro[0] + "].[" + ArrEstro[1] + "].[" + ArrEstro[2] + "] ";
                            strsql += " Union All ";
                            strsql = " select rtrim(da_name) Name , hld_ac_code as  BOId , count(0) Recs, '1' ordr";
                            strsql += " from (";
                            strsql += " select da_dpid+hld_ac_code hld_ac_code  , hld_ac_pos ,da_name";
                            strsql += " from Dematact, " + strEstroTable + ".Holding  Where da_clientcd = '" + clientCd + "' and da_actno = hld_ac_code ";
                        }
                        strsql += " ) A Group by hld_ac_code ,da_name ";
                        strsql += " Union All ";
                        strsql += "select 'Lying with '+rtrim(em_name)+' for various Reasons' ,'', (select count(0) from  (  " + strsubsql + ") B ) Recs, ";
                        strsql += " '2' ordr from Entity_Master with (nolock) where em_cd=";
                        if (Convert.ToDouble(Strings.Trim(objUtility.fnFireQuery("Entity_master", "count(0)", " em_cd='B' and em_name like 'SYKES & RAY EQUITIES%' and 1", "1"))) > 0d)
                        {
                            strsql += " 'B' ";
                        }
                        else
                        {
                            strsql += " (select min(em_cd) from Entity_master Where Len(Ltrim(Rtrim(em_cd))) = 1) ";
                        }
                        strsql += " group by em_name order by ordr, recs desc";
                        dtTable = OpenDataTableTemp(strsql, objConnection, objTrans);
                    }
                }
                else
                {
                    strsql = "  Select rtrim(da_name) Name , case left(da_dpid,2) when 'IN' then rtrim(da_dpid)+rtrim(da_actno) else da_actno end BOId, sum(case isnull(hld_ac_pos,0) when 0 then 0 else 1 end) Recs, '1' ordr ";
                    strsql += "  from dematact with (nolock) left outer join Holding on  hld_dpid+hld_ac_code=case left(da_dpid,2) when 'IN' then rtrim(da_dpid) else '' end+DA_ActNo where da_clientcd = '" + clientCd + "'";
                    strsql += "  group by da_name, da_dpid, da_actno ";
                    strsql += "  Union All ";
                    strsql += "  select 'Lying with '+rtrim(em_name)+' for various Reasons' ,'', sum(case bh_qty when 0 then 0 else 1 end ) Recs, '2' ordr from ";
                    strsql += "  Entity_Master with (nolock) left outer join BenHolding on 'A'='a' where";
                    if (Convert.ToDouble(Strings.Trim(objUtility.fnFireQuery("Entity_master", "count(0)", " em_cd='B' and em_name like 'SYKES & RAY EQUITIES%' and 1", "1"))) > 0d)
                    {
                        strsql += " em_cd = 'B' ";
                    }
                    else
                    {
                        strsql += " em_cd = (select min(em_cd) from Entity_master Where Len(Ltrim(Rtrim(em_cd))) = 1) ";
                    }
                    strsql += " and bh_clientcd='" + clientCd + "'";
                    strsql += " group by em_name ";
                    strsql += " order by ordr, recs desc ";
                    dtTable = objUtility.OpenDataTable(strsql);
                }
                return dtTable;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetDPHolding(string clientCd, string DematActNo)
        {
            try
            {
                DataTable dtTable;
                double dblValue = 0;
                string strsql = "", strCross = "", strEstro = "", strCommex = "", strCrossTable = "", strEstroTable = "";
                if (objUtility.GetWebParameter("IsTradeWeb") == "O")
                {
                    strCross = objUtility.GetWebParameter("Cross");
                    strEstro = objUtility.GetWebParameter("Estro");
                    strCommex = objUtility.GetWebParameter("Commex");
                    if ((Strings.Left(DematActNo, 2) == "12") & strCross != "")
                    {
                        string[] ArrCross = Strings.Split(strCross, "/");
                        strCrossTable = "[" + ArrCross[0] + "].[" + ArrCross[1] + "].[" + ArrCross[2] + "]";
                        strsql = "select a.hld_isin_code,b.sc_isinname sc_company_name,cast((a.hld_ac_pos) as decimal(15,2)) hld_ac_pos,d.bt_description 'bt_description', cast((sc_rate) as decimal(15,2)) as";
                        strsql += " sc_security_rate, cast(( ( a.hld_ac_pos * sc_Rate)) as decimal(15,2))  as valuation,bt_description as BType from " + strCrossTable + ".Holding a, " + strCrossTable + ".Security b,";
                        strsql += " " + strCrossTable + ".Beneficiary_type d where a.hld_ac_code in ('" + Strings.Trim(DematActNo) + "') and a.hld_isin_code = b.sc_isincode  and d.bt_code = a.hld_ac_type order by a.hld_ac_code,b.sc_company_name ";
                    }
                    else if ((Strings.Left(DematActNo, 2) == "IN") & strEstro != "")
                    {
                        string[] ArrEstro = Strings.Split(strEstro, "/");
                        strEstroTable = "[" + ArrEstro[0] + "].[" + ArrEstro[1] + "].[" + ArrEstro[2] + "]";
                        strsql = "select a.hld_isin_code,b.sc_company_name,cast((a.hld_ac_pos) as decimal(15,2)) hld_ac_pos,d.bt_description 'bt_description', cast((sc_rate) as decimal(15,2)) as";
                        strsql += " sc_security_rate, cast(( ( a.hld_ac_pos * sc_Rate)) as decimal(15,2))  as valuation,bt_description as BType from " + strEstroTable + ".Holding a, " + strEstroTable + ".Security b, ";
                        strsql += " " + strEstroTable + ".Beneficiary_type d , " + strEstroTable + ".Sysparameter e  where  RTrim(sp_sysvalue)+a.hld_ac_code in ('" + Strings.Trim(DematActNo) + "') and a.hld_isin_code = b.sc_isincode  and d.bt_code = Case When hld_blf = 'L' then Case hld_ac_type when  '22' then '17' when  '21' then '17'  when '29' then '18' else hld_ac_type end ";
                        strsql += " else case hld_ac_type when '25' then '28' else hld_ac_type end end and sp_parmcd = 'DPID' order by a.hld_ac_code,b.sc_company_name ";
                    }
                }
                else
                {
                    strsql = " select hld_isin_code, sc_company_name,cast((hld_ac_pos) as decimal(15,3)) hld_ac_pos, bt_description,  cast((sc_security_rate) as decimal(15,2)) sc_security_rate,   ";
                    strsql += " cast(( ( hld_ac_pos * sc_security_rate)) as decimal(15,2))  as valuation ,  ";
                    strsql += " bt_description + case When Len(Rtrim(hld_settlement)) = 8 and  left(hld_dpid,2) = 'IN'  Then ' Locked Till : ' + Ltrim(Rtrim(convert(char,convert(datetime,hld_settlement),103))) else '' end as BType  ";
                    strsql += " from Holding with (nolock),Security with (nolock) ,  Beneficiary_type with (nolock) ";
                    strsql += " where hld_dpid+hld_ac_code='" + DematActNo + "' and hld_isin_code=sc_isincode  ";
                    strsql += " and bt_code = hld_ac_type ";
                    strsql += " order by hld_ac_code ,bt_description,sc_company_name ";
                }

                if (strsql != "")
                {
                    dtTable = objUtility.OpenDataTable(strsql);
                    if (dtTable.Rows.Count > 0)
                    {
                        for (var i = 0; i <= dtTable.Rows.Count - 1; i++)
                            dblValue = dblValue + Math.Round(Convert.ToDouble(dtTable.Rows[i]["valuation"]), 2);
                        DataRow dtRow = dtTable.NewRow();
                        dtRow[0] = "Total";
                        dtRow[1] = "";
                        dtRow[2] = 0;
                        dtRow[3] = 0;
                        dtRow[4] = 0;
                        dtRow[5] = FormatCurr(Convert.ToDouble(Math.Round(dblValue, 2)).ToString(), 2, "Y");
                        dtRow[6] = "";
                        dtTable.Rows.Add(dtRow);
                    }
                    return dtTable;
                }
                else
                    return "";
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetBenHolding(string clientCd)
        {
            try
            {
                string strsql;
                DataTable dtTable;
                SqlTransaction objTrans;
                string strcollat = "";
                if (GetWebParameter("IsTradeWeb", false) == "O")
                {
                    string connetionString = objUtility.GetConnectionStr();
                    using (SqlConnection objConnection = new SqlConnection(connetionString))
                    {
                        objConnection.Open();
                        objTrans = objConnection.BeginTransaction();
                        SqlCommand cmd = objConnection.CreateCommand();
                        cmd.Transaction = objTrans;
                        SqlDataAdapter sqlDtAdap = new SqlDataAdapter();
                        SqlCommandBuilder sqlCmdBld = new SqlCommandBuilder(sqlDtAdap);
                        strcollat = PrIProcessBenHolding(objConnection, objTrans, clientCd);
                        strsql = FnGetBenHolding(objConnection, objTrans, strcollat, clientCd);
                        strsql += " order by bh_type,ss_name";
                        dtTable = OpenDataTableTemp(strsql, objConnection, objTrans);
                    }
                }
                else
                {
                    strsql = "  select bh_scripcd as 'dm_scripcd',bh_isin as 'dm_isin', bh_scripname as 'ss_name',bh_stlmnt as 'se_stlmnt',";
                    strsql += " convert(decimal(15,2),bh_bserate) as bh_bserate,convert(decimal(15,2),bh_valuation) as 'valuation', ";
                    strsql += " sum(bh_qty) as 'qty', case bh_type when 'BEN' then 'Beneficiary Holding' when 'EXP' then 'Expected Holding' when 'UNDEL' then 'Undelivered Holding' else bh_type end bh_type";
                    strsql += " from benholding with (nolock) where bh_clientcd = '" + clientCd + "'";
                    strsql += " group by bh_clientcd,bh_scripcd,bh_isin, bh_scripname,bh_stlmnt,bh_bserate,bh_valuation,bh_type,bh_type+bh_scripcd ";
                    strsql += " having abs(sum(bh_qty)) > 0  order by bh_type, bh_scripname ";
                    dtTable = objUtility.OpenDataTable(strsql);
                }
                return dtTable;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetCollateral(string clientCd)
        {
            try
            {
                string strsql;
                DataTable dtTable;
                SqlTransaction objTrans;
                double dblValue = 0.0d;
                double dblNetValue = 0.0d;

                if (GetWebParameter("IsTradeWeb", false) == "O")
                {
                    string connetionString = objUtility.GetConnectionStr();
                    using (SqlConnection objConnection = new SqlConnection(connetionString))
                    {
                        objConnection.Open();
                        objTrans = objConnection.BeginTransaction();
                        SqlCommand cmd = objConnection.CreateCommand();
                        cmd.Transaction = objTrans;
                        SqlDataAdapter sqlDtAdap = new SqlDataAdapter();
                        SqlCommandBuilder sqlCmdBld = new SqlCommandBuilder(sqlDtAdap);
                        PrCreatetblmargincol(objConnection, objTrans);
                        PrInserttblmargincol(objConnection, clientCd, objTrans);
                        strsql = " select  ts_scripcd as ScripCode, ts_scripname as ScripName , cast(sum(ts_qty) as decimal(15,0)) as Qty ,cast(sum(ts_closeprice) as decimal(15,2))";
                        strsql += " as rate,cast(sum(ts_value) as decimal(15,2)) as Value , cast(sum(ts_haircut) as decimal(15,2))  as HairCut , cast( sum(ts_netvalue) as decimal(15,2)) as NetValue ";
                        strsql += " from #tempmargincollaterial, client_master ,Fcollateral_types where cm_cd = ts_clientcd and fct_cd =ts_collateraltype  and ts_qty <> 0 and ts_clientcd = '" + clientCd + "' ";
                        strsql += " group by ts_clientcd, cm_name, ts_category , ts_collateraltype,fct_desc ,  ts_scripcd , ts_scripname ,ts_transactiondt ,ts_maturitydt,ts_isin";
                        strsql += " order by ts_clientcd, cm_name, ts_category,ts_collateraltype,fct_desc  ,  ts_scripname,ts_scripcd , ts_transactiondt ,ts_maturitydt ";
                        dtTable = OpenDataTableTemp(strsql, objConnection, objTrans);
                    }
                }
                else
                {
                    strsql = "select  fc_pscd as ScripCode,ss_name as ScripName,";
                    strsql += " Sum(Case fc_controlflag When 'D' Then fc_qty else -fc_qty end ) as Qty, ";
                    strsql += " cast(case substring(fc_companycode,2,1) when 'B' then ss_bserate when 'N' then ss_nserate else 0 end as decimal(15,2)) as rate, ";
                    strsql += " cast(Sum(Case fc_controlflag When 'D' Then fc_qty else -fc_qty end )* (case substring(fc_companycode,2,1) when 'B' then ss_bserate when 'N' then ss_nserate else 0 end) as decimal(15,2)) as Value , ";
                    strsql += " cast(case substring(fc_companycode,2,1) when 'B' then ss_bseHaircut when 'N' then ss_nseHaircut else 0 end as decimal(15,2)) as HairCut,";
                    strsql += " cast(Sum(Case fc_controlflag When 'D' Then fc_qty else -fc_qty end )* (case substring(fc_companycode,2,1) when 'B' then ss_bserate when 'N' then ss_nserate else 0 end)* cast(100 - case substring(fc_companycode,2,1) when 'B' then ss_bseHaircut when 'N' then ss_nseHaircut else 0 end as decimal(15,2)) /100 as decimal(15,2)) as NetValue ";
                    strsql += " from collateral_detail with (nolock)  ,tpsecurities with (nolock)   ";
                    strsql += " where fc_pscd =ss_cd and fc_clientcd = '" + clientCd + "'";
                    strsql += " group by fc_clientcd,fc_pscd,ss_name,fc_companycode,ss_bserate,ss_nserate,ss_bseHaircut,ss_nseHaircut";
                    strsql += " order by ScripName";
                    dtTable = objUtility.OpenDataTable(strsql);
                }

                if (dtTable.Rows.Count > 0)
                {
                    for (int i = 0, loopTo = dtTable.Rows.Count - 1; i <= loopTo; i++)
                    {
                        dblValue = dblValue + Math.Round(Convert.ToDouble(dtTable.Rows[i]["Value"]), 2);
                        dblNetValue = dblNetValue + Math.Round(Convert.ToDouble(dtTable.Rows[i]["NetValue"]), 2);
                    }
                    DataRow dtRow = dtTable.NewRow();
                    dtRow[0] = "Total";
                    dtRow[1] = "";
                    dtRow[2] = 0;
                    dtRow[3] = 0;
                    dtRow[4] = FormatCurr(Convert.ToDouble(Math.Round(dblValue, 2)).ToString(), 2, "Y");
                    dtRow[5] = 0;
                    dtRow[6] = FormatCurr(Convert.ToDouble(Math.Round(dblValue, 2)).ToString(), 2, "Y");
                    dtTable.Rows.Add(dtRow);
                }
                return dtTable;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetMarginShortFall(string clientCd)
        {
            try
            {
                DataTable dtTable;
                string strsql, strCommex = "", strCommTable = "";
                strCommex = GetWebParameter("Commex");

                if (GetWebParameter("IsTradeWeb") == "O")
                {
                    strsql = " Select case right(fm_companycode+fm_exchange+fm_Segment,2) When 'BF' Then 'BSE F&O' When 'NF' Then 'NSE F&O' When 'MF' Then 'MCX F&O' else '' end ExchSeg,";
                    strsql += " cast(fm_spanmargin as decimal(15,2)) as fm_spanmargin,cast(fm_exposurevalue as decimal(15,2)) as fm_exposurevalue, cast(fm_buypremmargin as decimal(15,2)) as fm_buypremmargin,";
                    strsql += " cast(fm_initialmargin as decimal(15,2)) as fm_initialmargin,cast(fm_additionalmargin as decimal(15,2)) as fm_additionalmargin, cast(fm_collected as decimal(15,2)) as fm_collected,cast(isnull(fm_spanmargin,0 )+ isnull(fm_exposurevalue,0) as decimal(15,2)) 'margin',";
                    strsql += " cast(case when (fm_initialmargin + fm_exposurevalue- case when fm_collected > 0 then fm_collected else 0 end) >0 then (fm_initialmargin + fm_exposurevalue- case when fm_collected > 0 then fm_collected else 0 end) else 0 end as decimal(15,2)) ShortFall";
                    strsql += " from fmargins where right(fm_companycode+fm_exchange+fm_Segment,1) = 'F' and fm_dt = (select max(fm_Dt) from fmargins Where right(fm_companycode+fm_exchange+fm_Segment,1) = 'F' ) and fm_clientcd='" + clientCd + "'";
                    strsql += " union all";
                    strsql += " Select case right(fm_companycode+fm_exchange+fm_Segment,2) When 'BK' Then 'BSE FX' When 'NK' Then 'NSE FX' When 'MK' Then 'MCX FX' else '' end ExchSeg,";
                    strsql += " cast(fm_spanmargin as decimal(15,2)) as fm_spanmargin,cast(fm_exposurevalue as decimal(15,2)) as fm_exposurevalue, cast(fm_buypremmargin as decimal(15,2)) as fm_buypremmargin,";
                    strsql += " cast(fm_initialmargin as decimal(15,2)) as fm_initialmargin,cast(fm_additionalmargin as decimal(15,2)) as fm_additionalmargin, cast(fm_collected as decimal(15,2)) as fm_collected,cast(isnull(fm_spanmargin,0 )+ isnull(fm_exposurevalue,0) as decimal(15,2)) 'margin',";
                    strsql += " cast(case when (fm_initialmargin + fm_exposurevalue- case when fm_collected > 0 then fm_collected else 0 end) >0 then (fm_initialmargin + fm_exposurevalue- case when fm_collected > 0 then fm_collected else 0 end) else 0 end as decimal(15,2)) ShortFall";
                    strsql += " from fmargins where right(fm_companycode+fm_exchange+fm_Segment,1) = 'K' and fm_dt = (select max(fm_Dt) from fmargins Where right(fm_companycode+fm_exchange+fm_Segment,1) = 'K' ) and fm_clientcd='" + clientCd + "'";
                    if ((strCommex != ""))
                    {
                        string[] ArrCommex = Strings.Split(GetWebParameter("Commex"), "/");
                        strCommTable = " [" + ArrCommex[0] + "].[" + ArrCommex[1] + "].[" + ArrCommex[2] + "]";
                        strsql += " union all";
                        strsql += " Select case fm_exchange When 'M' Then 'MCX Commodity' When 'N' Then 'NCDEX Commodity' When 'F' Then 'NCDEX Commodity' else '' end ExchSeg,";
                        strsql += " cast((fm_initialmargin - fm_buypremmargin + fm_additionalmargin +  fm_Regmargin + fm_Tndmargin + fm_Dlvmargin + fm_SplMargin - fm_SpreadBen)as decimal(15,2)) fm_spanmargin, ";
                        strsql += " cast(fm_exposurevalue as decimal(15,2)) fm_exposurevalue, Cast(0 as decimal(15,2)) fm_buypremmargin,Cast(0 as decimal(15,2)) fm_initialmargin,";
                        strsql += " Cast(0 as decimal(15,2)) fm_additionalmargin, Cast(fm_collected as decimal(15,2)) fm_collected, Cast((fm_initialmargin - fm_buypremmargin + fm_additionalmargin +  fm_Regmargin + fm_Tndmargin + fm_Dlvmargin + fm_SplMargin - fm_SpreadBen + fm_exposurevalue)as decimal(15,2))  'margin',";
                        strsql += " Cast(case When (fm_initialmargin - fm_buypremmargin + fm_additionalmargin +  fm_Regmargin + fm_Tndmargin + fm_Dlvmargin + fm_SplMargin - fm_SpreadBen + fm_exposurevalue) > fm_collected  Then";
                        strsql += "	(fm_initialmargin - fm_buypremmargin + fm_additionalmargin +  fm_Regmargin + fm_Tndmargin + fm_Dlvmargin + fm_SplMargin - fm_SpreadBen + fm_exposurevalue) - fm_collected ";
                        strsql += " else 0 end as decimal(15,2)) ShortFall ";
                        strsql += " from " + strCommTable + ".fmargins ";
                        strsql += " where fm_dt = (select max(fm_Dt) from " + strCommTable + ".fmargins) ";
                        strsql += " and fm_clientcd ='" + clientCd + "'";
                    }
                }
                else
                {
                    strsql = " Select case right(fm_companycode,2) When 'BF' Then 'BSE DERIVATIVE' When 'NF' Then 'NSE DERIVATIVE' When 'MF' Then 'MCX DERIVATIVE' else '?' end ExchSeg,";
                    strsql += " cast(fm_spanmargin as decimal(15,2)) as fm_spanmargin,cast(fm_exposurevalue as decimal(15,2)) as fm_exposurevalue, cast(fm_buypremmargin as decimal(15,2)) as fm_buypremmargin,";
                    strsql += " cast(fm_initialmargin as decimal(15,2)) as fm_initialmargin,cast(fm_additionalmargin as decimal(15,2)) as fm_additionalmargin, cast(fm_collected as decimal(15,2)) as fm_collected,cast(isnull(fm_spanmargin,0 )+ isnull(fm_exposurevalue,0) as decimal(15,2)) 'margin',";
                    strsql += " cast(case when (fm_initialmargin + fm_exposurevalue- case when fm_collected > 0 then fm_collected else 0 end) >0 then (fm_initialmargin + fm_exposurevalue- case when fm_collected > 0 then fm_collected else 0 end) else 0 end as decimal(15,2)) ShortFall";
                    strsql += " from fmargins with (nolock) where right(fm_companycode,1) = 'F' and fm_dt = (select max(fm_Dt) from fmargins with (nolock) Where right(fm_companycode,1) = 'F' ) and fm_clientcd='" + clientCd + "'";
                    strsql += " union all";
                    strsql += " Select case right(fm_companycode,2) When 'BK' Then 'BSE FX' When 'NK' Then 'NSE FX' When 'MK' Then 'MCX FX' else '?' end ExchSeg,";
                    strsql += " cast(fm_spanmargin as decimal(15,2)) as fm_spanmargin,cast(fm_exposurevalue as decimal(15,2)) as fm_exposurevalue, cast(fm_buypremmargin as decimal(15,2)) as fm_buypremmargin,";
                    strsql += " cast(fm_initialmargin as decimal(15,2)) as fm_initialmargin,cast(fm_additionalmargin as decimal(15,2)) as fm_additionalmargin, cast(fm_collected as decimal(15,2)) as fm_collected,cast(isnull(fm_spanmargin,0 )+ isnull(fm_exposurevalue,0) as decimal(15,2)) 'margin',";
                    strsql += " cast(case when (fm_initialmargin + fm_exposurevalue- case when fm_collected > 0 then fm_collected else 0 end) >0 then (fm_initialmargin + fm_exposurevalue- case when fm_collected > 0 then fm_collected else 0 end) else 0 end as decimal(15,2)) ShortFall";
                    strsql += " from fmargins with (nolock) where right(fm_companycode,1) = 'K' and fm_dt = (select max(fm_Dt) from fmargins with (nolock) Where right(fm_companycode,1) = 'K' ) and fm_clientcd='" + clientCd + "'";
                    strsql += " union all";
                    strsql += " Select case right(fm_companycode,2) When 'MX' Then 'MCX Commodity' When 'NX' Then 'NCDEX Commodity' When 'NF' Then 'NCDEX Commodity' else '?' end ExchSeg,cast(fm_spanmargin as decimal(15,2)) as fm_spanmargin,";
                    strsql += " cast(fm_exposurevalue as decimal(15,2)) as fm_exposurevalue, cast(fm_buypremmargin as decimal(15,2)) as fm_buypremmargin,cast(fm_initialmargin as decimal(15,2)) as fm_initialmargin,cast(fm_additionalmargin as decimal(15,2)) as fm_additionalmargin, cast(fm_collected as decimal(15,2)) as fm_collected,";
                    strsql += " cast(isnull(fm_spanmargin,0 )+ isnull(fm_exposurevalue,0) as decimal(15,2)) 'margin',cast(case when (fm_initialmargin + fm_exposurevalue- case when fm_collected > 0 then fm_collected else 0 end) >0 then (fm_initialmargin + fm_exposurevalue- case when fm_collected > 0 then fm_collected else 0 end) else 0 end as decimal(15,2)) ShortFall";
                    strsql += " from fmargins with (nolock)  where right(fm_companycode,1) = 'X' and fm_dt = (select max(fm_Dt) from fmargins with (nolock) Where right(fm_companycode,1) = 'X' ) and fm_clientcd='" + clientCd + "'";
                }
                dtTable = objUtility.OpenDataTable(strsql);

                return dtTable;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetBillsYear(string clientCd, string Seg)
        {
            try
            {
                DataTable dtTable;
                string strsql = "", strCommex = "", strCommTable = "";
                if (Seg.ToUpper() == "C")
                {
                    if (GetWebParameter("IsTradeWeb") == "O")
                    {
                        strsql = "SELECT distinct  case when  MONTH(bl_billdt)>3 then Left(bl_billdt,4)  +'-' + Right(LEft(bl_billdt,4)+1,2) else  LEFT(Left(bl_billdt,4)-1,4) +'-' + Right(LEft(bl_billdt,4),2) end cYEar  ";
                        strsql += " from Bills Where bl_clientcd='" + clientCd + "' Order by cYear desc ";
                    }
                    else
                    {
                        strsql = " SELECT distinct  case when  MONTH(TD_DT)>3 then Left(TD_DT,4)  +'-' + Right(LEft(TD_DT,4)+1,2) else  LEFT(Left(TD_DT,4)-1,4) +'-' + Right(LEft(TD_DT,4),2) end cYEar ";
                        strsql += " from trx Where td_clientcd='" + clientCd + "' Order by cYear desc ";
                    }
                }
                else if (Seg.ToUpper() == "F" | Seg.ToUpper() == "K")
                {
                    strsql = "SELECT distinct  case when  MONTH(fb_billdt)>3 then Left(fb_billdt,4)  +'-' + Right(LEft(fb_billdt,4)+1,2) else  LEFT(Left(fb_billdt,4)-1,4) +'-' + Right(LEft(fb_billdt,4),2) end cYEar  ";
                    strsql += " from Fbills Where fb_clientcd='" + clientCd + "' Order by cYear desc ";
                }
                else if (Seg.ToUpper() == "X")
                {
                    if (GetWebParameter("IsTradeWeb") == "O")
                    {
                        strCommex = GetWebParameter("Commex");
                        if (strCommex != "")
                        {
                            string[] ArrCommex = Strings.Split(strCommex, "/");
                            strCommTable = "[" + ArrCommex[0] + "].[" + ArrCommex[1] + "].[" + ArrCommex[2] + "]";
                            strsql = "SELECT distinct  case when  MONTH(fb_billdt)>3 then Left(fb_billdt,4)  +'-' + Right(LEft(fb_billdt,4)+1,2) else  LEFT(Left(fb_billdt,4)-1,4) +'-' + Right(LEft(fb_billdt,4),2) end cYEar  ";
                            strsql += " from  " + strCommTable + " .Fbills where fb_clientcd= '" + clientCd + "' Order by cYear desc ";
                        }
                    }
                    else
                    {
                        strsql = "SELECT distinct  case when  MONTH(fb_billdt)>3 then Left(fb_billdt,4)  +'-' + Right(LEft(fb_billdt,4)+1,2) else  LEFT(Left(fb_billdt,4)-1,4) +'-' + Right(LEft(fb_billdt,4),2) end cYEar  ";
                        strsql += " from CFbills Where fb_clientcd='" + clientCd + "' Order by cYear desc ";
                    }
                }
                dtTable = objUtility.OpenDataTable(strsql);
                return dtTable;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetBillsN(string clientcd, string Seg, string Year)
        {
            try
            {
                string strsql = "";
                DataTable dtTable;
                string strFromDt;
                string strToDt;
                string StrTRXIndex = "", strCommex = "", strCommTable = "";
                if (Convert.ToInt16(objUtility.fnFireQuery("sysobjects a, sysindexes b", "COUNT(0)", "a.id = b.id and a.name = 'Trx' and b.name", "idx_Trx_Clientcd", true)) == 1)
                    StrTRXIndex = "index(idx_trx_clientcd),";

                strFromDt = "20" + Strings.Mid(Year, 3, 2) + "0401";
                strToDt = "20" + Strings.Mid(Year, 6, 2) + "0331";
                Seg = Strings.Right(Seg, 1);
                if (GetWebParameter("IsTradeWeb") == "O")
                {
                    strCommex = GetWebParameter("Commex");

                    if (Seg.ToUpper() == "C")
                    {
                        strsql = "select  ltrim(rtrim(convert(char,convert(datetime,bl_billdt),103))) as billdate, case left(bl_stlmnt,1) When 'B' Then 'BSE' When 'N' Then 'NSE' When 'M' Then 'MCX' else";
                        strsql += " '?' end  exchsegdesc , left(bl_stlmnt,1)+'C' exchseg,  cast(Sum(Amount) as decimal(15,2)) billamount , bl_stlmnt as stlmnt ,'' TradeType   From";
                        strsql += " (Select bl_billdt,bl_stlmnt,Sum(bl_amount) Amount from Bills where   bl_clientcd = '" + clientcd + "' and bl_billdt between '" + strFromDt + "' and '" + strToDt + "'  Group By bl_billdt,bl_stlmnt";
                        strsql += " ) a";
                        strsql += "  Group By bl_stlmnt,bl_billdt,left(bl_stlmnt,1)  Having abs(Sum(Amount)) > 0  Order by bl_billdt desc,bl_stlmnt";
                    }
                    else if (Seg.ToUpper() == "F" | Seg.ToUpper() == "K")
                    {
                        strsql = "select ltrim(rtrim(convert(char,convert(datetime,fb_billdt),103))) as billdate , case fb_exchange When 'B' Then 'BSE' When 'N' Then 'NSE' When 'M' Then 'MCX' else '?' End exchsegdesc , ";
                        strsql += " fb_exchange+fb_Segment exchseg,  cast(fb_amount as decimal(15,2)) billamount,'' stlmnt, ";
                        strsql += " case When ( select count(0) from Trades Where td_dt = fb_billdt and td_companyCode = fb_companycode and td_exchange= fb_exchange ";
                        strsql += " and td_Segment = fb_Segment and tD_clientcd = fb_clientcd  and  td_trxflag <> 'O') > 0 Then '[Trade]' else '' End TradeType ";
                        strsql += " from Fbills with (nolock)  ";
                        strsql += " where fb_clientcd= '" + clientcd + "' and fb_Segment= '" + Seg + "' and abs(fb_amount) > 0 ";
                        strsql += " and  fb_billdt between '" + strFromDt + "' and '" + strToDt + "'";
                        strsql += " Order by fb_billdt desc ";
                    }
                    else if (Seg.ToUpper() == "X" & strCommex != "")
                    {
                        string[] ArrCommex = Strings.Split(strCommex, "/");
                        strCommTable = " [" + ArrCommex[0] + "].[" + ArrCommex[1] + "].[" + ArrCommex[2] + "] ";
                        strsql = " select ltrim(rtrim(convert(char,convert(datetime,fb_billdt),103))) as billdate , ";
                        strsql += " case fb_exchange When 'M' Then 'MCX' When 'N' Then 'NCDEX' When 'F' Then 'NCDEX' else '?' End  exchsegdesc , ";
                        strsql += " fb_exchange+'X' exchseg, cast(fb_amount as decimal(15,2)) billamount ,'' stlmnt,";
                        strsql += " case When ( select count(0) from " + strCommTable + ".Trades Where td_dt = fb_billdt and td_companyCode = fb_companycode and td_exchange= fb_exchange ";
                        strsql += " and tD_clientcd = fb_clientcd  and  td_trxflag <> 'O') > 0 Then '[Trade]' else '' End TradeType ";
                        strsql += " from  " + strCommTable + " .Fbills with (nolock)  ";
                        strsql += " where fb_clientcd= '" + clientcd + "' and abs(fb_amount) > 0  and fb_billdt between '" + strFromDt + "' and '" + strToDt + "'";
                        strsql += " Order by fb_billdt desc ";
                    }
                }
                else if (Seg.ToUpper() == "C")
                {
                    if (Convert.ToDouble(objUtility.fnFireQuery("sysObjects", "count(0)", "Name", "Bills")) > 0)
                    {
                        strsql = "select  ltrim(rtrim(convert(char,convert(datetime,bl_billdt),103))) as billdate, case left(bl_stlmnt,1) When 'B' Then 'BSE' When 'N' Then 'NSE' When 'M' Then 'MCX' else";
                        strsql += " '?' end  exchsegdesc , left(bl_stlmnt,1)+'C' exchseg,  cast(Sum(Amount) as decimal(15,2)) billamount , bl_stlmnt as stlmnt ,'' TradeType   From";
                        strsql += " (Select bl_billdt,bl_stlmnt,Sum(bl_amount) Amount from Bills where   bl_clientcd = '" + clientcd + "' and bl_billdt between '" + strFromDt + "' and '" + strToDt + "'  Group By bl_billdt,bl_stlmnt";
                        strsql += " ) a";
                        strsql += "  Group By bl_stlmnt,bl_billdt,left(bl_stlmnt,1)  Having abs(Sum(Amount)) > 0  Order by bl_billdt desc,bl_stlmnt";
                        dtTable = objUtility.OpenDataTable(strsql);
                        if (dtTable.Rows.Count > 0)
                            return dtTable;
                    }
                    strsql = "select  ltrim(rtrim(convert(char,convert(datetime,td_dt),103))) as billdate, case left(td_stlmnt,1) When 'B' Then 'BSE' When 'N' Then 'NSE' When 'M' Then 'MCX' else '?' end  exchsegdesc ,";
                    strsql += " left(td_stlmnt,1)+'C' exchseg,  cast(Sum(Amount) as decimal(15,2)) billamount , td_stlmnt as stlmnt ,'' TradeType   From (Select td_dt,td_Stlmnt,Sum((td_bqty-td_sqty)*td_rate) Amount from trx with(" + StrTRXIndex + "nolock),";
                    strsql += "  TPSecurities with (nolock) where td_Scripcd=ss_Cd and  td_clientcd= '" + clientcd + "' and td_dt between '" + strFromDt + "' and '" + strToDt + "'  Group By td_dt,td_Stlmnt ";
                    strsql += " union all";
                    strsql += " Select se_stdt,sh_Stlmnt, cast(sum(sh_amount) as decimal(15,2)) from specialcharges with (nolock) ,settlements with (nolock) where  sh_clientcd= '" + clientcd + "' and sh_Stlmnt = se_stlmnt ";
                    strsql += " and se_stdt  between '" + strFromDt + "' and '" + strToDt + "' ";
                    strsql += " group by se_stdt,sh_Stlmnt ) a Group By td_stlmnt,td_dt,left(td_stlmnt,1) ";
                    strsql += " Having abs(Sum(Amount)) > 0 ";
                    strsql += " Order by td_dt desc,td_stlmnt";
                }
                else if (Seg.ToUpper() == "F" | Seg.ToUpper() == "K")
                {
                    strsql = "select ltrim(rtrim(convert(char,convert(datetime,fb_billdt),103))) as billdate , case fb_exchange When 'B' Then 'BSE' When 'N' Then 'NSE' When 'M' Then 'MCX' else '?' End exchsegdesc , ";
                    strsql += " fb_exchange+fb_Segment exchseg,  cast(fb_amount as decimal(15,2)) billamount,'' stlmnt, ";
                    strsql += " case When ( select count(0) from Trades Where td_dt = fb_billdt and td_companyCode = fb_companycode and td_exchange= fb_exchange ";
                    strsql += " and td_Segment = fb_Segment and tD_clientcd = fb_clientcd  and  td_trxflag <> 'O') > 0 Then '[Trade]' else '' End TradeType ";
                    strsql += " from Fbills with (nolock)  ";
                    strsql += " where fb_clientcd= '" + clientcd + "' and fb_Segment= '" + Seg + "' and abs(fb_amount) > 0 ";
                    strsql += " and  fb_billdt between '" + strFromDt + "' and '" + strToDt + "'";
                    strsql += " Order by fb_billdt desc ";
                }
                else if (Seg.ToUpper() == "X")
                {
                    strsql = " select ltrim(rtrim(convert(char,convert(datetime,fb_billdt),103))) as billdate , case fb_exchange When 'B' Then 'BSE' When 'N' Then 'NSE' When 'M' Then 'MCX' else '?' End  exchsegdesc , ";
                    strsql += " fb_exchange+'X' exchseg, cast(fb_amount as decimal(15,2)) billamount ,'' stlmnt,";
                    strsql += " case When ( select count(0) from CTrades Where td_dt = fb_billdt and td_companyCode = fb_companycode and td_exchange= fb_exchange ";
                    strsql += " and tD_clientcd = fb_clientcd  and  td_trxflag <> 'O') > 0 Then '[Trade]' else '' End TradeType ";
                    strsql += " from CFbills with (nolock)  ";
                    strsql += " where fb_clientcd= '" + clientcd + "' and abs(fb_amount) > 0  and fb_billdt between '" + strFromDt + "' and '" + strToDt + "'";
                    strsql += "  Order by fb_billdt desc ";
                }
                dtTable = objUtility.OpenDataTable(strsql);
                return dtTable;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetBillDetail(string clientCd, string Date, string Exch, string Seg, string CompCd)
        {
            try
            {
                string strsql = "";
                DataTable dtTable;
                SqlTransaction objTrans;
                double dblTotal = 0;
                string StrTRXIndex = "", strCommex = "", dblValue = "";
                bool blnInterOP = false;

                string strStlmntWhere = " and  td_stlmnt = '" + Date + "'";
                if (Seg == "C")
                {
                    blnInterOP = fnIsInterOperability("C", "", Date);
                    if (blnInterOP)
                    {
                        string[] arrStlmnt = Strings.Split(fnGetInterOpStlmnts(Date), ",");
                        if (Information.UBound(arrStlmnt) > 0)
                            strStlmntWhere = " and td_stlmnt in ('" + arrStlmnt[0] + "','" + arrStlmnt[1] + "')";
                        else
                            strStlmntWhere = " and td_stlmnt = '" + arrStlmnt[0] + "'";
                    }
                }

                if (Convert.ToInt16(objUtility.fnFireQuery("sysobjects a, sysindexes b", "COUNT(0)", "a.id = b.id and a.name = 'Trx' and b.name", "idx_Trx_Clientcd", true)) == 1)
                    StrTRXIndex = "index(idx_trx_clientcd),";

                string StrTradesIndex = "";
                if (Convert.ToInt16(objUtility.fnFireQuery("sysobjects a, sysindexes b", "COUNT(0)", "a.id = b.id and a.name = 'trades' and b.name", "idx_trades_clientcd", true)) == 1)
                    StrTradesIndex = "index(idx_trades_clientcd),";


                if (GetWebParameter("IsTradeWeb") == "O")
                {
                    strCommex = GetWebParameter("Commex");
                    string connetionString = objUtility.GetConnectionStr();
                    using (SqlConnection objConnection = new SqlConnection(connetionString))
                    {
                        objConnection.Open();
                        objTrans = objConnection.BeginTransaction();
                        SqlCommand cmd = objConnection.CreateCommand();
                        cmd.Transaction = objTrans;
                        SqlDataAdapter sqlDtAdap = new SqlDataAdapter();
                        SqlCommandBuilder sqlCmdBld = new SqlCommandBuilder(sqlDtAdap);
                        if (Seg == "C")
                        {
                            strsql = "Select ss_Name as smsname,  'N'TRXFLAG,Sum(td_bqty-td_sqty) Qty,  cast(Sum((td_bqty+td_sqty)*td_rate)/Sum(td_bqty+td_sqty) AS decimal(15,4)) tdrate,";
                            strsql += " 0lastclose,cast(Sum(((td_bqty-td_sqty)*td_rate) ) as decimal(15,2))  drcr, 1 sortorder  from trx with(" + StrTRXIndex + "nolock), Securities with (nolock)";
                            strsql += " where td_Scripcd=ss_Cd and  td_clientcd= '" + clientCd + "'" + strStlmntWhere + " Group By ss_Name ";
                            strsql += " union all";
                            strsql += " Select sh_desc sm_sname,'C' TRXFLAG,0 Qty,0 td_rate,0 td_lastclose,cast(sum(sh_amount) as decimal(15,2))  drcr,2 SorOrder";
                            strsql += " from specialcharges with (nolock) where   sh_clientcd= '" + clientCd + "'" + strStlmntWhere.Replace("td_", "sh_") + " group by sh_desc  ";
                            strsql += " union all";
                            strsql += " select sm_sname, 'C' TRXFLAG ,0 Qty,0 td_rate, 0 td_lastclose, SUM(drcr) ,2 SorOrder";
                            strsql += " from (select cg_desc sm_sname,cast(bc_amount as decimal(15,2)) drcr";
                            strsql += " from Cbilled_charges with (nolock),charges_master with (nolock)   where";
                            strsql += " bc_clientcd='" + clientCd + "'" + strStlmntWhere.Replace("td_", "bc_");
                            strsql += "  and Left(bc_stlmnt,1) = cg_exchange";
                            strsql += " and  bc_chargecode =cg_cd and bc_companycode=cg_companycode";
                            strsql += " union all";
                            strsql += " Select  cg_desc sm_sname,cast(sh_servicetax as decimal(15,2))  drcr ";
                            strsql += " from specialcharges with (nolock), charges_master where";
                            strsql += " sh_clientcd= '" + clientCd + "'" + strStlmntWhere.Replace("td_", "sh_") + " and Left(sh_stlmnt,1) = cg_exchange and  sh_companycode=cg_companycode and cg_cd='01' and sh_servicetax > 0)  A  group by sm_sname";
                            strsql += " Order by sortorder ,smsname ";
                        }
                        else
                        {
                            strsql = fnForBill(objConnection, objTrans, clientCd, Date, Exch, Seg, CompCd);
                        }
                        dtTable = OpenDataTableTemp(strsql, objConnection, objTrans);
                    }
                }
                else
                {
                    // '''''''''''Trtadeweb ''''''''''''''''
                    if (Seg == "C")
                    {
                        strsql = "Select ss_Name as smsname,'N'TRXFLAG,Sum(td_bqty-td_sqty) Qty,  cast(Sum((td_bqty+td_sqty)*td_rate)/Sum(td_bqty+td_sqty) AS decimal(15,4)) tdrate,";
                        strsql += " 0lastclose,cast(Sum(((td_bqty-td_sqty)*td_rate) ) as decimal(15,2))  drcr, 1 sortorder  from trx with(" + StrTRXIndex + "nolock), TPSecurities with (nolock)";
                        strsql += " where td_Scripcd=ss_Cd and  td_clientcd= '" + clientCd + "'" + strStlmntWhere + " Group By ss_Name ";
                        strsql += " union all";
                        strsql += " Select sh_desc sm_sname,'C' TRXFLAG,0 Qty,0 td_rate,0 td_lastclose,cast(sum(sh_amount) as decimal(15,2))  drcr,2 SorOrder";
                        strsql += " from specialcharges with (nolock) where   sh_clientcd= '" + clientCd + "'" + strStlmntWhere.Replace("td_", "sh_") + " group by sh_desc  Order by sortorder ,smsname ";
                    }
                    else if (Seg == "F" | Seg == "K")
                    {
                        strsql = "Select left(sm_Productcd,1)+ ' ' + ltrim( substring(sm_Productcd,4,3)) + ' ' + Ltrim(RtRim( sm_Symbol)) + ' ' + left(Ltrim(Rtrim(convert(char, convert(datetime, sm_Expirydt,3),103))),6) + ";
                        strsql += " Right(Ltrim(Rtrim(convert(char, convert(datetime, sm_Expirydt,3),103))),2) + ' ' + Ltrim(Rtrim(case When sm_Strikeprice  > 0 Then case When CEILING(sm_Strikeprice)-Floor(sm_Strikeprice) > 0 ";
                        strsql += " Then CONVERT(char,cast(sm_Strikeprice AS decimal(18,2))) else convert(char,cast(sm_Strikeprice AS decimal(18,0)))  end else '' end)) + ' ' + Sm_Callput + Sm_optionStyle as smsname, ";
                        strsql += " case td_trxflag When 'O' Then 'O' else 'N' end TRXFLAG,Sum(td_bqty-td_sqty) Qty,  cast(Sum((td_bqty+td_sqty)*td_rate)/Sum(td_bqty+td_sqty) AS decimal(15,4)) tdrate, ";
                        strsql += " cast(td_lastclose AS decimal(15,2)) lastclose,cast(Sum(case right(sm_prodtype,1) when 'F' then  ";
                        strsql += " (((td_bqty-td_sqty)*td_rate*sm_multiplier)-((td_bqty-td_sqty)*td_lastclose* sm_multiplier)) else ((td_bqty-td_sqty)*td_rate*sm_multiplier) end) as decimal(15,2))  drcr, case td_trxflag When 'O' Then '1' else '2' end sortorder ";
                        strsql += " from trades with(" + StrTradesIndex + "nolock), series_master with (nolock) where td_seriesid=sm_seriesid  and td_exchange = sm_exchange  and td_Segment = sm_Segment and ";
                        strsql += " td_clientcd= '" + clientCd + "' and   td_dt= '" + Date + "' and td_exchange= '" + Exch + "' and td_Segment= '" + Seg + "' ";
                        strsql += " Group By sm_prodtype,sm_Symbol, sm_Strikeprice,sm_Productcd, sm_Expirydt,Sm_Callput,Sm_optionStyle,td_trxflag ,td_lastclose,td_Segment ";
                        strsql += " union all ";
                        strsql += " Select left(sm_Productcd,1)+ ' ' + ltrim( substring(sm_Productcd,4,3)) + ' ' + Ltrim(RtRim( sm_Symbol)) + ' ' + left(Ltrim(Rtrim(convert(char, convert(datetime, sm_Expirydt,3),103))),6) + ";
                        strsql += " Right(Ltrim(Rtrim(convert(char, convert(datetime, sm_Expirydt,3),103))),2) + ' ' +  Ltrim(Rtrim(case When sm_Strikeprice  > 0 Then  ";
                        strsql += " case When CEILING(sm_Strikeprice)-Floor(sm_Strikeprice) > 0 Then CONVERT(char,cast(sm_Strikeprice AS decimal(18,2))) else convert(char,cast(sm_Strikeprice AS decimal(18,0)))  end else '' end)) + ' ' + Sm_Callput + Sm_optionStyle as sm_sname,  ";
                        strsql += " ex_eaflag TRXFLAG,Sum(ex_aqty-ex_aqty) Qty, cast(Sum((ex_aqty+ex_aqty)*ex_diffbrokrate)/Sum(ex_aqty+ex_aqty) as decimal(15,2)) td_rate,0 td_lastclose, convert(decimal(15,2),(-(ex_eqty+ex_aqty) * ex_diffbrokrate)) drcr, case ex_eaflag When 'E' Then '3' else '4' end SorOrder ";
                        strsql += " from exercise with (nolock),series_master with (nolock)   where ex_seriesid=sm_seriesid and  ex_exchange = sm_exchange and ex_Segment = sm_Segment and ";
                        strsql += " ex_clientcd= '" + clientCd + "' and   ex_dt= '" + Date + "' and ex_exchange= '" + Exch + "' and ex_Segment= '" + Seg + "' ";
                        strsql += " Group By sm_prodtype,sm_Symbol, sm_Strikeprice,sm_Productcd, sm_Expirydt,Sm_Callput,Sm_optionStyle,ex_eaflag ,ex_eqty, ex_aqty,ex_diffbrokrate";
                        strsql += " union all ";
                        strsql += " Select fc_desc sm_sname,'C' TRXFLAG,0 Qty,0 td_rate,0 td_lastclose,cast(sum(fc_amount) as decimal(15,2))  drcr,5 SorOrder  from fspecialcharges with (nolock)   where ";
                        strsql += " fc_clientcd= '" + clientCd + "' and   fc_dt= '" + Date + "' and fc_exchange= '" + Exch + "' and fc_Segment= '" + Seg + "' group by fc_desc ";
                        strsql += " union all ";
                        strsql += " Select  '[Prev. Day Mrgn.]' sm_sname, 'X' TRXFLAG,0 Qty,0 td_rate,0 td_lastclose,cast(fb_margin1 as decimal(15,2)) drcr,6 SorOrder  from Fbills with (nolock) where ";
                        strsql += " fb_clientcd= '" + clientCd + "' and   fb_billdt= '" + Date + "' and fb_exchange= '" + Exch + "' and fb_Segment= '" + Seg + "' and fb_postmrgyn = 'Y' and fb_margin1 <> 0";
                        strsql += " union all ";
                        strsql += " Select 'CurrMrgn' sm_sname, 'X' TRXFLAG,0 Qty,0 td_rate,0 td_lastclose, cast(fb_margin2 as decimal(15,2)) drcr,7 SorOrder from Fbills with (nolock) where ";
                        strsql += " fb_clientcd= '" + clientCd + "' and   fb_billdt= '" + Date + "' and fb_exchange= '" + Exch + "' and fb_Segment= '" + Seg + "' and fb_postmrgyn = 'Y' and fb_margin2 <> 0    ";
                        strsql += " Order by sortorder ,smsname ";
                    }
                    else if (Seg == "X")
                    {
                        strsql = "Select left(sm_Productcd,1)+ ' ' + ltrim( substring(sm_Productcd,4,3)) + ' ' + Ltrim(RtRim( sm_Symbol)) + ' ' + left(Ltrim(Rtrim(convert(char, convert(datetime, sm_Expirydt,3),103))),6) + ";
                        strsql += " Right(Ltrim(Rtrim(convert(char, convert(datetime, sm_Expirydt,3),103))),2) + ' ' + Ltrim(Rtrim(case When sm_Strikeprice  > 0 Then case When CEILING(sm_Strikeprice)-Floor(sm_Strikeprice) > 0 ";
                        strsql += " Then CONVERT(char,cast(sm_Strikeprice AS decimal(18,2))) else convert(char,cast(sm_Strikeprice AS decimal(18,0)))  end else '' end)) + ' ' + Sm_Callput + Sm_optionStyle as smsname, ";
                        strsql += " case td_trxflag When 'O' Then 'O' else 'N' end TRXFLAG,Sum(td_bqty-td_sqty) Qty,  cast(Sum((td_bqty+td_sqty)*td_rate)/Sum(td_bqty+td_sqty) AS decimal(15,4)) tdrate, ";
                        strsql += " cast(td_lastclose AS decimal(15,2)) lastclose,cast(Sum(case right(sm_prodtype,1) when 'F' then  ";
                        strsql += " (((td_bqty-td_sqty)*td_rate*sm_multiplier)-((td_bqty-td_sqty)*td_lastclose* sm_multiplier)) else ((td_bqty-td_sqty)*td_rate) end) as decimal(15,2))  drcr, case td_trxflag When 'O' Then '1' else '2' end sortorder ";
                        strsql += " from ctrades with( nolock), cseries_master with (nolock) where td_seriesid=sm_seriesid  and td_exchange = sm_exchange   and ";
                        strsql += " td_clientcd= '" + clientCd + "' and   td_dt= '" + Date + "' and td_exchange= '" + Exch + "'";
                        strsql += " Group By sm_prodtype,sm_Symbol, sm_Strikeprice,sm_Productcd, sm_Expirydt,Sm_Callput,Sm_optionStyle,td_trxflag ,td_lastclose ";
                        strsql += " union all ";
                        strsql += " Select fc_desc sm_sname,'C' TRXFLAG,0 Qty,0 td_rate,0 td_lastclose,cast(sum(fc_amount) as decimal(15,2))  drcr,5 SorOrder  from cfspecialcharges with (nolock)   where ";
                        strsql += " fc_clientcd= '" + clientCd + "' and   fc_dt= '" + Date + "' and fc_exchange= '" + Exch + "'   group by fc_desc ";
                        strsql += " union all ";
                        strsql += " Select  '[Prev. Day Mrgn.]' sm_sname, 'X' TRXFLAG,0 Qty,0 td_rate,0 td_lastclose,cast(fb_margin1 as decimal(15,2)) drcr,6 SorOrder  from CFbills with (nolock) where ";
                        strsql += " fb_clientcd= '" + clientCd + "' and   fb_billdt= '" + Date + "' and fb_exchange= '" + Exch + "' and fb_postmrgyn = 'Y' and fb_margin1 <> 0";
                        strsql += " union all ";
                        strsql += " Select 'CurrMrgn' sm_sname, 'X' TRXFLAG,0 Qty,0 td_rate,0 td_lastclose, cast(fb_margin2 as decimal(15,2)) drcr,7 SorOrder from CFbills with (nolock) where ";
                        strsql += " fb_clientcd= '" + clientCd + "' and   fb_billdt= '" + Date + "' and fb_exchange= '" + Exch + "'  and fb_postmrgyn = 'Y' and fb_margin2 <> 0    ";
                        strsql += " Order by sortorder ,smsname";
                    }
                    dtTable = objUtility.OpenDataTable(strsql);
                }
                if (dtTable.Rows.Count > 0)
                {
                    for (var i = 0; i <= dtTable.Rows.Count - 1; i++)
                        dblValue = dblValue + Math.Round(Convert.ToDouble(dtTable.Rows[i]["drcr"]), 2);

                    DataRow dtRow = dtTable.NewRow();
                    dtRow[0] = Interaction.IIf(dblTotal > 0, "Due To Us", "Due To You");
                    dtRow[1] = "Z";
                    dtRow[2] = 0;
                    dtRow[3] = 0;
                    dtRow[4] = 0;
                    dtRow[5] = FormatCurr(dblTotal.ToString(), 2, "Y");
                    //if (Seg == "C")
                    //{
                    //  string strRefDt = objUtility.fnFireQuery("settlements", "se_stdt", "se_stlmnt", Date);
                    //  dtRow[5] = FormatCurr(objUtility.mfnRoundoffCashbill(clientCd, strRefDt, dblTotal, Exch));
                    //}
                    dtRow[6] = 9;
                    dtTable.Rows.Add(dtRow);
                }
                return dtTable;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetOutstandingSummary(string clientCd)
        {
            try
            {
                DataTable dtTable;
                string strsql, strCommex = "", strCommTable = "";
                string strAsOnDt = DateTime.Today.Date.ToString("yyyyMMdd");
                if (GetWebParameter("IsTradeWeb") == "O")
                {
                    strCommex = GetWebParameter("Commex");
                    strsql = "Select '" + strAsOnDt + "' OT_AsOn, case substring(SM_Productcd,1,3) when 'FUT' then 'Futures' else 'Options' end Ot_FutOpt,case";
                    strsql += " td_exchange when 'N' then 'NSE' when 'B' then 'BSE' when 'M' then 'MCX' else '?' end + ' - ' +  case substring(SM_ProductCd,4,3) when 'IDX' then 'Index' when";
                    strsql += " 'STK' then 'Stocks' when 'CUR' then 'Currency' end OT_ExchSeg,  Count(*) Recs, case substring(SM_Productcd,4,3) when 'CUR' then '2' else '1' end  ordr ";
                    strsql += " from Trades with (nolock), Series_master with (nolock)  where td_seriesid=sm_seriesid and td_exchange = sm_exchange and td_Segment = sm_Segment and ";
                    strsql += " td_clientcd='" + clientCd + "' and td_dt <= '" + strAsOnDt + "' and sm_expirydt >= '" + strAsOnDt + "' ";
                    strsql += "  group by td_exchange, substring(SM_Productcd,4,3), substring(SM_Productcd,1,3)";
                    if ((strCommex != ""))
                    {
                        string[] ArrCommex = Strings.Split(GetWebParameter("Commex"), "/");
                        strCommTable = " [" + ArrCommex[0] + "].[" + ArrCommex[1] + "].[" + ArrCommex[2] + "] ";
                        strsql += " Union All ";
                        strsql += " Select '" + strAsOnDt + "' OT_AsOn, case substring(SM_Productcd,1,3) when 'FUT' then 'Futures' else 'Options' end Ot_FutOpt,";
                        strsql += " case td_exchange when 'N' then 'NCDEX' when 'F' then 'NCDEX' when 'M' then 'MCX' else '?' end + ' - ' +  ";
                        strsql += " case substring(SM_ProductCd,4,3) when 'COM' then 'Commodity' else '?' end OT_ExchSeg,";
                        strsql += " Count(*) Recs, '3' ordr ";
                        strsql += " from  " + strCommTable + ".Trades with (nolock), " + strCommTable + ".Series_master with (nolock)  where td_seriesid=sm_seriesid and td_exchange = sm_exchange and ";
                        strsql += " td_clientcd='" + clientCd + "' and td_dt <= '" + strAsOnDt + "' and sm_expirydt >= '" + strAsOnDt + "' ";
                        strsql += "  group by td_exchange, substring(SM_Productcd,4,3), substring(SM_Productcd,1,3)";
                    }
                }
                else
                {
                    strsql = " select max(convert(char,convert(datetime,ot_dt),103)) OT_AsOn, case substring(SM_Productcd,1,3) when 'FUT' then 'Futures' else 'Options' end Ot_FutOpt, ";
                    strsql += " case ot_Exchange when 'N' then 'NSE' when 'B' then 'BSE' when 'M' then 'MCX' else '?' end + ' - ' + ";
                    strsql += " case substring(SM_ProductCd,4,3) when 'IDX' then 'Index' when 'STK' then 'Stocks' when 'CUR' then 'Currency' end OT_ExchSeg, ";
                    strsql += " Count(*) Recs, case substring(SM_Productcd,4,3) when 'CUR' then '2' else '1' end  ordr from Foutstanding with (nolock) , series_master with (nolock) where OT_Exchange=SM_Exchange and OT_SeriesId=SM_SeriesId and  ";
                    strsql += " ot_clientcd='" + clientCd + "' group by OT_Exchange, substring(SM_ProductCd,4,3), substring(SM_ProductCd,1,3), ot_dt ";
                    strsql += " union all ";
                    strsql += " select max(convert(char,convert(datetime,ot_dt),103)) OT_Ason, case substring(SM_Productcd,1,3) when 'FUT' then 'Futures' else 'Options' end Ot_FutOpt, ";
                    strsql += " case ot_Exchange when 'N' then 'NCDEX' when 'F' then 'NCDEX' when 'M' then 'MCX' else '?' end + ' - ' + ";
                    strsql += " case substring(SM_ProductCd,4,3) when 'COM' then 'Commodity' else '?' end OT_ExchSeg, ";
                    strsql += " Count(*) , '3' ordr from Coutstanding with (nolock)  , Cseries_master with (nolock) where OT_Exchange=SM_Exchange and OT_SeriesId=SM_SeriesId and  ";
                    strsql += " ot_clientcd='" + clientCd + "' group by OT_Exchange, substring(SM_ProductCd,4,3), substring(SM_ProductCd,1,3), ot_dt ";
                    strsql += " order by ordr, OT_FutOpt, OT_ExchSeg ";
                }
                dtTable = objUtility.OpenDataTable(strsql);
                return dtTable;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetOutstandingDetail(string clientCd, string FutureOption, string ExchSeg)
        {
            try
            {
                DataTable dtTable;
                string strsql, strCommex = "", strCommTable = "";
                string strAsOnDt = DateTime.Today.Date.ToString("yyyyMMdd");
                if (GetWebParameter("IsTradeWeb") == "O")
                {
                    strCommex = GetWebParameter("Commex");
                    strsql = "  Select case substring(SM_Productcd,1,3) when 'FUT' then 'Futures' else 'Options' end Ot_FutOpt, ";
                    strsql += "  case td_Exchange when 'N' then 'NSE' when 'B' then 'BSE' when 'M' then 'MCX' else '?' end + ' - ' + case substring(SM_ProductCd,4,3) when 'IDX' then 'Index' when 'STK' then 'Stocks' when 'CUR' then 'Currency' end OT_ExchSeg, ";
                    strsql += "  ltrim(rtrim(sm_desc)) sm_desc, sum(td_bqty) ot_bqty ,sum(td_sqty) ot_sqty, sum(td_bqty-td_sqty) as net,  ";
                    strsql += "  convert(decimal(15,2), case sum(td_bqty -td_sqty) when 0 then 0 else abs(sum((td_bqty -td_sqty)*td_rate)/sum(td_bqty-td_sqty)) end) ot_avgrate,";
                    strsql += " convert(decimal(15,2), isnull((select ms_lastprice from Market_summary with (nolock) where ms_exchange = td_exchange and ms_Segment = td_Segment and ms_seriesid = td_seriesid and ms_dt = (select max(ms_dt) from Market_summary with (nolock) where ms_exchange = td_exchange and ms_Segment = td_Segment and ms_seriesid = td_seriesid and  ms_dt <= '" + strAsOnDt + "')),0)) ot_closeprice,";
                    strsql += " convert(decimal(15,2), isnull((select ms_lastprice from Market_summary with (nolock) where ms_exchange = td_exchange and ms_Segment = td_Segment and ms_seriesid = td_seriesid and ms_dt = (select max(ms_dt) from Market_summary with (nolock) where ms_exchange = td_exchange and ms_Segment = td_Segment and ms_seriesid = td_seriesid and  ms_dt <= '" + strAsOnDt + "')),0)*sum(td_bqty-td_sqty)) Closing,";
                    strsql += "  convert(char, convert(datetime, '" + strAsOnDt + "'),103) ot_dt , ";
                    strsql += "  case td_exchange when 'N' then 1 else 2 end Td_order ";
                    strsql += "  from Trades with (nolock), Series_master with (nolock)  ";
                    strsql += "  where td_seriesid=sm_seriesid and td_exchange = sm_exchange and td_dt <= '" + strAsOnDt + "' and sm_expirydt >= '" + strAsOnDt + "' and td_Segment = sm_Segment";
                    strsql += "  and td_clientcd = '" + clientCd + "' and sm_expirydt >= '" + strAsOnDt + "' ";
                    if (FutureOption != "")
                        strsql += " and case substring(SM_Productcd,1,3) when 'FUT' then 'Futures' else 'Options' end = '" + FutureOption + "'";
                    if (ExchSeg != "")
                        strsql += " and case td_Exchange when 'N' then 'NSE' when 'B' then 'BSE' when 'M' then 'MCX' else '?' end + ' - ' + case substring(SM_ProductCd,4,3) when 'IDX' then 'Index' when 'STK' then 'Stocks' when 'CUR' then 'Currency' end = '" + ExchSeg + "'";
                    strsql += " group by sm_productcd,td_exchange,sm_desc,td_Segment,td_seriesid";
                    strsql += " Having Sum(td_bqty - td_sqty) <> 0 ";
                    if ((strCommex != ""))
                    {
                        string[] ArrCommex = Strings.Split(GetWebParameter("Commex"), "/");
                        strCommTable = " [" + ArrCommex[0] + "].[" + ArrCommex[1] + "].[" + ArrCommex[2] + "]";
                        strsql += "  union all";
                        strsql += "  Select case substring(SM_Productcd,1,3) when 'FUT' then 'Futures' else 'Options' end Ot_FutOpt,";
                        strsql += "  case td_Exchange when 'N' then 'NCDEX' when 'M' then 'MCX' when 'F' Then 'NCDEX' else '?' end + ' - ' + case substring(SM_ProductCd,4,3) when 'COM' then 'Commodity' else '?' end OT_ExchSeg,  ";
                        strsql += "  ltrim(rtrim(sm_desc)) sm_desc,sum(td_bqty) ot_bqty ,sum(td_sqty) ot_sqty, sum(td_bqty-td_sqty) as net,convert(decimal(15,2), case sum(td_bqty -td_sqty) when 0 then 0 else abs(sum((td_bqty -td_sqty)*td_rate)/sum(td_bqty-td_sqty)) end) ot_avgrate ,";
                        strsql += " convert(decimal(15,2), isnull((select ms_lastprice from Market_summary with (nolock) where ms_exchange = td_exchange and ms_seriesid = td_seriesid and ms_dt = (select max(ms_dt) from Market_summary with (nolock) where ms_exchange = td_exchange and ms_seriesid = td_seriesid and  ms_dt <= '" + strAsOnDt + "')),0)) ot_closeprice,";
                        strsql += " convert(decimal(15,2), isnull((select ms_lastprice from Market_summary with (nolock) where ms_exchange = td_exchange and ms_seriesid = td_seriesid and ms_dt = (select max(ms_dt) from Market_summary with (nolock) where ms_exchange = td_exchange and ms_seriesid = td_seriesid and  ms_dt <= '" + strAsOnDt + "')),0)*sum(td_bqty-td_sqty)) Closing,";
                        strsql += "  convert(char, convert(datetime, '" + strAsOnDt + "'),103) ot_dt , ";
                        strsql += "  3 Td_order from " + strCommTable + ".Trades  with (nolock), " + strCommTable + ".Series_master with (nolock) ";
                        strsql += "  where td_seriesid=sm_seriesid and td_exchange = sm_exchange ";
                        strsql += "  and td_clientcd = '" + clientCd + "' and td_dt <= '" + strAsOnDt + "' and sm_expirydt >= '" + strAsOnDt + "'";
                        if (FutureOption != "")
                            strsql += " and case substring(SM_Productcd,1,3) when 'FUT' then 'Futures' else 'Options' end = '" + FutureOption + "'";
                        if (ExchSeg != "")
                            strsql += " and case td_Exchange when 'N' then 'NCDEX' when 'M' then 'MCX' when 'F' Then 'NCDEX' else '?' end + ' - ' + case substring(SM_ProductCd,4,3) when 'COM' then 'Commodity' else '?' end = '" + ExchSeg + "'";
                        strsql += " group by sm_productcd,td_exchange,sm_desc,td_seriesid";
                        strsql += " Having Sum(td_bqty - td_sqty) <> 0 ";
                        strsql += "  order by Td_order, sm_desc ";
                    }
                }
                else
                {
                    // **************Tradeweb************************
                    strsql = "  Select case substring(SM_Productcd,1,3) when 'FUT' then 'Futures' else 'Options' end Ot_FutOpt, ";
                    strsql += "  case ot_Exchange when 'N' then 'NSE' when 'B' then 'BSE' when 'M' then 'MCX' else '?' end + ' - ' + case substring(SM_ProductCd,4,3) when 'IDX' then 'Index' when 'STK' then 'Stocks' when 'CUR' then 'Currency' end OT_ExchSeg, ";
                    strsql += "  ltrim(rtrim(sm_desc)) sm_desc, ot_bqty,ot_sqty,(ot_bqty-ot_sqty) as net,  ";
                    strsql += "  cast(ot_avgrate as decimal(15,2)) ot_avgrate, cast(ot_closeprice as decimal(15,2))ot_closeprice, ";
                    strsql += "  cast(((ot_bqty-ot_sqty)*ot_closeprice*sm_multiplier)as decimal(15,2)) as Closing, ";
                    strsql += "  convert(char,convert(datetime,ot_dt),103) ot_dt , ";
                    strsql += "  case ot_exchange when 'N' then 1 else 2 end Td_order ";
                    strsql += "  from Foutstanding with (nolock), Series_master with (nolock)  ";
                    strsql += "  where ot_seriesid=sm_seriesid and ot_exchange = sm_exchange and ot_Segment = sm_Segment ";
                    strsql += "  and ot_clientcd = '" + clientCd + "' ";
                    if (FutureOption != "")
                        strsql += " and case substring(SM_Productcd,1,3) when 'FUT' then 'Futures' else 'Options' end = '" + FutureOption + "'";
                    if (ExchSeg != "")
                        strsql += " and case ot_Exchange when 'N' then 'NSE' when 'B' then 'BSE' when 'M' then 'MCX' else '?' end + ' - ' + case substring(SM_ProductCd,4,3) when 'IDX' then 'Index' when 'STK' then 'Stocks' when 'CUR' then 'Currency' end = '" + ExchSeg + "'";
                    strsql += "  and (ot_bqty-ot_sqty) <> 0 ";
                    strsql += "  union all";
                    strsql += "  Select case substring(SM_Productcd,1,3) when 'FUT' then 'Futures' else 'Options' end Ot_FutOpt,";
                    strsql += "  case ot_Exchange when 'N' then 'NCDEX' when 'F' then 'NCDEX' when 'M' then 'MCX' else '?' end + ' - ' + case substring(SM_ProductCd,4,3) when 'COM' then 'Commodity' else '?' end OT_ExchSeg,  ";
                    strsql += "  ltrim(rtrim(sm_desc)) sm_desc,ot_bqty,ot_sqty,(ot_bqty-ot_sqty) as net, cast(ot_avgrate as decimal(15,2))ot_avgrate,cast(ot_closeprice as decimal(15,2))ot_closeprice, ";
                    strsql += "  cast(((ot_bqty-ot_sqty)*ot_closeprice*sm_multiplier)as decimal(15,2)) as Closing, ";
                    strsql += "  convert(char,convert(datetime,ot_dt),103) ot_dt , ";
                    strsql += "  3 Td_order from Coutstanding with (nolock),CSeries_master with (nolock) ";
                    strsql += "  where ot_seriesid=sm_seriesid and ot_exchange = sm_exchange ";
                    strsql += "  and ot_clientcd = '" + clientCd + "'";
                    if (FutureOption != "")
                        strsql += " and case substring(SM_Productcd,1,3) when 'FUT' then 'Futures' else 'Options' end = '" + FutureOption + "'";
                    if (ExchSeg != "")
                        strsql += " and case ot_Exchange when 'N' then 'NCDEX' when 'F' then 'NCDEX' when 'M' then 'MCX' else '?' end + ' - ' + case substring(SM_ProductCd,4,3) when 'COM' then 'Commodity' else '?' end = '" + ExchSeg + "'";
                    strsql += "  and (ot_bqty-ot_sqty) <> 0 ";
                    strsql += "  order by Td_order, sm_desc ";
                }
                dtTable = objUtility.OpenDataTable(strsql);
                return dtTable;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetRMSPayoutAmount(string clientCd, string Type)
        {
            try
            {
                DataTable dtTable;
                string strSQL = "";
                if ("FS".Contains(Type))
                {
                    strSQL = " select sp_sysvalue from Sysparameter Where sp_parmcd = 'RMSFORMULA' + (select SUBSTRING ( sp_parmcd,8,2) from Sysparameter Where sp_sysvalue = '" + Interaction.IIf(Type == "F", "FUNDPAYOUT", "SHAREPAYOUT") + "')";
                    DataTable dtTemp = objUtility.OpenDataTable(strSQL);
                    if (dtTemp.Rows.Count > 0)
                    {
                        strSQL = "select rs_clientcd ClientCode , CONVERT( numeric(18,2), Sum(" + Interaction.IIf(Type == "F", "rs_FundPayout", "rs_SharePayout") + ")) Amount from RMS_Summary Where rs_Clientcd = '" + clientCd + "' and rs_Dt = (Select max(rs_dt) from RMS_Summary) Group By rs_clientcd ";
                        dtTable = objUtility.OpenDataTable(strSQL);
                        return dtTable;
                    }
                    else
                    {
                        string strTDay = DateTime.Today.Date.ToString("yyyyMMdd");
                        string strCompanyCode = Strings.Trim(objUtility.OpenDataTable("select min(em_cd) from Entity_master").Rows[0][0].ToString());
                        string strT1Day = objUtility.mfnGetTPlusDt("", strTDay, 1);
                        string strT2Day = objUtility.mfnGetTPlusDt("", strTDay, 2);

                        strSQL = " select ld_clientcd ClientCode, case When T2Day > case When TDay > T1Day Then TDay else T1Day end Then  T2Day else case When TDay > T1Day Then TDay else T1Day end  end " + Interaction.IIf(Type == "F", "*1", "*-1") + " Amount  ";
                        strSQL += " from ( select ld_clientcd,Sum(case When ld_Dt <= '" + strTDay + "' Then ld_amount else 0 end) TDay, ";
                        strSQL += " Sum(case When ld_Dt <= '" + strT1Day + "' Then ld_amount else 0 end) T1Day, ";
                        strSQL += " Sum(case When ld_Dt <= '" + strT2Day + "' Then ld_amount else 0 end) T2Day ";
                        strSQL += " from Ledger ";
                        strSQL += " Where ld_dt <= '" + strT2Day + "'  ";
                        strSQL += " and ld_clientcd = '" + clientCd + "' ";
                        strSQL += " Group By ld_clientcd ) a ";
                        dtTable = objUtility.OpenDataTable(strSQL);
                        if (dtTable.Rows.Count > 0)
                            return dtTable;
                        else
                        {
                            dtTable = objUtility.OpenDataTable("Select 'InValid " + Interaction.IIf(Type == "F", "FUNDPAYOUT", "SHAREPAYOUT") + " Setup' ErrorMG");
                            return dtTable;
                        }
                    }
                }
                else
                {
                    dtTable = objUtility.OpenDataTable("Select 'InValid strType value' ErrorMG");
                    return dtTable;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetInvestorPLCash(string clientCd, string FromDt, string ToDt, string ScripCd, string ReportType, string StockValuation)
        {
            try
            {
                DataTable DtTable;
                SqlTransaction objTrans;
                string connetionString = objUtility.GetConnectionStr();
                using (SqlConnection objConnection = new SqlConnection(connetionString))
                {
                    objConnection.Open();
                    objTrans = objConnection.BeginTransaction();
                    SqlCommand cmd = objConnection.CreateCommand();
                    cmd.Transaction = objTrans;
                    SqlDataAdapter sqlDtAdap = new SqlDataAdapter();
                    SqlCommandBuilder sqlCmdBld = new SqlCommandBuilder(sqlDtAdap);

                    if (string.IsNullOrWhiteSpace(ScripCd))
                    {
                        ScripCd = "";
                    }

                    fnGetGenerateCash(objConnection, objTrans, clientCd, FromDt, ToDt, ReportType, ScripCd, StockValuation);
                    if (ReportType == "C")
                    {
                        strsql = " SELECT ic_desc ChargeDesc,Sum(ic_amount) Charges " +
                            " FROM #invcharges " +
                            " Group BY ic_desc Having Sum(ic_amount) <> 0 Order by ic_desc   ";
                    }
                    else if (ReportType == "D")
                    {
                        strsql = " SELECT td_stlmnt Stlmnt,td_clientcd ClientCode,td_scripcd ScripCode,ss_Name ScripName, " +
                            " td_dt TradeDt,td_bsflag BSFlag,td_bqty+td_sqty Qty,td_rate Rate,td_marketrate MarketRate,td_flag Flag " +
                            " FROM #VX,Securities " + " Where td_scripcd = ss_cd " +
                            " oRDER BY ScripName,TD_SCRIPCD,TD_DT,td_FLAG ";
                    }
                    else if (ReportType == "S" & StockValuation == "2")
                    {
                        strsql = " select td_scripcd ScripCode,ss_Name ScripName,Sum(td_bqty) BQty,Sum(td_bqty*td_rate) BAmount,Sum(td_sqty) SQty,Sum(td_sqty*td_rate) SAmount," +
                            " Sum(td_bqty-td_sqty) NetQty, " +
                            " case When Abs(Sum(td_bqty-td_sqty)) > 0 Then  " +
                            " abs(Sum(td_bqty-td_sqty)) * (Sum((td_bqty+td_sqty)*td_rate)/abs(Sum(td_bqty+td_sqty))) " +
                            " else 0 end NetAmount, " +
                            " case When abs(Sum(td_bqty-td_sqty)) = 0 Then  " +
                            " Sum(td_sqty*td_rate) - Sum(td_bqty*td_rate) " +
                            " Else " +
                            " (Sum(td_bqty-td_sqty)) * (Sum((td_bqty+td_sqty)*td_rate)/abs(Sum(td_bqty+td_sqty)))  " +
                            " + Sum(td_sqty*td_rate) - Sum(td_bqty*td_rate) " +
                            " end PL,td_flag Flag" +
                            " from #VX,Securities " +
                            " Where td_scripcd = ss_cd " +
                            " Group by ss_Name,td_scripcd,td_MarketRate,td_flag " +
                            " Order by ss_Name,td_scripcd ";
                    }
                    else if (ReportType == "S" & StockValuation == "3")
                    {
                        strsql = " select td_scripcd ScripCode,ss_Name ScripName,Sum(td_bqty) BQty,Sum(td_bqty*td_rate) BAmount,Sum(td_sqty) SQty,Sum(td_sqty*td_rate) SAmount, " +
                            " Sum(td_bqty-td_sqty) NetQty, " +
                            " case When Abs(Sum(td_bqty-td_sqty)) > 0 Then " +
                            " abs(Sum(td_bqty-td_sqty)) * (Sum((td_bqty+td_sqty)*td_rate)/abs(Sum(td_bqty+td_sqty))) " +
                            " else 0 end NetAmount, " +
                            " case When abs(Sum(td_bqty-td_sqty)) = 0 Then " +
                            " Sum(td_sqty*td_rate) - Sum(td_bqty*td_rate) " +
                            " else " +
                            " (Sum(td_bqty-td_sqty)) * (Sum((td_bqty+td_sqty)*td_rate)/abs(Sum(td_bqty+td_sqty))) " +
                            " + Sum(td_sqty*td_rate) - Sum(td_bqty*td_rate) " +
                            " end PL,td_flag Flag" +
                            " from #VX,Securities " +
                            " Where td_scripcd= ss_cd " +
                            " Group by ss_Name,td_scripcd,td_MarketRate,td_flag " +
                            " Order by ss_Name,td_scripcd ";
                    }
                    else if (ReportType == "S" & StockValuation == "4")
                    {
                        strsql = " select td_scripcd ScripCode,ss_Name ScripName,Sum(td_bqty) BQty,Sum(td_bqty*td_rate) BAmount,Sum(td_sqty) SQty,Sum(td_sqty*td_rate) SAmount," +
                            " Sum(td_bqty-td_sqty) NetQty, " +
                            " case When Abs(Sum(td_bqty-td_sqty)) > 0 Then " +
                            " abs(Sum(td_bqty-td_sqty)) * " +
                            " case When td_MarketRAte > (Sum((td_bqty+td_sqty)*td_rate)/abs(Sum(td_bqty+td_sqty))) " +
                            " Then (Sum((td_bqty+td_sqty)*td_rate)/abs(Sum(td_bqty+td_sqty))) " +
                            " else td_MarketRAte  end " +
                            " else 0 end NetAmount, " +
                            " case When abs(Sum(td_bqty-td_sqty)) = 0 Then " +
                            " Sum(td_sqty*td_rate) - Sum(td_bqty*td_rate) " +
                            " else " +
                            " ( " +
                            " (Sum(td_bqty-td_sqty)) * " +
                            " case When td_MarketRAte > (Sum((td_bqty+td_sqty)*td_rate)/abs(Sum(td_bqty+td_sqty))) " +
                            " Then (Sum((td_bqty+td_sqty)*td_rate)/abs(Sum(td_bqty+td_sqty))) " +
                            " else td_MarketRAte  end " +
                            " ) + Sum(td_sqty*td_rate) - Sum(td_bqty*td_rate) " +
                            " end PL,td_flag Flag" +
                            " from #VX,Securities " +
                            " Where td_scripcd= ss_cd " +
                            " Group by ss_Name,td_scripcd,td_MarketRate,td_flag " +
                            " Order by ss_Name,td_scripcd ";
                    }
                    else
                    {
                        strsql = " select td_scripcd ScripCode,ss_Name ScripName,Sum(td_bqty) BQty,Sum(td_bqty*td_rate) BAmount,Sum(td_sqty) SQty,Sum(td_sqty*td_rate) SAmount, " +
                            " Sum(td_bqty-td_sqty) NetQty,Sum((td_bqty-td_sqty)*td_MarketRate) NetAmount, " +
                            " case When abs(Sum(td_bqty-td_sqty)) = 0 Then " +
                            " Sum(td_sqty*td_rate) - Sum(td_bqty*td_rate) " +
                            " else " +
                            " Sum((td_bqty-td_sqty)*td_MarketRate)  - Sum(td_bqty*td_rate) + Sum(td_sqty*td_rate) " +
                            " end PL,td_MarketRate MarketRate " +
                            " from #VX,Securities " +
                            " Where td_scripcd= ss_cd " +
                            " Group by ss_Name,td_scripcd,td_MarketRate " +
                            " Order by ss_Name,td_scripcd ";
                    }
                    DtTable = OpenDataTableTemp(strsql, objConnection, objTrans);
                    return DtTable;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetParameter(string ParameterCd)
        {
            try
            {
                string strValue = "";
                switch (ParameterCd.Trim().ToUpper() ?? "")
                {
                    case "COMPANYNAME":
                        {
                            if (Convert.ToDouble(Strings.Trim(objUtility.fnFireQuery("Entity_master", "count(0)", " em_cd='B' and em_name like 'SYKES & RAY EQUITIES%' and 1", "1"))) > 0d)
                            {
                                strValue = Strings.Trim(objUtility.fnFireQuery("Entity_master", "em_Name", "em_cd= 'B' and 1", "1"));
                            }
                            else
                            {
                                strValue = Strings.Trim(objUtility.fnFireQuery("Entity_master", "em_Name", "em_cd=(select min(em_cd) from Entity_master Where Len(Ltrim(Rtrim(em_cd))) = 1) and 1", "1"));
                            }

                            break;
                        }
                    case "PANASPASSWORD":
                        {
                            strValue = GetWebParameter("PANASPASSWORD").Trim().ToUpper();
                            break;
                        }
                    case "FPOUT_RQ_TM":
                        {
                            if (GetWebParameter("IsTradeWeb") == "O")
                            {
                                strValue = GetSysParameter("FPOUT_RQ_TM").Trim().ToUpper();
                            }
                            else
                            {
                                strValue = "InValid Parameter";
                            }

                            break;
                        }
                    case "ESCALATURL":
                        {
                            strValue = GetWebParameter("ESCALATURL").Trim();
                            break;
                        }
                    case "OURDETAILURL":
                        {
                            strValue = GetWebParameter("OURDETAILURL").Trim();
                            break;
                        }

                    default:
                        {
                            strValue = "InValid Parameter";
                            break;
                        }
                }
                DataTable dtTable = objUtility.OpenDataTable("Select '" + strValue + "' Value ");
                return dtTable;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic GetReportParm(string clientCd)
        {
            try
            {
                string strsql;
                DataTable dtTable;
                string strBENHLD = "";
                string strDPHLD = "";
                string strSTT = GetWebParameter("REQ_STT");
                string stLEDGER = GetWebParameter("REQ_LEDGER");
                string strPNL = GetWebParameter("REQ_PNL");
                string strSTTCURRYR = GetWebParameter("REQ_STTCURRYR");
                if (Convert.ToInt16(objUtility.fnFireQuery("WebParameter", "count(*)", "sp_parmcd", "REQ_DPHLD")) > 0)
                {
                    strDPHLD = GetWebParameter("REQ_DPHLD");
                }
                if (Convert.ToInt16(objUtility.fnFireQuery("WebParameter", "count(*)", "sp_parmcd", "REQ_BENHLD")) > 0)
                {
                    strBENHLD = GetWebParameter("REQ_BENHLD");
                }
                string strFUNDS = GetWebParameter("REQ_FUNDS");
                string strSHARES = GetWebParameter("REQ_SHARES");

                strsql = "select '" + strSTT + "' strSTT,'" + stLEDGER + "' stLEDGER,'" + strPNL + "' strPNL,'" + strDPHLD + "' strDPHLD ,'" + strBENHLD + "' strBENHLD ,'" + strSTTCURRYR + "' strSTTCURRYR,'" + strFUNDS + "' strFUNDS,'" + strSHARES + "' strSHARES";
                dtTable = objUtility.OpenDataTable(strsql);
                return dtTable;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic SMSSetting()
        {
            try
            {
                DataTable dtTable;
                var strSMSParamVal = new string[5];
                string strSMSParameter = "SMSUSERID/SMSPWD/SMSSENDER/SMSLENGTH/SMSLINK";
                string strvalue = string.Empty;

                for (int i = 0; i <= 4; i++)
                {
                    strvalue = strSMSParameter.Split('/')[i];
                    strSMSParamVal[i] = objUtility.fnFireQuery("sysparameter", "SP_SYSVALUE", "sp_parmcd", strvalue, true);
                }

                if (!string.IsNullOrEmpty(strSMSParamVal[0]) && !string.IsNullOrEmpty(strSMSParamVal[1]) && !string.IsNullOrEmpty(strSMSParamVal[2]) && !string.IsNullOrEmpty(strSMSParamVal[3]) && !string.IsNullOrEmpty(strSMSParamVal[4]))
                {
                    dtTable = objUtility.OpenDataTable("Select 'Y' Response");
                    return dtTable;
                }
                else
                {
                    dtTable = objUtility.OpenDataTable("Select 'N' Response");
                    return dtTable;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic ForgetPasswordSMS(string clientCd)
        {
            #region old code
            //try
            //{
            //    string strsql = "";
            //    DataTable dtTable;
            //    SqlTransaction objTrans;
            //    var strSMSParamVal = new string[5];
            //    string strSMSParameter = "SMSUSERID/SMSPWD/SMSSENDER/SMSLENGTH/SMSLINK";
            //    string strvalue = string.Empty;
            //    string strPass = "";
            //    string strMsgTxt = "";
            //    string strUser = "";
            //    string strMobile = "";
            //    string strFromNo1 = "";
            //    string strFromNo2 = "";
            //    string strresponse = "";

            //    string connetionString = objUtility.GetConnectionStr();
            //    using (SqlConnection objConnection = new SqlConnection(connetionString))
            //    {
            //        objConnection.Open();
            //        objTrans = objConnection.BeginTransaction();
            //        SqlCommand cmd = objConnection.CreateCommand();
            //        cmd.Transaction = objTrans;
            //        SqlDataAdapter sqlDtAdap = new SqlDataAdapter();
            //        SqlCommandBuilder sqlCmdBld = new SqlCommandBuilder(sqlDtAdap);

            //        if (string.IsNullOrEmpty(clientCd.Trim()))
            //        {
            //            dtTable = objUtility.OpenDataTable("Select 'Client Not Found' Response");
            //            return dtTable;
            //        }
            //        clientCd = clientCd.Trim().Replace("'", "");
            //        if (Convert.ToInt16(objUtility.fnFireQuery("Client_master", "count(0)", "cm_cd", clientCd.Trim(), true)) > 0)
            //        {
            //            string strMobileNo = objUtility.fnFireQuery("Client_master", "cm_mobile", "cm_cd", clientCd.Trim(), true).Trim();
            //            if (string.IsNullOrEmpty(strMobileNo.Trim()))
            //            {
            //                dtTable = objUtility.OpenDataTable("Select 'Mobile Number is not registered with us' Response");
            //                return dtTable;
            //            }
            //            strPass = objUtility.fnFireQuery("Client_master", "cm_pwd", "cm_cd", clientCd.Trim(), true);
            //            if (Convert.ToInt16(objUtility.fnFireQuery("Sysparameter", "count(0)", "sp_parmcd", "SMSTWEBPWD", true)) > 0)
            //            {
            //                strMsgTxt = objUtility.fnFireQuery("sysparameter", "SP_SYSVALUE", "sp_parmcd", "SMSTWEBPWD", true);
            //            }
            //            else if (Convert.ToInt16(objUtility.fnFireQuery("Webparameter", "count(0)", "sp_parmcd", "SMSGETPWD", true)) > 0)
            //            {
            //                strMsgTxt = objUtility.fnFireQuery("Webparameter", "SP_SYSVALUE", "sp_parmcd", "SMSGETPWD", true);
            //            }
            //            else
            //            {
            //                strMsgTxt = "Your password to access TradeWeb is <Password> login ID : <Client>";
            //            }
            //            var sb = new StringBuilder();
            //            sb = new StringBuilder(strMobileNo);
            //            int i;
            //            var loopTo = strMobileNo.Length - 3;
            //            for (i = 2; i <= loopTo; i++)
            //                sb[i] = 'x';
            //            strMobile = sb.ToString();
            //            sb = new StringBuilder(clientCd.Trim().ToUpper());
            //            var loopTo1 = clientCd.Length - 1;
            //            for (i = 1; i <= loopTo1; i++)
            //                sb[i] = 'x';
            //            strUser = sb.ToString();
            //            strMsgTxt = strMsgTxt.Replace("<Client>", strUser);
            //            strMsgTxt = strMsgTxt.Replace("<Password>", strPass);

            //            strMsgTxt = strMsgTxt.Replace("<&>", "<~>");
            //            strMsgTxt = strMsgTxt.Replace("&", "");
            //            strMsgTxt = strMsgTxt.Replace("<~>", "&");

            //            for (i = 0; i <= 4; i++)
            //            {
            //                strvalue = strSMSParameter.Split('/')[i];
            //                strSMSParamVal[i] = objUtility.fnFireQuery("sysparameter", "SP_SYSVALUE", "sp_parmcd", strvalue, true);
            //            }

            //            string strURLLink = strSMSParamVal[4];

            //            if (strURLLink.IndexOf("<USERID>") != -1 & !string.IsNullOrEmpty(strSMSParamVal[0].Trim()))
            //            {
            //                strURLLink = strURLLink.Replace("<USERID>", strSMSParamVal[0].Trim());
            //            }
            //            if (strURLLink.IndexOf("<PASSWORD>") != -1 & !string.IsNullOrEmpty(strSMSParamVal[1].Trim()))
            //            {
            //                strURLLink = strURLLink.Replace("<PASSWORD>", strSMSParamVal[1].Trim());
            //            }
            //            if (string.IsNullOrEmpty(strSMSParamVal[2].Trim()))
            //            {
            //                if (strURLLink.IndexOf("<SENDERID>") != -1)
            //                {
            //                    strURLLink = strURLLink.Replace("<SENDERID>", strSMSParamVal[2].Trim());
            //                }
            //            }
            //            else
            //            {
            //                strFromNo1 = strSMSParamVal[2].Trim();
            //                if (strFromNo1.IndexOf("|") != -1)
            //                {
            //                    strFromNo2 = strFromNo1.Split("|")[1];
            //                    strFromNo1 = strFromNo1.Split("|")[0];
            //                }
            //                else
            //                {
            //                    strFromNo1 = Strings.Left(strFromNo1.Trim(), 10);
            //                    strFromNo2 = "";
            //                }
            //                if (strURLLink.IndexOf("<SENDERID>") != -1)
            //                {
            //                    strURLLink = strURLLink.Replace("<SENDERID>", strSMSParamVal[2].Trim());
            //                }
            //                else if (strURLLink.IndexOf("<SENDERID1>") != -1 | strURLLink.IndexOf("<SENDERID2>") != -1)
            //                {
            //                    strURLLink = strURLLink.Replace("<SENDERID1>", strFromNo1).Replace("<SENDERID2>", strFromNo2);
            //                }
            //            }
            //            strURLLink = strURLLink.Replace("<MESSAGE>", strMsgTxt);

            //            if (strURLLink.IndexOf("/opted.smsapi.org/v1.0.7/") != -1)
            //            {
            //                strURLLink = strURLLink.Replace("<MESSAGE>", strMsgTxt);
            //            }

            //            if (strURLLink.IndexOf("/174.143.34.193/") != -1)
            //            {
            //                if (strMsgTxt.Trim().Length > 160)
            //                {
            //                    strURLLink = strURLLink + "&mt=4";
            //                }
            //                else
            //                {
            //                    strURLLink = strURLLink + "&mt=0";
            //                }
            //                strURLLink = strURLLink + "&typeofmessage=1";
            //            }

            //            if (GetSysParameter("SMSCOUNTRYCD") == "Y")
            //            {
            //                if (strMobileNo.Trim().Length == 10)
            //                {
            //                    strMobileNo = "91" + strMobileNo;
            //                }
            //            }
            //            else if (strMobileNo.Trim().Length > 10)
            //            {
            //                strMobileNo = Strings.Right(strMobileNo.Trim(), 10);
            //            }
            //            strURLLink = strURLLink.Replace("<CLIENTMOBILE>", strMobileNo.Trim());

            //            if (strURLLink.IndexOf("myvaluefirst.com") != -1)
            //            {
            //                string strSENDER = "";
            //                if (strSMSParamVal[2].Trim().IndexOf("|") != -1)
            //                {
            //                    if (Strings.Left(strMobileNo.Trim(), 2) == "92" | Strings.Left(strMobileNo.Trim(), 2) == "93")
            //                    {
            //                        strSENDER = strSMSParamVal[2].Trim().Split("|")[1];
            //                    }
            //                    else
            //                    {
            //                        strSENDER = strSMSParamVal[2].Trim().Split("|")[0];
            //                    }
            //                }
            //                else
            //                {
            //                    strSENDER = Strings.Left(strSMSParamVal[2].Trim(), 10);
            //                }
            //                strURLLink = strURLLink.Replace("<SENDERID3>", strSENDER);
            //            }

            //            if (GetWebParameter("SECURITYPROT").Trim() == "TLS12")
            //            {
            //                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            //            }

            //            string content = "";

            //            try
            //            {
            //                HttpWebRequest http = (HttpWebRequest)WebRequest.Create(strURLLink);
            //                ServicePointManager.Expect100Continue = true;
            //                HttpWebResponse response = (HttpWebResponse)http.GetResponse();
            //                var sr = new StreamReader(response.GetResponseStream());
            //                content = sr.ReadToEnd();
            //                strresponse = content;

            //                if (response.StatusCode == HttpStatusCode.OK)
            //                {
            //                    strresponse = "SMS Sent Successfully.";
            //                }
            //                else if (Strings.InStr(1, content, "<ERROR>") > 0)
            //                {
            //                    if (Strings.InStr(1, strresponse, "<DESC>") > 0)
            //                    {
            //                        strresponse = Strings.Mid(content, Strings.InStr(1, content, "<DESC>") + 6);
            //                        strresponse = Strings.Left(content, Strings.InStr(1, content, "</DESC>") - 1);
            //                    }
            //                    else
            //                    {
            //                        strresponse = "SMS Sent Successfully.";
            //                    }
            //                }
            //                else if (Strings.InStr(1, content, "\"error-status\":\"Success\"") > 0)
            //                {
            //                    strresponse = "SMS Sent Successfully.";
            //                }
            //                else if (Strings.InStr(1, content, Strings.Trim(strMobileNo)) > 0)
            //                {
            //                    strresponse = "Message Send Successfully.";
            //                }
            //                else if (Strings.InStr(1, content, "<sms>") > 0)
            //                {
            //                    if (Strings.InStr(1, content, "-1") > 0)
            //                    {
            //                        strresponse = "Message Sending Failed";
            //                    }
            //                    else if (Strings.InStr(1, Strings.UCase(content), Strings.UCase("Invalid Username Or Password")) > 0)
            //                    {
            //                        strresponse = "Sending Failed. Invalid Username Or Password.";
            //                    }
            //                    else
            //                    {
            //                        strresponse = "Message Send Successfully.";
            //                    }
            //                }
            //                else if (Strings.InStr(1, Strings.UCase(content), "FAIL") > 0)
            //                {
            //                    strresponse = "Message Sending Failed";
            //                }
            //                else if (Strings.InStr(1, Strings.UCase(content), "INVALID USERNAME OR PASSWORD") > 0 | Strings.InStr(1, Strings.UCase(content), "INVALID USERNAME AND PASSWORD") > 0)
            //                {
            //                    strresponse = "Message Sending Failed. Invalid Username Or Password.";
            //                }
            //                else if (Strings.InStr(1, Strings.UCase(content), "1701|") > 0 | Strings.InStr(1, Strings.UCase(content), "SUCCESS") > 0)
            //                {
            //                    strresponse = "Message Send Successfully.";
            //                }
            //                else if (Strings.InStr(1, Strings.UCase(content), ":") > 0)
            //                {
            //                    if (string.IsNullOrEmpty(Strings.Split(Strings.UCase(content), ":")[1]))
            //                    {
            //                        strresponse = content;
            //                    }
            //                    else
            //                    {
            //                        strresponse = "Message Send Successfully.";
            //                    }
            //                }
            //                else if (content == "100")
            //                {
            //                    strresponse = "Message Send Successfully.";
            //                }
            //                else if (Strings.InStr(1, Strings.UCase(content), "GID") > 0)
            //                {
            //                    strresponse = "Message Send Successfully.";
            //                }
            //                else
            //                {
            //                    strresponse = content;
            //                }
            //            }
            //            catch (Exception ex)
            //            {
            //            }

            //            if (Convert.ToInt16(objUtility.fnFireQuery("SysObjects", "count(*)", "name", "SMS_Logs", true)) == 0)
            //            {
            //                strsql = "";
            //                strsql += " CREATE TABLE [dbo].[SMS_Logs]( ";
            //                strsql += " [sl_CmCd] [char](16) NOT NULL, ";
            //                strsql += " [sl_Mobile] [char](15) NOT NULL, ";
            //                strsql += " [sl_Message] [varchar](2000) NOT NULL, ";
            //                strsql += " [sl_Response] [char](150) NOT NULL, ";
            //                strsql += " [mkrid] [char](8) NOT NULL, ";
            //                strsql += " [mkrdt] [char](8) NOT NULL, ";
            //                strsql += " [mkrtm] [char](8) NOT NULL )";
            //                objUtility.ExecuteSQL(strsql);
            //            }

            //            strsql = "Insert into sms_Logs values(";
            //            strsql += "'" + clientCd.ToUpper() + "','" + strMobileNo + "','" + strMsgTxt + "',";
            //            strsql += "'" + strresponse.Replace("'", " ") + "','" + clientCd.ToUpper() + "',Convert(char,getdate(),112),Convert(char,getdate(),108))";
            //            objUtility.ExecuteSQL(strsql);
            //        }
            //        else
            //        {
            //            dtTable = objUtility.OpenDataTable("Select 'Client Not Found' Response");
            //            return dtTable;
            //        }
            //        if (strresponse.IndexOf("Successfully") != -1)
            //        {
            //            strresponse = "Password sent to your registered mobile " + strMobile;
            //        }
            //        else
            //        {
            //            strresponse = "Error Sending SMS " + strresponse;
            //        }
            //        dtTable = objUtility.OpenDataTable("Select '" + strresponse + "' Response");
            //        return dtTable;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    throw ex;
            //}
            #endregion

            string strMobileVer = "";
            if (!string.IsNullOrWhiteSpace(clientCd) && clientCd.Contains("~"))
            {
                string[] parts = clientCd.Split('~');
                clientCd = parts[0];
                strMobileVer = parts[1];
            }

            if (strMobileVer != "2.0.0.1")
            {
                return objUtility.OpenDataTable("Select 'You are currently using an older version. Please update to the latest version.' Response, '' Token");
            }

            /*strsql = "select count(0) from OTP_Master with (NoLock) where OTP_ClientCode='" + clientCd + "' and OTP_Product='TradeMobile' and OTP_Type = 'FP' and CAST(OTP_SentDate AS DATETIME) + CAST(OTP_SentTime AS DATETIME) >= DATEADD(MINUTE, -15, GETDATE())";
            DataTable dtCheck = objUtility.OpenDataTable(strsql);
            if (dtCheck.Rows.Count > 0)
            {
                if (Convert.ToInt32(dtCheck.Rows[0][0]) >= 10)
                {
                    return objUtility.OpenDataTable("Select 'Too many Attempts. Try after some time.' Response, '' Token");
                }
            }*/

            DataTable dtResponse = objUtility.TradeMobileSendOTP(clientCd, "TradeMobile", "FP", "");
            string strStatus = dtResponse.Rows[0]["Status"].ToString();
            string strMessage = dtResponse.Rows[0]["Message"].ToString();
            //string strMobile = objUtility.fnFireQuery("Client_master", "cm_mobile", "cm_cd", clientCd.Trim(), true).Trim();
            string strFPToken = "";

            if (strStatus == "Y")
            {
                string decryptJwtKey = objUtility.Decrypt(_configuration["Jwt:Key"].ToString());
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(decryptJwtKey));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
                int tokenExpTime = 5;
                var claims = new[] {
                            new Claim(JwtRegisteredClaimNames.Sub, clientCd.ToUpper()),
                            new Claim(JwtRegisteredClaimNames.Jti, clientCd.ToUpper()),
                            new Claim(type: "username", value: clientCd.ToUpper()),
                            new Claim(type: "product", value: "TradeMobile"),
                            new Claim(ClaimTypes.Role, "TradeMobileFP"),
                        };
                var token = new JwtSecurityToken(_configuration["Jwt:Issuer"],
                    _configuration["Jwt:Issuer"],
                    claims,
                    expires: DateTime.UtcNow.AddMinutes(tokenExpTime),
                    signingCredentials: credentials);

                strFPToken = new JwtSecurityTokenHandler().WriteToken(token);

            }
            else
            {
                strMessage = "Error Sending SMS " + strMessage;
            }

            DataTable dtTable = objUtility.OpenDataTable("Select '" + strMessage + "' Response, '" + strFPToken + "' Token");
            return dtTable;
        }

        public dynamic ChangePassword(string clientCd, string OldPwd, string NewPwd)
        {
            try
            {
                string strsql = "", strLastLoginDt = "", strresponse = "";
                double intMinChar = Conversion.Val(objUtility.GetSysParmSt("PWDMINCHR", ""));
                string strAlphaNum = objUtility.GetSysParmSt("PWDALPHANUM", "");
                double intLastPass = Conversion.Val(objUtility.GetWebParameter("PWDSAMECHK"));
                DataTable dtReturn = new DataTable();
                dtReturn.Columns.Add("Response", typeof(string));
                if (string.IsNullOrEmpty(OldPwd))
                {
                    dtReturn.Rows.Add("Old password should not be blank.");
                    return dtReturn;
                }
                if (string.IsNullOrEmpty(NewPwd))
                {
                    dtReturn.Rows.Add("New password should not be blank.");
                    return dtReturn;
                }
                if (intMinChar > 0)
                {
                    if (NewPwd.Length < intMinChar)
                    {
                        dtReturn.Rows.Add("Password should contain minimum " + intMinChar + " characters.");
                        return dtReturn;
                    }
                }
                if (strAlphaNum.Trim() == "Y")
                {
                    int intPwdAlphaCnt = 0;
                    int intPwdNumCnt = 0;
                    int intPwdSplCnt = 0;

                    for (int i = 0; i <= NewPwd.Length - 1; i++)
                    {
                        if (Strings.Asc(NewPwd.Substring(i, 1)) >= 65 && Strings.Asc(NewPwd.Substring(i, 1)) <= 90)
                        {
                            intPwdAlphaCnt++;
                        }
                        else if (Strings.Asc(NewPwd.Substring(i, 1)) >= 97 && Strings.Asc(NewPwd.Substring(i, 1)) <= 122)
                        {
                            intPwdAlphaCnt++;
                        }
                        else if (Strings.Asc(NewPwd.Substring(i, 1)) >= 48 && Strings.Asc(NewPwd.Substring(i, 1)) <= 57)
                        {
                            intPwdNumCnt++;
                        }
                        else
                        {
                            intPwdSplCnt++;
                        }
                    }
                    if (intPwdNumCnt == 0)
                    {
                        dtReturn.Rows.Add("Password should contain at least one number.");
                        return dtReturn;
                    }
                    if (intPwdAlphaCnt == 0)
                    {
                        dtReturn.Rows.Add("Password should contain at least one Alphabet.");
                        return dtReturn;
                    }
                    if (intPwdSplCnt == 0)
                    {
                        dtReturn.Rows.Add("Password should contain at least one special character.");
                        return dtReturn;
                    }
                }
                DataTable dtTable;
                strsql = "select cm_cd,cm_pwd from client_master where cm_cd='" + clientCd.Trim() + "'";
                dtTable = objUtility.OpenDataTable(strsql);
                if (dtTable.Rows.Count > 0)
                {
                    strLastLoginDt = objUtility.fnFireQueryTradeWeb("Client_master", "cm_lastlogindt", "cm_cd", clientCd, true);
                    if (objUtility.GetWebParameter("TWEBENCPWD") == "Y")
                    {
                        OldPwd = objUtility.Encrypt(OldPwd);
                        NewPwd = objUtility.Encrypt(NewPwd);
                    }
                    if (dtTable.Rows[0]["cm_pwd"].ToString().Trim() == OldPwd.Trim())
                    {
                        if (intLastPass > 0)
                        {
                            if (InvalidPassword(intLastPass, "", clientCd, NewPwd))
                            {
                                dtReturn.Rows.Add("You cannot use any of the previous " + intLastPass + " passwords. Please set a different password.!");
                                return dtReturn;
                            }
                        }
                        strsql = "update Client_master set cm_pwd='" + NewPwd.Trim() + "' where cm_cd='" + clientCd.Trim() + "'";
                        strsql += " and cm_pwd='" + OldPwd.Trim() + "'";
                        objUtility.ExecuteSQL(strsql);
                        if (intLastPass > 0)
                        {
                            strsql = "Insert into Password_history(ph_dpid, ph_code, ph_logintype, ph_password, mkrdt) values('', '" + clientCd + "', 'C', '" + NewPwd + "', '" + DateTime.Now.ToString("yyyyMMdd") + "')";
                            objUtility.ExecuteSQL(strsql);
                        }
                        dtReturn.Rows.Add("Password changed successfully");
                    }
                    else
                    {
                        dtReturn.Rows.Add("Invalid Old Password");
                    }
                }
                return dtReturn;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool InvalidPassword(double intLastPass, string strDPID, string strCode, string strNewPassword)
        {
            bool blnPwd = false;
            string Strsql = "select top " + intLastPass + " ph_dpid, ph_code, ph_password from Password_history where ph_logintype='C' and ph_dpid = '" + strDPID + "' and ph_code='" + strCode + "' order by ph_srno desc";
            DataTable dtPass = objUtility.OpenDataTable(Strsql);
            foreach (DataRow dr in dtPass.Rows)
            {
                if (dr["ph_password"].ToString().Trim() == strNewPassword)
                {
                    blnPwd = true;
                    break;
                }
            }
            return blnPwd;
        }

        public dynamic PostFundRequest(string clientCd, string flag, string Type, string Value)
        {
            try
            {
                string strsql = "";
                string StrDT = "";
                DataTable dtTable;
                bool blnIdentity = false;
                long intcnt = 0L;
                string StrDTTm = "";

                StrDTTm = GetWebParameter("FP_CL_RQ_TM");

                if (!string.IsNullOrEmpty(StrDTTm.Trim()))
                {
                    string[] arrStrDtTm;
                    var Separators = new char[1];
                    Separators[0] = ',';
                    arrStrDtTm = StrDTTm.Split(Separators);
                    string strTime = "";
                    var Dstime = new DataTable();
                    Dstime = objUtility.OpenDataTable("select substring(convert(char,convert(datetime,GETDATE()),108),1,5) as Times");
                    if (Dstime.Rows.Count > 0)
                    {
                        strTime = Dstime.Rows[0]["Times"].ToString().Trim();
                    }

                    string strT1 = arrStrDtTm[0].ToString().Replace(":", "").Replace(".", "");
                    string strT2 = arrStrDtTm[1].ToString().Replace(":", "").Replace(".", "");
                    strTime = strTime.Replace(":", "");

                    if (Conversion.Val(strT1) > Conversion.Val(strTime) | Conversion.Val(strT2) < Conversion.Val(strTime))
                    {
                        dtTable = objUtility.OpenDataTable("Select 'Payout Request can be made between " + arrStrDtTm[0] + " and " + arrStrDtTm[1] + " hours' ErrorMG");
                        return dtTable;
                    }
                }

                if (Convert.ToInt64(objUtility.OpenDataTable("SELECT isnull (IDENT_CURRENT('FundsRequest'),0)").Rows[0][0]) > 0L)
                {
                    blnIdentity = true;
                    DataTable dtReqId;
                    dtReqId = objUtility.OpenDataTable("SELECT IDENT_CURRENT('FundsRequest')");
                    intcnt = Convert.ToInt64(dtReqId.Rows[0][0]);
                }
                else
                {
                    blnIdentity = false;
                    DataTable dtmax;
                    dtmax = objUtility.OpenDataTable("SELECT isNull(Max(Rq_srNo),0) from FundsRequest");
                    intcnt = Convert.ToInt64(dtmax.Rows[0][0]) + 1L;
                }
                if (flag.ToUpper() == "V")
                {
                    dtTable = objUtility.OpenDataTable("select Rq_Type,cast(round(Rq_Amount,2) as decimal(15,2)) Rq_Amount from FundsRequest with (nolock) where Rq_Clientcd='" + clientCd + "' and Rq_Satus1='P'");
                    if (dtTable.Rows.Count > 0)
                    {
                        dtTable = objUtility.OpenDataTable("Select 'Y," + dtTable.Rows[0]["Rq_Type"] + "," + dtTable.Rows[0]["Rq_Amount"] + "' ErrorMG");
                    }
                    else
                    {
                        dtTable = objUtility.OpenDataTable("Select 'N' ErrorMG");
                    }
                }
                else if (Strings.UCase(flag) == "D")
                {
                    // Delete Request'
                    strsql = "Delete From FundsRequest where Rq_Clientcd='" + clientCd + "' and Rq_Satus1='P'";
                    objUtility.ExecuteSQL(strsql);
                    dtTable = objUtility.OpenDataTable("Select 'Y' ErrorMG");
                }
                else if (Strings.UCase(flag) == "P")
                {
                    strsql = "Delete From FundsRequest where Rq_Clientcd='" + clientCd + "' and Rq_Satus1='P'";
                    objUtility.ExecuteSQL(strsql);

                    StrDT = objUtility.OpenDataTable("select Convert(char(8),getdate(),112)").Rows[0][0].ToString();
                    strsql = "insert into FundsRequest values ( ";
                    if (blnIdentity == false)
                    {
                        strsql += intcnt + ",";
                    }
                    strsql += " '" + clientCd + "','" + Type + "','" + Conversion.Val(Strings.Replace(Value, ",", "")) + "','Mobile',";
                    strsql += " '" + StrDT + "',";
                    strsql += " convert(char(8),getdate(),108),";
                    strsql += " 'P','P','P','" + objUtility.Encrypt(StrDT) + "','')";
                    objUtility.ExecuteSQL(strsql);
                    // StrRefID = objDataAccessLayer.OpenDataTable("SELECT IDENT_CURRENT('FundsRequest')").Rows(0).Item(0)
                    dtTable = objUtility.OpenDataTable("Select 'Y' ErrorMG");
                }
                else
                {
                    dtTable = objUtility.OpenDataTable("Select 'Invalid Parameters' ErrorMG");
                }
                return dtTable;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic PostShareRequest(string clientCd, string Flag, string ScripCd, string Qty)
        {
            try
            {
                string strsql = "";
                DataTable dtTable;
                SqlTransaction objTrans;
                string StrDT = "";
                if (Strings.UCase(Flag) == "V")
                {
                    if (GetWebParameter("IsTradeWeb", false) == "O")
                    {
                        string connetionString = objUtility.GetConnectionStr();
                        using (SqlConnection objConnection = new SqlConnection(connetionString))
                        {
                            objConnection.Open();
                            objTrans = objConnection.BeginTransaction();
                            SqlCommand cmd = objConnection.CreateCommand();
                            cmd.Transaction = objTrans;
                            SqlDataAdapter sqlDtAdap = new SqlDataAdapter();
                            SqlCommandBuilder sqlCmdBld = new SqlCommandBuilder(sqlDtAdap);

                            string strcollat = PrIProcessBenHolding(objConnection, objTrans, clientCd);
                            strsql = FnGetBenHolding(objConnection, objTrans, strcollat, clientCd, "SP"); // Get Sql
                            dtTable = OpenDataTableTemp(strsql, objConnection, objTrans);
                        }
                    }

                    else
                    {
                        strsql = "select bh_scripcd,bh_isin,bh_Scripname,(bh_qty*-1)bh_qty, cast((bh_valuation*-1 )as decimal(15,0))bh_valuation, ";
                        strsql += " isNull((select sum(Rq_Qty) From SharesRequest Where Rq_Clientcd = bh_clientcd and Rq_Scripcd=bh_scripcd and Rq_Satus1 = 'P'),0) ";
                        strsql += " ReqQty from benholding where bh_clientcd='" + clientCd + "' and bh_Type not in ('UNDEL') order by  bh_Scripname ";
                        dtTable = objUtility.OpenDataTable(strsql);
                    }
                }
                else if (Strings.UCase(Flag) == "D")
                {
                    // Delete Request'
                    strsql = "Delete From SharesRequest where Rq_Clientcd='" + clientCd + "' and Rq_Satus1='P'";
                    objUtility.ExecuteSQL(strsql);
                    dtTable = objUtility.OpenDataTable("Select 'Y' ErrorMG");
                }
                else if (Strings.UCase(Flag) == "P")
                {
                    StrDT = objUtility.OpenDataTable("select Convert(char(8),getdate(),112)").Rows[0][0].ToString();
                    strsql = "insert into SharesRequest values ( ";
                    strsql += " '" + clientCd + "','" + ScripCd + "','" + Qty + "', 'Mobile',  '" + StrDT + "',";
                    strsql += " convert(char(8),getdate(),108),";
                    strsql += " 'P','P','P','" + objUtility.Encrypt(StrDT) + "','')";
                    objUtility.ExecuteSQL(strsql);
                    // StrRefID = OpenDataTable("SELECT IDENT_CURRENT('ShareRequest')").Rows(0).Item(0)
                    dtTable = objUtility.OpenDataTable("Select 'Y' ErrorMG");
                }
                else
                {
                    dtTable = objUtility.OpenDataTable("Select 'Invalid Parameters' ErrorMG");
                }
                return dtTable;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic PostRequestForreport(string clientCd, string Report, string FromDt, string ToDt, string LastSeg)
        {
            try
            {
                string strsql = "";
                DataTable dtTable;
                string StrDT;
                int intIDEN;
                StrDT = objUtility.OpenDataTable("select Convert(char(8),getdate(),112)").Rows[0][0].ToString();
                intIDEN = Convert.ToInt32(objUtility.OpenDataTable("select columnproperty(object_id('Requests'),'Rq_SrNo','IsIdentity')").Rows[0][0]);

                if (Report == "DP Holding" | Report == "Retained Holding")
                {
                    if (string.IsNullOrEmpty(ToDt.Trim()))
                    {
                        dtTable = objUtility.OpenDataTable("Select 'Improper Request' ErrorMG");
                        return dtTable;
                    }
                }
                else if (Report == "Profit & Loss")
                {
                    if (string.IsNullOrEmpty(LastSeg.Trim()) | string.IsNullOrEmpty(FromDt.Trim()))
                    {
                        dtTable = objUtility.OpenDataTable("Select 'Improper Request' ErrorMG");
                        return dtTable;
                    }
                }
                else if (string.IsNullOrEmpty(FromDt.Trim()))
                {
                    dtTable = objUtility.OpenDataTable("Select 'Improper Request' ErrorMG");
                    return dtTable;
                }

                strsql = "insert into Requests values ( ";
                if (intIDEN == 0)
                {
                    strsql += objUtility.OpenDataTable("select isnull(max(Rq_SrNo),0) +1 from Requests").Rows[0][0] + ",";
                }
                strsql += " '" + clientCd + "','" + Report + "','" + FromDt + "','" + ToDt + "','Mobile',";
                strsql += " '" + StrDT + "',";
                strsql += " convert(char(8),getdate(),108),";
                strsql += " 'P','P','P','" + objUtility.Encrypt(StrDT) + "','',";
                strsql += " '" + LastSeg + "')";
                objUtility.ExecuteSQL(strsql);

                dtTable = objUtility.OpenDataTable((intIDEN == 0) ? "select max(Rq_SrNo) ReqNo from Requests" : "SELECT IDENT_CURRENT('Requests') ReqNo");
                return dtTable;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic ForgotPasswordVerifyOTP(string clientCd, string OTP, string NewPassword, string ConfirmPassword)
        {
            try
            {
                string strsql = "";
                bool blnEncPwd = objUtility.GetWebParameter("TWEBENCPWD").Trim() == "Y";
                DataTable dtReturn = new DataTable();
                dtReturn.Columns.Add("Response", typeof(string));
                double intMinChar = Conversion.Val(objUtility.GetSysParmSt("PWDMINCHR", ""));
                string strAlphaNum = objUtility.GetSysParmSt("PWDALPHANUM", "");
                double intLastPass = Conversion.Val(objUtility.GetWebParameter("PWDSAMECHK"));

                if (string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(ConfirmPassword))
                {
                    dtReturn.Rows.Add("New password should not be blank.");
                    return dtReturn;
                }

                if (NewPassword != ConfirmPassword)
                {
                    dtReturn.Rows.Add("Your Passwords do not Match");
                    return dtReturn;
                }

                if (intMinChar > 0)
                {
                    if (NewPassword.Length < intMinChar)
                    {
                        dtReturn.Rows.Add("Password should contain minimum " + intMinChar + " characters.");
                        return dtReturn;
                    }
                }
                if (strAlphaNum.Trim() == "Y")
                {
                    int intPwdAlphaCnt = 0;
                    int intPwdNumCnt = 0;
                    int intPwdSplCnt = 0;

                    for (int i = 0; i <= NewPassword.Length - 1; i++)
                    {
                        if (Strings.Asc(NewPassword.Substring(i, 1)) >= 65 && Strings.Asc(NewPassword.Substring(i, 1)) <= 90)
                        {
                            intPwdAlphaCnt++;
                        }
                        else if (Strings.Asc(NewPassword.Substring(i, 1)) >= 97 && Strings.Asc(NewPassword.Substring(i, 1)) <= 122)
                        {
                            intPwdAlphaCnt++;
                        }
                        else if (Strings.Asc(NewPassword.Substring(i, 1)) >= 48 && Strings.Asc(NewPassword.Substring(i, 1)) <= 57)
                        {
                            intPwdNumCnt++;
                        }
                        else
                        {
                            intPwdSplCnt++;
                        }
                    }
                    if (intPwdNumCnt == 0)
                    {
                        dtReturn.Rows.Add("Password should contain at least one number.");
                        return dtReturn;
                    }
                    if (intPwdAlphaCnt == 0)
                    {
                        dtReturn.Rows.Add("Password should contain at least one Alphabet.");
                        return dtReturn;
                    }
                    if (intPwdSplCnt == 0)
                    {
                        dtReturn.Rows.Add("Password should contain at least one special character.");
                        return dtReturn;
                    }
                }

                if (intLastPass > 0)
                {
                    if (!objUtility.fnchkTable("Password_history"))
                    {
                        strsql = "CREATE TABLE Password_history(";
                        strsql += "[ph_srno] [numeric](18, 0) IDENTITY(1,1) NOT NULL,";
                        strsql += "[ph_dpid] [char](8) NOT NULL,";
                        strsql += "[ph_code] [char](8) NOT NULL,";
                        strsql += "[ph_logintype] [char](1) NOT NULL,";
                        strsql += "[ph_password] [char](10) NOT NULL,";
                        strsql += "[mkrdt] [char](8) NOT NULL";
                        strsql += ")";
                        objUtility.ExecuteSQL(strsql);
                    }

                    if (InvalidPassword(intLastPass, "", clientCd, NewPassword))
                    {
                        dtReturn.Rows.Add("You cannot use any of the previous " + intLastPass + " passwords. Please set a different password.!");
                        return dtReturn;
                    }
                }
                string checkOTP = "", strIdentity = "", strMobileNo = "";
                int maxOtpFailCount = 0;
                string Strsql = "select top 1 OTP_Identity,OTP_OTP,OTP_ValidTillDate,OTP_ValidTillTime, OTP_SentTo, OTP_NFiller1  from OTP_Master with (NoLock) where OTP_ClientCode='" + clientCd + "' and OTP_Status='P' and OTP_Product='TradeMobile' and OTP_Type = 'FP' order by OTP_Identity desc";
                DataTable dt = objUtility.OpenDataTable(Strsql);
                if (dt.Rows.Count > 0)
                {
                    checkOTP = dt.Rows[0]["OTP_OTP"].ToString().Trim();
                    strIdentity = dt.Rows[0]["OTP_Identity"].ToString().Trim();
                    strMobileNo = dt.Rows[0]["OTP_SentTo"].ToString().Trim();
                    maxOtpFailCount = Convert.ToInt32(dt.Rows[0]["OTP_NFiller1"].ToString());
                }

                if (maxOtpFailCount >= 3)
                {
                    dtReturn.Rows.Add("Maximum OTP attempts reached. Please generate a new OTP.");
                }
                else if (!string.IsNullOrWhiteSpace(OTP) && !string.IsNullOrWhiteSpace(checkOTP) && OTP == checkOTP)
                {
                    DateTime dtNow = objUtility.GetSqlCurrentDateTime();
                    DateTime dt3 = objUtility.stod(dt.Rows[0]["OTP_ValidTillDate"].ToString());
                    TimeSpan time = TimeSpan.Parse(dt.Rows[0]["OTP_ValidTillTime"].ToString());
                    DateTime dtExpiry = dt3 + time;
                    if (dtNow > dtExpiry)
                    {
                        Strsql = "update OTP_Master set OTP_Status='E' where OTP_Identity='" + strIdentity + "'";
                        objUtility.ExecuteSQL(Strsql);
                        dtReturn.Rows.Add("OTP has been expired");
                        return dtReturn;
                    }
                    Strsql = "update OTP_Master set OTP_Status='M' where OTP_Identity='" + strIdentity + "'";
                    objUtility.ExecuteSQL(Strsql);

                    strsql = " update Client_master set cm_pwd='" + (blnEncPwd ? objUtility.Encrypt(NewPassword) : NewPassword) + "' where cm_cd='" + clientCd + "'";
                    objUtility.ExecuteSQL(strsql);

                    if (intLastPass > 0)
                    {
                        strsql = "Insert into Password_history(ph_dpid, ph_code, ph_logintype, ph_password, mkrdt) values('', '" + clientCd + "', 'C', '" + NewPassword + "', '" + DateTime.Now.ToString("yyyyMMdd") + "')";
                        objUtility.ExecuteSQL(strsql);
                    }
                    dtReturn.Rows.Add("Password has been changed successfully!");
                }
                else
                {
                    Strsql = "update OTP_Master set OTP_NFiller1=" + (maxOtpFailCount + 1) + " where OTP_Identity='" + strIdentity + "'";
                    objUtility.ExecuteSQL(Strsql);
                    dtReturn.Rows.Add("OTP Mismatched");
                }
                return dtReturn;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region Common Functions

        public string GetSysParameter(string strParmcd)
        {
            var dtTable = new DataTable();
            dtTable = objUtility.OpenDataTable("select sp_sysvalue from Sysparameter with (nolock) where sp_parmcd='" + strParmcd + "'");
            if (dtTable.Rows.Count > 0)
            {
                return dtTable.Rows[0][0].ToString();
            }
            else
            {
                return "";
            }
        }
        public void fnGetGenerateCash(SqlConnection objConnection, SqlTransaction objTrans, string struserid, string strFromDt, string strToDt, string strReportType, string strScripCode, string strStockValuation)
        {
            string strsql = "";
            DataTable rstemp;
            string strDelSide;
            int lngLoop;
            DataTable rsSquare;
            int lngDelQty;
            int lngBalqty;
            int lngCurSerial;
            string strClient = "";
            string strScrip;
            string strStlmnt;

            try
            {
                ExecuteSQLTemp("drop table #VX", objConnection, objTrans);
            }
            catch (Exception ex)
            {
            }
            finally
            {
                strsql = " CREATE TABLE [#VX] (";
                strsql += " [td_companycode] [char] (1) NOT NULL ,";
                strsql += " [td_stlmnt] [char] (9) NOT NULL ,";
                strsql += " [td_clientcd] [char] (8) NOT NULL ,";
                strsql += " [td_scripcd] [char] (6) NOT NULL ,";
                strsql += " [td_dt] [char] (8) NOT NULL ,";
                strsql += " [td_srno] [numeric](18, 0) IDENTITY(1,1) NOT NULL ,";
                strsql += " [td_bsflag] [char] (1) NOT NULL ,";
                strsql += " [td_bqty] [numeric](18, 0) NOT NULL ,";
                strsql += " [td_sqty] [numeric](18, 0) NOT NULL ,";
                strsql += " [td_rate] [money] NOT NULL ,";
                strsql += " [td_marketrate] [money] NOT NULL ,";
                strsql += " [td_flag] [VarChar](1) Not null ";
                strsql += " PRIMARY KEY CLUSTERED (td_srno))";
                ExecuteSQLTemp(strsql, objConnection, objTrans);
            }

            try
            {
                ExecuteSQLTemp("drop table #invcharges", objConnection, objTrans);
            }
            catch (Exception ex)
            {
            }
            finally
            {

                strsql = " CREATE TABLE [#invcharges] (";
                strsql += " [ic_stlmnt] [char] (9) NOT NULL ,";
                strsql += " [ic_clientcd] [char] (8) NOT NULL ,";
                strsql += " [ic_desc] [char] (20) NOT NULL ,";
                strsql += " [ic_amount] money";
                strsql += " ) ON [PRIMARY]";
                ExecuteSQLTemp(strsql, objConnection, objTrans);
            }

            strsql = "insert into #invcharges";
            strsql += " select sh_stlmnt,sh_clientcd,left(sh_desc,12),sh_amount ";
            strsql += " from Specialcharges,Settlements,client_master ";
            strsql += " Where sh_clientcd = cm_cd and sh_stlmnt = se_stlmnt and se_stdt between '" + strFromDt + "' and '" + strToDt + "'";
            strsql += " and sh_clientcd = '" + struserid + "'";
            strsql += " and not exists (select sy_exchange+sy_type from Settlement_type Where se_exchange = sy_exchange and se_type = sy_type and sy_maptype in ('S','W','V') ) ";
            ExecuteSQLTemp(strsql, objConnection, objTrans);

            strsql = "insert into #invcharges";
            strsql += " select sh_stlmnt,sh_clientcd,left('Service Tax',12),sh_servicetax ";
            strsql += " from Specialcharges,Settlements,client_master  ";
            strsql += " where sh_clientcd = cm_cd and sh_stlmnt = se_stlmnt";
            strsql += " and sh_servicetaxyn = 'Y' and sh_servicetax > 0 and se_stdt between '" + strFromDt + "' and '" + strToDt + "'";
            strsql += " and sh_clientcd = '" + struserid + "'";
            strsql += " and se_exchange+se_type not in (select sy_exchange+sy_type from Settlement_type Where sy_maptype in ('S','W','V')) ";
            ExecuteSQLTemp(strsql, objConnection, objTrans);

            strsql = "Insert into  #invcharges";
            strsql += " select bc_stlmnt,bc_clientcd,left(cg_desc,12),";
            strsql += " bc_amount from Cbilled_charges,Charges_master,Settlements,client_master";
            strsql += " Where bc_clientcd = cm_cd and bc_companycode = cg_companycode And Left(bc_stlmnt, 1) = cg_exchange";
            strsql += " and bc_chargecode = cg_cd and bc_stlmnt = se_stlmnt and se_stdt between '" + strFromDt + "' and '" + strToDt + "'";
            strsql += " and bc_clientcd = '" + struserid + "'";
            strsql += " and se_exchange+se_type not in (select sy_exchange+sy_type from Settlement_type Where sy_maptype in ('S','W','V')) ";
            ExecuteSQLTemp(strsql, objConnection, objTrans);

            strsql = "insert into #VX SELECT ";
            strsql += " td_companycode ,td_stlmnt ,td_clientcd ,td_scripcd,";
            strsql += " td_dt , td_bsflag , sum(td_bqty) ,sum(td_sqty) ,sum(td_rate*(td_bqty+td_sqty)), sum(td_marketrate*(td_bqty+td_sqty)),'Y' td_flag ";
            strsql += " FROM Trx With (index(idx_trx_dt_clientcd)),client_master where td_clientcd = cm_cd and td_Dt between '" + strFromDt + "' and '" + strToDt + "'";
            strsql += " and td_cfflag = 'N' and td_clientcd = '" + struserid + "'" + ((strScripCode.Trim() != "") ? " and td_Scripcd = '" + strScripCode + "'" : "");
            strsql += " and td_stlmnt not in (select se_stlmnt from settlements Where se_exchange+se_type in (select sy_exchange+sy_type from Settlement_type Where sy_maptype in ('S','W','V'))) ";
            strsql += " group by td_companycode ,td_stlmnt ,td_clientcd ,td_scripcd, td_dt , td_bsflag ";
            ExecuteSQLTemp(strsql, objConnection, objTrans);

            string strCompanyCode = Strings.Trim(objUtility.fnFireQuery("Entity_master", "em_cd", "em_cd=(select min(em_cd) from Entity_master Where Len(Ltrim(Rtrim(em_cd))) = 1) and 1", "1"));
            strsql = " select * from ClearingHouse Where CH_CompanyCode = '" + strCompanyCode + "' and CH_Segment = 'C' " + " and  CH_EffDt = (select Min(CH_EffDt) from ClearingHouse " + " Where CH_CompanyCode = '" + strCompanyCode + "' and CH_Segment = 'C' and CH_EffDt <= '" + strToDt + "') ";

            DataTable dtTemp = OpenDataTableTemp(strsql, objConnection, objTrans);
            if (dtTemp.Rows.Count > 0)
            {
                strsql = " Update X set td_stlmnt = b.se_stlmnt " + " from #VX X, Settlements a, Settlements b " + " Where td_dt >= '" + dtTemp.Rows[0]["CH_EffDt"] + "' and left(td_stlmnt,2) in ('BW','NN') and td_stlmnt = a.se_stlmnt " + " and a.se_stdt = b.se_stdt and left(a.se_stlmnt,2) = '" + ((dtTemp.Rows[0]["CH_ClgHs"] == "N") ? "BW" : "NN") + "' and left(b.se_stlmnt,2) = '" + ((dtTemp.Rows[0]["CH_ClgHs"] == "N") ? "NN" : "BW") + "'";


                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = " Update X set td_stlmnt = b.se_stlmnt " + " from #VX X, Settlements a, Settlements b " + " Where td_dt >= '" + dtTemp.Rows[0]["CH_EffDt"] + "' and left(td_stlmnt,2) in ('BC','NZ') and td_stlmnt = a.se_stlmnt " + " and a.se_stdt = b.se_stdt and left(a.se_stlmnt,2) = '" + ((dtTemp.Rows[0]["CH_ClgHs"] == "N") ? "BC" : "NZ") + "' and left(b.se_stlmnt,2) = '" + ((dtTemp.Rows[0]["CH_ClgHs"] == "N") ? "NZ" : "BC") + "'";


                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = " Update X set td_stlmnt = b.se_stlmnt " + " from #VX X, Settlements a, Settlements b " + " Where td_dt >= '" + dtTemp.Rows[0]["CH_EffDt"] + "' and left(td_stlmnt,2) in ('BR','NA') and td_stlmnt = a.se_stlmnt " + " and a.se_stdt = b.se_stdt and left(a.se_stlmnt,2) = '" + ((dtTemp.Rows[0]["CH_ClgHs"] == "N") ? "BR" : "NA") + "' and left(b.se_stlmnt,2) = '" + ((dtTemp.Rows[0]["CH_ClgHs"] == "N") ? "NA" : "BR") + "'";


                ExecuteSQLTemp(strsql, objConnection, objTrans);
            }


            strsql = "update #VX set td_rate = Case when (td_bqty+td_sqty) > 0 then td_rate/(td_bqty+td_sqty) else 0 end , td_marketrate= Case When (td_bqty+td_sqty) > 0 then td_marketrate/(td_bqty+td_sqty) else 0 end ";
            ExecuteSQLTemp(strsql, objConnection, objTrans);

            strsql = "update a set a.td_flag = 'N' from #VX a where a.td_clientcd + a.td_scripcd + a.td_stlmnt";
            strsql += " in(select b.td_clientcd + b.td_scripcd + b.td_stlmnt from #VX b where a.td_clientcd = b.td_clientcd";
            strsql += " and a.td_scripcd = b.td_scripcd and a.td_stlmnt = b.td_stlmnt";
            strsql += " group by td_clientcd,td_scripcd,td_stlmnt having sum(td_bqty) = 0 or sum(td_sqty) = 0)";
            ExecuteSQLTemp(strsql, objConnection, objTrans);

            ExecuteSQLTemp("update #VX set td_flag = 'N' from Settlements,SEttlement_type Where td_stlmnt = se_stlmnt and se_type = sy_type and se_exchange = sy_exchange and sy_maptype = 'C' ", objConnection, objTrans);

            try
            {
                strsql = "Drop Table #TmpRates ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);
            }
            catch (Exception ex)
            {
            }
            finally
            {
                strsql = "Create Table #TmpRates (Tmp_Scripcd Varchar(6),Tmp_Rate money)";
                ExecuteSQLTemp(strsql, objConnection, objTrans);
            }

            strsql = "Insert into #TmpRates select distinct td_scripcd ,0 from #VX ";
            ExecuteSQLTemp(strsql, objConnection, objTrans);

            strsql = "Update #TmpRates set Tmp_Rate = mk_closerate from Market_Rates Where mk_exchange = 'B' and Tmp_scripcd = mk_Scripcd " + " and mk_dt = (select max(mk_dt) from Market_Rates Where mk_Scripcd = Tmp_Scripcd and mk_dt<='" + strToDt + "') ";
            ExecuteSQLTemp(strsql, objConnection, objTrans);

            strsql = "Update #TmpRates set Tmp_Rate = mk_closerate from Market_Rates Where mk_exchange = 'N' and Tmp_scripcd = mk_Scripcd " + " and mk_dt = (select max(mk_dt) from Market_Rates Where mk_Scripcd = Tmp_Scripcd and mk_dt<='" + strToDt + "') ";
            ExecuteSQLTemp(strsql, objConnection, objTrans);

            strsql = "Update #VX set td_MarketRate = Tmp_Rate from #TmpRates Where Tmp_Scripcd = td_scripcd ";
            ExecuteSQLTemp(strsql, objConnection, objTrans);

            int iR = 0;
            int iSQ = 0;

            strsql = "SELECT ";
            strsql += " td_stlmnt,td_clientcd , td_scripcd,cm_name,ss_name,";
            strsql += " sum(td_bqty) td_bqty ,sum(td_sqty) td_sqty, sum(td_bqty-td_sqty) net FROM #VX,Client_master,Securities ";
            strsql += " where td_clientcd = cm_cd and td_scripcd = ss_cd and td_flag = 'Y' group by  ";
            strsql += " td_stlmnt,td_clientcd,cm_name,td_scripcd,ss_name ";
            strsql += " having sum(td_bqty - td_sqty) <> 0 ORDER BY td_clientcd , td_scripcd, td_stlmnt ";
            rstemp = OpenDataTableTemp(strsql, objConnection, objTrans);
            if (rstemp.Rows.Count > 0)
            {

                iR = 0;
                iSQ = 0;
                while (iR < rstemp.Rows.Count)
                {

                    strClient = rstemp.Rows[iR]["td_Clientcd"].ToString().Trim();
                    strScrip = rstemp.Rows[iR]["td_scripcd"].ToString().Trim();
                    strStlmnt = rstemp.Rows[iR]["td_stlmnt"].ToString().Trim();
                    if (Convert.ToDouble(rstemp.Rows[iR]["Net"]) > 0d)
                    {
                        strDelSide = "B";
                        lngDelQty = Math.Abs(Convert.ToInt32(rstemp.Rows[iR]["Net"]));
                    }
                    else
                    {
                        strDelSide = "S";
                        lngDelQty = Math.Abs(Convert.ToInt32(rstemp.Rows[iR]["Net"]));
                    }
                    lngLoop = 0;
                    strsql = "select * from #VX where td_clientcd = '" + strClient + "' and td_scripcd = '" + strScrip + "'";
                    strsql += " and td_stlmnt = '" + strStlmnt + "'";
                    strsql += " and td_bsflag = '" + strDelSide + "' order by td_dt desc,td_stlmnt desc";
                    rsSquare = OpenDataTableTemp(strsql, objConnection, objTrans);
                    iSQ = 0;
                    while (iSQ < rsSquare.Rows.Count - 1 | lngDelQty > 0)
                    {
                        while (lngDelQty > 0)
                        {
                            lngLoop += 1;
                            lngCurSerial = Convert.ToInt32(rsSquare.Rows[iSQ]["td_SrNo"]);
                            {
                                var withBlock = rsSquare.Rows[iSQ];
                                if (Convert.ToInt32(withBlock["td_bqty"]) + Convert.ToInt32(withBlock["td_Sqty"]) > lngDelQty)
                                {
                                    lngBalqty = Convert.ToInt32(withBlock["td_bqty"]) + Convert.ToInt32(withBlock["td_Sqty"]) - lngDelQty;

                                    strsql = " insert into #VX select td_companycode ,td_stlmnt,td_clientcd , td_scripcd, td_dt, td_bsflag,";
                                    if (strDelSide == "B")
                                    {
                                        strsql += lngBalqty + ", td_sqty";
                                    }
                                    else
                                    {
                                        strsql += " td_bqty ," + lngBalqty;
                                    }
                                    strsql += ", td_rate, td_marketrate,'Y' from #VX where td_srno =" + lngCurSerial;
                                    ExecuteSQLTemp(strsql, objConnection, objTrans);

                                    strsql = "update #VX set td_flag = 'N' ";
                                    if (strDelSide == "B")
                                    {
                                        strsql += ",td_bqty = ";
                                    }
                                    else
                                    {
                                        strsql += ",td_sqty = ";
                                    }
                                    strsql += lngDelQty + " where td_srno = " + lngCurSerial;
                                    ExecuteSQLTemp(strsql, objConnection, objTrans);

                                    lngDelQty = 0;
                                }
                                else
                                {
                                    ExecuteSQLTemp("update #VX set td_flag = 'N' where td_srno = " + lngCurSerial, objConnection, objTrans);
                                    lngDelQty -= Convert.ToInt32(withBlock["td_bqty"]) + Convert.ToInt32(withBlock["td_Sqty"]);
                                }
                            }
                            iSQ += 1;
                            if (iSQ >= rsSquare.Rows.Count)
                                break;
                        }
                    }
                    iR += 1;
                }
            }
            if (Convert.ToDouble(strStockValuation) != 2d)
            {
                strsql = "SELECT ";
                strsql += " td_clientcd , td_scripcd,cm_name,ss_name,";
                strsql += " sum(td_bqty) td_bqty ,sum(td_sqty) td_sqty, sum(td_bqty-td_sqty) net FROM #VX,Client_master,Securities ";
                strsql += " where td_clientcd = cm_cd and td_scripcd = ss_cd and td_flag = 'N' group by  ";
                strsql += " td_clientcd,cm_name,td_scripcd,ss_name";
                strsql += " having sum(td_bqty - td_sqty) <> 0 ORDER BY td_clientcd , td_scripcd";
                rstemp = OpenDataTableTemp(strsql, objConnection, objTrans);
                if (rstemp.Rows.Count > 0)
                {

                    iR = 0;
                    iSQ = 0;
                    while (iR < rstemp.Rows.Count)
                    {

                        strClient = rstemp.Rows[iR]["td_Clientcd"].ToString().Trim();
                        strScrip = rstemp.Rows[iR]["td_scripcd"].ToString().Trim();
                        if (Convert.ToDouble(rstemp.Rows[iR]["Net"]) > 0d)
                        {
                            strDelSide = "B";
                            lngDelQty = Math.Abs(Convert.ToInt32(rstemp.Rows[iR]["Net"]));
                        }
                        else
                        {
                            strDelSide = "S";
                            lngDelQty = Math.Abs(Convert.ToInt32(rstemp.Rows[iR]["Net"]));
                        }
                        lngLoop = 0;
                        strsql = "select * from #VX where td_clientcd = '" + strClient + "' and td_scripcd = '" + strScrip + "'";
                        strsql += " and td_bsflag = '" + strDelSide + "' and td_flag = 'N' order by td_dt desc,td_stlmnt desc";
                        rsSquare = OpenDataTableTemp(strsql, objConnection, objTrans);
                        iSQ = 0;
                        while (iSQ < rsSquare.Rows.Count - 1 | lngDelQty > 0)
                        {
                            while (lngDelQty > 0)
                            {
                                lngLoop += 1;

                                lngCurSerial = Convert.ToInt32(rsSquare.Rows[iSQ]["td_SrNo"]);
                                {
                                    var withBlock1 = rsSquare.Rows[iSQ];
                                    if (Convert.ToInt32(withBlock1["td_bqty"]) + Convert.ToInt32(withBlock1["td_Sqty"]) > lngDelQty)
                                    {
                                        lngBalqty = Convert.ToInt32(withBlock1["td_bqty"]) + Convert.ToInt32(withBlock1["td_Sqty"]) - lngDelQty;

                                        strsql = " insert into #VX select td_companycode ,td_stlmnt,td_clientcd , td_scripcd, td_dt, td_bsflag,";
                                        if (strDelSide == "B")
                                        {
                                            strsql += lngBalqty + ", td_sqty";
                                        }
                                        else
                                        {
                                            strsql += " td_bqty ," + lngBalqty;
                                        }
                                        strsql += ", td_rate, td_marketrate,td_flag from #VX where td_srno =" + lngCurSerial;
                                        ExecuteSQLTemp(strsql, objConnection, objTrans);

                                        strsql = "update #VX set td_flag = 'X' ";
                                        if (strDelSide == "B")
                                        {
                                            strsql += ",td_bqty = ";
                                        }
                                        else
                                        {
                                            strsql += ",td_sqty = ";
                                        }
                                        strsql += lngDelQty + " where td_srno = " + lngCurSerial;
                                        ExecuteSQLTemp(strsql, objConnection, objTrans);

                                        lngDelQty = 0;
                                    }
                                    else
                                    {
                                        ExecuteSQLTemp("update #VX set td_flag = 'X' where td_srno = " + lngCurSerial, objConnection, objTrans);
                                        lngDelQty -= Convert.ToInt32(withBlock1["td_bqty"]) + Convert.ToInt32(withBlock1["td_Sqty"]);
                                    }
                                }
                                iSQ += 1;
                                if (iSQ >= rsSquare.Rows.Count)
                                    break;
                                if (lngDelQty <= 0)
                                    break;
                            }
                            if (lngDelQty <= 0)
                                break;
                        }
                        iR += 1;
                    }
                }
            }
        }
        public void prTempFOBill1(SqlConnection objConnection, SqlTransaction objTrans)
        {
            string Strsql = "";
            ExecuteSQLTemp("IF OBJECT_ID('tempdb..#tmpfobill') IS NOT NULL DROP TABLE #tmpfobill", objConnection, objTrans);

            Strsql = "Create table #tmpfobill(";
            Strsql += " [tx_controlflag] [numeric](18,3) NOT NULL,";
            Strsql += " [tx_dt] [char](8) NOT NULL,";
            Strsql += " [tx_clientcd] [char] (8) NOT NULL,";
            Strsql += " [tx_mainbrcd] [char] (8) NOT NULL,";
            Strsql += " [tx_seriesid] [numeric]  NOT NULL,";
            Strsql += " [tx_desc] char(45) NOT NULL,";
            Strsql += " [tx_bqty] numeric (18,3)  NOT NULL,";
            Strsql += " [tx_sqty] numeric(18,3)   NOT NULL,";
            Strsql += " [tx_rate] [money]  NOT NULL,";
            Strsql += " [tx_mainbrrate] [money]  NOT NULL,";
            Strsql += " [tx_marketrate] [money]  NOT NULL,";
            Strsql += " [tx_servicetax] [money]  NOT NULL,";
            Strsql += " [tx_closerate] [money]  NOT NULL,";
            Strsql += " [tx_sortlist] [numeric] NOT NULL,";
            Strsql += " [tx_prodtype] [char] (2) NOT NULL,";
            Strsql += " [tx_value] [money] NOT NULL,";
            Strsql += " [tx_multiplier] [money] NOT Null,";
            Strsql += " [tx_tradeid] [varchar](20) NOT NULL,";
            Strsql += " [tx_time] [char](8) NOT NULL,";
            Strsql += " [tx_orderid] [varchar](20) NOT NULL";
            Strsql += " )";
            ExecuteSQLTemp(Strsql, objConnection, objTrans);

            ExecuteSQLTemp("IF OBJECT_ID('tempdb..#tmpbillcharges') IS NOT NULL DROP TABLE #tmpbillcharges", objConnection, objTrans);

            Strsql = "CREATE TABLE [#tmpbillcharges] ([bc_dt] [char] (8) NOT NULL, ";
            Strsql += "[bc_clientcd] [char] (8) NOT NULL,[bc_desc] [char] (40) NOT NULL, ";
            Strsql += "[bc_amount] [money] NOT NULL,[bc_billno] [numeric] NOT NULL ) ";
            ExecuteSQLTemp(Strsql, objConnection, objTrans);

            Strsql = " Drop Table #tmpmosesdates";
            ExecuteSQLTemp(Strsql, objConnection, objTrans);

            Strsql = "CREATE TABLE [#tmpmosesdates] ([bd_dt] [char] (8) NOT NULL )";
            ExecuteSQLTemp(Strsql, objConnection, objTrans);
        }
        public void prTempFOBill(SqlConnection objConnection, SqlTransaction objTrans)
        {
            string Strsql = "";
            ExecuteSQLTemp("IF OBJECT_ID('tempdb..#tmpfobill') IS NOT NULL DROP TABLE #tmpfobill", objConnection, objTrans);

            Strsql = "CREATE TABLE  #tmpfobill ( ";
            Strsql += "[tx_controlflag] numeric(18,3) NOT NULL ,";
            Strsql += "[tx_dt] [char] (8) NOT NULL ,";
            Strsql += "[tx_clientcd] [char] (8) NOT NULL ,";
            Strsql += "[tx_mainbrcd] [char] (8) NOT NULL ,";
            Strsql += "[tx_seriesid] [numeric]  NOT NULL ,";
            Strsql += "[tx_desc] char(45) NOT NULL,";
            Strsql += "[tx_bqty] numeric (18,3)  NOT NULL ,";
            Strsql += "[tx_sqty] numeric(18,3)   NOT NULL ,";
            Strsql += "[tx_rate] [money]  NOT NULL ,";
            Strsql += "[tx_mainbrrate] [money]  NOT NULL ,";
            Strsql += "[tx_marketrate] [money]  NOT NULL ,";
            Strsql += "[tx_servicetax] [money]  NOT NULL ,";
            Strsql += "[tx_closerate] [money]  NOT NULL ,";
            Strsql += "[tx_sortlist] [numeric] NOT NULL,";
            Strsql += "[tx_prodtype] [char] (2) NOT NULL,";
            Strsql += "[tx_value] [money] NOT NULL, ";
            Strsql += "[tx_exchange] [char] (1) NOT NULL ";
            Strsql += " )";
            ExecuteSQLTemp(Strsql, objConnection, objTrans);

            ExecuteSQLTemp("IF OBJECT_ID('tempdb..#tmpbillcharges') IS NOT NULL DROP TABLE #tmpbillcharges", objConnection, objTrans);

            Strsql = "CREATE TABLE [#tmpbillcharges] (";
            Strsql += "[bc_dt] [char] (8) NOT NULL,";
            Strsql += "[bc_clientcd] [char] (8) NOT NULL,";
            Strsql += "[bc_desc] [char] (40) NOT NULL,";
            Strsql += "[bc_amount] [money] NOT NULL,";
            Strsql += "[bc_billno] [numeric] NOT NULL, ";
            Strsql += "[bc_exchange] [char] (1) NOT NULL ";
            Strsql += ")";
            ExecuteSQLTemp(Strsql, objConnection, objTrans);

            ExecuteSQLTemp("IF OBJECT_ID('tempdb..#tmpmosesdates') IS NOT NULL DROP TABLE #tmpmosesdates", objConnection, objTrans);

            Strsql = "CREATE TABLE [#tmpmosesdates] ([bd_dt] [char] (8) NOT NULL )";
            ExecuteSQLTemp(Strsql, objConnection, objTrans);
        }

        public string fnCheckInterOperability(string strDate, string strSegment)
        {
            string StrSql;

            if (Convert.ToInt16(objUtility.fnFireQuery("sysobjects", "count(0)", "name", "ClearingHouse", true)) == 0)
                return "";

            StrSql = " select ClearingHouse.* from ClearingHouse ,entity_master with (nolock)";
            StrSql += " Where CH_CompanyCode = em_cd and CH_Segment = '" + strSegment + "'";
            StrSql += " and CH_EffDt = (Select max(CH_EffDt) from ClearingHouse,entity_master with (nolock) ";
            StrSql += " Where CH_CompanyCode = em_cd and CH_Segment = '" + strSegment + "' and CH_EffDt <='" + strDate + "')";
            DataTable DsInterOP = objUtility.OpenDataTable(StrSql);
            if (DsInterOP.Rows.Count == 0)
                return "";
            else if (DsInterOP.Rows[0]["CH_ClgHs"].ToString().Trim() == "")
                return "";
            else
                return "TRUE";
        }

        public string fnGetInterOpExchange(string strSegment, string strCompCd)
        {
            DataTable DsInterOP;
            string strSQL;
            string strData;

            strSQL = " select substring(CES_Cd,2,1) as CES_Cd from CompanyExchangeSegments Where CES_CompanyCd = '" + strCompCd + "'  and substring(CES_Cd,2,1) in ('B','N') and Right(CES_Cd,1) = '" + strSegment + "'";
            DsInterOP = objUtility.OpenDataTable(strSQL);
            if ((DsInterOP.Rows.Count == 0))
                strData = "";
            else
            {
                strData = "";
                for (int j = 0; j <= DsInterOP.Rows.Count - 1; j++)
                    strData += DsInterOP.Rows[j]["CES_Cd"].ToString().Trim() + ",";
            }
            strData = Strings.Left(strData, Strings.Len(strData) - 1);
            return strData;
        }

        public string GetSysParmStComm(string strParmcd)
        {
            DataTable dtTable = new DataTable();
            string strCommex = "";
            strCommex = GetWebParameter("Commex");
            string[] ArrCommex = Strings.Split(strCommex, "/");
            dtTable = objUtility.OpenDataTable("select sp_sysvalue from [" + ArrCommex[0] + "]." + ArrCommex[1] + "." + ArrCommex[2] + ".Sysparameter with (nolock) where sp_parmcd='" + strParmcd + "'");
            if (dtTable.Rows.Count > 0)
                return dtTable.Rows[0][0].ToString();
            else
                return "";
        }

        public string fnForBill(SqlConnection objConnection, SqlTransaction objTrans, string strUserid, string strDate, string strExchange, string strSegment, string strCompCd)
        {
            string strsql = "";
            if (Strings.UCase(strSegment) == "X")
            {
                string strDelivery;
                string strSeries_master;
                string strClient_master;
                string strTrades;
                string strMarket_summary;
                string strFspecialcharges;
                string strproduct_master;
                string strFbills, strCommex = "";

                prTempFOBill1(objConnection, objTrans);

                strsql = "insert into #tmpmosesdates values('" + strDate + "') ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);
                if (strExchange.Trim() == "N")
                    strExchange = (Strings.Trim(GetSysParmStComm("CHGNCDEXCD")) == "Y") ? "F" : "N";
                strCommex = GetWebParameter("Commex");
                string[] ArrCommex = Strings.Split(strCommex, "/");
                strSeries_master = "[" + ArrCommex[0] + "]." + ArrCommex[1] + "." + ArrCommex[2] + ".Series_master";
                strClient_master = "[" + ArrCommex[0] + "]." + ArrCommex[1] + "." + ArrCommex[2] + ".Client_master";
                strTrades = "[" + ArrCommex[0] + "]." + ArrCommex[1] + "." + ArrCommex[2] + ".Trades";
                strDelivery = "[" + ArrCommex[0] + "]." + ArrCommex[1] + "." + ArrCommex[2] + ".Delivery";
                strMarket_summary = "[" + ArrCommex[0] + "]." + ArrCommex[1] + "." + ArrCommex[2] + ".Market_summary";
                strFspecialcharges = "[" + ArrCommex[0] + "]." + ArrCommex[1] + "." + ArrCommex[2] + ".Fspecialcharges";
                strproduct_master = "[" + ArrCommex[0] + "]." + ArrCommex[1] + "." + ArrCommex[2] + ".product_master";
                strFbills = "[" + ArrCommex[0] + "]." + ArrCommex[1] + "." + ArrCommex[2] + ".Fbills";

                strsql = "insert into #tmpfobill select 1 td_controlflag,bd_dt,td_clientcd,td_mainbrcd, td_seriesid,'',case sign(sum(td_bqty - td_sqty)) when 1 then abs(sum(td_bqty - td_sqty)) else 0 end  td_bqty, case sign(sum(td_bqty - td_sqty)) when 1 then 0 else abs(sum(td_bqty - td_sqty)) end td_sqty, 0.0000 td_rate,0.0000 td_mainbrrate,0.0000 td_mainbrrate, 0.0000 td_servicetax,0.0000 td_closeprice, case sm_prodtype when 'CF' then 1 when 'CO' then 2 else 6 end td_sortlist, sm_prodtype,0, sm_multiplier , 0 td_tradeid, '' td_time, 0 td_orderid";
                strsql = strsql + " From " + strTrades + ", #tmpmosesdates, " + strSeries_master + "," + strClient_master + " ";
                strsql = strsql + "Where td_clientcd = cm_cd and td_exchange = sm_exchange And td_seriesid = sm_seriesid and sm_expirydt >= bd_dt and  td_dt < bd_dt and td_exchange = '" + strExchange + "' and sm_prodtype in('CF') and cm_cd = '" + strUserid + "' group by bd_dt,td_clientcd,td_mainbrcd,td_seriesid,sm_prodtype, sm_multiplier   having sum(td_bqty - td_sqty) <> 0 ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "insert into #tmpfobill select 2 td_controlflag,bd_dt,td_clientcd,td_mainbrcd, td_seriesid,'',td_bqty,td_sqty, td_rate,td_mainbrrate,td_mainbrrate, td_servicetax,0.0000 td_closeprice, case sm_prodtype when 'CF' then 1 when 'CO' then 2 else 6 end td_sortlist, sm_prodtype, 0, sm_multiplier, td_tradeid, td_time, td_orderid";
                strsql = strsql + " From " + strTrades + ", #tmpmosesdates, " + strSeries_master + "," + strClient_master + " ";
                strsql = strsql + "Where td_clientcd = cm_cd and td_exchange = sm_exchange and td_seriesid = sm_seriesid and sm_expirydt >= bd_dt and  td_dt = bd_dt and td_exchange = '" + strExchange + "' and cm_cd = '" + strUserid + "' ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = " insert into #tmpfobill  select Case dl_type When 'DL' Then 7 When 'PD' Then 8 When 'SL' Then 9 When 'DS' Then 9.5 Else '' End td_controlflag, dl_BillDate,dl_clientcd,dl_mainbrcd, dl_seriesid,'',  dl_Bqty , dl_SQty ,  dl_rate , dl_mainbrrate, dl_marketrate, dl_servicetax,0,  case sm_prodtype when 'CF' then 1 when 'CO' then 2 else 6 end + 3 td_sortlist, sm_prodtype, (dl_bqty - dl_sQty) * dl_Rate * sm_multiplier  , sm_multiplier , 0 tx_tradeid, '' tx_time, 0 tx_orderid ";
                strsql = strsql + "  From " + strDelivery + ", #tmpmosesdates, " + strSeries_master + "," + strClient_master + " ";
                strsql = strsql + "  Where dl_clientcd = cm_cd And dl_exchange = sm_exchange And dl_seriesid = sm_seriesid  and  dl_BillDate = bd_dt  and dl_exchange = '" + strExchange + "' and dl_type In ('DL','SL','PD','DS')  and cm_cd = '" + strUserid + "' ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "insert into #tmpfobill select 3 td_controlflag,bd_dt,td_clientcd,td_mainbrcd, td_seriesid,'',case sign(sum(td_bqty - td_sqty)) when 1 then 0 else abs(sum(td_bqty - td_sqty)) end  td_bqty, case sign(sum(td_bqty - td_sqty)) when 1 then abs(sum(td_bqty - td_sqty)) else 0 end td_sqty, 0.0000 td_rate,0.0000 td_mainbrrate,0.0000 td_mainbrrate, 0.0000 td_servicetax,0.0000 td_closeprice, case sm_prodtype when 'CF' then 1 when 'CO' then 2 else 6 end td_sortlist, sm_prodtype,0, sm_multiplier, 0,'',0 ";
                strsql = strsql + " From " + strTrades + ", #tmpmosesdates, " + strSeries_master + "," + strClient_master + " ";
                strsql = strsql + " Where td_clientcd = cm_cd and td_exchange = sm_exchange And td_seriesid = sm_seriesid and sm_expirydt >= bd_dt and  td_dt <= bd_dt and td_exchange = '" + strExchange + "' and sm_prodtype in('CF') and cm_cd = '" + strUserid + "' group by bd_dt,td_clientcd,td_mainbrcd,td_seriesid,sm_prodtype, sm_multiplier,td_exchange having sum(td_bqty - td_sqty) <> 0";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "insert into #tmpbillcharges select tx_dt,tx_clientcd,'SERVICE TAX',round(sum(tx_servicetax),2),0 ";
                strsql = strsql + " from #tmpfobill,#tmpmosesdates," + strClient_master + " ";
                strsql = strsql + "where tx_clientcd = cm_cd and tx_dt = bd_dt and cm_cd = '" + strUserid + "' group by tx_dt,tx_clientcd having sum(tx_servicetax) > 0 ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "update #tmpfobill set tx_closerate = ms_lastprice ";
                strsql = strsql + " from #tmpfobill, " + strMarket_summary + " ";
                strsql = strsql + " where ms_seriesid = tx_seriesid and tx_controlflag in ('1','2') and ms_exchange = '" + strExchange + "' and ms_dt = tx_dt ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "update #tmpfobill set tx_rate = ms_prcloseprice ";
                strsql = strsql + " from #tmpfobill," + strMarket_summary + " ";
                strsql = strsql + " where ms_seriesid = tx_seriesid and tx_controlflag = 1 and ms_exchange = '" + strExchange + "' and ms_dt = tx_dt ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "update #tmpfobill set tx_rate = ms_lastprice ";
                strsql = strsql + " from #tmpfobill, " + strMarket_summary + " ";
                strsql = strsql + " where ms_seriesid = tx_seriesid and tx_controlflag = 3 and ms_exchange = '" + strExchange + "' and ms_dt = tx_dt ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "insert into #tmpbillcharges select fc_dt,fc_clientcd,fc_desc,round(sum(fc_amount),2),0";
                strsql = strsql + " from " + strFspecialcharges + ",#tmpmosesdates," + strClient_master + " ";
                strsql = strsql + " where fc_clientcd = cm_cd and fc_dt = bd_dt and cm_cd = '" + strUserid + "' and fc_exchange = '" + strExchange + "' group by fc_dt,fc_clientcd,fc_desc having round(sum(fc_amount),2) <> 0 ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "insert into #tmpbillcharges select fc_dt,fc_clientcd,'SERVICE TAX',round(sum(fc_servicetax),2),0 ";
                strsql = strsql + " from " + strFspecialcharges + ",#tmpmosesdates," + strClient_master + " ";
                strsql = strsql + " where fc_clientcd = cm_cd and fc_dt = bd_dt and cm_cd = '" + strUserid + "' and fc_exchange = '" + strExchange + "' group by fc_dt,fc_clientcd,fc_desc having round(sum(fc_servicetax),2) <> 0 ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "update #tmpfobill set tx_value = round(((case tx_controlflag when 5  then (tx_bqty + tx_sqty)*0 when 6 then (tx_bqty + tx_sqty)*0 else (tx_bqty - tx_sqty) end) *tx_rate)*tx_multiplier,4) ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "insert into #tmpfobill select 10 ,bc_dt,bc_clientcd,bc_clientcd, 1,upper(bc_desc),0 td_bqty,0 td_sqty, 0,0 td_mainbrrate,0 td_mainbrrate, 0 td_servicetax,0.0000 td_closeprice, 10 td_sortlist, 'XX',round(sum(bc_amount),2),0,0,'',0 ";
                strsql = strsql + " From #tmpbillcharges group by bc_dt,bc_clientcd,bc_desc ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "insert into #tmpfobill select 90 ,fb_billdt,fb_clientcd,fb_clientcd, 1,'[PREV. DAY MRGN.]',0 td_bqty,0 td_sqty, 0,0 td_mainbrrate,0 td_mainbrrate, 0 td_servicetax,0.0000 td_closeprice, 90 td_sortlist, 'XX',round(fb_margin1,2),0, 0,'',0 ";
                strsql = strsql + " From #tmpmosesdates," + strFbills + "," + strClient_master + " ";
                strsql = strsql + " where fb_clientcd = cm_cd and fb_billdt = bd_dt and fb_exchange = '" + strExchange + "' and fb_margin1 <> 0 and fb_postmrgyn = 'Y' and cm_cd = '" + strUserid + "' ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "insert into #tmpfobill select 91 ,fb_billdt,fb_clientcd,fb_clientcd, 1,'[CURR. DAY MRGN.]',0 td_bqty,0 td_sqty, 0,0 td_mainbrrate,0 td_mainbrrate, 0 td_servicetax,0.0000 td_closeprice, 91 td_sortlist, 'XX',round(fb_margin2,2),0, 0, '', 0 ";
                strsql = strsql + " From #tmpmosesdates," + strFbills + "," + strClient_master + " ";
                strsql = strsql + " where fb_clientcd = cm_cd and fb_billdt = bd_dt and fb_exchange = '" + strExchange + "' and fb_margin2 <> 0 and fb_postmrgyn = 'Y' and cm_cd = '" + strUserid + "' ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "alter table #tmpfobill add tx_billno numeric default(0) NOT NULL ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "update #tmpfobill set tx_billno = fb_billno ";
                strsql = strsql + " from #tmpfobill," + strFbills + " ";
                strsql = strsql + " where fb_clientcd = tx_clientcd  and fb_exchange = '" + strExchange + "' and fb_billdt = tx_dt ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "alter table #tmpfobill add tx_unit char (15) ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "update a Set tx_unit=left(rtrim(convert(char,convert(numeric,floor(pm_divisor))))+' '+pm_unitper,15) ";
                strsql = strsql + " from #tmpfobill a, " + strSeries_master + ", " + strproduct_master + " ";
                strsql = strsql + " where tx_seriesid = sm_seriesid and sm_exchange='" + strExchange + "' and sm_prodtype=pm_type and sm_exchange=pm_exchange and sm_symbol=pm_assetcd ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = " Select sm_sname smsname,sm_prodtype TRXFLAG, tx_bqty-tx_sqty Qty,cast((tx_rate) as decimal(15,2)) as tdrate,cast((tx_closerate) as decimal(15,2)) as lastclose, ";
                strsql += " cast((tx_value) as decimal(15,2)) as drcr,tx_sortlist sortorder ";
                strsql += " from #tmpfobill," + strSeries_master + "," + strClient_master + " ";
                strsql += " where tx_clientcd = cm_cd and tx_seriesid = sm_seriesid and sm_exchange = '" + strExchange + "' and tx_controlflag < 10 and cm_brboffcode <> '' ";
                strsql += " union all ";
                strsql += " select tx_desc smsname,'EF' TRXFLAG,tx_bqty-tx_sqty Qty,cast((tx_rate) as decimal(15,2)) as tdrate,cast((tx_closerate) as decimal(15,2)) as lastclose, ";
                strsql += " cast((tx_value) as decimal(15,2)) as drcr,tx_sortlist sortorder ";
                strsql += " from #tmpfobill," + strClient_master + " ";
                strsql += " where tx_clientcd = cm_cd  and tx_controlflag >= 10 and cm_brboffcode <> ''  ";
                strsql += " order by tx_sortlist";
            }
            else
            {
                bool blnInterOP = false;
                string StrMsg = "";
                string[] arrexchange;
                string StrExchWhere = "";

                StrMsg = fnCheckInterOperability(strDate, strSegment);
                if (StrMsg == "TRUE")
                {
                    blnInterOP = true;
                    arrexchange = fnGetInterOpExchange(strSegment, strCompCd).Split(",");
                    StrExchWhere += "('" + arrexchange[0] + "'";
                    for (int j = 1; j <= arrexchange.Length - 1; j++)
                        StrExchWhere += ",'" + arrexchange[j] + "'";
                    StrExchWhere += ")";
                }
                if (blnInterOP)
                    StrExchWhere = "";
                else
                    StrExchWhere = "and td_exchange = '" + strExchange + "'";

                prTempFOBill(objConnection, objTrans);

                strsql = "insert into #tmpmosesdates values('" + strDate + "') ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                string strIndexName;
                DataTable dtTable = new DataTable();

                strIndexName = "idx_trades_dt_clientcd";
                strsql = "Select Name from sysindexes where Name= 'idx_trades_clientcd'";
                dtTable = OpenDataTableTemp(strsql, objConnection, objTrans);
                if ((dtTable.Rows.Count > 0))
                    strIndexName = "idx_trades_clientcd";

                strsql = "insert into  #tmpfobill  select 1 td_controlflag,'" + strDate + "',td_clientcd, ";
                strsql += " td_mainbrcd, td_seriesid,'',case sign(sum(td_bqty - td_sqty)) when 1 then abs(sum(td_bqty - td_sqty)) else 0 end  td_bqty, case sign(sum(td_bqty - td_sqty)) when 1 then 0 else abs(sum(td_bqty - td_sqty)) end td_sqty, 0.0000 td_rate,0.0000 td_mainbrrate,0.0000 td_mainbrrate, 0.0000 td_servicetax,0.0000 td_closeprice, case sm_prodtype when 'IF' then 1 when 'CF' then 1 when 'EF' then 2 when 'IO' then 5 else 6 end td_sortlist, sm_prodtype,0, td_exchange  ";
                strsql += " From Trades with(nolock,index(" + strIndexName + ")) , Series_master with(nolock),Client_master with(nolock)";
                strsql += "  Where td_clientcd = cm_cd and td_exchange = sm_exchange and td_Segment = sm_Segment And td_seriesid = sm_seriesid and sm_expirydt >= '" + strDate + "' and  td_dt < '" + strDate + "' " + StrExchWhere + " and td_Segment = '" + strSegment + "' and sm_prodtype in('IF','EF','CF')  and ltrim(rtrim(td_groupid)) <> 'B'  and td_clientcd = '" + strUserid + "' group by td_clientcd,td_mainbrcd,td_seriesid,sm_prodtype, td_exchange having sum(td_bqty - td_sqty) <> 0 ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = " insert into #tmpfobill select 2 td_controlflag,td_dt,td_clientcd,td_mainbrcd, td_seriesid,'',td_bqty,td_sqty, td_rate, td_mainbrrate, td_mainbrrate, td_servicetax,0.0000 td_closeprice, case sm_prodtype when 'IF' then 1 when 'CF' then 1 when 'EF' then 2 when 'IO' then 5 else 6 end td_sortlist, sm_prodtype,0, td_exchange ";
                strsql += " From Trades with(nolock,index(" + strIndexName + ")) , Series_master with(nolock),Client_master with(nolock)";
                strsql += " Where td_clientcd = cm_cd and td_exchange = sm_exchange and td_Segment = sm_Segment and td_seriesid = sm_seriesid and sm_expirydt >= '" + strDate + "' and  td_dt between '" + strDate + "' and '" + strDate + "' " + StrExchWhere + " and td_Segment = '" + strSegment + "' and cm_cd = '" + strUserid + "' Order By td_tradeid , td_subtradeid ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = " insert into #tmpfobill select 2 td_controlflag,td_dt,td_clientcd,td_mainbrcd, td_seriesid,'',td_sqty,td_bqty, td_MarketRate, td_mainbrrate, td_mainbrrate, 0,0.0000 td_closeprice, case sm_prodtype when 'IF' then 1 when 'CF' then 1 when 'EF' then 2 when 'IO' then 5 else 6 end td_sortlist, sm_prodtype,0, td_exchange ";
                strsql += "  From Trades with(nolock,index(" + strIndexName + ")) , Series_master,Client_master  ";
                strsql += "  Where td_clientcd = cm_cd and td_exchange = sm_exchange and td_Segment = sm_Segment and td_seriesid = sm_seriesid and sm_expirydt >= '" + strDate + "' and  td_dt between '" + strDate + "' and '" + strDate + "' " + StrExchWhere + " and td_Segment = '" + strSegment + "' and ltrim(rtrim(td_groupid)) = 'B'  and td_clientcd = '" + strUserid + "' Order By td_tradeid , td_subtradeid ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = " insert into #tmpfobill  select 99 tx_controlflag,bd_dt,tx_clientcd,tx_mainbrcd, tx_seriesid,'',  case sign(sum(tx_bqty - tx_sqty)) when 1 then abs(sum(tx_bqty - tx_sqty)) else 0 end  tx_bqty,  case sign(sum(tx_bqty - tx_sqty)) when 1 then 0 else abs(sum(tx_bqty - tx_sqty)) end tx_sqty,  0.0000 tx_rate,0.0000 tx_mainbrrate,0.0000 tx_mainbrrate, 0.0000 tx_servicetax,0.0000 tx_closeprice,  case sm_prodtype when 'IF' then 1 when 'CF' then 1 when 'EF' then 2 when 'IO' then 5 else 6 end tx_sortlist, sm_prodtype,0, sm_exchange ";
                strsql += " From #tmpfobill  , #tmpmosesdates , Series_master with (nolock),Client_master with (nolock)";
                strsql += " Where tx_clientcd = cm_cd and sm_exchange = '" + strExchange + "'  and sm_Segment = '" + strSegment + "' And tx_seriesid = sm_seriesid  and sm_expirydt >= bd_dt and  tx_dt < bd_dt  and sm_prodtype in('IF','EF','CF')  and tx_controlflag not in ( '99','3')  group by bd_dt,tx_clientcd,tx_mainbrcd,tx_seriesid,sm_prodtype, sm_exchange  Having Sum(tx_bqty - tx_sqty) <> 0 ";
                strsql += " Union All ";
                strsql += " select 3 tx_controlflag,bd_dt,tx_clientcd,tx_mainbrcd, tx_seriesid,'', ";
                strsql += " case sign(sum(tx_bqty - tx_sqty)) when 1 then 0 else abs(sum(tx_bqty - tx_sqty)) end tx_bqty, ";
                strsql += " case sign(sum(tx_bqty - tx_sqty)) when 1 then abs(sum(tx_bqty - tx_sqty)) else 0 end  tx_sqty, ";
                strsql += " 0.0000 tx_rate,0.0000 tx_mainbrrate,0.0000 tx_mainbrrate, 0.0000 tx_servicetax,0.0000 tx_closeprice, ";
                strsql += " case sm_prodtype when 'IF' then 1 when 'CF' then 1 when 'EF' then 2 when 'IO' then 5 else 6 end tx_sortlist, sm_prodtype,0,sm_exchange ";
                strsql += " From #tmpfobill  , #tmpmosesdates , Series_master with (nolock),Client_master with (nolock) ";
                strsql += " Where tx_clientcd = cm_cd and sm_exchange = '" + strExchange + "'  and sm_Segment = '" + strSegment + "' And tx_seriesid = sm_seriesid ";
                strsql += " and sm_expirydt >= bd_dt and  tx_dt <= bd_dt ";
                strsql += " and sm_prodtype in('IF','EF','CF') ";
                strsql += " and tx_controlflag not in ( '99','3') ";
                strsql += " group by bd_dt,tx_clientcd,tx_mainbrcd,tx_seriesid,sm_prodtype ,sm_exchange";
                strsql += " Having Sum(tx_bqty - tx_sqty) <> 0 ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "Update #tmpfobill set tx_controlflag = '1' where tx_controlflag = '99' ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "insert into #tmpfobill select case ex_eaflag when 'E' then 5 else 6 end td_controlflag,ex_dt,ex_clientcd,ex_mainbrcd, ex_seriesid,'',ex_eqty,ex_aqty, ex_diffbrokrate,ex_mainbrdiffrate,ex_mainbrdiffrate, ex_servicetax,ex_settlerate, case sm_prodtype when 'IF' then 1 when 'CF' then 1 when 'EF' then 2 when 'IO' then 5 else 6 end + 3 td_sortlist, sm_prodtype,0, ex_exchange ";
                strsql += " From Exercise with (nolock), Series_master with (nolock) ,Client_master with (nolock) ";
                strsql += "Where ex_clientcd = cm_cd and ex_exchange = sm_exchange and ex_Segment = sm_Segment And ex_seriesid = sm_seriesid and sm_expirydt >= '" + strDate + "' and ex_dt between '" + strDate + "' and '" + strDate + "' " + StrExchWhere.Replace("td_", "ex_") + " and ex_Segment = '" + strSegment + "' and cm_cd = '" + strUserid + "' ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "insert into #tmpbillcharges select tx_dt,tx_clientcd,'SERVICE TAX',round(sum(tx_servicetax),2),0,tx_exchange ";
                strsql += " from #tmpfobill,#tmpmosesdates,Client_master with (nolock) ";
                strsql += " where tx_clientcd = cm_cd and tx_dt = bd_dt and cm_cd = '" + strUserid + "' group by tx_dt,tx_clientcd,tx_exchange having sum(tx_servicetax) > 0 ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "update #tmpfobill set tx_closerate = ms_lastprice ";
                strsql += " from #tmpfobill, Market_summary with (nolock) ";
                strsql += " where ms_seriesid = tx_seriesid and tx_controlflag in ('1','2') and ms_exchange=tx_exchange and ms_Segment = '" + strSegment + "' and ms_dt = tx_dt ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "update #tmpfobill set tx_rate = ms_prcloseprice ";
                strsql += " from #tmpfobill,Market_summary with (nolock) ";
                strsql += " where ms_seriesid = tx_seriesid and tx_controlflag = 1 and ms_exchange = tx_Exchange and ms_Segment = '" + strSegment + "' and ms_dt = tx_dt ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "update #tmpfobill set tx_rate = ms_lastprice from #tmpfobill,Market_summary with (nolock) ";
                strsql += " where ms_seriesid = tx_seriesid and tx_controlflag = 3";
                strsql += " and ms_exchange = tx_Exchange and ms_Segment = '" + strSegment + "'";
                strsql += " and ms_dt = tx_dt";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "insert into #tmpbillcharges select fc_dt,fc_clientcd,fc_desc,round(sum(fc_amount),2),0,fc_exchange ";
                strsql += " from Fspecialcharges with (nolock) ,#tmpmosesdates,Client_master with (nolock) ";
                strsql += " where fc_clientcd = cm_cd and fc_dt = bd_dt and cm_cd = '" + strUserid + "' " + StrExchWhere.Replace("td_", "fc_") + " and fc_Segment = '" + strSegment + "' group by fc_dt,fc_clientcd,fc_desc,fc_exchange having round(sum(fc_amount),2) <> 0 ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "insert into #tmpbillcharges select fc_dt,fc_clientcd,'SERVICE TAX',round(sum(fc_servicetax),2),0,fc_exchange ";
                strsql += " from Fspecialcharges with (nolock) ,#tmpmosesdates,Client_master with (nolock) ";
                strsql += " where fc_clientcd = cm_cd and fc_dt = bd_dt and cm_cd = '" + strUserid + "' " + StrExchWhere.Replace("td_", "fc_") + " and fc_Segment = '" + strSegment + "' group by fc_dt,fc_clientcd,fc_desc,fc_exchange having round(sum(fc_servicetax),2) <> 0 ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "update #tmpfobill set tx_value = round(((case tx_controlflag when 5  then (tx_bqty + tx_sqty)*-1 when 6 then (tx_bqty + tx_sqty)*-1 else (tx_bqty - tx_sqty) end) *tx_rate)*sm_multiplier,4)";
                strsql += " From series_master with (nolock) ";
                strsql += " Where sm_exchange = tx_Exchange and sm_Segment = '" + strSegment + "' and tx_seriesid = sm_seriesid ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "insert into #tmpfobill select 10 ,bc_dt,bc_clientcd,bc_clientcd, 1,upper(bc_desc),0 td_bqty,0 td_sqty, 0,0 td_mainbrrate,0 td_mainbrrate, 0 td_servicetax,0.0000 td_closeprice, 10 td_sortlist, 'XX',round(sum(bc_amount),2),bc_exchange From #tmpbillcharges group by bc_dt,bc_clientcd,bc_desc,bc_exchange ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "insert into #tmpfobill select 90 ,fb_billdt,fb_clientcd,fb_clientcd, 1,'[PREV. DAY MRGN.]',0 td_bqty,0 td_sqty, 0,0 td_mainbrrate,0 td_mainbrrate, 0 td_servicetax,0.0000 td_closeprice, 90 td_sortlist, 'XX',round(Case when fb_postmrgyn = 'Y' then fb_margin1 else 0 end + CAse When fb_postExpmrgyn = 'Y' then fb_Expmargin1 else 0 end ,2),fb_exchange ";
                strsql += " From #tmpmosesdates,Fbills with (nolock) ,Client_master with (nolock) ";
                strsql += " where fb_clientcd = cm_cd and fb_billdt = bd_dt and fb_exchange = '" + strExchange + "' and fb_Segment = '" + strSegment + "' and round(Case when fb_postmrgyn = 'Y' then fb_margin1 else 0 end + CAse When fb_postExpmrgyn = 'Y' then fb_Expmargin1 else 0 end ,2) <> 0  and cm_cd = '" + strUserid + "' ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "insert into #tmpfobill select 91 ,fb_billdt,fb_clientcd,fb_clientcd, 1,'[CURR. DAY MRGN.]',0 td_bqty,0 td_sqty, 0,0 td_mainbrrate,0 td_mainbrrate, 0 td_servicetax,0.0000 td_closeprice, 91 td_sortlist, 'XX',round(Case When fb_postmrgyn = 'Y' then fb_margin2 else 0 end + Case When fb_postExpmrgyn = 'Y' then fb_Expmargin2 else 0 end,2),fb_exchange ";
                strsql += " From #tmpmosesdates,Fbills with (nolock) ,Client_master with (nolock) ";
                strsql += " where fb_clientcd = cm_cd and fb_billdt = bd_dt and fb_exchange = '" + strExchange + "' and fb_Segment = '" + strSegment + "' and round(Case When fb_postmrgyn = 'Y' then fb_margin2 else 0 end + Case When fb_postExpmrgyn = 'Y' then fb_Expmargin2 else 0 end,2) <> 0  and cm_cd = '" + strUserid + "' ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "alter table #tmpfobill add tx_billno numeric default(0) NOT NULL ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);

                strsql = "update #tmpfobill set tx_billno = fb_billno from #tmpfobill,Fbills with (nolock) where fb_clientcd = tx_clientcd  and fb_exchange = tx_exchange and fb_Segment = '" + strSegment + "' and fb_billdt = tx_dt  ";
                ExecuteSQLTemp(strsql, objConnection, objTrans);


                strsql = "select sm_desc smsname,sm_productcd TRXFLAG,tx_bqty - tx_sqty Qty,cast((tx_rate)as decimal(15,2))as tdrate,";
                strsql += " cast((tx_closerate)as decimal(15,2))as lastclose,cast((tx_value)as decimal(15,2))as drcr,tx_sortlist sortorder";
                strsql += " from #tmpfobill,Series_master with (nolock) ,Client_master with (nolock) ";
                strsql += " where tx_clientcd = cm_cd and tx_seriesid = sm_seriesid and sm_exchange = tx_exchange and sm_Segment = '" + strSegment + "' and tx_controlflag < 10 ";
                strsql += " union all ";
                strsql += " Select tx_desc smsname,'EF' TRXFLAG,tx_bqty - tx_sqty Qty,cast((tx_rate)as decimal(15,2))as tdrate,";
                strsql += " cast((tx_closerate)as decimal(15,2))as lastclose,cast((tx_value)as decimal(15,2)) as drcr,tx_sortlist sortorder ";
                strsql += " from #tmpfobill,Client_master with (nolock) ";
                strsql += " where tx_clientcd = cm_cd and tx_controlflag >= 10 ";
                strsql += " order by sortorder,smsname ";
            }
            return strsql;
        }

        private string fnGetInterOpStlmnts(string strStlmnt, bool blnIncludeT2T = false)
        {
            DataTable dtInterOP;
            string strData;
            string strSQL = "";
            if (Strings.InStr(1, ",BR,BW,BC,NA,NN,NZ,MA,MN,MZ,BQ,BU,NQ,NU,", "," + Strings.Left(strStlmnt, 2) + ",") > 0)
            {
                strData = "";

                strSQL = " select * from SEttlements Where se_stdt = (Select se_stdt from Settlements " + " Where se_stlmnt = '" + strStlmnt + "')";
                if (Strings.InStr(1, ",BW,NN,MN,", "," + Strings.Left(strStlmnt, 2) + ",") > 0)
                    strSQL = strSQL + " and left(se_stlmnt,2) in ('BW','NN','MN')";
                else if (Strings.InStr(1, ",BC,NZ,MZ,", "," + Strings.Left(strStlmnt, 2) + ",") > 0)
                    strSQL = strSQL + " and left(se_stlmnt,2) in ('BC','NZ','MZ')";
                else if (Strings.InStr(1, ",BQ,NQ,", "," + Strings.Left(strStlmnt, 2) + ",") > 0)
                    strSQL = strSQL + " and left(se_stlmnt,2) in ('BQ','NQ')";
                else if (Strings.InStr(1, ",BU,NU,", "," + Strings.Left(strStlmnt, 2) + ",") > 0)
                    strSQL = strSQL + " and left(se_stlmnt,2) in ('BU','NU')";
                else
                    strSQL = strSQL + " and left(se_stlmnt,2) in ('BR','NA','MA')";
                dtInterOP = objUtility.OpenDataTable(strSQL);
                if (dtInterOP.Rows.Count == 0)
                    strData = strStlmnt + ",";
                else
                {
                    strData = "";
                    for (int intRow = 0; intRow <= dtInterOP.Rows.Count - 1; intRow++)
                        strData = strData + dtInterOP.Rows[intRow]["se_stlmnt"] + ",";
                }

                if (blnIncludeT2T)
                {
                    strSQL = " select * from SEttlements Where se_stdt = (Select se_stdt from Settlements " + " Where se_stlmnt = '" + strStlmnt + "')";
                    strSQL = strSQL + " and left(se_stlmnt,2) in ('BC','NZ','MZ')";
                    dtInterOP = objUtility.OpenDataTable(strSQL);
                    if (dtInterOP.Rows.Count == 0)
                        strData = strData + strStlmnt + ",";
                    else
                        for (int intRow = 0; intRow <= dtInterOP.Rows.Count - 1; intRow++)
                            strData = strData + dtInterOP.Rows[intRow]["se_stlmnt"] + ",";
                }
                strData = Strings.Left(strData, Strings.Len(strData) - 1);
                return strData;
            }
            else
                return strStlmnt;
        }

        private bool fnIsInterOperability(string strSegment, string strDate, string strStlmnt = "")
        {
            string strSQL;
            DataTable dtInterOP;

            if (strSegment == "C")
            {
                if (strStlmnt != "")
                {
                    if (Strings.InStr(1, ",BR,BW,BC,NA,NN,NZ,MA,MN,MZ,", "," + Strings.Left(strStlmnt, 2) + ",") > 0)
                    {
                    }
                    else if (Strings.InStr(1, ",L,M,", "," + objUtility.fnFireQuery("Settlement_type", "sy_maptype", "sy_exchange+sy_type", Strings.Left(strStlmnt, 2), true) + ",") > 0)
                    {
                        return false;
                    }
                    strDate = objUtility.fnFireQuery("Settlements", "se_stdt", "se_stlmnt", strStlmnt, true);
                }
            }

            strSQL = " select * from ClearingHouse " + " Where CH_Segment = '" + strSegment + "'" + " and CH_EffDt = (Select max(CH_EffDt) from ClearingHouse " + " Where CH_Segment = '" + strSegment + "' and CH_EffDt <='" + strDate + "')";
            dtInterOP = objUtility.OpenDataTable(strSQL);
            if (dtInterOP.Rows.Count > 0)
            {
                return true;
            }
            return false;
        }

        public void PrInserttblmargincol(SqlConnection objConnection, string strUserid, SqlTransaction objTrans)
        {
            string Strsql = "";
            Strsql = "INSERT INTO ";
            Strsql += " #tempmargincollaterial ( ";
            Strsql += " ts_scripcd,ts_scripname,ts_qty,ts_closeprice,";
            Strsql += " ts_value,ts_haircut,ts_haircutvalue,ts_netvalue,ts_rflag,ts_category , ts_categoryname , ";
            Strsql += " ts_clientcd , ts_collateraltype ,ts_transactiondt ,ts_maturitydt,ts_amount,ts_isin )";
            Strsql += " SELECT fc_pscd,left(ss_name,20),";
            Strsql += " sum(case fc_controlflag  when 'D' then fc_qty else fc_qty*(-1) end),";
            Strsql += " 0,0,0,0,0,'S','1','Share',fc_clientcd , fc_collateraltype,'','',0,''";
            Strsql += " FROM  Fcollateral_detail,Fcollateral_types,Securities,Client_master";
            Strsql += " WHERE fc_collateraltype = fct_cd and fc_pscd = ss_cd ";
            Strsql += " and fc_clientcd = cm_cd";
            Strsql += " and fct_category = 'SH' and fc_clientcd='" + strUserid + "'";
            Strsql += " GROUP BY fc_clientcd ,fc_collateraltype,fc_pscd,ss_name,ss_HairCut";
            ExecuteSQLTemp(Strsql, objConnection, objTrans);

            Strsql = "update #tempmargincollaterial set ts_closeprice =isnull((select mk_closerate from Market_rates  ";
            Strsql += " where mk_exchange = 'B' and mk_scripcd = ts_scripcd ";
            Strsql += " and mk_dt =(select max(mk_dt) from Market_rates ";
            Strsql += " where mk_exchange = 'B'";
            Strsql += " and mk_scripcd = ts_scripcd )),0)";
            ExecuteSQLTemp(Strsql, objConnection, objTrans);

            Strsql = "update #tempmargincollaterial set ts_closeprice =isnull((select mk_closerate from Market_rates  ";
            Strsql += " where mk_exchange = 'N' and mk_scripcd = ts_scripcd ";
            Strsql += " and mk_dt =(select max(mk_dt) from Market_rates ";
            Strsql += " where mk_exchange = 'N'";
            Strsql += " and mk_scripcd = ts_scripcd )),0)";

            ExecuteSQLTemp(Strsql, objConnection, objTrans);

            Strsql = "update #tempmargincollaterial set ts_haircut =isnull((select case vm_exchange When 'B' then vm_margin_rate  else vm_applicable_var end from VarMargin  ";
            Strsql += " where vm_exchange = 'B' and vm_scripcd = ts_scripcd ";
            Strsql += " and vm_dt =(select max(vm_dt) from VarMargin ";
            Strsql += " where vm_exchange = 'B'";
            Strsql += " and vm_scripcd = ts_scripcd )),0)";
            ExecuteSQLTemp(Strsql, objConnection, objTrans);

            Strsql = "update #tempmargincollaterial set ts_haircut =isnull((select case vm_exchange When 'B' then vm_margin_rate  else vm_applicable_var end from VarMargin  ";
            Strsql += " where vm_exchange = 'N' and vm_scripcd = ts_scripcd ";
            Strsql += " and vm_dt =(select max(vm_dt) from VarMargin ";
            Strsql += " where vm_exchange = 'N'";
            Strsql += " and vm_scripcd = ts_scripcd )),0)";
            ExecuteSQLTemp(Strsql, objConnection, objTrans);

            Strsql = "UPDATE a ";
            Strsql += " SET ts_haircut = ts_haircut + (select cm_haircut ";
            Strsql += " FROM Client_master where cm_cd = a.ts_clientcd )";
            Strsql += " FROM #tempmargincollaterial a";
            ExecuteSQLTemp(Strsql, objConnection, objTrans);


            Strsql = "update #tempmargincollaterial set ts_value = ts_qty * ts_closeprice";
            ExecuteSQLTemp(Strsql, objConnection, objTrans);

            Strsql = "update #tempmargincollaterial set ts_haircutvalue = case ts_value when 0 then 0 else round(ts_value * ts_haircut/100,2) end";
            ExecuteSQLTemp(Strsql, objConnection, objTrans);

            Strsql = "update #tempmargincollaterial set ts_netvalue = ts_value - ts_haircutvalue";
            ExecuteSQLTemp(Strsql, objConnection, objTrans);


            Strsql = "INSERT INTO ";
            Strsql += " #tempmargincollaterial ( ";
            Strsql += " ts_scripcd,ts_scripname,ts_qty,ts_closeprice,";
            Strsql += " ts_value,ts_haircut,ts_haircutvalue,ts_netvalue,ts_rflag,ts_category ,ts_categoryname, ";
            Strsql += " ts_clientcd , ts_collateraltype ,ts_transactiondt ,ts_maturitydt,ts_amount,ts_isin )";
            Strsql += " SELECT fct_category,(case fct_category when 'FD' then 'Fixed Deposit' else 'Bank Gurantee' end),";
            Strsql += " 10,0,";
            Strsql += " 0,0,0,";
            Strsql += " 0,'S',(case fct_category when 'FD' then '2' else '3' end),";
            Strsql += " (case fct_category when 'FD' then 'Fixed Dep' else 'Bank Gu' end),fc_clientcd , fc_collateraltype , fc_transactiondt ,fc_maturitydt,Case fc_controlflag When 'D' Then fc_amount  else - fc_amount end,''  ";
            Strsql += " FROM  Fcollateral_detail,Fcollateral_types,Client_master";
            Strsql += " WHERE fc_collateraltype = fct_cd ";
            Strsql += " and fc_clientcd = cm_cd and fc_clientcd='" + strUserid + "'";
            Strsql += " and fct_category in ('FD','BG')";
            ExecuteSQLTemp(Strsql, objConnection, objTrans);

            Strsql = "Update #tempmargincollaterial set ts_isin = isNull";
            Strsql += "((select top 1 im_isin From isin Where im_Scripcd = ts_scripcd ";
            Strsql += "and im_priority in (select min(im_priority) From isin where im_Scripcd = ts_scripcd )),'')";
            ExecuteSQLTemp(Strsql, objConnection, objTrans);
        }

        public void PrCreatetblmargincol(SqlConnection objConnection, SqlTransaction objTrans)
        {
            string Strsql;
            ExecuteSQLTemp("IF OBJECT_ID('tempdb..#tempmargincollaterial') IS NOT NULL DROP TABLE #tempmargincollaterial", objConnection, objTrans);

            Strsql = "Create table #tempmargincollaterial(";
            Strsql += " ts_scripcd char(8) not null,";
            Strsql += " ts_scripname char(50) not null,";
            Strsql += " ts_qty numeric not null,";
            Strsql += " ts_closeprice money not null,";
            Strsql += " ts_value money not null,";
            Strsql += " ts_haircut money not null,";
            Strsql += " ts_haircutvalue money not null,";
            Strsql += " ts_netvalue money not null,";
            Strsql += " ts_rflag char(1) not null,";
            Strsql += " ts_category char(1) not null,";
            Strsql += " ts_categoryname char(20) not null,";
            Strsql += " ts_clientcd char(8) not null,";
            Strsql += " ts_collateraltype char(8) not null,";
            Strsql += " ts_transactiondt char(8) not null,";
            Strsql += " ts_maturitydt char(8) not null,";
            Strsql += " ts_amount money not null,";
            Strsql += " ts_isin char(12) not null";
            Strsql += " )";
            ExecuteSQLTemp(Strsql, objConnection, objTrans);
        }

        public string FormatCurr(string str, int intDecimal = 0, string strComma = "N")
        {
            if (!Information.IsNumeric(str))
                str = "0";

            string strR = Strings.FormatNumber(str, intDecimal);
            if (strComma == "N" | Conversion.Val(str) < 1000d)
                return strR;

            string strI = Strings.Split(strR, ".")[0];
            string strF = Strings.Split(strR + ".", ".")[1];
            int iX = 4;
            while (iX <= strI.Length)
            {
                strI = Strings.Mid(strI, 1, strI.Length - iX + 1) + "," + Strings.Right(strI, iX - 1);
                iX += 3;
            }
            if (intDecimal > 0)
            {
                strR = strI + "." + strF;
            }
            else
            {
                strR = strI;
            }
            return strR;
        }

        public dynamic GetWebParameter(string strParmcd)
        {
            var dtTable = new DataTable();
            dtTable = objUtility.OpenDataTable("select sp_sysvalue from WebParameter with (nolock) where sp_parmcd='" + strParmcd + "'");
            if (dtTable.Rows.Count > 0)
            {
                return dtTable.Rows[0][0];
            }
            else
            {
                return "";
            }
        }

        public string GetWebParameter(string strParmcd, bool blnconn)
        {
            var dtTable = new DataTable();
            dtTable = objUtility.OpenDataTable("select sp_sysvalue from WebParameter with (nolock) where sp_parmcd='" + strParmcd + "'");
            if (dtTable.Rows.Count > 0)
            {
                return dtTable.Rows[0][0].ToString();
            }
            else
            {
                return "";
            }
        }
        public string fnGetLedgerBalanceSQL(string struserid, string strYear)
        {
            string strsQL;
            string strYEARWhere;
            string strFromDt;
            string strToDt;
            bool blnMAccount = false;
            if (strYear.Trim().Length == 0)
            {
                strYear = (string)objUtility.OpenDataTable(fnGetLedgerYearSQL(struserid)).Rows[0][0];
            }

            strFromDt = "20" + Strings.Mid(strYear, 3, 2) + "0401";
            strToDt = "20" + Strings.Mid(strYear, 6, 2) + "0331";
            strYEARWhere = " and ld_dt <= '" + strToDt + "'";

            strsQL = " select Year,account,ld_clientcd,exch,ld_dpid,Rtrim(Ltrim(convert(char,abs(OpenBal)))) + case When OpenBal > 0 Then ' Dr' else ' Cr' End OpenBal,Debit,Credit,Rtrim(Ltrim(convert(char,abs(Closing)))) + case When Closing > 0 Then ' Dr' else ' Cr' End Closing,heading,Ordr from ( ";
            if (GetWebParameter("IsTradeWeb") == "O")
            {
                strsQL += " select '" + strYear + "' Year, 'C' as account,ld_clientcd, right(rtrim(ld_dpid),1) as exch,ld_dpid, ";
                strsQL += " cast( Sum(case When ld_dt <'" + strFromDt + "' Then ld_amount else 0 end) as decimal(15,2)) OpenBal,";
                strsQL += " cast( Sum(case When ld_dt >= '" + strFromDt + "' and ld_debitflag = 'D' Then ld_amount else 0 end) as decimal(15,2)) Debit, ";
                strsQL += " cast( Sum(case When ld_dt >= '" + strFromDt + "' and ld_debitflag = 'C' Then -ld_amount else 0 end) as decimal(15,2)) Credit,";
                strsQL += " cast( Sum(ld_amount) as decimal(15,2)) Closing ,";
                strsQL += " case substring(ld_dpid,2,1) when 'B' then 'BSE'  when 'N' then 'NSE' when 'M' then 'MCX' when 'F' then 'NCDEX' end + '-' + case substring(ld_dpid,3,1) when 'C' then 'Cash' when 'F' then 'DERIVATIVE' when 'K' then 'FX'  when 'M' then 'MF' when 'X' then 'COMM'  end heading,";
                strsQL += " case substring(ld_dpid,3,1) when 'C' then 1 else 2 end +case substring(ld_dpid,2,1) when 'B' then 10 when 'N' then 10 else 20 end Ordr ";
                strsQL += " from ledger with (nolock) ";
                strsQL += " where ld_clientcd='" + struserid + "'" + strYEARWhere + " group by ld_dpid , ld_clientcd";
                if (blnMAccount == true)
                {
                    strsQL += " union all ";
                    strsQL += " select '" + strYear + "' Year,'M' as account,ld_clientcd, right(rtrim(ld_dpid),1) as exch,ld_dpid,";
                    strsQL += " cast( Sum(case When ld_dt <'" + strFromDt + "' Then ld_amount else 0 end) as decimal(15,2)) OpenBal,";
                    strsQL += " cast( Sum(case When ld_dt >= '" + strFromDt + "' and ld_debitflag = 'D' Then ld_amount else 0 end) as decimal(15,2)) Debit, ";
                    strsQL += " cast( Sum(case When ld_dt >= '" + strFromDt + "' and ld_debitflag = 'C' Then -ld_amount else 0 end) as decimal(15,2)) Credit,";
                    strsQL += " cast( Sum(ld_amount) as decimal(15,2)) Closing ,";
                    strsQL += " case substring(ld_dpid,2,1) when 'B' then 'BSE'  when 'N' then 'NSE' when 'M' then 'MCX' when 'F' then 'NCDEX' end + '-' + case substring(ld_dpid,3,1) when 'C' then 'Cash' when 'F' then 'DERIVATIVE' when 'K' then 'FX' when 'X' then 'COMM' end + ' (M)' heading,";
                    strsQL += " case substring(ld_dpid,3,1) when 'C' then 1 else 2 end + case substring(ld_dpid,2,1) when 'B' then 30 when 'N' then 30 else 40 end";
                    strsQL += " from ledger with (nolock) where  ld_clientcd=(select distinct cm_brkggroup from client_master with (nolock) where cm_cd='" + struserid + "') " + strYEARWhere + " ";
                    strsQL += " group by ld_dpid , ld_clientcd ";
                }

                if (GetWebParameter("Commex") != "null" && GetWebParameter("Commex") != string.Empty)
                {

                    strsQL += " union all select '" + strYear + "' Year,'CM' as account,ld_clientcd, right(rtrim(ld_dpid),1) as exch,ld_dpid, ";
                    strsQL += " cast( Sum(case When ld_dt <'" + strFromDt + "' Then ld_amount else 0 end) as decimal(15,2)) OpenBal,";
                    strsQL += " cast( Sum(case When ld_dt >= '" + strFromDt + "' and ld_debitflag = 'D' Then ld_amount else 0 end) as decimal(15,2)) Debit, ";
                    strsQL += " cast( Sum(case When ld_dt >= '" + strFromDt + "' and ld_debitflag = 'C' Then -ld_amount else 0 end) as decimal(15,2)) Credit,";
                    strsQL += " cast( Sum(ld_amount) as decimal(15,2)) Closing ,";
                    strsQL += " case substring(ld_dpid,2,1) when 'M' then 'MCX' when 'N' then 'NCDEX' when 'F' then 'NCDEX' when 'S' then 'NSEL' when 'D' then 'NSX' end + '-' + 'Comm' as heading,52  ";

                    var ArrCommex = Strings.Split(GetWebParameter("Commex"), "/");
                    strsQL += " from   [" + ArrCommex[0] + "]." + ArrCommex[1] + "." + ArrCommex[2] + ".ledger ";
                    strsQL += " where ld_clientcd='" + struserid + "'" + strYEARWhere + "";
                    strsQL += " group by ld_dpid , ld_clientcd ";

                    if (blnMAccount == true)
                    {
                        strsQL += " union all select '" + strYear + "' Year,'CX' as account,ld_clientcd, right(rtrim(ld_dpid),1) as exch,ld_dpid, ";
                        strsQL += " cast( Sum(case When ld_dt <'" + strFromDt + "' Then ld_amount else 0 end) as decimal(15,2)) OpenBal,";
                        strsQL += " cast( Sum(case When ld_dt >= '" + strFromDt + "' and ld_debitflag = 'D' Then ld_amount else 0 end) as decimal(15,2)) Debit, ";
                        strsQL += " cast( Sum(case When ld_dt >= '" + strFromDt + "' and ld_debitflag = 'C' Then -ld_amount else 0 end) as decimal(15,2)) Credit,";
                        strsQL += " cast( Sum(ld_amount) as decimal(15,2)) Closing ,";
                        strsQL += " case substring(ld_dpid,2,1) when 'M' then 'MCX' when 'N' then 'NCDEX' when 'F' then 'NCDEX' when 'S' then 'NSEL' when 'D' then 'NSX' end + '-' + 'Comm' as heading,60 ";
                        strsQL += " from   [" + ArrCommex[0] + "]." + ArrCommex[1] + "." + ArrCommex[2] + ".ledger ";
                        strsQL += " where ld_clientcd=(select distinct cm_brkggroup from [" + ArrCommex[0] + "]." + ArrCommex[1] + "." + ArrCommex[2] + ".client_master ";
                        strsQL += " where cm_cd='" + struserid + "'" + strYEARWhere + ") ";
                        strsQL += " group by ld_dpid , ld_clientcd ";
                    }
                }
            }
            else
            {
                strsQL += " select '" + strYear + "' Year,'C' as account,ld_clientcd, right(rtrim(ld_dpid),1) as exch,ld_dpid, ";
                strsQL += " cast( Sum(case When ld_dt <'" + strFromDt + "' Then ld_amount else 0 end) as decimal(15,2)) OpenBal,";
                strsQL += " cast( Sum(case When ld_dt >= '" + strFromDt + "' and ld_debitflag = 'D' Then ld_amount else 0 end) as decimal(15,2)) Debit, ";
                strsQL += " cast( Sum(case When ld_dt >= '" + strFromDt + "' and ld_debitflag = 'C' Then -ld_amount else 0 end) as decimal(15,2)) Credit,";
                strsQL += " cast( Sum(ld_amount) as decimal(15,2)) Closing ,";
                strsQL += " case substring(ld_dpid,2,1) when 'B' then 'BSE'  when 'N' then 'NSE' when 'M' then 'MCX' end + '-' + case substring(ld_dpid,3,1) when 'C' then 'Cash' when 'F' then 'DERIVATIVE' when 'K' then 'FX' end  heading, ";
                strsQL += " case substring(ld_dpid,3,1) when 'C' then 1 else 2 end +case substring(ld_dpid,2,1) when 'B' then 10 when 'N' then 10 else 20 end Ordr ";
                strsQL += " from ledger with (nolock) where ld_clientcd='" + struserid + "'" + strYEARWhere + " and Len( Ltrim(Rtrim(ld_dpid))) <> 8 group by ld_dpid , ld_clientcd ";

                if (blnMAccount == true)
                {
                    strsQL += " union all  ";

                    strsQL += " select '" + strYear + "' Year,'M' as account, ld_clientcd, right(rtrim(ld_dpid),1) as exch,ld_dpid, ";
                    strsQL += " cast( Sum(case When ld_dt <'" + strFromDt + "' Then ld_amount else 0 end) as decimal(15,2)) OpenBal,";
                    strsQL += " cast( Sum(case When ld_dt >= '" + strFromDt + "' and ld_debitflag = 'D' Then ld_amount else 0 end) as decimal(15,2)) Debit, ";
                    strsQL += " cast( Sum(case When ld_dt >= '" + strFromDt + "' and ld_debitflag = 'C' Then -ld_amount else 0 end) as decimal(15,2)) Credit,";
                    strsQL += " cast( Sum(ld_amount) as decimal(15,2)) Closing ,";
                    strsQL += " case substring(ld_dpid,2,1) When 'B' Then 'BSE' When 'N' Then 'NSE' When 'M' Then 'MCX' else '' end + '-' + case substring(ld_dpid,3,1) When 'C' Then 'CASH' When 'F' Then 'DERIVATIVE' When 'K' Then 'FX' else '' end + '(M)'  heading,   ";
                    strsQL += " case substring(ld_dpid,3,1) when 'C' then 1 else 2 end + case substring(ld_dpid,2,1) when 'B' then 30 when 'N' then 30 else 40 end";
                    strsQL += " from ledger with (nolock) where  ld_clientcd = (select distinct cm_brkggroup from client_master with (nolock) where cm_cd='" + struserid + "')" + strYEARWhere + " and ";
                    strsQL += " Len( Ltrim(Rtrim(ld_dpid))) <> 8 group by ld_dpid , ld_clientcd ";
                }

                strsQL += " union all ";

                strsQL += " select '" + strYear + "' Year,'CM' as account, ld_clientcd, right(rtrim(ld_dpid),1) as exch,ld_dpid, ";

                strsQL += " cast( Sum(case When ld_dt <'" + strFromDt + "' Then ld_amount else 0 end) as decimal(15,2)) OpenBal,";
                strsQL += " cast( Sum(case When ld_dt >= '" + strFromDt + "' and ld_debitflag = 'D' Then ld_amount else 0 end) as decimal(15,2)) Debit, ";
                strsQL += " cast( Sum(case When ld_dt >= '" + strFromDt + "' and ld_debitflag = 'C' Then -ld_amount else 0 end) as decimal(15,2)) Credit,";
                strsQL += " cast( Sum(ld_amount) as decimal(15,2)) Closing ,";

                strsQL += " case substring(ld_dpid,2,1) when 'M' then 'MCX' when 'N' then 'NCDEX' when 'F' then 'NCDEX' when 'S' then 'NSEL' when 'D' then 'NSX' end + '-' + 'Comm' as heading,  ";
                strsQL += " 52 from Cledger with (nolock) ";
                strsQL += " where ld_clientcd='" + struserid + "'" + strYEARWhere + " group by ld_dpid , ld_clientcd ";
                if (blnMAccount == true)
                {
                    strsQL += " union all ";
                    strsQL += " select '" + strYear + "' Year,'CX' as account, ld_clientcd, right(rtrim(ld_dpid),1) as exch,ld_dpid, ";
                    strsQL += " cast( Sum(case When ld_dt <'" + strFromDt + "' Then ld_amount else 0 end) as decimal(15,2)) OpenBal,";
                    strsQL += " cast( Sum(case When ld_dt >= '" + strFromDt + "' and ld_debitflag = 'D' Then ld_amount else 0 end) as decimal(15,2)) Debit, ";
                    strsQL += " cast( Sum(case When ld_dt >= '" + strFromDt + "' and ld_debitflag = 'C' Then -ld_amount else 0 end) as decimal(15,2)) Credit,";
                    strsQL += " cast( Sum(ld_amount) as decimal(15,2)) Closing ,";
                    strsQL += " case substring(ld_dpid,2,1) when 'M' then 'MCX(M)' when 'N' then 'NCDEX(M)'  when 'F' then 'NCDEX(M)' when 'S' then 'NSEL(M)' when 'D' then 'NSX(M)' end + '-' + 'Comm' as heading,  ";
                    strsQL += " 60 from Cledger with (nolock) where  ld_clientcd=(select distinct cm_brkggroup from client_master with (nolock) where cm_cd='" + struserid + "')" + strYEARWhere + " ";
                    strsQL += " group by ld_dpid , ld_clientcd ";
                    strsQL += " ";
                }
            }
            strsQL += " ) a where (abs(OpenBal)+abs(Debit)+abs(Credit)) > 0 order by Ordr,account,ld_clientcd,exch,ld_dpid ";
            return strsQL;
        }

        public string fnGetLedgerYearSQL(string struserid)
        {
            string strsQL, strCommex = "";
            if (GetWebParameter("IsTradeWeb") == "O")
            {
                strCommex = GetWebParameter("Commex");
            }
            if (GetWebParameter("IsTradeWeb") == "O")
            {
                strsQL = " select distinct '20'+LEFT(ld_accYear ,2) + '-' + SUBSTRING(ld_accYear ,5,2) cYEar From Ledger where ld_dt <= Convert(char(8),getdate(),112) ";
                if (strCommex != "null" & strCommex != string.Empty)
                {
                    strsQL += " union select distinct '20'+LEFT(ld_accYear ,2) + '-' + SUBSTRING(ld_accYear ,5,2) ";
                    var ArrCommex = Strings.Split(strCommex, "/");
                    strsQL += " from   " + ArrCommex[0] + "." + ArrCommex[1] + "." + ArrCommex[2] + ".ledger with (nolock) where ld_dt <= Convert(char(8),getdate(),112) ";
                }
            }
            else
            {
                strsQL = " select distinct '20'+LEFT(ld_accYear ,2) + '-' + SUBSTRING(ld_accYear ,5,2) cYEar From Ledger where ld_dt <= Convert(char(8),getdate(),112) ";
                strsQL += " union select distinct '20'+LEFT(ld_accYear ,2) + '-' + SUBSTRING(ld_accYear ,5,2) From CLedger where ld_dt <= Convert(char(8),getdate(),112) ";
            }
            strsQL += " Order by cYear desc ";
            return strsQL;
        }

        public bool mfnGetKey()
        {
            bool mfnGetKeyRet = default;
            mfnGetKeyRet = false;
            if (objUtility.fnFireQuery("SysObjects", "count(0)", "name", "sysTable", true) == "0")
            {
                mfnGetKeyRet = false;
                return false;
            }
            if (GetWebParameter("IsTradeWeb") == "O")
            {
                int Query = Convert.ToInt32(objUtility.fnFireQuery("Entity_master", "count(0)", "em_bclearingno = '74' Or em_nclearingno", "11420", true));
                if (mfnGetSysSplFeature("TMB") || Query > 0)
                {
                    mfnGetKeyRet = true;
                }
            }
            else if (mfnGetSysSplFeature("TMB") == true)
            {
                mfnGetKeyRet = true;
            }

            return mfnGetKeyRet;
        }

        public bool mfnGetSysSplFeature(string strKeyCode)
        {
            bool mfnGetSysSplFeatureRet = default;
            string strsql;
            string strcomname;
            var DTfind = new DataTable();
            mfnGetSysSplFeatureRet = false;
            strsql = "select st_KeyCode ,st_KeyVal From sysTable Where st_KeyCode  = '" + strKeyCode + "'";
            DTfind = objUtility.OpenDataTable(strsql);
            if (DTfind.Rows.Count > 0)
            {
                if (Convert.ToDouble(Strings.Trim(objUtility.fnFireQuery("Entity_master", "count(0)", " em_cd='B' and em_name like 'SYKES & RAY EQUITIES%' and 1", "1"))) > 0d)
                {
                    strsql = "select em_Name from Entity_master where em_cd ='B'";
                }
                else
                {
                    strsql = "select em_Name from Entity_master where em_cd =(select min(em_cd) from Entity_master)";
                }

                var dtcomp = new DataTable();
                dtcomp = objUtility.OpenDataTable(strsql);
                if (dtcomp.Rows.Count > 0)
                {
                    strcomname = Strings.Trim(Strings.Left(dtcomp.Rows[0]["em_Name"].ToString().ToUpper(), 20));
                    if (Decrypt(DTfind.Rows[0]["st_KeyVal"].ToString()) == Strings.UCase(strKeyCode + strcomname))
                    {
                        mfnGetSysSplFeatureRet = true;
                    }
                }
            }

            return mfnGetSysSplFeatureRet;
        }

        public string Decrypt(string strdec)
        {
            string DecryptRet = default;
            long m;
            string strEncKey, gsFinal, glNumber, gsEcDc, gsCompare;
            strEncKey = "ASHOKKHE";
            gsFinal = "";
            glNumber = Strings.Len(Strings.Trim(strdec)).ToString();
            var loopTo = (long)Math.Round(Math.Round(Convert.ToDouble(glNumber) / 8d) + 1d);
            for (m = 1L; m <= loopTo; m++)
                strEncKey = strEncKey + strEncKey;
            var loopTo1 = Convert.ToDouble(glNumber);
            for (m = 1L; m <= loopTo1; m++)
            {
                gsEcDc = Strings.Mid(strdec, (int)m, 1);
                gsCompare = Strings.Mid(strEncKey, (int)m, 1);
                gsFinal = gsFinal + Convert.ToString(Strings.Chr(Strings.Asc(gsEcDc) - Strings.Asc(gsCompare) - 13));
            }
            DecryptRet = gsFinal;
            return DecryptRet;
        }

        public string fnGetLedgerDetailSQL(string struserid, string strProduct, string strDPID, string strFromDt, string strToDt, bool blnDataSet = true, bool blnMAccount = false)
        {
            bool blnTplus = false;
            bool blnComm = false;
            string strCurDpid = "";
            if (!string.IsNullOrEmpty(strDPID))
            {
                string dpid = "";
                for (int i = 0, loopTo = Information.UBound(strDPID.Split(",")); i <= loopTo; i++)
                {
                    if (!string.IsNullOrEmpty(Strings.Trim(strDPID.Split(",")[i])))
                    {
                        dpid = strDPID.Split(",")[i];
                        if (dpid.Split("-")[1].ToUpper() == "CASH" || Strings.Left(dpid.Split("-")[1].ToUpper(), 10) == "DERIVATIVE" || dpid.Split("-")[1].ToUpper() == "FX" || dpid.Split("-")[1].ToUpper() == "MF" || dpid.Split("-")[1].ToUpper() == "NF")
                        {
                            blnTplus = true;
                        }

                        if (dpid.Split("-")[1].ToUpper() == "COMM")
                        {
                            blnComm = true;
                            if (objUtility.mfnGetSysSplFeature("TCM") == true)
                            {
                                blnTplus = true;
                                blnComm = false;
                            }
                        }

                        if (Information.UBound(dpid.Split("-")) > 0)
                        {
                            if (dpid.Split("-")[1].ToUpper().Trim() == "COMM")
                            {
                                if (dpid.Split("-")[0].ToUpper().Trim() == "NCDEX")
                                {
                                    string strExchange = Convert.ToString(Interaction.IIf(Strings.Trim(objUtility.GetSysParmStComm("CHGNCDEXCD", "")) == "Y", "F", "N"));
                                    dpid = strExchange + "X";
                                }
                                else if (dpid.Split("-")[0].ToUpper().Trim() == "BSE")
                                {
                                    dpid = "BX";
                                }
                                else
                                {
                                    dpid = "M" + Convert.ToString(Interaction.IIf(objUtility.mfnGetSysSplFeature("TCM"), "X", "F"));
                                }
                            }
                            else
                            {
                                //dpid = Convert.ToString((Strings.Left(dpid.Split("-")[0], 1), Interaction.IIf(dpid.Split("-")[1].ToUpper() == "CASH", "C", Interaction.IIf(dpid.Split("-")[1].ToUpper() == "FX", "K", Interaction.IIf(dpid.Split("-")[1].ToUpper() == "MF", "M", "F")))));
                                dpid = Strings.Left(dpid.Split("-")[0], 1) + (dpid.Split("-")[1].ToUpper() == "CASH" ? "C" : (dpid.Split("-")[1].ToUpper() == "FX" ? "K" : (dpid.Split("-")[1].ToUpper() == "MF" ? "M" : "F")));
                            }
                        }
                        else
                        {
                            dpid = Strings.Left(dpid.Split("-")[0], 1) + "F";
                        }
                        strCurDpid += "'" + dpid + "',";
                    }
                }
                if (Strings.Right(strCurDpid, 1) == ",")
                {
                    strCurDpid = Strings.Left(strCurDpid, Strings.Len(strCurDpid) - 1);
                }
            }
            else if (string.IsNullOrEmpty(strProduct.ToUpper()))
            {
                blnTplus = true;
                blnComm = true;
            }
            else if (strProduct.ToUpper() == "T")
            {
                blnTplus = true;
                blnComm = false;
            }
            else if (strProduct.ToUpper() == "X")
            {
                blnTplus = false;
                blnComm = true;
            }

            string strTable = " Ledger ";
            string strCommTable = string.Empty;
            string strCommClientMaster = string.Empty;
            string strSql = "";

            if (objUtility.GetWebParameter("IsTradeWeb") == "O")
            {
                if (objUtility.GetWebParameter("Commex") != "null" & objUtility.GetWebParameter("Commex") != string.Empty)
                {
                    var ArrCommex = Strings.Split(objUtility.GetWebParameter("Commex"), "/");
                    strCommTable = "[" + ArrCommex[0] + "].[" + ArrCommex[1] + "].[" + ArrCommex[2] + "].[ledger]";
                    strCommClientMaster = "[" + ArrCommex[0] + "].[" + ArrCommex[1] + "].[" + ArrCommex[2] + "].[Client_master]";
                }
            }
            else
            {
                strCommTable = "Cledger";
                strCommClientMaster = "Client_master";
            }

            if (string.IsNullOrEmpty(strFromDt))
            {
                if (DateTime.Now.Day > 15)
                {
                    strFromDt = Strings.Format(DateTime.Now, "yyyyMM") + "01";
                }
                else
                {
                    strFromDt = Strings.Format(DateTime.Now.AddMonths(-1), "yyyyMM") + "01";
                }
            }
            strToDt = Convert.ToString(Interaction.IIf(string.IsNullOrEmpty(strToDt), Strings.Format(DateTime.Now, "yyyyMMdd"), strToDt));
            // 'Ledger------------------------    
            if (blnTplus)
            {
                strSql = " select substring(ld_dpid,2,2) as ExCode, ";
                strSql += " case substring(ld_dpid,2,1) when 'B' then 'BSE'  when 'N' then 'NSE' when 'M' then 'MCX' when 'F' then 'NCDEX' end + '-' + case substring(ld_dpid,3,1) when 'C' then 'Cash' when 'F' then 'DERIVATIVE' when 'K' then 'FX'  when 'M' then 'MF' when 'X' then 'COMM' end ExchangeTitle,";
                strSql += " 'C' as acc,convert(char,convert(datetime,'" + strFromDt + "'),103) ld_dt, ";
                strSql += " 'Opening Balance' ld_particular,'O' ld_documenttype, ''ld_common,'" + strFromDt + "' Ldate , ";
                strSql += " cast( case When Sum(case When ld_dt <= '" + strFromDt + "' Then ld_amount else 0 end) > 0 Then Sum(case When ld_dt <= '" + strFromDt + "' Then ld_amount else 0 end) else 0 end as decimal(15,2)) Debit, ";
                strSql += " cast( case When Sum(case When ld_dt <= '" + strFromDt + "' Then ld_amount else 0 end) > 0 Then 0 else Sum(case When ld_dt <= '" + strFromDt + "' Then -ld_amount else 0 end) end as decimal(15,2)) Credit , '' Balance ";
                strSql += " from " + strTable + " with (nolock) ";
                strSql += " where ld_clientcd = '" + struserid + "'  ";
                strSql += " and ld_dt < '" + strFromDt + "' ";
                if (!string.IsNullOrEmpty(strCurDpid))
                {
                    strSql += "and right(rtrim(ld_dpid),2)in (" + strCurDpid + ")";
                }
                strSql += " group by ld_clientcd, ld_dpid having sum(ld_amount)<> 0 ";
                strSql += " union all ";
                strSql += " select substring(ld_dpid,2,2) as ExCode,";
                strSql += " case substring(ld_dpid,2,1) when 'B' then 'BSE'  when 'N' then 'NSE' when 'M' then 'MCX' when 'F' then 'NCDEX' end + '-' + case substring(ld_dpid,3,1) when 'C' then 'Cash' when 'F' then 'DERIVATIVE' when 'K' then 'FX'  when 'M' then 'MF' when 'X' then 'COMM' end ExchangeTitle,";
                strSql += @" 'C',ltrim(rtrim(convert(char,convert(datetime,ld_dt),103))) as ld_dt, Replace(ld_particular,'\','/') ld_particular, ld_documenttype, ld_common, ld_dt Ldate , ";
                strSql += " cast( case When ld_amount > 0 Then ld_amount else 0 end as decimal(15,2)) Debit , cast( case When ld_amount < 0 Then -ld_amount else 0 end as decimal(15,2)) Credit , '' Balance ";
                strSql += " from " + strTable + " with (nolock) ";
                strSql += " where ld_clientcd = '" + struserid + "'  ";
                strSql += " and ld_dt between '" + strFromDt + "' and '" + strToDt + "' ";
                if (!string.IsNullOrEmpty(strCurDpid))
                {
                    strSql += " and right(rtrim(ld_dpid),2)in (" + strCurDpid + ")";
                }
                if (blnMAccount)
                {
                    strSql += " union all ";
                    strSql += " select substring(ld_dpid,2,2) as ExCode, ";
                    strSql += " case substring(ld_dpid,2,1) when 'B' then 'BSE'  when 'N' then 'NSE' when 'M' then 'MCX' when 'F' then 'NCDEX' end + '-' + case substring(ld_dpid,3,1) when 'C' then 'Cash' when 'F' then 'DERIVATIVE' when 'K' then 'FX' when 'X' then 'COMM' end ExchangeTitle,";
                    strSql += " 'M' as acc,convert(char,convert(datetime,'" + strFromDt + "'),103) ld_dt,";
                    strSql += " 'Opening Balance' ld_particular,'O' ld_documenttype, ''ld_common,'" + strFromDt + "' Ldate , ";
                    strSql += " cast( case When Sum(case When ld_dt <= '" + strFromDt + "' Then ld_amount else 0 end) > 0 Then Sum(case When ld_dt <= '" + strFromDt + "' Then ld_amount else 0 end) else 0 end as decimal(15,2)) Debit, ";
                    strSql += " cast( case When Sum(case When ld_dt <= '" + strFromDt + "' Then ld_amount else 0 end) > 0 Then 0 else Sum(case When ld_dt <= '" + strFromDt + "' Then -ld_amount else 0 end) end as decimal(15,2)) Credit , '' Balance ";
                    strSql += " from " + strTable + " with (nolock) ";
                    strSql += " where ld_clientcd = (select distinct cm_brkggroup from client_master   ";
                    strSql += " where cm_cd='" + struserid + "') ";
                    strSql += " and ld_dt < '" + strFromDt + "' ";

                    if (!string.IsNullOrEmpty(strCurDpid))
                    {
                        strSql += " and right(rtrim(ld_dpid),2)in (" + strCurDpid + ")";
                    }

                    strSql += "group by ld_clientcd, ld_dpid having sum(ld_amount)<> 0 ";
                    strSql += " union all ";
                    strSql += " select substring(ld_dpid,2,2) as ExCode, ";
                    strSql += " case substring(ld_dpid,2,1) when 'B' then 'BSE'  when 'N' then 'NSE' when 'M' then 'MCX' when 'F' then 'NCDEX' end + '-' + case substring(ld_dpid,3,1) when 'C' then 'Cash' when 'F' then 'DERIVATIVE' when 'K' then 'FX' when 'X' then 'COMM' end ExchangeTitle, ";
                    strSql += @" 'M' ,ltrim(rtrim(convert(char,convert(datetime,ld_dt),103))) as ld_dt, Replace(ld_particular,'\','/') ld_particular, ld_documenttype, ld_common, ld_dt Ldate , ";
                    strSql += " cast( case When ld_amount > 0 Then ld_amount else 0 end as decimal(15,2)) Debit , cast( case When ld_amount < 0 Then -ld_amount else 0 end as decimal(15,2)) Credit , '' Balance ";
                    strSql += " from " + strTable + " with (nolock) ";
                    strSql += " where ld_clientcd = (select distinct cm_brkggroup from client_master ";
                    strSql += " where cm_cd='" + struserid + "' ) ";
                    strSql += " and ld_dt between '" + strFromDt + "' and '" + strToDt + "' ";
                    if (!string.IsNullOrEmpty(strCurDpid))
                    {
                        strSql += " and right(rtrim(ld_dpid),2)in (" + strCurDpid + ")";
                    }
                }
            }

            // 'Cledger===============        
            if (blnComm)
            {
                if (strSql.Trim().Length > 0)
                    strSql += " union all ";
                strSql += " select substring(ld_dpid,2,2) as ExCode, ";
                strSql += " case substring(ld_dpid,2,1) when 'M' then 'MCX' when 'N' then 'NCDEX' when 'F' then 'NCDEX' when 'S' then 'NSEL' when 'D' then 'NSX' end + '-' + 'Comm' ExchangeTitle,";
                strSql += " 'CM' as acc,Convert(char,Convert(datetime,'" + strFromDt + "'),103) ld_dt,";
                strSql += " 'Opening Balance' ld_particular,'O' ld_documenttype,''ld_common,'" + strFromDt + "' as Ldate , ";
                strSql += " cast( case When Sum(case When ld_dt <= '" + strFromDt + "' Then ld_amount else 0 end) > 0 Then Sum(case When ld_dt <= '" + strFromDt + "' Then ld_amount else 0 end) else 0 end as decimal(15,2)) Debit, ";
                strSql += " cast( case When Sum(case When ld_dt <= '" + strFromDt + "' Then ld_amount else 0 end) > 0 Then 0 else Sum(case When ld_dt <= '" + strFromDt + "' Then -ld_amount else 0 end) end as decimal(15,2)) Credit , '' Balance ";
                strSql += " from " + strCommTable + " ";
                strSql += " where ld_clientcd = '" + struserid + "' and ld_dt < '" + strFromDt + "' ";
                if (!string.IsNullOrEmpty(strCurDpid))
                {
                    strSql += " and right(rtrim(ld_dpid),2) in (" + strCurDpid + ") ";
                }
                strSql += " group by ld_clientcd, ld_dpid having sum(ld_amount)<> 0 ";
                strSql += " union all ";
                strSql += " select substring(ld_dpid,2,2) as ExCode, ";
                strSql += " case substring(ld_dpid,2,1) when 'M' then 'MCX' when 'N' then 'NCDEX' when 'F' then 'NCDEX' when 'S' then 'NSEL' when 'D' then 'NSX' end + '-' + 'Comm' ExchCommTitle,'CM', ";
                strSql += @" ltrim(rtrim(convert(char,convert(datetime,ld_dt),103))) as ld_dt,Replace(ld_particular,'\','/') ld_particular,ld_documenttype,ld_common,ld_dt Ldate,";
                strSql += " cast( case When ld_amount > 0 Then ld_amount else 0 end as decimal(15,2)) Debit , cast( case When ld_amount < 0 Then -ld_amount else 0 end as decimal(15,2)) Credit , '' Balance ";
                strSql += " from " + strCommTable + " ";
                strSql += " where ld_clientcd = '" + struserid + "' ";
                strSql += " and ld_dt between '" + strFromDt + "' and '" + strToDt + "' ";
                if (!string.IsNullOrEmpty(strCurDpid))
                {
                    strSql += " and right(rtrim(ld_dpid),2)in (" + strCurDpid + ")";
                }

                if (blnMAccount)
                {
                    if (strSql.Trim().Length > 0)
                        strSql += " union all ";
                    strSql += " select substring(ld_dpid,2,2) as ExCode, ";
                    strSql += " case substring(ld_dpid,2,1) when 'M' then 'MCX' when 'N' then 'NCDEX' when 'F' then 'NCDEX' when 'S' then 'NSEL' when 'D' then 'NSX' end + '-' + 'Comm' ExchangeTitle,";
                    strSql += " 'CX' as acc,Convert(char,Convert(datetime,'" + strFromDt + "'),103) ld_dt,";
                    strSql += " 'Opening Balance' ld_particular,'O' ld_documenttype,''ld_common,'" + strFromDt + "' as Ldate, ";
                    strSql += " cast( case When Sum(case When ld_dt <= '" + strFromDt + "' Then ld_amount else 0 end) > 0 Then Sum(case When ld_dt <= '" + strFromDt + "' Then ld_amount else 0 end) else 0 end as decimal(15,2)) Debit, ";
                    strSql += " cast( case When Sum(case When ld_dt <= '" + strFromDt + "' Then ld_amount else 0 end) > 0 Then 0 else Sum(case When ld_dt <= '" + strFromDt + "' Then -ld_amount else 0 end) end as decimal(15,2)) Credit , '' Balance ";
                    strSql += " from " + strCommTable + " where  ld_clientcd=(select distinct cm_brkggroup from " + strCommClientMaster + " where cm_cd='" + struserid + "' ";
                    strSql += " ) and ld_dt < '" + strFromDt + "' ";
                    if (!string.IsNullOrEmpty(strCurDpid))
                    {
                        strSql += " and right(rtrim(ld_dpid),2)in (" + strCurDpid + ")";
                    }
                    strSql += " group by ld_clientcd, ld_dpid having sum(ld_amount)<> 0 ";
                    strSql += " union all ";
                    strSql += " select substring(ld_dpid,2,2) as ExCode, ";
                    strSql += " case substring(ld_dpid,2,1) when 'M' then 'MCX' when 'N' then 'NCDEX' when 'F' then 'NCDEX' when 'S' then 'NSEL' when 'D' then 'NSX' end + '-' + 'Comm' ExchCommTitle,'CX', ";
                    strSql += @" ltrim(rtrim(convert(char,convert(datetime,ld_dt),103))) as ld_dt ,Replace(ld_particular,'\','/') ld_particular,ld_documenttype,ld_common,ld_dt Ldate,";
                    strSql += " cast( case When ld_amount > 0 Then ld_amount else 0 end as decimal(15,2)) Debit , cast( case When ld_amount < 0 Then -ld_amount else 0 end as decimal(15,2)) Credit , '' Balance ";
                    strSql += " from " + strCommTable + " where  ld_clientcd=(select distinct cm_brkggroup from " + strCommClientMaster + "  ";
                    strSql += " where cm_cd='" + struserid + "'";
                    strSql += " ) and ld_dt between '" + strFromDt + "' and '" + strToDt + "' ";
                    if (!string.IsNullOrEmpty(strCurDpid))
                    {
                        strSql += " and right(rtrim(ld_dpid),2)in (" + strCurDpid + ")";
                    }
                }
            }
            strSql += " order by Ldate desc";
            return strSql;
        }

        public string PrIProcessBenHolding(SqlConnection objConnection, SqlTransaction objTrans, string strUserid)
        {
            string strstat = string.Empty;
            string strcollat = "";
            string strsql = "";
            var dtTable = new DataTable();
            strsql = "select st_sysvalue from stationary with (nolock) where st_parmcd='DMTCOLLATDP' ";
            dtTable = OpenDataTableTemp(strsql, objConnection, objTrans);
            if (dtTable.Rows.Count != 0)
                strstat = dtTable.Rows[0][0].ToString().Trim();
            var arrstat = Strings.Split(strstat, ",");
            strcollat = "( ";
            for (int ir = 0, loopTo = arrstat.Length - 1; ir <= loopTo; ir++)
                strcollat += "'" + arrstat[ir] + "',";
            strcollat += ")";
            strcollat = strcollat.Replace(",)", ")");

            try
            {
                ExecuteSQLTemp("Create Table #TmpVarMargin (Tmp_scripcd VarChar(6), Tmp_haircut money)", objConnection, objTrans);
            }
            catch (Exception ex)
            {
                ExecuteSQLTemp("Drop table  #TmpVarMargin", objConnection, objTrans);
                ExecuteSQLTemp("Create Table #TmpVarMargin (Tmp_scripcd VarChar(6), Tmp_haircut money)", objConnection, objTrans);
            }

            ExecuteSQLTemp("Create index #idx_TmpVarMargin_scripcd on #TmpVarMargin (Tmp_scripcd)", objConnection, objTrans);
            ExecuteSQLTemp("insert into #TmpVarMargin select ss_cd , 0 From securities with (nolock)", objConnection, objTrans);

            strsql = " update #TmpVarMargin set Tmp_haircut = case When lv_BseDt > lv_NseDt Then lv_BseVarMargin else lv_NseVarMargin end from LVarMargin with (nolock) where lv_Scripcd = Tmp_scripcd ";
            ExecuteSQLTemp(strsql, objConnection, objTrans);
            return strcollat;
        }

        public DataTable OpenDataTableTemp(string strQRY, SqlConnection objConnection, SqlTransaction objTrans)
        {
            using (var myCMD = new SqlCommand())
            {
                myCMD.Connection = objConnection;
                myCMD.CommandText = strQRY;
                myCMD.Transaction = objTrans;
                myCMD.CommandTimeout = 5000;
                var myDStable = new DataTable();
                using (var myAdapt = new SqlDataAdapter(myCMD))
                {
                    myAdapt.Fill(myDStable);
                }
                return myDStable;
            }
        }

        public string ExecuteSQLTemp(string Str_Qry, SqlConnection objConnection, SqlTransaction objTrans)
        {
            using (var myCMD = new SqlCommand())
            {
                if (objConnection.State == ConnectionState.Closed)
                {
                    objConnection.Open();
                }
                myCMD.Connection = objConnection;
                myCMD.CommandTimeout = 5000;
                myCMD.CommandText = Str_Qry;
                myCMD.Transaction = objTrans;
                myCMD.ExecuteNonQuery();
                return "";
            }
        }

        public string FnGetBenHolding(SqlConnection objConnection, SqlTransaction objTrans, string strcollat, string strUserid, string strType = "")
        {
            string strSQL;
            string strdate = OpenDataTableTemp("select convert(char,GetDate(),112)", objConnection, objTrans).Rows[0][0].ToString().Trim();
            ExecuteSQLTemp("IF OBJECT_ID('tempdb..#demat') IS NOT NULL DROP TABLE #demat", objConnection, objTrans);
            strSQL = "Create Table #demat ( dm_clientcd varchar(8) Not Null , dm_scripcd varchar(6) Not Null , dm_isin  varchar(12) Not Null , dm_ssname varchar(24) Not Null , dm_stlmnt varchar(9) Not Null , dm_rate money Not Null , dm_valuation money Not Null , dm_Qty Numeric Not Null , dm_type varchar(25) Not Null) ";
            ExecuteSQLTemp(strSQL, objConnection, objTrans);

            DataTable DsMPTrx;
            DsMPTrx = objUtility.OpenDataTable("select * from SysObjects where name= 'MrgPledge_TRX'");
            strSQL = "Insert Into #demat select dm_clientcd,dm_scripcd,dm_isin,ss_name,se_stlmnt,cast( ss_bserate as decimal(15,2)) bh_bserate, cast(convert(decimal(15,2),(ss_bserate*sum(dm_qty))) as decimal(15,2)) valuation ,sum(dm_qty) as qty,'Beneficiary' as bh_type from demat with (nolock), ourdps with (nolock) ,settlements with (nolock) , client_master with (nolock), securities with (nolock),#TmpVarMargin where cm_cd = dm_clientcd and dm_stlmnt = se_stlmnt and dm_scripcd = ss_cd and dm_ourdp = od_cd and Tmp_scripcd = dm_scripcd and od_acttype in ('B','M') and dm_type = 'BC' and dm_locked = 'N' and dm_transfered = 'N' and dm_ourdp not in " + strcollat + " and dm_clientcd = '" + strUserid + "' group by dm_clientcd,dm_scripcd,dm_isin, ss_name,se_stlmnt,Tmp_haircut,ss_bserate having abs(sum(dm_qty)) > 0 union all select dm_clientcd,dm_scripcd,dm_isin,ss_name,se_stlmnt,cast( ss_bserate as decimal(15,2)) bh_bserate, cast( convert(decimal(15,2),(ss_bserate*sum(dm_qty))) as decimal(15,2)) as valuation ,sum(dm_qty) as qty, 'COLLAT' as bh_type from demat with (nolock), ourdps with (nolock) , settlements with (nolock) , client_master with (nolock) , securities with (nolock) ,#TmpVarMargin where cm_cd = dm_clientcd and dm_stlmnt = se_stlmnt and dm_scripcd = ss_cd and dm_ourdp = od_cd and Tmp_scripcd = dm_scripcd and od_acttype in ('B','M') and dm_type = 'BC' and dm_locked = 'N' and dm_transfered = 'N' and dm_ourdp in " + strcollat + " and dm_clientcd = '" + strUserid + "' group by dm_clientcd,dm_scripcd,dm_isin, ss_name,se_stlmnt,Tmp_haircut,ss_bserate having abs(sum(dm_qty)) > 0 Union all select dm_clientcd,dm_scripcd,dm_isin,ss_name,se_stlmnt, cast( ss_bserate as decimal(15,2)) bh_bserate, cast( convert(decimal(15,2),(ss_bserate*sum(dm_qty)))as decimal(15,2)) as valuation ,sum(dm_qty) as qty, 'Expected' as bh_type from demat with (nolock), ourdps with (nolock) ,settlements with (nolock) , client_master with (nolock) , securities  with (nolock) ,#TmpVarMargin where cm_cd = dm_clientcd and dm_stlmnt = se_stlmnt and dm_scripcd = ss_cd and dm_ourdp = od_cd and od_acttype = 'P'and dm_type = 'BC' and dm_locked = 'N' and dm_transfered <> 'S' and se_shpayoutdt > '" + strdate + "' and dm_ourdp not in " + strcollat + " and Tmp_scripcd = dm_scripcd and dm_clientcd = '" + strUserid + "' group by dm_clientcd,dm_scripcd,dm_isin, ss_name,se_stlmnt,Tmp_haircut,ss_bserate having abs(sum(dm_qty)) > 0 Union all select dm_clientcd,dm_scripcd,dm_isin,ss_name,se_stlmnt, cast( ss_bserate as decimal(15,2)) bh_bserate, cast( convert(decimal(15,2),(ss_bserate*sum(dm_qty)))as decimal(15,2)) as valuation ,sum(dm_qty) as qty, 'Pool' as bh_type from demat with (nolock), ourdps with (nolock) ,settlements with (nolock) , client_master with (nolock) , securities  with (nolock) ,#TmpVarMargin where cm_cd = dm_clientcd and dm_stlmnt = se_stlmnt and dm_scripcd = ss_cd and dm_ourdp = od_cd and od_acttype = 'P'and dm_type = 'BC' and dm_locked = 'N' and  dm_transfered ='N' and se_shpayoutdt <= '" + strdate + "' and dm_ourdp not in " + strcollat + " and Tmp_scripcd = dm_scripcd and dm_clientcd = '" + strUserid + "' group by dm_clientcd,dm_scripcd,dm_isin, ss_name,se_stlmnt,Tmp_haircut,ss_bserate having abs(sum(dm_qty)) > 0 Union all select dm_clientcd,dm_scripcd,dm_isin,ss_name,se_stlmnt,cast(ss_bserate as decimal(15,2)) bh_bserate, cast( convert(decimal(15,2),(ss_bserate*sum(dm_qty)))as decimal(15,2)) as valuation ,sum(dm_qty) as qty, 'Undelivered' as bh_type from demat with (nolock), ourdps with (nolock), settlements with (nolock), client_master with (nolock), securities with (nolock),#TmpVarMargin where cm_cd = dm_clientcd and dm_stlmnt = se_stlmnt and dm_scripcd = ss_cd and dm_ourdp = od_cd and od_acttype = 'P' and dm_type = 'CB' and dm_locked = 'N' and dm_transfered <> 'S' and dm_clientcd = '" + strUserid + "' and dm_ourdp not in " + strcollat + " and Tmp_scripcd = dm_scripcd group by dm_clientcd,dm_scripcd,dm_isin, ss_name,se_stlmnt,Tmp_haircut,ss_bserate having abs(sum(dm_qty)) > 0 ";
            if (DsMPTrx.Rows.Count > 0)
            {
                string strOurdp = string.Empty;
                DataTable dt = OpenDataTableTemp("select distinct MPT_OurDP from MrgPledge_TRX Where MPT_TRXFlag ='P'", objConnection, objTrans);
                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i <= dt.Rows.Count - 1; i++)
                    {
                        strOurdp += "'" + dt.Rows[i]["MPT_OurDP"].ToString().Trim() + "',";
                    }
                    strOurdp = strOurdp.Remove(strOurdp.Length - 1);
                }
                else
                {
                    strOurdp = "''";
                }

                strSQL += " union all";
                strSQL += " select MPT_clientcd dm_clientcd,MPT_scripcd dm_scripcd,im_isin,ss_name,'',cast( ss_bserate as decimal(15,2)) bh_bserate,";
                strSQL += " (ss_bserate*sum(case When MPT_DRCR ='C' Then MPT_Qty else -MPT_Qty  end)) as valuation,sum(case When MPT_DRCR ='C' Then MPT_Qty else -MPT_Qty  end) as qty,'Margin Pledge' bh_type";
                strSQL += " from MrgPledge_TRX a,securities with (nolock),#TmpVarMargin ,Isin with (nolock)";
                strSQL += " where im_scripcd = ss_cd and im_active = 'Y' and im_priority = (select min(im_priority) from Isin Where im_scripcd = Tmp_scripcd and im_active = 'Y' )  and Tmp_scripcd = im_scripcd  and  Tmp_scripcd = MPT_scripcd";
                strSQL += " and MPT_clientcd = '" + strUserid + "' and MPT_OurDP in ( " + strOurdp + ")";
                strSQL += " group by MPT_clientcd,MPT_scripcd,im_isin,ss_name,ss_bserate,Tmp_haircut";
                strSQL += " having abs(sum(case When MPT_DRCR ='C' Then MPT_Qty else -MPT_Qty  end)) > 0";
                strSQL += " union all";
                strSQL += " select MPT_clientcd dm_clientcd,MPT_scripcd dm_scripcd,im_isin,ss_name,'',cast( ss_bserate as decimal(15,2)) bh_bserate,";
                strSQL += " (ss_bserate*sum(case When MPT_DRCR ='D' Then MPT_Qty else -MPT_Qty  end)) as valuation,sum(case When MPT_DRCR ='D' Then MPT_Qty else -MPT_Qty  end) as qty,'Margin Pledge' bh_type";
                strSQL += " from MrgPledge_TRX,securities with (nolock),#TmpVarMargin ,Isin with (nolock)";
                strSQL += " where im_scripcd = ss_cd and im_active = 'Y'  and im_priority = (select min(im_priority) from Isin Where im_scripcd = Tmp_scripcd and im_active = 'Y' ) and Tmp_scripcd = im_scripcd  and  Tmp_scripcd = MPT_scripcd and MPT_TRXFlag = 'R'";
                strSQL += " and MPT_clientcd = '" + strUserid + "' and MPT_OurDP in ( " + strOurdp + ")";
                strSQL += " group by MPT_clientcd,MPT_scripcd,im_isin,ss_name,ss_bserate,Tmp_haircut";
                strSQL += " having abs(Sum(case MPT_DRCR when 'D' Then MPT_Qty else -MPT_Qty end)) > 0";
            }

            DsMPTrx = objUtility.OpenDataTable("select * from SysObjects where name= 'CUSAPledge_TRX'");
            if (DsMPTrx.Rows.Count > 0)
            {
                string strOurdp = string.Empty;
                DataTable dt = OpenDataTableTemp("select distinct CUP_OurDP from CUSAPledge_TRX Where CUP_TRXFlag ='P'", objConnection, objTrans);
                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i <= dt.Rows.Count - 1; i++)
                    {
                        strOurdp += "'" + dt.Rows[i]["CUP_OurDP"].ToString().Trim() + "',";
                    }
                    strOurdp = strOurdp.Remove(strOurdp.Length - 1);
                }
                else
                {
                    strOurdp = "''";
                }

                strSQL += " union all";
                strSQL += " select CUP_clientcd dm_clientcd,CUP_scripcd dm_scripcd,im_isin,ss_name,'',cast( ss_bserate as decimal(15,2)) bh_bserate,";
                strSQL += " (ss_bserate*sum((case When CUP_DRCR ='C' Then CUP_Qty else -CUP_Qty  end) * (-1))) as valuation,sum((case When CUP_DRCR ='C' Then CUP_Qty else -CUP_Qty  end) * (-1)) as qty,'CUSPA Pledge' bh_type";
                strSQL += " from CUSAPledge_TRX a,securities with (nolock),#TmpVarMargin ,Isin with (nolock)";
                strSQL += " where im_scripcd = ss_cd and im_active = 'Y' and im_priority = (select min(im_priority) from Isin Where im_scripcd = Tmp_scripcd and im_active = 'Y' )  and Tmp_scripcd = im_scripcd  and  Tmp_scripcd = CUP_scripcd";
                strSQL += " and CUP_clientcd = '" + strUserid + "' and CUP_OurDP in ( " + strOurdp + ")";
                strSQL += " group by CUP_clientcd,CUP_scripcd,im_isin,ss_name,ss_bserate,Tmp_haircut";
                strSQL += " having abs(sum(case When CUP_DRCR ='C' Then CUP_Qty else -CUP_Qty  end)) > 0";
                //strSQL += " union all";
                //strSQL += " select CUP_clientcd dm_clientcd,CUP_scripcd dm_scripcd,im_isin,ss_name,'',cast( ss_bserate as decimal(15,2)) bh_bserate,";
                //strSQL += " (ss_bserate*sum((case When CUP_DRCR ='C' Then CUP_Qty else -CUP_Qty  end) * (-1))) as valuation,sum((case When CUP_DRCR ='C' Then CUP_Qty else -CUP_Qty  end) * (-1)) as qty,'CUSPA Pledge' bh_type";
                //strSQL += " from CUSAPledge_TRX,securities with (nolock),#TmpVarMargin ,Isin with (nolock)";
                //strSQL += " where im_scripcd = ss_cd and im_active = 'Y'  and im_priority = (select min(im_priority) from Isin Where im_scripcd = Tmp_scripcd and im_active = 'Y' ) and Tmp_scripcd = im_scripcd  and  Tmp_scripcd = CUP_scripcd and CUP_TRXFlag = 'R'";
                //strSQL += " and CUP_clientcd = '" + strUserid + "' and exists ( select CUP_OurDP from CUSAPledge_TRX b Where CUP_TRXFlag ='P' and CUP_OurDP = b.CUP_OurDP )";
                //strSQL += " group by CUP_clientcd,CUP_scripcd,im_isin,ss_name,ss_bserate,Tmp_haircut";
                //strSQL += " having abs(Sum(case CUP_DRCR when 'D' Then CUP_Qty else -CUP_Qty end)) > 0";
            }
            ExecuteSQLTemp(strSQL, objConnection, objTrans);


            if (strType == "SP")
                strSQL = "select dm_scripcd as bh_scripcd, dm_isin as bh_isin, dm_ssname as bh_Scripname,(dm_qty*-1) bh_qty, cast((dm_valuation*-1 )as decimal(15,0))bh_valuation, isNull((select sum(Rq_Qty) From SharesRequest Where Rq_Clientcd = dm_clientcd and Rq_Scripcd=dm_scripcd and Rq_Satus1 = 'P'),0) ReqQty from #demat where dm_type not in ('UNDEL') order by dm_ssname ";
            else
                strSQL = " select dm_scripcd,dm_isin,dm_ssname as ss_name,dm_stlmnt as  se_stlmnt,cast( dm_rate as decimal(15,2)) bh_bserate, convert(decimal(15,2), dm_valuation) valuation , dm_qty as qty, dm_type as bh_type from #demat ";

            return strSQL;
        }

        #endregion
    }
}
