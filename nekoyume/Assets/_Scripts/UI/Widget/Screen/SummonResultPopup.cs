using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Controller;
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
        [SerializeField] private SimpleCostButton normalDrawButton;
        [SerializeField] private SimpleCostButton goldenDrawButton;

        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private Button skipButton;
        [SerializeField] private ResultVideoClip normalVideoClip;
        [SerializeField] private ResultVideoClip goldenVideoClip;

        [SerializeField] private SummonItemView[] summonItemViews;
        [SerializeField] private SummonItemView singleSummonItemView;

        private int _normalSummonId;
        private bool _isGreat;
        private Coroutine _coroutine;
        private string _previousMusicName;
        private readonly List<IDisposable> _disposables = new();
        private static readonly WaitForSeconds ItemViewAnimInterval = new(0.1f);
        private static readonly WaitForSeconds DefaultAnimInterval = new(1f);
        private static readonly int AnimatorHashHide = Animator.StringToHash("Hide");
        private static readonly int AnimatorHashShow = Animator.StringToHash("Show");
        private static readonly int AnimatorHashShowButton = Animator.StringToHash("ShowButton");

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() =>
            {
                Close(true);
            });
            CloseWidget = () =>
            {
                Close(true);
            };
            skipButton.onClick.AddListener(() =>
            {
                if (_coroutine != null)
                {
                    StopCoroutine(_coroutine);
                    _coroutine = null;
                }

                videoPlayer.Stop();
                videoPlayer.gameObject.SetActive(false);
                StartCoroutine(PlayResultAnimation(_isGreat));
            });

            Summon.ButtonSubscribe(new[] { normalDrawButton, goldenDrawButton }, gameObject);
        }

        public void Show(
            SummonSheet.Row summonRow,
            int summonCount,
            List<Equipment> resultList,
            bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            animator.SetTrigger(AnimatorHashHide);

            if (_normalSummonId == default)
            {
                _normalSummonId = Find<Summon>().normalSummonId;
            }

            var normal = summonRow.GroupId == _normalSummonId;
            var bonus = summonCount == 10 ? 1 : 0;
            var great = resultList.First().Grade == 5;

            var single = summonCount == 1;
            if (single)
            {
                singleSummonItemView.SetData(resultList.First(), true, true);
                singleSummonItemView.Show();
            }
            else
            {
                singleSummonItemView.Hide();
            }

            for (var i = 0; i < summonItemViews.Length; i++)
            {
                var view = summonItemViews[i];
                if (!single && i < resultList.Count)
                {
                    view.SetData(resultList[i], true);
                    view.Show();
                }
                else
                {
                    view.Hide();
                }
            }

            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }

            _coroutine = StartCoroutine(PlayVideo(normal, great));

            _disposables.DisposeAllAndClear();
            var drawButton = normal ? normalDrawButton : goldenDrawButton;
            drawButton.Text = L10nManager.Localize("UI_DRAW_AGAIN_FORMAT", summonCount + bonus);
            Summon.ButtonSubscribe(drawButton, summonRow, summonCount, _disposables);

            normalDrawButton.gameObject.SetActive(normal);
            goldenDrawButton.gameObject.SetActive(!normal);

            closeButton.interactable = true;
            skipButton.interactable = true;
        }

        private IEnumerator PlayVideo(bool normal, bool great)
        {
            _isGreat = great;
            var audioController = AudioController.instance;
            _previousMusicName = audioController.CurrentPlayingMusicName;
            audioController.StopAll(0.5f);

            var currentVideoClip = normal ? normalVideoClip : goldenVideoClip;

            videoPlayer.clip = currentVideoClip.summoning;
            videoPlayer.SetDirectAudioVolume(0, AudioListener.volume);
            videoPlayer.gameObject.SetActive(true);
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

            yield return PlayResultAnimation(great);
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
                .Where(v => v.gameObject.activeSelf);
            foreach (var view in viewsToAnimate)
            {
                view.ShowWithAnimation();
                AudioController.PlaySelect();
                yield return ItemViewAnimInterval;
            }

            yield return DefaultAnimInterval;
            if (great)
            {
                audioController.PlaySfx(AudioController.SfxCode.RewardItem);
            }

            yield return null;
            audioController.PlayMusic(_previousMusicName);
            animator.SetTrigger(AnimatorHashShowButton);
        }
    }
}
