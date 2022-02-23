using System;
using System.Threading.Tasks;
using RevitToIfcScheduler.Context;
using RevitToIfcScheduler.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using RevitToIfcScheduler.Utilities;
using JWT.Algorithms;
using JWT.Builder;

namespace RevitToIfcScheduler.Controllers
{
    public class AuthController: ControllerBase
    {
        public AuthController(Context.RevitIfcContext revitIfcContext)
        {
            RevitIfcContext = revitIfcContext;
        }
         
        private Context.RevitIfcContext RevitIfcContext { get; set; }
        
        
        [HttpGet]
        [Route("api/auth/user")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetUser()
        {
            try
            {
                if (!Authentication.IsAuthorized(HttpContext, RevitIfcContext)) return Unauthorized();

                var user = RevitToIfcScheduler.Models.User.FetchByContext(HttpContext, RevitIfcContext);
                
                if (user != null)
                {
                    var accounts = await RevitIfcContext.Accounts.ToListAsync();
                    var twoLeggedToken = await TokenManager.GetTwoLeggedToken();
                    await user.FetchPermissions(accounts, twoLeggedToken);
                    RevitIfcContext.Users.Update(user);
                    await RevitIfcContext.SaveChangesAsync();
                    
                    
                    var jwtToken = new JwtBuilder()
                        .WithAlgorithm(new HMACSHA256Algorithm()) // symmetric
                        .WithSecret(AppConfig.ClientSecret + HttpContext.Request.Cookies[AppConfig.AppId])
                        .AddClaim("expires", DateTime.UtcNow.AddSeconds(3600))
                        .AddClaim("applicationAdmin", user.HasPermission(AccountRole.ApplicationAdmin))
                        .Encode();
                    HttpContext.Response.Cookies.Append(AppConfig.AppId + "_jwt", jwtToken);
                    
                    
                    return Ok(user);
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
        [Route("api/auth/logout")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> LogoutUser()
        {
            try
            {
                var url = (HttpContext.Request.IsHttps ? "https://" : "http://") +
                           HttpContext.Request.Host.ToUriComponent();
                
                if (Authentication.IsAuthorized(HttpContext, RevitIfcContext))
                {
                    var user = RevitToIfcScheduler.Models.User.FetchByContext(HttpContext, RevitIfcContext);
                    if (user != null)
                    {
                        RevitIfcContext.Users.Remove(user);
                        await RevitIfcContext.SaveChangesAsync();
                        HttpContext.Response.Cookies.Append(AppConfig.AppId, "");
                    }
                }
                
                return Redirect(url);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, this.GetType().FullName);
                return BadRequest(ex);
            }
        }
    }
}