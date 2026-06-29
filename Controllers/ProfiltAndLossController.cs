using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
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
    public class ProfiltAndLossController : BaseController
    {
        private readonly ITradeWebRepository _tradeWebRepository;
        ConvertData returnJson = new ConvertData();
        DataTable retrunDt = new DataTable();
        private readonly UtilityCommon objUtility;
        public ProfiltAndLossController(ITradeWebRepository tradeWebRepository, UtilityCommon _objUtility)
        {
            _tradeWebRepository = tradeWebRepository;
            objUtility = _objUtility;
        }

        #region ProfitLoss Api

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("GetSegment", Name = "GetSegment")]
        public IActionResult GetSegment()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var getData = _tradeWebRepository.ProfitLoss_Segment();
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
        [HttpGet("Exchange", Name = "Exchange")]
        public IActionResult Exchange([FromQuery] string segment)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    segment = objUtility.mfnReplaceForSQLInjection(segment);
                    var getData = _tradeWebRepository.ProfitLoss_Exchange(segment);
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
        /// <summary>
        /// Get ProfitLoss Main Data
        /// </summary>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <returns></returns>
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("ProfitLoss_Cash_Summary", Name = "ProfitLoss_Cash_Summary")]
        public IActionResult ProfitLoss_Cash_Summary([FromQuery] string fromDate, string toDate)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var companyCode = tokenS.Claims.First(claim => claim.Type == "companyCode").Value;
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    userId = " and cm_cd = '" + userId + "' ";
                    fromDate = objUtility.mfnReplaceForSQLInjection(fromDate);
                    toDate = objUtility.mfnReplaceForSQLInjection(toDate);
                    var getData = _tradeWebRepository.ProfitLoss_CashSummary(userId, fromDate, toDate);
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
        [HttpGet("ProfitLoss_Cash_Detail", Name = "ProfitLoss_Cash_Detail")]
        public IActionResult ProfitLoss_Cash_Detail([FromQuery] string fromDate, string toDate, string scripCode)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    userId = " and cm_cd = '" + userId + "' ";
                    fromDate = objUtility.mfnReplaceForSQLInjection(fromDate);
                    toDate = objUtility.mfnReplaceForSQLInjection(toDate);
                    scripCode = objUtility.mfnReplaceForSQLInjection(scripCode);
                    var getData = _tradeWebRepository.ProfitLoss_CashDetail(userId, fromDate, toDate, scripCode);
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
        [HttpGet("ProfitLoss_FO_Summary", Name = "ProfitLoss_FO_Summary")]
        public IActionResult ProfitLoss_FO_Summary([FromQuery] string exchange, string segment, string fromDate, string toDate, bool includeBfOptions, int bfOptionPL)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var companyCode = tokenS.Claims.First(claim => claim.Type == "companyCode").Value;
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    userId = " and cm_cd = '" + userId + "' ";
                    exchange = objUtility.mfnReplaceForSQLInjection(exchange);
                    segment = objUtility.mfnReplaceForSQLInjection(segment);
                    fromDate = objUtility.mfnReplaceForSQLInjection(fromDate);
                    toDate = objUtility.mfnReplaceForSQLInjection(toDate);
                    var getData = _tradeWebRepository.ProfitLoss_FO_Summary(userId, exchange, segment, fromDate, toDate, includeBfOptions, bfOptionPL);
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
        [HttpGet("ProfitLoss_Commodity_Summary", Name = "ProfitLoss_Commodity_Summary")]
        public IActionResult ProfitLoss_Commodity_Summary([FromQuery] string exchange, string fromDate, string toDate)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var companyCode = tokenS.Claims.First(claim => claim.Type == "companyCode").Value;
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    userId = " and cm_cd = '" + userId + "' ";
                    fromDate = objUtility.mfnReplaceForSQLInjection(fromDate);
                    toDate = objUtility.mfnReplaceForSQLInjection(toDate);
                    exchange = objUtility.mfnReplaceForSQLInjection(exchange);
                    var getData = _tradeWebRepository.ProfitLoss_Commodity_Summary(userId, exchange, fromDate, toDate);
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
        [HttpPost("ProfitLoss_Combined", Name = "ProfitLoss_Combined")]
        public IActionResult ProfitLoss_Combined(List<ProfitLossCombinedInputModel> model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var companyCode = tokenS.Claims.First(claim => claim.Type == "companyCode").Value;
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

                    userId = " and cm_cd = '" + userId + "' ";
                    var getData = _tradeWebRepository.ProfitLoss_Combined(userId, model);
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

        //// Stock portfolio concentration
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("StockPortfolioConc", Name = "StockPortfolioConc")]
        public IActionResult StockPortfolioConc(string fromDate, string toDate)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var companyCode = tokenS.Claims.First(claim => claim.Type == "companyCode").Value;
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    fromDate = objUtility.mfnReplaceForSQLInjection(fromDate);
                    toDate = objUtility.mfnReplaceForSQLInjection(toDate);
                    userId = " and cm_cd = '" + userId + "' ";
                    var getData = _tradeWebRepository.StockPortfolioConc(fromDate, toDate, userId);
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

        //#region CapitalGainLoss Api

        //[Authorize(AuthenticationSchemes = "Bearer")]
        //[HttpGet("CapitalGainLoss_Dividend", Name = "CapitalGainLoss_Dividend")]
        //public IActionResult CapitalGainLoss_Dividend(string fromDate, string toDate)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            var tokenS = GetToken();
        //            var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

        //            var getData = _tradeWebRepository.GetINVPLDivListing(userId, fromDate, toDate);
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

        //[Authorize(AuthenticationSchemes = "Bearer")]
        //[HttpGet("CapitalGainLoss_ActualPLSummary", Name = "CapitalGainLoss_ActualPLSummary")]
        //public IActionResult CapitalGainLoss_ActualPLSummary(string fromDate, string toDate, Boolean jobing, Boolean delivery, Boolean ignore112A, string type)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            var tokenS = GetToken();
        //            var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

        //            var getData = _tradeWebRepository.GetINVPLGainLoss(userId, fromDate, toDate, jobing, delivery, ignore112A, type);
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

        //[Authorize(AuthenticationSchemes = "Bearer")]
        //[HttpGet("CapitalGainLoss_ActualPLDetail", Name = "CapitalGainLoss_ActualPLDetail")]
        //public IActionResult CapitalGainLoss_ActualPLDetail(string fromDate, string toDate, string type, string ignore112A, string scripCode)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            var tokenS = GetToken();
        //            var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

        //            var getData = _tradeWebRepository.GetINVPLGainLossDetails(userId, fromDate, toDate, type, ignore112A, scripCode);
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


        //[Authorize(AuthenticationSchemes = "Bearer")]
        //[HttpGet("CapitalGainLoss_TradeListingSummary", Name = "CapitalGainLoss_TradeListingSummary")]
        //public IActionResult CapitalGainLoss_TradeListingSummary(string fromDate, string toDate)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            var tokenS = GetToken();
        //            var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

        //            var getData = _tradeWebRepository.GetINVPLTradeListing(userId, fromDate, toDate);
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

        //[Authorize(AuthenticationSchemes = "Bearer")]
        //[HttpGet("CapitalGainLoss_TradeListingDetail", Name = "CapitalGainLoss_TradeListingDetail")]
        //public IActionResult CapitalGainLoss_TradeListingDetail(string fromDate, string toDate, string scripCode)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            var tokenS = GetToken();
        //            var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

        //            var getData = _tradeWebRepository.GeTINVPLTradeListingDetails(userId, fromDate, toDate, scripCode);
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

        //[Authorize(AuthenticationSchemes = "Bearer")]
        //[HttpPost("CapitalGainLoss_TradeInsert", Name = "CapitalGainLoss_TradeInsert")]
        //public IActionResult CapitalGainLoss_TradeInsert(GainLossTradeInsertModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            var tokenS = GetToken();
        //            var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

        //            string getData = null;
        //            foreach (var i in model.data)
        //            {
        //                getData = _tradeWebRepository.GetINVPLTradeListingSave(userId, i.Date, i.Settelment, i.Flag, i.Type, i.Quantity, i.NetRate, i.ServiceTax, i.STT, i.OtherCharge1, i.OtherCharge2, i.ScripCode);
        //            }
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
        //[Authorize(AuthenticationSchemes = "Bearer")]
        //[HttpGet("CapitalGainLoss_NationalDetail", Name = "CapitalGainLoss_NationalDetail")]
        //public IActionResult CapitalGainLoss_NationalDetail(string strDate, string type, string ignore112A, string scripCode)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            var tokenS = GetToken();
        //            var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

        //            var getData = _tradeWebRepository.GeTINVPLNationalDetail(userId, strDate, type, ignore112A, scripCode);
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
        //[Authorize(AuthenticationSchemes = "Bearer")]
        //[HttpGet("CapitalGainLoss_NationalSummary", Name = "CapitalGainLoss_NationalSummary")]
        //public IActionResult CapitalGainLoss_NationalSummary(string strDate, Boolean ignore112A, string type)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            var tokenS = GetToken();
        //            var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

        //            var getData = _tradeWebRepository.GetINVPLNationalSummary(userId, strDate, ignore112A, type);
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

        //#endregion
    }
}