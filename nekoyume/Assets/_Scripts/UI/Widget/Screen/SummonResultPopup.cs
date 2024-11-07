using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Types.Assets;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.TableData.Summon;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Nekoyume.UI
{
    public class SummonResultPopup : PopupWidget
    {
        [Serializable]
        private class ResultVideoClip
        {
            public VideoClip summoning;
            public VideoClip result;
            public VideoClip great;
        }

        [SerializeField] private Button closeButton;
        [SerializeField] private Animator animator;
        [SerializeField] private SummonCostButton normalDrawButton;
        [SerializeField] private SummonCostButton goldenDrawButton;

        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private Button skipButton;

        [SerializeField] private ResultVideoClip normalVideoClip;
        [SerializeField] private ResultVideoClip goldenVideoClip;
        [SerializeField] private ResultVideoClip rubyVideoClip;
        [SerializeField] private ResultVideoClip emeraldVideoClip;

        [SerializeField] private SummonItemView[] manySummonItemViews;
        [SerializeField] private SummonItemView[] summonItemViews;
        [SerializeField] private SummonItemView singleSummonItemView;
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform scrollView;
        [SerializeField] private GameObject summonItemViewParentObject;
        [SerializeField] private GameObject manySummonItemViewParentObject;

        private bool _isGreat;
        private Coroutine _coroutine;
        private string _previousMusicName;
        private System.Action _completeCallback;

        private readonly List<IDisposable> _disposables = new();

        private static readonly WaitForSeconds ItemViewAnimInterval = new(0.1f);
        private static readonly WaitForSeconds DefaultAnimInterval = new(1f);

        private static readonly int AnimatorHashHide = Animator.StringToHash("Hide");
        private static readonly int AnimatorHashShow = Animator.StringToHash("Show");
        private static readonly int AnimatorHashShowButton = Animator.StringToHash("ShowButton");

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() => Close(true));
            CloseWidget = closeButton.onClick.Invoke;
            skipButton.onClick.AddListener(() =>
            {
                if (_coroutine != null)
                {
                    StopCoroutine(_coroutine);
                    _coroutine = null;
                }

                videoPlayer.Stop();
                videoPlayer.gameObject.SetActive(false);
                skipButton.gameObject.SetActive(false);
                StartCoroutine(PlayResultAnimation(_isGreat));
            });

            normalDrawButton.Subscribe(gameObject);
            goldenDrawButton.Subscribe(gameObject);
        }

        public void Show<T>(
            SummonSheet.Row summonRow,
            int summonCount,
            List<T> resultList,
            System.Action completeCallback = null,
            bool ignoreShowAnimation = false) where T : ItemBase
        {
            base.Show(ignoreShowAnimation);
            _completeCallback = completeCallback;

            animator.SetTrigger(AnimatorHashHide);

            var bonus = summonCount == 10 ? 1 : 0;
            var great = resultList.First().Grade >= 5;
            summonItemViewParentObject.SetActive(false);
            manySummonItemViewParentObject.SetActive(false);
            singleSummonItemView.Hide();

            if (summonCount == 1)
            {
                singleSummonItemView.SetData(resultList.First(), true, true);
                singleSummonItemView.Show();
            }
            else if (summonCount == 10)
            {
                summonItemViewParentObject.SetActive(true);
                for (var i = 0; i < summonItemViews.Length; i++)
                {
                    var view = summonItemViews[i];
                    if (i < resultList.Count)
                    {
                        view.SetData(resultList[i], true);
                        view.Show();
                    }
                    else
                    {
                        view.Hide();
                    }
                }
            }
            else
            {
                manySummonItemViewParentObject.SetActive(true);
                for (var i = 0; i < manySummonItemViews.Length; i++)
                {
                    var view = manySummonItemViews[i];
                    if (i < resultList.Count)
                    {
                        view.SetData(resultList[i], true);
                        view.Show();
                    }
                    else
                    {
                        view.Hide();
                    }
                }
            }

            var costType = (CostType)summonRow.CostMaterial;
            StartPlayVideo(costType, great);
            RefreshUI(costType, summonRow, summonCount, bonus);
        }

        public void Show(SummonSheet.Row summonRow,
            int summonCount,
            List<FungibleAssetValue> resultList,
            System.Action completeCallback = null,
            bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            _completeCallback = completeCallback;

            animator.SetTrigger(AnimatorHashHide);

            var bonus = summonCount == 10 ? 1 : 0;
            summonItemViews.First().transform.parent.parent.gameObject.SetActive(false);
            manySummonItemViews.First().transform.parent.gameObject.SetActive(false);
            singleSummonItemView.Hide();

            if (summonCount == 1)
            {
                singleSummonItemView.SetData(resultList.First(), true, true);
                singleSummonItemView.Show();
            }
            else if (summonCount == 10)
            {
                summonItemViews.First().transform.parent.parent.gameObject.SetActive(true);
                for (var i = 0; i < summonItemViews.Length; i++)
                {
                    var view = summonItemViews[i];
                    if (i < resultList.Count)
                    {
                        view.SetData(resultList[i], true);
                        view.Show();
                    }
                    else
                    {
                        view.Hide();
                    }
                }
            }
            else
            {
                manySummonItemViews.First().transform.parent.gameObject.SetActive(true);
                for (var i = 0; i < manySummonItemViews.Length; i++)
                {
                    var view = manySummonItemViews[i];
                    if (i < resultList.Count)
                    {
                        view.SetData(resultList[i], true);
                        view.Show();
                    }
                    else
                    {
                        view.Hide();
                    }
                }
            }

            var costType = (CostType)summonRow.CostMaterial;
            StartPlayVideo(costType, true);
            RefreshUI(costType, summonRow, summonCount, bonus);
        }

        private void StartPlayVideo(CostType costType, bool great)
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }

            _coroutine = StartCoroutine(PlayVideo(costType, great));
        }

        private void RefreshUI(CostType costType, SummonSheet.Row data, int summonCount, int bonus)
        {
            _disposables.DisposeAllAndClear();

            closeButton.interactable = true;
            skipButton.interactable = true;
            background.anchoredPosition = Vector2.up * SummonUtil.GetBackGroundPosition(SummonFrontHelper.GetSummonResultByRow(data));

            normalDrawButton.gameObject.SetActive(false);
            goldenDrawButton.gameObject.SetActive(false);

            var drawButton = GetDrawButton(costType);
            if (drawButton == null)
            {
                return;
            }

            drawButton.Text = L10nManager.Localize("UI_DRAW_AGAIN_FORMAT", summonCount + bonus);
            drawButton.Subscribe(data, summonCount, _disposables);
            drawButton.gameObject.SetActive(true);
        }

        private SummonCostButton GetDrawButton(CostType costType)
        {
            return costType switch
            {
                CostType.SilverDust => normalDrawButton,
                CostType.GoldDust => goldenDrawButton,
                CostType.RubyDust => goldenDrawButton,
                CostType.EmeraldDust => goldenDrawButton,
                _ => null
            };
        }

        private IEnumerator PlayVideo(CostType costType, bool great)
        {
            _isGreat = great;
            var audioController = AudioController.instance;
            _previousMusicName = audioController.CurrentPlayingMusicName;
            audioController.StopAll(0.5f);

            var currentVideoClip = GetCurrentVideoClip(costType);
            if (currentVideoClip != null)
            {
                videoPlayer.clip = currentVideoClip.summoning;
                videoPlayer.SetDirectAudioVolume(0, AudioListener.volume);
                videoPlayer.gameObject.SetActive(true);
                skipButton.gameObject.SetActive(true);
                videoPlayer.Play();

                yield return new WaitUntil(() => videoPlayer.isPlaying);
                yield return new WaitUntil(() => !videoPlayer.isPlaying);

                if (great && currentVideoClip.great)
                {
                    videoPlayer.clip = currentVideoClip.great;
                }
                else
                {
                    videoPlayer.clip = currentVideoClip.result;
                }

                videoPlayer.Play();

                yield return new WaitUntil(() => videoPlayer.isPlaying);
                yield return new WaitUntil(() => !videoPlayer.isPlaying);

                videoPlayer.Stop();
                videoPlayer.gameObject.SetActive(false);
                skipButton.gameObject.SetActive(false);
            }

            yield return PlayResultAnimation(great);
        }

        private ResultVideoClip GetCurrentVideoClip(CostType costType)
        {
            return costType switch
            {
                CostType.SilverDust => normalVideoClip,
                CostType.GoldDust => goldenVideoClip,
                CostType.RubyDust => rubyVideoClip,
                CostType.EmeraldDust => emeraldVideoClip,
                _ => null
            };
        }

        private IEnumerator PlayResultAnimation(bool great)
        {
            var audioController = AudioController.instance;

            yield return null;
            audioController.PlaySfx(AudioController.SfxCode.Win);
            animator.SetTrigger(AnimatorHashShow);

            yield return DefaultAnimInterval;
            audioController.PlaySfx(AudioController.SfxCode.Success);
            var viewsToAnimate = summonItemViews
                .Concat(new[] { singleSummonItemView })
                .Concat(manySummonItemViews)
                .Where(v => v.gameObject.activeInHierarchy)
                .ToList();
            // 110회 소환의 경우 아이템을 스크롤로 내리며 보여줘야합니다. 해당 연출을 하는 호출 코드입니다
            if (viewsToAnimate.Count == 110)
            {
                StartCoroutine(DoMoveScroll());
            }

            foreach (var view in viewsToAnimate)
            {
                view.ShowWithAnimation();
                AudioController.PlaySelect();
                // 보여줄 아이템이 1개인 경우엔 그냥 0.1초 대기 후 연출을 처리합니다.
                // 그 이외의 경우, 모든 아이템의 대기 시간이 기존 11회 소환의 최종 연출 시간이었던 1.1초를 넘지 않도록 아이템 수로 나눠줍니다.
                yield return viewsToAnimate.Count != 1 ? new WaitForSeconds(1.1f/viewsToAnimate.Count) : ItemViewAnimInterval;
            }

            yield return DefaultAnimInterval;
            if (great)
            {
                audioController.PlaySfx(AudioController.SfxCode.RewardItem);
            }

            yield return null;
            audioController.PlayMusic(_previousMusicName);
            animator.SetTrigger(AnimatorHashShowButton);

            _completeCallback?.Invoke();
            _completeCallback = null;
        }

        private IEnumerator DoMoveScroll()
        {
            var pos = scrollView.anchoredPosition;
            pos.y = -(scrollView.sizeDelta.y * .5f);
            scrollView.anchoredPosition = pos;
            yield return new WaitForSeconds(.4f);
            scrollView.DoAnchoredMoveY(scrollView.sizeDelta.y * .5f, 2f);
        }
    }
}
