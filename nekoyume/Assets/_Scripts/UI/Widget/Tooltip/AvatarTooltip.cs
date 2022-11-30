using System.Linq;
using Libplanet;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class AvatarTooltip : TooltipWidget<AvatarTooltip.ViewModel>
    {
        public class ViewModel : Model.Tooltip
        {
            public ViewModel(RectTransform target)
            {
                this.target.Value = target;
            }
        }

        [SerializeField]
        private FramedCharacterView characterView = null;

        [SerializeField]
        private TextMeshProUGUI levelText = null;

        [SerializeField]
        private TextMeshProUGUI titleText = null;

        [SerializeField]
        private TextMeshProUGUI nameAndHashText = null;

        [SerializeField]
        private Button avatarInfoButton = null;

        private AvatarState _selectedAvatarState = null;

        protected override PivotPresetType TargetPivotPresetType => PivotPresetType.TopRight;
        protected override float2 OffsetFromTarget => new float2(-20f, -30f);

        protected override void Awake()
        {
            base.Awake();
            avatarInfoButton
                .OnClickAsObservable()
                .Subscribe(OnClickAvatarInfo)
                .AddTo(gameObject);
        }

        protected override void Update()
        {
            base.Update();

            if (Input.touchCount == 0 &&
                !Input.anyKeyDown)
            {
                return;
            }

            var current = EventSystem.current;
            if (current.currentSelectedGameObject == avatarInfoButton.gameObject)
            {
                return;
            }

            Close();
        }

        public void Show(RectTransform target, Address avatarAddress)
        {
            ShowAsync(target, avatarAddress);
        }

        public async void ShowAsync(RectTransform target, Address avatarAddress)
        {
            var (exist, avatarState) = await States.TryGetAvatarStateAsync(avatarAddress);
            if (!exist)
            {
                return;
            }

            Show(target, avatarState);
        }

        public void Show(RectTransform target, AvatarState avatarState)
        {
            var isCurrentAvatar =
                States.Instance.CurrentAvatarState?.address.Equals(avatarState.address)
                ?? false;
            Show(target, avatarState, isCurrentAvatar);
        }

        public void Show(RectTransform target, AvatarState avatarState, bool isCurrentAvatar)
        {
            if (isCurrentAvatar)
            {
                var player = Game.Game.instance.Stage.SelectedPlayer;
                if (player is null)
                {
                    player = Game.Game.instance.Stage.GetPlayer();
                    characterView.SetByPlayer(player);
                    player.gameObject.SetActive(false);
                }
                else
                {
                    characterView.SetByPlayer(player);
                }
            }
            else
            {
                characterView.SetByAvatarState(avatarState);
            }

            levelText.text = $"<color=#B38271>LV.{avatarState.level}</color>";

            var title = avatarState.inventory.Costumes.FirstOrDefault(costume =>
                costume.ItemSubType == ItemSubType.Title &&
                costume.equipped);
            titleText.text = title is null
                ? ""
                : title.GetLocalizedNonColoredName();
            nameAndHashText.text = avatarState.NameWithHash;
            _selectedAvatarState = avatarState;
            avatarInfoButton.gameObject.SetActive(!isCurrentAvatar);
            Show(new ViewModel(target));
        }

        private void OnClickAvatarInfo(Unit unit)
        {
            AudioController.PlayClick();
            Find<FriendInfoPopup>().ShowAsync(_selectedAvatarState, BattleType.Adventure, true).Forget();
            Close();
        }
    }
}
