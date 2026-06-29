using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using TradeWeb.API.Models;
using TradeWeb.API.Repository;
using TradeWeb.Service;

namespace TradeWeb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RequestController : BaseController
    {
        private readonly ITradeWebRepository _tradeWebRepository;
        ConvertData returnJson = new ConvertData();
        DataTable retrunDt = new DataTable();
        private readonly UtilityCommon objUtility;
        public RequestController(ITradeWebRepository tradeWebRepository, UtilityCommon _objUtility)
        {
            _tradeWebRepository = tradeWebRepository;
            objUtility = _objUtility;
        }

        #region Request Api

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("Request_Get_ShareRequest", Name = "Request_Get_ShareRequest")]
        public IActionResult Request_Get_ShareRequest()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

                    var getData = _tradeWebRepository.Request_Get_ShareRequest(userId);
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

        // Radio button shares checked
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpPost("Request_Post_ShareRequest", Name = "Request_Post_ShareRequest")]
        public IActionResult Request_Post_ShareRequest(Request_Post_ShareRequestModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    string getData = null;
                    foreach (var i in model.data)
                    {
                        getData = _tradeWebRepository.Request_Post_ShareRequest(userId, objUtility.mfnReplaceForSQLInjection(i.scrip_Code), objUtility.mfnReplaceForSQLInjection(i.request_Quantity));
                    }

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

        // Get Rms Request
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin,FundPayout")]
        [HttpGet("Request_Get_FundRequest", Name = "Request_Get_FundRequest")]
        public IActionResult Request_Get_FundRequest()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

                    var getData = _tradeWebRepository.Request_Get_FundRequest(userId);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin,FundPayout")]
        [HttpPost("Request_Post_FundRequest", Name = "Request_Post_FundRequest")]
        public IActionResult Request_Post_FundRequest(FundRequest_Model model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

                    string getData = _tradeWebRepository.Request_Post_FundRequest(model, userId);

                    if (getData.Contains("registered"))
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", getData, ""));
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        // TODO : Get margin pledge data
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("Request_Get_PledgeForMargin", Name = "Request_Get_PledgeForMargin")]
        public IActionResult Request_Get_PledgeForMargin([FromQuery] string dematActNo)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var compCode = tokenS.Claims.First(claim => claim.Type == "companyCode").Value;
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    dematActNo = objUtility.mfnReplaceForSQLInjection(dematActNo);
                    var getData = _tradeWebRepository.Request_Get_PledgeForMargin(userId, dematActNo);
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
        [HttpPost("Request_Post_PledgeForMargin", Name = "Request_Post_PledgeForMargin")]
        public IActionResult Request_Post_PledgeForMargin(PledgeForMarginModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    string getData = null;
                    foreach (var i in model.data)
                    {
                        getData = _tradeWebRepository.Request_Post_PledgeForMargin(userId.ToUpper(), objUtility.mfnReplaceForSQLInjection(i.DematActNo), objUtility.mfnReplaceForSQLInjection(i.Securities_Code), objUtility.mfnReplaceForSQLInjection(i.Request_Qty));
                    }
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


        // insert unpledge request
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpPost("Request_Post_UnPledge", Name = "Request_Post_UnPledge")]
        public IActionResult Request_Post_UnPledge(UnPledgeRequestModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

                    string getData = null;
                    foreach (var i in model.data)
                    {
                        getData = _tradeWebRepository.Request_Post_UnPledge_UnRepledge(userId, "Pledge", objUtility.mfnReplaceForSQLInjection(i.ScripCode), objUtility.mfnReplaceForSQLInjection(i.Request_Qty));
                    }
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

        // insert unpledge request
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpPost("Request_Post_UnRePledge", Name = "Request_Post_UnRePledge")]
        public IActionResult Request_Post_UnRePledge(UnPledgeRequestModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

                    string getData = null;
                    foreach (var i in model.data)
                    {
                        getData = _tradeWebRepository.Request_Post_UnPledge_UnRepledge(userId, "Un-Re-Pledge", objUtility.mfnReplaceForSQLInjection(i.ScripCode), objUtility.mfnReplaceForSQLInjection(i.Request_Qty));
                    }
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
        [HttpPost("Request_Post_Report", Name = "Request_Post_Report")]
        public IActionResult Request_Post_Report(string ExchSeg, string Report, string strFromDt, string strToDt)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    ExchSeg = objUtility.mfnReplaceForSQLInjection(ExchSeg);
                    Report = objUtility.mfnReplaceForSQLInjection(Report);
                    strFromDt = objUtility.mfnReplaceForSQLInjection(strFromDt);
                    strToDt = objUtility.mfnReplaceForSQLInjection(strToDt);
                    var getData = _tradeWebRepository.Request_Post_Report(userId, ExchSeg, Report, strFromDt, strToDt);
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
        [HttpGet("Request_Report_Setting", Name = "Request_Report_Setting")]
        public IActionResult Request_ReportSetting_Get()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var compCode = tokenS.Claims.First(claim => claim.Type == "companyCode").Value;
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

                    var getData = _tradeWebRepository.Request_Report_Setting();
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
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, ex.Message.ToString(), retrunDt, ""));
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
