using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using TradeWeb.API.Repository;
using TradeWeb.Service;

namespace TradeWeb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MiscellaniousController : BaseController
    {
        private readonly ITradeWebRepository _tradeWebRepository;
        private readonly UtilityCommon objUtility;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        ConvertData returnJson = new ConvertData();
        DataTable retrunDt = new DataTable();

        public MiscellaniousController(ITradeWebRepository tradeWebRepository, UtilityCommon objUtility, IConfiguration configuration, IWebHostEnvironment environment)
        {
            _tradeWebRepository = tradeWebRepository;
            this.objUtility = objUtility;
            _configuration = configuration;
            _environment = environment;
        }

        #region Agreement Api
        // TODO : Get Family Page_Load Data
        //[Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("GetAgreementPdf", Name = "GetAgreementPdf")]
        public IActionResult GetAgreementPdf()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    string aggPath = string.IsNullOrWhiteSpace(_configuration["AgreementPATH"]) ? "" : _configuration["AgreementPATH"];

                    string path = Path.Combine(this._environment.WebRootPath, aggPath, userId) + ".pdf";
                    path = path.Replace("wwwroot", "");
                    var docBytes = System.IO.File.ReadAllBytes(path.Replace("wwwroot", ""));
                    string docBase64 = "data:application/pdf;base64," + Convert.ToBase64String(docBytes);

                    if (docBase64 != null)
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", docBase64, ""));
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
        #endregion

        #region DigitalDocument Api


        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("DigitalDocument_Type", Name = "DigitalDocument_Type")]
        public IActionResult DigitalDocument_Type()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var getData = _tradeWebRepository.DigitalDocument_Type(userId);
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
        [HttpGet("DigitalDocument_List", Name = "DigitalDocument_List")]
        public IActionResult DigitalDocument_List([FromQuery] int product, string fromDate, string toDate, string docType)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    fromDate = objUtility.mfnReplaceForSQLInjection(fromDate);
                    toDate = objUtility.mfnReplaceForSQLInjection(toDate);
                    docType = objUtility.mfnReplaceForSQLInjection(docType);
                    var getData = _tradeWebRepository.DigitalDocument_List(userId, product, fromDate, toDate, docType);
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
        [HttpGet("DigitalDocument_File", Name = "DigitalDocument_File")]
        public IActionResult DigitalDocument_File([FromQuery] int Product, string date, string srNo)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    date = objUtility.mfnReplaceForSQLInjection(date);
                    srNo = objUtility.mfnReplaceForSQLInjection(srNo);
                    var getData = _tradeWebRepository.DigitalDocument_File(Product, date, srNo);
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


        #endregion


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
