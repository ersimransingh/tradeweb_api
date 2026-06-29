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
using TradeWeb.API.Repository;
using TradeWeb.Service;

namespace TradeWeb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CrossNetMasterController : Controller
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
        public CrossNetMasterController(UserManager<AppUser> userManager, IConfiguration configuration, UtilityCommon objUtility, IHttpContextAccessor httpContextAccessor, SignInManager<AppUser> signInManager, ICrossNetRepository crossNetRepository, IWebHostEnvironment environment)
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet,CrossModification")]
        [HttpGet("Branch", Name = "Branch")]
        public IActionResult Branch([FromQuery] string BranchCode)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    BranchCode = objUtility.mfnReplaceForSQLInjection(BranchCode);
                    var getData = _crossNetRepository.GetBranchName(BranchCode);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet,CrossModification")]
        [HttpGet("Family", Name = "Family")]
        public IActionResult Family([FromQuery] string FamilyCode)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    FamilyCode = objUtility.mfnReplaceForSQLInjection(FamilyCode);
                    var getData = _crossNetRepository.GetFamilyName(FamilyCode);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet,CrossModification")]
        [HttpGet("Group", Name = "Group")]
        public IActionResult Group([FromQuery] string GroupCode)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    GroupCode = objUtility.mfnReplaceForSQLInjection(GroupCode);
                    var getData = _crossNetRepository.GetGroupName(GroupCode);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet,CrossModification")]
        [HttpGet("Client", Name = "Client")]
        public IActionResult Client([FromQuery] string ClientCode, string BranchCd)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    ClientCode = objUtility.mfnReplaceForSQLInjection(ClientCode);
                    BranchCd = objUtility.mfnReplaceForSQLInjection(BranchCd);
                    var getData = _crossNetRepository.ClientSearch(ClientCode, BranchCd);
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

        //[Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet_Instruction")]
        //[HttpGet("BOIDName", Name = "BOIDName")]
        //public IActionResult BOIDName([FromQuery] string BOID)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            var tokenS = GetToken();
        //            var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
        //            var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
        //            var getData = _crossNetRepository.GetBOIDName(BOID);
        //            if (getData != null)
        //            {
        //                return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
        //            }
        //            else
        //            {
        //                return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", returnDt, ""));
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
        //        }
        //    }
        //    return BadRequest();
        //}

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet,CrossModification")]
        [HttpGet("DPName", Name = "DPName")]
        public IActionResult DPName([FromQuery] string ClientCd)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    ClientCd = objUtility.mfnReplaceForSQLInjection(ClientCd);
                    var getData = _crossNetRepository.GetDPName(ClientCd);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet,CrossModification")]
        [HttpGet("ISIN", Name = "ISIN")]
        public IActionResult ISIN([FromQuery] string ISINCode)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    ISINCode = objUtility.mfnReplaceForSQLInjection(ISINCode);
                    var getData = _crossNetRepository.GetISINName(ISINCode);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet,CrossModification")]
        [HttpGet("Settlement", Name = "Settlement")]
        public IActionResult Settlement([FromQuery] string ExecDate)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    ExecDate = objUtility.mfnReplaceForSQLInjection(ExecDate);
                    var getData = _crossNetRepository.FromSettNoSearch(ExecDate);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet,CrossModification")]
        [HttpGet("Search_Masters", Name = "Search_Masters")]
        public IActionResult Search_Masters([FromQuery] string Type)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    Type = objUtility.mfnReplaceForSQLInjection(Type);
                    var getData = _crossNetRepository.GetMasterCmb(Type, loginAccess);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet,CrossModification")]
        [HttpGet, Route("Holding_Dates")]
        public IActionResult Holding_Dates()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    var getData = _crossNetRepository.GetHoldingDates();
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet,CrossModification")]
        [HttpGet, Route("Transaction_Status")]
        public IActionResult Transaction_Status([FromQuery] string TransactionType)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    TransactionType = objUtility.mfnReplaceForSQLInjection(TransactionType);
                    var getData = _crossNetRepository.GetStatusCode(TransactionType);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet,CrossModification")]
        [HttpGet, Route("Transaction_Type")]
        public IActionResult Transaction_Type()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAccess = tokenS.Claims.First(claim => claim.Type == "loginaccess").Value;
                    var getData = _crossNetRepository.TransactionType();
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet,CrossModification")]
        [HttpGet, Route("ISD_Codes")]
        public IActionResult ISD_Codes()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var getData = _crossNetRepository.ISD_Codes();
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet,CrossModification")]
        [HttpGet, Route("Relationship")]
        public IActionResult Relationship()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var getData = _crossNetRepository.GetSubMaster("RELATIONSHIP");
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet,CrossModification")]
        [HttpGet, Route("AnnualIncome")]
        public IActionResult AnnualIncome()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var getData = _crossNetRepository.GetSubMaster("ANNUALINCOME");
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet,CrossModification")]
        [HttpGet, Route("BankCurrency")]
        public IActionResult BankCurrency()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var getData = _crossNetRepository.GetSubMaster("BANKCURRENCY");
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
