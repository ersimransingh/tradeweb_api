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
    public class ModificationController : BaseController
    {
        private readonly ITradeWebRepository _tradeWebRepository;
        ConvertData returnJson = new ConvertData();
        DataTable retrunDt = new DataTable();
        private readonly UtilityCommon objUtility;
        public ModificationController(ITradeWebRepository tradeWebRepository, UtilityCommon _objUtility)
        {
            _tradeWebRepository = tradeWebRepository;
            objUtility = _objUtility;
        }


        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPost("Client_Closure", Name = "Client_Closure")]
        public IActionResult Client_Closure([FromBody] AccountCloserModel data)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    data.clientCode = objUtility.mfnReplaceForSQLInjection(data.clientCode);
                    data.dematAccount = objUtility.mfnReplaceForSQLInjection(data.dematAccount);
                    data.closureType = objUtility.mfnReplaceForSQLInjection(data.closureType);
                    var getData = _tradeWebRepository.Client_Closer(data, userId);
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
    }
}
