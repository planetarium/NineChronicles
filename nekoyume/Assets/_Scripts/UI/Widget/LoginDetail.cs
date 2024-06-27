using System;
using Nekoyume.State;
using Nekoyume.Game.Controller;
using Nekoyume.Model;
using UnityEngine;
using System.Text.RegularExpressions;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine.UI;
using Nekoyume.Model.State;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.EnumType;

namespace Nekoyume.UI
{
    using Nekoyume.Model.Stat;
    using UniRx;

    public class LoginDetail : Widget
    {
        public GameObject btnLogin;
        public GameObject btnCreate;
        public TextMeshProUGUI levelAndNameInfo;

        public GameObject jobInfoContainer;
        public TextMeshProUGUI jobDescriptionText;
        public GameObject statusContainer;
        public DetailedStatView[] statusRows;
        public LoginDetailCostume loginDetailCostume;

        public Button backButton;
        public TextMeshProUGUI backButtonText;

        private readonly HashSet<StatType> visibleStats = new()
        {
            StatType.HP,
            StatType.ATK,
            StatType.DEF,
            StatType.CRI,
            StatType.HIT,
            StatType.SPD
        };

        private int _selectedIndex;
        private bool _isCreateMode;

        protected override void Awake()
        {
            base.Awake();

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
            AudioController.PlayClick();
            Analyzer.Instance.Track("Unity/Create Click");

            var evt = new AirbridgeEvent("Create_Avatar_Click");
            AirbridgeUnity.TrackEvent(evt);

            var inputBox = Find<InputBoxPopup>();
            inputBox.CloseCallback = result =>
            {
                if (result == ConfirmResult.Yes)
                {
                    CreateAndLogin(inputBox.text);
                }
            };
            inputBox.Show("UI_INPUT_NAME", "UI_NICKNAME_CONDITION");
        }

        private void CreateAndLogin(string nickName)
        {
            if (!Regex.IsMatch(nickName, GameConfig.AvatarNickNamePattern))
            {
                Find<Alert>().Show("UI_ERROR", "UI_NICKNAME_CONDITION");
                return;
            }

            Analyzer.Instance.Track("Unity/Choose Nickname");

            var evt = new AirbridgeEvent("Choose_Avatar_Nickname");
            AirbridgeUnity.TrackEvent(evt);

            Find<LoadingScreen>().Show(
                LoadingScreen.LoadingType.Entering,
                L10nManager.Localize("UI_IN_MINING_A_BLOCK"));
            var (earIndex, tailIndex, hairIndex, eyeIndex) = loginDetailCostume.GetCostumeId();
            Game.Game.instance.ActionManager
                .CreateAvatar(_selectedIndex, nickName, hairIndex, eyeIndex, earIndex, tailIndex)
                .DoOnError(e =>
                {
                    Game.Game.PopupError(e).Forget();
                    Find<LoadingScreen>().Close();
                })
                .Subscribe();
        }

        public void OnRenderCreateAvatar()
        {
            var avatarState = States.Instance.CurrentAvatarState;
            StartCoroutine(CreateAndLoginAnimation(avatarState));
        }

        private IEnumerator CreateAndLoginAnimation(AvatarState state)
        {
            var loadingScreen = Find<LoadingScreen>();
            if (loadingScreen is null)
            {
                yield break;
            }

            loadingScreen.Close();
            yield return new WaitUntil(() => loadingScreen.IsCloseAnimationCompleted);
            OnDidAvatarStateLoaded(state);
        }

        public async void LoginClick()
        {
            AudioController.PlayClick();
            btnLogin.SetActive(false);
            var loadingScreen = Find<LoadingScreen>();
            loadingScreen.Show(
                LoadingScreen.LoadingType.Entering, L10nManager.Localize("UI_IN_MINING_A_BLOCK"));
            await RxProps.SelectAvatarAsync(_selectedIndex, Game.Game.instance.Agent.BlockTipStateRootHash);
            loadingScreen.Close();
            OnDidAvatarStateLoaded(States.Instance.CurrentAvatarState);
        }

        public void BackToLogin()
        {
            Close();
            Game.Event.OnNestEnter.Invoke();
            var login = Find<Login>();
            login.Show();
        }

        private async void Init(int index)
        {
            _selectedIndex = index;
            Player player;
            _isCreateMode = !States.Instance.AvatarStates.ContainsKey(index);
            TableSheets tableSheets = Game.Game.instance.TableSheets;

            if (_isCreateMode)
            {
                player = new Player(1, tableSheets.CharacterSheet, tableSheets.CharacterLevelSheet,
                    tableSheets.EquipmentItemSetEffectSheet);
            }
            else
            {
                var loadingScreen = Find<LoadingScreen>();
                loadingScreen.Show(
                    LoadingScreen.LoadingType.JustModule,
                    L10nManager.Localize("UI_LOADING_BOOTSTRAP_START"));
                await States.Instance.SelectAvatarAsync(_selectedIndex);
                Game.Event.OnUpdateAddresses.Invoke();
                loadingScreen.Close();
                player = new Player(
                    States.Instance.CurrentAvatarState,
                    tableSheets.CharacterSheet,
                    tableSheets.CharacterLevelSheet,
                    tableSheets.EquipmentItemSetEffectSheet
                );

                var runeStates = States.Instance.GetEquippedRuneStates(BattleType.Adventure);

                var allRuneState = States.Instance.AllRuneState;
                var runeListSheet = tableSheets.RuneListSheet;
                var runeLevelBonusSheet = tableSheets.RuneLevelBonusSheet;
                var runeLevelBonus = RuneHelper.CalculateRuneLevelBonus(
                    allRuneState, runeListSheet, runeLevelBonusSheet);

                var costumeStatSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
                var collectionState = States.Instance.CollectionState;
                var collectionSheet = Game.Game.instance.TableSheets.CollectionSheet;
                player.ConfigureStats(
                    costumeStatSheet,
                    runeStates,
                    tableSheets.RuneOptionSheet,
                    runeLevelBonus,
                    tableSheets.SkillSheet,
                    collectionState.GetEffects(collectionSheet));
            }

            // create new or login
            btnCreate.SetActive(_isCreateMode);
            loginDetailCostume.SetActive(_isCreateMode);

            // 프로필 사진의 용도가 정리되지 않아서 주석 처리함.
            // profileImage.SetActive(!isCreateMode);
            btnLogin.SetActive(!_isCreateMode);
            jobInfoContainer.SetActive(!_isCreateMode);
            levelAndNameInfo.gameObject.SetActive(!_isCreateMode);
            statusContainer.SetActive(!_isCreateMode);
            if (!_isCreateMode)
            {
                var level = player.Level;
                var name = States.Instance.CurrentAvatarState.NameWithHash;
                levelAndNameInfo.text = $"LV. {level} {name}";
                SetInformation(player);
            }

            backButtonText.text = _isCreateMode ? L10nManager.Localize("UI_CHARACTER_CREATE") : "";

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
            var idx = 0;
            foreach (var (statType, value) in player.Stats.GetStats())
            {
                if (!visibleStats.Contains(statType))
                {
                    continue;
                }

                var info = statusRows[idx];
                info.Show(statType, value, 0);
                ++idx;
            }
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            Analyzer.Instance.Track("Unity/CustomizeAvatar/Show");

            var evt = new AirbridgeEvent("Customize_Avatar");
            AirbridgeUnity.TrackEvent(evt);

            base.Show(ignoreShowAnimation);
            if (_isCreateMode)
            {
                loginDetailCostume.Show();
            }
        }

        private void BackClick()
        {
            BackToLogin();
        }

        private void OnDidAvatarStateLoaded(AvatarState avatarState)
        {
            Util.SaveAvatarSlotIndex(_selectedIndex);
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
            Game.Event.OnUpdateAddresses.Invoke();
            Find<Login>()?.Close();
        }
    }
}
