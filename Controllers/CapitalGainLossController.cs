using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.Linq;
using System.Net;
using TradeWeb.API.Models;
using TradeWeb.API.Repository;
using TradeWeb.Service;

namespace TradeWeb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CapitalGainLossController : BaseController
    {

        private readonly ITradeWebRepository _tradeWebRepository;
        ConvertData returnJson = new ConvertData();
        DataTable retrunDt = new DataTable();
        private readonly UtilityCommon objUtility;

        public CapitalGainLossController(ITradeWebRepository tradeWebRepository, UtilityCommon objUtility)
        {
            _tradeWebRepository = tradeWebRepository;
            this.objUtility = objUtility;
        }

        #region CapitalGainLoss Api

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin,GainLoss")]
        [HttpGet("CapitalGainLoss_Dividend", Name = "CapitalGainLoss_Dividend")]
        public IActionResult CapitalGainLoss_Dividend(string fromDate, string toDate)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    fromDate = objUtility.mfnReplaceForSQLInjection(fromDate);
                    toDate = objUtility.mfnReplaceForSQLInjection(toDate);
                    userId = " and cm_cd = '" + userId + "' ";
                    var getData = _tradeWebRepository.CapitalGainLoss_Dividend_Process(userId, fromDate, toDate);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin,GainLoss")]
        [HttpGet("CapitalGainLoss_ActualPLSummary", Name = "CapitalGainLoss_ActualPLSummary")]
        public IActionResult CapitalGainLoss_ActualPLSummary(string fromDate, string toDate, bool ignore112A, bool isDetails)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    fromDate = objUtility.mfnReplaceForSQLInjection(fromDate);
                    toDate = objUtility.mfnReplaceForSQLInjection(toDate);

                    userId = " and cm_cd = '" + userId + "' ";
                    var getData = _tradeWebRepository.CapitalGainLoss_ActualPLSummary_Process(userId, fromDate, toDate, ignore112A, isDetails);
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


        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin,GainLoss")]
        [HttpGet("CapitalGainLoss_ActualPLDetail", Name = "CapitalGainLoss_ActualPLDetail")]
        public IActionResult CapitalGainLoss_ActualPLDetail(string fromDate, string toDate, bool blnignore112A, string scripCode)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    fromDate = objUtility.mfnReplaceForSQLInjection(fromDate);
                    toDate = objUtility.mfnReplaceForSQLInjection(toDate);
                    scripCode = objUtility.mfnReplaceForSQLInjection(scripCode);
                    userId = " and cm_cd = '" + userId + "' ";
                    var getData = _tradeWebRepository.CapitalGainLoss_ActualPLDetail_Process(userId, fromDate, toDate, blnignore112A, scripCode);
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


        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin,GainLoss")]
        [HttpGet("CapitalGainLoss_TradeListingSummary", Name = "CapitalGainLoss_TradeListingSummary")]
        public IActionResult CapitalGainLoss_TradeListingSummary(string fromDate, string toDate)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    fromDate = objUtility.mfnReplaceForSQLInjection(fromDate);
                    toDate = objUtility.mfnReplaceForSQLInjection(toDate);
                    userId = " and cm_cd = '" + userId + "' ";
                    var getData = _tradeWebRepository.CapitalGainLoss_TradeListingSummary_Process(userId, fromDate, toDate);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin,GainLoss")]
        [HttpGet("CapitalGainLoss_TradeListingDetail", Name = "CapitalGainLoss_TradeListingDetail")]
        public IActionResult CapitalGainLoss_TradeListingDetail(string fromDate, string toDate, string scripCode)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    fromDate = objUtility.mfnReplaceForSQLInjection(fromDate);
                    toDate = objUtility.mfnReplaceForSQLInjection(toDate);
                    scripCode = objUtility.mfnReplaceForSQLInjection(scripCode);
                    userId = " and cm_cd = '" + userId + "' ";
                    var getData = _tradeWebRepository.CapitalGainLoss_TradeListingDetail_Process(userId, fromDate, toDate, scripCode);
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin,GainLoss")]
        [HttpPost("CapitalGainLoss_TradeInsert", Name = "CapitalGainLoss_TradeInsert")]
        public IActionResult CapitalGainLoss_TradeInsert(GainLossTradeInsertModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    string getData = null;
                    foreach (var i in model.data)
                    {
                        getData = _tradeWebRepository.CapitalGainLoss_TradeInsert_Process(userId, objUtility.mfnReplaceForSQLInjection(i.Date), objUtility.mfnReplaceForSQLInjection(i.Settelment), objUtility.mfnReplaceForSQLInjection(i.Flag), objUtility.mfnReplaceForSQLInjection(i.Type), i.Quantity, i.NetRate, i.ServiceTax, i.STT, i.OtherCharge1, i.OtherCharge2, objUtility.mfnReplaceForSQLInjection(i.ScripCode));
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

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin,GainLoss")]
        [HttpGet("CapitalGainLoss_NationalDetail", Name = "CapitalGainLoss_NationalDetail")]
        public IActionResult CapitalGainLoss_NationalDetail(string strDate, Boolean ignore112A, string scripCode)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    strDate = objUtility.mfnReplaceForSQLInjection(strDate);
                    scripCode = objUtility.mfnReplaceForSQLInjection(scripCode);
                    userId = " and cm_cd = '" + userId + "' ";
                    var getData = _tradeWebRepository.CapitalGainLoss_NationalDetail_Process(userId, strDate, ignore112A, scripCode);
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
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin,GainLoss")]
        [HttpGet("CapitalGainLoss_NationalSummary", Name = "CapitalGainLoss_NationalSummary")]
        public IActionResult CapitalGainLoss_NationalSummary(string strDate, Boolean ignore112A)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    strDate = objUtility.mfnReplaceForSQLInjection(strDate);
                    userId = " and cm_cd = '" + userId + "' ";
                    var getData = _tradeWebRepository.CapitalGainLoss_NationalSummary_Process(userId, strDate, ignore112A);
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
    }
}