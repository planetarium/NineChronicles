using System;
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
using Nekoyume.Model.State;
using System.Collections;
using mixpanel;
using Nekoyume.Game;
using Nekoyume.L10n;

namespace Nekoyume.UI
{
    using UniRx;

    public class LoginDetail : Widget
    {
        public GameObject btnLogin;
        public GameObject btnCreate;
        public TextMeshProUGUI btnCreateText;
        public TextMeshProUGUI levelAndNameInfo;
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
        public Button backButton;

        private int _selectedIndex;
        private bool _isCreateMode;

        private int _hair;
        private int _lens;
        private int _ear;
        private int _tail;

        public const string RecentlyLoggedInAvatarKey = "RecentlyLoggedInAvatarAddress";
        private const int HairCount = 7;
        private const int LensCount = 6;
        private const int EarCount = 10;
        private const int TailCount = 10;

        protected override void Awake()
        {
            base.Awake();

            btnCreateText.text = L10nManager.Localize("UI_CREATE_CHARACTER_CONFIRM");
            jobDescriptionText.text = L10nManager.Localize("UI_WARRIOR_DESCRIPTION");

            Game.Event.OnLoginDetail.AddListener(Init);

            CloseWidget = BackClick;
            SubmitWidget = CreateClick;

            backButton.OnClickAsObservable()
                .ThrottleFirst(new TimeSpan(0, 0, 1))
                .Subscribe(_ => BackClick())
                .AddTo(gameObject);
        }

        public void CreateClick()
        {
            Mixpanel.Track("Unity/Create Click");
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

            Mixpanel.Track("Unity/Choose Nickname");
            Find<GrayLoadingScreen>().Show();

            Game.Game.instance.ActionManager
                .CreateAvatar(_selectedIndex, nickName, _hair,
                    _lens, _ear, _tail)
                .Subscribe(eval =>
                    {
                        var avatarState = States.Instance.SelectAvatar(_selectedIndex);
                        StartCoroutine(CreateAndLoginAnimation(avatarState));
                        ActionRenderHandler.RenderQuest(avatarState.address,
                            avatarState.questList.completedQuestIds);
                    },
                    e =>
                    {
                        ActionRenderHandler.PopupError(e);
                        Find<GrayLoadingScreen>().Close();
                    });
            AudioController.PlayClick();
        }

        private IEnumerator CreateAndLoginAnimation(AvatarState state)
        {
            var grayLoadingScreen = Find<GrayLoadingScreen>();
            if (grayLoadingScreen is null)
            {
                yield break;
            }

            grayLoadingScreen.Close();
            yield return new WaitUntil(() => grayLoadingScreen.IsCloseAnimationCompleted);
            OnDidAvatarStateLoaded(state);
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
            TableSheets tableSheets = Game.Game.instance.TableSheets;

            if (_isCreateMode)
            {
                player = new Player(1, tableSheets.CharacterSheet, tableSheets.CharacterLevelSheet, tableSheets.EquipmentItemSetEffectSheet);
            }
            else
            {
                States.Instance.SelectAvatar(_selectedIndex);
                player = new Player(
                    States.Instance.CurrentAvatarState,
                    tableSheets.CharacterSheet,
                    tableSheets.CharacterLevelSheet,
                    tableSheets.EquipmentItemSetEffectSheet
                );
                var costumeStatSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
                player.SetCostumeStat(costumeStatSheet);
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
            var idx = 0;
            foreach (var (statType, value, additionalValue) in tuples)
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
            {
                Find<Alert>().Show(
                    "UI_ALERT_NOT_IMPLEMENTED_TITLE",
                    "UI_ALERT_NOT_IMPLEMENTED_CONTENT");
            }
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            warriorButton.gameObject.SetActive(_isCreateMode);
            archerButton.gameObject.SetActive(_isCreateMode);
            mageButton.gameObject.SetActive(_isCreateMode);
            acolyteButton.gameObject.SetActive(_isCreateMode);
            if (_isCreateMode)
            {
                _hair = _lens = _ear = _tail = 0;
                paletteHairText.text = $"{L10nManager.Localize("UI_HAIR")} {_hair + 1}";
                paletteLensText.text = $"{L10nManager.Localize("UI_LENS")} {_lens + 1}";
                paletteEarText.text = $"{L10nManager.Localize("UI_EAR")} {_ear + 1}";
                paletteTailText.text = $"{L10nManager.Localize("UI_TAIL")} {_tail + 1}";
            }

            base.Show(ignoreShowAnimation);
        }

        public void ChangeEar(int offset)
        {
            var ear = _ear + offset;

            if (ear < 0)
            {
                ear = EarCount + offset;
            }
            else if (ear >= EarCount)
            {
                ear = 0;
            }

            if (ear == _ear)
            {
                return;
            }

            _ear = ear;

            paletteEarText.text = $"{L10nManager.Localize("UI_EAR")} {_ear + 1}";

            var player = Game.Game.instance.Stage.selectedPlayer;
            if (player is null)
            {
                throw new NullReferenceException(nameof(player));
            }

            player.UpdateEarByCustomizeIndex(_ear);
        }

        public void ChangeLens(int offset)
        {
            var lens = _lens + offset;

            if (lens < 0)
            {
                lens = LensCount + offset;
            }
            else if (lens >= LensCount)
            {
                lens = 0;
            }

            if (lens == _lens)
            {
                return;
            }

            _lens = lens;

            paletteLensText.text = $"{L10nManager.Localize("UI_LENS")} {_lens + 1}";

            var player = Game.Game.instance.Stage.selectedPlayer;
            if (player is null)
            {
                throw new NullReferenceException(nameof(player));
            }

            player.UpdateEyeByCustomizeIndex(_lens);
        }

        public void ChangeHair(int offset)
        {
            var hair = _hair + offset;

            if (hair < 0)
            {
                hair = HairCount + offset;
            }
            else if (hair >= HairCount)
            {
                hair = 0;
            }

            if (hair == _hair)
            {
                return;
            }

            _hair = hair;

            paletteHairText.text = $"{L10nManager.Localize("UI_HAIR")} {_hair + 1}";

            var player = Game.Game.instance.Stage.selectedPlayer;
            if (player is null)
            {
                throw new NullReferenceException(nameof(player));
            }

            player.UpdateHairByCustomizeIndex(_hair);
        }

        public void ChangeTail(int offset)
        {
            var tail = _tail + offset;

            if (tail < 0)
            {
                tail = TailCount + offset;
            }
            else if (tail >= TailCount)
            {
                tail = 0;
            }

            if (tail == _tail)
            {
                return;
            }

            _tail = tail;

            paletteTailText.text = $"{L10nManager.Localize("UI_TAIL")} {_tail + 1}";

            var player = Game.Game.instance.Stage.selectedPlayer;
            if (player is null)
            {
                throw new NullReferenceException(nameof(player));
            }

            player.UpdateTailByCustomizeIndex(_tail);
        }

        public void BackClick()
        {
            BackToLogin();
        }

        private void OnDidAvatarStateLoaded(AvatarState avatarState)
        {
            PlayerPrefs.SetString(RecentlyLoggedInAvatarKey, avatarState.address.ToString());
            if (_isCreateMode)
            {
                Close();
            }

            EnterRoom();
        }

        private void EnterRoom()
        {
            Close();
            Game.Event.OnRoomEnter.Invoke(false);
            Find<Login>()?.Close();
        }
    }
}
