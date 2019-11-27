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
        public TextMeshProUGUI text;
        public Image image;
        public Button button;
        public string localizationKey;
        
        #region Mono

        protected virtual void Awake()
        {
            text.text = LocalizationManager.Localize(string.IsNullOrEmpty(localizationKey) ? "null" : localizationKey);
            button.OnClickAsObservable().Subscribe(_ => AudioController.PlayClick()).AddTo(gameObject);
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
