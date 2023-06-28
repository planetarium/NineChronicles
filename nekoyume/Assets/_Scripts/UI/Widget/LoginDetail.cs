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
using Lib9c.Renderers;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;

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
            Find<GrayLoadingScreen>().Show();

            var (hairIndex, eyeIndex, earIndex, tailIndex) = loginDetailCostume.GetCostumeId();
            Game.Game.instance.ActionManager
                .CreateAvatar(_selectedIndex, nickName, hairIndex, eyeIndex, earIndex, tailIndex)
                .DoOnError(e =>
                {
                    Game.Game.PopupError(e).Forget();
                    Find<GrayLoadingScreen>().Close();
                })
                .Subscribe();
        }

        public void OnRenderCreateAvatar(ActionEvaluation<CreateAvatar> eval)
        {
            if (eval.Exception is { })
            {
                // NOTE: If eval has an exception then
                // UIs will handled in other places.
                return;
            }

            var avatarState = States.Instance.CurrentAvatarState;
            StartCoroutine(CreateAndLoginAnimation(avatarState));
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

        public async void LoginClick()
        {
            AudioController.PlayClick();
            btnLogin.SetActive(false);
            var loadingScreen = Find<GrayLoadingScreen>();
            loadingScreen.Show();
            await RxProps.SelectAvatarAsync(_selectedIndex);
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
                var loadingScreen = Find<DimmedLoadingScreen>();
                loadingScreen.Show(L10nManager.Localize("UI_LOADING_BOOTSTRAP_START"));
                await States.Instance.SelectAvatarAsync(_selectedIndex);
                Game.Event.OnUpdateAddresses.Invoke();
                loadingScreen.Close();
                player = new Player(
                    States.Instance.CurrentAvatarState,
                    tableSheets.CharacterSheet,
                    tableSheets.CharacterLevelSheet,
                    tableSheets.EquipmentItemSetEffectSheet
                );
                var costumeStatSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
                player.SetCostumeStat(costumeStatSheet);
            }

            // create new or login
            btnCreate.SetActive(_isCreateMode);
            loginDetailCostume.SetActive(_isCreateMode);

            // 프로필 사진의 용도가 정리되지 않아서 주석 처리함.
            // profileImage.SetActive(!isCreateMode);
            btnLogin.SetActive(!_isCreateMode);
            jobInfoContainer.SetActive(!_isCreateMode);
            levelAndNameInfo.gameObject.SetActive(!_isCreateMode);
            if (!_isCreateMode)
            {
                var level = player.Level;
                var name = States.Instance.CurrentAvatarState.NameWithHash;
                levelAndNameInfo.text = $"LV. {level} {name}";
            }

            backButtonText.text = _isCreateMode ? L10nManager.Localize("UI_CHARACTER_CREATE") : "";
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
