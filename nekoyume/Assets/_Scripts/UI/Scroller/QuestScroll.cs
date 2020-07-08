using System;
using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using FancyScrollView;
using UnityEngine;
using UnityEngine.UI;
using QuestModel = Nekoyume.Model.Quest.Quest;

namespace Nekoyume.UI.Scroller
{
    public class QuestScroll : FancyScrollRect<QuestModel, QuestScroll.ContextModel>
    {
        public class ContextModel : IFancyScrollRectContext
        {
            public ScrollDirection ScrollDirection { get; set; }
            public Func<(float ScrollSize, float ReuseMargin)> CalculateScrollSize { get; set; }
        }

        [SerializeField]
        private GameObject cellPrefab = null;

        [SerializeField]
        private float cellSize = default;

        /// <summary>
        /// `FancyScrollRect`는 `Scroller.ScrollSensitivity`를 강제로 바꿔버립니다.
        /// 그렇기 때문에 사용자가 설정한 스크롤 감도가 무시됩니다.
        /// 이를 강제로 할당하기 윈해서 각 스크롤에서 원하는 감도를 받습니다.
        /// </summary>
        [SerializeField]
        private float forcedScrollSensitivity = 20f;

        [SerializeField]
        private GameObject animationCellContainer = null;

        private QuestCell[] animationCells = null;

        protected override GameObject CellPrefab => cellPrefab;

        protected override float CellSize => cellSize;

        public void UpdateData(IList<QuestModel> items, bool jumpToFirst = true)
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

        protected override void UpdateContents(IList<QuestModel> items)
        {
            base.UpdateContents(items);
            Scroller.ScrollSensitivity = forcedScrollSensitivity;
        }

        public void DisappearAnimation(int index)
        {
            animationCellContainer.SetActive(true);
            if (animationCells == null)
            {
                animationCells = new QuestCell[cellContainer.childCount];
                for (var i = 0; i < cellContainer.childCount; i++)
                {
                    animationCells[i] = Instantiate(cellPrefab, animationCellContainer.transform).GetComponent<QuestCell>();
                }
            }

            var cell = cellContainer.GetChild(0).GetComponent<QuestCell>();
            var endValue = Mathf.Clamp(cell.Index + animationCells.Length, 0, ItemsSource.Count);

            for (var i = cell.Index; i < endValue; i++)
            {
                animationCells[i - cell.Index].transform.localScale = Vector3.one;
                animationCells[i - cell.Index].transform.position =
                    cell.transform.position - Vector3.up * (CellSize * i - cell.Index);
                animationCells[i - cell.Index].gameObject.SetActive(true);

                animationCells[i - cell.Index].UpdateContent(ItemsSource[i]);
            }

            for (var i = endValue - cell.Index; i < animationCells.Length; i++)
            {
                animationCells[i].gameObject.SetActive(false);
            }

            cellContainer.gameObject.SetActive(false);

            var target = animationCells[index].transform;

            var t = DOTween.To(() => target.localScale, x =>
                {
                    target.localScale = x;
                    LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)animationCellContainer.transform);
                }, new Vector3(1, 0, 1), 0.3f);
            t.From(new Vector3(1, 1, 1)).onComplete += animationCells[index].UpdateTab;
            t.SetTarget(target);
        }

        public void DoneAnimation()
        {
            animationCellContainer.SetActive(false);
            cellContainer.gameObject.SetActive(true);
        }
    }
}
