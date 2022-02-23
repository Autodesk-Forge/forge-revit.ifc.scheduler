using System;
using System.Threading.Tasks;
using RevitToIfcScheduler.Context;
using RevitToIfcScheduler.Models;
using Hangfire;
using Newtonsoft.Json;
using Serilog;

namespace RevitToIfcScheduler.Utilities
{
    public class HangfireJobs
    {
        private readonly Context.RevitIfcContext _revitIfcContext;
        public HangfireJobs(Context.RevitIfcContext revitIfcContext)
        {
            _revitIfcContext = revitIfcContext;
        }

        public async Task PollConversionJob(Guid conversionJobId)
        {
            try
            {

                var conversionJob = await _revitIfcContext.ConversionJobs.FindAsync(conversionJobId);

                var token = await new TwoLeggedTokenGetter().GetToken();
                var manifest = await Forge.GetModelDerivativeManifest(conversionJob.EncodedStorageUrn, token, conversionJob.Region);

                if (manifest == null)
                {
                    conversionJob.AddLog("IFC Derivative not available");
                }
                
                switch (manifest.status)
                {
                    case "pending":
                        conversionJob.AddLog($"Processing Model Derivative: {manifest.progress}");
                        BackgroundJob.Schedule<HangfireJobs>(x => x.PollConversionJob(conversionJob.Id),TimeSpan.FromMinutes(1));
                        break;
                    case "inprogress":
                        conversionJob.AddLog($"Processing Model Derivative: {manifest.progress}");
                        BackgroundJob.Schedule<HangfireJobs>(x => x.PollConversionJob(conversionJob.Id),TimeSpan.FromMinutes(1));
                        break;
                    case "processing":
                        conversionJob.AddLog($"Processing Model Derivative: {manifest.progress}");
                        BackgroundJob.Schedule<HangfireJobs>(x => x.PollConversionJob(conversionJob.Id),TimeSpan.FromMinutes(1));
                        break;
                    case "success":
                        //Get Derivative URN
                        
                        foreach (dynamic derivative in manifest.derivatives)
                        {
                            if (derivative.outputType == "ifc")
                            {
                                conversionJob.DerivativeUrn = derivative.children[0].urn;
                                conversionJob.AddLog($"Added Derivative URN from Manifest: {conversionJob.DerivativeUrn}");
                            }
                        }

                        if (string.IsNullOrWhiteSpace(conversionJob.DerivativeUrn))
                        {
                            conversionJob.AddLog("IFC derivative Not Generated");
                            conversionJob.Status = ConversionJobStatus.Failed;
                            _revitIfcContext.ConversionJobs.Update(conversionJob);
                            await _revitIfcContext.SaveChangesAsync();
                            return;
                        }

                        _revitIfcContext.ConversionJobs.Update(conversionJob);
                        await _revitIfcContext.SaveChangesAsync();
                        
                        //Create OnReceive
                        await ConversionJob.OnReceive(conversionJob);
                        break;
                    case "failed":
                        conversionJob.Status = ConversionJobStatus.Failed;
                        conversionJob.AddLog("Conversion Failed");
                        break;
                    case "timeout":
                        conversionJob.Status = ConversionJobStatus.TimeOut;
                        conversionJob.AddLog("Conversion Timed Out");
                        break;
                    default: 
                        conversionJob.AddLog(JsonConvert.SerializeObject(manifest));
                        break;
                }

                _revitIfcContext.ConversionJobs.Update(conversionJob);
                await _revitIfcContext.SaveChangesAsync();

            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                Log.Error(exception.StackTrace);
                throw;
            }
        }
    }
}