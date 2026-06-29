using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
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
    public class CrossNetController : Controller
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
        public CrossNetController(UserManager<AppUser> userManager, IConfiguration configuration, UtilityCommon objUtility, IHttpContextAccessor httpContextAccessor, SignInManager<AppUser> signInManager, ICrossNetRepository crossNetRepository, ICrossWebRepository crossWebRepository, IWebHostEnvironment environment)
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
        [HttpPost, Route("CrossUserProfile")]
        public IActionResult CrossUserProfile([FromBody] Filter filter)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    var strClientWhere = loginAccess + _crossNetRepository.GetFilterSql(filter);

                    var getData = _crossWebRepository.GetUserDetails(strClientWhere);
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
        public IActionResult Holding([FromBody] Filter filter)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    var strClientWhere = loginAccess + _crossNetRepository.GetFilterSql(filter);

                    var getData = _crossWebRepository.GetHolding(strClientWhere);
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
        public IActionResult Ledger(CrossLedgerRequest req)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    var strClientWhere = loginAccess + _crossNetRepository.GetFilterSql(req.Filter);

                    var getData = _crossWebRepository.GetLedger(strClientWhere, objUtility.mfnReplaceForSQLInjection(req.FromDate), objUtility.mfnReplaceForSQLInjection(req.ToDate));
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
        [HttpPost, Route("Transaction")]
        public IActionResult Transaction(CrossNetTransactionRequest req)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    var strClientWhere = loginAccess + _crossNetRepository.GetFilterSql(req.Filter);

                    var getData = _crossWebRepository.GetTransaction(strClientWhere, objUtility.mfnReplaceForSQLInjection(req.FromDate), objUtility.mfnReplaceForSQLInjection(req.ToDate));
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
        [HttpGet, Route("Security_Listing")]
        public IActionResult Security_Listing(string SearchBy, string SearchText, string Alphabet, bool blnActive)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    SearchBy = objUtility.mfnReplaceForSQLInjection(SearchBy);
                    SearchText = objUtility.mfnReplaceForSQLInjection(SearchText);
                    Alphabet = objUtility.mfnReplaceForSQLInjection(Alphabet);
                    var getData = _crossWebRepository.Security_Listing(SearchBy, SearchText, Alphabet, blnActive);
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
        [HttpPost, Route("Bill")]
        public IActionResult Bill(CrossNetBillRequest req)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    var strClientWhere = loginAccess + _crossNetRepository.GetFilterSql(req.Filter);

                    var getData = _crossWebRepository.Bill(strClientWhere, objUtility.mfnReplaceForSQLInjection(req.FromDate), objUtility.mfnReplaceForSQLInjection(req.ToDate));
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

    }
}
