using System;
using System.Threading.Tasks;
using RevitToIfcScheduler.Context;
using RevitToIfcScheduler.Models;
using Flurl;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using RevitToIfcScheduler.Utilities;

namespace RevitToIfcScheduler.Controllers
{
    public class OauthController: ControllerBase
    {
        
        public OauthController(Context.RevitIfcContext revitIfcContext)
        {
            RevitIfcContext = revitIfcContext;
        }
         
        private Context.RevitIfcContext RevitIfcContext { get; set; }
        
        
        [HttpGet]
        [Route("api/forge/oauth/callback")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> OauthCallback([FromQuery] string code, [FromQuery] string state)
        {
            try
            {
                if (code != null && state != null)
                {
                    var threeLeggedToken = await TokenManager.GetThreeLeggedTokenFromCode(code, HttpContext);

                    var sessionKey = Guid.NewGuid();
                    
                    var user = new User()
                    {
                        HashedSessionKey = RevitToIfcScheduler.Models.User.ComputeSha256Hash(sessionKey.ToString()),
                        AutodeskId = "",
                        Token = threeLeggedToken.AccessToken,
                        Refresh = threeLeggedToken.RefreshToken,
                        TokenExpiration = DateTime.UtcNow.AddSeconds(threeLeggedToken.ExpiresIn - 300)
                    };

                    await user.FetchAutodeskDetails();
                    
                    RevitIfcContext.Users.Add(user);
                    await RevitIfcContext.SaveChangesAsync();
                    HttpContext.Response.Cookies.Append(AppConfig.AppId, sessionKey.ToString());
                    
                    var redirectString = Base64Encoder.Decode(state);
                    return Redirect(redirectString);
                }
                else
                {
                    return Unauthorized();
                }
                
            }
            catch (Exception ex)
            {
                Log.Debug(ex, this.GetType().FullName);
                return BadRequest(ex);
            }
        }
        [HttpGet]
        [Route("api/forge/oauth/url")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult GetOAuthURL([FromQuery] string encodedRedirectUrl)
        {
            try
            {
                var forgeAuthUrl = "https://developer.api.autodesk.com/authentication/v1/authorize"
                    .SetQueryParams(new
                    {
                        client_id = AppConfig.ClientId,
                        response_type = "code",
                        redirect_uri = TokenManager.GetRedirectUrl(HttpContext),
                        scope = AppConfig.ThreeLegScope,
                        state = encodedRedirectUrl
                    });
                
                return Ok(forgeAuthUrl.ToString());
            }
            catch (Exception ex)
            {
                var msg = $"SessionAuthController.GetOAuthURL: {ex.Message}";
                Log.Debug(msg);
                return BadRequest(msg);
            }
        }
        
    }
}