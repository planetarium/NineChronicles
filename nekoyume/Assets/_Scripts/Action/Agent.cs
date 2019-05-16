using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
using Nekoyume.Game;
#if BLOCK_LOG_USE
using Nekoyume.Helper;
#endif
using Nekoyume.Serilog;
using Serilog;
using UnityEngine;

namespace Nekoyume.Action
{
    public class Agent : IDisposable
    {
        private readonly BlockChain<PolymorphicAction<ActionBase>> _blocks;
        private readonly PrivateKey _agentPrivateKey;
        public readonly ConcurrentQueue<PolymorphicAction<ActionBase>> QueuedActions;

        private const float TxProcessInterval = 3.0f;

        private static readonly int SwarmDialTimeout = 5000;

        private const float ActionRetryInterval = 15.0f;

        private readonly Swarm _swarm;

        private readonly ConcurrentQueue<GameAction> _actionPool;

        private const int RewardAmount = 1;

        private static readonly TimeSpan BlockInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan SleepInterval = TimeSpan.FromSeconds(3);

        static Agent() 
        {
            ForceDotNet.Force();
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Sink(new UnityDebugSink())
                .CreateLogger();
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
            this._agentPrivateKey = agentPrivateKey;
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
        }

        public PrivateKey AvatarPrivateKey { get; set; }
        public Address AvatarAddress => AvatarPrivateKey.PublicKey.ToAddress();
        public Address ShopAddress => ActionManager.shopAddress;
        public Address AgentAddress => _agentPrivateKey.PublicKey.ToAddress();

        public Guid ChainId => _blocks.Id;

        public event EventHandler<Shop> UpdateShop;

        public event EventHandler PreloadStarted;
        public event EventHandler<BlockDownloadState> PreloadProcessed;
        public event EventHandler PreloadEnded;


        public IEnumerator CoSwarmRunner()
        {
            PreloadStarted?.Invoke(this, null);
            // Unity 플레이어에서 성능 문제로 Async를 직접 쓰지 않고 
            // Task.Run(async ()) 로 감쌉니다.
            Task preload = Task.Run(async () => 
            {
                await _swarm.PreloadAsync(_blocks, new Progress<BlockDownloadState>(
                    state => 
                    {
                        PreloadProcessed?.Invoke(this, state);
                    }
                ));
            });
            yield return new WaitUntil(() => preload.IsCompleted);
            PreloadEnded?.Invoke(this, null);
            
            Task runSwarm = Task.Run(async () => await _swarm.StartAsync(_blocks));

            yield return new WaitUntil(() => runSwarm.IsCompleted);
        }

        public IEnumerator CoActionRetryer() 
        {
            while (true)
            {
                yield return new WaitForSeconds(ActionRetryInterval);
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
            while (true)
            {
                var tx = Transaction<PolymorphicAction<ActionBase>>.Create(
                        _agentPrivateKey,
                        new List<PolymorphicAction<ActionBase>>()
                        {
                            new RewardGold { Gold = RewardAmount }
                        },
                        timestamp: DateTime.UtcNow);
                var txs = new HashSet<Transaction<PolymorphicAction<ActionBase>>> { tx };

                var task = Task.Run(() =>
                {
                    _blocks.StageTransactions(txs);
                    var block = _blocks.MineBlock(AgentAddress);
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
                        UnityEngine.Debug.LogException(task.Exception);
                    }
                    _blocks.UnstageTransactions(txs);
                }
            }
        }

        private void StageActions(IList<PolymorphicAction<ActionBase>> actions)
        {
            var tx = Transaction<PolymorphicAction<ActionBase>>.Create(
                AvatarPrivateKey,
                actions,
                timestamp: DateTime.UtcNow
            );
            _blocks.StageTransactions(new HashSet<Transaction<PolymorphicAction<ActionBase>>> {tx});
            _swarm.BroadcastTxs(new[] { tx });
        }

        public object GetState(Address address)
        {
            AddressStateMap states = _blocks.GetStates(new[] {address});
            states.TryGetValue(address, out object value);
            return value;
        }

        public void Dispose()
        {
            _swarm?.StopAsync().Wait(0);
        }

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
    }
}
