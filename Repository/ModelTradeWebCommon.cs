using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using Newtonsoft.Json;

//using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TradeWeb.API.Data;
using TradeWeb.API.ExtentionMethod;
using TradeWeb.API.Models;

namespace TradeWeb.API.Repository
{
    public class ModelTradeWebCommon
    {
        public string strSQL;
        public bool blnAuthorise = false;
        private readonly UtilityCommon utility;

        public ModelTradeWebCommon(UtilityCommon utility)
        {
            this.utility = utility;
        }

        public bool mfnGetSysSplFeature(string strKeyCode, SqlConnection ObjConn)
        {
            string strSysSpl = "";
            DataTable dtspl = new DataTable();

            strSysSpl = "Select count(0) From sysobjects (NoLock) Where name = 'sysTable'";
            dtspl = utility.OpenDataTable(strSysSpl, ObjConn);
            if (Conversion.Val(dtspl.Rows[0][0].ToString()) == 0)
            {
                return false;
            }

            strSysSpl = "select st_KeyCode , st_KeyVal From sysTable (NoLock) Where st_KeyCode  = '" + strKeyCode + "'";
            dtspl = utility.OpenDataTable(strSysSpl, ObjConn);
            if (dtspl.Rows.Count > 0)
            {
                if (strKeyCode == "TPI")
                    strSysSpl = "select em_name from Entity_master (NoLock) Where em_cd in (select MIN(em_cd) from Entity_master (NoLock))";
                else
                    strSysSpl = "select sp_sysValue from sysParameter (NoLock) where sp_parmcd = 'NAME'";
                DataTable dtEnt = utility.OpenDataTable(strSysSpl, ObjConn);
                if (dtEnt.Rows.Count > 0)
                {
                    string compname = Strings.Left(dtEnt.Rows[0][0].ToString(), 20).Trim();
                    if (utility.Decrypt(dtspl.Rows[0]["st_KeyVal"].ToString().Trim()) == (strKeyCode + compname).ToUpper().Trim())
                    {
                        return true;
                    }
                }
            }
            return false;
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

        public bool isValidUID(string Value)
        {
            bool isNumber = long.TryParse(Value, out long a);
            if (Value.Trim().Length < 12 || !isNumber)
            {
                return false;
            }
            return true;
        }

        public bool isValidPAN(string Value)
        {
            if (Value.Trim().Length < 10)
            {
                return false;
            }
            Regex regex = new Regex("^[a-zA-Z]{5}[0-9]{4}[a-zA-Z]{1}$");
            Match match = regex.Match(Value);
            return match.Success;
        }

        public bool IsValidEmail(string Value)
        {
            bool isEmail = Regex.IsMatch(Value, @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z", RegexOptions.IgnoreCase);
            return isEmail;
        }

        public TplusResponse ValidateAPIKey()
        {
            var db = new DataContext();

            using (SqlConnection ObjConnection = new SqlConnection((db.Database.GetDbConnection()).ConnectionString))
            {
                ObjConnection.Open();
                if (!mfnGetSysSplFeature("TPI", ObjConnection))
                {
                    TplusResponse r1 = new TplusResponse()
                    {
                        ClientCode = "Invalid API Key",
                        Remark = "You are not authorise  to use TradeWeb API Service,Please contact Acer.",
                        Status = "N"
                    };
                    return r1;
                }
                else
                {
                    TplusResponse r1 = new TplusResponse()
                    {
                        ClientCode = "Valid Key",
                        Remark = "API Success",
                        Status = "Y"
                    };
                    return r1;
                }
            }
        }

        public dynamic CheckValidation(dynamic cm, string clientCode, string valUser, string refNo)
        {
            try
            {
                //var spJson = System.Text.Json.JsonSerializer.Serialize(cm);
                SqlParameter[] sqlPrm = new[]
                {
                        new SqlParameter("@i_vcJsonString",cm),
                        new SqlParameter("@i_vcClientCode",clientCode),
                        new SqlParameter("@i_vcRefNo",refNo),
                        new SqlParameter("@i_vcValidationType",valUser)
                     };
                List<OutputParamSP> spModelList = new List<OutputParamSP>();
                OutputParamSP spModel = new OutputParamSP();
                spModel.ParamValue = "@o_vcErrorFlag";
                spModel.DbType = SqlDbType.VarChar;
                spModel.Length = 1;
                spModelList.Add(spModel);
                spModel = new OutputParamSP();
                spModel.ParamValue = "@o_vcErrorMessage";
                spModel.DbType = SqlDbType.VarChar;
                spModel.Length = 500;
                spModelList.Add(spModel);
                spModel = new OutputParamSP();
                spModel.ParamValue = "@o_vcJsonOutput";
                spModel.DbType = SqlDbType.NVarChar;
                spModel.Length = 4000;
                spModelList.Add(spModel);
                var respSP = utility.Execute_SP_OuputParam("SP_ReKyc_CheckValidation", sqlPrm, spModelList);
                return respSP;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public TplusResponse ValidateUpdateClientData(FinalReKycModel cm, string valUser)
        {
            string errMesssage = "";
            string strValue = "";
            try
            {
                DataTable dtChk = new DataTable();
                TplusResponse res = new TplusResponse();
                var db = new DataContext();

                using (SqlConnection ObjConnection = new SqlConnection((db.Database.GetDbConnection()).ConnectionString))
                {
                    res = ValidateAPIKey();
                    if (res.Status == "N")
                    {
                        return res;
                    }
                    strValue = isNull(cm.PersonalDetails.ClientCode);
                    if (strValue.Trim() == "")
                    {
                        return new TplusResponse()
                        {
                            ClientCode = "",
                            Status = "N",
                            Remark = "Client Code is either Blank or Invalid"
                        };
                    }
                    //Check if client exists
                    strSQL = "Select count(0) From Client_Master (NoLock) Where cm_cd = '" + strValue.Trim() + "'";
                    dtChk = utility.OpenDataTable(strSQL, ObjConnection);
                    if (Conversion.Val(dtChk.Rows[0][0].ToString()) == 0)
                    {
                        return new TplusResponse()
                        {
                            ClientCode = "",
                            Status = "N",
                            Remark = "Invalid Client Code, Not Found in Master"
                        };
                    }

                    strValue = isNull(cm.PersonalDetails.CorrAddress1);
                    if (strValue.Trim() != "" && strValue.Trim().Length > 50)
                    {
                        errMesssage += "Length of Correspondence Address Line 1 cannot be greater than 50  ( " + strValue + " ) " + Environment.NewLine;
                    }
                    strValue = isNull(cm.PersonalDetails.CorrAddress2);
                    if (strValue.Trim() != "" && strValue.Trim().Length > 50)
                    {
                        errMesssage += "Length of Correspondence Address Line 2 cannot be greater than 50  ( " + strValue + " ) " + Environment.NewLine;
                    }
                    strValue = isNull(cm.PersonalDetails.CorrAddress3);
                    if (strValue.Trim() != "" && strValue.Trim().Length > 50)
                    {
                        errMesssage += "Length of Correspondence Address Line 3 cannot be greater than 50  ( " + strValue + " ) " + Environment.NewLine;
                    }
                    //Check State In Tradeplus
                    strValue = isNull(cm.PersonalDetails.CorrState);
                    if (strValue.Trim() != "")
                    {
                        strSQL = "Select count(0) From State_master (Nolock) Where st_State = '" + strValue + "'";
                        dtChk = utility.OpenDataTable(strSQL, ObjConnection);
                        if (Conversion.Val(dtChk.Rows[0][0].ToString()) == 0)
                        {
                            errMesssage += "Invalid Value Specified in State ( " + strValue + " ) " + Environment.NewLine;
                        }
                        //errMesssage += " State should not be blank" + Environment.NewLine;
                    }
                    //Check country In Tradeplus
                    //strValue = isNull(cm.PersonalDetails.CorrCountry);
                    //if (strValue.Trim() != "")
                    //{
                    //    strSQL = "Select count(0) From Country_master (Nolock) Where ct_name = '" + strValue + "'";
                    //    dtChk = utility.OpenDataTable(strSQL, ObjConnection);
                    //    if (Conversion.Val(dtChk.Rows[0][0].ToString()) == 0)
                    //    {
                    //        errMesssage += "Invalid Value Specified in Country ( " + strValue + " ) " + Environment.NewLine;
                    //    }
                    //}
                    strValue = isNull(cm.PersonalDetails.Email).Trim();
                    if (strValue.Trim() != "")
                    {
                        if (!IsValidEmail(strValue.Trim()))
                        {
                            errMesssage += "Invalid Value Specified in Email ( " + strValue + " ) " + Environment.NewLine;
                        }
                    }
                    strValue = isNull(cm.PersonalDetails.Mobile).Trim();
                    if (strValue.Trim() != "" && strValue.Trim().ToUpper() != "NONE")
                    {
                        if (strValue.Trim().Length < 10)
                        {
                            errMesssage += "Invalid Value Specified in Mobile ( " + strValue + " ) " + Environment.NewLine;
                        }
                    }

                    if (valUser == "checkerHH")
                    {
                        foreach (var demat in cm.DematDetails)
                        {
                            strValue = isNull(demat.DPID).Trim();
                            if (strValue.Trim() != "")
                            {
                                strSQL = "Select count(0) From DPS (Nolock) Where dp_dpid = '" + strValue.Trim() + "'";
                                dtChk = utility.OpenDataTable(strSQL, ObjConnection);
                                if (Conversion.Val(dtChk.Rows[0][0].ToString()) == 0)
                                {
                                    errMesssage += "Invalid Value Specified in DPID ( " + strValue + " ) " + Environment.NewLine;
                                }
                            }
                            strValue = isNull(demat.DPAcNo).Trim();
                            if (strValue.Trim() != "")
                            {
                                if (strValue.Trim().Length != 8 && strValue.Trim().Length != 16)
                                {
                                    errMesssage += "Invalid Value Specified in DP A/c No ( " + strValue + " ) " + Environment.NewLine;
                                }
                                strSQL = "Select count(0) from DematAct (NoLock) where ";
                                if (Strings.Left(strValue.Trim(), 2) == "IN")
                                {
                                    strSQL += " da_dpid='" + Strings.Left(strValue.Trim(), 8) + "' and da_actno='" + Strings.Right(strValue.Trim(), 8) + "' ";
                                }
                                else
                                {
                                    strSQL += " da_actno='" + strValue + "' ";
                                }
                                strSQL += " and da_status='I' ";
                                dtChk = utility.OpenDataTable(strSQL, ObjConnection);
                                if (Conversion.Val(dtChk.Rows[0][0].ToString()) > 0)
                                {
                                    errMesssage += "Demat Account is Inactive " + Environment.NewLine;
                                }
                            }
                        }

                        foreach (var bank in cm.BankDetails)
                        {
                            bool blnifsc = false;
                            strSQL = "Select count(0) from sysobjects a (NoLock), syscolumns b (NoLock) where a.id=b.id and a.name='Bankact'  and b.name='ba_ifsccode'";
                            dtChk = utility.OpenDataTable(strSQL, ObjConnection);
                            if (Conversion.Val(dtChk.Rows[0][0].ToString()) > 0)
                            {
                                blnifsc = true;
                            }

                            string bnkmicr = isNull(bank.BankMICR);
                            string bnkifsc = "";
                            if (blnifsc)
                            {
                                bnkifsc = isNull(bank.BankIFSC);
                            }
                            if (bnkmicr.Trim() != "")
                            {
                                strSQL = "Select count(0) from Bank_master (NoLock) Where bk_micr = '" + bnkmicr.Trim() + "' ";
                                if (blnifsc)
                                {
                                    strSQL += " and bk_IFCCode = '" + bnkifsc.Trim() + "' ";
                                }
                                dtChk = utility.OpenDataTable(strSQL, ObjConnection);
                                if (Conversion.Val(dtChk.Rows[0][0].ToString()) == 0)
                                {
                                    errMesssage += "Invalid Bank Details, MICR / IFSC not found in Master " + Environment.NewLine;
                                }
                            }
                            if (bank.AccountType.Trim() != "")
                            {
                                if (!"SB/CA/CC/OD/NE/NO".Contains(bank.AccountType.Trim()))
                                {
                                    errMesssage += "Invalid Bank Details (Saving A/c) not found in Master " + Environment.NewLine;
                                }
                            }
                        }
                    }

                    foreach (var nominee in cm.NomineeDetails)
                    {
                        strValue = isNull(nominee.NomineePAN).Trim();
                        if (strValue.Trim() != "")
                        {
                            if (!isValidPAN(strValue.Trim()) || strValue.Trim().Length != 10)
                            {
                                errMesssage += "Invalid Value Specified in Nominee PAN ( " + strValue + " ) " + Environment.NewLine;
                            }
                        }
                        strValue = isNull(nominee.NomineeUID).Trim();
                        if (strValue.Trim() != "" && strValue.Trim().ToUpper() != "NONE")
                        {
                            if (!isValidUID(strValue.Trim()))
                            {
                                errMesssage += "Invalid Value Specified in Nominee Aadhar ( " + strValue + " ) " + Environment.NewLine;
                            }
                        }
                        //Check State In Tradeplus
                        strValue = isNull(nominee.NomAddressState);
                        if (strValue.Trim() != "")
                        {
                            strSQL = "Select count(0) From State_master (NoLock) Where st_State = '" + strValue + "'";
                            dtChk = utility.OpenDataTable(strSQL, ObjConnection);
                            if (Conversion.Val(dtChk.Rows[0][0].ToString()) == 0)
                            {
                                errMesssage += "Invalid Value Specified in State ( " + strValue + " ) " + Environment.NewLine;
                            }
                        }
                        //Check country In Tradeplus
                        strValue = isNull(nominee.NomAddressCountry);
                        if (strValue.Trim() != "")
                        {
                            //strSQL = "Select count(0) From Country_master Where ct_name = '" + strValue + "'";
                            //dtChk = utility.OpenDataTable(strSQL, ObjConnection);
                            //if (Conversion.Val(dtChk.Rows[0][0].ToString()) == 0)
                            //{
                            //    errMesssage += "Invalid Value Specified in Country ( " + strValue + " ) " + Environment.NewLine;
                            //}
                        }
                        strValue = isNull(nominee.NomAddress1);
                        if (strValue.Trim() != "" && strValue.Trim().Length > 50)
                        {
                            errMesssage += "Length of Correspondence Address Line 1 cannot be greater than 50  ( " + strValue + " ) " + Environment.NewLine;
                        }
                        strValue = isNull(nominee.NomAddress2);
                        if (strValue.Trim() != "" && strValue.Trim().Length > 50)
                        {
                            errMesssage += "Length of Correspondence Address Line 2 cannot be greater than 50  ( " + strValue + " ) " + Environment.NewLine;
                        }
                        if (nominee.GuardianDetails != null)
                        {
                            strValue = isNull(nominee.GuardianDetails.NomGuardianPAN).Trim();
                            if (strValue.Trim() != "")
                            {
                                if ((!isValidPAN(strValue.Trim()) || strValue.Trim().Length != 10) && (!isValidUID(strValue.Trim()) || strValue.Trim().Length != 12))
                                {
                                    errMesssage += "Invalid Value Specified in Nominee Guardian PAN/UID ( " + strValue + " ) " + Environment.NewLine;
                                }
                            }
                            strValue = isNull(nominee.GuardianDetails.NomGuardianEmail).Trim();
                            if (strValue.Trim() != "")
                            {
                                if (!IsValidEmail(strValue.Trim()))
                                {
                                    errMesssage += "Invalid Value Specified in Email ( " + strValue + " ) " + Environment.NewLine;
                                }
                            }
                            strValue = isNull(nominee.GuardianDetails.NomGuardianMobile).Trim();
                            if (strValue.Trim() != "" && strValue.Trim().ToUpper() != "NONE")
                            {
                                if (strValue.Trim().Length < 10)
                                {
                                    errMesssage += "Invalid Value Specified in Mobile ( " + strValue + " ) " + Environment.NewLine;
                                }
                            }
                        }
                    }

                }

                if (errMesssage.Trim() != "")
                {
                    res.ClientCode = cm.PersonalDetails.ClientCode;
                    res.Status = "N";
                    res.Remark = errMesssage;
                }
                else
                {
                    res.ClientCode = cm.PersonalDetails.ClientCode;
                    res.Status = "Y";
                    res.Remark = "Success";
                }


                SqlParameter[] sqlPrm = new[]
                {
                    new SqlParameter("@i_vcJsonString",cm),
                    new SqlParameter("@i_vcClientCode",cm.PersonalDetails.ClientCode),
                    new SqlParameter("@i_vcRefNo","12345"),
                    new SqlParameter("@i_vcValidationType","Maker"),
                    new SqlParameter("@o_vcOutputJson",""),
                };
                var respSP = utility.Execute_SP("stpr_ReKycValidate", sqlPrm);
                return respSP;
            }
            catch (Exception ex)
            {
                return new Models.TplusResponse()
                {
                    ClientCode = cm.PersonalDetails.ClientCode,
                    Status = "N",
                    Remark = "Error in Process : ValidateUpdateClientData " + Environment.NewLine + "Error :" + ex.Message.ToString() + Environment.NewLine + "Last SQL query used : " + strSQL
                };
            }
        }

        public dynamic SaveAllReKycAttachments(object newModel, string refNo, string userId)
        {
            try
            {
                SqlTransaction objTransTplus = null;
                var jsonObj = JsonConvert.DeserializeObject<dynamic>(newModel.ToString());
                var db = new DataContext();
                using (SqlConnection ObjConnection = new SqlConnection((db.Database.GetDbConnection()).ConnectionString))
                {
                    string clientId = userId;
                    var gstrPCName = System.Environment.GetEnvironmentVariable("COMPUTERNAME");
                    var gstrDBtoday = DateTime.Now.ToString("yyyyMMdd");

                    ObjConnection.Open();

                    #region Save Attachments

                    string attachment = isNull(jsonObj["RekycJson"][0]["Attachments"][0]["AddressAttachment"]).ToString().Trim();
                    string attachmentType = isNull(jsonObj["RekycJson"][0]["Attachments"][0]["AddressProofType"]).ToString().Trim().ToUpper();
                    if (!string.IsNullOrEmpty(attachment) && !string.IsNullOrEmpty(attachmentType))
                    {
                        prCommonAttachmentInsert(ObjConnection, clientId, attachmentType, attachment, "AddressAttachment", objTransTplus, refNo, 1, false);
                    }
                    attachment = isNull(jsonObj["RekycJson"][0]["Attachments"][0]["IncomeAttachment"]).ToString().Trim();
                    attachmentType = isNull(jsonObj["RekycJson"][0]["Attachments"][0]["IncomeProofType"]).ToString().Trim();
                    if (!string.IsNullOrEmpty(attachment))
                    {
                        prCommonAttachmentInsert(ObjConnection, clientId, attachmentType, attachment, "IncomeAttachment", objTransTplus, refNo, 1, false);
                    }
                    attachment = isNull(jsonObj["RekycJson"][0]["Attachments"][0]["SignatureAttachment"]).ToString().Trim(); // For signature 
                    if (!string.IsNullOrEmpty(attachment))
                    {
                        prCommonAttachmentInsert(ObjConnection, clientId, "Sign", attachment, "SignAttachment", objTransTplus, refNo, 1, false);
                    }
                    int k = 1;
                    bool isDel = true;
                    foreach (var nom in jsonObj["RekycJson"][0]["NomineeDetails"])
                    {
                        attachment = isNull(nom["NomineeAttachment"]).ToString().Trim();
                        if (!string.IsNullOrEmpty(attachment))
                        {
                            prCommonAttachmentInsert(ObjConnection, clientId, "Nominee", attachment, "NomineeAttachment", objTransTplus, refNo, k, isDel);
                            isDel = false;
                        }
                        k++;
                    }
                    k = 1;
                    isDel = true;
                    foreach (var bnk in jsonObj["RekycJson"][0]["BankDetails"])
                    {
                        attachment = isNull(bnk["BankAttachment"]).ToString().Trim();
                        if (!string.IsNullOrEmpty(attachment))
                        {
                            prCommonAttachmentInsert(ObjConnection, clientId, "Bank", attachment, "BankAttachment", objTransTplus, refNo, k, isDel);
                            isDel = false;
                        }
                        k++;
                    }
                    k = 1;
                    isDel = true;
                    foreach (var dmt in jsonObj["RekycJson"][0]["DematDetails"])
                    {
                        attachment = isNull(dmt["DematAttachment"]).ToString().Trim();
                        if (!string.IsNullOrEmpty(attachment))
                        {
                            prCommonAttachmentInsert(ObjConnection, clientId, "Demat", attachment, "DematAttachment", objTransTplus, refNo, k, isDel);
                            isDel = false;
                        }
                        k++;
                    }

                    attachment = isNull(jsonObj["RekycJson"][0]["Attachments"][0]["SegmentAttachment"]).ToString().Trim();
                    attachmentType = isNull(jsonObj["RekycJson"][0]["Attachments"][0]["SegmentProofType"]).ToString().Trim().ToUpper();
                    if (!string.IsNullOrEmpty(attachment))
                    {
                        prCommonAttachmentInsert(ObjConnection, clientId, attachmentType, attachment, "SegmentAttachment", objTransTplus, refNo, 1, false);
                    }
                    #endregion

                    return "successfully saved attachments!";
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic UpdateClientMaster(dynamic newModel, string refNo, string userId)
        {
            try
            {
                var gstrPCName = System.Environment.GetEnvironmentVariable("COMPUTERNAME");
                //var newJson = System.Text.Json.JsonSerializer.Serialize(newModel);
                var oldJson = "";// System.Text.Json.JsonSerializer.Serialize(oldModel);
                SqlParameter[] sqlPrm = new[]
                {
                        new SqlParameter("@i_vcOldJsonString",oldJson),
                        new SqlParameter("@i_vcNewJsonString",newModel),
                        new SqlParameter("@i_vcClientCode",userId),
                        new SqlParameter("@i_vcRefNo",refNo),
                        new SqlParameter("@i_vccomputername",gstrPCName)
                };
                List<OutputParamSP> spModelList = new List<OutputParamSP>();
                OutputParamSP spModel = new OutputParamSP();
                spModel.ParamValue = "@o_vcErrorFlag";
                spModel.DbType = SqlDbType.VarChar;
                spModel.Length = 1;
                spModelList.Add(spModel);
                spModel = new OutputParamSP();
                spModel.ParamValue = "@o_vcErrorMessage";
                spModel.DbType = SqlDbType.VarChar;
                spModel.Length = 500;
                spModelList.Add(spModel);
                spModel = new OutputParamSP();
                spModel.ParamValue = "@o_vcJsonOutput";
                spModel.DbType = SqlDbType.NVarChar;
                spModel.Length = 4000;
                spModelList.Add(spModel);

                var respSP = utility.Execute_SP_OuputParam("SP_ReKyc_MakerPost", sqlPrm, spModelList);

                return respSP;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public FillPdfPersonalInfo Fill_PDF_ModelPersonalInfo(object newModel)
        {
            FillPdfPersonalInfo pdfModel = new FillPdfPersonalInfo();
            try
            {
                var valdJsonObj = JsonConvert.DeserializeObject<dynamic>(newModel.ToString());

                pdfModel.KycMode = valdJsonObj["RekycJson"][0]["ReKycDetails"][0]["KycMode"];
                pdfModel.KycNumber = valdJsonObj["RekycJson"][0]["ReKycDetails"][0]["KycNumber"];
                pdfModel.Prefix = valdJsonObj["RekycJson"][0]["PersonalDetails"][0]["ClientsNamePrefix"];
                pdfModel.FirstName = valdJsonObj["RekycJson"][0]["PersonalDetails"][0]["FirstName"];
                pdfModel.MiddleName = valdJsonObj["RekycJson"][0]["PersonalDetails"][0]["MiddleName"];
                pdfModel.LastName = valdJsonObj["RekycJson"][0]["PersonalDetails"][0]["LastName"];
                pdfModel.FatherName = valdJsonObj["RekycJson"][0]["PersonalDetails"][0]["FatherName"];
                pdfModel.PAN_Number = valdJsonObj["RekycJson"][0]["PersonalDetails"][0]["PAN"];
                pdfModel.Gender = valdJsonObj["RekycJson"][0]["PersonalDetails"][0]["Gender"];
                pdfModel.MaritalStatus = valdJsonObj["RekycJson"][0]["PersonalDetails"][0]["MaritalStatus"];
                string dob = valdJsonObj["RekycJson"][0]["PersonalDetails"][0]["DateOfBirth"];
                pdfModel.DobDay = dob?.Substring(6, 2) ?? "";
                pdfModel.DobMonth = dob?.Substring(4, 2) ?? "";
                pdfModel.DobYear = dob?.Substring(0, 4) ?? "";
                //// For update Full Address
                pdfModel.CorrAddress1 = valdJsonObj["RekycJson"][0]["PersonalDetails"][0]["CorrAddress1"];
                pdfModel.CorrAddress2 = valdJsonObj["RekycJson"][0]["PersonalDetails"][0]["CorrAddress2"];
                pdfModel.CorrAddress3 = valdJsonObj["RekycJson"][0]["PersonalDetails"][0]["CorrAddress3"];
                pdfModel.CorrCity = valdJsonObj["RekycJson"][0]["PersonalDetails"][0]["CorrCity"];
                pdfModel.CorrState = valdJsonObj["RekycJson"][0]["PersonalDetails"][0]["CorrState"];
                pdfModel.CorrCountry = valdJsonObj["RekycJson"][0]["PersonalDetails"][0]["CorrCountry"];
                pdfModel.CorrPincode = valdJsonObj["RekycJson"][0]["PersonalDetails"][0]["CorrPincode"];
                pdfModel.setuEsignReturnUrl = valdJsonObj["RekycJson"][0]["ReKycDetails"][0]["ReturnUrlEsignSetu"];
                pdfModel.MobNo = valdJsonObj["RekycJson"][0]["PersonalDetails"][0]["Mobile"];
                pdfModel.UID = valdJsonObj["RekycJson"][0]["PersonalDetails"][0]["UID"];
                pdfModel.Email = valdJsonObj["RekycJson"][0]["PersonalDetails"][0]["Email"];
                pdfModel.PermanentAddressFlag = valdJsonObj["RekycJson"][0]["PersonalDetails"][0]["PermanentAddressFlag"];
                pdfModel.PreviewFlag = valdJsonObj["RekycJson"][0]["ReKycDetails"][0]["PreviewFlag"];

            }
            catch (Exception)
            {
            }
            return pdfModel;
        }


        public dynamic Fill_PDF_Model(object newModel, string refNo, string clientId)
        {
            try
            {
                var valdJsonObj = JsonConvert.DeserializeObject<dynamic>(newModel.ToString());
                Pdf_ModificationModel pdfModel = new Pdf_ModificationModel();
                FillPdfPersonalInfo persInfo = new FillPdfPersonalInfo();
                #region Personal Details
                try
                {
                    persInfo = Fill_PDF_ModelPersonalInfo(newModel);
                    pdfModel.ClientCode = clientId;
                    pdfModel.KycMode = isNull(persInfo.KycMode).ToUpper();
                    pdfModel.KycNumber = isNull(persInfo.KycNumber).ToUpper();
                    pdfModel.Prefix = isNull(persInfo.Prefix).ToUpper();
                    pdfModel.FirstName = isNull(persInfo.FirstName).ToUpper();
                    pdfModel.MiddleName = isNull(persInfo.MiddleName).ToUpper();
                    pdfModel.LastName = isNull(persInfo.LastName).ToUpper();
                    pdfModel.FatherName = isNull(persInfo.FatherName).ToUpper();
                    pdfModel.PAN_Number = isNull(persInfo.PAN_Number).ToUpper() == "" ? "" : isNull(persInfo.PAN_Number).Length > 4 ? "xxxxxx" + isNull(persInfo.PAN_Number).Substring(isNull(persInfo.PAN_Number).Length - 4) : "xxxxxx" + isNull(persInfo.PAN_Number);
                    //pdfModel.PAN_Number = isNull(persInfo.PAN_Number).ToUpper();
                    pdfModel.Gender = isNull(persInfo.Gender).ToUpper();
                    pdfModel.MaritalStatus = isNull(persInfo.MaritalStatus).ToUpper();
                    pdfModel.DobDay = persInfo.DobDay;
                    pdfModel.DobMonth = persInfo.DobMonth;
                    pdfModel.DobYear = persInfo.DobYear;
                    pdfModel.DOB_KRA = persInfo.DobDay + "-" + persInfo.DobMonth + "-" + persInfo.DobYear;
                    pdfModel.CurDateKRA = DateTime.Now.ToString("dd-MMM-yyyy");
                    //// For update Full Address
                    pdfModel.IsAddressModified = false;
                    pdfModel.CorrAddress1 = isNull(persInfo.CorrAddress1);
                    pdfModel.CorrAddress2 = isNull(persInfo.CorrAddress2);
                    pdfModel.CorrAddress3 = isNull(persInfo.CorrAddress3);
                    pdfModel.CorrCity = isNull(persInfo.CorrCity);
                    pdfModel.CorrState = isNull(persInfo.CorrState);
                    pdfModel.CorrCountry = isNull(persInfo.CorrCountry);
                    pdfModel.CorrPincode = isNull(persInfo.CorrPincode);
                    pdfModel.setuEsignReturnUrl = isNull(persInfo.setuEsignReturnUrl);
                    pdfModel.MobileNoPrint = isNull(persInfo.MobNo) == "" ? "" : isNull(persInfo.MobNo).Length > 4 ? "xxxxxx" + isNull(persInfo.MobNo).Substring(isNull(persInfo.MobNo).Length - 4) : "xxxxxx" + isNull(persInfo.MobNo);
                    //pdfModel.MobileNoPrint = isNull(persInfo.MobNo);
                    pdfModel.MobNo = isNull(persInfo.MobNo) == "" ? "" : isNull(persInfo.MobNo).Length > 4 ? "xxxxxx" + isNull(persInfo.MobNo).Substring(isNull(persInfo.MobNo).Length - 4) : "xxxxxx" + isNull(persInfo.MobNo);
                    //pdfModel.MobNo = isNull(persInfo.MobNo);
                    pdfModel.EmailPrint = isNull(persInfo.Email) == "" ? "" : isNull(persInfo.Email).Length > 4 ? "xxxxxxxxxx" + isNull(persInfo.Email).Substring(isNull(persInfo.Email).Length - 4) : "xxxxxxxxxxx" + isNull(persInfo.Email);
                    //pdfModel.EmailPrint = isNull(persInfo.Email);
                    pdfModel.UID = isNull(persInfo.UID) == "" ? "" : isNull(persInfo.UID).Length > 4 ? "xxxxxxxx" + isNull(persInfo.UID).Substring(isNull(persInfo.UID).Length - 4) : "xxxxxxxx" + isNull(persInfo.UID);
                    pdfModel.PermanentAddressFlag = isNull(persInfo.PermanentAddressFlag);
                    pdfModel.PreviewFlag = isNull(persInfo.PreviewFlag).ToLower();
                }
                catch (Exception)
                {
                }
                #endregion
                try
                {
                    pdfModel.DPID = "";
                    string qry = " SELECT top 1 da_actno FROM Dematact(NOLOCK) WHERE da_clientcd = '" + clientId + "' AND da_status = 'A' AND da_defaultyn = 'Y' ";
                    //qry += " AND CHARINDEX(da_dpid , (SELECT sp_sysvalue FROM Sysparameter WHERE sp_parmcd = 'POADPIDS')) >= 1 ";
                    DataTable dtDpid = utility.OpenDataTable(qry);
                    if (dtDpid.Rows.Count > 0)
                    {
                        pdfModel.DPID = dtDpid.Rows[0][0].ToString();
                    }
                }
                catch (Exception) { }
                string strSql = "Select ca_Srno SrNo, ca_cmcd ClientCode, ca_field Field, ca_desc Description, ca_oldValue OldValue, ";
                strSql += " ca_newValue NewValue,  ca_Nfiller1 NFiller1, ca_Nfiller3 NFiller3 ,ca_filler3 Filler3 ";
                strSql += " from Client_ModifyAPI With (NoLock)  ";
                strSql += " where ca_Tplus = 'N' and ca_cmcd = '" + clientId + "' And ca_Nfiller3 = " + refNo + "  Order by Left(ca_field,3) , ca_Nfiller1, Srno";
                var dbTable = utility.OpenDataTable(strSql);
                List<RekycCheckerDto> rekycCheckerDto = CommonExtensions.ConvertToList<RekycCheckerDto>(dbTable);

                var personalGroup = rekycCheckerDto.Where(x => x.Field.Substring(0, 3).ToLower() == "cm_").ToList();
                var prsnlDetail = personalGroup?.GroupBy(x => x.NFiller1);
                foreach (var itm in prsnlDetail)
                {
                    var incomeDateNew = itm?.FirstOrDefault(x => x.Field == "cm_grossincomedt")?.NewValue ?? "";
                    var incomeDateOld = itm?.FirstOrDefault(x => x.Field == "cm_grossincomedt")?.OldValue ?? "";
                    incomeDateNew = incomeDateNew == "" ? "" : DateTime.ParseExact(incomeDateNew, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy");
                    incomeDateOld = incomeDateOld == "" ? "" : DateTime.ParseExact(incomeDateOld, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy");
                    var newMob = isNull(itm?.FirstOrDefault(x => x.Field == "cm_mobile")?.NewValue) ?? "";
                    newMob = newMob.Length > 4 ? newMob.Substring(newMob.Length - 4) : newMob;
                    var oldMob = isNull(itm?.FirstOrDefault(x => x.Field == "cm_mobile")?.OldValue) ?? "";
                    oldMob = oldMob.Length > 4 ? oldMob.Substring(oldMob.Length - 4) : oldMob;
                    var newEmail = isNull(itm?.FirstOrDefault(x => x.Field == "cm_email")?.NewValue) ?? "";
                    newEmail = newEmail.Length > 4 ? newEmail.Substring(newEmail.Length - 4) : newEmail;
                    var oldEmail = isNull(itm?.FirstOrDefault(x => x.Field == "cm_email")?.OldValue) ?? "";
                    oldEmail = oldEmail.Length > 4 ? oldEmail.Substring(oldEmail.Length - 4) : oldEmail;

                    pdfModel.IncomeOld = utility.ReturnIncomeString(itm?.FirstOrDefault(x => x.Field == "cm_grossincome")?.OldValue ?? "");
                    pdfModel.Income = utility.ReturnIncomeString(itm?.FirstOrDefault(x => x.Field == "cm_grossincome")?.NewValue ?? "");
                    pdfModel.IncomeDateOld = incomeDateOld;
                    pdfModel.IncomeDate = incomeDateNew;
                    pdfModel.MobileOld = oldMob;
                    pdfModel.Mobile = newMob;
                    pdfModel.EmailOld = oldEmail;
                    pdfModel.Email = newEmail;
                    pdfModel.IsMobEmailIncomeModified = true;

                    pdfModel.CorrAddress1Old = itm?.FirstOrDefault(x => x.Field == "cm_add1")?.OldValue ?? "";
                    pdfModel.CorrAddress2Old = itm?.FirstOrDefault(x => x.Field == "cm_add2")?.OldValue ?? "";
                    pdfModel.CorrAddress3Old = itm?.FirstOrDefault(x => x.Field == "cm_add3")?.OldValue ?? "";
                    pdfModel.CorrCityOld = itm?.FirstOrDefault(x => x.Field == "cm_add4")?.OldValue ?? "";
                    pdfModel.CorrStateOld = itm?.FirstOrDefault(x => x.Field == "cm_state")?.OldValue ?? "";
                    pdfModel.CorrCountryOld = itm?.FirstOrDefault(x => x.Field == "cm_pcountry")?.OldValue ?? "";
                    pdfModel.CorrPincodeOld = itm?.FirstOrDefault(x => x.Field == "cm_pincode")?.OldValue ?? "";

                    string add1 = itm?.FirstOrDefault(x => x.Field == "cm_add1")?.NewValue ?? "";
                    string add2 = itm?.FirstOrDefault(x => x.Field == "cm_add2")?.NewValue ?? "";
                    string add3 = itm?.FirstOrDefault(x => x.Field == "cm_add3")?.NewValue ?? "";
                    string addCity = itm?.FirstOrDefault(x => x.Field == "cm_add4")?.NewValue ?? "";
                    string addPincode = itm?.FirstOrDefault(x => x.Field == "cm_pincode")?.NewValue ?? "";
                    string oldCompareAddr = pdfModel.CorrAddress1Old + pdfModel.CorrAddress2Old + pdfModel.CorrAddress3Old + pdfModel.CorrCityOld + pdfModel.CorrPincodeOld;
                    string newCompareAddr = add1 + add2 + add3 + addCity + addPincode;
                    if (oldCompareAddr != newCompareAddr)
                    {
                        pdfModel.IsAddressModified = true;
                    }
                }

                var nomMakergroup = rekycCheckerDto.Where(x => x.Field.Substring(0, 3).ToLower() == "cn_" && (x.Filler3 == "1" || x.Filler3 == "3" || x.Filler3 == "5")).ToList();
                var nomineeGroup = nomMakergroup?.OrderBy(x => x.NFiller1)?.GroupBy(x => x.NFiller1);
                int i = 1;
                foreach (var itm in nomineeGroup)
                {
                    string fullname = itm?.FirstOrDefault(x => x.Field == "cn_firstname")?.NewValue + " " + itm?.FirstOrDefault(x => x.Field == "cn_middlename")?.NewValue + " " + itm?.FirstOrDefault(x => x.Field == "cn_lastname")?.NewValue;
                    var nomDOB = itm?.FirstOrDefault(x => x.Field == "cn_dob")?.NewValue ?? "";
                    nomDOB = nomDOB == "" ? "" : DateTime.ParseExact(nomDOB, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy");
                    var maskUID = itm?.FirstOrDefault(x => x.Field == "cn_uid")?.NewValue ?? "";
                    maskUID = maskUID == "" ? "" : maskUID.Length > 4 ? "xxxxxxxx" + maskUID.Substring(maskUID.Length - 4) : "xxxxxx" + maskUID;
                    var maskPAN = itm?.FirstOrDefault(x => x.Field == "cn_pan")?.NewValue ?? "";
                    maskPAN = maskPAN == "" ? "" : maskPAN.Length > 4 ? "xxxxxx" + maskPAN.Substring(maskPAN.Length - 4) : "xxxxxx" + maskPAN;
                    var maskMobNo = itm?.FirstOrDefault(x => x.Field == "cn_tel")?.NewValue ?? "";
                    maskMobNo = maskMobNo.Length > 4 ? "xxxxxx" + maskMobNo.Substring(maskMobNo.Length - 4) : "xxxxxx" + maskMobNo;
                    var maskEmail = itm?.FirstOrDefault(x => x.Field == "cn_email")?.NewValue ?? "";
                    maskEmail = maskEmail.Length > 4 ? "xxxxxxxxxx" + maskEmail.Substring(maskEmail.Length - 4) : "xxxxxx" + maskEmail;
                    if (i == 1)
                    {
                        pdfModel.NomFullName1 = fullname;
                        pdfModel.NomAddress11 = itm?.FirstOrDefault(x => x.Field == "cn_add1")?.NewValue ?? "";
                        pdfModel.NomAddress21 = itm?.FirstOrDefault(x => x.Field == "cn_add2")?.NewValue ?? "";
                        pdfModel.NomAddress31 = itm?.FirstOrDefault(x => x.Field == "cn_add3")?.NewValue ?? "";
                        pdfModel.NomAddressCity1 = itm?.FirstOrDefault(x => x.Field == "cn_city")?.NewValue ?? "";
                        pdfModel.NomAddressState1 = itm?.FirstOrDefault(x => x.Field == "cn_state")?.NewValue ?? "";
                        pdfModel.NomAddressCountry1 = itm?.FirstOrDefault(x => x.Field == "cn_country")?.NewValue ?? "";
                        pdfModel.NomPincode1 = itm?.FirstOrDefault(x => x.Field == "cn_pin")?.NewValue ?? "";
                        pdfModel.NomMobile1 = maskMobNo;
                        pdfModel.NomineePAN1 = maskPAN;
                        pdfModel.NomineeUID1 = maskUID;
                        pdfModel.NomineeDOB1 = nomDOB;
                        pdfModel.NomEmail1 = maskEmail;
                        pdfModel.NomPercentage1 = itm?.FirstOrDefault(x => x.Field == "cn_NomPershare")?.NewValue ?? "";
                        pdfModel.NomRelation1 = utility.ReturnNomineeRelation(itm?.FirstOrDefault(x => x.Field == "cn_relation")?.NewValue ?? "");
                        pdfModel.IsNomShow1 = true;
                        pdfModel.ResidualFlag1 = itm?.FirstOrDefault(x => x.Field == "cn_ResidualFlag")?.NewValue ?? "";
                    }
                    else if (i == 2)
                    {
                        pdfModel.NomFullName2 = fullname;
                        pdfModel.NomAddress12 = itm?.FirstOrDefault(x => x.Field == "cn_add1")?.NewValue ?? "";
                        pdfModel.NomAddress22 = itm?.FirstOrDefault(x => x.Field == "cn_add2")?.NewValue ?? "";
                        pdfModel.NomAddress32 = itm?.FirstOrDefault(x => x.Field == "cn_add3")?.NewValue ?? "";
                        pdfModel.NomAddressCity2 = itm?.FirstOrDefault(x => x.Field == "cn_city")?.NewValue ?? "";
                        pdfModel.NomAddressState2 = itm?.FirstOrDefault(x => x.Field == "cn_state")?.NewValue ?? "";
                        pdfModel.NomAddressCountry2 = itm?.FirstOrDefault(x => x.Field == "cn_country")?.NewValue ?? "";
                        pdfModel.NomPincode2 = itm?.FirstOrDefault(x => x.Field == "cn_pin")?.NewValue ?? "";
                        pdfModel.NomMobile2 = maskMobNo;
                        pdfModel.NomineePAN2 = maskPAN;
                        pdfModel.NomineeUID2 = maskUID;
                        pdfModel.NomineeDOB2 = nomDOB;
                        pdfModel.NomEmail2 = maskEmail;
                        pdfModel.NomPercentage2 = itm?.FirstOrDefault(x => x.Field == "cn_NomPershare")?.NewValue ?? "";
                        pdfModel.NomRelation2 = utility.ReturnNomineeRelation(itm?.FirstOrDefault(x => x.Field == "cn_relation")?.NewValue ?? "");
                        pdfModel.IsNomShow2 = true;
                        pdfModel.ResidualFlag2 = itm?.FirstOrDefault(x => x.Field == "cn_ResidualFlag")?.NewValue ?? "";
                    }
                    else if (i == 3)
                    {
                        pdfModel.NomFullName3 = fullname;
                        pdfModel.NomAddress13 = itm?.FirstOrDefault(x => x.Field == "cn_add1")?.NewValue ?? "";
                        pdfModel.NomAddress23 = itm?.FirstOrDefault(x => x.Field == "cn_add2")?.NewValue ?? "";
                        pdfModel.NomAddress33 = itm?.FirstOrDefault(x => x.Field == "cn_add3")?.NewValue ?? "";
                        pdfModel.NomAddressCity3 = itm?.FirstOrDefault(x => x.Field == "cn_city")?.NewValue ?? "";
                        pdfModel.NomAddressState3 = itm?.FirstOrDefault(x => x.Field == "cn_state")?.NewValue ?? "";
                        pdfModel.NomAddressCountry3 = itm?.FirstOrDefault(x => x.Field == "cn_country")?.NewValue ?? "";
                        pdfModel.NomPincode3 = itm?.FirstOrDefault(x => x.Field == "cn_pin")?.NewValue ?? "";
                        pdfModel.NomMobile3 = maskMobNo;
                        pdfModel.NomineePAN3 = maskPAN;
                        pdfModel.NomineeUID3 = maskUID;
                        pdfModel.NomineeDOB3 = nomDOB;
                        pdfModel.NomEmail3 = maskEmail;
                        pdfModel.NomPercentage3 = itm?.FirstOrDefault(x => x.Field == "cn_NomPershare")?.NewValue ?? "";
                        pdfModel.NomRelation3 = utility.ReturnNomineeRelation(itm?.FirstOrDefault(x => x.Field == "cn_relation")?.NewValue ?? "");
                        pdfModel.IsNomShow3 = true;
                        pdfModel.ResidualFlag3 = itm?.FirstOrDefault(x => x.Field == "cn_ResidualFlag")?.NewValue ?? "";
                    }
                    i++;
                }

                var grdMakergroup = rekycCheckerDto.Where(x => x.Field.Substring(0, 3).ToLower() == "cn_" && (x.Filler3 == "2" || x.Filler3 == "4" || x.Filler3 == "6")).ToList();
                var gaurdianGroup = grdMakergroup?.OrderBy(x => x.NFiller1)?.GroupBy(x => x.NFiller1);
                foreach (var itm in gaurdianGroup)
                {
                    string fullname = itm?.FirstOrDefault(x => x.Field == "cn_firstname")?.NewValue + " " + itm?.FirstOrDefault(x => x.Field == "cn_middlename")?.NewValue + " " + itm?.FirstOrDefault(x => x.Field == "cn_lastname")?.NewValue;
                    int filler1 = Convert.ToInt32(itm?.FirstOrDefault(x => x.Field == "cn_firstname")?.Filler3);
                    var grdDOB = itm?.FirstOrDefault(x => x.Field == "cn_dob")?.NewValue ?? "";
                    grdDOB = grdDOB == "" ? "" : DateTime.ParseExact(grdDOB, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy");
                    var maskUID = itm?.FirstOrDefault(x => x.Field == "cn_uid")?.NewValue ?? "";
                    maskUID = maskUID == "" ? "" : maskUID.Length > 4 ? "xxxxxxxx" + maskUID.Substring(maskUID.Length - 4) : "xxxxxx" + maskUID;
                    var maskPAN = itm?.FirstOrDefault(x => x.Field == "cn_pan")?.NewValue ?? "";
                    maskPAN = maskPAN == "" ? "" : maskPAN.Length > 4 ? "xxxxxx" + maskPAN.Substring(maskPAN.Length - 4) : "xxxxxx" + maskPAN;
                    var maskMobNo = itm?.FirstOrDefault(x => x.Field == "cn_tel")?.NewValue ?? "";
                    maskMobNo = maskMobNo.Length > 4 ? "xxxxxx" + maskMobNo.Substring(maskMobNo.Length - 4) : "xxxxxx" + maskMobNo;
                    var maskEmail = itm?.FirstOrDefault(x => x.Field == "cn_email")?.NewValue ?? "";
                    maskEmail = maskEmail.Length > 4 ? "xxxxxxxxxx" + maskEmail.Substring(maskEmail.Length - 4) : "xxxxxx" + maskEmail;
                    if (filler1 == 2)
                    {
                        pdfModel.NomGuardianFullName1 = fullname;
                        pdfModel.NomGuardianAddress11 = itm?.FirstOrDefault(x => x.Field == "cn_add1")?.NewValue ?? "";
                        pdfModel.NomGuardianAddress21 = itm?.FirstOrDefault(x => x.Field == "cn_add2")?.NewValue ?? "";
                        pdfModel.NomGuardianAddress31 = itm?.FirstOrDefault(x => x.Field == "cn_add3")?.NewValue ?? "";
                        pdfModel.NomGuardianCity1 = itm?.FirstOrDefault(x => x.Field == "cn_city")?.NewValue ?? "";
                        pdfModel.NomGuardianState1 = itm?.FirstOrDefault(x => x.Field == "cn_state")?.NewValue ?? "";
                        pdfModel.NomGuardianPincode1 = itm?.FirstOrDefault(x => x.Field == "cn_pin")?.NewValue ?? "";
                        pdfModel.NomGuardianMobile1 = maskMobNo;
                        pdfModel.NomGuardianPAN1 = maskPAN;
                        pdfModel.NomGuardianUID1 = maskUID;
                        pdfModel.NomGuardianDob1 = grdDOB;
                        pdfModel.NomGuardianEmail1 = maskEmail;
                        pdfModel.NomGuardianRelation1 = utility.ReturnNomineeRelation(itm?.FirstOrDefault(x => x.Field == "cn_relation")?.NewValue ?? "");
                        pdfModel.IsGaurdnShow1 = true;
                    }
                    else if (filler1 == 4)
                    {
                        pdfModel.NomGuardianFullName2 = fullname;
                        pdfModel.NomGuardianAddress12 = itm?.FirstOrDefault(x => x.Field == "cn_add1")?.NewValue ?? "";
                        pdfModel.NomGuardianAddress22 = itm?.FirstOrDefault(x => x.Field == "cn_add2")?.NewValue ?? "";
                        pdfModel.NomGuardianAddress32 = itm?.FirstOrDefault(x => x.Field == "cn_add3")?.NewValue ?? "";
                        pdfModel.NomGuardianCity2 = itm?.FirstOrDefault(x => x.Field == "cn_city")?.NewValue ?? "";
                        pdfModel.NomGuardianState2 = itm?.FirstOrDefault(x => x.Field == "cn_state")?.NewValue ?? "";
                        pdfModel.NomGuardianPincode2 = itm?.FirstOrDefault(x => x.Field == "cn_pin")?.NewValue ?? "";
                        pdfModel.NomGuardianMobile2 = maskMobNo;
                        pdfModel.NomGuardianPAN2 = maskPAN;
                        pdfModel.NomGuardianUID2 = maskUID;
                        pdfModel.NomGuardianDob2 = grdDOB;
                        pdfModel.NomGuardianEmail2 = maskEmail;
                        pdfModel.NomGuardianRelation2 = utility.ReturnNomineeRelation(itm?.FirstOrDefault(x => x.Field == "cn_relation")?.NewValue ?? "");
                        pdfModel.IsGaurdnShow2 = true;
                    }
                    else if (filler1 == 6)
                    {
                        pdfModel.NomGuardianFullName3 = fullname;
                        pdfModel.NomGuardianAddress13 = itm?.FirstOrDefault(x => x.Field == "cn_add1")?.NewValue ?? "";
                        pdfModel.NomGuardianAddress23 = itm?.FirstOrDefault(x => x.Field == "cn_add2")?.NewValue ?? "";
                        pdfModel.NomGuardianAddress33 = itm?.FirstOrDefault(x => x.Field == "cn_add3")?.NewValue ?? "";
                        pdfModel.NomGuardianCity3 = itm?.FirstOrDefault(x => x.Field == "cn_city")?.NewValue ?? "";
                        pdfModel.NomGuardianState3 = itm?.FirstOrDefault(x => x.Field == "cn_state")?.NewValue ?? "";
                        pdfModel.NomGuardianPincode3 = itm?.FirstOrDefault(x => x.Field == "cn_pin")?.NewValue ?? "";
                        pdfModel.NomGuardianMobile3 = maskMobNo;
                        pdfModel.NomGuardianPAN3 = maskPAN;
                        pdfModel.NomGuardianUID3 = maskUID;
                        pdfModel.NomGuardianDob3 = grdDOB;
                        pdfModel.NomGuardianEmail3 = maskEmail;
                        pdfModel.NomGuardianRelation3 = utility.ReturnNomineeRelation(itm?.FirstOrDefault(x => x.Field == "cn_relation")?.NewValue ?? "");
                        pdfModel.IsGaurdnShow3 = true;
                    }
                }

                var bankMakergroup = rekycCheckerDto.Where(x => x.Field.Substring(0, 3).ToLower() == "ba_").ToList();
                var bankGroup = bankMakergroup?.OrderBy(x => x.NFiller1)?.GroupBy(x => x.NFiller1);
                i = 1;
                foreach (var itm in bankGroup)
                {
                    string deflt = itm?.FirstOrDefault(x => x.Field == "ba_default")?.NewValue ?? "";
                    string acNo = isNull(itm?.FirstOrDefault(x => x.Field == "ba_actno")?.NewValue) ?? "";
                    string maskAcNo = acNo.Length > 4 ? acNo.Substring(acNo.Length - 4) : acNo;
                    if (i == 1)
                    {
                        pdfModel.BankAccNo1 = "xxxxxxxx" + maskAcNo;
                        pdfModel.BankIFSC1 = itm?.FirstOrDefault(x => x.Field == "ba_ifsccode")?.NewValue ?? "";
                        pdfModel.BankMICR1 = itm?.FirstOrDefault(x => x.Field == "ba_micr")?.NewValue ?? "";
                        pdfModel.IsDefault1 = deflt == "Y" ? true : false;
                        pdfModel.AccountType1 = itm?.FirstOrDefault(x => x.Field == "ba_acttype")?.NewValue ?? "";
                        pdfModel.IsBankShow1 = true;
                    }
                    else if (i == 2)
                    {
                        pdfModel.BankAccNo2 = "xxxxxxxx" + maskAcNo;
                        pdfModel.BankIFSC2 = itm?.FirstOrDefault(x => x.Field == "ba_ifsccode")?.NewValue ?? "";
                        pdfModel.BankMICR2 = itm?.FirstOrDefault(x => x.Field == "ba_micr")?.NewValue ?? "";
                        pdfModel.IsDefault2 = deflt == "Y" ? true : false;
                        pdfModel.AccountType2 = itm?.FirstOrDefault(x => x.Field == "ba_acttype")?.NewValue ?? "";
                        pdfModel.IsBankShow2 = true;
                    }
                    else if (i == 3)
                    {
                        pdfModel.BankAccNo3 = "xxxxxxxx" + maskAcNo;
                        pdfModel.BankIFSC3 = itm?.FirstOrDefault(x => x.Field == "ba_ifsccode")?.NewValue ?? "";
                        pdfModel.BankMICR3 = itm?.FirstOrDefault(x => x.Field == "ba_micr")?.NewValue ?? "";
                        pdfModel.IsDefault3 = deflt == "Y" ? true : false;
                        pdfModel.AccountType3 = itm?.FirstOrDefault(x => x.Field == "ba_acttype")?.NewValue ?? "";
                        pdfModel.IsBankShow3 = true;
                    }
                    i++;
                }

                var demtMakergroup = rekycCheckerDto.Where(x => x.Field.Substring(0, 3).ToLower() == "da_").ToList();
                var dmtGroup = demtMakergroup?.OrderBy(x => x.NFiller1)?.GroupBy(x => x.NFiller1);
                i = 1;
                foreach (var itm in dmtGroup)
                {
                    string deflt = itm?.FirstOrDefault(x => x.Field == "da_defaultyn")?.NewValue ?? "";
                    if (i == 1)
                    {
                        pdfModel.DPAcNo1 = itm?.FirstOrDefault(x => x.Field == "da_actno")?.NewValue ?? "";
                        pdfModel.DPID1 = itm?.FirstOrDefault(x => x.Field == "da_dpid")?.NewValue ?? "";
                        pdfModel.DPType1 = itm?.FirstOrDefault(x => x.Field == "da_type")?.NewValue ?? "";
                        pdfModel.IsDematDefault1 = deflt == "Y" ? true : false;
                        pdfModel.IsDematShow1 = true;
                    }
                    else if (i == 2)
                    {
                        pdfModel.DPAcNo2 = itm?.FirstOrDefault(x => x.Field == "da_actno")?.NewValue ?? "";
                        pdfModel.DPID2 = itm?.FirstOrDefault(x => x.Field == "da_dpid")?.NewValue ?? "";
                        pdfModel.DPType2 = itm?.FirstOrDefault(x => x.Field == "da_type")?.NewValue ?? "";
                        pdfModel.IsDematDefault2 = deflt == "Y" ? true : false;
                        pdfModel.IsDematShow2 = true;
                    }
                    else if (i == 3)
                    {
                        pdfModel.DPAcNo3 = itm?.FirstOrDefault(x => x.Field == "da_actno")?.NewValue ?? "";
                        pdfModel.DPID3 = itm?.FirstOrDefault(x => x.Field == "da_dpid")?.NewValue ?? "";
                        pdfModel.DPType3 = itm?.FirstOrDefault(x => x.Field == "da_type")?.NewValue ?? "";
                        pdfModel.IsDematDefault3 = deflt == "Y" ? true : false;
                        pdfModel.IsDematShow3 = true;
                    }
                    i++;
                }

                var sgmtGroup = rekycCheckerDto.Where(x => x.Field.Substring(0, 3).ToLower() == "sg_").ToList();
                pdfModel.SegmentDetailsOld = sgmtGroup?.FirstOrDefault(x => x.Field == "sg_segment")?.OldValue ?? "";
                pdfModel.SegmentDetailsNew = sgmtGroup?.FirstOrDefault(x => x.Field == "sg_segment")?.NewValue ?? "";
                if (pdfModel.SegmentDetailsOld != pdfModel.SegmentDetailsNew)
                {
                    pdfModel.IsSegmentModified = true;
                }

                return pdfModel;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public TplusResponse UpdateClientMasterOld(FinalReKycModel cm, Pdf_ModificationModel pdfModel, string refNo, FinalReKycModel oldModelValue)
        {
            TplusResponse res = new TplusResponse();
            bool blnBeginTranTplus = false;
            SqlTransaction objTransTplus = null;
            string successmsg = "";
            try
            {
                var db = new DataContext();
                using (SqlConnection ObjConnection = new SqlConnection((db.Database.GetDbConnection()).ConnectionString))
                {
                    string strOldValue = "";
                    string strNewValue = "";
                    string Code = cm.PersonalDetails.ClientCode;
                    var gstrPCName = System.Environment.GetEnvironmentVariable("COMPUTERNAME");
                    var gstrDBtoday = DateTime.Now.ToString("yyyyMMdd");
                    bool blnClientNomOld = false;

                    ObjConnection.Open();

                    string conCmx = "";
                    DataTable dtcmx = utility.OpenDataTable("SELECT * FROM Other_Products where OP_Product='Commex' and OP_Status='A'", ObjConnection, objTransTplus);
                    if (dtcmx.Rows.Count > 0)
                    {
                        conCmx = (dtcmx.Rows[0]["OP_DataBase"].ToString()) + "." + (dtcmx.Rows[0]["OP_Owner"].ToString()) + ".";
                        conCmx = conCmx.Trim();
                    }
                    objTransTplus = ObjConnection.BeginTransaction(IsolationLevel.Serializable);
                    //utility.BeginTrans();
                    blnBeginTranTplus = true;

                    #region Personal Details
                    string panNoStr = cm.PersonalDetails.PAN.Trim().ToUpper() ?? "";
                    pdfModel.KycMode = cm.KycMode.ToUpper() ?? "";
                    pdfModel.KycNumber = cm.KycNumber;
                    pdfModel.Prefix = cm.PersonalDetails.ClientsNamePrefix.ToUpper();
                    pdfModel.FirstName = cm.PersonalDetails.FirstName.ToUpper();
                    pdfModel.MiddleName = cm.PersonalDetails.MiddleName.ToUpper();
                    pdfModel.LastName = cm.PersonalDetails.LastName.ToUpper();
                    pdfModel.FatherName = cm.PersonalDetails.FatherName.ToUpper();
                    pdfModel.PAN_Number = cm.PersonalDetails.PAN.Trim().ToUpper() ?? "";//panNoStr == "" ? "" : "******" + panNoStr.Substring(6);
                    pdfModel.Gender = cm.PersonalDetails.Gender.Trim().ToUpper();
                    pdfModel.MaritalStatus = cm.PersonalDetails.MaritalStatus.ToUpper().Trim();
                    pdfModel.DobDay = cm.PersonalDetails.DateOfBirth?.Trim().Substring(6, 2) ?? "";
                    pdfModel.DobMonth = cm.PersonalDetails.DateOfBirth?.Trim().Substring(4, 2) ?? "";
                    pdfModel.DobYear = cm.PersonalDetails.DateOfBirth?.Trim().Substring(0, 4) ?? "";
                    //// For update Full Address
                    pdfModel.CorrAddress1 = cm.PersonalDetails.CorrAddress1.ToUpper();
                    pdfModel.CorrAddress2 = cm.PersonalDetails.CorrAddress2.ToUpper();
                    pdfModel.CorrAddress3 = cm.PersonalDetails.CorrAddress3.ToUpper();
                    pdfModel.CorrCity = cm.PersonalDetails.CorrCity.ToUpper();
                    pdfModel.CorrState = cm.PersonalDetails.CorrState.ToUpper();
                    pdfModel.CorrCountry = cm.PersonalDetails.CorrCountry.ToUpper() ?? "INDIA";
                    pdfModel.CorrPincode = cm.PersonalDetails.CorrPincode;

                    bool isPerAddress = false, isNomChange = false;
                    strOldValue = oldModelValue.PersonalDetails.CorrAddress1.ToUpper();
                    strNewValue = Strings.Left(isNull(cm.PersonalDetails.CorrAddress1).Trim().ToUpper(), 50);
                    if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                    {
                        isPerAddress = true;
                    }
                    strOldValue = oldModelValue.PersonalDetails.CorrAddress2.ToUpper();
                    strNewValue = Strings.Left(isNull(cm.PersonalDetails.CorrAddress2).Trim().ToUpper(), 50);
                    if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                    {
                        isPerAddress = true;
                    }
                    strOldValue = oldModelValue.PersonalDetails.CorrAddress3.ToUpper();
                    strNewValue = Strings.Left(isNull(cm.PersonalDetails.CorrAddress3).Trim().ToUpper(), 50);
                    if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                    {
                        isPerAddress = true;
                    }
                    strOldValue = oldModelValue.PersonalDetails.CorrCity.ToUpper(); //// For City
                    strNewValue = Strings.Left(isNull(cm.PersonalDetails.CorrCity).Trim().ToUpper(), 50);
                    if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                    {
                        isPerAddress = true;
                    }
                    strOldValue = oldModelValue.PersonalDetails.CorrState.ToUpper();
                    strNewValue = isNull(cm.PersonalDetails.CorrState).Trim().ToUpper();
                    if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                    {
                        isPerAddress = true;
                    }
                    strNewValue = isNull(cm.PersonalDetails.CorrCountry).Trim().ToUpper();
                    strOldValue = oldModelValue.PersonalDetails.CorrCountry.ToUpper();
                    if (strNewValue.Trim() != "" && (strOldValue.Trim() != strNewValue.Trim()))
                    {
                        isPerAddress = true;
                    }
                    strOldValue = oldModelValue.PersonalDetails.CorrPincode.ToUpper();
                    strNewValue = isNull(cm.PersonalDetails.CorrPincode).Trim().ToUpper();
                    if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                    {
                        isPerAddress = true;
                    }
                    if (isPerAddress == true)
                    {
                        prCommonInsert(ObjConnection, false, "Client Master", "Client_master", "cm_cd", "Client Code", Code, "cm_add1", "Address Address1", oldModelValue.PersonalDetails.CorrAddress1, Strings.Left(isNull(cm.PersonalDetails.CorrAddress1).Trim().ToUpper(), 50), gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo);
                        prCommonInsert(ObjConnection, false, "Client Master", "Client_master", "cm_cd", "Client Code", Code, "cm_add2", "Address Address2", oldModelValue.PersonalDetails.CorrAddress2, Strings.Left(isNull(cm.PersonalDetails.CorrAddress2).Trim().ToUpper(), 50), gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo);
                        prCommonInsert(ObjConnection, false, "Client Master", "Client_master", "cm_cd", "Client Code", Code, "cm_add3", "Address Address3", oldModelValue.PersonalDetails.CorrAddress3, Strings.Left(isNull(cm.PersonalDetails.CorrAddress3).Trim().ToUpper(), 50), gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo);
                        prCommonInsert(ObjConnection, false, "Client Master", "Client_master", "cm_cd", "Client Code", Code, "cm_add4", "Address City/add4", oldModelValue.PersonalDetails.CorrCity, Strings.Left(isNull(cm.PersonalDetails.CorrCity).Trim().ToUpper(), 50), gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo);
                        prCommonInsert(ObjConnection, false, "Client Info", "Client_Info", "cm2_cd", "Client Code", Code, "cm_state", "Address State", oldModelValue.PersonalDetails.CorrState, isNull(cm.PersonalDetails.CorrState).Trim(), gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo);
                        prCommonInsert(ObjConnection, false, "Client Info", "Client_Info", "cm2_cd", "Client Code", Code, "cm_pcountry", "Address Country", oldModelValue.PersonalDetails.CorrCountry, isNull(cm.PersonalDetails.CorrCountry).Trim(), gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo);
                        prCommonInsert(ObjConnection, false, "Client Master", "Client_master", "cm_cd", "Client Code", Code, "cm_pincode", "Address PinCode", oldModelValue.PersonalDetails.CorrPincode, isNull(cm.PersonalDetails.CorrPincode).Trim(), gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo);
                    }
                    //// For update Mobile No.
                    string MobRelation = "", EmailRelation = "";
                    if (isNull(cm.PersonalDetails.Mobile).Trim() != "" || isNull(cm.PersonalDetails.Mobile).Trim() != "")
                    {
                        switch (isNull(cm.PersonalDetails.MobileRelation))
                        {
                            case "0":
                                MobRelation = "Self";
                                break;
                            case "1":
                                MobRelation = "Spouse";
                                break;
                            case "2":
                                MobRelation = "Dependent Children";
                                break;
                            case "3":
                                MobRelation = "Dependent Parent";
                                break;
                        }
                        switch (isNull(cm.PersonalDetails.EmailRelation))
                        {
                            case "0":
                                EmailRelation = "Self";
                                break;
                            case "1":
                                EmailRelation = "Spouse";
                                break;
                            case "2":
                                EmailRelation = "Dependent Children";
                                break;
                            case "3":
                                EmailRelation = "Dependent Parent";
                                break;
                        }
                    }
                    strOldValue = oldModelValue.PersonalDetails.Mobile.ToUpper();
                    strNewValue = isNull(cm.PersonalDetails.Mobile).Trim().ToUpper();
                    if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                    {
                        prCommonInsert(ObjConnection, false, "Client Master", "Client_master", "cm_cd", "Client Code", Code, "cm_mobile", "Mobile", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo);
                        //// For save mobile relation
                        prCommonInsert(ObjConnection, false, "Common Contacts", "Common_Contacts", "cc_Client", "Client Mobile", Code, "cc_RelationMobile", "Mobile Relation", "", MobRelation, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo);
                        pdfModel.IsMobEmailIncomeModified = true;
                        pdfModel.Mobile = strNewValue;
                        pdfModel.MobileOld = strOldValue;
                    }
                    //// For update Email address
                    strOldValue = oldModelValue.PersonalDetails.Email.ToUpper();
                    strNewValue = isNull(cm.PersonalDetails.Email).Trim().ToUpper();
                    if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                    {
                        prCommonInsert(ObjConnection, false, "Client Master", "Client_master", "cm_cd", "Client Code", Code, "cm_email", "Email", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo);
                        //// For save Email relation
                        prCommonInsert(ObjConnection, false, "Common Contacts", "Common_Contacts", "cc_Client", "Client Email", Code, "cc_RelationEmail", "Email Relation", "", EmailRelation, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo);
                        pdfModel.IsMobEmailIncomeModified = true;
                        pdfModel.Email = strNewValue;
                        pdfModel.EmailOld = strOldValue;
                    }
                    //// For update Income Details
                    strOldValue = oldModelValue.PersonalDetails.IncomeValue.ToUpper();
                    strNewValue = isNull(cm.PersonalDetails.IncomeValue).Trim().ToUpper();
                    if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                    {
                        prCommonInsert(ObjConnection, false, "Client Info", "Client_Info", "cm2_cd", "Client Code", Code, "cm_grossincome", "Income", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo);
                        pdfModel.IsMobEmailIncomeModified = true;
                        pdfModel.Income = utility.ReturnIncomeString(strNewValue);
                        pdfModel.IncomeOld = utility.ReturnIncomeString(strOldValue);
                    }
                    //// For update Income Date
                    strOldValue = oldModelValue.PersonalDetails.IncomeDate.ToUpper();
                    strNewValue = isNull(cm.PersonalDetails.IncomeDate).Trim().ToUpper();
                    if (strNewValue.Trim() != "" && strOldValue.Trim() != strNewValue.Trim())
                    {
                        prCommonInsert(ObjConnection, false, "Client Info", "Client_Info", "cm2_cd", "Client Code", Code, "cm_grossincomedt", "Income Date", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo);
                        pdfModel.IsMobEmailIncomeModified = true;
                        pdfModel.IncomeDate = DateTime.ParseExact(strNewValue, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd-MM-yyyy");
                        pdfModel.IncomeDateOld = DateTime.ParseExact(strOldValue, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd-MM-yyyy");
                    }
                    string attachment = isNull(cm.PersonalDetails.AddressAttachment).ToString().Trim();
                    string attachmentType = isNull(cm.PersonalDetails.AddressAttachmentType).ToString().Trim().ToUpper();
                    if (!string.IsNullOrEmpty(attachment) && !string.IsNullOrEmpty(attachmentType))
                    {
                        prCommonAttachmentInsert(ObjConnection, Code, attachmentType, attachment, "AddressAttachment", objTransTplus, refNo, 1, false);
                        pdfModel.AddressProof = attachmentType.ToUpper();
                    }

                    attachment = isNull(cm.PersonalDetails.IncomeAttachment).ToString().Trim();
                    if (!string.IsNullOrEmpty(attachment))
                    {
                        prCommonAttachmentInsert(ObjConnection, Code, "Income", attachment, "IncomeAttachment", objTransTplus, refNo, 1, false);
                    }
                    //// For Dormant/Status
                    if (cm.DormantDetails.DormantIsActive == true)
                    {
                        strOldValue = oldModelValue.DormantDetails.DormantStatus;
                        strNewValue = strOldValue == "" ? "Rekyc" : "Active";
                        prCommonInsert(ObjConnection, false, "Client Master", "Client_master", "cm_cd", "Client Code", Code, "cm_freezeyn", "Dormant Status " + cm.DormantDetails.DormantStatus, strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo);
                    }

                    #endregion
                    int k = 1;
                    //// Nominee Details
                    if (cm.NomineeDetails != null)
                    {
                        k = 1;
                        List<Nominee> nomPdfList = new List<Nominee>();
                        if (oldModelValue.NomineeDetails.Count > 0)
                        {
                            blnClientNomOld = true;
                        }
                        foreach (var nominee in cm.NomineeDetails)
                        {
                            Nominee nomPdf = new Nominee();
                            //Nominee Name
                            if (blnClientNomOld == true)
                            {
                                strOldValue = "";
                                if (blnClientNomOld)
                                {
                                    strOldValue = oldModelValue.NomineeDetails[0].NomFirstName + " " + oldModelValue.NomineeDetails[0].NomMiddleName + " " + oldModelValue.NomineeDetails[0].NomLastName;
                                }
                                strNewValue = (isNull(nominee.NomFirstName).Trim() + " " + isNull(nominee.NomMiddleName).Trim() + " " + isNull(nominee.NomLastName).Trim());
                                if (strNewValue.Trim() != "" && (strOldValue.Trim() != strNewValue.Trim()))
                                {
                                    isNomChange = true;
                                }
                                strOldValue = "";
                                if (blnClientNomOld)
                                {
                                    strOldValue = oldModelValue.NomineeDetails[0].NomAddress1.ToUpper();
                                }
                                strNewValue = isNull(nominee.NomAddress1).Trim().ToUpper();
                                if (strNewValue.Trim() != "" && (strOldValue.Trim() != strNewValue.Trim()))
                                {
                                    isNomChange = true;
                                }
                                strOldValue = "";
                                if (blnClientNomOld)
                                {
                                    strOldValue = oldModelValue.NomineeDetails[0].NomAddress2.ToUpper();
                                }
                                strNewValue = isNull(nominee.NomAddress2).Trim().ToUpper();
                                if (strNewValue.Trim() != "" && (strOldValue.Trim() != strNewValue.Trim()))
                                {
                                    isNomChange = true;
                                }
                                strOldValue = "";
                                if (blnClientNomOld)
                                {
                                    strOldValue = oldModelValue.NomineeDetails[0].NomAddress3.ToUpper();
                                }
                                strNewValue = isNull(nominee.NomAddress3).Trim().ToUpper();
                                if (strNewValue.Trim() != "" && (strOldValue.Trim() != strNewValue.Trim()))
                                {
                                    isNomChange = true;
                                }
                                strOldValue = "";
                                if (blnClientNomOld)
                                {
                                    strOldValue = oldModelValue.NomineeDetails[0].NomPincode.ToUpper();
                                }
                                strNewValue = isNull(nominee.NomPincode).Trim().ToUpper();
                                if (strNewValue.Trim() != "" && (strOldValue.Trim() != strNewValue.Trim()))
                                {
                                    isNomChange = true;
                                }
                                strOldValue = "";
                                if (blnClientNomOld)
                                {
                                    strOldValue = oldModelValue.NomineeDetails[0].NomAddressCity.ToUpper();
                                }
                                strNewValue = isNull(nominee.NomAddressCity).Trim().ToUpper();
                                if (strNewValue.Trim() != "" && (strOldValue.Trim() != strNewValue.Trim()))
                                {
                                    isNomChange = true;
                                }
                                strOldValue = "";
                                if (blnClientNomOld)
                                {
                                    strOldValue = oldModelValue.NomineeDetails[0].NomAddressState.ToUpper();
                                }
                                strNewValue = isNull(nominee.NomAddressState).Trim().ToUpper();
                                if (strNewValue.Trim() != "" && (strOldValue.Trim() != strNewValue.Trim()))
                                {
                                    isNomChange = true;
                                }
                                strNewValue = isNull(nominee.NomAddressCountry).Trim().ToUpper();
                                strOldValue = "";
                                if (blnClientNomOld)
                                {
                                    strOldValue = oldModelValue.NomineeDetails[0].NomAddressCountry.ToUpper();
                                }
                                if (strNewValue.Trim() != "" && (strOldValue.Trim() != strNewValue.Trim()))
                                {
                                    isNomChange = true;
                                }
                                strNewValue = isNull(nominee.NomRelation).Trim().ToUpper();
                                strOldValue = "";
                                if (strNewValue.Trim() != "" && (strOldValue.Trim() != strNewValue.Trim()))
                                {
                                    isNomChange = true;
                                }
                                strOldValue = "";
                                if (blnClientNomOld)
                                {
                                    strOldValue = oldModelValue.NomineeDetails[0].NomineePAN.ToUpper();
                                }
                                strNewValue = isNull(nominee.NomineePAN).Trim().ToUpper();
                                if (strNewValue.Trim() != "" && (strOldValue.Trim() != strNewValue.Trim()))
                                {
                                    isNomChange = true;
                                }
                                strOldValue = "";
                                if (blnClientNomOld)
                                {
                                    strOldValue = oldModelValue.NomineeDetails[0].NomMobile.ToUpper();
                                }
                                strNewValue = isNull(nominee.NomMobile).Trim().ToUpper();
                                if (strNewValue.Trim() != "" && (strOldValue.Trim() != strNewValue.Trim()))
                                {
                                    isNomChange = true;
                                }
                                strNewValue = isNull(nominee.NomineeUID).Trim().ToUpper();
                                strOldValue = "";
                                if (strNewValue.Trim() != "")
                                {
                                    isNomChange = true;
                                }
                                strOldValue = "";
                                if (blnClientNomOld)
                                {
                                    strOldValue = oldModelValue.NomineeDetails[0].NomineeDOB.ToUpper();
                                }
                                strNewValue = isNull(nominee.NomineeDOB).Trim();
                                if (strNewValue.Trim() != "" && (strOldValue.Trim() != strNewValue.Trim()))
                                {
                                    isNomChange = true;
                                }
                                strNewValue = isNull(nominee.NomPercentage).Trim().ToUpper();
                                strOldValue = "";
                                if (strNewValue.Trim() != "" && (strOldValue.Trim() != strNewValue.Trim()))
                                {
                                    isNomChange = true;
                                }
                            }
                            if (k == 1 && isNomChange == true)
                            {
                                //Nominee Name
                                strOldValue = "";
                                if (blnClientNomOld)
                                {
                                    strOldValue = oldModelValue.NomineeDetails[0].NomFirstName + " " + oldModelValue.NomineeDetails[0].NomMiddleName + " " + oldModelValue.NomineeDetails[0].NomLastName;
                                }
                                strNewValue = (isNull(nominee.NomFirstName).Trim() + " " + isNull(nominee.NomMiddleName).Trim() + " " + isNull(nominee.NomLastName).Trim());
                                pdfModel.NomFullName1 = strNewValue;
                                strNewValue = isNull(nominee.NomFirstName).Trim();
                                if (strNewValue.Trim() != "" && (strOldValue.Trim() != strNewValue.Trim()))
                                {
                                    prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_firstname", "Nominee First Name", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                }
                                strNewValue = isNull(nominee.NomMiddleName).Trim();
                                if (strNewValue.Trim() != "" && (strOldValue.Trim() != strNewValue.Trim()))
                                {
                                    prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_middlename", "Nominee Middle Name", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                }
                                strNewValue = isNull(nominee.NomLastName).Trim();
                                if (strNewValue.Trim() != "" && (strOldValue.Trim() != strNewValue.Trim()))
                                {
                                    prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_lastname", "Nominee Last Name", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                }
                                //Nominee Add1
                                strOldValue = "";
                                if (blnClientNomOld)
                                {
                                    strOldValue = oldModelValue.NomineeDetails[0].NomAddress1.ToUpper();
                                }
                                strNewValue = isNull(nominee.NomAddress1).Trim().ToUpper();
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_add1", "Nominee Add1", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                pdfModel.NomAddress11 = strNewValue;
                                //Nominee Add2
                                strOldValue = "";
                                if (blnClientNomOld)
                                {
                                    strOldValue = oldModelValue.NomineeDetails[0].NomAddress2.ToUpper();
                                }
                                strNewValue = isNull(nominee.NomAddress2).Trim().ToUpper();
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_add2", "Nominee Add2", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                pdfModel.NomAddress21 = strNewValue;
                                //Nominee Add3
                                strOldValue = "";
                                strNewValue = isNull(nominee.NomAddress3).Trim().ToUpper();
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_add3", "Nominee Add3", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                pdfModel.NomAddress31 = strNewValue;
                                //Nominee PinCode
                                strOldValue = "";
                                if (blnClientNomOld)
                                {
                                    strOldValue = oldModelValue.NomineeDetails[0].NomPincode.ToUpper();
                                }
                                strNewValue = isNull(nominee.NomPincode).Trim().ToUpper();
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_pin", "Nominee PinCode", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                pdfModel.NomPincode1 = strNewValue;
                                //Nominee City
                                strOldValue = "";
                                if (blnClientNomOld)
                                {
                                    strOldValue = oldModelValue.NomineeDetails[0].NomAddressCity.ToUpper();
                                }
                                strNewValue = isNull(nominee.NomAddressCity).Trim().ToUpper();
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_city", "Nominee City", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                pdfModel.NomAddressCity1 = strNewValue;
                                //Nominee State
                                strOldValue = "";
                                if (blnClientNomOld)
                                {
                                    strOldValue = oldModelValue.NomineeDetails[0].NomAddressState.ToUpper();
                                }
                                strNewValue = isNull(nominee.NomAddressState).Trim().ToUpper();
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_state", "Nominee State", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                pdfModel.NomAddressState1 = strNewValue;
                                strNewValue = isNull(nominee.NomAddressCountry).Trim().ToUpper();
                                strOldValue = "";
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_country", "Nominee Country", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                pdfModel.NomAddressCountry1 = strNewValue;
                                strNewValue = isNull(nominee.NomRelation).Trim().ToUpper();
                                strOldValue = "";
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_relation", "Nominee Relation", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                pdfModel.NomRelation1 = utility.ReturnNomineeRelation(strNewValue);
                                //Nominee PAN
                                strOldValue = "";
                                if (blnClientNomOld)
                                {
                                    strOldValue = oldModelValue.NomineeDetails[0].NomineePAN.ToUpper();
                                }
                                strNewValue = isNull(nominee.NomineePAN).Trim().ToUpper();
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_pan", "Nominee PAN", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                pdfModel.NomineePAN1 = strNewValue; //"******" + strNewValue.Substring(6);
                                                                    //// Nominee Mobile
                                strOldValue = "";
                                if (blnClientNomOld)
                                {
                                    strOldValue = oldModelValue.NomineeDetails[0].NomMobile.ToUpper();
                                }
                                strNewValue = isNull(nominee.NomMobile).Trim().ToUpper();
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_tel", "Nominee Mobile", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                pdfModel.NomMobile1 = strNewValue;
                                //Nominee UID/Aadhar
                                strNewValue = isNull(nominee.NomineeUID).Trim().ToUpper();
                                strOldValue = "";
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_uid", "Nominee UID", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                pdfModel.NomineeUID1 = "xxxxxxxx" + strNewValue.Substring(8);
                                //Nominee Date of Birth
                                strOldValue = "";
                                if (blnClientNomOld)
                                {
                                    strOldValue = oldModelValue.NomineeDetails[0].NomineeDOB.ToUpper();
                                }
                                strNewValue = isNull(nominee.NomineeDOB).Trim();
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_dob", "Nominee Date of Birth", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                nomPdf.NomineeDOB = DateTime.ParseExact(strNewValue, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd-MM-yyyy");
                                pdfModel.NomineeDOB1 = DateTime.ParseExact(strNewValue, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd-MM-yyyy");
                                //Nominee Share percentage
                                strNewValue = isNull(nominee.NomPercentage).Trim().ToUpper();
                                strOldValue = "";
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_share", "Nominee Percentage", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                pdfModel.NomPercentage1 = strNewValue;
                                attachment = isNull(nominee.NomineeAttachment).ToString().Trim();
                                if (!string.IsNullOrEmpty(attachment))
                                {
                                    prCommonAttachmentInsert(ObjConnection, Code, "Nominee", attachment, "NomineeAttachment", objTransTplus, refNo, k, false);
                                }
                                if (nominee.GuardianDetails != null)
                                {
                                    Guardian grdPdf = new Guardian();
                                    //Nominee Guardian Name
                                    blnClientNomOld = false;
                                    if (oldModelValue.NomineeDetails[0].GuardianDetails.NomineeId > 0)
                                        blnClientNomOld = true;
                                    strOldValue = "";
                                    if (blnClientNomOld)
                                    {
                                        strOldValue = oldModelValue.NomineeDetails[0].GuardianDetails.NomGuardianFirstName + " " + oldModelValue.NomineeDetails[0].GuardianDetails.NomGuardianMiddleName + " " + oldModelValue.NomineeDetails[0].GuardianDetails.NomGuardianLastName;
                                    }
                                    strNewValue = (isNull(nominee.GuardianDetails.NomGuardianFirstName).Trim() + " " + isNull(nominee.GuardianDetails.NomGuardianMiddleName).Trim() + " " + isNull(nominee.GuardianDetails.NomGuardianLastName).Trim());
                                    pdfModel.NomGuardianFullName1 = strNewValue;
                                    strNewValue = isNull(nominee.GuardianDetails.NomGuardianFirstName).Trim();
                                    if (strNewValue.Trim() != "" && (strOldValue.Trim() != strNewValue.Trim().ToUpper()))
                                    {
                                        prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_gfirstname", "Nominee Guardian First Name", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                    }
                                    strNewValue = isNull(nominee.GuardianDetails.NomGuardianMiddleName).Trim();
                                    if (strNewValue.Trim() != "" && (strOldValue.Trim() != strNewValue.Trim().ToUpper()))
                                    {
                                        prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_gmiddlename", "Nominee Guardian Middle Name", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                    }
                                    strNewValue = isNull(nominee.GuardianDetails.NomGuardianLastName).Trim();
                                    if (strNewValue.Trim() != "" && (strOldValue.Trim() != strNewValue.Trim().ToUpper()))
                                    {
                                        prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_glastname", "Nominee Guardian Last Name", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                    }
                                    ///// Gaurdian Address 
                                    strOldValue = ""; //// address 1
                                    if (blnClientNomOld)
                                    {
                                        strOldValue = oldModelValue.NomineeDetails[0].GuardianDetails.NomGuardianAddress1.ToUpper();
                                    }
                                    strNewValue = isNull(nominee.GuardianDetails.NomGuardianAddress1).Trim();

                                    prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_gadd1", "Nominee Guardian Address1", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                    pdfModel.NomGuardianAddress11 = strNewValue;
                                    strOldValue = "";  //// address 2
                                    if (blnClientNomOld)
                                    {
                                        strOldValue = oldModelValue.NomineeDetails[0].GuardianDetails.NomGuardianAddress2.ToUpper();
                                    }
                                    strNewValue = isNull(nominee.GuardianDetails.NomGuardianAddress2).Trim();
                                    prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_gadd2", "Nominee Guardian Address2", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                    pdfModel.NomGuardianAddress21 = strNewValue;
                                    strOldValue = "";  //// address 3
                                    strNewValue = isNull(nominee.GuardianDetails.NomGuardianAddress3).Trim();
                                    prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_gadd3", "Nominee Guardian Address3", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                    pdfModel.NomGuardianAddress31 = strNewValue;
                                    strOldValue = "";  //// Gaurdian City
                                    if (blnClientNomOld)
                                    {
                                        strOldValue = oldModelValue.NomineeDetails[0].GuardianDetails.NomGuardianCity.ToUpper();
                                    }
                                    strNewValue = isNull(nominee.GuardianDetails.NomGuardianCity).Trim();
                                    prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_gcity", "Nominee Guardian City", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                    pdfModel.NomGuardianCity1 = strNewValue;
                                    strOldValue = "";  //// Gaurdian State
                                    if (blnClientNomOld)
                                    {
                                        strOldValue = oldModelValue.NomineeDetails[0].GuardianDetails.NomGuardianState.ToUpper();
                                    }
                                    strNewValue = isNull(nominee.GuardianDetails.NomGuardianState).Trim();
                                    prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_gstate", "Nominee Guardian State", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                    pdfModel.NomGuardianState1 = strNewValue;
                                    strOldValue = "";  //// Gaurdian State
                                    if (blnClientNomOld)
                                    {
                                        strOldValue = oldModelValue.NomineeDetails[0].GuardianDetails.NomGuardianPincode.ToUpper();
                                    }
                                    strNewValue = isNull(nominee.GuardianDetails.NomGuardianPincode).Trim();
                                    prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_gpin", "Nominee Guardian Pincode", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                    pdfModel.NomGuardianPincode1 = strNewValue;
                                    if (blnClientNomOld)
                                    {
                                        strOldValue = oldModelValue.NomineeDetails[0].GuardianDetails.NomGuardianMobile.ToUpper();
                                    }
                                    strNewValue = isNull(nominee.GuardianDetails.NomGuardianMobile).Trim().ToUpper();
                                    prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_gtel", "Nominee Guardian Mobile", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                    pdfModel.NomGuardianMobile1 = strNewValue;
                                    strNewValue = isNull(nominee.GuardianDetails.NomGuardianEmail).Trim().ToUpper();
                                    strOldValue = "";
                                    prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_gmail", "Nominee Guardian Email", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                    pdfModel.NomGuardianEmail1 = strNewValue;
                                    strNewValue = isNull(nominee.GuardianDetails.NomGuardianDob).Trim().ToUpper();
                                    strOldValue = "";
                                    prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_gdob", "Nominee Guardian Dob", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                    string dobG = DateTime.ParseExact(strNewValue, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd-MM-yyyy");
                                    pdfModel.NomGuardianDob1 = dobG;
                                    strNewValue = isNull(nominee.GuardianDetails.NomGuardianRelation).Trim().ToUpper();
                                    strOldValue = "";
                                    prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_grelation", "Nominee Guardian Relation", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                    string relG = utility.ReturnNomineeRelation(strNewValue);
                                    pdfModel.NomGuardianRelation1 = relG;
                                    //Nominee Guardian PAN
                                    strOldValue = "";
                                    if (blnClientNomOld)
                                    {
                                        strOldValue = oldModelValue.NomineeDetails[0].GuardianDetails.NomGuardianPAN.ToUpper();
                                    }
                                    strNewValue = isNull(nominee.GuardianDetails.NomGuardianPAN).Trim().ToUpper();
                                    if (!string.IsNullOrEmpty(strNewValue))
                                    {
                                        prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_gpan", "Nominee Guardian PAN", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                        pdfModel.NomGuardianPAN1 = strNewValue;
                                    }
                                    else
                                    {
                                        strNewValue = isNull(nominee.GuardianDetails.NomGuardianUID).Trim().ToUpper();
                                        prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_guid", "Nominee Guardian UID", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                        pdfModel.NomGuardianUID1 = "xxxxxxxx" + strNewValue.Substring(8);
                                    }
                                }
                            }
                            else if (k > 1)
                            {
                                //Nominee Name
                                strOldValue = "";
                                strNewValue = (isNull(nominee.NomFirstName).Trim() + " " + isNull(nominee.NomMiddleName).Trim() + " " + isNull(nominee.NomLastName).Trim());
                                if (k == 2)
                                {
                                    pdfModel.NomFullName2 = strNewValue;
                                }
                                else
                                {
                                    pdfModel.NomFullName3 = strNewValue;
                                }
                                strNewValue = isNull(nominee.NomFirstName).Trim();
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_firstname", "Nominee First Name", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);

                                strNewValue = isNull(nominee.NomMiddleName).Trim();
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_middlename", "Nominee Middle Name", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);

                                strNewValue = isNull(nominee.NomLastName).Trim();
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_lastname", "Nominee Last Name", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                //Nominee Add1
                                strNewValue = isNull(nominee.NomAddress1).Trim().ToUpper();
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_add1", "Nominee Add1", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                if (k == 2)
                                {
                                    pdfModel.NomAddress12 = strNewValue;
                                }
                                else
                                {
                                    pdfModel.NomAddress13 = strNewValue;
                                }
                                //Nominee Add2
                                strNewValue = isNull(nominee.NomAddress2).Trim().ToUpper();
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_add2", "Nominee Add2", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                if (k == 2)
                                {
                                    pdfModel.NomAddress22 = strNewValue;
                                }
                                else
                                {
                                    pdfModel.NomAddress23 = strNewValue;
                                }
                                //Nominee Add3
                                strOldValue = "";
                                strNewValue = isNull(nominee.NomAddress3).Trim().ToUpper();
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_add3", "Nominee Add3", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                if (k == 2)
                                {
                                    pdfModel.NomAddress32 = strNewValue;
                                }
                                else
                                {
                                    pdfModel.NomAddress33 = strNewValue;
                                }
                                //Nominee PinCode
                                strNewValue = isNull(nominee.NomPincode).Trim().ToUpper();
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_pin", "Nominee PinCode", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                if (k == 2)
                                {
                                    pdfModel.NomPincode2 = strNewValue;
                                }
                                else
                                {
                                    pdfModel.NomPincode3 = strNewValue;
                                }
                                //Nominee City
                                strNewValue = isNull(nominee.NomAddressCity).Trim().ToUpper();
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_city", "Nominee City", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                if (k == 2)
                                {
                                    pdfModel.NomAddressCity2 = strNewValue;
                                }
                                else
                                {
                                    pdfModel.NomAddressCity3 = strNewValue;
                                }
                                //Nominee State
                                strNewValue = isNull(nominee.NomAddressState).Trim().ToUpper();
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_state", "Nominee State", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                if (k == 2)
                                {
                                    pdfModel.NomAddressState2 = strNewValue;
                                }
                                else
                                {
                                    pdfModel.NomAddressState3 = strNewValue;
                                }
                                strNewValue = isNull(nominee.NomAddressCountry).Trim().ToUpper();
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_country", "Nominee Country", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                if (k == 2)
                                {
                                    pdfModel.NomAddressCountry2 = strNewValue;
                                }
                                else
                                {
                                    pdfModel.NomAddressCountry3 = strNewValue;
                                }
                                strNewValue = isNull(nominee.NomRelation).Trim().ToUpper();
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_relation", "Nominee Relation", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                if (k == 2)
                                {
                                    pdfModel.NomRelation2 = utility.ReturnNomineeRelation(strNewValue);
                                }
                                else
                                {
                                    pdfModel.NomRelation3 = utility.ReturnNomineeRelation(strNewValue);
                                }
                                //Nominee PAN
                                strNewValue = isNull(nominee.NomineePAN).Trim().ToUpper();
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_pan", "Nominee PAN", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                if (k == 2)
                                {
                                    pdfModel.NomineePAN2 = strNewValue;
                                }
                                else
                                {
                                    pdfModel.NomineePAN3 = strNewValue;
                                }
                                //// Nominee Mobile
                                strNewValue = isNull(nominee.NomMobile).Trim().ToUpper();
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_tel", "Nominee Mobile", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                if (k == 2)
                                {
                                    pdfModel.NomMobile2 = strNewValue;
                                }
                                else
                                {
                                    pdfModel.NomMobile3 = strNewValue;
                                }
                                //Nominee UID/Aadhar
                                strNewValue = isNull(nominee.NomineeUID).Trim().ToUpper();
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_uid", "Nominee UID", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                if (k == 2)
                                {
                                    pdfModel.NomineeUID2 = "xxxxxxxx" + strNewValue.Substring(8);
                                }
                                else
                                {
                                    pdfModel.NomineeUID3 = "xxxxxxxx" + strNewValue.Substring(8);
                                }
                                //Nominee Date of Birth
                                strNewValue = isNull(nominee.NomineeDOB).Trim();
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_dob", "Nominee Date of Birth", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                nomPdf.NomineeDOB = DateTime.ParseExact(strNewValue, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd-MM-yyyy");
                                if (k == 2)
                                {
                                    pdfModel.NomineeDOB2 = DateTime.ParseExact(strNewValue, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd-MM-yyyy");
                                }
                                else
                                {
                                    pdfModel.NomineeDOB3 = DateTime.ParseExact(strNewValue, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd-MM-yyyy");
                                }
                                //Nominee Share percentage
                                strNewValue = isNull(nominee.NomPercentage).Trim().ToUpper();
                                prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_share", "Nominee Percentage", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                if (k == 2)
                                {
                                    pdfModel.NomPercentage2 = strNewValue;
                                }
                                else
                                {
                                    pdfModel.NomPercentage3 = strNewValue;
                                }
                                attachment = isNull(nominee.NomineeAttachment).ToString().Trim();
                                if (!string.IsNullOrEmpty(attachment))
                                {
                                    prCommonAttachmentInsert(ObjConnection, Code, "Nominee", attachment, "NomineeAttachment", objTransTplus, refNo, k, false);
                                }
                                if (nominee.GuardianDetails != null)
                                {
                                    Guardian grdPdf = new Guardian();
                                    //Nominee Guardian Name
                                    strOldValue = "";
                                    strNewValue = (isNull(nominee.GuardianDetails.NomGuardianFirstName).Trim() + " " + isNull(nominee.GuardianDetails.NomGuardianMiddleName).Trim() + " " + isNull(nominee.GuardianDetails.NomGuardianLastName).Trim());
                                    if (k == 2)
                                    {
                                        pdfModel.NomGuardianFullName2 = strNewValue;
                                    }
                                    else
                                    {
                                        pdfModel.NomGuardianFullName3 = strNewValue;
                                    }
                                    strNewValue = isNull(nominee.GuardianDetails.NomGuardianFirstName).Trim();
                                    prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_gfirstname", "Nominee Guardian First Name", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);

                                    strNewValue = isNull(nominee.GuardianDetails.NomGuardianMiddleName).Trim();
                                    prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_gmiddlename", "Nominee Guardian Middle Name", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);

                                    strNewValue = isNull(nominee.GuardianDetails.NomGuardianLastName).Trim();
                                    prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_glastname", "Nominee Guardian Last Name", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                    ///// Gaurdian Address 
                                    strNewValue = isNull(nominee.GuardianDetails.NomGuardianAddress1).Trim();
                                    prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_gadd1", "Nominee Guardian Address1", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                    if (k == 2)
                                    {
                                        pdfModel.NomGuardianAddress12 = strNewValue;
                                    }
                                    else
                                    {
                                        pdfModel.NomGuardianAddress13 = strNewValue;
                                    }
                                    strNewValue = isNull(nominee.GuardianDetails.NomGuardianAddress2).Trim();
                                    prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_gadd2", "Nominee Guardian Address2", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                    if (k == 2)
                                    {
                                        pdfModel.NomGuardianAddress22 = strNewValue;
                                    }
                                    else
                                    {
                                        pdfModel.NomGuardianAddress23 = strNewValue;
                                    }
                                    strOldValue = "";  //// address 3
                                    strNewValue = isNull(nominee.GuardianDetails.NomGuardianAddress3).Trim();
                                    prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_gadd3", "Nominee Guardian Address3", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                    if (k == 2)
                                    {
                                        pdfModel.NomGuardianAddress32 = strNewValue;
                                    }
                                    else
                                    {
                                        pdfModel.NomGuardianAddress33 = strNewValue;
                                    }
                                    strNewValue = isNull(nominee.GuardianDetails.NomGuardianCity).Trim();
                                    prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_gcity", "Nominee Guardian City", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                    if (k == 2)
                                    {
                                        pdfModel.NomGuardianCity2 = strNewValue;
                                    }
                                    else
                                    {
                                        pdfModel.NomGuardianCity3 = strNewValue;
                                    }
                                    strNewValue = isNull(nominee.GuardianDetails.NomGuardianState).Trim();
                                    prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_gstate", "Nominee Guardian State", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                    if (k == 2)
                                    {
                                        pdfModel.NomGuardianState2 = strNewValue;
                                    }
                                    else
                                    {
                                        pdfModel.NomGuardianState3 = strNewValue;
                                    }
                                    strNewValue = isNull(nominee.GuardianDetails.NomGuardianPincode).Trim();
                                    prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_gpin", "Nominee Guardian Pincode", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                    if (k == 2)
                                    {
                                        pdfModel.NomGuardianPincode2 = strNewValue;
                                    }
                                    else
                                    {
                                        pdfModel.NomGuardianPincode3 = strNewValue;
                                    }
                                    strNewValue = isNull(nominee.GuardianDetails.NomGuardianMobile).Trim().ToUpper();
                                    prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_gtel", "Nominee Guardian Mobile", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                    if (k == 2)
                                    {
                                        pdfModel.NomGuardianMobile2 = strNewValue;
                                    }
                                    else
                                    {
                                        pdfModel.NomGuardianMobile3 = strNewValue;
                                    }
                                    strNewValue = isNull(nominee.GuardianDetails.NomGuardianEmail).Trim().ToUpper();
                                    prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_gmail", "Nominee Guardian Email", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                    if (k == 2)
                                    {
                                        pdfModel.NomGuardianEmail2 = strNewValue;
                                    }
                                    else
                                    {
                                        pdfModel.NomGuardianEmail3 = strNewValue;
                                    }
                                    strNewValue = isNull(nominee.GuardianDetails.NomGuardianDob).Trim().ToUpper();
                                    prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_gdob", "Nominee Guardian Dob", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                    string dobG = DateTime.ParseExact(strNewValue, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd-MM-yyyy");
                                    if (k == 2)
                                    {
                                        pdfModel.NomGuardianDob2 = dobG;
                                    }
                                    else
                                    {
                                        pdfModel.NomGuardianDob3 = dobG;
                                    }
                                    strNewValue = isNull(nominee.GuardianDetails.NomGuardianRelation).Trim().ToUpper();
                                    prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_grelation", "Nominee Guardian Relation", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                    string relG = utility.ReturnNomineeRelation(strNewValue);
                                    if (k == 2)
                                    {
                                        pdfModel.NomGuardianRelation2 = relG;
                                    }
                                    else
                                    {
                                        pdfModel.NomGuardianRelation3 = relG;
                                    }
                                    //Nominee Guardian PAN
                                    strNewValue = isNull(nominee.GuardianDetails.NomGuardianPAN).Trim().ToUpper();
                                    if (!string.IsNullOrEmpty(strNewValue))
                                    {
                                        prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_gpan", "Nominee Guardian PAN", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                        if (k == 2)
                                        {
                                            pdfModel.NomGuardianPAN2 = strNewValue;
                                        }
                                        else
                                        {
                                            pdfModel.NomGuardianPAN3 = strNewValue;
                                        }
                                    }
                                    else //Nominee Guardian UID
                                    {
                                        strNewValue = isNull(nominee.GuardianDetails.NomGuardianUID).Trim().ToUpper();
                                        prCommonInsert(ObjConnection, nominee.IsNomDeleted, "Client Nominee", "Client_Nominee", "cn_cd", "Client Code", Code, "cn_guid", "Nominee Guardian UID", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                        if (k == 2)
                                        {
                                            pdfModel.NomGuardianUID2 = "xxxxxxxx" + strNewValue.Substring(8);
                                        }
                                        else
                                        {
                                            pdfModel.NomGuardianUID3 = "xxxxxxxx" + strNewValue.Substring(8);
                                        }
                                    }

                                }
                                if (nomPdf != null)
                                {
                                    nomPdfList.Add(nomPdf);
                                }
                            }
                            k++;
                        }
                        //if (nomPdfList.Count > 0)
                        //{
                        //    pdfModel.NomineeDetailsPdf = nomPdfList;
                        //}
                    }
                    //// Bank Details
                    int cnt = 1;
                    if (cm.BankDetails != null)
                    {
                        k = 1;
                        List<Bank> bankPdfList = new List<Bank>();
                        foreach (var bank in cm.BankDetails)
                        {
                            bool blnifsc = false;
                            strSQL = "Select count(0) from sysobjects a (NoLock), syscolumns b (NoLock)  Where a.id=b.id and a.name='Bankact'  and b.name='ba_ifsccode'";
                            DataTable dtChk = utility.OpenDataTable(strSQL, ObjConnection, objTransTplus);
                            if (Conversion.Val(dtChk.Rows[0][0].ToString()) > 0)
                            {
                                blnifsc = true;
                            }
                            string bnkmicr = isNull(bank.BankMICR);
                            string bnkifsc = "";
                            if (blnifsc)
                            {
                                bnkifsc = bank.BankIFSC;
                            }
                            string bnkactype = bank.AccountType; // isNull(cm.BankAccType);
                            string bnkacc = isNull(bank.BankAccNo);
                            strSQL = "Select * from BankAct (NoLock) Where ba_clientcd = '" + Code + "' and ba_actno = '" + bnkacc + "' and ba_micr='" + bnkmicr + "' and ba_ifsccode='" + bnkifsc + "' ";
                            var dbBank = utility.OpenDataTable(strSQL, ObjConnection, objTransTplus);
                            if (bnkmicr.Trim() != "" || bnkifsc.Trim() != "")
                            {
                                string strbankacno = "";
                                string strbankCode = "";
                                string strbankIFSC = "";
                                string strbankActype = "";
                                string strbankDefault = "";
                                string strbankProof = "";
                                var tempIsDefault = (bank.IsDefault ? "Y" : "N");
                                if (dbBank.Rows.Count > 0)
                                {
                                    strbankacno = dbBank.Rows[0]["ba_actno"].ToString().Trim();
                                    strbankCode = dbBank.Rows[0]["ba_micr"].ToString().Trim();
                                    strbankIFSC = dbBank.Rows[0]["ba_ifsccode"].ToString().Trim();
                                    strbankActype = dbBank.Rows[0]["ba_acttype"].ToString().Trim();
                                    strbankDefault = dbBank.Rows[0]["ba_default"].ToString().Trim();
                                    strbankProof = dbBank.Rows[0]["ba_proof"].ToString().Trim();
                                    if (tempIsDefault != strbankDefault || bnkactype != strbankActype)
                                    {
                                        if ((!string.IsNullOrWhiteSpace(bnkmicr) || !string.IsNullOrWhiteSpace(bnkifsc)) && !string.IsNullOrWhiteSpace(bnkactype) && !string.IsNullOrWhiteSpace(bnkacc))
                                        {
                                            strOldValue = strbankCode;
                                            strNewValue = bnkmicr;
                                            prCommonInsert(ObjConnection, bank.IsBankDeleted, "Bank Act", "Bankact", "ba_clientcd", "Bank Code", Code, "ba_micr", "MICR", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                            prCommonInsert(ObjConnection, bank.IsBankDeleted, "Bank Act", "Bankact", "ba_clientcd", "Bank Code", Code, "ba_ifsccode", "IFSC Code", strbankIFSC, bnkifsc, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                            prCommonInsert(ObjConnection, bank.IsBankDeleted, "Bank Act", "Bankact", "ba_clientcd", "Bank Code", Code, "ba_actno", "Account No.", strbankacno, bnkacc, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                            prCommonInsert(ObjConnection, bank.IsBankDeleted, "Bank Act", "Bankact", "ba_clientcd", "Bank Code", Code, "ba_default", "Default Bank for MICR (" + bnkmicr + ") A/c change defualt value for exisitng default bank", strbankDefault, tempIsDefault, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                            prCommonInsert(ObjConnection, bank.IsBankDeleted, "Bank Act", "Bankact", "ba_clientcd", "Bank Code", Code, "ba_acttype", "Account Type", strbankActype, bnkactype, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                            if (cnt == 1)
                                            {
                                                pdfModel.BankAccNo1 = bank.BankAccNo;
                                                pdfModel.BankMICR1 = bank.BankMICR;
                                                pdfModel.BankIFSC1 = bank.BankIFSC;
                                                pdfModel.IsDefault1 = bank.IsDefault;
                                                pdfModel.AccountType1 = bank.AccountType;
                                                pdfModel.IsBankShow1 = true;
                                            }
                                            else if (cnt == 2)
                                            {
                                                pdfModel.BankAccNo2 = bank.BankAccNo;
                                                pdfModel.BankMICR2 = bank.BankMICR;
                                                pdfModel.BankIFSC2 = bank.BankIFSC;
                                                pdfModel.IsDefault2 = bank.IsDefault;
                                                pdfModel.AccountType2 = bank.AccountType;
                                                pdfModel.IsBankShow2 = true;
                                            }
                                            else if (cnt == 3)
                                            {
                                                pdfModel.BankAccNo3 = bank.BankAccNo;
                                                pdfModel.BankMICR3 = bank.BankMICR;
                                                pdfModel.BankIFSC3 = bank.BankIFSC;
                                                pdfModel.IsDefault3 = bank.IsDefault;
                                                pdfModel.AccountType3 = bank.AccountType;
                                                pdfModel.IsBankShow3 = true;
                                            }
                                            cnt++;
                                        }
                                    }
                                }
                                else if (dbBank.Rows.Count == 0)
                                {
                                    strOldValue = strbankCode;
                                    strNewValue = bnkmicr;
                                    if ((!string.IsNullOrWhiteSpace(bnkmicr) || !string.IsNullOrWhiteSpace(bnkifsc)) && !string.IsNullOrWhiteSpace(bnkactype) && !string.IsNullOrWhiteSpace(bnkacc))
                                    {
                                        prCommonInsert(ObjConnection, bank.IsBankDeleted, "Bank Act", "Bankact", "ba_clientcd", "Bank Code", Code, "ba_micr", "MICR", strOldValue, strNewValue, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                        prCommonInsert(ObjConnection, bank.IsBankDeleted, "Bank Act", "Bankact", "ba_clientcd", "Bank Code", Code, "ba_ifsccode", "IFSC Code", strbankIFSC, bnkifsc, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                        prCommonInsert(ObjConnection, bank.IsBankDeleted, "Bank Act", "Bankact", "ba_clientcd", "Bank Code", Code, "ba_actno", "Account No.", strbankacno, bnkacc, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                        prCommonInsert(ObjConnection, bank.IsBankDeleted, "Bank Act", "Bankact", "ba_clientcd", "Bank Code", Code, "ba_default", "Default Bank for MICR (" + bnkmicr + ") A/c", strbankDefault, tempIsDefault, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                        prCommonInsert(ObjConnection, bank.IsBankDeleted, "Bank Act", "Bankact", "ba_clientcd", "Bank Code", Code, "ba_acttype", "Account Type", strbankDefault, bnkactype, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                        attachment = isNull(bank.BankCCAttachment).ToString().Trim();
                                        if (!string.IsNullOrEmpty(attachment))
                                        {
                                            prCommonAttachmentInsert(ObjConnection, Code, "Bank", attachment, "BankAttachment", objTransTplus, refNo, k, false);
                                        }
                                        if (cnt == 1)
                                        {
                                            pdfModel.BankAccNo1 = bank.BankAccNo;
                                            pdfModel.BankMICR1 = bank.BankMICR;
                                            pdfModel.BankIFSC1 = bank.BankIFSC;
                                            pdfModel.IsDefault1 = bank.IsDefault;
                                            pdfModel.AccountType1 = bank.AccountType;
                                            pdfModel.IsBankShow1 = true;
                                        }
                                        else if (cnt == 2)
                                        {
                                            pdfModel.BankAccNo2 = bank.BankAccNo;
                                            pdfModel.BankMICR2 = bank.BankMICR;
                                            pdfModel.BankIFSC2 = bank.BankIFSC;
                                            pdfModel.IsDefault2 = bank.IsDefault;
                                            pdfModel.AccountType2 = bank.AccountType;
                                            pdfModel.IsBankShow2 = true;
                                        }
                                        else if (cnt == 3)
                                        {
                                            pdfModel.BankAccNo3 = bank.BankAccNo;
                                            pdfModel.BankMICR3 = bank.BankMICR;
                                            pdfModel.BankIFSC3 = bank.BankIFSC;
                                            pdfModel.IsDefault3 = bank.IsDefault;
                                            pdfModel.AccountType3 = bank.AccountType;
                                            pdfModel.IsBankShow3 = true;
                                        }
                                        cnt++;
                                        //Bank bnkPdf = new Bank();
                                        //bnkPdf.BankMICR = strNewValue;
                                        //bnkPdf.BankIFSC = bank.BankIFSC;
                                        //bnkPdf.BankAccNo = bank.BankAccNo;
                                        //bnkPdf.IsDefault = bank.IsDefault;
                                        //bnkPdf.AccountType = bank.AccountType;
                                        //bankPdfList.Add(bnkPdf);
                                    }
                                }
                            }
                            k++;
                        }
                        //if (bankPdfList.Count > 0)
                        //{
                        //    pdfModel.BankDetails = bankPdfList;
                        //}
                    }
                    //// Demat Details
                    if (cm.DematDetails != null)
                    {
                        k = 1;
                        cnt = 1;
                        List<Demat> dematPdfList = new List<Demat>();
                        foreach (var demat in cm.DematDetails)
                        {
                            if (isNull(demat.DPID) != "" && isNull(demat.DPAcNo) != "")
                            {
                                string dpid = isNull(demat.DPID);
                                string dpacno = "";
                                string dpDefault = (demat.IsDefault ? "Y" : "N");
                                if (Strings.Left(isNull(demat.DPAcNo).Trim(), 2) == "IN")
                                {
                                    dpacno = Strings.Right(isNull(demat.DPAcNo).Trim(), 8);
                                }
                                else
                                {
                                    dpacno = isNull(demat.DPAcNo);
                                }
                                strSQL = $"Select * from Dematact (NoLock) Where da_clientcd = '{Code}' and da_actno = '{dpacno}' and da_dpid='" + dpid + "'";
                                DataTable dbDmt = utility.OpenDataTable(strSQL, ObjConnection, objTransTplus);
                                if (dbDmt.Rows.Count > 0)
                                {
                                    string dpname = utility.fnFireQuery("DPS", "dp_name", "dp_dpid", dpid, true, ObjConnection, objTransTplus);
                                    if (dpDefault != dbDmt.Rows[0]["da_defaultyn"].ToString().Trim())
                                    {
                                        if (!string.IsNullOrWhiteSpace(dpid) && !string.IsNullOrWhiteSpace(dpacno))
                                        {
                                            string oldDPType = dbDmt.Rows[0]["da_dpid"].ToString();
                                            oldDPType = (Strings.Left(oldDPType, 2) == "IN" ? "NSDL" : "CDSL");
                                            prCommonInsert(ObjConnection, demat.IsDematDeleted, "Demat Act", "Dematact", "da_clientcd", "Demat Code", Code, "da_dpid", "Demat A/c DPID", dbDmt.Rows[0]["da_dpid"].ToString(), dpid, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                            prCommonInsert(ObjConnection, demat.IsDematDeleted, "Demat Act", "Dematact", "da_clientcd", "Demat Code", Code, "da_actno", "Demat A/c no", dbDmt.Rows[0]["da_actno"].ToString(), dpacno, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                            prCommonInsert(ObjConnection, demat.IsDematDeleted, "Demat Act", "Dematact", "da_clientcd", "Demat Code", Code, "da_name", "Demat A/c name", dbDmt.Rows[0]["da_name"].ToString(), dpname, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                            prCommonInsert(ObjConnection, demat.IsDematDeleted, "Demat Act", "Dematact", "da_clientcd", "Demat Ac Code", Code, "da_defaultyn", "Default Demat for DPID (" + dpid + ") change old defualt value", dbDmt.Rows[0]["da_defaultyn"].ToString(), dpDefault, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                            prCommonInsert(ObjConnection, demat.IsDematDeleted, "Demat Act", "Dematact", "da_clientcd", "Demat Ac Code", Code, "da_type", "Demat A/c Type", oldDPType, demat.DematAccountType, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                            attachment = isNull(demat.DPAttachment).ToString().Trim();
                                            if (!string.IsNullOrEmpty(attachment))
                                            {
                                                prCommonAttachmentInsert(ObjConnection, Code, "Demat", attachment, "DematAttachment", objTransTplus, refNo, k, false);
                                            }
                                            if (cnt == 1)
                                            {
                                                pdfModel.DPID1 = demat.DPID;
                                                pdfModel.DPAcNo1 = demat.DPAcNo;
                                                pdfModel.IsDematDefault1 = demat.IsDefault;
                                                pdfModel.DPType1 = demat.DematAccountType;
                                                pdfModel.IsDematShow1 = true;
                                            }
                                            else if (cnt == 2)
                                            {
                                                pdfModel.DPID2 = demat.DPID;
                                                pdfModel.DPAcNo2 = demat.DPAcNo;
                                                pdfModel.IsDematDefault2 = demat.IsDefault;
                                                pdfModel.DPType2 = demat.DematAccountType;
                                                pdfModel.IsDematShow2 = true;
                                            }
                                            else if (cnt == 3)
                                            {
                                                pdfModel.DPID3 = demat.DPID;
                                                pdfModel.DPAcNo3 = demat.DPAcNo;
                                                pdfModel.IsDematDefault3 = demat.IsDefault;
                                                pdfModel.DPType3 = demat.DematAccountType;
                                                pdfModel.IsDematShow3 = true;
                                            }
                                            cnt++;
                                        }
                                    }
                                }
                                else
                                {
                                    string dpname = utility.fnFireQuery("DPS", "dp_name", "dp_dpid", dpid, true, ObjConnection, objTransTplus);
                                    strOldValue = "";
                                    if (!string.IsNullOrWhiteSpace(dpid) && !string.IsNullOrWhiteSpace(dpacno))
                                    {
                                        prCommonInsert(ObjConnection, demat.IsDematDeleted, "Demat Act", "Dematact", "da_clientcd", "Demat Code", Code, "da_dpid", "Demat A/c DPID", strOldValue, dpid, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                        prCommonInsert(ObjConnection, demat.IsDematDeleted, "Demat Act", "Dematact", "da_clientcd", "Demat Code", Code, "da_actno", "Demat A/c no", strOldValue, dpacno, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                        prCommonInsert(ObjConnection, demat.IsDematDeleted, "Demat Act", "Dematact", "da_clientcd", "Demat Code", Code, "da_name", "Demat A/c name", strOldValue, dpname, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                        prCommonInsert(ObjConnection, demat.IsDematDeleted, "Demat Act", "Dematact", "da_clientcd", "Demat Ac Code", Code, "da_defaultyn", "Default Demat for DPID (" + dpid + ")", strOldValue, dpDefault, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                        prCommonInsert(ObjConnection, demat.IsDematDeleted, "Demat Act", "Dematact", "da_clientcd", "Demat Ac Code", Code, "da_type", "Demat A/c Type", "", demat.DematAccountType, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo, k);
                                        attachment = isNull(demat.DPAttachment).ToString().Trim();
                                        if (!string.IsNullOrEmpty(attachment))
                                        {
                                            prCommonAttachmentInsert(ObjConnection, Code, "Demat", attachment, "DematAttachment", objTransTplus, refNo, k, false);
                                        }
                                        if (cnt == 1)
                                        {
                                            pdfModel.DPID1 = demat.DPID;
                                            pdfModel.DPAcNo1 = demat.DPAcNo;
                                            pdfModel.IsDematDefault1 = demat.IsDefault;
                                            pdfModel.DPType1 = demat.DematAccountType;
                                            pdfModel.IsDematShow1 = true;
                                        }
                                        else if (cnt == 2)
                                        {
                                            pdfModel.DPID2 = demat.DPID;
                                            pdfModel.DPAcNo2 = demat.DPAcNo;
                                            pdfModel.IsDematDefault2 = demat.IsDefault;
                                            pdfModel.DPType2 = demat.DematAccountType;
                                            pdfModel.IsDematShow2 = true;
                                        }
                                        else if (cnt == 3)
                                        {
                                            pdfModel.DPID3 = demat.DPID;
                                            pdfModel.DPAcNo3 = demat.DPAcNo;
                                            pdfModel.IsDematDefault3 = demat.IsDefault;
                                            pdfModel.DPType3 = demat.DematAccountType;
                                            pdfModel.IsDematShow3 = true;
                                        }
                                        cnt++;
                                        //Demat dmtPdf = new Demat();
                                        //dmtPdf.DPID = demat.DPID;
                                        //dmtPdf.DPAcNo = demat.DPAcNo;
                                        //dmtPdf.IsDefault = demat.IsDefault;
                                        //dmtPdf.DematAccountType = demat.DematAccountType;
                                        //dematPdfList.Add(dmtPdf);
                                    }
                                }
                                //// For saving DP_Type value
                            }
                            k++;
                        }
                        //if (dematPdfList.Count > 0)
                        //{
                        //    pdfModel.DematDetails = dematPdfList;
                        //}
                    }
                    //// Segment Details
                    if (cm.SegmentDetails != null)
                    {
                        var AllSegmentList = new List<string>();
                        AllSegmentList.AddRange(cm.SegmentDetails.CapitalMarket);
                        AllSegmentList.AddRange(cm.SegmentDetails.FO);
                        AllSegmentList.AddRange(cm.SegmentDetails.CurrencyDerivatives);
                        AllSegmentList.AddRange(cm.SegmentDetails.Commodities);
                        AllSegmentList.AddRange(cm.SegmentDetails.MF);
                        string NewSegmentList = string.IsNullOrWhiteSpace(string.Join(",", AllSegmentList.Select(x => "'" + x + "'"))) ? "" : string.Join(",", AllSegmentList.Select(x => "" + x + "").OrderBy(x => x));

                        string sgmStr = "Select ce_companycode as OldCode from client_details with(NoLock) Where ce_clientcd = '" + Code + "' and Isnull(ce_regDt,'')<>'' and ce_companycode IN (Select Distinct CES_Cd From CompanyExchangeSegments (NoLock)) Order by ce_companycode Asc";
                        DataTable sgmDt = utility.OpenDataTable(sgmStr, ObjConnection, objTransTplus);
                        string sgmStrCommex = "Select ce_companycode as OldCode from " + conCmx + "client_details with(NoLock) Where ce_clientcd = '" + Code + "' and Isnull(ce_regDt,'')<>'' and ce_companycode IN (Select Distinct CES_Cd From commex.dbo.CompanyExchangeSegments (NoLock)) Order by ce_companycode Asc";
                        DataTable sgmDtComx = utility.OpenDataTable(sgmStrCommex, ObjConnection, objTransTplus);
                        for (int i = 0; i < sgmDtComx.Rows.Count - 1; i++)
                        {
                            sgmDt.Rows.Add(new Object[] { sgmDtComx.Rows[i][0].ToString() });
                        }
                        var Oldlist = sgmDt.AsEnumerable().Select(r => r["OldCode"].ToString());
                        string OldSegmentList = string.Join(",", Oldlist.Select(x => "" + x + "").OrderBy(x => x));
                        if (OldSegmentList != NewSegmentList)
                        {
                            prCommonInsert(ObjConnection, false, "Client Segment", "Client_Segment", "ce_cd", "Client Code", Code, "ce_companycode", "Segment Details", OldSegmentList, NewSegmentList, gstrPCName, "API", gstrDBtoday, DateTime.Now.ToString("HH:mm:ss"), "", "", "00:00:00", objTransTplus, refNo);

                            pdfModel.IsSegmentModified = true;
                            pdfModel.SegmentDetailsOld = "";
                            pdfModel.SegmentDetailsNew = "";
                            string[] oldStr = OldSegmentList.Split(',');
                            string[] newStr = NewSegmentList.Split(',');
                            string sgmtOld = "", sgmtNew = "";
                            foreach (string old in oldStr)
                            {
                                if (!NewSegmentList.Contains(old))
                                {
                                    sgmtOld += "'" + old + "',";
                                }
                            }
                            foreach (string nw in newStr)
                            {
                                if (!OldSegmentList.Contains(nw))
                                {
                                    sgmtNew += "'" + nw + "',";
                                }
                            }
                            sgmtOld = sgmtOld.Trim() == "" ? "''" : sgmtOld.TrimEnd(',');
                            sgmtNew = sgmtNew.Trim() == "" ? "''" : sgmtNew.TrimEnd(',');
                            string sgmStrOld = "Select stuff((select ', ' + trim(CES_Exchange)+'/'+trim(CES_Segment)  from CompanyExchangeSegments where CES_Cd in (" + sgmtOld + ") for xml path('') ),1,1,'')";
                            DataTable sgmDtOld = utility.OpenDataTable(sgmStrOld, ObjConnection, objTransTplus);
                            DataTable sgmDtOldCmx = utility.OpenDataTable(sgmStrOld.Replace("CompanyExchangeSegments", conCmx + "CompanyExchangeSegments"), ObjConnection, objTransTplus);
                            if (sgmDtOld.Rows.Count > 0)
                            {
                                pdfModel.SegmentDetailsOld = isNull(sgmDtOld.Rows[0][0].ToString()) == "" ? "" : sgmDtOld.Rows[0][0].ToString();
                            }
                            if (sgmDtOldCmx.Rows.Count > 0)
                            {
                                pdfModel.SegmentDetailsOld += isNull(sgmDtOldCmx.Rows[0][0].ToString()) == "" ? "" : ", " + sgmDtOldCmx.Rows[0][0].ToString();
                            }
                            string sgmStrNew = "Select stuff((select ', ' + trim(CES_Exchange)+'/'+trim(CES_Segment)  from CompanyExchangeSegments where CES_Cd in (" + sgmtNew + ") for xml path('') ),1,1,'')";
                            DataTable sgmDtNew = utility.OpenDataTable(sgmStrNew, ObjConnection, objTransTplus);
                            DataTable sgmDtNewCmx = utility.OpenDataTable(sgmStrNew.Replace("CompanyExchangeSegments", conCmx + "CompanyExchangeSegments"), ObjConnection, objTransTplus);
                            if (sgmDtNew.Rows.Count > 0)
                            {
                                pdfModel.SegmentDetailsNew = isNull(sgmDtNew.Rows[0][0].ToString()) == "" ? "" : sgmDtNew.Rows[0][0].ToString();
                            }
                            if (sgmDtNewCmx.Rows.Count > 0)
                            {
                                pdfModel.SegmentDetailsNew += isNull(sgmDtNewCmx.Rows[0][0].ToString()) == "" ? "" : ", " + sgmDtNewCmx.Rows[0][0].ToString();
                            }
                            pdfModel.SegmentDetailsOld = pdfModel.SegmentDetailsOld.Replace("amp;", "");
                            pdfModel.SegmentDetailsNew = pdfModel.SegmentDetailsNew.Replace("amp;", "");
                        }
                        attachment = isNull(cm.SegmentDetails.SegmentAttachment).ToString().Trim();
                        attachmentType = isNull(cm.SegmentDetails.SegmentAttachmentType).ToString().Trim().ToUpper();
                        if (!string.IsNullOrEmpty(attachment))
                        {
                            prCommonAttachmentInsert(ObjConnection, Code, attachmentType, attachment, "SegmentAttachment", objTransTplus, refNo, 1, false);
                        }
                    }

                    if (blnBeginTranTplus)
                    {
                        objTransTplus.Commit();
                        //utility.CommitTrans();
                    }
                }
                res.ClientCode = cm.PersonalDetails.ClientCode;
                res.Status = "Y";
                res.Remark = successmsg += Environment.NewLine + "Data Updated successfully!";
            }
            catch (Exception ex)
            {
                if (blnBeginTranTplus)
                {
                    try
                    {
                        objTransTplus.Rollback();
                        //utility.RollBackTrans();
                    }
                    catch (Exception)
                    {
                    }
                }
                res.ClientCode = cm.PersonalDetails.ClientCode;
                res.Status = "N";
                res.Remark = "Error in Process : UpdateClientMaster " + Environment.NewLine + "Error :" + ex.Message.ToString() + Environment.NewLine;
            }
            return res;
        }

        public void prCommonInsert(SqlConnection objCon, bool IsDeleted, string strMaster, string strTable, string strKeyfield, string strKeyName, string strKeyValue, string strField, string strFieldDesc, string strOldValue, string strNewvalue, string strComputername, string strmkrid, string strmkrdt, string strMkrtm, string strMkridOld, string strMkrdtOld, string strMkrtmOld, SqlTransaction objTran, string nFiller3RefNo, int nFiller1 = 0)
        {
            try
            {
                strSQL = "Insert into Client_ModifyAPI (";
                strSQL += " ca_cmcd, ca_field, ca_desc, ca_oldvalue, ca_newvalue, ca_date, ca_time,  ca_computername, ca_Tplus, ca_Cross, ca_Estro, ca_Dematacno, ca_filler1, ca_Nfiller1, ca_Nfiller3 ";
                strSQL += ") ";
                strSQL += " Select '" + strKeyValue.Trim() + "' ca_cmcd, '" + strField + "' ca_field, '" + strFieldDesc + "' ca_desc, ";
                strSQL += " '" + strOldValue.Trim().Replace("'", "") + "' ca_oldvalue, '" + strNewvalue.Trim().Replace("'", "") + "' ca_newvalue, ";
                strSQL += " '" + strmkrdt + "' ca_date, '" + strMkrtm.Trim() + "' ca_time, '" + strComputername + "' ca_computername,   'N' ca_Tplus, 'N' ca_Cross, 'N' ca_Estro, '' ca_Dematacno, '" + (IsDeleted ? 1 : 0) + "' ca_filler1, " + nFiller1 + " ca_Nfiller1, " + nFiller3RefNo + " ca_Nfiller3;";
                utility.ExecuteSQL(strSQL, objCon, objTran);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void prCommonAttachmentInsert(SqlConnection objCon, string userName, string fileName, string ma_proof, string ma_field, SqlTransaction objTran, string refNo, int nFiller, bool isDel)
        {
            try
            {
                DataTable dt = new DataTable();
                strSQL = "Select * From Client_ModifyAttach where ma_cmcd = '" + userName + "' and ma_field = '" + ma_field + "' and ma_Nfiller1 = " + nFiller + " and ma_refno = " + refNo;
                dt = utility.OpenDataTable(strSQL, objCon, objTran);
                if (dt.Rows.Count > 0)
                {
                    if (isDel == true)
                    {
                        strSQL = "Delete From Client_ModifyAttach where ma_cmcd = '" + userName + "' and ma_field = '" + ma_field + "'  and ma_refno = " + refNo;
                        utility.ExecuteSQL(strSQL);

                        strSQL = "INSERT INTO Client_ModifyAttach (";
                        strSQL += "ma_cmcd, ma_date, ma_filename, ma_proof, ma_field, mkrdt, mkrtm,ma_refNo, ma_status, ma_Nfiller1)";
                        strSQL += "VALUES";
                        strSQL += "('" + userName + "','" + DateTime.Now.ToString("yyyyMMdd") + "', '" + fileName + "', @proofJson, '" + ma_field + "', '" + DateTime.Now.ToString("yyyyMMdd") + "', '" + DateTime.Now.ToString("hh:mm:ss") + "'," + refNo + ", 'N', " + nFiller + ")";
                    }
                    else
                    {
                        strSQL = "Update Client_ModifyAttach Set ";
                        strSQL += "  ma_proof = @proofJson,  mkrdt = '" + DateTime.Now.ToString("yyyyMMdd") + "', mkrtm = '" + DateTime.Now.ToString("hh:mm:ss") + "' ";
                        strSQL += " Where ma_cmcd = '" + userName + "' and ma_field = '" + ma_field + "' and ma_Nfiller1 = " + nFiller + " and ma_refno = " + refNo;
                    }
                }
                else
                {
                    strSQL = "INSERT INTO Client_ModifyAttach (";
                    strSQL += "ma_cmcd, ma_date, ma_filename, ma_proof, ma_field, mkrdt, mkrtm,ma_refNo, ma_status, ma_Nfiller1)";
                    strSQL += "VALUES";
                    strSQL += "('" + userName + "','" + DateTime.Now.ToString("yyyyMMdd") + "', '" + fileName + "', @proofJson, '" + ma_field + "', '" + DateTime.Now.ToString("yyyyMMdd") + "', '" + DateTime.Now.ToString("hh:mm:ss") + "'," + refNo + ", 'N', " + nFiller + ")";
                }

                var byteProofJson = Encoding.UTF8.GetBytes(ma_proof);
                Dictionary<string, byte[]> sqlParams = new Dictionary<string, byte[]>();
                sqlParams.Add("@proofJson", byteProofJson);

                utility.ExecuteSQL(strSQL, sqlParams, objCon, objTran);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void prCommonInsertInCommon_Audit(SqlConnection objCon, string strMaster, string strTable, string gDpid, string strKeyfield, string strKeyName, string strKeyValue, string strField, string strFieldDesc, string strOldValue, string strNewvalue, string strComputername, string strmkrid, string strmkrdt, string strMkrtm, string strMkridOld, string strMkrdtOld, string strMkrtmOld, SqlTransaction objTrans, DataTable tempDataTable = null)
        {
            DataTable rstemp = new DataTable();
            string strSql = string.Empty;

            strSql = "insert into Common_audit(ca_master,";
            strSql += " ca_table,ca_dpid,ca_keyfield,ca_keyname,";
            strSql += " ca_keyvalue,ca_field,ca_fielddescription,ca_oldvalue,";
            strSql += " ca_newvalue,ca_computername,mkrid,mkrdt,mkrtm,";
            strSql += " mkridold,mkrdtold,mkrtmold)";
            strSql += " Values ('" + strMaster + "','" + strTable + "', '" + gDpid + "', '" + strKeyfield + "','" + strKeyName + "',";
            strSql += " '" + strKeyValue.Trim() + "','" + strField + "','" + strFieldDesc + "', '" + strOldValue.Trim().Replace("'", "") + "', ";
            strSql += "  '" + strNewvalue.Trim().Replace("'", "") + "', '" + strComputername + "', '" + strmkrid.Trim() + "','" + strmkrdt.Trim() + "','" + strMkrtm.Trim() + "',";
            strSql += " '" + strMkridOld.Trim() + "','" + strMkrdtOld.Trim() + "','" + strMkrtmOld.Trim() + "')";

            utility.ExecuteSQL(strSql, objCon, objTrans);
        }

        public void prCommonInsertInClient_Modification(SqlConnection objCon, string clientCode, string strField, string strOldValue, string strNewValue, string strmkrId, string strComputerName, string strAllow, string strBrcode, string strAuthId, int strBatchNo, string strFieldDesc, string strRefNo, string temp, string strInWardNo, string strFlag, string strRemarks, SqlTransaction objTrans, string strClosure = "", string reqintref = "", string strTrxType = "")
        {
            DateTime todayDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, UtilityCommon.GetIndianTimeZone());
            string strmkrDt = utility.dtos(todayDate.ToString());
            string strRefDt = strmkrDt; //utility.dtos(todayDate.ToString());
            string strTime = todayDate.ToString("hh:mm:ss");
            string strAuthDate = "";
            string strRecTime = strTime;
            strAuthId = "";

            string strSql = "INSERT INTO Client_Modification(ca_cmcd,ca_field,ca_oldvalue,ca_newvalue,mkrid,mkrdt,ca_computername,ca_time,ca_allow,ca_brcode,";
            strSql += " ca_authid,ca_authdt,ca_batchno,ca_fdesc,ca_refno,ca_refdt,ca_inwardno,ca_flag,ca_remarks,ca_closure,ca_RecTime, ca_trxtype, ca_closurereason, ca_newboid, ca_reqintref, ca_destroyslip,ca_check,ca_rembal)";
            strSql += $" VALUES ('{clientCode}', '{strField}', '{strOldValue}', '{strNewValue}', '{strmkrId}', '{strmkrDt}', '{strComputerName}', '{strTime}', '{strAllow}', '{strBrcode}',";
            strSql += $" '{strAuthId}', '{strAuthDate}', {strBatchNo}, '{strFieldDesc}', '{strRefNo}', '{strRefDt}', '{strInWardNo}', '{strFlag}', '{strRemarks}','{strClosure}','{strRecTime}','{strTrxType}','','','{reqintref}','','','')";

            utility.ExecuteSQL(strSql, objCon, objTrans);
        }

        public string GetEsignUrl(string userId, string refNo)
        {
            string strSql = string.Empty;
            strSql = "Select * From Client_ModifyAttach (NoLock) where ma_cmcd = '" + userId + "' and ma_status = 'N' and ma_filename = 'EsignRequest' and ma_refNo = " + refNo;
            DataTable dt = utility.OpenDataTable(strSql);
            if (dt.Rows.Count > 0)
            {
                var getEsignReq = Newtonsoft.Json.Linq.JObject.Parse(Encoding.UTF8.GetString((byte[])dt.Rows[0]["ma_proof"]).ToString());
                var signers = (string)getEsignReq["signers"][0]["url"];
                return signers;
            }
            return "";
        }

        public dynamic GetCheckerRekycClientDetails(string refNo, string userId)
        {
            try
            {
                SqlParameter[] sqlPrm = new[]
                {
                    new SqlParameter("@i_vcClientCode",userId),
                    new SqlParameter("@i_vcRefNo",refNo)
                };
                List<OutputParamSP> spModelList = new List<OutputParamSP>();
                OutputParamSP spModel = new OutputParamSP();
                spModel.ParamValue = "@o_JsonOutput";
                spModel.DbType = SqlDbType.NVarChar;
                spModel.Length = 40000;
                spModelList.Add(spModel);

                var respSP = utility.Execute_SP_OuputParam("stpr_RekycGetMakerData", sqlPrm, spModelList);
                var jsonResp = JsonConvert.DeserializeObject<dynamic>(respSP.ToString());
                return jsonResp;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public dynamic CallSetuProcedure(string vendor, string funName, string applName, string jsonObj, string refNo)
        {
            try
            {
                SqlParameter[] sqlPrm = new[]
                {
                    new SqlParameter("@i_vcVendorCode",vendor),
                    new SqlParameter("@i_vcFunctionName",funName),
                    new SqlParameter("@i_vcUserID",applName),
                    new SqlParameter("@i_vcInputJSON",jsonObj),
                    new SqlParameter("@i_vcRefNo",refNo)
                };
                List<OutputParamSP> spModelList = new List<OutputParamSP>();
                OutputParamSP spModel = new OutputParamSP();
                spModel.ParamValue = "@o_vcOutPutJSON";
                spModel.DbType = SqlDbType.NVarChar;
                spModel.Length = 40000;
                spModelList.Add(spModel);

                var respSP = utility.Execute_SP_OuputParam("stpr_APIReKycThirdParty", sqlPrm, spModelList);
                var jsonResp = JsonConvert.DeserializeObject<dynamic>(respSP.ToString());
                return jsonResp;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}