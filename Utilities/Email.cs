using System.Collections.Generic;
using System.Threading.Tasks;
using RevitToIfcScheduler.Models;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace RevitToIfcScheduler.Utilities
{
    public class Email
    {
        public static async Task SendConfirmation(ConversionJob conversionJob)
        {
            if (AppConfig.SendGridApiKey != null && AppConfig.FromEmail != null && AppConfig.ToEmail != null)
            {
                //TODO: Get Webview URL
                
                foreach (var toEmail in AppConfig.ToEmail.Split(','))
                {
                    var sendGridClient = new SendGridClient(AppConfig.SendGridApiKey);
                    var from = new EmailAddress(AppConfig.FromEmail);
                    var to = new EmailAddress(toEmail.Trim());
                    var subject = "Revit to IFC Conversion Completed";
                    var plainContent = $"Revit File Converted to IFC: {conversionJob.FileName}";
                    var htmlContent = $"<h1>Revit File Converted to IFC:</h1> " +
                                      $"<p>{conversionJob.FileName}</p>" +
                                      $"<a href='https://docs.b360.autodesk.com/projects/{conversionJob.ProjectId.Substring(2)}/folders/{conversionJob.FolderId}/detail'>Open Project Folder in BIM 360</a>";

                    var mailMessage = MailHelper.CreateSingleEmailToMultipleRecipients(from, new List<EmailAddress>() {to},
                        subject, plainContent, htmlContent);

                    await sendGridClient.SendEmailAsync(mailMessage);
                }
            }
        }
    }
}