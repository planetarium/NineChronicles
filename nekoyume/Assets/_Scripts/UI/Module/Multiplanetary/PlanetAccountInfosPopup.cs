using Nekoyume.L10n;
using Nekoyume.Multiplanetary;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module.Multiplanetary
{
    using UniRx;

    public class PlanetAccountInfosPopup : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI planetAccountInfosTitleText;

        [SerializeField]
        private TextMeshProUGUI planetAccountInfosDescriptionText;

        [SerializeField]
        private PlanetAccountInfoScroll planetAccountInfoScroll;

        public readonly Subject<(PlanetAccountInfoScroll scroll, PlanetId selectedPlanetId)>
            OnSelectedPlanetSubject = new();

        public bool IsVisible => gameObject.activeSelf;

        private void Awake()
        {
            planetAccountInfoScroll.OnSelectedPlanetSubject.Subscribe(tuple =>
            {
                OnSelectedPlanetSubject.OnNext(tuple);
                Hide();
            }).AddTo(gameObject);
        }

        public void ApplyL10n()
        {
            planetAccountInfosTitleText.text = L10nManager.Localize("WORD_NOTIFICATION");
            planetAccountInfosDescriptionText.text =
                L10nManager.Localize("STC_MULTIPLANETARY_AGENT_INFOS_POPUP_ACCOUNT_ALREADY_EXIST");
        }

        public void SetData(
            PlanetRegistry planetRegistry,
            PlanetAccountInfo[] planetAccountInfos,
            bool needToImportKey)
        {
            planetAccountInfoScroll.SetData(planetRegistry, planetAccountInfos, needToImportKey);
        }

        public void Show()
        {
            if (IsVisible)
            {
                return;
            }

            gameObject.SetActive(true);
        }

        public void Show(
            PlanetRegistry planetRegistry,
            PlanetAccountInfo[] planetAccountInfos,
            bool needToImportKey)
        {
            SetData(planetRegistry, planetAccountInfos, needToImportKey);
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
