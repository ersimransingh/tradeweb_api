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


namespace TradeWeb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CrossNetReportController : Controller
    {
        #region Class level declarations.
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly UtilityCommon objUtility;
        private readonly ICrossNetRepository _crossNetRepository;
        private readonly ICrossWebRepository _crossWebRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IWebHostEnvironment _environment;

        private string strsql = "";
        ConvertData returnJson = new ConvertData();
        DataTable returnDt = new DataTable();

        #endregion

        #region Constructor
        public CrossNetReportController(UserManager<AppUser> userManager, IConfiguration configuration, UtilityCommon objUtility, IHttpContextAccessor httpContextAccessor, SignInManager<AppUser> signInManager, ICrossNetRepository crossNetRepository, ICrossWebRepository crossWebRepository, IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _configuration = configuration;
            this.objUtility = objUtility;
            _httpContextAccessor = httpContextAccessor;
            _signInManager = signInManager;
            _crossNetRepository = crossNetRepository;
            _crossWebRepository = crossWebRepository;
            _environment = environment;
        }
        #endregion


        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpPost, Route("BillSummary")]
        public IActionResult BillSummary(CrossBillSummaryReq req)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    req.BillDate = objUtility.mfnReplaceForSQLInjection(req.BillDate);
                    req.BillType = objUtility.mfnReplaceForSQLInjection(req.BillType);
                    var getData = _crossNetRepository.BillSummary(req, loginAccess);
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
        [HttpPost, Route("ClientListing")]
        public IActionResult ClientListing(ClientListingReq Req)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    Req.Status = objUtility.mfnReplaceForSQLInjection(Req.Status);
                    var getData = _crossNetRepository.ClientListing(Req, loginAccess);
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
        [HttpPost, Route("Transaction_Status")]
        public IActionResult Transaction_Status(TransactionStatusReq req)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    var getData = _crossNetRepository.TransactionStatus(req, loginAccess);
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
        [HttpPost, Route("BillBreakUp")]
        public IActionResult BillBreakUp(BillBreakUpReq req)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    var getData = _crossNetRepository.BillBreakUp(req, loginAccess);
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
        [HttpPost, Route("ClientOutstanding")]
        public IActionResult ClientOutstanding(ClientOutstandingReq req)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    var getData = _crossNetRepository.ClientOutstanding(req, loginAccess);
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
        [HttpPost, Route("Ledger")]
        public IActionResult Ledger(CrossLedgerReq req)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    //var getData = _crossNetRepository.Ledger(req, loginAccess);
                    var clientWhere = loginAccess + _crossNetRepository.GetFilterSql(req.Filter);
                    var getData = _crossWebRepository.GetLedger(clientWhere, req.FromDate, req.ToDate, false);
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
        [HttpPost, Route("Holding")]
        public IActionResult Holding(HoldingRequest req)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    //var getData = _crossNetRepository.HoldingFind(req, loginAccess);
                    var strClientWhere = loginAccess + _crossNetRepository.GetFilterSql(req.Filter);
                    var getData = _crossWebRepository.GetHolding(strClientWhere, false, req.BalanceType, req.ShowValuation, req.AsOn);
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
        [HttpPost, Route("TransactionStatement")]
        public IActionResult TransactionStatement(TransectionStatementRequest req)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    //var getData = _crossNetRepository.TransactionStatementFind(req, loginAccess);
                    var strClientWhere = loginAccess + _crossNetRepository.GetFilterSql(req.Filter);
                    var getData = _crossWebRepository.GetTransaction(strClientWhere, req.FromDate, req.ToDate, false, req.ISIN, req.TransactionType);
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
        [HttpPost, Route("PerformanceReport")]
        public IActionResult PerformanceReport(PerformanceRepRequest req)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    var getData = _crossNetRepository.PerformanceReport(req, loginAccess);
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
