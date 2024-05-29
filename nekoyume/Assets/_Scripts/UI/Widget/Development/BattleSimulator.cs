using System;
using System.Collections.Generic;
using System.Linq;
using Lib9c.Renderers;
using Libplanet.Action;
using Nekoyume.Action;
using Nekoyume.Battle;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Game.Battle;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI;
using UnityEngine;
using UnityEngine.UI;
using Skill = Nekoyume.Model.Skill.Skill;

public class BattleSimulator : Widget
{
    public override WidgetType WidgetType => WidgetType.Development;

    [SerializeField] private GameObject content;
    [SerializeField] private InputField world;
    [SerializeField] private InputField stage;
    [SerializeField] private InputField level;
    [SerializeField] private List<InputField> weapon;
    [SerializeField] private List<InputField> armor;
    [SerializeField] private List<InputField> belt;
    [SerializeField] private List<InputField> necklace;
    [SerializeField] private List<InputField> ringR;
    [SerializeField] private List<InputField> ringL;
    [SerializeField] private List<InputField> food;

    [SerializeField] private InputField trials;
    [SerializeField] private Text clear;
    [SerializeField] private Text odds;

    private const string WorldKey = "battle_simulator_world";
    private const string StageKey = "battle_simulator_stage";
    private const string LevelKey = "battle_simulator_level";

    public void Start()
    {
        LoadField();
    }

    public void SaveField()
    {
        PlayerPrefs.SetString(WorldKey, world.text);
        PlayerPrefs.SetString(StageKey, stage.text);
        PlayerPrefs.SetString(LevelKey, level.text);
        SaveEquipField(nameof(weapon), weapon);
        SaveEquipField(nameof(armor), armor);
        SaveEquipField(nameof(belt), belt);
        SaveEquipField(nameof(necklace), necklace);
        SaveEquipField(nameof(ringR), ringR);
        SaveEquipField(nameof(ringL), ringL);
        SaveEquipField(nameof(food), food);
        Debug.Log("[SaveField] SUCCESS");
    }

    public void LoadField()
    {
        if (PlayerPrefs.HasKey(WorldKey))
        {
            world.text = PlayerPrefs.GetString(WorldKey, world.text);
        }

        if (PlayerPrefs.HasKey(StageKey))
        {
            stage.text = PlayerPrefs.GetString(StageKey, stage.text);
        }

        if (PlayerPrefs.HasKey(LevelKey))
        {
            level.text = PlayerPrefs.GetString(LevelKey, level.text);
        }

        LoadEquipField(nameof(weapon), weapon);
        LoadEquipField(nameof(armor), armor);
        LoadEquipField(nameof(belt), belt);
        LoadEquipField(nameof(necklace), necklace);
        LoadEquipField(nameof(ringR), ringR);
        LoadEquipField(nameof(ringL), ringL);
        LoadEquipField(nameof(food), food);
        Debug.Log("[LoadField] SUCCESS");
    }

    private static void SaveEquipField(string name, IReadOnlyList<InputField> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            PlayerPrefs.SetString($"battle_simulator_{name}_{i}", list[i].text);
        }
    }

    private static void LoadEquipField(string name, IReadOnlyList<InputField> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            var key = $"battle_simulator_{name}_{i}";
            if (PlayerPrefs.HasKey(key))
            {
                list[i].text = PlayerPrefs.GetString(key);
            }
        }
    }

    public void OnClickActive()
    {
        content.SetActive(!content.activeSelf);
        SaveField();
    }
    public void OnClickGoStage()
    {
        var log = Simulate();
        BattleRenderer.Instance.PrepareStage(log);
        SaveField();
        content.SetActive(false);
    }

    public void OnClickSimulate()
    {
        var clearCount = 0;
        var count = TextToInt(trials.text);
        for (int i = 0; i < count; i++)
        {
            var log = Simulate();
            if (log.IsClear)
            {
                clearCount++;
            }
        }

        clear.text = clearCount.ToString();
        var ratio = clearCount > 0 ? ((float) clearCount / (float) count) * 100.0f : 0;
        odds.text =  $"{ratio:f2}%";
        Debug.Log($"[Simulate] SUCCESS / count : {count} /clear {clearCount} / odds : {ratio:f2}");
    }

    private BattleLog Simulate()
    {
        var tableSheets = Game.instance.TableSheets;
        var avatarState = new AvatarState(States.Instance.CurrentAvatarState) {level = TextToInt(level.text)};
        var skillSheet = tableSheets.SkillSheet;
        var equipmentItemSheet = tableSheets.EquipmentItemSheet;
        var optionSheet = tableSheets.EquipmentItemOptionSheet;
        var foodItemSheet = tableSheets.ConsumableItemSheet;

        var inventoryEquipments = avatarState.inventory.Items
            .Select(i => i.item)
            .OfType<Equipment>()
            .Where(i => i.equipped).ToList();

        foreach (var equipment in inventoryEquipments)
        {
            equipment.Unequip();
        }

        var collectionState = Game.instance.States.CollectionState;
        var collectionSheet = Game.instance.TableSheets.CollectionSheet;
        var collectionModifiers = new List<StatModifier>();
        foreach (var id in collectionState.Ids)
        {
            if (collectionSheet.TryGetValue(id, out var row))
            {
                collectionModifiers.AddRange(row.StatModifiers);
            }
        }

        var random = new DebugRandom();
        // weapon
        AddCustomEquipment(avatarState: avatarState, random: random,
            skillSheet: skillSheet, equipmentItemSheet: equipmentItemSheet,
            equipmentItemOptionSheet: optionSheet, level: TextToInt(weapon[1].text),
            recipeId: TextToInt(weapon[0].text), GetOptions(weapon[2].text, weapon[3].text));

        // armor
        AddCustomEquipment(avatarState: avatarState, random: random,
            skillSheet: skillSheet, equipmentItemSheet: equipmentItemSheet,
            equipmentItemOptionSheet: optionSheet, level: TextToInt(armor[1].text),
            recipeId: TextToInt(armor[0].text), GetOptions(armor[2].text, armor[3].text));

        // belt
        AddCustomEquipment(avatarState: avatarState, random: random,
            skillSheet: skillSheet, equipmentItemSheet: equipmentItemSheet,
            equipmentItemOptionSheet: optionSheet, level: TextToInt(belt[1].text),
            recipeId: TextToInt(belt[0].text), GetOptions(belt[2].text, belt[3].text));

        // necklace
        AddCustomEquipment(avatarState: avatarState, random: random,
            skillSheet: skillSheet, equipmentItemSheet: equipmentItemSheet,
            equipmentItemOptionSheet: optionSheet, level: TextToInt(necklace[1].text),
            recipeId: TextToInt(necklace[0].text), GetOptions(necklace[2].text, necklace[3].text));

        // ringR
        AddCustomEquipment(avatarState: avatarState, random: random,
            skillSheet: skillSheet, equipmentItemSheet: equipmentItemSheet,
            equipmentItemOptionSheet: optionSheet, level: TextToInt(ringR[1].text),
            recipeId: TextToInt(ringR[0].text), GetOptions(ringR[2].text, ringR[3].text));

        // ringL
        AddCustomEquipment(avatarState: avatarState, random: random,
            skillSheet: skillSheet, equipmentItemSheet: equipmentItemSheet,
            equipmentItemOptionSheet: optionSheet, level: TextToInt(ringL[1].text),
            recipeId: TextToInt(ringL[0].text), GetOptions(ringL[2].text, ringL[3].text));

        // food
        var consumables = AddFood(avatarState, foodItemSheet, random, food);

        var worldId = TextToInt(world.text);
        var stageId = TextToInt(stage.text);
        var simulator = new StageSimulator(
            random,
            avatarState,
            consumables,
            States.Instance.AllRuneState,
            States.Instance.CurrentRuneSlotStates[BattleType.Adventure],
            new List<Skill>(),
            worldId,
            stageId,
            tableSheets.StageSheet[stageId],
            tableSheets.StageWaveSheet[stageId],
            avatarState.worldInformation.IsStageCleared(stageId),
            StageRewardExpHelper.GetExp(avatarState.level, stageId),
            tableSheets.GetStageSimulatorSheets(),
            tableSheets.EnemySkillSheet,
            tableSheets.CostumeStatSheet,
            StageSimulator.GetWaveRewards(random, tableSheets.StageSheet[stageId], tableSheets.MaterialItemSheet),
            collectionModifiers,
            tableSheets.DeBuffLimitSheet,
            tableSheets.BuffLinkSheet,
            logEvent: true,
            States.Instance.GameConfigState.ShatterStrikeMaxDamage);

        simulator.Simulate();

        var log = simulator.Log;
        return log;
    }

    private static int TextToInt(string text)
    {
        return text.Equals(string.Empty) ? 1 : Nekoyume.MathematicsExtensions.ConvertToInt32(text);
    }

    private int[] GetOptions(string first, string second)
    {
        var options = new List<int>();

        if (first.Equals(string.Empty))
        {
            options.Add(TextToInt(first));
        }

        if (second.Equals(string.Empty))
        {
            options.Add(TextToInt(second));
        }

        return options.ToArray();
    }

    private static void AddCustomEquipment(
            AvatarState avatarState,
            IRandom random,
            SkillSheet skillSheet,
            EquipmentItemSheet equipmentItemSheet,
            EquipmentItemOptionSheet equipmentItemOptionSheet,
            int level,
            int recipeId,
            params int[] optionIds
            )
        {
            if (!equipmentItemSheet.TryGetValue(recipeId, out var equipmentRow))
            {
                return;
            }

            var itemId = random.GenerateRandomGuid();
            var equipment = (Equipment)ItemFactory.CreateItemUsable(equipmentRow, itemId, 0, level);
            var optionRows = new List<EquipmentItemOptionSheet.Row>();
            foreach (var optionId in optionIds)
            {
                if (!equipmentItemOptionSheet.TryGetValue(optionId, out var optionRow))
                {
                    continue;
                }
                optionRows.Add(optionRow);
            }

            AddOption(skillSheet, equipment, optionRows, random);
            avatarState.inventory.AddItem(equipment);
            equipment.Equip();
            LocalLayerModifier.AddItem(avatarState.agentAddress, equipment.ItemId, equipment.RequiredBlockIndex,1, false);
        }

        private static HashSet<int> AddOption(
            SkillSheet skillSheet,
            Equipment equipment,
            IEnumerable<EquipmentItemOptionSheet.Row> optionRows,
            IRandom random)
        {
            var optionIds = new HashSet<int>();

            foreach (var optionRow in optionRows.OrderBy(r => r.Id))
            {
                if (optionRow.StatType != StatType.NONE)
                {
                    var statMap = CombinationEquipment5.GetStat(optionRow, random);
                    equipment.StatsMap.AddStatAdditionalValue(statMap.StatType, statMap.TotalValue);
                }
                else
                {
                    var skill = CombinationEquipment5.GetSkill(optionRow, skillSheet, random);
                    if (!(skill is null))
                    {
                        equipment.Skills.Add(skill);
                    }
                }

                optionIds.Add(optionRow.Id);
            }

            return optionIds;
        }

        private static List<Guid> AddFood(AvatarState avatarState,
            ConsumableItemSheet foodItemSheet,
            IRandom random,
            IEnumerable<InputField> list)
        {
            var guids = new List<Guid>();
            foreach (var i in list)
            {
                if (i.text.Equals(string.Empty))
                {
                    continue;
                }
                var row = foodItemSheet.OrderedList.First(x => x.Id == TextToInt(i.text));
                var food= ItemFactory.CreateItemUsable(row,
                    random.GenerateRandomGuid(),
                    default);
                avatarState.inventory.AddItem(food);
                guids.Add(food.ItemId);
            }
            return guids;
        }

        private class DebugRandom : IRandom
        {
            private readonly System.Random _random = new System.Random();

            public int Seed => throw new NotImplementedException();

            public int Next()
            {
                return _random.Next();
            }

            public int Next(int maxValue)
            {
                return _random.Next(maxValue);
            }

            public int Next(int minValue, int maxValue)
            {
                return _random.Next(minValue, maxValue);
            }

            public void NextBytes(byte[] buffer)
            {
                _random.NextBytes(buffer);
            }

            public double NextDouble()
            {
                return _random.NextDouble();
            }
        }
}
