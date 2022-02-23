using System.Collections.Generic;
using RevitToIfcScheduler.Context;
using RevitToIfcScheduler.Models;
using Microsoft.AspNetCore.Http;

namespace RevitToIfcScheduler.Utilities
{
    public static class Authentication
    {
        public static bool IsAuthorized(HttpContext httpContext, Context.RevitIfcContext revitIfcContext)
        {
            try
            {
                var user = RevitToIfcScheduler.Models.User.FetchByContext(httpContext, revitIfcContext);
                return user != null;
            }
            catch 
            {
                return false;
            }
        }
        public static bool IsAuthorized(HttpContext httpContext, Context.RevitIfcContext revitIfcContext, List<AccountRole> accountRoles, string projectId)
        {
            try
            {
                var user = RevitToIfcScheduler.Models.User.FetchByContext(httpContext, revitIfcContext);
                foreach (var accountRole in accountRoles)
                {
                    if (accountRole == AccountRole.ApplicationAdmin && user.HasPermission(accountRole))
                    {
                        return true;
                    }

                    if (user.HasPermission(accountRole, projectId))
                    {
                        return true;
                    }
                }
                
                return false;
            }
            catch 
            {
                return false;
            }
        }
        public static bool IsAuthorized(HttpContext httpContext, Context.RevitIfcContext revitIfcContext, List<AccountRole> accountRoles)
        {
            try
            {
                var user = RevitToIfcScheduler.Models.User.FetchByContext(httpContext, revitIfcContext);
                foreach (var accountRole in accountRoles)
                {
                    if (user.HasPermission(accountRole))
                    {
                        return true;
                    }
                }
                
                return false;
            }
            catch 
            {
                return false;
            }
        }

        public static bool IsAuthorized(HttpContext httpsContext, string projectId)
        {
            return true;
        }
    }
}