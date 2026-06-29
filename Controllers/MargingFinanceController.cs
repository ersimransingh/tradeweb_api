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
    public class MargingFinanceController : BaseController
    {
        private readonly ITradeWebRepository _tradeWebRepository;
        ConvertData returnJson = new ConvertData();
        DataTable retrunDt = new DataTable();
        private readonly UtilityCommon objUtility;
        public MargingFinanceController(ITradeWebRepository tradeWebRepository, UtilityCommon _objUtility)
        {
            _tradeWebRepository = tradeWebRepository;
            objUtility = _objUtility;
        }

        #region Marging Finance Api

        // Get Trades Data
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("TradeListing", Name = "TradeListing")]
        public IActionResult GetTradeListingData([FromQuery] string type, string fromDate, string toDate)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    type = objUtility.mfnReplaceForSQLInjection(type);
                    fromDate = objUtility.mfnReplaceForSQLInjection(fromDate);
                    toDate = objUtility.mfnReplaceForSQLInjection(toDate);
                    var getData = _tradeWebRepository.GetTradeListingData(userId, type, fromDate, toDate);
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

        // Get Trades Data
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("TradeListingDetail", Name = "TradeListingDetail")]
        public IActionResult GetTradeListingDetailData([FromQuery] string fromDate, string toDate, string scripCode)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    fromDate = objUtility.mfnReplaceForSQLInjection(fromDate);
                    toDate = objUtility.mfnReplaceForSQLInjection(toDate);
                    scripCode = objUtility.mfnReplaceForSQLInjection(scripCode);
                    var getData = _tradeWebRepository.GetTradeListingDetailData(userId, fromDate, toDate, scripCode);
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

        // Get Status Main Grid Data.
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("Status", Name = "Status")]
        public IActionResult GetStatusMainGridData()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var compCode = tokenS.Claims.First(claim => claim.Type == "companyCode").Value;
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

                    var getData = _tradeWebRepository.GetStatusMainGridData(userId, compCode);
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


        #region Old Methods
        //// get Temp table RmsSummary Data for status module.
        //[Authorize(AuthenticationSchemes = "Bearer")]
        //[HttpGet("GetTempRMSSummaryData", Name = "GetTempRMSSummaryData")]
        //public IActionResult GetTempRMSSummaryData()
        //{
        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            var tokenS = GetToken();
        //            var compCode = tokenS.Claims.First(claim => claim.Type == "companyCode").Value;
        //            var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

        //            var getData = _tradeWebRepository.MTFRMSSymmaryData(userId, compCode);
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


        //// Get fund data of status module.
        //[Authorize(AuthenticationSchemes = "Bearer")]
        //[HttpGet("GetStatusFundData", Name = "GetStatusFundData")]
        //public IActionResult GetStatusFundData()
        //{
        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            var tokenS = GetToken();
        //            var compCode = tokenS.Claims.First(claim => claim.Type == "companyCode").Value;
        //            var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

        //            var getData = _tradeWebRepository.GetStatusFundData(userId, compCode);
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


        //// Get collateral data of status module.
        //[Authorize(AuthenticationSchemes = "Bearer")]
        //[HttpGet("GetStatusCollateralData", Name = "GetStatusCollateralData")]
        //public IActionResult GetStatusCollateralData()
        //{
        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            var tokenS = GetToken();
        //            var compCode = tokenS.Claims.First(claim => claim.Type == "companyCode").Value;
        //            var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

        //            var getData = _tradeWebRepository.GetStatusCollateralData(userId, compCode);
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
        #endregion

        //Get Approved Security Data
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("ApprovedSecurity", Name = "ApprovedSecurity")]
        public IActionResult ApprovedSecurity()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var dataList = _tradeWebRepository.GetprSecurityListRptHandler();
                    if (dataList != null)
                    {
                        return Ok(dataList);
                    }
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", retrunDt, ""));
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }


        //Get margin trading finance shortfall main grid data
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("ShortFall", Name = "ShortFall")]
        public IActionResult GetFinanceShortFallMainGridData(string AsOnDate)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    AsOnDate = objUtility.mfnReplaceForSQLInjection(AsOnDate);
                    var getData = _tradeWebRepository.GetShortFallMainGridData(userId, AsOnDate);
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
            var token = handler.ReadToken(authHeader) as JwtSecurityToken;
            return token;
        }
        #endregion
    }
}
