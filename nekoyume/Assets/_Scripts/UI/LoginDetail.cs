using System;
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
using Nekoyume.TableData;
using Nekoyume.Model.State;

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
        public TextMeshProUGUI paletteEarText;
        public TextMeshProUGUI paletteTailText;
        public TextMeshProUGUI jobDescriptionText;
        public DetailedStatView[] statusRows;

        public Button warriorButton;
        public Button archerButton;
        public Button mageButton;
        public Button acolyteButton;

        private int _selectedIndex;
        private bool _isCreateMode;

        private int _hair;
        private int _lens;
        private int _ear;
        private int _tail;
        

        protected override void Awake()
        {
            base.Awake();

            btnCreateText.text = LocalizationManager.Localize("UI_CREATE_CHARACTER_CONFIRM");
            jobDescriptionText.text = LocalizationManager.Localize("UI_WARRIOR_DESCRIPTION");

            Game.Event.OnLoginDetail.AddListener(Init);

            CloseWidget = BackClick;
            SubmitWidget = CreateClick;
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
                .CreateAvatar(AvatarState.CreateAvatarAddress(), _selectedIndex, nickName, _hair, _lens, _ear, _tail)
                .Subscribe(eval =>
                {
                    var avatarState = States.Instance.SelectAvatar(_selectedIndex);
                    OnDidAvatarStateLoaded(avatarState);
                    ActionRenderHandler.Instance.RenderQuest(avatarState.address, eval.Action.completedQuestIds);
                    Find<GrayLoadingScreen>()?.Close();
                }, onError: e => Widget.Find<ActionFailPopup>().Show("Action timeout during CreateAvatar."));
            AudioController.PlayClick();
        }

        public void LoginClick()
        {
            btnLogin.SetActive(false);
            var avatarState = States.Instance.SelectAvatar(_selectedIndex);
            OnDidAvatarStateLoaded(avatarState);
            AudioController.PlayClick();
        }

        public void BackToLogin()
        {
            Close();
            Game.Event.OnNestEnter.Invoke();
            var login = Find<Login>();
            login.Show();
        }

        private void Init(int index)
        {
            _selectedIndex = index;
            Player player;
            _isCreateMode = !States.Instance.AvatarStates.ContainsKey(index);
            
            // FIXME TableSheetsState.Current 써도 괜찮은지 체크해야 합니다.
            TableSheets tableSheets = TableSheets.FromTableSheetsState(TableSheetsState.Current);

            if (_isCreateMode)
            {
                player = new Player(1, tableSheets);
            }
            else
            {
                States.Instance.SelectAvatar(_selectedIndex);
                player = new Player(
                    States.Instance.CurrentAvatarState,
                    tableSheets
                );
            }

            palette.SetActive(_isCreateMode);
            // create new or login
            btnCreate.SetActive(_isCreateMode);
            levelAndNameInfo.gameObject.SetActive(!_isCreateMode);
            if (!_isCreateMode)
            {
                var level = player.Level;
                var name = States.Instance.CurrentAvatarState.NameWithHash;
                levelAndNameInfo.text = $"LV. {level} {name}";
            }

            // 프로필 사진의 용도가 정리되지 않아서 주석 처리함.
            // profileImage.SetActive(!isCreateMode);
            btnLogin.SetActive(!_isCreateMode);
            
            SetInformation(player);

            if (_isCreateMode)
            {
                SubmitWidget = CreateClick;
            }
            else
            {
                SubmitWidget = LoginClick;
            }

            Show();
        }

        private void SetInformation(Player player)
        {
            var tuples = player.Stats.GetBaseAndAdditionalStats();
            int idx = 0;
            foreach(var (statType, value, additionalValue) in tuples)
            {
                var info = statusRows[idx];
                info.Show(statType, value, additionalValue);
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
            warriorButton.gameObject.SetActive(_isCreateMode);
            archerButton.gameObject.SetActive(_isCreateMode);
            mageButton.gameObject.SetActive(_isCreateMode);
            acolyteButton.gameObject.SetActive(_isCreateMode);
            if (_isCreateMode)
            {
                _hair = _lens = _ear = _tail =  0;
                paletteHairText.text = $"{LocalizationManager.Localize("UI_HAIR")} {_hair + 1}";
                paletteLensText.text = $"{LocalizationManager.Localize("UI_LENS")} {_lens + 1}";
                paletteEarText.text = $"{LocalizationManager.Localize("UI_EAR")} {_ear + 1}";
                paletteTailText.text = $"{LocalizationManager.Localize("UI_TAIL")} {_tail + 1}";
            }

            base.Show();
        }

        public void ChangeHair(int offset)
        {
            var hair = Mathf.Clamp(_hair + offset, 0, 0);
            if (hair == _hair)
                return;

            _hair = hair;
            
            paletteHairText.text = $"{LocalizationManager.Localize("UI_HAIR")} {_hair + 1}";
        }

        public void ChangeLens(int offset)
        {
            var lens = Mathf.Clamp(_lens + offset, 0, 5);
            if (lens == _lens)
                return;
            
            _lens = lens;
            
            paletteLensText.text = $"{LocalizationManager.Localize("UI_LENS")} {_lens + 1}";
            
            var player = Game.Game.instance.Stage.selectedPlayer;
            if (player is null)
                throw new NullReferenceException(nameof(player));
            
            player.UpdateEye(_lens);
        }

        public void ChangeEar(int offset)
        {
            var ear = Mathf.Clamp(_ear + offset, 0, 9);
            if (ear == _ear)
                return;
            
            _ear = ear;
            
            paletteEarText.text = $"{LocalizationManager.Localize("UI_EAR")} {_ear + 1}";
            
            var player = Game.Game.instance.Stage.selectedPlayer;
            if (player is null)
                throw new NullReferenceException(nameof(player));
            
            player.UpdateEar(_ear);
        }

        public void ChangeTail(int offset)
        {
            var tail = Mathf.Clamp(_tail + offset, 0, 9);
            if (tail == _tail)
                return;
            
            _tail = tail;
            
            paletteTailText.text = $"{LocalizationManager.Localize("UI_TAIL")} {_tail + 1}";
            
            var player = Game.Game.instance.Stage.selectedPlayer;
            if (player is null)
                throw new NullReferenceException(nameof(player));
            
            player.UpdateTail(_tail);
        }

        public void BackClick()
        {
            BackToLogin();
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
