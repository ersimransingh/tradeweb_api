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

namespace TradeWeb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EstroNetController : Controller
    {
        #region Class level declarations.
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly UtilityCommon objUtility;
        private readonly IEstroNetRepository _estroNetRepository;
        private readonly IEstroWebRepository _estroWebRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IWebHostEnvironment _environment;

        private string strsql = "";
        ConvertData returnJson = new ConvertData();
        DataTable returnDt = new DataTable();

        #endregion

        #region Constructor
        public EstroNetController(UserManager<AppUser> userManager, IConfiguration configuration, UtilityCommon objUtility, IHttpContextAccessor httpContextAccessor, SignInManager<AppUser> signInManager, IEstroNetRepository estroNetRepository, IEstroWebRepository estroWebRepository, IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _configuration = configuration;
            this.objUtility = objUtility;
            _httpContextAccessor = httpContextAccessor;
            _signInManager = signInManager;
            _estroNetRepository = estroNetRepository;
            _estroWebRepository = estroWebRepository;
            _environment = environment;
        }
        #endregion

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "EstroNet")]
        [HttpPost, Route("EstroUserProfile")]
        public IActionResult EstroUserProfile([FromBody] Filter filter)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    var strClientWhere = loginAccess + _estroNetRepository.GetFilterSql(filter);

                    var getData = _estroWebRepository.GetUserDetails(strClientWhere);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "EstroNet")]
        [HttpPost, Route("Holding")]
        public IActionResult Holding([FromBody] Filter filter)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    var strClientWhere = loginAccess + _estroNetRepository.GetFilterSql(filter);

                    var getData = _estroWebRepository.GetHolding(strClientWhere);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "EstroNet")]
        [HttpPost, Route("Ledger")]
        public IActionResult Ledger(CrossLedgerRequest req)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    var strClientWhere = loginAccess + _estroNetRepository.GetFilterSql(req.Filter);
                    req.FromDate = objUtility.mfnReplaceForSQLInjection(req.FromDate);
                    req.ToDate = objUtility.mfnReplaceForSQLInjection(req.ToDate);
                    var getData = _estroWebRepository.GetLedger(strClientWhere, req.FromDate, req.ToDate);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "EstroNet")]
        [HttpPost, Route("Transaction")]
        public IActionResult Transaction(CrossNetTransactionRequest req)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    var strClientWhere = loginAccess + _estroNetRepository.GetFilterSql(req.Filter);
                    req.FromDate = objUtility.mfnReplaceForSQLInjection(req.FromDate);
                    req.ToDate = objUtility.mfnReplaceForSQLInjection(req.ToDate);
                    var getData = _estroWebRepository.GetTransaction(strClientWhere, req.FromDate, req.ToDate);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "EstroNet")]
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
                    var getData = _estroWebRepository.Security_Listing(SearchBy, SearchText, Alphabet, blnActive);
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


