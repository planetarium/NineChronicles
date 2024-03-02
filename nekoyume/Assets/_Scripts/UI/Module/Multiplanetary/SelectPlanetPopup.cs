using Nekoyume.L10n;
using Nekoyume.Multiplanetary;
using Nekoyume.UI.Scroller;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module.Multiplanetary
{
    public class SelectPlanetPopup : MonoBehaviour
    {
        [SerializeField]
        private Button selectPlanetPopupBgButton;

        [SerializeField]
        private TextMeshProUGUI selectPlanetPopupTitleText;

        [SerializeField]
        private SelectPlanetScroll selectPlanetScroll;

        public readonly Subject<(SelectPlanetScroll scroll, PlanetId selectedPlanetId)>
            OnChangeSelectedPlanetSubject = new();

        public bool IsVisible => gameObject.activeSelf;

        private void Awake()
        {
            selectPlanetPopupBgButton.onClick.AddListener(Hide);
            selectPlanetScroll.OnClickSelectedPlanetSubject
                .Subscribe(_ => Hide())
                .AddTo(gameObject);
            selectPlanetScroll.OnChangeSelectedPlanetSubject
                .Subscribe(tuple =>
                {
                    OnChangeSelectedPlanetSubject.OnNext(tuple);
                    Hide();
                })
                .AddTo(gameObject);
        }

        public void ApplyL10n()
        {
            selectPlanetPopupTitleText.text = L10nManager.Localize("UI_SELECT_YOUR_PLANET");
        }

        public void SetData(PlanetRegistry planetRegistry, PlanetId? selectedPlanetId)
        {
            selectPlanetScroll.SetData(planetRegistry, selectedPlanetId);
        }

        public void Show()
        {
            if (IsVisible)
            {
                return;
            }

            gameObject.SetActive(true);
        }

        public void Show(PlanetRegistry planetRegistry, PlanetId? selectedPlanetId)
        {
            SetData(planetRegistry, selectedPlanetId);
            Show();
        }

        public void Hide()
        {
            if (!IsVisible)
            {
                return;
            }

            gameObject.SetActive(false);
        }
    }
}
