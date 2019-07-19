using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Crypto;
using Libplanet.Store;
using Libplanet.Tx;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    [TestFixture]
    public class MinerFixture
    {
        private readonly string _storePath;
        private readonly PrivateKey _privateKey;
        private readonly LiteDBStore _store;
        private readonly BlockChain<PolymorphicAction<ActionBase>> _block;

        public MinerFixture()
        {
        }
        public MinerFixture(string storeName)
        {
            _storePath = $"{storeName}.ldb";
            const string hex = "02ed49dbe0f2c34d9dff8335d6dd9097f7a3ef17dfb5f048382eebc7f451a50aa1";
            _privateKey =
                new PrivateKey(ByteUtil.ParseHex(hex));
            if (File.Exists(_storePath))
                File.Delete(_storePath);
            _store = new LiteDBStore(_storePath);
            _block = new BlockChain<PolymorphicAction<ActionBase>>(AgentController.Agent.Policy, _store);
        }

        public IEnumerator CoMine(Transaction<PolymorphicAction<ActionBase>> transaction)
        {
            var task = Task.Run(() =>
                _block.StageTransactions(new Dictionary<Transaction<PolymorphicAction<ActionBase>>, bool>()
                    {
                        {
                            transaction,
                            true
                        }
                    }
                )
            );
            yield return new WaitUntil(() => task.IsCompleted);
            var mine = Task.Run(() => _block.MineBlock(_privateKey.PublicKey.ToAddress()));
            yield return new WaitUntil(() => mine.IsCompleted);
            var block = mine.Result;
            AgentController.Agent.AppendBlock(block);
        }

        public void TearDown()
        {
            _store.Dispose();
            if (File.Exists(_storePath))
                File.Delete(_storePath);
        }
    }
}
