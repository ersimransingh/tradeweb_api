using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Security.Claims;
using TradeWeb.API.Repository;

namespace TradeWeb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BaseController : Controller
    {

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            UtilityCommon _baseUtilityCommon = new UtilityCommon();
            ClaimsIdentity claimIdentity = context.HttpContext.User.Identity as ClaimsIdentity;
            string userId = claimIdentity.FindFirst("username")?.Value ?? "";

            try
            {
                //if (userId != "")
                //{
                //    string role = claimIdentity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role).Value;
                //    if (role == "Admin")
                //    {
                //        string tokenGuid = _baseUtilityCommon.ExecuteSQLReturn($"select top 1 ls_token from login_session where ls_logintype = 'C' And ls_code = '{userId}' And len(ls_token) > 3 Order by ls_srno desc");
                //        string jti = claimIdentity.FindFirst("jti")?.Value ?? "";
                //        if (tokenGuid != jti)
                //        {
                //            context.Result = new UnauthorizedObjectResult("Unauthorized: You have been logged out since you logged in from a different source / machine.");
                //        }
                //    }
                //}
            }
            catch (Exception)
            {
                base.OnActionExecuting(context);
            }

            base.OnActionExecuting(context);
        }
    }
}
