// #define TEST_LOG

using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Action;
using Nekoyume.Model;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.Model.Buff;
using Nekoyume.TableData;
using Priority_Queue;

namespace Nekoyume.Battle
{
    public class StageSimulatorV1 : Simulator, IStageSimulator
    {
        private readonly List<Wave> _waves;
        private readonly List<ItemBase> _waveRewards;
        private readonly List<Model.Skill.Skill> _skillsOnWaveStart = new List<Model.Skill.Skill>();
        public CollectionMap ItemMap { get; private set; } = new CollectionMap();
        public EnemySkillSheet EnemySkillSheet { get; }

        public const int ConstructorVersionDefault = 1;
        public const int ConstructorVersionV100025 = 2;
        public const int ConstructorVersionV100080 = 3;

        private int WorldId { get; }
        public int StageId { get; }
        private bool IsCleared { get; }
        private int Exp { get; }
        private int TurnLimit { get; }
        public override IEnumerable<ItemBase> Reward => _waveRewards;

        public StageSimulatorV1(
            IRandom random,
            AvatarState avatarState,
            List<Guid> foods,
            int worldId,
            int stageId,
            StageSimulatorSheetsV1 stageSimulatorSheets,
            int constructorVersion,
            int playCount
            )
            : base(
                random,
                avatarState,
                foods,
                stageSimulatorSheets
            )
        {
            _waves = new List<Wave>();

            WorldId = worldId;
            StageId = stageId;
            IsCleared = avatarState.worldInformation.IsStageCleared(StageId);
            EnemySkillSheet = stageSimulatorSheets.EnemySkillSheet;

            var stageSheet = stageSimulatorSheets.StageSheet;
            if (!stageSheet.TryGetValue(StageId, out var stageRow))
                throw new SheetRowNotFoundException(nameof(stageSheet), StageId);

            var stageWaveSheet = stageSimulatorSheets.StageWaveSheet;
            if (!stageWaveSheet.TryGetValue(StageId, out var stageWaveRow))
                throw new SheetRowNotFoundException(nameof(stageWaveSheet), StageId);

            Exp = StageRewardExpHelper.GetExp(avatarState.level, stageId);
            TurnLimit = stageRow.TurnLimit;

            SetWave(stageRow, stageWaveRow);
            var itemSelector = SetItemSelector(stageRow, Random);
            var maxCount = Random.Next(stageRow.DropItemMin, stageRow.DropItemMax + 1);
            switch (constructorVersion)
            {
                case ConstructorVersionDefault:
                    _waveRewards = SetReward(
                        itemSelector,
                        maxCount,
                        random,
                        MaterialItemSheet
                    );
                    break;
                case ConstructorVersionV100025:
                    _waveRewards = SetRewardV2(
                        itemSelector,
                        maxCount,
                        Random,
                        MaterialItemSheet
                    );
                    break;
                case ConstructorVersionV100080:
                    _waveRewards = new List<ItemBase>();
                    for (var i = 0; i < playCount; i++)
                    {
                        itemSelector = SetItemSelector(stageRow, Random);
                        var rewards = SetRewardV2(
                            itemSelector,
                            maxCount,
                            Random,
                            MaterialItemSheet
                        );

                        foreach (var reward in rewards)
                        {
                            _waveRewards.Add(reward);
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Do not use anymore since v100025.
        /// </summary>
        public StageSimulatorV1(
            IRandom random,
            AvatarState avatarState,
            List<Guid> foods,
            int worldId,
            int stageId,
            StageSimulatorSheetsV1 stageSimulatorSheets
        )
            : base(
                random,
                avatarState,
                foods,
                stageSimulatorSheets
            )
        {
            _waves = new List<Wave>();

            WorldId = worldId;
            StageId = stageId;
            IsCleared = avatarState.worldInformation.IsStageCleared(StageId);
            EnemySkillSheet = stageSimulatorSheets.EnemySkillSheet;

            var stageSheet = stageSimulatorSheets.StageSheet;
            if (!stageSheet.TryGetValue(StageId, out var stageRow))
                throw new SheetRowNotFoundException(nameof(stageSheet), StageId);

            var stageWaveSheet = stageSimulatorSheets.StageWaveSheet;
            if (!stageWaveSheet.TryGetValue(StageId, out var stageWaveRow))
                throw new SheetRowNotFoundException(nameof(stageWaveSheet), StageId);

            Exp = StageRewardExpHelper.GetExp(avatarState.level, stageId);
            TurnLimit = stageRow.TurnLimit;

            SetWave(stageRow, stageWaveRow);
            var itemSelector = SetItemSelector(stageRow, Random);
            _waveRewards = SetReward(
                itemSelector,
                Random.Next(stageRow.DropItemMin, stageRow.DropItemMax + 1),
                Random,
                stageSimulatorSheets.MaterialItemSheet
            );
        }

        public StageSimulatorV1(
            IRandom random,
            AvatarState avatarState,
            List<Guid> foods,
            int worldId,
            int stageId,
            StageSimulatorSheetsV1 stageSimulatorSheets,
            Model.Skill.Skill skill,
            int constructorVersion,
            int playCount
        )
            : this(
                random,
                avatarState,
                foods,
                worldId,
                stageId,
                stageSimulatorSheets,
                constructorVersion,
                playCount
            )
        {
            var stageSheet = stageSimulatorSheets.StageSheet;
            if (!stageSheet.TryGetValue(StageId, out var stageRow))
                throw new SheetRowNotFoundException(nameof(stageSheet), StageId);

            Exp = StageRewardExpHelper.GetExp(avatarState.level, stageId);
            TurnLimit = stageRow.TurnLimit;

            if (!ReferenceEquals(skill, null))
            {
                Player.OverrideSkill(skill);
            }
        }

        /// <summary>
        /// Do not use anymore since v100025.
        /// </summary>
        public StageSimulatorV1(
            IRandom random,
            AvatarState avatarState,
            List<Guid> foods,
            int worldId,
            int stageId,
            StageSimulatorSheetsV1 stageSimulatorSheets,
            Model.Skill.Skill skill
        )
            : this(
                random,
                avatarState,
                foods,
                worldId,
                stageId,
                stageSimulatorSheets
            )
        {
            var stageSheet = stageSimulatorSheets.StageSheet;
            if (!stageSheet.TryGetValue(StageId, out var stageRow))
                throw new SheetRowNotFoundException(nameof(stageSheet), StageId);

            Exp = StageRewardExpHelper.GetExp(avatarState.level, stageId);
            TurnLimit = stageRow.TurnLimit;

            if (!ReferenceEquals(skill, null))
            {
                Player.OverrideSkill(skill);
            }
        }

        public StageSimulatorV1(
            IRandom random,
            AvatarState avatarState,
            List<Guid> foods,
            List<Model.Skill.Skill> skillsOnWaveStart,
            int worldId,
            int stageId,
            StageSimulatorSheetsV1 stageSimulatorSheets,
            CostumeStatSheet costumeStatSheet,
            int constructorVersion,
            int playCount = 1
        )
            : this(
                random,
                avatarState,
                foods,
                worldId,
                stageId,
                stageSimulatorSheets,
                costumeStatSheet,
                constructorVersion,
                playCount
            )
        {
            _skillsOnWaveStart = skillsOnWaveStart;
        }

        public StageSimulatorV1(
            IRandom random,
            AvatarState avatarState,
            List<Guid> foods,
            int worldId,
            int stageId,
            StageSimulatorSheetsV1 stageSimulatorSheets,
            CostumeStatSheet costumeStatSheet,
            int constructorVersion,
            int playCount = 1
        )
            : this(
                random,
                avatarState,
                foods,
                worldId,
                stageId,
                stageSimulatorSheets,
                constructorVersion,
                playCount
            )
        {
            Player.SetCostumeStat(costumeStatSheet);
        }

        /// <summary>
        /// Do not use anymore since v100025.
        /// </summary>
        public StageSimulatorV1(
            IRandom random,
            AvatarState avatarState,
            List<Guid> foods,
            int worldId,
            int stageId,
            StageSimulatorSheetsV1 stageSimulatorSheets,
            CostumeStatSheet costumeStatSheet
        )
            : this(
                random,
                avatarState,
                foods,
                worldId,
                stageId,
                stageSimulatorSheets
            )
        {
            Player.SetCostumeStat(costumeStatSheet);
        }
        
        public Player Simulate(int playCount)
        {
            Log.worldId = WorldId;
            Log.stageId = StageId;
            Log.waveCount = _waves.Count;
            Log.clearedWaveNumber = 0;
            Log.newlyCleared = false;
            Player.Spawn();
            TurnNumber = 0;
            for (var i = 0; i < _waves.Count; i++)
            {
                Characters = new SimplePriorityQueue<CharacterBase, decimal>();
                Characters.Enqueue(Player, TurnPriority / Player.SPD);

                WaveNumber = i + 1;
                WaveTurn = 1;
                _waves[i].Spawn(this);

                foreach (var skill in _skillsOnWaveStart)
                {
                    var buffs = BuffFactory.GetBuffs(
                        Player.ATK,
                        skill,
                        SkillBuffSheet,
                        StatBuffSheet,
                        SkillActionBuffSheet,
                        ActionBuffSheet
                    );

                    var usedSkill = skill.Use(Player, 0, buffs);
                    Log.Add(usedSkill);
                }

                while (true)
                {
                    // 제한 턴을 넘어서는 경우 break.
                    if (TurnNumber > TurnLimit)
                    {
                        if (i == 0)
                        {
                            Result = BattleLog.Result.Lose;
                            if (StageId < GameConfig.MimisbrunnrStartStageId)
                            {
                                Player.GetExp((int)(Exp * 0.3m * playCount), true);
                            }
                        }
                        else
                        {
                            Result = BattleLog.Result.TimeOver;
                        }
                        break;
                    }

                    // 캐릭터 큐가 비어 있는 경우 break.
                    if (!Characters.TryDequeue(out var character))
                        break;

                    character.Tick();

                    // 플레이어가 죽은 경우 break;
                    if (Player.IsDead)
                    {
                        if (i == 0)
                        {
                            Result = BattleLog.Result.Lose;
                            if (StageId < GameConfig.MimisbrunnrStartStageId)
                            {
                                Player.GetExp((int)(Exp * 0.3m * playCount), true);
                            }
                        }
                        else
                        {
                            Result = BattleLog.Result.Win;
                        }
                        break;
                    }

                    // 플레이어의 타겟(적)이 없는 경우 break.
                    if (!Player.Targets.Any())
                    {
                        Result = BattleLog.Result.Win;
                        Log.clearedWaveNumber = WaveNumber;

                        switch (WaveNumber)
                        {
                            case 1:
                                if (StageId < GameConfig.MimisbrunnrStartStageId)
                                {
                                    Player.GetExp(Exp * playCount, true);
                                }

                                break;
                            case 2:
                                {
                                    ItemMap = Player.GetRewards(_waveRewards);
                                    var dropBox = new DropBox(null, _waveRewards);
                                    Log.Add(dropBox);
                                    var getReward = new GetReward(null, _waveRewards);
                                    Log.Add(getReward);
                                    break;
                                }
                            default:
                                if (WaveNumber == _waves.Count)
                                {
                                    if (!IsCleared)
                                    {
                                        Log.newlyCleared = true;
                                    }
                                }

                                break;
                        }

                        break;
                    }

                    foreach (var other in Characters)
                    {
                        var current = Characters.GetPriority(other);
                        var speed = current * 0.6m;
                        Characters.UpdatePriority(other, speed);
                    }

                    Characters.Enqueue(character, TurnPriority / character.SPD);
                }

                // 제한 턴을 넘거나 플레이어가 죽은 경우 break;
                if (TurnNumber > TurnLimit ||
                    Player.IsDead)
                    break;
            }

            Log.result = Result;
            return Player;
        }

        [Obsolete("Use Simulate")]
        public Player SimulateV1()
        {
#if TEST_LOG
            var sb = new System.Text.StringBuilder();
#endif
            Log.worldId = WorldId;
            Log.stageId = StageId;
            Log.waveCount = _waves.Count;
            Log.clearedWaveNumber = 0;
            Log.newlyCleared = false;
            Player.SpawnV1();
            TurnNumber = 0;
            for (var i = 0; i < _waves.Count; i++)
            {
                Characters = new SimplePriorityQueue<CharacterBase, decimal>();
                Characters.Enqueue(Player, TurnPriority / Player.SPD);

                WaveNumber = i + 1;
                WaveTurn = 1;
                _waves[i].SpawnV1(this);
#if TEST_LOG
                sb.Clear();
                sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
                sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
                sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
                sb.Append(" / Wave Start");
                UnityEngine.Debug.LogWarning(sb.ToString());
#endif
                while (true)
                {
                    // 제한 턴을 넘어서는 경우 break.
                    if (TurnNumber > TurnLimit)
                    {
                        if (i == 0)
                        {
                            Result = BattleLog.Result.Lose;
                            if (StageId < GameConfig.MimisbrunnrStartStageId)
                            {
                                Player.GetExp2((int) (Exp * 0.3m), true);
                            }
                        }
                        else
                        {
                            Result = BattleLog.Result.TimeOver;
                        }
#if TEST_LOG
                        sb.Clear();
                        sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
                        sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
                        sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
                        sb.Append($" / {nameof(TurnLimit)}: {TurnLimit}");
                        sb.Append($" / {nameof(Result)}: {Result.ToString()}");
                        UnityEngine.Debug.LogWarning(sb.ToString());
#endif
                        break;
                    }

                    // 캐릭터 큐가 비어 있는 경우 break.
                    if (!Characters.TryDequeue(out var character))
                        break;
#if TEST_LOG
                    var turnBefore = TurnNumber;
#endif
                    character.Tick();
#if TEST_LOG
                    var turnAfter = TurnNumber;
                    if (turnBefore != turnAfter)
                    {
                        sb.Clear();
                        sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
                        sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
                        sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
                        sb.Append(" / Turn End");
                        UnityEngine.Debug.LogWarning(sb.ToString());
                    }
#endif

                    // 플레이어가 죽은 경우 break;
                    if (Player.IsDead)
                    {
                        if (i == 0)
                        {
                            Result = BattleLog.Result.Lose;
                            if (StageId < GameConfig.MimisbrunnrStartStageId)
                            {
                                Player.GetExp2((int) (Exp * 0.3m), true);
                            }
                        }
                        else
                        {
                            Result = BattleLog.Result.Win;
                        }
#if TEST_LOG
                        sb.Clear();
                        sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
                        sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
                        sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
                        sb.Append($" / {nameof(Player)} Dead");
                        sb.Append($" / {nameof(Result)}: {Result.ToString()}");
                        UnityEngine.Debug.LogWarning(sb.ToString());
#endif
                        break;
                    }

                    // 플레이어의 타겟(적)이 없는 경우 break.
                    if (!Player.Targets.Any())
                    {
                        Result = BattleLog.Result.Win;
                        Log.clearedWaveNumber = WaveNumber;

                        switch (WaveNumber)
                        {
                            case 1:
                                if (StageId < GameConfig.MimisbrunnrStartStageId)
                                {
                                    Player.GetExp2(Exp, true);
                                }

                                break;
                            case 2:
                            {
                                ItemMap = Player.GetRewards2(_waveRewards);
                                var dropBox = new DropBox(null, _waveRewards);
                                Log.Add(dropBox);
                                var getReward = new GetReward(null, _waveRewards);
                                Log.Add(getReward);
                                break;
                            }
                            default:
                                if (WaveNumber == _waves.Count)
                                {
                                    if (!IsCleared)
                                    {
                                        Log.newlyCleared = true;
                                    }
                                }

                                break;
                        }

                        break;
                    }

                    foreach (var other in Characters)
                    {
                        var current = Characters.GetPriority(other);
                        var speed = current * 0.6m;
                        Characters.UpdatePriority(other, speed);
                    }

                    Characters.Enqueue(character, TurnPriority / character.SPD);
                }

                // 제한 턴을 넘거나 플레이어가 죽은 경우 break;
                if (TurnNumber > TurnLimit ||
                    Player.IsDead)
                    break;
            }

            Log.result = Result;
#if TEST_LOG
            sb.Clear();
            sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
            sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
            sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
            sb.Append($" / {nameof(Simulate)} End");
            sb.Append($" / {nameof(Result)}: {Result.ToString()}");
            UnityEngine.Debug.LogWarning(sb.ToString());
#endif
            return Player;
        }

        [Obsolete("Use Simulate")]
        public Player SimulateV2()
        {
#if TEST_LOG
            var sb = new System.Text.StringBuilder();
#endif
            Log.worldId = WorldId;
            Log.stageId = StageId;
            Log.waveCount = _waves.Count;
            Log.clearedWaveNumber = 0;
            Log.newlyCleared = false;
            Player.SpawnV2();
            TurnNumber = 0;
            for (var i = 0; i < _waves.Count; i++)
            {
                Characters = new SimplePriorityQueue<CharacterBase, decimal>();
                Characters.Enqueue(Player, TurnPriority / Player.SPD);

                WaveNumber = i + 1;
                WaveTurn = 1;
                _waves[i].SpawnV2(this);
#if TEST_LOG
                sb.Clear();
                sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
                sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
                sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
                sb.Append(" / Wave Start");
                UnityEngine.Debug.LogWarning(sb.ToString());
#endif
                while (true)
                {
                    // 제한 턴을 넘어서는 경우 break.
                    if (TurnNumber > TurnLimit)
                    {
                        if (i == 0)
                        {
                            Result = BattleLog.Result.Lose;
                            if (StageId < GameConfig.MimisbrunnrStartStageId)
                            {
                                Player.GetExp3((int) (Exp * 0.3m), true);
                            }
                        }
                        else
                        {
                            Result = BattleLog.Result.TimeOver;
                        }
#if TEST_LOG
                        sb.Clear();
                        sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
                        sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
                        sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
                        sb.Append($" / {nameof(TurnLimit)}: {TurnLimit}");
                        sb.Append($" / {nameof(Result)}: {Result.ToString()}");
                        UnityEngine.Debug.LogWarning(sb.ToString());
#endif
                        break;
                    }

                    // 캐릭터 큐가 비어 있는 경우 break.
                    if (!Characters.TryDequeue(out var character))
                        break;
#if TEST_LOG
                    var turnBefore = TurnNumber;
#endif
                    character.Tick();
#if TEST_LOG
                    var turnAfter = TurnNumber;
                    if (turnBefore != turnAfter)
                    {
                        sb.Clear();
                        sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
                        sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
                        sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
                        sb.Append(" / Turn End");
                        UnityEngine.Debug.LogWarning(sb.ToString());
                    }
#endif

                    // 플레이어가 죽은 경우 break;
                    if (Player.IsDead)
                    {
                        if (i == 0)
                        {
                            Result = BattleLog.Result.Lose;
                            if (StageId < GameConfig.MimisbrunnrStartStageId)
                            {
                                Player.GetExp3((int) (Exp * 0.3m), true);
                            }
                        }
                        else
                        {
                            Result = BattleLog.Result.Win;
                        }
#if TEST_LOG
                        sb.Clear();
                        sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
                        sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
                        sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
                        sb.Append($" / {nameof(Player)} Dead");
                        sb.Append($" / {nameof(Result)}: {Result.ToString()}");
                        UnityEngine.Debug.LogWarning(sb.ToString());
#endif
                        break;
                    }

                    // 플레이어의 타겟(적)이 없는 경우 break.
                    if (!Player.Targets.Any())
                    {
                        Result = BattleLog.Result.Win;
                        Log.clearedWaveNumber = WaveNumber;

                        switch (WaveNumber)
                        {
                            case 1:
                                if (StageId < GameConfig.MimisbrunnrStartStageId)
                                {
                                    Player.GetExp3(Exp, true);
                                }

                                break;
                            case 2:
                            {
                                ItemMap = Player.GetRewards2(_waveRewards);
                                var dropBox = new DropBox(null, _waveRewards);
                                Log.Add(dropBox);
                                var getReward = new GetReward(null, _waveRewards);
                                Log.Add(getReward);
                                break;
                            }
                            default:
                                if (WaveNumber == _waves.Count)
                                {
                                    if (!IsCleared)
                                    {
                                        Log.newlyCleared = true;
                                    }
                                }

                                break;
                        }

                        break;
                    }

                    foreach (var other in Characters)
                    {
                        var current = Characters.GetPriority(other);
                        var speed = current * 0.6m;
                        Characters.UpdatePriority(other, speed);
                    }

                    Characters.Enqueue(character, TurnPriority / character.SPD);
                }

                // 제한 턴을 넘거나 플레이어가 죽은 경우 break;
                if (TurnNumber > TurnLimit ||
                    Player.IsDead)
                    break;
            }

            Log.result = Result;
#if TEST_LOG
            sb.Clear();
            sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
            sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
            sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
            sb.Append($" / {nameof(Simulate)} End");
            sb.Append($" / {nameof(Result)}: {Result.ToString()}");
            UnityEngine.Debug.LogWarning(sb.ToString());
#endif
            return Player;
        }

        [Obsolete("Use Simulate")]
        public Player SimulateV3()
        {
            Log.worldId = WorldId;
            Log.stageId = StageId;
            Log.waveCount = _waves.Count;
            Log.clearedWaveNumber = 0;
            Log.newlyCleared = false;
            Player.SpawnV2();
            TurnNumber = 0;
            for (var i = 0; i < _waves.Count; i++)
            {
                Characters = new SimplePriorityQueue<CharacterBase, decimal>();
                Characters.Enqueue(Player, TurnPriority / Player.SPD);

                WaveNumber = i + 1;
                WaveTurn = 1;
                _waves[i].SpawnV2(this);
                while (true)
                {
                    // 제한 턴을 넘어서는 경우 break.
                    if (TurnNumber > TurnLimit)
                    {
                        if (i == 0)
                        {
                            Result = BattleLog.Result.Lose;
                            if (StageId < GameConfig.MimisbrunnrStartStageId)
                            {
                                Player.GetExp((int) (Exp * 0.3m), true);
                            }
                        }
                        else
                        {
                            Result = BattleLog.Result.TimeOver;
                        }
                        break;
                    }

                    // 캐릭터 큐가 비어 있는 경우 break.
                    if (!Characters.TryDequeue(out var character))
                        break;

                    character.Tick();

                    // 플레이어가 죽은 경우 break;
                    if (Player.IsDead)
                    {
                        if (i == 0)
                        {
                            Result = BattleLog.Result.Lose;
                            if (StageId < GameConfig.MimisbrunnrStartStageId)
                            {
                                Player.GetExp((int) (Exp * 0.3m), true);
                            }
                        }
                        else
                        {
                            Result = BattleLog.Result.Win;
                        }
                        break;
                    }

                    // 플레이어의 타겟(적)이 없는 경우 break.
                    if (!Player.Targets.Any())
                    {
                        Result = BattleLog.Result.Win;
                        Log.clearedWaveNumber = WaveNumber;

                        switch (WaveNumber)
                        {
                            case 1:
                                if (StageId < GameConfig.MimisbrunnrStartStageId)
                                {
                                    Player.GetExp(Exp, true);
                                }

                                break;
                            case 2:
                            {
                                ItemMap = Player.GetRewards2(_waveRewards);
                                var dropBox = new DropBox(null, _waveRewards);
                                Log.Add(dropBox);
                                var getReward = new GetReward(null, _waveRewards);
                                Log.Add(getReward);
                                break;
                            }
                            default:
                                if (WaveNumber == _waves.Count)
                                {
                                    if (!IsCleared)
                                    {
                                        Log.newlyCleared = true;
                                    }
                                }

                                break;
                        }

                        break;
                    }

                    foreach (var other in Characters)
                    {
                        var current = Characters.GetPriority(other);
                        var speed = current * 0.6m;
                        Characters.UpdatePriority(other, speed);
                    }

                    Characters.Enqueue(character, TurnPriority / character.SPD);
                }

                // 제한 턴을 넘거나 플레이어가 죽은 경우 break;
                if (TurnNumber > TurnLimit ||
                    Player.IsDead)
                    break;
            }

            Log.result = Result;
            return Player;
        }

        [Obsolete("Use Simulate")]
        public Player SimulateV4()
        {
            Log.worldId = WorldId;
            Log.stageId = StageId;
            Log.waveCount = _waves.Count;
            Log.clearedWaveNumber = 0;
            Log.newlyCleared = false;
            Player.SpawnV2();
            TurnNumber = 0;
            for (var i = 0; i < _waves.Count; i++)
            {
                Characters = new SimplePriorityQueue<CharacterBase, decimal>();
                Characters.Enqueue(Player, TurnPriority / Player.SPD);

                WaveNumber = i + 1;
                WaveTurn = 1;
                _waves[i].SpawnV2(this);
                while (true)
                {
                    // 제한 턴을 넘어서는 경우 break.
                    if (TurnNumber > TurnLimit)
                    {
                        if (i == 0)
                        {
                            Result = BattleLog.Result.Lose;
                            if (StageId < GameConfig.MimisbrunnrStartStageId)
                            {
                                Player.GetExp((int) (Exp * 0.3m), true);
                            }
                        }
                        else
                        {
                            Result = BattleLog.Result.TimeOver;
                        }
                        break;
                    }

                    // 캐릭터 큐가 비어 있는 경우 break.
                    if (!Characters.TryDequeue(out var character))
                        break;

                    character.Tick();

                    // 플레이어가 죽은 경우 break;
                    if (Player.IsDead)
                    {
                        if (i == 0)
                        {
                            Result = BattleLog.Result.Lose;
                            if (StageId < GameConfig.MimisbrunnrStartStageId)
                            {
                                Player.GetExp((int) (Exp * 0.3m), true);
                            }
                        }
                        else
                        {
                            Result = BattleLog.Result.Win;
                        }
                        break;
                    }

                    // 플레이어의 타겟(적)이 없는 경우 break.
                    if (!Player.Targets.Any())
                    {
                        Result = BattleLog.Result.Win;
                        Log.clearedWaveNumber = WaveNumber;

                        switch (WaveNumber)
                        {
                            case 1:
                                if (StageId < GameConfig.MimisbrunnrStartStageId)
                                {
                                    Player.GetExp(Exp, true);
                                }

                                break;
                            case 2:
                            {
                                ItemMap = Player.GetRewards(_waveRewards);
                                var dropBox = new DropBox(null, _waveRewards);
                                Log.Add(dropBox);
                                var getReward = new GetReward(null, _waveRewards);
                                Log.Add(getReward);
                                break;
                            }
                            default:
                                if (WaveNumber == _waves.Count)
                                {
                                    if (!IsCleared)
                                    {
                                        Log.newlyCleared = true;
                                    }
                                }

                                break;
                        }

                        break;
                    }

                    foreach (var other in Characters)
                    {
                        var current = Characters.GetPriority(other);
                        var speed = current * 0.6m;
                        Characters.UpdatePriority(other, speed);
                    }

                    Characters.Enqueue(character, TurnPriority / character.SPD);
                }

                // 제한 턴을 넘거나 플레이어가 죽은 경우 break;
                if (TurnNumber > TurnLimit ||
                    Player.IsDead)
                    break;
            }

            Log.result = Result;
            return Player;
        }

        [Obsolete("Use Simulate")]
        public Player SimulateV5(int playCount)
        {
            Log.worldId = WorldId;
            Log.stageId = StageId;
            Log.waveCount = _waves.Count;
            Log.clearedWaveNumber = 0;
            Log.newlyCleared = false;
            Player.SpawnV2();
            TurnNumber = 0;
            for (var i = 0; i < _waves.Count; i++)
            {
                Characters = new SimplePriorityQueue<CharacterBase, decimal>();
                Characters.Enqueue(Player, TurnPriority / Player.SPD);

                WaveNumber = i + 1;
                WaveTurn = 1;
                _waves[i].SpawnV2(this);
                while (true)
                {
                    // 제한 턴을 넘어서는 경우 break.
                    if (TurnNumber > TurnLimit)
                    {
                        if (i == 0)
                        {
                            Result = BattleLog.Result.Lose;
                            if (StageId < GameConfig.MimisbrunnrStartStageId)
                            {
                                Player.GetExp((int) (Exp * 0.3m * playCount), true);
                            }
                        }
                        else
                        {
                            Result = BattleLog.Result.TimeOver;
                        }
                        break;
                    }

                    // 캐릭터 큐가 비어 있는 경우 break.
                    if (!Characters.TryDequeue(out var character))
                        break;

                    character.Tick();

                    // 플레이어가 죽은 경우 break;
                    if (Player.IsDead)
                    {
                        if (i == 0)
                        {
                            Result = BattleLog.Result.Lose;
                            if (StageId < GameConfig.MimisbrunnrStartStageId)
                            {
                                Player.GetExp((int) (Exp * 0.3m * playCount), true);
                            }
                        }
                        else
                        {
                            Result = BattleLog.Result.Win;
                        }
                        break;
                    }

                    // 플레이어의 타겟(적)이 없는 경우 break.
                    if (!Player.Targets.Any())
                    {
                        Result = BattleLog.Result.Win;
                        Log.clearedWaveNumber = WaveNumber;

                        switch (WaveNumber)
                        {
                            case 1:
                                if (StageId < GameConfig.MimisbrunnrStartStageId)
                                {
                                    Player.GetExp(Exp * playCount, true);
                                }

                                break;
                            case 2:
                            {
                                ItemMap = Player.GetRewards(_waveRewards);
                                var dropBox = new DropBox(null, _waveRewards);
                                Log.Add(dropBox);
                                var getReward = new GetReward(null, _waveRewards);
                                Log.Add(getReward);
                                break;
                            }
                            default:
                                if (WaveNumber == _waves.Count)
                                {
                                    if (!IsCleared)
                                    {
                                        Log.newlyCleared = true;
                                    }
                                }

                                break;
                        }

                        break;
                    }

                    foreach (var other in Characters)
                    {
                        var current = Characters.GetPriority(other);
                        var speed = current * 0.6m;
                        Characters.UpdatePriority(other, speed);
                    }

                    Characters.Enqueue(character, TurnPriority / character.SPD);
                }

                // 제한 턴을 넘거나 플레이어가 죽은 경우 break;
                if (TurnNumber > TurnLimit ||
                    Player.IsDead)
                    break;
            }

            Log.result = Result;
            return Player;
        }

        private void SetWave(StageSheet.Row stageRow, StageWaveSheet.Row stageWaveRow)
        {
            var enemyStatModifiers = stageRow.EnemyOptionalStatModifiers;
            var waves = stageWaveRow.Waves;
            foreach (var wave in waves.Select(e => SpawnWave(e, enemyStatModifiers)))
            {
                _waves.Add(wave);
            }
        }

        private Wave SpawnWave(StageWaveSheet.WaveData waveData, IReadOnlyList<StatModifier> optionalStatModifiers)
        {
            var wave = new Wave();
            foreach (var monsterData in waveData.Monsters)
            {
                for (var i = 0; i < monsterData.Count; i++)
                {
                    CharacterSheet.TryGetValue(monsterData.CharacterId, out var row, true);
                    var enemyModel = new Enemy(Player, row, monsterData.Level, optionalStatModifiers);

                    wave.Add(enemyModel);
                    wave.HasBoss = waveData.HasBoss;
                }
            }

            return wave;
        }

        public static WeightedSelector<StageSheet.RewardData> SetItemSelector(StageSheet.Row stageRow, IRandom random)
        {
            var itemSelector = new WeightedSelector<StageSheet.RewardData>(random);
            foreach (var r in stageRow.Rewards)
            {
                itemSelector.Add(r, r.Ratio);
            }

            return itemSelector;
        }
    }
}
