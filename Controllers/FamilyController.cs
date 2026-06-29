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
    public class FamilyController : BaseController
    {
        private readonly ITradeWebRepository _tradeWebRepository;
        ConvertData returnJson = new ConvertData();
        DataTable retrunDt = new DataTable();
        private readonly UtilityCommon objUtility;
        public FamilyController(ITradeWebRepository tradeWebRepository, UtilityCommon _objUtility)
        {
            _tradeWebRepository = tradeWebRepository;
            objUtility = _objUtility;
        }


        #region new family api

        // TODO : Get Family List
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet("Family_List", Name = "Family_List")]
        public IActionResult Family_List()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

                    var getData = _tradeWebRepository.Family_List(userId);
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

        // TODO : Get Family List
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpPost("Family_Add", Name = "Family_Add")]
        public IActionResult Family_Add(List<FamilyAddButtonModel> model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

                    string getData = null;
                    foreach (var i in model)
                    {
                        getData = _tradeWebRepository.Family_Add(userId, objUtility.mfnReplaceForSQLInjection(i.password), objUtility.mfnReplaceForSQLInjection(i.uccCode));
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

        // TODO : Get Family List
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpPost("Family_Remove", Name = "Family_Remove")]
        public IActionResult Family_Remove(List<Family_Remove_Model> model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

                    string getData = null;
                    foreach (var i in model)
                    {
                        getData = _tradeWebRepository.Family_Remove(objUtility.mfnReplaceForSQLInjection(i.uccCode));
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

        // TODO : Get Family List
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpPost("Family_Balance", Name = "Family_Balance")]
        public IActionResult Family_Balance(List<string> uccCode)
        {
            if (ModelState.IsValid)
            {
                try
                {

                    var getData = _tradeWebRepository.Family_BalanceJson(uccCode);
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

        // TODO : Get Family List
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpPost("Family_RetainedStock", Name = "Family_RetainedStock")]
        public IActionResult Family_RetainedStock(List<string> uccCode)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    //var tokenS = GetToken();
                    //var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;

                    var getData = _tradeWebRepository.Family_RetainedStokeJson(uccCode);
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

        // TODO : Get Family List
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpPost("Family_Holding", Name = "Family_Holding")]
        public IActionResult Family_Holding([FromBody] List<string> uccCode)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var getData = _tradeWebRepository.Family_HoldingJson(uccCode);
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

        // TODO : Get Family List
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpPost("Family_Position", Name = "Family_Position")]
        public IActionResult Family_Position(List<string> uccCode)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var getData = _tradeWebRepository.Family_PositionJson(uccCode);
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

        [Authorize(AuthenticationSchemes = "Bearer")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpPost("Family_Transaction", Name = "Family_Transaction")]
        public IActionResult Family_Transaction(FamilyTransactionModel uccCode)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    uccCode.FromDate = objUtility.mfnReplaceForSQLInjection(uccCode.FromDate);
                    uccCode.ToDate = objUtility.mfnReplaceForSQLInjection(uccCode.ToDate);
                    var getData = _tradeWebRepository.Family_TransactionJson(uccCode);
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
        [HttpGet("Family_Transaction_Details", Name = "Family_Transaction_Details")]
        public IActionResult Family_Transaction_Details(string type, string fromDate, string toDate, string uccCode)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    type = objUtility.mfnReplaceForSQLInjection(type);
                    fromDate = objUtility.mfnReplaceForSQLInjection(fromDate);
                    toDate = objUtility.mfnReplaceForSQLInjection(toDate);
                    uccCode = objUtility.mfnReplaceForSQLInjection(uccCode);
                    var getData = _tradeWebRepository.Family_Transaction_Details(uccCode, type, fromDate, toDate);
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
