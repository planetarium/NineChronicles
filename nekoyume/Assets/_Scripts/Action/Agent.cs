using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Libplanet;
using Libplanet.Crypto;
using Libplanet.Store;
using Libplanet.Tx;
using UnityEngine;

namespace Nekoyume.Action
{
    public class Agent
    {
        public event EventHandler<ActionBase> DidReceiveAction;
        private readonly PrivateKey privateKey;
        private readonly float interval;
        public Address UserAddress => privateKey.PublicKey.ToAddress();

        internal readonly Blockchain<ActionBase> blocks;

        public Agent(PrivateKey privateKey, float interval = 3.0f)
        {
            this.privateKey = privateKey;
            this.interval = interval;
            var path = Path.Combine(Application.persistentDataPath, "planetarium");
            blocks = new Blockchain<ActionBase>(new FileStore(path));
        }

        public IEnumerator Sync()
        {
            while (true)
            {
                yield return new WaitForSeconds(interval);

//                var states = blocks.GetStates(new[] {UserAddress});
                yield return null;
            }
        }

        public IEnumerator Mine()
        {
//            while (true)
//            {
//                yield return new WaitForSeconds(interval);
//                yield return blocks.MineBlock(UserAddress);
//            }
            yield return blocks.MineBlock(UserAddress);

        }

        public void StageTransaction(IList<ActionBase> actions)
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
