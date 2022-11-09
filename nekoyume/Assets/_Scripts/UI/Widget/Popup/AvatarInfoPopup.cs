using System.Collections;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using Nekoyume.Model.EnumType;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Toggle = Nekoyume.UI.Module.Toggle;
using ToggleGroup = Nekoyume.UI.Module.ToggleGroup;

namespace Nekoyume.UI
{
    using UniRx;

    public class AvatarInfoPopup : XTweenPopupWidget
    {
        private const string NicknameTextFormat = "<color=#B38271>Lv.{0}</color=> {1}";

        [SerializeField]
        private AvatarInformation information;

        [SerializeField]
        private TextMeshProUGUI nicknameText;

        [SerializeField]
        private CategoryTabButton adventureButton;

        [SerializeField]
        private CategoryTabButton arenaButton;

        [SerializeField]
        private CategoryTabButton raidButton;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private Toggle grindModeToggle;

        [SerializeField]
        private GrindModule grindModule;

        [SerializeField]
        private GameObject grindModePanel;

        [SerializeField]
        private GameObject statusObject;

        [SerializeField]
        private GameObject equipmentSlotObject;

        private readonly ToggleGroup _toggleGroup = new();
        private readonly Dictionary<BattleType, System.Action> _onToggleCallback = new()
        {
            { BattleType.Adventure , null},
            { BattleType.Arena , null},
            { BattleType.Raid , null},
        };

        protected override void Awake()
        {
            _toggleGroup.RegisterToggleable(adventureButton);
            _toggleGroup.RegisterToggleable(arenaButton);
            _toggleGroup.RegisterToggleable(raidButton);

            adventureButton.OnClick
                .Subscribe(b =>
                {
                    OnClickPresetTab(b, BattleType.Adventure, _onToggleCallback[BattleType.Adventure]);
                })
                .AddTo(gameObject);
            arenaButton.OnClick
                .Subscribe(b =>
                {
                    OnClickPresetTab(b, BattleType.Arena, _onToggleCallback[BattleType.Arena]);
                })
                .AddTo(gameObject);
            raidButton.OnClick
                .Subscribe(b =>
                {
                    OnClickPresetTab(b, BattleType.Raid, _onToggleCallback[BattleType.Raid]);
                })
                .AddTo(gameObject);

            closeButton.onClick.AddListener(() =>
            {
                Close();
                AudioController.PlayClick();
            });

            base.Awake();
        }

        public override void Initialize()
        {
            base.Initialize();

            information.Initialize(false);

            grindModeToggle.onValueChanged.AddListener(toggledOn =>
            {
                grindModePanel.SetActive(toggledOn);
                statusObject.SetActive(!toggledOn);
                equipmentSlotObject.SetActive(!toggledOn);
                if (toggledOn)
                {
                    grindModule.Show();
                }
                else
                {
                    grindModule.gameObject.SetActive(false);
                    information.UpdateInventory(BattleType.Adventure);
                }
            });
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            grindModeToggle.isOn = false;
            information.UpdateInventory(BattleType.Adventure);
            OnClickPresetTab(adventureButton, BattleType.Adventure, _onToggleCallback[BattleType.Adventure]);
            HelpTooltip.HelpMe(100013, true);
        }

        private void OnClickPresetTab(
            IToggleable toggle,
            BattleType battleType,
            System.Action onSetToggle = null)
        {
            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            _toggleGroup.SetToggledOffAll();
            toggle.SetToggledOn();
            onSetToggle?.Invoke();

            UpdateNickname(currentAvatarState.level, currentAvatarState.NameWithHash);
            information.UpdateView(battleType);
            AudioController.PlayClick();
        }

        private void UpdateNickname(int level, string nameWithHash)
        {
            nicknameText.text = string.Format(NicknameTextFormat, level, nameWithHash);
        }

        #region For tutorial5

        public void TutorialActionClickAvatarInfoFirstInventoryCellView()
        {
            if (information.TryGetFirstCell(out var item))
            {
                item.Selected.Value = true;
            }
            else
            {
                Debug.LogError($"TutorialActionClickAvatarInfoFirstInventoryCellView() throw error.");
            }
        }

        public void TutorialActionCloseAvatarInfoWidget() => Close();

        #endregion
    }
}
