using Microsoft.AspNetCore.Mvc;
using TradeWeb.API.Models;
using TradeWeb.API.Service;

namespace TradeWeb.API.Controllers
{
    /// <summary>
    /// Compares two names and returns similarity scores (Token Sort, Token Set,
    /// Jaro-Winkler) with a final weighted score and match status.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class NameMatchController : ControllerBase
    {
        /// <summary>
        /// Matches two names and returns similarity scores.
        /// </summary>
        /// <param name="request">Object containing Name1 and Name2 to compare.</param>
        [HttpPost("Match")]
        public IActionResult Match([FromBody] NameMatchRequest request)
        {
            if (request == null)
                return BadRequest(new NameMatchResult { Status = "Fail", MatchStatus = "INVALID INPUT" });

            var result = NameMatcher.Match(request.Name1, request.Name2);
            return Ok(result);
        }
    }
}
