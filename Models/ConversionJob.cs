using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using RevitToIfcScheduler.Context;
using RevitToIfcScheduler.Models;
using RevitToIfcScheduler.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;

namespace RevitToIfcScheduler.Models
{
    public class ConversionJob
    {
        [Key]
        [JsonProperty("id")]
        public Guid Id { get; set; }
        
        [JsonIgnore]
        public string HubId { get; set; }

        [JsonProperty("projectId")]
        public string ProjectId { get; set; }
        
        [JsonProperty("folderId")]
        public string FolderId { get; set; }
        
        [JsonProperty("ifcSettingsSetName")]
        public string IfcSettingsSetName { get; set; }
        
        [JsonProperty("schedule")]
        public Schedule JobSchedule { get; set; }
        
        [JsonProperty("fileUrn")]
        public string FileUrn { get; set; }
        
        [JsonProperty("fileName")]
        public string FileName { get; set; }
        
        [JsonProperty("itemId")]
        public string ItemId { get; set; }
        
        [JsonIgnore]
        public string DerivativeUrn { get; set; }
        
        [JsonIgnore]
        public string ForgeUrl { get; set; }
        
        [JsonProperty("jobCreated")]
        public DateTime JobCreated { get; set; }

        [JsonProperty("jobFinished")] public DateTime? JobFinished { get; set; } = null;
        
        [JsonProperty("status")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ConversionJobStatus Status { get; set; }
        
        [JsonProperty("notes")]
        public string Notes { get; set; }
        
        [JsonProperty("region")]
        public string Region { get; set; }
        
        [JsonProperty("createdBy")]
        public string CreatedBy { get; set; }
        
        [JsonProperty("isCompositeDesign")]
        public bool IsCompositeDesign { get; set; }
        
        [JsonProperty("inputStorageLocation")]
        public string InputStorageLocation { get; set; }
        
        [JsonProperty("outputStorageLocation")]
        public string OutputStorageLocation { get; set; }

        public string EncodedFileUrn
        {
            get
            {
                return string.IsNullOrWhiteSpace(FileUrn) ? "" :Base64Encoder.Encode(FileUrn).Replace('/', '_').Replace('=', ' ').Trim();
            }
            
        }
        public string EncodedInputStorageLocation
        {
            get
            {
                return string.IsNullOrWhiteSpace(InputStorageLocation) ? "" : Base64Encoder.Encode(InputStorageLocation).Replace('=', ' ').Trim();
            }
            
        }
        public string EncodedStorageUrn
        {
            get
            {
                return string.IsNullOrWhiteSpace(EncodedInputStorageLocation) ? EncodedFileUrn : EncodedInputStorageLocation;
            }
            
        }
        
        public void AddLog(string logLine)
        {
            if (string.IsNullOrWhiteSpace(Notes))
            {
                Notes = $"[{DateTime.UtcNow.ToShortTimeString()}] {logLine}";
            }
            else
            {
                Notes += $"\n[{DateTime.UtcNow.ToShortTimeString()}] {logLine}";
            }
        }
        
        public static async Task<ConversionJob> GetJobByUrn(Context.RevitIfcContext revitIfcContext, string fileUrn, ConversionJobStatus conversionJobStatus)
        {
            try
            {
                var decodedFileUrn = Base64Encoder.Decode(fileUrn.Replace("_", "/"));
                return await revitIfcContext.ConversionJobs.FirstAsync(x => x.FileUrn == decodedFileUrn && x.Status == conversionJobStatus);
            }
            catch (Exception exception)
            {
                return null;
            }
        }

        public static async Task OnReceive(ConversionJob conversionJob)
        {
            ServiceProvider provider = (AppConfig.Services as ServiceCollection).BuildServiceProvider();
            Context.RevitIfcContext revitIfcContext = provider.GetService<Context.RevitIfcContext>();
            try{
                conversionJob.AddLog("Received Conversion Job");
                
                var tokenGetter = new TwoLeggedTokenGetter();
                var token = await tokenGetter.GetToken();
            
                if (conversionJob.FolderId == null)
                {
                    conversionJob.FolderId =
                        await Forge.GetFileParentFolderUrn(conversionJob.ProjectId, conversionJob.ItemId, token);
                
                    conversionJob.AddLog($"Added Folder ID: {conversionJob.FolderId}");
                    revitIfcContext.ConversionJobs.Update(conversionJob);
                    await revitIfcContext.SaveChangesAsync();
                }
            
                //Get Derivative URN
                if (conversionJob.DerivativeUrn == null)
                {
                    conversionJob.DerivativeUrn = await Forge.GetIfcDerivativeUrn(conversionJob.FileUrn, token, conversionJob.Region);

                    conversionJob.AddLog($"Retrieved Derivative URN: {conversionJob.DerivativeUrn}");
                    revitIfcContext.ConversionJobs.Update(conversionJob);
                    await revitIfcContext.SaveChangesAsync();
                }
                
                //Create Storage Object
                var storageLocation = await Forge.CreateStorageLocation(conversionJob.ProjectId, conversionJob.FolderId,
                    conversionJob.DerivativeUrn, token);
                conversionJob.AddLog($"Created Storage Location: {storageLocation}");


                var objectId = await Forge.PassDownloadToStorageLocation(conversionJob.DerivativeUrn,
                    conversionJob.FileUrn,
                    storageLocation, 
                    conversionJob,
                    token);
                
                conversionJob.AddLog($"Created Object in Storage Location: {objectId}");
            
                //Remove unsafe characters from the name
                var suffix = string.Empty;
                // var suffix = " - " + (conversionJob.JobSchedule?.Name ?? "ad-hoc")
                //              .Replace("/", "")
                //              .Replace(@"\", "");
                
                //Look for IFC file with same name as revit file in the same folder
                var existingVersion = await Forge.GetExistingVersion(conversionJob.ProjectId, conversionJob.FolderId,
                    conversionJob.FileName, suffix, token);

                
                if (existingVersion == null)
                {
                    //If exists, create new version https://forge.autodesk.com/en/docs/data/v2/reference/http/projects-project_id-versions-POST/
                    await Forge.CreateFirstVersion(conversionJob.ProjectId, conversionJob.FolderId, objectId,
                        conversionJob.FileName, suffix, token);
                    conversionJob.AddLog($"Created First Version of File");
                }
                else
                {
                    //Otherwise, create item https://forge.autodesk.com/en/docs/data/v2/reference/http/projects-project_id-items-POST/
                    await Forge.CreateSubsequentVersion(conversionJob.ProjectId, existingVersion.Split('?').First(), objectId,
                        conversionJob.FileName, suffix, token);
                    conversionJob.AddLog($"Created Next Version of File");
                }

                //Update Status
                conversionJob.AddLog("Conversion Succeeded");
                conversionJob.Status = ConversionJobStatus.Success;
                conversionJob.JobFinished = DateTime.UtcNow;
                revitIfcContext.ConversionJobs.Update(conversionJob);
                
                await revitIfcContext.SaveChangesAsync();

                //Send Completion Email
                await Email.SendConfirmation(conversionJob);

                Log.Information($"Completed Conversion: {conversionJob.Id.ToString()}");
            } catch (Exception exception)
            {
                if (exception.Message.Contains("Revit Version Not Supported"))
                {
                    Log.Error(exception.Message);
                    conversionJob.AddLog(($"Conversion Error (Revit Version Not Supported): {conversionJob.Notes} {exception.Message}").Trim());
                    conversionJob.Status = ConversionJobStatus.Failed;
                    conversionJob.JobFinished = DateTime.UtcNow;
                    revitIfcContext.ConversionJobs.Update(conversionJob);
                    await revitIfcContext.SaveChangesAsync();
                }
                else
                {
                    Log.Error(exception.Message);
                    Log.Error(exception.StackTrace);
                    conversionJob.AddLog(($"Conversion Error: {conversionJob.Notes} {exception.Message}").Trim());
                    revitIfcContext.ConversionJobs.Update(conversionJob);
                    await revitIfcContext.SaveChangesAsync();
                    throw exception;
                }
            }
        }
    }

    public enum ConversionJobStatus
    {
        Processing,
        Converted,
        Success,
        Failed,
        Unchanged,
        ShallowCopy,
        TimeOut,
        Created
    }
}