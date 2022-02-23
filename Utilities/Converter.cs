using System.Collections.Generic;
using System.Threading.Tasks;
using RevitToIfcScheduler.Context;
using RevitToIfcScheduler.Models;

namespace RevitToIfcScheduler.Utilities
{
    public static class Converter
    {
        public static async Task ProcessConversionBatch(User user, Context.RevitIfcContext revitIfcContext, string projectId, ConversionBatch conversionBatch, Schedule schedule)
        {
            
            var finalFiles = new List<File>();

            if (user.HasPermission(AccountRole.AccountAdmin, projectId))
            {
                finalFiles = await Forge.GetAllChildRevitFiles(projectId, conversionBatch.FolderUrns,
                    new TwoLeggedTokenGetter());
            } else if (user.HasPermission(AccountRole.ProjectAdmin, projectId))
            {
                finalFiles = await Forge.GetAllChildRevitFiles(projectId, conversionBatch.FolderUrns,
                    new ThreeLeggedTokenGetter(user, revitIfcContext));
            }

            foreach (var file in conversionBatch.Files)
            {
                if (finalFiles.Find(x => x.ItemId == file.ItemId) == null)
                {
                    finalFiles.Add(file);
                }
            }
            
            var account = user.GetAccountFromProjectId(projectId);
            await CreateConversionJobs(account.HubId, account.Region, projectId, finalFiles, conversionBatch.ifcSettingsName, user.Email, schedule);
        }

        public static async Task CreateConversionJobs(string hubId, string region, string projectId,
            List<File> files, string exportSettingName, string createdBy, Schedule schedule)
        {
            foreach (File file in files)
            {
                if (schedule != null)
                {
                    await Forge.CreateIfcConversionJob(hubId, region, projectId, file.Id, file.ItemId, file.Name, file.FolderId, exportSettingName, createdBy, file.IsCompositeDesign, schedule.Id);
                }
                else
                {
                    await Forge.CreateIfcConversionJob(hubId, region, projectId, file.Id, file.ItemId, file.Name, file.FolderId, exportSettingName, createdBy, file.IsCompositeDesign, null);
                }
            }
        }
    }
}