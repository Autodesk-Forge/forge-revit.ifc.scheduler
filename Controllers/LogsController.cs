using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using RevitToIfcScheduler.Context;
using RevitToIfcScheduler.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using RevitToIfcScheduler.Utilities;

namespace RevitToIfcScheduler.Controllers
{
    public class LogsController : ControllerBase
    {
        public LogsController(Context.RevitIfcContext revitIfcContext)
        {
            RevitIfcContext = revitIfcContext;
        }

        private Context.RevitIfcContext RevitIfcContext { get; }

        private string LogFolderFullPath
        {
            get
            {
                var logPath = AppConfig.LogPath;
                // get the file attributes for file or directory
                FileAttributes attr = System.IO.File.GetAttributes(logPath);
                if (!attr.HasFlag(FileAttributes.Directory))
                {
                    logPath = new FileInfo(logPath).Directory.FullName;
                }

                return logPath;
            }
        }

        [HttpGet]
        [Route("api/logs")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetAllLogs()
        {
            try
            {
                if (!Authentication.IsAuthorized(HttpContext, RevitIfcContext, new List<AccountRole>() { AccountRole.AccountAdmin, AccountRole.ApplicationAdmin })) return Unauthorized();

                //Get list of file names from log location
                DirectoryInfo logFolder = new DirectoryInfo(this.LogFolderFullPath);
                FileInfo[] Files = logFolder.GetFiles("*.txt");

                List<string> fileNames = new List<string>();

                foreach (var file in Files)
                {
                    fileNames.Add(file.Name);
                }

                return Ok(fileNames);
            }
            catch (Exception ex)
            {
                Log.Error($"Could not retrieve logs: {ex}", this.GetType().FullName);
                return BadRequest(ex);
            }
        }

        [HttpGet]
        [Route("api/logs/{fileName}")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetSingleLog(string fileName)
        {
            try
            {
                if (!Authentication.IsAuthorized(HttpContext, RevitIfcContext, new List<AccountRole>() { AccountRole.AccountAdmin, AccountRole.ApplicationAdmin })) return Unauthorized();

                var path = Path.Combine(this.LogFolderFullPath, fileName);

                //FileStream necessary as the latest file is currently being used by the logger
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var sr = new StreamReader(fs, Encoding.Default))
                    {
                        var file2 = await sr.ReadToEndAsync();
                        return Ok(file2);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Could not retrieve single log: {ex}", this.GetType().FullName);
                return BadRequest(ex);
            }
        }
    }
}