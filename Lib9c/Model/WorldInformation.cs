using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Bencodex;
using Bencodex.Types;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model
{
    [Serializable]
    public class WorldInformation : IState, ISerializable
    {
        [Serializable]
        public struct World : IState
        {
            public readonly int Id;
            public readonly string Name;
            public readonly int StageBegin;
            public readonly int StageEnd;
            public readonly long UnlockedBlockIndex;
            public readonly long StageClearedBlockIndex;
            public readonly int StageClearedId;

            public bool IsUnlocked => UnlockedBlockIndex != -1;
            public bool IsStageCleared => StageClearedBlockIndex != -1;

            public World(
                WorldSheet.Row worldRow,
                long unlockedBlockIndex = -1,
                long stageClearedBlockIndex = -1,
                int stageClearedId = -1)
            {
                Id = worldRow.Id;
                Name = worldRow.Name;
                StageBegin = worldRow.StageBegin;
                StageEnd = worldRow.StageEnd;
                UnlockedBlockIndex = unlockedBlockIndex;
                StageClearedBlockIndex = stageClearedBlockIndex;
                StageClearedId = stageClearedId;
            }

            public World(World world, long unlockedBlockIndex = -1)
            {
                Id = world.Id;
                Name = world.Name;
                StageBegin = world.StageBegin;
                StageEnd = world.StageEnd;
                UnlockedBlockIndex = unlockedBlockIndex;
                StageClearedBlockIndex = world.StageClearedBlockIndex;
                StageClearedId = world.StageClearedId;
            }

            public World(World world, long stageClearedBlockIndex, int stageClearedId)
            {
                Id = world.Id;
                Name = world.Name;
                StageBegin = world.StageBegin;
                StageEnd = world.StageEnd;
                UnlockedBlockIndex = world.UnlockedBlockIndex;
                StageClearedBlockIndex = stageClearedBlockIndex;
                StageClearedId = stageClearedId;
            }

            public World(Bencodex.Types.Dictionary serialized)
            {
                Id = serialized.GetInteger("Id");
                Name = serialized.GetString("Name");
                StageBegin = serialized.GetInteger("StageBegin");
                StageEnd = serialized.GetInteger("StageEnd");
                UnlockedBlockIndex = serialized.GetLong("UnlockedBlockIndex");
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
                    [(Bencodex.Types.Text) "UnlockedBlockIndex"] = UnlockedBlockIndex.Serialize(),
                    [(Bencodex.Types.Text) "StageClearedBlockIndex"] =
                        StageClearedBlockIndex.Serialize(),
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

        private static readonly Codec _codec = new Codec();
        private Dictionary _serialized;
        private Dictionary<int, World> _worlds;

        /// <summary>
        /// key: worldId
        /// </summary>
        private IDictionary<int, World> worlds
        {
            get
            {
                if (_worlds is null)
                {
                    _worlds = _serialized.ToDictionary(
                        kv => kv.Key.ToInteger(),
                        kv => new World((Bencodex.Types.Dictionary)kv.Value)
                    );
                    _serialized = null;
                }

                return _worlds;
            }
        }

        public WorldInformation(
            long blockIndex,
            WorldSheet worldSheet,
            bool openAllOfWorldsAndStages = false)
        {
            if (worldSheet is null)
            {
                return;
            }

            var orderedSheet = worldSheet.OrderedList;
            _worlds = new Dictionary<int, World>();

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

        public WorldInformation(long blockIndex, WorldSheet worldSheet, int clearStageId = 0)
        {
            if (worldSheet is null)
            {
                return;
            }

            var orderedSheet = worldSheet.OrderedList;
            _worlds = new Dictionary<int, World>();

            if (clearStageId > 0)
            {
                foreach (var row in orderedSheet)
                {
                    if (row.StageBegin > clearStageId)
                    {
                        _worlds.Add(row.Id, new World(row));
                    }
                    else if (row.StageEnd > clearStageId)
                    {
                        _worlds.Add(row.Id, new World(row, blockIndex, blockIndex, clearStageId));
                    }
                    else
                    {
                        _worlds.Add(row.Id, new World(row, blockIndex, blockIndex, row.StageEnd));
                    }
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
            _serialized = serialized;
        }

        private WorldInformation(SerializationInfo info, StreamingContext context)
            : this((Dictionary)_codec.Decode((byte[])info.GetValue("serialized", typeof(byte[]))))
        {
        }

        public IValue Serialize()
        {
            if (_serialized is Dictionary d)
            {
                return d;
            }

#pragma warning disable LAA1002
            return new Bencodex.Types.Dictionary(_worlds.Select(kv =>
#pragma warning restore LAA1002
                new KeyValuePair<IKey, IValue>(
                    (Bencodex.Types.Text) kv.Key.Serialize(),
                    (Bencodex.Types.Dictionary) kv.Value.Serialize())));
        }

        public bool IsWorldUnlocked(int worldId) =>
            TryGetWorld(worldId, out var world)
            && world.IsUnlocked;

        public bool IsStageCleared(int stageId)
        {
            int clearedStageId;
            if (stageId >= GameConfig.MimisbrunnrStartStageId
                ? TryGetLastClearedMimisbrunnrStageId(out clearedStageId)
                : TryGetLastClearedStageId(out clearedStageId))
            {
                return stageId <= clearedStageId;
            }

            return false;
        }

        public bool TryAddWorld(WorldSheet.Row worldRow, out World world)
        {
            if (worldRow is null || (_serialized is Dictionary d
                    ? d.ContainsKey((IKey)worldRow.Id.Serialize())
                    : _worlds.ContainsKey(worldRow.Id)))
            {
                world = default;
                return false;
            }

            world = new World(worldRow);

            if (_serialized is Dictionary s)
            {
                var key = (IKey)worldRow.Id.Serialize();
                _serialized = (Dictionary)s.Add(key, world.Serialize());
            }
            else
            {
                worlds.Add(worldRow.Id, world);
            }

            return true;
        }

        public void UpdateWorld(WorldSheet.Row worldRow)
        {
            var key = (IKey)worldRow.Id.Serialize();
            var originWorld = _serialized is Dictionary d
                ? new World((Dictionary)d[key])
                : _worlds[worldRow.Id];

            var world = new World(
                worldRow,
                originWorld.UnlockedBlockIndex,
                originWorld.StageClearedBlockIndex,
                originWorld.StageClearedId
            );

            if (_serialized is Dictionary s)
            {
                _serialized = (Dictionary)s.SetItem(key, world.Serialize());
            }
            else
            {
                _worlds[worldRow.Id] = world;
            }
        }

        /// <summary>
        /// Get `World` object that equals to `worldId` argument.
        /// </summary>
        /// <param name="worldId"></param>
        /// <param name="world"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public bool TryGetWorld(int worldId, out World world)
        {
            if (!worlds.ContainsKey(worldId))
            {
                world = default;
                return false;
            }

            world = worlds[worldId];
            return true;
        }

        public bool TryGetFirstWorld(out World world)
        {
            if (worlds.Count == 0)
            {
                world = default;
                return false;
            }

            world = worlds.OrderBy(w => w.Key).First().Value;
            return true;
        }

        /// <summary>
        /// Get `World` object that contains `stageId` argument.
        /// </summary>
        /// <param name="stageId"></param>
        /// <param name="world"></param>
        /// <returns></returns>
        public bool TryGetWorldByStageId(int stageId, out World world)
        {
            foreach (World w in worlds.Values)
            {
                if (w.ContainsStageId(stageId))
                {
                    world = w;
                    return true;
                }
            }

            world = default;
            return false;
        }

        /// <summary>
        /// Get `World` object that contains the most recent stage clear.
        /// </summary>
        /// <param name="world"></param>
        /// <returns></returns>
        public bool TryGetUnlockedWorldByStageClearedBlockIndex(out World world)
        {
            try
            {
                world = worlds.Values
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
        /// Get stage id of the most recent cleared.(ignore mimisbrunnr)
        /// </summary>
        /// <param name="stageId"></param>
        /// <returns></returns>
        public bool TryGetLastClearedStageId(out int stageId)
        {
            var clearedStages = worlds.Values
                .Where(world => world.Id < GameConfig.MimisbrunnrWorldId &&
                                world.IsStageCleared)
                .ToList();

            if (clearedStages.Any())
            {
                stageId = clearedStages.Max(world => world.StageClearedId);
                return true;
            }

            stageId = default;
            return false;
        }

        public bool TryGetLastClearedMimisbrunnrStageId(out int stageId)
        {
            var clearedStages = worlds.Values
                .Where(world => world.Id == GameConfig.MimisbrunnrWorldId &&
                                world.IsStageCleared)
                .ToList();

            if (clearedStages.Any())
            {
                stageId = clearedStages.Max(world => world.StageClearedId);
                return true;
            }

            stageId = default;
            return false;
        }

        /// <summary>
        /// Clear a specific stage. And consider world unlock.
        /// </summary>
        /// <param name="worldId"></param>
        /// <param name="stageId"></param>
        /// <param name="clearedAt"></param>
        /// <param name="worldSheet"></param>
        /// <param name="worldUnlockSheet"></param>
        /// <exception cref="FailedToUnlockWorldException"></exception>
        public void ClearStage(
            int worldId,
            int stageId,
            long clearedAt,
            WorldSheet worldSheet,
            WorldUnlockSheet worldUnlockSheet)
        {
            if (!worlds.ContainsKey(worldId))
            {
                return;
            }

            var world = worlds[worldId];
            if (stageId < world.StageBegin ||
                stageId > world.StageEnd)
            {
                return;
            }

            // NOTE: Always consider world unlock.
            // Because even a stage that has already been cleared can be a trigger for world unlock due to the table patch.
            if (worldUnlockSheet.TryGetUnlockedInformation(worldId, stageId, out var worldIdsToUnlock))
            {
                foreach (var worldIdToUnlock in worldIdsToUnlock)
                {
                    UnlockWorld(worldIdToUnlock, clearedAt, worldSheet);
                }
            }

            if (stageId <= world.StageClearedId)
            {
                return;
            }

            worlds[worldId] = new World(world, clearedAt, stageId);
        }

        public void AddAndUnlockNewWorld(WorldSheet.Row worldRow, long unlockedAt, WorldSheet worldSheet)
        {
            var worldId = worldRow.Id;
            if (IsStageCleared(worldRow.StageBegin - 1))
            {
                var world = new World(worldRow);
                worlds.Add(worldId, world);
                UnlockWorld(worldId, unlockedAt, worldSheet);
            }
            else
            {
                throw new FailedAddWorldException($"Failed to add {worldId} world to WorldInformation.");
            }
        }

        public void AddAndUnlockMimisbrunnrWorld(
            WorldSheet.Row worldRow,
            long unlockedAt,
            WorldSheet worldSheet,
            WorldUnlockSheet worldUnlockSheet)
        {
            var succeed = false;
            var worldId = worldRow.Id;
            if (worldId == GameConfig.MimisbrunnrWorldId)
            {
                var unlockRow = worldUnlockSheet.OrderedList.FirstOrDefault(row => row.WorldIdToUnlock == worldId);
                if (!(unlockRow is null) &&
                    IsStageCleared(unlockRow.StageId))
                {
                    succeed = true;
                }
            }
            else if (IsStageCleared(worldRow.StageBegin - 1))
            {
                succeed = true;
            }

            if (succeed)
            {
                var world = new World(worldRow);
                worlds.Add(worldId, world);
                UnlockWorld(worldId, unlockedAt, worldSheet);
            }
            else
            {
                throw new FailedAddWorldException($"Failed to add {worldId} world to WorldInformation.");
            }
        }

        /// <summary>
        /// Unlock a specific world.
        /// </summary>
        /// <param name="worldId"></param>
        /// <param name="unlockedAt"></param>
        /// <param name="worldSheet"></param>
        /// <exception cref="FailedToUnlockWorldException"></exception>
        public void UnlockWorld(int worldId, long unlockedAt, WorldSheet worldSheet)
        {
            World world;
            if (worlds.ContainsKey(worldId))
            {
                world = worlds[worldId];
            }
            else if (!worldSheet.TryGetValue(worldId, out var worldRow) ||
                     !TryAddWorld(worldRow, out world))
            {
                throw new FailedToUnlockWorldException($"{nameof(worldId)}: {worldId}");
            }

            if (world.IsUnlocked)
            {
                return;
            }

            worlds[worldId] = new World(world, unlockedAt);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("serialized", _codec.Encode(Serialize()));
        }
    }

    [Serializable]
    public class FailedToUnlockWorldException : Exception
    {
        public FailedToUnlockWorldException(string message) : base(message)
        {
        }

        protected FailedToUnlockWorldException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class FailedAddWorldException : Exception
    {
        public FailedAddWorldException(string message) : base(message)
        {
        }

        protected FailedAddWorldException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
