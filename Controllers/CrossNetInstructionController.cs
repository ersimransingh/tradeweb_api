using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using TradeWeb.API.Data;
using TradeWeb.API.Models;
using TradeWeb.API.Repository;
using TradeWeb.Service;
//using static Microsoft.EntityFrameworkCore.DbLoggerCategory;


namespace TradeWeb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CrossNetInstructionController : Controller
    {
        #region Class level declarations.
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly UtilityCommon objUtility;
        private readonly ICrossNetRepository _crossNetRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IWebHostEnvironment _environment;

        private string strsql = "";
        ConvertData returnJson = new ConvertData();
        DataTable returnDt = new DataTable();

        #endregion

        #region Constructor
        public CrossNetInstructionController(UserManager<AppUser> userManager, IConfiguration configuration, UtilityCommon objUtility, IHttpContextAccessor httpContextAccessor, SignInManager<AppUser> signInManager, ICrossNetRepository crossNetRepository, IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _configuration = configuration;
            this.objUtility = objUtility;
            _httpContextAccessor = httpContextAccessor;
            _signInManager = signInManager;
            _crossNetRepository = crossNetRepository;
            _environment = environment;
        }
        #endregion

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpPost("OffMarket_Post", Name = "OffMarket_Post")]
        public IActionResult OffMarket_Post(CrossOffMarketReq req)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    req.TransectionDate = objUtility.mfnReplaceForSQLInjection(req.TransectionDate);
                    req.InstrumentType = objUtility.mfnReplaceForSQLInjection(req.InstrumentType);
                    req.ExecDate = objUtility.mfnReplaceForSQLInjection(req.ExecDate);
                    req.BranchCode = objUtility.mfnReplaceForSQLInjection(req.BranchCode);
                    req.ClientID = objUtility.mfnReplaceForSQLInjection(req.ClientID);
                    req.InternalRefNo = objUtility.mfnReplaceForSQLInjection(req.InternalRefNo);
                    req.TransectionType = objUtility.mfnReplaceForSQLInjection(req.TransectionType);
                    req.ReceiveMode = objUtility.mfnReplaceForSQLInjection(req.ReceiveMode);
                    var getData = _crossNetRepository.OffMarketAdd(userId, req);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet("OffMarket_Get", Name = "OffMarket_Get")]
        public IActionResult OffMarket_Get([FromQuery] string InstrumentType, string InternalRefNo, string TransectionType)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    InstrumentType = objUtility.mfnReplaceForSQLInjection(InstrumentType);
                    InternalRefNo = objUtility.mfnReplaceForSQLInjection(InternalRefNo);
                    TransectionType = objUtility.mfnReplaceForSQLInjection(TransectionType);
                    var getData = _crossNetRepository.OffMarketFind(InstrumentType, InternalRefNo, TransectionType);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpPost("InterDipository_Post", Name = "InterDipository_Post")]
        public IActionResult InterDipository_Post(InterDipositoryAddReq req)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    req.TransectionDate = objUtility.mfnReplaceForSQLInjection(req.TransectionDate);
                    req.InstrumentType = objUtility.mfnReplaceForSQLInjection(req.InstrumentType);
                    req.ReceiveMode = objUtility.mfnReplaceForSQLInjection(req.ReceiveMode);
                    req.Branch = objUtility.mfnReplaceForSQLInjection(req.Branch);
                    req.ClientID = objUtility.mfnReplaceForSQLInjection(req.ClientID);
                    req.InternalRefNo = objUtility.mfnReplaceForSQLInjection(req.InternalRefNo);
                    req.TransectionType = objUtility.mfnReplaceForSQLInjection(req.TransectionType);
                    req.ExecutionDate = objUtility.mfnReplaceForSQLInjection(req.ExecutionDate);
                    for (int i = 0; i < req.Data.Count; i++)
                    {
                        req.Data[i].ClientID = objUtility.mfnReplaceForSQLInjection(req.Data[i].ClientID);
                        req.Data[i].ISIN = objUtility.mfnReplaceForSQLInjection(req.Data[i].ISIN);
                        req.Data[i].DPId = objUtility.mfnReplaceForSQLInjection(req.Data[i].DPId);
                        req.Data[i].CounterSettNo = objUtility.mfnReplaceForSQLInjection(req.Data[i].CounterSettNo);
                        req.Data[i].FromSettNo = objUtility.mfnReplaceForSQLInjection(req.Data[i].FromSettNo);
                        req.Data[i].Remarks = objUtility.mfnReplaceForSQLInjection(req.Data[i].Remarks);
                        req.Data[i].Reason = objUtility.mfnReplaceForSQLInjection(req.Data[i].Reason);
                        req.Data[i].PaymentMode = objUtility.mfnReplaceForSQLInjection(req.Data[i].PaymentMode);
                        req.Data[i].PayeeName = objUtility.mfnReplaceForSQLInjection(req.Data[i].PayeeName);
                        req.Data[i].ChequeOrRefNo = objUtility.mfnReplaceForSQLInjection(req.Data[i].ChequeOrRefNo);
                        req.Data[i].DateOfIssue = objUtility.mfnReplaceForSQLInjection(req.Data[i].DateOfIssue);
                        req.Data[i].BankAccountNo = objUtility.mfnReplaceForSQLInjection(req.Data[i].BankAccountNo);
                        req.Data[i].BankName = objUtility.mfnReplaceForSQLInjection(req.Data[i].BankName);
                        req.Data[i].BranchName = objUtility.mfnReplaceForSQLInjection(req.Data[i].BranchName);
                        req.Data[i].Consideration = objUtility.mfnReplaceForSQLInjection(req.Data[i].Consideration);
                        req.Data[i].PaidBy = objUtility.mfnReplaceForSQLInjection(req.Data[i].PaidBy);
                        req.Data[i].Exchange = objUtility.mfnReplaceForSQLInjection(req.Data[i].Exchange);
                        req.Data[i].Segment = objUtility.mfnReplaceForSQLInjection(req.Data[i].Segment);
                        req.Data[i].UCC = objUtility.mfnReplaceForSQLInjection(req.Data[i].UCC);
                        req.Data[i].CMId = objUtility.mfnReplaceForSQLInjection(req.Data[i].CMId);
                        req.Data[i].EntryBy = objUtility.mfnReplaceForSQLInjection(req.Data[i].EntryBy);
                        req.Data[i].EarlyPayin = objUtility.mfnReplaceForSQLInjection(req.Data[i].EarlyPayin);
                        req.Data[i].TMID = objUtility.mfnReplaceForSQLInjection(req.Data[i].TMID);
                    }
                    var getData = _crossNetRepository.InterDipositoryAdd(userId, req);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet("InterDipository_Get", Name = "InterDipository_Get")]
        public IActionResult InterDipository_Get([FromQuery] string InstrumentType, string InternalRefNo, string TransectionType)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    InstrumentType = objUtility.mfnReplaceForSQLInjection(InstrumentType);
                    InternalRefNo = objUtility.mfnReplaceForSQLInjection(InternalRefNo);
                    TransectionType = objUtility.mfnReplaceForSQLInjection(TransectionType);
                    var getData = _crossNetRepository.InterDepositoryFind(InstrumentType, InternalRefNo, TransectionType);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpPost("OnMarket_Post", Name = "OnMarket_Post")]
        public IActionResult OnMarket_Post(OnMarketReq req)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    req.TransectionDate = objUtility.mfnReplaceForSQLInjection(req.TransectionDate);
                    req.InstrumentType = objUtility.mfnReplaceForSQLInjection(req.InstrumentType);
                    req.BranchCode = objUtility.mfnReplaceForSQLInjection(req.BranchCode);
                    req.ClientID = objUtility.mfnReplaceForSQLInjection(req.ClientID);
                    req.InternalRefNo = objUtility.mfnReplaceForSQLInjection(req.InternalRefNo);
                    req.TransectionType = objUtility.mfnReplaceForSQLInjection(req.TransectionType);
                    req.ReceiveMode = objUtility.mfnReplaceForSQLInjection(req.ReceiveMode);
                    for (int i = 0; i < req.Data.Count; i++)
                    {
                        req.Data[i].ClientID = objUtility.mfnReplaceForSQLInjection(req.Data[i].ClientID);
                        req.Data[i].ISIN = objUtility.mfnReplaceForSQLInjection(req.Data[i].ISIN);
                        req.Data[i].SettlementID = objUtility.mfnReplaceForSQLInjection(req.Data[i].SettlementID);
                        req.Data[i].TMId = objUtility.mfnReplaceForSQLInjection(req.Data[i].TMId);
                        req.Data[i].Exchange = objUtility.mfnReplaceForSQLInjection(req.Data[i].Exchange);
                        req.Data[i].Segment = objUtility.mfnReplaceForSQLInjection(req.Data[i].Segment);
                        req.Data[i].UCC = objUtility.mfnReplaceForSQLInjection(req.Data[i].UCC);
                        req.Data[i].CMId = objUtility.mfnReplaceForSQLInjection(req.Data[i].CMId);
                        req.Data[i].EntryBy = objUtility.mfnReplaceForSQLInjection(req.Data[i].EntryBy);
                        req.Data[i].Remark = objUtility.mfnReplaceForSQLInjection(req.Data[i].Remark);
                    }
                    var getData = _crossNetRepository.OnMarketAdd(userId, req);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet("OnMarket_Get", Name = "OnMarket_Get")]
        public IActionResult OnMarket_Get([FromQuery] string InstrumentType, string InternalRefNo, string TransectionType)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    InstrumentType = objUtility.mfnReplaceForSQLInjection(InstrumentType);
                    InternalRefNo = objUtility.mfnReplaceForSQLInjection(InternalRefNo);
                    TransectionType = objUtility.mfnReplaceForSQLInjection(TransectionType);
                    var getData = _crossNetRepository.OnMarketFind(InstrumentType, InternalRefNo, TransectionType);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpPost("EarlyPayin_Post", Name = "EarlyPayin_Post")]
        public IActionResult EarlyPayin_Post(EarlyPayInReq req)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    req.TransectionDate = objUtility.mfnReplaceForSQLInjection(req.TransectionDate);
                    req.InstrumentType = objUtility.mfnReplaceForSQLInjection(req.InstrumentType);
                    req.BranchCode = objUtility.mfnReplaceForSQLInjection(req.BranchCode);
                    req.ClientID = objUtility.mfnReplaceForSQLInjection(req.ClientID);
                    req.InternalRefNo = objUtility.mfnReplaceForSQLInjection(req.InternalRefNo);
                    req.TransectionType = objUtility.mfnReplaceForSQLInjection(req.TransectionType);
                    req.ReceiveMode = objUtility.mfnReplaceForSQLInjection(req.ReceiveMode);
                    for (int i = 0; i < req.Data.Count; i++)
                    {
                        req.Data[i].ClientID = objUtility.mfnReplaceForSQLInjection(req.Data[i].ClientID);
                        req.Data[i].ISIN = objUtility.mfnReplaceForSQLInjection(req.Data[i].ISIN);
                        req.Data[i].SettlementID = objUtility.mfnReplaceForSQLInjection(req.Data[i].SettlementID);
                        req.Data[i].TMId = objUtility.mfnReplaceForSQLInjection(req.Data[i].TMId);
                        req.Data[i].Exchange = objUtility.mfnReplaceForSQLInjection(req.Data[i].Exchange);
                        req.Data[i].Segment = objUtility.mfnReplaceForSQLInjection(req.Data[i].Segment);
                        req.Data[i].UCC = objUtility.mfnReplaceForSQLInjection(req.Data[i].UCC);
                        req.Data[i].CMId = objUtility.mfnReplaceForSQLInjection(req.Data[i].CMId);
                        req.Data[i].EntryBy = objUtility.mfnReplaceForSQLInjection(req.Data[i].EntryBy);
                        req.Data[i].Remark = objUtility.mfnReplaceForSQLInjection(req.Data[i].Remark);
                        req.Data[i].CounterClientID = objUtility.mfnReplaceForSQLInjection(req.Data[i].CounterClientID);
                    }
                    var getData = _crossNetRepository.EarlyPayInAdd(userId, req);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet("EarlyPayin_Get", Name = "EarlyPayin_Get")]
        public IActionResult EarlyPayin_Get([FromQuery] string InstrumentType, string InternalRefNo, string TransectionType)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    InstrumentType = objUtility.mfnReplaceForSQLInjection(InstrumentType);
                    InternalRefNo = objUtility.mfnReplaceForSQLInjection(InternalRefNo);
                    TransectionType = objUtility.mfnReplaceForSQLInjection(TransectionType);
                    var getData = _crossNetRepository.EarlyPayInFind(InstrumentType, InternalRefNo, TransectionType);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpPost("SlipIssue_Post", Name = "SlipIssue_Post")]
        public IActionResult SlipIssue_Post(SlipIssueReq req)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    req.Slip_Issue_Type = objUtility.mfnReplaceForSQLInjection(req.Slip_Issue_Type);
                    req.Id = objUtility.mfnReplaceForSQLInjection(req.Id);
                    req.Instrument = objUtility.mfnReplaceForSQLInjection(req.Instrument);
                    req.Ref_Date = objUtility.mfnReplaceForSQLInjection(req.Ref_Date);
                    req.Ref_No = objUtility.mfnReplaceForSQLInjection(req.Ref_No);
                    req.Date = objUtility.mfnReplaceForSQLInjection(req.Date);
                    var getData = _crossNetRepository.SlipIssueAdd(userId, req);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpPost("SlipIssue_Get", Name = "SlipIssue_Get")]
        public IActionResult SlipIssue_Get(SlipIssueRequest req)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    req.Issued_To = objUtility.mfnReplaceForSQLInjection(req.Issued_To);
                    req.From_Date = objUtility.mfnReplaceForSQLInjection(req.From_Date);
                    req.To_Date = objUtility.mfnReplaceForSQLInjection(req.To_Date);

                    var getData = _crossNetRepository.SlipIssueFind(req);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpPost("SlipStop_Post", Name = "SlipStop_Post")]
        public IActionResult SlipStop_Post(SlipStopReq req)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    req.ClientID = objUtility.mfnReplaceForSQLInjection(req.ClientID);
                    req.Instrument = objUtility.mfnReplaceForSQLInjection(req.Instrument);
                    req.RefDate = objUtility.mfnReplaceForSQLInjection(req.RefDate);
                    req.Reference = objUtility.mfnReplaceForSQLInjection(req.Reference);
                    req.Date = objUtility.mfnReplaceForSQLInjection(req.Date);
                    req.Remarks = objUtility.mfnReplaceForSQLInjection(req.Remarks);

                    var getData = _crossNetRepository.SlipStop(userId, req);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpPost("ReceiptPayment_Post", Name = "ReceiptPayment_Post")]
        public IActionResult ReceiptPayment_Post(ReceiptAddReq req)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    req.Type = objUtility.mfnReplaceForSQLInjection(req.Type);
                    req.SrNo = objUtility.mfnReplaceForSQLInjection(req.SrNo);
                    req.VoucherNo = objUtility.mfnReplaceForSQLInjection(req.VoucherNo);
                    req.Date = objUtility.mfnReplaceForSQLInjection(req.Date);
                    req.ClearedOn = objUtility.mfnReplaceForSQLInjection(req.ClearedOn);
                    req.BankCode = objUtility.mfnReplaceForSQLInjection(req.BankCode);
                    for (int i = 0; i < req.Entries.Count; i++)
                    {
                        req.Entries[i].Account = objUtility.mfnReplaceForSQLInjection(req.Entries[i].Account);
                        req.Entries[i].ChequeNo = objUtility.mfnReplaceForSQLInjection(req.Entries[i].ChequeNo);
                        req.Entries[i].MICR = objUtility.mfnReplaceForSQLInjection(req.Entries[i].MICR);
                        req.Entries[i].Particular = objUtility.mfnReplaceForSQLInjection(req.Entries[i].Particular);
                    }
                    var getData = _crossNetRepository.ReceiptPaymentAdd(req, userId);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet("ReceiptPayment_Get", Name = "ReceiptPayment_Get")]
        public IActionResult ReceiptPayment_Get([FromQuery] string Type, string SrNo)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    Type = objUtility.mfnReplaceForSQLInjection(Type);
                    SrNo = objUtility.mfnReplaceForSQLInjection(SrNo);

                    var getData = _crossNetRepository.ReceiptPaymentFind(Type, SrNo);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet("ReceiptPayment_Delete", Name = "ReceiptPayment_Delete")]
        public IActionResult ReceiptPayment_Delete([FromQuery] string Type, string SerialNo)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    Type = objUtility.mfnReplaceForSQLInjection(Type);
                    SerialNo = objUtility.mfnReplaceForSQLInjection(SerialNo);
                    var getData = _crossNetRepository.ReceiptPaymentDelete(Type, SerialNo, userId);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        /*[Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpPost("Payment_Post", Name = "Payment_Post")]
        public IActionResult Payment_Post(ReceiptAddReq req)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var getData = _crossNetRepository.PaymentAdd(req, userId);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet("Payment_Get", Name = "Payment_Get")]
        public IActionResult Payment_Get([FromQuery] string SerialNo)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var getData = _crossNetRepository.PaymentFind(SerialNo);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet("Payment_Delete", Name = "Payment_Delete")]
        public IActionResult Payment_Delete([FromQuery] string SerialNo)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var getData = _crossNetRepository.PaymentDelete(SerialNo, userId);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }*/

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpPost("ReceiptPaymentEntries_Get", Name = "ReceiptPaymentEntries_Get")]
        public IActionResult ReceiptPaymentEntries_Get(ReceiptPaymentEntriesReq req)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    req.FromDate = objUtility.mfnReplaceForSQLInjection(req.FromDate);
                    req.ToDate = objUtility.mfnReplaceForSQLInjection(req.ToDate);
                    req.IsReceipt = objUtility.mfnReplaceForSQLInjection(req.IsReceipt);
                    req.IsPayment = objUtility.mfnReplaceForSQLInjection(req.IsPayment);

                    var getData = _crossNetRepository.ReceiptPaymentEntries(req, loginAccess);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpPost("Journal_Post", Name = "Journal_Post")]
        public IActionResult Journal_Post(JournalRequest req)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    req.SrNo = objUtility.mfnReplaceForSQLInjection(req.SrNo);
                    req.Date = objUtility.mfnReplaceForSQLInjection(req.Date);
                    req.VoucherNo = objUtility.mfnReplaceForSQLInjection(req.VoucherNo);
                    req.Particular = objUtility.mfnReplaceForSQLInjection(req.Particular);
                    for (int i = 0; i < req.Entries.Count; i++)
                    {
                        req.Entries[i].Account = objUtility.mfnReplaceForSQLInjection(req.Entries[i].Account);
                        req.Entries[i].DrCr = objUtility.mfnReplaceForSQLInjection(req.Entries[i].DrCr);
                        req.Entries[i].Particular = objUtility.mfnReplaceForSQLInjection(req.Entries[i].Particular);
                    }
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var getData = _crossNetRepository.JournalAdd(req, userId);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet("Journal_Get", Name = "Journal_Get")]
        public IActionResult Journal_Get([FromQuery] string SrNo)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    SrNo = objUtility.mfnReplaceForSQLInjection(SrNo);
                    var getData = _crossNetRepository.JournalFind(SrNo);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet("Journal_Delete", Name = "Journal_Delete")]
        public IActionResult Journal_Delete([FromQuery] string SerialNo)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    SerialNo = objUtility.mfnReplaceForSQLInjection(SerialNo);
                    var getData = _crossNetRepository.JournalDelete(SerialNo, userId);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpPost("DebitCreditNote_Post", Name = "DebitCreditNote_Post")]
        public IActionResult DebitCreditNote_Post(DebitNotesRequest req)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    req.Type = objUtility.mfnReplaceForSQLInjection(req.Type);
                    req.SrNo = objUtility.mfnReplaceForSQLInjection(req.SrNo);
                    req.Date = objUtility.mfnReplaceForSQLInjection(req.Date);
                    req.Account = objUtility.mfnReplaceForSQLInjection(req.Account);
                    req.CounterAccount = objUtility.mfnReplaceForSQLInjection(req.CounterAccount);
                    req.Particular = objUtility.mfnReplaceForSQLInjection(req.Particular);

                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var getData = _crossNetRepository.DebitCreditNotesAdd(req, userId);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet("DebitCreditNote_Get", Name = "DebitCreditNote_Get")]
        public IActionResult DebitCreditNote_Get([FromQuery] string Type, string SrNo)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    Type = objUtility.mfnReplaceForSQLInjection(Type);
                    SrNo = objUtility.mfnReplaceForSQLInjection(SrNo);
                    var getData = _crossNetRepository.DebitCreditNotesFind(Type, SrNo);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet("DebitCreditNote_Delete", Name = "DebitCreditNote_Delete")]
        public IActionResult DebitCreditNote_Delete([FromQuery] string Type, string DebitNote)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    Type = objUtility.mfnReplaceForSQLInjection(Type);
                    DebitNote = objUtility.mfnReplaceForSQLInjection(DebitNote);
                    var getData = _crossNetRepository.DebitCreditNotesDelete(Type, DebitNote, userId);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        /*[Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpPost("CreditNotes_Post", Name = "CreditNotes_Post")]
        public IActionResult CreditNotes_Post(CreditNotesRequest req)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var getData = _crossNetRepository.CreditNotesAdd(req, userId);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet("CreditNote_Get", Name = "CreditNote_Get")]
        public IActionResult CreditNote_Get([FromQuery] string CreditNote)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var getData = _crossNetRepository.CreditNotesFind(CreditNote);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet("CreditNote_Delete", Name = "CreditNote_Delete")]
        public IActionResult CreditNote_Delete([FromQuery] string CreditNote)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var getData = _crossNetRepository.CreditNotesDelete(CreditNote, userId);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }*/

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpPost("Audit_Get", Name = "Audit_Get")]
        public IActionResult Audit_Get(AuditRequest req)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    req.FromDate = objUtility.mfnReplaceForSQLInjection(req.FromDate);
                    req.ToDate = objUtility.mfnReplaceForSQLInjection(req.ToDate);

                    var getData = _crossNetRepository.AuditFind(req, userId);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet("BranchSlipIssue_Master", Name = "BranchSlipIssue_Master")]
        public IActionResult BranchSlipIssue_Master([FromQuery] string BranchCd)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    BranchCd = objUtility.mfnReplaceForSQLInjection(BranchCd);
                    var getData = _crossNetRepository.BranchSilpIssue(BranchCd);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet("GroupSlipIssue_Master", Name = "GroupSlipIssue_Master")]
        public IActionResult GroupSlipIssue_Master([FromQuery] string GroupCd)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    GroupCd = objUtility.mfnReplaceForSQLInjection(GroupCd);
                    var getData = _crossNetRepository.GroupSilpIssue(GroupCd);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet("FamilySlipIssue_Master", Name = "FamilySlipIssue_Master")]
        public IActionResult FamilySlipIssue_Master([FromQuery] string FamilyCd)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    FamilyCd = objUtility.mfnReplaceForSQLInjection(FamilyCd);
                    var getData = _crossNetRepository.FamilySilpIssue(FamilyCd);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet("POASlipIssue_Master", Name = "POASlipIssue_Master")]
        public IActionResult POASlipIssue_Master([FromQuery] string POA_Id, string SlipNo)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    POA_Id = objUtility.mfnReplaceForSQLInjection(POA_Id);
                    SlipNo = objUtility.mfnReplaceForSQLInjection(SlipNo);
                    var getData = _crossNetRepository.POASlipIssue(POA_Id, SlipNo);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet("ClientSlipIssue_Master", Name = "ClientSlipIssue_Master")]
        public IActionResult ClientSlipIssue_Master([FromQuery] string ClientCd, string SlipNo)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    ClientCd = objUtility.mfnReplaceForSQLInjection(ClientCd);
                    SlipNo = objUtility.mfnReplaceForSQLInjection(SlipNo);
                    var getData = _crossNetRepository.ClientSlipIssue(ClientCd, SlipNo);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet("Validate_SlipNo", Name = "Validate_SlipNo")]
        public IActionResult Validate_SlipNo([FromQuery] string InstrumentType, string InternalRefNo)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    InstrumentType = objUtility.mfnReplaceForSQLInjection(InstrumentType);
                    InternalRefNo = objUtility.mfnReplaceForSQLInjection(InternalRefNo);
                    var getData = _crossNetRepository.GetInterDepositoryClientCd(InstrumentType, InternalRefNo);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet("Master_InstrumentType", Name = "Master_InstrumentType")]
        public IActionResult Master_InstrumentType([FromQuery] string InstCd)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    InstCd = objUtility.mfnReplaceForSQLInjection(InstCd);
                    var getData = _crossNetRepository.InstrumentTypeSearch(InstCd);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet("Master_ReceiveMode", Name = "Master_ReceiveMode")]
        public IActionResult Master_ReceiveMode([FromQuery] string ReceiveCd)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    ReceiveCd = objUtility.mfnReplaceForSQLInjection(ReceiveCd);
                    var getData = _crossNetRepository.ReceiveModeSearch(ReceiveCd);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet("Master_PaymentMode", Name = "Master_PaymentMode")]
        public IActionResult Master_PaymentMode([FromQuery] string PaymentCd)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    PaymentCd = objUtility.mfnReplaceForSQLInjection(PaymentCd);
                    var getData = _crossNetRepository.PaymentModeSearch(PaymentCd);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet("Master_Reason", Name = "Master_Reason")]
        public IActionResult Master_Reason([FromQuery] string ReasonCd)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    ReasonCd = objUtility.mfnReplaceForSQLInjection(ReasonCd);
                    var getData = _crossNetRepository.ReasonSearch(ReasonCd);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet("Master_PaidBy", Name = "Master_PaidBy")]
        public IActionResult Master_PaidBy([FromQuery] string PaidByCd)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    PaidByCd = objUtility.mfnReplaceForSQLInjection(PaidByCd);
                    var getData = _crossNetRepository.PaidBySearch(PaidByCd);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet("Master_EarlyPay", Name = "Master_EarlyPay")]
        public IActionResult Master_EarlyPay([FromQuery] string EarlyPayCd)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    EarlyPayCd = objUtility.mfnReplaceForSQLInjection(EarlyPayCd);
                    var getData = _crossNetRepository.EarlyPaySearch(EarlyPayCd);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet("Master_EntryBy", Name = "Master_EntryBy")]
        public IActionResult Master_EntryBy([FromQuery] string EntryCd)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    EntryCd = objUtility.mfnReplaceForSQLInjection(EntryCd);
                    var getData = _crossNetRepository.EntryBySearch(EntryCd);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet, Route("Master_Segment")]
        public IActionResult Master_Segment(string Segment)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    Segment = objUtility.mfnReplaceForSQLInjection(Segment);
                    var getData = _crossNetRepository.GetSegment(Segment);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }


        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet, Route("Master_Exchange")]
        public IActionResult Master_Exchange(string Exchange)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    Exchange = objUtility.mfnReplaceForSQLInjection(Exchange);
                    var getData = _crossNetRepository.GetExchange(Exchange);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpGet, Route("Master_UCC")]
        public IActionResult Master_UCC(string BOID)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    BOID = objUtility.mfnReplaceForSQLInjection(BOID);
                    var getData = _crossNetRepository.GetUCCDetails(BOID);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        private JwtSecurityToken GetToken()
        {
            var handler = new JwtSecurityTokenHandler();
            string authHeader = Request.Headers["Authorization"];
            authHeader = authHeader.Replace("Bearer ", "");
            var token = handler.ReadToken(authHeader) as JwtSecurityToken;
            return token;
        }
    }
}
