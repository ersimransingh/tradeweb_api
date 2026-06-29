using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using TradeWeb.API.Data;
using TradeWeb.API.Repository;
using TradeWeb.Service;

namespace TradeWeb.API.Controllers
{
    /*public class ValidateUser : ActionFilterAttribute
    {
        private readonly TokenStore _tokenStore;

        public ValidateUser(TokenStore tokenService)
        {
            _tokenStore = tokenService;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var token = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            var userInfo = GetUserInfoFromToken(token);
            var userId = userInfo?.Username;
            var role = userInfo?.Role;

            if (!string.IsNullOrEmpty(userId) && !_tokenStore.IsTokenValid(userId, token) && role != "TradeMobileFP")
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.HttpContext.Response.WriteAsync("Invalid Token: Concurrent login detected.");
                return;
            }

            await next(); // Continue request execution if the token is valid
        }

        public class JwtUserInfo
        {
            public string Username { get; set; }
            public string Role { get; set; }
        }

        private JwtUserInfo GetUserInfoFromToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(token) as JwtSecurityToken;

            return new JwtUserInfo
            {
                Username = jsonToken?.Claims.FirstOrDefault(c => c.Type == "username")?.Value,
                Role = jsonToken.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value
            };
        }
    }*/
    public class ValidateUser : ActionFilterAttribute
    {
        private readonly string _expectedRole;

        public ValidateUser(string expectedRole)
        {
            _expectedRole = expectedRole;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var token = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            var userInfo = GetUserInfoFromToken(token);
            var userId = userInfo?.Username;
            var role = userInfo?.Role;

            if (_expectedRole == "TradeWeb" && role != "EstroWeb" && role != "Admin" && role != "Branch")
            {
                await next(); // Skip validation and continue
                return;
            }

            // Resolve TokenStore from the DI container manually
            var tokenStore = context.HttpContext.RequestServices.GetRequiredService<TokenStore>();

            if (_expectedRole == "TradeMobile")
            {
                if (!string.IsNullOrEmpty(userId) && !tokenStore.IsTokenValid(userId, token) && role != "TradeMobileFP")
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.HttpContext.Response.WriteAsync("Invalid Token: Concurrent login detected.");
                    return;
                }
            }
            else if (_expectedRole == "TradeWeb")
            {
                if (!string.IsNullOrEmpty(userId) && !tokenStore.IsTokenValid(userId, token))
                {
                    context.Result = new UnauthorizedObjectResult("Concurrent login detected or session expired.");
                    return;
                }
            }

            await next(); // Proceed to the next middleware
        }

        private JwtUserInfo GetUserInfoFromToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(token) as JwtSecurityToken;

            return new JwtUserInfo
            {
                Username = jsonToken?.Claims.FirstOrDefault(c => c.Type == "username")?.Value,
                Role = jsonToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value
            };
        }

        public class JwtUserInfo
        {
            public string Username { get; set; }
            public string Role { get; set; }
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class TradeMobileController : Controller
    {
        #region Class level declarations.
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly UtilityCommon objUtility;
        private readonly ITradeMobileRepository _tradeMobileRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IWebHostEnvironment _environment;

        private string strsql = "";
        ConvertData returnJson = new ConvertData();
        DataTable returnDt = new DataTable();

        #endregion

        #region Constructor
        public TradeMobileController(UserManager<AppUser> userManager, IConfiguration configuration, UtilityCommon objUtility, IHttpContextAccessor httpContextAccessor, SignInManager<AppUser> signInManager, ITradeMobileRepository tradeMobileRepository, IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _configuration = configuration;
            this.objUtility = objUtility;
            _httpContextAccessor = httpContextAccessor;
            _signInManager = signInManager;
            _tradeMobileRepository = tradeMobileRepository;
            _environment = environment;
        }
        #endregion

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "TradeMobile")]
        [ValidateUser("TradeMobile")]
        [HttpGet("GetUserProfile", Name = "GetUserProfile")]
        public IActionResult GetUserProfile()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;

                    var getData = _tradeMobileRepository.GetUserProfile(clientcd);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "TradeMobile")]
        [ValidateUser("TradeMobile")]
        [HttpGet("GetLedgerYear", Name = "GetLedgerYear")]
        public IActionResult GetLedgerYear()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;

                    var getData = _tradeMobileRepository.GetLedgerYear(clientcd);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "TradeMobile")]
        [ValidateUser("TradeMobile")]
        [HttpGet("GetLedgerBalance", Name = "GetLedgerBalance")]
        public IActionResult GetLedgerBalance(string strYear)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    strYear = objUtility.mfnReplaceForSQLInjection(strYear);
                    var getData = _tradeMobileRepository.GetLedgerBalance(clientcd, strYear);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "TradeMobile")]
        [ValidateUser("TradeMobile")]
        [HttpGet("GetLedgerDetailsM", Name = "GetLedgerDetailsM")]
        public IActionResult GetLedgerDetailsM(string DpId, string FromDt, string ToDt)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    FromDt = objUtility.mfnReplaceForSQLInjection(FromDt);
                    ToDt = objUtility.mfnReplaceForSQLInjection(ToDt);
                    DpId = objUtility.mfnReplaceForSQLInjection(DpId);
                    var getData = _tradeMobileRepository.GetLedgerDetailsM(clientcd, DpId, FromDt, ToDt);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "TradeMobile")]
        //[ServiceFilter(typeof(ValidateUser))]
        [ValidateUser("TradeMobile")]
        [HttpGet("GetTrxDates", Name = "GetTrxDates")]
        public IActionResult GetTrxDates(string Seg)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    Seg = objUtility.mfnReplaceForSQLInjection(Seg);
                    var getData = _tradeMobileRepository.GetTrxDates(clientcd, Seg);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "TradeMobile")]
        [ValidateUser("TradeMobile")]
        [HttpGet("GetTradesForDate", Name = "GetTradesForDate")]
        public IActionResult GetTradesForDate(string StartDt, string Seg)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    StartDt = objUtility.mfnReplaceForSQLInjection(StartDt);
                    Seg = objUtility.mfnReplaceForSQLInjection(Seg);
                    var getData = _tradeMobileRepository.GetTradesForDate(clientcd, StartDt, Seg);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "TradeMobile")]
        //[ServiceFilter(typeof(ValidateUser))]
        [ValidateUser("TradeMobile")]
        [HttpGet("GetTrxItems", Name = "GetTrxItems")]
        public IActionResult GetTrxItems(string Seg)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    Seg = objUtility.mfnReplaceForSQLInjection(Seg);
                    var getData = _tradeMobileRepository.GetTrxItems(clientcd, Seg);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "TradeMobile")]
        [ValidateUser("TradeMobile")]
        [HttpGet("GetTrxItemsDetail", Name = "GetTrxItemsDetail")]
        public IActionResult GetTrxItemsDetail(string Seg, string ScripCd)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    Seg = objUtility.mfnReplaceForSQLInjection(Seg);
                    ScripCd = objUtility.mfnReplaceForSQLInjection(ScripCd);
                    var getData = _tradeMobileRepository.GetTrxItemsDetail(clientcd, Seg, ScripCd);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "TradeMobile")]
        //[ServiceFilter(typeof(ValidateUser))]
        [ValidateUser("TradeMobile")]
        [HttpGet("GetHoldingSummary", Name = "GetHoldingSummary")]
        public IActionResult GetHoldingSummary()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;

                    var getData = _tradeMobileRepository.GetHoldingSummary(clientcd);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "TradeMobile")]
        [ValidateUser("TradeMobile")]
        [HttpGet("GetDPHolding", Name = "GetDPHolding")]
        public IActionResult GetDPHolding(string DematActNo)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    DematActNo = objUtility.mfnReplaceForSQLInjection(DematActNo);
                    var getData = _tradeMobileRepository.GetDPHolding(clientcd, DematActNo);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "TradeMobile")]
        //[ServiceFilter(typeof(ValidateUser))]
        [ValidateUser("TradeMobile")]
        [HttpGet("GetBenHolding", Name = "GetBenHolding")]
        public IActionResult GetBenHolding()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;

                    var getData = _tradeMobileRepository.GetBenHolding(clientcd);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "TradeMobile")]
        [ValidateUser("TradeMobile")]
        [HttpGet("GetCollateral", Name = "GetCollateral")]
        public IActionResult GetCollateral()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;

                    var getData = _tradeMobileRepository.GetCollateral(clientcd);
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


        [Authorize(AuthenticationSchemes = "Bearer", Roles = "TradeMobile")]
        [ValidateUser("TradeMobile")]
        [HttpGet("GetMarginShortFall", Name = "GetMarginShortFall")]
        public IActionResult GetMarginShortFall()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;

                    var getData = _tradeMobileRepository.GetMarginShortFall(clientcd);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "TradeMobile")]
        [ValidateUser("TradeMobile")]
        [HttpGet("GetBillsYear", Name = "GetBillsYear")]
        public IActionResult GetBillsYear(string Seg)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    Seg = objUtility.mfnReplaceForSQLInjection(Seg);
                    var getData = _tradeMobileRepository.GetBillsYear(clientcd, Seg);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "TradeMobile")]
        [ValidateUser("TradeMobile")]
        [HttpGet("GetBillsN", Name = "GetBillsN")]
        public IActionResult GetBillsN(string Seg, string Year)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    Seg = objUtility.mfnReplaceForSQLInjection(Seg);
                    Year = objUtility.mfnReplaceForSQLInjection(Year);
                    var getData = _tradeMobileRepository.GetBillsN(clientcd, Seg, Year);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "TradeMobile")]
        [ValidateUser("TradeMobile")]
        [HttpGet("GetBillDetail", Name = "GetBillDetail")]
        public IActionResult GetBillDetail(string Date, string Exch, string Seg)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var compCd = tokenS.Claims.First(claim => claim.Type == "companyCode").Value;
                    Date = objUtility.mfnReplaceForSQLInjection(Date);
                    Exch = objUtility.mfnReplaceForSQLInjection(Exch);
                    Seg = objUtility.mfnReplaceForSQLInjection(Seg);
                    var getData = _tradeMobileRepository.GetBillDetail(clientcd, Date, Exch, Seg, compCd);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "TradeMobile")]
        [ValidateUser("TradeMobile")]
        [HttpGet("GetOutstandingSummary", Name = "GetOutstandingSummary")]
        public IActionResult GetOutstandingSummary()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;

                    var getData = _tradeMobileRepository.GetOutstandingSummary(clientcd);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "TradeMobile")]
        [ValidateUser("TradeMobile")]
        [HttpGet("GetOutstandingDetail", Name = "GetOutstandingDetail")]
        public IActionResult GetOutstandingDetail(string FutureOption, string ExchSeg)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    FutureOption = objUtility.mfnReplaceForSQLInjection(FutureOption);
                    ExchSeg = objUtility.mfnReplaceForSQLInjection(ExchSeg);
                    var getData = _tradeMobileRepository.GetOutstandingDetail(clientcd, FutureOption, ExchSeg);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "TradeMobile")]
        [ValidateUser("TradeMobile")]
        [HttpGet("GetRMSPayoutAmount", Name = "GetRMSPayoutAmount")]
        public IActionResult GetRMSPayoutAmount(string Type)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    Type = objUtility.mfnReplaceForSQLInjection(Type);
                    var getData = _tradeMobileRepository.GetRMSPayoutAmount(clientcd, Type);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "TradeMobile")]
        [ValidateUser("TradeMobile")]
        [HttpGet("GetInvestorPLCash", Name = "GetInvestorPLCash")]
        public IActionResult GetInvestorPLCash(string FromDt, string ToDt, string ScripCd, string ReportType, string StockValuation)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    FromDt = objUtility.mfnReplaceForSQLInjection(FromDt);
                    ToDt = objUtility.mfnReplaceForSQLInjection(ToDt);
                    ScripCd = objUtility.mfnReplaceForSQLInjection(ScripCd);
                    ReportType = objUtility.mfnReplaceForSQLInjection(ReportType);
                    StockValuation = objUtility.mfnReplaceForSQLInjection(StockValuation);
                    var getData = _tradeMobileRepository.GetInvestorPLCash(clientcd, FromDt, ToDt, ScripCd, ReportType, StockValuation);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "TradeMobile")]
        [ValidateUser("TradeMobile")]
        [HttpGet("GetParameter", Name = "GetParameter")]
        public IActionResult GetParameter(string strParmcd)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    strParmcd = objUtility.mfnReplaceForSQLInjection(strParmcd);
                    var getData = _tradeMobileRepository.GetParameter(strParmcd);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "TradeMobile")]
        [ValidateUser("TradeMobile")]
        [HttpGet("GetReportParm", Name = "GetReportParm")]
        public IActionResult GetReportParm()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;

                    var getData = _tradeMobileRepository.GetReportParm(clientcd);
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

        [HttpGet("SMSSetting", Name = "SMSSetting")]
        public IActionResult SMSSetting()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    /*var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;*/

                    var getData = _tradeMobileRepository.SMSSetting();
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

        [HttpGet("ForgetPasswordSMS", Name = "ForgetPasswordSMS")]
        public IActionResult ForgetPasswordSMS(string clientCode)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    clientCode = objUtility.mfnReplaceForSQLInjection(clientCode);
                    var getData = _tradeMobileRepository.ForgetPasswordSMS(clientCode);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "TradeMobile")]
        [ValidateUser("TradeMobile")]
        [HttpGet("ChangePassword", Name = "ChangePassword")]
        public IActionResult ChangePassword(string OldPwd, string NewPwd)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    OldPwd = objUtility.mfnReplaceForSQLInjection(OldPwd);
                    NewPwd = objUtility.mfnReplaceForSQLInjection(NewPwd);
                    var getData = _tradeMobileRepository.ChangePassword(clientcd, OldPwd, NewPwd);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "TradeMobile")]
        [ValidateUser("TradeMobile")]
        [HttpPost("PostFundRequest", Name = "PostFundRequest")]
        public IActionResult PostFundRequest(string Flag, string Type, string Value)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    Flag = objUtility.mfnReplaceForSQLInjection(Flag);
                    Type = objUtility.mfnReplaceForSQLInjection(Type);
                    Value = objUtility.mfnReplaceForSQLInjection(Value);
                    var getData = _tradeMobileRepository.PostFundRequest(clientcd, Flag, Type, Value);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "TradeMobile")]
        [ValidateUser("TradeMobile")]
        [HttpPost("PostShareRequest", Name = "PostShareRequest")]
        public IActionResult PostShareRequest(string Flag, string ScripCd, string Qty)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    Flag = objUtility.mfnReplaceForSQLInjection(Flag);
                    ScripCd = objUtility.mfnReplaceForSQLInjection(ScripCd);
                    Qty = objUtility.mfnReplaceForSQLInjection(Qty);
                    var getData = _tradeMobileRepository.PostShareRequest(clientcd, Flag, ScripCd, Qty);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "TradeMobile")]
        [ValidateUser("TradeMobile")]
        [HttpPost("PostRequestForreport", Name = "PostRequestForreport")]
        public IActionResult PostRequestForreport(string Report, string FromDt, string ToDt, string LastSeg)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    Report = objUtility.mfnReplaceForSQLInjection(Report);
                    FromDt = objUtility.mfnReplaceForSQLInjection(FromDt);
                    ToDt = objUtility.mfnReplaceForSQLInjection(ToDt);
                    LastSeg = objUtility.mfnReplaceForSQLInjection(LastSeg);
                    var getData = _tradeMobileRepository.PostRequestForreport(clientcd, Report, FromDt, ToDt, LastSeg);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "TradeMobileFP")]
        [ValidateUser("TradeMobile")]
        [HttpPost("ForgotPasswordVerifyOTP", Name = "ForgotPasswordVerifyOTP")]
        public IActionResult ForgotPasswordVerifyOTP(string OTP, string NewPassword, string ConfirmPassword)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    OTP = objUtility.mfnReplaceForSQLInjection(OTP);
                    NewPassword = objUtility.mfnReplaceForSQLInjection(NewPassword);
                    ConfirmPassword = objUtility.mfnReplaceForSQLInjection(ConfirmPassword);
                    var getData = _tradeMobileRepository.ForgotPasswordVerifyOTP(clientcd, OTP, NewPassword, ConfirmPassword);
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
