using Nekoyume.L10n;
using Nekoyume.Multiplanetary;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module.Multiplanetary
{
    using System;
    using UniRx;
    using UnityEngine.UI;

    public class PlanetAccountInfosPopup : MonoBehaviour
    {
        public enum SubmitState
        {
            Proceed,
            ImportKey,
            ResetKey,
        }

        private SubmitState _submitState;

        [SerializeField]
        private TextMeshProUGUI titleText;

        [SerializeField]
        private GameObject importKeyDescriptionGO;

        [SerializeField]
        private TextMeshProUGUI importKeyDescriptionText;

        [SerializeField]
        private GameObject resetKeyDescriptionGO;

        [SerializeField]
        private TextMeshProUGUI resetKeyDescriptionText;

        [SerializeField]
        private PlanetAccountInfoScroll planetAccountInfoScroll;

        [SerializeField]
        private Button submitButton;

        [SerializeField]
        private TextMeshProUGUI submitButtonText;

        public readonly Subject<SubmitState> OnSubmitSubject = new();

        public bool IsVisible => gameObject.activeSelf;

        private void Awake()
        {
            submitButton.onClick.AddListener(() =>
            {
                OnSubmitSubject.OnNext(_submitState);
                Hide();
            });
        }

        private void OnDestroy()
        {
            OnSubmitSubject.Dispose();
        }

        public void ApplyL10n()
        {
            titleText.text = L10nManager.Localize("WORD_NOTIFICATION");
            importKeyDescriptionText.text =
                L10nManager.Localize("STC_MULTIPLANETARY_AGENT_INFOS_POPUP_ACCOUNT_ALREADY_EXIST");
            resetKeyDescriptionText.text =
                L10nManager.Localize("STC_MULTIPLANETARY_AGENT_INFOS_POPUP_RESET_KEY");
        }

        public void SetData(
            PlanetRegistry planetRegistry,
            PlanetAccountInfo[] planetAccountInfos,
            SubmitState submitState)
        {
            planetAccountInfoScroll.SetData(planetRegistry, planetAccountInfos);
            _submitState = submitState;
            switch (_submitState)
            {
                case SubmitState.Proceed:
                    submitButtonText.text = L10nManager.Localize("BTN_PROCEED");
                    importKeyDescriptionGO.SetActive(true);
                    resetKeyDescriptionGO.SetActive(false);
                    break;
                case SubmitState.ImportKey:
                    submitButtonText.text = L10nManager.Localize("BTN_IMPORT_KEY");
                    importKeyDescriptionGO.SetActive(true);
                    resetKeyDescriptionGO.SetActive(false);
                    break;
                case SubmitState.ResetKey:
                    submitButtonText.text = L10nManager.Localize("BTN_RESET_KEY");
                    importKeyDescriptionGO.SetActive(false);
                    resetKeyDescriptionGO.SetActive(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(submitState), submitState, null);
            }
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
            SubmitState submitState)
        {
            SetData(planetRegistry, planetAccountInfos, submitState);
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
