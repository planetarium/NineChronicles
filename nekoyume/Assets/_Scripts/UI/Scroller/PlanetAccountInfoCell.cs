using System.Linq;
using Nekoyume.L10n;
using Nekoyume.Multiplanetary;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Scroller
{
    public class PlanetAccountInfoCell :
        FancyGridViewCell<PlanetAccountInfoCell.ViewModel, PlanetAccountInfoScroll.ContextModel>
    {
        public class ViewModel
        {
            public string PlanetName;
            public PlanetAccountInfo PlanetAccountInfo;
            public bool NeedToImportKey;
        }

        private const string AccountTextFormat =
            "<color=#B38271>Lv. {0}</color> {1} <color=#A68F7E>#{2}</color>";

        private bool _isAppliedL10N;

        [SerializeField]
        private TextMeshProUGUI title;

        [SerializeField]
        private GameObject noAccount;

        [SerializeField]
        private TextMeshProUGUI noAccountText;

        [SerializeField]
        private Button noAccountCreateButton;

        [SerializeField]
        private TextMeshProUGUI noAccountCreateButtonText;

        [SerializeField]
        private GameObject account;

        [SerializeField]
        private TextMeshProUGUI[] accountTexts;

        [SerializeField]
        private Button accountImportKeyButton;

        [SerializeField]
        private TextMeshProUGUI accountImportKeyButtonText;

        private ViewModel _viewModel;

        public override void Initialize()
        {
            base.Initialize();
            ApplyL10nOnce();
            noAccountCreateButton.onClick.AddListener(() =>
                Context.OnClickCreateAccountSubject.OnNext((this, _viewModel)));
            accountImportKeyButton.onClick.AddListener(() =>
            {
                if (_viewModel.NeedToImportKey)
                {
                    Context.OnClickImportKeySubject.OnNext((this, _viewModel));
                }
                else
                {
                    Context.OnClickSelectPlanetSubject.OnNext((this, _viewModel));
                }
            });
        }

        public override void UpdateContent(ViewModel itemData)
        {
            _viewModel = itemData;
            ApplyL10nOnce();

            if (_viewModel is null)
            {
                NcDebug.Log("[PlanetAccountInfoCell] UpdateContent()... viewModel is null.");
                title.text = "null";
                noAccount.SetActive(false);
                account.SetActive(false);
                return;
            }

            title.text = _viewModel.PlanetName;

            var planetAccountInfo = _viewModel.PlanetAccountInfo;
            if (planetAccountInfo is null)
            {
                NcDebug.LogError("[PlanetAccountInfoCell] UpdateContent()... planetAccountInfo is null.");
                noAccount.SetActive(false);
                account.SetActive(false);
                return;
            }

            if (planetAccountInfo.AgentAddress is null)
            {
                noAccount.SetActive(true);
                account.SetActive(false);
                return;
            }

            var avatars = planetAccountInfo.AvatarGraphTypes.ToArray();
            if (avatars.Length == 0)
            {
                for (var i = 0; i < accountTexts.Length; ++i)
                {
                    var text = accountTexts[i];
                    if (i == 0)
                    {
                        text.text = L10nManager.IsInitialized
                            ? L10nManager.Localize("SDESC_NO_CHARACTER")
                            : "No character";
                        text.gameObject.SetActive(true);
                    }
                    else
                    {
                        text.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                for (var i = 0; i < accountTexts.Length; ++i)
                {
                    var text = accountTexts[i];
                    if (avatars.Length > i)
                    {
                        var avatar = avatars[i];
                        text.text = string.Format(
                            format: AccountTextFormat,
                            avatar.Level,
                            avatar.Name,
                            avatar.Address[..6]);
                        text.gameObject.SetActive(true);
                    }
                    else
                    {
                        text.gameObject.SetActive(false);
                    }
                }
            }

            accountImportKeyButtonText.text = _viewModel.NeedToImportKey
                ? L10nManager.Localize("BTN_IMPORT_KEY")
                : L10nManager.Localize("BTN_SELECT");
            noAccount.SetActive(false);
            account.SetActive(true);
        }

        private void ApplyL10nOnce()
        {
            if (_isAppliedL10N || !L10nManager.IsInitialized)
            {
                return;
            }

            _isAppliedL10N = true;
            noAccountText.text = L10nManager.Localize("SDESC_NO_ACCOUNT");
            noAccountCreateButtonText.text = L10nManager.Localize("BTN_CREATE_A_NEW_CHARACTER");
            accountImportKeyButtonText.text = L10nManager.Localize("BTN_IMPORT_KEY");
        }
    }
}
