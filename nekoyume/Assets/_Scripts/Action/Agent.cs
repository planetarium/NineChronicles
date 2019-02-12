using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Libplanet;
using Libplanet.Crypto;
using Libplanet.Store;
using Libplanet.Tx;
using Nekoyume.Data.Table;
using UnityEngine;

namespace Nekoyume.Action
{
    public class Agent
    {
        internal readonly Blockchain<ActionBase> blocks;
        private readonly float interval;
        private readonly PrivateKey privateKey;
        public readonly ConcurrentQueue<ActionBase> queuedActions;
        private const string kItemBoxPath = "Assets/Resources/DataTable/item_box.csv";
        private const string kItemEquipPath = "Assets/Resources/DataTable/item_equip.csv";
        private const string kItemPath = "Assets/Resources/DataTable/item.csv";

        public Agent(PrivateKey privateKey, string path, float interval = 3.0f)
        {
            this.privateKey = privateKey;
            this.interval = interval;
            blocks = new Blockchain<ActionBase>(new FileStore(path));
            queuedActions = new ConcurrentQueue<ActionBase>();
        }

        public Address UserAddress => privateKey.PublicKey.ToAddress();
        public event EventHandler<Context> DidReceiveAction;

        public IEnumerator Sync()
        {
            while (true)
            {
                yield return new WaitForSeconds(interval);
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

        public IEnumerator Mine()
        {
            while (true)
            {
                var processedActions = new List<ActionBase>();
                for (var i = 0; i < queuedActions.Count; i++)
                {
                    ActionBase action;
                    queuedActions.TryDequeue(out action);
                    if (action != null)
                    {
                        processedActions.Add(action);
                    }
                }

                StageTransaction(processedActions);
                var task = Task.Run(() => blocks.MineBlock(UserAddress));
                yield return new WaitUntil(() => task.IsCompleted);
                Debug.Log($"created block index: {task.Result.Index}");
            }
        }

        private void StageTransaction(IList<ActionBase> actions)
        {
            var tx = Transaction<ActionBase>.Make(
                privateKey,
                UserAddress,
                actions,
                DateTime.UtcNow
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
    }
}
