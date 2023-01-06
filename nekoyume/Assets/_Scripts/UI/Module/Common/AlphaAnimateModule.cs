using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Nekoyume.UI.Module.Common
{
    public class AlphaAnimateModule : MonoBehaviour
    {
        [SerializeField]
        protected CanvasGroup canvasGroup = null;

        [SerializeField]
        private bool animateAlpha = false;

        private Tweener _tweener;

        protected virtual void OnEnable()
        {
            if (animateAlpha)
            {
                canvasGroup.alpha = 0f;
                _tweener = canvasGroup.DOFade(1, 1f);
            }
            else
            {
                canvasGroup.alpha = 1f;
            }

            if (Platform.IsMobilePlatform())
            {
                EventTrigger eventTrigger = this.GetComponent<EventTrigger>();
                if(eventTrigger is null)
                {
                    return;
                }
                // Disable pointer_exit event on mobile platform
                // Because this causes wrong interaction
                foreach (var item in eventTrigger.triggers)
                {
                    if (item.eventID == EventTriggerType.PointerExit)
                    {
                        eventTrigger.triggers.Remove(item);
                        break;
                    }
                }
            }
        }

        protected virtual void OnDisable()
        {
            _tweener?.Kill();
        }
    }
}
