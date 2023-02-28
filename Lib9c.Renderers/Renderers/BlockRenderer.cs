using System;
using Libplanet.Action;
using Libplanet.Blockchain.Renderers;
using Libplanet.Blocks;
using Nekoyume.Action;
#if UNITY_EDITOR || UNITY_STANDALONE
using UniRx;
#else
using System.Reactive.Subjects;
using System.Reactive.Linq;
#endif

namespace Lib9c.Renderers
{
    using NCAction = PolymorphicAction<ActionBase>;
    using NCBlock = Block<PolymorphicAction<ActionBase>>;

    public class BlockRenderer : IRenderer<NCAction>
    {
        public readonly Subject<(NCBlock OldTip, NCBlock NewTip)> BlockSubject =
            new Subject<(NCBlock OldTip, NCBlock NewTip)>();

        public readonly Subject<(NCBlock OldTip, NCBlock NewTip, NCBlock Branchpoint)> ReorgSubject =
            new Subject<(NCBlock OldTip, NCBlock NewTip, NCBlock Branchpoint)>();

        public readonly Subject<(NCBlock OldTip, NCBlock NewTip, NCBlock Branchpoint)> ReorgEndSubject =
            new Subject<(NCBlock OldTip, NCBlock NewTip, NCBlock Branchpoint)>();

        public void RenderBlock(NCBlock oldTip, NCBlock newTip) =>
            BlockSubject.OnNext((oldTip, newTip));

        public void RenderReorg(NCBlock oldTip, NCBlock newTip, NCBlock branchpoint) =>
            ReorgSubject.OnNext((oldTip, newTip, branchpoint));

        public void RenderReorgEnd(NCBlock oldTip, NCBlock newTip, NCBlock branchpoint) =>
            ReorgEndSubject.OnNext((oldTip, newTip, branchpoint));
    }
}
