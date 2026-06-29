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
    public class SubscriptionController : Controller
    {
        #region Class level declarations.
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly UtilityCommon objUtility;
        private readonly ISubscriptionRepository _SubscriptionRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IWebHostEnvironment _environment;
        private string strsql = "";
        ConvertData returnJson = new ConvertData();
        DataTable returnDt = new DataTable();

        #endregion

        #region Constructor
        public SubscriptionController(UserManager<AppUser> userManager, IConfiguration configuration, UtilityCommon objUtility, IHttpContextAccessor httpContextAccessor, SignInManager<AppUser> signInManager, ISubscriptionRepository subscriptionRepository, IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _configuration = configuration;
            this.objUtility = objUtility;
            _httpContextAccessor = httpContextAccessor;
            _signInManager = signInManager;
            _SubscriptionRepository = subscriptionRepository;
            _environment = environment;
        }
        #endregion

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Subscription")]
        [HttpPost("Subscription_Add", Name = "Subscription_Add")]
        public IActionResult Subscription_Add(SubscriptionAddModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    model.ClientCode = objUtility.mfnReplaceForSQLInjection(model.ClientCode);
                    model.FromDate = objUtility.mfnReplaceForSQLInjection(model.FromDate);
                    model.ToDate = objUtility.mfnReplaceForSQLInjection(model.ToDate);
                    model.SpecialScheme = objUtility.mfnReplaceForSQLInjection(model.SpecialScheme);
                    model.NormalScheme = objUtility.mfnReplaceForSQLInjection(model.NormalScheme);
                    var getData = _SubscriptionRepository.Subscription_Add(userId, model);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Subscription")]
        [HttpPost("Subscription_Discontinue", Name = "Subscription_Discontinue")]
        public IActionResult Subscription_Discontinue(SubscriptionDiscontinueModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    model.SrNo = objUtility.mfnReplaceForSQLInjection(model.SrNo);
                    model.ClientCode = objUtility.mfnReplaceForSQLInjection(model.ClientCode);
                    var getData = _SubscriptionRepository.Subscription_Discontinue(model, userId);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Subscription")]
        [HttpPost("Subscription_Delete", Name = "Subscription_Delete")]
        public IActionResult Subscription_Delete(SubscriptionDiscontinueModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    model.SrNo = objUtility.mfnReplaceForSQLInjection(model.SrNo);
                    model.ClientCode = objUtility.mfnReplaceForSQLInjection(model.ClientCode);
                    var getData = _SubscriptionRepository.Subscription_Delete(model, userId);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Subscription")]
        [HttpPost("Subscription_Status", Name = "Subscription_Status")]
        public IActionResult Subscription_Status(SubscriptionStatusModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    model.AsOn = objUtility.mfnReplaceForSQLInjection(model.AsOn);
                    model.ClientCode = objUtility.mfnReplaceForSQLInjection(model.ClientCode);
                    var getData = _SubscriptionRepository.Subscription_Status(model);
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
