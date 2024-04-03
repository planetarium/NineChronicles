using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using QuestModel = Nekoyume.Model.Quest.Quest;

namespace Nekoyume.UI.Scroller
{
    public class QuestScroll : RectScroll<QuestModel, QuestScroll.ContextModel>
    {
        public class ContextModel : RectScrollDefaultContext
        {
        }

        [SerializeField]
        private GameObject animationCellContainer = null;

        private QuestCell[] animationCells = null;

        public readonly Queue<QuestModel> CompletedQuestQueue = new Queue<QuestModel>();

        public void DisappearAnimation(int index)
        {
            var completedQuest = CompletedQuestQueue.Peek();

            if (!ItemsSource[index].Equals(completedQuest))
            {
                OnCompleteQuest(false, index);
                return;
            }

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

            animationCells[index].ShowAsComplete();
            var t = animationCells[index].transform.DOScale(new Vector3(1, 0, 1), 0.3f)
                .From(new Vector3(1, 1, 1));
            t.onComplete = () => OnCompleteQuest(true, index);

            for (var i = index + 1; i < animationCells.Length; i++)
            {
                ((RectTransform) animationCells[i].transform).DoAnchoredMoveY(CellSize, 0.3f, true);
            }
        }

        public void EnqueueCompletedQuest(QuestModel questModel)
        {
            if (questModel is null)
            {
                NcDebug.LogError("Quest is null.");
                return;
            }

            if (!CompletedQuestQueue.Contains(questModel))
            {
                CompletedQuestQueue.Enqueue(questModel);
            }
        }

        public void OnCompleteQuest(bool update, int index)
        {
            if (CompletedQuestQueue.Count <= 0)
                return;

            var completedQuest = CompletedQuestQueue.Dequeue();
            Widget.Find<CelebratesPopup>().Show(completedQuest);

            if (update)
                animationCells[index].UpdateTab();
        }

        public void DoneAnimation()
        {
            animationCellContainer.SetActive(false);
            cellContainer.gameObject.SetActive(true);
        }
    }
}
