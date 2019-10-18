using System;
using Assets.SimpleLocalization;
using DG.Tweening;
using Nekoyume.BlockChain;
using Nekoyume.State;
using Nekoyume.Game.Controller;
using Nekoyume.Model;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

namespace Nekoyume.UI
{
    public class LoginDetail : Widget
    {
        public GameObject btnLogin;
        public Text btnLoginText;
        public GameObject btnCreate;
        public Text btnCreateText;
        public InputField nameField;
        public Text namePlaceHolder;
        public Text textExp;
        public Slider expBar;
        public Text levelInfo;
        public Text nameInfo;
        public RectTransform content;
        public GameObject profileImage;
        public GameObject statusGrid;
        public GameObject statusRow;
        public GameObject palette;
        public Text paletteHairText;
        public Text paletteLensText;
        public Text paletteTopText;

        private int _selectedIndex;
        private bool _isCreateMode;

        private Color _namePlaceHolderOriginColor;
        private Color _namePlaceHolderFocusedColor;

        protected override void Awake()
        {
            base.Awake();

            btnCreateText.text = LocalizationManager.Localize("UI_CREATE_CHARACTER_CONFIRM");
            btnLoginText.text = LocalizationManager.Localize("UI_GAME_START");
            namePlaceHolder.text = LocalizationManager.Localize("UI_INPUT_NAME");
            paletteHairText.text = LocalizationManager.Localize("UI_HAIR");
            paletteLensText.text = LocalizationManager.Localize("UI_LENS");
            paletteTopText.text = LocalizationManager.Localize("UI_ETC");

            nameField.gameObject.SetActive(false);
            Game.Event.OnLoginDetail.AddListener(Init);
            
            _namePlaceHolderOriginColor = namePlaceHolder.color;
            _namePlaceHolderFocusedColor = namePlaceHolder.color;
            _namePlaceHolderFocusedColor.a = 0.3f;
        }

        private void Update()
        {
            if (nameField.isFocused)
            {
                namePlaceHolder.color = _namePlaceHolderFocusedColor;
            }
            else
            {
                namePlaceHolder.color = _namePlaceHolderOriginColor;
            }
        }

        public void CreateClick()
        {
            var nickName = nameField.text;
            if (!Regex.IsMatch(nickName, GameConfig.AvatarNickNamePattern))
            {
                Find<Alert>().Show("UI_ERROR", "UI_INVALID_NICKNAME");
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
            nameField.gameObject.SetActive(false);
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
            _isCreateMode = !States.Instance.avatarStates.ContainsKey(index);
            if (_isCreateMode)
            {
                player = new Player(1);
                nameField.text = "";
                nameInfo.text = "";
            }
            else
            {
                States.Instance.currentAvatarState.Value = States.Instance.avatarStates[_selectedIndex];
                player = new Player(States.Instance.currentAvatarState.Value);
                nameInfo.text = States.Instance.currentAvatarState.Value.name;
            }
            
            // create new or login
            nameField.gameObject.SetActive(_isCreateMode);
            btnCreate.SetActive(_isCreateMode);

            // 프로필 사진의 용도가 정리되지 않아서 주석 처리함.
            // profileImage.SetActive(!isCreateMode);
            btnLogin.SetActive(!_isCreateMode);
            
            SetInformation(player);

            Show();
        }

        private void SetInformation(Player player)
        {
            var level = player.Level;
            levelInfo.text = $"LV. {level}";
            
            var expNeed = player.Exp.Need;
            var levelExp = player.Exp.Max - expNeed;
            var currentExp = player.Exp.Current - levelExp;

            //hp, exp
            textExp.text = $"{currentExp} / {expNeed}";

            //percentage
            var expPercentage = (float) currentExp / expNeed;

            foreach (
                var tuple in new[]
                {
                    new Tuple<Slider, float>(expBar, expPercentage)
                }
            )
            {
                var slide = tuple.Item1;
                var percentage = tuple.Item2;
                slide.fillRect.gameObject.SetActive(percentage > 0.0f);
                percentage = Mathf.Min(Mathf.Max(percentage, 0.1f), 1.0f);
                slide.value = 0.0f;
                slide.DOValue(percentage, 2.0f).SetEase(Ease.OutCubic);
            }

            var tuples = player.GetStatTuples();
            foreach(var (statType, value, additionalValue) in tuples)
            {
                var go = Instantiate(statusRow, statusGrid.transform);
                var info = go.GetComponent<StatusInfo>();
                info.Set(statType, value, additionalValue);
            }
        }

        public override void Show()
        {
            base.Show();
            nameField.Select();
            nameField.ActivateInputField();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Clear();
            base.Close(ignoreCloseAnimation);
        }

        private void Clear()
        {
            foreach (Transform child in statusGrid.transform)
            {
                Destroy(child.gameObject);
            }
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
