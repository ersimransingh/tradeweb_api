/*using DevExpress.Pdf;
using DevExpress.XtraPrinting;
using DevExpress.XtraRichEdit.Layout.Engine;*/
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
/*using TradeWeb.API.DevForms;*/
/*using TradeWeb.API.ExtentionMethod;*/
using TradeWeb.API.Models;
using TradeWeb.API.Repository;
using TradeWeb.Service;


namespace TradeWeb.API.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ReKYCController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly UtilityCommon _objUtility;
        private readonly ModelTradeWebCommon _modelTRadeWeb;
        private readonly ITradeWebRepository _tradeWebRepository;
        private readonly ITradeNetRepository _tradeNetRepository;
        private readonly ILogger<ReKYCController> _logger;
        ConvertData returnJson = new ConvertData();
        DataTable retrunDt = new DataTable();

        public ReKYCController(IConfiguration configuration, UtilityCommon objUtility, ITradeWebRepository tradeWebRepository, ILogger<ReKYCController> logger, ModelTradeWebCommon modelTRadeWeb, ITradeNetRepository tradeNetRepository)
        {
            _configuration = configuration;
            _objUtility = objUtility;
            _tradeWebRepository = tradeWebRepository;
            _logger = logger;
            _modelTRadeWeb = modelTRadeWeb;
            _tradeNetRepository = tradeNetRepository;
        }


        #region  ReKyc APIs

        //        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("GetMasters", Name = "GetMasters")]
        public IActionResult GetMasters(string request)
        {
            if (ModelState.IsValid)
            {
                DataTable retrunDt = new DataTable();
                try
                {
                    if (!_objUtility.mfnGetSysSplFeature("RKC"))
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "You are not authorise to use TradeWebAPI Web Service,Please contact SecMark.", retrunDt, ""));
                    }

                    var result = _tradeWebRepository.GetMasterData(request);
                    if (result != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", result, ""));
                    }
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", retrunDt, ""));
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("GetEntryIncomplete", Name = "GetEntryIncomplete")]
        public IActionResult GetEntryIncomplete()
        {
            if (ModelState.IsValid)
            {
                DataTable retrunDt = new DataTable();
                try
                {
                    if (!_objUtility.mfnGetSysSplFeature("RKC"))
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "You are not authorise to use TradeWebAPI Web Service,Please contact SecMark.", retrunDt, ""));
                    }

                    JwtSecurityToken token = _objUtility.GetToken();
                    var userId = token.Claims.First(claim => claim.Type == "username").Value;
                    RekycGetJsonModel rekycGetJsonModel = new RekycGetJsonModel();
                    FinalReKycModel respModel = new FinalReKycModel();
                    rekycGetJsonModel = _tradeWebRepository.GetKyc_Details(respModel, userId);

                    if (string.IsNullOrEmpty(rekycGetJsonModel.errorMsg))
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", rekycGetJsonModel.rekycJson, ""));
                    }
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "failed", rekycGetJsonModel.errorMsg, ""));
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpPost("PostEntryIncomplete", Name = "PostEntryIncomplete")]
        public IActionResult PostEntryIncomplete([FromBody] object jsonObj)
        {
            if (ModelState.IsValid)
            {
                DataTable retrunDt = new DataTable();
                try
                {
                    if (!_objUtility.mfnGetSysSplFeature("RKC"))
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "You are not authorise to use TradeWebAPI Web Service,Please contact SecMark.", retrunDt, ""));
                    }

                    JwtSecurityToken token = _objUtility.GetToken();
                    var userId = token.Claims.First(claim => claim.Type == "username").Value;

                    if (jsonObj == null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", "Data should not be blank", ""));
                    }
                    else if (string.IsNullOrEmpty(jsonObj.ToString()) || jsonObj.ToString() == "string")
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", "Data should not be blank", ""));
                    }

                    string refNo = _tradeWebRepository.ReKYC_MasterTable(userId, "Pending", "N", 1, "Initiate Rekyc Post Incomplete", "ReKYC");
                    var result = _tradeWebRepository.AddKyc_Details(userId, jsonObj, "N", refNo);
                    if (result != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", result, ""));
                    }
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", retrunDt, ""));
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        /*[Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpPost("PostEntryFinal", Name = "PostEntryFinal")]
        public IActionResult PostEntryFinal([FromBody] object jsonObj)
        {
            if (ModelState.IsValid)
            {
                DataTable retrunDt = new DataTable();
                try
                {
                    if (!_objUtility.mfnGetSysSplFeature("RKC"))
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "You are not authorise to use TradeWebAPI Web Service,Please contact SecMark.", retrunDt, ""));
                    }
                    JwtSecurityToken token = _objUtility.GetToken();
                    var userId = token.Claims.First(claim => claim.Type == "username").Value;
                    if (jsonObj == null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", "Data should not be blank", ""));
                    }
                    else if (string.IsNullOrEmpty(jsonObj.ToString()) || jsonObj.ToString() == "string")
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", "Data should not be blank", ""));
                    }
                    EsignResponse esignResponse = new EsignResponse();

                    var step = _tradeWebRepository.ReKYC_GetSteps(userId, "rm_step", "ReKYC");
                    var desc = _tradeWebRepository.ReKYC_GetSteps(userId, "rm_desc", "ReKYC");
                    //// Get Referenct No
                    step = step == "" ? 0 : step;
                    step = Convert.ToInt32(step);
                    var revJson = JsonConvert.DeserializeObject<dynamic>(jsonObj.ToString());
                    string reviewFlag = revJson["RekycJson"][0]["ReKycDetails"][0]["PreviewFlag"] ?? "";
                    if (step < 3 && reviewFlag == "esign")
                    {
                        step = 3;
                    }
                    string refN = _tradeWebRepository.ReKYC_MasterTable(userId, "Pending", "N", step, desc, "ReKYC");

                    if (step == 3)
                    {
                        var strQuery = $"Select Rtrim(cm_mobile) cm_mobile, LEFT(cm_name,CHARINDEX(' ',cm_name)-1) FName, Left(cm_dob, 4) cm_dob, ma_field  from Client_ModifyAttach (NoLock), Client_master (NoLock)  where ma_cmcd = cm_cd And ma_cmcd = '{userId}' and ma_filename = 'UnSignedPdf' and ma_refno=" + refN + " And ma_status = 'N' ";
                        var tempDb = _objUtility.OpenDataTable(strQuery);
                        if (tempDb.Rows.Count > 0)
                        {
                            Microsoft.Data.SqlClient.SqlParameter[] sqlPrm = new[]
                                              {
                                                    new Microsoft.Data.SqlClient.SqlParameter("@userId",userId),
                                                    new Microsoft.Data.SqlClient.SqlParameter("@fileName","UnSignedpdf"),
                                                    new Microsoft.Data.SqlClient.SqlParameter("@refNo",refN),
                                                };

                            var spResponse = _objUtility.Execute_SP_DataTable("SP_GetPdfBase64", sqlPrm);
                            var pdfBase64 = spResponse.Rows[0]["pdfFile"].ToString();

                            var valdJsonObj = JsonConvert.DeserializeObject<dynamic>(jsonObj.ToString());

                            string mobNo = tempDb.Rows[0]["cm_mobile"].ToString() ?? "";
                            string firstName = tempDb.Rows[0]["FName"].ToString();
                            string rtnUrl = valdJsonObj["RekycJson"][0]["ReKycDetails"][0]["ReturnUrlEsignSetu"];
                            string dob = tempDb.Rows[0]["cm_dob"].ToString();
                            var esignUrl = UploadPdfForEsignSetu(refN, pdfBase64, mobNo, firstName, dob, rtnUrl, userId + "_ReKYC.pdf", userId);

                            return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", esignUrl, ""));
                        }
                    }
                    else if (step == 4)
                    {
                        string strSql = string.Empty;
                        strSql = "Select * From Client_ModifyAttach (NoLock) where ma_cmcd = '" + userId + "' and ma_status = 'N' and ma_filename = 'SignedKRAPdf' and ma_refNo = " + refN;
                        DataTable dtEsng = _objUtility.OpenDataTable(strSql);
                        if (dtEsng.Rows.Count < 1)
                        {
                            strSql = "Select * From Client_ModifyAttach (NoLock) where ma_cmcd = '" + userId + "' and ma_status = 'N' and ma_filename = 'EsignRequestKRA' and ma_refNo = " + refN;
                            DataTable dt = _objUtility.OpenDataTable(strSql);
                            if (dt.Rows.Count > 0)
                            {
                                if (dt.Rows[0]["ma_date"].ToString() != DateTime.Now.ToString("yyyyMMdd"))
                                {
                                    var strQuery2 = $"Select Rtrim(cm_mobile) cm_mobile, LEFT(cm_name,CHARINDEX(' ',cm_name)-1) FName, Left(cm_dob, 4) cm_dob, ma_field  from Client_ModifyAttach (NoLock), Client_master (NoLock)  where ma_cmcd = cm_cd And ma_cmcd = '{userId}' and ma_filename = 'UnSignedKRApdf' and ma_refno=" + refN + " And ma_status = 'N' ";
                                    var dtKra = _objUtility.OpenDataTable(strQuery2);
                                    if (dtKra.Rows.Count > 0)
                                    {
                                        Microsoft.Data.SqlClient.SqlParameter[] sqlPrm = new[]
                                           {
                                                    new Microsoft.Data.SqlClient.SqlParameter("@userId",userId),
                                                    new Microsoft.Data.SqlClient.SqlParameter("@fileName","UnSignedKRApdf"),
                                                    new Microsoft.Data.SqlClient.SqlParameter("@refNo",refN),
                                           };
                                        var spResponse = _objUtility.Execute_SP_DataTable("SP_GetPdfBase64", sqlPrm);

                                        var pdfBase64 = spResponse.Rows[0]["pdfFile"].ToString();
                                        string mobNo = dtKra.Rows[0]["cm_mobile"].ToString() ?? "";
                                        string firstName = dtKra.Rows[0]["FName"].ToString();
                                        string rtnUrl = dtKra.Rows[0]["ma_field"].ToString().Replace("KRA UnSigned Pdf", "");
                                        string dob = dtKra.Rows[0]["cm_dob"].ToString();
                                        string kraEsignUrl = UploadKRApdfForEsign(refN, pdfBase64, mobNo, firstName, dob, rtnUrl, userId + "_KRA.pdf", userId);
                                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", kraEsignUrl, ""));
                                    }
                                }

                                var getEsignReq = Newtonsoft.Json.Linq.JObject.Parse(Encoding.UTF8.GetString((byte[])dt.Rows[0]["ma_proof"]).ToString());
                                var esignUrl = (string)getEsignReq["signers"][0]["url"];
                                return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", esignUrl, ""));
                            }
                        }
                    }
                    else if (step == 5)
                    {
                        //// check if data pending for approval 
                        var strQuery = $"select count(*) as Total from Client_ModifyAPI (NoLock) where ca_cmcd = '{userId}' and ca_Tplus = 'N'  and ca_Nfiller3=" + refN + " ";
                        var tempDb = _objUtility.OpenDataTable(strQuery);
                        if (tempDb.Rows[0]["Total"].ToInt() > 0)
                        {
                            return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "pending", "Data is pending for approval.", ""));
                        }
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "pending", "No records found.", ""));
                    }
                    else
                    {
                        var valdJsonObj = JsonConvert.DeserializeObject<dynamic>(jsonObj.ToString());
                        valdJsonObj["RekycJson"][0]["Attachments"][0]["AddressAttachment"] = "";
                        valdJsonObj["RekycJson"][0]["Attachments"][0]["IncomeAttachment"] = "";
                        valdJsonObj["RekycJson"][0]["Attachments"][0]["SegmentAttachment"] = "";
                        valdJsonObj["RekycJson"][0]["Attachments"][0]["SignatureAttachment"] = "";
                        foreach (var bnk in valdJsonObj["RekycJson"][0]["BankDetails"])
                        {
                            bnk["BankAttachment"] = "";
                        }
                        foreach (var nom in valdJsonObj["RekycJson"][0]["NomineeDetails"])
                        {
                            nom["NomineeAttachment"] = "";
                        }
                        foreach (var dmt in valdJsonObj["RekycJson"][0]["DematDetails"])
                        {
                            dmt["DematAttachment"] = "";
                        }
                        var rmvAttchJsonSP = JsonConvert.SerializeObject(valdJsonObj);

                        /////********* Checke validation using SP ********
                        var respns = _modelTRadeWeb.CheckValidation(rmvAttchJsonSP, userId, "MAKER", refN);
                        string[] breakResp = respns.ToString().Split('$');
                        if (breakResp[0] != "S")
                        {
                            JObject jsonResp = JObject.Parse(breakResp[2]);
                            if (jsonResp != null)
                            {
                                return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", jsonResp, ""));
                            }
                            return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", breakResp[1], ""));
                        }
                        /////********* Save all attachments in database ********
                        var saveAttach = _modelTRadeWeb.SaveAllReKycAttachments(jsonObj, refN, userId);

                        /////********* Final save Rekyc post data using SP ********
                        var finalOutput = _modelTRadeWeb.UpdateClientMaster(rmvAttchJsonSP, refN, userId);

                        string[] output = finalOutput.ToString().Split('$');
                        if (output[0] != "S")
                        {
                            return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", output[1], ""));
                        }

                        _tradeWebRepository.AddKyc_Details(userId, jsonObj, "N", refN);

                        string refNo = _tradeWebRepository.ReKYC_MasterTable(userId, "Pending", "N", 2, "Submit Final post data - pending for Esign/Approval", "ReKYC");

                        var pdfModf = _modelTRadeWeb.Fill_PDF_Model(jsonObj, refN, userId);

                        var digioData = GeneratePDF(refN, userId, pdfModf, jsonObj);
                        esignResponse.EsignUrl = digioData.EsignUrl;
                        esignResponse.UnsignedPdf = digioData.UnsignedPdf;
                        esignResponse.Status = digioData.Status;
                        esignResponse.ClientCode = userId;
                        esignResponse.Remark = digioData.Remark;
                        if (esignResponse != null)
                        {
                            if (esignResponse.Status == "Y")
                            {
                                return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", esignResponse, ""));
                            }
                            return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", esignResponse, ""));
                        }
                    }
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "notfound", "No record found.", ""));
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "failed", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }*/

        #endregion


        #region ReKyc Dashboard/Checker API

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("GetCheckerSummary", Name = "GetCheckerSummary")]
        public IActionResult GetCheckerSummary(string fromDate, string toDate, string status)
        {
            if (ModelState.IsValid)
            {
                DataTable retrunDt = new DataTable();
                try
                {
                    if (!_objUtility.mfnGetSysSplFeature("RKC"))
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "You are not authorise to use TradeWebAPI Web Service,Please contact SecMark.", retrunDt, ""));
                    }
                    var result = _tradeWebRepository.RekycViewAllModification(fromDate, toDate, status);
                    if (result != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", result, ""));
                    }
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "notfound", "No Record Found", ""));
                }
                catch (Exception ex)
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }


        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpPost("PostChecker", Name = "PostChecker")]
        public IActionResult PostChecker(RekycApprovePost model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (!_objUtility.mfnGetSysSplFeature("RKC"))
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "You are not authorise to use TradeWebAPI Web Service,Please contact SecMark.", retrunDt, ""));
                    }

                    JwtSecurityToken token = _objUtility.GetToken();
                    var userId = token.Claims.First(claim => claim.Type == "username").Value;
                    var compCode = token.Claims.First(claim => claim.Type == "companyCode").Value;
                    if (model.Status != "R")
                    {
                        var respns = _modelTRadeWeb.CheckValidation("", model.ClientCode, "CHECKER", model.RefNo);
                        string[] breakResp = respns.ToString().Split('$');
                        if (breakResp[0] != "S")
                        {
                            JObject jsonResp = JObject.Parse(breakResp[2]);
                            if (jsonResp != null)
                            {
                                var respVal = Convert.ToString(jsonResp["Response"][0]);
                                var parseError = JObject.Parse(respVal);
                                var errVal = Convert.ToString(parseError["ErrorTag"]);
                                if (errVal != "S")
                                {
                                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", jsonResp, ""));
                                }
                            }
                        }
                    }
                    var result = _tradeWebRepository.RekycApprove(userId, model, compCode);
                    string[] resStr = result.ToString().Split('$');
                    if (resStr[0] != "S")
                    {
                        JObject jsonResp = JObject.Parse(resStr[2]);
                        if (jsonResp != null)
                        {
                            if (jsonResp.ToString() == "" || jsonResp.ToString() == "{}")
                            {
                                return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", resStr[1], ""));
                            }
                            var respVal = Convert.ToString(jsonResp["Response"][0]);
                            var parseError = JObject.Parse(respVal);
                            var errVal = Convert.ToString(parseError["ErrorTag"]);
                            if (errVal != "S")
                            {
                                return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", jsonResp, ""));
                            }
                        }
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", "Some error occured.", ""));
                    }
                    if (resStr[0] == "S")
                    {
                        string respMsg = model.Status == "R" ? "Request rejected successfully!" : "Request approved successfully!";
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", respMsg, ""));
                    }
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "failed", result, ""));
                }
                catch (Exception ex)
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }


        //// for SP object
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("GetCheckerDetail", Name = "GetCheckerDetail")]
        public IActionResult GetCheckerDetail(string clientCode, string refNo)
        {
            if (ModelState.IsValid)
            {
                DataTable retrunDt = new DataTable();
                try
                {
                    if (!_objUtility.mfnGetSysSplFeature("RKC"))
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "You are not authorise to use TradeWebAPI Web Service,Please contact SecMark.", retrunDt, ""));
                    }

                    JwtSecurityToken token = _objUtility.GetToken();
                    var userId = token.Claims.First(claim => claim.Type == "username").Value;

                    var result = _modelTRadeWeb.GetCheckerRekycClientDetails(refNo, clientCode);
                    if (result != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", result, ""));
                    }
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", retrunDt, ""));
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        #endregion

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpPost("CallThirdParyAPI", Name = "CallThirdParyAPI")]
        public IActionResult CallThirdParyAPI([FromBody] ThirdPartyCallApiModel request)
        {
            if (ModelState.IsValid)
            {
                DataTable retrunDt = new DataTable();
                try
                {
                    if (!_objUtility.mfnGetSysSplFeature("RKC"))
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "You are not authorise to use TradeWebAPI Web Service,Please contact SecMark.", retrunDt, ""));
                    }

                    JwtSecurityToken token = _objUtility.GetToken();
                    var userId = token.Claims.First(claim => claim.Type == "username").Value;
                    //string vendor = _configuration["ThirdPartyAPIVendor"];
                    string funcNameUpr = request.paramName.ToUpper();
                    string refNo = "";
                    if (request.vName.ToUpper() == "REKYC" || request.vName.ToUpper() == "ACCOUNT CLOSURE")
                    {
                        switch (funcNameUpr)
                        {
                            case "DIGILOCKERREQUEST":
                                refNo = _tradeWebRepository.ReKYC_MasterTable(userId, "Pending", "N", 0, "Call address change DigiLocker api", request.vName);
                                break;
                            case "DIGILOCKERJSON":
                                refNo = _tradeWebRepository.ReKYC_MasterTable(userId, "Pending", "N", 0, "Call address change DigiLocker api for get pdf json", request.vName);
                                break;
                            case "GETESIGNDOCUMENT":
                                var desc = _tradeWebRepository.ReKYC_GetSteps(userId, "rm_desc", request.vName);
                                refNo = _tradeWebRepository.ReKYC_MasterTable(userId, "Pending", "N", 3, desc, request.vName);
                                break;
                            case "RPD_CALLED":
                                refNo = _tradeWebRepository.ReKYC_MasterTable(userId, "Pending", "N", 0, "Call RPD post api", request.vName);
                                break;
                            case "RPD_GETRESPONSE":
                                refNo = _tradeWebRepository.ReKYC_MasterTable(userId, "Pending", "N", 0, "Call RPD get details api", request.vName);
                                break;
                            case "PENNY-DROP":
                                refNo = _tradeWebRepository.ReKYC_MasterTable(userId, "Pending", "N", 0, "Call Penny-Drop post api", request.vName);
                                break;
                            case "GETESIGNDOCUMENTKRA":
                                refNo = _tradeWebRepository.ReKYC_GetSteps(userId, "rm_refno", request.vName);
                                break;
                        }
                    }
                    var jsonString = ((System.Text.Json.JsonElement)request.jsonRequest).GetRawText();
                    var result = _modelTRadeWeb.CallSetuProcedure("SETU", request.paramName, request.vName, jsonString, refNo);
                    if (result != null)
                    {
                        string esignUrl = "";
                        if (result["ResponseFlag"] == "E")
                        {
                            return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", result, ""));
                        }
                        else
                        {
                            if (funcNameUpr == "GETESIGNDOCUMENT")
                            {
                                if (request.vName.ToUpper() == "ACCOUNT CLOSURE")
                                    refNo = _tradeWebRepository.ReKYC_MasterTable(userId, "Pending", "N", 5, "AC Esign done: pdf docs done with Esign", request.vName);
                                else
                                    refNo = _tradeWebRepository.ReKYC_MasterTable(userId, "Pending", "N", 4, "Esign done: pdf docs done with Esign", request.vName);

                                try
                                {
                                    var strQuery = $"Select Rtrim(cm_mobile) cm_mobile, LEFT(cm_name,CHARINDEX(' ',cm_name)-1) FName, Left(cm_dob, 4) cm_dob, Client_ModifyAttach.* from Client_ModifyAttach (NoLock), Client_master (NoLock)  where ma_cmcd = cm_cd And ma_cmcd = '{userId}' and ma_filename = 'UnSignedKRApdf' and ma_refno=" + refNo + " And ma_status = 'N' ";
                                    var tempDb = _objUtility.OpenDataTable(strQuery);
                                    if (tempDb.Rows.Count > 0)
                                    {
                                        Microsoft.Data.SqlClient.SqlParameter[] sqlPrm = new[]
                                            {
                                                    new Microsoft.Data.SqlClient.SqlParameter("@userId",userId),
                                                    new Microsoft.Data.SqlClient.SqlParameter("@fileName","UnSignedKRApdf"),
                                                    new Microsoft.Data.SqlClient.SqlParameter("@refNo",refNo),
                                                };

                                        var spResponse = _objUtility.Execute_SP_DataTable("SP_GetPdfBase64", sqlPrm);

                                        var pdfBase64 = spResponse.Rows[0]["pdfFile"].ToString();
                                        string mobNo = tempDb.Rows[0]["cm_mobile"].ToString() ?? "";
                                        string firstName = tempDb.Rows[0]["FName"].ToString();
                                        string rtnUrl = tempDb.Rows[0]["ma_field"].ToString().Replace("KRA UnSigned Pdf", "");
                                        string dob = tempDb.Rows[0]["cm_dob"].ToString();
                                        //esignUrl = UploadKRApdfForEsign(refNo, pdfBase64, mobNo, firstName, dob, rtnUrl, userId + "_KRA.pdf", userId);
                                        esignUrl = "";
                                        result["KRAesignUrl"] = esignUrl;
                                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", result, ""));
                                    }
                                }
                                catch (Exception) { }
                            }
                            else if (funcNameUpr == "GETESIGNDOCUMENTKRA")
                            {
                                refNo = _tradeWebRepository.ReKYC_MasterTable(userId, "Pending", "N", 5, "KRA Esign done: KRA pdf docs done with Esign", request.vName);
                            }
                            return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", result, ""));
                        }
                    }
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "notfound", "No record found.", ""));
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        #region New Kyc helper method

        //private string UploadPdfForEsignSetu(string refNo, string pdfBase64, string identifierMobileNo, string DispName, string yob, string esignRedirectUrl, string fileName, string clientCode)
        //{
        //    try
        //    {
        //        string esignReqId = string.Empty;
        //        string vendor = "SETU";
        //        #region upload file for esign
        //        var jsonObj = new
        //        {
        //            ClientCode = clientCode,
        //            base64 = pdfBase64,
        //            FileName = fileName,
        //            FilePassword = ""
        //        };
        //        var jsonParams = JsonConvert.SerializeObject(jsonObj);
        //        try
        //        {
        //            var resp = _modelTRadeWeb.CallSetuProcedure(vendor, "EsignRequest", "ReKYC", jsonParams, refNo);
        //            //var parseJson = JObject.Parse(resp);
        //            esignReqId = Convert.ToString(resp["id"]);
        //            refNo = _tradeWebRepository.ReKYC_MasterTable(clientCode, "Pending", "N", 3, "Call Esign for upload pdf for Request Id: " + esignReqId, "ReKYC");
        //        }
        //        catch (Exception ex)
        //        {
        //            return "Issue while uploading pdf on setu server \\n " + ex.Message.ToString();
        //        }

        //        #endregion

        //        #region Create signature for esign
        //        var baseString = Convert.FromBase64String(pdfBase64);
        //        MemoryStream msBase64 = new MemoryStream(baseString);
        //        PdfDocumentProcessor pdfDocumentProcessor = new PdfDocumentProcessor();
        //        pdfDocumentProcessor.LoadDocument(msBase64);
        //        var countPage = pdfDocumentProcessor.Document.Pages.Count;

        //        var jsonSignerObj = new
        //        {
        //            ClientCode = clientCode,
        //            Requestid = esignReqId,
        //            redirectUrl = esignRedirectUrl,
        //            identifier = identifierMobileNo,
        //            displayName = DispName,
        //            height = "60",
        //            onPages = countPage,
        //            position = "bottom-right",
        //            width = "180",
        //            birthYear = yob
        //        };
        //        var jsonVal = JsonConvert.SerializeObject(jsonSignerObj);
        //        try
        //        {
        //            var getSigner = _modelTRadeWeb.CallSetuProcedure(vendor, "CreateSign", "ReKYC", jsonVal, refNo);
        //            string signerUrl = "";
        //            try
        //            {
        //                if (getSigner == null)
        //                {
        //                    string strSql = string.Empty;
        //                    strSql = "Select * From Client_ModifyAttach (NoLock) where ma_cmcd = '" + clientCode + "' and ma_status = 'N' and ma_filename = 'EsignRequest' and ma_refNo = " + refNo;
        //                    DataTable dt = _objUtility.OpenDataTable(strSql);
        //                    if (dt.Rows.Count > 0)
        //                    {
        //                        var getEsignReq = Newtonsoft.Json.Linq.JObject.Parse(Encoding.UTF8.GetString((byte[])dt.Rows[0]["ma_proof"]).ToString());
        //                        var esignUrl = (string)getEsignReq["signers"][0]["url"];
        //                        return esignUrl;
        //                    }
        //                }
        //                if (getSigner["ResponseFlag"] == "E")
        //                {
        //                    return "Pdf uploaded on setu, Please submit again to get pdf and Esign url. \\n " + getSigner["ResponseMessage"];
        //                }
        //                signerUrl = Convert.ToString(getSigner["signers"][0]["url"]);
        //                var serlzJson = JsonConvert.SerializeObject(getSigner);
        //                refNo = _tradeWebRepository.ReKYC_MasterTable(clientCode, "Pending", "N", 3, "PDF uploaded on setu, Pending for Esign (Request Id:" + esignReqId, "ReKYC");
        //                byte[] getJsonBytes = Encoding.UTF8.GetBytes(serlzJson);
        //                var result = _tradeWebRepository.AddEsignPdf(clientCode, getJsonBytes, "EsignRequest", "Esign Request Output for Request Id=" + esignReqId, refNo);
        //                return signerUrl;
        //            }
        //            catch (Exception e)
        //            {
        //                return "Pdf uploaded on setu, Please submit again to get pdf and Esign url. \\n " + e.Message.ToString();
        //            }
        //        }
        //        catch (WebException exception)
        //        {
        //            string responseText = string.Empty;

        //            var responseStream = exception.Response?.GetResponseStream();

        //            if (responseStream != null)
        //            {
        //                using (var reader = new StreamReader(responseStream))
        //                {
        //                    responseText = reader.ReadToEnd();
        //                }
        //            }
        //            return responseText;
        //        }
        //        #endregion
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //private string UploadKRApdfForEsign(string refNo, string pdfBase64, string identifierMobileNo, string DispName, string yob, string esignRedirectUrl, string fileName, string clientCode)
        //{
        //    try
        //    {
        //        string esignReqId = string.Empty;
        //        string vendor = "SETU";
        //        #region upload file for esign
        //        var jsonObj = new
        //        {
        //            ClientCode = clientCode,
        //            base64 = pdfBase64,
        //            FileName = fileName,
        //            FilePassword = ""
        //        };
        //        var jsonParams = JsonConvert.SerializeObject(jsonObj);
        //        try
        //        {
        //            var resp = _modelTRadeWeb.CallSetuProcedure(vendor, "EsignRequestKRA", "KRA", jsonParams, refNo);
        //            esignReqId = Convert.ToString(resp["id"]);
        //        }
        //        catch (Exception ex)
        //        {
        //            return "Issue while uploading pdf on setu server \\n " + ex.Message.ToString();
        //        }

        //        #endregion

        //        #region Create signature for esign
        //        var baseString = Convert.FromBase64String(pdfBase64);
        //        MemoryStream msBase64 = new MemoryStream(baseString);
        //        PdfDocumentProcessor pdfDocumentProcessor = new PdfDocumentProcessor();
        //        pdfDocumentProcessor.LoadDocument(msBase64);
        //        var countPage = pdfDocumentProcessor.Document.Pages.Count;

        //        var jsonSignerObj = new
        //        {
        //            ClientCode = clientCode,
        //            Requestid = esignReqId,
        //            redirectUrl = esignRedirectUrl,
        //            identifier = identifierMobileNo,
        //            displayName = DispName,
        //            height = "60",
        //            onPages = countPage,
        //            position = "bottom-right",
        //            width = "180",
        //            birthYear = yob
        //        };
        //        var jsonVal = JsonConvert.SerializeObject(jsonSignerObj);
        //        try
        //        {
        //            var getSigner = _modelTRadeWeb.CallSetuProcedure(vendor, "CreateSignKRA", "KRA", jsonVal, refNo);
        //            string signerUrl = "";
        //            try
        //            {
        //                if (getSigner == null)
        //                {
        //                    string strSql = string.Empty;
        //                    strSql = "Select * From Client_ModifyAttach (NoLock) where ma_cmcd = '" + clientCode + "' and ma_status = 'N' and ma_filename = 'EsignRequestKRA' and ma_refNo = " + refNo;
        //                    DataTable dt = _objUtility.OpenDataTable(strSql);
        //                    if (dt.Rows.Count > 0)
        //                    {
        //                        var getEsignReq = Newtonsoft.Json.Linq.JObject.Parse(Encoding.UTF8.GetString((byte[])dt.Rows[0]["ma_proof"]).ToString());
        //                        var esignUrl = (string)getEsignReq["signers"][0]["url"];
        //                        return esignUrl;
        //                    }
        //                }
        //                if (getSigner["ResponseFlag"] == "E")
        //                {
        //                    return "Pdf uploaded on setu, Please submit again to get pdf and Esign url. \\n " + getSigner["ResponseMessage"];
        //                }
        //                signerUrl = Convert.ToString(getSigner["signers"][0]["url"]);
        //                var serlzJson = JsonConvert.SerializeObject(getSigner);
        //                byte[] getJsonBytes = Encoding.UTF8.GetBytes(serlzJson);
        //                var result = _tradeWebRepository.AddEsignPdf(clientCode, getJsonBytes, "EsignRequestKRA", "Esign Request Output for Request Id=" + esignReqId, refNo);
        //                return signerUrl;
        //            }
        //            catch (Exception e)
        //            {
        //                return "Pdf uploaded on setu, Please submit again to get pdf and Esign url. \\n " + e.Message.ToString();
        //            }
        //        }
        //        catch (WebException exception)
        //        {
        //            string responseText = string.Empty;

        //            var responseStream = exception.Response?.GetResponseStream();

        //            if (responseStream != null)
        //            {
        //                using (var reader = new StreamReader(responseStream))
        //                {
        //                    responseText = reader.ReadToEnd();
        //                }
        //            }
        //            return responseText;
        //        }
        //        #endregion
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}


        /*private dynamic GeneratePDF(string refNo, string userId, Pdf_ModificationModel pdfModel, object jsonObj)
        {
            try
            {
                MemoryStream ms = new MemoryStream();
                ms.Seek(0, SeekOrigin.Begin);
                try
                {
                    UtilityCommon objUtility = new UtilityCommon();
                    string sqlQry = "Select ma_filename fileName, ma_field fieldName, ma_proof attachment, ma_Nfiller1 NFiller1 From Client_ModifyAttach where ma_cmcd = '" + userId + "' and ma_refno = '" + refNo + "' and ma_status = 'N' ";
                    string jsonString = "", adrPhoto = "", signPhoto = "";
                    DataTable dbTable = objUtility.OpenDataTable(sqlQry);
                    if (dbTable.Rows.Count > 0)
                    {
                        List<RekycAttachmentTable> rekycAttach = CommonExtensions.ConvertToList<RekycAttachmentTable>(dbTable);

                        var adharJsonObj = rekycAttach.FirstOrDefault(x => x.fileName.ToLower() == "digilockerjson");
                        if (adharJsonObj != null)
                        {
                            var byteAdharJson = adharJsonObj.attachment;
                            jsonString = JObject.Parse(Encoding.UTF8.GetString((byte[])byteAdharJson)).ToString();
                        }
                        var adharPhoto = rekycAttach.FirstOrDefault(x => x.fieldName.ToLower() == "photoattachment");
                        if (adharPhoto != null)
                        {
                            adrPhoto = Encoding.UTF8.GetString((byte[])adharPhoto.attachment);
                        }
                        var signImage = rekycAttach.FirstOrDefault(x => x.fieldName.ToLower() == "signattachment");
                        if (signImage != null)
                        {
                            signPhoto = Encoding.UTF8.GetString((byte[])signImage.attachment);
                            signPhoto = signPhoto.Replace("data:image/jpeg;base64,", "");
                        }
                        using (PdfDocumentProcessor processor = new PdfDocumentProcessor())
                        {
                            processor.CreateEmptyDocument(ms);
                            using (DevExpress.Pdf.PdfGraphics graph = processor.CreateGraphics())
                            {
                                var formMs = Pdf_RegistrationForm(pdfModel, jsonString, adrPhoto, signPhoto);
                                byte[] bytes = formMs.ToArray();
                                var point = new PointF(15, 15);
                                graph.DrawImage(bytes, point);
                                processor.RenderNewPage(PdfPaperSize.A4, graph);
                            }
                            using (DevExpress.Pdf.PdfGraphics graph = processor.CreateGraphics())
                            {
                                var page2 = Pdf_SecondPage(pdfModel);
                                byte[] bytes2 = page2.ToArray();
                                var point2 = new PointF(25, 50);
                                graph.DrawImage(bytes2, point2);
                                processor.RenderNewPage(PdfPaperSize.A4, graph);
                            }
                            var ifNomModified = rekycAttach.Where(x => x.fieldName.ToLower() == "nomineeattachment").ToList();
                            if (ifNomModified != null)
                            {
                                foreach (var nomList in ifNomModified)
                                {
                                    using (DevExpress.Pdf.PdfGraphics graph = processor.CreateGraphics())
                                    {
                                        var page2 = Pdf_NomineeForm(pdfModel);
                                        byte[] bytes2 = page2.ToArray();
                                        var point2 = new PointF(25, 50);
                                        graph.DrawImage(bytes2, point2);
                                        processor.RenderNewPage(PdfPaperSize.A4, graph);
                                    }
                                    break;
                                }
                            }
                            if (pdfModel.IsMobEmailIncomeModified == true || pdfModel.IsSegmentModified == true || pdfModel.IsNomShow1 == true || pdfModel.IsBankShow1 == true || pdfModel.IsDematShow1 == true)
                            {
                                using (DevExpress.Pdf.PdfGraphics graph = processor.CreateGraphics())
                                {
                                    // var formMs = Pdf_RegistrationForm(pdfModel, jsonString);
                                    float x = 30;  // Initial X position
                                    float y = 70;  // Initial Y position
                                    var msBase64 = Pdf_ModificationForm(pdfModel);
                                    PdfDocumentProcessor pdfDocumentProcessor = new PdfDocumentProcessor();
                                    pdfDocumentProcessor.LoadDocument(msBase64);
                                    var countPage = pdfDocumentProcessor.Document.Pages.Count;
                                    for (int i = 0; i < countPage; i++)
                                    {
                                        MemoryStream mainStream = new MemoryStream();
                                        msBase64 = new MemoryStream();
                                        var size = new System.Drawing.Size(Convert.ToInt32(pdfDocumentProcessor.Document.Pages[i].MediaBox.Width), Convert.ToInt32(pdfDocumentProcessor.Document.Pages[i].MediaBox.Height));
                                        IEnumerable<int> myEnumberable = new List<int>() { i + 1 };
                                        pdfDocumentProcessor.CreateTiff(msBase64, myEnumberable, 100);
                                        Image panImage = Image.FromStream(msBase64);
                                        float aspectRatio = (float)panImage.Width / (float)panImage.Height;
                                        float newWidthImg = size.Width; // Adjust this size as needed
                                        float newHeight = newWidthImg / aspectRatio;
                                        RectangleF panRect = new RectangleF(x, y, newWidthImg, newHeight);
                                        graph.DrawImage(panImage, panRect);
                                        processor.RenderNewPage(PdfPaperSize.A4, graph);
                                    }
                                }
                            }

                            using (DevExpress.Pdf.PdfGraphics graph = processor.CreateGraphics())
                            {
                                //float pageWidth = (float)PdfPaperSize.A4.Width;
                                //float pageHeight = (float)PdfPaperSize.A4.Height;
                                float x = 5;  // Initial X position
                                float y = 5;  // Initial Y position

                                var ipvImage = rekycAttach.FirstOrDefault(x => x.fileName.ToLower() == "ipv_final");
                                if (ipvImage != null)
                                {
                                    try
                                    {
                                        string ipvAttc = Encoding.UTF8.GetString((byte[])ipvImage.attachment);
                                        if (!string.IsNullOrEmpty(ipvAttc))
                                        {
                                            ipvAttc = ipvAttc.Replace("data:image/jpeg;base64,", "").Trim();
                                            var byteArr = Convert.FromBase64String(ipvAttc);
                                            MemoryStream msBase64 = new MemoryStream(byteArr);
                                            byte[] bytes = msBase64.ToArray();
                                            var point = new PointF(0, 0);
                                            var recPoints = new Rectangle(10, 10, (int)PdfPaperSize.A4.Width, (int)PdfPaperSize.A4.Height);
                                            graph.DrawImage(msBase64, recPoints);
                                            processor.RenderNewPage(PdfPaperSize.A4, graph);
                                        }
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }

                                var addressAttchObj = rekycAttach.FirstOrDefault(x => x.fieldName.ToLower() == "addressattachment");
                                if (addressAttchObj != null)
                                {
                                    if (!string.IsNullOrEmpty(addressAttchObj.attachment.ToString()))
                                    {
                                        var bs64 = Encoding.UTF8.GetString((byte[])addressAttchObj.attachment);
                                        var byteArr = Convert.FromBase64String(bs64);
                                        MemoryStream msBase64 = new MemoryStream(byteArr);
                                        PdfDocumentProcessor pdfDocumentProcessor = new PdfDocumentProcessor();
                                        pdfDocumentProcessor.LoadDocument(msBase64);
                                        var countPage = pdfDocumentProcessor.Document.Pages.Count;
                                        for (int i = 0; i < countPage; i++)
                                        {
                                            MemoryStream mainStream = new MemoryStream();
                                            msBase64 = new MemoryStream();
                                            var size = new System.Drawing.Size(Convert.ToInt32(pdfDocumentProcessor.Document.Pages[i].MediaBox.Width), Convert.ToInt32(pdfDocumentProcessor.Document.Pages[i].MediaBox.Height));
                                            IEnumerable<int> myEnumberable = new List<int>() { i + 1 };
                                            pdfDocumentProcessor.CreateTiff(msBase64, size.Height, myEnumberable);
                                            Image panImage = Image.FromStream(msBase64);
                                            float aspectRatio = (float)panImage.Width / (float)panImage.Height;
                                            float newWidthImg = size.Width; // Adjust this size as needed
                                            float newHeight = newWidthImg / aspectRatio;
                                            //RectangleF panRect = new RectangleF(x, y, newWidthImg, newHeight);
                                            RectangleF panRect = new RectangleF(x, y, (int)PdfPaperSize.A4.Width, (int)PdfPaperSize.A4.Height);
                                            graph.DrawImage(panImage, panRect);
                                            processor.RenderNewPage(PdfPaperSize.A4, graph);
                                        }
                                    }
                                }

                                var incomeAttchObj = rekycAttach.FirstOrDefault(x => x.fieldName.ToLower() == "incomeattachment");
                                if (incomeAttchObj != null)
                                {
                                    if (!string.IsNullOrEmpty(incomeAttchObj.attachment.ToString()))
                                    {
                                        var bs64 = Encoding.UTF8.GetString((byte[])incomeAttchObj.attachment);
                                        var byteArr = Convert.FromBase64String(bs64);
                                        MemoryStream msBase64 = new MemoryStream(byteArr);
                                        PdfDocumentProcessor pdfDocumentProcessor = new PdfDocumentProcessor();
                                        pdfDocumentProcessor.LoadDocument(msBase64);
                                        var countPage = pdfDocumentProcessor.Document.Pages.Count;
                                        for (int i = 0; i < countPage; i++)
                                        {
                                            MemoryStream mainStream = new MemoryStream();
                                            msBase64 = new MemoryStream();
                                            var size = new System.Drawing.Size(Convert.ToInt32(pdfDocumentProcessor.Document.Pages[i].MediaBox.Width), Convert.ToInt32(pdfDocumentProcessor.Document.Pages[i].MediaBox.Height));
                                            IEnumerable<int> myEnumberable = new List<int>() { i + 1 };
                                            pdfDocumentProcessor.CreateTiff(msBase64, myEnumberable, 100);
                                            Image panImage = Image.FromStream(msBase64);
                                            float aspectRatio = (float)panImage.Width / (float)panImage.Height;
                                            float newWidthImg = size.Width; // Adjust this size as needed
                                            float newHeight = newWidthImg / aspectRatio;
                                            //RectangleF panRect = new RectangleF(x, y, newWidthImg, newHeight);
                                            RectangleF panRect = new RectangleF(x, y, (int)PdfPaperSize.A4.Width, (int)PdfPaperSize.A4.Height);
                                            graph.DrawImage(panImage, panRect);
                                            processor.RenderNewPage(PdfPaperSize.A4, graph);
                                        }
                                    }
                                }

                                var nomAttachGroup = rekycAttach.Where(x => x.fieldName.ToLower() == "nomineeattachment").ToList();
                                var nomAttach = nomAttachGroup?.OrderBy(x => x.NFiller1);
                                if (nomAttach != null)
                                {
                                    foreach (var nomList in nomAttach)
                                    {
                                        var bs64 = Encoding.UTF8.GetString((byte[])nomList.attachment);
                                        var byteArr = Convert.FromBase64String(bs64);
                                        MemoryStream msBase64 = new MemoryStream(byteArr);
                                        PdfDocumentProcessor pdfDocumentProcessor = new PdfDocumentProcessor();
                                        pdfDocumentProcessor.LoadDocument(msBase64);
                                        var countPage = pdfDocumentProcessor.Document.Pages.Count;

                                        for (int i = 0; i < countPage; i++)
                                        {
                                            MemoryStream mainStream = new MemoryStream();
                                            msBase64 = new MemoryStream();
                                            var size = new System.Drawing.Size(Convert.ToInt32(pdfDocumentProcessor.Document.Pages[i].MediaBox.Width), Convert.ToInt32(pdfDocumentProcessor.Document.Pages[i].MediaBox.Height));
                                            IEnumerable<int> myEnumberable = new List<int>() { i + 1 };
                                            pdfDocumentProcessor.CreateTiff(mainStream, myEnumberable, 135);
                                            Image panImage = Image.FromStream(mainStream);
                                            //float aspectRatio = (float)panImage.Width / (float)panImage.Height;
                                            //float newWidthImg = size.Width; // Adjust this size as needed
                                            //float newHeight = newWidthImg / aspectRatio;
                                            //RectangleF panRect = new RectangleF(x, y, newWidthImg, newHeight);
                                            var recPoints = new RectangleF(x, y, (int)PdfPaperSize.A4.Width, (int)PdfPaperSize.A4.Height);
                                            graph.DrawImage(panImage, recPoints);
                                            processor.RenderNewPage(PdfPaperSize.A4, graph);
                                        }
                                    }
                                }

                                var bankAttachGroup = rekycAttach.Where(x => x.fieldName.ToLower() == "bankattachment").ToList();
                                var bankAttach = bankAttachGroup?.OrderBy(x => x.NFiller1);
                                if (bankAttach != null)
                                {
                                    foreach (var bnkList in bankAttach)
                                    {
                                        var bs64 = Encoding.UTF8.GetString((byte[])bnkList.attachment);
                                        var byteArr = Convert.FromBase64String(bs64);
                                        MemoryStream msBase64 = new MemoryStream(byteArr);
                                        PdfDocumentProcessor pdfDocumentProcessor = new PdfDocumentProcessor();
                                        pdfDocumentProcessor.LoadDocument(msBase64);
                                        var countPage = pdfDocumentProcessor.Document.Pages.Count;
                                        for (int i = 0; i < countPage; i++)
                                        {
                                            MemoryStream mainStream = new MemoryStream();
                                            msBase64 = new MemoryStream();
                                            var size = new System.Drawing.Size(Convert.ToInt32(pdfDocumentProcessor.Document.Pages[i].MediaBox.Width), Convert.ToInt32(pdfDocumentProcessor.Document.Pages[i].MediaBox.Height));
                                            IEnumerable<int> myEnumberable = new List<int>() { i + 1 };
                                            pdfDocumentProcessor.CreateTiff(msBase64, size.Height, myEnumberable);
                                            Image panImage = Image.FromStream(msBase64);
                                            float aspectRatio = (float)panImage.Width / (float)panImage.Height;
                                            float newWidthImg = size.Width; // Adjust this size as needed
                                            float newHeight = newWidthImg / aspectRatio;
                                            //RectangleF panRect = new RectangleF(x, y, newWidthImg, newHeight);
                                            RectangleF panRect = new RectangleF(x, y, (int)PdfPaperSize.A4.Width, (int)PdfPaperSize.A4.Height);
                                            graph.DrawImage(panImage, panRect);
                                            processor.RenderNewPage(PdfPaperSize.A4, graph);
                                        }
                                    }
                                }

                                var dmtAttachGroup = rekycAttach.Where(x => x.fieldName.ToLower() == "dematattachment").ToList();
                                var demtAttach = dmtAttachGroup?.OrderBy(x => x.NFiller1);
                                if (demtAttach != null)
                                {
                                    foreach (var dmtList in demtAttach)
                                    {
                                        var bs64 = Encoding.UTF8.GetString((byte[])dmtList.attachment);
                                        var byteArr = Convert.FromBase64String(bs64);
                                        MemoryStream msBase64 = new MemoryStream(byteArr);
                                        PdfDocumentProcessor pdfDocumentProcessor = new PdfDocumentProcessor();
                                        pdfDocumentProcessor.LoadDocument(msBase64);
                                        var countPage = pdfDocumentProcessor.Document.Pages.Count;
                                        for (int i = 0; i < countPage; i++)
                                        {
                                            MemoryStream mainStream = new MemoryStream();
                                            msBase64 = new MemoryStream();
                                            var size = new System.Drawing.Size(Convert.ToInt32(pdfDocumentProcessor.Document.Pages[i].MediaBox.Width), Convert.ToInt32(pdfDocumentProcessor.Document.Pages[i].MediaBox.Height));
                                            IEnumerable<int> myEnumberable = new List<int>() { i + 1 };
                                            pdfDocumentProcessor.CreateTiff(msBase64, myEnumberable, 100);
                                            Image panImage = Image.FromStream(msBase64);
                                            float aspectRatio = (float)panImage.Width / (float)panImage.Height;
                                            float newWidthImg = size.Width; // Adjust this size as needed
                                            float newHeight = newWidthImg / aspectRatio;
                                            //RectangleF panRect = new RectangleF(x, y, newWidthImg, newHeight);
                                            RectangleF panRect = new RectangleF(x, y, (int)PdfPaperSize.A4.Width, (int)PdfPaperSize.A4.Height);
                                            graph.DrawImage(panImage, panRect);
                                            processor.RenderNewPage(PdfPaperSize.A4, graph);
                                        }
                                    }
                                }

                                var segmentAttchObj = rekycAttach.FirstOrDefault(x => x.fieldName.ToLower() == "segmentattachment");
                                if (segmentAttchObj != null)
                                {
                                    var bs64 = Encoding.UTF8.GetString((byte[])segmentAttchObj.attachment);
                                    var byteArr = Convert.FromBase64String(bs64);
                                    MemoryStream msBase64 = new MemoryStream(byteArr);
                                    PdfDocumentProcessor pdfDocumentProcessor = new PdfDocumentProcessor();
                                    pdfDocumentProcessor.LoadDocument(msBase64);
                                    var countPage = pdfDocumentProcessor.Document.Pages.Count;
                                    for (int i = 0; i < countPage; i++)
                                    {
                                        MemoryStream mainStream = new MemoryStream();
                                        msBase64 = new MemoryStream();
                                        var size = new System.Drawing.Size(Convert.ToInt32(pdfDocumentProcessor.Document.Pages[i].MediaBox.Width), Convert.ToInt32(pdfDocumentProcessor.Document.Pages[i].MediaBox.Height));
                                        IEnumerable<int> myEnumberable = new List<int>() { i + 1 };
                                        pdfDocumentProcessor.CreateTiff(msBase64, size.Height, myEnumberable);
                                        Image panImage = Image.FromStream(msBase64);
                                        float aspectRatio = (float)panImage.Width / (float)panImage.Height;
                                        float newWidthImg = size.Width; // Adjust this size as needed
                                        float newHeight = newWidthImg / aspectRatio;
                                        //RectangleF panRect = new RectangleF(x, y, newWidthImg, newHeight);
                                        RectangleF panRect = new RectangleF(x, y, (int)PdfPaperSize.A4.Width, (int)PdfPaperSize.A4.Height);
                                        graph.DrawImage(panImage, panRect);
                                        processor.RenderNewPage(PdfPaperSize.A4, graph);
                                    }
                                }

                                var signAttch = rekycAttach.FirstOrDefault(x => x.fieldName.ToLower() == "signattachment");
                                if (signAttch != null)
                                {
                                    try
                                    {
                                        string sgnAttc = Encoding.UTF8.GetString((byte[])signAttch.attachment);
                                        if (!string.IsNullOrEmpty(sgnAttc))
                                        {
                                            sgnAttc = sgnAttc.Replace("data:image/jpeg;base64,", "").Trim();
                                            var byteArr = Convert.FromBase64String(sgnAttc);
                                            MemoryStream msBase64 = new MemoryStream(byteArr);
                                            byte[] bytes = msBase64.ToArray();
                                            var point = new PointF(0, 0);
                                            var recPoints = new Rectangle(5, 5, (int)PdfPaperSize.A4.Width - 15, (int)PdfPaperSize.A4.Height - 15);
                                            graph.DrawImage(msBase64, recPoints);
                                            processor.RenderNewPage(PdfPaperSize.A4, graph);
                                        }
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }

                                var panAttch = rekycAttach.FirstOrDefault(x => x.fieldName.ToLower() == "panattachment");
                                if (panAttch != null)
                                {
                                    try
                                    {
                                        var bs64 = Encoding.UTF8.GetString((byte[])panAttch.attachment);
                                        var byteArr = Convert.FromBase64String(bs64);
                                        MemoryStream msBase64 = new MemoryStream(byteArr);
                                        PdfDocumentProcessor pdfDocumentProcessor = new PdfDocumentProcessor();
                                        pdfDocumentProcessor.LoadDocument(msBase64);
                                        var countPage = pdfDocumentProcessor.Document.Pages.Count;
                                        for (int i = 0; i < countPage; i++)
                                        {
                                            MemoryStream mainStream = new MemoryStream();
                                            msBase64 = new MemoryStream();
                                            var size = new System.Drawing.Size(Convert.ToInt32(pdfDocumentProcessor.Document.Pages[i].MediaBox.Width), Convert.ToInt32(pdfDocumentProcessor.Document.Pages[i].MediaBox.Height));
                                            IEnumerable<int> myEnumberable = new List<int>() { i + 1 };
                                            pdfDocumentProcessor.CreateTiff(msBase64, size.Height, myEnumberable);
                                            Image panImage = Image.FromStream(msBase64);
                                            float aspectRatio = (float)panImage.Width / (float)panImage.Height;
                                            float newWidthImg = size.Width; // Adjust this size as needed
                                            float newHeight = newWidthImg / aspectRatio;
                                            //RectangleF panRect = new RectangleF(x, y, newWidthImg, newHeight);
                                            RectangleF panRect = new RectangleF(x, y, 580, 830);
                                            graph.DrawImage(panImage, panRect);
                                            processor.RenderNewPage(PdfPaperSize.A4, graph);
                                        }
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }

                                var adharAttch = rekycAttach.FirstOrDefault(x => x.fieldName.ToLower() == "aadhaarattachment");
                                if (adharAttch != null)
                                {
                                    try
                                    {
                                        string adrAttc = Encoding.UTF8.GetString((byte[])adharAttch.attachment);
                                        if (!string.IsNullOrEmpty(adrAttc))
                                        {
                                            var byteArr = Convert.FromBase64String(adrAttc);
                                            MemoryStream msBase64 = new MemoryStream(byteArr);

                                            byte[] bytes = msBase64.ToArray();
                                            var point = new PointF(0, 0);
                                            var recPoints = new Rectangle(5, 5, (int)PdfPaperSize.A4.Width + 20, (int)PdfPaperSize.A4.Height + 30);
                                            // var recPoints2 = new Rectangle((int)20, (int)25, (int)PdfPaperSize.A3.Width, (int)PdfPaperSize.A3.Height);
                                            graph.DrawImage(msBase64, recPoints);
                                            processor.RenderNewPage(PdfPaperSize.A4, graph);
                                        }
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                            }
                        }

                        string digitalDocument = Convert.ToBase64String(ms.ToArray());

                        byte[] getPdfBytes = System.Convert.FromBase64String(digitalDocument);
                        try
                        {
                            var result = _tradeWebRepository.AddEsignPdf(userId, getPdfBytes, "UnSignedPdf", "ReKyc UnSigned Pdf", refNo);
                            // refNo = _tradeWebRepository.ReKYC_MasterTable(userId, "Pending", "N", 3, "PDF created, Call Esign Request for unsigned pdf", "ReKYC");
                            try
                            {
                                MemoryStream ms2 = new MemoryStream();
                                ms2.Seek(0, SeekOrigin.Begin);
                                using (PdfDocumentProcessor processor = new PdfDocumentProcessor())
                                {
                                    processor.CreateEmptyDocument(ms2);
                                    using (DevExpress.Pdf.PdfGraphics graph = processor.CreateGraphics())
                                    {
                                        FillPdfPersonalInfo persInfo = new FillPdfPersonalInfo();
                                        persInfo = _modelTRadeWeb.Fill_PDF_ModelPersonalInfo(jsonObj);
                                        pdfModel.PAN_Number = _modelTRadeWeb.isNull(persInfo.PAN_Number).ToUpper();
                                        pdfModel.MobileNoPrint = _modelTRadeWeb.isNull(persInfo.MobNo);
                                        pdfModel.EmailPrint = _modelTRadeWeb.isNull(persInfo.Email);
                                        var formMs = Pdf_NewKRAForm(pdfModel, jsonString, adrPhoto, signPhoto);
                                        PdfDocumentProcessor pdfDocumentProcessor = new PdfDocumentProcessor();
                                        pdfDocumentProcessor.LoadDocument(formMs);
                                        var countPage = pdfDocumentProcessor.Document.Pages.Count;
                                        for (int i = 0; i < 2; i++)
                                        {
                                            MemoryStream mainStream = new MemoryStream();
                                            formMs = new MemoryStream();
                                            var size = new System.Drawing.Size(Convert.ToInt32(pdfDocumentProcessor.Document.Pages[i].MediaBox.Width), Convert.ToInt32(pdfDocumentProcessor.Document.Pages[i].MediaBox.Height));
                                            IEnumerable<int> myEnumberable = new List<int>() { i + 1 };
                                            pdfDocumentProcessor.CreateTiff(formMs, size.Height, myEnumberable);
                                            Image panImage = Image.FromStream(formMs);
                                            float aspectRatio = (float)panImage.Width / (float)panImage.Height;
                                            float newWidthImg = size.Width; // Adjust this size as needed
                                            float newHeight = newWidthImg / aspectRatio;
                                            RectangleF panRect = new RectangleF(10, 10, newWidthImg, newHeight);
                                            graph.DrawImage(panImage, panRect);
                                            processor.RenderNewPage(PdfPaperSize.A4, graph);
                                        }
                                    }
                                    using (DevExpress.Pdf.PdfGraphics graph = processor.CreateGraphics())
                                    {
                                        float x = 5;  // Initial X position
                                        float y = 5;  // Initial Y position
                                        var panAttch = rekycAttach.FirstOrDefault(x => x.fieldName.ToLower() == "panattachment");
                                        if (panAttch != null)
                                        {
                                            try
                                            {
                                                var bs64 = Encoding.UTF8.GetString((byte[])panAttch.attachment);
                                                var byteArr = Convert.FromBase64String(bs64);
                                                MemoryStream msBase64 = new MemoryStream(byteArr);
                                                PdfDocumentProcessor pdfDocumentProcessor = new PdfDocumentProcessor();
                                                pdfDocumentProcessor.LoadDocument(msBase64);
                                                var countPage = pdfDocumentProcessor.Document.Pages.Count;
                                                for (int i = 0; i < countPage; i++)
                                                {
                                                    MemoryStream mainStream = new MemoryStream();
                                                    msBase64 = new MemoryStream();
                                                    var size = new System.Drawing.Size(Convert.ToInt32(pdfDocumentProcessor.Document.Pages[i].MediaBox.Width), Convert.ToInt32(pdfDocumentProcessor.Document.Pages[i].MediaBox.Height));
                                                    IEnumerable<int> myEnumberable = new List<int>() { i + 1 };
                                                    pdfDocumentProcessor.CreateTiff(msBase64, size.Height, myEnumberable);
                                                    Image panImage = Image.FromStream(msBase64);
                                                    float aspectRatio = (float)panImage.Width / (float)panImage.Height;
                                                    float newWidthImg = size.Width; // Adjust this size as needed
                                                    float newHeight = newWidthImg / aspectRatio;
                                                    //RectangleF panRect = new RectangleF(x, y, newWidthImg, newHeight);
                                                    RectangleF panRect = new RectangleF(x, y, 580, 830);
                                                    graph.DrawImage(panImage, panRect);
                                                    processor.RenderNewPage(PdfPaperSize.A4, graph);
                                                }
                                            }
                                            catch (Exception)
                                            {
                                            }
                                        }

                                        var adharAttch = rekycAttach.FirstOrDefault(x => x.fieldName.ToLower() == "aadhaarattachment");
                                        if (adharAttch != null)
                                        {
                                            try
                                            {
                                                string adrAttc = Encoding.UTF8.GetString((byte[])adharAttch.attachment);
                                                if (!string.IsNullOrEmpty(adrAttc))
                                                {
                                                    var byteArr = Convert.FromBase64String(adrAttc);
                                                    MemoryStream msBase64 = new MemoryStream(byteArr);

                                                    byte[] bytes = msBase64.ToArray();
                                                    var point = new PointF(0, 0);
                                                    var recPoints = new Rectangle(5, 5, (int)PdfPaperSize.A4.Width + 20, (int)PdfPaperSize.A4.Height + 30);
                                                    // var recPoints2 = new Rectangle((int)20, (int)25, (int)PdfPaperSize.A3.Width, (int)PdfPaperSize.A3.Height);
                                                    graph.DrawImage(msBase64, recPoints);
                                                    processor.RenderNewPage(PdfPaperSize.A4, graph);
                                                }
                                            }
                                            catch (Exception)
                                            {
                                            }
                                        }
                                    }
                                }
                                string kraDoc = Convert.ToBase64String(ms2.ToArray());
                                byte[] kraPdfBytes = System.Convert.FromBase64String(kraDoc);
                                var result2 = _tradeWebRepository.AddEsignPdf(userId, kraPdfBytes, "UnSignedKRApdf", "KRA UnSigned Pdf" + pdfModel.setuEsignReturnUrl, refNo);
                            }
                            catch (Exception)
                            {
                            }
                            return new { Status = "Y", UnsignedPdf = digitalDocument, EsignUrl = "", Remark = "Pdf generated successfully!" };
                        }
                        catch (Exception)
                        {
                        }

                    }
                    return new { Status = "N", UnsignedPdf = "", EsignUrl = "", Remark = "Records saved successfully!, but some error occured while creating pdf. It may be some attachment issue." };
                }
                catch (Exception e)
                {
                    return new { Status = "N", UnsignedPdf = "", EsignUrl = "", Remark = "Records saved successfully!, but some error occured while creating pdf. It may be some attachment issue, Please check error below and submit again. \n " + e.Message.ToString() };
                }
            }
            catch (Exception ex)
            {
                return new { Status = "N", UnsignedPdf = "", EsignUrl = "", Remark = ex.Message.ToString() };
            }
        }*/



        /*private MemoryStream Pdf_RegistrationForm(Pdf_ModificationModel model, string jsonString, string adharBase64, string signBase64)
        {
            MemoryStream ms = new MemoryStream();
            EsignForm form = new EsignForm();
            if (!string.IsNullOrEmpty(jsonString))
            {
                try
                {
                    var jsonObj = JObject.Parse(jsonString);
                    model.UID = Convert.ToString(jsonObj["aadhaar"]["maskedNumber"]);
                }
                catch (Exception)
                { }
            }
            string date = DateTime.Now.ToString("ddMMyyyy");
            model.CurtDD = date.Substring(0, 2);
            model.CurtMM = date.Substring(2, 2);
            model.CurtYYYY = date.Substring(4, 4);
            if (model.PermanentAddressFlag == "Y")
            {
                model.PermAddress1 = model.CorrAddress1;
                model.PermAddress2 = model.CorrAddress2;
                model.PermAddress3 = model.CorrAddress3;
                model.PermCity = model.CorrCity;
                model.PermState = model.CorrState;
                model.PermCountry = model.CorrCountry;
                model.PermPincode = model.CorrPincode;
            }
            Dictionary<string, string> dictParam = new Dictionary<string, string>();
            {
                dictParam.Add("@strClientCode", model.ClientCode);
                dictParam.Add("@o_vcOutput", "OUTPUT");
            }
            System.Data.DataSet dsSP = _objUtility.ExecuteCommonSP("stpr_GetAdditionalDetailRekyc", dictParam);

            if (dsSP.Tables[0].Rows.Count > 0)
            {
                try
                {
                    if (model.PermanentAddressFlag != "Y")
                    {
                        model.PermAddress1 = dsSP.Tables[0].Rows[0]["PerAddress1"].ToString();
                        model.PermAddress2 = dsSP.Tables[0].Rows[0]["PerAddress2"].ToString();
                        model.PermAddress3 = dsSP.Tables[0].Rows[0]["PerAddress3"].ToString();
                        model.PermCity = dsSP.Tables[0].Rows[0]["PerCity"].ToString();
                        model.PermState = dsSP.Tables[0].Rows[0]["PerState"].ToString();
                        model.PermCountry = dsSP.Tables[0].Rows[0]["PerCountry"].ToString();
                        model.PermPincode = dsSP.Tables[0].Rows[0]["PerPincode"].ToString();
                    }
                    model.DPID8 = dsSP.Tables[0].Rows[0]["DPID"].ToString();
                    model.ClientId8 = dsSP.Tables[0].Rows[0]["ClientId"].ToString();
                    model.CompanyName = dsSP.Tables[0].Rows[0]["CompanyName"].ToString();
                    model.KycVerfName = dsSP.Tables[0].Rows[0]["KYCVerificatioName"].ToString();
                    model.KycVerfDesignation = dsSP.Tables[0].Rows[0]["KYCVerificationDesig"].ToString();
                    model.KycVerfEmpCode = dsSP.Tables[0].Rows[0]["KYCVerificationEmpCode"].ToString();
                    model.KycVerfBranch = dsSP.Tables[0].Rows[0]["KYCVerificationBranch"].ToString();
                    model.KycPlace = dsSP.Tables[0].Rows[0]["PlaceOfDeclaration"].ToString();
                    model.FatherPrefix = dsSP.Tables[0].Rows[0]["FatherPrefix"].ToString();
                    model.MotherPrefix = dsSP.Tables[0].Rows[0]["MotherPrefix"].ToString();
                    model.MotherFname = dsSP.Tables[0].Rows[0]["MotherFname"].ToString();
                    model.MaidenPrefix = dsSP.Tables[0].Rows[0]["MaidernPrefix"].ToString();
                    model.MaidenFname = dsSP.Tables[0].Rows[0]["MaidenFname"].ToString();
                    model.CompanyFullAddress = dsSP.Tables[0].Rows[0]["CompanyAddress"].ToString();

                    byte[] salt = (byte[])dsSP.Tables[0].Rows[0]["CompanyLogo"];
                    string base64String = Convert.ToBase64String(salt);
                    model.CompanyLogo = base64String;
                }
                catch (Exception)
                {
                }
            }

            model.ClientImage = adharBase64;
            model.ClientSignature = signBase64;

            IEnumerable<Pdf_ModificationModel> regPdf = new List<Pdf_ModificationModel>() { model };
            form.DataSource = regPdf;
            ImageExportOptions imageExportOptions = new ImageExportOptions()
            {
                ExportMode = ImageExportMode.SingleFile
            };
            ms.Seek(0, SeekOrigin.Begin);
            form.ExportToImage(ms);
            return ms;
        }*/

        /*private MemoryStream Pdf_ModificationForm(Pdf_ModificationModel pdfModel)
        {
            MemoryStream ms = new MemoryStream();
            XtraReport1 form = new XtraReport1();
            IEnumerable<Pdf_ModificationModel> esigns = new List<Pdf_ModificationModel>() { pdfModel };
            form.DataSource = esigns;
            ImageExportOptions imageExportOptions = new ImageExportOptions()
            {
                ExportMode = ImageExportMode.SingleFile
            };
            ms.Seek(0, SeekOrigin.Begin);
            form.ExportToPdf(ms);
            return ms;
        }*/

        /*private MemoryStream Pdf_SecondPage(Pdf_ModificationModel pdfModel)
        {
            MemoryStream ms = new MemoryStream();
            SecondPage form = new SecondPage();
            IEnumerable<Pdf_ModificationModel> esigns = new List<Pdf_ModificationModel>() { pdfModel };
            form.DataSource = esigns;
            ImageExportOptions imageExportOptions = new ImageExportOptions()
            {
                ExportMode = ImageExportMode.SingleFile
            };
            ms.Seek(0, SeekOrigin.Begin);
            form.ExportToImage(ms);
            return ms;
        }*/
        /*private MemoryStream Pdf_NomineeForm(Pdf_ModificationModel pdfModel)
        {
            MemoryStream ms = new MemoryStream();
            NomineeForm form = new NomineeForm();
            IEnumerable<Pdf_ModificationModel> esigns = new List<Pdf_ModificationModel>() { pdfModel };
            form.DataSource = esigns;
            ImageExportOptions imageExportOptions = new ImageExportOptions()
            {
                ExportMode = ImageExportMode.SingleFile
            };
            ms.Seek(0, SeekOrigin.Begin);
            form.ExportToImage(ms);
            return ms;
        }*/

        /*private dynamic GenerateNewPDF(string refNo, string userId, Pdf_ModificationModel pdfModel)
        {

            MemoryStream ms = new MemoryStream();
            ms.Seek(0, SeekOrigin.Begin);
            try
            {
                UtilityCommon objUtility = new UtilityCommon();
                string sqlQry = "Select ma_filename fileName, ma_field fieldName, ma_proof attachment, ma_Nfiller1 NFiller1 From Client_ModifyAttach where ma_cmcd = '" + userId + "' and ma_refno = '" + refNo + "' and ma_status = 'N' ";
                string jsonString = "", adrPhoto = "", signPhoto = "";
                DataTable dbTable = objUtility.OpenDataTable(sqlQry);
                if (dbTable.Rows.Count > 0)
                {
                    List<RekycAttachmentTable> rekycAttach = CommonExtensions.ConvertToList<RekycAttachmentTable>(dbTable);

                    var adharJsonObj = rekycAttach.FirstOrDefault(x => x.fileName.ToLower() == "digilockerjson");
                    if (adharJsonObj != null)
                    {
                        var byteAdharJson = adharJsonObj.attachment;
                        jsonString = JObject.Parse(Encoding.UTF8.GetString((byte[])byteAdharJson)).ToString();
                    }
                    var adharPhoto = rekycAttach.FirstOrDefault(x => x.fieldName.ToLower() == "photoattachment");
                    if (adharPhoto != null)
                    {
                        adrPhoto = Encoding.UTF8.GetString((byte[])adharPhoto.attachment);
                    }
                    var signImage = rekycAttach.FirstOrDefault(x => x.fieldName.ToLower() == "signattachment");
                    if (signImage != null)
                    {
                        signPhoto = Encoding.UTF8.GetString((byte[])signImage.attachment);
                        signPhoto = signPhoto.Replace("data:image/jpeg;base64,", "");
                    }
                    using (PdfDocumentProcessor processor = new PdfDocumentProcessor())
                    {
                        processor.CreateEmptyDocument(ms);
                        using (DevExpress.Pdf.PdfGraphics graph = processor.CreateGraphics())
                        {
                            var formMs = Pdf_NewKRAForm(pdfModel, jsonString, adrPhoto, signPhoto);
                            PdfDocumentProcessor pdfDocumentProcessor = new PdfDocumentProcessor();
                            pdfDocumentProcessor.LoadDocument(formMs);
                            var countPage = pdfDocumentProcessor.Document.Pages.Count;
                            for (int i = 0; i < 2; i++)
                            {
                                MemoryStream mainStream = new MemoryStream();
                                formMs = new MemoryStream();
                                var size = new System.Drawing.Size(Convert.ToInt32(pdfDocumentProcessor.Document.Pages[i].MediaBox.Width), Convert.ToInt32(pdfDocumentProcessor.Document.Pages[i].MediaBox.Height));
                                IEnumerable<int> myEnumberable = new List<int>() { i + 1 };
                                pdfDocumentProcessor.CreateTiff(formMs, size.Height, myEnumberable);
                                Image panImage = Image.FromStream(formMs);
                                float aspectRatio = (float)panImage.Width / (float)panImage.Height;
                                float newWidthImg = size.Width; // Adjust this size as needed
                                float newHeight = newWidthImg / aspectRatio;
                                RectangleF panRect = new RectangleF(10, 10, newWidthImg, newHeight);
                                graph.DrawImage(panImage, panRect);
                                processor.RenderNewPage(PdfPaperSize.A4, graph);
                            }
                            //byte[] bytes = formMs.ToArray();
                            //var point = new PointF(15, 15);
                            //graph.DrawImage(bytes, point);
                            //processor.RenderNewPage(PdfPaperSize.A4, graph);
                        }

                    }
                    string digitalDocument = Convert.ToBase64String(ms.ToArray());

                    return digitalDocument;
                }

            }
            catch (Exception e)
            {
            }
            return null;
        }*/

        /*private MemoryStream Pdf_NewKRAForm(Pdf_ModificationModel model, string jsonString, string adharBase64, string signBase64)
        {
            MemoryStream ms = new MemoryStream();
            NewKRAForm1 form = new NewKRAForm1();
            IEnumerable<Pdf_ModificationModel> regPdf = new List<Pdf_ModificationModel>() { model };
            form.DataSource = regPdf;
            ImageExportOptions imageExportOptions = new ImageExportOptions()
            {
                ExportMode = ImageExportMode.SingleFile
            };
            ms.Seek(0, SeekOrigin.Begin);
            form.ExportToPdf(ms);
            return ms;
        }*/

        #endregion


        #region Helper method

        private dynamic SendEmail()
        {
            return "Email has been sent successfully!!";
        }

        private string MaskedIdentityNumber(string identityNumber)
        {
            if (!string.IsNullOrEmpty(identityNumber))
            {
                var aStringBuilder = new StringBuilder(identityNumber);
                aStringBuilder.Remove(0, identityNumber.Length - 4);
                aStringBuilder.Insert(0, "X", identityNumber.Length - 4);
                identityNumber = aStringBuilder.ToString();
            }
            return identityNumber;
        }

        /*private void SaveDigiLockerPhotoAddressAttchment(string jsonString, string userId, string refNo)
        {
            var jsonObj = JObject.Parse(jsonString);
            var photo = Convert.ToString(jsonObj["aadhaar"]["photo"]);
            byte[] bytesPhoto = Encoding.UTF8.GetBytes(photo);
            var result = _tradeWebRepository.AddEsignPdf(userId, bytesPhoto, "Photo", "PhotoAttachment", refNo);

            var adharMs = GetAdharStreamByJson(jsonString);
            byte[] bytes = adharMs.ToArray();

            using (var ms = new MemoryStream(bytes))
            {
                var img = Image.FromStream(ms);

                using (var ms2 = new MemoryStream())
                {
                    img.Save(ms2, img.RawFormat);
                    var byteAadhar = ms2.ToArray();
                    var adharsave = _tradeWebRepository.AddEsignPdf(userId, byteAadhar, "ID", "IdAttachment", refNo);
                }
            }

        }*/

        /*private MemoryStream GetAdharStream(string AdharXml)
        {
            AdharReport adharDocument = new AdharReport();
            XmlDocument myDoc = new XmlDocument();

            MemoryStream ms = new MemoryStream();

            myDoc.LoadXml(AdharXml);

            System.Xml.XmlNode PoiNod = myDoc.SelectSingleNode("KycRes/UidData/Poi");
            System.Xml.XmlNode PoaNod = myDoc.SelectSingleNode("KycRes/UidData/Poa");
            System.Xml.XmlNode UidDataNod = myDoc.SelectSingleNode("KycRes/UidData");
            System.Xml.XmlNode UidImageNod = myDoc.SelectSingleNode("KycRes/UidData/Pht");
            string AdharId = UidDataNod.Attributes.GetNamedItem("uid").InnerText;

            IEnumerable<AdharDocumentModel> model = new List<AdharDocumentModel>() {
                new AdharDocumentModel{
                    AdharNo = AdharId,
                    Co = PoaNod.Attributes.GetNamedItem("co").InnerText,
                    Country = PoaNod.Attributes.GetNamedItem("country").InnerText,
                    Dist = PoaNod.Attributes.GetNamedItem("dist").InnerText,
                    DOB = PoiNod.Attributes.GetNamedItem("dob").InnerText,
                    Gender = (PoiNod.Attributes.GetNamedItem("gender").InnerText == "M" ? "Male" : ((PoiNod.Attributes.GetNamedItem("gender").InnerText) == "F" ? "Female" : "Other")),
                    House = PoaNod.Attributes.GetNamedItem("house").InnerText,
                    Loc = PoaNod.Attributes.GetNamedItem("loc").InnerText,
                    Name = PoiNod.Attributes.GetNamedItem("name").InnerText,
                    PinCode = PoaNod.Attributes.GetNamedItem("pc").InnerText,
                    State = PoaNod.Attributes.GetNamedItem("state").InnerText,
                    Street = PoaNod.Attributes.GetNamedItem("street").InnerText,
                    Vtc = PoaNod.Attributes.GetNamedItem("vtc").InnerText,
                     ImageFile = UidImageNod.InnerText
                }
            };

            adharDocument.DataSource = model;

            ImageExportOptions imageExportOptions = new ImageExportOptions()
            {
                ExportMode = ImageExportMode.SingleFile
            };

            ms.Seek(0, SeekOrigin.Begin);
            adharDocument.ExportToImage(ms);

            return ms;
        }*/

        /*private MemoryStream GetAdharStreamByJson(string adharJson)
        {
            AdharReport adharDocument = new AdharReport();
            MemoryStream ms = new MemoryStream();

            var jsonObj = JObject.Parse(adharJson);
            var address = Convert.ToString(jsonObj["aadhaar"]["address"]);
            var addressObj = JObject.Parse(address);

            IEnumerable<AdharDocumentModel> model = new List<AdharDocumentModel>() {
                new AdharDocumentModel{
                    AdharNo = Convert.ToString(jsonObj["aadhaar"]["maskedNumber"]),
                    Co = Convert.ToString(addressObj["careOf"]),
                    Country = Convert.ToString(addressObj["country"]),
                    Dist = Convert.ToString(addressObj["district"]),
                    DOB = Convert.ToString(jsonObj["aadhaar"]["dateOfBirth"]),
                    Gender = (Convert.ToString(jsonObj["aadhaar"]["gender"]) == "M" ? "Male" : (Convert.ToString(jsonObj["aadhaar"]["gender"]) == "F" ? "Female" : "Other")),
                    House = Convert.ToString(addressObj["house"]),
                    Loc = Convert.ToString(addressObj["locality"]),
                    Name = Convert.ToString(jsonObj["aadhaar"]["name"]),
                    PinCode =Convert.ToString(addressObj["pin"]),
                    State = Convert.ToString(addressObj["state"]),
                    Street = Convert.ToString(addressObj["street"]),
                    Vtc = Convert.ToString(addressObj["vtc"]),
                    ImageFile = Convert.ToString(jsonObj["aadhaar"]["photo"]),
                }
            };
            adharDocument.DataSource = model;
            ImageExportOptions imageExportOptions = new ImageExportOptions()
            {
                ExportMode = ImageExportMode.SingleFile
            };
            ms.Seek(0, SeekOrigin.Begin);
            adharDocument.ExportToImage(ms);
            return ms;
        }*/

        #endregion




    }
}