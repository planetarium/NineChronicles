using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bencodex.Types;
using Nekoyume.Data.Table;
using Nekoyume.State;
using Nekoyume.TableData;

namespace Nekoyume.Model
{
    public class WorldInformation : IState
    {
        public struct World : IState
        {
            public readonly int Id;
            public readonly string Name;
            public readonly int StageBegin;
            public readonly int StageEnd;
            public readonly bool IsUnlocked;
            public readonly long UnlockedAt;
            public readonly bool IsStageCleared;
            public readonly long StageClearedAt;
            public readonly int StageClearedId;

            public World(WorldSheet.Row worldRow, long unlockedAt = -1, long stageClearedAt = -1, int stageClearedId = -1)
            {
                Id = worldRow.Id;
                Name = worldRow.Name;
                StageBegin = worldRow.StageBegin;
                StageEnd = worldRow.StageEnd;
                IsUnlocked = unlockedAt != -1;
                UnlockedAt = unlockedAt;
                IsStageCleared = stageClearedAt != -1;
                StageClearedAt = stageClearedAt;
                StageClearedId = stageClearedId;
            }

            public World(World world, long unlockedAt = -1)
            {
                Id = world.Id;
                Name = world.Name;
                StageBegin = world.StageBegin;
                StageEnd = world.StageEnd;
                IsUnlocked = true;
                UnlockedAt = unlockedAt;
                IsStageCleared = world.IsStageCleared;
                StageClearedAt = world.StageClearedAt;
                StageClearedId = world.StageClearedId;
            }

            public World(World world, long stageClearedAt, int stageClearedId)
            {
                Id = world.Id;
                Name = world.Name;
                StageBegin = world.StageBegin;
                StageEnd = world.StageEnd;
                IsUnlocked = world.IsUnlocked;
                UnlockedAt = world.UnlockedAt;
                IsStageCleared = true;
                StageClearedAt = stageClearedAt;
                StageClearedId = stageClearedId;
            }

            public World(Bencodex.Types.Dictionary serialized)
            {
                Id = serialized["Id"].ToInteger();
                Name = (Bencodex.Types.Text) serialized["Name"];
                StageBegin = serialized["StageBegin"].ToInteger();
                StageEnd = serialized["StageEnd"].ToInteger();
                IsUnlocked = serialized["IsUnlocked"].ToBoolean();
                UnlockedAt = serialized["UnlockedAt"].ToLong();
                IsStageCleared = serialized["IsStageCleared"].ToBoolean();
                StageClearedAt = serialized["StageClearedAt"].ToLong();
                StageClearedId = serialized["StageClearedId"].ToInteger();
            }

            public IValue Serialize()
            {
                return new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Bencodex.Types.Text) "Id"] = Id.Serialize(),
                    [(Bencodex.Types.Text) "Name"] = (Bencodex.Types.Text) Name,
                    [(Bencodex.Types.Text) "StageBegin"] = StageBegin.Serialize(),
                    [(Bencodex.Types.Text) "StageEnd"] = StageEnd.Serialize(),
                    [(Bencodex.Types.Text) "IsUnlocked"] = IsUnlocked.Serialize(),
                    [(Bencodex.Types.Text) "UnlockedAt"] = UnlockedAt.Serialize(),
                    [(Bencodex.Types.Text) "IsStageCleared"] = IsStageCleared.Serialize(),
                    [(Bencodex.Types.Text) "StageClearedAt"] = StageClearedAt.Serialize(),
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
                
                return IsStageCleared ? Math.Min(StageEnd, StageClearedId + 1) : StageBegin;
            }
            
            public int GetNextStageIdForSelect()
            {
                return IsStageCleared ? Math.Min(StageEnd, StageClearedId + 1) : StageBegin;
            }
        }

        /// <summary>
        /// key: worldId
        /// </summary>
        private readonly Dictionary<int, World> _worlds = new Dictionary<int, World>();

        public WorldInformation(long blockIndex, bool openAllOfWorldsAndStages = false)
        {
            var worldSheet = Game.Game.instance.TableSheets.WorldSheet.OrderedList;

            if (openAllOfWorldsAndStages)
            {
                foreach (var row in worldSheet)
                {
                    _worlds.Add(row.Id, new World(row, 0, 0, row.StageEnd));
                }
            }
            else
            {
                var isFirst = true;
                foreach (var row in worldSheet)
                {
                    var worldId = row.Id;
                    var world = new World(row);
                    _worlds.Add(worldId, world);
                    if (!isFirst)
                        continue;
                    
                    isFirst = false;
                    UnlockWorld(world, blockIndex);
                }
            }
        }

        public WorldInformation(Bencodex.Types.Dictionary serialized)
        {
            _worlds = ((Bencodex.Types.Dictionary) serialized["_worlds"])
                .ToDictionary(
                    kv => kv.Key.ToInteger(),
                    kv => new World((Bencodex.Types.Dictionary) kv.Value)
                );
        }

        public IValue Serialize()
        {
            return new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Bencodex.Types.Text) "_worlds"] = new Bencodex.Types.Dictionary(
                    _worlds.Select(kv =>
                        new KeyValuePair<IKey, IValue>(
                            (Bencodex.Types.Text) kv.Key.ToString(),
                            (Bencodex.Types.Dictionary) kv.Value.Serialize())))
            });
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
        /// 잠금 해제된 시간이 가장 최근인 월드를 얻는다. 
        /// </summary>
        /// <param name="world"></param>
        /// <returns></returns>
        private bool TryGetUnlockedWorldByLastUnlockedAt(out World world)
        {
            var worlds = _worlds.Values
                .Where(e => e.IsUnlocked)
                .OrderByDescending(e => e.UnlockedAt)
                .ToList();
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
            var worlds = _worlds.Values
                .Where(e => e.IsStageCleared)
                .OrderByDescending(e => e.StageClearedAt)
                .ToList();
            if (worlds.Count == 0)
            {
                world = default;
                return false;
            }

            world = worlds[0];
            return true;
        }

        /// <summary>
        /// 스테이지를 클리어 시킨다.
        /// </summary>
        /// <param name="worldId"></param>
        /// <param name="stageId"></param>
        /// <param name="clearedAt"></param>
        /// <exception cref="ArgumentException"></exception>
        public void ClearStage(int worldId, int stageId, long clearedAt)
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
                sb.AppendLine($"Cleared {nameof(stageId)} is ({world}).");
                throw new ArgumentException(sb.ToString());
            }

            _worlds[worldId] = new World(world, clearedAt, stageId);

            var unlockSheet = Game.Game.instance.TableSheets.WorldUnlockSheet;
            if (unlockSheet.TryGetUnlockedInformation(worldId, stageId, out var worldIdToUnlock))
            {
                UnlockWorld(worldIdToUnlock, clearedAt);
            }
        }
        
        /// <summary>
        /// 특정 월드를 잠금 해제한다.
        /// </summary>
        /// <param name="world"></param>
        /// <param name="unlockedAt"></param>
        /// <exception cref="ArgumentException"></exception>
        private void UnlockWorld(World world, long unlockedAt)
        {
            if (!_worlds.ContainsKey(world.Id))
                throw new KeyNotFoundException($"{nameof(world.Id)}: {world.Id}");

            _worlds[world.Id] = new World(world, unlockedAt);
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

            var world = _worlds[worldId];
            _worlds[worldId] = new World(world, unlockedAt);
        }
    }
}
