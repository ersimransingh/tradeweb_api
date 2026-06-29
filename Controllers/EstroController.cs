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
    public class EstroController : Controller
    {
        #region Class level declarations.
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly UtilityCommon objUtility;
        private readonly IEstroWebRepository _estroWebRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IWebHostEnvironment _environment;
        private string strsql = "";
        ConvertData returnJson = new ConvertData();
        DataTable retrunDt = new DataTable();

        #endregion

        #region Constructor
        public EstroController(UserManager<AppUser> userManager, IConfiguration configuration, UtilityCommon objUtility, IHttpContextAccessor httpContextAccessor, SignInManager<AppUser> signInManager, IEstroWebRepository estroWebRepository, IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _configuration = configuration;
            this.objUtility = objUtility;
            _httpContextAccessor = httpContextAccessor;
            _signInManager = signInManager;
            _estroWebRepository = estroWebRepository;
            _environment = environment;
        }
        #endregion

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpPost("Report", Name = "Report")]
        public IActionResult Report(EstroReportModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var getData = _estroWebRepository.GetReport(model, userId);
                    if (string.IsNullOrWhiteSpace(getData.Message))
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", getData.Message, ""));
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
        [HttpGet("EstroUserProfile", Name = "EstroUserProfile")]
        public IActionResult Home_UserProfile1()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    userId = " and cm_cd = '" + userId + "'";

                    var getData = _estroWebRepository.GetUserDetails(userId);
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
        [HttpGet, Route("Holding")]
        public IActionResult Holding()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    userId = " and cm_cd = '" + userId + "'";

                    var getData = _estroWebRepository.GetHolding(userId);
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
        [HttpGet, Route("Ledger")]
        public IActionResult Ledger(string FromDt, string ToDt)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    userId = " and cm_cd = '" + userId + "'";
                    FromDt = objUtility.mfnReplaceForSQLInjection(FromDt);
                    ToDt = objUtility.mfnReplaceForSQLInjection(ToDt);
                    var getData = _estroWebRepository.GetLedger(userId, FromDt, ToDt);
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
        [HttpGet, Route("Transaction")]
        public IActionResult Transaction(string FromDt, string ToDt)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    userId = " and cm_cd = '" + userId + "'";
                    FromDt = objUtility.mfnReplaceForSQLInjection(FromDt);
                    ToDt = objUtility.mfnReplaceForSQLInjection(ToDt);
                    var getData = _estroWebRepository.GetTransaction(userId, FromDt, ToDt);
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
        [HttpGet, Route("Security_Listing")]
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
                    var getData = _estroWebRepository.Security_Listing(SearchBy, SearchText, Alphabet, blnActive);
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

        //[Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        //[HttpGet("Bills", Name = "[Controller]/Bills")]
        //public IActionResult Bills(string FromDt, string ToDt)
        //{
        //    try
        //    {
        //        var tokenS = objUtility.GetToken();
        //        var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
        //        var getData = _estroWebRepository.GetBills(userId, FromDt, ToDt);
        //        if (getData != null)
        //        {
        //            return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
        //        }
        //        else
        //        {
        //            return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", retrunDt, ""));
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
        //    }
        //}
    }
}
