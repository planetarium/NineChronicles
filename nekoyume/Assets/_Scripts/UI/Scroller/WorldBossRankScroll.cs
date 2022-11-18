using System;
using UnityEngine.UI.Extensions;
using Nekoyume.UI.Model;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class WorldBossRankScroll : RectScroll<WorldBossRankItem, WorldBossRankScroll.ContextModel>
    {
        public class ContextModel : RectScrollDefaultContext
        {
            public readonly Subject<WorldBossRankItem> OnClick = new();

            public override void Dispose()
            {
                OnClick?.Dispose();
                base.Dispose();
            }
        }

        [SerializeField]
        private WorldBossRankCell cellTemplate = null;

        public IObservable<WorldBossRankItem> OnClick => Context.OnClick;
    }
}
