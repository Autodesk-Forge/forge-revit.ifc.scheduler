using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RevitToIfcScheduler.Context;
using RevitToIfcScheduler.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using RevitToIfcScheduler.Utilities;

namespace RevitToIfcScheduler.Controllers
{
    public class AccountsController: ControllerBase
    {
        public AccountsController(Context.RevitIfcContext revitIfcContext)
        {
            RevitIfcContext = revitIfcContext;
        }
         
        private Context.RevitIfcContext RevitIfcContext { get; set; }
        
        
        [HttpPost]
        [Route("api/accounts")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> PostAccount([FromBody] Account newItem)
        {
            try
            {
                if (!Authentication.IsAuthorized(HttpContext, RevitIfcContext, new List<AccountRole>(){AccountRole.AccountAdmin, AccountRole.ApplicationAdmin})) return Unauthorized();

                var item = await RevitIfcContext.Accounts.FindAsync(newItem.Id);
                if (item == null)
                {
                    await RevitIfcContext.Accounts.AddAsync(newItem);
                    await RevitIfcContext.SaveChangesAsync();

                    return Created($"/api/accounts", newItem);
                }
                else
                {
                    return Ok(newItem);
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, GetType().FullName);
                return BadRequest(ex);
            }
        }
        
        [HttpDelete]
        [Route("api/accounts/{id}")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> DeleteAccount(string id)
        {
            try
            {
                if (!Authentication.IsAuthorized(HttpContext, RevitIfcContext, new List<AccountRole>(){AccountRole.AccountAdmin, AccountRole.ApplicationAdmin})) return Unauthorized();
                var item = await RevitIfcContext.Accounts.FindAsync(id);
                if (item == null) return NotFound(id);
                
                RevitIfcContext.Accounts.Remove(item);
                await RevitIfcContext.SaveChangesAsync();
                
                return NoContent();
            }
            catch (Exception ex)
            {
                Log.Debug(ex, GetType().FullName);
                return BadRequest(ex);
            }
        }
    }
}