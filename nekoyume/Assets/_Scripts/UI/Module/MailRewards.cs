using System.Collections.Generic;
using DG.Tweening;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume
{
    public class MailRewards : MonoBehaviour
    {
        [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField]
        private List<MailRewardItemView> items;

        public void Set(List<MailReward> mailRewards)
        {
            if (mailRewards.Count > items.Count)
            {
                return;
            }

            foreach (var item in items)
            {
                item.gameObject.SetActive(false);
            }

            for (var i = 0; i < mailRewards.Count; ++i)
            {
                items[i].gameObject.SetActive(true);
                items[i].Set(mailRewards[i]);
            }

            canvasGroup.alpha = 1;
        }

        public void ShowEffect()
        {
            foreach (var item in items)
            {
                item.ShowEffect();
            }
        }

        public void FadeOut(float duration, float delay, AnimationCurve graph)
        {
            canvasGroup.DOFade(0, duration).SetDelay(delay).SetEase(graph);
        }
    }
}
