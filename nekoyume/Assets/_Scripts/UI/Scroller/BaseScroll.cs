using System.Collections.Generic;
using FancyScrollView;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public abstract class BaseScroll<TItemData, TContext> : FancyScrollRect<TItemData, TContext>
        where TContext : class, IFancyScrollRectContext, new()
    {
        [SerializeField]
        protected GameObject cellPrefab = null;

        [SerializeField]
        private float cellSize = default;

        /// <summary>
        /// `FancyScrollRect`는 `Scroller.ScrollSensitivity`를 강제로 바꿔버립니다.
        /// 그렇기 때문에 사용자가 설정한 스크롤 감도가 무시됩니다.
        /// 이를 강제로 할당하기 윈해서 각 스크롤에서 원하는 감도를 받습니다.
        /// </summary>
        [SerializeField]
        private float forcedScrollSensitivity = 20f;

        protected override GameObject CellPrefab => cellPrefab;

        protected override float CellSize => cellSize;

        public void UpdateData(IList<TItemData> items, bool jumpToFirst = true)
        {
            if (!initialized)
            {
                Initialize();
                initialized = true;
            }

            UpdateContents(items);

            if (!jumpToFirst)
            {
                return;
            }

            JumpTo(0);
        }

        protected override void Refresh()
        {
            base.Refresh();
            Scroller.ScrollSensitivity = forcedScrollSensitivity;
        }

        protected override void Relayout()
        {
            base.Relayout();
            Scroller.ScrollSensitivity = forcedScrollSensitivity;
        }

        protected override void UpdateContents(IList<TItemData> items)
        {
            base.UpdateContents(items);
            Scroller.ScrollSensitivity = forcedScrollSensitivity;
        }
    }
}
