using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Libplanet;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Store;
using Libplanet.Tx;
using Nekoyume.Data.Table;
using Nekoyume.Game;
using Nekoyume.Helper;
using UnityEngine;
using Uno.Extensions;

namespace Nekoyume.Action
{
    public class Agent
    {
        internal readonly BlockChain<ActionBase> blocks;
        private readonly PrivateKey privateKey;
        public readonly ConcurrentQueue<ActionBase> queuedActions;

        private const string kItemBoxPath = "Assets/Resources/DataTable/item_box.csv";
        private const string kItemEquipPath = "Assets/Resources/DataTable/item_equip.csv";
        private const string kItemPath = "Assets/Resources/DataTable/item.csv";

        private const float AvatarUpdateInterval = 3.0f;

        private const float ShopUpdateInterval = 3.0f;

        private const float TxProcessInterval = 3.0f;

        public Agent(PrivateKey privateKey, string path, Guid chainId)
        {
            IBlockPolicy<ActionBase> policy = new BlockPolicy<ActionBase>(TimeSpan.FromMilliseconds(500));
# if DEBUG
            policy = new DebugPolicy();
#endif
            this.privateKey = privateKey;
            blocks = new BlockChain<ActionBase>(
                policy,
                new FileStore(path),
                chainId);
            queuedActions = new ConcurrentQueue<ActionBase>();
            
#if BLOCK_LOG_USE
            FileHelper.WriteAllText("Block.log", "");
#endif
        }

        public Address UserAddress => privateKey.PublicKey.ToAddress();
        public Address ShopAddress => ActionManager.shopAddress;

        public event EventHandler<Context> DidReceiveAction;
        public event EventHandler<Shop> UpdateShop;

        public IEnumerator CoAvatarUpdator()
        {
            while (true)
            {
                yield return new WaitForSeconds(AvatarUpdateInterval);
                var task = Task.Run(() => blocks.GetStates(new[] {UserAddress}));
                yield return new WaitUntil(() => task.IsCompleted);
                var ctx = (Context) task.Result.GetValueOrDefault(UserAddress);
                if (ctx?.avatar != null)
                {
                    DidReceiveAction?.Invoke(this, ctx);
                }
                yield return null;
            }
        }

        public IEnumerator CoShopUpdator()
        {
            while (true)
            {
                yield return new WaitForSeconds(ShopUpdateInterval);
                var task = Task.Run(() => blocks.GetStates(new[] {ShopAddress}));
                yield return new WaitUntil(() => task.IsCompleted);
                var shop = (Shop) task.Result.GetValueOrDefault(ShopAddress);
                if (shop != null)
                {
                    UpdateShop?.Invoke(this, shop);
                }
            }
        }

        public IEnumerator CoTxProcessor() 
        {
            var actions = new List<ActionBase>();

            while (true)
            {
                yield return new WaitForSeconds(TxProcessInterval);
                ActionBase action;
                while (queuedActions.TryDequeue(out action))
                {
                    actions.Add(action);
                }
                
                if (actions.Count > 0)
                {
                    var task = Task.Run(() => 
                    {
                        StageActions(actions);
                        actions.Clear();
                    });
                    yield return new WaitUntil(() => task.IsCompleted);
                }
            }
        }

        public IEnumerator CoMiner()
        {
            while (true)
            {
                var task = Task.Run(() =>
                {
                    return blocks.MineBlock(UserAddress);
                });
                yield return new WaitUntil(() => task.IsCompleted);
                Debug.Log($"created block index: {task.Result.Index}");

#if BLOCK_LOG_USE
                FileHelper.AppendAllText("Block.log", task.Result.ToVerboseString());
#endif
            }
        }

        private void StageActions(IList<ActionBase> actions)
        {
            var tx = Transaction<ActionBase>.Create(
                privateKey,
                actions,
                timestamp: DateTime.UtcNow
            );
            blocks.StageTransactions(new HashSet<Transaction<ActionBase>> {tx});
        }

        public static Table<Item> ItemTable()
        {
            var itemTable = new Table<Item>();
            foreach (var path in new []{kItemPath, kItemBoxPath, kItemEquipPath})
            {
                var itemPath = Path.Combine(Directory.GetCurrentDirectory(), path);
                itemTable.Load(File.ReadAllText(itemPath));
            }

            return itemTable;
        }

        private class DebugPolicy : IBlockPolicy<ActionBase>
        {
            public InvalidBlockException ValidateBlocks(IEnumerable<Block<ActionBase>> blocks, DateTimeOffset currentTime)
            {
                return null;
            }

            public int GetNextBlockDifficulty(IEnumerable<Block<ActionBase>> blocks)
            {
                return blocks.Empty() ? 0 : 1;
            }
        }
    }
}
