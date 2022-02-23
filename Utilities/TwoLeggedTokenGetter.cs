using System;
using System.Threading.Tasks;

namespace RevitToIfcScheduler.Utilities
{
    public class TwoLeggedTokenGetter: TokenGetter
    {
        
        public TwoLeggedTokenGetter()
        {
        }
        
        
        public override async Task<string> GetToken()
        {
               return await TokenManager.GetTwoLeggedToken();
        }
    }
}