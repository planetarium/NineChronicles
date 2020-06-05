using System.Threading;

namespace NineChronicles.Standalone
{
    public static class NodeCancellationContext
    {
        public static CancellationToken CancellationToken = new CancellationTokenSource().Token;
    }
}
