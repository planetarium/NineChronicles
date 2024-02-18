using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;

namespace BalanceTool
{
    public static partial class HackAndSlashCalculator
    {
        public readonly struct PlayData
        {
            public readonly int WorldId;
            public readonly int StageId;
            public readonly int AvatarLevel;
            public readonly (int equipmentId, int level)[] Equipments;
            public readonly (int consumableId, int count)[] Foods;
            public readonly int[] CostumeIds;
            public readonly (int runeSlotIndex, int runeId, int level)[] Runes;
            public readonly int CrystalRandomBuffId;
            public readonly int PlayCount;
            public readonly PlayResult? Result;

            public PlayData(
                int worldId,
                int stageId,
                int avatarLevel,
                (int equipmentId, int level)[] equipments,
                (int consumableId, int count)[] foods,
                int[] costumeIds,
                (int runeSlotIndex, int runeId, int level)[] runes,
                int crystalRandomBuffId,
                int playCount = 1,
                PlayResult? result = null)
            {
                WorldId = worldId;
                StageId = stageId;
                AvatarLevel = avatarLevel;
                Equipments = equipments;
                Foods = foods;
                CostumeIds = costumeIds;
                Runes = runes;
                CrystalRandomBuffId = crystalRandomBuffId;
                PlayCount = playCount;
                Result = result;
            }

            public PlayData WithResult(PlayResult result)
            {
                return new PlayData(
                    WorldId,
                    StageId,
                    AvatarLevel,
                    Equipments,
                    Foods,
                    CostumeIds,
                    Runes,
                    CrystalRandomBuffId,
                    PlayCount,
                    result);
            }
        }

        private const int EquipmentCount = 6;
        private const int FoodCount = 4;
        private const int CostumeCount = 6;
        private const int RuneCount = 4;
        private const int WaveCountDefault = 3;
        private const int RewardCountDefault = 10;

        public static List<PlayData> ConvertToPlayDataList(
            string playDataCsv,
            int globalPlayCount = 0,
            int waveCount = WaveCountDefault)
        {
            using var reader = new StringReader(playDataCsv);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var records = new List<PlayData>();
            csv.Read();
            csv.ReadHeader();
            while (csv.Read())
            {
                var worldId = csv.GetField<int>("world_id");
                var stageId = csv.GetField<int>("stage_id");
                var level = csv.GetField<int>("avatar_level");
                var equipments = Enumerable.Range(0, EquipmentCount)
                    .Select<int, (int equipmentId, int level)>(i => (
                        csv.TryGetField<int>($"equipment_{i:00}_id", out var equipmentId)
                            ? equipmentId
                            : 0,
                        csv.TryGetField<int>($"equipment_{i:00}_level", out var level)
                            ? level
                            : 0))
                    .Where(tuple => tuple.equipmentId > 0)
                    .ToArray();
                var foods = Enumerable.Range(0, FoodCount)
                    .Select<int, (int consumableId, int count)>(i => (
                        csv.TryGetField<int>($"food_{i:00}_id", out var consumableId)
                            ? consumableId
                            : 0,
                        csv.TryGetField<int>($"food_{i:00}_count", out var count)
                            ? count
                            : 0))
                    .Where(tuple => tuple is { consumableId: > 0, count: > 0 })
                    .ToArray();
                var costumeIds = Enumerable.Range(0, CostumeCount)
                    .Select(i => csv.TryGetField<int>($"costume_{i:00}_id", out var costumeId)
                        ? costumeId
                        : 0)
                    .Where(costumeId => costumeId > 0)
                    .ToArray();
                var runes = Enumerable.Range(0, RuneCount)
                    .Select<int, (int runeSlotIndex, int runeId, int level)>(i => (
                        i,
                        csv.TryGetField<int>($"rune_{i:00}_id", out var runeId)
                            ? runeId
                            : 0,
                        csv.TryGetField<int>($"rune_{i:00}_level", out var level)
                            ? level
                            : 0))
                    .Where(tuple => tuple.runeId > 0)
                    .ToArray();
                var crystalRandomBuffId = csv.TryGetField<int>("crystal_random_buff_id", out var buffId)
                    ? buffId
                    : 0;
                var playCount = csv.TryGetField<int>("play_count", out var count)
                    ? count
                    : 0;

                PlayResult? result = null;
                if (csv.TryGetField<int>("wave_01_cleared", out _))
                {
                    var clearedWaves = Enumerable.Range(1, waveCount)
                        .Select(waveNum => (waveNum, clears: csv.GetField<int>($"wave_{waveNum:00}_cleared")))
                        .Where(tuple => tuple.clears > 0)
                        .ToDictionary(
                            tuple => tuple.waveNum,
                            tuple => tuple.clears);
                    var totalRewards = Enumerable.Range(0, RewardCountDefault)
                        .Select<int, (int itemId, int count)>(i => (
                            csv.GetField<int>($"reward_{i:00}_id"),
                            csv.GetField<int>($"reward_{i:00}_count")))
                        .Where(tuple => tuple is { itemId: > 0, count: > 0 })
                        .ToDictionary(
                            tuple => tuple.itemId,
                            tuple => tuple.count);
                    var totalExp = csv.GetField<int>("total_exp");
                    result = new PlayResult(
                        clearedWaves,
                        totalRewards,
                        totalExp);
                }

                records.Add(new PlayData(
                    worldId,
                    stageId,
                    level,
                    equipments,
                    foods,
                    costumeIds,
                    runes,
                    crystalRandomBuffId,
                    globalPlayCount > 0
                        ? globalPlayCount
                        : playCount,
                    result));
            }

            return records;
        }

        public static string ConvertToCsv(IEnumerable<PlayData> playDataList)
        {
            using var writer = new StringWriter();
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            csv.WriteField("world_id");
            csv.WriteField("stage_id");
            csv.WriteField("avatar_level");
            for (var i = 0; i < EquipmentCount; i++)
            {
                csv.WriteField($"equipment_{i:00}_id");
                csv.WriteField($"equipment_{i:00}_level");
            }

            for (var i = 0; i < FoodCount; i++)
            {
                csv.WriteField($"food_{i:00}_id");
                csv.WriteField($"food_{i:00}_count");
            }

            for (var i = 0; i < CostumeCount; i++)
            {
                csv.WriteField($"costume_{i:00}_id");
            }

            for (var i = 0; i < RuneCount; i++)
            {
                csv.WriteField($"rune_{i:00}_id");
                csv.WriteField($"rune_{i:00}_level");
            }

            csv.WriteField("crystal_random_buff_id");
            csv.WriteField("play_count");

            for (var i = 1; i <= WaveCountDefault; i++)
            {
                csv.WriteField($"wave_{i:00}_cleared");
            }

            for (var i = 0; i < RewardCountDefault; i++)
            {
                csv.WriteField($"reward_{i:00}_id");
                csv.WriteField($"reward_{i:00}_count");
            }

            csv.WriteField("total_exp");
            csv.NextRecord();

            foreach (var playData in playDataList)
            {
                csv.WriteField(playData.WorldId);
                csv.WriteField(playData.StageId);
                csv.WriteField(playData.AvatarLevel);
                for (var i = 0; i < EquipmentCount; i++)
                {
                    if (i < playData.Equipments.Length)
                    {
                        var (equipmentId, level) = playData.Equipments[i];
                        csv.WriteField(equipmentId);
                        csv.WriteField(level);
                    }
                    else
                    {
                        csv.WriteField(0);
                        csv.WriteField(0);
                    }
                }

                for (var i = 0; i < FoodCount; i++)
                {
                    if (i < playData.Foods.Length)
                    {
                        var (consumableId, count) = playData.Foods[i];
                        csv.WriteField(consumableId);
                        csv.WriteField(count);
                    }
                    else
                    {
                        csv.WriteField(0);
                        csv.WriteField(0);
                    }
                }

                for (var i = 0; i < CostumeCount; i++)
                {
                    if (i < playData.CostumeIds.Length)
                    {
                        csv.WriteField(playData.CostumeIds[i]);
                    }
                    else
                    {
                        csv.WriteField(0);
                    }
                }

                for (var i = 0; i < RuneCount; i++)
                {
                    if (i < playData.Runes.Length)
                    {
                        var (_, runeId, level) = playData.Runes[i];
                        csv.WriteField(runeId);
                        csv.WriteField(level);
                    }
                    else
                    {
                        csv.WriteField(0);
                        csv.WriteField(0);
                    }
                }

                csv.WriteField(playData.CrystalRandomBuffId);
                csv.WriteField(playData.PlayCount);

                if (playData.Result.HasValue)
                {
                    var result = playData.Result.Value;
                    for (var i = 1; i <= WaveCountDefault; i++)
                    {
                        csv.WriteField(result.ClearedWaves.TryGetValue(i, out var value)
                            ? value
                            : 0);
                    }

                    var rewardIds = result.TotalRewards.Keys.ToArray();
                    for (var i = 0; i < RewardCountDefault; i++)
                    {
                        if (i < rewardIds.Length)
                        {
                            csv.WriteField(rewardIds[i]);
                            csv.WriteField(result.TotalRewards[rewardIds[i]]);
                        }
                        else
                        {
                            csv.WriteField(0);
                            csv.WriteField(0);
                        }
                    }

                    csv.WriteField(result.TotalExp);
                }
                else
                {
                    for (var i = 1; i <= WaveCountDefault; i++)
                    {
                        csv.WriteField(0);
                    }

                    for (var i = 0; i < RewardCountDefault; i++)
                    {
                        csv.WriteField(0);
                        csv.WriteField(0);
                    }

                    csv.WriteField(0);
                }

                csv.NextRecord();
            }

            return writer.ToString();
        }
    }
}
