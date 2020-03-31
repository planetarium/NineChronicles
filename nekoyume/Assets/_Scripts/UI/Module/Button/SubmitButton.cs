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
        public TextMeshProUGUI submitTextForSubmittable;

        public readonly Subject<SubmitButton> OnSubmitClick = new Subject<SubmitButton>();

        public bool IsSubmittable { get; private set; }

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
            IsSubmittable = submittable;
            button.interactable = submittable;
            backgroundImage.enabled = !submittable;
            backgroundImageForSubmittable.enabled = submittable;
            submitText.gameObject.SetActive(!submittable);
            submitTextForSubmittable.gameObject.SetActive(submittable);
        }

        public void SetSubmitText(string text)
        {
            SetSubmitText(text, text);
        }

        public void SetSubmitText(string nonSubmittableText, string submittableText)
        {
            submitText.text = nonSubmittableText;
            submitTextForSubmittable.text = submittableText;
        }
    }
}
