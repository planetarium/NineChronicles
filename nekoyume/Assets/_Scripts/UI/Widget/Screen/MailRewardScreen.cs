using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Nekoyume.Model.Item;
using Nekoyume.UI.Module;
using Unity.Mathematics;
using UnityEngine;

namespace Nekoyume.UI
{
    using UniRx;

    public class MailRewardScreen : ScreenWidget
    {
        private const int MaxItemCount = 40;

        [SerializeField]
        private GameObject rewardsObject;

        [SerializeField]
        private RectTransform content;

        [SerializeField]
        private GameObject pressToContinue;

        [SerializeField]
        private float moveDelay;

        [SerializeField]
        private float moveDuration;

        [SerializeField]
        private AnimationCurve moveGraph;

        [SerializeField]
        private float fadeDelay;

        [SerializeField]
        private float fadeDuration;

        [SerializeField]
        private AnimationCurve fadeGraph;

        private int _count;
        private readonly List<MailRewards> _mailRewardsList = new();
        private readonly ReactiveProperty<bool> _isDone = new();
        private readonly List<IDisposable> _disposables = new();

        protected override void Awake()
        {
            base.Awake();
            CloseWidget = () =>
            {
                if (!_isDone.Value)
                {
                    return;
                }

                Close(true);
            };

            var background = GetComponentInChildren<UIBackground>();
            if (background != null)
            {
                background.OnClick = CloseWidget;
            }

            _isDone.Subscribe(b => pressToContinue.SetActive(b)).AddTo(_disposables);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _disposables.DisposeAllAndClear();
        }

        public void Show(List<MailReward> mailRewards, bool ignoreShowAnimation = false)
        {
            foreach (var rewards in _mailRewardsList)
            {
                DestroyImmediate(rewards.gameObject);
            }

            _mailRewardsList.Clear();

            var sortedRewards = mailRewards
                .OrderBy(x => x.ItemBase.ItemType == ItemType.Consumable)
                .ThenBy(x => x.ItemBase.ItemType == ItemType.Equipment)
                .ThenBy(x => x.ItemBase.ItemType == ItemType.Material)
                .ThenBy(x => x.IsPurchased)
                .ToList();

            _count = (sortedRewards.Count - 1) / MaxItemCount;
            for (var i = 0; i < _count + 1; i++)
            {
                var clone = Instantiate(rewardsObject, content);
                var item = clone.GetComponent<MailRewards>();
                _mailRewardsList.Add(item);
                var rewards = new List<MailReward>();
                var min = i * MaxItemCount;
                var max = math.min(sortedRewards.Count, (i + 1) * MaxItemCount);
                for (var j = min; j < max; j++)
                {
                    rewards.Add(sortedRewards[j]);
                }

                item.Set(rewards);
            }

            base.Show(ignoreShowAnimation);
            _isDone.SetValueAndForceNotify(false);
            StartCoroutine(PlayAnimation());
        }

        private IEnumerator PlayAnimation()
        {
            yield return null;
            var movement = content.sizeDelta.x / (_count + 1);
            content.anchoredPosition = new Vector2(movement * 0.5f * _count, content.anchoredPosition.y);
            yield return new WaitForSeconds(1);
            MoveContent(movement);
        }

        private void MoveContent(float movement)
        {
            var index = _mailRewardsList.Count - _count - 1;
            var rewards = _mailRewardsList[index];
            rewards.ShowEffect();

            if (_count <= 0)
            {
                _isDone.SetValueAndForceNotify(true);
                return;
            }

            rewards.FadeOut(fadeDuration, fadeDelay, fadeGraph);
            _count--;
            content.DOLocalMoveX(-movement, moveDuration)
                .SetDelay(moveDelay)
                .SetEase(moveGraph).SetRelative()
                .OnComplete(() => MoveContent(movement));
        }
    }
}
