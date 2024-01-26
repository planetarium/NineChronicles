using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;

namespace BalanceTool
{
    public static partial class ArenaCalculator
    {
        public readonly struct PlayData
        {
            public readonly int AvatarLevel;
            public readonly (int equipmentId, int level)[] Equipments;
            public readonly int[] CostumeIds;
            public readonly (int runeSlotIndex, int runeId, int level)[] Runes;
            public readonly int EnemyLevel;
            public readonly (int equipmentId, int level)[] EnemyEquipments;
            public readonly int[] EnemyCostumeIds;
            public readonly (int runeSlotIndex, int runeId, int level)[] EnemyRunes;
            public readonly int PlayCount;
            public readonly PlayResult? Result;

            public PlayData(
                int avatarLevel,
                (int equipmentId, int level)[] equipments,
                int[] costumeIds,
                (int runeSlotIndex, int runeId, int level)[] runes,
                int enemyLevel,
                (int equipmentId, int level)[] enemyEquipments,
                int[] enemyCostumeIds,
                (int runeSlotIndex, int runeId, int level)[] enemyRunes,
                int playCount = 1,
                PlayResult? result = null)
            {
                AvatarLevel = avatarLevel;
                Equipments = equipments;
                CostumeIds = costumeIds;
                Runes = runes;
                EnemyLevel = enemyLevel;
                EnemyEquipments = enemyEquipments;
                EnemyCostumeIds = enemyCostumeIds;
                EnemyRunes = enemyRunes;
                PlayCount = playCount;
                Result = result;
            }

            public PlayData WithResult(PlayResult result)
            {
                return new PlayData(
                    AvatarLevel,
                    Equipments,
                    CostumeIds,
                    Runes,
                    EnemyLevel,
                    EnemyEquipments,
                    EnemyCostumeIds,
                    EnemyRunes,
                    PlayCount,
                    result);
            }
        }

        private const int EquipmentCount = 6;
        private const int CostumeCount = 6;
        private const int RuneCount = 4;
        private const int RewardCountDefault = 10;

        public static List<PlayData> ConvertToPlayDataList(
            string playDataCsv,
            int globalPlayCount = 0)
        {
            using var reader = new StringReader(playDataCsv);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var records = new List<PlayData>();
            csv.Read();
            csv.ReadHeader();
            while (csv.Read())
            {
                var avatarLevel = csv.GetField<int>("avatar_level");
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
                var enemyLevel = csv.GetField<int>("enemy_level");
                var enemyEquipments = Enumerable.Range(0, EquipmentCount)
                    .Select<int, (int equipmentId, int level)>(i => (
                        csv.TryGetField<int>($"enemy_equipment_{i:00}_id", out var equipmentId)
                            ? equipmentId
                            : 0,
                        csv.TryGetField<int>($"enemy_equipment_{i:00}_level", out var level)
                            ? level
                            : 0))
                    .Where(tuple => tuple.equipmentId > 0)
                    .ToArray();
                var enemyCostumeIds = Enumerable.Range(0, CostumeCount)
                    .Select(i => csv.TryGetField<int>($"enemy_costume_{i:00}_id", out var costumeId)
                        ? costumeId
                        : 0)
                    .Where(costumeId => costumeId > 0)
                    .ToArray();
                var enemyRunes = Enumerable.Range(0, RuneCount)
                    .Select<int, (int runeSlotIndex, int runeId, int level)>(i => (
                        i,
                        csv.TryGetField<int>($"enemy_rune_{i:00}_id", out var runeId)
                            ? runeId
                            : 0,
                        csv.TryGetField<int>($"enemy_rune_{i:00}_level", out var level)
                            ? level
                            : 0))
                    .Where(tuple => tuple.runeId > 0)
                    .ToArray();
                var playCount = csv.TryGetField<int>("play_count", out var count)
                    ? count
                    : 0;
                PlayResult? result = null;
                if (csv.TryGetField<int>("arena_result_win", out var arenaResultWin))
                {
                    var arenaResultLose = csv.GetField<int>("arena_result_lose");
                    var totalRewards = Enumerable.Range(0, RewardCountDefault)
                        .Select<int, (int itemId, int count)>(i => (
                            csv.GetField<int>($"reward_{i:00}_id"),
                            csv.GetField<int>($"reward_{i:00}_count")))
                        .Where(tuple => tuple is { itemId: > 0, count: > 0 })
                        .ToDictionary(
                            tuple => tuple.itemId,
                            tuple => tuple.count);
                    result = new PlayResult(
                        arenaResultWin,
                        arenaResultLose,
                        totalRewards);
                }

                records.Add(new PlayData(
                    avatarLevel,
                    equipments,
                    costumeIds,
                    runes,
                    enemyLevel,
                    enemyEquipments,
                    enemyCostumeIds,
                    enemyRunes,
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

            csv.WriteField("avatar_level");
            for (var i = 0; i < EquipmentCount; i++)
            {
                csv.WriteField($"equipment_{i:00}_id");
                csv.WriteField($"equipment_{i:00}_level");
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

            csv.WriteField("enemy_level");
            for (var i = 0; i < EquipmentCount; i++)
            {
                csv.WriteField($"enemy_equipment_{i:00}_id");
                csv.WriteField($"enemy_equipment_{i:00}_level");
            }

            for (var i = 0; i < CostumeCount; i++)
            {
                csv.WriteField($"enemy_costume_{i:00}_id");
            }

            for (var i = 0; i < RuneCount; i++)
            {
                csv.WriteField($"enemy_rune_{i:00}_id");
                csv.WriteField($"enemy_rune_{i:00}_level");
            }

            csv.WriteField("play_count");
            csv.WriteField("arena_result_win");
            csv.WriteField("arena_result_lose");

            for (var i = 0; i < RewardCountDefault; i++)
            {
                csv.WriteField($"reward_{i:00}_id");
                csv.WriteField($"reward_{i:00}_count");
            }

            csv.NextRecord();

            foreach (var playData in playDataList)
            {
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

                csv.WriteField(playData.EnemyLevel);
                for (var i = 0; i < EquipmentCount; i++)
                {
                    if (i < playData.EnemyEquipments.Length)
                    {
                        var (equipmentId, level) = playData.EnemyEquipments[i];
                        csv.WriteField(equipmentId);
                        csv.WriteField(level);
                    }
                    else
                    {
                        csv.WriteField(0);
                        csv.WriteField(0);
                    }
                }

                for (var i = 0; i < CostumeCount; i++)
                {
                    if (i < playData.EnemyCostumeIds.Length)
                    {
                        csv.WriteField(playData.EnemyCostumeIds[i]);
                    }
                    else
                    {
                        csv.WriteField(0);
                    }
                }

                for (var i = 0; i < RuneCount; i++)
                {
                    if (i < playData.EnemyRunes.Length)
                    {
                        var (_, runeId, level) = playData.EnemyRunes[i];
                        csv.WriteField(runeId);
                        csv.WriteField(level);
                    }
                    else
                    {
                        csv.WriteField(0);
                        csv.WriteField(0);
                    }
                }

                csv.WriteField(playData.PlayCount);

                if (playData.Result.HasValue)
                {
                    var result = playData.Result.Value;
                    csv.WriteField(result.ArenaResultWin);
                    csv.WriteField(result.ArenaResultLose);

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
                }
                else
                {
                    csv.WriteField(0);
                    csv.WriteField(0);

                    for (var i = 0; i < RewardCountDefault; i++)
                    {
                        csv.WriteField(0);
                        csv.WriteField(0);
                    }
                }

                csv.NextRecord();
            }

            return writer.ToString();
        }
    }
}
