using System.Threading.Tasks;

namespace RevitToIfcScheduler.Utilities
{
    public abstract class TokenGetter
    {
        public abstract Task<string> GetToken();
    }
}