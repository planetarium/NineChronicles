using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
using Nekoyume.Action;
#if BLOCK_LOG_USE
using Nekoyume.Helper;
#endif
using Nekoyume.Serilog;
using Serilog;
using UnityEngine;

namespace Nekoyume.BlockChain
{
    /// <summary>
    /// 메인넷에 직접 붙어서 블록을 마이닝 한다.
    /// </summary>
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
        private const int RewardAmount = 1;
        private const int SwarmDialTimeout = 5000;
        
        private static readonly TimeSpan BlockInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan SleepInterval = TimeSpan.FromSeconds(3);
        
        private readonly ConcurrentQueue<PolymorphicAction<ActionBase>> _queuedActions = new ConcurrentQueue<PolymorphicAction<ActionBase>>();
        private readonly BlockChain<PolymorphicAction<ActionBase>> _blocks;
        private readonly Swarm _swarm;
        private readonly LiteDBStore _store;

        
        public PrivateKey PrivateKey { get; }
        public Address Address { get; }
        public Guid ChainId => _blocks.Id;
        
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

        public Agent(
            PrivateKey privateKey,
            string path,
            Guid chainId,
            IEnumerable<Peer> peers,
            IEnumerable<IceServer> iceServers,
            string host,
            int? port)
        {
            var policy = GetPolicy();
            PrivateKey = privateKey;
            Address = privateKey.PublicKey.ToAddress();
            _store = new LiteDBStore($"{path}.ldb");
            _blocks = new BlockChain<PolymorphicAction<ActionBase>>(
                policy,
                _store,
                chainId);
#if BLOCK_LOG_USE
            FileHelper.WriteAllText("Block.log", "");
#endif

            _swarm = new Swarm(
                privateKey,
                appProtocolVersion: 1,
                millisecondsDialTimeout: SwarmDialTimeout,
                host: host,
                listenPort: port,
                iceServers: iceServers);

            foreach (var peer in peers)
            {
                if (peer.PublicKey != privateKey.PublicKey)
                {
                    _swarm.Add(peer);
                }
            }
        }

        public void Dispose()
        {
            _store?.Dispose();
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

        public IEnumerator CoTxProcessor()
        {
            while (true)
            {
                yield return new WaitForSeconds(TxProcessInterval);
                var actions = new List<PolymorphicAction<ActionBase>>();

                while (_queuedActions.TryDequeue(out PolymorphicAction<ActionBase> action))
                {
                    actions.Add(action);
                }

                if (actions.Any())
                {
                    var task = Task.Run(() => AvatarManager.MakeTransaction(actions, _blocks));
                    yield return new WaitUntil(() => task.IsCompleted);
                }
            }
        }

        public IEnumerator CoMiner()
        {
            while (true)
            {
                var rewardGoldTx = RewardGold();
                var task = Task.Run(() =>
                {
                    var block = _blocks.MineBlock(Address);
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
                    var invalidTxs = new HashSet<Transaction<PolymorphicAction<ActionBase>>> { rewardGoldTx };
                    var retryActions = new HashSet<IImmutableList<PolymorphicAction<ActionBase>>>();

                    if (task.IsFaulted)
                    {
                        foreach (var ex in task.Exception.InnerExceptions)
                        {
                            if (ex is InvalidTxNonceException invalidTxNonceException)
                            {
                                var invalidNonceTx = _blocks.Transactions[invalidTxNonceException.TxId];

                                if (invalidNonceTx.Signer == Address && invalidNonceTx.Id != rewardGoldTx.Id)
                                {
                                    Debug.Log($"Tx[{invalidTxNonceException.TxId}] nonce is invalid. Retry it.");
                                    retryActions.Add(invalidNonceTx.Actions);
                                }
                            }

                            if (ex is InvalidTxException invalidTxException)
                            {
                                Debug.Log($"Tx[{invalidTxException.TxId}] is invalid. mark to unstage.");
                                invalidTxs.Add(_blocks.Transactions[invalidTxException.TxId]);
                            }

                            Debug.LogException(ex);
                        }
                    }
                    _blocks.UnstageTransactions(invalidTxs);

                    foreach (var retryAction in retryActions)
                    {
                        _blocks.MakeTransaction(PrivateKey, retryAction);
                    }
                }
            }
        }

        public void EnqueueAction(GameAction gameAction)
        {
            _queuedActions.Enqueue(gameAction);
        }
        
        public object GetState(Address address)
        {
            AddressStateMap states = _blocks.GetStates(new[] {address});
            states.TryGetValue(address, out object value);
            return value;
        }
        
        public CreateAvatar CreateAvatar(Address avatarAddress, int index, string nickName)
        {
            var createAvatar = new CreateAvatar
            {
                avatarAddress = avatarAddress,
                index = index,
                name = nickName,
            };
            var actions = new List<PolymorphicAction<ActionBase>>
            {
                createAvatar
            };
            Task.Run(() => _blocks.MakeTransaction(PrivateKey, actions));

            return createAvatar;
        }
        
        public DeleteAvatar DeleteAvatar(int index, Address avatarAddress)
        {
            var deleteAvatar = new DeleteAvatar
            {
                index = index,
                avatarAddress = avatarAddress,
            };
            var actions = new List<PolymorphicAction<ActionBase>>
            {
                deleteAvatar
            };
            Task.Run(() => _blocks.MakeTransaction(PrivateKey, actions));

            return deleteAvatar;
        }

        private IBlockPolicy<PolymorphicAction<ActionBase>> GetPolicy()
        {
# if UNITY_EDITOR
            return new DebugPolicy();
# else
            return new BlockPolicy<PolymorphicAction<ActionBase>>(
                BlockInterval,
                0x2000,
                256
            );
#endif
        }

        private Transaction<PolymorphicAction<ActionBase>> RewardGold()
        {
            var actions = new List<PolymorphicAction<ActionBase>>
            {
                new RewardGold
                {
                    gold = RewardAmount
                }
            };
            return _blocks.MakeTransaction(
                PrivateKey,
                actions,
                broadcast: false);
        }
    }
}
