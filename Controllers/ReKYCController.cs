using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using TradeWeb.API.ExtentionMethod;
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
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
        }

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
                                        esignUrl = UploadKRApdfForEsign(refNo, pdfBase64, mobNo, firstName, dob, rtnUrl, userId + "_KRA.pdf", userId);
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

        private string UploadPdfForEsignSetu(string refNo, string pdfBase64, string identifierMobileNo, string DispName, string yob, string esignRedirectUrl, string fileName, string clientCode)
        {
            try
            {
                string esignReqId = string.Empty;
                string vendor = "SETU";
                #region upload file for esign
                var jsonObj = new
                {
                    ClientCode = clientCode,
                    base64 = pdfBase64,
                    FileName = fileName,
                    FilePassword = ""
                };
                var jsonParams = JsonConvert.SerializeObject(jsonObj);
                try
                {
                    var resp = _modelTRadeWeb.CallSetuProcedure(vendor, "EsignRequest", "ReKYC", jsonParams, refNo);
                    //var parseJson = JObject.Parse(resp);
                    esignReqId = Convert.ToString(resp["id"]);
                    refNo = _tradeWebRepository.ReKYC_MasterTable(clientCode, "Pending", "N", 3, "Call Esign for upload pdf for Request Id: " + esignReqId, "ReKYC");
                }
                catch (Exception ex)
                {
                    return "Issue while uploading pdf on setu server \\n " + ex.Message.ToString();
                }

                #endregion

                #region Create signature for esign
                var baseString = Convert.FromBase64String(pdfBase64);
                int countPage;
                using (PdfReader reader = new PdfReader(baseString))
                {
                    countPage = reader.NumberOfPages;
                }

                var jsonSignerObj = new
                {
                    ClientCode = clientCode,
                    Requestid = esignReqId,
                    redirectUrl = esignRedirectUrl,
                    identifier = identifierMobileNo,
                    displayName = DispName,
                    height = "60",
                    onPages = countPage,
                    position = "bottom-right",
                    width = "180",
                    birthYear = yob
                };
                var jsonVal = JsonConvert.SerializeObject(jsonSignerObj);
                try
                {
                    var getSigner = _modelTRadeWeb.CallSetuProcedure(vendor, "CreateSign", "ReKYC", jsonVal, refNo);
                    string signerUrl = "";
                    try
                    {
                        if (getSigner == null)
                        {
                            string strSql = string.Empty;
                            strSql = "Select * From Client_ModifyAttach (NoLock) where ma_cmcd = '" + clientCode + "' and ma_status = 'N' and ma_filename = 'EsignRequest' and ma_refNo = " + refNo;
                            DataTable dt = _objUtility.OpenDataTable(strSql);
                            if (dt.Rows.Count > 0)
                            {
                                var getEsignReq = Newtonsoft.Json.Linq.JObject.Parse(Encoding.UTF8.GetString((byte[])dt.Rows[0]["ma_proof"]).ToString());
                                var esignUrl = (string)getEsignReq["signers"][0]["url"];
                                return esignUrl;
                            }
                        }
                        if (getSigner["ResponseFlag"] == "E")
                        {
                            return "Pdf uploaded on setu, Please submit again to get pdf and Esign url. \\n " + getSigner["ResponseMessage"];
                        }
                        signerUrl = Convert.ToString(getSigner["signers"][0]["url"]);
                        var serlzJson = JsonConvert.SerializeObject(getSigner);
                        refNo = _tradeWebRepository.ReKYC_MasterTable(clientCode, "Pending", "N", 3, "PDF uploaded on setu, Pending for Esign (Request Id:" + esignReqId, "ReKYC");
                        byte[] getJsonBytes = Encoding.UTF8.GetBytes(serlzJson);
                        var result = _tradeWebRepository.AddEsignPdf(clientCode, getJsonBytes, "EsignRequest", "Esign Request Output for Request Id=" + esignReqId, refNo);
                        return signerUrl;
                    }
                    catch (Exception e)
                    {
                        return "Pdf uploaded on setu, Please submit again to get pdf and Esign url. \\n " + e.Message.ToString();
                    }
                }
                catch (WebException exception)
                {
                    string responseText = string.Empty;

                    var responseStream = exception.Response?.GetResponseStream();

                    if (responseStream != null)
                    {
                        using (var reader = new StreamReader(responseStream))
                        {
                            responseText = reader.ReadToEnd();
                        }
                    }
                    return responseText;
                }
                #endregion
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private string UploadKRApdfForEsign(string refNo, string pdfBase64, string identifierMobileNo, string DispName, string yob, string esignRedirectUrl, string fileName, string clientCode)
        {
            try
            {
                string esignReqId = string.Empty;
                string vendor = "SETU";
                #region upload file for esign
                var jsonObj = new
                {
                    ClientCode = clientCode,
                    base64 = pdfBase64,
                    FileName = fileName,
                    FilePassword = ""
                };
                var jsonParams = JsonConvert.SerializeObject(jsonObj);
                try
                {
                    var resp = _modelTRadeWeb.CallSetuProcedure(vendor, "EsignRequestKRA", "KRA", jsonParams, refNo);
                    esignReqId = Convert.ToString(resp["id"]);
                }
                catch (Exception ex)
                {
                    return "Issue while uploading pdf on setu server \\n " + ex.Message.ToString();
                }

                #endregion

                #region Create signature for esign
                var baseString = Convert.FromBase64String(pdfBase64);
                int countPage;
                using (PdfReader reader = new PdfReader(baseString))
                {
                    countPage = reader.NumberOfPages;
                }

                var jsonSignerObj = new
                {
                    ClientCode = clientCode,
                    Requestid = esignReqId,
                    redirectUrl = esignRedirectUrl,
                    identifier = identifierMobileNo,
                    displayName = DispName,
                    height = "60",
                    onPages = countPage,
                    position = "bottom-right",
                    width = "180",
                    birthYear = yob
                };
                var jsonVal = JsonConvert.SerializeObject(jsonSignerObj);
                try
                {
                    var getSigner = _modelTRadeWeb.CallSetuProcedure(vendor, "CreateSignKRA", "KRA", jsonVal, refNo);
                    string signerUrl = "";
                    try
                    {
                        if (getSigner == null)
                        {
                            string strSql = string.Empty;
                            strSql = "Select * From Client_ModifyAttach (NoLock) where ma_cmcd = '" + clientCode + "' and ma_status = 'N' and ma_filename = 'EsignRequestKRA' and ma_refNo = " + refNo;
                            DataTable dt = _objUtility.OpenDataTable(strSql);
                            if (dt.Rows.Count > 0)
                            {
                                var getEsignReq = Newtonsoft.Json.Linq.JObject.Parse(Encoding.UTF8.GetString((byte[])dt.Rows[0]["ma_proof"]).ToString());
                                var esignUrl = (string)getEsignReq["signers"][0]["url"];
                                return esignUrl;
                            }
                        }
                        if (getSigner["ResponseFlag"] == "E")
                        {
                            return "Pdf uploaded on setu, Please submit again to get pdf and Esign url. \\n " + getSigner["ResponseMessage"];
                        }
                        signerUrl = Convert.ToString(getSigner["signers"][0]["url"]);
                        var serlzJson = JsonConvert.SerializeObject(getSigner);
                        byte[] getJsonBytes = Encoding.UTF8.GetBytes(serlzJson);
                        var result = _tradeWebRepository.AddEsignPdf(clientCode, getJsonBytes, "EsignRequestKRA", "Esign Request Output for Request Id=" + esignReqId, refNo);
                        return signerUrl;
                    }
                    catch (Exception e)
                    {
                        return "Pdf uploaded on setu, Please submit again to get pdf and Esign url. \\n " + e.Message.ToString();
                    }
                }
                catch (WebException exception)
                {
                    string responseText = string.Empty;

                    var responseStream = exception.Response?.GetResponseStream();

                    if (responseStream != null)
                    {
                        using (var reader = new StreamReader(responseStream))
                        {
                            responseText = reader.ReadToEnd();
                        }
                    }
                    return responseText;
                }
                #endregion
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        private dynamic GeneratePDF(string refNo, string userId, Pdf_ModificationModel pdfModel, object jsonObj)
        {
            return new { Status = "N", UnsignedPdf = "", EsignUrl = "", Remark = "ReKYC PDF generation has been disabled." };
        }

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

        #endregion




    }
}