using System;
using DG.Tweening;
using Nekoyume.Action;
using Nekoyume.State;
using Nekoyume.Game.Controller;
using Nekoyume.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class LoginDetail : Widget
    {
        public GameObject btnLogin;
        public GameObject btnCreate;
        public InputField nameField;
        public Text namePlaceHolder;
        public Text textHp;
        public Text textExp;
        public Slider hpBar;
        public Slider expBar;
        public Text levelInfo;
        public Text nameInfo;
        public GameObject profileImage;
        public GameObject statusGrid;
        public GameObject statusRow;
        public GameObject optionGrid;
        public GameObject optionRow;
        public GameObject palette;
        
        private int _selectedIndex;
        private AvatarState _avatarState;

        private Color _namePlaceHolderOriginColor;
        private Color _namePlaceHolderFocusedColor;

        protected override void Awake()
        {
            base.Awake();

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

        private void OnEnable()
        {
            AvatarManager.DidAvatarStateLoaded += OnDidAvatarStateLoaded;
        }

        private void OnDisable()
        {
            AvatarManager.DidAvatarStateLoaded -= OnDidAvatarStateLoaded;
        }

        public void LoginClick()
        {
            btnLogin.SetActive(false);
            nameField.gameObject.SetActive(false);
            AvatarManager.InitAvatarState(_selectedIndex);
            AvatarManager.SubscribeAvatarUpdates();
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

        public void CreateClick()
        {
            var w = Find<GrayLoadingScreen>();
            if (!ReferenceEquals(w, null))
            {
                w.Show();   
            }
            
            AvatarManager.InitAvatarPrivateKeyAndFilePath(_selectedIndex);
            var nickName = nameField.text;
            ActionManager.instance.CreateNovice(nickName);
            AvatarManager.SubscribeAvatarUpdates();
            AudioController.PlayClick();
        }

        private void Init(int index)
        {
            _selectedIndex = index;
            var isCreateMode = false;
            _avatarState = AvatarManager.AvatarStates[_selectedIndex];
            if (ReferenceEquals(_avatarState, null))
            {
                isCreateMode = true;
                _avatarState = new AvatarState(AddressBook.Avatar.Value);
                nameField.text = "";
            }
            var isSelectMode = !isCreateMode;

            // create new or login
            nameField.gameObject.SetActive(isCreateMode);
            btnCreate.SetActive(isCreateMode);
            palette.SetActive(isCreateMode);

            profileImage.SetActive(isSelectMode);
            btnLogin.SetActive(isSelectMode);
            optionGrid.SetActive(isSelectMode);

            var player = new Player(_avatarState);
            levelInfo.text = $"LV. {player.level}";
            nameInfo.text = $"{_avatarState.name}";

            SetInformation(player);

            Show();
        }

        private void SetInformation(Player player)
        {
            var hp = player.currentHP;
            var hpMax = player.currentHP;
            var exp = player.exp;
            var expMax = player.expMax;

            //hp, exp
            textHp.text = $"{hp}/{hpMax}";
            textExp.text = $"{exp}/{expMax}";

            //percentage
            var hpPercentage = hp / (float) hpMax;
            var expPercentage = player.exp / (float) expMax;

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
                    info.Set(field.Name, field.GetValue(player), player.GetAdditionalStatus(field.Name));
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
            if (palette.activeInHierarchy)
            {
                BackClick();
            }
            else
            {
                Game.Event.OnRoomEnter.Invoke();
                Widget.Find<Login>().Close();
                Close();
            }

            var w = Find<GrayLoadingScreen>();
            if (!ReferenceEquals(w, null))
            {
                w.Close();
            }
        }
    }
}
