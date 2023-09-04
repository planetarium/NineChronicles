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
        [SerializeField] private Button closeButton;
        [SerializeField] private Button drawButton;
        [SerializeField] private TextMeshProUGUI drawButtonText;

        [SerializeField] private SummonItemView[] summonItemViews;

        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private VideoClip greatVideoClip;
        [SerializeField] private VideoClip normalVideoClip;

        private int _viewCount;
        private Coroutine _animationCoroutine;

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

            videoPlayer.loopPointReached += OnVideoEnd;
        }

        public void Show(int groupId, List<Equipment> resultList, bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

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

            videoPlayer.clip = great ? greatVideoClip : normalVideoClip;
            videoPlayer.gameObject.SetActive(true);
            videoPlayer.Play();

            // For Test
            drawButtonText.text = $"Summon\n{count} times";
            drawButton.interactable = true;
            drawButton.onClick.RemoveAllListeners();
            drawButton.onClick.AddListener(() =>
            {
                drawButton.interactable = false;
                Find<Summon>().AuraSummonAction(groupId, count);
            });
            closeButton.interactable = true;
        }

        private void OnVideoEnd(VideoPlayer source)
        {
            videoPlayer.Stop();
            videoPlayer.gameObject.SetActive(false);

            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }

            _animationCoroutine = StartCoroutine(CoShowWithAnimation());
        }

        private IEnumerator CoShowWithAnimation()
        {
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
