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
using TradeWeb.API.Repository;
using TradeWeb.Service;

namespace TradeWeb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CrossController : BaseController
    {
        #region Class level declarations.
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly UtilityCommon objUtility;
        private readonly ICrossWebRepository _crossWebRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IWebHostEnvironment _environment;
        private string strsql = "";
        ConvertData returnJson = new ConvertData();
        DataTable retrunDt = new DataTable();

        #endregion

        #region Constructor
        public CrossController(UserManager<AppUser> userManager, IConfiguration configuration, UtilityCommon objUtility, IHttpContextAccessor httpContextAccessor, SignInManager<AppUser> signInManager, ICrossWebRepository crossWebRepository, IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _configuration = configuration;
            this.objUtility = objUtility;
            _httpContextAccessor = httpContextAccessor;
            _signInManager = signInManager;
            _crossWebRepository = crossWebRepository;
            _environment = environment;
        }
        #endregion

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("CrossUserProfile", Name = "CrossUserProfile")]
        public IActionResult Home_UserProfile1()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    userId = " and cm_cd = '" + userId + "'";

                    var getData = _crossWebRepository.GetUserDetails(userId);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", retrunDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("Holding", Name = "Holding")]
        public IActionResult Holding()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    userId = " and cm_cd = '" + userId + "'";

                    var getData = _crossWebRepository.GetHolding(userId);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", retrunDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("Ledger", Name = "Ledger")]
        public IActionResult Ledger(string fromDate, string toDate)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    userId = " and cm_cd = '" + userId + "'";
                    fromDate = objUtility.mfnReplaceForSQLInjection(fromDate);
                    toDate = objUtility.mfnReplaceForSQLInjection(toDate);
                    var getData = _crossWebRepository.GetLedger(userId, fromDate, toDate);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", retrunDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("Transaction", Name = "Transaction")]
        public IActionResult Transaction(string fromDate, string toDate)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    userId = " and cm_cd = '" + userId + "'";
                    fromDate = objUtility.mfnReplaceForSQLInjection(fromDate);
                    toDate = objUtility.mfnReplaceForSQLInjection(toDate);
                    var getData = _crossWebRepository.GetTransaction(userId, fromDate, toDate);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", retrunDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("Security_Listing", Name = "Security_Listing")]
        public IActionResult Security_Listing(string SearchBy, string SearchText, string Alphabet, bool blnActive)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
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
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", retrunDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("Bill", Name = "Bill")]
        public IActionResult Bill(string fromDate, string toDate)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    userId = " and cm_cd = '" + userId + "'";
                    fromDate = objUtility.mfnReplaceForSQLInjection(fromDate);
                    toDate = objUtility.mfnReplaceForSQLInjection(toDate);
                    var getData = _crossWebRepository.Bill(userId, fromDate, toDate);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", retrunDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

    }
}
