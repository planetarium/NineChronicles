using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Item;
using Nekoyume.UI.Module;
using TMPro;
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
        [SerializeField] private Button drawButton;
        [SerializeField] private TextMeshProUGUI drawButtonText;

        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private ResultVideoClip normalVideoClip;
        [SerializeField] private ResultVideoClip goldenVideoClip;

        [SerializeField] private SummonItemView[] summonItemViews;

        private bool _isPlaying;
        private int _viewCount;
        private Coroutine _Coroutine;
        private int _normalSummonId = default;

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

        }

        public void Show(int groupId, List<Equipment> resultList, bool ignoreShowAnimation = false)
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

            _viewCount = Mathf.Min(count, summonItemViews.Length);

            var firstView = summonItemViews.First();
            var firstResult = resultList.First();
            firstView.SetData(firstResult, count == 1);

            for (var i = 1; i < summonItemViews.Length; i++)
            {
                var view = summonItemViews[i];
                if (i < count)
                {
                    view.SetData(resultList[i]);
                }
                else
                {
                    view.gameObject.SetActive(false);
                }
            }

            if (_Coroutine != null)
            {
                StopCoroutine(_Coroutine);
                _Coroutine = null;
            }

            _Coroutine = StartCoroutine(PlayResult(normal, great));

            // For Test
            drawButtonText.text = $"Summon\n{count} times";
            drawButton.interactable = true;
            drawButton.onClick.RemoveAllListeners();
            drawButton.onClick.AddListener(() =>
            var drawButton = normal ? normalDrawButton : goldenDrawButton;
            {
                drawButton.interactable = false;
                Find<Summon>().AuraSummonAction(groupId, count);
            });
            closeButton.interactable = true;
        }

        private IEnumerator PlayResult(bool normal, bool great)
        {
            _isPlaying = false;
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
            for (var i = 0; i < _viewCount; i++)
            {
                var view = summonItemViews[i];
                view.ShowWithAnimation();
                yield return new WaitForSeconds(0.05f);
            }
        }
    }
}
