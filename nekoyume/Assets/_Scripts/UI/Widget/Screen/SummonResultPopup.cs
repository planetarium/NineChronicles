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
    using UniRx;
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
        [SerializeField] private ResultVideoClip normalVideoClip;
        [SerializeField] private ResultVideoClip goldenVideoClip;

        [SerializeField] private SummonItemView[] summonItemViews;

        private int _normalSummonId = default;
        private Coroutine _coroutine;
        private readonly WaitForSeconds _waitAnimation = new(0.05f);
        private readonly List<IDisposable> _disposables = new();
        private static readonly int AnimatorHashShow = Animator.StringToHash("Show");

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() =>
            {
                Close();
            });
            CloseWidget = () =>
            {
                Close(true);
            };

            Summon.ButtonSubscribe(new[] { normalDrawButton, goldenDrawButton }, gameObject);
        }

        public void Show(
            SummonSheet.Row summonRow,
            List<Equipment> resultList,
            bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            if (_normalSummonId == default)
            {
                _normalSummonId = Find<Summon>().normalSummonId;
            }

            var normal = summonRow.GroupId == _normalSummonId;
            var count = resultList.Count;
            var great = resultList.First().Grade == 5;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[{nameof(SummonResultPopup)}] Summon Sorted Result");
            foreach (var item in resultList)
            {
                sb.AppendLine($"{item.GetLocalizedName()} {item.Grade}");
            }
            Debug.LogError(sb.ToString());

            var firstView = summonItemViews.First();
            var firstResult = resultList.First();
            firstView.SetData(firstResult, true, count == 1);

            for (var i = 1; i < summonItemViews.Length; i++)
            {
                var view = summonItemViews[i];
                if (i < count)
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

            _coroutine = StartCoroutine(PlayResult(normal, great));

            _disposables.DisposeAllAndClear();
            var drawButton = normal ? normalDrawButton : goldenDrawButton;
            drawButton.Text = L10nManager.Localize("UI_DRAW_AGAIN_FORMAT", count);
            Summon.ButtonSubscribe(drawButton, summonRow, count, _disposables);

            normalDrawButton.gameObject.SetActive(normal);
            goldenDrawButton.gameObject.SetActive(!normal);

            closeButton.interactable = true;
        }

        private IEnumerator PlayResult(bool normal, bool great)
        {
            var audioController = AudioController.instance;
            var currentMusic = audioController.CurrentPlayingMusicName;
            audioController.StopAll(0.5f);

            var currentVideoClip = normal ? normalVideoClip : goldenVideoClip;

            videoPlayer.clip = currentVideoClip.summoning;
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

            yield return null;
            animator.SetTrigger(AnimatorHashShow);
            foreach (var view in summonItemViews.Where(v => v.gameObject.activeSelf))
            {
                view.ShowWithAnimation();
                yield return _waitAnimation;
            }

            yield return new WaitForSeconds(1f);
            audioController.PlayMusic(currentMusic);
        }
    }
}
