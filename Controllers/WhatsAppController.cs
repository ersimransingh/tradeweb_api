using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using TradeWeb.API.Models;
using TradeWeb.API.Repository;
using TradeWeb.Service;
using Newtonsoft.Json;

namespace TradeWeb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WhatsAppController : Controller
    {
        #region Class level declarations.
        //private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly UtilityCommon objUtility;
        private readonly WhatsAppRepository objRepository;
        private readonly IWhatsAppRepository _whatsAppRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        //private readonly SignInManager<AppUser> _signInManager;
        private readonly IWebHostEnvironment _environment;

        private string strsql = "";
        ConvertData returnJson = new ConvertData();
        DataTable returnDt = new DataTable();

        #endregion

        #region Constructor
        public WhatsAppController(IConfiguration configuration, UtilityCommon objUtility, IHttpContextAccessor httpContextAccessor, IWhatsAppRepository whatsAppRepository, IWebHostEnvironment environment)
        {
            //_userManager = userManager;
            _configuration = configuration;
            this.objUtility = objUtility;
            _httpContextAccessor = httpContextAccessor;
            //_signInManager = signInManager;
            _whatsAppRepository = whatsAppRepository;
            _environment = environment;
        }
        #endregion

        [HttpPost("ChatBot", Name = "ChatBot")]
        public IActionResult ChatBot(WhatsAppBotRequest req)
        {
            try
            {
                if (objUtility.mfnGetSysSplFeature("BOT"))
                {
                    objUtility.CheckSP("ChatBot");
                }
                var getData = _whatsAppRepository.ChatBot(req);
                if (getData != null)
                {
                    return Ok(JsonConvert.SerializeObject(getData, Formatting.Indented));
                }
                else
                {
                    return Ok(JsonConvert.SerializeObject(getData, Formatting.Indented));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(JsonConvert.SerializeObject(ex.Message, Formatting.Indented));
            }
        }
    }
}
