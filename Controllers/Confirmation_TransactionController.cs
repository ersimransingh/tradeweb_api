using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.Linq;
using System.Net;
using TradeWeb.API.Repository;
using TradeWeb.Service;

namespace TradeWeb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Confirmation_TransactionController : BaseController
    {
        private readonly UtilityCommon objUtility;
        private readonly ITradeWebRepository _tradeWebRepository;
        ConvertData returnJson = new ConvertData();
        DataTable retrunDt = new DataTable();
        public Confirmation_TransactionController(UtilityCommon objUtility, ITradeWebRepository tradeWebRepository)
        {
            this.objUtility = objUtility;
            _tradeWebRepository = tradeWebRepository;
        }


        #region Confirmation Api

        // get confirmation main data
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("Confirmation", Name = "Confirmation")]
        public IActionResult Confirmation([FromQuery] int type, string date)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    date = objUtility.mfnReplaceForSQLInjection(date);
                    var getData = _tradeWebRepository.Confirmation(userId, type, date);
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
        [HttpGet("Transaction_Detail", Name = "Transaction_Detail")]
        public IActionResult Transaction_Detail([FromQuery] string exch, string seg, int type, string fromdate, string todate, string scripCode)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    exch = objUtility.mfnReplaceForSQLInjection(exch);
                    seg = objUtility.mfnReplaceForSQLInjection(seg);
                    fromdate = objUtility.mfnReplaceForSQLInjection(fromdate);
                    todate = objUtility.mfnReplaceForSQLInjection(todate);
                    scripCode = objUtility.mfnReplaceForSQLInjection(scripCode);
                    var getData = _tradeWebRepository.Transaction_Detail(userId, exch, seg, type, fromdate, todate, scripCode);
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

        #region Transaction Api
        /// <summary>
        ///   Transaction API  
        /// </summary>
        /// <param name="type"></param>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        // TODO : For getting Transaction main form data
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("Transaction_Summary", Name = "Transaction_Summary")]
        public IActionResult Transaction_Summary([FromQuery] string tradeType, string selectType, string fromDate, string toDate)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var companyCode = tokenS.Claims.First(claim => claim.Type == "companyCode").Value;
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    tradeType = objUtility.mfnReplaceForSQLInjection(tradeType);
                    selectType = objUtility.mfnReplaceForSQLInjection(selectType);
                    fromDate = objUtility.mfnReplaceForSQLInjection(fromDate);
                    toDate = objUtility.mfnReplaceForSQLInjection(toDate);
                    var getData = _tradeWebRepository.Transaction_Summary(userId, tradeType, selectType, fromDate, toDate);

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
        /// Used for Receipt, Pament, Jouranal and Bill 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <returns></returns>
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("Transaction_Accounts", Name = "Transaction_Accounts")]
        public IActionResult Transaction_Accounts([FromQuery] string type, string fromDate, string toDate)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var companyCode = tokenS.Claims.First(claim => claim.Type == "companyCode").Value;
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    fromDate = objUtility.mfnReplaceForSQLInjection(fromDate);
                    toDate = objUtility.mfnReplaceForSQLInjection(toDate);
                    type = objUtility.mfnReplaceForSQLInjection(type);
                    var getData = _tradeWebRepository.Transaction_Accounts(userId, type, fromDate, toDate);

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
        [HttpGet("Transaction_AGTS", Name = "Transaction_AGTS")]
        public IActionResult Transaction_AGTS([FromQuery] string segment, string fromDate, string toDate)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var companyCode = tokenS.Claims.First(claim => claim.Type == "companyCode").Value;
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    fromDate = objUtility.mfnReplaceForSQLInjection(fromDate);
                    toDate = objUtility.mfnReplaceForSQLInjection(toDate);
                    segment = objUtility.mfnReplaceForSQLInjection(segment);
                    var getData = _tradeWebRepository.Transaction_AGTS(userId, segment, fromDate, toDate);

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
