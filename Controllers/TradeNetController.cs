using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using TradeWeb.API.Data;
using TradeWeb.API.Models;
using TradeWeb.API.Repository;
using TradeWeb.Service;

namespace TradeWeb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TradeNetController : Controller
    {
        #region Class level declarations.
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly UtilityCommon objUtility;
        private readonly ITradeNetRepository _tradeNetRepository;
        private readonly ITradeWebRepository _tradeWebRepository;
        private readonly ICrossWebRepository _crossWebRepository;
        private readonly IEstroWebRepository _estroWebRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IWebHostEnvironment _environment;
        private string strsql = "";
        ConvertData returnJson = new ConvertData();
        DataTable returnDt = new DataTable();

        #endregion
        #region Constructor
        public TradeNetController(UserManager<AppUser> userManager, IConfiguration configuration, UtilityCommon objUtility, IHttpContextAccessor httpContextAccessor, SignInManager<AppUser> signInManager, ITradeNetRepository tradeNetRepository, IWebHostEnvironment environment, ITradeWebRepository tradeWebRepository, ICrossWebRepository crossWebRepository, IEstroWebRepository estroWebRepository)
        {
            _userManager = userManager;
            _configuration = configuration;
            this.objUtility = objUtility;
            _httpContextAccessor = httpContextAccessor;
            _signInManager = signInManager;
            _tradeNetRepository = tradeNetRepository;
            _environment = environment;
            _tradeWebRepository = tradeWebRepository;
            _crossWebRepository = crossWebRepository;
            _estroWebRepository = estroWebRepository;
        }
        #endregion
        [HttpGet("CompanyExchangeSegments", Name = "CompanyExchangeSegments")]
        public IActionResult CompanyExchangeSegments()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var getData = _tradeNetRepository.Get_exchSeg();
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpPost("OutstandingBalance", Name = "OutstandingBalance")]
        public IActionResult OutstandingBalance([FromBody] OutstandingBalanceReq req)
        {
            try
            {
                var tokenS = GetToken();
                var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                req.AsOnDate = objUtility.mfnReplaceForSQLInjection(req.AsOnDate);

                var getData = _tradeNetRepository.OutstandingBalance(userId, req, loginAccess);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpGet("LedgerSummary", Name = "LedgerSummary")]
        public IActionResult LedgerSummary(string clientCd, string type, string fromDate, string toDate)
        {
            try
            {
                var tokenS = GetToken();
                var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                fromDate = objUtility.mfnReplaceForSQLInjection(fromDate);
                toDate = objUtility.mfnReplaceForSQLInjection(toDate);
                clientCd = objUtility.mfnReplaceForSQLInjection(clientCd);
                type = objUtility.mfnReplaceForSQLInjection(type);
                var getAccess = _tradeNetRepository.Validate_LoginAccess(clientCd, loginAccess);
                if (getAccess == null || getAccess != true)
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Client is not mapped to you", returnDt, ""));
                }

                var getData = _tradeWebRepository.Ledger_Summary(clientCd, type, fromDate, toDate);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpPost("LedgerDetail", Name = "LedgerDetail")]
        public IActionResult LedgerDetail([FromBody] TradeNetLedgerDetailReqModel req)
        {
            try
            {
                var tokenS = GetToken();
                var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;

                var getAccess = _tradeNetRepository.Validate_LoginAccess(objUtility.mfnReplaceForSQLInjection(req.ClientCode), loginAccess);
                if (getAccess == null || getAccess != true)
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Client is not mapped to you", returnDt, ""));
                }

                var getData = _tradeWebRepository.Ledger_Detail(req.ClientCode, req, req.FromDate, req.ToDate);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpGet, Route("Bills_cash_stlmnt")]
        public IActionResult Bills_cash_stlmnt([FromQuery] string clientCode, string settelment)
        {
            try
            {
                var tokenS = GetToken();
                var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                clientCode = objUtility.mfnReplaceForSQLInjection(clientCode);
                settelment = objUtility.mfnReplaceForSQLInjection(settelment);
                var getAccess = _tradeNetRepository.Validate_LoginAccess(clientCode, loginAccess);
                if (getAccess == null || getAccess != true)
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Client is not mapped to you", returnDt, ""));
                }

                string dt = objUtility.fnFireQuery("settlements", "se_stdt", "se_stlmnt", settelment, true);
                var getData = _tradeWebRepository.Bill_data(clientCode, settelment.Substring(0, 1) + 'C', settelment.Substring(1, 1), dt);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpGet, Route("Bills_FO")]
        public IActionResult Bills_FO([FromQuery] string clientCode, string exch, string seg, string date)
        {
            try
            {
                var tokenS = GetToken();
                var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                clientCode = objUtility.mfnReplaceForSQLInjection(clientCode);
                exch = objUtility.mfnReplaceForSQLInjection(exch);
                seg = objUtility.mfnReplaceForSQLInjection(seg);
                date = objUtility.mfnReplaceForSQLInjection(date);
                var getAccess = _tradeNetRepository.Validate_LoginAccess(clientCode, loginAccess);
                if (getAccess == null || getAccess != true)
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Client is not mapped to you", returnDt, ""));
                }

                var getData = _tradeWebRepository.Bill_data(clientCode, exch + seg, "", date);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpGet, Route("Bills_Commodity")]
        public IActionResult Bills_Commodity([FromQuery] string clientCode, string exch, string date)
        {
            try
            {
                var tokenS = GetToken();
                var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                clientCode = objUtility.mfnReplaceForSQLInjection(clientCode);
                exch = objUtility.mfnReplaceForSQLInjection(exch);
                date = objUtility.mfnReplaceForSQLInjection(date);
                var getAccess = _tradeNetRepository.Validate_LoginAccess(clientCode, loginAccess);
                if (getAccess == null || getAccess != true)
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Client is not mapped to you", returnDt, ""));
                }

                var getData = _tradeWebRepository.Bill_data(clientCode, exch + "X", "", date);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpPost("BrokerageScheme_Cash", Name = "BrokerageScheme_Cash")]
        public IActionResult BrokerageScheme_Cash(BrokerageRequestModel model)
        {
            try
            {
                var tokenS = GetToken();
                var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                //var compId = tokenS.Claims.First(claim => claim.Type == "companyCode").Value;                
                var getData = _tradeNetRepository.BrokerageCashSegment(objUtility.mfnReplaceForSQLInjection(model.Client), objUtility.mfnReplaceForSQLInjection(model.Scheme), objUtility.mfnReplaceForSQLInjection(model.Exchange));
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpPost("BrokerageScheme_FO", Name = "BrokerageScheme_FO")]
        public IActionResult BrokerageScheme_FO(BrokerageFORequestModel model)
        {
            try
            {
                var getData = _tradeNetRepository.BrokerageFOSegment("EDIT", objUtility.mfnReplaceForSQLInjection(model.Scheme), objUtility.mfnReplaceForSQLInjection(model.ExchSeg), objUtility.mfnReplaceForSQLInjection(model.Client));
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpPost("BillSummary_Cash", Name = "BillSummary_Cash")]
        public IActionResult BillSummary_Cash([FromBody] BillSummaryModel selection)
        {
            try
            {
                var tokenS = GetToken();
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                selection.SettlementNo = objUtility.mfnReplaceForSQLInjection(selection.SettlementNo);
                var getData = _tradeNetRepository.BillSummaryCash(selection, loginAccess);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpPost("BillSummary_FO", Name = "BillSummary_FO")]
        public IActionResult BillSummary_FO([FromBody] BillSummaryFORequest selection)
        {
            try
            {
                var tokenS = GetToken();
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                selection.Date = objUtility.mfnReplaceForSQLInjection(selection.Date);
                selection.ExchSeg = objUtility.mfnReplaceForSQLInjection(selection.ExchSeg);
                var getData = _tradeNetRepository.BillSummaryFO(selection, loginAccess);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpPost("ClientMaster", Name = "ClientMaster")]
        public IActionResult GetClientMaster([FromBody] ClientMasterModel selection)
        {
            try
            {
                var tokenS = GetToken();
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                var getData = _tradeNetRepository.ClientMaster(selection, loginAccess);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpPost("Entry_ReceiptPayment", Name = "Entry_ReceiptPayment")]
        public IActionResult Entry_ReceiptPayment([FromBody] EntryReceiptPaymentReq req)
        {
            try
            {
                var tokenS = GetToken();
                var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                req.Type = objUtility.mfnReplaceForSQLInjection(req.Type);
                req.ExchSeg = objUtility.mfnReplaceForSQLInjection(req.ExchSeg);
                req.EntryDt = objUtility.mfnReplaceForSQLInjection(req.EntryDt);
                req.ReceivedAs = objUtility.mfnReplaceForSQLInjection(req.ReceivedAs);
                req.BankAccNo = objUtility.mfnReplaceForSQLInjection(req.BankAccNo);
                req.BankCode = objUtility.mfnReplaceForSQLInjection(req.BankCode);
                req.ClientCode = objUtility.mfnReplaceForSQLInjection(req.ClientCode);
                req.VoucherNo = objUtility.mfnReplaceForSQLInjection(req.VoucherNo);
                req.ChequeNo = objUtility.mfnReplaceForSQLInjection(req.ChequeNo);
                req.Particulars = objUtility.mfnReplaceForSQLInjection(req.Particulars);
                req.MICR = objUtility.mfnReplaceForSQLInjection(req.MICR);
                req.ClearDt = objUtility.mfnReplaceForSQLInjection(req.ClearDt);
                var getAccess = _tradeNetRepository.Validate_LoginAccess(req.ClientCode, loginAccess);
                if (getAccess == null || getAccess != true)
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Client is not mapped to you", returnDt, ""));
                }

                var getData = _tradeNetRepository.EntryReceiptPayment(userId, req);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, getData.Status, getData.Response, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch,Performance")]
        [HttpPost("Performance_Cash", Name = "Performance_Cash")]
        public IActionResult Performance_Cash(PerformanceRequestModel model)
        {
            try
            {
                var tokenS = GetToken();
                var compId = tokenS.Claims.First(claim => claim.Type == "companyCode").Value;
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                model.FromDate = objUtility.mfnReplaceForSQLInjection(model.FromDate);
                model.ToDate = objUtility.mfnReplaceForSQLInjection(model.ToDate);
                var getData = _tradeNetRepository.Performance_Cash(model, compId, loginAccess);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch,Performance")]
        [HttpPost("Performance_FO", Name = "Performance_FO")]
        public IActionResult Performance_FO([FromBody] PerformanceFORequestModel req)
        {
            try
            {
                var tokenS = GetToken();
                var compId = tokenS.Claims.First(claim => claim.Type == "companyCode").Value;
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                req.FromDate = objUtility.mfnReplaceForSQLInjection(req.FromDate);
                req.ToDate = objUtility.mfnReplaceForSQLInjection(req.ToDate);
                req.ExchSeg = objUtility.mfnReplaceForSQLInjection(req.ExchSeg);
                var getData = _tradeNetRepository.Performance_FO(req, compId, loginAccess);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch,Performance")]
        [HttpPost("Performance_Commex", Name = "Performance_Commex")]
        public IActionResult Performance_Commex([FromBody] PerformanceCommexRequestModel selection)
        {
            try
            {
                selection.FromDate = objUtility.mfnReplaceForSQLInjection(selection.FromDate);
                selection.ToDate = objUtility.mfnReplaceForSQLInjection(selection.ToDate);
                selection.ExchSeg = objUtility.mfnReplaceForSQLInjection(selection.ExchSeg);
                var getData = _tradeNetRepository.Performance_Commex(selection);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpPost, Route("OutstandingPosition")]
        public IActionResult OutstandingPosition(OutstandingPositionReq req)
        {
            try
            {
                var tokenS = GetToken();
                var compId = tokenS.Claims.First(claim => claim.Type == "companyCode").Value;
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                /*var getAccess = _tradeNetRepository.Validate_LoginAccess(ClientCode, loginAccess);
                if (getAccess == null || getAccess != true)
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Client is not mapped to you", returnDt, ""));
                }*/
                var clientWhere = _tradeNetRepository.GetFilterSql(req.Filter) + loginAccess;

                var getData = _tradeWebRepository.OutStandingPosition(clientWhere, objUtility.mfnReplaceForSQLInjection(req.AsOnDt), req.ExchSeg, true);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpGet, Route("OutstandingPosition_Detail")]
        public IActionResult OutstandingPosition_Detail(string ClientCode, string CESCd, string seriesId)
        {
            try
            {
                var tokenS = GetToken();
                var compId = tokenS.Claims.First(claim => claim.Type == "companyCode").Value;
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                ClientCode = objUtility.mfnReplaceForSQLInjection(ClientCode);
                CESCd = objUtility.mfnReplaceForSQLInjection(CESCd);
                seriesId = objUtility.mfnReplaceForSQLInjection(seriesId);
                var getAccess = _tradeNetRepository.Validate_LoginAccess(ClientCode, loginAccess);
                if (getAccess == null || getAccess != true)
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Client is not mapped to you", returnDt, ""));
                }

                var getData = _tradeWebRepository.OutStandingPosition_Detail(ClientCode, seriesId, CESCd);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpGet("DP_Holding", Name = "DP_Holding")]
        public IActionResult DP_Holding(string BOID, string AsOn)
        {
            try
            {
                var tokenS = GetToken();
                var compId = tokenS.Claims.First(claim => claim.Type == "companyCode").Value;
                BOID = objUtility.mfnReplaceForSQLInjection(BOID);
                AsOn = objUtility.mfnReplaceForSQLInjection(AsOn);
                var product = objUtility.GetDPProduct();
                if (product == null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "DP connection not present", returnDt, ""));
                }

                BOID = string.IsNullOrWhiteSpace(BOID) ? "" : " and cm_cd = '" + BOID + "' ";
                var getData = product == "C" ? _crossWebRepository.GetHolding(BOID, true, null, true, AsOn) : _estroWebRepository.GetHolding(BOID, true);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpGet("DP_Ledger", Name = "DP_Ledger")]
        public IActionResult DP_Ledger(string BOID, string fromDate, string toDate)
        {
            try
            {
                var tokenS = GetToken();
                fromDate = objUtility.mfnReplaceForSQLInjection(fromDate);
                toDate = objUtility.mfnReplaceForSQLInjection(toDate);
                BOID = objUtility.mfnReplaceForSQLInjection(BOID);
                var product = objUtility.GetDPProduct();
                if (product == null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "DP connection not present", returnDt, ""));
                }

                BOID = " and cm_cd = '" + BOID + "' ";
                var getData = product == "C" ? _crossWebRepository.GetLedger(BOID, fromDate, toDate, true) : _estroWebRepository.GetLedger(BOID, fromDate, toDate, true);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpGet("DP_Transaction", Name = "DP_Transaction")]
        public IActionResult DP_Transaction(string BOID, string fromDate, string toDate)
        {
            try
            {
                var tokenS = GetToken();
                fromDate = objUtility.mfnReplaceForSQLInjection(fromDate);
                toDate = objUtility.mfnReplaceForSQLInjection(toDate);
                BOID = objUtility.mfnReplaceForSQLInjection(BOID);
                var product = objUtility.GetDPProduct();
                if (product == null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "DP connection not present", returnDt, ""));
                }

                BOID = " and cm_cd = '" + BOID + "' ";
                var getData = product == "C" ? _crossWebRepository.GetTransaction(BOID, fromDate, toDate, true) : _estroWebRepository.GetTransaction(BOID, fromDate, toDate, true);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpGet("DP_Bill", Name = "DP_Bill")]
        public IActionResult DP_Bill(string BOID, string fromDate, string toDate)
        {
            try
            {
                var tokenS = GetToken();
                fromDate = objUtility.mfnReplaceForSQLInjection(fromDate);
                toDate = objUtility.mfnReplaceForSQLInjection(toDate);
                BOID = objUtility.mfnReplaceForSQLInjection(BOID);
                var product = objUtility.GetDPProduct();
                if (product == null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "DP connection not present", returnDt, ""));
                }

                BOID = " and cm_cd = '" + BOID + "' ";
                var getData = product == "C" ? _crossWebRepository.Bill(BOID, fromDate, toDate, true) : null;
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpPost("Entry_GSTInvoice", Name = "Entry_GSTInvoice")]
        public IActionResult Entry_GSTInvoice([FromBody] EntryGSTInvoiceRequest req)
        {
            try
            {
                var tokenS = GetToken();
                var compId = tokenS.Claims.First(claim => claim.Type == "companyCode").Value;
                var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                req.ExchSeg = objUtility.mfnReplaceForSQLInjection(req.ExchSeg);
                req.Date = objUtility.mfnReplaceForSQLInjection(req.Date);
                var getData = _tradeNetRepository.EntryGSTInvoice(userId, compId, req);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, getData.Status, getData.Data, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpPost("OutStanding_Ageing", Name = "OutStanding_Ageing")]
        public IActionResult OutStanding_Ageing([FromBody] OutstandingAgeingRequestModel req)
        {
            try
            {
                var tokenS = GetToken();
                var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                req.AsOnDate = objUtility.mfnReplaceForSQLInjection(req.AsOnDate);
                req.PeriodType = objUtility.mfnReplaceForSQLInjection(req.PeriodType);
                var getData = _tradeNetRepository.Outstanding_Ageing(req, loginAccess);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpPost("CommisionReport", Name = "CommisionReport")]
        public IActionResult CommisionReport(CommissionReportRequestModel model)
        {
            try
            {
                var tokenS = GetToken();
                var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;

                var getData = _tradeNetRepository.CommisionReport(objUtility.mfnReplaceForSQLInjection(model.FromDate), objUtility.mfnReplaceForSQLInjection(model.ToDate), model.Filter, objUtility.mfnReplaceForSQLInjection(model.ReportType), loginAccess);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpPost, Route("Transaction_Detail")]
        public IActionResult Transaction_Detail([FromBody] TransactionDetailModel req)
        {
            try
            {
                var tokenS = GetToken();
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                req.FromDate = objUtility.mfnReplaceForSQLInjection(req.FromDate);
                req.ToDate = objUtility.mfnReplaceForSQLInjection(req.ToDate);
                req.Exch = objUtility.mfnReplaceForSQLInjection(req.Exch);
                req.Seg = objUtility.mfnReplaceForSQLInjection(req.Seg);
                req.ScripCode = objUtility.mfnReplaceForSQLInjection(req.ScripCode);

                var getData = _tradeNetRepository.Transaction_Detail(req, loginAccess);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpPost("BrokerageScheme_Change", Name = "BrokerageScheme_Change")]
        public IActionResult BrokerageScheme_Change([FromBody] List<BrokerageSchemeChange> param)
        {
            try
            {
                var tokenS = GetToken();
                var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                var getData = _tradeNetRepository.BrokerageSchemChange(param, userId);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpGet, Route("Holding_Broker_Current")]
        public IActionResult Holding_Broker_Current()
        {
            try
            {
                var tokenS = GetToken();
                var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;

                var getData = _tradeWebRepository.Holding_Broker_Current(loginAccess);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpPost, Route("ProfitLoss_Cash_Summary")]
        public IActionResult ProfitLoss_Cash_Summary(ProfitLossCashSummaryRequest req)
        {
            try
            {
                var tokenS = GetToken();
                var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;

                var clientWhere = _tradeNetRepository.GetFilterSql(req.Filter) + loginAccess;
                var getData = _tradeWebRepository.ProfitLoss_CashSummary(clientWhere, objUtility.mfnReplaceForSQLInjection(req.FromDate), objUtility.mfnReplaceForSQLInjection(req.ToDate), req.StockValuation);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpPost, Route("ProfitLoss_Cash_Detail")]
        public IActionResult ProfitLoss_Cash_Detail(ProfitLossCashDetailRequest req)
        {
            try
            {
                var tokenS = GetToken();
                var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;

                var clientWhere = _tradeNetRepository.GetFilterSql(req.Filter) + loginAccess;
                var getData = _tradeWebRepository.ProfitLoss_CashDetail(clientWhere, objUtility.mfnReplaceForSQLInjection(req.FromDate), objUtility.mfnReplaceForSQLInjection(req.ToDate), objUtility.mfnReplaceForSQLInjection(req.scripCode));
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpPost, Route("ProfitLoss_FO_Summary")]
        public IActionResult ProfitLoss_FO_Summary(ProfitLossFOSummaryRequest req)
        {
            try
            {
                var tokenS = GetToken();
                var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;

                var clientWhere = _tradeNetRepository.GetFilterSql(req.Filter) + loginAccess;
                var getData = _tradeWebRepository.ProfitLoss_FO_Summary(clientWhere, objUtility.mfnReplaceForSQLInjection(req.Exchange), objUtility.mfnReplaceForSQLInjection(req.Segment), objUtility.mfnReplaceForSQLInjection(req.FromDate), objUtility.mfnReplaceForSQLInjection(req.ToDate), req.IncludeBfOptions, req.BfOptionPL);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpPost, Route("ProfitLoss_Commodity_Summary")]
        public IActionResult ProfitLoss_Commodity_Summary(ProfitLossCommoditySummaryRequest req)
        {
            try
            {
                var tokenS = GetToken();
                var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;

                var clientWhere = _tradeNetRepository.GetFilterSql(req.Filter) + loginAccess;
                var getData = _tradeWebRepository.ProfitLoss_Commodity_Summary(clientWhere, objUtility.mfnReplaceForSQLInjection(req.Exchange), objUtility.mfnReplaceForSQLInjection(req.FromDate), objUtility.mfnReplaceForSQLInjection(req.ToDate));
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpPost, Route("CapitalGainLoss_Dividend")]
        public IActionResult CapitalGainLoss_Dividend(GainLossDividendModel req)
        {
            try
            {
                var tokenS = GetToken();
                var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;

                var clientWhere = _tradeNetRepository.GetFilterSql(req.Filter) + loginAccess;
                var getData = _tradeWebRepository.CapitalGainLoss_Dividend_Process(clientWhere, objUtility.mfnReplaceForSQLInjection(req.FromDate), objUtility.mfnReplaceForSQLInjection(req.ToDate));
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpPost, Route("CapitalGainLoss_ActualPLSummary")]
        public IActionResult CapitalGainLoss_ActualPLSummary(GainLossActualPLSummaryModel req)
        {
            try
            {
                var tokenS = GetToken();
                var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;

                var clientWhere = _tradeNetRepository.GetFilterSql(req.Filter) + loginAccess;
                var getData = _tradeWebRepository.CapitalGainLoss_ActualPLSummary_Process(clientWhere, objUtility.mfnReplaceForSQLInjection(req.FromDate), objUtility.mfnReplaceForSQLInjection(req.ToDate), req.Ignore112A, false);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpPost, Route("CapitalGainLoss_ActualPLDetail")]
        public IActionResult CapitalGainLoss_ActualPLDetail(GainLossActualPLDetailModel req)
        {
            try
            {
                var tokenS = GetToken();
                var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;

                var clientWhere = _tradeNetRepository.GetFilterSql(req.Filter) + loginAccess;
                var getData = _tradeWebRepository.CapitalGainLoss_ActualPLDetail_Process(clientWhere, objUtility.mfnReplaceForSQLInjection(req.FromDate), objUtility.mfnReplaceForSQLInjection(req.ToDate), req.Ignore112A, objUtility.mfnReplaceForSQLInjection(req.ScripCode));
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpPost, Route("CapitalGainLoss_TradeListingSummary")]
        public IActionResult CapitalGainLoss_TradeListingSummary(GainLossTradeListingSummary req)
        {
            try
            {
                var tokenS = GetToken();
                var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;

                var clientWhere = _tradeNetRepository.GetFilterSql(req.Filter) + loginAccess;
                var getData = _tradeWebRepository.CapitalGainLoss_TradeListingSummary_Process(clientWhere, objUtility.mfnReplaceForSQLInjection(req.FromDate), objUtility.mfnReplaceForSQLInjection(req.ToDate));
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpPost, Route("CapitalGainLoss_TradeListingDetail")]
        public IActionResult CapitalGainLoss_TradeListingDetail(GainLossTradeListingDetailModel req)
        {
            try
            {
                var tokenS = GetToken();
                var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;

                var clientWhere = _tradeNetRepository.GetFilterSql(req.Filter) + loginAccess;
                var getData = _tradeWebRepository.CapitalGainLoss_TradeListingDetail_Process(clientWhere, objUtility.mfnReplaceForSQLInjection(req.FromDate), objUtility.mfnReplaceForSQLInjection(req.ToDate), objUtility.mfnReplaceForSQLInjection(req.ScripCode));
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpPost, Route("CapitalGainLoss_NationalSummary")]
        public IActionResult CapitalGainLoss_NationalSummary(GainLossNationalSummaryModel req)
        {
            try
            {
                var tokenS = GetToken();
                var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;

                var clientWhere = _tradeNetRepository.GetFilterSql(req.Filter) + loginAccess;
                var getData = _tradeWebRepository.CapitalGainLoss_NationalSummary_Process(clientWhere, objUtility.mfnReplaceForSQLInjection(req.strDate), req.Ignore112A);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpPost, Route("CapitalGainLoss_NationalDetail")]
        public IActionResult CapitalGainLoss_NationalDetail(GainLossNationalDetailModel req)
        {
            try
            {
                var tokenS = GetToken();
                var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;

                var clientWhere = _tradeNetRepository.GetFilterSql(req.Filter) + loginAccess;
                var getData = _tradeWebRepository.CapitalGainLoss_NationalDetail_Process(clientWhere, objUtility.mfnReplaceForSQLInjection(req.strDate), req.Ignore112A, objUtility.mfnReplaceForSQLInjection(req.ScripCode));
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpPost, Route("DeliveryStatement")]
        public IActionResult DeliveryStatement(DeliveryStatementReq req)
        {
            try
            {
                var tokenS = GetToken();
                var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                var compCode = tokenS.Claims.First(claim => claim.Type == "companyCode").Value;
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                req.Security = objUtility.mfnReplaceForSQLInjection(req.Security);
                req.TradeDate = objUtility.mfnReplaceForSQLInjection(req.TradeDate);
                req.Settlement = objUtility.mfnReplaceForSQLInjection(req.Settlement);
                //var clientWhere = _tradeNetRepository.GetFilterSql(req.Filter) + loginAccess;
                var getData = _tradeNetRepository.DeliveryStatement(req, compCode, loginAccess);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpPost, Route("CompanyPerformanceReport")]
        public IActionResult CompanyPerformanceReport(CompanyPerformanceRequest req)
        {
            try
            {
                var tokenS = GetToken();
                var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                req.FromDate = objUtility.mfnReplaceForSQLInjection(req.FromDate);
                req.ToDate = objUtility.mfnReplaceForSQLInjection(req.ToDate);
                var getData = _tradeNetRepository.CompanyPerformance(req);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Branch")]
        [HttpGet, Route("GetMasters")]
        public IActionResult GetMasters(string Type)
        {
            try
            {
                Type = objUtility.mfnReplaceForSQLInjection(Type);
                var getData = _tradeNetRepository.GetMasters(Type);
                if (getData != null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                }
                else
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
            }
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
