using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Libplanet;
using Libplanet.Crypto;
using Libplanet.Store;
using Libplanet.Tx;
using UnityEngine;
using Avatar = Nekoyume.Model.Avatar;

namespace Nekoyume.Action
{
    public class Agent
    {
        public event EventHandler<Avatar> DidReceiveAction;
        private readonly PrivateKey privateKey;
        private readonly float interval;
        public Address UserAddress => privateKey.PublicKey.ToAddress();
        public List<ActionBase> incompleteActions;


        internal readonly Blockchain<ActionBase> blocks;

        public Agent(PrivateKey privateKey, string path, float interval = 3.0f)
        {
            this.privateKey = privateKey;
            this.interval = interval;
            blocks = new Blockchain<ActionBase>(new FileStore(path));
            incompleteActions = new List<ActionBase>();
        }
        
        public IEnumerator Sync()
        {
            while (true)
            {
                yield return new WaitForSeconds(interval);
                var task = Task.Run(() => blocks.GetStates(new[] {UserAddress}));
                yield return new WaitUntil(() => task.IsCompleted);
                var avatar = (Avatar) task.Result.GetValueOrDefault(UserAddress);
                if (avatar != null)
                {
                    DidReceiveAction?.Invoke(this, avatar);
                }

                yield return null;
            }
        }

        public IEnumerator Mine()
        {
            while (true)
            {
                var actions = incompleteActions.ToList();
                if (actions.Count > 0)
                {
                    Debug.Assert(actions != incompleteActions);
                }
                StageTransaction(actions);
                var task = Task.Run(() => blocks.MineBlock(UserAddress));
                yield return new WaitUntil(() => task.IsCompleted);

                if (actions.Count < incompleteActions.Count)
                {
                    incompleteActions = incompleteActions.Except(actions).ToList();
                }
                else
                {
                    incompleteActions.Clear();
                }
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
            blocks.StageTransactions(new HashSet<Transaction<ActionBase>> { tx });
        }
    }
}