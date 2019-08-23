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
        public Text textHp;
        public Text textExp;
        public Slider hpBar;
        public Slider expBar;
        public Text levelInfo;
        public Text nameInfo;
        public RectTransform content;
        public GameObject contentPivot;
        public GameObject profileImage;
        public GameObject statusGrid;
        public GameObject statusRow;
        public GameObject optionGrid;
        public GameObject optionRow;
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
                Find<Alert>().Show(
                    LocalizationManager.Localize("UI_ERROR"),
                    LocalizationManager.Localize("UI_INVALID_NICKNAME"));
                return;
            }

            Find<GrayLoadingScreen>()?.Show();

            ActionManager.instance
                .CreateAvatar(AvatarManager.CreateAvatarAddress(), _selectedIndex, nickName)
                .Subscribe(eval =>
                {
                    var avatarState = AvatarManager.SetIndex(_selectedIndex);
                    OnDidAvatarStateLoaded(avatarState);
                    Find<GrayLoadingScreen>()?.Close();
                });
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
            palette.SetActive(_isCreateMode);

            // 프로필 사진의 용도가 정리되지 않아서 주석 처리함.
            // profileImage.SetActive(!isCreateMode);
            btnLogin.SetActive(!_isCreateMode);
            optionGrid.SetActive(!_isCreateMode);
            
            SetInformation(player);

            Show();
        }

        private void SetInformation(Player player)
        {
            var level = player.level;
            levelInfo.text = $"LV. {level}";
            
            var hp = player.currentHP;
            var hpMax = player.currentHP;
            var expNeed = player.expNeed;
            var levelExp = player.expMax - expNeed;
            var currentExp = player.exp - levelExp;

            //hp, exp
            textHp.text = $"{hp}/{hpMax}";
            textExp.text = $"{currentExp} / {expNeed}";

            //percentage
            var hpPercentage = hp / (float) hpMax;
            var expPercentage = (float) currentExp / expNeed;

            foreach (
                var tuple in new[]
                {
                    new Tuple<Slider, float>(hpBar, hpPercentage),
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

            // status info
            var fields = player.GetType().GetFields();
            foreach (var field in fields)
            {
                if (field.IsDefined(typeof(InformationFieldAttribute), true))
                {
                    GameObject row = Instantiate(statusRow, statusGrid.transform);
                    var info = row.GetComponent<StatusInfo>();
                    info.Set(field.Name, field.GetValue(player), decimal.ToSingle(player.GetAdditionalStatus(field.Name)));
                }
            }

            //option info
            foreach (var option in player.GetOptions())
            {
                GameObject row = Instantiate(optionRow, optionGrid.transform);
                var text = row.GetComponent<Text>();
                text.text = option;
                row.SetActive(true);
            }
        }

        public override void Show()
        {
            base.Show();
            contentPivot.SetActive(!_isCreateMode);
        }

        public override void Close()
        {
            Clear();
            base.Close();
        }

        private void Clear()
        {
            foreach (Transform child in statusGrid.transform)
            {
                Destroy(child.gameObject);
            }

            foreach (Transform child in optionGrid.transform)
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
