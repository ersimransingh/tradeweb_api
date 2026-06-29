using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using TradeWeb.API.Repository;
using TradeWeb.Service;

namespace TradeWeb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MarginController : BaseController
    {
        private readonly ITradeWebRepository _tradeWebRepository;
        ConvertData returnJson = new ConvertData();
        DataTable retrunDt = new DataTable();
        private readonly UtilityCommon objUtility;
        public MarginController(ITradeWebRepository tradeWebRepository, UtilityCommon _objUtility)
        {
            _tradeWebRepository = tradeWebRepository;
            objUtility = _objUtility;
        }

        #region margin api

        // TODO : Get margin grid main data
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("Margin", Name = "Margin")]
        public IActionResult Margin(string date)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    date = objUtility.mfnReplaceForSQLInjection(date);
                    var getData = _tradeWebRepository.Margin(userId, date);
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

        // TODO : Get Last margin date
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("LastMarginDt", Name = "LastMarginDt")]
        public IActionResult LastMarginDt()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var getData = _tradeWebRepository.Margin_LastMarginDate();
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


        // TODO : Get margin grid main data
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("CollateralAllocation", Name = "CollateralAllocation")]
        public IActionResult CollateralAllocation()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

                    var getData = _tradeWebRepository.CollateralAllocation(userId);
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
