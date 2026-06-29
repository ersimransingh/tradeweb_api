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
    public class ChangeDetailController : BaseController
    {
        private readonly ITradeWebRepository _tradeWebRepository;
        private readonly UtilityCommon _objUtility;
        ConvertData returnJson = new ConvertData();
        DataTable retrunDt = new DataTable();
        public ChangeDetailController(ITradeWebRepository tradeWebRepository, UtilityCommon objUtility)
        {
            _tradeWebRepository = tradeWebRepository;
            _objUtility = objUtility;
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPost("GenerateOTP", Name = "GenerateOTP")]
        public IActionResult GenerateOTP(GenerateOtpModel generateOtp)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = _objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var getData = _tradeWebRepository.GetOtpForChangeDetail(userId, generateOtp);
                    if (getData.Otp == "success")
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, getData.Otp, getData, ""));
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
        [HttpPost("GenerateOTPNew", Name = "GenerateOTPNew")]
        public IActionResult GenerateOTPNew(GenerateOtpNew request)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = _objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var getData = _tradeWebRepository.GetGenerateOtpNew(userId, request);
                    if (getData.Otp == "success")
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, getData.Otp, getData, ""));
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
        [HttpPost("UpdateDetail", Name = "UpdateDetail")]
        public IActionResult UpdateDetail([FromBody] UpdateDetailModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = _objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    model.NewEmailId = _objUtility.mfnReplaceForSQLInjection(model.NewEmailId);
                    model.NewMobileNo = _objUtility.mfnReplaceForSQLInjection(model.NewMobileNo);
                    model.Otp = _objUtility.mfnReplaceForSQLInjection(model.Otp);
                    var getData = _tradeWebRepository.UpdateDetail(userId, model);
                    if (getData.Contains("has been"))
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", getData, ""));
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
        [HttpPost("UpdateMobileNo", Name = "UpdateMobileNo")]
        public IActionResult UpdateMobileNo(UpdateMobileNoModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = _objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    model.NewMobileNo = _objUtility.mfnReplaceForSQLInjection(model.NewMobileNo);
                    model.Otp = _objUtility.mfnReplaceForSQLInjection(model.Otp);
                    var getData = _tradeWebRepository.UpdateMobileNo(userId, model);
                    if (getData.Contains("has been"))
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", retrunDt, ""));
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
        [HttpPost("UpdateEmail", Name = "UpdateEmail")]
        public IActionResult UpdateEmail(UpdateEmailModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = _objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    model.NewEmailId = _objUtility.mfnReplaceForSQLInjection(model.NewEmailId);
                    model.Otp = _objUtility.mfnReplaceForSQLInjection(model.Otp);
                    var getData = _tradeWebRepository.UpdateEmail(userId, model);
                    if (getData.Contains("has been"))
                    {
                        return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", getData, ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", retrunDt, ""));
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
        [HttpGet("IncomeList", Name = "IncomeList")]
        public IActionResult IncomeList()
        {
            try
            {
                var getData = _tradeWebRepository.GetIncomeDetail();
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

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet("RelationList", Name = "RelationList")]
        public IActionResult RelationList()
        {
            try
            {
                var getData = _tradeWebRepository.Get_Change_Relation();
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


        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPost("SendOTP", Name = "SendOTP")]
        public IActionResult SendOTP([FromBody] EmailSmsReqModel reqObject)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenS = _objUtility.GetToken();
                    var userId = tokenS.Claims.First(claim => claim.Type == "username").Value;
                    var getData = _tradeWebRepository.SendOTPcommon(userId, reqObject);
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



    }
}
