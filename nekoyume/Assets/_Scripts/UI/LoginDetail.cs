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
using UnityEngine.UI;

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
        public TextMeshProUGUI jobDescriptionText;
        public StatusInfo[] statusRows;

        public Button archerButton;
        public Button mageButton;
        public Button acolyteButton;

        private int _selectedIndex;
        private bool _isCreateMode;

        private int _hair;
        private int _lens;
        private int _etc;
        

        protected override void Awake()
        {
            base.Awake();

            btnCreateText.text = LocalizationManager.Localize("UI_CREATE_CHARACTER_CONFIRM");
            paletteHairText.text = $"{LocalizationManager.Localize("UI_HAIR")} {_hair}";
            paletteLensText.text = $"{LocalizationManager.Localize("UI_LENS")} {_lens}";
            paletteTopText.text = $"{LocalizationManager.Localize("UI_ETC")} {_etc}";
            jobDescriptionText.text = LocalizationManager.Localize("UI_WARRIOR_DESCRIPTION");

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
                .CreateAvatar(AvatarManager.CreateAvatarAddress(), _selectedIndex, nickName, _hair, _lens, _etc)
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

            Debug.LogWarning($"customize : {avatarState.hair}, {avatarState.lens}, {avatarState.etc}");
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

        public override void Initialize()
        {
            base.Initialize();
            archerButton.onClick.AddListener(OnClickNotImplemented);
            mageButton.onClick.AddListener(OnClickNotImplemented);
            acolyteButton.onClick.AddListener(OnClickNotImplemented);
        }

        private void OnClickNotImplemented()
        {
            if (_isCreateMode)
                Find<Alert>().Show("UI_ALERT_NOT_IMPLEMENTED_TITLE", "UI_ALERT_NOT_IMPLEMENTED_CONTENT");
        }

        public override void Show()
        {
            _hair = _lens = _etc = 0;
            base.Show();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
        }

        public void ChangeHair(int offset)
        {
            _hair += offset;
            if (_hair < 0) _hair = 0;
            paletteHairText.text = $"{LocalizationManager.Localize("UI_HAIR")} {_hair}";
        }

        public void ChangeLens(int offset)
        {
            _lens += offset;
            if (_lens < 0) _lens = 0;
            paletteLensText.text = $"{LocalizationManager.Localize("UI_LENS")} {_lens}";
        }

        public void ChangeEtc(int offset)
        {
            _etc += offset;
            if (_etc < 0) _etc = 0;
            paletteTopText.text = $"{LocalizationManager.Localize("UI_ETC")} {_etc}";
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
