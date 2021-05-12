using Nekoyume.Game.Controller;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    [RequireComponent(typeof(Animator))]
    public class SubmitButton : MonoBehaviour
    {
        [SerializeField]
        private Button button = null;

        [SerializeField]
        private Image backgroundImage = null;

        [SerializeField]
        private Image backgroundImageForSubmittable = null;

        [SerializeField]
        private TextMeshProUGUI submitText = null;

        [SerializeField]
        private TextMeshProUGUI submitTextForSubmittable = null;

        private Animator _animatorCache;

        public Animator Animator => !_animatorCache
            ? _animatorCache = GetComponent<Animator>()
            : _animatorCache;

        public bool Interactable => button.interactable;

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

        public void SetSubmittableWithoutInteractable(bool submittable)
        {
            IsSubmittable = submittable;
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

        public void SetSubmitTextColor(Color color)
        {
            submitText.color = color;
        }
    }
}
