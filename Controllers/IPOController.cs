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
    public class IPOController : BaseController
    {
        private readonly ITradeWebRepository _tradeWebRepository;
        ConvertData returnJson = new ConvertData();
        DataTable retrunDt = new DataTable();
        private readonly UtilityCommon objUtility;
        public IPOController(ITradeWebRepository tradeWebRepository, UtilityCommon _objUtility)
        {
            _tradeWebRepository = tradeWebRepository;
            objUtility = _objUtility;
        }

        #region IPO Api

        //Get IPO Main Data
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("List", Name = "List")]
        public IActionResult GetIPOMainData()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

                    var getData = _tradeWebRepository.GetIPOMainData(userId);
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
        [HttpGet("GetRemarks", Name = "GetRemarks")]
        public IActionResult GetIPORemark(string IPOName, string InvestorType)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    IPOName = objUtility.mfnReplaceForSQLInjection(IPOName);
                    InvestorType = objUtility.mfnReplaceForSQLInjection(InvestorType);
                    var getData = _tradeWebRepository.GetIPORemark(userId, IPOName, InvestorType);
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

        //Get IPO Category
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("IPO_Category", Name = "IPO_Category")]
        public IActionResult GetIPO_Category()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

                    var getData = _tradeWebRepository.GetIPO_Category();
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
        [HttpPost("IPOSubmit", Name = "IPOSubmit")]
        public IActionResult IPOSubmit(IPOSubmitModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    model.IPOName = objUtility.mfnReplaceForSQLInjection(model.IPOName);
                    model.MinimumOrder = objUtility.mfnReplaceForSQLInjection(model.MinimumOrder);
                    model.Discount = objUtility.mfnReplaceForSQLInjection(model.Discount);
                    model.CutoffPrice = objUtility.mfnReplaceForSQLInjection(model.CutoffPrice);
                    model.UPIid = objUtility.mfnReplaceForSQLInjection(model.UPIid);
                    model.InvestorType = objUtility.mfnReplaceForSQLInjection(model.InvestorType);
                    model.CutoffFlag1 = objUtility.mfnReplaceForSQLInjection(model.CutoffFlag1);
                    model.CutoffFlag2 = objUtility.mfnReplaceForSQLInjection(model.CutoffFlag2);
                    model.CutoffFlag3 = objUtility.mfnReplaceForSQLInjection(model.CutoffFlag3);
                    model.Qty1 = objUtility.mfnReplaceForSQLInjection(model.Qty1);
                    model.Qty2 = objUtility.mfnReplaceForSQLInjection(model.Qty2);
                    model.Qty3 = objUtility.mfnReplaceForSQLInjection(model.Qty3);
                    model.Price1 = objUtility.mfnReplaceForSQLInjection(model.Price1);
                    model.Price2 = objUtility.mfnReplaceForSQLInjection(model.Price2);
                    model.Price3 = objUtility.mfnReplaceForSQLInjection(model.Price3);
                    var getData = _tradeWebRepository.GetIPOSubmit(userId, model);
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
        [HttpPost("IPOCheckStatus", Name = "IPOCheckStatus")]
        public IActionResult IPOStatus(string IPOName, string InvestorType)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    IPOName = objUtility.mfnReplaceForSQLInjection(IPOName);
                    InvestorType = objUtility.mfnReplaceForSQLInjection(InvestorType);
                    var getData = _tradeWebRepository.GetIPOStatus(userId, IPOName, InvestorType);
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
        [HttpPost("IPODelete", Name = "IPODelete")]
        public IActionResult IPODelete(string IPOName, string InvestorType)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    IPOName = objUtility.mfnReplaceForSQLInjection(IPOName);
                    InvestorType = objUtility.mfnReplaceForSQLInjection(InvestorType);
                    var getData = _tradeWebRepository.GetIPODelete(userId, IPOName, InvestorType);
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

        private JwtSecurityToken GetToken()
        {
            var handler = new JwtSecurityTokenHandler();
            string authHeader = Request.Headers["Authorization"];
            authHeader = authHeader.Replace("Bearer ", "");
            //var jsonToken = handler.ReadToken(authHeader);
            var token = handler.ReadToken(authHeader) as JwtSecurityToken;
            return token;
        }

        #endregion
    }
}

