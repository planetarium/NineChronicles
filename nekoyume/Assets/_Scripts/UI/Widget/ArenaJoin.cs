using System.Collections.Generic;
using Nekoyume.UI.Module.Arena;
using UnityEngine;

namespace Nekoyume.UI
{
    public class ArenaJoin : Widget
    {
#if UNITY_EDITOR
        [SerializeField]
        private bool _useSo;

        [SerializeField]
        private ArenaJoinSO _so;
#endif

        [SerializeField]
        private ArenaJoinSeasonScroll _scroll;

        [SerializeField]
        private ArenaJoinSeasonInfo _info;

        [SerializeField]
        private ArenaJoinBottomButtons _buttons;

        public override void Show(bool ignoreShowAnimation = false)
        {
            var scrollData = GetScrollData();
            _scroll.SetData(scrollData, 0);
            base.Show(ignoreShowAnimation);
        }

        private IList<ArenaJoinSeasonItemData> GetScrollData()
        {
            IList<ArenaJoinSeasonItemData> scrollData = null;
#if UNITY_EDITOR
            scrollData = _so
                ? _so.ScrollData
                : new List<ArenaJoinSeasonItemData>();
#else
            scrollData = new List<ArenaJoinSeasonItemData>();
#endif

            return scrollData;
        }
    }
}
