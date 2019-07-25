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
        private const int SwarmDialTimeout = 5000;
        
        private static readonly TimeSpan BlockInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan SleepInterval = TimeSpan.FromSeconds(3);
        
        private readonly ConcurrentQueue<PolymorphicAction<ActionBase>> _queuedActions = new ConcurrentQueue<PolymorphicAction<ActionBase>>();
        protected readonly BlockChain<PolymorphicAction<ActionBase>> _blocks;
        private readonly Swarm<PolymorphicAction<ActionBase>> _swarm;
        protected readonly LiteDBStore _store;
        private readonly IImmutableSet<Address> _trustedPeers;

        public IDictionary<TxId, Transaction<PolymorphicAction<ActionBase>>> Transactions => _blocks.Transactions;
        public IBlockPolicy<PolymorphicAction<ActionBase>> Policy => _blocks.Policy;

        public PrivateKey PrivateKey { get; }
        public Address Address { get; }
        
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
            IEnumerable<Peer> peers,
            IEnumerable<IceServer> iceServers,
            string host,
            int? port)
        {
            var policy = GetPolicy();
            PrivateKey = privateKey;
            Address = privateKey.PublicKey.ToAddress();
            _store = new LiteDBStore($"{path}.ldb");
            _blocks = new BlockChain<PolymorphicAction<ActionBase>>(policy, _store);
#if BLOCK_LOG_USE
            FileHelper.WriteAllText("Block.log", "");
#endif

            _swarm = new Swarm<PolymorphicAction<ActionBase>>(
                _blocks,
                privateKey,
                appProtocolVersion: 1,
                millisecondsDialTimeout: SwarmDialTimeout,
                host: host,
                listenPort: port,
                iceServers: iceServers);

            var otherPeers = peers.Where(peer => peer.PublicKey != privateKey.PublicKey).ToList();
            _swarm.AddPeersAsync(otherPeers);

            // FIXME: Trusted peers should be configurable
            _trustedPeers = otherPeers.Select(peer => peer.Address).ToImmutableHashSet();
        }

        public void Dispose()
        {
            _store?.Dispose();
            _swarm?.StopAsync().Wait(0);
        }
        
        public IEnumerator CoSwarmRunner()
        {
            PreloadStarted?.Invoke(this, null);
            Debug.Log("PreloadingStarted event was invoked");

            DateTimeOffset started = DateTimeOffset.UtcNow;
            long existingBlocks = _blocks?.Tip?.Index ?? 0;
            Debug.Log("Preloading starts");
            Task swarmPreloadTask = _swarm.PreloadAsync(
                new Progress<BlockDownloadState>(state =>
                    PreloadProcessed?.Invoke(this, state)
                ),
                trustedStateValidators: _trustedPeers
            );
            yield return new WaitUntil(() => swarmPreloadTask.IsCompleted);
            DateTimeOffset ended = DateTimeOffset.UtcNow;

            if (swarmPreloadTask.Exception is Exception exc)
            {
                Debug.LogErrorFormat(
                    "Preloading terminated with an exception: {0}",
                    exc
                );
                throw exc;
            }

            var index = _blocks?.Tip?.Index ?? 0;
            Debug.LogFormat(
                "Preloading finished; elapsed time: {0}; blocks: {1}",
                ended - started,
                index - existingBlocks
            );


            PreloadEnded?.Invoke(this, null);

            var swarmStartTask = Task.Run(async () =>
            {
                try
                {
                    await _swarm.StartAsync();
                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat(
                        "Swarm terminated with an exception: {0}",
                        e
                    );
                    throw;
                }
            });

            Task.Run(async () =>
            {
                await _swarm.WaitForRunningAsync();

                Debug.LogFormat(
                    "The address of this node: {0},{1},{2}",
                    ByteUtil.Hex(PrivateKey.PublicKey.Format(true)),
                    _swarm.EndPoint.Host,
                    _swarm.EndPoint.Port
                );
            });

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

                var txs = new HashSet<Transaction<PolymorphicAction<ActionBase>>> {rewardGoldTx};

                var timeStamp = DateTimeOffset.UtcNow;
                var prevTimeStamp = _blocks?.Tip?.Timestamp;
                //FIXME 년도가 바뀌면 깨지는 계산 방식. 테스트 끝나면 변경해야함
                // 하루 한번 보상을 제공
                if (prevTimeStamp is DateTimeOffset t && timeStamp.DayOfYear - t.DayOfYear == 1)
                {
                    var rankingRewardTx = RankingReward();
                    txs.Add(rankingRewardTx);
                }

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
                    var invalidTxs = txs;
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
            var actions = new List<PolymorphicAction<ActionBase>> {
                new RewardGold
                {
                    gold = 1
                }
            };
            return _blocks.MakeTransaction(
                PrivateKey,
                actions,
                broadcast: false);
        }

        private Transaction<PolymorphicAction<ActionBase>> RankingReward()
        {
            var actions = new List<PolymorphicAction<ActionBase>>
            {
                new RankingReward
                {
                    gold1 = 10,
                    gold2 = 5,
                    gold3 = 3,
                    agentAddresses = States.Instance.rankingState.Value.GetAgentAddresses(3, null),
                }
            };
            return _blocks.MakeTransaction(
                PrivateKey,
                actions,
                broadcast: false);
        }

        public void AppendBlock(Block<PolymorphicAction<ActionBase>> block)
        {
            _blocks.Append(block);
        }
    }
}
