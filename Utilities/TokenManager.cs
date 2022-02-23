using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Forge;
using Autodesk.Forge.Client;
using RevitToIfcScheduler.Context;
using RevitToIfcScheduler.Models;
using Flurl;
using Microsoft.AspNetCore.Http;

namespace RevitToIfcScheduler.Utilities
{
    public static class TokenManager
    {
        private static string TwoLeggedToken { get; set; }
        private static DateTime Expiration { get; set; }
        private static TwoLeggedApi _twoLeggedApi = new TwoLeggedApi();
        private static ThreeLeggedApi _threeLeggedApi = new ThreeLeggedApi();

        public static async Task<string> GetTwoLeggedToken()
        {
            if (TwoLeggedToken != null && Expiration > DateTime.UtcNow)
            {
                return TwoLeggedToken;
            }
            else
            {
                ApiResponse<dynamic> bearer =_twoLeggedApi.AuthenticateWithHttpInfo (AppConfig.ClientId, 
                    AppConfig.ClientSecret, oAuthConstants.CLIENT_CREDENTIALS, 
                    ScopeStringToArray(AppConfig.TwoLegScope)) ;
                if ( bearer.StatusCode != 200 )
                    throw new Exception ("Request failed! (with HTTP response " + bearer.StatusCode + ")") ;

                TwoLeggedToken = bearer.Data.access_token;
                Expiration = DateTime.UtcNow.AddSeconds(bearer.Data.expires_in);

                return TwoLeggedToken;
            }
        }

        public static async Task<ThreeLeggedToken> GetThreeLeggedTokenFromCode(string code, HttpContext httpContext)
        {
            try
            {
                var redirectUrl = GetRedirectUrl(httpContext);
                var token = _threeLeggedApi.Gettoken(AppConfig.ClientId, AppConfig.ClientSecret,
                    oAuthConstants.AUTHORIZATION_CODE,
                    code, redirectUrl);

                
                return new ThreeLeggedToken()
                {
                    AccessToken = token.access_token,
                    RefreshToken = token.refresh_token,
                    ExpiresIn = (int)token.expires_in,
                    TokenType = token.token_type
                };
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        public static async Task RefreshThreeLeggedToken(User user, Context.RevitIfcContext revitIfcContext)
        {
            try
            {
                var token = _threeLeggedApi.Refreshtoken(AppConfig.ClientId, AppConfig.ClientSecret,
                    oAuthConstants.AUTHORIZATION_CODE,
                    user.Refresh, ScopeStringToArray(AppConfig.ThreeLegScope));

                var threeLeggedToken = new ThreeLeggedToken()
                {
                    AccessToken = token.access_token,
                    RefreshToken = token.refresh_token,
                    ExpiresIn = (int)token.expires_in,
                    TokenType = token.token_type
                };

                user.Token = threeLeggedToken.AccessToken;
                user.Refresh = threeLeggedToken.RefreshToken;
                user.TokenExpiration = DateTime.UtcNow.AddSeconds(threeLeggedToken.ExpiresIn - 300);
                
                revitIfcContext.Users.Update(user);
                await revitIfcContext.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        public static string GetRedirectUrl(HttpContext httpContext)
        {
            return ((httpContext.Request.IsHttps ? "https://" : "http://") + httpContext.Request.Host.ToUriComponent())
                .AppendPathSegments("api", "forge", "oauth", "callback");
        }

        public static Scope[] ScopeStringToArray(string scopeString)
        {
            var scopeStrings = scopeString.Split(' ');
            var scopes = new List<Scope>();

            if (scopeStrings.Contains("data:read")) scopes.Add(Scope.DataRead);
            if (scopeStrings.Contains("data:write")) scopes.Add(Scope.DataWrite);
            if (scopeStrings.Contains("data:create")) scopes.Add(Scope.DataCreate);
            if (scopeStrings.Contains("data:search")) scopes.Add(Scope.DataSearch);
            
            if (scopeStrings.Contains("account:read")) scopes.Add(Scope.AccountRead);
            if (scopeStrings.Contains("account:write")) scopes.Add(Scope.AccountWrite);
            
            if (scopeStrings.Contains("bucket:read")) scopes.Add(Scope.BucketRead);
            if (scopeStrings.Contains("bucket:create")) scopes.Add(Scope.BucketCreate);
            if (scopeStrings.Contains("bucket:update")) scopes.Add(Scope.BucketUpdate);
            if (scopeStrings.Contains("bucket:delete")) scopes.Add(Scope.BucketDelete);
            
            if (scopeStrings.Contains("user:profileRead")) scopes.Add(Scope.UserProfileRead);
            
            return scopes.ToArray();
        }
    }
}