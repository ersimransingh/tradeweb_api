using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Linq;
using System.Net;
using System.Text.Json;
using TradeWeb.API.Data;
using TradeWeb.API.Models;
using TradeWeb.API.Repository;
using TradeWeb.Service;
//using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace TradeWeb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CrossNet_ClientController : Controller
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
        public CrossNet_ClientController(UserManager<AppUser> userManager, IConfiguration configuration, UtilityCommon objUtility, IHttpContextAccessor httpContextAccessor, SignInManager<AppUser> signInManager, ICrossNetRepository crossNetRepository, ICrossWebRepository crossWebRepository, IWebHostEnvironment environment)
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet,CrossModification")]
        [HttpGet, Route("ClientModification_Get")]
        public IActionResult ClientModification_Get(string BOID)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    BOID = objUtility.mfnReplaceForSQLInjection(BOID);
                    var getData = _crossNetRepository.GetClientModification(BOID);
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
        [HttpPost, Route("ClientModification_Post")]
        public IActionResult ClientModification_Post([FromBody] JsonElement json)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    CrossClientModification req = new CrossClientModification();

                    try
                    {
                        req = JsonConvert.DeserializeObject<CrossClientModification>(json.ToString());
                    }
                    catch (Exception)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "JSON is not valid", returnDt, ""));
                    }

                    string strMessage = _crossNetRepository.ValidateClientModification(req);
                    if (!string.IsNullOrEmpty(strMessage))
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, strMessage, returnDt, ""));
                    }

                    var getData = _crossNetRepository.ClientModification(req, userId);
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
