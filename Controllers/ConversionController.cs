﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RevitToIfcScheduler.Context;
using RevitToIfcScheduler.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using RevitToIfcScheduler.Utilities;
using Microsoft.EntityFrameworkCore;

namespace RevitToIfcScheduler.Controllers
{
    public class ConversionController: ControllerBase
    {
        public ConversionController(Context.RevitIfcContext revitIfcContext)
        {
            RevitIfcContext = revitIfcContext;
        }
         
        private Context.RevitIfcContext RevitIfcContext { get;}
        
        [HttpPost]
        [Route("api/projects/{projectId}/conversions")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> PostConversionJob([FromBody] ConversionBatch conversionBatch, string projectId)
        {
            try
            {
                if (!Authentication.IsAuthorized(HttpContext, RevitIfcContext, new List<AccountRole>(){AccountRole.ProjectAdmin, AccountRole.AccountAdmin, AccountRole.ApplicationAdmin}, projectId)) return Unauthorized();
                var user = RevitToIfcScheduler.Models.User.FetchByContext(HttpContext, RevitIfcContext);
                
                //TODO: Don't repeat files
                //Let this work be done asynchronously
                Converter.ProcessConversionBatch(user, RevitIfcContext, projectId, conversionBatch, null);
                
                return Ok();
            }
            catch (Exception ex)
            {
                Log.Debug(ex, this.GetType().FullName);
                return BadRequest(ex);
            }
        }
        
        [HttpGet]
        [Route("api/projects/{projectId}/conversions")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetConversionJobs(string projectId, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            try
            {
                if (!Authentication.IsAuthorized(HttpContext, RevitIfcContext, new List<AccountRole>(){AccountRole.ProjectAdmin, AccountRole.AccountAdmin, AccountRole.ApplicationAdmin}, projectId)) return Unauthorized();

                var conversions = await RevitIfcContext.ConversionJobs
                    .Where(x => x.ProjectId == projectId)
                    .Include(x=>x.JobSchedule)
                    .Select(p => new ConversionJob()
                    {
                        Id = p.Id,
                        HubId = p.HubId,
                        ProjectId = p.ProjectId,
                        FolderId = p.FolderId,
                        IfcSettingsSetName = p.IfcSettingsSetName,
                        JobSchedule = p.JobSchedule != null ? new Schedule()
                        {
                            Id = p.JobSchedule.Id,
                            Name = p.JobSchedule.Name
                        } : null,
                        FileUrn = p.FileUrn,
                        FileName = p.FileName,
                        ItemId = p.ItemId,
                        JobCreated = p.JobCreated,
                        JobFinished = p.JobFinished,
                        Status = p.Status,
                        Notes = p.Notes,
                        CreatedBy = p.CreatedBy,
                        IsCompositeDesign = p.IsCompositeDesign
                    })
                    .OrderByDescending(x=>x.JobCreated)
                    .Skip(offset)
                    .Take(limit)
                    .ToListAsync();

                return Ok(conversions);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, this.GetType().FullName);
                return BadRequest(ex);
            }
        }
        
        [HttpGet]
        [Route("api/projects/{projectId}/schedules/{scheduleId}/conversions")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetScheduleConversionJobs(string projectId, Guid scheduleId, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            try
            {
                if (!Authentication.IsAuthorized(HttpContext, RevitIfcContext, new List<AccountRole>(){AccountRole.ProjectAdmin, AccountRole.AccountAdmin, AccountRole.ApplicationAdmin}, projectId)) return Unauthorized();

                var conversions = await RevitIfcContext.ConversionJobs
                    .Where(x => x.ProjectId == projectId && x.JobSchedule.Id == scheduleId)
                    .Include(x=>x.JobSchedule)
                    .Select(p => new ConversionJob()
                    {
                        Id = p.Id,
                        HubId = p.HubId,
                        ProjectId = p.ProjectId,
                        FolderId = p.FolderId,
                        IfcSettingsSetName = p.IfcSettingsSetName,
                        JobSchedule = p.JobSchedule != null ? new Schedule()
                        {
                            Id = p.JobSchedule.Id,
                            Name = p.JobSchedule.Name
                        } : null,
                        FileUrn = p.FileUrn,
                        FileName = p.FileName,
                        ItemId = p.ItemId,
                        JobCreated = p.JobCreated,
                        JobFinished = p.JobFinished,
                        Status = p.Status,
                        Notes = p.Notes,
                        CreatedBy = p.CreatedBy,
                        IsCompositeDesign = p.IsCompositeDesign
                    })
                    .OrderByDescending(x=>x.JobCreated)
                    .Skip(offset)
                    .Take(limit)
                    .ToListAsync();

                return Ok(conversions);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, this.GetType().FullName);
                return BadRequest(ex);
            }
        }
    }
}