using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
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
    public class BillsController : BaseController
    {
        private readonly ITradeWebRepository _tradeWebRepository;
        private readonly UtilityCommon objUtility;
        ConvertData returnJson = new ConvertData();
        DataTable retrunDt = new DataTable();
        public BillsController(ITradeWebRepository tradeWebRepository, UtilityCommon objUtility)
        {
            _tradeWebRepository = tradeWebRepository;
            this.objUtility = objUtility;
        }

        #region Bill Api


        //// [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        //[HttpGet("TestAPI", Name = "TestAPI")]
        //public IActionResult TestAPI()
        //{
        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            var getData = _tradeWebRepository.TestAPIRepo();
        //            //var getData = _tradeWebRepository.Get_Change_Relation();
        //            if (getData != null)
        //            {
        //                //commonResponse aa = new commonResponse();
        //                //aa.status = true;
        //                //aa.message = "success";
        //                //aa.status_code = (int)HttpStatusCode.OK;
        //                //aa.data = getData;
        //                //var bb = JsonConvert.SerializeObject(aa, Formatting.Indented);
        //                return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
        //                // return Ok(new commonResponse { status = true, status_code = (int)HttpStatusCode.OK, message = "success", data = JsonConvert.SerializeObject(getData, Formatting.Indented) });
        //            }
        //            else
        //            {
        //                return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "No Record Found", retrunDt, ""));
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
        //        }
        //    }
        //    return BadRequest();
        //}

        //[Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("Bills_exchSeg", Name = "Bills_exchSeg")]
        public IActionResult Bills_exchSeg()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var getData = _tradeWebRepository.Bills_exchSeg();
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

        // get settelment type for dropdown settlement
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("Bills_cash_settTypes_list", Name = "Bills_cash_settTypes_list")]
        public IActionResult Bills_cash_settTypes_list([FromQuery] string exchange)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    exchange = objUtility.mfnReplaceForSQLInjection(exchange);
                    var getData = _tradeWebRepository.Bills_cash_settTypes_list(exchange);
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
        [HttpGet("GetSysParmSt", Name = "GetSysParmSt")]
        public IActionResult GetSysParmSt(string parmId, string tableName)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    parmId = objUtility.mfnReplaceForSQLInjection(parmId);
                    tableName = objUtility.mfnReplaceForSQLInjection(tableName);
                    var dataList = _tradeWebRepository.CommonGetSysParmStHandler(parmId, tableName);
                    if (dataList != null)
                    {
                        return Ok(JsonConvert.SerializeObject(dataList));
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

        // get bill main data
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("Bills_cash_stlmnt", Name = "Bills_cash_stlmnt")]
        public IActionResult Bills_cash_stlmnt([FromQuery] string settelment)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    settelment = objUtility.mfnReplaceForSQLInjection(settelment);
                    string dt = objUtility.fnFireQuery("settlements", "se_stdt", "se_stlmnt", settelment, true);
                    var getData = _tradeWebRepository.Bill_data(userId, settelment.Substring(0, 1) + 'C', settelment.Substring(1, 1), dt);
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
        [HttpGet("Bills_cash_settType", Name = "Bills_cash_settType")]
        public IActionResult Bills_cash_settType([FromQuery] string exch_settType, string date)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    exch_settType = objUtility.mfnReplaceForSQLInjection(exch_settType);
                    date = objUtility.mfnReplaceForSQLInjection(date);
                    var getData = _tradeWebRepository.Bill_data(userId, exch_settType.Substring(0, 1) + 'C', exch_settType.Substring(1, 1), date);
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

        // get bill main data
        //[Authorize(AuthenticationSchemes = "Bearer")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("Bills_FO", Name = "Bills_FO")]
        public IActionResult Bills_FO([FromQuery] string exch, string seg, string date)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    exch = objUtility.mfnReplaceForSQLInjection(exch);
                    seg = objUtility.mfnReplaceForSQLInjection(seg);
                    date = objUtility.mfnReplaceForSQLInjection(date);
                    var getData = _tradeWebRepository.Bill_data(userId, exch + seg, "", date);
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
        [HttpGet("Bills_Commodity", Name = "Bills_Commodity")]
        public IActionResult Bills_Commodity([FromQuery] string exch, string date)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    exch = objUtility.mfnReplaceForSQLInjection(exch);
                    date = objUtility.mfnReplaceForSQLInjection(date);
                    var getData = _tradeWebRepository.Bill_data(userId, exch + "X", "", date);
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
