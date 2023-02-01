using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Libplanet;
using Nekoyume.Battle;
using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

namespace Nekoyume.UI
{
    using UniRx;

    public class FriendInfoPopup : PopupWidget
    {
        private const string NicknameTextFormat = "<color=#B38271>Lv.{0}</color=> {1}";

        [SerializeField]
        private TextMeshProUGUI nicknameText;

        [SerializeField]
        private Transform titleSocket;

        [SerializeField]
        private TextMeshProUGUI cpText;

        [SerializeField]
        private EquipmentSlots costumeSlots;

        [SerializeField]
        private EquipmentSlots equipmentSlots;

        [SerializeField]
        private RuneSlots runeSlots;

        [SerializeField]
        private AvatarStats avatarStats = null;

        [SerializeField]
        private CategoryTabButton adventureButton;

        [SerializeField]
        private CategoryTabButton arenaButton;

        [SerializeField]
        private CategoryTabButton raidButton;

        private GameObject _cachedCharacterTitle;
        private AvatarState _avatarState;
        private readonly ToggleGroup _toggleGroup = new();
        private readonly Dictionary<BattleType, List<Equipment>> _equipments = new();
        private readonly Dictionary<BattleType, List<Costume>> _costumes = new();
        private readonly Dictionary<BattleType, RuneSlotState> _runes = new();
        private readonly List<RuneState> _runeStates = new();


        protected override void Awake()
        {
            _toggleGroup.RegisterToggleable(adventureButton);
            _toggleGroup.RegisterToggleable(arenaButton);
            _toggleGroup.RegisterToggleable(raidButton);

            adventureButton.OnClick
                .Subscribe(b =>
                {
                    OnClickPresetTab(b, BattleType.Adventure);
                })
                .AddTo(gameObject);
            arenaButton.OnClick
                .Subscribe(b =>
                {
                    OnClickPresetTab(b, BattleType.Arena);
                })
                .AddTo(gameObject);
            raidButton.OnClick
                .Subscribe(b =>
                {
                    OnClickPresetTab(b, BattleType.Raid);
                })
                .AddTo(gameObject);

            base.Awake();
        }

        private void OnClickPresetTab(
            IToggleable toggle,
            BattleType battleType)
        {
            _toggleGroup.SetToggledOffAll();
            toggle.SetToggledOn();

            Game.Game.instance.Lobby.FriendCharacter.Set(
                _avatarState,
                _costumes[battleType],
                _equipments[battleType]);

            UpdateCp(_avatarState, battleType);
            UpdateName(_avatarState);
            UpdateTitle(battleType);
            UpdateSlotView(_avatarState, battleType);
            UpdateStatViews(_avatarState, battleType);
        }

        public async UniTaskVoid ShowAsync(
            AvatarState avatarState,
            BattleType battleType,
            bool ignoreShowAnimation = false)
        {
            _avatarState = avatarState;
            var (itemSlotStates, runeSlotStates) = await avatarState.GetSlotStatesAsync();
            var runeStates = await avatarState.GetRuneStatesAsync();
            SetItems(avatarState, itemSlotStates, runeSlotStates, runeStates);

            base.Show(ignoreShowAnimation);
            switch (battleType)
            {
                case BattleType.Adventure:
                    OnClickPresetTab(adventureButton, battleType);
                    break;
                case BattleType.Arena:
                    OnClickPresetTab(arenaButton, battleType);
                    break;
                case BattleType.Raid:
                    OnClickPresetTab(raidButton, battleType);
                    break;
            }
        }

        private void SetItems(
            AvatarState avatarState,
            List<ItemSlotState> itemSlotStates,
            List<RuneSlotState> runeSlotStates,
            List<RuneState> runeStates)
        {
            _equipments.Clear();
            _costumes.Clear();
            _equipments.Add(BattleType.Adventure, new List<Equipment>());
            _equipments.Add(BattleType.Arena, new List<Equipment>());
            _equipments.Add(BattleType.Raid, new List<Equipment>());
            _costumes.Add(BattleType.Adventure, new List<Costume>());
            _costumes.Add(BattleType.Arena, new List<Costume>());
            _costumes.Add(BattleType.Raid, new List<Costume>());
            foreach (var state in itemSlotStates)
            {
                var equipments = state.Equipments
                    .Select(guid =>
                        avatarState.inventory.Equipments.FirstOrDefault(x => x.ItemId == guid))
                    .Where(item => item != null).ToList();
                _equipments[state.BattleType] = equipments;

                var costumes = state.Costumes
                    .Select(guid =>
                        avatarState.inventory.Costumes.FirstOrDefault(x => x.ItemId == guid))
                    .Where(item => item != null).ToList();
                _costumes[state.BattleType] = costumes;
            }

            _runes.Clear();
            _runes.Add(BattleType.Adventure, new RuneSlotState(BattleType.Adventure));
            _runes.Add(BattleType.Arena, new RuneSlotState(BattleType.Arena));
            _runes.Add(BattleType.Raid, new RuneSlotState(BattleType.Raid));
            foreach (var state in runeSlotStates)
            {
                _runes[state.BattleType] = state;
            }

            _runeStates.Clear();
            _runeStates.AddRange(runeStates);
        }

        private void UpdateCp(AvatarState avatarState, BattleType battleType)
        {
            var level = avatarState.level;
            var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
            if (!characterSheet.TryGetValue(avatarState.characterId, out var row))
            {
                throw new SheetRowNotFoundException("CharacterSheet", avatarState.characterId);
            }

            var equippedRuneStates = new List<RuneState>();
            foreach (var slot in _runes[battleType].GetRuneSlot())
            {
                if (!slot.RuneId.HasValue)
                {
                    continue;
                }

                var runeState = _runeStates.FirstOrDefault(x => x.RuneId == slot.RuneId);
                if (runeState != null)
                {
                    equippedRuneStates.Add(runeState);
                }
            }

            var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            var equipments = _equipments[battleType];
            var costumes = _costumes[battleType];
            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            var runeOptions = Util.GetRuneOptions(equippedRuneStates, runeOptionSheet);
            var cp = CPHelper.TotalCP(equipments, costumes, runeOptions, level, row, costumeSheet);
            cpText.text = $"{cp}";
        }

        private void UpdateName(AvatarState avatarState)
        {
            nicknameText.text = string.Format(
                NicknameTextFormat,
                avatarState.level,
                avatarState.NameWithHash);
        }

        private void UpdateTitle(BattleType battleType)
        {
            Destroy(_cachedCharacterTitle);
            var costumes = _costumes[battleType];
            var title = costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.Title);
            if (title == null)
            {
                return;
            }

            var clone = ResourcesHelper.GetCharacterTitle(title.Grade,
                title.GetLocalizedNonColoredName(false));
            _cachedCharacterTitle = Instantiate(clone, titleSocket);
        }

        private void UpdateSlotView(AvatarState avatarState, BattleType battleType)
        {
            var level = avatarState.level;
            var equipments = _equipments[battleType];
            var costumes = _costumes[battleType];
            var runeSlot = _runes[battleType].GetRuneSlot();
            costumeSlots.SetPlayerCostumes(level, costumes, ShowTooltip, null);
            equipmentSlots.SetPlayerEquipments(level, equipments, ShowTooltip, null);
            runeSlots.Set(runeSlot, _runeStates,ShowRuneTooltip);
        }

        private void UpdateStatViews(AvatarState avatarState, BattleType battleType)
        {
            var equipments = _equipments[battleType];
            var costumes = _costumes[battleType];
            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            var runeStates = _runeStates;
            var equipmentSetEffectSheet = Game.Game.instance.TableSheets.EquipmentItemSetEffectSheet;
            var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
            var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            if (!characterSheet.TryGetValue(avatarState.characterId, out var row))
            {
                return;
            }
            var characterStats = new CharacterStats(row, avatarState.level);
            characterStats.SetAll(
                avatarState.level,
                equipments,
                costumes,
                null,
                equipmentSetEffectSheet,
                costumeSheet);

            foreach (var runeState in runeStates)
            {
                if (!runeOptionSheet.TryGetValue(runeState.RuneId, out var statRow) ||
                    !statRow.LevelOptionMap.TryGetValue(runeState.Level, out var statInfo))
                {
                    continue;
                }

                var statModifiers = new List<StatModifier>();
                statModifiers.AddRange(
                    statInfo.Stats.Select(x =>
                        new StatModifier(
                            x.statMap.StatType,
                            x.operationType,
                            x.statMap.ValueAsInt)));

                characterStats.AddOption(statModifiers);
                characterStats.EqualizeCurrentHPWithHP();
            }

            avatarStats.SetData(characterStats);
        }

        private static void ShowTooltip(EquipmentSlot slot)
        {
            if (slot.Item == null)
            {
                return;
            }

            var item = new InventoryItem(slot.Item, 1, false, true);
            var tooltip = ItemTooltip.Find(item.ItemBase.ItemType);
            tooltip.Show(item, string.Empty, false, null);
        }

        private void ShowRuneTooltip(RuneSlotView slot)
        {
            if (!slot.RuneSlot.RuneId.HasValue)
            {
                return;
            }

            var runeState = _runeStates.FirstOrDefault(x => x.RuneId == slot.RuneSlot.RuneId.Value);
            if (runeState == null)
            {
                return;
            }

            Find<RuneTooltip>().ShowForDisplay(runeState);
        }
    }
}
