using Assets.SimpleLocalization;
using Nekoyume.Game.Controller;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class NormalButton : MonoBehaviour
    {
        [SerializeField]
        private Button button = null;

        [SerializeField]
        private TextMeshProUGUI text = null;

        [SerializeField]
        private string localizationKey = null;

        [SerializeField]
        private Canvas sortingGroup;

        public readonly Subject<NormalButton> OnClick = new Subject<NormalButton>();

        #region Mono

        protected virtual void Awake()
        {
            text.text = LocalizationManager.Localize(string.IsNullOrEmpty(localizationKey) ? "null" : localizationKey);
            button.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                OnClick.OnNext(this);
            }).AddTo(gameObject);

            sortingGroup.sortingLayerName = "UI";
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

        public void SetSortOrderToTop()
        {
            sortingGroup.sortingOrder = 100;
        }

        public void SetSortOrderToNormal()
        {
            sortingGroup.sortingOrder = 0;
        }
    }
}
