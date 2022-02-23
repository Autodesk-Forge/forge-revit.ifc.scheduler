using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RevitToIfcScheduler.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using RevitToIfcScheduler.Utilities;
using System.Linq;

namespace RevitToIfcScheduler.Controllers
{
    public class TimezonesController: ControllerBase
    {
        public TimezonesController(Context.RevitIfcContext revitIfcContext)
        {
            RevitIfcContext = revitIfcContext;
        }
         
        private Context.RevitIfcContext RevitIfcContext { get; set; }
        
        [HttpGet]
        [Route("api/timezones")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<TimeZoneInfo[]> GetTimezones()
        {
            try
            {
                if (!Authentication.IsAuthorized(HttpContext, RevitIfcContext)) return Unauthorized();

                var timeZones = TimeZoneInfo.GetSystemTimeZones();
                var timeZoneIds = new List<string>();
                foreach(var timeZone in timeZones)
                {
                    timeZoneIds.Add(timeZone.Id);
                }

                timeZoneIds.Sort();
                
                return Ok(timeZoneIds);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, this.GetType().FullName);
                return BadRequest(ex);
            }
        }
    }
}