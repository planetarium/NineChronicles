using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.UI.Module.WorldBoss;
using UnityEngine;

namespace Nekoyume.Helper
{
    public static class WorldBossFrontHelper
    {
        private static WorldBossScriptableObject _worldBossData;

        private static WorldBossScriptableObject WorldBossData
        {
            get
            {
                if (_worldBossData == null)
                {
                    _worldBossData = Resources.Load<WorldBossScriptableObject>(
                        "ScriptableObject/UI_WorldBossData");
                }

                return _worldBossData;
            }
        }

        /// <summary>
        /// 보상 룬 범위(뽑는 총량 <paramref name="runeMin"/>~<paramref name="runeMax"/>)에 부스트를 액션과 같은 규칙으로
        /// 적용한 범위. 액션(<c>WorldBossHelper.CalculateReward</c>)은 뽑은 룬을 종류별로 나눈 뒤 종류마다
        /// <c>ApplyRewardBoost</c> 를 적용한다 — ADD 는 뽑힌 종류 수만큼 더해지므로 총량에 한 번만 더하면 실제보다
        /// 적게(음수면 많게) 보인다.
        /// </summary>
        /// <param name="boost">걸린 부스트 행. 없으면 그대로.</param>
        /// <param name="runeMin">뽑는 총량의 최소.</param>
        /// <param name="runeMax">뽑는 총량의 최대.</param>
        /// <param name="bossId">보스 id. 이 보스·등급의 <see cref="RuneWeightSheet"/> 행에서 룬 종류 수를 센다.</param>
        /// <param name="rank">보상 등급.</param>
        /// <returns>
        /// 지급될 수 있는 룬 합의 (최소, 최대). MUL 은 종류별로 내림하므로 실제 합이 끝값보다 조금 작을 수 있다.
        /// </returns>
        public static (int min, int max) ApplyRuneRewardBoost(
            BoostScheduleSheet.Row boost,
            int runeMin,
            int runeMax,
            int bossId,
            int rank)
        {
            if (boost is null || boost.Op != BoostScheduleSheet.Operation.Add)
            {
                return (WorldBossHelper.ApplyRewardBoost(boost, runeMin), WorldBossHelper.ApplyRewardBoost(boost, runeMax));
            }

            // 총 N 개가 k 종류(1 ≤ k ≤ min(종류 수, N))로 나뉘면 합은 Σ max(0, c_i + v) 이다. v ≥ 0 이면 k 가 클수록
            // 크고(N + k·v), v < 0 이면 k 가 클수록 작다(최소 max(0, N + k·v)). 양 끝을 둘 다 보고 고른다.
            var kinds = GetRuneKindCount(bossId, rank);
            var lower = Math.Min(
                AddPerKind(runeMin, 1, boost.Value),
                AddPerKind(runeMin, Math.Min(kinds, runeMin), boost.Value));
            var upper = Math.Max(
                AddPerKind(runeMax, 1, boost.Value),
                AddPerKind(runeMax, Math.Min(kinds, runeMax), boost.Value));
            return (lower, upper);
        }

        /// <summary>
        /// 고정 총량 <paramref name="rune"/> 에 부스트를 적용한 값(등급 보상처럼 범위가 아닌 곳). ADD 는 이 보스·등급의 룬이
        /// 모두 뽑혔다고 보고 종류 수만큼 더한다 — 등급 보상은 뽑는 수가 커서 거의 항상 모든 종류가 나온다.
        /// </summary>
        public static int ApplyRuneRewardBoost(BoostScheduleSheet.Row boost, int rune, int bossId, int rank)
        {
            if (boost is null || boost.Op != BoostScheduleSheet.Operation.Add)
            {
                return WorldBossHelper.ApplyRewardBoost(boost, rune);
            }

            return AddPerKind(rune, Math.Min(GetRuneKindCount(bossId, rank), rune), boost.Value);
        }

        /// <summary>이 보스·등급에서 뽑힐 수 있는 룬 종류 수(가중치 0 은 뽑히지 않는다). 행이 없으면 1.</summary>
        private static int GetRuneKindCount(int bossId, int rank)
        {
            var row = Game.Game.instance?.TableSheets?.RuneWeightSheet?.Values
                .FirstOrDefault(r => r.BossId == bossId && r.Rank == rank);
            var count = row?.RuneInfos?.Where(info => info.Weight > 0).Select(info => info.RuneId).Distinct().Count() ?? 0;
            return Math.Max(count, 1);
        }

        /// <summary>
        /// 총 <paramref name="total"/> 개가 <paramref name="kinds"/> 종류로 나뉘어 종류마다 <paramref name="value"/> 를 더할 때의
        /// 합(위 범위의 끝값). 액션처럼 0 미만은 0, int 범위로 자른다.
        /// </summary>
        private static int AddPerKind(int total, int kinds, decimal value)
        {
            if (total <= 0)
            {
                return total;
            }

            var result = decimal.Floor(total + kinds * value);
            if (result <= 0m)
            {
                return 0;
            }

            return result >= int.MaxValue ? int.MaxValue : (int)result;
        }

        public static bool TryGetGrade(WorldBossGrade grade, bool isSmall, out GameObject prefab)
        {
            var result = WorldBossData.Grades.FirstOrDefault(x => x.grade == grade);
            if (result is null)
            {
                prefab = null;
                return false;
            }

            prefab = isSmall ? result.smallPrefab : result.prefab;
            return true;
        }

        public static bool TryGetBossData(int bossId, out WorldBossScriptableObject.MonsterData data)
        {
            var result = WorldBossData.Monsters.FirstOrDefault(x => x.id == bossId);
            if (result is null)
            {
                data = null;
                return false;
            }

            data = result;
            return true;
        }

        public static bool TryGetRaid(int raidId, out WorldBossListSheet.Row row)
        {
            var sheet = Game.Game.instance.TableSheets.WorldBossListSheet;
            row = sheet.Values.FirstOrDefault(x => x.Id.Equals(raidId));
            return row != null;
        }

        public static bool TryGetRunes(int bossId, out List<RuneSheet.Row> rows)
        {
            var runeIds = new List<int>();
            var sheet = Game.Game.instance.TableSheets.RuneWeightSheet;
            var weightRows = sheet.Values.Where(x => x.BossId == bossId).ToList();
            foreach (var row in weightRows)
            {
                runeIds.AddRange(row.RuneInfos.Select(x => x.RuneId));
            }

            var ids = runeIds.Distinct().ToList();
            var runeSheet = Game.Game.instance.TableSheets.RuneSheet;
            rows = runeSheet.Values.Where(x => ids.Contains(x.Id)).ToList();
            return rows.Any();
        }

        public static bool TryGetKillRewards(int bossId, out List<WorldBossKillRewardSheet.Row> rows)
        {
            var sheet = Game.Game.instance.TableSheets.WorldBossKillRewardSheet;
            rows = sheet.Values.Where(x => x.BossId == bossId).ToList();
            return rows.Any();
        }

        public static bool TryGetBattleRewards(int bossId, out List<WorldBossBattleRewardSheet.Row> rows)
        {
            var sheet = Game.Game.instance.TableSheets.WorldBossBattleRewardSheet;
            rows = sheet.Values.Where(x => x.BossId == bossId).ToList();
            return rows.Any();
        }

        public static bool IsItInSeason(long currentBlockIndex)
        {
            var sheet = Game.Game.instance.TableSheets.WorldBossListSheet;
            return sheet.Values.Any(x => x.StartedBlockIndex <= currentBlockIndex &&
                currentBlockIndex <= x.EndedBlockIndex);
        }

        public static bool TryGetCurrentRow(long currentBlockIndex, out WorldBossListSheet.Row row)
        {
            var sheet = Game.Game.instance.TableSheets.WorldBossListSheet;
            row = sheet.Values.FirstOrDefault(x => x.StartedBlockIndex <= currentBlockIndex &&
                currentBlockIndex <= x.EndedBlockIndex);
            return row != null;
        }

        public static bool TryGetPreviousRow(long currentBlockIndex, out WorldBossListSheet.Row row)
        {
            var sheet = Game.Game.instance.TableSheets.WorldBossListSheet;
            var rows = sheet.Values.Where(x => x.EndedBlockIndex < currentBlockIndex)
                .OrderByDescending(x => x.EndedBlockIndex)
                .ToList();
            row = rows.Any() ? rows.First() : null;
            return rows.Any();
        }

        public static bool TryGetNextRow(long currentBlockIndex, out WorldBossListSheet.Row row)
        {
            var sheet = Game.Game.instance.TableSheets.WorldBossListSheet;
            var rows = sheet.Values.Where(x => x.StartedBlockIndex > currentBlockIndex)
                .OrderBy(x => x.StartedBlockIndex)
                .ToList();
            row = rows.Any() ? rows.First() : null;
            return rows.Any();
        }

        public static WorldBossStatus GetStatus(long currentBlockIndex)
        {
            return IsItInSeason(currentBlockIndex) ? WorldBossStatus.Season : WorldBossStatus.OffSeason;
        }

        public static int GetRemainTicket(RaiderState state, long currentBlockIndex, int refillInterval)
        {
            if (!TryGetCurrentRow(currentBlockIndex, out var row))
            {
                return WorldBossHelper.MaxChallengeCount;
            }

            if (state == null)
            {
                return WorldBossHelper.MaxChallengeCount;
            }

            var startBlockIndex = row.StartedBlockIndex;
            var refillBlockIndex = state.RefillBlockIndex;
            var refillable = WorldBossHelper.CanRefillTicket(
                currentBlockIndex, refillBlockIndex, startBlockIndex, refillInterval);
            return refillable ? WorldBossHelper.MaxChallengeCount : state.RemainChallengeCount;
        }

        public static long GetScoreInRank(int rank, WorldBossCharacterSheet.Row bossRow)
        {
            var sum = bossRow.WaveStats
                .Where(x => x.Wave <= rank)
                .Sum(x => x.HP);

            return NumberConversionHelper.SafeDecimalToInt64(sum);
        }

        public static GameObject GetRankPrefab(int rank)
        {
            var index = rank - 1;
            return WorldBossData.Rank[index];
        }
    }
}
