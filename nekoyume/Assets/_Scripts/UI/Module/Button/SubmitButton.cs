using Assets.SimpleLocalization;
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
        public TextMeshProUGUI disabledText;
        
        public readonly Subject<SubmitButton> OnSubmitClick = new Subject<SubmitButton>();

        private void Awake()
        {
            button.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                OnSubmitClick.OnNext(this);
            }).AddTo(gameObject);
        }

        public void Show() => gameObject.SetActive(true);

        public void Hide() => gameObject.SetActive(false);
        
        public virtual void SetSubmittable(bool submittable)
        {
            button.interactable = submittable;
            backgroundImage.enabled = !submittable; 
            backgroundImageForSubmittable.enabled = submittable;
            submitText.gameObject.SetActive(submittable);
            disabledText.gameObject.SetActive(!submittable);
        }

        public void SetText(string value, bool localize = true)
        {
            submitText.text = LocalizationManager.Localize(value);
            disabledText.text = LocalizationManager.Localize(value);
        }
    }
}
