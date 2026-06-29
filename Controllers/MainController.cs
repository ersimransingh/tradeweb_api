using Fernet;
using MailKit.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using TradeWeb.API.Data;
using TradeWeb.API.Models;
using TradeWeb.API.QuestPdfServicesClass;
using TradeWeb.API.Repository;
using TradeWeb.Entity;
using TradeWeb.Service;
using Formatting = Newtonsoft.Json.Formatting;

namespace TradeWeb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MainController : BaseController
    {
        #region Class level declarations.
        private readonly Microsoft.AspNetCore.Identity.UserManager<AppUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly UtilityCommon objUtility;
        private readonly ITradeWebRepository _tradeWebRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IWebHostEnvironment _environment;
        private readonly TokenStore _tokenStore;

        private string strsql = "";
        ConvertData returnJson = new ConvertData();
        DataTable returnDt = new DataTable();
        private string strAPIVersion = "2.0.0.1";


        #endregion

        #region Constructor
        public MainController(Microsoft.AspNetCore.Identity.UserManager<AppUser> userManager, IConfiguration configuration, UtilityCommon objUtility, IHttpContextAccessor httpContextAccessor, SignInManager<AppUser> signInManager, ITradeWebRepository tradeWebRepository, IWebHostEnvironment environment, TokenStore tokenStore)
        {
            _userManager = userManager;
            _configuration = configuration;
            this.objUtility = objUtility;
            _httpContextAccessor = httpContextAccessor;
            _signInManager = signInManager;
            _tradeWebRepository = tradeWebRepository;
            _environment = environment;
            _tokenStore = tokenStore;
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

        #region API Methods

        #region Login


        public class EncryptResponseAttribute : ActionFilterAttribute
        {
            private readonly byte[] _key;
            private readonly bool _blnEncrypt;
            private readonly byte[] _cryptoKey;

            public EncryptResponseAttribute(IConfiguration config, UtilityCommon objutility)
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
            }

            public override void OnActionExecuted(ActionExecutedContext context)
            {
                var blnSkip = context.HttpContext.Items["Option"]?.ToString() == "InitializeLogin";
                var blnMobile = context.HttpContext.Items["TradeMobile"]?.ToString() == "Y";
                if (blnSkip)
                {
                    base.OnActionExecuted(context);
                    return;
                }

                if (_blnEncrypt && context.Result is ObjectResult objectResult)
                {
                    string plainJson;
                    if (objectResult.Value is string s)
                    {
                        plainJson = s;
                    }
                    else
                    {
                        plainJson = JsonConvert.SerializeObject(objectResult.Value, Formatting.None);
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
                        StatusCode = objectResult.StatusCode ?? StatusCodes.Status200OK
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
                    var getData = "12/05/2026 10:00 AM";

                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));

                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpPost("UserProfile", Name = "UserProfile")]
        public IActionResult Home_UserProfile()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var getData = _tradeWebRepository.GetUserDetais(userId);
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

        /// <summary>
        /// Login validate USER
        /// </summary>
        /// <param name="userId"></param>/param>
        /// <returns></returns>
        [HttpPost("Login_validate_USER", Name = "Login_validate_USER")]
        public IActionResult Login_validate_USER(string userId)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    userId = objUtility.mfnReplaceForSQLInjection(userId);
                    var getData = _tradeWebRepository.Login_validate_USER(userId);
                    if (getData != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }


        /// <summary>
        /// Login validate Password
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [HttpPost("Login_validate_Password", Name = "Login_validate_Password")]
        public IActionResult Login_validate_Password(string userId, string password, string ePassword, string key, string loginAs, string product, string ICPV, string feature = null)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string strDebug = string.IsNullOrWhiteSpace(_configuration["DebugLog"]) ? "" : _configuration["DebugLog"].Trim();
                    if (strDebug == "Y")
                    {
                        var jsonObject = new
                        {
                            userId,
                            password,
                            ePassword,
                            key,
                            loginAs,
                            product,
                            ICPV
                        };

                        string jsonString = JsonConvert.SerializeObject(jsonObject);
                        objUtility.InsertLog(jsonString, "");
                    }

                    userId = objUtility.mfnReplaceForSQLInjection(userId);
                    password = objUtility.mfnReplaceForSQLInjection(password);
                    ePassword = objUtility.mfnReplaceForSQLInjection(ePassword);
                    key = objUtility.mfnReplaceForSQLInjection(key);
                    loginAs = objUtility.mfnReplaceForSQLInjection(loginAs);
                    product = objUtility.mfnReplaceForSQLInjection(product);
                    string strICPV = string.IsNullOrWhiteSpace(ICPV) ? "" : ICPV;
                    string strFeature = string.IsNullOrWhiteSpace(feature) ? "" : feature;
                    string strParmICPV = objUtility.fnchkTable("WebParameter") ? objUtility.GetWebParameter("ICPV") : "";
                    bool keyBased = false;
                    bool branch = false;
                    var role = "";
                    string strMobileVer = "";

                    if (!string.IsNullOrWhiteSpace(loginAs) && loginAs.Substring(0, 1) == "M")
                    {
                        if (loginAs.Contains("~"))
                        {
                            string[] parts = loginAs.Split('~');
                            strMobileVer = parts[1];
                            loginAs = parts[0];
                        }

                        if (strMobileVer != "2.0.0.1" && objUtility.GetWebParameter("SMS2FA").Trim() != "")
                        {
                            return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "You are currently using an older version. Please update to the latest version.", returnDt, ""));
                        }
                    }

                    if (string.IsNullOrWhiteSpace(product))
                    {
                        product = "T";
                    }
                    product = product.ToUpper().Trim();

                    if (!string.IsNullOrWhiteSpace(key) && (objUtility.isBPT() || (strICPV == "Y" && strParmICPV == "Y")))
                    {
                        keyBased = true;
                    }
                    if (string.IsNullOrWhiteSpace(loginAs))
                    {
                        loginAs = "C";
                    }
                    if (loginAs.Trim() == "B")
                    {
                        branch = true;
                    }
                    var autResp = _tradeWebRepository.Login_API_Authorize(key, product, loginAs, feature);
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
                        autResp = _tradeWebRepository.Login_API_Authorize(key, product, loginAs, feature);
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
                        case "TPI":
                            role = "ReKYC";
                            break;
                        case "RKM":
                            role = "ReKYC";
                            break;
                        case "COM":
                            role = "CommonAPI";
                            break;
                        case "CMP":
                            role = "ComAPI";
                            break;
                        case "EBW":
                            role = "EstroWeb";
                            break;
                        case "CBW":
                            role = "CrossWeb";
                            break;
                    }
                    if (objUtility.mfnGetSysSplFeature("RKC") || objUtility.mfnGetSysSplFeature("RKM") || role == "ReKYC")
                    {
                        objUtility.CreateReKYCTables();
                        if (objUtility.GetWebParameter("TRADEPLUSTEMPDB") != "")
                        {
                            string strDatabase = objUtility.GetWebParameter("TRADEPLUSTEMPDB").Trim();
                            var dbCon = new DataContext();
                            string connectionString = dbCon.Database.GetDbConnection().ConnectionString;
                            Microsoft.Data.SqlClient.SqlConnectionStringBuilder builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString)
                            {
                                InitialCatalog = strDatabase
                            };
                            string newConnectionString = builder.ToString();

                            using (Microsoft.Data.SqlClient.SqlConnection sqlCon = new Microsoft.Data.SqlClient.SqlConnection(newConnectionString))
                            {
                                sqlCon.Open();
                                objUtility.CheckSP("json", sqlCon);
                                objUtility.CheckSP("ReKYC2", sqlCon);
                            }
                        }
                        objUtility.CheckSP("ReKYC");

                    }
                    objUtility.CreateMrgpTables();
                    if (loginAs == "C" && product == "E") //For EstroWeb
                    {
                        if (objUtility.mfnGetSysSplFeatureDP("TBL") && role == "EstroWeb")
                        {
                            objUtility.CreateTables(loginAs, product, "EstroWeb");
                            objUtility.CheckSP("EstroWeb");
                        }
                        else if (objUtility.mfnGetSysSplFeatureDP("TBO") && role == "EstroWeb")
                        {
                            objUtility.CreateTables(loginAs, product, "EstroWebOffline");
                            objUtility.CheckSP("EstroWebO");
                        }
                        else
                        {
                            objUtility.CreateTables(loginAs, product, "Estro");
                            objUtility.CheckSP("Estro");
                        }
                    }
                    else if ((loginAs == "C" || loginAs == "B") && product == "C" && (role == "CrossWeb" || role == "CrossNet")) //For CrossWeb
                    {
                        if (objUtility.mfnGetSysSplFeatureDP("TBL") || role == "CrossNet")
                        {
                            objUtility.CreateTables(loginAs, product, "CrossWeb");
                            if (objUtility.GetWebParameter("TRADEPLUSTEMPDB") != "")
                            {
                                string strDatabase = objUtility.GetWebParameter("TRADEPLUSTEMPDB").Trim();
                                var dbCon = new DataContext();
                                string connectionString = dbCon.Database.GetDbConnection().ConnectionString;
                                Microsoft.Data.SqlClient.SqlConnectionStringBuilder builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString)
                                {
                                    InitialCatalog = strDatabase
                                };
                                string newConnectionString = builder.ToString();

                                using (Microsoft.Data.SqlClient.SqlConnection sqlCon = new Microsoft.Data.SqlClient.SqlConnection(newConnectionString))
                                {
                                    sqlCon.Open();
                                    objUtility.CheckSP("json", sqlCon);
                                }
                            }
                            objUtility.CheckSP("CrossWeb");
                        }
                        else if (objUtility.mfnGetSysSplFeatureDP("TBO"))
                        {
                            objUtility.CreateTables(loginAs, product, "CrossWebOffline");
                            objUtility.CheckSP("CrossWebO");
                        }
                    }
                    else if (loginAs == "B" && product == "#")
                    {
                        if (objUtility.isRatnakar())
                        {
                            objUtility.CreateTables(loginAs, product, "TradeWeb");
                            if (objUtility.GetWebParameter("TRADEPLUSTEMPDB") != "")
                            {
                                string strDatabase = objUtility.GetWebParameter("TRADEPLUSTEMPDB").Trim();
                                var dbCon = new DataContext();
                                string connectionString = dbCon.Database.GetDbConnection().ConnectionString;
                                Microsoft.Data.SqlClient.SqlConnectionStringBuilder builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString)
                                {
                                    InitialCatalog = strDatabase
                                };
                                string newConnectionString = builder.ToString();

                                using (Microsoft.Data.SqlClient.SqlConnection sqlCon = new Microsoft.Data.SqlClient.SqlConnection(newConnectionString))
                                {
                                    sqlCon.Open();
                                    objUtility.CheckSP("json", sqlCon);
                                }
                            }
                            objUtility.CheckSP("TradeWeb");
                        }
                    }
                    /*else if ((loginAs == "C" || loginAs == "M") && product == "T" && (role == "Admin" || role == "TradeMobile"))
                    {
                        objUtility.CreateTables(loginAs, product, "TradeWeb");
                        if (objUtility.GetWebParameter("TRADEPLUSTEMPDB") != "")
                        {
                            string strDatabase = objUtility.GetWebParameter("TRADEPLUSTEMPDB").Trim();
                            var dbCon = new DataContext();
                            string connectionString = dbCon.Database.GetDbConnection().ConnectionString;
                            Microsoft.Data.SqlClient.SqlConnectionStringBuilder builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString)
                            {
                                InitialCatalog = strDatabase
                            };
                            string newConnectionString = builder.ToString();

                            using (Microsoft.Data.SqlClient.SqlConnection sqlCon = new Microsoft.Data.SqlClient.SqlConnection(newConnectionString))
                            {
                                sqlCon.Open();
                                objUtility.CheckSP("json", sqlCon);
                            }
                        }
                        objUtility.CheckSP("TradeWeb");
                    }*/
                    if (!string.IsNullOrEmpty(ePassword))
                    {
                        string encKey = _configuration["epwdKey"];
                        encKey = objUtility.Decrypt(encKey);
                        var keybytes = Encoding.UTF8.GetBytes(encKey);
                        var iv = Encoding.UTF8.GetBytes(encKey);
                        var encrypted = Convert.FromBase64String(ePassword);
                        password = DecryptStringFromBytes(encrypted, keybytes, iv);
                    }
                    try
                    {
                        string encKey = _configuration["epwdKey"];
                        encKey = objUtility.Decrypt(encKey);
                        var keybytes = Encoding.UTF8.GetBytes(encKey);
                        var iv = Encoding.UTF8.GetBytes(encKey);
                        var encrypted = Convert.FromBase64String(userId);
                        var decUserId = DecryptStringFromBytes(encrypted, keybytes, iv);
                        if (decUserId != "keyError")
                        {
                            userId = decUserId;
                        }
                    }
                    catch (Exception) { }
                    if (string.IsNullOrWhiteSpace(userId) || (!keyBased && string.IsNullOrWhiteSpace(password)))
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Invalid Username or Password", returnDt, ""));
                    }

                    loginResponseWithDT userList = new loginResponseWithDT();
                    userList = branch == true ? _tradeWebRepository.BranchUserDetails(userId, password, role) : _tradeWebRepository.UserDetails(userId, password, keyBased, product, role);

                    if (!string.IsNullOrEmpty(userList.message))
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, userList.message, returnDt, ""));
                    }
                    else if (userList.data.Rows.Count > 0)
                    {
                        int expTime = Convert.ToInt32(_configuration["tokenExpireTime"]);
                        var datetimeExp = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, UtilityCommon.GetIndianTimeZone());
                        string checkCondition = "N", sms2FA = "", product2FA = role;
                        string[] allowedRoles = { "Admin", "CrossNet", "EstroNet", "CrossWeb", "EstroWeb", "TradeMobile", "Branch" };
                        string userType = userList.data.Rows[0]["UserType"].ToString();
                        //if (role == "TradeMobile")
                        //{
                        if (loginAs == "B" && (role == "CrossNet" || role == "EstroNet" || role == "Branch"))
                        {
                            sms2FA = objUtility.GetSysParmSt("SMS2FA", "").Trim();
                            if (sms2FA != "")
                            {
                                if (role == "Branch")
                                {
                                    product2FA = "TradeNet";
                                }
                            }
                        }
                        else
                        {
                            sms2FA = objUtility.GetWebParameter("SMS2FA").Trim();
                        }

                        string str2FA = objUtility.GetWebParameter("2FA").Trim();
                        if (str2FA != "")
                        {
                            if (role == "Admin" && str2FA != "W" && str2FA != "B")
                            {
                                sms2FA = "";
                            }
                            else if (role == "TradeMobile" && str2FA != "M" && str2FA != "B")
                            {
                                sms2FA = "";
                            }
                        }

                        if (userType == "checker")
                        {
                            sms2FA = "";
                        }

                        if (!string.IsNullOrEmpty(sms2FA) && allowedRoles.Contains(role))
                        {
                            string dpid = "", tempUserId = userId;
                            string[] userStr = userId.Split("|");
                            if (loginAs == "C" && product == "E" && role == "EstroWeb")
                            {
                                product2FA = "EstroWeb";
                                if (userStr.Length > 1)
                                {
                                    dpid = userStr[0];
                                    userId = userStr[1];
                                }
                            }
                            else if (loginAs == "C" && product == "C" && role == "CrossWeb")
                            {
                                product2FA = "CrossWeb";
                                if (userStr.Length > 1)
                                {
                                    dpid = userStr[0];
                                    userId = userStr[1];
                                }
                            }
                            else if (loginAs == "C" && product == "T")
                            {
                                product2FA = "TradeWeb";
                            }

                            /*strsql = "select count(0) from OTP_Master with (NoLock) where OTP_ClientCode='" + userId + "' and OTP_Product='" + product2FA + "' and OTP_Type = '2FA' and CAST(OTP_SentDate AS DATETIME) + CAST(OTP_SentTime AS DATETIME) >= DATEADD(MINUTE, -15, GETDATE())";
                            DataTable dtCheck = objUtility.OpenDataTable(strsql);
                            if (dtCheck.Rows.Count > 0)
                            {
                                if (Convert.ToInt32(dtCheck.Rows[0][0]) >= 10)
                                {
                                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Too many Attempts", returnDt, ""));
                                }
                            }*/

                            DataTable dtResponse = objUtility.TradeMobileSendOTP(userId, product2FA, "2FA", role);
                            userId = tempUserId;
                            string strStatus = dtResponse.Rows[0]["Status"].ToString();
                            string strMessage = dtResponse.Rows[0]["Message"].ToString();

                            if (strStatus == "Y")
                            {
                                string decryptJwtKey = objUtility.Decrypt(_configuration["Jwt:Key"].ToString());
                                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(decryptJwtKey));
                                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
                                int tokenExpTime = 5;
                                var claims = new[] {
                                        new Claim(JwtRegisteredClaimNames.Sub, userId.ToUpper()),
                                        new Claim(JwtRegisteredClaimNames.Jti, userId.ToUpper()),
                                        new Claim(type: "username", value: userId.ToUpper()),
                                        new Claim(type: "loginas", value: loginAs),
                                        new Claim(type: "product", value: product),
                                        new Claim(type: "loginrole", value: role),
                                        new Claim(type: "branch", value: branch ? "Y" : "N"),
                                        new Claim(type: "userData", value: JsonConvert.SerializeObject(userList.data)),
                                        new Claim(ClaimTypes.Role, "TradeWeb2FA"),
                                        new Claim(type: "otpproduct", value: product2FA),
                                    };
                                var token = new JwtSecurityToken(_configuration["Jwt:Issuer"],
                                    _configuration["Jwt:Issuer"],
                                    claims,
                                    expires: DateTime.UtcNow.AddMinutes(tokenExpTime),
                                    signingCredentials: credentials);

                                string str2FAToken = new JwtSecurityTokenHandler().WriteToken(token);
                                DataTable dtUserData = userList.data;
                                dtUserData.Columns.Add("LoginType", typeof(string));
                                dtUserData.Columns.Add("LoginMessage", typeof(string));
                                dtUserData.Rows[0]["LoginType"] = "2FA";
                                dtUserData.Rows[0]["LoginMessage"] = strMessage;

                                tokenResponse result2fa = new tokenResponse
                                {
                                    status = true,
                                    message = "success",
                                    status_code = (int)HttpStatusCode.OK,
                                    token = str2FAToken,
                                    tokenExpireTime = datetimeExp.AddMinutes(5).ToString("yyyy/MM/dd HH:mm:ss"),
                                    data = dtUserData
                                };
                                return Ok(JsonConvert.SerializeObject(result2fa, Formatting.Indented));
                            }
                            else
                            {
                                return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, strMessage, returnDt, ""));
                            }
                        }
                        //}

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
                                //userId = strUserID.Length > 1 ? strUserID[1] : strUserID[0];
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
                        var guidVal = Guid.NewGuid();
                        if ((role == "Admin" || role == "TradeMobile") && branch == false)
                        {
                            objUtility.ExecuteSQL("update Login_Session set ls_token = '" + guidVal + "', ls_logintype = '" + (role == "Admin" ? "C" : "M") + "', ls_logintm = '" + DateTime.Now.ToString("HH:mm:ss") + "'  Where ls_code = '" + userId + "' And ls_logindt = '" + DateTime.Now.ToString("yyyyMMdd") + "'  And len(ls_token) < 3");
                        }
                        string strUserAccess = "";
                        if (branch)
                        {
                            if (role == "Branch")
                            {
                                strUserAccess = objUtility.GetUserAccess(objUtility.fnFireQueryTradeWeb("User_master", "um_specialrights", "um_user_id", userId, true));
                            }
                            else
                            {
                                strUserAccess = "Branch";
                            }
                        }

                        var tokenString = branch == true ? GenerateJSONWebTokenUser(new TradeWebLoginModel { username = userId, password = password, role = role, guid = guidVal.ToString(), tokenExpTime = expTime, useraccess = strUserAccess }) : GenerateJSONWebToken(new TradeWebLoginModel { username = userId, password = password, role = role, guid = guidVal.ToString(), tokenExpTime = expTime });

                        tokenResponse result = new tokenResponse();
                        result.status = true;
                        result.message = "success";
                        result.status_code = (int)HttpStatusCode.OK;
                        result.token = tokenString;
                        result.tokenExpireTime = datetimeExp.AddMinutes(expTime).ToString("yyyy/MM/dd HH:mm:ss");
                        result.data = userList.data;
                        var jsonData = JsonConvert.SerializeObject(result, Formatting.Indented);
                        return Ok(jsonData);
                    }
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, userList.message, userList, ""));
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }


        [HttpPost("Login_SSO", Name = "Login_SSO")]
        public IActionResult Login_SSO([FromBody] object reqObject)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var jsonString = ((System.Text.Json.JsonElement)reqObject).GetRawText();
                    var valdJsonObj = JsonConvert.DeserializeObject<dynamic>(jsonString.ToString());
                    string TradeWebUName = valdJsonObj["TradeWebUName"];
                    string PANNo = valdJsonObj["PANNo"];
                    string ID = valdJsonObj["ID"];
                    string PDate = valdJsonObj["PDate"];
                    string UserID = valdJsonObj["UserID"];
                    string SessionId = valdJsonObj["SessionId"];
                    string Link = valdJsonObj["Link"];
                    string DefaultMenu = valdJsonObj["DefaultMenu"];
                    string sUserId = valdJsonObj["sUserId"]; // Form request value
                    string sToken = valdJsonObj["sToken"]; // Form request value
                    string userID = valdJsonObj["userID"];
                    string token = valdJsonObj["token"];
                    string product = valdJsonObj["product"];
                    string loginAs = valdJsonObj["loginAs"];
                    string key = valdJsonObj["key"];

                    if (string.IsNullOrWhiteSpace(loginAs) && !string.IsNullOrWhiteSpace(UserID))
                    {
                        loginAs = objUtility.GetLoginAs(UserID);
                    }

                    string Strsql = string.Empty;
                    //// Using Query string with PanNo and userName
                    if (TradeWebUName != null && PANNo != null)
                    {
                        string strUname = string.Empty;
                        string strPanno = string.Empty;
                        strUname = objUtility.mfnReplaceForSQLInjection(objUtility.DecodeFrom64(TradeWebUName));
                        strPanno = objUtility.mfnReplaceForSQLInjection(objUtility.DecodeFrom64(PANNo));

                        Strsql = "select LTrim(RTrim(cm_cd)) ClientCode, LTrim(RTrim(cm_Name)) ClientName, 'user' UserType from client_master with (nolock) where cm_cd='" + strUname.ToString().Trim() + "' and cm_panno='" + strPanno.ToString().Trim() + "'";
                        DataTable dt = new DataTable();
                        dt = objUtility.OpenDataTable(Strsql);
                        if (dt.Rows.Count != 0)
                        {
                            var response = LoginSSOResponse(key, product, loginAs, strUname, dt);
                            return Ok(response);
                        }
                    }

                    //// Using Query string with UserID and PDate(current date)
                    else if (ID != null && PDate != null)
                    {
                        string strID = objUtility.mfnReplaceForSQLInjection(objUtility.DecodeFrom64(ID));
                        string strPDate = objUtility.DecodeFrom64(PDate);

                        if (_configuration["IsTradeWeb"] == "C" || _configuration["IsTradeWeb"] == "E")
                        {
                            //if (Convert.ToString(Session["LiveCross"]).Trim() == "Y")
                            //{
                            //    Strsql = "select cm_cd,cm_pwd,convert(char,convert(datetime,getdate()),103) as DT from client_master with (nolock) where cm_cd='" + strID.ToString().Trim() + "'";
                            //}
                            //else
                            //{
                            Strsql = "select LTrim(RTrim(cm_cd)) ClientCode, LTrim(RTrim(cm_Name)) ClientName, 'user' UserType,convert(char,convert(datetime,getdate()),103) as DT from client_master with (nolock) where cm_companycode+cm_cd='" + strID.ToString().Trim() + "'";
                            //}
                        }
                        else if (_configuration["IsTradeWeb"] == "T" || _configuration["IsTradeWeb"] == "O")
                        {
                            Strsql = "select LTrim(RTrim(cm_cd)) ClientCode, LTrim(RTrim(cm_Name)) ClientName, 'user' UserType,convert(char,convert(datetime,getdate()),103) as DT from client_master with (nolock) where cm_cd='" + strID.ToString().Trim() + "'";
                        }
                        DataTable dt = new DataTable();
                        dt = objUtility.OpenDataTable(Strsql);

                        if (dt.Rows.Count != 0)
                        {
                            if (strPDate == dt.Rows[0]["DT"].ToString().Replace("/", "").Trim())
                            {
                                dt.Columns.Remove("DT");
                                dt.AcceptChanges();
                                if (_configuration["IsTradeWeb"] == "O")
                                {
                                    string guid = Guid.NewGuid().ToString("N");
                                    //Response.Cookies.Add(new HttpCookie("AuthTradeWeb", guid));
                                    Strsql = "update login_session set ls_logoutdt = '" + DateTime.Now.ToString("yyyyMMdd") + "', ls_logouttm = '" + DateTime.Now.ToString("HH:mm:ss") + "' where ls_token = (select top 1 ls_token from Login_Session where ls_code = '" + objUtility.mfnReplaceForSQLInjection(strID).ToUpper() + "' and ls_logintype = 'C' and ls_logoutdt = '' and ls_logouttm = '' order by ls_srno desc)";
                                    objUtility.ExecuteSQL(Strsql);
                                    Strsql = "insert into Login_Session values('" + objUtility.mfnReplaceForSQLInjection(strID).ToUpper() + "', 'C', '" + Strings.Left(ReturnsHost().ToString(), 30) + "', '" + guid + "', '" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HH:mm:ss") + "','','')";
                                    objUtility.ExecuteSQL(Strsql);
                                }
                                var response = LoginSSOResponse(key, product, loginAs, strID, dt);
                                return Ok(response);
                            }
                        }
                    }

                    //// For FTSOSURL   *** Using Query string with UserID, SessionID, Link, Default Value
                    string strparm = objUtility.GetWebParameter("FTSOSURL");
                    if (!String.IsNullOrEmpty(strparm))
                    {
                        if (UserID != null && SessionId != null)
                        {
                            char seperator = '?';
                            string[] separateURL = strparm.Split(seperator);
                            string struserid = objUtility.mfnReplaceForSQLInjection(UserID);
                            System.Collections.Specialized.NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(separateURL[1]);
                            queryString["UserId"] = UserID;
                            queryString["SessionId"] = SessionId;
                            queryString["Link"] = string.IsNullOrEmpty(queryString["Link"]) ? Link : queryString["Link"];
                            string strlnk = separateURL[0] + "?" + queryString.ToString();
                            string strresponse = objUtility.WebRequestTestStr(strlnk);
                            if (!string.IsNullOrEmpty(strresponse))
                            {
                                XmlDocument doc = new XmlDocument();
                                doc.LoadXml(strresponse);
                                string strresponse1 = doc.InnerText;
                                if (strresponse1.Contains("10000AUTHENTICATION"))
                                {
                                    Strsql = "select LTrim(RTrim(cm_cd)) ClientCode, LTrim(RTrim(cm_Name)) ClientName, 'user' UserType  from client_master with (nolock) where cm_cd='" + struserid.ToString().Trim() + "'";
                                    DataTable dt = new DataTable();
                                    dt = objUtility.OpenDataTable(Strsql);
                                    if (dt.Rows.Count > 0)
                                    {
                                        var response = LoginSSOResponse(key, product, loginAs, UserID, dt);
                                        return Ok(response);
                                        //var DefaultMenuDecoded = HttpUtility.UrlDecode(DefaultMenu);
                                        //if (string.IsNullOrEmpty(DefaultMenu))
                                        //{
                                        //    Response.Redirect("~/MasterPages/Home.aspx");
                                        //}
                                        //else
                                        //{
                                        //    if (File.Exists(Server.MapPath("~" + DefaultMenuDecoded)))
                                        //    {
                                        //        Response.Redirect("~" + DefaultMenuDecoded);
                                        //    }
                                        //    else
                                        //    {
                                        //        Response.Redirect("~/MasterPages/Home.aspx");
                                        //    }
                                        //}
                                    }
                                }
                                else
                                {
                                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", "Session has Expired", ""));
                                }
                            }
                        }
                    }

                    //// For SSOURL kmbala   *** Reqest Form with sUserId, sToken 
                    else if (objUtility.GetWebParameter("SSOURLkmbala") != "")
                    {
                        if (!string.IsNullOrEmpty(sUserId) && !string.IsNullOrEmpty(sToken))
                        {
                            string strSUserID = sUserId;
                            string strSToken = sToken;

                            string URI = objUtility.GetWebParameter("SSOURLkmbala");
                            URI = URI.Replace("<sUserId>", strSUserID);
                            URI = URI.Replace("<sToken>", strSToken);
                            var result = "";
                            try
                            {
                                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                                var httpRequest = (HttpWebRequest)WebRequest.Create(URI);
                                httpRequest.Method = "POST";

                                var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                                {
                                    result = streamReader.ReadToEnd();
                                }
                            }
                            catch (Exception ex)
                            {
                                return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", ex.Message.ToString(), ""));
                            }
                            if (result.Trim() == "TRUE")
                            {
                                var response = LoginSSOResponse(key, product, loginAs, sUserId, new DataTable());
                                return Ok(response);
                            }
                            else
                            {
                                return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", "Token has Expired", ""));
                            }
                        }
                    }

                    //// For SSOURL SYMPNY   *** Using Query string with userID, token, Link, 
                    else if (objUtility.GetWebParameter("SSOURLSYMPNY") != "")
                    {
                        if (!string.IsNullOrEmpty(userID) && !string.IsNullOrEmpty(token))
                        {
                            string URI = objUtility.GetWebParameter("SSOURLSYMPNY");
                            string strUserID = userID;
                            string strToken = token;
                            try
                            {
                                JObject jObj = new JObject
                                    {
                                        new JProperty("UserId", strUserID),
                                        new JProperty("Token", strToken)
                                    };
                                string strJson = jObj.ToString();

                                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                                var httpRequest = (HttpWebRequest)WebRequest.Create(URI);
                                httpRequest.ContentType = "application/json";
                                httpRequest.Method = "POST";
                                httpRequest.Headers.Add("Authorization", strToken);
                                using (StreamWriter streamWriter = new StreamWriter(httpRequest.GetRequestStream()))
                                {
                                    streamWriter.Write(strJson);
                                }
                                var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                                httpResponse.Close();
                                if (httpResponse.StatusCode == HttpStatusCode.OK)
                                {
                                    var response = LoginSSOResponse(key, product, loginAs, userID, new DataTable());
                                    return Ok(response);
                                }
                            }
                            catch (WebException ex)
                            {
                                return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", "Token has Expired", ""));
                            }
                        }
                    }

                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", "UserId not found.", ""));
                }
                catch (Exception ex)
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "TradeWeb2FA")]
        [HttpPost("Login_2FA", Name = "Login_2FA")]
        public IActionResult Login_2FA(string otp)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var loginAs = tokenS.Claims.First(claim => claim.Type == "loginas").Value;
                    var product = tokenS.Claims.First(claim => claim.Type == "product").Value;
                    var otpProduct = tokenS.Claims.First(claim => claim.Type == "otpproduct").Value;
                    var role = tokenS.Claims.First(claim => claim.Type == "loginrole").Value;
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
                            return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "OTP has been expired", returnDt, ""));
                        }

                        int expTime = Convert.ToInt32(_configuration["tokenExpireTime"]);
                        var datetimeExp = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, UtilityCommon.GetIndianTimeZone());

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
                        var guidVal = Guid.NewGuid();
                        if (role == "Admin" && branch == false)
                            objUtility.ExecuteSQL("update Login_Session set ls_token = '" + guidVal + "', ls_logintype = 'C', ls_logintm = '" + DateTime.Now.ToString("HH:mm:ss") + "'  Where ls_code = '" + userId + "' And ls_logindt = '" + DateTime.Now.ToString("yyyyMMdd") + "'  And len(ls_token) < 3");

                        string strUserAccess = "";
                        if (branch)
                        {
                            if (role == "Branch")
                            {
                                strUserAccess = objUtility.GetUserAccess(objUtility.fnFireQueryTradeWeb("User_master", "um_specialrights", "um_user_id", userId, true));
                            }
                            else
                            {
                                strUserAccess = "Branch";
                            }
                        }
                        var tokenString = branch == true ? GenerateJSONWebTokenUser(new TradeWebLoginModel { username = userId, password = password, role = role, guid = guidVal.ToString(), tokenExpTime = expTime, useraccess = strUserAccess }) : GenerateJSONWebToken(new TradeWebLoginModel { username = userId, password = password, role = role, guid = guidVal.ToString(), tokenExpTime = expTime });

                        Strsql = "update OTP_Master set OTP_Status='M' where OTP_Identity='" + strIdentity + "'";
                        objUtility.ExecuteSQL(Strsql);

                        tokenResponse result = new tokenResponse();
                        result.status = true;
                        result.message = "success";
                        result.status_code = (int)HttpStatusCode.OK;
                        result.token = tokenString;
                        result.tokenExpireTime = datetimeExp.AddMinutes(expTime).ToString("yyyy/MM/dd HH:mm:ss");
                        result.data = userData;
                        var jsonData = JsonConvert.SerializeObject(result, Formatting.Indented);
                        return Ok(jsonData);
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "OTP Mismached", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), returnDt, ""));
                }
            }
            return BadRequest();
        }


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

        /// <summary>
        /// Login validate Password
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpPost("Login_GetOTP", Name = "Login_GetOTP")]
        public IActionResult Login_GetOTP(string userId, string otpType)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (string.IsNullOrEmpty(userId))
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", "UserId should not be blank!", ""));
                    }
                    userId = objUtility.mfnReplaceForSQLInjection(userId.Trim());
                    var getData = _tradeWebRepository.Login_GetPassword(userId, otpType);
                    if (getData.status == "success")
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", getData, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        [HttpPost("Login_Password_GenerateOTP", Name = "Login_Password_GenerateOTP")]
        public IActionResult Login_Password_GenerateOTP(string userId, string mode)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    userId = objUtility.mfnReplaceForSQLInjection(userId);
                    mode = objUtility.mfnReplaceForSQLInjection(mode);
                    var result = _tradeWebRepository.Login_Password_GenerateOTP(userId, mode);
                    if (result != "failed")
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", result, ""));
                    }
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        [HttpPost("Login_Password_Update", Name = "Login_Password_Update")]
        public IActionResult Login_Password_Update(string userId, string OTP, string oldPassword, string newPassword)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    userId = objUtility.mfnReplaceForSQLInjection(userId);
                    OTP = objUtility.mfnReplaceForSQLInjection(OTP);
                    oldPassword = objUtility.mfnReplaceForSQLInjection(oldPassword);
                    newPassword = objUtility.mfnReplaceForSQLInjection(newPassword);
                    var result = _tradeWebRepository.Login_Password_Update(userId, OTP, oldPassword, newPassword);
                    if (result != "failed")
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", result, ""));
                    }
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpPost("ChangePassword", Name = "ChangePasswordWeb")]
        public IActionResult ChangePassword(ChangePassword request)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var clientcd = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var compCode = tokenS.Claims.First(claim => claim.Type == "companyCode").Value;
                    request.newPassword = objUtility.mfnReplaceForSQLInjection(request.newPassword);
                    request.currentPassword = objUtility.mfnReplaceForSQLInjection(request.currentPassword);
                    var getData = _tradeWebRepository.ChangePassword(clientcd, compCode, request.currentPassword, request.newPassword);
                    if (getData != null)
                    {
                        if (getData.Contains("succes"))
                        {
                            return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                        }
                        else
                        {
                            return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", getData, ""));
                        }
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

        [HttpGet("UserRole", Name = "UserRole")]
        public IActionResult UserRole()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    JwtSecurityToken token = GetToken();
                    var userId = token.Claims.First(claim => claim.Type == "username").Value;

                    var result = _tradeWebRepository.UserRole(userId);
                    if (result != "failed")
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", result, ""));
                    }
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        [HttpGet("Login2Factor", Name = "Login2Factor")]
        public IActionResult Login2Factor(string userId)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    userId = objUtility.mfnReplaceForSQLInjection(userId);
                    var result = _tradeWebRepository.Login_GetPassword(userId, "2FA");
                    if (result != "failed")
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", result, ""));
                        //return Ok(result);
                    }
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        [HttpGet("XtreamLivePOS", Name = "XtreamLivePOS")]
        public IActionResult XtreamLivePOS()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    JwtSecurityToken token = GetToken();
                    var userId = token.Claims.First(claim => claim.Type == "username").Value;

                    var result = _tradeWebRepository.XtreamLivePOS(userId);
                    if (result != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", result, ""));
                    }
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        [HttpGet("AdditionalMenu", Name = "AdditionalMenu")]
        public IActionResult AdditionalMenu()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    JwtSecurityToken token = GetToken();
                    var userId = token.Claims.First(claim => claim.Type == "username").Value;

                    var result = _tradeWebRepository.AdditionalMenu(userId);
                    if (result != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", result, ""));
                    }
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        [HttpGet("Product", Name = "Product")]
        public IActionResult GetProduct()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var result = _tradeWebRepository.Product();
                    if (result != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", result, ""));
                    }
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        #endregion

        #region Ledger Api

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("Ledger_Year", Name = "Ledger_Year")]
        public IActionResult Ledger_Year()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var getData = _tradeWebRepository.Ledger_Year();
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <returns></returns>
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("Ledger_Summary", Name = "Ledger_Summary")]
        public IActionResult Ledger_Summary(string type, string fromDate, string toDate)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    JwtSecurityToken token = GetToken();
                    var CompCode = token.Claims.First(claim => claim.Type == "companyCode").Value;
                    var userId = token.Claims.First(claim => claim.Type == "username").Value;
                    type = objUtility.mfnReplaceForSQLInjection(type);
                    fromDate = objUtility.mfnReplaceForSQLInjection(fromDate);
                    toDate = objUtility.mfnReplaceForSQLInjection(toDate);
                    var dataList = _tradeWebRepository.Ledger_Summary(userId, type, fromDate, toDate);
                    var strDataList = Newtonsoft.Json.JsonConvert.SerializeObject(dataList).Replace(@"\", String.Empty);
                    if (dataList != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", dataList, ""));
                    }
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpPost("Ledger_Detail", Name = "Ledger_Detail")]
        public IActionResult Ledger_Detail(LedgerDetailsModel model /*string fromDate, string toDate, string type_cesCd*/)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userName = tokenS.Claims.First(claim => claim.Type == "username").Value;

                    var getData = _tradeWebRepository.Ledger_Detail(userName, model, objUtility.mfnReplaceForSQLInjection(model.FromDate), objUtility.mfnReplaceForSQLInjection(model.ToDate));
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpPost("Ledger_Type", Name = "Ledger_Type")]
        public IActionResult Ledger_Type()
        {
            if (ModelState.IsValid)
            {
                try
                {

                    var getData = _tradeWebRepository.Ledger_Type();
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

        #endregion

        #region OutStanding Api
        /// <summary>
        ///  OutStanding data 
        /// </summary>
        /// <param name="AsOnDt"></param>
        /// <returns></returns>
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("OutStandingPosition", Name = "OutStandingPosition")]
        public IActionResult OutStandingPosition(string AsOnDt)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    AsOnDt = objUtility.mfnReplaceForSQLInjection(AsOnDt);
                    var getData = _tradeWebRepository.OutStandingPosition(userId, AsOnDt);
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

        /// <summary>
        /// OutStanding details data
        /// </summary>
        /// <param name="CESCd"></param>
        /// <param name="seriesId"></param>
        /// <returns></returns>
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("OutStandingPosition_Detail", Name = "OutStandingPosition_Detail")]
        public IActionResult OutStandingPosition_Detail(string CESCd, string seriesId)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    CESCd = objUtility.mfnReplaceForSQLInjection(CESCd);
                    seriesId = objUtility.mfnReplaceForSQLInjection(seriesId);
                    var getData = _tradeWebRepository.OutStandingPosition_Detail(userId, seriesId, CESCd);
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
        #endregion

        #region ReKYC API

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "ReKYC,Admin")]
        [HttpPost("ReKYC", Name = "ReKYC")]
        public IActionResult ReKYC(ReKYCModel req)
        {
            if (ModelState.IsValid)
            {
                long srNo = 0;
                try
                {
                    srNo = objUtility.InsertLog(System.Text.Json.JsonSerializer.Serialize(req), "");
                    if (!objUtility.mfnGetSysSplFeature("TPI") && !objUtility.mfnGetSysSplFeature("RKC"))
                    {
                        objUtility.UpdateLog(srNo, "You are not authorise to use TradeWebAPI Web Service,Please contact SecMark.", "");
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "You are not authorise to use TradeWebAPI Web Service,Please contact SecMark.", returnDt, ""));
                    }
                    CommonModel model = new CommonModel();
                    ReKYCRequest rekycReq = new ReKYCRequest();

                    model.ProjectName = "TradeWebAPI";
                    model.ModuleName = "ReKYC";
                    model.FunctionName = req.FunctionName.Trim();
                    model.RequestString = req.RequestString.ToString();
                    model.UserID = "API";
                    model.RequestSource = "M";
                    model.RequestUniqueID = "1";
                    JArray result = _tradeWebRepository.Execute(model);
                    if (result != null)
                    {
                        if (model.FunctionName == "PostClientClosureMaker")
                        {
                            string remark = result[0]["Remark"].ToString();
                            JObject jsonObj = JObject.Parse(req.RequestString.ToString());
                            string userId = jsonObj["ClientCode"]?.ToString();
                            string esignUrl = jsonObj["EsignReturnUrl"]?.ToString();
                            string newBOID = jsonObj["BOID"]?.ToString();
                            string accountType = jsonObj["ClosureType"]?.ToString();
                            string cmrBase64 = jsonObj["CMRAttachment"]?.ToString();
                            var step = _tradeWebRepository.ReKYC_GetSteps(userId, "rm_step", "Account Closure");
                            step = step == "" ? 0 : step;
                            step = Convert.ToInt32(step);
                            XDocument xdocData = XDocument.Parse($"<X_Data></X_Data>");
                            xdocData.Root.Add(new XElement("ClientCode", userId));
                            xdocData.Root.Add(new XElement("AccountType", accountType));
                            xdocData.Root.Add(new XElement("HoldingBal", jsonObj["HoldingBalance"].ToString()));
                            var xml = "<dsXml>" + xdocData.Root.ToString(SaveOptions.DisableFormatting) + "</dsXml>";

                            if (step < 3 || remark.Contains("submit"))
                            {
                                var pdfData = Generate_AccountClosurePDF(xml, esignUrl, newBOID, userId, accountType, cmrBase64);
                                EsignResponse esignResponse = new EsignResponse();
                                if (pdfData != null)
                                {
                                    esignResponse.EsignUrl = pdfData.EsignUrl;
                                    esignResponse.UnsignedPdf = pdfData.UnsignedPdf;
                                    esignResponse.Status = pdfData.Status;
                                    esignResponse.ClientCode = userId;
                                    esignResponse.Remark = pdfData.Remark;
                                    if (esignResponse.Status == "Y")
                                    {
                                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", esignResponse, ""));
                                    }
                                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", esignResponse, ""));
                                }
                                return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "Not found", result, ""));
                            }
                            else if (step == 3)
                            {
                                string refN = _tradeWebRepository.ReKYC_MasterTable(userId, "Pending", "N", 2, "Account Closure PDF created, Call Esign Request for Account Closure unsigned pdf", "Account Closure");
                                var strQuery = $"select * from Client_ModifyAttach (NoLock) where ma_cmcd = '{userId}' and ma_filename = 'UnSignedPdf' and ma_refno=" + refN + " And rm_RequestType = 'Account Closure' ";
                                var tempDb = objUtility.OpenDataTable(strQuery);
                                if (tempDb.Rows.Count > 0)
                                {
                                    var pdfBase64 = System.Convert.ToBase64String((byte[])tempDb.Rows[0]["ma_proof"]);
                                    System.Data.DataSet ds = _tradeWebRepository.XMLCommonSPCall(xml, "SP_AccountTransferPDF");
                                    if (ds.Tables[0].Rows.Count > 0)
                                    {
                                        string mobNo = ds.Tables[0].Rows[0]["MobileNo"].ToString();
                                        string fullName = ds.Tables[0].Rows[0]["FullName"].ToString();
                                        string yob = ds.Tables[0].Rows[0]["BirthYear"].ToString();
                                        var esign_detail = _tradeWebRepository.UploadPdfForEsignSetu(refN, pdfBase64, mobNo, fullName, yob, esignUrl, userId + "_AccountClosure.pdf", userId, "Account Closure");
                                        EsignResponse esignResponse = new EsignResponse();
                                        esignResponse.EsignUrl = esign_detail;
                                        esignResponse.UnsignedPdf = pdfBase64;
                                        esignResponse.Status = "Y";
                                        esignResponse.ClientCode = userId;
                                        esignResponse.Remark = "Pdf uploaded on server, Please Esign the pdf.";
                                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", esignResponse, ""));
                                    }
                                }
                            }
                            else if (step == 4)
                            {
                                string desc = _tradeWebRepository.ReKYC_GetSteps(userId, "rm_desc", "Account Closure");
                                string refN = _tradeWebRepository.ReKYC_MasterTable(userId, "Pending", "N", 4, desc, "Account Closure");
                                string strSql = string.Empty;
                                strSql = "Select * From Client_ModifyAttach (NoLock) where ma_cmcd = '" + userId + "' and ma_status = 'N' and ma_filename = 'EsignRequest' and ma_refNo = " + refN;
                                DataTable dt = objUtility.OpenDataTable(strSql);
                                if (dt.Rows.Count > 0)
                                {
                                    EsignResponse esignResponse = new EsignResponse();
                                    var getEsignReq = Newtonsoft.Json.Linq.JObject.Parse(Encoding.UTF8.GetString((byte[])dt.Rows[0]["ma_proof"]).ToString());
                                    var signers = (string)getEsignReq["signers"][0]["url"];
                                    var pdfBase64 = System.Convert.ToBase64String((byte[])dt.Rows[0]["ma_proof"]);
                                    esignResponse.EsignUrl = signers;
                                    esignResponse.UnsignedPdf = pdfBase64;
                                    esignResponse.Status = "Y";
                                    esignResponse.ClientCode = userId;
                                    esignResponse.Remark = "You have not done esign, Please do esign for complete your Rekyc";
                                    return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", esignResponse, ""));
                                }
                                return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "failed", result, ""));
                            }
                            return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "failed", result, ""));
                        }
                        objUtility.UpdateLog(srNo, JsonConvert.SerializeObject(result), "");
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", result, ""));
                    }
                    else
                    {
                        objUtility.UpdateLog(srNo, "No Record Found", "");
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                    }
                }
                catch (Exception ex)
                {
                    objUtility.UpdateLog(srNo, "", ex.Message.ToString());
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        #endregion

        #region TradeWeb CommonGrid API

        //[Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        //[HttpPost("TradeWeb", Name = "TradeWeb")]
        //public IActionResult TradeWebGrid(TradeWebDataGridRequest req)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            var tokenS = GetToken();
        //            var clientCode = tokenS.Claims.First(claim => claim.Type == "username").Value;

        //            TradeWebDataGridResponse getData = _tradeWebRepository.TradeWebCommonGrid(clientCode, req);

        //            if (string.IsNullOrWhiteSpace(getData.Message))
        //            {
        //                return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData.Data, ""));
        //            }
        //            else
        //            {
        //                return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, getData.Message, returnDt, ""));
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
        //        }
        //    }
        //    return BadRequest();
        //}

        [HttpPost("SendEmail", Name = "SendEmail")]
        public IActionResult SendEmail([FromBody] EmailSmsReqModel reqObject)
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
                            if (objUtility.GetWebParameter("SECURITYPROT").Trim() == "TLS12")
                            {
                                ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072 | (SecurityProtocolType)48;
                            }
                            HttpWebRequest http = (HttpWebRequest)WebRequest.Create(strURLLink);
                            HttpWebResponse response = (HttpWebResponse)http.GetResponse();
                            StreamReader sr = new StreamReader(response.GetResponseStream());
                            string content = sr.ReadToEnd();
                            string strresponse = content;
                            sr.Close();

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
                                var jsonString = ((System.Text.Json.JsonElement)reqObject.requestObject).GetRawText();
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
                                smtp.Send(Msg);
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

        //[Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpPost("fillDropDown", Name = "fillDropDown")]
        public IActionResult fillDropDown(string pageName, string ddlName, object jsonObj)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    //var tokenS = GetToken();
                    //var clientCode = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    DataTable dt = new DataTable();
                    dt.Columns.Add("KeyName", typeof(string));
                    dt.Columns.Add("KeyValue", typeof(string));
                    string Strsql = " select em_name from Entity_Master where em_cd=(select min(em_cd) from Entity_master Where Len(Ltrim(Rtrim(em_cd))) = 1)";

                    if (_configuration["IsTradeWeb"] == "T" || _configuration["IsTradeWeb"] == "O")
                    {
                        if (Convert.ToInt16(objUtility.fnFireQueryTradeWeb("Entity_master", "count(0)", " em_cd='B' and em_name like 'SYKES & RAY EQUITIES%' and 1", "1", true)) > 0)
                            Strsql = " select em_name from Entity_Master with (nolock) where em_cd='B'";
                        else
                            Strsql = " select em_name from Entity_Master with (nolock) where em_cd =(select min(em_cd) from Entity_master)";
                    }
                    System.Data.DataSet ObjDataSet = objUtility.OpenDataSet(Strsql);
                    dt.Rows.Add("Available in My Demat A/c", "Dp");
                    if (ObjDataSet.Tables[0].Rows.Count > 0)
                    {
                        dt.Rows.Add("Lying with " + ObjDataSet.Tables[0].Rows[0]["em_name"].ToString().Trim() + " for various Reasons", "Ben");
                    }
                    else
                    {
                        dt.Rows.Add("Lying with your various Reasons", "Ben");
                    }
                    if (dt.Rows.Count > 0)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", dt, ""));
                    }
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "notfound", "No record found.", ""));
                }
                catch (Exception ex)
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        #endregion

        [HttpGet("GetExchSeg", Name = "GetExchSeg")]
        public IActionResult GetExchSeg()
        {
            try
            {
                var getData = _tradeWebRepository.GetExchSeg();
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CommonAPI")]
        [HttpPost("CommonAPI", Name = "CommonAPI")]
        public IActionResult CommonAPI(CommonAPIRequest req)
        {
            if (ModelState.IsValid)
            {
                long srNo = 0;
                string connetionString = objUtility.GetConnectionStr();
                try
                {
                    using (Microsoft.Data.SqlClient.SqlConnection conn = new Microsoft.Data.SqlClient.SqlConnection(connetionString))
                    {
                        conn.Open();
                        srNo = objUtility.InsertLog(0, "COMMONAPI", "", System.Text.Json.JsonSerializer.Serialize(req), "", "", "", conn);
                    }
                    var tokenS = GetToken();
                    var clientCode = tokenS.Claims.First(claim => claim.Type == "username").Value;

                    bool blnAuth = _tradeWebRepository.CommonAPI_Authorize(req.ModuleName.Trim());

                    if (!blnAuth)
                    {
                        objUtility.UpdateLog(srNo, "You are not authorise to use TradeWebAPI Web Service,Please contact SecMark.", "");
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "You are not authorise to use TradeWebAPI Web Service,Please contact SecMark.", returnDt, ""));
                    }

                    CommonModel model = new CommonModel();
                    ReKYCRequest rekycReq = new ReKYCRequest();

                    model.ProjectName = req.ProjectName.Trim();
                    model.ModuleName = req.ModuleName.Trim();
                    model.FunctionName = req.FunctionName.Trim();
                    model.RequestString = req.RequestString.ToString();
                    model.UserID = clientCode;
                    model.RequestSource = "M";
                    model.RequestUniqueID = "1";

                    JArray result = _tradeWebRepository.Execute(model);
                    CommonAPIResponse response = result[0].ToObject<CommonAPIResponse>();

                    using (Microsoft.Data.SqlClient.SqlConnection conn = new Microsoft.Data.SqlClient.SqlConnection(connetionString))
                    {
                        conn.Open();
                        if (response != null)
                        {
                            objUtility.UpdateLog(srNo, "", JsonConvert.SerializeObject(response), "", conn);
                            if (response.ResponseFlag == "S")
                            {
                                return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", response.DATA, ""));
                            }
                            else
                            {
                                return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", response.ResponseMessage, ""));
                            }
                        }
                        else
                        {
                            objUtility.UpdateLog(srNo, "", "No Record Found", "", conn);
                            return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", returnDt, ""));
                        }
                    }
                }
                catch (Exception ex)
                {
                    using (Microsoft.Data.SqlClient.SqlConnection conn = new Microsoft.Data.SqlClient.SqlConnection(connetionString))
                    {
                        conn.Open();
                        if (srNo > 0)
                        {
                            objUtility.UpdateLog(srNo, "", "", ex.Message.ToString(), conn);
                        }
                        else
                        {
                            objUtility.InsertLog(0, "COMMONAPI", "", System.Text.Json.JsonSerializer.Serialize(req), "", ex.Message.ToString(), "", conn);
                        }
                    }
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        [HttpGet("GetDPID", Name = "GetDPID")]
        public IActionResult GetDPID()
        {
            try
            {
                var getData = _tradeWebRepository.GetDPID();
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "EstroWeb")]
        [HttpPost, Route("EstroWeb")]
        [Consumes("application/xml")]
        public async Task<IActionResult> EstroWeb()
        {
            if (ModelState.IsValid)
            {
                long logSrNo = 0;
                string logRespMessage = "", logErrorMessage = "";
                string connetionString = objUtility.GetConnectionStr();
                try
                {
                    string userId = "", companyCode = "";
                    JwtSecurityToken token = GetToken();
                    var userName = token.Claims.First(claim => claim.Type == "username").Value;
                    string[] strUserID = userName.Split("|");
                    if (strUserID.Length > 1)
                    {
                        userId = strUserID[1];
                        companyCode = strUserID[0];
                    }
                    else
                    {
                        userId = strUserID[0];
                    }
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
                        jsonString = xml.GetElementsByTagName("J_Api")[0].InnerText;

                        if (!string.IsNullOrWhiteSpace(jsonString))
                        {
                            // Replace smart quotes if any
                            jsonString = "{" + jsonString.Replace("\u201C", "\"").Replace("\u201D", "\"") + "}";
                            JApi jApiObject;
                            try
                            {
                                jApiObject = JsonConvert.DeserializeObject<JApi>(jsonString);
                                jApiObject.UserId = userId.Trim();
                                xml.DocumentElement["J_Api"].InnerText = JsonConvert.SerializeObject(jApiObject).Replace("{", "").Replace("}", "");
                            }
                            catch (JsonException ex)
                            {
                                return BadRequest($"Invalid JSON in J_Api element: {ex.Message}");
                            }
                        }
                        string strModuleName = jUiObject.ActionName.Trim();
                        string strFunctionName = jUiObject.Option.Trim();

                        if (strFunctionName == "ChangePassword")
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
                                logSrNo = objUtility.InsertLog(0, "EstroWeb", "", strXMLCP, "", "", clientCode, conn);
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
                        var filterxml = xml.GetElementsByTagName("X_Filter")[0]?.InnerXml;
                        XDocument xdoc = XDocument.Parse($"<root>{filterxml}</root>");
                        xdoc.Root.Element("ClientCode")?.Remove();
                        xdoc.Root.Add(new XElement("ClientCode", userId));
                        if (_configuration["IsEstroOffLine"] == "Y")
                        {
                            xdoc.Root.Element("CompanyCode")?.Remove();
                            xdoc.Root.Add(new XElement("CompanyCode", companyCode));
                        }
                        xml.DocumentElement["X_Filter"].InnerXml = xdoc.Root.ToString(SaveOptions.DisableFormatting).Replace("<root>", "").Replace("</root>", "");

                        string strXML = objUtility.SerializeToXml(xml, omitXmlDeclaration: true, indent: false, encoding: "utf-16");
                        using (Microsoft.Data.SqlClient.SqlConnection conn = new Microsoft.Data.SqlClient.SqlConnection(connetionString))
                        {
                            conn.Open();
                            logSrNo = objUtility.InsertLog(0, "EstroWeb", "", strXML, "", "", userId, conn);
                        }
                        var getData = _tradeWebRepository.CommonAPIrepoCall(strModuleName, strFunctionName, strXML);
                        if (getData != null)
                        {
                            logRespMessage = "Success";
                            return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "", data = getData.Data, datarows = getData.DataRows }, Formatting.Indented));
                        }
                        else
                        {
                            logRespMessage = "No record found";
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossWeb")]
        [HttpPost, Route("CrossWeb")]
        [Consumes("application/xml")]
        public async Task<IActionResult> CrossWeb()
        {
            if (ModelState.IsValid)
            {
                long logSrNo = 0;
                string logRespMessage = "", logErrorMessage = "";
                string connetionString = objUtility.GetConnectionStr();
                try
                {
                    string userId = "", companyCode = "";
                    JwtSecurityToken token = GetToken();
                    var userName = token.Claims.First(claim => claim.Type == "username").Value;
                    string[] strUserID = userName.Split("|");
                    if (strUserID.Length > 1)
                    {
                        userId = strUserID[1];
                        companyCode = strUserID[0];
                    }
                    else
                    {
                        userId = strUserID[0];
                    }
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
                        jsonString = xml.GetElementsByTagName("J_Api")[0].InnerText;

                        if (!string.IsNullOrWhiteSpace(jsonString))
                        {
                            // Replace smart quotes if any
                            jsonString = "{" + jsonString.Replace("\u201C", "\"").Replace("\u201D", "\"") + "}";
                            JApi jApiObject;
                            try
                            {
                                jApiObject = JsonConvert.DeserializeObject<JApi>(jsonString);
                                jApiObject.UserId = userId.Trim();
                                xml.DocumentElement["J_Api"].InnerText = JsonConvert.SerializeObject(jApiObject).Replace("{", "").Replace("}", "");
                            }
                            catch (JsonException ex)
                            {
                                return BadRequest($"Invalid JSON in J_Api element: {ex.Message}");
                            }
                        }
                        string strModuleName = jUiObject.ActionName.Trim();
                        string strFunctionName = jUiObject.Option.Trim();

                        if (strFunctionName == "ChangePassword")
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
                                logSrNo = objUtility.InsertLog(0, "CrossWeb", "", strXMLCP, "", "", clientCode, conn);
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
                        var filterxml = xml.GetElementsByTagName("X_Filter")[0]?.InnerXml;
                        XDocument xdoc = XDocument.Parse($"<root>{filterxml}</root>");
                        xdoc.Root.Element("ClientCode")?.Remove();
                        xdoc.Root.Add(new XElement("ClientCode", userId));
                        if (_configuration["IsCrossOffLine"] == "Y")
                        {
                            xdoc.Root.Element("CompanyCode")?.Remove();
                            xdoc.Root.Add(new XElement("CompanyCode", companyCode));
                        }
                        xml.DocumentElement["X_Filter"].InnerXml = xdoc.Root.ToString(SaveOptions.DisableFormatting).Replace("<root>", "").Replace("</root>", "");

                        string strXML = objUtility.SerializeToXml(xml, omitXmlDeclaration: true, indent: false, encoding: "utf-16");
                        using (Microsoft.Data.SqlClient.SqlConnection conn = new Microsoft.Data.SqlClient.SqlConnection(connetionString))
                        {
                            conn.Open();
                            logSrNo = objUtility.InsertLog(0, "CrossWeb", "", strXML, "", "", userId, conn);
                        }
                        var getData = _tradeWebRepository.CommonAPIrepoCall(strModuleName, strFunctionName, strXML);
                        if (getData != null)
                        {
                            logRespMessage = "Success";
                            return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "", data = getData.Data, datarows = getData.DataRows }, Formatting.Indented));
                        }
                        else
                        {
                            logRespMessage = "No record found";
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin,TradeMobile,ComAPI,CrossNet,EstroNet,CrossWeb,EstroWeb,Branch")]
        [HttpPost, Route("TradeWeb")]
        [Consumes("application/xml")]
        [TypeFilter(typeof(ValidateUser), Arguments = new object[] { "TradeWeb" })]
        [ServiceFilter(typeof(EncryptResponseAttribute))]
        public async Task<IActionResult> TradeWeb()
        {
            if (ModelState.IsValid)
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
                    if (role == "TradeMobile")
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
                    using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
                    {
                        var xmlStr = await reader.ReadToEndAsync();
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

                            string strJson = JsonConvert.SerializeObject(reqJson);

                            var finljson = strJson.Replace(@"\", "");
                            finljson = finljson.Replace("\"[{", "[{");
                            finljson = finljson.Replace("}]\"", "}]");

                            var EncryptedData = objUtility.EDIS_Encrypt(finljson, aesKey);
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

                            DataTable dtResponse = objUtility.TradeMobileSendOTP(ClientCd, "TradeWeb", OTPType, "Admin");
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
                            if (strFunctionName == "AccountOpenGenrate") pdfType = "ACCOUNTOPENINGLETTER";
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
                        else if (strReportDisplay == "J")//// Using Table Loop
                        {
                            string jsonData = getData.Data.Tables[0].Rows[0]["Column1"].ToString();
                            typstResponse = _tradeWebRepository.GenerateTypstAccountOpeningJson(strModuleName, strFunctionName, jsonData);
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
            return BadRequest();
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "CrossNet")]
        [HttpPost, Route("CrossNet")]
        [Consumes("application/xml")]
        public async Task<IActionResult> CrossNet()
        {
            if (ModelState.IsValid)
            {
                long logSrNo = 0;
                string logRespMessage = "", logErrorMessage = "";
                string connetionString = objUtility.GetConnectionStr();
                try
                {
                    string userId = "";
                    string strXML = "";
                    string strModuleName = "";
                    string strFunctionName = "";
                    JwtSecurityToken token = GetToken();
                    var userName = token.Claims.First(claim => claim.Type == "username").Value;
                    var role = token.Claims.First(claim => claim.Type == ClaimTypes.Role).Value;
                    string[] strUserID = userName.Split("|");
                    userId = userName;
                    using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
                    {
                        var xmlStr = await reader.ReadToEndAsync();
                        XmlDocument xml = new XmlDocument();
                        xml.LoadXml(xmlStr);

                        CommonXMLResponse xmlResponse = objUtility.CreateCommonXML(xml, role, userId);
                        if (xmlResponse.Success)
                        {
                            strXML = objUtility.SerializeToXml(xml, omitXmlDeclaration: true, indent: false, encoding: "utf-16");
                            strModuleName = xmlResponse.ModuleName;
                            strFunctionName = xmlResponse.FunctionName;
                        }
                        else
                        {
                            return BadRequest(xmlResponse.Message);
                        }

                        using (Microsoft.Data.SqlClient.SqlConnection conn = new Microsoft.Data.SqlClient.SqlConnection(connetionString))
                        {
                            conn.Open();
                            logSrNo = objUtility.InsertLog(0, "CrossNet", "", strXML, "", "", userId, conn);
                        }
                        var getData = _tradeWebRepository.CommonAPIrepoCall(strModuleName, strFunctionName, strXML);
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
                                    return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = getData.Success, message = getData.Message, data = getData.Data, datarows = getData.DataRows }, Formatting.Indented));
                                }
                                var result = new
                                {
                                    rs0 = column1Json
                                };
                                return Ok(JsonConvert.SerializeObject(new { success = getData.Success, message = getData.Message, data = result, datarows = getData.DataRows }, Formatting.Indented));
                            }
                            else
                            {
                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = getData.Success, message = getData.Message, data = getData.Data, datarows = getData.DataRows }, Formatting.Indented));
                            }
                        }
                        else
                        {
                            logRespMessage = "No record found";
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

        [HttpPost, Route("InitializeLogin")]
        [Consumes("application/xml")]
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

                            var response = _tradeWebRepository.Login(userId, password, epassword, key, loginAs, product, icpv, feature, "", ip);
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
                            var datetimeExp = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, UtilityCommon.GetIndianTimeZone());

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

                            var currDatetime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, UtilityCommon.GetIndianTimeZone());
                            if (string.IsNullOrWhiteSpace(storedToken) || expiry < currDatetime)
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

                            dtRes.Rows.Add(tokenString, refreshToken, datetimeExp.AddMinutes(refreshExpTime).ToString("yyyy/MM/dd HH:mm:ss"));

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

                            DataTable dtResponse = objUtility.TradeMobileSendOTP(clientCode, ProductFP, "FP", "");
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
                                var datetimeExp = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, UtilityCommon.GetIndianTimeZone());
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
                                    //var datetimeExp = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, UtilityCommon.GetIndianTimeZone());

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
                                    strsql = "select * from Other_Products where OP_Product in ('Cross','Estro','Commex') and OP_Status = 'A'";
                                    DataTable dtOP = objUtility.OpenDataTable(strsql);
                                    foreach (DataRow dr in dtOP.Rows)
                                    {
                                        if (dr["OP_Product"].ToString().Trim() == "Cross")
                                        {
                                            strDPParamName = "CrossWeb";

                                            /*if (Message != "")
                                            {
                                                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = Message, data = null, datarows = null }, Formatting.Indented));
                                            }*/
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

                                                strDPParamDBDate = objUtility.GetWebParameter(strDPParamName + "DBDT", sqlcon);
                                                DateTime DPParamDBDate;
                                                bool isDPQueryDateValid = DateTime.TryParseExact(strDPParamDBDate, "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DPParamDBDate);

                                                //var client = new HttpClient();
                                                urlWithParams = $"{TplusAPIURL}/Tplus/Versions?ApplicationName={Uri.EscapeDataString(strDPParamName)}&Date={Uri.EscapeDataString(DPParamDBDate.ToString("yyyyMMddHHmmss"))}";
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

                                                strVersionExc = objUtility.ExecuteVersion(strDPParamName, strDPParamName, dtRes, versionInfo, VersionRes, sqlcon);
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
                            var ssoResp = _tradeWebRepository.LoginSSO_Req(xmlStr);
                            logRespMessage = ssoResp;
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

        [HttpPost("UpdatePasswordFP", Name = "UpdatePasswordFP")]
        public IActionResult UpdatePasswordFP([FromBody] UpdatePasswordFP request)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (string.IsNullOrEmpty(request.userId))
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", "UserId should not be blank!", ""));
                    }
                    request.userId = objUtility.mfnReplaceForSQLInjection(request.userId.Trim());
                    request.newPassword = objUtility.mfnReplaceForSQLInjection(request.newPassword.Trim());
                    var getData = _tradeWebRepository.UpdatePasswordFP(request.userId, request.otp, request.newPassword);
                    if (getData.status == "success")
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", getData, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        [HttpPost("TestPDF", Name = "TestPDF")]
        public IActionResult TestPDF(string ReportName, string ReportDisplayName, string json, string pdfType)
        {
            var resp = _tradeWebRepository.TestPDFHardcode(ReportName, ReportDisplayName, json, pdfType);
            return Ok(resp);

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
            long logSrNo = 0;
            string logRespMessage = "";
            string logErrorMessage = "";
            var processWatch = System.Diagnostics.Stopwatch.StartNew();
            var processStart = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
            string connectionString = objUtility.GetConnectionStr();
            int intBatchSize = 100;
            string userId = request.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                userId = "API";
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                logSrNo = objUtility.InsertLog(0, "TradeWeb", "", "DigitalSendEmail Process Started. Start Time : " + processStart, "", "", "API", conn);
            }

            try
            {
                var counters = new Counters();
                int sentInThisConnection = 0;

                DataTable dtDocType = objUtility.OpenDataTable($@"SELECT dd_DocumentType, COUNT(0) AS TotalCount, MIN(dd_Priority) AS MinPriority
                                      FROM Digital_Emaildetails
                                      WHERE dd_SendFlag = 'N' GROUP BY dd_DocumentType ORDER BY MinPriority");
                if (dtDocType.Rows.Count < 1)
                {
                    logRespMessage = "No records found";
                    return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = false, message = "No records found" }, Formatting.Indented));
                }

                string isParallel = objUtility.GetWebParameter("ParallelEmail").Trim();

                foreach (DataRow dr in dtDocType.Rows)
                {
                    string strDocType = dr["dd_DocumentType"].ToString().Trim();

                    int maxParallel = Math.Max(2, Environment.ProcessorCount);
                    var emailQueue = new ConcurrentQueue<EmailModel>();
                    var EmailResults = new List<(string Id, string Status, string Error)>(); //int
                    var emailResultsLock = new object();
                    bool producerCompleted = false;
                    int activeWorkers = maxParallel;

                    strsql = "select top 1 dd_EmailParamCode from Digital_Emaildetails where dd_DocumentType='" + strDocType + "'";
                    DataTable dtSMTP = objUtility.OpenDataTable(strsql);

                    if (dtSMTP.Rows.Count == 0)
                    {
                        continue;
                    }

                    strsql = $@"Select SMTPServer [Host], SMTPPort [Port], SMTPMailId Username, SMTPPassword[Password], SMTPSSLFlag Tls, SMTPAccountName SenderName, ParamCode 
                            EmailParamCode FROM tbl_EMailParameters where ParamCode = '{dtSMTP.Rows[0]["dd_EmailParamCode"].ToString().Trim()}'";
                    DataTable dtSMTPSetting = objUtility.OpenDataTable(strsql);
                    if (dtSMTPSetting.Rows.Count == 0)
                    {
                        int failedCount = await MarkFullGroupFailedNew(
                                connectionString,
                                strDocType,
                                "SMTP details not found");

                        Interlocked.Add(ref counters.TotalErrors, failedCount);
                        continue; // skip this document type
                    }

                    int port = (int)dtSMTPSetting.Rows[0]["Port"];
                    string host = dtSMTPSetting.Rows[0]["Host"].ToString().Trim();
                    string username = dtSMTPSetting.Rows[0]["Username"].ToString().Trim();
                    string password = dtSMTPSetting.Rows[0]["Password"].ToString();
                    string senderName = dtSMTPSetting.Rows[0]["SenderName"].ToString().Trim();
                    string eParamCode = dtSMTPSetting.Rows[0]["EmailParamCode"].ToString().Trim();
                    string enableSSL = dtSMTPSetting.Rows[0]["Tls"].ToString().Trim();

                    string strReportingEmail = "";
                    string strReportingSubject = "";
                    string strReportingBody = "";

                    strsql = "select ReportingEmailID, ReportingEmailSubject, ReportingEmailBody from tbl_EmailTemplate where RefName='" + strDocType + "'";
                    DataTable dtReporting = objUtility.OpenDataTable(strsql);
                    if (dtReporting.Rows.Count > 0)
                    {
                        strReportingEmail = dtReporting.Rows[0]["ReportingEmailID"].ToString().Trim();
                        strReportingSubject = dtReporting.Rows[0]["ReportingEmailSubject"].ToString().Trim();
                        strReportingBody = dtReporting.Rows[0]["ReportingEmailBody"].ToString().Trim();
                    }

                    if (isParallel == "Y")
                    {
                        try
                        {
                            using var smtpTest = new MailKit.Net.Smtp.SmtpClient();

                            if (enableSSL == "N")
                            {
                                //bypass certificates validations
                                smtpTest.ServerCertificateValidationCallback = (s, c, h, e) => true;
                            }

                            await smtpTest.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                            await smtpTest.AuthenticateAsync(username, password);

                            if (!string.IsNullOrEmpty(strReportingEmail))
                            {
                                var message = new MimeMessage();
                                message.From.Add(new MailboxAddress(senderName, username));
                                message.To.Add(MailboxAddress.Parse(strReportingEmail));
                                message.Subject = strReportingSubject + " [Started]";

                                var builder = new BodyBuilder { HtmlBody = strReportingBody + " [Started]" };

                                message.Body = builder.ToMessageBody();

                                await smtpTest.SendAsync(message);
                            }

                            await smtpTest.DisconnectAsync(true);
                        }
                        catch (AuthenticationException)
                        {
                            int failedCount = await MarkFullGroupFailedNew(
                                connectionString,
                                strDocType,
                                "Invalid SMTP username or password");

                            Interlocked.Add(ref counters.TotalErrors, failedCount);
                            continue; // skip this SMTP account
                        }
                        catch (Exception ex)
                        {
                            int failedCount = await MarkFullGroupFailedNew(
                                connectionString,
                                strDocType,
                                "SMTP connection failed : " + ex.Message);

                            Interlocked.Add(ref counters.TotalErrors, failedCount);
                            continue;
                        }

                        var producer = Task.Run(async () =>
                        {
                            while (true)
                            {
                                var batch = await GetEmailBatchAsync(connectionString, strDocType, intBatchSize);
                                if (batch.Count == 0)
                                    break;

                                foreach (var mail in batch)
                                    emailQueue.Enqueue(mail);

                                await Task.Delay(3000); // gap between DB reads
                            }

                            producerCompleted = true;
                        });

                        var bulkUpdater = Task.Run(async () =>
                        {
                            while (!producerCompleted || !emailQueue.IsEmpty || activeWorkers > 0)
                            {
                                await Task.Delay(5000); // flush every 5 sec

                                List<(string Id, string Status, string Error)> snapshot; //int

                                lock (emailResultsLock)
                                {
                                    if (EmailResults.Count == 0)
                                        continue;

                                    snapshot = EmailResults.ToList();
                                    EmailResults.Clear();
                                }

                                var converted = snapshot.Select(x =>
                                (
                                    Id: x.Id.ToString(),        // int → string
                                    Flag: x.Status,             // rename
                                    Reason: x.Error             // rename
                                )).ToList();

                                await BulkEmailStatusUpdateAsync(connectionString, userId, converted);
                            }

                            // ⭐ FINAL FLUSH
                            lock (emailResultsLock)
                            {
                                if (EmailResults.Count > 0)
                                {
                                    var snapshot = EmailResults.ToList();
                                    EmailResults.Clear();

                                    var converted = snapshot.Select(x =>
                                    (
                                        Id: x.Id.ToString(),        // int → string
                                        Flag: x.Status,             // rename
                                        Reason: x.Error             // rename
                                    )).ToList();

                                    BulkEmailStatusUpdateAsync(connectionString, userId, converted).Wait();
                                }
                            }
                        });

                        activeWorkers = maxParallel;

                        var workers = Enumerable.Range(0, maxParallel).Select(_ => Task.Run(async () =>
                        {
                            using var smtp = new MailKit.Net.Smtp.SmtpClient();

                            if (enableSSL == "N")
                            {
                                //bypass certificates validations
                                smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;
                            }

                            int sentInThisConnection = 0;

                            try
                            {
                                await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                                await smtp.AuthenticateAsync(username, password);

                                while (!producerCompleted || !emailQueue.IsEmpty)
                                {
                                    if (!emailQueue.TryDequeue(out var email))
                                    {
                                        await Task.Delay(500);
                                        continue;
                                    }

                                    try
                                    {
                                        var message = new MimeMessage();
                                        message.From.Add(new MailboxAddress(senderName, username));
                                        message.To.Add(MailboxAddress.Parse(email.ToEmail));
                                        message.Subject = email.Subject;

                                        var builder = new BodyBuilder { HtmlBody = email.Body };

                                        if (email.Document != null)
                                            builder.Attachments.Add(email.FileName, email.Document);

                                        message.Body = builder.ToMessageBody();

                                        await smtp.SendAsync(message);

                                        lock (emailResultsLock)
                                            EmailResults.Add((email.Id, "S", ""));

                                        Interlocked.Increment(ref counters.TotalProcessed);
                                        sentInThisConnection++;
                                    }
                                    catch (Exception ex)
                                    {
                                        lock (emailResultsLock)
                                            EmailResults.Add((email.Id, "F", ex.Message));

                                        Interlocked.Increment(ref counters.TotalErrors);
                                    }

                                    // throttle per mail
                                    await Task.Delay(500);

                                    // reconnect every 80 mails per worker
                                    if (sentInThisConnection >= 80)
                                    {
                                        if (smtp.IsConnected)
                                            await smtp.DisconnectAsync(true);

                                        await Task.Delay(2000);
                                        await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                                        await smtp.AuthenticateAsync(username, password);

                                        sentInThisConnection = 0;
                                    }
                                }

                                if (smtp.IsConnected)
                                    await smtp.DisconnectAsync(true);
                            }
                            catch (Exception ex)
                            {
                                // If worker dies → mark remaining queue mails failed
                                while (emailQueue.TryDequeue(out var email))
                                {
                                    lock (emailResultsLock)
                                        EmailResults.Add((email.Id, "F", "SMTP worker crashed: " + ex.Message));

                                    Interlocked.Increment(ref counters.TotalErrors);
                                }
                            }
                            finally
                            {
                                Interlocked.Decrement(ref activeWorkers);
                            }
                        }));

                        await producer;                 // wait until DB batches finished loading
                        await Task.WhenAll(workers);    // wait until all SMTP workers finish sending
                        await bulkUpdater;              // wait until final DB flush completes

                        try
                        {
                            using var smtpEnd = new MailKit.Net.Smtp.SmtpClient();

                            if (enableSSL == "N")
                            {
                                //bypass certificates validations
                                smtpEnd.ServerCertificateValidationCallback = (s, c, h, e) => true;
                            }

                            await smtpEnd.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                            await smtpEnd.AuthenticateAsync(username, password);

                            if (!string.IsNullOrEmpty(strReportingEmail))
                            {
                                var endMessage = new MimeMessage();
                                endMessage.From.Add(new MailboxAddress(senderName, username));
                                endMessage.To.Add(MailboxAddress.Parse(strReportingEmail));

                                endMessage.Subject = strReportingSubject + " [Ended]";

                                var builder = new BodyBuilder
                                {
                                    HtmlBody = strReportingBody + " [Ended]"
                                };

                                endMessage.Body = builder.ToMessageBody();

                                await smtpEnd.SendAsync(endMessage);
                            }

                            await smtpEnd.DisconnectAsync(true);
                        }
                        catch
                        {
                            // Never crash main flow because END mail failed
                        }
                    }
                    else
                    {
                        using (var smtp = new MailKit.Net.Smtp.SmtpClient())
                        {
                            sentInThisConnection = 0;

                            if (enableSSL == "N")
                            {
                                //bypass certificates validations
                                smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;
                            }

                            try
                            {
                                try
                                {
                                    await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                                    await smtp.AuthenticateAsync(username, password);

                                    if (!string.IsNullOrEmpty(strReportingEmail))
                                    {
                                        var message = new MimeMessage();
                                        message.From.Add(new MailboxAddress(senderName, username));
                                        message.To.Add(MailboxAddress.Parse(strReportingEmail));
                                        message.Subject = strReportingSubject + " [Started]";

                                        var builder = new BodyBuilder { HtmlBody = strReportingBody + " [Started]" };

                                        message.Body = builder.ToMessageBody();

                                        await smtp.SendAsync(message);
                                    }
                                }
                                catch (AuthenticationException)
                                {
                                    // ⭐ AUTH FAILED → mark ALL emails of this document type failed
                                    int failedCount = await MarkFullGroupFailedNew(connectionString, strDocType,
                                        "Invalid SMTP username or password");
                                    Interlocked.Add(ref counters.TotalErrors, failedCount);
                                    continue;
                                }
                                catch (Exception ex)
                                {
                                    int failedCount = await MarkFullGroupFailedNew(connectionString, strDocType,
                                        "SMTP connection failed : " + ex.Message);
                                    Interlocked.Add(ref counters.TotalErrors, failedCount);
                                    continue;
                                }

                                while (true)
                                {
                                    var batch = await GetEmailBatchAsync(connectionString, strDocType, intBatchSize);

                                    if (batch.Count == 0)
                                        break; // no more emails left

                                    foreach (var email in batch)
                                    {
                                        if (!smtp.IsConnected)
                                        {
                                            await Task.Delay(2000);
                                            await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                                            await smtp.AuthenticateAsync(username, password);
                                        }

                                        var message = new MimeMessage();
                                        message.From.Add(new MailboxAddress(senderName, username));
                                        message.To.Add(MailboxAddress.Parse(email.ToEmail));
                                        message.Subject = email.Subject;

                                        try
                                        {
                                            var builder = new BodyBuilder { HtmlBody = email.Body };

                                            if (email.Document != null)
                                                builder.Attachments.Add(email.FileName, email.Document);

                                            message.Body = builder.ToMessageBody();

                                            await smtp.SendAsync(message);

                                            EmailResults.Add((email.Id, "S", ""));
                                            Interlocked.Increment(ref counters.TotalProcessed);
                                        }
                                        catch (Exception ex)
                                        {
                                            EmailResults.Add((email.Id, "F", ex.Message));
                                            Interlocked.Increment(ref counters.TotalErrors);
                                        }

                                        await Task.Delay(500); // throttle per mail

                                        sentInThisConnection++;

                                        //reconnect every 80 mails
                                        if (sentInThisConnection >= 80)
                                        {
                                            if (smtp.IsConnected)
                                                await smtp.DisconnectAsync(true);
                                            //await smtp.DisconnectAsync(true);
                                            await Task.Delay(2000);
                                            await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                                            await smtp.AuthenticateAsync(username, password);
                                            sentInThisConnection = 0;
                                        }
                                    }

                                    if (EmailResults.Count > 0)
                                    {
                                        await BulkEmailStatusUpdateAsync(connectionString, userId, EmailResults);
                                        EmailResults.Clear();
                                    }

                                    // cooldown between batches
                                    await Task.Delay(5000);
                                }

                                if (smtp.IsConnected)
                                    await smtp.DisconnectAsync(true);
                            }
                            catch (Exception)
                            {
                                if (smtp.IsConnected)
                                    await smtp.DisconnectAsync(true);
                            }
                            finally
                            {
                                try
                                {
                                    // Reconnect if needed
                                    if (!smtp.IsConnected)
                                    {
                                        await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                                        await smtp.AuthenticateAsync(username, password);
                                    }

                                    if (!string.IsNullOrEmpty(strReportingEmail))
                                    {
                                        var message = new MimeMessage();
                                        message.From.Add(new MailboxAddress(senderName, username));
                                        message.To.Add(MailboxAddress.Parse(strReportingEmail));
                                        message.Subject = strReportingSubject + " [Ended]";

                                        var builder = new BodyBuilder { HtmlBody = strReportingBody + " [Ended]" };

                                        message.Body = builder.ToMessageBody();

                                        await smtp.SendAsync(message);
                                    }
                                }
                                catch
                                {

                                }

                                if (smtp.IsConnected)
                                    await smtp.DisconnectAsync(true);
                            }
                        }
                    }
                }

                DataSet dsRes = new DataSet();
                dsRes.Tables.Add("rs0");
                dsRes.Tables[0].Columns.Add("SuccessCount", typeof(string));
                dsRes.Tables[0].Columns.Add("FailedCount", typeof(string));
                dsRes.Tables[0].Rows.Add(counters.TotalProcessed, counters.TotalErrors);

                var processEnd = DateTime.Now;

                processWatch.Stop();
                logRespMessage = "DigitalSendEmail Process completed. End Time : " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + " Time taken : " + processWatch.Elapsed;
                return Ok(JsonConvert.SerializeObject(new CrossNetCommonModel { success = true, message = "Email has been sent successfully", data = dsRes, datarows = new List<string>() { "1" } }, Formatting.Indented));
            }
            catch (Exception ex)
            {
                processWatch.Stop();
                logErrorMessage = "Error in DigitalSendEmail process. Error : " + ex.Message + ". End Time : " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + " Time taken" + processWatch.Elapsed;
            }
            finally
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    objUtility.UpdateLog(logSrNo, "", logRespMessage, logErrorMessage, conn);
                }
            }

            return BadRequest();
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        private async Task<List<EmailModel>> GetEmailBatchAsync(string conStr, string docType, int batchSize)
        {
            var list = new List<EmailModel>();

            using var con = new SqlConnection(conStr);
            await con.OpenAsync();

            string sql = @"
    ;WITH cte AS
    (
        SELECT TOP (@BatchSize) *
        FROM Digital_Emaildetails WITH (ROWLOCK, READPAST)
        WHERE dd_SendFlag = 'N'
        AND dd_DocumentType = @ParamCode
        ORDER BY dd_Priority ASC, dd_srno ASC
    )
    UPDATE cte
       SET dd_SendFlag = 'L',      -- lock rows
           dd_RetryCount = ISNULL(dd_RetryCount,0)+1, 
		   dd_LastRetryDate = GETDATE()
    OUTPUT inserted.dd_srno,
           inserted.dd_toEmailid,
           inserted.dd_Subject,
           inserted.dd_bodyText,
           inserted.dd_Document,
           inserted.dd_FileName";

            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@BatchSize", batchSize);
            cmd.Parameters.AddWithValue("@ParamCode", docType);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new EmailModel
                {
                    Id = reader["dd_srno"].ToString(),
                    ToEmail = reader["dd_toEmailid"].ToString(),
                    Subject = reader["dd_Subject"].ToString(),
                    Body = reader["dd_bodyText"].ToString(),
                    Document = reader["dd_Document"] as byte[],
                    FileName = reader["dd_FileName"].ToString()
                });
            }

            return list;
        }


        [ApiExplorerSettings(IgnoreApi = true)]
        public async IAsyncEnumerable<EmailModel> GetEmailsAsync(string connectionString, string eParmCode)
        {
            using var con = new SqlConnection(connectionString);
            await con.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT
                    Cast(dd_srno as varchar),
                    dd_toEmailid,
                    dd_subject,
                    dd_bodyText,
                    dd_document,
                    dd_filename 
                FROM Digital_Emaildetails WHERE dd_EmailParamCode = '" + eParmCode + "' AND dd_SendFlag = 'N' ", con);

            using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

            while (await reader.ReadAsync())
            {
                yield return new EmailModel
                {
                    Id = reader.GetString(0),
                    ToEmail = reader.GetString(1),
                    Subject = reader.GetString(2),
                    Body = reader.GetString(3),
                    Document = reader.IsDBNull(4) ? null : (byte[])reader[4],
                    FileName = reader.IsDBNull(5) ? null : reader.GetString(5)
                };
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<int> MarkFullGroupFailed(string connectionString, string paramCode, string reason)
        {
            using var con = new SqlConnection(connectionString);
            using var cmd = new SqlCommand(@"
        UPDATE Digital_Emaildetails
        SET dd_SendFlag = 'F',
            dd_RetryCount = ISNULL(dd_RetryCount,0) + 1,
            dd_LastRetryDate = GETDATE(),
            dd_BounceReason = @reason
        WHERE dd_EmailParamCode = @paramCode
        AND dd_SendFlag = 'N'", con); //dd_SendFlag IN ('P','F')

            cmd.Parameters.AddWithValue("@reason", reason);
            cmd.Parameters.AddWithValue("@paramCode", paramCode);

            await con.OpenAsync();
            int rows = await cmd.ExecuteNonQueryAsync();
            return rows;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<int> MarkFullGroupFailedNew(string connectionString, string documentType, string reason)
        {
            using var con = new SqlConnection(connectionString);
            using var cmd = new SqlCommand(@"
                UPDATE Digital_Emaildetails
                SET dd_SendFlag = 'F',
                    dd_RetryCount = ISNULL(dd_RetryCount,0) + 1,
                    dd_LastRetryDate = GETDATE(),
                    dd_BounceReason = @reason
                WHERE dd_DocumentType = @documentType
                AND dd_SendFlag = 'N'", con); //dd_SendFlag IN ('P','F')

            cmd.Parameters.AddWithValue("@reason", reason);
            cmd.Parameters.AddWithValue("@documentType", documentType);

            await con.OpenAsync();
            int rows = await cmd.ExecuteNonQueryAsync();
            return rows;
        }


        [HttpPost("QuestPDF", Name = "QuestPDF")]
        public IActionResult QuestPDF()
        {
            try
            {
                string time = DateTime.Now.ToString("HHmmss");
                string jsonFilePath = "E:/My Work/16022026/TradeWebAPI/QuestPdfServicesClass/JsonFiles/ClientFundLedger.json";
                string outputFilePath = "E:/My Work/16022026/TradeWebAPI/QuestPdfServicesClass/JsonFiles/output_" + time + ".pdf";
                string logoPath = "E:/My Work/16022026/TradeWebAPI/QuestPdfServicesClass/JsonFiles/CompanyLogo.png";
                DataSet dsCFL = objUtility.OpenDataSet("exec SP_CFL");
                TestQuestPdf.Generate(jsonFilePath, outputFilePath, logoPath);
                //QuestPdfGeneratorByJson.GenerateBytes(jsonFilePath);

                return Ok();
            }

            catch (Exception ex)
            {
                return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", "An error occurred during file processing. " + ex.Message.ToString(), ""));
            }
        }

        [HttpPost("QuestPDFNew", Name = "QuestPDFNew")]
        public IActionResult QuestPDFNew()
        {
            try
            {
                string time = DateTime.Now.ToString("HHmmss");
                string jsonFilePath = "E:/My Work/16022026/TradeWebAPI/QuestPdfServicesClass/JsonFiles/ClientFundLedgerNew.json";
                string outputFilePath = "E:/My Work/16022026/TradeWebAPI/QuestPdfServicesClass/JsonFiles/output_" + time + ".pdf";
                string logoPath = "E:/My Work/16022026/TradeWebAPI/QuestPdfServicesClass/JsonFiles/CompanyLogo.png";
                DataSet dsCFL = objUtility.OpenDataSet("exec SP_CFL");
                TestQuestPdf_New.Generate(jsonFilePath, outputFilePath, logoPath, dsCFL);
                return Ok();
            }
            catch (Exception ex)
            {
                return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", "An error occurred during file processing. " + ex.Message.ToString(), ""));
            }
        }

        [HttpPost("QuestPDFCFL", Name = "QuestPDFCFL")]
        public IActionResult QuestPDFCFL()
        {
            try
            {
                string time = DateTime.Now.ToString("HHmmss");
                //string jsonFilePath = "E:/My Work/16022026/TradeWebAPI/QuestPdfServicesClass/JsonFiles/ClientFundLedgerNew.json";
                string jsonFilePath = "E:/My Work/16022026/TradeWebAPI/QuestPdfServicesClass/JsonFiles/CombineContractNote.json";
                string outputFilePath = "E:/My Work/16022026/TradeWebAPI/QuestPdfServicesClass/JsonFiles/output_" + time + ".pdf";
                string logoPath = "E:/My Work/16022026/TradeWebAPI/QuestPdfServicesClass/JsonFiles/CompanyLogo.png";

                DataSet ds = new DataSet();

                var typstResponse = _tradeWebRepository.GenerateQuestPDF("", "", ds, "", "");


                //TestQuestPdf.Generate(jsonFilePath, outputFilePath, logoPath);
                //QuestPdfGeneratorByJson.GenerateBytes(jsonFilePath);
                return Ok();
            }

            catch (Exception ex)
            {
                return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", "An error occurred during file processing. " + ex.Message.ToString(), ""));
            }
        }


        /*[ApiExplorerSettings(IgnoreApi = true)]
        public async Task BulkUpdateAsync(string connectionString, List<string> ids)
        {
            using var con = new SqlConnection(connectionString);
            await con.OpenAsync();
            string idList = string.Join(",", ids.Select(x => $"'{x}'"));
            string sql = $@"
                    UPDATE dbo.Digital_Emaildetails
                    SET dd_SentDate = GETDATE(),
                        dd_SendFlag = 'Y'
                    WHERE dd_srno IN ({idList})";

            using var cmd = new SqlCommand(sql, con);
            await cmd.ExecuteNonQueryAsync();
        }*/

        #region old code
        /*[ApiExplorerSettings(IgnoreApi = true)]
        public async Task BulkFailedEmailUpdateAsync(string connectionString, List<string> ids)
        {
            using var con = new SqlConnection(connectionString);
            await con.OpenAsync();
            string idList = string.Join(",", ids.Select(x => $"'{x}'"));
            string sql = $@"
                    UPDATE dbo.Digital_Emaildetails
                    SET dd_SentDate = GETDATE(),
                        dd_SendFlag = 'F'
                    WHERE dd_srno IN ({idList})";

            using var cmd = new SqlCommand(sql, con);
            await cmd.ExecuteNonQueryAsync();
        }*/
        #endregion


        private string GenerateJwtTokenOnly(string username, string compCode, string guid, string role, int tokenExpTime)
        {
            string decryptJwtKey = objUtility.Decrypt(_configuration["Jwt:Key"].ToString());
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(decryptJwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(type: "companyCode", value: compCode),
                new Claim(JwtRegisteredClaimNames.Jti, guid),
                new Claim(type: "username", value: username),
                new Claim(ClaimTypes.Role, role),
            };
            var token = new JwtSecurityToken(_configuration["Jwt:Issuer"],
                _configuration["Jwt:Issuer"],
                claims,
                expires: DateTime.UtcNow.AddMinutes(tokenExpTime),
                signingCredentials: credentials);

            //if (role == "Branch")
            //{
            //    _tokenStore.StoreToken(username, new JwtSecurityTokenHandler().WriteToken(token));
            //}

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task BulkEmailStatusUpdateAsync(string connectionString, string userId, IEnumerable<(string Id, string Flag, string Reason)> results)
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(string));
            table.Columns.Add("SendFlag", typeof(string));
            table.Columns.Add("BounceReason", typeof(string));

            foreach (var r in results)
                table.Rows.Add(r.Id, r.Flag, r.Reason);

            using var con = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.Stpr_BulkEmailStatusUpdate", con);
            cmd.CommandType = CommandType.StoredProcedure;

            var tvp = cmd.Parameters.AddWithValue("@EmailStatus", table);
            tvp.SqlDbType = SqlDbType.Structured;
            tvp.TypeName = "dbo.EmailStatusType";
            cmd.Parameters.AddWithValue("@UserId", userId);

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        private JwtSecurityToken GetToken()
        {
            var handler = new JwtSecurityTokenHandler();
            string authHeader = Request.Headers["Authorization"];
            authHeader = authHeader.Replace("Bearer ", "");
            var token = handler.ReadToken(authHeader) as JwtSecurityToken;
            return token;
        }
        #endregion


        #region Other methods for JWT Token
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
            using (var aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.KeySize = 128;           // key is 16 bytes
                aesAlg.BlockSize = 128;
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;

                aesAlg.Key = key;
                aesAlg.IV = iv;

                using (var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV))
                using (var msEncrypt = new MemoryStream())
                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                using (var swEncrypt = new StreamWriter(csEncrypt))
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
            {
                throw new ArgumentNullException("cipherText");
            }
            if (key == null || key.Length <= 0)
            {
                throw new ArgumentNullException("key");
            }
            if (iv == null || iv.Length <= 0)
            {
                throw new ArgumentNullException("key");
            }
            // Declare the string used to hold  
            // the decrypted text.  
            string plaintext = null;

            #region old encryption logic
            // Create an RijndaelManaged object  
            // with the specified key and IV.  
            /*using (var rijAlg = new RijndaelManaged())
            {
                //Settings  
                rijAlg.Mode = CipherMode.CBC;
                rijAlg.Padding = PaddingMode.PKCS7;
                rijAlg.FeedbackSize = 128;

                rijAlg.Key = key;
                rijAlg.IV = iv;
                // Create a decrytor to perform the stream transform.  
                var decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);
                try
                {
                    // Create the streams used for decryption.  
                    using (var msDecrypt = new MemoryStream(cipherText))
                    {
                        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (var srDecrypt = new StreamReader(csDecrypt))
                            {
                                // Read the decrypted bytes from the decrypting stream  
                                // and place them in a string.  
                                plaintext = srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
                catch
                {
                    plaintext = "keyError";
                }
            }*/
            #endregion

            using (var aesAlg = new AesCryptoServiceProvider())
            {
                try
                {
                    aesAlg.Mode = CipherMode.CBC;
                    aesAlg.Padding = PaddingMode.PKCS7;

                    aesAlg.KeySize = 256;
                    aesAlg.BlockSize = 128;

                    aesAlg.Key = key;
                    aesAlg.IV = iv;

                    using (var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV))
                    {
                        using (var msDecrypt = new MemoryStream(cipherText))
                        {
                            using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                            {
                                using (var srDecrypt = new StreamReader(csDecrypt))
                                {
                                    plaintext = srDecrypt.ReadToEnd();
                                }
                            }
                        }
                    }
                }
                finally
                {
                    aesAlg.Clear();
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
            catch (Exception ex)
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
            var datetimeExp = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, UtilityCommon.GetIndianTimeZone());
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
            result.tokenExpireTime = datetimeExp.AddMinutes(expTime).ToString("yyyy/MM/dd HH:mm:ss");
            result.refreshToken = refreshToken;
            result.data = dt;
            var jsonData = JsonConvert.SerializeObject(result, Formatting.Indented);
            return jsonData;
        }

        #endregion

        #region Fill Configuration
        private WebParamterLoginEntity FillConfigParameters()
        {
            try
            {
                // TODO : Added code of web from Login.aspx(125 -175)

                WebParamterLoginEntity wp = new WebParamterLoginEntity();
                wp.Segments = objUtility.GetWebParameter("Segments");
                wp.CLR_TITLE = objUtility.GetWebParameter("CLR_TITLE");
                wp.CLR_MENU = objUtility.GetWebParameter("CLR_MENU");
                wp.CLR_HEADER = objUtility.GetWebParameter("CLR_HEADER");
                wp.CLR_TOTALS = objUtility.GetWebParameter("CLR_TOTALS");
                wp.IsTradeWeb = objUtility.GetWebParameter("IsTradeWeb");
                wp.TPlus = objUtility.GetWebParameter("TPlus");
                wp.TPlusES = objUtility.GetWebParameter("TPlusES");
                wp.Cross = objUtility.GetWebParameter("Cross");
                wp.CrossEs = objUtility.GetWebParameter("CrossEs");
                wp.Estro = objUtility.GetWebParameter("Estro");
                wp.EstroEs = objUtility.GetWebParameter("EstroEs");
                wp.Commex = objUtility.GetWebParameter("Commex");
                wp.CommexEs = objUtility.GetWebParameter("CommexEs");

                wp.CAPTCHA = objUtility.GetWebParameter("CAPTCHA");
                wp.SMTPHOST = objUtility.GetWebParameter("SMTPHOST");
                wp.PANASPASSWORD = objUtility.GetWebParameter("PANASPASSWORD");
                wp.GroupLogin = objUtility.GetWebParameter("GroupLogin");
                wp.ShowRupeeSymbol = objUtility.GetWebParameter("ShowRupeeSymbol");
                wp.CLR_WEBSKIN = objUtility.GetWebParameter("CLR_WEBSKIN");
                wp.FTSOSURL = objUtility.GetWebParameter("FTSOSURL");
                wp.PMSUrl = objUtility.GetWebParameter("PMSUrl");
                wp.KYCPDF = objUtility.GetWebParameter("KYCPDF");
                wp.OrganizationName = objUtility.GetWebParameter("OrganizationName");
                wp.TNetInvplUrl = objUtility.GetWebParameter("TNetInvplUrl");
                wp.OUTPOSURL = objUtility.GetWebParameter("OUTPOSURL");
                wp.SMTPSERVER = objUtility.GetWebParameter("SMTPSERVER");
                wp.SECURITYPROT = objUtility.GetWebParameter("SECURITYPROT");

                return wp;
            }
            catch (Exception ex)
            {
                throw ex;
            }
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

        #endregion


        private dynamic Generate_AccountClosurePDF(string spXML, string esignReturnUrl, string newBOID, string userId, string accountType, string cmrBase64)
        {
            return new { Status = "N", UnsignedPdf = "", EsignUrl = "", EmailId = "", Remark = "Account closure PDF generation has been disabled." };
        }


    }
}
