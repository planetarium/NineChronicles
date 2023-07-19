using Libplanet.Blockchain.Renderers;
using Libplanet.Types.Blocks;
#if UNITY_EDITOR || UNITY_STANDALONE
using UniRx;
#else
using System.Reactive.Subjects;
using System.Reactive.Linq;
#endif

namespace Lib9c.Renderers
{
    using NCBlock = Block;

    public class BlockRenderer : IRenderer
    {
        public readonly Subject<(NCBlock OldTip, NCBlock NewTip)> BlockSubject =
            new Subject<(NCBlock OldTip, NCBlock NewTip)>();

        public void RenderBlock(NCBlock oldTip, NCBlock newTip) =>
            BlockSubject.OnNext((oldTip, newTip));
    }
}
