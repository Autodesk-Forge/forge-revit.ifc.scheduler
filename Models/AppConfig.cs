using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace RevitToIfcScheduler.Models
{
    public static class AppConfig
    {
        public static string ClientId { get; set; }
        public static string ClientSecret { get; set; }
        public static string LogPath { get; set; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static List<string> AdminEmails { get; set; }
        public static string FilePath { get; set; }
        public static string AppId { get; set; }
        public static string SendGridApiKey { get; set; }
        public static string FromEmail { get; set; }
        public static string ToEmail { get; set; }
        public static string TwoLegScope { get; set; }
        public static string ThreeLegScope { get; set; }
        public static IDataProtector DataProtector { get; set; }
        public static string SqlDB { get; set; }
        public static IServiceCollection Services { get; set; }
        public static string BucketKey { get; set; }
        public static bool IncludeShallowCopies { get; set; }
        public static string ForgeBaseUrl { get; set; }
    }
}