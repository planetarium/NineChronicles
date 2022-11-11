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
using Nekoyume.Model.Rune;
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

        private GameObject _cachedCharacterTitle;
        private readonly Dictionary<BattleType, List<Equipment>> _equipments = new();
        private readonly Dictionary<BattleType, List<Costume>> _costumes = new();
        private readonly Dictionary<BattleType, RuneSlotState> _runes = new();

        public async UniTaskVoid ShowAsync(AvatarState avatarState, bool ignoreShowAnimation = false)
        {
            var (itemSlotStates, runeSlotStates) = await GetStatesAsync(avatarState);
            SetItems(avatarState, itemSlotStates, runeSlotStates);

            base.Show(ignoreShowAnimation);

            Game.Game.instance.Lobby.FriendCharacter.Set(
                avatarState,
                _costumes[BattleType.Adventure],
                _equipments[BattleType.Adventure]);

            UpdateCp(avatarState, BattleType.Adventure);
            UpdateName(avatarState);
            UpdateTitle(BattleType.Adventure);
            UpdateSlotView(avatarState, BattleType.Adventure);
            UpdateStatViews(avatarState, BattleType.Adventure);
        }

        private void SetItems(
            AvatarState avatarState,
            List<ItemSlotState> itemSlotStates,
            List<RuneSlotState> runeSlotStates)
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
                _runes.Add(state.BattleType, state);
            }
        }

        private void UpdateCp(AvatarState avatarState, BattleType battleType)
        {
            var level = avatarState.level;
            var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
            if (!characterSheet.TryGetValue(avatarState.characterId, out var row))
            {
                throw new SheetRowNotFoundException("CharacterSheet", avatarState.characterId);
            }

            var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            var equipments = _equipments[battleType];
            var costumes = _costumes[battleType];
            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            var runes = _runes[battleType].GetEquippedRuneOptions(runeOptionSheet);
            var cp = CPHelper.TotalCP(equipments, costumes, runes, level, row, costumeSheet);
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
            var costumes = _costumes[battleType];
            Destroy(_cachedCharacterTitle);
            var title = costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.Title);
            if (title == null)
            {
                return;
            }

            var clone = ResourcesHelper.GetCharacterTitle(title.Grade,
                title.GetLocalizedNonColoredName(false));
            _cachedCharacterTitle = Instantiate(clone, titleSocket);
        }

        private async Task<(List<ItemSlotState>, List<RuneSlotState>)> GetStatesAsync(
            AvatarState avatarState)
        {
            var avatarAddress = avatarState.address;

            var itemAddresses = new List<Address>
            {
                ItemSlotState.DeriveAddress(avatarAddress, BattleType.Adventure),
                ItemSlotState.DeriveAddress(avatarAddress, BattleType.Arena),
                ItemSlotState.DeriveAddress(avatarAddress, BattleType.Raid)
            };
            var itemBulk = await Game.Game.instance.Agent.GetStateBulk(itemAddresses);
            var itemStates = new List<ItemSlotState>();
            foreach (var value in itemBulk.Values)
            {
                if (value is List list)
                {
                    itemStates.Add(new ItemSlotState(list));
                }
            }

            var runeAddresses = new List<Address>
            {
                RuneSlotState.DeriveAddress(avatarAddress, BattleType.Adventure),
                RuneSlotState.DeriveAddress(avatarAddress, BattleType.Arena),
                RuneSlotState.DeriveAddress(avatarAddress, BattleType.Raid)
            };
            var runeBulk = await Game.Game.instance.Agent.GetStateBulk(runeAddresses);
            var runeStates = new List<RuneSlotState>();
            foreach (var value in runeBulk.Values)
            {
                if (value is List list)
                {
                    runeStates.Add(new RuneSlotState(list));
                }
            }

            return (itemStates, runeStates);
        }

        private void UpdateSlotView(AvatarState avatarState, BattleType battleType)
        {
            var level = avatarState.level;
            var equipments = _equipments[battleType];
            var costumes = _costumes[battleType];
            // var runeSlot = _runes.ContainsKey(battleType)
            //     ? _runes[battleType].GetRuneSlot()
            //     : new List<RuneSlot>();

            var runeSlot = _runes[battleType].GetRuneSlot();
            costumeSlots.SetPlayerCostumes(level, costumes, ShowTooltip, null);
            equipmentSlots.SetPlayerEquipments(level, equipments, ShowTooltip, null);
            runeSlots.Set(runeSlot, ShowRuneTooltip, null);
        }

        private void UpdateStatViews(AvatarState avatarState, BattleType battleType)
        {
            var equipments = _equipments[battleType];
            var costumes = _costumes[battleType];
            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            // var runeStates = _runes.ContainsKey(battleType)
            //     ? _runes[battleType].GetEquippedRuneStates()
            //     : new List<RuneState>();
            var runeStates = _runes[battleType].GetEquippedRuneStates();
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
            tooltip.Show(item, string.Empty, false, null, target: slot.RectTransform);
        }

        private void ShowRuneTooltip(RuneSlotView slot)
        {
            if (!slot.RuneSlot.IsEquipped(out var runeState))
            {
                return;
            }

            Find<RuneTooltip>().ShowForDisplay(runeState, slot.RectTransform, new float2(-50, 0));
        }
    }
}
