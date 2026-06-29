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
    public class HoldingController : BaseController
    {
        private readonly UtilityCommon objUtility;
        private readonly ITradeWebRepository _tradeWebRepository;
        ConvertData returnJson = new ConvertData();
        DataTable retrunDt = new DataTable();
        public HoldingController(UtilityCommon objUtility, ITradeWebRepository tradeWebRepository)
        {
            this.objUtility = objUtility;
            _tradeWebRepository = tradeWebRepository;
        }

        #region Holding Api

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("Holding_Broker_Current", Name = "Holding_Broker_Current")]
        public IActionResult Holding_Broker_Current()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

                    userId = " and cm_cd ='" + userId + "' ";
                    var getData = _tradeWebRepository.Holding_Broker_Current(userId);
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
        [HttpGet("Holding_Broker_Ason", Name = "Holding_Broker_Ason")]
        public IActionResult Holding_Broker_Ason(string AsOnDt)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    AsOnDt = objUtility.mfnReplaceForSQLInjection(AsOnDt);
                    var getData = _tradeWebRepository.Holding_Broker_Ason(userId, AsOnDt);
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
        [HttpGet("Holding_MyDematAct_List", Name = "Holding_MyDematAct_List")]
        public IActionResult Holding_MyDematAct_List()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

                    var getData = _tradeWebRepository.Holding_MyDematAct_List(userId);
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

        //[Authorize(AuthenticationSchemes = "Bearer")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("Holding_MyDematAct_Current", Name = "Holding_MyDematAct_Current")]
        public IActionResult Holding_MyDematAct_Current(string dematActNo, int graphDays)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    dematActNo = objUtility.mfnReplaceForSQLInjection(dematActNo);
                    var getData = _tradeWebRepository.Holding_MyDemat_Current_Graph(userId, dematActNo, "Holding", graphDays);
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

        ////[Authorize(AuthenticationSchemes = "Bearer")]
        //[HttpGet("Holding_MyDematAct_Current_Graph", Name = "Holding_MyDematAct_Current_Graph")]
        //public IActionResult Holding_MyDematAct_Current_Graph(string dematActNo, bool isGraph, string fromDate, string toDate)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            var tokenS = GetToken();
        //            var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

        //            var getData = _tradeWebRepository.Holding_MyDemat_Current_Graph(userId, dematActNo, "Holding", isGraph, fromDate, toDate);
        //            if (getData != null)
        //            {
        //                return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
        //            }
        //            else
        //            {
        //                return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "records not found", retrunDt, ""));
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
        //        }
        //    }
        //    return BadRequest();
        //}

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("Holding_MyDematAct_Ason", Name = "Holding_MyDematAct_Ason")]
        public IActionResult Holding_MyDematAct_Ason(string dematActNo, string AsOnDt)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    dematActNo = objUtility.mfnReplaceForSQLInjection(dematActNo);
                    AsOnDt = objUtility.mfnReplaceForSQLInjection(AsOnDt);
                    var getData = _tradeWebRepository.Holding_MyDemat_Current(userId, dematActNo, "Holding_" + AsOnDt);
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

        // Get data for bind dropdownlist combo as on
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("Holding_MyDematAct_HoldingDates", Name = "Holding_MyDematAct_HoldingDates")]
        public IActionResult Holding_MyDematAct_HoldingDates()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var getData = _tradeWebRepository.Holding_MyDematAct_HoldingDates_Execute();
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

        // Get Graph data for dynamic period
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("ISIN_Rates", Name = "ISIN_Rates")]
        public IActionResult ISIN_Rates(string ISIN, string fromDate, string toDate)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    ISIN = objUtility.mfnReplaceForSQLInjection(ISIN);
                    fromDate = objUtility.mfnReplaceForSQLInjection(fromDate);
                    toDate = objUtility.mfnReplaceForSQLInjection(toDate);
                    var getData = _tradeWebRepository.ISIN_Rates_Graph(ISIN, fromDate, toDate);
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
        [HttpGet("GetDpDetails", Name = "GetDpDetails")]
        public IActionResult GetDpDetails(string BOID)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    BOID = objUtility.mfnReplaceForSQLInjection(BOID);
                    var getData = _tradeWebRepository.Get_Holding_DpDetail(BOID);
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
