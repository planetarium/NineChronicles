using Nekoyume.Game.Controller;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class SubmitButton : MonoBehaviour
    {
        public Button button;
        public Image backgroundImage;
        public Image backgroundImageForSubmittable;
        public TextMeshProUGUI submitText;
        
        public readonly Subject<SubmitButton> OnSubmitClick = new Subject<SubmitButton>();

        private void Awake()
        {
            button.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                OnSubmitClick.OnNext(this);
            }).AddTo(gameObject);
        }
        
        public virtual void SetSubmittable(bool submittable)
        {
            button.interactable = submittable;
            backgroundImage.enabled = !submittable;
            backgroundImageForSubmittable.enabled = submittable;
            submitText.alpha = submittable ? 1f : .3f;
        }
    }
}
