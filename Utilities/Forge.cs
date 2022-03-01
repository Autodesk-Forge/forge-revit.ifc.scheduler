using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using RevitToIfcScheduler.Models;
using Flurl;
using Flurl.Http;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;
using File = RevitToIfcScheduler.Models.File;

namespace RevitToIfcScheduler.Utilities
{

    public static class Forge
    {
        public static async Task<List<Folder>> GetTopFolders(string hubId, string projectId, string token)
        {
            try
            {
                var folders = new List<Folder>();
                var url = $"{AppConfig.ForgeBaseUrl}/project/v1/hubs/{hubId}/projects/{projectId}/topFolders";
                var data = await url
                    .WithOAuthBearerToken(token)
                    .GetJsonAsync<dynamic>();

                foreach (dynamic folder in data.data)
                {
                    string name = folder.attributes.name;
                    if (folder.attributes.hidden == false && IsValidTopFolder(name))
                    {
                        folders.Add(new Folder()
                        {
                            Id = folder.id,
                            Name = folder.attributes.name,
                            WebView = folder.links.webView.href
                        });
                    }
                }

                return folders;
            }
            catch (Exception exception)
            {
                throw;
            }
        }

        public static async Task<List<Base>> GetFolderContents(string projectId, string folderId, string token)
        {
            try
            {
                var children = new List<Base>();

                var url = $"{AppConfig.ForgeBaseUrl}/data/v1/projects/{projectId}/folders/{folderId}/contents";
                while (true)
                {
                    var data = await url
                        .WithOAuthBearerToken(token)
                        .GetJsonAsync<dynamic>();

                    foreach (dynamic item in data.data)
                    {
                        if (item.attributes.extension.type == "folders:autodesk.bim360:Folder")
                        {
                            children.Add(new Folder()
                            {
                                Id = item.id,
                                Name = item.attributes.name,
                                WebView = item.links.webView.href
                            });
                        }
                    }

                    if (data.included != null)
                    {
                        foreach (dynamic item in data.included)
                        {
                            if (item.attributes.fileType == "rvt" || item.attributes.fileType == "ifc")
                            {
                                children.Add(new File()
                                {
                                    Id = item.id,
                                    Name = item.attributes.name,
                                    ItemId = item.relationships.item.data.id,
                                    FileType = item.attributes.fileType,
                                    FolderId = folderId,
                                    IsCompositeDesign = item.attributes.extension.data.isCompositeDesign ?? false,
                                    WebView = item.links.webView.href
                                });
                            }
                        }
                    }

                    if (data.links != null && data.links.next != null && data.links.next.href != null)
                    {
                        url = data.links.next.href;
                    }
                    else
                    {
                        break;
                    }
                }

                return children;
            }
            catch (Exception exception)
            {
                throw;
            }
        }

        public static async Task<List<File>> GetAllChildRevitFiles(string projectId, List<string> folderIds, TokenGetter tokenGetter)
        {
            var files = new HashSet<File>();
            var fetchedFolderIds = new List<string>();

            while (folderIds.Count > 0)
            {
                var folderUrn = folderIds.PopAt(0);
                if (!fetchedFolderIds.Contains(folderUrn))
                {
                    var token = await tokenGetter.GetToken();
                    var folderContents = await GetFolderContents(projectId, folderUrn, token);

                    foreach (var item in folderContents)
                    {
                        if (item is File file && file.FileType == "rvt")
                        {
                            files.Add(file);
                        }
                        else if (item is Folder)
                        {
                            folderIds.Add(item.Id);
                        }
                    }
                }
                fetchedFolderIds.Add(folderUrn);
            }

            return files.ToList();
        }

        private static bool IsValidTopFolder(string name)
        {
            Guid guidResult;
            if (name.Contains("checklist_") || name.Contains("submittals-attachments") || name.Contains("Photos") ||
                name.Contains("ProjectTb") || name.Contains("dailylog_") || name.Contains("issue_")
                || name.Contains("issues_") || name.Contains("COST Root Folder") || Guid.TryParse(name, out guidResult))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static async Task CheckOrCreateTransientBucket(string bucketKey)
        {
            try
            {
                var token = await new TwoLeggedTokenGetter().GetToken();
                var url = $"{AppConfig.ForgeBaseUrl}/oss/v2/buckets/{bucketKey}/details";

                var response = await url
                    .WithOAuthBearerToken(token)
                    .AllowHttpStatus("4xx")
                    .GetAsync();

                if (response.StatusCode == StatusCodes.Status200OK)
                {
                    Log.Information("Bucket Exists");
                }
                else if (response.StatusCode == StatusCodes.Status404NotFound)
                {
                    Log.Information("Bucket Does not Exist");
                    await CreateTransientBucket(bucketKey, token);
                }
                else
                {
                    Log.Warning("Bucket owned by another ClientID");
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                Log.Error(exception.StackTrace);
                throw;
            }
        }

        public static async Task CreateTransientBucket(string bucketKey, string token)
        {
            try
            {
                var url = $"{AppConfig.ForgeBaseUrl}/oss/v2/buckets";
                var body = new
                {
                    bucketKey,
                    policyKey = "transient"
                };

                await url
                    .WithOAuthBearerToken(token)
                    .PostJsonAsync(body);
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                Log.Error(exception.StackTrace);
                throw;
            }
        }

        public static async Task<string> GetItemStorageLocation(string projectId, string itemId, string token)
        {
            try
            {
                var url = $"{AppConfig.ForgeBaseUrl}/data/v1/projects/{projectId}/items/{itemId}/tip";

                var response = await url
                    .WithOAuthBearerToken(token)
                    .GetJsonAsync<dynamic>();

                return response.data.relationships.storage.meta.link.href;
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                Log.Error(exception.StackTrace);
                throw;
            }
        }
        public static async Task<string> MoveFileToOss(ConversionJob conversionJob, Context.RevitIfcContext revitIfcContext)
        {
            try
            {
                //Move file from WIPDM bucket (or other) into Transient bucket
                conversionJob.AddLog("Moving file to OSS");
                var objectName = conversionJob.Id.ToString() + (conversionJob.IsCompositeDesign ? ".zip" : ".rvt");

                var token = await new TwoLeggedTokenGetter().GetToken();
                var sourceStorageLocation =
                    await GetItemStorageLocation(conversionJob.ProjectId, conversionJob.ItemId, token);
                var targetStorageLocation = $"{AppConfig.ForgeBaseUrl}/oss/v2/buckets/{AppConfig.BucketKey}/objects/{objectName}";

                //Return objectID
                var objectId = await MoveFileFromDmToOss(sourceStorageLocation, targetStorageLocation, token, conversionJob, revitIfcContext);

                conversionJob.AddLog($"Moved file to OSS: {objectId}");

                return objectId;
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                Log.Error(exception.StackTrace);

                conversionJob.AddLog($"FAILED moving file to OSS: {exception.Message}");
                revitIfcContext.ConversionJobs.Update(conversionJob);
                await revitIfcContext.SaveChangesAsync();
                throw;
            }
        }

        public static async Task CreateIfcConversionJob(string hubId, string region, string projectId, string fileUrn, string itemId, string name, string folderId, string exportSettingName, string createdBy, bool isCompositeDesign, Guid? scheduleId)
        {
            try
            {
                ServiceProvider provider = (AppConfig.Services as ServiceCollection).BuildServiceProvider();
                Context.RevitIfcContext revitIfcContext = provider.GetService<Context.RevitIfcContext>();

                var schedule = await revitIfcContext.Schedules.FindAsync(scheduleId);
                var conversionJob = new ConversionJob()
                {
                    Id = Guid.NewGuid(),
                    HubId = hubId,
                    ProjectId = projectId,
                    FileUrn = fileUrn,
                    FileName = name,
                    FolderId = folderId,
                    JobSchedule = schedule,
                    IfcSettingsSetName = exportSettingName,
                    JobCreated = DateTime.UtcNow,
                    CreatedBy = createdBy,
                    ItemId = itemId,
                    Region = region,
                    IsCompositeDesign = isCompositeDesign,
                    Status = ConversionJobStatus.Created
                };

                //Look for identical past jobs -- if already completed, don't repeat.
                var pastSuccess = await revitIfcContext.ConversionJobs.Where(x =>
                        x.FileUrn == conversionJob.FileUrn &&
                        x.IfcSettingsSetName == conversionJob.IfcSettingsSetName &&
                        x.Status == ConversionJobStatus.Success)
                    .FirstOrDefaultAsync();

                conversionJob.AddLog("Created Job");
                revitIfcContext.ConversionJobs.Add(conversionJob);
                await revitIfcContext.SaveChangesAsync();

                if (pastSuccess != null)
                {
                    conversionJob.Status = ConversionJobStatus.Unchanged;
                    conversionJob.AddLog($"File has already been created on {pastSuccess.JobCreated}");
                    conversionJob.AddLog(pastSuccess.Id.ToString());
                    revitIfcContext.ConversionJobs.Update(conversionJob);
                    await revitIfcContext.SaveChangesAsync();
                }
                else
                {
                    BackgroundJob.Enqueue(() => BeginConversionJob(conversionJob.Id));
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                Log.Error(exception.StackTrace);
                throw;
            }
        }

        public static async Task BeginConversionJob(Guid conversionJobId)
        {
            ServiceProvider provider = (AppConfig.Services as ServiceCollection).BuildServiceProvider();
            Context.RevitIfcContext revitIfcContext = provider.GetService<Context.RevitIfcContext>();

            try
            {

                var conversionJob = await revitIfcContext.ConversionJobs.FindAsync(conversionJobId);
                if (conversionJob.IsCompositeDesign && string.IsNullOrWhiteSpace(conversionJob.InputStorageLocation))
                {
                    conversionJob.InputStorageLocation = await MoveFileToOss(conversionJob, revitIfcContext);
                    revitIfcContext.ConversionJobs.Update(conversionJob);
                    await revitIfcContext.SaveChangesAsync();
                }

                TokenGetter tokenGetter = new TwoLeggedTokenGetter();
                var token = await tokenGetter.GetToken();

                var regionSpecifier = conversionJob.Region == "EU" ? "/regions/eu" : "";
                var url = $"{AppConfig.ForgeBaseUrl}/modelderivative/v2{regionSpecifier}/designdata/job";

                var body =
                    new
                    {
                        input = new
                        {
                            urn = string.IsNullOrWhiteSpace(conversionJob.EncodedInputStorageLocation)
                                ? conversionJob.EncodedFileUrn
                                : conversionJob.EncodedInputStorageLocation,
                            compressedUrn = conversionJob.IsCompositeDesign,
                            rootFilename = conversionJob.FileName
                        },
                        output = new
                        {
                            destination = new
                            {
                                region = conversionJob.Region
                            },
                            formats = new List<dynamic>()
                            {
                                new
                                {
                                    type = "IFC",
                                    advanced = new {exportSettingName = conversionJob.IfcSettingsSetName}
                                }
                            }
                        }
                    };


                var inputJson = JsonConvert.SerializeObject(body, Formatting.Indented);
                conversionJob.AddLog("Input JSON:");
                conversionJob.AddLog(inputJson);
                revitIfcContext.ConversionJobs.Update(conversionJob);
                await revitIfcContext.SaveChangesAsync();


                var result = await url
                    .WithOAuthBearerToken(token)
                    .AllowHttpStatus("406")
                    .PostJsonAsync(body);

                var jsonResponse = await result.GetJsonAsync<dynamic>();

                if (result.StatusCode == StatusCodes.Status406NotAcceptable && jsonResponse.diagnostic == "This URN is from a shallow copy, not acceptable for any other modification.")
                {
                    conversionJob.AddLog("This URN is from a shallow copy, not acceptable for any other modification.");
                    //Check settings to determine if the file should be moved to OSS, or if it should be marked as failed
                    if (AppConfig.IncludeShallowCopies)
                    {
                        conversionJob.AddLog("Creating Deep Copy of file in OSS");

                        //Move file to OSS
                        conversionJob.InputStorageLocation = await MoveFileToOss(conversionJob, revitIfcContext);
                        revitIfcContext.ConversionJobs.Update(conversionJob);
                        await revitIfcContext.SaveChangesAsync();

                        //Retry processing
                        BackgroundJob.Enqueue(() => BeginConversionJob(conversionJobId));
                    }
                    else
                    {
                        conversionJob.Status = ConversionJobStatus.ShallowCopy;
                    }
                }
                else
                {
                    conversionJob.Status = result.StatusCode == StatusCodes.Status201Created
                        ? ConversionJobStatus.Unchanged
                        : ConversionJobStatus.Processing;

                    if (conversionJob.Status == ConversionJobStatus.Unchanged)
                    {
                        conversionJob.JobFinished = DateTime.UtcNow;
                        conversionJob.AddLog(($"This file has not changed since the last conversion to IFC. {conversionJob.Notes}").Trim());
                    }

                    BackgroundJob.Enqueue<HangfireJobs>(x => x.PollConversionJob(conversionJob.Id));

                    Log.Information($"Processing Conversion Job: {result.ResponseMessage} {conversionJob.Id}");
                }

                revitIfcContext.ConversionJobs.Update(conversionJob);
                await revitIfcContext.SaveChangesAsync();
            }
            catch (FlurlHttpException exception)
            {
                var conversionJob = await revitIfcContext.ConversionJobs.FindAsync(conversionJobId);
                conversionJob.AddLog($"Conversion Failed: {exception.Message}");
                conversionJob.Status = ConversionJobStatus.Failed;
                conversionJob.JobFinished = DateTime.UtcNow;
                revitIfcContext.ConversionJobs.Update(conversionJob);
                await revitIfcContext.SaveChangesAsync();
                Log.Error(exception.Message);
                Log.Error(exception.StackTrace);
                throw;
            }
            catch (Exception exception)
            {
                var conversionJob = await revitIfcContext.ConversionJobs.FindAsync(conversionJobId);
                conversionJob.AddLog($"Conversion Failed: {exception.Message}");
                conversionJob.Status = ConversionJobStatus.Failed;
                conversionJob.JobFinished = DateTime.UtcNow;
                revitIfcContext.ConversionJobs.Update(conversionJob);

                Log.Error(exception.Message);
                Log.Error(exception.StackTrace);
                throw;
            }
        }

        public static async Task<dynamic> GetModelDerivativeManifest(string urn, string token, string region)
        {
            var regionSpecifier = region == "EU" ? "/regions/eu" : "";
            var url = $"{AppConfig.ForgeBaseUrl}/modelderivative/v2{regionSpecifier}/designdata/{urn}/manifest";

            var response = await url
                .WithOAuthBearerToken(token)
                .GetJsonAsync();

            //Get IFC specific derivative

            return response;
        }

        /* Downloading File */

        public static async Task<string> GetFileParentFolderUrn(string projectId, string itemId, string token)
        {
            try
            {
                var url = $"{AppConfig.ForgeBaseUrl}/data/v1/projects/{projectId}/items/{itemId}/parent";

                var data = await url
                    .WithOAuthBearerToken(token)
                    .GetJsonAsync<dynamic>();

                return data.data.id;
            }
            catch (Exception exception)
            {
                Log.Error("Could not get Parent Folder URN");
                throw new Exception("Could not get Parent Folder URN");
            }
        }

        public static async Task<string> GetIfcDerivativeUrn(string fileUrn, string token, string region)
        {
            try
            {
                var regionSpecifier = region == "EU" ? "/regions/eu" : "";
                var urn = Base64Encoder.Encode(fileUrn).Replace('/', '_');
                var url = $"{AppConfig.ForgeBaseUrl}/modelderivative/v2{regionSpecifier}/designdata/{urn}/manifest";

                var data = await url
                    .WithOAuthBearerToken(token)
                    .GetJsonAsync();

                foreach (dynamic derivative in data.derivatives)
                {
                    if (derivative.outputType == "ifc")
                    {
                        Log.Information("Found Correct one");
                        return derivative.children[0].urn;
                    }
                }

                foreach (dynamic message in data.messages)
                {
                    if (message.code == "Revit-UnsupportedVersionOlder")
                    {
                        string exceptionMessage = "Revit Version Not Supported: ";
                        foreach (dynamic messageMessage in message.message)
                        {
                            exceptionMessage += messageMessage;
                        }
                        throw new Exception(exceptionMessage);
                    }
                }

                throw new Exception();
            }
            catch (Exception exception)
            {
                Log.Error($"Could not get Download URL: {exception.Message}");
                throw new Exception("Could not get Download Url");
            }
        }

        public static async Task<string> PassDownloadToStorageLocation(string derivativeUrn, string fileUrn, string storageLocation, ConversionJob conversionJob, string token)
        {
            try
            {
                var regionSpecifier = conversionJob.Region == "EU" ? "/regions/eu" : "";
                var objectName = storageLocation.Split('/').Last();
                var urlEncodedDerivative = UrlEncoder.Default.Encode(derivativeUrn);
                var base64EncodedFileUrn = !string.IsNullOrWhiteSpace(conversionJob.EncodedInputStorageLocation) ? conversionJob.EncodedInputStorageLocation : conversionJob.EncodedFileUrn;

                var downloadUrl = $"{AppConfig.ForgeBaseUrl}/modelderivative/v2{regionSpecifier}/designdata/{base64EncodedFileUrn}/manifest/{urlEncodedDerivative}";
                var uploadStreamUrl = $"{AppConfig.ForgeBaseUrl}/oss/v2/buckets/wip.dm.prod/objects/{objectName}";


                //Get the length of the object
                using IFlurlResponse headResponse = await downloadUrl
                    .WithOAuthBearerToken(token)
                    .HeadAsync(completionOption: HttpCompletionOption.ResponseHeadersRead);

                int contentLength = (int)int.Parse(headResponse.Headers.FirstOrDefault(x => x.Name == "Content-Length").Value);

                //If less than 20MB, upload normally
                if (contentLength < 20_000_000)
                {
                    var downloadStream = await downloadUrl
                        .WithOAuthBearerToken(token)
                        .GetStreamAsync(completionOption: HttpCompletionOption.ResponseHeadersRead);

                    var content = new StreamContent(downloadStream);

                    var response = await uploadStreamUrl
                        .WithOAuthBearerToken(token)
                        .PutAsync(content);

                    Log.Information("Successful Upload");
                    var data = await response.GetJsonAsync();

                    return data.objectId;
                }
                else
                {
                    var sessionId = Guid.NewGuid().ToString();
                    Log.Information($"File is greater than 20MB. Uploading file in 5MB chunks instead. {sessionId}");
                    string objectId = "";
                    //If greater than 20MB, upload in 5mb chunks
                    for (int i = 0; i < contentLength; i += 5_000_000)
                    {
                        string range = $"bytes={i}-{Math.Min(contentLength - 1, i + 5_000_000 - 1)}";
                        string uploadRange = $"bytes {i}-{Math.Min(contentLength - 1, i + 5_000_000 - 1)}/{contentLength}";

                        using Stream downloadStream = await downloadUrl
                            .WithOAuthBearerToken(token)
                            .WithHeader("Range", range)
                            .GetStreamAsync(completionOption: HttpCompletionOption.ResponseHeadersRead);

                        var content = new StreamContent(downloadStream);

                        var response = await uploadStreamUrl
                            .AppendPathSegment("resumable")
                            .WithOAuthBearerToken(token)
                            .WithHeader("Content-Range", uploadRange)
                            .WithHeader("Session-Id", sessionId)
                            .PutAsync(content);

                        if (response.StatusCode == StatusCodes.Status200OK)
                        {
                            var jsonResponse = response.GetStringAsync().Result;
                            dynamic data = JsonConvert.DeserializeObject<dynamic>(jsonResponse);

                            objectId = data.objectId;
                        }
                    }

                    return objectId;
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                Log.Error(exception.StackTrace);
                throw;
            }
        }

        public static async Task<string> MoveFileFromDmToOss(string sourceStorageLocation, string targetStorageLocation, string token, ConversionJob conversionJob, Context.RevitIfcContext revitIfcContext)
        {
            try
            {
                var downloadUrl = sourceStorageLocation;
                var uploadStreamUrl = targetStorageLocation;


                //Get the length of the object
                var headResponse = await downloadUrl
                    .WithOAuthBearerToken(token)
                    .HeadAsync(completionOption: HttpCompletionOption.ResponseHeadersRead);

                int contentLength = (int)int.Parse(headResponse.Headers.FirstOrDefault(x => x.Name == "Content-Length").Value);
                // int contentLength = (int) headResponse.Content.Headers.ContentLength;

                conversionJob.AddLog($"Moving file from DM to OSS. Content Length: {Math.Round((double)contentLength / 1_000_000.0)}MB");
                revitIfcContext.ConversionJobs.Update(conversionJob);
                await revitIfcContext.SaveChangesAsync();

                //If less than 20MB, upload normally
                if (contentLength < 20_000_000)
                {
                    using Stream downloadStream = await downloadUrl
                        .WithOAuthBearerToken(token)
                        .GetStreamAsync(completionOption: HttpCompletionOption.ResponseHeadersRead);

                    var content = new StreamContent(downloadStream);

                    var response = await uploadStreamUrl
                        .WithOAuthBearerToken(token)
                        .PutAsync(content);

                    var data = await response.GetJsonAsync();

                    Log.Information("File fully uploaded to OSS");
                    conversionJob.AddLog($"File fully uploaded to OSS");
                    revitIfcContext.ConversionJobs.Update(conversionJob);
                    await revitIfcContext.SaveChangesAsync();

                    return data.objectId;
                }
                else
                {
                    var sessionId = Guid.NewGuid().ToString();
                    Log.Information($"File is greater than 20MB, {sessionId}");
                    string objectId = "";
                    //If greater than 20MB, upload in 5mb chunks
                    //Loop through in 5MB chunks until all pieces are uploaded
                    for (int i = 0; i < contentLength; i += 5_000_000)
                    {
                        int lowerRange = i;
                        int upperRange = Math.Min(contentLength - 1, i + 5_000_000 - 1);
                        string range = $"bytes={lowerRange}-{upperRange}";
                        string uploadRange = $"bytes {lowerRange}-{upperRange}/{contentLength}";

                        using Stream downloadStream = await downloadUrl
                            .WithOAuthBearerToken(token)
                            .WithHeader("Range", range)
                            .GetStreamAsync(completionOption: HttpCompletionOption.ResponseHeadersRead);

                        var content = new StreamContent(downloadStream);

                        var response = await uploadStreamUrl
                            .AppendPathSegment("resumable")
                            .WithOAuthBearerToken(token)
                            .WithHeader("Content-Range", uploadRange)
                            .WithHeader("Session-Id", sessionId)
                            .PutAsync(content);

                        if (response.StatusCode == StatusCodes.Status200OK)
                        {
                            dynamic data = await response.GetJsonAsync();

                            objectId = data.objectId;
                        }

                        Log.Information($"Uploaded file chunk to OSS: {Math.Round(100.0 * ((double)upperRange / (double)contentLength))}%");

                        conversionJob.AddLog($"Uploaded file chunk to OSS: {Math.Round(100.0 * ((double)upperRange / (double)contentLength))}%");
                        revitIfcContext.ConversionJobs.Update(conversionJob);
                        await revitIfcContext.SaveChangesAsync();
                    }

                    conversionJob.AddLog($"File fully uploaded to OSS");
                    revitIfcContext.ConversionJobs.Update(conversionJob);
                    await revitIfcContext.SaveChangesAsync();

                    return objectId;
                }
            }
            catch (Exception exception)
            {
                conversionJob.AddLog($"File Upload Failed: {exception.Message}");
                conversionJob.Status = ConversionJobStatus.Failed;
                revitIfcContext.ConversionJobs.Update(conversionJob);
                await revitIfcContext.SaveChangesAsync();

                Log.Error(exception.Message);
                Log.Error(exception.StackTrace);
                throw;
            }
        }

        /* Uploading Code */

        public static async Task<string> CreateStorageLocation(string projectId, string folderId, string derivativeUrn,
            string token)
        {
            var fileName = derivativeUrn.Split('/').Last();

            var url = $"{AppConfig.ForgeBaseUrl}/data/v1/projects/{projectId}/storage";

            var body =
                new
                {
                    jsonapi = new
                    {
                        version = "1.0"
                    },
                    data = new
                    {
                        type = "objects",
                        attributes = new
                        {
                            name = fileName
                        },
                        relationships = new
                        {
                            target = new
                            {
                                data = new
                                {
                                    type = "folders",
                                    id = folderId
                                }
                            }
                        }
                    }
                };

            HttpContent httpContent = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

            var result = await url
                .WithOAuthBearerToken(token)
                .PostAsync(httpContent);

            var data = await result.GetJsonAsync<dynamic>();

            return data.data.id;
        }

        public static async Task<string> GetExistingVersion(string projectId, string folderId, string fileName,
            string suffix, string token)
        {
            try
            {
                var name = fileName.Split('.').First();
                if (string.IsNullOrWhiteSpace(suffix))
                    name = name + ".ifc";
                else
                    name = name + suffix + ".ifc";

                var folderContents = await GetFolderContents(projectId, folderId, token);

                foreach (var file in folderContents)
                {
                    if (file is File && file.Name == name)
                    {
                        //Return existing URN
                        return (file as File).ItemId;
                    }
                }

                return null;

            }
            catch (Exception exception)
            {
                throw;
            }
        }

        public static async Task CreateFirstVersion(string projectId, string folderId, string objectId,
            string fileName, string suffix, string token)
        {
            try
            {
                var name = fileName.Split('.').First();
                if (string.IsNullOrWhiteSpace(suffix))
                    name = name + ".ifc";
                else
                    name = name + suffix + ".ifc";

                var url = $"{AppConfig.ForgeBaseUrl}/data/v1/projects/{projectId}/items";

                var body = new
                {
                    jsonapi = new
                    {
                        version = "1.0"
                    },
                    data = new
                    {
                        type = "items",
                        attributes = new
                        {
                            displayName = name,
                            extension = new
                            {
                                type = "items:autodesk.bim360:File",
                                version = "1.0"
                            }
                        },
                        relationships = new
                        {
                            tip = new
                            {
                                data = new
                                {
                                    type = "versions",
                                    id = "1"
                                }
                            },
                            parent = new
                            {
                                data = new
                                {
                                    type = "folders",
                                    id = folderId
                                }
                            }
                        }
                    },
                    included = new List<dynamic>()
                    {
                        new
                        {
                            type = "versions",
                            id = "1",
                            attributes = new
                            {
                                name = name,
                                extension = new
                                {
                                    type = "versions:autodesk.bim360:File",
                                    version = "1.0"
                                }
                            },
                            relationships = new
                            {
                                storage = new
                                {
                                    data = new
                                    {
                                        type = "objects",
                                        id = objectId
                                    }
                                }
                            }
                        }
                    }
                };

                HttpContent httpContent = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
                var result = await url
                    .WithOAuthBearerToken(token)
                    .PostAsync(httpContent);
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                throw;
            }
        }


        public static async Task CreateSubsequentVersion(string projectId, string itemId, string objectId,
            string fileName, string suffix, string token)
        {
            try
            {
                var name = fileName.Split('.').First();
                if (string.IsNullOrWhiteSpace(suffix))
                    name = name + ".ifc";
                else
                    name = name + suffix + ".ifc";

                var url = $"{AppConfig.ForgeBaseUrl}/data/v1/projects/{projectId}/versions";

                var body = new
                {
                    jsonapi = new
                    {
                        version = "1.0"
                    },
                    data = new
                    {
                        type = "versions",
                        attributes = new
                        {
                            name = name,
                            extension = new
                            {
                                type = "versions:autodesk.bim360:File",
                                version = "1.0"
                            }
                        },
                        relationships = new
                        {
                            item = new
                            {
                                data = new
                                {
                                    type = "items",
                                    id = itemId
                                }
                            },
                            storage = new
                            {
                                data = new
                                {
                                    type = "objects",
                                    id = objectId
                                }
                            }
                        }
                    }
                };

                HttpContent httpContent = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
                var result = await url
                    .WithOAuthBearerToken(token)
                    .PostAsync(httpContent);

                Log.Information("Success");
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                throw;
            }

        }
    }

    static class ListExtension
    {
        public static T PopAt<T>(this List<T> list, int index)
        {
            T r = list[index];
            list.RemoveAt(index);
            return r;
        }
    }
}