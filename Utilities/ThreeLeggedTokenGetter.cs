using System;
using System.Threading.Tasks;
using RevitToIfcScheduler.Context;
using RevitToIfcScheduler.Models;

namespace RevitToIfcScheduler.Utilities
{
    public class ThreeLeggedTokenGetter: TokenGetter
    {
        private User User;
        private Context.RevitIfcContext _revitIfcContext;
        
        public ThreeLeggedTokenGetter(User user, Context.RevitIfcContext revitIfcContext)
        {
            User = user;
            _revitIfcContext = revitIfcContext;
        }
        
        
        public override async Task<string> GetToken()
        {
            if (User.TokenExpiration <= DateTime.UtcNow)
            {
                await TokenManager.RefreshThreeLeggedToken(User, _revitIfcContext);
            }

            return User.Token;
        }
    }
}