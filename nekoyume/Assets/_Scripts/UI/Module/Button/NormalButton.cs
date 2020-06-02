using Assets.SimpleLocalization;
using Nekoyume.Game.Controller;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class NormalButton : MonoBehaviour
    {
        [SerializeField]
        private Button button;

        [SerializeField]
        private Image image;

        [SerializeField]
        private TextMeshProUGUI text;

        [SerializeField]
        private string localizationKey;

        public readonly Subject<NormalButton> OnClick = new Subject<NormalButton>();

        #region Mono

        protected void Awake()
        {
            text.text = LocalizationManager.Localize(string.IsNullOrEmpty(localizationKey) ? "null" : localizationKey);
            button.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                OnClick.OnNext(this);
            }).AddTo(gameObject);
        }

        #endregion

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
