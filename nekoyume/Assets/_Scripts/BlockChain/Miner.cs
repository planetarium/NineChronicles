using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Crypto;
using Libplanet.Net;
using Libplanet.Tx;
using Nekoyume.Action;
using Nekoyume.Model.State;
using Serilog;

namespace Nekoyume.BlockChain
{
    public class Miner
    {
        private BlockChain<PolymorphicAction<ActionBase>> _chain;
        private Swarm<PolymorphicAction<ActionBase>> _swarm;
        private PrivateKey _privateKey;

        public Address Address { get; }

        public Miner(BlockChain<PolymorphicAction<ActionBase>> chain, Swarm<PolymorphicAction<ActionBase>> swarm, PrivateKey privateKey)
        {
            _chain = chain ?? throw new ArgumentNullException(nameof(chain));
            _swarm = swarm ?? throw new ArgumentNullException(nameof(swarm));

            _privateKey = privateKey;
            Address = _privateKey.PublicKey.ToAddress();
        }
        
        private Transaction<PolymorphicAction<ActionBase>> RankingReward()
        {
            var rankingState = new RankingState((Bencodex.Types.Dictionary) _chain.GetState(RankingState.Address));
            // NOTE: 마이너가 돈과, Agent를 잡는 것이 옳을까요?
            var actions = new List<PolymorphicAction<ActionBase>>
            {
                new RankingReward
                {
                    gold1 = 50,
                    gold2 = 30,
                    gold3 = 10,
                    agentAddresses = rankingState.GetAgentAddresses(3, null),
                }
            };
            return _chain.MakeTransaction(_privateKey, actions.ToImmutableHashSet());
        }

        public void MineBlock()
        {
            var txs = new HashSet<Transaction<PolymorphicAction<ActionBase>>>();

            var timeStamp = DateTimeOffset.UtcNow;
            var prevTimeStamp = _chain?.Tip?.Timestamp;
            // FIXME 년도가 바뀌면 깨지는 계산 방식. 테스트 끝나면 변경해야함
            // 하루 한번 보상을 제공
            if (prevTimeStamp is DateTimeOffset t && timeStamp.DayOfYear - t.DayOfYear == 1)
            {
                var rankingRewardTx = RankingReward();
                txs.Add(rankingRewardTx);
            }

            var task = Task.Run(async () =>
            {
                var block = await _chain.MineBlock(Address);
                if (_swarm.Running)
                {
                    _swarm.BroadcastBlock(block);
                }

                return block;
            });

            if (!task.IsCanceled && !task.IsFaulted)
            {
                var block = task.Result;
                Log.Debug($"created block index: {block.Index}, difficulty: {block.Difficulty}");
#if BLOCK_LOG_USE
                    FileHelper.AppendAllText("Block.log", task.Result.ToVerboseString());
#endif
            }
            else if (task.Exception?.InnerExceptions.OfType<OperationCanceledException>().Count() != 0)
            {
                Log.Debug("Mining was canceled due to change of tip.");
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
                            var invalidNonceTx = _chain.GetTransaction(invalidTxNonceException.TxId);

                            if (invalidNonceTx.Signer == Address)
                            {
                                Log.Debug($"Tx[{invalidTxNonceException.TxId}] nonce is invalid. Retry it.");
                                retryActions.Add(invalidNonceTx.Actions);
                            }
                        }

                        if (ex is InvalidTxException invalidTxException)
                        {
                            Log.Debug($"Tx[{invalidTxException.TxId}] is invalid. mark to unstage.");
                            invalidTxs.Add(_chain.GetTransaction(invalidTxException.TxId));
                        }
                        else if (ex is UnexpectedlyTerminatedActionException actionException
                                 && actionException.TxId is TxId txId)
                        {
                            Log.Debug(
                                $"Tx[{actionException.TxId}]'s action is invalid. mark to unstage. {actionException}");
                            invalidTxs.Add(_chain.GetTransaction(txId));
                        }

                        Log.Error(ex, $"exception was thrown. {ex}");
                    }
                }

                _chain.UnstageTransactions(invalidTxs);

                foreach (var retryAction in retryActions)
                {
                    _chain.MakeTransaction(_privateKey, retryAction);
                }
            }
        }
    }
}
