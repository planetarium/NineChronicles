using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.State;
using Nekoyume.Game.Controller;
using Nekoyume.Model;
using UniRx;
using UnityEngine;
using System.Text.RegularExpressions;
using Nekoyume.UI.Module;
using TMPro;

namespace Nekoyume.UI
{
    public class LoginDetail : Widget
    {
        public GameObject btnLogin;
        public GameObject btnCreate;
        public TextMeshProUGUI btnCreateText;
        public TextMeshProUGUI levelAndNameInfo;
        public RectTransform content;
        public GameObject profileImage;
        public GameObject palette;
        public TextMeshProUGUI paletteHairText;
        public TextMeshProUGUI paletteLensText;
        public TextMeshProUGUI paletteTopText;
        public StatusInfo[] statusRows;

        private int _selectedIndex;
        private bool _isCreateMode;

        protected override void Awake()
        {
            base.Awake();

            btnCreateText.text = LocalizationManager.Localize("UI_CREATE_CHARACTER_CONFIRM");
            paletteHairText.text = LocalizationManager.Localize("UI_HAIR");
            paletteLensText.text = LocalizationManager.Localize("UI_LENS");
            paletteTopText.text = LocalizationManager.Localize("UI_ETC");

            Game.Event.OnLoginDetail.AddListener(Init);
        }

        public void CreateClick()
        {
            var inputBox = Find<InputBox>();
            inputBox.CloseCallback = result =>
            {
                if (result == ConfirmResult.Yes)
                {
                    CreateAndLogin(inputBox.text);
                }
            };
            inputBox.Show("UI_INPUT_NAME", "UI_NICKNAME_CONDITION");
        }

        public void CreateAndLogin(string nickName)
        {
            if (!Regex.IsMatch(nickName, GameConfig.AvatarNickNamePattern))
            {
                Find<Alert>().Show("UI_ERROR", "UI_NICKNAME_CONDITION");
                return;
            }

            Find<GrayLoadingScreen>().Show();

            ActionManager.instance
                .CreateAvatar(AvatarManager.CreateAvatarAddress(), _selectedIndex, nickName)
                .Subscribe(eval =>
                {
                    var avatarState = AvatarManager.SetIndex(_selectedIndex);
                    OnDidAvatarStateLoaded(avatarState);
                    Find<GrayLoadingScreen>()?.Close();
                }, onError: e => Widget.Find<ActionFailPopup>().Show("Action timeout during CreateAvatar."));
            AudioController.PlayClick();
        }

        public void LoginClick()
        {
            btnLogin.SetActive(false);
            var avatarState = AvatarManager.SetIndex(_selectedIndex);
            OnDidAvatarStateLoaded(avatarState);
            AudioController.PlayClick();
        }

        public void BackClick()
        {
            Close();
            Game.Event.OnNestEnter.Invoke();
            var login = Find<Login>();
            login.Show();
            AudioController.PlayClick();
        }

        private void Init(int index)
        {
            _selectedIndex = index;
            Player player;
            _isCreateMode = !States.Instance.AvatarStates.ContainsKey(index);
            if (_isCreateMode)
            {
                player = new Player(1);
            }
            else
            {
                States.Instance.CurrentAvatarState.Value = States.Instance.AvatarStates[_selectedIndex];
                player = new Player(States.Instance.CurrentAvatarState.Value);
            }

            palette.SetActive(_isCreateMode);
            // create new or login
            btnCreate.SetActive(_isCreateMode);
            levelAndNameInfo.gameObject.SetActive(!_isCreateMode);
            if (!_isCreateMode)
            {
                var level = player.Level;
                var name = States.Instance.CurrentAvatarState.Value.name;
                levelAndNameInfo.text = $"LV. {level} {name}";
            }

            // 프로필 사진의 용도가 정리되지 않아서 주석 처리함.
            // profileImage.SetActive(!isCreateMode);
            btnLogin.SetActive(!_isCreateMode);
            
            SetInformation(player);

            Show();
        }

        private void SetInformation(Player player)
        {
            var tuples = player.GetStatTuples();
            int idx = 0;
            foreach(var (statType, value, additionalValue) in tuples)
            {
                var info = statusRows[idx];
                info.Set(statType, value, additionalValue);
                ++idx;
            }
        }

        public override void Show()
        {
            base.Show();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
        }

        private void OnDidAvatarStateLoaded(AvatarState avatarState)
        {
            if (_isCreateMode)
                Close();
            
            EnterRoom();
        }
        
        private void EnterRoom()
        {
            Game.Event.OnRoomEnter.Invoke();
            Find<Login>()?.Close();
            Close();
        }
    }
}
