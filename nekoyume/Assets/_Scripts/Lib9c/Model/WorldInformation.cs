using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bencodex.Types;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model
{
    [Serializable]
    public class WorldInformation : IState
    {
        [Serializable]
        public struct World : IState
        {
            public readonly int Id;
            public readonly string Name;
            public readonly int StageBegin;
            public readonly int StageEnd;
            public readonly bool IsUnlocked;
            public readonly long UnlockedBlockIndex;
            public readonly bool IsStageCleared;
            public readonly long StageClearedBlockIndex;
            public readonly int StageClearedId;

            public World(WorldSheet.Row worldRow, long unlockedBlockIndex = -1, long stageClearedBlockIndex = -1,
                int stageClearedId = -1)
            {
                Id = worldRow.Id;
                Name = worldRow.Name;
                StageBegin = worldRow.StageBegin;
                StageEnd = worldRow.StageEnd;
                IsUnlocked = unlockedBlockIndex != -1;
                UnlockedBlockIndex = unlockedBlockIndex;
                IsStageCleared = stageClearedBlockIndex != -1;
                StageClearedBlockIndex = stageClearedBlockIndex;
                StageClearedId = stageClearedId;
            }

            public World(World world, long unlockedBlockIndex = -1)
            {
                Id = world.Id;
                Name = world.Name;
                StageBegin = world.StageBegin;
                StageEnd = world.StageEnd;
                IsUnlocked = true;
                UnlockedBlockIndex = unlockedBlockIndex;
                IsStageCleared = world.IsStageCleared;
                StageClearedBlockIndex = world.StageClearedBlockIndex;
                StageClearedId = world.StageClearedId;
            }

            public World(World world, long stageClearedBlockIndex, int stageClearedId)
            {
                Id = world.Id;
                Name = world.Name;
                StageBegin = world.StageBegin;
                StageEnd = world.StageEnd;
                IsUnlocked = world.IsUnlocked;
                UnlockedBlockIndex = world.UnlockedBlockIndex;
                IsStageCleared = true;
                StageClearedBlockIndex = stageClearedBlockIndex;
                StageClearedId = stageClearedId;
            }

            public World(Bencodex.Types.Dictionary serialized)
            {
                Id = serialized.GetInteger("Id");
                Name = serialized.GetString("Name");
                StageBegin = serialized.GetInteger("StageBegin");
                StageEnd = serialized.GetInteger("StageEnd");
                IsUnlocked = serialized.GetBoolean("IsUnlocked");
                UnlockedBlockIndex = serialized.GetLong("UnlockedBlockIndex");
                IsStageCleared = serialized.GetBoolean("IsStageCleared");
                StageClearedBlockIndex = serialized.GetLong("StageClearedBlockIndex");
                StageClearedId = serialized.GetInteger("StageClearedId");
            }

            public IValue Serialize()
            {
                return new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Bencodex.Types.Text) "Id"] = Id.Serialize(),
                    [(Bencodex.Types.Text) "Name"] = Name.Serialize(),
                    [(Bencodex.Types.Text) "StageBegin"] = StageBegin.Serialize(),
                    [(Bencodex.Types.Text) "StageEnd"] = StageEnd.Serialize(),
                    [(Bencodex.Types.Text) "IsUnlocked"] = IsUnlocked.Serialize(),
                    [(Bencodex.Types.Text) "UnlockedBlockIndex"] = UnlockedBlockIndex.Serialize(),
                    [(Bencodex.Types.Text) "IsStageCleared"] = IsStageCleared.Serialize(),
                    [(Bencodex.Types.Text) "StageClearedBlockIndex"] = StageClearedBlockIndex.Serialize(),
                    [(Bencodex.Types.Text) "StageClearedId"] = StageClearedId.Serialize(),
                });
            }

            public bool ContainsStageId(int stageId)
            {
                return stageId >= StageBegin &&
                       stageId <= StageEnd;
            }

            public bool IsPlayable(int stageId)
            {
                return stageId <= GetNextStageIdForPlay();
            }

            public int GetNextStageIdForPlay()
            {
                if (!IsUnlocked)
                    return -1;

                return GetNextStageId();
            }

            public int GetNextStageId()
            {
                return IsStageCleared ? Math.Min(StageEnd, StageClearedId + 1) : StageBegin;
            }
        }

        /// <summary>
        /// key: worldId
        /// </summary>
        private readonly Dictionary<int, World> _worlds = new Dictionary<int, World>();

        public WorldInformation(long blockIndex, WorldSheet worldSheet, bool openAllOfWorldsAndStages = false)
        {
            if (worldSheet is null)
                return;
            
            var orderedSheet = worldSheet.OrderedList;

            if (openAllOfWorldsAndStages)
            {
                foreach (var row in orderedSheet)
                {
                    _worlds.Add(row.Id, new World(row, blockIndex, blockIndex, row.StageEnd));
                }
            }
            else
            {
                var isFirst = true;
                foreach (var row in orderedSheet)
                {
                    var worldId = row.Id;
                    if (isFirst)
                    {
                        isFirst = false;
                        _worlds.Add(worldId, new World(row, blockIndex));
                    }
                    else
                    {
                        _worlds.Add(worldId, new World(row));
                    }
                }
            }
        }

        public WorldInformation(Bencodex.Types.Dictionary serialized)
        {
            _worlds = serialized.ToDictionary(
                kv => kv.Key.ToInteger(),
                kv => new World((Bencodex.Types.Dictionary) kv.Value)
            );
        }

        public IValue Serialize()
        {
            return new Bencodex.Types.Dictionary(_worlds.Select(kv =>
                new KeyValuePair<IKey, IValue>(
                    (Bencodex.Types.Text) kv.Key.Serialize(),
                    (Bencodex.Types.Dictionary) kv.Value.Serialize())));
        }

        /// <summary>
        /// 인자로 받은 `worldId`에 해당하는 `World` 객체를 얻는다.
        /// </summary>
        /// <param name="worldId"></param>
        /// <param name="world"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public bool TryGetWorld(int worldId, out World world)
        {
            if (!_worlds.ContainsKey(worldId))
                throw new KeyNotFoundException($"{nameof(worldId)}: {worldId}");

            world = _worlds[worldId];
            return true;
        }

        /// <summary>
        /// `CurrentWorldId`와 `CurrentStageId`를 초기화 한다.
        /// </summary>
        public bool TryGetFirstWorld(out World world)
        {
            if (_worlds.Count == 0)
            {
                world = default;
                return false;
            }

            world = _worlds.First(e => true).Value;
            return true;
        }

        /// <summary>
        /// 인자로 받은 `stageId`가 속한 `World` 객체를 얻는다.
        /// </summary>
        /// <param name="stageId"></param>
        /// <param name="world"></param>
        /// <returns></returns>
        public bool TryGetWorldByStageId(int stageId, out World world)
        {
            var worlds = _worlds.Values.Where(e => e.ContainsStageId(stageId)).ToList();
            if (worlds.Count == 0)
            {
                world = default;
                return false;
            }

            world = worlds[0];
            return true;
        }

        /// <summary>
        /// 새롭게 스테이지를 클리어한 시간이 가장 최근인 월드를 얻는다. 
        /// </summary>
        /// <param name="world"></param>
        /// <returns></returns>
        public bool TryGetUnlockedWorldByLastStageClearedAt(out World world)
        {
            try
            {
                world = _worlds.Values
                    .Where(e => e.IsStageCleared)
                    .OrderByDescending(e => e.StageClearedBlockIndex)
                    .First();
                return true;
            }
            catch
            {
                world = default;
                return false;
            }
        }

        /// <summary>
        /// 스테이지를 클리어 시킨다.
        /// </summary>
        /// <param name="worldId"></param>
        /// <param name="stageId"></param>
        /// <param name="clearedAt"></param>
        /// <exception cref="ArgumentException"></exception>
        public void ClearStage(int worldId, int stageId, long clearedAt, WorldUnlockSheet unlockSheet)
        {
            if (!_worlds.ContainsKey(worldId))
                throw new ArgumentException(
                    $"{nameof(worldId)}({worldId}) not unlocked. If you want to clear, unlock first.");

            var world = _worlds[worldId];
            if (stageId <= world.StageClearedId)
                return;

            if (world.IsStageCleared && stageId > world.StageClearedId + 1 ||
                !world.IsStageCleared && stageId != world.StageBegin)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"{nameof(worldId)}({worldId})-{nameof(stageId)}({stageId}) is too big.");
                sb.AppendLine($"Cleared {nameof(stageId)} is ({world.StageClearedId}).");
                throw new ArgumentException(sb.ToString());
            }

            _worlds[worldId] = new World(world, clearedAt, stageId);

            if (unlockSheet.TryGetUnlockedInformation(worldId, stageId, out var worldIdsToUnlock))
            {
                foreach (var worldIdToUnlock in worldIdsToUnlock)
                {
                    UnlockWorld(worldIdToUnlock, clearedAt);
                }
            }
        }

        /// <summary>
        /// 특정 월드를 잠금 해제한다.
        /// </summary>
        /// <param name="worldId"></param>
        /// <param name="unlockedAt"></param>
        /// <exception cref="KeyNotFoundException"></exception>
        private void UnlockWorld(int worldId, long unlockedAt)
        {
            if (!_worlds.ContainsKey(worldId))
                throw new KeyNotFoundException($"{nameof(worldId)}: {worldId}");

            if (_worlds[worldId].IsUnlocked)
                return;

            var world = _worlds[worldId];
            _worlds[worldId] = new World(world, unlockedAt);
        }
    }
}
