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
using System.Linq;
using Cysharp.Threading.Tasks;
using Lib9c.Renderers;
using mixpanel;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using Nekoyume.UI.Module.WorldBoss;

namespace Nekoyume.UI
{
    using Nekoyume.Model.Stat;
    using UniRx;

    public class LoginDetail : Widget
    {
        public GameObject btnLogin;
        public GameObject btnCreate;
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

        private CostumeItemSheet _costumeItemSheet;

        private readonly Dictionary<ItemSubType, List<int>> _costumes =
            new Dictionary<ItemSubType, List<int>>()
            {
                {ItemSubType.HairCostume, new List<int>()},
                {ItemSubType.EyeCostume, new List<int>()},
                {ItemSubType.EarCostume, new List<int>()},
                {ItemSubType.TailCostume, new List<int>()},
            };

        private readonly Dictionary<ItemSubType, int> _index = new Dictionary<ItemSubType, int>()
        {
            { ItemSubType.HairCostume, 0 },
            { ItemSubType.EyeCostume, 0 },
            { ItemSubType.EarCostume, 0 },
            { ItemSubType.TailCostume, 0 },
        };

        private HashSet<StatType> visibleStats = new()
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

        private const int PartnershipIndex = 10000;

        protected override void Awake()
        {
            base.Awake();

            jobDescriptionText.text = L10nManager.Localize("UI_WARRIOR_DESCRIPTION");

            Game.Event.OnLoginDetail.AddListener(Init);
            _costumeItemSheet = Game.Game.instance.TableSheets.CostumeItemSheet;
            foreach (var costume in _costumes)
            {
                costume.Value.AddRange(GetCostumes(_costumeItemSheet.OrderedList, costume.Key));
            }

            CloseWidget = BackClick;
            SubmitWidget = CreateClick;

            backButton.OnClickAsObservable()
                .ThrottleFirst(new TimeSpan(0, 0, 1))
                .Subscribe(_ => BackClick())
                .AddTo(gameObject);
        }

        private IEnumerable<int> GetCostumes(IEnumerable<CostumeItemSheet.Row> rows, ItemSubType itemSubType)
        {
            var items = rows.Where(x => x.ItemSubType == itemSubType);
            var startIndex = items.First().Id;
            var partnership = new List<int>();
            var origin = new List<int>();
            var result = new List<int>();
            foreach (var item in items)
            {
                var id = item.Id - startIndex;
                if (id < PartnershipIndex)
                {
                    origin.Add(id);
                }
                else
                {
                    partnership.Add(id);
                }
            }
            result.AddRange(partnership);
            result.AddRange(origin);
            return result;
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

            Game.Game.instance.ActionManager
                .CreateAvatar(_selectedIndex, nickName,
                    _costumes[ItemSubType.HairCostume][_index[ItemSubType.HairCostume]],
                    _costumes[ItemSubType.EyeCostume][_index[ItemSubType.EyeCostume]],
                    _costumes[ItemSubType.EarCostume][_index[ItemSubType.EarCostume]],
                    _costumes[ItemSubType.TailCostume][_index[ItemSubType.TailCostume]])
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
                if (!visibleStats.Contains(statType))
                {
                    continue;
                }

                var info = statusRows[idx];
                info.Show(statType, value + additionalValue, 0);
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
                _index[ItemSubType.HairCostume] = 0;
                _index[ItemSubType.EyeCostume] = 0;
                _index[ItemSubType.EarCostume] = 0;
                _index[ItemSubType.TailCostume] = 0;

                var hairIndex = _costumes[ItemSubType.HairCostume][_index[ItemSubType.HairCostume]];
                var eyeIndex = _costumes[ItemSubType.EyeCostume][_index[ItemSubType.EyeCostume]];
                var earIndex = _costumes[ItemSubType.EarCostume][_index[ItemSubType.EarCostume]];
                var tailIndex = _costumes[ItemSubType.TailCostume][_index[ItemSubType.TailCostume]];
                paletteHairText.text = GetPaletteText(ItemSubType.HairCostume, hairIndex);
                paletteLensText.text = GetPaletteText(ItemSubType.EyeCostume, eyeIndex);
                paletteEarText.text = GetPaletteText(ItemSubType.EarCostume, earIndex);
                paletteTailText.text = GetPaletteText(ItemSubType.TailCostume, tailIndex);

                var player = Game.Game.instance.Stage.SelectedPlayer;
                if (player is null)
                {
                    throw new NullReferenceException(nameof(player));
                }

                player.UpdateEarByCustomizeIndex(earIndex);
                player.UpdateTailByCustomizeIndex(tailIndex);
            }

            base.Show(ignoreShowAnimation);
        }

        public void ChangeLens(int offset)
        {
            UpdateCostume(ItemSubType.EyeCostume, offset);
        }
        public void ChangeHair(int offset)
        {
            UpdateCostume(ItemSubType.HairCostume, offset);
        }

        public void ChangeEar(int offset)
        {
            UpdateCostume(ItemSubType.EarCostume, offset);
        }

        public void ChangeTail(int offset)
        {
            UpdateCostume(ItemSubType.TailCostume, offset);
        }

        private void UpdateCostume(ItemSubType itemSubType, int offset)
        {
            var player = Game.Game.instance.Stage.SelectedPlayer;
            if (player is null)
            {
                throw new NullReferenceException(nameof(player));
            }

            var currentIndex = _index[itemSubType] + offset;
            var count = _costumes[itemSubType].Count;

            if (currentIndex < 0)
            {
                currentIndex = count + offset;
            }
            else if (currentIndex >= count)
            {
                currentIndex = 0;
            }

            if (currentIndex == _index[itemSubType])
            {
                return;
            }

            _index[itemSubType] = currentIndex;
            var index = _costumes[itemSubType][_index[itemSubType]];
            switch (itemSubType)
            {
                case ItemSubType.HairCostume:
                    paletteHairText.text = GetPaletteText(ItemSubType.HairCostume, index);
                    player.UpdateHairByCustomizeIndex(index);
                    break;
                case ItemSubType.EyeCostume:
                    paletteLensText.text = GetPaletteText(ItemSubType.EyeCostume, index);
                    player.UpdateEyeByCustomizeIndex(index);
                    break;
                case ItemSubType.EarCostume:
                    paletteEarText.text = GetPaletteText(ItemSubType.EarCostume, index);
                    player.UpdateEarByCustomizeIndex(index);
                    break;
                case ItemSubType.TailCostume:
                    paletteTailText.text = GetPaletteText(ItemSubType.TailCostume, index);
                    player.UpdateTailByCustomizeIndex(index);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(itemSubType), itemSubType, null);
            }
        }

        private string GetPaletteText(ItemSubType itemSubType, int index)
        {
            switch (itemSubType)
            {
                case ItemSubType.HairCostume:
                    return $"{L10nManager.Localize("UI_HAIR")} {index + 1}";
                case ItemSubType.EyeCostume:
                    return $"{L10nManager.Localize("UI_LENS")} {index + 1}";
                case ItemSubType.EarCostume:
                    return index < PartnershipIndex
                        ? $"{L10nManager.Localize("UI_EAR")} {index + 1}"
                        : $"{L10nManager.Localize("UI_EAR_REVOMON")} {index - PartnershipIndex + 1}";
                case ItemSubType.TailCostume:
                    return index < PartnershipIndex
                        ? $"{L10nManager.Localize("UI_TAIL")} {index + 1}"
                        : $"{L10nManager.Localize("UI_TAIL_REVOMON")} {index - PartnershipIndex + 1}";
                default:
                    throw new ArgumentOutOfRangeException(nameof(itemSubType), itemSubType, null);
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
