using System;
using DG.Tweening;
using FancyScrollView;
using UnityEngine;
using UnityEngine.UI;
using QuestModel = Nekoyume.Model.Quest.Quest;

namespace Nekoyume.UI.Scroller
{
    public class QuestScroll : BaseScroll<QuestModel, QuestScroll.ContextModel>
    {
        public class ContextModel : IFancyScrollRectContext
        {
            public ScrollDirection ScrollDirection { get; set; }
            public Func<(float ScrollSize, float ReuseMargin)> CalculateScrollSize { get; set; }
        }

        [SerializeField]
        private GameObject animationCellContainer = null;

        private QuestCell[] animationCells = null;

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
                ((RectTransform) animationCells[i - cell.Index].transform).anchoredPosition =
                    ((RectTransform) cell.transform).anchoredPosition -
                    Vector2.up * (CellSize * i - cell.Index);

                animationCells[i - cell.Index].gameObject.SetActive(true);
                animationCells[i - cell.Index].UpdateContent(ItemsSource[i]);
            }

            for (var i = endValue - cell.Index; i < animationCells.Length; i++)
            {
                animationCells[i].gameObject.SetActive(false);
            }

            cellContainer.gameObject.SetActive(false);

            Debug.Log(index);
            animationCells[index].ShowAsComplete();
            var t = animationCells[index].transform.DOScale(new Vector3(1, 0, 1), 0.3f)
                .From(new Vector3(1, 1, 1));
            t.onComplete = animationCells[index].UpdateTab;

            for (var i = index + 1; i < animationCells.Length; i++)
            {
                ((RectTransform) animationCells[i].transform).DoAnchoredMoveY(CellSize, 0.3f, true);
            }
        }

        public void DoneAnimation()
        {
            animationCellContainer.SetActive(false);
            cellContainer.gameObject.SetActive(true);
        }
    }
}
