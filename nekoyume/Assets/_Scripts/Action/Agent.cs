using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using AsyncIO;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Net;
using Libplanet.Store;
using Libplanet.Tx;
using Nekoyume.Model;
#if BLOCK_LOG_USE
using Nekoyume.Helper;
#endif
using Nekoyume.Serilog;
using Nekoyume.State;
using Serilog;
using UniRx;
using UnityEngine;

namespace Nekoyume.Action
{
    public class Agent : IDisposable
    {
        private class DebugPolicy : IBlockPolicy<PolymorphicAction<ActionBase>>
        {
            public InvalidBlockException ValidateNextBlock(IReadOnlyList<Block<PolymorphicAction<ActionBase>>> blocks, Block<PolymorphicAction<ActionBase>> nextBlock)
            {
                return null;
            }

            public long GetNextBlockDifficulty(IReadOnlyList<Block<PolymorphicAction<ActionBase>>> blocks)
            {
                Thread.Sleep(SleepInterval);
                return blocks.Any() ? 1 : 0;
            }
        }
        
        private const float TxProcessInterval = 3.0f;
        private const float ActionRetryInterval = 15.0f;
        private const int RewardAmount = 1;
        
        public const string PrivateKeyFormat = "private_key_{0}";
        public const string AvatarFileFormat = "avatar_{0}.dat";
        
        public static event EventHandler<Model.Avatar> DidAvatarLoaded;
        
        private static readonly int SwarmDialTimeout = 5000;
        private static readonly TimeSpan BlockInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan SleepInterval = TimeSpan.FromSeconds(3);
        
        public static List<Model.Avatar> Avatars
        {
            get
            {
                return Enumerable.Range(0, 3).Select(index => string.Format(Agent.AvatarFileFormat, index))
                    .Select(fileName => Path.Combine(Application.persistentDataPath, fileName))
                    .Select(path => LoadStatus(path)).ToList();
            }
        }
        
        public readonly ConcurrentQueue<PolymorphicAction<ActionBase>> QueuedActions;
        
        private readonly BlockChain<PolymorphicAction<ActionBase>> _blocks;
        private readonly Swarm _swarm;
        private readonly ConcurrentQueue<GameAction> _actionPool;
        private readonly PrivateKey _agentPrivateKey;
        
        private PrivateKey _avatarPrivateKey;
        
        private string _saveFilePath;

        private IDisposable _disposableForEveryRender;
        
        public Guid ChainId => _blocks.Id;
        
        public Model.Avatar Avatar { get; set; }
        public BattleLog battleLog;

        public event EventHandler PreloadStarted;
        public event EventHandler<BlockDownloadState> PreloadProcessed;
        public event EventHandler PreloadEnded;

        static Agent() 
        {
            ForceDotNet.Force();
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Sink(new UnityDebugSink())
                .CreateLogger();
        }

        private static Model.Avatar LoadStatus(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }
            
            var formatter = new BinaryFormatter();
            using (FileStream stream = File.Open(path, FileMode.Open))
            {
                var data = (SaveData) formatter.Deserialize(stream);
                return data.Avatar;
            }
        }
        
        public Agent(
            PrivateKey agentPrivateKey,
            string path,
            Guid chainId,
            IEnumerable<Peer> peers,
            IEnumerable<IceServer> iceServers,
            string host,
            int? port)
        {
# if UNITY_EDITOR
            var policy = new DebugPolicy();
# else
            var policy = new BlockPolicy<PolymorphicAction<ActionBase>>(
                BlockInterval,
                0x2000,
                256
            );
#endif
            _agentPrivateKey = agentPrivateKey;
            _blocks = new BlockChain<PolymorphicAction<ActionBase>>(
                policy,
                new FileStore(path),
                chainId);
            QueuedActions = new ConcurrentQueue<PolymorphicAction<ActionBase>>();
            _actionPool = new ConcurrentQueue<GameAction>();
#if BLOCK_LOG_USE
            FileHelper.WriteAllText("Block.log", "");
#endif

            _swarm = new Swarm(
                agentPrivateKey,
                appProtocolVersion: 1,
                millisecondsDialTimeout: SwarmDialTimeout,
                host: host,
                listenPort: port,
                iceServers: iceServers);

            foreach (var peer in peers)
            {
                if (peer.PublicKey != agentPrivateKey.PublicKey)
                {
                    _swarm.Add(peer);
                }
            }

            AddressBook.Agent.Value = _agentPrivateKey.PublicKey.ToAddress();
        }

        public void Dispose()
        {
            _disposableForEveryRender.Dispose();
            _swarm?.StopAsync().Wait(0);
        }
        
        public IEnumerator CoSwarmRunner()
        {
            PreloadStarted?.Invoke(this, null);
            
            // Unity 플레이어에서 성능 문제로 Async를 직접 쓰지 않고 
            // Task.Run(async ()) 로 감쌉니다.
            var swarmPreloadTask = Task.Run(async () =>
            {
                await _swarm.PreloadAsync(_blocks,
                    new Progress<BlockDownloadState>(state => PreloadProcessed?.Invoke(this, state)));
            });
            yield return new WaitUntil(() => swarmPreloadTask.IsCompleted);
            
            PreloadEnded?.Invoke(this, null);

            var swarmStartTask = Task.Run(async () => await _swarm.StartAsync(_blocks));
            yield return new WaitUntil(() => swarmStartTask.IsCompleted);
        }

        public IEnumerator CoActionRetryer() 
        {
            HashDigest<SHA256>? previousTipHash = _blocks.Tip?.Hash;
            while (true)
            {
                yield return new WaitForSeconds(ActionRetryInterval);
                if (_blocks.Tip?.Hash is HashDigest<SHA256> currentTipHash && 
                    currentTipHash.Equals(previousTipHash)) 
                {
                    continue;
                }

                previousTipHash = _blocks.Tip.Hash;
                var task = Task.Run(() =>
                {
                    return (HashSet<Guid>)_blocks.GetStates(
                        new[] { GameAction.ProcessedActionsAddress }
                    ).GetValueOrDefault(
                        GameAction.ProcessedActionsAddress,
                        new HashSet<Guid>()
                    );
                });
                
                yield return new WaitUntil(() => task.IsCompleted);

                if (!task.IsFaulted && !task.IsCanceled) 
                {
                    var processedActions = task.Result;
                    while (_actionPool.TryDequeue(out GameAction action)) 
                    {
                        if (!processedActions.Contains(action.Id))
                        {
                            QueuedActions.Enqueue(action);
                        }
                    }
                }
            }
        }

        public IEnumerator CoTxProcessor()
        {
            while (true)
            {
                yield return new WaitForSeconds(TxProcessInterval);
                var actions = new List<PolymorphicAction<ActionBase>>();

                while (QueuedActions.TryDequeue(out PolymorphicAction<ActionBase> action))
                {
                    actions.Add(action);
                    if (action.InnerAction is GameAction asGameAction) 
                    {
                        _actionPool.Enqueue(asGameAction);
                    }
                }

                if (actions.Any())
                {
                    StageActions(actions);
                }
            }
        }

        public IEnumerator CoMiner()
        {
            ActionBase.EveryRender<RewardGold>()
                .Where(eval => eval.InputContext.Signer == AddressBook.Agent.Value)
                .Subscribe(eval =>
                {
//                    Model.AgentContext.gold.Value = eval.Action.gold;
                });
            
            while (true)
            {
                var tx = Transaction<PolymorphicAction<ActionBase>>.Create(
                        _agentPrivateKey,
                        new List<PolymorphicAction<ActionBase>>()
                        {
                            new RewardGold { gold = RewardAmount }
                        },
                        timestamp: DateTime.UtcNow);
                var txs = new HashSet<Transaction<PolymorphicAction<ActionBase>>> { tx };

                var task = Task.Run(() =>
                {
                    _blocks.StageTransactions(txs);
                    var block = _blocks.MineBlock(AddressBook.Agent.Value);
                    _swarm.BroadcastBlocks(new[] {block});
                    return block;
                });
                yield return new WaitUntil(() => task.IsCompleted);

                if (!task.IsCanceled && !task.IsFaulted)
                {
                    var block = task.Result;
                    Debug.Log($"created block index: {block.Index}, difficulty: {block.Difficulty}");
#if BLOCK_LOG_USE
                    FileHelper.AppendAllText("Block.log", task.Result.ToVerboseString());
#endif
                }
                else
                {
                    if (task.IsFaulted)
                    {
                        Debug.LogException(task.Exception);
                    }
                    _blocks.UnstageTransactions(txs);
                }
            }
        }
        
        public void InitAvatar(int index)
        {
            PrivateKey privateKey = null;
            var key = string.Format(PrivateKeyFormat, index);
            var privateKeyHex = PlayerPrefs.GetString(key, "");

            if (string.IsNullOrEmpty(privateKeyHex))
            {
                privateKey = new PrivateKey();
                PlayerPrefs.SetString(key, ByteUtil.Hex(privateKey.ByteArray));
            }
            else
            {
                privateKey = new PrivateKey(ByteUtil.ParseHex(privateKeyHex));
            }

            _avatarPrivateKey = privateKey;

            var fileName = string.Format(AvatarFileFormat, index);
            _saveFilePath = Path.Combine(Application.persistentDataPath, fileName);
            Avatar = LoadStatus(_saveFilePath);

            AddressBook.Avatar.Value = _avatarPrivateKey.PublicKey.ToAddress();
        }
        
        public object GetState(Address address)
        {
            AddressStateMap states = _blocks.GetStates(new[] {address});
            states.TryGetValue(address, out object value);
            return value;
        }
        
        /// <summary>
        /// FixMe. 모든 액션에 대한 랜더 단계에서 아바타 주소의 상태를 얻어 오고 있음.
        /// 모든 액션 생성 단계에서 각각의 변경점을 업데이트 하는 방향으로 수정해볼 필요성 있음.
        /// CreateNovice와 HackAndSlash 액션의 처리를 개선해서 테스트해 볼 예정.
        /// 시작 전에 양님에게 문의!
        /// </summary>
        public void SubscribeAvatarUpdates()
        {
            if (Avatar != null)
            {
                DidAvatarLoaded?.Invoke(this, Avatar);
            }

            _disposableForEveryRender = ActionBase.EveryRender(AddressBook.Avatar.Value).ObserveOnMainThread().Subscribe(eval =>
            {
                var ctx = (AvatarState) eval.OutputStates.GetState(AddressBook.Avatar.Value);
                if (!(ctx?.avatar is null))
                {
                    ReceiveAction(ctx);
                }
            });
        }
        
        private void ReceiveAction(AvatarState ctx)
        {
            var avatar = Avatar;
            Avatar = ctx.avatar;
            SaveStatus();
            if (avatar == null)
            {
                DidAvatarLoaded?.Invoke(this, Avatar);
            }
            battleLog = ctx.battleLog;
        }
        
        private void SaveStatus()
        {
            var data = new SaveData
            {
                Avatar = Avatar,
            };
            var formatter = new BinaryFormatter();
            using (FileStream stream = File.Open(_saveFilePath, FileMode.OpenOrCreate))
            {
                formatter.Serialize(stream, data);
            }
        }

        private void StageActions(IList<PolymorphicAction<ActionBase>> actions)
        {
            var tx = Transaction<PolymorphicAction<ActionBase>>.Create(
                _avatarPrivateKey,
                actions,
                timestamp: DateTime.UtcNow
            );
            _blocks.StageTransactions(new HashSet<Transaction<PolymorphicAction<ActionBase>>> {tx});
            _swarm.BroadcastTxs(new[] { tx });
        }
    }
}
