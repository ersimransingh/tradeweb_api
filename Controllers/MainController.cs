using Fernet;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using MimeKit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using TradeWeb.API.Filters;
using TradeWeb.API.Models;
using TradeWeb.API.Repository;
using TradeWeb.API.Service;
using TradeWeb.Service;
using static TradeWeb.API.Startup;
using Formatting = Newtonsoft.Json.Formatting;

namespace TradeWeb.API.Controllers
{
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
    public class MainController : BaseController
    {
        #region Class level declarations.
        //private readonly Microsoft.AspNetCore.Identity.UserManager<AppUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly UtilityCommon objUtility;
        private readonly ITradeWebRepository _tradeWebRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        //private readonly SignInManager<AppUser> _signInManager;
        private readonly IWebHostEnvironment _environment;
        private readonly TokenStore _tokenStore;
        private readonly string _connStr;
        private readonly E2EKeyService _e2eKeyService;
        private readonly DigitalEmailService _svc;              // new
        private readonly Channel<DigitalEmailJob> _channel;     // new

        private string strsql = "";
        ConvertData returnJson = new ConvertData();
        DataTable returnDt = new DataTable();
        private string strAPIVersion = "2.0.0.1";
        public const string UtcDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

        #endregion

        #region Constructor
        public MainController(IConfiguration configuration, UtilityCommon objUtility, IHttpContextAccessor httpContextAccessor, ITradeWebRepository tradeWebRepository, IWebHostEnvironment environment, TokenStore tokenStore, ConnectionString connectionString, E2EKeyService e2eKeyService,
            DigitalEmailService svc, Channel<DigitalEmailJob> channel)
        {
            //_userManager = userManager;
            _configuration = configuration;
            this.objUtility = objUtility;
            _httpContextAccessor = httpContextAccessor;
            //_signInManager = signInManager;
            _tradeWebRepository = tradeWebRepository;
            _environment = environment;
            _tokenStore = tokenStore;
            _connStr = connectionString.Default;
            _e2eKeyService = e2eKeyService;
            _svc = svc;
            _channel = channel;
        }
        #endregion

        /*public class ValidateUser : ActionFilterAttribute
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
        }*/

        public class EncryptResponseAttribute : ActionFilterAttribute
        {
            private readonly byte[] _key;
            private readonly bool _blnEncrypt;
            private readonly byte[] _cryptoKey;
            private readonly E2EKeyService _e2eKeyService;

            public EncryptResponseAttribute(IConfiguration config, UtilityCommon objutility, E2EKeyService e2eKeyService)
            {
                string encKey = objutility.Decrypt(config["fernetKey"]);
                _key = Base64UrlDecode(encKey);

                string encCryptoKey = objutility.Decrypt(config["epwdKey"]);
                _cryptoKey = Encoding.UTF8.GetBytes(encCryptoKey);

                _blnEncrypt = false;
                if (objutility.fnchkTable("Sysparameter"))
                {
                    _blnEncrypt = objutility.GetSysParmSt("APIENCDATA", "") == "Y";
                }

                _e2eKeyService = e2eKeyService;
            }

            public override void OnActionExecuted(ActionExecutedContext context)
            {
                // E2E encryption takes precedence when the client sent an ECDH public key.
                if (context.HttpContext.Items.TryGetValue(E2EDecryptFilter.E2EContextKey, out var e2eObj) && e2eObj is E2EContext e2eContext)
                {
                    if (context.Result is ObjectResult objectResult)
                    {
                        string plainJson = objectResult.Value is string s
                            ? s
                            : JsonConvert.SerializeObject(objectResult.Value, Formatting.None);

                        string encryptedToken = E2EEncryptionHelper.Encrypt(e2eContext.AesKey, plainJson);

                        context.HttpContext.Response.Headers[E2EEncryptionHelper.PublicKeyHeader] = Convert.ToBase64String(_e2eKeyService.PublicKey);

                        context.Result = new JsonResult(new { data = encryptedToken })
                        {
                            StatusCode = objectResult.StatusCode ?? StatusCodes.Status200OK
                        };
                    }
                    return;
                }

                var blnSkip = context.HttpContext.Items["Option"]?.ToString() == "InitializeLogin";
                var blnMobile = context.HttpContext.Items["TradeMobile"]?.ToString() == "Y";
                if (blnSkip)
                {
                    base.OnActionExecuted(context);
                    return;
                }

                if (_blnEncrypt && context.Result is ObjectResult objectResultLegacy)
                {
                    string plainJson;
                    if (objectResultLegacy.Value is string s)
                    {
                        plainJson = s;
                    }
                    else
                    {
                        plainJson = JsonConvert.SerializeObject(objectResultLegacy.Value, Formatting.None);
                    }

                    string encryptedToken = "";
                    if (blnMobile)
                    {
                        encryptedToken = Convert.ToBase64String(EncryptStringToBytes_Aes(plainJson, _cryptoKey, _cryptoKey));
                        //var encrypted = Convert.FromBase64String(encryptedToken);
                        //string decString = DecryptStringFromBytes(encrypted, _cryptoKey, _cryptoKey);
                    }
                    else
                    {
                        encryptedToken = SimpleFernet.Encrypt(_key, Encoding.UTF8.GetBytes(plainJson));
                    }

                    //context.Result = new JsonResult(new { data = encryptedToken });
                    // preserve the original status code
                    context.Result = new JsonResult(new { data = encryptedToken })
                    {
                        StatusCode = objectResultLegacy.StatusCode ?? StatusCodes.Status200OK
                    };
                }
            }

            private static byte[] Base64UrlDecode(string base64Url)
            {
                string padded = base64Url.Replace('-', '+').Replace('_', '/');
                switch (padded.Length % 4)
                {
                    case 2:
                        padded += "==";
                        break;
                    case 3:
                        padded += "=";
                        break;
                }
                return Convert.FromBase64String(padded);
            }
        }

        [HttpGet("ReleasedOn", Name = "ReleasedOn")]
        public IActionResult ReleasedOn()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var getData = "05/11/2026 15:15 PM";

                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));

                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        [AllowAnonymous]
        [HttpGet("E2EPublicKey", Name = "E2EPublicKey")]
        public IActionResult E2EPublicKey()
        {
            return Ok(new { publicKey = Convert.ToBase64String(_e2eKeyService.PublicKey) });
        }

        //[HttpPost("Login_validate_Password", Name = "Login_validate_Password")]
        //public async Task<IActionResult> Login_validate_Password(string userId, string password, string ePassword, string key, string loginAs, string product, string ICPV, string feature = null)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            string strDebug = string.IsNullOrWhiteSpace(_configuration["DebugLog"]) ? "" : _configuration["DebugLog"].Trim();
        //            if (strDebug == "Y")
        //            {
        //                var jsonObject = new
        //                {
        //                    userId,
        //                    password,
        //                    ePassword,
        //                    key,
        //                    loginAs,
        //                    product,
        //                    ICPV
        //                };

        //                string jsonString = JsonConvert.SerializeObject(jsonObject);
        //                objUtility.InsertLog(jsonString, "");
        //            }

        //            userId = objUtility.mfnReplaceForSQLInjection(userId);
        //            password = objUtility.mfnReplaceForSQLInjection(password);
        //            ePassword = objUtility.mfnReplaceForSQLInjection(ePassword);
        //            key = objUtility.mfnReplaceForSQLInjection(key);
        //            loginAs = objUtility.mfnReplaceForSQLInjection(loginAs);
        //            product = objUtility.mfnReplaceForSQLInjection(product);
        //            string strICPV = string.IsNullOrWhiteSpace(ICPV) ? "" : ICPV;
        //            string strFeature = string.IsNullOrWhiteSpace(feature) ? "" : feature;
        //            string strParmICPV = objUtility.fnchkTable("WebParameter") ? objUtility.GetWebParameter("ICPV") : "";
        //            bool keyBased = false;
        //            bool branch = false;
        //            var role = "";
        //            string strMobileVer = "";

        //            if (!string.IsNullOrWhiteSpace(loginAs) && loginAs.Substring(0, 1) == "M")
        //            {
        //                if (loginAs.Contains("~"))
        //                {
        //                    string[] parts = loginAs.Split('~');
        //                    strMobileVer = parts[1];
        //                    loginAs = parts[0];
        //                }

        //                if (strMobileVer != "2.0.0.1" && objUtility.GetWebParameter("SMS2FA").Trim() != "")
        //                {
        //                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "You are currently using an older version. Please update to the latest version.", returnDt, ""));
        //                }
        //            }

        //            if (string.IsNullOrWhiteSpace(product))
        //            {
        //                product = "T";
        //            }
        //            product = product.ToUpper().Trim();

        //            if (!string.IsNullOrWhiteSpace(key) && (objUtility.isBPT() || (strICPV == "Y" && strParmICPV == "Y")))
        //            {
        //                keyBased = true;
        //            }
        //            if (string.IsNullOrWhiteSpace(loginAs))
        //            {
        //                loginAs = "C";
        //            }
        //            if (loginAs.Trim() == "B")
        //            {
        //                branch = true;
        //            }
        //            var autResp = _tradeWebRepository.Login_API_Authorize(key, product, loginAs, feature);
        //            if (autResp == null || autResp == "")
        //            {
        //                if (!string.IsNullOrWhiteSpace(key))
        //                {
        //                    string encKey = _configuration["encKey"];
        //                    encKey = objUtility.Decrypt(encKey);
        //                    encKey = encKey + DateTime.Now.ToString("yyyyMMdd");
        //                    var keybytes = Encoding.UTF8.GetBytes(encKey);
        //                    var iv = Encoding.UTF8.GetBytes(encKey);
        //                    var encrypted = Convert.FromBase64String(key);
        //                    var decriptedFromJavascript = DecryptStringFromBytes(encrypted, keybytes, iv);
        //                    key = string.Format(decriptedFromJavascript);
        //                }
        //                autResp = _tradeWebRepository.Login_API_Authorize(key, product, loginAs, feature);
        //                if (autResp == null || autResp == "")
        //                {
        //                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "You are not authorise to use TradeWebAPI Web Service,Please contact SecMark.", returnDt, ""));
        //                }
        //            }
        //            switch (autResp)
        //            {
        //                case "BPI":
        //                    role = "Admin";
        //                    break;
        //                case "TBL":
        //                    role = "Admin";
        //                    break;
        //                case "INP":
        //                    role = "GainLoss";
        //                    break;
        //                case "TBI":
        //                    role = "Branch";
        //                    break;
        //                case "PBI":
        //                    role = "Performance";
        //                    break;
        //                case "SUB":
        //                    role = "Subscription";
        //                    break;
        //                case "TMB":
        //                    role = "TradeMobile";
        //                    break;
        //                case "CBI":
        //                    role = "CrossNet";
        //                    break;
        //                case "EBI":
        //                    role = "EstroNet";
        //                    break;
        //                case "FUP":
        //                    role = "FundPayout";
        //                    break;
        //                case "CLM":
        //                    role = "CrossModification";
        //                    break;
        //                case "TPI":
        //                    role = "ReKYC";
        //                    break;
        //                case "RKM":
        //                    role = "ReKYC";
        //                    break;
        //                case "COM":
        //                    role = "CommonAPI";
        //                    break;
        //                case "CMP":
        //                    role = "ComAPI";
        //                    break;
        //                case "EBW":
        //                    role = "EstroWeb";
        //                    break;
        //                case "CBW":
        //                    role = "CrossWeb";
        //                    break;
        //            }
        //            if (objUtility.mfnGetSysSplFeature("RKC") || objUtility.mfnGetSysSplFeature("RKM") || role == "ReKYC")
        //            {
        //                objUtility.CreateReKYCTables();
        //                if (objUtility.GetWebParameter("TRADEPLUSTEMPDB") != "")
        //                {
        //                    string strDatabase = objUtility.GetWebParameter("TRADEPLUSTEMPDB").Trim();
        //                    /*var dbCon = new DataContext();
        //                    string connectionString = dbCon.Database.GetDbConnection().ConnectionString;*/
        //                    Microsoft.Data.SqlClient.SqlConnectionStringBuilder builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_connStr)
        //                    {
        //                        InitialCatalog = strDatabase
        //                    };
        //                    string newConnectionString = builder.ToString();

        //                    using (Microsoft.Data.SqlClient.SqlConnection sqlCon = new Microsoft.Data.SqlClient.SqlConnection(newConnectionString))
        //                    {
        //                        sqlCon.Open();
        //                        objUtility.CheckSP("json", sqlCon);
        //                        objUtility.CheckSP("ReKYC2", sqlCon);
        //                    }
        //                }
        //                objUtility.CheckSP("ReKYC");

        //            }
        //            objUtility.CreateMrgpTables();
        //            if (loginAs == "C" && product == "E") //For EstroWeb
        //            {
        //                if (objUtility.mfnGetSysSplFeatureDP("TBL") && role == "EstroWeb")
        //                {
        //                    objUtility.CreateTables(loginAs, product, "EstroWeb");
        //                    objUtility.CheckSP("EstroWeb");
        //                }
        //                else if (objUtility.mfnGetSysSplFeatureDP("TBO") && role == "EstroWeb")
        //                {
        //                    objUtility.CreateTables(loginAs, product, "EstroWebOffline");
        //                    objUtility.CheckSP("EstroWebO");
        //                }
        //                else
        //                {
        //                    objUtility.CreateTables(loginAs, product, "Estro");
        //                    objUtility.CheckSP("Estro");
        //                }
        //            }
        //            else if ((loginAs == "C" || loginAs == "B") && product == "C" && (role == "CrossWeb" || role == "CrossNet")) //For CrossWeb
        //            {
        //                if (objUtility.mfnGetSysSplFeatureDP("TBL") || role == "CrossNet")
        //                {
        //                    objUtility.CreateTables(loginAs, product, "CrossWeb");
        //                    if (objUtility.GetWebParameter("TRADEPLUSTEMPDB") != "")
        //                    {
        //                        string strDatabase = objUtility.GetWebParameter("TRADEPLUSTEMPDB").Trim();
        //                        //var dbCon = new DataContext();
        //                        //string connectionString = dbCon.Database.GetDbConnection().ConnectionString;
        //                        Microsoft.Data.SqlClient.SqlConnectionStringBuilder builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_connStr)
        //                        {
        //                            InitialCatalog = strDatabase
        //                        };
        //                        string newConnectionString = builder.ToString();

        //                        using (Microsoft.Data.SqlClient.SqlConnection sqlCon = new Microsoft.Data.SqlClient.SqlConnection(newConnectionString))
        //                        {
        //                            sqlCon.Open();
        //                            objUtility.CheckSP("json", sqlCon);
        //                        }
        //                    }
        //                    objUtility.CheckSP("CrossWeb");
        //                }
        //                else if (objUtility.mfnGetSysSplFeatureDP("TBO"))
        //                {
        //                    objUtility.CreateTables(loginAs, product, "CrossWebOffline");
        //                    objUtility.CheckSP("CrossWebO");
        //                }
        //            }
        //            else if (loginAs == "B" && product == "#")
        //            {
        //                if (objUtility.isRatnakar())
        //                {
        //                    objUtility.CreateTables(loginAs, product, "TradeWeb");
        //                    if (objUtility.GetWebParameter("TRADEPLUSTEMPDB") != "")
        //                    {
        //                        string strDatabase = objUtility.GetWebParameter("TRADEPLUSTEMPDB").Trim();
        //                        //var dbCon = new DataContext();
        //                        //string connectionString = dbCon.Database.GetDbConnection().ConnectionString;
        //                        Microsoft.Data.SqlClient.SqlConnectionStringBuilder builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_connStr)
        //                        {
        //                            InitialCatalog = strDatabase
        //                        };
        //                        string newConnectionString = builder.ToString();

        //                        using (Microsoft.Data.SqlClient.SqlConnection sqlCon = new Microsoft.Data.SqlClient.SqlConnection(newConnectionString))
        //                        {
        //                            sqlCon.Open();
        //                            objUtility.CheckSP("json", sqlCon);
        //                        }
        //                    }
        //                    objUtility.CheckSP("TradeWeb");
        //                }
        //            }
        //            /*else if ((loginAs == "C" || loginAs == "M") && product == "T" && (role == "Admin" || role == "TradeMobile"))
        //            {
        //                objUtility.CreateTables(loginAs, product, "TradeWeb");
        //                if (objUtility.GetWebParameter("TRADEPLUSTEMPDB") != "")
        //                {
        //                    string strDatabase = objUtility.GetWebParameter("TRADEPLUSTEMPDB").Trim();
        //                    var dbCon = new DataContext();
        //                    string connectionString = dbCon.Database.GetDbConnection().ConnectionString;
        //                    Microsoft.Data.SqlClient.SqlConnectionStringBuilder builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString)
        //                    {
        //                        InitialCatalog = strDatabase
        //                    };
        //                    string newConnectionString = builder.ToString();

        //                    using (Microsoft.Data.SqlClient.SqlConnection sqlCon = new Microsoft.Data.SqlClient.SqlConnection(newConnectionString))
        //                    {
        //                        sqlCon.Open();
        //                        objUtility.CheckSP("json", sqlCon);
        //                    }
        //                }
        //                objUtility.CheckSP("TradeWeb");
        //            }*/
        //            if (!string.IsNullOrEmpty(ePassword))
        //            {
        //                string encKey = _configuration["epwdKey"];
        //                encKey = objUtility.Decrypt(encKey);
        //                var keybytes = Encoding.UTF8.GetBytes(encKey);
        //                var iv = Encoding.UTF8.GetBytes(encKey);
        //                var encrypted = Convert.FromBase64String(ePassword);
        //                password = DecryptStringFromBytes(encrypted, keybytes, iv);
        //            }
        //            try
        //            {
        //                string encKey = _configuration["epwdKey"];
        //                encKey = objUtility.Decrypt(encKey);
        //                var keybytes = Encoding.UTF8.GetBytes(encKey);
        //                var iv = Encoding.UTF8.GetBytes(encKey);
        //                var encrypted = Convert.FromBase64String(userId);
        //                var decUserId = DecryptStringFromBytes(encrypted, keybytes, iv);
        //                if (decUserId != "keyError")
        //                {
        //                    userId = decUserId;
        //                }
        //            }
        //            catch (Exception) { }
        //            if (string.IsNullOrWhiteSpace(userId) || (!keyBased && string.IsNullOrWhiteSpace(password)))
        //            {
        //                return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Invalid Username or Password", returnDt, ""));
        //            }

        //            loginResponseWithDT userList = new loginResponseWithDT();
        //            userList = branch == true ? _tradeWebRepository.BranchUserDetails(userId, password, role) : _tradeWebRepository.UserDetails(userId, password, keyBased, product, role);

        //            if (!string.IsNullOrEmpty(userList.message))
        //            {
        //                return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, userList.message, returnDt, ""));
        //            }
        //            else if (userList.data.Rows.Count > 0)
        //            {
        //                int expTime = Convert.ToInt32(_configuration["tokenExpireTime"]);
        //                var datetimeExp = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
        //                string checkCondition = "N", sms2FA = "", product2FA = role;
        //                string[] allowedRoles = { "Admin", "CrossNet", "EstroNet", "CrossWeb", "EstroWeb", "TradeMobile", "Branch" };
        //                string userType = userList.data.Rows[0]["UserType"].ToString();
        //                //if (role == "TradeMobile")
        //                //{
        //                if (loginAs == "B" && (role == "CrossNet" || role == "EstroNet" || role == "Branch"))
        //                {
        //                    sms2FA = objUtility.GetSysParmSt("SMS2FA", "").Trim();
        //                    if (sms2FA != "")
        //                    {
        //                        if (role == "Branch")
        //                        {
        //                            product2FA = "TradeNet";
        //                        }
        //                    }
        //                }
        //                else
        //                {
        //                    sms2FA = objUtility.GetWebParameter("SMS2FA").Trim();
        //                }

        //                string str2FA = objUtility.GetWebParameter("2FA").Trim();
        //                if (str2FA != "")
        //                {
        //                    if (role == "Admin" && str2FA != "W" && str2FA != "B")
        //                    {
        //                        sms2FA = "";
        //                    }
        //                    else if (role == "TradeMobile" && str2FA != "M" && str2FA != "B")
        //                    {
        //                        sms2FA = "";
        //                    }
        //                }

        //                if (userType == "checker")
        //                {
        //                    sms2FA = "";
        //                }

        //                if (!string.IsNullOrEmpty(sms2FA) && allowedRoles.Contains(role))
        //                {
        //                    string dpid = "", tempUserId = userId;
        //                    string[] userStr = userId.Split("|");
        //                    if (loginAs == "C" && product == "E" && role == "EstroWeb")
        //                    {
        //                        product2FA = "EstroWeb";
        //                        if (userStr.Length > 1)
        //                        {
        //                            dpid = userStr[0];
        //                            userId = userStr[1];
        //                        }
        //                    }
        //                    else if (loginAs == "C" && product == "C" && role == "CrossWeb")
        //                    {
        //                        product2FA = "CrossWeb";
        //                        if (userStr.Length > 1)
        //                        {
        //                            dpid = userStr[0];
        //                            userId = userStr[1];
        //                        }
        //                    }
        //                    else if (loginAs == "C" && product == "T")
        //                    {
        //                        product2FA = "TradeWeb";
        //                    }

        //                    /*strsql = "select count(0) from OTP_Master with (NoLock) where OTP_ClientCode='" + userId + "' and OTP_Product='" + product2FA + "' and OTP_Type = '2FA' and CAST(OTP_SentDate AS DATETIME) + CAST(OTP_SentTime AS DATETIME) >= DATEADD(MINUTE, -15, GETDATE())";
        //                    DataTable dtCheck = objUtility.OpenDataTable(strsql);
        //                    if (dtCheck.Rows.Count > 0)
        //                    {
        //                        if (Convert.ToInt32(dtCheck.Rows[0][0]) >= 10)
        //                        {
        //                            return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Too many Attempts", returnDt, ""));
        //                        }
        //                    }*/

        //                    DataTable dtResponse = await objUtility.TradeMobileSendOTP(userId, product2FA, "2FA", role);
        //                    userId = tempUserId;
        //                    string strStatus = dtResponse.Rows[0]["Status"].ToString();
        //                    string strMessage = dtResponse.Rows[0]["Message"].ToString();

        //                    if (strStatus == "Y")
        //                    {
        //                        string decryptJwtKey = objUtility.Decrypt(_configuration["Jwt:Key"].ToString());
        //                        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(decryptJwtKey));
        //                        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        //                        int tokenExpTime = 5;
        //                        var claims = new[] {
        //                                new Claim(JwtRegisteredClaimNames.Sub, userId.ToUpper()),
        //                                new Claim(JwtRegisteredClaimNames.Jti, userId.ToUpper()),
        //                                new Claim(type: "username", value: userId.ToUpper()),
        //                                new Claim(type: "loginas", value: loginAs),
        //                                new Claim(type: "product", value: product),
        //                                new Claim(type: "loginrole", value: role),
        //                                new Claim(type: "branch", value: branch ? "Y" : "N"),
        //                                new Claim(type: "userData", value: JsonConvert.SerializeObject(userList.data)),
        //                                new Claim(ClaimTypes.Role, "TradeWeb2FA"),
        //                                new Claim(type: "otpproduct", value: product2FA),
        //                            };
        //                        var token = new JwtSecurityToken(_configuration["Jwt:Issuer"],
        //                            _configuration["Jwt:Issuer"],
        //                            claims,
        //                            expires: DateTime.UtcNow.AddMinutes(tokenExpTime),
        //                            signingCredentials: credentials);

        //                        string str2FAToken = new JwtSecurityTokenHandler().WriteToken(token);
        //                        DataTable dtUserData = userList.data;
        //                        dtUserData.Columns.Add("LoginType", typeof(string));
        //                        dtUserData.Columns.Add("LoginMessage", typeof(string));
        //                        dtUserData.Rows[0]["LoginType"] = "2FA";
        //                        dtUserData.Rows[0]["LoginMessage"] = strMessage;

        //                        tokenResponse result2fa = new tokenResponse
        //                        {
        //                            status = true,
        //                            message = "success",
        //                            status_code = (int)HttpStatusCode.OK,
        //                            token = str2FAToken,
        //                            tokenExpireTime = datetimeExp.AddMinutes(5).ToString("yyyy/MM/dd HH:mm:ss"),
        //                            data = dtUserData
        //                        };
        //                        return Ok(JsonConvert.SerializeObject(result2fa, Formatting.Indented));
        //                    }
        //                    else
        //                    {
        //                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, strMessage, returnDt, ""));
        //                    }
        //                }
        //                //}

        //                if (product.Trim() == "T")
        //                {
        //                    FillConfigParametersString();
        //                }
        //                else
        //                {
        //                    _configuration["IsTradeWeb"] = product.Trim();
        //                    if (loginAs == "C" && product == "E" && objUtility.mfnGetSysSplFeatureDP("TBL") == false)
        //                    {
        //                        string[] strUserID = userId.Split("|");
        //                        //userId = strUserID.Length > 1 ? strUserID[1] : strUserID[0];
        //                        _configuration["IsEstroOffLine"] = "Y";
        //                        _configuration["SessionDPID"] = strUserID[0];
        //                    }
        //                    else if (loginAs == "C" && product == "C" && objUtility.mfnGetSysSplFeatureDP("TBL") == false)
        //                    {
        //                        string[] strUserID = userId.Split("|");
        //                        _configuration["IsCrossOffLine"] = "Y";
        //                        _configuration["SessionDPID"] = strUserID[0];
        //                    }
        //                }
        //                var guidVal = Guid.NewGuid();
        //                if ((role == "Admin" || role == "TradeMobile") && branch == false)
        //                {
        //                    objUtility.ExecuteSQL("update Login_Session set ls_token = '" + guidVal + "', ls_logintype = '" + (role == "Admin" ? "C" : "M") + "', ls_logintm = '" + DateTime.Now.ToString("HH:mm:ss") + "'  Where ls_code = '" + userId + "' And ls_logindt = '" + DateTime.Now.ToString("yyyyMMdd") + "'  And len(ls_token) < 3");
        //                }
        //                string strUserAccess = "";
        //                if (branch)
        //                {
        //                    if (role == "Branch")
        //                    {
        //                        strUserAccess = objUtility.GetUserAccess(objUtility.fnFireQueryTradeWeb("User_master", "um_specialrights", "um_user_id", userId, true));
        //                    }
        //                    else
        //                    {
        //                        strUserAccess = "Branch";
        //                    }
        //                }

        //                var tokenString = branch == true ? GenerateJSONWebTokenUser(new TradeWebLoginModel { username = userId, password = password, role = role, guid = guidVal.ToString(), tokenExpTime = expTime, useraccess = strUserAccess }) : GenerateJSONWebToken(new TradeWebLoginModel { username = userId, password = password, role = role, guid = guidVal.ToString(), tokenExpTime = expTime });

        //                tokenResponse result = new tokenResponse();
        //                result.status = true;
        //                result.message = "success";
        //                result.status_code = (int)HttpStatusCode.OK;
        //                result.token = tokenString;
        //                result.tokenExpireTime = datetimeExp.AddMinutes(expTime).ToString("yyyy/MM/dd HH:mm:ss");
        //                result.data = userList.data;
        //                var jsonData = JsonConvert.SerializeObject(result, Formatting.Indented);
        //                return Ok(jsonData);
        //            }
        //            return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, userList.message, userList, ""));
        //        }
        //        catch (Exception ex)
        //        {
        //            return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
        //        }
        //    }
        //    return BadRequest();
        //}

        [HttpGet("Login_Get_CompanyName", Name = "Login_Get_CompanyName")]
        public IActionResult Login_Get_CompanyName()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var getData = _tradeWebRepository.Get_CompanyName();

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
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        [HttpPost("SendEmail", Name = "SendEmail")]
        public async Task<IActionResult> SendEmail([FromBody] EmailSmsReqModel reqObject)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string reqName = reqObject.reqestName;

                    if (reqName == "SMS")
                    {
                        try
                        {
                            var jsonString = ((System.Text.Json.JsonElement)reqObject.requestObject).GetRawText();
                            var jsonObject = JsonConvert.DeserializeObject<dynamic>(jsonString.ToString());
                            string strMessage = jsonObject["Message"];
                            string strMobileNo = jsonObject["MobileNo"];
                            string[] strSMSParamVal = new string[5];
                            string strSMSParameter = "SMSUSERID/SMSPWD/SMSSENDER/SMSLENGTH/SMSLINK";
                            string strvalue = string.Empty;
                            string strFromNo1 = "";
                            string strFromNo2 = "";
                            for (int i = 0; i <= 4; i++)
                            {
                                strvalue = strSMSParameter.Split('/')[i];
                                strSMSParamVal[i] = objUtility.fnFireQueryTradeWeb("sysparameter", "SP_SYSVALUE", "sp_parmcd", strvalue, true);
                            }

                            string strURLLink = strSMSParamVal[4];

                            if (strURLLink.IndexOf("<USERID>") != -1 && strSMSParamVal[0].Trim() != "")
                            {
                                strURLLink = strURLLink.Replace("<USERID>", strSMSParamVal[0].Trim());
                            }
                            if (strURLLink.IndexOf("<PASSWORD>") != -1 && strSMSParamVal[1].Trim() != "")
                            {
                                strURLLink = strURLLink.Replace("<PASSWORD>", strSMSParamVal[1].Trim());
                            }
                            if (strSMSParamVal[2].Trim() == "")
                            {
                                if (strURLLink.IndexOf("<SENDERID>") != -1)
                                {
                                    strURLLink = strURLLink.Replace("<SENDERID>", strSMSParamVal[2].Trim());
                                }
                            }
                            else
                            {
                                strFromNo1 = strSMSParamVal[2].Trim();
                                if (strFromNo1.IndexOf("|") != -1)
                                {
                                    strFromNo2 = strFromNo1.Split('|')[1];
                                    strFromNo1 = strFromNo1.Split('|')[0];
                                }
                                else
                                {
                                    strFromNo1 = Strings.Left(strFromNo1.Trim(), 10);
                                    strFromNo2 = "";
                                }
                                if (strURLLink.IndexOf("<SENDERID>") != -1)
                                {
                                    strURLLink = strURLLink.Replace("<SENDERID>", strSMSParamVal[2].Trim());
                                }
                                else if (strURLLink.IndexOf("<SENDERID1>") != -1 || strURLLink.IndexOf("<SENDERID2>") != -1)
                                {
                                    strURLLink = strURLLink.Replace("<SENDERID1>", strFromNo1).Replace("<SENDERID2>", strFromNo2);
                                }
                            }

                            strURLLink = strURLLink.Replace("<MESSAGE>", strMessage);

                            if (strURLLink.IndexOf("/opted.smsapi.org/v1.0.7/") != -1)
                            {
                                strURLLink = strURLLink.Replace("<MESSAGE>", strMessage);
                            }

                            if (strURLLink.IndexOf("/174.143.34.193/") != -1)
                            {
                                if (strMessage.Trim().Length > 160)
                                {
                                    strURLLink = strURLLink + "&mt=4";
                                }
                                else
                                {
                                    strURLLink = strURLLink + "&mt=0";
                                }
                                strURLLink = strURLLink + "&typeofmessage=1";
                            }

                            if (objUtility.GetSysParmSt("SMSCOUNTRYCD", "") == "Y")
                            {
                                if (strMobileNo.Trim().Length == 10)
                                {
                                    strMobileNo = "91" + strMobileNo;
                                }
                            }
                            else
                            {
                                if (strMobileNo.Trim().Length > 10)
                                {
                                    strMobileNo = Strings.Right(strMobileNo.Trim(), 10);
                                }
                            }
                            strURLLink = strURLLink.Replace("<CLIENTMOBILE>", strMobileNo.Trim());

                            if (strURLLink.IndexOf("myvaluefirst.com") != -1)
                            {
                                string strSENDER = "";
                                if (strSMSParamVal[2].Trim().IndexOf("|") != -1)
                                {
                                    if (Strings.Left(strMobileNo.Trim(), 2) == "92" || Strings.Left(strMobileNo.Trim(), 2) == "93")
                                    {
                                        strSENDER = strSMSParamVal[2].Trim().Split('|')[1];
                                    }
                                    else
                                    {
                                        strSENDER = strSMSParamVal[2].Trim().Split('|')[0];
                                    }
                                }
                                else
                                {
                                    strSENDER = Strings.Left(strSMSParamVal[2].Trim(), 10);
                                }
                                strURLLink = strURLLink.Replace("<SENDERID3>", strSENDER);
                            }
                            //if (objUtility.GetWebParameter("SECURITYPROT").Trim() == "TLS12")
                            //{
                            //    ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072 | (SecurityProtocolType)48;
                            //}
                            //HttpWebRequest http = (HttpWebRequest)WebRequest.Create(strURLLink);
                            //HttpWebResponse response = (HttpWebResponse)http.GetResponse();
                            //StreamReader sr = new StreamReader(response.GetResponseStream());
                            //string content = sr.ReadToEnd();
                            //string strresponse = content;
                            //sr.Close();

                            HttpResponseMessage response = await objUtility.GetResponseAsync(strURLLink);

                            string content = await response.Content.ReadAsStringAsync();

                            string strresponse = content;

                            if (response.StatusCode == HttpStatusCode.OK)
                            {
                                strresponse = "SMS Sent Successfully.";
                            }
                            else if (content.IndexOf("<ERROR>") != -1)
                            {
                                if (content.IndexOf("<DESC>") != -1)
                                {
                                    strresponse = Strings.Mid(content, Strings.InStr(1, content, "<DESC>", CompareMethod.Text) + 6);
                                    strresponse = Strings.Left(content, Strings.InStr(1, content, "</DESC>", CompareMethod.Text) - 1);
                                }
                                else
                                {
                                    strresponse = "SMS Sent Successfully.";
                                }
                            }
                            else if (content.IndexOf("\"\"error-status\"\":\"\"Success\"\"") != -1)
                            {
                                strresponse = "SMS Sent Successfully.";
                            }
                            else if (content.IndexOf(strMobileNo.Trim()) != -1)
                            {
                                strresponse = "Message Send Successfully.";
                            }
                            else if (content.IndexOf("<sms>") != -1)
                            {
                                if (content.IndexOf("-1") != -1)
                                {
                                    strresponse = "Message Sending Failed";
                                }
                                else if (content.ToUpper().IndexOf("INVALID USERNAME OR PASSWORD") != -1)
                                {
                                    strresponse = "Sending Failed. Invalid Username Or Password.";
                                }
                                else
                                {
                                    strresponse = "Message Send Successfully.";
                                }
                            }
                            else if (content.IndexOf("Fail") != -1)
                            {
                                strresponse = "Message Sending Failed";
                            }
                            else if (content.ToUpper().IndexOf("INVALID USERNAME OR PASSWORD") != -1 || content.ToUpper().IndexOf("INVALID USERNAME AND PASSWORD") != -1)
                            {
                                strresponse = "Message Sending Failed. Invalid Username Or Password.";
                            }
                            else if (content.ToUpper().IndexOf("1701|") != -1 || content.ToUpper().IndexOf("SUCCESS") != -1)
                            {
                                strresponse = "Message Send Successfully.";
                            }
                            else if (content == "100")
                            {
                                strresponse = "Message Send Successfully.";
                            }
                            else if (content.ToUpper().IndexOf(":") != -1)
                            {
                                if (content.ToUpper().Split(':')[1] == "")
                                {
                                    strresponse = content;
                                }
                                else
                                {
                                    strresponse = "Message Send Successfully.";
                                }
                            }
                            else
                            {
                                strresponse = content;
                            }
                            return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", strresponse, ""));
                        }
                        catch (Exception ex)
                        {
                            return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", ex.Message.ToString(), ""));
                        }
                    }
                    else if (reqName == "Email")
                    {
                        string strSMTPParamVal = objUtility.GetWebParameter("SMTPSERVER");
                        if (!string.IsNullOrEmpty(strSMTPParamVal.Trim()))
                        {
                            try
                            {
                                /*var jsonString = ((System.Text.Json.JsonElement)reqObject.requestObject).GetRawText();
                                var jsonObject = JsonConvert.DeserializeObject<dynamic>(jsonString.ToString());
                                string toEmailId = jsonObject["ToEmailId"];
                                string ccEmailId = jsonObject["CCEmailId"];
                                string bccEmailId = jsonObject["BCCEmailId"];
                                string[] toEmailIdStr = toEmailId.Split(",", StringSplitOptions.RemoveEmptyEntries);
                                string[] ccEmailIdStr = ccEmailId.Split(",", StringSplitOptions.RemoveEmptyEntries);
                                string[] bccEmailIdStr = bccEmailId.Split(",", StringSplitOptions.RemoveEmptyEntries);
                                string subject = jsonObject["Subject"];
                                var emailBody = jsonObject["Body"];
                                var jsonAttachment = jsonObject["Attachment"];

                                string strHost = strSMTPParamVal.Split('/')[0];
                                int intPort = Convert.ToInt32(strSMTPParamVal.Split('/')[1]);
                                string strUserID = strSMTPParamVal.Split('/')[2];
                                string strPWD = strSMTPParamVal.Split('/')[3];
                                string strEmail = strSMTPParamVal.Split('/')[4];
                                string strSSL = strSMTPParamVal.Split('/')[5];

                                MailMessage Msg = new MailMessage();

                                Msg.From = new MailAddress(strEmail);
                                foreach (string toEmail in toEmailIdStr)
                                {
                                    Msg.To.Add(toEmail);
                                }
                                foreach (string ccEmail in ccEmailIdStr)
                                {
                                    Msg.CC.Add(ccEmail);
                                }
                                foreach (string bccEmail in bccEmailIdStr)
                                {
                                    Msg.Bcc.Add(bccEmail);
                                }
                                Msg.Subject = subject;
                                Msg.Body = emailBody;
                                Msg.IsBodyHtml = true;

                                if (jsonAttachment != null)
                                {
                                    var attchObject = JsonConvert.DeserializeObject<dynamic>(jsonAttachment.ToString());
                                    foreach (var attFile in attchObject)
                                    {
                                        string fileName = attFile.FileName.ToString();
                                        string base64 = attFile.Base64.ToString();
                                        Byte[] bytes = Convert.FromBase64String(base64);
                                        MemoryStream ms = new MemoryStream(bytes);
                                        Attachment data = new Attachment(ms, fileName);
                                        data.ContentId = fileName;
                                        data.ContentDisposition.Inline = true;
                                        Msg.Attachments.Add(data);
                                    }
                                }
                                SmtpClient smtp = new SmtpClient();
                                smtp.Host = strHost;
                                smtp.Port = intPort;
                                smtp.UseDefaultCredentials = false;
                                smtp.Credentials = new System.Net.NetworkCredential(strUserID, strPWD);
                                smtp.EnableSsl = strSSL == "N" ? false : true;
                                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                                System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate (object s,
                                System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                                System.Security.Cryptography.X509Certificates.X509Chain chain,
                                System.Net.Security.SslPolicyErrors sslPolicyErrors)
                                {
                                    return true;
                                };
                                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                                smtp.Send(Msg);*/

                                var jsonString = ((System.Text.Json.JsonElement)reqObject.requestObject).GetRawText();

                                var jsonObject = JsonConvert.DeserializeObject<dynamic>(jsonString);

                                string toEmailId = jsonObject["ToEmailId"];
                                string ccEmailId = jsonObject["CCEmailId"];
                                string bccEmailId = jsonObject["BCCEmailId"];

                                string[] toEmailIdStr = toEmailId.Split(",", StringSplitOptions.RemoveEmptyEntries);
                                string[] ccEmailIdStr = ccEmailId.Split(",", StringSplitOptions.RemoveEmptyEntries);
                                string[] bccEmailIdStr = bccEmailId.Split(",", StringSplitOptions.RemoveEmptyEntries);

                                string subject = jsonObject["Subject"];
                                string emailBody = jsonObject["Body"];

                                var jsonAttachment = jsonObject["Attachment"];

                                string strHost = strSMTPParamVal.Split('/')[0];
                                int intPort = Convert.ToInt32(strSMTPParamVal.Split('/')[1]);
                                string strUserID = strSMTPParamVal.Split('/')[2];
                                string strPWD = strSMTPParamVal.Split('/')[3];
                                string strEmail = strSMTPParamVal.Split('/')[4];
                                string strSSL = strSMTPParamVal.Split('/')[5];

                                var message = new MimeMessage();

                                message.From.Add(new MailboxAddress("", strEmail));

                                foreach (string toEmail in toEmailIdStr)
                                {
                                    if (!string.IsNullOrWhiteSpace(toEmail))
                                    {
                                        message.To.Add(MailboxAddress.Parse(toEmail.Trim()));
                                    }
                                }

                                foreach (string ccEmail in ccEmailIdStr)
                                {
                                    if (!string.IsNullOrWhiteSpace(ccEmail))
                                    {
                                        message.Cc.Add(MailboxAddress.Parse(ccEmail.Trim()));
                                    }
                                }

                                foreach (string bccEmail in bccEmailIdStr)
                                {
                                    if (!string.IsNullOrWhiteSpace(bccEmail))
                                    {
                                        message.Bcc.Add(MailboxAddress.Parse(bccEmail.Trim()));
                                    }
                                }

                                message.Subject = subject;

                                var bodyBuilder = new BodyBuilder();

                                bodyBuilder.HtmlBody = emailBody;

                                if (jsonAttachment != null)
                                {
                                    var attchObject = JsonConvert.DeserializeObject<dynamic>(jsonAttachment.ToString());

                                    foreach (var attFile in attchObject)
                                    {
                                        string fileName = attFile.FileName.ToString();
                                        string base64 = attFile.Base64.ToString();

                                        byte[] bytes = Convert.FromBase64String(base64);

                                        bodyBuilder.Attachments.Add(fileName, bytes);
                                    }
                                }

                                message.Body = bodyBuilder.ToMessageBody();

                                using (var smtp = new MailKit.Net.Smtp.SmtpClient())
                                {
                                    // SSL bypass for TESTING/UAT only
                                    smtp.ServerCertificateValidationCallback =
                                        (sender, certificate, chain, sslPolicyErrors) => true;

                                    smtp.Connect(
                                        strHost,
                                        intPort,
                                        strSSL == "N"
                                            ? SecureSocketOptions.None
                                            : SecureSocketOptions.StartTls
                                    );

                                    smtp.Authenticate(strUserID, strPWD);

                                    smtp.Send(message);

                                    smtp.Disconnect(true);
                                }

                                return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", "Email has been sent successfully.", ""));
                            }
                            catch (Exception ex)
                            {
                                return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", ex.Message.ToString(), ""));
                            }
                        }

                    }
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "failed", "Details not found", ""));

                }
                catch (Exception ex)
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin,TradeMobile,ComAPI,CrossNet,EstroNet,CrossWeb,EstroWeb,Branch")]
        [HttpPost, Route("TradeWeb")]
        [Consumes("application/xml")]
        [TypeFilter(typeof(ValidateUser), Arguments = new object[] { "TradeWeb" })]
        [ServiceFilter(typeof(E2EDecryptFilter))]
        [ServiceFilter(typeof(EncryptResponseAttribute))]
        public async Task<IActionResult> TradeWeb()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            string xmlStr;
            try
            {
                using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
                {
                    xmlStr = await reader.ReadToEndAsync();
                }
            }
            catch (Exception ex)
            {
                return BadRequest(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = ex.Message.ToString(), data = null, datarows = null }, Formatting.Indented));
            }
            return await ProcessTradeWebXml(xmlStr);
        }

        /// <summary>
        /// Core handler for a single TradeWeb XML request (&lt;dsXml&gt;...&lt;/dsXml&gt;).
        /// Shared by the TradeWeb endpoint and each item of the TradeWebBatch endpoint, so
        /// both paths get identical validation, user-id injection and logging.
        /// </summary>
        internal async Task<IActionResult> ProcessTradeWebXml(string xmlStr)
        {
            {
                long logSrNo = 0;
                string logRespMessage = "", logErrorMessage = "", logReq = "";
                string strDebugFlag = "";
                string connetionString = objUtility.GetConnectionStr();
                try
                {
                    string userId = "", companyCode = "";
                    JwtSecurityToken token = GetToken();
                    var userName = token.Claims.First(claim => claim.Type == "username").Value;
                    var role = token.Claims.First(claim => claim.Type == ClaimTypes.Role).Value;
                    // The batch endpoint sets this before running items in parallel, so only
                    // write it when missing (HttpContext.Items is not safe for concurrent writes).
                    if (role == "TradeMobile" && HttpContext.Items["TradeMobile"]?.ToString() != "Y")
                    {
                        HttpContext.Items["TradeMobile"] = "Y";
                    }
                    var userAccess = token.Claims.FirstOrDefault(claim => claim.Type == "useraccess")?.Value ?? "";
                    string[] strUserID = userName.Split("|");
                    //userId = userName;
                    if (strUserID.Length > 1)
                    {
                        userId = strUserID[1];
                        companyCode = strUserID[0];
                    }
                    else
                    {
                        userId = strUserID[0];
                    }
                    {
                        logReq = xmlStr;
                        XmlDocument xml = new XmlDocument();
                        xml.LoadXml(xmlStr);
                        ///// *********  For XML object J_Ui *********
                        if (xml.GetElementsByTagName("J_Ui") == null || xml.GetElementsByTagName("J_Ui").Count == 0)
                        {
                            logErrorMessage = "J_Ui element not present";
                            return BadRequest("J_Ui element not present");
                        }
                        string jsonString = xml.GetElementsByTagName("J_Ui")[0].InnerText;

                        if (string.IsNullOrWhiteSpace(jsonString))
                        {
                            logErrorMessage = "J_Ui element is empty";
                            return BadRequest("J_Ui element is empty");
                        }
                        // Replace smart quotes if any
                        jsonString = "{" + jsonString.Replace("\u201C", "\"").Replace("\u201D", "\"") + "}";
                        JUiModel jUiObject;
                        try
                        {
                            // Deserialize the JSON string into a C# object
                            jUiObject = JsonConvert.DeserializeObject<JUiModel>(jsonString);

                            if (role == "TradeMobile")
                            {
                                string pattern = "\"RequestFrom\"\\s*:\\s*\".*?\"";

                                if (Regex.IsMatch(jsonString, pattern, RegexOptions.IgnoreCase))
                                {
                                    // Replace existing RequestFrom
                                    jsonString = Regex.Replace(jsonString,
                                                               pattern,
                                                               $"\"RequestFrom\":\"M\"",
                                                               RegexOptions.IgnoreCase);
                                }
                                else
                                {
                                    // If not found append RequestFrom
                                    if (!string.IsNullOrWhiteSpace(jsonString))
                                    {
                                        if (jsonString.TrimEnd().EndsWith(","))
                                            jsonString += $"\"RequestFrom\":\"M\"";
                                        else
                                            jsonString += $",\"RequestFrom\":\"M\"";
                                    }
                                    else
                                    {
                                        jsonString = $"\"RequestFrom\":\"M\"";
                                    }
                                }
                                xml.GetElementsByTagName("J_Ui")[0].InnerText = jsonString.Replace("{", "").Replace("}", "");
                            }
                        }
                        catch (JsonException ex)
                        {
                            logErrorMessage = $"Invalid JSON in J_Ui element: {ex.Message}";
                            return BadRequest($"Invalid JSON in J_Ui element: {ex.Message}");
                        }
                        jsonString = xml.GetElementsByTagName("J_Api")[0].InnerText;

                        if (!string.IsNullOrWhiteSpace(jsonString))
                        {
                            // Replace smart quotes if any
                            jsonString = "{" + jsonString.Replace("\u201C", "\"").Replace("\u201D", "\"") + "}";
                            JApi jApiObject;
                            try
                            {
                                jApiObject = JsonConvert.DeserializeObject<JApi>(jsonString);
                                if (role != "ComAPI")
                                {
                                    jApiObject.UserId = userId.Trim();
                                }
                                jApiObject.UserAccess = userAccess;
                                xml.DocumentElement["J_Api"].InnerText = JsonConvert.SerializeObject(jApiObject).Replace("{", "").Replace("}", "");
                            }
                            catch (JsonException ex)
                            {
                                logErrorMessage = $"Invalid JSON in J_Api element: {ex.Message}";
                                return BadRequest($"Invalid JSON in J_Api element: {ex.Message}");
                            }
                        }
                        string strModuleName = objUtility.mfnReplaceForSQLInjection(jUiObject.ActionName?.Trim() ?? "");
                        string strFunctionName = objUtility.mfnReplaceForSQLInjection(jUiObject.Option?.Trim() ?? "");
                        string strReportDisplay = objUtility.mfnReplaceForSQLInjection(jUiObject.ReportDisplay?.Trim() ?? "");

                        if (strFunctionName == "ChangePassword" && (role == "CrossWeb" || role == "EstroWeb") && (strModuleName.ToUpper() != "ESTRONET" && strModuleName.ToUpper() != "CROSSNET"))
                        {
                            var dataXML = xml.GetElementsByTagName("X_Data")[0]?.InnerXml;
                            XDocument xdocData = XDocument.Parse($"<root>{dataXML}</root>");
                            string clientCode = userId; //xdocData.Root.Element("ClientCode")?.Value;
                            string oldPassword = xdocData.Root.Element("OldPassword")?.Value;
                            string newPassword = xdocData.Root.Element("NewPassword")?.Value;
                            if (string.IsNullOrEmpty(clientCode))
                            {
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "ClientCode should not be blank.", data = null, datarows = null }, Formatting.Indented));
                            }
                            else if (string.IsNullOrEmpty(oldPassword))
                            {
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "Old Password should not be blank.", data = null, datarows = null }, Formatting.Indented));
                            }
                            else if (string.IsNullOrEmpty(newPassword))
                            {
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "New Password should not be blank.", data = null, datarows = null }, Formatting.Indented));
                            }
                            newPassword = objUtility.mfnReplaceForSQLInjection(newPassword);
                            companyCode = objUtility.mfnReplaceForSQLInjection(companyCode ?? "");
                            clientCode = objUtility.mfnReplaceForSQLInjection(clientCode ?? "");
                            /////// **** Check password validation and policies ****
                            string pwdCondition = objUtility.CheckPasswordCondition(newPassword, companyCode, clientCode);
                            if (!string.IsNullOrEmpty(pwdCondition))
                            {
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = pwdCondition, data = null, datarows = null }, Formatting.Indented));
                            }
                            oldPassword = objUtility.mfnReplaceForSQLInjection(oldPassword);
                            newPassword = objUtility.mfnReplaceForSQLInjection(newPassword);
                            if (objUtility.GetWebParameter("TWEBENCPWD") == "Y")
                            {
                                oldPassword = objUtility.Encrypt(oldPassword);
                                newPassword = objUtility.Encrypt(newPassword);
                            }
                            xdocData.Root.Element("OldPassword").Value = oldPassword;
                            xdocData.Root.Element("NewPassword").Value = newPassword;
                            xdocData.Root.Element("ClientCode")?.Remove();
                            xdocData.Root.Add(new XElement("ClientCode", userId));
                            xml.DocumentElement["X_Data"].InnerXml = xdocData.Root.ToString(SaveOptions.DisableFormatting).Replace("<root>", "").Replace("</root>", "");
                            string strXMLCP = objUtility.SerializeToXml(xml, omitXmlDeclaration: true, indent: false, encoding: "utf-16");
                            using (Microsoft.Data.SqlClient.SqlConnection conn = new Microsoft.Data.SqlClient.SqlConnection(connetionString))
                            {
                                conn.Open();
                                logSrNo = objUtility.InsertLog(0, role, "", strXMLCP, "", "", clientCode, conn);
                            }
                            var getDataCP = _tradeWebRepository.CommonAPIrepoCall(strModuleName, strFunctionName, strXMLCP);
                            if (getDataCP != null)
                            {
                                if (getDataCP.Data?.Tables[0]?.Rows.Count > 0)
                                {
                                    if (getDataCP.Data.Tables[0].Rows[0][0].Contains("success"))
                                    {
                                        logRespMessage = "Password changed successfully";
                                        objUtility.AfterChangePasswordTblEntry(newPassword, companyCode, clientCode);
                                        return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "", data = getDataCP.Data, datarows = getDataCP.DataRows }, Formatting.Indented));
                                    }
                                }
                                logErrorMessage = "Old Password not matched";
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "Old password not matched.", data = null, datarows = null }, Formatting.Indented));
                            }
                            else
                            {
                                logErrorMessage = "Old Password not matched";
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "Old password not matched.", data = null, datarows = null }, Formatting.Indented));
                            }
                        }
                        if (strFunctionName.ToUpper() == "CHANGEPASSWORD")
                        {
                            var dataXML = xml.GetElementsByTagName("X_Data")[0]?.InnerXml;
                            XDocument xdocData = XDocument.Parse($"<root>{dataXML}</root>");
                            string oldPassword = xdocData.Root.Element("EOldPassword")?.Value;
                            string newPassword = xdocData.Root.Element("ENewPassword")?.Value;

                            string encKey = _configuration["epwdKey"];
                            encKey = objUtility.Decrypt(encKey);
                            var keybytes = Encoding.UTF8.GetBytes(encKey);
                            var iv = Encoding.UTF8.GetBytes(encKey);

                            if (!string.IsNullOrEmpty(oldPassword))
                            {
                                var encryptedOld = Convert.FromBase64String(oldPassword);
                                xdocData.Root.Element("EOldPassword")?.Remove();
                                xdocData.Root.Add(new XElement("OldPassword", DecryptStringFromBytes(encryptedOld, keybytes, iv)));
                            }

                            if (!string.IsNullOrEmpty(newPassword))
                            {
                                var encryptedNew = Convert.FromBase64String(newPassword);
                                xdocData.Root.Element("ENewPassword")?.Remove();
                                xdocData.Root.Add(new XElement("NewPassword", DecryptStringFromBytes(encryptedNew, keybytes, iv)));
                            }
                            xml.DocumentElement["X_Data"].InnerXml = xdocData.Root.ToString(SaveOptions.DisableFormatting).Replace("<root>", "").Replace("</root>", "");
                        }

                        if (strModuleName.ToUpper() == "QUERYFORM" && strFunctionName.ToUpper() == "VALIDATEPASSWORD")
                        {
                            var dataXML = xml.GetElementsByTagName("X_Filter_Multiple")[0]?.InnerXml;
                            XDocument xdocData = XDocument.Parse($"<root>{dataXML}</root>");
                            string monthlyPassword = xdocData.Root.Element("EPassword")?.Value;

                            string encKey = _configuration["epwdKey"];
                            encKey = objUtility.Decrypt(encKey);
                            var keybytes = Encoding.UTF8.GetBytes(encKey);
                            var iv = Encoding.UTF8.GetBytes(encKey);

                            if (!string.IsNullOrEmpty(monthlyPassword))
                            {
                                var encryptedPwd = Convert.FromBase64String(monthlyPassword);
                                monthlyPassword = DecryptStringFromBytes(encryptedPwd, keybytes, iv);
                            }

                            DataSet dsRes = new DataSet();
                            dsRes.Tables.Add("rs0");
                            dsRes.Tables[0].Columns.Add("Column1", typeof(string));

                            if (!string.IsNullOrEmpty(monthlyPassword))
                            {
                                if (monthlyPassword == objUtility.mfnGetDemoMonthpwd())
                                {
                                    dsRes.Tables[0].Rows.Add("<Flag>S</Flag><Message>Password Match</Message>");
                                    return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "", data = dsRes, datarows = new List<string>() { "1" } }, Formatting.Indented));
                                }
                            }

                            dsRes.Tables[0].Rows.Add("<Flag>E</Flag><Message>Wrong Password</Message>");
                            return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "", data = dsRes, datarows = new List<string>() { "1" } }, Formatting.Indented));
                        }

                        var filterxml = xml.GetElementsByTagName("X_Filter")[0]?.InnerXml;
                        XDocument xdoc = XDocument.Parse($"<root>{filterxml}</root>");
                        if (companyCode != "")
                        {
                            xdoc.Root.Element("ClientCode")?.Remove();
                            xdoc.Root.Add(new XElement("ClientCode", userId));
                            xdoc.Root.Element("CompanyCode")?.Remove();
                            xdoc.Root.Add(new XElement("CompanyCode", companyCode));
                        }

                        var xDataNode = xml.GetElementsByTagName("X_Data")[0];

                        bool isEmailTemplateSave = string.Equals(strModuleName?.Trim(), "EMAILTEMPLATE", StringComparison.OrdinalIgnoreCase) &&
                            (string.Equals(strFunctionName?.Trim(), "ADD", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(strFunctionName?.Trim(), "EDIT", StringComparison.OrdinalIgnoreCase));

                        // 1. sanitize/validate first
                        if (xDataNode != null && isEmailTemplateSave)
                        {
                            var errors = objUtility.SanitizeXData(xDataNode);
                            if (errors.Count > 0)
                            {
                                return Ok(new
                                {
                                    success = false,
                                    message = string.Join("; ", errors),
                                    data = (object)null,
                                    datarows = (object)null
                                });
                            }
                        }

                        string pdfType = "", entryName = "", getJson = "", reportType = "";

                        var xDataFilter = xml.GetElementsByTagName("X_Data")[0]?.InnerXml;
                        if (xDataFilter != null)
                        {
                            XDocument xData = XDocument.Parse($"<root>{xDataFilter}</root>");
                            pdfType = xData.Root.Element("ReportName")?.Value;
                            entryName = xData.Root.Element("EntryName")?.Value;
                            getJson = xData.Root.Element("GetJson")?.Value;
                        }
                        if (xml.DocumentElement["X_Filter"] != null)
                        {
                            xml.DocumentElement["X_Filter"].InnerXml = xdoc.Root.ToString(SaveOptions.DisableFormatting).Replace("<root>", "").Replace("</root>", "");
                            XDocument xFilter = XDocument.Parse($"<root>{xml.GetElementsByTagName("X_Filter")[0]?.InnerXml}</root>");
                            reportType = xFilter.Root.Element("TemplateName")?.Value;
                            if (strModuleName.ToUpper() == "LETTERGENERATION" && strFunctionName.ToUpper() == "TYPSTMAIL" && strReportDisplay.ToUpper() == "D")
                            {
                                pdfType = xFilter.Root.Element("TemplateName")?.Value;
                            }
                            else if (strFunctionName.ToUpper() == "GENERATEPDF")
                            {
                                pdfType = xFilter.Root.Element("PdfReportName")?.Value;
                            }
                        }

                        strDebugFlag = _tradeWebRepository.GetDebugFlag(strModuleName, strFunctionName);
                        if (strModuleName.ToUpper() == "MARGINPLEDGENSDL" && strFunctionName.ToUpper() == "GETSIGNATURE")
                        {
                            strDebugFlag = "Y";
                        }
                        string strXML = objUtility.SerializeToXml(xml, omitXmlDeclaration: true, indent: false, encoding: "utf-16");
                        if (strDebugFlag == "Y")
                        {
                            using (SqlConnection conn = new SqlConnection(connetionString))
                            {
                                conn.Open();
                                logSrNo = objUtility.InsertLog(0, "TradeWeb", "", strXML, "", "", userId, conn);
                            }
                        }

                        if (strModuleName.ToUpper() == "MARGINPLEDGENSDL" && strFunctionName.ToUpper() == "GETSIGNATURE")
                        {
                            var dataXML = xml.GetElementsByTagName("X_Data")[0]?.InnerXml;
                            XDocument xdocData = XDocument.Parse($"<root>{dataXML}</root>");
                            string reqJson = xdocData.Root.Element("RequestJson")?.Value;

                            string strETokenURL = objUtility.GetWebParameter("NSDLSIGURL");
                            string strEToken = objUtility.GetWebParameter("NSDLETOKEN");
                            string encryptedJson = objUtility.Encrypt(reqJson);

                            string[] strArrEtoken = strEToken.Trim().Split('~');

                            if (strArrEtoken.Length < 4)
                            {
                                logRespMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "Invalid Etoken parameter values", data = null, datarows = null });
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "Invalid Etoken parameter values", data = null, datarows = null }, Formatting.Indented));
                            }

                            string strTokenType = strArrEtoken[1].Trim();
                            string strTokenSrNo = strArrEtoken[2].Trim();
                            string strTokenPwd = strArrEtoken[3].Trim();

                            string soapRequest = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                            <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                                           xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
                                           xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                              <soap:Body>
                                <Getsignature xmlns=""http://tempuri.org/"">
                                  <strmessage>{SecurityElement.Escape(encryptedJson)}</strmessage>
                                  <strtoken>{strTokenType}</strtoken>
                                  <strcerno>{strTokenSrNo}</strcerno>
                                  <strpass>{strTokenPwd}</strpass>
                                </Getsignature>
                              </soap:Body>
                            </soap:Envelope>";

                            using var client = new HttpClient();

                            var content = new StringContent(soapRequest, Encoding.UTF8);
                            content.Headers.ContentType =
                                new System.Net.Http.Headers.MediaTypeHeaderValue("text/xml") { CharSet = "utf-8" };

                            content.Headers.Add(
                                "SOAPAction",
                                "\"http://tempuri.org/IGetSignatureService/Getsignature\""
                            );

                            try
                            {
                                var response = await client.PostAsync(
                                    strETokenURL + "/GetSignatureService",
                                    content
                                );

                                var responseXml = await response.Content.ReadAsStringAsync();

                                if (!response.IsSuccessStatusCode)
                                {
                                    logRespMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "SignatureAPI Error - Status Code : " + (int)response.StatusCode + " Response : " + responseXml, data = null, datarows = null });
                                    return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "SignatureAPI Error - Status Code : " + (int)response.StatusCode + " Response : " + responseXml, data = null, datarows = null }, Formatting.Indented));
                                }

                                string signature = objUtility.ExtractSignature(responseXml).Replace("\r", "").Replace("\n", "");

                                DataSet dsRes = new DataSet();
                                dsRes.Tables.Add("rs0");
                                dsRes.Tables[0].Columns.Add("Signature", typeof(string));
                                dsRes.Tables[0].Rows.Add(signature);
                                logRespMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "", data = dsRes, datarows = new List<string>() { "1" } });
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "", data = dsRes, datarows = new List<string>() { "1" } }, Formatting.Indented));
                            }
                            catch (HttpRequestException ex)
                            {
                                logErrorMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "SignatureAPI Error - Service Unreachable : " + ex.Message, data = null, datarows = null });
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "SignatureAPI Error - Service Unreachable : " + ex.Message, data = null, datarows = null }, Formatting.Indented));
                            }
                            catch (Exception ex)
                            {
                                logErrorMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "SignatureAPI Error - Unexpected error : " + ex.Message, data = null, datarows = null });
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "SignatureAPI Error - Unexpected error : " + ex.Message, data = null, datarows = null }, Formatting.Indented));
                            }
                        }
                        else if (strModuleName.ToUpper() == "MARGINPLEDGECDSL" && strFunctionName.ToUpper() == "ENCRYPTDATA")
                        {
                            var dataXML = xml.GetElementsByTagName("X_Data")[0]?.InnerXml;
                            XDocument xdocData = XDocument.Parse($"<root>{dataXML}</root>");
                            string reqJson = xdocData.Root.Element("RequestJson")?.Value;

                            string aesKey = "";
                            string strsql = "SELECT RequestJson FROM tbl_VendorAPISetting(NOLOCK) WHERE APIVendorName = 'CDSL' AND APINAME = 'PLEDGEENQ'";
                            DataTable dtTemp = objUtility.OpenDataTable(strsql);
                            if (dtTemp.Rows.Count > 0)
                            {
                                string value = dtTemp.Rows[0][0].ToString().Trim();
                                if (!string.IsNullOrEmpty(value) && value.Contains("|"))
                                {
                                    string[] parts = value.Split('|');
                                    if (parts.Length > 1)
                                    {
                                        aesKey = parts[1];
                                    }
                                }
                            }
                            
                            if (string.IsNullOrEmpty(aesKey))
                            {
                                logErrorMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "CDSL MP Encrypt Data Error - AES key cannot be blank.", data = null, datarows = null });
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "CDSL MP Encrypt Data Error - AES key cannot be blank.", data = null, datarows = null }, Formatting.Indented));
                            }

                            //string strJson = JsonConvert.SerializeObject(reqJson);

                            //var finljson = strJson.Replace(@"\", "");
                            //finljson = finljson.Replace("\"[{", "[{");
                            //finljson = finljson.Replace("}]\"", "}]");

                            var EncryptedData = objUtility.EDIS_Encrypt(reqJson, aesKey);
                            DataSet dsRes = new DataSet();
                            dsRes.Tables.Add("rs0");
                            dsRes.Tables[0].Columns.Add("EncryptedData", typeof(string));
                            dsRes.Tables[0].Rows.Add(EncryptedData);
                            logRespMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "", data = dsRes, datarows = new List<string>() { "1" } });
                            return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "", data = dsRes, datarows = new List<string>() { "1" } }, Formatting.Indented));
                        }

                        if (string.Equals(strFunctionName.Trim(), "SendOTP", StringComparison.OrdinalIgnoreCase))
                        {
                            var dataXML = xml.GetElementsByTagName("X_Data")[0]?.InnerXml;
                            XDocument xdocData = XDocument.Parse($"<root>{dataXML}</root>");
                            string ClientCd = xdocData.Root.Element("ClientCode")?.Value;
                            string OTPType = xdocData.Root.Element("Type")?.Value;

                            DataTable dtResponse = await objUtility.TradeMobileSendOTP(ClientCd, "TradeWeb", OTPType, "Admin");
                            string strStatus = dtResponse.Rows[0]["Status"].ToString();
                            string strMessage = dtResponse.Rows[0]["Message"].ToString();

                            logRespMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = (strStatus == "Y"), message = strMessage });
                            return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = (strStatus == "Y"), message = strMessage }, Formatting.Indented));
                        }
                        else if (string.Equals(strFunctionName.Trim(), "VerifyOTP", StringComparison.OrdinalIgnoreCase))
                        {
                            var dataXML = xml.GetElementsByTagName("X_Data")[0]?.InnerXml;
                            XDocument xdocData = XDocument.Parse($"<root>{dataXML}</root>");
                            string ClientCd = xdocData.Root.Element("ClientCode")?.Value;
                            string OTPType = xdocData.Root.Element("Type")?.Value;
                            string OTP = xdocData.Root.Element("OTP")?.Value;

                            if (string.IsNullOrEmpty(ClientCd))
                            {
                                logRespMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "ClientCode should not be blank.", data = null, datarows = null });
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "ClientCode should not be blank.", data = null, datarows = null }, Formatting.Indented));
                            }

                            if (string.IsNullOrEmpty(OTPType))
                            {
                                logRespMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "OTP Type should not be blank.", data = null, datarows = null });
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "OTP Type should not be blank.", data = null, datarows = null }, Formatting.Indented));
                            }

                            if (string.IsNullOrEmpty(OTP))
                            {
                                logRespMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "OTP should not be blank.", data = null, datarows = null });
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "OTP should not be blank.", data = null, datarows = null }, Formatting.Indented));
                            }
                            ClientCd = objUtility.mfnReplaceForSQLInjection(ClientCd);
                            string checkOTP = "", strIdentity = "";
                            string Strsql = "select OTP_Identity,OTP_OTP,OTP_ValidTillDate,OTP_ValidTillTime from OTP_Master where OTP_ClientCode='" + ClientCd + "' and OTP_Status='P' and OTP_Product='TradeWeb' and OTP_Type = '" + OTPType + "' order by OTP_SentDate desc,OTP_SentTime desc";
                            DataTable dt = objUtility.OpenDataTable(Strsql);
                            if (dt.Rows.Count > 0)
                            {
                                checkOTP = dt.Rows[0]["OTP_OTP"].ToString().Trim();
                                strIdentity = dt.Rows[0]["OTP_Identity"].ToString().Trim();
                            }
                            if (!string.IsNullOrWhiteSpace(OTP) && !string.IsNullOrWhiteSpace(checkOTP) && OTP == checkOTP)
                            {
                                DateTime dtNow = objUtility.GetSqlCurrentDateTime();
                                DateTime dt3 = objUtility.stod(dt.Rows[0]["OTP_ValidTillDate"].ToString());
                                TimeSpan time = TimeSpan.Parse(dt.Rows[0]["OTP_ValidTillTime"].ToString());
                                DateTime dtExpiry = dt3 + time;
                                if (dtNow > dtExpiry)
                                {
                                    Strsql = "update OTP_Master set OTP_Status='E' where OTP_Identity='" + strIdentity + "'";
                                    objUtility.ExecuteSQL(Strsql);
                                    logRespMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "OTP has been expired.", data = null, datarows = null });
                                    return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "OTP has been expired.", data = null, datarows = null }, Formatting.Indented));
                                }

                                Strsql = " Update OTP_Master set OTP_Status='M' where OTP_Identity='" + strIdentity + "'";
                                objUtility.ExecuteSQL(Strsql);

                                logRespMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "OTP Matched", data = null, datarows = null });
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "OTP Matched", data = null, datarows = null }, Formatting.Indented));
                            }
                            else
                            {
                                logRespMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "OTP Mismached", data = null, datarows = null });
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "OTP Mismached", data = null, datarows = null }, Formatting.Indented));
                            }
                        }

                        if (strFunctionName.ToUpper() == "GETAGREEMENT") //// For Agreement pdf
                        {
                            try
                            {
                                DataSet dsAggr = objUtility.OpenDataSet("SELECT ISNULL(SP_SYSVALUE,'') AS AgreementFile FROM WEBPARAMETER (NOLOCK) WHERE SP_PARMCD = 'KYCPDF'");
                                if (dsAggr?.Tables[0]?.Rows.Count > 0)
                                {
                                    string kycPdfPath = dsAggr.Tables[0].Rows[0][0].ToString();
                                    if (!string.IsNullOrEmpty(kycPdfPath))
                                    {
                                        string clintCode = "";
                                        try
                                        {
                                            var dataXML = xml.GetElementsByTagName("X_Data")[0]?.InnerXml;
                                            XDocument xdocData = XDocument.Parse($"<root>{dataXML}</root>");
                                            string uid = xdocData.Root.Element("ClientCode")?.Value;

                                            string encKey = _configuration["epwdKey"];
                                            encKey = objUtility.Decrypt(encKey);
                                            var keybytes = Encoding.UTF8.GetBytes(encKey);
                                            var iv = Encoding.UTF8.GetBytes(encKey);
                                            var encrypted = Convert.FromBase64String(uid);
                                            clintCode = DecryptStringFromBytes(encrypted, keybytes, iv);
                                        }
                                        catch
                                        { }
                                        if (string.IsNullOrEmpty(clintCode))
                                            return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "UserId Not Found", data = null, datarows = null }, Formatting.Indented));

                                        string path = Path.Combine(kycPdfPath, clintCode) + ".pdf";
                                        var docBytes = System.IO.File.ReadAllBytes(path);
                                        //string docBase64 = "data:application/pdf;base64," + Convert.ToBase64String(docBytes);
                                        string docBase64 = Convert.ToBase64String(docBytes);

                                        if (docBase64 != null)
                                        {
                                            dsAggr.Tables[0].TableName = "rs0";
                                            dsAggr.Tables[0].Rows[0][0] = docBase64;
                                            return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "success", data = dsAggr, datarows = null }, Formatting.Indented));
                                        }
                                        else
                                        {
                                            return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "No Record Found", data = null, datarows = null }, Formatting.Indented));
                                        }
                                    }
                                }
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "File path not found!", data = null, datarows = null }, Formatting.Indented));
                            }
                            catch (Exception ex)
                            {
                                return BadRequest(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = ex.Message.ToString(), data = null, datarows = null }, Formatting.Indented));
                            }
                        }

                        var getData = _tradeWebRepository.CommonAPIrepoCall(strModuleName, strFunctionName, strXML);

                        if (getData?.Data?.Tables != null && getData.Data.Tables.Count > 0)
                        {
                            var dataSet = getData.Data;
                            if (dataSet.Tables[0].Rows.Count > 0)
                            {
                                if (dataSet.Tables[0].Columns.Contains("ErrorFlag"))
                                {
                                    var errorFlag = dataSet.Tables[0].Rows[0]["ErrorFlag"].ToString();
                                    if (errorFlag == "E")
                                    {
                                        return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "", data = dataSet, datarows = null }, Formatting.Indented));
                                    }
                                }
                            }
                        }

                        var typstResponse = (dynamic)null;
                        if (getJson == "Y")
                        {
                            return Ok(JsonConvert.SerializeObject(getData.Data, Formatting.Indented));
                        }
                        if (strReportDisplay == "D")
                        {
                            typstResponse = _tradeWebRepository.CompileTypst(strModuleName, strFunctionName, getData.Data, pdfType);
                            logRespMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "" });

                            return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "", data = typstResponse, datarows = null }, Formatting.Indented));
                        }
                        else if (strReportDisplay == "M" || strReportDisplay == "P")//// For creating Quest pdf and add in table for send auto mail // P for download pdf
                        {
                            typstResponse = await _tradeWebRepository.GenerateQuestPDF(strModuleName, strFunctionName, getData.Data, strReportDisplay, userId);
                            if (strReportDisplay == "M")
                            {
                                if (typstResponse != null)
                                {
                                    string reportPDFType = "";
                                    DataSet dsPDF = getData.Data as DataSet;
                                    if (dsPDF != null && dsPDF.Tables.Count > 0 && dsPDF.Tables[0].Columns.Contains("DocumentType") && dsPDF.Tables[0].Rows.Count > 0)
                                    {
                                        reportPDFType = Convert.ToString(dsPDF.Tables[0].Rows[0]["DocumentType"]);
                                    }

                                    DataColumn col = new DataColumn("MessageText", typeof(string));
                                    col.DefaultValue = "";
                                    typstResponse.Tables[0].Columns.Add(col);

                                    foreach (DataRow dr in typstResponse.Tables[0].Rows)
                                    {
                                        if (Convert.ToString(dr["MessageType"]) == "SUCCESS")
                                        {
                                            dr["MessageText"] = "Letter is Generated For " + (reportPDFType == "AlertForDormant" ? "Dormant Client (No Trading Since)" : (reportPDFType == "AlertForIncomeUpdation" ? "Income Updation (Date Prior To)" : ""));
                                        }
                                    }
                                }
                            }
                            return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "", data = typstResponse, datarows = null }, Formatting.Indented));
                        }
                        else if (strReportDisplay == "S")////X S
                        {
                            typstResponse = _tradeWebRepository.CompileTypstSeperateFiles(strModuleName, strFunctionName, getData.Data, pdfType);
                            return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "", data = typstResponse, datarows = null }, Formatting.Indented));
                        }
                        else if (strReportDisplay == "Z")  /// For single and multiple zip creation
                        {
                            typstResponse = _tradeWebRepository.DownloadMultiplePdfZip(strModuleName, strFunctionName, getData.Data, pdfType);
                            return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "", data = typstResponse, datarows = null }, Formatting.Indented));
                        }
                        else if (strReportDisplay == "X")  /// For Excel Export
                        {
                            typstResponse = _tradeWebRepository.CompileExcelExport(strModuleName, strFunctionName, getData.Data);
                            return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "", data = typstResponse, datarows = null }, Formatting.Indented));
                        }
                        else if (strFunctionName == "GeneratePDF")//// For creating Quest pdf and add in table for send auto mail // P for download pdf
                        {
                            typstResponse = await _tradeWebRepository.GenerateQuestPDFusingDynamicJson(pdfType, getData.Data.Tables[0], userId);

                            return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "", data = typstResponse, datarows = null }, Formatting.Indented));
                        }
                        if (getData != null)
                        {
                            logRespMessage = "Success";
                            //return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "", data = getData.Data, datarows = getData.DataRows }, Formatting.Indented));
                            if (getData.Data != null && getData.Data.Tables.Count > 0 && getData.Data.Tables[0].Columns.Count > 0 && getData.Data.Tables[0].Columns[0].ColumnName == "Column1")
                            {
                                object column1Json;
                                var column1Value = getData.Data.Tables[0].Rows[0]["Column1"].ToString();
                                try
                                {
                                    column1Json = JsonConvert.DeserializeObject(column1Value);
                                }
                                catch (JsonException)
                                {
                                    // Handle invalid JSON in 'Column1'
                                    //return BadRequest(new
                                    //{
                                    //    success = false,
                                    //    message = $"Invalid JSON in Column1: {ex.Message}",
                                    //    data = (object)null,
                                    //    datarows = getData.DataRows
                                    //});
                                    logRespMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = getData.Success, message = getData.Message, data = getData.Data, datarows = getData.DataRows });
                                    return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = getData.Success, message = getData.Message, data = getData.Data, datarows = getData.DataRows }, Formatting.Indented));
                                }
                                //var result = new
                                //{
                                //    rs0 = column1Json
                                //};
                                var result = new Dictionary<string, object>
                                {
                                    { "rs0", column1Json }
                                };
                                if (getData.Data.Tables.Count > 1)
                                {
                                    for (int i = 1; i < getData.Data.Tables.Count; i++)
                                    {
                                        DataTable dt = getData.Data.Tables[i];
                                        var tableJson = dt.AsEnumerable()
                                                          .Select(row => dt.Columns.Cast<DataColumn>()
                                                              .ToDictionary(col => col.ColumnName, col => row[col]));
                                        result[$"rs{i}"] = tableJson;
                                    }
                                }
                                if (column1Json is JObject jObj && jObj["Flag"]?.ToString() == "S" && strFunctionName.ToUpper() == "CHANGEPASSWORD")
                                {
                                    _tokenStore.RemoveToken(userId);
                                }
                                logRespMessage = JsonConvert.SerializeObject(new { success = getData.Success, message = getData.Message, data = result, datarows = getData.DataRows });
                                return Ok(JsonConvert.SerializeObject(new { success = getData.Success, message = getData.Message, data = result, datarows = getData.DataRows }, Formatting.Indented));
                            }
                            else
                            {
                                //if (getData.Data.Tables.Count > 0)
                                //{
                                //    return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "", data = getData.Data, datarows = getData.DataRows }, Formatting.Indented));
                                //}
                                //else
                                //{
                                //    logRespMessage = "No record found";
                                //    return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "No records found", data = getData.Data, datarows = getData.DataRows }, Formatting.Indented));
                                //}
                                logRespMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = getData.Success, message = getData.Message, data = getData.Data, datarows = getData.DataRows });
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = getData.Success, message = getData.Message, data = getData.Data, datarows = getData.DataRows }, Formatting.Indented));
                            }
                        }
                        else
                        {
                            logRespMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "No records found" });
                            return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "No records found", data = null, datarows = null }, Formatting.Indented));
                        }
                    }
                }
                catch (Exception ex)
                {
                    logErrorMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = ex.Message.ToString() });
                    return BadRequest(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = ex.Message.ToString(), data = null, datarows = null }, Formatting.Indented));
                }
                finally
                {
                    if (strDebugFlag != "N")
                    {
                        using (SqlConnection conn = new SqlConnection(connetionString))
                        {
                            conn.Open();
                            if (logSrNo == 0)
                            {
                                logSrNo = objUtility.InsertLog(0, "TradeWeb", "", logReq, logRespMessage, logErrorMessage, "", conn);
                            }
                            else
                            {
                                objUtility.UpdateLog(logSrNo, "", logRespMessage, logErrorMessage, conn);
                            }
                        }
                    }
                }
            }
        }

        #region TradeWeb batch

        // Options that change data, send messages, or generate files. They stay on the
        // single TradeWeb endpoint so a batch is always a set of independent read/lookup
        // queries that are safe to run in parallel.
        private static readonly HashSet<string> BatchBlockedOptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ADD", "EDIT", "DELETE", "CHANGEPASSWORD", "VALIDATEPASSWORD", "SENDOTP", "VERIFYOTP",
            "GETAGREEMENT", "GETSIGNATURE", "ENCRYPTDATA", "GENERATEPDF", "TYPSTMAIL"
        };

        /// <summary>
        /// Runs several TradeWeb requests in one HTTP round trip.
        /// Body (JSON): { "requests": [ { "id": "1", "body": "&lt;dsXml&gt;...&lt;/dsXml&gt;" }, ... ] }
        /// Response:    { "success": true, "results": [ { "id": "1", "status": 200, "body": { ...same JSON as TradeWeb... } } ] }
        /// Each item goes through the same handler as api/Main/TradeWeb, so auth-derived
        /// UserId injection, validation and logging are identical. Items run in parallel,
        /// each in its own DI scope.
        /// </summary>
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin,TradeMobile,ComAPI,CrossNet,EstroNet,CrossWeb,EstroWeb,Branch")]
        [HttpPost, Route("TradeWebBatch")]
        [Consumes("application/json")]
        [TypeFilter(typeof(ValidateUser), Arguments = new object[] { "TradeWeb" })]
        [ServiceFilter(typeof(E2EDecryptFilter))]
        [ServiceFilter(typeof(EncryptResponseAttribute))]
        public async Task<IActionResult> TradeWebBatch()
        {
            int maxItems = _configuration.GetValue<int?>("BatchRequest:MaxItems") ?? 50;
            int maxParallelism = Math.Max(1, _configuration.GetValue<int?>("BatchRequest:MaxParallelism") ?? 6);

            // Read the raw body ourselves: model binding runs before E2EDecryptFilter, so
            // [FromBody] would see the encrypted payload.
            TradeWebBatchRequest batch;
            try
            {
                string bodyStr;
                using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
                {
                    bodyStr = await reader.ReadToEndAsync();
                }
                batch = JsonConvert.DeserializeObject<TradeWebBatchRequest>(bodyStr);
            }
            catch (Exception ex)
            {
                return BadRequest(JsonConvert.SerializeObject(new TradeWebBatchResponse { Success = false, Message = "Invalid batch body: " + ex.Message }));
            }

            if (batch?.Requests == null || batch.Requests.Count == 0)
            {
                return BadRequest(JsonConvert.SerializeObject(new TradeWebBatchResponse { Success = false, Message = "requests must contain at least one item" }));
            }
            if (batch.Requests.Count > maxItems)
            {
                return BadRequest(JsonConvert.SerializeObject(new TradeWebBatchResponse { Success = false, Message = $"A batch may contain at most {maxItems} requests" }));
            }

            // Set once up front so parallel items never write HttpContext.Items concurrently.
            var role = GetToken()?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            if (role == "TradeMobile")
            {
                HttpContext.Items["TradeMobile"] = "Y";
            }

            var scopeFactory = HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>();
            var results = new TradeWebBatchResult[batch.Requests.Count];
            using (var gate = new SemaphoreSlim(maxParallelism))
            {
                var tasks = batch.Requests.Select(async (item, index) =>
                {
                    await gate.WaitAsync();
                    try
                    {
                        results[index] = await Task.Run(() => RunBatchItem(scopeFactory, item, index));
                    }
                    finally
                    {
                        gate.Release();
                    }
                }).ToList();
                await Task.WhenAll(tasks);
            }

            return Ok(JsonConvert.SerializeObject(new TradeWebBatchResponse { Success = true, Message = "", Results = results.ToList() }));
        }

        private async Task<TradeWebBatchResult> RunBatchItem(IServiceScopeFactory scopeFactory, TradeWebBatchItem item, int index)
        {
            string id = string.IsNullOrEmpty(item?.Id) ? index.ToString() : item.Id;
            try
            {
                if (string.IsNullOrWhiteSpace(item?.Body))
                {
                    return BatchError(id, StatusCodes.Status400BadRequest, "body is empty");
                }

                string rejection = GetBatchRejectionReason(item.Body);
                if (rejection != null)
                {
                    return BatchError(id, StatusCodes.Status400BadRequest, rejection);
                }

                // A fresh scope gives each parallel item its own controller, repository and
                // utility instances, since those hold per-call mutable state.
                using (var scope = scopeFactory.CreateScope())
                {
                    var controller = ActivatorUtilities.CreateInstance<MainController>(scope.ServiceProvider);
                    controller.ControllerContext = new ControllerContext { HttpContext = HttpContext };
                    IActionResult actionResult = await controller.ProcessTradeWebXml(item.Body);
                    return ToBatchResult(id, actionResult);
                }
            }
            catch (Exception ex)
            {
                return BatchError(id, StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        private static string GetBatchRejectionReason(string xmlStr)
        {
            try
            {
                var xml = new XmlDocument();
                xml.LoadXml(xmlStr);
                var jUiNode = xml.GetElementsByTagName("J_Ui");
                if (jUiNode == null || jUiNode.Count == 0) return null; // let the normal handler report it

                string jsonString = "{" + jUiNode[0].InnerText.Replace("“", "\"").Replace("”", "\"") + "}";
                var jUi = JsonConvert.DeserializeObject<JUiModel>(jsonString);
                string option = jUi?.Option?.Trim() ?? "";
                if (BatchBlockedOptions.Contains(option))
                {
                    return $"Option '{option}' is not allowed in a batch request; call api/Main/TradeWeb instead";
                }
                if (!string.IsNullOrWhiteSpace(jUi?.ReportDisplay))
                {
                    return "ReportDisplay requests are not allowed in a batch request; call api/Main/TradeWeb instead";
                }
                return null;
            }
            catch
            {
                return null; // malformed XML/JSON: the normal handler returns its usual error
            }
        }

        private static TradeWebBatchResult ToBatchResult(string id, IActionResult actionResult)
        {
            switch (actionResult)
            {
                case ObjectResult objectResult:
                    return new TradeWebBatchResult
                    {
                        Id = id,
                        Status = objectResult.StatusCode ?? StatusCodes.Status200OK,
                        Body = ToJToken(objectResult.Value)
                    };
                case StatusCodeResult statusCodeResult:
                    return new TradeWebBatchResult { Id = id, Status = statusCodeResult.StatusCode, Body = null };
                default:
                    return BatchError(id, StatusCodes.Status500InternalServerError, "Unsupported result type");
            }
        }

        private static JToken ToJToken(object value)
        {
            if (value == null) return null;
            if (value is string s)
            {
                string trimmed = s.TrimStart();
                // Handler output is produced by JsonConvert.SerializeObject, so embed it as-is
                // instead of paying to parse and re-serialize large result sets.
                if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
                {
                    return new JRaw(s);
                }
                return new JValue(s);
            }
            return JToken.FromObject(value);
        }

        private static TradeWebBatchResult BatchError(string id, int status, string message)
        {
            return new TradeWebBatchResult
            {
                Id = id,
                Status = status,
                Body = JObject.FromObject(new { success = false, message, data = (object)null, datarows = (object)null })
            };
        }

        #endregion

        [HttpPost, Route("InitializeLogin")]
        [Consumes("application/xml")]
        [ServiceFilter(typeof(E2EDecryptFilter))]
        [ServiceFilter(typeof(EncryptResponseAttribute))]
        public async Task<IActionResult> InitializeLogin()
        {
            if (ModelState.IsValid)
            {
                long logSrNo = 0;
                string logRespMessage = "", logErrorMessage = "";
                string moduleName = _configuration["IsTradeWeb"] == "O" ? "TradeWeb" : "EstroWeb";
                string connetionString = objUtility.GetConnectionStr();
                try
                {
                    using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
                    {
                        var xmlStr = await reader.ReadToEndAsync();
                        XmlDocument xml = new XmlDocument();
                        xml.LoadXml(xmlStr);

                        ///// *********  For XML object J_Ui *********
                        string jsonString = xml.GetElementsByTagName("J_Ui")[0].InnerText;
                        if (string.IsNullOrWhiteSpace(jsonString))
                        {
                            return BadRequest("J_Ui element is empty");
                        }
                        // Replace smart quotes if any
                        jsonString = "{" + jsonString.Replace("\u201C", "\"").Replace("\u201D", "\"") + "}";
                        JUiModel jUiObject;
                        try
                        {
                            // Deserialize the JSON string into a C# object
                            jUiObject = JsonConvert.DeserializeObject<JUiModel>(jsonString);
                        }
                        catch (JsonException ex)
                        {
                            return BadRequest($"Invalid JSON in J_Ui element: {ex.Message}");
                        }
                        ///// *********  For XML object J_Api *********
                        jsonString = xml.GetElementsByTagName("J_Api")[0].InnerText;
                        if (!string.IsNullOrWhiteSpace(jsonString))
                        {
                            // Replace smart quotes if any
                            jsonString = "{" + jsonString.Replace("\u201C", "\"").Replace("\u201D", "\"") + "}";
                            JApi jApiObject;
                            try
                            {
                                jApiObject = JsonConvert.DeserializeObject<JApi>(jsonString);
                                xml.DocumentElement["J_Api"].InnerText = JsonConvert.SerializeObject(jApiObject).Replace("{", "").Replace("}", "");
                            }
                            catch (JsonException ex)
                            {
                                return BadRequest($"Invalid JSON in J_Api element: {ex.Message}");
                            }
                        }
                        string strModuleName = objUtility.mfnReplaceForSQLInjection(jUiObject.ActionName?.Trim() ?? "");
                        string strFunctionName = objUtility.mfnReplaceForSQLInjection(jUiObject.Option?.Trim() ?? "");
                        string strRequestFrom = objUtility.mfnReplaceForSQLInjection(jUiObject.RequestFrom?.Trim() ?? "");

                        if (strFunctionName == "InitializeLogin")
                        {
                            HttpContext.Items["Option"] = "InitializeLogin";
                        }

                        if (strRequestFrom == "M")
                        {
                            HttpContext.Items["TradeMobile"] = "Y";
                        }

                        string strXML = objUtility.SerializeToXml(xml, omitXmlDeclaration: true, indent: false, encoding: "utf-16");
                        using (Microsoft.Data.SqlClient.SqlConnection conn = new Microsoft.Data.SqlClient.SqlConnection(connetionString))
                        {
                            conn.Open();
                            if (strFunctionName != "InitializeLogin")
                            {
                                logSrNo = objUtility.InsertLog(0, moduleName, "", strXML, "", "", "", conn);
                            }
                        }

                        if (strFunctionName == "Login")
                        {
                            var dataXML = xml.GetElementsByTagName("X_Data")[0]?.InnerXml;
                            XDocument xdoc = XDocument.Parse($"<root>{dataXML}</root>");
                            string userId = xdoc.Root.Element("UserId")?.Value?.Trim();
                            string password = xdoc.Root.Element("Password")?.Value;
                            string epassword = xdoc.Root.Element("EPassword")?.Value;
                            string key = xdoc.Root.Element("Key")?.Value;
                            string loginAs = xdoc.Root.Element("LoginAs")?.Value;
                            string product = xdoc.Root.Element("Product")?.Value;
                            string icpv = xdoc.Root.Element("ICPV")?.Value;
                            string feature = xdoc.Root.Element("Feature")?.Value;

                            string ip = "";
                            string chkParamIP = objUtility.GetSysParmSt("IPBLOCKING", "");

                            if (chkParamIP != "D" && chkParamIP != "")
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress;

                                ip = ipAddress == null
                                    ? ""
                                    : IPAddress.IsLoopback(ipAddress)
                                        ? "127.0.0.1"
                                        : ipAddress.MapToIPv4().ToString();
                            }

                            var response = await _tradeWebRepository.Login(userId, password, epassword, key, loginAs, product, icpv, feature, "", ip);
                            logRespMessage = response;
                            return Ok(response);
                        }
                        else if (strFunctionName == "Refresh")
                        {
                            var dataXML = xml.GetElementsByTagName("X_Data")[0]?.InnerXml;
                            XDocument xdoc = XDocument.Parse($"<root>{dataXML}</root>");
                            string token = xdoc.Root.Element("RefreshToken")?.Value;
                            string storedToken = "";
                            string UserCode = "";
                            bool branch = false;
                            string role = "";
                            DateTime expiry = new DateTime();
                            string strLoginAs = "";
                            //var datetimeExp = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                            var datetimeExp = DateTime.UtcNow;

                            int refreshExpTime = 0;

                            if (objUtility.GetWebParameter("RefTokenExpTm").Trim() != "")
                            {
                                refreshExpTime = Convert.ToInt32(objUtility.GetWebParameter("RefTokenExpTm").Trim());
                            }
                            else
                            {
                                refreshExpTime = Convert.ToInt32(_configuration["refreshTokenExpireTime"]);
                            }

                            string Strsql = "select Id, UserCode, Token, ExpiryDate, UserType, Role from RefreshTokens where Token = '" + token + "' and IsRevoked = 0";
                            DataTable dtTemp = objUtility.OpenDataTable(Strsql);
                            if (dtTemp.Rows.Count > 0)
                            {
                                UserCode = dtTemp.Rows[0]["UserCode"].ToString().Trim();
                                storedToken = dtTemp.Rows[0]["Token"].ToString().Trim();
                                expiry = Convert.ToDateTime(dtTemp.Rows[0]["ExpiryDate"]);
                                branch = dtTemp.Rows[0]["UserType"].ToString().Trim() == "B";
                                role = dtTemp.Rows[0]["Role"].ToString().Trim();
                                strLoginAs = dtTemp.Rows[0]["UserType"].ToString().Trim();

                                Strsql = "update RefreshTokens set IsRevoked = 1 where Id = " + dtTemp.Rows[0]["Id"].ToString().Trim();
                                objUtility.ExecuteSQL(Strsql);
                            }

                            //var currDatetime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                            var currDatetime = DateTime.UtcNow;
                            if (string.IsNullOrWhiteSpace(storedToken) || expiry <= currDatetime)
                            {
                                //return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "", data = dsRes, datarows = new List<string> { dtRes.Rows.Count.ToString() } }, Formatting.Indented));
                                logRespMessage = "Invalid or expired refresh token.";
                                return Unauthorized("Invalid or expired refresh token.");
                            }

                            int expTime;
                            int refExpTime = 0;
                            if (objUtility.GetWebParameter("AccTokenExpTm").Trim() != "")
                            {
                                expTime = Convert.ToInt32(objUtility.GetWebParameter("AccTokenExpTm").Trim());
                            }
                            else
                            {
                                expTime = Convert.ToInt32(_configuration["tokenExpireTime"]);
                            }

                            if (objUtility.GetWebParameter("RefTokenExpTm").Trim() != "")
                            {
                                refExpTime = Convert.ToInt32(objUtility.GetWebParameter("RefTokenExpTm").Trim());
                            }
                            else
                            {
                                refExpTime = Convert.ToInt32(_configuration["refreshTokenExpireTime"]);
                            }

                            DataTable dtRes = new DataTable("rs0");
                            dtRes.Columns.Add("AccessToken", typeof(string));
                            dtRes.Columns.Add("RefreshToken", typeof(string));
                            dtRes.Columns.Add("RefreshTokenExpTime", typeof(string));

                            var guidVal = Guid.NewGuid();
                            if (role == "Admin" && branch == false)
                                objUtility.ExecuteSQL("update Login_Session set ls_token = '" + guidVal + "', ls_logintype = 'C', ls_logintm = '" + DateTime.Now.ToString("HH:mm:ss") + "'  Where ls_code = '" + UserCode + "' And ls_logindt = '" + DateTime.Now.ToString("yyyyMMdd") + "'  And len(ls_token) < 3");

                            string strUserAccess = "";
                            if (branch)
                            {
                                if (role == "Branch")
                                {
                                    strUserAccess = objUtility.GetUserAccess(objUtility.fnFireQueryTradeWeb("User_master", "um_specialrights", "um_user_id", UserCode, true));
                                }
                                else
                                {
                                    strUserAccess = "Branch";
                                }
                            }
                            /*var tokenString = branch == true ? GenerateJSONWebTokenUser(new TradeWebLoginModel { username = UserCode, password = "", role = role, guid = guidVal.ToString(), tokenExpTime = expTime, useraccess = strUserAccess }) : GenerateJSONWebToken(new TradeWebLoginModel { username = UserCode, password = "", role = role, guid = guidVal.ToString(), tokenExpTime = expTime });
                            var refreshToken = objUtility.GenerateRefreshToken(UserCode, strLoginAs, role, refExpTime);*/

                            (string tokenString, string refreshToken) = _tradeWebRepository.RefreshToken(UserCode, role, guidVal.ToString(), expTime, strUserAccess, strLoginAs, refExpTime, branch);

                            //dtRes.Rows.Add(tokenString, refreshToken, datetimeExp.AddMinutes(refreshExpTime).ToString("yyyy/MM/dd HH:mm:ss"));
                            dtRes.Rows.Add(tokenString, refreshToken, datetimeExp.AddMinutes(refreshExpTime).ToString(UtcDateTimeFormat));

                            DataSet dsRes = new DataSet();
                            dsRes.Tables.Add(dtRes);

                            logRespMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "", data = dsRes, datarows = new List<string> { dtRes.Rows.Count.ToString() } });
                            return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "", data = dsRes, datarows = new List<string> { dtRes.Rows.Count.ToString() } }, Formatting.Indented));

                            /*return Ok(new
                            {
                                accessToken = tokenString,
                                refreshToken = refreshToken
                            });*/
                        }
                        else if (strFunctionName == "ForgotPassword")
                        {
                            var dataXML = xml.GetElementsByTagName("X_Data")[0]?.InnerXml;
                            XDocument xdoc = XDocument.Parse($"<root>{dataXML}</root>");
                            string clientCode = xdoc.Root.Element("ClientCode")?.Value;
                            if (string.IsNullOrEmpty(clientCode))
                            {
                                logRespMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "ClientCode should not be blank.", data = null, datarows = null });
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "ClientCode should not be blank.", data = null, datarows = null }, Formatting.Indented));
                            }
                            clientCode = objUtility.mfnReplaceForSQLInjection(clientCode);
                            /*using (Microsoft.Data.SqlClient.SqlConnection conn = new Microsoft.Data.SqlClient.SqlConnection(connetionString))
                            {
                                conn.Open();
                                logSrNo = objUtility.InsertLog(0, moduleName, "", xmlStr, "", "", clientCode, conn);
                            }*/
                            /*int intAttempts = 10;
                            int intMinutes = 15;
                            string strParmOTP = objUtility.GetWebParameter("OTPMAXATMPT");
                            if (!string.IsNullOrWhiteSpace(strParmOTP) && strParmOTP.Contains("~"))
                            {
                                var parts = strParmOTP.Split('~');
                                if (parts.Length == 2)
                                {
                                    int.TryParse(parts[0], out intAttempts);
                                    int.TryParse(parts[1], out intMinutes);
                                }
                            }
                            strsql = "select count(0) from OTP_Master with (NoLock) where OTP_ClientCode='" + clientCode + "' and OTP_Product='" + strModuleName + "' and OTP_Type = 'FP' and CAST(OTP_SentDate AS DATETIME) + CAST(OTP_SentTime AS DATETIME) >= DATEADD(MINUTE, -" + intMinutes + ", GETDATE())";
                            DataTable dtCheck = objUtility.OpenDataTable(strsql);
                            if (dtCheck.Rows.Count > 0)
                            {
                                if (Convert.ToInt32(dtCheck.Rows[0][0]) >= intAttempts)
                                {
                                    return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "Too many Attempts. Try after some time.", data = null, datarows = null }, Formatting.Indented));
                                }
                            }*/
                            string ProductFP =
                                string.Equals(strModuleName, "EstroNet", StringComparison.OrdinalIgnoreCase) ? "EstroWeb" :
                                string.Equals(strModuleName, "CrossNet", StringComparison.OrdinalIgnoreCase) ? "CrossWeb" :
                                strModuleName;

                            DataTable dtResponse = await objUtility.TradeMobileSendOTP(clientCode, ProductFP, "FP", "");
                            string strStatus = dtResponse.Rows[0]["Status"].ToString();
                            string strMessage = dtResponse.Rows[0]["Message"].ToString();
                            if (strStatus == "Y")
                            {
                                int intPwdMaxLength = Convert.ToInt16(objUtility.fnFireQueryTradeWeb("sysobjects a,syscolumns b", "b.Length", "a.id=b.id and a.name='Client_master' and b.name", "cm_pwd", true));
                                double intLastPass = Conversion.Val(objUtility.GetWebParameter("PWDSAMECHK"));
                                int intMinChar = 0;
                                string strAlphaNum = "", addMessage = "";
                                dtResponse.Columns.Add("AdditionalMessage", typeof(string));
                                dtResponse.Columns.Add("IsAlphaNum", typeof(string));
                                dtResponse.Columns.Add("MaxLength", typeof(string));
                                dtResponse.Columns.Add("MinLength", typeof(string));

                                if (strModuleName.Trim().ToUpper() == "TRADEWEB")
                                {
                                    intMinChar = Convert.ToInt16(objUtility.GetSysParmSt("PWDMINCHR", ""));
                                    strAlphaNum = objUtility.GetSysParmSt("PWDALPHANUM", "");
                                    dtResponse.Rows[0]["IsAlphaNum"] = strAlphaNum;
                                }
                                else
                                {
                                    dtResponse.Rows[0]["IsAlphaNum"] = "";
                                }

                                if (strAlphaNum.Trim() == "Y")
                                {
                                    addMessage += "<strong>Your Password must:<ol> <li><strong>Contain at least one letter (A-Z or a-z)</strong></li><li><strong>Contain at least one digit (0-9)</strong></li><li><strong>Contain at least one special character (e.g., !, @, #, $, etc.)</strong></li></strong></ol>";
                                }

                                addMessage += "<strong>Additional Guidelines:</strong><br />";
                                addMessage += "<ol><li>Maximum length: <strong>" + intPwdMaxLength + " characters</strong></li>";
                                dtResponse.Rows[0]["MaxLength"] = intPwdMaxLength;
                                if (intMinChar > 0)
                                {
                                    addMessage += "<li>Minimum length: <strong>" + intMinChar + " characters</strong></li>";
                                    dtResponse.Rows[0]["MinLength"] = intMinChar;
                                }
                                else
                                {
                                    dtResponse.Rows[0]["MinLength"] = "";
                                }
                                if (intLastPass > 0)
                                {
                                    addMessage += "<li><strong>You cannot reuse your last " + intLastPass + " passwords.</strong></li>";
                                }
                                addMessage += "</ol>";
                                dtResponse.Rows[0]["AdditionalMessage"] = addMessage;
                            }
                            dtResponse.Columns.Remove("Status");
                            dtResponse.AcceptChanges();
                            System.Data.DataSet dsRtn = new System.Data.DataSet();
                            dsRtn.Tables.Add(dtResponse);
                            dsRtn.Tables[0].TableName = "rs0";
                            logRespMessage = strMessage;

                            if (strStatus == "Y")
                            {
                                logRespMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "", data = dsRtn, datarows = null });
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "", data = dsRtn, datarows = null }, Formatting.Indented));
                            }
                            else
                            {
                                logRespMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = strMessage, data = null, datarows = null });
                                if (!string.IsNullOrWhiteSpace(objUtility.GetWebParameter("GENERICLOGINMSG")))
                                {
                                    strMessage = objUtility.GetWebParameter("GENERICLOGINMSG");
                                }
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = strMessage, data = null, datarows = null }, Formatting.Indented));
                            }
                        }
                        else if (strFunctionName == "ChangePassword")
                        {
                            var dataXML = xml.GetElementsByTagName("X_Data")[0]?.InnerXml;
                            XDocument xdoc = XDocument.Parse($"<root>{dataXML}</root>");
                            string clientCode = xdoc.Root.Element("ClientCode")?.Value;
                            string newPassword = xdoc.Root.Element("NewPassword")?.Value;
                            string otp = xdoc.Root.Element("otp")?.Value;
                            if (string.IsNullOrEmpty(clientCode))
                            {
                                logRespMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "ClientCode should not be blank.", data = null, datarows = null });
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "ClientCode should not be blank.", data = null, datarows = null }, Formatting.Indented));
                            }
                            else if (string.IsNullOrEmpty(newPassword))
                            {
                                logRespMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "New Password should not be blank.", data = null, datarows = null });
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "New Password should not be blank.", data = null, datarows = null }, Formatting.Indented));
                            }
                            clientCode = objUtility.mfnReplaceForSQLInjection(clientCode);
                            newPassword = objUtility.mfnReplaceForSQLInjection(newPassword);
                            string checkOTP = "", strIdentity = "", oldPassword = "", companyCode = "";
                            string ProductFP =
                                string.Equals(strModuleName, "EstroNet", StringComparison.OrdinalIgnoreCase) ? "EstroWeb" :
                                string.Equals(strModuleName, "CrossNet", StringComparison.OrdinalIgnoreCase) ? "CrossWeb" :
                                strModuleName;
                            string Strsql = "select OTP_Identity,OTP_OTP,OTP_ValidTillDate,OTP_ValidTillTime from OTP_Master where OTP_ClientCode='" + clientCode + "' and OTP_Status='P' and OTP_Product='" + ProductFP + "' and OTP_Type = 'FP' order by OTP_SentDate desc,OTP_SentTime desc";
                            DataTable dt = objUtility.OpenDataTable(Strsql);
                            if (dt.Rows.Count > 0)
                            {
                                checkOTP = dt.Rows[0]["OTP_OTP"].ToString().Trim();
                                strIdentity = dt.Rows[0]["OTP_Identity"].ToString().Trim();
                            }
                            if (!string.IsNullOrWhiteSpace(otp) && !string.IsNullOrWhiteSpace(checkOTP) && otp == checkOTP)
                            {
                                DateTime dtNow = objUtility.GetSqlCurrentDateTime();
                                DateTime dt3 = objUtility.stod(dt.Rows[0]["OTP_ValidTillDate"].ToString());
                                TimeSpan time = TimeSpan.Parse(dt.Rows[0]["OTP_ValidTillTime"].ToString());
                                DateTime dtExpiry = dt3 + time;
                                if (dtNow > dtExpiry)
                                {
                                    Strsql = "update OTP_Master set OTP_Status='E' where OTP_Identity='" + strIdentity + "'";
                                    objUtility.ExecuteSQL(Strsql);
                                    logRespMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "OTP has been expired.", data = null, datarows = null });
                                    return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "OTP has been expired.", data = null, datarows = null }, Formatting.Indented));
                                }
                                if (strModuleName.Trim().ToUpper() == "CROSSWEB" || strModuleName.Trim().ToUpper() == "ESTROWEB")
                                {
                                    Strsql = "select cm_pwd, cm_companycode  from Client_Master where cm_cd = '" + clientCode + "' ";
                                    DataTable dtOldPwd = objUtility.OpenDataTable(Strsql);
                                    if (dtOldPwd.Rows.Count > 0)
                                    {
                                        oldPassword = dtOldPwd.Rows[0]["cm_pwd"].ToString().Trim();
                                        companyCode = dtOldPwd.Rows[0]["cm_companycode"].ToString().Trim();
                                    }
                                }
                                else
                                {
                                    Strsql = "select cm_pwd  from Client_Master where cm_cd = '" + clientCode + "' ";
                                    DataTable dtOldPwd = objUtility.OpenDataTable(Strsql);
                                    if (dtOldPwd.Rows.Count > 0)
                                    {
                                        oldPassword = dtOldPwd.Rows[0]["cm_pwd"].ToString().Trim();
                                    }
                                }
                            }
                            else
                            {
                                logRespMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "OTP Mismached", data = null, datarows = null });
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "OTP Mismached", data = null, datarows = null }, Formatting.Indented));
                            }
                            /////// **** Check password validation and policies ****
                            string pwdCondition = objUtility.CheckPasswordCondition(newPassword, companyCode, clientCode);
                            if (!string.IsNullOrEmpty(pwdCondition))
                            {
                                logRespMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = pwdCondition, data = null, datarows = null });
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = pwdCondition, data = null, datarows = null }, Formatting.Indented));
                            }
                            if (objUtility.GetWebParameter("TWEBENCPWD") == "Y")
                            {
                                oldPassword = objUtility.Decrypt(oldPassword);
                            }
                            newPassword = objUtility.mfnReplaceForSQLInjection(newPassword);
                            /*if (objUtility.GetWebParameter("TWEBENCPWD") == "Y")
                            {
                                newPassword = objUtility.Encrypt(newPassword);
                            }*/
                            xdoc.Root.Element("NewPassword").Value = newPassword;
                            xdoc.Root.Add(new XElement("OldPassword", oldPassword));
                            xdoc.Root.Add(new XElement("CompanyCode", companyCode));
                            xml.DocumentElement["X_Data"].InnerXml = xdoc.Root.ToString(SaveOptions.DisableFormatting).Replace("<root>", "").Replace("</root>", "");
                            string strXMLCP = objUtility.SerializeToXml(xml, omitXmlDeclaration: true, indent: false, encoding: "utf-16");
                            using (Microsoft.Data.SqlClient.SqlConnection conn = new Microsoft.Data.SqlClient.SqlConnection(connetionString))
                            {
                                conn.Open();
                                logSrNo = objUtility.InsertLog(0, moduleName, "", strXMLCP, "", "", clientCode, conn);
                            }
                            var getDataCP = _tradeWebRepository.CommonAPIrepoCall(strModuleName, strFunctionName, strXMLCP);
                            if (getDataCP != null)
                            {
                                if (getDataCP.Data?.Tables[0]?.Rows.Count > 0)
                                {
                                    string jsonStr = getDataCP.Data.Tables[0].Rows[0]["Column1"]?.ToString();
                                    string flag = null;
                                    string message = null;

                                    if (!string.IsNullOrWhiteSpace(jsonStr))
                                    {
                                        try
                                        {
                                            JObject obj = JObject.Parse(jsonStr);

                                            flag = obj["Flag"]?.ToString();
                                            message = obj["Message"]?.ToString();
                                        }
                                        catch (JsonReaderException)
                                        {
                                            flag = "";
                                            message = "";
                                        }
                                    }
                                    if (flag == "S")
                                    {
                                        logRespMessage = "Password changed successfully!";
                                        Strsql = " Update OTP_Master set OTP_Status='M' where OTP_Identity='" + strIdentity + "'";
                                        objUtility.ExecuteSQL(Strsql);
                                        objUtility.AfterChangePasswordTblEntry(newPassword, companyCode, clientCode);

                                        object column1Json;
                                        var column1Value = getDataCP.Data.Tables[0].Rows[0]["Column1"].ToString();
                                        try
                                        {
                                            column1Json = JsonConvert.DeserializeObject(column1Value);
                                        }
                                        catch (JsonException)
                                        {
                                            logRespMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = getDataCP.Success, message = getDataCP.Message, data = getDataCP.Data, datarows = getDataCP.DataRows });
                                            // Handle invalid JSON in 'Column1'
                                            return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = getDataCP.Success, message = getDataCP.Message, data = getDataCP.Data, datarows = getDataCP.DataRows }, Formatting.Indented));
                                        }

                                        var result = new Dictionary<string, object>
                                        {
                                            { "rs0", column1Json }
                                        };

                                        logRespMessage = JsonConvert.SerializeObject(new { success = getDataCP.Success, message = getDataCP.Message, data = result, datarows = getDataCP.DataRows });
                                        //return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "", data = result, datarows = getDataCP.DataRows }, Formatting.Indented));
                                        return Ok(JsonConvert.SerializeObject(new { success = getDataCP.Success, message = getDataCP.Message, data = result, datarows = getDataCP.DataRows }, Formatting.Indented));
                                    }
                                    else
                                    {
                                        logRespMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = message, data = null, datarows = null });
                                        return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = message, data = null, datarows = null }, Formatting.Indented));
                                    }
                                }
                                logRespMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "No records found.", data = null, datarows = null });
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "No records found.", data = null, datarows = null }, Formatting.Indented));
                            }
                            else
                            {
                                logRespMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "No records found", data = null, datarows = null });
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "No records found", data = null, datarows = null }, Formatting.Indented));
                            }
                        }
                        else if (strFunctionName == "VerifyBiometric")
                        {
                            var tokenS = GetToken();
                            var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                            var role = tokenS.Claims.First(claim => claim.Type == ClaimTypes.Role)?.Value;

                            var dataXML = xml.GetElementsByTagName("X_Data")[0]?.InnerXml;
                            XDocument xdocData = XDocument.Parse($"<root>{dataXML}</root>");
                            string PublicKey = "";
                            string Payload = xdocData.Root.Element("Payload")?.Value;
                            string Signature = xdocData.Root.Element("Signature")?.Value;

                            string strXML1 = "<dsXml><J_Ui>\"ActionName\":\"TradeWeb\", \"Option\":\"RegisterPublicKey\",\"Level\":1, \"RequestFrom\":\"M\"</J_Ui><Sql/><X_Filter><PublicKey></PublicKey></X_Filter><X_GFilter/><J_Api>\"UserId\":\"" + userId + "\"</J_Api></dsXml>";
                            var getData1 = _tradeWebRepository.CommonAPIrepoCall(strModuleName, "RegisterPublicKey", strXML1);

                            if (getData1 != null)
                            {
                                if (getData1.Data.Tables[0].Rows.Count > 0)
                                {
                                    if (getData1.Data.Tables[0].Rows[0]["Flag"].ToString().Trim() == "E")
                                    {
                                        PublicKey = getData1.Data.Tables[0].Rows[0]["PublicKey"].ToString().Trim();
                                    }
                                }
                            }

                            if (PublicKey == "")
                            {
                                logRespMessage = returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Invalid Key", returnDt, "");
                                return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Invalid Key", returnDt, ""));
                            }

                            bool isValid = objUtility.VerifySignature(Payload, PublicKey, Signature);
                            //string tokenString = "";
                            if (isValid)
                            {
                                #region old code
                                /*Guid guidVal = Guid.NewGuid();
                                tokenString = GenerateJSONWebToken(new TradeWebLoginModel { username = userId, password = "", role = role, guid = guidVal.ToString() });
                                int expTime = Convert.ToInt32(_configuration["tokenExpireTime"]);
                                var datetimeExp = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                                var userList = _tradeWebRepository.UserDetails(userId, "", true, strModuleName, role);

                                tokenResponse result = new tokenResponse();
                                result.status = true;
                                result.message = "success";
                                result.status_code = (int)HttpStatusCode.OK;
                                result.token = tokenString;
                                result.tokenExpireTime = datetimeExp.AddMinutes(expTime).ToString("yyyy/MM/dd HH:mm:ss");
                                result.data = userList.data;
                                var jsonData = JsonConvert.SerializeObject(result, Formatting.Indented);*/
                                #endregion

                                var jsonData = _tradeWebRepository.VerifyBiometric(userId, "", role, strModuleName);
                                logRespMessage = jsonData;
                                return Ok(jsonData);
                            }
                            else
                            {
                                logRespMessage = returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "This account has been registered from a different device", returnDt, "");
                                return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "This account has been registered from a different device", returnDt, ""));
                            }
                        }
                        else if (strFunctionName == "Verify2FA")
                        {
                            try
                            {
                                var dataXML = xml.GetElementsByTagName("X_Data")[0]?.InnerXml;
                                XDocument xdoc = XDocument.Parse($"<root>{dataXML}</root>");
                                string otp = objUtility.mfnReplaceForSQLInjection(xdoc.Root.Element("OTP")?.Value ?? "");

                                var authHeader = Request.Headers["Authorization"].FirstOrDefault();

                                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                                {
                                    logRespMessage = returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Token is missing or invalid", returnDt, "");
                                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Token is missing or invalid", returnDt, ""));
                                }

                                var tokenS = GetToken();
                                var role = tokenS.Claims.First(claim => claim.Type == ClaimTypes.Role)?.Value;

                                if (tokenS.ValidTo < DateTime.UtcNow)
                                {
                                    logRespMessage = "Token has expired";
                                    return Unauthorized("Token has expired");
                                }

                                if (role != "TradeWeb2FA")
                                {
                                    logRespMessage = "Invalid Token Role";
                                    return StatusCode(403, "");
                                }

                                var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                                var loginAs = tokenS.Claims.First(claim => claim.Type == "loginas").Value;
                                var product = tokenS.Claims.First(claim => claim.Type == "product").Value;
                                var otpProduct = tokenS.Claims.First(claim => claim.Type == "otpproduct").Value;
                                //var role = tokenS.Claims.First(claim => claim.Type == "loginrole").Value;
                                var branch = tokenS.Claims.First(claim => claim.Type == "branch").Value == "Y" ? true : false;
                                var userData = JsonConvert.DeserializeObject<DataTable>(tokenS.Claims.First(claim => claim.Type == "userData").Value);
                                var password = "";

                                string checkOTP = "";
                                string strIdentity = "", tempUserId = userId;
                                string[] userStr = userId.Split("|");
                                if (userStr.Length > 1)
                                {
                                    userId = userStr[1];
                                }
                                string Strsql = "select top 1 OTP_Identity,OTP_OTP,OTP_ValidTillDate,OTP_ValidTillTime from OTP_Master with (NoLock) where OTP_ClientCode='" + userId + "' and OTP_Status='P' and OTP_Product='" + otpProduct + "' and OTP_Type = '2FA' order by OTP_Identity desc";
                                DataTable dt = objUtility.OpenDataTable(Strsql);
                                if (dt.Rows.Count > 0)
                                {
                                    checkOTP = dt.Rows[0]["OTP_OTP"].ToString().Trim();
                                    strIdentity = dt.Rows[0]["OTP_Identity"].ToString().Trim();
                                }
                                userId = tempUserId;
                                if (!string.IsNullOrWhiteSpace(otp) && !string.IsNullOrWhiteSpace(checkOTP) && otp == checkOTP)
                                {
                                    DateTime dtNow = objUtility.GetSqlCurrentDateTime();
                                    DateTime dt3 = objUtility.stod(dt.Rows[0]["OTP_ValidTillDate"].ToString());
                                    TimeSpan time = TimeSpan.Parse(dt.Rows[0]["OTP_ValidTillTime"].ToString());
                                    DateTime dtExpiry = dt3 + time;
                                    if (dtNow > dtExpiry)
                                    {
                                        Strsql = "update OTP_Master set OTP_Status='E' where OTP_Identity='" + strIdentity + "'";
                                        objUtility.ExecuteSQL(Strsql);
                                        logRespMessage = returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "OTP has been expired", returnDt, "");
                                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "OTP has been expired", returnDt, ""));
                                    }

                                    int expTime = Convert.ToInt32(_configuration["accessTokenExpireTime"]);
                                    int refreshExpTime = 0;

                                    if (objUtility.GetWebParameter("AccTokenExpTm").Trim() != "")
                                    {
                                        expTime = Convert.ToInt32(objUtility.GetWebParameter("AccTokenExpTm").Trim());
                                    }
                                    else
                                    {
                                        expTime = Convert.ToInt32(_configuration["tokenExpireTime"]);
                                    }
                                    if (objUtility.GetWebParameter("RefTokenExpTm").Trim() != "")
                                    {
                                        refreshExpTime = Convert.ToInt32(objUtility.GetWebParameter("RefTokenExpTm").Trim());
                                    }
                                    else
                                    {
                                        refreshExpTime = Convert.ToInt32(_configuration["refreshTokenExpireTime"]);
                                    }
                                    //var datetimeExp = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                                    if (product.Trim() == "T")
                                    {
                                        FillConfigParametersString();
                                    }
                                    else
                                    {
                                        _configuration["IsTradeWeb"] = product.Trim();
                                        if (loginAs == "C" && product == "E" && objUtility.mfnGetSysSplFeatureDP("TBL") == false)
                                        {
                                            string[] strUserID = userId.Split("|");
                                            _configuration["IsEstroOffLine"] = "Y";
                                            _configuration["SessionDPID"] = strUserID[0];
                                        }
                                        else if (loginAs == "C" && product == "C" && objUtility.mfnGetSysSplFeatureDP("TBL") == false)
                                        {
                                            string[] strUserID = userId.Split("|");
                                            _configuration["IsCrossOffLine"] = "Y";
                                            _configuration["SessionDPID"] = strUserID[0];
                                        }
                                    }

                                    #region old code
                                    /*var guidVal = Guid.NewGuid();
                                    *//*if (role == "Admin" && branch == false)
                                        objUtility.ExecuteSQL("update Login_Session set ls_token = '" + guidVal + "', ls_logintype = 'C', ls_logintm = '" + DateTime.Now.ToString("HH:mm:ss") + "'  Where ls_code = '" + userId + "' And ls_logindt = '" + DateTime.Now.ToString("yyyyMMdd") + "'  And len(ls_token) < 3");*//*

                                    var loginRole = tokenS.Claims.First(claim => claim.Type == "loginrole").Value;
                                    if ((loginRole == "Admin" || loginRole == "TradeMobile" || loginRole == "EstroWeb") && branch == false)
                                    {
                                        objUtility.ExecuteSQL("update Login_Session set ls_token = '" + guidVal + "', ls_logintype = '" + (role == "TradeMobile" ? "M" : "C") + "', ls_logintm = '" + DateTime.Now.ToString("HH:mm:ss") + "'  Where ls_code = '" + userId + "' And ls_logindt = '" + DateTime.Now.ToString("yyyyMMdd") + "'  And len(ls_token) < 3");
                                    }

                                    string strUserAccess = "";
                                    if (userData.Columns.Contains("UserAccess"))//branch
                                    {
                                        strUserAccess = userData.Rows[0]["UserAccess"].ToString().Trim();
                                    }
                                    var tokenString = branch == true ? GenerateJSONWebTokenUser(new TradeWebLoginModel { username = userId, password = password, role = loginRole, guid = guidVal.ToString(), tokenExpTime = expTime, useraccess = strUserAccess }) : GenerateJSONWebToken(new TradeWebLoginModel { username = userId, password = password, role = loginRole, guid = guidVal.ToString(), tokenExpTime = expTime });

                                    Strsql = "update OTP_Master set OTP_Status='M' where OTP_Identity='" + strIdentity + "'";
                                    objUtility.ExecuteSQL(Strsql);

                                    var refreshToken = objUtility.GenerateRefreshToken(userId, loginAs, loginRole, refreshExpTime);

                                    tokenResponseNew result = new tokenResponseNew();
                                    result.status = true;
                                    result.message = "success";
                                    result.status_code = (int)HttpStatusCode.OK;
                                    result.token = tokenString;
                                    result.tokenExpireTime = datetimeExp.AddMinutes(expTime).ToString("yyyy/MM/dd HH:mm:ss");
                                    result.refreshToken = refreshToken;
                                    result.data = userData;
                                    var jsonData = JsonConvert.SerializeObject(result, Formatting.Indented);*/
                                    #endregion
                                    var loginRole = tokenS.Claims.First(claim => claim.Type == "loginrole").Value;
                                    var jsonData = _tradeWebRepository.Verify2FA(userId, password, loginAs, branch, loginRole, role, strIdentity, expTime, refreshExpTime, userData);
                                    logRespMessage = jsonData;
                                    return Ok(jsonData);
                                }
                                else
                                {
                                    logRespMessage = returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "OTP Mismached", returnDt, "");
                                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "OTP Mismached", returnDt, ""));
                                }
                            }
                            catch (Exception ex)
                            {
                                logRespMessage = returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, "");
                                return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                            }
                        }
                        else if (strFunctionName == "CheckVersion")
                        {
                            strsql = "select 1 from WebParameter where sp_parmcd='TplusAPIURL'";
                            DataTable dtTemp = objUtility.OpenDataTable(strsql);
                            if (dtTemp.Rows.Count == 0)
                            {
                                strsql = "Insert into WebParameter values('TplusAPIURL', 'Tplus Website API URL', 'https://tplus.in/api')";
                                objUtility.ExecuteSQL(strsql);
                            }

                            strsql = "select 1 from WebParameter where sp_parmcd='TWebVerUSR'";
                            dtTemp = objUtility.OpenDataTable(strsql);
                            if (dtTemp.Rows.Count == 0)
                            {
                                strsql = "INSERT INTO WebParameter VALUES('TWebVerUSR', 'TradeWeb Version Update User', 'ADMIN');";
                                objUtility.ExecuteSQL(strsql);
                            }

                            string TplusAPIURL = objUtility.GetWebParameter("TplusAPIURL");

                            if (string.IsNullOrWhiteSpace(TplusAPIURL))
                            {
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "success", data = null, datarows = null }, Formatting.Indented));
                            }

                            var dataXML = xml.GetElementsByTagName("X_Filter")[0]?.InnerXml;
                            XDocument xdoc = XDocument.Parse($"<root>{dataXML}</root>");
                            string ApplicationName = objUtility.mfnReplaceForSQLInjection(xdoc.Root.Element("ApplicationName")?.Value ?? "");
                            string LoginAs = objUtility.mfnReplaceForSQLInjection(xdoc.Root.Element("LoginAs")?.Value ?? "");
                            string Product = objUtility.mfnReplaceForSQLInjection(xdoc.Root.Element("Product")?.Value ?? "");
                            string Version = objUtility.mfnReplaceForSQLInjection(xdoc.Root.Element("Version")?.Value ?? "");
                            string strParmUAT = objUtility.GetWebParameter("WebsiteUAT");
                            string strParamName = ApplicationName + (strParmUAT == "Y" ? "UAT" : "");

                            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                            string strLoggedUser = "";

                            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                            {
                                return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Token is missing or invalid", returnDt, "");
                            }

                            var tokenS = GetToken();

                            if (tokenS.ValidTo < DateTime.UtcNow)
                            {
                                return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Token has expired", returnDt, "");
                            }

                            strLoggedUser = tokenS.Claims
                                .FirstOrDefault(claim => claim.Type.Equals("username", StringComparison.OrdinalIgnoreCase))
                                ?.Value ?? "";

                            if (LoginAs == "")
                            {
                                LoginAs = objUtility.GetLoginAs(strLoggedUser);
                            }

                            #region old code
                            /*bool blnValid = false;
                            if (Product == "T" && ApplicationName.ToUpper() == "TRADEWEB")
                            {
                                if (LoginAs == "C" || LoginAs == "B")
                                {
                                    if (objUtility.mfnGetSysSplFeature("TBL"))
                                    {
                                        blnValid = true;
                                    }
                                }
                                else if (LoginAs == "M")
                                {
                                    if (objUtility.mfnGetSysSplFeature("TMB"))
                                    {
                                        blnValid = true;
                                    }
                                }
                            }
                            else if (Product == "E")
                            {
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "success", data = null, datarows = null }, Formatting.Indented));
                            }

                            if (!blnValid)
                            {
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "You are not authorise to use TradeWebAPI Web Service,Please contact SecMark.", data = null, datarows = null }, Formatting.Indented));
                            }*/
                            #endregion

                            LoginRole loginRole = objUtility.GetLoginRole(LoginAs, Product);
                            string role = loginRole.Role;

                            if (loginRole.Offline)
                            {
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "success", data = null, datarows = null }, Formatting.Indented));
                            }

                            if (string.IsNullOrWhiteSpace(role) || !loginRole.ContinueWithoutKey)
                            {
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = !loginRole.ContinueWithoutKey ? loginRole.LicMsg : "You are not authorise to use TradeWebAPI Web Service,Please contact SecMark.", data = null, datarows = null }, Formatting.Indented));
                            }

                            if (ApplicationName.ToUpper().Trim() == "CROSSNET")
                            {
                                ApplicationName = "CrossWeb";
                            }
                            else if (ApplicationName.ToUpper().Trim() == "ESTRONET")
                            {
                                ApplicationName = "EstroWeb";
                            }

                            /*if (Product == "T")
                            {
                                var (blnContinueWithouKey, strLicMessage, arrKey) = objUtility.chkAMCReceived();
                                if (!blnContinueWithouKey)
                                {
                                    return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = strLicMessage, data = null, datarows = null }, Formatting.Indented));
                                }
                            }*/

                            string strParamDBDate = objUtility.GetWebParameter(strParamName + "DBDT");
                            DateTime ParamDBDate;
                            bool isQueryDateValid = DateTime.TryParseExact(strParamDBDate, "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out ParamDBDate);
                            var client = new HttpClient();
                            //string urlWithParams = $"{TplusAPIURL}/Tplus/Versions?ApplicationName={Uri.EscapeDataString(ApplicationName)}";
                            string urlWithParams = $"{TplusAPIURL}/Tplus/Versions?ApplicationName={Uri.EscapeDataString(ApplicationName)}&Date={Uri.EscapeDataString(ParamDBDate.ToString("yyyyMMddHHmmss"))}";
                            if (strParmUAT == "Y")
                            {
                                urlWithParams += "&Environment=UAT";
                            }

                            HttpResponseMessage response;
                            try
                            {
                                response = await client.GetAsync(urlWithParams);
                            }
                            catch (Exception ex)
                            {
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = $"Request failed: {ex.Message}", data = null, datarows = null }, Formatting.Indented));
                            }

                            if (!response.IsSuccessStatusCode)
                            {
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = $"Error: {response.StatusCode}", data = null, datarows = null }, Formatting.Indented));
                            }

                            var responseBody = await response.Content.ReadAsStringAsync();
                            var wrapper = JsonConvert.DeserializeObject<VersionReponse<VersionData>>(responseBody);

                            if (wrapper?.Data == null)
                            {
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "No data returned from API", data = null, datarows = null }, Formatting.Indented));
                            }

                            VersionData VersionRes = wrapper.Data;

                            DataTable dtRes = new DataTable("rs0");
                            dtRes.Columns.Add("Name", typeof(string));
                            dtRes.Columns.Add("Status", typeof(string));
                            dtRes.Columns.Add("Message", typeof(string));
                            dtRes.Columns.Add("Remark", typeof(string));
                            string strMessage = "";
                            string strLatestVersion = "";
                            string strAppStatus = "";
                            string strAppName = "";
                            if (Product == "T")
                            {
                                if (LoginAs == "C" || LoginAs == "B")
                                {
                                    objUtility.ParseVersion(VersionRes.ApplicationVersion, out strLatestVersion, out strAppStatus);
                                    strAppName = "TradeWeb";
                                }
                                else if (LoginAs == "M")
                                {
                                    objUtility.ParseVersion(VersionRes.MobileAppVersion, out strLatestVersion, out strAppStatus);
                                    strAppName = "TradeMobile";
                                }
                            }
                            else if (Product == "C")
                            {
                                objUtility.ParseVersion(VersionRes.ApplicationVersion, out strLatestVersion, out strAppStatus);
                                strAppName = "CrossWeb";
                            }
                            else if (Product == "E")
                            {
                                objUtility.ParseVersion(VersionRes.ApplicationVersion, out strLatestVersion, out strAppStatus);
                                strAppName = "EstroWeb";
                            }

                            if (Version != strLatestVersion)
                            {
                                strMessage = "The new version " + strLatestVersion + " is now available." + (strAppStatus == "M" ? "Please Update" : "");
                                dtRes.Rows.Add(strAppName, strAppStatus, strMessage, "");
                            }

                            objUtility.ParseVersion(VersionRes.APIVersion, out strLatestVersion, out strAppStatus);
                            if (strAPIVersion != strLatestVersion)
                            {
                                //objUtility.ParseVersion(VersionRes.APIVersion, out strLatestVersion, out strAppStatus);
                                strMessage = "The new version " + strLatestVersion + " is now available." + (strAppStatus == "M" ? "Please Update" : "");
                                dtRes.Rows.Add(strAppName + "API", strAppStatus, strMessage, "");
                            }

                            VersionInfo versionInfo = objUtility.GetVersionInfo(VersionRes);

                            if ((versionInfo.WebDBDate - versionInfo.ParamDBDate).Duration() > TimeSpan.FromMinutes(1)) //versionInfo.WebDBDate != versionInfo.ParamDBDate
                            {
                                strMessage = "This version requires a database update. (Release Date: " + versionInfo.WebDBDate.ToString("dd/MM/yyyy") + ")";
                                string strRemark = string.Join(Environment.NewLine, VersionRes.DBDetails
                                    .Where(d => !string.IsNullOrWhiteSpace(d.Remark))
                                    .Select(d => d.Remark));
                                dtRes.Rows.Add(strAppName + "DB", "O", strMessage, strRemark);
                            }

                            /*if ((versionInfo.LocalSPFileDate - versionInfo.WebSPFileDate).Duration() > TimeSpan.FromMinutes(1) || (versionInfo.ParamSPFileDate - versionInfo.WebSPFileDate).Duration() > TimeSpan.FromMinutes(1))
                            {
                                strMessage = "This version requires an update to the database component. (Release Date: " + versionInfo.WebSPFileDate.ToString("dd/MM/yyyy") + ")" + (versionInfo.WebSPFileStatus == "M" ? "Please Update" : "");
                                dtRes.Rows.Add(strAppName + "SP", versionInfo.WebSPFileStatus, strMessage, "");
                            }*/

                            if ((versionInfo.LocalSPFileDate - versionInfo.WebSPFileDate).Duration() > TimeSpan.FromMinutes(1) || (versionInfo.ParamSPFileDate - versionInfo.WebSPFileDate).Duration() > TimeSpan.FromMinutes(1))
                            {
                                if (versionInfo.LocalSPFileDate <= versionInfo.WebSPFileDate.AddMinutes(1))
                                {
                                    strMessage = "This version requires an update to the database component. (Release Date: " + versionInfo.WebSPFileDate.ToString("dd/MM/yyyy") + ")" + (versionInfo.WebSPFileStatus == "M" ? "Please Update" : "");
                                    dtRes.Rows.Add(strAppName + "SP", versionInfo.WebSPFileStatus, strMessage, "");
                                }
                            }

                            if (ApplicationName.ToUpper() == "TRADEWEB")
                            {
                                if (objUtility.fnchkTable("Other_Products"))
                                {
                                    string strDPParamName = "";
                                    strsql = "select * from Other_Products where OP_Product in ('Cross','Estro','Commex') and OP_Status = 'A'";
                                    DataTable dtOP = objUtility.OpenDataTable(strsql);
                                    foreach (DataRow dr in dtOP.Rows)
                                    {
                                        if (dr["OP_Product"].ToString().Trim() == "Cross")
                                        {
                                            strDPParamName = "CrossWeb";
                                        }
                                        else if (dr["OP_Product"].ToString().Trim() == "Estro")
                                        {
                                            strDPParamName = "EstroWeb";
                                        }
                                        else if (dr["OP_Product"].ToString().Trim() == "Commex")
                                        {
                                            strDPParamName = "Commex";
                                        }

                                        if (strDPParamName != "")
                                        {
                                            string strDPParamDBDate = "";
                                            //string connectionString = "server=" + dr["OP_Server"].ToString().Trim() + ";Database=" + dr["OP_Database"].ToString().Trim() + ";Uid=" + dr["OP_User"].ToString().Trim() + ";Pwd=" + dr["OP_PWD"].ToString().Trim() + ";Max Pool Size=200;Connect Timeout=20000;pooling='true';";
                                            string connectionString = objUtility.GetDynamicConnectionString(connetionString, dr["OP_Server"].ToString().Trim(), dr["OP_Database"].ToString().Trim(), dr["OP_User"].ToString().Trim(), dr["OP_PWD"].ToString().Trim());

                                            using (SqlConnection sqlcon = new SqlConnection(connectionString))
                                            {
                                                sqlcon.Open();

                                                if (!objUtility.fnchkTable("WebParameter", sqlcon))
                                                {
                                                    strsql = "CREATE TABLE WebParameter( ";
                                                    strsql += " [sp_parmcd] [varchar](15) NOT NULL, ";
                                                    strsql += " [sp_name] [varchar](40) NOT NULL, ";
                                                    strsql += " [sp_sysvalue] [varchar](250) NOT NULL, ";
                                                    strsql += " CONSTRAINT [PK_WebParameter] PRIMARY KEY CLUSTERED ";
                                                    strsql += " ( sp_parmcd ) ";
                                                    strsql += " ) ON [PRIMARY] ";
                                                    objUtility.ExecuteSQL(strsql, sqlcon);
                                                }

                                                strDPParamDBDate = objUtility.GetWebParameter(strDPParamName + "DBDT", sqlcon);
                                                DateTime DPParamDBDate;
                                                bool isDPQueryDateValid = DateTime.TryParseExact(strDPParamDBDate, "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DPParamDBDate);

                                                //var client = new HttpClient();
                                                urlWithParams = $"{TplusAPIURL}/Tplus/Versions?ApplicationName={Uri.EscapeDataString(strDPParamName)}&Date={Uri.EscapeDataString(DPParamDBDate.ToString("yyyyMMddHHmmss"))}";
                                                if (strParmUAT == "Y")
                                                {
                                                    urlWithParams += "&Environment=UAT";
                                                }

                                                string Message = "";
                                                VersionRes = new VersionData();
                                                (Message, VersionRes) = await objUtility.VersionAPI(urlWithParams, strDPParamName);

                                                if (Message != "")
                                                {
                                                    return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = Message, data = null, datarows = null }, Formatting.Indented));
                                                }

                                                versionInfo = objUtility.GetVersionInfo(VersionRes, sqlcon);

                                                if ((versionInfo.WebDBDate - versionInfo.ParamDBDate).Duration() > TimeSpan.FromMinutes(1)) //versionInfo.WebDBDate != versionInfo.ParamDBDate
                                                {
                                                    strMessage = "This version requires a database update. (Release Date: " + versionInfo.WebDBDate.ToString("dd/MM/yyyy") + ")";
                                                    string strRemark = string.Join(Environment.NewLine, VersionRes.DBDetails
                                                            .Where(d => !string.IsNullOrWhiteSpace(d.Remark))
                                                            .Select(d => d.Remark));
                                                    dtRes.Rows.Add(strDPParamName + "DB", "O", strMessage, strRemark);
                                                }

                                                if ((versionInfo.LocalSPFileDate - versionInfo.WebSPFileDate).Duration() > TimeSpan.FromMinutes(1) || (versionInfo.ParamSPFileDate - versionInfo.WebSPFileDate).Duration() > TimeSpan.FromMinutes(1))
                                                {
                                                    if (versionInfo.LocalSPFileDate <= versionInfo.WebSPFileDate.AddMinutes(1))
                                                    {
                                                        strMessage = "This version requires an update to the database component. (Release Date: " + versionInfo.WebSPFileDate.ToString("dd/MM/yyyy") + ")" + (versionInfo.WebSPFileStatus == "M" ? "Please Update" : "");
                                                        dtRes.Rows.Add(strDPParamName + "SP", versionInfo.WebSPFileStatus, strMessage, "");
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                if (objUtility.mfnGetSysSplFeature("RKC") || objUtility.mfnGetSysSplFeature("RKM") || objUtility.mfnGetSysSplFeature("CKC"))
                                {
                                    string strDPParamName = "ReKYC";
                                    string strDPParamDBDate = objUtility.GetWebParameter(strDPParamName + "DBDT");
                                    DateTime DPParamDBDate;
                                    bool isDPQueryDateValid = DateTime.TryParseExact(strDPParamDBDate, "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DPParamDBDate);

                                    //var client = new HttpClient();
                                    urlWithParams = $"{TplusAPIURL}/Tplus/Versions?ApplicationName={Uri.EscapeDataString("ReKYC")}&Date={Uri.EscapeDataString(DPParamDBDate.ToString("yyyyMMddHHmmss"))}";
                                    if (strParmUAT == "Y")
                                    {
                                        urlWithParams += "&Environment=UAT";
                                    }

                                    string Message = "";
                                    VersionRes = new VersionData();
                                    (Message, VersionRes) = await objUtility.VersionAPI(urlWithParams, strDPParamName);

                                    if (Message != "")
                                    {
                                        return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = Message, data = null, datarows = null }, Formatting.Indented));
                                    }

                                    versionInfo = objUtility.GetVersionInfo(VersionRes);

                                    if ((versionInfo.WebDBDate - versionInfo.ParamDBDate).Duration() > TimeSpan.FromMinutes(1)) //versionInfo.WebDBDate != versionInfo.ParamDBDate
                                    {
                                        strMessage = "This version requires a database update. (Release Date: " + versionInfo.WebDBDate.ToString("dd/MM/yyyy") + ")";
                                        string strRemark = string.Join(Environment.NewLine, VersionRes.DBDetails
                                                .Where(d => !string.IsNullOrWhiteSpace(d.Remark))
                                                .Select(d => d.Remark));
                                        dtRes.Rows.Add(strDPParamName + "DB", "O", strMessage, strRemark);
                                    }

                                    if ((versionInfo.LocalSPFileDate - versionInfo.WebSPFileDate).Duration() > TimeSpan.FromMinutes(1) || (versionInfo.ParamSPFileDate - versionInfo.WebSPFileDate).Duration() > TimeSpan.FromMinutes(1))
                                    {
                                        if (versionInfo.LocalSPFileDate <= versionInfo.WebSPFileDate.AddMinutes(1))
                                        {
                                            strMessage = "This version requires an update to the database component. (Release Date: " + versionInfo.WebSPFileDate.ToString("dd/MM/yyyy") + ")" + (versionInfo.WebSPFileStatus == "M" ? "Please Update" : "");
                                            dtRes.Rows.Add(strDPParamName + "SP", versionInfo.WebSPFileStatus, strMessage, "");
                                        }
                                    }
                                }
                            }

                            DataSet dsRes = new DataSet();
                            dsRes.Tables.Add(dtRes);

                            return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "", data = dsRes, datarows = new List<string> { dtRes.Rows.Count.ToString() } }, Formatting.Indented));
                        }
                        else if (strFunctionName == "UpdateVersion")
                        {
                            string TplusAPIURL = objUtility.GetWebParameter("TplusAPIURL");

                            if (string.IsNullOrWhiteSpace(TplusAPIURL))
                            {
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "success", data = null, datarows = null }, Formatting.Indented));
                            }

                            var dataXML = xml.GetElementsByTagName("X_Filter")[0]?.InnerXml;
                            XDocument xdoc = XDocument.Parse($"<root>{dataXML}</root>");
                            string ApplicationName = objUtility.mfnReplaceForSQLInjection(xdoc.Root.Element("ApplicationName")?.Value ?? "");
                            string UserId = objUtility.mfnReplaceForSQLInjection(xdoc.Root.Element("UserId")?.Value ?? "");
                            string LoginAs = objUtility.mfnReplaceForSQLInjection(xdoc.Root.Element("LoginAs")?.Value ?? "");
                            string Product = objUtility.mfnReplaceForSQLInjection(xdoc.Root.Element("Product")?.Value ?? "");
                            string Version = objUtility.mfnReplaceForSQLInjection(xdoc.Root.Element("Version")?.Value ?? "");
                            string strParmUAT = objUtility.GetWebParameter("WebsiteUAT");
                            string strMessage = "";
                            string strParamName = ApplicationName + (strParmUAT == "Y" ? "UAT" : "");

                            if (Product == "C")
                            {
                                strParamName = "CrossWeb" + (strParmUAT == "Y" ? "UAT" : "");
                            }
                            else if (Product == "E")
                            {
                                strParamName = "EstroWeb" + (strParmUAT == "Y" ? "UAT" : "");
                            }

                            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                            string strLoggedUser = "";

                            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                            {
                                //return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Token is missing or invalid", returnDt, "");
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "Token is missing or invalid", data = null, datarows = null }, Formatting.Indented));
                            }

                            var tokenS = GetToken();

                            if (tokenS.ValidTo < DateTime.UtcNow)
                            {
                                //return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Token has expired", returnDt, "");
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "Token has expired", data = null, datarows = null }, Formatting.Indented));
                            }

                            strLoggedUser = tokenS.Claims
                                .FirstOrDefault(claim => claim.Type.Equals("username", StringComparison.OrdinalIgnoreCase))
                                ?.Value ?? "";

                            if (LoginAs == "")
                            {
                                //string UserID = objUtility.mfnReplaceForSQLInjection(xdoc.Root.Element("UserId")?.Value ?? "");
                                LoginAs = objUtility.GetLoginAs(strLoggedUser);
                            }

                            /*bool blnValid = false;
                            if (Product == "T" && ApplicationName.ToUpper() == "TRADEWEB")
                            {
                                if (LoginAs == "C" || LoginAs == "B")
                                {
                                    if (objUtility.mfnGetSysSplFeature("TBL"))
                                    {
                                        blnValid = true;
                                    }
                                }
                                else if (LoginAs == "M")
                                {
                                    if (objUtility.mfnGetSysSplFeature("TMB"))
                                    {
                                        blnValid = true;
                                    }
                                }
                            }

                            if (!blnValid)
                            {
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "You are not authorise to use TradeWebAPI Web Service,Please contact SecMark.", data = null, datarows = null }, Formatting.Indented));
                            }*/

                            LoginRole loginRole = objUtility.GetLoginRole(LoginAs, Product);
                            string role = loginRole.Role;

                            if (loginRole.Offline)
                            {
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "success", data = null, datarows = null }, Formatting.Indented));
                            }

                            if (string.IsNullOrWhiteSpace(role) || !loginRole.ContinueWithoutKey)
                            {
                                //return returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, !loginRole.ContinueWithoutKey ? loginRole.LicMsg : "You are not authorise to use TradeWebAPI Web Service,Please contact SecMark.", returnDt, "");
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = !loginRole.ContinueWithoutKey ? loginRole.LicMsg : "You are not authorise to use TradeWebAPI Web Service,Please contact SecMark.", data = null, datarows = null }, Formatting.Indented));
                            }

                            if (ApplicationName.ToUpper().Trim() == "CROSSNET" || Product == "C")
                            {
                                ApplicationName = "CrossWeb";
                            }
                            else if (ApplicationName.ToUpper().Trim() == "ESTRONET" || Product == "E")
                            {
                                ApplicationName = "EstroWeb";
                            }

                            /*if (Product == "T")
                            {
                                var (blnContinueWithouKey, strLicMessage, arrKey) = objUtility.chkAMCReceived();
                                if (!blnContinueWithouKey)
                                {
                                    return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = strLicMessage, data = null, datarows = null }, Formatting.Indented));
                                }
                            }*/

                            /*var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                                string strLoggedUser = "";

                                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                                {
                                    var tokenS = GetToken();

                                    strLoggedUser = tokenS.Claims
                                        .FirstOrDefault(claim => claim.Type.Equals("username", StringComparison.OrdinalIgnoreCase))
                                        ?.Value ?? "";
                            }*/

                            /*if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                            {
                                strLoggedUser = "";
                            }
                            else
                            {
                                var tokenS = GetToken();
                                strLoggedUser = tokenS.Claims.First(claim => claim.Type == "username").Value;
                            }*/

                            string strUpdateUser = "";
                            if (objUtility.fnchkTable("Webparameter"))
                            {
                                strUpdateUser = objUtility.GetWebParameter("TWebVerUSR");
                            }

                            if (string.IsNullOrWhiteSpace(strLoggedUser) || !string.Equals(strUpdateUser, strLoggedUser?.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "You are not authorise to update version", data = null, datarows = null }, Formatting.Indented));
                            }

                            //string strParamDBDate = objUtility.GetWebParameter(ApplicationName + "DBDT");
                            string strParamDBDate = objUtility.GetWebParameter(strParamName + "DBDT");
                            DateTime ParamDBDate;
                            bool isQueryDateValid = DateTime.TryParseExact(strParamDBDate, "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out ParamDBDate);

                            var client = new HttpClient();
                            string urlWithParams = $"{TplusAPIURL}/Tplus/Versions?ApplicationName={Uri.EscapeDataString(ApplicationName)}&Date={Uri.EscapeDataString(ParamDBDate.ToString("yyyyMMddHHmmss"))}";
                            if (strParmUAT == "Y")
                            {
                                urlWithParams += "&Environment=UAT";
                            }

                            /*HttpResponseMessage response;
                            try
                            {
                                response = await client.GetAsync(urlWithParams);
                            }
                            catch (Exception ex)
                            {
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = $"Request failed: {ex.Message}", data = null, datarows = null }, Formatting.Indented));
                            }

                            if (!response.IsSuccessStatusCode)
                            {
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = $"Error: {response.StatusCode}", data = null, datarows = null }, Formatting.Indented));
                            }

                            var responseBody = await response.Content.ReadAsStringAsync();
                            var wrapper = JsonConvert.DeserializeObject<VersionReponse<VersionData>>(responseBody);

                            if (wrapper?.Data == null)
                            {
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "No data returned from API", data = null, datarows = null }, Formatting.Indented));
                            }*/

                            string Message = "";
                            VersionData VersionRes = new VersionData();
                            (Message, VersionRes) = await objUtility.VersionAPI(urlWithParams, strParamName);

                            if (Message != "")
                            {
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = Message, data = null, datarows = null }, Formatting.Indented));
                            }

                            DataTable dtRes = new DataTable("rs0");
                            dtRes.Columns.Add("Name", typeof(string));
                            dtRes.Columns.Add("Status", typeof(string));
                            dtRes.Columns.Add("Message", typeof(string));
                            //VersionData VersionRes = wrapper.Data;
                            VersionInfo versionInfo = objUtility.GetVersionInfo(VersionRes);

                            /*if ((versionInfo.LocalSPFileDate - versionInfo.WebSPFileDate).Duration() < TimeSpan.FromMinutes(1))
                            {
                                if ((versionInfo.WebDBDate - versionInfo.ParamDBDate).Duration() > TimeSpan.FromMinutes(1))
                                {
                                    if (VersionRes.DBDetails != null)
                                    {
                                        var db = new DataContext();
                                        Microsoft.Data.SqlClient.SqlConnection sqlCon = new Microsoft.Data.SqlClient.SqlConnection(db.Database.GetDbConnection().ConnectionString);
                                        sqlCon.Open();
                                        foreach (var dbDetail in VersionRes.DBDetails)
                                        {
                                            if (!string.IsNullOrEmpty(dbDetail.Query))
                                            {
                                                var statements = dbDetail.Query.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

                                                foreach (var statement in statements)
                                                {
                                                    try
                                                    {
                                                        using Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(statement, sqlCon);
                                                        command.ExecuteNonQuery();
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = ApplicationName + " DB Error : " + ex.Message + " Last Used Query : " + statement, data = null, datarows = null }, Formatting.Indented));
                                                    }
                                                }

                                                //var queries = objUtility.SplitSqlStatements(dbDetail.Query);

                                                //foreach (var query in queries)
                                                //{
                                                //    try
                                                //    {
                                                //        using Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(query, sqlCon);
                                                //        command.ExecuteNonQuery();
                                                //    }
                                                //    catch (Exception)
                                                //    {
                                                //        continue;
                                                //    }
                                                //}
                                            }
                                        }

                                        if (Convert.ToInt16(objUtility.fnFireQueryTradeWeb("webparameter", "count(0)", "sp_parmcd", strParamName + "DBDT", true)) > 0)
                                        {
                                            strsql = "update webparameter set sp_sysvalue = '" + versionInfo.WebDBDate.ToString("yyyyMMddHHmmss") + "' where sp_parmcd='" + strParamName + "DBDT'";
                                        }
                                        else
                                        {
                                            strsql = "Insert into Webparameter values('" + strParamName + "DBDT', '" + strParamName + " Database Date', '" + versionInfo.WebDBDate.ToString("yyyyMMddHHmmss") + "')";
                                        }
                                        objUtility.ExecuteSQL(strsql);
                                    }
                                }

                                if ((versionInfo.ParamSPFileDate - versionInfo.WebSPFileDate).Duration() > TimeSpan.FromMinutes(1))
                                {
                                    try
                                    {
                                        objUtility.CheckSPNew(strParamName);
                                    }
                                    catch (Exception ex)
                                    {
                                        return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = ApplicationName + " SP Error : " + ex.Message, data = null, datarows = null }, Formatting.Indented));
                                    }
                                }
                            }
                            else
                            {
                                strMessage = "This version requires an update to the database component. (Release Date: " + versionInfo.WebSPFileDate.ToString("dd/MM/yyyy") + ")" + (versionInfo.WebSPFileStatus == "M" ? "Please Update" : "");
                                dtRes.Rows.Add(ApplicationName + "WebSP", versionInfo.WebSPFileStatus, strMessage);
                                DataSet dsResTemp = new DataSet();
                                dsResTemp.Tables.Add(dtRes);

                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = strMessage, data = dsResTemp, datarows = new List<string> { "1" } }, Formatting.Indented));
                            }*/

                            string strVersionExc = objUtility.ExecuteVersion(strParamName, ApplicationName, dtRes, versionInfo, VersionRes);
                            if (strVersionExc != "")
                            {
                                return Ok(strVersionExc);
                            }

                            if (ApplicationName.ToUpper() == "TRADEWEB")
                            {
                                if (objUtility.fnchkTable("Other_Products"))
                                {
                                    string strDPParamName = "";
                                    string DPApplicationName = "";
                                    strsql = "select * from Other_Products where OP_Product in ('Cross','Estro','Commex') and OP_Status = 'A'";
                                    DataTable dtOP = objUtility.OpenDataTable(strsql);
                                    foreach (DataRow dr in dtOP.Rows)
                                    {
                                        if (dr["OP_Product"].ToString().Trim() == "Cross")
                                        {
                                            strDPParamName = "CrossWeb" + (strParmUAT == "Y" ? "UAT" : "");
                                            DPApplicationName = "CrossWeb";

                                            /*if (Message != "")
                                            {
                                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = Message, data = null, datarows = null }, Formatting.Indented));
                                            }*/
                                        }
                                        else if (dr["OP_Product"].ToString().Trim() == "Estro")
                                        {
                                            strDPParamName = "EstroWeb" + (strParmUAT == "Y" ? "UAT" : "");
                                            DPApplicationName = "EstroWeb";
                                        }
                                        else if (dr["OP_Product"].ToString().Trim() == "Commex")
                                        {
                                            strDPParamName = "Commex" + (strParmUAT == "Y" ? "UAT" : "");
                                            DPApplicationName = "Commex";
                                        }

                                        if (strDPParamName != "")
                                        {
                                            string strDPParamDBDate = "";
                                            //string connectionString = "server=" + dr["OP_Server"].ToString().Trim() + ";Database=" + dr["OP_Database"].ToString().Trim() + ";Uid=" + dr["OP_User"].ToString().Trim() + ";Pwd=" + dr["OP_PWD"].ToString().Trim() + ";Max Pool Size=200;Connect Timeout=20000;pooling='true';";
                                            string connectionString = objUtility.GetDynamicConnectionString(connetionString, dr["OP_Server"].ToString().Trim(), dr["OP_Database"].ToString().Trim(), dr["OP_User"].ToString().Trim(), dr["OP_PWD"].ToString().Trim());

                                            using (SqlConnection sqlcon = new SqlConnection(connectionString))
                                            {
                                                sqlcon.Open();

                                                strDPParamDBDate = objUtility.GetWebParameter(strDPParamName + "DBDT", sqlcon);
                                                DateTime DPParamDBDate;
                                                bool isDPQueryDateValid = DateTime.TryParseExact(strDPParamDBDate, "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DPParamDBDate);

                                                //var client = new HttpClient();
                                                urlWithParams = $"{TplusAPIURL}/Tplus/Versions?ApplicationName={Uri.EscapeDataString(DPApplicationName)}&Date={Uri.EscapeDataString(DPParamDBDate.ToString("yyyyMMddHHmmss"))}";
                                                if (strParmUAT == "Y")
                                                {
                                                    urlWithParams += "&Environment=UAT";
                                                }

                                                Message = "";
                                                VersionRes = new VersionData();
                                                (Message, VersionRes) = await objUtility.VersionAPI(urlWithParams, strDPParamName);

                                                if (Message != "")
                                                {
                                                    return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = Message, data = null, datarows = null }, Formatting.Indented));
                                                }

                                                versionInfo = objUtility.GetVersionInfo(VersionRes, sqlcon);

                                                strVersionExc = objUtility.ExecuteVersion(strDPParamName, DPApplicationName, dtRes, versionInfo, VersionRes, sqlcon);
                                                if (strVersionExc != "")
                                                {
                                                    return Ok(strVersionExc);
                                                }
                                            }
                                        }
                                    }
                                }

                                if (objUtility.mfnGetSysSplFeature("RKC") || objUtility.mfnGetSysSplFeature("RKM") || objUtility.mfnGetSysSplFeature("CKC"))
                                {
                                    string strDPParamName = "ReKYC";
                                    string strDPParamDBDate = objUtility.GetWebParameter(strDPParamName + "DBDT");
                                    DateTime DPParamDBDate;
                                    bool isDPQueryDateValid = DateTime.TryParseExact(strDPParamDBDate, "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DPParamDBDate);

                                    //var client = new HttpClient();
                                    urlWithParams = $"{TplusAPIURL}/Tplus/Versions?ApplicationName={Uri.EscapeDataString("ReKYC")}&Date={Uri.EscapeDataString(DPParamDBDate.ToString("yyyyMMddHHmmss"))}";
                                    if (strParmUAT == "Y")
                                    {
                                        urlWithParams += "&Environment=UAT";
                                    }

                                    Message = "";
                                    VersionRes = new VersionData();
                                    (Message, VersionRes) = await objUtility.VersionAPI(urlWithParams, strDPParamName);

                                    if (Message != "")
                                    {
                                        return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = Message, data = null, datarows = null }, Formatting.Indented));
                                    }

                                    versionInfo = objUtility.GetVersionInfo(VersionRes);

                                    strVersionExc = objUtility.ExecuteVersion("ReKYC", "ReKYC", dtRes, versionInfo, VersionRes);
                                    if (strVersionExc != "")
                                    {
                                        return Ok(strVersionExc);
                                    }
                                }
                            }

                            if (objUtility.fnchkTable("tbl_GroupUserAccessMaster"))
                            {
                                strsql = "select count(0) from tbl_GroupUserAccessMaster";
                                DataTable dtTemp = objUtility.OpenDataTable(strsql);
                                if (Convert.ToInt16(dtTemp.Rows[0][0].ToString()) == 0)
                                {
                                    objUtility.ExecuteCommonSP("stp_GenerateAccessRights", new Dictionary<string, string>());
                                }
                            }

                            string strLatestVersion = "";
                            string strAppStatus = "";
                            string strAppName = "";
                            if (Product == "T")
                            {
                                if (LoginAs == "C" || LoginAs == "B")
                                {
                                    objUtility.ParseVersion(VersionRes.ApplicationVersion, out strLatestVersion, out strAppStatus);
                                    strAppName = "TradeWeb";
                                }
                                else if (LoginAs == "M")
                                {
                                    objUtility.ParseVersion(VersionRes.MobileAppVersion, out strLatestVersion, out strAppStatus);
                                    strAppName = "TradeMobile";
                                }
                            }
                            else if (Product == "C")
                            {
                                objUtility.ParseVersion(VersionRes.ApplicationVersion, out strLatestVersion, out strAppStatus);
                                strAppName = "CrossWeb";
                            }
                            else if (Product == "E")
                            {
                                objUtility.ParseVersion(VersionRes.ApplicationVersion, out strLatestVersion, out strAppStatus);
                                strAppName = "EstroWeb";
                            }

                            if (Version != strLatestVersion)
                            {
                                strMessage = "The new version " + strLatestVersion + " is now available." + (strAppStatus == "M" ? "Please Update" : "");
                                dtRes.Rows.Add(strAppName, strAppStatus, strMessage);
                            }

                            objUtility.ParseVersion(VersionRes.APIVersion, out strLatestVersion, out strAppStatus);
                            if (strAPIVersion != strLatestVersion)
                            {
                                strMessage = "The new version " + strLatestVersion + " is now available." + (strAppStatus == "M" ? "Please Update" : "");
                                dtRes.Rows.Add(strAppName + "API", strAppStatus, strMessage);
                            }

                            DataSet dsRes = new DataSet();
                            dsRes.Tables.Add(dtRes);

                            return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "", data = dsRes, datarows = new List<string> { dtRes.Rows.Count.ToString() } }, Formatting.Indented));
                        }
                        else if (strFunctionName == "LoginSSO")
                        {
                            var ssoResp = await _tradeWebRepository.LoginSSO_Req(xmlStr);
                            logRespMessage = Convert.ToString(ssoResp);
                            return Ok(ssoResp);
                        }

                        /*string strXML = objUtility.SerializeToXml(xml, omitXmlDeclaration: true, indent: false, encoding: "utf-16");
                        using (Microsoft.Data.SqlClient.SqlConnection conn = new Microsoft.Data.SqlClient.SqlConnection(connetionString))
                        {
                            conn.Open();
                            logSrNo = objUtility.InsertLog(0, moduleName, "", strXML, "", "", "", conn);
                        }*/
                        var getData = _tradeWebRepository.CommonAPIrepoCall(strModuleName, strFunctionName, strXML);
                        if (getData != null)
                        {
                            return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "", data = getData.Data, datarows = getData.DataRows }, Formatting.Indented));
                        }
                        else
                        {
                            return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "No records found", data = null, datarows = null }, Formatting.Indented));
                        }
                    }
                }
                catch (Exception ex)
                {
                    logErrorMessage = ex.Message.ToString();
                    return BadRequest(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = ex.Message.ToString(), data = null, datarows = null }, Formatting.Indented));
                }
                finally
                {
                    using (Microsoft.Data.SqlClient.SqlConnection conn = new Microsoft.Data.SqlClient.SqlConnection(connetionString))
                    {
                        conn.Open();
                        objUtility.UpdateLog(logSrNo, "", logRespMessage, logErrorMessage, conn);
                    }
                }
            }
            return BadRequest();
        }

        [Route("ReceiveCallback/{source?}")]
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> ReceiveCallback(string source = null)
        {
            long logSrNo = 0;
            string logRespMessage = "", logErrorMessage = "", logReq = "";
            string strDebugFlag = "";
            string connetionString = objUtility.GetConnectionStr();

            try
            {
                string body = string.Empty;

                object bodyNode = null;

                Request.EnableBuffering();

                if (Request.HasFormContentType)
                {
                    var form = await Request.ReadFormAsync();

                    bodyNode = form.Keys
                        .ToDictionary(k => k, v => form[v].ToString());
                }
                else
                {
                    using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
                    {
                        var rawBody = await reader.ReadToEndAsync();
                        Request.Body.Position = 0;

                        if (!string.IsNullOrWhiteSpace(rawBody))
                        {
                            if (Request.ContentType != null &&
                                Request.ContentType.Contains("application/json"))
                            {
                                bodyNode = JsonConvert.DeserializeObject(rawBody);
                            }
                            else
                            {
                                bodyNode = rawBody;
                            }
                        }
                    }
                }

                var logObject = new
                {
                    Method = Request.Method,
                    Path = Request.Path.ToString(),
                    QueryString = Request.QueryString.ToString(),
                    ContentType = Request.ContentType,
                    Body = bodyNode,
                    ClientIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    RequestTimeUtc = DateTime.UtcNow.ToString("o"),
                    TraceIdentifier = HttpContext.TraceIdentifier
                };

                string logJson = JsonConvert.SerializeObject(logObject);

                strDebugFlag = "Y";

                if (strDebugFlag == "Y")
                {
                    using (SqlConnection conn = new SqlConnection(connetionString))
                    {
                        conn.Open();
                        logSrNo = objUtility.InsertLog(0, "ReceiveCallback", "", logJson, "", "", "", conn);
                    }
                }

                body = string.Empty;
                bodyNode = string.Empty;

                if (Request.HasFormContentType)
                {
                    var form = await Request.ReadFormAsync();

                    bodyNode = form.Keys
                        .ToDictionary(k => k, v => form[v].ToString());
                }
                else
                {
                    using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
                    {
                        var rawBody = await reader.ReadToEndAsync();
                        Request.Body.Position = 0;

                        if (!string.IsNullOrWhiteSpace(rawBody))
                        {
                            if (Request.ContentType != null &&
                                Request.ContentType.Contains("application/json"))
                            {
                                bodyNode = rawBody;
                            }
                            else
                            {
                                bodyNode = rawBody; // XML or text stays raw
                            }
                        }
                    }
                }

                var requestObject = new
                {
                    Source = source,
                    Method = Request.Method,
                    Path = Request.Path.ToString(),
                    ContentType = Request.ContentType ?? "",
                    ContentLength = Request.ContentLength?.ToString() ?? "0",
                    QueryString = Request.Query.Any() ? Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString()) : null,
                    Body = bodyNode,
                    Headers = Request.Headers.Any() ? Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()) : null,
                    ClientIp = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
                };

                string strJSON = JsonConvert.SerializeObject(requestObject, Formatting.Indented);
                using (SqlConnection conn = new SqlConnection(connetionString))
                {
                    using (SqlCommand cmd = new SqlCommand("stpr_ReceiveCallback"))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        //cmd.CommandTimeout = 0;
                        if (conn.State == ConnectionState.Closed)
                            conn.Open();
                        cmd.Connection = conn;
                        cmd.Parameters.Add(new SqlParameter("@i_vcInputJSON", SqlDbType.VarChar));
                        cmd.Parameters["@i_vcInputJSON"].Value = strJSON;

                        cmd.Parameters.Add(new SqlParameter("@o_vcOutPutJSON", SqlDbType.VarChar, 2000000));
                        cmd.Parameters["@o_vcOutPutJSON"].Direction = ParameterDirection.Output;
                        cmd.CommandTimeout = 0;
                        cmd.ExecuteNonQuery();

                        /*string retValue = cmd.Parameters["@o_vcOutPutJSON"].Value.ToString();

                        JArray jsonArray = null;
                        try
                        {
                            if (!string.IsNullOrEmpty(retValue))
                            {
                                jsonArray = JArray.Parse(retValue);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }*/

                        string retValue = cmd.Parameters["@o_vcOutPutJSON"].Value?.ToString();

                        logRespMessage = "ReturnValue = " + retValue;

                        JArray jsonArray = null;
                        JObject jsonObject = null;
                        string plainText = null;

                        try
                        {
                            if (!string.IsNullOrWhiteSpace(retValue))
                            {
                                retValue = retValue.Trim();

                                if (retValue.StartsWith("["))
                                {
                                    jsonArray = JArray.Parse(retValue);
                                }
                                else if (retValue.StartsWith("{"))
                                {
                                    jsonObject = JObject.Parse(retValue);
                                }
                                else
                                {
                                    plainText = retValue; // normal text
                                }
                            }
                        }
                        catch (Exception)
                        {
                            // fallback: treat everything as plain text if parsing fails
                            plainText = retValue;
                        }
                        cmd.Parameters.Clear();
                    }
                }

                logRespMessage += " " + JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "Data posted successfully" });
                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "Data posted successfully" }, Formatting.Indented));
            }
            catch (Exception ex)
            {
                logErrorMessage = JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = ex.Message.ToString() });
                return BadRequest(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = ex.Message.ToString(), data = null, datarows = null }, Formatting.Indented));
            }
            finally
            {
                if (strDebugFlag != "N")
                {
                    using (SqlConnection conn = new SqlConnection(connetionString))
                    {
                        conn.Open();
                        if (logSrNo == 0)
                        {
                            logSrNo = objUtility.InsertLog(0, "TradeWeb", "", logReq, logRespMessage, logErrorMessage, "", conn);
                        }
                        else
                        {
                            objUtility.UpdateLog(logSrNo, "", logRespMessage, logErrorMessage, conn);
                        }
                    }
                }
            }
        }

        [HttpPost, Route("/api/PayoutToBackOffice")]
        [Consumes("application/json")]
        public async Task<IActionResult> PayoutToBackOffice()
        {
            var strJson = "";
            long srNo = 0;
            string connetionString = objUtility.GetConnectionStr();

            try
            {
                Request.EnableBuffering();
                Request.Body.Position = 0;

                using (var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
                {
                    strJson = await reader.ReadToEndAsync();
                    Request.Body.Position = 0;
                }

                if (string.IsNullOrWhiteSpace(strJson))
                {
                    using (Microsoft.Data.SqlClient.SqlConnection conn = new Microsoft.Data.SqlClient.SqlConnection(connetionString))
                    {
                        conn.Open();
                        srNo = objUtility.InsertLog(0, "/api/Main/PostData", "", strJson, "", "Request body cannot be blank", "", conn);
                    }
                    return BadRequest(JsonConvert.SerializeObject(new commonResponse
                    {
                        status = false,
                        message = "Request body cannot be blank",
                        status_code = (int)HttpStatusCode.BadRequest,
                        data = returnDt
                    }, Formatting.Indented));
                }
            }
            catch (Exception ex)
            {
                using (Microsoft.Data.SqlClient.SqlConnection conn = new Microsoft.Data.SqlClient.SqlConnection(connetionString))
                {
                    conn.Open();
                    srNo = objUtility.InsertLog(0, "/api/Main/PostData", "", strJson, "", "Failed to read body: " + ex.Message, "", conn);
                }
                return BadRequest(JsonConvert.SerializeObject(new commonResponse
                {
                    status = false,
                    message = "Failed to read body: " + ex.Message,
                    status_code = (int)HttpStatusCode.BadRequest,
                    data = returnDt
                }, Formatting.Indented));
            }

            var res = _tradeWebRepository.PostData(strJson);
            if (res.Status)
            {
                return Ok(res.Data);
            }
            else
            {
                return StatusCode(500, res.Data);
            }
        }

        [HttpPost, Route("DigitalSendEmail")]
        [Consumes("application/json")]
        public async Task<IActionResult> DigitalSendEmail([FromBody] DigitalSendEmailRequest request)
        {
            string userId = string.IsNullOrWhiteSpace(request?.UserId) ? "API" : request.UserId;

            await _svc.ReleaseStaleAsync();

            if (!await _svc.HasPendingAsync())
                return Ok(Reply(false, "No records found"));

            long? jobId = await _svc.TryStartJobAsync(userId);
            if (jobId == null)
                return Ok(Reply(true, "Email sending is already in progress. Please wait until it finishes."));

            try { await _channel.Writer.WriteAsync(new DigitalEmailJob(jobId.Value, userId)); }
            catch (Exception ex)
            {
                await _svc.FinishJobAsync(jobId.Value, new SendRunResult(false, "Could not queue: " + ex.Message, 0, 0));
                throw;
            }

            return Ok(Reply(true, "Email process started"));
        }

        [HttpGet, Route("DigitalSendEmailStatus")]
        public async Task<IActionResult> DigitalSendEmailStatus()
        {
            DataTable dt = await _svc.GetCurrentJobAsync();
            dt.TableName = "rs0";
            var ds = new DataSet(); ds.Tables.Add(dt);
            return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel
            {
                success = true,
                message = "",
                data = ds,
                datarows = new List<string> { dt.Rows.Count.ToString() }
            }, Formatting.Indented));
        }

        private static string Reply(bool ok, string msg) =>
            JsonConvert.SerializeObject(new CrossNetCommonModel { success = ok, message = msg }, Formatting.Indented);

        private JwtSecurityToken GetToken()
        {
            var handler = new JwtSecurityTokenHandler();
            string authHeader = Request.Headers["Authorization"];
            authHeader = authHeader.Replace("Bearer ", "");
            var token = handler.ReadToken(authHeader) as JwtSecurityToken;
            return token;
        }

        // TODO : Write a method for generate jwt token.
        private string GenerateJSONWebToken(TradeWebLoginModel userInfo)
        {
            string decryptJwtKey = objUtility.Decrypt(_configuration["Jwt:Key"].ToString());
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(decryptJwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            string Strsql = "";
            if (_configuration["IsTradeWeb"] == "T" || _configuration["IsTradeWeb"] == "O")
            {
                string apiRes3 = objUtility.fnFireQueryTradeWeb("Entity_master", "count(0)", " em_cd='B' and em_name like 'SYKES & RAY EQUITIES%' and 1", "1", true);
                if (Convert.ToInt16(apiRes3) > 0)
                    Strsql = " select em_name OrgName,em_cd CompnyCd from Entity_Master with (nolock) where em_cd='B'";
                else
                    Strsql = " select em_name OrgName,em_cd CompnyCd from Entity_Master with (nolock) where em_cd =(select min(em_cd) from Entity_master Where len(em_cd) = 1)";
            }
            else if (_configuration["IsTradeWeb"] == "C")
            {
                Strsql = "select sp_sysvalue OrgName,'' CompnyCd from sysparameter with (nolock) where sp_parmcd='NAME'";
            }

            else if (_configuration["IsTradeWeb"] == "E")
            {
                Strsql = "select sp_sysvalue OrgName,'' CompnyCd from sysparameter with (nolock) where sp_parmcd='NAME'";
            }

            else if (_configuration["IsTradeWeb"] == "B")
            {
                Strsql = " select em_name OrgName,em_cd CompnyCd from Entity_Master with (nolock) where em_cd =(select min(em_cd) from Entity_master )";
            }

            var ObjDataSet = objUtility.OpenDataSet(Strsql);
            int tokenExpTime = userInfo.tokenExpTime;

            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, userInfo.username),
                new Claim(type: "companyCode", value: ObjDataSet.Tables[0].Rows[0]["CompnyCd"].ToString().Trim()),
                new Claim(JwtRegisteredClaimNames.Jti, userInfo.guid),
                new Claim(type: "username", value: userInfo.username),
                new Claim(type: "OFLV", value: _configuration["IsTradeWeb"].Trim() !="O" ? "Y" : "N"),
                new Claim(ClaimTypes.Role, userInfo.role),
            };
            var token = new JwtSecurityToken(_configuration["Jwt:Issuer"],
                _configuration["Jwt:Issuer"],
                claims,
                expires: DateTime.UtcNow.AddMinutes(tokenExpTime),
                signingCredentials: credentials);

            if (userInfo.role == "TradeMobile" || userInfo.role == "EstroWeb")
            {
                _tokenStore.StoreToken(userInfo.username, new JwtSecurityTokenHandler().WriteToken(token));
            }

            //_configuration["Segments"] = ObjDataSet.Tables[0].Rows[0]["CompnyCd"].ToString().Trim();
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        private string GenerateJSONWebTokenUser(TradeWebLoginModel userInfo)
        {
            string decryptJwtKey = objUtility.Decrypt(_configuration["Jwt:Key"].ToString());
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(decryptJwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            string Strsql = "";
            if (_configuration["IsTradeWeb"] == "T" || _configuration["IsTradeWeb"] == "O")
            {
                Strsql = "select em_cd CompanyCode,em_name OrgName from Entity_Master";
                Strsql += " where em_cd in (select min(em_cd) from Entity_master)";
            }
            else
            {
                Strsql = "select (select sp_sysvalue from Sysparameter where sp_parmcd = 'DPID') CompanyCode, ";
                Strsql += " (select sp_sysvalue from Sysparameter where sp_parmcd = 'NAME') OrgName";
            }

            var ObjDataSet = objUtility.OpenDataSet(Strsql);

            string loginAccess = "";
            if (userInfo.useraccess != "HO")
            {
                if (_configuration["IsTradeWeb"] == "T" || _configuration["IsTradeWeb"] == "O")
                {
                    loginAccess = LoginAccess(userInfo.username, "T");
                }
                else
                {
                    loginAccess = LoginAccess(userInfo.username, "");
                }
            }

            int tokenExpTime = userInfo.tokenExpTime;

            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, userInfo.username),
                new Claim(type: "companyCode", value: ObjDataSet.Tables[0].Rows[0]["CompanyCode"].ToString().Trim()),
                new Claim(JwtRegisteredClaimNames.Jti, userInfo.guid),
                new Claim(type: "username", value: userInfo.username),
                new Claim(ClaimTypes.Role, userInfo.role),
                new Claim(type: "loginaccess", value: loginAccess),
                new Claim(type: "useraccess", value: userInfo.useraccess),
            };
            var token = new JwtSecurityToken(_configuration["Jwt:Issuer"],
                _configuration["Jwt:Issuer"],
                claims,
                expires: DateTime.UtcNow.AddMinutes(tokenExpTime),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        private string LoginAccess(string userId, string product)
        {
            string Strsql = "";
            string AccessFilter = "";
            if (product == "T")
            {
                Strsql = "if (select count(*) from sysobjects where name='LoginAccess')>0 select * from loginAccess where la_userID='" + userId + "' order by la_grouping else select * from group_master where 1=2";
                DataTable dtLogin = objUtility.OpenDataTable(Strsql);
                if (dtLogin.Rows.Count > 0)
                {
                    short j;
                    string strCatg = "";
                    string strTmp;
                    j = 0;
                    strsql = "";
                    while (j < dtLogin.Rows.Count)
                    {
                        strCatg = dtLogin.Rows[j]["La_grouping"].ToString();
                        strTmp = "";
                        while (strCatg == dtLogin.Rows[j]["La_grouping"].ToString())
                        {
                            strTmp = strTmp + "'" + Strings.Trim(dtLogin.Rows[j]["la_grcode"].ToString()) + "',";
                            j = Conversions.ToShort(j + 1);
                            if (j >= dtLogin.Rows.Count)
                            {
                                break;
                            }
                        }

                        strsql = strTmp;    // for Branches 
                        strTmp = Strings.Mid(strTmp, 1, Strings.Len(strTmp) - 1);
                        var switchExpr = Strings.UCase(strCatg);
                        switch (switchExpr)
                        {
                            case "B":
                                {
                                    strCatg = "cm_brboffcode";

                                    strsql = strsql.Replace("'", "").Replace(",", "|");
                                    //HttpContext.Current.Session["Branch"] = strsql;
                                    break;
                                }

                            case "G":
                                {
                                    strCatg = "cm_groupcd";
                                    break;
                                }

                            case "F":
                                {
                                    strCatg = "cm_familycd";
                                    break;
                                }

                            case "R":
                                {
                                    strCatg = "cm_Subbroker";
                                    break;
                                }

                            case "A":
                                {
                                    break;
                                }

                            case "C":
                                {
                                    strCatg = "cm_cd";
                                    break;
                                }

                            case "M":
                                {
                                    strCatg = "cm_dpactno";
                                    break;
                                }
                            case "D":
                                {
                                    strCatg = "cm_margintype";
                                    break;
                                }
                        }

                        //AccessFilter = Conversions.ToString(Conversions.ToString(AccessFilter + Interaction.IIf(Strings.Len(AccessFilter) > 0, " or ", "") + strCatg) + Interaction.IIf(Strings.InStr(1, strTmp, ",") > 0, " in (" + strTmp + ")", "=" + strTmp));
                        if (switchExpr == "R")
                        {
                            AccessFilter = Conversions.ToString(Conversions.ToString(AccessFilter + Interaction.IIf(Strings.Len(AccessFilter) > 0, " or (", "(") + strCatg) + Interaction.IIf(Strings.InStr(1, strTmp, ",") > 0, " in (" + strTmp + ")", "=" + strTmp) + " or exists ( select cm2_cd from client_info where cm2_cd = cm_cd and cm_remissier2 " + Interaction.IIf(Strings.InStr(1, strTmp, ",") > 0, " in (" + strTmp + ")))", "=" + strTmp + "))"));
                        }
                        else
                        {
                            AccessFilter = Conversions.ToString(Conversions.ToString(AccessFilter + Interaction.IIf(Strings.Len(AccessFilter) > 0, " or ", "") + strCatg) + Interaction.IIf(Strings.InStr(1, strTmp, ",") > 0, " in (" + strTmp + ")", "=" + strTmp));
                        }
                    }

                    if ((Strings.UCase(strCatg) ?? "") == "A")
                    {
                        return "";
                    }
                    else
                    {
                        return " and ( " + AccessFilter + " )";
                    }
                }
            }
            else
            {
                Strsql = "if (select count(*) from sysobjects with (noLock) where name='LoginAccess') > 0 select * from loginAccess with (noLock) where la_userID='" + userId + "' order by la_grouping else select * from group_master with (noLock) where 1=2";
                DataTable dtLogin = objUtility.OpenDataTable(Strsql);

                if (dtLogin.Rows.Count > 0)
                {
                    short j;
                    string strCatg = "";
                    string strTmp;
                    j = 0;
                    strsql = "";
                    while (j < dtLogin.Rows.Count)
                    {
                        strCatg = dtLogin.Rows[j]["La_grouping"].ToString();
                        strTmp = "";
                        while (strCatg == dtLogin.Rows[j]["La_grouping"].ToString())
                        {
                            strTmp = strTmp + "'" + Strings.Trim(dtLogin.Rows[j]["la_grcode"].ToString()) + "',";
                            j = Conversions.ToShort(j + 1);
                            if (j >= dtLogin.Rows.Count)
                            {
                                break;
                            }
                        }

                        strsql = strTmp;    // for Branches 
                        strTmp = Strings.Mid(strTmp, 1, Strings.Len(strTmp) - 1);
                        var switchExpr = Strings.UCase(strCatg);
                        switch (switchExpr)
                        {
                            case "B":
                                strCatg = "cm_brboffcode";
                                break;
                            case "G":
                                strCatg = "cm_groupcd";
                                break;
                            case "F":
                                strCatg = "cm_familycd";
                                break;
                            case "R":
                                strCatg = "cm_remisser";
                                break;
                            case "A":
                                break; // TODO: might not be correct. Was : Exit Do
                            case "C":
                                strCatg = "cm_cd";
                                break;
                            case "M":
                                strCatg = "cm_dpactno";
                                break;
                        }
                        AccessFilter = AccessFilter + (Strings.Len(AccessFilter) > 0 ? " or " : "") + strCatg + (Strings.InStr(1, strTmp, ",", CompareMethod.Text) > 0 ? " in (" + strTmp + ")" : "=" + strTmp);
                    }

                    if (Strings.UCase(strCatg) == "A")
                    {
                        return "";
                    }
                    else
                    {
                        return "and (" + AccessFilter + ")";
                    }
                }
                else
                {
                    Strsql = "select um_brcode from user_master with (noLock) where um_user_id='" + userId + "'";
                    DataTable dtBranch = objUtility.OpenDataTable(Strsql);

                    if (dtBranch.Rows.Count > 0)
                    {
                        if (dtBranch.Rows[0][0].ToString().Trim() == "<ALL>")
                        {
                            return "";
                        }
                        string[] Branch = dtBranch.Rows[0][0].ToString().Split('|');
                        for (int i = 0; i < Branch.Length - 1; i++)
                        {
                            AccessFilter = AccessFilter + "'" + Branch[i] + "',";
                        }
                        if (AccessFilter != string.Empty)
                        {
                            AccessFilter = AccessFilter.Substring(0, AccessFilter.Length - 1);
                            AccessFilter = " and cm_brboffcode in (" + AccessFilter + ")";
                            return AccessFilter.ToString();
                        }
                        else
                        {
                            return "";
                        }
                    }
                    else
                    {
                        return "";
                    }
                }
            }
            return null;
        }

        private static byte[] EncryptStringToBytes_Aes(string plainText, byte[] key, byte[] iv)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.KeySize = 128;   // key is 16 bytes
                aesAlg.BlockSize = 128;
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;

                aesAlg.Key = key;
                aesAlg.IV = iv;

                using (ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV))
                using (MemoryStream msEncrypt = new MemoryStream())
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                    swEncrypt.Flush();
                    csEncrypt.FlushFinalBlock();

                    return msEncrypt.ToArray();
                }
            }
        }

        private static string DecryptStringFromBytes(byte[] cipherText, byte[] key, byte[] iv)
        {
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException(nameof(cipherText));

            if (key == null || key.Length <= 0)
                throw new ArgumentNullException(nameof(key));

            if (iv == null || iv.Length <= 0)
                throw new ArgumentNullException(nameof(iv));

            string plaintext = null;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;

                aesAlg.KeySize = 256;
                aesAlg.BlockSize = 128;

                aesAlg.Key = key;
                aesAlg.IV = iv;

                using (ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV))
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                {
                    plaintext = srDecrypt.ReadToEnd();
                }
            }

            return plaintext;
        }

        private string ReturnsHost()
        {
            string strHost = string.Empty;
            try
            {
                strHost = System.Environment.GetEnvironmentVariable("COMPUTERNAME");// System.Net.Dns.GetHostEntry(Request.ServerVariables["remote_addr"]).HostName.Trim().ToUpper();
            }
            catch (Exception)
            {
                strHost = "";// Request.ServerVariables["remote_Addr"].ToString();
            }
            return strHost;
        }

        private dynamic LoginSSOResponse(string key, string product, string loginAs, string userId, DataTable dt)
        {
            string role = "";
            if (string.IsNullOrWhiteSpace(product))
            {
                product = "T";
            }
            product = product.ToUpper().Trim();

            var autResp = _tradeWebRepository.Login_API_Authorize(key, product, loginAs, "");
            if (autResp == null || autResp == "")
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    string encKey = _configuration["encKey"];
                    encKey = objUtility.Decrypt(encKey);
                    encKey = encKey + DateTime.Now.ToString("yyyyMMdd");
                    var keybytes = Encoding.UTF8.GetBytes(encKey);
                    var iv = Encoding.UTF8.GetBytes(encKey);
                    var encrypted = Convert.FromBase64String(key);
                    var decriptedFromJavascript = DecryptStringFromBytes(encrypted, keybytes, iv);
                    key = string.Format(decriptedFromJavascript);
                }
                autResp = _tradeWebRepository.Login_API_Authorize(key, product, loginAs, "");
                if (autResp == null || autResp == "")
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "You are not authorise to use TradeWebAPI Web Service,Please contact SecMark.", returnDt, ""));
                }
            }
            switch (autResp)
            {
                case "BPI":
                    role = "Admin";
                    break;
                case "TBL":
                    role = "Admin";
                    break;
                case "INP":
                    role = "GainLoss";
                    break;
                case "TBI":
                    role = "Branch";
                    break;
                case "PBI":
                    role = "Performance";
                    break;
                case "SUB":
                    role = "Subscription";
                    break;
                case "TMB":
                    role = "TradeMobile";
                    break;
                case "CBI":
                    role = "CrossNet";
                    break;
                case "EBI":
                    role = "EstroNet";
                    break;
                case "FUP":
                    role = "FundPayout";
                    break;
                case "CLM":
                    role = "CrossModification";
                    break;
            }
            //if (objUtility.mfnGetSysSplFeature("RKC"))
            //{
            //    objUtility.CreateReKYCTables();
            //    if (objUtility.GetWebParameter("TRADEPLUSTEMPDB") != "")
            //    {
            //        string strDatabase = objUtility.GetWebParameter("TRADEPLUSTEMPDB").Trim();
            //        var dbCon = new DataContext();
            //        string connectionString = dbCon.Database.GetDbConnection().ConnectionString;
            //        Microsoft.Data.SqlClient.SqlConnectionStringBuilder builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString)
            //        {
            //            InitialCatalog = strDatabase
            //        };
            //        string newConnectionString = builder.ToString();

            //        using (Microsoft.Data.SqlClient.SqlConnection sqlCon = new Microsoft.Data.SqlClient.SqlConnection(newConnectionString))
            //        {
            //            sqlCon.Open();
            //            objUtility.CheckSP("ReKYC2", sqlCon);
            //        }
            //    }
            //    objUtility.CheckSP("ReKYC");
            //}

            //try
            //{
            //    var respTemp = _tradeWebRepository.UserDetails(userId, "", true, product, role);
            //}
            //catch (Exception)
            //{
            //}
            var addLog = _tradeWebRepository.AddLog_Session(userId, "", "C");
            if (addLog != null)
            {
                return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", addLog, ""));
            }
            int expTime;
            int refreshExpTime = 0;
            if (objUtility.GetWebParameter("AccTokenExpTm").Trim() != "")
            {
                expTime = Convert.ToInt32(objUtility.GetWebParameter("AccTokenExpTm").Trim());
            }
            else
            {
                expTime = Convert.ToInt32(_configuration["tokenExpireTime"]);
            }
            if (objUtility.GetWebParameter("refreshExpTime").Trim() != "")
            {
                refreshExpTime = Convert.ToInt32(objUtility.GetWebParameter("refreshExpTime").Trim());
            }
            //var datetimeExp = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
            var datetimeExp = DateTime.UtcNow;
            if (product.Trim() == "T")
            {
                FillConfigParametersString();
            }
            else
            {
                _configuration["IsTradeWeb"] = product.Trim();
            }
            var guidVal = Guid.NewGuid();
            if (role == "Admin")
                objUtility.ExecuteSQL("update Login_Session set ls_token = '" + guidVal + "', ls_logintype = 'C', ls_logintm = '" + DateTime.Now.ToString("HH:mm:ss") + "'  Where ls_code = '" + userId + "' And ls_logindt = '" + DateTime.Now.ToString("yyyyMMdd") + "'  And len(ls_token) < 3");

            var tokenString = GenerateJSONWebToken(new TradeWebLoginModel { username = userId, password = "", role = role, guid = guidVal.ToString(), tokenExpTime = expTime });
            var refreshToken = objUtility.GenerateRefreshToken(userId, loginAs, role, refreshExpTime);

            tokenResponseNew result = new tokenResponseNew();
            result.status = true;
            result.message = "success";
            result.status_code = (int)HttpStatusCode.OK;
            result.token = tokenString;
            result.tokenExpireTime = datetimeExp.AddMinutes(expTime).ToString(UtcDateTimeFormat);
            result.refreshToken = refreshToken;
            result.data = dt;
            var jsonData = JsonConvert.SerializeObject(result, Formatting.Indented);
            return jsonData;
        }

        private string FillConfigParametersString()
        {
            try
            {
                _configuration["Segments"] = objUtility.GetWebParameter("segments");
                _configuration["ColourTitle"] = objUtility.GetWebParameter("cLR_TITLE");
                _configuration["ColourMenu"] = objUtility.GetWebParameter("cLR_MENU");
                _configuration["ColourHeader"] = objUtility.GetWebParameter("cLR_HEADER");
                _configuration["ColourTotals"] = objUtility.GetWebParameter("cLR_TOTALS");
                _configuration["IsTradeWeb"] = objUtility.GetWebParameter("isTradeWeb");
                _configuration["TPlus"] = objUtility.AddBracket(objUtility.GetWebParameter("tPlus"));
                _configuration["TPlusES"] = objUtility.AddBracket(objUtility.GetWebParameter("tPlusES"));
                _configuration["Cross"] = objUtility.AddBracket(objUtility.GetWebParameter("cross"));
                _configuration["CrossEs"] = objUtility.AddBracket(objUtility.GetWebParameter("crossEs"));
                _configuration["Estro"] = objUtility.AddBracket(objUtility.GetWebParameter("estro"));
                _configuration["EstroEs"] = objUtility.AddBracket(objUtility.GetWebParameter("estroEs"));
                _configuration["Commex"] = objUtility.AddBracket(objUtility.GetWebParameter("commex"));
                _configuration["CommexEs"] = objUtility.AddBracket(objUtility.GetWebParameter("commexEs"));
                _configuration["CAPTCHA"] = objUtility.AddBracket(objUtility.GetWebParameter("cAPTCHA"));

                _configuration["SMTPHOST"] = objUtility.AddBracket(objUtility.GetWebParameter("sMTPHOST"));
                _configuration["PANASPASSWORD"] = objUtility.AddBracket(objUtility.GetWebParameter("pANASPASSWORD"));
                _configuration["GroupLogin"] = objUtility.AddBracket(objUtility.GetWebParameter("groupLogin"));
                _configuration["ShowRupeeSymbol"] = objUtility.AddBracket(objUtility.GetWebParameter("showRupeeSymbol"));
                _configuration["CLR_WEBSKIN"] = objUtility.AddBracket(objUtility.GetWebParameter("cLR_WEBSKIN"));
                _configuration["FTSOSURL"] = objUtility.AddBracket(objUtility.GetWebParameter("fTSOSURL"));
                _configuration["PMSUrl"] = objUtility.AddBracket(objUtility.GetWebParameter("pMSUrl"));
                _configuration["KYCPDF"] = objUtility.AddBracket(objUtility.GetWebParameter("kYCPDF"));
                _configuration["OrganizationName"] = objUtility.AddBracket(objUtility.GetWebParameter("organizationName"));
                _configuration["TNetInvplUrl"] = objUtility.AddBracket(objUtility.GetWebParameter("tNetInvplUrl"));
                _configuration["OUTPOSURL"] = objUtility.AddBracket(objUtility.GetWebParameter("oUTPOSURL"));
                _configuration["SMTPSERVER"] = objUtility.AddBracket(objUtility.GetWebParameter("sMTPSERVER"));
                _configuration["SECURITYPROT"] = objUtility.AddBracket(objUtility.GetWebParameter("sECURITYPROT"));
                _configuration["TCM"] = objUtility.mfnGetSysSplFeatureCommodity("TCM").ToString();

                return "success";
            }
            catch (Exception ex)
            {
                return ex.Message.ToString();

            }
        }
    }
}
